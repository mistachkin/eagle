/*
 * UpdateOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if !NET_STANDARD_20
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if !NET_STANDARD_20
using _PublicKey = Eagle._Components.Shared.PublicKey;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("711b1e60-8516-4f41-ba61-89c48f904d0a")]
    internal static class UpdateOps
    {
        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Public Key Data
#if !NET_STANDARD_20
        //
        // NOTE: This lock is used to synchronize access to the static fields
        //       "PublicKey1", "PublicKey2", "PublicKey3", "PublicKey4", and
        //       "PublicKey5".
        //
        private static readonly object publicKeySyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is one specifically reserved for updates to
        //       the core library.  It is logically constant and should not be
        //       changed (except to null, which will disable its use).  This
        //       is a "legacy" key (2048 bits).  It is trusted by the vast
        //       majority of published Eagle builds when checking for updates.
        //       In the future, newer builds of Eagle may start refusing to
        //       trust this key.
        //
        private static byte[] PublicKey1 = _PublicKey.SoftwareUpdate1;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        private static byte[] PublicKey2 = _PublicKey.SoftwareUpdate2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        private static byte[] PublicKey3 = _PublicKey.SoftwareUpdate3;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        private static byte[] PublicKey4 = _PublicKey.SoftwareUpdate4;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is RESERVED for use by third-party plugins
        //       and applications; however, it is not public because it is
        //       not intended to be used lightly.
        //
        private static byte[] PublicKey5 = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Exclusive Mode Support
        //
        // NOTE: This lock is used to synchronize access to the static field
        //       "exclusive".
        //
        private static readonly object exclusiveSyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static bool exclusive = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Data
#if !NET_STANDARD_20
#if !MONO
        //
        // HACK: This is purposely not read-only; however, it is logically a
        //       constant.
        //
        private static bool useLegacyCertificatePolicy = false;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This lock is used to synchronize access to the static fields
        //       "savedCertificatePolicy", "haveSavedCertificatePolicy", and
        //       "certificatePolicy".
        //
        private static readonly object certificatePolicySyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static ICertificatePolicy savedCertificatePolicy;
        private static bool haveSavedCertificatePolicy;

        ///////////////////////////////////////////////////////////////////////

        private static readonly ICertificatePolicy certificatePolicy =
            new CertificatePolicy();
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ServerCertificateValidationCallback Support Data
#if !NET_STANDARD_20
        //
        // NOTE: This lock is used to synchronize access to the property
        //       "ServicePointManager.ServerCertificateValidationCallback".
        //
        private static readonly object certificateCallbackSyncRoot = new object();
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        public static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Introspection Methods
        public static void GetStatus(
            ref StringList list
            )
        {
            if (list == null)
                list = new StringList();

            list.Add("updates(active)");
            list.Add(IsTrusted().ToString());

            list.Add("updates(exclusive)");
            list.Add(IsExclusive().ToString());

#if !NET_STANDARD_20
            GetPublicKeys(ref list);

            list.Add("updates(legacyActive)");
            list.Add(IsLegacyCertificatePolicyActive().ToString());

            list.Add("updates(modernActive)");
            list.Add(IsServerCertificateValidationCallbackActive().ToString());

#if !MONO
            list.Add("updates(legacyFavored)");
            list.Add(ShouldUseLegacyCertificatePolicy().ToString());
#endif
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Exclusive Mode Support Methods
        public static bool IsExclusive()
        {
            lock (exclusiveSyncRoot)
            {
                return exclusive;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetExclusive(
            bool exclusive,
            ref Result error
            )
        {
            lock (exclusiveSyncRoot) /* TRANSACTIONAL */
            {
                bool wasExclusive = IsExclusive();

                if (exclusive != wasExclusive)
                {
                    UpdateOps.exclusive = exclusive;

                    TraceOps.DebugTrace(String.Format(
                        "SetExclusive: exclusive mode {0}",
                        exclusive ? "enabled" : "disabled"),
                        typeof(UpdateOps).Name,
                        TracePriority.SecurityDebug);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "already {0}", exclusive ?
                            "exclusive" : "non-exclusive");
                }
            }

            TraceOps.DebugTrace(String.Format(
                "SetExclusive: exclusive = {0}, error = {1}",
                exclusive, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Trust Support Methods
        public static bool IsTrusted()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
#if !NET_STANDARD_20
#if MONO
                return IsLegacyCertificatePolicyActive();
#else
                if (!ShouldUseLegacyCertificatePolicy())
                    return IsServerCertificateValidationCallbackActive();
                else
                    return IsLegacyCertificatePolicyActive();
#endif
#else
                return false;
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetTrusted(
            bool trusted,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool wasTrusted = IsTrusted();

                if (trusted != wasTrusted)
                {
                    try
                    {
#if !NET_STANDARD_20
#if !MONO
                        if (!ShouldUseLegacyCertificatePolicy())
                        {
                            //
                            // NOTE: When using the .NET Framework, use the
                            //       newer certification validation callback
                            //       interface.
                            //
                            if (trusted)
                                AddServerCertificateValidationCallback();
                            else
                                RemoveServerCertificateValidationCallback();

                            TraceOps.DebugTrace(String.Format(
                                "SetTrusted: {0} " +
                                "RemoteCertificateValidationCallback",
                                trusted ? "added" : "removed"),
                                typeof(UpdateOps).Name,
                                TracePriority.SecurityDebug);
                        }
                        else
#endif
                        {
                            //
                            // NOTE: When running on Mono, fallback to the
                            //       "obsolete" CertificatePolicy property.
                            //
                            if (trusted)
                                EnableLegacyCertificatePolicy();
                            else
                                DisableLegacyCertificatePolicy();

                            TraceOps.DebugTrace(String.Format(
                                "SetTrusted: {0} CertificatePolicy",
                                trusted ? "overridden" : "restored"),
                                typeof(UpdateOps).Name,
                                TracePriority.SecurityDebug);
                        }

                        return ReturnCode.Ok;
#else
                        error = "not implemented";
#endif
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = String.Format(
                        "already {0}", trusted ?
                            "trusted" : "untrusted");
                }
            }

            TraceOps.DebugTrace(String.Format(
                "SetTrusted: trusted = {0}, error = {1}",
                trusted, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ServerCertificateValidationCallback Support Methods
#if !NET_STANDARD_20
        private static bool RemoteCertificateValidationCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
            )
        {
            //
            // NOTE: Permit all X.509 certificates that are considered to
            //       to be valid by the platform itself (i.e. they do not
            //       have an error status).  If exclusive mode is enabled,
            //       this will be skipped.
            //
            lock (exclusiveSyncRoot) /* TRANSACTIONAL */
            {
                if (!exclusive &&
                    (sslPolicyErrors == SslPolicyErrors.None))
                {
                    return true;
                }
            }

            //
            // NOTE: Emit diagnostic message with the certificate
            //       status information as this can be quite useful
            //       when troubleshooting.
            //
            TraceOps.DebugTrace(String.Format(
                "RemoteCertificateValidationCallback: certificate = {0}, " +
                "exclusive = {1}, sslPolicyErrors = {2}",
                FormatOps.Certificate(certificate, false, true),
                exclusive, FormatOps.WrapOrNull(sslPolicyErrors)),
                typeof(UpdateOps).Name, TracePriority.SecurityError);

            //
            // NOTE: If this ServerCertificateValidationCallback is being
            //       called when it should not be active, then it's not
            //       supposed to be "always trusted" right now; therefore,
            //       just return false.
            //
            if (!IsServerCertificateValidationCallbackActive())
                return false;

            return IsTrustedCertificate(certificate);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsServerCertificateValidationCallbackActive()
        {
            lock (certificateCallbackSyncRoot)
            {
                return (
                    ServicePointManager.ServerCertificateValidationCallback
                        == RemoteCertificateValidationCallback
                );
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddServerCertificateValidationCallback()
        {
            lock (certificateCallbackSyncRoot)
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    RemoteCertificateValidationCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void RemoveServerCertificateValidationCallback()
        {
            lock (certificateCallbackSyncRoot)
            {
                ServicePointManager.ServerCertificateValidationCallback -=
                    RemoteCertificateValidationCallback;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Class & Methods
#if !NET_STANDARD_20
        #region ICertificatePolicy Support Class
        [ObjectId("4062e197-ed96-4db3-87e8-f463e5fb818b")]
        private sealed class CertificatePolicy : ICertificatePolicy
        {
            #region ICertificatePolicy Members
            public bool CheckValidationResult(
                ServicePoint srvPoint,
                X509Certificate certificate,
                WebRequest request,
                int certificateProblem
                )
            {
                //
                // NOTE: Unless exclusive mode is enabled, permit all
                //       X.509 certificates that are considered to to
                //       be valid by the platform itself (i.e. they do
                //       not have an error status).
                //
                lock (exclusiveSyncRoot) /* TRANSACTIONAL */
                {
                    if (!exclusive && (certificateProblem == 0))
                        return true;
                }

                //
                // NOTE: Emit diagnostic message with the certificate
                //       status information as this can be quite useful
                //       when troubleshooting.
                //
                TraceOps.DebugTrace(String.Format(
                    "CheckValidationResult: certificate = {0}, " +
                    "exclusive = {1}, certificateProblem = {2}",
                    FormatOps.Certificate(certificate, false, true),
                    exclusive, certificateProblem), typeof(UpdateOps).Name,
                    TracePriority.SecurityError);

                //
                // NOTE: If this ICertificatePolicy is being called when it
                //       should not be active, then it's not supposed to be
                //       "always trusted" right now; therefore, just return
                //       false.
                //
                if (!IsLegacyCertificatePolicyActive())
                    return false;

                return IsTrustedCertificate(certificate);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Methods
        private static bool IsLegacyCertificatePolicyActive()
        {
            lock (certificatePolicySyncRoot) /* TRANSACTIONAL */
            {
                return Object.ReferenceEquals(
                    ServicePointManager.CertificatePolicy, certificatePolicy);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void EnableLegacyCertificatePolicy()
        {
            lock (certificatePolicySyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: First, save the current certificate policy for
                //       possible later restoration.
                //
                savedCertificatePolicy = ServicePointManager.CertificatePolicy;
                haveSavedCertificatePolicy = true;

                //
                // NOTE: Next, set the certificate policy to the one we
                //       use for software updates.
                //
                ServicePointManager.CertificatePolicy = certificatePolicy;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DisableLegacyCertificatePolicy()
        {
            lock (certificatePolicySyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Restore the previously saved certificate policy,
                //       if any.
                //
                if (!haveSavedCertificatePolicy)
                    return;

                //
                // NOTE: Restore the saved ICertificatePolicy.
                //
                ServicePointManager.CertificatePolicy = savedCertificatePolicy;

                //
                // NOTE: Clear the saved ICertificatePolicy.
                //
                haveSavedCertificatePolicy = false;
                savedCertificatePolicy = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        private static bool ShouldUseLegacyCertificatePolicy()
        {
            return useLegacyCertificatePolicy ||
                CommonOps.Runtime.IsMono();
        }
#endif
        #endregion
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Trusted Certificate Support Methods
#if !NET_STANDARD_20
        private static void GetPublicKeys(
            ref StringList list
            )
        {
            if (PublicKey1 != null)
            {
                if (list == null)
                    list = new StringList();

                list.Add("updates(publicKey1)");

                list.Add(Convert.ToBase64String(PublicKey1,
                    Base64FormattingOptions.InsertLineBreaks));
            }

            ///////////////////////////////////////////////////////////////////

            if (PublicKey2 != null)
            {
                if (list == null)
                    list = new StringList();

                list.Add("updates(publicKey2)");

                list.Add(Convert.ToBase64String(PublicKey2,
                    Base64FormattingOptions.InsertLineBreaks));
            }

            ///////////////////////////////////////////////////////////////////

            if (PublicKey3 != null)
            {
                if (list == null)
                    list = new StringList();

                list.Add("updates(publicKey3)");

                list.Add(Convert.ToBase64String(PublicKey3,
                    Base64FormattingOptions.InsertLineBreaks));
            }

            ///////////////////////////////////////////////////////////////////

            if (PublicKey4 != null)
            {
                if (list == null)
                    list = new StringList();

                list.Add("updates(publicKey4)");

                list.Add(Convert.ToBase64String(PublicKey4,
                    Base64FormattingOptions.InsertLineBreaks));
            }

            ///////////////////////////////////////////////////////////////////

            if (PublicKey5 != null)
            {
                if (list == null)
                    list = new StringList();

                list.Add("updates(publicKey5)");

                list.Add(Convert.ToBase64String(PublicKey5,
                    Base64FormattingOptions.InsertLineBreaks));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsTrustedCertificate(
            X509Certificate certificate
            )
        {
            bool result = false;
            string name = null;

            //
            // NOTE: Make sure the certificate public key matches what
            //       we expect it to be for our own software updates.
            //
            if (certificate != null)
            {
                //
                // NOTE: Grab the public key of the certificate.
                //
                byte[] certificatePublicKey = certificate.GetPublicKey();

                if ((certificatePublicKey != null) &&
                    (certificatePublicKey.Length > 0))
                {
                    lock (publicKeySyncRoot) /* TRANSACTIONAL */
                    {
                        //
                        // NOTE: Compare the public key of the certificate to
                        //       one(s) that we trust for our software updates.
                        //
                        if (!result &&
                            (PublicKey1 != null) && (PublicKey1.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey1))
                        {
                            name = "PublicKey1";
                            result = true;
                        }

                        if (!result &&
                            (PublicKey2 != null) && (PublicKey2.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey2))
                        {
                            name = "PublicKey2";
                            result = true;
                        }

                        if (!result &&
                            (PublicKey3 != null) && (PublicKey3.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey3))
                        {
                            name = "PublicKey3";
                            result = true;
                        }

                        if (!result &&
                            (PublicKey4 != null) && (PublicKey4.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey4))
                        {
                            name = "PublicKey4";
                            result = true;
                        }

                        //
                        // NOTE: Compare the public key of the certificate to
                        //       the auxiliary one that we trust for use by
                        //       third-party applications and plugins.
                        //
                        if (!result &&
                            (PublicKey5 != null) && (PublicKey5.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey5))
                        {
                            name = "PublicKey5";
                            result = true;
                        }
                    }
                }
            }

            //
            // NOTE: Report this trust result to any trace listeners.
            //
            TraceOps.DebugTrace(String.Format(
                "IsTrustedCertificate: certificate = {0}, name = {1}, " +
                "result = {2}", FormatOps.Certificate(certificate, false,
                true), FormatOps.WrapOrNull(name), result),
                typeof(UpdateOps).Name, TracePriority.SecurityDebug);

            return result;
        }
#endif
        #endregion
    }
}
