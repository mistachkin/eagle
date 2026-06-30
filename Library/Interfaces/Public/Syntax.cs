/*
 * Syntax.cs --
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
    /// This interface is implemented by entities that carry human-readable
    /// syntax (usage) metadata describing how they are invoked.
    /// </summary>
    [ObjectId("b616f5bd-92b5-4f5d-b4ee-d9154ec27cdd")]
    public interface ISyntax
    {
        /// <summary>
        /// Gets or sets the syntax (usage) string for this entity, describing
        /// its supported arguments.  This value may be null.
        /// </summary>
        string Syntax { get; set; }
    }
}