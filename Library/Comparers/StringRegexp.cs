/*
 * StringRegexp.cs --
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
    /// This class compares two strings by testing whether one matches the
    /// other when treated as a regular expression pattern.  It is used to
    /// implement regular expression list sorting for the script engine, and it
    /// tracks duplicate elements so that uniqueness may optionally be enforced.
    /// </summary>
    [ObjectId("ba6a6bca-570d-434d-b630-989729659975")]
    internal sealed class StringRegexpComparer : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The current recursion depth used while tracking duplicate elements.
        /// </summary>
        private int levels;

        /// <summary>
        /// The interpreter context used when extracting the elements to be
        /// compared.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Non-zero if the elements are being sorted in ascending order.
        /// </summary>
        private bool ascending;

        /// <summary>
        /// The list index used to extract the sub-element to compare from each
        /// value, or null to compare the entire value.
        /// </summary>
        private string indexText;

        /// <summary>
        /// Non-zero to apply index-based extraction to the left value only.
        /// </summary>
        private bool leftOnly;

        /// <summary>
        /// Non-zero to perform case-insensitive pattern matching.
        /// </summary>
        private bool noCase;

        /// <summary>
        /// Non-zero to enforce uniqueness by tracking and counting duplicate
        /// elements.
        /// </summary>
        private bool unique;

        /// <summary>
        /// The culture used when extracting and comparing the elements.
        /// </summary>
        private CultureInfo cultureInfo;

        /// <summary>
        /// The dictionary used to track the number of duplicate elements
        /// encountered during comparison.
        /// </summary>
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with the specified comparison
        /// settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when extracting the elements to be
        /// compared.
        /// </param>
        /// <param name="ascending">
        /// Non-zero if the elements are being sorted in ascending order.
        /// </param>
        /// <param name="indexText">
        /// The list index used to extract the sub-element to compare from each
        /// value, or null to compare the entire value.
        /// </param>
        /// <param name="leftOnly">
        /// Non-zero to apply index-based extraction to the left value only.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="unique">
        /// Non-zero to enforce uniqueness by tracking and counting duplicate
        /// elements.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when extracting and comparing the elements.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary used to track the number of duplicate elements.  When
        /// null, a new dictionary is created and stored here.
        /// </param>
        public StringRegexpComparer(
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
        //
        //  NOTE: This comparer tests for matching only.  If the text does not match the regular 
        //        expression pattern, a non-zero value will be returned; however, callers should
        //        NOT rely on the exact non-match value because it is meaningless.
        //
        /// <summary>
        /// This method compares two strings by testing whether the right string
        /// matches the left string when treated as a regular expression
        /// pattern.
        /// </summary>
        /// <param name="left">
        /// The first string to compare, treated as a regular expression
        /// pattern.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare, treated as the input text.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the input text matches the pattern; otherwise, a non-zero
        /// value whose exact magnitude is meaningless.
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
                    interpreter, MatchMode.RegExp, left, right,
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
        /// This method determines whether two strings are considered equal by
        /// this comparer.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two strings are considered equal; otherwise, false.
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
        /// This method returns a hash code for the specified string.
        /// </summary>
        /// <param name="value">
        /// The string for which a hash code is computed.  This parameter may be
        /// null.
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
