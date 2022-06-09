/*
 * Ascii.cs --
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
    [ObjectId("24dcf855-93d7-49b8-9e5c-c1d68bf502a8")]
    internal sealed class StringAsciiComparer : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private int levels;
        private Interpreter interpreter;
        private bool ascending;
        private string indexText;
        private bool leftOnly;
        private bool noCase;
        private bool unique;
        private CultureInfo cultureInfo;
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
                duplicates = new IntDictionary(new Custom(this, this));

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
            return ListOps.ComparerGetHashCode(this, value, noCase);
        }
        #endregion
    }
}
