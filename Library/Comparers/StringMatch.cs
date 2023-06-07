/*
 * StringMatch.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;

namespace Eagle._Comparers
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0475f1ab-7e33-4467-b890-25e1c37338ab")]
    internal sealed class StringMatch : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private MatchMode mode;
        private bool noCase;
        private RegexOptions regExOptions;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringMatch(
            )
        {
            mode = StringOps.DefaultMatchMode;
            noCase = false;
            regExOptions = StringOps.DefaultRegExOptions;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public StringMatch(
            MatchMode mode,
            bool noCase
            )
            : this(mode, noCase, StringOps.DefaultRegExOptions)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringMatch(
            MatchMode mode,
            bool noCase,
            RegexOptions regExOptions
            )
            : this()
        {
            this.mode = mode;
            this.noCase = noCase;
            this.regExOptions = regExOptions;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        //
        //  NOTE: This comparer tests for matching only.  If the text does not match the pattern, a
        //        non-zero value will be returned; however, callers should NOT rely on the exact
        //        non-match value because it is meaningless.
        //
        public int Compare(
            string left,
            string right
            )
        {
            bool match = false;
            Result error = null;

            if (StringOps.Match(
                    null, mode, left, right, noCase, this,
                    regExOptions, ref match, ref error) == ReturnCode.Ok)
            {
                return ConversionOps.ToInt(!match);
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
            return ListOps.ComparerEquals<string>(this, left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            string value
            )
        {
            return ListOps.ComparerGetHashCode<string>(this, value, noCase);
        }
        #endregion
    }
}
