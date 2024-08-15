/*
 * SupportVariable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("485b2465-1daf-48f4-9a5c-802978eeea41")]
    public interface ISupportVariable
    {
        #region Array Sub-Command Helper Methods
        bool DoesExist(
            Interpreter interpreter,
            string name
        );

        ///////////////////////////////////////////////////////////////////////

        long? GetCount(
            Interpreter interpreter,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ObjectDictionary GetList(
            Interpreter interpreter,
            bool names,
            bool values,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ObjectDictionary GetList(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            bool names,
            bool values,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        string KeysToString(
            Interpreter interpreter,
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        string KeysAndValuesToString(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref Result error
        );
        #endregion
    }
}
