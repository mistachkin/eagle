/*
 * AnyTriplet.cs --
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
    /// This interface represents an ordered triplet of three arbitrary
    /// values, exposed as objects.
    /// </summary>
    [ObjectId("b1255d1b-868c-4a04-b2c1-7f49ef6171ea")]
    public interface IAnyTriplet
    {
        /// <summary>
        /// Gets the first value of the triplet.  This value may be null.
        /// </summary>
        object X { get; }

        /// <summary>
        /// Gets the second value of the triplet.  This value may be null.
        /// </summary>
        object Y { get; }

        /// <summary>
        /// Gets the third value of the triplet.  This value may be null.
        /// </summary>
        object Z { get; }
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents an ordered triplet of three strongly typed
    /// values.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value of the triplet.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value of the triplet.
    /// </typeparam>
    /// <typeparam name="T3">
    /// The type of the third value of the triplet.
    /// </typeparam>
    [ObjectId("8937ed3b-1e45-43a8-ad8e-c390fac435b7")]
    public interface IAnyTriplet<T1, T2, T3>
    {
        /// <summary>
        /// Gets the first value of the triplet.
        /// </summary>
        T1 X { get; }

        /// <summary>
        /// Gets the second value of the triplet.
        /// </summary>
        T2 Y { get; }

        /// <summary>
        /// Gets the third value of the triplet.
        /// </summary>
        T3 Z { get; }
    }
}
