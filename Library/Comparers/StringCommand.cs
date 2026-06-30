/*
 * StringCommand.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares and tests strings for equality by invoking a
    /// user-supplied script callback that returns an integer ordering result.
    /// It supports extracting a list element from each string prior to
    /// comparison and tracking duplicate counts on behalf of the list sorting
    /// subsystem.
    /// </summary>
    [ObjectId("8be45be8-9736-4387-b896-a84c3ac2b627")]
    internal sealed class StringCommandComparer : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        /// <summary>
        /// The number of nested comparison levels, used when tracking duplicate
        /// elements during a sort.
        /// </summary>
        private int levels;

        /// <summary>
        /// The interpreter context used when extracting list elements to
        /// compare and when recording error information, or null if none.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// The script callback invoked to compare two strings; it must return
        /// an integer ordering result.
        /// </summary>
        private ICallback callback;

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
        /// The culture used when extracting list elements to compare and when
        /// parsing the integer result returned by the callback.
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
        /// compare and when recording error information, or null if none.
        /// </param>
        /// <param name="callback">
        /// The script callback invoked to compare two strings; it must return
        /// an integer ordering result.
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
        /// The culture used when extracting list elements to compare and when
        /// parsing the integer result returned by the callback.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary used to record duplicate element counts.  If null, a
        /// new dictionary is created and returned via this parameter.
        /// </param>
        public StringCommandComparer(
            Interpreter interpreter,
            ICallback callback,
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
            this.callback = callback;
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
        /// Compares two strings by invoking the configured script callback and
        /// returns the integer ordering result it produces, applying the
        /// configured index extraction and duplicate tracking.
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

            ReturnCode code;
            Result result = null;

            if (callback != null)
            {
                code = callback.Invoke(new StringList(left, right), ref result);

                if (code == ReturnCode.Ok)
                {
                    int order = 0;

                    code = Value.GetInteger2((string)result, ValueFlags.AnyInteger,
                        cultureInfo, ref order, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        ListOps.UpdateDuplicateCount(this, duplicates, left, right,
                            unique, order, ref levels); /* throw */

                        return order;
                    }
                    else
                    {
                        result = "-compare command returned non-integer result"; /* COMPAT */
                    }
                }
                else
                {
                    //
                    // NOTE: Fetch the innermost active interpreter on the call stack since we 
                    //       are inside of a non-extensible .NET Framework callback interface 
                    //       and therefore have no direct access to our calling interpreter.
                    //
                    /* IGNORED */
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format("{0}    (-compare command)",
                            Environment.NewLine));
                }
            }
            else
            {
                result = "invalid sort command callback";
                code = ReturnCode.Error;
            }

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);
            else
                throw new ScriptException(); /* NOT REACHED */
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
