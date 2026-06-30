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
    /// <summary>
    /// This interface is implemented by entities that manage the callback
    /// used to drive the Eagle interactive loop.  It exposes the hook that
    /// allows the default interactive loop implementation to be replaced or
    /// augmented.
    /// </summary>
    [ObjectId("1887a4ff-e362-4a74-8e8f-0785b2ed537e")]
    public interface IInteractiveLoopManager
    {
        /// <summary>
        /// Gets or sets the callback used to run the interactive loop.  When
        /// non-null, this callback is used in place of the default interactive
        /// loop implementation.
        /// </summary>
        InteractiveLoopCallback InteractiveLoopCallback { get; set; }
    }
}
