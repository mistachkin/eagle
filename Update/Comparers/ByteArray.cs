/*
 * ByteArray.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Eagle._Components.Private;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class provides an equality comparer for arrays of bytes, comparing
    /// two byte arrays for equality based on their contents.
    /// </summary>
    [Guid("0b8549d0-a0ce-4bb7-9517-b10ad13148ef")]
    internal sealed class ByteArray : IEqualityComparer<byte[]>
    {
        #region IEqualityComparer<byte[]> Members
        /// <summary>
        /// This method determines whether two byte arrays are equal based on
        /// their contents.
        /// </summary>
        /// <param name="x">
        /// The first byte array to be compared.
        /// </param>
        /// <param name="y">
        /// The second byte array to be compared.
        /// </param>
        /// <returns>
        /// True if the two byte arrays are equal; otherwise, false.
        /// </returns>
        public bool Equals(
            byte[] x,
            byte[] y
            )
        {
            return GenericOps<byte>.Equals(x, y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates a hash code for the specified byte array
        /// based on its contents.
        /// </summary>
        /// <param name="obj">
        /// The byte array for which the hash code is to be calculated.
        /// </param>
        /// <returns>
        /// The calculated hash code for the specified byte array.
        /// </returns>
        public int GetHashCode(
            byte[] obj
            )
        {
            return unchecked((int)HashOps.HashFnv1UInt(obj, true));
        }
        #endregion
    }
}
