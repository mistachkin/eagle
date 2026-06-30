/*
 * HaveCultureInfo.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Globalization;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that expose read-write access
    /// to an associated <see cref="CultureInfo" /> instance.
    /// </summary>
    [ObjectId("8b91280d-3051-4377-9d95-82d52d540751")]
    public interface IHaveCultureInfo
    {
        /// <summary>
        /// Gets or sets the culture associated with this entity.  This value
        /// may be null.
        /// </summary>
        CultureInfo CultureInfo { get; set; }
    }
}
