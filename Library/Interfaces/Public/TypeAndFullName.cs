/*
 * TypeAndFullName.cs --
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
    [ObjectId("fab38a07-4a33-4df9-9323-36fb1e5e9952")]
    public interface ITypeAndFullName : ITypeAndName
    {
        string TypeFullName { get; set; }
    }
}
