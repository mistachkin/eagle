/*
 * ReadOnly.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("eb1581f0-2d25-4f61-87af-c8de7982ad5b")]
    internal interface IReadOnly
    {
        bool IsReadOnly { get; }
    }
}
