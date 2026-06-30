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

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents an ordered pair of values in which both elements
    /// share the same type.  It is a specialization of
    /// <see cref="AnyPair{T1, T2}" /> where the two element types are
    /// identical.
    /// </summary>
    /// <typeparam name="T">
    /// The type of both elements stored in this pair.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("81b9b427-76ad-41e7-9352-25488c740044")]
    public class Pair<T> : AnyPair<T, T>, IPair<T>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty pair with both elements set to their default
        /// values.
        /// </summary>
        public Pair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair using the specified value for its first element;
        /// the second element is set to its default value.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of this pair.
        /// </param>
        public Pair(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair using the specified values for its first and
        /// second elements.
        /// </summary>
        /// <param name="x">
        /// The value to use for the first element of this pair.
        /// </param>
        /// <param name="y">
        /// The value to use for the second element of this pair.
        /// </param>
        public Pair(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }
        #endregion
    }
}
