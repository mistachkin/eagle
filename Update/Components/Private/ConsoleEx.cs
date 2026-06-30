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
    /// <summary>
    /// This class provides helper methods for attaching to, allocating, and
    /// freeing the Windows console, using the native console APIs via P/Invoke.
    /// </summary>
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
        /// <summary>
        /// This class contains the native console management methods imported
        /// from the Windows kernel32 library via P/Invoke.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [Guid("7463bad4-17f1-46f8-8caa-0868b0273925")]
        internal static class UnsafeNativeMethods
        {
            /// <summary>
            /// The name of the native Windows kernel library that exports the
            /// console management functions.
            /// </summary>
            private const string Kernel32 = "kernel32.dll";

            /// <summary>
            /// The pseudo process identifier used with
            /// <see cref="AttachConsole" /> to attach to the console of the
            /// parent process.
            /// </summary>
            internal const int ATTACH_PARENT_PROCESS = -1;

            /// <summary>
            /// This method retrieves the window handle of the console
            /// associated with the calling process.
            /// </summary>
            /// <returns>
            /// The handle of the console window, or zero if there is no
            /// associated console.
            /// </returns>
            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();

            /// <summary>
            /// This method attaches the calling process to the console of the
            /// specified process.
            /// </summary>
            /// <param name="processId">
            /// The identifier of the process whose console is to be used, or
            /// <see cref="ATTACH_PARENT_PROCESS" /> for the parent process.
            /// </param>
            /// <returns>
            /// True if the calling process was attached to the console;
            /// otherwise, false.
            /// </returns>
            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AttachConsole(int processId);

            /// <summary>
            /// This method allocates a new console for the calling process.
            /// </summary>
            /// <returns>
            /// True if a new console was allocated; otherwise, false.
            /// </returns>
            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AllocConsole();

            /// <summary>
            /// This method detaches the calling process from its console.
            /// </summary>
            /// <returns>
            /// True if the calling process was detached from its console;
            /// otherwise, false.
            /// </returns>
            [DllImport(Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeConsole();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Wrapper Methods
        /// <summary>
        /// This method determines whether a console window is currently open
        /// for the calling process.
        /// </summary>
        /// <param name="isOpen">
        /// Upon success, receives non-zero if a console window is open; zero
        /// otherwise.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if the open state was determined successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method attaches the calling process to the console of its
        /// parent process.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if the console was attached successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method allocates a new console for the calling process.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if a new console was allocated successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method detaches the calling process from its console.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if the console was freed successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method closes the console window if one is currently open,
        /// doing nothing if no console is open.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if no console was open or the console was closed successfully;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method ensures a console is available, doing nothing if one is
        /// already open; otherwise, it attempts to attach to the parent
        /// process console and, failing that, allocates a new console.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True if a console was already open or one was made available
        /// successfully; otherwise, false.
        /// </returns>
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
