/*
 * MutablePair.cs --
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
    [ObjectId("c86a59d7-ff8d-4b6c-9879-d72f8da3a400")]
    public interface IMutablePair : IMutableAnyPair /* INTERNAL: DO NOT USE */
    {
        // nothing.
    }

    ///////////////////////////////////////////////////////////////////////

    [ObjectId("0ac3e8a0-f04e-44a5-a165-6935c6ce7ef9")]
    public interface IMutablePair<T> : IMutableAnyPair<T, T>
    {
        // nothing.
    }
}
