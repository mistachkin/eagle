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
    /// <summary>
    /// This class provides security-related helper methods used by the software
    /// updater, including administrator detection, Authenticode and strong name
    /// signature verification, and remote certificate validation.
    /// </summary>
    [Guid("aa944fc9-6286-443b-bb3c-61fa70cc56a7")]
    internal static class SecurityOps
    {
        #region Private Security Data
        /// <summary>
        /// This class holds the default public key token expected for the Eagle
        /// assembly.
        /// </summary>
        [Guid("99639a18-01b9-4dc0-b3de-05aa9609709a")]
        private static class PublicKeyToken
        {
            /// <summary>
            /// The default public key token expected for the Eagle assembly.
            /// </summary>
            internal static readonly byte[] Default = {
                0x29, 0xc6, 0x29, 0x76, 0x30, 0xbe, 0x05, 0xeb
            };
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Administrator Support Methods
        /// <summary>
        /// This method determines whether the current user is a member of the
        /// built-in Administrators role, catching and suppressing any
        /// exceptions.
        /// </summary>
        /// <returns>
        /// True if the current user is an administrator; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified assembly is
        /// Authenticode signed by a certificate matching the specified subject.
        /// In debug builds, this check is faked and always succeeds because
        /// such builds are never officially released.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose Authenticode signature is to be checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="subject">
        /// The certificate subject name to match, or null to match any
        /// subject.
        /// </param>
        /// <param name="verify">
        /// Non-zero to verify the certificate chain in addition to matching the
        /// subject.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, this contains the certificate used to sign the
        /// assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the assembly is Authenticode signed by a matching
        /// certificate; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified file is Authenticode
        /// signed by a certificate matching the specified subject, catching any
        /// exception that may occur.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file whose Authenticode signature is to be checked.
        /// </param>
        /// <param name="subject">
        /// The certificate subject name to match, or null to match any
        /// subject.
        /// </param>
        /// <param name="verify">
        /// Non-zero to verify the certificate chain in addition to matching the
        /// subject.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, this contains the certificate used to sign the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the file is Authenticode signed by a matching certificate;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified public key token
        /// matches the default public key token expected for the Eagle
        /// assembly.
        /// </summary>
        /// <param name="publicKeyToken">
        /// The public key token to check.  A null value is treated as matching.
        /// </param>
        /// <returns>
        /// True if the public key token matches the default (or is null);
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified assembly is strong name
        /// signed, returning its public key token upon success.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose strong name signature is to be checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, this contains the public key token of the assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the assembly is strong name signed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method is the remote certificate validation callback used to
        /// validate the server certificate for software update requests.  It
        /// accepts the certificate if there are no policy errors or if its
        /// public key matches one of the expected software update public keys.
        /// </summary>
        /// <param name="sender">
        /// The source of the validation request.
        /// </param>
        /// <param name="certificate">
        /// The remote certificate being validated.
        /// </param>
        /// <param name="chain">
        /// The chain of certificate authorities associated with the remote
        /// certificate.
        /// </param>
        /// <param name="sslPolicyErrors">
        /// The policy errors, if any, detected during the default validation.
        /// </param>
        /// <returns>
        /// True if the remote certificate is considered valid; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the public key of the specified
        /// certificate matches the specified expected public key.
        /// </summary>
        /// <param name="certificate">
        /// The certificate whose public key is to be compared.  This parameter
        /// may be null.
        /// </param>
        /// <param name="publicKey">
        /// The expected public key to compare against.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the certificate public key matches the expected public key;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the subject (or simple name) of the
        /// specified certificate matches, or begins with, the specified
        /// subject.
        /// </summary>
        /// <param name="certificate2">
        /// The certificate whose subject is to be compared.  This parameter may
        /// be null.
        /// </param>
        /// <param name="subject">
        /// The subject name to match, or null to match any subject.
        /// </param>
        /// <returns>
        /// True if the certificate subject matches the specified subject (or
        /// the specified subject is null); otherwise, false.
        /// </returns>
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
