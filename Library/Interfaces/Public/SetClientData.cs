/*
 * SetClientData.cs --
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
    /// This interface is implemented by entities that allow their associated
    /// client data to be set after they have been created.
    /// </summary>
    [ObjectId("e7bf1ddb-d01f-483a-bdbd-eb7df6677505")]
    public interface ISetClientData
    {
        /// <summary>
        /// Sets the extra, entity-specific data associated with this object.
        /// This value may be null.
        /// </summary>
        IClientData ClientData { set; }
    }
}
