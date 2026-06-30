/*
 * TypeAndFullName.cs --
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
    /// This interface associates a type name and resolved type with the fully
    /// qualified name of that type.  It extends <see cref="ITypeAndName" />
    /// with the assembly-qualified, or otherwise fully qualified, type name.
    /// </summary>
    [ObjectId("fab38a07-4a33-4df9-9323-36fb1e5e9952")]
    public interface ITypeAndFullName : ITypeAndName
    {
        /// <summary>
        /// Gets or sets the fully qualified name of the type.
        /// </summary>
        string TypeFullName { get; set; }
    }
}
