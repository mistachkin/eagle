/*
 * IdentifierName.cs --
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
    /// This interface is the minimal identity contract that supplies the name
    /// of an entity managed by an Eagle interpreter.
    /// </summary>
    [ObjectId("dfdc14a1-94be-45bc-bdab-f0df1623eeb9")]
    public interface IIdentifierName
    {
        //
        // NOTE: The name of this identifier.
        //
        /// <summary>
        /// Gets or sets the name of this identifier.
        /// </summary>
        string Name { get; set; }
    }
}
