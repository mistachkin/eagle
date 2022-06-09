/*
 * AnyDataBase.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("1a040ae5-b312-478b-b69e-ebfc6e66be08")]
    public interface IAnyDataBase
    {
        bool TryResetAny(
            ref Result error
            );

        bool TryHasAny(
            string name,
            ref bool hasAny,
            ref Result error
            );

        bool TryListAny(
            string pattern,
            bool noCase,
            ref IList<string> list,
            ref Result error
            );

        bool TryGetAny(
            string name,
            out object value,
            ref Result error
            );

        bool TrySetAny(
            string name,
            object value,
            bool overwrite,
            bool create,
            bool toString,
            ref Result error
            );

        bool TryUnsetAny(
            string name,
            ref Result error
            );
    }
}
