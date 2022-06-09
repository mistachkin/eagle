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
            public const string MsCorEe = "mscoree.dll";

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
