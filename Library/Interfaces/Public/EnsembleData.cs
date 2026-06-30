/*
 * EnsembleData.cs --
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
    /// This interface defines the identifying and configuration data
    /// associated with a command ensemble.  In addition to the identity
    /// (<see cref="IIdentifier" />) and wrapper (<see cref="IWrapperData" />)
    /// metadata it inherits, it exposes the entity used to execute the
    /// ensemble's sub-commands.
    /// </summary>
    [ObjectId("b929c34c-26d1-4ec5-b1e7-b136ddb74994")]
    public interface IEnsembleData : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets or sets the <see cref="IExecute" /> entity responsible for
        /// executing the sub-commands of this ensemble.  This value may be
        /// null if no custom sub-command execution entity is configured.
        /// </summary>
        IExecute SubCommandExecute { get; set; }
    }
}
