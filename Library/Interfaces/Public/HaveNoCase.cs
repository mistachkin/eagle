/*
 * HaveNoCase.cs --
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
    [ObjectId("bd5a1865-dc0b-4170-a172-1ac6b4a5f28d")]
    public interface IHaveNoCase
    {
        bool NoCase { get; set; }
    }
}
