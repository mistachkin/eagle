/*
 * DynamicExecuteTrace.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("8ab3d0fe-2aba-40db-9a86-cf464b1d9ac7")]
    public interface IDynamicExecuteTrace
    {
        TraceCallback Callback { get; set; }
    }
}
