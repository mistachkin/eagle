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

#if !NATIVE
#error "This file cannot be compiled or used properly with native code disabled."
#endif

using System;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("447e6857-c104-440a-9988-e04b1ec09066")]
    internal static class SecurityOps
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6d48f680-28c0-45e9-9469-0c8769dffd93")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            //
            // NOTE: For use with Windows NT and Windows 2000 RTM+
            //       (does not work properly with Vista).
            //
            [DllImport(DllName.AdvPack,
                CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsNTAdmin(
                uint reserved1,
                ref uint reserved2
            );

            //
            // NOTE: Imported by ordinal to support Windows 2000 SP4+, 
            //       Windows XP Home/Pro RTM+, and Vista (and hopefully 
            //       Windows Server 2008).
            //
            [DllImport(DllName.Shell32, EntryPoint = "#680",
                CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsUserAnAdmin();
#endif

            ///////////////////////////////////////////////////////////////////

#if UNIX
            /* NOTE: *POSIX* Cannot fail. */
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int getuid();
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        public static bool IsAdministrator()
        {
            bool administrator = false;

            if (IsAdministrator(ref administrator) != ReturnCode.Ok)
                return false;

            return administrator;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode IsAdministrator(
            ref bool administrator,
            ref Result error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsIsAdministrator(ref administrator, ref error);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixIsAdministrator(ref administrator, ref error);
#endif

            error = "unknown operating system";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Platform Abstraction Methods
        private static ReturnCode IsAdministrator(
            ref bool administrator
            )
        {
            Result error = null;

            return IsAdministrator(ref administrator, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Windows-Specific Methods
#if WINDOWS
        private static ReturnCode WindowsIsAdministrator(
            ref bool administrator,
            ref Result error
            )
        {
            try
            {
                //
                // NOTE: Are we running on Windows 2000 SP4 or higher?
                //
                if (PlatformOps.CheckVersion(PlatformID.Win32NT, 5, 0, 4, 0))
                {
                    //
                    // HACK: Use a "documented" function for Windows
                    //       2000 SP4+, Windows XP, and Vista (this
                    //       function used to be undocumented).
                    //
                    administrator = UnsafeNativeMethods.IsUserAnAdmin();
                }
                else
                {
                    //
                    // HACK: Use a different undocumented function for 
                    //       Windows NT and Windows 2000 RTM to SP3.
                    //
                    uint reserved2 = 0;

                    administrator = UnsafeNativeMethods.IsNTAdmin(
                        0, ref reserved2);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            TraceOps.DebugTrace(String.Format(
                "WindowsIsAdministrator: administrator = {0}, error = {1}",
                administrator, FormatOps.WrapOrNull(error)),
                typeof(SecurityOps).Name, CommonOps.Runtime.IsMono() ?
                TracePriority.SecurityError2 : TracePriority.SecurityError);

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Unix-Specific Methods
#if UNIX
        private static ReturnCode UnixIsAdministrator(
            ref bool administrator,
            ref Result error
            )
        {
            try
            {
                administrator = (UnsafeNativeMethods.getuid() == 0);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            TraceOps.DebugTrace(String.Format(
                "UnixIsAdministrator: administrator = {0}, error = {1}",
                administrator, FormatOps.WrapOrNull(error)),
                typeof(SecurityOps).Name, TracePriority.SecurityError);

            return ReturnCode.Error;
        }
#endif
        #endregion
    }
}
