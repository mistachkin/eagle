/*
 * SecurityOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Policy;
using System.Security.Principal;
using _PublicKey = Eagle._Components.Shared.PublicKey;
using _StringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
    [Guid("aa944fc9-6286-443b-bb3c-61fa70cc56a7")]
    internal static class SecurityOps
    {
        #region Private Security Data
        [Guid("99639a18-01b9-4dc0-b3de-05aa9609709a")]
        private static class PublicKeyToken
        {
            internal static readonly byte[] Default = {
                0x29, 0xc6, 0x29, 0x76, 0x30, 0xbe, 0x05, 0xeb
            };
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Administrator Support Methods
        public static bool IsAdministrator()
        {
            try
            {
                //
                // BUGBUG: This may not work correctly on Mono.
                //
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                if (identity == null)
                    return false;

                WindowsPrincipal principal = new WindowsPrincipal(identity);

                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                // do nothing.
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Signature Checking Methods
        public static bool IsAuthenticodeSigned(
            Assembly assembly,
            string subject,
            bool verify,
            ref X509Certificate2 certificate2,
            ref string error
            )
        {
#if !DEBUG
            if (assembly != null)
            {
                return IsAuthenticodeSigned(
                    assembly.Location, subject, verify,
                    ref certificate2, ref error);
            }

            return false;
#else
            //
            // NOTE: In-development version, fake it.  We can do this
            //       because DEBUG builds are never officially released.
            //
            return true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAuthenticodeSigned(
            string fileName,
            string subject,
            bool verify,
            ref X509Certificate2 certificate2,
            ref string error
            )
        {
            try
            {
                X509Certificate certificate =
                    X509Certificate.CreateFromSignedFile(fileName);

                if (certificate != null)
                {
                    certificate2 = new X509Certificate2(certificate);

                    if (certificate2 != null)
                    {
                        if (MatchCertificateSubject(certificate2, subject))
                        {
                            if (verify)
                                return certificate2.Verify();
                            else
                                return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDefaultPublicKeyToken(
            byte[] publicKeyToken
            )
        {
            if (publicKeyToken == null)
                return true;

            byte[] defaultPublicKeyToken = PublicKeyToken.Default;

            if (defaultPublicKeyToken == null)
                return false;

            if (GenericOps<byte>.Equals(publicKeyToken, defaultPublicKeyToken))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStrongNameSigned(
            Assembly assembly,
            ref byte[] publicKeyToken,
            ref string error
            )
        {
            if (assembly == null)
            {
                error = "assembly is invalid";
                return false;
            }

            AssemblyName assemblyName = assembly.GetName();

            if (assemblyName == null)
            {
                error = "assembly has invalid name";
                return false;
            }

            byte[] publicKey = assemblyName.GetPublicKey();

            if (publicKey == null)
            {
                error = "assembly has invalid public key";
                return false;
            }

            Evidence evidence = assembly.Evidence;

            if (evidence == null)
            {
                error = "assembly has invalid evidence";
                return false;
            }

            IEnumerator enumerator = evidence.GetHostEnumerator();

            if (enumerator == null)
            {
                error = "assembly has invalid evidence enumerator";
                return false;
            }

            while (enumerator.MoveNext())
            {
                StrongName strongName = enumerator.Current as StrongName;

                if (strongName == null)
                    continue;

                StrongNamePublicKeyBlob strongNamePublicKey =
                    strongName.PublicKey;

                if (strongNamePublicKey == null)
                {
                    error = "assembly strong name has invalid public key";
                    return false;
                }

                if (GenericOps<byte>.Equals(ParseOps.HexString(
                        strongNamePublicKey.ToString()), publicKey))
                {
                    publicKeyToken = assemblyName.GetPublicKeyToken();

                    if (publicKeyToken == null)
                    {
                        error = "assembly has invalid public key token";
                        return false;
                    }

                    return true;
                }
            }

            error = "assembly is not signed";
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Software Update Trust Methods
        public static bool RemoteCertificateValidationCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
            )
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if (MatchCertificatePublicKey(
                    certificate, _PublicKey.SoftwareUpdate1))
            {
                return true;
            }

            if (MatchCertificatePublicKey(
                    certificate, _PublicKey.SoftwareUpdate2))
            {
                return true;
            }

            if (MatchCertificatePublicKey(
                    certificate, _PublicKey.SoftwareUpdate3))
            {
                return true;
            }

            if (MatchCertificatePublicKey(
                    certificate, _PublicKey.SoftwareUpdate4))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchCertificatePublicKey(
            X509Certificate certificate,
            byte[] publicKey
            )
        {
            //
            // NOTE: Make sure the certificate public key matches what we
            //       expect it to be for our own software updates.
            //
            if (certificate != null)
            {
                byte[] certificatePublicKey = certificate.GetPublicKey();

                if ((certificatePublicKey != null) &&
                    (certificatePublicKey.Length > 0))
                {
                    if ((publicKey != null) && (publicKey.Length > 0))
                    {
                        return GenericOps<byte>.Equals(
                            certificatePublicKey, publicKey);
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchCertificateSubject(
            X509Certificate2 certificate2,
            string subject
            )
        {
            if (certificate2 == null)
                return false;

            if (subject == null)
                return true;

            string localSubject = certificate2.Subject;

            if (_StringOps.SystemEquals(localSubject, subject))
                return true;

            if ((localSubject != null) && localSubject.StartsWith(
                    subject + Characters.Space,
                    _StringOps.GetSystemComparisonType(false)))
            {
                return true;
            }

            string localSimpleName = certificate2.GetNameInfo(
                X509NameType.SimpleName, false);

            if (_StringOps.SystemEquals(localSimpleName, subject))
                return true;

            if ((localSimpleName != null) && localSimpleName.StartsWith(
                    subject + Characters.Space,
                    _StringOps.GetSystemComparisonType(false)))
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}
