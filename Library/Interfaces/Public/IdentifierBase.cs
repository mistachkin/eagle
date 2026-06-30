/*
 * IdentifierBase.cs --
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
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is the core identity contract for entities managed by an
    /// Eagle interpreter.  It composes the name member
    /// (<see cref="IIdentifierName" />) and adds the enumerated kind and the
    /// unique identifier that together distinguish one entity from another.
    /// </summary>
    [ObjectId("cb8c4833-74ff-48b4-9991-689317a6c9da")]
    public interface IIdentifierBase : IIdentifierName
    {
        //
        // NOTE: The enumerated kind of this identifier
        //       (i.e. command, plugin, etc).
        //
        /// <summary>
        /// Gets or sets the enumerated kind of this identifier (for example,
        /// command or plugin).
        /// </summary>
        IdentifierKind Kind { get; set; }

        //
        // NOTE: The unique Id of this identifier.
        //
        /// <summary>
        /// Gets or sets the unique identifier associated with this identifier.
        /// </summary>
        Guid Id { get; set; }
    }
}
