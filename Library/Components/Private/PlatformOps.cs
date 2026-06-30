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
    /// <summary>
    /// This class provides static methods used to detect and describe the
    /// platform, processor architecture, and operating system that the
    /// current process is executing on.
    /// </summary>
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
        /// <summary>
        /// The operating system build number that corresponds to the original
        /// (RTM) release of Windows 10.
        /// </summary>
        private const int Windows10RtmBuildNumber = 10240;
        /// <summary>
        /// The operating system display name that corresponds to the original
        /// (RTM) release of Windows 10.
        /// </summary>
        private const string Windows10RtmName = "Windows 10, RTM";

        /// <summary>
        /// The operating system build number that corresponds to the November
        /// Update of Windows 10.
        /// </summary>
        private const int Windows10NovemberUpdateBuildNumber = 10586;
        /// <summary>
        /// The operating system display name that corresponds to the November
        /// Update of Windows 10.
        /// </summary>
        private const string Windows10NovemberUpdateName = "Windows 10, November Update";

        /// <summary>
        /// The operating system build number that corresponds to the
        /// Anniversary Update of Windows 10.
        /// </summary>
        private const int Windows10AnniversaryUpdateBuildNumber = 14393;
        /// <summary>
        /// The operating system display name that corresponds to the
        /// Anniversary Update of Windows 10.
        /// </summary>
        private const string Windows10AnniversaryUpdateName = "Windows 10, Anniversary Update";

        /// <summary>
        /// The operating system build number that corresponds to the Creators
        /// Update of Windows 10.
        /// </summary>
        private const int Windows10CreatorsUpdateBuildNumber = 15063;
        /// <summary>
        /// The operating system display name that corresponds to the Creators
        /// Update of Windows 10.
        /// </summary>
        private const string Windows10CreatorsUpdateName = "Windows 10, Creators Update";

        /// <summary>
        /// The operating system build number that corresponds to the Fall
        /// Creators Update of Windows 10.
        /// </summary>
        private const int Windows10FallCreatorsUpdateBuildNumber = 16299;
        /// <summary>
        /// The operating system display name that corresponds to the Fall
        /// Creators Update of Windows 10.
        /// </summary>
        private const string Windows10FallCreatorsUpdateName = "Windows 10, Fall Creators Update";

        /// <summary>
        /// The operating system build number that corresponds to the April
        /// 2018 Update of Windows 10.
        /// </summary>
        private const int Windows10April2018UpdateBuildNumber = 17134;
        /// <summary>
        /// The operating system display name that corresponds to the April
        /// 2018 Update of Windows 10.
        /// </summary>
        private const string Windows10April2018UpdateName = "Windows 10, April 2018 Update";

        /// <summary>
        /// The operating system build number that corresponds to the October
        /// 2018 Update of Windows 10.
        /// </summary>
        private const int Windows10October2018UpdateBuildNumber = 17763;
        /// <summary>
        /// The operating system display name that corresponds to the October
        /// 2018 Update of Windows 10.
        /// </summary>
        private const string Windows10October2018UpdateName = "Windows 10, October 2018 Update";

        /// <summary>
        /// The operating system build number that corresponds to the May 2019
        /// Update of Windows 10.
        /// </summary>
        private const int Windows10May2019UpdateBuildNumber = 18362;
        /// <summary>
        /// The operating system display name that corresponds to the May 2019
        /// Update of Windows 10.
        /// </summary>
        private const string Windows10May2019UpdateName = "Windows 10, May 2019 Update";

        /// <summary>
        /// The operating system build number that corresponds to the November
        /// 2019 Update of Windows 10.
        /// </summary>
        private const int Windows10November2019UpdateBuildNumber = 18363;
        /// <summary>
        /// The operating system display name that corresponds to the November
        /// 2019 Update of Windows 10.
        /// </summary>
        private const string Windows10November2019UpdateName = "Windows 10, November 2019 Update";

        /// <summary>
        /// The operating system build number that corresponds to the May 2020
        /// Update of Windows 10.
        /// </summary>
        private const int Windows10May2020UpdateBuildNumber = 19041;
        /// <summary>
        /// The operating system display name that corresponds to the May 2020
        /// Update of Windows 10.
        /// </summary>
        private const string Windows10May2020UpdateName = "Windows 10, May 2020 Update";

        /// <summary>
        /// The operating system build number that corresponds to the October
        /// 2020 Update of Windows 10.
        /// </summary>
        private const int Windows10October2020UpdateBuildNumber = 19042;
        /// <summary>
        /// The operating system display name that corresponds to the October
        /// 2020 Update of Windows 10.
        /// </summary>
        private const string Windows10October2020UpdateName = "Windows 10, October 2020 Update";

        /// <summary>
        /// The operating system build number that corresponds to the May 2021
        /// Update of Windows 10.
        /// </summary>
        private const int Windows10May2021UpdateBuildNumber = 19043;
        /// <summary>
        /// The operating system display name that corresponds to the May 2021
        /// Update of Windows 10.
        /// </summary>
        private const string Windows10May2021UpdateName = "Windows 10, May 2021 Update";

        /// <summary>
        /// The operating system build number that corresponds to the November
        /// 2021 Update of Windows 10.
        /// </summary>
        private const int Windows10November2021UpdateBuildNumber = 19044;
        /// <summary>
        /// The operating system display name that corresponds to the November
        /// 2021 Update of Windows 10.
        /// </summary>
        private const string Windows10November2021UpdateName = "Windows 10, November 2021 Update";

        /// <summary>
        /// The operating system build number that corresponds to the October
        /// 2022 Update of Windows 10.
        /// </summary>
        private const int Windows10October2022UpdateBuildNumber = 19045;
        /// <summary>
        /// The operating system display name that corresponds to the October
        /// 2022 Update of Windows 10.
        /// </summary>
        private const string Windows10October2022UpdateName = "Windows 10, October 2022 Update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows 11 Update Constants
        /// <summary>
        /// The operating system build number that corresponds to the original
        /// (RTM) release of Windows 11.
        /// </summary>
        private const int Windows11RtmBuildNumber = 22000;
        /// <summary>
        /// The operating system display name that corresponds to the original
        /// (RTM) release of Windows 11.
        /// </summary>
        private const string Windows11RtmName = "Windows 11, RTM";

        /// <summary>
        /// The operating system build number that corresponds to the
        /// September 2022 Update of Windows 11.
        /// </summary>
        private const int Windows11September2022UpdateBuildNumber = 22621;
        /// <summary>
        /// The operating system display name that corresponds to the
        /// September 2022 Update of Windows 11.
        /// </summary>
        private const string Windows11September2022UpdateName = "Windows 11, September 2022 Update";

        /// <summary>
        /// The operating system build number that corresponds to the October
        /// 2023 Update of Windows 11.
        /// </summary>
        private const int Windows11October2023UpdateBuildNumber = 22631;
        /// <summary>
        /// The operating system display name that corresponds to the October
        /// 2023 Update of Windows 11.
        /// </summary>
        private const string Windows11October2023UpdateName = "Windows 11, October 2023 Update";

        /// <summary>
        /// The operating system build number that corresponds to the October
        /// 2024 Update of Windows 11.
        /// </summary>
        private const int Windows11October2024UpdateBuildNumber = 26100;
        /// <summary>
        /// The operating system display name that corresponds to the October
        /// 2024 Update of Windows 11.
        /// </summary>
        private const string Windows11October2024UpdateName = "Windows 11, October 2024 Update";

        /// <summary>
        /// The operating system build number that corresponds to the
        /// September 2025 Update of Windows 11.
        /// </summary>
        private const int Windows11September2025UpdateBuildNumber = 26200;
        /// <summary>
        /// The operating system display name that corresponds to the
        /// September 2025 Update of Windows 11.
        /// </summary>
        private const string Windows11September2025UpdateName = "Windows 11, September 2025 Update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows Version Registry Constants
#if !NET_STANDARD_20
        /// <summary>
        /// The name of the registry subkey, under the local machine root key,
        /// that contains the current operating system version information.
        /// </summary>
        private const string OsVersionSubKeyName =
            "Software\\Microsoft\\Windows NT\\CurrentVersion";

        /// <summary>
        /// The name of the registry value that contains the operating system
        /// product name.
        /// </summary>
        private const string ProductNameValueName = "ProductName";
        /// <summary>
        /// The name of the registry value that contains the operating system
        /// release identifier.
        /// </summary>
        private const string ReleaseIdValueName = "ReleaseId";
        /// <summary>
        /// The name of the registry value that contains the current operating
        /// system type.
        /// </summary>
        private const string CurrentTypeValueName = "CurrentType";
        /// <summary>
        /// The name of the registry value that contains the operating system
        /// installation type.
        /// </summary>
        private const string InstallationTypeValueName = "InstallationType";
        /// <summary>
        /// The name of the registry value that contains the extended build
        /// lab information.
        /// </summary>
        private const string BuildLabExValueName = "BuildLabEx";
        /// <summary>
        /// The name of the registry value that contains the operating system
        /// installation date.
        /// </summary>
        private const string InstallDateValueName = "InstallDate";
        /// <summary>
        /// The name used when reporting the list of named operating system
        /// updates.
        /// </summary>
        private const string UpdateNamesValueName = "UpdateNames";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WMI Query (for Windows Update) Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The file name of the WMI command line utility used to query the
        /// list of installed operating system updates.
        /// </summary>
        private static string WmiQfeGetUpdatesCommandFileName =
            "%SystemRoot%\\System32\\wbem\\wmic.exe"; // BUGBUG: Constant?

        /// <summary>
        /// The name of the property emitted by the WMI command that contains
        /// the hotfix identifier.
        /// </summary>
        private static string WmiQfePropertyName = "HotFixID";

        /// <summary>
        /// The command line arguments passed to the WMI command line utility
        /// to query the list of installed operating system updates.
        /// </summary>
        private static string WmiQfeGetUpdatesCommandArguments =
            "QFE GET HotFixID";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region PowerShell (for Windows Update) Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The file name of the Windows PowerShell executable used to query
        /// the list of installed operating system updates.
        /// </summary>
        private static string PowerShellQfeGetUpdatesCommandFileName =
            "%SystemRoot%\\System32\\WindowsPowerShell\\v1.0\\PowerShell.exe"; // BUGBUG: Constant?

        /// <summary>
        /// The name of the property emitted by the PowerShell command that
        /// contains the hotfix identifier.
        /// </summary>
        private static string PowerShellQfePropertyName = "HotFixID";

        /// <summary>
        /// The separator used between a property name and its value in the
        /// output produced by the PowerShell command.
        /// </summary>
        private static string PowerShellQfeValueSeparator =
            Characters.Colon.ToString();

        /// <summary>
        /// The command line arguments passed to Windows PowerShell to query
        /// the list of installed operating system updates.
        /// </summary>
        private static string PowerShellQfeGetUpdatesCommandArguments =
            "Get-HotFix | Format-List -Property HotFixID";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default native memory page size, in bytes, to assume when it
        /// cannot be otherwise determined.
        /// </summary>
        private const uint defaultPageSize = 4096; /* COMPAT: x86. */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The display name used for the Win32s operating system.
        /// </summary>
        private static readonly string Win32SubsetOperatingSystemName = "Win32s";
        /// <summary>
        /// The display name used for the Windows 9x operating system.
        /// </summary>
        private static readonly string Windows9xOperatingSystemName = "Windows 9x";
        /// <summary>
        /// The display name used for the Windows NT operating system.
        /// </summary>
        private static readonly string WindowsNtOperatingSystemName = "Windows NT";
        /// <summary>
        /// The display name used for the Windows CE operating system.
        /// </summary>
        private static readonly string WindowsCeOperatingSystemName = "Windows CE";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The display name used for the Unix operating system.
        /// </summary>
        private static readonly string UnixOperatingSystemName = "Unix";
        /// <summary>
        /// The display name used for the Xbox operating system.
        /// </summary>
        private static readonly string XboxOperatingSystemName = "Xbox";
        /// <summary>
        /// The display name used for the Darwin (macOS) operating system.
        /// </summary>
        private static readonly string DarwinOperatingSystemName = "Darwin";
        /// <summary>
        /// The display name used for the Linux operating system.
        /// </summary>
        private static readonly string LinuxOperatingSystemName = "Linux";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The value used to represent a name that is not known.
        /// </summary>
        private static readonly string UnknownName = "unknown";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of known processor names, indexed by processor
        /// architecture.
        /// </summary>
        private static StringList processorNames = null;
        /// <summary>
        /// The list of platform names, indexed by operating system
        /// identifier.
        /// </summary>
        private static StringList platformNames = null;
        /// <summary>
        /// The list of product type names, indexed by product type.
        /// </summary>
        private static StringList productTypeNames = null;
        /// <summary>
        /// The list of operating system names, indexed by operating system
        /// identifier.
        /// </summary>
        private static StringList operatingSystemNames = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The mapping of platform or processor names to their normalized
        /// machine names.
        /// </summary>
        private static IDictionary<string, string> machineNames = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to extract the leading major and minor
        /// version components from an operating system version string.
        /// </summary>
        private static Regex majorMinorRegEx = RegExOps.Create("^\\d+\\.\\d+");

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// The mapping of platform or processor names to their alternate
        /// processor names.
        /// </summary>
        private static IDictionary<string, string> alternateProcessorNames = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the static state of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();
        /// <summary>
        /// Non-zero if the static state of this class has been initialized.
        /// </summary>
        private static bool initialized = false;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// The cached native system information for the current process.
        /// </summary>
        private static UnsafeNativeMethods.SYSTEM_INFO systemInfo;
        /// <summary>
        /// The cached native operating system version information for the
        /// current process.
        /// </summary>
        private static UnsafeNativeMethods.OSVERSIONINFOEX versionInfo;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
        /// <summary>
        /// The characters used to separate the individual fields within the
        /// native uname buffer.
        /// </summary>
        private static readonly char[] utsNameSeparators = {
            Characters.Null
        };

        /// <summary>
        /// The cached native uname information for the current process.
        /// </summary>
        private static UnsafeNativeMethods.utsname utsName;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The detected processor architecture for the current process.
        /// </summary>
        private static ProcessorArchitecture processorArchitecture =
            ProcessorArchitecture.Unknown;

        /// <summary>
        /// The detected operating system identifier for the current process.
        /// </summary>
        private static OperatingSystemId operatingSystemId =
            OperatingSystemId.Unknown;

        /// <summary>
        /// The detected operating system product type for the current
        /// process.
        /// </summary>
        private static VER_PRODUCT_TYPE productType =
            VER_PRODUCT_TYPE.VER_NT_NONE;

        /// <summary>
        /// The detected native memory page size, in bytes.
        /// </summary>
        private static uint pageSize = defaultPageSize;

        /// <summary>
        /// The lowest memory address available to applications.
        /// </summary>
        private static IntPtr minimumApplicationAddress = IntPtr.Zero;
        /// <summary>
        /// The highest memory address available to applications.
        /// </summary>
        private static IntPtr maximumApplicationAddress = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The operating system that the current process is executing on.
        /// </summary>
        private static OperatingSystem operatingSystem = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The primary name of the processor that the current process is
        /// executing on.
        /// </summary>
        private static string processorName = null;
        /// <summary>
        /// The primary name of the machine that the current process is
        /// executing on.
        /// </summary>
        private static string machineName = null;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// The alternate name of the processor that the current process is
        /// executing on.
        /// </summary>
        private static string alternateProcessorName = null;
        /// <summary>
        /// The alternate name of the machine that the current process is
        /// executing on.
        /// </summary>
        private static string alternateMachineName = null;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the platform that the current process is executing on.
        /// </summary>
        private static string platformName = null;
        /// <summary>
        /// The name of the operating system product type that the current
        /// process is executing on.
        /// </summary>
        private static string productTypeName = null;
        /// <summary>
        /// The name of the operating system that the current process is
        /// executing on.
        /// </summary>
        private static string operatingSystemName = null;
        /// <summary>
        /// The version of the operating system that the current process is
        /// executing on.
        /// </summary>
        private static string operatingSystemVersion = null;
        /// <summary>
        /// The extra service pack, or version, information for the operating
        /// system that the current process is executing on.
        /// </summary>
        private static string operatingSystemServicePack = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the current process is a 32-bit process running on a
        /// 64-bit version of Windows (WoW64).
        /// </summary>
        private static bool isWin32onWin64 = false;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached list of installed operating system updates.
        /// </summary>
        private static StringList installedUpdates = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        #region Private Unsafe Native Methods Class
        /// <summary>
        /// This class contains the native methods, and related types, that
        /// are used (via P/Invoke) by the platform detection logic in the
        /// containing class.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("259417ba-e318-4982-b2c0-9f6fd4196b74")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            /// <summary>
            /// This structure contains information about the current computer
            /// system, corresponding to the native SYSTEM_INFO structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("a858d038-1313-43d2-a9b0-7a00b2975933")]
            internal struct SYSTEM_INFO
            {
                /// <summary>
                /// The processor architecture of the installed operating
                /// system.
                /// </summary>
                public ProcessorArchitecture wProcessorArchitecture;
                /// <summary>
                /// Reserved for future use.
                /// </summary>
                public ushort wReserved;
                /// <summary>
                /// The page size and the granularity of page protection and
                /// commitment.
                /// </summary>
                public uint dwPageSize;
                /// <summary>
                /// The lowest memory address accessible to applications and
                /// dynamic-link libraries (DLLs).
                /// </summary>
                public IntPtr lpMinimumApplicationAddress;
                /// <summary>
                /// The highest memory address accessible to applications and
                /// dynamic-link libraries (DLLs).
                /// </summary>
                public IntPtr lpMaximumApplicationAddress;
                /// <summary>
                /// A mask representing the set of processors configured into
                /// the system.
                /// </summary>
                public UIntPtr dwActiveProcessorMask;
                /// <summary>
                /// The number of logical processors in the current group.
                /// </summary>
                public uint dwNumberOfProcessors;
                /// <summary>
                /// The processor type, retained for compatibility.
                /// </summary>
                public uint dwProcessorType;
                /// <summary>
                /// The granularity for the starting address at which virtual
                /// memory can be allocated.
                /// </summary>
                public uint dwAllocationGranularity;
                /// <summary>
                /// The architecture-dependent processor level.
                /// </summary>
                public ushort wProcessorLevel;
                /// <summary>
                /// The architecture-dependent processor revision.
                /// </summary>
                public ushort wProcessorRevision;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Yes, this has been tested and the size must be exactly
            //       148 bytes.
            //
            /// <summary>
            /// This structure contains operating system version information,
            /// corresponding to the native OSVERSIONINFOEX structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            [ObjectId("f33f3aa7-8ddc-48a0-8ac9-e950dcbaaac1")]
            internal struct OSVERSIONINFOEX
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public uint dwOSVersionInfoSize;
                /// <summary>
                /// The major version number of the operating system.
                /// </summary>
                public uint dwMajorVersion;
                /// <summary>
                /// The minor version number of the operating system.
                /// </summary>
                public uint dwMinorVersion;
                /// <summary>
                /// The build number of the operating system.
                /// </summary>
                public uint dwBuildNumber;
                /// <summary>
                /// The operating system platform identifier.
                /// </summary>
                public OperatingSystemId dwPlatformId;
                /// <summary>
                /// A string that contains the service pack, or other extra
                /// version, information for the operating system.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string szCSDVersion;
                /// <summary>
                /// The major version number of the latest service pack
                /// installed on the operating system.
                /// </summary>
                public ushort wServicePackMajor;
                /// <summary>
                /// The minor version number of the latest service pack
                /// installed on the operating system.
                /// </summary>
                public ushort wServicePackMinor;
                /// <summary>
                /// A bit mask that identifies the product suites available on
                /// the operating system.
                /// </summary>
                public short wSuiteMask;
                /// <summary>
                /// The product type of the operating system.
                /// </summary>
                public VER_PRODUCT_TYPE wProductType;
                /// <summary>
                /// Reserved for future use.
                /// </summary>
                public byte wReserved;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves information about the current computer
            /// system.
            /// </summary>
            /// <param name="systemInfo">
            /// Upon success, receives the system information for the current
            /// computer.
            /// </param>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern void GetSystemInfo(
                ref SYSTEM_INFO systemInfo
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method retrieves information about the version of the
            /// currently running operating system.
            /// </summary>
            /// <param name="versionInfo">
            /// Upon success, receives the operating system version
            /// information; the size field of this structure must be
            /// initialized prior to the call.
            /// </param>
            /// <returns>
            /// True if the version information was retrieved successfully;
            /// otherwise, false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetVersionEx(
                ref OSVERSIONINFOEX versionInfo
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the specified process is
            /// running under WOW64.
            /// </summary>
            /// <param name="hProcess">
            /// The handle to the process to check.
            /// </param>
            /// <param name="wow64Process">
            /// Upon success, set to non-zero if the process is running under
            /// WOW64; otherwise, set to zero.
            /// </param>
            /// <returns>
            /// True if the function succeeds; otherwise, false.
            /// </returns>
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
            /// <summary>
            /// This structure contains operating system name and version
            /// information, corresponding to the native utsname structure
            /// used on Unix-like systems.
            /// </summary>
            [ObjectId("4c41ee57-ee1d-4db6-8735-a4d78dd810b9")]
            internal struct utsname
            {
                /// <summary>
                /// The name of this implementation of the operating system.
                /// </summary>
                public string sysname;  /* Name of this implementation of
                                         * the operating system. */
                /// <summary>
                /// The name of this node within the communications network,
                /// if any.
                /// </summary>
                public string nodename; /* Name of this node within the
                                         * communications network to which
                                         * this node is attached, if any. */
                /// <summary>
                /// The current release level of this implementation.
                /// </summary>
                public string release;  /* Current release level of this
                                         * implementation. */
                /// <summary>
                /// The current version level of this release.
                /// </summary>
                public string version;  /* Current version level of this
                                         * release. */
                /// <summary>
                /// The name of the hardware type on which the system is
                /// running.
                /// </summary>
                public string machine;  /* Name of the hardware type on
                                         * which the system is running. */
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure is used to marshal the raw, native uname buffer
            /// from unmanaged code.
            /// </summary>
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
                /// <summary>
                /// The raw buffer containing the zero-terminated uname
                /// fields.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
                public byte[] buffer;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method obtains the native uname information for the
            /// current system.
            /// </summary>
            /// <param name="name">
            /// Upon success, receives the marshaled uname information.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a negative value.
            /// </returns>
            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int uname(out utsname_interop name);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Static Constructor
        /// <summary>
        /// This static constructor initializes the static state of this
        /// class.
        /// </summary>
        static PlatformOps()
        {
            Initialize(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Initialization Methods
        /// <summary>
        /// This method initializes, or re-initializes, the cached platform,
        /// processor, and operating system information for the current
        /// process.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force re-initialization even when the information has
        /// already been initialized.
        /// </param>
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
        /// <summary>
        /// This method gets the detected processor architecture for the
        /// current process.
        /// </summary>
        /// <returns>
        /// The detected processor architecture.
        /// </returns>
        public static ProcessorArchitecture GetProcessorArchitecture()
        {
            lock (syncRoot)
            {
                return processorArchitecture;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the detected processor architecture
        /// is part of the Intel family.
        /// </summary>
        /// <returns>
        /// True if the processor architecture is part of the Intel family;
        /// otherwise, false.
        /// </returns>
        public static bool IsIntelProcessorArchitecture()
        {
            switch (GetProcessorArchitecture())
            {
                case ProcessorArchitecture.Intel:
                case ProcessorArchitecture.IA32_on_Win64:
                case ProcessorArchitecture.AMD64:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the processor architecture for the current
        /// process, falling back to the machine name when necessary.
        /// </summary>
        /// <returns>
        /// The processor architecture name, or null if it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method gets the detected native memory page size, in bytes.
        /// </summary>
        /// <returns>
        /// The native memory page size, in bytes.
        /// </returns>
        public static uint GetPageSize()
        {
            lock (syncRoot)
            {
                return pageSize;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the range of memory addresses available to
        /// applications, formatted as a string.
        /// </summary>
        /// <returns>
        /// The application address range, formatted as a string.
        /// </returns>
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

        /// <summary>
        /// This method gets the primary name of the machine that the current
        /// process is executing on.
        /// </summary>
        /// <returns>
        /// The primary machine name.
        /// </returns>
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
        /// <summary>
        /// This method gets the alternate name of the machine that the
        /// current process is executing on.
        /// </summary>
        /// <returns>
        /// The alternate machine name.
        /// </returns>
        public static string GetAlternateMachineName()
        {
            lock (syncRoot)
            {
                return alternateMachineName;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the primary name of the processor that the
        /// current process is executing on.
        /// </summary>
        /// <returns>
        /// The primary processor name.
        /// </returns>
        public static string GetProcessorName()
        {
            lock (syncRoot)
            {
                return processorName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// This method gets the alternate name of the processor that the
        /// current process is executing on.
        /// </summary>
        /// <returns>
        /// The alternate processor name.
        /// </returns>
        public static string GetAlternateProcessorName()
        {
            lock (syncRoot)
            {
                return alternateProcessorName;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the platform that the current process
        /// is executing on.
        /// </summary>
        /// <returns>
        /// The platform name.
        /// </returns>
        public static string GetPlatformName()
        {
            lock (syncRoot)
            {
                return platformName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the operating system product type
        /// that the current process is executing on.
        /// </summary>
        /// <returns>
        /// The product type name.
        /// </returns>
        public static string GetProductTypeName()
        {
            lock (syncRoot)
            {
                return productTypeName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the operating system that the current process is
        /// executing on.
        /// </summary>
        /// <returns>
        /// The operating system, or null if it is not available.
        /// </returns>
        public static OperatingSystem GetOperatingSystem()
        {
            lock (syncRoot)
            {
                return operatingSystem;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the patch level (i.e. version or release) of the
        /// operating system that the current process is executing on.
        /// </summary>
        /// <returns>
        /// The operating system patch level, or null if it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method gets the major and minor version components of the
        /// operating system that the current process is executing on.
        /// </summary>
        /// <returns>
        /// The major and minor version components, or null if they cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method gets the service pack, or other extra version,
        /// information for the operating system that the current process is
        /// executing on.
        /// </summary>
        /// <returns>
        /// The service pack information, or null if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method gets the release identifier of the operating system,
        /// as recorded in the registry.
        /// </summary>
        /// <returns>
        /// The operating system release identifier, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetOperatingSystemReleaseId()
        {
#if !NET_STANDARD_20
            string releaseId = null;

            try
            {
                RegistryKey rootKey = Registry.LocalMachine;

                if (rootKey == null)
                    return null;

                using (RegistryKey key = rootKey.OpenSubKey(
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
        /// <summary>
        /// This method gets the extended operating system information,
        /// including product details and the list of installed updates.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use; this parameter may be null.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to allow expensive, time-consuming queries to be
        /// performed.
        /// </param>
        /// <returns>
        /// The extended operating system information, formatted as a string,
        /// or null if it cannot be determined.
        /// </returns>
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
                RegistryKey rootKey = Registry.LocalMachine;

                if (rootKey == null)
                    return null;

                using (RegistryKey key = rootKey.OpenSubKey(
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

        /// <summary>
        /// This method determines whether the extended operating system
        /// information should be populated for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check; this parameter may be null.
        /// </param>
        /// <param name="ignoreAppDomain">
        /// Non-zero to skip the check for the default application domain.
        /// </param>
        /// <param name="ignoreFlags">
        /// Non-zero to skip the check of the interpreter creation flags.
        /// </param>
        /// <param name="ignoreSafe">
        /// Non-zero to skip the check for a safe interpreter.
        /// </param>
        /// <param name="ignoreSdk">
        /// Non-zero to skip the check for an SDK interpreter.
        /// </param>
        /// <param name="ignoreInteractive">
        /// Non-zero to skip the check for an interactive interpreter.
        /// </param>
        /// <returns>
        /// True if the extended operating system information should be
        /// populated; otherwise, false.
        /// </returns>
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
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
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

        /// <summary>
        /// This method asynchronously populates the extended operating system
        /// information for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to populate; this parameter may be null.
        /// </param>
        /// <param name="errorOnDisposed">
        /// Non-zero to treat a deleted or disposed interpreter as an error.
        /// </param>
        /// <param name="unsignalOnStart">
        /// Non-zero to reset the setup event when the work begins.
        /// </param>
        /// <param name="signalWhenDone">
        /// Non-zero to signal the setup event when the work completes.
        /// </param>
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
                            interpreter, true); /* WARNING: ~60 secs... */

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

        /// <summary>
        /// This method gets the detected operating system identifier for the
        /// current process.
        /// </summary>
        /// <returns>
        /// The detected operating system identifier.
        /// </returns>
        public static OperatingSystemId GetOperatingSystemId()
        {
            lock (syncRoot)
            {
                return operatingSystemId;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the operating system that the current
        /// process is executing on.
        /// </summary>
        /// <returns>
        /// The operating system name.
        /// </returns>
        public static string GetOperatingSystemName()
        {
            lock (syncRoot)
            {
                return operatingSystemName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a value indicating whether the current process is
        /// a 32-bit process running on a 64-bit version of Windows (WoW64).
        /// </summary>
        /// <returns>
        /// True if the current process is running as WoW64; otherwise, false.
        /// </returns>
        public static bool GetWin32onWin64()
        {
            lock (syncRoot)
            {
                return isWin32onWin64;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the current user, optionally
        /// qualified with the domain name.
        /// </summary>
        /// <param name="domain">
        /// Non-zero to include the domain name in the returned user name.
        /// </param>
        /// <returns>
        /// The current user name, optionally qualified with the domain name.
        /// </returns>
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

        /// <summary>
        /// This method gets a human-readable string describing the operating
        /// system name, version, and related platform information for the
        /// current process.
        /// </summary>
        /// <returns>
        /// A string describing the operating system and platform.
        /// </returns>
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
        /// <summary>
        /// This method queries the processor architecture from the
        /// environment, optionally falling back to a default value.
        /// </summary>
        /// <param name="force">
        /// Non-zero to query the environment even when not running on a
        /// Windows operating system.
        /// </param>
        /// <param name="default">
        /// The default value to return when the environment is not queried;
        /// this parameter is optional.
        /// </param>
        /// <returns>
        /// The processor architecture name, or the default value.
        /// </returns>
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

        /// <summary>
        /// This method corrects the processor architecture when an impossible
        /// combination of pointer size and architecture is detected.
        /// </summary>
        /// <param name="processorArchitecture">
        /// The processor architecture to check and, if necessary, correct;
        /// this parameter is optional.
        /// </param>
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

        /// <summary>
        /// This method initializes the cached processor and machine names
        /// based on the specified processor architecture.
        /// </summary>
        /// <param name="processorArchitecture">
        /// The processor architecture to use when initializing the names.
        /// </param>
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

        /// <summary>
        /// This method initializes the cached processor and machine names
        /// based on the specified processor name.
        /// </summary>
        /// <param name="processorName">
        /// The processor name to use when initializing the names.
        /// </param>
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

        /// <summary>
        /// This method attempts to parse the specified name into a processor
        /// architecture.
        /// </summary>
        /// <param name="name">
        /// The name to parse.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the parsed processor architecture;
        /// otherwise, set to null.
        /// </param>
        /// <returns>
        /// True if the name was parsed successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method parses the specified name into a processor
        /// architecture.
        /// </summary>
        /// <param name="name">
        /// The name to parse.
        /// </param>
        /// <returns>
        /// The parsed processor architecture, or <see
        /// cref="ProcessorArchitecture.Unknown" /> if the name cannot be
        /// parsed.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified processor
        /// architecture is known.
        /// </summary>
        /// <param name="processorArchitecture">
        /// The processor architecture to check.
        /// </param>
        /// <returns>
        /// True if the processor architecture is known; otherwise, false.
        /// </returns>
        private static bool IsKnownProcessorArchitecture(
            ProcessorArchitecture processorArchitecture /* in */
            )
        {
            return processorArchitecture != ProcessorArchitecture.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method guesses the processor architecture based on the
        /// pointer size of the current process.
        /// </summary>
        /// <returns>
        /// The guessed processor architecture.
        /// </returns>
        private static ProcessorArchitecture GuessProcessorArchitecture()
        {
            return Is64BitProcess() ?
                ProcessorArchitecture.AMD64 : ProcessorArchitecture.Intel;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method guesses the processor architecture based on the
        /// specified operating system and/or platform or processor name.
        /// </summary>
        /// <param name="operatingSystem">
        /// The operating system to use; this parameter may be null.
        /// </param>
        /// <param name="platformOrProcessorName">
        /// The platform or processor name to use; this parameter may be null.
        /// </param>
        /// <returns>
        /// The guessed processor architecture, or <see
        /// cref="ProcessorArchitecture.Unknown" /> if it cannot be guessed.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified operating system
        /// should be treated as Windows.
        /// </summary>
        /// <param name="operatingSystem">
        /// The operating system to check.
        /// </param>
        /// <param name="default">
        /// The default value to return when the operating system is null.
        /// </param>
        /// <returns>
        /// True if the operating system should be treated as Windows;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified operating system
        /// identifier should be treated as Windows.
        /// </summary>
        /// <param name="platformId">
        /// The operating system identifier to check.
        /// </param>
        /// <param name="default">
        /// The default value to return when the identifier is not recognized.
        /// </param>
        /// <returns>
        /// True if the operating system identifier should be treated as
        /// Windows; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the platform identifier of the operating system
        /// that the current process is executing on.
        /// </summary>
        /// <returns>
        /// The operating system platform identifier.
        /// </returns>
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

        /// <summary>
        /// This method gets the version of the operating system that the
        /// current process is executing on.
        /// </summary>
        /// <returns>
        /// The operating system version, or null if it is not available.
        /// </returns>
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

        /// <summary>
        /// This method gets the product type of the operating system using
        /// only managed APIs.
        /// </summary>
        /// <returns>
        /// The operating system product type.
        /// </returns>
        private static VER_PRODUCT_TYPE GetOperatingSystemProductType()
        {
            //
            // TODO: No pure managed way to obtain this information?
            //
            return VER_PRODUCT_TYPE.VER_NT_UNKNOWN;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the setup event for the specified interpreter,
        /// complaining if the operation fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setup event should be reset.
        /// </param>
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

        /// <summary>
        /// This method signals the setup event for the specified interpreter,
        /// complaining if the operation fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose setup event should be signaled.
        /// </param>
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
        /// <summary>
        /// This method determines whether the current operating system is a
        /// Unix operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is a Unix operating system;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// macOS (Darwin) operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the macOS (Darwin)
        /// operating system; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// Linux operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the Linux operating
        /// system; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is a
        /// Windows operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is a Windows operating
        /// system; otherwise, false.
        /// </returns>
        public static bool IsWindowsOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                return ((operatingSystemId == OperatingSystemId.Windows9x) ||
                    (operatingSystemId == OperatingSystemId.WindowsNT));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method guesses the operating system identifier based on the
        /// detected operating system family.
        /// </summary>
        /// <returns>
        /// The guessed operating system identifier.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Vista or higher.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Vista or higher;
        /// otherwise, false.
        /// </returns>
        public static bool IsWindowsVistaOrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindowsVistaOrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 8.1.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows 8.1; otherwise,
        /// false.
        /// </returns>
        public static bool IsWindows81()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows81(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows 10; otherwise,
        /// false.
        /// </returns>
        public static bool IsWindows10()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows10(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 10 or higher.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows 10 or higher;
        /// otherwise, false.
        /// </returns>
        public static bool IsWindows10OrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows10OrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is the
        /// November Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the November Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10NovemberUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// Anniversary Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the Anniversary Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10AnniversaryUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// Creators Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the Creators Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10CreatorsUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// Fall Creators Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the Fall Creators Update
        /// of Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10FallCreatorsUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// April 2018 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the April 2018 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10April2018Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// October 2018 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the October 2018 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10October2018Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// May 2019 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the May 2019 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10May2019Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// November 2019 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the November 2019 Update
        /// of Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10November2019Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// May 2020 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the May 2020 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10May2020Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// October 2020 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the October 2020 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10October2020Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// May 2021 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the May 2021 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10May2021Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// November 2021 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the November 2021 Update
        /// of Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10November2021Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// October 2022 Update of Windows 10.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the October 2022 Update of
        /// Windows 10; otherwise, false.
        /// </returns>
        public static bool IsWindows10October2022Update()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 11.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows 11; otherwise,
        /// false.
        /// </returns>
        public static bool IsWindows11()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows11(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 11 or higher.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows 11 or higher;
        /// otherwise, false.
        /// </returns>
        public static bool IsWindows11OrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows11OrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is the
        /// September 2022 Update of Windows 11.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the September 2022 Update
        /// of Windows 11; otherwise, false.
        /// </returns>
        public static bool IsWindows11September2022Update()
        {
            Version osVersion = null;

            if (!IsWindows11OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
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

        /// <summary>
        /// This method determines whether the current operating system is the
        /// October 2023 Update of Windows 11.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the October 2023 Update of
        /// Windows 11; otherwise, false.
        /// </returns>
        public static bool IsWindows11October2023Update()
        {
            Version osVersion = null;

            if (!IsWindows11OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows11October2023UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is the
        /// October 2024 Update of Windows 11.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the October 2024 Update of
        /// Windows 11; otherwise, false.
        /// </returns>
        public static bool IsWindows11October2024Update()
        {
            Version osVersion = null;

            if (!IsWindows11OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows11October2024UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is the
        /// September 2025 Update of Windows 11.
        /// </summary>
        /// <returns>
        /// True if the current operating system is the September 2025 Update
        /// of Windows 11; otherwise, false.
        /// </returns>
        public static bool IsWindows11September2025Update()
        {
            Version osVersion = null;

            if (!IsWindows11OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be included
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows11September2025UpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server 2012 R2.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server 2012 R2;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server 2016.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server 2016;
        /// otherwise, false.
        /// </returns>
        public static bool IsWindowsServer2016() /* IsWindowsServerVersion1607() */
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1607");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server, version 1709.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server, version
        /// 1709; otherwise, false.
        /// </returns>
        public static bool IsWindowsServerVersion1709()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1709");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server, version 1803.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server, version
        /// 1803; otherwise, false.
        /// </returns>
        public static bool IsWindowsServerVersion1803()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1803");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server, version 1809.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server, version
        /// 1809; otherwise, false.
        /// </returns>
        public static bool IsWindowsServerVersion1809()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1809");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server, version 1903.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server, version
        /// 1903; otherwise, false.
        /// </returns>
        public static bool IsWindowsServerVersion1903()
        {
            if (!IsWindowsServerOperatingSystem())
                return false;

            if (!IsWindows10())
                return false;

            return IsWindowsReleaseId("1903");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Server 2022.
        /// </summary>
        /// <returns>
        /// True if the current operating system is Windows Server 2022;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the current operating system is at,
        /// or above, the specified platform, version, and service pack.
        /// </summary>
        /// <param name="platformId">
        /// The platform identifier that must match the current operating
        /// system.
        /// </param>
        /// <param name="major">
        /// The minimum required major version number.
        /// </param>
        /// <param name="minor">
        /// The minimum required minor version number.
        /// </param>
        /// <param name="servicePackMajor">
        /// The minimum required major service pack version number.
        /// </param>
        /// <param name="servicePackMinor">
        /// The minimum required minor service pack version number.
        /// </param>
        /// <returns>
        /// True if the current operating system is at, or above, the
        /// specified version; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method gets the number of bits (e.g. 32 or 64) for the
        /// pointer size of the current process.
        /// </summary>
        /// <returns>
        /// The number of bits for the pointer size of the current process.
        /// </returns>
        public static int GetProcessBits() // (e.g. 32, 64, etc)
        {
            return (IntPtr.Size * ConversionOps.ByteBits);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current process is a 32-bit
        /// process.
        /// </summary>
        /// <returns>
        /// True if the current process is a 32-bit process; otherwise, false.
        /// </returns>
        public static bool Is32BitProcess()
        {
            return (IntPtr.Size == sizeof(uint));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current process is a 64-bit
        /// process.
        /// </summary>
        /// <returns>
        /// True if the current process is a 64-bit process; otherwise, false.
        /// </returns>
        public static bool Is64BitProcess()
        {
            return (IntPtr.Size == sizeof(ulong));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Name Lookup Methods
#if NATIVE
        /// <summary>
        /// This method looks up the alternate processor name for the
        /// specified platform or processor name.
        /// </summary>
        /// <param name="platformOrProcessorName">
        /// The platform or processor name to look up.
        /// </param>
        /// <param name="notFoundType">
        /// The action to take when the name cannot be found.
        /// </param>
        /// <returns>
        /// The alternate processor name, or a value determined by <paramref
        /// name="notFoundType" /> when it cannot be found.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the current operating system is a
        /// Windows Server operating system.
        /// </summary>
        /// <returns>
        /// True if the current operating system is a Windows Server operating
        /// system; otherwise, false.
        /// </returns>
        private static bool IsWindowsServerOperatingSystem()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return ((productType == VER_PRODUCT_TYPE.VER_NT_DOMAIN_CONTROLLER) ||
                    (productType == VER_PRODUCT_TYPE.VER_NT_SERVER));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the release identifier of the
        /// current operating system matches the specified value.
        /// </summary>
        /// <param name="releaseId">
        /// The release identifier to compare against.
        /// </param>
        /// <returns>
        /// True if the release identifier matches; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows Vista or higher, also returning the operating system
        /// version.
        /// </summary>
        /// <param name="osVersion">
        /// Upon success, receives the operating system version.
        /// </param>
        /// <returns>
        /// True if the current operating system is Windows Vista or higher;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 8.1, also returning the operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// Upon success, receives the operating system version.
        /// </param>
        /// <returns>
        /// True if the current operating system is Windows 8.1; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 10, also returning the operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// Upon success, receives the operating system version.
        /// </param>
        /// <returns>
        /// True if the current operating system is Windows 10; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 10 or higher, also returning the operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// Upon success, receives the operating system version.
        /// </param>
        /// <returns>
        /// True if the current operating system is Windows 10 or higher;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 11, also returning the operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// Upon success, receives the operating system version.
        /// </param>
        /// <returns>
        /// True if the current operating system is Windows 11; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current operating system is
        /// Windows 11 or higher, also returning the operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// Upon success, receives the operating system version.
        /// </param>
        /// <returns>
        /// True if the current operating system is Windows 11 or higher;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the list of installed operating system updates.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use; this parameter may be null.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to allow expensive, time-consuming queries to be
        /// performed.
        /// </param>
        /// <returns>
        /// The list of installed operating system updates, or null if it
        /// cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the list of installed Windows
        /// updates can be obtained, selecting the command to be used.
        /// </summary>
        /// <param name="type">
        /// Upon success, receives the type of command that will be used.
        /// </param>
        /// <param name="fileName">
        /// Upon success, receives the file name of the command that will be
        /// used.
        /// </param>
        /// <param name="arguments">
        /// Upon success, receives the command line arguments that will be
        /// used.
        /// </param>
        /// <returns>
        /// True if the list of installed updates can be obtained; otherwise,
        /// false.
        /// </returns>
        private static bool CanGetWindowsInstalledUpdates(
            ref GetInstalledUpdatesType? type, /* out */
            ref string fileName,               /* out */
            ref string arguments               /* out */
            )
        {
            string localFileName; /* REUSED */

            //
            // HACK: The "wmic.exe" executable is not available on
            //       Windows 2000 or Windows XP Home Edition; so,
            //       check if it exists prior to attempting to use
            //       it.  Also, it has been removed from the latest
            //       versions of Windows 11.
            //
            localFileName = CommonOps.Environment.ExpandVariables(
                WmiQfeGetUpdatesCommandFileName);

            if (File.Exists(localFileName))
            {
                type = GetInstalledUpdatesType.WmiCommand;
                fileName = localFileName;
                arguments = WmiQfeGetUpdatesCommandArguments;

                return true;
            }

            localFileName = CommonOps.Environment.ExpandVariables(
                PowerShellQfeGetUpdatesCommandFileName);

            if (File.Exists(localFileName))
            {
                type = GetInstalledUpdatesType.PowerShell;
                fileName = localFileName;
                arguments = PowerShellQfeGetUpdatesCommandArguments;

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes header and separator elements from the raw
        /// list of installed Windows updates.
        /// </summary>
        /// <param name="type">
        /// The type of command that produced the list.
        /// </param>
        /// <param name="list">
        /// The list of installed updates to fix up.
        /// </param>
        /// <returns>
        /// True if the list was modified; otherwise, false.
        /// </returns>
        private static bool FixupWindowsInstalledUpdates(
            GetInstalledUpdatesType? type, /* in */
            ref StringList list            /* in, out */
            )
        {
            if ((type == null) || (list == null))
                return false;

            switch ((GetInstalledUpdatesType)type)
            {
                case GetInstalledUpdatesType.WmiCommand:
                    {
                        if ((list.Count > 0) &&
                            SharedStringOps.SystemNoCaseEquals(
                                list[0], WmiQfePropertyName))
                        {
                            list.RemoveAt(0);
                            return true;
                        }

                        break;
                    }
                case GetInstalledUpdatesType.PowerShell:
                    {
                        StringList localList = new StringList();
                        int count = 0;

                        foreach (string element in list)
                        {
                            if (String.IsNullOrEmpty(element) ||
                                SharedStringOps.SystemNoCaseEquals(
                                    element, PowerShellQfePropertyName) ||
                                SharedStringOps.SystemNoCaseEquals(
                                    element, PowerShellQfeValueSeparator))
                            {
                                count++;
                                continue;
                            }

                            localList.Add(element);
                        }

                        if (count > 0)
                        {
                            list = localList;
                            return true;
                        }

                        break;
                    }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of installed Windows updates, optionally
        /// using a cached result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use; this parameter may be null.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the expensive query; otherwise, only a cached
        /// result is returned.
        /// </param>
        /// <returns>
        /// The list of installed Windows updates, or null if it cannot be
        /// determined.
        /// </returns>
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
                GetInstalledUpdatesType? type = null;
                string fileName = null;
                string arguments = null;

                if (!CanGetWindowsInstalledUpdates(
                        ref type, ref fileName, ref arguments))
                {
                    TraceOps.DebugTrace(String.Format(
                        "WindowsGetInstalledUpdates: " +
                        "unavailable, interpreter = {0}",
                        FormatOps.InterpreterNoThrow(
                        interpreter)), typeof(PlatformOps).Name,
                        TracePriority.PlatformDebug);

                    return null;
                }

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
                            /* IGNORED */
                            FixupWindowsInstalledUpdates(
                                type, ref installedUpdates);

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
        /// <summary>
        /// This method gets the display name of the named Windows 10 update
        /// that corresponds to the specified operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// The operating system version to look up.
        /// </param>
        /// <returns>
        /// The display name of the named Windows 10 update, or null if there
        /// is none.
        /// </returns>
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
        /// <summary>
        /// This method gets the display name of the named Windows 11 update
        /// that corresponds to the specified operating system version.
        /// </summary>
        /// <param name="osVersion">
        /// The operating system version to look up.
        /// </param>
        /// <returns>
        /// The display name of the named Windows 11 update, or null if there
        /// is none.
        /// </returns>
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
                    case Windows11October2023UpdateBuildNumber:
                        return Windows11October2023UpdateName;
                    case Windows11October2024UpdateBuildNumber:
                        return Windows11October2024UpdateName;
                    case Windows11September2025UpdateBuildNumber:
                        return Windows11September2025UpdateName;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Name Lookup Methods
        /// <summary>
        /// This method looks up the machine name for the specified platform
        /// or processor name.
        /// </summary>
        /// <param name="platformOrProcessorName">
        /// The platform or processor name to look up.
        /// </param>
        /// <param name="notFoundType">
        /// The action to take when the name cannot be found.
        /// </param>
        /// <returns>
        /// The machine name, or a value determined by <paramref
        /// name="notFoundType" /> when it cannot be found.
        /// </returns>
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

        /// <summary>
        /// This method looks up the processor name for the specified
        /// processor architecture.
        /// </summary>
        /// <param name="processorArchitecture">
        /// The processor architecture to look up.
        /// </param>
        /// <param name="notFoundType">
        /// The action to take when the name cannot be found.
        /// </param>
        /// <returns>
        /// The processor name, or a value determined by <paramref
        /// name="notFoundType" /> when it cannot be found.
        /// </returns>
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

        /// <summary>
        /// This method looks up the operating system name for the specified
        /// operating system identifier.
        /// </summary>
        /// <param name="platformId">
        /// The operating system identifier to look up.
        /// </param>
        /// <param name="notFoundType">
        /// The action to take when the name cannot be found.
        /// </param>
        /// <returns>
        /// The operating system name, or a value determined by <paramref
        /// name="notFoundType" /> when it cannot be found.
        /// </returns>
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

        /// <summary>
        /// This method looks up the platform name for the specified operating
        /// system identifier.
        /// </summary>
        /// <param name="platformId">
        /// The operating system identifier to look up.
        /// </param>
        /// <param name="notFoundType">
        /// The action to take when the name cannot be found.
        /// </param>
        /// <returns>
        /// The platform name, or a value determined by <paramref
        /// name="notFoundType" /> when it cannot be found.
        /// </returns>
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

        /// <summary>
        /// This method looks up the product type name for the specified
        /// product type.
        /// </summary>
        /// <param name="productType">
        /// The product type to look up.
        /// </param>
        /// <param name="notFoundType">
        /// The action to take when the name cannot be found.
        /// </param>
        /// <returns>
        /// The product type name, or a value determined by <paramref
        /// name="notFoundType" /> when it cannot be found.
        /// </returns>
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
        /// <summary>
        /// This method retrieves the native system information for the
        /// current computer.
        /// </summary>
        /// <param name="systemInfo">
        /// Upon success, receives the system information.
        /// </param>
        /// <returns>
        /// True if the system information was retrieved successfully;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the native operating system version
        /// information.
        /// </summary>
        /// <param name="versionInfo">
        /// Upon success, receives the operating system version information.
        /// </param>
        /// <returns>
        /// True if the version information was retrieved successfully;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current process is a 32-bit
        /// process running on a 64-bit version of Windows (WoW64).
        /// </summary>
        /// <returns>
        /// True if the current process is running as WoW64; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method retrieves the native uname information for the current
        /// system.
        /// </summary>
        /// <param name="utsName">
        /// Upon success, receives the uname information.
        /// </param>
        /// <returns>
        /// True if the uname information was retrieved successfully;
        /// otherwise, false.
        /// </returns>
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
