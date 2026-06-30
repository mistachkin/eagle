/*
 * ObjectPair.cs --
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

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a pair of values, each of an arbitrary object
    /// type.  It is a convenience specialization of <see cref="Pair{T}" /> in
    /// which both elements are of type <see cref="object" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("954b090a-6916-42f7-bc27-8b4e66be1f7c")]
    public sealed class ObjectPair : Pair<object>
    {
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        /// <summary>
        /// Constructs an immutable pair with both values set to null.
        /// </summary>
        public ObjectPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable pair with the first value set to the
        /// specified value and the second value set to null.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public ObjectPair(object x)
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public ObjectPair(object x, object y)
            : base(x, y)
        {
            // do nothing.
        }
    }
}

