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
    /// <summary>
    /// This interface represents a mutable ordered triplet of three arbitrary
    /// values, exposed as objects.  It extends <see cref="IAnyTriplet" /> with
    /// the ability to replace any value after construction.
    /// </summary>
    [ObjectId("7ccb4d0a-1edc-4047-9aff-54cf7f4f82d8")]
    public interface IMutableAnyTriplet : IAnyTriplet /* INTERNAL: DO NOT USE */
    {
        /// <summary>
        /// Gets a value indicating whether the values of this triplet may be
        /// modified.  True if the triplet is mutable; otherwise, false.
        /// </summary>
        bool Mutable { get; }

        /// <summary>
        /// Gets or sets the first value of the triplet.  This value may be
        /// null.
        /// </summary>
        new object X { get; [Throw(true)] set; }
        /// <summary>
        /// Gets or sets the second value of the triplet.  This value may be
        /// null.
        /// </summary>
        new object Y { get; [Throw(true)] set; }
        /// <summary>
        /// Gets or sets the third value of the triplet.  This value may be
        /// null.
        /// </summary>
        new object Z { get; [Throw(true)] set; }

        /// <summary>
        /// Attempts to set the first value of the triplet without raising an
        /// exception when the triplet is immutable.
        /// </summary>
        /// <param name="value">
        /// The new first value of the triplet.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the value was set successfully; otherwise, false.
        /// </returns>
        bool TrySetX(object value);
        /// <summary>
        /// Attempts to set the second value of the triplet without raising an
        /// exception when the triplet is immutable.
        /// </summary>
        /// <param name="value">
        /// The new second value of the triplet.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the value was set successfully; otherwise, false.
        /// </returns>
        bool TrySetY(object value);
        /// <summary>
        /// Attempts to set the third value of the triplet without raising an
        /// exception when the triplet is immutable.
        /// </summary>
        /// <param name="value">
        /// The new third value of the triplet.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the value was set successfully; otherwise, false.
        /// </returns>
        bool TrySetZ(object value);
    }

    ///////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This interface represents a mutable ordered triplet of three strongly
    /// typed values.  It extends <see cref="IAnyTriplet{T1, T2, T3}" /> with
    /// the ability to replace any value after construction.
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
    [ObjectId("db0bf1e1-3c6c-42ad-abbc-97617352d13a")]
    public interface IMutableAnyTriplet<T1, T2, T3> : IAnyTriplet<T1, T2, T3>
    {
        /// <summary>
        /// Gets a value indicating whether the values of this triplet may be
        /// modified.  True if the triplet is mutable; otherwise, false.
        /// </summary>
        bool Mutable { get; }

        /// <summary>
        /// Gets or sets the first value of the triplet.
        /// </summary>
        new T1 X { get; [Throw(true)] set; }
        /// <summary>
        /// Gets or sets the second value of the triplet.
        /// </summary>
        new T2 Y { get; [Throw(true)] set; }
        /// <summary>
        /// Gets or sets the third value of the triplet.
        /// </summary>
        new T3 Z { get; [Throw(true)] set; }
    }
}
