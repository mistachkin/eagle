/*
 * NativeConsole.cs --
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using SharedStringOps = Eagle._Components.Shared.StringOps;
using UNM = Eagle._Components.Private.NativeConsole.UnsafeNativeMethods;
using ScreenStack = System.Collections.Generic.Stack<string>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the native Windows console integration used by the
    /// rest of the library, wrapping the relevant Win32 console APIs (via
    /// P/Invoke) for opening, closing, attaching, querying, and configuring the
    /// console as well as its standard input, output, and error handles,
    /// screen buffers, modes, history, and font.  It is only usable on Windows
    /// and can be forcibly disabled via configuration.
    /// </summary>
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("7b199e66-8290-4cdc-8312-d02de62683c5")]
    internal static class NativeConsole
    {
        #region Private Constants
        /// <summary>
        /// The default value indicating whether native (Win32) handles should
        /// be used, as opposed to managed handles, when interacting with the
        /// console.
        /// </summary>
        private static bool DefaultNativeHandle = true; /* IsMono(); */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The native end-of-line character sequence written to the console.
        /// </summary>
        private static readonly char[] NativeNewLine = { '\r', '\n' };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the native console
        //       input and output handles managed by this class (below).
        //
        /// <summary>
        /// This is used to synchronize access to the native console input and
        /// output handles managed by this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *SPECIAL* This is used (with interlocked operations) to
        //       synchronize access to the "isDisabled" field (below).
        //
        /// <summary>
        /// This is used (with interlocked operations) to synchronize access to
        /// the <see cref="isDisabled" /> field.
        /// </summary>
        private static int isDisabledLockCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default, the native console subsystem will be enabled
        //       for all platforms where the necessary native and managed
        //       platform integration has been implemented.  However, it
        //       can be forcibly disabled by setting this field to true
        //       -OR- via setting the associated environment variable
        //       before library startup.
        //
        /// <summary>
        /// When non-null, indicates whether the native console subsystem has
        /// been forcibly disabled.  When null, this has not yet been
        /// determined.  By default, the native console subsystem is enabled on
        /// all platforms where the necessary native and managed platform
        /// integration has been implemented; however, it can be forcibly
        /// disabled by setting this field to true or via the associated
        /// environment variable prior to library startup.
        /// </summary>
        private static bool? isDisabled;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is either zero or the native console input handle
        //       returned via CreateFile for "CONIN$".  When non-zero,
        //       this is considered to be the "primary" console input
        //       buffer.
        //
        /// <summary>
        /// This is either zero or the native console input handle returned via
        /// CreateFile for "CONIN$".  When non-zero, this is considered to be
        /// the "primary" console input buffer.
        /// </summary>
        private static IntPtr inputHandle = IntPtr.Zero;

        //
        // NOTE: This is either zero or the native console output handle
        //       returned via CreateFile for "CONOUT$".  When non-zero,
        //       this is considered to be the "primary" console screen
        //       buffer.
        //
        /// <summary>
        /// This is either zero or the native console output handle returned via
        /// CreateFile for "CONOUT$".  When non-zero, this is considered to be
        /// the "primary" console screen buffer.
        /// </summary>
        private static IntPtr outputHandle = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is either null or the name of the most recently saved
        //       console screen buffer.  Changing the current console screen
        //       buffer will always reset this value.  If this value is null,
        //       the "primary" console screen buffer will be the one that is
        //       reverted back to.
        //
        /// <summary>
        /// This is either null or the name of the most recently saved console
        /// screen buffer.  Changing the current console screen buffer will
        /// always reset this value.  If this value is null, the "primary"
        /// console screen buffer will be the one that is reverted back to.
        /// </summary>
        private static string savedActiveScreenName = null;

        //
        // NOTE: This is the stack of names for the active console screen
        //       buffer.  Initially, this stack will be null.  It will be
        //       created on-demand.  If this stack is null or empty then
        //       the "primary" console screen buffer is considered active.
        //       A screen name will be pushed onto this stack whenever the
        //       active console screen buffer is changed UNLESS it is being
        //       reverted to a previously active console screen buffer.
        //
        /// <summary>
        /// This is the stack of names for the active console screen buffer.
        /// Initially, this stack will be null.  It will be created on-demand.
        /// If this stack is null or empty then the "primary" console screen
        /// buffer is considered active.  A screen name will be pushed onto this
        /// stack whenever the active console screen buffer is changed unless it
        /// is being reverted to a previously active console screen buffer.
        /// </summary>
        private static ScreenStack activeScreenNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This will contain all created console screen buffers, if any,
        //       except the primary console screen buffer.
        //
        /// <summary>
        /// This will contain all created console screen buffers, if any, except
        /// the primary console screen buffer.
        /// </summary>
        private static IntPtrDictionary screenBuffers;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Setting this value to non-null will either force the native
        //       console window to be locked open -OR- prevent it from being
        //       forcibly locked open.
        //
        /// <summary>
        /// When non-null, this either forces the native console window to be
        /// locked open (when true) or prevents it from being forcibly locked
        /// open (when false).
        /// </summary>
        private static bool? forcePreventClose = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the saved console font.  The console font should be
        //       saved prior to changing it.  Also, it should be restored if
        //       this class is being unloaded, for whatever reason.
        //
        /// <summary>
        /// This is the saved console font.  The console font should be saved
        /// prior to changing it.  Also, it should be restored if this class is
        /// being unloaded, for whatever reason.
        /// </summary>
        private static UNM.CONSOLE_FONT_INFOEX? savedConsoleFontEx = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the per-thread character buffer for writing to the
        //       native console.  Its use is merely an optimization.
        //
        /// <summary>
        /// This is the per-thread character buffer for writing to the native
        /// console.  Its use is merely an optimization.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static char[] consoleWriteBuffer = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        /// <summary>
        /// This class contains the P/Invoke method signatures, constants, and
        /// structures for the native Win32 console (and related window) APIs
        /// used by the containing <see cref="NativeConsole" /> class.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6c20c88a-dd55-4e35-a5a5-ec288041c8f4")]
        internal static class UnsafeNativeMethods
        {
            //
            // NOTE: Console input modes.
            //
            /// <summary>
            /// Console input mode flag that enables mouse input events.
            /// </summary>
            internal const uint ENABLE_MOUSE_INPUT = 0x10;

            //
            // NOTE: Console output modes.
            //
            //internal const uint ENABLE_PROCESSED_OUTPUT = 0x01;

            //
            // NOTE: Win32 error numbers.
            //
            /// <summary>
            /// The Win32 error number indicating that no error occurred.
            /// </summary>
            internal const int NO_ERROR = 0;

            /// <summary>
            /// The Win32 error number indicating that an invalid handle was
            /// specified.
            /// </summary>
            internal const int ERROR_INVALID_HANDLE = 6;

            //
            // NOTE: Values returned by GetFileType.
            //
            /// <summary>
            /// File type returned by GetFileType for an unknown (or
            /// indeterminate) file type.
            /// </summary>
            internal const uint FILE_TYPE_UNKNOWN = 0x0;

            /// <summary>
            /// File type returned by GetFileType for a disk file.
            /// </summary>
            internal const uint FILE_TYPE_DISK = 0x1;

            /// <summary>
            /// File type returned by GetFileType for a character device (e.g. a
            /// console or printer).
            /// </summary>
            internal const uint FILE_TYPE_CHAR = 0x2;

            /// <summary>
            /// File type returned by GetFileType for a named or anonymous pipe.
            /// </summary>
            internal const uint FILE_TYPE_PIPE = 0x3;

            /// <summary>
            /// File type bit returned by GetFileType indicating the file is
            /// remote (currently unused by Windows).
            /// </summary>
            internal const uint FILE_TYPE_REMOTE = 0x8000;

            //
            // NOTE: Console handles.
            //
            /// <summary>
            /// The standard device identifier for the standard input handle, as
            /// passed to GetStdHandle and SetStdHandle.
            /// </summary>
            internal const int STD_INPUT_HANDLE = -10;

            /// <summary>
            /// The standard device identifier for the standard output handle, as
            /// passed to GetStdHandle and SetStdHandle.
            /// </summary>
            internal const int STD_OUTPUT_HANDLE = -11;

            /// <summary>
            /// The standard device identifier for the standard error handle, as
            /// passed to GetStdHandle and SetStdHandle.
            /// </summary>
            internal const int STD_ERROR_HANDLE = -12;

            //
            // NOTE: Font family constants.
            //
            /// <summary>
            /// Font family constant indicating no preference as to the font
            /// family.
            /// </summary>
            internal const uint FF_DONTCARE = 0x00;

            /// <summary>
            /// Font family constant for fonts with variable stroke width and
            /// serifs (e.g. Times New Roman).
            /// </summary>
            internal const uint FF_ROMAN = 0x10;

            /// <summary>
            /// Font family constant for fonts with variable stroke width and
            /// without serifs (e.g. Arial).
            /// </summary>
            internal const uint FF_SWISS = 0x20;

            /// <summary>
            /// Font family constant for fonts with constant stroke width, with
            /// or without serifs (e.g. fixed-pitch fonts).
            /// </summary>
            internal const uint FF_MODERN = 0x30;

            /// <summary>
            /// Font family constant for fonts designed to look like handwriting
            /// (e.g. Script).
            /// </summary>
            internal const uint FF_SCRIPT = 0x40;

            /// <summary>
            /// Font family constant for novelty (decorative) fonts (e.g. Old
            /// English).
            /// </summary>
            internal const uint FF_DECORATIVE = 0x50;

            //
            // NOTE: Text metric pitch and family constants.
            //
            /// <summary>
            /// Text metric pitch and family constant indicating none of the
            /// other pitch and family bits are set.
            /// </summary>
            internal const uint TMPF_NONE = 0x00;

            /// <summary>
            /// Text metric pitch and family constant indicating the font is a
            /// variable-pitch (proportional) font; note that, contrary to the
            /// name, the bit is set for variable-pitch fonts.
            /// </summary>
            internal const uint TMPF_FIXED_PITCH = 0x01; /* variable pitch */

            /// <summary>
            /// Text metric pitch and family constant indicating the font is a
            /// vector font.
            /// </summary>
            internal const uint TMPF_VECTOR = 0x02;

            /// <summary>
            /// Text metric pitch and family constant indicating the font is a
            /// TrueType font.
            /// </summary>
            internal const uint TMPF_TRUETYPE = 0x04;

            /// <summary>
            /// Text metric pitch and family constant indicating the font is a
            /// device font.
            /// </summary>
            internal const uint TMPF_DEVICE = 0x08;

            //
            // NOTE: Per MSDN, besides having their own bits set, both
            //       TrueType and PostScript fonts set the TMPF_VECTOR
            //       bit as well.
            //
            /// <summary>
            /// Combined text metric pitch and family constant for TrueType
            /// fonts, which set both the TrueType and vector bits.
            /// </summary>
            internal const uint TMPF_TRUETYPE_VECTOR =
                TMPF_VECTOR | TMPF_TRUETYPE;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Special console file names.
            //
            /// <summary>
            /// The special file name used with CreateFile to open the console
            /// input buffer.
            /// </summary>
            internal const string ConsoleInputFileName = "CONIN$";

            /// <summary>
            /// The special file name used with CreateFile to open the console
            /// screen (output) buffer.
            /// </summary>
            internal const string ConsoleOutputFileName = "CONOUT$";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Special process id.
            //
            /// <summary>
            /// The special process identifier passed to AttachConsole to attach
            /// to the console of the parent process.
            /// </summary>
            internal const int ATTACH_PARENT_PROCESS = -1;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Console history flag value indicating that none of the history
            /// flags are set.
            /// </summary>
            internal const uint HISTORY_NONE = 0;

            /// <summary>
            /// Console history flag indicating that duplicate entries should not
            /// be stored in the command history.
            /// </summary>
            internal const uint HISTORY_NO_DUP_FLAG = 1;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure contains extended information about a console
            /// font, as used by the GetCurrentConsoleFontEx and
            /// SetCurrentConsoleFontEx native methods.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [ObjectId("615b15c1-b994-40cf-8948-a85dcafd9ee1")]
            internal struct CONSOLE_FONT_INFOEX
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public uint cbSize;

                /// <summary>
                /// The index of the font in the system console font table.
                /// </summary>
                public uint nFont;

                /// <summary>
                /// The size, in logical units, of each character in the font.
                /// </summary>
                public COORD dwFontSize;

                /// <summary>
                /// The font pitch and family.
                /// </summary>
                public uint FontFamily;

                /// <summary>
                /// The font weight (e.g. 400 for normal, 700 for bold).
                /// </summary>
                public uint FontWeight;

                /// <summary>
                /// The name of the typeface (face name) of the font.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string FaceName;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure defines the coordinates of a character cell in a
            /// console screen buffer, where the origin is at the top-left
            /// corner.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("cfd3c6be-0c16-4599-8ae8-e2e513daa5f4")]
            internal struct COORD
            {
                /// <summary>
                /// The horizontal (column) coordinate.
                /// </summary>
                public /* SHORT */ short X;

                /// <summary>
                /// The vertical (row) coordinate.
                /// </summary>
                public /* SHORT */ short Y;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure contains the security descriptor for an object
            /// and specifies whether the handle retrieved by specifying it is
            /// inheritable.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("b3bbba22-17eb-49c6-b030-7419de0b4490")]
            internal struct SECURITY_ATTRIBUTES
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public /* DWORD */ uint nLength;

                /// <summary>
                /// A pointer to the security descriptor for the object, or zero
                /// to use the default security descriptor.
                /// </summary>
                public /* LPVOID */ IntPtr lpSecurityDescriptor;

                /// <summary>
                /// Non-zero if the returned handle is inherited when a new
                /// process is created; otherwise, zero.
                /// </summary>
                public /* BOOL */ bool bInheritHandle;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure contains information about the console command
            /// history, as used by the GetConsoleHistoryInfo and
            /// SetConsoleHistoryInfo native methods.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("134a8f50-32fe-4789-bc18-8c37c27ab391")]
            internal struct CONSOLE_HISTORY_INFO
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public /* UINT */ uint cbSize;

                /// <summary>
                /// The number of commands kept in each command history buffer.
                /// </summary>
                public /* UINT */ uint HistoryBufferSize;

                /// <summary>
                /// The number of command history buffers kept simultaneously.
                /// </summary>
                public /* UINT */ uint NumberOfHistoryBuffers;

                /// <summary>
                /// The flags controlling console command history behavior.
                /// </summary>
                public /* DWORD */ uint dwFlags;
            }

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
            /// <summary>
            /// This structure defines the coordinates of the upper-left and
            /// lower-right corners of a rectangle in a console screen buffer.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("16757437-8f5f-4550-b986-6406b5954705")]
            internal struct SMALL_RECT
            {
                /// <summary>
                /// The x-coordinate of the upper-left corner of the rectangle.
                /// </summary>
                public /* SHORT */ short Left;
                /// <summary>
                /// The y-coordinate of the upper-left corner of the rectangle.
                /// </summary>
                public /* SHORT */ short Top;
                /// <summary>
                /// The x-coordinate of the lower-right corner of the rectangle.
                /// </summary>
                public /* SHORT */ short Right;
                /// <summary>
                /// The y-coordinate of the lower-right corner of the rectangle.
                /// </summary>
                public /* SHORT */ short Bottom;
            }

            /// <summary>
            /// This structure contains information about a console screen buffer,
            /// as used by the GetConsoleScreenBufferInfo native method.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("9b96c63e-606d-4b1e-8be7-0945aa7da03a")]
            internal struct CONSOLE_SCREEN_BUFFER_INFO
            {
                /// <summary>
                /// The size, in character cells, of the console screen buffer.
                /// </summary>
                public COORD dwSize;
                /// <summary>
                /// The current position of the cursor within the console screen
                /// buffer.
                /// </summary>
                public COORD dwCursorPosition;
                /// <summary>
                /// The character attributes (text and background colors) used by the
                /// console screen buffer.
                /// </summary>
                public /* WORD */ short wAttributes;
                /// <summary>
                /// The rectangle that describes the portion of the screen buffer
                /// currently displayed in the console window.
                /// </summary>
                public SMALL_RECT srWindow;
                /// <summary>
                /// The maximum size, in character cells, of the console window given
                /// the current screen buffer size and font.
                /// </summary>
                public COORD dwMaximumWindowSize;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves the file type of the specified file, pipe, or device.
            /// </summary>
            /// <param name="handle">
            /// The handle for which the file type is to be retrieved.
            /// </param>
            /// <returns>
            /// One of the FILE_TYPE_* values indicating the type of the file.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern uint GetFileType(IntPtr handle);

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the output code page used by the console associated with
            /// the calling process.
            /// </summary>
            /// <param name="codePageID">
            /// The identifier of the code page to set.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleOutputCP(uint codePageID);

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the input code page used by the console associated with the
            /// calling process.
            /// </summary>
            /// <param name="codePageID">
            /// The identifier of the code page to set.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleCP(uint codePageID);

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the font used by the specified console screen buffer to the
            /// font identified by the specified index in the system console
            /// font table.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer.
            /// </param>
            /// <param name="fontIndex">
            /// The index of the font in the system console font table.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleFont(
                IntPtr handle, uint fontIndex
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves extended information about the current console font.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer.
            /// </param>
            /// <param name="maximumWindow">
            /// Non-zero to retrieve the font information for the maximum window
            /// size; otherwise, zero to retrieve it for the current window
            /// size.
            /// </param>
            /// <param name="consoleFontEx">
            /// Upon success, receives the extended console font information.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCurrentConsoleFontEx(
                IntPtr handle,
                [MarshalAs(UnmanagedType.Bool)] bool maximumWindow,
                ref CONSOLE_FONT_INFOEX consoleFontEx
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets extended information about the current console font.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer.
            /// </param>
            /// <param name="maximumWindow">
            /// Non-zero to set the font information for the maximum window size;
            /// otherwise, zero to set it for the current window size.
            /// </param>
            /// <param name="consoleFontEx">
            /// The extended console font information to set.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetCurrentConsoleFontEx(
                IntPtr handle,
                [MarshalAs(UnmanagedType.Bool)] bool maximumWindow,
                ref CONSOLE_FONT_INFOEX consoleFontEx
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Writes a character string to a console screen buffer beginning
            /// at the current cursor location (Unicode variant).
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer.
            /// </param>
            /// <param name="buffer">
            /// The buffer containing the characters to be written.
            /// </param>
            /// <param name="numberOfCharsToWrite">
            /// The number of characters to be written.
            /// </param>
            /// <param name="numberOfCharsWritten">
            /// Upon success, receives the number of characters actually written.
            /// </param>
            /// <param name="reserved">
            /// Reserved; must be zero.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool WriteConsoleW(
                IntPtr handle,
                char[] buffer,
                uint numberOfCharsToWrite,
                out uint numberOfCharsWritten,
                IntPtr reserved
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves the current input mode of a console input buffer or
            /// the current output mode of a console screen buffer.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console input buffer or console screen buffer.
            /// </param>
            /// <param name="mode">
            /// Upon success, receives the current mode of the specified buffer.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleMode(
                IntPtr handle, ref uint mode
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves a list of the process identifiers of the processes
            /// currently attached to the console.
            /// </summary>
            /// <param name="ids">
            /// A buffer that receives the list of process identifiers.
            /// </param>
            /// <param name="count">
            /// The maximum number of process identifiers that can be stored in
            /// the buffer.
            /// </param>
            /// <returns>
            /// The number of processes attached to the console; if this is
            /// greater than <paramref name="count" />, the buffer was too small
            /// and no identifiers were stored.  Zero indicates failure.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern uint GetConsoleProcessList(
                uint[] ids, uint count
            );

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
             /// <summary>
             /// Retrieves a handle to the event that is signaled when console input
             /// is available.
             /// </summary>
             /// <returns>
             /// A handle to the console input wait event.
             /// </returns>
             /* UNDOCUMENTED */
             [DllImport(DllName.Kernel32,
                 CallingConvention = CallingConvention.Winapi,
                 SetLastError = true)]
             internal static extern IntPtr GetConsoleInputWaitHandle();
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the input mode of a console input buffer or the output mode
            /// of a console screen buffer.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console input buffer or console screen buffer.
            /// </param>
            /// <param name="mode">
            /// The input or output mode to set.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleMode(
                IntPtr handle,
                uint mode
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sends a specified signal to a console process group that shares
            /// the console associated with the calling process.
            /// </summary>
            /// <param name="controlEvent">
            /// The control event (signal) to be generated.
            /// </param>
            /// <param name="processGroupId">
            /// The identifier of the process group to receive the signal, or
            /// zero to signal all processes sharing the console.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GenerateConsoleCtrlEvent(
                ControlEvent controlEvent,
                uint processGroupId
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves a handle to the specified standard device (standard
            /// input, standard output, or standard error).
            /// </summary>
            /// <param name="nStdHandle">
            /// The standard device identifier (one of the STD_*_HANDLE values).
            /// </param>
            /// <returns>
            /// A handle to the specified standard device, or an invalid handle
            /// value on failure.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr GetStdHandle(int nStdHandle);

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves the size of the largest possible console window, based
            /// on the current font and the size of the display.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer.
            /// </param>
            /// <returns>
            /// A <see cref="COORD" /> containing the largest window size, in
            /// character cells; both coordinates are zero on failure.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern COORD GetLargestConsoleWindowSize(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Creates a new console screen buffer.
            /// </summary>
            /// <param name="desiredAccess">
            /// The desired access to the console screen buffer.
            /// </param>
            /// <param name="shareMode">
            /// The sharing mode of the console screen buffer.
            /// </param>
            /// <param name="securityAttributes">
            /// A pointer to a security attributes structure, or zero for the
            /// default security descriptor.
            /// </param>
            /// <param name="flags">
            /// The type of console screen buffer to create.
            /// </param>
            /// <param name="screenBufferData">
            /// Reserved; must be zero.
            /// </param>
            /// <returns>
            /// A handle to the new console screen buffer, or an invalid handle
            /// value on failure.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr CreateConsoleScreenBuffer(
                FileAccessMask desiredAccess,
                FileShareMode shareMode,
                IntPtr securityAttributes,
                ConsoleScreenBufferFlags flags,
                IntPtr screenBufferData
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the specified screen buffer to be the currently displayed
            /// console screen buffer.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer to display.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleActiveScreenBuffer(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
            /// <summary>
            /// Retrieves information about the specified console screen buffer.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console screen buffer.
            /// </param>
            /// <param name="consoleScreenBufferInfo">
            /// Upon success, receives the information about the console screen
            /// buffer.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleScreenBufferInfo(
                IntPtr handle,
                ref CONSOLE_SCREEN_BUFFER_INFO consoleScreenBufferInfo
            );
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the handle for the specified standard device (standard
            /// input, standard output, or standard error).
            /// </summary>
            /// <param name="nStdHandle">
            /// The standard device identifier (one of the STD_*_HANDLE values).
            /// </param>
            /// <param name="handle">
            /// The handle for the standard device.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetStdHandle(
                int nStdHandle,
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves the window handle used by the console associated with
            /// the calling process.
            /// </summary>
            /// <returns>
            /// The window handle of the console window, or zero if there is no
            /// associated console.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Flushes the console input buffer, discarding all pending input
            /// records.
            /// </summary>
            /// <param name="handle">
            /// A handle to the console input buffer.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FlushConsoleInputBuffer(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Attaches the calling process to the console of the specified
            /// process.
            /// </summary>
            /// <param name="processId">
            /// The identifier of the process whose console is to be used, or
            /// <see cref="ATTACH_PARENT_PROCESS" /> for the parent process.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AttachConsole(int processId);

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Allocates a new console for the calling process.
            /// </summary>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AllocConsole();

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Detaches the calling process from its console.
            /// </summary>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeConsole();

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves the history settings for the calling process's
            /// console.
            /// </summary>
            /// <param name="consoleHistoryInfo">
            /// Upon success, receives the console command history settings.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleHistoryInfo(
                ref CONSOLE_HISTORY_INFO consoleHistoryInfo /* out */
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the history settings for the calling process's console.
            /// </summary>
            /// <param name="consoleHistoryInfo">
            /// The console command history settings to apply.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleHistoryInfo(
                ref CONSOLE_HISTORY_INFO consoleHistoryInfo /* in */
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves a handle to the foreground window (the window with
            /// which the user is currently working).
            /// </summary>
            /// <returns>
            /// A handle to the foreground window, or zero if there is no
            /// foreground window.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetForegroundWindow();

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Retrieves a handle to the window that has the keyboard focus, if
            /// the window is attached to the calling thread's message queue.
            /// </summary>
            /// <returns>
            /// A handle to the window with the keyboard focus, or zero if no
            /// such window is attached to the calling thread's message queue.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetFocus();

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Sets the keyboard focus to the specified window.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window that is to receive the keyboard focus, or
            /// zero to remove keyboard focus.
            /// </param>
            /// <returns>
            /// A handle to the window that previously had the keyboard focus, or
            /// zero on failure.
            /// </returns>
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr SetFocus(
                IntPtr hWnd /* in */
            );
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildHostInfoList method.
        //
        /// <summary>
        /// This method adds rows describing the current native console state
        /// (e.g. process list, handles, screen buffers, and saved font) to the
        /// specified list, for introspection purposes.
        /// </summary>
        /// <param name="list">
        /// The list to which the native console information is added.  If this
        /// parameter is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling the level of detail to include.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    ReturnCode code;
                    IntList processIds = null;
                    Result error = null;

                    code = GetProcessList(ref processIds, ref error);

                    if (empty || (code != ReturnCode.Ok) ||
                        ((processIds != null) && (processIds.Count > 0)))
                    {
                        if (code == ReturnCode.Ok)
                        {
                            localList.Add("ProcessList",
                                FormatOps.DisplayList(processIds));
                        }
                        else
                        {
                            localList.Add("ProcessList",
                                ResultOps.Format(code, error));
                        }
                    }
                }

                if (empty || (inputHandle != IntPtr.Zero))
                    localList.Add("InputHandle", inputHandle.ToString());

                if (empty || (outputHandle != IntPtr.Zero))
                    localList.Add("OutputHandle", outputHandle.ToString());

                if (empty || (savedActiveScreenName != null))
                {
                    localList.Add("SavedActiveScreenName",
                        FormatOps.DisplayString(savedActiveScreenName));
                }

                if (empty || ((activeScreenNames != null) &&
                    (activeScreenNames.Count > 0)))
                {
                    localList.Add("ActiveScreenNames",
                        (activeScreenNames != null) ?
                            activeScreenNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty ||
                    ((screenBuffers != null) && (screenBuffers.Count > 0)))
                {
                    localList.Add("ScreenBuffers", (screenBuffers != null) ?
                        screenBuffers.Count.ToString() : FormatOps.DisplayNull);
                }

                if (empty || (forcePreventClose != null))
                {
                    localList.Add("ForcePreventClose",
                        FormatOps.WrapOrNull(forcePreventClose));
                }

                if (empty || (savedConsoleFontEx != null))
                {
                    StringList font = null;

                    if (savedConsoleFontEx != null)
                    {
                        font = FontToList(
                            (UNM.CONSOLE_FONT_INFOEX)
                                savedConsoleFontEx);
                    }

                    localList.Add("SavedConsoleFontEx",
                        (font != null) ? font.ToString() :
                            FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Native Console");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Support Methods
        //
        // WARNING: This method is called from within various contexts
        //          where the static class mutex cannot be used due to
        //          possible deadlocks with the interpreter lock.
        //
        /// <summary>
        /// This method determines whether the native console subsystem is
        /// supported in the current environment, i.e. it has not been forcibly
        /// disabled and the operating system is Windows.
        /// </summary>
        /// <returns>
        /// True if the native console subsystem is supported; otherwise, false.
        /// </returns>
        public static bool IsSupported()
        {
            int lockCount = Interlocked.Increment(ref isDisabledLockCount);

            try
            {
                if (lockCount == 1)
                {
                    if (isDisabled == null)
                        isDisabled = IsDisabled();

                    if ((bool)isDisabled)
                        return false;
                }
            }
            finally
            {
                /* IGNORED */
                Interlocked.Decrement(ref isDisabledLockCount);
            }

            if (!PlatformOps.IsWindowsOperatingSystem())
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Support Methods
        /// <summary>
        /// This method determines whether the native console subsystem has been
        /// forcibly disabled via the associated environment variable.
        /// </summary>
        /// <returns>
        /// True if the native console subsystem has been forcibly disabled;
        /// otherwise, false.
        /// </returns>
        private static bool IsDisabled()
        {
            return GlobalConfiguration.DoesValueExist(
                EnvVars.NoNativeConsole, ConfigurationFlags.NativeConsole);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Support Methods for [host screen] Sub-Command
        /// <summary>
        /// This method determines whether there is at least one active console
        /// screen buffer name on the stack.
        /// </summary>
        /// <returns>
        /// True if there is at least one active screen name; otherwise, false.
        /// </returns>
        public static bool HaveActiveScreenName()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((activeScreenNames != null) &&
                    (activeScreenNames.Count > 0));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the name of the currently active console
        /// screen buffer (the one at the top of the stack).
        /// </summary>
        /// <param name="result">
        /// Upon success, receives the active screen buffer name; upon failure,
        /// receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetActiveScreenName(
            ref Result result /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (activeScreenNames == null)
                {
                    result = "active screen names not available";
                    return ReturnCode.Error;
                }

                if (activeScreenNames.Count == 0)
                {
                    result = "no active screen buffer";
                    return ReturnCode.Error;
                }

                result = activeScreenNames.Peek();
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a console screen buffer with the
        /// specified name exists.
        /// </summary>
        /// <param name="name">
        /// The name of the screen buffer to check for.  If this parameter is
        /// null, this method returns false.
        /// </param>
        /// <param name="primary">
        /// Non-zero to also consider the primary console screen buffer when
        /// checking for a match.
        /// </param>
        /// <returns>
        /// True if a matching screen buffer exists; otherwise, false.
        /// </returns>
        public static bool DoesScreenBufferExist(
            string name, /* in */
            bool primary /* in */
            )
        {
            if (name == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (primary)
                {
                    string primaryName = outputHandle.ToString();

                    if (SharedStringOps.SystemEquals(name, primaryName))
                        return true;
                }

                if (screenBuffers == null)
                    return false;

                return screenBuffers.ContainsKey(name); /* EXEMPT */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves a list of the names of all created console
        /// screen buffers.
        /// </summary>
        /// <param name="primary">
        /// Non-zero to also include the primary console screen buffer in the
        /// returned list.
        /// </param>
        /// <returns>
        /// A list of console screen buffer names, or null if none are
        /// available.
        /// </returns>
        public static StringList ListScreenBuffers(
            bool primary /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringList list = null;

                if (screenBuffers != null)
                {
                    if (list == null)
                        list = new StringList();

                    list.AddRange(screenBuffers.Keys);
                }

                if (primary)
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(outputHandle.ToString());
                }

                return list;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new console screen buffer and makes it the
        /// active console screen buffer, unless one has already been created and
        /// activated (e.g. by another interpreter).
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode MaybeChangeToNewActiveScreenBuffer(
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (HaveScreenBuffer() && HaveActiveScreenName())
                {
                    //
                    // NOTE: There is already at least one created console
                    //       screen buffer -AND- there is already at least
                    //       one active console screen buffer on the stack.
                    //       This may mean another interpreter has already
                    //       completed the operation normally performed by
                    //       this method.
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    string name = null;

                    if (CreateScreenBuffer(
                            ref name, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (ChangeActiveScreenBuffer(
                            name, false, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new console screen buffer and adds it to the
        /// collection of tracked screen buffers.
        /// </summary>
        /// <param name="name">
        /// Upon success, receives the name assigned to the new screen buffer.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CreateScreenBuffer(
            ref string name, /* out */
            ref Result error /* out */
            )
        {
            bool success = false;
            IntPtr handle = IntPtr.Zero;

            try
            {
                //
                // HACK: Since it is harmless to have the exit handler run,
                //       always add it before creating things that it will
                //       cleanup.
                //
                AddExitedEventHandler();

                handle = UNM.CreateConsoleScreenBuffer(
                    FileAccessMask.GENERIC_READ_WRITE,
                    FileShareMode.FILE_SHARE_READ_WRITE, IntPtr.Zero,
                    ConsoleScreenBufferFlags.CONSOLE_TEXTMODE_BUFFER,
                    IntPtr.Zero);

                if (NativeOps.IsValidHandle(handle))
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (screenBuffers == null)
                            screenBuffers = new IntPtrDictionary();

                        name = handle.ToString();

                        screenBuffers.Add(name, handle);
                        success = true;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if (!success && (handle != IntPtr.Zero))
                {
                    Result closeError = null;

                    if (!NativeOps.CloseHandle(handle, ref closeError))
                    {
                        //
                        // HACK: At this point, the local handle may be
                        //       "leaked"; however, the call to CloseHandle
                        //       failed so there is nothing else we can do.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "CreateScreenBuffer: could not close handle: {0}",
                            closeError), typeof(NativeConsole).Name,
                            TracePriority.NativeError);
                    }

                    handle = IntPtr.Zero;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the active console screen buffer to the named
        /// buffer or reverts to a previously active buffer.
        /// </summary>
        /// <param name="name">
        /// The name of the screen buffer to activate.  When
        /// <paramref name="useSaved" /> is non-zero, this should be null to
        /// revert to the previously active screen buffer.
        /// </param>
        /// <param name="useSaved">
        /// Non-zero to revert to the previously active (saved) screen buffer
        /// instead of activating a named one.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the name of the screen buffer that was
        /// previously active; upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ChangeActiveScreenBuffer(
            string name,      /* in */
            bool useSaved,    /* in */
            ref Result result /* out */
            )
        {
            if ((name == null) && !useSaved)
            {
                result = "invalid screen buffer name";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (screenBuffers == null)
                {
                    result = "screen buffers not available";
                    return ReturnCode.Error;
                }

                IntPtr handle;

                if (!useSaved &&
                    screenBuffers.TryGetValue(name, out handle))
                {
                    if (SetActiveScreenBuffer(handle, ref result))
                    {
                        if (activeScreenNames == null)
                            activeScreenNames = new ScreenStack();

                        activeScreenNames.Push(savedActiveScreenName);

                        result = savedActiveScreenName;
                        savedActiveScreenName = name;

                        return ReturnCode.Ok;
                    }
                }
                else if (useSaved &&
                    (name == null) && (activeScreenNames != null) &&
                    (activeScreenNames.Count > 0))
                {
                    savedActiveScreenName = activeScreenNames.Pop();

                    if (savedActiveScreenName != null)
                    {
                        if (screenBuffers.TryGetValue(
                                savedActiveScreenName, out handle))
                        {
                            if (SetActiveScreenBuffer(
                                    handle, ref result))
                            {
                                result = savedActiveScreenName;
                                savedActiveScreenName = null;

                                return ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "saved screen buffer {0} not found",
                                FormatOps.WrapOrNull(savedActiveScreenName));
                        }
                    }
                    else
                    {
                        if (outputHandle != IntPtr.Zero)
                        {
                            if (SetActiveScreenBuffer(
                                    outputHandle, ref result))
                            {
                                result = savedActiveScreenName;
                                savedActiveScreenName = null; /* REDUNDANT */

                                return ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Note use of singular "buffers" in error
                            //       message.
                            //
                            result = "no saved screen buffer to restore";
                        }
                    }
                }
                else
                {
                    if (useSaved && (name == null))
                    {
                        //
                        // NOTE: Note use of plural "buffers" in error
                        //       message.
                        //
                        result = "no saved screen buffers to restore";
                    }
                    else
                    {
                        result = String.Format(
                            "{0}screen buffer {1} not found",
                            useSaved ? "saved " : String.Empty,
                            FormatOps.WrapOrNull(name));
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes and removes the named console screen buffer.
        /// </summary>
        /// <param name="name">
        /// The name of the screen buffer to close.  If this parameter is null,
        /// this method fails.
        /// </param>
        /// <param name="active">
        /// Non-zero to permit closing the screen buffer even if it is the
        /// active one; otherwise, closing the active screen buffer fails.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CloseScreenBuffer(
            string name,     /* in */
            bool active,     /* in */
            ref Result error /* out */
            )
        {
            if (name == null)
            {
                error = "invalid screen buffer name";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (screenBuffers == null)
                {
                    error = "screen buffers not available";
                    return ReturnCode.Error;
                }

                if (!active)
                {
                    if (SharedStringOps.SystemEquals(
                            name, savedActiveScreenName))
                    {
                        error = "cannot close active screen buffer";
                        return ReturnCode.Error;
                    }
                }

                IntPtr handle;

                if (screenBuffers.TryGetValue(name, out handle))
                {
                    if (NativeOps.CloseHandle(handle, ref error))
                    {
                        if (screenBuffers.Remove(name))
                        {
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "screen buffer {0} not removed",
                                FormatOps.WrapOrNull(name));
                        }
                    }
                }
                else
                {
                    error = String.Format(
                        "screen buffer {0} not found",
                        FormatOps.WrapOrNull(name));
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Support Methods for [host screen] Sub-Command
        //
        // WARNING: This method is only for use by the Close and
        //          CleanupActiveScreenNames methods.  All other
        //          callers must use CleanupActiveScreenNames.
        //
        /// <summary>
        /// This method resets the saved active screen name and clears the stack
        /// of active console screen buffer names.  It is only for use by the
        /// <c>Close</c> and <c>CleanupActiveScreenNames</c> methods.
        /// </summary>
        private static void ResetActiveScreenNames()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                savedActiveScreenName = null;

                if (activeScreenNames != null)
                {
                    activeScreenNames.Clear();
                    activeScreenNames = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is only for use by the Close method.
        //          All other callers must use CleanupScreenBuffers.
        //
        /// <summary>
        /// This method clears and discards the collection of tracked console
        /// screen buffers.  It is only for use by the <c>Close</c> method; all
        /// other callers must use <c>CleanupScreenBuffers</c>.
        /// </summary>
        private static void ResetScreenBuffers()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (screenBuffers != null)
                {
                    screenBuffers.Clear();
                    screenBuffers = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether there is at least one created console
        /// screen buffer being tracked.
        /// </summary>
        /// <returns>
        /// True if there is at least one tracked screen buffer; otherwise,
        /// false.
        /// </returns>
        private static bool HaveScreenBuffer()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((screenBuffers != null) &&
                    (screenBuffers.Count > 0));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method makes the specified console screen buffer handle the
        /// active one and resets the affected standard handles and interpreter
        /// channels.
        /// </summary>
        /// <param name="handle">
        /// The handle of the console screen buffer to activate.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the screen buffer was successfully activated; otherwise,
        /// false.
        /// </returns>
        private static bool SetActiveScreenBuffer(
            IntPtr handle,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (!NativeOps.IsValidHandle(handle))
                {
                    error = "invalid screen buffer handle";
                    return false;
                }

                if (!UNM.SetConsoleActiveScreenBuffer(handle))
                {
                    error = NativeOps.GetErrorMessage();
                    return false;
                }

                if (ResetHandles(
                        IntPtr.Zero, handle, false, true,
                        true, ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                /* NO RESULT */
                HostOps.ResetAllInterpreterStandardChannels(
                    ChannelType.Output | ChannelType.Error);

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Standard Handle Support Methods
        /// <summary>
        /// This method retrieves the native console handle for the specified
        /// channel type, using the default native handle preference.
        /// </summary>
        /// <param name="channelType">
        /// The channel type (input, output, or error) whose handle is to be
        /// retrieved.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The console handle for the specified channel, or zero on failure.
        /// </returns>
        public static IntPtr GetHandle(
            ChannelType channelType, /* in */
            ref Result error         /* out */
            )
        {
            return GetHandle(channelType, DefaultNativeHandle, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Standard Handle Support Methods
        /// <summary>
        /// This method retrieves the console handle for the specified channel
        /// type, optionally using the native (Win32) handle.
        /// </summary>
        /// <param name="channelType">
        /// The channel type (input, output, or error) whose handle is to be
        /// retrieved.
        /// </param>
        /// <param name="native">
        /// Non-zero to retrieve the native (Win32) handle; otherwise, the
        /// managed handle is retrieved.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The console handle for the specified channel, or zero on failure.
        /// </returns>
        private static IntPtr GetHandle(
            ChannelType channelType, /* in */
            bool native,             /* in */
            ref Result error         /* out */
            )
        {
            switch (channelType)
            {
                case ChannelType.Input:
                    return GetInputHandle(native, ref error);
                case ChannelType.Output:
                    return GetOutputHandle(native, ref error);
                case ChannelType.Error:
                    return GetErrorHandle(native, ref error);
                default:
                    error = "unsupported console channel";
                    return IntPtr.Zero;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the console input handle, optionally using the
        /// native (Win32) handle.
        /// </summary>
        /// <param name="native">
        /// Non-zero to retrieve the native (Win32) handle; otherwise, the
        /// managed handle is retrieved.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The console input handle, or zero on failure.
        /// </returns>
        private static IntPtr GetInputHandle(
            bool native,     /* in */
            ref Result error /* out */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (native)
                {
                    handle = UNM.GetStdHandle(UNM.STD_INPUT_HANDLE);

                    bool invalid = false;

                    if (!NativeOps.IsValidHandle(handle, ref invalid))
                    {
                        if (invalid)
                            error = NativeOps.GetErrorMessage();
                        else
                            error = "invalid native input handle";
                    }
                }
                else
                {
#if CONSOLE
                    handle = ConsoleOps.GetInputHandle(ref error);
#else
                    error = "not implemented";
#endif
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the console output handle, optionally using
        /// the native (Win32) handle.
        /// </summary>
        /// <param name="native">
        /// Non-zero to retrieve the native (Win32) handle; otherwise, the
        /// managed handle is retrieved.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The console output handle, or zero on failure.
        /// </returns>
        private static IntPtr GetOutputHandle(
            bool native,     /* in */
            ref Result error /* out */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (native)
                {
                    handle = UNM.GetStdHandle(UNM.STD_OUTPUT_HANDLE);

                    bool invalid = false;

                    if (!NativeOps.IsValidHandle(handle, ref invalid))
                    {
                        if (invalid)
                            error = NativeOps.GetErrorMessage();
                        else
                            error = "invalid native output handle";
                    }
                }
                else
                {
#if CONSOLE
                    handle = ConsoleOps.GetOutputHandle(ref error);
#else
                    error = "not implemented";
#endif
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the console error handle.  This is always done
        /// natively, since the System.Console class does not keep track of the
        /// standard error channel.
        /// </summary>
        /// <param name="native">
        /// Non-zero to retrieve the native (Win32) handle; the non-native case
        /// is not implemented for the error channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The console error handle, or zero on failure.
        /// </returns>
        private static IntPtr GetErrorHandle(
            bool native,     /* in */
            ref Result error /* out */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (native)
                {
                    //
                    // NOTE: This is always done natively.  The System.Console
                    //       class does not keep track of the standard error
                    //       channel.
                    //
                    handle = UNM.GetStdHandle(UNM.STD_ERROR_HANDLE);

                    bool invalid = false;

                    if (!NativeOps.IsValidHandle(handle, ref invalid))
                    {
                        if (invalid)
                            error = NativeOps.GetErrorMessage();
                        else
                            error = "invalid native error handle";
                    }
                }
                else
                {
                    error = "not implemented";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the native console handle for the specified channel
        /// type.
        /// </summary>
        /// <param name="channelType">
        /// The channel type (input, output, or error) whose handle is to be
        /// set.
        /// </param>
        /// <param name="handle">
        /// The handle to set for the specified channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the handle was successfully set; otherwise, false.
        /// </returns>
        private static bool SetHandle(
            ChannelType channelType, /* in */
            IntPtr handle,           /* in */
            ref Result error         /* out */
            )
        {
            switch (channelType)
            {
                case ChannelType.Input:
                    return SetInputHandle(handle, ref error);
                case ChannelType.Output:
                    return SetOutputHandle(handle, ref error);
                case ChannelType.Error:
                    return SetErrorHandle(handle, ref error);
                default:
                    error = "unsupported console channel";
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the native console input handle and resets the
        /// associated managed streams.
        /// </summary>
        /// <param name="handle">
        /// The handle to set as the standard input handle.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the handle was successfully set; otherwise, false.
        /// </returns>
        private static bool SetInputHandle(
            IntPtr handle,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (UNM.SetStdHandle(UNM.STD_INPUT_HANDLE, handle))
                {
#if CONSOLE
                    if (ConsoleOps.ResetStreams(
                            ChannelType.Input, ref error) == ReturnCode.Ok)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#else
                    return true;
#endif
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the native console output handle and resets the
        /// associated managed streams.
        /// </summary>
        /// <param name="handle">
        /// The handle to set as the standard output handle.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the handle was successfully set; otherwise, false.
        /// </returns>
        private static bool SetOutputHandle(
            IntPtr handle,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (UNM.SetStdHandle(UNM.STD_OUTPUT_HANDLE, handle))
                {
#if CONSOLE
                    if (ConsoleOps.ResetStreams(
                            ChannelType.Output, ref error) == ReturnCode.Ok)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#else
                    return true;
#endif
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the native console error handle and resets the
        /// associated managed streams.
        /// </summary>
        /// <param name="handle">
        /// The handle to set as the standard error handle.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the handle was successfully set; otherwise, false.
        /// </returns>
        private static bool SetErrorHandle(
            IntPtr handle,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (UNM.SetStdHandle(UNM.STD_ERROR_HANDLE, handle))
                {
#if CONSOLE
                    if (ConsoleOps.ResetStreams(
                            ChannelType.Error, ref error) == ReturnCode.Ok)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#else
                    return true;
#endif
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Input/Output Support Methods
        /// <summary>
        /// This method determines whether the specified console handle has been
        /// redirected (i.e. it does not refer to an actual console).
        /// </summary>
        /// <param name="handle">
        /// The handle to test for redirection.
        /// </param>
        /// <param name="redirected">
        /// Upon success, set to non-zero if the handle appears to be redirected;
        /// otherwise, set to zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode IsHandleRedirected(
            IntPtr handle,       /* in */
            ref bool redirected, /* out */
            ref Result error     /* out */
            )
        {
            if (!NativeOps.IsValidHandle(handle))
            {
                error = "invalid handle";
                return ReturnCode.Error;
            }

            try
            {
                uint type = UNM.GetFileType(handle);

                if ((type != UNM.FILE_TYPE_UNKNOWN) ||
                    (Marshal.GetLastWin32Error() == UNM.NO_ERROR))
                {
                    type &= ~UNM.FILE_TYPE_REMOTE;

                    if (type == UNM.FILE_TYPE_CHAR)
                    {
                        uint mode = 0;

                        if (UNM.GetConsoleMode(handle, ref mode))
                        {
                            //
                            // NOTE: We do not care about the mode, this is a
                            //       console simply because GetConsoleMode
                            //       succeeded.
                            //
                            redirected = false;
                        }
                        else if (Marshal.GetLastWin32Error() == UNM.ERROR_INVALID_HANDLE)
                        {
                            //
                            // NOTE: The handle appears to be valid (see above)
                            //       and it does not appear to be a console
                            //       because GetConsoleMode set the error to
                            //       ERROR_INVALID_HANDLE; therefore, it has
                            //       probably been redirected to something that
                            //       is not a console.
                            //
                            redirected = true;
                        }
                        else
                        {
                            //
                            // NOTE: The handle appears to be valid (see above)
                            //       and it is most likely a console because
                            //       GetConsoleMode did not set the error to
                            //       ERROR_INVALID_HANDLE.
                            //
                            redirected = false;
                        }
                    }
                    else
                    {
                        //
                        // NOTE: The handle appears to be valid (see above); It
                        //       cannot be a console because it is not being
                        //       reported as a character device; therefore, it
                        //       must have been redirected.
                        //
                        redirected = true;
                    }
                }
                else
                {
                    //
                    // NOTE: The handle appears to be valid; however, we cannot
                    //       determine the file type.  We must assume that it
                    //       has not been redirected.
                    //
                    redirected = false;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Input Support Methods
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method sets or resets the console input wait handle event,
        /// which is signaled when console input is available.
        /// </summary>
        /// <param name="set">
        /// Non-zero to signal (set) the console input wait handle; zero to
        /// reset it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the operation succeeds; otherwise, false.
        /// </returns>
        public static bool SetInputWaitHandle(
            bool @set,       /* in */
            ref Result error /* out */
            )
        {
            bool result = false;

            try
            {
                IntPtr handle = UNM.GetConsoleInputWaitHandle();

                if (!NativeOps.IsValidHandle(handle))
                {
                    error = "invalid console input wait handle";
                    return false;
                }

                if (@set)
                    result = NativeOps.UnsafeNativeMethods.SetEvent(handle);
                else
                    result = NativeOps.UnsafeNativeMethods.ResetEvent(handle);

                if (!result)
                    error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sends the specified console control event to all
        /// processes sharing the console.
        /// </summary>
        /// <param name="event">
        /// The control event (signal) to generate.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SendControlEvent(
            ControlEvent @event, /* in */
            ref Result error     /* out */
            )
        {
            try
            {
                if (UNM.GenerateConsoleCtrlEvent(@event, 0))
                    return ReturnCode.Ok;

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes the console input buffer, discarding any pending
        /// input.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode FlushInputBuffer(
            ref Result error /* out */
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    ChannelType.Input, DefaultNativeHandle, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    if (UNM.FlushConsoleInputBuffer(handle))
                        return ReturnCode.Ok;

                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid handle";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Keyboard Support Methods
        /* Eagle._Components.Public.Delegates.CheckCancelCallback */
        /// <summary>
        /// This method determines whether the console window currently has the
        /// keyboard focus.  It conforms to the
        /// <see cref="CheckCancelCallback" /> delegate signature.
        /// </summary>
        /// <param name="clientData">
        /// The client data for the callback.  This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the console window has the keyboard focus; otherwise, false.
        /// </returns>
        private static bool HasWindowFocus(
            IClientData clientData, /* in: NOT USED */
            ref Result error        /* out */
            )
        {
            IntPtr hWnd = IntPtr.Zero; /* NOT USED */

            return HasWindowFocus(ref hWnd, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the console window currently has the
        /// keyboard focus, also returning the console window handle.
        /// </summary>
        /// <param name="hWnd">
        /// Upon return, receives the handle of the console window.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the console window has the keyboard focus; otherwise, false.
        /// </returns>
        private static bool HasWindowFocus(
            ref IntPtr hWnd, /* out */
            ref Result error /* out */
            )
        {
            Result localError = null;

            hWnd = GetWindow(ref localError);

            if (hWnd == IntPtr.Zero)
            {
                if (localError != null)
                    error = localError;
                else
                    error = "invalid window";

                return false;
            }

            return HasWindowFocus(hWnd, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified window currently has
        /// the keyboard focus or is the foreground window.
        /// </summary>
        /// <param name="hWnd">
        /// The handle of the window to test.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// True if the specified window has the keyboard focus; otherwise,
        /// false.
        /// </returns>
        private static bool HasWindowFocus(
            IntPtr hWnd,     /* in */
            ref Result error /* out */
            )
        {
            if (hWnd != IntPtr.Zero)
            {
                try
                {
                    //
                    // TODO: Should this really require checking both of
                    //       these?
                    //
                    if ((UNM.GetFocus() == hWnd) ||
                        (UNM.GetForegroundWindow() == hWnd))
                    {
                        return true;
                    }
                    else
                    {
                        error = "window does not have focus";
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid window";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the console window has the keyboard focus,
        /// optionally attempting to set the focus to it.
        /// </summary>
        /// <param name="setFocus">
        /// Non-zero to attempt to set the keyboard focus to the console window
        /// if it does not already have it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the console window has (or was given)
        /// the keyboard focus; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode CheckWindowFocus(
            bool setFocus,   /* in */
            ref Result error /* out */
            )
        {
            IntPtr hWnd;
            Result localError = null;

            hWnd = GetWindow(ref localError);

            if (hWnd == IntPtr.Zero)
            {
                if (localError != null)
                    error = localError;
                else
                    error = "invalid window";

                return ReturnCode.Error;
            }

            return CheckWindowFocus(hWnd, setFocus, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Keyboard Support Methods
        /// <summary>
        /// This method simulates typing the specified string into the console
        /// window, using a default cancellation callback that checks for window
        /// focus.
        /// </summary>
        /// <param name="stringCallback">
        /// An optional callback invoked to inspect or transform the string
        /// being simulated.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// Optional client data passed to the callbacks.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The string of characters to simulate typing.
        /// </param>
        /// <param name="milliseconds">
        /// The delay, in milliseconds, between simulated keystrokes.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the keyboard input is simulated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SimulateKeyboardString(
            CheckStringCallback stringCallback, /* in: OPTIONAL */
            IClientData clientData,             /* in: OPTIONAL */
            string value,                       /* in */
            int milliseconds,                   /* in */
            SimulatedKeyFlags flags,            /* in */
            ref Result error                    /* out */
            )
        {
            return SimulateKeyboardString(
                null, stringCallback, clientData, value,
                milliseconds, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method simulates typing the specified string into the console
        /// window, first ensuring the console window has the keyboard focus.
        /// </summary>
        /// <param name="cancelCallback">
        /// An optional callback invoked to determine whether the simulation
        /// should be canceled.  This parameter may be null, in which case a
        /// default callback that checks for window focus is used.
        /// </param>
        /// <param name="stringCallback">
        /// An optional callback invoked to inspect or transform the string
        /// being simulated.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// Optional client data passed to the callbacks.  This parameter may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The string of characters to simulate typing.
        /// </param>
        /// <param name="milliseconds">
        /// The delay, in milliseconds, between simulated keystrokes.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the keyboard input is simulated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
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
            if (CheckWindowFocus(FlagOps.HasFlags(
                    flags, SimulatedKeyFlags.SetFocus, true),
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (cancelCallback == null)
            {
                cancelCallback = new CheckCancelCallback(
                    HasWindowFocus);
            }

            return NativeOps.SimulateKeyboardString(
                cancelCallback, stringCallback, clientData,
                value, milliseconds, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the specified window has the keyboard
        /// focus, optionally attempting to set the focus to it.
        /// </summary>
        /// <param name="hWnd">
        /// The handle of the window to check.  If this parameter is zero, this
        /// method fails.
        /// </param>
        /// <param name="setFocus">
        /// Non-zero to attempt to set the keyboard focus to the window if it
        /// does not already have it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the window has (or was given) the
        /// keyboard focus; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CheckWindowFocus(
            IntPtr hWnd,     /* in */
            bool setFocus,   /* in */
            ref Result error /* out */
            )
        {
            if (hWnd == IntPtr.Zero)
            {
                error = "invalid window";
                return ReturnCode.Error;
            }

            if (HasWindowFocus(hWnd, ref error))
                return ReturnCode.Ok;

            if (!setFocus)
            {
                error = "window does not have focus";
                return ReturnCode.Error;
            }

            try
            {
                if (UNM.SetFocus(hWnd) == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();

                    if (lastError != 0)
                    {
                        error = NativeOps.GetErrorMessage(lastError);
                        return ReturnCode.Error;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            if (HasWindowFocus(hWnd, ref error))
                return ReturnCode.Ok;

            error = "failed set focus to window";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Output Support Methods
        /// <summary>
        /// This method retrieves the size, in character cells, of the largest
        /// possible console window based on the current font and display size.
        /// </summary>
        /// <param name="width">
        /// Upon success, receives the largest window width, in character cells.
        /// </param>
        /// <param name="height">
        /// Upon success, receives the largest window height, in character
        /// cells.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetLargestWindowSize(
            ref int width,   /* out */
            ref int height,  /* out */
            ref Result error /* out */
            )
        {
            try
            {
                IntPtr handle;
                bool invalid = false;

                handle = UNM.GetStdHandle(UNM.STD_OUTPUT_HANDLE);

                if (NativeOps.IsValidHandle(handle, ref invalid))
                {
                    UNM.COORD coordinates = UNM.GetLargestConsoleWindowSize(
                        handle);

                    if ((coordinates.X != 0) || (coordinates.Y != 0))
                    {
                        width = coordinates.X;
                        height = coordinates.Y;

                        return ReturnCode.Ok;
                    }

                    error = NativeOps.GetErrorMessage();
                }
                else if (invalid)
                {
                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid native output handle";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Open/Close State Support Methods
        /// <summary>
        /// This method determines whether the console is open, also returning
        /// the console window handle.
        /// </summary>
        /// <param name="handle">
        /// Upon return, receives the handle of the console window (or zero if
        /// the console is not open).
        /// </param>
        /// <returns>
        /// True if the console is open; otherwise, false.
        /// </returns>
        private static bool IsOpen(
            ref IntPtr handle /* out */
            )
        {
            handle = GetWindow();
            return (handle != IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method allocates and initializes a native
        /// <see cref="UnsafeNativeMethods.SECURITY_ATTRIBUTES" /> structure that
        /// allows the resulting handle to be inherited.
        /// </summary>
        /// <param name="pSecurityAttributes">
        /// Upon return, receives a pointer to the newly allocated security
        /// attributes structure.  The caller is responsible for freeing this
        /// memory.
        /// </param>
        private static void CreateSecurityAttributes(
            out IntPtr pSecurityAttributes /* out */
            )
        {
            pSecurityAttributes = Marshal.AllocCoTaskMem(
                Marshal.SizeOf(typeof(UNM.SECURITY_ATTRIBUTES)));

            UNM.SECURITY_ATTRIBUTES securityAttributes =
                new UNM.SECURITY_ATTRIBUTES();

            securityAttributes.nLength = (uint)Marshal.SizeOf(
                typeof(UNM.SECURITY_ATTRIBUTES));

            securityAttributes.lpSecurityDescriptor = IntPtr.Zero;
            securityAttributes.bInheritHandle = true;

            Marshal.StructureToPtr(
                securityAttributes, pSecurityAttributes, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens a new native handle to the console input buffer
        /// ("CONIN$") via CreateFile.
        /// </summary>
        /// <param name="pSecurityAttributes">
        /// A pointer to the security attributes structure to use, or zero for
        /// the default security descriptor.
        /// </param>
        /// <returns>
        /// A native handle to the console input buffer, or an invalid handle
        /// value on failure.
        /// </returns>
        private static IntPtr OpenInputHandle(
            IntPtr pSecurityAttributes /* in */
            )
        {
            return PathOps.UnsafeNativeMethods.CreateFile(
                UNM.ConsoleInputFileName, FileAccessMask.GENERIC_READ_WRITE,
                FileShareMode.FILE_SHARE_READ, pSecurityAttributes,
                FileCreationDisposition.OPEN_EXISTING,
                FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE, IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens a new native handle to the console screen (output)
        /// buffer ("CONOUT$") via CreateFile.
        /// </summary>
        /// <param name="pSecurityAttributes">
        /// A pointer to the security attributes structure to use, or zero for
        /// the default security descriptor.
        /// </param>
        /// <returns>
        /// A native handle to the console screen buffer, or an invalid handle
        /// value on failure.
        /// </returns>
        private static IntPtr OpenOutputHandle(
            IntPtr pSecurityAttributes /* in */
            )
        {
            return PathOps.UnsafeNativeMethods.CreateFile(
                UNM.ConsoleOutputFileName, FileAccessMask.GENERIC_READ_WRITE,
                FileShareMode.FILE_SHARE_WRITE, pSecurityAttributes,
                FileCreationDisposition.OPEN_EXISTING,
                FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE, IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the primary console input or output handle,
        /// opening the handles first if necessary.
        /// </summary>
        /// <param name="output">
        /// Non-zero to return the output handle; otherwise, the input handle is
        /// returned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The requested console handle, or zero on failure.
        /// </returns>
        private static IntPtr GetOrOpenHandle(
            bool output,     /* in */
            ref Result error /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (MaybeOpenHandles(ref error) != ReturnCode.Ok)
                    return IntPtr.Zero;

                return output ? outputHandle : inputHandle;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens new native handles to the console input and/or
        /// screen (output) buffers, cleaning up partially opened handles on
        /// failure.
        /// </summary>
        /// <param name="openInput">
        /// Non-zero to open a new console input handle.
        /// </param>
        /// <param name="openOutput">
        /// Non-zero to open a new console output handle.
        /// </param>
        /// <param name="inputHandle">
        /// Upon success, receives the newly opened console input handle (when
        /// requested).
        /// </param>
        /// <param name="outputHandle">
        /// Upon success, receives the newly opened console output handle (when
        /// requested).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode OpenHandles(
            bool openInput,          /* in */
            bool openOutput,         /* in */
            ref IntPtr inputHandle,  /* out */
            ref IntPtr outputHandle, /* out */
            ref Result error         /* out */
            )
        {
            bool success = false;
            IntPtr pSecurityAttributes = IntPtr.Zero;
            IntPtr localInputHandle = IntPtr.Zero;
            IntPtr localOutputHandle = IntPtr.Zero;

            try
            {
                if (openInput || openOutput)
                    CreateSecurityAttributes(out pSecurityAttributes);

                if (openInput)
                {
                    localInputHandle = OpenInputHandle(pSecurityAttributes);

                    if (!NativeOps.IsValidHandle(localInputHandle))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }
                }

                if (openOutput)
                {
                    localOutputHandle = OpenOutputHandle(pSecurityAttributes);

                    if (!NativeOps.IsValidHandle(localOutputHandle))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }
                }

                inputHandle = localInputHandle;
                outputHandle = localOutputHandle;

                success = true;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if (!success && (localOutputHandle != IntPtr.Zero))
                {
                    Result closeError = null;

                    if (!NativeOps.CloseHandle(
                            localOutputHandle, ref closeError))
                    {
                        //
                        // HACK: At this point, the local handle might be
                        //       "leaked"; however, the call to CloseHandle
                        //       failed so there is nothing else we can do.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "OpenHandles: could not close output handle: {0}",
                            closeError), typeof(NativeConsole).Name,
                            TracePriority.NativeError);
                    }

                    localOutputHandle = IntPtr.Zero;
                }

                ///////////////////////////////////////////////////////////////

                if (!success && (localInputHandle != IntPtr.Zero))
                {
                    Result closeError = null;

                    if (!NativeOps.CloseHandle(
                            localInputHandle, ref closeError))
                    {
                        //
                        // HACK: At this point, the local handle might be
                        //       "leaked"; however, the call to CloseHandle
                        //       failed so there is nothing else we can do.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "OpenHandles: could not close input handle: {0}",
                            closeError), typeof(NativeConsole).Name,
                            TracePriority.NativeError);
                    }

                    localInputHandle = IntPtr.Zero;
                }

                ///////////////////////////////////////////////////////////////

                if (pSecurityAttributes != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pSecurityAttributes);
                    pSecurityAttributes = IntPtr.Zero;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: Using this method used to cause issues with interactive
        //         child processes, e.g. Windows Command Prompt (cmd.exe).
        //         This failure could be most easily be seen by evaluating
        //         the "host.eagle" test file followed by the "exec.eagle"
        //         test file.  This was a bug in Eagle, caused by setting
        //         the lpSecurityAttributes parameter to null.  Instead,
        //         it must be set to a valid structure with bInheritHandle
        //         set to non-zero.
        //
        /// <summary>
        /// This method cleans up any existing console handles, opens fresh
        /// native input and output handles, resets the standard handles, and
        /// resets all interpreter standard channels.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode FixupHandles(
            ref Result error /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (CleanupHandles(true, ref error) != ReturnCode.Ok)
                    return ReturnCode.Error;

                //
                // HACK: Since it is harmless to have the exit handler run,
                //       always add it before creating things that it will
                //       cleanup.
                //
                AddExitedEventHandler();

                if (OpenHandles(
                        true, true, ref inputHandle, ref outputHandle,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (ResetHandles(
                        inputHandle, outputHandle, true, true, true,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                /* NO RESULT */
                HostOps.ResetAllInterpreterStandardChannels(
                    ChannelType.Input | ChannelType.Output |
                    ChannelType.Error);

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the standard input, output, and error handles from
        /// the specified handles, accumulating any errors that occur.
        /// </summary>
        /// <param name="inputHandle">
        /// The handle to set as the standard input handle (when
        /// <paramref name="resetInput" /> is non-zero).
        /// </param>
        /// <param name="outputHandle">
        /// The handle to set as the standard output and standard error handles
        /// (when <paramref name="resetOutput" /> is non-zero).
        /// </param>
        /// <param name="resetInput">
        /// Non-zero to reset the standard input handle.
        /// </param>
        /// <param name="resetOutput">
        /// Non-zero to reset the standard output and standard error handles.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing at the first error encountered.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message (or list of error messages).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ResetHandles(
            IntPtr inputHandle,  /* in */
            IntPtr outputHandle, /* in */
            bool resetInput,     /* in */
            bool resetOutput,    /* in */
            bool stopOnError,    /* in */
            ref Result error     /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            ResultList errors = null;
            Result localError = null; /* REUSED */

            if (resetInput && !SetHandle(
                    ChannelType.Input, inputHandle, ref localError))
            {
                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                code = ReturnCode.Error;

                if (stopOnError)
                {
                    if (errors != null)
                        error = errors;

                    return code;
                }
            }

            localError = null;

            if (resetOutput && !SetHandle(
                    ChannelType.Output, outputHandle, ref localError))
            {
                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                code = ReturnCode.Error;

                if (stopOnError)
                {
                    if (errors != null)
                        error = errors;

                    return code;
                }
            }

            localError = null;

            if (resetOutput && !SetHandle(
                    ChannelType.Error, outputHandle, ref localError))
            {
                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                code = ReturnCode.Error;

                if (stopOnError)
                {
                    if (errors != null)
                        error = errors;

                    return code;
                }
            }

            if (errors != null)
                error = errors;

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the specified console input and output handles,
        /// accumulating any errors that occur.
        /// </summary>
        /// <param name="stopOnError">
        /// Non-zero to stop processing at the first error encountered.
        /// </param>
        /// <param name="inputHandle">
        /// The console input handle to close.  Upon return, this is set to zero
        /// if it was closed.
        /// </param>
        /// <param name="outputHandle">
        /// The console output handle to close.  Upon return, this is set to
        /// zero if it was closed.
        /// </param>
        /// <param name="errors">
        /// A list to which any error messages encountered are added; it is
        /// created on-demand.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode CloseHandles(
            bool stopOnError,        /* in */
            ref IntPtr inputHandle,  /* in, out */
            ref IntPtr outputHandle, /* in, out */
            ref ResultList errors    /* in, out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            Result localError; /* REUSED */

            if (NativeOps.IsValidHandle(inputHandle))
            {
                localError = null;

                if (!NativeOps.CloseHandle(inputHandle, ref localError))
                {
                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    code = ReturnCode.Error;

                    if (stopOnError)
                        return code;
                }

                inputHandle = IntPtr.Zero;
            }

            if (NativeOps.IsValidHandle(outputHandle))
            {
                localError = null;

                if (!NativeOps.CloseHandle(outputHandle, ref localError))
                {
                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    code = ReturnCode.Error;

                    if (stopOnError)
                        return code;
                }

                outputHandle = IntPtr.Zero;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Open/Close State Support Methods
        /// <summary>
        /// This method determines whether a console is currently open (i.e.
        /// associated with the calling process).
        /// </summary>
        /// <returns>
        /// True if a console is open; otherwise, false.
        /// </returns>
        public static bool IsOpen()
        {
            return GetWindow() != IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the window handle of the console associated
        /// with the calling process, ignoring any error.
        /// </summary>
        /// <returns>
        /// The window handle of the console window, or zero if there is no
        /// associated console.
        /// </returns>
        public static IntPtr GetWindow()
        {
            Result error = null; /* NOT USED */

            return GetWindow(ref error); /* PUBLIC */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the window handle of the console associated
        /// with the calling process, when running on Windows.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The window handle of the console window, or zero if there is no
        /// associated console (or the platform is not Windows).
        /// </returns>
        public static IntPtr GetWindow(
            ref Result error /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    return UNM.GetConsoleWindow(); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens the primary native console input and output
        /// handles if neither has already been opened.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode MaybeOpenHandles(
            ref Result error /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If one or both of the native handles has already
                //       been opened, do nothing.
                //
                if ((inputHandle != IntPtr.Zero) ||
                    (outputHandle != IntPtr.Zero))
                {
                    return ReturnCode.Ok;
                }

                //
                // HACK: Since it is harmless to have the exit handler run,
                //       always add it before creating things that it will
                //       cleanup.
                //
                AddExitedEventHandler();

                if (OpenHandles(
                        true, true, ref inputHandle, ref outputHandle,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the native console window should be
        /// prevented from being closed, honoring any manual override.
        /// </summary>
        /// <param name="attached">
        /// Indicates whether the console was attached (as opposed to freshly
        /// opened by this class).  When null, the console state is unknown.
        /// </param>
        /// <returns>
        /// True if the console window should be prevented from being closed;
        /// otherwise, false.
        /// </returns>
        public static bool ShouldPreventClose(
            bool? attached
            )
        {
            //
            // HACK: Check for a manual override for the console
            //       "locking" behavior, just in case.
            //
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (forcePreventClose != null)
                    return (bool)forcePreventClose;
            }

            //
            // NOTE: These checks mean that the console was freshly
            //       opened by this class (i.e. and not attached);
            //       therefore, it is assumed that the application
            //       itself is unaware of the console.  Also, if a
            //       user closes the console window, this process
            //       will be unceremoniously terminated by Windows.
            //
            if (attached == null)
                return false;

            return !(bool)attached;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locks the native console window open, preventing it from
        /// being closed by the user.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success (including when the console
        /// is not open); otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode PreventClose(
            ref Result error /* out */
            )
        {
            try
            {
                IntPtr handle = IntPtr.Zero;

                if (!IsOpen(ref handle))
                {
                    TraceOps.DebugTrace(
                        "PreventClose: console not open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (WindowOps.PreventWindowClose(
                        handle, ref error))
                {
                    TraceOps.DebugTrace(
                        "PreventClose: locked console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method attaches the calling process to the console of its
        /// parent process and fixes up the associated handles.
        /// </summary>
        /// <param name="force">
        /// Non-zero to attach to the parent console even if a console already
        /// appears to be open.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Attach(
            bool force,      /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (!force && IsOpen())
                {
                    TraceOps.DebugTrace(
                        "Attach: console already open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (UNM.AttachConsole(UNM.ATTACH_PARENT_PROCESS))
                {
                    TraceOps.DebugTrace(
                        "Attach: attached parent console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return FixupHandles(ref error);
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method allocates a new console for the calling process and
        /// fixes up the associated handles.
        /// </summary>
        /// <param name="force">
        /// Non-zero to allocate a new console even if one already appears to be
        /// open.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Open(
            bool force,      /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (!force && IsOpen())
                {
                    TraceOps.DebugTrace(
                        "Open: console already open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (UNM.AllocConsole())
                {
                    TraceOps.DebugTrace(
                        "Open: allocated new console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return FixupHandles(ref error);
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attaches to the parent process's console (when
        /// requested) or, failing that, allocates a new console, then fixes up
        /// the associated handles.
        /// </summary>
        /// <param name="force">
        /// Non-zero to attach or allocate even if a console already appears to
        /// be open.
        /// </param>
        /// <param name="attach">
        /// Non-zero to first attempt to attach to the parent process's console
        /// before allocating a new one.
        /// </param>
        /// <param name="attached">
        /// Upon success, set to non-zero if the console was attached, or zero if
        /// a new console was allocated.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode AttachOrOpen(
            bool force,         /* in */
            bool attach,        /* in */
            ref bool? attached, /* out */
            ref Result error    /* out */
            )
        {
            try
            {
                if (!force && IsOpen())
                {
                    TraceOps.DebugTrace(
                        "AttachOrOpen: console already open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (attach && UNM.AttachConsole(UNM.ATTACH_PARENT_PROCESS))
                {
                    TraceOps.DebugTrace(
                        "AttachOrOpen: attached parent console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    attached = true;
                    return FixupHandles(ref error);
                }

                if (UNM.AllocConsole())
                {
                    TraceOps.DebugTrace(
                        "AttachOrOpen: allocated new console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    attached = false;
                    return FixupHandles(ref error);
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method frees the console associated with the calling process,
        /// resetting the tracked handles, screen buffers, and active screen
        /// names.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Close(
            ref Result error /* out */
            )
        {
            try
            {
                if (UNM.FreeConsole())
                {
                    TraceOps.DebugTrace(
                        "Close: freed existing console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        inputHandle = IntPtr.Zero;
                        outputHandle = IntPtr.Zero;
                    }

                    ResetScreenBuffers();
                    ResetActiveScreenNames();

                    if (ResetHandles(
                            IntPtr.Zero, IntPtr.Zero, true, true,
                            true, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Console Mode Support Methods
        /// <summary>
        /// This method retrieves the current console mode for the specified
        /// channel type.
        /// </summary>
        /// <param name="channelType">
        /// The channel type (input, output, or error) whose mode is to be
        /// retrieved.
        /// </param>
        /// <param name="mode">
        /// Upon success, receives the current console mode.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetMode(
            ChannelType channelType, /* in */
            ref uint mode,           /* out */
            ref Result error         /* out */
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    channelType, DefaultNativeHandle, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    if (UNM.GetConsoleMode(handle, ref mode))
                        return ReturnCode.Ok;

                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the console mode for the specified channel type.
        /// </summary>
        /// <param name="channelType">
        /// The channel type (input, output, or error) whose mode is to be set.
        /// </param>
        /// <param name="mode">
        /// The console mode to set.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SetMode(
            ChannelType channelType, /* in */
            uint mode,               /* in */
            ref Result error         /* out */
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    channelType, DefaultNativeHandle, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    if (UNM.SetConsoleMode(handle, mode))
                        return ReturnCode.Ok;

                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the specified mode bits in the
        /// current console mode for the specified channel type.
        /// </summary>
        /// <param name="channelType">
        /// The channel type (input, output, or error) whose mode is to be
        /// changed.
        /// </param>
        /// <param name="enable">
        /// Non-zero to add the specified mode bits; otherwise, the specified
        /// mode bits are removed.
        /// </param>
        /// <param name="mode">
        /// The mode bits to add or remove.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ChangeMode(
            ChannelType channelType, /* in */
            bool enable,             /* in */
            uint mode,               /* in */
            ref Result error         /* out */
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    channelType, DefaultNativeHandle, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    uint currentMode = 0;

                    if (UNM.GetConsoleMode(handle, ref currentMode))
                    {
                        if (enable)
                            currentMode |= mode;  /* NOTE: Add mode(s). */
                        else
                            currentMode &= ~mode; /* NOTE: Remove mode(s). */

                        if (UNM.SetConsoleMode(handle, currentMode))
                        {
                            return ReturnCode.Ok;
                        }
                    }

                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Console History Support Methods
        /// <summary>
        /// This method clears the console command history by temporarily setting
        /// the history buffer size to zero and then restoring it.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ClearHistory(
            ref Result error /* out */
            )
        {
            try
            {
                UNM.CONSOLE_HISTORY_INFO historyInfo =
                    new UNM.CONSOLE_HISTORY_INFO();

                historyInfo.cbSize = (uint)Marshal.SizeOf(
                    typeof(UNM.CONSOLE_HISTORY_INFO));

                if (!UNM.GetConsoleHistoryInfo(ref historyInfo))
                {
                    error = NativeOps.GetErrorMessage();
                    return ReturnCode.Error;
                }

                uint savedBufferSize = historyInfo.HistoryBufferSize;

                try
                {
                    historyInfo.HistoryBufferSize = 0;

                    if (UNM.SetConsoleHistoryInfo(ref historyInfo))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }
                }
                finally
                {
                    historyInfo.HistoryBufferSize = savedBufferSize;

                    if (!UNM.SetConsoleHistoryInfo(ref historyInfo))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "ClearHistory: " +
                            "could not restore history size: {0}",
                            NativeOps.GetErrorMessage()),
                            typeof(NativeConsole).Name,
                            TracePriority.NativeError);
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Figure out if this method should change the struct field
        //       NumberOfHistoryBuffers as well.  What exactly does it do?
        //
        /// <summary>
        /// This method ensures the console command history buffer size is at
        /// least the specified minimum.
        /// </summary>
        /// <param name="minimumBufferSize">
        /// The minimum required history buffer size.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SetupHistory(
            uint minimumBufferSize, /* in */
            ref Result error        /* out */
            )
        {
            try
            {
                UNM.CONSOLE_HISTORY_INFO historyInfo =
                    new UNM.CONSOLE_HISTORY_INFO();

                historyInfo.cbSize = (uint)Marshal.SizeOf(
                    typeof(UNM.CONSOLE_HISTORY_INFO));

                if (!UNM.GetConsoleHistoryInfo(ref historyInfo))
                {
                    error = NativeOps.GetErrorMessage();
                    return ReturnCode.Error;
                }

                if (historyInfo.HistoryBufferSize < minimumBufferSize)
                {
                    historyInfo.HistoryBufferSize = minimumBufferSize;

                    if (!UNM.SetConsoleHistoryInfo(ref historyInfo))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Console Font Support Methods
        /// <summary>
        /// This method converts the specified extended console font information
        /// into a name/value string list for display purposes.
        /// </summary>
        /// <param name="consoleFontEx">
        /// The extended console font information to convert.
        /// </param>
        /// <returns>
        /// A string list containing the font properties as name/value pairs.
        /// </returns>
        private static StringList FontToList(
            UNM.CONSOLE_FONT_INFOEX consoleFontEx /* in */
            )
        {
            StringList list = new StringList();

            list.Add("sizeOf", consoleFontEx.cbSize.ToString());
            list.Add("index", consoleFontEx.nFont.ToString());
            list.Add("sizeX", consoleFontEx.dwFontSize.X.ToString());
            list.Add("sizeY", consoleFontEx.dwFontSize.Y.ToString());
            list.Add("family", consoleFontEx.FontFamily.ToString());
            list.Add("weight", consoleFontEx.FontWeight.ToString());
            list.Add("faceName", consoleFontEx.FaceName);

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the native end-of-line character sequence, or no
        /// sequence, based on whether a new line is requested.
        /// </summary>
        /// <param name="newLine">
        /// Non-zero to return the native end-of-line character sequence;
        /// otherwise, an empty sequence is returned.
        /// </param>
        /// <param name="value">
        /// Upon return, receives the end-of-line character sequence, or null if
        /// none was requested.
        /// </param>
        /// <param name="length">
        /// Upon return, receives the length of the end-of-line character
        /// sequence, or zero if none was requested.
        /// </param>
        private static void GetNewLine(
            bool newLine,     /* in */
            out char[] value, /* out */
            out int length    /* out */
            )
        {
            value = null;
            length = 0;

            if (newLine)
            {
                value = NativeNewLine;

                if (value != null)
                    length = value.Length;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the per-thread console write buffer, growing it
        /// if necessary to hold at least the specified number of characters.
        /// </summary>
        /// <param name="length">
        /// The minimum required length, in characters, of the buffer.
        /// </param>
        /// <returns>
        /// The per-thread console write buffer.
        /// </returns>
        private static char[] GetWriteBuffer(
            int length /* in */
            )
        {
            if ((consoleWriteBuffer == null) ||
                (consoleWriteBuffer.Length < length))
            {
                consoleWriteBuffer = new char[length];
            }

            return consoleWriteBuffer;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a console write buffer sized to hold the
        /// specified content plus an optional trailing new line, pre-populated
        /// with the new line (if any) and cleared otherwise.
        /// </summary>
        /// <param name="length">
        /// The length, in characters, of the content (excluding any new line).
        /// </param>
        /// <param name="newLine">
        /// Non-zero to reserve and append the native end-of-line character
        /// sequence at the end of the buffer.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to always allocate a fresh buffer instead of reusing the
        /// per-thread cached buffer.
        /// </param>
        /// <returns>
        /// A console write buffer; this method never returns null.
        /// </returns>
        private static char[] GetWriteBuffer( /* CANNOT RETURN NULL */
            int length,   /* in */
            bool newLine, /* in */
            bool noCache  /* in */
            )
        {
            char[] newLineValue;
            int newLineLength;

            GetNewLine(
                newLine, out newLineValue, out newLineLength);

            int resultLength = length + newLineLength;
            char[] result;

            if (noCache)
                result = new char[resultLength];
            else
                result = GetWriteBuffer(resultLength);

            Array.Clear(result, 0, result.Length);

            if (newLineValue != null)
            {
                Array.Copy(
                    newLineValue, 0, result, length, newLineLength);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a console write buffer from the specified value,
        /// which must be a character array, a single character, or a string,
        /// optionally appending a trailing new line.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to write; must be <c>char[]</c>, <c>char</c>,
        /// or <c>string</c>.
        /// </typeparam>
        /// <param name="value">
        /// The value to convert into a write buffer.  If this parameter is
        /// null, this method fails.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append the native end-of-line character sequence (only
        /// applies to single-character and string values).
        /// </param>
        /// <param name="noCache">
        /// Non-zero to always allocate a fresh buffer instead of reusing the
        /// per-thread cached buffer.
        /// </param>
        /// <param name="length">
        /// Upon return, receives the number of content characters in the
        /// returned buffer (excluding any appended new line).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The console write buffer, or null on failure.
        /// </returns>
        private static char[] GetWriteBuffer<T>(
            T value,         /* in */
            bool newLine,    /* in */
            bool noCache,    /* in */
            out int length,  /* out */
            ref Result error /* out */
            )
        {
            length = 0;

            if (value == null)
            {
                error = "invalid value";
                return null;
            }

            char[] result;

            if (typeof(T) == typeof(char[]))
            {
                if (!(value is char[]))
                {
                    error = String.Format(
                        "unsupported value {0}, must be {1}",
                        MarshalOps.GetErrorTypeName(typeof(T)),
                        MarshalOps.GetErrorTypeName(typeof(char[])));

                    return null;
                }

                char[] arrayValue = (char[])(value as object);

                length = arrayValue.Length;
                result = arrayValue;

                return result;
            }
            else if (typeof(T) == typeof(char))
            {
                if (!(value is char))
                {
                    error = String.Format(
                        "unsupported value {0}, must be {1}",
                        MarshalOps.GetErrorTypeName(typeof(T)),
                        MarshalOps.GetErrorTypeName(typeof(char)));

                    return null;
                }

                length = 1;
                result = GetWriteBuffer(length, newLine, noCache);
                result[0] = (char)(value as object);

                return result;
            }
            else if (typeof(T) == typeof(string))
            {
                if (!(value is string))
                {
                    error = String.Format(
                        "unsupported value {0}, must be {1}",
                        MarshalOps.GetErrorTypeName(typeof(T)),
                        MarshalOps.GetErrorTypeName(typeof(string)));

                    return null;
                }

                string stringValue = (string)(value as object);

                length = stringValue.Length;
                result = GetWriteBuffer(length, newLine, noCache);
                stringValue.CopyTo(0, result, 0, length);

                return result;
            }
            else
            {
                error = String.Format(
                    "unsupported value type {0} for write",
                    MarshalOps.GetErrorTypeName(typeof(T)));

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the given console handle,
        /// optionally appending a trailing new line.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to write; must be <c>char[]</c>, <c>char</c>,
        /// or <c>string</c>.
        /// </typeparam>
        /// <param name="handle">
        /// The console handle to write to.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append the native end-of-line character sequence.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode WriteString<T>(
            IntPtr handle,   /* in */
            T value,         /* in */
            bool newLine,    /* in */
            ref Result error /* out */
            )
        {
            int length;

            char[] buffer = GetWriteBuffer<T>(
                value, newLine, false, out length, ref error);

            if (buffer == null)
                return ReturnCode.Error;

            uint numberWritten;

            if (!UNM.WriteConsoleW(
                    handle, buffer, (uint)length,
                    out numberWritten, IntPtr.Zero))
            {
                error = NativeOps.GetErrorMessage();
                return ReturnCode.Error;
            }

            if (numberWritten != length)
            {
                error = String.Format(
                    "actually wrote {0}, wanted to write {1}",
                    numberWritten, length);

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Console Font Support Methods
        /// <summary>
        /// This method retrieves the current console font as a name/value
        /// string list.
        /// </summary>
        /// <param name="list">
        /// Upon success, receives the current console font properties as a
        /// string list.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetFont(
            ref StringList list, /* in, out */
            ref Result error     /* out */
            )
        {
            try
            {
                IntPtr handle = GetOrOpenHandle(true, ref error);

                if (!NativeOps.IsValidHandle(handle))
                    return ReturnCode.Error;

                UNM.CONSOLE_FONT_INFOEX consoleFontEx =
                    new UNM.CONSOLE_FONT_INFOEX();

                consoleFontEx.cbSize = (uint)Marshal.SizeOf(
                    typeof(UNM.CONSOLE_FONT_INFOEX));

                if (!UNM.GetCurrentConsoleFontEx(
                        handle, false, ref consoleFontEx))
                {
                    error = NativeOps.GetErrorMessage();
                    return ReturnCode.Error;
                }

                list = FontToList(consoleFontEx);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the console font by font index or by face name and
        /// (optionally) size, saving the original font first unless requested
        /// otherwise.
        /// </summary>
        /// <param name="faceName">
        /// The font face name to set, or a string that parses as an unsigned
        /// integer to select a font by index.  This parameter may be null.
        /// </param>
        /// <param name="fontSize">
        /// The optional font size (height), in logical units.  This parameter
        /// may be null to leave the size unchanged.
        /// </param>
        /// <param name="noSave">
        /// Non-zero to skip saving the original console font before changing it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SetFont(
            string faceName, /* in: OPTIONAL */
            short? fontSize, /* in: OPTIONAL */
            bool noSave,     /* in */
            ref Result error /* out */
            )
        {
            try
            {
                IntPtr handle = GetOrOpenHandle(true, ref error);

                if (!NativeOps.IsValidHandle(handle))
                    return ReturnCode.Error;

                UNM.CONSOLE_FONT_INFOEX consoleFontEx; /* REUSED */

                if (!noSave)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (savedConsoleFontEx == null)
                        {
                            consoleFontEx = new UNM.CONSOLE_FONT_INFOEX();

                            consoleFontEx.cbSize = (uint)Marshal.SizeOf(
                                typeof(UNM.CONSOLE_FONT_INFOEX));

                            if (!UNM.GetCurrentConsoleFontEx(
                                    handle, false, ref consoleFontEx))
                            {
                                error = NativeOps.GetErrorMessage();
                                return ReturnCode.Error;
                            }

                            TraceOps.DebugTrace(String.Format(
                                "SetFont: original = {0}", FontToList(
                                consoleFontEx)), typeof(NativeConsole).Name,
                                TracePriority.ConsoleDebug2);

                            savedConsoleFontEx = consoleFontEx;
                        }
                    }
                }

                uint fontIndex = 0;

                if (Value.GetUnsignedInteger2(
                        faceName, ValueFlags.AnyInteger, null,
                        ref fontIndex) == ReturnCode.Ok)
                {
                    if (!UNM.SetConsoleFont(handle, fontIndex))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    consoleFontEx = new UNM.CONSOLE_FONT_INFOEX();

                    consoleFontEx.cbSize = (uint)Marshal.SizeOf(
                        typeof(UNM.CONSOLE_FONT_INFOEX));

                    if (!UNM.GetCurrentConsoleFontEx(
                            handle, false, ref consoleFontEx))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }

                    TraceOps.DebugTrace(String.Format(
                        "SetFont: before = {0}", FontToList(
                        consoleFontEx)), typeof(NativeConsole).Name,
                        TracePriority.ConsoleDebug2);

                    consoleFontEx.nFont = 0; // TODO: Zero is allowed?

                    consoleFontEx.FontFamily =
                        UNM.FF_MODERN | UNM.TMPF_TRUETYPE_VECTOR;

                    if (fontSize != null)
                    {
                        consoleFontEx.dwFontSize.X = 0; // TODO: Zero allowed?
                        consoleFontEx.dwFontSize.Y = (short)fontSize;
                    }

                    consoleFontEx.FaceName = faceName;

                    TraceOps.DebugTrace(String.Format(
                        "SetFont: modified = {0}", FontToList(
                        consoleFontEx)), typeof(NativeConsole).Name,
                        TracePriority.ConsoleDebug2);

                    if (!UNM.SetCurrentConsoleFontEx(
                            handle, false, ref consoleFontEx))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }

#if DEBUG || FORCE_TRACE
                    consoleFontEx = new UNM.CONSOLE_FONT_INFOEX();

                    consoleFontEx.cbSize = (uint)Marshal.SizeOf(
                        typeof(UNM.CONSOLE_FONT_INFOEX));

                    if (!UNM.GetCurrentConsoleFontEx(
                            handle, false, ref consoleFontEx))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }

                    TraceOps.DebugTrace(String.Format(
                        "SetFont: after = {0}", FontToList(
                        consoleFontEx)), typeof(NativeConsole).Name,
                        TracePriority.ConsoleDebug2);
#endif
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the previously saved console font, if any.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to treat the absence of a saved console font as success
        /// rather than as an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CleanupFont(
            bool noComplain, /* in */
            ref Result error /* out */
            )
        {
            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (savedConsoleFontEx == null)
                    {
                        if (noComplain)
                        {
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "no saved console font";
                            return ReturnCode.Error;
                        }
                    }

                    IntPtr handle = GetOrOpenHandle(true, ref error);

                    if (!NativeOps.IsValidHandle(handle))
                        return ReturnCode.Error;

                    UNM.CONSOLE_FONT_INFOEX consoleFontEx =
                        (UNM.CONSOLE_FONT_INFOEX)savedConsoleFontEx;

                    if (!UNM.SetCurrentConsoleFontEx(
                            handle, false, ref consoleFontEx))
                    {
                        error = NativeOps.GetErrorMessage();
                        return ReturnCode.Error;
                    }

                    savedConsoleFontEx = null;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the primary console output
        /// (screen) buffer, opening the handle first if necessary, optionally
        /// appending a trailing new line.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to write; must be <c>char[]</c>, <c>char</c>,
        /// or <c>string</c>.
        /// </typeparam>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append the native end-of-line character sequence.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode WriteString<T>(
            T value,
            bool newLine,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                IntPtr handle = GetOrOpenHandle(true, ref error);

                if (!NativeOps.IsValidHandle(handle))
                    return ReturnCode.Error;

                return WriteString<T>(handle, value, newLine, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the native end-of-line character sequence to the
        /// primary console output (screen) buffer.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode WriteLine(
            ref Result error /* out */
            )
        {
            return WriteString<char[]>(NativeNewLine, false, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Console Other Support Methods
        /// <summary>
        /// This method retrieves the list of process identifiers for the
        /// processes currently attached to the console.
        /// </summary>
        /// <param name="list">
        /// Upon success, receives the list of attached process identifiers.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetProcessList(
            ref IntList list,
            ref Result error
            )
        {
            try
            {
                uint count = 1;
                uint[] ids = new uint[count];

                count = UNM.GetConsoleProcessList(ids, count);

                if (count == 1)
                {
                    list = new IntList(ids);
                    return ReturnCode.Ok;
                }
                else if (count > 0)
                {
                    ids = new uint[count];

                    count = UNM.GetConsoleProcessList(ids, count);

                    if (count > 0)
                    {
                        list = new IntList(ids);
                        return ReturnCode.Ok;
                    }
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Support Methods for [host exit] Sub-Command
        //
        // TODO: Rediscover the true purpose of this method.  Why does it not
        //       operate on the actual input handle?  What was the design of
        //       its original error handling?  At first glance, this method
        //       does appear to work "correctly" in that it prevents further
        //       trips through the interactive loop when executed via #hexit,
        //       which causes it to be run asynchronously against the console
        //       host.  It should be noted this method has been refactored
        //       several times, primarily to improve its error handling.
        //
        /// <summary>
        /// This method closes the console output and error handles (used for
        /// the screen buffer), notifying other native and managed code that
        /// they are no longer valid.  It is used to implement the
        /// <c>[host exit]</c> sub-command.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message (or list of error messages).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CloseStandardInput(
            ref Result error /* out */
            )
        {
            ResultList errors = null;
            Result localError; /* REUSED */

            //
            // TODO: Huh, output?  Why?  The output handle is allowed
            //       to be invalid here; however, we track all errors.
            //
            localError = null;

            IntPtr outputHandle = GetHandle(
                ChannelType.Output, ref localError);

            if ((outputHandle == IntPtr.Zero) && (localError != null))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            //
            // TODO: Huh, output?  Why?  The error handle is allowed
            //       to be invalid here; however, we track all errors.
            //
            localError = null;

            IntPtr errorHandle = GetHandle(
                ChannelType.Error, ref localError);

            if ((errorHandle == IntPtr.Zero) && (localError != null))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            //
            // NOTE: Does one of the [console] output handles need to be
            //       closed (i.e. the one used for the screen buffer).
            //
            if (NativeOps.IsValidHandle(outputHandle) ||
                NativeOps.IsValidHandle(errorHandle))
            {
                //
                // NOTE: Does the [console] output handle look like it
                //       needs to be closed?
                //
                if (NativeOps.IsValidHandle(outputHandle))
                {
                    localError = null;

                    if (NativeOps.CloseHandle(
                            outputHandle, ref localError))
                    {
                        //
                        // NOTE: Notify other native and managed code
                        //       that the [console] output handle is
                        //       no longer valid.
                        //
                        localError = null;

                        if (!SetHandle(
                                ChannelType.Output, IntPtr.Zero,
                                ref localError))
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }

                            if (errors != null)
                                error = errors;

                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: If the [console] output and error
                        //       handles are the same, notify other
                        //       native and managed code that the
                        //       [console] error handle is [also]
                        //       no longer valid.
                        //
                        localError = null;

                        if ((errorHandle == outputHandle) && !SetHandle(
                                ChannelType.Error, IntPtr.Zero,
                                ref localError))
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }

                            if (errors != null)
                                error = errors;

                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        if (errors != null)
                            error = errors;

                        return ReturnCode.Error;
                    }
                }

                //
                // NOTE: If the [console] output and error
                //       handles are the same, we are already
                //       done; otherwise, we need to [re-]check
                //       and possibly close the error handle.
                //
                if (errorHandle == outputHandle)
                {
                    //
                    // NOTE: All handles cleaned up, success.
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    //
                    // NOTE: Does the [console] error handle
                    //       look like it needs to be closed?
                    //
                    if (NativeOps.IsValidHandle(errorHandle))
                    {
                        localError = null;

                        if (NativeOps.CloseHandle(
                                errorHandle, ref localError))
                        {
                            localError = null;

                            if (!SetHandle(
                                    ChannelType.Error, IntPtr.Zero,
                                    ref localError))
                            {
                                if (localError != null)
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(localError);
                                }

                                if (errors != null)
                                    error = errors;

                                return ReturnCode.Error;
                            }

                            //
                            // NOTE: All handles cleaned up, success.
                            //
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }

                            if (errors != null)
                                error = errors;

                            return ReturnCode.Error;
                        }
                    }
                }
            }
            else
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("output and error handles are invalid");
            }

            if (errors != null)
                error = errors;

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cleanup Support Methods
        /// <summary>
        /// This method closes all tracked console screen buffer handles,
        /// accumulating any errors that occur.
        /// </summary>
        /// <param name="stopOnError">
        /// Non-zero to stop processing at the first error encountered.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message (or list of error messages).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode CleanupScreenBuffers(
            bool stopOnError, /* in */
            ref Result error  /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                ReturnCode code = ReturnCode.Ok;
                ResultList errors = null;

                if (screenBuffers == null)
                    return code;

                foreach (KeyValuePair<string, IntPtr> pair in screenBuffers)
                {
                    string name = pair.Key;

                    if (SharedStringOps.SystemEquals(
                            name, savedActiveScreenName))
                    {
                        savedActiveScreenName = null;
                    }

                    IntPtr handle = pair.Value;

                    if (!NativeOps.IsValidHandle(handle))
                        continue;

                    Result localError = null;

                    if (!NativeOps.CloseHandle(handle, ref localError))
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        code = ReturnCode.Error;

                        if (stopOnError)
                        {
                            if (errors != null)
                                error = errors;

                            return code;
                        }
                    }
                }

                return code;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the saved active screen name and clears the stack
        /// of active console screen buffer names.
        /// </summary>
        /// <param name="error">
        /// Reserved for an error message.  This parameter is not used.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Ok" />.
        /// </returns>
        private static ReturnCode CleanupActiveScreenNames(
            ref Result error /* out: NOT USED */
            )
        {
            ResetActiveScreenNames();
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the primary console input and output handles and
        /// resets the standard handles, accumulating any errors that occur.
        /// </summary>
        /// <param name="stopOnError">
        /// Non-zero to stop processing at the first error encountered.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message (or list of error messages).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode CleanupHandles(
            bool stopOnError, /* in */
            ref Result error  /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                ReturnCode code = ReturnCode.Ok;
                ResultList errors = null;

                if (CloseHandles(
                        stopOnError, ref inputHandle, ref outputHandle,
                        ref errors) != ReturnCode.Ok)
                {
                    code = ReturnCode.Error;

                    if (stopOnError)
                    {
                        if (errors != null)
                            error = errors;

                        return code;
                    }
                }

                if (ResetHandles(
                        IntPtr.Zero, IntPtr.Zero, true, true,
                        stopOnError, ref error) != ReturnCode.Ok)
                {
                    code = ReturnCode.Error;

                    if (stopOnError)
                    {
                        if (errors != null)
                            error = errors;

                        return code;
                    }
                }

                return code;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally reports a console cleanup failure, both by
        /// emitting a trace message and (when appropriate for the current
        /// application domain) by complaining.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the failure.
        /// </param>
        /// <param name="result">
        /// The result (error message) associated with the failure.
        /// </param>
        private static void MaybeComplain(
            ReturnCode code, /* in */
            Result result    /* in */
            )
        {
            //
            // TODO: *HACK* Maybe come up with a better semantic here?  For
            //       now, we assume that complaining about console handles
            //       from a non-default AppDomain is a "bad idea" because it
            //       can be quite difficult to predict and/or prevent issues
            //       (e.g. AppDomain isolation in [test2], [interp], etc).
            //
            if (AppDomainOps.ShouldComplain())
                DebugOps.Complain(null, code, result);

            TraceOps.DebugTrace(String.Format(
                "MaybeComplain: code = {0}, result = {1}",
                code, FormatOps.WrapOrNull(true, true, result)),
                typeof(NativeConsole).Name, TracePriority.NativeError);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method registers the exit (or domain unload) event handler that
        /// cleans up native console resources, unless it has been disabled via
        /// configuration.
        /// </summary>
        private static void AddExitedEventHandler()
        {
            if (!GlobalConfiguration.DoesValueExist(
                    "No_NativeConsole_Exited",
                    ConfigurationFlags.NativeConsole))
            {
                AppDomain appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    if (!AppDomainOps.IsDefault(appDomain))
                    {
                        appDomain.DomainUnload -= NativeConsole_Exited;
                        appDomain.DomainUnload += NativeConsole_Exited;
                    }
                    else
                    {
                        appDomain.ProcessExit -= NativeConsole_Exited;
                        appDomain.ProcessExit += NativeConsole_Exited;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unregisters the exit (or domain unload) event handler
        /// that cleans up native console resources.
        /// </summary>
        private static void RemoveExitedEventHandler()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= NativeConsole_Exited;
                else
                    appDomain.ProcessExit -= NativeConsole_Exited;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the exit (or domain unload) event handler that cleans
        /// up all native console resources, including screen buffers, active
        /// screen names, handles, and the saved font.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments.
        /// </param>
        private static void NativeConsole_Exited(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            ReturnCode cleanupCode;
            Result cleanupError = null; /* REUSED */

            cleanupCode = CleanupScreenBuffers(false, ref cleanupError);

            if (cleanupCode != ReturnCode.Ok)
                MaybeComplain(cleanupCode, cleanupError);

            cleanupError = null;

            cleanupCode = CleanupActiveScreenNames(ref cleanupError);

            if (cleanupCode != ReturnCode.Ok)
                MaybeComplain(cleanupCode, cleanupError);

            cleanupError = null;

            cleanupCode = CleanupHandles(false, ref cleanupError);

            if (cleanupCode != ReturnCode.Ok)
                MaybeComplain(cleanupCode, cleanupError);

            cleanupError = null;

            cleanupCode = CleanupFont(true, ref cleanupError);

            if (cleanupCode != ReturnCode.Ok)
                MaybeComplain(cleanupCode, cleanupError);

            RemoveExitedEventHandler();
        }
        #endregion
    }
}
