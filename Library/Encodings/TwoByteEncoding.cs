/*
 * TwoByteEncoding.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Encodings
{
    /// <summary>
    /// This class implements a simple, lossless text encoding that maps each
    /// character to exactly two bytes (and vice versa) using little-endian
    /// byte order.  It is primarily intended for use within the Eagle core
    /// library where a fixed, deterministic two-byte representation of
    /// character data is required.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("53e6fe46-a477-44f1-bb64-c99903168a5e")]
    public class TwoByteEncoding : CoreEncoding
    {
        #region Public Constants
        /// <summary>
        /// A shared, pre-built instance of this encoding for general use.
        /// </summary>
        public static readonly Encoding TwoByte = new TwoByteEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The registered, human-readable name of this encoding.
        /// </summary>
        internal static readonly string webName = "TwoByte";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.Encoding Overrides
        /// <summary>
        /// Gets the registered, human-readable name of this encoding.
        /// </summary>
        public override string WebName
        {
            get { return webName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of bytes that would be produced
        /// by encoding the specified range of characters.
        /// </summary>
        /// <param name="chars">
        /// The array of characters to encode.
        /// </param>
        /// <param name="index">
        /// The index of the first character to encode.
        /// </param>
        /// <param name="count">
        /// The number of characters to encode.
        /// </param>
        /// <returns>
        /// The number of bytes that would be produced; this is always twice
        /// the value of <paramref name="count" />.
        /// </returns>
        public override int GetByteCount(
            char[] chars,
            int index,
            int count
            )
        {
            if (!MathOps.CanDouble(count))
                throw new ArgumentOutOfRangeException("count");

            return count * 2; /* one-to-two mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method encodes a range of characters into bytes, writing each
        /// character as two bytes in little-endian order into the destination
        /// array.
        /// </summary>
        /// <param name="chars">
        /// The array of characters to encode.
        /// </param>
        /// <param name="charIndex">
        /// The index of the first character to encode.
        /// </param>
        /// <param name="charCount">
        /// The number of characters to encode.
        /// </param>
        /// <param name="bytes">
        /// The destination array that receives the encoded bytes.
        /// </param>
        /// <param name="byteIndex">
        /// The index in the destination array at which to begin writing bytes.
        /// </param>
        /// <returns>
        /// The number of bytes actually written to the destination array.
        /// </returns>
        public override int GetBytes(
            char[] chars,
            int charIndex,
            int charCount,
            byte[] bytes,
            int byteIndex
            )
        {
            if (!MathOps.CanDouble(charCount))
                throw new ArgumentOutOfRangeException("charCount");

            int oldByteIndex = byteIndex;

            while (charCount-- > 0)
            {
                //
                // NOTE: Non-lossy, one-to-two mapping (LITTLE-ENDIAN).
                //
                bytes[byteIndex++] = ConversionOps.ToLowByte(chars[charIndex]);
                bytes[byteIndex++] = ConversionOps.ToHighByte(chars[charIndex]);

                //
                // NOTE: We just used one character.
                //
                charIndex++;
            }

            return (byteIndex - oldByteIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of characters that would be
        /// produced by decoding the specified range of bytes.  An extra
        /// character is included when an odd number of bytes is specified; the
        /// final character is padded with a null byte in that case.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes to decode.
        /// </param>
        /// <param name="index">
        /// The index of the first byte to decode.
        /// </param>
        /// <param name="count">
        /// The number of bytes to decode.
        /// </param>
        /// <returns>
        /// The number of characters that would be produced.
        /// </returns>
        public override int GetCharCount(
            byte[] bytes,
            int index,
            int count
            )
        {
            //
            // NOTE: Make sure to add an extra character here if there is an
            //       odd number of bytes specified.  The final character will
            //       be padded with a null byte in that case.
            //
            return (count / 2) + (count % 2); /* two-to-one mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes a range of bytes into characters, combining
        /// each pair of bytes (in little-endian order) into a single character
        /// in the destination array.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes to decode.
        /// </param>
        /// <param name="byteIndex">
        /// The index of the first byte to decode.
        /// </param>
        /// <param name="byteCount">
        /// The number of bytes to decode.
        /// </param>
        /// <param name="chars">
        /// The destination array that receives the decoded characters.
        /// </param>
        /// <param name="charIndex">
        /// The index in the destination array at which to begin writing
        /// characters.
        /// </param>
        /// <returns>
        /// The number of characters actually written to the destination array.
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

            while (byteCount > 0)
            {
                //
                // NOTE: Non-lossy, one-to-one mapping (LITTLE-ENDIAN).
                //
                chars[charIndex++] = ConversionOps.ToChar(
                    bytes[byteIndex++], bytes[byteIndex++]);

                //
                // NOTE: We just used two bytes.
                //
                byteCount -= 2;
            }

            return (charIndex - oldCharIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the maximum number of bytes that could be
        /// produced by encoding the specified number of characters.
        /// </summary>
        /// <param name="charCount">
        /// The number of characters to encode.
        /// </param>
        /// <returns>
        /// The maximum number of bytes that could be produced; this is always
        /// twice the value of <paramref name="charCount" />.
        /// </returns>
        public override int GetMaxByteCount(
            int charCount
            )
        {
            if (!MathOps.CanDouble(charCount))
                throw new ArgumentOutOfRangeException("charCount");

            return charCount * 2; /* one-to-two mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the maximum number of characters that could
        /// be produced by decoding the specified number of bytes.  An extra
        /// character is included when an odd number of bytes is specified.
        /// </summary>
        /// <param name="byteCount">
        /// The number of bytes to decode.
        /// </param>
        /// <returns>
        /// The maximum number of characters that could be produced.
        /// </returns>
        public override int GetMaxCharCount(
            int byteCount
            )
        {
            //
            // NOTE: Make sure to add an extra character here if there is an
            //       odd number of bytes specified.  The final character will
            //       be padded with a null byte in that case.
            //
            return (byteCount / 2) + (byteCount % 2); /* two-to-one mapping */
        }
        #endregion
    }
}
