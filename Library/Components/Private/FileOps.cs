/*
 * FileOps.cs --
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
using System.Diagnostics;
using System.IO;
using System.Reflection;

#if !NET_STANDARD_20 && !MONO
using System.Security.AccessControl;
using System.Security.Principal;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("814d0e8c-c65a-4c7c-8c2e-2e6cee551509")]
    internal static class FileOps
    {
        #region Private StreamReader Support Constants
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if MONO || NET_STANDARD_20
        //
        // HACK: Newer versions (6.0+) of the Mono runtime use the same code
        //       as .NET Core for the StreamReader class; therefore, try its
        //       name first ("_byteBuffer") when targeted to those platforms.
        //
        private static string[] byteBufferFieldNames = {
            "_byteBuffer", "byteBuffer", null, null
        };
#else
        //
        // NOTE: The desktop .NET Framework StreamReader class still uses the
        //       legacy name ("byteBuffer") as of v4.8.0; therefore, try that
        //       name first when targeted to that platform.
        //
        private static string[] byteBufferFieldNames = {
            "byteBuffer", "_byteBuffer", null, null
        };
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static FieldInfo byteBufferFieldInfo = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private [glob] Support Collection Classes
        [ObjectId("64966544-0142-4f06-a335-63baa4c14293")]
        private sealed class FileSystemInfoDictionary
            : Dictionary<string, FileSystemInfo>
        {
            // nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The EXE header signature.
        //
        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // "MZ"

        //
        // NOTE: The PE header signature.
        //
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // "PE\0\0"

        //
        // NOTE: This "magic" value means that we have no idea what
        //       the value for the file (or operating system) is.
        //
        internal const ushort IMAGE_NT_OPTIONAL_BAD_MAGIC = 0x0;

        //
        // NOTE: The "magic" values from the PE header for 32-bit
        //       and 64-bit executables.
        //
        private const ushort IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x010B;
        private const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x020B;

        //
        // NOTE: The offset into the file where the offset into
        //       the file of the PE header is located.
        //
        private const int filePeSignatureOffsetOffset = 0x3C;

        //
        // NOTE: The offset for the TimeDateStamp field in the
        //       IMAGE_FILE_HEADER structure from the start of the
        //       PE signature.
        //
        private const int peTimeStampOffset = 0x8;

        //
        // NOTE: The offset for the Magic field in the
        //       IMAGE_OPTIONAL_HEADER structure from the start of
        //       the PE signature.
        //
        private const int peMagicOffset = 0x18;

        //
        // NOTE: The offset for the CLR header virtual address from
        //       the start of the IMAGE_OPTIONAL_HEADER structure.
        //
        private const int peClrHeaderOffset32 = 0xD0;
        private const int peClrHeaderOffset64 = 0xE0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: The offset for the SizeOfStackReserve field in the
        //       IMAGE_OPTIONAL_HEADER structure from the start of
        //       the PE signature.  This value just happens to be
        //       the same for 32-bit and 64-bit executables.
        //
        private const int peReserveOffset = 0x60;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The offset for the SizeOfStackCommit field in the
        //       IMAGE_OPTIONAL_HEADER structure from the start of
        //       the PE signature.  This value is different for
        //       32-bit and 64-bit executables.
        //
        private const int peCommitOffset32Bit = 0x64;
        private const int peCommitOffset64Bit = 0x68;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These strings are only used by GetPeFileMagicName.
        //
        private const string magicPe32 = "PE32";
        private const string magicPe32Plus = "PE32+";

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        internal static readonly FileSystemRights NoFileSystemRights =
            (FileSystemRights)0; /* None */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char[] GlobWildcardChars = {
            Characters.OpenBracket,
            Characters.Backslash,
            Characters.CloseBracket,
            Characters.OpenBrace,
            Characters.CloseBrace
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        private static NativeStack.StackSize PeFileStackSize;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void Initialize(
            bool force
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
#if NATIVE
                if (force || (PeFileStackSize == null))
                {
                    PeFileStackSize = GetPeFileStackSize(
                        GlobalState.GetAssembly());
                }
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetPeFileMagicName(
            ushort magic
            )
        {
            switch (magic)
            {
                case IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                    return magicPe32; // (i.e. 32-bit)
                case IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                    return magicPe32Plus; // (i.e. 64-bit)
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static ushort GetPeFileMagicForProcess()
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                if (PlatformOps.Is32BitProcess())
                    return IMAGE_NT_OPTIONAL_HDR32_MAGIC;
                else if (PlatformOps.Is64BitProcess())
                    return IMAGE_NT_OPTIONAL_HDR64_MAGIC;
            }

            return IMAGE_NT_OPTIONAL_BAD_MAGIC;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CheckPeFileArchitecture(
            string fileName,
            ref Result error
            )
        {
            ushort magic = IMAGE_NT_OPTIONAL_BAD_MAGIC;

            return CheckPeFileArchitecture(
                fileName, FindFlags.MatchArchitecture, ref magic,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CheckPeFileArchitecture(
            string fileName,
            FindFlags findFlags,
            ref ushort magic,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                if (GetPeFileMagic(fileName, ref magic, ref error))
                {
                    if (FlagOps.HasFlags(
                            findFlags, FindFlags.MatchArchitecture, true))
                    {
                        ushort processMagic = GetPeFileMagicForProcess();

                        if (processMagic != IMAGE_NT_OPTIONAL_BAD_MAGIC)
                        {
                            if (magic == processMagic)
                                return true;

                            error = String.Format(
                                "file {0} is not for this architecture " +
                                "(magic mismatch, got 0x{1:X}, wanted 0x{2:X}).",
                                FormatOps.WrapOrNull(fileName), magic, processMagic);
                        }
                        else
                        {
                            error = "operating system is neither 32-bit Windows nor 64-bit Windows";
                        }
                    }
                    else
                    {
                        //
                        // NOTE: The magic value was extracted successfully;
                        //       however, we are not allowed to match against
                        //       it; therefore, just return true.
                        //
                        return true;
                    }
                }

                return false;
            }
            else
            {
                //
                // NOTE: Not Windows, we do not even know if this operating
                //       system uses PE files for executables.
                //
                return true;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetPeFileMagic(
            string fileName,
            ref ushort magic,
            ref Result error
            )
        {
            uint clrHeader = 0;

            return GetPeFileMagic(
                fileName, ref magic, ref clrHeader, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetPeFileMagic(
            string fileName,
            ref ushort magic,
            ref uint clrHeader
            )
        {
            Result error = null;

            return GetPeFileMagic(
                fileName, ref magic, ref clrHeader, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetPeFileMagic(
            string fileName,
            ref ushort magic,
            ref uint clrHeader,
            ref Result error
            )
        {
            try
            {
                using (Stream stream = new FileStream(
                        fileName, FileMode.Open, FileAccess.Read)) /* EXEMPT */
                {
                    long length = stream.Length;

                    if (length < (filePeSignatureOffsetOffset + sizeof(uint)))
                    {
                        error = "file is too small for NT signature offset";
                        return false;
                    }

                    using (BinaryReader binaryReader = new BinaryReader(stream))
                    {
                        ushort dosSignature = binaryReader.ReadUInt16();

                        if (dosSignature != IMAGE_DOS_SIGNATURE)
                        {
                            error = String.Format(
                                "DOS signature mismatch, got 0x{0:X}, " +
                                "wanted 0x{1:X}.", dosSignature,
                                IMAGE_DOS_SIGNATURE);

                            return false;
                        }

                        stream.Seek(
                            filePeSignatureOffsetOffset,
                            SeekOrigin.Begin);

                        uint offset = binaryReader.ReadUInt32();

                        if (length < (offset + sizeof(uint)))
                        {
                            error = "file is too small for NT signature";
                            return false;
                        }

                        stream.Seek(
                            offset, SeekOrigin.Begin);

                        uint ntSignature = binaryReader.ReadUInt32();

                        if (ntSignature != IMAGE_NT_SIGNATURE)
                        {
                            error = String.Format(
                                "NT signature mismatch, got 0x{0:X}, " +
                                "wanted 0x{1:X}.", ntSignature,
                                IMAGE_NT_SIGNATURE);

                            return false;
                        }

                        if (length < (offset + peMagicOffset + sizeof(ushort)))
                        {
                            error = "file is too small for magic";
                            return false;
                        }

                        stream.Seek(
                            offset + peMagicOffset, SeekOrigin.Begin);

                        magic = binaryReader.ReadUInt16(); /* NOT VALIDATED */

                        long position = stream.Position;

                        if (magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC)
                        {
                            if (length >= (position +
                                    peClrHeaderOffset64 + sizeof(uint)))
                            {
                                stream.Seek(
                                    peClrHeaderOffset64,
                                    SeekOrigin.Current);

                                clrHeader = binaryReader.ReadUInt32();
                            }
                            else
                            {
                                clrHeader = 0;
                            }
                        }
                        else
                        {
                            if (length >= (position +
                                    peClrHeaderOffset32 + sizeof(uint)))
                            {
                                stream.Seek(
                                    peClrHeaderOffset32,
                                    SeekOrigin.Current);

                                clrHeader = binaryReader.ReadUInt32();
                            }
                            else
                            {
                                clrHeader = 0;
                            }
                        }

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        private static NativeStack.StackSize GetPeFileStackSize(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            NativeStack.StackSize stackSize = null;

            if (assembly != null)
            {
                stackSize = new NativeStack.StackSize();

                /* IGNORED */
                GetPeFileStackReserveAndCommit(assembly.Location,
                    ref stackSize.reserve, ref stackSize.commit);
            }

            return stackSize;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetPeFileStackReserveAndCommit(
            string fileName,
            ref UIntPtr reserve,
            ref UIntPtr commit
            )
        {
            bool result = false;

            try
            {
                UIntPtr localReserve = UIntPtr.Zero;
                UIntPtr localCommit = UIntPtr.Zero;

                //
                // NOTE: The BinaryReader.Close() method is documented to
                //       close the contained stream as well.
                //
                using (BinaryReader binaryReader = new BinaryReader(new FileStream(
                    fileName, FileMode.Open, FileAccess.Read))) /* EXEMPT */
                {
                    if (binaryReader.ReadUInt16() == IMAGE_DOS_SIGNATURE)
                    {
                        binaryReader.BaseStream.Seek(
                            filePeSignatureOffsetOffset,
                            SeekOrigin.Begin);

                        uint offset = binaryReader.ReadUInt32();

                        binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

                        if (binaryReader.ReadUInt32() == IMAGE_NT_SIGNATURE)
                        {
                            binaryReader.BaseStream.Seek(offset + peMagicOffset,
                                SeekOrigin.Begin);

                            switch (binaryReader.ReadUInt16())
                            {
                                case IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                                    {
                                        binaryReader.BaseStream.Seek(
                                            offset + peReserveOffset,
                                            SeekOrigin.Begin);

                                        localReserve = new UIntPtr(
                                            binaryReader.ReadUInt32());

                                        binaryReader.BaseStream.Seek(
                                            offset + peCommitOffset32Bit,
                                            SeekOrigin.Begin);

                                        localCommit = new UIntPtr(
                                            binaryReader.ReadUInt32());

                                        result = true;
                                        break;
                                    }
                                case IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                                    {
                                        binaryReader.BaseStream.Seek(
                                            offset + peReserveOffset,
                                            SeekOrigin.Begin);

                                        localReserve = new UIntPtr(
                                            binaryReader.ReadUInt64());

                                        binaryReader.BaseStream.Seek(
                                            offset + peCommitOffset64Bit,
                                            SeekOrigin.Begin);

                                        localCommit = new UIntPtr(
                                            binaryReader.ReadUInt64());

                                        result = true;
                                        break;
                                    }
                            }
                        }
                    }
                }

                //
                // NOTE: Did we succeed in reading the
                //       value(s) from the file?
                //
                if (result)
                {
                    reserve = localReserve;
                    commit = localCommit;
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the RuntimeOps.CheckForStackSpace method only.
        //
        public static void CopyPeFileStackReserveAndCommit(
            NativeStack.StackSize stackSize
            )
        {
            if (stackSize == null)
                return;

            Initialize(false);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (PeFileStackSize != null)
                {
                    if (stackSize.reserve == UIntPtr.Zero)
                        stackSize.reserve = PeFileStackSize.reserve;

                    if (stackSize.commit == UIntPtr.Zero)
                        stackSize.commit = PeFileStackSize.commit;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the NativeStack.QueryNewThreadNativeStackSize
        //       method only.
        //
        public static ulong GetPeFileStackReserve()
        {
            Initialize(false);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (PeFileStackSize != null)
                    return PeFileStackSize.reserve.ToUInt64();
            }

            return 0;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetPeFileDateTime(
            string fileName,      /* in */
            ref DateTime dateTime /* out */
            )
        {
            //
            // NOTE: This should be reliable.
            //
            uint timeStamp = 0;

            if (!GetPeFileTimeStamp(fileName, ref timeStamp))
                return false;

            DateTime localDateTime = DateTime.MinValue;

            if (!TimeOps.SecondsToDateTime(
                    timeStamp, ref localDateTime, TimeOps.PeEpoch))
            {
                return false;
            }

            dateTime = localDateTime;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetPeFileTimeStamp(
            string fileName,
            ref uint timeStamp
            )
        {
            bool result = false;

            try
            {
                uint localTimeStamp = 0;

                //
                // NOTE: The BinaryReader.Close() method is documented to
                //       close the contained stream as well.
                //
                using (BinaryReader binaryReader = new BinaryReader(new FileStream(
                    fileName, FileMode.Open, FileAccess.Read))) /* EXEMPT */
                {
                    if (binaryReader.ReadUInt16() == IMAGE_DOS_SIGNATURE)
                    {
                        binaryReader.BaseStream.Seek(filePeSignatureOffsetOffset,
                            SeekOrigin.Begin);

                        uint offset = binaryReader.ReadUInt32();

                        binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);

                        if (binaryReader.ReadUInt32() == IMAGE_NT_SIGNATURE)
                        {
                            binaryReader.BaseStream.Seek(offset + peMagicOffset,
                                SeekOrigin.Begin);

                            switch (binaryReader.ReadUInt16())
                            {
                                case IMAGE_NT_OPTIONAL_HDR32_MAGIC:
                                case IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                                    {
                                        binaryReader.BaseStream.Seek(
                                            offset + peTimeStampOffset,
                                            SeekOrigin.Begin);

                                        localTimeStamp =
                                            binaryReader.ReadUInt32();

                                        result = true;
                                        break;
                                    }
                            }
                        }
                    }
                }

                //
                // NOTE: Did we succeed in reading the
                //       value(s) from the file?
                //
                if (result)
                    timeStamp = localTimeStamp;
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsFileVersionEmpty(
            FileVersionInfo version,
            bool nullIsEmpty
            )
        {
            if (version == null)
                return nullIsEmpty;

            return (version.FileMajorPart == 0) &&
                (version.FileMinorPart == 0) &&
                (version.FileBuildPart == 0) &&
                (version.FilePrivatePart == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetFileVersion(
            string fileName,
            bool nonEmpty,
            ref FileVersionInfo version,
            ref Result error
            )
        {
            if (File.Exists(fileName))
            {
                try
                {
                    FileVersionInfo localVersion =
                        FileVersionInfo.GetVersionInfo(fileName);

                    if (!nonEmpty || !IsFileVersionEmpty(
                            localVersion, true))
                    {
                        version = localVersion;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "file {0} cannot have empty version",
                            FormatOps.WrapOrNull(fileName));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = String.Format(
                    "could not read {0}: no such file",
                    FormatOps.WrapOrNull(fileName));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TryCreate(
            string fileName,
            ref Result error
            )
        {
            try
            {
                using (FileStream stream = new FileStream(
                        fileName, FileMode.CreateNew, FileAccess.Write))
                {
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Touch(
            string path,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(path))
            {
                error = "invalid path";
                return ReturnCode.Error;
            }

            SetDateTimeCallback callback;

            if (Directory.Exists(path))
                callback = Directory.SetLastWriteTimeUtc;
            else if (File.Exists(path))
                callback = File.SetLastWriteTimeUtc;
            else
                callback = null;

            if (callback != null)
            {
                try
                {
                    callback(path,
                        TimeOps.GetUtcNow()); /* throw */

                    return ReturnCode.Ok;
                }
                catch (FileNotFoundException)
                {
                    return TryCreate(path, ref error);
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                return TryCreate(path, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void VerifyExecutable(
            Interpreter interpreter,
            string path,
            out bool accessStatus
            )
        {
#if !NET_STANDARD_20 && !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                try
                {
                    FileSystemRights grantedRights = NoFileSystemRights;
                    bool localAccessStatus = false;
                    Result error = null;

                    if (AccessCheck(
                            path, FileSystemRights.ExecuteFile,
                            ref grantedRights, ref localAccessStatus,
                            ref error) == ReturnCode.Ok)
                    {
                        accessStatus = localAccessStatus;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "VerifyExecutable: error = {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(FileOps).Name,
                            TracePriority.PathError2);

                        accessStatus = false;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(FileOps).Name,
                        TracePriority.PathError);

                    accessStatus = false;
                }
            }
            else
#endif
            {
                //
                // HACK: Assume anything readable can be executed.
                //
                accessStatus = VerifyPathAccess(
                    interpreter, path, FileAccess.Read);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void VerifyReadable(
            Interpreter interpreter,
            string path,
            out bool accessStatus
            )
        {
#if !NET_STANDARD_20 && !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                try
                {
                    FileSystemRights grantedRights = NoFileSystemRights;
                    bool localAccessStatus = false;
                    Result error = null;

                    if (AccessCheck(
                            path, FileSystemRights.Read,
                            ref grantedRights, ref localAccessStatus,
                            ref error) == ReturnCode.Ok)
                    {
                        accessStatus = localAccessStatus;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "VerifyReadable: error = {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(FileOps).Name,
                            TracePriority.PathError2);

                        accessStatus = false;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(FileOps).Name,
                        TracePriority.PathError);

                    accessStatus = false;
                }
            }
            else
#endif
            {
                accessStatus = VerifyPathAccess(
                    interpreter, path, FileAccess.Read);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void VerifyWritable(
            Interpreter interpreter,
            string path,
            out bool accessStatus
            )
        {
#if !NET_STANDARD_20 && !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                try
                {
                    FileSystemRights grantedRights = NoFileSystemRights;
                    bool localAccessStatus = false;
                    Result error = null;

                    if (AccessCheck(
                            path, FileSystemRights.Write,
                            ref grantedRights, ref localAccessStatus,
                            ref error) == ReturnCode.Ok)
                    {
                        accessStatus = localAccessStatus;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "VerifyWritable: error = {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(FileOps).Name,
                            TracePriority.PathError2);

                        accessStatus = false;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(FileOps).Name,
                        TracePriority.PathError);

                    accessStatus = false;
                }
            }
            else
#endif
            {
                accessStatus = VerifyPathAccess(
                    interpreter, path, FileAccess.Write);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool VerifyPathAccess(
            Interpreter interpreter,
            string path,
            FileAccess access
            )
        {
            Result error = null;

            return VerifyPathAccess(
                interpreter, path, access, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool VerifyPathAccess(
            Interpreter interpreter,
            string path,
            FileAccess access,
            bool mustCreate,
            ref Result error
            )
        {
            return Directory.Exists(path) ?
                VerifyDirectoryAccess(interpreter, path, access, ref error) :
                VerifyFileAccess(interpreter, path, access, mustCreate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool VerifyDirectoryWriteAccess(
            Interpreter interpreter,
            string directory,
            ref Result error
            )
        {
            string path = null;

            try
            {
                path = PathOps.GetUniquePath(
                    interpreter, directory, null, null, ref error);

                if (path == null)
                    return false;

                Directory.CreateDirectory(path); /* throw */
                Directory.Delete(path); /* throw */

                return VerifyFileAccess(
                    interpreter, path, FileAccess.Write,
                    true, ref error);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FileOps).Name,
                    TracePriority.FileSystemError);

                error = e;
            }
            finally
            {
                if (path != null)
                {
                    try
                    {
                        if (File.Exists(path))
                            File.Delete(path); /* throw */
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(FileOps).Name,
                            TracePriority.FileSystemError);
                    }

                    try
                    {
                        if (Directory.Exists(path))
                            Directory.Delete(path); /* throw */
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(FileOps).Name,
                            TracePriority.FileSystemError);
                    }

                    path = null;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool VerifyDirectoryAccess(
            Interpreter interpreter,
            string directory,
            FileAccess access,
            ref Result error
            )
        {
            try
            {
                switch (access)
                {
                    case FileAccess.Read:
                        {
                            /* IGNORED */
                            Directory.GetFileSystemEntries(
                                directory); /* throw */

                            return true;
                        }
                    case FileAccess.Write:
                        {
                            return VerifyDirectoryWriteAccess(
                                interpreter, directory, ref error);
                        }
                    case FileAccess.ReadWrite:
                        {
                            /* IGNORED */
                            Directory.GetFileSystemEntries(
                                directory); /* throw */

                            return VerifyDirectoryWriteAccess(
                                interpreter, directory, ref error);
                        }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FileOps).Name,
                    TracePriority.FileSystemError);

                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool VerifyFileAccess(
            Interpreter interpreter,
            string fileName,
            FileAccess access,
            bool mustCreate,
            ref Result error
            )
        {
            try
            {
                Stream stream = null;

                try
                {
                    FileMode fileMode = mustCreate ?
                        FileMode.CreateNew : FileMode.Open;

                    if (RuntimeOps.NewStream(
                            interpreter, fileName, fileMode, access,
                            ref stream, ref error) == ReturnCode.Ok)
                    {
                        return (stream != null);
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream = null;
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FileOps).Name,
                    TracePriority.FileSystemError);

                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        private static bool HasRights(
            FileSystemRights rights,
            FileSystemRights hasRights,
            bool all
            )
        {
            if (all)
                return ((rights & hasRights) == hasRights);
            else
                return ((rights & hasRights) != 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static FileSystemRights FilePermissionsToFileSystemRights(
            FilePermission permissions
            )
        {
            FileSystemRights rights = NoFileSystemRights; /* NONE */

            if (FlagOps.HasFlags(permissions, FilePermission.Read, true))
                rights |= FileSystemRights.Read;

            if (FlagOps.HasFlags(permissions, FilePermission.Write, true))
                rights |= FileSystemRights.Write;

            if (FlagOps.HasFlags(permissions, FilePermission.Execute, true))
                rights |= FileSystemRights.ExecuteFile | FileSystemRights.Traverse;

            return rights;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode IsOwner(
            string path,
            ref IdentityReference owner,
            ref bool ownerStatus,
            ref Result error
            )
        {
            return IsOwner(
                null, path, ref owner, ref ownerStatus, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode IsOwner(
            WindowsIdentity identity,
            string path,
            ref IdentityReference owner,
            ref bool ownerStatus,
            ref Result error
            )
        {
            try
            {
                //
                // NOTE: These must be reset prior to doing anything else in case
                //       further actions fail (by raising an exception).
                //
                ownerStatus = false;

                //
                // NOTE: If the file or directory does not exist, proceed no further.
                //
                if (PathOps.PathExists(path))
                {
                    //
                    // NOTE: If not specified, get the identity of the user for the
                    //       current thread.
                    //
                    if (identity == null)
                        identity = WindowsIdentity.GetCurrent();

                    //
                    // NOTE: Attempt to get the file security object for this file or
                    //       directory.
                    //
                    FileSystemSecurity security;

                    //
                    // NOTE: Use the correct method based on whether this path represents
                    //       a file or directory.
                    //
                    if (Directory.Exists(path))
                        security = Directory.GetAccessControl(path);
                    else
                        security = File.GetAccessControl(path);

                    //
                    // NOTE: Attempt to get the owning user and group for this file or
                    //       directory.
                    //
                    owner = security.GetOwner(typeof(SecurityIdentifier));

                    //
                    // NOTE: If the current user is the owner of the file OR if the current
                    //       user belongs to the group that owns the file then they are the
                    //       "owner".
                    //
                    if (identity.User.Equals(owner) || identity.Groups.Contains(owner))
                        ownerStatus = true;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode AccessCheck(
            WindowsIdentity identity,
            FileSystemSecurity security,
            FileSystemRights desiredRights,
            ref FileSystemRights grantedRights,
            ref bool accessStatus,
            ref Result error
            )
        {
            //
            // NOTE: These must be reset prior to doing anything else in case
            //       further actions fail (by raising an exception).
            //
            grantedRights = NoFileSystemRights;
            accessStatus = false;

            try
            {
                //
                // NOTE: If not specified, get the identity of the user for the
                //       current thread.
                //
                if (identity == null)
                    identity = WindowsIdentity.GetCurrent();

                //
                // NOTE: Attempt to get the access rules for this file or directory.
                //
                AuthorizationRuleCollection accessRules =
                    security.GetAccessRules(true, true, typeof(SecurityIdentifier));

                //
                // NOTE: Iterate over the access rules and change the granted rights
                //       according to what we find.
                //
                foreach (FileSystemAccessRule accessRule in accessRules)
                {
                    //
                    // BUGBUG: Double-check this logic.  There appears to be no formal
                    //         documentation in MSDN that indicates the correct way to
                    //         perform an access check using the .NET Framework supplied
                    //         functionality; however, this works and appears to be
                    //         "correct".
                    //
                    // NOTE: Check to see if this access rule applies to the current
                    //       user directly or indirectly via a group that the current
                    //       user belongs to.
                    //
                    if (identity.User.Equals(accessRule.IdentityReference) ||
                        identity.Groups.Contains(accessRule.IdentityReference))
                    {
                        switch (accessRule.AccessControlType)
                        {
                            case AccessControlType.Allow:
                                //
                                // NOTE: The file system rights access mask represents
                                //       rights the user has been granted, add them to
                                //       the granted rights.
                                //
                                grantedRights |= accessRule.FileSystemRights;
                                break;
                            case AccessControlType.Deny:
                                //
                                // NOTE: The file system rights access mask represents
                                //       rights the user has been deined, remove them
                                //       from the granted rights.
                                //
                                grantedRights &= ~accessRule.FileSystemRights;
                                break;
                        }
                    }
                }

                //
                // NOTE: Only return "true" for the access status if ALL the desired
                //       rights are granted.
                //
                accessStatus = HasRights(grantedRights, desiredRights, true);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode AccessCheck(
            string path,
            FileSystemRights desiredRights,
            ref FileSystemRights grantedRights,
            ref bool accessStatus,
            ref Result error
            )
        {
            //
            // NOTE: These must be reset prior to doing anything else in case
            //       further actions fail (by raising an exception).
            //
            grantedRights = NoFileSystemRights;
            accessStatus = false;

            try
            {
                //
                // NOTE: Make sure the file or directory exists before proceeding
                //       any further.
                //
                if (PathOps.PathExists(path))
                {
                    //
                    // NOTE: First, get the identity of the user for the current
                    //       thread.  Then, get the file security object for this
                    //       file or directory, using the correct method based on
                    //       whether this path represents a file or directory.
                    //       Finally, call the private method overload to perform
                    //       the rest of the file security checking.
                    //
                    return AccessCheck(
                        null, Directory.Exists(path) ? (FileSystemSecurity)
                        Directory.GetAccessControl(path) : File.GetAccessControl(
                        path), desiredRights, ref grantedRights, ref accessStatus,
                        ref error);
                }
                else
                {
                    error = String.Format(
                        "no such path {0}", FormatOps.WrapOrNull(path));
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode CheckReadOnlyAndDirectory(
            string path,
            ref bool exists,
            ref bool readOnly,
            ref bool directory,
            ref Result error
            )
        {
            try
            {
                FileAttributes fileAttributes = 0; /* NONE */

                bool newExists = PathOps.PathExists(path);

                if (newExists)
                    fileAttributes = File.GetAttributes(path);

                exists = newExists; /* NOTE: Transactional. */
                readOnly = FlagOps.HasFlags(fileAttributes, FileAttributes.ReadOnly, true);
                directory = FlagOps.HasFlags(fileAttributes, FileAttributes.Directory, true);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = String.Format(
                    "can't get attributes {0}: {1}",
                    FormatOps.WrapOrNull(path), e.Message);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode VerifyPath(
            string path,
            FilePermission permissions,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                bool exists = false;
                bool readOnly = false;
                bool directory = false;

                if (CheckReadOnlyAndDirectory(path, ref exists, ref readOnly, ref directory, ref error) == ReturnCode.Ok)
                {
                    if ((directory && FlagOps.HasFlags(permissions, FilePermission.Directory, true)) ||
                        (!directory && FlagOps.HasFlags(permissions, FilePermission.File, true)) ||
                        !FlagOps.HasFlags(permissions, FilePermission.Directory | FilePermission.File, false))
                    {
                        //
                        // NOTE: For directories, use 1 as the size so that we do not allow it to be
                        //       verified if it exists and the caller indicated that it must not exist.
                        //
                        //       For our purposes, we want to consider zero byte files to "not exist"
                        //       because of how temporary file names are allocated.
                        //
                        long fileSize = !directory ? GetFileSize(path) : 1;

                        if ((exists && FlagOps.HasFlags(permissions, FilePermission.Exists, true)) ||
                            ((!exists || (fileSize == 0)) && FlagOps.HasFlags(permissions, FilePermission.NotExists, true)) ||
                            !FlagOps.HasFlags(permissions, FilePermission.Exists | FilePermission.NotExists, false))
                        {
                            //
                            // NOTE: Are we asking for write permission on the file?
                            //
                            bool write = FlagOps.HasFlags(permissions, FilePermission.Write, true);

                            if (!write || !readOnly)
                            {
                                FileSystemRights desiredRights =
                                    FilePermissionsToFileSystemRights(permissions);

                                if (desiredRights != NoFileSystemRights)
                                {
                                    if (exists || write)
                                    {
                                        string originalPath = path; /* save */
                                        FileSystemRights grantedRights = NoFileSystemRights;
                                        bool accessStatus = false;

                                        //
                                        // NOTE: If the file does not exist, be sure we can create
                                        //       things in the parent directory.
                                        //
                                        if (!exists)
                                            path = Path.GetDirectoryName(path);

                                        if (!String.IsNullOrEmpty(path))
                                        {
                                            if (AccessCheck(
                                                    path, desiredRights, ref grantedRights,
                                                    ref accessStatus, ref error) == ReturnCode.Ok)
                                            {
                                                if (accessStatus)
                                                    return ReturnCode.Ok;
                                                else
                                                    error = String.Format(
                                                        "access denied: no rights to path {0}",
                                                        FormatOps.WrapOrNull(path));
                                            }
                                        }
                                        else
                                        {
                                            error = String.Format(
                                                "no directory in path {0}",
                                                FormatOps.WrapOrNull(originalPath));
                                        }
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "access denied: no such path {0}",
                                            FormatOps.WrapOrNull(path));
                                    }
                                }
                                else
                                {
                                    return ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "access denied: path {0} is read-only",
                                    FormatOps.WrapOrNull(path));
                            }
                        }
                        else
                        {
                            error = String.Format(exists ?
                                "path already exists {0}" : "no such path {0}",
                                FormatOps.WrapOrNull(path));
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "no such path {0}",
                            FormatOps.WrapOrNull(path));
                    }
                }
            }
            else
            {
                error = "invalid path";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region [glob] Command Support Methods
        private static bool HasGlobWildcard(
            string value
            )
        {
            return (value != null) &&
                (GlobWildcardChars != null) &&
                (value.IndexOfAny(GlobWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string EscapeGlobWildcards(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            if (GlobWildcardChars == null)
                return value;

            int index = value.IndexOfAny(GlobWildcardChars);

            if (index == Index.Invalid)
                return value;

            StringBuilder builder = StringOps.NewStringBuilder(
                value.Length * 2);

            int lastIndex = index;

            while (index != Index.Invalid)
            {
                if (index > lastIndex)
                {
                    builder.Append(
                        value, lastIndex + 1, index - lastIndex - 1);
                }

                builder.Append(Characters.Backslash);
                builder.Append(value, index, 1);

                lastIndex = index;
                index = value.IndexOfAny(GlobWildcardChars, index + 1);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetGlobFileName(
            string directory,
            FileSystemInfo fileSystemInfo,
            bool withDirectory,
            bool allowDrive
            )
        {
            if (fileSystemInfo == null)
                return null;

            string fileNameOnly = fileSystemInfo.Name;

            if (!withDirectory)
                return fileNameOnly;

            if (allowDrive &&
                PathOps.IsDriveLetterAndColon(directory, true))
            {
                return PathOps.GetUnixPath(String.Format("{0}{1}",
                    directory, fileNameOnly)); /* COMPAT: Tcl. */
            }
            else
            {
                return PathOps.GetUnixPath(PathOps.CombinePath(
                    true, directory, fileNameOnly));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SplitGlobPathPrefix(
            ref string pathPrefix, /* in, out */
            ref string directory   /* out */
            )
        {
            if (pathPrefix == null)
                return;

            int index = Index.Invalid;

            if (PathOps.EndsWithDirectory(pathPrefix, ref index) &&
                (index == (pathPrefix.Length - 1)))
            {
                directory = pathPrefix;
                pathPrefix = null;

                return;
            }

            string localPathPrefix;

            if (index == Index.Invalid)
            {
                localPathPrefix = pathPrefix;
                directory = null;
            }
            else
            {
                localPathPrefix = pathPrefix.Substring(index);
                directory = pathPrefix.Substring(0, index - 1);

                if (!PathOps.HasDirectory(directory))
                    directory = directory + pathPrefix[index];
            }

            pathPrefix = EscapeGlobWildcards(localPathPrefix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        private static FileSystemSecurity GetFileSystemSecurity(
            FileSystemInfo fileSystemInfo
            )
        {
            if (fileSystemInfo is DirectoryInfo)
                return ((DirectoryInfo)fileSystemInfo).GetAccessControl();

            if (fileSystemInfo is FileInfo)
                return ((FileInfo)fileSystemInfo).GetAccessControl();

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetGlobFileAttributes(
            FileSystemInfo fileSystemInfo,
            out bool isReadOnly,
            out bool isHidden,
            out bool isSystem,
            out bool isDirectory,
            out bool isArchive,
            out bool isDevice,
            out bool isNormal,
            out bool isTemporary,
            out bool isSparseFile,
            out bool isReparsePoint,
            out bool isCompressed,
            out bool isOffline,
            out bool isNotContentIndexed,
            out bool isEncrypted
            )
        {
            isReadOnly = false;
            isHidden = false;
            isSystem = false;
            isDirectory = false;
            isArchive = false;
            isDevice = false;
            isNormal = false;
            isTemporary = false;
            isSparseFile = false;
            isReparsePoint = false;
            isCompressed = false;
            isOffline = false;
            isNotContentIndexed = false;
            isEncrypted = false;

            if (fileSystemInfo == null)
                return;

            FileAttributes fileAttributes = fileSystemInfo.Attributes;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.ReadOnly, true))
                isReadOnly = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Hidden, true))
                isHidden = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.System, true))
                isSystem = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Directory, true))
                isDirectory = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Archive, true))
                isArchive = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Device, true))
                isDevice = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Normal, true))
                isNormal = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Temporary, true))
                isTemporary = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.SparseFile, true))
                isSparseFile = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.ReparsePoint, true))
                isReparsePoint = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Compressed, true))
                isCompressed = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Offline, true))
                isOffline = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.NotContentIndexed, true))
                isNotContentIndexed = true;

            if (FlagOps.HasFlags(fileAttributes, FileAttributes.Encrypted, true))
                isEncrypted = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MatchGlobFileTypes(
            Interpreter interpreter,
            IntDictionary types,
            FileSystemInfo fileSystemInfo
            )
        {
            if (fileSystemInfo == null)
                return false;

            if (types == null)
                return true;

            bool isReadOnly;
            bool isHidden;
            bool isSystem;
            bool isDirectory;
            bool isArchive;
            bool isDevice;
            bool isNormal;
            bool isTemporary;
            bool isSparseFile;
            bool isReparsePoint;
            bool isCompressed;
            bool isOffline;
            bool isNotContentIndexed;
            bool isEncrypted;

            GetGlobFileAttributes(
                fileSystemInfo, out isReadOnly, out isHidden, out isSystem,
                out isDirectory, out isArchive, out isDevice, out isNormal,
                out isTemporary, out isSparseFile, out isReparsePoint,
                out isCompressed, out isOffline, out isNotContentIndexed,
                out isEncrypted);

            ///////////////////////////////////////////////////////////////////
            // BEGIN EXCLUDED BY DEFAULT
            ///////////////////////////////////////////////////////////////////

            if (isSystem && !types.ContainsKey("system"))
                return false;

            if (isHidden && !types.ContainsKey("hidden"))
                return false;

            if (isDevice && !types.ContainsKey("device"))
                return false;

            ///////////////////////////////////////////////////////////////////
            // BEGIN INCLUDED BY DEFAULT
            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Assume the reparse point contains a link (junction).
            //
            if (isReparsePoint && types.ContainsKey("nonlink"))
                return false;

            if (!isReadOnly && types.ContainsKey("readonly"))
                return false;

            if (!isArchive && types.ContainsKey("archive"))
                return false;

            if (!isNormal && types.ContainsKey("normal"))
                return false;

            if (!isTemporary && types.ContainsKey("temporary"))
                return false;

            if (!isSparseFile && types.ContainsKey("sparsefile"))
                return false;

            if (!isCompressed && types.ContainsKey("compressed"))
                return false;

            if (!isOffline && types.ContainsKey("offline"))
                return false;

            if (!isNotContentIndexed && types.ContainsKey("notcontentindexed"))
                return false;

            if (!isEncrypted && types.ContainsKey("encrypted"))
                return false;

            /* directory */
            if (!isDirectory && types.ContainsKey(Characters.d.ToString()))
                return false;

            /* file */
            if (isDirectory && types.ContainsKey(Characters.f.ToString()))
                return false;

            //
            // HACK: Assume the reparse point contains a link (junction).
            //
            /* link */
            if (!isReparsePoint && types.ContainsKey(Characters.l.ToString()))
                return false;

            ///////////////////////////////////////////////////////////////////
            // BEGIN PERMISSIONS
            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                FileSystemRights desiredRights = NoFileSystemRights;

                /* read */
                if (types.ContainsKey(Characters.r.ToString()))
                    desiredRights |= FileSystemRights.Read;

                /* write */
                if (types.ContainsKey(Characters.w.ToString()))
                    desiredRights |= FileSystemRights.Write;

                /* execute */
                if (types.ContainsKey(Characters.x.ToString()))
                {
                    desiredRights |= (isDirectory ?
                        FileSystemRights.Traverse : FileSystemRights.ExecuteFile);
                }

                /* permissions */
                if (desiredRights != NoFileSystemRights)
                {
                    ReturnCode accessCode;
                    FileSystemRights grantedRights = NoFileSystemRights;
                    bool accessStatus = false;
                    Result accessError = null;

                    accessCode = AccessCheck(
                        null, GetFileSystemSecurity(fileSystemInfo),
                        desiredRights, ref grantedRights, ref accessStatus,
                        ref accessError);

                    if (accessCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(interpreter, accessCode, accessError);
                        return false;
                    }

                    return accessStatus;
                }
            }
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static FileSystemInfoDictionary GetGlobFileSystemInfos(
            DirectoryInfo directoryInfo,
            string directory,
            bool includeDirectories,
            bool includeFiles,
            bool includeSpecial,
            bool withDirectory,
            bool allowDrive
            )
        {
            FileSystemInfoDictionary fileSystemInfos = null;

            try
            {
                if (directoryInfo == null)
                    return null;

                fileSystemInfos = new FileSystemInfoDictionary();

                if (includeDirectories && includeFiles)
                {
                    foreach (FileSystemInfo fileSystemInfo in
                            directoryInfo.GetFileSystemInfos())
                    {
                        fileSystemInfos.Add(GetGlobFileName(
                            directory, fileSystemInfo, withDirectory,
                            allowDrive), fileSystemInfo);
                    }
                }
                else if (includeDirectories)
                {
                    foreach (FileSystemInfo fileSystemInfo in
                            directoryInfo.GetDirectories())
                    {
                        fileSystemInfos.Add(GetGlobFileName(
                            directory, fileSystemInfo, withDirectory,
                            allowDrive), fileSystemInfo);
                    }
                }
                else if (includeFiles)
                {
                    foreach (FileSystemInfo fileSystemInfo in
                            directoryInfo.GetFiles())
                    {
                        fileSystemInfos.Add(GetGlobFileName(
                            directory, fileSystemInfo, withDirectory,
                            allowDrive), fileSystemInfo);
                    }
                }

                if (includeDirectories && includeSpecial &&
                    (PlatformOps.IsWindowsOperatingSystem() ||
                    PlatformOps.IsUnixOperatingSystem()))
                {
                    DirectoryInfo currentDirectoryInfo = new DirectoryInfo(
                        PathOps.CurrentDirectory);

                    fileSystemInfos.Add(withDirectory ? PathOps.GetUnixPath(
                        PathOps.CombinePath(true, PathOps.CurrentDirectory,
                        PathOps.CurrentDirectory)) : PathOps.CurrentDirectory,
                        currentDirectoryInfo);

                    DirectoryInfo parentDirectoryInfo = new DirectoryInfo(
                        PathOps.ParentDirectory);

                    fileSystemInfos.Add(withDirectory ? PathOps.GetUnixPath(
                        PathOps.CombinePath(true, PathOps.CurrentDirectory,
                        PathOps.ParentDirectory)) : PathOps.ParentDirectory,
                        parentDirectoryInfo);
                }
            }
            catch (PathTooLongException)
            {
                // do nothing.
            }
            catch (DirectoryNotFoundException)
            {
                // do nothing.
            }
            catch (FileNotFoundException)
            {
                // do nothing.
            }
#if MONO || MONO_HACKS
            catch (IOException) /* HACK: Mono 4.x / 5.x. */
            {
                // do nothing.
            }
#endif

            return fileSystemInfos;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DirectoryInfo GetDirectoryInfo(
            string directory /* in: OPTIONAL */
            )
        {
            return (directory != null) ?
                new DirectoryInfo(directory) : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode DoGlobFiles(
            Interpreter interpreter,
            string pattern,
            DirectoryInfo directoryInfo,
            IntDictionary types,
            string pathPrefix,
            string directory,
            StringList fileNames,
            int level,
            bool tailOnly,
            bool withDirectory,
            bool allowDrive,
            bool allowCurrent,
            ref Result error
            )
        {
            if (pattern == null)
            {
                error = "invalid glob pattern";
                return ReturnCode.Error;
            }

            bool hasTilde = (pattern.Length > 0) &&
                (pattern[0] == Characters.Tilde);

            bool havePrefix = !String.IsNullOrEmpty(pathPrefix);

            if (hasTilde)
            {
                if (havePrefix || withDirectory)
                {
                    error = String.Format(
                        "no files matched glob pattern {0}",
                        FormatOps.WrapOrNull(pattern));

                    return ReturnCode.Error;
                }

                string originalPattern = pattern;

                pattern = PathOps.GetUnixPath(PathOps.TildeSubstitution(
                    interpreter, pattern, true, true));

                if (pattern == null)
                {
                    error = String.Format(
                        "user {0} doesn't exist",
                        FormatOps.WrapOrNull(originalPattern.Substring(1)));

                    return ReturnCode.Error;
                }
            }

            //
            // NOTE: Are we allowing the special "." and ".." entries to be
            //       found?
            //
            bool matchSpecialDots = (pattern.Length > 0) &&
                (pattern[0] == Characters.Period);

            if (PathOps.HasDirectory(pattern) || (allowDrive &&
                PathOps.IsDriveLetterAndColon(pattern, false)))
            {
                //
                // NOTE: Since this pattern is qualified with a directory
                //       name, we must return fully qualified file names
                //       for them to be meaningful.
                //
                // HACK: The directory separator character used here cannot
                //       be a backslash, as that character is reserved for
                //       glob matching.
                //
                string patternPrefix = null;
                string patternDirectory = null;
                string patternFileName = null;

                PathOps.SplitPathRaw(pattern,
                    PathOps.AltDirectorySeparatorChar.ToString(),
                    allowDrive, allowCurrent, out patternPrefix,
                    out patternDirectory, out patternFileName);

                if (HasGlobWildcard(patternDirectory) ||
                    PathOps.HasPathWildcard(patternDirectory))
                {
                    FileSystemInfoDictionary childFileSystemInfos =
                        GetGlobFileSystemInfos(directoryInfo, directory,
                            true, false, false, (level > 1) || withDirectory,
                            allowDrive);

                    if (childFileSystemInfos != null)
                    {
                        if (matchSpecialDots)
                        {
                            childFileSystemInfos.Add(PathOps.CurrentDirectory,
                                new DirectoryInfo(PathOps.CurrentDirectory));

                            childFileSystemInfos.Add(PathOps.ParentDirectory,
                                new DirectoryInfo(PathOps.ParentDirectory));
                        }

                        foreach (KeyValuePair<string, FileSystemInfo> pair in
                                childFileSystemInfos)
                        {
                            string childDirectory = pair.Key;

                            if (childDirectory == null) /* IMPOSSIBLE */
                                continue;

                            StringList subPatternDirectories = null;

                            if (StringOps.SplitSubPatterns(
                                    patternDirectory, 0, true,
                                    ref subPatternDirectories,
                                    ref error) == ReturnCode.Ok)
                            {
                                //
                                // NOTE: When no sub-patterns are found, use the
                                //       directory portion of the original pattern,
                                //       verbatim.
                                //
                                if (subPatternDirectories == null)
                                {
                                    subPatternDirectories = new StringList(
                                        patternDirectory);
                                }

                                foreach (string subPattern in subPatternDirectories)
                                {
                                    if (!StringOps.Match(
                                            interpreter, MatchMode.Glob,
                                            childDirectory, subPattern,
                                            PathOps.NoCase))
                                    {
                                        continue;
                                    }

                                    DirectoryInfo childDirectoryInfo =
                                        pair.Value as DirectoryInfo;

                                    if (childDirectoryInfo == null)
                                        continue;

                                    if (DoGlobFiles( /* RECURSION */
                                            interpreter, patternFileName,
                                            childDirectoryInfo, types,
                                            pathPrefix, childDirectory,
                                            fileNames, level + 1, false,
                                            withDirectory, allowDrive,
                                            allowCurrent,
                                            ref error) != ReturnCode.Ok)
                                    {
                                        return ReturnCode.Error;
                                    }
                                }
                            }
                        }
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    return DoGlobFiles( /* RECURSION */
                        interpreter, patternFileName,
                        GetDirectoryInfo(
                            patternDirectory),
                        types, pathPrefix,
                        (patternPrefix != null) ?
                            patternPrefix :
                            patternDirectory,
                        fileNames, level + 1, false,
                        withDirectory, allowDrive,
                        allowCurrent, ref error);
                }
            }
            else
            {
                StringList subPatterns = null;

                if (StringOps.SplitSubPatterns(
                        pattern, 0, true, ref subPatterns,
                        ref error) == ReturnCode.Ok)
                {
                    if (subPatterns != null)
                    {
                        foreach (string subPattern in subPatterns)
                        {
                            if (DoGlobFiles( /* RECURSION */
                                    interpreter, subPattern,
                                    directoryInfo, types,
                                    pathPrefix, directory,
                                    fileNames, level + 1,
                                    tailOnly, withDirectory,
                                    allowDrive, allowCurrent,
                                    ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }
                        }

                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            FileSystemInfoDictionary fileSystemInfos = GetGlobFileSystemInfos(
                directoryInfo, directory, true, true, true,
                (level > 1) || withDirectory, allowDrive);

            if (fileSystemInfos != null)
            {
                foreach (KeyValuePair<string, FileSystemInfo> pair in
                        fileSystemInfos)
                {
                    FileSystemInfo fileSystemInfo = pair.Value;

                    if (fileSystemInfo == null)
                        continue;

                    string originalFileName = pair.Key;

                    if (originalFileName == null) /* IMPOSSIBLE */
                        continue;

                    //
                    // BUGBUG: Is this handling correct for the "." and ".."
                    //         directory entries here?  Must use the original
                    //         file name here.
                    //
                    bool isCurrentDirectory = PathOps.IsEqualFileName(
                        Path.GetFileName(originalFileName),
                        PathOps.CurrentDirectory);

                    bool isParentDirectory = PathOps.IsEqualFileName(
                        Path.GetFileName(originalFileName),
                        PathOps.ParentDirectory);

                    //
                    // NOTE: The file name to match and potentially add to the
                    //       result list start out as the original file name.
                    //
                    string matchFileName = originalFileName;
                    string addFileName = originalFileName;

                    //
                    // NOTE: First, attempt to match the specified literal path
                    //       prefix, if any.
                    //
                    if (havePrefix)
                    {
                        //
                        // NOTE: If the length of the file name is too small
                        //       for the prefix, there is no point in going
                        //       any further.
                        //
                        if (matchFileName.Length < pathPrefix.Length)
                            continue;

                        //
                        // NOTE: If the specified path prefix does not match,
                        //       this file name cannot be a match.
                        //
                        if (!SharedStringOps.Equals(
                                matchFileName, 0, pathPrefix, 0,
                                pathPrefix.Length, PathOps.ComparisonType))
                        {
                            continue;
                        }

                        //
                        // NOTE: Skip the entire path prefix portion of the
                        //       file name, for matching purposes.
                        //
                        matchFileName = matchFileName.Substring(pathPrefix.Length);
                    }
                    else
                    {
                        //
                        // BUGBUG: When not using the -path prefix, we should
                        //         only consider the file name portion for
                        //         pattern matching?
                        //
                        matchFileName = Path.GetFileName(matchFileName);
                    }

                    //
                    // NOTE: Check for the various special cases involving the "."
                    //       and ".." directory entries.
                    //
                    if (!havePrefix && isCurrentDirectory)
                    {
                        if (!matchSpecialDots)
                            continue;
                        else if (!withDirectory)
                            addFileName = PathOps.CurrentDirectory;
                    }
                    else if (!havePrefix && isParentDirectory)
                    {
                        if (!matchSpecialDots)
                            continue;
                        else if (!withDirectory)
                            addFileName = PathOps.ParentDirectory;
                    }

                    //
                    // NOTE: This call into the "glob-style" matching engine
                    //       should handle all the syntax we support, except
                    //       the curly-brace extension used by [glob].  That
                    //       is handled above, using SplitGlobSubPatterns().
                    //
                    if (!StringOps.Match(
                            interpreter, MatchMode.Glob,
                            matchFileName, pattern, PathOps.NoCase))
                    {
                        continue;
                    }

                    //
                    // NOTE: Next, check if any type filtering needs to be done.
                    //       If so, make sure the types match.
                    //
                    if (!MatchGlobFileTypes(interpreter, types, fileSystemInfo))
                        continue;

                    //
                    // NOTE: At this point, we have a matching file or directory.
                    //
                    if (tailOnly)
                        fileNames.Add(Path.GetFileName(addFileName));
                    else
                        fileNames.Add(addFileName);
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList GlobFiles(
            Interpreter interpreter, /* in */
            StringList patterns,     /* in */
            IntDictionary types,     /* in: may be NULL. */
            string pathPrefix,       /* in: may be NULL. */
            string directory,        /* in: may be NULL. */
            bool join,               /* in */
            bool tailOnly,           /* in */
            bool allowDrive,         /* in */
            bool allowCurrent,       /* in */
            bool errorOnNotFound,    /* in */
            ref Result error         /* out */
            )
        {
            if (patterns == null)
            {
                error = "invalid pattern list";
                return null;
            }

            StringList fileNames = new StringList();
            bool withDirectory = (directory != null);

            SplitGlobPathPrefix(ref pathPrefix, ref directory);

            if (String.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            if (join)
            {
                if (DoGlobFiles(
                        interpreter, PathOps.GetUnixPath(
                        PathOps.CombinePath(true, patterns)),
                        directoryInfo, types, pathPrefix,
                        withDirectory ? directory : null,
                        fileNames, 1, tailOnly, withDirectory,
                        allowDrive, allowCurrent,
                        ref error) != ReturnCode.Ok)
                {
                    return null;
                }
            }
            else
            {
                foreach (string pattern in patterns)
                {
                    if (DoGlobFiles(
                            interpreter, pattern, directoryInfo,
                            types, pathPrefix, withDirectory ?
                                directory : null, fileNames, 1,
                            tailOnly, withDirectory, allowDrive,
                            allowCurrent, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }
                }
            }

            if (!errorOnNotFound || (fileNames.Count > 0))
            {
                return fileNames;
            }
            else
            {
                error = String.Format(
                    "no files matched glob pattern{0} {1}",
                    (join || (patterns.Count > 1)) ? "s" :
                    String.Empty, FormatOps.WrapOrNull(
                    patterns.ToRawString(
                    Characters.Space.ToString())));

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static SearchOption GetSearchOption(
            bool recursive
            )
        {
            return recursive ?
                SearchOption.AllDirectories :
                SearchOption.TopDirectoryOnly;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long GetFileSize(
            string path
            )
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path); /* throw */

                return fileInfo.Length; /* throw */
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static byte[] GetFileBytes(
            string path,
            int count
            )
        {
            if (String.IsNullOrEmpty(path))
                return null;

            if (count == 0)
                return new byte[0];

            long fileSize = GetFileSize(path);

            if ((fileSize == 0) || (count > fileSize))
                return null;

            try
            {
                if (count < 0)
                    return File.ReadAllBytes(path);

                using (FileStream fileStream = File.OpenRead(path))
                {
                    byte[] bytes = new byte[count];

                    fileStream.Read(bytes, 0, count);

                    return bytes;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FileOps).Name,
                    TracePriority.FileSystemError);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetFileType(
            string path
            )
        {
            if (Directory.Exists(path))
                return "directory";
            else if (File.Exists(path))
                return "file";
            else
                return "unknown";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static FileAccess FileAccessFromAccess(
            MapOpenAccess access
            )
        {
            if (FlagOps.HasFlags(access, MapOpenAccess.RdWr, true))
                return FileAccess.ReadWrite;

            if (FlagOps.HasFlags(access, MapOpenAccess.WrOnly, true))
                return FileAccess.Write;

            return FileAccess.Read;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static FileMode FileModeFromAccess(
            MapOpenAccess access
            )
        {
            //
            // NOTE: The MapOpenAccess.Append flag is not handled by this function
            //       as it requires special treatment to get POSIX-like behavior.
            //
            switch (access & (MapOpenAccess.Creat | MapOpenAccess.Excl | MapOpenAccess.Trunc))
            {
                case (MapOpenAccess.Creat | MapOpenAccess.Excl):
                case (MapOpenAccess.Creat | MapOpenAccess.Excl | MapOpenAccess.Trunc):
                    //
                    // NOTE: Create new file, error if file exists.
                    //
                    return FileMode.CreateNew;

                case (MapOpenAccess.Creat | MapOpenAccess.Trunc):
                    //
                    // NOTE: Create new file, overwrite if file exists.
                    //
                    return FileMode.Create;

                case MapOpenAccess.Creat:
                    //
                    // NOTE: Create a new file or open existing file.
                    //
                    return FileMode.OpenOrCreate;

                case MapOpenAccess.Trunc:
                case (MapOpenAccess.Trunc | MapOpenAccess.Excl):
                    //
                    // NOTE: Open existing file and truncate to zero length,
                    //       error if file does not exist.
                    //
                    return FileMode.Truncate;
            }

            //
            // NOTE: Open existing file, error if file does not exist.
            //
            return FileMode.Open;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HaveFileAttributes(
            FileAttributes attributes,
            FileAttributes haveAttributes
            )
        {
            return ((attributes & haveAttributes) == haveAttributes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetFileAttributes(
            string path,
            FileAttributes fileAttributes,
            ref Result error
            )
        {
            try
            {
                File.SetAttributes(path, fileAttributes);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = String.Format(
                    "can't set attributes {0}: {1}",
                    FormatOps.WrapOrNull(path), e.Message);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetFileAttributes(
            string path,
            ref FileAttributes fileAttributes,
            ref Result error
            )
        {
            try
            {
                fileAttributes = File.GetAttributes(path);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = String.Format(
                    "can't get attributes {0}: {1}",
                    FormatOps.WrapOrNull(path), e.Message);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CleanupDirectory(
            string directory,             /* in */
            IEnumerable<string> patterns, /* in */
            bool recursive                /* in */
            )
        {
            try
            {
                if (String.IsNullOrEmpty(directory))
                    return false;

                if (!Directory.Exists(directory))
                    return false;

                SearchOption searchOption = GetSearchOption(
                    recursive);

                if (patterns != null)
                {
                    foreach (string pattern in patterns)
                    {
                        if (pattern == null)
                            continue;

                        string[] fileNames = Directory.GetFiles(
                            directory, pattern, searchOption);

                        if (fileNames == null)
                            continue;

                        foreach (string fileName in fileNames)
                        {
                            if (String.IsNullOrEmpty(fileName))
                                continue;

                            if (!File.Exists(fileName))
                                continue;

                            try
                            {
                                File.Delete(fileName); /* throw */
                            }
                            catch (Exception e2)
                            {
                                TraceOps.DebugTrace(
                                    e2, typeof(FileOps).Name,
                                    TracePriority.FileSystemError);
                            }
                        }
                    }
                }

                string[] subDirectories = Directory.GetDirectories(
                    directory, Characters.Asterisk.ToString(),
                    searchOption);

                foreach (string subDirectory in subDirectories)
                {
                    if (!Directory.Exists(subDirectory))
                        continue;

                    try
                    {
                        Directory.Delete(subDirectory); /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(FileOps).Name,
                            TracePriority.FileSystemError);
                    }
                }

                Directory.Delete(directory); /* throw */
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FileOps).Name,
                    TracePriority.FileSystemError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FileDelete(
            IList paths,
            bool recursive,
            bool force,
            bool noComplain,
            ref Result error
            )
        {
            string pathType = null;

            return FileDelete(
                paths, recursive, force, noComplain, ref pathType, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FileDelete(
            IList paths,
            bool recursive,
            bool force,
            bool noComplain,
            ref string pathType,
            ref Result error
            )
        {
            if ((paths != null) && (paths.Count > 0))
            {
                for (int index = 0; index < paths.Count; index++)
                {
                    if (paths[index] != null)
                    {
                        string path = paths[index].ToString();

                        try
                        {
                            if (File.Exists(path))
                            {
                                if (force)
                                {
                                    FileAttributes fileAttributes = File.GetAttributes(path);

                                    if (FlagOps.HasFlags(fileAttributes, FileAttributes.ReadOnly, true))
                                    {
                                        fileAttributes &= ~FileAttributes.ReadOnly;
                                        File.SetAttributes(path, fileAttributes);
                                    }
                                }

                                File.Delete(path);
                                pathType = PathType.File.ToString();
                            }
                            else if (Directory.Exists(path))
                            {
                                if (recursive && force)
                                {
                                    string[] fileNames = Directory.GetFiles(
                                        path, Characters.Asterisk.ToString(),
                                        GetSearchOption(true));

                                    if (fileNames != null)
                                    {
                                        foreach (string fileName in fileNames)
                                        {
                                            if (String.IsNullOrEmpty(fileName))
                                                continue;

                                            FileAttributes fileAttributes = File.GetAttributes(fileName);

                                            if (FlagOps.HasFlags(fileAttributes, FileAttributes.ReadOnly, true))
                                            {
                                                fileAttributes &= ~FileAttributes.ReadOnly;
                                                File.SetAttributes(fileName, fileAttributes);
                                            }
                                        }
                                    }
                                }

                                Directory.Delete(path, recursive);

                                pathType = String.Format(
                                    "{0}{1}", recursive ? "RECURSIVE " : String.Empty,
                                    PathType.Directory);
                            }
                            else
                            {
                                if (!noComplain)
                                {
                                    error = String.Format(
                                        "error deleting {0}: no such file or directory",
                                        FormatOps.WrapOrNull(path));

                                    return ReturnCode.Error;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (!noComplain)
                            {
                                error = String.Format(
                                    "error deleting {0}: {1}",
                                    FormatOps.WrapOrNull(path), e.Message);

                                return ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        if (!noComplain)
                        {
                            error = "invalid file name";

                            return ReturnCode.Error;
                        }
                    }
                }
            }
            else
            {
                if (!noComplain)
                {
                    error = "no files";

                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FileCopy(
            IList fileNames,
            string path,
            bool move,
            bool force,
            ref Result error
            )
        {
            if ((fileNames != null) && (fileNames.Count > 0))
            {
                if (!String.IsNullOrEmpty(path))
                {
                    if (fileNames.Count > 1)
                    {
                        if (Directory.Exists(path))
                        {
                            for (int index = 0; index < fileNames.Count; index++)
                            {
                                if (fileNames[index] != null)
                                {
                                    string fileName = fileNames[index].ToString();

                                    if (File.Exists(fileName))
                                    {
                                        try
                                        {
                                            File.Copy(fileName, PathOps.CombinePath(
                                                null, path, Path.GetFileName(fileName)), force);

                                            if (move)
                                                File.Delete(fileName);
                                        }
                                        catch (Exception e)
                                        {
                                            error = String.Format(
                                                "error {0} {1}: {2}",
                                                move ? "moving" : "copying",
                                                FormatOps.WrapOrNull(fileName), e.Message);

                                            return ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "error {0} {1}: no such file",
                                            move ? "moving" : "copying",
                                            FormatOps.WrapOrNull(fileName));

                                        return ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "invalid file name";

                                    return ReturnCode.Error;
                                }
                            }

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = String.Format(
                                "error {0}: target {1} is not a directory",
                                move ? "moving" : "copying",
                                FormatOps.WrapOrNull(path));

                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (fileNames[0] != null)
                        {
                            string fileName = fileNames[0].ToString();

                            try
                            {
                                File.Copy(fileName, path, force);

                                if (move)
                                    File.Delete(fileName);

                                return ReturnCode.Ok;
                            }
                            catch (Exception e)
                            {
                                error = String.Format(
                                    "error {0} {1}: {2}",
                                    move ? "moving" : "copying",
                                    FormatOps.WrapOrNull(fileName), e.Message);

                                return ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "invalid file name";

                            return ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    error = "invalid target";

                    return ReturnCode.Error;
                }
            }
            else
            {
                error = "no files";

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetFileTime(
            string path,
            GetDateTimeCallback callback,
            ref DateTime dateTime,
            ref Result error
            )
        {
            try
            {
                dateTime = callback(path);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = String.Format(
                    "can't get file time {0}: {1}",
                    FormatOps.WrapOrNull(path), e.Message);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetFileTime(
            string path,
            SetDateTimeCallback callback,
            DateTime dateTime,
            ref Result error
            )
        {
            try
            {
                callback(path, dateTime);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = String.Format(
                    "can't set file time {0}: {1}",
                    FormatOps.WrapOrNull(path), e.Message);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetFileSystemEntries(
            string path,
            string searchPattern,
            ref StringList entries,
            ref Result error
            )
        {
            if (Directory.Exists(path))
            {
                try
                {
                    if (searchPattern != null)
                        entries = new StringList(
                            PathOps.GetUnixPath,
                            Directory.GetFileSystemEntries(path, searchPattern));
                    else
                        entries = new StringList(
                            PathOps.GetUnixPath,
                            Directory.GetFileSystemEntries(path));

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = String.Format(
                        "can't get entries {0}: {1}",
                        FormatOps.WrapOrNull(path), e.Message);

                    return ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "can't get entries {0}: no such directory",
                    FormatOps.WrapOrNull(path));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static FieldInfo FindByteBufferFieldInfo()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                foreach (string fieldName in byteBufferFieldNames)
                {
                    if (fieldName == null)
                        continue;

                    FieldInfo fieldInfo = null;

                    try
                    {
                        fieldInfo = typeof(StreamReader).GetField(
                            fieldName, ObjectOps.GetBindingFlags(
                                MetaBindingFlags.ByteBuffer,
                                true)); /* throw */
                    }
                    catch
                    {
                        // do nothing.
                    }

                    if (fieldInfo != null)
                        return fieldInfo;
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryGrabByteBuffer(
            StreamReader streamReader,
            ref ByteList bytes,
            ref Result error
            )
        {
            if (streamReader != null)
            {
                try
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (byteBufferFieldInfo == null)
                            byteBufferFieldInfo = FindByteBufferFieldInfo();

                        if (byteBufferFieldInfo != null)
                        {
                            byte[] buffer = byteBufferFieldInfo.GetValue(
                                streamReader) as byte[];

                            if (buffer != null)
                            {
                                if (bytes == null)
                                    bytes = new ByteList();

                                bytes.AddRange(buffer);
                                return true;
                            }
                            else
                            {
                                error = String.Format(
                                    "invalid byte buffer from {0}",
                                    MarshalOps.GetErrorMemberName(
                                    byteBufferFieldInfo));
                            }
                        }
                        else
                        {
                            error = String.Format(
                                "missing byte buffer field from: {0}",
                                new StringList(byteBufferFieldNames));
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return false;
        }
    }
}
