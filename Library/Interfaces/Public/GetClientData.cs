/*
 * GetClientData.cs --
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
    /// This interface is implemented by entities that expose read-only access
    /// to their associated <see cref="IClientData" /> instance.
    /// </summary>
    [ObjectId("3ff94706-1943-4118-b973-6d2eb7266dd6")]
    public interface IGetClientData
    {
        /// <summary>
        /// Gets the extra, entity-specific data associated with this entity, if
        /// any.  This value may be null.
        /// </summary>
        IClientData ClientData { get; }
    }
}
