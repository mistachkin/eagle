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
    /// <summary>
    /// This interface represents an immutable pair of two related values.
    /// It is a non-generic aggregate over <see cref="IAnyPair" /> and adds
    /// no members of its own.  The <see cref="IAnyPair" /> base is marked
    /// for internal use only and should not be relied upon directly.
    /// </summary>
    [ObjectId("a95fdd8a-67b5-41be-b017-97d3b232c550")]
    public interface IPair : IAnyPair /* INTERNAL: DO NOT USE */
    {
        // nothing.
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents an immutable pair of two values that share
    /// the same type <typeparamref name="T" />.  It is a generic aggregate
    /// over <see cref="IAnyPair{T1,T2}" /> with both element types fixed to
    /// <typeparamref name="T" /> and adds no members of its own.
    /// </summary>
    /// <typeparam name="T">
    /// The type of both values held by this pair.
    /// </typeparam>
    [ObjectId("9c632656-3b1e-4e26-ac26-504665d0422a")]
    public interface IPair<T> : IAnyPair<T, T>
    {
        // nothing.
    }
}
