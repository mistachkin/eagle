/*
 * InteractiveLoopManager.cs --
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
    [ObjectId("1887a4ff-e362-4a74-8e8f-0785b2ed537e")]
    public interface IInteractiveLoopManager
    {
        InteractiveLoopCallback InteractiveLoopCallback { get; set; }
    }
}
