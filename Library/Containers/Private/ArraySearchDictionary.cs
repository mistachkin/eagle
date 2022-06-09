/*
 * ArraySearchDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Containers.Private
{
    [ObjectId("3f992cdc-cf6c-49c0-82fa-0f91c8ff2113")]
    internal sealed class ArraySearchDictionary : Dictionary<string, ArraySearch>
    {
        public ArraySearchDictionary()
            : base()
        {
            // do nothing.
        }
    }
}

