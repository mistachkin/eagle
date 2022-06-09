/*
 * Integer.cs --
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
    [ObjectId("338b2ea1-85f9-41c2-8f84-c6823e9c5d8e")]
    internal sealed class StringIntegerComparer : IComparer<string>, IEqualityComparer<string>
    {
        private int levels;
        private Interpreter interpreter;
        private bool ascending;
        private string indexText;
        private bool leftOnly;
        private bool unique;
        private CultureInfo cultureInfo;
        private IntDictionary duplicates;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringIntegerComparer(
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

            long leftWide = 0;

            if (Value.GetWideInteger2(left, ValueFlags.AnyWideInteger, cultureInfo,
                    ref leftWide, ref error) == ReturnCode.Ok)
            {
                long rightWide = 0;

                if (Value.GetWideInteger2(right, ValueFlags.AnyWideInteger, cultureInfo,
                        ref rightWide, ref error) == ReturnCode.Ok)
                {
                    int result = LogicOps.Compare(leftWide, rightWide);

                    ListOps.UpdateDuplicateCount(this, duplicates, leftWide.ToString(),
                        rightWide.ToString(), unique, result, ref levels); /* throw */

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
