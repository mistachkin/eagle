/*
 * TypeAndName.cs --
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
    /// This interface associates the name of a type with its resolved
    /// <see cref="System.Type" /> instance, allowing a type to be referred to
    /// by name and, once resolved, by its reflected type object.
    /// </summary>
    [ObjectId("764c2026-5b21-47dd-a397-746eef5a7e8c")]
    public interface ITypeAndName
    {
        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        string TypeName { get; set; }
        /// <summary>
        /// Gets or sets the resolved type.
        /// </summary>
        Type Type { get; set; }
    }
}
