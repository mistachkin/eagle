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

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a pair of values that share a single type and
    /// that may optionally be mutable.  It is a convenience specialization of
    /// <see cref="MutableAnyPair{T1, T2}" /> in which both elements have the
    /// same type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of both values stored in the pair.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("513a7ba4-1477-4d2a-a47d-bf39602530fb")]
    public class MutablePair<T> : MutableAnyPair<T, T>, IMutablePair<T>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an immutable pair with both values set to their default.
        /// </summary>
        public MutablePair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable pair with the first value set to the
        /// specified value and the second value set to its default.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public MutablePair(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable pair with both values set to the specified
        /// values.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        /// <param name="y">
        /// The second value of the pair.
        /// </param>
        public MutablePair(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair, with both values set to their default, whose
        /// mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </param>
        public MutablePair(
            bool mutable
            )
            : base(mutable)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair, with the first value set to the specified value
        /// and the second value set to its default, whose mutability is
        /// determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public MutablePair(
            bool mutable,
            T x
            )
            : base(mutable, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair, with both values set to the specified values,
        /// whose mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        /// <param name="y">
        /// The second value of the pair.
        /// </param>
        public MutablePair(
            bool mutable,
            T x,
            T y
            )
            : base(mutable, x, y)
        {
            // do nothing.
        }
        #endregion
    }
}
