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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("954b090a-6916-42f7-bc27-8b4e66be1f7c")]
    public sealed class ObjectPair : Pair<object>
    {
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        public ObjectPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ObjectPair(object x)
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ObjectPair(object x, object y)
            : base(x, y)
        {
            // do nothing.
        }
    }
}

