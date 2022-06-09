/*
 * MutableAnyPair.cs --
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
    [ObjectId("d4b0d64e-9544-4719-b0f7-588cbb3c5abc")]
    public interface IMutableAnyPair : IAnyPair /* INTERNAL: DO NOT USE */
    {
        bool Mutable { get; }

        new object X { get; [Throw(true)] set; }
        new object Y { get; [Throw(true)] set; }

        bool TrySetX(object value);
        bool TrySetY(object value);
    }

    ///////////////////////////////////////////////////////////////////////

    [ObjectId("68863146-12dd-45fa-b2a9-3f0f2dd0e67b")]
    public interface IMutableAnyPair<T1, T2> : IAnyPair<T1, T2>
    {
        bool Mutable { get; }

        new T1 X { get; [Throw(true)] set; }
        new T2 Y { get; [Throw(true)] set; }
    }
}
