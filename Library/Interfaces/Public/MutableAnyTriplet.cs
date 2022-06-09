/*
 * MutableAnyTriplet.cs --
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
    [ObjectId("7ccb4d0a-1edc-4047-9aff-54cf7f4f82d8")]
    public interface IMutableAnyTriplet : IAnyTriplet /* INTERNAL: DO NOT USE */
    {
        bool Mutable { get; }

        new object X { get; [Throw(true)] set; }
        new object Y { get; [Throw(true)] set; }
        new object Z { get; [Throw(true)] set; }

        bool TrySetX(object value);
        bool TrySetY(object value);
        bool TrySetZ(object value);
    }

    ///////////////////////////////////////////////////////////////////////

    [ObjectId("db0bf1e1-3c6c-42ad-abbc-97617352d13a")]
    public interface IMutableAnyTriplet<T1, T2, T3> : IAnyTriplet<T1, T2, T3>
    {
        bool Mutable { get; }

        new T1 X { get; [Throw(true)] set; }
        new T2 Y { get; [Throw(true)] set; }
        new T3 Z { get; [Throw(true)] set; }
    }
}
