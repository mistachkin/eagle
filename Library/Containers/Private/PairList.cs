/*
 * PairList.cs --
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

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("fc0e1512-3837-462e-a7bb-01460fcede60")]
    internal class PairList<T> : List<IPair<T>>
    {
        public PairList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PairList(
            IEnumerable<IPair<T>> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PairList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PairList(
            params IPair<T>[] pairs
            )
            : base(pairs)
        {
            // do nothing.
        }
    }
}
