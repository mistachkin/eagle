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

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("7b199e66-8290-4cdc-8312-d02de62683c5")]
    internal static class NativeConsole
    {
        #region Private Constants
        private static bool DefaultNativeHandle = true; /* IsMono(); */

        ///////////////////////////////////////////////////////////////////////

        private static readonly char[] NativeNewLine = { '\r', '\n' };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the native console
        //       input and output handles managed by this class (below).
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *SPECIAL* This is used (with interlocked operations) to
        //       synchronize access to the "isDisabled" field (below).
        //
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
        private static bool? isDisabled;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is either zero or the native console input handle
        //       returned via CreateFile for "CONIN$".  When non-zero,
        //       this is considered to be the "primary" console input
        //       buffer.
        //
        private static IntPtr inputHandle = IntPtr.Zero;

        //
        // NOTE: This is either zero or the native console output handle
        //       returned via CreateFile for "CONOUT$".  When non-zero,
        //       this is considered to be the "primary" console screen
        //       buffer.
        //
        private static IntPtr outputHandle = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is either null or the name of the most recently saved
        //       console screen buffer.  Changing the current console screen
        //       buffer will always reset this value.  If this value is null,
        //       the "primary" console screen buffer will be the one that is
        //       reverted back to.
        //
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
        private static Stack<string> activeScreenNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This will contain all created console screen buffers, if any,
        //       except the primary console screen buffer.
        //
        private static IntPtrDictionary screenBuffers;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Setting this value to non-null will either force the native
        //       console window to be locked open -OR- prevent it from being
        //       forcibly locked open.
        //
        private static bool? forcePreventClose = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the saved console font.  The console font should be
        //       saved prior to changing it.  Also, it should be restored if
        //       this class is being unloaded, for whatever reason.
        //
        private static UNM.CONSOLE_FONT_INFOEX? savedConsoleFontEx = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the per-thread character buffer for writing to the
        //       native console.  Its use is merely an optimization.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static char[] consoleWriteBuffer = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6c20c88a-dd55-4e35-a5a5-ec288041c8f4")]
        internal static class UnsafeNativeMethods
        {
            //
            // NOTE: Console input modes.
            //
            internal const uint ENABLE_MOUSE_INPUT = 0x10;

            //
            // NOTE: Console output modes.
            //
            //internal const uint ENABLE_PROCESSED_OUTPUT = 0x01;

            //
            // NOTE: Win32 error numbers.
            //
            internal const int NO_ERROR = 0;
            internal const int ERROR_INVALID_HANDLE = 6;

            //
            // NOTE: Values returned by GetFileType.
            //
            internal const uint FILE_TYPE_UNKNOWN = 0x0;
            internal const uint FILE_TYPE_DISK = 0x1;
            internal const uint FILE_TYPE_CHAR = 0x2;
            internal const uint FILE_TYPE_PIPE = 0x3;
            internal const uint FILE_TYPE_REMOTE = 0x8000;

            //
            // NOTE: Console handles.
            //
            internal const int STD_INPUT_HANDLE = -10;
            internal const int STD_OUTPUT_HANDLE = -11;
            internal const int STD_ERROR_HANDLE = -12;

            //
            // NOTE: Font family constants.
            //
            internal const uint FF_DONTCARE = 0x00;
            internal const uint FF_ROMAN = 0x10;
            internal const uint FF_SWISS = 0x20;
            internal const uint FF_MODERN = 0x30;
            internal const uint FF_SCRIPT = 0x40;
            internal const uint FF_DECORATIVE = 0x50;

            //
            // NOTE: Text metric pitch and family constants.
            //
            internal const uint TMPF_NONE = 0x00;
            internal const uint TMPF_FIXED_PITCH = 0x01; /* variable pitch */
            internal const uint TMPF_VECTOR = 0x02;
            internal const uint TMPF_TRUETYPE = 0x04;
            internal const uint TMPF_DEVICE = 0x08;

            //
            // NOTE: Per MSDN, besides having their own bits set, both
            //       TrueType and PostScript fonts set the TMPF_VECTOR
            //       bit as well.
            //
            internal const uint TMPF_TRUETYPE_VECTOR =
                TMPF_VECTOR | TMPF_TRUETYPE;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Special console file names.
            //
            internal const string ConsoleInputFileName = "CONIN$";
            internal const string ConsoleOutputFileName = "CONOUT$";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Special process id.
            //
            internal const int ATTACH_PARENT_PROCESS = -1;

            ///////////////////////////////////////////////////////////////////

            internal const uint HISTORY_NONE = 0;
            internal const uint HISTORY_NO_DUP_FLAG = 1;

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [ObjectId("615b15c1-b994-40cf-8948-a85dcafd9ee1")]
            internal struct CONSOLE_FONT_INFOEX
            {
                public uint cbSize;
                public uint nFont;
                public COORD dwFontSize;
                public uint FontFamily;
                public uint FontWeight;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string FaceName;
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("cfd3c6be-0c16-4599-8ae8-e2e513daa5f4")]
            internal struct COORD
            {
                public /* SHORT */ short X;
                public /* SHORT */ short Y;
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("b3bbba22-17eb-49c6-b030-7419de0b4490")]
            internal struct SECURITY_ATTRIBUTES
            {
                public /* DWORD */ uint nLength;
                public /* LPVOID */ IntPtr lpSecurityDescriptor;
                public /* BOOL */ bool bInheritHandle;
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("134a8f50-32fe-4789-bc18-8c37c27ab391")]
            internal struct CONSOLE_HISTORY_INFO
            {
                public /* UINT */ uint cbSize;
                public /* UINT */ uint HistoryBufferSize;
                public /* UINT */ uint NumberOfHistoryBuffers;
                public /* DWORD */ uint dwFlags;
            }

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("16757437-8f5f-4550-b986-6406b5954705")]
            internal struct SMALL_RECT
            {
                public /* SHORT */ short Left;
                public /* SHORT */ short Top;
                public /* SHORT */ short Right;
                public /* SHORT */ short Bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("9b96c63e-606d-4b1e-8be7-0945aa7da03a")]
            internal struct CONSOLE_SCREEN_BUFFER_INFO
            {
                public COORD dwSize;
                public COORD dwCursorPosition;
                public /* WORD */ short wAttributes;
                public SMALL_RECT srWindow;
                public COORD dwMaximumWindowSize;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern uint GetFileType(IntPtr handle);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleOutputCP(uint codePageID);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleCP(uint codePageID);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleFont(
                IntPtr handle, uint fontIndex
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCurrentConsoleFontEx(
                IntPtr handle,
                [MarshalAs(UnmanagedType.Bool)] bool maximumWindow,
                ref CONSOLE_FONT_INFOEX consoleFontEx
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetCurrentConsoleFontEx(
                IntPtr handle,
                [MarshalAs(UnmanagedType.Bool)] bool maximumWindow,
                ref CONSOLE_FONT_INFOEX consoleFontEx
            );

            ///////////////////////////////////////////////////////////////////

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

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleMode(
                IntPtr handle, ref uint mode
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern uint GetConsoleProcessList(
                uint[] ids, uint count
            );

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
             /* UNDOCUMENTED */
             [DllImport(DllName.Kernel32,
                 CallingConvention = CallingConvention.Winapi,
                 SetLastError = true)]
             internal static extern IntPtr GetConsoleInputWaitHandle();
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleMode(
                IntPtr handle,
                uint mode
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GenerateConsoleCtrlEvent(
                ControlEvent controlEvent,
                uint processGroupId
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr GetStdHandle(int nStdHandle);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern COORD GetLargestConsoleWindowSize(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

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

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetStdHandle(
                int nStdHandle,
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FlushConsoleInputBuffer(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AttachConsole(int processId);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AllocConsole();

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeConsole();

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleHistoryInfo(
                ref CONSOLE_HISTORY_INFO consoleHistoryInfo /* out */
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleHistoryInfo(
                ref CONSOLE_HISTORY_INFO consoleHistoryInfo /* in */
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetForegroundWindow();

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetFocus();

            ///////////////////////////////////////////////////////////////////

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
        private static bool IsDisabled()
        {
            return GlobalConfiguration.DoesValueExist(
                EnvVars.NoNativeConsole, ConfigurationFlags.NativeConsole);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Support Methods for [host screen] Sub-Command
        public static bool HaveActiveScreenName()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((activeScreenNames != null) &&
                    (activeScreenNames.Count > 0));
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
                            activeScreenNames = new Stack<string>();

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

        private static bool HaveScreenBuffer()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((screenBuffers != null) &&
                    (screenBuffers.Count > 0));
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

                HostOps.ResetAllInterpreterStandardOutputChannels();
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
        private static bool HasWindowFocus(
            IClientData clientData, /* in: NOT USED */
            ref Result error        /* out */
            )
        {
            IntPtr hWnd = IntPtr.Zero; /* NOT USED */

            return HasWindowFocus(ref hWnd, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static ReturnCode SimulateKeyboardString(
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

            CheckCancelCallback cancelCallback =
                new CheckCancelCallback(HasWindowFocus);

            return NativeOps.SimulateKeyboardString(
                cancelCallback, stringCallback, clientData,
                value, milliseconds, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static bool IsOpen(
            ref IntPtr handle /* out */
            )
        {
            handle = GetWindow();
            return (handle != IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr GetWindow()
        {
            try
            {
                return UNM.GetConsoleWindow();
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(NativeConsole).Name,
                    TracePriority.NativeError);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

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
                        // HACK: At this point, the local handle may be
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
                        // HACK: At this point, the local handle may be
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

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsOpen()
        {
            return GetWindow() != IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetWindow(
            ref Result error /* out */
            )
        {
            try
            {
                return UNM.GetConsoleWindow();
            }
            catch (Exception e)
            {
                error = e;
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode WriteLine(
            ref Result error /* out */
            )
        {
            return WriteString<char[]>(NativeNewLine, false, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Console Other Support Methods
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

        private static ReturnCode CleanupActiveScreenNames(
            ref Result error /* out: NOT USED */
            )
        {
            ResetActiveScreenNames();
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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
