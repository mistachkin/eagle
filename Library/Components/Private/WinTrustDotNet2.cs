// Authenticode cross-platform verification for .NET Core 2.x+ (macOS/Linux/Windows)
// - No third-party packages (only System.*). On .NET Core 2.x, add Microsoft package:
//      <PackageReference Include="System.Security.Cryptography.Pkcs" Version="4.*" />
// - Verifies:
//     1) CMS/PKCS#7 signature (cryptographic).
//     2) PE Authenticode file hash matches (SpcIndirectDataContent Digest).
//     3) Signer chain trust (OS store or custom root store).
//     4) Timestamp (PKCS#7 countersignature and/or RFC 3161 timeStampToken): cryptographic, binding, TSA EKU, chain trust.
// - Optional online revocation via X509ChainPolicy.RevocationMode.
//
// References (APIs / specs):
// * SignedCms / SignerInfo in System.Security.Cryptography.Pkcs (cross-platform). See .NET 2.1+ announcement.
// * RFC 3161 timestamp format and TSA EKU 1.3.6.1.5.5.7.3.8.
// * Authenticode timestamp OIDs: counterSignature 1.2.840.113549.1.9.6; RFC3161 id-aa-timeStampToken 1.2.840.113549.1.9.16.2.14; Microsoft 1.3.6.1.4.1.311.3.3.1.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Eagle._Attributes;

namespace Eagle._Components.Private
{
    internal static partial class WinTrustDotNet
    {
        // ---------- Options & Results ----------

        [ObjectId("b9c0859a-615c-4e09-bd23-bd64919e297f")]
        public sealed class VerificationOptions
        {
            // Chain & revocation
            public bool ValidateSignerChain = true;
            public X509RevocationMode RevocationMode = X509RevocationMode.NoCheck;   // Online to enable HTTP/OCSP/CRL
            public TimeSpan RevocationUrlRetrievalTimeout = TimeSpan.FromSeconds(15);

            // Custom roots (directory with *.cer/*.crt/*.pem)
            public bool UseCustomRootTrust = false;
            public string CustomRootDirectory = null;
            // If runtime lacks CustomRootTrust (e.g., .NET Core 2.x), allow compat fallback:
            public bool TrustIfOnlyUntrustedRootAndMatchesCustomRoot = true;

            // Extra intermediates (optional)
            public string AdditionalIntermediatesDirectory = null;

            // Timestamp policy
            public bool RequireTimestamp = false;             // if true, AllValid requires a valid timestamp.
            public bool ValidateTimestampChain = true;        // build TSA chain (EKU timeStamping).
            public bool PreferRfc3161OverCountersign = true;  // try RFC3161 token first if both exist.
            public bool HashAnyRemainingBytes = true;         // hash bytes that should not be there?

            // Advanced: Require TSA EKU. Standards say yes; set false to relax.
            public bool RequireTsaEku = true; // 1.3.6.1.5.5.7.3.8

            // NEW: AIA auto-download (intermediate discovery) ---------------------
            public bool AutoDownloadIntermediates = true;        // behave like WinVerifyTrust
            public bool AllowAiaInsecureHttp = true;             // many AIA URLs are plain http; cryptographic verification still applies
            public TimeSpan AiaHttpTimeout = TimeSpan.FromSeconds(10);
            public int AiaMaxDepth = 3;                          // chase up to 3 hops of intermediates
        }

        [ObjectId("47831b1d-b6cf-4643-8bcf-b8700a1b0cad")]
        public sealed class TimestampInfo
        {
            public bool Present;
            public bool IsRfc3161;
            public bool CryptographicallyValid;
            public bool BoundToSignerSignature;      // RFC3161: messageImprint == hash(signer.Signature)
            public bool ChainValid;
            public string TsaSubject;
            public string TsaThumbprint;
            public DateTimeOffset? TimeUtc;
            public string[] ChainStatus = Array.Empty<string>();
            public DateTimeOffset? TimeLocal => TimeUtc.HasValue ? TimeUtc.Value.ToLocalTime() : (DateTimeOffset?)null;
        }

        [ObjectId("a8a46ae4-2929-4a1b-bc38-c7ca78a91019")]
        public sealed class VerificationResult
        {
            public bool IsSigned;
            public bool CmsSignatureValid;
            public bool FileHashMatchesSignature;
            public bool SignerChainValid;
            public string[] SignerChainStatus = Array.Empty<string>();

            public string DigestAlgorithmOid;
            public string SignerSubject;
            public string SignerThumbprint;

            public DateTimeOffset? SigningTimeUtc;
            public TimestampInfo Timestamp = new TimestampInfo();

            public bool AllValid
            {
                get
                {
                    if (!IsSigned) return false;
                    if (!CmsSignatureValid) return false;
                    if (!FileHashMatchesSignature) return false;
                    if (!SignerChainValid) return false;

                    // Enforce timestamp only if the caller requires one
                    if (OptionsReference != null && OptionsReference.RequireTimestamp)
                    {
                        if (!(Timestamp != null && Timestamp.Present)) return false;
                        if (!Timestamp.BoundToSignerSignature) return false;
                        if (!Timestamp.CryptographicallyValid) return false;
                        //
                        // HACK: This is needed due to: https://github.com/dotnet/runtime/issues/62307
                        //
                        // NOTE: Please also see: https://github.com/dotnet/runtime/issues/65163
                        //                        https://github.com/dotnet/runtime/pull/64348
                        //
                        if (OptionsReference.ValidateTimestampChain && !Timestamp.ChainValid) return false;
                    }

                    return true;
                }
            }

            internal VerificationOptions OptionsReference; // for AllValid computation w/ RequireTimestamp

            public override string ToString()
            {
                var ts = Timestamp != null && Timestamp.Present
                    ? $"Timestamp: {(Timestamp.IsRfc3161 ? "RFC3161" : "CounterSig")}, " +
                      $"Crypto={Timestamp.CryptographicallyValid}, Bound={Timestamp.BoundToSignerSignature}, " +
                      $"Chain={Timestamp.ChainValid}, TimeUTC={Timestamp.TimeUtc}, TimeLocal={Timestamp.TimeLocal}"
                    : "Timestamp: none";
                return $"Signed={IsSigned}, CMS={CmsSignatureValid}, FileDigestMatch={FileHashMatchesSignature}, " +
                       $"SignerChain={SignerChainValid}, Signer='{SignerSubject}', Thumbprint={SignerThumbprint}, {ts}";
            }
        }

        // ---------- Public entry point ----------

        public static VerificationResult VerifyPeFileSignature(string filePath, VerificationOptions options = null)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);
            if (options == null) options = new VerificationOptions();

            using (FileStream fs = File.OpenRead(filePath))
            {
                // 1) Locate PE offsets (checksum, security directory, certificate table)
                var pe = ReadPeLayout(fs);
                if (pe.CertTableOffset == 0 || pe.CertTableSize == 0 || pe.CertTableOffset >= fs.Length)
                {
                    return new VerificationResult
                    {
                        IsSigned = false,
                        OptionsReference = options
                    };
                }

                // 2) Read first PKCS#7 blob from WIN_CERTIFICATE table
                byte[] pkcs7 = ReadFirstPkcs7FromWinCertificateTable(fs, pe.CertTableOffset, pe.CertTableSize);
                if (pkcs7 == null || pkcs7.Length == 0)
                {
                    return new VerificationResult
                    {
                        IsSigned = false,
                        OptionsReference = options
                    };
                }

                var result = new VerificationResult { IsSigned = true, OptionsReference = options };

                // 3) Decode CMS
                var cms = new SignedCms();
                cms.Decode(pkcs7);

                // 4) Cryptographic verification of CMS signatures (not chain)
                try
                {
                    cms.CheckSignature(true);
                    result.CmsSignatureValid = true;
                }
                catch (CryptographicException)
                {
                    result.CmsSignatureValid = false;
                }

                // 5) Parse SpcIndirectDataContent to learn digest algorithm and expected digest
                byte[] eContent = cms.ContentInfo.Content;
                string digestOid;
                byte[] expectedDigest;
                if (!TryParseSpcIndirectDataDigest(eContent, out digestOid, out expectedDigest))
                    throw new InvalidDataException("Failed to parse SpcIndirectDataContent / DigestInfo.");

                result.DigestAlgorithmOid = digestOid;

                // 6) Compute the Authenticode hash of the file and compare
                using (HashAlgorithm hash = CreateHashFromOid(digestOid))
                {
                    if (hash == null) throw new NotSupportedException("Unsupported digest algorithm OID: " + digestOid);
                    byte[] actual = ComputeAuthenticodeHash(fs, hash, pe, options);
                    result.FileHashMatchesSignature = expectedDigest != null && actual != null && BytesEqual(expectedDigest, actual);
                }

                // 7) Get primary signer (first)
                X509Certificate2 signerCert = null;
                SignerInfo primarySigner = null;
                if (cms.SignerInfos.Count > 0)
                {
                    primarySigner = cms.SignerInfos[0];
                    signerCert = primarySigner.Certificate;
                }

                // SigningTime (if present)
                result.SigningTimeUtc = TryGetSigningTimeUtc(primarySigner);

                // 8) Build signer chain (OS trust or custom roots)
                if (options.ValidateSignerChain && signerCert != null)
                {
                    X509Certificate2Collection extra = new X509Certificate2Collection();
                    for (int i = 0; i < cms.Certificates.Count; i++)
                    {
                        var c = cms.Certificates[i];
                        if (!CertEqualThumbprint(c, signerCert))
                            extra.Add(c);
                    }

                    // Add user-supplied intermediates
                    X509Certificate2[] extraFromDir = LoadCertificatesFromDirectory(options.AdditionalIntermediatesDirectory);
                    for (int i = 0; extraFromDir != null && i < extraFromDir.Length; i++) extra.Add(extraFromDir[i]);

                    var customRoots = LoadCertificatesFromDirectory(options.CustomRootDirectory);

                    // Discover timestamp across all signers & nests *before* building the signer chain:
                    result.Timestamp = FindBestTimestampAcrossSigners(cms, options);

                    // Prefer the timestamp time (if present) when building the signer chain
                    DateTimeOffset? chainTime = result.Timestamp != null ? result.Timestamp.TimeUtc : null;

                    string[] statuses;

                    // Build signer chain (this is the call you already upgraded to AIA-aware earlier)
                    bool chainOk = BuildChainWithAutoAia(
                        signerCert, extra, /*verificationTime*/ chainTime,
                        options, customRoots, /*requireTimeStampingEku*/ false,
                        out statuses);

                    result.SignerChainValid = chainOk;
                    result.SignerChainStatus = statuses;
                    result.SignerSubject = signerCert.Subject;
                    result.SignerThumbprint = signerCert.Thumbprint;
                }
                else
                {
                    result.SignerChainValid = !options.ValidateSignerChain; // if not required, treat as ok
                }

                return result;
            }
        }

        // ---------- Timestamp verification ----------

        // Try to extract a nested ContentInfo from Microsoft RFC3161 attr if it's a wrapper:
        // TimeStampResponseWrapper ::= SEQUENCE { oid OBJECT IDENTIFIER, attributes [OPTIONAL], content ContentInfo }
        private static bool TryUnwrapMicrosoftRfc3161(byte[] attrValue, out byte[] contentInfoDer)
        {
            contentInfoDer = null;
            try
            {
                var r = new DerReader(attrValue);
                var seq = r.ReadSequence();
                // First element: OID (but some signers omit it); if present, just skip.
                // If first tag is OID, read it, else assume we're already at ContentInfo.
                byte nextTagPeek = seq.PeekTag();
                if (nextTagPeek == 0x06) { seq.ReadOid(); nextTagPeek = seq.PeekTag(); }
                // Optional attributes: SET/SEQUENCE – skip if present.
                if (nextTagPeek == 0x31 || nextTagPeek == 0x30) { seq.SkipValue(); nextTagPeek = seq.PeekTag(); }
                // Now expect ContentInfo (SEQUENCE). Return its raw slice.
                int start, length;
                if (seq.TryReadRawSequence(out start, out length))
                {
                    contentInfoDer = new byte[length];
                    Buffer.BlockCopy(attrValue, start, contentInfoDer, 0, length);
                    return true;
                }
            }
            catch { /* fall through */ }
            return false;
        }

        // Legacy PKCS #7 countersignature (OID 1.2.840.113549.1.9.6)
        // We verify: countersignature signature itself; that messageDigest(signedAttrs) == HASH(parent signature);
        // and (optionally) TSA chain trust (usually TSA cert has EKU timeStamping).
        private static bool TryVerifyCounterSignatureTimestamp(SignerInfo signer, SignedCms rootCms, VerificationOptions options, TimestampInfo info)
        {
            SignerInfo countersigner = null;

            // Look at CounterSignerInfos collection
            if (signer.CounterSignerInfos != null && signer.CounterSignerInfos.Count > 0)
            {
                countersigner = signer.CounterSignerInfos[0];
            }
            else
            {
                // Some implementations include the OID but not the convenient property; try to detect attribute anyway.
                var ua = signer.UnsignedAttributes;
                Oid oidCounter = new Oid("1.2.840.113549.1.9.6");
                for (int i = 0; i < ua.Count; i++)
                {
                    var attr = ua[i];
                    if (attr.Oid != null && StringEquals(attr.Oid.Value, oidCounter.Value))
                    {
                        // .NET exposes countersigners via CounterSignerInfos, so if it's missing, we can't easily decode here.
                        break;
                    }
                }
            }

            if (countersigner == null) return false;

            info.Present = true;
            info.IsRfc3161 = false;

            // 1) Verify countersignature cryptographically
            try
            {
                countersigner.CheckSignature(true);
                info.CryptographicallyValid = true;
            }
            catch
            {
                info.CryptographicallyValid = false;
            }

            // 2) Extract signingTime from countersigner (if present)
            info.TimeUtc = TryGetSigningTimeUtc(countersigner);

            // 3) Verify binding: messageDigest(signedAttrs) == HASH(parentSigner.GetSignature())
            byte[] parentSig = null;
            try { parentSig = GetSignerSignatureBytes(signer, rootCms); } catch { parentSig = null; }
            if (parentSig != null)
            {
                // Find signed attribute messageDigest (OID 1.2.840.113549.1.9.4)
                byte[] mdAttr = GetSingleAttributeOctetValue(countersigner.SignedAttributes, "1.2.840.113549.1.9.4");
                string csHashOid = (countersigner.DigestAlgorithm != null && countersigner.DigestAlgorithm.Value != null)
                    ? countersigner.DigestAlgorithm.Value : null;

                if (mdAttr != null && csHashOid != null)
                {
                    using (var h = CreateHashFromOid(csHashOid))
                    {
                        if (h != null)
                        {
                            byte[] digest = h.ComputeHash(parentSig);
                            info.BoundToSignerSignature = BytesEqual(digest, mdAttr);
                        }
                    }
                }
            }

            // 4) TSA chain trust (countersigner cert usually has EKU timeStamping)
            if (options.ValidateTimestampChain && countersigner.Certificate != null)
            {
                X509Certificate2 tsa = countersigner.Certificate;
                X509Certificate2Collection extra = new X509Certificate2Collection();
                for (int i = 0; i < rootCms.Certificates.Count; i++)
                    if (!CertEqualThumbprint(rootCms.Certificates[i], tsa))
                        extra.Add(rootCms.Certificates[i]);

                X509Certificate2[] customRoots = LoadCertificatesFromDirectory(options.CustomRootDirectory);
                X509Certificate2[] extraFromDir = LoadCertificatesFromDirectory(options.AdditionalIntermediatesDirectory);
                for (int i = 0; extraFromDir != null && i < extraFromDir.Length; i++) extra.Add(extraFromDir[i]);

                string[] statuses;
                bool chainOk = BuildChainWithAutoAia(
                    tsa, extra, info.TimeUtc, options, customRoots,
                    /*requireTimeStampingEku*/ options.RequireTsaEku, out statuses);

                info.ChainValid = chainOk;
                info.ChainStatus = statuses;
                info.TsaSubject = tsa.Subject;
                info.TsaThumbprint = tsa.Thumbprint;
            }
            else
            {
                info.ChainValid = !options.ValidateTimestampChain;
            }

            return true;
        }

        // OIDs we’ll look for
        private const string OID_CMS_SIGNED_DATA = "1.2.840.113549.1.7.2";
        private const string OID_RFC3161_TSTOKEN = "1.2.840.113549.1.9.16.2.14";
        private const string OID_MS_TSTOKEN     = "1.3.6.1.4.1.311.3.3.1";
        private const string OID_MS_NESTED_SIG  = "1.3.6.1.4.1.311.2.4.1";

        // ============ Public entry point (returns a formatted string) ============

        public static string DumpSignatureAttributes(string filePath, bool includeNested = true)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            var sb = new StringBuilder();

            using (var fs = File.OpenRead(filePath))
            {
                var pe = ReadPeLayout(fs);
                if (pe.CertTableOffset == 0 || pe.CertTableSize == 0 || pe.CertTableOffset >= fs.Length)
                {
                    sb.AppendLine("No WIN_CERTIFICATE / PKCS#7 found.");
                    return sb.ToString();
                }

                byte[] pkcs7 = ReadFirstPkcs7FromWinCertificateTable(fs, pe.CertTableOffset, pe.CertTableSize);
                if (pkcs7 == null || pkcs7.Length == 0)
                {
                    sb.AppendLine("Empty PKCS#7.");
                    return sb.ToString();
                }

                var cms = new SignedCms();
                cms.Decode(pkcs7);

                var pkcsAsm = typeof(SignedCms).Assembly;

                sb.AppendLine(pkcsAsm.GetName().Name + " " + pkcsAsm.GetName().Version);
                sb.AppendLine("=== TOP-LEVEL CMS ===");
                sb.AppendLine("ContentType OID: " + (cms.ContentInfo.ContentType != null ? cms.ContentInfo.ContentType.Value : ""));
                sb.AppendLine("SignerInfos.Count: " + cms.SignerInfos.Count);
                sb.AppendLine("Certificates.Count: " + cms.Certificates.Count);
                sb.AppendLine();

                DumpCmsRecursive(cms, sb, 0, includeNested);
            }

            return sb.ToString();
        }

        // ============ Core dumpers ============

        private static void DumpCmsRecursive(SignedCms cms, StringBuilder sb, int indent, bool includeNested)
        {
            for (int i = 0; i < cms.SignerInfos.Count; i++)
            {
                var si = cms.SignerInfos[i];
                DumpSignerInfo(si, cms, sb, indent);

                // Signed attributes
                DumpAttributeCollection("SignedAttributes", si.SignedAttributes, si, cms, sb, indent + 2, includeNested);

                // Unsigned attributes (where RFC3161, nested signatures, countersignature references usually live)
                DumpAttributeCollection("UnsignedAttributes", si.UnsignedAttributes, si, cms, sb, indent + 2, includeNested);

                // Countersigner(s) (PKCS#9 countersignature) – recurse
                if (si.CounterSignerInfos != null && si.CounterSignerInfos.Count > 0)
                {
                    Indent(sb, indent + 2).AppendLine("CounterSignerInfos.Count = " + si.CounterSignerInfos.Count);
                    for (int k = 0; k < si.CounterSignerInfos.Count; k++)
                    {
                        var csi = si.CounterSignerInfos[k];
                        Indent(sb, indent + 4).AppendLine("[CounterSigner " + k + "]");
                        DumpSignerInfo(csi, cms, sb, indent + 6);
                        DumpAttributeCollection("SignedAttributes", csi.SignedAttributes, csi, cms, sb, indent + 8, includeNested);
                        DumpAttributeCollection("UnsignedAttributes", csi.UnsignedAttributes, csi, cms, sb, indent + 8, includeNested);
                    }
                }
                sb.AppendLine();
            }
        }

        private static void DumpSignerInfo(SignerInfo si, SignedCms cms, StringBuilder sb, int indent)
        {
            var cert = si.Certificate;
            Indent(sb, indent).AppendLine("Signer:");
            if (cert != null)
            {
                Indent(sb, indent + 2).AppendLine("Subject: " + cert.Subject);
                Indent(sb, indent + 2).AppendLine("Issuer : " + cert.Issuer);
                Indent(sb, indent + 2).AppendLine("Thumb  : " + cert.Thumbprint);
                Indent(sb, indent + 2).AppendLine("Serial : " + cert.SerialNumber);
                Indent(sb, indent + 2).AppendLine("NotBefore / NotAfter: " + cert.NotBefore.ToUniversalTime() + " / " + cert.NotAfter.ToUniversalTime());
            }
            else
            {
                Indent(sb, indent + 2).AppendLine("(Signer certificate is null)");
            }
            var dig = si.DigestAlgorithm; // e.g., 2.16.840.1.101.3.4.2.1 for SHA-256
            Indent(sb, indent + 2).AppendLine("DigestAlgorithm: " + (dig != null ? dig.Value : ""));
            // SigningTime (signed attribute, if present)
            var st = TryGetSigningTimeUtc(si);
            if (st.HasValue) Indent(sb, indent + 2).AppendLine("SigningTime (signed attr): " + st.Value.ToString("u"));
        }

        private static void DumpAttributeCollection(
            string label,
            CryptographicAttributeObjectCollection attrs,
            SignerInfo parentSigner,
            SignedCms parentCms,
            StringBuilder sb,
            int indent,
            bool includeNested)
        {
            if (attrs == null || attrs.Count == 0)
            {
                Indent(sb, indent).AppendLine(label + ": (none)");
                return;
            }

            Indent(sb, indent).AppendLine(label + ":");
            for (int i = 0; i < attrs.Count; i++)
            {
                var a = attrs[i];
                string oid = a.Oid != null ? a.Oid.Value : "";
                Indent(sb, indent + 2).AppendLine("- OID: " + oid + "  (" + OidFriendly(oid) + ")");
                if (a.Values == null || a.Values.Count == 0)
                {
                    Indent(sb, indent + 4).AppendLine("(no values)");
                    continue;
                }

                for (int v = 0; v < a.Values.Count; v++)
                {
                    var asn = a.Values[v];
                    byte[] raw = asn.RawData;
                    Indent(sb, indent + 4).AppendLine("Value[" + v + "] len=" + (raw != null ? raw.Length.ToString() : "null"));

                    // Known decodes:
                    if (StringEquals(oid, "1.2.840.113549.1.9.5"))       // signingTime
                    {
                        try { var t = new Pkcs9SigningTime(raw); Indent(sb, indent + 6).AppendLine("signingTime=" + t.SigningTime.ToUniversalTime().ToString("u")); }
                        catch { }
                    }
                    else if (StringEquals(oid, "1.2.840.113549.1.9.4"))  // messageDigest
                    {
                        byte[] val = ExtractOctetString(raw);
                        if (val != null) Indent(sb, indent + 6).AppendLine("messageDigest=" + Hex(val, 200));
                    }
                    else if (StringEquals(oid, "1.2.840.113549.1.9.6"))  // counterSignature
                    {
                        // CounterSignerInfos are exposed on the SignerInfo already; we printed them elsewhere
                        Indent(sb, indent + 6).AppendLine("(PKCS#9 countersignature present; see CounterSignerInfos above)");
                    }
                    else if (StringEquals(oid, "1.2.840.113549.1.9.16.2.14") ||   // RFC3161 id-aa-signatureTimeStampToken
                             StringEquals(oid, "1.3.6.1.4.1.311.3.3.1"))           // Microsoft timestamp OID
                    {
                        // Try to decode a nested SignedCms (timestamp token)
                        SignedCms ts;
                        if (TryDecodeCmsFromAttributeValue(raw, out ts))
                        {
                            Indent(sb, indent + 6).AppendLine("RFC3161/TST token decoded:");
                            Indent(sb, indent + 8).AppendLine("Signers=" + ts.SignerInfos.Count + ", Certs=" + ts.Certificates.Count);

                            // Parse TSTInfo
                            string algOid; byte[] msg; DateTimeOffset? genTime;
                            if (TryParseRfc3161TstInfo(ts.ContentInfo.Content, out algOid, out msg, out genTime))
                            {
                                Indent(sb, indent + 8).AppendLine("TSTInfo.hashAlg=" + algOid);
                                Indent(sb, indent + 8).AppendLine("TSTInfo.hashedMessage=" + (msg != null ? Hex(msg, 200) : ""));
                                Indent(sb, indent + 8).AppendLine("TSTInfo.genTime(UTC)=" + (genTime.HasValue ? genTime.Value.ToString("u") : "(null)"));
                            }

                            // TSA signer (if present)
                            if (ts.SignerInfos.Count > 0 && ts.SignerInfos[0].Certificate != null)
                            {
                                var tsa = ts.SignerInfos[0].Certificate;
                                Indent(sb, indent + 8).AppendLine("TSA: " + tsa.Subject + "  [" + tsa.Thumbprint + "]");
                            }
                        }
                        else
                        {
                            Indent(sb, indent + 6).AppendLine("RFC3161/TST token could not be decoded (raw first bytes): " + Hex(raw, 200));
                            // Fallback: try to pull TSTInfo directly so we can at least show the time.
                            byte[] tstInfo;
                            if (TryExtractTstInfoByScan(raw, out tstInfo))
                            {
                                string alg; byte[] msg; DateTimeOffset? genTime;
                                if (TryParseRfc3161TstInfo(tstInfo, out alg, out msg, out genTime))
                                {
                                    Indent(sb, indent + 6).AppendLine("TSTInfo (scan) parsed:");
                                    Indent(sb, indent + 8).AppendLine("hashAlg=" + alg);
                                    Indent(sb, indent + 8).AppendLine("hashedMessage=" + (msg != null ? Hex(msg, 64) : ""));
                                    Indent(sb, indent + 8).AppendLine("genTime(UTC)=" + (genTime.HasValue ? genTime.Value.ToString("u") : "(null)"));
                                }
                                else
                                {
                                    Indent(sb, indent + 6).AppendLine("TSTInfo (scan) present but could not parse.");
                                }
                            }
                        }
                    }
                    else if (includeNested && StringEquals(oid, "1.3.6.1.4.1.311.2.4.1")) // SpcNestedSignature
                    {
                        SignedCms nested;
                        if (TryDecodeCmsFromAttributeValue(raw, out nested))
                        {
                            Indent(sb, indent + 6).AppendLine("Nested SignedData decoded:");
                            Indent(sb, indent + 8).AppendLine("Signers=" + nested.SignerInfos.Count + ", Certs=" + nested.Certificates.Count);
                            DumpCmsRecursive(nested, sb, indent + 8, includeNested);
                        }
                        else
                        {
                            Indent(sb, indent + 6).AppendLine("Nested SignedData present but not decodable (raw first bytes): " + Hex(raw, 200));
                        }
                    }
                    else
                    {
                        // Unknown – show a small hex preview
                        if (raw != null) Indent(sb, indent + 6).AppendLine("raw(0..199)=" + Hex(raw, 200));
                    }
                }
            }
        }

        // ============ Small helpers used above ============

        private static StringBuilder Indent(StringBuilder sb, int n)
        {
            for (int i = 0; i < n; i++) sb.Append(' ');
            return sb;
        }

        private static string Hex(byte[] b, int max)
        {
            if (b == null) return "(null)";
            int n = b.Length < max ? b.Length : max;
            char[] c = new char[n * 2 + (b.Length > max ? 3 : 0)];
            int p = 0;
            for (int i = 0; i < n; i++)
            {
                byte v = b[i];
                int hi = (v >> 4) & 0xF;
                int lo = v & 0xF;
                c[p++] = (char)(hi < 10 ? '0' + hi : 'a' + (hi - 10));
                c[p++] = (char)(lo < 10 ? '0' + lo : 'a' + (lo - 10));
            }
            if (b.Length > max) { c[p++] = '.'; c[p++] = '.'; c[p++] = '.'; }
            return new string(c, 0, p);
        }

        private static string OidFriendly(string oid)
        {
            if (oid == null) return "";
            if (StringEquals(oid, "1.2.840.113549.1.7.2")) return "signedData";
            if (StringEquals(oid, "1.2.840.113549.1.9.4")) return "messageDigest";
            if (StringEquals(oid, "1.2.840.113549.1.9.5")) return "signingTime";
            if (StringEquals(oid, "1.2.840.113549.1.9.6")) return "counterSignature";
            if (StringEquals(oid, "1.2.840.113549.1.9.16.2.14")) return "id-aa-signatureTimeStampToken (RFC3161)";
            if (StringEquals(oid, "1.3.6.1.4.1.311.3.3.1")) return "MS RFC3161 timestamp";
            if (StringEquals(oid, "1.3.6.1.4.1.311.2.4.1")) return "SpcNestedSignature";
            return "";
        }

        private static byte[] ExtractOctetString(byte[] raw)
        {
            if (raw == null || raw.Length < 2 || raw[0] != 0x04) return null;
            int len, off;
            if (!DerReader.ReadLength(raw, 1, out len, out off)) return null;
            if (off + len > raw.Length) return null;
            byte[] v = new byte[len];
            Buffer.BlockCopy(raw, off, v, 0, len);
            return v;
        }

        // Encoded OID for id-ct-TSTInfo (1.2.840.113549.1.9.16.1.4): 06 0B 2A 86 48 86 F7 0D 01 09 10 01 04
        private static readonly byte[] OID_TSTINFO_ENC = new byte[]
        {
            0x06, 0x0B, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x09, 0x10, 0x01, 0x04
        };

        // Fallback: scan an attribute's RawData for a ContentInfo carrying TSTInfo and extract the
        // inner TSTInfo DER (OCTET STRING). This works even when SignedCms.Decode fails.
        private static bool TryExtractTstInfoByScan(byte[] raw, out byte[] tstInfoDer)
        {
            tstInfoDer = null;
            if (raw == null || raw.Length < OID_TSTINFO_ENC.Length + 8) return false;

            // 1) Find the TSTInfo OID byte pattern anywhere in the attribute value.
            int idx = -1;
            for (int i = 0; i <= raw.Length - OID_TSTINFO_ENC.Length; i++)
            {
                bool match = true;
                for (int k = 0; k < OID_TSTINFO_ENC.Length; k++)
                {
                    if (raw[i + k] != OID_TSTINFO_ENC[k]) { match = false; break; }
                }
                if (match) { idx = i; break; }
            }
            if (idx < 0) return false;

            // After eContentType OID, ContentInfo.encapContentInfo MUST contain eContent as [0] EXPLICIT OCTET STRING.
            // We'll scan forward for tag 0xA0 (context-specific [0]) and then for an OCTET STRING (0x04) inside.
            int pos = idx + OID_TSTINFO_ENC.Length;
            // Bound the scan so we don't walk the whole blob on malformed data.
            int scanEnd = Math.Min(raw.Length, pos + 4096);

            // 2) Find [0] EXPLICIT wrapper (tag 0xA0).
            int a0Pos = -1;
            for (int p = pos; p < scanEnd; p++)
            {
                if (raw[p] == 0xA0) { a0Pos = p; break; }
            }
            if (a0Pos < 0) return false;

            // Read [0] length and compute its content range.
            int a0Len, a0Content;
            if (!DerReader.ReadLength(raw, a0Pos + 1, out a0Len, out a0Content)) return false;
            int a0End = a0Content + a0Len;
            if (a0End > raw.Length) return false;

            // 3) Inside [0], the first element should be the OCTET STRING carrying TSTInfo.
            // Try the first element; if it isn't an OCTET STRING, scan a few bytes inside for tag 0x04.
            int osPos = a0Content;
            if (osPos >= a0End) return false;

            if (raw[osPos] != 0x04)
            {
                // Scan for an OCTET STRING tag within the [0] content
                int found = -1;
                for (int p = a0Content; p < a0End; p++)
                {
                    if (raw[p] == 0x04) { found = p; break; }
                }
                if (found < 0) return false;
                osPos = found;
            }

            // Read OCTET STRING length and slice out the TSTInfo DER.
            int osLen, osContent;
            if (!DerReader.ReadLength(raw, osPos + 1, out osLen, out osContent)) return false;
            if (osContent + osLen > raw.Length) return false;

            tstInfoDer = new byte[osLen];
            Buffer.BlockCopy(raw, osContent, tstInfoDer, 0, osLen);
            return true;
        }

        private static bool TryDecodeCmsFromAttributeValue(byte[] raw, out SignedCms cms)
        {
            cms = null;
            if (raw == null || raw.Length == 0) return false;

            // 1) Direct decode (works for most tokens)
            if (TryDecodeCms(raw, out cms)) return true;

            // 2) OCTET STRING unwrap (sometimes the value is an OCTET STRING containing ContentInfo)
            if (raw[0] == 0x04)
            {
                int osLen, osContent;
                if (DerReader.ReadLength(raw, 1, out osLen, out osContent) &&
                    osContent + osLen <= raw.Length)
                {
                    byte[] inner = new byte[osLen];
                    Buffer.BlockCopy(raw, osContent, inner, 0, osLen);
                    if (TryDecodeCms(inner, out cms)) return true;
                    if (TryWrapSignedDataAsContentInfo(inner, out var wrapped) && TryDecodeCms(wrapped, out cms)) return true;
                }
            }

            // 3) SEQUENCE wrapper case (your dump shows this): copy the ENTIRE SEQUENCE (tag+len+content)
            if (raw[0] == 0x30)
            {
                // Use a DerReader just to compute the correct slice for the outer SEQUENCE
                var r = new DerReader(raw);
                int seqStart, seqTotal;
                if (r.TryReadRawSequenceFull(out seqStart, out seqTotal))
                {
                    var contentInfo = new byte[seqTotal];
                    Buffer.BlockCopy(raw, seqStart, contentInfo, 0, seqTotal);

                    // Try decode this ContentInfo
                    if (TryDecodeCms(contentInfo, out cms)) return true;

                    // If it's actually raw SignedData inside, try wrapping it
                    if (TryWrapSignedDataAsContentInfo(contentInfo, out var wrapped2) && TryDecodeCms(wrapped2, out cms))
                        return true;
                }
            }

            // 4) Last resort: assume raw SignedData; wrap into ContentInfo and decode
            if (TryWrapSignedDataAsContentInfo(raw, out var wrapped3) && TryDecodeCms(wrapped3, out cms)) return true;

            return false;
        }

        // Extract TSTInfo OCTET STRING from a Microsoft RFC3161 attribute value.
        // Returns true and fills tstInfoDer if the structure is: ContentInfo { oid signedData, [0] SignedData }
        // with SignedData.encapContentInfo.eContentType=id-ct-TSTInfo and eContent=[0] EXPLICIT OCTET STRING.
        private static bool TryExtractTstInfoFromMsRfc3161(byte[] raw, out byte[] tstInfoDer)
        {
            tstInfoDer = null;
            if (raw == null || raw.Length < 2 || raw[0] != 0x30) return false;

            int ciLen, ciContent;
            if (!DerReader.ReadLength(raw, 1, out ciLen, out ciContent)) return false;
            int ciEnd = ciContent + ciLen;
            int p = ciContent;

            // Optional leading OID (contentType)
            if (p < ciEnd && raw[p] == 0x06)
            {
                p++;
                int ctLen, ctContent;
                if (!DerReader.ReadLength(raw, p, out ctLen, out ctContent)) return false;
                p = ctContent + ctLen;
            }

            // Expect [0] EXPLICIT SignedData
            if (!(p < ciEnd && raw[p] == 0xA0)) return false;
            p++;
            int a0Len, a0Content;
            if (!DerReader.ReadLength(raw, p, out a0Len, out a0Content)) return false;
            int a0End = a0Content + a0Len;
            int q = a0Content;

            // SignedData ::= SEQUENCE { version INTEGER, digestAlgorithms SET, encapContentInfo SEQUENCE, ... }
            if (!(q < a0End && raw[q] == 0x30)) return false;
            q++;
            int sdLen, sdContent; if (!DerReader.ReadLength(raw, q, out sdLen, out sdContent)) return false;
            int sdEnd = sdContent + sdLen;
            q = sdContent;

            // version INTEGER
            if (!(q < sdEnd && raw[q] == 0x02)) return false;
            q++; int vLen, vContent; if (!DerReader.ReadLength(raw, q, out vLen, out vContent)) return false; q = vContent + vLen;

            // digestAlgorithms SET
            if (!(q < sdEnd && raw[q] == 0x31)) return false;
            q++; int daLen, daContent; if (!DerReader.ReadLength(raw, q, out daLen, out daContent)) return false; q = daContent + daLen;

            // encapContentInfo SEQUENCE
            if (!(q < sdEnd && raw[q] == 0x30)) return false;
            q++; int eciLen, eciContent; if (!DerReader.ReadLength(raw, q, out eciLen, out eciContent)) return false;
            int eciEnd = eciContent + eciLen;
            int r = eciContent;

            // eContentType OID (should be id-ct-TSTInfo: 1.2.840.113549.1.9.16.1.4)
            if (!(r < eciEnd && raw[r] == 0x06)) return false;
            r++; int oidLen, oidContent; if (!DerReader.ReadLength(raw, r, out oidLen, out oidContent)) return false;
            r = oidContent + oidLen;

            // eContent [0] EXPLICIT OCTET STRING
            if (!(r < eciEnd && raw[r] == 0xA0)) return false;
            r++; int ecLen, ecContent; if (!DerReader.ReadLength(raw, r, out ecLen, out ecContent)) return false;

            int s = ecContent;
            if (!(s < eciEnd && raw[s] == 0x04)) return false; // OCTET STRING
            s++; int osLen, osContent; if (!DerReader.ReadLength(raw, s, out osLen, out osContent)) return false;
            if (osContent + osLen > raw.Length) return false;

            tstInfoDer = new byte[osLen];
            Buffer.BlockCopy(raw, osContent, tstInfoDer, 0, osLen);
            return true;
        }

        private static bool TryDecodeCms(byte[] der, out SignedCms cms)
        {
            cms = null;
            try
            {
                var s = new SignedCms();
                s.Decode(der);
                cms = s;
                return true;
            }
            catch { return false; }
        }

        // Build: SEQUENCE { OID signedData (06 09 2A864886F70D010702), [0] EXPLICIT <signedData> }
        private static bool TryWrapSignedDataAsContentInfo(byte[] signedData, out byte[] contentInfo)
        {
            contentInfo = null;
            if (signedData == null || signedData.Length == 0) return false;

            // Quick sanity: a DER SEQUENCE starts with 0x30
            if (signedData[0] != 0x30) return false;

            byte[] oidSignedData = new byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02 }; // 1.2.840.113549.1.7.2

            // [0] EXPLICIT wrapper for the ANY content
            byte[] a0Len = EncodeDerLength(signedData.Length);
            byte[] a0 = new byte[1 + a0Len.Length + signedData.Length];
            a0[0] = 0xA0;
            Buffer.BlockCopy(a0Len, 0, a0, 1, a0Len.Length);
            Buffer.BlockCopy(signedData, 0, a0, 1 + a0Len.Length, signedData.Length);

            // SEQUENCE { oid, [0] content }
            int innerLen = oidSignedData.Length + a0.Length;
            byte[] seqLen = EncodeDerLength(innerLen);
            contentInfo = new byte[1 + seqLen.Length + innerLen];
            int p = 0;
            contentInfo[p++] = 0x30;
            Buffer.BlockCopy(seqLen, 0, contentInfo, p, seqLen.Length); p += seqLen.Length;
            Buffer.BlockCopy(oidSignedData, 0, contentInfo, p, oidSignedData.Length); p += oidSignedData.Length;
            Buffer.BlockCopy(a0, 0, contentInfo, p, a0.Length);

            return true;
        }

        private static byte[] EncodeDerLength(int len)
        {
            if (len < 0x80) return new byte[] { (byte)len };
            // up to 4 bytes length is enough here
            byte b3 = (byte)((len >> 24) & 0xFF);
            byte b2 = (byte)((len >> 16) & 0xFF);
            byte b1 = (byte)((len >> 8) & 0xFF);
            byte b0 = (byte)(len & 0xFF);
            if (b3 != 0) return new byte[] { 0x84, b3, b2, b1, b0 };
            if (b2 != 0) return new byte[] { 0x83, b2, b1, b0 };
            if (b1 != 0) return new byte[] { 0x82, b1, b0 };
            return new byte[] { 0x81, b0 };
        }

        private static TimestampInfo FindBestTimestampAcrossSigners(SignedCms cms, VerificationOptions options)
        {
            HashSet<string> visitedCmsThumbs = new HashSet<string>(StringComparer.Ordinal); // avoid cycles
            return FindBestTimestampRecursive(cms, options, 0, visitedCmsThumbs);
        }

        private static bool IsFullyValidTs(TimestampInfo t, VerificationOptions options)
        {
            if (t == null || !t.Present) return false;
            if (!t.TimeUtc.HasValue) return false;
            if (!t.BoundToSignerSignature) return false;
            if (!t.CryptographicallyValid) return false;
            if (options.ValidateTimestampChain && !t.ChainValid) return false;
            return true;
        }

        // Prefer fully-valid > has time+binding > just present.
        private static TimestampInfo PickBetter(TimestampInfo current, TimestampInfo candidate)
        {
            if (candidate == null || !candidate.Present) return current;

            bool currPresent = current != null && current.Present;
            if (!currPresent) return candidate;

            bool candFull = candidate.CryptographicallyValid && candidate.BoundToSignerSignature && candidate.TimeUtc.HasValue;
            bool currFull = current.CryptographicallyValid && current.BoundToSignerSignature && current.TimeUtc.HasValue;

            if (candFull && !currFull) return candidate;
            if (candFull == currFull)
            {
                // If both same class, prefer the one that at least has Time
                if (candidate.TimeUtc.HasValue && !current.TimeUtc.HasValue) return candidate;
            }
            return current;
        }

        private static TimestampInfo FindBestTimestampRecursive(SignedCms cms, VerificationOptions options, int depth, HashSet<string> visited)
        {
            if (cms == null || depth > 4) return new TimestampInfo(); // safety

            TimestampInfo best = new TimestampInfo();

            // 1) Scan all signers at this level
            for (int s = 0; s < cms.SignerInfos.Count; s++)
            {
                var si = cms.SignerInfos[s];

                TimestampInfo rfc = new TimestampInfo();
                bool foundRfc = TryFindRfc3161OnSigner(si, cms, options, out rfc);

                TimestampInfo cs = new TimestampInfo();
                bool foundCs = TryVerifyCounterSignatureTimestamp(si, cms, options, cs);

                // If we have a fully valid timestamp, return immediately
                if (foundRfc && IsFullyValidTs(rfc, options)) return rfc;
                if (foundCs && IsFullyValidTs(cs, options)) return cs;

                // Otherwise, keep the better partial candidate
                if (foundRfc) best = PickBetter(best, rfc);
                if (foundCs) best = PickBetter(best, cs);
            }

            // 2) Recurse into nested signatures
            for (int s = 0; s < cms.SignerInfos.Count; s++)
            {
                var si = cms.SignerInfos[s];
                var ua = si.UnsignedAttributes;
                if (ua == null) continue;

                for (int i = 0; i < ua.Count; i++)
                {
                    var attr = ua[i];
                    if (attr.Oid == null) continue;
                    if (!StringEquals(attr.Oid.Value, OID_MS_NESTED_SIG)) continue;

                    if (attr.Values == null) continue;

                    for (int v = 0; v < attr.Values.Count; v++)
                    {
                        var val = attr.Values[v].RawData;
                        if (val == null || val.Length == 0) continue;

                        if (TryDecodeCmsFromAttributeValue(val, out var nested))
                        {
                            string mark = null;
                            if (nested.SignerInfos.Count > 0 && nested.SignerInfos[0].Certificate != null)
                                mark = nested.SignerInfos[0].Certificate.Thumbprint;

                            if (mark != null)
                            {
                                if (visited.Contains(mark)) continue;
                                visited.Add(mark);
                            }

                            var nestedTs = FindBestTimestampRecursive(nested, options, depth + 1, visited);

                            if (IsFullyValidTs(nestedTs, options)) return nestedTs;
                            best = PickBetter(best, nestedTs);
                        }
                    }
                }
            }

            // 3) If nothing fully valid was found, return the best partial (so Time shows up)
            return best;
        }

        private static bool TryFindRfc3161OnSigner(SignerInfo signer, SignedCms rootCms, VerificationOptions options, out TimestampInfo info)
        {
            info = new TimestampInfo();
            if (signer == null) return false;

            var ua = signer.UnsignedAttributes;
            if (ua == null || ua.Count == 0) return false;

            for (int i = 0; i < ua.Count; i++)
            {
                var attr = ua[i];
                if (attr.Oid == null) continue;
                string oid = attr.Oid.Value;

                // Standard + Microsoft RFC3161 attribute OIDs
                if (!StringEquals(oid, "1.2.840.113549.1.9.16.2.14") && !StringEquals(oid, "1.3.6.1.4.1.311.3.3.1"))
                    continue;

                if (attr.Values == null || attr.Values.Count == 0) continue;
                byte[] raw = attr.Values[0].RawData;

                // --- Path A: full CMS decode for cryptographic/chain validation ---
                SignedCms tsCms;
                if (TryDecodeCmsFromAttributeValue(raw, out tsCms))
                {
                    info = new TimestampInfo { Present = true, IsRfc3161 = true };

                    try { tsCms.CheckSignature(true); info.CryptographicallyValid = true; }
                    catch { info.CryptographicallyValid = false; }

                    string algOid; byte[] msg; DateTimeOffset? genTime;
                    if (TryParseRfc3161TstInfo(tsCms.ContentInfo.Content, out algOid, out msg, out genTime))
                    {
                        info.TimeUtc = genTime;
                        try
                        {
                            byte[] parentSig = GetSignerSignatureBytes(signer, rootCms);
                            using (var h = CreateHashFromOid(algOid))
                            {
                                if (h != null)
                                {
                                    var dig = h.ComputeHash(parentSig);
                                    info.BoundToSignerSignature = (msg != null && BytesEqual(dig, msg));
                                }
                            }
                        }
                        catch { /* ignore */ }
                    }

                    // Optional TSA chain
                    if (options.ValidateTimestampChain && tsCms.SignerInfos.Count > 0 && tsCms.SignerInfos[0].Certificate != null)
                    {
                        var tsa = tsCms.SignerInfos[0].Certificate;
                        var extra = new X509Certificate2Collection();
                        for (int c = 0; c < tsCms.Certificates.Count; c++)
                            if (!CertEqualThumbprint(tsCms.Certificates[c], tsa))
                                extra.Add(tsCms.Certificates[c]);

                        var customRoots = LoadCertificatesFromDirectory(options.CustomRootDirectory);
                        X509Certificate2[] extraFromDir = LoadCertificatesFromDirectory(options.AdditionalIntermediatesDirectory);
                        for (int e = 0; extraFromDir != null && e < extraFromDir.Length; e++) extra.Add(extraFromDir[e]);

                        string[] st;
                        bool ok = BuildChainWithAutoAia(tsa, extra, info.TimeUtc, options, customRoots,
                                                        /*requireTimeStampingEku*/ options.RequireTsaEku, out st);
                        info.ChainValid = ok;
                        info.ChainStatus = st;
                        info.TsaSubject = tsa.Subject;
                        info.TsaThumbprint = tsa.Thumbprint;
                    }
                    else
                    {
                        info.ChainValid = !options.ValidateTimestampChain;
                    }

                    return true; // found a token (decode path)
                }

                // --- Path B: decoder failed -> scan/extract TSTInfo so we still get Time + Binding ---
                byte[] tstInfo;
                if (TryExtractTstInfoByScan(raw, out tstInfo))
                {
                    info = new TimestampInfo { Present = true, IsRfc3161 = true, CryptographicallyValid = false };

                    string algOid; byte[] msg; DateTimeOffset? genTime;
                    if (TryParseRfc3161TstInfo(tstInfo, out algOid, out msg, out genTime))
                    {
                        info.TimeUtc = genTime;
                        try
                        {
                            byte[] parentSig = GetSignerSignatureBytes(signer, rootCms);
                            using (var h = CreateHashFromOid(algOid))
                            {
                                if (h != null)
                                {
                                    var dig = h.ComputeHash(parentSig);
                                    info.BoundToSignerSignature = (msg != null && BytesEqual(dig, msg));
                                }
                            }
                        }
                        catch { /* ignore */ }
                    }

                    // No TSA chain possible without a decoded CMS – leave ChainValid based on policy
                    info.ChainValid = !options.ValidateTimestampChain;
                    return true; // found via scan
                }

                // continue loop in case there are multiple timestamp attributes (rare)
            }

            return false;
        }

        // ---------- Chain build with optional custom trust & revocation ----------

        private static bool BuildChainWithOptions(
            X509Certificate2 leaf,
            X509Certificate2Collection extra,
            DateTimeOffset? verificationTime,
            VerificationOptions options,
            X509Certificate2[] customRoots,
            bool requireTimeStampingEku,
            out string[] statusStrings)
        {
            var chain = new X509Chain();
            // Policy
            chain.ChainPolicy.RevocationMode = options.RevocationMode;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            chain.ChainPolicy.UrlRetrievalTimeout = options.RevocationUrlRetrievalTimeout;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            if (verificationTime.HasValue) chain.ChainPolicy.VerificationTime = verificationTime.Value.UtcDateTime;

            // EKU constraints: for code signing or TSA depending on use-case
            if (requireTimeStampingEku)
            {
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.8")); // Time Stamping
            }
            else
            {
                chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.3")); // Code Signing
            }

            if (extra != null && extra.Count > 0)
                chain.ChainPolicy.ExtraStore.AddRange(extra);

            // Try to enable Custom Root Trust on capable runtimes (.NET 5+)
            bool customApplied = false;
            if (options.UseCustomRootTrust && customRoots != null && customRoots.Length > 0)
            {
                try
                {
                    // Reflection to avoid compile-time dependency (still compiles on netstandard2.0).
                    var policy = chain.ChainPolicy;
                    var trustModeProp = policy.GetType().GetProperty("TrustMode");
                    var customTrustStoreProp = policy.GetType().GetProperty("CustomTrustStore");
                    if (trustModeProp != null && customTrustStoreProp != null)
                    {
                        var x509ChainTrustModeType = trustModeProp.PropertyType; // enum
                        object customRootTrustEnum = Enum.Parse(x509ChainTrustModeType, "CustomRootTrust");
                        trustModeProp.SetValue(policy, customRootTrustEnum, null);

                        var customStore = (X509Certificate2Collection)customTrustStoreProp.GetValue(policy, null);
                        for (int i = 0; i < customRoots.Length; i++) customStore.Add(customRoots[i]);

                        customApplied = true;
                    }
                }
                catch { customApplied = false; }
            }

            // Build
            bool ok = chain.Build(leaf);

            // Convert statuses
            List<string> st = new List<string>();
            for (int i = 0; i < chain.ChainStatus.Length; i++)
            {
                var s = chain.ChainStatus[i];
                string line = s.Status.ToString();
                if (!string.IsNullOrEmpty(s.StatusInformation))
                    line += ": " + s.StatusInformation.Trim();
                st.Add(line);
            }
            statusStrings = st.ToArray();

            // Compatibility fallback for older runtimes if only UntrustedRoot & top matches our custom root.
            if (!ok && options.UseCustomRootTrust && !customApplied && options.TrustIfOnlyUntrustedRootAndMatchesCustomRoot)
            {
                bool onlyUntrustedRoot = true;
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    var s = chain.ChainStatus[i].Status;
                    if (s != X509ChainStatusFlags.UntrustedRoot &&
                        s != X509ChainStatusFlags.NoError)
                    {
                        onlyUntrustedRoot = false;
                        break;
                    }
                }

                if (onlyUntrustedRoot && chain.ChainElements.Count > 0)
                {
                    X509Certificate2 root = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                    if (IsInCollectionByThumbprint(customRoots, root))
                    {
                        ok = true; // treat as trusted via provided roots
                    }
                }
            }

            return ok;
        }

        private static bool BuildChainWithAutoAia(
            X509Certificate2 leaf,
            X509Certificate2Collection extra,
            DateTimeOffset? verificationTime,
            VerificationOptions options,
            X509Certificate2[] customRoots,
            bool requireTimeStampingEku,
            out string[] statusStrings)
        {
            // 1) Try once with what we already have.
            bool ok = BuildChainWithOptions(leaf, extra, verificationTime, options, customRoots, requireTimeStampingEku, out statusStrings);
            if (ok || !options.AutoDownloadIntermediates) return ok;

            // 2) AIA-chase loop: download intermediates and retry up to AiaMaxDepth times.
            HashSet<string> visitedThumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visitedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Seed visited with what we already have to avoid loops
            visitedThumbprints.Add(leaf.Thumbprint);
            for (int i = 0; i < extra.Count; i++) visitedThumbprints.Add(extra[i].Thumbprint);

            int depth = 0;
            while (depth < options.AiaMaxDepth)
            {
                depth++;

                // Find certs currently in the partial chain that have AIA "caIssuers" URIs we have not tried yet.
                // We rebuild to get the latest ChainElements for inspection.
                string[] ignore;
                BuildChainWithOptions(leaf, extra, verificationTime, options, customRoots, requireTimeStampingEku, out ignore);

                // Gather candidates from leaf + all extras (simple, robust)
                List<string> urls = new List<string>();
                GatherAiaUrls(leaf, urls, visitedUrls);
                for (int i = 0; i < extra.Count; i++)
                    GatherAiaUrls(extra[i], urls, visitedUrls);

                if (urls.Count == 0) break; // nothing left to try

                // Try to download any new certs and add to extra
                int added = 0;
                for (int u = 0; u < urls.Count; u++)
                {
                    string uri = urls[u];
                    X509Certificate2[] fromUrl = DownloadCertificatesFromAia(uri, options);
                    if (fromUrl == null) continue;

                    for (int c = 0; c < fromUrl.Length; c++)
                    {
                        var cert = fromUrl[c];
                        if (cert == null) continue;
                        if (visitedThumbprints.Contains(cert.Thumbprint)) continue;

                        // Avoid adding the leaf itself again
                        if (!StringEquals(cert.Thumbprint, leaf.Thumbprint))
                        {
                            extra.Add(cert);
                            visitedThumbprints.Add(cert.Thumbprint);
                            added++;
                        }
                    }
                }

                if (added == 0) break; // no progress

                // Retry build with the newly added intermediates
                ok = BuildChainWithOptions(leaf, extra, verificationTime, options, customRoots, requireTimeStampingEku, out statusStrings);
                if (ok) return true;
            }

            // Final attempt result is already in statusStrings from last build
            return false;
        }

        // ---------- PE parsing + Authenticode hashing ----------

        [ObjectId("f776625a-56e5-4784-a404-7355ce61dc83")]
        private sealed class PeLayout
        {
            public long OptionalHeaderOffset;
            public bool Pe32Plus;
            public long ChecksumFieldOffset;
            public long SecurityDirEntryOffset;
            public uint SizeOfHeaders;
            public ushort NumberOfSections;
            public ushort SizeOfOptionalHeader;
            public long SectionTableOffset;     // file offset to first IMAGE_SECTION_HEADER
            public long CertTableOffset;
            public long CertTableSize;
        }

        private static PeLayout ReadPeLayout(FileStream fs)
        {
            using (var br = new BinaryReader(fs, Encoding.UTF8, true))
            {
                fs.Position = 0x3C;
                uint peHeaderOffset = br.ReadUInt32();
                if (peHeaderOffset == 0 || peHeaderOffset > fs.Length - 256)
                    throw new InvalidDataException("Not a valid PE file (bad e_lfanew).");

                fs.Position = peHeaderOffset;
                uint sig = br.ReadUInt32();
                if (sig != 0x00004550) // "PE\0\0"
                    throw new InvalidDataException("Missing PE signature.");

                long coffOffset = peHeaderOffset + 4;

                // COFF File Header (20 bytes)
                fs.Position = coffOffset + 2;                  // NumberOfSections offset
                ushort numSections = br.ReadUInt16();

                fs.Position = coffOffset + 16;                 // SizeOfOptionalHeader
                ushort sizeOfOptional = br.ReadUInt16();

                long optionalHeaderOffset = coffOffset + 20;

                // Optional Header
                fs.Position = optionalHeaderOffset;
                ushort magic = br.ReadUInt16();                // 0x10B (PE32) or 0x20B (PE32+)
                bool pe32Plus = (magic == 0x20B);

                // CheckSum field is at +0x40 from start of optional header on both PE32 and PE32+ (per PE/COFF).
                long checksumOffset = optionalHeaderOffset + 0x40;

                // DataDirectory start at +0x60 (PE32) or +0x70 (PE32+)
                long dataDirStart = optionalHeaderOffset + (pe32Plus ? 0x70 : 0x60);

                // Security Directory entry (index 4): 8 bytes (VA/Offset + Size)
                long securityDirEntryOffset = dataDirStart + (8 * 4);

                // Read SizeOfHeaders (offset 0x3C from optional header start)
                fs.Position = optionalHeaderOffset + 0x3C;
                uint sizeOfHeaders = br.ReadUInt32();

                // Read Cert Table (file offset, size) from security directory entry
                fs.Position = securityDirEntryOffset;
                uint certFileOffset = br.ReadUInt32(); // For Security dir, this is a FILE OFFSET (not RVA)
                uint certSize = br.ReadUInt32();

                // Section table starts right after the optional header
                long sectionTableOffset = optionalHeaderOffset + sizeOfOptional;

                return new PeLayout
                {
                    OptionalHeaderOffset = optionalHeaderOffset,
                    Pe32Plus = pe32Plus,
                    ChecksumFieldOffset = checksumOffset,
                    SecurityDirEntryOffset = securityDirEntryOffset,
                    SizeOfHeaders = sizeOfHeaders,
                    NumberOfSections = numSections,
                    SizeOfOptionalHeader = sizeOfOptional,
                    SectionTableOffset = sectionTableOffset,
                    CertTableOffset = certFileOffset,
                    CertTableSize = certSize
                };
            }
        }

        private static byte[] ReadFirstPkcs7FromWinCertificateTable(FileStream fs, long certTableOffset, long certTableSize)
        {
            fs.Position = certTableOffset;
            long limit = Math.Min(certTableOffset + certTableSize, fs.Length);

            using (var br = new BinaryReader(fs, Encoding.UTF8, true))
            {
                while (fs.Position + 8 <= limit)
                {
                    uint dwLength = br.ReadUInt32();
                    ushort wRevision = br.ReadUInt16();
                    ushort wType = br.ReadUInt16();

                    if (dwLength < 8 || fs.Position + (dwLength - 8) > limit)
                        break;

                    byte[] body = br.ReadBytes((int)(dwLength - 8));

                    if (wType == 0x0002 /*WIN_CERT_TYPE_PKCS_SIGNED_DATA*/ && body != null && body.Length > 0)
                        return body;

                    // Each entry is aligned to 8 bytes
                    long padded = ((dwLength + 7U) & ~7U);
                    long nextPos = (fs.Position - (dwLength - 8)) + padded;
                    fs.Position = Math.Min(nextPos, limit);
                }
            }
            return null;
        }

        [ObjectId("8ff97be4-d03f-4a00-a15b-24cfc10c8c55")]
        private struct SectionSpan
        {
            public uint PtrToRaw;   // PointerToRawData
            public uint SizeRaw;    // SizeOfRawData
        }

        private static byte[] ComputeAuthenticodeHash(FileStream fs, HashAlgorithm hash, PeLayout pe, VerificationOptions options)
        {
            long fileLen = fs.Length;

            // ---- 1) Headers (0 .. SizeOfHeaders), skipping Checksum and Cert Table entry
            long headersEnd = Math.Min((long)pe.SizeOfHeaders, fileLen);

            // [0, Checksum)
            HashRange(fs, hash, 0, pe.ChecksumFieldOffset);

            // skip Checksum (4 bytes)
            long afterChecksum = pe.ChecksumFieldOffset + 4;

            // (Checksum+4 .. SecurityDirEntry)
            HashRange(fs, hash, afterChecksum, pe.SecurityDirEntryOffset);

            // skip Security Directory entry (8 bytes)
            long afterSecDir = pe.SecurityDirEntryOffset + 8;

            // (afterSecDir .. headersEnd)
            if (afterSecDir < headersEnd)
                HashRange(fs, hash, afterSecDir, headersEnd);

            // We'll track a "cursor" so we never double-hash if a section overlaps the header.
            long cursor = headersEnd;

            // ---- 2) Sections: sort by PointerToRawData; hash SizeOfRawData bytes for each
            SectionSpan[] secs = ReadSections(fs, pe);
            SortSectionsByPtr(secs);

            for (int i = 0; i < secs.Length; i++)
            {
                uint ptr = secs[i].PtrToRaw;
                uint len = secs[i].SizeRaw;
                if (len == 0) continue;

                long start = (long)ptr;
                long end   = (long)ptr + (long)len;

                // Clamp to file
                if (start > fileLen) continue;
                if (end   > fileLen) end = fileLen;

                // Avoid double-hashing if section begins inside already-hashed header area
                if (start < cursor) start = cursor;

                if (end > start)
                {
                    HashRange(fs, hash, start, end);
                    if (end > cursor) cursor = end;
                }
            }

            // ---- Step 14: Hash any remaining bytes before the certificate table (overlay/padding)
            //      TODO: Make this optional?
#if true
            if (options.HashAnyRemainingBytes)
            {
                long certSize = pe.CertTableSize;
                if (certSize < 0) certSize = 0;
                long remaining2 = fileLen - (certSize + cursor);
                if (remaining2 > 0)
                {
                    long overlayStart = cursor;
                    long overlayEnd   = overlayStart + remaining2;
                    if (overlayStart < fileLen && overlayEnd <= fileLen)
                        HashRange(fs, hash, overlayStart, overlayEnd);
                }
            }
#endif

            // ---- 3) DO NOT hash any bytes past the last section. (Overlay is excluded.)
            // Per Microsoft Learn "Process for Generating the Authenticode PE Image Hash":
            // "Information past the end of the last section ... is not hashed."

            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hash.Hash;
        }

        private static SectionSpan[] ReadSections(FileStream fs, PeLayout pe)
        {
            // Each IMAGE_SECTION_HEADER is 40 bytes.
            const int SectSize = 40;

            int count = pe.NumberOfSections;
            if (count <= 0) return Array.Empty<SectionSpan>();

            var list = new List<SectionSpan>(count);
            using (var br = new BinaryReader(fs, Encoding.UTF8, true))
            {
                long pos = pe.SectionTableOffset;
                for (int i = 0; i < count; i++)
                {
                    long off = pos + i * SectSize;
                    if (off + SectSize > fs.Length) break;
                    fs.Position = off;

                    fs.Position = off + 0x14; // PointerToRawData offset within section header
                    uint ptrToRaw = br.ReadUInt32();

                    fs.Position = off + 0x10; // SizeOfRawData offset
                    uint sizeRaw = br.ReadUInt32();

                    if (sizeRaw != 0)
                    {
                        SectionSpan s;
                        s.PtrToRaw = ptrToRaw;
                        s.SizeRaw = sizeRaw;
                        list.Add(s);
                    }
                }
            }
            return list.ToArray();
        }

        private static void SortSectionsByPtr(SectionSpan[] secs)
        {
            // Simple insertion sort; avoids LINQ.
            for (int i = 1; i < secs.Length; i++)
            {
                SectionSpan key = secs[i];
                int j = i - 1;
                while (j >= 0 && secs[j].PtrToRaw > key.PtrToRaw)
                {
                    secs[j + 1] = secs[j];
                    j--;
                }
                secs[j + 1] = key;
            }
        }

        private static void HashRange(FileStream fs, HashAlgorithm hash, long start, long end)
        {
            if (start >= end) return;
            fs.Position = start;
            long remaining = end - start;
            byte[] buffer = new byte[64 * 1024];
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = fs.Read(buffer, 0, toRead);
                if (read <= 0) throw new EndOfStreamException();
                hash.TransformBlock(buffer, 0, read, null, 0);
                remaining -= read;
            }
        }

        // ---------- Minimal DER helpers (no external ASN.1 lib) ----------

        // Parse SpcIndirectDataContent -> DigestInfo (AlgorithmIdentifier OID + digest OCTET STRING)
        private static bool TryParseSpcIndirectDataDigest(byte[] eContent, out string digestOid, out byte[] digest)
        {
            digestOid = null;
            digest = null;

            try
            {
                var r = new DerReader(eContent);
                DerReader seq = r.ReadSequence();       // SpcIndirectDataContent
                seq.SkipValue();                         // data (we don't need it)
                DerReader digestInfo = seq.ReadSequence(); // DigestInfo

                DerReader alg = digestInfo.ReadSequence();  // AlgorithmIdentifier
                digestOid = alg.ReadOid();                  // algorithm OID
                if (alg.HasData) alg.SkipToEnd();           // skip params if present

                digest = digestInfo.ReadOctetString();      // digest OCTET STRING
                return (digestOid != null && digest != null);
            }
            catch
            {
                return false;
            }
        }

        // Parse RFC3161 TSTInfo: messageImprint.hashAlgorithm OID, messageImprint.hashedMessage, genTime (GeneralizedTime)
        private static bool TryParseRfc3161TstInfo(byte[] tstInfoDer, out string hashAlgOid, out byte[] hashedMessage, out DateTimeOffset? genTime)
        {
            hashAlgOid = null;
            hashedMessage = null;
            genTime = null;

            try
            {
                var r = new DerReader(tstInfoDer);
                var seq = r.ReadSequence();     // TSTInfo
                seq.SkipValueExpectedTag(0x02); // version INTEGER
                seq.SkipValueExpectedTag(0x06); // policy OID
                var mi = seq.ReadSequence();    // messageImprint
                var alg = mi.ReadSequence();    // AlgorithmIdentifier
                hashAlgOid = alg.ReadOid();
                if (alg.HasData) alg.SkipToEnd(); // skip params
                hashedMessage = mi.ReadOctetString(); // hashedMessage
                seq.SkipValueExpectedTag(0x02); // serialNumber INTEGER

                // genTime (GeneralizedTime, tag 0x18)
                genTime = seq.ReadGeneralizedTime();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // --- NEW: Unified way to obtain the raw signature bytes for a SignerInfo ---
        private static byte[] GetSignerSignatureBytes(SignerInfo signer, SignedCms containerCms)
        {
            // Path A: use the API if it exists (newer runtimes)
            try
            {
                var mi = typeof(SignerInfo).GetMethod("GetSignature",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (mi != null)
                {
                    object roMem = mi.Invoke(signer, null);
                    if (roMem != null)
                    {
                        // On modern runtimes returns ReadOnlyMemory<byte>
                        var t = roMem.GetType();
                        if (t.FullName != null && t.FullName.StartsWith("System.ReadOnlyMemory"))
                        {
                            var toArray = t.GetMethod("ToArray", System.Type.EmptyTypes);
                            if (toArray != null)
                            {
                                object arr = toArray.Invoke(roMem, null);
                                return arr as byte[];
                            }
                        }
                        // Some builds may return byte[] directly
                        var asBytes = roMem as byte[];
                        if (asBytes != null) return asBytes;
                    }
                }
            }
            catch { /* ignore and fall back */ }

            // Path B: parse the CMS DER to find the matching SignerInfo.signature
            byte[] sig;
            if (TryGetSignerSignatureFromCms(containerCms, signer, out sig))
                return sig;

            return null;
        }

        private static bool TryGetSignerSignatureFromCms(SignedCms cms, SignerInfo target, out byte[] signature)
        {
            signature = null;
            try
            {
                return TryExtractSignerSignatureFromCmsDer(cms.Encode(), target, out signature);
            }
            catch
            {
                return false;
            }
        }

        // Extract SignerInfo.signature (OCTET STRING) from SignedData.signerInfos matching the 'target' signer.
        // Match by Issuer+SerialNumber if available, otherwise by SubjectKeyIdentifier (SKI).
        private static bool TryExtractSignerSignatureFromCmsDer(byte[] der, SignerInfo target, out byte[] signature)
        {
            signature = null;
            if (der == null || der.Length < 2) return false;

            int len, content;
            // ContentInfo ::= SEQUENCE { contentType OID, [0] EXPLICIT ANY }
            if (der[0] != 0x30 || !DerReader.ReadLength(der, 1, out len, out content)) return false;
            int ciEnd = content + len;
            int p = content;

            // contentType OID
            if (p >= ciEnd || der[p] != 0x06) return false;
            int oidLen, oidContent;
            if (!DerReader.ReadLength(der, p + 1, out oidLen, out oidContent)) return false;
            // skip OID
            p = oidContent + oidLen;

            // [0] EXPLICIT signedData
            if (p >= ciEnd || der[p] != 0xA0) return false;
            int a0Len, a0Content;
            if (!DerReader.ReadLength(der, p + 1, out a0Len, out a0Content)) return false;
            int sdPos = a0Content; int sdEnd = a0Content + a0Len;

            // SignedData ::= SEQUENCE { version, digestAlgorithms, encapContentInfo, [0] certs OPTIONAL, [1] crls OPTIONAL, signerInfos SET }
            if (sdPos >= sdEnd || der[sdPos] != 0x30) return false;
            int sdLen, sdContent;
            if (!DerReader.ReadLength(der, sdPos + 1, out sdLen, out sdContent)) return false;
            int q = sdContent, qEnd = sdContent + sdLen;

            // version INTEGER
            if (q >= qEnd || der[q] != 0x02) return false;
            int vLen, vContent; if (!DerReader.ReadLength(der, q + 1, out vLen, out vContent)) return false;
            q = vContent + vLen;

            // digestAlgorithms SET
            if (q >= qEnd || der[q] != 0x31) return false;
            int daLen, daContent; if (!DerReader.ReadLength(der, q + 1, out daLen, out daContent)) return false;
            q = daContent + daLen;

            // encapContentInfo SEQUENCE
            if (q >= qEnd || der[q] != 0x30) return false;
            int eciLen, eciContent; if (!DerReader.ReadLength(der, q + 1, out eciLen, out eciContent)) return false;
            q = eciContent + eciLen;

            // [0] certificates OPTIONAL
            if (q < qEnd && der[q] == 0xA0)
            {
                int cLen, cContent; if (!DerReader.ReadLength(der, q + 1, out cLen, out cContent)) return false;
                q = cContent + cLen;
            }
            // [1] crls OPTIONAL
            if (q < qEnd && der[q] == 0xA1)
            {
                int rLen, rContent; if (!DerReader.ReadLength(der, q + 1, out rLen, out rContent)) return false;
                q = rContent + rLen;
            }

            // signerInfos SET
            if (q >= qEnd || der[q] != 0x31) return false;
            int siSetLen, siSetContent; if (!DerReader.ReadLength(der, q + 1, out siSetLen, out siSetContent)) return false;
            int siPos = siSetContent, siEnd = siSetContent + siSetLen;

            // Prepare target identifiers
            byte[] targetIssuer = null, targetSerialBE = null, targetSki = null;
            X509Certificate2 cert = target != null ? target.Certificate : null;
            if (cert != null)
            {
                // Issuer DER from the certificate TBSCertificate
                try { targetIssuer = cert.IssuerName.RawData; } catch { targetIssuer = null; }
                targetSerialBE = GetCertSerialBigEndian(cert);
                targetSki = GetCertSubjectKeyIdentifier(cert);
            }

            // Iterate each SignerInfo (SEQUENCE) in the SET
            while (siPos < siEnd)
            {
                if (der[siPos] != 0x30) break;
                int oneLen, oneContent; if (!DerReader.ReadLength(der, siPos + 1, out oneLen, out oneContent)) break;
                int t = oneContent, tEnd = oneContent + oneLen;

                // SignerInfo ::= SEQUENCE { version, sid, digestAlgorithm, [0] signedAttrs OPTIONAL, signatureAlgorithm, signature OCTET STRING, ... }
                // version
                if (t >= tEnd || der[t] != 0x02) { siPos = tEnd; continue; }
                int svLen, svContent; if (!DerReader.ReadLength(der, t + 1, out svLen, out svContent)) { siPos = tEnd; continue; }
                t = svContent + svLen;

                // sid (IssuerAndSerialNumber OR [0] SubjectKeyIdentifier)
                bool sidMatches = false;
                if (t < tEnd && der[t] == 0x30) // IssuerAndSerialNumber
                {
                    // Capture issuer DER
                    int issLen, issContent; if (!DerReader.ReadLength(der, t + 1, out issLen, out issContent)) { siPos = tEnd; continue; }
                    int issuerStart = t; int issuerTotalLen = 1 + (issContent - (t + 1)) + issLen;
                    byte[] issuerDer = new byte[issuerTotalLen];
                    Buffer.BlockCopy(der, issuerStart, issuerDer, 0, issuerTotalLen);
                    t = issContent + issLen;

                    // serial INTEGER
                    if (t >= tEnd || der[t] != 0x02) { siPos = tEnd; continue; }
                    int sLen, sContent; if (!DerReader.ReadLength(der, t + 1, out sLen, out sContent)) { siPos = tEnd; continue; }
                    byte[] serialDer = new byte[sLen];
                    Buffer.BlockCopy(der, sContent, serialDer, 0, sLen);
                    t = sContent + sLen;

                    if (targetIssuer != null && targetSerialBE != null)
                    {
                        // Normalize serial: remove leading 0x00 from INTEGER content
                        byte[] serialNorm = TrimLeftZeros(serialDer);
                        byte[] targetSerialNorm = TrimLeftZeros(targetSerialBE);
                        if (BytesEqual(issuerDer, targetIssuer) && BytesEqual(serialNorm, targetSerialNorm))
                            sidMatches = true;
                    }
                }
                else if (t < tEnd && der[t] == 0x80) // [0] IMPLICIT SubjectKeyIdentifier
                {
                    int skiLen, skiContent; if (!DerReader.ReadLength(der, t + 1, out skiLen, out skiContent)) { siPos = tEnd; continue; }
                    byte[] skiVal = new byte[skiLen];
                    Buffer.BlockCopy(der, skiContent, skiVal, 0, skiLen);
                    t = skiContent + skiLen;

                    if (targetSki != null && BytesEqual(targetSki, skiVal)) sidMatches = true;
                }
                else
                {
                    // Unknown sid; move on
                    siPos = tEnd; continue;
                }

                // digestAlgorithm (skip)
                if (t >= tEnd || der[t] != 0x30) { siPos = tEnd; continue; }
                int dAlgLen, dAlgContent; if (!DerReader.ReadLength(der, t + 1, out dAlgLen, out dAlgContent)) { siPos = tEnd; continue; }
                t = dAlgContent + dAlgLen;

                // [0] signedAttrs OPTIONAL (skip if present)
                if (t < tEnd && der[t] == 0xA0)
                {
                    int saLen, saContent; if (!DerReader.ReadLength(der, t + 1, out saLen, out saContent)) { siPos = tEnd; continue; }
                    t = saContent + saLen;
                }

                // signatureAlgorithm (skip)
                if (t >= tEnd || der[t] != 0x30) { siPos = tEnd; continue; }
                int sAlgLen, sAlgContent; if (!DerReader.ReadLength(der, t + 1, out sAlgLen, out sAlgContent)) { siPos = tEnd; continue; }
                t = sAlgContent + sAlgLen;

                // signature OCTET STRING -> THIS IS WHAT WE NEED
                if (t < tEnd && der[t] == 0x04)
                {
                    int sigLen, sigContent; if (!DerReader.ReadLength(der, t + 1, out sigLen, out sigContent)) { siPos = tEnd; continue; }
                    if (sidMatches)
                    {
                        signature = new byte[sigLen];
                        Buffer.BlockCopy(der, sigContent, signature, 0, sigLen);
                        return true;
                    }
                    // Move past signature to continue scanning (unsignedAttrs may follow)
                    t = sigContent + sigLen;
                }

                siPos = tEnd;
            }

            return false;
        }

        // Helpers for matching
        private static byte[] GetCertSerialBigEndian(X509Certificate2 cert)
        {
            try
            {
                // GetSerialNumber() returns little-endian; reverse to big-endian
                byte[] le = cert.GetSerialNumber(); // available on netstandard2.0
                if (le == null) return null;
                Array.Reverse(le);
                return le;
            }
            catch { return null; }
        }

        private static byte[] TrimLeftZeros(byte[] v)
        {
            if (v == null || v.Length == 0) return v;
            int i = 0; while (i < v.Length - 1 && v[i] == 0x00) i++;
            if (i == 0) return v;
            byte[] t = new byte[v.Length - i];
            Buffer.BlockCopy(v, i, t, 0, t.Length);
            return t;
        }

        private static byte[] GetCertSubjectKeyIdentifier(X509Certificate2 cert)
        {
            try
            {
                for (int i = 0; i < cert.Extensions.Count; i++)
                {
                    var ext = cert.Extensions[i];
                    if (ext != null && ext.Oid != null && StringEquals(ext.Oid.Value, "2.5.29.14"))
                    {
                        // The extension value is an OCTET STRING wrapping the SKI OCTET STRING
                        // Use the helper you already have:
                        byte[] ski = ExtractOctetString(ext.RawData); // returns inner OCTET STRING
                        return ski;
                    }
                }
            }
            catch { }
            return null;
        }

        private static DateTimeOffset? TryGetSigningTimeUtc(SignerInfo si)
        {
            if (si == null) return null;
            CryptographicAttributeObjectCollection sa = si.SignedAttributes;
            if (sa == null) return null;

            for (int i = 0; i < sa.Count; i++)
            {
                var attr = sa[i];
                if (attr.Oid != null && StringEquals(attr.Oid.Value, "1.2.840.113549.1.9.5")) // signingTime
                {
                    if (attr.Values != null && attr.Values.Count > 0)
                    {
                        try
                        {
                            var st = new Pkcs9SigningTime(attr.Values[0].RawData);
                            return st.SigningTime.ToUniversalTime();
                        }
                        catch { }
                    }
                }
            }
            return null;
        }

        // Extract OCTET STRING value from a single-valued attribute (RawData encodes the value, i.e., includes '04 len ...')
        private static byte[] GetSingleAttributeOctetValue(CryptographicAttributeObjectCollection attrs, string oid)
        {
            if (attrs == null) return null;
            for (int i = 0; i < attrs.Count; i++)
            {
                var a = attrs[i];
                if (a.Oid != null && StringEquals(a.Oid.Value, oid))
                {
                    if (a.Values != null && a.Values.Count > 0)
                    {
                        byte[] raw = a.Values[0].RawData;
                        // Expect OCTET STRING
                        if (raw != null && raw.Length >= 2 && raw[0] == 0x04)
                        {
                            int len;
                            int offset;
                            if (!DerReader.ReadLength(raw, 1, out len, out offset)) return null;
                            if (offset + len <= raw.Length)
                            {
                                byte[] val = new byte[len];
                                Buffer.BlockCopy(raw, offset, val, 0, len);
                                return val;
                            }
                        }
                    }
                }
            }
            return null;
        }

        // ---------- Utilities ----------

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            bool same = true;
            for (int i = 0; i < a.Length; i++) same &= (a[i] == b[i]);
            return same;
        }

        private static bool StringEquals(string a, string b) =>
            string.Equals(a, b, StringComparison.Ordinal);

        private static bool CertEqualThumbprint(X509Certificate2 a, X509Certificate2 b)
        {
            if (a == null || b == null) return false;
            return StringEquals(a.Thumbprint, b.Thumbprint);
        }

        private static bool IsInCollectionByThumbprint(X509Certificate2[] set, X509Certificate2 cert)
        {
            if (set == null || cert == null) return false;
            for (int i = 0; i < set.Length; i++)
                if (StringEquals(set[i].Thumbprint, cert.Thumbprint)) return true;
            return false;
        }

        private static HashAlgorithm CreateHashFromOid(string oid)
        {
            if (oid == null) return null;
            switch (oid)
            {
                case "1.3.14.3.2.26": return SHA1.Create();
                case "2.16.840.1.101.3.4.2.1": return SHA256.Create();
                case "2.16.840.1.101.3.4.2.2": return SHA384.Create();
                case "2.16.840.1.101.3.4.2.3": return SHA512.Create();
                case "1.2.840.113549.2.5": return MD5.Create(); // legacy
                default: return null;
            }
        }

        private static X509Certificate2[] LoadCertificatesFromDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

            List<X509Certificate2> list = new List<X509Certificate2>();
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".cer" || ext == ".crt" || ext == ".der" || ext == ".pem")
                {
                    try
                    {
                        byte[] raw = File.ReadAllBytes(path);
                        if (ext == ".pem")
                        {
                            raw = PemToDer(raw);
                            if (raw == null) continue;
                        }
                        var cert = new X509Certificate2(raw);
                        list.Add(cert);
                    }
                    catch { /* ignore bad files */ }
                }
            }
            return list.ToArray();
        }

        private static byte[] PemToDer(byte[] pemBytes)
        {
            // Minimal PEM parser for "-----BEGIN CERTIFICATE-----"
            string s = Encoding.ASCII.GetString(pemBytes);
            const string begin = "-----BEGIN CERTIFICATE-----";
            const string end = "-----END CERTIFICATE-----";
            int i0 = s.IndexOf(begin, StringComparison.Ordinal);
            if (i0 < 0) return null;
            i0 += begin.Length;
            int i1 = s.IndexOf(end, i0, StringComparison.Ordinal);
            if (i1 < 0) return null;
            string b64 = s.Substring(i0, i1 - i0);
            b64 = b64.Replace("\r", "").Replace("\n", "").Trim();
            try { return Convert.FromBase64String(b64); } catch { return null; }
        }

        private static void GatherAiaUrls(X509Certificate2 cert, List<string> outUrls, HashSet<string> visitedUrls)
        {
            if (cert == null || cert.Extensions == null) return;

            for (int i = 0; i < cert.Extensions.Count; i++)
            {
                var ext = cert.Extensions[i];
                if (ext == null || ext.Oid == null) continue;
                if (!StringEquals(ext.Oid.Value, "1.3.6.1.5.5.7.1.1")) continue; // Authority Information Access

                try
                {
                    var r = new DerReader(ext.RawData);
                    var aiaSeq = r.ReadSequence(); // AuthorityInfoAccessSyntax ::= SEQUENCE OF AccessDescription
                    while (aiaSeq.HasData)
                    {
                        var ad = aiaSeq.ReadSequence();   // AccessDescription
                        string methodOid = ad.ReadOid();  // accessMethod
                        // accessLocation is a GeneralName; for URI it's [6] IA5String => tag 0x86
                        string uri = ad.ReadUriIfPresent(); // returns null if not a URI

                        if (uri != null &&
                            StringEquals(methodOid, "1.3.6.1.5.5.7.48.2")) // caIssuers
                        {
                            if (!visitedUrls.Contains(uri))
                            {
                                outUrls.Add(uri);
                                visitedUrls.Add(uri);
                            }
                        }
                    }
                }
                catch
                {
                    // ignore malformed AIA
                }
            }
        }

        private static X509Certificate2[] DownloadCertificatesFromAia(string uri, VerificationOptions options)
        {
            try
            {
                // Allow only http/https
                if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !options.AllowAiaInsecureHttp)
                    return null;

                using (var http = new HttpClient())
                {
                    http.Timeout = options.AiaHttpTimeout;
                    http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "WinTrustDotNet/1.0");
                    http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/pkix-cert, application/pkcs7-mime, application/pkcs7-certificates, */*");

                    byte[] data = http.GetByteArrayAsync(uri).GetAwaiter().GetResult();
                    if (data == null || data.Length == 0) return null;

                    // Try decode as PKCS#7 cert bag (p7c)
                    try
                    {
                        var cms = new SignedCms();
                        cms.Decode(data);
                        if (cms.Certificates != null && cms.Certificates.Count > 0)
                        {
                            var arr = new X509Certificate2[cms.Certificates.Count];
                            for (int i = 0; i < cms.Certificates.Count; i++)
                                arr[i] = cms.Certificates[i];
                            return arr;
                        }
                    }
                    catch { /* not a PKCS#7 */ }

                    // Try DER single cert
                    try
                    {
                        var single = new X509Certificate2(data);
                        return new[] { single };
                    }
                    catch { /* not a single DER */ }

                    // Try PEM (rare via AIA)
                    try
                    {
                        byte[] der = PemToDer(data);
                        if (der != null)
                        {
                            var pemCert = new X509Certificate2(der);
                            return new[] { pemCert };
                        }
                    }
                    catch { }

                    return null;
                }
            }
            catch
            {
                return null; // network/parse issues
            }
        }

        // ---------- Minimal DER reader ----------

        [ObjectId("4339cc70-a38e-46fb-ae69-26590049be84")]
        private sealed class DerReader
        {
            private readonly byte[] _data;
            private int _pos;
            private readonly int _end;

            public DerReader(byte[] data) : this(data, 0, data != null ? data.Length : 0) { }

            private DerReader(byte[] data, int offset, int length)
            {
                _data = data;
                _pos = offset;
                _end = offset + length;
            }

            public bool HasData => _pos < _end;

            // Inside DerReader
            public string ReadUriIfPresent()
            {
                // A GeneralName with tag [6] IA5String uses the context-specific primitive tag 0x86.
                if (!HasData) return null;
                if (_data[_pos] != 0x86) // [6] IA5String
                {
                    // Not a URI; skip the value to keep parsing aligned
                    SkipValue();
                    return null;
                }

                byte tag;
                int len, header;
                ReadTagLen(out tag, out len, out header);
                if (tag != 0x86 || _pos + len > _end) return null;

                string uri = Encoding.ASCII.GetString(_data, _pos, len);
                _pos += len;
                return uri;
            }

            public byte PeekTag()
            {
                if (_pos >= _end) throw new InvalidDataException("Unexpected end.");
                return _data[_pos];
            }

            // Correctly returns the SEQUENCE start (tag) and total encoded length (header+content).
            public bool TryReadRawSequenceFull(out int sequenceStart, out int totalEncodedLength)
            {
                sequenceStart = 0;
                totalEncodedLength = 0;
                int save = _pos;
                try
                {
                    byte tag; int len, hdr;
                    ReadTagLen(out tag, out len, out hdr);
                    if (tag != 0x30) { _pos = save; return false; }
                    sequenceStart = save;               // <-- start at the SEQUENCE TAG (not at content)
                    totalEncodedLength = hdr + len;     // header + content
                    _pos = save + totalEncodedLength;   // advance to end of this SEQUENCE
                    return true;
                }
                catch
                {
                    _pos = save;
                    return false;
                }
            }

            // Reads a SEQUENCE and returns its raw slice offsets (without advancing outer reader except consuming the sequence)
            public bool TryReadRawSequence(out int contentStart, out int totalLength)
            {
                contentStart = 0;
                totalLength = 0;
                int save = _pos;
                try
                {
                    byte tag;
                    int len, hdr;
                    ReadTagLen(out tag, out len, out hdr);
                    if (tag != 0x30) { _pos = save; return false; }
                    contentStart = _pos; // at start of content
                    totalLength = len + hdr;
                    // Move to end of sequence
                    _pos = contentStart + len;
                    return true;
                }
                catch
                {
                    _pos = save; return false;
                }
            }

            public DerReader ReadSequence()
            {
                byte tag;
                int len, hdr;
                ReadTagLen(out tag, out len, out hdr);
                if (tag != 0x30) throw new InvalidDataException("Expected SEQUENCE");
                int start = _pos;
                int end = _pos + len;
                DerReader inner = new DerReader(_data, start, len);
                _pos = end;
                return inner;
            }

            public void SkipValue()
            {
                byte tag;
                int len, hdr;
                ReadTagLen(out tag, out len, out hdr);
                _pos += len;
            }

            public void SkipToEnd() { _pos = _end; }

            public void SkipValueExpectedTag(byte expectedTag)
            {
                byte tag;
                int len, hdr;
                ReadTagLen(out tag, out len, out hdr);
                if (tag != expectedTag) throw new InvalidDataException("Unexpected tag.");
                _pos += len;
            }

            public string ReadOid()
            {
                byte tag;
                int len, hdr;
                ReadTagLen(out tag, out len, out hdr);
                if (tag != 0x06) throw new InvalidDataException("Expected OID");

                if (len <= 0 || _pos + len > _end) throw new InvalidDataException("Bad OID length");

                int start = _pos;
                int end = _pos + len;

                StringBuilder sb = new StringBuilder();

                byte first = _data[_pos++];
                int v1 = first / 40;
                int v2 = first % 40;
                sb.Append(v1.ToString());
                sb.Append('.');
                sb.Append(v2.ToString());

                ulong value = 0;
                while (_pos < end)
                {
                    byte b = _data[_pos++];
                    value = (value << 7) | (uint)(b & 0x7F);
                    if ((b & 0x80) == 0)
                    {
                        sb.Append('.');
                        sb.Append(value.ToString());
                        value = 0;
                    }
                }

                if (_pos != end) throw new InvalidDataException("OID parse error");
                return sb.ToString();
            }

            public byte[] ReadOctetString()
            {
                byte tag;
                int len, hdr;
                ReadTagLen(out tag, out len, out hdr);
                if (tag != 0x04) throw new InvalidDataException("Expected OCTET STRING");

                if (_pos + len > _end) throw new InvalidDataException("Truncated OCTET STRING");
                byte[] val = new byte[len];
                Buffer.BlockCopy(_data, _pos, val, 0, len);
                _pos += len;
                return val;
            }

            public DateTimeOffset ReadGeneralizedTime()
            {
                byte tag;
                int len, hdr;
                ReadTagLen(out tag, out len, out hdr);
                if (tag != 0x18) throw new InvalidDataException("Expected GeneralizedTime");
                if (_pos + len > _end) throw new InvalidDataException("Truncated GeneralizedTime");
                string s = Encoding.ASCII.GetString(_data, _pos, len);
                _pos += len;

                // Accept forms: YYYYMMDDHHMMSSZ or with fractional seconds (e.g., .fff) and 'Z'
                // We’ll parse minimally and assume 'Z' (UTC).
                if (!s.EndsWith("Z", StringComparison.Ordinal)) throw new InvalidDataException("GeneralizedTime not UTC");
                s = s.Substring(0, s.Length - 1);

                // Trim fractional seconds if present
                int dot = s.IndexOf('.');
                string basePart = dot >= 0 ? s.Substring(0, dot) : s;

                // Ensure at least YYYYMMDDHHMMSS
                if (basePart.Length < 14) throw new InvalidDataException("GeneralizedTime too short");

                int year = int.Parse(basePart.Substring(0, 4));
                int mon = int.Parse(basePart.Substring(4, 2));
                int day = int.Parse(basePart.Substring(6, 2));
                int hh = int.Parse(basePart.Substring(8, 2));
                int mm = int.Parse(basePart.Substring(10, 2));
                int ss = int.Parse(basePart.Substring(12, 2));

                return new DateTimeOffset(new DateTime(year, mon, day, hh, mm, ss, DateTimeKind.Utc));
            }

            private void ReadTagLen(out byte tag, out int len, out int headerLen)
            {
                if (_pos >= _end) throw new InvalidDataException("Unexpected end of data");
                tag = _data[_pos++];

                if (_pos >= _end) throw new InvalidDataException("Unexpected end of data");
                int b = _data[_pos++];

                if ((b & 0x80) == 0)
                {
                    len = b;
                    headerLen = 2;
                }
                else
                {
                    int n = b & 0x7F;
                    if (n == 0 || n > 4) throw new InvalidDataException("Unsupported length form");
                    if (_pos + n > _end) throw new InvalidDataException("Truncated length");
                    len = 0;
                    for (int i = 0; i < n; i++)
                    {
                        len = (len << 8) | _data[_pos++];
                    }
                    headerLen = 2 + n;
                }

                if (_pos + len > _end) throw new InvalidDataException("Length exceeds container");
            }

            // Helper used by GetSingleAttributeOctetValue
            public static bool ReadLength(byte[] data, int offset, out int len, out int contentOffset)
            {
                len = 0;
                contentOffset = 0;
                if (offset >= data.Length) return false;
                int b = data[offset];
                if ((b & 0x80) == 0)
                {
                    len = b;
                    contentOffset = offset + 1;
                    return (contentOffset + len <= data.Length);
                }
                int n = b & 0x7F;
                if (n <= 0 || n > 4) return false;
                if (offset + 1 + n > data.Length) return false;
                int val = 0;
                for (int i = 0; i < n; i++)
                {
                    val = (val << 8) | data[offset + 1 + i];
                }
                len = val;
                contentOffset = offset + 1 + n;
                return (contentOffset + len <= data.Length);
            }
        }
    }
}
