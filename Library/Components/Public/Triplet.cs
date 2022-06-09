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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("cf7ffcf2-c272-4a12-a602-37d404dd4218")]
    public class Triplet<T> : AnyTriplet<T, T, T>, ITriplet<T>
    {
        #region Public Constructors
        public Triplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public Triplet(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public Triplet(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
