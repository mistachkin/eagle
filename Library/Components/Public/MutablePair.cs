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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("513a7ba4-1477-4d2a-a47d-bf39602530fb")]
    public class MutablePair<T> : MutableAnyPair<T, T>, IMutablePair<T>
    {
        #region Public Constructors
        public MutablePair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutablePair(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutablePair(
            T x,
            T y
            )
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutablePair(
            bool mutable
            )
            : base(mutable)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutablePair(
            bool mutable,
            T x
            )
            : base(mutable, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
