/*
 * Regexp.cs --
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
    [ObjectId("ba6a6bca-570d-434d-b630-989729659975")]
    internal sealed class StringRegexpComparer : IComparer<string>, IEqualityComparer<string>
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
        //
        //  NOTE: This comparer tests for matching only.  If the text does not match the regular 
        //        expression pattern, a non-zero value will be returned; however, callers should
        //        NOT rely on the exact non-match value because it is meaningless.
        //
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
