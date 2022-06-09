/*
 * AnyPair.cs --
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
    [ObjectId("521d3aae-d4b1-4321-8fe1-a3e981111e51")]
    public interface IAnyPair
    {
        object X { get; }
        object Y { get; }
    }

    ///////////////////////////////////////////////////////////////////////

    [ObjectId("97655866-7ec2-4254-9997-ba51d60b6c62")]
    public interface IAnyPair<T1, T2>
    {
        T1 X { get; }
        T2 Y { get; }
    }
}
