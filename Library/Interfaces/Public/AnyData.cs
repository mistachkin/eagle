/*
 * AnyData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("95d45bb8-bbc5-4eaf-ad12-2b65bc08cab4")]
    public interface IAnyData : IAnyDataBase
    {
        bool TryResetAny();

        bool HasAny(
            string name
            );

        bool TryGetAny(
            string name,
            out object value
            );

        bool TrySetAny(
            string name,
            object value
            );

        bool TryUnsetAny(
            string name
            );
    }
}
