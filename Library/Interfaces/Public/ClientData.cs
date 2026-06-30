/*
 * ClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by objects that carry an arbitrary,
    /// caller-supplied data payload through the Eagle engine.  It provides a
    /// single, opaque slot that the core library passes along unchanged so
    /// that callers may associate their own context with an entity (e.g. a
    /// command, alias, or callback) and retrieve it later.
    /// </summary>
    [ObjectId("54293fde-8bf7-4df1-8b1c-4dd374feace4")]
    public interface IClientData
    {
        /// <summary>
        /// Gets or sets the opaque, caller-supplied data payload associated
        /// with this object.  This value may be null.
        /// </summary>
        object Data { get; set; }
    }
}
