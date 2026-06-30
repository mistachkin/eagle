/*
 * Triplet.cs --
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
    /// This class represents an ordered triple of values that all share the
    /// same type.  It is a specialization of <see cref="AnyTriplet{T1, T2, T3}" />
    /// where the element types are identical.
    /// </summary>
    /// <typeparam name="T">
    /// The type of each of the three elements of the triple.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("cf7ffcf2-c272-4a12-a602-37d404dd4218")]
    public class Triplet<T> : AnyTriplet<T, T, T>, ITriplet<T>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class that contains no values.
        /// </summary>
        public Triplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the specified value
        /// for its first element.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of the triple.
        /// </param>
        public Triplet(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the specified values
        /// for its first and second elements.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of the triple.
        /// </param>
        /// <param name="y">
        /// The value to use for the second element of the triple.
        /// </param>
        public Triplet(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class using the specified values
        /// for its first, second, and third elements.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of the triple.
        /// </param>
        /// <param name="y">
        /// The value to use for the second element of the triple.
        /// </param>
        /// <param name="z">
        /// The value to use for the third element of the triple.
        /// </param>
        public Triplet(
            T x,
            T y,
            T z
            )
            : base(x, y, z)
        {
            // do nothing.
        }
        #endregion
    }
}
