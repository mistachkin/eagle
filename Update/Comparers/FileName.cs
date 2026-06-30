/*
 * FileName.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Eagle._Components.Private;
using Eagle._Components.Shared;
using Eagle._Interfaces.Private;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares file name strings for ordering and equality, using
    /// the platform-appropriate string comparison and a hash derived from a
    /// configured text encoding.
    /// </summary>
    [Guid("a86b864a-2087-4856-8918-c7abffdeeb47")]
    internal sealed class FileName : IAnyComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The kind of string comparison used when comparing file names.
        /// </summary>
        private StringComparison comparisonType;

        /// <summary>
        /// The text encoding used when computing the hash code of a file name.
        /// </summary>
        private Encoding encoding;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this comparer that uses the UTF-8 encoding
        /// when computing hash codes.
        /// </summary>
        public FileName()
            : this(Encoding.UTF8)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this comparer.
        /// </summary>
        /// <param name="encoding">
        /// The text encoding used when computing the hash code of a file name.
        /// </param>
        public FileName(
            Encoding encoding
            )
        {
            this.comparisonType = FileOps.GetComparisonType();
            this.encoding = encoding;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        /// <summary>
        /// This method compares two file name strings.
        /// </summary>
        /// <param name="x">
        /// The first file name to compare.
        /// </param>
        /// <param name="y">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// A negative number if <paramref name="x" /> sorts before
        /// <paramref name="y" />, a positive number if it sorts after, or zero
        /// if they are equal.
        /// </returns>
        public int Compare(
            string x,
            string y
            )
        {
            return StringOps.Compare(x, y, comparisonType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        /// <summary>
        /// This method determines whether two file name strings are equal.
        /// </summary>
        /// <param name="x">
        /// The first file name to compare.
        /// </param>
        /// <param name="y">
        /// The second file name to compare.
        /// </param>
        /// <returns>
        /// True if the two file names are equal; otherwise, false.
        /// </returns>
        public bool Equals(
            string x,
            string y
            )
        {
            return Compare(x, y) == 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash code for the specified file name.
        /// </summary>
        /// <param name="obj">
        /// The file name to compute the hash code for.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The computed hash code, or zero if <paramref name="obj" /> is null
        /// or no encoding is available.
        /// </returns>
        public int GetHashCode(string obj)
        {
            int result = 0;

            if ((obj != null) && (encoding != null))
            {
                result ^= unchecked((int)HashOps.HashFnv1UInt(
                    encoding.GetBytes(obj), true));
            }

            return result;
        }
        #endregion
    }
}
