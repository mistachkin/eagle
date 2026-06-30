/*
 * HaveClientData.cs --
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
    /// This interface is implemented by entities that expose read-write access
    /// to their associated <see cref="IClientData" /> instance.  It composes
    /// the read-only accessor (<see cref="IGetClientData" />) and the
    /// write-only accessor (<see cref="ISetClientData" />).
    /// </summary>
    [ObjectId("ac1c43de-7488-4af8-92ef-7c5c7b82625f")]
    public interface IHaveClientData : IGetClientData, ISetClientData
    {
        /// <summary>
        /// Gets or sets the extra, entity-specific data associated with this
        /// entity, if any.  This value may be null.
        /// </summary>
        new IClientData ClientData { get; set; }
    }
}
