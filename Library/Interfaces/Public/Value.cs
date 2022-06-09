/*
 * Value.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("faa4913e-e3d6-4b75-bcaa-4773a2fc41f0")]
    public interface IValue : IGetValue, ISetValue
    {
        new object Value { get; set; }
    }
}
