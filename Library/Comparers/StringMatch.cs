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
    /// <summary>
    /// This class tests strings against a pattern by treating the left operand
    /// as text and the right operand as a pattern of the configured
    /// <see cref="MatchMode" /> (for example, glob or regular expression); it is
    /// used to group matching elements rather than to order them.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0475f1ab-7e33-4467-b890-25e1c37338ab")]
    internal sealed class StringMatch : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The matching mode (for example, glob or regular expression) used
        /// when testing text against a pattern.
        /// </summary>
        private MatchMode mode;

        /// <summary>
        /// When true, matching is performed without regard to character case.
        /// </summary>
        private bool noCase;

        /// <summary>
        /// The regular expression options used when the matching mode is
        /// regular expression.
        /// </summary>
        private RegexOptions regExOptions;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the default matching mode,
        /// case-sensitive matching, and the default regular expression options.
        /// </summary>
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
        /// <summary>
        /// Constructs an instance of this class using the specified matching
        /// mode and case sensitivity, and the default regular expression
        /// options.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used when testing text against a pattern.
        /// </param>
        /// <param name="noCase">
        /// When true, matching is performed without regard to character case.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class using the specified matching
        /// mode, case sensitivity, and regular expression options.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used when testing text against a pattern.
        /// </param>
        /// <param name="noCase">
        /// When true, matching is performed without regard to character case.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when the matching mode is
        /// regular expression.
        /// </param>
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
        /// <summary>
        /// Tests whether the left string matches the right pattern according to
        /// the configured matching mode.
        /// </summary>
        /// <param name="left">
        /// The text to test against the pattern.
        /// </param>
        /// <param name="right">
        /// The pattern to match against.
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
