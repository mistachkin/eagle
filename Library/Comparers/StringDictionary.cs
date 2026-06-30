/*
 * StringDictionary.cs --
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

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares and tests strings for equality using a dictionary
    /// ordering, in which embedded runs of digits are compared as numbers and
    /// case differences are treated only as a secondary ordering criterion.  It
    /// supports extracting a list element from each string prior to comparison
    /// and tracking duplicate counts on behalf of the list sorting subsystem.
    /// </summary>
    [ObjectId("6e8a6ed6-daea-4e9c-946c-cb425f2e73ed")]
    internal sealed class StringDictionaryComparer : IComparer<string>, IEqualityComparer<string>
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
        public StringDictionaryComparer(
            Interpreter interpreter,
            bool ascending,
            string indexText,
            bool leftOnly,
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
            this.unique = unique;
            this.cultureInfo = cultureInfo;
            this.duplicates = duplicates;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        /// <summary>
        /// Returns the character at the specified index within the specified
        /// string, or a null character if the string is null or the index is
        /// out of range.
        /// </summary>
        /// <param name="text">
        /// The string from which to retrieve a character.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the character to retrieve.
        /// </param>
        /// <returns>
        /// The character at the specified index, or a null character if it is
        /// not available.
        /// </returns>
        private static char GetChar(
            string text,
            int index
            )
        {
            char result = Characters.Null;

            if ((text != null) && ((index >= 0) && (index < text.Length)))
                result = text[index];

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        /// <summary>
        /// Compares two strings using a dictionary ordering and returns a value
        /// indicating their relative order, applying the configured index
        /// extraction and duplicate tracking.  Embedded digit runs are compared
        /// numerically and case is used only as a secondary criterion.
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

            if ((left != null) && (right != null))
            {
                int diff;
                int leftIndex = 0;
                int rightIndex = 0;
                int secondaryDiff = 0;

                while (true)
                {
                    if (Char.IsDigit(GetChar(left, leftIndex)) &&
                        Char.IsDigit(GetChar(right, rightIndex)))
                    {
                        /*
                         * There are decimal numbers embedded in the two
                         * strings.  Compare them as numbers, rather than
                         * strings.  If one number has more leading zeros than
                         * the other, the number with more leading zeros sorts
                         * later, but only as a secondary choice.
                         */

                        int zeros = 0;

                        while ((GetChar(right, rightIndex) == Characters.Zero) &&
                               Char.IsDigit(GetChar(right, rightIndex + 1)))
                        {
                            rightIndex++;
                            zeros--;
                        }

                        while ((GetChar(left, leftIndex) == Characters.Zero) &&
                               Char.IsDigit(GetChar(left, leftIndex + 1)))
                        {
                            leftIndex++;
                            zeros++;
                        }

                        if (secondaryDiff == 0)
                            secondaryDiff = zeros;

                        /*
                         * The code below compares the numbers in the two
                         * strings without ever converting them to integers.  It
                         * does this by first comparing the lengths of the
                         * numbers and then comparing the digit values.
                         */

                        diff = 0;

                        while (true)
                        {
                            if (diff == 0)
                                diff = GetChar(left, leftIndex) - GetChar(right, rightIndex);

                            rightIndex++;
                            leftIndex++;

                            if (!Char.IsDigit(GetChar(right, rightIndex)))
                            {
                                if (Char.IsDigit(GetChar(left, leftIndex)))
                                {
                                    return 1;
                                }
                                else
                                {
                                    /*
                                     * The two numbers have the same length. See
                                     * if their values are different.
                                     */

                                    if (diff != 0)
                                        return diff;

                                    break;
                                }
                            }
                            else if (!Char.IsDigit(GetChar(left, leftIndex)))
                            {
                                return -1;
                            }
                        }
                        continue;
                    }

                    /*
                     * Convert character to Unicode for comparison purposes.  If either
                     * string is at the terminating null, do a byte-wise comparison and
                     * bail out immediately.
                     */

                    char leftChar;
                    char rightChar;
                    char leftLower;
                    char rightLower;

                    if ((GetChar(left, leftIndex) != Characters.Null) &&
                        (GetChar(right, rightIndex) != Characters.Null))
                    {
                        leftChar = GetChar(left, leftIndex++);
                        rightChar = GetChar(right, rightIndex++);

                        /*
                         * Convert both chars to lower for the comparison, because
                         * dictionary sorts are case insensitive.  Covert to lower, not
                         * upper, so chars between Z and a will sort before A (where most
                         * other interesting punctuations occur)
                         */

                        leftLower = Char.ToLower(leftChar);
                        rightLower = Char.ToLower(rightChar);
                    }
                    else
                    {
                        diff = GetChar(left, leftIndex) - GetChar(right, rightIndex);
                        break;
                    }

                    diff = leftLower - rightLower;

                    if (diff != 0)
                    {
                        return diff;
                    }
                    else if (secondaryDiff == 0)
                    {
                        if (Char.IsUpper(leftChar) &&
                            Char.IsLower(rightChar))
                        {
                            secondaryDiff = -1;
                        }
                        else if (Char.IsUpper(rightChar) &&
                                 Char.IsLower(leftChar))
                        {
                            secondaryDiff = 1;
                        }
                    }
                }

                if (diff == 0)
                    diff = secondaryDiff;

                ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                    unique, diff, ref levels); /* throw */

                return diff;
            }
            else
            {
                if ((left == null) && (right == null))
                {
                    //
                    // NOTE: Currently, this function does nothing when passed null 
                    //       for either the left or right strings; however, this may
                    //       change in the future.
                    //
                    ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                        unique, 0, ref levels); /* throw */

                    return 0;
                }
                else
                {
                    if (left == null)
                        return -1;
                    else
                        return 1;
                }
            }
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
            return ListOps.ComparerGetHashCode<string>(this, value, false);
        }
        #endregion
    }
}
