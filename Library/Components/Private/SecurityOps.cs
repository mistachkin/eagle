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
    /// <summary>
    /// This class provides security-related helper methods, primarily the
    /// platform-specific logic used to determine whether the current process is
    /// running with administrative privileges.
    /// </summary>
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
        /// <summary>
        /// This class contains the private platform native method declarations,
        /// accessed via P/Invoke, that are used to query the administrative
        /// status of the current process.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6d48f680-28c0-45e9-9469-0c8769dffd93")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            //
            // NOTE: For use with Windows NT and Windows 2000 RTM+
            //       (does not work properly with Vista).
            //
            /// <summary>
            /// This method determines whether the current user is an
            /// administrator on Windows NT and Windows 2000 RTM through SP3.
            /// </summary>
            /// <param name="reserved1">
            /// Reserved; this parameter must be zero.
            /// </param>
            /// <param name="reserved2">
            /// Reserved; this parameter must be zero.
            /// </param>
            /// <returns>
            /// Non-zero if the current user is an administrator; otherwise,
            /// zero.
            /// </returns>
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
            /// <summary>
            /// This method determines whether the current user is a member of
            /// the local Administrators group.
            /// </summary>
            /// <returns>
            /// Non-zero if the current user is an administrator; otherwise,
            /// zero.
            /// </returns>
            [DllImport(DllName.Shell32, EntryPoint = "#680",
                CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsUserAnAdmin();
#endif

            ///////////////////////////////////////////////////////////////////

#if UNIX
            /// <summary>
            /// This method returns the real user identifier of the calling
            /// process.
            /// </summary>
            /// <returns>
            /// The real user identifier of the calling process.
            /// </returns>
            /* NOTE: *POSIX* Cannot fail. */
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int getuid();
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges, ignoring any error that may occur during
        /// the query.
        /// </summary>
        /// <returns>
        /// True if the current process is running with administrative
        /// privileges; otherwise, false.
        /// </returns>
        public static bool IsAdministrator()
        {
            bool administrator = false;

            if (IsAdministrator(ref administrator) != ReturnCode.Ok)
                return false;

            return administrator;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges, dispatching to the appropriate
        /// platform-specific implementation.
        /// </summary>
        /// <param name="administrator">
        /// Upon success, this parameter will be set to non-zero if the current
        /// process is running with administrative privileges.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges, discarding any error that may occur
        /// during the query.
        /// </summary>
        /// <param name="administrator">
        /// Upon success, this parameter will be set to non-zero if the current
        /// process is running with administrative privileges.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges on a Windows operating system, selecting
        /// the appropriate native query based on the operating system version.
        /// </summary>
        /// <param name="administrator">
        /// Upon success, this parameter will be set to non-zero if the current
        /// process is running with administrative privileges.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges on a Unix operating system by checking
        /// whether the real user identifier is that of the root user.
        /// </summary>
        /// <param name="administrator">
        /// Upon success, this parameter will be set to non-zero if the current
        /// process is running with administrative privileges.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
