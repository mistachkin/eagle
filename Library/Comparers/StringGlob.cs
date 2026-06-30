/*
 * StringGlob.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Private;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class tests strings against glob patterns by treating the left
    /// operand as text and the right operand as a glob pattern; it is used to
    /// group matching elements rather than to order them.  It supports
    /// extracting a list element from each string prior to matching and
    /// tracking duplicate counts on behalf of the list sorting subsystem.
    /// </summary>
    [ObjectId("e8192ac3-602c-46f8-abab-d19237ba64ca")]
    internal sealed class StringGlobComparer : IComparer<string>, IEqualityComparer<string>
    {
        /// <summary>
        /// The number of nested comparison levels, used when tracking duplicate
        /// elements during a sort.
        /// </summary>
        private int levels;

        /// <summary>
        /// The interpreter context used when extracting list elements to
        /// compare and when performing glob matching, or null if none.
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
        /// When true, glob matching is performed without regard to character
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with the specified matching
        /// options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when extracting list elements to
        /// compare and when performing glob matching, or null if none.
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
        /// When true, glob matching is performed without regard to character
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
        public StringGlobComparer(
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        //
        //  NOTE: This comparer tests for matching only.  If the text does not match the glob 
        //        pattern, a non-zero value will be returned; however, callers should NOT rely 
        //        on the exact non-match value because it is meaningless.
        //
        /// <summary>
        /// Tests whether the left string matches the right glob pattern,
        /// applying the configured index extraction and duplicate tracking.
        /// </summary>
        /// <param name="left">
        /// The text to test against the glob pattern.
        /// </param>
        /// <param name="right">
        /// The glob pattern to match against.
        /// </param>
        /// <returns>
        /// Zero if the text matches the pattern; otherwise, a non-zero value
        /// whose exact magnitude is not meaningful.
        /// </returns>
        public int Compare(
            string left,
            string right
            )
        {
            ListOps.GetElementsToCompare(
                interpreter, ascending, indexText, leftOnly, true,
                cultureInfo, ref left, ref right); /* throw */

            bool match = false;
            Result error = null;

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, left, right,
                    noCase, ref match, ref error) == ReturnCode.Ok)
            {
                int result = ConversionOps.ToInt(!match);

                ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                    unique, result, ref levels); /* throw */

                return result;
            }

            if (error != null)
                throw new ScriptException(error);
            else
                throw new ScriptException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        /// <summary>
        /// Determines whether two strings are equal according to this
        /// comparer's matching behavior.
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
