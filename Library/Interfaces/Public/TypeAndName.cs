/*
 * TypeAndName.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("764c2026-5b21-47dd-a397-746eef5a7e8c")]
    public interface ITypeAndName
    {
        string TypeName { get; set; }
        Type Type { get; set; }
    }
}
