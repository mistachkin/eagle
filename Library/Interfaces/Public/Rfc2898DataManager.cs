/*
 * Rfc2898DataManager.cs --
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
    /// This interface is implemented by entities that manage the data and the
    /// data provider used for RFC 2898 (PBKDF2) byte derivation.
    /// </summary>
    [ObjectId("0f334a93-1b7c-4e16-bfc3-45100a9b75b9")]
    public interface IRfc2898DataManager
    {
        /// <summary>
        /// Gets or sets the RFC 2898 data parameters managed by this entity.
        /// </summary>
        IRfc2898Data Rfc2898Data { get; set; }

        /// <summary>
        /// Gets or sets the RFC 2898 data provider used to obtain the data
        /// parameters.
        /// </summary>
        IRfc2898DataProvider Rfc2898DataProvider { get; set; }
    }
}
