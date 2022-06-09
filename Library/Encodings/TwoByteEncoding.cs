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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("53e6fe46-a477-44f1-bb64-c99903168a5e")]
    public class TwoByteEncoding : CoreEncoding
    {
        #region Public Constants
        public static readonly Encoding TwoByte = new TwoByteEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        internal static readonly string webName = "TwoByte";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Text.Encoding Overrides
        public override string WebName
        {
            get { return webName; }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public override int GetMaxByteCount(
            int charCount
            )
        {
            if (!MathOps.CanDouble(charCount))
                throw new ArgumentOutOfRangeException("charCount");

            return charCount * 2; /* one-to-two mapping */
        }

        ///////////////////////////////////////////////////////////////////////

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
