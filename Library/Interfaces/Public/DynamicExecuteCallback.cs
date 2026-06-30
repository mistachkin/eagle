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
    /// <summary>
    /// This interface is implemented by entities whose execution is delegated
    /// to a dynamically assigned <see cref="ExecuteCallback" /> rather than to
    /// a fixed, compiled-in method body.  It exposes the callback so that it
    /// may be queried and replaced at runtime.
    /// </summary>
    [ObjectId("f0f847c0-a28c-4ebb-bf0c-a913b835069c")]
    public interface IDynamicExecuteCallback
    {
        /// <summary>
        /// Gets or sets the <see cref="ExecuteCallback" /> delegate that is
        /// invoked to perform the execution for this entity.  This value may
        /// be null, in which case no dynamic callback is associated with the
        /// entity.
        /// </summary>
        ExecuteCallback Callback { get; set; }
    }
}
