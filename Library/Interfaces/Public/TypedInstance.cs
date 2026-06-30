/*
 * TypedInstance.cs --
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
    /// This interface represents an opaque object instance together with its
    /// type and the names by which it is known.  It extends
    /// <see cref="IHaveObjectFlags" /> with the underlying object, its type,
    /// its short and fully qualified object names, and any extra name parts.
    /// </summary>
    [ObjectId("b3e65c6d-0831-4d74-8ed9-6b6fcd085517")]
    public interface ITypedInstance : IHaveObjectFlags
    {
        /// <summary>
        /// Gets the type of the underlying object.
        /// </summary>
        Type Type { get; }
        /// <summary>
        /// Gets the underlying object instance.
        /// </summary>
        object Object { get; }
        /// <summary>
        /// Gets the short name of the object.
        /// </summary>
        string ObjectName { get; }
        /// <summary>
        /// Gets the fully qualified name of the object.
        /// </summary>
        string FullObjectName { get; }
        /// <summary>
        /// Gets the extra name parts associated with the object, if any.
        /// </summary>
        string[] ExtraParts { get; }
        /// <summary>
        /// Resets this typed instance to its default state.
        /// </summary>
        void Reset();
    }
}
