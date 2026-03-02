/*
 * CacheValue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("5f29ebf4-ab12-4019-982a-915e4c73051c")]
    internal interface ICacheValue
    {
        //
        // WARNING: This property is for private and/or diagnostic use only.
        //
        object CacheValue { get; }

        //
        // WARNING: This property is for private and/or diagnostic use only.
        //
        long CacheGeneration { get; }

        ///////////////////////////////////////////////////////////////////////

        object GetCacheValue(
            Interpreter interpreter,
            bool noGeneration
        );

        bool SetCacheValue(
            Interpreter interpreter,
            object value,
            bool noGeneration
        );
    }
}
