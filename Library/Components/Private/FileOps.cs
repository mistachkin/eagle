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

using CleanupPathPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Components.Private.CleanupPathClientData>;

using CleanupPathPairs = System.Collections.Generic.IEnumerable<
    System.Collections.Generic.KeyValuePair<string,
        Eagle._Components.Private.CleanupPathClientData>>;

using FilePair = System.Collections.Generic.KeyValuePair<string, object>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of private file system helper methods
    /// used internally by the Eagle core library, including support for
    /// querying portable executable (PE) file metadata, checking and
    /// manipulating file system access rights, implementing the various forms
    /// of the [glob] command, and performing common file and directory
    /// operations (copy, move, delete, touch, attribute and time queries).
    /// </summary>
    [ObjectId("814d0e8c-c65a-4c7c-8c2e-2e6cee551509")]
    internal static class FileOps
    {
        #region Private StreamReader Support Constants
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class from multiple threads.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if MONO || NET_STANDARD_20
        //
        // HACK: Newer versions (6.0+) of the Mono runtime use the same code
        //       as .NET Core for the StreamReader class; therefore, try its
        //       name first ("_byteBuffer") when targeted to those platforms.
        //
        /// <summary>
        /// The candidate names of the private byte buffer field within the
        /// <see cref="StreamReader" /> class, in the order they should be
        /// tried when reflecting that field on Mono or .NET Standard targets.
        /// </summary>
        private static string[] byteBufferFieldNames = {
            "_byteBuffer", "byteBuffer", null, null
        };
#else
        //
        // NOTE: The desktop .NET Framework StreamReader class still uses the
        //       legacy name ("byteBuffer") as of v4.8.0; therefore, try that
        //       name first when targeted to that platform.
        //
        /// <summary>
        /// The candidate names of the private byte buffer field within the
        /// <see cref="StreamReader" /> class, in the order they should be
        /// tried when reflecting that field on the desktop .NET Framework.
        /// </summary>
        private static string[] byteBufferFieldNames = {
            "byteBuffer", "_byteBuffer", null, null
        };
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The cached reflected field information for the private byte buffer
        /// field of the <see cref="StreamReader" /> class, or null if it has
        /// not yet been resolved.
        /// </summary>
        private static FieldInfo byteBufferFieldInfo = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private [glob] Support Collection Classes
        /// <summary>
        /// This class represents a dictionary that maps file system path names
        /// to their associated file system information objects, for use while
        /// processing the [glob] command.
        /// </summary>
        [ObjectId("64966544-0142-4f06-a335-63baa4c14293")]
        private sealed class FileSystemInfoDictionary : PathDictionary<object>
        {
            // nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The EXE header signature.
        //
        /// <summary>
        /// The signature value found at the start of an MS-DOS executable
        /// header (the ASCII characters "MZ").
        /// </summary>
        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // "MZ"

        //
        // NOTE: The PE header signature.
        //
        /// <summary>
        /// The signature value found at the start of a portable executable
        /// (PE) header (the ASCII characters "PE" followed by two null bytes).
        /// </summary>
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // "PE\0\0"

        //
        // NOTE: This "magic" value means that we have no idea what
        //       the value for the file (or operating system) is.
        //
        /// <summary>
        /// The sentinel "magic" value indicating that the architecture of the
        /// file (or operating system) is unknown.
        /// </summary>
        internal const ushort IMAGE_NT_OPTIONAL_BAD_MAGIC = 0x0;

        //
        // NOTE: The "magic" values from the PE header for 32-bit
        //       and 64-bit executables.
        //
        /// <summary>
        /// The "magic" value found in the optional header of a 32-bit portable
        /// executable (PE) file.
        /// </summary>
        private const ushort IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x010B;

        /// <summary>
        /// The "magic" value found in the optional header of a 64-bit portable
        /// executable (PE) file.
        /// </summary>
        private const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x020B;

        //
        // NOTE: The offset into the file where the offset into
        //       the file of the PE header is located.
        //
        /// <summary>
        /// The byte offset into the file at which the offset of the portable
        /// executable (PE) header is located.
        /// </summary>
        private const int filePeSignatureOffsetOffset = 0x3C;

        //
        // NOTE: The offset for the TimeDateStamp field in the
        //       IMAGE_FILE_HEADER structure from the start of the
        //       PE signature.
        //
        /// <summary>
        /// The byte offset of the TimeDateStamp field in the IMAGE_FILE_HEADER
        /// structure, measured from the start of the PE signature.
        /// </summary>
        private const int peTimeStampOffset = 0x8;

        //
        // NOTE: The offset for the Magic field in the
        //       IMAGE_OPTIONAL_HEADER structure from the start of
        //       the PE signature.
        //
        /// <summary>
        /// The byte offset of the Magic field in the IMAGE_OPTIONAL_HEADER
        /// structure, measured from the start of the PE signature.
        /// </summary>
        private const int peMagicOffset = 0x18;

        //
        // NOTE: The offset for the CLR header virtual address from
        //       the start of the IMAGE_OPTIONAL_HEADER structure.
        //
        /// <summary>
        /// The byte offset of the CLR header virtual address from the start of
        /// the IMAGE_OPTIONAL_HEADER structure for a 32-bit executable.
        /// </summary>
        private const int peClrHeaderOffset32 = 0xD0;

        /// <summary>
        /// The byte offset of the CLR header virtual address from the start of
        /// the IMAGE_OPTIONAL_HEADER structure for a 64-bit executable.
        /// </summary>
        private const int peClrHeaderOffset64 = 0xE0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: The offset for the SizeOfStackReserve field in the
        //       IMAGE_OPTIONAL_HEADER structure from the start of
        //       the PE signature.  This value just happens to be
        //       the same for 32-bit and 64-bit executables.
        //
        /// <summary>
        /// The byte offset of the SizeOfStackReserve field in the
        /// IMAGE_OPTIONAL_HEADER structure, measured from the start of the PE
        /// signature.  This value is the same for 32-bit and 64-bit
        /// executables.
        /// </summary>
        private const int peReserveOffset = 0x60;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The offset for the SizeOfStackCommit field in the
        //       IMAGE_OPTIONAL_HEADER structure from the start of
        //       the PE signature.  This value is different for
        //       32-bit and 64-bit executables.
        //
        /// <summary>
        /// The byte offset of the SizeOfStackCommit field in the
        /// IMAGE_OPTIONAL_HEADER structure for a 32-bit executable, measured
        /// from the start of the PE signature.
        /// </summary>
        private const int peCommitOffset32Bit = 0x64;

        /// <summary>
        /// The byte offset of the SizeOfStackCommit field in the
        /// IMAGE_OPTIONAL_HEADER structure for a 64-bit executable, measured
        /// from the start of the PE signature.
        /// </summary>
        private const int peCommitOffset64Bit = 0x68;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These strings are only used by GetPeFileMagicName.
        //
        /// <summary>
        /// The human-readable name returned for the "magic" value of a 32-bit
        /// portable executable (PE) file.
        /// </summary>
        private const string magicPe32 = "PE32";

        /// <summary>
        /// The human-readable name returned for the "magic" value of a 64-bit
        /// portable executable (PE) file.
        /// </summary>
        private const string magicPe32Plus = "PE32+";

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        /// <summary>
        /// Represents the empty set of file system rights (i.e. no rights at
        /// all).
        /// </summary>
        internal static readonly FileSystemRights NoFileSystemRights =
            (FileSystemRights)0; /* None */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Represents the complete set of file system rights that the .NET
        /// Framework is able to handle, used to detect and skip rights values
        /// it cannot process.
        /// </summary>
        private static readonly FileSystemRights AllFileSystemRights =
            FileSystemRights.ListDirectory |
            FileSystemRights.ReadData |
            FileSystemRights.CreateFiles |
            FileSystemRights.WriteData |
            FileSystemRights.CreateDirectories |
            FileSystemRights.AppendData |
            FileSystemRights.ReadExtendedAttributes |
            FileSystemRights.WriteExtendedAttributes |
            FileSystemRights.ExecuteFile |
            FileSystemRights.Traverse |
            FileSystemRights.DeleteSubdirectoriesAndFiles |
            FileSystemRights.ReadAttributes |
            FileSystemRights.WriteAttributes |
            FileSystemRights.Write |
            FileSystemRights.Delete |
            FileSystemRights.ReadPermissions |
            FileSystemRights.Read |
            FileSystemRights.ReadAndExecute |
            FileSystemRights.Modify |
            FileSystemRights.ChangePermissions |
            FileSystemRights.TakeOwnership |
            FileSystemRights.Synchronize |
            FileSystemRights.FullControl;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, only "normal" file system paths are returned by the
        /// [glob] command, filtering out "special" paths (e.g. reparse points
        /// and other non-normal entries).
        /// </summary>
        private static bool GlobNormalPathsOnly = true;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, "synthetic" file attributes (those fabricated by the
        /// .NET runtime on non-Windows operating systems, such as the hidden
        /// flag for "dotfiles") are ignored during [glob] type matching.
        /// </summary>
        private static bool GlobIgnoreSyntheticAttributes = true;

        /// <summary>
        /// When non-zero, the file attributes reported by the operating system
        /// are treated as Windows-style (non-synthetic) attributes; this is
        /// initialized based on whether the current operating system is
        /// Windows.
        /// </summary>
        private static bool GlobWindowsSyntheticAttributes =
            PlatformOps.IsWindowsOperatingSystem();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of characters that are treated as glob wildcard or escape
        /// metacharacters, used to detect and escape them within path prefixes.
        /// </summary>
        private static readonly char[] GlobWildcardChars = {
            Characters.OpenBracket,
            Characters.Backslash,
            Characters.CloseBracket,
            Characters.OpenBrace,
            Characters.CloseBrace
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// The cached native stack size (reserve and commit) extracted from the
        /// PE header of the main Eagle assembly, or null if it has not yet been
        /// initialized.
        /// </summary>
        private static NativeStack.StackSize PeFileStackSize;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the static state of this class, optionally
        /// forcing the cached PE file stack size to be recomputed.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the cached values to be recomputed even if they
        /// have already been initialized.
        /// </param>
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

        /// <summary>
        /// This method returns the human-readable name corresponding to a
        /// portable executable (PE) file "magic" value.
        /// </summary>
        /// <param name="magic">
        /// The PE optional header "magic" value to translate.
        /// </param>
        /// <returns>
        /// The human-readable name for the specified "magic" value (e.g. "PE32"
        /// or "PE32+"), or null if the value is not recognized.
        /// </returns>
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
        /// <summary>
        /// This method returns the portable executable (PE) file "magic" value
        /// that corresponds to the bitness of the current process.
        /// </summary>
        /// <returns>
        /// The "magic" value matching the current process (32-bit or 64-bit) on
        /// Windows, or <see cref="IMAGE_NT_OPTIONAL_BAD_MAGIC" /> if it cannot
        /// be determined.
        /// </returns>
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

        /// <summary>
        /// This method checks whether the architecture of the specified
        /// portable executable (PE) file matches that of the current process.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the file architecture matches (or could not be checked
        /// because the operating system does not use PE files); otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method checks whether the architecture of the specified
        /// portable executable (PE) file matches that of the current process,
        /// optionally returning the extracted "magic" value.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to check.
        /// </param>
        /// <param name="findFlags">
        /// The flags that control whether the file architecture is actually
        /// matched against that of the current process.
        /// </param>
        /// <param name="magic">
        /// Upon success, receives the "magic" value extracted from the PE file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the file architecture matches (or matching was not requested
        /// or the operating system does not use PE files); otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method extracts the "magic" value from the optional header of
        /// the specified portable executable (PE) file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="magic">
        /// Upon success, receives the "magic" value extracted from the PE file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the "magic" value was extracted successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method extracts the "magic" value and CLR header virtual
        /// address from the specified portable executable (PE) file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="magic">
        /// Upon success, receives the "magic" value extracted from the PE file.
        /// </param>
        /// <param name="clrHeader">
        /// Upon success, receives the CLR header virtual address extracted from
        /// the PE file, or zero if it is not present.
        /// </param>
        /// <returns>
        /// True if the values were extracted successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method extracts the "magic" value and CLR header virtual
        /// address from the specified portable executable (PE) file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="magic">
        /// Upon success, receives the "magic" value extracted from the PE file.
        /// </param>
        /// <param name="clrHeader">
        /// Upon success, receives the CLR header virtual address extracted from
        /// the PE file, or zero if it is not present.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the values were extracted successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method extracts the native stack reserve and commit sizes from
        /// the portable executable (PE) file backing the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose backing PE file should be examined.
        /// </param>
        /// <returns>
        /// A new stack size object populated from the PE file, or null if the
        /// specified assembly is null.
        /// </returns>
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

        /// <summary>
        /// This method extracts the native stack reserve and commit sizes from
        /// the specified portable executable (PE) file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="reserve">
        /// Upon success, receives the size of the stack reserve from the PE
        /// file.
        /// </param>
        /// <param name="commit">
        /// Upon success, receives the size of the stack commit from the PE
        /// file.
        /// </param>
        /// <returns>
        /// True if the values were extracted successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method copies the cached PE file stack reserve and commit sizes
        /// into the specified stack size object for any of its values that have
        /// not already been set.
        /// </summary>
        /// <param name="stackSize">
        /// The stack size object to populate; any reserve or commit value that
        /// is currently zero is filled in from the cached PE file values.
        /// </param>
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
        /// <summary>
        /// This method returns the cached native stack reserve size extracted
        /// from the PE header of the main Eagle assembly.
        /// </summary>
        /// <returns>
        /// The cached stack reserve size, or zero if it is not available.
        /// </returns>
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

        /// <summary>
        /// This method extracts the link timestamp from the specified portable
        /// executable (PE) file and converts it to a date and time value.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="dateTime">
        /// Upon success, receives the date and time corresponding to the PE
        /// file link timestamp.
        /// </param>
        /// <returns>
        /// True if the date and time were extracted successfully; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method extracts the raw link timestamp from the
        /// IMAGE_FILE_HEADER of the specified portable executable (PE) file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read.
        /// </param>
        /// <param name="timeStamp">
        /// Upon success, receives the raw link timestamp from the PE file.
        /// </param>
        /// <returns>
        /// True if the timestamp was extracted successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the product version portion of the
        /// specified file version information is empty (i.e. all parts are
        /// zero).
        /// </summary>
        /// <param name="version">
        /// The file version information to examine; this may be null.
        /// </param>
        /// <param name="nullIsEmpty">
        /// The value to return when the specified version information is null.
        /// </param>
        /// <returns>
        /// True if the product version is empty (or the version is null and
        /// <paramref name="nullIsEmpty" /> is non-zero); otherwise, false.
        /// </returns>
        private static bool IsProductVersionEmpty(
            FileVersionInfo version,
            bool nullIsEmpty
            )
        {
            if (version == null)
                return nullIsEmpty;

            return (version.ProductMajorPart == 0) &&
                (version.ProductMinorPart == 0) &&
                (version.ProductBuildPart == 0) &&
                (version.ProductPrivatePart == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the file version portion of the
        /// specified file version information is empty (i.e. all parts are
        /// zero).
        /// </summary>
        /// <param name="version">
        /// The file version information to examine; this may be null.
        /// </param>
        /// <param name="nullIsEmpty">
        /// The value to return when the specified version information is null.
        /// </param>
        /// <returns>
        /// True if the file version is empty (or the version is null and
        /// <paramref name="nullIsEmpty" /> is non-zero); otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the version of the specified file, preferring
        /// the file version and falling back to the product version.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file whose version is to be retrieved.
        /// </param>
        /// <param name="nonEmpty">
        /// Non-zero to treat an empty version as an error.
        /// </param>
        /// <param name="version">
        /// Upon success, receives the version of the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetFileVersion(
            string fileName,                 /* in */
            bool nonEmpty,                   /* in */
            ref Version version,             /* out */
            ref Result error                 /* out */
            )
        {
            FileVersionInfo fileVersion = null;

            return GetFileVersion(
                fileName, nonEmpty, ref fileVersion,
                ref version, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the version of the specified file, preferring
        /// the file version and falling back to the product version, also
        /// returning the underlying file version information.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file whose version is to be retrieved.
        /// </param>
        /// <param name="nonEmpty">
        /// Non-zero to treat an empty version as an error.
        /// </param>
        /// <param name="fileVersion">
        /// Upon success, receives the underlying file version information.
        /// </param>
        /// <param name="version">
        /// Upon success, receives the version of the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetFileVersion(
            string fileName,                 /* in */
            bool nonEmpty,                   /* in */
            ref FileVersionInfo fileVersion, /* out */
            ref Version version,             /* out */
            ref Result error                 /* out */
            )
        {
            if (File.Exists(fileName))
            {
                try
                {
                    FileVersionInfo localFileVersion =
                        FileVersionInfo.GetVersionInfo(fileName);

                    if (!IsFileVersionEmpty(localFileVersion, true))
                    {
                        fileVersion = localFileVersion;

                        version = new Version(
                            localFileVersion.FileMajorPart,
                            localFileVersion.FileMinorPart,
                            localFileVersion.FileBuildPart,
                            localFileVersion.FilePrivatePart);

                        return ReturnCode.Ok;
                    }
                    else if (!IsProductVersionEmpty(localFileVersion, true))
                    {
                        fileVersion = localFileVersion;

                        version = new Version(
                            localFileVersion.ProductMajorPart,
                            localFileVersion.ProductMinorPart,
                            localFileVersion.ProductBuildPart,
                            localFileVersion.ProductPrivatePart);

                        return ReturnCode.Ok;
                    }
                    else if (nonEmpty)
                    {
                        error = String.Format(
                            "file {0} cannot have empty version",
                            FormatOps.WrapOrNull(fileName));
                    }
                    else
                    {
                        return ReturnCode.Ok;
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

        /// <summary>
        /// This method attempts to create a new, empty file with the specified
        /// name.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to create.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method updates the last write time of the specified file or
        /// directory to the current time, creating the file if it does not
        /// already exist.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to touch.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method verifies whether the specified path can be executed by
        /// the current user.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="accessStatus">
        /// Upon return, receives non-zero if the path can be executed;
        /// otherwise, false.
        /// </param>
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

        /// <summary>
        /// This method verifies whether the specified path can be read by the
        /// current user.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="accessStatus">
        /// Upon return, receives non-zero if the path can be read; otherwise,
        /// false.
        /// </param>
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

        /// <summary>
        /// This method verifies whether the specified path can be written by
        /// the current user.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="accessStatus">
        /// Upon return, receives non-zero if the path can be written;
        /// otherwise, false.
        /// </param>
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

        /// <summary>
        /// This method verifies whether the specified path can be accessed with
        /// the specified file access.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="access">
        /// The kind of access (read, write, or both) to verify.
        /// </param>
        /// <returns>
        /// True if the path can be accessed with the specified access;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method verifies whether the specified path can be accessed with
        /// the specified file access, dispatching to the directory or file
        /// verification helper as appropriate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="access">
        /// The kind of access (read, write, or both) to verify.
        /// </param>
        /// <param name="mustCreate">
        /// Non-zero if the file must be created in order to verify access.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the path can be accessed with the specified access;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method verifies whether the specified directory can be written
        /// to by attempting to create and delete a unique temporary file and
        /// directory within it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="directory">
        /// The directory whose write access is to be verified.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the directory can be written to; otherwise, false.
        /// </returns>
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
                Directory.Delete(path, true); /* throw */

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
                            Directory.Delete(path, true); /* throw */
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

        /// <summary>
        /// This method verifies whether the specified directory can be accessed
        /// with the specified file access.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="directory">
        /// The directory whose access is to be verified.
        /// </param>
        /// <param name="access">
        /// The kind of access (read, write, or both) to verify.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the directory can be accessed with the specified access;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method verifies whether the specified file can be accessed with
        /// the specified file access by attempting to open it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose access is to be verified.
        /// </param>
        /// <param name="access">
        /// The kind of access (read, write, or both) to verify.
        /// </param>
        /// <param name="mustCreate">
        /// Non-zero if the file must be newly created in order to verify access.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the file can be accessed with the specified access;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified set of file system
        /// rights contains a given subset of rights.
        /// </summary>
        /// <param name="rights">
        /// The set of file system rights to examine.
        /// </param>
        /// <param name="hasRights">
        /// The subset of file system rights to look for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified rights are present;
        /// otherwise, only any one of them need be present.
        /// </param>
        /// <returns>
        /// True if the required rights are present according to
        /// <paramref name="all" />; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified Eagle file permissions into the
        /// equivalent set of .NET Framework file system rights.
        /// </summary>
        /// <param name="permissions">
        /// The Eagle file permissions to translate.
        /// </param>
        /// <returns>
        /// The set of file system rights corresponding to the specified
        /// permissions.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current user is the owner of the
        /// specified file or directory.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="owner">
        /// Upon success, receives the identity reference of the owner of the
        /// path.
        /// </param>
        /// <param name="ownerStatus">
        /// Upon success, receives non-zero if the current user is the owner of
        /// the path; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified user (or the current
        /// user, if none is specified) is the owner of the specified file or
        /// directory.
        /// </summary>
        /// <param name="identity">
        /// The Windows identity to check against; if null, the identity of the
        /// current thread is used.
        /// </param>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="owner">
        /// Upon success, receives the identity reference of the owner of the
        /// path.
        /// </param>
        /// <param name="ownerStatus">
        /// Upon success, receives non-zero if the specified user is the owner
        /// of the path; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method performs an access check against the specified file
        /// system security object, computing the rights granted to the
        /// specified user and whether the desired rights are all granted.
        /// </summary>
        /// <param name="identity">
        /// The Windows identity to check against; if null, the identity of the
        /// current thread is used.
        /// </param>
        /// <param name="security">
        /// The file system security object describing the access rules to
        /// evaluate.
        /// </param>
        /// <param name="desiredRights">
        /// The set of file system rights that the caller wants to be granted.
        /// </param>
        /// <param name="grantedRights">
        /// Upon success, receives the set of file system rights actually
        /// granted to the user.
        /// </param>
        /// <param name="accessStatus">
        /// Upon success, receives non-zero if all of the desired rights are
        /// granted; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
                                //       rights the user has been denied, remove them
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

        /// <summary>
        /// This method performs an access check against the specified file or
        /// directory for the current user, computing the rights granted and
        /// whether the desired rights are all granted.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="desiredRights">
        /// The set of file system rights that the caller wants to be granted.
        /// </param>
        /// <param name="grantedRights">
        /// Upon success, receives the set of file system rights actually
        /// granted to the current user.
        /// </param>
        /// <param name="accessStatus">
        /// Upon success, receives non-zero if all of the desired rights are
        /// granted; otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the current user has the rights
        /// necessary to read the attributes of the specified file or directory.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the current user can read the file attributes; otherwise,
        /// false.
        /// </returns>
        public static bool CanReadFileAttributes(
            string path,     /* in */
            ref Result error /* out */
            )
        {
            FileSystemRights grantedRights = NoFileSystemRights;
            bool accessStatus = false;

            if (AccessCheck(
                    path, FileSystemRights.ReadAttributes,
                    ref grantedRights, ref accessStatus,
                    ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "CanReadFileAttributes: access check error = {0}",
                    FormatOps.WrapOrNull(error)), typeof(FileOps).Name,
                    TracePriority.FileSystemWarning);

                return false;
            }

            if (!accessStatus)
            {
                TraceOps.DebugTrace(String.Format(
                    "CanReadFileAttributes: missing some rights = {0}",
                    FormatOps.WrapOrNull(grantedRights)),
                    typeof(FileOps).Name, TracePriority.FileSystemWarning);

                error = "missing rights to read file attributes";
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries whether the specified path exists and, if so,
        /// whether it is read-only and whether it is a directory.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to check.
        /// </param>
        /// <param name="exists">
        /// Upon success, receives non-zero if the path exists; otherwise,
        /// false.
        /// </param>
        /// <param name="readOnly">
        /// Upon success, receives non-zero if the path is read-only; otherwise,
        /// false.
        /// </param>
        /// <param name="directory">
        /// Upon success, receives non-zero if the path is a directory;
        /// otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified set of file system
        /// rights contains any bits that the .NET Framework is unable to
        /// handle.
        /// </summary>
        /// <param name="rights">
        /// The set of file system rights to examine.
        /// </param>
        /// <returns>
        /// True if the rights contain any bits outside the set of known-good
        /// rights; otherwise, false.
        /// </returns>
        private static bool IsBadFileSystemRights(
            FileSystemRights rights /* in */
            )
        {
            //
            // HACK: Apparently, the .NET Framework cannot handle all the
            //       valid file system rights.  For more details, please
            //       refer to the following:
            //
            //       https://stackoverflow.com/questions/9694834
            //
            return (rights & ~AllFileSystemRights) != NoFileSystemRights;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string representation of the specified file
        /// system access rule, as a flat list of property name and value pairs.
        /// </summary>
        /// <param name="rule">
        /// The file system access rule to format; this may be null.
        /// </param>
        /// <returns>
        /// A string representation of the access rule, or null if the specified
        /// rule is null.
        /// </returns>
        private static string ToString(
            FileSystemAccessRule rule /* in */
            )
        {
            if (rule == null)
                return null;

            StringList list = new StringList();

            IdentityReference identity = rule.IdentityReference;

            list.Add("IdentityReference");

            if (identity != null)
                list.Add(identity.ToString());
            else
                list.Add((string)null);

            list.Add("FileSystemRights");
            list.Add(rule.FileSystemRights.ToString());

            list.Add("AccessControlType");
            list.Add(rule.AccessControlType.ToString());

            list.Add("InheritanceFlags");
            list.Add(rule.InheritanceFlags.ToString());

            list.Add("PropagationFlags");
            list.Add(rule.PropagationFlags.ToString());

            list.Add("IsInherited");
            list.Add(rule.IsInherited.ToString());

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of string representations for each file
        /// system access rule in the specified collection.
        /// </summary>
        /// <param name="rules">
        /// The collection of authorization rules to format; this may be null.
        /// </param>
        /// <returns>
        /// A list containing the string representation of each access rule, or
        /// null if the specified collection is null.
        /// </returns>
        public static StringList ToList(
            AuthorizationRuleCollection rules /* in */
            )
        {
            if (rules == null)
                return null;

            StringList list = new StringList();

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule == null)
                    continue;

                list.Add(ToString(rule));
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes access rules from the specified file system
        /// security object, optionally disabling inheritance and skipping rules
        /// that cannot be handled.
        /// </summary>
        /// <param name="security">
        /// The file system security object whose access rules are to be
        /// removed.
        /// </param>
        /// <param name="includeExplicit">
        /// Non-zero to include explicitly defined access rules.
        /// </param>
        /// <param name="includeInherited">
        /// Non-zero to include inherited access rules (and to first protect the
        /// access rules from further inheritance).
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to silently skip null access rules instead of treating them
        /// as an error.
        /// </param>
        /// <param name="skipBadRights">
        /// Non-zero to skip access rules whose rights cannot be handled by the
        /// .NET Framework.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode RemoveAccessRules(
            FileSystemSecurity security, /* in */
            bool includeExplicit,        /* in */
            bool includeInherited,       /* in */
            bool allowNull,              /* in */
            bool skipBadRights,          /* in */
            ref Result error             /* out */
            )
        {
            if (security == null)
            {
                error = "invalid security object";
                return ReturnCode.Error;
            }

            try
            {
                if (includeInherited)
                    security.SetAccessRuleProtection(true, false); /* throw */

                AuthorizationRuleCollection rules = security.GetAccessRules(
                    includeExplicit, includeInherited, typeof(SecurityIdentifier));

                if (rules == null)
                {
                    error = "invalid authorization rules";
                    return ReturnCode.Error;
                }

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule == null)
                    {
                        if (allowNull)
                        {
                            continue;
                        }
                        else
                        {
                            error = "invalid file system access rule";
                            return ReturnCode.Error;
                        }
                    }

                    if (skipBadRights &&
                        IsBadFileSystemRights(rule.FileSystemRights))
                    {
                        continue;
                    }

                    if (!security.RemoveAccessRule(rule))
                    {
                        error = String.Format(
                            "failed to remove access rule {0}",
                            ToString(rule));

                        return ReturnCode.Error;
                    }
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

        /// <summary>
        /// This method verifies that the specified path satisfies the requested
        /// permissions, taking into account whether it exists, whether it is a
        /// file or directory, whether it is read-only, and the access rights of
        /// the current user.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to verify.
        /// </param>
        /// <param name="permissions">
        /// The set of file permissions to verify against the path.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the path satisfies the requested
        /// permissions; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified string contains any
        /// glob wildcard or escape metacharacters.
        /// </summary>
        /// <param name="value">
        /// The string to examine.
        /// </param>
        /// <returns>
        /// True if the string contains at least one glob wildcard character;
        /// otherwise, false.
        /// </returns>
        private static bool HasGlobWildcard(
            string value
            )
        {
            return (value != null) &&
                (GlobWildcardChars != null) &&
                (value.IndexOfAny(GlobWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method escapes any glob wildcard metacharacters within the
        /// specified string by prefixing each one with a backslash.
        /// </summary>
        /// <param name="value">
        /// The string whose glob wildcard characters are to be escaped.
        /// </param>
        /// <returns>
        /// The string with each glob wildcard character escaped, or the
        /// original string if it is null, empty, or contains no such
        /// characters.
        /// </returns>
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

            StringBuilder builder = StringBuilderFactory.Create(
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

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the file name to use for a glob match result,
        /// optionally including the directory portion.
        /// </summary>
        /// <param name="directory">
        /// The directory that contains the file system entry.
        /// </param>
        /// <param name="fileSystemInfo">
        /// The file system information describing the matched entry.
        /// </param>
        /// <param name="withDirectory">
        /// Non-zero to include the directory portion in the returned file name.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to allow a leading drive letter and colon in the directory
        /// to be combined without an extra separator (for Tcl compatibility).
        /// </param>
        /// <returns>
        /// The computed file name, or null if the specified file system
        /// information is null.
        /// </returns>
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

        /// <summary>
        /// This method splits the directory portion out of the specified glob
        /// path prefix, escaping the remaining prefix for glob matching.
        /// </summary>
        /// <param name="pathPrefix">
        /// On input, the path prefix to split; on output, the (escaped)
        /// non-directory remainder of the prefix, or null.
        /// </param>
        /// <param name="directory">
        /// Upon return, receives the directory portion extracted from the path
        /// prefix, or null if there is none.
        /// </param>
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
        /// <summary>
        /// This method retrieves the file system security object for the
        /// specified file system entry.
        /// </summary>
        /// <param name="fileSystemInfo">
        /// The file system information describing the file or directory.
        /// </param>
        /// <returns>
        /// The file system security object for the entry, or null if the entry
        /// is neither a file nor a directory.
        /// </returns>
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

        /// <summary>
        /// This method decomposes the file attributes of the specified file
        /// system entry into a set of individual boolean flags.
        /// </summary>
        /// <param name="fileSystemInfo">
        /// The file system information describing the file or directory; this
        /// may be null, in which case all output flags are false.
        /// </param>
        /// <param name="isReadOnly">
        /// Upon return, receives non-zero if the entry is read-only.
        /// </param>
        /// <param name="isHidden">
        /// Upon return, receives non-zero if the entry is hidden.
        /// </param>
        /// <param name="isSystem">
        /// Upon return, receives non-zero if the entry is a system file.
        /// </param>
        /// <param name="isDirectory">
        /// Upon return, receives non-zero if the entry is a directory.
        /// </param>
        /// <param name="isArchive">
        /// Upon return, receives non-zero if the entry has the archive
        /// attribute.
        /// </param>
        /// <param name="isDevice">
        /// Upon return, receives non-zero if the entry is a device.
        /// </param>
        /// <param name="isNormal">
        /// Upon return, receives non-zero if the entry has the normal
        /// attribute.
        /// </param>
        /// <param name="isTemporary">
        /// Upon return, receives non-zero if the entry is temporary.
        /// </param>
        /// <param name="isSparseFile">
        /// Upon return, receives non-zero if the entry is a sparse file.
        /// </param>
        /// <param name="isReparsePoint">
        /// Upon return, receives non-zero if the entry is a reparse point.
        /// </param>
        /// <param name="isCompressed">
        /// Upon return, receives non-zero if the entry is compressed.
        /// </param>
        /// <param name="isOffline">
        /// Upon return, receives non-zero if the entry is offline.
        /// </param>
        /// <param name="isNotContentIndexed">
        /// Upon return, receives non-zero if the entry is not content indexed.
        /// </param>
        /// <param name="isEncrypted">
        /// Upon return, receives non-zero if the entry is encrypted.
        /// </param>
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

        /// <summary>
        /// This method determines whether the specified file system entry
        /// matches the requested set of [glob] type and permission filters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="types">
        /// The dictionary of requested type and permission filters; this may be
        /// null, in which case all entries match.
        /// </param>
        /// <param name="fileSystemInfo">
        /// The file system information describing the entry to test.
        /// </param>
        /// <returns>
        /// True if the entry matches the requested type filters; otherwise,
        /// false.
        /// </returns>
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
            // MAYBE IGNORE SYNTHETIC ATTRIBUTES (OPTIONAL?)
            ///////////////////////////////////////////////////////////////////

            //
            // HACK: On non-Windows operating systems, the .NET runtime will
            //       return "FileAttributes" values that include several of
            //       the "legacy" MS-DOS file system flags based on metadata
            //       not otherwise available to managed code, e.g. it will
            //       "synthesize" the "Hidden" flag if the name begins with
            //       a dot -OR- the file should (simply?) be hidden from the
            //       user interface (e.g. macOS).  This ends up causing some
            //       issues with native Tcl compatibility; therefore, we try
            //       to detect and work around this situation.  Please refer
            //       to the following .NET runtime source code for complete
            //       implementation details:
            //
            //       src/libraries/System.Private.CoreLib/src/System/IO/FileStatus.Unix.cs (GetAttributes)
            //       src/coreclr/pal/src/file/file.cpp (GetFileAttributesA)
            //       src/native/libs/System.Native/pal_io.c (UF_HIDDEN)
            //
            if (GlobIgnoreSyntheticAttributes &&
                !GlobWindowsSyntheticAttributes &&
                !types.ContainsKey("synthetic"))
            {
                if (isReadOnly)
                    isReadOnly = false; /* NOTE: HasReadOnlyFlag. */

                if (isHidden)
                {
                    //
                    // HACK: For compatibility with native Tcl, treat all
                    //       file names that start with a period (a.k.a.
                    //       "dotfiles") as hidden for our purposes here.
                    //
                    if (types.ContainsKey("dotfiles") ||
                        !SharedStringOps.StartsWith(
                            fileSystemInfo.Name, Characters.PeriodString,
                            StringComparison.Ordinal))
                    {
                        isHidden = false; /* NOTE: HasHiddenFlag. */
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////
            // FILTER OUT "SPECIAL" PATHS (OPTIONAL?)
            ///////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
            if ((isNormal || isDirectory) && GlobNormalPathsOnly)
            {
                if (!PathOps.IsNormal(
                        fileSystemInfo.FullName, null, true))
                {
                    isNormal = false;
                    isDirectory = false;
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////
            // BEGIN EXCLUDED BY DEFAULT
            ///////////////////////////////////////////////////////////////////

            if (isSystem && !types.ContainsKey("system"))
                return false;

            if (!isSystem && types.ContainsKey("systemonly"))
                return false;

            if (isHidden && !types.ContainsKey("hidden"))
                return false;

            if (!isHidden && types.ContainsKey("hiddenonly"))
                return false;

            if (isDevice && !types.ContainsKey("device"))
                return false;

            if (!isDevice && types.ContainsKey("deviceonly"))
                return false;

            ///////////////////////////////////////////////////////////////////
            // BEGIN INCLUDED BY DEFAULT
            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Assume the reparse point contains a link (junction).
            //
            if (isReparsePoint && types.ContainsKey("nolink"))
                return false;

            if (!isReparsePoint && types.ContainsKey("link"))
                return false;

            if (isReadOnly && types.ContainsKey("noreadonly"))
                return false;

            if (!isReadOnly && types.ContainsKey("readonly"))
                return false;

            if (isArchive && types.ContainsKey("noarchive"))
                return false;

            if (!isArchive && types.ContainsKey("archive"))
                return false;

            if (isNormal && types.ContainsKey("nonormal"))
                return false;

            if (!isNormal && types.ContainsKey("normal"))
                return false;

            if (isTemporary && types.ContainsKey("notemporary"))
                return false;

            if (!isTemporary && types.ContainsKey("temporary"))
                return false;

            if (isSparseFile && types.ContainsKey("nosparsefile"))
                return false;

            if (!isSparseFile && types.ContainsKey("sparsefile"))
                return false;

            if (isCompressed && types.ContainsKey("nocompressed"))
                return false;

            if (!isCompressed && types.ContainsKey("compressed"))
                return false;

            if (isOffline && types.ContainsKey("nooffline"))
                return false;

            if (!isOffline && types.ContainsKey("offline"))
                return false;

            if (isNotContentIndexed && types.ContainsKey("contentindexed"))
                return false;

            if (!isNotContentIndexed && types.ContainsKey("notcontentindexed"))
                return false;

            if (isEncrypted && types.ContainsKey("noencrypted"))
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

        /// <summary>
        /// This method enumerates the file system entries within the specified
        /// directory, returning a dictionary that maps their glob file names to
        /// their file system information objects.
        /// </summary>
        /// <param name="directoryInfo">
        /// The directory to enumerate; this may be null, in which case null is
        /// returned.
        /// </param>
        /// <param name="directory">
        /// The directory name used when computing the returned file names.
        /// </param>
        /// <param name="includeDirectories">
        /// Non-zero to include subdirectories in the result.
        /// </param>
        /// <param name="includeFiles">
        /// Non-zero to include files in the result.
        /// </param>
        /// <param name="includeSpecial">
        /// Non-zero to include the special current and parent directory entries
        /// in the result.
        /// </param>
        /// <param name="withDirectory">
        /// Non-zero to include the directory portion in the computed file
        /// names.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to allow a leading drive letter and colon when computing
        /// the file names.
        /// </param>
        /// <returns>
        /// A dictionary mapping file names to their file system information, or
        /// null if the specified directory is null.
        /// </returns>
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

        /// <summary>
        /// This method creates a directory information object for the specified
        /// directory name.
        /// </summary>
        /// <param name="directory">
        /// The directory name; this may be null.
        /// </param>
        /// <returns>
        /// A new directory information object, or null if the specified
        /// directory name is null.
        /// </returns>
        private static DirectoryInfo GetDirectoryInfo(
            string directory /* in: OPTIONAL */
            )
        {
            return (directory != null) ?
                new DirectoryInfo(directory) : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the recursive work of matching a single glob
        /// pattern against the file system, accumulating any matching file
        /// names into the specified list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="pattern">
        /// The glob pattern to match.
        /// </param>
        /// <param name="directoryInfo">
        /// The directory in which to search.
        /// </param>
        /// <param name="types">
        /// The dictionary of requested type and permission filters; this may be
        /// null.
        /// </param>
        /// <param name="pathPrefix">
        /// The literal path prefix that matching file names must begin with;
        /// this may be null.
        /// </param>
        /// <param name="directory">
        /// The directory name used when computing the returned file names; this
        /// may be null.
        /// </param>
        /// <param name="fileNames">
        /// The list to which matching file names are added.
        /// </param>
        /// <param name="level">
        /// The current recursion level, used to decide when to include the
        /// directory portion.
        /// </param>
        /// <param name="tailOnly">
        /// Non-zero to add only the final file name component of each match.
        /// </param>
        /// <param name="withDirectory">
        /// Non-zero to include the directory portion in the matched file names.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to allow a leading drive letter and colon in the pattern.
        /// </param>
        /// <param name="allowCurrent">
        /// Non-zero to allow the pattern to reference the current directory.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

                        foreach (FilePair pair in childFileSystemInfos)
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
                foreach (FilePair pair in fileSystemInfos)
                {
                    FileSystemInfo fileSystemInfo = pair.Value as FileSystemInfo;

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

        /// <summary>
        /// This method implements the core of the [glob] command, matching one
        /// or more glob patterns against the file system and returning the list
        /// of matching file names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="patterns">
        /// The list of glob patterns to match.
        /// </param>
        /// <param name="types">
        /// The dictionary of requested type and permission filters; this may be
        /// null.
        /// </param>
        /// <param name="pathPrefix">
        /// The literal path prefix that matching file names must begin with;
        /// this may be null.
        /// </param>
        /// <param name="directory">
        /// The directory in which to perform the search; this may be null, in
        /// which case the current directory is used.
        /// </param>
        /// <param name="join">
        /// Non-zero to join all of the patterns into a single combined path
        /// before matching.
        /// </param>
        /// <param name="tailOnly">
        /// Non-zero to return only the final file name component of each match.
        /// </param>
        /// <param name="allowDrive">
        /// Non-zero to allow a leading drive letter and colon in the patterns.
        /// </param>
        /// <param name="allowCurrent">
        /// Non-zero to allow the patterns to reference the current directory.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat the absence of any matching file as an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The list of matching file names, or null on failure.
        /// </returns>
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
            {
                directory = PathOps.GetCurrentDirectory();

                if (String.IsNullOrEmpty(directory))
                {
                    error = "invalid current directory";
                    return null;
                }
            }

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
                    patterns.ToRawString(Characters.SpaceString)));

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates a recursive flag into the corresponding
        /// directory search option.
        /// </summary>
        /// <param name="recursive">
        /// Non-zero to search all subdirectories; otherwise, only the top
        /// directory.
        /// </param>
        /// <returns>
        /// The search option corresponding to the specified recursive flag.
        /// </returns>
        public static SearchOption GetSearchOption(
            bool recursive
            )
        {
            return recursive ?
                SearchOption.AllDirectories :
                SearchOption.TopDirectoryOnly;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the size, in bytes, of the specified file,
        /// silently returning zero on any error.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose size is to be retrieved.
        /// </param>
        /// <returns>
        /// The size of the file in bytes, or zero if it could not be
        /// determined.
        /// </returns>
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
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose size is to be retrieved.
        /// </param>
        /// <param name="size">
        /// Upon success, receives the size of the file in bytes.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetFileSize(
            string path,
            ref long size
            )
        {
            Result error = null;

            return GetFileSize(path, ref size, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the size, in bytes, of the specified file.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose size is to be retrieved.
        /// </param>
        /// <param name="size">
        /// Upon success, receives the size of the file in bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetFileSize(
            string path,
            ref long size,
            ref Result error
            )
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path); /* throw */

                size = fileInfo.Length; /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads up to the specified number of bytes from the
        /// beginning of the specified file.
        /// </summary>
        /// <param name="path">
        /// The path of the file to read.
        /// </param>
        /// <param name="count">
        /// The number of bytes to read; if negative, the entire file is read;
        /// if zero, an empty array is returned.
        /// </param>
        /// <returns>
        /// The bytes read from the file, an empty array if the count is zero,
        /// or null if the file is empty, too small, or could not be read.
        /// </returns>
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

        /// <summary>
        /// This method returns a human-readable description of the type of the
        /// specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the file or directory to classify.
        /// </param>
        /// <returns>
        /// The string "directory", "file", or "unknown", depending on what the
        /// path refers to.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified map-open access flags into the
        /// equivalent file access value.
        /// </summary>
        /// <param name="access">
        /// The map-open access flags to translate.
        /// </param>
        /// <returns>
        /// The file access value (read, write, or read/write) corresponding to
        /// the specified flags.
        /// </returns>
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

        /// <summary>
        /// This method translates the specified map-open access flags into the
        /// equivalent file mode value, following POSIX-like semantics.
        /// </summary>
        /// <param name="access">
        /// The map-open access flags to translate.
        /// </param>
        /// <returns>
        /// The file mode value corresponding to the specified flags.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified set of file attributes
        /// contains all of a given subset of attributes.
        /// </summary>
        /// <param name="attributes">
        /// The set of file attributes to examine.
        /// </param>
        /// <param name="haveAttributes">
        /// The subset of file attributes that must all be present.
        /// </param>
        /// <returns>
        /// True if all of the specified attributes are present; otherwise,
        /// false.
        /// </returns>
        public static bool HaveFileAttributes(
            FileAttributes attributes,
            FileAttributes haveAttributes
            )
        {
            return ((attributes & haveAttributes) == haveAttributes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the file attributes of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose attributes are to be set.
        /// </param>
        /// <param name="fileAttributes">
        /// The file attributes to apply to the path.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the file attributes of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose attributes are to be retrieved.
        /// </param>
        /// <param name="fileAttributes">
        /// Upon success, receives the file attributes of the path.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetFileAttributes(
            string path,
            ref FileAttributes fileAttributes
            )
        {
            Result error = null;

            return GetFileAttributes(path, ref fileAttributes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the file attributes of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose attributes are to be retrieved.
        /// </param>
        /// <param name="fileAttributes">
        /// Upon success, receives the file attributes of the path.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method deletes the files matching the specified patterns within
        /// the specified directory, then removes the (now empty)
        /// subdirectories and the directory itself.
        /// </summary>
        /// <param name="directory">
        /// The directory to clean up and remove.
        /// </param>
        /// <param name="patterns">
        /// The file name patterns identifying which files to delete; this may
        /// be null.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to process all subdirectories of the specified directory.
        /// </param>
        /// <returns>
        /// True if the directory was successfully cleaned up and removed;
        /// otherwise, false.
        /// </returns>
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

                        Array.Sort(fileNames); /* O(N) */

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

                Array.Sort(subDirectories); /* O(N) */
                Array.Reverse(subDirectories); /* O(N) */

                foreach (string subDirectory in subDirectories)
                {
                    if (!Directory.Exists(subDirectory))
                        continue;

                    try
                    {
                        Directory.Delete(
                            subDirectory, false); /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(FileOps).Name,
                            TracePriority.FileSystemError);
                    }
                }

                Directory.Delete(directory, false); /* throw */
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

        /// <summary>
        /// This method sets the current working directory to the specified
        /// directory, silently ignoring any error.
        /// </summary>
        /// <param name="directory">
        /// The directory to make current; if null, no action is taken.
        /// </param>
        private static void MaybeSetCurrentDirectory(
            string directory /* in */
            )
        {
            MaybeSetCurrentDirectory(ref directory);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the current working directory to the specified
        /// directory, silently ignoring any error and resetting the reference
        /// to null afterward.
        /// </summary>
        /// <param name="directory">
        /// On input, the directory to make current; if null, no action is
        /// taken; on output, this is set to null.
        /// </param>
        private static void MaybeSetCurrentDirectory(
            ref string directory /* in, out */
            )
        {
            if (directory == null)
                return;

            try
            {
                Directory.SetCurrentDirectory(directory); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Interpreter).Name,
                    TracePriority.CleanupError);
            }

            directory = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method deletes the set of registered cleanup paths, in reverse
        /// order, after validating each path against its associated cleanup
        /// metadata.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="paths">
        /// The dictionary of paths to clean up, mapped to their cleanup
        /// metadata; this may be null.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress diagnostic trace output.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress complaints about failed deletions.
        /// </param>
        /// <param name="list">
        /// If not null, receives the path and metadata of each successfully
        /// deleted entry.
        /// </param>
        /// <param name="errors">
        /// Receives the accumulated errors encountered while cleaning up the
        /// paths.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if all paths were cleaned up without
        /// error; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CleanupPaths(
            Interpreter interpreter,
            PathDictionary<CleanupPathClientData> paths,
            bool quiet,
            bool noComplain,
            ref StringList list,
            ref ResultList errors
            )
        {
            if (paths == null)
                return ReturnCode.Ok;

            CleanupPathPairs pairs = paths.GetPairsInOrder(true);

            if (pairs == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("cannot get paths in reverse order");
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Make sure that we are *NOT* currently within
            //         any directory that we may want to cleanup.
            //
            // BUGFIX: However, make 100% sure the current directory
            //         is saved AND restored; otherwise, any external
            //         code relying on the current directory may be
            //         messed up.
            //
            string savedDirectory = PathOps.GetCurrentDirectory();

            if (savedDirectory == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid current directory");
                return ReturnCode.Error;
            }

            try
            {
                MaybeSetCurrentDirectory(
                    GlobalState.GetAnyEntryAssemblyPath());

                int errorCount = 0;

                foreach (CleanupPathPair pair in pairs)
                {
                    string path = pair.Key;

                    if (String.IsNullOrEmpty(path))
                        continue;

                    CleanupPathClientData clientData = pair.Value;
                    Result matchError = null;

                    if ((clientData == null) ||
                        !clientData.MatchPathType(path, ref matchError))
                    {
                        errorCount++;

                        if (matchError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(matchError);
                        }

                        if (!quiet)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "CleanupPaths: cannot delete {0}, " +
                                "mismatched {1}: {2}",
                                FormatOps.WrapOrNull(path),
                                FormatOps.WrapOrNull(clientData),
                                FormatOps.WrapOrNull(matchError)),
                                typeof(FileOps).Name,
                                TracePriority.FileSystemError);
                        }

                        continue;
                    }

                    ReturnCode deleteCode;
                    Result deleteError = null;
                    string pathType = null;

                    deleteCode = FileOps.FileDelete(
                        new string[] { path }, clientData.Recursive,
                        clientData.Force, clientData.NoComplain,
                        ref pathType, ref deleteError);

                    if (deleteCode == ReturnCode.Ok)
                    {
                        /* IGNORED */
                        paths.Remove(path);

                        if (list != null)
                        {
                            list.Add(path);
                            list.Add(clientData.ToString());
                        }
                    }
                    else
                    {
                        errorCount++;

                        if (deleteError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(deleteError);
                        }

                        if (!noComplain)
                        {
                            DebugOps.Complain(
                                interpreter, deleteCode,
                                deleteError);
                        }

                        continue;
                    }

                    if (!quiet)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "CleanupPaths: DELETED {0}{1}: {2}",
                            (pathType != null) ? String.Format(
                            "{0} ", pathType) : String.Empty,
                            FormatOps.WrapOrNull(path),
                            clientData.ToString()),
                            typeof(FileOps).Name,
                            TracePriority.FileSystemDebug);
                    }
                }

                return (errorCount == 0) ?
                    ReturnCode.Ok : ReturnCode.Error;
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
                return ReturnCode.Error;
            }
            finally
            {
                MaybeSetCurrentDirectory(ref savedDirectory);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method deletes the specified files and/or directories.
        /// </summary>
        /// <param name="paths">
        /// The list of file or directory paths to delete.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to recursively delete the contents of directories.
        /// </param>
        /// <param name="force">
        /// Non-zero to clear the read-only attribute on files prior to deleting
        /// them.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress errors for missing or invalid paths.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method deletes the specified files and/or directories, also
        /// returning a description of the type of the last path that was
        /// deleted.
        /// </summary>
        /// <param name="paths">
        /// The list of file or directory paths to delete.
        /// </param>
        /// <param name="recursive">
        /// Non-zero to recursively delete the contents of directories.
        /// </param>
        /// <param name="force">
        /// Non-zero to clear the read-only attribute on files prior to deleting
        /// them.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress errors for missing or invalid paths.
        /// </param>
        /// <param name="pathType">
        /// Upon return, receives a description of the type of the last path
        /// that was deleted (e.g. file or directory).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
                SearchOption searchOption = GetSearchOption(true);

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
                                        searchOption);

                                    if (fileNames != null)
                                    {
                                        Array.Sort(fileNames); /* O(N) */

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

        /// <summary>
        /// This method copies (or moves) one or more files to the specified
        /// target file or directory.
        /// </summary>
        /// <param name="fileNames">
        /// The list of source file names to copy or move.
        /// </param>
        /// <param name="path">
        /// The target file name (when there is a single source) or target
        /// directory (when there are multiple sources).
        /// </param>
        /// <param name="move">
        /// Non-zero to move the files (delete each source after copying);
        /// otherwise, copy them.
        /// </param>
        /// <param name="force">
        /// Non-zero to overwrite any existing target file.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method retrieves a file time value for the specified path using
        /// the specified callback.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose time is to be retrieved.
        /// </param>
        /// <param name="callback">
        /// The callback used to obtain the desired file time (e.g. creation,
        /// last access, or last write time).
        /// </param>
        /// <param name="dateTime">
        /// Upon success, receives the requested file time.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method sets a file time value for the specified path using the
        /// specified callback.
        /// </summary>
        /// <param name="path">
        /// The path of the file whose time is to be set.
        /// </param>
        /// <param name="callback">
        /// The callback used to set the desired file time (e.g. creation, last
        /// access, or last write time).
        /// </param>
        /// <param name="dateTime">
        /// The file time value to set.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the names of the file system entries within
        /// the specified directory, optionally filtered by a search pattern.
        /// </summary>
        /// <param name="path">
        /// The path of the directory to enumerate.
        /// </param>
        /// <param name="searchPattern">
        /// The search pattern used to filter the entries; this may be null to
        /// return all entries.
        /// </param>
        /// <param name="entries">
        /// Upon success, receives the list of file system entry names (in Unix
        /// path form).
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method reflects the private byte buffer field of the
        /// <see cref="StreamReader" /> class, trying each of the candidate
        /// field names in order.
        /// </summary>
        /// <returns>
        /// The reflected field information for the byte buffer field, or null if
        /// none of the candidate names could be resolved.
        /// </returns>
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

        /// <summary>
        /// This method attempts to extract the bytes currently buffered within
        /// the specified stream reader, by reflecting its private byte buffer
        /// field.
        /// </summary>
        /// <param name="streamReader">
        /// The stream reader whose buffered bytes are to be extracted.
        /// </param>
        /// <param name="bytes">
        /// Receives the buffered bytes; if null, a new list is created and the
        /// bytes are appended to it.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the buffered bytes were successfully extracted; otherwise,
        /// false.
        /// </returns>
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
