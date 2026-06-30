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

namespace Eagle._SubCommands
{
    /// <summary>
    /// This class is the base class for the built-in (core) sub-commands of
    /// the Eagle core library.  It extends the default sub-command
    /// implementation by automatically applying the command flags declared via
    /// attributes on the derived type and its base type.
    /// </summary>
    [ObjectId("dcecceed-2dba-4784-8047-65fe0e8d3ddc")]
    [CommandFlags(CommandFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of a core sub-command.  Unless the supplied
        /// data opts out via the <see cref="CommandFlags.NoAttributes" />
        /// flag, the command flags declared on this type and its base type are
        /// merged into the current command flags.
        /// </summary>
        /// <param name="subCommandData">
        /// The data used to create and identify this sub-command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Core(
            ISubCommandData subCommandData
            )
            : base(subCommandData)
        {
            if ((subCommandData == null) || !FlagOps.HasFlags(
                    subCommandData.CommandFlags, CommandFlags.NoAttributes,
                    true))
            {
                this.CommandFlags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }
        }
        #endregion
    }
}
