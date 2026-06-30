/*
 * WinTrustDotNet2.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    internal static partial class WinTrustDotNet
    {
        #region Private Constants
        //
        // NOTE: OIDs used for timestamp and nested signature attribute lookup.
        //
        /// <summary>
        /// The object identifier (OID) for the PKCS#7 / CMS signedData content
        /// type.
        /// </summary>
        private const string OID_CMS_SIGNED_DATA = "1.2.840.113549.1.7.2";

        /// <summary>
        /// The object identifier (OID) for the RFC 3161 id-aa-
        /// signatureTimeStampToken unsigned attribute.
        /// </summary>
        private const string OID_RFC3161_TSTOKEN =
            "1.2.840.113549.1.9.16.2.14";

        /// <summary>
        /// The object identifier (OID) for the Microsoft RFC 3161 timestamp
        /// attribute.
        /// </summary>
        private const string OID_MS_TSTOKEN = "1.3.6.1.4.1.311.3.3.1";

        /// <summary>
        /// The object identifier (OID) for the Microsoft SpcNestedSignature
        /// unsigned attribute.
        /// </summary>
        private const string OID_MS_NESTED_SIG = "1.3.6.1.4.1.311.2.4.1";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Encoded OID for id-ct-TSTInfo (1.2.840.113549.1.9.16.1.4).
        //
        /// <summary>
        /// The DER-encoded object identifier (OID) for id-ct-TSTInfo
        /// (1.2.840.113549.1.9.16.1.4), used when scanning attribute values
        /// for an embedded RFC 3161 TSTInfo structure.
        /// </summary>
        private static readonly byte[] OID_TSTINFO_ENC = new byte[] {
            0x06, 0x0B, 0x2A, 0x86, 0x48, 0x86, 0xF7,
            0x0D, 0x01, 0x09, 0x10, 0x01, 0x04
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Classes
        /// <summary>
        /// This class holds the tunable policy settings that control how a PE
        /// file signature is verified, including signer-chain validation,
        /// revocation checking, custom root trust, digest-algorithm policy,
        /// timestamp policy, and Authority Information Access (AIA) download
        /// behavior.
        /// </summary>
        [ObjectId("b9c0859a-615c-4e09-bd23-bd64919e297f")]
        public sealed class VerificationOptions
        {
            //
            // NOTE: Chain and revocation settings.
            //
            /// <summary>
            /// When true, the signer certificate chain is built and validated;
            /// when false, the signer chain is treated as valid without being
            /// checked.
            /// </summary>
            public bool ValidateSignerChain = true;

            /// <summary>
            /// The revocation-checking mode applied when building certificate
            /// chains.
            /// </summary>
            public X509RevocationMode RevocationMode =
                X509RevocationMode.NoCheck;

            /// <summary>
            /// The maximum time allowed for online revocation URL retrieval
            /// while building certificate chains.
            /// </summary>
            public TimeSpan RevocationUrlRetrievalTimeout =
                TimeSpan.FromSeconds(15);

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Custom roots (directory with *.cer/*.crt/*.pem files).
            //
            /// <summary>
            /// When true, certificate chains are validated against the custom
            /// root certificates loaded from <see cref="CustomRootDirectory" />
            /// instead of (or in addition to) the operating system trust store.
            /// </summary>
            public bool UseCustomRootTrust = false;
            /// <summary>
            /// The path to a directory containing custom root certificate files
            /// (*.cer/*.crt/*.der/*.pem); a null or empty value disables custom
            /// root loading.
            /// </summary>
            public string CustomRootDirectory = null;

            //
            // NOTE: If the runtime lacks CustomRootTrust
            //       (e.g., .NET Core 2.x), allow a compatibility fallback.
            //
            /// <summary>
            /// When true, on runtimes that cannot apply custom root trust
            /// directly, a chain that fails only with an untrusted-root status
            /// is accepted if its root matches one of the supplied custom roots.
            /// </summary>
            public bool TrustIfOnlyUntrustedRootAndMatchesCustomRoot = true;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Extra intermediates (optional).
            //
            /// <summary>
            /// The path to a directory containing additional intermediate
            /// certificate files used to help build certificate chains; a null
            /// or empty value disables loading of extra intermediates.
            /// </summary>
            public string AdditionalIntermediatesDirectory = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Digest algorithm policy.
            //
            /// <summary>
            /// When true, the MD5 digest algorithm is permitted; otherwise, a
            /// signature using MD5 is rejected by policy.
            /// </summary>
            public bool AllowMd5 = false;
            /// <summary>
            /// When true, the SHA-1 digest algorithm is permitted; otherwise, a
            /// signature using SHA-1 is rejected by policy.
            /// </summary>
            public bool AllowSha1 = true;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Timestamp policy.
            //
            /// <summary>
            /// When true, a valid timestamp is required for the overall result
            /// to be considered fully valid.
            /// </summary>
            public bool RequireTimestamp = false;
            /// <summary>
            /// When true, the timestamping authority (TSA) certificate chain is
            /// built and validated; when false, the TSA chain is treated as
            /// valid without being checked.
            /// </summary>
            public bool ValidateTimestampChain = true;
            /// <summary>
            /// When true, an RFC 3161 timestamp token is preferred over a legacy
            /// PKCS#9 countersignature when both are present.
            /// </summary>
            public bool PreferRfc3161OverCountersign = true;
            /// <summary>
            /// When true, a best-effort byte-scan fallback is allowed to recover
            /// TSTInfo time and binding when the timestamp token cannot be
            /// decoded as a CMS message.
            /// </summary>
            public bool AllowTstInfoScanFallback = true;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Require TSA EKU.  Standards say yes; set false to relax.
            //
            /// <summary>
            /// When true, the timestamping authority (TSA) certificate is
            /// required to carry the timeStamping extended key usage (EKU); set
            /// to false to relax this standards requirement.
            /// </summary>
            public bool RequireTsaEku = true;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: AIA auto-download settings for intermediate certificate
            //       discovery.
            //
            // BUGFIX: Default to DISABLED.  Following caIssuers (AIA) URLs read
            //         from the not-yet-trusted certificate under verification
            //         makes the verifying host issue attacker-directed outbound
            //         requests (an SSRF / callback-on-verify vector).  It is a
            //         convenience for chain building, not a correctness
            //         requirement, so it must be opted into explicitly.  (HTTPS
            //         is still required unless AllowAiaInsecureHttp, and the
            //         download is size/time/depth bounded with a loop guard.)
            //
            /// <summary>
            /// When true, missing intermediate certificates may be downloaded
            /// automatically by following Authority Information Access (AIA)
            /// caIssuers URLs; disabled by default to avoid an SSRF / callback-
            /// on-verify vector.
            /// </summary>
            public bool AutoDownloadIntermediates = false;
            /// <summary>
            /// When true, insecure (plain HTTP) AIA URLs are allowed for
            /// intermediate certificate download; otherwise, only HTTPS URLs are
            /// used.
            /// </summary>
            public bool AllowAiaInsecureHttp = false;

            /// <summary>
            /// The maximum time allowed for each AIA certificate download
            /// request.
            /// </summary>
            public TimeSpan AiaHttpTimeout = TimeSpan.FromSeconds(10);

            /// <summary>
            /// The maximum number of AIA download-and-retry iterations performed
            /// while chasing missing intermediate certificates.
            /// </summary>
            public int AiaMaxDepth = 3;
            /// <summary>
            /// The maximum size, in bytes, of an AIA download response, used to
            /// bound memory consumption.
            /// </summary>
            public int AiaMaxResponseSize = 1024 * 1024;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This class describes the timestamp discovered for a signature,
        /// including whether one is present, its kind (RFC 3161 or legacy
        /// countersignature), its cryptographic and binding validity, the
        /// timestamping authority (TSA) identity, and the timestamp value.
        /// </summary>
        [ObjectId("47831b1d-b6cf-4643-8bcf-b8700a1b0cad")]
        public sealed class TimestampInfo
        {
            /// <summary>
            /// When true, a timestamp was found for the signature.
            /// </summary>
            public bool Present;
            /// <summary>
            /// When true, the timestamp is an RFC 3161 timestamp token;
            /// otherwise, it is a legacy PKCS#9 countersignature.
            /// </summary>
            public bool IsRfc3161;
            /// <summary>
            /// When true, the timestamp token signature is cryptographically
            /// valid.
            /// </summary>
            public bool CryptographicallyValid;
            /// <summary>
            /// When true, the timestamp is bound to the signer's signature (its
            /// message digest matches the hash of the signer signature).
            /// </summary>
            public bool BoundToSignerSignature;
            /// <summary>
            /// When true, the timestamping authority (TSA) certificate chain was
            /// validated successfully (or chain validation was not required).
            /// </summary>
            public bool ChainValid;
            /// <summary>
            /// The subject name of the timestamping authority (TSA)
            /// certificate, if available.
            /// </summary>
            public string TsaSubject;
            /// <summary>
            /// The thumbprint of the timestamping authority (TSA) certificate,
            /// if available.
            /// </summary>
            public string TsaThumbprint;
            /// <summary>
            /// The timestamp value in Coordinated Universal Time (UTC), or null
            /// if no time was recovered.
            /// </summary>
            public DateTimeOffset? TimeUtc;

            /// <summary>
            /// The status strings produced when building the timestamping
            /// authority (TSA) certificate chain.
            /// </summary>
            public string[] ChainStatus = Array.Empty<string>();

            /// <summary>
            /// Gets the timestamp value converted to local time, or null if no
            /// time was recovered.
            /// </summary>
            public DateTimeOffset? TimeLocal
            {
                get
                {
                    return TimeUtc.HasValue ? TimeUtc.Value.ToLocalTime() :
                        (DateTimeOffset?)null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This class holds the outcome of verifying a PE file signature,
        /// including whether the file is signed, whether the CMS signature and
        /// file digest are valid, the signer identity and chain status, the
        /// signing time, and the discovered timestamp information.
        /// </summary>
        [ObjectId("a8a46ae4-2929-4a1b-bc38-c7ca78a91019")]
        public sealed class VerificationResult
        {
            /// <summary>
            /// When true, the file contains an Authenticode signature.
            /// </summary>
            public bool IsSigned;
            /// <summary>
            /// When true, the CMS signature over the signed content is
            /// cryptographically valid.
            /// </summary>
            public bool CmsSignatureValid;
            /// <summary>
            /// When true, the Authenticode hash computed over the file matches
            /// the digest carried in the signature.
            /// </summary>
            public bool FileHashMatchesSignature;
            /// <summary>
            /// When true, the signer certificate chain was validated
            /// successfully (or chain validation was not required).
            /// </summary>
            public bool SignerChainValid;

            /// <summary>
            /// The status strings produced when building the signer certificate
            /// chain.
            /// </summary>
            public string[] SignerChainStatus = Array.Empty<string>();

            /// <summary>
            /// The object identifier (OID) of the digest algorithm used by the
            /// signature.
            /// </summary>
            public string DigestAlgorithmOid;
            /// <summary>
            /// The subject name of the signer certificate, if available.
            /// </summary>
            public string SignerSubject;
            /// <summary>
            /// The thumbprint of the signer certificate, if available.
            /// </summary>
            public string SignerThumbprint;

            /// <summary>
            /// The signing time taken from the signer's signed attributes in
            /// Coordinated Universal Time (UTC), if present.
            /// </summary>
            public DateTimeOffset? SigningTimeUtc;

            /// <summary>
            /// The timestamp information discovered for the signature.
            /// </summary>
            public TimestampInfo Timestamp = new TimestampInfo();

            /// <summary>
            /// Gets a value indicating whether every required aspect of the
            /// signature is valid, honoring the timestamp policy carried in
            /// <see cref="OptionsReference" />.
            /// </summary>
            public bool AllValid
            {
                get
                {
                    if (!IsSigned)
                        return false;

                    if (!CmsSignatureValid)
                        return false;

                    if (!FileHashMatchesSignature)
                        return false;

                    if (!SignerChainValid)
                        return false;

                    //
                    // NOTE: Enforce timestamp only if the caller requires one.
                    //
                    if ((OptionsReference != null) &&
                        (OptionsReference.RequireTimestamp))
                    {
                        if ((Timestamp == null) || (!Timestamp.Present))
                        {
                            return false;
                        }

                        if (!Timestamp.BoundToSignerSignature)
                        {
                            return false;
                        }

                        if (!Timestamp.CryptographicallyValid)
                        {
                            return false;
                        }

                        //
                        // HACK: This is needed due to: https://github.com/
                        //       dotnet/runtime/issues/62307
                        //
                        // NOTE: Please also see: https://github.com/
                        //       dotnet/runtime/issues/65163
                        //
                        //       https://github.com/dotnet/runtime/pull/64348
                        //
                        if ((OptionsReference.ValidateTimestampChain) &&
                            (!Timestamp.ChainValid))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: For AllValid computation with RequireTimestamp.
            //
            /// <summary>
            /// A reference to the verification options used to produce this
            /// result, consulted by <see cref="AllValid" /> to apply the
            /// timestamp policy.
            /// </summary>
            internal VerificationOptions
                OptionsReference;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method returns a human-readable summary of the verification
            /// result, including the signature, digest, signer-chain, and
            /// timestamp status.
            /// </summary>
            /// <returns>
            /// A string describing this verification result.
            /// </returns>
            public override string ToString()
            {
                string ts = ((Timestamp != null) &&
                     (Timestamp.Present)) ? String.Format(
                        "Timestamp: {0}, " + "Crypto={1}, Bound={2}, " +
                        "Chain={3}, TimeUTC={4}, " + "TimeLocal={5}",
                        Timestamp.IsRfc3161 ? "RFC3161" : "CounterSig",
                        Timestamp.CryptographicallyValid,
                        Timestamp.BoundToSignerSignature,
                        Timestamp.ChainValid, Timestamp.TimeUtc,
                        Timestamp.TimeLocal) : "Timestamp: none";

                return String.Format("Signed={0}, CMS={1}, " +
                    "FileDigestMatch={2}, " + "SignerChain={3}, " +
                    "Signer='{4}', " + "Thumbprint={5}, {6}",
                    IsSigned, CmsSignatureValid, FileHashMatchesSignature,
                    SignerChainValid, SignerSubject, SignerThumbprint, ts);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method verifies the Authenticode signature of a portable
        /// executable (PE) file, checking the CMS signature, the file digest,
        /// the signer certificate chain, and any embedded timestamp according
        /// to the supplied options.
        /// </summary>
        /// <param name="filePath">
        /// The path to the PE file to verify.  This parameter may not be null
        /// and must refer to an existing file.
        /// </param>
        /// <param name="options">
        /// The verification options to apply.  If this parameter is null, a
        /// default set of options is used.
        /// </param>
        /// <returns>
        /// A <see cref="VerificationResult" /> describing the outcome of the
        /// verification.
        /// </returns>
        public static VerificationResult
            VerifyPeFileSignature(
            string filePath,                 /* in */
            VerificationOptions options      /* in */
            )
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            if (options == null)
                options = new VerificationOptions();

            using (FileStream fs = File.OpenRead(filePath))
            {
                //
                // NOTE: Step 1 - Locate PE offsets
                //       (checksum, security directory, certificate table).
                //
                PeLayout pe = ReadPeLayout(fs);

                if ((pe.CertTableOffset == 0) || (pe.CertTableSize == 0) ||
                    (pe.CertTableOffset >= fs.Length))
                {
                    return new VerificationResult {
                        IsSigned = false,
                        OptionsReference = options
                    };
                }

                //
                // NOTE: Step 2 - Read first PKCS#7 blob from WIN_CERTIFICATE
                //       table.
                //
                byte[] pkcs7 = ReadFirstPkcs7FromWinCertificateTable(
                        fs, pe.CertTableOffset, pe.CertTableSize);

                if ((pkcs7 == null) || (pkcs7.Length == 0))
                {
                    return new VerificationResult {
                        IsSigned = false,
                        OptionsReference = options
                    };
                }

                VerificationResult result = new VerificationResult {
                        IsSigned = true,
                        OptionsReference = options
                    };

                //
                // NOTE: Step 3 - Decode CMS.
                //
                SignedCms cms = new SignedCms();
                cms.Decode(pkcs7);

                //
                // NOTE: Step 4 - Cryptographic verification of CMS signatures
                //       (not chain).
                //
                try
                {
                    cms.CheckSignature(true);
                    result.CmsSignatureValid = true;
                }
                catch (CryptographicException e)
                {
                    TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                        TracePriority.SecurityError);

                    result.CmsSignatureValid = false;
                }

                //
                // NOTE: Step 5 - Parse SpcIndirectDataContent to
                //       learn digest algorithm and expected digest.
                //
                byte[] eContent = cms.ContentInfo.Content;

                string digestOid;
                byte[] expectedDigest;

                if (!TryParseSpcIndirectDataDigest(eContent, out digestOid,
                        out expectedDigest))
                {
                    throw new InvalidDataException("Failed to parse " +
                        "SpcIndirectDataContent" + " / DigestInfo.");
                }

                result.DigestAlgorithmOid = digestOid;

                //
                // NOTE: Step 5b - Reject disallowed digest algorithms.
                //
                if (!IsDigestAlgorithmAllowed(digestOid, options))
                {
                    throw new NotSupportedException(
                        "Digest algorithm rejected " +
                        "by policy: " + digestOid);
                }

                //
                // NOTE: Step 6 - Compute the Authenticode hash of the file
                //       and compare.
                //
                using (HashAlgorithm hash = CreateHashFromOid(digestOid))
                {
                    if (hash == null)
                    {
                        throw new
                            NotSupportedException("Unsupported digest " +
                            "algorithm OID: " + digestOid);
                    }

                    byte[] actual = ComputeAuthenticodeHash(
                            fs, hash, pe, options);

                    result.FileHashMatchesSignature =
                        (expectedDigest != null) &&
                        (actual != null) &&
                        (BytesEqual(
                            expectedDigest, actual));
                }

                //
                // NOTE: Step 7 - Get primary signer (first).
                //
                X509Certificate2 signerCert = null;
                SignerInfo primarySigner = null;

                if (cms.SignerInfos.Count > 0)
                {
                    primarySigner = cms.SignerInfos[0];

                    signerCert = primarySigner.Certificate;
                }

                //
                // NOTE: SigningTime (if present).
                //
                result.SigningTimeUtc = TryGetSigningTimeUtc(primarySigner);

                //
                // NOTE: Step 8 - Build signer chain
                //       (OS trust or custom roots).
                //
                if ((options.ValidateSignerChain) && (signerCert != null))
                {
                    X509Certificate2Collection extra =
                        new X509Certificate2Collection();

                    for (int i = 0;
                            i < cms.Certificates.Count;
                            i++)
                    {
                        X509Certificate2 c = cms.Certificates[i];

                        if (!CertEqualThumbprint(c, signerCert))
                        {
                            extra.Add(c);
                        }
                    }

                    //
                    // NOTE: Add user-supplied intermediates.
                    //
                    X509Certificate2[] extraFromDir =
                        LoadCertificatesFromDirectory(options
                            .AdditionalIntermediatesDirectory);

                    for (int i = 0;
                            (extraFromDir != null) &&
                            (i < extraFromDir.Length);
                            i++)
                    {
                        extra.Add(extraFromDir[i]);
                    }

                    X509Certificate2[] customRoots =
                        LoadCertificatesFromDirectory(options
                            .CustomRootDirectory);

                    //
                    // NOTE: Discover timestamp across all signers and nests
                    //       *before* building the signer chain.
                    //
                    result.Timestamp = FindBestTimestampAcrossSigners(
                            cms, options);

                    //
                    // BUGFIX: Use the timestamp time as the signer-chain
                    //         verification time ONLY when the timestamp is
                    //         itself trustworthy -- cryptographically valid,
                    //         bound to this signer's signature, and (when the
                    //         TSA chain is being validated) chain-valid.  A
                    //         partial/unverified timestamp (e.g. a scan-fallback
                    //         candidate, or one that failed binding) must NOT be
                    //         allowed to set the validity-period reference time:
                    //         otherwise an attacker could embed a forged
                    //         timestamp with an arbitrary time to make an
                    //         expired (or not-yet-valid) signing certificate
                    //         pass the NotTimeValid check.  When the timestamp
                    //         is not fully trustworthy, leave the verification
                    //         time null so the chain is validated against the
                    //         current time.  (Note: ChainValid already accounts
                    //         for ValidateTimestampChain being disabled.)
                    //
                    DateTimeOffset? chainTime = null;

                    if ((result.Timestamp != null) &&
                        result.Timestamp.CryptographicallyValid &&
                        result.Timestamp.BoundToSignerSignature &&
                        result.Timestamp.ChainValid)
                    {
                        chainTime = result.Timestamp.TimeUtc;
                    }

                    string[] statuses;

                    bool chainOk = BuildChainWithAutoAia(signerCert, extra,
                            chainTime /* verificationTime */,
                            options, customRoots,
                            false /* requireTimeStampingEku */, out statuses);

                    result.SignerChainValid = chainOk;
                    result.SignerChainStatus = statuses;

                    result.SignerSubject = signerCert.Subject;

                    result.SignerThumbprint = signerCert.Thumbprint;

                    //
                    // BUGFIX: dispose the intermediate / custom-root certificates
                    //         we loaded from disk above (these are populated only
                    //         when the AdditionalIntermediatesDirectory /
                    //         CustomRootDirectory options are set).  They were used
                    //         solely to build the signer chain, which is now
                    //         complete.  The CMS-provided certificates also present
                    //         in "extra" are owned by the SignedCms and are
                    //         deliberately NOT disposed here, nor is signerCert.
                    //
                    for (int i = 0;
                            (extraFromDir != null) &&
                            (i < extraFromDir.Length);
                            i++)
                    {
                        try { extraFromDir[i].Dispose(); }
                        catch { /* best effort */ }
                    }

                    for (int i = 0;
                            (customRoots != null) &&
                            (i < customRoots.Length);
                            i++)
                    {
                        try { customRoots[i].Dispose(); }
                        catch { /* best effort */ }
                    }
                }
                else
                {
                    //
                    // NOTE: If not required, treat as ok.
                    //
                    result.SignerChainValid = !options.ValidateSignerChain;
                }

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method unwraps a Microsoft RFC 3161 attribute value, locating
        /// the inner ContentInfo SEQUENCE and returning its raw DER slice.
        /// </summary>
        /// <param name="attrValue">
        /// The raw DER bytes of the attribute value to unwrap.
        /// </param>
        /// <param name="contentInfoDer">
        /// Upon success, receives the raw DER bytes of the inner ContentInfo;
        /// upon failure, is set to null.
        /// </param>
        /// <returns>
        /// True if the ContentInfo was located and extracted; otherwise, false.
        /// </returns>
        private static bool TryUnwrapMicrosoftRfc3161(
            byte[] attrValue,
            out byte[] contentInfoDer
            )
        {
            contentInfoDer = null;

            try
            {
                DerReader r = new DerReader(attrValue);
                DerReader seq = r.ReadSequence();

                //
                // NOTE: First element is OID (but some
                //       signers omit it); if present,
                //       just skip.  If first tag is OID,
                //       read it, else assume we are already at ContentInfo.
                //
                byte nextTagPeek = seq.PeekTag();

                if (nextTagPeek == 0x06)
                {
                    seq.ReadOid();
                    nextTagPeek = seq.PeekTag();
                }

                //
                // NOTE: Optional attributes (SET or
                //       SEQUENCE) -- skip if present.
                //
                if ((nextTagPeek == 0x31) || (nextTagPeek == 0x30))
                {
                    seq.SkipValue();
                    nextTagPeek = seq.PeekTag();
                }

                //
                // NOTE: Now expect ContentInfo as a SEQUENCE.  Return its raw
                //       slice.
                //
                int start, length;

                if (seq.TryReadRawSequence(out start, out length))
                {
                    contentInfoDer = new byte[length];

                    Buffer.BlockCopy(attrValue, start,
                        contentInfoDer, 0, length);

                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Legacy PKCS #7 countersignature
        //       (OID 1.2.840.113549.1.9.6).  We verify:
        //       countersignature signature itself; that
        //       messageDigest(signedAttrs) matches
        //       HASH(parent signature); and (optionally)
        //       TSA chain trust (usually TSA cert has EKU timeStamping).
        //
        /// <summary>
        /// This method verifies a legacy PKCS#9 countersignature timestamp on
        /// the given signer, checking the countersignature cryptographically,
        /// confirming its binding to the parent signature, and (optionally)
        /// validating the timestamping authority (TSA) certificate chain.
        /// </summary>
        /// <param name="signer">
        /// The signer whose countersignature timestamp is being verified.
        /// </param>
        /// <param name="rootCms">
        /// The CMS message that contains the signer and its certificates.
        /// </param>
        /// <param name="options">
        /// The verification options controlling timestamp-chain validation.
        /// </param>
        /// <param name="info">
        /// The timestamp information instance populated by this method.
        /// </param>
        /// <returns>
        /// True if a countersignature timestamp was found and processed;
        /// otherwise, false.
        /// </returns>
        private static bool
            TryVerifyCounterSignatureTimestamp(
            SignerInfo signer,
            SignedCms rootCms,
            VerificationOptions options,
            TimestampInfo info
            )
        {
            SignerInfo countersigner = null;

            //
            // NOTE: Look at CounterSignerInfos collection.
            //
            if ((signer.CounterSignerInfos != null) &&
                (signer.CounterSignerInfos.Count > 0))
            {
                countersigner = signer.CounterSignerInfos[0];
            }
            else
            {
                //
                // NOTE: Some implementations include
                //       the OID but not the convenient
                //       property; try to detect the attribute anyway.
                //
                CryptographicAttributeObjectCollection
                    ua = signer.UnsignedAttributes;

                Oid oidCounter = new Oid("1.2.840.113549.1.9.6");

                for (int i = 0; i < ua.Count; i++)
                {
                    CryptographicAttributeObject attr = ua[i];

                    if ((attr.Oid != null) && (StringEquals(
                            attr.Oid.Value, oidCounter.Value)))
                    {
                        //
                        // NOTE: .NET exposes counter- signers via the
                        //       CounterSignerInfos property, so if it is
                        //       missing, we cannot easily decode here.
                        //
                        break;
                    }
                }
            }

            if (countersigner == null)
                return false;

            info.Present = true;
            info.IsRfc3161 = false;

            //
            // NOTE: Step 1: Verify countersignature cryptographically.
            //
            try
            {
                countersigner.CheckSignature(true);
                info.CryptographicallyValid = true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityError);

                info.CryptographicallyValid = false;
            }

            //
            // NOTE: Step 2: Extract signingTime from
            //       countersigner (if present).
            //
            info.TimeUtc = TryGetSigningTimeUtc(countersigner);

            //
            // NOTE: Step 3: Verify binding -- messageDigest(signedAttrs) must
            //       equal HASH(parentSigner signature).
            //
            byte[] parentSig = null;

            try
            {
                parentSig = GetSignerSignatureBytes(signer, rootCms);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityError);

                parentSig = null;
            }

            if (parentSig != null)
            {
                //
                // NOTE: Find signed attribute messageDigest (OID
                //       1.2.840.113549.1.9.4).
                //
                byte[] mdAttr = GetSingleAttributeOctetValue(
                        countersigner.SignedAttributes,
                        "1.2.840.113549.1.9.4");

                string csHashOid = ((countersigner.DigestAlgorithm !=
                        null) && (countersigner.DigestAlgorithm
                        .Value != null)) ? countersigner.DigestAlgorithm
                        .Value : null;

                if ((mdAttr != null) && (csHashOid != null))
                {
                    HashAlgorithm h = CreateHashFromOid(csHashOid);

                    using (h)
                    {
                        if (h != null)
                        {
                            byte[] digest = h.ComputeHash(parentSig);

                            info.BoundToSignerSignature = BytesEqual(
                                    digest, mdAttr);
                        }
                    }
                }
            }

            //
            // NOTE: Step 4: TSA chain trust (countersigner cert usually
            //       has EKU timeStamping).
            //
            if ((options.ValidateTimestampChain) &&
                (countersigner.Certificate != null))
            {
                X509Certificate2 tsa = countersigner.Certificate;

                X509Certificate2Collection extra =
                    new X509Certificate2Collection();

                for (int i = 0;
                        i < rootCms.Certificates.Count;
                        i++)
                {
                    if (!CertEqualThumbprint(rootCms.Certificates[i], tsa))
                    {
                        extra.Add(rootCms.Certificates[i]);
                    }
                }

                X509Certificate2[] customRoots = LoadCertificatesFromDirectory(
                        options.CustomRootDirectory);

                X509Certificate2[] extraFromDir =
                    LoadCertificatesFromDirectory(options
                            .AdditionalIntermediatesDirectory);

                for (int i = 0;
                        (extraFromDir != null) &&
                        (i < extraFromDir.Length); i++)
                {
                    extra.Add(extraFromDir[i]);
                }

                string[] statuses;

                bool chainOk = BuildChainWithAutoAia(
                        tsa, extra, info.TimeUtc, options, customRoots,
                        options.RequireTsaEku, out statuses);

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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a human-readable dump of the signature
        /// attributes found in a PE file, including signers, signed and
        /// unsigned attributes, countersigners, and (optionally) nested
        /// signatures.
        /// </summary>
        /// <param name="filePath">
        /// The path to the PE file whose signature attributes are dumped.  This
        /// parameter may not be null and must refer to an existing file.
        /// </param>
        /// <param name="includeNested">
        /// When true, nested SpcNestedSignature contents are recursively
        /// dumped.
        /// </param>
        /// <returns>
        /// A string containing the formatted signature attribute dump.
        /// </returns>
        public static string DumpSignatureAttributes(
            string filePath,
            bool includeNested
            )
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            StringBuilder sb = new StringBuilder();

            using (FileStream fs = File.OpenRead(filePath))
            {
                PeLayout pe = ReadPeLayout(fs);

                if ((pe.CertTableOffset == 0) || (pe.CertTableSize == 0) ||
                    (pe.CertTableOffset >= fs.Length))
                {
                    sb.AppendLine("No WIN_CERTIFICATE" + " / PKCS#7 found.");

                    return sb.ToString();
                }

                byte[] pkcs7 = ReadFirstPkcs7FromWinCertificateTable(
                        fs, pe.CertTableOffset, pe.CertTableSize);

                if ((pkcs7 == null) || (pkcs7.Length == 0))
                {
                    sb.AppendLine("Empty PKCS#7.");
                    return sb.ToString();
                }

                SignedCms cms = new SignedCms();
                cms.Decode(pkcs7);

                System.Reflection.Assembly pkcsAsm =
                    typeof(SignedCms).Assembly;

                sb.AppendLine(pkcsAsm.GetName().Name +
                    " " + pkcsAsm.GetName().Version);

                sb.AppendLine("=== TOP-LEVEL CMS ===");

                sb.AppendLine("ContentType OID: " +
                    (cms.ContentInfo.ContentType !=
                        null ? cms.ContentInfo.ContentType.Value : ""));

                sb.AppendLine("SignerInfos.Count: " + cms.SignerInfos.Count);

                sb.AppendLine("Certificates.Count: " + cms.Certificates.Count);

                sb.AppendLine();

                DumpCmsRecursive(cms, sb, 0, includeNested);
            }

            return sb.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method recursively dumps the signers, attributes, and
        /// countersigners of a CMS message into the supplied builder.
        /// </summary>
        /// <param name="cms">
        /// The CMS message to dump.
        /// </param>
        /// <param name="sb">
        /// The builder that receives the formatted output.
        /// </param>
        /// <param name="indent">
        /// The current indentation level, in spaces.
        /// </param>
        /// <param name="includeNested">
        /// When true, nested SpcNestedSignature contents are recursively
        /// dumped.
        /// </param>
        private static void DumpCmsRecursive(
            SignedCms cms,
            StringBuilder sb,
            int indent,
            bool includeNested
            )
        {
            for (int i = 0;
                    i < cms.SignerInfos.Count; i++)
            {
                SignerInfo si = cms.SignerInfos[i];

                DumpSignerInfo(si, cms, sb, indent);

                //
                // NOTE: Signed attributes.
                //
                DumpAttributeCollection("SignedAttributes",
                    si.SignedAttributes, si, cms, sb,
                    indent + 2, includeNested);

                //
                // NOTE: Unsigned attributes (where RFC3161, nested signatures,
                //       and countersignature references usually live).
                //
                DumpAttributeCollection("UnsignedAttributes",
                    si.UnsignedAttributes, si, cms, sb,
                    indent + 2, includeNested);

                //
                // NOTE: Countersigner(s) via PKCS#9
                //       countersignature -- recurse.
                //
                if ((si.CounterSignerInfos != null) &&
                    (si.CounterSignerInfos.Count > 0))
                {
                    Indent(sb, indent + 2).AppendLine(
                        "CounterSignerInfos.Count = " +
                        si.CounterSignerInfos.Count);

                    for (int k = 0;
                            k < si.CounterSignerInfos.Count;
                            k++)
                    {
                        SignerInfo csi = si.CounterSignerInfos[k];

                        Indent(sb, indent + 4).AppendLine(
                                "[CounterSigner " + k + "]");

                        DumpSignerInfo(csi, cms, sb, indent + 6);

                        DumpAttributeCollection("SignedAttributes",
                            csi.SignedAttributes, csi, cms, sb,
                            indent + 8, includeNested);

                        DumpAttributeCollection("UnsignedAttributes",
                            csi.UnsignedAttributes, csi, cms, sb,
                            indent + 8, includeNested);
                    }
                }

                sb.AppendLine();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method dumps the certificate, digest algorithm, and signing
        /// time of a single signer into the supplied builder.
        /// </summary>
        /// <param name="si">
        /// The signer to dump.
        /// </param>
        /// <param name="cms">
        /// The CMS message that contains the signer.
        /// </param>
        /// <param name="sb">
        /// The builder that receives the formatted output.
        /// </param>
        /// <param name="indent">
        /// The current indentation level, in spaces.
        /// </param>
        private static void DumpSignerInfo(
            SignerInfo si,
            SignedCms cms,
            StringBuilder sb,
            int indent
            )
        {
            X509Certificate2 cert = si.Certificate;

            Indent(sb, indent).AppendLine("Signer:");

            if (cert != null)
            {
                Indent(sb, indent + 2).AppendLine("Subject: " + cert.Subject);

                Indent(sb, indent + 2).AppendLine("Issuer : " + cert.Issuer);

                Indent(sb, indent + 2).AppendLine(
                    "Thumb  : " + cert.Thumbprint);

                Indent(sb, indent + 2).AppendLine(
                    "Serial : " + cert.SerialNumber);

                Indent(sb, indent + 2).AppendLine("NotBefore / NotAfter: " +
                    cert.NotBefore.ToUniversalTime() + " / " + cert.NotAfter
                        .ToUniversalTime());
            }
            else
            {
                Indent(sb, indent + 2).AppendLine(
                    "(Signer certificate is null)");
            }

            //
            // NOTE: e.g., 2.16.840.1.101.3.4.2.1 for SHA-256.
            //
            Oid dig = si.DigestAlgorithm;

            Indent(sb, indent + 2).AppendLine("DigestAlgorithm: " +
                (dig != null ? dig.Value : ""));

            //
            // NOTE: SigningTime (signed attribute, if present).
            //
            DateTimeOffset? st = TryGetSigningTimeUtc(si);

            if (st.HasValue)
            {
                Indent(sb, indent + 2).AppendLine(
                    "SigningTime (signed attr): " + st.Value.ToString("u"));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method dumps a collection of cryptographic attributes into the
        /// supplied builder, decoding well-known attributes (such as signing
        /// time, message digest, countersignature, RFC 3161 timestamp, and
        /// nested signature) where possible.
        /// </summary>
        /// <param name="label">
        /// A label describing the attribute collection (for example,
        /// "SignedAttributes" or "UnsignedAttributes").
        /// </param>
        /// <param name="attrs">
        /// The attribute collection to dump.
        /// </param>
        /// <param name="parentSigner">
        /// The signer that owns the attribute collection.
        /// </param>
        /// <param name="parentCms">
        /// The CMS message that contains the parent signer.
        /// </param>
        /// <param name="sb">
        /// The builder that receives the formatted output.
        /// </param>
        /// <param name="indent">
        /// The current indentation level, in spaces.
        /// </param>
        /// <param name="includeNested">
        /// When true, nested SpcNestedSignature contents are recursively
        /// dumped.
        /// </param>
        private static void DumpAttributeCollection(
            string label, CryptographicAttributeObjectCollection
                attrs,
            SignerInfo parentSigner,
            SignedCms parentCms,
            StringBuilder sb,
            int indent,
            bool includeNested
            )
        {
            if ((attrs == null) || (attrs.Count == 0))
            {
                Indent(sb, indent).AppendLine(label + ": (none)");

                return;
            }

            Indent(sb, indent).AppendLine(label + ":");

            for (int i = 0; i < attrs.Count; i++)
            {
                CryptographicAttributeObject a = attrs[i];

                string oid = a.Oid != null ? a.Oid.Value : "";

                Indent(sb, indent + 2).AppendLine("- OID: " + oid +
                    "  (" + OidFriendly(oid) + ")");

                if ((a.Values == null) || (a.Values.Count == 0))
                {
                    Indent(sb, indent + 4).AppendLine("(no values)");

                    continue;
                }

                for (int v = 0;
                        v < a.Values.Count; v++)
                {
                    AsnEncodedData asn = a.Values[v];

                    byte[] raw = asn.RawData;

                    Indent(sb, indent + 4).AppendLine("Value[" + v + "] len=" +
                        (raw != null ? raw.Length.ToString() : "null"));

                    //
                    // NOTE: Known decodes follow.
                    //
                    if (StringEquals(oid, "1.2.840.113549.1.9.5"))
                    {
                        //
                        // NOTE: signingTime.
                        //
                        try
                        {
                            Pkcs9SigningTime t = new Pkcs9SigningTime(raw);

                            Indent(sb, indent + 6).AppendLine(
                                "signingTime=" + t.SigningTime
                                    .ToUniversalTime().ToString("u"));
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                                TracePriority.SecurityDebug2);
                        }
                    }
                    else if (StringEquals(oid, "1.2.840.113549.1.9.4"))
                    {
                        //
                        // NOTE: messageDigest.
                        //
                        byte[] val = ExtractOctetString(raw);

                        if (val != null)
                        {
                            Indent(sb, indent + 6).AppendLine(
                                "messageDigest=" + Hex(val, 200));
                        }
                    }
                    else if (StringEquals(oid, "1.2.840.113549.1.9.6"))
                    {
                        //
                        // NOTE: counterSignature. CounterSignerInfos
                        //       are exposed on the SignerInfo already;
                        //       we printed them elsewhere.
                        //
                        Indent(sb, indent + 6).AppendLine(
                            "(PKCS#9 countersignature" + " present; see " +
                            "CounterSignerInfos " + "above)");
                    }
                    else if ((StringEquals(oid, "1.2.840.113549" +
                            ".1.9.16.2.14")) || (StringEquals(oid,
                            "1.3.6.1.4.1" + ".311.3.3.1")))
                    {
                        //
                        // NOTE: RFC3161 id-aa- signatureTimeStampToken
                        //       or Microsoft timestamp OID.  Try to decode a
                        //       nested SignedCms (timestamp token).
                        //
                        SignedCms ts;

                        if (TryDecodeCmsFromAttributeValue(raw, out ts))
                        {
                            Indent(sb, indent + 6).AppendLine(
                                "RFC3161/TST " + "token decoded:");

                            Indent(sb, indent + 8).AppendLine(
                                "Signers=" + ts.SignerInfos.Count +
                                ", Certs=" + ts.Certificates.Count);

                            //
                            // NOTE: Parse TSTInfo.
                            //
                            string algOid;
                            byte[] msg;
                            DateTimeOffset? genTime;

                            if (TryParseRfc3161TstInfo(ts.ContentInfo
                                        .Content, out algOid,
                                    out msg, out genTime))
                            {
                                Indent(sb, indent + 8).AppendLine(
                                    "TSTInfo" + ".hashAlg=" + algOid);

                                Indent(sb, indent + 8).AppendLine(
                                    "TSTInfo" + ".hashedMessage=" +
                                    (msg != null ? Hex(msg, 200) : ""));

                                Indent(sb, indent + 8).AppendLine(
                                    "TSTInfo" + ".genTime(UTC)=" +
                                    (genTime.HasValue ? genTime.Value
                                            .ToString("u") : "(null)"));
                            }

                            //
                            // NOTE: TSA signer (if present).
                            //
                            if ((ts.SignerInfos.Count
                                    > 0) && (ts.SignerInfos[0].Certificate !=
                                    null))
                            {
                                X509Certificate2 tsa = ts.SignerInfos[0]
                                        .Certificate;

                                Indent(sb, indent + 8).AppendLine(
                                    "TSA: " + tsa.Subject +
                                    "  [" + tsa.Thumbprint + "]");
                            }
                        }
                        else
                        {
                            Indent(sb, indent + 6).AppendLine(
                                "RFC3161/TST token " + "could not be decoded" +
                                " (raw first bytes): " + Hex(raw, 200));

                            //
                            // NOTE: Fallback -- try to pull TSTInfo
                            //       directly so we can at least show the time.
                            //
                            byte[] tstInfo;

                            if (TryExtractTstInfoByScan(raw, out tstInfo))
                            {
                                string alg;
                                byte[] msg;
                                DateTimeOffset? genTime;

                                if (TryParseRfc3161TstInfo(tstInfo,
                                        out alg, out msg, out genTime))
                                {
                                    Indent(sb, indent + 6)
                                        .AppendLine("TSTInfo " +
                                        "(scan) " + "parsed:");

                                    Indent(sb, indent + 8)
                                        .AppendLine("hashAlg=" + alg);

                                    Indent(sb, indent + 8)
                                        .AppendLine("hashedMessage" +
                                        "=" + (msg != null ? Hex(msg, 64)
                                            : ""));

                                    Indent(sb, indent + 8)
                                        .AppendLine("genTime" +
                                        "(UTC)=" + (genTime.HasValue ? genTime
                                                .Value.ToString(
                                                    "u") : "(null)"));
                                }
                                else
                                {
                                    Indent(sb, indent + 6)
                                        .AppendLine("TSTInfo " +
                                        "(scan) " + "present but " +
                                        "could not " + "parse.");
                                }
                            }
                        }
                    }
                    else if ((includeNested) && (StringEquals(oid,
                            "1.3.6.1.4.1" + ".311.2.4.1")))
                    {
                        //
                        // NOTE: SpcNestedSignature.
                        //
                        SignedCms nested;

                        if (TryDecodeCmsFromAttributeValue(raw, out nested))
                        {
                            Indent(sb, indent + 6).AppendLine(
                                "Nested SignedData " + "decoded:");

                            Indent(sb, indent + 8).AppendLine(
                                "Signers=" + nested.SignerInfos
                                    .Count + ", Certs=" +
                                nested.Certificates.Count);

                            DumpCmsRecursive(nested, sb,
                                indent + 8, includeNested);
                        }
                        else
                        {
                            Indent(sb, indent + 6).AppendLine(
                                "Nested SignedData " + "present but not " +
                                "decodable (raw " + "first bytes): " +
                                Hex(raw, 200));
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Unknown -- show a small hex preview.
                        //
                        if (raw != null)
                        {
                            Indent(sb, indent + 6).AppendLine(
                                "raw(0..199)=" + Hex(raw, 200));
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the requested number of space characters to the
        /// supplied builder for indentation.
        /// </summary>
        /// <param name="sb">
        /// The builder to append spaces to.
        /// </param>
        /// <param name="n">
        /// The number of space characters to append.
        /// </param>
        /// <returns>
        /// The same builder passed via <paramref name="sb" />, to allow call
        /// chaining.
        /// </returns>
        private static StringBuilder Indent(
            StringBuilder sb,
            int n
            )
        {
            for (int i = 0; i < n; i++)
                sb.Append(' ');

            return sb;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a byte array as a lowercase hexadecimal string,
        /// truncating to a maximum length and appending an ellipsis when the
        /// input is longer.
        /// </summary>
        /// <param name="b">
        /// The byte array to format.  If this parameter is null, the literal
        /// <c>(null)</c> is returned.
        /// </param>
        /// <param name="max">
        /// The maximum number of bytes to render.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the input bytes.
        /// </returns>
        private static string Hex(
            byte[] b,
            int max
            )
        {
            if (b == null) return "(null)";

            int n = b.Length < max ? b.Length : max;

            char[] c = new char[
                n * 2 + (b.Length > max ? 3 : 0)];

            int p = 0;

            for (int i = 0; i < n; i++)
            {
                byte v = b[i];
                int hi = (v >> 4) & 0xF;
                int lo = v & 0xF;

                c[p++] = (char)(hi < 10 ? '0' + hi : 'a' + (hi - 10));

                c[p++] = (char)(lo < 10 ? '0' + lo : 'a' + (lo - 10));
            }

            if (b.Length > max)
            {
                c[p++] = '.';
                c[p++] = '.';
                c[p++] = '.';
            }

            return new string(c, 0, p);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a friendly display name for a well-known object
        /// identifier (OID), or an empty string when the OID is not recognized.
        /// </summary>
        /// <param name="oid">
        /// The object identifier (OID) to translate.
        /// </param>
        /// <returns>
        /// A friendly name for the OID, or an empty string if it is unknown.
        /// </returns>
        private static string OidFriendly(
            string oid
            )
        {
            if (oid == null) return "";

            if (StringEquals(oid, "1.2.840.113549.1.7.2"))
                return "signedData";

            if (StringEquals(oid, "1.2.840.113549.1.9.4"))
                return "messageDigest";

            if (StringEquals(oid, "1.2.840.113549.1.9.5"))
                return "signingTime";

            if (StringEquals(oid, "1.2.840.113549.1.9.6"))
                return "counterSignature";

            if (StringEquals(oid, "1.2.840.113549.1.9.16.2.14"))
            {
                return "id-aa-" + "signatureTimeStampToken" + " (RFC3161)";
            }

            if (StringEquals(oid, "1.3.6.1.4.1.311.3.3.1"))
                return "MS RFC3161 timestamp";

            if (StringEquals(oid, "1.3.6.1.4.1.311.2.4.1"))
                return "SpcNestedSignature";

            return "";
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the contents of a DER OCTET STRING from the
        /// beginning of the supplied buffer.
        /// </summary>
        /// <param name="raw">
        /// The raw DER bytes that begin with an OCTET STRING.
        /// </param>
        /// <returns>
        /// The OCTET STRING contents, or null if the buffer does not begin with
        /// a well-formed OCTET STRING.
        /// </returns>
        private static byte[] ExtractOctetString(
            byte[] raw
            )
        {
            if ((raw == null) || (raw.Length < 2) || (raw[0] != 0x04))
            {
                return null;
            }

            int len, off;

            if (!DerReader.ReadLength(raw, 1, out len, out off))
            {
                return null;
            }

            if (off + len > raw.Length)
                return null;

            byte[] v = new byte[len];

            Buffer.BlockCopy(raw, off, v, 0, len);

            return v;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Fallback -- scan an attribute's RawData
        //       for a ContentInfo carrying TSTInfo and
        //       extract the inner TSTInfo DER (OCTET
        //       STRING).  This works even when SignedCms.Decode fails.
        //
        /// <summary>
        /// This method scans an attribute value for an embedded TSTInfo
        /// structure by locating the id-ct-TSTInfo OID and the enclosing
        /// explicit OCTET STRING, returning the inner TSTInfo DER.  It works
        /// even when full CMS decoding of the attribute fails.
        /// </summary>
        /// <param name="raw">
        /// The raw attribute value bytes to scan.
        /// </param>
        /// <param name="tstInfoDer">
        /// Upon success, receives the extracted TSTInfo DER bytes; upon
        /// failure, is set to null.
        /// </param>
        /// <returns>
        /// True if a TSTInfo structure was located and extracted; otherwise,
        /// false.
        /// </returns>
        private static bool TryExtractTstInfoByScan(
            byte[] raw,
            out byte[] tstInfoDer
            )
        {
            tstInfoDer = null;

            if ((raw == null) || (raw.Length < OID_TSTINFO_ENC.Length + 8))
            {
                return false;
            }

            //
            // NOTE: Step 1: Find the TSTInfo OID byte pattern anywhere in the
            //       attribute value.
            //
            int idx = -1;

            for (int i = 0;
                    i <= raw.Length -
                        OID_TSTINFO_ENC.Length;
                    i++)
            {
                bool match = true;

                for (int k = 0;
                        k < OID_TSTINFO_ENC.Length;
                        k++)
                {
                    if (raw[i + k] !=
                            OID_TSTINFO_ENC[k])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0) return false;

            //
            // NOTE: After eContentType OID, ContentInfo.encapContentInfo
            //       MUST contain eContent as [0] EXPLICIT OCTET STRING.  Scan
            //       forward for tag 0xA0 (context-specific [0]) and
            //       then for an OCTET STRING (0x04) inside.
            //
            int pos = idx + OID_TSTINFO_ENC.Length;

            //
            // NOTE: Bound the scan so we do not
            //       walk the whole blob on malformed data.
            //
            int scanEnd = Math.Min(raw.Length, pos + 4096);

            //
            // NOTE: Step 2: Find [0] EXPLICIT wrapper (tag 0xA0).
            //
            int a0Pos = -1;

            for (int p = pos; p < scanEnd; p++)
            {
                if (raw[p] == 0xA0)
                {
                    a0Pos = p;
                    break;
                }
            }

            if (a0Pos < 0) return false;

            //
            // NOTE: Read [0] length and compute its content range.
            //
            int a0Len, a0Content;

            if (!DerReader.ReadLength(raw, a0Pos + 1,
                    out a0Len, out a0Content))
            {
                return false;
            }

            int a0End = a0Content + a0Len;

            if (a0End > raw.Length)
                return false;

            //
            // NOTE: Step 3: Inside [0], the first element should be the OCTET
            //       STRING carrying TSTInfo.  Try
            //       the first element; if it is not
            //       an OCTET STRING, scan a few bytes inside for tag 0x04.
            //
            int osPos = a0Content;

            if (osPos >= a0End)
                return false;

            if (raw[osPos] != 0x04)
            {
                //
                // NOTE: Scan for an OCTET STRING tag within the [0] content.
                //
                int found = -1;

                for (int p = a0Content;
                        p < a0End; p++)
                {
                    if (raw[p] == 0x04)
                    {
                        found = p;
                        break;
                    }
                }

                if (found < 0)
                    return false;

                osPos = found;
            }

            //
            // NOTE: Read OCTET STRING length and slice out the TSTInfo DER.
            //
            int osLen, osContent;

            if (!DerReader.ReadLength(raw, osPos + 1,
                    out osLen, out osContent))
            {
                return false;
            }

            if (osContent + osLen > raw.Length)
                return false;

            tstInfoDer = new byte[osLen];

            Buffer.BlockCopy(raw, osContent, tstInfoDer, 0, osLen);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to decode a CMS message from an attribute
        /// value, trying a direct decode first and then several unwrapping
        /// strategies (OCTET STRING unwrap, SEQUENCE slice, and raw SignedData
        /// wrapping) as fallbacks.
        /// </summary>
        /// <param name="raw">
        /// The raw attribute value bytes to decode.
        /// </param>
        /// <param name="cms">
        /// Upon success, receives the decoded CMS message; upon failure, is set
        /// to null.
        /// </param>
        /// <returns>
        /// True if a CMS message was decoded; otherwise, false.
        /// </returns>
        private static bool
            TryDecodeCmsFromAttributeValue(
            byte[] raw,
            out SignedCms cms
            )
        {
            cms = null;

            if ((raw == null) || (raw.Length == 0))
                return false;

            //
            // NOTE: Step 1: Direct decode (works for most tokens).
            //
            if (TryDecodeCms(raw, out cms))
                return true;

            //
            // NOTE: Step 2: OCTET STRING unwrap (sometimes the value is an
            //       OCTET STRING containing ContentInfo).
            //
            if (raw[0] == 0x04)
            {
                int osLen, osContent;

                if ((DerReader.ReadLength(raw, 1, out osLen, out osContent)) &&
                    (osContent + osLen <= raw.Length))
                {
                    byte[] inner = new byte[osLen];

                    Buffer.BlockCopy(raw, osContent, inner, 0, osLen);

                    if (TryDecodeCms(inner, out cms))
                    {
                        return true;
                    }

                    byte[] wrapped;

                    if ((TryWrapSignedDataAsContentInfo(inner, out wrapped)) &&
                        (TryDecodeCms(wrapped, out cms)))
                    {
                        return true;
                    }
                }
            }

            //
            // NOTE: Step 3: SEQUENCE wrapper case -- copy the ENTIRE SEQUENCE
            //       (tag + len + content).
            //
            if (raw[0] == 0x30)
            {
                //
                // NOTE: Use a DerReader just to compute the correct slice
                //       for the outer SEQUENCE.
                //
                DerReader r = new DerReader(raw);
                int seqStart, seqTotal;

                if (r.TryReadRawSequenceFull(out seqStart, out seqTotal))
                {
                    byte[] contentInfo = new byte[seqTotal];

                    Buffer.BlockCopy(raw, seqStart, contentInfo, 0, seqTotal);

                    //
                    // NOTE: Try decode this ContentInfo.
                    //
                    if (TryDecodeCms(contentInfo, out cms))
                    {
                        return true;
                    }

                    //
                    // NOTE: If it is actually raw SignedData inside, try
                    //       wrapping it.
                    //
                    byte[] wrapped2;

                    if ((TryWrapSignedDataAsContentInfo(
                            contentInfo,
                            out wrapped2)) &&
                        (TryDecodeCms(
                            wrapped2, out cms)))
                    {
                        return true;
                    }
                }
            }

            //
            // NOTE: Step 4: Last resort -- assume raw SignedData; wrap into
            //       ContentInfo and decode.
            //
            byte[] wrapped3;

            if ((TryWrapSignedDataAsContentInfo(raw, out wrapped3)) &&
                (TryDecodeCms(wrapped3, out cms)))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Extract TSTInfo OCTET STRING from a
        //       Microsoft RFC3161 attribute value.
        //       Returns true and fills tstInfoDer if the
        //       structure is: ContentInfo { oid signedData,
        //       [0] SignedData } with
        //       SignedData.encapContentInfo.eContentType =
        //       id-ct-TSTInfo and eContent = [0] EXPLICIT OCTET STRING.
        //
        /// <summary>
        /// This method extracts the inner TSTInfo OCTET STRING from a Microsoft
        /// RFC 3161 attribute value by walking the ContentInfo and SignedData
        /// structure down to the encapsulated content.
        /// </summary>
        /// <param name="raw">
        /// The raw Microsoft RFC 3161 attribute value bytes.
        /// </param>
        /// <param name="tstInfoDer">
        /// Upon success, receives the extracted TSTInfo DER bytes; upon
        /// failure, is set to null.
        /// </param>
        /// <returns>
        /// True if the TSTInfo structure was located and extracted; otherwise,
        /// false.
        /// </returns>
        private static bool
            TryExtractTstInfoFromMsRfc3161(
            byte[] raw,
            out byte[] tstInfoDer
            )
        {
            tstInfoDer = null;

            if ((raw == null) || (raw.Length < 2) || (raw[0] != 0x30))
            {
                return false;
            }

            int ciLen, ciContent;

            if (!DerReader.ReadLength(raw, 1, out ciLen, out ciContent))
            {
                return false;
            }

            int ciEnd = ciContent + ciLen;
            int p = ciContent;

            //
            // NOTE: Optional leading OID (contentType).
            //
            if ((p < ciEnd) && (raw[p] == 0x06))
            {
                p++;
                int ctLen, ctContent;

                if (!DerReader.ReadLength(raw, p, out ctLen, out ctContent))
                {
                    return false;
                }

                p = ctContent + ctLen;
            }

            //
            // NOTE: Expect [0] EXPLICIT SignedData.
            //
            if (!((p < ciEnd) && (raw[p] == 0xA0)))
                return false;

            p++;
            int a0Len, a0Content;

            if (!DerReader.ReadLength(raw, p, out a0Len, out a0Content))
            {
                return false;
            }

            int a0End = a0Content + a0Len;
            int q = a0Content;

            //
            // NOTE: SignedData ::= SEQUENCE { version INTEGER,
            //       digestAlgorithms SET, encapContentInfo SEQUENCE, ... }
            //
            if (!((q < a0End) && (raw[q] == 0x30)))
                return false;

            q++;
            int sdLen, sdContent;

            if (!DerReader.ReadLength(raw, q, out sdLen, out sdContent))
            {
                return false;
            }

            int sdEnd = sdContent + sdLen;
            q = sdContent;

            //
            // NOTE: version INTEGER.
            //
            if (!((q < sdEnd) && (raw[q] == 0x02)))
                return false;

            q++;
            int vLen, vContent;

            if (!DerReader.ReadLength(raw, q, out vLen, out vContent))
            {
                return false;
            }

            q = vContent + vLen;

            //
            // NOTE: digestAlgorithms SET.
            //
            if (!((q < sdEnd) && (raw[q] == 0x31)))
                return false;

            q++;
            int daLen, daContent;

            if (!DerReader.ReadLength(raw, q, out daLen, out daContent))
            {
                return false;
            }

            q = daContent + daLen;

            //
            // NOTE: encapContentInfo SEQUENCE.
            //
            if (!((q < sdEnd) && (raw[q] == 0x30)))
                return false;

            q++;
            int eciLen, eciContent;

            if (!DerReader.ReadLength(raw, q, out eciLen, out eciContent))
            {
                return false;
            }

            int eciEnd = eciContent + eciLen;
            int r = eciContent;

            //
            // NOTE: eContentType OID (should be id-ct-TSTInfo:
            //       1.2.840.113549.1.9.16.1.4).
            //
            if (!((r < eciEnd) && (raw[r] == 0x06)))
                return false;

            r++;
            int oidLen, oidContent;

            if (!DerReader.ReadLength(raw, r, out oidLen, out oidContent))
            {
                return false;
            }

            r = oidContent + oidLen;

            //
            // NOTE: eContent [0] EXPLICIT OCTET STRING.
            //
            if (!((r < eciEnd) && (raw[r] == 0xA0)))
                return false;

            r++;
            int ecLen, ecContent;

            if (!DerReader.ReadLength(raw, r, out ecLen, out ecContent))
            {
                return false;
            }

            int s = ecContent;

            //
            // NOTE: OCTET STRING.
            //
            if (!((s < eciEnd) && (raw[s] == 0x04)))
                return false;

            s++;
            int osLen, osContent;

            if (!DerReader.ReadLength(raw, s, out osLen, out osContent))
            {
                return false;
            }

            if (osContent + osLen > raw.Length)
                return false;

            tstInfoDer = new byte[osLen];

            Buffer.BlockCopy(raw, osContent, tstInfoDer, 0, osLen);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to decode a CMS message directly from the
        /// supplied DER bytes.
        /// </summary>
        /// <param name="der">
        /// The DER-encoded ContentInfo bytes to decode.
        /// </param>
        /// <param name="cms">
        /// Upon success, receives the decoded CMS message; upon failure, is set
        /// to null.
        /// </param>
        /// <returns>
        /// True if the CMS message was decoded; otherwise, false.
        /// </returns>
        private static bool TryDecodeCms(
            byte[] der,
            out SignedCms cms
            )
        {
            cms = null;

            try
            {
                SignedCms s = new SignedCms();
                s.Decode(der);
                cms = s;
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Build: SEQUENCE { OID signedData (06 09 2A864886F70D010702),
        //       [0] EXPLICIT <signedData> }
        //
        /// <summary>
        /// This method wraps raw SignedData DER in a ContentInfo SEQUENCE
        /// carrying the signedData object identifier (OID), so it can be decoded
        /// as a CMS message.
        /// </summary>
        /// <param name="signedData">
        /// The raw SignedData DER bytes to wrap.
        /// </param>
        /// <param name="contentInfo">
        /// Upon success, receives the constructed ContentInfo DER bytes; upon
        /// failure, is set to null.
        /// </param>
        /// <returns>
        /// True if the ContentInfo was constructed; otherwise, false.
        /// </returns>
        private static bool
            TryWrapSignedDataAsContentInfo(
            byte[] signedData,
            out byte[] contentInfo
            )
        {
            contentInfo = null;

            if ((signedData == null) || (signedData.Length == 0))
            {
                return false;
            }

            //
            // NOTE: Quick sanity check -- a DER SEQUENCE starts with 0x30.
            //
            if (signedData[0] != 0x30)
                return false;

            //
            // NOTE: 1.2.840.113549.1.7.2
            //
            byte[] oidSignedData = new byte[] {
                0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07,
                0x02
            };

            //
            // NOTE: [0] EXPLICIT wrapper for the ANY content.
            //
            byte[] a0Len = EncodeDerLength(signedData.Length);

            byte[] a0 = new byte[
                1 + a0Len.Length + signedData.Length];

            a0[0] = 0xA0;

            Buffer.BlockCopy(a0Len, 0, a0, 1, a0Len.Length);

            Buffer.BlockCopy(signedData, 0, a0,
                1 + a0Len.Length, signedData.Length);

            //
            // NOTE: SEQUENCE { oid, [0] content }
            //
            int innerLen = oidSignedData.Length + a0.Length;

            byte[] seqLen = EncodeDerLength(innerLen);

            contentInfo = new byte[
                1 + seqLen.Length + innerLen];

            int p = 0;
            contentInfo[p++] = 0x30;

            Buffer.BlockCopy(seqLen, 0, contentInfo, p, seqLen.Length);

            p += seqLen.Length;

            Buffer.BlockCopy(oidSignedData, 0, contentInfo, p,
                oidSignedData.Length);

            p += oidSignedData.Length;

            Buffer.BlockCopy(a0, 0, contentInfo, p, a0.Length);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method encodes a length value using the DER definite-length
        /// form, producing either the short form or the appropriate long form.
        /// </summary>
        /// <param name="len">
        /// The length value to encode.
        /// </param>
        /// <returns>
        /// The DER-encoded length bytes.
        /// </returns>
        private static byte[] EncodeDerLength(
            int len
            )
        {
            if (len < 0x80)
                return new byte[] { (byte)len };

            //
            // NOTE: Up to 4 bytes length is enough here.
            //
            byte b3 = (byte)((len >> 24) & 0xFF);
            byte b2 = (byte)((len >> 16) & 0xFF);
            byte b1 = (byte)((len >> 8) & 0xFF);
            byte b0 = (byte)(len & 0xFF);

            if (b3 != 0)
            {
                return new byte[] {
                    0x84, b3, b2, b1, b0
                };
            }

            if (b2 != 0)
            {
                return new byte[] {
                    0x83, b2, b1, b0
                };
            }

            if (b1 != 0)
                return new byte[] { 0x82, b1, b0 };

            return new byte[] { 0x81, b0 };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches all signers (and nested signatures) of a CMS
        /// message for the best available timestamp, preferring a fully valid
        /// timestamp over a partial one.
        /// </summary>
        /// <param name="cms">
        /// The CMS message to search.
        /// </param>
        /// <param name="options">
        /// The verification options controlling timestamp processing.
        /// </param>
        /// <returns>
        /// The best <see cref="TimestampInfo" /> found, which will report not
        /// present if no timestamp was discovered.
        /// </returns>
        private static TimestampInfo
            FindBestTimestampAcrossSigners(
            SignedCms cms,
            VerificationOptions options
            )
        {
            //
            // NOTE: Avoid cycles via visited set.
            //
            HashSet<string> visitedCmsThumbs = new HashSet<string>(
                    StringComparer.Ordinal);

            return FindBestTimestampRecursive(cms, options, 0,
                visitedCmsThumbs);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a timestamp is fully valid: present,
        /// carrying a time, bound to the signer signature, cryptographically
        /// valid, and (when required) chain-valid.
        /// </summary>
        /// <param name="t">
        /// The timestamp information to evaluate.
        /// </param>
        /// <param name="options">
        /// The verification options controlling timestamp-chain validation.
        /// </param>
        /// <returns>
        /// True if the timestamp is fully valid; otherwise, false.
        /// </returns>
        private static bool IsFullyValidTs(
            TimestampInfo t,
            VerificationOptions options
            )
        {
            if ((t == null) || (!t.Present))
                return false;

            if (!t.TimeUtc.HasValue)
                return false;

            if (!t.BoundToSignerSignature)
                return false;

            if (!t.CryptographicallyValid)
                return false;

            if ((options.ValidateTimestampChain) && (!t.ChainValid))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Prefer fully-valid > has time+binding > just present.
        //
        /// <summary>
        /// This method selects the better of two timestamp candidates,
        /// preferring a fully valid timestamp, then one with binding and time,
        /// and finally one that at least carries a time.
        /// </summary>
        /// <param name="current">
        /// The current best timestamp candidate.
        /// </param>
        /// <param name="candidate">
        /// The new timestamp candidate to compare against the current best.
        /// </param>
        /// <returns>
        /// Whichever of the two candidates is considered better.
        /// </returns>
        private static TimestampInfo PickBetter(
            TimestampInfo current,
            TimestampInfo candidate
            )
        {
            if ((candidate == null) || (!candidate.Present))
            {
                return current;
            }

            bool currPresent = (current != null) && (current.Present);

            if (!currPresent)
                return candidate;

            bool candFull =
                (candidate.CryptographicallyValid) &&
                (candidate.BoundToSignerSignature) &&
                (candidate.TimeUtc.HasValue);

            bool currFull = (current.CryptographicallyValid) &&
                (current.BoundToSignerSignature) && (current.TimeUtc.HasValue);

            if ((candFull) && (!currFull))
                return candidate;

            if (candFull == currFull)
            {
                //
                // NOTE: If both same class, prefer the one that at least has
                //       Time.
                //
                if ((candidate.TimeUtc.HasValue) &&
                    (!current.TimeUtc.HasValue))
                {
                    return candidate;
                }
            }

            return current;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method recursively searches a CMS message and its nested
        /// signatures for the best available timestamp, returning immediately
        /// when a fully valid timestamp is found and otherwise tracking the best
        /// partial candidate.
        /// </summary>
        /// <param name="cms">
        /// The CMS message to search.
        /// </param>
        /// <param name="options">
        /// The verification options controlling timestamp processing.
        /// </param>
        /// <param name="depth">
        /// The current recursion depth, used to bound nested-signature
        /// traversal.
        /// </param>
        /// <param name="visited">
        /// The set of already-visited nested signer thumbprints, used to avoid
        /// cycles.
        /// </param>
        /// <returns>
        /// The best <see cref="TimestampInfo" /> found at this level or below.
        /// </returns>
        private static TimestampInfo
            FindBestTimestampRecursive(
            SignedCms cms,
            VerificationOptions options,
            int depth,
            HashSet<string> visited
            )
        {
            //
            // NOTE: Safety limit on recursion depth.
            //
            if ((cms == null) || (depth > 4))
                return new TimestampInfo();

            TimestampInfo best = new TimestampInfo();

            //
            // NOTE: Step 1: Scan all signers at this level.
            //
            for (int s = 0;
                    s < cms.SignerInfos.Count; s++)
            {
                SignerInfo si = cms.SignerInfos[s];

                TimestampInfo rfc;

                bool foundRfc = TryFindRfc3161OnSigner(
                        si, cms, options, out rfc);

                TimestampInfo cs = new TimestampInfo();

                bool foundCs = TryVerifyCounterSignatureTimestamp(
                        si, cms, options, cs);

                //
                // NOTE: If we have a fully valid timestamp, return it
                //       immediately.
                //
                if ((foundRfc) && (IsFullyValidTs(rfc, options)))
                {
                    return rfc;
                }

                if ((foundCs) && (IsFullyValidTs(cs, options)))
                {
                    return cs;
                }

                //
                // NOTE: Otherwise, keep the better partial candidate.
                //
                if (foundRfc)
                    best = PickBetter(best, rfc);

                if (foundCs)
                    best = PickBetter(best, cs);
            }

            //
            // NOTE: Step 2: Recurse into nested signatures.
            //
            for (int s = 0;
                    s < cms.SignerInfos.Count; s++)
            {
                SignerInfo si = cms.SignerInfos[s];

                CryptographicAttributeObjectCollection
                    ua = si.UnsignedAttributes;

                if (ua == null)
                    continue;

                for (int i = 0;
                        i < ua.Count; i++)
                {
                    CryptographicAttributeObject
                        attr = ua[i];

                    if (attr.Oid == null)
                        continue;

                    if (!StringEquals(attr.Oid.Value, OID_MS_NESTED_SIG))
                    {
                        continue;
                    }

                    if (attr.Values == null)
                        continue;

                    for (int v = 0;
                            v < attr.Values.Count;
                            v++)
                    {
                        byte[] val = attr.Values[v].RawData;

                        if ((val == null) || (val.Length == 0))
                        {
                            continue;
                        }

                        SignedCms nested;

                        if (TryDecodeCmsFromAttributeValue(val, out nested))
                        {
                            string mark = null;

                            if ((nested.SignerInfos.Count > 0) &&
                                (nested.SignerInfos[0].Certificate !=
                                    null))
                            {
                                mark = nested.SignerInfos[0]
                                    .Certificate.Thumbprint;
                            }

                            if (mark != null)
                            {
                                if (visited.Contains(mark))
                                {
                                    continue;
                                }

                                visited.Add(mark);
                            }

                            TimestampInfo nestedTs =
                                FindBestTimestampRecursive(nested, options,
                                    depth + 1, visited);

                            if (IsFullyValidTs(nestedTs, options))
                            {
                                return nestedTs;
                            }

                            best = PickBetter(best, nestedTs);
                        }
                    }
                }
            }

            //
            // NOTE: Step 3: If nothing fully valid was found, return the best
            //       partial (so Time shows up).
            //
            return best;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches a signer's unsigned attributes for an RFC 3161
        /// timestamp token, verifying it cryptographically, parsing its TSTInfo,
        /// confirming its binding to the signer signature, and (optionally)
        /// validating the timestamping authority (TSA) chain, with a byte-scan
        /// fallback when full decoding fails.
        /// </summary>
        /// <param name="signer">
        /// The signer whose unsigned attributes are searched.
        /// </param>
        /// <param name="rootCms">
        /// The CMS message that contains the signer and its certificates.
        /// </param>
        /// <param name="options">
        /// The verification options controlling timestamp processing.
        /// </param>
        /// <param name="info">
        /// Upon return, receives the timestamp information populated for the
        /// signer.
        /// </param>
        /// <returns>
        /// True if an RFC 3161 timestamp token was found; otherwise, false.
        /// </returns>
        private static bool TryFindRfc3161OnSigner(
            SignerInfo signer,
            SignedCms rootCms,
            VerificationOptions options,
            out TimestampInfo info
            )
        {
            info = new TimestampInfo();

            if (signer == null)
                return false;

            CryptographicAttributeObjectCollection
                ua = signer.UnsignedAttributes;

            if ((ua == null) || (ua.Count == 0))
                return false;

            for (int i = 0; i < ua.Count; i++)
            {
                CryptographicAttributeObject attr = ua[i];

                if (attr.Oid == null)
                    continue;

                string oid = attr.Oid.Value;

                //
                // NOTE: Standard + Microsoft RFC3161 attribute OIDs.
                //
                if ((!StringEquals(oid, "1.2.840.113549" +
                        ".1.9.16.2.14")) && (!StringEquals(oid,
                        "1.3.6.1.4.1.311.3.3.1")))
                {
                    continue;
                }

                if ((attr.Values == null) || (attr.Values.Count == 0))
                {
                    continue;
                }

                byte[] raw = attr.Values[0].RawData;

                //
                // NOTE: Path A -- full CMS decode for cryptographic / chain
                //       validation.
                //
                SignedCms tsCms;

                if (TryDecodeCmsFromAttributeValue(raw, out tsCms))
                {
                    info = new TimestampInfo {
                        Present = true,
                        IsRfc3161 = true
                    };

                    try
                    {
                        tsCms.CheckSignature(true);

                        info.CryptographicallyValid = true;
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                            TracePriority.SecurityError);

                        info.CryptographicallyValid = false;
                    }

                    string algOid;
                    byte[] msg;
                    DateTimeOffset? genTime;

                    if (TryParseRfc3161TstInfo(tsCms.ContentInfo.Content,
                            out algOid, out msg, out genTime))
                    {
                        info.TimeUtc = genTime;

                        try
                        {
                            byte[] parentSig = GetSignerSignatureBytes(
                                    signer, rootCms);

                            HashAlgorithm h = CreateHashFromOid(algOid);

                            using (h)
                            {
                                if (h != null)
                                {
                                    byte[] dig = h.ComputeHash(parentSig);

                                    info.BoundToSignerSignature =
                                        ((msg != null) &&
                                        (BytesEqual(
                                            dig, msg)));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                                TracePriority.SecurityError);
                        }
                    }

                    //
                    // NOTE: Optional TSA chain.
                    //
                    if ((options.ValidateTimestampChain) &&
                        (tsCms.SignerInfos.Count > 0) && (tsCms.SignerInfos[0]
                            .Certificate != null))
                    {
                        X509Certificate2 tsa = tsCms.SignerInfos[0]
                                .Certificate;

                        X509Certificate2Collection
                            extra = new X509Certificate2Collection();

                        for (int c = 0;
                                c < tsCms.Certificates.Count;
                                c++)
                        {
                            if (!CertEqualThumbprint(tsCms
                                        .Certificates[c], tsa))
                            {
                                extra.Add(tsCms.Certificates[c]);
                            }
                        }

                        X509Certificate2[]
                            customRoots = LoadCertificatesFromDirectory(
                                options.CustomRootDirectory);

                        X509Certificate2[]
                            extraFromDir = LoadCertificatesFromDirectory(
                                options.AdditionalIntermediatesDirectory);

                        for (int e = 0;
                                (extraFromDir != null) && (e < extraFromDir
                                    .Length);
                                e++)
                        {
                            extra.Add(extraFromDir[e]);
                        }

                        string[] st;

                        bool ok = BuildChainWithAutoAia(
                                tsa, extra, info.TimeUtc,
                                options, customRoots, options.RequireTsaEku,
                                out st);

                        info.ChainValid = ok;
                        info.ChainStatus = st;
                        info.TsaSubject = tsa.Subject;

                        info.TsaThumbprint = tsa.Thumbprint;
                    }
                    else
                    {
                        info.ChainValid = !options.ValidateTimestampChain;
                    }

                    //
                    // NOTE: Found a token via the decode path.
                    //
                    return true;
                }

                //
                // NOTE: Path B -- decoder failed; scan/extract TSTInfo so we
                //       still get Time + Binding. The byte-scan fallback is
                //       fragile and can be disabled
                //       via options.  It is gated to
                //       prevent a crafted timestamp blob from being
                //       misinterpreted.
                //
                byte[] tstInfo;

                if ((options.AllowTstInfoScanFallback) &&
                    (TryExtractTstInfoByScan(raw, out tstInfo)))
                {
                    info = new TimestampInfo {
                        Present = true,
                        IsRfc3161 = true,
                        CryptographicallyValid = false
                    };

                    string algOid;
                    byte[] msg;
                    DateTimeOffset? genTime;

                    if (TryParseRfc3161TstInfo(tstInfo, out algOid,
                            out msg, out genTime))
                    {
                        info.TimeUtc = genTime;

                        try
                        {
                            byte[] parentSig = GetSignerSignatureBytes(
                                    signer, rootCms);

                            HashAlgorithm h = CreateHashFromOid(algOid);

                            using (h)
                            {
                                if (h != null)
                                {
                                    byte[] dig = h.ComputeHash(parentSig);

                                    info.BoundToSignerSignature =
                                        ((msg != null) &&
                                        (BytesEqual(
                                            dig, msg)));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                                TracePriority.SecurityError);
                        }
                    }

                    //
                    // NOTE: No TSA chain possible without a decoded CMS --
                    //       leave ChainValid based on policy.
                    //
                    info.ChainValid = !options.ValidateTimestampChain;

                    //
                    // NOTE: Found via scan.
                    //
                    return true;
                }

                //
                // NOTE: Continue loop in case there are multiple timestamp
                //       attributes (rare).
                //
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Chain Build With Custom Trust & Revocation
        /// <summary>
        /// This method builds and validates a certificate chain for the given
        /// leaf certificate, applying revocation, verification-time, extended
        /// key usage (EKU), extra-store, and custom-root-trust policy, with a
        /// compatibility fallback for runtimes that cannot apply custom root
        /// trust directly.
        /// </summary>
        /// <param name="leaf">
        /// The leaf certificate to build the chain for.
        /// </param>
        /// <param name="extra">
        /// Additional certificates to add to the chain's extra store.
        /// </param>
        /// <param name="verificationTime">
        /// The time at which to evaluate certificate validity, or null to use
        /// the current time.
        /// </param>
        /// <param name="options">
        /// The verification options controlling chain-building policy.
        /// </param>
        /// <param name="customRoots">
        /// The custom root certificates to trust when custom root trust is
        /// enabled.
        /// </param>
        /// <param name="requireTimeStampingEku">
        /// When true, the timeStamping extended key usage (EKU) is required;
        /// otherwise, the code-signing EKU is required.
        /// </param>
        /// <param name="statusStrings">
        /// Upon return, receives the chain status strings produced by the build.
        /// </param>
        /// <returns>
        /// True if the chain was built and validated successfully; otherwise,
        /// false.
        /// </returns>
        private static bool BuildChainWithOptions(
            X509Certificate2 leaf,
            X509Certificate2Collection extra,
            DateTimeOffset? verificationTime,
            VerificationOptions options,
            X509Certificate2[] customRoots,
            bool requireTimeStampingEku,
            out string[] statusStrings
            )
        {
            using (X509Chain chain = new X509Chain())
            {
                //
                // NOTE: Policy settings.
                //
                chain.ChainPolicy.RevocationMode = options.RevocationMode;

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

                chain.ChainPolicy.UrlRetrievalTimeout =
                    options.RevocationUrlRetrievalTimeout;

                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                if (verificationTime.HasValue)
                {
                    chain.ChainPolicy.VerificationTime = verificationTime.Value
                            .UtcDateTime;
                }

                //
                // NOTE: EKU constraints -- for code signing or TSA depending on
                //       use-case.
                //
                if (requireTimeStampingEku)
                {
                    chain.ChainPolicy.ApplicationPolicy.Add(new Oid(
                            "1.3.6.1.5.5.7.3.8"));
                }
                else
                {
                    chain.ChainPolicy.ApplicationPolicy.Add(new Oid(
                            "1.3.6.1.5.5.7.3.3"));
                }

                if ((extra != null) && (extra.Count > 0))
                {
                    chain.ChainPolicy.ExtraStore.AddRange(extra);
                }

                //
                // NOTE: Try to enable Custom Root Trust
                //       on capable runtimes (.NET 5+).
                //
                bool customApplied = false;

                if ((options.UseCustomRootTrust) && (customRoots != null) &&
                    (customRoots.Length > 0))
                {
                    try
                    {
                        //
                        // NOTE: Reflection to avoid compile-time dependency
                        //       (still compiles on netstandard2.0).
                        //
                        X509ChainPolicy policy = chain.ChainPolicy;

                        System.Reflection.PropertyInfo
                            trustModeProp = policy.GetType().GetProperty(
                                "TrustMode");

                        System.Reflection.PropertyInfo
                            customTrustStoreProp = policy.GetType().GetProperty(
                                "CustomTrustStore");

                        if ((trustModeProp != null) &&
                            (customTrustStoreProp != null))
                        {
                            Type x509ChainTrustModeType =
                                trustModeProp.PropertyType;

                            object customRootTrustEnum = Enum.Parse(
                                    x509ChainTrustModeType, "CustomRootTrust");

                            trustModeProp.SetValue(policy,
                                customRootTrustEnum, null);

                            X509Certificate2Collection
                                customStore = (X509Certificate2Collection)
                                customTrustStoreProp.GetValue(policy, null);

                            for (int i = 0;
                                    i < customRoots.Length;
                                    i++)
                            {
                                customStore.Add(customRoots[i]);
                            }

                            customApplied = true;
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                            TracePriority.SecurityError);

                        customApplied = false;
                    }
                }

                //
                // NOTE: Build the chain.
                //
                bool ok = chain.Build(leaf);

                //
                // NOTE: Convert statuses.
                //
                List<string> st = new List<string>();

                for (int i = 0;
                        i < chain.ChainStatus.Length;
                        i++)
                {
                    X509ChainStatus s = chain.ChainStatus[i];

                    string line = s.Status.ToString();

                    if (!string.IsNullOrEmpty(s.StatusInformation))
                    {
                        line += ": " + s.StatusInformation.Trim();
                    }

                    st.Add(line);
                }

                statusStrings = st.ToArray();

                //
                // NOTE: Compatibility fallback for older
                //       runtimes if only UntrustedRoot
                //       and top matches our custom root.
                //
                if ((!ok) &&
                    (options.UseCustomRootTrust) &&
                    (!customApplied) &&
                    (options
                        .TrustIfOnlyUntrustedRootAndMatchesCustomRoot))
                {
                    bool onlyUntrustedRoot = true;

                    for (int i = 0;
                            i < chain.ChainStatus.Length;
                            i++)
                    {
                        X509ChainStatusFlags s = chain.ChainStatus[i].Status;

                        if ((s != X509ChainStatusFlags.UntrustedRoot) &&
                            (s != X509ChainStatusFlags.NoError))
                        {
                            onlyUntrustedRoot = false;
                            break;
                        }
                    }

                    if ((onlyUntrustedRoot) && (chain.ChainElements.Count > 0))
                    {
                        X509Certificate2 root = chain.ChainElements[
                                chain.ChainElements.Count - 1].Certificate;

                        if (IsInCollectionByThumbprint(customRoots, root))
                        {
                            //
                            // NOTE: Treat as trusted via provided roots.
                            //
                            ok = true;
                        }
                    }
                }

                return ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a certificate chain for the given leaf, and when
        /// the initial build fails and AIA auto-download is enabled, repeatedly
        /// downloads missing intermediate certificates by following Authority
        /// Information Access (AIA) caIssuers URLs and retries up to the
        /// configured depth.
        /// </summary>
        /// <param name="leaf">
        /// The leaf certificate to build the chain for.
        /// </param>
        /// <param name="extra">
        /// Additional certificates to add to the chain's extra store; newly
        /// downloaded intermediates are added to this collection during the
        /// build.
        /// </param>
        /// <param name="verificationTime">
        /// The time at which to evaluate certificate validity, or null to use
        /// the current time.
        /// </param>
        /// <param name="options">
        /// The verification options controlling chain-building and AIA download
        /// policy.
        /// </param>
        /// <param name="customRoots">
        /// The custom root certificates to trust when custom root trust is
        /// enabled.
        /// </param>
        /// <param name="requireTimeStampingEku">
        /// When true, the timeStamping extended key usage (EKU) is required;
        /// otherwise, the code-signing EKU is required.
        /// </param>
        /// <param name="statusStrings">
        /// Upon return, receives the chain status strings produced by the final
        /// build attempt.
        /// </param>
        /// <returns>
        /// True if the chain was built and validated successfully; otherwise,
        /// false.
        /// </returns>
        private static bool BuildChainWithAutoAia(
            X509Certificate2 leaf,
            X509Certificate2Collection extra,
            DateTimeOffset? verificationTime,
            VerificationOptions options,
            X509Certificate2[] customRoots,
            bool requireTimeStampingEku,
            out string[] statusStrings
            )
        {
            //
            // NOTE: Step 1: Try once with what we already have.
            //
            bool ok = BuildChainWithOptions(leaf, extra, verificationTime,
                options, customRoots, requireTimeStampingEku,
                out statusStrings);

            if ((ok) || (!options.AutoDownloadIntermediates))
            {
                return ok;
            }

            //
            // NOTE: Step 2: AIA-chase loop -- download intermediates and
            //       retry up to AiaMaxDepth times.
            //
            HashSet<string> visitedThumbprints = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            HashSet<string> visitedUrls = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            //
            // NOTE: Seed visited with what we already have to avoid loops.
            //
            visitedThumbprints.Add(leaf.Thumbprint);

            for (int i = 0; i < extra.Count; i++)
            {
                visitedThumbprints.Add(extra[i].Thumbprint);
            }

            List<X509Certificate2> downloaded =
                new List<X509Certificate2>();

            try
            {
                int depth = 0;

                while (depth < options.AiaMaxDepth)
                {
                    depth++;

                    //
                    // NOTE: Find certs currently in the
                    //       partial chain that have AIA
                    //       "caIssuers" URIs we have not
                    //       tried yet.  Rebuild to get the latest ChainElements
                    //       for inspection.
                    //
                    string[] ignore;

                    BuildChainWithOptions(leaf, extra, verificationTime,
                        options, customRoots, requireTimeStampingEku, out ignore);

                    //
                    // NOTE: Gather candidates from leaf + all extras (simple and
                    //       robust).
                    //
                    List<string> urls = new List<string>();

                    GatherAiaUrls(leaf, urls, visitedUrls);

                    for (int i = 0;
                            i < extra.Count; i++)
                    {
                        GatherAiaUrls(extra[i], urls, visitedUrls);
                    }

                    //
                    // NOTE: Nothing left to try.
                    //
                    if (urls.Count == 0)
                        break;

                    //
                    // NOTE: Try to download any new certs and add to extra.
                    //
                    int added = 0;

                    for (int u = 0;
                            u < urls.Count; u++)
                    {
                        string uri = urls[u];

                        X509Certificate2[] fromUrl = DownloadCertificatesFromAia(
                                uri, options);

                        if (fromUrl == null)
                            continue;

                        for (int c = 0;
                                c < fromUrl.Length; c++)
                        {
                            X509Certificate2 cert = fromUrl[c];

                            if (cert == null)
                                continue;

                            downloaded.Add(cert);

                            if (visitedThumbprints.Contains(cert.Thumbprint))
                            {
                                continue;
                            }

                            //
                            // NOTE: Avoid adding the leaf itself again.
                            //
                            if (!StringEquals(cert.Thumbprint, leaf.Thumbprint))
                            {
                                extra.Add(cert);

                                visitedThumbprints.Add(cert.Thumbprint);

                                added++;
                            }
                        }
                    }

                    //
                    // NOTE: No progress.
                    //
                    if (added == 0)
                        break;

                    //
                    // NOTE: Retry build with the newly added intermediates.
                    //
                    ok = BuildChainWithOptions(leaf, extra, verificationTime,
                        options, customRoots, requireTimeStampingEku,
                        out statusStrings);

                    if (ok) return true;
                }

                //
                // NOTE: Final attempt result is already
                //       in statusStrings from the last build.
                //
                return false;
            }
            finally
            {
                //
                // BUGFIX: dispose the intermediate certificates downloaded
                //         via AIA above.  They were added to the caller-
                //         owned "extra" collection and used across the chain
                //         builds, but the caller does not use that collection
                //         after this method returns.
                //
                for (int i = 0; i < downloaded.Count; i++)
                {
                    try { downloaded[i].Dispose(); }
                    catch { /* best effort */ }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region PE Parsing and Authenticode Hashing
        /// <summary>
        /// This class captures the parsed layout of a portable executable (PE)
        /// file, recording the offsets and sizes needed to compute the
        /// Authenticode hash and to locate the attribute certificate table.
        /// </summary>
        [ObjectId("f776625a-56e5-4784-a404-7355ce61dc83")]
        private sealed class PeLayout
        {
            /// <summary>
            /// The file offset of the start of the PE optional header.
            /// </summary>
            public long OptionalHeaderOffset;
            /// <summary>
            /// When true, the image uses the PE32+ (64-bit) optional header
            /// format; otherwise, it uses the PE32 (32-bit) format.
            /// </summary>
            public bool Pe32Plus;
            /// <summary>
            /// The file offset of the optional header CheckSum field.
            /// </summary>
            public long ChecksumFieldOffset;
            /// <summary>
            /// The file offset of the security (certificate table) data
            /// directory entry.
            /// </summary>
            public long SecurityDirEntryOffset;
            /// <summary>
            /// The size, in bytes, of all PE headers.
            /// </summary>
            public uint SizeOfHeaders;
            /// <summary>
            /// The number of sections in the image.
            /// </summary>
            public ushort NumberOfSections;
            /// <summary>
            /// The size, in bytes, of the optional header.
            /// </summary>
            public ushort SizeOfOptionalHeader;

            //
            // NOTE: File offset to first IMAGE_SECTION_HEADER.
            //
            /// <summary>
            /// The file offset of the first section header in the section table.
            /// </summary>
            public long SectionTableOffset;

            /// <summary>
            /// The file offset of the attribute certificate table.
            /// </summary>
            public long CertTableOffset;
            /// <summary>
            /// The size, in bytes, of the attribute certificate table.
            /// </summary>
            public long CertTableSize;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads and validates the layout of a portable executable
        /// (PE) file from the supplied stream, returning the offsets and sizes
        /// needed for Authenticode hashing and signature location.
        /// </summary>
        /// <param name="fs">
        /// The file stream positioned over the PE file to parse.
        /// </param>
        /// <returns>
        /// A <see cref="PeLayout" /> describing the parsed PE file.
        /// </returns>
        private static PeLayout ReadPeLayout(
            FileStream fs
            )
        {
            BinaryReader br = new BinaryReader(fs, Encoding.UTF8, true);

            using (br)
            {
                //
                // NOTE: A valid PE file must be at least
                //       large enough to contain the DOS
                //       header (64 bytes) plus the PE
                //       signature and COFF header.
                //
                if (fs.Length < 0x40 + 4 + 20)
                    throw new InvalidDataException(
                        "File too small to be a valid PE.");

                fs.Position = 0x3C;
                uint peHeaderOffset = br.ReadUInt32();
                if ((peHeaderOffset == 0) ||
                    (peHeaderOffset > fs.Length - 256))
                {
                    throw new InvalidDataException(
                        "Not a valid PE file (bad e_lfanew).");
                }

                fs.Position = peHeaderOffset;
                uint sig = br.ReadUInt32();
                //
                // NOTE: "PE\0\0"
                //
                if (sig != 0x00004550)
                {
                    throw new InvalidDataException("Missing PE signature.");
                }

                long coffOffset = peHeaderOffset + 4;

                //
                // NOTE: COFF File Header (20 bytes).
                //

                //
                // NOTE: NumberOfSections offset.
                //
                fs.Position = coffOffset + 2;

                ushort numSections = br.ReadUInt16();

                //
                // NOTE: SizeOfOptionalHeader.
                //
                fs.Position = coffOffset + 16;

                ushort sizeOfOptional = br.ReadUInt16();

                long optionalHeaderOffset = coffOffset + 20;

                //
                // NOTE: Optional Header.
                //
                fs.Position = optionalHeaderOffset;

                //
                // NOTE: 0x10B (PE32) or 0x20B (PE32+).
                //
                ushort magic = br.ReadUInt16();

                if ((magic != 0x10B) && (magic != 0x20B))
                {
                    throw new InvalidDataException("Invalid PE optional " +
                        "header magic.");
                }

                bool pe32Plus = (magic == 0x20B);

                //
                // NOTE: CheckSum field is at +0x40 from start of optional
                //       header on both PE32 and PE32+ (per PE/COFF).
                long checksumOffset = optionalHeaderOffset + 0x40;

                //
                // NOTE: DataDirectory start at +0x60 (PE32) or +0x70 (PE32+).
                //
                long dataDirStart = optionalHeaderOffset +
                    (pe32Plus ? 0x70 : 0x60);

                //
                // NOTE: Security Directory entry (index 4): 8 bytes
                //       (VA/Offset + Size).
                //
                long securityDirEntryOffset = dataDirStart + (8 * 4);

                //
                // NOTE: Verify the security directory entry falls
                //       within the file.
                //
                if (securityDirEntryOffset + 8 > fs.Length)
                {
                    throw new InvalidDataException("Security directory " +
                        "entry extends past " + "end of file.");
                }

                //
                // NOTE: Read SizeOfHeaders (offset 0x3C from optional header
                //       start).
                //
                fs.Position = optionalHeaderOffset + 0x3C;

                uint sizeOfHeaders = br.ReadUInt32();

                //
                // NOTE: Read Cert Table (file offset, size) from security
                //       directory entry.  For the Security directory, this
                //       is a FILE OFFSET (not RVA).
                //
                fs.Position = securityDirEntryOffset;

                uint certFileOffset = br.ReadUInt32();

                uint certSize = br.ReadUInt32();

                //
                // NOTE: Section table starts right after the optional header.
                //
                long sectionTableOffset = optionalHeaderOffset +
                    sizeOfOptional;

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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method iterates the WIN_CERTIFICATE entries in the attribute
        /// certificate table and returns the body of the first PKCS#7 signed
        /// data entry, validating entry lengths to guard against malformed
        /// tables.
        /// </summary>
        /// <param name="fs">
        /// The file stream positioned over the PE file.
        /// </param>
        /// <param name="certTableOffset">
        /// The file offset of the attribute certificate table.
        /// </param>
        /// <param name="certTableSize">
        /// The size, in bytes, of the attribute certificate table.
        /// </param>
        /// <returns>
        /// The PKCS#7 blob from the first signed-data entry, or null if none
        /// was found.
        /// </returns>
        private static byte[]
            ReadFirstPkcs7FromWinCertificateTable(
            FileStream fs,
            long certTableOffset,
            long certTableSize
            )
        {
            fs.Position = certTableOffset;

            long limit = Math.Min(certTableOffset + certTableSize,
                fs.Length);

            BinaryReader br = new BinaryReader(fs, Encoding.UTF8, true);

            using (br)
            {
                while (fs.Position + 8 <= limit)
                {
                    uint dwLength = br.ReadUInt32();

                    ushort wRevision = br.ReadUInt16();

                    ushort wType = br.ReadUInt16();

                    //
                    // NOTE: Validate dwLength to prevent integer overflow
                    //       and excessively large allocations from
                    //       malformed PE files.
                    //
                    long bodyLen = (long)dwLength - 8;

                    if ((bodyLen <= 0) || (bodyLen >
                            (long)int.MaxValue) || (fs.Position + bodyLen >
                            limit))
                    {
                        break;
                    }

                    byte[] body = br.ReadBytes((int)bodyLen);

                    //
                    // NOTE: WIN_CERT_TYPE_PKCS_SIGNED_DATA
                    //
                    if ((wType == 0x0002) &&
                        (body != null) &&
                        (body.Length > 0))
                    {
                        return body;
                    }

                    //
                    // NOTE: Each entry is aligned to 8 bytes.
                    //
                    long padded = ((dwLength + 7U) & ~7U);

                    //
                    // BUGFIX: Advance to the next entry at (entryStart +
                    //         padded).  At this point fs.Position is
                    //         (entryStart + dwLength) -- the full entry was
                    //         consumed (8-byte header + (dwLength - 8) body) --
                    //         so the entry start is (fs.Position - dwLength).
                    //         The previous expression subtracted only the body
                    //         length, overshooting the next entry by 8 bytes
                    //         and misaligning iteration over multi-entry
                    //         certificate tables.
                    //
                    long nextPos = (fs.Position - dwLength) + padded;

                    fs.Position = Math.Min(nextPos, limit);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This structure describes the raw-data span of a single PE section,
        /// recording its file pointer and size for use during Authenticode
        /// hashing.
        /// </summary>
        [ObjectId("8ff97be4-d03f-4a00-a15b-24cfc10c8c55")]
        private struct SectionSpan
        {
            /// <summary>
            /// The file offset of the section's raw data
            /// (PointerToRawData).
            /// </summary>
            public uint PtrToRaw;  // PointerToRawData
            /// <summary>
            /// The size, in bytes, of the section's raw data
            /// (SizeOfRawData).
            /// </summary>
            public uint SizeRaw;   // SizeOfRawData
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the Authenticode hash of a PE file, hashing the
        /// headers (excluding the checksum and certificate-table directory
        /// entry), the sections in file order, and the overlay region up to the
        /// attribute certificate table, while rejecting malformed or
        /// non-trailing certificate tables.
        /// </summary>
        /// <param name="fs">
        /// The file stream positioned over the PE file.
        /// </param>
        /// <param name="hash">
        /// The hash algorithm used to accumulate the Authenticode digest.
        /// </param>
        /// <param name="pe">
        /// The parsed PE layout describing the regions to hash.
        /// </param>
        /// <param name="options">
        /// The verification options in effect for this computation.
        /// </param>
        /// <returns>
        /// The computed Authenticode hash bytes.
        /// </returns>
        private static byte[] ComputeAuthenticodeHash(
            FileStream fs,
            HashAlgorithm hash,
            PeLayout pe,
            VerificationOptions options
            )
        {
            long fileLen = fs.Length;

            //
            // NOTE: Step 1: Headers (0 .. SizeOfHeaders), skipping
            //       Checksum and Cert Table entry.
            //
            long headersEnd = Math.Min((long)pe.SizeOfHeaders, fileLen);

            //
            // NOTE: [0, Checksum).
            //
            HashRange(fs, hash, 0, pe.ChecksumFieldOffset);

            //
            // NOTE: Skip Checksum (4 bytes).
            //
            long afterChecksum = pe.ChecksumFieldOffset + 4;

            //
            // NOTE: (Checksum+4 .. SecurityDirEntry).
            //
            HashRange(fs, hash, afterChecksum, pe.SecurityDirEntryOffset);

            //
            // NOTE: Skip Security Directory entry (8 bytes).
            //
            long afterSecDir = pe.SecurityDirEntryOffset + 8;

            //
            // NOTE: (afterSecDir .. headersEnd).
            //
            if (afterSecDir < headersEnd)
            {
                HashRange(fs, hash, afterSecDir, headersEnd);
            }

            //
            // NOTE: Track a "cursor" so we never double-hash if a section
            //       overlaps the header.
            //
            long cursor = headersEnd;

            //
            // NOTE: Step 2: Sections -- sort by PointerToRawData; hash
            //       SizeOfRawData bytes for each.
            //
            SectionSpan[] secs = ReadSections(fs, pe);

            SortSectionsByPtr(secs);

            for (int i = 0;
                    i < secs.Length; i++)
            {
                uint ptr = secs[i].PtrToRaw;
                uint len = secs[i].SizeRaw;

                if (len == 0) continue;

                long start = (long)ptr;
                long end = (long)ptr + (long)len;

                //
                // NOTE: Clamp to file.
                //
                if (start > fileLen) continue;
                if (end > fileLen) end = fileLen;

                //
                // NOTE: Avoid double-hashing if section begins inside
                //       already-hashed header area.
                //
                if (start < cursor)
                    start = cursor;

                if (end > start)
                {
                    HashRange(fs, hash, start, end);

                    if (end > cursor)
                        cursor = end;
                }
            }

            //
            // BUGFIX: Step 3: hash the "extra data" (overlay) region between
            //         the end of the last section and the attribute certificate
            //         table.  The Authenticode algorithm hashes the ENTIRE file
            //         except three regions: the checksum field, the certificate-
            //         table data-directory entry, and the attribute certificate
            //         table itself.  Everything else past the last section (the
            //         overlay) IS hashed.
            //
            //         Previously this region was skipped unless the (off by
            //         default) HashAnyRemainingBytes option was set.  That let
            //         an attacker inject arbitrary bytes into the overlay of a
            //         signed image and bump the (unhashed) certificate-table
            //         directory entry past them, producing a file this routine
            //         accepted but Windows WinVerifyTrust rejected -- i.e. a
            //         signature-verification bypass.  (COMPAT: Windows
            //         Authenticode)
            //
            //         For this to be sound the certificate table must be the
            //         FINAL structure in the file (so there are no unhashed
            //         trailing bytes after it) and must begin at or after the
            //         already-hashed region; any other layout is malformed and
            //         is rejected (fail-closed) by throwing.
            //
            long certTableOffset = pe.CertTableOffset;
            long certTableSize = pe.CertTableSize;

            if ((certTableSize < 0) ||
                (certTableOffset < cursor) ||
                (certTableOffset > fileLen) ||
                ((certTableOffset + certTableSize) != fileLen))
            {
                throw new InvalidOperationException(
                    "malformed or non-trailing certificate table");
            }

            if (certTableOffset > cursor)
                HashRange(fs, hash, cursor, certTableOffset);

            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            return hash.Hash;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the section table of a PE file and returns the
        /// raw-data spans of all sections that contain raw data.
        /// </summary>
        /// <param name="fs">
        /// The file stream positioned over the PE file.
        /// </param>
        /// <param name="pe">
        /// The parsed PE layout describing the section table location and count.
        /// </param>
        /// <returns>
        /// An array of <see cref="SectionSpan" /> values for the sections with
        /// raw data.
        /// </returns>
        private static SectionSpan[] ReadSections(
            FileStream fs,
            PeLayout pe
            )
        {
            //
            // NOTE: Each IMAGE_SECTION_HEADER is 40 bytes.
            //
            const int SectSize = 40;

            int count = pe.NumberOfSections;

            if (count <= 0)
                return Array.Empty<SectionSpan>();

            List<SectionSpan> list = new List<SectionSpan>(count);

            BinaryReader br = new BinaryReader(fs, Encoding.UTF8, true);

            using (br)
            {
                long pos = pe.SectionTableOffset;

                for (int i = 0; i < count; i++)
                {
                    long off = pos + i * SectSize;

                    if (off + SectSize > fs.Length)
                        break;

                    fs.Position = off;

                    //
                    // NOTE: PointerToRawData offset within section header.
                    //
                    fs.Position = off + 0x14;

                    uint ptrToRaw = br.ReadUInt32();

                    //
                    // NOTE: SizeOfRawData offset.
                    //
                    fs.Position = off + 0x10;

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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sorts the supplied section spans in place by their raw
        /// data pointer using an insertion sort.
        /// </summary>
        /// <param name="secs">
        /// The array of section spans to sort.
        /// </param>
        private static void SortSectionsByPtr(
            SectionSpan[] secs
            )
        {
            //
            // NOTE: Simple insertion sort; avoids LINQ.
            //
            for (int i = 1; i < secs.Length; i++)
            {
                SectionSpan key = secs[i];
                int j = i - 1;

                while ((j >= 0) && (secs[j].PtrToRaw > key.PtrToRaw))
                {
                    secs[j + 1] = secs[j];
                    j--;
                }

                secs[j + 1] = key;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method feeds a half-open byte range of the file into the hash
        /// algorithm, reading in buffered chunks.
        /// </summary>
        /// <param name="fs">
        /// The file stream to read from.
        /// </param>
        /// <param name="hash">
        /// The hash algorithm to update with the range bytes.
        /// </param>
        /// <param name="start">
        /// The inclusive start file offset of the range.
        /// </param>
        /// <param name="end">
        /// The exclusive end file offset of the range.
        /// </param>
        private static void HashRange(
            FileStream fs,
            HashAlgorithm hash,
            long start,
            long end
            )
        {
            if (start >= end) return;

            fs.Position = start;
            long remaining = end - start;
            byte[] buffer = new byte[64 * 1024];

            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);

                int read = fs.Read(buffer, 0, toRead);

                if (read <= 0)
                    throw new EndOfStreamException();

                hash.TransformBlock(buffer, 0, read, null, 0);

                remaining -= read;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Minimal DER Helpers
        //
        // NOTE: Parse SpcIndirectDataContent into
        //       DigestInfo (AlgorithmIdentifier OID + digest OCTET STRING).
        //
        /// <summary>
        /// This method parses an SpcIndirectDataContent structure to recover
        /// the digest algorithm object identifier (OID) and the expected digest
        /// value from its embedded DigestInfo.
        /// </summary>
        /// <param name="eContent">
        /// The encapsulated content bytes of the signed data.
        /// </param>
        /// <param name="digestOid">
        /// Upon success, receives the digest algorithm OID; upon failure, is set
        /// to null.
        /// </param>
        /// <param name="digest">
        /// Upon success, receives the expected digest bytes; upon failure, is
        /// set to null.
        /// </param>
        /// <returns>
        /// True if both the digest OID and digest were parsed; otherwise, false.
        /// </returns>
        private static bool
            TryParseSpcIndirectDataDigest(
            byte[] eContent,
            out string digestOid,
            out byte[] digest
            )
        {
            digestOid = null;
            digest = null;

            try
            {
                DerReader r = new DerReader(eContent);

                //
                // NOTE: SpcIndirectDataContent.
                //
                DerReader seq = r.ReadSequence();

                //
                // NOTE: Data (we do not need it).
                //
                seq.SkipValue();

                //
                // NOTE: DigestInfo.
                //
                DerReader digestInfo = seq.ReadSequence();

                //
                // NOTE: AlgorithmIdentifier.
                //
                DerReader alg = digestInfo.ReadSequence();

                //
                // NOTE: Algorithm OID.
                //
                digestOid = alg.ReadOid();

                //
                // NOTE: Skip params if present.
                //
                if (alg.HasData)
                    alg.SkipToEnd();

                //
                // NOTE: Digest OCTET STRING.
                //
                digest = digestInfo.ReadOctetString();

                return ((digestOid != null) && (digest != null));
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Parse RFC3161 TSTInfo: messageImprint.hashAlgorithm OID,
        //       messageImprint.hashedMessage, genTime (GeneralizedTime).
        //
        /// <summary>
        /// This method parses an RFC 3161 TSTInfo structure to recover the
        /// message imprint hash algorithm object identifier (OID), the hashed
        /// message, and the generation time.
        /// </summary>
        /// <param name="tstInfoDer">
        /// The DER-encoded TSTInfo bytes to parse.
        /// </param>
        /// <param name="hashAlgOid">
        /// Upon success, receives the message imprint hash algorithm OID; upon
        /// failure, is set to null.
        /// </param>
        /// <param name="hashedMessage">
        /// Upon success, receives the hashed message bytes; upon failure, is set
        /// to null.
        /// </param>
        /// <param name="genTime">
        /// Upon success, receives the timestamp generation time; upon failure,
        /// is set to null.
        /// </param>
        /// <returns>
        /// True if the TSTInfo structure was parsed; otherwise, false.
        /// </returns>
        private static bool TryParseRfc3161TstInfo(
            byte[] tstInfoDer,
            out string hashAlgOid,
            out byte[] hashedMessage,
            out DateTimeOffset? genTime
            )
        {
            hashAlgOid = null;
            hashedMessage = null;
            genTime = null;

            try
            {
                DerReader r = new DerReader(tstInfoDer);

                //
                // NOTE: TSTInfo.
                //
                DerReader seq = r.ReadSequence();

                //
                // NOTE: version INTEGER.
                //
                seq.SkipValueExpectedTag(0x02);

                //
                // NOTE: policy OID.
                //
                seq.SkipValueExpectedTag(0x06);

                //
                // NOTE: messageImprint.
                //
                DerReader mi = seq.ReadSequence();

                //
                // NOTE: AlgorithmIdentifier.
                //
                DerReader alg = mi.ReadSequence();

                hashAlgOid = alg.ReadOid();

                //
                // NOTE: Skip params if present.
                //
                if (alg.HasData)
                    alg.SkipToEnd();

                //
                // NOTE: hashedMessage.
                //
                hashedMessage = mi.ReadOctetString();

                //
                // NOTE: serialNumber INTEGER.
                //
                seq.SkipValueExpectedTag(0x02);

                //
                // NOTE: genTime (GeneralizedTime, tag 0x18).
                //
                genTime = seq.ReadGeneralizedTime();

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Unified way to obtain the raw
        //       signature bytes for a SignerInfo.
        //
        /// <summary>
        /// This method obtains the raw signature bytes for a signer, preferring
        /// the runtime API when available and falling back to parsing the CMS
        /// DER to locate the matching SignerInfo signature.
        /// </summary>
        /// <param name="signer">
        /// The signer whose signature bytes are requested.
        /// </param>
        /// <param name="containerCms">
        /// The CMS message that contains the signer.
        /// </param>
        /// <returns>
        /// The raw signature bytes, or null if they could not be obtained.
        /// </returns>
        private static byte[]
            GetSignerSignatureBytes(
            SignerInfo signer,
            SignedCms containerCms
            )
        {
            //
            // NOTE: Path A -- use the API if it exists (newer runtimes).
            //
            try
            {
                System.Reflection.MethodInfo mi = typeof(SignerInfo).GetMethod(
                        "GetSignature", System.Reflection
                            .BindingFlags.Instance | System.Reflection
                            .BindingFlags.Public);

                if (mi != null)
                {
                    object roMem = mi.Invoke(signer, null);

                    if (roMem != null)
                    {
                        //
                        // NOTE: On modern runtimes this returns
                        //       ReadOnlyMemory<byte>.
                        //
                        Type t = roMem.GetType();

                        if ((t.FullName != null) && (t.FullName.StartsWith(
                                "System" + ".ReadOnlyMemory")))
                        {
                            System.Reflection.MethodInfo toArray =
                                t.GetMethod("ToArray", System.Type.EmptyTypes);

                            if (toArray != null)
                            {
                                object arr = toArray.Invoke(roMem, null);

                                return arr as byte[];
                            }
                        }

                        //
                        // NOTE: Some builds may return byte[] directly.
                        //
                        byte[] asBytes = roMem as byte[];

                        if (asBytes != null)
                            return asBytes;
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);
            }

            //
            // NOTE: Path B -- parse the CMS DER to find the matching
            //       SignerInfo.signature.
            //
            byte[] sig;

            if (TryGetSignerSignatureFromCms(containerCms, signer, out sig))
            {
                return sig;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method re-encodes a CMS message to DER and extracts the
        /// signature bytes of the target signer from it.
        /// </summary>
        /// <param name="cms">
        /// The CMS message to encode and search.
        /// </param>
        /// <param name="target">
        /// The signer whose signature bytes are requested.
        /// </param>
        /// <param name="signature">
        /// Upon success, receives the signer signature bytes; upon failure, is
        /// set to null.
        /// </param>
        /// <returns>
        /// True if the signer signature was extracted; otherwise, false.
        /// </returns>
        private static bool
            TryGetSignerSignatureFromCms(
            SignedCms cms,
            SignerInfo target,
            out byte[] signature
            )
        {
            signature = null;

            try
            {
                return
                    TryExtractSignerSignatureFromCmsDer(cms.Encode(), target,
                        out signature);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Extract SignerInfo.signature (OCTET
        //       STRING) from SignedData.signerInfos
        //       matching the 'target' signer.  Match
        //       by Issuer+SerialNumber if available,
        //       otherwise by SubjectKeyIdentifier (SKI).
        //
        /// <summary>
        /// This method walks the DER encoding of a CMS message to find the
        /// SignerInfo matching the target signer (by issuer and serial number,
        /// or by subject key identifier) and returns its signature OCTET STRING.
        /// </summary>
        /// <param name="der">
        /// The DER-encoded CMS ContentInfo bytes to search.
        /// </param>
        /// <param name="target">
        /// The signer to match within the signerInfos set.
        /// </param>
        /// <param name="signature">
        /// Upon success, receives the matched signer's signature bytes; upon
        /// failure, is set to null.
        /// </param>
        /// <returns>
        /// True if the matching signer signature was extracted; otherwise,
        /// false.
        /// </returns>
        private static bool
            TryExtractSignerSignatureFromCmsDer(
            byte[] der,
            SignerInfo target,
            out byte[] signature
            )
        {
            signature = null;

            if ((der == null) || (der.Length < 2))
                return false;

            //
            // NOTE: ContentInfo ::= SEQUENCE { contentType OID,
            //       [0] EXPLICIT ANY }
            //
            int len, content;

            if ((der[0] != 0x30) || (!DerReader.ReadLength(
                    der, 1, out len, out content)))
            {
                return false;
            }

            int ciEnd = content + len;
            int p = content;

            //
            // NOTE: contentType OID.
            //
            if ((p >= ciEnd) || (der[p] != 0x06))
                return false;

            int oidLen, oidContent;

            if (!DerReader.ReadLength(der, p + 1, out oidLen, out oidContent))
            {
                return false;
            }

            //
            // NOTE: Skip OID.
            //
            p = oidContent + oidLen;

            //
            // NOTE: [0] EXPLICIT signedData.
            //
            if ((p >= ciEnd) || (der[p] != 0xA0))
                return false;

            int a0Len, a0Content;

            if (!DerReader.ReadLength(der, p + 1, out a0Len, out a0Content))
            {
                return false;
            }

            int sdPos = a0Content;
            int sdEnd = a0Content + a0Len;

            //
            // NOTE: SignedData ::= SEQUENCE { version, digestAlgorithms,
            //       encapContentInfo, [0] certs OPTIONAL, [1] crls OPTIONAL,
            //       signerInfos SET }
            //
            if ((sdPos >= sdEnd) || (der[sdPos] != 0x30))
            {
                return false;
            }

            int sdLen, sdContent;

            if (!DerReader.ReadLength(der, sdPos + 1,
                    out sdLen, out sdContent))
            {
                return false;
            }

            int q = sdContent;
            int qEnd = sdContent + sdLen;

            //
            // NOTE: version INTEGER.
            //
            if ((q >= qEnd) || (der[q] != 0x02))
                return false;

            int vLen, vContent;

            if (!DerReader.ReadLength(der, q + 1, out vLen, out vContent))
            {
                return false;
            }

            q = vContent + vLen;

            //
            // NOTE: digestAlgorithms SET.
            //
            if ((q >= qEnd) || (der[q] != 0x31))
                return false;

            int daLen, daContent;

            if (!DerReader.ReadLength(der, q + 1, out daLen, out daContent))
            {
                return false;
            }

            q = daContent + daLen;

            //
            // NOTE: encapContentInfo SEQUENCE.
            //
            if ((q >= qEnd) || (der[q] != 0x30))
                return false;

            int eciLen, eciContent;

            if (!DerReader.ReadLength(der, q + 1, out eciLen, out eciContent))
            {
                return false;
            }

            q = eciContent + eciLen;

            //
            // NOTE: [0] certificates OPTIONAL.
            //
            if ((q < qEnd) && (der[q] == 0xA0))
            {
                int cLen, cContent;

                if (!DerReader.ReadLength(der, q + 1, out cLen, out cContent))
                {
                    return false;
                }

                q = cContent + cLen;
            }

            //
            // NOTE: [1] crls OPTIONAL.
            //
            if ((q < qEnd) && (der[q] == 0xA1))
            {
                int rLen, rContent;

                if (!DerReader.ReadLength(der, q + 1, out rLen, out rContent))
                {
                    return false;
                }

                q = rContent + rLen;
            }

            //
            // NOTE: signerInfos SET.
            //
            if ((q >= qEnd) || (der[q] != 0x31))
                return false;

            int siSetLen, siSetContent;

            if (!DerReader.ReadLength(der, q + 1,
                    out siSetLen, out siSetContent))
            {
                return false;
            }

            int siPos = siSetContent;
            int siEnd = siSetContent + siSetLen;

            //
            // NOTE: Prepare target identifiers.
            //
            byte[] targetIssuer = null;
            byte[] targetSerialBE = null;
            byte[] targetSki = null;

            X509Certificate2 cert = target != null ? target.Certificate : null;

            if (cert != null)
            {
                //
                // NOTE: Issuer DER from the certificate TBSCertificate.
                //
                try
                {
                    targetIssuer = cert.IssuerName.RawData;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(e,
                        typeof(WinTrustDotNet).Name, TracePriority
                            .SecurityDebug2);

                    targetIssuer = null;
                }

                targetSerialBE = GetCertSerialBigEndian(cert);

                targetSki = GetCertSubjectKeyIdentifier(cert);
            }

            //
            // NOTE: Iterate each SignerInfo (SEQUENCE) in the SET.
            //
            while (siPos < siEnd)
            {
                if (der[siPos] != 0x30) break;

                int oneLen, oneContent;

                if (!DerReader.ReadLength(der, siPos + 1,
                        out oneLen, out oneContent))
                {
                    break;
                }

                int t = oneContent;
                int tEnd = oneContent + oneLen;

                //
                // NOTE: SignerInfo ::= SEQUENCE { version, sid,
                //       digestAlgorithm, [0] signedAttrs OPTIONAL,
                //       signatureAlgorithm, signature OCTET STRING, ... }
                //

                //
                // NOTE: version.
                //
                if ((t >= tEnd) || (der[t] != 0x02))
                {
                    siPos = tEnd;
                    continue;
                }

                int svLen, svContent;

                if (!DerReader.ReadLength(der, t + 1,
                        out svLen, out svContent))
                {
                    siPos = tEnd;
                    continue;
                }

                t = svContent + svLen;

                //
                // NOTE: sid (IssuerAndSerialNumber OR
                //       [0] SubjectKeyIdentifier).
                //
                bool sidMatches = false;

                if ((t < tEnd) && (der[t] == 0x30))
                {
                    //
                    // NOTE: IssuerAndSerialNumber -- capture issuer DER.
                    //
                    int issLen, issContent;

                    if (!DerReader.ReadLength(der, t + 1,
                            out issLen, out issContent))
                    {
                        siPos = tEnd;
                        continue;
                    }

                    int issuerStart = t;

                    int issuerTotalLen = 1 + (issContent - (t + 1)) + issLen;

                    byte[] issuerDer = new byte[issuerTotalLen];

                    Buffer.BlockCopy(der, issuerStart,
                        issuerDer, 0, issuerTotalLen);

                    t = issContent + issLen;

                    //
                    // NOTE: serial INTEGER.
                    //
                    if ((t >= tEnd) || (der[t] != 0x02))
                    {
                        siPos = tEnd;
                        continue;
                    }

                    int sLen, sContent;

                    if (!DerReader.ReadLength(der, t + 1,
                            out sLen, out sContent))
                    {
                        siPos = tEnd;
                        continue;
                    }

                    byte[] serialDer = new byte[sLen];

                    Buffer.BlockCopy(der, sContent, serialDer, 0, sLen);

                    t = sContent + sLen;

                    if ((targetIssuer != null) && (targetSerialBE != null))
                    {
                        //
                        // NOTE: Normalize serial -- remove leading 0x00
                        //       from INTEGER content.
                        //
                        byte[] serialNorm = TrimLeftZeros(serialDer);

                        byte[] targetSerialNorm = TrimLeftZeros(
                                targetSerialBE);

                        if ((BytesEqual(
                                issuerDer,
                                targetIssuer)) &&
                            (BytesEqual(
                                serialNorm,
                                targetSerialNorm)))
                        {
                            sidMatches = true;
                        }
                    }
                }
                else if ((t < tEnd) && (der[t] == 0x80))
                {
                    //
                    // NOTE: [0] IMPLICIT SubjectKeyIdentifier.
                    //
                    int skiLen, skiContent;

                    if (!DerReader.ReadLength(der, t + 1,
                            out skiLen, out skiContent))
                    {
                        siPos = tEnd;
                        continue;
                    }

                    byte[] skiVal = new byte[skiLen];

                    Buffer.BlockCopy(der, skiContent, skiVal, 0, skiLen);

                    t = skiContent + skiLen;

                    if ((targetSki != null) && (BytesEqual(targetSki, skiVal)))
                    {
                        sidMatches = true;
                    }
                }
                else
                {
                    //
                    // NOTE: Unknown sid; move on.
                    //
                    siPos = tEnd;
                    continue;
                }

                //
                // NOTE: digestAlgorithm (skip).
                //
                if ((t >= tEnd) || (der[t] != 0x30))
                {
                    siPos = tEnd;
                    continue;
                }

                int dAlgLen, dAlgContent;

                if (!DerReader.ReadLength(der, t + 1,
                        out dAlgLen, out dAlgContent))
                {
                    siPos = tEnd;
                    continue;
                }

                t = dAlgContent + dAlgLen;

                //
                // NOTE: [0] signedAttrs OPTIONAL (skip if present).
                //
                if ((t < tEnd) && (der[t] == 0xA0))
                {
                    int saLen, saContent;

                    if (!DerReader.ReadLength(der, t + 1,
                            out saLen, out saContent))
                    {
                        siPos = tEnd;
                        continue;
                    }

                    t = saContent + saLen;
                }

                //
                // NOTE: signatureAlgorithm (skip).
                //
                if ((t >= tEnd) || (der[t] != 0x30))
                {
                    siPos = tEnd;
                    continue;
                }

                int sAlgLen, sAlgContent;

                if (!DerReader.ReadLength(der, t + 1,
                        out sAlgLen, out sAlgContent))
                {
                    siPos = tEnd;
                    continue;
                }

                t = sAlgContent + sAlgLen;

                //
                // NOTE: signature OCTET STRING -- this is what we need.
                //
                if ((t < tEnd) && (der[t] == 0x04))
                {
                    int sigLen, sigContent;

                    if (!DerReader.ReadLength(der, t + 1,
                            out sigLen, out sigContent))
                    {
                        siPos = tEnd;
                        continue;
                    }

                    if (sidMatches)
                    {
                        signature = new byte[sigLen];

                        Buffer.BlockCopy(der, sigContent,
                            signature, 0, sigLen);

                        return true;
                    }

                    //
                    // NOTE: Move past signature to continue scanning
                    //       (unsignedAttrs may follow).
                    //
                    t = sigContent + sigLen;
                }

                siPos = tEnd;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Helpers for matching.
        //
        /// <summary>
        /// This method returns the serial number of a certificate in big-endian
        /// byte order.
        /// </summary>
        /// <param name="cert">
        /// The certificate whose serial number is returned.
        /// </param>
        /// <returns>
        /// The big-endian serial number bytes, or null on failure.
        /// </returns>
        private static byte[] GetCertSerialBigEndian(
            X509Certificate2 cert
            )
        {
            try
            {
                //
                // NOTE: GetSerialNumber() returns little-endian; reverse to
                //       big-endian.
                //
                byte[] le = cert.GetSerialNumber();

                if (le == null) return null;

                Array.Reverse(le);
                return le;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes leading zero bytes from a byte array, leaving at
        /// least one byte, so that two integer encodings can be compared after
        /// normalization.
        /// </summary>
        /// <param name="v">
        /// The byte array to trim.
        /// </param>
        /// <returns>
        /// The trimmed byte array, or the original array when there is nothing
        /// to trim.
        /// </returns>
        private static byte[] TrimLeftZeros(
            byte[] v
            )
        {
            if ((v == null) || (v.Length == 0))
                return v;

            int i = 0;

            while ((i < v.Length - 1) && (v[i] == 0x00))
            {
                i++;
            }

            if (i == 0) return v;

            byte[] t = new byte[v.Length - i];

            Buffer.BlockCopy(v, i, t, 0, t.Length);

            return t;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the subject key identifier (SKI) extension value
        /// of a certificate, if present.
        /// </summary>
        /// <param name="cert">
        /// The certificate whose subject key identifier is returned.
        /// </param>
        /// <returns>
        /// The subject key identifier bytes, or null if the extension is absent.
        /// </returns>
        private static byte[]
            GetCertSubjectKeyIdentifier(
            X509Certificate2 cert
            )
        {
            try
            {
                for (int i = 0;
                        i < cert.Extensions.Count;
                        i++)
                {
                    X509Extension ext = cert.Extensions[i];

                    if ((ext != null) && (ext.Oid != null) &&
                        (StringEquals(ext.Oid.Value, "2.5.29.14")))
                    {
                        //
                        // NOTE: The extension value is an OCTET STRING
                        //       wrapping the SKI OCTET STRING.
                        //
                        byte[] ski = ExtractOctetString(ext.RawData);

                        return ski;
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the signingTime signed attribute of a signer and
        /// returns it in Coordinated Universal Time (UTC).
        /// </summary>
        /// <param name="si">
        /// The signer whose signing time is read.
        /// </param>
        /// <returns>
        /// The signing time in UTC, or null if it is not present.
        /// </returns>
        private static DateTimeOffset? TryGetSigningTimeUtc(
            SignerInfo si
            )
        {
            if (si == null) return null;

            CryptographicAttributeObjectCollection
                sa = si.SignedAttributes;

            if (sa == null) return null;

            for (int i = 0; i < sa.Count; i++)
            {
                CryptographicAttributeObject attr = sa[i];

                //
                // NOTE: signingTime.
                //
                if ((attr.Oid != null) && (StringEquals(
                        attr.Oid.Value, "1.2.840.113549.1.9.5")))
                {
                    if ((attr.Values != null) && (attr.Values.Count > 0))
                    {
                        try
                        {
                            Pkcs9SigningTime st = new Pkcs9SigningTime(
                                    attr.Values[0].RawData);

                            return st.SigningTime.ToUniversalTime();
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                                TracePriority.SecurityDebug2);
                        }
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Extract OCTET STRING value from a
        //       single-valued attribute (RawData
        //       encodes the value, i.e., includes '04 len ...').
        //
        /// <summary>
        /// This method finds the attribute with the given object identifier
        /// (OID) in a collection and returns the contents of its single
        /// OCTET STRING value.
        /// </summary>
        /// <param name="attrs">
        /// The attribute collection to search.
        /// </param>
        /// <param name="oid">
        /// The object identifier (OID) of the attribute to find.
        /// </param>
        /// <returns>
        /// The OCTET STRING contents of the matching attribute, or null if it is
        /// not found or not well-formed.
        /// </returns>
        private static byte[]
            GetSingleAttributeOctetValue(CryptographicAttributeObjectCollection
                attrs,
            string oid
            )
        {
            if (attrs == null) return null;

            for (int i = 0; i < attrs.Count; i++)
            {
                CryptographicAttributeObject a = attrs[i];

                if ((a.Oid != null) && (StringEquals(a.Oid.Value, oid)))
                {
                    if ((a.Values != null) && (a.Values.Count > 0))
                    {
                        byte[] raw = a.Values[0].RawData;

                        //
                        // NOTE: Expect OCTET STRING.
                        //
                        if ((raw != null) &&
                            (raw.Length >= 2) &&
                            (raw[0] == 0x04))
                        {
                            int len;
                            int offset;

                            if (!DerReader.ReadLength(raw, 1,
                                    out len, out offset))
                            {
                                return null;
                            }

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

        ///////////////////////////////////////////////////////////////////////

        #region Utilities
        /// <summary>
        /// This method compares two byte arrays for equality using a
        /// constant-time comparison to avoid timing side channels.
        /// </summary>
        /// <param name="a">
        /// The first byte array to compare.
        /// </param>
        /// <param name="b">
        /// The second byte array to compare.
        /// </param>
        /// <returns>
        /// True if the arrays are equal in length and contents; otherwise,
        /// false.
        /// </returns>
        private static bool BytesEqual(
            byte[] a,
            byte[] b
            )
        {
            if (ReferenceEquals(a, b))
                return true;

            if ((a == null) || (b == null) || (a.Length != b.Length))
            {
                return false;
            }

#if NET_STANDARD_21
            //
            // NOTE: Use constant-time comparison to avoid timing side-channel
            //       attacks on digest comparisons.
            //
            return CryptographicOperations.FixedTimeEquals(a, b);
#else
            //
            // NOTE: Best-effort constant-time comparison for older runtimes.
            //       This avoids short-circuit evaluation; however, JIT or CPU
            //       branch prediction may still introduce timing variance.
            //
            int diff = 0;

            for (int i = 0; i < a.Length; i++)
                diff |= (a[i] ^ b[i]);

            return diff == 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two strings for equality using an ordinal
        /// (case-sensitive) comparison.
        /// </summary>
        /// <param name="a">
        /// The first string to compare.
        /// </param>
        /// <param name="b">
        /// The second string to compare.
        /// </param>
        /// <returns>
        /// True if the strings are ordinally equal; otherwise, false.
        /// </returns>
        private static bool StringEquals(
            string a,
            string b
            )
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two certificates have the same
        /// thumbprint.
        /// </summary>
        /// <param name="a">
        /// The first certificate to compare.
        /// </param>
        /// <param name="b">
        /// The second certificate to compare.
        /// </param>
        /// <returns>
        /// True if both certificates are non-null and share the same
        /// thumbprint; otherwise, false.
        /// </returns>
        private static bool CertEqualThumbprint(
            X509Certificate2 a,
            X509Certificate2 b
            )
        {
            if ((a == null) || (b == null))
                return false;

            return StringEquals(a.Thumbprint, b.Thumbprint);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a certificate appears in a collection
        /// by comparing thumbprints.
        /// </summary>
        /// <param name="set">
        /// The collection of certificates to search.
        /// </param>
        /// <param name="cert">
        /// The certificate to look for.
        /// </param>
        /// <returns>
        /// True if a certificate with the same thumbprint is present; otherwise,
        /// false.
        /// </returns>
        private static bool
            IsInCollectionByThumbprint(
            X509Certificate2[] set,
            X509Certificate2 cert
            )
        {
            if ((set == null) || (cert == null))
                return false;

            for (int i = 0; i < set.Length; i++)
            {
                if (StringEquals(set[i].Thumbprint, cert.Thumbprint))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a digest algorithm is permitted by
        /// policy, rejecting MD5 and SHA-1 unless explicitly allowed by the
        /// options.
        /// </summary>
        /// <param name="oid">
        /// The object identifier (OID) of the digest algorithm to check.
        /// </param>
        /// <param name="options">
        /// The verification options carrying the digest-algorithm policy.
        /// </param>
        /// <returns>
        /// True if the digest algorithm is allowed; otherwise, false.
        /// </returns>
        private static bool IsDigestAlgorithmAllowed(
            string oid,
            VerificationOptions options
            )
        {
            if ((oid == null) || (options == null))
                return true;

            //
            // NOTE: MD5 is cryptographically broken for collision resistance.
            //
            if ((StringEquals(
                    oid, "1.2.840.113549.2.5")) &&
                (!options.AllowMd5))
            {
                return false;
            }

            //
            // NOTE: SHA-1 has practical collision attacks (SHAttered, 2017).
            //
            if ((StringEquals(oid, "1.3.14.3.2.26")) && (!options.AllowSha1))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a hash algorithm instance corresponding to a
        /// digest algorithm object identifier (OID).
        /// </summary>
        /// <param name="oid">
        /// The object identifier (OID) of the digest algorithm.
        /// </param>
        /// <returns>
        /// A new hash algorithm instance, or null if the OID is unrecognized.
        /// </returns>
        private static HashAlgorithm CreateHashFromOid(
            string oid
            )
        {
            if (oid == null) return null;

            switch (oid)
            {
                case "1.3.14.3.2.26":
                    return SHA1.Create();
                case "2.16.840.1.101.3.4.2.1":
                    return SHA256.Create();
                case "2.16.840.1.101.3.4.2.2":
                    return SHA384.Create();
                case "2.16.840.1.101.3.4.2.3":
                    return SHA512.Create();
                case "1.2.840.113549.2.5":
                    return MD5.Create();
                default:
                    return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads all certificate files (*.cer/*.crt/*.der/*.pem)
        /// from a directory, expanding PEM files that contain multiple
        /// certificates.
        /// </summary>
        /// <param name="dir">
        /// The directory to load certificate files from.  If this parameter is
        /// null, empty, or refers to a missing directory, null is returned.
        /// </param>
        /// <returns>
        /// An array of the loaded certificates, or null if the directory is not
        /// usable.
        /// </returns>
        private static X509Certificate2[]
            LoadCertificatesFromDirectory(
            string dir
            )
        {
            if ((string.IsNullOrEmpty(dir)) || (!Directory.Exists(dir)))
            {
                return null;
            }

            List<X509Certificate2> list = new List<X509Certificate2>();

            string[] files = Directory.GetFiles(dir);

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];

                string ext = Path.GetExtension(path).ToLowerInvariant();

                if ((ext == ".cer") || (ext == ".crt") ||
                    (ext == ".der") || (ext == ".pem"))
                {
                    try
                    {
                        byte[] raw = File.ReadAllBytes(path);

                        if (ext == ".pem")
                        {
                            //
                            // NOTE: PEM files may contain multiple
                            //       certificates (e.g., a full chain).
                            //
                            byte[][] derBlocks = PemToDerMultiple(raw);

                            if (derBlocks == null)
                                continue;

                            for (int d = 0;
                                    d < derBlocks.Length;
                                    d++)
                            {
                                X509Certificate2
                                    pemCert = new X509Certificate2(
                                        derBlocks[d]);

                                list.Add(pemCert);
                            }

                            continue;
                        }

                        X509Certificate2 cert = new X509Certificate2(raw);

                        list.Add(cert);
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                            TracePriority.SecurityDebug2);
                    }
                }
            }

            return list.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Minimal PEM parser for "-----BEGIN CERTIFICATE-----".
        //
        /// <summary>
        /// This method extracts the DER bytes of the first certificate from a
        /// PEM-encoded buffer.
        /// </summary>
        /// <param name="pemBytes">
        /// The PEM-encoded certificate bytes.
        /// </param>
        /// <returns>
        /// The DER bytes of the first certificate, or null if no valid
        /// certificate block was found.
        /// </returns>
        private static byte[] PemToDer(
            byte[] pemBytes
            )
        {
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

            try
            {
                return Convert.FromBase64String(b64);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Extract all certificates from a PEM
        //       file that may contain multiple
        //       "-----BEGIN CERTIFICATE-----" blocks
        //       (e.g., a full certificate chain).
        //
        /// <summary>
        /// This method extracts the DER bytes of every certificate from a
        /// PEM-encoded buffer that may contain multiple certificate blocks.
        /// </summary>
        /// <param name="pemBytes">
        /// The PEM-encoded certificate bytes.
        /// </param>
        /// <returns>
        /// An array of DER certificate byte arrays, or null if no valid
        /// certificate blocks were found.
        /// </returns>
        private static byte[][] PemToDerMultiple(
            byte[] pemBytes
            )
        {
            if ((pemBytes == null) || (pemBytes.Length == 0))
            {
                return null;
            }

            string s = Encoding.ASCII.GetString(pemBytes);
            const string begin = "-----BEGIN CERTIFICATE-----";
            const string end = "-----END CERTIFICATE-----";

            List<byte[]> results = new List<byte[]>();
            int searchFrom = 0;

            while (searchFrom < s.Length)
            {
                int i0 = s.IndexOf(begin, searchFrom,
                    StringComparison.Ordinal);

                if (i0 < 0) break;
                i0 += begin.Length;

                int i1 = s.IndexOf(end, i0, StringComparison.Ordinal);

                if (i1 < 0) break;

                string b64 = s.Substring(i0, i1 - i0);
                b64 = b64.Replace("\r", "").Replace("\n", "").Trim();

                try
                {
                    byte[] der = Convert.FromBase64String(b64);

                    results.Add(der);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                        TracePriority.SecurityDebug2);
                }

                searchFrom = i1 + end.Length;
            }

            if (results.Count == 0) return null;
            return results.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collects the Authority Information Access (AIA)
        /// caIssuers URLs from a certificate, skipping any URL already visited.
        /// </summary>
        /// <param name="cert">
        /// The certificate whose AIA extension is examined.
        /// </param>
        /// <param name="outUrls">
        /// The list that receives the newly discovered caIssuers URLs.
        /// </param>
        /// <param name="visitedUrls">
        /// The set of already-seen URLs, updated with any URLs added to
        /// <paramref name="outUrls" />.
        /// </param>
        private static void GatherAiaUrls(
            X509Certificate2 cert,
            List<string> outUrls,
            HashSet<string> visitedUrls
            )
        {
            if ((cert == null) || (cert.Extensions == null))
            {
                return;
            }

            for (int i = 0;
                    i < cert.Extensions.Count;
                    i++)
            {
                X509Extension ext = cert.Extensions[i];

                if ((ext == null) || (ext.Oid == null))
                    continue;

                //
                // NOTE: Authority Information Access.
                //
                if (!StringEquals(ext.Oid.Value, "1.3.6.1.5.5.7.1.1"))
                {
                    continue;
                }

                try
                {
                    DerReader r = new DerReader(ext.RawData);

                    //
                    // NOTE: AuthorityInfoAccessSyntax ::= SEQUENCE OF
                    //       AccessDescription.
                    //
                    DerReader aiaSeq = r.ReadSequence();

                    while (aiaSeq.HasData)
                    {
                        //
                        // NOTE: AccessDescription.
                        //
                        DerReader ad = aiaSeq.ReadSequence();

                        //
                        // NOTE: accessMethod.
                        //
                        string methodOid = ad.ReadOid();

                        //
                        // NOTE: accessLocation is a GeneralName; for URI
                        //       it is [6] IA5String (tag 0x86).
                        //
                        string uri = ad.ReadUriIfPresent();

                        //
                        // NOTE: caIssuers.
                        //
                        if ((uri != null) && (StringEquals(
                                methodOid, "1.3.6.1.5.5" + ".7.48.2")))
                        {
                            if (!visitedUrls.Contains(uri))
                            {
                                outUrls.Add(uri);

                                visitedUrls.Add(uri);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                        TracePriority.SecurityDebug2);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method downloads certificates from an Authority Information
        /// Access (AIA) URL, enforcing the scheme, timeout, and response-size
        /// policy, and attempts to decode the response as a PKCS#7 certificate
        /// bag, a single DER certificate, or a PEM certificate.
        /// </summary>
        /// <param name="uri">
        /// The AIA URL to download certificates from.
        /// </param>
        /// <param name="options">
        /// The verification options carrying the AIA download policy.
        /// </param>
        /// <returns>
        /// An array of the downloaded certificates, or null on policy rejection,
        /// network failure, or decode failure.
        /// </returns>
        private static X509Certificate2[]
            DownloadCertificatesFromAia(
            string uri,
            VerificationOptions options
            )
        {
            try
            {
                //
                // NOTE: Allow only http/https.
                //
                if ((!uri.StartsWith("http://",
                        StringComparison.OrdinalIgnoreCase)) &&
                    (!uri.StartsWith("https://",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                if ((uri.StartsWith("http://",
                        StringComparison.OrdinalIgnoreCase)) &&
                    (!options.AllowAiaInsecureHttp))
                {
                    return null;
                }

                HttpClient http = new HttpClient();

                using (http)
                {
                    http.Timeout = options.AiaHttpTimeout;

                    //
                    // NOTE: Enforce a maximum response size to prevent
                    //       memory exhaustion from a
                    //       malicious or misconfigured AIA URL.
                    //
                    http.MaxResponseContentBufferSize =
                        options.AiaMaxResponseSize;

                    http.DefaultRequestHeaders.TryAddWithoutValidation(
                            "User-Agent", "WinTrustDotNet/1.0");

                    http.DefaultRequestHeaders.TryAddWithoutValidation(
                            "Accept", "application/pkix-cert" +
                            ", application/" + "pkcs7-mime" +
                            ", application/" + "pkcs7-certificates" + ", */*");

                    byte[] data = http.GetByteArrayAsync(uri)
                        .GetAwaiter().GetResult();

                    if ((data == null) || (data.Length == 0))
                    {
                        return null;
                    }

                    //
                    // NOTE: Try decode as PKCS#7 cert bag (p7c).
                    //
                    try
                    {
                        SignedCms cms = new SignedCms();

                        cms.Decode(data);

                        if ((cms.Certificates !=
                                null) && (cms.Certificates.Count > 0))
                        {
                            X509Certificate2[] arr = new X509Certificate2[
                                    cms.Certificates.Count];

                            for (int i = 0;
                                    i < cms.Certificates.Count;
                                    i++)
                            {
                                arr[i] = cms.Certificates[i];
                            }

                            return arr;
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                            TracePriority.SecurityDebug2);
                    }

                    //
                    // NOTE: Try DER single cert.
                    //
                    try
                    {
                        X509Certificate2 single = new X509Certificate2(data);

                        return new X509Certificate2[]
                            { single };
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                            TracePriority.SecurityDebug2);
                    }

                    //
                    // NOTE: Try PEM (rare via AIA).
                    //
                    try
                    {
                        byte[] der = PemToDer(data);

                        if (der != null)
                        {
                            X509Certificate2
                                pemCert = new X509Certificate2(der);

                            return
                                new X509Certificate2[]
                                    { pemCert };
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                            TracePriority.SecurityDebug2);
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(e, typeof(WinTrustDotNet).Name,
                    TracePriority.SecurityDebug2);

                //
                // NOTE: Network/parse issues.
                //
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Minimal DER Reader
        /// <summary>
        /// This class implements a minimal forward-only reader for a subset of
        /// DER (Distinguished Encoding Rules) used to parse the ASN.1 structures
        /// involved in Authenticode signatures and timestamps.
        /// </summary>
        [ObjectId("4339cc70-a38e-46fb-ae69-26590049be84")]
        private sealed class DerReader
        {
            /// <summary>
            /// The backing buffer being read.
            /// </summary>
            private readonly byte[] _data;
            /// <summary>
            /// The current read position within the buffer.
            /// </summary>
            private int _pos;
            /// <summary>
            /// The exclusive end position of the readable region.
            /// </summary>
            private readonly int _end;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Constructs a reader over an entire byte buffer.
            /// </summary>
            /// <param name="data">
            /// The buffer to read from.  This parameter may be null, in which
            /// case the reader has no readable data.
            /// </param>
            public DerReader(
                byte[] data
                ) : this(data, 0, data != null ? data.Length : 0)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Constructs a reader over a sub-range of a byte buffer.
            /// </summary>
            /// <param name="data">
            /// The buffer to read from.
            /// </param>
            /// <param name="offset">
            /// The start offset of the readable region within the buffer.
            /// </param>
            /// <param name="length">
            /// The length, in bytes, of the readable region.
            /// </param>
            private DerReader(
                byte[] data,
                int offset,
                int length
                )
            {
                _data = data;
                _pos = offset;
                _end = offset + length;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets a value indicating whether there is more data to read.
            /// </summary>
            public bool HasData
            {
                get { return _pos < _end; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads a GeneralName URI ([6] IA5String, tag 0x86) at
            /// the current position if present; otherwise, it skips the current
            /// value to keep parsing aligned.
            /// </summary>
            /// <returns>
            /// The URI string if the current element is a URI; otherwise, null.
            /// </returns>
            public string ReadUriIfPresent()
            {
                //
                // NOTE: A GeneralName with tag [6] IA5String uses the context-
                //       specific primitive tag 0x86.
                //
                if (!HasData) return null;

                if (_data[_pos] != 0x86)
                {
                    //
                    // NOTE: Not a URI; skip the value to keep parsing aligned.
                    //
                    SkipValue();
                    return null;
                }

                byte tag;
                int len, header;

                ReadTagLen(out tag, out len, out header);

                if ((tag != 0x86) || (_pos + len > _end))
                {
                    return null;
                }

                string uri = Encoding.ASCII.GetString(_data, _pos, len);

                _pos += len;
                return uri;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method returns the tag byte at the current position without
            /// advancing the reader.
            /// </summary>
            /// <returns>
            /// The tag byte at the current position.
            /// </returns>
            public byte PeekTag()
            {
                if (_pos >= _end)
                {
                    throw new InvalidDataException("Unexpected end.");
                }

                return _data[_pos];
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Correctly returns the SEQUENCE
            //       start (tag) and total encoded length (header + content).
            //
            /// <summary>
            /// This method reads a SEQUENCE and reports its start offset (at the
            /// tag) and total encoded length (header plus content), advancing
            /// past it.
            /// </summary>
            /// <param name="sequenceStart">
            /// Upon success, receives the offset of the SEQUENCE tag; upon
            /// failure, is set to zero.
            /// </param>
            /// <param name="totalEncodedLength">
            /// Upon success, receives the total encoded length of the SEQUENCE;
            /// upon failure, is set to zero.
            /// </param>
            /// <returns>
            /// True if a SEQUENCE was read; otherwise, false.
            /// </returns>
            public bool TryReadRawSequenceFull(
                out int sequenceStart,
                out int totalEncodedLength
                )
            {
                sequenceStart = 0;
                totalEncodedLength = 0;
                int save = _pos;

                try
                {
                    byte tag;
                    int len, hdr;

                    ReadTagLen(out tag, out len, out hdr);

                    if (tag != 0x30)
                    {
                        _pos = save;
                        return false;
                    }

                    //
                    // NOTE: Start at the SEQUENCE TAG (not at content).
                    //
                    sequenceStart = save;

                    //
                    // NOTE: header + content.
                    //
                    totalEncodedLength = hdr + len;

                    //
                    // NOTE: Advance to end of this SEQUENCE.
                    //
                    _pos = save + totalEncodedLength;

                    return true;
                }
                catch
                {
                    _pos = save;
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Reads a SEQUENCE and returns
            //       its raw slice offsets (without
            //       advancing outer reader except consuming the sequence).
            //
            /// <summary>
            /// This method reads a SEQUENCE and reports the offset of its
            /// content and its total encoded length (header plus content),
            /// advancing past it.
            /// </summary>
            /// <param name="contentStart">
            /// Upon success, receives the offset of the SEQUENCE content; upon
            /// failure, is set to zero.
            /// </param>
            /// <param name="totalLength">
            /// Upon success, receives the total encoded length of the SEQUENCE;
            /// upon failure, is set to zero.
            /// </param>
            /// <returns>
            /// True if a SEQUENCE was read; otherwise, false.
            /// </returns>
            public bool TryReadRawSequence(
                out int contentStart,
                out int totalLength
                )
            {
                contentStart = 0;
                totalLength = 0;
                int save = _pos;

                try
                {
                    byte tag;
                    int len, hdr;

                    ReadTagLen(out tag, out len, out hdr);

                    if (tag != 0x30)
                    {
                        _pos = save;
                        return false;
                    }

                    //
                    // NOTE: At start of content.
                    //
                    contentStart = _pos;
                    totalLength = len + hdr;

                    //
                    // NOTE: Move to end of sequence.
                    //
                    _pos = contentStart + len;
                    return true;
                }
                catch
                {
                    _pos = save;
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads a SEQUENCE and returns a new reader scoped to
            /// its content, advancing this reader past the SEQUENCE.
            /// </summary>
            /// <returns>
            /// A reader over the content of the SEQUENCE.
            /// </returns>
            public DerReader ReadSequence()
            {
                byte tag;
                int len, hdr;

                ReadTagLen(out tag, out len, out hdr);

                if (tag != 0x30)
                {
                    throw new InvalidDataException("Expected SEQUENCE");
                }

                int start = _pos;
                int end = _pos + len;

                DerReader inner = new DerReader(_data, start, len);

                _pos = end;
                return inner;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method skips the current value (of any tag), advancing the
            /// reader past it.
            /// </summary>
            public void SkipValue()
            {
                byte tag;
                int len, hdr;

                ReadTagLen(out tag, out len, out hdr);

                _pos += len;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method advances the reader to the end of its readable
            /// region, discarding any remaining data.
            /// </summary>
            public void SkipToEnd()
            {
                _pos = _end;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method skips the current value, requiring that it carry the
            /// expected tag and throwing if it does not.
            /// </summary>
            /// <param name="expectedTag">
            /// The tag byte that the current value is required to have.
            /// </param>
            public void SkipValueExpectedTag(
                byte expectedTag
                )
            {
                byte tag;
                int len, hdr;

                ReadTagLen(out tag, out len, out hdr);

                if (tag != expectedTag)
                {
                    throw new InvalidDataException("Unexpected tag.");
                }

                _pos += len;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads an OBJECT IDENTIFIER and returns its
            /// dotted-decimal string form.
            /// </summary>
            /// <returns>
            /// The dotted-decimal representation of the OID.
            /// </returns>
            public string ReadOid()
            {
                byte tag;
                int len, hdr;

                ReadTagLen(out tag, out len, out hdr);

                if (tag != 0x06)
                {
                    throw new InvalidDataException("Expected OID");
                }

                if ((len <= 0) || (_pos + len > _end))
                {
                    throw new InvalidDataException("Bad OID length");
                }

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

                if (_pos != end)
                {
                    throw new InvalidDataException("OID parse error");
                }

                return sb.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads an OCTET STRING and returns its contents.
            /// </summary>
            /// <returns>
            /// The bytes contained in the OCTET STRING.
            /// </returns>
            public byte[] ReadOctetString()
            {
                byte tag;
                int len, hdr;

                ReadTagLen(out tag, out len, out hdr);

                if (tag != 0x04)
                {
                    throw new InvalidDataException("Expected OCTET STRING");
                }

                if (_pos + len > _end)
                {
                    throw new InvalidDataException("Truncated OCTET STRING");
                }

                byte[] val = new byte[len];

                Buffer.BlockCopy(_data, _pos, val, 0, len);

                _pos += len;
                return val;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads a GeneralizedTime value (which must be in UTC,
            /// ending with 'Z'), tolerating optional fractional seconds, and
            /// returns it as a date and time offset.
            /// </summary>
            /// <returns>
            /// The parsed GeneralizedTime as a UTC date and time offset.
            /// </returns>
            public DateTimeOffset
                ReadGeneralizedTime()
            {
                byte tag;
                int len, hdr;

                ReadTagLen(out tag, out len, out hdr);

                if (tag != 0x18)
                {
                    throw new InvalidDataException("Expected " +
                        "GeneralizedTime");
                }

                if (_pos + len > _end)
                {
                    throw new InvalidDataException("Truncated " +
                        "GeneralizedTime");
                }

                string s = Encoding.ASCII.GetString(_data, _pos, len);

                _pos += len;

                //
                // NOTE: Accept forms: YYYYMMDDHHMMSSZ or with
                //       fractional seconds (e.g., .fff) and 'Z'.  Parse
                //       minimally and assume 'Z' (UTC).
                //
                if (!s.EndsWith("Z", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("GeneralizedTime " +
                        "not UTC");
                }

                s = s.Substring(0, s.Length - 1);

                //
                // NOTE: Trim fractional seconds if present.
                //
                int dot = s.IndexOf('.');

                string basePart = dot >= 0 ? s.Substring(0, dot) : s;

                //
                // NOTE: Ensure at least YYYYMMDDHHMMSS.
                //
                if (basePart.Length < 14)
                {
                    throw new InvalidDataException("GeneralizedTime " +
                        "too short");
                }

                int year = int.Parse(basePart.Substring(0, 4));

                int mon = int.Parse(basePart.Substring(4, 2));

                int day = int.Parse(basePart.Substring(6, 2));

                int hh = int.Parse(basePart.Substring(8, 2));

                int mm = int.Parse(basePart.Substring(10, 2));

                int ss = int.Parse(basePart.Substring(12, 2));

                return new DateTimeOffset(new DateTime(
                        year, mon, day, hh, mm, ss, DateTimeKind.Utc));
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method reads the tag and definite-length fields at the
            /// current position, advancing past them and validating that the
            /// length does not exceed the readable region.
            /// </summary>
            /// <param name="tag">
            /// Upon return, receives the tag byte.
            /// </param>
            /// <param name="len">
            /// Upon return, receives the content length.
            /// </param>
            /// <param name="headerLen">
            /// Upon return, receives the number of bytes consumed by the tag and
            /// length fields.
            /// </param>
            private void ReadTagLen(
                out byte tag,
                out int len,
                out int headerLen
                )
            {
                if (_pos >= _end)
                {
                    throw new InvalidDataException("Unexpected end of data");
                }

                tag = _data[_pos++];

                if (_pos >= _end)
                {
                    throw new InvalidDataException("Unexpected end of data");
                }

                int b = _data[_pos++];

                if ((b & 0x80) == 0)
                {
                    len = b;
                    headerLen = 2;
                }
                else
                {
                    int n = b & 0x7F;

                    if ((n == 0) || (n > 4))
                    {
                        throw
                            new InvalidDataException("Unsupported " +
                            "length form");
                    }

                    if (_pos + n > _end)
                    {
                        throw
                            new InvalidDataException("Truncated length");
                    }

                    len = 0;

                    for (int i = 0; i < n; i++)
                    {
                        len = (len << 8) | _data[_pos++];
                    }

                    //
                    // BUGFIX: A 4-byte length with the high bit set yields a
                    //         negative int; reject any negative length so the
                    //         bounds check below cannot be bypassed by integer
                    //         wraparound (lengths beyond the int range are not
                    //         supported here).
                    //
                    if (len < 0)
                    {
                        throw
                            new InvalidDataException("Invalid length");
                    }

                    headerLen = 2 + n;
                }

                if ((long)_pos + (long)len > _end)
                {
                    throw new InvalidDataException("Length exceeds container");
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Helper used by GetSingleAttributeOctetValue.
            //
            /// <summary>
            /// This method reads a DER definite-length field from a buffer at a
            /// given offset, validating that the resulting content fits within
            /// the buffer.
            /// </summary>
            /// <param name="data">
            /// The buffer containing the length field.
            /// </param>
            /// <param name="offset">
            /// The offset of the length field within the buffer.
            /// </param>
            /// <param name="len">
            /// Upon success, receives the decoded content length; upon failure,
            /// is set to zero.
            /// </param>
            /// <param name="contentOffset">
            /// Upon success, receives the offset of the content following the
            /// length field; upon failure, is set to zero.
            /// </param>
            /// <returns>
            /// True if a valid length was read and the content fits within the
            /// buffer; otherwise, false.
            /// </returns>
            public static bool ReadLength(
                byte[] data,
                int offset,
                out int len,
                out int contentOffset
                )
            {
                len = 0;
                contentOffset = 0;

                if (offset >= data.Length)
                    return false;

                int b = data[offset];

                if ((b & 0x80) == 0)
                {
                    len = b;
                    contentOffset = offset + 1;

                    return (contentOffset + len <= data.Length);
                }

                int n = b & 0x7F;

                if ((n <= 0) || (n > 4))
                    return false;

                if (offset + 1 + n > data.Length)
                    return false;

                int val = 0;

                for (int i = 0; i < n; i++)
                {
                    val = (val << 8) | data[offset + 1 + i];
                }

                len = val;
                contentOffset = offset + 1 + n;

                //
                // BUGFIX: Reject a negative length (4-byte length with the high
                //         bit set) and use widened arithmetic for the bounds
                //         check so it cannot be bypassed by integer overflow.
                //
                if (len < 0)
                    return false;

                return ((long)contentOffset + (long)len <= data.Length);
            }
        }
        #endregion
        #endregion
    }
}
