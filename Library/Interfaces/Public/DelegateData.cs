/*
 * DelegateData.cs --
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
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("01746c05-81b9-4348-a02a-fd42feb82ae8")]
    public interface IDelegateData : IIdentifier, IWrapperData, IDynamicExecuteDelegate
    {
        DelegateFlags DelegateFlags { get; set; }
    }
}
