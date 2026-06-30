/*
 * MutableTriplet.cs --
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
    /// This interface represents a mutable ordered triplet whose three
    /// arbitrary values, exposed as objects, share no common type constraint
    /// beyond <see cref="IMutableAnyTriplet" />.  It declares no additional
    /// members.
    /// </summary>
    [ObjectId("10bb2212-bef9-4c2a-95a1-4151b44c609c")]
    public interface IMutableTriplet : IMutableAnyTriplet /* INTERNAL: DO NOT USE */
    {
        // nothing.
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents a mutable ordered triplet whose three values
    /// are all of the same strongly typed element type.  It declares no
    /// additional members.
    /// </summary>
    /// <typeparam name="T">
    /// The type of all three values of the triplet.
    /// </typeparam>
    [ObjectId("3342a991-a34c-4371-862f-3154f1d06237")]
    public interface IMutableTriplet<T> : IMutableAnyTriplet<T, T, T>
    {
        // nothing.
    }
}
