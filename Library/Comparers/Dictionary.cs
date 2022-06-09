/*
 * Dictionary.cs --
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
    [ObjectId("6e8a6ed6-daea-4e9c-946c-cb425f2e73ed")]
    internal sealed class StringDictionaryComparer : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private int levels;
        private Interpreter interpreter;
        private bool ascending;
        private string indexText;
        private bool leftOnly;
        private bool unique;
        private CultureInfo cultureInfo;
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
                duplicates = new IntDictionary(new Custom(this, this));

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
                         * dictionary sorts are case insensitve.  Covert to lower, not
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
        public bool Equals(
            string left,
            string right
            )
        {
            return ListOps.ComparerEquals(this, left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            string value
            )
        {
            return ListOps.ComparerGetHashCode(this, value, false);
        }
        #endregion
    }
}
