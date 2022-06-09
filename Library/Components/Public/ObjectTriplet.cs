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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("1c391f18-feec-4142-95e5-55700564d121")]
    public sealed class ObjectTriplet : Triplet<object>
    {
        //
        // WARNING: This constructor produces an immutable null triplet object.
        //
        public ObjectTriplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ObjectTriplet(object x)
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ObjectTriplet(object x, object y)
            : base(x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ObjectTriplet(object x, object y, object z)
            : base(x, y, z)
        {
            // do nothing.
        }
    }
}

