/*
 * PolicyEnsemble.cs --
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
    /// This interface is implemented by ensemble entities that constrain
    /// which sub-commands may be dispatched.  It exposes the explicit
    /// allow and disallow lists consulted when an ensemble policy decides
    /// whether a given sub-command is permitted.
    /// </summary>
    [ObjectId("4232d0b8-22b7-4738-9c69-4b751d44dec4")]
    public interface IPolicyEnsemble
    {
        /// <summary>
        /// Gets or sets the dictionary of sub-commands that are explicitly
        /// allowed for this ensemble.  This value may be null.
        /// </summary>
        EnsembleDictionary AllowedSubCommands { get; set; }
        /// <summary>
        /// Gets or sets the dictionary of sub-commands that are explicitly
        /// disallowed for this ensemble.  This value may be null.
        /// </summary>
        EnsembleDictionary DisallowedSubCommands { get; set; }
    }
}
