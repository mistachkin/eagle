/*
 * StrongNameEx.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !NATIVE || !WINDOWS
#error "This file cannot be compiled or used properly with native Windows code disabled."
#endif

using System;
using System.Runtime.InteropServices;

#if (!DEBUG && !MONO) || NET_40 || NET_STANDARD_20
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides helper methods used to verify the strong name
    /// signature of a managed assembly file via the native runtime.
    /// </summary>
#if NET_40 || NET_STANDARD_20
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [Guid("32870a6b-fc20-43ae-80b7-42d684680d98")]
    internal static class StrongNameEx
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
#if !DEBUG && !MONO
        /// <summary>
        /// This class contains the native APIs used, via P/Invoke, to verify
        /// the strong name signature of a managed assembly file.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [Guid("0748329a-8ab7-46ba-80da-01b0785b3cbe")]
        private static class UnsafeNativeMethods
        {
            //
            // NOTE: This is the file name for the "Microsoft COM Object
            //       Runtime Execution Engine" and it should be available
            //       on the real .NET Framework (all versions).  This is
            //       not available on Mono.
            //
            /// <summary>
            /// The native module file name that exports the strong name
            /// verification API.
            /// </summary>
            public const string MsCorEe = "mscoree.dll";

            /// <summary>
            /// This method verifies the strong name signature of the specified
            /// managed assembly file.
            /// </summary>
            /// <param name="filePath">
            /// The path to the managed assembly file to verify.
            /// </param>
            /// <param name="forceVerification">
            /// Non-zero to force verification even when it would normally be
            /// skipped.
            /// </param>
            /// <param name="wasVerified">
            /// Upon return, this parameter will be non-zero if the strong name
            /// signature was actually verified.
            /// </param>
            /// <returns>
            /// Non-zero if the strong name signature is valid; otherwise,
            /// zero.
            /// </returns>
            [DllImport(MsCorEe,
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.U1)]
            internal static extern bool StrongNameSignatureVerificationEx(
                [MarshalAs(UnmanagedType.LPWStr)] string filePath,
                [MarshalAs(UnmanagedType.U1)] bool forceVerification,
                [MarshalAs(UnmanagedType.U1)] ref bool wasVerified
            );
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the specified managed assembly file
        /// has a valid strong name signature.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing.
        /// </param>
        /// <param name="fileName">
        /// The path to the managed assembly file to verify.
        /// </param>
        /// <param name="force">
        /// Non-zero to force verification even when it would normally be
        /// skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the strong name signature is valid; otherwise, false.
        /// </returns>
        public static bool IsStrongNameSigned(
            Configuration configuration,
            string fileName,
            bool force,
            ref string error
            )
        {
#if !DEBUG
            bool returnValue = false;
            bool verified = false;
            string localError = null;

            if ((IsStrongNameSigned(
                    fileName, force, ref returnValue, ref verified,
                    ref localError)) &&
                returnValue && verified)
            {
                return true;
            }
            else
            {
                if (localError != null)
                    error = localError;
                else
                    error = "StrongNameSignatureVerificationEx() failed.";

                return false;
            }
#else
            //
            // NOTE: Emit a log entry so that the user knows for sure
            //       that we did NOT actually verify the strong name
            //       signature.
            //
            TraceOps.Trace(configuration, String.Format(
                "File \"{0}\" strong name unchecked: " +
                "StrongNameSignatureVerificationEx use is disabled.",
                fileName), typeof(StrongNameEx).Name);

            //
            // NOTE: In-development version, fake it.  We can do this
            //       because DEBUG builds are never officially released.
            //
            return true;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
#if !DEBUG
        /// <summary>
        /// This method verifies the strong name signature of the specified
        /// managed assembly file via the native runtime.
        /// </summary>
        /// <param name="fileName">
        /// The path to the managed assembly file to verify.
        /// </param>
        /// <param name="force">
        /// Non-zero to force verification even when it would normally be
        /// skipped.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, this parameter will be non-zero if the native
        /// verification API reported that the signature is valid.
        /// </param>
        /// <param name="verified">
        /// Upon return, this parameter will be non-zero if the strong name
        /// signature was actually verified.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the native verification API was invoked successfully;
        /// otherwise, false.
        /// </returns>
        private static bool IsStrongNameSigned(
            string fileName,
            bool force,
            ref bool returnValue,
            ref bool verified,
            ref string error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return false;
            }

#if !MONO
            if (!VersionOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return false;
            }

            try
            {
                returnValue =
                    UnsafeNativeMethods.StrongNameSignatureVerificationEx(
                        fileName, force, ref verified);

                return true;
            }
            catch (Exception e)
            {
                error = e.ToString();
            }
#else
            error = "not implemented";
#endif

            return false;
        }
#endif
        #endregion
    }
}
