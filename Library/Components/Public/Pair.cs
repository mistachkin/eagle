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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("81b9b427-76ad-41e7-9352-25488c740044")]
    public class Pair<T> : AnyPair<T, T>, IPair<T>
    {
        #region Public Constructors
        public Pair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public Pair(
            T x
            )
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
