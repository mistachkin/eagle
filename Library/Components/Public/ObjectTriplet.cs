/*
 * ObjectTriplet.cs --
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
    /// This class represents a triplet of values, each of an arbitrary object
    /// type.  It is a convenience specialization of <see cref="Triplet{T}" />
    /// in which all three elements are of type <see cref="object" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("1c391f18-feec-4142-95e5-55700564d121")]
    public sealed class ObjectTriplet : Triplet<object>
    {
        //
        // WARNING: This constructor produces an immutable null triplet object.
        //
        /// <summary>
        /// Constructs an immutable triplet with all three values set to null.
        /// </summary>
        public ObjectTriplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with the first value set to the
        /// specified value and the remaining values set to null.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        public ObjectTriplet(object x)
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with the first two values set to the
        /// specified values and the third value set to null.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        public ObjectTriplet(object x, object y)
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public ObjectTriplet(object x, object y, object z)
            : base(x, y, z)
        {
            // do nothing.
        }
    }
}

