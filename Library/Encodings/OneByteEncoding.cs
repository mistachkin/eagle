/*
 * OneByteEncoding.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Encodings
{
    /// <summary>
    /// This class represents a single-byte encoding that maps each character
    /// to one byte and each byte to one character.  Encoding is lossy (the
    /// high byte of each character is discarded) while decoding is non-lossy.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f36a83c4-1043-4db1-9e96-6ab30a188748")]
    public class OneByteEncoding : CoreEncoding
    {
        #region Public Constants
        /// <summary>
        /// A shared, pre-built instance of this encoding.
        /// </summary>
        public static readonly Encoding OneByte = new OneByteEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The registered (IANA) name reported for this encoding.
        /// </summary>
        internal static readonly string webName = "OneByte";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.Encoding Overrides
        /// <summary>
        /// Gets the registered (IANA) name for this encoding.
        /// </summary>
        public override string WebName
        {
            get { return webName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates the number of bytes produced by encoding a range of
        /// characters from the specified character array.
        /// </summary>
        /// <param name="chars">
        /// The character array containing the characters to encode.
        /// </param>
        /// <param name="index">
        /// The index of the first character to encode.
        /// </param>
        /// <param name="count">
        /// The number of characters to encode.
        /// </param>
        /// <returns>
        /// The number of bytes produced by encoding the specified characters,
        /// which (for this one-to-one mapping) is equal to
        /// <paramref name="count" />.
        /// </returns>
        public override int GetByteCount(
            char[] chars,
            int index,
            int count
            )
        {
            return count; /* one-to-one mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Encodes a range of characters from the specified character array
        /// into the specified byte array.
        /// </summary>
        /// <param name="chars">
        /// The character array containing the characters to encode.
        /// </param>
        /// <param name="charIndex">
        /// The index of the first character to encode.
        /// </param>
        /// <param name="charCount">
        /// The number of characters to encode.
        /// </param>
        /// <param name="bytes">
        /// The byte array that receives the resulting encoded bytes.
        /// </param>
        /// <param name="byteIndex">
        /// The index at which to begin writing the resulting bytes.
        /// </param>
        /// <returns>
        /// The number of bytes written into <paramref name="bytes" />.
        /// </returns>
        public override int GetBytes(
            char[] chars,
            int charIndex,
            int charCount,
            byte[] bytes,
            int byteIndex
            )
        {
            int oldByteIndex = byteIndex;

            while (charCount-- > 0)
                //
                // NOTE: *WARNING* Lossy, one-to-one mapping.
                //
                bytes[byteIndex++] = ConversionOps.ToByte(chars[charIndex++]);

            return (byteIndex - oldByteIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates the number of characters produced by decoding a range of
        /// bytes from the specified byte array.
        /// </summary>
        /// <param name="bytes">
        /// The byte array containing the bytes to decode.
        /// </param>
        /// <param name="index">
        /// The index of the first byte to decode.
        /// </param>
        /// <param name="count">
        /// The number of bytes to decode.
        /// </param>
        /// <returns>
        /// The number of characters produced by decoding the specified bytes,
        /// which (for this one-to-one mapping) is equal to
        /// <paramref name="count" />.
        /// </returns>
        public override int GetCharCount(
            byte[] bytes,
            int index,
            int count
            )
        {
            return count; /* one-to-one mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decodes a range of bytes from the specified byte array into the
        /// specified character array.
        /// </summary>
        /// <param name="bytes">
        /// The byte array containing the bytes to decode.
        /// </param>
        /// <param name="byteIndex">
        /// The index of the first byte to decode.
        /// </param>
        /// <param name="byteCount">
        /// The number of bytes to decode.
        /// </param>
        /// <param name="chars">
        /// The character array that receives the resulting decoded characters.
        /// </param>
        /// <param name="charIndex">
        /// The index at which to begin writing the resulting characters.
        /// </param>
        /// <returns>
        /// The number of characters written into <paramref name="chars" />.
        /// </returns>
        public override int GetChars(
            byte[] bytes,
            int byteIndex,
            int byteCount,
            char[] chars,
            int charIndex
            )
        {
            int oldCharIndex = charIndex;

            while (byteCount-- > 0)
                //
                // NOTE: Non-lossy, one-to-one mapping.
                //
                chars[charIndex++] = (char)bytes[byteIndex++];

            return (charIndex - oldCharIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates the maximum number of bytes produced by encoding the
        /// specified number of characters.
        /// </summary>
        /// <param name="charCount">
        /// The number of characters to encode.
        /// </param>
        /// <returns>
        /// The maximum number of bytes produced by encoding the specified
        /// number of characters, which (for this one-to-one mapping) is equal
        /// to <paramref name="charCount" />.
        /// </returns>
        public override int GetMaxByteCount(
            int charCount
            )
        {
            return charCount; /* one-to-one mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calculates the maximum number of characters produced by decoding the
        /// specified number of bytes.
        /// </summary>
        /// <param name="byteCount">
        /// The number of bytes to decode.
        /// </param>
        /// <returns>
        /// The maximum number of characters produced by decoding the specified
        /// number of bytes, which (for this one-to-one mapping) is equal to
        /// <paramref name="byteCount" />.
        /// </returns>
        public override int GetMaxCharCount(
            int byteCount
            )
        {
            return byteCount; /* one-to-one mapping */
        }
        #endregion
    }
}
