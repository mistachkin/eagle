/*
 * HaveEncoding.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that expose read-write access
    /// to an associated <see cref="Encoding" /> instance.
    /// </summary>
    [ObjectId("01eb0013-a2ab-4839-9b57-6ecfc13782fb")]
    public interface IHaveEncoding
    {
        /// <summary>
        /// Gets or sets the encoding associated with this entity.  This value
        /// may be null.
        /// </summary>
        Encoding Encoding { get; set; }
    }
}
