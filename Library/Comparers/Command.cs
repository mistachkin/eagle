/*
 * Command.cs --
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
    [ObjectId("8be45be8-9736-4387-b896-a84c3ac2b627")]
    internal sealed class StringCommandComparer : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private int levels;
        private Interpreter interpreter;
        private ICallback callback;
        private bool ascending;
        private string indexText;
        private bool leftOnly;
        private bool unique;
        private CultureInfo cultureInfo;
        private IntDictionary duplicates;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
                duplicates = new IntDictionary(new Custom(this, this));

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
