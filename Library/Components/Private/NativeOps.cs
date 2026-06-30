/*
 * NativeOps.cs --
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

#if !NET_STANDARD_20
using System.Diagnostics;
#endif

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using UNM = Eagle._Components.Private.NativeOps.UnsafeNativeMethods;

#if WINDOWS
using Eagle._Components.Public.Delegates;
#endif

#if WINDOWS || UNIX
using Eagle._Components.Private.Delegates;
#endif

using Eagle._Containers.Public;
using Eagle._Constants;

#if WINDOWS
using Eagle._Interfaces.Public;
#endif

#if WINDOWS
using SharedStringOps = Eagle._Components.Shared.StringOps;

using VirtualKeyCode =
    Eagle._Components.Private.NativeOps.UnsafeNativeMethods.VirtualKeyCode;

using KeyEventFlags =
    Eagle._Components.Private.NativeOps.UnsafeNativeMethods.KeyEventFlags;

using VirtualKeyMapType =
    Eagle._Components.Private.NativeOps.UnsafeNativeMethods.VirtualKeyMapType;

using VirtualKeyCodeList = System.Collections.Generic.List<
    Eagle._Components.Private.NativeOps.UnsafeNativeMethods.VirtualKeyCode>;
#endif

using DebugPriorityDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.DebugPriority, Eagle._Components.Public.DebugPriority>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private, platform-specific native interop layer
    /// used throughout the Eagle core.  It declares the P/Invoke signatures for
    /// the Windows and Unix (Linux and macOS) operating system APIs needed by
    /// the library and wraps them in higher-level, error-aware helper methods.
    /// All of the native functionality is guarded so that it gracefully fails
    /// (rather than throwing) when invoked on an unsupported operating system.
    /// </summary>
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("c8e735d9-c893-4da3-9845-51c8479f4d53")]
    internal static class NativeOps
    {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Safe Native Methods Class
        /// <summary>
        /// This class contains the native methods that are considered safe to
        /// expose to partially trusted callers; the unmanaged-code security
        /// check is suppressed for these methods because, by themselves, they
        /// cannot be used to compromise security.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("317a3846-f0c5-4da6-89bc-185a51bb7015")]
        internal static class SafeNativeMethods
        {
#if WINDOWS
            #region Windows Data
            //
            // HACK: This is purposely not read-only.
            //
            /// <summary>
            /// The console control event that is delivered in order to simulate
            /// a Control-C (interrupt) condition for the current process.
            /// </summary>
            internal static ControlEvent ConsoleControlEvent = ControlEvent.CTRL_C_EVENT;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Methods
            /// <summary>
            /// This method retrieves a pseudo handle for the current process.
            /// </summary>
            /// <returns>
            /// A pseudo handle to the current process.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetCurrentProcess();

            /// <summary>
            /// This method retrieves a pseudo handle for the current thread.
            /// </summary>
            /// <returns>
            /// A pseudo handle to the current thread.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetCurrentThread();

            /// <summary>
            /// This method retrieves the identifier of the current process.
            /// </summary>
            /// <returns>
            /// The identifier of the current process.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetCurrentProcessId();

            /// <summary>
            /// This method retrieves the identifier of the current thread.
            /// </summary>
            /// <returns>
            /// The identifier of the current thread.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetCurrentThreadId();

            /// <summary>
            /// This method determines whether the calling process is being
            /// debugged by a user-mode debugger.
            /// </summary>
            /// <returns>
            /// True if the current process is running in the context of a
            /// debugger; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsDebuggerPresent();
            #endregion
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
            #region Unix Signal Constants
            /// <summary>
            /// The Unix signal number for a hangup (SIGHUP) condition.
            /// </summary>
            internal const int SIGHUP = 1;

            /// <summary>
            /// The Unix signal number for an interrupt (SIGINT) condition.
            /// </summary>
            internal const int SIGINT = 2;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Signal Data
            //
            // HACK: This is purposely not read-only.
            //
            /// <summary>
            /// The Unix signal number used to simulate a console interrupt for
            /// the current process.
            /// </summary>
            internal static int ConsoleSignal = SIGINT;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Signal Methods
            /// <summary>
            /// This method sends the specified signal to the current process.
            /// </summary>
            /// <param name="sig">
            /// The signal number to be sent.
            /// </param>
            /// <returns>
            /// Zero on success, or non-zero on failure.
            /// </returns>
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl,
                SetLastError = true)]
            internal static extern int raise(int sig);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Threading Methods
            /// <summary>
            /// This method obtains the identifier of the calling thread.
            /// </summary>
            /// <returns>
            /// The thread identifier of the calling thread.
            /// </returns>
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr pthread_self();

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the calling thread is the main
            /// thread of the process.
            /// </summary>
            /// <returns>
            /// A non-zero value if the calling thread is the main thread of the
            /// process; otherwise, zero.
            /// </returns>
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_main_np();
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        /// <summary>
        /// This class contains the native methods that require full trust to
        /// use; the unmanaged-code security check is suppressed here for
        /// performance, so these methods must only ever be exposed to fully
        /// trusted callers.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6dc268be-697f-41a1-98a2-be2ce602bdfe")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            #region Windows Dynamic Loading Methods
            /// <summary>
            /// This method retrieves the fully qualified path of the file that
            /// contains the specified module.
            /// </summary>
            /// <param name="module">
            /// A handle to the loaded module whose path is being requested, or
            /// null to request the path of the file used to create the calling
            /// process.
            /// </param>
            /// <param name="fileName">
            /// A buffer that receives the fully qualified path of the module.
            /// </param>
            /// <param name="size">
            /// The size of the buffer, in characters.
            /// </param>
            /// <returns>
            /// The length of the returned string, in characters, or zero on
            /// failure.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern uint GetModuleFileName(IntPtr module, IntPtr fileName, uint size);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves a handle for the specified module that has
            /// already been loaded into the calling process.
            /// </summary>
            /// <param name="fileName">
            /// The name of the loaded module, or null to request a handle to
            /// the file used to create the calling process.
            /// </param>
            /// <returns>
            /// A handle to the specified module, or null on failure.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(string fileName);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method adds a directory to the search path used to locate
            /// native libraries for the calling process.
            /// </summary>
            /// <param name="directory">
            /// The directory to be added to the native library search path, or
            /// null to restore the default search order.
            /// </param>
            /// <returns>
            /// True if the search path was modified successfully; otherwise,
            /// false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetDllDirectory(string directory);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method loads the specified native library into the address
            /// space of the calling process.
            /// </summary>
            /// <param name="fileName">
            /// The name of the native library to be loaded.
            /// </param>
            /// <returns>
            /// A handle to the loaded native library, or null on failure.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr LoadLibrary(string fileName);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method frees the specified loaded native library, unloading
            /// it from the calling process when it is no longer in use.
            /// </summary>
            /// <param name="module">
            /// A handle to the loaded native library to be freed.
            /// </param>
            /// <returns>
            /// True if the native library was freed successfully; otherwise,
            /// false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeLibrary(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi, even on Unicode OS. */
            /// <summary>
            /// This method retrieves the address of the specified exported
            /// function from the given loaded native library.
            /// </summary>
            /// <param name="module">
            /// A handle to the loaded native library that contains the
            /// function.
            /// </param>
            /// <param name="name">
            /// The name of the exported function whose address is being
            /// requested.
            /// </param>
            /// <returns>
            /// The address of the exported function, or null on failure.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr GetProcAddress(IntPtr module, string name);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Message Methods
            /// <summary>
            /// This method formats a message string, retrieving the text that
            /// corresponds to a system-defined error code.
            /// </summary>
            /// <param name="flags">
            /// The flags that control the formatting process and the
            /// interpretation of the source parameter.
            /// </param>
            /// <param name="source">
            /// The location of the message definition, the interpretation of
            /// which depends on the formatting flags.
            /// </param>
            /// <param name="messageId">
            /// The identifier of the requested message.
            /// </param>
            /// <param name="languageId">
            /// The language identifier of the requested message.
            /// </param>
            /// <param name="buffer">
            /// Receives a pointer to the buffer that contains the formatted
            /// message.
            /// </param>
            /// <param name="size">
            /// The size of the output buffer, in characters.
            /// </param>
            /// <param name="arguments">
            /// An array of values used as insert arguments in the formatted
            /// message.
            /// </param>
            /// <returns>
            /// The number of characters stored in the output buffer, or zero on
            /// failure.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern uint FormatMessage(
                FormatMessageFlags flags, IntPtr source, uint messageId, uint languageId,
                ref IntPtr buffer, uint size, IntPtr arguments);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method frees the specified local memory object that was
            /// previously allocated.
            /// </summary>
            /// <param name="handle">
            /// A handle to the local memory object to be freed.
            /// </param>
            /// <returns>
            /// Null if the object was freed successfully; otherwise, a handle to
            /// the memory object.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr LocalFree(IntPtr handle);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Mutex Methods
            /// <summary>
            /// This method creates or opens a named or unnamed mutex object.
            /// </summary>
            /// <param name="securityAttributes">
            /// The security attributes for the mutex object, or null to use the
            /// default security descriptor.
            /// </param>
            /// <param name="initialOwner">
            /// Non-zero if the calling thread should obtain initial ownership of
            /// the mutex object.
            /// </param>
            /// <param name="name">
            /// The name of the mutex object, or null to create an unnamed mutex
            /// object.
            /// </param>
            /// <returns>
            /// A handle to the newly created or opened mutex object, or null on
            /// failure.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr CreateMutex(IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string name);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Logging Methods
            /// <summary>
            /// This method sends the specified string to the debugger for
            /// display.
            /// </summary>
            /// <param name="outputString">
            /// The string to be displayed by the debugger.
            /// </param>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void OutputDebugString(string outputString);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Event Methods
            #region Dead Code
#if DEAD_CODE
            /// <summary>
            /// This method sets the specified event object to the signaled
            /// state.
            /// </summary>
            /// <param name="handle">
            /// A handle to the event object to be set.
            /// </param>
            /// <returns>
            /// Non-zero if the event object was set successfully; otherwise,
            /// zero.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetEvent(IntPtr handle);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method sets the specified event object to the non-signaled
            /// state.
            /// </summary>
            /// <param name="handle">
            /// A handle to the event object to be reset.
            /// </param>
            /// <returns>
            /// Non-zero if the event object was reset successfully; otherwise,
            /// zero.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ResetEvent(IntPtr handle);
#endif
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Keyboard Constants
            /// <summary>
            /// The prefix shared by the names of all the virtual-key code
            /// values.
            /// </summary>
            internal const string VirtualKeyCodePrefix = "VK_";
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Keyboard Enumerations
            /// <summary>
            /// This enumeration contains the Windows virtual-key codes used to
            /// identify keyboard, mouse, and related input keys when
            /// synthesizing or translating keystrokes.
            /// </summary>
            [ObjectId("1f0ab632-90bc-4572-98fc-a497a5f7354a")]
            internal enum VirtualKeyCode : byte
            {
                /// <summary>
                /// The left mouse button virtual-key code.
                /// </summary>
                VK_LBUTTON = 0x01,
                /// <summary>
                /// The right mouse button virtual-key code.
                /// </summary>
                VK_RBUTTON = 0x02,
                /// <summary>
                /// The Control-Break processing virtual-key code.
                /// </summary>
                VK_CANCEL = 0x03,
                /// <summary>
                /// The middle mouse button virtual-key code.
                /// </summary>
                VK_MBUTTON = 0x04,
                /// <summary>
                /// The first extended mouse button virtual-key code.
                /// </summary>
                VK_XBUTTON1 = 0x05,
                /// <summary>
                /// The second extended mouse button virtual-key code.
                /// </summary>
                VK_XBUTTON2 = 0x06,
                /// <summary>
                /// The Backspace key virtual-key code.
                /// </summary>
                VK_BACK = 0x08,
                /// <summary>
                /// The Tab key virtual-key code.
                /// </summary>
                VK_TAB = 0x09,
                /// <summary>
                /// The Clear key virtual-key code.
                /// </summary>
                VK_CLEAR = 0x0C,
                /// <summary>
                /// The Enter key virtual-key code.
                /// </summary>
                VK_RETURN = 0x0D,
                /// <summary>
                /// The Shift key virtual-key code.
                /// </summary>
                VK_SHIFT = 0x10,
                /// <summary>
                /// The Control key virtual-key code.
                /// </summary>
                VK_CONTROL = 0x11,
                /// <summary>
                /// The Alt key virtual-key code.
                /// </summary>
                VK_MENU = 0x12,
                /// <summary>
                /// The Pause key virtual-key code.
                /// </summary>
                VK_PAUSE = 0x13,
                /// <summary>
                /// The Caps Lock key virtual-key code.
                /// </summary>
                VK_CAPITAL = 0x14,
                /// <summary>
                /// The IME Kana mode virtual-key code.
                /// </summary>
                VK_KANA = 0x15,
                /// <summary>
                /// The IME Hangul mode virtual-key code.
                /// </summary>
                VK_HANGUL = 0x15,
                /// <summary>
                /// The IME On virtual-key code.
                /// </summary>
                VK_IME_ON = 0x16,
                /// <summary>
                /// The IME Junja mode virtual-key code.
                /// </summary>
                VK_JUNJA = 0x17,
                /// <summary>
                /// The IME final mode virtual-key code.
                /// </summary>
                VK_FINAL = 0x18,
                /// <summary>
                /// The IME Hanja mode virtual-key code.
                /// </summary>
                VK_HANJA = 0x19,
                /// <summary>
                /// The IME Kanji mode virtual-key code.
                /// </summary>
                VK_KANJI = 0x19,
                /// <summary>
                /// The IME Off virtual-key code.
                /// </summary>
                VK_IME_OFF = 0x1A,
                /// <summary>
                /// The Escape key virtual-key code.
                /// </summary>
                VK_ESCAPE = 0x1B,
                /// <summary>
                /// The IME convert virtual-key code.
                /// </summary>
                VK_CONVERT = 0x1C,
                /// <summary>
                /// The IME non-convert virtual-key code.
                /// </summary>
                VK_NONCONVERT = 0x1D,
                /// <summary>
                /// The IME accept virtual-key code.
                /// </summary>
                VK_ACCEPT = 0x1E,
                /// <summary>
                /// The IME mode change request virtual-key code.
                /// </summary>
                VK_MODECHANGE = 0x1F,
                /// <summary>
                /// The Spacebar virtual-key code.
                /// </summary>
                VK_SPACE = 0x20,
                /// <summary>
                /// The Page Up key virtual-key code.
                /// </summary>
                VK_PRIOR = 0x21,
                /// <summary>
                /// The Page Down key virtual-key code.
                /// </summary>
                VK_NEXT = 0x22,
                /// <summary>
                /// The End key virtual-key code.
                /// </summary>
                VK_END = 0x23,
                /// <summary>
                /// The Home key virtual-key code.
                /// </summary>
                VK_HOME = 0x24,
                /// <summary>
                /// The Left Arrow key virtual-key code.
                /// </summary>
                VK_LEFT = 0x25,
                /// <summary>
                /// The Up Arrow key virtual-key code.
                /// </summary>
                VK_UP = 0x26,
                /// <summary>
                /// The Right Arrow key virtual-key code.
                /// </summary>
                VK_RIGHT = 0x27,
                /// <summary>
                /// The Down Arrow key virtual-key code.
                /// </summary>
                VK_DOWN = 0x28,
                /// <summary>
                /// The Select key virtual-key code.
                /// </summary>
                VK_SELECT = 0x29,
                /// <summary>
                /// The Print key virtual-key code.
                /// </summary>
                VK_PRINT = 0x2A,
                /// <summary>
                /// The Execute key virtual-key code.
                /// </summary>
                VK_EXECUTE = 0x2B,
                /// <summary>
                /// The Print Screen key virtual-key code.
                /// </summary>
                VK_SNAPSHOT = 0x2C,
                /// <summary>
                /// The Insert key virtual-key code.
                /// </summary>
                VK_INSERT = 0x2D,
                /// <summary>
                /// The Delete key virtual-key code.
                /// </summary>
                VK_DELETE = 0x2E,
                /// <summary>
                /// The Help key virtual-key code.
                /// </summary>
                VK_HELP = 0x2F,
                /// <summary>
                /// The '0' key virtual-key code.
                /// </summary>
                VK_0 = 0x30,
                /// <summary>
                /// The '1' key virtual-key code.
                /// </summary>
                VK_1 = 0x31,
                /// <summary>
                /// The '2' key virtual-key code.
                /// </summary>
                VK_2 = 0x32,
                /// <summary>
                /// The '3' key virtual-key code.
                /// </summary>
                VK_3 = 0x33,
                /// <summary>
                /// The '4' key virtual-key code.
                /// </summary>
                VK_4 = 0x34,
                /// <summary>
                /// The '5' key virtual-key code.
                /// </summary>
                VK_5 = 0x35,
                /// <summary>
                /// The '6' key virtual-key code.
                /// </summary>
                VK_6 = 0x36,
                /// <summary>
                /// The '7' key virtual-key code.
                /// </summary>
                VK_7 = 0x37,
                /// <summary>
                /// The '8' key virtual-key code.
                /// </summary>
                VK_8 = 0x38,
                /// <summary>
                /// The '9' key virtual-key code.
                /// </summary>
                VK_9 = 0x39,
                /// <summary>
                /// The 'A' key virtual-key code.
                /// </summary>
                VK_A = 0x41,
                /// <summary>
                /// The 'B' key virtual-key code.
                /// </summary>
                VK_B = 0x42,
                /// <summary>
                /// The 'C' key virtual-key code.
                /// </summary>
                VK_C = 0x43,
                /// <summary>
                /// The 'D' key virtual-key code.
                /// </summary>
                VK_D = 0x44,
                /// <summary>
                /// The 'E' key virtual-key code.
                /// </summary>
                VK_E = 0x45,
                /// <summary>
                /// The 'F' key virtual-key code.
                /// </summary>
                VK_F = 0x46,
                /// <summary>
                /// The 'G' key virtual-key code.
                /// </summary>
                VK_G = 0x47,
                /// <summary>
                /// The 'H' key virtual-key code.
                /// </summary>
                VK_H = 0x48,
                /// <summary>
                /// The 'I' key virtual-key code.
                /// </summary>
                VK_I = 0x49,
                /// <summary>
                /// The 'J' key virtual-key code.
                /// </summary>
                VK_J = 0x4A,
                /// <summary>
                /// The 'K' key virtual-key code.
                /// </summary>
                VK_K = 0x4B,
                /// <summary>
                /// The 'L' key virtual-key code.
                /// </summary>
                VK_L = 0x4C,
                /// <summary>
                /// The 'M' key virtual-key code.
                /// </summary>
                VK_M = 0x4D,
                /// <summary>
                /// The 'N' key virtual-key code.
                /// </summary>
                VK_N = 0x4E,
                /// <summary>
                /// The 'O' key virtual-key code.
                /// </summary>
                VK_O = 0x4F,
                /// <summary>
                /// The 'P' key virtual-key code.
                /// </summary>
                VK_P = 0x50,
                /// <summary>
                /// The 'Q' key virtual-key code.
                /// </summary>
                VK_Q = 0x51,
                /// <summary>
                /// The 'R' key virtual-key code.
                /// </summary>
                VK_R = 0x52,
                /// <summary>
                /// The 'S' key virtual-key code.
                /// </summary>
                VK_S = 0x53,
                /// <summary>
                /// The 'T' key virtual-key code.
                /// </summary>
                VK_T = 0x54,
                /// <summary>
                /// The 'U' key virtual-key code.
                /// </summary>
                VK_U = 0x55,
                /// <summary>
                /// The 'V' key virtual-key code.
                /// </summary>
                VK_V = 0x56,
                /// <summary>
                /// The 'W' key virtual-key code.
                /// </summary>
                VK_W = 0x57,
                /// <summary>
                /// The 'X' key virtual-key code.
                /// </summary>
                VK_X = 0x58,
                /// <summary>
                /// The 'Y' key virtual-key code.
                /// </summary>
                VK_Y = 0x59,
                /// <summary>
                /// The 'Z' key virtual-key code.
                /// </summary>
                VK_Z = 0x5A,
                /// <summary>
                /// The left Windows key virtual-key code.
                /// </summary>
                VK_LWIN = 0x5B,
                /// <summary>
                /// The right Windows key virtual-key code.
                /// </summary>
                VK_RWIN = 0x5C,
                /// <summary>
                /// The Application key virtual-key code.
                /// </summary>
                VK_APPS = 0x5D,
                /// <summary>
                /// The Sleep key virtual-key code.
                /// </summary>
                VK_SLEEP = 0x5F,
                /// <summary>
                /// The numeric keypad '0' key virtual-key code.
                /// </summary>
                VK_NUMPAD0 = 0x60,
                /// <summary>
                /// The numeric keypad '1' key virtual-key code.
                /// </summary>
                VK_NUMPAD1 = 0x61,
                /// <summary>
                /// The numeric keypad '2' key virtual-key code.
                /// </summary>
                VK_NUMPAD2 = 0x62,
                /// <summary>
                /// The numeric keypad '3' key virtual-key code.
                /// </summary>
                VK_NUMPAD3 = 0x63,
                /// <summary>
                /// The numeric keypad '4' key virtual-key code.
                /// </summary>
                VK_NUMPAD4 = 0x64,
                /// <summary>
                /// The numeric keypad '5' key virtual-key code.
                /// </summary>
                VK_NUMPAD5 = 0x65,
                /// <summary>
                /// The numeric keypad '6' key virtual-key code.
                /// </summary>
                VK_NUMPAD6 = 0x66,
                /// <summary>
                /// The numeric keypad '7' key virtual-key code.
                /// </summary>
                VK_NUMPAD7 = 0x67,
                /// <summary>
                /// The numeric keypad '8' key virtual-key code.
                /// </summary>
                VK_NUMPAD8 = 0x68,
                /// <summary>
                /// The numeric keypad '9' key virtual-key code.
                /// </summary>
                VK_NUMPAD9 = 0x69,
                /// <summary>
                /// The numeric keypad Multiply key virtual-key code.
                /// </summary>
                VK_MULTIPLY = 0x6A,
                /// <summary>
                /// The numeric keypad Add key virtual-key code.
                /// </summary>
                VK_ADD = 0x6B,
                /// <summary>
                /// The numeric keypad Separator key virtual-key code.
                /// </summary>
                VK_SEPARATOR = 0x6C,
                /// <summary>
                /// The numeric keypad Subtract key virtual-key code.
                /// </summary>
                VK_SUBTRACT = 0x6D,
                /// <summary>
                /// The numeric keypad Decimal key virtual-key code.
                /// </summary>
                VK_DECIMAL = 0x6E,
                /// <summary>
                /// The numeric keypad Divide key virtual-key code.
                /// </summary>
                VK_DIVIDE = 0x6F,
                /// <summary>
                /// The F1 function key virtual-key code.
                /// </summary>
                VK_F1 = 0x70,
                /// <summary>
                /// The F2 function key virtual-key code.
                /// </summary>
                VK_F2 = 0x71,
                /// <summary>
                /// The F3 function key virtual-key code.
                /// </summary>
                VK_F3 = 0x72,
                /// <summary>
                /// The F4 function key virtual-key code.
                /// </summary>
                VK_F4 = 0x73,
                /// <summary>
                /// The F5 function key virtual-key code.
                /// </summary>
                VK_F5 = 0x74,
                /// <summary>
                /// The F6 function key virtual-key code.
                /// </summary>
                VK_F6 = 0x75,
                /// <summary>
                /// The F7 function key virtual-key code.
                /// </summary>
                VK_F7 = 0x76,
                /// <summary>
                /// The F8 function key virtual-key code.
                /// </summary>
                VK_F8 = 0x77,
                /// <summary>
                /// The F9 function key virtual-key code.
                /// </summary>
                VK_F9 = 0x78,
                /// <summary>
                /// The F10 function key virtual-key code.
                /// </summary>
                VK_F10 = 0x79,
                /// <summary>
                /// The F11 function key virtual-key code.
                /// </summary>
                VK_F11 = 0x7A,
                /// <summary>
                /// The F12 function key virtual-key code.
                /// </summary>
                VK_F12 = 0x7B,
                /// <summary>
                /// The F13 function key virtual-key code.
                /// </summary>
                VK_F13 = 0x7C,
                /// <summary>
                /// The F14 function key virtual-key code.
                /// </summary>
                VK_F14 = 0x7D,
                /// <summary>
                /// The F15 function key virtual-key code.
                /// </summary>
                VK_F15 = 0x7E,
                /// <summary>
                /// The F16 function key virtual-key code.
                /// </summary>
                VK_F16 = 0x7F,
                /// <summary>
                /// The F17 function key virtual-key code.
                /// </summary>
                VK_F17 = 0x80,
                /// <summary>
                /// The F18 function key virtual-key code.
                /// </summary>
                VK_F18 = 0x81,
                /// <summary>
                /// The F19 function key virtual-key code.
                /// </summary>
                VK_F19 = 0x82,
                /// <summary>
                /// The F20 function key virtual-key code.
                /// </summary>
                VK_F20 = 0x83,
                /// <summary>
                /// The F21 function key virtual-key code.
                /// </summary>
                VK_F21 = 0x84,
                /// <summary>
                /// The F22 function key virtual-key code.
                /// </summary>
                VK_F22 = 0x85,
                /// <summary>
                /// The F23 function key virtual-key code.
                /// </summary>
                VK_F23 = 0x86,
                /// <summary>
                /// The F24 function key virtual-key code.
                /// </summary>
                VK_F24 = 0x87,
                /// <summary>
                /// The navigation view virtual-key code.
                /// </summary>
                VK_NAVIGATION_VIEW = 0x88,
                /// <summary>
                /// The navigation menu virtual-key code.
                /// </summary>
                VK_NAVIGATION_MENU = 0x89,
                /// <summary>
                /// The navigation up virtual-key code.
                /// </summary>
                VK_NAVIGATION_UP = 0x8A,
                /// <summary>
                /// The navigation down virtual-key code.
                /// </summary>
                VK_NAVIGATION_DOWN = 0x8B,
                /// <summary>
                /// The navigation left virtual-key code.
                /// </summary>
                VK_NAVIGATION_LEFT = 0x8C,
                /// <summary>
                /// The navigation right virtual-key code.
                /// </summary>
                VK_NAVIGATION_RIGHT = 0x8D,
                /// <summary>
                /// The navigation accept virtual-key code.
                /// </summary>
                VK_NAVIGATION_ACCEPT = 0x8E,
                /// <summary>
                /// The navigation cancel virtual-key code.
                /// </summary>
                VK_NAVIGATION_CANCEL = 0x8F,
                /// <summary>
                /// The Num Lock key virtual-key code.
                /// </summary>
                VK_NUMLOCK = 0x90,
                /// <summary>
                /// The Scroll Lock key virtual-key code.
                /// </summary>
                VK_SCROLL = 0x91,
                /// <summary>
                /// The NEC PC-9800 keypad Equals key virtual-key code.
                /// </summary>
                VK_OEM_NEC_EQUAL = 0x92,
                /// <summary>
                /// The Fujitsu/OASYS Dictionary key virtual-key code.
                /// </summary>
                VK_OEM_FJ_JISHO = 0x92,
                /// <summary>
                /// The Fujitsu/OASYS Unregister Word key virtual-key code.
                /// </summary>
                VK_OEM_FJ_MASSHOU = 0x93,
                /// <summary>
                /// The Fujitsu/OASYS Register Word key virtual-key code.
                /// </summary>
                VK_OEM_FJ_TOUROKU = 0x94,
                /// <summary>
                /// The Fujitsu/OASYS Left OYAYUBI key virtual-key code.
                /// </summary>
                VK_OEM_FJ_LOYA = 0x95,
                /// <summary>
                /// The Fujitsu/OASYS Right OYAYUBI key virtual-key code.
                /// </summary>
                VK_OEM_FJ_ROYA = 0x96,
                /// <summary>
                /// The left Shift key virtual-key code.
                /// </summary>
                VK_LSHIFT = 0xA0,
                /// <summary>
                /// The right Shift key virtual-key code.
                /// </summary>
                VK_RSHIFT = 0xA1,
                /// <summary>
                /// The left Control key virtual-key code.
                /// </summary>
                VK_LCONTROL = 0xA2,
                /// <summary>
                /// The right Control key virtual-key code.
                /// </summary>
                VK_RCONTROL = 0xA3,
                /// <summary>
                /// The left Alt key virtual-key code.
                /// </summary>
                VK_LMENU = 0xA4,
                /// <summary>
                /// The right Alt key virtual-key code.
                /// </summary>
                VK_RMENU = 0xA5,
                /// <summary>
                /// The browser Back key virtual-key code.
                /// </summary>
                VK_BROWSER_BACK = 0xA6,
                /// <summary>
                /// The browser Forward key virtual-key code.
                /// </summary>
                VK_BROWSER_FORWARD = 0xA7,
                /// <summary>
                /// The browser Refresh key virtual-key code.
                /// </summary>
                VK_BROWSER_REFRESH = 0xA8,
                /// <summary>
                /// The browser Stop key virtual-key code.
                /// </summary>
                VK_BROWSER_STOP = 0xA9,
                /// <summary>
                /// The browser Search key virtual-key code.
                /// </summary>
                VK_BROWSER_SEARCH = 0xAA,
                /// <summary>
                /// The browser Favorites key virtual-key code.
                /// </summary>
                VK_BROWSER_FAVORITES = 0xAB,
                /// <summary>
                /// The browser Home key virtual-key code.
                /// </summary>
                VK_BROWSER_HOME = 0xAC,
                /// <summary>
                /// The Volume Mute key virtual-key code.
                /// </summary>
                VK_VOLUME_MUTE = 0xAD,
                /// <summary>
                /// The Volume Down key virtual-key code.
                /// </summary>
                VK_VOLUME_DOWN = 0xAE,
                /// <summary>
                /// The Volume Up key virtual-key code.
                /// </summary>
                VK_VOLUME_UP = 0xAF,
                /// <summary>
                /// The media Next Track key virtual-key code.
                /// </summary>
                VK_MEDIA_NEXT_TRACK = 0xB0,
                /// <summary>
                /// The media Previous Track key virtual-key code.
                /// </summary>
                VK_MEDIA_PREV_TRACK = 0xB1,
                /// <summary>
                /// The media Stop key virtual-key code.
                /// </summary>
                VK_MEDIA_STOP = 0xB2,
                /// <summary>
                /// The media Play/Pause key virtual-key code.
                /// </summary>
                VK_MEDIA_PLAY_PAUSE = 0xB3,
                /// <summary>
                /// The Start Mail key virtual-key code.
                /// </summary>
                VK_LAUNCH_MAIL = 0xB4,
                /// <summary>
                /// The Select Media key virtual-key code.
                /// </summary>
                VK_LAUNCH_MEDIA_SELECT = 0xB5,
                /// <summary>
                /// The Start Application 1 key virtual-key code.
                /// </summary>
                VK_LAUNCH_APP1 = 0xB6,
                /// <summary>
                /// The Start Application 2 key virtual-key code.
                /// </summary>
                VK_LAUNCH_APP2 = 0xB7,
                /// <summary>
                /// The OEM-1 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_1 = 0xBA,
                /// <summary>
                /// The OEM Plus key virtual-key code.
                /// </summary>
                VK_OEM_PLUS = 0xBB,
                /// <summary>
                /// The OEM Comma key virtual-key code.
                /// </summary>
                VK_OEM_COMMA = 0xBC,
                /// <summary>
                /// The OEM Minus key virtual-key code.
                /// </summary>
                VK_OEM_MINUS = 0xBD,
                /// <summary>
                /// The OEM Period key virtual-key code.
                /// </summary>
                VK_OEM_PERIOD = 0xBE,
                /// <summary>
                /// The OEM-2 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_2 = 0xBF,
                /// <summary>
                /// The OEM-3 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_3 = 0xC0,
                /// <summary>
                /// The Brazilian ABNT C1 key virtual-key code.
                /// </summary>
                VK_ABNT_C1 = 0xC1,
                /// <summary>
                /// The Brazilian ABNT C2 key virtual-key code.
                /// </summary>
                VK_ABNT_C2 = 0xC2,
                /// <summary>
                /// The gamepad A button virtual-key code.
                /// </summary>
                VK_GAMEPAD_A = 0xC3,
                /// <summary>
                /// The gamepad B button virtual-key code.
                /// </summary>
                VK_GAMEPAD_B = 0xC4,
                /// <summary>
                /// The gamepad X button virtual-key code.
                /// </summary>
                VK_GAMEPAD_X = 0xC5,
                /// <summary>
                /// The gamepad Y button virtual-key code.
                /// </summary>
                VK_GAMEPAD_Y = 0xC6,
                /// <summary>
                /// The gamepad right shoulder button virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_SHOULDER = 0xC7,
                /// <summary>
                /// The gamepad left shoulder button virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_SHOULDER = 0xC8,
                /// <summary>
                /// The gamepad left trigger virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_TRIGGER = 0xC9,
                /// <summary>
                /// The gamepad right trigger virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_TRIGGER = 0xCA,
                /// <summary>
                /// The gamepad directional pad up virtual-key code.
                /// </summary>
                VK_GAMEPAD_DPAD_UP = 0xCB,
                /// <summary>
                /// The gamepad directional pad down virtual-key code.
                /// </summary>
                VK_GAMEPAD_DPAD_DOWN = 0xCC,
                /// <summary>
                /// The gamepad directional pad left virtual-key code.
                /// </summary>
                VK_GAMEPAD_DPAD_LEFT = 0xCD,
                /// <summary>
                /// The gamepad directional pad right virtual-key code.
                /// </summary>
                VK_GAMEPAD_DPAD_RIGHT = 0xCE,
                /// <summary>
                /// The gamepad Menu button virtual-key code.
                /// </summary>
                VK_GAMEPAD_MENU = 0xCF,
                /// <summary>
                /// The gamepad View button virtual-key code.
                /// </summary>
                VK_GAMEPAD_VIEW = 0xD0,
                /// <summary>
                /// The gamepad left thumbstick button virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1,
                /// <summary>
                /// The gamepad right thumbstick button virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2,
                /// <summary>
                /// The gamepad left thumbstick up virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3,
                /// <summary>
                /// The gamepad left thumbstick down virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4,
                /// <summary>
                /// The gamepad left thumbstick right virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5,
                /// <summary>
                /// The gamepad left thumbstick left virtual-key code.
                /// </summary>
                VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6,
                /// <summary>
                /// The gamepad right thumbstick up virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7,
                /// <summary>
                /// The gamepad right thumbstick down virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8,
                /// <summary>
                /// The gamepad right thumbstick right virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9,
                /// <summary>
                /// The gamepad right thumbstick left virtual-key code.
                /// </summary>
                VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA,
                /// <summary>
                /// The OEM-4 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_4 = 0xDB,
                /// <summary>
                /// The OEM-5 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_5 = 0xDC,
                /// <summary>
                /// The OEM-6 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_6 = 0xDD,
                /// <summary>
                /// The OEM-7 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_7 = 0xDE,
                /// <summary>
                /// The OEM-8 (varies by keyboard) virtual-key code.
                /// </summary>
                VK_OEM_8 = 0xDF,
                /// <summary>
                /// The Japanese AX key virtual-key code.
                /// </summary>
                VK_OEM_AX = 0xE1,
                /// <summary>
                /// The OEM-102 angle-bracket or backslash key virtual-key code.
                /// </summary>
                VK_OEM_102 = 0xE2,
                /// <summary>
                /// The ICO Help key virtual-key code.
                /// </summary>
                VK_ICO_HELP = 0xE3,
                /// <summary>
                /// The ICO 00 key virtual-key code.
                /// </summary>
                VK_ICO_00 = 0xE4,
                /// <summary>
                /// The IME Process key virtual-key code.
                /// </summary>
                VK_PROCESSKEY = 0xE5,
                /// <summary>
                /// The ICO Clear key virtual-key code.
                /// </summary>
                VK_ICO_CLEAR = 0xE6,
                /// <summary>
                /// The packet key, used to pass Unicode characters, virtual-key
                /// code.
                /// </summary>
                VK_PACKET = 0xE7,
                /// <summary>
                /// The Nokia/Ericsson OEM Reset key virtual-key code.
                /// </summary>
                VK_OEM_RESET = 0xE9,
                /// <summary>
                /// The Nokia/Ericsson OEM Jump key virtual-key code.
                /// </summary>
                VK_OEM_JUMP = 0xEA,
                /// <summary>
                /// The Nokia/Ericsson OEM PA1 key virtual-key code.
                /// </summary>
                VK_OEM_PA1 = 0xEB,
                /// <summary>
                /// The Nokia/Ericsson OEM PA2 key virtual-key code.
                /// </summary>
                VK_OEM_PA2 = 0xEC,
                /// <summary>
                /// The Nokia/Ericsson OEM PA3 key virtual-key code.
                /// </summary>
                VK_OEM_PA3 = 0xED,
                /// <summary>
                /// The Nokia/Ericsson OEM WSCTRL key virtual-key code.
                /// </summary>
                VK_OEM_WSCTRL = 0xEE,
                /// <summary>
                /// The Nokia/Ericsson OEM CUSEL key virtual-key code.
                /// </summary>
                VK_OEM_CUSEL = 0xEF,
                /// <summary>
                /// The Nokia/Ericsson OEM ATTN key virtual-key code.
                /// </summary>
                VK_OEM_ATTN = 0xF0,
                /// <summary>
                /// The Nokia/Ericsson OEM Finish key virtual-key code.
                /// </summary>
                VK_OEM_FINISH = 0xF1,
                /// <summary>
                /// The Nokia/Ericsson OEM Copy key virtual-key code.
                /// </summary>
                VK_OEM_COPY = 0xF2,
                /// <summary>
                /// The Nokia/Ericsson OEM Auto key virtual-key code.
                /// </summary>
                VK_OEM_AUTO = 0xF3,
                /// <summary>
                /// The Nokia/Ericsson OEM ENLW key virtual-key code.
                /// </summary>
                VK_OEM_ENLW = 0xF4,
                /// <summary>
                /// The Nokia/Ericsson OEM Backtab key virtual-key code.
                /// </summary>
                VK_OEM_BACKTAB = 0xF5,
                /// <summary>
                /// The Attn key virtual-key code.
                /// </summary>
                VK_ATTN = 0xF6,
                /// <summary>
                /// The CrSel key virtual-key code.
                /// </summary>
                VK_CRSEL = 0xF7,
                /// <summary>
                /// The ExSel key virtual-key code.
                /// </summary>
                VK_EXSEL = 0xF8,
                /// <summary>
                /// The Erase EOF key virtual-key code.
                /// </summary>
                VK_EREOF = 0xF9,
                /// <summary>
                /// The Play key virtual-key code.
                /// </summary>
                VK_PLAY = 0xFA,
                /// <summary>
                /// The Zoom key virtual-key code.
                /// </summary>
                VK_ZOOM = 0xFB,
                /// <summary>
                /// The reserved NoName virtual-key code.
                /// </summary>
                VK_NONAME = 0xFC,
                /// <summary>
                /// The PA1 key virtual-key code.
                /// </summary>
                VK_PA1 = 0xFD,
                /// <summary>
                /// The OEM Clear key virtual-key code.
                /// </summary>
                VK_OEM_CLEAR = 0xFE
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This enumeration contains the flags that control the various
            /// aspects of a synthesized keystroke.
            /// </summary>
            [Flags()]
            [ObjectId("5b1663e9-d742-4655-84f6-27cef9fd2fbf")]
            internal enum KeyEventFlags : byte
            {
                /// <summary>
                /// No key event flags are specified.
                /// </summary>
                KEYEVENTF_NONE = 0x0,
                /// <summary>
                /// The scan code is preceded by an extended-key prefix byte.
                /// </summary>
                KEYEVENTF_EXTENDEDKEY = 0x1,
                /// <summary>
                /// The key is being released rather than pressed.
                /// </summary>
                KEYEVENTF_KEYUP = 0x2,
                /// <summary>
                /// The keystroke specifies a Unicode character.
                /// </summary>
                KEYEVENTF_UNICODE = 0x4,
                /// <summary>
                /// The keystroke is identified by its hardware scan code.
                /// </summary>
                KEYEVENTF_SCANCODE = 0x8
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This enumeration contains the kinds of translation that may be
            /// performed between virtual-key codes, scan codes, and character
            /// values.
            /// </summary>
            [ObjectId("18354957-7345-472e-b041-f8d2d6532ede")]
            internal enum VirtualKeyMapType : uint
            {
                /// <summary>
                /// Translate a virtual-key code into a scan code.
                /// </summary>
                MAPVK_VK_TO_VSC = 0,
                /// <summary>
                /// Translate a scan code into a virtual-key code.
                /// </summary>
                MAPVK_VSC_TO_VK = 1,
                /// <summary>
                /// Translate a virtual-key code into an unshifted character
                /// value.
                /// </summary>
                MAPVK_VK_TO_CHAR = 2,
                /// <summary>
                /// Translate a scan code into a virtual-key code that
                /// distinguishes the left- and right-hand keys.
                /// </summary>
                MAPVK_VSC_TO_VK_EX = 3,
                /// <summary>
                /// Translate a virtual-key code into a scan code that
                /// distinguishes the left- and right-hand keys.
                /// </summary>
                MAPVK_VK_TO_VSC_EX = 4
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Keyboard Methods
            /// <summary>
            /// This method translates a virtual-key code into a scan code or
            /// character value, or translates a scan code into a virtual-key
            /// code.
            /// </summary>
            /// <param name="value">
            /// The virtual-key code or scan code to be translated.
            /// </param>
            /// <param name="mapType">
            /// The kind of translation to perform.
            /// </param>
            /// <returns>
            /// The translated value, or zero if there is no translation.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern uint MapVirtualKey(
                uint value,
                VirtualKeyMapType mapType
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method synthesizes a keystroke, simulating a key press or
            /// release.
            /// </summary>
            /// <param name="vk">
            /// The virtual-key code of the key to be simulated.
            /// </param>
            /// <param name="scan">
            /// The hardware scan code for the key.
            /// </param>
            /// <param name="flags">
            /// The flags that control the various aspects of the simulated
            /// keystroke.
            /// </param>
            /// <param name="extraInfo">
            /// An additional value associated with the keystroke.
            /// </param>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern void keybd_event(
                VirtualKeyCode vk,
                byte scan,
                KeyEventFlags flags,
                IntPtr extraInfo
            );
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Handle Methods
            /// <summary>
            /// This method marks pending synchronous input/output operations
            /// that are issued by the specified thread as canceled.
            /// </summary>
            /// <param name="thread">
            /// A handle to the thread whose synchronous operations are to be
            /// canceled.
            /// </param>
            /// <returns>
            /// True if the operation succeeded; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CancelSynchronousIo(IntPtr thread);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method closes an open object handle.
            /// </summary>
            /// <param name="handle">
            /// A valid handle to an open object.
            /// </param>
            /// <returns>
            /// True if the operation succeeded; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr handle);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Process Methods
            /* NOTE: Needed for use with NtQueryInformationProcess. */
            /// <summary>
            /// The access right required to query certain information about a
            /// process.
            /// </summary>
            internal const uint PROCESS_QUERY_INFORMATION = 0x400;

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves the process affinity mask for the
            /// specified process and the system affinity mask for the system.
            /// </summary>
            /// <param name="process">
            /// A handle to the process whose affinity mask is desired.
            /// </param>
            /// <param name="processAffinityMask">
            /// Upon success, receives the affinity mask for the specified
            /// process.
            /// </param>
            /// <param name="systemAffinityMask">
            /// Upon success, receives the affinity mask for the system.
            /// </param>
            /// <returns>
            /// True if the operation succeeded; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetProcessAffinityMask(IntPtr process,
                ref IntPtr processAffinityMask, ref IntPtr systemAffinityMask);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This enumeration specifies the kind of process information to be
            /// retrieved, corresponding to the native PROCESSINFOCLASS values.
            /// </summary>
            [ObjectId("07983ce3-bac0-48af-b98b-b88eac97cc08")]
            internal enum PROCESSINFOCLASS
            {
                /// <summary>
                /// Requests the basic process information block.
                /// </summary>
                ProcessBasicInformation
                // ...
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure contains basic information about a process,
            /// corresponding to the native PROCESS_BASIC_INFORMATION structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("bc739fdb-8d62-482d-a529-ba64bbfa388d")]
            internal struct PROCESS_BASIC_INFORMATION
            {
                /// <summary>
                /// The exit status of the process.
                /// </summary>
                public /* NTSTATUS */ int ExitStatus;
                /// <summary>
                /// A pointer to the process environment block (PEB) of the
                /// process.
                /// </summary>
                public /* PPEB */ IntPtr PebBaseAddress;
                /// <summary>
                /// The processor affinity mask for the process.
                /// </summary>
                public /* KAFFINITY */ IntPtr AffinityMask;
                /// <summary>
                /// The base priority of the process.
                /// </summary>
                public /* KPRIORITY */ int BasePriority;
                /// <summary>
                /// The unique identifier of the process.
                /// </summary>
                public /* HANDLE */ IntPtr UniqueProcessId;
                /// <summary>
                /// The unique identifier of the parent process.
                /// </summary>
                public /* HANDLE */ IntPtr InheritedFromUniqueProcessId;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves information about the specified process.
            /// </summary>
            /// <param name="process">
            /// A handle to the process for which information is to be
            /// retrieved.
            /// </param>
            /// <param name="processInformationClass">
            /// The kind of process information to be retrieved.
            /// </param>
            /// <param name="processInformation">
            /// Upon success, receives the requested process information.
            /// </param>
            /// <param name="processInformationLength">
            /// The size, in bytes, of the buffer that receives the process
            /// information.
            /// </param>
            /// <param name="returnLength">
            /// Upon return, receives the number of bytes written to the
            /// process information buffer.
            /// </param>
            /// <returns>
            /// An NTSTATUS code indicating success or the reason for failure.
            /// </returns>
            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern int NtQueryInformationProcess(
                /* HANDLE */ IntPtr process,
                PROCESSINFOCLASS processInformationClass,
                /* PVOID */ ref PROCESS_BASIC_INFORMATION processInformation,
                /* ULONG */ uint processInformationLength,
                /* PULONG */ ref uint returnLength
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This enumeration specifies the mandatory integrity level of a
            /// process, corresponding to the native security mandatory RID
            /// values from "WinNT.h".
            /// </summary>
            [ObjectId("1d24e68f-8f1a-433c-8dae-940847e02cbd")]
            internal enum ProcessIntegrityLevel /* NOTE: From "WinNT.h". */
            {
                /// <summary>
                /// The integrity level is not supported on this platform.
                /// </summary>
                /* SECURITY_MANDATORY_UNSUPPORTED_RID */ UNSUPPORTED_INTEGRITY = -2,
                /// <summary>
                /// The integrity level could not be determined.
                /// </summary>
                /* SECURITY_MANDATORY_UNKNOWN_RID */ UNKNOWN_INTEGRITY = -1,
                /// <summary>
                /// The untrusted integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_UNTRUSTED_RID */ UNTRUSTED_INTEGRITY = 0x0000,
                /// <summary>
                /// The low integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_LOW_RID */ LOW_INTEGRITY = 0x1000,
                /// <summary>
                /// The medium integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_MEDIUM_RID */ MEDIUM_INTEGRITY = 0x2000,
                /// <summary>
                /// The medium-plus integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_MEDIUM_PLUS_RID */ MEDIUM_PLUS_INTEGRITY = 0x2100,
                /// <summary>
                /// The high integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_HIGH_RID */ HIGH_INTEGRITY = 0x3000,
                /// <summary>
                /// The system integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_SYSTEM_RID */ SYSTEM_INTEGRITY = 0x4000,
                /// <summary>
                /// The protected-process integrity level.
                /// </summary>
                /* SECURITY_MANDATORY_PROTECTED_PROCESS_RID */ PROTECTED_PROCESS = 0x5000
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves the mandatory integrity level associated
            /// with the specified access token.
            /// </summary>
            /// <param name="token">
            /// A handle to the access token whose integrity level is desired.
            /// </param>
            /// <param name="integrityLevel">
            /// Upon success, receives the integrity level of the access token.
            /// </param>
            /// <returns>
            /// An HRESULT indicating success or the reason for failure.
            /// </returns>
            [DllImport(DllName.IeRtUtil,
                CallingConvention = CallingConvention.Winapi, EntryPoint = "#35")]
            internal static extern /* HRESULT */ int GetProcessIntegrityLevel(
                /* HANDLE */ IntPtr token,
                /* LPDWORD */ ref ProcessIntegrityLevel integrityLevel
            );
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Threading Methods
            /* NOTE: Needed for use with QueueUserAPC. */
            /// <summary>
            /// The access right required to set the context of a thread.
            /// </summary>
            internal const uint THREAD_SET_CONTEXT = 0x10;

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method opens an existing local process object.
            /// </summary>
            /// <param name="desiredAccess">
            /// The access rights desired for the process object.
            /// </param>
            /// <param name="inheritHandle">
            /// Non-zero if the returned handle should be inheritable by child
            /// processes; otherwise, zero.
            /// </param>
            /// <param name="processId">
            /// The identifier of the process to be opened.
            /// </param>
            /// <returns>
            /// A handle to the opened process, or zero if the operation
            /// failed.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr OpenProcess(uint desiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method opens the access token associated with a process.
            /// </summary>
            /// <param name="handle">
            /// A handle to the process whose access token is to be opened.
            /// </param>
            /// <param name="desiredAccess">
            /// The access rights desired for the access token.
            /// </param>
            /// <param name="token">
            /// Upon success, receives a handle to the opened access token.
            /// </param>
            /// <returns>
            /// A non-zero value if the operation succeeded; otherwise, zero.
            /// </returns>
            [DllImport(DllName.AdvApi32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr OpenProcessToken(IntPtr handle,
                uint desiredAccess, ref IntPtr token);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method opens an existing thread object.
            /// </summary>
            /// <param name="desiredAccess">
            /// The access rights desired for the thread object.
            /// </param>
            /// <param name="inheritHandle">
            /// Non-zero if the returned handle should be inheritable by child
            /// processes; otherwise, zero.
            /// </param>
            /// <param name="threadId">
            /// The identifier of the thread to be opened.
            /// </param>
            /// <returns>
            /// A handle to the opened thread, or zero if the operation failed.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr OpenThread(uint desiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint threadId);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method queues an asynchronous procedure call (APC) to the
            /// specified thread.
            /// </summary>
            /// <param name="proc">
            /// The callback to be invoked when the thread enters an alertable
            /// state.
            /// </param>
            /// <param name="thread">
            /// A handle to the thread to which the APC is to be queued.
            /// </param>
            /// <param name="data">
            /// A value passed to the callback when it is invoked.
            /// </param>
            /// <returns>
            /// A non-zero value if the APC was queued successfully; otherwise,
            /// zero.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint QueueUserAPC(ApcCallback proc, IntPtr thread, IntPtr data);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Memory Constants
            /// <summary>
            /// No heap options are specified.
            /// </summary>
            internal const uint HEAP_NONE = 0x00000000;

            /// <summary>
            /// All memory blocks allocated from the heap allow code execution.
            /// </summary>
            internal const uint HEAP_CREATE_ENABLE_EXECUTE = 0x00040000;

            /// <summary>
            /// The system raises an exception to indicate a function failure
            /// instead of returning a null value.
            /// </summary>
            internal const uint HEAP_GENERATE_EXCEPTIONS = 0x00000004;

            /// <summary>
            /// Serialized access to the heap is not used.
            /// </summary>
            internal const uint HEAP_NO_SERIALIZE = 0x00000001;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Memory Methods
            /// <summary>
            /// This structure contains information about the current state of
            /// both physical and virtual memory, corresponding to the native
            /// MEMORYSTATUSEX structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("ad38df98-9796-41ee-99b0-8543b52ca6a7")]
            internal struct MEMORYSTATUSEX
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public uint dwLength;
                /// <summary>
                /// A number between zero and one hundred that specifies the
                /// approximate percentage of physical memory that is in use.
                /// </summary>
                public uint dwMemoryLoad;
                /// <summary>
                /// The amount of actual physical memory, in bytes.
                /// </summary>
                public ulong ullTotalPhys;
                /// <summary>
                /// The amount of physical memory currently available, in bytes.
                /// </summary>
                public ulong ullAvailPhys;
                /// <summary>
                /// The current committed memory limit for the system or the
                /// current process, whichever is smaller, in bytes.
                /// </summary>
                public ulong ullTotalPageFile;
                /// <summary>
                /// The maximum amount of memory the current process can commit,
                /// in bytes.
                /// </summary>
                public ulong ullAvailPageFile;
                /// <summary>
                /// The size of the user-mode portion of the virtual address
                /// space of the calling process, in bytes.
                /// </summary>
                public ulong ullTotalVirtual;
                /// <summary>
                /// The amount of unreserved and uncommitted memory currently
                /// in the user-mode portion of the virtual address space of the
                /// calling process, in bytes.
                /// </summary>
                public ulong ullAvailVirtual;
                /// <summary>
                /// Reserved; this value is always zero.
                /// </summary>
                public ulong ullAvailExtendedVirtual;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method fills a block of memory with zeros.
            /// </summary>
            /// <param name="pMemory">
            /// A pointer to the starting address of the block of memory to be
            /// filled with zeros.
            /// </param>
            /// <param name="size">
            /// The number of bytes to be filled with zeros.
            /// </param>
            [DllImport(DllName.NtDll, CallingConvention = CallingConvention.Winapi)]
            internal static extern void RtlZeroMemory(IntPtr pMemory, UIntPtr size);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves information about the current state of both
            /// physical and virtual memory for the system.
            /// </summary>
            /// <param name="memoryStatus">
            /// Upon success, receives the current memory status; the size field
            /// of this structure must be initialized prior to the call.
            /// </param>
            /// <returns>
            /// True if the operation succeeded; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX memoryStatus);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method creates a private heap object that can be used by the
            /// calling process.
            /// </summary>
            /// <param name="flags">
            /// The heap allocation options.
            /// </param>
            /// <param name="initialSize">
            /// The initial size of the heap, in bytes.
            /// </param>
            /// <param name="maximumSize">
            /// The maximum size of the heap, in bytes, or zero for a growable
            /// heap.
            /// </param>
            /// <returns>
            /// A handle to the newly created heap, or zero if the operation
            /// failed.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr HeapCreate(
                uint flags, UIntPtr initialSize, UIntPtr maximumSize
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method coalesces adjacent free blocks of memory in the
            /// specified heap.
            /// </summary>
            /// <param name="heap">
            /// A handle to the heap to be compacted.
            /// </param>
            /// <param name="flags">
            /// The heap access options.
            /// </param>
            /// <returns>
            /// The size, in bytes, of the largest committed free block in the
            /// heap, or zero if the operation failed.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern UIntPtr HeapCompact(IntPtr heap, uint flags);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method destroys the specified heap object and releases all
            /// of its memory.
            /// </summary>
            /// <param name="heap">
            /// A handle to the heap to be destroyed.
            /// </param>
            /// <returns>
            /// True if the operation succeeded; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool HeapDestroy(IntPtr heap);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Constants
            /// <summary>
            /// The system error code indicating that access is denied.
            /// </summary>
            internal const int ERROR_ACCESS_DENIED = 5;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Methods
            /// <summary>
            /// This enumeration specifies the error mode flags that control how
            /// the system handles certain serious errors, corresponding to the
            /// native SEM_* values.
            /// </summary>
            [Flags()]
            [ObjectId("b55212c7-b9ff-4812-a08b-b9fadcd864d1")]
            internal enum SystemErrorMode : uint
            {
                /// <summary>
                /// A sentinel value used to represent an invalid error mode.
                /// </summary>
                SEM_ERROR = 0xFFFF,
                /// <summary>
                /// No error mode flags are specified.
                /// </summary>
                SEM_NONE = 0x0000,

                /// <summary>
                /// The system does not display the critical-error-handler
                /// message box.
                /// </summary>
                SEM_FAILCRITICALERRORS = 0x0001,
                /// <summary>
                /// The system does not display the Windows Error Reporting
                /// dialog.
                /// </summary>
                SEM_NOGPFAULTERRORBOX = 0x0002,
                /// <summary>
                /// The system automatically fixes memory alignment faults and
                /// makes them invisible to the application.
                /// </summary>
                SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
                /// <summary>
                /// The system does not display a message box when it fails to
                /// find a file.
                /// </summary>
                SEM_NOOPENFILEERRORBOX = 0x8000
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves the error mode for the current process.
            /// </summary>
            /// <returns>
            /// The current error mode for the process.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern SystemErrorMode GetErrorMode();

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method controls whether the system handles certain serious
            /// errors or whether the calling process handles them.
            /// </summary>
            /// <param name="mode">
            /// The new error mode for the process.
            /// </param>
            /// <returns>
            /// The previous error mode for the process.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern SystemErrorMode SetErrorMode(SystemErrorMode mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method installs a top-level exception handler that is called
            /// whenever an unhandled exception occurs in the process.
            /// </summary>
            /// <param name="callback">
            /// The top-level exception filter callback to be installed.
            /// </param>
            /// <returns>
            /// The previously installed top-level exception filter callback, or
            /// null if there was none.
            /// </returns>
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern TopLevelExceptionFilterCallback SetUnhandledExceptionFilter(
                TopLevelExceptionFilterCallback callback
            );
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows String Formatting Constants
            //
            // NOTE: This constant is used with the _snprintf_s() function from
            //       the MSVCRT.  It tells that function to truncate its output
            //       when the buffer size is reached.
            //
            /// <summary>
            /// The value passed to the native _snprintf_s() function to request
            /// that its output be truncated when the buffer size is reached.
            /// </summary>
            internal static UIntPtr _TRUNCATE = new UIntPtr(
                (IntPtr.Size >= sizeof(ulong)) ? ConversionOps.ToULong(-1) :
                ConversionOps.ToUInt(-1));
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows String Formatting Methods
#if MONO || MONO_HACKS
            /// <summary>
            /// This method formats a double-precision floating-point value into
            /// the specified buffer using the native _snprintf() function from
            /// the MSVCRT.
            /// </summary>
            /// <param name="buffer">
            /// The buffer that receives the formatted output.
            /// </param>
            /// <param name="count">
            /// The maximum number of characters to write to the buffer.
            /// </param>
            /// <param name="format">
            /// The format string used to format the value.
            /// </param>
            /// <param name="value">
            /// The double-precision floating-point value to be formatted.
            /// </param>
            /// <returns>
            /// The number of characters written, or a negative value if an
            /// error occurred.
            /// </returns>
            [DllImport(DllName.MsVcRt, EntryPoint = "_snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int msvc_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);
#endif

            /// <summary>
            /// This method formats a variable number of arguments into the
            /// specified buffer using the native _snprintf() function from the
            /// MSVCRT.
            /// </summary>
            /// <param name="buffer">
            /// The buffer that receives the formatted output.
            /// </param>
            /// <param name="count">
            /// The maximum number of characters to write to the buffer.
            /// </param>
            /// <param name="format">
            /// The format string used to format the arguments.
            /// </param>
            /// <returns>
            /// The number of characters written, or a negative value if an
            /// error occurred.
            /// </returns>
            [DllImport(DllName.MsVcRt, EntryPoint = "_snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int msvc_snprintf(StringBuilder buffer, UIntPtr count,
                string format, __arglist);

            //
            // NOTE: For reasons that are not entirely clear, the .NET Core runtime
            //       cannot does not seem to correctly handle calling the "unsafe"
            //       version of this MSVCRT function.  This workaround was obtained
            //       from the StackOverflow page:
            //
            //       https://stackoverflow.com/questions/2479153
            //
            //       This is a bit arcane; however, it has the benefit of working.
            //
            /// <summary>
            /// This method formats a double-precision floating-point value into
            /// the specified buffer using the native _snprintf_s() function from
            /// the MSVCRT, which supports explicit truncation of the output.
            /// </summary>
            /// <param name="buffer">
            /// The buffer that receives the formatted output.
            /// </param>
            /// <param name="sizeOf">
            /// The total size of the buffer, in characters.
            /// </param>
            /// <param name="count">
            /// The maximum number of characters to write to the buffer, or
            /// <see cref="_TRUNCATE" /> to truncate at the buffer size.
            /// </param>
            /// <param name="format">
            /// The format string used to format the value.
            /// </param>
            /// <param name="value">
            /// The double-precision floating-point value to be formatted.
            /// </param>
            /// <returns>
            /// The number of characters written, or a negative value if an
            /// error occurred.
            /// </returns>
            [DllImport(DllName.MsVcRt, EntryPoint = "_snprintf_s",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int msvc_snprintf_s_double(StringBuilder buffer, UIntPtr sizeOf,
                UIntPtr count, string format, double value);
            #endregion
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
            #region Unix System Call Constants
            /// <summary>
            /// The gettid() system call number on the x86 (i386) architecture.
            /// </summary>
            internal static int SYS_gettid_i386 = 224;

            /// <summary>
            /// The gettid() system call number on the IA-64 architecture.
            /// </summary>
            internal static int SYS_gettid_IA64 = 1105;

            /// <summary>
            /// The gettid() system call number on the x64 (AMD64) architecture.
            /// </summary>
            internal static int SYS_gettid_AMD64 = 186;

            /// <summary>
            /// The gettid() system call number on the ARM architecture.
            /// </summary>
            internal static int SYS_gettid_ARM = 224;

            /// <summary>
            /// The gettid() system call number on the ARM64 architecture.
            /// </summary>
            internal static int SYS_gettid_ARM64 = 178;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Constants
            //
            // BUGBUG: These values are probably only portable to Linux.
            //
            /// <summary>
            /// Resolves symbols lazily, only as the code that references them is
            /// executed.
            /// </summary>
            internal const int RTLD_LAZY = 0x1;     /* Bind function calls lazily */

            /// <summary>
            /// Resolves all symbols immediately when the library is loaded.
            /// </summary>
            internal const int RTLD_NOW = 0x2;      /* Bind function calls immediately */

            /// <summary>
            /// Makes the symbols of the loaded library globally available for
            /// subsequently loaded libraries.
            /// </summary>
            internal const int RTLD_GLOBAL = 0x100; /* Make symbols globally available */

            /// <summary>
            /// The opposite of <see cref="RTLD_GLOBAL" />, and the default;
            /// symbols are not made globally available.
            /// </summary>
            internal const int RTLD_LOCAL = 0x000;  /* Opposite of RTLD_GLOBAL, and the default */

            /// <summary>
            /// The default mode combining <see cref="RTLD_NOW" /> and
            /// <see cref="RTLD_GLOBAL" />.
            /// </summary>
            internal const int RTLD_DEFMODE = RTLD_NOW | RTLD_GLOBAL;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // BUGBUG: These values are probably only portable to Linux.
            //
            /// <summary>
            /// A pseudo-handle directing a symbol search to use the default
            /// global search order.
            /// </summary>
            internal static readonly IntPtr RTLD_DEFAULT = IntPtr.Zero; /* Global symbol search */

            /// <summary>
            /// A pseudo-handle directing a symbol search to find the next
            /// occurrence of a symbol after the current library.
            /// </summary>
            internal static readonly IntPtr RTLD_NEXT = new IntPtr(-1); /* Get Next symbol */
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Structures
            /// <summary>
            /// This structure receives information about the shared object and
            /// symbol nearest a given address, corresponding to the native
            /// Dl_info structure returned by dladdr().
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("5c668971-693c-492a-b5a3-8573f689b39e")]
            internal struct Dl_info_t
            {
                /// <summary>
                /// A pointer to the path name of the shared object containing
                /// the address.
                /// </summary>
                public /* const char* */ IntPtr dli_fname;
                /// <summary>
                /// The base address at which the shared object is loaded.
                /// </summary>
                public /* void* */ IntPtr dli_fbase;
                /// <summary>
                /// A pointer to the name of the nearest symbol with an address
                /// lower than the given address.
                /// </summary>
                public /* const char* */ IntPtr dli_sname;
                /// <summary>
                /// The exact address of the nearest symbol.
                /// </summary>
                public /* void* */ IntPtr dli_saddr;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Methods (libc)
            //
            // NOTE: Some systems, such as FreeBSD and OpenBSD, seem to have these in "libc";
            //       however, you cannot actually try to declare them from there because you
            //       get the "stub" versions.  Therefore, we assume they are already globally
            //       available in the process.
            //
            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method loads the specified dynamic library, resolving it
            /// against the global symbols of the process.
            /// </summary>
            /// <param name="fileName">
            /// The name or path of the dynamic library to load.
            /// </param>
            /// <param name="mode">
            /// The flags controlling how the library and its symbols are
            /// resolved.
            /// </param>
            /// <returns>
            /// A handle to the loaded library, or zero if the operation failed.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "dlopen",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libc_dlopen(string fileName, int mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method decrements the reference count for the specified
            /// dynamic library, unloading it when the count reaches zero.
            /// </summary>
            /// <param name="module">
            /// A handle to the dynamic library to unload.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero value.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "dlclose",
                CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            internal static extern int libc_dlclose(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method resolves the address of the named symbol within the
            /// specified dynamic library.
            /// </summary>
            /// <param name="module">
            /// A handle to the dynamic library to search.
            /// </param>
            /// <param name="name">
            /// The name of the symbol to resolve.
            /// </param>
            /// <returns>
            /// The address of the symbol, or zero if it could not be found.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "dlsym",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libc_dlsym(IntPtr module, string name);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method retrieves information about the shared object and
            /// symbol nearest the specified address.
            /// </summary>
            /// <param name="address">
            /// The address to look up.
            /// </param>
            /// <param name="info">
            /// Upon success, receives information about the nearest shared
            /// object and symbol.
            /// </param>
            /// <returns>
            /// A non-zero value on success; otherwise, zero.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "dladdr",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int libc_dladdr(IntPtr address, ref Dl_info_t info);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves a human-readable string describing the most
            /// recent error that occurred during dynamic loading.
            /// </summary>
            /// <returns>
            /// A pointer to the error string, or zero if no error has occurred
            /// since it was last called.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "dlerror",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr libc_dlerror();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Methods (libdl)
            //
            // NOTE: Some systems, such as Linux, seem to have these in "libdl".
            //
            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method loads the specified dynamic library, resolving it
            /// against the global symbols of the process.
            /// </summary>
            /// <param name="fileName">
            /// The name or path of the dynamic library to load.
            /// </param>
            /// <param name="mode">
            /// The flags controlling how the library and its symbols are
            /// resolved.
            /// </param>
            /// <returns>
            /// A handle to the loaded library, or zero if the operation failed.
            /// </returns>
            [DllImport(DllName.LibDL, EntryPoint = "dlopen",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libdl_dlopen(string fileName, int mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method decrements the reference count for the specified
            /// dynamic library, unloading it when the count reaches zero.
            /// </summary>
            /// <param name="module">
            /// A handle to the dynamic library to unload.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero value.
            /// </returns>
            [DllImport(DllName.LibDL, EntryPoint = "dlclose",
                CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            internal static extern int libdl_dlclose(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method resolves the address of the named symbol within the
            /// specified dynamic library.
            /// </summary>
            /// <param name="module">
            /// A handle to the dynamic library to search.
            /// </param>
            /// <param name="name">
            /// The name of the symbol to resolve.
            /// </param>
            /// <returns>
            /// The address of the symbol, or zero if it could not be found.
            /// </returns>
            [DllImport(DllName.LibDL, EntryPoint = "dlsym",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libdl_dlsym(IntPtr module, string name);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method retrieves information about the shared object and
            /// symbol nearest the specified address.
            /// </summary>
            /// <param name="address">
            /// The address to look up.
            /// </param>
            /// <param name="info">
            /// Upon success, receives information about the nearest shared
            /// object and symbol.
            /// </param>
            /// <returns>
            /// A non-zero value on success; otherwise, zero.
            /// </returns>
            [DllImport(DllName.LibDL, EntryPoint = "dladdr",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int libdl_dladdr(IntPtr address, ref Dl_info_t info);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves a human-readable string describing the most
            /// recent error that occurred during dynamic loading.
            /// </summary>
            /// <returns>
            /// A pointer to the error string, or zero if no error has occurred
            /// since it was last called.
            /// </returns>
            [DllImport(DllName.LibDL, EntryPoint = "dlerror",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr libdl_dlerror();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Workaround Methods (macOS)
            /// <summary>
            /// This method determines whether the specified notification token
            /// is valid.
            /// </summary>
            /// <param name="val">
            /// The notification token to be checked.
            /// </param>
            /// <returns>
            /// True if the token is valid; otherwise, false.
            /// </returns>
            [DllImport(DllName.LibSystem)]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool notify_is_valid_token(
                int val /* in */
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method creates an XPC date object representing the current
            /// date and time.
            /// </summary>
            /// <returns>
            /// A pointer to the newly created XPC date object.
            /// </returns>
            [DllImport(DllName.LibSystem)]
            internal static extern /* xpc_object_t */ IntPtr xpc_date_create_from_current();

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method releases a reference to the specified XPC object.
            /// </summary>
            /// <param name="object">
            /// The XPC object whose reference is to be released.
            /// </param>
            [DllImport(DllName.LibXpc)]
            internal static extern void xpc_release(
                /* xpc_object_t */ IntPtr @object /* in */
            );
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Process Methods
            /// <summary>
            /// This method retrieves the process identifier of the parent of
            /// the calling process.
            /// </summary>
            /// <returns>
            /// The process identifier of the parent process.
            /// </returns>
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int getppid();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Threading Methods
            /// <summary>
            /// This method retrieves the unique, system-wide identifier of the
            /// specified thread.
            /// </summary>
            /// <param name="thread">
            /// A handle to the thread whose identifier is desired, or zero for
            /// the calling thread.
            /// </param>
            /// <param name="tid">
            /// Upon success, receives the unique identifier of the thread.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero error number.
            /// </returns>
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_threadid_np(IntPtr thread, ref ulong tid);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Memory Methods
            /// <summary>
            /// This method fills a block of memory with the specified byte
            /// value.
            /// </summary>
            /// <param name="pMemory">
            /// A pointer to the starting address of the block of memory to be
            /// filled.
            /// </param>
            /// <param name="value">
            /// The byte value used to fill the block of memory.
            /// </param>
            /// <param name="size">
            /// The number of bytes to be filled.
            /// </param>
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern void memset(IntPtr pMemory, int value, UIntPtr size);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix String Formatting Methods
#if MONO || MONO_HACKS || NET_STANDARD_20
            /// <summary>
            /// This method formats a double-precision floating-point value into
            /// the specified buffer using the native snprintf() function.
            /// </summary>
            /// <param name="buffer">
            /// The buffer that receives the formatted output.
            /// </param>
            /// <param name="count">
            /// The maximum number of characters to write to the buffer.
            /// </param>
            /// <param name="format">
            /// The format string used to format the value.
            /// </param>
            /// <param name="value">
            /// The double-precision floating-point value to be formatted.
            /// </param>
            /// <returns>
            /// The number of characters that would have been written, or a
            /// negative value if an error occurred.
            /// </returns>
            [DllImport(DllName.LibC, EntryPoint = "snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int ansi_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This delegate matches the "bolt_snprintf_double" function in
            //       the optional Bolt helper library (see "bolt.c").  It is the
            //       native wrapper around the variadic snprintf() and is used on
            //       platforms where the fixed-signature P/Invoke above does not
            //       work (e.g. arm64 macOS).  It is resolved dynamically.
            //
            /// <summary>
            /// This delegate represents the native "bolt_snprintf_double"
            /// function from the optional Bolt helper library, which wraps the
            /// variadic snprintf() to format a double-precision floating-point
            /// value into a buffer.
            /// </summary>
            /// <param name="buffer">
            /// The buffer that receives the formatted output.
            /// </param>
            /// <param name="count">
            /// The maximum number of characters to write to the buffer.
            /// </param>
            /// <param name="format">
            /// The format string used to format the value.
            /// </param>
            /// <param name="value">
            /// The double-precision floating-point value to be formatted.
            /// </param>
            /// <returns>
            /// The number of characters that would have been written, or a
            /// negative value if an error occurred.
            /// </returns>
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            [ObjectId("c3651c6f-b2e7-4cf9-85e3-8c87d7a5c7a6")]
            internal delegate int bolt_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method formats a variable number of arguments into the
            /// specified buffer using the native snprintf() function.
            /// </summary>
            /// <param name="buffer">
            /// The buffer that receives the formatted output.
            /// </param>
            /// <param name="count">
            /// The maximum number of characters to write to the buffer.
            /// </param>
            /// <param name="format">
            /// The format string used to format the arguments.
            /// </param>
            /// <returns>
            /// The number of characters that would have been written, or a
            /// negative value if an error occurred.
            /// </returns>
            [DllImport(DllName.LibC, EntryPoint = "snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int ansi_snprintf(StringBuilder buffer, UIntPtr count,
                string format, __arglist);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Call Methods
            /// <summary>
            /// This method performs the system call whose number is specified,
            /// taking no additional arguments.
            /// </summary>
            /// <param name="number">
            /// The number of the system call to perform.
            /// </param>
            /// <returns>
            /// The result of the system call, which is system-call specific.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "syscall",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int syscall_int(int number);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Delegates (Private Static Data)
            /// <summary>
            /// The object used to synchronize access to the cached dynamic
            /// loading delegates.
            /// </summary>
            internal static readonly object syncRoot = new object();

            /// <summary>
            /// The cached delegate used to load a dynamic library, or null if it
            /// has not yet been resolved.
            /// </summary>
            internal static dlopen dlopen = null;

            /// <summary>
            /// The cached delegate used to unload a dynamic library, or null if
            /// it has not yet been resolved.
            /// </summary>
            internal static dlclose dlclose = null;

            /// <summary>
            /// The cached delegate used to resolve a symbol within a dynamic
            /// library, or null if it has not yet been resolved.
            /// </summary>
            internal static dlsym dlsym = null;

            /// <summary>
            /// The cached delegate used to look up information about a loaded
            /// symbol, or null if it has not yet been resolved.
            /// </summary>
            internal static dladdr dladdr = null;

            /// <summary>
            /// The cached delegate used to retrieve the most recent dynamic
            /// loading error, or null if it has not yet been resolved.
            /// </summary>
            internal static dlerror dlerror = null;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Logging Constants
            /// <summary>
            /// The system logging priority indicating that the system is
            /// unusable.
            /// </summary>
            internal const int LOG_EMERG = 0;          /* system is unusable */

            /// <summary>
            /// The system logging priority indicating that action must be taken
            /// immediately.
            /// </summary>
            internal const int LOG_ALERT = 1;          /* action must be taken immediately */

            /// <summary>
            /// The system logging priority indicating critical conditions.
            /// </summary>
            internal const int LOG_CRIT = 2;           /* critical conditions */

            /// <summary>
            /// The system logging priority indicating error conditions.
            /// </summary>
            internal const int LOG_ERR = 3;            /* error conditions */

            /// <summary>
            /// The system logging priority indicating warning conditions.
            /// </summary>
            internal const int LOG_WARNING = 4;        /* warning conditions */

            /// <summary>
            /// The system logging priority indicating a normal but significant
            /// condition.
            /// </summary>
            internal const int LOG_NOTICE = 5;         /* normal but significant condition */

            /// <summary>
            /// The system logging priority indicating an informational message.
            /// </summary>
            internal const int LOG_INFO = 6;           /* informational */

            /// <summary>
            /// The system logging priority indicating a debug-level message.
            /// </summary>
            internal const int LOG_DEBUG = 7;          /* debug-level messages */

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// The system logging facility for random user-level messages.
            /// </summary>
            internal const int LOG_USER = (1 << 3);    /* random user-level messages */

            /// <summary>
            /// The system logging facility reserved for local use (zero).
            /// </summary>
            internal const int LOG_LOCAL0 = (16 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (one).
            /// </summary>
            internal const int LOG_LOCAL1 = (17 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (two).
            /// </summary>
            internal const int LOG_LOCAL2 = (18 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (three).
            /// </summary>
            internal const int LOG_LOCAL3 = (19 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (four).
            /// </summary>
            internal const int LOG_LOCAL4 = (20 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (five).
            /// </summary>
            internal const int LOG_LOCAL5 = (21 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (six).
            /// </summary>
            internal const int LOG_LOCAL6 = (22 << 3); /* reserved for local use */

            /// <summary>
            /// The system logging facility reserved for local use (seven).
            /// </summary>
            internal const int LOG_LOCAL7 = (23 << 3); /* reserved for local use */
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Logging Methods
            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method writes the specified message to the system log at the
            /// given priority.
            /// </summary>
            /// <param name="priority">
            /// The priority and facility of the log message.
            /// </param>
            /// <param name="message">
            /// The message to be written to the system log.
            /// </param>
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "syslog", CharSet = CharSet.Ansi,
                BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            internal static extern void bare_syslog(
                int priority,  /* in */
                string message /* in */
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            /// <summary>
            /// This method writes the specified message to the system log at the
            /// given priority, using the message as a format string with a
            /// single string argument.
            /// </summary>
            /// <param name="priority">
            /// The priority and facility of the log message.
            /// </param>
            /// <param name="message">
            /// The format string for the message to be written to the system
            /// log.
            /// </param>
            /// <param name="argument1">
            /// The string argument substituted into the format string.
            /// </param>
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "syslog", CharSet = CharSet.Ansi,
                BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            internal static extern void string_syslog(
                int priority,    /* in */
                string message,  /* in */
                string argument1 /* in */
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves a human-readable string describing the
            /// specified error number.
            /// </summary>
            /// <param name="error">
            /// The error number to be described.
            /// </param>
            /// <returns>
            /// A pointer to the string describing the error number.
            /// </returns>
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr strerror(
                int error /* in */
            );
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
#if WINDOWS
        //
        // NOTE: This is the successful value for the NTSTATUS data type.
        //
        /// <summary>
        /// The successful value for the NTSTATUS data type.
        /// </summary>
        private const int STATUS_SUCCESS = 0;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A pre-built native pointer whose value is one.
        /// </summary>
        internal static readonly IntPtr IntPtrOne = new IntPtr(1);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A default, unallocated garbage collector handle used to represent
        /// the absence of a valid pinned handle.
        /// </summary>
#pragma warning disable 649 // NOTE: Yes, this is by design.
        private static readonly GCHandle invalidGCHandle;
#pragma warning restore 649

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        //
        // HACK: These are not read-only.
        //
        /// <summary>
        /// The name of the performance counter category used to track physical
        /// memory on Mono.
        /// </summary>
        private static string MonoMemoryCategoryName = "Mono Memory";

        /// <summary>
        /// The name of the performance counter used to track the total
        /// physical memory on Mono.
        /// </summary>
        private static string MonoTotalPhysicalMemoryCounterName = "Total Physical Memory";

        /// <summary>
        /// The name of the performance counter used to track the available
        /// physical memory on Mono.
        /// </summary>
        private static string MonoAvailablePhysicalMemoryCounterName = "Available Physical Memory";
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the macOS-specific output debug message support is
        /// disabled.
        /// </summary>
        private static bool NoMacintoshOutputDebugMessage = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the static performance
        //       counters (below) that are [only] used to track physical memory
        //       on Mono.
        //
        /// <summary>
        /// This is used to synchronize access to the static performance
        /// counters that are only used to track physical memory on Mono.
        /// </summary>
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the one-time initialization for this class has
        /// already been performed.
        /// </summary>
        private static bool once = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default "priority" value for use with the syslog()
        //       function on Linux and macOS, et al.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default priority value for use with the syslog() function on
        /// Linux and macOS, et al.
        /// </summary>
        private static DebugPriority defaultDebugPriority = DebugPriority.Default;

        /// <summary>
        /// The mapping of debug priority names to their associated priority
        /// values.
        /// </summary>
        private static DebugPriorityDictionary debugPriorities = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These performance counters are only used on Mono for tracking
        //       available and total physical memory.
        //
#if !NET_STANDARD_20
        /// <summary>
        /// The performance counter used to track the total physical memory on
        /// Mono.
        /// </summary>
        private static PerformanceCounter totalPhysicalMemoryCounter = null;

        /// <summary>
        /// The performance counter used to track the available physical memory
        /// on Mono.
        /// </summary>
        private static PerformanceCounter availablePhysicalMemoryCounter = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Windows-Only Abstraction Methods (ALWAYS FAIL ON NON-WINDOWS)
#if WINDOWS
        /// <summary>
        /// This method queries the processor affinity masks for the current
        /// process and its threads.
        /// </summary>
        /// <returns>
        /// The list of processor affinity masks, or a list containing the
        /// error message upon failure.
        /// </returns>
        public static StringList GetProcessorAffinityMasks()
        {
            ReturnCode code;
            StringList list = null;
            Result error = null;

            code = WindowsGetProcessorAffinityMasks(ref list, ref error);

            return (code == ReturnCode.Ok) ? list : new StringList(error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the specified native handle.
        /// </summary>
        /// <param name="handle">
        /// The native handle to close.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the handle was closed successfully; otherwise, false.
        /// </returns>
        public static bool CloseHandle(
            IntPtr handle,   /* in */
            ref Result error /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    if (UNM.CloseHandle(handle)) /* throw */
                        return true;
                    else
                        error = GetErrorMessage();
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens an existing thread object using its identifier.
        /// </summary>
        /// <param name="desiredAccess">
        /// The desired access rights for the thread object.
        /// </param>
        /// <param name="inheritHandle">
        /// Non-zero if the returned handle should be inheritable by child
        /// processes.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread to open.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The native handle to the opened thread, or
        /// <see cref="IntPtr.Zero" /> upon failure.
        /// </returns>
        public static IntPtr OpenThread(
            uint desiredAccess, /* in */
            bool inheritHandle, /* in */
            uint threadId,      /* in */
            ref Result error    /* out */
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    IntPtr thread = UNM.OpenThread(
                        desiredAccess, inheritHandle, threadId);

                    if (IsValidHandle(thread))
                    {
                        return thread;
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "OpenThread({1}) failed with error {0}: {2}",
                            lastError, threadId, GetDynamicLoadingError(
                            lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues an asynchronous procedure call to the specified
        /// thread.
        /// </summary>
        /// <param name="proc">
        /// The callback to be invoked when the asynchronous procedure call is
        /// dispatched.
        /// </param>
        /// <param name="thread">
        /// The native handle to the thread that will run the asynchronous
        /// procedure call.
        /// </param>
        /// <param name="data">
        /// The value to be passed to the callback.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the asynchronous procedure call was queued successfully;
        /// otherwise, false.
        /// </returns>
        public static bool QueueUserApc(
            ApcCallback proc, /* in */
            IntPtr thread,    /* in */
            IntPtr data,      /* in */
            ref Result error  /* out */
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    uint result = UNM.QueueUserAPC(
                        proc, thread, data);

                    return ConversionOps.ToBool(result);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method simulates keyboard input for the specified string of
        /// virtual key codes or characters.
        /// </summary>
        /// <param name="cancelCallback">
        /// The optional callback used to check whether the operation should be
        /// canceled.  This parameter may be null.
        /// </param>
        /// <param name="stringCallback">
        /// The optional callback invoked after each portion of the input has
        /// been simulated.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the callbacks.  This parameter
        /// may be null.
        /// </param>
        /// <param name="value">
        /// The string of characters (or virtual key codes) to simulate.
        /// </param>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep between simulated keys.  A
        /// negative value disables sleeping.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the keyboard input is simulated.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode SimulateKeyboardString(
            CheckCancelCallback cancelCallback, /* in: OPTIONAL */
            CheckStringCallback stringCallback, /* in: OPTIONAL */
            IClientData clientData,             /* in: OPTIONAL */
            string value,                       /* in */
            int milliseconds,                   /* in */
            SimulatedKeyFlags flags,            /* in */
            ref Result error                    /* out */
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return ReturnCode.Error;
            }

            if (value == null)
            {
                error = "invalid virtual key code string";
                return ReturnCode.Error;
            }

            if (FlagOps.HasFlags(
                    flags, SimulatedKeyFlags.Direct, true))
            {
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        null, value, 0, Length.Invalid, true,
                        ref list, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                int count = list.Count;

                for (int index = 0; index < count; index++)
                {
                    VirtualKeyCodeList keyCodes = null;

                    if (MapToVirtualKeyCodes(list[index],
                            flags, ref keyCodes,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (SimulateKeyboardEvents(
                            cancelCallback, clientData, keyCodes,
                            flags, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (milliseconds >= 0)
                        HostOps.ThreadSleep(milliseconds);

                    if ((stringCallback != null) &&
                        !stringCallback(
                            clientData, list[index], null,
                            ref error))
                    {
                        return ReturnCode.Error;
                    }
                }
            }
            else
            {
                char[] characters = value.ToCharArray();
                int length = characters.Length;

                for (int index = 0; index < length; index++)
                {
                    VirtualKeyCodeList keyCodes = null;

                    if (MapToVirtualKeyCodes(characters[index],
                            flags, ref keyCodes,
                            ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (SimulateKeyboardEvents(
                            cancelCallback, clientData, keyCodes,
                            flags, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (milliseconds >= 0)
                        HostOps.ThreadSleep(milliseconds);

                    if ((stringCallback != null) &&
                        !stringCallback(
                            clientData, value, index,
                            ref error))
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method simulates a single keyboard event for the specified
        /// virtual key code.
        /// </summary>
        /// <param name="cancelCallback">
        /// The optional callback used to check whether the operation should be
        /// canceled.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the callback.  This parameter
        /// may be null.
        /// </param>
        /// <param name="keyCode">
        /// The virtual key code to simulate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the keyboard event is simulated.
        /// </param>
        /// <param name="press">
        /// Non-zero to simulate a key press, zero to simulate a key release,
        /// or null to simulate both a press and a release.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode SimulateKeyboardEvent(
            CheckCancelCallback cancelCallback, /* in: OPTIONAL */
            IClientData clientData,             /* in: OPTIONAL */
            VirtualKeyCode keyCode,             /* in */
            SimulatedKeyFlags flags,            /* in */
            bool? press,                        /* in: OPTIONAL, null means press -AND- release */
            ref Result error                    /* out */
            )
        {
            try
            {
                if (!FlagOps.HasFlags(
                        flags, SimulatedKeyFlags.SafeOnly, true) ||
                    IsSafeVirtualKeyCode(keyCode))
                {
                    byte scanCode; /* REUSED */
                    KeyEventFlags keyFlags; /* REUSED */

                    if ((press == null) || (bool)press)
                    {
                        TranslateVirtualKeyCode(
                            keyCode, true, out scanCode,
                            out keyFlags);

                        if ((cancelCallback == null) ||
                            cancelCallback(clientData, ref error))
                        {
                            /* NO RESULT */
                            UNM.keybd_event(
                                keyCode, scanCode, keyFlags,
                                IntPtr.Zero);
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    if ((press == null) || !(bool)press)
                    {
                        TranslateVirtualKeyCode(
                            keyCode, false, out scanCode,
                            out keyFlags);

                        if ((cancelCallback == null) ||
                            cancelCallback(clientData, ref error))
                        {
                            /* NO RESULT */
                            UNM.keybd_event(
                                keyCode, scanCode, keyFlags,
                                IntPtr.Zero);
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "virtual key code {0} is unsafe",
                        FormatOps.WrapOrNull(keyCode));
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method simulates a sequence of keyboard events for the
        /// specified list of virtual key codes.
        /// </summary>
        /// <param name="cancelCallback">
        /// The optional callback used to check whether the operation should be
        /// canceled.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the callback.  This parameter
        /// may be null.
        /// </param>
        /// <param name="keyCodes">
        /// The list of virtual key codes to simulate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the keyboard events are simulated.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode SimulateKeyboardEvents(
            CheckCancelCallback cancelCallback, /* in: OPTIONAL */
            IClientData clientData,             /* in: OPTIONAL */
            VirtualKeyCodeList keyCodes,        /* in */
            SimulatedKeyFlags flags,            /* in */
            ref Result error                    /* out */
            )
        {
            if (keyCodes == null)
            {
                error = "invalid key codes";
                return ReturnCode.Error;
            }

            if (!FlagOps.HasFlags(
                    flags, SimulatedKeyFlags.NoReverse, true) &&
                (keyCodes.Count > 1))
            {
                VirtualKeyCodeList reverseKeyCodes =
                    new VirtualKeyCodeList(keyCodes);

                reverseKeyCodes.Reverse(); /* O(N) */

                foreach (VirtualKeyCode keyCode in keyCodes)
                {
                    if (SimulateKeyboardEvent(
                            cancelCallback, clientData, keyCode, flags,
                            true, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                foreach (VirtualKeyCode keyCode in reverseKeyCodes)
                {
                    if (SimulateKeyboardEvent(
                            cancelCallback, clientData, keyCode, flags,
                            false, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }
            else
            {
                foreach (VirtualKeyCode keyCode in keyCodes)
                {
                    if (SimulateKeyboardEvent(
                            cancelCallback, clientData, keyCode, flags,
                            null, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINDOWS || UNIX
        /// <summary>
        /// This method securely zeroes the specified region of native memory.
        /// </summary>
        /// <param name="pMemory">
        /// The native pointer to the start of the memory region to zero.
        /// </param>
        /// <param name="size">
        /// The size, in bytes, of the memory region to zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        [MethodImpl(
            MethodImplOptions.NoInlining
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
            | MethodImplOptions.NoOptimization
#endif
        )]
        public static ReturnCode ZeroMemory(
            IntPtr pMemory,  /* in */
            uint size,       /* in */
            ref Result error /* out */
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    UNM.RtlZeroMemory(pMemory, new UIntPtr(size));
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
            {
                try
                {
                    UNM.memset(pMemory, 0, new UIntPtr(size));
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
#endif

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Securely zero the contents of a managed byte array by pinning it
        //       and zeroing via the platform primitive (RtlZeroMemory on Windows,
        //       memset() on Unix). Routing through the native primitive prevents
        //       the managed JIT from eliding the write as a dead store.
        //
        /// <summary>
        /// This method securely zeroes the contents of the specified managed
        /// byte array.
        /// </summary>
        /// <param name="array">
        /// The managed byte array whose contents should be zeroed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        [MethodImpl(
            MethodImplOptions.NoInlining
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
            | MethodImplOptions.NoOptimization
#endif
        )]
        public static ReturnCode ZeroMemory(
            byte[] array,    /* in */
            ref Result error /* out */
            )
        {
            if (array == null)
            {
                error = "invalid array";
                return ReturnCode.Error;
            }

            int length = array.Length;

            if (length <= 0)
                return ReturnCode.Ok;

            GCHandle handle = GetInvalidGCHandle();

            try
            {
                handle = GCHandle.Alloc(array, GCHandleType.Pinned);

                if (handle.IsAllocated)
                {
                    IntPtr pMemory = handle.AddrOfPinnedObject();

                    if (pMemory != IntPtr.Zero)
                    {
                        return ZeroMemory(
                            pMemory, (uint)length, ref error);
                    }
                    else
                    {
                        error = "could not get address of pinned array";
                    }
                }
                else
                {
                    error = "could not allocate pinned array";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Windows-Only Helper Methods
#if WINDOWS
        /// <summary>
        /// This method determines whether the specified virtual key code is
        /// considered safe to simulate (i.e. it corresponds to a normal
        /// printable key or a benign modifier).
        /// </summary>
        /// <param name="keyCode">
        /// The virtual key code to check.
        /// </param>
        /// <returns>
        /// True if the virtual key code is considered safe to simulate;
        /// otherwise, false.
        /// </returns>
        private static bool IsSafeVirtualKeyCode(
            VirtualKeyCode keyCode /* in */
            )
        {
            switch (keyCode)
            {
                case VirtualKeyCode.VK_OEM_3:      // '`~'
                case VirtualKeyCode.VK_OEM_MINUS:  // '-'
                case VirtualKeyCode.VK_OEM_PLUS:   // '+'
                case VirtualKeyCode.VK_BACK:       // Backspace
                case VirtualKeyCode.VK_TAB:        // HorizontalTab
                case VirtualKeyCode.VK_OEM_4:      // '[{'
                case VirtualKeyCode.VK_OEM_6:      // ']}'
                case VirtualKeyCode.VK_OEM_5:      // '\|'
                case VirtualKeyCode.VK_OEM_1:      // ';:'
                case VirtualKeyCode.VK_OEM_7:      // ''"'
                case VirtualKeyCode.VK_RETURN:     // CarriageReturn
                case VirtualKeyCode.VK_SHIFT:      // Modifier
                case VirtualKeyCode.VK_OEM_COMMA:  // ','
                case VirtualKeyCode.VK_OEM_PERIOD: // '.'
                case VirtualKeyCode.VK_OEM_2:      // '/?'
                case VirtualKeyCode.VK_SPACE:      // ' '
                case VirtualKeyCode.VK_0:
                case VirtualKeyCode.VK_1:
                case VirtualKeyCode.VK_2:
                case VirtualKeyCode.VK_3:
                case VirtualKeyCode.VK_4:
                case VirtualKeyCode.VK_5:
                case VirtualKeyCode.VK_6:
                case VirtualKeyCode.VK_7:
                case VirtualKeyCode.VK_8:
                case VirtualKeyCode.VK_9:
                case VirtualKeyCode.VK_A:
                case VirtualKeyCode.VK_B:
                case VirtualKeyCode.VK_C:
                case VirtualKeyCode.VK_D:
                case VirtualKeyCode.VK_E:
                case VirtualKeyCode.VK_F:
                case VirtualKeyCode.VK_G:
                case VirtualKeyCode.VK_H:
                case VirtualKeyCode.VK_I:
                case VirtualKeyCode.VK_J:
                case VirtualKeyCode.VK_K:
                case VirtualKeyCode.VK_L:
                case VirtualKeyCode.VK_M:
                case VirtualKeyCode.VK_N:
                case VirtualKeyCode.VK_O:
                case VirtualKeyCode.VK_P:
                case VirtualKeyCode.VK_Q:
                case VirtualKeyCode.VK_R:
                case VirtualKeyCode.VK_S:
                case VirtualKeyCode.VK_T:
                case VirtualKeyCode.VK_U:
                case VirtualKeyCode.VK_V:
                case VirtualKeyCode.VK_W:
                case VirtualKeyCode.VK_X:
                case VirtualKeyCode.VK_Y:
                case VirtualKeyCode.VK_Z:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified virtual key code is an
        /// extended key, per the operating system definition of extended keys
        /// (e.g. the navigation cluster, numeric keypad keys, and right-hand
        /// modifier keys).
        /// </summary>
        /// <param name="keyCode">
        /// The virtual key code to check.
        /// </param>
        /// <returns>
        /// True if the virtual key code is an extended key; otherwise, false.
        /// </returns>
        private static bool IsExtendedVirtualKeyCode(
            VirtualKeyCode keyCode /* in */
            )
        {
            //
            // NOTE: From MSDN: The extended keys consist of the ALT and
            //                  CTRL keys on the right-hand side of the
            //                  keyboard; the INS, DEL, HOME, END, PAGE
            //                  UP, PAGE DOWN, and arrow keys in the
            //                  clusters to the left of the numeric
            //                  keypad; the NUM LOCK key; the BREAK
            //                  (CTRL+PAUSE) key; the PRINT SCRN key;
            //                  and the divide (/) and ENTER keys in the
            //                  numeric keypad.
            //
            switch (keyCode)
            {
                case VirtualKeyCode.VK_PRIOR:
                case VirtualKeyCode.VK_NEXT:
                case VirtualKeyCode.VK_END:
                case VirtualKeyCode.VK_HOME:
                case VirtualKeyCode.VK_LEFT:
                case VirtualKeyCode.VK_UP:
                case VirtualKeyCode.VK_RIGHT:
                case VirtualKeyCode.VK_DOWN:
                case VirtualKeyCode.VK_SNAPSHOT:
                case VirtualKeyCode.VK_INSERT:
                case VirtualKeyCode.VK_DELETE:
                case VirtualKeyCode.VK_LWIN:
                case VirtualKeyCode.VK_RWIN:
                case VirtualKeyCode.VK_APPS:
                case VirtualKeyCode.VK_NUMLOCK:
                case VirtualKeyCode.VK_RCONTROL:
                case VirtualKeyCode.VK_RMENU:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses the specified string into a sequence of virtual
        /// key codes, appending them to the supplied list.  Each element of the
        /// string is interpreted as a virtual key code name, optionally falling
        /// back to a prefixed form when permitted by the flags.
        /// </summary>
        /// <param name="value">
        /// The string containing one or more virtual key code names to parse.
        /// </param>
        /// <param name="flags">
        /// The flags used to control the parsing behavior, including whether
        /// the prefixed fallback form is permitted.
        /// </param>
        /// <param name="keyCodes">
        /// Upon success, the parsed virtual key codes are appended to this
        /// list.  If null, a new list is created and returned via this
        /// parameter.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode MapToVirtualKeyCodes(
            string value,                    /* in */
            SimulatedKeyFlags flags,         /* in */
            ref VirtualKeyCodeList keyCodes, /* in, out */
            ref Result error                 /* out */
            )
        {
            if (keyCodes == null)
                keyCodes = new VirtualKeyCodeList();

            if (value == null)
            {
                error = "invalid virtual key code string";
                return ReturnCode.Error;
            }

            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            string prefix = UNM.VirtualKeyCodePrefix;

            foreach (string element in list)
            {
                ResultList errors = null;

                object enumValue; /* REUSED */
                Result localError; /* REUSED */

                localError = null;

                enumValue = EnumOps.TryParse(
                    typeof(VirtualKeyCode), element,
                    true, true, ref localError);

                if (enumValue is VirtualKeyCode)
                {
                    keyCodes.Add((VirtualKeyCode)enumValue);
                    continue;
                }

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                if (!FlagOps.HasFlags(
                        flags, SimulatedKeyFlags.NoFallback, true) &&
                    !SharedStringOps.SystemNoCaseStartsWith(
                        element, prefix))
                {
                    localError = null;

                    enumValue = EnumOps.TryParse(
                        typeof(VirtualKeyCode), String.Format(
                        "{0}{0}", prefix, element), true, true,
                        ref localError);

                    if (enumValue is VirtualKeyCode)
                    {
                        keyCodes.Add((VirtualKeyCode)enumValue);
                        continue;
                    }

                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }

                if (errors != null)
                {
                    error = errors;
                }
                else
                {
                    error = String.Format(
                        "no virtual key code mapping for sub-string {0}",
                        FormatOps.WrapOrNull(element));
                }

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps the specified character to the sequence of virtual
        /// key codes required to produce it, appending them to the supplied
        /// list.  Characters requiring a shifted key produce a shift modifier
        /// followed by the base key.
        /// </summary>
        /// <param name="character">
        /// The character to map to one or more virtual key codes.
        /// </param>
        /// <param name="flags">
        /// The flags used to control the mapping behavior.  This parameter is
        /// not currently used.
        /// </param>
        /// <param name="keyCodes">
        /// Upon success, the mapped virtual key codes are appended to this
        /// list.  If null, a new list is created and returned via this
        /// parameter.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode MapToVirtualKeyCodes(
            char character,                  /* in */
            SimulatedKeyFlags flags,         /* in: NOT USED */
            ref VirtualKeyCodeList keyCodes, /* in, out */
            ref Result error                 /* out */
            )
        {
            if (keyCodes == null)
                keyCodes = new VirtualKeyCodeList();

            switch (character)
            {
                case Characters.GraveAccent:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_3);
                        return ReturnCode.Ok;
                    }
                case Characters.Tilde:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_3);
                        return ReturnCode.Ok;
                    }
                case Characters.One:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_1);
                        return ReturnCode.Ok;
                    }
                case Characters.ExclamationMark:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_1);
                        return ReturnCode.Ok;
                    }
                case Characters.Two:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_2);
                        return ReturnCode.Ok;
                    }
                case Characters.AtSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_2);
                        return ReturnCode.Ok;
                    }
                case Characters.Three:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_3);
                        return ReturnCode.Ok;
                    }
                case Characters.NumberSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_3);
                        return ReturnCode.Ok;
                    }
                case Characters.Four:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_4);
                        return ReturnCode.Ok;
                    }
                case Characters.DollarSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_4);
                        return ReturnCode.Ok;
                    }
                case Characters.Five:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_5);
                        return ReturnCode.Ok;
                    }
                case Characters.PercentSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_5);
                        return ReturnCode.Ok;
                    }
                case Characters.Six:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_6);
                        return ReturnCode.Ok;
                    }
                case Characters.CircumflexAccent:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_6);
                        return ReturnCode.Ok;
                    }
                case Characters.Seven:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_7);
                        return ReturnCode.Ok;
                    }
                case Characters.Ampersand:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_7);
                        return ReturnCode.Ok;
                    }
                case Characters.Eight:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_8);
                        return ReturnCode.Ok;
                    }
                case Characters.Asterisk:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_8);
                        return ReturnCode.Ok;
                    }
                case Characters.Nine:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_9);
                        return ReturnCode.Ok;
                    }
                case Characters.OpenParenthesis:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_9);
                        return ReturnCode.Ok;
                    }
                case Characters.Zero:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_0);
                        return ReturnCode.Ok;
                    }
                case Characters.CloseParenthesis:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_0);
                        return ReturnCode.Ok;
                    }
                case Characters.MinusSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_MINUS);
                        return ReturnCode.Ok;
                    }
                case Characters.Underscore:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_MINUS);
                        return ReturnCode.Ok;
                    }
                case Characters.EqualSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_PLUS);
                        return ReturnCode.Ok;
                    }
                case Characters.PlusSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_PLUS);
                        return ReturnCode.Ok;
                    }
                case Characters.Backspace:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_BACK);
                        return ReturnCode.Ok;
                    }
                case Characters.HorizontalTab:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_TAB);
                        return ReturnCode.Ok;
                    }
                case Characters.q:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_Q);
                        return ReturnCode.Ok;
                    }
                case Characters.Q:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_Q);
                        return ReturnCode.Ok;
                    }
                case Characters.w:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_W);
                        return ReturnCode.Ok;
                    }
                case Characters.W:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_W);
                        return ReturnCode.Ok;
                    }
                case Characters.e:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_E);
                        return ReturnCode.Ok;
                    }
                case Characters.E:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_E);
                        return ReturnCode.Ok;
                    }
                case Characters.r:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_R);
                        return ReturnCode.Ok;
                    }
                case Characters.R:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_R);
                        return ReturnCode.Ok;
                    }
                case Characters.t:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_T);
                        return ReturnCode.Ok;
                    }
                case Characters.T:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_T);
                        return ReturnCode.Ok;
                    }
                case Characters.y:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_Y);
                        return ReturnCode.Ok;
                    }
                case Characters.Y:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_Y);
                        return ReturnCode.Ok;
                    }
                case Characters.u:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_U);
                        return ReturnCode.Ok;
                    }
                case Characters.U:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_U);
                        return ReturnCode.Ok;
                    }
                case Characters.i:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_I);
                        return ReturnCode.Ok;
                    }
                case Characters.I:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_I);
                        return ReturnCode.Ok;
                    }
                case Characters.o:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_O);
                        return ReturnCode.Ok;
                    }
                case Characters.O:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_O);
                        return ReturnCode.Ok;
                    }
                case Characters.p:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_P);
                        return ReturnCode.Ok;
                    }
                case Characters.P:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_P);
                        return ReturnCode.Ok;
                    }
                case Characters.OpenBracket:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_4);
                        return ReturnCode.Ok;
                    }
                case Characters.OpenBrace:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_4);
                        return ReturnCode.Ok;
                    }
                case Characters.CloseBracket:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_6);
                        return ReturnCode.Ok;
                    }
                case Characters.CloseBrace:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_6);
                        return ReturnCode.Ok;
                    }
                case Characters.Backslash:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_5);
                        return ReturnCode.Ok;
                    }
                case Characters.Pipe:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_5);
                        return ReturnCode.Ok;
                    }
                case Characters.a:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_A);
                        return ReturnCode.Ok;
                    }
                case Characters.A:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_A);
                        return ReturnCode.Ok;
                    }
                case Characters.s:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_S);
                        return ReturnCode.Ok;
                    }
                case Characters.S:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_S);
                        return ReturnCode.Ok;
                    }
                case Characters.d:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_D);
                        return ReturnCode.Ok;
                    }
                case Characters.D:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_D);
                        return ReturnCode.Ok;
                    }
                case Characters.f:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_F);
                        return ReturnCode.Ok;
                    }
                case Characters.F:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_F);
                        return ReturnCode.Ok;
                    }
                case Characters.g:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_G);
                        return ReturnCode.Ok;
                    }
                case Characters.G:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_G);
                        return ReturnCode.Ok;
                    }
                case Characters.h:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_H);
                        return ReturnCode.Ok;
                    }
                case Characters.H:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_H);
                        return ReturnCode.Ok;
                    }
                case Characters.j:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_J);
                        return ReturnCode.Ok;
                    }
                case Characters.J:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_J);
                        return ReturnCode.Ok;
                    }
                case Characters.k:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_K);
                        return ReturnCode.Ok;
                    }
                case Characters.K:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_K);
                        return ReturnCode.Ok;
                    }
                case Characters.l:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_L);
                        return ReturnCode.Ok;
                    }
                case Characters.L:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_L);
                        return ReturnCode.Ok;
                    }
                case Characters.SemiColon:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_1);
                        return ReturnCode.Ok;
                    }
                case Characters.Colon:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_1);
                        return ReturnCode.Ok;
                    }
                case Characters.Apostrophe:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_7);
                        return ReturnCode.Ok;
                    }
                case Characters.QuotationMark:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_7);
                        return ReturnCode.Ok;
                    }
                case Characters.CarriageReturn:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_RETURN);
                        return ReturnCode.Ok;
                    }
                case Characters.z:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_Z);
                        return ReturnCode.Ok;
                    }
                case Characters.Z:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_Z);
                        return ReturnCode.Ok;
                    }
                case Characters.x:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_X);
                        return ReturnCode.Ok;
                    }
                case Characters.X:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_X);
                        return ReturnCode.Ok;
                    }
                case Characters.c:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_C);
                        return ReturnCode.Ok;
                    }
                case Characters.C:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_C);
                        return ReturnCode.Ok;
                    }
                case Characters.v:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_V);
                        return ReturnCode.Ok;
                    }
                case Characters.V:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_V);
                        return ReturnCode.Ok;
                    }
                case Characters.b:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_B);
                        return ReturnCode.Ok;
                    }
                case Characters.B:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_B);
                        return ReturnCode.Ok;
                    }
                case Characters.n:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_N);
                        return ReturnCode.Ok;
                    }
                case Characters.N:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_N);
                        return ReturnCode.Ok;
                    }
                case Characters.m:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_M);
                        return ReturnCode.Ok;
                    }
                case Characters.M:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_M);
                        return ReturnCode.Ok;
                    }
                case Characters.Comma:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_COMMA);
                        return ReturnCode.Ok;
                    }
                case Characters.LessThanSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_COMMA);
                        return ReturnCode.Ok;
                    }
                case Characters.Period:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_PERIOD);
                        return ReturnCode.Ok;
                    }
                case Characters.GreaterThanSign:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_PERIOD);
                        return ReturnCode.Ok;
                    }
                case Characters.Slash:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_OEM_2);
                        return ReturnCode.Ok;
                    }
                case Characters.QuestionMark:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SHIFT);
                        keyCodes.Add(VirtualKeyCode.VK_OEM_2);
                        return ReturnCode.Ok;
                    }
                case Characters.Space:
                    {
                        keyCodes.Add(VirtualKeyCode.VK_SPACE);
                        return ReturnCode.Ok;
                    }
                default:
                    {
                        error = String.Format(
                            "virtual key code mapping for character {0} is missing",
                            (int)character);

                        return ReturnCode.Error;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified virtual key code into its
        /// hardware scan code and the keyboard event flags appropriate for the
        /// requested key press or release.
        /// </summary>
        /// <param name="keyCode">
        /// The virtual key code to translate.
        /// </param>
        /// <param name="press">
        /// Non-zero if the key is being pressed; zero if the key is being
        /// released.
        /// </param>
        /// <param name="scanCode">
        /// Upon return, this contains the hardware scan code corresponding to
        /// the virtual key code.
        /// </param>
        /// <param name="keyFlags">
        /// Upon return, this contains the keyboard event flags appropriate for
        /// the requested key press or release.
        /// </param>
        private static void TranslateVirtualKeyCode(
            VirtualKeyCode keyCode,    /* in */
            bool press,                /* in */
            out byte scanCode,         /* out */
            out KeyEventFlags keyFlags /* out */
            )
        {
            VirtualKeyMapType mapType = VirtualKeyMapType.MAPVK_VK_TO_VSC;

            scanCode = (byte)UNM.MapVirtualKey((uint)keyCode, mapType);

            keyFlags = KeyEventFlags.KEYEVENTF_NONE;

            if (IsExtendedVirtualKeyCode(keyCode))
                keyFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            if (!press)
                keyFlags |= KeyEventFlags.KEYEVENTF_KEYUP;

            if (scanCode > 0)
                keyFlags |= KeyEventFlags.KEYEVENTF_SCANCODE;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Windows Specific Methods (DO NOT CALL)
#if WINDOWS
        /// <summary>
        /// This method performs the platform-specific workarounds required for
        /// proper operation on Windows systems.  Currently, it does nothing.
        /// </summary>
        private static void WindowsPlatformWorkarounds()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the memory status structure used to query
        /// system memory information on Windows systems.
        /// </summary>
        /// <param name="memoryStatus">
        /// Upon return, this parameter will receive the newly initialized memory
        /// status structure.
        /// </param>
        private static void WindowsInitializeMemoryStatus(
            out UNM.MEMORYSTATUSEX memoryStatus /* out */
            )
        {
            memoryStatus = new UNM.MEMORYSTATUSEX();

            memoryStatus.dwLength = (uint)Marshal.SizeOf(
                typeof(UNM.MEMORYSTATUSEX));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the current memory
        /// status on Windows systems.
        /// </summary>
        /// <param name="memoryStatus">
        /// Upon success, this parameter will receive the current memory status.
        /// </param>
        /// <returns>
        /// True if the memory status was obtained; otherwise, false.
        /// </returns>
        private static bool WindowsGetMemoryStatus(
            ref UNM.MEMORYSTATUSEX memoryStatus /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return UNM.GlobalMemoryStatusEx(ref memoryStatus);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the process and system
        /// processor affinity masks on Windows systems.
        /// </summary>
        /// <param name="list">
        /// Upon success, this parameter will receive the list of processor
        /// affinity masks.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive information about the error
        /// that was encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode WindowsGetProcessorAffinityMasks(
            ref StringList list, /* out */
            ref Result error     /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    IntPtr processAffinityMask = IntPtr.Zero;
                    IntPtr systemAffinityMask = IntPtr.Zero;

                    if (UNM.GetProcessAffinityMask(
                            ProcessOps.GetHandle(), ref processAffinityMask,
                            ref systemAffinityMask))
                    {
                        list = new StringList(
                            "process", FormatOps.Hexadecimal(processAffinityMask, true),
                            "system", FormatOps.Hexadecimal(systemAffinityMask, true));

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "GetProcessAffinityMask() failed with error {0}: {1}",
                            lastError, GetErrorMessage(lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method cancels any pending synchronous I/O operation being
        /// performed by the specified thread on Windows systems.
        /// </summary>
        /// <param name="thread">
        /// The handle of the thread whose synchronous I/O operation is being
        /// cancelled.
        /// </param>
        /// <returns>
        /// True if the operation was cancelled; otherwise, false.
        /// </returns>
        private static bool WindowsCancelSynchronousIo(
            IntPtr thread /* in */
            )
        {
            try
            {
                return UNM.CancelSynchronousIo(thread);
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for a handle to the current
        /// thread on Windows systems.
        /// </summary>
        /// <returns>
        /// The handle of the current thread, or <see cref="IntPtr.Zero" /> if it
        /// cannot be determined.
        /// </returns>
        private static IntPtr WindowsGetCurrentThread()
        {
            try
            {
                return SafeNativeMethods.GetCurrentThread();
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the identifier of the
        /// current thread on Windows systems.
        /// </summary>
        /// <returns>
        /// The identifier of the current thread, or <see cref="IntPtr.Zero" />
        /// if it cannot be determined.
        /// </returns>
        private static IntPtr WindowsGetCurrentThreadId()
        {
            try
            {
                return new IntPtr(
                    SafeNativeMethods.GetCurrentThreadId());
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the identifier of the
        /// parent of the specified process on Windows systems.
        /// </summary>
        /// <param name="processId">
        /// The identifier of the process whose parent is being queried; if this
        /// is <see cref="IntPtr.Zero" />, the current process is used.
        /// </param>
        /// <returns>
        /// The identifier of the parent process, or <see cref="IntPtr.Zero" />
        /// if it cannot be determined.
        /// </returns>
        private static IntPtr WindowsGetParentProcessId(
            IntPtr processId /* in */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (processId != IntPtr.Zero)
                {
                    handle = UNM.OpenProcess(
                        UNM.PROCESS_QUERY_INFORMATION, false,
                        ConversionOps.ToUInt(processId));

                    if (handle == IntPtr.Zero)
                        return IntPtr.Zero;
                }

                UNM.PROCESS_BASIC_INFORMATION processInformation =
                    new UNM.PROCESS_BASIC_INFORMATION();

                uint returnLength = 0;

                if (UNM.NtQueryInformationProcess(
                        (handle != IntPtr.Zero) ?
                            handle : SafeNativeMethods.GetCurrentProcess(),
                        UNM.PROCESSINFOCLASS.ProcessBasicInformation,
                        ref processInformation, (uint)Marshal.SizeOf(processInformation),
                        ref returnLength) == STATUS_SUCCESS)
                {
                    return processInformation.InheritedFromUniqueProcessId;
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                if (IsValidHandle(handle))
                {
                    try
                    {
                        UNM.CloseHandle(handle); /* throw */
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeOps).Name,
                            TracePriority.NativeError);
                    }

                    handle = IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the integrity level of
        /// the specified process on Windows systems.
        /// </summary>
        /// <param name="processId">
        /// The identifier of the process whose integrity level is being queried;
        /// if this is <see cref="IntPtr.Zero" />, the current process is used.
        /// </param>
        /// <param name="level">
        /// Upon success, this parameter will receive the integrity level of the
        /// process.
        /// </param>
        /// <returns>
        /// True if the integrity level was obtained; otherwise, false.
        /// </returns>
        private static bool WindowsGetIntegrityLevel(
            IntPtr processId, /* in */
            ref string level  /* out */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (processId != IntPtr.Zero)
                {
                    handle = UNM.OpenProcess(
                        UNM.PROCESS_QUERY_INFORMATION, false,
                        ConversionOps.ToUInt(processId));

                    if (handle == IntPtr.Zero)
                        return false;
                }

                IntPtr token = IntPtr.Zero;

                try
                {
                    int hResult;

                    UNM.ProcessIntegrityLevel integrityLevel =
                        UNM.ProcessIntegrityLevel.UNKNOWN_INTEGRITY;

                    hResult = UNM.GetProcessIntegrityLevel(
                        token, ref integrityLevel);

                    switch (hResult)
                    {
                        case 0: /* HRESULT: S_OK */
                            {
                                level = StringList.MakeList(
                                    (int)integrityLevel, integrityLevel);

                                return true;
                            }
                        case 1: /* HRESULT: S_FALSE */
                            {
                                integrityLevel = UNM.ProcessIntegrityLevel.UNSUPPORTED_INTEGRITY;
                                goto case 0;
                            }
                        default: /* HRESULT: E_FAIL, etc. */
                            {
                                return false;
                            }
                    }
                }
                finally
                {
                    if (IsValidHandle(token))
                    {
                        try
                        {
                            UNM.CloseHandle(token); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(NativeOps).Name,
                                TracePriority.NativeError);
                        }

                        token = IntPtr.Zero;
                    }
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                if (IsValidHandle(handle))
                {
                    try
                    {
                        UNM.CloseHandle(handle); /* throw */
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeOps).Name,
                            TracePriority.NativeError);
                    }

                    handle = IntPtr.Zero;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the current system error
        /// mode on Windows systems.
        /// </summary>
        /// <returns>
        /// The current system error mode, or <c>SEM_ERROR</c> if it cannot be
        /// determined.
        /// </returns>
        private static UNM.SystemErrorMode WindowsGetErrorMode()
        {
            try
            {
                if (PlatformOps.IsWindowsVistaOrHigher())
                {
                    return UNM.GetErrorMode();
                }
                else
                {
                    return UNM.SetErrorMode(
                        UNM.SystemErrorMode.SEM_NONE);
                }
            }
            catch
            {
                // do nothing.
            }

            return UNM.SystemErrorMode.SEM_ERROR;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the system error mode on Windows systems.
        /// </summary>
        /// <param name="mode">
        /// The system error mode to set.
        /// </param>
        /// <returns>
        /// The previous system error mode, or <c>SEM_ERROR</c> if it cannot be
        /// determined.
        /// </returns>
        private static UNM.SystemErrorMode WindowsSetErrorMode(
            UNM.SystemErrorMode mode /* in */
            )
        {
            try
            {
                return UNM.SetErrorMode(mode);
            }
            catch
            {
                // do nothing.
            }

            return UNM.SystemErrorMode.SEM_ERROR;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the human-readable message associated with the
        /// specified error number on Windows systems.
        /// </summary>
        /// <param name="error">
        /// The error number whose message is being queried.
        /// </param>
        /// <returns>
        /// The error message string, or null if it cannot be determined.
        /// </returns>
        private static string WindowsGetErrorMessage(
            int error /* in */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                FormatMessageFlags flags =
                    FormatMessageFlags.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                    FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS |
                    FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM;

                if (UNM.FormatMessage(
                        flags, IntPtr.Zero, ConversionOps.ToUInt(error),
                        0, ref handle, 0, IntPtr.Zero) != 0)
                {
                    return Marshal.PtrToStringAuto(handle);
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    UNM.LocalFree(handle);
                    handle = IntPtr.Zero;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the human-readable message associated with the
        /// most recent dynamic loading error on Windows systems.
        /// </summary>
        /// <param name="error">
        /// The error number whose message is being queried.
        /// </param>
        /// <returns>
        /// The error message string, or null if it cannot be determined.
        /// </returns>
        private static string WindowsGetDynamicLoadingError(
            int error /* in */
            )
        {
            if (InitializeDynamicLoading())
                return GetErrorMessage(error);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the native dynamic loading subsystem on
        /// Windows systems.  Currently, there is nothing to do.
        /// </summary>
        /// <returns>
        /// True if the dynamic loading subsystem was initialized; otherwise,
        /// false.  Currently, this method always returns true.
        /// </returns>
        private static bool WindowsInitializeDynamicLoading()
        {
            //
            // NOTE: Nothing really to do in here for Windows.
            //
            if (!once)
            {
                TraceOps.DebugTrace(String.Format(
                    "WindowsInitializeDynamicLoading: {0}running on Windows",
                    PlatformOps.IsWindowsOperatingSystem() ? String.Empty : "not "),
                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                once = true;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the file name of the specified loaded module on
        /// Windows systems.
        /// </summary>
        /// <param name="module">
        /// The handle of the module whose file name is being queried.
        /// </param>
        /// <param name="fileName">
        /// The buffer that receives the file name of the module.
        /// </param>
        /// <param name="size">
        /// The size, in characters, of the buffer specified by
        /// <paramref name="fileName" />.
        /// </param>
        /// <returns>
        /// The number of characters copied into the buffer, or zero upon
        /// failure.
        /// </returns>
        private static uint WindowsGetModuleFileName(
            IntPtr module,   /* in */
            IntPtr fileName, /* in */
            uint size        /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UNM.GetModuleFileName(module, fileName, size);

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is intended to obtain the file name of the module that
        /// contains the specified address on Windows systems.  There is no
        /// standard implementation of this function on Windows; therefore, it
        /// always returns null.
        /// </summary>
        /// <param name="address">
        /// The address contained within the module whose file name is being
        /// queried.
        /// </param>
        /// <returns>
        /// The file name of the module, or null if it cannot be determined.
        /// Currently, this method always returns null.
        /// </returns>
        private static string WindowsGetModuleFileName(
            IntPtr address /* in */
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Windows.
            //
            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the handle of the already-loaded module with the
        /// specified file name on Windows systems.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the module whose handle is being queried.
        /// </param>
        /// <returns>
        /// The handle of the module, or <see cref="IntPtr.Zero" /> upon failure.
        /// </returns>
        private static IntPtr WindowsGetModuleHandle(
            string fileName /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UNM.GetModuleHandle(fileName);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the directory used to locate native libraries on
        /// Windows systems.
        /// </summary>
        /// <param name="directory">
        /// The directory to be used when locating native libraries.
        /// </param>
        /// <returns>
        /// True if the directory was set; otherwise, false.
        /// </returns>
        private static bool WindowsSetDllDirectory(
            string directory /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UNM.SetDllDirectory(directory);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the native library with the specified file name on
        /// Windows systems.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native library to load.
        /// </param>
        /// <returns>
        /// The handle of the loaded library, or <see cref="IntPtr.Zero" /> upon
        /// failure.
        /// </returns>
        private static IntPtr WindowsLoadLibrary(
            string fileName /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UNM.LoadLibrary(fileName);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unloads the specified native library on Windows systems.
        /// </summary>
        /// <param name="module">
        /// The handle of the native library to unload.
        /// </param>
        /// <returns>
        /// True if the library was unloaded; otherwise, false.
        /// </returns>
        private static bool WindowsFreeLibrary(
            IntPtr module /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UNM.FreeLibrary(module);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the address of the named function exported by the
        /// specified native library on Windows systems.
        /// </summary>
        /// <param name="module">
        /// The handle of the native library that exports the function.
        /// </param>
        /// <param name="name">
        /// The name of the exported function whose address is being queried.
        /// </param>
        /// <returns>
        /// The address of the named function, or <see cref="IntPtr.Zero" /> upon
        /// failure.
        /// </returns>
        private static IntPtr WindowsGetProcAddress(
            IntPtr module, /* in */
            string name    /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UNM.GetProcAddress(module, name);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified debugging message to the debugger on
        /// Windows systems.
        /// </summary>
        /// <param name="message">
        /// The debugging message to emit.
        /// </param>
        /// <param name="priority">
        /// The optional priority to associate with the debugging message.  This
        /// parameter is not used.
        /// </param>
        /// <returns>
        /// True if the message was emitted; otherwise, false.
        /// </returns>
        private static bool WindowsOutputDebugMessage(
            string message,         /* in */
            DebugPriority? priority /* in: NOT USED */
            )
        {
            if (message != null)
            {
                try
                {
                    UNM.OutputDebugString(message);

                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Unix Specific Methods (DO NOT CALL)
#if UNIX
        /// <summary>
        /// This method performs the platform-specific workarounds required for
        /// proper operation on Macintosh (macOS) systems.  Currently, it forces
        /// the one-time initialization of the "libnotify" and "libxpc" native
        /// libraries.
        /// </summary>
        private static void MacintoshPlatformWorkarounds()
        {
            #region Force "libnotify" One-Time Initialization
            bool libNotifyOk = false;

            try
            {
                libNotifyOk = !UNM.notify_is_valid_token(0);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(NativeOps).Name,
                    TracePriority.NativeError);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Force "libxpc" One-Time Initialization
            bool libXpcCreateOk = false;
            bool libXpcReleaseOk = false;
            IntPtr @object = IntPtr.Zero;

            try
            {
                @object = UNM.xpc_date_create_from_current();
                libXpcCreateOk = (@object != IntPtr.Zero);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(NativeOps).Name,
                    TracePriority.NativeError);
            }
            finally
            {
                if (@object != IntPtr.Zero)
                {
                    try
                    {
                        UNM.xpc_release(@object);
                        libXpcReleaseOk = true;
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeOps).Name,
                            TracePriority.NativeError);
                    }
                    finally
                    {
                        @object = IntPtr.Zero;
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            TraceOps.DebugTrace(String.Format(
                "MacintoshPlatformWorkarounds: libNotifyOk = {0}, " +
                "libXpcCreateOk = {1}, libXpcReleaseOk = {2}",
                libNotifyOk, libXpcCreateOk, libXpcReleaseOk),
                typeof(NativeOps).Name, TracePriority.NativeDebug5);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the identifier of the
        /// current thread on Macintosh (macOS) systems.
        /// </summary>
        /// <returns>
        /// The identifier of the current thread, or <see cref="IntPtr.Zero" />
        /// if it cannot be determined.
        /// </returns>
        private static IntPtr MacintoshGetCurrentThreadId()
        {
            try
            {
                ulong tid = 0;

                if (UNM.pthread_threadid_np(
                        SafeNativeMethods.pthread_self(), ref tid) == 0)
                {
                    return new IntPtr(ConversionOps.ToLong(tid));
                }
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the platform-specific workarounds required for
        /// proper operation on Linux systems.  Currently, it does nothing.
        /// </summary>
        private static void LinuxPlatformWorkarounds()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the identifier of the
        /// current thread on Linux systems, using the appropriate system call
        /// number for the detected processor architecture.
        /// </summary>
        /// <returns>
        /// The identifier of the current thread, or <see cref="IntPtr.Zero" />
        /// if it cannot be determined.
        /// </returns>
        private static IntPtr LinuxGetCurrentThreadId()
        {
            int number;

            switch (PlatformOps.GetProcessorArchitecture())
            {
                case ProcessorArchitecture.Intel:
                    number = UNM.SYS_gettid_i386;
                    break;
                case ProcessorArchitecture.ARM:
                    number = UNM.SYS_gettid_ARM;
                    break;
                case ProcessorArchitecture.IA64:
                    number = UNM.SYS_gettid_IA64;
                    break;
                case ProcessorArchitecture.AMD64:
                    number = UNM.SYS_gettid_AMD64;
                    break;
                case ProcessorArchitecture.ARM64:
                    number = UNM.SYS_gettid_ARM64;
                    break;
                default:
                    return IntPtr.Zero;
            }

            try
            {
                return new IntPtr(UNM.syscall_int(
                    number)); /* 2.4.11+ */
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the platform-specific workarounds required for
        /// proper operation on Unix systems, dispatching to the Linux and/or
        /// Macintosh (macOS) specific workarounds as appropriate.
        /// </summary>
        private static void UnixPlatformWorkarounds()
        {
            if (PlatformOps.IsLinuxOperatingSystem())
                LinuxPlatformWorkarounds();

            if (PlatformOps.IsMacintoshOperatingSystem())
                MacintoshPlatformWorkarounds();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the identifier of the
        /// current thread on Unix systems.
        /// </summary>
        /// <returns>
        /// The identifier of the current thread, or <see cref="IntPtr.Zero" />
        /// if it cannot be determined.
        /// </returns>
        private static IntPtr UnixGetCurrentThreadId()
        {
            try
            {
                return SafeNativeMethods.pthread_self();
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the operating system for the identifier of the
        /// parent of the current process on Unix systems.
        /// </summary>
        /// <param name="processId">
        /// The identifier of the process whose parent is being queried.  This
        /// parameter is not used.
        /// </param>
        /// <returns>
        /// The identifier of the parent process, or <see cref="IntPtr.Zero" />
        /// if it cannot be determined.
        /// </returns>
        private static IntPtr UnixGetParentProcessId(
            IntPtr processId /* in: NOT USED */
            )
        {
            try
            {
                return new IntPtr(UNM.getppid());
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is the main thread
        /// of the process on Macintosh (macOS) systems.
        /// </summary>
        /// <returns>
        /// True if the current thread is the main thread; otherwise, false.
        /// </returns>
        private static bool MacintoshIsMainThread()
        {
            try
            {
                return SafeNativeMethods.pthread_main_np() != 0;
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is the main thread
        /// of the process on Linux systems, by comparing the current thread
        /// identifier to the process identifier.
        /// </summary>
        /// <returns>
        /// True if the current thread is the main thread; otherwise, false.
        /// </returns>
        private static bool LinuxIsMainThread()
        {
            try
            {
                IntPtr processId = new IntPtr(ProcessOps.GetId());

                return processId == GetCurrentThreadId();
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is the main thread
        /// of the process on Unix systems.  Currently, it always returns false.
        /// </summary>
        /// <returns>
        /// True if the current thread is the main thread; otherwise, false.
        /// </returns>
        private static bool UnixIsMainThread()
        {
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method raises the specified signal for the current process on
        /// Unix systems.
        /// </summary>
        /// <param name="signal">
        /// The number of the signal to raise.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will receive information about the error
        /// that was encountered.
        /// </param>
        /// <returns>
        /// Zero upon success, or a non-zero value upon failure.
        /// </returns>
        private static int UnixRaiseSignal(
            int signal,      /* in */
            ref Result error /* out */
            )
        {
            try
            {
                return SafeNativeMethods.raise(signal);
            }
            catch (Exception e)
            {
                error = e;
            }

            return -3;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the human-readable message associated with the
        /// specified error number on Unix systems.
        /// </summary>
        /// <param name="error">
        /// The error number whose message is being queried.
        /// </param>
        /// <returns>
        /// The error message string, or null if it cannot be determined.
        /// </returns>
        private static string UnixGetErrorMessage(
            int error /* in */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                handle = UNM.strerror(error);

                if (handle != IntPtr.Zero)
                    return Marshal.PtrToStringAnsi(handle);
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the human-readable message associated with the
        /// most recent dynamic loading error on Unix systems, using the native
        /// <c>dlerror</c> function.
        /// </summary>
        /// <param name="error">
        /// The error number whose message is being queried.  This parameter is
        /// not used.
        /// </param>
        /// <returns>
        /// The error message string, or null if it cannot be determined.
        /// </returns>
        private static string UnixGetDynamicLoadingError(
            int error /* in: NOT USED */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlerror dlerror;

                lock (UNM.syncRoot)
                {
                    dlerror = UNM.dlerror;
                }

                if (dlerror != null)
                {
                    IntPtr handle = dlerror();

                    if (handle != IntPtr.Zero)
                        return Marshal.PtrToStringAnsi(handle);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the native dynamic loading subsystem on Unix
        /// systems, configuring the delegates used to invoke the <c>dlopen</c>,
        /// <c>dlclose</c>, <c>dlsym</c>, <c>dladdr</c>, and <c>dlerror</c>
        /// functions from either "libdl" or "libc", whichever is available.
        /// </summary>
        /// <returns>
        /// True if the dynamic loading subsystem was initialized; otherwise,
        /// false.  Currently, this method always returns true.
        /// </returns>
        private static bool UnixInitializeDynamicLoading()
        {
            lock (UNM.syncRoot)
            {
                if ((UNM.dlopen == null) ||
                    (UNM.dlclose == null) ||
                    (UNM.dlsym == null) ||
                    (UNM.dladdr == null) ||
                    (UNM.dlerror == null))
                {
                    IntPtr module = IntPtr.Zero;

                    try
                    {
                        try
                        {
                            //
                            // HACK: Attempt to determine if we should be using the "libdl"
                            //       version of dlopen or the ones from "libc".
                            //
                            module = UNM.libdl_dlopen(null, UNM.RTLD_DEFMODE); /* throw */
                        }
                        catch
                        {
                            // do nothing.
                        }

                        //
                        // NOTE: Did we manage to get a valid module handle (i.e. we did not
                        //       throw an exception and the function returned something that
                        //       looks valid)?
                        //
                        if (module != IntPtr.Zero)
                        {
                            if (!once)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "UnixInitializeDynamicLoading: {0}running on Unix, " +
                                    "using dlopen from libdl",
                                    PlatformOps.IsUnixOperatingSystem() ? String.Empty : "not "),
                                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                                once = true;
                            }

                            //
                            // NOTE: Setup our delegates to use the "libdl" functions.
                            //
                            if (UNM.dlopen == null)
                                UNM.dlopen = UNM.libdl_dlopen;

                            if (UNM.dlclose == null)
                                UNM.dlclose = UNM.libdl_dlclose;

                            if (UNM.dlsym == null)
                                UNM.dlsym = UNM.libdl_dlsym;

                            if (UNM.dladdr == null)
                                UNM.dladdr = UNM.libdl_dladdr;

                            if (UNM.dlerror == null)
                                UNM.dlerror = UNM.libdl_dlerror;
                        }
                        else
                        {
                            if (!once)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "UnixInitializeDynamicLoading: {0}running on Unix, " +
                                    "using dlopen from __Internal",
                                    PlatformOps.IsUnixOperatingSystem() ? String.Empty : "not "),
                                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                                once = true;
                            }

                            //
                            // NOTE: Using the "libdl" dlopen() did not work.  Either this
                            //       platform does not put the dlopen() function there or it
                            //       does not work at all.  Either way, setup our delegates
                            //       to use the "libc" functions.  If these [later] fail, we
                            //       will assume that dynamic loading is somehow broken on
                            //       this platform (at least from inside Mono).
                            //
                            if (UNM.dlopen == null)
                                UNM.dlopen = UNM.libc_dlopen;

                            if (UNM.dlclose == null)
                                UNM.dlclose = UNM.libc_dlclose;

                            if (UNM.dlsym == null)
                                UNM.dlsym = UNM.libc_dlsym;

                            if (UNM.dladdr == null)
                                UNM.dladdr = UNM.libc_dladdr;

                            if (UNM.dlerror == null)
                                UNM.dlerror = UNM.libc_dlerror;
                        }
                    }
                    finally
                    {
                        //
                        // NOTE: Ok, the test "libdl" dlopen() worked, close
                        //       the module handle we got now.
                        //
                        if (module != IntPtr.Zero)
                        {
                            if (UNM.libdl_dlclose(module) == 0)
                            {
                                module = IntPtr.Zero;
                            }
                            else
                            {
                                //
                                // NOTE: This could be bad.  Report it to the user.
                                //
                                int lastError = Marshal.GetLastWin32Error();

                                DebugOps.Complain(ReturnCode.Error, String.Format(
                                    "libdl_dlclose(0x{1:X}) failed with error {0}",
                                    lastError, module));
                            }
                        }
                    }
                }
            }

            //
            // NOTE: Currently, this method cannot "fail"; however, that does
            //       not necessarily mean that subsequent calls into the Unix
            //       dynamic loading subsystem will actually succeed.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is intended to obtain the file name of the specified
        /// loaded module on Unix systems.  There is no standard implementation
        /// of this function on Unix; therefore, it always returns zero.
        /// </summary>
        /// <param name="module">
        /// The handle of the module whose file name is being queried.
        /// </param>
        /// <param name="fileName">
        /// The buffer that would receive the file name of the module.
        /// </param>
        /// <param name="size">
        /// The size, in characters, of the buffer specified by
        /// <paramref name="fileName" />.
        /// </param>
        /// <returns>
        /// The number of characters copied into the buffer, or zero upon
        /// failure.  Currently, this method always returns zero.
        /// </returns>
        private static uint UnixGetModuleFileName(
            IntPtr module,   /* in */
            IntPtr fileName, /* in */
            uint size        /* in */
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Unix.
            //
            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the file name of the module that contains the
        /// specified address on Unix systems, using the native <c>dladdr</c>
        /// function.
        /// </summary>
        /// <param name="address">
        /// The address contained within the module whose file name is being
        /// queried.
        /// </param>
        /// <returns>
        /// The file name of the module, or null if it cannot be determined.
        /// </returns>
        private static string UnixGetModuleFileName(
            IntPtr address /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dladdr dladdr;

                lock (UNM.syncRoot)
                {
                    dladdr = UNM.dladdr;
                }

                UNM.Dl_info_t info =
                    new UNM.Dl_info_t();

                if ((dladdr != null) &&
                    (dladdr(address, ref info) != 0) &&
                    (info.dli_fname != IntPtr.Zero))
                {
                    return Marshal.PtrToStringAnsi(info.dli_fname);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is intended to obtain the handle of the already-loaded
        /// module with the specified file name on Unix systems.  There is no
        /// standard implementation of this function on Unix; therefore, it
        /// always returns <see cref="IntPtr.Zero" />.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the module whose handle is being queried.
        /// </param>
        /// <returns>
        /// The handle of the module, or <see cref="IntPtr.Zero" /> upon failure.
        /// Currently, this method always returns <see cref="IntPtr.Zero" />.
        /// </returns>
        private static IntPtr UnixGetModuleHandle(
            string fileName /* in */
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Unix.
            //
            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is intended to set the directory used to locate native
        /// libraries on Unix systems.  There is no standard implementation of
        /// this function on Unix; therefore, it always returns false.
        /// </summary>
        /// <param name="directory">
        /// The directory to be used when locating native libraries.
        /// </param>
        /// <returns>
        /// True if the directory was set; otherwise, false.  Currently, this
        /// method always returns false.
        /// </returns>
        private static bool UnixSetDllDirectory(
            string directory /* in */
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Unix.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the native library with the specified file name on
        /// Unix systems, using the native <c>dlopen</c> function.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native library to load.
        /// </param>
        /// <returns>
        /// The handle of the loaded library, or <see cref="IntPtr.Zero" /> upon
        /// failure.
        /// </returns>
        private static IntPtr UnixLoadLibrary(
            string fileName /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlopen dlopen;

                lock (UNM.syncRoot)
                {
                    dlopen = UNM.dlopen;
                }

                if (dlopen != null)
                    return dlopen(fileName, UNM.RTLD_DEFMODE);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unloads the specified native library on Unix systems,
        /// using the native <c>dlclose</c> function.
        /// </summary>
        /// <param name="module">
        /// The handle of the native library to unload.
        /// </param>
        /// <returns>
        /// True if the library was unloaded; otherwise, false.
        /// </returns>
        private static bool UnixFreeLibrary(
            IntPtr module /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlclose dlclose;

                lock (UNM.syncRoot)
                {
                    dlclose = UNM.dlclose;
                }

                if (dlclose != null)
                {
                    //
                    // NOTE: The "dlclose" function is supposed to
                    //       return zero upon success.
                    //
                    return (dlclose(module) == 0);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the address of the named function exported by the
        /// specified native library on Unix systems, using the native
        /// <c>dlsym</c> function.
        /// </summary>
        /// <param name="module">
        /// The handle of the native library that exports the function.
        /// </param>
        /// <param name="name">
        /// The name of the exported function whose address is being queried.
        /// </param>
        /// <returns>
        /// The address of the named function, or <see cref="IntPtr.Zero" /> upon
        /// failure.
        /// </returns>
        private static IntPtr UnixGetProcAddress(
            IntPtr module, /* in */
            string name    /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlsym dlsym;

                lock (UNM.syncRoot)
                {
                    dlsym = UNM.dlsym;
                }

                if (dlsym != null)
                    return dlsym(module, name);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified debugging message to the system log
        /// on Macintosh (macOS) systems, escaping any printf-style formatting
        /// in the message beforehand.
        /// </summary>
        /// <param name="message">
        /// The debugging message to emit.
        /// </param>
        /// <param name="priority">
        /// The optional priority to associate with the debugging message.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the message was emitted; otherwise, false.
        /// </returns>
        private static bool MacintoshOutputDebugMessage(
            string message,         /* in */
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
            EscapePrintfStyleFormatting(ref message);

            if (message != null)
            {
                try
                {
                    UNM.bare_syslog(
                        GetOutputDebugMessagePriority(priority),
                        message);

                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified debugging message to the system log
        /// on Unix systems.
        /// </summary>
        /// <param name="message">
        /// The debugging message to emit.
        /// </param>
        /// <param name="priority">
        /// The optional priority to associate with the debugging message.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the message was emitted; otherwise, false.
        /// </returns>
        private static bool UnixOutputDebugMessage(
            string message,         /* in */
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
            if (message != null)
            {
                try
                {
                    UNM.string_syslog(
                        GetOutputDebugMessagePriority(priority),
                        FormatOps.StringInputFormat, message);

                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Bolt Helper Library
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The name of the native function exported by the optional Bolt helper
        /// library that formats a double-precision floating point value.
        /// </summary>
        private static string boltFunctionName = "bolt_snprintf_double";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// This object is used to synchronize access to the optional Bolt
        /// helper library delegate.
        /// </summary>
        private static readonly object boltSyncRoot = new object();

        /// <summary>
        /// Non-zero if an attempt has already been made to load the optional
        /// Bolt helper library and resolve its delegate.
        /// </summary>
        private static bool boltAttempted;

        /// <summary>
        /// The cached delegate for the native function exported by the optional
        /// Bolt helper library, or null if it could not be loaded.
        /// </summary>
        private static UNM.bolt_snprintf_double boltDelegate;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // NOTE: Build the candidate file names for the optional Bolt helper
        //       library, using the shared-library prefix ("lib") and extension;
        //       the directory containing this assembly is tried first, then the
        //       bare name (relying on the operating system loader search path).
        //
        /// <summary>
        /// This method builds the list of candidate file names for the optional
        /// Bolt helper library, trying the directory containing this assembly
        /// first and then the bare name.
        /// </summary>
        /// <returns>
        /// The list of candidate file names for the optional Bolt helper
        /// library.
        /// </returns>
        private static StringList GetBoltFileNames()
        {
            StringList result = new StringList();

            string fileNameOnly = String.Format(
                "{0}{1}{2}", PlatformOps.IsUnixOperatingSystem() ?
                    TclVars.Path.Lib : String.Empty,
                DllName.Bolt, RuntimeOps.GetSharedLibraryExtension());

            try
            {
                string location = GlobalState.GetAssemblyLocation();

                if (!String.IsNullOrEmpty(location))
                {
                    string directory = Path.GetDirectoryName(location);

                    if (!String.IsNullOrEmpty(directory))
                        result.Add(Path.Combine(directory, fileNameOnly));
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetBoltFileNames: {0}", e),
                    typeof(NativeOps).Name, TracePriority.NativeError2);
            }

            result.Add(fileNameOnly);
            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads the optional Bolt helper library, when necessary,
        /// and resolves the delegate for its native double formatting function.
        /// The load is attempted at most once for the lifetime of the process.
        /// </summary>
        /// <returns>
        /// The resolved delegate for the native function exported by the
        /// optional Bolt helper library, or null if it could not be loaded.
        /// </returns>
        private static UNM.bolt_snprintf_double GetBoltDelegate()
        {
            lock (boltSyncRoot) /* TRANSACTIONAL */
            {
                if (boltAttempted)
                    return boltDelegate;

                boltAttempted = true;

                Type delegateType = typeof(UNM.bolt_snprintf_double);

                try
                {
                    foreach (string fileName in GetBoltFileNames())
                    {
                        if (String.IsNullOrEmpty(fileName))
                            continue;

                        int lastError; /* NOT USED */

                        IntPtr module = LoadLibrary(fileName, out lastError);

                        if (module == IntPtr.Zero)
                            continue;

                        IntPtr address = GetProcAddress(
                            module, boltFunctionName, out lastError);

                        if (address == IntPtr.Zero)
                        {
                            if (FreeLibrary(module, out lastError)) /* throw */
                            {
                                module = IntPtr.Zero;
                            }
                            else
                            {
                                DebugOps.Complain(ReturnCode.Error, String.Format(
                                    "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                                    lastError, module, GetDynamicLoadingError(
                                    lastError)));
                            }

                            continue;
                        }

                        //
                        // NOTE: Intentionally leave the module loaded for the
                        //       lifetime of the process; the delegate refers to
                        //       it.
                        //
                        boltDelegate = (UNM.bolt_snprintf_double)
                            Marshal.GetDelegateForFunctionPointer(
                                address, delegateType); /* throw */

                        break;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetBoltDelegate: {0}", e), typeof(NativeOps).Name,
                        TracePriority.NativeError2);
                }

                return boltDelegate;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is this a platform where the fixed-signature P/Invoke to
        //       the variadic snprintf() does not work (e.g. arm64 macOS)?
        //
        /// <summary>
        /// This method determines whether the current platform is one where the
        /// fixed-signature P/Invoke to the variadic native snprintf() does not
        /// work (for example, arm64 macOS).
        /// </summary>
        /// <returns>
        /// True if the fixed-signature P/Invoke to the variadic native
        /// snprintf() does not work on the current platform; otherwise, false.
        /// </returns>
        private static bool IsProblematicPlatformForPrintf()
        {
            if (!PlatformOps.IsIntelProcessorArchitecture())
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the native printf-style formatting
        /// should be used when converting a double-precision floating point
        /// value to its string representation.
        /// </summary>
        /// <returns>
        /// True if native printf-style formatting should be used for a
        /// double-precision floating point value; otherwise, false.
        /// </returns>
        public static bool ShouldUsePrintfForDouble()
        {
            if (IsProblematicPlatformForPrintf())
                return GetBoltDelegate() != null;

            return true;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Mono Specific Methods (DO NOT CALL)
#if MONO || MONO_HACKS
        /// <summary>
        /// This method formats a double-precision floating point value into the
        /// specified buffer using the native snprintf() function, via a path
        /// compatible with the Mono runtime.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that receives the formatted value.
        /// </param>
        /// <param name="format">
        /// The native printf-style format string used to format the value.
        /// </param>
        /// <param name="value">
        /// The double-precision floating point value to format.
        /// </param>
        /// <param name="returnValue">
        /// The running total of characters produced by the native formatting
        /// function.  Upon success, the count returned by this call is added to
        /// this value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode MonoPrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
            try
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UNM.msvc_snprintf_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, value);

                    return ReturnCode.Ok;
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UNM.ansi_snprintf_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, value);

                    return ReturnCode.Ok;
                }
#endif

                error = "unknown operating system";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// This method queries the system memory status on the Mono runtime
        /// using performance counters.
        /// </summary>
        /// <param name="memoryLoad">
        /// Upon success, this parameter will be modified to contain the
        /// approximate percentage of physical memory that is in use.
        /// </param>
        /// <param name="totalPhysical">
        /// Upon success, this parameter will be modified to contain the total
        /// amount of physical memory, in bytes.
        /// </param>
        /// <param name="availablePhysical">
        /// Upon success, this parameter will be modified to contain the amount
        /// of available physical memory, in bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the memory status was queried successfully; otherwise,
        /// false.
        /// </returns>
        private static bool MonoGetMemoryStatus(
            ref uint memoryLoad,         /* out */
            ref ulong totalPhysical,     /* out */
            ref ulong availablePhysical, /* out */
            ref Result error             /* out */
            )
        {
            if (CommonOps.Runtime.IsMono())
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    try
                    {
                        if (totalPhysicalMemoryCounter == null)
                        {
                            totalPhysicalMemoryCounter = new PerformanceCounter(
                                MonoMemoryCategoryName,
                                MonoTotalPhysicalMemoryCounterName);
                        }

                        if (availablePhysicalMemoryCounter == null)
                        {
                            availablePhysicalMemoryCounter = new PerformanceCounter(
                                MonoMemoryCategoryName,
                                MonoAvailablePhysicalMemoryCounterName);
                        }

                        totalPhysical = ConversionOps.ToULong(
                            totalPhysicalMemoryCounter.RawValue);

                        availablePhysical = ConversionOps.ToULong(
                            availablePhysicalMemoryCounter.RawValue);

                        ulong usedPhysical = totalPhysical - availablePhysical;

                        double percent = (totalPhysical != 0) ?
                            ((double)usedPhysical /
                                (double)totalPhysical) * 100 : 0;

                        if (percent < 0.0)
                            percent = 0.0;
                        else if (percent > 100.0)
                            percent = 100.0;

                        memoryLoad = (uint)percent;
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }
            else
            {
                error = "not supported on this platform";
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private .NET Core Specific Methods (DO NOT CALL)
        /// <summary>
        /// This method formats a double-precision floating point value into the
        /// specified buffer using the native snprintf() function, via a path
        /// compatible with the .NET Core runtime.  On platforms where the
        /// fixed-signature P/Invoke does not work, the optional Bolt helper
        /// library is used when available.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that receives the formatted value.
        /// </param>
        /// <param name="format">
        /// The native printf-style format string used to format the value.
        /// </param>
        /// <param name="value">
        /// The double-precision floating point value to format.
        /// </param>
        /// <param name="returnValue">
        /// The running total of characters produced by the native formatting
        /// function.  Upon success, the count returned by this call is added to
        /// this value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode DotNetCorePrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
            try
            {
                if (IsProblematicPlatformForPrintf())
                {
                    //
                    // BUGFIX: On platforms where the fixed-signature P/Invoke to
                    //         the variadic snprintf() does not work (e.g. macOS
                    //         on arm64), attempt to use the native wrapper from
                    //         the optional Bolt helper library, when available;
                    //         otherwise, fall back to the direct P/Invoke (which
                    //         is correct on the platforms where it works).
                    //
                    UNM.bolt_snprintf_double bolt = GetBoltDelegate();

                    if (bolt != null)
                    {
                        returnValue += bolt(buffer,
                            new UIntPtr((uint)buffer.Capacity), format, value);

                        return ReturnCode.Ok;
                    }
                }

#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // NOTE: The .NET Core runtime does not appear to support
                    //       the "varargs" calling convention, as of the .NET
                    //       Standard 2.0 (i.e. we cannot use "__arglist").
                    //
                    returnValue += UNM.msvc_snprintf_s_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), UNM._TRUNCATE,
                        format, value);

                    return ReturnCode.Ok;
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    //
                    // NOTE: The .NET Core runtime does not appear to support
                    //       the "varargs" calling convention, as of the .NET
                    //       Standard 2.0 (i.e. we cannot use "__arglist").
                    //
                    returnValue += UNM.ansi_snprintf_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, value);

                    return ReturnCode.Ok;
                }
#endif

                error = "unknown operating system";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Platform Abstraction Methods (DO NOT CALL)
        /// <summary>
        /// This method initializes the native dynamic loading subsystem for the
        /// current operating system.
        /// </summary>
        /// <returns>
        /// True if the dynamic loading subsystem was initialized successfully;
        /// otherwise, false.
        /// </returns>
        private static bool InitializeDynamicLoading()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsInitializeDynamicLoading();
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixInitializeDynamicLoading();
#endif

            //
            // NOTE: If we get to this point, we are running on neither
            //       Windows nor Unix; this is currently considered a
            //       failure.
            //
            if (!once)
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeDynamicLoading: running on unknown operating " +
                    "system {0} with Id {1}",
                    FormatOps.WrapOrNull(PlatformOps.GetOperatingSystemName()),
                    PlatformOps.GetOperatingSystemId()),
                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                once = true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a double-precision floating-point value into the
        /// specified buffer using the native printf-style implementation for the
        /// current operating system.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that receives the formatted output.
        /// </param>
        /// <param name="format">
        /// The printf-style format string used to format the value.
        /// </param>
        /// <param name="value">
        /// The double-precision floating-point value to format.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this value is incremented by the number of characters
        /// written by the native formatting function.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code is returned.
        /// </returns>
        private static ReturnCode NormalPrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
            try
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UNM.msvc_snprintf(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, __arglist(value));

                    return ReturnCode.Ok;
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UNM.ansi_snprintf(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, __arglist(value));

                    return ReturnCode.Ok;
                }
#endif

                error = "unknown operating system";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the total amount of physical memory installed on
        /// the current operating system.
        /// </summary>
        /// <param name="totalPhysical">
        /// Upon success, this parameter will be modified to contain the total
        /// amount of physical memory, in bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code is returned.
        /// </returns>
        private static ReturnCode GetTotalMemory(
            ref ulong totalPhysical, /* out */
            ref Result error         /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UNM.MEMORYSTATUSEX memoryStatus;

                WindowsInitializeMemoryStatus(out memoryStatus);

                if (WindowsGetMemoryStatus(ref memoryStatus))
                {
                    totalPhysical = memoryStatus.ullTotalPhys;
                    return ReturnCode.Ok;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GlobalMemoryStatusEx() failed with error {0}: {1}",
                        lastError, GetErrorMessage(lastError));

                    return ReturnCode.Error;
                }
            }
#endif

#if !NET_STANDARD_20
            if (CommonOps.Runtime.IsMono())
            {
                uint memoryLoad = 0;
                ulong availablePhysical = 0;

                if (MonoGetMemoryStatus(
                        ref memoryLoad, ref totalPhysical,
                        ref availablePhysical, ref error))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
#endif

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the approximate percentage of physical memory
        /// that is currently in use on the current operating system.
        /// </summary>
        /// <param name="memoryLoad">
        /// Upon success, this parameter will be modified to contain the
        /// approximate percentage of physical memory that is in use.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code is returned.
        /// </returns>
        private static ReturnCode GetMemoryLoad(
            ref uint memoryLoad, /* out */
            ref Result error     /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UNM.MEMORYSTATUSEX memoryStatus;

                WindowsInitializeMemoryStatus(out memoryStatus);

                if (WindowsGetMemoryStatus(ref memoryStatus))
                {
                    memoryLoad = memoryStatus.dwMemoryLoad;
                    return ReturnCode.Ok;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GlobalMemoryStatusEx() failed with error {0}: {1}",
                        lastError, GetErrorMessage(lastError));

                    return ReturnCode.Error;
                }
            }
#endif

#if !NET_STANDARD_20
            if (CommonOps.Runtime.IsMono())
            {
                ulong totalPhysical = 0;
                ulong availablePhysical = 0;

                if (MonoGetMemoryStatus(
                        ref memoryLoad, ref totalPhysical,
                        ref availablePhysical, ref error))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
#endif

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method escapes any printf-style formatting specifiers contained
        /// in the specified message by doubling each percent sign.
        /// </summary>
        /// <param name="message">
        /// Upon input, this parameter contains the message to be escaped.  Upon
        /// output, this parameter contains the escaped message.
        /// </param>
        /// <returns>
        /// The number of characters added to the message as a result of the
        /// escaping, or a negative value if the message is null or empty.
        /// </returns>
        private static int EscapePrintfStyleFormatting(
            ref string message /* in, out */
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(message, out length))
                return Count.Invalid;

            message = message.Replace(
                Characters.SinglePercentSignString,
                Characters.DoublePercentSignString);

            return (message.Length - length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the mapping of debug priority sources to
        /// their corresponding debug priority levels.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly reinitialize the mapping even if it has already
        /// been initialized.
        /// </param>
        private static void InitializeDebugPriorities(
            bool force /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (force || (debugPriorities == null))
                {
                    if (debugPriorities != null)
                        debugPriorities.Clear();
                    else
                        debugPriorities = new DebugPriorityDictionary();

                    DebugPriority[] priorities = {
                        DebugPriority.FromFailSafe,
                            DebugPriority.Alert,
                        DebugPriority.FromSelf,
                            DebugPriority.Critical,
                        DebugPriority.FromTraceException,
                            DebugPriority.Error,
                        DebugPriority.FromTraceMessage,
                            DebugPriority.Warning,
                        DebugPriority.FromTest,
                            DebugPriority.Notice,
                        DebugPriority.FromExternal,
                            DebugPriority.Information
                    };

                    int length = priorities.Length;

                    for (int index = 0; index < length; index += 2)
                    {
                        debugPriorities.Add(
                            priorities[index], priorities[index + 1]);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified debug priority into its base level
        /// component and its source component.
        /// </summary>
        /// <param name="priority">
        /// The debug priority to split.  This parameter may be null.
        /// </param>
        /// <param name="fromPriority">
        /// Upon output, this parameter will be modified to contain the base
        /// level component of the specified debug priority.
        /// </param>
        /// <param name="viaPriority">
        /// Upon output, this parameter will be modified to contain the source
        /// component of the specified debug priority.
        /// </param>
        private static void SplitDebugPriority(
            DebugPriority? priority,        /* in: OPTIONAL */
            out DebugPriority fromPriority, /* in */
            out DebugPriority viaPriority   /* in */
            )
        {
            if (priority != null)
            {
                //
                // HACK: Mask off our "custom" bits (i.e. which
                //       indicate the source of the debug output
                //       message).  This will allow the priority
                //       range checking to work correctly.
                //
                DebugPriority localPriority = (DebugPriority)priority;

                fromPriority = localPriority & ~DebugPriority.NonBaseMask;
                viaPriority = localPriority & DebugPriority.NonBaseMask;
            }
            else
            {
                //
                // NOTE: There is debug priority value, set both
                //       of the output parameters to nothing.
                //
                fromPriority = DebugPriority.None;
                viaPriority = DebugPriority.None;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to translate the specified debug priority source
        /// into its corresponding debug priority level.
        /// </summary>
        /// <param name="fromPriority">
        /// The debug priority source to translate.
        /// </param>
        /// <param name="toPriority">
        /// Upon success, this parameter will be modified to contain the
        /// translated debug priority level.  Upon failure, this parameter will
        /// be modified to contain the default debug priority.
        /// </param>
        /// <returns>
        /// True if the specified debug priority source was translated
        /// successfully; otherwise, false.
        /// </returns>
        private static bool TryTranslateDebugPriority(
            DebugPriority fromPriority,  /* in */
            out DebugPriority toPriority /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeDebugPriorities(false);

                if ((debugPriorities != null) &&
                    debugPriorities.TryGetValue(
                        fromPriority, out toPriority))
                {
                    return true;
                }

                toPriority = defaultDebugPriority;
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified debug priority falls
        /// within the valid range of debug priority levels.
        /// </summary>
        /// <param name="priority">
        /// The debug priority to check.
        /// </param>
        /// <returns>
        /// True if the specified debug priority is valid; otherwise, false.
        /// </returns>
        private static bool IsValidDebugPriority(
            DebugPriority priority /* in */
            )
        {
            //
            // HACK: *SPECIAL* The DebugPriority enumeration is
            //       based on POSIX values and those include a
            //       value that is zero.  Hence, standard flags
            //       handling will not quite work here.
            //
            if ((priority >= DebugPriority.Minimum) &&
                (priority <= DebugPriority.Maximum))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified debug priority into a specific
        /// debug priority level, falling back to the default debug priority when
        /// no specific level can be determined.
        /// </summary>
        /// <param name="priority">
        /// The debug priority to translate.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The translated debug priority level, or the default debug priority
        /// when no specific level can be determined.
        /// </returns>
        private static DebugPriority TranslateDebugPriority(
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (priority != null)
                {
                    //
                    // NOTE: STEP #1, Split priority into a specific
                    //       level, if any, and its source, if any.
                    //
                    DebugPriority fromPriority;
                    DebugPriority viaPriority;

                    SplitDebugPriority(
                        priority, out fromPriority, out viaPriority);

                    //
                    // NOTE: STEP #2, If the specific level is found,
                    //       just use it.  For this case, the source
                    //       of the debug message is ignored.
                    //
                    if (IsValidDebugPriority(fromPriority))
                        return fromPriority;

                    //
                    // NOTE: STEP #3, Otherwise, see if the source is
                    //       mapped to a specific level.  If so, just
                    //       use it.
                    //
                    DebugPriority toPriority;

                    if (TryTranslateDebugPriority(
                            viaPriority, out toPriority) &&
                        IsValidDebugPriority(toPriority))
                    {
                        return toPriority;
                    }
                }

                //
                // NOTE: STEP #4, Just use the default debug priority.
                //
                return defaultDebugPriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified debug priority into the raw
        /// POSIX priority value suitable for use when emitting an output debug
        /// message.
        /// </summary>
        /// <param name="priority">
        /// The debug priority to translate.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The raw POSIX priority value corresponding to the specified debug
        /// priority.
        /// </returns>
        private static int GetOutputDebugMessagePriority(
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
            //
            // HACK: Adjust the translated DebugPriority level by
            //       subtracting the offset that was added to all
            //       the contained "raw" POSIX values.  This was
            //       needed in order to reserve zero as a special
            //       indicator of "nothing has been specified".
            //
            return (int)TranslateDebugPriority(
                priority) - (int)DebugPriority.Offset;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        /// <summary>
        /// This method applies any platform-specific workarounds that are
        /// necessary for the current operating system.
        /// </summary>
        public static void PlatformWorkarounds()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                WindowsPlatformWorkarounds();
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                UnixPlatformWorkarounds();
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the sentinel value used to represent an invalid
        /// garbage collector handle.
        /// </summary>
        /// <returns>
        /// The invalid garbage collector handle value.
        /// </returns>
        public static GCHandle GetInvalidGCHandle()
        {
            return invalidGCHandle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a double-precision floating-point value into the
        /// specified buffer using the specified format string.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that the formatted value will be appended to.
        /// </param>
        /// <param name="format">
        /// The format string used to format the value.
        /// </param>
        /// <param name="value">
        /// The double-precision floating-point value to format.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode PrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref Result error      /* out */
            )
        {
            int returnValue = 0;

            return PrintDouble(
                buffer, format, value, ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a double-precision floating-point value into the
        /// specified buffer using the specified format string, dispatching to
        /// the appropriate runtime-specific implementation.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that the formatted value will be appended to.
        /// </param>
        /// <param name="format">
        /// The format string used to format the value.
        /// </param>
        /// <param name="value">
        /// The double-precision floating-point value to format.
        /// </param>
        /// <param name="returnValue">
        /// Upon input, the value to start with; upon output, this parameter will
        /// be modified to contain the result returned by the underlying
        /// formatting routine.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode PrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
            if (CommonOps.Runtime.IsDotNetCore())
            {
                return DotNetCorePrintDouble(
                    buffer, format, value, ref returnValue, ref error);
            }

#if MONO || MONO_HACKS
            //
            // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
            //       does not support using the C# "__arglist" keyword.
            //       https://bugzilla.novell.com/show_bug.cgi?id=472845
            //
            if (CommonOps.Runtime.IsMono())
            {
                return MonoPrintDouble(
                    buffer, format, value, ref returnValue, ref error);
            }
#endif

            return NormalPrintDouble(
                buffer, format, value, ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified native handle is valid.
        /// </summary>
        /// <param name="handle">
        /// The native handle to check.
        /// </param>
        /// <returns>
        /// True if the handle is valid; otherwise, false.
        /// </returns>
        public static bool IsValidHandle(
            IntPtr handle /* in */
            )
        {
            return RuntimeOps.IsValidHandle(handle);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified native handle is valid,
        /// also indicating whether it is the well-known invalid handle value.
        /// </summary>
        /// <param name="handle">
        /// The native handle to check.
        /// </param>
        /// <param name="invalid">
        /// Upon return, this parameter will be modified to non-zero if the
        /// handle is the well-known invalid handle value.
        /// </param>
        /// <returns>
        /// True if the handle is valid; otherwise, false.
        /// </returns>
        public static bool IsValidHandle(
            IntPtr handle,   /* in */
            ref bool invalid /* out */
            )
        {
            return RuntimeOps.IsValidHandle(handle, ref invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to cancel any pending synchronous I/O
        /// operations for the specified thread.
        /// </summary>
        /// <param name="thread">
        /// The native handle of the thread whose synchronous I/O operations are
        /// to be canceled.
        /// </param>
        /// <returns>
        /// True if the operation was successful; otherwise, false.
        /// </returns>
        public static bool CancelSynchronousIo(
            IntPtr thread /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem() &&
                PlatformOps.IsWindowsVistaOrHigher())
            {
                return WindowsCancelSynchronousIo(thread);
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native handle of the current thread.
        /// </summary>
        /// <returns>
        /// The native handle of the current thread, or
        /// <see cref="IntPtr.Zero" /> if it cannot be determined.
        /// </returns>
        public static IntPtr GetCurrentThread()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetCurrentThread();
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native identifier of the current thread.
        /// </summary>
        /// <returns>
        /// The native identifier of the current thread, or
        /// <see cref="IntPtr.Zero" /> if it cannot be determined.
        /// </returns>
        public static IntPtr GetCurrentThreadId()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetCurrentThreadId();
#endif

#if UNIX
            if (PlatformOps.IsMacintoshOperatingSystem())
                return MacintoshGetCurrentThreadId();

            if (PlatformOps.IsLinuxOperatingSystem())
                return LinuxGetCurrentThreadId();

            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetCurrentThreadId();
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native identifier of the grandparent process of
        /// the current process.
        /// </summary>
        /// <returns>
        /// The native identifier of the grandparent process, or
        /// <see cref="IntPtr.Zero" /> if it cannot be determined.
        /// </returns>
        public static IntPtr GetGrandparentProcessId()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                return WindowsGetParentProcessId(
                    WindowsGetParentProcessId(IntPtr.Zero));
            }
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return IntPtr.Zero;
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native identifier of the parent process of the
        /// current process.
        /// </summary>
        /// <returns>
        /// The native identifier of the parent process, or
        /// <see cref="IntPtr.Zero" /> if it cannot be determined.
        /// </returns>
        public static IntPtr GetParentProcessId()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetParentProcessId(IntPtr.Zero);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetParentProcessId(IntPtr.Zero);
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the integrity level of the specified process.
        /// </summary>
        /// <param name="processId">
        /// The native identifier of the process to query.
        /// </param>
        /// <param name="level">
        /// Upon success, this parameter will be modified to contain the
        /// integrity level of the process.
        /// </param>
        /// <returns>
        /// True if the integrity level was successfully obtained; otherwise,
        /// false.
        /// </returns>
        public static bool GetIntegrityLevel(
            IntPtr processId, /* in */
            ref string level  /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetIntegrityLevel(processId, ref level);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the calling thread is the main thread
        /// of the current process.
        /// </summary>
        /// <returns>
        /// True if the calling thread is the main thread; otherwise, false.
        /// </returns>
        public static bool IsMainThread()
        {
#if WINDOWS
            //
            // HACK: There is no 100% reliable way to detect
            //       this on Win32; however, we can fake it.
            //       This is not technically accurate; also,
            //       it does not really matter on Win32.
            //
            if (PlatformOps.IsWindowsOperatingSystem())
                return GlobalState.IsPrimaryThread();
#endif

#if UNIX
            if (PlatformOps.IsMacintoshOperatingSystem())
                return MacintoshIsMainThread();

            if (PlatformOps.IsLinuxOperatingSystem())
                return LinuxIsMainThread();

            if (PlatformOps.IsUnixOperatingSystem())
                return UnixIsMainThread();
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method tests whether the specified native library file can be
        /// dynamically loaded, complaining via the diagnostic trace facility if
        /// it cannot.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native library to test.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the library was successfully loaded;
        /// otherwise, an appropriate error code.
        /// </returns>
        public static ReturnCode TestLoadLibrary(
            string fileName /* in */
            )
        {
            ReturnCode code;
            Result error = null;

            code = TestLoadLibrary(fileName, ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "TestLoadLibrary: fileName = {0}, error = {1}",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(error)),
                    typeof(NativeOps).Name,
                    TracePriority.NativeError3);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method tests whether the specified native library file can be
        /// dynamically loaded, freeing it again if it was successfully loaded.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native library to test.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the library was successfully loaded;
        /// otherwise, an appropriate error code.
        /// </returns>
        public static ReturnCode TestLoadLibrary(
            string fileName, /* in */
            ref Result error /* out */
            )
        {
            IntPtr module = IntPtr.Zero;

            try
            {
                //
                // NOTE: Attempt to dynamically load the module.  This module
                //       handle will be cleaned up in the finally block, if
                //       necessary (i.e. if it was successfully opened).
                //
                int lastError;

                module = LoadLibrary(fileName, out lastError); /* throw */

                if (IsValidHandle(module))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "LoadLibrary({1}) failed with error {0}: {2}",
                        lastError, FormatOps.WrapOrNull(fileName),
                        GetDynamicLoadingError(lastError));
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                try
                {
                    if (IsValidHandle(module))
                    {
                        int lastError;

                        if (FreeLibrary(module, out lastError)) /* throw */
                        {
                            module = IntPtr.Zero;
                        }
                        else
                        {
                            DebugOps.Complain(ReturnCode.Error, String.Format(
                                "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                                lastError, module, GetDynamicLoadingError(
                                lastError)));
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, String.Format(
                        "FreeLibrary(0x{1:X}) failed with exception: {0}",
                        e, module));
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the error message associated with the last native
        /// error for the calling thread, if any.
        /// </summary>
        /// <returns>
        /// The error message associated with the last native error, or null if
        /// there is no last error.
        /// </returns>
        public static string MaybeGetErrorMessage()
        {
            int lastError = Marshal.GetLastWin32Error();

            if (lastError == 0)
                return null;

            return GetErrorMessage(lastError);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the error message associated with the last native
        /// error for the calling thread.
        /// </summary>
        /// <returns>
        /// The error message associated with the last native error.
        /// </returns>
        public static string GetErrorMessage()
        {
            return GetErrorMessage(Marshal.GetLastWin32Error());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the error message associated with the specified
        /// native error code.
        /// </summary>
        /// <param name="error">
        /// The native error code to translate into a message.
        /// </param>
        /// <returns>
        /// The error message associated with the specified error code, or null
        /// if it cannot be determined.
        /// </returns>
        public static string GetErrorMessage(
            int error /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetErrorMessage(error);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetErrorMessage(error);
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the error message associated with the specified
        /// native error code in the context of dynamic library loading.
        /// </summary>
        /// <param name="error">
        /// The native error code to translate into a message.
        /// </param>
        /// <returns>
        /// The dynamic loading error message associated with the specified
        /// error code, or null if it cannot be determined.
        /// </returns>
        public static string GetDynamicLoadingError(
            int error /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetDynamicLoadingError(error);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetDynamicLoadingError(error);
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the file name of the specified loaded module into a
        /// caller-supplied native buffer.
        /// </summary>
        /// <param name="module">
        /// The native handle of the module to query.
        /// </param>
        /// <param name="fileName">
        /// A native buffer that will receive the file name of the module.
        /// </param>
        /// <param name="size">
        /// The size, in characters, of the buffer pointed to by
        /// <paramref name="fileName" />.
        /// </param>
        /// <returns>
        /// The number of characters copied into the buffer, or zero on failure.
        /// </returns>
        public static uint GetModuleFileName(
            IntPtr module,   /* in */
            IntPtr fileName, /* in */
            uint size        /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetModuleFileName(module, fileName, size);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetModuleFileName(module, fileName, size);
#endif

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the file name of the module that contains the
        /// specified native address.
        /// </summary>
        /// <param name="address">
        /// The native address contained within the module to query.
        /// </param>
        /// <returns>
        /// The file name of the module containing the specified address, or null
        /// if it cannot be determined.
        /// </returns>
        public static string GetModuleFileName(
            IntPtr address /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetModuleFileName(address);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetModuleFileName(address);
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native handle of the already-loaded module with
        /// the specified file name.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the module to query.
        /// </param>
        /// <returns>
        /// The native handle of the module, or <see cref="IntPtr.Zero" /> if it
        /// cannot be determined.
        /// </returns>
        public static IntPtr GetModuleHandle(
            string fileName /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetModuleHandle(fileName);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetModuleHandle(fileName);
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the directory used to locate native libraries that
        /// are subsequently loaded.
        /// </summary>
        /// <param name="directory">
        /// The directory to use when searching for native libraries.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, this parameter will be modified to contain the native
        /// error code; otherwise, it will be set to zero.
        /// </param>
        /// <returns>
        /// True if the operation was successful; otherwise, false.
        /// </returns>
        public static bool SetDllDirectory(
            string directory, /* in */
            out int lastError /* out */
            )
        {
            lastError = 0;

            bool result = false;

#if WINDOWS
            if (!result && PlatformOps.IsWindowsOperatingSystem())
                result = WindowsSetDllDirectory(directory);
#endif

#if UNIX
            if (!result && PlatformOps.IsUnixOperatingSystem())
                result = UnixSetDllDirectory(directory);
#endif

            if (!result)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "SetDllDirectory: directory = {0}, result = {1}, lastError = {2}",
                FormatOps.WrapOrNull(directory), result, lastError),
                typeof(NativeOps).Name, TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method dynamically loads the native library with the specified
        /// file name.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the native library to load.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, this parameter will be modified to contain the native
        /// error code; otherwise, it will be set to zero.
        /// </param>
        /// <returns>
        /// The native handle of the loaded library, or
        /// <see cref="IntPtr.Zero" /> on failure.
        /// </returns>
        public static IntPtr LoadLibrary(
            string fileName,  /* in */
            out int lastError /* out */
            )
        {
            lastError = 0;

            IntPtr result = IntPtr.Zero;

#if WINDOWS
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                result = WindowsLoadLibrary(fileName);
            }
#endif

#if UNIX
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsUnixOperatingSystem())
            {
                result = UnixLoadLibrary(fileName);
            }
#endif

            if (result == IntPtr.Zero)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "LoadLibrary: fileName = {0}, result = {1}, lastError = {2}",
                FormatOps.WrapOrNull(fileName), result, lastError),
                typeof(NativeOps).Name, TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unloads the previously loaded native library identified
        /// by the specified module handle.
        /// </summary>
        /// <param name="module">
        /// The native handle of the library to unload.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, this parameter will be modified to contain the native
        /// error code; otherwise, it will be set to zero.
        /// </param>
        /// <returns>
        /// True if the operation was successful; otherwise, false.
        /// </returns>
        public static bool FreeLibrary(
            IntPtr module,    /* in */
            out int lastError /* out */
            )
        {
            lastError = 0;

            bool result = false;

#if WINDOWS
            if (!result && PlatformOps.IsWindowsOperatingSystem())
                result = WindowsFreeLibrary(module);
#endif

#if UNIX
            if (!result && PlatformOps.IsUnixOperatingSystem())
                result = UnixFreeLibrary(module);
#endif

            if (!result)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "FreeLibrary: module = {0}, result = {1}, lastError = {2}",
                module, result, lastError), typeof(NativeOps).Name,
                TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native address of the exported function or
        /// variable with the specified name from the specified module.
        /// </summary>
        /// <param name="module">
        /// The native handle of the module to query.
        /// </param>
        /// <param name="name">
        /// The name of the exported function or variable to locate.
        /// </param>
        /// <param name="lastError">
        /// Upon failure, this parameter will be modified to contain the native
        /// error code; otherwise, it will be set to zero.
        /// </param>
        /// <returns>
        /// The native address of the exported function or variable, or
        /// <see cref="IntPtr.Zero" /> on failure.
        /// </returns>
        public static IntPtr GetProcAddress(
            IntPtr module,    /* in */
            string name,      /* in */
            out int lastError /* out */
            )
        {
            lastError = 0;

            IntPtr result = IntPtr.Zero;

#if WINDOWS
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                result = WindowsGetProcAddress(module, name);
            }
#endif

#if UNIX
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsUnixOperatingSystem())
            {
                result = UnixGetProcAddress(module, name);
            }
#endif

            if (result == IntPtr.Zero)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "GetProcAddress: module = {0}, name = {1}, " +
                "result = {2}, lastError = {3}",
                module, FormatOps.WrapOrNull(name), result, lastError),
                typeof(NativeOps).Name, (result != IntPtr.Zero) ?
                    TracePriority.NativeDebug2 :
                    TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits the specified message to the platform-specific
        /// debug output facility.
        /// </summary>
        /// <param name="message">
        /// The debug message to emit.
        /// </param>
        /// <param name="priority">
        /// The optional priority level to associate with the debug message.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the message was successfully emitted; otherwise, false.
        /// </returns>
        public static bool OutputDebugMessage(
            string message,         /* in */
            DebugPriority? priority /* in: OPTIONAL */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsOutputDebugMessage(message, priority);
#endif

#if UNIX
            if (!NoMacintoshOutputDebugMessage &&
                PlatformOps.IsMacintoshOperatingSystem() &&
                !PlatformOps.IsIntelProcessorArchitecture())
            {
                return MacintoshOutputDebugMessage(message, priority);
            }

            if (PlatformOps.IsUnixOperatingSystem())
                return UnixOutputDebugMessage(message, priority);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method raises the platform-specific console interrupt signal.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// Zero on success; otherwise, a non-zero value indicating the failure.
        /// </returns>
        public static int RaiseConsoleSignal(
            ref Result error /* out */
            )
        {
#if WINDOWS
            if (NativeConsole.IsSupported())
            {
                if (NativeConsole.SendControlEvent(
                        SafeNativeMethods.ConsoleControlEvent,
                        ref error) == ReturnCode.Ok)
                {
                    return 0;
                }
                else
                {
                    return -4;
                }
            }
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
            {
                return UnixRaiseSignal(
                    SafeNativeMethods.ConsoleSignal,
                    ref error);
            }
#endif

            return -2;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method raises the platform-specific console interrupt signal
        /// while temporarily suppressing the console cancellation event handler.
        /// </summary>
        /// <param name="timeout">
        /// The amount of time, in milliseconds, to wait for the console
        /// cancellation event to be reset.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// Zero on success; otherwise, a non-zero value indicating the failure.
        /// </returns>
        public static int RaiseConsoleSignalNoCancel(
            int timeout,     /* in */
            ref Result error /* out */
            )
        {
#if CONSOLE
            /* IGNORED */
            Interpreter.ResetCancelViaConsoleEvent();

            int savedCancelViaConsole = 0;

            Interpreter.BeginNoConsoleCancelEventHandler(
                ref savedCancelViaConsole);

            try
            {
#endif
                return RaiseConsoleSignal(ref error);
#if CONSOLE
            }
            finally
            {
                /* IGNORED */
                Interpreter.WaitForNotCancelViaConsoleEvent(
                    timeout);

                Interpreter.EndNoConsoleCancelEventHandler(
                    ref savedCancelViaConsole);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the total amount of physical memory available on
        /// the system.
        /// </summary>
        /// <param name="totalPhysical">
        /// Upon success, this parameter will be modified to contain the total
        /// amount of physical memory, in bytes.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        public static bool GetTotalMemory(
            ref ulong totalPhysical /* out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = GetTotalMemory(ref totalPhysical, ref error);

            if (code == ReturnCode.Ok)
                return true;

#if DEBUG && VERBOSE
            DebugOps.Complain(code, error);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the approximate percentage of physical memory that
        /// is currently in use.
        /// </summary>
        /// <param name="memoryLoad">
        /// Upon success, this parameter will be modified to contain the
        /// percentage of physical memory currently in use.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        public static bool GetMemoryLoad(
            ref uint memoryLoad /* out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = GetMemoryLoad(ref memoryLoad, ref error);

            if (code == ReturnCode.Ok)
                return true;

#if DEBUG && VERBOSE
            DebugOps.Complain(code, error);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets detailed information about the memory usage of the
        /// system as a list of name/value pairs.
        /// </summary>
        /// <param name="list">
        /// Upon success, this parameter will be modified to contain the list of
        /// memory status name/value pairs.  If this parameter is null, a new
        /// list will be created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetMemoryStatus(
            ref StringList list, /* out */
            ref Result error     /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UNM.MEMORYSTATUSEX memoryStatus;

                WindowsInitializeMemoryStatus(out memoryStatus);

                if (WindowsGetMemoryStatus(ref memoryStatus))
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "memoryLoad",
                        memoryStatus.dwMemoryLoad.ToString(),
                        "totalPhysical",
                        memoryStatus.ullTotalPhys.ToString(),
                        "availablePhysical",
                        memoryStatus.ullAvailPhys.ToString(),
                        "totalPageFile",
                        memoryStatus.ullTotalPageFile.ToString(),
                        "availablePageFile",
                        memoryStatus.ullAvailPageFile.ToString(),
                        "totalVirtual",
                        memoryStatus.ullTotalVirtual.ToString(),
                        "availableVirtual",
                        memoryStatus.ullAvailVirtual.ToString(),
                        "availableExtendedVirtual",
                        memoryStatus.ullAvailExtendedVirtual.ToString());

                    return ReturnCode.Ok;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GlobalMemoryStatusEx() failed with error {0}: {1}",
                        lastError, GetErrorMessage(lastError));

                    return ReturnCode.Error;
                }
            }
#endif

#if !NET_STANDARD_20
            if (CommonOps.Runtime.IsMono())
            {
                uint memoryLoad = 0;
                ulong totalPhysical = 0;
                ulong availablePhysical = 0;

                if (MonoGetMemoryStatus(
                        ref memoryLoad, ref totalPhysical,
                        ref availablePhysical, ref error))
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "memoryLoad", memoryLoad.ToString(),
                        "totalPhysical", totalPhysical.ToString(),
                        "availablePhysical", availablePhysical.ToString());

                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
#endif

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }
        #endregion
    }
}
