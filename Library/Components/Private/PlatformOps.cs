/*
 * PlatformOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;

#if NATIVE && WINDOWS
using System.Diagnostics;
#endif

using System.IO;
using System.Runtime.InteropServices;

#if NATIVE
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using System.Text.RegularExpressions;

#if !NET_STANDARD_20
using Microsoft.Win32;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("751e3aab-6f53-4a0d-bc13-f1ab217ef7dc")]
    internal static class PlatformOps
    {
        #region Private Constants
        #region Windows 10 Update Constants
        private const int Windows10RtmBuildNumber = 10240;
        private const string Windows10RtmName = "Windows 10, RTM";

        private const int Windows10NovemberUpdateBuildNumber = 10586;
        private const string Windows10NovemberUpdateName = "Windows 10, November Update";

        private const int Windows10AnniversaryUpdateBuildNumber = 14393;
        private const string Windows10AnniversaryUpdateName = "Windows 10, Anniversary Update";

        private const int Windows10CreatorsUpdateBuildNumber = 15063;
        private const string Windows10CreatorsUpdateName = "Windows 10, Creators Update";

        private const int Windows10FallCreatorsUpdateBuildNumber = 16299;
        private const string Windows10FallCreatorsUpdateName = "Windows 10, Fall Creators Update";

        private const int Windows10April2018UpdateBuildNumber = 17134;
        private const string Windows10April2018UpdateName = "Windows 10, April 2018 Update";

        private const int Windows10October2018UpdateBuildNumber = 17763;
        private const string Windows10October2018UpdateName = "Windows 10, October 2018 Update";

        private const int Windows10May2019UpdateBuildNumber = 18362;
        private const string Windows10May2019UpdateName = "Windows 10, May 2019 Update";

        private const int Windows10November2019UpdateBuildNumber = 18363;
        private const string Windows10November2019UpdateName = "Windows 10, November 2019 Update";

        private const int Windows10May2020UpdateBuildNumber = 19041;
        private const string Windows10May2020UpdateName = "Windows 10, May 2020 Update";

        private const int Windows10October2020UpdateBuildNumber = 19042;
        private const string Windows10October2020UpdateName = "Windows 10, October 2020 Update";

        private const int Windows10May2021UpdateBuildNumber = 19043;
        private const string Windows10May2021UpdateName = "Windows 10, May 2021 Update";

        private const int Windows10November2021UpdateBuildNumber = 19044;
        private const string Windows10November2021UpdateName = "Windows 10, November 2021 Update";

        private const int Windows10October2022UpdateBuildNumber = 19045;
        private const string Windows10October2022UpdateName = "Windows 10, October 2022 Update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows 11 Update Constants
        private const int Windows11RtmBuildNumber = 22000;
        private const string Windows11RtmName = "Windows 11, RTM";

        private const int Windows11September2022UpdateBuildNumber = 22621;
        private const string Windows11September2022UpdateName = "Windows 11, September 2022 Update";

        private const int Windows11September2023UpdateBuildNumber = 22631;
        private const string Windows11September2023UpdateName = "Windows 11, September 2023 Update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows Version Registry Constants
#if !NET_STANDARD_20
        private const string OsVersionSubKeyName =
            "Software\\Microsoft\\Windows NT\\CurrentVersion";

        private const string ProductNameValueName = "ProductName";
        private const string ReleaseIdValueName = "ReleaseId";
        private const string CurrentTypeValueName = "CurrentType";
        private const string InstallationTypeValueName = "InstallationType";
        private const string BuildLabExValueName = "BuildLabEx";
        private const string InstallDateValueName = "InstallDate";
        private const string UpdateNamesValueName = "UpdateNames";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WMI Query (for Windows Update) Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string WmiQfeGetUpdatesCommandFileName =
            "%SystemRoot%\\System32\\wbem\\wmic.exe"; // BUGBUG: Constant?

        private static string WmiQfePropertyName = "HotFixID";

        private static string WmiQfeGetUpdatesCommandArguments =
            "QFE GET HotFixID";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private const uint defaultPageSize = 4096; /* COMPAT: x86. */

        ///////////////////////////////////////////////////////////////////////

        private static readonly string Win32SubsetOperatingSystemName = "Win32s";
        private static readonly string Windows9xOperatingSystemName = "Windows 9x";
        private static readonly string WindowsNtOperatingSystemName = "Windows NT";
        private static readonly string WindowsCeOperatingSystemName = "Windows CE";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string UnixOperatingSystemName = "Unix";
        private static readonly string XboxOperatingSystemName = "Xbox";
        private static readonly string DarwinOperatingSystemName = "Darwin";
        private static readonly string LinuxOperatingSystemName = "Linux";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string UnknownName = "unknown";

        ///////////////////////////////////////////////////////////////////////

        private static StringList processorNames = null;
        private static StringList platformNames = null;
        private static StringList productTypeNames = null;
        private static StringList operatingSystemNames = null;

        ///////////////////////////////////////////////////////////////////////

        private static IDictionary<string, string> machineNames = null;

        ///////////////////////////////////////////////////////////////////////

        private static Regex majorMinorRegEx = RegExOps.Create("^\\d+\\.\\d+");

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        private static IDictionary<string, string> alternateProcessorNames = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();
        private static bool initialized = false;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static UnsafeNativeMethods.SYSTEM_INFO systemInfo;
        private static UnsafeNativeMethods.OSVERSIONINFOEX versionInfo;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
        private static readonly char[] utsNameSeparators = {
            Characters.Null
        };

        private static UnsafeNativeMethods.utsname utsName;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static ProcessorArchitecture processorArchitecture =
            ProcessorArchitecture.Unknown;

        private static OperatingSystemId operatingSystemId =
            OperatingSystemId.Unknown;

        private static VER_PRODUCT_TYPE productType =
            VER_PRODUCT_TYPE.VER_NT_NONE;

        private static uint pageSize = defaultPageSize;

        private static IntPtr minimumApplicationAddress = IntPtr.Zero;
        private static IntPtr maximumApplicationAddress = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////

        private static OperatingSystem operatingSystem = null;

        ///////////////////////////////////////////////////////////////////////

        private static string processorName = null;
        private static string machineName = null;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        private static string alternateProcessorName = null;
        private static string alternateMachineName = null;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static string platformName = null;
        private static string productTypeName = null;
        private static string operatingSystemName = null;
        private static string operatingSystemVersion = null;
        private static string operatingSystemServicePack = null;

        ///////////////////////////////////////////////////////////////////////

        private static bool isWin32onWin64 = false;

        ///////////////////////////////////////////////////////////////////////

        private static StringList installedUpdates = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("259417ba-e318-4982-b2c0-9f6fd4196b74")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("a858d038-1313-43d2-a9b0-7a00b2975933")]
            internal struct SYSTEM_INFO
            {
                public ProcessorArchitecture wProcessorArchitecture;
                public ushort wReserved;
                public uint dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public UIntPtr dwActiveProcessorMask;
                public uint dwNumberOfProcessors;
                public uint dwProcessorType;
                public uint dwAllocationGranularity;
                public ushort wProcessorLevel;
                public ushort wProcessorRevision;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Yes, this has been tested and the size must be exactly
            //       148 bytes.
            //
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            [ObjectId("f33f3aa7-8ddc-48a0-8ac9-e950dcbaaac1")]
            internal struct OSVERSIONINFOEX
            {
                public uint dwOSVersionInfoSize;
                public uint dwMajorVersion;
                public uint dwMinorVersion;
                public uint dwBuildNumber;
                public OperatingSystemId dwPlatformId;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string szCSDVersion;
                public ushort wServicePackMajor;
                public ushort wServicePackMinor;
                public short wSuiteMask;
                public VER_PRODUCT_TYPE wProductType;
                public byte wReserved;
            }

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern void GetSystemInfo(
                ref SYSTEM_INFO systemInfo
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetVersionEx(
                ref OSVERSIONINFOEX versionInfo
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsWow64Process(
                IntPtr hProcess,
                [MarshalAs(UnmanagedType.Bool)]
                ref bool wow64Process
            );
#endif

            ///////////////////////////////////////////////////////////////////

#if UNIX
            [ObjectId("4c41ee57-ee1d-4db6-8735-a4d78dd810b9")]
            internal struct utsname
            {
                public string sysname;  /* Name of this implementation of
                                         * the operating system. */
                public string nodename; /* Name of this node within the
                                         * communications network to which
                                         * this node is attached, if any. */
                public string release;  /* Current release level of this
                                         * implementation. */
                public string version;  /* Current version level of this
                                         * release. */
                public string machine;  /* Name of the hardware type on
                                         * which the system is running. */
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("86669ff8-3031-46e1-a51b-6a0b837c0c14")]
            internal struct utsname_interop
            {
                //
                // NOTE: The following string fields should be present in
                //       this buffer, all of which will be zero-terminated:
                //
                //                      sysname
                //                      nodename
                //                      release
                //                      version
                //                      machine
                //
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
                public byte[] buffer;
            }

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int uname(out utsname_interop name);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static PlatformOps()
        {
            Initialize(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Initialization Methods
        public static void Initialize(
            bool force
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!force && initialized)
                    return;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What operating system we are executing on?
                //
                operatingSystem = Environment.OSVersion;

                ///////////////////////////////////////////////////////////////

                if (processorNames == null)
                {
                    processorNames = new StringList(new string[] {
                        "intel", "mips", "alpha", "ppc", "shx", "arm",
                        "ia64", "alpha64", "msil", "amd64", "ia32_on_win64",
                        "neutral", "arm64"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (platformNames == null)
                {
                    platformNames = new StringList(new string[] {
                        "windows", "windows", "windows", "windows", "unix",
                        "windows", "unix"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (productTypeNames == null)
                {
                    productTypeNames = new StringList(new string[] {
                        "none", "workstation", "server domain controller",
                        "server"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (operatingSystemNames == null)
                {
                    operatingSystemNames = new StringList(new string[] {
                        Win32SubsetOperatingSystemName,
                        Windows9xOperatingSystemName,
                        WindowsNtOperatingSystemName,
                        WindowsCeOperatingSystemName,
                        UnixOperatingSystemName,
                        XboxOperatingSystemName,
                        DarwinOperatingSystemName
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (machineNames == null)
                {
                    machineNames = new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                    machineNames.Add("i386", "intel");
                    machineNames.Add("i486", "intel");
                    machineNames.Add("i586", "intel");
                    machineNames.Add("i686", "intel");
                    machineNames.Add("Win32", "intel");
                    machineNames.Add("x86", "intel");
                    machineNames.Add("Win64", "amd64"); /* HACK */
                    machineNames.Add("x86_64", "amd64");
                    machineNames.Add("x64", "amd64");
                    machineNames.Add("Itanium", "ia64");

                    if (processorNames != null)
                    {
                        foreach (string name in processorNames)
                        {
                            if (name == null)
                                continue;

                            machineNames[name] = name; /* IDENTITY */
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

#if NATIVE
                if (alternateProcessorNames == null)
                {
                    alternateProcessorNames = new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                    alternateProcessorNames.Add("Intel", "x86");
                    alternateProcessorNames.Add("Win32", "x86");
                    alternateProcessorNames.Add("x86", "x86");
                    alternateProcessorNames.Add("ia32_on_win64", "x86");

                    ///////////////////////////////////////////////////////////

                    alternateProcessorNames.Add("ARM", "arm");
                    alternateProcessorNames.Add("ARM64", "arm64");

                    ///////////////////////////////////////////////////////////

                    alternateProcessorNames.Add("Win64", "x64"); /* HACK */
                    alternateProcessorNames.Add("AMD64", "x64");
                    alternateProcessorNames.Add("x64", "x64");
                    alternateProcessorNames.Add("x86_64", "x64");

                    ///////////////////////////////////////////////////////////

                    alternateProcessorNames.Add("Itanium", "IA64");
                    alternateProcessorNames.Add("IA64", "IA64");
                }
#endif

                ///////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
                if (ShouldTreatAsWindows(operatingSystem, false) &&
                    GetSystemInfo(ref systemInfo))
                {
                    //
                    // NOTE: What is the processor architecture that we are
                    //       executing on?
                    //
                    processorArchitecture = systemInfo.wProcessorArchitecture;

                    //
                    // NOTE: What is the native memory page size?
                    //
                    pageSize = systemInfo.dwPageSize;

                    //
                    // NOTE: What is the range of memory addresses that can
                    //       be used for applications?
                    //
                    minimumApplicationAddress =
                        systemInfo.lpMinimumApplicationAddress;

                    maximumApplicationAddress =
                        systemInfo.lpMaximumApplicationAddress;
                }
#endif

                ///////////////////////////////////////////////////////////////

                /* NO RESULT */
                InitializeProcessorAndMachineNames(processorArchitecture);

                ///////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
                if (ShouldTreatAsWindows(operatingSystem, false) &&
                    GetOsVersionInfo(ref versionInfo)) /* WINDOWS */
                {
                    //
                    // NOTE: What is the platform we are executing on?
                    //
                    operatingSystemId = versionInfo.dwPlatformId;
                    productType = versionInfo.wProductType;
                }
                else
#endif
                {
                    operatingSystemId = (OperatingSystemId)
                        GetOperatingSystemPlatformId();

                    productType = GetOperatingSystemProductType();
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the name of the platform that we are executing
                //       on?
                //
                if (operatingSystemId != OperatingSystemId.Unknown)
                {
                    platformName = GetPlatformName(
                        operatingSystemId, IfNotFoundType.None);
                }
                else
                {
                    platformName = GetPlatformName(
                        (OperatingSystemId)GetOperatingSystemPlatformId(),
                        IfNotFoundType.None);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the name of the platform that we are executing
                //       on?
                //
                if (productType != VER_PRODUCT_TYPE.VER_NT_NONE)
                {
                    productTypeName = GetProductTypeName(
                        productType, IfNotFoundType.None);
                }
                else
                {
                    productTypeName = GetProductTypeName(
                        GetOperatingSystemProductType(),
                        IfNotFoundType.None);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the name of the operating system that we are
                //       executing on?
                //
                operatingSystemName = GetOperatingSystemName(
                    operatingSystemId, IfNotFoundType.None);

                operatingSystemVersion = null;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Check if this process is running as Win32-on-Win64
                //       (WoW64).
                //
#if NATIVE && WINDOWS
                isWin32onWin64 = IsWin32onWin64();
#else
                isWin32onWin64 = false;
#endif

                ///////////////////////////////////////////////////////////////

#if NATIVE && UNIX
                if (!ShouldTreatAsWindows(operatingSystemId, true) &&
                    GetOsVersionInfo(ref utsName)) /* LINUX */
                {
                    //
                    // NOTE: What are the primary and alternative names of the
                    //       processor that we are executing on?
                    //
                    processorName = utsName.machine;

                    InitializeProcessorAndMachineNames(processorName);

                    //
                    // NOTE: What is the name of the platform that we are
                    //       executing on?
                    //
                    platformName = TclVars.Platform.UnixValue;

                    //
                    // NOTE: What is the name of the operating system that
                    //       we are executing on?
                    //
                    operatingSystemName = utsName.sysname;

                    //
                    // NOTE: What is the version of the operating system
                    //       that we are executing on?
                    //
                    operatingSystemVersion = utsName.release;

                    //
                    // NOTE: What is the extra version information for the
                    //       operating system that we are executing on?
                    //
                    operatingSystemServicePack = utsName.version;

                    //
                    // NOTE: Attempt to set the processor architecture based on
                    //       the processor name and/or the alternate processor
                    //       name.
                    //
                    if (!IsKnownProcessorArchitecture(processorArchitecture))
                    {
                        processorArchitecture = ParseProcessorArchitecture(
                            processorName);
                    }

                    if (!IsKnownProcessorArchitecture(processorArchitecture))
                    {
                        processorArchitecture = ParseProcessorArchitecture(
                            alternateProcessorName);
                    }

                    //
                    // NOTE: Attempt to set the processor architecture based on
                    //       the machine name and/or the alternate machine name.
                    //
                    if (!IsKnownProcessorArchitecture(processorArchitecture))
                    {
                        processorArchitecture = ParseProcessorArchitecture(
                            machineName);
                    }

                    if (!IsKnownProcessorArchitecture(processorArchitecture))
                    {
                        processorArchitecture = ParseProcessorArchitecture(
                            alternateMachineName);
                    }
                }
#endif

                ///////////////////////////////////////////////////////////////

                //
                // HACK: We really want to know the processor architecture
                //       for the current process; therefore, if we did not
                //       already figure it out, attempt to guess.
                //
                if (!IsKnownProcessorArchitecture(processorArchitecture))
                {
                    processorArchitecture = GuessProcessorArchitecture(
                        operatingSystem, null);

                    if (IsKnownProcessorArchitecture(processorArchitecture))
                    {
                        /* NO RESULT */
                        InitializeProcessorAndMachineNames(
                            processorArchitecture);
                    }
                }

                ///////////////////////////////////////////////////////////////

                initialized = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Querying Methods
        public static ProcessorArchitecture GetProcessorArchitecture()
        {
            lock (syncRoot)
            {
                return processorArchitecture;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string QueryProcessorArchitecture()
        {
            //
            // HACK: Technically, this may not be 100% accurate.
            //
            string processorArchitecture = QueryProcessorArchitecture(
                false, GetMachineName());

            CheckProcessorArchitecture(ref processorArchitecture);

            return processorArchitecture;
        }

        ///////////////////////////////////////////////////////////////////////

        public static uint GetPageSize()
        {
            lock (syncRoot)
            {
                return pageSize;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetApplicationAddressRange()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return String.Format("{0}-{1}",
                    FormatOps.Hexadecimal(
                        minimumApplicationAddress.ToInt64(), true),
                    FormatOps.Hexadecimal(
                        maximumApplicationAddress.ToInt64(), true));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetMachineName()
        {
            lock (syncRoot)
            {
                return machineName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: For future test suite usage.  Do not remove.
        //
        public static string GetAlternateMachineName()
        {
            lock (syncRoot)
            {
                return alternateMachineName;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string GetProcessorName()
        {
            lock (syncRoot)
            {
                return processorName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        public static string GetAlternateProcessorName()
        {
            lock (syncRoot)
            {
                return alternateProcessorName;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string GetPlatformName()
        {
            lock (syncRoot)
            {
                return platformName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetProductTypeName()
        {
            lock (syncRoot)
            {
                return productTypeName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static OperatingSystem GetOperatingSystem()
        {
            lock (syncRoot)
            {
                return operatingSystem;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemPatchLevel()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystemVersion != null)
                    return operatingSystemVersion;

                Version osVersion = GetOperatingSystemVersion();

                return (osVersion != null) ? osVersion.ToString() : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemMajorMinor()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystemVersion != null)
                {
                    if (majorMinorRegEx != null)
                    {
                        Match match = majorMinorRegEx.Match(
                            operatingSystemVersion);

                        if ((match != null) && match.Success)
                            return match.Value;
                    }

                    return null;
                }

                Version osVersion = GetOperatingSystemVersion();

                return FormatOps.MajorMinor(osVersion);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemServicePack()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystemServicePack != null)
                    return operatingSystemServicePack;

#if NATIVE && WINDOWS
                return FormatOps.MajorMinor(
                    GlobalState.GetTwoPartVersion(
                        versionInfo.wServicePackMajor,
                        versionInfo.wServicePackMinor));
#else
                return (operatingSystem != null) ?
                    operatingSystem.ServicePack : null;
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemReleaseId()
        {
#if !NET_STANDARD_20
            string releaseId = null;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        OsVersionSubKeyName)) /* throw */
                {
                    if (key != null)
                    {
                        try
                        {
                            releaseId = key.GetValue(
                                ReleaseIdValueName) as string; /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PlatformOps).Name,
                    TracePriority.PlatformError);
            }

            return releaseId;
#else
            return null;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        public static string GetOperatingSystemExtra(
            Interpreter interpreter, /* in: OPTIONAL */
            bool asynchronous        /* in: WARNING, Non-zero is expensive. */
            )
        {
            string productName = null;
            string releaseId = null;
            string currentType = null;
            string installationType = null;
            string buildLabEx = null;
            int installDate = 0;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        OsVersionSubKeyName)) /* throw */
                {
                    if (key != null)
                    {
                        try
                        {
                            productName = key.GetValue(
                                ProductNameValueName) as string; /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }

                        ///////////////////////////////////////////////////////

                        try
                        {
                            releaseId = key.GetValue(
                                ReleaseIdValueName) as string; /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }

                        ///////////////////////////////////////////////////////

                        try
                        {
                            currentType = key.GetValue(
                                CurrentTypeValueName) as string; /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }

                        ///////////////////////////////////////////////////////

                        try
                        {
                            installationType = key.GetValue(
                                InstallationTypeValueName) as string; /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }

                        ///////////////////////////////////////////////////////

                        try
                        {
                            buildLabEx = key.GetValue(
                                BuildLabExValueName) as string; /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }

                        ///////////////////////////////////////////////////////

                        try
                        {
                            installDate = (int)key.GetValue(
                                InstallDateValueName); /* throw */
                        }
                        catch (Exception e2)
                        {
                            TraceOps.DebugTrace(
                                e2, typeof(PlatformOps).Name,
                                TracePriority.PlatformError);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PlatformOps).Name,
                    TracePriority.PlatformError);
            }

            ///////////////////////////////////////////////////////////////////

            StringList list = new StringList();

            if (productName != null)
            {
                list.Add(ProductNameValueName);
                list.Add(productName);
            }

            if (releaseId != null)
            {
                list.Add(ReleaseIdValueName);
                list.Add(releaseId);
            }

            if (currentType != null)
            {
                list.Add(CurrentTypeValueName);
                list.Add(currentType);
            }

            if (installationType != null)
            {
                list.Add(InstallationTypeValueName);
                list.Add(installationType);
            }

            if (buildLabEx != null)
            {
                list.Add(BuildLabExValueName);
                list.Add(buildLabEx);
            }

            ///////////////////////////////////////////////////////////////////

            StringList updateNames = null;
            Version osVersion = null;

            if (IsWindows10OrHigher(ref osVersion))
            {
                string updateName = GetWindows10UpdateName(osVersion);

                if (updateName != null)
                {
                    if (updateNames == null)
                        updateNames = new StringList();

                    updateNames.Add(updateName);
                }
            }

            if (IsWindows11OrHigher(ref osVersion))
            {
                string updateName = GetWindows11UpdateName(osVersion);

                if (updateName != null)
                {
                    if (updateNames == null)
                        updateNames = new StringList();

                    updateNames.Add(updateName);
                }
            }

            StringList installedUpdates = GetInstalledUpdates(
                interpreter, asynchronous);

            if (installedUpdates != null)
            {
                if (updateNames == null)
                    updateNames = new StringList();

                updateNames.AddRange(installedUpdates);
            }

            if (updateNames != null)
            {
                list.Add(UpdateNamesValueName);
                list.Add(updateNames.ToString());
            }

            ///////////////////////////////////////////////////////////////////

            DateTime installDateTime = DateTime.MinValue;

            if ((installDate != 0) && TimeOps.UnixSecondsToDateTime(
                    installDate, ref installDateTime))
            {
                list.Add(InstallDateValueName);

                list.Add(FormatOps.Iso8601DateTime(
                    installDateTime, true));
            }

            ///////////////////////////////////////////////////////////////////

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldPopulateOperatingSystemExtra(
            Interpreter interpreter,
            bool ignoreAppDomain,
            bool ignoreFlags,
            bool ignoreSafe,
            bool ignoreSdk,
            bool ignoreInteractive
            )
        {
            if (GlobalConfiguration.DoesValueExist(
                    EnvVars.NoPopulateOsExtra,
                    ConfigurationFlags.Interpreter))
            {
                return false;
            }

            if (!ignoreAppDomain &&
                !AppDomainOps.IsCurrentDefault())
            {
                return false;
            }

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot)/* TRANSACTIONAL */
                {
                    CreateFlags createFlags = interpreter.CreateFlags;

                    if (!ignoreFlags && FlagOps.HasFlags(
                            createFlags, CreateFlags.NoPopulateOsExtra,
                            true))
                    {
                        return false;
                    }

                    if (!ignoreSafe && interpreter.InternalIsSafe())
                        return false;

                    if (!ignoreSdk && interpreter.InternalIsAnySdk())
                        return false;

                    if (!ignoreInteractive &&
                        !interpreter.InternalInteractive)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PopulateOperatingSystemExtra(
            Interpreter interpreter,
            bool errorOnDisposed,
            bool unsignalOnStart,
            bool signalWhenDone
            )
        {
            /* IGNORED */
            ThreadOps.QueueUserWorkItem(delegate(object state)
            {
                ReturnCode code;
                Result result = null;

                if (interpreter != null)
                {
                    if (unsignalOnStart)
                        UnSignalSetupEventOrComplain(interpreter);

                    try
                    {
                        string value = GetOperatingSystemExtra(
                            interpreter, true); /* WARNING: ~20 secs... */

                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            if (!Interpreter.IsDeletedOrDisposed(
                                    interpreter, false, ref result))
                            {
                                code = interpreter.SetOperatingSystemExtra(
                                    value, ref result);
                            }
                            else if (errorOnDisposed)
                            {
                                code = ReturnCode.Error;
                            }
                            else
                            {
                                result = null;
                                code = ReturnCode.Ok;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        result = e;
                        code = ReturnCode.Error;
                    }
                    finally
                    {
                        if (signalWhenDone)
                            SignalSetupEventOrComplain(interpreter);
                    }
                }
                else
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;
                }

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, result);
            }, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static OperatingSystemId GetOperatingSystemId()
        {
            lock (syncRoot)
            {
                return operatingSystemId;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemName()
        {
            lock (syncRoot)
            {
                return operatingSystemName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetWin32onWin64()
        {
            lock (syncRoot)
            {
                return isWin32onWin64;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetUserName(
            bool domain
            )
        {
            string result = Environment.UserName;

            if (!domain)
                return result;

            //
            // HACK: For now, the (qualified) user name always
            //       uses the backslash character, even on Unix.
            //
            return Environment.UserDomainName +
                PathOps.DirectorySeparatorChar + result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemNameAndVersion()
        {
            StringBuilder builder = StringBuilderFactory.Create();

            StringOps.MaybeAppend(
                builder, "{0}-bit", GetProcessBits(), true, false);

            StringOps.MaybeAppend(
                builder, "({0})", GetMachineName(), true, true);

#if NATIVE
            StringOps.MaybeAppend(
                builder, null, GetAlternateProcessorName(), true, false);
#endif

            StringOps.MaybeAppend(
                builder, null, GetOperatingSystemName(), true, false);

            StringOps.MaybeAppend(
                builder, null, GetProductTypeName(), true, true);

            StringOps.MaybeAppend(
                builder, null, GetOperatingSystemPatchLevel(), true, false);

            StringOps.MaybeAppend(
                builder, "release {0}", GetOperatingSystemReleaseId(),
                true, false);

            StringOps.MaybeAppend(
                builder, "({0})", GetPlatformName(), true, true);

            StringOps.MaybeAppend(
                builder, "[{0}]", GetOperatingSystem(), true, false);

#if !NET_STANDARD_20
            StringOps.MaybeAppend(
                builder, "[{0}]", GetOperatingSystemExtra(null, false),
                true, false);
#endif

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Querying Methods
        private static string QueryProcessorArchitecture(
            bool force,     /* in */
            string @default /* in: OPTIONAL */
            )
        {
            if (force || IsWindowsOperatingSystem())
            {
                return CommonOps.Environment.GetVariable(
                    EnvVars.ProcessorArchitecture);
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void CheckProcessorArchitecture(
            ref string processorArchitecture /* in, out: OPTIONAL */
            )
        {
            //
            // HACK: Check for an "impossible" situation.  If the pointer size
            //       is 32-bits, the processor architecture cannot be "AMD64".
            //
            //       In that case, we are (almost certainly) hitting a bug in
            //       the operating system and/or Visual Studio that causes the
            //       "PROCESSOR_ARCHITECTURE" environment variable to contain
            //       the wrong value in some circumstances.  There are several
            //       reports of this issue from users on StackOverflow.
            //
            if (Is32BitProcess() && SharedStringOps.SystemNoCaseEquals(
                    processorArchitecture, "AMD64"))
            {
                //
                // NOTE: When tracing is enabled, save the originally detected
                //       processor architecture before changing it.
                //
                string savedProcessorArchitecture = processorArchitecture;

                //
                // NOTE: We know that operating systems that return "AMD64" as
                //       the processor architecture are actually a superset of
                //       the "x86" processor architecture; therefore, return
                //       "x86" when the pointer size is 32-bits.
                //
                processorArchitecture = "x86";

                //
                // NOTE: Show that we hit a fairly unusual situation (i.e. the
                //       "wrong" processor architecture was detected).
                //
                TraceOps.DebugTrace(String.Format(
                    "Detected {0}-bit process pointer size with " +
                    "processor architecture {1}, using processor " +
                    "architecture {2} instead...", GetProcessBits(),
                    FormatOps.WrapOrNull(savedProcessorArchitecture),
                    FormatOps.WrapOrNull(processorArchitecture)),
                    typeof(PlatformOps).Name, TracePriority.StartupDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InitializeProcessorAndMachineNames(
            ProcessorArchitecture processorArchitecture /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: What are the primary and alternative names
                //       of the processor that we are executing on?
                //
                processorName = GetProcessorName(
                    processorArchitecture, IfNotFoundType.None);

                InitializeProcessorAndMachineNames(processorName);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InitializeProcessorAndMachineNames(
            string processorName /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
#if NATIVE
                alternateProcessorName = GetAlternateProcessorName(
                    processorName, IfNotFoundType.Unknown);
#endif

                //
                // NOTE: What are the primary and alternative names
                //       of the "machine" that we are executing on?
                //       This is being done based on the processor
                //       name; however, it may or may not end up
                //       with the same value as the processor name.
                //
                machineName = GetMachineName(
                    processorName, IfNotFoundType.Unknown);

#if NATIVE
                alternateMachineName = GetMachineName(
                    alternateProcessorName, IfNotFoundType.Unknown);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryParseProcessorArchitecture(
            string name,                     /* in */
            out ProcessorArchitecture? value /* out */
            )
        {
            value = null;

            if (!String.IsNullOrEmpty(name))
            {
                object enumValue = EnumOps.TryParse(
                    typeof(ProcessorArchitecture), name, true, true);

                if (enumValue is ProcessorArchitecture)
                {
                    value = (ProcessorArchitecture)enumValue;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ProcessorArchitecture ParseProcessorArchitecture(
            string name /* in */
            )
        {
            ProcessorArchitecture? value;

            if (TryParseProcessorArchitecture(name, out value))
                return (ProcessorArchitecture)value;

            return ProcessorArchitecture.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsKnownProcessorArchitecture(
            ProcessorArchitecture processorArchitecture /* in */
            )
        {
            return processorArchitecture != ProcessorArchitecture.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ProcessorArchitecture GuessProcessorArchitecture()
        {
            return Is64BitProcess() ?
                ProcessorArchitecture.AMD64 : ProcessorArchitecture.Intel;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ProcessorArchitecture GuessProcessorArchitecture(
            OperatingSystem operatingSystem, /* in: OPTIONAL */
            string platformOrProcessorName   /* in: OPTIONAL */
            )
        {
            if (platformOrProcessorName == null)
            {
                platformOrProcessorName = QueryProcessorArchitecture(
                    true, null);

                CheckProcessorArchitecture(ref platformOrProcessorName);
            }

            string machineName = GetMachineName(
                platformOrProcessorName, IfNotFoundType.Null);

            foreach (string name in new string[] {
                    platformOrProcessorName, machineName
                })
            {
                if (name == null)
                    continue;

                ProcessorArchitecture? value;

                if (TryParseProcessorArchitecture(name, out value))
                    return (ProcessorArchitecture)value;
            }

            //
            // HACK: For Windows, just try to guess based on the
            //       the size of the IntPtr; for other platforms,
            //       do nothing.
            //
            if (ShouldTreatAsWindows(operatingSystem, false))
                return GuessProcessorArchitecture();

            //
            // BUGBUG: On any non-Windows platform (e.g. Linux,
            //         macOS, etc), we really have no idea what
            //         the processor is, i.e. at least from the
            //         perspective of this method.  Please see
            //         the platform-specific GetOsVersionInfo
            //         method overloads for details.
            //
            return ProcessorArchitecture.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldTreatAsWindows(
            OperatingSystem operatingSystem, /* in */
            bool @default                    /* in */
            )
        {
            if (operatingSystem == null)
                return @default;

            return ShouldTreatAsWindows(
                (OperatingSystemId)operatingSystem.Platform, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldTreatAsWindows(
            OperatingSystemId platformId, /* in */
            bool @default                 /* in */
            )
        {
            switch (platformId)
            {
                case OperatingSystemId.Win32s:
                case OperatingSystemId.Windows9x:
                case OperatingSystemId.WindowsNT:
                case OperatingSystemId.WindowsCE:
                    return true;
                case OperatingSystemId.Unix:
                    return false;
                case OperatingSystemId.Xbox:
                    return true;
                case OperatingSystemId.Darwin:
                case OperatingSystemId.Mono_on_Unix:
                    return false;
                default:
                    return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static PlatformID GetOperatingSystemPlatformId()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystem == null)
                    return (PlatformID)(int)OperatingSystemId.Unknown;

                return operatingSystem.Platform;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static Version GetOperatingSystemVersion()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystem == null)
                    return null;

                return operatingSystem.Version;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static VER_PRODUCT_TYPE GetOperatingSystemProductType()
        {
            //
            // TODO: No pure managed way to obtain this information?
            //
            return VER_PRODUCT_TYPE.VER_NT_UNKNOWN;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnSignalSetupEventOrComplain(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return;

            ReturnCode unsignalCode;
            Result unsignalError = null;

            try
            {
                unsignalCode = interpreter.UnSignalSetupEvent(
                    ref unsignalError);
            }
            catch (Exception e)
            {
                unsignalError = e;
                unsignalCode = ReturnCode.Error;
            }

            if (unsignalCode != ReturnCode.Ok)
            {
                DebugOps.Complain(
                    interpreter, unsignalCode, unsignalError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SignalSetupEventOrComplain(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return;

            ReturnCode signalCode;
            Result signalError = null;

            try
            {
                signalCode = interpreter.SignalSetupEvent(
                    ref signalError);
            }
            catch (Exception e)
            {
                signalError = e;
                signalCode = ReturnCode.Error;
            }

            if (signalCode != ReturnCode.Ok)
            {
                DebugOps.Complain(
                    interpreter, signalCode, signalError);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Operating System Detection Support Methods
        public static bool IsUnixOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                return ((operatingSystemId == OperatingSystemId.Unix) ||
                    (operatingSystemId == OperatingSystemId.Darwin) ||
                    (operatingSystemId == OperatingSystemId.Mono_on_Unix));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsMacintoshOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                if (operatingSystemId == OperatingSystemId.Darwin)
                    return true;

                //
                // HACK: This is mostly to support running on .NET Core
                //       on the Mac OS X operating system.
                //
                if (SharedStringOps.SystemNoCaseEquals(
                        operatingSystemName, DarwinOperatingSystemName))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsLinuxOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                if (operatingSystemId != OperatingSystemId.Unix)
                    return false;

                if (SharedStringOps.SystemNoCaseEquals(
                        operatingSystemName, LinuxOperatingSystemName))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                return ((operatingSystemId == OperatingSystemId.Windows9x) ||
                    (operatingSystemId == OperatingSystemId.WindowsNT));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static OperatingSystemId GuessOperatingSystemId()
        {
            if (IsWindowsOperatingSystem())
                return OperatingSystemId.WindowsNT;
            else if (IsMacintoshOperatingSystem())
                return OperatingSystemId.Darwin;
            else if (IsUnixOperatingSystem())
                return OperatingSystemId.Unix;

            return OperatingSystemId.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsVistaOrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindowsVistaOrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows81()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows81(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows10(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10OrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows10OrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10NovemberUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10NovemberUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10AnniversaryUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10AnniversaryUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10CreatorsUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10CreatorsUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10FallCreatorsUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10FallCreatorsUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10April2018Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10April2018UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10October2018Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10October2018UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10May2019Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10May2019UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10November2019Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10November2019UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10May2020Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10May2020UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10October2020Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10October2020UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10May2021Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10May2021UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10November2021Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10November2021UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10October2022Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10October2022UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows11()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows11(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows11OrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows11OrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows11September2022Update()
        {
            Version osVersion = null;

            if (!IsWindows11OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows11September2022UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows11September2023Update()
        {
            Version osVersion = null;

            if (!IsWindows11OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows11September2023UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServer2012R2()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            //
            // HACK: Windows Server 2012 R2 has the same version as
            //       Windows 8.1.
            //
            return IsWindows81();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServer2016() /* IsWindowsServerVersion1607() */
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1607");
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServerVersion1709()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1709");
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServerVersion1803()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1803");
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServerVersion1809()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1809");
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServerVersion1903()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1903");
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsServer2022() /* IsWindowsServerVersion21H2() */
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("21H2");
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool CheckVersion(
            PlatformID platformId,
            int major,
            int minor,
            short servicePackMajor,
            short servicePackMinor
            )
        {
            if (GetOperatingSystemPlatformId() == platformId)
            {
                Version osVersion = GetOperatingSystemVersion();

                if (osVersion != null)
                {
                    if (osVersion.Major > major)
                    {
                        return true;
                    }
                    else if ((osVersion.Major == major) &&
                        (osVersion.Minor > minor))
                    {
                        return true;
                    }
                    else if ((osVersion.Major == major) &&
                        (osVersion.Minor == minor))
                    {
                        ushort osServicePackMajor;
                        ushort osServicePackMinor;

                        lock (syncRoot)
                        {
                            osServicePackMajor = versionInfo.wServicePackMajor;
                            osServicePackMinor = versionInfo.wServicePackMinor;
                        }

                        if (osServicePackMajor > servicePackMajor)
                        {
                            return true;
                        }
                        else if ((osServicePackMajor == servicePackMajor) &&
                            (osServicePackMinor >= servicePackMinor))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Process Bits Querying Methods
        public static int GetProcessBits() // (e.g. 32, 64, etc)
        {
            return (IntPtr.Size * ConversionOps.ByteBits);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Is32BitProcess()
        {
            return (IntPtr.Size == sizeof(uint));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Is64BitProcess()
        {
            return (IntPtr.Size == sizeof(ulong));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Name Lookup Methods
#if NATIVE
        public static string GetAlternateProcessorName(
            string platformOrProcessorName,
            IfNotFoundType notFoundType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string processorName;

                if ((platformOrProcessorName != null) &&
                    (alternateProcessorNames != null) &&
                    alternateProcessorNames.TryGetValue(
                        platformOrProcessorName, out processorName))
                {
                    return processorName;
                }
            }

            if (notFoundType == IfNotFoundType.Null)
                return null;

            if (notFoundType == IfNotFoundType.Unknown)
                return UnknownName;

            return platformOrProcessorName;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Operating System Detection Support Methods
        private static bool IsWindowsServerOperatingSystem()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((productType == VER_PRODUCT_TYPE.VER_NT_DOMAIN_CONTROLLER) ||
                    (productType == VER_PRODUCT_TYPE.VER_NT_SERVER));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindowsReleaseId(
            string releaseId
            )
        {
            string localReleaseId = GetOperatingSystemReleaseId();

            if (localReleaseId == null)
                return false;

            return SharedStringOps.SystemEquals(localReleaseId, releaseId);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindowsVistaOrHigher(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if (osVersion.Major >= 6) /* Windows Vista = 6.0 */
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindows81(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if ((osVersion.Major == 6) && (osVersion.Minor == 3))
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindows10(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if ((osVersion.Major == 10) && (osVersion.Minor == 0))
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindows10OrHigher(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if (osVersion.Major >= 10) /* Windows 10 = 10.0 */
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindows11(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if ((osVersion.Major == 10) && (osVersion.Minor == 0) &&
                        (osVersion.Build >= Windows11RtmBuildNumber))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindows11OrHigher(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if (osVersion.Major > 10)
                        return true;

                    if (osVersion.Major < 10)
                        return false;

                    if (osVersion.Minor > 0)
                        return true;

                    if (osVersion.Minor < 0)
                        return false;

                    if (osVersion.Build >= Windows11RtmBuildNumber)
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringList GetInstalledUpdates(
            Interpreter interpreter, /* in: OPTIONAL */
            bool asynchronous        /* in */
            )
        {
            //
            // HACK: For now, full introspection of installed updates is
            //       only supported on Windows.
            //
            if (IsWindowsOperatingSystem())
                return WindowsGetInstalledUpdates(interpreter, asynchronous);

            TraceOps.DebugTrace(
                "GetInstalledUpdates: not supported on this platform",
                typeof(PlatformOps).Name, TracePriority.PlatformError);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringList WindowsGetInstalledUpdates(
            Interpreter interpreter, /* in: OPTIONAL */
            bool asynchronous        /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (installedUpdates != null)
                    return new StringList(installedUpdates);
            }

            //
            // HACK: When operating in synchronous mode, skip doing the
            //       time consuming portion of this method (i.e. cached
            //       values are still used, if available; otherwise, it
            //       it will be done asynchronously later).
            //
            if (!asynchronous)
                return null;

            //
            // HACK: For now, full introspection of installed updates is
            //       only supported on Windows.
            //
            if (!IsWindowsOperatingSystem())
                return null;

            try
            {
                //
                // HACK: The "wmic.exe" executable is not available on
                //       Windows 2000 or Windows XP Home Edition; so,
                //       check if it exists prior to attempting to use
                //       it.
                //
                string fileName = CommonOps.Environment.ExpandVariables(
                    WmiQfeGetUpdatesCommandFileName);

                if (!File.Exists(fileName))
                    return null;

                string arguments = WmiQfeGetUpdatesCommandArguments;

                EventFlags eventFlags = (interpreter != null) ?
                    interpreter.EngineEventFlags : EventFlags.None;

                ExitCode exitCode = ResultOps.SuccessExitCode();
                ReturnCode code;
                Result result = null;
                Result error = null; /* REUSED */

                code = ProcessOps.ExecuteProcess(
                    interpreter, fileName, arguments, eventFlags,
                    ref exitCode, ref result, ref error);

                if ((code == ReturnCode.Ok) &&
                    (exitCode == ResultOps.SuccessExitCode()))
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        error = null;

                        installedUpdates = StringList.FromString(
                            result, ref error);

                        if (installedUpdates != null)
                        {
                            if ((installedUpdates.Count > 0) &&
                                SharedStringOps.SystemNoCaseEquals(
                                    installedUpdates[0], WmiQfePropertyName))
                            {
                                installedUpdates.RemoveAt(0);
                            }

                            int count = installedUpdates.Count;

                            TraceOps.DebugTrace(String.Format(
                                "WindowsGetInstalledUpdates: got {0}, " +
                                "interpreter = {1}, code = {2}, " +
                                "exitCode = {3}, result = {4}, " +
                                "error = {5}", count,
                                FormatOps.InterpreterNoThrow(interpreter),
                                code, exitCode, FormatOps.WrapOrNull(true,
                                true, result), FormatOps.WrapOrNull(true,
                                true, error)), typeof(PlatformOps).Name,
                                TracePriority.PlatformDebug);

                            return new StringList(installedUpdates);
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "WindowsGetInstalledUpdates: invalid, " +
                                "interpreter = {0}, code = {1}, " +
                                "exitCode = {2}, result = {3}, " +
                                "error = {4}",
                                FormatOps.InterpreterNoThrow(interpreter),
                                code, exitCode, FormatOps.WrapOrNull(true,
                                true, result), FormatOps.WrapOrNull(true,
                                true, error)), typeof(PlatformOps).Name,
                                TracePriority.PlatformDebug);
                        }
                    }
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "WindowsGetInstalledUpdates: failure, " +
                        "interpreter = {0}, code = {1}, " +
                        "exitCode = {2}, result = {3}, " +
                        "error = {4}",
                        FormatOps.InterpreterNoThrow(interpreter),
                        code, exitCode, FormatOps.WrapOrNull(true,
                        true, result), FormatOps.WrapOrNull(true,
                        true, error)), typeof(PlatformOps).Name,
                        TracePriority.PlatformError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PlatformOps).Name,
                    TracePriority.PlatformError);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: This method and its caller assume that there can be only
        //         one "named update" installed at a time.
        //
        private static string GetWindows10UpdateName(
            Version osVersion
            )
        {
            if (osVersion != null)
            {
                switch (osVersion.Build)
                {
                    case Windows10RtmBuildNumber:
                        return Windows10RtmName;
                    case Windows10NovemberUpdateBuildNumber:
                        return Windows10NovemberUpdateName;
                    case Windows10AnniversaryUpdateBuildNumber:
                        return Windows10AnniversaryUpdateName;
                    case Windows10CreatorsUpdateBuildNumber:
                        return Windows10CreatorsUpdateName;
                    case Windows10FallCreatorsUpdateBuildNumber:
                        return Windows10FallCreatorsUpdateName;
                    case Windows10April2018UpdateBuildNumber:
                        return Windows10April2018UpdateName;
                    case Windows10October2018UpdateBuildNumber:
                        return Windows10October2018UpdateName;
                    case Windows10May2019UpdateBuildNumber:
                        return Windows10May2019UpdateName;
                    case Windows10November2019UpdateBuildNumber:
                        return Windows10November2019UpdateName;
                    case Windows10May2020UpdateBuildNumber:
                        return Windows10May2020UpdateName;
                    case Windows10October2020UpdateBuildNumber:
                        return Windows10October2020UpdateName;
                    case Windows10May2021UpdateBuildNumber:
                        return Windows10May2021UpdateName;
                    case Windows10November2021UpdateBuildNumber:
                        return Windows10November2021UpdateName;
                    case Windows10October2022UpdateBuildNumber:
                        return Windows10October2022UpdateName;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: This method and its caller assume that there can be only
        //         one "named update" installed at a time.
        //
        private static string GetWindows11UpdateName(
            Version osVersion
            )
        {
            if (osVersion != null)
            {
                switch (osVersion.Build)
                {
                    case Windows11RtmBuildNumber:
                        return Windows11RtmName;
                    case Windows11September2022UpdateBuildNumber:
                        return Windows11September2022UpdateName;
                    case Windows11September2023UpdateBuildNumber:
                        return Windows11September2023UpdateName;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Name Lookup Methods
        private static string GetMachineName(
            string platformOrProcessorName,
            IfNotFoundType notFoundType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string machineName;

                if ((platformOrProcessorName != null) &&
                    (machineNames != null) &&
                    machineNames.TryGetValue(
                        platformOrProcessorName, out machineName))
                {
                    return machineName;
                }
            }

            if (notFoundType == IfNotFoundType.Null)
                return null;

            if (notFoundType == IfNotFoundType.Unknown)
                return UnknownName;

            return platformOrProcessorName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetProcessorName(
            ProcessorArchitecture processorArchitecture,
            IfNotFoundType notFoundType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                ProcessorArchitecture count = (ProcessorArchitecture)
                    processorNames.Count;

                if ((processorArchitecture >= 0) &&
                    (processorArchitecture < count))
                {
                    return processorNames[(int)processorArchitecture];
                }
            }

            if (notFoundType == IfNotFoundType.Null)
                return null;

            return UnknownName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetOperatingSystemName(
            OperatingSystemId platformId,
            IfNotFoundType notFoundType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                OperatingSystemId count = (OperatingSystemId)
                    operatingSystemNames.Count;

                if ((platformId >= 0) && (platformId < count))
                {
                    return operatingSystemNames[(int)platformId];
                }
                else if (platformId == OperatingSystemId.Mono_on_Unix)
                {
                    return platformId.ToString().Replace(
                        Characters.Underscore, Characters.Space);
                }
            }

            if (notFoundType == IfNotFoundType.Null)
                return null;

            return UnknownName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetPlatformName(
            OperatingSystemId platformId,
            IfNotFoundType notFoundType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                OperatingSystemId count = (OperatingSystemId)
                    platformNames.Count;

                if ((platformId >= 0) && (platformId < count))
                    return platformNames[(int)platformId];
                else if (platformId == OperatingSystemId.Mono_on_Unix)
                    return OperatingSystemId.Unix.ToString();
            }

            if (notFoundType == IfNotFoundType.Null)
                return null;

            return UnknownName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetProductTypeName(
            VER_PRODUCT_TYPE productType,
            IfNotFoundType notFoundType
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                VER_PRODUCT_TYPE count = (VER_PRODUCT_TYPE)
                    productTypeNames.Count;

                if ((productType >= 0) && (productType < count))
                    return productTypeNames[(int)productType];
            }

            if (notFoundType == IfNotFoundType.Null)
                return null;

            return UnknownName;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Win32 Support Methods
#if NATIVE && WINDOWS
        private static bool GetSystemInfo(
            ref UnsafeNativeMethods.SYSTEM_INFO systemInfo
            )
        {
            try
            {
                /* CANNOT FAIL? */
                UnsafeNativeMethods.GetSystemInfo(ref systemInfo);

                return true;
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetOsVersionInfo(
            ref UnsafeNativeMethods.OSVERSIONINFOEX versionInfo
            )
        {
            try
            {
                versionInfo.dwOSVersionInfoSize = (uint)Marshal.SizeOf(
                    versionInfo);

                return UnsafeNativeMethods.GetVersionEx(ref versionInfo);
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWin32onWin64()
        {
            try
            {
                Process process = ProcessOps.GetCurrent();

                if (process != null)
                {
                    bool wow64Process = false;

                    if (UnsafeNativeMethods.IsWow64Process(
                            process.Handle, ref wow64Process))
                    {
                        return wow64Process;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Unix Support Methods
#if NATIVE && UNIX
        private static bool GetOsVersionInfo(
            ref UnsafeNativeMethods.utsname utsName
            )
        {
            try
            {
                UnsafeNativeMethods.utsname_interop utfNameInterop;

                if (UnsafeNativeMethods.uname(out utfNameInterop) < 0)
                    return false;

                if (utfNameInterop.buffer == null)
                    return false;

                string bufferAsString = Encoding.UTF8.GetString(
                    utfNameInterop.buffer);

                if ((bufferAsString == null) || (utsNameSeparators == null))
                    return false;

                bufferAsString = bufferAsString.Trim(utsNameSeparators);

                string[] parts = bufferAsString.Split(
                    utsNameSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (parts == null)
                    return false;

                UnsafeNativeMethods.utsname localUtsName =
                    new UnsafeNativeMethods.utsname();

                if (parts.Length >= 1)
                    localUtsName.sysname = parts[0];

                if (parts.Length >= 2)
                    localUtsName.nodename = parts[1];

                if (parts.Length >= 3)
                    localUtsName.release = parts[2];

                if (parts.Length >= 4)
                    localUtsName.version = parts[3];

                if (parts.Length >= 5)
                    localUtsName.machine = parts[4];

                utsName = localUtsName;
                return true;
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
#endif
        #endregion
    }
}
