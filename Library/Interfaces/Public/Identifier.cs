/*
 * Identifier.cs --
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
    /// This interface is the standard identity contract for entities managed
    /// by an Eagle interpreter.  It composes the core identity members
    /// (<see cref="IIdentifierBase" />) and per-entity client data
    /// (<see cref="IHaveClientData" />), and adds a logical group and a
    /// human-readable description.
    /// </summary>
    [ObjectId("513d51f3-f12a-48d7-a26b-02e27734bd50")]
    public interface IIdentifier : IIdentifierBase, IHaveClientData
    {
        //
        // NOTE: The logical group this identifier belongs to.
        //
        /// <summary>
        /// Gets or sets the logical group that this identifier belongs to.
        /// </summary>
        string Group { get; set; }

        //
        // NOTE: The description of this identifier.
        //
        /// <summary>
        /// Gets or sets the human-readable description of this identifier.
        /// </summary>
        string Description { get; set; }
    }
}
