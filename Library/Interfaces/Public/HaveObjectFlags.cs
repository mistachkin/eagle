/*
 * HaveObjectFlags.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("2471d451-241c-4a65-94fd-e483d4bb494a")]
    public interface IHaveObjectFlags
    {
        ObjectFlags ObjectFlags { get; set; }
    }
}
