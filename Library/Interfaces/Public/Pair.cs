/*
 * Pair.cs --
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
    [ObjectId("a95fdd8a-67b5-41be-b017-97d3b232c550")]
    public interface IPair : IAnyPair /* INTERNAL: DO NOT USE */
    {
        // nothing.
    }

    ///////////////////////////////////////////////////////////////////////

    [ObjectId("9c632656-3b1e-4e26-ac26-504665d0422a")]
    public interface IPair<T> : IAnyPair<T, T>
    {
        // nothing.
    }
}
