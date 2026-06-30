/*
 * EnsembleManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the set of
    /// sub-commands belonging to a command ensemble, allowing sub-commands to
    /// be added or updated at runtime.
    /// </summary>
    [ObjectId("23fa5163-9096-40cf-ba25-19494feaf08d")]
    public interface IEnsembleManager
    {
        /// <summary>
        /// Adds a new sub-command to the ensemble or updates the existing
        /// sub-command that has the specified name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use for this operation.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the sub-command to add or update.
        /// </param>
        /// <param name="subCommand">
        /// The sub-command to associate with the specified name.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to associate with the sub-command,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The <see cref="SubCommandFlags" /> that control how the sub-command
        /// is added or updated.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode AddOrUpdateSubCommand(
            Interpreter interpreter,
            string name,
            ISubCommand subCommand,
            IClientData clientData,
            SubCommandFlags flags,
            ref Result error
        );
    }
}
