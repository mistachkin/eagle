/*
 * AnyCollection.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections;
using System.Collections.Generic;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("80d86eb1-3a8f-4e9f-8b92-2db9a0ed5c98")]
    public interface IAnyCollection<T> : ICollection<T>, ICollection
    {
        // nothing.
    }
}
