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

#if NATIVE
using UNM = Eagle._Components.Private.PathOps.UnsafeNativeMethods;

#if WINDOWS || UNIX
using FSM = Eagle._Components.Public.FileStatusModes;
#endif
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private helper methods used throughout the
    /// Eagle core library for manipulating, normalizing, comparing, combining,
    /// and validating file system paths and URIs, as well as for creating
    /// temporary and unique paths.  It also contains the native interop
    /// declarations needed to query file system information on the supported
    /// platforms.
    /// </summary>
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
        /// <summary>
        /// This class contains the native interop declarations (P/Invoke entry
        /// points) and the associated native structures used by this class to
        /// query file system information on the supported platforms.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("73db358b-e0ef-42b5-9de5-46362ad86e91")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            /// <summary>
            /// This structure mirrors the native Windows <c>FILETIME</c>
            /// structure, representing a date and time as the number of
            /// 100-nanosecond intervals since the Windows system epoch.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("41367b38-86f2-41c3-b7ee-4b3374372039")]
            internal struct FILETIME
            {
                /// <summary>
                /// The low-order 32 bits of the file time.
                /// </summary>
                public uint dwLowDateTime;
                /// <summary>
                /// The high-order 32 bits of the file time.
                /// </summary>
                public uint dwHighDateTime;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure mirrors the native Windows
            /// <c>BY_HANDLE_FILE_INFORMATION</c> structure, which contains the
            /// information retrieved for a file via an open handle.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("bb894e4a-17d1-4f0e-aae1-6f878ad05f2c")]
            internal struct BY_HANDLE_FILE_INFORMATION
            {
                /// <summary>
                /// The file attributes for the file.
                /// </summary>
                public FileFlagsAndAttributes dwFileAttributes;
                /// <summary>
                /// The time the file was created.
                /// </summary>
                public FILETIME ftCreationTime;
                /// <summary>
                /// The time the file was last accessed.
                /// </summary>
                public FILETIME ftLastAccessTime;
                /// <summary>
                /// The time the file was last written to.
                /// </summary>
                public FILETIME ftLastWriteTime;
                /// <summary>
                /// The serial number of the volume that contains the file.
                /// </summary>
                public uint dwVolumeSerialNumber;
                /// <summary>
                /// The high-order 32 bits of the file size, in bytes.
                /// </summary>
                public uint nFileSizeHigh;
                /// <summary>
                /// The low-order 32 bits of the file size, in bytes.
                /// </summary>
                public uint nFileSizeLow;
                /// <summary>
                /// The number of links to the file.
                /// </summary>
                public uint nNumberOfLinks;
                /// <summary>
                /// The high-order 32 bits of the unique identifier associated
                /// with the file.
                /// </summary>
                public uint nFileIndexHigh;
                /// <summary>
                /// The low-order 32 bits of the unique identifier associated
                /// with the file.
                /// </summary>
                public uint nFileIndexLow;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// The native control code used to retrieve the object identifier
            /// for the specified file or directory.
            /// </summary>
            internal const uint FSCTL_GET_OBJECT_ID = 0x9009c;
            /// <summary>
            /// The native control code used to retrieve the object identifier
            /// for the specified file or directory, creating one if it does not
            /// already exist.
            /// </summary>
            internal const uint FSCTL_CREATE_OR_GET_OBJECT_ID = 0x900c0;

            /// <summary>
            /// This structure mirrors the native Windows
            /// <c>FILE_OBJECTID_BUFFER</c> structure, which contains the object
            /// identifiers associated with a file or directory.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("71d21cdf-5626-4197-9c5c-428e4717dc80")]
            internal struct FILE_OBJECTID_BUFFER
            {
                /// <summary>
                /// The object identifier of the file or directory.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] ObjectId;
                /// <summary>
                /// The identifier of the volume on which the object resided
                /// when the object identifier was first created.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] BirthVolumeId;
                /// <summary>
                /// The object identifier of the object at the time it was
                /// first created.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] BirthObjectId;
                /// <summary>
                /// Reserved; the domain identifier associated with the object.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] DomainId;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native Windows <c>CreateFile</c> function,
            /// which creates or opens a file or input/output device and returns
            /// a handle to it.
            /// </summary>
            /// <param name="fileName">
            /// The name of the file or device to be created or opened.
            /// </param>
            /// <param name="desiredAccess">
            /// The requested access to the file or device.
            /// </param>
            /// <param name="shareMode">
            /// The requested sharing mode of the file or device.
            /// </param>
            /// <param name="securityAttributes">
            /// An optional pointer to a security attributes structure, or
            /// <see cref="IntPtr.Zero" /> for none.
            /// </param>
            /// <param name="creationDisposition">
            /// An action to take on a file or device that exists or does not
            /// exist.
            /// </param>
            /// <param name="flagsAndAttributes">
            /// The file or device attributes and flags.
            /// </param>
            /// <param name="templateFile">
            /// An optional handle to a template file, or
            /// <see cref="IntPtr.Zero" /> for none.
            /// </param>
            /// <returns>
            /// Upon success, an open handle to the specified file or device;
            /// otherwise, an invalid handle value.
            /// </returns>
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

            /// <summary>
            /// This method wraps the native Windows <c>DeviceIoControl</c>
            /// function, which sends a control code directly to a specified
            /// device driver, causing the corresponding device to perform the
            /// associated operation.
            /// </summary>
            /// <param name="device">
            /// A handle to the device on which the operation is to be
            /// performed.
            /// </param>
            /// <param name="ioControlCode">
            /// The control code for the operation to be performed.
            /// </param>
            /// <param name="inBuffer">
            /// An optional pointer to the input buffer that contains the data
            /// required to perform the operation, or
            /// <see cref="IntPtr.Zero" /> for none.
            /// </param>
            /// <param name="inBufferSize">
            /// The size, in bytes, of the input buffer.
            /// </param>
            /// <param name="outBuffer">
            /// An optional pointer to the output buffer that is to receive the
            /// data returned by the operation, or <see cref="IntPtr.Zero" />
            /// for none.
            /// </param>
            /// <param name="outBufferSize">
            /// The size, in bytes, of the output buffer.
            /// </param>
            /// <param name="bytesReturned">
            /// Upon success, receives the size, in bytes, of the data stored
            /// in the output buffer.
            /// </param>
            /// <param name="overlapped">
            /// An optional pointer to an overlapped structure, or
            /// <see cref="IntPtr.Zero" /> for none.
            /// </param>
            /// <returns>
            /// True if the operation succeeds; otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method wraps the native Windows
            /// <c>GetFileInformationByHandle</c> function, which retrieves file
            /// information for the file referenced by the specified handle.
            /// </summary>
            /// <param name="file">
            /// A handle to the file for which information is to be retrieved.
            /// </param>
            /// <param name="fileInformation">
            /// Upon success, receives the information for the specified file.
            /// </param>
            /// <returns>
            /// True if the information was retrieved successfully; otherwise,
            /// false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetFileInformationByHandle(
                IntPtr file,
                ref BY_HANDLE_FILE_INFORMATION fileInformation
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native Windows <c>PathIsExe</c> function,
            /// which determines whether a file is an executable by examining its
            /// file name extension.
            /// </summary>
            /// <param name="path">
            /// The path of the file to test.
            /// </param>
            /// <returns>
            /// True if the file is an executable; otherwise, false.
            /// </returns>
            [DllImport(DllName.Shell32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Unicode, BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PathIsExe(string path);
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
            /// <summary>
            /// This structure mirrors the native Unix <c>timespec</c> structure,
            /// representing a time as a number of whole seconds plus a number of
            /// nanoseconds.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("ace5d181-10ec-4bdf-ab7a-72c9e2d698f0")]
            internal struct timespec
            {
                /// <summary>
                /// The number of whole seconds.
                /// </summary>
                public long /* time_t */ tv_sec; // wrong?
                /// <summary>
                /// The number of nanoseconds.
                /// </summary>
                public long tv_nsec;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure mirrors the native Linux <c>stat</c> structure,
            /// which contains the file system metadata for a file or directory.
            /// </summary>
            /* WARNING: Non-portable, select versions of Linux only? */
            [StructLayout(LayoutKind.Explicit)]
            [ObjectId("3f01d0eb-ba6b-4b5a-b15b-68488418a11b")]
            internal struct linux_stat /* monophile: Ubuntu 16.04.7 LTS */
            {
                /// <summary>
                /// The identifier of the device containing the file.
                /// </summary>
                [FieldOffset(0)]
                public ulong /* dev_t */ st_dev; /* 0 */
                /// <summary>
                /// The inode number of the file.
                /// </summary>
                [FieldOffset(8)]
                public ulong /* ino_t */ st_ino; /* 8 */
                /// <summary>
                /// The number of hard links to the file.
                /// </summary>
                [FieldOffset(16)]
                public ulong /* nlink_t */ st_nlink; /* 16 */
                /// <summary>
                /// The file type and mode (permission) bits.
                /// </summary>
                [FieldOffset(24)]
                public uint /* mode_t */ st_mode; /* 24 */
                /// <summary>
                /// The user identifier of the owner of the file.
                /// </summary>
                [FieldOffset(28)]
                public uint /* uid_t */ st_uid; /* 28 */
                /// <summary>
                /// The group identifier of the owner of the file.
                /// </summary>
                [FieldOffset(32)]
                public uint /* gid_t */ st_gid; /* 32 */
                /// <summary>
                /// The device identifier, if the file is a special file.
                /// </summary>
                [FieldOffset(40)]
                public ulong /* dev_t */ st_rdev; /* 40 */
                /// <summary>
                /// The total size of the file, in bytes.
                /// </summary>
                [FieldOffset(48)]
                public ulong /* off_t */ st_size; /* 48 */
                /// <summary>
                /// The preferred block size, in bytes, for file system
                /// input/output.
                /// </summary>
                [FieldOffset(56)]
                public ulong /* blksize_t */ st_blksize; /* 56 */
                /// <summary>
                /// The number of 512-byte blocks allocated to the file.
                /// </summary>
                [FieldOffset(64)]
                public ulong /* blkcnt_t */ st_blocks; /* 64 */
                /// <summary>
                /// The time the file was last accessed.
                /// </summary>
                [FieldOffset(72)]
                public /* struct */ timespec st_atim; /* 72 */
                /// <summary>
                /// The time the file was last modified.
                /// </summary>
                [FieldOffset(88)]
                public /* struct */ timespec st_mtim; /* 88 */
                /// <summary>
                /// The time the file status was last changed.
                /// </summary>
                [FieldOffset(104)]
                public /* struct */ timespec st_ctim; /* 104 */
                /// <summary>
                /// Reserved padding.
                /// </summary>
                [FieldOffset(120)]
                public ulong padding1; /* 120 */
                /// <summary>
                /// Reserved padding.
                /// </summary>
                [FieldOffset(128)]
                public ulong padding2; /* 128 */
                /// <summary>
                /// Reserved padding.
                /// </summary>
                [FieldOffset(136)]
                public ulong padding3; /* 136 */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure mirrors the native macOS <c>stat</c> structure,
            /// which contains the file system metadata for a file or directory.
            /// </summary>
            /* WARNING: Non-portable, select versions of macOS only? */
            [StructLayout(LayoutKind.Explicit)]
            [ObjectId("6904da5b-3efb-4a92-8f4c-3f30c0adc5fb")]
            internal struct macos_stat_buf
            {
                /// <summary>
                /// The identifier of the device containing the file.
                /// </summary>
                [FieldOffset(0)]
                public uint /* dev_t */ st_dev; /* 0 */
                /// <summary>
                /// The file type and mode (permission) bits.
                /// </summary>
                [FieldOffset(4)]
                public ushort /* mode_t */ st_mode; /* 4 */
                /// <summary>
                /// The number of hard links to the file.
                /// </summary>
                [FieldOffset(6)]
                public ushort /* nlink_t */ st_nlink; /* 6 */
                /// <summary>
                /// The inode number of the file.
                /// </summary>
                [FieldOffset(8)]
                public ulong /* ino_t */ st_ino; /* 8 */
                /// <summary>
                /// The user identifier of the owner of the file.
                /// </summary>
                [FieldOffset(16)]
                public uint /* uid_t */ st_uid; /* 16 */
                /// <summary>
                /// The group identifier of the owner of the file.
                /// </summary>
                [FieldOffset(20)]
                public uint /* gid_t */ st_gid; /* 20 */
                /// <summary>
                /// The device identifier, if the file is a special file.
                /// </summary>
                [FieldOffset(24)]
                public ulong /* dev_t */ st_rdev; /* 24 */
                /// <summary>
                /// Reserved padding.
                /// </summary>
                [FieldOffset(28)]
                private uint __pad; /* 28 */
                /// <summary>
                /// The time the file was last accessed.
                /// </summary>
                [FieldOffset(32)]
                public /* struct */ timespec st_atimespec; /* 32 */
                /// <summary>
                /// The time the file was last modified.
                /// </summary>
                [FieldOffset(48)]
                public /* struct */ timespec st_mtimespec; /* 48 */
                /// <summary>
                /// The time the file status was last changed.
                /// </summary>
                [FieldOffset(64)]
                public /* struct */ timespec st_ctimespec; /* 64 */
                /// <summary>
                /// The time the file was created.
                /// </summary>
                [FieldOffset(80)]
                public /* struct */ timespec st_birthtimespec; /* 80 */
                /// <summary>
                /// The total size of the file, in bytes.
                /// </summary>
                [FieldOffset(96)]
                public ulong /* off_t */ st_size; /* 96 */
                /// <summary>
                /// The number of 512-byte blocks allocated to the file.
                /// </summary>
                [FieldOffset(104)]
                public ulong /* blkcnt_t */ st_blocks; /* 104 */
                /// <summary>
                /// The preferred block size, in bytes, for file system
                /// input/output.
                /// </summary>
                [FieldOffset(112)]
                public uint /* blksize_t */ st_blksize; /* 112 */
                /// <summary>
                /// The user-defined flags for the file.
                /// </summary>
                [FieldOffset(116)]
                public uint /* uint32_t */ st_flags; /* 116 */
                /// <summary>
                /// The file generation number.
                /// </summary>
                [FieldOffset(120)]
                public uint /* uint32_t */ st_gen; /* 120 */
                /// <summary>
                /// Reserved for future use.
                /// </summary>
                [FieldOffset(124)]
                public int /* int32_t */ st_lspare; /* 124 */
                /// <summary>
                /// Reserved for future use.
                /// </summary>
                [FieldOffset(128)]
                public long /* int64_t */ st_qspare0; /* 128 */
                /// <summary>
                /// Reserved for future use.
                /// </summary>
                [FieldOffset(136)]
                public long /* int64_t */ st_qspare1; /* 136 */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native Linux <c>__xstat</c> function,
            /// which retrieves the file system metadata for the file at the
            /// specified path.
            /// </summary>
            /// <param name="ver">
            /// The version of the <c>stat</c> structure expected by the caller.
            /// </param>
            /// <param name="path">
            /// The path of the file for which information is to be retrieved.
            /// </param>
            /// <param name="buf">
            /// Upon success, receives the file system metadata for the file.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero value.
            /// </returns>
            /* WARNING: Non-portable, select versions of Linux only? */
            [DllImport(DllName.LibC, EntryPoint = "__xstat",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int linux_xstat(int ver, string path, out linux_stat buf);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native Linux <c>__lxstat</c> function,
            /// which retrieves the file system metadata for the file at the
            /// specified path, without following a final symbolic link.
            /// </summary>
            /// <param name="ver">
            /// The version of the <c>stat</c> structure expected by the caller.
            /// </param>
            /// <param name="path">
            /// The path of the file for which information is to be retrieved.
            /// </param>
            /// <param name="buf">
            /// Upon success, receives the file system metadata for the file.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero value.
            /// </returns>
            /* WARNING: Non-portable, select versions of Linux only? */
            [DllImport(DllName.LibC, EntryPoint = "__lxstat",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int linux_lxstat(int ver, string path, out linux_stat buf);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native macOS <c>stat</c> function, which
            /// retrieves the file system metadata for the file at the specified
            /// path.
            /// </summary>
            /// <param name="path">
            /// The path of the file for which information is to be retrieved.
            /// </param>
            /// <param name="buf">
            /// Upon success, receives the file system metadata for the file.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero value.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "stat",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int macos_stat(string path, out macos_stat_buf buf);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native macOS <c>lstat</c> function, which
            /// retrieves the file system metadata for the file at the specified
            /// path, without following a final symbolic link.
            /// </summary>
            /// <param name="path">
            /// The path of the file for which information is to be retrieved.
            /// </param>
            /// <param name="buf">
            /// Upon success, receives the file system metadata for the file.
            /// </param>
            /// <returns>
            /// Zero on success; otherwise, a non-zero value.
            /// </returns>
            [DllImport(DllName.Internal, EntryPoint = "lstat",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int macos_lstat(string path, out macos_stat_buf buf);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The length, in characters, of a drive prefix (e.g. <c>C:</c>).
        /// </summary>
        private const int DrivePrefixLength = 2;
        /// <summary>
        /// The length, in characters, of an extended-length path prefix (e.g.
        /// <c>\\?\</c>).
        /// </summary>
        private const int ExtendedPrefixLength = 4;
        /// <summary>
        /// The length, in characters, of a UNC path prefix (e.g. <c>\\</c>).
        /// </summary>
        private const int UncPrefixLength = 2;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: The maximum length for a module file name.
        //
        /// <summary>
        /// The maximum length, in characters, for a native module file name.
        /// </summary>
        private static readonly uint UNICODE_STRING_MAX_CHARS = 32767;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if path comparisons should be performed without regard to
        /// case, based on the conventions of the current operating system.
        /// </summary>
        public static readonly bool NoCase =
            PlatformOps.IsWindowsOperatingSystem() ?
                true : PlatformOps.IsUnixOperatingSystem() ? false : true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The <see cref="StringComparison" /> value used when comparing paths,
        /// based on the conventions of the current operating system.
        /// </summary>
        public static readonly StringComparison ComparisonType =
            GetComparisonType();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The <see cref="StringComparer" /> used when comparing paths, based
        /// on the conventions of the current operating system.
        /// </summary>
        public static readonly StringComparer Comparer = GetComparer();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of characters that are treated as wildcards within a path
        /// pattern.
        /// </summary>
        private static readonly char[] PathWildcardChars = {
            Characters.Asterisk,
            Characters.QuestionMark
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of build configuration names considered when searching for
        /// build output directories.
        /// </summary>
        private static readonly string[] BuildConfigurations = {
            BuildConfiguration.Debug, BuildConfiguration.Release
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used when reading a scalar (single) native
        /// path configuration value.
        /// </summary>
        private static readonly ConfigurationFlags ScalarConfigurationFlags =
            ConfigurationFlags.PathOps | ConfigurationFlags.NativePathValue;

        /// <summary>
        /// The configuration flags used when reading a list of native path
        /// configuration values.
        /// </summary>
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
        /// <summary>
        /// The primary directory separator character used by this library.
        /// </summary>
        public static readonly char DirectorySeparatorChar = Characters.DirectorySeparator;
        /// <summary>
        /// The alternate directory separator character used by this library.
        /// </summary>
        public static readonly char AltDirectorySeparatorChar = Characters.AltDirectorySeparator;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The relative path component that refers to the current directory
        /// (e.g. <c>.</c>).
        /// </summary>
        public static readonly string CurrentDirectory = _Path.Current;
        /// <summary>
        /// The relative path component that refers to the parent directory
        /// (e.g. <c>..</c>).
        /// </summary>
        public static readonly string ParentDirectory = _Path.Parent;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The directory separator character that is not native to the current
        /// operating system.
        /// </summary>
        public static readonly char NonNativeDirectorySeparatorChar =
            PlatformOps.IsWindowsOperatingSystem() ?
                AltDirectorySeparatorChar :
                DirectorySeparatorChar;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The directory separator character that is native to the current
        /// operating system.
        /// </summary>
        public static readonly char NativeDirectorySeparatorChar =
            PlatformOps.IsWindowsOperatingSystem() ?
                DirectorySeparatorChar :
                AltDirectorySeparatorChar;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached set of directory separator characters, lazily
        /// initialized on first use.
        /// </summary>
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

        /// <summary>
        /// The number of 100-nanosecond intervals between the Windows system
        /// epoch (1601-01-01) and the POSIX epoch (1970-01-01).
        /// </summary>
        private const ulong POSIX_EPOCH_AS_FILETIME = (ulong)116444736 * (ulong)1000000000;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This URI is only used to temporarily build an absolute
        //       URI from a relative one so the GetComponents method may
        //       be used to grab portions of the relative URI.
        //
        /// <summary>
        /// The default base URI used to temporarily build an absolute URI from
        /// a relative one when extracting URI components.
        /// </summary>
        private static readonly Uri DefaultBaseUri = new Uri("https://www.example.com/");

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to determine whether a string is a valid
        /// identifier (i.e. consists only of letters, digits, and underscores).
        /// </summary>
        private static readonly Regex identifierRegEx = RegExOps.Create(
            "^[0-9A-Z_]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unique Path Constants
        /// <summary>
        /// The default prefix used when forming a unique path on Windows.
        /// </summary>
        private static readonly string DefaultWindowsUniquePrefix = "eiq-";
        /// <summary>
        /// The default prefix used when forming a unique path on Unix.
        /// </summary>
        private static readonly string DefaultUnixUniquePrefix = "eagle-unique-path-";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default maximum number of times to retry when attempting to
        /// create a unique path.
        /// </summary>
        private static readonly int DefaultUniqueMaximumRetries = 10000;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default number of random bytes used within a unique path on
        /// Windows.
        /// </summary>
        private static readonly int DefaultWindowsUniqueByteCount = sizeof(ushort);
        /// <summary>
        /// The default number of random bytes used within a unique path on
        /// Unix.
        /// </summary>
        private static readonly int DefaultUnixUniqueByteCount = sizeof(ulong);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the other static data
        //       members in this class.
        //
        /// <summary>
        /// The object used to synchronize access to the other static data
        /// members in this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is greater than zero, trace calls into the very
        //       popular method NormalizePath.  This is handled specially due
        //       to it being in the hot-path for basically everything.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When greater than zero, trace calls into the NormalizePath method,
        /// which is handled specially due to it being in the hot-path for
        /// nearly everything.
        /// </summary>
        private static int traceForNormalize = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is non-zero the "TEMP" and "TMP" environment
        //       variables may be used when searching for a suitable (base)
        //       directory for temporary files.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the test-specific temporary environment variables may
        /// be used when searching for a suitable base directory for temporary
        /// files.
        /// </summary>
        private static bool includeTestTemporaryEnvVars = true;
        /// <summary>
        /// When non-zero, the XDG temporary environment variables may be used
        /// when searching for a suitable base directory for temporary files.
        /// </summary>
        private static bool includeXdgTemporaryEnvVars = false;
        /// <summary>
        /// When non-zero, the system temporary environment variables (e.g.
        /// "TEMP" and "TMP") may be used when searching for a suitable base
        /// directory for temporary files.
        /// </summary>
        private static bool includeSystemTemporaryEnvVars = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is non-zero, all temporary file names returned
        //       from this class will be validated beforehand, via the method
        //       ValidatePathAsFile.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, all temporary file names returned from this class are
        /// validated beforehand, via the ValidatePathAsFile method.
        /// </summary>
        private static bool validateTemporaryFileName = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this value is non-null, it will be (Path) combined with
        //       any directory value that is being used as a base temporary
        //       directory.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the temporary sub-path is combined with any directory
        /// value being used as a base temporary directory.
        /// </summary>
        private static bool useTemporarySubPath = false;
        /// <summary>
        /// When non-null, this value is combined with any directory value being
        /// used as a base temporary directory.
        /// </summary>
        private static string temporarySubPath = GlobalState.GetPackageFileNameOnly();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this is not null, it will be used as the return value from
        //       the (extremely important) GetBinaryPath method.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-null, this value is used as the return value from the
        /// GetBinaryPath method.
        /// </summary>
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
        /// <summary>
        /// The list of candidate path components, in priority order, used when
        /// attempting to locate the cloud drive directory for a user.  These
        /// path components are appended to the home directory for a given user
        /// prior to being checked for validity.
        /// </summary>
        private static readonly string[] defaultCloudPaths = {
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

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-null, this callback is used to obtain a temporary file name
        /// instead of the default mechanism.
        /// </summary>
        private static GetStringValueCallback getTempFileNameCallback = null;
        /// <summary>
        /// When non-null, this callback is used to obtain the temporary
        /// directory path instead of the default mechanism.
        /// </summary>
        private static GetStringValueCallback getTempPathCallback = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, native methods are not used when determining whether
        /// two paths refer to the same file.
        /// </summary>
        private static bool NoNativeIsSameFile = false;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the URI components to be used from the baseUri in
        //       the TryCombineUris method.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The URI components to be used from the base URI in the
        /// TryCombineUris method.
        /// </summary>
        private static UriComponents BaseUriComponents = UriComponents.Scheme |
            UriComponents.UserInfo | UriComponents.Host | UriComponents.Port;

        /// <summary>
        /// The default <see cref="UriFormat" /> used when extracting URI
        /// components.
        /// </summary>
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
        /// <summary>
        /// The prefix used when attempting to form a unique path for use by
        /// external callers (e.g. temporary log files).
        /// </summary>
        private static string UniquePrefix;
        /// <summary>
        /// The suffix used when attempting to form a unique path for use by
        /// external callers (e.g. temporary log files).
        /// </summary>
        private static string UniqueSuffix;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the maximum number of times to retry before giving upon
        //       on being able to create a unique path.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The maximum number of times to retry before giving up on being able
        /// to create a unique path.
        /// </summary>
        private static int UniqueMaximumRetries;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of random bytes to use within the path when
        //       attempting to create a unique path.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The number of random bytes to use within the path when attempting to
        /// create a unique path.
        /// </summary>
        private static int UniqueByteCount;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds rows describing the current path-related
        /// configuration of this class to the specified list, for use in
        /// building diagnostic or introspection output.
        /// </summary>
        /// <param name="list">
        /// The list to which the path information rows are added.  If this
        /// value is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control how much detail is included in the
        /// resulting path information.
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

        /// <summary>
        /// This method initializes the static fields used when generating
        /// unique file or directory names, populating any of them that have
        /// not yet been set with their platform-appropriate default values.
        /// </summary>
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

        /// <summary>
        /// This method fills in any unspecified unique-name generation
        /// properties with the corresponding configured default values.  Only
        /// those parameters that are null or zero upon entry are modified.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to use for generated names.  If this value is null upon
        /// entry, it is set to the configured default prefix.
        /// </param>
        /// <param name="suffix">
        /// The suffix to use for generated names.  If this value is null upon
        /// entry, it is set to the configured default suffix.
        /// </param>
        /// <param name="byteCount">
        /// The number of random bytes to use for generated names.  If this
        /// value is zero upon entry, it is set to the configured default byte
        /// count.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry name generation.  If this
        /// value is zero upon entry, it is set to the configured default
        /// maximum retry count.
        /// </param>
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

        /// <summary>
        /// This method sets the cached binary path used by this class.
        /// </summary>
        /// <param name="path">
        /// The binary path to store.
        /// </param>
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

        /// <summary>
        /// This method sets the URI components used by this class when forming
        /// base URIs.
        /// </summary>
        /// <param name="uriComponents">
        /// The URI components to store.
        /// </param>
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

        /// <summary>
        /// This method sets the URI format used by this class when forming
        /// URIs.
        /// </summary>
        /// <param name="uriFormat">
        /// The URI format to store.
        /// </param>
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

        /// <summary>
        /// This method determines whether the specified array of directory
        /// separator characters is non-null and non-empty.
        /// </summary>
        /// <param name="characters">
        /// The array of directory separator characters to check.
        /// </param>
        /// <returns>
        /// True if the array is non-null and contains at least one character;
        /// otherwise, false.
        /// </returns>
        private static bool HaveDirectoryChars(
            char[] characters
            )
        {
            return (characters != null) && (characters.Length > 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new array of directory separator characters,
        /// optionally including both the native and non-native separators.
        /// </summary>
        /// <param name="both">
        /// Non-zero to include both the native and non-native directory
        /// separator characters; zero to include only the native directory
        /// separator character.
        /// </param>
        /// <returns>
        /// The newly created array of directory separator characters.
        /// </returns>
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

        /// <summary>
        /// This method returns the cached array of directory separator
        /// characters, creating and caching it based on the current operating
        /// system if it does not already exist.
        /// </summary>
        /// <returns>
        /// The cached array of directory separator characters.
        /// </returns>
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

        /// <summary>
        /// This method attempts to obtain the array of directory separator
        /// characters appropriate for the current operating system.
        /// </summary>
        /// <param name="characters">
        /// Upon success, this receives the array of directory separator
        /// characters.
        /// </param>
        /// <returns>
        /// True if a non-empty array of directory separator characters was
        /// obtained; otherwise, false.
        /// </returns>
        private static bool TryGetDirectoryChars(
            out char[] characters
            )
        {
            return TryGetDirectoryChars(null, out characters);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain an array of directory separator
        /// characters, optionally building a fresh array that includes both the
        /// native and non-native separators.
        /// </summary>
        /// <param name="both">
        /// When non-null, a fresh array is created and always returned: a value
        /// of true includes both the native and non-native directory separator
        /// characters, while false includes only the native one.  When null,
        /// the cached array for the current operating system is used instead.
        /// </param>
        /// <param name="characters">
        /// Upon success, this receives the array of directory separator
        /// characters.
        /// </param>
        /// <returns>
        /// True if a non-empty array of directory separator characters was
        /// obtained; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the string comparer appropriate for comparing
        /// file names on the current operating system.
        /// </summary>
        /// <returns>
        /// A case-insensitive comparer on Windows, where file names are not
        /// case-sensitive; otherwise, a case-sensitive (ordinal) comparer.
        /// </returns>
        public static StringComparer GetComparer()
        {
            //
            // WINDOWS: File names are not case-sensitive.
            //
            return PlatformOps.IsWindowsOperatingSystem() ?
                StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string comparison type appropriate for
        /// comparing file names on the current operating system.
        /// </summary>
        /// <returns>
        /// A case-insensitive comparison type on Windows, where file names are
        /// not case-sensitive; otherwise, a case-sensitive comparison type, as
        /// file names are assumed to be binary and case-sensitive.
        /// </returns>
        public static StringComparison GetComparisonType()
        {
            //
            // WINDOWS: File names are not case-sensitive.
            //
            if (PlatformOps.IsWindowsOperatingSystem())
                return SharedStringOps.GetSystemComparisonType(true);

#if DEAD_CODE
            //
            // UNIX: File names are case-sensitive.
            //
            if (PlatformOps.IsUnixOperatingSystem())
                return SharedStringOps.GetSystemComparisonType(false);
#endif

            //
            // UNKNOWN: Assume that file names are binary
            //          (and case-sensitive).
            //
            return SharedStringOps.GetSystemComparisonType(false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the currently configured callback delegate of
        /// the specified type.
        /// </summary>
        /// <param name="callbackType">
        /// The type of path-related callback to retrieve.
        /// </param>
        /// <param name="delegate">
        /// Upon success, this receives the configured callback delegate of the
        /// requested type, which may be null if no callback has been set.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// callback could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method changes the configured callback delegate of the
        /// specified type, clearing it when the supplied delegate is null.
        /// </summary>
        /// <param name="callbackType">
        /// The type of path-related callback to change.
        /// </param>
        /// <param name="delegate">
        /// The callback delegate to store, or null to clear the existing
        /// callback.  When non-null, it must be convertible to the delegate
        /// type expected for the specified callback type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// callback could not be changed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method initializes the specified native file information
        /// structure to its default (zeroed) state prior to use.
        /// </summary>
        /// <param name="fileInformation">
        /// Upon return, this receives a fully initialized file information
        /// structure with all of its fields reset to their default values.
        /// </param>
        private static void InitializeFileInformation(
            out UNM.BY_HANDLE_FILE_INFORMATION fileInformation /* out */
            )
        {
            fileInformation.dwFileAttributes =
                FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

            fileInformation.ftCreationTime = new UNM.FILETIME();
            fileInformation.ftLastAccessTime = new UNM.FILETIME();
            fileInformation.ftLastWriteTime = new UNM.FILETIME();

            fileInformation.dwVolumeSerialNumber = 0;

            fileInformation.nFileSizeHigh = 0;
            fileInformation.nFileSizeLow = 0;

            fileInformation.nNumberOfLinks = 0;

            fileInformation.nFileIndexHigh = 0;
            fileInformation.nFileIndexLow = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the native file system for information about the
        /// specified file or directory, populating a native file information
        /// structure.  This operation is only supported on Windows.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file or directory to query.
        /// </param>
        /// <param name="directory">
        /// Non-zero if the named path refers to a directory; otherwise, zero.
        /// </param>
        /// <param name="reparse">
        /// Non-zero to open the reparse point itself rather than its target,
        /// when the named path is a reparse point.
        /// </param>
        /// <param name="fileInformation">
        /// Upon success, this receives the file information retrieved for the
        /// named path.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// file information could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode GetPathInformation(
            string fileName,                                    /* in */
            bool directory,                                     /* in */
            bool reparse,                                       /* in */
            ref UNM.BY_HANDLE_FILE_INFORMATION fileInformation, /* out */
            ref Result error                                    /* out */
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

                        handle = UNM.CreateFile(
                            fileName, FileAccessMask.FILE_NONE,
                            FileShareMode.FILE_SHARE_ALL, IntPtr.Zero,
                            FileCreationDisposition.OPEN_EXISTING,
                            fileFlagsAndAttributes, IntPtr.Zero);

                        if (NativeOps.IsValidHandle(handle))
                        {
                            if (UNM.GetFileInformationByHandle(
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

        /// <summary>
        /// This method queries the native file system for information about the
        /// specified path and returns it as a list of name/value pairs.  This
        /// operation is only supported on Windows.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to query.
        /// </param>
        /// <param name="directory">
        /// Non-zero if the path refers to a directory; otherwise, zero.
        /// </param>
        /// <param name="reparse">
        /// Non-zero to open the reparse point itself rather than its target,
        /// when the path is a reparse point.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of name/value pairs describing
        /// the file information.  If it is null upon entry, a new list is
        /// created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// file information could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
                UNM.BY_HANDLE_FILE_INFORMATION fileInformation;

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

        /// <summary>
        /// This method queries the native file system for the size, in bytes,
        /// of the specified file or directory.  This operation is only
        /// supported on Windows.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to query.
        /// </param>
        /// <param name="directory">
        /// Non-zero if the path refers to a directory; otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the size of the path, in bytes, as a
        /// string.  Upon failure, this receives an error message that describes
        /// why the size could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetSize(
            string path,      /* in */
            bool directory,   /* in */
            ref Result result /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UNM.BY_HANDLE_FILE_INFORMATION fileInformation;

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
        /// <summary>
        /// This method converts a native file time value into a POSIX time
        /// value, i.e. the number of seconds elapsed since the POSIX epoch.
        /// </summary>
        /// <param name="fileTime">
        /// The native file time value to convert.
        /// </param>
        /// <returns>
        /// The converted POSIX time value, in seconds since the POSIX epoch.
        /// </returns>
        private static ulong ToTimeT(
            UNM.FILETIME fileTime /* in */
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
        /// <summary>
        /// This method derives the POSIX-style file mode bits from the
        /// specified native file flags and attributes.
        /// </summary>
        /// <param name="flagsAndAttributes">
        /// The native file flags and attributes to translate into file mode
        /// bits.
        /// </param>
        /// <param name="checkLinks">
        /// Non-zero to treat a reparse point as a symbolic link; otherwise, the
        /// reparse-point attribute is ignored.
        /// </param>
        /// <param name="isExecutable">
        /// Non-zero if the file should be considered executable, causing the
        /// execute mode bit to be set.
        /// </param>
        /// <param name="userOnly">
        /// Non-zero to limit the resulting mode bits to the owning user; zero to
        /// also propagate the user permissions to the group and other classes.
        /// </param>
        /// <returns>
        /// The computed file mode bits.
        /// </returns>
        private static FSM GetMode(
            FileFlagsAndAttributes flagsAndAttributes, /* in */
            bool checkLinks,                           /* in */
            bool isExecutable,                         /* in */
            bool userOnly                              /* in */
            )
        {
            FSM mode = FSM.S_INONE;

            if (checkLinks && FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_REPARSE_POINT, true))
            {
                mode |= FSM.S_IFLNK;
            }
            else if (FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY, true))
            {
                mode |= FSM.S_IFDIR | FSM.S_IEXEC;
            }
            else
            {
                mode |= FSM.S_IFREG;
            }

            if (FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_READONLY, true))
            {
                mode |= FSM.S_IREAD;
            }
            else
            {
                mode |= FSM.S_IREAD | FSM.S_IWRITE;
            }

            if (isExecutable)
                mode |= FSM.S_IEXEC;

            if (!userOnly)
                AdjustPermissions(true, ref mode);

            return mode;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path is likely to refer
        /// to an executable file, based on platform-specific checks and the file
        /// name extension.
        /// </summary>
        /// <param name="path">
        /// The path of the file to examine.
        /// </param>
        /// <returns>
        /// True if the path appears to refer to an executable file; otherwise,
        /// false.
        /// </returns>
        private static bool MightBeExecutable(
            string path /* in */
            )
        {
            try
            {
                if (String.IsNullOrEmpty(path))
                    return false;

                if (PlatformOps.IsWindowsOperatingSystem() &&
                    UNM.PathIsExe(path)) /* throw */
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
        /// <summary>
        /// This method retrieves the owning user and group for the specified
        /// file or directory.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory whose owner is to be retrieved.
        /// </param>
        /// <param name="ownerUser">
        /// Upon success, this receives the identity reference of the owning
        /// user.
        /// </param>
        /// <param name="ownerGroup">
        /// Upon success, this receives the identity reference of the owning
        /// group.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// owner could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the native file system object identifier for
        /// the specified file or directory, optionally creating one if it does
        /// not already exist.  This operation is only supported on Windows.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file or directory whose object identifier is to be
        /// retrieved.
        /// </param>
        /// <param name="directory">
        /// Non-zero if the named path refers to a directory; otherwise, zero.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an object identifier if one does not already
        /// exist; zero to only retrieve an existing object identifier.
        /// </param>
        /// <param name="fileObjectId">
        /// Upon success, this receives the native object identifier buffer for
        /// the named path.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// object identifier could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode GetObjectId(
            string fileName,                           /* in */
            bool directory,                            /* in */
            bool create,                               /* in */
            ref UNM.FILE_OBJECTID_BUFFER fileObjectId, /* out */
            ref Result error                           /* out */
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

                        handle = UNM.CreateFile(
                            fileName, FileAccessMask.FILE_NONE,
                            FileShareMode.FILE_SHARE_READ_WRITE, IntPtr.Zero,
                            FileCreationDisposition.OPEN_EXISTING,
                            fileFlagsAndAttributes, IntPtr.Zero);

                        if (NativeOps.IsValidHandle(handle))
                        {
                            IntPtr outBuffer = IntPtr.Zero;

                            try
                            {
                                int outBufferSize = Marshal.SizeOf(typeof(
                                    UNM.FILE_OBJECTID_BUFFER));

                                outBuffer = Marshal.AllocCoTaskMem(
                                    outBufferSize);

                                if (outBuffer != IntPtr.Zero)
                                {
                                    uint bytesReturned = 0;

                                    if (UNM.DeviceIoControl(
                                            handle, create ?
                                                UNM.FSCTL_CREATE_OR_GET_OBJECT_ID :
                                                UNM.FSCTL_GET_OBJECT_ID,
                                            IntPtr.Zero, 0, outBuffer, (uint)outBufferSize,
                                            ref bytesReturned, IntPtr.Zero))
                                    {
                                        fileObjectId = (UNM.FILE_OBJECTID_BUFFER)
                                            Marshal.PtrToStructure(outBuffer,
                                                typeof(UNM.FILE_OBJECTID_BUFFER));

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

        /// <summary>
        /// This method retrieves the native file system object identifier for
        /// the specified path and returns it as a list of name/value pairs,
        /// optionally creating one if it does not already exist.  This operation
        /// is only supported on Windows.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory whose object identifier is to be
        /// retrieved.
        /// </param>
        /// <param name="directory">
        /// Non-zero if the path refers to a directory; otherwise, zero.
        /// </param>
        /// <param name="create">
        /// Non-zero to create an object identifier if one does not already
        /// exist; zero to only retrieve an existing object identifier.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the list of name/value pairs describing
        /// the object identifier.  If it is null upon entry, a new list is
        /// created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// object identifier could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
                UNM.FILE_OBJECTID_BUFFER fileObjectId =
                    new UNM.FILE_OBJECTID_BUFFER();

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

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        /// <summary>
        /// This method adjusts the group and other permission bits of the
        /// specified file mode, either propagating the user permissions to them
        /// or clearing them entirely.
        /// </summary>
        /// <param name="addFromUser">
        /// Non-zero to add the group and other permission bits derived from the
        /// user permissions; zero to clear all of the group, other, and combined
        /// permission bits.
        /// </param>
        /// <param name="mode">
        /// The file mode bits to adjust in place.
        /// </param>
        private static void AdjustPermissions(
            bool addFromUser, /* in */
            ref FSM mode      /* in, out */
            )
        {
            FSM groupModes = (FSM)((uint)(mode & FSM.S_IRWX) >> 3);
            FSM otherModes = (FSM)((uint)(mode & FSM.S_IRWX) >> 6);

            if (addFromUser)
            {
                mode |= groupModes;
                mode |= otherModes;
            }
            else
            {
                mode &= ~otherModes;
                mode &= ~groupModes;
                mode &= ~FSM.S_IRWX;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the POSIX-style file mode bits for the
        /// specified file or directory, using the appropriate native mechanism
        /// for the current operating system.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory whose mode is to be retrieved.
        /// </param>
        /// <param name="checkLinks">
        /// Non-zero to query the symbolic link itself rather than its target,
        /// when the path refers to a symbolic link.
        /// </param>
        /// <param name="mode">
        /// Upon success, this receives the file mode bits for the path.
        /// </param>
        /// <param name="error">
        /// Upon failure, this receives an error message that describes why the
        /// mode could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode GetMode(
            string path,     /* in */
            bool checkLinks, /* in */
            ref FSM mode,    /* out */
            ref Result error /* out */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
#if WINDOWS
                UNM.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, Directory.Exists(path), false,
                        ref fileInformation, ref error) == ReturnCode.Ok)
                {
                    mode = GetMode(
                        fileInformation.dwFileAttributes, checkLinks,
                        MightBeExecutable(path), false);

                    return ReturnCode.Ok;
                }
#else
                error = "not implemented on this operating system";
#endif
            }
            else if (PlatformOps.IsLinuxOperatingSystem())
            {
#if UNIX
                try
                {
                    UNM.linux_stat buf;

                    if ((!checkLinks &&
                        (UNM.linux_xstat(0, path, out buf) == 0)) ||
                        (checkLinks &&
                        (UNM.linux_lxstat(0, path, out buf) == 0)))
                    {
                        mode = (FSM)buf.st_mode;
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
#else
                error = "not implemented on this operating system";
#endif
            }
            else if (PlatformOps.IsMacintoshOperatingSystem())
            {
#if UNIX
                try
                {
                    UNM.macos_stat_buf buf;

                    if ((!checkLinks &&
                        (UNM.macos_stat(path, out buf) == 0)) ||
                        (checkLinks &&
                        (UNM.macos_lstat(path, out buf) == 0)))
                    {
                        mode = (FSM)buf.st_mode;
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
#else
                error = "not implemented on this operating system";
#endif
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used directly by both the [file lstat]
        //       and [file stat] sub-commands.
        //
        /// <summary>
        /// This method queries the file system status information for the
        /// specified path, returning the results as a name/value pair list
        /// (e.g. device, inode, mode, link count, owner, size, and time
        /// stamps).  The exact mechanism used to obtain this information is
        /// dependent upon the host operating system.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory for which the status information
        /// is to be queried.
        /// </param>
        /// <param name="checkLinks">
        /// Non-zero to query the status of the link itself rather than the
        /// target of the link, when the path refers to a symbolic link.
        /// </param>
        /// <param name="reparse">
        /// Non-zero to follow reparse points when querying the path
        /// information.
        /// </param>
        /// <param name="list">
        /// Upon success, this list is populated with the name/value pairs that
        /// describe the status information for the path.  If the value is null
        /// upon entry, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
#if WINDOWS
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

                    UNM.BY_HANDLE_FILE_INFORMATION fileInformation;

                    InitializeFileInformation(out fileInformation);

                    if (GetPathInformation(
                            path, Directory.Exists(path), reparse,
                            ref fileInformation, ref error) == ReturnCode.Ok)
                    {
                        int device = 0;

                        if (!String.IsNullOrEmpty(path) && Char.IsLetter(path[0]))
                            device = Char.ToLower(path[0]) - Characters.a;

                        uint mode = (uint)GetMode(
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
#else
                error = "not implemented on this operating system";
#endif
            }
            else if (PlatformOps.IsLinuxOperatingSystem())
            {
#if UNIX
                try
                {
                    UNM.linux_stat buf;

                    if ((!checkLinks &&
                        (UNM.linux_xstat(0, path, out buf) == 0)) ||
                        (checkLinks &&
                        (UNM.linux_lxstat(0, path, out buf) == 0)))
                    {
                        if (list == null)
                            list = new StringList();

                        list.Add(
                            "dev",
                            buf.st_dev.ToString(),
                            "ino",
                            buf.st_ino.ToString(),
                            "mode",
                            buf.st_mode.ToString(),
                            "nlink",
                            buf.st_nlink.ToString(),
                            "uid",
                            buf.st_uid.ToString(),
                            "gid",
                            buf.st_gid.ToString(),
                            "rdev",
                            buf.st_rdev.ToString(),
                            "size",
                            buf.st_size.ToString(),
                            "atime",
                            StringList.MakeList(
                                buf.st_atim.tv_sec,
                                buf.st_atim.tv_nsec),
                            "mtime",
                            StringList.MakeList(
                                buf.st_mtim.tv_sec,
                                buf.st_mtim.tv_nsec),
                            "ctime",
                            StringList.MakeList(
                                buf.st_ctim.tv_sec,
                                buf.st_ctim.tv_nsec),
                            "type",
                            FileOps.GetFileType(path));

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
#else
                error = "not implemented on this operating system";
#endif
            }
            else if (PlatformOps.IsMacintoshOperatingSystem())
            {
#if UNIX
                try
                {
                    UNM.macos_stat_buf buf;

                    if ((!checkLinks &&
                        (UNM.macos_stat(path, out buf) == 0)) ||
                        (checkLinks &&
                        (UNM.macos_lstat(path, out buf) == 0)))
                    {
                        if (list == null)
                            list = new StringList();

                        list.Add(
                            "dev",
                            buf.st_dev.ToString(),
                            "ino",
                            buf.st_ino.ToString(),
                            "mode",
                            buf.st_mode.ToString(),
                            "nlink",
                            buf.st_nlink.ToString(),
                            "uid",
                            buf.st_uid.ToString(),
                            "gid",
                            buf.st_gid.ToString(),
                            "rdev",
                            buf.st_rdev.ToString(),
                            "size",
                            buf.st_size.ToString(),
                            "atime",
                            StringList.MakeList(
                                buf.st_atimespec.tv_sec,
                                buf.st_atimespec.tv_nsec),
                            "mtime",
                            StringList.MakeList(
                                buf.st_mtimespec.tv_sec,
                                buf.st_mtimespec.tv_nsec),
                            "ctime",
                            StringList.MakeList(
                                buf.st_ctimespec.tv_sec,
                                buf.st_ctimespec.tv_nsec),
                            "type",
                            FileOps.GetFileType(path));

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
#else
                error = "not implemented on this operating system";
#endif
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path refers to a
        /// "normal" file system object, optionally verifying that it is a
        /// file (or directory) and, on platforms with native support,
        /// checking its mode bits.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="mustBeFile">
        /// When non-null, this controls the type check: a non-zero value
        /// requires the path to refer to an existing file, while a value of
        /// zero requires it to refer to an existing file or directory.  When
        /// null, no existence check is performed.
        /// </param>
        /// <param name="checkLinks">
        /// When non-null on platforms with native support, the mode bits of
        /// the path are checked; a non-zero value examines the link itself
        /// rather than its target.  When null, no mode check is performed.
        /// </param>
        /// <returns>
        /// True if the path is considered normal; otherwise, false.
        /// </returns>
        public static bool IsNormal(
            string path,      /* in */
            bool? mustBeFile, /* in: OPTIONAL */
            bool? checkLinks  /* in: OPTIONAL */
            )
        {
            bool isWindows = PlatformOps.IsWindowsOperatingSystem();

            if (!CheckForValid(
                    null, path, false, false, true, isWindows))
            {
                return false;
            }

            if (mustBeFile != null)
            {
                if ((bool)mustBeFile)
                {
                    if (!File.Exists(path))
                        return false;
                }
                else
                {
                    if (!Directory.Exists(path) &&
                        !File.Exists(path))
                    {
                        return false;
                    }
                }
            }

#if NATIVE && (WINDOWS || UNIX)
            if (checkLinks != null)
            {
                Result error; /* REUSED */

#if !NET_STANDARD_20 && !MONO
                if (isWindows)
                {
                    error = null;

                    if (!FileOps.CanReadFileAttributes(
                            path, ref error))
                    {
                        goto skipGetMode;
                    }
                }
#endif

                FSM mode = FSM.S_INONE;

                error = null;

                if (GetMode(
                        path, (bool)checkLinks, ref mode,
                        ref error) != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsNormal: error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(PathOps).Name,
                        TracePriority.FileSystemWarning);

                    return false;
                }

                if (mustBeFile != null)
                {
                    mode &= FSM.S_IFMT;

                    if ((bool)mustBeFile)
                    {
                        if (mode != FSM.S_IFREG)
                            return false;
                    }
                    else
                    {
                        if (mode != FSM.S_IFDIR)
                            return false;
                    }
                }
                else
                {
                    mode &= ~FSM.S_IFNRML;

                    AdjustPermissions(false, ref mode);

                    if (mode != FSM.S_INONE)
                        return false;
                }
            }

#if !NET_STANDARD_20 && !MONO
        skipGetMode:
            ;
#endif
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// drive letter followed by a colon (e.g. <c>C:</c>).
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require that the path consist of exactly the drive
        /// letter and colon; otherwise, the path may contain additional
        /// characters following the colon.
        /// </param>
        /// <returns>
        /// True if the path begins with a drive letter and colon; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// drive letter followed by a colon (e.g. <c>C:</c>), using a
        /// previously computed path length.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="length">
        /// The length, in characters, of the path.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require that the path consist of exactly the drive
        /// letter and colon; otherwise, the path may contain additional
        /// characters following the colon.
        /// </param>
        /// <returns>
        /// True if the path begins with a drive letter and colon; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// drive letter followed by a colon (e.g. <c>C:</c>), also returning
        /// the path length and the offset just past the drive prefix.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require that the path consist of exactly the drive
        /// letter and colon; otherwise, the path may contain additional
        /// characters following the colon.
        /// </param>
        /// <param name="length">
        /// Upon return, this contains the length, in characters, of the path.
        /// </param>
        /// <param name="offset">
        /// Upon success, this is set to the offset of the first character
        /// following the drive letter and colon prefix.
        /// </param>
        /// <returns>
        /// True if the path begins with a drive letter and colon; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// drive letter followed by a colon (e.g. <c>C:</c>), using a
        /// previously computed path length and returning the offset just past
        /// the drive prefix.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="length">
        /// The length, in characters, of the path.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require that the path consist of exactly the drive
        /// letter and colon; otherwise, the path may contain additional
        /// characters following the colon.
        /// </param>
        /// <param name="offset">
        /// Upon success, this is set to the offset of the first character
        /// following the drive letter and colon prefix.
        /// </param>
        /// <returns>
        /// True if the path begins with a drive letter and colon; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with an
        /// extended-length path prefix (e.g. <c>\\?\</c> or <c>\??\</c>),
        /// returning the offset just past that prefix.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="length">
        /// The length, in characters, of the path.
        /// </param>
        /// <param name="offset">
        /// Upon success, this is set to the offset of the first character
        /// following the extended-length path prefix.
        /// </param>
        /// <returns>
        /// True if the path begins with an extended-length path prefix;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// UNC prefix (i.e. two leading slashes or backslashes).
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <returns>
        /// True if the path begins with a UNC prefix; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// UNC prefix (i.e. two leading slashes or backslashes), using a
        /// previously computed path length.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="length">
        /// The length, in characters, of the path.
        /// </param>
        /// <returns>
        /// True if the path begins with a UNC prefix; otherwise, false.
        /// </returns>
        private static bool HasUncPrefix(
            string path, /* in */
            int length   /* in */
            )
        {
            int offset = 0;

            return HasUncPrefix(path, length, ref offset);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// UNC prefix (i.e. two leading slashes or backslashes), using a
        /// previously computed path length and returning the offset just past
        /// that prefix.
        /// </summary>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="length">
        /// The length, in characters, of the path.
        /// </param>
        /// <param name="offset">
        /// Upon success, this is set to the offset of the first character
        /// following the UNC prefix.
        /// </param>
        /// <returns>
        /// True if the path begins with a UNC prefix; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the set of characters that are not permitted in
        /// a path or file name on the current platform.
        /// </summary>
        /// <param name="fileNameOnly">
        /// Non-zero to return the characters that are invalid within a file
        /// name; otherwise, the characters that are invalid within a full path
        /// are returned.
        /// </param>
        /// <returns>
        /// An array of the invalid characters, which may be null.
        /// </returns>
        private static char[] GetInvalidChars( /* MAY RETURN NULL */
            bool fileNameOnly /* in */
            )
        {
            return fileNameOnly ?
                Path.GetInvalidFileNameChars() :
                Path.GetInvalidPathChars();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates the specified path by splitting it into its
        /// components and verifying that each component contains no invalid
        /// file name characters, optionally allowing a leading drive letter
        /// prefix.
        /// </summary>
        /// <param name="path">
        /// The path to be validated.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to allow the first component to be a drive letter and
        /// colon prefix.
        /// </param>
        /// <returns>
        /// True if every component of the path is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path is valid by
        /// checking it for characters that are not permitted in a path or file
        /// name and, optionally, by validating its individual components.
        /// </summary>
        /// <param name="unix">
        /// This parameter is not used.
        /// </param>
        /// <param name="path">
        /// The path to be checked.
        /// </param>
        /// <param name="fileNameOnly">
        /// Non-zero to treat the path as a file name, using the set of
        /// characters that are invalid within a file name; otherwise, the set
        /// of characters that are invalid within a full path is used.
        /// </param>
        /// <param name="allowExtended">
        /// Non-zero to permit and skip over a leading extended-length path
        /// prefix when the path is not being treated as a file name only.
        /// </param>
        /// <param name="useComponents">
        /// Non-zero to additionally validate the path by splitting it into its
        /// individual components.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to allow a leading drive letter and colon prefix when
        /// validating the path components.
        /// </param>
        /// <returns>
        /// True if the path is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value contains any of
        /// the characters that are treated as path wildcards.
        /// </summary>
        /// <param name="value">
        /// The value to be checked.
        /// </param>
        /// <returns>
        /// True if the value contains a path wildcard character; otherwise,
        /// false.
        /// </returns>
        public static bool HasPathWildcard(
            string value /* in */
            )
        {
            return (value != null) && (PathWildcardChars != null) &&
                (value.IndexOfAny(PathWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method cleans the specified path by removing any surrounding
        /// double quotes and, optionally, removing or replacing all invalid
        /// path characters.
        /// </summary>
        /// <param name="path">
        /// The path to be cleaned.
        /// </param>
        /// <param name="full">
        /// Non-zero to perform full cleaning, removing or replacing all
        /// invalid path characters in addition to the surrounding double
        /// quotes.
        /// </param>
        /// <param name="invalidChar">
        /// When non-null, each invalid path character is replaced with this
        /// character during full cleaning; otherwise, each invalid path
        /// character is removed.
        /// </param>
        /// <returns>
        /// The cleaned path.
        /// </returns>
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
                    StringBuilder builder = StringBuilderFactory.Create(result);

                    foreach (char character in Path.GetInvalidPathChars())
                    {
                        if (invalidChar != null)
                            builder.Replace(character, (char)invalidChar);
                        else
                            builder.Replace(character.ToString(), null);
                    }

                    result = StringBuilderCache.GetStringAndRelease(ref builder);
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
        /// <summary>
        /// This method determines whether the ASP.NET runtime is using the
        /// integrated request processing pipeline.
        /// </summary>
        /// <returns>
        /// True if the integrated pipeline is in use; otherwise, false.
        /// </returns>
        private static bool HttpRuntimeUsingIntegratedPipeline()
        {
            return HttpRuntime.UsingIntegratedPipeline;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB && !NET_STANDARD_20
        /// <summary>
        /// This method determines whether there is a current ASP.NET HTTP
        /// context available.
        /// </summary>
        /// <returns>
        /// True if a current HTTP context is available; otherwise, false.
        /// </returns>
        private static bool HaveHttpContext()
        {
            HttpContext context = null;

            return HaveHttpContext(ref context);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether there is a current ASP.NET HTTP
        /// context available, returning that context.
        /// </summary>
        /// <param name="context">
        /// Upon success, this is set to the current HTTP context.
        /// </param>
        /// <returns>
        /// True if a current HTTP context is available; otherwise, false.
        /// </returns>
        private static bool HaveHttpContext(
            ref HttpContext context /* out */
            )
        {
            context = HttpContext.Current;

            return (context != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified ASP.NET HTTP context
        /// has an associated server utility object, returning that object.
        /// </summary>
        /// <param name="context">
        /// The HTTP context from which the server utility object is to be
        /// obtained.
        /// </param>
        /// <param name="server">
        /// Upon success, this is set to the server utility object associated
        /// with the HTTP context.
        /// </param>
        /// <returns>
        /// True if the server utility object is available; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified ASP.NET HTTP context
        /// has an associated request object, returning that object.
        /// </summary>
        /// <param name="context">
        /// The HTTP context from which the request object is to be obtained.
        /// </param>
        /// <param name="request">
        /// Upon success, this is set to the request object associated with the
        /// HTTP context.
        /// </param>
        /// <returns>
        /// True if the request object is available; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the machine name of the server associated with
        /// the current ASP.NET HTTP context, if any.
        /// </summary>
        /// <returns>
        /// The server machine name, or null if no current HTTP context or
        /// server utility object is available.
        /// </returns>
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

        /// <summary>
        /// This method determines the directory containing the binary files
        /// for the running application.  If a manual override has been set, it
        /// is used; otherwise, the path is derived from the hosting web
        /// application (when applicable) or from the base directory of the
        /// current application domain.
        /// </summary>
        /// <param name="full">
        /// Non-zero to resolve the resulting path to a fully-qualified,
        /// absolute path.
        /// </param>
        /// <returns>
        /// The binary path, or null if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method appends the name of the current processor architecture
        /// to the specified base path, producing a processor-specific
        /// sub-directory path.
        /// </summary>
        /// <param name="path">
        /// The base path to which the processor name should be appended.
        /// </param>
        /// <param name="alternateName">
        /// Non-zero to use the alternate processor name instead of the primary
        /// processor name.
        /// </param>
        /// <returns>
        /// The processor-specific path, or the original path verbatim if it is
        /// null or empty or no processor name is available.
        /// </returns>
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
        /// <summary>
        /// This method queries one of the various well-known paths used by the
        /// Eagle runtime, as selected by the specified path type flags, and
        /// optionally appends the processor name and/or fully resolves the
        /// result to an absolute path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="infoPathType">
        /// The flags that select which path to query and how it should be
        /// processed (e.g. whether to fully resolve it, use the local library
        /// location, omit the processor name, or use the alternate processor
        /// name).
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the resolved path.  Upon failure, this
        /// contains an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method determines the path to the native library directory
        /// used by the Eagle runtime.  On Unix-like platforms this is derived
        /// from the well-known system library locations; otherwise, it is
        /// derived from the binary path.
        /// </summary>
        /// <param name="local">
        /// Non-zero to use the local library location (e.g. the per-user local
        /// library directory) on platforms that support it.
        /// </param>
        /// <param name="noProcessor">
        /// Non-zero to omit the processor-specific sub-directory from the
        /// resulting path.
        /// </param>
        /// <param name="alternateName">
        /// Non-zero to use the alternate processor name when appending the
        /// processor-specific sub-directory.
        /// </param>
        /// <returns>
        /// The native library path.
        /// </returns>
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

        /// <summary>
        /// This method determines the directory containing the main module
        /// (i.e. the primary executable) of the current process.
        /// </summary>
        /// <param name="full">
        /// Non-zero to resolve the main module file name to a fully-qualified,
        /// absolute path before extracting its directory.
        /// </param>
        /// <returns>
        /// The directory of the current process main module, or null if it
        /// cannot be determined.
        /// </returns>
        private static string GetProcessMainModulePath(
            bool full /* in */
            )
        {
            return Path.GetDirectoryName(
                GetProcessMainModuleFileName(full));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the file name of the main module (i.e. the
        /// primary executable) of the current process.
        /// </summary>
        /// <param name="full">
        /// Non-zero to resolve the resulting file name to a fully-qualified,
        /// absolute path.
        /// </param>
        /// <returns>
        /// The file name of the current process main module, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetProcessMainModuleFileName(
            bool full /* in */
            )
        {
            return GetProcessMainModuleFileName(
                ProcessOps.GetCurrent(), full);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the file name of the main module (i.e. the
        /// primary executable) of the specified process.
        /// </summary>
        /// <param name="process">
        /// The process whose main module file name is to be determined.  This
        /// parameter may be null.
        /// </param>
        /// <param name="full">
        /// Non-zero to resolve the resulting file name to a fully-qualified,
        /// absolute path.
        /// </param>
        /// <returns>
        /// The file name of the specified process main module, or null if it
        /// cannot be determined.
        /// </returns>
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
        /// <summary>
        /// This method resolves the file name of a loaded native module.  It
        /// first attempts the primary mechanism (which works on Windows) and,
        /// failing that, falls back to alternative mechanisms that may work on
        /// other platforms, optionally using an exported function name as the
        /// basis for the lookup.
        /// </summary>
        /// <param name="module">
        /// The native module handle whose file name is to be resolved.
        /// </param>
        /// <param name="functionName">
        /// The name of an exported function that may be used as the basis for
        /// locating the module file name when the primary mechanism fails.
        /// This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains error information explaining why the
        /// module file name could not be resolved.
        /// </param>
        /// <returns>
        /// The resolved native module file name, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetNativeModuleFileName(
            IntPtr module,       /* in */
            string functionName, /* in: OPTIONAL */
            ref Result error     /* out */
            )
        {
            IntPtr outBuffer = IntPtr.Zero;

            try
            {
                //
                // HACK: First, attempt to the primary method,
                //       which should work on Windows.
                //
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
                    //       have been "truncated").
                    //
                    return Marshal.PtrToStringAuto(
                        outBuffer, (int)result); /* SUCCESS */
                }

                int lastError; /* REUSED */
                ResultList errors = null;

                lastError = Marshal.GetLastWin32Error();

                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "cannot resolve module file name, " +
                    "GetModuleFileName({1}) failed with " +
                    "error {0}: {2}", lastError, module,
                    NativeOps.GetDynamicLoadingError(
                        lastError)));

                //
                // HACK: Fallback to the alternative method(s),
                //       which may work on Linux, etc.  First,
                //       just attempt to use the "module handle"
                //       itself to locate the module file name.
                //       Otherwise, optionally lookup an exported
                //       function specified by the caller, if any,
                //       and then attempt to use that as the basis
                //       for locating the module file name.  This
                //       appears to be necessary (sometimes?) in
                //       order for the underlying dladdr() POSIX
                //       API to work correctly.
                //
                string fileName = NativeOps.GetModuleFileName(
                    module);

                if (fileName != null)
                    return fileName; /* SUCCESS */

                if (functionName != null)
                {
                    IntPtr address = NativeOps.GetProcAddress(
                        module, functionName, out lastError);

                    if (address != IntPtr.Zero)
                    {
                        fileName = NativeOps.GetModuleFileName(
                            address);

                        if (fileName != null)
                            return fileName; /* SUCCESS */

                        lastError = Marshal.GetLastWin32Error();

                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "cannot resolve module file name, " +
                            "GetModuleFileName({1}) failed with " +
                            "error {0}: {2}", lastError, address,
                            NativeOps.GetDynamicLoadingError(
                                lastError)));
                    }
                    else
                    {
                        lastError = Marshal.GetLastWin32Error();

                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "cannot resolve module function, " +
                            "GetProcAddress({1}, {2}) failed " +
                            "with error {0}: {3}", lastError,
                            module, FormatOps.WrapOrNull(
                                functionName),
                            NativeOps.GetDynamicLoadingError(
                                lastError)));
                    }
                }

                if (errors != null)
                    error = errors;

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

        /// <summary>
        /// This method resolves the file name of the native executable for the
        /// current process by querying the main module via the native module
        /// file name resolution mechanism.
        /// </summary>
        /// <returns>
        /// The native executable file name, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetNativeExecutableName()
        {
            Result error = null;

            return GetNativeModuleFileName(IntPtr.Zero, null, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the file name of the managed executable for
        /// the current process, preferring the entry assembly location and
        /// falling back to the main module file name of the current process.
        /// </summary>
        /// <returns>
        /// The managed executable file name, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetManagedExecutableName()
        {
            return GetManagedExecutableName(
                ProcessOps.GetCurrent(), true, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the file name of the managed executable for
        /// the specified process, preferring the entry assembly location and
        /// optionally falling back to the main module file name of the
        /// specified process.
        /// </summary>
        /// <param name="process">
        /// The process to use when falling back to the main module file name.
        /// This parameter may be null.
        /// </param>
        /// <param name="fallback">
        /// Non-zero to fall back to the main module file name of the specified
        /// process when the entry assembly location is not available.
        /// </param>
        /// <param name="full">
        /// Non-zero to resolve the fallback file name to a fully-qualified,
        /// absolute path.
        /// </param>
        /// <returns>
        /// The managed executable file name, or null if it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method generates a random file name (without any directory
        /// component), optionally using the specified prefix.  When a prefix is
        /// supplied, it is validated against the identifier pattern and a
        /// fail-safe fallback prefix is substituted if it is invalid.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to prepend to the random file name.  This parameter may
        /// be null.
        /// </param>
        /// <param name="fileNameOnly">
        /// Upon return, this contains the generated random file name, without
        /// any directory component.
        /// </param>
        private static void GetRandomFileName(
            string prefix,          /* in: OPTIONAL */
            out string fileNameOnly /* out */
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

            if (prefix != null)
            {
                Regex regEx = identifierRegEx;

                if (regEx != null)
                {
                    Match match = regEx.Match(prefix);

                    if ((match != null) && match.Success)
                    {
                        builder.Append(prefix);
                        goto suffix;
                    }
                }

                //
                // HACK: *SECURITY* This is fail-safe fallback
                //       handling, which will make sure that a
                //       prefix is always added, even if the
                //       specified prefix is invalid or cannot
                //       be validated.
                //
                builder.Append(
                    "egrfn_"); /* Eagle Get Random File Name */

                goto suffix; /* REDUNDANT */
            }

        suffix:

            builder.Append(Path.GetRandomFileName()); /* throw */

            fileNameOnly = StringBuilderCache.GetStringAndRelease(
                ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the configured temporary sub-path that should be
        /// appended to the temporary directory, if the use of a temporary
        /// sub-path is currently enabled.
        /// </summary>
        /// <returns>
        /// The configured temporary sub-path, or null if the use of a temporary
        /// sub-path is not currently enabled.
        /// </returns>
        private static string GetTempSubPath()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (useTemporarySubPath)
                    return temporarySubPath;

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method configures the temporary sub-path that should be
        /// appended to the temporary directory and, optionally, whether the use
        /// of that temporary sub-path is enabled.
        /// </summary>
        /// <param name="subPath">
        /// The temporary sub-path to configure.  This parameter may be null.
        /// </param>
        /// <param name="enabled">
        /// Non-zero to enable the use of the temporary sub-path, zero to
        /// disable it, or null to leave the enabled state unchanged.
        /// </param>
        private static void SetTempSubPath( /* NOT USED */
            string subPath, /* in: OPTIONAL */
            bool? enabled   /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                temporarySubPath = subPath;

                if (enabled != null)
                    useTemporarySubPath = (bool)enabled;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified temporary sub-path to the
        /// specified rooted path, creating the resulting directory if
        /// necessary.  The path is left unchanged if it is null or empty, not
        /// rooted, or if no sub-path is supplied.
        /// </summary>
        /// <param name="path">
        /// The base path to which the temporary sub-path should be appended.
        /// Upon return, this contains the combined path when a sub-path was
        /// applied; otherwise, it is left unchanged.
        /// </param>
        /// <param name="subPath">
        /// The temporary sub-path to append.  This parameter may be null.
        /// </param>
        private static void ApplyTempSubPath(
            ref string path, /* in, out */
            string subPath   /* in: OPTIONAL */
            )
        {
            if (String.IsNullOrEmpty(path))
                return;

            if (!Path.IsPathRooted(path))
                return;

            if (subPath != null)
            {
                path = Path.Combine(path, subPath);
                Directory.CreateDirectory(path); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the system temporary directory, optionally with
        /// the specified temporary sub-path appended.
        /// </summary>
        /// <param name="subPath">
        /// The temporary sub-path to append to the system temporary directory.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The temporary directory path, or null if it cannot be determined.
        /// </returns>
        private static string GetTempPath(
            string subPath /* in: OPTIONAL */
            )
        {
            try
            {
                string path = Path.GetTempPath();

                ApplyTempSubPath(ref path, subPath);

                return path;
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

        //
        // WARNING: For use by the TestGetTempFileNameCallback
        //          method only.
        //
        /// <summary>
        /// This method determines whether generated temporary file names should
        /// be validated.
        /// </summary>
        /// <returns>
        /// True if temporary file names should be validated; otherwise, false.
        /// </returns>
        public static bool ShouldValidateTempFileName()
        {
            lock (syncRoot)
            {
                return validateTemporaryFileName;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the full path to a temporary file, using the
        /// configured temporary file name callback when one is present or, by
        /// default, combining a random file name with the temporary directory.
        /// When validation is enabled, the resulting path is validated as a
        /// file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to use when generating the random file name.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The full path to a temporary file.
        /// </returns>
        public static string GetTempFileName(
            Interpreter interpreter, /* in: OPTIONAL */
            string prefix            /* in: OPTIONAL */
            )
        {
            GetStringValueCallback callback;
            bool validate;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                callback = getTempFileNameCallback;
                validate = validateTemporaryFileName;
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
                    string fileNameOnly;

                    GetRandomFileName(
                        prefix, out fileNameOnly);

                    result = Path.Combine(
                        GetTempPath(interpreter), fileNameOnly);
                }

                if (validate &&
                    !ValidatePathAsFile(result, true, false))
                {
                    throw new ScriptException(String.Format(
                        "temporary file name failed validation: {0}",
                        FormatOps.WrapOrNull(result)));
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
                TracePriority.PathDebug2);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to determine the temporary directory from the
        /// relevant environment variables, in priority order, returning the
        /// first writable location found.  The categories of environment
        /// variables that are consulted are controlled by the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when verifying that a candidate path
        /// is writable.  This parameter may be null.
        /// </param>
        /// <param name="subPath">
        /// The temporary sub-path to append to the resulting directory.  This
        /// parameter may be null.
        /// </param>
        /// <param name="includeTest">
        /// Non-zero to consult the Eagle test-specific temporary directory
        /// environment variables.
        /// </param>
        /// <param name="includeXdg">
        /// Non-zero to consult the XDG runtime directory environment variable.
        /// </param>
        /// <param name="includeSystem">
        /// Non-zero to consult the system temporary directory environment
        /// variables.
        /// </param>
        /// <returns>
        /// The first writable temporary directory found, or null if none is
        /// available.
        /// </returns>
        private static string GetTempPathViaEnvironment(
            Interpreter interpreter, /* in: OPTIONAL */
            string subPath,          /* in: OPTIONAL */
            bool includeTest,        /* in */
            bool includeXdg,         /* in */
            bool includeSystem       /* in */
            )
        {
            //
            // HACK: Do not use the "system" environment variables that
            //       are (normally?) involved in configuring temporary
            //       directory locations unless the caller explicitly
            //       allows it.
            //
            // WARNING: This may be a breaking change from the previous
            //          beta releases; however, it is seen as necessary
            //          to allow customization options to work correctly.
            //
            foreach (string name in new string[] {
                    includeTest ? EnvVars.EagleTestTemp : null,
                    includeTest ? EnvVars.EagleTemp : null,
                    includeXdg ? EnvVars.XdgRuntimeDir : null,
                    includeSystem ? EnvVars.Temp : null,
                    includeSystem ? EnvVars.Tmp : null
                })
            {
                if (String.IsNullOrEmpty(name))
                    continue;

                string path = CommonOps.Environment.GetVariable(name);

                if (String.IsNullOrEmpty(path))
                    continue;

                bool accessStatus;

                FileOps.VerifyWritable(
                    interpreter, path, out accessStatus);

                if (!accessStatus)
                    continue;

                try
                {
                    ApplyTempSubPath(ref path, subPath);

                    return path;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(PathOps).Name,
                        TracePriority.PathError);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the temporary directory to use, using the
        /// configured temporary path callback when one is present or, by
        /// default, consulting the relevant environment variables and falling
        /// back to the system temporary directory.  Any configured temporary
        /// sub-path is applied to the result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The temporary directory path.
        /// </returns>
        public static string GetTempPath(
            Interpreter interpreter /* in: OPTIONAL */
            ) /* throw */
        {
            GetStringValueCallback callback;
            bool includeTest;
            bool includeXdg;
            bool includeSystem;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                callback = getTempPathCallback;
                includeTest = includeTestTemporaryEnvVars;
                includeXdg = includeXdgTemporaryEnvVars;
                includeSystem = includeSystemTemporaryEnvVars;
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
                    string subPath = GetTempSubPath(); // (?)

                    result = GetTempPathViaEnvironment(
                        interpreter, subPath, includeTest,
                        includeXdg, includeSystem);

                    if (result == null)
                        result = GetTempPath(subPath);
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

        /// <summary>
        /// This method generates a unique file or directory path within the
        /// specified base directory by combining the configured prefix and
        /// suffix with a randomly generated hexadecimal identifier, retrying
        /// until an unused path is found or the maximum number of retries is
        /// exhausted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="directory">
        /// The base directory in which to generate the unique path.  If this
        /// parameter is null or an empty string, the temporary directory is
        /// used instead.
        /// </param>
        /// <param name="prefix">
        /// The prefix to prepend to the generated identifier, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to append to the generated identifier, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that
        /// describes why a unique path could not be generated.
        /// </param>
        /// <returns>
        /// The generated unique path, or null if a unique path could not be
        /// generated.
        /// </returns>
        public static string GetUniquePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string directory,        /* in: OPTIONAL */
            string prefix,           /* in: OPTIONAL */
            string suffix,           /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            if (String.IsNullOrEmpty(directory))
            {
                directory = GetTempPath(interpreter);

                if (String.IsNullOrEmpty(directory))
                {
                    error = "invalid temporary directory";
                    return null;
                }
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

        /// <summary>
        /// This method returns the file name only (i.e. without any directory
        /// information) of the executable file for the current process.
        /// </summary>
        /// <returns>
        /// The file name of the current process executable, or null if it
        /// cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method returns the fully qualified file name of the executable
        /// file for the current process.
        /// </summary>
        /// <returns>
        /// The fully qualified file name of the current process executable, or
        /// null if it cannot be determined.
        /// </returns>
        public static string GetExecutableName()
        {
            return GetExecutableName(
                ProcessOps.GetCurrent(), true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the file name of the main module for the
        /// specified process.
        /// </summary>
        /// <param name="process">
        /// The process whose main module file name is to be returned.
        /// </param>
        /// <param name="full">
        /// Non-zero to return the fully qualified file name; otherwise, the
        /// file name only is returned.
        /// </param>
        /// <returns>
        /// The file name of the main module for the specified process, or null
        /// if it cannot be determined.
        /// </returns>
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
        /// <summary>
        /// This method returns the build configuration (e.g. <c>Debug</c> or
        /// <c>Release</c>) associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose build configuration is to be returned.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The build configuration of the specified assembly, or null if it
        /// cannot be determined.
        /// </returns>
        private static string GetBuildConfiguration( /* MAY RETURN NULL */
            Assembly assembly /* in: OPTIONAL */
            )
        {
            return AttributeOps.GetAssemblyConfiguration(assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value starts with any
        /// of the known build configuration names, including the build
        /// configuration associated with the specified assembly, if any.
        /// </summary>
        /// <param name="value">
        /// The value to examine.  This parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose build configuration should also be considered.
        /// This parameter may be null.
        /// </param>
        /// <param name="length">
        /// Upon success, this parameter receives the length of the matched
        /// build configuration name; otherwise, it receives zero.
        /// </param>
        /// <returns>
        /// True if the specified value starts with a known build configuration
        /// name; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method builds the default (fallback) file name by combining the
        /// specified base file name with the specified file extension.
        /// </summary>
        /// <param name="fileName">
        /// The base file name.  If this parameter is null or an empty string,
        /// null is returned.
        /// </param>
        /// <param name="fileExtension">
        /// The file extension to append to the base file name.
        /// </param>
        /// <returns>
        /// The fallback file name, or null if the base file name is null or an
        /// empty string.
        /// </returns>
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

        /// <summary>
        /// This method builds the web-specific fallback file name (i.e.
        /// <c>Web</c> combined with the specified file extension) located within
        /// the same directory as the specified base file name.
        /// </summary>
        /// <param name="fileName">
        /// The base file name used to determine the target directory.  If this
        /// parameter is null or an empty string, null is returned.
        /// </param>
        /// <param name="fileExtension">
        /// The file extension to append to the web fallback file name.
        /// </param>
        /// <returns>
        /// The web fallback file name, or null if the base file name is null or
        /// an empty string.
        /// </returns>
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

        /// <summary>
        /// This method determines whether any of the local name environment
        /// variables (e.g. user name, computer name, and user domain) are
        /// present in the environment.
        /// </summary>
        /// <param name="perUser">
        /// Non-zero to also consider the per-user environment variables (i.e.
        /// the user name and user domain); otherwise, only the computer name is
        /// considered.
        /// </param>
        /// <returns>
        /// True if at least one of the applicable local name environment
        /// variables is present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method obtains the local user name, machine name, and domain
        /// name, using either the built-in runtime values or the corresponding
        /// environment variables.
        /// </summary>
        /// <param name="perUser">
        /// Non-zero to consider the per-user environment variables when
        /// deciding whether the built-in values should be used.
        /// </param>
        /// <param name="forceBuiltIn">
        /// Non-zero to force the use of the built-in runtime values, zero to
        /// force the use of the environment variables, or null to decide
        /// automatically based on the operating system and environment.
        /// </param>
        /// <param name="userName">
        /// Upon return, this parameter receives the local user name.
        /// </param>
        /// <param name="machineName">
        /// Upon return, this parameter receives the local machine name.
        /// </param>
        /// <param name="domainName">
        /// Upon return, this parameter receives the local domain name.
        /// </param>
        /// <returns>
        /// Non-zero if the built-in runtime values were used, zero if the
        /// environment variables were used, or null if no local names could be
        /// obtained.
        /// </returns>
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

        /// <summary>
        /// This method builds the ordered list of candidate override file names
        /// for the specified base file name, incorporating the local user name,
        /// machine name, and domain name in their various combinations and,
        /// optionally, the default and web fallback file names.
        /// </summary>
        /// <param name="fileName">
        /// The base file name.  If this parameter is null or an empty string,
        /// null is returned.
        /// </param>
        /// <param name="fileExtension">
        /// The file extension to append to each candidate file name.
        /// </param>
        /// <param name="includeFallback">
        /// Non-zero to include the default (fallback) file name as the last
        /// entry in the returned list.
        /// </param>
        /// <param name="includeWeb">
        /// Non-zero to also include the web fallback file name when the default
        /// fallback file name is included.
        /// </param>
        /// <returns>
        /// The ordered list of candidate override file names, or null if the
        /// base file name is null or an empty string.
        /// </returns>
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

        /// <summary>
        /// This method determines the base path for the specified path,
        /// accounting for the build configuration of the specified assembly and
        /// various well-known directory layouts.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose build configuration should be considered.  This
        /// parameter may be null.
        /// </param>
        /// <param name="path">
        /// The path for which the base path is to be determined.
        /// </param>
        /// <returns>
        /// The base path, or null if it cannot be determined.
        /// </returns>
        public static string GetBasePath( /* MAY RETURN NULL */
            Assembly assembly, /* in: OPTIONAL */
            string path        /* in */
            )
        {
            string suffix = null;

            return GetBasePathAndSuffix(assembly, path, ref suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the base suffix (i.e. the portion of the path
        /// that follows the base path) for the binary path associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose build configuration should be considered.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The base suffix, or null if it cannot be determined.
        /// </returns>
        public static string GetBaseSuffix( /* MAY RETURN NULL */
            Assembly assembly /* in: OPTIONAL */
            )
        {
            return GetBaseSuffix(
                assembly, GlobalState.InitializeOrGetBinaryPath(false));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the base suffix (i.e. the portion of the path
        /// that follows the base path) for the specified path, accounting for
        /// the build configuration of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose build configuration should be considered.  This
        /// parameter may be null.
        /// </param>
        /// <param name="path">
        /// The path for which the base suffix is to be determined.
        /// </param>
        /// <returns>
        /// The base suffix, or null if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path refers to the root
        /// directory of its drive or volume.
        /// </summary>
        /// <param name="path">
        /// The path to examine.  If this parameter is null or an empty string,
        /// false is returned.
        /// </param>
        /// <returns>
        /// True if the specified path is a root path; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method conditionally adjusts the specified path to handle
        /// certain corner cases prior to base path computation, such as running
        /// from the special build tasks output directory or a target framework
        /// specific directory (e.g. <c>netstandard2.0</c>).
        /// </summary>
        /// <param name="path">
        /// The path to possibly mutate.  Upon return, this parameter may
        /// contain a modified path.
        /// </param>
        /// <returns>
        /// True if the specified path was modified by this method; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method conditionally removes the trailing <c>bin</c> directory
        /// from the specified path, subject to the <c>StrictBasePath</c>
        /// environment variable, unless the specified path is a root path.
        /// </summary>
        /// <param name="path">
        /// The path to possibly mutate.  Upon return, this parameter may
        /// contain a modified path.
        /// </param>
        /// <returns>
        /// True if the specified path was modified by this method; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method determines both the base path and the base suffix for
        /// the specified path, accounting for the build configuration of the
        /// specified assembly and various well-known directory layouts (e.g.
        /// source tree, build output, and <c>bin</c> directories).
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose build configuration should be considered.  This
        /// parameter may be null.
        /// </param>
        /// <param name="path">
        /// The path for which the base path and suffix are to be determined.
        /// </param>
        /// <param name="suffix">
        /// Upon return, this parameter receives the base suffix (i.e. the
        /// portion of the path that follows the base path), if any.
        /// </param>
        /// <returns>
        /// The base path, or null if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method splits the specified path into its component parts using
        /// the available directory separator characters, falling back to a
        /// single-element array containing the original path when the separator
        /// characters cannot be determined.
        /// </summary>
        /// <param name="path">
        /// The path to split.  This parameter cannot be null.
        /// </param>
        /// <returns>
        /// An array containing the component parts of the specified path.
        /// </returns>
        public static string[] MaybeSplit(
            string path /* in: CANNOT BE NULL */
            )
        {
            return MaybeSplit(path, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified path into its component parts using
        /// the available directory separator characters.
        /// </summary>
        /// <param name="path">
        /// The path to split.  This parameter cannot be null.
        /// </param>
        /// <param name="fallback">
        /// Non-zero to return a single-element array containing the original
        /// path when the directory separator characters cannot be determined;
        /// otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// An array containing the component parts of the specified path, or
        /// null if the directory separator characters cannot be determined and
        /// fallback behavior is not requested.
        /// </returns>
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

        /// <summary>
        /// This method trims the trailing directory separator characters from
        /// the specified path, when the directory separator characters can be
        /// determined.
        /// </summary>
        /// <param name="path">
        /// The path to trim.  This parameter cannot be null.
        /// </param>
        /// <returns>
        /// The trimmed path, or the original path when the directory separator
        /// characters cannot be determined.
        /// </returns>
        public static string MaybeTrim(
            string path /* in: CANNOT BE NULL */
            )
        {
            return MaybeTrim(path, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method trims the directory separator characters from the
        /// specified path, when the directory separator characters can be
        /// determined.
        /// </summary>
        /// <param name="path">
        /// The path to trim.  This parameter cannot be null.
        /// </param>
        /// <param name="both">
        /// Selects which set of directory separator characters is used when
        /// trimming; this value is passed through to the routine that resolves
        /// the directory separator characters.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trimmed path, or the original path when the directory separator
        /// characters cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method trims the leading directory separator characters from
        /// the specified path, when the directory separator characters can be
        /// determined.
        /// </summary>
        /// <param name="path">
        /// The path to trim.  This parameter cannot be null.
        /// </param>
        /// <returns>
        /// The trimmed path, or the original path when the directory separator
        /// characters cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method trims the trailing directory separator characters from
        /// the specified path, when the directory separator characters can be
        /// determined.
        /// </summary>
        /// <param name="path">
        /// The path to trim.  This parameter cannot be null.
        /// </param>
        /// <returns>
        /// The trimmed path, or the original path when the directory separator
        /// characters cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method trims the trailing directory separator characters from
        /// the end of the specified path and, optionally, appends a single
        /// separator character in their place.  The path is returned unchanged
        /// when it is null, empty, a single character, or does not end with a
        /// directory separator character.
        /// </summary>
        /// <param name="path">
        /// The path to trim.
        /// </param>
        /// <param name="separator">
        /// The separator character to append to the trimmed path, or null to
        /// append nothing.
        /// </param>
        /// <returns>
        /// The trimmed path, with the specified separator character appended
        /// when applicable.
        /// </returns>
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

        /// <summary>
        /// This method splits the specified path string into its component
        /// parts, using the directory separator character indicated by the
        /// caller, or the first directory separator character found within the
        /// path when none is specified.  Leading empty parts are collapsed into
        /// a single separator entry.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the path itself.
        /// </param>
        /// <param name="path">
        /// The path to split.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The list of path components, an empty list when the path is empty,
        /// or null when the path is null.
        /// </returns>
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

        /// <summary>
        /// This method combines the elements of the specified list into a
        /// single path string, using the directory separator character
        /// indicated by the caller, or the first directory separator character
        /// found within the list when none is specified.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the list itself.
        /// </param>
        /// <param name="list">
        /// The list of path components to combine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The combined path string.
        /// </returns>
        private static string CombinePath(
            bool? unix,        /* in */
            IList<string> list /* in */
            )
        {
            return CombinePath(
                unix, list, Index.Invalid, Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the elements within the specified range of the
        /// specified list into a single path string, using the directory
        /// separator character indicated by the caller, or the first directory
        /// separator character found within the list when none is specified.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the list itself.
        /// </param>
        /// <param name="list">
        /// The list of path components to combine.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first list element to combine, or
        /// <see cref="Index.Invalid" /> to start with the first element.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last list element to combine, or
        /// <see cref="Index.Invalid" /> to stop with the last element.
        /// </param>
        /// <returns>
        /// The combined path string.
        /// </returns>
        private static string CombinePath(
            bool? unix,         /* in */
            IList<string> list, /* in */
            int startIndex,     /* in */
            int stopIndex       /* in */
            )
        {
            return CombinePath(
                unix, list as IList, startIndex, stopIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the elements of the specified list into a
        /// single path string, using the directory separator character
        /// indicated by the caller, or the first directory separator character
        /// found within the list when none is specified.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the list itself.
        /// </param>
        /// <param name="list">
        /// The list of path components to combine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The combined path string.
        /// </returns>
        public static string CombinePath(
            bool? unix, /* in */
            IList list  /* in */
            )
        {
            return CombinePath(
                unix, list, Index.Invalid, Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the elements within the specified range of the
        /// specified list into a single path string, using the directory
        /// separator character indicated by the caller, or the first directory
        /// separator character found within the list when none is specified.
        /// Null and empty parts are skipped and surrounding whitespace is
        /// trimmed.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the list itself.
        /// </param>
        /// <param name="list">
        /// The list of path components to combine.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first list element to combine, or
        /// <see cref="Index.Invalid" /> to start with the first element.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last list element to combine, or
        /// <see cref="Index.Invalid" /> to stop with the last element.
        /// </param>
        /// <returns>
        /// The combined path string.
        /// </returns>
        private static string CombinePath(
            bool? unix,     /* in */
            IList list,     /* in */
            int startIndex, /* in */
            int stopIndex   /* in */
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

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

                                builder.Append(MaybeTrim(trimPath, true));
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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of paths from the environment variables
        /// with the specified names, splitting the value of each environment
        /// variable on the platform path separator character.  Each resulting
        /// entry pairs the originating environment variable name with one of
        /// its path values.
        /// </summary>
        /// <param name="names">
        /// The environment variable names to query.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The list of environment variable name and path value pairs.
        /// </returns>
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

        /// <summary>
        /// This method combines the specified path components into a single
        /// path string, using the directory separator character indicated by
        /// the caller, or the first directory separator character found within
        /// the components when none is specified.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the components themselves.
        /// </param>
        /// <param name="paths">
        /// The array of path components to combine.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The combined path string.
        /// </returns>
        public static string CombinePath(
            bool? unix,           /* in */
            params string[] paths /* in */
            )
        {
            return CombinePath(unix, (IList)paths);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the path components within the specified range
        /// of the specified array into a single path string, using the
        /// directory separator character indicated by the caller, or the first
        /// directory separator character found within the components when none
        /// is specified.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use the Unix directory separator character, zero to use
        /// the Windows directory separator character, or null to detect the
        /// separator character from the components themselves.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first path component to combine, or
        /// <see cref="Index.Invalid" /> to start with the first component.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last path component to combine, or
        /// <see cref="Index.Invalid" /> to stop with the last component.
        /// </param>
        /// <param name="paths">
        /// The array of path components to combine.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The combined path string.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified character is a
        /// directory separator character.
        /// </summary>
        /// <param name="character">
        /// The character to check.
        /// </param>
        /// <returns>
        /// True if the character is a directory separator character; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines the <see cref="PathType" /> of the specified
        /// path, treating a null or empty path as
        /// <see cref="PathType.Relative" />.
        /// </summary>
        /// <param name="path">
        /// The path to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The <see cref="PathType" /> of the path.
        /// </returns>
        public static PathType GetPathType(
            string path /* in */
            )
        {
            return GetPathType(path, PathType.Relative);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the <see cref="PathType" /> of the specified
        /// path.  On Windows, paths beginning with a directory separator
        /// character or a drive letter without a following separator are
        /// treated as volume relative.
        /// </summary>
        /// <param name="path">
        /// The path to examine.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The <see cref="PathType" /> to return when the path is null or
        /// empty.
        /// </param>
        /// <returns>
        /// The <see cref="PathType" /> of the path.
        /// </returns>
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

        /// <summary>
        /// This method computes a hash code for the specified path, normalizing
        /// it to its native form and, when path comparisons are case
        /// insensitive, to lower case beforehand.
        /// </summary>
        /// <param name="path">
        /// The path to hash.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The hash code for the path, or zero when the path is null.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path contains a
        /// directory separator character.
        /// </summary>
        /// <param name="path">
        /// The path to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the path contains a directory separator character;
        /// otherwise, false.
        /// </returns>
        public static bool HasDirectory(
            string path /* in */
            )
        {
            int index = Index.Invalid;

            return StartsWithDirectory(path, ref index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the extension, if any, from the specified path
        /// by removing everything from the final period character onward.
        /// </summary>
        /// <param name="path">
        /// The path from which to remove the extension.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The path without its extension, or the original path when it is
        /// null, empty, or contains no period character.
        /// </returns>
        public static string RemoveExtension(
            string path /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            int index = path.LastIndexOf(Characters.Period);

            if (index == Index.Invalid)
                return path;

            return path.Substring(0, index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the extension, if any, of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path from which to extract the extension.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The extension of the path, including its leading period character,
        /// or null when the path has no extension or an error is encountered.
        /// </returns>
        public static string GetExtension(
            string path /* in */
            )
        {
            try
            {
                return Path.GetExtension(path); /* throw */
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file name might refer
        /// to a bundle file, based on whether its extension matches the
        /// database file extension.
        /// </summary>
        /// <param name="fileName">
        /// The file name to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the file name might refer to a bundle file; otherwise,
        /// false.
        /// </returns>
        public static bool MightBeBundleFile(
            string fileName /* in */
            )
        {
            if (SharedStringOps.Equals(
                    GetExtension(fileName), FileExtension.Database,
                    ComparisonType))
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
        /// This method determines whether the specified path has an extension.
        /// </summary>
        /// <param name="path">
        /// The path to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the path has an extension; otherwise, false.
        /// </returns>
        public static bool HasExtension(
            string path /* in */
            )
        {
            string extension;

            return HasExtension(path, out extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path has an extension,
        /// also returning the extension itself.
        /// </summary>
        /// <param name="path">
        /// The path to check.  This parameter may be null.
        /// </param>
        /// <param name="extension">
        /// Upon return, receives the extension of the path, including its
        /// leading period character, or null when the path has no extension.
        /// </param>
        /// <returns>
        /// True if the path has an extension; otherwise, false.
        /// </returns>
        private static bool HasExtension(
            string path,         /* in */
            out string extension /* out */
            )
        {
            extension = GetExtension(path);

            return !String.IsNullOrEmpty(extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path has an extension
        /// that is in the list of well-known file extensions.
        /// </summary>
        /// <param name="path">
        /// The path to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the path has a well-known extension; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the extension of the specified path
        /// matches the specified extension.
        /// </summary>
        /// <param name="path">
        /// The path whose extension is to be compared.  This parameter may be
        /// null.
        /// </param>
        /// <param name="extension">
        /// The extension to compare against.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the path has an extension and it matches the specified
        /// extension; otherwise, false.
        /// </returns>
        public static bool MatchExtension(
            string path,
            string extension
            )
        {
            string localExtension;

            if (!HasExtension(path, out localExtension))
                return false;

            if (localExtension == null)
                return false;

            if (extension == null)
                return false;

            return IsEqualParts(extension, localExtension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path contains a
        /// directory separator character, returning the index of the first one
        /// found.
        /// </summary>
        /// <param name="path">
        /// The path to check.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// Upon success, receives the index of the first directory separator
        /// character found within the path.  This parameter is left unchanged
        /// upon failure.
        /// </param>
        /// <returns>
        /// True if the path contains a directory separator character;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path contains a
        /// directory separator character, returning the index of the last one
        /// found.
        /// </summary>
        /// <param name="path">
        /// The path to check.  This parameter may be null.
        /// </param>
        /// <param name="index">
        /// Upon success, receives the index of the last directory separator
        /// character found within the path.  This parameter is left unchanged
        /// upon failure.
        /// </param>
        /// <returns>
        /// True if the path contains a directory separator character;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method adds the specified path to the specified dictionary,
        /// creating the dictionary when necessary and ignoring null, empty, or
        /// duplicate paths.
        /// </summary>
        /// <param name="path">
        /// The path to add.  This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary to which the path is added.  When null, a new
        /// dictionary is created and returned via this parameter.
        /// </param>
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

        /// <summary>
        /// This method adds each of the specified paths to the specified
        /// dictionary, creating the dictionary when necessary and ignoring
        /// null, empty, or duplicate paths.
        /// </summary>
        /// <param name="paths">
        /// The paths to add.  This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary to which the paths are added.  When null, a new
        /// dictionary is created and returned via this parameter.
        /// </param>
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

        /// <summary>
        /// This method queries the per-path variable mappings configured within
        /// the specified interpreter for the specified path, its directory
        /// name, and its file name, adding any resulting mapped paths to the
        /// specified dictionary.  Path mappings are always ignored for safe
        /// interpreters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose path mappings are queried.  This parameter may
        /// be null.
        /// </param>
        /// <param name="path">
        /// The path whose mappings are queried.  This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary to which any mapped paths are added.  When null, a
        /// new dictionary is created and returned via this parameter.
        /// </param>
        /// <returns>
        /// True if the interpreter could be used to query path mappings;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method queries the Tcl auto-source-path variable within the
        /// specified interpreter and adds any resulting paths to the specified
        /// dictionary.  The auto-source-path is always ignored for safe
        /// interpreters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose auto-source-path is queried.  This parameter
        /// may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary to which any auto-source paths are added.  When null,
        /// a new dictionary is created and returned via this parameter.
        /// </param>
        /// <returns>
        /// True if the interpreter could be used to query the auto-source-path;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method extracts the individual boolean options represented by
        /// the specified <see cref="FileSearchFlags" /> value into separate
        /// output parameters.
        /// </summary>
        /// <param name="fileSearchFlags">
        /// The file search flags to extract.
        /// </param>
        /// <param name="specificPath">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.SpecificPath" /> flag is set.
        /// </param>
        /// <param name="mapped">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Mapped" /> flag is set.
        /// </param>
        /// <param name="autoSourcePath">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.AutoSourcePath" /> flag is set.
        /// </param>
        /// <param name="current">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Current" /> flag is set.
        /// </param>
        /// <param name="user">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.User" /> flag is set.
        /// </param>
        /// <param name="externals">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Externals" /> flag is set.
        /// </param>
        /// <param name="application">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Application" /> flag is set.
        /// </param>
        /// <param name="applicationBase">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.ApplicationBase" /> flag is set.
        /// </param>
        /// <param name="vendor">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Vendor" /> flag is set.
        /// </param>
        /// <param name="nullOnNotFound">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.NullOnNotFound" /> flag is set.
        /// </param>
        /// <param name="directoryLocation">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.DirectoryLocation" /> flag is set.
        /// </param>
        /// <param name="fileLocation">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.FileLocation" /> flag is set.
        /// </param>
        /// <param name="fullPath">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.FullPath" /> flag is set.
        /// </param>
        /// <param name="stripBasePath">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.StripBasePath" /> flag is set.
        /// </param>
        /// <param name="tailOnly">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.TailOnly" /> flag is set.
        /// </param>
        /// <param name="verbose">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Verbose" /> flag is set.
        /// </param>
        /// <param name="isolated">
        /// Upon return, non-zero when the
        /// <see cref="FileSearchFlags.Isolated" /> flag is set.
        /// </param>
        /// <param name="unix">
        /// Upon return, non-zero when the directory separator should be the
        /// Unix separator, zero when it should be the Windows separator, or
        /// null when no directory separator preference is specified.
        /// </param>
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

        /// <summary>
        /// This method returns a human-readable description of the file search
        /// mode, for use in trace and error messages.
        /// </summary>
        /// <param name="isolated">
        /// Non-zero when the search is performed in isolated mode, zero when it
        /// is performed in standard mode.
        /// </param>
        /// <returns>
        /// A string describing the search mode.
        /// </returns>
        private static string GetSearchMode(
            bool isolated
            )
        {
            return String.Format(
                " in {0} mode", isolated ? "isolated" : "standard");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the file system path of the specified special
        /// folder, returning null when the path cannot be determined.
        /// </summary>
        /// <param name="folder">
        /// The <see cref="Environment.SpecialFolder" /> whose path is to be
        /// returned.
        /// </param>
        /// <returns>
        /// The path of the special folder, or null when it cannot be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method returns the configured override path for the specified
        /// special folder, falling back to the actual path of the special
        /// folder when no override has been configured.
        /// </summary>
        /// <param name="folder">
        /// The <see cref="Environment.SpecialFolder" /> whose path is to be
        /// returned.
        /// </param>
        /// <returns>
        /// The configured override path, or the actual path of the special
        /// folder when no override is configured.
        /// </returns>
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

        /// <summary>
        /// This method returns the current working directory of the process.
        /// </summary>
        /// <returns>
        /// The current working directory, or null if it cannot be queried.
        /// </returns>
        public static string GetCurrentDirectory()
        {
            try
            {
                return Directory.GetCurrentDirectory(); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds various categories of file system paths to the
        /// specified path dictionary, based on the supplied flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="path">
        /// The base path to consider when adding specific and mapped paths.
        /// </param>
        /// <param name="specificPath">
        /// Non-zero to add <paramref name="path" /> itself when it is fully
        /// rooted.
        /// </param>
        /// <param name="mapped">
        /// Non-zero to add any mapped paths associated with
        /// <paramref name="path" />.
        /// </param>
        /// <param name="autoSourcePath">
        /// Non-zero to add the configured auto-source paths.
        /// </param>
        /// <param name="current">
        /// Non-zero to add the current working directory.
        /// </param>
        /// <param name="user">
        /// Non-zero to add the various user-specific directories.
        /// </param>
        /// <param name="externals">
        /// Non-zero to add the externals directory.
        /// </param>
        /// <param name="application">
        /// Non-zero to add the various application-specific directories.
        /// </param>
        /// <param name="applicationBase">
        /// Non-zero to add the various application base directories.
        /// </param>
        /// <param name="dictionary">
        /// Upon entry, the path dictionary to populate; it is created if null.
        /// Upon return, it contains the added paths.
        /// </param>
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
                AddPathToDictionary(
                    GetCurrentDirectory(), ref dictionary);
            }

            if (user)
            {
                AddPathsToDictionary(GetHomeDirectories(
                    HomeFlags.AnyDataMask), ref dictionary);

                string documentDirectory = GetDocumentDirectory(false);

                if (!String.IsNullOrEmpty(documentDirectory))
                {
                    AddPathToDictionary(Path.Combine(
                        documentDirectory, GlobalState.GetPackageName()),
                        ref dictionary);

                    AddPathToDictionary(
                        documentDirectory, ref dictionary);
                }

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

        /// <summary>
        /// This method returns the specified path with any leading drive
        /// letter and colon removed.
        /// </summary>
        /// <param name="path">
        /// The path to examine.
        /// </param>
        /// <returns>
        /// The path with the leading drive letter and colon removed, or null
        /// if it does not begin with a drive letter and colon.
        /// </returns>
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

        /// <summary>
        /// This method removes the specified base path prefix from the start
        /// of the specified path, when present.
        /// </summary>
        /// <param name="path">
        /// The path to process. This parameter is optional and may be null.
        /// </param>
        /// <param name="basePath">
        /// The base path prefix to remove. This parameter is optional and may
        /// be null.
        /// </param>
        /// <param name="default">
        /// The value to return when the base path cannot be removed. This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="separator">
        /// Non-zero to trim any leading directory separator from the result.
        /// </param>
        /// <returns>
        /// The path with the base path prefix removed, or
        /// <paramref name="default" /> when the prefix is absent or cannot be
        /// removed.
        /// </returns>
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

        /// <summary>
        /// This method computes the candidate file names to be used when
        /// searching for the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to search for.
        /// </param>
        /// <param name="basePath">
        /// The base path used to strip an absolute path prefix, when enabled.
        /// </param>
        /// <param name="fullPath">
        /// Non-zero to compute the full path file name.
        /// </param>
        /// <param name="stripBasePath">
        /// Non-zero to strip <paramref name="basePath" /> from an absolute
        /// path.
        /// </param>
        /// <param name="tailOnly">
        /// Non-zero to compute the tail-only (file name only) file name.
        /// </param>
        /// <param name="fileName1">
        /// Upon return, the computed full path file name, or null when not
        /// applicable. This parameter is optional.
        /// </param>
        /// <param name="fileName2">
        /// Upon return, the computed tail-only file name, or null when not
        /// applicable. This parameter is optional.
        /// </param>
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

        /// <summary>
        /// This method checks for the existence of the specified file name
        /// within the specified location, optionally including a vendor
        /// sub-path.
        /// </summary>
        /// <param name="unix">
        /// Non-zero to use Unix-style path combining, zero to use the native
        /// style, or null to use the default. This parameter is optional.
        /// </param>
        /// <param name="location">
        /// The directory location in which to check for the file.
        /// </param>
        /// <param name="vendorPath">
        /// The vendor sub-path to check, or null to skip the vendor check.
        /// </param>
        /// <param name="fileName">
        /// The file name to check for.
        /// </param>
        /// <param name="mode">
        /// A string describing the mode, used only for diagnostic tracing.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit verbose diagnostic tracing.
        /// </param>
        /// <param name="count">
        /// Upon entry, the running count of file names checked. Upon return,
        /// it is incremented by the number of file names checked.
        /// </param>
        /// <returns>
        /// The native path of the located file, or null if no matching file
        /// was found.
        /// </returns>
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

        /// <summary>
        /// This method searches the specified directory and each of its parent
        /// directories for files matching the specified search patterns.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context. This parameter is not used.
        /// </param>
        /// <param name="directory">
        /// The directory at which to begin the upward search.
        /// </param>
        /// <param name="subParts">
        /// The sub-directory parts to append to each directory before
        /// searching. This parameter is optional and may be null.
        /// </param>
        /// <param name="searchPatterns">
        /// The list of file name search patterns to match.
        /// </param>
        /// <param name="limit">
        /// The maximum number of matching paths to collect, or a negative
        /// value for no limit.
        /// </param>
        /// <param name="unix">
        /// Non-zero to use Unix-style path combining, zero to use the native
        /// style, or null to use the default. This parameter is optional.
        /// </param>
        /// <param name="paths">
        /// Upon entry, the list of matching paths to add to; it is created if
        /// null. Upon return, it contains any newly matched paths.
        /// </param>
        /// <returns>
        /// The number of matching paths that were found.
        /// </returns>
        public static int SearchParents(
            Interpreter interpreter,   /* in: NOT USED */
            string directory,          /* in */
            StringList subParts,       /* in: OPTIONAL */
            StringList searchPatterns, /* in */
            int limit,                 /* in */
            bool? unix,                /* in: OPTIONAL */
            ref StringList paths       /* in, out */
            )
        {
            int count = 0;

            TraceOps.DebugTrace(String.Format(
                "SearchParents: searching for {0} from {1} ({2})...",
                FormatOps.WrapOrNull(searchPatterns),
                FormatOps.WrapOrNull(directory),
                FormatOps.WrapOrNull(subParts)),
                typeof(PathOps).Name, TracePriority.PathDebug);

            string subDirectory = null;

            if (String.IsNullOrEmpty(directory) ||
                !Directory.Exists(directory))
            {
                goto done;
            }

            if ((searchPatterns == null) || (searchPatterns.Count == 0))
                goto done;

            if ((subParts != null) && (subParts.Count > 0))
            {
                foreach (string subPart in subParts)
                {
                    if (!CheckForValid(
                            unix, subPart, true, false, false, false))
                    {
                        goto done;
                    }
                }

                subDirectory = CombinePath(unix, (IList<string>)subParts);
            }
            else
            {
                subDirectory = null;
            }

            while (true)
            {
                string path;

                if (subDirectory != null)
                    path = CombinePath(unix, directory, subDirectory);
                else
                    path = directory;

                if (Directory.Exists(path))
                {
                    foreach (string searchPattern in searchPatterns)
                    {
                        if (String.IsNullOrEmpty(searchPattern))
                            continue;

                        if (HasDirectory(searchPattern))
                            continue;

                        string[] fileNames;

                        try
                        {
                            fileNames = Directory.GetFiles(
                                path, searchPattern,
                                SearchOption.TopDirectoryOnly);
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(PathOps).Name,
                                TracePriority.FileSystemError);

                            continue;
                        }

                        if (fileNames != null)
                        {
                            int length = fileNames.Length;

                            if (length > 0)
                            {
                                if (paths == null)
                                    paths = new StringList();

                                if (limit < 0)
                                {
                                    paths.AddRange(fileNames);
                                    count += length;
                                }
                                else
                                {
                                    count += paths.Add(
                                        fileNames, 0, limit);

                                    if (count >= limit)
                                        goto done;
                                }
                            }
                        }
                    }
                }

                try
                {
                    string newDirectory = Path.GetDirectoryName(
                        directory);

                    if (String.IsNullOrEmpty(newDirectory) ||
                        SharedStringOps.SystemEquals(
                            newDirectory, directory, NoCase))
                    {
                        break;
                    }

                    directory = newDirectory;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(PathOps).Name,
                        TracePriority.FileSystemError);

                    break;
                }
            }

        done:

            TraceOps.DebugTrace(String.Format(
                "SearchParents: found {0} matches out " +
                "of {1} in {2} ({3}): {4}", count,
                (paths != null) ? paths.Count : 0,
                FormatOps.WrapOrNull(directory),
                FormatOps.WrapOrNull(subDirectory),
                FormatOps.WrapOrNull(paths)),
                typeof(PathOps).Name,
                TracePriority.PathDebug);

            return count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches for a file using the specified interpreter
        /// context, candidate path, and search behavior flags.  This overload
        /// does not report the number of candidate names that were checked.
        /// </summary>
        /// <param name="interpreter">
        /// The optional interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="path">
        /// The (possibly qualified) file name to search for.
        /// </param>
        /// <param name="fileSearchFlags">
        /// The flags that control the search behavior.
        /// </param>
        /// <returns>
        /// The resolved file name if it was found; otherwise, either null or
        /// the original input path, depending on the search flags.
        /// </returns>
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

        /// <summary>
        /// This method searches for a file using the specified interpreter
        /// context, candidate path, and search behavior flags, reporting how
        /// many candidate names were checked during the search.
        /// </summary>
        /// <param name="interpreter">
        /// The optional interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="path">
        /// The (possibly qualified) file name to search for.
        /// </param>
        /// <param name="fileSearchFlags">
        /// The flags that control the search behavior.
        /// </param>
        /// <param name="count">
        /// Upon return, this value is incremented by the number of candidate
        /// names that were checked during the search.
        /// </param>
        /// <returns>
        /// The resolved file name if it was found; otherwise, either null or
        /// the original input path, depending on the search flags.
        /// </returns>
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

        /// <summary>
        /// This method returns the directory considered to be the current
        /// user's home directory, preferring the legacy home directory and
        /// then the user profile directory.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to return null when no existing user directory can be
        /// found; otherwise, the legacy home directory is returned even if it
        /// does not exist.
        /// </param>
        /// <returns>
        /// The user home directory if one was found; otherwise, null or the
        /// legacy home directory, depending on the value of
        /// <paramref name="strict" />.
        /// </returns>
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

        /// <summary>
        /// This method returns the configured user profile directory, as
        /// obtained from the associated environment variable.
        /// </summary>
        /// <returns>
        /// The user profile directory, or null if it is not configured.
        /// </returns>
        private static string GetUserProfileDirectory()
        {
            return GlobalConfiguration.GetValue(
                EnvVars.UserProfile, ScalarConfigurationFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method overrides one of the default cloud path entries,
        /// selecting the entry to replace based on the specified priority.
        /// </summary>
        /// <param name="priority">
        /// The priority that determines which default cloud path entry is
        /// overridden.
        /// </param>
        /// <param name="value">
        /// The new value to store for the selected default cloud path entry.
        /// </param>
        /// <returns>
        /// True if the cloud path entry was overridden, or no override was
        /// necessary; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the configured user rule set directory, as
        /// obtained from the associated environment variable.
        /// </summary>
        /// <returns>
        /// The user rule set directory, or null if it is not configured.
        /// </returns>
        public static string GetUserRuleSetDirectory()
        {
            return GlobalConfiguration.GetValue(
                EnvVars.XdgRuleSetDir, ScalarConfigurationFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the first existing cloud directory found beneath
        /// the user profile directory, using the configured default cloud
        /// paths.
        /// </summary>
        /// <returns>
        /// The fully combined cloud directory if an existing one was found;
        /// otherwise, null.
        /// </returns>
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

        /// <summary>
        /// This method returns the vendor path "offset" to be appended to
        /// search directories when looking for files.
        /// </summary>
        /// <returns>
        /// The configured vendor path, or null if it is not configured.
        /// </returns>
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

        /// <summary>
        /// This method sets the vendor path "offset" to be appended to search
        /// directories when looking for files.
        /// </summary>
        /// <param name="path">
        /// The vendor path to store.
        /// </param>
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

        /// <summary>
        /// This method selects a single home directory flags and path pair from
        /// the set of configured home directories matching the specified flags,
        /// using the supplied priority to choose among the available entries.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags used to determine the set of candidate home
        /// directories to consider.
        /// </param>
        /// <param name="priority">
        /// The priority used as an index into the candidate list, selecting which
        /// matching home directory pair to return; values out of range are clamped
        /// to the nearest valid entry.
        /// </param>
        /// <param name="reverse">
        /// Non-zero to reverse the order of the candidate list before applying the
        /// priority based selection.
        /// </param>
        /// <returns>
        /// The selected <c>IAnyPair&lt;HomeFlags, string&gt;</c> containing the home
        /// directory flags and associated path, or null if no matching home
        /// directory was found.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a home directory is configured for the
        /// specified home directory flags.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags identifying which home directory to check for.
        /// </param>
        /// <returns>
        /// True if a home directory is configured for the specified flags;
        /// otherwise, false.
        /// </returns>
        public static bool HaveHomeDirectory(
            HomeFlags flags /* in */
            )
        {
            return GetHomeDirectory(flags) != null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the directory that should be used for storing
        /// document files, preferring the appropriate special folder and falling
        /// back to the configured home directories when necessary.
        /// </summary>
        /// <param name="common">
        /// Non-zero to use the common (shared, all users) documents folder; zero
        /// to use the per-user documents folder.
        /// </param>
        /// <returns>
        /// The full path of the document directory, or null if no suitable
        /// directory could be determined.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the configured home directory associated with a
        /// single home directory kind, as identified by the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags identifying which home directory to retrieve;
        /// any flags portion is masked off and an exact match on the remaining
        /// value is required.
        /// </param>
        /// <returns>
        /// The configured home directory path for the specified flags, or null if
        /// no matching home directory is configured.
        /// </returns>
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

        /// <summary>
        /// This method sets the configured home directory associated with a single
        /// home directory kind, as identified by the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags identifying which home directory to set; any
        /// flags portion is masked off and an exact match on the remaining value
        /// is required.
        /// </param>
        /// <param name="value">
        /// The home directory path to store for the specified flags.
        /// </param>
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

        /// <summary>
        /// This method adds the specified home directory flags to the supplied
        /// list, creating the list if necessary, unless the flags are empty.
        /// </summary>
        /// <param name="list">
        /// A reference to the list of home directory flags to add to. If this is
        /// null and the flags are non-empty, a new list is created and returned
        /// via this parameter.
        /// </param>
        /// <param name="flags">
        /// The home directory flags to add to the list; nothing is added when this
        /// is <see cref="HomeFlags.None" />.
        /// </param>
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

        /// <summary>
        /// This method adds a single home directory flags and path pair to the
        /// supplied list, creating the list if necessary, optionally skipping the
        /// entry when the directory does not exist.
        /// </summary>
        /// <param name="list">
        /// A reference to the list of home directory pairs to add to. If this is
        /// null and an entry is to be added, a new list is created and returned
        /// via this parameter.
        /// </param>
        /// <param name="flags">
        /// The home directory flags associated with the path being added.
        /// </param>
        /// <param name="value">
        /// The home directory path to add; nothing is added when this is null or
        /// an empty string.
        /// </param>
        /// <param name="exists">
        /// Non-zero to require that the directory actually exists before it is
        /// added to the list; zero to add it unconditionally.
        /// </param>
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

        /// <summary>
        /// This method splits the specified value into individual paths using the
        /// platform path separator and adds each one to the supplied list of home
        /// directory pairs, optionally skipping entries whose directories do not
        /// exist.
        /// </summary>
        /// <param name="list">
        /// A reference to the list of home directory pairs to add to. If this is
        /// null and entries are to be added, a new list is created and returned
        /// via this parameter.
        /// </param>
        /// <param name="flags">
        /// The home directory flags associated with the paths being added.
        /// </param>
        /// <param name="value">
        /// The value containing zero or more paths separated by the platform path
        /// separator; nothing is added when this is null or an empty string.
        /// </param>
        /// <param name="exists">
        /// Non-zero to require that each directory actually exists before it is
        /// added to the list; zero to add them unconditionally.
        /// </param>
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

        /// <summary>
        /// This method builds an ordered list of the individual home directory
        /// flags present in the specified flags value, in priority order from the
        /// startup directory down to the legacy directory.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags to decompose into a list of individual flags.
        /// </param>
        /// <returns>
        /// A list of the individual home directory flags present in the specified
        /// value, or null if none are present.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the list of home directory paths matching the
        /// specified flags, discarding the associated flags from each pair.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags used to determine the set of home directories
        /// to return.
        /// </param>
        /// <returns>
        /// A list of home directory paths matching the specified flags, or null if
        /// none were found.
        /// </returns>
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

        /// <summary>
        /// This method builds the complete list of home directory flags and path
        /// pairs matching the specified flags, including both the individual home
        /// directories and the associated lists of additional directories from the
        /// relevant environment configuration values.
        /// </summary>
        /// <param name="flags">
        /// The home directory flags used to determine which home directories and
        /// directory lists to include; the existence requirement is also derived
        /// from these flags.
        /// </param>
        /// <returns>
        /// A list of home directory flags and path pairs matching the specified
        /// flags, or null if the flags were invalid or no directories were found.
        /// </returns>
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

        /// <summary>
        /// This method scrubs a path for display or logging by replacing the
        /// portion matching the specified base path with a base directory token,
        /// or otherwise reducing it to just its file name.
        /// </summary>
        /// <param name="basePath">
        /// The base path to look for at the start of the specified path; when it
        /// matches, that portion is replaced with the base directory token.
        /// </param>
        /// <param name="path">
        /// The path to scrub.
        /// </param>
        /// <returns>
        /// The scrubbed path: the base directory token (optionally followed by the
        /// remaining path) when the base path matches, the file name component
        /// otherwise, or the original path when it is null or empty.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value represents an
        /// absolute URI.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if the value represents an absolute URI; otherwise, false.
        /// </returns>
        public static bool IsUri(
            string value /* in */
            )
        {
            return IsUri(value, UriKind.Absolute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value represents a URI of
        /// the specified kind.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <param name="uriKind">
        /// The kind of URI to test for; when this is
        /// <see cref="UriKind.RelativeOrAbsolute" />, any URI is accepted,
        /// otherwise the detected kind must match this value.
        /// </param>
        /// <returns>
        /// True if the value represents a URI of the specified kind; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method concatenates the specified strings into a single string for
        /// use as part of a URI, skipping any null elements.
        /// </summary>
        /// <param name="values">
        /// The strings to concatenate; null elements are skipped.
        /// </param>
        /// <returns>
        /// The concatenated string, or null if the array of values itself is null.
        /// </returns>
        private static string CombineStringsForUri(
            params string[] values /* in */
            )
        {
            if (values == null) // NOTE: Impossible?
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

            foreach (string value in values)
            {
                if (value == null)
                    continue;

                builder.Append(value);
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the specified path segments into a single URI path
        /// using the forward slash directory separator, skipping invalid segments
        /// and optionally trimming and normalizing each segment.
        /// </summary>
        /// <param name="normalize">
        /// Non-zero to replace any contained native directory separators with the
        /// forward slash separator used by URI paths.
        /// </param>
        /// <param name="both">
        /// Controls how each path segment is trimmed before being combined; passed
        /// through to the trimming helper to select leading, trailing, or both
        /// ends, or null to disable trimming.
        /// </param>
        /// <param name="paths">
        /// The path segments to combine; null and empty segments are skipped.
        /// </param>
        /// <returns>
        /// The combined URI path, or null if the array of paths itself is null.
        /// </returns>
        private static string CombinePathsForUri(
            bool normalize,       /* in */
            bool? both,           /* in */
            params string[] paths /* in */
            )
        {
            if (paths == null) // NOTE: Impossible?
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB
        /// <summary>
        /// This method parses a URI query string into a collection of name and
        /// value pairs, optionally using the specified encoding.
        /// </summary>
        /// <param name="query">
        /// The query string to parse.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when parsing the query string, or null to use the
        /// default encoding.
        /// </param>
        /// <returns>
        /// A <see cref="NameValueCollection" /> containing the parsed name and
        /// value pairs, or null if the query string is null.
        /// </returns>
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

        /// <summary>
        /// This method URL-encodes the specified value for safe inclusion in a URI,
        /// optionally using the specified encoding.
        /// </summary>
        /// <param name="value">
        /// The value to URL-encode.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when URL-encoding the value, or null to use the
        /// default encoding.
        /// </param>
        /// <returns>
        /// The URL-encoded value, or null if the value is null.
        /// </returns>
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

        /// <summary>
        /// This method appends the name and value pairs from the specified
        /// dictionary to a URI query string, URL-encoding each name and value and
        /// separating successive pairs with ampersands.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary of name and value pairs to append; nothing is appended
        /// when this is null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when URL-encoding the names and values, or null to
        /// use the default encoding.
        /// </param>
        /// <param name="builder">
        /// A reference to the string builder that receives the query string. If
        /// this is null, a new string builder is created and returned via this
        /// parameter.
        /// </param>
        public static void QueryFromDictionary(
            StringDictionary dictionary, /* in */
            Encoding encoding,           /* in */
            ref StringBuilder builder    /* in, out */
            )
        {
            if (dictionary == null)
                return;

            if (builder == null)
                builder = StringBuilderFactory.CreateNoCache(); /* EXEMPT */

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

        /// <summary>
        /// This method combines two URI query strings into a single query string,
        /// parsing each one and re-emitting all of their name and value pairs with
        /// the names and values URL-encoded and separated by ampersands.
        /// </summary>
        /// <param name="query1">
        /// The first query string to combine.
        /// </param>
        /// <param name="query2">
        /// The second query string to combine.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when parsing the query strings and URL-encoding the
        /// names and values, or null to use the default encoding.
        /// </param>
        /// <returns>
        /// The combined query string, or null if both query strings are null or
        /// could not be parsed.
        /// </returns>
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

            StringBuilder builder = StringBuilderFactory.Create();
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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the absolute auxiliary <see cref="Uri" /> for a
        /// named resource, relative to the assembly auxiliary base
        /// <see cref="Uri" />.  The resource name is first normalized (a
        /// default text suffix may be appended) and then validated against the
        /// expected pattern before being combined with the base
        /// <see cref="Uri" />.
        /// </summary>
        /// <param name="resourceName">
        /// Upon entry, the original resource name.  Upon return, the resolved
        /// (and possibly suffixed) resource name that was used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The combined absolute <see cref="Uri" /> for the resource, or null
        /// if it could not be built.
        /// </returns>
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

            Regex regEx = identifierRegEx;

            if (regEx == null)
            {
                error = "cannot check resource name";
                return null;
            }

            Match match = regEx.Match(resourceName);

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

        /// <summary>
        /// This method attempts to combine a base <see cref="Uri" /> with a
        /// relative <see cref="Uri" /> string, producing a new absolute
        /// <see cref="Uri" />.  The selected components (scheme, path, query,
        /// fragment, etc.) from each <see cref="Uri" /> are combined according
        /// to the specified flags and formatting.
        /// </summary>
        /// <param name="baseUri">
        /// The absolute base <see cref="Uri" /> to combine.  This cannot be
        /// null and must be absolute.
        /// </param>
        /// <param name="relativeUri">
        /// The relative <see cref="Uri" /> string to combine with the base
        /// <see cref="Uri" />.  If null or empty, the base <see cref="Uri" />
        /// is returned unchanged.
        /// </param>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when combining query name/value
        /// pairs.  This is only used when compiled with web support enabled.
        /// </param>
        /// <param name="components">
        /// The <see cref="UriComponents" /> to include from the source URIs
        /// when building the combined <see cref="Uri" />.
        /// </param>
        /// <param name="format">
        /// The <see cref="UriFormat" /> used when extracting components.  This
        /// may be replaced with the default format unless the appropriate flag
        /// is set.
        /// </param>
        /// <param name="flags">
        /// The <see cref="UriFlags" /> that control how the URIs are combined
        /// (e.g. path separator handling, normalization, and any "allow"
        /// scheme constraints).
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The combined absolute <see cref="Uri" />, or null if the URIs could
        /// not be combined.
        /// </returns>
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
            StringBuilder builder = StringBuilderFactory.Create();

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
            string builderUri = StringBuilderCache.GetStringAndRelease(
                ref builder);

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

        /// <summary>
        /// This method attempts to create a <see cref="Uri" /> from a string,
        /// trying an absolute <see cref="Uri" /> first and then falling back to
        /// a relative one.
        /// </summary>
        /// <param name="value">
        /// The string value from which to create the <see cref="Uri" />.
        /// </param>
        /// <param name="uri">
        /// Upon success, this contains the created <see cref="Uri" />.
        /// </param>
        /// <param name="uriKind">
        /// Upon success, this contains the <see cref="UriKind" /> of the
        /// created <see cref="Uri" /> (absolute or relative).
        /// </param>
        /// <returns>
        /// True if the <see cref="Uri" /> was created; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method translates the "allow" scheme bits of a
        /// <see cref="UriFlags" /> value into their corresponding "was" scheme
        /// bits (e.g. <c>AllowHttp</c> becomes <c>WasHttp</c>).
        /// </summary>
        /// <param name="flags">
        /// The <see cref="UriFlags" /> value containing the "allow" scheme
        /// bits to translate.
        /// </param>
        /// <returns>
        /// A <see cref="UriFlags" /> value containing the "was" scheme bits
        /// that correspond to the set "allow" scheme bits.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="Uri" /> is
        /// a supported web <see cref="Uri" />, based on the schemes permitted
        /// by the supplied flags.  This overload does not return the host name.
        /// </summary>
        /// <param name="uri">
        /// The <see cref="Uri" /> to check.  This cannot be null and must be
        /// absolute.
        /// </param>
        /// <param name="flags">
        /// Upon entry, the <see cref="UriFlags" /> specifying which schemes are
        /// allowed.  Upon return, the "was" scheme bits are updated to reflect
        /// the detected scheme.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the <see cref="Uri" /> uses one of the allowed schemes;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="Uri" /> is
        /// a supported web <see cref="Uri" />, based on the schemes permitted
        /// by the supplied flags, and optionally returns the host name.
        /// </summary>
        /// <param name="uri">
        /// The <see cref="Uri" /> to check.  This cannot be null and must be
        /// absolute.
        /// </param>
        /// <param name="flags">
        /// Upon entry, the <see cref="UriFlags" /> specifying which schemes are
        /// allowed (and whether the host is required).  Upon return, the "was"
        /// scheme bits are updated to reflect the detected scheme.
        /// </param>
        /// <param name="host">
        /// Upon success, and unless the no-host flag is set, this contains the
        /// DNS-safe host name of the <see cref="Uri" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the <see cref="Uri" /> uses one of the allowed schemes;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified scheme string is the
        /// HTTPS <see cref="Uri" /> scheme.
        /// </summary>
        /// <param name="scheme">
        /// The <see cref="Uri" /> scheme string to check.
        /// </param>
        /// <returns>
        /// True if the scheme is the HTTPS scheme; otherwise, false.
        /// </returns>
        private static bool IsHttpsUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeHttps);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified scheme string is the
        /// HTTP <see cref="Uri" /> scheme.
        /// </summary>
        /// <param name="scheme">
        /// The <see cref="Uri" /> scheme string to check.
        /// </param>
        /// <returns>
        /// True if the scheme is the HTTP scheme; otherwise, false.
        /// </returns>
        private static bool IsHttpUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeHttp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified scheme string is the
        /// FTP <see cref="Uri" /> scheme.
        /// </summary>
        /// <param name="scheme">
        /// The <see cref="Uri" /> scheme string to check.
        /// </param>
        /// <returns>
        /// True if the scheme is the FTP scheme; otherwise, false.
        /// </returns>
        private static bool IsFtpUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeFtp);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified scheme string is the
        /// file <see cref="Uri" /> scheme.
        /// </summary>
        /// <param name="scheme">
        /// The <see cref="Uri" /> scheme string to check.
        /// </param>
        /// <returns>
        /// True if the scheme is the file scheme; otherwise, false.
        /// </returns>
        private static bool IsFileUriScheme(
            string scheme /* in */
            )
        {
            return SharedStringOps.SystemNoCaseEquals(scheme, Uri.UriSchemeFile);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="Uri" /> uses
        /// the HTTPS scheme.
        /// </summary>
        /// <param name="uri">
        /// The <see cref="Uri" /> to check.
        /// </param>
        /// <returns>
        /// True if the <see cref="Uri" /> is non-null and uses the HTTPS
        /// scheme; otherwise, false.
        /// </returns>
        public static bool IsHttpsUriScheme(
            Uri uri /* in */
            )
        {
            return (uri != null) && IsHttpsUriScheme(uri.Scheme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="Uri" /> uses
        /// the file scheme.
        /// </summary>
        /// <param name="uri">
        /// The <see cref="Uri" /> to check.
        /// </param>
        /// <returns>
        /// True if the <see cref="Uri" /> is non-null and uses the file
        /// scheme; otherwise, false.
        /// </returns>
        public static bool IsFileUriScheme(
            Uri uri /* in */
            )
        {
            return (uri != null) && IsFileUriScheme(uri.Scheme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is a remote
        /// (non-file) absolute <see cref="Uri" />.  This overload does not
        /// return the parsed <see cref="Uri" /> and does not treat existing
        /// local files as a match.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <returns>
        /// True if the value is an absolute <see cref="Uri" /> that does not
        /// use the file scheme; otherwise, false.
        /// </returns>
        public static bool IsRemoteUri(
            string value /* in */
            )
        {
            Uri uri = null;

            return IsRemoteUriOrFile(value, false, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is a remote
        /// (non-file) absolute <see cref="Uri" />, returning the parsed
        /// <see cref="Uri" />.  This overload does not treat existing local
        /// files as a match.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <param name="uri">
        /// Upon return, this contains the parsed absolute <see cref="Uri" />,
        /// or null if the value could not be parsed.
        /// </param>
        /// <returns>
        /// True if the value is an absolute <see cref="Uri" /> that does not
        /// use the file scheme; otherwise, false.
        /// </returns>
        public static bool IsRemoteUri(
            string value, /* in */
            ref Uri uri   /* out */
            )
        {
            return IsRemoteUriOrFile(value, false, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is a remote
        /// (non-file) absolute <see cref="Uri" /> or the name of an existing
        /// local file.  This overload does not return the parsed
        /// <see cref="Uri" />.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <returns>
        /// True if the value is an absolute <see cref="Uri" /> that does not
        /// use the file scheme, or names an existing local file; otherwise,
        /// false.
        /// </returns>
        public static bool IsRemoteUriOrFile(
            string value /* in */
            )
        {
            Uri uri = null;

            return IsRemoteUriOrFile(value, true, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string is a remote
        /// (non-file) absolute <see cref="Uri" /> and, optionally, whether it
        /// names an existing local file, returning the parsed
        /// <see cref="Uri" />.
        /// </summary>
        /// <param name="value">
        /// The string value to check.
        /// </param>
        /// <param name="allowFile">
        /// Non-zero to treat the name of an existing local file as a match when
        /// the value is not a remote <see cref="Uri" />; otherwise, zero.
        /// </param>
        /// <param name="uri">
        /// Upon return, this contains the parsed absolute <see cref="Uri" />,
        /// or null if the value could not be parsed.
        /// </param>
        /// <returns>
        /// True if the value is an absolute <see cref="Uri" /> that does not
        /// use the file scheme, or (when permitted) names an existing local
        /// file; otherwise, false.
        /// </returns>
        private static bool IsRemoteUriOrFile(
            string value,   /* in */
            bool allowFile, /* in */
            ref Uri uri     /* out */
            )
        {
            uri = null;

            if (!String.IsNullOrEmpty(value))
            {
                //
                // WARNING: *SECURITY* The "UriKind" value here must be
                //          "Absolute", please do not change it.
                //
                if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                    !IsFileUriScheme(uri))
                {
                    return true;
                }
            }

            return allowFile ? File.Exists(value) : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the portion of an absolute file name that is
        /// relative to one of the well-known package paths for the current
        /// AppDomain.
        /// </summary>
        /// <param name="fileName">
        /// The absolute file name to make relative to a package path.
        /// </param>
        /// <param name="keepLib">
        /// Non-zero to retain a trailing library directory component from the
        /// matched package path as part of the resulting relative file name;
        /// otherwise, zero.
        /// </param>
        /// <param name="verbatim">
        /// Non-zero to return the relative file name exactly as computed;
        /// otherwise, zero to remove an intermediate platform (framework)
        /// directory component, when present.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The file name relative to the matched package path, or null if the
        /// file name is not relative to any of the package paths or another
        /// error is encountered.
        /// </returns>
        public static string GetPackageRelativeFileName(
            string fileName, /* in */
            bool keepLib,    /* in */
            bool verbatim,   /* in */
            ref Result error /* out */
            )
        {
            string localFileName = fileName;

            if (String.IsNullOrEmpty(localFileName))
            {
                error = "invalid file name";
                return null;
            }

            try
            {
                if (GetPathType(localFileName) != PathType.Absolute)
                {
                    error = "file name not absolute";
                    return null;
                }

                //
                // BUGFIX: Make sure that the native directory
                //         separators are always used.
                //
                localFileName = GetNativePath(localFileName);

                //
                // HACK: Make sure the global paths are setup
                //       in this AppDomain, which may not be
                //       the primary one.
                //
                GlobalState.SetupPaths(true, true, false);

                string[] paths = {
                    GlobalState.GetAssemblyPackageRootPath(),
                    GlobalState.GetPackagePeerBinaryPath(),
                    GlobalState.GetPackagePeerAssemblyPath()
                };

                foreach (string path in paths)
                {
                    if (String.IsNullOrEmpty(path))
                        continue;

                    if (!SharedStringOps.StartsWith(
                            localFileName, path, ComparisonType))
                    {
                        continue;
                    }

                    string relativeFileName = MaybeTrimStart(
                        localFileName.Substring(path.Length));

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

        /// <summary>
        /// This method combines a relative file name with the directory that
        /// contains the file for the specified plugin.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose containing directory will be used as the base for
        /// the relative file name.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the operation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="fileName">
        /// The relative file name to combine with the directory of the plugin.
        /// </param>
        /// <returns>
        /// The combined file name, or null if the plugin is invalid, the file
        /// name is invalid or already rooted, the plugin file name cannot be
        /// determined, or an error is encountered.
        /// </returns>
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

        /// <summary>
        /// This method returns the file name portion of the specified script
        /// path, omitting any directory information.
        /// </summary>
        /// <param name="path">
        /// The script path from which to extract the file name.
        /// </param>
        /// <returns>
        /// The file name portion of the path, the original path when it does
        /// not contain any directory information, or null if an error is
        /// encountered.
        /// </returns>
        public static string ScriptFileNameOnly(
            string path /* in */
            )
        {
            string result = path;

            if (String.IsNullOrEmpty(result))
                return result;

            try
            {
                if (!HasDirectory(result)) /* throw? */
                    return result;

                return Path.GetFileName(result); /* throw */
            }
            catch
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the directory information for the specified
        /// path, accounting for paths that refer to a remote
        /// <see cref="Uri" />.
        /// </summary>
        /// <param name="path">
        /// The path from which to extract the directory information.
        /// </param>
        /// <returns>
        /// The directory information for the path, the original path when it is
        /// null or empty, or null if an error is encountered.
        /// </returns>
        public static string GetDirectoryName(
            string path /* in */
            )
        {
            string result = path;

            if (String.IsNullOrEmpty(result))
                return result;

            try
            {
                //
                // HACK: This is a horrible hack.
                //
                if (IsRemoteUri(result))
                {
                    return GetUnixPath(
                        Path.GetDirectoryName(
                            result)); /* throw */
                }
                else
                {
                    return Path.GetDirectoryName(
                        result); /* throw */
                }
            }
#if DEBUG && VERBOSE
            catch (Exception e)
#else
            catch
#endif
            {
#if DEBUG && VERBOSE
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.FileSystemError);
#endif

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the current working directory, optionally for a
        /// specific drive letter on Windows.
        /// </summary>
        /// <param name="path">
        /// The optional path whose leading drive letter selects the drive for
        /// which the current directory is returned; when null, empty, or not a
        /// drive-qualified path on Windows, the overall current working
        /// directory is returned instead.
        /// </param>
        /// <returns>
        /// The current directory for the selected drive, the overall current
        /// working directory, or null if an error is encountered.
        /// </returns>
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
                try
                {
                    return Path.GetFullPath(String.Format(
                        "{0}{1}", path[0], Characters.Colon));
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(PathOps).Name,
                        TracePriority.PathError);

                    return null;
                }
            }

            return GetCurrentDirectory();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified path into its drive prefix,
        /// directory, and file name components.
        /// </summary>
        /// <param name="path">
        /// The path to split into its component parts.
        /// </param>
        /// <param name="separator">
        /// The directory separator string to use when normalizing the
        /// resulting directory component, or null to leave the native
        /// separators in place.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to recognize a leading drive letter and colon as a drive
        /// prefix; otherwise, zero.
        /// </param>
        /// <param name="allowCurrent">
        /// Non-zero to resolve a bare drive prefix to its current directory;
        /// otherwise, zero.
        /// </param>
        /// <param name="prefix">
        /// Upon return, this contains the drive prefix component of the path,
        /// or null if there is none.
        /// </param>
        /// <param name="directory">
        /// Upon return, this contains the directory component of the path, or
        /// null if there is none.
        /// </param>
        /// <param name="fileName">
        /// Upon return, this contains the file name component of the path, or
        /// null if there is none.
        /// </param>
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
                        string newDirectory = GetCurrentDirectory(
                            drive);

                        if (newDirectory == null)
                        {
                            fileName = path;
                            return;
                        }

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

        /// <summary>
        /// This method determines the first directory separator character that
        /// appears in the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to search for a directory separator character.
        /// </param>
        /// <param name="separator">
        /// Upon success, this contains the first directory separator character
        /// found in the path; otherwise, it is left unchanged.
        /// </param>
        /// <returns>
        /// True if a directory separator character was found; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the first directory separator character that
        /// appears in the specified path, falling back to the native directory
        /// separator character when none is present.
        /// </summary>
        /// <param name="path">
        /// The path to search for a directory separator character.
        /// </param>
        /// <returns>
        /// The first directory separator character found in the path, or the
        /// native directory separator character if none is present.
        /// </returns>
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

        /// <summary>
        /// This method determines the first directory separator character that
        /// appears in any of the paths contained in the specified list.
        /// </summary>
        /// <param name="list">
        /// The list of paths to search for a directory separator character.
        /// </param>
        /// <param name="separator">
        /// Upon finding a directory separator character in one of the paths,
        /// this contains that character; otherwise, it is left unchanged.
        /// </param>
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

        /// <summary>
        /// This method removes the leading drive letter prefix (e.g. "C:")
        /// from a Windows-style path, optionally also removing any leading
        /// directory separator characters, in order to produce a relative
        /// path.
        /// </summary>
        /// <param name="path">
        /// The path to convert into a relative path.
        /// </param>
        /// <param name="separator">
        /// Non-zero to also remove any leading directory separator characters
        /// from the resulting path; zero to leave them intact.
        /// </param>
        /// <returns>
        /// The relative path with the drive letter prefix removed, or the
        /// original path if it is null, empty, too short, or does not begin
        /// with a drive letter prefix.
        /// </returns>
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

        /// <summary>
        /// This method appends a directory separator character to the end of
        /// the specified path.  If the path already contains a directory
        /// separator character, that same character is used; otherwise, the
        /// default directory separator character is appended.
        /// </summary>
        /// <param name="path">
        /// The path to which a directory separator character should be
        /// appended.
        /// </param>
        /// <returns>
        /// The path with a directory separator character appended, or the
        /// original path if it is null or empty.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the path specified via
        /// <paramref name="path1" /> is contained within (i.e. is under) the
        /// path specified via <paramref name="path2" />.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path1">
        /// The candidate child path to check.
        /// </param>
        /// <param name="path2">
        /// The candidate parent path.
        /// </param>
        /// <returns>
        /// True if <paramref name="path1" /> is under
        /// <paramref name="path2" />; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method recursively collects the directories contained within
        /// the specified path (and all of its subdirectories) that match the
        /// specified search pattern, adding them to the specified list.
        /// </summary>
        /// <param name="path">
        /// The directory path to search.
        /// </param>
        /// <param name="searchPattern">
        /// The search pattern used to match directory names.
        /// </param>
        /// <param name="paths">
        /// A reference to the list of directory paths.  Each matching
        /// directory found is added to this list, which is created if it is
        /// null.
        /// </param>
        /// <param name="errors">
        /// A reference to the list of errors.  If an exception is encountered,
        /// it is added to this list, which is created if it is null.
        /// </param>
        private static void GetDirectories( /* RECURSIVE */
            string path,          /* in */
            string searchPattern, /* in */
            ref StringList paths, /* in, out */
            ref ResultList errors /* in, out */
            )
        {
            try
            {
                string[] localPaths; /* REUSED */

                localPaths = Directory.GetDirectories(
                    GetNativePath(path), searchPattern,
                    SearchOption.TopDirectoryOnly);

                if (localPaths != null)
                {
                    Array.Sort(localPaths); /* O(N) */

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

        /// <summary>
        /// This method recursively collects the files contained within the
        /// specified path (and all of its subdirectories) that match the
        /// specified search pattern, adding them to the specified list.
        /// </summary>
        /// <param name="path">
        /// The directory path to search.
        /// </param>
        /// <param name="searchPattern">
        /// The search pattern used to match file names.
        /// </param>
        /// <param name="paths">
        /// A reference to the list of file paths.  Each matching file found is
        /// added to this list, which is created if it is null.
        /// </param>
        /// <param name="errors">
        /// A reference to the list of errors.  If an exception is encountered,
        /// it is added to this list, which is created if it is null.
        /// </param>
        private static void GetFiles( /* RECURSIVE */
            string path,          /* in */
            string searchPattern, /* in */
            ref StringList paths, /* in, out */
            ref ResultList errors /* in, out */
            )
        {
            try
            {
                string[] localPaths; /* REUSED */

                localPaths = Directory.GetFiles(
                    GetNativePath(path), searchPattern,
                    SearchOption.TopDirectoryOnly);

                if (localPaths != null)
                {
                    Array.Sort(localPaths); /* O(N) */

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
                    Array.Sort(localPaths); /* O(N) */

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

        /// <summary>
        /// This method adds an entry, associating the specified path with the
        /// specified path type and optional full path, to the specified
        /// dictionary.
        /// </summary>
        /// <param name="paths">
        /// A reference to the dictionary of paths to add the entry to, which
        /// is created if it is null.
        /// </param>
        /// <param name="path">
        /// The path used as the key for the entry.
        /// </param>
        /// <param name="pathType">
        /// The type of path being added.
        /// </param>
        /// <param name="fullPath">
        /// The full path associated with the entry.  This parameter may be
        /// null.
        /// </param>
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

        /// <summary>
        /// This method normalizes the specified path and adds it (optionally
        /// at every directory level) to the specified dictionary, removing the
        /// specified base path from its start if present.
        /// </summary>
        /// <param name="paths">
        /// A reference to the dictionary of paths to add the entry (or
        /// entries) to, which is created if it is null.
        /// </param>
        /// <param name="path1">
        /// The base path to be removed from the start of
        /// <paramref name="path2" />, if present.  This parameter may be null.
        /// </param>
        /// <param name="path2">
        /// The path to add.
        /// </param>
        /// <param name="pathType">
        /// The type of path being added.
        /// </param>
        /// <param name="anyLevel">
        /// Non-zero to add an entry for every level of the path (i.e. each
        /// trailing sub-path); zero to add only the full path.
        /// </param>
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

        /// <summary>
        /// This method enumerates the directories and/or files (according to
        /// the specified path type) contained within the specified path that
        /// match the specified search pattern, adding the results to the
        /// specified dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path">
        /// The primary directory path to enumerate.
        /// </param>
        /// <param name="searchPattern">
        /// The search pattern used to match directory and/or file names.  If
        /// this parameter is null, a pattern matching all names is used.
        /// </param>
        /// <param name="searchOption">
        /// The option that controls whether the search includes only the top
        /// directory or all subdirectories.
        /// </param>
        /// <param name="pathType">
        /// The flags that control whether directories, files, or both are
        /// enumerated, as well as whether the search is robust and whether
        /// entries are added at any level.
        /// </param>
        /// <param name="paths">
        /// A reference to the dictionary of paths to add the results to, which
        /// is created if it is null.
        /// </param>
        /// <param name="errors">
        /// A reference to the list of errors.  If an error is encountered, it
        /// is added to this list, which is created if it is null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

            string[] localPaths; /* REUSED */
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

                    if (localPaths != null)
                        Array.Sort(localPaths); /* O(N) */
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

                    if (localPaths != null)
                        Array.Sort(localPaths); /* O(N) */
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

        /// <summary>
        /// This method enumerates the directories and/or files under the
        /// specified primary path and determines which of them match the
        /// specified relative path or pattern, adding the matching pairs to
        /// the specified list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path1">
        /// The primary directory path to enumerate.
        /// </param>
        /// <param name="path2">
        /// The relative path or pattern to match against the entries found
        /// under the primary path.  When the match mode is
        /// <see cref="MatchMode.None" />, this must be a relative path that is
        /// looked up exactly; otherwise, it is used as a matching pattern.
        /// </param>
        /// <param name="mode">
        /// The matching mode used to compare entries against
        /// <paramref name="path2" />.  A value of <see cref="MatchMode.None" />
        /// requests an exact lookup.
        /// </param>
        /// <param name="searchOption">
        /// The option that controls whether the search includes only the top
        /// directory or all subdirectories.
        /// </param>
        /// <param name="pathType">
        /// The flags that control whether directories, files, or both are
        /// enumerated.
        /// </param>
        /// <param name="matches">
        /// A reference to the list of matching pairs to add the results to,
        /// which is created if it is null.
        /// </param>
        /// <param name="errors">
        /// A reference to the list of errors.  If an error is encountered, it
        /// is added to this list, which is created if it is null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the first specified path begins with
        /// (i.e. is located under) the second specified path, after both paths
        /// have been resolved to their full native forms.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path1">
        /// A reference to the candidate path that is tested for being located
        /// under the other path.  Upon a successful match, it is updated to its
        /// resolved full path.
        /// </param>
        /// <param name="path2">
        /// A reference to the candidate containing path.  Upon a successful
        /// match, it is updated to its resolved full path.
        /// </param>
        /// <returns>
        /// True if <paramref name="path1" /> is located under
        /// <paramref name="path2" />; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the first specified path part begins
        /// with the second specified path part, using filesystem-appropriate
        /// string comparison.
        /// </summary>
        /// <param name="part1">
        /// The path part to be examined.
        /// </param>
        /// <param name="part2">
        /// The path part to look for at the start of <paramref name="part1" />.
        /// </param>
        /// <returns>
        /// True if <paramref name="part1" /> begins with
        /// <paramref name="part2" />; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method compares two path parts using filesystem-appropriate
        /// string comparison.
        /// </summary>
        /// <param name="part1">
        /// The first path part to compare.
        /// </param>
        /// <param name="part2">
        /// The second path part to compare.
        /// </param>
        /// <returns>
        /// Zero if the two path parts are equal, a negative number if
        /// <paramref name="part1" /> sorts before <paramref name="part2" />, or
        /// a positive number if <paramref name="part1" /> sorts after
        /// <paramref name="part2" />.
        /// </returns>
        public static int CompareParts(
            string part1, /* in */
            string part2  /* in */
            )
        {
            return SharedStringOps.Compare(part1, part2, ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two path parts are equal, using
        /// filesystem-appropriate string comparison.
        /// </summary>
        /// <param name="path1">
        /// The first path part to compare.
        /// </param>
        /// <param name="path2">
        /// The second path part to compare.
        /// </param>
        /// <returns>
        /// True if the two path parts are equal; otherwise, false.
        /// </returns>
        public static bool IsEqualParts(
            string path1, /* in */
            string path2  /* in */
            )
        {
            return SharedStringOps.Equals(path1, path2, ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two file names, after converting each one to
        /// its native form, using filesystem-appropriate string comparison.
        /// </summary>
        /// <param name="path1">
        /// The first file name to compare.
        /// </param>
        /// <param name="path2">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// Zero if the two file names are equal, a negative number if
        /// <paramref name="path1" /> sorts before <paramref name="path2" />, or
        /// a positive number if <paramref name="path1" /> sorts after
        /// <paramref name="path2" />.
        /// </returns>
        public static int CompareFileNames(
            string path1, /* in */
            string path2  /* in */
            )
        {
            return SharedStringOps.Compare(
                GetNativePath(path1), GetNativePath(path2), ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two file names are equal, after
        /// converting each one to its native form, using filesystem-appropriate
        /// string comparison.
        /// </summary>
        /// <param name="path1">
        /// The first file name to compare.
        /// </param>
        /// <param name="path2">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// True if the two file names are equal; otherwise, false.
        /// </returns>
        public static bool IsEqualFileName(
            string path1, /* in */
            string path2  /* in */
            )
        {
            return SharedStringOps.Equals(
                GetNativePath(path1), GetNativePath(path2), ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the leading portions of two file
        /// names are equal, after converting each one to its native form, using
        /// filesystem-appropriate string comparison.
        /// </summary>
        /// <param name="path1">
        /// The first file name to compare.
        /// </param>
        /// <param name="path2">
        /// The second file name to compare.
        /// </param>
        /// <param name="length">
        /// The number of characters, from the start of each native file name,
        /// to compare.
        /// </param>
        /// <returns>
        /// True if the leading portions of the two file names are equal;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the first specified file name begins
        /// with the second specified file name, after converting each one to
        /// its native form, using filesystem-appropriate string comparison.
        /// </summary>
        /// <param name="path1">
        /// The file name to be examined.
        /// </param>
        /// <param name="path2">
        /// The file name to look for at the start of
        /// <paramref name="path1" />.
        /// </param>
        /// <returns>
        /// True if <paramref name="path1" /> begins with
        /// <paramref name="path2" />; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method computes a hash value for the specified path, for use as
        /// part of a file serial number, using the filesystem encoding.
        /// </summary>
        /// <param name="path">
        /// The path to hash.  This parameter may be null or an empty string.
        /// </param>
        /// <returns>
        /// The computed hash value, or zero if the path is null or empty, or if
        /// a suitable encoding cannot be obtained.
        /// </returns>
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

        /// <summary>
        /// This method combines the specified seed value and additional values
        /// into a single hash value, for use as a file serial number.
        /// </summary>
        /// <param name="value">
        /// An optional seed value to include in the combined hash.  When zero,
        /// it is omitted.
        /// </param>
        /// <param name="values">
        /// The array of additional values to include in the combined hash.
        /// </param>
        /// <returns>
        /// The combined hash value, or zero if there are no additional values.
        /// </returns>
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

        /// <summary>
        /// This method calculates the serial number string for the specified
        /// path, using the specified path flags and component values.
        /// </summary>
        /// <param name="path">
        /// The path associated with the serial number.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the serial number is calculated, such as
        /// whether the raw component values are used, whether a stable serial
        /// number is produced, or whether the values are hashed.
        /// </param>
        /// <param name="values">
        /// The array of component values used to calculate the serial number.
        /// </param>
        /// <returns>
        /// The calculated serial number string.
        /// </returns>
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
        /// <summary>
        /// This method attempts to obtain a serial number that uniquely
        /// identifies the file or directory at the specified path, using the
        /// Windows native file information.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to obtain a serial number for.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the serial number is calculated.
        /// </param>
        /// <param name="serialNumber">
        /// A reference to receive the calculated serial number.  Upon success,
        /// it is set to the serial number string.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// True if the serial number was obtained successfully; otherwise,
        /// false.
        /// </returns>
        private static bool WindowsTryGetSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            try
            {
                UNM.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, Directory.Exists(path),
                        false, ref fileInformation,
                        ref error) == ReturnCode.Ok)
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
        /// <summary>
        /// This method attempts to obtain a serial number that uniquely
        /// identifies the file or directory at the specified path, using the
        /// Linux native file status information.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to obtain a serial number for.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the serial number is calculated.
        /// </param>
        /// <param name="serialNumber">
        /// A reference to receive the calculated serial number.  Upon success,
        /// it is set to the serial number string.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// True if the serial number was obtained successfully; otherwise,
        /// false.
        /// </returns>
        private static bool LinuxTryGetSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            try
            {
                bool checkLinks = FlagOps.HasFlags(
                    flags, PathFlags.LinkSerialNumber, true);

                UNM.linux_stat buf;

                if ((!checkLinks &&
                    (UNM.linux_xstat(0, path, out buf) == 0)) ||
                    (checkLinks &&
                    (UNM.linux_lxstat(0, path, out buf) == 0)))
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain a serial number that uniquely
        /// identifies the file or directory at the specified path, using the
        /// macOS native file status information.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to obtain a serial number for.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the serial number is calculated.
        /// </param>
        /// <param name="serialNumber">
        /// A reference to receive the calculated serial number.  Upon success,
        /// it is set to the serial number string.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// True if the serial number was obtained successfully; otherwise,
        /// false.
        /// </returns>
        private static bool MacintoshTryGetSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            try
            {
                bool checkLinks = FlagOps.HasFlags(
                    flags, PathFlags.LinkSerialNumber, true);

                UNM.macos_stat_buf buf;

                if ((!checkLinks &&
                    (UNM.macos_stat(path, out buf) == 0)) ||
                    (checkLinks &&
                    (UNM.macos_lstat(path, out buf) == 0)))
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
        /// <summary>
        /// This method attempts to obtain a serial number that uniquely
        /// identifies the file or directory at the specified path, dispatching
        /// to the appropriate platform-specific implementation for the current
        /// operating system.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to obtain a serial number for.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the serial number is calculated.
        /// </param>
        /// <param name="serialNumber">
        /// A reference to receive the calculated serial number.  Upon success,
        /// it is set to the serial number string.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// True if the serial number was obtained successfully; otherwise,
        /// false.
        /// </returns>
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

            if (PlatformOps.IsMacintoshOperatingSystem())
            {
                return MacintoshTryGetSerialNumber(
                    path, flags, ref serialNumber, ref error);
            }
#endif

            error = "not supported on this operating system";
            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method determines whether two paths refer to the same
        /// underlying file or directory, using the Windows native file
        /// information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <param name="match">
        /// A reference to receive the result of the comparison.  Upon success,
        /// it is set to true if the two paths refer to the same file or
        /// directory; otherwise, false.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                UNM.BY_HANDLE_FILE_INFORMATION fileInformation1;
                UNM.BY_HANDLE_FILE_INFORMATION fileInformation2;

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
        /// <summary>
        /// This method determines whether two paths refer to the same
        /// underlying file or directory, using the Linux native file status
        /// information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <param name="match">
        /// A reference to receive the result of the comparison.  Upon success,
        /// it is set to true if the two paths refer to the same file or
        /// directory; otherwise, false.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
                UNM.linux_stat buf1;
                UNM.linux_stat buf2;

                if ((UNM.linux_xstat(0, path1, out buf1) == 0) &&
                    (UNM.linux_xstat(0, path2, out buf2) == 0))
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two paths refer to the same
        /// underlying file or directory, using the macOS native file status
        /// information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter is not used.
        /// </param>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <param name="match">
        /// A reference to receive the result of the comparison.  Upon success,
        /// it is set to true if the two paths refer to the same file or
        /// directory; otherwise, false.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode MacintoshIsSameFile(
            Interpreter interpreter, /* in: NOT USED */
            string path1,            /* in */
            string path2,            /* in */
            ref bool match,          /* out */
            ref Result error         /* out */
            )
        {
            try
            {
                UNM.macos_stat_buf buf1;
                UNM.macos_stat_buf buf2;

                if ((UNM.macos_lstat(path1, out buf1) == 0) &&
                    (UNM.macos_lstat(path2, out buf2) == 0))
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

        /// <summary>
        /// This method determines whether two paths refer to the same file or
        /// directory by resolving each one to its full path and comparing the
        /// results, without using any platform-specific file information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <param name="match">
        /// A reference to receive the result of the comparison.  Upon success,
        /// it is set to true if the two resolved paths are equal; otherwise,
        /// false.
        /// </param>
        /// <param name="error">
        /// A reference to receive error information.  Upon failure, it is set to
        /// the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path appears to refer
        /// to a script file, based on its file extension and, optionally, the
        /// content of any associated markup (XML) file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path">
        /// The path to examine.
        /// </param>
        /// <param name="viaGetScript">
        /// Non-zero if this method is being called via the <c>GetScript</c>
        /// pipeline, which affects how an otherwise unverifiable XML file path
        /// is treated; null to disable that assumption.
        /// </param>
        /// <param name="noXml">
        /// Non-zero to skip treating a markup (XML) file as a possible script
        /// file.
        /// </param>
        /// <param name="noValidate">
        /// Non-zero to skip validating a candidate XML file against the script
        /// schema.
        /// </param>
        /// <returns>
        /// True if the path appears to refer to a script file; otherwise,
        /// false.
        /// </returns>
        public static bool IsScriptFile(
            Interpreter interpreter, /* in */
            string path,             /* in */
            bool? viaGetScript,      /* in */
            bool noXml,              /* in */
            bool noValidate          /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string extension = GetExtension(path);

            if (String.IsNullOrEmpty(extension))
                return false;

            if (SharedStringOps.Equals(extension,
                    FileExtension.Script, ComparisonType))
            {
                return true;
            }

#if XML
            //
            // HACK: Generally, it would be something of an
            //       "bad" pattern in this library to have
            //       this class call into the XmlOps class;
            //       however, it is needed for the snippet
            //       subsystem -AND- callers can opt-out of
            //       this behavior.
            //
            if (!noXml && SharedStringOps.Equals(extension,
                    FileExtension.Markup, ComparisonType))
            {
                //
                // HACK: The general theory here is that if
                //       we are be called via the GetScript
                //       pipeline, we should presume that
                //       any XML file path presented to us
                //       is actually an XML script file.
                //
                // HACK: Therefore, if the file path passed
                //       to this method is a remote URI or
                //       a (pre-)existing file on the file
                //       system, we can check it against the
                //       XML script schema; otherwise, if we
                //       are being called via the GetScript
                //       pipeline, just assume it is an XML
                //       script file unless the caller has
                //       explicitly disabled that behavior,
                //       via setting "viaGetScript" to false
                //       instead of the default, which would
                //       be null.
                //
                Result error = null;

                if (IsRemoteUri(path) || File.Exists(path))
                {
                    if (noValidate || (XmlOps.ValidateScriptFile(
                            path, true, ref error) == ReturnCode.Ok))
                    {
                        return true;
                    }
                }
                else if (viaGetScript != null)
                {
                    if ((bool)viaGetScript)
                    {
                        return true;
                    }
                    else
                    {
                        //
                        // NOTE: *RARE* Attempt to actually fetch the
                        //       script file, from any possible source.
                        //
                        ScriptFlags scriptFlags = ScriptOps.GetFlags(
                            interpreter, ScriptFlags.UserOptionalFile,
                            false, false);

                        IClientData clientData = ClientData.Empty;
                        Result result = null;

                        if (interpreter.GetScript(
                                path, ref scriptFlags, ref clientData,
                                ref result) == ReturnCode.Ok)
                        {
                            string xml = result;

                            if (noValidate || (XmlOps.ValidateScriptString(
                                    xml, true, ref error) == ReturnCode.Ok))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            error = result;
                        }
                    }
                }

                TraceOps.DebugTrace(String.Format(
                    "IsScriptFile: path = {0}, error = {1}",
                    FormatOps.WrapOrNull(path),
                    FormatOps.WrapOrNull(error)),
                    typeof(PathOps).Name,
                    TracePriority.ScriptError2);
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path appears to refer
        /// to a signature file, based on its file extension.
        /// </summary>
        /// <param name="path">
        /// The path to examine.
        /// </param>
        /// <returns>
        /// True if the path appears to refer to a signature file; otherwise,
        /// false.
        /// </returns>
        public static bool IsSignatureFile(
            string path /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string extension = GetExtension(path);

            if (String.IsNullOrEmpty(extension))
                return false;

            if (SharedStringOps.Equals(extension,
                    FileExtension.Signature, ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two paths refer to the same
        /// underlying file or directory, preferring native platform file
        /// information when available and falling back to a generic comparison.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="path1">
        /// The first path to compare.
        /// </param>
        /// <param name="path2">
        /// The second path to compare.
        /// </param>
        /// <returns>
        /// True if the two paths refer to the same file or directory;
        /// otherwise, false.
        /// </returns>
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
                        TracePriority.PathError3);
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

#if NATIVE && UNIX
            if (!noNative &&
                PlatformOps.IsMacintoshOperatingSystem())
            {
                match = false;
                error = null;

                if (MacintoshIsSameFile(
                        interpreter, path1, path2, ref match,
                        ref error) == ReturnCode.Ok)
                {
                    return match;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsSameFile: macOS error = {0}",
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

        /// <summary>
        /// This method determines whether the specified path consists of just a
        /// single tilde character, ignoring any trailing path separators.
        /// </summary>
        /// <param name="path">
        /// The path to examine.
        /// </param>
        /// <returns>
        /// True if the path is just a tilde; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the first path ends with the second
        /// path, comparing both the original strings and their native and
        /// non-native translations.
        /// </summary>
        /// <param name="path1">
        /// The path to examine.
        /// </param>
        /// <param name="path2">
        /// The suffix to look for at the end of <paramref name="path1" />.
        /// </param>
        /// <returns>
        /// True if <paramref name="path1" /> ends with
        /// <paramref name="path2" />; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path exists as either a
        /// directory or a regular file.
        /// </summary>
        /// <param name="path">
        /// The path to check.
        /// </param>
        /// <returns>
        /// True if the path exists as a directory or a file; otherwise, false.
        /// </returns>
        public static bool PathExists(
            string path /* in */
            )
        {
            bool isDirectory;
            bool isFile;

            return PathExists(path, out isDirectory, out isFile);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path exists as either a
        /// directory or a regular file, reporting which of those it is.
        /// </summary>
        /// <param name="path">
        /// The path to check.
        /// </param>
        /// <param name="isDirectory">
        /// Upon return, set to true if the path exists as a directory;
        /// otherwise, false.
        /// </param>
        /// <param name="isFile">
        /// Upon return, set to true if the path exists as a regular file;
        /// otherwise, false.
        /// </param>
        /// <returns>
        /// True if the path exists as a directory or a file; otherwise, false.
        /// </returns>
        public static bool PathExists(
            string path,          /* in */
            out bool isDirectory, /* out */
            out bool isFile       /* out */
            )
        {
            //
            // NOTE: Does the path specified actually exist
            //       as either a directory or regular file?
            //
            isDirectory = Directory.Exists(path);
            isFile = File.Exists(path);

            return isDirectory || isFile;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified path to use the directory
        /// separator style indicated by the translation type.
        /// </summary>
        /// <param name="path">
        /// The path to translate.
        /// </param>
        /// <param name="translationType">
        /// The kind of path translation to perform.
        /// </param>
        /// <returns>
        /// The translated path, or the original path if no translation applies.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified path to use the directory
        /// separator style native to the current operating system.
        /// </summary>
        /// <param name="path">
        /// The path to translate.
        /// </param>
        /// <returns>
        /// The path using the native directory separator style.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified path to use the directory
        /// separator style that is not native to the current operating system.
        /// </summary>
        /// <param name="path">
        /// The path to translate.
        /// </param>
        /// <returns>
        /// The path using the non-native directory separator style.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified path to use the Windows
        /// directory separator style.
        /// </summary>
        /// <param name="path">
        /// The path to translate.
        /// </param>
        /// <returns>
        /// The path using the Windows directory separator style.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified path to use the Unix directory
        /// separator style.
        /// </summary>
        /// <param name="path">
        /// The path to translate.
        /// </param>
        /// <returns>
        /// The path using the Unix directory separator style.
        /// </returns>
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

        /// <summary>
        /// This method normalizes the directory separators in the specified
        /// path, replacing each one with the supplied separator string (or with
        /// the native directory separator when none is supplied).
        /// </summary>
        /// <param name="path">
        /// The path whose separators are to be normalized.
        /// </param>
        /// <param name="separator">
        /// The separator string to substitute for each directory separator
        /// character; null to use the native directory separator.
        /// </param>
        /// <returns>
        /// The path with its directory separators normalized.
        /// </returns>
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

            StringBuilder builder = StringBuilderFactory.Create(
                path.Length);

            foreach (char character in path)
            {
                if (IsDirectoryChar(character))
                    builder.Append(separator);
                else
                    builder.Append(character);
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to normalize the specified path, returning the
        /// original path unchanged if it is invalid or cannot be normalized.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="path">
        /// The path to normalize.
        /// </param>
        /// <returns>
        /// The normalized path, or the original path if it is invalid or cannot
        /// be normalized.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path is valid for use
        /// as a file, optionally requiring it to be rooted and/or to exist (and,
        /// on non-Windows operating systems, to be a normal file).
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <param name="rooted">
        /// Non-zero to require the path to be rooted, zero to require it not to
        /// be rooted; null to skip this check.
        /// </param>
        /// <param name="exists">
        /// Non-zero to require the path to exist as a file, zero to require that
        /// it not exist as a file or directory; null to skip this check.
        /// </param>
        /// <returns>
        /// True if the path is valid for use as a file; otherwise, false.
        /// </returns>
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

            //
            // HACK: The target path MUST be a normal file, not any
            //       kind of socket, etc.  This is required for use
            //       on a non-Windows operating systems, e.g. Linux,
            //       macOS, etc.  When on Windows, this is generally
            //       not required.
            //
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                FileAttributes fileAttributes = (FileAttributes)0;

                if (FileOps.GetFileAttributes(
                        path, ref fileAttributes) != ReturnCode.Ok)
                {
                    if ((fileAttributes != FileAttributes.Normal) &&
                        (fileAttributes != FileAttributes.ReadOnly))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path is valid for use
        /// as a directory, optionally requiring it to be rooted and/or to
        /// exist.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <param name="rooted">
        /// Non-zero to require the path to be rooted, zero to require it not to
        /// be rooted; null to skip this check.
        /// </param>
        /// <param name="exists">
        /// Non-zero to require the path to exist as a directory, zero to require
        /// that it not exist as a directory or file; null to skip this check.
        /// </param>
        /// <returns>
        /// True if the path is valid for use as a directory; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified path begins with a
        /// tilde character, also reporting the length of the path.
        /// </summary>
        /// <param name="path">
        /// The path to examine.  This parameter may be null.
        /// </param>
        /// <param name="length">
        /// Upon return, set to the length of the path, or an invalid length if
        /// the path is null.
        /// </param>
        /// <returns>
        /// True if the path begins with a tilde character; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method performs Unix-style leading tilde substitution on the
        /// specified path, replacing a leading tilde with the home directory of
        /// the current user -OR- with the directory that actually contains the
        /// specified file name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path on which to perform leading tilde substitution.
        /// </param>
        /// <param name="noSearch">
        /// Non-zero to skip searching standard user/application profile
        /// locations for the file name; otherwise, such a search may be
        /// performed.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return null when the path uses an unsupported tilde
        /// form; otherwise, the original path is returned verbatim in that
        /// case.
        /// </param>
        /// <returns>
        /// The path with any leading tilde substituted, the original path when
        /// no substitution applies, or null when strict mode rejects the path.
        /// </returns>
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

        /// <summary>
        /// This method replaces a leading base-directory token in the specified
        /// path with the actual base path of the currently executing assembly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter is not used.
        /// </param>
        /// <param name="path">
        /// The path on which to perform base-directory substitution.
        /// </param>
        /// <returns>
        /// The path with the leading base-directory token replaced, or the
        /// original path when no substitution applies.
        /// </returns>
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

        /// <summary>
        /// This method expands any environment variable references contained
        /// within the specified path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter is not used.
        /// </param>
        /// <param name="path">
        /// The path on which to perform environment variable expansion.
        /// </param>
        /// <returns>
        /// The path with its environment variable references expanded, or the
        /// original value when it is null or empty.
        /// </returns>
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

        /// <summary>
        /// This method performs base-directory, environment, and (optionally)
        /// leading tilde substitution on the specified path, optionally fully
        /// resolving it, while detecting whether it refers to a remote URI.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path to substitute or resolve.
        /// </param>
        /// <param name="resolve">
        /// Non-zero to fully resolve a local path; otherwise, only perform
        /// leading tilde substitution and normalize its directory separators.
        /// </param>
        /// <param name="remoteUri">
        /// Upon return, set to non-zero if the path refers to a remote URI;
        /// otherwise, set to zero.
        /// </param>
        /// <returns>
        /// The substituted or resolved path.
        /// </returns>
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

        /// <summary>
        /// This method resolves the specified path to a fully qualified path
        /// without performing environment variable substitution.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <returns>
        /// The resolved path, or null if the path could not be resolved.
        /// </returns>
        private static string ResolvePathNoEnvironment(
            Interpreter interpreter, /* in: OPTIONAL */
            string path              /* in */
            )
        {
            return NormalizePath(
                interpreter, null, path, null, false, true, null, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified path to a fully qualified path,
        /// performing environment variable and leading tilde substitution.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <returns>
        /// The resolved path, or null if the path could not be resolved.
        /// </returns>
        public static string ResolvePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path              /* in */
            )
        {
            return NormalizePath(
                interpreter, null, path, null, true, true, null, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified path to a fully qualified path,
        /// performing environment variable and leading tilde substitution and
        /// normalizing its directory separators.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <param name="unix">
        /// Non-zero to normalize directory separators to the Unix forward
        /// slash, zero to normalize them to the Windows backslash; null to
        /// leave them unchanged.
        /// </param>
        /// <returns>
        /// The resolved path, or null if the path could not be resolved.
        /// </returns>
        public static string ResolvePath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path,             /* in */
            bool? unix               /* in */
            )
        {
            return NormalizePath(
                interpreter, null, path, unix, true, true, null, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fully resolves the specified path to an absolute path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <returns>
        /// The fully resolved path, or null if the path could not be resolved.
        /// </returns>
        public static string ResolveFullPath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path              /* in */
            )
        {
            Result error = null; /* NOT USED */

            return ResolveFullPath(interpreter, path, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fully resolves the specified path to an absolute path,
        /// capturing any error that prevents resolution.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <param name="error">
        /// Upon failure, set to information about the error that prevented
        /// resolution.
        /// </param>
        /// <returns>
        /// The fully resolved path, or null if the path could not be resolved.
        /// </returns>
        public static string ResolveFullPath(
            Interpreter interpreter, /* in: OPTIONAL */
            string path,             /* in */
            ref Result error         /* out */
            )
        {
            string newPath = null;

            if (NormalizePath(
                    interpreter, null, path, null, true,
                    true, true, true, false, ref newPath,
                    ref error) == ReturnCode.Ok)
            {
                return newPath;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally transforms a path that refers to the Eagle
        /// source library or test suite directory layout into its installed
        /// "lib" equivalent.
        /// </summary>
        /// <param name="path">
        /// The path to transform.
        /// </param>
        /// <param name="skipLibraryToLib">
        /// Non-zero to skip the "Library" to "lib" transformation.
        /// </param>
        /// <param name="skipTestsToLib">
        /// Non-zero to skip the "Tests" to "lib" transformation.
        /// </param>
        /// <param name="relative">
        /// Non-zero to return only the relative matched portion of the path;
        /// otherwise, return the entire transformed path.
        /// </param>
        /// <returns>
        /// The transformed path, or the original path when no transformation
        /// applies.
        /// </returns>
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

        /// <summary>
        /// This method transforms a path ending in a "Tests" directory into one
        /// with a "lib" directory inserted just before it.
        /// </summary>
        /// <param name="path">
        /// The path to transform.
        /// </param>
        /// <param name="relative">
        /// Non-zero to return only the relative matched portion of the path;
        /// otherwise, return the entire transformed path.
        /// </param>
        /// <param name="done">
        /// Upon return, set to non-zero if a transformation was performed;
        /// otherwise, set to zero.
        /// </param>
        /// <returns>
        /// The transformed path, or the original path when no transformation
        /// applies.
        /// </returns>
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
        // HACK: If the last three parts of the path are "Library/YYYYY/<fileName>" -OR- the
        //       last four parts of the path are "Library/YYYYY/<dirName>/<fileName>" where
        //       "YYYYY" is either "Tests" or "Resources" -AND- with a "<dirName>" value of
        //       "data" or "tcl", (case-insensitive), then replace the "Library" part with
        //       "lib", replace the "Resources" part with "Loader1.0", and then return the
        //       resulting path.  Also, if the "relative" parameter is non-zero, return only
        //       the final X parts of the path, separated by forward slashes (Unix-style),
        //       where X will be either three or four (i.e. X will only be four if supported
        //       "<dirName>" part exists).  The returned path may not actually exist on the
        //       file system -AND- that is perfectly OK.
        //
        /// <summary>
        /// This method transforms a path matching the Eagle source library
        /// directory layout (e.g. "Library/Tests" or "Library/Resources",
        /// optionally followed by a "data" or "tcl" directory) into its
        /// installed equivalent, replacing the "Library" part with "lib" and
        /// the "Resources" part with the binary plugin loader directory.
        /// </summary>
        /// <param name="path">
        /// The path to transform.
        /// </param>
        /// <param name="relative">
        /// Non-zero to return only the relative matched portion of the path;
        /// otherwise, return the entire transformed path.
        /// </param>
        /// <param name="done">
        /// Upon return, set to non-zero if a transformation was performed;
        /// otherwise, set to zero.
        /// </param>
        /// <returns>
        /// The transformed path, or the original path when no transformation
        /// applies.
        /// </returns>
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
            //       "Library/YYYYY/<fileName>".  Instead, there could be 4,
            //       where they may form "Library/YYYYY/<dirName>/<fileName>",
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
            //       fit the supported pattern of "Library/YYYYY".
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
                //       skip to the previous part, which should be "YYYYY".
                //
                offset++;
            }

            //
            // NOTE: The next two parts before that must be exactly "Library"
            //       and "YYYYY".  On some systems, the case does not matter
            //       (e.g. Windows).
            //
            bool resources = false;

            if (SharedStringOps.Equals(
                    parts[length - offset], _Path.Resources, ComparisonType))
            {
                resources = true;
            }
            else if (!SharedStringOps.Equals(
                    parts[length - offset], _Path.Tests, ComparisonType))
            {
                done = false;
                return path;
            }

            int nextOffset = offset + 1;

            if (!SharedStringOps.Equals(
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
            // HACK: The only package currently residing in the resources
            //       directory is the (binary plugin) loader; therefore,
            //       fix it.
            //
            if (resources)
                parts[length - offset] = _Path.Loader;

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

        /// <summary>
        /// This method enables or disables, and queries, diagnostic tracing for
        /// path normalization.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable tracing, zero to disable it; null to query the
        /// current state without changing it.
        /// </param>
        /// <returns>
        /// True if path normalization tracing is currently enabled; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method returns the fully qualified form of the specified path
        /// while leaving any trailing tail component (which may contain illegal
        /// characters, such as the "?" and "*" glob characters) unresolved.
        /// </summary>
        /// <param name="path">
        /// The path to resolve.
        /// </param>
        /// <param name="unix">
        /// Non-zero to use Unix-style directory separators, zero to use
        /// Windows-style directory separators; null to use the native
        /// separator.
        /// </param>
        /// <returns>
        /// The fully qualified path with its tail component preserved, or the
        /// original value when it is null or empty.
        /// </returns>
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

        /// <summary>
        /// This method normalizes the specified path, returning the resulting
        /// path or null on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="rootPath">
        /// The root path against which to resolve a relative path, or null to
        /// use the current directory.
        /// </param>
        /// <param name="path">
        /// The path to normalize.
        /// </param>
        /// <param name="unix">
        /// Non-zero to normalize directory separators to Unix forward slashes,
        /// zero to normalize them to Windows backslashes; null to leave them
        /// unchanged.
        /// </param>
        /// <param name="environment">
        /// Non-zero to perform environment variable substitution on the path.
        /// </param>
        /// <param name="tilde">
        /// Non-zero to perform leading tilde substitution on the path.
        /// </param>
        /// <param name="full">
        /// Non-zero to fully resolve the path, zero to never resolve it; null
        /// to resolve only when the path is already rooted.
        /// </param>
        /// <param name="legacyResolve">
        /// Non-zero to use the legacy full-path resolution method.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to normalize the resulting path to lower case.
        /// </param>
        /// <returns>
        /// The normalized path, or null if the path could not be normalized.
        /// </returns>
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

        /// <summary>
        /// This method normalizes the specified path, placing the result into
        /// an output parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.  This parameter
        /// is optional.
        /// </param>
        /// <param name="rootPath">
        /// The root path against which to resolve a relative path, or null to
        /// use the current directory.
        /// </param>
        /// <param name="path">
        /// The path to normalize.
        /// </param>
        /// <param name="unix">
        /// Non-zero to normalize directory separators to Unix forward slashes,
        /// zero to normalize them to Windows backslashes; null to leave them
        /// unchanged.
        /// </param>
        /// <param name="environment">
        /// Non-zero to perform environment variable substitution on the path.
        /// </param>
        /// <param name="tilde">
        /// Non-zero to perform leading tilde substitution on the path.
        /// </param>
        /// <param name="full">
        /// Non-zero to fully resolve the path, zero to never resolve it; null
        /// to resolve only when the path is already rooted.
        /// </param>
        /// <param name="legacyResolve">
        /// Non-zero to use the legacy full-path resolution method.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to normalize the resulting path to lower case.
        /// </param>
        /// <param name="newPath">
        /// Upon success, set to the normalized path.
        /// </param>
        /// <param name="error">
        /// Upon failure, set to information about the error that prevented
        /// normalization.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                                rootPath = GetCurrentDirectory();

                            if (rootPath == null)
                            {
                                error = "invalid current directory";
                                code = ReturnCode.Error;
                                goto done;
                            }

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

        done:

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
