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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;

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

namespace Eagle._Components.Private
{
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
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("317a3846-f0c5-4da6-89bc-185a51bb7015")]
        internal static class SafeNativeMethods
        {
#if WINDOWS
            #region Windows Data
            //
            // HACK: This is purposely not read-only.
            //
            internal static ControlEvent ConsoleControlEvent = ControlEvent.CTRL_C_EVENT;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetCurrentProcess();

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetCurrentThread();

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetCurrentProcessId();

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetCurrentThreadId();

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsDebuggerPresent();
            #endregion
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
            #region Unix Signal Constants
            internal const int SIGHUP = 1;
            internal const int SIGINT = 2;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Signal Data
            //
            // HACK: This is purposely not read-only.
            //
            internal static int ConsoleSignal = SIGINT;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Signal Methods
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl,
                SetLastError = true)]
            internal static extern int raise(int sig);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Threading Methods
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr pthread_self();

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_main_np();
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6dc268be-697f-41a1-98a2-be2ce602bdfe")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            #region Windows Dynamic Loading Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern uint GetModuleFileName(IntPtr module, IntPtr fileName, uint size);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(string fileName);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetDllDirectory(string directory);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr LoadLibrary(string fileName);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeLibrary(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi, even on Unicode OS. */
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr GetProcAddress(IntPtr module, string name);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Message Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern uint FormatMessage(
                FormatMessageFlags flags, IntPtr source, uint messageId, uint languageId,
                ref IntPtr buffer, uint size, IntPtr arguments);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr LocalFree(IntPtr handle);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Mutex Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr CreateMutex(IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string name);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Logging Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void OutputDebugString(string outputString);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Event Methods
            #region Dead Code
#if DEAD_CODE
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetEvent(IntPtr handle);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ResetEvent(IntPtr handle);
#endif
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Keyboard Constants
            internal const string VirtualKeyCodePrefix = "VK_";
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Keyboard Enumerations
            [ObjectId("1f0ab632-90bc-4572-98fc-a497a5f7354a")]
            internal enum VirtualKeyCode : byte
            {
                VK_LBUTTON = 0x01,
                VK_RBUTTON = 0x02,
                VK_CANCEL = 0x03,
                VK_MBUTTON = 0x04,
                VK_XBUTTON1 = 0x05,
                VK_XBUTTON2 = 0x06,
                VK_BACK = 0x08,
                VK_TAB = 0x09,
                VK_CLEAR = 0x0C,
                VK_RETURN = 0x0D,
                VK_SHIFT = 0x10,
                VK_CONTROL = 0x11,
                VK_MENU = 0x12,
                VK_PAUSE = 0x13,
                VK_CAPITAL = 0x14,
                VK_KANA = 0x15,
                VK_HANGUL = 0x15,
                VK_IME_ON = 0x16,
                VK_JUNJA = 0x17,
                VK_FINAL = 0x18,
                VK_HANJA = 0x19,
                VK_KANJI = 0x19,
                VK_IME_OFF = 0x1A,
                VK_ESCAPE = 0x1B,
                VK_CONVERT = 0x1C,
                VK_NONCONVERT = 0x1D,
                VK_ACCEPT = 0x1E,
                VK_MODECHANGE = 0x1F,
                VK_SPACE = 0x20,
                VK_PRIOR = 0x21,
                VK_NEXT = 0x22,
                VK_END = 0x23,
                VK_HOME = 0x24,
                VK_LEFT = 0x25,
                VK_UP = 0x26,
                VK_RIGHT = 0x27,
                VK_DOWN = 0x28,
                VK_SELECT = 0x29,
                VK_PRINT = 0x2A,
                VK_EXECUTE = 0x2B,
                VK_SNAPSHOT = 0x2C,
                VK_INSERT = 0x2D,
                VK_DELETE = 0x2E,
                VK_HELP = 0x2F,
                VK_0 = 0x30,
                VK_1 = 0x31,
                VK_2 = 0x32,
                VK_3 = 0x33,
                VK_4 = 0x34,
                VK_5 = 0x35,
                VK_6 = 0x36,
                VK_7 = 0x37,
                VK_8 = 0x38,
                VK_9 = 0x39,
                VK_A = 0x41,
                VK_B = 0x42,
                VK_C = 0x43,
                VK_D = 0x44,
                VK_E = 0x45,
                VK_F = 0x46,
                VK_G = 0x47,
                VK_H = 0x48,
                VK_I = 0x49,
                VK_J = 0x4A,
                VK_K = 0x4B,
                VK_L = 0x4C,
                VK_M = 0x4D,
                VK_N = 0x4E,
                VK_O = 0x4F,
                VK_P = 0x50,
                VK_Q = 0x51,
                VK_R = 0x52,
                VK_S = 0x53,
                VK_T = 0x54,
                VK_U = 0x55,
                VK_V = 0x56,
                VK_W = 0x57,
                VK_X = 0x58,
                VK_Y = 0x59,
                VK_Z = 0x5A,
                VK_LWIN = 0x5B,
                VK_RWIN = 0x5C,
                VK_APPS = 0x5D,
                VK_SLEEP = 0x5F,
                VK_NUMPAD0 = 0x60,
                VK_NUMPAD1 = 0x61,
                VK_NUMPAD2 = 0x62,
                VK_NUMPAD3 = 0x63,
                VK_NUMPAD4 = 0x64,
                VK_NUMPAD5 = 0x65,
                VK_NUMPAD6 = 0x66,
                VK_NUMPAD7 = 0x67,
                VK_NUMPAD8 = 0x68,
                VK_NUMPAD9 = 0x69,
                VK_MULTIPLY = 0x6A,
                VK_ADD = 0x6B,
                VK_SEPARATOR = 0x6C,
                VK_SUBTRACT = 0x6D,
                VK_DECIMAL = 0x6E,
                VK_DIVIDE = 0x6F,
                VK_F1 = 0x70,
                VK_F2 = 0x71,
                VK_F3 = 0x72,
                VK_F4 = 0x73,
                VK_F5 = 0x74,
                VK_F6 = 0x75,
                VK_F7 = 0x76,
                VK_F8 = 0x77,
                VK_F9 = 0x78,
                VK_F10 = 0x79,
                VK_F11 = 0x7A,
                VK_F12 = 0x7B,
                VK_F13 = 0x7C,
                VK_F14 = 0x7D,
                VK_F15 = 0x7E,
                VK_F16 = 0x7F,
                VK_F17 = 0x80,
                VK_F18 = 0x81,
                VK_F19 = 0x82,
                VK_F20 = 0x83,
                VK_F21 = 0x84,
                VK_F22 = 0x85,
                VK_F23 = 0x86,
                VK_F24 = 0x87,
                VK_NAVIGATION_VIEW = 0x88,
                VK_NAVIGATION_MENU = 0x89,
                VK_NAVIGATION_UP = 0x8A,
                VK_NAVIGATION_DOWN = 0x8B,
                VK_NAVIGATION_LEFT = 0x8C,
                VK_NAVIGATION_RIGHT = 0x8D,
                VK_NAVIGATION_ACCEPT = 0x8E,
                VK_NAVIGATION_CANCEL = 0x8F,
                VK_NUMLOCK = 0x90,
                VK_SCROLL = 0x91,
                VK_OEM_NEC_EQUAL = 0x92,
                VK_OEM_FJ_JISHO = 0x92,
                VK_OEM_FJ_MASSHOU = 0x93,
                VK_OEM_FJ_TOUROKU = 0x94,
                VK_OEM_FJ_LOYA = 0x95,
                VK_OEM_FJ_ROYA = 0x96,
                VK_LSHIFT = 0xA0,
                VK_RSHIFT = 0xA1,
                VK_LCONTROL = 0xA2,
                VK_RCONTROL = 0xA3,
                VK_LMENU = 0xA4,
                VK_RMENU = 0xA5,
                VK_BROWSER_BACK = 0xA6,
                VK_BROWSER_FORWARD = 0xA7,
                VK_BROWSER_REFRESH = 0xA8,
                VK_BROWSER_STOP = 0xA9,
                VK_BROWSER_SEARCH = 0xAA,
                VK_BROWSER_FAVORITES = 0xAB,
                VK_BROWSER_HOME = 0xAC,
                VK_VOLUME_MUTE = 0xAD,
                VK_VOLUME_DOWN = 0xAE,
                VK_VOLUME_UP = 0xAF,
                VK_MEDIA_NEXT_TRACK = 0xB0,
                VK_MEDIA_PREV_TRACK = 0xB1,
                VK_MEDIA_STOP = 0xB2,
                VK_MEDIA_PLAY_PAUSE = 0xB3,
                VK_LAUNCH_MAIL = 0xB4,
                VK_LAUNCH_MEDIA_SELECT = 0xB5,
                VK_LAUNCH_APP1 = 0xB6,
                VK_LAUNCH_APP2 = 0xB7,
                VK_OEM_1 = 0xBA,
                VK_OEM_PLUS = 0xBB,
                VK_OEM_COMMA = 0xBC,
                VK_OEM_MINUS = 0xBD,
                VK_OEM_PERIOD = 0xBE,
                VK_OEM_2 = 0xBF,
                VK_OEM_3 = 0xC0,
                VK_ABNT_C1 = 0xC1,
                VK_ABNT_C2 = 0xC2,
                VK_GAMEPAD_A = 0xC3,
                VK_GAMEPAD_B = 0xC4,
                VK_GAMEPAD_X = 0xC5,
                VK_GAMEPAD_Y = 0xC6,
                VK_GAMEPAD_RIGHT_SHOULDER = 0xC7,
                VK_GAMEPAD_LEFT_SHOULDER = 0xC8,
                VK_GAMEPAD_LEFT_TRIGGER = 0xC9,
                VK_GAMEPAD_RIGHT_TRIGGER = 0xCA,
                VK_GAMEPAD_DPAD_UP = 0xCB,
                VK_GAMEPAD_DPAD_DOWN = 0xCC,
                VK_GAMEPAD_DPAD_LEFT = 0xCD,
                VK_GAMEPAD_DPAD_RIGHT = 0xCE,
                VK_GAMEPAD_MENU = 0xCF,
                VK_GAMEPAD_VIEW = 0xD0,
                VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1,
                VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2,
                VK_GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3,
                VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4,
                VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5,
                VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6,
                VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7,
                VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8,
                VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9,
                VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA,
                VK_OEM_4 = 0xDB,
                VK_OEM_5 = 0xDC,
                VK_OEM_6 = 0xDD,
                VK_OEM_7 = 0xDE,
                VK_OEM_8 = 0xDF,
                VK_OEM_AX = 0xE1,
                VK_OEM_102 = 0xE2,
                VK_ICO_HELP = 0xE3,
                VK_ICO_00 = 0xE4,
                VK_PROCESSKEY = 0xE5,
                VK_ICO_CLEAR = 0xE6,
                VK_PACKET = 0xE7,
                VK_OEM_RESET = 0xE9,
                VK_OEM_JUMP = 0xEA,
                VK_OEM_PA1 = 0xEB,
                VK_OEM_PA2 = 0xEC,
                VK_OEM_PA3 = 0xED,
                VK_OEM_WSCTRL = 0xEE,
                VK_OEM_CUSEL = 0xEF,
                VK_OEM_ATTN = 0xF0,
                VK_OEM_FINISH = 0xF1,
                VK_OEM_COPY = 0xF2,
                VK_OEM_AUTO = 0xF3,
                VK_OEM_ENLW = 0xF4,
                VK_OEM_BACKTAB = 0xF5,
                VK_ATTN = 0xF6,
                VK_CRSEL = 0xF7,
                VK_EXSEL = 0xF8,
                VK_EREOF = 0xF9,
                VK_PLAY = 0xFA,
                VK_ZOOM = 0xFB,
                VK_NONAME = 0xFC,
                VK_PA1 = 0xFD,
                VK_OEM_CLEAR = 0xFE
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [Flags()]
            [ObjectId("5b1663e9-d742-4655-84f6-27cef9fd2fbf")]
            internal enum KeyEventFlags : byte
            {
                KEYEVENTF_NONE = 0x0,
                KEYEVENTF_EXTENDEDKEY = 0x1,
                KEYEVENTF_KEYUP = 0x2,
                KEYEVENTF_UNICODE = 0x4,
                KEYEVENTF_SCANCODE = 0x8
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [ObjectId("18354957-7345-472e-b041-f8d2d6532ede")]
            internal enum VirtualKeyMapType : uint
            {
                MAPVK_VK_TO_VSC = 0,
                MAPVK_VSC_TO_VK = 1,
                MAPVK_VK_TO_CHAR = 2,
                MAPVK_VSC_TO_VK_EX = 3,
                MAPVK_VK_TO_VSC_EX = 4
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Keyboard Methods
            [DllImport(DllName.User32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern uint MapVirtualKey(
                uint value,
                VirtualKeyMapType mapType
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

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
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CancelSynchronousIo(IntPtr thread);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr handle);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Process Methods
            /* NOTE: Needed for use with NtQueryInformationProcess. */
            internal const uint PROCESS_QUERY_INFORMATION = 0x400;

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetProcessAffinityMask(IntPtr process,
                ref IntPtr processAffinityMask, ref IntPtr systemAffinityMask);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [ObjectId("07983ce3-bac0-48af-b98b-b88eac97cc08")]
            internal enum PROCESSINFOCLASS
            {
                ProcessBasicInformation
                // ...
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("bc739fdb-8d62-482d-a529-ba64bbfa388d")]
            internal struct PROCESS_BASIC_INFORMATION
            {
                public /* NTSTATUS */ int ExitStatus;
                public /* PPEB */ IntPtr PebBaseAddress;
                public /* KAFFINITY */ IntPtr AffinityMask;
                public /* KPRIORITY */ int BasePriority;
                public /* HANDLE */ IntPtr UniqueProcessId;
                public /* HANDLE */ IntPtr InheritedFromUniqueProcessId;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern int NtQueryInformationProcess(
                /* HANDLE */ IntPtr process,
                PROCESSINFOCLASS processInformationClass,
                /* PVOID */ ref PROCESS_BASIC_INFORMATION processInformation,
                /* ULONG */ uint processInformationLength,
                /* PULONG */ ref uint returnLength
            );
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Threading Methods
            /* NOTE: Needed for use with QueueUserAPC. */
            internal const uint THREAD_SET_CONTEXT = 0x10;

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr OpenProcess(uint desiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr OpenThread(uint desiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint threadId);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint QueueUserAPC(ApcCallback proc, IntPtr thread, IntPtr data);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Memory Constants
            internal const uint HEAP_NONE = 0x00000000;
            internal const uint HEAP_CREATE_ENABLE_EXECUTE = 0x00040000;
            internal const uint HEAP_GENERATE_EXCEPTIONS = 0x00000004;
            internal const uint HEAP_NO_SERIALIZE = 0x00000001;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Memory Methods
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("ad38df98-9796-41ee-99b0-8543b52ca6a7")]
            internal struct MEMORYSTATUSEX
            {
                public uint dwLength;
                public uint dwMemoryLoad;
                public ulong ullTotalPhys;
                public ulong ullAvailPhys;
                public ulong ullTotalPageFile;
                public ulong ullAvailPageFile;
                public ulong ullTotalVirtual;
                public ulong ullAvailVirtual;
                public ulong ullAvailExtendedVirtual;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.NtDll, CallingConvention = CallingConvention.Winapi)]
            internal static extern void RtlZeroMemory(IntPtr pMemory, UIntPtr size);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX memoryStatus);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr HeapCreate(
                uint flags, UIntPtr initialSize, UIntPtr maximumSize
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern UIntPtr HeapCompact(IntPtr heap, uint flags);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool HeapDestroy(IntPtr heap);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Constants
            internal const int ERROR_ACCESS_DENIED = 5;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Methods
            [Flags()]
            [ObjectId("b55212c7-b9ff-4812-a08b-b9fadcd864d1")]
            internal enum SystemErrorMode : uint
            {
                SEM_ERROR = 0xFFFF,
                SEM_NONE = 0x0000,

                SEM_FAILCRITICALERRORS = 0x0001,
                SEM_NOGPFAULTERRORBOX = 0x0002,
                SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
                SEM_NOOPENFILEERRORBOX = 0x8000
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern SystemErrorMode GetErrorMode();

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern SystemErrorMode SetErrorMode(SystemErrorMode mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

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
            internal static UIntPtr _TRUNCATE = new UIntPtr(
                (IntPtr.Size >= sizeof(ulong)) ? ConversionOps.ToULong(-1) :
                ConversionOps.ToUInt(-1));
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows String Formatting Methods
#if MONO || MONO_HACKS
            [DllImport(DllName.MsVcRt, EntryPoint = "_snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int msvc_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);
#endif

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
            internal static int SYS_gettid_i386 = 224;
            internal static int SYS_gettid_IA64 = 1105;
            internal static int SYS_gettid_AMD64 = 186;
            internal static int SYS_gettid_ARM = 224;
            internal static int SYS_gettid_ARM64 = 178;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Constants
            //
            // BUGBUG: These values are probably only portable to Linux.
            //
            internal const int RTLD_LAZY = 0x1;     /* Bind function calls lazily */
            internal const int RTLD_NOW = 0x2;      /* Bind function calls immediately */
            internal const int RTLD_GLOBAL = 0x100; /* Make symbols globally available */
            internal const int RTLD_LOCAL = 0x000;  /* Opposite of RTLD_GLOBAL, and the default */
            internal const int RTLD_DEFMODE = RTLD_NOW | RTLD_GLOBAL;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // BUGBUG: These values are probably only portable to Linux.
            //
            internal static readonly IntPtr RTLD_DEFAULT = IntPtr.Zero; /* Global symbol search */
            internal static readonly IntPtr RTLD_NEXT = new IntPtr(-1); /* Get Next symbol */
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
            [DllImport(DllName.Internal, EntryPoint = "dlopen",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libc_dlopen(string fileName, int mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal, EntryPoint = "dlclose",
                CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            internal static extern int libc_dlclose(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.Internal, EntryPoint = "dlsym",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libc_dlsym(IntPtr module, string name);

            ///////////////////////////////////////////////////////////////////////////////////////////

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
            [DllImport(DllName.LibDL, EntryPoint = "dlopen",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libdl_dlopen(string fileName, int mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibDL, EntryPoint = "dlclose",
                CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            internal static extern int libdl_dlclose(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.LibDL, EntryPoint = "dlsym",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libdl_dlsym(IntPtr module, string name);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibDL, EntryPoint = "dlerror",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr libdl_dlerror();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Process Methods
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int getppid();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Threading Methods
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_threadid_np(IntPtr thread, ref ulong tid);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Memory Methods
            [DllImport(DllName.LibC,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern void memset(IntPtr pMemory, int value, UIntPtr size);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix String Formatting Methods
#if MONO || MONO_HACKS || NET_STANDARD_20
            [DllImport(DllName.LibC, EntryPoint = "snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int ansi_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibC, EntryPoint = "snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int ansi_snprintf(StringBuilder buffer, UIntPtr count,
                string format, __arglist);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Call Methods
            [DllImport(DllName.Internal, EntryPoint = "syscall",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int syscall_int(int number);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Delegates (Private Static Data)
            internal static readonly object syncRoot = new object();

            internal static dlopen dlopen = null;
            internal static dlclose dlclose = null;
            internal static dlsym dlsym = null;
            internal static dlerror dlerror = null;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Logging Constants
            internal const int LOG_EMERG = 0;          /* system is unusable */
            internal const int LOG_ALERT = 1;          /* action must be taken immediately */
            internal const int LOG_CRIT = 2;           /* critical conditions */
            internal const int LOG_ERR = 3;            /* error conditions */
            internal const int LOG_WARNING = 4;        /* warning conditions */
            internal const int LOG_NOTICE = 5;         /* normal but significant condition */
            internal const int LOG_INFO = 6;           /* informational */
            internal const int LOG_DEBUG = 7;          /* debug-level messages */

            ///////////////////////////////////////////////////////////////////////////////////////////

            internal const int LOG_USER = (1 << 3);    /* random user-level messages */
            internal const int LOG_LOCAL0 = (16 << 3); /* reserved for local use */
            internal const int LOG_LOCAL1 = (17 << 3); /* reserved for local use */
            internal const int LOG_LOCAL2 = (18 << 3); /* reserved for local use */
            internal const int LOG_LOCAL3 = (19 << 3); /* reserved for local use */
            internal const int LOG_LOCAL4 = (20 << 3); /* reserved for local use */
            internal const int LOG_LOCAL5 = (21 << 3); /* reserved for local use */
            internal const int LOG_LOCAL6 = (22 << 3); /* reserved for local use */
            internal const int LOG_LOCAL7 = (23 << 3); /* reserved for local use */
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Logging Methods
            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.LibC, CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void syslog(int priority, string outputString, string argument);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibC, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr strerror(int error);
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
        private const int STATUS_SUCCESS = 0;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly IntPtr IntPtrOne = new IntPtr(1);

        ///////////////////////////////////////////////////////////////////////////////////////////////

#pragma warning disable 649 // NOTE: Yes, this is by design.
        private static readonly GCHandle invalidGCHandle;
#pragma warning restore 649

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        //
        // HACK: These are not read-only.
        //
        private static string MonoMemoryCategoryName = "Mono Memory";
        private static string MonoTotalPhysicalMemoryCounterName = "Total Physical Memory";
        private static string MonoAvailablePhysicalMemoryCounterName = "Available Physical Memory";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the static performance
        //       counters (below) that are [only] used to track physical memory
        //       on Mono.
        //
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool once = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These performance counters are only used on Mono for tracking
        //       available and total physical memory.
        //
#if !NET_STANDARD_20
        private static PerformanceCounter totalPhysicalMemoryCounter = null;
        private static PerformanceCounter availablePhysicalMemoryCounter = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Windows-Only Abstraction Methods (ALWAYS FAIL ON NON-WINDOWS)
#if WINDOWS
        public static StringList GetProcessorAffinityMasks()
        {
            ReturnCode code;
            StringList list = null;
            Result error = null;

            code = WindowsGetProcessorAffinityMasks(ref list, ref error);

            return (code == ReturnCode.Ok) ? list : new StringList(error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CloseHandle(
            IntPtr handle,   /* in */
            ref Result error /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    if (UnsafeNativeMethods.CloseHandle(handle)) /* throw */
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
                    IntPtr thread = UnsafeNativeMethods.OpenThread(
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
                    uint result = UnsafeNativeMethods.QueueUserAPC(
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
                            UnsafeNativeMethods.keybd_event(
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
                            UnsafeNativeMethods.keybd_event(
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
                    UnsafeNativeMethods.RtlZeroMemory(pMemory, new UIntPtr(size));
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
                    UnsafeNativeMethods.memset(pMemory, 0, new UIntPtr(size));
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
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Windows-Only Helper Methods
#if WINDOWS
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

            string prefix =
                UnsafeNativeMethods.VirtualKeyCodePrefix;

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

        private static void TranslateVirtualKeyCode(
            VirtualKeyCode keyCode,    /* in */
            bool press,                /* in */
            out byte scanCode,         /* out */
            out KeyEventFlags keyFlags /* out */
            )
        {
            VirtualKeyMapType mapType = VirtualKeyMapType.MAPVK_VK_TO_VSC;

            scanCode = (byte)UnsafeNativeMethods.MapVirtualKey(
                (uint)keyCode, mapType);

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
        private static void WindowsInitializeMemoryStatus(
            out UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus /* out */
            )
        {
            memoryStatus = new UnsafeNativeMethods.MEMORYSTATUSEX();

            memoryStatus.dwLength = (uint)Marshal.SizeOf(
                typeof(UnsafeNativeMethods.MEMORYSTATUSEX));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsGetMemoryStatus(
            ref UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return UnsafeNativeMethods.GlobalMemoryStatusEx(ref memoryStatus);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

                    if (UnsafeNativeMethods.GetProcessAffinityMask(
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

        private static bool WindowsCancelSynchronousIo(
            IntPtr thread /* in */
            )
        {
            try
            {
                return UnsafeNativeMethods.CancelSynchronousIo(thread);
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static IntPtr WindowsGetParentProcessId(
            IntPtr processId /* in */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (processId != IntPtr.Zero)
                {
                    handle = UnsafeNativeMethods.OpenProcess(
                        UnsafeNativeMethods.PROCESS_QUERY_INFORMATION, false,
                        ConversionOps.ToUInt(processId));

                    if (handle == IntPtr.Zero)
                        return IntPtr.Zero;
                }

                UnsafeNativeMethods.PROCESS_BASIC_INFORMATION processInformation =
                    new UnsafeNativeMethods.PROCESS_BASIC_INFORMATION();

                uint returnLength = 0;

                if (UnsafeNativeMethods.NtQueryInformationProcess(
                        (handle != IntPtr.Zero) ?
                            handle : SafeNativeMethods.GetCurrentProcess(),
                        UnsafeNativeMethods.PROCESSINFOCLASS.ProcessBasicInformation,
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
                        UnsafeNativeMethods.CloseHandle(handle); /* throw */
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

        private static UnsafeNativeMethods.SystemErrorMode WindowsGetErrorMode()
        {
            try
            {
                if (PlatformOps.IsWindowsVistaOrHigher())
                {
                    return UnsafeNativeMethods.GetErrorMode();
                }
                else
                {
                    return UnsafeNativeMethods.SetErrorMode(
                        UnsafeNativeMethods.SystemErrorMode.SEM_NONE);
                }
            }
            catch
            {
                // do nothing.
            }

            return UnsafeNativeMethods.SystemErrorMode.SEM_ERROR;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static UnsafeNativeMethods.SystemErrorMode WindowsSetErrorMode(
            UnsafeNativeMethods.SystemErrorMode mode /* in */
            )
        {
            try
            {
                return UnsafeNativeMethods.SetErrorMode(mode);
            }
            catch
            {
                // do nothing.
            }

            return UnsafeNativeMethods.SystemErrorMode.SEM_ERROR;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

                if (UnsafeNativeMethods.FormatMessage(
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
                    UnsafeNativeMethods.LocalFree(handle);
                    handle = IntPtr.Zero;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WindowsGetDynamicLoadingError(
            int error /* in */
            )
        {
            if (InitializeDynamicLoading())
                return GetErrorMessage(error);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static uint WindowsGetModuleFileName(
            IntPtr module,   /* in */
            IntPtr fileName, /* in */
            uint size        /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.GetModuleFileName(module, fileName, size);

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsGetModuleHandle(
            string fileName /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.GetModuleHandle(fileName);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsSetDllDirectory(
            string directory /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.SetDllDirectory(directory);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsLoadLibrary(
            string fileName /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.LoadLibrary(fileName);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsFreeLibrary(
            IntPtr module /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.FreeLibrary(module);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsGetProcAddress(
            IntPtr module, /* in */
            string name    /* in */
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.GetProcAddress(module, name);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsOutputDebugMessage(
            string message /* in */
            )
        {
            if (message != null)
            {
                try
                {
                    UnsafeNativeMethods.OutputDebugString(message);

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
        private static IntPtr MacintoshGetCurrentThreadId()
        {
            try
            {
                ulong tid = 0;

                if (UnsafeNativeMethods.pthread_threadid_np(
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

        private static IntPtr LinuxGetCurrentThreadId()
        {
            int number;

            switch (PlatformOps.GetProcessorArchitecture())
            {
                case ProcessorArchitecture.Intel:
                    number = UnsafeNativeMethods.SYS_gettid_i386;
                    break;
                case ProcessorArchitecture.ARM:
                    number = UnsafeNativeMethods.SYS_gettid_ARM;
                    break;
                case ProcessorArchitecture.IA64:
                    number = UnsafeNativeMethods.SYS_gettid_IA64;
                    break;
                case ProcessorArchitecture.AMD64:
                    number = UnsafeNativeMethods.SYS_gettid_AMD64;
                    break;
                case ProcessorArchitecture.ARM64:
                    number = UnsafeNativeMethods.SYS_gettid_ARM64;
                    break;
                default:
                    return IntPtr.Zero;
            }

            try
            {
                return new IntPtr(UnsafeNativeMethods.syscall_int(
                    number)); /* 2.4.11+ */
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static IntPtr UnixGetParentProcessId(
            IntPtr processId /* in: NOT USED */
            )
        {
            try
            {
                return new IntPtr(UnsafeNativeMethods.getppid());
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static bool UnixIsMainThread()
        {
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static string UnixGetErrorMessage(
            int error /* in */
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                handle = UnsafeNativeMethods.strerror(error);

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

        private static string UnixGetDynamicLoadingError(
            int error /* in: NOT USED */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlerror dlerror;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlerror = UnsafeNativeMethods.dlerror;
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

        private static bool UnixInitializeDynamicLoading()
        {
            lock (UnsafeNativeMethods.syncRoot)
            {
                if ((UnsafeNativeMethods.dlopen == null) ||
                    (UnsafeNativeMethods.dlclose == null) ||
                    (UnsafeNativeMethods.dlsym == null) ||
                    (UnsafeNativeMethods.dlerror == null))
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
                            module = UnsafeNativeMethods.libdl_dlopen(
                                null, UnsafeNativeMethods.RTLD_DEFMODE); /* throw */
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
                            if (UnsafeNativeMethods.dlopen == null)
                                UnsafeNativeMethods.dlopen = UnsafeNativeMethods.libdl_dlopen;

                            if (UnsafeNativeMethods.dlclose == null)
                                UnsafeNativeMethods.dlclose = UnsafeNativeMethods.libdl_dlclose;

                            if (UnsafeNativeMethods.dlsym == null)
                                UnsafeNativeMethods.dlsym = UnsafeNativeMethods.libdl_dlsym;

                            if (UnsafeNativeMethods.dlerror == null)
                                UnsafeNativeMethods.dlerror = UnsafeNativeMethods.libdl_dlerror;
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
                            if (UnsafeNativeMethods.dlopen == null)
                                UnsafeNativeMethods.dlopen = UnsafeNativeMethods.libc_dlopen;

                            if (UnsafeNativeMethods.dlclose == null)
                                UnsafeNativeMethods.dlclose = UnsafeNativeMethods.libc_dlclose;

                            if (UnsafeNativeMethods.dlsym == null)
                                UnsafeNativeMethods.dlsym = UnsafeNativeMethods.libc_dlsym;

                            if (UnsafeNativeMethods.dlerror == null)
                                UnsafeNativeMethods.dlerror = UnsafeNativeMethods.libc_dlerror;
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
                            if (UnsafeNativeMethods.libdl_dlclose(module) == 0)
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

        private static IntPtr UnixLoadLibrary(
            string fileName /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlopen dlopen;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlopen = UnsafeNativeMethods.dlopen;
                }

                if (dlopen != null)
                    return dlopen(fileName, UnsafeNativeMethods.RTLD_DEFMODE);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool UnixFreeLibrary(
            IntPtr module /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlclose dlclose;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlclose = UnsafeNativeMethods.dlclose;
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

        private static IntPtr UnixGetProcAddress(
            IntPtr module, /* in */
            string name    /* in */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlsym dlsym;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlsym = UnsafeNativeMethods.dlsym;
                }

                if (dlsym != null)
                    return dlsym(module, name);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool UnixOutputDebugMessage(
            string message /* in */
            )
        {
            if (message != null)
            {
                try
                {
                    UnsafeNativeMethods.syslog(
                        UnsafeNativeMethods.LOG_DEBUG,
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

        #region Private Mono Specific Methods (DO NOT CALL)
#if MONO || MONO_HACKS
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
                    returnValue += UnsafeNativeMethods.msvc_snprintf_double(buffer,
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
                    returnValue += UnsafeNativeMethods.ansi_snprintf_double(buffer,
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
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // NOTE: The .NET Core runtime does not appear to support
                    //       the "varargs" calling convention, as of the .NET
                    //       Standard 2.0 (i.e. we cannot use "__arglist").
                    //
                    returnValue += UnsafeNativeMethods.msvc_snprintf_s_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), UnsafeNativeMethods._TRUNCATE,
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
                    returnValue += UnsafeNativeMethods.ansi_snprintf_double(buffer,
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
                    returnValue += UnsafeNativeMethods.msvc_snprintf(buffer,
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
                    returnValue += UnsafeNativeMethods.ansi_snprintf(buffer,
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

        private static ReturnCode GetTotalMemory(
            ref ulong totalPhysical, /* out */
            ref Result error         /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus;

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

        private static ReturnCode GetMemoryLoad(
            ref uint memoryLoad, /* out */
            ref Result error     /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus;

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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        public static GCHandle GetInvalidGCHandle()
        {
            return invalidGCHandle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode PrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref Result error      /* out */
            )
        {
            int returnValue = 0;

            return PrintDouble(buffer, format, value, ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool IsValidHandle(
            IntPtr handle /* in */
            )
        {
            return RuntimeOps.IsValidHandle(handle);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsValidHandle(
            IntPtr handle,   /* in */
            ref bool invalid /* out */
            )
        {
            return RuntimeOps.IsValidHandle(handle, ref invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static IntPtr GetCurrentThread()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetCurrentThread();
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static string MaybeGetErrorMessage()
        {
            int lastError = Marshal.GetLastWin32Error();

            if (lastError == 0)
                return null;

            return GetErrorMessage(lastError);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetErrorMessage()
        {
            return GetErrorMessage(Marshal.GetLastWin32Error());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool OutputDebugMessage(
            string message /* in */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsOutputDebugMessage(message);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixOutputDebugMessage(message);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static ReturnCode GetMemoryStatus(
            ref StringList list, /* out */
            ref Result error     /* out */
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus;

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
