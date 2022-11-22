/*
 * WindowOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NATIVE && WINDOWS
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#endif

using System.Runtime.InteropServices;

#if NATIVE && WINDOWS
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using System.Threading;
#endif

#if NATIVE && WINDOWS
using Microsoft.Win32.SafeHandles;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if NATIVE && WINDOWS
using Eagle._Components.Private.Delegates;
#endif

using Eagle._Constants;

#if NATIVE && WINDOWS
using WindowDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.AnyPair<System.IntPtr, long>,
    Eagle._Components.Public.Pair<string>>;

using WindowPair = System.Collections.Generic.KeyValuePair<
    Eagle._Components.Public.AnyPair<System.IntPtr, long>,
    Eagle._Components.Public.Pair<string>>;
#endif

namespace Eagle._Components.Private
{
#if NATIVE && WINDOWS
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("9e185cdc-bb2e-42bf-8d66-a176a18df7f1")]
    internal static class WindowOps
    {
        #region Private Static Data
#if NATIVE && WINDOWS
        #region Windows Terminal (Cascadia) Support
        //
        // HACK: These are the (cached) handle for the windows used by
        //       the Windows Terminal application (Cascadia) in order to
        //       accept standard Windows input messages like WM_KEYDOWN,
        //       etc.  They will be (re-)initialized when needed by this
        //       class.
        //
        private static IntPtr hWndCascadiaMain = IntPtr.Zero;
        private static IntPtr hWndCascadiaInput = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string CascadiaFileName = "WindowsTerminal.exe";
        private static string CascadiaClassName1 = "CASCADIA_HOSTING_WINDOW_CLASS";
        private static string CascadiaClassName2 = "Windows.UI.Composition.DesktopWindowContentBridge";
        private static string CascadiaClassName3 = "Windows.UI.Input.InputSite.WindowClass";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool traceWait = false;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if (NATIVE && WINDOWS) || WINFORMS
        //
        // HACK: This is purposely not read-only.
        //
        private static bool traceException = false;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool? overrideIsUserInteractive = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        #region Safe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("ad8acd8e-6180-4392-8916-6e76cb0929d9")]
        internal static class SafeNativeMethods
        {
            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetDesktopWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetForegroundWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetShellWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetActiveWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("b8dd6936-cd78-4a1f-b51e-e34b254e66bd")]
        internal static class UnsafeNativeMethods
        {
            internal const int SW_HIDE = 0;
            internal const int SW_SHOW = 5;

            internal const uint WM_NULL = 0x0000;

            internal const uint WM_CLOSE = 0x0010;

            internal const uint WM_GETICON = 0x007F;
            internal const uint WM_SETICON = 0x0080;

            internal const uint ICON_SMALL = 0;
            internal const uint ICON_BIG = 1;

            internal const uint VK_RETURN = 0x0D;

            internal const uint WM_KEYDOWN = 0x100;
            internal const uint WM_KEYUP = 0x101;

            internal const uint SC_CLOSE = 0xF060;
            internal const uint MF_BYCOMMAND = 0x0;

            internal const uint QS_NONE = 0x0000;
            internal const uint QS_KEY = 0x0001;
            internal const uint QS_MOUSEMOVE = 0x0002;
            internal const uint QS_MOUSEBUTTON = 0x0004;
            internal const uint QS_POSTMESSAGE = 0x0008;
            internal const uint QS_TIMER = 0x0010;
            internal const uint QS_PAINT = 0x0020;
            internal const uint QS_SENDMESSAGE = 0x0040;
            internal const uint QS_HOTKEY = 0x0080;
            internal const uint QS_ALLPOSTMESSAGE = 0x0100;
            internal const uint QS_RAWINPUT = 0x0400;

            internal const uint QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON);
            internal const uint QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT);

            internal const uint QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE |
                                                QS_TIMER | QS_PAINT |
                                                QS_HOTKEY);

            internal const uint QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE |
                                               QS_TIMER | QS_PAINT |
                                               QS_HOTKEY | QS_SENDMESSAGE);

            internal const uint MWMO_NONE = 0x0;
            internal const uint MWMO_WAITALL = 0x1;
            internal const uint MWMO_ALERTABLE = 0x2;
            internal const uint MWMO_INPUTAVAILABLE = 0x4;

            internal const uint MWMO_DEFAULT = MWMO_ALERTABLE |
                                               MWMO_INPUTAVAILABLE;

            internal const int ERROR_INVALID_THREAD_ID = 1444;

            internal const int MAX_CLASS_NAME = 257; // 256 + NUL (per MSDN, "The maximum length for lpszClassName is 256")

            ///////////////////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("b01f5772-a193-4cac-9a2c-6c73fd452e6e")]
            internal struct LASTINPUTINFO
            {
                public uint cbSize;
                public uint dwTime;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ShowWindow(
                IntPtr hWnd,
                int cmdShow
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern uint MsgWaitForMultipleObjectsEx(
                uint count,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] handles,
                uint milliseconds,
                uint wakeMask,
                uint flags
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern uint WaitForMultipleObjectsEx(
                uint count,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] handles,
                [MarshalAs(UnmanagedType.Bool)] bool waitAll,
                uint milliseconds,
                [MarshalAs(UnmanagedType.Bool)] bool alertable
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto)]
            internal static extern IntPtr SendMessage(
                IntPtr hWnd,
                uint message,
                UIntPtr wParam,
                IntPtr lParam
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostMessage(
                IntPtr hWnd,
                uint message,
                UIntPtr wParam,
                IntPtr lParam
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostThreadMessage(
                int threadId,
                uint message,
                UIntPtr wParam,
                IntPtr lParam
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetQueueStatus(
                uint flags
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern int GetWindowThreadProcessId(
                IntPtr hWnd,
                ref int processId
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr FindWindow(
                string className,
                string windowName
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr FindWindowEx(
                IntPtr hWndParent,
                IntPtr hWndChildAfter,
                string className,
                string windowName
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool EnumWindows(
                EnumWindowCallback callback,
                IntPtr lParam
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern int GetWindowTextLength(
                IntPtr hWnd
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int GetClassName(
                IntPtr hWnd,
                StringBuilder buffer,
                int count
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int GetWindowText(
                IntPtr hWnd,
                StringBuilder buffer,
                int count
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetLastInputInfo(
                ref LASTINPUTINFO pLastInputInfo
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr GetSystemMenu(
                IntPtr hWnd,
                [MarshalAs(UnmanagedType.Bool)] bool revert
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteMenu(
                IntPtr hMenu,
                uint position,
                uint flags
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            // [DllImport(DllName.User32,
            //     CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr GetFocus();

            ///////////////////////////////////////////////////////////////////////////////////////////

            // [DllImport(DllName.User32,
            //     CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr SetFocus(
            //     IntPtr hWnd
            // );

            ///////////////////////////////////////////////////////////////////////////////////////////

            // [DllImport(DllName.User32,
            //     CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr GetParent(
            //     IntPtr hWnd
            // );

            ///////////////////////////////////////////////////////////////////////////////////////////

            // [DllImport(DllName.User32,
            //     CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr SetParent(
            //     IntPtr hWndChild,
            //     IntPtr hWndNewParent
            // );
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool PreventWindowClose(
            IntPtr hWnd,
            ref Result error
            )
        {
            try
            {
                IntPtr hMenu = UnsafeNativeMethods.GetSystemMenu(
                    hWnd, false);

                if (hMenu != IntPtr.Zero)
                {
                    if(UnsafeNativeMethods.DeleteMenu(
                            hMenu, UnsafeNativeMethods.SC_CLOSE,
                            UnsafeNativeMethods.MF_BYCOMMAND))
                    {
                        return true;
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "DeleteMenu() failed with error {0}: {1}",
                            lastError, NativeOps.GetErrorMessage(lastError));
                    }
                }
                else
                {
                    //
                    // BUGBUG: Apparently, the DeleteMenu() Win32 API does
                    //         not report error codes via GetLastError()?
                    //         Either way, this should be mostly harmless.
                    //
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GetSystemMenu() failed with error {0}: {1}",
                        lastError, NativeOps.GetErrorMessage(lastError));
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? YesOrNo(
            string text,
            string caption,
            bool? @default
            )
        {
#if WINFORMS
            return FormOps.YesOrNo(text, caption, @default);
#else
            return @default;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? YesOrNoOrCancel(
            string text,
            string caption,
            bool? @default
            )
        {
#if WINFORMS
            return FormOps.YesOrNoOrCancel(text, caption, @default);
#else
            return @default;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetInteractiveHandle()
        {
            return IsInteractive() ? IntPtr.Zero : INVALID_HANDLE_VALUE;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsUserInteractiveViaEnvironment(
            ref UserInteractiveType? userInteractive
            )
        {
            //
            // HACK: Has the user interactivity status been manually
            //       overridden via the environment?
            //
            string value = CommonOps.Environment.GetVariable(
                EnvVars.UserInteractive);

            if (String.IsNullOrEmpty(value))
                return false;

            object enumValue = EnumOps.TryParse(
                typeof(UserInteractiveType), value, true, true);

            if (enumValue is UserInteractiveType)
            {
                userInteractive = (UserInteractiveType)enumValue;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsUserInteractiveViaOverride(
            ref bool? isUserInteractive
            )
        {
            //
            // HACK: Has the user interactivity status been manually
            //       overridden via our internal module state?
            //
            if (overrideIsUserInteractive != null)
            {
                isUserInteractive = overrideIsUserInteractive;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        private static bool IsUserInteractiveViaWinForms()
        {
            return FormOps.IsUserInteractive();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsUserInteractiveViaEnvironment()
        {
            return Environment.UserInteractive;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsUserInteractiveViaFramework()
        {
#if WINFORMS
            return IsUserInteractiveViaWinForms();
#else
            return IsUserInteractiveViaEnvironment();
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsInteractiveViaInterpreter()
        {
            //
            // BUGFIX: The interpreter may have been disposed and we do
            //         not want to throw any exception; therefore, wrap
            //         interpreter property access in a try block.
            //
            Interpreter interpreter = Interpreter.GetActive();

            if (interpreter == null)
                return false;

            bool locked = false;

            try
            {
                //
                // TODO: This was a soft lock; however, since there
                //       is no easy way to communicate a failure to
                //       our caller, try harder.
                //
                interpreter.InternalHardTryLock(
                    ref locked); /* TRANSACTIONAL */

                if (locked && !interpreter.Disposed)
                    return interpreter.InternalInteractive; /* throw */
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsInteractive()
        {
            UserInteractiveType? userInteractive = null;

            if (IsUserInteractiveViaEnvironment(ref userInteractive))
            {
                switch ((UserInteractiveType)userInteractive)
                {
                    case UserInteractiveType.False:
                        {
                            return false;
                        }
                    case UserInteractiveType.True:
                        {
                            return true;
                        }
                    case UserInteractiveType.Continue:
                        {
                            break; // do nothing.
                        }
                    case UserInteractiveType.Fallback:
                        {
                            goto fallback;
                        }
                    case UserInteractiveType.Environment:
                        {
                            return IsUserInteractiveViaEnvironment();
                        }
                    case UserInteractiveType.WinForms:
                        {
#if WINFORMS
                            return IsUserInteractiveViaWinForms();
#else
                            break;
#endif
                        }
                    case UserInteractiveType.Framework:
                        {
                            return IsUserInteractiveViaFramework();
                        }
                    case UserInteractiveType.Interpreter:
                        {
                            return IsInteractiveViaInterpreter();
                        }
                    case UserInteractiveType.InterpreterIfFalse:
                        {
                            if (!IsInteractiveViaInterpreter())
                                return false;

                            break;
                        }
                    case UserInteractiveType.InterpreterIfTrue:
                        {
                            if (IsInteractiveViaInterpreter())
                                return true;

                            break;
                        }
                    case UserInteractiveType.MaybeInterpreter:
                        {
                            if (CommonOps.Runtime.IsMono())
                                return IsInteractiveViaInterpreter();

                            break;
                        }
                    case UserInteractiveType.MaybeInterpreterIfFalse:
                        {
                            if (CommonOps.Runtime.IsMono() &&
                                !IsInteractiveViaInterpreter())
                            {
                                return false;
                            }

                            break;
                        }
                    case UserInteractiveType.MaybeInterpreterIfTrue:
                        {
                            if (CommonOps.Runtime.IsMono() &&
                                IsInteractiveViaInterpreter())
                            {
                                return true;
                            }

                            break;
                        }
                }
            }

            bool? isUserInteractive = null;

            if (IsUserInteractiveViaOverride(ref isUserInteractive))
                return (bool)isUserInteractive;

#if MONO || MONO_HACKS
            //
            // HACK: On Mono, the "*.UserInteractive" properties may always
            //       return false.  It is unknown whether this problem will
            //       be fixed in future versions of Mono.
            //
            if (CommonOps.Runtime.IsMono() && IsInteractiveViaInterpreter())
                return true;
#endif

        fallback:

            return IsUserInteractiveViaFramework();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && WINFORMS
        private static bool HasMessageQueue(
            long threadId,
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.PostThreadMessage(
                        ConversionOps.ToInt(threadId),
                        UnsafeNativeMethods.WM_NULL,
                        UIntPtr.Zero, IntPtr.Zero))
                {
                    return true;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    if (lastError == UnsafeNativeMethods.ERROR_INVALID_THREAD_ID)
                        return false;

                    error = NativeOps.GetErrorMessage(lastError);
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        public static ReturnCode ProcessEvents(
            Interpreter interpreter /* NOT USED */
            )
        {
            Result error = null;

            return ProcessEvents(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ProcessEvents(
            Interpreter interpreter, /* NOT USED */
            ref Result error
            )
        {
            try
            {
#if NATIVE && WINDOWS
                //
                // NOTE: If this thread has a message queue and there
                //       appears to be anything in it, process it now.
                //
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    if (HasMessageQueue(
                            GlobalState.GetCurrentNativeThreadId(),
                            ref error))
                    {
                        uint flags = UnsafeNativeMethods.QS_ALLINPUT;

                        if (UnsafeNativeMethods.GetQueueStatus(flags) != 0)
#endif
                            FormOps.DoEvents();
#if NATIVE && WINDOWS
                    }
                }
                else
                {
                    FormOps.DoEvents();
                }
#endif

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsX11Terminal()
        {
            return CommonOps.Environment.DoesVariableExist(
                EnvVars.Display);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Windows Terminal (Cascadia) Support
        public static bool IsWindowsTerminal()
        {
            return CommonOps.Environment.DoesVariableExist(
                EnvVars.WindowsTerminalSession);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static IntPtr GetInputWindow()
        {
            IntPtr handle;
            Result error = null;

            handle = GetInputWindow(ref error);

            if (handle == IntPtr.Zero)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetInputWindow: error = {0}",
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(WindowOps).Name,
                    TracePriority.NativeError);
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetInputWindow(
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                #region Windows Terminal (Cascadia) Support
                if (IsWindowsTerminal())
                    return GetCascadiaInputWindow(false, ref error);
                else
                #endregion
                    return NativeConsole.GetConsoleWindow(ref error);
            }
            else
            {
                error = "not supported on this operating system";
                return IntPtr.Zero;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetIconWindow()
        {
            IntPtr handle;
            Result error = null;

            handle = GetIconWindow(ref error);

            if (handle == IntPtr.Zero)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetIconWindow: error = {0}",
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(WindowOps).Name,
                    TracePriority.NativeError);
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr GetIconWindow(
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                #region Windows Terminal (Cascadia) Support
                if (IsWindowsTerminal())
                    return GetCascadiaMainWindow(false, ref error);
                else
                #endregion
                    return NativeConsole.GetConsoleWindow(ref error);
            }
            else
            {
                error = "not supported on this operating system";
                return IntPtr.Zero;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Windows Terminal (Cascadia) Support
        private static ICollection<long?> GetCascadiaProcessIds()
        {
            IList<long?> result = new List<long?>();

            foreach (IntPtr item in new IntPtr[] {
                    NativeOps.GetParentProcessId(),     /* cmd.exe (?) */
                    NativeOps.GetGrandparentProcessId() /* WindowsTerminal.exe (?) */
                })
            {
                if (item == IntPtr.Zero)
                    continue;

                long processId = item.ToInt64();
                string fileName = ProcessOps.GetFileName(processId);

                if ((fileName == null) || !PathOps.IsEqualFileName(
                        Path.GetFileName(fileName), CascadiaFileName))
                {
                    continue;
                }

                result.Add(processId);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr GetCascadiaMainWindow()
        {
            IntPtr handle;
            Result error = null;

            handle = GetCascadiaMainWindow(false, ref error);

            if (handle == IntPtr.Zero)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCascadiaMainWindow: error = {0}",
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(WindowOps).Name,
                    TracePriority.NativeError);
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr GetCascadiaMainWindow(
            bool force,
            ref Result error
            )
        {
            try
            {
                IntPtr handle = Interlocked.CompareExchange(
                    ref hWndCascadiaMain, IntPtr.Zero, IntPtr.Zero);

                if (!force && (handle != IntPtr.Zero))
                    return handle;

                WindowEnumerator windowEnumerator = new WindowEnumerator();
                bool returnValue = false;

                if (windowEnumerator.Populate(
                        ref returnValue, ref error) != ReturnCode.Ok)
                {
                    return IntPtr.Zero;
                }

                if (!returnValue)
                    return IntPtr.Zero;

                foreach (WindowPair pair in windowEnumerator.GetWindows(
                        GetCascadiaProcessIds(), CascadiaClassName1, null,
                        MatchMode.Exact, false))
                {
                    AnyPair<IntPtr, long> key = pair.Key;

                    if (key == null) /* IMPOSSIBLE */
                        continue;

                    handle = key.X;

                    if (handle != IntPtr.Zero)
                        return handle;
                }

                error = String.Format(
                    "cannot find window in process tree with class {0}",
                    CascadiaClassName1);
            }
            catch (Exception e)
            {
                error = e;
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr GetCascadiaInputWindow(
            bool force,
            ref Result error
            )
        {
            try
            {
                IntPtr handle = Interlocked.CompareExchange(
                    ref hWndCascadiaInput, IntPtr.Zero, IntPtr.Zero);

                if (!force && (handle != IntPtr.Zero))
                    return handle;

                handle = GetCascadiaMainWindow(force, ref error);

                if (handle == IntPtr.Zero)
                    return IntPtr.Zero;

                string className = CascadiaClassName2;

                handle = UnsafeNativeMethods.FindWindowEx(
                    handle, IntPtr.Zero, className, null);

                if (handle == IntPtr.Zero)
                    goto error;

                className = CascadiaClassName3;

                handle = UnsafeNativeMethods.FindWindowEx(
                    handle, IntPtr.Zero, className, null);

                if (handle == IntPtr.Zero)
                    goto error;

                /* IGNORED */
                Interlocked.CompareExchange(
                    ref hWndCascadiaInput, handle, IntPtr.Zero);

                return handle;

            error:

                int lastError = Marshal.GetLastWin32Error();

                error = String.Format(
                    "FindWindowEx({0}) failed with error {1}: {2}",
                    FormatOps.WrapOrNull(className), lastError,
                    NativeOps.GetErrorMessage(lastError));
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return IntPtr.Zero;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetIcons(
            IntPtr hWnd,
            out IntPtr smallIcon,
            out IntPtr bigIcon
            )
        {
            smallIcon = IntPtr.Zero;
            bigIcon = IntPtr.Zero;

            try
            {
                if (hWnd != IntPtr.Zero)
                {
                    /* IGNORED */
                    smallIcon = UnsafeNativeMethods.SendMessage(
                        hWnd, UnsafeNativeMethods.WM_GETICON,
                        new UIntPtr(UnsafeNativeMethods.ICON_SMALL),
                        IntPtr.Zero);

                    /* IGNORED */
                    bigIcon = UnsafeNativeMethods.SendMessage(
                        hWnd, UnsafeNativeMethods.WM_GETICON,
                        new UIntPtr(UnsafeNativeMethods.ICON_BIG),
                        IntPtr.Zero);

                    return true;
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SetIcons(
            IntPtr hWnd,
            IntPtr hIcon
            )
        {
            try
            {
                if (hWnd != IntPtr.Zero)
                {
                    /* IGNORED */
                    UnsafeNativeMethods.SendMessage(
                        hWnd, UnsafeNativeMethods.WM_SETICON,
                        new UIntPtr(UnsafeNativeMethods.ICON_SMALL),
                        hIcon);

                    /* IGNORED */
                    UnsafeNativeMethods.SendMessage(
                        hWnd, UnsafeNativeMethods.WM_SETICON,
                        new UIntPtr(UnsafeNativeMethods.ICON_BIG),
                        hIcon);

                    return true;
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLastInputTickCount(
            ref Result result
            )
        {
            try
            {
                UnsafeNativeMethods.LASTINPUTINFO lastInputInfo =
                    new UnsafeNativeMethods.LASTINPUTINFO();

                lastInputInfo.cbSize = (uint)Marshal.SizeOf(
                    typeof(UnsafeNativeMethods.LASTINPUTINFO));

                if (UnsafeNativeMethods.GetLastInputInfo(
                        ref lastInputInfo))
                {
                    result = lastInputInfo.dwTime;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNativeWindow(
            NativeWindowType windowType, /* in */
            ref IntPtr hWnd,             /* out */
            ref Result error             /* out */
            )
        {
            GetNativeWindowCallback callback = null;

            switch (windowType)
            {
                case NativeWindowType.Active:
                    {
                        callback = new GetNativeWindowCallback(
                            SafeNativeMethods.GetActiveWindow);

                        break;
                    }
                case NativeWindowType.Console:
                    {
                        callback = new GetNativeWindowCallback(
                            SafeNativeMethods.GetConsoleWindow);

                        break;
                    }
                case NativeWindowType.Foreground:
                    {
                        callback = new GetNativeWindowCallback(
                            SafeNativeMethods.GetForegroundWindow);

                        break;
                    }
                case NativeWindowType.Shell:
                    {
                        callback = new GetNativeWindowCallback(
                            SafeNativeMethods.GetShellWindow);

                        break;
                    }
                case NativeWindowType.Desktop:
                    {
                        callback = new GetNativeWindowCallback(
                            SafeNativeMethods.GetDesktopWindow);

                        break;
                    }
                #region Windows Terminal (Cascadia) Support
                case NativeWindowType.Terminal:
                    {
                        callback = new GetNativeWindowCallback(
                            GetCascadiaMainWindow);

                        break;
                    }
                #endregion
                case NativeWindowType.Input:
                    {
                        callback = new GetNativeWindowCallback(
                            GetInputWindow);

                        break;
                    }
                case NativeWindowType.Icon:
                    {
                        callback = new GetNativeWindowCallback(
                            GetIconWindow);

                        break;
                    }
            }

            if (callback == null)
            {
                error = String.Format(
                    "unsupported native window type {0}",
                    windowType);

                return ReturnCode.Error;
            }

            try
            {
                hWnd = callback(); /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Enumerator Class
#if NET_40
        [SecurityCritical()]
#else
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
        [ObjectId("12dd831f-79c8-4e34-a7e7-16eaf46bcbd2")]
        internal sealed class WindowEnumerator
        {
            #region Private Data
            private StringBuilder buffer;
            private WindowDictionary windows;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public WindowEnumerator()
            {
                windows = new WindowDictionary();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private bool EnumWindowCallback(
                IntPtr hWnd,
                IntPtr lParam
                )
            {
                try
                {
                    string text = null;
                    int length = UnsafeNativeMethods.GetWindowTextLength(hWnd);

                    if (length > 0)
                    {
                        length++; /* NUL terminator */

                        buffer = StringOps.NewStringBuilder(buffer, length);

                        if (UnsafeNativeMethods.GetWindowText(
                                hWnd, buffer, length) > 0)
                        {
                            text = buffer.ToString();
                        }
                    }

                    string @class = null;
                    length = UnsafeNativeMethods.MAX_CLASS_NAME;

                    buffer = StringOps.NewStringBuilder(buffer, length);

                    if (UnsafeNativeMethods.GetClassName(
                            hWnd, buffer, length) > 0)
                    {
                        @class = buffer.ToString();
                    }

                    int processId = 0;

                    /* IGNORED */
                    UnsafeNativeMethods.GetWindowThreadProcessId(
                        hWnd, ref processId);

                    windows[new AnyPair<IntPtr, long>(hWnd, processId)] =
                        new Pair<string>(@class, text);

                    return true;
                }
                catch (Exception e)
                {
                    if (traceException)
                    {
                        //
                        // NOTE: Nothing much we can do here except log the
                        //       failure.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(EnumWindowCallback).Name,
                            TracePriority.NativeError);
                    }
                }

                return false;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode Populate(
                ref bool returnValue,
                ref Result error
                )
            {
                try
                {
                    returnValue = UnsafeNativeMethods.EnumWindows(
                        EnumWindowCallback, IntPtr.Zero);

                    if (!returnValue)
                        error = NativeOps.GetErrorMessage();

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    if (traceException)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(WindowOps).Name,
                            TracePriority.NativeError);
                    }

                    error = e;
                }

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public WindowDictionary GetWindows()
            {
                return new WindowDictionary(windows);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public WindowDictionary GetWindows(
                ICollection<long?> processIds,
                string className,
                string windowName,
                MatchMode mode,
                bool noCase
                )
            {
                if (windows == null)
                    return null;

                WindowDictionary result = new WindowDictionary();

                foreach (WindowPair pair in windows)
                {
                    AnyPair<IntPtr, long> key = pair.Key;

                    if (key == null) /* IMPOSSIBLE */
                        continue;

                    if ((processIds != null) &&
                        !processIds.Contains(key.Y))
                    {
                        continue;
                    }

                    Pair<string> value = pair.Value;

                    if (value != null)
                    {
                        if ((className != null) && !StringOps.Match(
                                null, mode, value.X, className, noCase))
                        {
                            continue;
                        }

                        if ((windowName != null) && !StringOps.Match(
                                null, mode, value.Y, windowName, noCase))
                        {
                            continue;
                        }
                    }

                    result[key] = value;
                }

                return result;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ShowWindow(
            IntPtr handle,
            bool show,
            ref bool returnValue,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    returnValue = UnsafeNativeMethods.ShowWindow(
                        handle, show ? UnsafeNativeMethods.SW_SHOW :
                        UnsafeNativeMethods.SW_HIDE);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CloseWindow(
            IntPtr handle,
            ref bool returnValue,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    IntPtr result = UnsafeNativeMethods.SendMessage(
                        handle, UnsafeNativeMethods.WM_CLOSE,
                        UIntPtr.Zero, IntPtr.Zero);

                    returnValue = (result == IntPtr.Zero);

                    if (returnValue)
                        return ReturnCode.Ok;
                    else
                        error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetWindowText(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                int length = UnsafeNativeMethods.GetWindowTextLength(handle);

                if (length > 0)
                {
                    length++; /* NUL terminator */

                    StringBuilder buffer = StringOps.NewStringBuilder(length);

                    if (UnsafeNativeMethods.GetWindowText(
                            handle, buffer, length) > 0)
                    {
                        return buffer.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetWindowThreadProcessId(
            IntPtr handle,
            ref long processId,
            ref long threadId,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    int localThreadId;
                    int localProcessId = 0;

                    localThreadId = UnsafeNativeMethods.GetWindowThreadProcessId(
                        handle, ref localProcessId);

                    if (localThreadId != 0)
                    {
                        processId = localProcessId;
                        threadId = localThreadId;

                        return ReturnCode.Ok;
                    }

                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SimulateReturnKey(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    UIntPtr virtualKey = new UIntPtr(
                        UnsafeNativeMethods.VK_RETURN);

                    if (UnsafeNativeMethods.PostMessage(
                            handle, UnsafeNativeMethods.WM_KEYDOWN,
                            virtualKey, IntPtr.Zero))
                    {
                        if (UnsafeNativeMethods.PostMessage(
                                handle, UnsafeNativeMethods.WM_KEYUP,
                                virtualKey, IntPtr.Zero))
                        {
                            return ReturnCode.Ok;
                        }
                    }

                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool DangerousGetHandle(
            WaitHandle waitHandle,             /* in */
            out SafeWaitHandle safeWaitHandle, /* out */
            out bool success,                  /* out */
            out IntPtr[] handles,              /* out */
            ref Result error                   /* out */
            )
        {
            safeWaitHandle = null;
            success = false;
            handles = null;

            if (waitHandle == null)
            {
                error = "invalid wait handle";
                return false;
            }

            safeWaitHandle = waitHandle.SafeWaitHandle;

            if (safeWaitHandle == null)
            {
                error = "invalid safe wait handle";
                return false;
            }

            safeWaitHandle.DangerousAddRef(ref success);

            if (!success)
            {
                error = "failed to add reference to safe wait handle";
                return false;
            }

            IntPtr handle = safeWaitHandle.DangerousGetHandle();

            if (handle == IntPtr.Zero)
            {
                error = "failed to get native handle from safe wait handle";
                return false;
            }

            handles = new IntPtr[] { handle };
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void DangerousReleaseHandle(
            ref SafeWaitHandle safeWaitHandle, /* in, out */
            ref bool success                   /* in, out */
            )
        {
            if (success)
            {
                if (safeWaitHandle != null)
                {
                    safeWaitHandle.DangerousRelease();
                    safeWaitHandle = null;
                }

                success = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WaitForSingleHandle(
            WaitHandle waitHandle,
            int timeout,
            bool userInterface
            )
        {
            uint returnValue = 0;

            return WaitForSingleHandle(
                waitHandle, timeout, userInterface, ref returnValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WaitForSingleHandle(
            WaitHandle waitHandle,
            int timeout,
            bool userInterface,
            ref uint returnValue
            )
        {
            ReturnCode code;
            Result error = null;

            code = WaitForSingleHandle(
                waitHandle, timeout, userInterface, ref returnValue,
                ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(code, error);

            if (traceWait)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitForSingleHandle: exited, waitHandle = {0}, " +
                    "timeout = {1}, userInterface = {2}, " +
                    "returnValue = {3}, code = {4}, error = {5}",
                    FormatOps.DisplayWaitHandle(waitHandle), timeout,
                    userInterface, returnValue, code,
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(WindowOps).Name, TracePriority.NativeDebug);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: Contains a "Constrained Execution Region",
        //          modify carefully.
        //
        private static ReturnCode WaitForSingleHandle(
            WaitHandle waitHandle,
            int timeout,
            bool userInterface,
            ref uint returnValue,
            ref Result error
            )
        {
            SafeWaitHandle safeWaitHandle = null;
            bool success = false;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                IntPtr[] handles;

                if (!DangerousGetHandle(
                        waitHandle, out safeWaitHandle, out success, out handles,
                        ref error))
                {
                    return ReturnCode.Error;
                }

                if (userInterface)
                {
                    uint wakeMask = UnsafeNativeMethods.QS_ALLINPUT;
                    uint flags = UnsafeNativeMethods.MWMO_DEFAULT;

                    returnValue = UnsafeNativeMethods.MsgWaitForMultipleObjectsEx(
                        1, handles, (uint)timeout, wakeMask, flags);
                }
                else
                {
                    returnValue = UnsafeNativeMethods.WaitForMultipleObjectsEx(
                        1, handles, false, (uint)timeout, true);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                /* NO RESULT */
                DangerousReleaseHandle(ref safeWaitHandle, ref success);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool DangerousGetHandles(
            WaitHandle[] waitHandles,             /* in */
            out int length,                       /* out */
            out SafeWaitHandle[] safeWaitHandles, /* out */
            out bool [] successes,                /* out */
            out IntPtr[] handles,                 /* out */
            ref Result error                      /* out */
            )
        {
            length = 0;
            safeWaitHandles = null;
            successes = null;
            handles = null;

            if (waitHandles == null)
            {
                error = "invalid wait handles";
                return false;
            }

            length = waitHandles.Length;

            if (length <= 0)
            {
                error = "no wait handles";
                return false;
            }

            safeWaitHandles = new SafeWaitHandle[length];
            successes = new bool[length];
            handles = new IntPtr[length];

            for (int index = 0; index < length; index++)
            {
                if (waitHandles[index] == null)
                {
                    error = String.Format(
                        "invalid wait handle {0}", index);

                    return false;
                }

                safeWaitHandles[index] = waitHandles[index].SafeWaitHandle;

                if (safeWaitHandles[index] == null)
                {
                    error = String.Format(
                        "invalid safe wait handle {0}", index);

                    return false;
                }

                safeWaitHandles[index].DangerousAddRef(ref successes[index]);

                if (!successes[index])
                {
                    error = String.Format(
                        "failed to add reference to safe wait handle {0}",
                        index);

                    return false;
                }

                handles[index] = safeWaitHandles[index].DangerousGetHandle();

                if (handles[index] == IntPtr.Zero)
                {
                    error = String.Format(
                        "failed to get native handle from safe wait handle {0}",
                        index);

                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void DangerousReleaseHandles(
            SafeWaitHandle[] safeWaitHandles, /* in, out */
            bool[] successes,                 /* in, out */
            int length                        /* in */
            )
        {
            if ((safeWaitHandles != null) && (successes != null))
            {
                for (int index = 0; index < length; index++)
                {
                    if (successes[index])
                    {
                        if (safeWaitHandles[index] != null)
                        {
                            safeWaitHandles[index].DangerousRelease();
                            safeWaitHandles[index] = null;
                        }

                        successes[index] = false;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode WaitForMultipleHandles(
            WaitHandle[] waitHandles,
            int timeout,
            bool userInterface,
            ref Result error
            )
        {
            uint returnValue = 0;

            return WaitForMultipleHandles(
                waitHandles, timeout, userInterface, ref returnValue,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WaitForMultipleHandles(
            WaitHandle[] waitHandles,
            int timeout,
            bool userInterface,
            ref uint returnValue
            )
        {
            ReturnCode code;
            Result error = null;

            code = WaitForMultipleHandles(
                waitHandles, timeout, userInterface, ref returnValue,
                ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(code, error);

            if (traceWait)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitForMultipleHandles: exited, waitHandles = {0}, " +
                    "timeout = {1}, userInterface = {2}, " +
                    "returnValue = {3}, code = {4}, error = {5}",
                    FormatOps.DisplayWaitHandles(waitHandles), timeout,
                    userInterface, returnValue, code,
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(WindowOps).Name, TracePriority.NativeDebug);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: Contains a "Constrained Execution Region",
        //          modify carefully.
        //
        private static ReturnCode WaitForMultipleHandles(
            WaitHandle[] waitHandles,
            int timeout,
            bool userInterface,
            ref uint returnValue,
            ref Result error
            )
        {
            SafeWaitHandle[] safeWaitHandles = null;
            bool[] successes = null;
            int length = 0;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                IntPtr[] handles;

                if (!DangerousGetHandles(
                        waitHandles, out length, out safeWaitHandles, out successes,
                        out handles, ref error))
                {
                    return ReturnCode.Error;
                }

                if (userInterface)
                {
                    uint wakeMask = UnsafeNativeMethods.QS_ALLINPUT;
                    uint flags = UnsafeNativeMethods.MWMO_DEFAULT;

                    returnValue = UnsafeNativeMethods.MsgWaitForMultipleObjectsEx(
                        (uint)length, handles, (uint)timeout, wakeMask, flags);
                }
                else
                {
                    returnValue = UnsafeNativeMethods.WaitForMultipleObjectsEx(
                        (uint)length, handles, false, (uint)timeout, true);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                /* NO RESULT */
                DangerousReleaseHandles(safeWaitHandles, successes, length);
            }
        }
#endif
    }
}
