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
#endif

using System.Globalization;
using System.IO;

#if NATIVE && WINDOWS
using System.Runtime.CompilerServices;
#endif

using System.Runtime.InteropServices;

#if NATIVE && WINDOWS
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
#endif

using System.Text.RegularExpressions;

#if NATIVE && WINDOWS
using System.Threading;
using Microsoft.Win32.SafeHandles;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if NATIVE && WINDOWS
using Eagle._Components.Private.Delegates;
#endif

using Eagle._Constants;
using Eagle._Containers.Public;

#if NATIVE && WINDOWS
using SBF = Eagle._Components.Private.StringBuilderFactory;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using UNM = Eagle._Components.Private.WindowOps.UnsafeNativeMethods;

using WindowDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.AnyPair<System.IntPtr, long>,
    Eagle._Components.Public.Pair<string>>;

using WindowPair = System.Collections.Generic.KeyValuePair<
    Eagle._Components.Public.AnyPair<System.IntPtr, long>,
    Eagle._Components.Public.Pair<string>>;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of private utility methods for
    /// working with native windows, console windows, and related user
    /// interface concerns, including detecting user interactivity,
    /// enumerating top-level windows, manipulating window icons, simulating
    /// input, and waiting on synchronization handles.  On Windows, many of
    /// these methods are implemented using the underlying Win32 API via
    /// P/Invoke.
    /// </summary>
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
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The directory used to locate the dialog automation data file.
        /// </summary>
        private static string DialogsDirectory = GlobalState.GetAssemblyPath();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The file name (without any directory) of the dialog automation
        /// data file.
        /// </summary>
        private static string DialogsFileNameOnly = "dialogs.tsv";

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        #region Windows Terminal (Cascadia) Support
        //
        // HACK: These are the (cached) handle for the windows used by
        //       the Windows Terminal application (Cascadia) in order to
        //       accept standard Windows input messages like WM_KEYDOWN,
        //       etc.  They will be (re-)initialized when needed by this
        //       class.
        //
        /// <summary>
        /// The cached handle to the main window of the Windows Terminal
        /// (Cascadia) application.
        /// </summary>
        private static IntPtr hWndCascadiaMain = IntPtr.Zero;

        /// <summary>
        /// The cached handle to the input window of the Windows Terminal
        /// (Cascadia) application.
        /// </summary>
        private static IntPtr hWndCascadiaInput = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The executable file name of the Windows Terminal (Cascadia)
        /// application.
        /// </summary>
        private static string CascadiaFileName = "WindowsTerminal.exe";

        /// <summary>
        /// The window class name of the main hosting window used by the
        /// Windows Terminal (Cascadia) application.
        /// </summary>
        private static string CascadiaClassName1 = "CASCADIA_HOSTING_WINDOW_CLASS";

        /// <summary>
        /// The window class name of the intermediate content-bridge window
        /// used by the Windows Terminal (Cascadia) application.
        /// </summary>
        private static string CascadiaClassName2 = "Windows.UI.Composition.DesktopWindowContentBridge";

        /// <summary>
        /// The window class name of the input-site window used by the Windows
        /// Terminal (Cascadia) application.
        /// </summary>
        private static string CascadiaClassName3 = "Windows.UI.Input.InputSite.WindowClass";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, diagnostic tracing is emitted for the handle
        /// waiting methods in this class.
        /// </summary>
        private static bool traceWait = false;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if (NATIVE && WINDOWS) || WINFORMS
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, exceptions caught by the methods in this class are
        /// emitted via the diagnostic tracing subsystem.
        /// </summary>
        private static bool traceException = false;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-null, this value forcibly overrides the detected
        /// user-interactivity status; non-zero means interactive and zero
        /// means non-interactive.
        /// </summary>
        private static bool? overrideIsUserInteractive = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        #region Safe Native Methods Class
        /// <summary>
        /// This class contains the P/Invoke signatures for native Win32 APIs
        /// that are considered safe to call without additional security
        /// checks.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("ad8acd8e-6180-4392-8916-6e76cb0929d9")]
        internal static class SafeNativeMethods
        {
            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            /// <summary>
            /// Calls the Win32 GetDesktopWindow API to retrieve a handle to
            /// the desktop window.
            /// </summary>
            /// <returns>
            /// A handle to the desktop window.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetDesktopWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            /// <summary>
            /// Calls the Win32 GetForegroundWindow API to retrieve a handle
            /// to the foreground window.
            /// </summary>
            /// <returns>
            /// A handle to the foreground window.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetForegroundWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            /// <summary>
            /// Calls the Win32 GetShellWindow API to retrieve a handle to the
            /// shell's desktop window.
            /// </summary>
            /// <returns>
            /// A handle to the shell's desktop window.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetShellWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            /// <summary>
            /// Calls the Win32 GetActiveWindow API to retrieve a handle to
            /// the active window attached to the calling thread's message
            /// queue.
            /// </summary>
            /// <returns>
            /// A handle to the active window, or zero if there is no such
            /// window.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetActiveWindow();

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // WARNING: For use by the GetNativeWindow method only.
            //
            /// <summary>
            /// Calls the Win32 GetConsoleWindow API to retrieve a handle to
            /// the console window associated with the calling process.
            /// </summary>
            /// <returns>
            /// A handle to the console window, or zero if there is no
            /// associated console.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        #region Unsafe Native Methods Class
        /// <summary>
        /// This class contains the P/Invoke signatures and supporting
        /// constants for native Win32 APIs that require unmanaged code
        /// permission and are not necessarily safe to call without additional
        /// care.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("b8dd6936-cd78-4a1f-b51e-e34b254e66bd")]
        internal static class UnsafeNativeMethods
        {
            /// <summary>
            /// Value for the GetWindow command that retrieves the first
            /// window in the Z order.
            /// </summary>
            internal const uint GW_HWNDFIRST = 0;
            /// <summary>
            /// Value for the GetWindow command that retrieves the last window
            /// in the Z order.
            /// </summary>
            internal const uint GW_HWNDLAST = 1;

            /// <summary>
            /// Value for the GetWindow command that retrieves the next window
            /// in the Z order.
            /// </summary>
            internal const uint GW_HWNDNEXT = 2;

            /// <summary>
            /// Value for the GetWindow command that retrieves the previous
            /// window in the Z order.
            /// </summary>
            internal const uint GW_HWNDPREV = 3;

            /// <summary>
            /// Value for the GetWindow command that retrieves the owner
            /// window.
            /// </summary>
            internal const uint GW_OWNER = 4;

            /// <summary>
            /// Value for the GetWindow command that retrieves the first child
            /// window.
            /// </summary>
            internal const uint GW_CHILD = 5;

            /// <summary>
            /// Value for the GetWindow command that retrieves the enabled
            /// popup window owned by the specified window.
            /// </summary>
            internal const uint GW_ENABLEDPOPUP = 6;

            /// <summary>
            /// Value for the ShowWindow command that hides the window.
            /// </summary>
            internal const int SW_HIDE = 0;

            /// <summary>
            /// Value for the ShowWindow command that activates and displays
            /// the window.
            /// </summary>
            internal const int SW_SHOW = 5;

            /// <summary>
            /// The WM_NULL window message, which performs no operation.
            /// </summary>
            internal const uint WM_NULL = 0x0000;

            /// <summary>
            /// The WM_CLOSE window message, which requests that a window be
            /// closed.
            /// </summary>
            internal const uint WM_CLOSE = 0x0010;

            /// <summary>
            /// The WM_GETICON window message, which retrieves a window's
            /// icon.
            /// </summary>
            internal const uint WM_GETICON = 0x007F;

            /// <summary>
            /// The WM_SETICON window message, which assigns a window's icon.
            /// </summary>
            internal const uint WM_SETICON = 0x0080;

            /// <summary>
            /// The icon type designating a small icon.
            /// </summary>
            internal const uint ICON_SMALL = 0;

            /// <summary>
            /// The icon type designating a large icon.
            /// </summary>
            internal const uint ICON_BIG = 1;

            /// <summary>
            /// The virtual-key code for the RETURN (ENTER) key.
            /// </summary>
            internal const uint VK_RETURN = 0x0D;

            /// <summary>
            /// The WM_KEYDOWN window message, which signals that a key has
            /// been pressed.
            /// </summary>
            internal const uint WM_KEYDOWN = 0x100;

            /// <summary>
            /// The WM_KEYUP window message, which signals that a key has been
            /// released.
            /// </summary>
            internal const uint WM_KEYUP = 0x101;

            /// <summary>
            /// The SC_CLOSE system command, which closes the window.
            /// </summary>
            internal const uint SC_CLOSE = 0xF060;

            /// <summary>
            /// The DeleteMenu flag indicating that the item is identified by
            /// command identifier.
            /// </summary>
            internal const uint MF_BYCOMMAND = 0x0;

            /// <summary>
            /// Queue status flag indicating no message types.
            /// </summary>
            internal const uint QS_NONE = 0x0000;

            /// <summary>
            /// Queue status flag indicating that a keystroke message is in
            /// the queue.
            /// </summary>
            internal const uint QS_KEY = 0x0001;

            /// <summary>
            /// Queue status flag indicating that a mouse-move message is in
            /// the queue.
            /// </summary>
            internal const uint QS_MOUSEMOVE = 0x0002;

            /// <summary>
            /// Queue status flag indicating that a mouse-button message is in
            /// the queue.
            /// </summary>
            internal const uint QS_MOUSEBUTTON = 0x0004;

            /// <summary>
            /// Queue status flag indicating that a posted message is in the
            /// queue.
            /// </summary>
            internal const uint QS_POSTMESSAGE = 0x0008;

            /// <summary>
            /// Queue status flag indicating that a timer message is in the
            /// queue.
            /// </summary>
            internal const uint QS_TIMER = 0x0010;

            /// <summary>
            /// Queue status flag indicating that a paint message is in the
            /// queue.
            /// </summary>
            internal const uint QS_PAINT = 0x0020;

            /// <summary>
            /// Queue status flag indicating that a sent message is in the
            /// queue.
            /// </summary>
            internal const uint QS_SENDMESSAGE = 0x0040;

            /// <summary>
            /// Queue status flag indicating that a hot-key message is in the
            /// queue.
            /// </summary>
            internal const uint QS_HOTKEY = 0x0080;

            /// <summary>
            /// Queue status flag indicating that a posted message (other than
            /// those listed here) is in the queue.
            /// </summary>
            internal const uint QS_ALLPOSTMESSAGE = 0x0100;

            /// <summary>
            /// Queue status flag indicating that a raw input message is in
            /// the queue.
            /// </summary>
            internal const uint QS_RAWINPUT = 0x0400;

            /// <summary>
            /// Composite queue status flag combining all mouse message types.
            /// </summary>
            internal const uint QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON);

            /// <summary>
            /// Composite queue status flag combining all input message types.
            /// </summary>
            internal const uint QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT);

            /// <summary>
            /// Composite queue status flag combining all event message types.
            /// </summary>
            internal const uint QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE |
                                                QS_TIMER | QS_PAINT |
                                                QS_HOTKEY);

            /// <summary>
            /// Composite queue status flag combining all message types.
            /// </summary>
            internal const uint QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE |
                                               QS_TIMER | QS_PAINT |
                                               QS_HOTKEY | QS_SENDMESSAGE);

            /// <summary>
            /// Flag for MsgWaitForMultipleObjectsEx indicating no special
            /// wait behavior.
            /// </summary>
            internal const uint MWMO_NONE = 0x0;

            /// <summary>
            /// Flag for MsgWaitForMultipleObjectsEx indicating that the wait
            /// completes only when all handles are signaled.
            /// </summary>
            internal const uint MWMO_WAITALL = 0x1;

            /// <summary>
            /// Flag for MsgWaitForMultipleObjectsEx indicating that the wait
            /// may be satisfied by an alert (e.g. an APC).
            /// </summary>
            internal const uint MWMO_ALERTABLE = 0x2;

            /// <summary>
            /// Flag for MsgWaitForMultipleObjectsEx indicating that already
            /// queued input should satisfy the wait.
            /// </summary>
            internal const uint MWMO_INPUTAVAILABLE = 0x4;

            /// <summary>
            /// Composite default flags for MsgWaitForMultipleObjectsEx used
            /// by this class.
            /// </summary>
            internal const uint MWMO_DEFAULT = MWMO_ALERTABLE |
                                               MWMO_INPUTAVAILABLE;

            /// <summary>
            /// The Win32 error code returned when an invalid thread
            /// identifier is supplied.
            /// </summary>
            internal const int ERROR_INVALID_THREAD_ID = 1444;

            /// <summary>
            /// The maximum length, including the terminating NUL, of a window
            /// class name.
            /// </summary>
            internal const int MAX_CLASS_NAME = 257; // 256 + NUL (per MSDN, "The maximum length for lpszClassName is 256")

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure mirrors the native Win32 LASTINPUTINFO
            /// structure, which conveys the time of the last input event.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("b01f5772-a193-4cac-9a2c-6c73fd452e6e")]
            internal struct LASTINPUTINFO
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public uint cbSize;

                /// <summary>
                /// The tick count at the time of the last input event.
                /// </summary>
                public uint dwTime;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 ShowWindow API to set the show state of the
            /// specified window.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose show state is being set.
            /// </param>
            /// <param name="cmdShow">
            /// A value that controls how the window is shown.
            /// </param>
            /// <returns>
            /// Non-zero if the window was previously visible; otherwise,
            /// zero.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ShowWindow(
                IntPtr hWnd,
                int cmdShow
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 MsgWaitForMultipleObjectsEx API to wait until
            /// one or all of the specified objects are signaled or an input
            /// event occurs.
            /// </summary>
            /// <param name="count">
            /// The number of object handles in the array.
            /// </param>
            /// <param name="handles">
            /// An array of object handles to wait on.
            /// </param>
            /// <param name="milliseconds">
            /// The time-out interval, in milliseconds.
            /// </param>
            /// <param name="wakeMask">
            /// The mask of input event types that satisfy the wait.
            /// </param>
            /// <param name="flags">
            /// The flags controlling the wait behavior.
            /// </param>
            /// <returns>
            /// A value that indicates the event that caused the function to
            /// return.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 WaitForMultipleObjectsEx API to wait until one
            /// or all of the specified objects are signaled, an I/O
            /// completion routine or APC runs, or the time-out elapses.
            /// </summary>
            /// <param name="count">
            /// The number of object handles in the array.
            /// </param>
            /// <param name="handles">
            /// An array of object handles to wait on.
            /// </param>
            /// <param name="waitAll">
            /// Non-zero to wait for all objects to be signaled; zero to wait
            /// for any one object.
            /// </param>
            /// <param name="milliseconds">
            /// The time-out interval, in milliseconds.
            /// </param>
            /// <param name="alertable">
            /// Non-zero to allow the wait to be satisfied by an I/O
            /// completion routine or APC.
            /// </param>
            /// <returns>
            /// A value that indicates the event that caused the function to
            /// return.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 SendMessage API to send the specified message
            /// to a window and wait until it has been processed.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose window procedure receives the
            /// message.
            /// </param>
            /// <param name="message">
            /// The message to be sent.
            /// </param>
            /// <param name="wParam">
            /// Additional message-specific information.
            /// </param>
            /// <param name="lParam">
            /// Additional message-specific information.
            /// </param>
            /// <returns>
            /// The result of the message processing, which depends on the
            /// message sent.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 PostMessage API to place a message in the
            /// message queue of the thread that created the specified window
            /// and return without waiting.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose message queue receives the
            /// message.
            /// </param>
            /// <param name="message">
            /// The message to be posted.
            /// </param>
            /// <param name="wParam">
            /// Additional message-specific information.
            /// </param>
            /// <param name="lParam">
            /// Additional message-specific information.
            /// </param>
            /// <returns>
            /// Non-zero if the message was posted; otherwise, zero.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 PostThreadMessage API to post a message to the
            /// message queue of the specified thread and return without
            /// waiting.
            /// </summary>
            /// <param name="threadId">
            /// The identifier of the thread whose message queue receives the
            /// message.
            /// </param>
            /// <param name="message">
            /// The message to be posted.
            /// </param>
            /// <param name="wParam">
            /// Additional message-specific information.
            /// </param>
            /// <param name="lParam">
            /// Additional message-specific information.
            /// </param>
            /// <returns>
            /// Non-zero if the message was posted; otherwise, zero.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 GetQueueStatus API to determine the types of
            /// messages present in the calling thread's message queue.
            /// </summary>
            /// <param name="flags">
            /// The queue status flags identifying the message types of
            /// interest.
            /// </param>
            /// <returns>
            /// A value whose high-order word indicates the types of messages
            /// currently in the queue and whose low-order word indicates the
            /// types added since the last call.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetQueueStatus(
                uint flags
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 GetWindowThreadProcessId API to retrieve the
            /// identifiers of the thread and process that created the
            /// specified window.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window.
            /// </param>
            /// <param name="processId">
            /// Upon return, receives the identifier of the process that
            /// created the window.
            /// </param>
            /// <returns>
            /// The identifier of the thread that created the window.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern int GetWindowThreadProcessId(
                IntPtr hWnd,
                ref int processId
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 GetWindow API to retrieve a handle to a window
            /// that has the specified relationship to the given window.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose related window is sought.
            /// </param>
            /// <param name="command">
            /// The relationship between the given window and the window to be
            /// retrieved.
            /// </param>
            /// <returns>
            /// A handle to the related window, or zero if no such window
            /// exists.
            /// </returns>
            [DllImport(DllName.User32,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr GetWindow(
                IntPtr hWnd,
                uint command
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 FindWindow API to retrieve a handle to a
            /// top-level window matching the specified class and window names.
            /// </summary>
            /// <param name="className">
            /// The class name of the window to find, or null to match any
            /// class.
            /// </param>
            /// <param name="windowName">
            /// The window name (title) of the window to find, or null to
            /// match any title.
            /// </param>
            /// <returns>
            /// A handle to the matching window, or zero if no such window
            /// exists.
            /// </returns>
            [DllImport(DllName.User32,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr FindWindow(
                string className,
                string windowName
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 FindWindowEx API to retrieve a handle to a
            /// child window matching the specified class and window names.
            /// </summary>
            /// <param name="hWndParent">
            /// A handle to the parent window whose child windows are searched.
            /// </param>
            /// <param name="hWndChildAfter">
            /// A handle to a child window after which the search begins, or
            /// zero to begin with the first child window.
            /// </param>
            /// <param name="className">
            /// The class name of the window to find, or null to match any
            /// class.
            /// </param>
            /// <param name="windowName">
            /// The window name (title) of the window to find, or null to
            /// match any title.
            /// </param>
            /// <returns>
            /// A handle to the matching window, or zero if no such window
            /// exists.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 EnumWindows API to enumerate all top-level
            /// windows by passing each to the specified callback.
            /// </summary>
            /// <param name="callback">
            /// The callback invoked for each top-level window.
            /// </param>
            /// <param name="lParam">
            /// An application-defined value passed through to the callback.
            /// </param>
            /// <returns>
            /// Non-zero if the enumeration completed; otherwise, zero.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool EnumWindows(
                EnumWindowCallback callback,
                IntPtr lParam
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 GetWindowTextLength API to retrieve the length,
            /// in characters, of the specified window's title text.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose text length is sought.
            /// </param>
            /// <returns>
            /// The length, in characters, of the window's title text, not
            /// including the terminating NUL.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern int GetWindowTextLength(
                IntPtr hWnd
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 GetClassName API to retrieve the class name of
            /// the specified window.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose class name is sought.
            /// </param>
            /// <param name="buffer">
            /// The buffer that receives the class name.
            /// </param>
            /// <param name="count">
            /// The size, in characters, of the buffer.
            /// </param>
            /// <returns>
            /// The number of characters copied to the buffer, not including
            /// the terminating NUL, or zero on failure.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 GetWindowText API to copy the title text of the
            /// specified window into a buffer.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window whose title text is sought.
            /// </param>
            /// <param name="buffer">
            /// The buffer that receives the title text.
            /// </param>
            /// <param name="count">
            /// The size, in characters, of the buffer.
            /// </param>
            /// <returns>
            /// The number of characters copied to the buffer, not including
            /// the terminating NUL, or zero on failure.
            /// </returns>
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

            /// <summary>
            /// Calls the Win32 GetLastInputInfo API to retrieve the time of
            /// the last input event.
            /// </summary>
            /// <param name="pLastInputInfo">
            /// The structure that, upon return, receives the time of the last
            /// input event.
            /// </param>
            /// <returns>
            /// Non-zero on success; otherwise, zero.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetLastInputInfo(
                ref LASTINPUTINFO pLastInputInfo
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 GetSystemMenu API to retrieve a handle to the
            /// system (window) menu of the specified window.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window that owns the system menu.
            /// </param>
            /// <param name="revert">
            /// Non-zero to reset the system menu to its default state; zero to
            /// retrieve a handle to the current menu.
            /// </param>
            /// <returns>
            /// A handle to the system menu, or zero depending on the value of
            /// <paramref name="revert" />.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr GetSystemMenu(
                IntPtr hWnd,
                [MarshalAs(UnmanagedType.Bool)] bool revert
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Calls the Win32 DeleteMenu API to delete an item from the
            /// specified menu.
            /// </summary>
            /// <param name="hMenu">
            /// A handle to the menu from which the item is deleted.
            /// </param>
            /// <param name="position">
            /// The menu item to be deleted, interpreted according to
            /// <paramref name="flags" />.
            /// </param>
            /// <param name="flags">
            /// Flags indicating how <paramref name="position" /> is
            /// interpreted.
            /// </param>
            /// <returns>
            /// Non-zero on success; otherwise, zero.
            /// </returns>
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
        /// <summary>
        /// The sentinel handle value used to represent an invalid native
        /// handle.
        /// </summary>
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method prevents the specified window from being closed by
        /// removing the close command from its system menu.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the window whose close command is removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the close command was removed successfully; otherwise,
        /// false.
        /// </returns>
        public static bool PreventWindowClose(
            IntPtr hWnd,
            ref Result error
            )
        {
            try
            {
                IntPtr hMenu = UNM.GetSystemMenu(hWnd, false);

                if (hMenu != IntPtr.Zero)
                {
                    if (UNM.DeleteMenu(
                            hMenu, UNM.SC_CLOSE, UNM.MF_BYCOMMAND))
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

        /// <summary>
        /// This method prompts the user with a yes-or-no question, falling
        /// back to dialog automation when Windows Forms support is not
        /// available.
        /// </summary>
        /// <param name="text">
        /// The text of the question to present to the user.
        /// </param>
        /// <param name="caption">
        /// The caption to display with the prompt.
        /// </param>
        /// <param name="default">
        /// The default answer to use when no explicit answer is obtained.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero for yes, zero for no, or null if no answer is determined.
        /// </returns>
        public static bool? YesOrNo(
            string text,
            string caption,
            bool? @default
            )
        {
#if WINFORMS
            return FormOps.YesOrNo(text, caption, @default);
#else
            return GetPromptResultForAutomation(text, caption, @default);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method prompts the user with a yes-or-no-or-cancel question,
        /// falling back to dialog automation when Windows Forms support is not
        /// available.
        /// </summary>
        /// <param name="text">
        /// The text of the question to present to the user.
        /// </param>
        /// <param name="caption">
        /// The caption to display with the prompt.
        /// </param>
        /// <param name="default">
        /// The default answer to use when no explicit answer is obtained.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// Non-zero for yes, zero for no, or null for cancel or when no answer
        /// is determined.
        /// </returns>
        public static bool? YesOrNoOrCancel(
            string text,
            string caption,
            bool? @default
            )
        {
#if WINFORMS
            return FormOps.YesOrNoOrCancel(text, caption, @default);
#else
            return GetPromptResultForAutomation(text, caption, @default);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST || !WINFORMS
        //
        // HACK: Include TEST in the list of compile-options that
        //       enable this method, for use by unit tests, etc.
        //
        /// <summary>
        /// This method determines an automated boolean answer to a prompt by
        /// matching the prompt text against the configured dialog automation
        /// data.
        /// </summary>
        /// <param name="text">
        /// The text of the prompt to be matched.
        /// </param>
        /// <param name="caption">
        /// The caption associated with the prompt.  This parameter is not
        /// used.
        /// </param>
        /// <param name="default">
        /// The default answer to use when no match is found.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The automated answer, or the supplied default when no match is
        /// found.
        /// </returns>
        public static bool? GetPromptResultForAutomation(
            string text,    /* in */
            string caption, /* in: NOT USED */
            bool? @default  /* in: OPTIONAL */
            )
        {
            return GetPromptResultForAutomation<bool>(
                text, caption, @default, null, AutomationFlags.Default);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: Consider caching parsed dialogs file and its created regular expressions?
        //
        /// <summary>
        /// This method determines an automated answer to a prompt by matching
        /// the prompt text against the configured dialog automation data,
        /// returning either a boolean or an enumerated value depending on the
        /// requested type.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the automated answer; typically a boolean or an
        /// enumerated dialog result type.
        /// </typeparam>
        /// <param name="text">
        /// The text of the prompt to be matched.
        /// </param>
        /// <param name="caption">
        /// The caption associated with the prompt.  This parameter is not
        /// used.
        /// </param>
        /// <param name="default">
        /// The default answer to use when no match is found.  This parameter
        /// may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing matched values.  This parameter may
        /// be null.
        /// </param>
        /// <param name="automationFlags">
        /// The flags controlling how the dialog automation data is parsed and
        /// matched.
        /// </param>
        /// <returns>
        /// The automated answer, or the supplied default when no match is
        /// found.
        /// </returns>
        public static T? GetPromptResultForAutomation<T>(
            string text,                    /* in */
            string caption,                 /* in: NOT USED */
            T? @default,                    /* in: OPTIONAL */
            CultureInfo cultureInfo,        /* in: OPTIONAL */
            AutomationFlags automationFlags /* in */
            ) where T : struct /* e.g. System.Windows.Forms.DialogResult */
        {
            Result error = null; /* REUSED */

            try
            {
                string fileName = Path.Combine(
                    DialogsDirectory, DialogsFileNameOnly);

                if (!File.Exists(fileName))
                    return @default; /* SUCCESS */

                StringPairList list = null;

                error = null;

                if (Value.ExtractMappings(
                        File.ReadAllText(fileName),
                        automationFlags, ref list,
                        ref error) != ReturnCode.Ok)
                {
                    return @default; /* FAILURE */
                }

                bool isTypeOfBool = typeof(T) == typeof(bool);

                bool ignoreValueError = FlagOps.HasFlags(
                    automationFlags, AutomationFlags.IgnoreValueError, true);

                foreach (StringPair pair in list)
                {
                    Regex regEx = RegExOps.Create(pair.X);

                    if (!regEx.Match(text).Success)
                        continue;

                    string value = pair.Y;

                    if (String.IsNullOrEmpty(value))
                        return @default; /* SUCCESS */

                    Result localError;

                    if (isTypeOfBool)
                    {
                        bool? boolValue = null;

                        localError = null;

                        if (Value.GetNullableBoolean2(
                                value, ValueFlags.AnyBoolean,
                                cultureInfo, ref boolValue,
                                ref localError) != ReturnCode.Ok)
                        {
                            if (ignoreValueError)
                            {
                                continue;
                            }
                            else
                            {
                                error = localError;
                                break;
                            }
                        }

                        //
                        // HACK: Yes, this happened.  Is
                        //       there a better way here?
                        //
                        return (T)(object)boolValue; /* SUCCESS */
                    }
                    else
                    {
                        object enumValue;

                        localError = null;

                        enumValue = EnumOps.TryParse(
                            typeof(T), value, true, true,
                            ref localError);

                        if (!(enumValue is T))
                        {
                            if (ignoreValueError)
                            {
                                continue;
                            }
                            else
                            {
                                error = localError;
                                break;
                            }
                        }

                        return (T)enumValue; /* SUCCESS */
                    }
                }

                return @default; /* FAILURE */
            }
            catch (Exception e)
            {
                error = e;
                return @default; /* FAILURE */
            }
            finally
            {
                //
                // HACK: Normally, errors in a method like this
                //       would not be serious enough to merit a
                //       formal complaint; however, any type of
                //       error in this method should be *quite*
                //       rare (and this method may be called
                //       from code that is not fault tolerant),
                //       which means that a formal complaint
                //       here is probably a really good idea.
                //
                if (error != null)
                    DebugOps.Complain(ReturnCode.Error, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a native handle that reflects whether the
        /// current session is interactive.
        /// </summary>
        /// <returns>
        /// Zero when the session is interactive; otherwise, the invalid handle
        /// value.
        /// </returns>
        public static IntPtr GetInteractiveHandle()
        {
            return IsInteractive() ? IntPtr.Zero : INVALID_HANDLE_VALUE;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the user-interactivity status has
        /// been manually overridden via an environment variable.
        /// </summary>
        /// <param name="userInteractive">
        /// Upon success, receives the overriding user-interactivity type.
        /// </param>
        /// <returns>
        /// True if an override was found in the environment; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the user-interactivity status has
        /// been manually overridden via this class's internal module state.
        /// </summary>
        /// <param name="isUserInteractive">
        /// Upon success, receives the overriding user-interactivity value.
        /// </param>
        /// <returns>
        /// True if an internal override was present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the current session is interactive
        /// using the Windows Forms support.
        /// </summary>
        /// <returns>
        /// True if the session is interactive according to Windows Forms;
        /// otherwise, false.
        /// </returns>
        private static bool IsUserInteractiveViaWinForms()
        {
            return FormOps.IsUserInteractive();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current session is interactive
        /// using the runtime environment's user-interactivity property.
        /// </summary>
        /// <returns>
        /// True if the session is interactive according to the runtime
        /// environment; otherwise, false.
        /// </returns>
        private static bool IsUserInteractiveViaEnvironment()
        {
            return Environment.UserInteractive;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current session is interactive
        /// using the most appropriate framework-provided mechanism for the
        /// current build configuration.
        /// </summary>
        /// <returns>
        /// True if the session is interactive according to the framework;
        /// otherwise, false.
        /// </returns>
        private static bool IsUserInteractiveViaFramework()
        {
#if WINFORMS
            return IsUserInteractiveViaWinForms();
#else
            return IsUserInteractiveViaEnvironment();
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the active interpreter is currently
        /// in interactive mode, taking care not to throw if the interpreter
        /// has been disposed.
        /// </summary>
        /// <returns>
        /// True if an active interpreter exists and reports that it is
        /// interactive; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current session is interactive,
        /// honoring any environment or internal overrides and falling back to
        /// framework-provided detection as needed.
        /// </summary>
        /// <returns>
        /// True if the session is determined to be interactive; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified thread has a message
        /// queue by attempting to post a null message to it.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to test.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the thread has a message queue; otherwise, false.
        /// </returns>
        private static bool HasMessageQueue(
            long threadId,
            ref Result error
            )
        {
            try
            {
                if (UNM.PostThreadMessage(
                        ConversionOps.ToInt(threadId), UNM.WM_NULL,
                        UIntPtr.Zero, IntPtr.Zero))
                {
                    return true;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    if (lastError == UNM.ERROR_INVALID_THREAD_ID)
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
        /// <summary>
        /// This method processes any pending user-interface events for the
        /// current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ProcessEvents(
            Interpreter interpreter /* NOT USED */
            )
        {
            Result error = null;

            return ProcessEvents(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes any pending user-interface events for the
        /// current thread, reporting any error that occurs.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                        uint flags = UNM.QS_ALLINPUT;

                        if (UNM.GetQueueStatus(flags) != 0)
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

        /// <summary>
        /// This method determines whether the current process appears to be
        /// running under an X11 terminal, based on the presence of the display
        /// environment variable.
        /// </summary>
        /// <returns>
        /// True if an X11 display environment variable is present; otherwise,
        /// false.
        /// </returns>
        public static bool IsX11Terminal()
        {
            return CommonOps.Environment.DoesVariableExist(
                EnvVars.Display);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Windows Terminal (Cascadia) Support
        /// <summary>
        /// This method determines whether the current process appears to be
        /// running under the Windows Terminal (Cascadia) application, based on
        /// environment variables and, on Windows, the console window class.
        /// </summary>
        /// <returns>
        /// True if the process appears to be running under Windows Terminal;
        /// otherwise, false.
        /// </returns>
        public static bool IsWindowsTerminal()
        {
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.WindowsTerminalSession))
            {
                return true;
            }

#if NATIVE && WINDOWS
            if (!PlatformOps.IsWindowsOperatingSystem())
                return false;

            IntPtr hWnd = NativeConsole.GetWindow();

            if (IsWindowsTerminalClass(hWnd))
                return true;

            IntPtr hWndOwner = UNM.GetWindow(hWnd, UNM.GW_OWNER);

            if (IsWindowsTerminalClass(hWndOwner))
                return true;
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method determines whether the specified window has the class
        /// name used by the main hosting window of the Windows Terminal
        /// (Cascadia) application.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the window whose class name is examined.
        /// </param>
        /// <returns>
        /// True if the window has the Windows Terminal hosting class name;
        /// otherwise, false.
        /// </returns>
        private static bool IsWindowsTerminalClass(
            IntPtr hWnd /* in */
            )
        {
            if (hWnd == IntPtr.Zero)
                return false;

            StringBuilder buffer = SBF.CreateNoCache(
                null, UNM.MAX_CLASS_NAME); /* EXEMPT */

            if (UNM.GetClassName(
                    hWnd, buffer, UNM.MAX_CLASS_NAME) <= 0)
            {
                return false;
            }

            if (SharedStringOps.SystemEquals(
                    buffer.ToString(), CascadiaClassName1))
            {
                return true;
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method retrieves a handle to the window that should receive
        /// simulated input, tracing any error that occurs.
        /// </summary>
        /// <returns>
        /// A handle to the input window, or zero on failure.
        /// </returns>
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

        /// <summary>
        /// This method retrieves a handle to the window that should receive
        /// simulated input, selecting the appropriate window for the Windows
        /// Terminal (Cascadia) application when applicable.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// A handle to the input window, or zero on failure.
        /// </returns>
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
                    return NativeConsole.GetWindow(ref error);
            }
            else
            {
                error = "not supported on this operating system";
                return IntPtr.Zero;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves a handle to the window whose icon should be
        /// manipulated, tracing any error that occurs.
        /// </summary>
        /// <returns>
        /// A handle to the icon window, or zero on failure.
        /// </returns>
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

        /// <summary>
        /// This method retrieves a handle to the window whose icon should be
        /// manipulated, selecting the appropriate window for the Windows
        /// Terminal (Cascadia) application when applicable.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// A handle to the icon window, or zero on failure.
        /// </returns>
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
                    return NativeConsole.GetWindow(ref error);
            }
            else
            {
                error = "not supported on this operating system";
                return IntPtr.Zero;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Windows Terminal (Cascadia) Support
        /// <summary>
        /// This method collects the process identifiers in the current
        /// process's ancestry that correspond to the Windows Terminal
        /// (Cascadia) application.
        /// </summary>
        /// <returns>
        /// A collection of process identifiers belonging to the Windows
        /// Terminal application.
        /// </returns>
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

        /// <summary>
        /// This method retrieves a handle to the main window of the Windows
        /// Terminal (Cascadia) application, tracing any error that occurs.
        /// </summary>
        /// <returns>
        /// A handle to the Windows Terminal main window, or zero on failure.
        /// </returns>
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

        /// <summary>
        /// This method retrieves a handle to the main window of the Windows
        /// Terminal (Cascadia) application, using and updating the cached
        /// handle and enumerating windows in the process tree as needed.
        /// </summary>
        /// <param name="force">
        /// Non-zero to bypass the cached handle and force a fresh lookup.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// A handle to the Windows Terminal main window, or zero on failure.
        /// </returns>
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

                handle = NativeConsole.GetWindow(ref error);

                if (handle == IntPtr.Zero)
                    return IntPtr.Zero;

                if (IsWindowsTerminalClass(handle))
                {
                    /* IGNORED */
                    Interlocked.CompareExchange(
                        ref hWndCascadiaMain, handle, IntPtr.Zero);

                    return handle;
                }

                handle = UNM.GetWindow(handle, UNM.GW_OWNER);

                if (IsWindowsTerminalClass(handle))
                {
                    /* IGNORED */
                    Interlocked.CompareExchange(
                        ref hWndCascadiaMain, handle, IntPtr.Zero);

                    return handle;
                }

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
                    {
                        /* IGNORED */
                        Interlocked.CompareExchange(
                            ref hWndCascadiaMain, handle, IntPtr.Zero);

                        return handle;
                    }
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

        /// <summary>
        /// This method retrieves a handle to the input window of the Windows
        /// Terminal (Cascadia) application by descending through the child
        /// windows of its main window, using and updating the cached handle.
        /// </summary>
        /// <param name="force">
        /// Non-zero to bypass the cached handle and force a fresh lookup.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// A handle to the Windows Terminal input window, or zero on failure.
        /// </returns>
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

                handle = UNM.FindWindowEx(
                    handle, IntPtr.Zero, className, null);

                if (handle == IntPtr.Zero)
                    goto error;

                className = CascadiaClassName3;

                handle = UNM.FindWindowEx(
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

        /// <summary>
        /// This method retrieves the small and large icons currently
        /// associated with the specified window.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the window whose icons are retrieved.
        /// </param>
        /// <param name="smallIcon">
        /// Upon success, receives the handle to the window's small icon.
        /// </param>
        /// <param name="bigIcon">
        /// Upon success, receives the handle to the window's large icon.
        /// </param>
        /// <returns>
        /// True if the icons were retrieved; otherwise, false.
        /// </returns>
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
                    smallIcon = UNM.SendMessage(
                        hWnd, UNM.WM_GETICON,
                        new UIntPtr(UNM.ICON_SMALL),
                        IntPtr.Zero);

                    /* IGNORED */
                    bigIcon = UNM.SendMessage(
                        hWnd, UNM.WM_GETICON,
                        new UIntPtr(UNM.ICON_BIG),
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

        /// <summary>
        /// This method assigns the same icon as both the small and large icon
        /// of the specified window.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the window whose icons are assigned.
        /// </param>
        /// <param name="hIcon">
        /// A handle to the icon to assign as both the small and large icon.
        /// </param>
        /// <returns>
        /// True if the icons were assigned; otherwise, false.
        /// </returns>
        public static bool SetIcons(
            IntPtr hWnd,
            IntPtr hIcon
            )
        {
            return SetIcons(hWnd, hIcon, hIcon);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method assigns the small and large icons of the specified
        /// window.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the window whose icons are assigned.
        /// </param>
        /// <param name="hSmallIcon">
        /// A handle to the icon to assign as the small icon.
        /// </param>
        /// <param name="hBigIcon">
        /// A handle to the icon to assign as the large icon.
        /// </param>
        /// <returns>
        /// True if the icons were assigned; otherwise, false.
        /// </returns>
        public static bool SetIcons(
            IntPtr hWnd,
            IntPtr hSmallIcon,
            IntPtr hBigIcon
            )
        {
            try
            {
                if (hWnd != IntPtr.Zero)
                {
                    /* IGNORED */
                    UNM.SendMessage(hWnd,
                        UNM.WM_SETICON, new UIntPtr(
                        UNM.ICON_SMALL), hSmallIcon);

                    /* IGNORED */
                    UNM.SendMessage(hWnd,
                        UNM.WM_SETICON, new UIntPtr(
                        UNM.ICON_BIG), hBigIcon);

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

        /// <summary>
        /// This method retrieves the tick count at the time of the last user
        /// input event.
        /// </summary>
        /// <param name="result">
        /// Upon success, receives the tick count of the last input event;
        /// upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetLastInputTickCount(
            ref Result result
            )
        {
            try
            {
                UNM.LASTINPUTINFO lastInputInfo =
                    new UNM.LASTINPUTINFO();

                lastInputInfo.cbSize = (uint)Marshal.SizeOf(
                    typeof(UNM.LASTINPUTINFO));

                if (UNM.GetLastInputInfo(
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

        /// <summary>
        /// This method retrieves a handle to a native window of the specified
        /// type.
        /// </summary>
        /// <param name="windowType">
        /// The type of native window to retrieve.
        /// </param>
        /// <param name="hWnd">
        /// Upon success, receives the handle to the requested native window.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
        /// <summary>
        /// This class enumerates the top-level windows of the system, capturing
        /// each window's handle, owning process identifier, class name, and
        /// title text, and provides filtered access to the collected windows.
        /// </summary>
#if NET_40
        [SecurityCritical()]
#else
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
        [ObjectId("12dd831f-79c8-4e34-a7e7-16eaf46bcbd2")]
        internal sealed class WindowEnumerator
        {
            #region Private Data
            /// <summary>
            /// A reusable string buffer used when retrieving window class
            /// names and title text.
            /// </summary>
            private StringBuilder buffer;

            /// <summary>
            /// The collection of enumerated windows, keyed by window handle and
            /// owning process identifier.
            /// </summary>
            private WindowDictionary windows;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Constructs an empty instance of the window enumerator.
            /// </summary>
            public WindowEnumerator()
            {
                windows = new WindowDictionary();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            /// <summary>
            /// This method is the callback invoked for each top-level window
            /// during enumeration; it records the window's class name, title
            /// text, and owning process identifier.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window being enumerated.
            /// </param>
            /// <param name="lParam">
            /// The application-defined value passed through the enumeration.
            /// </param>
            /// <returns>
            /// True to continue the enumeration; otherwise, false.
            /// </returns>
            private bool EnumWindowCallback(
                IntPtr hWnd,
                IntPtr lParam
                )
            {
                try
                {
                    string text = null;
                    int length = UNM.GetWindowTextLength(hWnd);

                    if (length > 0)
                    {
                        length++; /* NUL terminator */

                        buffer = SBF.CreateNoCache(buffer, length); /* EXEMPT */

                        if (UNM.GetWindowText(
                                hWnd, buffer, length) > 0)
                        {
                            text = buffer.ToString();
                        }
                    }

                    string @class = null;
                    length = UNM.MAX_CLASS_NAME;

                    buffer = SBF.CreateNoCache(buffer, length); /* EXEMPT */

                    if (UNM.GetClassName(
                            hWnd, buffer, length) > 0)
                    {
                        @class = buffer.ToString();
                    }

                    int processId = 0;

                    /* IGNORED */
                    UNM.GetWindowThreadProcessId(hWnd, ref processId);

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
            /// <summary>
            /// This method populates the enumerator by enumerating all
            /// top-level windows of the system.
            /// </summary>
            /// <param name="returnValue">
            /// Upon return, receives the boolean result of the underlying
            /// window enumeration.
            /// </param>
            /// <param name="error">
            /// Upon failure, receives information about the error that was
            /// encountered.
            /// </param>
            /// <returns>
            /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error
            /// code.
            /// </returns>
            public ReturnCode Populate(
                ref bool returnValue,
                ref Result error
                )
            {
                try
                {
                    returnValue = UNM.EnumWindows(
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

            /// <summary>
            /// This method returns a copy of all windows collected by this
            /// enumerator.
            /// </summary>
            /// <returns>
            /// A new dictionary containing all collected windows.
            /// </returns>
            public WindowDictionary GetWindows()
            {
                return new WindowDictionary(windows);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method returns the collected windows that match the
            /// specified process identifiers, class name, and window name
            /// criteria.
            /// </summary>
            /// <param name="processIds">
            /// The collection of owning process identifiers to match, or null
            /// to match windows of any process.
            /// </param>
            /// <param name="className">
            /// The class name pattern to match, or null to match any class
            /// name.
            /// </param>
            /// <param name="windowName">
            /// The window name pattern to match, or null to match any window
            /// name.
            /// </param>
            /// <param name="mode">
            /// The matching mode used to compare class and window names.
            /// </param>
            /// <param name="noCase">
            /// Non-zero to perform case-insensitive matching.
            /// </param>
            /// <returns>
            /// A new dictionary containing the matching windows, or null if no
            /// windows have been collected.
            /// </returns>
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

        /// <summary>
        /// This method shows or hides the specified window.
        /// </summary>
        /// <param name="handle">
        /// A handle to the window to show or hide.
        /// </param>
        /// <param name="show">
        /// Non-zero to show the window; zero to hide it.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, receives the boolean result of the underlying show
        /// operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                    returnValue = UNM.ShowWindow(
                        handle, show ? UNM.SW_SHOW : UNM.SW_HIDE);

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

        /// <summary>
        /// This method requests that the specified window be closed by sending
        /// it a close message.
        /// </summary>
        /// <param name="handle">
        /// A handle to the window to close.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, receives a non-zero value when the close message was
        /// processed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                    IntPtr result = UNM.SendMessage(
                        handle, UNM.WM_CLOSE, UIntPtr.Zero,
                        IntPtr.Zero);

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

        /// <summary>
        /// This method retrieves the title text of the specified window.
        /// </summary>
        /// <param name="handle">
        /// A handle to the window whose title text is retrieved.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The title text of the window, or null if it could not be retrieved
        /// or the window has no title text.
        /// </returns>
        public static string GetWindowText(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                int length = UNM.GetWindowTextLength(handle);

                if (length > 0)
                {
                    length++; /* NUL terminator */

                    StringBuilder buffer = SBF.Create(length);

                    if (UNM.GetWindowText(
                            handle, buffer, length) > 0)
                    {
                        return StringBuilderCache.GetStringAndRelease(ref buffer);
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

        /// <summary>
        /// This method retrieves the identifiers of the process and thread that
        /// created the specified window.
        /// </summary>
        /// <param name="handle">
        /// A handle to the window whose process and thread are retrieved.
        /// </param>
        /// <param name="processId">
        /// Upon success, receives the identifier of the process that created
        /// the window.
        /// </param>
        /// <param name="threadId">
        /// Upon success, receives the identifier of the thread that created the
        /// window.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

                    localThreadId = UNM.GetWindowThreadProcessId(
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

        /// <summary>
        /// This method simulates a press of the RETURN key against the current
        /// input window.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SimulateReturnKey(
            ref Result error
            )
        {
            IntPtr handle = GetInputWindow(ref error);

            if (handle == IntPtr.Zero)
                return ReturnCode.Error;

            return SimulateReturnKey(handle, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method simulates a press of the RETURN key against the
        /// specified window by posting key-down and key-up messages.
        /// </summary>
        /// <param name="handle">
        /// A handle to the window that receives the simulated key press.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode SimulateReturnKey(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    UIntPtr virtualKey = new UIntPtr(UNM.VK_RETURN);

                    if (UNM.PostMessage(
                            handle, UNM.WM_KEYDOWN, virtualKey,
                            IntPtr.Zero))
                    {
                        if (UNM.PostMessage(
                                handle, UNM.WM_KEYUP, virtualKey,
                                IntPtr.Zero))
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
        /// <summary>
        /// This method obtains the native operating system handle associated
        /// with the specified wait handle, adding a reference to its underlying
        /// safe handle so that it remains valid for the duration of the
        /// operation.
        /// </summary>
        /// <param name="waitHandle">
        /// The wait handle whose native handle is to be obtained.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="safeWaitHandle">
        /// Upon success, this receives the safe wait handle that owns the
        /// native handle.
        /// </param>
        /// <param name="success">
        /// Upon success, this receives non-zero to indicate that a reference
        /// was successfully added to the safe wait handle.
        /// </param>
        /// <param name="handles">
        /// Upon success, this receives an array containing the native handle.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the native handle was obtained successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method releases the reference previously added to the specified
        /// safe wait handle, if one was successfully added.
        /// </summary>
        /// <param name="safeWaitHandle">
        /// The safe wait handle to release.  Upon return, this is reset to null
        /// when a reference was released.
        /// </param>
        /// <param name="success">
        /// Non-zero on entry to indicate that a reference was previously added
        /// and should now be released.  Upon return, this is reset to false.
        /// </param>
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

        /// <summary>
        /// This method waits for the specified wait handle to be signaled,
        /// up to the specified timeout, discarding the native return value.
        /// </summary>
        /// <param name="waitHandle">
        /// The wait handle to wait on.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also wake when user-interface input becomes available;
        /// otherwise, only the wait handle is monitored.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method waits for the specified wait handle to be signaled,
        /// up to the specified timeout, complaining about any failure.
        /// </summary>
        /// <param name="waitHandle">
        /// The wait handle to wait on.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also wake when user-interface input becomes available;
        /// otherwise, only the wait handle is monitored.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, this receives the native value returned by the
        /// underlying wait operation.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method waits for the specified wait handle to be signaled,
        /// optionally allowing user-interface messages to satisfy the wait,
        /// obtaining and releasing the underlying native handle within a
        /// constrained execution region.
        /// </summary>
        /// <param name="waitHandle">
        /// The wait handle to wait on.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to also wake when user-interface input becomes available;
        /// otherwise, only the wait handle is monitored.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, this receives the native value returned by the
        /// underlying wait operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
                    uint wakeMask = UNM.QS_ALLINPUT;
                    uint flags = UNM.MWMO_DEFAULT;

                    returnValue = UNM.MsgWaitForMultipleObjectsEx(
                        1, handles, (uint)timeout, wakeMask, flags);
                }
                else
                {
                    returnValue = UNM.WaitForMultipleObjectsEx(
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

        /// <summary>
        /// This method obtains the native handles for an array of wait handles,
        /// adding a reference to each underlying safe handle so the native
        /// handles remain valid while in use.
        /// </summary>
        /// <param name="waitHandles">
        /// The array of wait handles whose native handles are obtained.
        /// </param>
        /// <param name="length">
        /// Upon return, receives the number of wait handles processed.
        /// </param>
        /// <param name="safeWaitHandles">
        /// Upon success, receives the array of safe wait handles that were
        /// referenced.
        /// </param>
        /// <param name="successes">
        /// Upon success, receives an array indicating which safe wait handles
        /// were successfully referenced.
        /// </param>
        /// <param name="handles">
        /// Upon success, receives the array of native handles.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if all native handles were obtained; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method releases the references previously added to an array of
        /// safe wait handles by <c>DangerousGetHandles</c>.
        /// </summary>
        /// <param name="safeWaitHandles">
        /// The array of safe wait handles to release.
        /// </param>
        /// <param name="successes">
        /// The array indicating which safe wait handles were successfully
        /// referenced and therefore must be released.
        /// </param>
        /// <param name="length">
        /// The number of safe wait handles to process.
        /// </param>
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

        /// <summary>
        /// This method waits on an array of wait handles, optionally allowing
        /// user-interface messages to satisfy the wait.
        /// </summary>
        /// <param name="waitHandles">
        /// The array of wait handles to wait on.
        /// </param>
        /// <param name="timeout">
        /// The time-out interval, in milliseconds.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to allow user-interface messages to satisfy the wait.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method waits on an array of wait handles, optionally allowing
        /// user-interface messages to satisfy the wait, complaining about any
        /// error and emitting diagnostic tracing.
        /// </summary>
        /// <param name="waitHandles">
        /// The array of wait handles to wait on.
        /// </param>
        /// <param name="timeout">
        /// The time-out interval, in milliseconds.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to allow user-interface messages to satisfy the wait.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, receives the raw value reported by the underlying
        /// native wait operation.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
        /// <summary>
        /// This method waits on an array of wait handles, optionally allowing
        /// user-interface messages to satisfy the wait, obtaining and
        /// releasing the underlying native handles within a constrained
        /// execution region.
        /// </summary>
        /// <param name="waitHandles">
        /// The array of wait handles to wait on.
        /// </param>
        /// <param name="timeout">
        /// The time-out interval, in milliseconds.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to allow user-interface messages to satisfy the wait.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, receives the raw value reported by the underlying
        /// native wait operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                        waitHandles, out length, out safeWaitHandles,
                        out successes, out handles, ref error))
                {
                    return ReturnCode.Error;
                }

                if (userInterface)
                {
                    uint wakeMask = UNM.QS_ALLINPUT;
                    uint flags = UNM.MWMO_DEFAULT;

                    returnValue = UNM.MsgWaitForMultipleObjectsEx(
                        (uint)length, handles, (uint)timeout,
                        wakeMask, flags);
                }
                else
                {
                    returnValue = UNM.WaitForMultipleObjectsEx(
                        (uint)length, handles, false, (uint)timeout,
                        true);
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
