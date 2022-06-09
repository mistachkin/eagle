/*
 * StringPair.cs --
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
    [ObjectId("2e342815-d397-43af-a5ac-8a8e5947967f")]
    public sealed class StringPair : Pair<string>
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        public StringPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringPair(string x)
            : base(x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringPair(string x, string y)
            : base(x, y)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        public static StringPair FromString(string value)
        {
            return new StringPair(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        public static implicit operator StringPair(string value)
        {
            return FromString(value);
        }
        #endregion
    }
}
