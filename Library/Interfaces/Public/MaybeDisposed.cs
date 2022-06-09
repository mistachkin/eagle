/*
 * MaybeDisposed.cs --
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
    [ObjectId("a9546e47-8bab-484c-a7e6-e3ea30aa793f")]
    public interface IMaybeDisposed
    {
        //
        // WARNING: This property may not throw exceptions.
        //
        [Throw(false)]
        bool Disposed { get; }

        //
        // WARNING: This property may not throw exceptions.
        //
        [Throw(false)]
        bool Disposing { get; }
    }
}
