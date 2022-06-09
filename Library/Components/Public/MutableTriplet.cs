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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("54a10e2f-f34b-4a29-8cb6-d323cc073dff")]
    public class MutableTriplet<T> :
        MutableAnyTriplet<T, T, T>,
        IMutableTriplet<T>
    {
        #region Public Constructors
        public MutableTriplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableTriplet(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableTriplet(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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

        public MutableTriplet(
            bool mutable
            )
            : base(mutable)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableTriplet(
            bool mutable,
            T x
            )
            : base(mutable, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
