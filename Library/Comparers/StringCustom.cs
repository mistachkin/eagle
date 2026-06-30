/*
 * StringCustom.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares and tests strings for equality, either by delegating
    /// to a pair of custom comparers supplied by the caller or, when none are
    /// supplied, by using the comparer associated with a configured
    /// <see cref="StringComparison" /> value.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("938e1374-e595-4e83-adda-132a3a363424")]
    internal sealed class StringCustom :
        IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The string comparison kind used to obtain a comparer when no custom
        /// comparer or equality comparer has been supplied.
        /// </summary>
        private StringComparison comparisonType;

        /// <summary>
        /// The custom comparer used to order strings, or null to use the
        /// comparer associated with the configured comparison kind.
        /// </summary>
        private IComparer<string> comparer;

        /// <summary>
        /// The custom equality comparer used to test strings for equality and
        /// to compute hash codes, or null to use the comparer associated with
        /// the configured comparison kind.
        /// </summary>
        private IEqualityComparer<string> equalityComparer;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class that obtains its comparer from
        /// the specified string comparison kind.
        /// </summary>
        /// <param name="comparisonType">
        /// The string comparison kind used to obtain a comparer.
        /// </param>
        public StringCustom(
            StringComparison comparisonType
            )
        {
            this.comparisonType = comparisonType;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that delegates to the specified
        /// custom comparer and equality comparer.
        /// </summary>
        /// <param name="comparer">
        /// The custom comparer used to order strings, or null to use the
        /// comparer associated with the configured comparison kind.
        /// </param>
        /// <param name="equalityComparer">
        /// The custom equality comparer used to test strings for equality and
        /// to compute hash codes, or null to use the comparer associated with
        /// the configured comparison kind.
        /// </param>
        public StringCustom(
            IComparer<string> comparer,
            IEqualityComparer<string> equalityComparer
            )
        {
            this.comparer = comparer;
            this.equalityComparer = equalityComparer;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that delegates to the specified
        /// custom comparer and uses the default equality comparer.
        /// </summary>
        /// <param name="comparer">
        /// The custom comparer used to order strings.
        /// </param>
        private StringCustom(
            IComparer<string> comparer
            )
            : this(comparer, null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that delegates to the specified
        /// custom equality comparer and uses the default comparer.
        /// </summary>
        /// <param name="equalityComparer">
        /// The custom equality comparer used to test strings for equality and
        /// to compute hash codes.
        /// </param>
        private StringCustom(
            IEqualityComparer<string> equalityComparer
            )
            : this(null, equalityComparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        /// <summary>
        /// Compares two strings and returns a value indicating their relative
        /// order, using the custom comparer when one is present or otherwise the
        /// comparer associated with the configured comparison kind.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.
        /// </param>
        /// <param name="right">
        /// The second string to compare.
        /// </param>
        /// <returns>
        /// Less than zero if <paramref name="left" /> is less than
        /// <paramref name="right" />, zero if they are equal, and greater than
        /// zero if <paramref name="left" /> is greater than
        /// <paramref name="right" />.
        /// </returns>
        public int Compare(
            string left,
            string right
            )
        {
            if (comparer != null)
                return comparer.Compare(left, right);

            return StringOps.GetStringComparer(
                comparisonType).Compare(left, right);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        /// <summary>
        /// Determines whether two strings are equal, using the custom equality
        /// comparer when one is present or otherwise the comparer associated
        /// with the configured comparison kind.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.
        /// </param>
        /// <param name="right">
        /// The second string to compare.
        /// </param>
        /// <returns>
        /// True if the strings are considered equal; otherwise, false.
        /// </returns>
        public bool Equals(
            string left,
            string right
            )
        {
            if (equalityComparer != null)
                return equalityComparer.Equals(left, right);

            return StringOps.GetStringComparer(
                comparisonType).Equals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a hash code for the specified string, using the custom
        /// equality comparer when one is present or otherwise the comparer
        /// associated with the configured comparison kind.
        /// </summary>
        /// <param name="value">
        /// The string for which a hash code is to be computed.
        /// </param>
        /// <returns>
        /// A hash code for the specified string.
        /// </returns>
        public int GetHashCode(
            string value
            )
        {
            if (equalityComparer != null)
                return equalityComparer.GetHashCode(value);

            return StringOps.GetStringComparer(
                comparisonType).GetHashCode(value);
        }
        #endregion
    }
}
