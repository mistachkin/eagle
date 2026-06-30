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
using System.Net;
using System.Net.Security;

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
using System.Runtime.CompilerServices;
#endif

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using _PublicKey = Eagle._Components.Shared.PublicKey;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the support methods used by the software update
    /// subsystem.  It holds the set of public keys that are trusted for
    /// signing core library updates and manages the X.509 certificate
    /// validation policy (both the modern callback-based mechanism and the
    /// legacy <c>ICertificatePolicy</c> mechanism) used when
    /// downloading updates over a secure connection.
    /// </summary>
    [ObjectId("711b1e60-8516-4f41-ba61-89c48f904d0a")]
    internal static class UpdateOps
    {
        #region Private Static Data
        #region Trusted Public Key Data
        //
        // NOTE: This lock is used to synchronize access to the static fields
        //       "PublicKey1", "PublicKey2", "PublicKey3", "PublicKey4", and
        //       "PublicKey5".
        //
        /// <summary>
        /// The object used to synchronize access to the trusted public key
        /// fields.
        /// </summary>
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
        /// <summary>
        /// The first public key trusted for signing core library updates; this
        /// is the legacy (2048-bit) key trusted by the majority of published
        /// Eagle builds.  Setting it to null disables its use.
        /// </summary>
        private static byte[] PublicKey1 = _PublicKey.SoftwareUpdate1;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        /// <summary>
        /// A second public key trusted for signing core library updates; this
        /// key is only recognized by builds of Eagle that are Beta 32 or later.
        /// Setting it to null disables its use.
        /// </summary>
        private static byte[] PublicKey2 = _PublicKey.SoftwareUpdate2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        /// <summary>
        /// A third public key trusted for signing core library updates; this
        /// key is only recognized by builds of Eagle that are Beta 32 or later.
        /// Setting it to null disables its use.
        /// </summary>
        private static byte[] PublicKey3 = _PublicKey.SoftwareUpdate3;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        /// <summary>
        /// A fourth public key trusted for signing core library updates; this
        /// key is only recognized by builds of Eagle that are Beta 32 or later.
        /// Setting it to null disables its use.
        /// </summary>
        private static byte[] PublicKey4 = _PublicKey.SoftwareUpdate4;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is RESERVED for use by third-party plugins
        //       and applications; however, it is not public because it is
        //       not intended to be used lightly.
        //
        /// <summary>
        /// An auxiliary public key reserved for use by third-party plugins and
        /// applications; it is null (and therefore unused) by default.
        /// </summary>
        private static byte[] PublicKey5 = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Certificate Support
        //
        // HACK: Which thread currently holds the static lock?
        //
        /// <summary>
        /// The identifier of the thread that currently holds the trusted state
        /// lock, or zero if it is not held.
        /// </summary>
        private static long trustedLockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This lock is used to synchronize access to the trusted
        //       state, e.g. callbacks, etc.
        //
        /// <summary>
        /// The object used to synchronize access to the trusted state (e.g. the
        /// certificate validation callbacks).
        /// </summary>
        private static readonly object trustedSyncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Exclusive Mode Support
        //
        // HACK: Which thread currently holds the static lock?
        //
        /// <summary>
        /// The identifier of the thread that currently holds the exclusive
        /// mode lock, or zero if it is not held.
        /// </summary>
        private static long exclusiveLockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This lock is used to synchronize access to the static field
        //       "exclusive".
        //
        /// <summary>
        /// The object used to synchronize access to the exclusive mode flag.
        /// </summary>
        private static readonly object exclusiveSyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the "exclusive" mode flag used with the "trusted"
        //       certificate status flag.
        //
        /// <summary>
        /// When non-zero, exclusive mode is enabled, which causes even
        /// platform-valid certificates to be subjected to the trusted public
        /// key check rather than being accepted automatically.
        /// </summary>
        private static bool exclusive = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Data
#if !NET_STANDARD_20
        //
        // HACK: This is purposely not read-only; however, it is logically a
        //       constant.
        //
        /// <summary>
        /// When non-zero, the legacy <c>ICertificatePolicy</c>
        /// mechanism is used instead of the modern certificate validation
        /// callback; it is logically a constant.
        /// </summary>
        private static bool useLegacyCertificatePolicy = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Which thread currently holds the static lock?
        //
        /// <summary>
        /// The identifier of the thread that currently holds the certificate
        /// policy lock, or zero if it is not held.
        /// </summary>
        private static long certificatePolicyLockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This lock is used to synchronize access to the static fields
        //       "savedCertificatePolicy", "haveSavedCertificatePolicy", and
        //       "certificatePolicy".
        //
        /// <summary>
        /// The object used to synchronize access to the saved and active
        /// certificate policy fields.
        /// </summary>
        private static readonly object certificatePolicySyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The certificate policy that was in effect before the legacy policy
        /// was enabled, saved so that it can be restored later.
        /// </summary>
        private static ICertificatePolicy savedCertificatePolicy;
        /// <summary>
        /// When non-zero, a previous certificate policy has been saved in
        /// <see cref="savedCertificatePolicy" /> and can be restored.
        /// </summary>
        private static bool haveSavedCertificatePolicy;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The shared <c>ICertificatePolicy</c> instance used to
        /// validate certificates when the legacy policy mechanism is active.
        /// </summary>
        private static readonly ICertificatePolicy certificatePolicy =
            new CertificatePolicy();
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ServerCertificateValidationCallback Support Data
        //
        // HACK: Which thread currently holds the static lock?
        //
        /// <summary>
        /// The identifier of the thread that currently holds the certificate
        /// validation callback lock, or zero if it is not held.
        /// </summary>
        private static long callbackLockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This lock is used to synchronize access to the property
        //       "ServicePointManager.ServerCertificateValidationCallback".
        //
        /// <summary>
        /// The object used to synchronize access to the
        /// <c>ServicePointManager.ServerCertificateValidationCallback</c>
        /// property.
        /// </summary>
        private static readonly object callbackSyncRoot = new object();
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        /// <summary>
        /// This method returns the identifier of the thread that currently
        /// holds the trusted state lock.
        /// </summary>
        /// <returns>
        /// The identifier of the thread holding the lock, or zero if it is not
        /// held.
        /// </returns>
        private static long MaybeWhoHasTrustedLock()
        {
            return Interlocked.CompareExchange(
                ref trustedLockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the current thread holds the trusted state
        /// lock, but only when the lock was actually acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the trusted state lock was acquired by the current
        /// thread.
        /// </param>
        private static void MaybeSomebodyHasTrustedLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref trustedLockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that no thread holds the trusted state lock, but
        /// only when the current thread is about to release it.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the trusted state lock is currently held by the current
        /// thread.
        /// </param>
        private static void MaybeNobodyHasTrustedLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref trustedLockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the trusted state lock without
        /// blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is non-zero if the trusted state lock was acquired
        /// by the current thread.
        /// </param>
        public static void TryTrustedLock(
            ref bool locked /* out */
            )
        {
            if (trustedSyncRoot == null)
                return;

            locked = Monitor.TryEnter(trustedSyncRoot);
            MaybeSomebodyHasTrustedLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the trusted state lock when it is currently
        /// held by the current thread.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the trusted state lock is held by the
        /// current thread; upon return, this is zero.
        /// </param>
        public static void ExitTrustedLock(
            ref bool locked /* in, out */
            )
        {
            if (trustedSyncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasTrustedLock(locked);
                Monitor.Exit(trustedSyncRoot);
                locked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the identifier of the thread that currently
        /// holds the exclusive mode lock.
        /// </summary>
        /// <returns>
        /// The identifier of the thread holding the lock, or zero if it is not
        /// held.
        /// </returns>
        private static long MaybeWhoHasExclusiveLock()
        {
            return Interlocked.CompareExchange(
                ref exclusiveLockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the current thread holds the exclusive mode
        /// lock, but only when the lock was actually acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the exclusive mode lock was acquired by the current
        /// thread.
        /// </param>
        private static void MaybeSomebodyHasExclusiveLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref exclusiveLockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that no thread holds the exclusive mode lock,
        /// but only when the current thread is about to release it.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the exclusive mode lock is currently held by the current
        /// thread.
        /// </param>
        private static void MaybeNobodyHasExclusiveLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref exclusiveLockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the exclusive mode lock without
        /// blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is non-zero if the exclusive mode lock was
        /// acquired by the current thread.
        /// </param>
        private static void TryExclusiveLock(
            ref bool locked /* out */
            )
        {
            if (exclusiveSyncRoot == null)
                return;

            locked = Monitor.TryEnter(exclusiveSyncRoot);
            MaybeSomebodyHasExclusiveLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the exclusive mode lock when it is currently
        /// held by the current thread.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the exclusive mode lock is held by the
        /// current thread; upon return, this is zero.
        /// </param>
        private static void ExitExclusiveLock(
            ref bool locked /* in, out */
            )
        {
            if (exclusiveSyncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasExclusiveLock(locked);
                Monitor.Exit(exclusiveSyncRoot);
                locked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// This method returns the identifier of the thread that currently
        /// holds the certificate policy lock.
        /// </summary>
        /// <returns>
        /// The identifier of the thread holding the lock, or zero if it is not
        /// held.
        /// </returns>
        private static long MaybeWhoHasCertificatePolicyLock()
        {
            return Interlocked.CompareExchange(
                ref certificatePolicyLockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the current thread holds the certificate
        /// policy lock, but only when the lock was actually acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the certificate policy lock was acquired by the current
        /// thread.
        /// </param>
        private static void MaybeSomebodyHasCertificatePolicyLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref certificatePolicyLockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that no thread holds the certificate policy
        /// lock, but only when the current thread is about to release it.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the certificate policy lock is currently held by the
        /// current thread.
        /// </param>
        private static void MaybeNobodyHasCertificatePolicyLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref certificatePolicyLockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the certificate policy lock without
        /// blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is non-zero if the certificate policy lock was
        /// acquired by the current thread.
        /// </param>
        private static void TryCertificatePolicyLock(
            ref bool locked /* out */
            )
        {
            if (certificatePolicySyncRoot == null)
                return;

            locked = Monitor.TryEnter(certificatePolicySyncRoot);
            MaybeSomebodyHasCertificatePolicyLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the certificate policy lock when it is
        /// currently held by the current thread.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the certificate policy lock is held by the
        /// current thread; upon return, this is zero.
        /// </param>
        private static void ExitCertificatePolicyLock(
            ref bool locked /* in, out */
            )
        {
            if (certificatePolicySyncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasCertificatePolicyLock(locked);
                Monitor.Exit(certificatePolicySyncRoot);
                locked = false;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the identifier of the thread that currently
        /// holds the certificate validation callback lock.
        /// </summary>
        /// <returns>
        /// The identifier of the thread holding the lock, or zero if it is not
        /// held.
        /// </returns>
        private static long MaybeWhoHasCallbackLock()
        {
            return Interlocked.CompareExchange(
                ref callbackLockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the current thread holds the certificate
        /// validation callback lock, but only when the lock was actually
        /// acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the certificate validation callback lock was acquired by
        /// the current thread.
        /// </param>
        private static void MaybeSomebodyHasCallbackLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref callbackLockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that no thread holds the certificate validation
        /// callback lock, but only when the current thread is about to release
        /// it.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the certificate validation callback lock is currently
        /// held by the current thread.
        /// </param>
        private static void MaybeNobodyHasCallbackLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref callbackLockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the certificate validation callback
        /// lock without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is non-zero if the certificate validation callback
        /// lock was acquired by the current thread.
        /// </param>
        private static void TryCallbackLock(
            ref bool locked /* out */
            )
        {
            if (callbackSyncRoot == null)
                return;

            locked = Monitor.TryEnter(callbackSyncRoot);
            MaybeSomebodyHasCallbackLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the certificate validation callback lock when
        /// it is currently held by the current thread.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the certificate validation callback lock is
        /// held by the current thread; upon return, this is zero.
        /// </param>
        private static void ExitCallbackLock(
            ref bool locked /* in, out */
            )
        {
            if (callbackSyncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasCallbackLock(locked);
                Monitor.Exit(callbackSyncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Introspection Methods
        /// <summary>
        /// This method appends a human-readable summary of the current state
        /// of the software update subsystem (e.g. trusted status, exclusive
        /// mode, trusted public keys, and the active certificate policy) to the
        /// specified list, for introspection purposes.
        /// </summary>
        /// <param name="list">
        /// The list to which the status information is appended; it is created
        /// first if it is null.
        /// </param>
        public static void GetStatus(
            ref StringList list /* in, out */
            )
        {
            if (list == null)
                list = new StringList();

            list.Add("updates(trusted)");
            list.Add(FormatOps.MaybeNull(IsTrusted()).ToString());

            list.Add("updates(exclusive)");
            list.Add(FormatOps.MaybeNull(IsExclusive()).ToString());

            GetPublicKeys(ref list);

#if !NET_STANDARD_20
            list.Add("updates(legacyActive)");
            list.Add(FormatOps.MaybeNull(
                IsLegacyCertificatePolicyActive()).ToString());
#endif

            list.Add("updates(modernActive)");
            list.Add(FormatOps.MaybeNull(
                IsServerCertificateValidationCallbackActive()).ToString());

            list.Add("updates(useLegacy)");
            list.Add(ShouldUseLegacyCertificatePolicy().ToString());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Exclusive Mode Support Methods
        /// <summary>
        /// This method determines whether exclusive mode is currently enabled.
        /// </summary>
        /// <returns>
        /// Non-zero if exclusive mode is enabled, zero if it is disabled, or
        /// null if the exclusive mode lock could not be acquired.
        /// </returns>
        public static bool? IsExclusive()
        {
            bool locked = false;

            try
            {
                TryExclusiveLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "IsExclusive",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasExclusiveLock());

                    return null;
                }

                return exclusive;
            }
            finally
            {
                ExitExclusiveLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables exclusive mode.
        /// </summary>
        /// <param name="exclusive">
        /// Non-zero to enable exclusive mode; zero to disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SetExclusive(
            bool exclusive,  /* in */
            ref Result error /* out */
            )
        {
            bool locked = false;

            try
            {
                TryExclusiveLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "SetExclusive",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasExclusiveLock());

                    goto error;
                }

                bool? wasExclusive = IsExclusive();

                if (wasExclusive == null)
                {
                    error = "exclusive mode status unknown";
                    goto error;
                }

                if (exclusive == (bool)wasExclusive)
                {
                    error = String.Format(
                        "already {0} mode", exclusive ?
                            "exclusive" : "non-exclusive");

                    goto error;
                }

                UpdateOps.exclusive = exclusive;

                TraceOps.DebugTrace(String.Format(
                    "SetExclusive: exclusive mode {0}",
                    exclusive ? "enabled" : "disabled"),
                    typeof(UpdateOps).Name,
                    TracePriority.SecurityDebug);

                return ReturnCode.Ok;
            }
            finally
            {
                ExitExclusiveLock(ref locked);
            }

        error:

            TraceOps.DebugTrace(String.Format(
                "SetExclusive: exclusive = {0}, error = {1}",
                exclusive, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Trusted Status Support Methods
        /// <summary>
        /// This method determines whether the software update trusted status is
        /// currently enabled, dispatching to either the legacy or modern
        /// implementation as appropriate.
        /// </summary>
        /// <returns>
        /// Non-zero if the trusted status is enabled, zero if it is disabled,
        /// or null if the status could not be determined.
        /// </returns>
        public static bool? IsTrusted()
        {
            bool? useLegacy = ShouldUseLegacyCertificatePolicy();

            if (useLegacy == null)
                return null;

            if ((bool)useLegacy)
            {
#if !NET_STANDARD_20
                return IsTrustedLegacy();
#else
                return null;
#endif
            }

            return IsTrustedModern();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the software update trusted status,
        /// dispatching to either the legacy or modern implementation as
        /// appropriate.
        /// </summary>
        /// <param name="trusted">
        /// Non-zero to enable the trusted status; zero to disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SetTrusted(
            bool trusted,    /* in */
            ref Result error /* out */
            )
        {
            bool? useLegacy = ShouldUseLegacyCertificatePolicy(
                ref error);

            if (useLegacy == null)
                return ReturnCode.Error;

            if ((bool)useLegacy)
            {
#if !NET_STANDARD_20
                return SetTrustedLegacy(trusted, ref error);
#else
                error = "not implemented";
                return ReturnCode.Error;
#endif
            }

            return SetTrustedModern(trusted, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Modern Support Methods
        /// <summary>
        /// This method is the modern certificate validation callback used when
        /// downloading software updates.  It accepts platform-valid
        /// certificates (unless exclusive mode is enabled), exempts loopback
        /// connections, and otherwise accepts only certificates whose public
        /// key is one of the trusted update keys.
        /// </summary>
        /// <param name="sender">
        /// The object that initiated the request being validated.
        /// </param>
        /// <param name="certificate">
        /// The certificate presented by the remote party.
        /// </param>
        /// <param name="chain">
        /// The chain of certificate authorities associated with the remote
        /// certificate.
        /// </param>
        /// <param name="sslPolicyErrors">
        /// The policy errors detected by the platform for the remote
        /// certificate.
        /// </param>
        /// <returns>
        /// True if the certificate should be accepted; otherwise, false.
        /// </returns>
        private static bool RemoteCertificateValidationCallback(
            object sender,                  /* in */
            X509Certificate certificate,    /* in */
            X509Chain chain,                /* in */
            SslPolicyErrors sslPolicyErrors /* in */
            )
        {
            //
            // NOTE: Permit all X.509 certificates that are considered to
            //       to be valid by the platform itself (i.e. they do not
            //       have an error status).  If exclusive mode is enabled,
            //       this will be skipped.
            //
            bool? wasExclusive = IsExclusive();

            if (wasExclusive == null)
                return false;

            if (!(bool)wasExclusive &&
                (sslPolicyErrors == SslPolicyErrors.None))
            {
                return true;
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
            bool? wasActive = IsServerCertificateValidationCallbackActive();

            if ((wasActive == null) || !(bool)wasActive)
                return false;

            //
            // HACK: When the policy is active, make all local host
            //       connections exempt for development and testing
            //       purposes.
            //
            HttpWebRequest request = sender as HttpWebRequest;

            if (request != null)
            {
                Uri uri = request.RequestUri;

                if ((uri != null) && uri.IsLoopback)
                    return true;
            }

            return IsTrustedCertificate(certificate);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the modern certificate validation
        /// callback is currently installed as the
        /// <c>ServicePointManager.ServerCertificateValidationCallback</c>.
        /// </summary>
        /// <returns>
        /// Non-zero if the callback is active, zero if it is not, or null if
        /// the certificate validation callback lock could not be acquired.
        /// </returns>
        private static bool? IsServerCertificateValidationCallbackActive()
        {
            bool locked = false;

            try
            {
                TryCallbackLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "IsServerCertificateValidationCallbackActive",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasCallbackLock());

                    return null;
                }

                return (
                    ServicePointManager.ServerCertificateValidationCallback
                        == RemoteCertificateValidationCallback
                );
            }
            finally
            {
                ExitCallbackLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method installs the modern certificate validation callback as
        /// the <c>ServicePointManager.ServerCertificateValidationCallback</c>.
        /// </summary>
        /// <returns>
        /// True if the callback was installed; otherwise, false.
        /// </returns>
        private static bool AddServerCertificateValidationCallback()
        {
            bool locked = false;

            try
            {
                TryCallbackLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "AddServerCertificateValidationCallback",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasCallbackLock());

                    return false;
                }

                ServicePointManager.ServerCertificateValidationCallback +=
                    RemoteCertificateValidationCallback;

                return true;
            }
            finally
            {
                ExitCallbackLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the modern certificate validation callback from
        /// the <c>ServicePointManager.ServerCertificateValidationCallback</c>.
        /// </summary>
        /// <returns>
        /// True if the callback was removed; otherwise, false.
        /// </returns>
        private static bool RemoveServerCertificateValidationCallback()
        {
            bool locked = false;

            try
            {
                TryCallbackLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "RemoveServerCertificateValidationCallback",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasCallbackLock());

                    return false;
                }

                ServicePointManager.ServerCertificateValidationCallback -=
                    RemoteCertificateValidationCallback;

                return true;
            }
            finally
            {
                ExitCallbackLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private Trusted Status Support Methods
        /// <summary>
        /// This method determines whether the trusted status is enabled using
        /// the modern certificate validation callback mechanism.
        /// </summary>
        /// <returns>
        /// Non-zero if the trusted status is enabled, zero if it is disabled,
        /// or null if the status could not be determined.
        /// </returns>
        private static bool? IsTrustedModern()
        {
            bool locked = false;

            try
            {
                TryTrustedLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "IsTrustedModern",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasTrustedLock());

                    return null;
                }

                try
                {
                    return IsServerCertificateValidationCallbackActive();
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(UpdateOps).Name,
                        TracePriority.SecurityError);

                    return null;
                }
            }
            finally
            {
                ExitTrustedLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the trusted status using the modern
        /// certificate validation callback mechanism.
        /// </summary>
        /// <param name="trusted">
        /// Non-zero to enable the trusted status; zero to disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode SetTrustedModern(
            bool trusted,    /* in */
            ref Result error /* out */
            )
        {
            bool locked = false;

            try
            {
                TryTrustedLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "SetTrustedModern",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasTrustedLock());

                    error = "unable to acquire lock";
                    goto error;
                }

                bool? wasTrusted = IsTrusted();

                if (wasTrusted == null)
                {
                    error = "trusted status unknown";
                    goto error;
                }

                if (trusted == (bool)wasTrusted)
                {
                    error = String.Format(
                        "already {0} status", trusted ?
                            "trusted" : "untrusted");

                    goto error;
                }

                try
                {
                    //
                    // NOTE: When using the .NET Framework, use the
                    //       newer certification validation callback
                    //       interface.
                    //
                    error = null;

                    if (trusted)
                    {
                        if (!AddServerCertificateValidationCallback())
                        {
                            error = "failed to add certificate " +
                                    "validation callback";
                        }
                    }
                    else
                    {
                        if (!RemoveServerCertificateValidationCallback())
                        {
                            error = "failed to remove certificate " +
                                    "validation callback";
                        }
                    }

                    if (error != null)
                        goto error;

                    TraceOps.DebugTrace(String.Format(
                        "SetTrustedModern: {0} " +
                            "RemoteCertificateValidationCallback",
                        trusted ? "added" : "removed"),
                        typeof(UpdateOps).Name,
                        TracePriority.SecurityDebug);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                ExitTrustedLock(ref locked);
            }

        error:

            TraceOps.DebugTrace(String.Format(
                "SetTrustedModern: trusted = {0}, error = {1}",
                trusted, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Legacy Support Class & Methods
#if !NET_STANDARD_20
        #region Private ICertificatePolicy Support Class
        /// <summary>
        /// This class implements the legacy <c>ICertificatePolicy</c>
        /// used to validate certificates when downloading software updates on
        /// platforms (such as Mono) where the modern certificate validation
        /// callback is unavailable or undesirable.
        /// </summary>
        [ObjectId("4062e197-ed96-4db3-87e8-f463e5fb818b")]
        private sealed class CertificatePolicy : ICertificatePolicy
        {
            #region ICertificatePolicy Members
            /// <summary>
            /// This method validates the certificate presented by a remote
            /// party.  It accepts platform-valid certificates (unless exclusive
            /// mode is enabled), exempts loopback connections, and otherwise
            /// accepts only certificates whose public key is one of the trusted
            /// update keys.
            /// </summary>
            /// <param name="srvPoint">
            /// The service point associated with the remote party.
            /// </param>
            /// <param name="certificate">
            /// The certificate presented by the remote party.
            /// </param>
            /// <param name="request">
            /// The web request being validated.
            /// </param>
            /// <param name="certificateProblem">
            /// The platform-defined problem code for the certificate; zero
            /// indicates no problem.
            /// </param>
            /// <returns>
            /// True if the certificate should be accepted; otherwise, false.
            /// </returns>
            public bool CheckValidationResult(
                ServicePoint srvPoint,       /* in */
                X509Certificate certificate, /* in */
                WebRequest request,          /* in */
                int certificateProblem       /* in */
                )
            {
                //
                // NOTE: Unless exclusive mode is enabled, permit all
                //       X.509 certificates that are considered to to
                //       be valid by the platform itself (i.e. they do
                //       not have an error status).
                //
                bool? wasExclusive = IsExclusive();

                if (wasExclusive == null)
                    return false;

                if (!(bool)wasExclusive && (certificateProblem == 0))
                    return true;

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
                bool? wasActive = IsLegacyCertificatePolicyActive();

                if ((wasActive == null) || !(bool)wasActive)
                    return false;

                //
                // HACK: When the legacy policy is active, make all local
                //       host connections exempt for development and test
                //       purposes.
                //
                if (request != null)
                {
                    Uri uri = request.RequestUri;

                    if ((uri != null) && uri.IsLoopback)
                        return true;
                }

                return IsTrustedCertificate(certificate);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private ICertificatePolicy Support Methods
        /// <summary>
        /// This method determines whether the legacy certificate policy is
        /// currently installed as the
        /// <c>ServicePointManager.CertificatePolicy</c>.
        /// </summary>
        /// <returns>
        /// Non-zero if the legacy policy is active, zero if it is not, or null
        /// if the certificate policy lock could not be acquired.
        /// </returns>
        private static bool? IsLegacyCertificatePolicyActive()
        {
            bool locked = false;

            try
            {
                TryCertificatePolicyLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "IsLegacyCertificatePolicyActive",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasCertificatePolicyLock());

                    return null;
                }

                return Object.ReferenceEquals(
                    ServicePointManager.CertificatePolicy,
                    certificatePolicy);
            }
            finally
            {
                ExitCertificatePolicyLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method installs the legacy certificate policy as the
        /// <c>ServicePointManager.CertificatePolicy</c>, first saving the
        /// previous policy so that it can be restored later.
        /// </summary>
        /// <returns>
        /// True if the legacy policy was installed; otherwise, false.
        /// </returns>
        private static bool EnableLegacyCertificatePolicy()
        {
            bool locked = false;

            try
            {
                TryCertificatePolicyLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "EnableLegacyCertificatePolicy",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasCertificatePolicyLock());

                    return false;
                }

                //
                // NOTE: First, save the current certificate
                //       policy for possible later restoration.
                //
                savedCertificatePolicy = ServicePointManager.CertificatePolicy;
                haveSavedCertificatePolicy = true;

                //
                // NOTE: Next, set the certificate policy to
                //       the one we use for software updates.
                //
                ServicePointManager.CertificatePolicy = certificatePolicy;

                return true;
            }
            finally
            {
                ExitCertificatePolicyLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the previously saved certificate policy,
        /// undoing the effect of <see cref="EnableLegacyCertificatePolicy" />.
        /// </summary>
        /// <returns>
        /// True if a previously saved policy was restored; otherwise, false.
        /// </returns>
        private static bool DisableLegacyCertificatePolicy()
        {
            bool locked = false;

            try
            {
                TryCertificatePolicyLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "DisableLegacyCertificatePolicy",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasCertificatePolicyLock());

                    return false;
                }

                //
                // NOTE: Restore the previously saved certificate
                //       policy, if any.
                //
                if (!haveSavedCertificatePolicy)
                    return false;

                //
                // NOTE: Restore the saved ICertificatePolicy.
                //
                ServicePointManager.CertificatePolicy = savedCertificatePolicy;

                //
                // NOTE: Clear the saved ICertificatePolicy.
                //
                haveSavedCertificatePolicy = false;
                savedCertificatePolicy = null;

                return true;
            }
            finally
            {
                ExitCertificatePolicyLock(ref locked);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Trusted Status Support Methods
        /// <summary>
        /// This method determines whether the trusted status is enabled using
        /// the legacy certificate policy mechanism.
        /// </summary>
        /// <returns>
        /// Non-zero if the trusted status is enabled, zero if it is disabled,
        /// or null if the status could not be determined.
        /// </returns>
        private static bool? IsTrustedLegacy()
        {
            bool locked = false;

            try
            {
                TryTrustedLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "IsTrustedLegacy",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasTrustedLock());

                    return null;
                }

                try
                {
                    return IsLegacyCertificatePolicyActive();
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(UpdateOps).Name,
                        TracePriority.SecurityError);

                    return null;
                }
            }
            finally
            {
                ExitTrustedLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the trusted status using the legacy
        /// certificate policy mechanism.
        /// </summary>
        /// <param name="trusted">
        /// Non-zero to enable the trusted status; zero to disable it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode SetTrustedLegacy(
            bool trusted,    /* in */
            ref Result error /* out */
            )
        {
            bool locked = false;

            try
            {
                TryTrustedLock(ref locked);

                if (!locked)
                {
                    TraceOps.LockTrace(
                        "SetTrustedLegacy",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasTrustedLock());

                    error = "unable to acquire lock";
                    goto error;
                }

                bool? wasTrusted = IsTrusted();

                if (wasTrusted == null)
                {
                    error = "trusted status unknown";
                    goto error;
                }

                if (trusted == (bool)wasTrusted)
                {
                    error = String.Format(
                        "already {0} status", trusted ?
                            "trusted" : "untrusted");

                    goto error;
                }

                try
                {
                    //
                    // NOTE: When running on Mono, fallback to the
                    //       "obsolete" CertificatePolicy property.
                    //
                    error = null;

                    if (trusted)
                    {
                        if (!EnableLegacyCertificatePolicy())
                        {
                            error = "failed to enable legacy " +
                                    "certificate policy";
                        }
                    }
                    else
                    {
                        if (!DisableLegacyCertificatePolicy())
                        {
                            error = "failed to disable legacy " +
                                    "certificate policy";
                        }
                    }

                    if (error != null)
                        goto error;

                    TraceOps.DebugTrace(String.Format(
                        "SetTrustedLegacy: {0} CertificatePolicy",
                        trusted ? "overridden" : "restored"),
                        typeof(UpdateOps).Name,
                        TracePriority.SecurityDebug);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                ExitTrustedLock(ref locked);
            }

        error:

            TraceOps.DebugTrace(String.Format(
                "SetTrustedLegacy: trusted = {0}, error = {1}",
                trusted, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Legacy Support Shared Methods
        /// <summary>
        /// This method determines whether the legacy certificate policy
        /// mechanism should be used instead of the modern certificate
        /// validation callback.
        /// </summary>
        /// <returns>
        /// Non-zero if the legacy mechanism should be used, zero if the modern
        /// mechanism should be used, or null if the decision could not be
        /// made.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool? ShouldUseLegacyCertificatePolicy()
        {
            Result error = null; /* NOT USED */

            return ShouldUseLegacyCertificatePolicy(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the legacy certificate policy
        /// mechanism should be used instead of the modern certificate
        /// validation callback.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero if the legacy mechanism should be used, zero if the modern
        /// mechanism should be used, or null if the decision could not be
        /// made.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool? ShouldUseLegacyCertificatePolicy(
            ref Result error /* out */
            )
        {
#if !NET_STANDARD_20
            //
            // HACK: Is this still (always) necessary on Mono?
            //
            if (CommonOps.Runtime.IsMono())
                return true;

            ///////////////////////////////////////////////////////////////////

            bool locked = false;

            try
            {
                TryTrustedLock(ref locked);

                if (locked)
                {
                    return useLegacyCertificatePolicy;
                }
                else
                {
                    TraceOps.LockTrace(
                        "ShouldUseLegacyCertificatePolicy",
                        typeof(UpdateOps).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasTrustedLock());

                    error = "unable to acquire lock";
                    return null;
                }
            }
            finally
            {
                ExitTrustedLock(ref locked);
            }
#else
            return false;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Trusted Certificate Support Methods
        /// <summary>
        /// This method appends the trusted update public keys (those that are
        /// not null), in Base64 form, to the specified list, for introspection
        /// purposes.
        /// </summary>
        /// <param name="list">
        /// The list to which the trusted public keys are appended; it is
        /// created first if it is null.
        /// </param>
        private static void GetPublicKeys(
            ref StringList list /* in, out */
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

        /// <summary>
        /// This method determines whether the public key of the specified
        /// certificate matches one of the public keys trusted for signing
        /// software updates.
        /// </summary>
        /// <param name="certificate">
        /// The certificate whose public key is to be checked.
        /// </param>
        /// <returns>
        /// True if the certificate's public key is trusted; otherwise, false.
        /// </returns>
        private static bool IsTrustedCertificate(
            X509Certificate certificate /* in */
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
        #endregion
    }
}
