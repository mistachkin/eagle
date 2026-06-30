/*
 * CertificateOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using CertificateDictionary = System.Collections.Generic.Dictionary<
    string, System.Security.Cryptography.X509Certificates.X509Certificate>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for loading, caching, and
    /// verifying X.509 certificates used by the Eagle library.
    /// </summary>
    [ObjectId("acaf57c4-509f-4953-b43b-2222a40d6d33")]
    internal static class CertificateOps
    {
        #region Private Data
        /// <summary>
        /// Stores the object used to synchronize access to the certificate
        /// cache and the associated chain verification settings.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the cache mapping signed file names to their loaded X.509
        /// certificates.
        /// </summary>
        private static CertificateDictionary certificates = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the X.509 chain verification flags used when verifying a
        /// certificate chain.
        /// </summary>
        private static X509VerificationFlags verificationFlags =
            X509VerificationFlags.NoFlag;

        /// <summary>
        /// Stores the X.509 revocation mode used when verifying a certificate
        /// chain.
        /// </summary>
        private static X509RevocationMode revocationMode =
            X509RevocationMode.Online;

        /// <summary>
        /// Stores the X.509 revocation flag used when verifying a certificate
        /// chain.
        /// </summary>
        private static X509RevocationFlag revocationFlag =
            X509RevocationFlag.ExcludeRoot;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds the certificate-related introspection information
        /// to the specified list of name/value pairs.
        /// </summary>
        /// <param name="list">
        /// The list of name/value pairs to add the information to.  If this
        /// parameter is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail in the resulting
        /// information.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty ||
                    ((certificates != null) && (certificates.Count > 0)))
                {
                    localList.Add("Certificates",
                        (certificates != null) ?
                            certificates.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty ||
                    (verificationFlags != X509VerificationFlags.NoFlag))
                {
                    localList.Add("VerificationFlags",
                        verificationFlags.ToString());
                }

                if (empty ||
                    (revocationMode != X509RevocationMode.NoCheck))
                {
                    localList.Add("RevocationMode",
                        revocationMode.ToString());
                }

                if (empty ||
                    (revocationFlag != X509RevocationFlag.EndCertificateOnly))
                {
                    localList.Add("RevocationFlag",
                        revocationFlag.ToString());
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Certificate Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Certificate Methods
        /// <summary>
        /// This method queries the X.509 chain verification settings currently
        /// in effect.
        /// </summary>
        /// <param name="verificationFlags">
        /// Upon return, this parameter will be set to the X.509 chain
        /// verification flags currently in effect.
        /// </param>
        /// <param name="revocationMode">
        /// Upon return, this parameter will be set to the X.509 revocation
        /// mode currently in effect.
        /// </param>
        /// <param name="revocationFlag">
        /// Upon return, this parameter will be set to the X.509 revocation
        /// flag currently in effect.
        /// </param>
        public static void QueryFlags(
            out X509VerificationFlags verificationFlags,
            out X509RevocationMode revocationMode,
            out X509RevocationFlag revocationFlag
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                verificationFlags = CertificateOps.verificationFlags;
                revocationMode = CertificateOps.revocationMode;
                revocationFlag = CertificateOps.revocationFlag;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the certificate cache, optionally forcing a
        /// new, empty cache to be created.
        /// </summary>
        /// <param name="force">
        /// Non-zero to always create a new, empty cache, even if one already
        /// exists.
        /// </param>
        public static void Initialize(
            bool force
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (force || (certificates == null))
                    certificates = new CertificateDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the certificate cache, discarding all cached
        /// certificates.
        /// </summary>
        /// <returns>
        /// The number of certificates that were removed from the cache.
        /// </returns>
        public static int ClearCache()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (certificates != null)
                {
                    result += certificates.Count;

                    certificates.Clear();
                    certificates = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the X.509 certificate associated with the
        /// specified signed file, optionally consulting and updating the
        /// certificate cache.
        /// </summary>
        /// <param name="fileName">
        /// The name of the signed file whose certificate is to be loaded.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass the certificate cache, always loading the
        /// certificate directly from the file.  This parameter should be
        /// non-zero in any context that makes security decisions based on the
        /// loaded certificate.
        /// </param>
        /// <param name="certificate">
        /// Upon success, this parameter will be set to the loaded certificate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an error message that
        /// describes why the certificate could not be loaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetCertificate(
            string fileName,
            bool noCache,
            ref X509Certificate certificate,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(fileName))
            {
                //
                // WARNING: Do not call this method with a false
                //          value for the noCache parameter from
                //          any context which may make security
                //          decisions based on the return value
                //          of this method.
                //
                X509Certificate localCertificate = null; /* REUSED */

                if (!noCache)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        //
                        // HACK: For speed, just return the already
                        //       cached certificate, which could be
                        //       null.
                        //
                        if ((certificates != null) &&
                            certificates.TryGetValue(
                                fileName, out localCertificate))
                        {
                            certificate = localCertificate;
                            return ReturnCode.Ok;
                        }
                    }
                }

                try
                {
                    localCertificate = X509Certificate.CreateFromSignedFile(
                        fileName); /* throw */

                    certificate = localCertificate;
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
                finally
                {
                    if (!noCache)
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (certificates != null)
                                certificates[fileName] = localCertificate;
                        }
                    }
                }
            }
            else
            {
                error = "invalid file name";
            }

#if DEBUG
            if (!PathOps.IsSameFile(
                    Interpreter.GetActive(), fileName,
                    GlobalState.GetAssemblyLocation()))
#endif
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCertificate: file {0} query failure, error = {1}",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(CertificateOps).Name, TracePriority.SecurityError);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the <see cref="X509Certificate2" /> associated
        /// with the specified signed file, optionally consulting and updating
        /// the certificate cache.
        /// </summary>
        /// <param name="fileName">
        /// The name of the signed file whose certificate is to be loaded.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass the certificate cache, always loading the
        /// certificate directly from the file.  This parameter should be
        /// non-zero in any context that makes security decisions based on the
        /// loaded certificate.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, this parameter will be set to the loaded certificate.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetCertificate2(
            string fileName,
            bool noCache,
            ref X509Certificate2 certificate2
            )
        {
            Result error = null;

            return GetCertificate2(
                fileName, noCache, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the <see cref="X509Certificate2" /> associated
        /// with the specified signed file, optionally consulting and updating
        /// the certificate cache.
        /// </summary>
        /// <param name="fileName">
        /// The name of the signed file whose certificate is to be loaded.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass the certificate cache, always loading the
        /// certificate directly from the file.  This parameter should be
        /// non-zero in any context that makes security decisions based on the
        /// loaded certificate.
        /// </param>
        /// <param name="certificate2">
        /// Upon success, this parameter will be set to the loaded certificate.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an error message that
        /// describes why the certificate could not be loaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetCertificate2(
            string fileName,
            bool noCache,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            X509Certificate certificate = null;

            if (GetCertificate(
                    fileName, noCache, ref certificate,
                    ref error) == ReturnCode.Ok)
            {
                if (certificate != null)
                {
                    try
                    {
                        certificate2 = new X509Certificate2(certificate);
                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid certificate";
                }
            }

#if DEBUG
            if (!PathOps.IsSameFile(
                    Interpreter.GetActive(), fileName,
                    GlobalState.GetAssemblyLocation()))
#endif
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCertificate2: file {0} query failure, error = {1}",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(CertificateOps).Name, TracePriority.SecurityError);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies the X.509 certificate chain for the specified
        /// certificate using the specified chain policy settings.
        /// </summary>
        /// <param name="assembly">
        /// The assembly associated with the certificate, used only to provide
        /// additional context in verbose error messages.  This parameter may
        /// be null.
        /// </param>
        /// <param name="certificate2">
        /// The certificate whose chain is to be verified.
        /// </param>
        /// <param name="verificationFlags">
        /// The X.509 chain verification flags to use.
        /// </param>
        /// <param name="revocationMode">
        /// The X.509 revocation mode to use.
        /// </param>
        /// <param name="revocationFlag">
        /// The X.509 revocation flag to use.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional context, such as the associated
        /// assembly, in the resulting error message.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an error message that
        /// describes why the certificate chain could not be verified.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode VerifyChain(
            Assembly assembly,
            X509Certificate2 certificate2,
            X509VerificationFlags verificationFlags,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            bool verbose,
            ref Result error
            )
        {
            if (certificate2 != null)
            {
                try
                {
                    X509Chain chain = X509Chain.Create();

                    if (chain != null)
                    {
                        X509ChainPolicy chainPolicy = chain.ChainPolicy;

                        if (chainPolicy != null)
                        {
                            //
                            // NOTE: Setup the chain policy settings as specified
                            //       by the caller.
                            //
                            chainPolicy.VerificationFlags = verificationFlags;
                            chainPolicy.RevocationMode = revocationMode;
                            chainPolicy.RevocationFlag = revocationFlag;

                            if (chain.Build(certificate2))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                StringList list = new StringList();

                                if (chain.ChainStatus != null)
                                {
                                    foreach (X509ChainStatus status in chain.ChainStatus)
                                    {
                                        list.Add(
                                            status.Status.ToString(),
                                            status.StatusInformation);
                                    }

                                    if (verbose && (assembly != null))
                                    {
                                        error = String.Format(
                                            "assembly {0}: {1}",
                                            FormatOps.WrapOrNull(assembly),
                                            FormatOps.WrapOrNull(list.ToString()));
                                    }
                                    else
                                    {
                                        error = list;
                                    }
                                }
                                else
                                {
                                    if (verbose && (assembly != null))
                                    {
                                        error = String.Format(
                                            "assembly {0}: \"invalid chain status\"",
                                            FormatOps.WrapOrNull(assembly));
                                    }
                                    else
                                    {
                                        error = "invalid chain status";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (verbose && (assembly != null))
                            {
                                error = String.Format(
                                    "assembly {0}: \"invalid chain policy\"",
                                    FormatOps.WrapOrNull(assembly));
                            }
                            else
                            {
                                error = "invalid chain policy";
                            }
                        }
                    }
                    else
                    {
                        if (verbose && (assembly != null))
                        {
                            error = String.Format(
                                "assembly {0}: \"invalid chain\"",
                                FormatOps.WrapOrNull(assembly));
                        }
                        else
                        {
                            error = "invalid chain";
                        }
                    }
                }
                catch (Exception e)
                {
                    if (verbose && (assembly != null))
                    {
                        error = String.Format(
                            "assembly {0}: {1}",
                            FormatOps.WrapOrNull(assembly),
                            FormatOps.WrapOrNull(e));
                    }
                    else
                    {
                        error = e;
                    }
                }
            }
            else
            {
                if (verbose && (assembly != null))
                {
                    error = String.Format(
                        "assembly {0}: \"invalid certificate\"",
                        FormatOps.WrapOrNull(assembly));
                }
                else
                {
                    error = "invalid certificate";
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
