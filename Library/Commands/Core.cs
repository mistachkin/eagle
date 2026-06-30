/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class is the base class for all commands that are part of the
    /// Eagle core command set.  It derives from <see cref="Default" /> and
    /// exists so that behavior common to core commands can be implemented in
    /// one place; presently its primary job is to compute and cache the
    /// correct command flags for every command in the core command set.  See
    /// <c>core_language.md</c> for the core command syntax and semantics.
    /// </summary>
    [ObjectId("50ea9bf8-6aa3-48a1-ae11-2813b9133fdd")]
    [CommandFlags(CommandFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default
    {
        #region Public Constructors
        //
        // NOTE: In the future, behavior specific to commands in the core
        //       will be implemented here rather than in _Commands.Default
        //       (which is available to external commands to derive from).
        //       For now, the primary job of this class is to set the
        //       cached command flags correctly for all commands in the
        //       core command set.
        //
        /// <summary>
        /// Constructs an instance of a core command, combining the command
        /// flags declared on this type and its base type into the cached
        /// flags unless the supplied command data opts out via
        /// <see cref="CommandFlags.NoAttributes" />.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Core(
            ICommandData commandData
            )
            : base(commandData)
        {
            if ((commandData == null) || !FlagOps.HasFlags(
                    commandData.Flags, CommandFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }
        }
        #endregion
    }
}
