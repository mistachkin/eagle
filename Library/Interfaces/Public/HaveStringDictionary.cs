/*
 * HaveStringDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that are associated with a
    /// <see cref="StringDictionary" /> of name and value pairs.
    /// </summary>
    [ObjectId("e8c9a7ca-18d4-4d32-b619-7fac72925254")]
    public interface IHaveStringDictionary
    {
        /// <summary>
        /// Gets or sets the <see cref="StringDictionary" /> associated with
        /// this entity.  This value may be null.
        /// </summary>
        StringDictionary Dictionary { get; set; }
    }
}
