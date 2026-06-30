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
    /// <summary>
    /// This interface is implemented by entities whose execution is delegated
    /// to a dynamically assigned <see cref="TraceCallback" /> rather than to a
    /// fixed, compiled-in method body.  It exposes the trace callback so that
    /// it may be queried and replaced at runtime.
    /// </summary>
    [ObjectId("8ab3d0fe-2aba-40db-9a86-cf464b1d9ac7")]
    public interface IDynamicExecuteTrace
    {
        /// <summary>
        /// Gets or sets the <see cref="TraceCallback" /> delegate that is
        /// invoked to perform the execution for this entity.  This value may
        /// be null, in which case no dynamic trace callback is associated with
        /// the entity.
        /// </summary>
        TraceCallback Callback { get; set; }
    }
}
