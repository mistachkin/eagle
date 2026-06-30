/*
 * Ensemble.cs --
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
    /// This interface is implemented by entities that act as a command
    /// ensemble, i.e. a command that dispatches to one of several named
    /// sub-commands based on its first argument.  It exposes the set of
    /// sub-commands that make up the ensemble.
    /// </summary>
    [ObjectId("c2a807b0-f6e2-4c06-b856-dbb37944fabd")]
    public interface IEnsemble
    {
        /// <summary>
        /// Gets or sets the dictionary of sub-commands belonging to this
        /// ensemble, keyed by sub-command name.  This value may be null if the
        /// ensemble has no sub-commands.
        /// </summary>
        EnsembleDictionary SubCommands { get; set; }
    }
}
