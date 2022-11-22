/*
 * PathOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;

#if WEB
using System.Collections.Specialized;
#endif

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

#if NATIVE
using System.Security;
#endif

#if !NET_STANDARD_20 && !MONO
using System.Security.AccessControl;
using System.Security.Principal;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#if WEB
using System.Web;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using StringDictionary = Eagle._Containers.Public.StringDictionary;

using UnderAnyPair = Eagle._Components.Public.AnyPair<
    Eagle._Components.Public.PathType, string>;

using UnderPair = System.Collections.Generic.KeyValuePair<string,
    System.Collections.Generic.List<Eagle._Components.Public.AnyPair<
        Eagle._Components.Public.PathType, string>>>;

using UnderDictionary = Eagle._Containers.Public.PathDictionary<
    System.Collections.Generic.List<Eagle._Components.Public.AnyPair<
        Eagle._Components.Public.PathType, string>>>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("1e358ef6-1b8f-49ac-a152-0ffece56f5af")]
    internal static class PathOps
    {
#if NATIVE
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("73db358b-e0ef-42b5-9de5-46362ad86e91")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("41367b38-86f2-41c3-b7ee-4b3374372039")]
            internal struct FILETIME
            {
                public uint dwLowDateTime;
                public uint dwHighDateTime;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("bb894e4a-17d1-4f0e-aae1-6f878ad05f2c")]
            internal struct BY_HANDLE_FILE_INFORMATION
            {
                public FileFlagsAndAttributes dwFileAttributes;
                public FILETIME ftCreationTime;
                public FILETIME ftLastAccessTime;
                public FILETIME ftLastWriteTime;
                public uint dwVolumeSerialNumber;
                public uint nFileSizeHigh;
                public uint nFileSizeLow;
                public uint nNumberOfLinks;
                public uint nFileIndexHigh;
                public uint nFileIndexLow;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            internal const uint FSCTL_GET_OBJECT_ID = 0x9009c;
            internal const uint FSCTL_CREATE_OR_GET_OBJECT_ID = 0x900c0;

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("71d21cdf-5626-4197-9c5c-428e4717dc80")]
            internal struct FILE_OBJECTID_BUFFER
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] ObjectId;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] BirthVolumeId;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] BirthObjectId;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] DomainId;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr CreateFile(
                string fileName,
                FileAccessMask desiredAccess,
                FileShareMode shareMode,
                IntPtr securityAttributes,
                FileCreationDisposition creationDisposition,
                FileFlagsAndAttributes flagsAndAttributes,
                IntPtr templateFile
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeviceIoControl(
                IntPtr device, uint ioControlCode, IntPtr inBuffer,
                uint inBufferSize, IntPtr outBuffer, uint outBufferSize,
                ref uint bytesReturned, IntPtr overlapped
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetFileInformationByHandle(
                IntPtr file,
                ref BY_HANDLE_FILE_INFORMATION fileInformation
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Shell32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Unicode, BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PathIsExe(string path);
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("ace5d181-10ec-4bdf-ab7a-72c9e2d698f0")]
            internal struct timespec
            {
                public long /* time_t */ tv_sec; // wrong?
                public long tv_nsec;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* WARNING: Non-portable, select versions of Linux only? */
            [StructLayout(LayoutKind.Explicit)]
            [ObjectId("3f01d0eb-ba6b-4b5a-b15b-68488418a11b")]
            internal struct stat /* monophile: Ubuntu 16.04.7 LTS */
            {
                [FieldOffset(0)]
                public ulong /* dev_t */ st_dev; /* 0 */
                [FieldOffset(8)]
                public ulong /* ino_t */ st_ino; /* 8 */
                [FieldOffset(16)]
                public ulong /* nlink_t */ st_nlink; /* 16 */
                [FieldOffset(24)]
                public uint /* mode_t */ st_mode; /* 24 */
                [FieldOffset(28)]
                public uint /* uid_t */ st_uid; /* 28 */
                [FieldOffset(32)]
                public uint /* gid_t */ st_gid; /* 32 */
                [FieldOffset(40)]
                public ulong /* dev_t */ st_rdev; /* 40 */
                [FieldOffset(48)]
                public ulong /* off_t */ st_size; /* 48 */
                [FieldOffset(56)]
                public ulong /* blksize_t */ st_blksize; /* 56 */
                [FieldOffset(64)]
                public ulong /* blkcnt_t */ st_blocks; /* 64 */
                [FieldOffset(72)]
                public /* struct */ timespec st_atim; /* 72 */
                [FieldOffset(88)]
                public /* struct */ timespec st_mtim; /* 88 */
                [FieldOffset(104)]
                public /* struct */ timespec st_ctim; /* 104 */
                [FieldOffset(120)]
                public ulong padding1; /* 120 */
                [FieldOffset(128)]
                public ulong padding2; /* 128 */
                [FieldOffset(136)]
                public ulong padding3; /* 136 */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* WARNING: Non-portable, select versions of Linux only? */
            [DllImport(DllName.LibC, EntryPoint = "__xstat",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int libc_xstat(int ver, string path, out stat buf);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        private const int DrivePrefixLength = 2;
        private const int ExtendedPrefixLength = 4;
        private const int UncPrefixLength = 2;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: The maximum length for a module file name.
        //
        private static readonly uint UNICODE_STRING_MAX_CHARS = 32767;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly bool NoCase =
            PlatformOps.IsWindowsOperatingSystem() ?
                true : PlatformOps.IsUnixOperatingSystem() ? false : true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly StringComparison ComparisonType =
            GetComparisonType();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char[] PathWildcardChars = {
            Characters.Asterisk,
            Characters.QuestionMark
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string[] BuildConfigurations = {
            BuildConfiguration.Debug, BuildConfiguration.Release
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly ConfigurationFlags ScalarConfigurationFlags =
            ConfigurationFlags.PathOps | ConfigurationFlags.NativePathValue;

        private static readonly ConfigurationFlags ListConfigurationFlags =
            ConfigurationFlags.PathOps | ConfigurationFlags.NativePathListValue;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: When NOT running on Windows, it is possible that neither
        //       of the directory separator character values will be the
        //       backslash character.  Therefore, use our fixed character
        //       values instead, because various methods in this library
        //       depend on these two character values being different.
        //
        public static readonly char DirectorySeparatorChar = Characters.DirectorySeparator;
        public static readonly char AltDirectorySeparatorChar = Characters.AltDirectorySeparator;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly string CurrentDirectory = _Path.Current;
        internal static readonly string ParentDirectory = _Path.Parent;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char NonNativeDirectorySeparatorChar =
            PlatformOps.IsWindowsOperatingSystem() ?
                AltDirectorySeparatorChar :
                DirectorySeparatorChar;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly char NativeDirectorySeparatorChar =
            PlatformOps.IsWindowsOperatingSystem() ?
                DirectorySeparatorChar :
                AltDirectorySeparatorChar;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static char[] DirectoryChars = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /*
         * The number of 100-ns intervals between the Windows system epoch
         * (1601-01-01 on the proleptic Gregorian calendar) and the Posix
         * epoch (1970-01-01).
         *
         * This value was stolen directly from the Tcl 8.6 source code.
         */

        private const ulong POSIX_EPOCH_AS_FILETIME = (ulong)116444736 * (ulong)1000000000;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This URI is only used to temporarily build an absolute
        //       URI from a relative one so the GetComponents method may
        //       be used to grab portions of the relative URI.
        //
        private static readonly Uri DefaultBaseUri = new Uri("https://www.example.com/");

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Regex identifierRegEx = RegExOps.Create(
            "^[0-9A-Z_]+$", RegexOptions.IgnoreCase);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unique Path Constants
        private static readonly string DefaultWindowsUniquePrefix = "eiq";
        private static readonly string DefaultUnixUniquePrefix = "eagle-unique-path-";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly int DefaultUniqueMaximumRetries = 10000;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly int DefaultWindowsUniqueByteCount = sizeof(ushort);
        private static readonly int DefaultUnixUniqueByteCount = sizeof(ulong);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the other static data
        //       members in this class.
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is greater than zero, trace calls into the very
        //       popular method NormalizePath.  This is handled specially due
        //       to it being in the hot-path for basically everything.
        //
        private static int traceForNormalize = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this is not null, it will be used as the return value from
        //       the (extremely important) GetBinaryPath method.
        //
        private static string binaryPath = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the list of candidate path components used when
        //       attempting to locate the cloud drive directory for a user,
        //       in priority order (i.e. the search will stop at the first
        //       cloud drive directory found).  These path components will
        //       be appended to the home directory for a given user prior
        //       to being checked for validity.
        //
        // WARNING: These values are probably not correct for non-Windows
        //          platforms.
        //
        // HACK: This is purposely not read-only.
        //
        private static string[] defaultCloudPaths = {
            null,           // Override #1
            null,           // Override #2
            null,           // Override #3
            null,           // Override #4
            "iCloudDrive",  // Apple iCloud (https://www.icloud.com/)
            "OneDrive",     // Microsoft OneDrive (https://onedrive.live.com/)
            "Box",          // Box (https://www.box.com/)
            "Dropbox",      // Dropbox (https://www.dropbox.com/)
            "Google Drive", // Google (https://www.google.com/drive/)
            null,           // Fallback #1
            null,           // Fallback #2
            null,           // Fallback #3
            null            // Fallback #4
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static GetStringValueCallback getTempFileNameCallback = null;
        private static GetStringValueCallback getTempPathCallback = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        //
        // HACK: This is purposely not read-only.
        //
        private static bool NoNativeIsSameFile = false;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the URI components to be used from the baseUri in
        //       the TryCombineUris method.
        //
        // HACK: These are purposely not read-only.
        //
        private static UriComponents BaseUriComponents = UriComponents.Scheme |
            UriComponents.UserInfo | UriComponents.Host | UriComponents.Port;

        private static UriFormat DefaultUriFormat = UriFormat.SafeUnescaped;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unique Path Data
        //
        // NOTE: The unique prefix and suffix are used when attempting to form
        //       a unique path for use by external callers (e.g. temporary log
        //       files, etc).
        //
        // HACK: These are purposely not read-only.
        //
        private static string UniquePrefix;
        private static string UniqueSuffix;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the maximum number of times to retry before giving upon
        //       on being able to create a unique path.
        //
        // HACK: This is purposely not read-only.
        //
        private static int UniqueMaximumRetries;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of random bytes to use within the path when
        //       attempting to create a unique path.
        //
        // HACK: This is purposely not read-only.
        //
        private static int UniqueByteCount;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (binaryPath != null))
                {
                    localList.Add("BinaryPath",
                        FormatOps.DisplayString(binaryPath));
                }

                if (empty || (defaultCloudPaths != null))
                {
                    localList.Add("DefaultCloudPaths",
                        (defaultCloudPaths != null) ?
                            defaultCloudPaths.Length.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || (getTempFileNameCallback != null))
                {
                    localList.Add("GetTempFileNameCallback",
                        FormatOps.DelegateMethodName(
                            getTempFileNameCallback, true, true));
                }

                if (empty || (getTempPathCallback != null))
                {
                    localList.Add("GetTempPathCallback",
                        FormatOps.DelegateMethodName(
                            getTempPathCallback, true, true));
                }

#if NATIVE && (WINDOWS || UNIX)
                if (empty || NoNativeIsSameFile)
                {
                    localList.Add("NoNativeIsSameFile",
                        NoNativeIsSameFile.ToString());
                }
#endif

                if (empty || (BaseUriComponents != (UriComponents)0))
                {
                    localList.Add("BaseUriComponents",
                        BaseUriComponents.ToString());
                }

                if (empty || (DefaultUriFormat != (UriFormat)0))
                {
                    localList.Add("DefaultUriFormat",
                        DefaultUriFormat.ToString());
                }

                if (empty || (UniquePrefix != null))
                {
                    localList.Add("UniquePrefix",
                        FormatOps.DisplayString(UniquePrefix));
                }

                if (empty || (UniqueSuffix != null))
                {
                    localList.Add("UniqueSuffix",
                        FormatOps.DisplayString(UniqueSuffix));
                }

                if (empty || (UniqueMaximumRetries != 0))
                {
                    localList.Add("UniqueMaximumRetries",
                        UniqueMaximumRetries.ToString());
                }

                if (empty || (UniqueByteCount != 0))
                {
                    localList.Add("UniqueByteCount",
                        UniqueByteCount.ToString());
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Path Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeUniquePathData()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool isWindows = PlatformOps.IsWindowsOperatingSystem();

                if (UniquePrefix == null)
                {
                    UniquePrefix = isWindows ?
                        DefaultWindowsUniquePrefix :
                        DefaultUnixUniquePrefix;
                }

                if (UniqueSuffix == null)
                    UniqueSuffix = FileExtension.Temporary;

                if (UniqueByteCount == 0)
                {
                    UniqueByteCount = isWindows ?
                        DefaultWindowsUniqueByteCount :
                        DefaultUnixUniqueByteCount;
                }

                if (UniqueMaximumRetries == 0)
                    UniqueMaximumRetries = DefaultUniqueMaximumRetries;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeGetUniqueProperties(
            ref string prefix,     /* in, out */
            ref string suffix,     /* in, out */
            ref int byteCount,     /* in, out */
            ref int maximumRetries /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (prefix == null)
                    prefix = UniquePrefix;

                if (suffix == null)
                    suffix = UniqueSuffix;

                if (byteCount == 0)
                    byteCount = UniqueByteCount;

                if (maximumRetries == 0)
                    maximumRetries = UniqueMaximumRetries;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SetBinaryPath(
            string path /* in */
            )
        {
            lock (syncRoot)
            {
                binaryPath = path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SetBaseUriComponents(
            UriComponents uriComponents /* in */
            )
        {
            lock (syncRoot)
            {
                BaseUriComponents = uriComponents;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SetBaseUriFormat(
            UriFormat uriFormat /* in */
            )
        {
            lock (syncRoot)
            {
                DefaultUriFormat = uriFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveDirectoryChars(
            char[] characters
            )
        {
            return (characters != null) && (characters.Length > 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static char[] NewDirectoryChars(
            bool both
            )
        {
            if (both)
            {
                return new char[] {
                    NativeDirectorySeparatorChar,
                    NonNativeDirectorySeparatorChar
                };
            }
            else
            {
                return new char[] {
                    NativeDirectorySeparatorChar
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static char[] GetOrNewDirectoryChars()
        {
            char[] characters = Interlocked.CompareExchange(
                ref DirectoryChars, null, null);

            if (characters == null)
            {
                characters = NewDirectoryChars(
                    PlatformOps.IsWindowsOperatingSystem());

                /* IGNORED */
                Interlocked.CompareExchange(
                    ref DirectoryChars, characters, null);
            }

            return characters;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryGetDirectoryChars(
            out char[] characters
            )
        {
            return TryGetDirectoryChars(null, out characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryGetDirectoryChars(
            bool? both,
            out char[] characters
            )
        {
            if (both != null)
            {
                characters = NewDirectoryChars((bool)both);
                return true;
            }

            characters = GetOrNewDirectoryChars();
            return HaveDirectoryChars(characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringComparison GetComparisonType()
        {
            //
            // WINDOWS: File names are not case-sensitive.
            //
            if (PlatformOps.IsWindowsOperatingSystem())
                return StringOps.GetUserComparisonType(true);

            //
            // UNIX: File names are case-sensitive.
            //
            if (PlatformOps.IsUnixOperatingSystem())
                return StringOps.GetUserComparisonType(false);

            //
            // UNKNOWN: Assume that file names are binary
            //          (and case-sensitive).
            //
            return SharedStringOps.GetSystemComparisonType(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetCallback(
            PathCallbackType callbackType, /* in */
            ref Delegate @delegate,        /* out */
            ref Result error               /* out */
            )
        {
            switch (callbackType)
            {
                case PathCallbackType.GetTempFileName:
                    {
                        lock (syncRoot)
                        {
                            @delegate = getTempFileNameCallback;
                        }

                        return ReturnCode.Ok;
                    }
                case PathCallbackType.GetTempPath:
                    {
                        lock (syncRoot)
                        {
                            @delegate = getTempPathCallback;
                        }

                        return ReturnCode.Ok;
                    }
                default:
                    {
                        error = String.Format(
                            "unsupported callback type \"{0}\"",
                            callbackType);

                        return ReturnCode.Error;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ChangeCallback(
            PathCallbackType callbackType, /* in */
            Delegate @delegate,            /* in */
            ref Result error               /* out */
            )
        {
            switch (callbackType)
            {
                case PathCallbackType.GetTempFileName:
                    {
                        if (@delegate == null)
                        {
                            lock (syncRoot)
                            {
                                getTempFileNameCallback = null;
                            }

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            GetStringValueCallback callback =
                                @delegate as GetStringValueCallback;

                            if (callback != null)
                            {
                                lock (syncRoot)
                                {
                                    getTempFileNameCallback = callback;
                                }

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = String.Format(
                                    "could not convert delegate to type {0}",
                                    typeof(GetStringValueCallback));

                                return ReturnCode.Error;
                            }
                        }
                    }
                case PathCallbackType.GetTempPath:
                    {
                        if (@delegate == null)
                        {
                            lock (syncRoot)
                            {
                                getTempPathCallback = null;
                            }

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            GetStringValueCallback callback =
                                @delegate as GetStringValueCallback;

                            if (callback != null)
                            {
                                lock (syncRoot)
                                {
                                    getTempPathCallback = callback;
                                }

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = String.Format(
                                    "could not convert delegate to type {0}",
                                    typeof(GetStringValueCallback));

                                return ReturnCode.Error;
                            }
                        }
                    }
                default:
                    {
                        error = String.Format(
                            "unsupported callback type \"{0}\"",
                            callbackType);

                        return ReturnCode.Error;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static void InitializeFileInformation(
            out UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation /* out */
            )
        {
            fileInformation.dwFileAttributes =
                FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

            fileInformation.ftCreationTime = new UnsafeNativeMethods.FILETIME();
            fileInformation.ftLastAccessTime = new UnsafeNativeMethods.FILETIME();
            fileInformation.ftLastWriteTime = new UnsafeNativeMethods.FILETIME();

            fileInformation.dwVolumeSerialNumber = 0;

            fileInformation.nFileSizeHigh = 0;
            fileInformation.nFileSizeLow = 0;

            fileInformation.nNumberOfLinks = 0;

            fileInformation.nFileIndexHigh = 0;
            fileInformation.nFileIndexLow = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetPathInformation(
            string fileName,                                                    /* in */
            bool directory,                                                     /* in */
            bool reparse,                                                       /* in */
            ref UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation, /* out */
            ref Result error                                                    /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                IntPtr handle = IntPtr.Zero;

                if (!String.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        FileFlagsAndAttributes fileFlagsAndAttributes =
                            FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

                        if (directory)
                            fileFlagsAndAttributes |=
                                FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS;

                        if (reparse)
                            fileFlagsAndAttributes |=
                                FileFlagsAndAttributes.FILE_FLAG_OPEN_REPARSE_POINT;

                        handle = UnsafeNativeMethods.CreateFile(
                            fileName, FileAccessMask.FILE_NONE,
                            FileShareMode.FILE_SHARE_NONE, IntPtr.Zero,
                            FileCreationDisposition.OPEN_EXISTING,
                            fileFlagsAndAttributes, IntPtr.Zero);

                        if (NativeOps.IsValidHandle(handle))
                        {
                            if (UnsafeNativeMethods.GetFileInformationByHandle(
                                    handle, ref fileInformation))
                            {
                                return ReturnCode.Ok;
                            }
                        }

                        error = NativeOps.GetErrorMessage();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        if (NativeOps.IsValidHandle(handle))
                        {
                            try
                            {
                                NativeOps.UnsafeNativeMethods.CloseHandle(
                                    handle); /* throw */
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(PathOps).Name,
                                    TracePriority.NativeError);
                            }

                            handle = IntPtr.Zero;
                        }
                    }
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPathInformation(
            string path,         /* in */
            bool directory,      /* in */
            bool reparse,        /* in */
            ref StringList list, /* out */
            ref Result error     /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, directory, reparse, ref fileInformation,
                        ref error) == ReturnCode.Ok)
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "name", path,
                        "directory", directory.ToString(),
                        "attributes",
                        fileInformation.dwFileAttributes.ToString(),
                        "created",
                        ConversionOps.ToULong(
                            fileInformation.ftCreationTime.dwLowDateTime,
                            fileInformation.ftCreationTime.dwHighDateTime).ToString(),
                        "accessed",
                        ConversionOps.ToULong(
                            fileInformation.ftLastAccessTime.dwLowDateTime,
                            fileInformation.ftLastAccessTime.dwHighDateTime).ToString(),
                        "modified",
                        ConversionOps.ToULong(
                            fileInformation.ftLastWriteTime.dwLowDateTime,
                            fileInformation.ftLastWriteTime.dwHighDateTime).ToString(),
                        "vsn",
                        fileInformation.dwVolumeSerialNumber.ToString(),
                        "size",
                        ConversionOps.ToULong(
                            fileInformation.nFileSizeLow,
                            fileInformation.nFileSizeHigh).ToString(),
                        "index",
                        ConversionOps.ToULong(
                            fileInformation.nFileIndexLow,
                            fileInformation.nFileIndexHigh).ToString(),
                        "links",
                        fileInformation.nNumberOfLinks.ToString());

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSize(
            string path,      /* in */
            bool directory,   /* in */
            ref Result result /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, directory, false, ref fileInformation,
                        ref result) == ReturnCode.Ok)
                {
                    result = ConversionOps.ToULong(
                        fileInformation.nFileSizeLow,
                        fileInformation.nFileSizeHigh).ToString();

                    return ReturnCode.Ok;
                }
            }
            else
            {
                result = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was stolen directly from the Tcl 8.6
        //       source code and modified to work in C#.
        //
        private static ulong ToTimeT(
            UnsafeNativeMethods.FILETIME fileTime /* in */
            )
        {
            ulong converted = ConversionOps.ToULong(
                fileTime.dwLowDateTime, fileTime.dwHighDateTime);

            return (converted - POSIX_EPOCH_AS_FILETIME) / 10000000;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was stolen directly from the Tcl 8.6
        //       source code and modified to work in C#.
        //
        private static FileStatusModes GetMode(
            FileFlagsAndAttributes flagsAndAttributes, /* in */
            bool checkLinks,                           /* in */
            bool isExecutable,                         /* in */
            bool userOnly                              /* in */
            )
        {
            FileStatusModes mode = FileStatusModes.S_INONE;

            if (checkLinks && FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_REPARSE_POINT, true))
            {
                mode |= FileStatusModes.S_IFLNK;
            }
            else if (FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY, true))
            {
                mode |= FileStatusModes.S_IFDIR | FileStatusModes.S_IEXEC;
            }
            else
            {
                mode |= FileStatusModes.S_IFREG;
            }

            if (FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_READONLY, true))
            {
                mode |= FileStatusModes.S_IREAD;
            }
            else
            {
                mode |= FileStatusModes.S_IREAD | FileStatusModes.S_IWRITE;
            }

            if (isExecutable)
                mode |= FileStatusModes.S_IEXEC;

            if (!userOnly)
            {
                mode |= (FileStatusModes)((int)(mode & FileStatusModes.S_IRWX) >> 3); /* group */
                mode |= (FileStatusModes)((int)(mode & FileStatusModes.S_IRWX) >> 6); /* other */
            }

            return mode;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MightBeExecutable(
            string path /* in */
            )
        {
            try
            {
                if (String.IsNullOrEmpty(path))
                    return false;

                if (PlatformOps.IsWindowsOperatingSystem() &&
                    UnsafeNativeMethods.PathIsExe(path)) /* throw */
                {
                    return true;
                }

                string extension = GetExtension(path);

                if (String.IsNullOrEmpty(extension))
                    return false;

                if (SharedStringOps.Equals(extension,
                        FileExtension.Command, ComparisonType) ||
                    SharedStringOps.Equals(extension,
                        FileExtension.Executable, ComparisonType) ||
                    SharedStringOps.Equals(extension,
                        FileExtension.Batch, ComparisonType))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        private static ReturnCode GetOwner(
            string path,                      /* in */
            ref IdentityReference ownerUser,  /* out */
            ref IdentityReference ownerGroup, /* out */
            ref Result error                  /* out */
            )
        {
            try
            {
                //
                // NOTE: Attempt to get the file security object for this
                //       file or directory -AND- use the correct method
                //       based on whether this path represents a file or
                //       directory.
                //
                FileSystemSecurity security = Directory.Exists(path) ?
                    (FileSystemSecurity)Directory.GetAccessControl(path) :
                    File.GetAccessControl(path);

                //
                // NOTE: Attempt to get the owning user and group for this
                //       file or directory.
                //
                ownerUser = security.GetOwner(typeof(SecurityIdentifier));
                ownerGroup = security.GetGroup(typeof(SecurityIdentifier));

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used directly by the [file lstat] and [file stat]
        //       sub-commands.
        //
        public static ReturnCode GetStatus(
            string path,         /* in */
            bool checkLinks,     /* in */
            bool reparse,        /* in */
            ref StringList list, /* in, out */
            ref Result error     /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                string uid = Value.ZeroString;
                string gid = Value.ZeroString;

#if !NET_STANDARD_20 && !MONO
                IdentityReference ownerUser = null;
                IdentityReference ownerGroup = null;

                if (GetOwner(
                        path, ref ownerUser, ref ownerGroup,
                        ref error) == ReturnCode.Ok)
#endif
                {
#if !NET_STANDARD_20 && !MONO
                    uid = ownerUser.ToString();
                    gid = ownerGroup.ToString();
#endif

                    UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                    InitializeFileInformation(out fileInformation);

                    if (GetPathInformation(
                            path, Directory.Exists(path), reparse,
                            ref fileInformation, ref error) == ReturnCode.Ok)
                    {
                        int device = 0;

                        if (!String.IsNullOrEmpty(path) && Char.IsLetter(path[0]))
                            device = Char.ToLower(path[0]) - Characters.a;

                        int mode = (int)GetMode(
                            fileInformation.dwFileAttributes, checkLinks,
                            MightBeExecutable(path), false);

                        if (list == null)
                            list = new StringList();

                        list.Add(
                            "dev",
                            device.ToString(),
                            "ino",
                            ConversionOps.ToULong(
                                fileInformation.nFileIndexLow,
                                fileInformation.nFileIndexHigh).ToString(),
                            "mode",
                            mode.ToString(),
                            "nlink",
                            fileInformation.nNumberOfLinks.ToString(),
                            "uid",
                            uid,
                            "gid",
                            gid,
                            "rdev",
                            fileInformation.dwVolumeSerialNumber.ToString(),
                            "size",
                            ConversionOps.ToULong(
                                fileInformation.nFileSizeLow,
                                fileInformation.nFileSizeHigh).ToString(),
                            "atime",
                            ToTimeT(
                                fileInformation.ftLastAccessTime).ToString(),
                            "mtime",
                            ToTimeT(
                                fileInformation.ftLastWriteTime).ToString(),
                            "ctime",
                            ToTimeT(
                                fileInformation.ftCreationTime).ToString(),
                            "type",
                            FileOps.GetFileType(path));

                        return ReturnCode.Ok;
                    }
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetObjectId(
            string fileName,                                           /* in */
            bool directory,                                            /* in */
            bool create,                                               /* in */
            ref UnsafeNativeMethods.FILE_OBJECTID_BUFFER fileObjectId, /* out */
            ref Result error                                           /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                IntPtr handle = IntPtr.Zero;

                if (!String.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        FileFlagsAndAttributes fileFlagsAndAttributes =
                            FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

                        if (directory)
                            fileFlagsAndAttributes |=
                                FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS;

                        handle = UnsafeNativeMethods.CreateFile(fileName,
                            FileAccessMask.FILE_NONE,
                            FileShareMode.FILE_SHARE_READ_WRITE, IntPtr.Zero,
                            FileCreationDisposition.OPEN_EXISTING,
                            fileFlagsAndAttributes, IntPtr.Zero);

                        if (NativeOps.IsValidHandle(handle))
                        {
                            IntPtr outBuffer = IntPtr.Zero;

                            try
                            {
                                int outBufferSize = Marshal.SizeOf(typeof(
                                    UnsafeNativeMethods.FILE_OBJECTID_BUFFER));

                                outBuffer = Marshal.AllocCoTaskMem(
                                    outBufferSize);

                                if (outBuffer != IntPtr.Zero)
                                {
                                    uint bytesReturned = 0;

                                    if (UnsafeNativeMethods.DeviceIoControl(
                                            handle, create ?
                                                UnsafeNativeMethods.FSCTL_CREATE_OR_GET_OBJECT_ID :
                                                UnsafeNativeMethods.FSCTL_GET_OBJECT_ID,
                                            IntPtr.Zero, 0, outBuffer, (uint)outBufferSize,
                                            ref bytesReturned, IntPtr.Zero))
                                    {
                                        fileObjectId = (UnsafeNativeMethods.FILE_OBJECTID_BUFFER)
                                            Marshal.PtrToStructure(outBuffer,
                                                typeof(UnsafeNativeMethods.FILE_OBJECTID_BUFFER));

                                        return ReturnCode.Ok;
                                    }
                                }
                                else
                                {
                                    error = "out of memory";
                                }
                            }
                            finally
                            {
                                if (outBuffer != IntPtr.Zero)
                                {
                                    Marshal.FreeCoTaskMem(outBuffer);
                                    outBuffer = IntPtr.Zero;
                                }
                            }
                        }

                        error = NativeOps.GetErrorMessage();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        if (NativeOps.IsValidHandle(handle))
                        {
                            try
                            {
                                NativeOps.UnsafeNativeMethods.CloseHandle(
                                    handle); /* throw */
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(PathOps).Name,
                                    TracePriority.NativeError);
                            }

                            handle = IntPtr.Zero;
                        }
                    }
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetObjectId(
            string path,         /* in */
            bool directory,      /* in */
            bool create,         /* in */
            ref StringList list, /* out */
            ref Result error     /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.FILE_OBJECTID_BUFFER fileObjectId =
                    new UnsafeNativeMethods.FILE_OBJECTID_BUFFER();

                if (GetObjectId(
                        path, directory, create,
                        ref fileObjectId,
                        ref error) == ReturnCode.Ok)
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "name", path,
                        "directory",
                        directory.ToString(),
                        "create",
                        create.ToString(),
                        "objectId",
                        ArrayOps.ToHexadecimalString(fileObjectId.ObjectId),
                        "birthVolumeId",
                        ArrayOps.ToHexadecimalString(fileObjectId.BirthVolumeId),
                        "birthObjectId",
                        ArrayOps.ToHexadecimalString(fileObjectId.BirthObjectId),
                        "domainId",
                        ArrayOps.ToHexadecimalString(fileObjectId.DomainId));

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsDriveLetterAndColon(
            string path, /* in */
            bool exact   /* in */
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return false;

            return IsDriveLetterAndColon(path, length, exact);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsDriveLetterAndColon(
            string path, /* in */
            int length,  /* in */
            bool exact   /* in */
            )
        {
            int offset = 0;

            return IsDriveLetterAndColon(path, length, exact, ref offset);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsDriveLetterAndColon(
            string path,    /* in */
            bool exact,     /* in */
            out int length, /* out */
            ref int offset  /* out */
            )
        {
            if (StringOps.IsNullOrEmpty(path, out length))
                return false;

            return IsDriveLetterAndColon(path, length, exact, ref offset);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsDriveLetterAndColon(
            string path,   /* in */
            int length,    /* in */
            bool exact,    /* in */
            ref int offset /* out */
            )
        {
            if (exact)
            {
                if (length != DrivePrefixLength) // "C:" -OR- "c:"
                    return false;
            }
            else
            {
                if (length < DrivePrefixLength) // "C:\" -OR- "c:\"
                    return false;
            }

            if (!StringOps.CharIsAsciiAlpha(path[0]))
                return false;

            if (path[1] != Characters.Colon)
                return false;

            offset = DrivePrefixLength;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsExtended(
            string path,   /* in */
            int length,    /* in */
            ref int offset /* out */
            )
        {
            if (length < ExtendedPrefixLength) // "\\?\" -OR- "\??\"
                return false;

            if (path[0] != Characters.Backslash)
                return false;

            if ((path[1] != Characters.Backslash) &&
                (path[1] != Characters.QuestionMark))
            {
                return false;
            }

            if (path[2] != Characters.QuestionMark)
                return false;

            if (path[3] != Characters.Backslash)
                return false;

            offset = ExtendedPrefixLength;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasUncPrefix(
            string path /* in */
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return false;

            return HasUncPrefix(path, length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasUncPrefix(
            string path, /* in */
            int length   /* in */
            )
        {
            int offset = 0;

            return HasUncPrefix(path, length, ref offset);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasUncPrefix(
            string path,   /* in */
            int length,    /* in */
            ref int offset /* out */
            )
        {
            if (length < UncPrefixLength) // "\\more"
                return false;

            if ((path[0] == Characters.Backslash) &&
                (path[1] == Characters.Backslash))
            {
                offset = UncPrefixLength;
                return true;
            }

            if ((path[0] == Characters.Slash) &&
                (path[1] == Characters.Slash))
            {
                offset = UncPrefixLength;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static char[] GetInvalidChars( /* MAY RETURN NULL */
            bool fileNameOnly /* in */
            )
        {
            return fileNameOnly ?
                Path.GetInvalidFileNameChars() :
                Path.GetInvalidPathChars();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ValidatePathAsComponents(
            string path,    /* in */
            bool allowDrive /* in */
            )
        {
            string[] parts = MaybeSplit(path, false);

            if (parts == null)
                return false;

            char[] characters = GetInvalidChars(true);
            bool hasDrivePrefix = false;
            int count = parts.Length;

            for (int index = 0; index < count; index++)
            {
                string part = parts[index];
                int length;

                if (StringOps.IsNullOrEmpty(part, out length))
                {
                    if (index == 0)
                        continue; /* "/home/file.txt" */

                    if (hasDrivePrefix && (index == 1))
                        continue; /* "C:/" */

                    if (index == (count - 1))
                        continue; /* "C:/trailing/" */

                    return false; /* empty component? */
                }

                if (allowDrive && (index == 0) &&
                    IsDriveLetterAndColon(part, length, true))
                {
                    hasDrivePrefix = true;
                    continue;
                }

                if ((characters != null) && part.IndexOfAny(
                        characters, 0) != Index.Invalid)
                {
                    return false; /* bad component? */
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CheckForValid(
            bool? unix,         /* in: NOT USED */
            string path,        /* in */
            bool fileNameOnly,  /* in */
            bool allowExtended, /* in */
            bool useComponents, /* in */
            bool allowDrive     /* in */
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return false;

            int startIndex = 0;

            if (!fileNameOnly && allowExtended)
            {
                int offset = 0;

                if (IsExtended(path, length, ref offset))
                    startIndex += offset;
            }

            char[] characters = GetInvalidChars(fileNameOnly);

            if ((characters != null) && (path.IndexOfAny(
                    characters, startIndex) != Index.Invalid))
            {
                return false;
            }

            if (useComponents)
            {
                if (startIndex > 0)
                    path = path.Substring(startIndex);

                if (!ValidatePathAsComponents(path, allowDrive))
                    return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasPathWildcard(
            string value /* in */
            )
        {
            return (value != null) && (PathWildcardChars != null) &&
                (value.IndexOfAny(PathWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CleanPath(
            string path,      /* in */
            bool full,        /* in */
            char? invalidChar /* in */
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: First, remove any surrounding double quotes.
                //
                // TODO: Maybe only remove *ONE* set of surrounding double quotes.
                //
                result = result.Trim(Characters.QuotationMark);

                if (full)
                {
                    //
                    // NOTE: Full cleaning required, remove all invalid path
                    //       characters.
                    //
                    StringBuilder builder = StringOps.NewStringBuilder(result);

                    foreach (char character in Path.GetInvalidPathChars())
                    {
                        if (invalidChar != null)
                            builder.Replace(character, (char)invalidChar);
                        else
                            builder.Replace(character.ToString(), null);
                    }

                    result = builder.ToString();
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB && !NET_STANDARD_20 && !MONO && (NET_20_SP2 || NET_40)
        //
        // HACK: Using the "HttpRuntime.UsingIntegratedPipeline" property when
        //       running on Mono seems to cause serious problems (I guess they
        //       cannot just return false).  Apparently, even referring to this
        //       method causes Mono to crash; therefore, it has been moved
        //       to a method by itself (which seems to get around the problem).
        //
        private static bool HttpRuntimeUsingIntegratedPipeline()
        {
            return HttpRuntime.UsingIntegratedPipeline;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB && !NET_STANDARD_20
        private static bool HaveHttpContext()
        {
            HttpContext context = null;

            return HaveHttpContext(ref context);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveHttpContext(
            ref HttpContext context /* out */
            )
        {
            context = HttpContext.Current;

            return (context != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveHttpServerUtility(
            HttpContext context,         /* in */
            ref HttpServerUtility server /* out */
            )
        {
            if (context == null)
                return false;

            server = context.Server;

            return (server != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveHttpRequest(
            HttpContext context,    /* in */
            ref HttpRequest request /* out */
            )
        {
            if (context == null)
                return false;

            request = context.Request;

            return (request != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetServerName()
        {
            HttpContext context = null;

            if (HaveHttpContext(ref context))
            {
                HttpServerUtility server = null;

                if (HaveHttpServerUtility(context, ref server))
                    return server.MachineName;
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetBinaryPath(
            bool full /* in */
            )
        {
            string result = null;

            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (binaryPath != null)
                    {
                        //
                        // NOTE: Use the specified path verbatim,
                        //       without any trimming of trailing
                        //       separators; however, if the full
                        //       parameter is true, resolve it to
                        //       an absolute path first.
                        //
                        TraceOps.DebugTrace(
                            "GetBinaryPath: using manual override",
                            typeof(PathOps).Name,
                            TracePriority.StartupDebug);

                        result = full ?
                            Path.GetFullPath(binaryPath) : /* throw */
                            binaryPath;
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.StartupError);
            }

            if (result != null)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetBinaryPath: full = {0}, result = {1}",
                    full, FormatOps.WrapOrNull(result)),
                    typeof(PathOps).Name,
                    TracePriority.StartupDebug);

                return result;
            }

            try
            {
#if WEB && !NET_STANDARD_20
                //
                // NOTE: Are we running in a web application?
                //
                HttpContext context = null;

                if (HaveHttpContext(ref context))
                {
                    TraceOps.DebugTrace(
                        "GetBinaryPath: found HTTP context",
                        typeof(PathOps).Name,
                        TracePriority.StartupDebug);

                    HttpServerUtility server = null;

                    if (HaveHttpServerUtility(context, ref server))
                    {
                        TraceOps.DebugTrace(
                            "GetBinaryPath: found HTTP server utility",
                            typeof(PathOps).Name,
                            TracePriority.StartupDebug);

                        string path = null;

#if !MONO && (NET_20_SP2 || NET_40)
                        //
                        // HACK: Using "HttpRuntime.UsingIntegratedPipeline" on
                        //       Mono seems to cause serious problems (I guess
                        //       they cannot just return false).  Apparently,
                        //       even referring to this method causes Mono to
                        //       crash; therefore, the check has been moved to
                        //       a method by itself (which seems to get around
                        //       the problem).
                        //
                        if (!CommonOps.Runtime.IsMono() &&
                            HttpRuntimeUsingIntegratedPipeline())
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: detected IIS integrated-pipeline mode",
                                typeof(PathOps).Name, TracePriority.StartupDebug);

                            //
                            // NOTE: Get the root of the web application (for
                            //       use in IIS7+ integrated mode).
                            //
                            path = HttpRuntime.AppDomainAppVirtualPath;
                        }
                        else
#endif
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: detected IIS classic mode",
                                typeof(PathOps).Name, TracePriority.StartupDebug);

                            //
                            // NOTE: Grab and verify the HTTP request object.
                            //
                            HttpRequest request = null;

                            if (HaveHttpRequest(context, ref request))
                            {
                                TraceOps.DebugTrace(
                                    "GetBinaryPath: found HTTP request",
                                    typeof(PathOps).Name, TracePriority.StartupDebug);

                                //
                                // NOTE: Get the root of the web application.
                                //
                                path = request.ApplicationPath;
                            }
                            else
                            {
                                TraceOps.DebugTrace(
                                    "GetBinaryPath: no HTTP request",
                                    typeof(PathOps).Name, TracePriority.StartupError);
                            }
                        }

                        //
                        // NOTE: Map the application path to the local file
                        //       system path and append the "bin" folder, which
                        //       should always be there according to MSDN.
                        //
                        if (path != null)
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: mapping path from HTTP context",
                                typeof(PathOps).Name, TracePriority.StartupError);

                            result = CombinePath(
                                null, server.MapPath(path), TclVars.Path.Bin);
                        }
                        else
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: no path from HTTP context",
                                typeof(PathOps).Name, TracePriority.StartupError);
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "GetBinaryPath: no HTTP server utility",
                            typeof(PathOps).Name, TracePriority.StartupError);
                    }
                }
                else
#endif
                {
                    TraceOps.DebugTrace(
                        "GetBinaryPath: no HTTP context",
                        typeof(PathOps).Name, TracePriority.StartupDebug);

                    //
                    // NOTE: Use the base directory of the current application
                    //       domain.
                    //
                    AppDomain appDomain = AppDomainOps.GetCurrent();

                    if (AppDomainOps.IsDefault(appDomain))
                    {
                        TraceOps.DebugTrace(
                            "GetBinaryPath: default application domain",
                            typeof(PathOps).Name, TracePriority.StartupDebug);

                        result = appDomain.BaseDirectory;
                    }
                    else
                    {
                        //
                        // HACK: This is an isolated AppDomain.  There is
                        //       [probably?] no entry assembly available and
                        //       the AppDomain base directory is not reliable
                        //       for the purpose of loading packages;
                        //       therefore, just use the directory of the
                        //       current (Eagle) assembly.
                        //
                        TraceOps.DebugTrace(
                            "GetBinaryPath: non-default application domain",
                            typeof(PathOps).Name, TracePriority.StartupDebug);

                        result = GlobalState.InitializeOrGetAssemblyPath(true);
                    }
                }

                //
                // NOTE: Remove trailing directory separator characters, if
                //       necessary.
                //
                if (result != null)
                    result = TrimEndOfPath(result, null);

                //
                // NOTE: Finally, if requested, fully resolve to an absolute
                //       path if we were requested to do so.
                //
                if (full && (result != null))
                    result = Path.GetFullPath(result); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.StartupError);
            }

            TraceOps.DebugTrace(String.Format(
                "GetBinaryPath: full = {0}, result = {1}",
                full, FormatOps.WrapOrNull(result)),
                typeof(PathOps).Name,
                TracePriority.StartupDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetProcessorPath(
            string path,       /* in */
            bool alternateName /* in */
            )
        {
            string result = path;

            if (String.IsNullOrEmpty(result))
                return result;

#if NATIVE
            string processorName = alternateName ?
                PlatformOps.GetAlternateProcessorName() :
                PlatformOps.GetProcessorName();
#else
            string processorName = PlatformOps.GetProcessorName();
#endif

            if (processorName != null)
                result = CombinePath(null, result, processorName);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is designed for use by the [info path]
        //          sub-command only.
        //
        public static ReturnCode GetInfoPath(
            Interpreter interpreter,   /* in: OPTIONAL */
            InfoPathType infoPathType, /* in */
            ref Result result          /* out */
            )
        {
            bool full = FlagOps.HasFlags(
                infoPathType, InfoPathType.Full, true);

            bool local = FlagOps.HasFlags(
                infoPathType, InfoPathType.Local, true);

            bool noProcessor = FlagOps.HasFlags(
                infoPathType, InfoPathType.NoProcessor, true);

            bool alternateName = FlagOps.HasFlags(
                infoPathType, InfoPathType.AlternateName, true);

            ReturnCode code = ReturnCode.Ok;
            bool needFull = full;
            bool needProcessor = !noProcessor;
            string path = null;

            try
            {
                switch (infoPathType & InfoPathType.TypeMask)
                {
                    case InfoPathType.NativeProcess:
                        {
                            path = GetProcessMainModulePath(full);
                            needFull = false;
                            break;
                        }
                    case InfoPathType.NativeBinary:
                        {
                            path = GetBinaryPath(true);
                            break;
                        }
                    case InfoPathType.NativeLibrary:
                        {
                            path = GetLibPath(
                                local, noProcessor, alternateName);

                            needProcessor = false; /* Already included. */
                            break;
                        }
                    case InfoPathType.NativeExternals:
                        {
                            path = GlobalState.GetExternalsPath();
                            break;
                        }
                    case InfoPathType.ScriptLibraryBase:
                        {
                            path = GlobalState.GetLibraryPath(
                                interpreter, false, false);

                            break;
                        }
                    default:
                        {
                            result = String.Format(
                                "unsupported path type \"{0}\"", infoPathType);

                            code = ReturnCode.Error;
                            break;
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
                if (code == ReturnCode.Ok)
                {
                    if (path != null)
                    {
                        if ((code == ReturnCode.Ok) && needProcessor)
                        {
                            try
                            {
                                path = GetProcessorPath(
                                    path, alternateName); /* throw */
                            }
                            catch (Exception e)
                            {
                                result = e;
                                code = ReturnCode.Error;
                            }
                        }

                        if ((code == ReturnCode.Ok) && needFull)
                        {
                            try
                            {
                                path = Path.GetFullPath(path); /* throw */
                            }
                            catch (Exception e)
                            {
                                result = e;
                                code = ReturnCode.Error;
                            }
                        }

                        if (code == ReturnCode.Ok)
                            result = path;
                    }
                    else
                    {
                        result = String.Format(
                            "no information available for path type \"{0}\"",
                            infoPathType);

                        code = ReturnCode.Error;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetLibPath(
            bool local,        /* in */
            bool noProcessor,  /* in */
            bool alternateName /* in */
            )
        {
#if UNIX
            string path = local ?
                TclVars.Path.UserLocalLib : TclVars.Path.UserLib;

            if (PlatformOps.IsMacintoshOperatingSystem())
            {
                return noProcessor ?
                    path : GetProcessorPath(path, alternateName);
            }
            else
            {
                return String.Format("{0}{1}", noProcessor ?
                    path : GetProcessorPath(path, alternateName),
                    TclVars.Path.LinuxGnuSuffix);
            }
#else
            string path = GetBinaryPath(true);

            return noProcessor ?
                path : GetProcessorPath(path, alternateName);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetProcessMainModulePath(
            bool full /* in */
            )
        {
            return Path.GetDirectoryName(
                GetProcessMainModuleFileName(full));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetProcessMainModuleFileName(
            bool full /* in */
            )
        {
            return GetProcessMainModuleFileName(
                ProcessOps.GetCurrent(), full);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetProcessMainModuleFileName(
            Process process, /* in */
            bool full        /* in */
            )
        {
            try
            {
                if (process != null)
                {
                    ProcessModule module = process.MainModule;

                    if (module != null)
                    {
                        return full ?
                            Path.GetFullPath(module.FileName) : /* throw */
                            module.FileName;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        public static string GetNativeModuleFileName(
            IntPtr module,   /* in */
            ref Result error /* out */
            )
        {
            IntPtr outBuffer = IntPtr.Zero;

            try
            {
                uint outBufferSize = UNICODE_STRING_MAX_CHARS;

                outBuffer = Marshal.AllocCoTaskMem(
                    (int)(outBufferSize + 1) * sizeof(char));

                uint result = NativeOps.GetModuleFileName(
                    module, outBuffer, outBufferSize);

                //
                // NOTE: If the result is zero, the function
                //       failed.
                //
                if (result > 0)
                {
                    //
                    // NOTE: Set the module file name to the
                    //       contents of the output buffer, up
                    //       to the returned length (which may
                    //       have been truncated).
                    //
                    return Marshal.PtrToStringAuto(
                        outBuffer, (int)result);
                }
                else
                {
                    //
                    // NOTE: Failure, cannot resolve the module
                    //       file name.
                    //
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "cannot resolve module file name, " +
                        "GetModuleFileName({1}) failed with " +
                        "error {0}: {2}", lastError, module,
                        NativeOps.GetDynamicLoadingError(lastError));
                }

                //
                // NOTE: If we reach this point, fail.
                //
                return null;
            }
            finally
            {
                if (outBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(outBuffer);
                    outBuffer = IntPtr.Zero;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetNativeExecutableName()
        {
            Result error = null;

            return GetNativeModuleFileName(IntPtr.Zero, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetManagedExecutableName()
        {
            return GetManagedExecutableName(
                ProcessOps.GetCurrent(), true, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetManagedExecutableName(
            Process process, /* in */
            bool fallback,   /* in */
            bool full        /* in */
            )
        {
            string location = GlobalState.GetEntryAssemblyLocation();

            if (location != null)
                return location;

            if (fallback)
                return GetProcessMainModuleFileName(process, full);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetTempFileName() /* throw */
        {
            GetStringValueCallback callback;

            lock (syncRoot)
            {
                callback = getTempFileNameCallback;
            }

            string result;

            try
            {
                if (callback != null)
                {
                    result = callback(); /* throw */
                }
                else
                {
                    result = Path.GetTempFileName(); /* throw */
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);

                throw;
            }

            TraceOps.DebugTrace(String.Format(
                "GetTempFileName: result = {0}",
                FormatOps.WrapOrNull(result)),
                typeof(PathOps).Name,
                TracePriority.PathDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetTempPathViaEnvironment(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            foreach (string name in new string[] {
                    EnvVars.EagleTemp, EnvVars.XdgRuntimeDir,
                    EnvVars.Temp, EnvVars.Tmp
                })
            {
                if (String.IsNullOrEmpty(name))
                    continue;

                string path = CommonOps.Environment.GetVariable(name);

                if (String.IsNullOrEmpty(path))
                    continue;

                bool accessStatus;

                FileOps.VerifyWritable(interpreter, path, out accessStatus);

                if (accessStatus)
                    return path;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetTempPath(
            Interpreter interpreter /* in: OPTIONAL */
            ) /* throw */
        {
            GetStringValueCallback callback;

            lock (syncRoot)
            {
                callback = getTempPathCallback;
            }

            string result;

            try
            {
                if (callback != null)
                {
                    result = callback(); /* throw */
                }
                else
                {
                    result = GetTempPathViaEnvironment(
                        interpreter);

                    if (result == null)
                        result = Path.GetTempPath(); /* throw */
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);

                throw;
            }

            TraceOps.DebugTrace(String.Format(
                "GetTempPath: result = {0}",
                FormatOps.WrapOrNull(result)),
                typeof(PathOps).Name,
                TracePriority.PathDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetUniquePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string directory,        /* in */
            string prefix,           /* in: OPTIONAL */
            string suffix,           /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (String.IsNullOrEmpty(directory))
            {
                error = "invalid directory";
                return null;
            }

            if (!Directory.Exists(directory))
            {
                error = String.Format(
                    "base directory {0} does not exist",
                    FormatOps.WrapOrNull(directory));

                return null;
            }

            //
            // HACK: On Windows, there could be an 8.3 file name limit.
            //       The unique path format in that situation would be:
            //
            //           "eiq-WXYZ.tmp"
            //
            //       Where "WXYZ" is a randomly generated hexadecimal
            //       number (16-bit).  On Unix, the unique path format
            //       would be:
            //
            //           "eagle-unique-path-KLMNOPQRSTUVWXYZ.tmp"
            //
            //       Where "KLMNOPQRSTUVWXYZ" is a randomly generated
            //       hexadecimal number (64-bit).
            //
            InitializeUniquePathData();

            int byteCount = 0;
            int maximumRetries = 0;

            MaybeGetUniqueProperties(ref prefix,
                ref suffix, ref byteCount, ref maximumRetries);

            if (byteCount <= 0)
            {
                error = "invalid unique byte count";
                return null;
            }

            if (maximumRetries <= 0)
            {
                error = "invalid unique maximum retries";
                return null;
            }

            int length = byteCount * 2; /* HEXADECIMAL */

            if (prefix != null)
            {
                prefix = prefix.Trim();
                length += prefix.Length;
            }

            if (suffix != null)
            {
                suffix = suffix.Trim();
                length += suffix.Length;
            }

            if (length == 0)
            {
                error = "invalid unique path length";
                return null;
            }

            int retries = 0;

            byte[] zeroBytes = new byte[byteCount];
            byte[] pathBytes = new byte[byteCount];

            while (retries++ < maximumRetries)
            {
                //
                // NOTE: Attempt to obtain some ("random") entropy
                //       to build the final part of the path.  This
                //       must succeed or we cannot continue.
                //
                if (RuntimeOps.GetRandomBytes(interpreter,
                        ref pathBytes, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                if (ArrayOps.Equals(pathBytes, zeroBytes))
                {
                    //
                    // BUGBUG: Perhaps there is a temporary problem
                    //         with the random number generator?  I
                    //         guess we should just retry?  Can this
                    //         actually happen?
                    //
                    continue;
                }

                string id = ArrayOps.ToHexadecimalString(pathBytes,
                    true);

                if (String.IsNullOrEmpty(id))
                    continue;

                //
                // NOTE: Build the final portion of the unique path,
                //       which could end up being used as a file or
                //       directory.
                //
                string name = String.Format(
                    "{0}{1}{2}", prefix, id, suffix).Trim();

                if (String.IsNullOrEmpty(name))
                    continue;

                string path = CombinePath(null, directory, name);

                if (String.IsNullOrEmpty(path))
                    continue;

                //
                // NOTE: Nothing with this fully qualified name is
                //       allowed to exist.  When that is true, the
                //       algorithm is complete.
                //
                if (!Directory.Exists(path) && !File.Exists(path))
                    return path;
            }

            error = String.Format(
                "unable to generate unique path in {0} after {1} tries",
                FormatOps.WrapOrNull(directory), maximumRetries);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetExecutableNameOnly()
        {
            try
            {
                return Path.GetFileName(GetExecutableName()); /* throw */
            }
            catch
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetExecutableName()
        {
            return GetExecutableName(
                ProcessOps.GetCurrent(), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetExecutableName(
            Process process, /* in */
            bool full        /* in */
            )
        {
            return GetProcessMainModuleFileName(process, full);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: At some point, possibly provide an environment variable that,
        //       when set [to anything], causes this method to always return
        //       null.
        //
        private static string GetBuildConfiguration( /* MAY RETURN NULL */
            Assembly assembly /* in: OPTIONAL */
            )
        {
            return AttributeOps.GetAssemblyConfiguration(assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool StartsWithBuildConfiguration(
            string value,      /* in: OPTIONAL */
            Assembly assembly, /* in: OPTIONAL */
            out int length     /* out */
            )
        {
            length = 0;

            if (String.IsNullOrEmpty(value))
                return false;

            if (BuildConfigurations != null)
            {
                foreach (string configuration in BuildConfigurations)
                {
                    if (String.IsNullOrEmpty(configuration))
                        continue;

                    if (value.StartsWith(configuration, ComparisonType))
                    {
                        length = configuration.Length;
                        return true;
                    }
                }
            }

            if (assembly != null)
            {
                string configuration = GetBuildConfiguration(assembly);

                if (!String.IsNullOrEmpty(configuration) &&
                    value.StartsWith(configuration, ComparisonType))
                {
                    length = configuration.Length;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetFallbackFileName(
            string fileName,     /* in */
            string fileExtension /* in */
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return null;

            return String.Format("{0}{1}", fileName, fileExtension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetWebFallbackFileName(
            string fileName,     /* in */
            string fileExtension /* in */
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return null;

            return Path.Combine(
                Path.GetDirectoryName(fileName), String.Format(
                "Web{0}", fileExtension));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveLocalNames(
            bool perUser /* in */
            )
        {
            if (perUser && CommonOps.Environment.DoesVariableExist(
                    EnvVars.UserName))
            {
                return true;
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ComputerName))
            {
                return true;
            }

            if (perUser && CommonOps.Environment.DoesVariableExist(
                    EnvVars.UserDomain))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool? GetLocalNames(
            bool perUser,           /* in */
            bool? forceBuiltIn,     /* in */
            out string userName,    /* out */
            out string machineName, /* out */
            out string domainName   /* out */
            )
        {
            bool useBuiltIn;

            if (forceBuiltIn != null)
            {
                useBuiltIn = (bool)forceBuiltIn;
            }
            else if (PlatformOps.IsWindowsOperatingSystem() ||
                !HaveLocalNames(perUser))
            {
                useBuiltIn = true;
            }
            else
            {
                useBuiltIn = false;
            }

            if (useBuiltIn)
            {
                userName = Environment.UserName;
                machineName = Environment.MachineName;
                domainName = Environment.UserDomainName;
            }
            else
            {
                userName = CommonOps.Environment.GetVariable(
                    EnvVars.UserName);

                machineName = CommonOps.Environment.GetVariable(
                    EnvVars.ComputerName);

                domainName = CommonOps.Environment.GetVariable(
                    EnvVars.UserDomain);
            }

            if (!String.IsNullOrEmpty(userName) ||
                !String.IsNullOrEmpty(machineName) ||
                !String.IsNullOrEmpty(domainName))
            {
                return useBuiltIn; /* NOTE: Yes, something was found. */
            }

            return null; /* Nope, nothing. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList GetOverrideFileNames(
            string fileName,      /* in */
            string fileExtension, /* in */
            bool includeFallback, /* in */
            bool includeWeb       /* in */
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return null;

            string machineName;
            string domainName;
            string userName;

            /* IGNORED */
            GetLocalNames(
                true, true, out userName, out machineName, out domainName);

            bool machineIsDomain = StringOps.UserNoCaseEquals(
                machineName, domainName);

            bool haveMachineName = !String.IsNullOrEmpty(machineName);
            bool haveDomainName = !String.IsNullOrEmpty(domainName);
            bool haveUserName = !String.IsNullOrEmpty(userName);

            StringList list = new StringList();

            if (!machineIsDomain &&
                haveUserName && haveMachineName && haveDomainName)
            {
                list.Add(String.Format(
                    "{0}.{1}.{2}.{3}{4}", fileName, userName,
                    machineName, domainName, fileExtension));
            }

            if (haveUserName && haveMachineName)
            {
                list.Add(String.Format(
                    "{0}.{1}.{2}{3}", fileName, userName, machineName,
                    fileExtension));
            }

            if (haveUserName)
            {
                list.Add(String.Format(
                    "{0}.{1}{2}", fileName, userName, fileExtension));
            }

            if (!machineIsDomain && haveMachineName && haveDomainName)
            {
                list.Add(String.Format(
                    "{0}.{1}.{2}{3}", fileName, machineName, domainName,
                    fileExtension));
            }

            if (haveMachineName)
            {
                list.Add(String.Format(
                    "{0}.{1}{2}", fileName, machineName, fileExtension));
            }

            if (!machineIsDomain && haveDomainName)
            {
                list.Add(String.Format(
                    "{0}.{1}{2}", fileName, domainName, fileExtension));
            }

            //
            // NOTE: The default override file name MUST be present in the
            //       returned list -AND- MUST be last in the returned list.
            //       The only other alternative that this method has is to
            //       return a list value of null.
            //
            if (includeFallback)
            {
                list.Add(GetFallbackFileName(fileName, fileExtension));

#if WEB && !NET_STANDARD_20
                //
                // HACK: When compiled with System.Web support enabled,
                //       attempt to use it to see if we are executing
                //       within an ASP.NET application.  If so, add the
                //       "Web.config" file as a fallback as well.
                //
                if (includeWeb || HaveHttpContext())
#else
                //
                // NOTE: Otherwise, just include the "Web.config" file
                //       as a fallback when instructed to do so by our
                //       caller.
                //
                if (includeWeb)
#endif
                {
                    list.Add(
                        GetWebFallbackFileName(fileName, fileExtension));
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetBasePath( /* MAY RETURN NULL */
            Assembly assembly, /* in: OPTIONAL */
            string path        /* in */
            )
        {
            string suffix = null;

            return GetBasePathAndSuffix(assembly, path, ref suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetBaseSuffix( /* MAY RETURN NULL */
            Assembly assembly /* in: OPTIONAL */
            )
        {
            return GetBaseSuffix(
                assembly, GlobalState.InitializeOrGetBinaryPath(false));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetBaseSuffix( /* MAY RETURN NULL */
            Assembly assembly, /* in: OPTIONAL */
            string path        /* in */
            )
        {
            string suffix = null;

            /* IGNORED */
            GetBasePathAndSuffix(assembly, path, ref suffix);

            return suffix;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsRootPath(
            string path /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            return IsEqualFileName(
                path, Path.GetPathRoot(path));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool MaybePreMutatePath(
            ref string path /* in, out */
            )
        {
            //
            // NOTE: This value will be returned to the caller and is used
            //       to indicate if the path specified was mutated by this
            //       method.  This value is initially false because nothing
            //       has been done yet.
            //
            bool result = false;

            //
            // NOTE: Garbage in, garbage out.
            //
            if (String.IsNullOrEmpty(path))
                return result;

            //
            // NOTE: Grab the final portion of the path, which should be a
            //       directory.
            //
            string directory = Path.GetFileName(path);

            //
            // HACK: If we are running in the special "build tasks" output
            //       directory (i.e. during the build process), go up one
            //       level now.  This is necessary to support loading the
            //       build tasks from a directory other than the primary
            //       output directory, because that would prevent us from
            //       modifying the built binaries, due to assembly locking
            //       by MSBuild.
            //
            // NOTE: *STRICT-MODE* This uses exact matching, so it is not
            //       an "assumption", per se.
            //
            if (IsEqualFileName(directory, _Path.BuildTasks))
            {
                path = Path.GetDirectoryName(path);
                directory = Path.GetFileName(path);

                //
                // NOTE: The path specified by the caller has now been
                //       changed.
                //
                result = true;
            }

            //
            //
            // HACK: Handle a directory specific to the target framework
            //       (e.g. "netstandard2.0", etc).  These are generally
            //       applicable only when running from inside the source
            //       tree.
            //
            // BUGBUG: Maybe only do this when running on .NET Core?  In
            //         general, this should not cause any issues, unless
            //         the core library has been deployed to a directory
            //         that starts with "net".
            //
            // NOTE: *STRICT-MODE* This uses prefix matching, so it is not
            //       an "assumption", per se.
            //
            if (StartsWithFileName(directory, _Path.NetPrefix))
            {
                path = Path.GetDirectoryName(path);
                directory = Path.GetFileName(path); /* NOT USED */

                //
                // NOTE: The path specified by the caller has now been
                //       changed.
                //
                result = true;
            }

            //
            // NOTE: Return non-zero to the caller if the path specified
            //       was actually changed by this method.
            //
            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MaybeRemoveBinDirectory(
            ref string path /* in, out */
            )
        {
            //
            // NOTE: This value will be returned to the caller and is used
            //       to indicate if the path specified was mutated by this
            //       method.  This value is initially false because nothing
            //       has been done yet.
            //
            bool result = false;

            //
            // NOTE: Garbage in, garbage out.
            //
            if (String.IsNullOrEmpty(path))
                return result;

            //
            // NOTE: Never want to do anything if the specified path is the
            //       root directory of the drive we are on.
            //
            // NOTE: *STRICT-MODE* This uses exact matching, so it is not
            //       an "assumption", per se.
            //
            if (IsRootPath(path))
                return result;

            //
            // NOTE: Grab the final portion of the path, which should be a
            //       directory.
            //
            string directory = Path.GetFileName(path);

            //
            // BUGBUG: This will not always do the right thing because it
            //         is unconditional.  Need to make this check smarter.
            //         *UPDATE* Actually, now this is conditional; however,
            //         it still may not be smart enough.
            //
            // HACK: Go up one level to get to the parent directory of the
            //       inner "bin" and "lib" directories.  This is not really
            //       optimal because it assumes the specified path must end
            //       with a "bin" directory and thus also typically assumes
            //       that the assembly for the core library itself must
            //       always reside within a "bin" directory to function
            //       properly when deployed.
            //
            // HACK: If the "StrictBasePath" environment variable is set,
            //       only remove the final directory of the path if it is
            //       equal to "bin".
            //
            // NOTE: *STRICT-MODE* This uses exact matching (when in strict
            //       mode), so it is not an "assumption", per se.
            //
            if (!CommonOps.Environment.DoesVariableExist(
                    EnvVars.StrictBasePath) ||
                IsEqualFileName(directory, TclVars.Path.Bin))
            {
                path = Path.GetDirectoryName(path);
                directory = Path.GetFileName(path); /* NOT USED */

                //
                // NOTE: The path specified by the caller has now been
                //       changed.
                //
                result = true;
            }

            //
            // NOTE: Return non-zero to the caller if the path specified
            //       was actually changed by this method.
            //
            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: At some point, it might be nice to support
        //       customizing (and/or skipping) the various
        //       internal behaviors of this method.
        //
        public static string GetBasePathAndSuffix( /* MAY RETURN NULL */
            Assembly assembly, /* in: OPTIONAL */
            string path,       /* in */
            ref string suffix  /* out */
            )
        {
            //
            // NOTE: Start with their entire path, verbatim.
            //
            string result = path;

            //
            // NOTE: Garbage in, garbage out.
            //
            // NOTE: *STRICT-MODE* This uses exact matching, so it is not
            //       an "assumption", per se.
            //
            if (String.IsNullOrEmpty(result))
                return result;

            //
            // NOTE: Maybe modify the incoming path to handle various corner
            //       cases (e.g. running from inside the source tree, Mono,
            //       .NET Core, etc).
            //
            /* IGNORED */
            MaybePreMutatePath(ref result);

            //
            // NOTE: Maybe modify the (now possibly modified) path to remove
            //       the trailing "bin" directory, if applicable.
            //
            /* IGNORED */
            MaybeRemoveBinDirectory(ref result);

            //
            // NOTE: Get the name of the directory at this level, which may be
            //       different than the original level specified by the caller.
            //
            string directory = Path.GetFileName(result);

            //
            // HACK: If it looks like we are running from the build directory
            //       for this configuration, go up another level to compensate.
            //       If the assembly configuration is null or empty, skip this
            //       step.  This is not optimal because it assumes a directory
            //       name starting with "Debug" or "Release" cannot be the base
            //       directory.
            //
            // NOTE: *STRICT-MODE* This uses prefix matching, so it is not
            //       an "assumption", per se.
            //
            int length;

            if (/* DebugOps.IsAttached() || */
                StartsWithBuildConfiguration(directory, assembly, out length))
            {
                string localSuffix = directory.Substring(length);

                if (!String.IsNullOrEmpty(localSuffix))
                    suffix = localSuffix;

                result = Path.GetDirectoryName(result);
                directory = Path.GetFileName(result);
            }

            //
            // HACK: We want the parent directory of the outer "bin" directory
            //       (which will only be in the result string at this point if
            //       we are running from the build output directory), if any.
            //       This is not optimal because it assumes a directory named
            //       "bin" cannot be the base directory.
            //
            // NOTE: *STRICT-MODE* This uses exact matching, so it is not
            //       an "assumption", per se.
            //
            if (/* DebugOps.IsAttached() || */
                IsEqualFileName(directory, TclVars.Path.Bin))
            {
                result = Path.GetDirectoryName(result);
                directory = Path.GetFileName(result); /* NOT USED */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string[] MaybeSplit(
            string path /* in: CANNOT BE NULL */
            )
        {
            return MaybeSplit(path, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string[] MaybeSplit(
            string path,  /* in: CANNOT BE NULL */
            bool fallback /* in */
            )
        {
            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return fallback ? new string[] { path } : null;

            return path.Split(characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeTrim(
            string path /* in: CANNOT BE NULL */
            )
        {
            return MaybeTrim(path, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeTrim(
            string path, /* in: CANNOT BE NULL */
            bool? both   /* in */
            )
        {
            char[] characters;

            if (!TryGetDirectoryChars(both, out characters))
                return path;

            return path.Trim(characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeTrimStart(
            string path /* in: CANNOT BE NULL */
            )
        {
            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return path;

            return path.TrimStart(characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeTrimEnd(
            string path /* in: CANNOT BE NULL */
            )
        {
            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return path;

            return path.TrimEnd(characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TrimEndOfPath(
            string path,    /* in */
            char? separator /* in */
            )
        {
            //
            // NOTE: If the original path string is null or empty, just return
            //       it as we cannot do anything else meaningful with it.
            //
            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return path;

            //
            // BUGFIX: Whatever the only character may be, we cannot reasonably
            //         be expected to remove it (i.e. even if it is a directory
            //         separator).
            //
            if (length == 1)
                return path;

            //
            // NOTE: If the last character is not a directory separator then
            //       there is no trimming to be done.
            //
            if (!IsDirectoryChar(path[length - 1]))
                return path;

            //
            // NOTE: Figure out the suffix, if any, we may need to append to
            //       the result.
            //
            string suffix = String.Empty;

            if (separator != null)
                suffix = separator.ToString();

            //
            // NOTE: Trim all trailing directory separator characters from the
            //       end of the path string and append the separator character
            //       provided by the caller.
            //
            return MaybeTrimEnd(path) + suffix;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList SplitPath(
            bool? unix, /* in */
            string path /* in */
            )
        {
            if (path == null)
                return null;

            path = path.Trim();

            if (path.Length == 0)
                return new StringList();

            char separator;

            if (unix != null)
            {
                separator = (bool)unix ?
                    AltDirectorySeparatorChar :
                    DirectorySeparatorChar;
            }
            else
            {
                separator = NativeDirectorySeparatorChar;
                GetFirstDirectorySeparator(path, ref separator);
            }

            StringList result = new StringList();
            string[] parts = MaybeSplit(path);

            if ((parts != null) && (parts.Length > 0))
            {
                for (int index = 0; index < parts.Length; index++)
                {
                    string part = parts[index];

                    if (part == null)
                        continue;

                    part = part.Trim(); // NOTE: Useful?  Correct?

                    if (part.Length == 0)
                    {
                        if (result.Count == 0)
                            result.Add(separator.ToString());

                        continue;
                    }

                    result.Add(part);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CombinePath(
            bool? unix, /* in */
            IList list  /* in */
            )
        {
            return CombinePath(
                unix, list, Index.Invalid, Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombinePath(
            bool? unix,     /* in */
            IList list,     /* in */
            int startIndex, /* in */
            int stopIndex   /* in */
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder();

            if (list != null)
            {
                int count = list.Count;

                if (ListOps.CheckStartAndStopIndex(
                        0, count - 1, ref startIndex, ref stopIndex))
                {
                    char separator;

                    if (unix != null)
                    {
                        separator = (bool)unix ?
                            AltDirectorySeparatorChar :
                            DirectorySeparatorChar;
                    }
                    else
                    {
                        separator = NativeDirectorySeparatorChar;
                        GetFirstDirectorySeparator(list, ref separator);
                    }

                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        string path = StringOps.GetStringFromObject(list[index]);

                        //
                        // NOTE: Skip all null/empty path parts.
                        //
                        if (String.IsNullOrEmpty(path))
                            continue;

                        //
                        // HACK: Remove surrounding whitespace.
                        //
                        string trimPath = path.Trim();

                        if (trimPath.Length > 0)
                        {
                            //
                            // NOTE: Have we already handled the first part of
                            //       the path?
                            //
                            if (builder.Length > 0)
                            {
                                if (!IsDirectoryChar(builder[builder.Length - 1]))
                                    builder.Append(separator);

                                builder.Append(MaybeTrim(trimPath));
                            }
                            else if ((trimPath.Length == 1) &&
                                IsDirectoryChar(trimPath[0]))
                            {
                                //
                                // BUGFIX: If the first part of the path is just
                                //         one separator character, append the
                                //         selected separator character instead.
                                //
                                builder.Append(separator);
                            }
                            else
                            {
                                string trimPath2 = TrimEndOfPath(trimPath, null);

                                if (trimPath2.Length > 0)
                                {
                                    //
                                    // BUGFIX: *MONO* Do not trim any separator
                                    //         characters from the start of the
                                    //         string.
                                    //
                                    builder.Append(trimPath2);
                                }
                                else
                                {
                                    //
                                    // BUGFIX: *MONO* If trimming the [first]
                                    //         non-empty part of the path ends
                                    //         removing all of its characters,
                                    //         append the selected separator
                                    //         character instead.
                                    //
                                    builder.Append(separator);
                                }
                            }
                        }
                    }
                }
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPairList GetPathList(
            IEnumerable<string> names /* in */
            )
        {
            StringPairList list = new StringPairList();

            if (names != null)
            {
                foreach (string name in names)
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    string path = CommonOps.Environment.GetVariable(name);

                    if (!String.IsNullOrEmpty(path))
                    {
                        string[] values = path.Split(Path.PathSeparator);

                        if (values == null)
                            continue;

                        foreach (string value in values)
                        {
                            if (String.IsNullOrEmpty(value))
                                continue;

                            list.Add(name, value);
                        }
                    }
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CombinePath(
            bool? unix,           /* in */
            params string[] paths /* in */
            )
        {
            return CombinePath(unix, (IList)paths);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombinePath(
            bool? unix,           /* in */
            int startIndex,       /* in */
            int stopIndex,        /* in */
            params string[] paths /* in */
            )
        {
            return CombinePath(
                unix, (IList)paths, startIndex, stopIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsDirectoryChar(
            char character /* in */
            )
        {
            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return false;

            return Array.IndexOf(
                characters, character) != Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static PathType GetPathType(
            string path /* in */
            )
        {
            return GetPathType(path, PathType.Relative);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static PathType GetPathType(
            string path,      /* in */
            PathType @default /* in */
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return @default;

            //
            // NOTE: Must check for volume relative first because
            //       Path.IsPathRooted thinks that paths starting
            //       with "/" and "\" are rooted.
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                if (IsDirectoryChar(path[0]))
                    return PathType.VolumeRelative;

                int offset = 0;

                if (IsDriveLetterAndColon(
                        path, length, false, ref offset))
                {
                    if ((offset < length) &&
                        IsDirectoryChar(path[offset]))
                    {
                        return PathType.Absolute;
                    }

                    return PathType.VolumeRelative;
                }
            }

            if (Path.IsPathRooted(path))
                return PathType.Absolute;
            else
                return PathType.Relative;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetHashCode(
            string path /* in */
            )
        {
            if (path != null)
            {
                string newPath = GetNativePath(path);

                if (NoCase)
                    newPath = newPath.ToLower();

                return newPath.GetHashCode();
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasDirectory(
            string path /* in */
            )
        {
            int index = Index.Invalid;

            return StartsWithDirectory(path, ref index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetExtension(
            string path /* in */
            )
        {
            try
            {
                return Path.GetExtension(path);
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasExtension(
            string path /* in */
            )
        {
            string extension;

            return HasExtension(path, out extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasExtension(
            string path,         /* in */
            out string extension /* out */
            )
        {
            extension = GetExtension(path);

            return !String.IsNullOrEmpty(extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasKnownExtension(
            string path /* in */
            )
        {
            string extension;

            if (!HasExtension(path, out extension))
                return false;

            if (extension == null)
                return false;

            PathDictionary<object> wellKnownList = FileExtension.WellKnownList;

            if (wellKnownList == null)
                return false;

            return wellKnownList.ContainsKey(extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool StartsWithDirectory(
            string path,  /* in */
            ref int index /* out */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return false;

            index = path.IndexOfAny(characters);
            return (index != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool EndsWithDirectory(
            string path,  /* in */
            ref int index /* out */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return false;

            index = path.LastIndexOfAny(characters);
            return (index != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddPathToDictionary(
            string path,                          /* in */
            ref PathDictionary<object> dictionary /* in, out */
            )
        {
            if (String.IsNullOrEmpty(path))
                return;

            if (dictionary == null)
                dictionary = new PathDictionary<object>();

            if (dictionary.ContainsKey(path))
                return;

            dictionary.Add(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddPathsToDictionary(
            IEnumerable<string> paths,            /* in */
            ref PathDictionary<object> dictionary /* in, out */
            )
        {
            if (paths == null)
                return;

            foreach (string path in paths)
                AddPathToDictionary(path, ref dictionary);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetMappedPaths(
            Interpreter interpreter,              /* in */
            string path,                          /* in */
            ref PathDictionary<object> dictionary /* in, out */
            )
        {
            //
            // NOTE: This method requires a valid interpreter context.
            //
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is either disposed or it has not [yet]
                //       fully completed the PreSetup() method, we cannot use it.
                //       In that case, just return null.
                //
                if (interpreter.Disposed || !interpreter.InternalPreSetup)
                    return false;

                //
                // NOTE: *SECURITY* Currently, all path mappings are always ignored
                //       for safe interpreters.
                //
                if (interpreter.InternalIsSafe())
                    return false;

                //
                // NOTE: Forbid any attempt to use a null or empty path string.
                //
                if (String.IsNullOrEmpty(path))
                    return false;

                try
                {
                    StringList list = new StringList();

                    foreach (string index in new string[] {
                            path, Path.GetDirectoryName(path),
                            Path.GetFileName(path)
                        })
                    {
                        if (index == null)
                            continue;

                        Result value = null;

                        if (interpreter.GetVariableValue2(
                                VariableFlags.GlobalOnly,
                                Vars.Core.Paths, index,
                                ref value) == ReturnCode.Ok)
                        {
                            if (String.IsNullOrEmpty(value))
                                continue;

                            list.Add(value);
                        }
                    }

                    if (list.Count > 0)
                        AddPathsToDictionary(list, ref dictionary);

                    return true;
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetAutoSourcePaths(
            Interpreter interpreter,              /* in */
            ref PathDictionary<object> dictionary /* in, out */
            )
        {
            //
            // NOTE: This method requires a valid interpreter context.
            //
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is either disposed or it has not [yet]
                //       fully completed the PreSetup() method, we cannot use it.
                //       In that case, just return null.
                //
                if (interpreter.Disposed || !interpreter.InternalPreSetup)
                    return false;

                //
                // NOTE: *SECURITY* Currently, the auto-source-path is always
                //       ignored for safe interpreters.
                //
                if (interpreter.InternalIsSafe())
                    return false;

                try
                {
                    Result value = null;

                    if (interpreter.GetVariableValue(
                            VariableFlags.GlobalOnly,
                            TclVars.Core.AutoSourcePath,
                            ref value) == ReturnCode.Ok)
                    {
                        StringList list = null;

                        if (!String.IsNullOrEmpty(value) &&
                            ParserOps<string>.SplitList(
                                interpreter, value, 0, Length.Invalid,
                                false, ref list) == ReturnCode.Ok)
                        {
                            if (list.Count > 0)
                                AddPathsToDictionary(list, ref dictionary);

                            return true;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractFileSearchFlags(
            FileSearchFlags fileSearchFlags, /* in */
            out bool specificPath,           /* out */
            out bool mapped,                 /* out */
            out bool autoSourcePath,         /* out */
            out bool current,                /* out */
            out bool user,                   /* out */
            out bool externals,              /* out */
            out bool application,            /* out */
            out bool applicationBase,        /* out */
            out bool vendor,                 /* out */
            out bool nullOnNotFound,         /* out */
            out bool directoryLocation,      /* out */
            out bool fileLocation,           /* out */
            out bool fullPath,               /* out */
            out bool stripBasePath,          /* out */
            out bool tailOnly,               /* out */
            out bool verbose,                /* out */
            out bool isolated,               /* out */
            out bool? unix                   /* out */
            )
        {
            specificPath = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.SpecificPath, true);

            mapped = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Mapped, true);

            autoSourcePath = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.AutoSourcePath, true);

            current = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Current, true);

            user = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.User, true);

            externals = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Externals, true);

            application = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Application, true);

            applicationBase = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.ApplicationBase, true);

            vendor = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Vendor, true);

            nullOnNotFound = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.NullOnNotFound, true);

            directoryLocation = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.DirectoryLocation, true);

            fileLocation = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.FileLocation, true);

            fullPath = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.FullPath, true);

            stripBasePath = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.StripBasePath, true);

            tailOnly = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.TailOnly, true);

            verbose = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Verbose, true);

            isolated = FlagOps.HasFlags(
                fileSearchFlags, FileSearchFlags.Isolated, true);

            if (FlagOps.HasFlags(
                    fileSearchFlags, FileSearchFlags.DirectorySeparator, true))
            {
                unix = FlagOps.HasFlags(
                    fileSearchFlags, FileSearchFlags.Unix, true);
            }
            else
            {
                unix = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSearchMode(
            bool isolated
            )
        {
            return String.Format(
                " in {0} mode", isolated ? "isolated" : "standard");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetSpecialFolder(
            Environment.SpecialFolder folder /* in */
            )
        {
            string directory = null;
            bool error = false;

            try
            {
                directory = Environment.GetFolderPath(folder); /* throw */
            }
            catch (Exception e)
            {
                error = true;

                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);
            }
            finally
            {
                bool success = !String.IsNullOrEmpty(directory);

                TraceOps.DebugTrace(String.Format(
                    "GetSpecialFolder: returning {0} directory {1} for folder {2}",
                    error ? "error" : success ? "good" : "bad", FormatOps.WrapOrNull(
                    directory), FormatOps.WrapOrNull(folder)), typeof(PathOps).Name,
                    success ? TracePriority.PathDebug : TracePriority.PathError);
            }

            return directory;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetOverrideOrSpecialFolder(
            Environment.SpecialFolder folder /* in */
            )
        {
            string directory = GlobalConfiguration.GetValue(
                String.Format("{0}_{1}", EnvVars.SpecialFolder,
                folder), ScalarConfigurationFlags);

            if (!String.IsNullOrEmpty(directory))
                return directory;

            return GetSpecialFolder(folder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddPathsToDictionary(
            Interpreter interpreter,              /* in */
            string path,                          /* in */
            bool specificPath,                    /* in */
            bool mapped,                          /* in */
            bool autoSourcePath,                  /* in */
            bool current,                         /* in */
            bool user,                            /* in */
            bool externals,                       /* in */
            bool application,                     /* in */
            bool applicationBase,                 /* in */
            ref PathDictionary<object> dictionary /* in, out */
            )
        {
            //
            // TODO: Should the IsPathRooted check always be done here?
            //       Maybe there should be a flag to disable it?
            //
            if (specificPath && Path.IsPathRooted(path))
                AddPathToDictionary(path, ref dictionary);

            if (mapped)
                /* IGNORED */
                GetMappedPaths(interpreter, path, ref dictionary);

            if (autoSourcePath)
                /* IGNORED */
                GetAutoSourcePaths(interpreter, ref dictionary);

            if (current)
            {
                AddPathToDictionary(Directory.GetCurrentDirectory(),
                    ref dictionary);
            }

            if (user)
            {
                AddPathsToDictionary(GetHomeDirectories(
                    HomeFlags.AnyDataMask), ref dictionary);

                AddPathToDictionary(GetDocumentDirectory(false),
                    ref dictionary);

                AddPathToDictionary(GetUserCloudDirectory(),
                    ref dictionary);

#if NET_40
                AddPathToDictionary(GetOverrideOrSpecialFolder(
                    Environment.SpecialFolder.UserProfile),
                    ref dictionary);

                AddPathToDictionary(GetDocumentDirectory(true),
                    ref dictionary);
#else
                AddPathToDictionary(GetUserProfileDirectory(),
                    ref dictionary);
#endif
            }

            if (externals)
            {
                AddPathToDictionary(GlobalState.GetExternalsPath(),
                    ref dictionary);
            }

            if (application)
            {
                AddPathsToDictionary(GetHomeDirectories(
                    HomeFlags.AnyConfigurationMask), ref dictionary);

                AddPathToDictionary(GetOverrideOrSpecialFolder(
                    Environment.SpecialFolder.LocalApplicationData),
                    ref dictionary);

                AddPathToDictionary(GetOverrideOrSpecialFolder(
                    Environment.SpecialFolder.ApplicationData),
                    ref dictionary);

                AddPathToDictionary(GetOverrideOrSpecialFolder(
                    Environment.SpecialFolder.CommonApplicationData),
                    ref dictionary);

                AddPathToDictionary(
                    GlobalState.InitializeOrGetBinaryPath(false),
                    ref dictionary);

                AddPathToDictionary(GlobalState.GetAssemblyPath(),
                    ref dictionary);
            }

            if (user || application)
            {
                AddPathToDictionary(GetUserCloudDirectory(),
                    ref dictionary);

                AddPathToDictionary(GetUserProfileDirectory(),
                    ref dictionary);
            }

            if (applicationBase)
            {
                AddPathToDictionary(AssemblyOps.GetAnchorPath(),
                    ref dictionary);

                AddPathToDictionary(GlobalState.GetAppDomainBaseDirectory(),
                    ref dictionary);

                AddPathToDictionary(GlobalState.GetBasePath(),
                    ref dictionary);

                AddPathToDictionary(GlobalState.GetRawBasePath(),
                    ref dictionary);
            }

            TraceOps.DebugTrace(String.Format(
                "AddPathsToDictionary: interpreter = {0}, path = {1}, " +
                "specificPath = {2}, mapped = {3}, autoSourcePath = {4}, " +
                "current = {5}, user = {6}, externals = {7}, " +
                "application = {8}, applicationBase = {9}, " +
                "dictionary = {10}", FormatOps.InterpreterNoThrow(
                interpreter), FormatOps.WrapOrNull(path), specificPath,
                mapped, autoSourcePath, current, user, externals,
                application, applicationBase, FormatOps.WrapOrNull(
                dictionary)), typeof(PathOps).Name, TracePriority.PathDebug);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeGetPathNoDrive(
            string path /* in */
            )
        {
            int length;
            int offset = 0;

            if (IsDriveLetterAndColon(
                    path, false, out length, ref offset) &&
                (offset < length))
            {
                return path.Substring(offset);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeRemoveBase(
            string path,     /* in: OPTIONAL */
            string basePath, /* in: OPTIONAL */
            string @default, /* in: OPTIONAL */
            bool separator   /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return @default;

            int length;

            if (StringOps.IsNullOrEmpty(basePath, out length))
                return @default;

            if (length > path.Length)
                return @default;

            if (!IsEqualFileName(path, basePath, length))
                return @default;

            return separator ?
                MaybeTrim(path.Substring(length), true) :
                path.Substring(length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetSearchFileNames(
            string path,          /* in */
            string basePath,      /* in */
            bool fullPath,        /* in */
            bool stripBasePath,   /* in */
            bool tailOnly,        /* in */
            out string fileName1, /* out: OPTIONAL, full path */
            out string fileName2  /* out: OPTIONAL, tail only */
            )
        {
            fileName1 = null;
            fileName2 = null;

            string pathNoDrive = MaybeGetPathNoDrive(path);
            string fileNameOnly = Path.GetFileName(path);
            PathType pathType = GetPathType(path);

            switch (pathType)
            {
                case PathType.Relative:
                    {
                        if (fullPath)
                            fileName1 = path;

                        break;
                    }
                case PathType.VolumeRelative:
                    {
                        if (fullPath)
                            fileName1 = pathNoDrive;

                        break;
                    }
                case PathType.Absolute:
                    {
                        if (fullPath && stripBasePath)
                        {
                            fileName1 = MaybeRemoveBase(
                                path, basePath, null, true);
                        }

                        break;
                    }
            }

            if (tailOnly)
                fileName2 = fileNameOnly;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CheckForFileName(
            bool? unix,
            string location,
            string vendorPath,
            string fileName,
            string mode,
            bool verbose,
            ref int count
            )
        {
            if (String.IsNullOrEmpty(location) ||
                String.IsNullOrEmpty(fileName))
            {
                return null;
            }

            string newFileName; /* REUSED */

            if (!String.IsNullOrEmpty(vendorPath))
            {
                newFileName = CombinePath(
                    unix, location, vendorPath, fileName);

                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "CheckForFileName: checking vendor file {0}{1}...",
                        FormatOps.WrapOrNull(newFileName), mode),
                        typeof(PathOps).Name,
                        TracePriority.PathDebug);
                }

                if (File.Exists(newFileName))
                {
                    if (verbose)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "CheckForFileName: found vendor file {0}{1}.",
                            FormatOps.WrapOrNull(newFileName), mode),
                            typeof(PathOps).Name,
                            TracePriority.PathDebug);
                    }

                    count++;
                    return GetNativePath(newFileName);
                }

                count++;
            }

            newFileName = CombinePath(unix, location, fileName);

            if (verbose)
            {
                TraceOps.DebugTrace(String.Format(
                    "CheckForFileName: checking normal file {0}{1}...",
                    FormatOps.WrapOrNull(newFileName), mode),
                    typeof(PathOps).Name,
                    TracePriority.PathDebug);
            }

            if (File.Exists(newFileName))
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "CheckForFileName: found normal file {0}{1}.",
                        FormatOps.WrapOrNull(newFileName), mode),
                        typeof(PathOps).Name,
                        TracePriority.PathDebug);
                }

                count++;
                return GetNativePath(newFileName);
            }

            count++;
            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Search(
            Interpreter interpreter,        /* in: OPTIONAL */
            string path,                    /* in */
            FileSearchFlags fileSearchFlags /* in */
            )
        {
            int count = 0;

            return Search(interpreter, path, fileSearchFlags, ref count);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Search(
            Interpreter interpreter,         /* in: optional interpreter context to use. */
            string path,                     /* in: [qualified?] file name to search for. */
            FileSearchFlags fileSearchFlags, /* in: flags that control search behavior. */
            ref int count                    /* in, out: how many names were checked? */
            )
        {
            bool specificPath;
            bool mapped;
            bool autoSourcePath;
            bool current;
            bool user;
            bool externals;
            bool application;
            bool applicationBase;
            bool vendor;
            bool nullOnNotFound;
            bool directoryLocation;
            bool fileLocation;
            bool fullPath;
            bool stripBasePath;
            bool tailOnly;
            bool verbose;
            bool isolated;
            bool? unix;

            ExtractFileSearchFlags(fileSearchFlags,
                out specificPath, out mapped, out autoSourcePath,
                out current, out user, out externals, out application,
                out applicationBase, out vendor, out nullOnNotFound,
                out directoryLocation, out fileLocation, out fullPath,
                out stripBasePath, out tailOnly, out verbose,
                out isolated, out unix);

            string mode = GetSearchMode(isolated);

            try
            {
                if (!String.IsNullOrEmpty(path))
                {
                    if (specificPath ||
                        mapped || autoSourcePath || current || user || application)
                    {
                        PathDictionary<object> dictionary = null;

                        AddPathsToDictionary(
                            interpreter, path, specificPath, mapped, autoSourcePath,
                            current, user, externals, application, applicationBase,
                            ref dictionary);

                        if (dictionary != null)
                        {
                            IEnumerable<KeyValuePair<string, object>> pairs =
                                dictionary.GetPairsInOrder(false);

                            if (pairs != null)
                            {
                                //
                                // NOTE: Grab the base path in advance as it is used
                                //       for each loop iteration.
                                //
                                string basePath = GlobalState.GetBasePath();

                                if ((basePath != null) &&
                                    (GetPathType(basePath) != PathType.Absolute))
                                {
                                    if (verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "Search: bad base path {0}, not absolute...",
                                            FormatOps.WrapOrNull(basePath)),
                                            typeof(PathOps).Name, TracePriority.PathDebug);
                                    }

                                    goto done;
                                }

                                //
                                // NOTE: Grab the vendor path in advance as it is used
                                //       for each loop iteration.
                                //
                                string vendorPath = vendor ? GetVendorPath() : null;

                                if ((vendorPath != null) &&
                                    (GetPathType(vendorPath) != PathType.Relative))
                                {
                                    if (verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "Search: bad vendor path {0}, not relative...",
                                            FormatOps.WrapOrNull(vendorPath)),
                                            typeof(PathOps).Name, TracePriority.PathDebug);
                                    }

                                    goto done;
                                }

                                foreach (KeyValuePair<string, object> pair in pairs)
                                {
                                    //
                                    // NOTE: Grab the location from the current pair.
                                    //
                                    string location = pair.Key;

                                    if (verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "Search: checking base location {0}{1}...",
                                            FormatOps.WrapOrNull(location), mode),
                                            typeof(PathOps).Name,
                                            TracePriority.PathDebug);
                                    }

                                    //
                                    // NOTE: Skip locations that are null or an empty
                                    //       string.
                                    //
                                    if (String.IsNullOrEmpty(location))
                                        continue;

                                    //
                                    // NOTE: If the location entry is actually a file,
                                    //       return it now if we are allowed to do so.
                                    //
                                    if (fileLocation)
                                    {
                                        if (File.Exists(location))
                                        {
                                            if (verbose)
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "Search: found file via location {0}{1}.",
                                                    FormatOps.WrapOrNull(location), mode),
                                                    typeof(PathOps).Name,
                                                    TracePriority.PathDebug);
                                            }

                                            count++;
                                            return GetNativePath(location);
                                        }

                                        count++;
                                    }

                                    //
                                    // NOTE: If the location entry is not allowed to
                                    //       be a directory -OR- the directory does
                                    //       not exist, skip this location entry.
                                    //
                                    if (!directoryLocation ||
                                        !Directory.Exists(location))
                                    {
                                        continue;
                                    }

                                    if (verbose)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "Search: found directory via location {0}{1}.",
                                            FormatOps.WrapOrNull(location), mode),
                                            typeof(PathOps).Name,
                                            TracePriority.PathDebug);
                                    }

                                    string fileName0; /* NOTE: Result file name. */
                                    string fileName1; /* NOTE: Full path name. */
                                    string fileName2; /* NOTE: Tail only name. */

                                    GetSearchFileNames(
                                        path, basePath, fullPath, stripBasePath,
                                        tailOnly, out fileName1, out fileName2);

                                    if (fileName1 != null)
                                    {
                                        fileName0 = CheckForFileName(
                                            unix, location, vendorPath, fileName1,
                                            mode, verbose, ref count);

                                        if (fileName0 != null)
                                            return fileName0;
                                    }

                                    if (fileName2 != null)
                                    {
                                        fileName0 = CheckForFileName(
                                            unix, location, vendorPath, fileName2,
                                            mode, verbose, ref count);

                                        if (fileName0 != null)
                                            return fileName0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

        done:

            //
            // NOTE: At this point, nothing was found.
            //
            if (nullOnNotFound)
            {
                //
                // NOTE: If we get here, we found nothing and that is
                //       considered an error (in strict mode).
                //
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Search: failed, returning null via input {0}{1}...",
                        FormatOps.WrapOrNull(path), mode),
                        typeof(PathOps).Name, TracePriority.PathDebug);
                }

                return null;
            }
            else
            {
                //
                // NOTE: Otherwise, just return whatever input value
                //       we received.
                //
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Search: failed, returning path via input {0}{1}...",
                        FormatOps.WrapOrNull(path), mode),
                        typeof(PathOps).Name, TracePriority.PathDebug);
                }

                return path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetUserDirectory( /* NOTE: Used by [cd] and TildeSubstitution() only. */
            bool strict /* in */
            )
        {
            string legacyDirectory = GetHomeDirectory(
                HomeFlags.Legacy);

            string[] directories = {
                legacyDirectory, GetUserProfileDirectory()
            };

            foreach (string directory in directories)
            {
                if (!String.IsNullOrEmpty(directory) &&
                    Directory.Exists(directory))
                {
                    return directory;
                }
            }

            //
            // NOTE: If we get here, we found nothing and that is
            //       considered an error.
            //
            return strict ? null : legacyDirectory;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetUserProfileDirectory()
        {
            return GlobalConfiguration.GetValue(
                EnvVars.UserProfile, ScalarConfigurationFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool OverrideCloudPath(
            Priority priority,
            string value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (defaultCloudPaths != null)
                {
                    int length = defaultCloudPaths.Length;

                    if (length == 0)
                        return false;

                    int index;

                    if (priority == Priority.Lowest)
                        index = 0;
                    else if (priority == Priority.None)
                        return true; // NOTE: Do nothing?  Ok.
                    else if (priority == Priority.Highest)
                        index = length - 1;
                    else
                        index = (int)priority;

                    if ((index < 0) || (index >= length))
                        return false;

                    defaultCloudPaths[index] = value;
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetUserCloudDirectory()
        {
            string directory = GetUserProfileDirectory();

            if (String.IsNullOrEmpty(directory) ||
                !Directory.Exists(directory))
            {
                return null;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (defaultCloudPaths == null)
                    return null;

                foreach (string path in defaultCloudPaths)
                {
                    if (path == null)
                        continue;

                    string subDirectory = CombinePath(
                        null, directory, path);

                    if (Directory.Exists(subDirectory))
                        return subDirectory;
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetVendorPath()
        {
            //
            // NOTE: Return the vendor path "offset" to be appended
            //       to search directories when looking for files.
            //
            return GlobalConfiguration.GetValue(EnvVars.VendorPath,
                ScalarConfigurationFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetVendorPath(
            string path
            )
        {
            //
            // NOTE: Set the vendor path "offset" to be appended
            //       to search directories when looking for files.
            //
            GlobalConfiguration.SetValue(EnvVars.VendorPath,
                path, ScalarConfigurationFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IAnyPair<HomeFlags, string> GetAnyHomeDirectoryPair(
            HomeFlags flags,   /* in */
            Priority priority, /* in */
            bool reverse       /* in */
            )
        {
            IList<IAnyPair<HomeFlags, string>> values =
                GetHomeDirectoryPairs(flags);

            if (values != null)
            {
                if (reverse)
                {
                    List<IAnyPair<HomeFlags, string>> list =
                        values as List<IAnyPair<HomeFlags, string>>;

                    if (list == null)
                        return null;

                    list.Reverse(); /* O(N) */
                }

                int count = values.Count;

                if (count > 0)
                {
                    int index = (int)priority;

                    if (index < 0)
                        index = 0;

                    if (index >= count)
                        index = count - 1;

                    return values[index];
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HaveHomeDirectory(
            HomeFlags flags /* in */
            )
        {
            return GetHomeDirectory(flags) != null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetDocumentDirectory(
            bool common /* in */
            )
        {
            //
            // BUGBUG: Why does the C# compiler see this variable
            //         as unassigned after the "if" block unless
            //         it is initialized to null first here?
            //
            string directory = null;

#if NET_40
            if (common)
            {
                directory = GetOverrideOrSpecialFolder(
                    Environment.SpecialFolder.CommonDocuments);
            }
            else
#else
            {
                directory = GetOverrideOrSpecialFolder(
                    Environment.SpecialFolder.MyDocuments);
            }
#endif

            if (!String.IsNullOrEmpty(directory))
                return directory;

            directory = GetHomeDirectory(HomeFlags.Data);

            if (!String.IsNullOrEmpty(directory))
                return directory;

            return GetHomeDirectory(HomeFlags.Legacy);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetHomeDirectory(
            HomeFlags flags /* in */
            )
        {
            flags &= ~HomeFlags.FlagsMask;

            switch (flags) /* HACK: Exact match. */
            {
                case HomeFlags.Legacy:
                    {
                        return GlobalConfiguration.GetValue(
                            EnvVars.Home,
                            ScalarConfigurationFlags);
                    }
                case HomeFlags.Data:
                    {
                        return GlobalConfiguration.GetValue(
                            EnvVars.XdgDataHome,
                            ScalarConfigurationFlags);
                    }
                case HomeFlags.Configuration:
                    {
                        return GlobalConfiguration.GetValue(
                            EnvVars.XdgConfigHome,
                            ScalarConfigurationFlags);
                    }
                case HomeFlags.Cloud:
                    {
                        return GlobalConfiguration.GetValue(
                            EnvVars.XdgCloudHome,
                            ScalarConfigurationFlags);
                    }
                case HomeFlags.Startup:
                    {
                        return GlobalConfiguration.GetValue(
                            EnvVars.XdgStartupHome,
                            ScalarConfigurationFlags);
                    }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetHomeDirectory(
            HomeFlags flags, /* in */
            string value     /* in */
            )
        {
            flags &= ~HomeFlags.FlagsMask;

            switch (flags) /* HACK: Exact match. */
            {
                case HomeFlags.Legacy:
                    {
                        GlobalConfiguration.SetValue(
                            EnvVars.Home, value,
                            ScalarConfigurationFlags);

                        break;
                    }
                case HomeFlags.Data:
                    {
                        GlobalConfiguration.SetValue(
                            EnvVars.XdgDataHome, value,
                            ScalarConfigurationFlags);

                        break;
                    }
                case HomeFlags.Configuration:
                    {
                        GlobalConfiguration.SetValue(
                            EnvVars.XdgConfigHome, value,
                            ScalarConfigurationFlags);

                        break;
                    }
                case HomeFlags.Cloud:
                    {
                        GlobalConfiguration.SetValue(
                            EnvVars.XdgCloudHome, value,
                            ScalarConfigurationFlags);

                        break;
                    }
                case HomeFlags.Startup:
                    {
                        GlobalConfiguration.SetValue(
                            EnvVars.XdgStartupHome, value,
                            ScalarConfigurationFlags);

                        break;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddHomeFlags(
            ref IList<HomeFlags> list, /* in, out */
            HomeFlags flags            /* in */
            )
        {
            if (flags != HomeFlags.None)
            {
                if (list == null)
                    list = new List<HomeFlags>();

                list.Add(flags);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddHomeDirectory(
            ref IList<IAnyPair<HomeFlags, string>> list, /* in, out */
            HomeFlags flags,                             /* in */
            string value,                                /* in */
            bool exists                                  /* in */
            )
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (!exists || Directory.Exists(value))
                {
                    if (list == null)
                        list = new List<IAnyPair<HomeFlags, string>>();

                    list.Add(new AnyPair<HomeFlags, string>(flags, value));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddHomeDirectories(
            ref IList<IAnyPair<HomeFlags, string>> list, /* in, out */
            HomeFlags flags,                             /* in */
            string value,                                /* in */
            bool exists                                  /* in */
            )
        {
            if (!String.IsNullOrEmpty(value))
            {
                string[] values = value.Split(Path.PathSeparator);

                if (values != null)
                {
                    foreach (string localValue in values)
                    {
                        MaybeAddHomeDirectory(
                            ref list, flags, localValue, exists);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IList<HomeFlags> MakeListOfHomeFlags(
            HomeFlags flags /* in */
            )
        {
            IList<HomeFlags> result = null;

            HomeFlags[] allHasFlags = {
                HomeFlags.Startup, HomeFlags.Cloud,
                HomeFlags.Configuration, HomeFlags.Data,
                HomeFlags.Legacy
            };

            foreach (HomeFlags hasFlags in allHasFlags)
                if (FlagOps.HasFlags(flags, hasFlags, true))
                    MaybeAddHomeFlags(ref result, hasFlags);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IList<string> GetHomeDirectories(
            HomeFlags flags /* in */
            )
        {
            IList<IAnyPair<HomeFlags, string>> values =
                GetHomeDirectoryPairs(flags);

            if (values == null)
                return null;

            IList<string> result = null;

            foreach (IAnyPair<HomeFlags, string> anyPair in values)
            {
                if (anyPair == null)
                    continue;

                if (result == null)
                    result = new StringList();

                result.Add(anyPair.Y);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IList<IAnyPair<HomeFlags, string>> GetHomeDirectoryPairs(
            HomeFlags flags /* in */
            )
        {
            IList<HomeFlags> allHasFlags = MakeListOfHomeFlags(flags);

            if (allHasFlags == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetHomeDirectoryPairs: invalid flags list for {0}",
                    FormatOps.WrapOrNull(flags)), typeof(PathOps).Name,
                    TracePriority.PathDebug);

                return null;
            }

            IList<IAnyPair<HomeFlags, string>> result = null;
            bool exists = FlagOps.HasFlags(flags, HomeFlags.Exists, true);
            bool[] done = { false, false, false, false };

            foreach (HomeFlags hasFlags in allHasFlags)
            {
                MaybeAddHomeDirectory(
                    ref result, hasFlags, GetHomeDirectory(hasFlags), exists);

                if (!done[0] && FlagOps.HasFlags(
                        hasFlags, HomeFlags.Startup, true))
                {
                    MaybeAddHomeDirectories(
                        ref result, hasFlags, GlobalConfiguration.GetValue(
                        EnvVars.XdgStartupDirs, ListConfigurationFlags),
                        exists);

                    done[0] = true;
                }

                if (!done[1] && FlagOps.HasFlags(
                        hasFlags, HomeFlags.Cloud, true))
                {
                    MaybeAddHomeDirectories(
                        ref result, hasFlags, GlobalConfiguration.GetValue(
                        EnvVars.XdgCloudDirs, ListConfigurationFlags),
                        exists);

                    done[1] = true;
                }

                if (!done[2] && FlagOps.HasFlags(
                        hasFlags, HomeFlags.Configuration, true))
                {
                    MaybeAddHomeDirectories(
                        ref result, hasFlags, GlobalConfiguration.GetValue(
                        EnvVars.XdgConfigDirs, ListConfigurationFlags),
                        exists);

                    done[2] = true;
                }

                if (!done[3] && FlagOps.HasFlags(
                        hasFlags, HomeFlags.Data, true))
                {
                    MaybeAddHomeDirectories(
                        ref result, hasFlags, GlobalConfiguration.GetValue(
                        EnvVars.XdgDataDirs, ListConfigurationFlags),
                        exists);

                    done[3] = true;
                }
            }

            TraceOps.DebugTrace(String.Format(
                "GetHomeDirectoryPairs: home directories list for {0} is: {1}",
                FormatOps.WrapOrNull(flags), FormatOps.HomeDirectoryPairs(result)),
                typeof(PathOps).Name, TracePriority.PathDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScrubPath(
            string basePath, /* in */
            string path      /* in */
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                //
                // WINDOWS: File names are not case-sensitive.
                //
                if (!String.IsNullOrEmpty(basePath))
                {
                    //
                    // BUGFIX: *WINDOWS* Make sure both paths have
                    //         the same separators.
                    //
                    string path1 = GetNativePath(path);
                    string path2 = GetNativePath(basePath);

                    //
                    // NOTE: See if the specified path starts with
                    //       the base path.
                    //
                    if (SharedStringOps.Equals(path1, path2, ComparisonType))
                    {
                        //
                        // NOTE: The specified path is exactly the
                        //       same as the base path; just return
                        //       the "base directory" token.
                        //
                        return Vars.Safe.BaseDirectory;
                    }
                    else
                    {
                        //
                        // NOTE: Get the native directory separator
                        //       character.
                        //
                        char separator = NativeDirectorySeparatorChar;

                        if (path1.StartsWith(
                                path2 + separator, ComparisonType))
                        {
                            //
                            // NOTE: Replace the base path with a
                            //       "base directory" token.
                            //
                            return Vars.Safe.BaseDirectory +
                                path1.Substring(path2.Length);
                        }
                    }
                }

                return Path.GetFileName(path);
            }

            return path;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsUri(
            string value /* in */
            )
        {
            return IsUri(value, UriKind.Absolute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsUri(
            string value,   /* in */
            UriKind uriKind /* in */
            )
        {
            if (!String.IsNullOrEmpty(value))
            {
                Uri uri = null;
                UriKind localUriKind = UriKind.RelativeOrAbsolute;

                if (TryCreateUri(value, ref uri, ref localUriKind))
                    return (uriKind == UriKind.RelativeOrAbsolute) || (localUriKind == uriKind);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombineStringsForUri(
            params string[] values /* in */
            )
        {
            if (values == null) // NOTE: Impossible?
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();

            foreach (string value in values)
            {
                if (value == null)
                    continue;

                builder.Append(value);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombinePathsForUri(
            bool normalize,       /* in */
            bool? both,           /* in */
            params string[] paths /* in */
            )
        {
            if (paths == null) // NOTE: Impossible?
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();

            foreach (string path in paths)
            {
                //
                // NOTE: Just skip any invalid portions of the path.
                //
                if (path == null)
                    continue;

                string localPath = MaybeTrim(path, both);

                if (!String.IsNullOrEmpty(localPath))
                {
                    //
                    // NOTE: URI path segments always use the Unix
                    //       directory separator (i.e. forward slash);
                    //       append one if necessary and optionally
                    //       replace any contained backslashes with
                    //       forward slash.
                    //
                    if (builder.Length > 0)
                        builder.Append(AltDirectorySeparatorChar);

                    if (normalize)
                    {
                        localPath = localPath.Replace(
                            DirectorySeparatorChar,
                            AltDirectorySeparatorChar);
                    }

                    builder.Append(localPath);
                }
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB
        private static NameValueCollection ParseQueryString(
            string query,     /* in */
            Encoding encoding /* in */
            )
        {
            if (query == null)
                return null;

            return (encoding != null) ?
                HttpUtility.ParseQueryString(query, encoding) :
                HttpUtility.ParseQueryString(query);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string UrlEncode(
            string value,     /* in */
            Encoding encoding /* in */
            )
        {
            if (value == null)
                return null;

            return (encoding != null) ?
                HttpUtility.UrlEncode(value, encoding) :
                HttpUtility.UrlEncode(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void QueryFromDictionary(
            StringDictionary dictionary, /* in */
            Encoding encoding,           /* in */
            ref StringBuilder builder    /* in, out */
            )
        {
            if (dictionary == null)
                return;

            if (builder == null)
                builder = StringOps.NewStringBuilder();

            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                if (builder.Length > 0)
                    builder.Append(Characters.Ampersand);

                builder.Append(UrlEncode(pair.Key, encoding));
                builder.Append(Characters.EqualSign);
                builder.Append(UrlEncode(pair.Value, encoding));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombineQueriesForUri(
            string query1,    /* in */
            string query2,    /* in */
            Encoding encoding /* in */
            )
        {
            NameValueCollection collection1 = ParseQueryString(
                query1, encoding);

            NameValueCollection collection2 = ParseQueryString(
                query2, encoding);

            if ((collection1 == null) && (collection2 == null))
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();
            NameValueCollection[] collections = { collection1, collection2 };

            foreach (NameValueCollection collection in collections)
            {
                if (collection == null)
                    continue;

                foreach (string key in collection.AllKeys)
                {
                    string[] values = collection.GetValues(key);

                    foreach (string value in values)
                    {
                        if (builder.Length > 0)
                            builder.Append(Characters.Ampersand);

                        builder.Append(UrlEncode(key, encoding));
                        builder.Append(Characters.EqualSign);
                        builder.Append(UrlEncode(value, encoding));
                    }
                }
            }

            return builder.ToString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Uri BuildAuxiliaryUri(
            ref string resourceName, /* in, out */
            ref Result error         /* out */
            )
        {
            if (resourceName == null)
            {
                error = "invalid original resource name";
                return null;
            }

            resourceName = RuntimeOps.MaybeAppendTextOrSuffix(
                resourceName);

            if (resourceName == null)
            {
                error = "invalid resolved resource name";
                return null;
            }

            if (identifierRegEx == null)
            {
                error = "cannot check resource name";
                return null;
            }

            Match match = identifierRegEx.Match(resourceName);

            if ((match == null) || !match.Success)
            {
                error = "malformed resource name";
                return null;
            }

            Uri baseUri = GlobalState.GetAssemblyAuxiliaryBaseUri();

            if (baseUri == null)
            {
                error = "invalid assembly auxiliary base uri";
                return null;
            }

            return TryCombineUris(baseUri,
                resourceName, null, UriComponents.AbsoluteUri,
                UriFormat.Unescaped, UriFlags.None, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Uri TryCombineUris(
            Uri baseUri,              /* in */
            string relativeUri,       /* in */
            Encoding encoding,        /* in */
            UriComponents components, /* in */
            UriFormat format,         /* in */
            UriFlags flags,           /* in */
            ref Result error          /* out */
            )
        {
            if (baseUri == null)
            {
                error = "invalid base uri";
                return null;
            }

            if (!baseUri.IsAbsoluteUri)
            {
                error = "uri is not absolute";
                return null;
            }

            //
            // NOTE: If no relative URI, just return the base URI as there
            //       is nothing else to combine it with.
            //
            if (String.IsNullOrEmpty(relativeUri))
                return baseUri;

            //
            // NOTE: Try to create an actual URI from the string of the
            //       relative URI.  If this fails, bail out now.
            //
            Uri localRelativeUri;

            if (!Uri.TryCreate(
                    DefaultBaseUri, relativeUri, out localRelativeUri))
            {
                error = String.Format(
                    "unable to create relative uri {0}",
                    FormatOps.WrapOrNull(relativeUri));

                return null;
            }

            //
            // NOTE: Use the URI format specified by the caller unless
            //       the right flag is not set.  In that case, use the
            //       default URI format.
            //
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!FlagOps.HasFlags(flags, UriFlags.UseFormat, true))
                    format = DefaultUriFormat;
            }

            //
            // NOTE: Grab components of the base URI that were requested
            //       by the caller, being careful to mask off those that
            //       are not applicable to the base portion of the URI.
            //
            string localBaseComponents = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (FlagOps.HasFlags(
                        components, BaseUriComponents, false))
                {
                    localBaseComponents = baseUri.GetComponents(
                        components & BaseUriComponents, format);
                }
            }

            //
            // NOTE: Attempt to combine the paths from both URIs.  This
            //       should result in a combined string, delimted by the
            //       appropriate path separator, without leading and/or
            //       trailing path separators.
            //
            string localPath = null;

            if (FlagOps.HasFlags(
                    components, UriComponents.Path, false))
            {
                //
                // NOTE: Should we treat all paths as bare strings and
                //       simply concatenate them together witout path
                //       separators?
                //
                if (FlagOps.HasFlags(
                        flags, UriFlags.RelativePath, true))
                {
                    localPath = localRelativeUri.GetComponents(
                        UriComponents.Path, format);
                }
                else if (FlagOps.HasFlags(
                        flags, UriFlags.BasePath, true))
                {
                    localPath = baseUri.GetComponents(
                        UriComponents.Path, format);
                }
                else if (FlagOps.HasFlags(
                        flags, UriFlags.NoSeparators, true))
                {
                    localPath = CombineStringsForUri(
                        baseUri.GetComponents(
                            UriComponents.Path, format),
                        localRelativeUri.GetComponents(
                            UriComponents.Path, format));
                }
                else
                {
                    bool? both = null;

                    if (FlagOps.HasFlags(
                            flags, UriFlags.OneSeparator, true))
                    {
                        both = false;
                    }
                    else if (FlagOps.HasFlags(
                            flags, UriFlags.BothSeparators, true))
                    {
                        both = true;
                    }

                    localPath = CombinePathsForUri(
                        FlagOps.HasFlags(
                            flags, UriFlags.Normalize, true),
                        both, baseUri.GetComponents(
                            UriComponents.Path, format),
                        localRelativeUri.GetComponents(
                            UriComponents.Path, format));
                }
            }

            //
            // NOTE: Attempt to combine all name/value pairs from both
            //       URIs.  This will only work when compiled with web
            //       support enabled (i.e. when we can make use of the
            //       System.Web assembly).
            //
            string localQuery = null;

#if WEB
            if (FlagOps.HasFlags(
                    components, UriComponents.Query, false))
            {
                localQuery = CombineQueriesForUri(
                    baseUri.GetComponents(
                        UriComponents.Query, format),
                    localRelativeUri.GetComponents(
                        UriComponents.Query, format),
                    encoding);
            }
#endif

            //
            // NOTE: We cannot combine fragments to help form the final
            //       URI; therefore, consider the one from the relative
            //       URI first, if any.  Failing that, consider the one
            //       from the base URI.  Reverse this preference if the
            //       caller passes the right flag.
            //
            string localFragment = null;

            if (FlagOps.HasFlags(
                    components, UriComponents.Fragment, false))
            {
                if (FlagOps.HasFlags(
                        flags, UriFlags.PreferBaseUri, false))
                {
                    localFragment = baseUri.GetComponents(
                        UriComponents.Fragment, format);

                    if (String.IsNullOrEmpty(localFragment))
                    {
                        localFragment = localRelativeUri.GetComponents(
                            UriComponents.Fragment, format);
                    }
                }
                else
                {
                    localFragment = localRelativeUri.GetComponents(
                        UriComponents.Fragment, format);

                    if (String.IsNullOrEmpty(localFragment))
                    {
                        localFragment = baseUri.GetComponents(
                            UriComponents.Fragment, format);
                    }
                }
            }

            //
            // NOTE: Start building the final URI string, starting with
            //       the main components of the absolute base URI (e.g.
            //       scheme, user-info, server, port, etc), if any.
            //
            StringBuilder builder = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(localBaseComponents))
                builder.Append(localBaseComponents);

            //
            // NOTE: If there is a path, append it to the final URI
            //       string now.  If any component was added before it,
            //       append the appropriate path separator first.
            //
            if (!String.IsNullOrEmpty(localPath))
            {
                //
                // BUGBUG: Is this compliant with the RFC for a URI
                //         that starts with a path (assuming such a
                //         URI is actually legal to begin with)?
                //
                if (builder.Length > 0)
                    builder.Append(AltDirectorySeparatorChar);

                builder.Append(localPath);
            }

            //
            // NOTE: If there is a query, append it to the final URI
            //       string now.  If any component was added before it,
            //       append the question mark first.
            //
            if (!String.IsNullOrEmpty(localQuery))
            {
                //
                // BUGBUG: Is this compliant with the RFC for a URI
                //         that starts with a query (assuming such a
                //         URI is actually legal to begin with)?
                //
                if (builder.Length > 0)
                    builder.Append(Characters.QuestionMark);

                builder.Append(localQuery);
            }

            //
            // NOTE: If there is a fragment, append it to the final URI
            //       string now.  If any component was added before it,
            //       append the number sign first.
            //
            if (!String.IsNullOrEmpty(localFragment))
            {
                //
                // BUGBUG: Is this compliant with the RFC for a URI
                //         that starts with a fragment (assuming such
                //         a URI is actually legal to begin with)?
                //
                if (builder.Length > 0)
                    builder.Append(Characters.NumberSign);

                builder.Append(localFragment);
            }

            //
            // NOTE: Grab the final (built) URI string now.  This will
            //       (potentially) be used for error reporting, should
            //       the actual URI creation fail.
            //
            string builderUri = builder.ToString();

            //
            // NOTE: Attempt to create the final URI object now, using
            //       the final built URI string.  If this fails, give
            //       an appropriate error message.
            //
            Uri uri;

            if (!Uri.TryCreate(builderUri, UriKind.Absolute, out uri))
            {
                error = String.Format(
                    "unable to create combined uri {0}",
                    FormatOps.WrapOrNull(builderUri));

                return null;
            }

            //
            // HACK: Finally, if the caller specified any "allow" bit
            //       in the flags, make sure the final URI conforms.
            //
            if (FlagOps.HasFlags(flags, UriFlags.AllowMask, false))
            {
                UriFlags haveFlags = flags | UriFlags.NoHost;

                if (!IsWebUri(uri, ref haveFlags, ref error))
                    return null;

                UriFlags wantFlags = AllowUriFlagsToWasUriFlags(flags);

                if (!FlagOps.HasFlags(haveFlags, wantFlags, false))
                {
                    error = String.Format(
                        "mismatched uri flags, have {0} want {1}",
                        FormatOps.WrapOrNull(haveFlags),
                        FormatOps.WrapOrNull(wantFlags));

                    return null;
                }
            }

            return uri;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryCreateUri(
            string value,       /* in */
            ref Uri uri,        /* out */
            ref UriKind uriKind /* out */
            )
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out uri))
            {
                uriKind = UriKind.Absolute;

                return true;
            }
            else if (Uri.TryCreate(value, UriKind.Relative, out uri))
            {
                uriKind = UriKind.Relative;

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static UriFlags AllowUriFlagsToWasUriFlags(
            UriFlags flags /* in */
            )
        {
            UriFlags result = UriFlags.None;

            if (FlagOps.HasFlags(flags, UriFlags.AllowFile, true))
                result |= UriFlags.WasFile;

            if (FlagOps.HasFlags(flags, UriFlags.AllowHttp, true))
                result |= UriFlags.WasHttp;

            if (FlagOps.HasFlags(flags, UriFlags.AllowHttps, true))
                result |= UriFlags.WasHttps;

            if (FlagOps.HasFlags(flags, UriFlags.AllowFtp, true))
                result |= UriFlags.WasFtp;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsWebUri(
            Uri uri,            /* in */
            ref UriFlags flags, /* in, out */
            ref Result error    /* out */
            )
        {
            string host = null;

            return IsWebUri(uri, ref flags, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsWebUri(
            Uri uri,            /* in */
            ref UriFlags flags, /* in, out */
            ref string host,    /* out */
            ref Result error    /* out */
            )
        {
            flags &= ~UriFlags.WasMask;

            if (uri == null)
            {
                error = "invalid uri";
                return false;
            }

            if (!uri.IsAbsoluteUri)
            {
                error = String.Format(
                    "uri {0} is not absolute",
                    FormatOps.WrapOrNull(uri));

                return false;
            }

            string scheme = uri.Scheme;

            if (String.IsNullOrEmpty(scheme))
            {
                error = String.Format(
                    "invalid scheme for uri {0}",
                    FormatOps.WrapOrNull(uri));

                return false;
            }

            bool allowHttps = FlagOps.HasFlags(
                flags, UriFlags.AllowHttps, true);

            bool allowHttp = FlagOps.HasFlags(
                flags, UriFlags.AllowHttp, true);

            bool allowFtp = FlagOps.HasFlags(
                flags, UriFlags.AllowFtp, true);

            bool allowFile = FlagOps.HasFlags(
                flags, UriFlags.AllowFile, true);

            bool wasHttps = false;
            bool wasHttp = false;
            bool wasFtp = false;
            bool wasFile = false;

            if ((allowHttps && (wasHttps = IsHttpsUriScheme(scheme))) ||
                (allowHttp && (wasHttp = IsHttpUriScheme(scheme))) ||
                (allowFtp && (wasFtp = IsFtpUriScheme(scheme))) ||
                (allowFile && (wasFile = IsFileUriScheme(scheme))))
            {
                bool noHost = FlagOps.HasFlags(
                    flags, UriFlags.NoHost, true);

                if (wasHttps)
                    flags |= UriFlags.WasHttps;

                if (wasHttp)
                    flags |= UriFlags.WasHttp;

                if (wasFtp)
                    flags |= UriFlags.WasFtp;

                if (wasFile)
                    flags |= UriFlags.WasFile;

                if (noHost)
                {
                    return true;
                }
                else
                {
                    try
                    {
                        host = uri.DnsSafeHost; /* throw */
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = String.Format(
                            "failed to get host for uri {0}: {1}",
                            FormatOps.WrapOrNull(uri), e);
                    }

                    return false;
                }
            }

            error = String.Format(
                "unsupported uri scheme {0}",
                FormatOps.WrapOrNull(scheme));

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsHttpsUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeHttps);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsHttpUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeHttp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsFtpUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeFtp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsFileUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeFile);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsHttpsUriScheme(
            Uri uri /* in */
            )
        {
            return (uri != null) && IsHttpsUriScheme(uri.Scheme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsFileUriScheme(
            Uri uri /* in */
            )
        {
            return (uri != null) && IsFileUriScheme(uri.Scheme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value /* in */
            )
        {
            Uri uri = null;

            return IsRemoteUri(value, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value, /* in */
            ref Uri uri   /* out */
            )
        {
            uri = null;

            if (!String.IsNullOrEmpty(value))
            {
                //
                // WARNING: *SECURITY* The "UriKind" value here must be
                //          "Absolute", please do not change it.
                //
                if (Uri.TryCreate(value, UriKind.Absolute, out uri))
                    return !IsFileUriScheme(uri);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetPackageRelativeFileName(
            string fileName, /* in */
            bool keepLib,    /* in */
            bool verbatim,   /* in */
            ref Result error /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return null;
            }

            try
            {
                if (GetPathType(fileName) != PathType.Absolute)
                {
                    error = "file name not absolute";
                    return null;
                }

                string[] paths = {
                    GlobalState.GetAssemblyPackageRootPath(),
                    GlobalState.GetPackagePeerBinaryPath()
                };

                foreach (string path in paths)
                {
                    if (String.IsNullOrEmpty(path))
                        continue;

                    if (!SharedStringOps.StartsWith(
                            fileName, path, ComparisonType))
                    {
                        continue;
                    }

                    string relativeFileName = MaybeTrimStart(
                        fileName.Substring(path.Length));

                    string[] parts; /* REUSED */
                    string part; /* REUSED */
                    int length; /* REUSED */

                    if (keepLib)
                    {
                        parts = MaybeSplit(path);

                        if (parts != null)
                        {
                            length = parts.Length;

                            if (length > 0)
                            {
                                part = parts[length - 1];

                                if (CompareParts(
                                        part, TclVars.Path.Lib) == 0)
                                {
                                    relativeFileName = Path.Combine(
                                        part, relativeFileName);
                                }
                            }
                        }
                    }

                    if (!verbatim)
                    {
                        parts = MaybeSplit(relativeFileName);

                        if (parts != null)
                        {
                            length = parts.Length;

                            if (length > 1)
                            {
                                part = parts[length - 2];

                                if (StartsWithPart(
                                        part, _Path.NetPrefix))
                                {
                                    if (length > 2)
                                    {
                                        relativeFileName = Path.Combine(
                                            CombinePath(null, 0, length - 3,
                                            parts), parts[length - 1]);
                                    }
                                    else
                                    {
                                        relativeFileName = parts[length - 1];
                                    }
                                }
                            }
                        }
                    }

                    return relativeFileName;
                }

                error = "file name not relative to package paths";
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetPluginRelativeFileName(
            IPlugin plugin,         /* in */
            IClientData clientData, /* in: NOT USED */
            string fileName         /* in */
            )
        {
            try
            {
                if (plugin == null)
                    return null;

                if (String.IsNullOrEmpty(fileName) ||
                    Path.IsPathRooted(fileName))
                {
                    return null;
                }

                string pluginFileName = plugin.FileName;

                if (String.IsNullOrEmpty(pluginFileName))
                    return null;

                string directory = Path.GetDirectoryName(
                    pluginFileName);

                if (String.IsNullOrEmpty(directory))
                    return null;

                return CombinePath(null, directory, fileName);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScriptFileNameOnly(
            string path /* in */
            )
        {
            if (!HasDirectory(path))
                return path;

            return Path.GetFileName(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetDirectoryName(
            string path /* in */
            )
        {
            string result = path;

            try
            {
                if (!String.IsNullOrEmpty(result))
                {
                    if (IsRemoteUri(result))
                    {
                        //
                        // HACK: This is a horrible hack.
                        //
                        result = GetUnixPath(Path.GetDirectoryName(result));
                    }
                    else
                    {
                        result = Path.GetDirectoryName(result);
                    }
                }

                return result;
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetCurrentDirectory(
            string path /* in: OPTIONAL */
            )
        {
            if (!String.IsNullOrEmpty(path) &&
                StringOps.CharIsAsciiAlpha(path[0]) &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                //
                // HACK: This will return the current directory
                //       for the specified drive letter.
                //
                return Path.GetFullPath(String.Format(
                    "{0}{1}", path[0], Characters.Colon));
            }

            return Directory.GetCurrentDirectory();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SplitPathRaw(
            string path,          /* in */
            string separator,     /* in */
            bool allowDrive,      /* in */
            bool allowCurrent,    /* in */
            out string prefix,    /* out */
            out string directory, /* out */
            out string fileName   /* out */
            )
        {
            prefix = null;
            directory = null;
            fileName = null;

            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return;

            char[] characters;

            if (!TryGetDirectoryChars(out characters))
            {
                fileName = path;
                return;
            }

            int index = path.LastIndexOfAny(characters);

            if (index == Index.Invalid)
            {
                int offset = 0;

                if (allowDrive && IsDriveLetterAndColon(
                        path, length, false, ref offset))
                {
                    string drive = path.Substring(0, offset);

                    if (allowCurrent)
                    {
                        string newDirectory =
                            GetCurrentDirectory(drive);

                        if (separator != null)
                        {
                            newDirectory = NormalizeSeparators(
                                newDirectory, separator);
                        }

                        //
                        // NOTE: Directory must have same drive
                        //       prefix as the path being split.
                        //
                        if (SharedStringOps.Equals(newDirectory,
                                0, path, 0, DrivePrefixLength,
                                ComparisonType))
                        {
                            prefix = path.Substring(0, offset);
                            directory = newDirectory;
                            fileName = path.Substring(offset);
                            return;
                        }
                    }

                    prefix = drive;
                    fileName = path.Substring(offset);
                    return;
                }
                else
                {
                    fileName = path;
                    return;
                }
            }

            directory = path.Substring(0, index);
            length = directory.Length;

            if (length == 0)
            {
                if (separator != null)
                    directory += separator;
                else
                    directory += characters[index];

                directory = Path.GetFullPath(directory); /* throw */

                if (separator != null)
                {
                    directory = NormalizeSeparators(
                        directory, separator);
                }
            }
            else if (allowDrive && IsDriveLetterAndColon(
                    directory, length, true))
            {
                if (separator != null)
                    directory += separator;
                else
                    directory += characters[index];

                if (separator != null)
                {
                    directory = NormalizeSeparators(
                        directory, separator);
                }
            }

            fileName = path.Substring(index + 1);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetFirstDirectorySeparator(
            string path,       /* in */
            ref char separator /* out */
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                int[] indexes = {
                    path.IndexOf(DirectorySeparatorChar),
                    path.IndexOf(AltDirectorySeparatorChar)
                };

                int minimumIndex = Index.Invalid;

                foreach (int index in indexes)
                {
                    if (index == Index.Invalid)
                        continue;

                    if ((minimumIndex == Index.Invalid) ||
                        (index < minimumIndex))
                    {
                        minimumIndex = index;
                    }
                }

                if (minimumIndex != Index.Invalid)
                {
                    separator = path[minimumIndex];
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static char GetFirstDirectorySeparator(
            string path /* in */
            )
        {
            char separator = NativeDirectorySeparatorChar;

            /* IGNORED */
            GetFirstDirectorySeparator(path, ref separator);

            return separator;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetFirstDirectorySeparator(
            IList list,        /* in */
            ref char separator /* out */
            )
        {
            if (list != null)
            {
                for (int index = 0; index < list.Count; index++)
                {
                    string path = StringOps.GetStringFromObject(list[index]);

                    if (!String.IsNullOrEmpty(path) &&
                        GetFirstDirectorySeparator(path, ref separator))
                    {
                        break;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MakeRelativePath(
            string path,   /* in */
            bool separator /* in: Also remove leading separator? */
            )
        {
            if (String.IsNullOrEmpty(path)) /* Garbage in, garbage out. */
                return path;

            if (path.Length <= 2) /* Do NOT return empty string. */
                return path;

            if (!StringOps.CharIsAsciiAlpha(path[0])) /* Unix? */
                return path;

            if (path[1] != Characters.Colon) /* Unix? */
                return path;

            string newPath = path.Substring(2);

            if (!separator)
                return newPath;

            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return newPath;

            return newPath.TrimStart(characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AppendSeparator(
            string path /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            char[] characters;

            if (!TryGetDirectoryChars(out characters))
                return path;

            foreach (char character in characters)
            {
                int index = path.IndexOf(character);

                if (index != Index.Invalid)
                    return path + character;
            }

            return path + characters[0];
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsUnderPath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path1,            /* in */
            string path2             /* in */
            )
        {
            if (!IsUnderPathSimple(
                    interpreter, ref path1, ref path2))
            {
                return false;
            }

#if NATIVE && (WINDOWS || UNIX)
            bool noNative;

            lock (syncRoot)
            {
                noNative = NoNativeIsSameFile;
            }

            if (!noNative &&
                (PlatformOps.IsWindowsOperatingSystem() ||
                PlatformOps.IsLinuxOperatingSystem()))
            {
                while (true)
                {
                    if (IsSameFile(interpreter, path1, path2))
                        return true;

                    string newPath1 = Path.GetDirectoryName(path1);

                    if (IsSameFile(interpreter, newPath1, path1))
                        return false;

                    if (IsRootPath(newPath1))
                        return false;

                    path1 = newPath1;
                }
            }
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetDirectories( /* RECURSIVE */
            string path,          /* in */
            string searchPattern, /* in */
            ref StringList paths, /* in, out */
            ref ResultList errors /* in, out */
            )
        {
            try
            {
                IEnumerable<string> localPaths; /* REUSED */

                localPaths = Directory.GetDirectories(
                    GetNativePath(path), searchPattern,
                    SearchOption.TopDirectoryOnly);

                if (localPaths != null)
                {
                    foreach (string localPath in localPaths)
                    {
                        if (String.IsNullOrEmpty(localPath))
                            continue;

                        if (paths == null)
                            paths = new StringList();

                        paths.Add(localPath);

                        GetDirectories(
                            localPath, searchPattern, ref paths,
                            ref errors); /* RECURSIVE */
                    }
                }
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetFiles( /* RECURSIVE */
            string path,          /* in */
            string searchPattern, /* in */
            ref StringList paths, /* in, out */
            ref ResultList errors /* in, out */
            )
        {
            try
            {
                IEnumerable<string> localPaths; /* REUSED */

                localPaths = Directory.GetFiles(
                    GetNativePath(path), searchPattern,
                    SearchOption.TopDirectoryOnly);

                if (localPaths != null)
                {
                    foreach (string localPath in localPaths)
                    {
                        if (String.IsNullOrEmpty(localPath))
                            continue;

                        if (paths == null)
                            paths = new StringList();

                        paths.Add(localPath);
                    }
                }

                localPaths = Directory.GetDirectories(
                    GetNativePath(path), searchPattern,
                    SearchOption.TopDirectoryOnly);

                if (localPaths != null)
                {
                    foreach (string localPath in localPaths)
                    {
                        if (String.IsNullOrEmpty(localPath))
                            continue;

                        GetFiles(
                            localPath, searchPattern, ref paths,
                            ref errors); /* RECURSIVE */
                    }
                }
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddUnderPathToList(
            ref UnderDictionary paths, /* in, out */
            string path,               /* in */
            PathType pathType,         /* in */
            string fullPath            /* in: OPTIONAL */
            )
        {
            if (String.IsNullOrEmpty(path))
                return;

            if (paths == null)
                paths = new UnderDictionary();

            UnderAnyPair match = new UnderAnyPair(
                pathType, fullPath);

            List<UnderAnyPair> matches;

            if (paths.TryGetValue(path, out matches) &&
                (matches != null))
            {
                matches.Add(match);
            }
            else
            {
                matches = new List<UnderAnyPair>();
                matches.Add(match);

                paths[path] = matches;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddUnderPathToList(
            ref UnderDictionary paths, /* in, out */
            string path1,              /* in */
            string path2,              /* in */
            PathType pathType,         /* in */
            bool anyLevel              /* in */
            )
        {
            if (String.IsNullOrEmpty(path2))
                return;

            path2 = GetUnixPath(path2);

            if (String.IsNullOrEmpty(path2))
                return;

            if (path1 != null)
            {
                path1 = GetUnixPath(path1);

                if (path1 != null)
                {
                    if (SharedStringOps.StartsWith(
                            path2, path1, ComparisonType))
                    {
                        path2 = path2.Substring(path1.Length + 1);
                    }
                }
            }

            if (anyLevel)
            {
                string[] parts = MaybeSplit(path2);

                if (parts != null)
                {
                    int length = parts.Length;

                    for (int index = length - 1; index >= 0; index--)
                    {
                        string[] subParts = ArrayOps.Copy<string>(
                            parts, index);

                        if (subParts == null)
                            continue;

                        MaybeAddUnderPathToList(
                            ref paths, CombinePath(null, subParts),
                            pathType, path2);
                    }

                    return;
                }
            }

            MaybeAddUnderPathToList(
                ref paths, path2, pathType, path2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDirectoriesAndFiles(
            Interpreter interpreter,   /* in: OPTIONAL */
            string path,               /* in */
            string searchPattern,      /* in: OPTIONAL */
            SearchOption searchOption, /* in */
            PathType pathType,         /* in */
            ref UnderDictionary paths, /* in, out */
            ref ResultList errors      /* in, out */
            )
        {
            if (String.IsNullOrEmpty(path))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid primary path");
                return ReturnCode.Error;
            }

            IEnumerable<string> localPaths; /* REUSED */
            StringList localList; /* REUSED */

            bool anyLevel = FlagOps.HasFlags(
                pathType, PathType.AnyLevel, true);

            if (searchPattern == null)
                searchPattern = Characters.Asterisk.ToString();

            if (FlagOps.HasFlags(
                    pathType, PathType.Directory, true))
            {
                if (FlagOps.HasFlags(
                        pathType, PathType.Robust, true))
                {
                    localList = null;

                    GetDirectories(
                        path, searchPattern, ref localList,
                        ref errors);

                    localPaths = (localList != null) ?
                        localList.ToArray() : null;
                }
                else
                {
                    try
                    {
                        localPaths = Directory.GetDirectories(
                            GetNativePath(path), searchPattern,
                            searchOption);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        localPaths = null;
                    }
                }

                if (localPaths != null)
                {
                    foreach (string localPath in localPaths)
                    {
                        MaybeAddUnderPathToList(
                            ref paths, path, GetUnixPath(
                            localPath), PathType.Directory,
                            anyLevel);
                    }
                }
            }

            if (FlagOps.HasFlags(
                    pathType, PathType.File, true))
            {
                if (FlagOps.HasFlags(
                        pathType, PathType.Robust, true))
                {
                    localList = null;

                    GetFiles(
                        path, searchPattern, ref localList,
                        ref errors);

                    localPaths = (localList != null) ?
                        localList.ToArray() : null;
                }
                else
                {
                    try
                    {
                        localPaths = Directory.GetFiles(
                            GetNativePath(path), searchPattern,
                            searchOption);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        localPaths = null;
                    }
                }

                if (localPaths != null)
                {
                    foreach (string localPath in localPaths)
                    {
                        MaybeAddUnderPathToList(
                            ref paths, path, GetUnixPath(
                            localPath), PathType.File,
                            anyLevel);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode IsNameUnderPath(
            Interpreter interpreter,        /* in */
            string path1,                   /* in */
            string path2,                   /* in */
            MatchMode mode,                 /* in */
            SearchOption searchOption,      /* in */
            PathType pathType,              /* in */
            ref List<UnderAnyPair> matches, /* in, out */
            ref ResultList errors           /* in, out */
            )
        {
            if ((mode == MatchMode.None) && (GetPathType(
                    path2, PathType.None) != PathType.Relative))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("second path must be relative");
                return ReturnCode.Error;
            }

            UnderDictionary paths = null;

            if (GetDirectoriesAndFiles(interpreter,
                    path1, null, searchOption, pathType,
                    ref paths, ref errors) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (paths != null)
            {
                if (mode == MatchMode.None)
                {
                    if (path2 != null)
                    {
                        List<UnderAnyPair> localMatches;

                        if (paths.TryGetValue(
                                path2, out localMatches) &&
                            (localMatches != null))
                        {
                            if (matches == null)
                                matches = new List<UnderAnyPair>();

                            matches.AddRange(localMatches);
                        }
                    }
                }
                else
                {
                    foreach (UnderPair pair in paths)
                    {
                        List<UnderAnyPair> localMatches = pair.Value;

                        if (localMatches == null)
                            continue;

                        if ((path2 != null) && !StringOps.Match(
                                interpreter, mode, pair.Key, path2,
                                NoCase))
                        {
                            continue;
                        }

                        if (matches == null)
                            matches = new List<UnderAnyPair>();

                        matches.AddRange(localMatches);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsUnderPathSimple(
            Interpreter interpreter, /* in: OPTIONAL */
            ref string path1,        /* in, out */
            ref string path2         /* in, out */
            )
        {
            string newPath1;

            if (!String.IsNullOrEmpty(path1))
                newPath1 = ResolveFullPath(interpreter, path1);
            else
                newPath1 = GetNativePath(path1);

            string newPath2;

            if (!String.IsNullOrEmpty(path2))
                newPath2 = ResolveFullPath(interpreter, path2);
            else
                newPath2 = GetNativePath(path2);

#if MONO || MONO_HACKS
            //
            // HACK: *MONO* This method crashes on Mono 3.2.3 for Windows with the following
            //       stack trace:
            //
            //       System.TypeInitializationException: An exception was thrown by the type
            //           initializer for Eagle._Components.Public.InterpreterHelper --->
            //           System.TypeInitializationException: An exception was thrown by the
            //           type initializer for Eagle._Components.Private.GlobalState --->
            //           System.NullReferenceException: Object reference not set to an
            //           instance of an object
            //         at System.String.Compare (System.String strA, Int32 indexA,
            //           System.String strB, Int32 indexB, Int32 length, Boolean ignoreCase,
            //           System.Globalization.CultureInfo culture)
            //         at System.String.Compare (System.String strA, Int32 indexA,
            //           System.String strB, Int32 indexB, Int32 length, StringComparison
            //           comparisonType)
            //         at Eagle._Components.Private.PathOps.IsUnderPath
            //           (Eagle._Components.Public.Interpreter interpreter, System.String
            //           path1, System.String path2)
            //         at Eagle._Components.Private.AssemblyOps.GetPath
            //           (Eagle._Components.Public.Interpreter interpreter,
            //           System.Reflection.Assembly assembly)
            //         at Eagle._Components.Private.GlobalState..cctor ()
            //         --- End of inner exception stack trace ---
            //         at Eagle._Components.Public.InterpreterHelper..cctor ()
            //         --- End of inner exception stack trace ---
            //         at (wrapper managed-to-native)
            //           System.Reflection.MonoCMethod:InternalInvoke
            //           (System.Reflection.MonoCMethod,object,object[],System.Exception&)
            //         at System.Reflection.MonoCMethod.InternalInvoke (System.Object obj,
            //           System.Object[] parameters)
            //
            //       The above exception seems to be caused by an error in their code for
            //       the String.Compare method when a non-default application domain is
            //       used.
            //
            if ((newPath1 == null) || (newPath2 == null))
                return false;

            if (SharedStringOps.Equals(
                    newPath1, 0, newPath2, 0, newPath2.Length,
                    ComparisonType))
            {
                path1 = newPath1;
                path2 = newPath2;

                return true;
            }
            else
            {
                return false;
            }
#else
            if (SharedStringOps.Equals(
                    newPath1, 0, newPath2, 0, (newPath2 != null) ?
                    newPath2.Length : 0, ComparisonType))
            {
                path1 = newPath1;
                path2 = newPath2;

                return true;
            }
            else
            {
                return false;
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool StartsWithPart(
            string part1, /* in */
            string part2  /* in */
            )
        {
            if (String.IsNullOrEmpty(part1))
                return false;

            int length;

            if (StringOps.IsNullOrEmpty(part2, out length))
                return false;

            return SharedStringOps.Equals(
                part1, 0, part2, 0, length, ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CompareParts(
            string part1, /* in */
            string part2  /* in */
            )
        {
            return SharedStringOps.Compare(part1, part2, ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int CompareFileNames(
            string path1, /* in */
            string path2  /* in */
            )
        {
            return SharedStringOps.Compare(
                GetNativePath(path1), GetNativePath(path2), ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEqualFileName(
            string path1, /* in */
            string path2  /* in */
            )
        {
            return SharedStringOps.Equals(
                GetNativePath(path1), GetNativePath(path2), ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEqualFileName(
            string path1, /* in */
            string path2, /* in */
            int length    /* in */
            )
        {
            return SharedStringOps.Equals(
                GetNativePath(path1), 0, GetNativePath(path2), 0, length,
                ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool StartsWithFileName(
            string path1, /* in */
            string path2  /* in */
            )
        {
            if (String.IsNullOrEmpty(path1))
                return false;

            int length;

            if (StringOps.IsNullOrEmpty(path2, out length))
                return false;

            return SharedStringOps.Equals(
                GetNativePath(path1), 0, GetNativePath(path2),
                0, length, ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        private static ulong MaybeHashForSerialNumber(
            string path
            )
        {
            if (String.IsNullOrEmpty(path))
                return 0;

            Encoding encoding = StringOps.GetEncoding(
                EncodingType.FileSystem);

            if (encoding == null)
                return 0;

            return MathOps.HashFnv1ULong(
                encoding.GetBytes(path), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ulong CombineForSerialNumber(
            ulong value,
            params ulong[] values
            )
        {
            ulong result = 0;

            if (values != null)
            {
                int length = values.Length;

                if (length > 0)
                {
                    ByteList list = new ByteList(length * sizeof(ulong));

                    if (value != 0)
                        list.AddRange(BitConverter.GetBytes(value));

                    for (int index = 0; index < length; index++)
                        list.AddRange(BitConverter.GetBytes(values[index]));

                    result = MathOps.HashFnv1ULong(list.ToArray(), true);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CalculateSerialNumber(
            string path,
            PathFlags flags,
            params ulong[] values
            )
        {
            if (FlagOps.HasFlags(
                    flags, PathFlags.RawSerialNumber, true))
            {
                return StringList.MakeList(values);
            }
            else if (FlagOps.HasFlags(
                    flags, PathFlags.StableSerialNumber, true))
            {
                return FormatOps.Hexadecimal(CombineForSerialNumber(
                    MaybeHashForSerialNumber(path), values), true);
            }
            else
            {
                return FormatOps.Hexadecimal(CombineForSerialNumber(
                    0, values), true);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static bool WindowsTryGetSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            try
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, Directory.Exists(path), false,
                        ref fileInformation, ref error) == ReturnCode.Ok)
                {
                    UlongList list = new UlongList();

                    list.Add(fileInformation.dwVolumeSerialNumber);

                    if (FlagOps.HasFlags(
                            flags, PathFlags.StableSerialNumber, true))
                    {
                        list.Add(fileInformation.nFileSizeHigh);
                        list.Add(fileInformation.nFileSizeLow);
                    }
                    else
                    {
                        list.Add(fileInformation.nFileIndexHigh);
                        list.Add(fileInformation.nFileIndexLow);
                    }

                    serialNumber = CalculateSerialNumber(
                        path, flags, list.ToArray());

                    return true;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
        private static bool LinuxTryGetSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            try
            {
                UnsafeNativeMethods.stat buf;

                if (UnsafeNativeMethods.libc_xstat(0, path, out buf) == 0)
                {
                    //
                    // TODO: Possibly revisit this algorithm in the future.
                    //
                    UlongList list = new UlongList();

                    list.Add(buf.st_dev);

                    if (FlagOps.HasFlags(
                            flags, PathFlags.StableSerialNumber, true))
                    {
                        list.Add(buf.st_size);
                    }
                    else
                    {
                        list.Add(buf.st_ino);
                    }

                    serialNumber = CalculateSerialNumber(
                        path, flags, list.ToArray());

                    return true;
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
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        public static bool TryGetSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                return WindowsTryGetSerialNumber(
                    path, flags, ref serialNumber, ref error);
            }
#endif

#if UNIX
            if (PlatformOps.IsLinuxOperatingSystem())
            {
                return LinuxTryGetSerialNumber(
                    path, flags, ref serialNumber, ref error);
            }
#endif

            error = "not supported on this operating system";
            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static ReturnCode WindowsIsSameFile(
            Interpreter interpreter, /* in: NOT USED */
            string path1,            /* in */
            string path2,            /* in */
            ref bool match,          /* out */
            ref Result error         /* out */
            )
        {
            try
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation1;
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation2;

                InitializeFileInformation(out fileInformation1);
                InitializeFileInformation(out fileInformation2);

                if ((GetPathInformation(
                        path1, Directory.Exists(path1), false,
                        ref fileInformation1, ref error) == ReturnCode.Ok) &&
                    (GetPathInformation(
                        path2, Directory.Exists(path2), false,
                        ref fileInformation2, ref error) == ReturnCode.Ok))
                {
                    if ((fileInformation1.dwVolumeSerialNumber ==
                            fileInformation2.dwVolumeSerialNumber) &&
                        (fileInformation1.nFileIndexHigh ==
                            fileInformation2.nFileIndexHigh) &&
                        (fileInformation1.nFileIndexLow ==
                            fileInformation2.nFileIndexLow))
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
        private static ReturnCode LinuxIsSameFile(
            Interpreter interpreter, /* in: NOT USED */
            string path1,            /* in */
            string path2,            /* in */
            ref bool match,          /* out */
            ref Result error         /* out */
            )
        {
            try
            {
                UnsafeNativeMethods.stat buf1;
                UnsafeNativeMethods.stat buf2;

                if ((UnsafeNativeMethods.libc_xstat(
                        0, path1, out buf1) == 0) &&
                    (UnsafeNativeMethods.libc_xstat(
                        0, path2, out buf2) == 0))
                {
                    if ((buf1.st_dev == buf2.st_dev) &&
                        (buf1.st_ino == buf2.st_ino))
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
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

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GenericIsSameFile(
            Interpreter interpreter, /* in: OPTIONAL */
            string path1,            /* in */
            string path2,            /* in */
            ref bool match,          /* out */
            ref Result error         /* out */
            )
        {
            string newPath1;

            if (!String.IsNullOrEmpty(path1))
                newPath1 = ResolveFullPath(interpreter, path1);
            else
                newPath1 = path1;

            string newPath2;

            if (!String.IsNullOrEmpty(path2))
                newPath2 = ResolveFullPath(interpreter, path2);
            else
                newPath2 = path2;

            //
            // NOTE: If both normalized path strings are the same
            //       (or they are both null or empty string) then
            //       we match.
            //
            match = IsEqualFileName(newPath1, newPath2);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            Interpreter interpreter, /* in: OPTIONAL */
            string path1,            /* in */
            string path2             /* in */
            )
        {
#if NATIVE && (WINDOWS || UNIX)
            bool noNative;

            lock (syncRoot)
            {
                noNative = NoNativeIsSameFile;
            }
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            bool match; /* REUSED */
            Result error; /* REUSED */

            ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
            if (!noNative &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                match = false;
                error = null;

                if (WindowsIsSameFile(
                        interpreter, path1, path2, ref match,
                        ref error) == ReturnCode.Ok)
                {
                    return match;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsSameFile: Windows error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(PathOps).Name,
                        TracePriority.PathError);
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
            if (!noNative &&
                PlatformOps.IsLinuxOperatingSystem())
            {
                match = false;
                error = null;

                if (LinuxIsSameFile(
                        interpreter, path1, path2, ref match,
                        ref error) == ReturnCode.Ok)
                {
                    return match;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsSameFile: Linux error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(PathOps).Name,
                        TracePriority.PathError);
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            match = false;
            error = null;

            if (GenericIsSameFile(
                    interpreter, path1, path2, ref match,
                    ref error) == ReturnCode.Ok)
            {
                return match;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "IsSameFile: generic error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(PathOps).Name,
                    TracePriority.PathError);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // HACK: In the event of a failure, assume the file
            //       names do not represent the same file.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsJustTilde(
            string path /* in */
            )
        {
            int length;

            if (StringOps.IsNullOrEmpty(path, out length))
                return false;

            if (length == 1)
            {
                return (path[0] == Characters.Tilde);
            }
            else
            {
                string trimPath = TrimEndOfPath(path, null);

                return ((trimPath != null) && (trimPath.Length == 1)
                    && (trimPath[0] == Characters.Tilde));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool MatchSuffix(
            string path1, /* in */
            string path2  /* in */
            )
        {
            if ((path1 == null) || (path2 == null))
                return false;

            if (path1.EndsWith(path2, ComparisonType))
                return true;

            string nativePath1 = GetNativePath(path1);
            string nativePath2 = GetNativePath(path2);

            if ((nativePath1 != null) && (nativePath2 != null) &&
                nativePath1.EndsWith(nativePath2, ComparisonType))
            {
                return true;
            }

            string nonNativePath1 = GetNonNativePath(path1);
            string nonNativePath2 = GetNonNativePath(path2);

            if ((nonNativePath1 != null) && (nonNativePath2 != null) &&
                nonNativePath1.EndsWith(nonNativePath2, ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool PathExists(
            string path /* in */
            )
        {
            //
            // NOTE: Does the specified path exist as either an
            //       existing directory or a file?
            //
            return Directory.Exists(path) || File.Exists(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TranslatePath(
            string path,                        /* in */
            PathTranslationType translationType /* in */
            )
        {
            switch (translationType)
            {
                case PathTranslationType.Unix:
                    return GetUnixPath(path);
                case PathTranslationType.Windows:
                    return GetWindowsPath(path);
                case PathTranslationType.Native:
                    return GetNativePath(path);
                case PathTranslationType.NonNative:
                    return GetNonNativePath(path);
                default:
                    return path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetNativePath(
            string path /* in */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return GetWindowsPath(path);
            else
                return GetUnixPath(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetNonNativePath(
            string path /* in */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return GetUnixPath(path);
            else
                return GetWindowsPath(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetWindowsPath(
            string path /* in */
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                result = result.Replace(
                    AltDirectorySeparatorChar,
                    DirectorySeparatorChar);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetUnixPath(
            string path /* in */
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                result = result.Replace(
                    DirectorySeparatorChar,
                    AltDirectorySeparatorChar);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NormalizeSeparators(
            string path,     /* in */
            string separator /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            if (separator == null)
            {
                return path.Replace(
                    NonNativeDirectorySeparatorChar,
                    NativeDirectorySeparatorChar);
            }

            StringBuilder builder = StringOps.NewStringBuilder(
                path.Length);

            foreach (char character in path)
            {
                if (IsDirectoryChar(character))
                    builder.Append(separator);
                else
                    builder.Append(character);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string RobustNormalizePath(
            Interpreter interpreter, /* in */
            string path              /* in */
            )
        {
            bool isWindows = PlatformOps.IsWindowsOperatingSystem();

            if (!CheckForValid(
                    null, path, false, false, true, isWindows))
            {
                return path; /* NOTE: Garbage in, garbage out. */
            }

            bool? unix = null;

            if (isWindows)
                unix = true;

            string newPath = null;
            Result error = null; /* NOT USED */

            if (NormalizePath(
                    interpreter, null, path, unix, true,
                    false, null, false, false, ref newPath,
                    ref error) != ReturnCode.Ok)
            {
                return path; /* NOTE: Garbage in, garbage out. */
            }

            return newPath;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ValidatePathAsFile(
            string path,  /* in */
            bool? rooted, /* in */
            bool? exists  /* in */
            )
        {
            if (!CheckForValid(
                    null, path, false, false, true,
                    PlatformOps.IsWindowsOperatingSystem()))
            {
                return false;
            }

            if ((rooted != null) &&
                ((bool)rooted != Path.IsPathRooted(path)))
            {
                return false;
            }

            if (exists != null)
            {
                if ((bool)exists)
                {
                    if (!File.Exists(path))
                        return false;
                }
                else
                {
                    if (File.Exists(path) ||
                        Directory.Exists(path))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ValidatePathAsDirectory(
            string path,  /* in */
            bool? rooted, /* in */
            bool? exists  /* in */
            )
        {
            if (!CheckForValid(
                    null, path, false, false, true,
                    PlatformOps.IsWindowsOperatingSystem()))
            {
                return false;
            }

            if ((rooted != null) &&
                ((bool)rooted != Path.IsPathRooted(path)))
            {
                return false;
            }

            if (exists != null)
            {
                if ((bool)exists)
                {
                    if (!Directory.Exists(path))
                        return false;
                }
                else
                {
                    if (Directory.Exists(path) ||
                        File.Exists(path))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckForTilde(
            string path,    /* in */
            out int length  /* out */
            )
        {
            if (path != null)
            {
                //
                // NOTE: Grab the length, once, now, because the
                //       caller will also need it.
                //
                length = path.Length;

                switch (length)
                {
                    case 0:
                        {
                            //
                            // NOTE: Empty string, do nothing.
                            //
                            break;
                        }
                    default:
                        {
                            if (path[0] == Characters.Tilde)
                            {
                                //
                                // NOTE: Leading tilde, remove
                                //       it.
                                //
                                return true;
                            }
                            else
                            {
                                //
                                // NOTE: No tilde, do nothing.
                                //
                                break;
                            }
                        }
                }
            }
            else
            {
                //
                // NOTE: Null string, do nothing.
                //
                length = Length.Invalid;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Unix-ism: convert leading tilde in the path to the home
        //       directory of the current user -OR- the directory that
        //       actually contains the specified file name (i.e. if the
        //       noSearch flag is false).
        //
        public static string TildeSubstitution(
            Interpreter interpreter, /* in: OPTIONAL */
            string path,             /* in */
            bool noSearch,           /* in */
            bool strict              /* in */
            )
        {
            //
            // NOTE: First, see if the specified path string has one (or
            //       more) leading tilde character(s).  If not, we have
            //       nothing to do.
            //
            int length;

            if (CheckForTilde(path, out length))
            {
                //
                // NOTE: At this point, we know there was at least one
                //       tilde at the front of the specified path.  Now
                //       figure out if there are more characters after
                //       it.  There are seven (general) cases here:
                //
                //       0. Empty string.  The length for this case is
                //          always zero.
                //
                //       1. Tilde only, e.g. "~".  The length for this
                //          case is always one.
                //
                //       2. Tilde and separator only, e.g. "~/" -OR-
                //          "~\".  The length for this case is always
                //          two.
                //
                //       3. Tilde and non-separator only, e.g. "~a".
                //          The length for this case is always two.
                //
                //       4. Tilde followed by non-separator(s), etc,
                //          e.g. "~a".  The length for this case is
                //          always three -OR- greater.
                //
                //       5. Tilde followed by a separator and a file
                //          name, e.g. "~/f.ext".  The length for
                //          this case is always three -OR- greater.
                //
                //       6. Tilde followed by a separator, directory
                //          name, and a file name, e.g. "~/d/f.ext".
                //          The length for this case is always three
                //          -OR- greater.
                //
                if (length == 0)
                {
                    //
                    // NOTE: This should not happen.  Do nothing and
                    //       return the original path.  This is case
                    //       #0 (above).
                    //
                    return path;
                }
                else if (length == 1)
                {
                    //
                    // NOTE: Simple case, tilde only, return just the
                    //       (home) directory for the user.  This is
                    //       case #1 (above).
                    //
                    return GetUserDirectory(false);
                }
                else if (length == 2)
                {
                    //
                    // NOTE: Is the character immediately following
                    //       the leading tilde a directory separator?
                    //
                    if (IsDirectoryChar(path[1]))
                    {
                        //
                        // NOTE: This is case #2 (above).  Return the
                        //       (home) directory for the user.
                        //
                        return GetUserDirectory(false);
                    }
                    else if (strict)
                    {
                        //
                        // NOTE: This is case #3 (above).  Not allowed
                        //       in strict mode.
                        //
                        return null;
                    }
                    else
                    {
                        //
                        // NOTE: This is case #3 (above).  Return the
                        //       originally specified path verbatim as
                        //       this is unsupported.
                        //
                        return path;
                    }
                }
                else /* (length >= 3) */
                {
                    //
                    // NOTE: Is the character immediately following
                    //       the leading tilde a directory separator?
                    //
                    if (IsDirectoryChar(path[1]))
                    {
                        //
                        // BUGFIX: If there is any directory after the
                        //         initial tilde-slash combo and before
                        //         the file name, do not search for the
                        //         file name.  This is case #6 (above).
                        //         We can also get (back) to this point
                        //         if the search fails.
                        //
                    fallback:

                        if (noSearch || HasDirectory(path.Substring(2)))
                        {
                            //
                            // NOTE: Effectively replace the first two
                            //       characters of the original path
                            //       with the fully qualified path for
                            //       the home directory of the current
                            //       user.  This is case #5 (above)
                            //       -OR- the noSearch flag is set.
                            //
                            return CombinePath(null, GetUserDirectory(
                                false), path.Substring(1));
                        }
                        else
                        {
                            //
                            // HACK: Attempt to search for the file in
                            //       standard user/application profile
                            //       locations; failing that, return
                            //       the file name as though it existed
                            //       in the user directory.
                            //
                            string fileName = Search(interpreter, path,
                                FileSearchFlags.StandardNullOnNotFound);

                            if (fileName != null)
                                return fileName;

                            noSearch = true;
                            goto fallback;
                        }
                    }
                    else if (strict)
                    {
                        //
                        // NOTE: This is case #4 (above).  Not allowed
                        //       in strict mode.
                        //
                        return null;
                    }
                    else
                    {
                        //
                        // NOTE: This is case #4 (above).  Return the
                        //       originally specified path verbatim as
                        //       this is unsupported.
                        //
                        return path;
                    }
                }
            }
            else
            {
                //
                // NOTE: There was no leading tilde, return originally
                //       specified path verbatim.
                //
                return path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string BaseDirectorySubstitution(
            Interpreter interpreter, /* in: NOT USED */
            string path              /* in */
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result) && result.StartsWith(
                    Vars.Safe.BaseDirectory, ComparisonType))
            {
                string basePath = GlobalState.GetBasePath();

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = basePath + result.Substring(
                        Vars.Safe.BaseDirectory.Length);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string EnvironmentSubstitution(
            Interpreter interpreter, /* in: NOT USED */
            string path              /* in */
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
                result = CommonOps.Environment.ExpandVariables(result);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SubstituteOrResolvePath(
            Interpreter interpreter,  /* in: OPTIONAL */
            string path,              /* in */
            bool resolve,             /* in */
            ref bool remoteUri        /* out */
            )
        {
            //
            // NOTE: Start with their original path value.
            //
            string result = path;

            //
            // NOTE: Did they pass null or an empty string?
            //
            bool isNullOrEmpty = String.IsNullOrEmpty(result);

            //
            // NOTE: Is this file name a remote URI?
            //
            remoteUri = !isNullOrEmpty ? IsRemoteUri(result) : false;

            //
            // NOTE: If they passed null or an empty string, there is no need
            //       to do anything else.
            //
            if (!isNullOrEmpty)
            {
                //
                // NOTE: Replace the base directory "token" with the actual
                //       base directory.
                //
                result = BaseDirectorySubstitution(interpreter, result);

                //
                // NOTE: Always perform environment substitution (even on
                //       remote URIs).
                //
                result = EnvironmentSubstitution(interpreter, result);

                //
                // NOTE: Only perform leading tilde substitution if the file
                //       name is local.
                //
                if (!remoteUri)
                {
                    //
                    // NOTE: Either resolve the path (skipping the environment
                    //       variables since they have already been done) -OR-
                    //       just perform tilde substitution on it.
                    //
                    result = resolve ?
                        ResolvePathNoEnvironment(interpreter, result) :
                        TildeSubstitution(interpreter, result, false, false);

                    //
                    // NOTE: When we are not fully resolving the file name, for
                    //       file names that do not represent a remote URI, we
                    //       normalize all directory separators in the result
                    //       to the native one for this operating system.
                    //
                    if (!resolve && !String.IsNullOrEmpty(result))
                    {
                        result = result.Replace(
                            NonNativeDirectorySeparatorChar,
                            NativeDirectorySeparatorChar);
                    }
                }
            }

            if (EnableTraceForNormalize(null))
            {
                TraceOps.DebugTrace(String.Format(
                    "SubstituteOrResolvePath: interpreter = {0}, " +
                    "path = {1}, resolve = {2}, remoteUri = {3}, " +
                    "result = {4}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(path), resolve, remoteUri,
                    FormatOps.WrapOrNull(result)),
                    typeof(PathOps).Name, TracePriority.PathDebug);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ResolvePathNoEnvironment(
            Interpreter interpreter, /* in: OPTIONAL */
            string path              /* in */
            )
        {
            return NormalizePath(
                interpreter, null, path, null, false, true, null, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ResolvePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path              /* in */
            )
        {
            return NormalizePath(
                interpreter, null, path, null, true, true, null, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ResolveFullPath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path              /* in */
            )
        {
            return NormalizePath(
                interpreter, null, path, null, true, true, true, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeToLib(
            string path,           /* in */
            bool skipLibraryToLib, /* in */
            bool skipTestsToLib,   /* in */
            bool relative          /* in */
            )
        {
            string newPath; /* REUSED */
            bool done; /* REUSED */

            if (!skipLibraryToLib)
            {
                newPath = LibraryToLib(path, relative, out done);

                if (done)
                    return newPath;
            }

            if (!skipTestsToLib)
            {
                newPath = TestsToLib(path, relative, out done);

                if (done)
                    return newPath;
            }

            return path;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TestsToLib(
            string path,   /* in */
            bool relative, /* in */
            out bool done  /* out */
            )
        {
            //
            // NOTE: This method is useless on null and empty strings, just
            //       return the input path verbatim.
            //
            if (String.IsNullOrEmpty(path))
            {
                done = false;
                return path;
            }

            //
            // NOTE: If there are no directory separator characters, then do
            //       nothing.
            //
            char[] characters;

            if (!TryGetDirectoryChars(out characters))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Break the path into parts, based on the known directory
            //       separator characters.
            //
            StringList parts = new StringList(path.Split(characters));

            //
            // NOTE: How many parts are there?
            //
            int count = parts.Count;

            //
            // NOTE: The minimum number of parts must be at least 2, to form
            //       "Tests/<fileName>".
            //
            if (count < 2)
            {
                done = false;
                return path;
            }

            //
            // NOTE: The final part, which is typically the file name, cannot
            //       be null or empty.
            //
            int offset = 1;

            if (String.IsNullOrEmpty(parts[count - offset]))
            {
                done = false;
                return path;
            }

            //
            // NOTE: The next part before that must be exactly "Tests".  On
            //       some systems, the case does not matter (e.g. Windows).
            //
            offset++;

            if (!SharedStringOps.Equals(
                    parts[count - offset], _Path.Tests, ComparisonType))
            {
                done = false;
                return path;
            }

            //
            // NOTE: If there is already a "lib" just prior to "Tests", skip
            //       doing anything.
            //
            int nextOffset = offset + 1;

            if (((count - nextOffset) >= 0) && SharedStringOps.Equals(
                    parts[count - nextOffset], TclVars.Path.Lib,
                    ComparisonType))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Insert "lib" just prior to "Tests".
            //
            parts.Insert(count - offset, TclVars.Path.Lib);

            //
            // NOTE: Since we just inserted an element, update the cached
            //       count.  Also, increment the offset because we are now
            //       interested in at least the final 3 elements.
            //
            count = parts.Count; offset++;

            //
            // NOTE: If we get to this point, this method is performing a
            //       real transformation on the provided path; therefore,
            //       set the output parameter accordingly.
            //
            done = true;

            //
            // NOTE: Are they wanting just the relative [matched] portion
            //       returned?
            //
            if (relative)
            {
                //
                // NOTE: *SPECIAL* Return only the final X parts, joined
                //       into one path, with the "lib" replacement made.
                //
                return String.Join(
                    AltDirectorySeparatorChar.ToString(),
                    parts.ToArray(), count - offset, offset);
            }
            else
            {
                //
                // NOTE: Return all the parts, joined into one path, with
                //       the "lib" replacement made.
                //
                char separator = NativeDirectorySeparatorChar;
                GetFirstDirectorySeparator(path, ref separator);

                return String.Join(separator.ToString(), parts.ToArray());
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: If the last 3 parts of the path are "Library/Tests/<fileName>" -OR- the last 4
        //       parts of the path are "Library/Tests/<dirName>/<fileName>" with a "<dirName>"
        //       value of "data" or "tcl", (case-insensitive), then replace the "Library" part
        //       with "lib" and return the resulting path.  Also, if the "relative" parameter
        //       is non-zero, return only the final X parts of the path, separated by forward
        //       slashes (Unix-style), where X will be either 3 or 4 (i.e. X will only be 4 if
        //       a supported "<dirName>" part exists), .  The returned path may not actually
        //       exist on the file system -AND- that is perfectly OK.
        //
        private static string LibraryToLib(
            string path,   /* in */
            bool relative, /* in */
            out bool done  /* out */
            )
        {
            //
            // NOTE: This method is useless on null and empty strings, just
            //       return the input path verbatim.
            //
            if (String.IsNullOrEmpty(path))
            {
                done = false;
                return path;
            }

            //
            // NOTE: If there are no directory separator characters, then do
            //       nothing.
            //
            char[] characters;

            if (!TryGetDirectoryChars(out characters))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Break the path into parts, based on the known directory
            //       separator characters.
            //
            string[] parts = path.Split(characters);

            //
            // NOTE: How many parts are there?
            //
            int length = parts.Length;

            //
            // NOTE: The minimum number of parts must be at least 3, to form
            //       "Library/Tests/<fileName>".  Instead, there could be 4,
            //       where they may form "Library/Tests/<dirName>/<fileName>",
            //       where the "<dirName>" may be "data" or "tcl".  However,
            //       the absolute minimum number of parts here is still 3.
            //
            if (length < 3)
            {
                done = false;
                return path;
            }

            //
            // NOTE: The final part, which is typically the file name, cannot
            //       be null or empty.
            //
            int offset = 1;

            if (String.IsNullOrEmpty(parts[length - offset]))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Is there a "<dirName>" part equal to "data" or "tcl"?  If
            //       so, skip over it when considering if the remaining parts
            //       fit the supported pattern of "Library/Tests".
            //
            offset++;

            if ((length > 3) && (SharedStringOps.Equals(
                    parts[length - offset], _Path.Data, ComparisonType) ||
                SharedStringOps.Equals(
                    parts[length - offset], _Path.Tcl, ComparisonType)))
            {
                //
                // NOTE: At this point, we know there are at least 4 parts
                //       -AND- that the final part is "data" or "tcl".  So,
                //       skip to the previous part, which should be "Tests".
                //
                offset++;
            }

            //
            // NOTE: The next two parts before that must be exactly "Library"
            //       and "Tests".  On some systems, the case does not matter
            //       (e.g. Windows).
            //
            int nextOffset = offset + 1;

            if (!SharedStringOps.Equals(
                    parts[length - offset], _Path.Tests, ComparisonType) ||
                !SharedStringOps.Equals(
                    parts[length - nextOffset], _Path.Library, ComparisonType))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Change the "Library" part into "lib".
            //
            parts[length - nextOffset] = TclVars.Path.Lib;

            //
            // NOTE: If we get to this point, this method is performing a
            //       real transformation on the provided path; therefore,
            //       set the output parameter accordingly.
            //
            done = true;

            //
            // NOTE: Are they wanting just the relative [matched] portion
            //       returned?
            //
            if (relative)
            {
                //
                // NOTE: *SPECIAL* Return only the final X parts, joined
                //       into one path, with the "lib" replacement made.
                //
                return String.Join(
                    AltDirectorySeparatorChar.ToString(), parts,
                    length - nextOffset, nextOffset);
            }
            else
            {
                //
                // NOTE: Return all the parts, joined into one path, with
                //       the "lib" replacement made.
                //
                char separator = NativeDirectorySeparatorChar;
                GetFirstDirectorySeparator(path, ref separator);

                return String.Join(separator.ToString(), parts);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool EnableTraceForNormalize(
            bool? enable /* in */
            )
        {
            if (enable != null)
            {
                if ((bool)enable)
                    Interlocked.Increment(ref traceForNormalize);
                else
                    Interlocked.Decrement(ref traceForNormalize);
            }

            return Interlocked.CompareExchange(ref traceForNormalize, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is designed to allow the tail portion of
        //       the specified path to contain illegal characters, e.g.
        //       the "?" and "*" characters, for use in glob patterns.
        //
        private static string GetFullPath(
            string path, /* in */
            bool? unix   /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            int index = Index.Invalid;

            if (!EndsWithDirectory(path, ref index))
                return path;

            string directory = path.Substring(0, index);
            string tailOnly = path.Substring(index + 1);

            return CombinePath(unix,
                Path.GetFullPath(directory), tailOnly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NormalizePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string rootPath,         /* in */
            string path,             /* in */
            bool? unix,              /* in */
            bool environment,        /* in */
            bool tilde,              /* in */
            bool? full,              /* in */
            bool legacyResolve,      /* in */
            bool noCase              /* in */
            )
        {
            string newPath = null;
            Result error = null;

            if (NormalizePath(
                    interpreter, rootPath, path, unix, environment,
                    tilde, full, legacyResolve, noCase, ref newPath,
                    ref error) == ReturnCode.Ok)
            {
                return newPath;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode NormalizePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string rootPath,         /* in */
            string path,             /* in */
            bool? unix,              /* in */
            bool environment,        /* in */
            bool tilde,              /* in */
            bool? full,              /* in */
            bool legacyResolve,      /* in */
            bool noCase,             /* in */
            ref string newPath,      /* out */
            ref Result error         /* out */
            )
        {
            ReturnCode code;

            try
            {
                //
                // FIXME: I do not like this function.  It is too complex and it
                //        tries to deal with too many corner cases.  Also, some
                //        places where this function is called should probably
                //        be doing something simpler instead.
                //
                newPath = path;

                if (!String.IsNullOrEmpty(newPath))
                {
                    //
                    // NOTE: Perform environment substitution on the path?
                    //
                    if (environment)
                    {
                        newPath = EnvironmentSubstitution(
                            interpreter, newPath);
                    }

                    //
                    // NOTE: Normalize implies clean.
                    //
                    newPath = CleanPath(newPath, false, null);

                    //
                    // NOTE: Collapse any extra trailing directory separator
                    //       characters into exactly one.
                    //
                    newPath = TrimEndOfPath(
                        newPath, NativeDirectorySeparatorChar);

                    //
                    // NOTE: Perform leading tilde substitution?
                    //
                    if (tilde)
                    {
                        newPath = TildeSubstitution(
                            interpreter, newPath, false, false);
                    }

                    //
                    // NOTE: Only resolve the full path if it will make sense
                    //       (we do not always want to to resolve relative to
                    //       the current directory).
                    //
                    if (!String.IsNullOrEmpty(newPath))
                    {
                        if (Path.IsPathRooted(newPath))
                        {
                            if ((full == null) || (bool)full)
                            {
                                newPath = legacyResolve ?
                                    Path.GetFullPath(newPath) /* throw */ :
                                    GetFullPath(newPath, unix);
                            }
                        }
                        else if ((full != null) && (bool)full)
                        {
                            //
                            // NOTE: In this case, fully resolve an entire
                            //       path, relative to the specified root
                            //       path -OR- the current directory when
                            //       there is no root path specified.
                            //
                            if (rootPath == null)
                                rootPath = Directory.GetCurrentDirectory();

                            if (!String.IsNullOrEmpty(newPath))
                            {
                                newPath = CombinePath(
                                    unix, rootPath, newPath);

                                newPath = legacyResolve ?
                                    Path.GetFullPath(newPath) /* throw */ :
                                    GetFullPath(newPath, unix);
                            }
                            else
                            {
                                //
                                // HACK: This converts null (or an empty
                                //       string) to the fully qualified
                                //       path of the current directory.
                                //
                                newPath = rootPath;
                            }
                        }
                    }

                    //
                    // NOTE: Does the caller want to make all the directory
                    //       separators contained in the path consistent?
                    //
                    if (unix != null)
                    {
                        //
                        // NOTE: When on Unix, use forward slashes; otherwise
                        //       (Windows), use backslashes.
                        //
                        newPath = (bool)unix ?
                            GetUnixPath(newPath) :
                            GetWindowsPath(newPath);
                    }

                    //
                    // NOTE: Does the result need to be normalized to lower
                    //       case?
                    //
                    if (noCase && !String.IsNullOrEmpty(newPath))
                    {
                        //
                        // NOTE: From the MSDN documentation at:
                        //
                        //       "ms-help://MS.NETDEVFX.v20.en/cpref7/html/
                        //              M_System_String_ToLowerInvariant.htm"
                        //
                        //       Security Considerations
                        //
                        //       If you need the lowercase or uppercase version
                        //       of an operating system identifier, such as a
                        //       file name, named pipe, or registry key, use
                        //       the ToLowerInvariant or ToUpperInvariant
                        //       methods.
                        //
                        newPath = newPath.ToLowerInvariant();
                    }

                    //
                    // BUGFIX: Do not remove trailing slashes from a root path.
                    //
                    if (!IsRootPath(newPath))
                    {
                        //
                        // BUGFIX: Finally, remove any trailing slashes.
                        //
                        newPath = TrimEndOfPath(newPath, null);
                    }
                }

                //
                // NOTE: If we get to this point, we have succeeded; however,
                //       this does not necessarily mean that we have a valid
                //       path in the result.
                //
                code = ReturnCode.Ok;
            }
            catch (Exception e)
            {
                //
                // NOTE: We encountered some kind of error while mutating
                //       the path, return null to signal the error to the
                //       caller.
                //
                error = e;
                code = ReturnCode.Error;
            }

            if (EnableTraceForNormalize(null))
            {
                TraceOps.DebugTrace(String.Format(
                    "NormalizePath: interpreter = {0}, rootPath = {1}, " +
                    "path = {2}, unix = {3}, environment = {4}, tilde = {5}, " +
                    "full = {6}, noCase = {7}, newPath = {8}, code = {9}, " +
                    "error = {10}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(rootPath), FormatOps.WrapOrNull(path),
                    FormatOps.WrapOrNull(unix), environment, tilde, full,
                    noCase, FormatOps.WrapOrNull(newPath), ReturnCode.Ok,
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(PathOps).Name, (code == ReturnCode.Ok) ?
                        TracePriority.PathDebug : TracePriority.PathError);
            }

            return code;
        }
    }
}
