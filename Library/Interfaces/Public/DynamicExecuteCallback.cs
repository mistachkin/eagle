/*
 * DynamicExecuteCallback.cs --
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
    [ObjectId("f0f847c0-a28c-4ebb-bf0c-a913b835069c")]
    public interface IDynamicExecuteCallback
    {
        ExecuteCallback Callback { get; set; }
    }
}
