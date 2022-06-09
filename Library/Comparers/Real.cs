/*
 * Real.cs --
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
    [ObjectId("974d030b-d68c-4f14-9d92-b7539ebc42af")]
    internal sealed class StringRealComparer : IComparer<string>, IEqualityComparer<string>
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

        #region IComparer<string> Members
        public int Compare(
            string left,
            string right
            )
        {
            Result error = null;

            ListOps.GetElementsToCompare(
                interpreter, ascending, indexText, leftOnly, false,
                cultureInfo, ref left, ref right); /* throw */

            Number leftNumber = null;

            if ((Value.GetNumber(left, ValueFlags.AnyNumberAnyRadix, cultureInfo,
                    ref leftNumber, ref error) == ReturnCode.Ok) &&
                leftNumber.ConvertTo(typeof(double)))
            {
                double leftDouble = (double)leftNumber.Value;
                Number rightNumber = null;

                if ((Value.GetNumber(right, ValueFlags.AnyNumberAnyRadix, cultureInfo,
                        ref rightNumber, ref error) == ReturnCode.Ok) &&
                    rightNumber.ConvertTo(typeof(double)))
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
