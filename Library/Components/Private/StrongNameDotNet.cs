/*
 * StrongNameDotNet.cs --
 *
 * The following code was shamelessly stolen (and adapted) from the following
 * example code written by Jan Kotas:
 *
 *     https://gist.github.com/jkotas/efc9d4f0e734da9a7d81e96599724955
 *
 * Which was linked from the following .NET Core issue on GitHub:
 *
 *     https://github.com/dotnet/runtime/issues/56976
 *
 * It is unclear what the exact license terms are for this code; however, the
 * Eagle Development Team believes it is licensed under the same terms as the
 * .NET Core runtime itself.  If necessary, it could be omitted when building
 * the Eagle core library; in that case, any features that rely on the strong
 * name signature verification functionality will be unavailable when running
 * on the .NET Core runtime (e.g. loading plugins matching a specified public
 * key token).
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;

///////////////////////////////////////////////////////////////////////////////

#region ImmutableArray<byte> Extension Methods Class
/// <summary>
/// This class provides extension methods for working with instances of
/// <see cref="ImmutableArray{T}" /> of bytes, notably converting one back
/// to its underlying mutable byte array without copying.
/// </summary>
[ObjectId("52ec8331-d48a-4478-8e71-45ff669fef88")]
internal static class ImmutableByteArrayMethods
{
    #region Private Byte Array Union Structure
    /// <summary>
    /// This structure provides an overlapped view of a mutable byte array
    /// and an immutable byte array so that one representation can be
    /// reinterpreted as the other without copying the underlying data.
    /// </summary>
    [ObjectId("9457be5b-48c3-4f88-9f9d-d5745114f40f")]
    [StructLayout(LayoutKind.Explicit)]
    private struct ByteArrayUnion
    {
        /// <summary>
        /// The mutable byte array view of the overlapped storage.
        /// </summary>
        [FieldOffset(0)]
        internal byte[] UnderlyingArray;

        /// <summary>
        /// The immutable byte array view of the overlapped storage.
        /// </summary>
        [FieldOffset(0)]
        internal ImmutableArray<byte> ImmutableArray;
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Public Extension Methods
    /// <summary>
    /// This method returns the mutable byte array underlying the specified
    /// immutable byte array, without copying its contents.
    /// </summary>
    /// <param name="array">
    /// The immutable byte array whose underlying mutable array is needed.
    /// </param>
    /// <returns>
    /// The mutable byte array that backs the specified immutable byte array.
    /// </returns>
    public static byte[] AsArray(
        this ImmutableArray<byte> array /* in */
        )
    {
        ByteArrayUnion union = new ByteArrayUnion();
        union.ImmutableArray = array;
        return union.UnderlyingArray;
    }
    #endregion
}
#endregion

///////////////////////////////////////////////////////////////////////////////

#region .NET Core 2.x / 3.x Strong Name Signature Verification Class
/// <summary>
/// This class implements strong name signature verification for managed
/// assemblies when running on the .NET Core 2.x / 3.x runtime, where the
/// native strong name verification APIs are unavailable.
/// </summary>
[ObjectId("4c129658-62a6-4508-a154-27d2cec6c416")]
internal static class StrongNameDotNet
{
    #region Blob Reader Helper Class
    /// <summary>
    /// This class provides sequential reading of primitive values and
    /// big-integer byte blocks from a byte array, as used when parsing a
    /// strong name public key blob.
    /// </summary>
    [ObjectId("23bff3cd-8bf9-4ef4-be1a-7a766ea1946d")]
    private sealed class BlobReader
    {
        #region Private Data
        /// <summary>
        /// The byte array being read from.
        /// </summary>
        private byte[] bytes;

        /// <summary>
        /// The current zero-based read position within the byte array.
        /// </summary>
        private int offset;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a blob reader positioned at the start of the specified
        /// byte array.
        /// </summary>
        /// <param name="bytes">
        /// The byte array to read from.
        /// </param>
        public BlobReader(
            byte[] bytes /* in */
            )
        {
            this.bytes = bytes;
            this.offset = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method reads a 32-bit little-endian signed integer from the
        /// current position and advances the read position past it.
        /// </summary>
        /// <returns>
        /// The 32-bit signed integer read from the current position.
        /// </returns>
        public int ReadInt32()
        {
            int oldOffset = offset;

            offset = oldOffset + sizeof(int);

            return (bytes[oldOffset + 0] << 0x00) |
                   (bytes[oldOffset + 1] << 0x08) |
                   (bytes[oldOffset + 2] << 0x10) |
                   (bytes[oldOffset + 3] << 0x18);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a big-integer value of the specified length, in
        /// bytes, from the current position, advancing the read position past
        /// it, and returns it in big-endian byte order.
        /// </summary>
        /// <param name="length">
        /// The number of bytes to read for the big-integer value.
        /// </param>
        /// <returns>
        /// The big-integer value, in big-endian byte order, read from the
        /// current position.
        /// </returns>
        public byte[] ReadBigInteger(
            int length /* in */
            )
        {
            byte[] result = new byte[length];

            Array.Copy(bytes, offset, result, 0, length);
            offset += length;

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Private Methods
    /// <summary>
    /// This method computes the strong name hash of the assembly image by
    /// feeding the relevant portions of the file into the specified hash
    /// algorithm, excluding the checksum, the security directory entry, and
    /// the strong name signature itself.
    /// </summary>
    /// <param name="peReader">
    /// The portable executable reader providing access to the assembly image.
    /// </param>
    /// <param name="peHeaders">
    /// The portable executable headers of the assembly image.
    /// </param>
    /// <param name="hashAlgorithm">
    /// The hash algorithm into which the assembly image bytes are fed.
    /// </param>
    /// <param name="signatureStart">
    /// The offset, in bytes, of the start of the strong name signature
    /// within the assembly image.
    /// </param>
    /// <param name="signatureSize">
    /// The size, in bytes, of the strong name signature.
    /// </param>
    private static void HashAssembly(
        PEReader peReader,           /* in */
        PEHeaders peHeaders,         /* in */
        HashAlgorithm hashAlgorithm, /* in */
        int signatureStart,          /* in */
        int signatureSize            /* in */
        ) /* throw */
    {
        PEMagic peMagic = peHeaders.PEHeader.Magic;
        int peHeaderOffset = peHeaders.PEHeaderStartOffset;
        int securityDirectoryEntryOffset;
        int peHeaderSize;

        if (peMagic == PEMagic.PE32)
        {
            //
            // offsetof(IMAGE_OPTIONAL_HEADER32,
            //          DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY])
            //
            securityDirectoryEntryOffset = peHeaderOffset + 0x80;

            //
            // sizeof(IMAGE_OPTIONAL_HEADER32)
            //
            peHeaderSize = 0xE0;
        }
        else if (peMagic == PEMagic.PE32Plus)
        {
            //
            // offsetof(IMAGE_OPTIONAL_HEADER64,
            //          DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY])
            //
            securityDirectoryEntryOffset = peHeaderOffset + 0x90;

            //
            // sizeof(IMAGE_OPTIONAL_HEADER64)
            //
            peHeaderSize = 0xF0;
        }
        else
        {
            throw new BadImageFormatException(String.Format(
                "unsupported PE magic {0}", peMagic));
        }

        //
        // sizeof(IMAGE_SECTION_HEADER)
        //
        int sectionHeaderSize = peHeaders.CoffHeader.NumberOfSections * 0x28;

        int allHeadersSize = peHeaderOffset + peHeaderSize + sectionHeaderSize;
        byte[] allHeaders = new byte[allHeadersSize];
        ImmutableArray<byte> content = peReader.GetEntireImage().GetContent();

        content.CopyTo(0, allHeaders, 0, allHeadersSize);

        //
        // offsetof(IMAGE_OPTIONAL_HEADER, CheckSum)
        //
        int checkSumOffset = peHeaderOffset + 0x40;

        Array.Clear(allHeaders, checkSumOffset, sizeof(uint));
        Array.Clear(allHeaders, securityDirectoryEntryOffset, sizeof(ulong));

        hashAlgorithm.TransformBlock(allHeaders, 0, allHeadersSize, null, 0);

        int signatureEnd = signatureStart + signatureSize;
        byte[] buffer = content.AsArray();

        foreach (SectionHeader sectionHeader in peHeaders.SectionHeaders)
        {
            int sectionStart = sectionHeader.PointerToRawData;
            int sectionEnd = sectionStart + sectionHeader.SizeOfRawData;

            if ((sectionStart <= signatureStart) &&
                (signatureStart < sectionEnd))
            {
                if (!((sectionStart < signatureEnd) &&
                    (signatureEnd <= sectionEnd)))
                {
                    throw new BadImageFormatException(
                        "signature not entirely within section");
                }

                hashAlgorithm.TransformBlock(buffer, sectionStart,
                    signatureStart - sectionStart, null, 0);

                sectionStart = signatureEnd;
            }

            hashAlgorithm.TransformBlock(
                buffer, sectionStart, sectionEnd - sectionStart,
                null, 0);
        }

        hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This method parses a strong name public key blob and extracts the RSA
    /// parameters (exponent and modulus) it contains.
    /// </summary>
    /// <param name="bytes">
    /// The strong name public key blob to parse.
    /// </param>
    /// <returns>
    /// The RSA parameters extracted from the specified public key blob.
    /// </returns>
    private static RSAParameters RSAParametersFromPublicKey(
        byte[] bytes /* in */
        ) /* throw */
    {
        BlobReader reader = new BlobReader(bytes);

        if (reader.ReadInt32() != 0x2400)
            throw new CryptographicException("expected CALG_RSA_SIGN");

        if (reader.ReadInt32() != 0x8004)
            throw new CryptographicException("expected CALG_SHA1");

        /* IGNORED */
        reader.ReadInt32();

        if (reader.ReadInt32() != 0x0206)
            throw new CryptographicException("expected BLOBHEADER");

        if (reader.ReadInt32() != 0x2400)
            throw new CryptographicException("expected CALG_RSA_SIGN");

        if (reader.ReadInt32() != 0x31415352) // "RSA1"
            throw new CryptographicException("expected RSA public key");

        int bitLength = reader.ReadInt32();

        if (bitLength % 16 != 0)
            throw new CryptographicException("invalid bit length");

        int byteLength = bitLength / 8;
        RSAParameters parameters = new RSAParameters();

        parameters.Exponent = reader.ReadBigInteger(4);
        parameters.Modulus = reader.ReadBigInteger(byteLength);

        return parameters;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This method verifies the strong name signature embedded within the
    /// assembly image contained in the specified stream, by recomputing the
    /// strong name hash of the image and checking it against the signature
    /// using the RSA public key stored in the assembly metadata.
    /// </summary>
    /// <param name="stream">
    /// The stream containing the assembly image whose strong name signature
    /// should be verified.
    /// </param>
    /// <returns>
    /// Non-zero if the strong name signature is valid; otherwise, zero.
    /// </returns>
    private static bool VerifyStrongNameSignature(
        Stream stream /* in */
        ) /* throw */
    {
        using (PEReader peReader = new PEReader(
                stream, PEStreamOptions.PrefetchEntireImage |
                PEStreamOptions.LeaveOpen))
        {
            PEHeaders peHeaders = peReader.PEHeaders;
            CorHeader corHeader = peHeaders.CorHeader;

            DirectoryEntry signatureDirectory =
                corHeader.StrongNameSignatureDirectory;

            int signatureSize = signatureDirectory.Size;

            if (signatureSize == 0)
            {
                throw new BadImageFormatException(
                    "missing strong name signature");
            }

            int signatureStart;

            if (!peHeaders.TryGetDirectoryOffset(
                    signatureDirectory, out signatureStart))
            {
                throw new BadImageFormatException(
                    "start of signature directory");
            }

            byte[] hashValue;

            using (HashAlgorithm hashAlgorithm = SHA1.Create())
            {
                HashAssembly(
                    peReader, peHeaders, hashAlgorithm, signatureStart,
                    signatureSize);

                hashValue = hashAlgorithm.Hash;
            }

            byte[] signature = new byte[signatureSize];

            PEMemoryBlock signatureBlock = peReader.GetSectionData(
                signatureDirectory.RelativeVirtualAddress);

            signatureBlock.GetContent().CopyTo(
                0, signature, 0, signature.Length);

            Array.Reverse(signature); // TODO: Always?

            MetadataReader metadataReader = peReader.GetMetadataReader();

            byte[] publicKey = metadataReader.GetBlobBytes(
                metadataReader.GetAssemblyDefinition().PublicKey);

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(RSAParametersFromPublicKey(publicKey));

                RSAPKCS1SignatureDeformatter deformatter =
                    new RSAPKCS1SignatureDeformatter(rsa);

                deformatter.SetHashAlgorithm("SHA1");

                return deformatter.VerifySignature(hashValue, signature);
            }
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Public Methods
    /// <summary>
    /// This method verifies the strong name signature of the specified
    /// assembly file using the managed .NET Core reflection metadata APIs.
    /// </summary>
    /// <param name="fileName">
    /// The fully qualified path to the assembly file whose strong name
    /// signature should be verified.
    /// </param>
    /// <param name="force">
    /// Non-zero to force the signature verification even when it would
    /// otherwise be skipped (e.g. due to a registry verification skip
    /// entry).
    /// </param>
    /// <param name="returnValue">
    /// Upon return, this is set to non-zero if the strong name signature
    /// was determined to be valid; otherwise, it is set to zero.
    /// </param>
    /// <param name="verified">
    /// Upon return, this is set to non-zero if the signature verification
    /// was actually performed; otherwise, it is set to zero.
    /// </param>
    /// <param name="error">
    /// Upon failure, this is set to an error message or exception describing
    /// why the verification could not be performed.
    /// </param>
    /// <returns>
    /// <see cref="ReturnCode.Ok" /> on success;
    /// <see cref="ReturnCode.Error" /> on failure.
    /// </returns>
    public static ReturnCode IsStrongNameVerifiedDotNet(
        string fileName,      /* in */
        bool force,           /* in */
        ref bool returnValue, /* out */
        ref bool verified,    /* out */
        ref Result error      /* out */
        )
    {
        if (String.IsNullOrEmpty(fileName))
        {
            error = "invalid file name";
            return ReturnCode.Error;
        }

        if (!CommonOps.Runtime.IsDotNetCore())
        {
            error = "not supported on this platform";
            return ReturnCode.Error;
        }

        try
        {
            using (FileStream stream = new FileStream(
                    fileName, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite)) /* throw */
            {
                returnValue = VerifyStrongNameSignature(
                    stream); /* throw */

                if (returnValue)
                    verified = true; /* NOTE: No "skipping". */

                return ReturnCode.Ok;
            }
        }
        catch (Exception e)
        {
            error = e;
        }

        TraceOps.DebugTrace(String.Format(
            "IsStrongNameVerifiedDotNet: file {0} verification failure, " +
            "force = {1}, returnValue = {2}, verified = {3}, error = {4}",
            FormatOps.WrapOrNull(fileName), force, returnValue, verified,
            FormatOps.WrapOrNull(error)), typeof(StrongNameDotNet).Name,
            TracePriority.SecurityError);

        return ReturnCode.Error;
    }
    #endregion
}
#endregion
