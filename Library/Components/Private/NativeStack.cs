/*
 * NativeStack.cs --
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

#if WINDOWS || UNIX
using System.Runtime.InteropServices;
#endif

using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public.Delegates;
using Eagle._Components.Public;

#if WINDOWS || UNIX
using Eagle._Constants;
#endif

using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

/////////////////////////////////////////////////////////////////////////////////////////
// NATIVE STACK HANDLING
/////////////////////////////////////////////////////////////////////////////////////////

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("0c681582-7e68-4e41-88c8-0891f10cd484")]
    internal static class NativeStack
    {
        /////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        /////////////////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("92cecfd9-3ef3-42c1-83e2-055cd6f9dbfe")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            [ObjectId("643773c8-cf43-4d74-9559-35ce377d86f5")]
            internal enum THREADINFOCLASS
            {
                ThreadBasicInformation
                // ...
            }

            /////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("d5537405-4611-48b8-a2d4-f588f4001fcb")]
            internal struct CLIENT_ID
            {
                public /* PVOID */ IntPtr UniqueProcess;
                public /* PVOID */ IntPtr UniqueThread;
            }

            /////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("e9663a89-68cd-420e-a37e-4058e1b8e6ea")]
            internal struct THREAD_BASIC_INFORMATION
            {
                public /* NTSTATUS */ int ExitStatus;
                public /* PVOID */ IntPtr TebBaseAddress;
                public CLIENT_ID ClientId;
                public /* KAFFINITY */ IntPtr AffinityMask;
                public /* KPRIORITY */ int Priority;
                public /* KPRIORITY */ int BasePriority;
            }

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetThreadContext(
                /* HANDLE */ IntPtr thread,
                /* LPCONTEXT */ IntPtr context
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern int NtQueryInformationThread(
                /* HANDLE */ IntPtr thread,
                THREADINFOCLASS threadInformationClass,
                /* PVOID */ ref THREAD_BASIC_INFORMATION threadInformation,
                /* ULONG */ uint threadInformationLength,
                /* PULONG */ ref uint returnLength
            );
#endif

            /////////////////////////////////////////////////////////////////////////////

#if UNIX
            internal static readonly int RLIMIT_STACK = 3; /* Linux only? */

            /////////////////////////////////////////////////////////////////////////////

            internal static readonly UIntPtr RLIM_INFINITY = new UIntPtr(
                (UIntPtr.Size == sizeof(ulong)) ? ulong.MaxValue : uint.MaxValue);

            /////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("82c1c43a-7b4b-45fd-a8f9-ce2b2ec44444")]
            internal struct rlimit
            {
                public /* rlim_t */ UIntPtr rlim_cur;
                public /* rlim_t */ UIntPtr rlim_max;
            }

            /////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("b6f59db7-2220-4a71-9727-17cd281f0958")]
            internal struct pthread_attr_t
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
                public byte[] buffer;
            }

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int getrlimit(
                int resource,
                /* struct rlimit */ ref rlimit rlp
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr pthread_get_stackaddr_np( /* Darwin */
                /* pthread_t */ IntPtr thread
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr pthread_get_stacksize_np( /* Darwin */
                /* pthread_t */ IntPtr thread
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_attr_init(
                ref pthread_attr_t attr
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_getattr_np( /* Linux */
                /* pthread_t */ IntPtr thread,
                ref pthread_attr_t attr
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_attr_get_np( /* FreeBSD */
                /* pthread_t */ IntPtr thread,
                ref pthread_attr_t attr
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_attr_getstacksize(
                ref pthread_attr_t attr,
                /* size_t */ ref UIntPtr stacksize
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int pthread_attr_destroy(
                ref pthread_attr_t attr
            );
#endif
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Safe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("89610a3c-5b4d-4c75-b779-426e72615f9c")]
        private static class SafeNativeMethods
        {
#if WINDOWS
            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern IntPtr NtCurrentTeb();
#endif

            /////////////////////////////////////////////////////////////////////////////

#if UNIX
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr pthread_self();
#endif
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Stack Size Support Class
        [ObjectId("32de459b-ba4d-492b-8e5e-731f418e9ca1")]
        internal sealed class StackSize
        {
            #region Public Constructors
            public StackSize()
            {
                used = UIntPtr.Zero;
                allocated = UIntPtr.Zero;
                extra = UIntPtr.Zero;
                margin = UIntPtr.Zero;
                maximum = UIntPtr.Zero;

                reserve = UIntPtr.Zero;
                commit = UIntPtr.Zero;
            }

            /////////////////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
            public StackSize(
                StackSize stackSize
                )
                : this()
            {
                if (stackSize != null)
                {
                    used = stackSize.used;
                    allocated = stackSize.allocated;
                    extra = stackSize.extra;
                    margin = stackSize.margin;
                    maximum = stackSize.maximum;

                    reserve = stackSize.reserve;
                    commit = stackSize.commit;
                }
            }
#endif
            #endregion
            #endregion

            /////////////////////////////////////////////////////////////////////////////

            #region Public Data
            public UIntPtr used;
            public UIntPtr allocated;
            public UIntPtr extra;
            public UIntPtr margin;
            public UIntPtr maximum;

            public UIntPtr reserve;
            public UIntPtr commit;
            #endregion

            /////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return StringList.MakeList(
                    "used", used, "allocated", allocated, "extra", extra,
                    "margin", margin, "maximum", maximum, "reserve", reserve,
                    "commit", commit);
            }
            #endregion
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // NOTE: This is the successful value for the NTSTATUS data type.
        //
        private const int STATUS_SUCCESS = 0;

        //
        // NOTE: The minimum number of memory pages required for a thread before
        //       the margin pages value (below) will actually be used; otherwise,
        //       the margin pages value will be set to half the available stack
        //       pages.  The development web server used with Visual Studio 2012
        //       (IIS Express) apparently creates some threads with a very small
        //       stack (256K) and without this extra check, scripts could not be
        //       evaluated on those threads.
        //
        private const uint StackMinimumPages = 128; /* 512K on x86, 1024K on x64 */

        //
        // NOTE: The number of memory pages reserved by the script engine for
        //       our safety margin (i.e. "buffer zone").  This includes enough
        //       space to cause our stack overflow logic to always trigger prior
        //       to the .NET Framework itself throwing a StackOverflowException.
        //       This value may need fine tuning and is subject to change for
        //       every new release of the .NET Framework.
        //
        private const uint StackMarginPages = 96; /* 384K on x86, 768K on x64 */

        //
        // NOTE: The script engine fallback stack reserve for all threads created
        //       via Engine.CreateThread if a larger stack reserve is not specified
        //       in the PE file header.  This value must be kept in sync with the
        //       "EagleDefaultStackSize" value in the "Eagle.Settings.targets" file.
        //
        private const ulong DefaultStackSize = 0x1000000; // 16MB

        /////////////////////////////////////////////////////////////////////////////////

#if WINDOWS
        //
        // NOTE: Magic offsets (in bytes) into the "undocumented" NT TEB (Thread
        //       Environment Block) structure.  We need these because that is the
        //       only reliable way to get access to the currently available stack
        //       size for the current thread.  To get these values from WinDbg use:
        //
        //          dt ntdll!_TEB TebAddr StackBase
        //          dt ntdll!_TEB TebAddr StackLimit
        //          dt ntdll!_TEB TebAddr DeallocationStack
        //
        private const uint TebStackBaseOffset32Bit = 0x04;     /* VERIFIED */
        private const uint TebStackLimitOffset32Bit = 0x08;    /* VERIFIED */
        private const uint TebDeallocationStack32Bit = 0xE0C;  /* VERIFIED */

        private const uint TebStackBaseOffset64Bit = 0x08;     /* VERIFIED */
        private const uint TebStackLimitOffset64Bit = 0x10;    /* VERIFIED */
        private const uint TebDeallocationStack64Bit = 0x1478; /* VERIFIED */

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These constants are from the Platform SDK header file "WinNT.h" and
        //       are for use with the Win32 GetThreadContext API.
        //
        private const uint CONTEXT_i386 = 0x00010000;
        private const uint CONTEXT_IA64 = 0x00080000;
        private const uint CONTEXT_AMD64 = 0x00100000;
        private const uint CONTEXT_ARM = 0x00200000;
        private const uint CONTEXT_ARM64 = 0x00400000;

        private const uint CONTEXT_CONTROL_i386 = (CONTEXT_i386 | 0x00000001);
        private const uint CONTEXT_CONTROL_IA64 = (CONTEXT_IA64 | 0x00000001);
        private const uint CONTEXT_CONTROL_AMD64 = (CONTEXT_AMD64 | 0x00000001);
        private const uint CONTEXT_CONTROL_ARM = (CONTEXT_ARM | 0x00000001);
        private const uint CONTEXT_CONTROL_ARM64 = (CONTEXT_ARM64 | 0x00000001);

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are offsets into the architecture specific _CONTEXT structure
        //       from the Platform SDK header file "WinNT.h".  The "VERIFIED" comment
        //       indicates that the calculation has been double-checked on an actual
        //       running system via WinDbg.
        //
        private const uint CONTEXT_FLAGS_OFFSET_i386 = 0;      /* VERIFIED */
        private const uint CONTEXT_SIZE_i386 = 716;            /* VERIFIED */
        private const uint CONTEXT_ESP_OFFSET_i386 = 196;      /* VERIFIED */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_IA64 = 0;      /* VERIFIED */
        private const uint CONTEXT_SIZE_IA64 = 2672;           /* ???????? */
        private const uint CONTEXT_ESP_OFFSET_IA64 = 2248;     /* ???????? */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_AMD64 = 48;    /* VERIFIED */
        private const uint CONTEXT_SIZE_AMD64 = 1232;          /* VERIFIED */
        private const uint CONTEXT_ESP_OFFSET_AMD64 = 152;     /* VERIFIED */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_ARM = 0;       /* VERIFIED */
        private const uint CONTEXT_SIZE_ARM = 228;             /* ???????? */
        private const uint CONTEXT_ESP_OFFSET_ARM = 56;        /* ???????? */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_ARM64 = 0;     /* ???????? */
        private const uint CONTEXT_SIZE_ARM64 = 912;           /* ???????? */
        private const uint CONTEXT_ESP_OFFSET_ARM64 = 256;     /* ???????? */
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: Initially, this field is null to indicate that the status
        //       of the native stack checking support is unknown.  It will
        //       be set to non-zero if native stack checking is available
        //       on the platform.
        //
        private static bool? IsAvailable;

        /////////////////////////////////////////////////////////////////////////////////

        #region Platform Abstraction Delegates
        //
        // NOTE: These delegates are set to the private static methods in
        //       this class that provide the specified information for the
        //       current platform.
        //
#if WINDOWS || UNIX || UNSAFE
        private static NativeIsMainThreadCallback isMainThreadCallback;
        private static NativeStackCallback getNativeStackPointerCallback;
        private static NativeStackCallback getNativeStackAllocatedCallback;
        private static NativeStackCallback getNativeStackMaximumCallback;
#endif

        /////////////////////////////////////////////////////////////////////////////////

#if UNIX
        private static NativeStackCallback unixGetNativeStackMaximumCallback;
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region "Unsafe" Stack Data
#if UNSAFE
        [ThreadStatic()] /* ThreadSpecificData */
        private static UIntPtr outerStackAddress;
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Windows Thread Context Metadata
#if WINDOWS
        //
        // NOTE: This is used to synchronize access to the
        //       ThreadContextBuffer static field.
        //
        private static readonly object syncRoot = new object();

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: When not zero, this will point to memory that must be freed
        //       via the Marshal.FreeCoTaskMem method.  This will be handled
        //       automatically by this class using an event handler for the
        //       AppDomain.DomainUnload -OR- AppDomain.ProcessExit event,
        //       depending on whether or not this is the default AppDomain.
        //
        private static IntPtr ThreadContextBuffer = IntPtr.Zero;

        /////////////////////////////////////////////////////////////////////////////////

        private static uint TebStackBaseOffset;
        private static uint TebStackLimitOffset;
        private static uint TebDeallocationStack;

        private static uint CONTEXT_FLAGS_OFFSET;
        private static uint CONTEXT_CONTROL;
        private static uint CONTEXT_SIZE;
        private static uint CONTEXT_ESP_OFFSET;

        /////////////////////////////////////////////////////////////////////////////////

        private static int CannotQueryThread;
        private static int InvalidTeb;
        private static int TebException;
        private static int ContextException;

        /////////////////////////////////////////////////////////////////////////////////

        private static int CanQueryThread;

        /////////////////////////////////////////////////////////////////////////////////

        private static NtCurrentTeb NtCurrentTeb = null; /* delegate */
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Unix Stack Data
#if UNIX
        private static int CannotQueryStack;
        private static int InvalidStackPointer;
        private static int InvalidStackRlimit;
        private static int InvalidStackSize;

        /////////////////////////////////////////////////////////////////////////////////

        private static int CanQueryStack;
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region New Thread Stack Size Data
        //
        // HACK: This should really be a "ulong"; however, there is no
        //       overload for Interlocked.Increment method that accepts
        //       a "ulong".
        //
        private static long NewThreadStackSize;
        #endregion
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static NativeStack()
        {
            MaybeInitialize();
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region AppDomain Initialization
        public static void MaybeInitialize()
        {
            if (IsAvailable == null)
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    WindowsSetupTebAndContextMetadata();

#if UNSAFE
                    /* IGNORED */
                    GetStackPointer();
#endif

                    IsAvailable = (Interlocked.CompareExchange(
                        ref CanQueryThread, 0, 0) > 0);
                }
#endif

                /////////////////////////////////////////////////////////////////////////

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    /* IGNORED */
                    Interlocked.Increment(ref CanQueryStack);

#if UNSAFE
                    /* IGNORED */
                    GetStackPointer();
#endif

                    IsAvailable = (Interlocked.CompareExchange(
                        ref CanQueryStack, 0, 0) > 0);
                }
#endif
            }
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region AppDomain EventHandler (ProcessExit / DomainUnload)
#if WINDOWS
        private static void AddExitedEventHandler()
        {
            if (!GlobalConfiguration.DoesValueExist(
                    "No_NativeStack_Exited",
                    ConfigurationFlags.NativeStack))
            {
                AppDomain appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    if (!AppDomainOps.IsDefault(appDomain))
                    {
                        appDomain.DomainUnload -= NativeStack_Exited;
                        appDomain.DomainUnload += NativeStack_Exited;
                    }
                    else
                    {
                        appDomain.ProcessExit -= NativeStack_Exited;
                        appDomain.ProcessExit += NativeStack_Exited;
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
                    appDomain.DomainUnload -= NativeStack_Exited;
                else
                    appDomain.ProcessExit -= NativeStack_Exited;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static void NativeStack_Exited(
            object sender,
            EventArgs e
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (ThreadContextBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(ThreadContextBuffer);
                    ThreadContextBuffer = IntPtr.Zero;
                }

                RemoveExitedEventHandler();
            }
        }
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Windows Thread Context Support Methods (DO NOT CALL)
#if WINDOWS
        private static void WindowsSetupTebAndContextMetadata()
        {
            OperatingSystemId operatingSystemId = PlatformOps.GetOperatingSystemId();

            switch (operatingSystemId)
            {
                case OperatingSystemId.WindowsNT:
                    {
                        ProcessorArchitecture processorArchitecture =
                            PlatformOps.GetProcessorArchitecture();

                        switch (processorArchitecture)
                        {
                            case ProcessorArchitecture.Intel:
                            case ProcessorArchitecture.IA32_on_Win64:
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        TebStackBaseOffset = TebStackBaseOffset32Bit;
                                        TebStackLimitOffset = TebStackLimitOffset32Bit;
                                        TebDeallocationStack = TebDeallocationStack32Bit;

                                        CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_i386;
                                        CONTEXT_CONTROL = CONTEXT_CONTROL_i386;
                                        CONTEXT_SIZE = CONTEXT_SIZE_i386;
                                        CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_i386;

                                        //
                                        // NOTE: Support is present in NTDLL, use the direct
                                        //       (fast) method.
                                        //
                                        NtCurrentTeb = null;
                                    }

                                    TraceOps.DebugTrace(String.Format(
                                        "selected {0}/x86 architecture",
                                        operatingSystemId),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    Interlocked.CompareExchange(ref CanQueryThread, 1, 0);
                                    break;
                                }
                            case ProcessorArchitecture.ARM:
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        TebStackBaseOffset = TebStackBaseOffset32Bit;
                                        TebStackLimitOffset = TebStackLimitOffset32Bit;
                                        TebDeallocationStack = TebDeallocationStack32Bit;

                                        CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_ARM;
                                        CONTEXT_CONTROL = CONTEXT_CONTROL_ARM;
                                        CONTEXT_SIZE = CONTEXT_SIZE_ARM;
                                        CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_ARM;

                                        //
                                        // NOTE: Native stack checking is not "officially"
                                        //       supported on this architecture; however,
                                        //       it may work.
                                        //
                                        NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);
                                    }

                                    TraceOps.DebugTrace(String.Format(
                                        "selected {0}/ARM architecture",
                                        operatingSystemId),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    Interlocked.CompareExchange(ref CanQueryThread, 1, 0);
                                    break;
                                }
                            case ProcessorArchitecture.IA64:
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        TebStackBaseOffset = TebStackBaseOffset64Bit;
                                        TebStackLimitOffset = TebStackLimitOffset64Bit;
                                        TebDeallocationStack = TebDeallocationStack64Bit;

                                        CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_IA64;
                                        CONTEXT_CONTROL = CONTEXT_CONTROL_IA64;
                                        CONTEXT_SIZE = CONTEXT_SIZE_IA64;
                                        CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_IA64;

                                        //
                                        // NOTE: Native stack checking is not "officially"
                                        //       supported on this architecture; however,
                                        //       it may work.
                                        //
                                        NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);
                                    }

                                    TraceOps.DebugTrace(String.Format(
                                        "selected {0}/ia64 architecture",
                                        operatingSystemId),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    Interlocked.CompareExchange(ref CanQueryThread, 1, 0);
                                    break;
                                }
                            case ProcessorArchitecture.AMD64:
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        TebStackBaseOffset = TebStackBaseOffset64Bit;
                                        TebStackLimitOffset = TebStackLimitOffset64Bit;
                                        TebDeallocationStack = TebDeallocationStack64Bit;

                                        CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_AMD64;
                                        CONTEXT_CONTROL = CONTEXT_CONTROL_AMD64;
                                        CONTEXT_SIZE = CONTEXT_SIZE_AMD64;
                                        CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_AMD64;

                                        //
                                        // HACK: Thanks for not exporting this function from
                                        //       NTDLL on x64 (you know who you are).  Since
                                        //       support is not present in NTDLL, use the
                                        //       slow method.
                                        //
                                        NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);
                                    }

                                    TraceOps.DebugTrace(String.Format(
                                        "selected {0}/x64 architecture",
                                        operatingSystemId),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    Interlocked.CompareExchange(ref CanQueryThread, 1, 0);
                                    break;
                                }
                            case ProcessorArchitecture.ARM64:
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        TebStackBaseOffset = TebStackBaseOffset64Bit;
                                        TebStackLimitOffset = TebStackLimitOffset64Bit;
                                        TebDeallocationStack = TebDeallocationStack64Bit;

                                        CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_ARM64;
                                        CONTEXT_CONTROL = CONTEXT_CONTROL_ARM64;
                                        CONTEXT_SIZE = CONTEXT_SIZE_ARM64;
                                        CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_ARM64;

                                        //
                                        // NOTE: Native stack checking is not "officially"
                                        //       supported on this architecture; however,
                                        //       it may work.
                                        //
                                        NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);
                                    }

                                    TraceOps.DebugTrace(String.Format(
                                        "selected {0}/ARM64 architecture",
                                        operatingSystemId),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    Interlocked.CompareExchange(ref CanQueryThread, 1, 0);
                                    break;
                                }
                            default:
                                {
                                    //
                                    // NOTE: We have no idea what processor architecture
                                    //       this is.  Native stack checking is disabled.
                                    //
                                    TraceOps.DebugTrace(String.Format(
                                        "unknown architecture {0}/{1}",
                                        operatingSystemId,
                                        processorArchitecture),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeError);

                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        //
                        // NOTE: We have no idea what operating system this is.
                        //       Native stack checking is disabled.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "unknown operating system {0}",
                            operatingSystemId),
                            typeof(NativeStack).Name,
                            TracePriority.NativeError);

                        break;
                    }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static IntPtr NtCurrentTebSlow()
        {
            UnsafeNativeMethods.THREAD_BASIC_INFORMATION threadInformation =
                new UnsafeNativeMethods.THREAD_BASIC_INFORMATION();

            uint returnLength = 0;

            if (UnsafeNativeMethods.NtQueryInformationThread(
                    NativeOps.SafeNativeMethods.GetCurrentThread(),
                    UnsafeNativeMethods.THREADINFOCLASS.ThreadBasicInformation,
                    ref threadInformation,
                    (uint)Marshal.SizeOf(threadInformation),
                    ref returnLength) == STATUS_SUCCESS)
            {
                return threadInformation.TebBaseAddress;
            }

            return IntPtr.Zero;
        }
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Windows Native Stack Support Methods (DO NOT CALL)
#if WINDOWS
        private static UIntPtr WindowsGetNativeStackAllocated()
        {
            UIntPtr result = UIntPtr.Zero;

            //
            // NOTE: Are we able to query the thread environment block (i.e. we
            //       know what platform we are on and the appropriate constants
            //       have been setup)?
            //
            if (Interlocked.CompareExchange(ref CanQueryThread, 0, 0) > 0)
            {
                try
                {
                    NtCurrentTeb ntCurrentTeb;
                    uint tebStackBaseOffset;
                    uint tebStackLimitOffset;

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        ntCurrentTeb = NtCurrentTeb;
                        tebStackBaseOffset = TebStackBaseOffset;
                        tebStackLimitOffset = TebStackLimitOffset;
                    }

                    IntPtr teb = IntPtr.Zero;

                    if (ntCurrentTeb != null)
                        teb = ntCurrentTeb();
                    else
                        teb = SafeNativeMethods.NtCurrentTeb();

                    if (teb != IntPtr.Zero)
                    {
                        IntPtr stackBase = Marshal.ReadIntPtr(
                            teb, (int)tebStackBaseOffset);

                        IntPtr stackLimit = Marshal.ReadIntPtr(
                            teb, (int)tebStackLimitOffset);

                        if (stackBase.ToInt64() > stackLimit.ToInt64())
                        {
                            result = new UIntPtr(ConversionOps.ToULong(
                                stackBase.ToInt64() - stackLimit.ToInt64()));
                        }
                    }
                    else
                    {
                        if (Interlocked.Increment(ref InvalidTeb) == 1)
                        {
                            TraceOps.DebugTrace(
                                "WindowsGetNativeStackAllocated: invalid TEB",
                                typeof(NativeStack).Name,
                                TracePriority.NativeError);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Interlocked.Increment(ref TebException) == 1)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeStack).Name,
                            TracePriority.NativeError);
                    }
                }
            }
            else
            {
                if (Interlocked.Increment(ref CannotQueryThread) == 1)
                {
                    TraceOps.DebugTrace(
                        "WindowsGetNativeStackAllocated: cannot query thread",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }
            }

            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr WindowsGetNativeStackMaximum()
        {
            UIntPtr result = UIntPtr.Zero;

            //
            // NOTE: Are we able to query the thread environment block (i.e. we
            //       know what platform we are on and the appropriate constants
            //       have been setup)?
            //
            if (Interlocked.CompareExchange(ref CanQueryThread, 0, 0) > 0)
            {
                try
                {
                    NtCurrentTeb ntCurrentTeb;
                    uint tebStackBaseOffset;
                    uint tebDeallocationStack;

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        ntCurrentTeb = NtCurrentTeb;
                        tebStackBaseOffset = TebStackBaseOffset;
                        tebDeallocationStack = TebDeallocationStack;
                    }

                    IntPtr teb = IntPtr.Zero;

                    if (ntCurrentTeb != null)
                        teb = ntCurrentTeb();
                    else
                        teb = SafeNativeMethods.NtCurrentTeb();

                    if (teb != IntPtr.Zero)
                    {
                        IntPtr stackBase = Marshal.ReadIntPtr(
                            teb, (int)tebStackBaseOffset);

                        IntPtr deallocationStack = Marshal.ReadIntPtr(
                            teb, (int)tebDeallocationStack);

                        if (stackBase.ToInt64() > deallocationStack.ToInt64())
                        {
                            result = new UIntPtr(ConversionOps.ToULong(
                                stackBase.ToInt64() - deallocationStack.ToInt64()));
                        }
                    }
                    else
                    {
                        if (Interlocked.Increment(ref InvalidTeb) == 1)
                        {
                            TraceOps.DebugTrace(
                                "WindowsGetNativeStackMaximum: invalid TEB",
                                typeof(NativeStack).Name,
                                TracePriority.NativeError);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Interlocked.Increment(ref TebException) == 1)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeStack).Name,
                            TracePriority.NativeError);
                    }
                }
            }
            else
            {
                if (Interlocked.Increment(ref CannotQueryThread) == 1)
                {
                    TraceOps.DebugTrace(
                        "WindowsGetNativeStackMaximum: cannot query thread",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }
            }

            return result;
        }
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private "Unsafe" Specific Methods (DO NOT CALL)
#if UNSAFE
        private static UIntPtr GetStackPointer()
        {
            int result = 0; /* stack */

            if (outerStackAddress != UIntPtr.Zero)
            {
                //
                // NOTE: Outer stack pointer address is already saved.
                //       Just return the current stack pointer address.
                //
                return GetStackPointer(ref result);
            }
            else
            {
                //
                // NOTE: The outer stack pointer address has not been
                //       initialized.  Do that now and also return the
                //       address.
                //
                return outerStackAddress = GetStackPointer(ref result);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Currently, the code in this class assumes that this
        //       method cannot fail.  In other words, if this method
        //       is compiled into the assembly, it is assumed that it
        //       will return the actual native stack pointer.
        //
        private unsafe static UIntPtr GetStackPointer(
            ref int parameter
            )
        {
            fixed (int* parameterPtr = &parameter)
            {
                return new UIntPtr(parameterPtr);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool StackGrowsDown()
        {
            int parent = 0; /* stack */

            return StackGrowsDown(ref parent);
        }

        /////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Currently, the code in this class assumes that this
        //       method cannot fail.  In other words, if this method
        //       is compiled into the assembly, it is assumed that it
        //       will return non-zero only if the stack really grows
        //       downward as it is being used.
        //
        private unsafe static bool StackGrowsDown(
            ref int parent
            )
        {
            int here; /* stack */

            fixed (int* parentPtr = &parent)
            {
                return (&here < parentPtr);
            }
        }
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Unix Specific Methods (DO NOT CALL)
#if UNIX
        private static UIntPtr UnixGetStackPointer()
        {
            if (Interlocked.CompareExchange(ref CanQueryStack, 0, 0) <= 0)
            {
                if (Interlocked.Increment(ref CannotQueryStack) == 1)
                {
                    TraceOps.DebugTrace(
                        "UnixGetStackPointer: cannot query stack",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }

                return UIntPtr.Zero;
            }

            UIntPtr pointer = UIntPtr.Zero;

#if UNSAFE
            pointer = GetStackPointer();
#endif

            if ((pointer == UIntPtr.Zero) &&
                (Interlocked.Increment(ref InvalidStackPointer) == 1))
            {
                /* IGNORED */
                Interlocked.Exchange(ref CanQueryStack, 0);

                TraceOps.DebugTrace(
                    "UnixGetStackPointer: cannot query pointer",
                    typeof(NativeStack).Name,
                    TracePriority.NativeError);
            }

            return pointer;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr UnixGetStackSizeViaRlimit()
        {
            try
            {
                UnsafeNativeMethods.rlimit rLimit = new UnsafeNativeMethods.rlimit();

                if (UnsafeNativeMethods.getrlimit(
                        UnsafeNativeMethods.RLIMIT_STACK, ref rLimit) == 0)
                {
                    return rLimit.rlim_cur;
                }
            }
            catch
            {
                // do nothing.
            }

            return UIntPtr.Zero;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr UnixGetStackAddressViaPthread() /* NOT USED */
        {
            try
            {
                return UnsafeNativeMethods.pthread_get_stackaddr_np(
                    SafeNativeMethods.pthread_self());
            }
            catch
            {
                // do nothing.
            }

            return UIntPtr.Zero;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr UnixGetStackSizeViaPthread()
        {
            try
            {
                return UnsafeNativeMethods.pthread_get_stacksize_np(
                    SafeNativeMethods.pthread_self());
            }
            catch
            {
                // do nothing.
            }

            return UIntPtr.Zero;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr UnixGetStackSizeViaPthreadAttr1()
        {
            UnsafeNativeMethods.pthread_attr_t attr =
                new UnsafeNativeMethods.pthread_attr_t();

            try
            {
                if (UnsafeNativeMethods.pthread_attr_init(ref attr) == 0)
                {
                    if (UnsafeNativeMethods.pthread_getattr_np(
                            SafeNativeMethods.pthread_self(), ref attr) == 0)
                    {
                        UIntPtr stackSize = UIntPtr.Zero;

                        if (UnsafeNativeMethods.pthread_attr_getstacksize(
                                ref attr, ref stackSize) == 0)
                        {
                            return stackSize;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                try
                {
                    /* IGNORED */
                    UnsafeNativeMethods.pthread_attr_destroy(ref attr);
                }
                catch
                {
                    // do nothing.
                }
            }

            return UIntPtr.Zero;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr UnixGetStackSizeViaPthreadAttr2()
        {
            UnsafeNativeMethods.pthread_attr_t attr =
                new UnsafeNativeMethods.pthread_attr_t();

            try
            {
                if (UnsafeNativeMethods.pthread_attr_init(ref attr) == 0)
                {
                    if (UnsafeNativeMethods.pthread_attr_get_np(
                            SafeNativeMethods.pthread_self(), ref attr) == 0)
                    {
                        UIntPtr stackSize = UIntPtr.Zero;

                        if (UnsafeNativeMethods.pthread_attr_getstacksize(
                                ref attr, ref stackSize) == 0)
                        {
                            return stackSize;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                try
                {
                    /* IGNORED */
                    UnsafeNativeMethods.pthread_attr_destroy(ref attr);
                }
                catch
                {
                    // do nothing.
                }
            }

            return UIntPtr.Zero;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr UnixGetStackSize()
        {
            if (Interlocked.CompareExchange(ref CanQueryStack, 0, 0) <= 0)
            {
                if (Interlocked.Increment(ref CannotQueryStack) == 1)
                {
                    TraceOps.DebugTrace(
                        "UnixGetStackSize: cannot query stack",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }

                return UIntPtr.Zero;
            }

            UIntPtr size; /* REUSED */

            if (Interlocked.CompareExchange(
                    ref isMainThreadCallback, null, null) == null)
            {
                Interlocked.CompareExchange(
                    ref isMainThreadCallback,
                    NativeOps.IsMainThread, null);

                if (isMainThreadCallback())
                {
                    size = UnixGetStackSizeViaRlimit();

                    if ((size == UIntPtr.Zero) &&
                        (Interlocked.Increment(ref InvalidStackRlimit) == 1))
                    {
                        /* IGNORED */
                        Interlocked.Exchange(ref CanQueryStack, 0);

                        TraceOps.DebugTrace(
                            "UnixGetStackSize: cannot query rlimit",
                            typeof(NativeStack).Name,
                            TracePriority.NativeError);
                    }

                    return size;
                }
            }
            else if (isMainThreadCallback())
            {
                size = UnixGetStackSizeViaRlimit();

                if ((size == UIntPtr.Zero) &&
                    (Interlocked.Increment(ref InvalidStackRlimit) == 1))
                {
                    /* IGNORED */
                    Interlocked.Exchange(ref CanQueryStack, 0);

                    TraceOps.DebugTrace(
                        "UnixGetStackSize: cannot query rlimit",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }

                return size;
            }

            if (Interlocked.CompareExchange(
                    ref unixGetNativeStackMaximumCallback, null, null) == null)
            {
                size = UnixGetStackSizeViaPthread();

                if (size != UIntPtr.Zero)
                {
                    Interlocked.CompareExchange(
                        ref unixGetNativeStackMaximumCallback,
                        UnixGetStackSizeViaPthread, null);

                    return size;
                }

                size = UnixGetStackSizeViaPthreadAttr1();

                if (size != UIntPtr.Zero)
                {
                    Interlocked.CompareExchange(
                        ref unixGetNativeStackMaximumCallback,
                        UnixGetStackSizeViaPthreadAttr1, null);

                    return size;
                }

                size = UnixGetStackSizeViaPthreadAttr2();

                if (size != UIntPtr.Zero)
                {
                    Interlocked.CompareExchange(
                        ref unixGetNativeStackMaximumCallback,
                        UnixGetStackSizeViaPthreadAttr2, null);

                    return size;
                }

                size = UnixGetStackSizeViaRlimit();

                if (size != UIntPtr.Zero)
                {
                    Interlocked.CompareExchange(
                        ref unixGetNativeStackMaximumCallback,
                        UnixGetStackSizeViaRlimit, null);

                    return size;
                }

                if (Interlocked.Increment(ref InvalidStackSize) == 1)
                {
                    /* IGNORED */
                    Interlocked.Exchange(ref CanQueryStack, 0);

                    TraceOps.DebugTrace(
                        "UnixGetStackSize: cannot query size",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }

                return UIntPtr.Zero;
            }
            else
            {
                return unixGetNativeStackMaximumCallback();
            }
        }
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Windows Specific Methods (DO NOT CALL)
#if WINDOWS
        private static UIntPtr WindowsGetNativeStackPointer()
        {
            uint flags;
            uint offset;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                flags = CONTEXT_CONTROL;
                offset = CONTEXT_ESP_OFFSET;
            }

            return WindowsGetNativeRegister(
                NativeOps.SafeNativeMethods.GetCurrentThread(),
                flags, (int)offset, IntPtr.Size);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr WindowsGetNativeRegister(
            IntPtr thread,
            uint flags,
            int offset,
            int size
            )
        {
            //
            // NOTE: Are we able to query the thread context (i.e. we know
            //       what platform we are on and the appropriate constants
            //       have been setup)?
            //
            if (Interlocked.CompareExchange(ref CanQueryThread, 0, 0) <= 0)
            {
                if (Interlocked.Increment(ref CannotQueryThread) == 1)
                {
                    TraceOps.DebugTrace(
                        "WindowsGetNativeRegister: cannot query thread",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }

                return UIntPtr.Zero;
            }

            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Do not allow any attempts to read outside what
                    //       the apparent bounds of the CONTEXT structure.
                    //
                    if ((offset < 0) ||
                        (offset > (CONTEXT_SIZE - IntPtr.Size)))
                    {
                        return UIntPtr.Zero;
                    }

                    //
                    // NOTE: Perform one-time allocation of the fixed-size
                    //       thread context buffer, on demand and schedule
                    //       to have it freed prior to process exit.
                    //
                    if (ThreadContextBuffer == IntPtr.Zero)
                    {
                        //
                        // NOTE: Schedule the fixed-size thread context
                        //       buffer to be freed either upon the
                        //       AppDomain being unloaded (if we are not
                        //       in the default AppDomain) or when the
                        //       process exits.  This should gracefully
                        //       handle both embedding and stand-alone
                        //       scenarios.
                        //
                        AddExitedEventHandler();

                        //
                        // NOTE: Now that we are sure that we have
                        //       succeeded in scheduling the cleanup for
                        //       this buffer, allocate it.
                        //
                        // NOTE: For safety, we now allocate at least a
                        //       whole page for this buffer.
                        //
                        // ThreadContextBuffer = Marshal.AllocCoTaskMem(
                        //     (int)CONTEXT_SIZE);
                        //
                        uint pageSize = PlatformOps.GetPageSize();

                        ThreadContextBuffer = Marshal.AllocCoTaskMem(
                            (int)Math.Max(CONTEXT_SIZE, pageSize));
                    }

                    //
                    // NOTE: Make sure we were able to allocate the
                    //       thread context buffer.
                    //
                    IntPtr threadContext = ThreadContextBuffer;

                    if (threadContext == IntPtr.Zero)
                        return UIntPtr.Zero;

                    //
                    // NOTE: Write flags that tell GetThreadContext
                    //       which fields of the thread context buffer
                    //       we would like it to populate.  For now,
                    //       we mainly want to support the control
                    //       registers (primarily for ESP and EBP).
                    //
                    Marshal.WriteInt32(
                        threadContext, (int)CONTEXT_FLAGS_OFFSET,
                        (int)flags);

                    //
                    // NOTE: Query the Win32 API to obtain the
                    //       requested thread context.  In theory,
                    //       this could fail or throw an exception
                    //       at this point.  In that case, we would
                    //       return zero from this function and the
                    //       stack checking code would assume that
                    //       native stack checking is unavailable
                    //       and should not be relied upon.
                    //
                    // BUGBUG: This does not work properly when
                    //         running on Mono (on Windows).  As of
                    //         Mono 5.14.0.177, it fails here with
                    //         the following exception:
                    //
                    //         System.BadImageFormatException:
                    //         Method has no body
                    //
                    if (UnsafeNativeMethods.GetThreadContext(thread,
                            threadContext))
                    {
                        if (size == IntPtr.Size)
                        {
                            return ConversionOps.ToUIntPtr(
                                Marshal.ReadIntPtr(threadContext,
                                offset));
                        }
                        else
                        {
                            switch (size)
                            {
                                case sizeof(long):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadInt64(threadContext,
                                            offset));
                                    }
                                case sizeof(int):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadInt32(threadContext,
                                            offset));
                                    }
                                case sizeof(short):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadInt16(threadContext,
                                            offset));
                                    }
                                case sizeof(byte):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadByte(threadContext,
                                            offset));
                                    }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Interlocked.Increment(ref ContextException) == 1)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }
            }

            return UIntPtr.Zero;
        }
#endif
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Platform Abstraction Methods (DO NOT CALL)
        #region PE File Stack Size Support Methods
        private static ulong QueryNewThreadNativeStackSize()
        {
            ulong result = FileOps.GetPeFileStackReserve();

            if (result < DefaultStackSize)
                result = DefaultStackSize;

            return result;
        }
        #endregion
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        #region PE File Stack Size Support Methods
        //
        // NOTE: For use by the Engine.GetNewThreadStackSize method only.
        //
        public static ulong GetNewThreadNativeStackSize()
        {
            long oldValue = Interlocked.CompareExchange(
                ref NewThreadStackSize, 0, 0);

            if (oldValue != 0)
                return ConversionOps.ToULong(oldValue);

            ulong newValue = QueryNewThreadNativeStackSize();

            if (newValue != 0)
            {
                oldValue = ConversionOps.ToLong(newValue);

                /* IGNORED */
                Interlocked.CompareExchange(
                    ref NewThreadStackSize, oldValue, 0);
            }

            return newValue;
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Native Stack Support Methods
#if WINDOWS || UNIX || UNSAFE
        public static ReturnCode ChangeCallback(
            NativeCallbackType callbackType, /* in */
            ref Delegate @delegate,          /* in, out */
            ref Result error                 /* out */
            )
        {
            switch (callbackType)
            {
                case NativeCallbackType.IsMainThread:
                    {
                        NativeIsMainThreadCallback callback =
                            @delegate as NativeIsMainThreadCallback;

                        if (callback != null)
                        {
                            @delegate = Interlocked.Exchange(
                                ref isMainThreadCallback,
                                callback);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "could not convert delegate to type {0}",
                                typeof(NativeIsMainThreadCallback));

                            return ReturnCode.Error;
                        }
                    }
                case NativeCallbackType.GetStackPointer:
                    {
                        NativeStackCallback callback =
                            @delegate as NativeStackCallback;

                        if (callback != null)
                        {
                            @delegate = Interlocked.Exchange(
                                ref getNativeStackPointerCallback,
                                callback);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "could not convert delegate to type {0}",
                                typeof(NativeStackCallback));

                            return ReturnCode.Error;
                        }
                    }
                case NativeCallbackType.GetStackAllocated:
                    {
                        NativeStackCallback callback =
                            @delegate as NativeStackCallback;

                        if (callback != null)
                        {
                            @delegate = Interlocked.Exchange(
                                ref getNativeStackAllocatedCallback,
                                callback);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "could not convert delegate to type {0}",
                                typeof(NativeStackCallback));

                            return ReturnCode.Error;
                        }
                    }
                case NativeCallbackType.GetStackMaximum:
                    {
                        NativeStackCallback callback =
                            @delegate as NativeStackCallback;

                        if (callback != null)
                        {
                            @delegate = Interlocked.Exchange(
                                ref getNativeStackMaximumCallback,
                                callback);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "could not convert delegate to type {0}",
                                typeof(NativeStackCallback));

                            return ReturnCode.Error;
                        }
                    }
#if UNIX
                case NativeCallbackType.UnixGetStackMaximum:
                    {
                        NativeStackCallback callback =
                            @delegate as NativeStackCallback;

                        if (callback != null)
                        {
                            @delegate = Interlocked.Exchange(
                                ref unixGetNativeStackMaximumCallback,
                                callback);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "could not convert delegate to type {0}",
                                typeof(NativeStackCallback));

                            return ReturnCode.Error;
                        }
                    }
#endif
                default:
                    {
                        error = String.Format(
                            "unsupported callback type \"{0}\"",
                            callbackType);

                        return ReturnCode.Error;
                    }
            }
        }
#endif

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackMinimum()
        {
            return new UIntPtr(
                (ulong)PlatformOps.GetPageSize() * (ulong)StackMinimumPages);
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackMargin()
        {
            return new UIntPtr(
                (ulong)PlatformOps.GetPageSize() * (ulong)StackMarginPages);
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackPointer()
        {
#if WINDOWS || UNIX || UNSAFE
            if (Interlocked.CompareExchange(
                    ref getNativeStackPointerCallback, null, null) == null)
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
#if UNSAFE
                    //
                    // HACK: Work around total brokenness of
                    //       the GetThreadContext Win32 API
                    //       when running on Mono.
                    //
                    if (CommonOps.Runtime.IsMono())
                    {
                        Interlocked.CompareExchange(
                            ref getNativeStackPointerCallback,
                            GetStackPointer, null);
                    }
                    else
#endif
                    {
                        Interlocked.CompareExchange(
                            ref getNativeStackPointerCallback,
                            WindowsGetNativeStackPointer, null);
                    }

                    return getNativeStackPointerCallback();
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    Interlocked.CompareExchange(
                        ref getNativeStackPointerCallback,
                        UnixGetStackPointer, null);

                    return getNativeStackPointerCallback();
                }
#endif

                return UIntPtr.Zero;
            }
            else
            {
                return getNativeStackPointerCallback();
            }
#else
            return UIntPtr.Zero;
#endif
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackAllocated()
        {
#if WINDOWS || UNIX || UNSAFE
            if (Interlocked.CompareExchange(
                    ref getNativeStackAllocatedCallback, null, null) == null)
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    Interlocked.CompareExchange(
                        ref getNativeStackAllocatedCallback,
                        WindowsGetNativeStackAllocated, null);

                    return getNativeStackAllocatedCallback();
                }
#endif

                return UIntPtr.Zero;
            }
            else
            {
                return getNativeStackAllocatedCallback();
            }
#else
            return UIntPtr.Zero;
#endif
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackMaximum()
        {
#if WINDOWS || UNIX || UNSAFE
            if (Interlocked.CompareExchange(
                    ref getNativeStackMaximumCallback, null, null) == null)
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    Interlocked.CompareExchange(
                        ref getNativeStackMaximumCallback,
                        WindowsGetNativeStackMaximum, null);

                    return getNativeStackMaximumCallback();
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    Interlocked.CompareExchange(
                        ref getNativeStackMaximumCallback,
                        UnixGetStackSize, null);

                    return getNativeStackMaximumCallback();
                }
#endif

                return UIntPtr.Zero;
            }
            else
            {
                return getNativeStackMaximumCallback();
            }
#else
            return UIntPtr.Zero;
#endif
        }
        #endregion
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            // lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (IsAvailable != null))
                {
                    localList.Add("IsAvailable", (IsAvailable != null) ?
                        IsAvailable.ToString() : FormatOps.DisplayNull);
                }

#if WINDOWS || UNIX || UNSAFE
                if (empty || (isMainThreadCallback != null))
                {
                    localList.Add("IsMainThreadCallback",
                        FormatOps.DelegateMethodName(
                            isMainThreadCallback, false, true));
                }

                if (empty || (getNativeStackPointerCallback != null))
                {
                    localList.Add("GetNativeStackPointerCallback",
                        FormatOps.DelegateMethodName(
                            getNativeStackPointerCallback, false,
                            true));
                }

                if (empty || (getNativeStackAllocatedCallback != null))
                {
                    localList.Add("GetNativeStackAllocatedCallback",
                        FormatOps.DelegateMethodName(
                            getNativeStackAllocatedCallback, false,
                            true));
                }

                if (empty || (getNativeStackMaximumCallback != null))
                {
                    localList.Add("GetNativeStackMaximumCallback",
                        FormatOps.DelegateMethodName(
                            getNativeStackMaximumCallback, false,
                            true));
                }
#endif

#if UNIX
                if (empty || (unixGetNativeStackMaximumCallback != null))
                {
                    localList.Add("UnixGetNativeStackMaximumCallback",
                        FormatOps.DelegateMethodName(
                            unixGetNativeStackMaximumCallback, false,
                            true));
                }
#endif

#if UNSAFE
                if (empty || (outerStackAddress != UIntPtr.Zero))
                {
                    localList.Add("OuterStackAddress",
                        outerStackAddress.ToString());
                }
#endif

#if WINDOWS
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (empty || (ThreadContextBuffer != IntPtr.Zero))
                    {
                        localList.Add("ThreadContextBuffer",
                            ThreadContextBuffer.ToString());
                    }

                    if (empty || (TebStackBaseOffset != 0))
                    {
                        localList.Add("TebStackBaseOffset",
                            TebStackBaseOffset.ToString());
                    }

                    if (empty || (TebStackLimitOffset != 0))
                    {
                        localList.Add("TebStackLimitOffset",
                            TebStackLimitOffset.ToString());
                    }

                    if (empty || (TebDeallocationStack != 0))
                    {
                        localList.Add("TebDeallocationStack",
                            TebDeallocationStack.ToString());
                    }

                    if (empty || (CONTEXT_FLAGS_OFFSET != 0))
                    {
                        localList.Add("CONTEXT_FLAGS_OFFSET",
                            CONTEXT_FLAGS_OFFSET.ToString());
                    }

                    if (empty || (CONTEXT_CONTROL != 0))
                    {
                        localList.Add("CONTEXT_CONTROL",
                            CONTEXT_CONTROL.ToString());
                    }

                    if (empty || (CONTEXT_SIZE != 0))
                    {
                        localList.Add("CONTEXT_SIZE",
                            CONTEXT_SIZE.ToString());
                    }

                    if (empty || (CONTEXT_ESP_OFFSET != 0))
                    {
                        localList.Add("CONTEXT_ESP_OFFSET",
                            CONTEXT_ESP_OFFSET.ToString());
                    }

                    if (empty || (NtCurrentTeb != null))
                    {
                        localList.Add("NtCurrentTeb",
                            FormatOps.DelegateMethodName(
                                NtCurrentTeb, false, true));
                    }
                }

                if (empty || (Interlocked.CompareExchange(
                        ref CannotQueryThread, 0, 0) != 0))
                {
                    localList.Add("CannotQueryThread",
                        CannotQueryThread.ToString());
                }

                if (empty || (Interlocked.CompareExchange(
                        ref InvalidTeb, 0, 0) != 0))
                    localList.Add("InvalidTeb", InvalidTeb.ToString());

                if (empty || (TebException != 0))
                    localList.Add("TebException", TebException.ToString());

                if (empty || (Interlocked.CompareExchange(
                        ref ContextException, 0, 0) != 0))
                {
                    localList.Add("ContextException",
                        ContextException.ToString());
                }

                if (empty || (Interlocked.CompareExchange(
                        ref CanQueryThread, 0, 0) != 0))
                {
                    localList.Add("CanQueryThread", CanQueryThread.ToString());
                }
#endif

#if UNIX
                if (empty || (Interlocked.CompareExchange(
                        ref CannotQueryStack, 0, 0) != 0))
                {
                    localList.Add("CannotQueryStack",
                        CannotQueryStack.ToString());
                }

                if (empty || (Interlocked.CompareExchange(
                        ref InvalidStackPointer, 0, 0) != 0))
                {
                    localList.Add("InvalidStackPointer",
                        InvalidStackPointer.ToString());
                }

                if (empty || (Interlocked.CompareExchange(
                        ref InvalidStackRlimit, 0, 0) != 0))
                {
                    localList.Add("InvalidStackRlimit",
                        InvalidStackRlimit.ToString());
                }

                if (empty || (Interlocked.CompareExchange(
                        ref InvalidStackSize, 0, 0) != 0))
                {
                    localList.Add("InvalidStackSize",
                        InvalidStackSize.ToString());
                }

                if (empty || (Interlocked.CompareExchange(
                        ref CanQueryStack, 0, 0) != 0))
                {
                    localList.Add("CanQueryStack", CanQueryStack.ToString());
                }
#endif

                long newThreadStackSize = Interlocked.CompareExchange(
                    ref NewThreadStackSize, 0, 0);

                if (empty || (newThreadStackSize != 0))
                {
                    localList.Add("NewThreadStackSize",
                        newThreadStackSize.ToString());
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Native Stack");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion
    }
}
