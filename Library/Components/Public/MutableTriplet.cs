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

using System;
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a triplet of values that share a single type and
    /// that may optionally be mutable.  It is a convenience specialization of
    /// <see cref="MutableAnyTriplet{T1, T2, T3}" /> in which all three elements
    /// have the same type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of all three values stored in the triplet.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("54a10e2f-f34b-4a29-8cb6-d323cc073dff")]
    public class MutableTriplet<T> :
        MutableAnyTriplet<T, T, T>,
        IMutableTriplet<T>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an immutable triplet with all three values set to their
        /// default.
        /// </summary>
        public MutableTriplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with the first value set to the
        /// specified value and the remaining values set to their default.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        public MutableTriplet(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with the first two values set to the
        /// specified values and the third value set to its default.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        public MutableTriplet(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with all three values set to the
        /// specified values.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        /// <param name="z">
        /// The third value of the triplet.
        /// </param>
        public MutableTriplet(
            T x,
            T y,
            T z
            )
            : base(x, y, z)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with all three values set to their default,
        /// whose mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        public MutableTriplet(
            bool mutable
            )
            : base(mutable)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with the first value set to the specified
        /// value and the remaining values set to their default, whose
        /// mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        public MutableTriplet(
            bool mutable,
            T x
            )
            : base(mutable, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with the first two values set to the specified
        /// values and the third value set to its default, whose mutability is
        /// determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        public MutableTriplet(
            bool mutable,
            T x,
            T y
            )
            : base(mutable, x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with all three values set to the specified
        /// values, whose mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        /// <param name="z">
        /// The third value of the triplet.
        /// </param>
        public MutableTriplet(
            bool mutable,
            T x,
            T y,
            T z
            )
            : base(mutable, x, y, z)
        {
            // do nothing.
        }
        #endregion
    }
}
