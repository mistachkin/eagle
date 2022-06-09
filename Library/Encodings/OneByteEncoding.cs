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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f36a83c4-1043-4db1-9e96-6ab30a188748")]
    public class OneByteEncoding : CoreEncoding
    {
        #region Public Constants
        public static readonly Encoding OneByte = new OneByteEncoding();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        internal static readonly string webName = "OneByte";
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
            return count; /* one-to-one mapping */
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
            int oldByteIndex = byteIndex;

            while (charCount-- > 0)
                //
                // NOTE: *WARNING* Lossy, one-to-one mapping.
                //
                bytes[byteIndex++] = ConversionOps.ToByte(chars[charIndex++]);

            return (byteIndex - oldByteIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetCharCount(
            byte[] bytes,
            int index,
            int count
            )
        {
            return count; /* one-to-one mapping */
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

            while (byteCount-- > 0)
                //
                // NOTE: Non-lossy, one-to-one mapping.
                //
                chars[charIndex++] = (char)bytes[byteIndex++];

            return (charIndex - oldCharIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetMaxByteCount(
            int charCount
            )
        {
            return charCount; /* one-to-one mapping */
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetMaxCharCount(
            int byteCount
            )
        {
            return byteCount; /* one-to-one mapping */
        }
        #endregion
    }
}
