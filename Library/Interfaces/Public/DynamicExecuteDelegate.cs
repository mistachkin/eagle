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
    /// <summary>
    /// This interface is implemented by entities whose execution is delegated
    /// to a dynamically assigned <see cref="System.Delegate" /> rather than to
    /// a fixed, compiled-in method body.  It exposes the delegate so that it
    /// may be queried and replaced at runtime.
    /// </summary>
    [ObjectId("e70c328b-ca39-41ca-9326-06cd312e6700")]
    public interface IDynamicExecuteDelegate
    {
        /// <summary>
        /// Gets or sets the <see cref="System.Delegate" /> that is invoked to
        /// perform the execution for this entity.  This value may be null, in
        /// which case no dynamic delegate is associated with the entity.
        /// </summary>
        Delegate Delegate { get; set; }
    }
}
