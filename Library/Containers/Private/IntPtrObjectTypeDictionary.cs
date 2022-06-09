/*
 * IntPtrObjectTypeDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("8442b84d-f17b-44e2-8a78-ae1ec7caed74")]
    internal sealed class IntPtrObjectTypeDictionary : Dictionary<IntPtr, IObjectType>
    {
        public IntPtrObjectTypeDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
