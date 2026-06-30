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
    /// <summary>
    /// This interface represents a mutable ordered pair of two arbitrary
    /// values, exposed as objects.  It extends <see cref="IAnyPair" /> with
    /// the ability to replace either value after construction.
    /// </summary>
    [ObjectId("d4b0d64e-9544-4719-b0f7-588cbb3c5abc")]
    public interface IMutableAnyPair : IAnyPair /* INTERNAL: DO NOT USE */
    {
        /// <summary>
        /// Gets a value indicating whether the values of this pair may be
        /// modified.  True if the pair is mutable; otherwise, false.
        /// </summary>
        bool Mutable { get; }

        /// <summary>
        /// Gets or sets the first value of the pair.  This value may be null.
        /// </summary>
        new object X { get; [Throw(true)] set; }
        /// <summary>
        /// Gets or sets the second value of the pair.  This value may be null.
        /// </summary>
        new object Y { get; [Throw(true)] set; }

        /// <summary>
        /// Attempts to set the first value of the pair without raising an
        /// exception when the pair is immutable.
        /// </summary>
        /// <param name="value">
        /// The new first value of the pair.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the value was set successfully; otherwise, false.
        /// </returns>
        bool TrySetX(object value);
        /// <summary>
        /// Attempts to set the second value of the pair without raising an
        /// exception when the pair is immutable.
        /// </summary>
        /// <param name="value">
        /// The new second value of the pair.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the value was set successfully; otherwise, false.
        /// </returns>
        bool TrySetY(object value);
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents a mutable ordered pair of two strongly typed
    /// values.  It extends <see cref="IAnyPair{T1, T2}" /> with the ability to
    /// replace either value after construction.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value of the pair.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value of the pair.
    /// </typeparam>
    [ObjectId("68863146-12dd-45fa-b2a9-3f0f2dd0e67b")]
    public interface IMutableAnyPair<T1, T2> : IAnyPair<T1, T2>
    {
        /// <summary>
        /// Gets a value indicating whether the values of this pair may be
        /// modified.  True if the pair is mutable; otherwise, false.
        /// </summary>
        bool Mutable { get; }

        /// <summary>
        /// Gets or sets the first value of the pair.
        /// </summary>
        new T1 X { get; [Throw(true)] set; }
        /// <summary>
        /// Gets or sets the second value of the pair.
        /// </summary>
        new T2 Y { get; [Throw(true)] set; }
    }
}
