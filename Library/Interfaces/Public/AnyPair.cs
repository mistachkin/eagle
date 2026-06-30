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
    /// <summary>
    /// This interface represents an ordered pair of two arbitrary values,
    /// exposed as objects.
    /// </summary>
    [ObjectId("521d3aae-d4b1-4321-8fe1-a3e981111e51")]
    public interface IAnyPair
    {
        /// <summary>
        /// Gets the first value of the pair.  This value may be null.
        /// </summary>
        object X { get; }

        /// <summary>
        /// Gets the second value of the pair.  This value may be null.
        /// </summary>
        object Y { get; }
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents an ordered pair of two strongly typed
    /// values.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value of the pair.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value of the pair.
    /// </typeparam>
    [ObjectId("97655866-7ec2-4254-9997-ba51d60b6c62")]
    public interface IAnyPair<T1, T2>
    {
        /// <summary>
        /// Gets the first value of the pair.
        /// </summary>
        T1 X { get; }

        /// <summary>
        /// Gets the second value of the pair.
        /// </summary>
        T2 Y { get; }
    }
}
