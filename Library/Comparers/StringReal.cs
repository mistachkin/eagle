/*
 * StringReal.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares two strings by interpreting them as real
    /// (floating-point) numbers and comparing their numeric values.  It is used
    /// to implement real-number list sorting for the script engine, and it
    /// tracks duplicate elements so that uniqueness may optionally be enforced.
    /// </summary>
    [ObjectId("974d030b-d68c-4f14-9d92-b7539ebc42af")]
    internal sealed class StringRealComparer : IComparer<string>, IEqualityComparer<string>
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
        public StringRealComparer(
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

        #region IComparer<string> Members
        /// <summary>
        /// This method compares two strings by interpreting each as a real
        /// (floating-point) number and comparing their numeric values.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number, indicating the
        /// relative order of the two numeric values.
        /// </returns>
        public int Compare(
            string left,
            string right
            )
        {
            Result error = null;

            ListOps.GetElementsToCompare(
                interpreter, ascending, indexText, leftOnly, false,
                cultureInfo, ref left, ref right); /* throw */

            INumber leftNumber = null;

            if ((Value.GetNumber(left, ValueFlags.AnyNumberAnyRadix, cultureInfo,
                    ref leftNumber, ref error) == ReturnCode.Ok) &&
                leftNumber.ConvertTo(TypeCode.Double))
            {
                double leftDouble = (double)leftNumber.Value;
                INumber rightNumber = null;

                if ((Value.GetNumber(right, ValueFlags.AnyNumberAnyRadix, cultureInfo,
                        ref rightNumber, ref error) == ReturnCode.Ok) &&
                    rightNumber.ConvertTo(TypeCode.Double))
                {
                    double rightDouble = (double)rightNumber.Value;

                    int result = LogicOps.Compare(leftDouble, rightDouble);

                    ListOps.UpdateDuplicateCount(this, duplicates, leftDouble.ToString(),
                        rightDouble.ToString(), unique, result, ref levels); /* throw */

                    return result;
                }
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
            return ListOps.ComparerGetHashCode<string>(this, value, false);
        }
        #endregion
    }
}
