/*
 * ConsoleEx.cs --
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
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [Guid("6f0babd7-d56b-4313-b240-071a6a9f6278")]
    internal static class ConsoleEx
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [Guid("7463bad4-17f1-46f8-8caa-0868b0273925")]
        internal static class UnsafeNativeMethods
        {
            private const string Kernel32 = "kernel32.dll";

            internal const int ATTACH_PARENT_PROCESS = -1;

            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();

            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AttachConsole(int processId);

            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AllocConsole();

            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeConsole();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Wrapper Methods
        private static bool IsOpen(
            ref bool isOpen,
            ref string error
            )
        {
            try
            {
                if (VersionOps.IsWindowsOperatingSystem())
                {
                    isOpen = UnsafeNativeMethods.GetConsoleWindow() != IntPtr.Zero;

                    return true;
                }
                else
                {
                    error = "not implemented";
                }
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool Attach(
            ref string error
            )
        {
            try
            {
                if (VersionOps.IsWindowsOperatingSystem())
                {
                    int processId = UnsafeNativeMethods.ATTACH_PARENT_PROCESS;

                    if (UnsafeNativeMethods.AttachConsole(processId))
                        return true;
                    else
                        error = "failed to attach console";
                }
                else
                {
                    error = "not implemented";
                }
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool Open(
            ref string error
            )
        {
            try
            {
                if (VersionOps.IsWindowsOperatingSystem())
                {
                    if (UnsafeNativeMethods.AllocConsole())
                        return true;
                    else
                        error = "failed to allocate console";
                }
                else
                {
                    error = "not implemented";
                }
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool Close(
            ref string error
            )
        {
            try
            {
                if (VersionOps.IsWindowsOperatingSystem())
                {
                    if (UnsafeNativeMethods.FreeConsole())
                        return true;
                    else
                        error = "failed to free console";
                }
                else
                {
                    error = "not implemented";
                }
            }
            catch (Exception e)
            {
                error = e.ToString();
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Console Support Methods
        public static bool TryClose(
            ref string error
            )
        {
            bool isOpen = false;

            if (!IsOpen(ref isOpen, ref error))
                return false;

            if (!isOpen)
                return true;

            if (Close(ref error))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////

        public static bool TryOpen(
            ref string error
            )
        {
            bool isOpen = false;

            if (!IsOpen(ref isOpen, ref error))
                return false;

            if (isOpen)
                return true;

            if (Attach(ref error))
                return true;

            if (Open(ref error))
                return true;

            return false;
        }
        #endregion
    }
}
