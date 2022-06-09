/*
 * DynamicExecuteDelegate.cs --
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
    [ObjectId("e70c328b-ca39-41ca-9326-06cd312e6700")]
    public interface IDynamicExecuteDelegate
    {
        Delegate Delegate { get; set; }
    }
}
