/*
 * StringAscii.cs --
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
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares and tests strings for equality using an ordinal
    /// (ASCII-style) comparison, optionally ignoring case.  It supports
    /// extracting a list element from each string prior to comparison and
    /// tracking duplicate counts on behalf of the list sorting subsystem.
    /// </summary>
    [ObjectId("24dcf855-93d7-49b8-9e5c-c1d68bf502a8")]
    internal sealed class StringAsciiComparer : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The number of nested comparison levels, used when tracking duplicate
        /// elements during a sort.
        /// </summary>
        private int levels;

        /// <summary>
        /// The interpreter context used when extracting list elements to
        /// compare, or null if none.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// When true, elements are compared in ascending order; otherwise, in
        /// descending order.
        /// </summary>
        private bool ascending;

        /// <summary>
        /// The index specification used to extract a sub-element from each
        /// string prior to comparison, or null to compare the whole string.
        /// </summary>
        private string indexText;

        /// <summary>
        /// When true, only the left operand has the index extraction applied to
        /// it during comparison.
        /// </summary>
        private bool leftOnly;

        /// <summary>
        /// When true, comparisons are performed without regard to character
        /// case.
        /// </summary>
        private bool noCase;

        /// <summary>
        /// When true, duplicate elements are tracked so that they may be
        /// removed from the sorted result.
        /// </summary>
        private bool unique;

        /// <summary>
        /// The culture used when extracting list elements to compare.
        /// </summary>
        private CultureInfo cultureInfo;

        /// <summary>
        /// The dictionary used to record the number of times each duplicate
        /// element has been seen during a sort.
        /// </summary>
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified comparison
        /// options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when extracting list elements to
        /// compare, or null if none.
        /// </param>
        /// <param name="ascending">
        /// When true, elements are compared in ascending order; otherwise, in
        /// descending order.
        /// </param>
        /// <param name="indexText">
        /// The index specification used to extract a sub-element from each
        /// string prior to comparison, or null to compare the whole string.
        /// </param>
        /// <param name="leftOnly">
        /// When true, only the left operand has the index extraction applied to
        /// it during comparison.
        /// </param>
        /// <param name="noCase">
        /// When true, comparisons are performed without regard to character
        /// case.
        /// </param>
        /// <param name="unique">
        /// When true, duplicate elements are tracked so that they may be
        /// removed from the sorted result.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when extracting list elements to compare.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary used to record duplicate element counts.  If null, a
        /// new dictionary is created and returned via this parameter.
        /// </param>
        public StringAsciiComparer(
            Interpreter interpreter,
            bool ascending,
            string indexText,
            bool leftOnly,
            bool noCase,
            bool unique,
            CultureInfo cultureInfo,
            ref IntDictionary duplicates
            )
        {
            if (duplicates == null)
                duplicates = new IntDictionary(new StringCustom(this, this));

            this.levels = 0;
            this.interpreter = interpreter;
            this.ascending = ascending;
            this.indexText = indexText;
            this.leftOnly = leftOnly;
            this.noCase = noCase;
            this.unique = unique;
            this.cultureInfo = cultureInfo;
            this.duplicates = duplicates;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        /// <summary>
        /// Compares two strings using an ordinal comparison and returns a value
        /// indicating their relative order, applying the configured index
        /// extraction and duplicate tracking.
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
            ListOps.GetElementsToCompare(
                interpreter, ascending, indexText, leftOnly, false,
                cultureInfo, ref left, ref right); /* throw */

            int result = SharedStringOps.Compare(left, right,
                SharedStringOps.GetSystemComparisonType(noCase));

            ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                unique, result, ref levels); /* throw */

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        /// <summary>
        /// Determines whether two strings are equal according to this
        /// comparer's ordering.
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
            return ListOps.ComparerEquals<string>(this, left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a hash code for the specified string that is consistent with
        /// this comparer's notion of equality.
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
            return ListOps.ComparerGetHashCode<string>(this, value, noCase);
        }
        #endregion
    }
}
