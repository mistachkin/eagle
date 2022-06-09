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
    [ObjectId("50ea9bf8-6aa3-48a1-ae11-2813b9133fdd")]
    [CommandFlags(CommandFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default
    {
        #region Public Constructors
        //
        // NOTE: In the future, behavior specific to commands in the core will 
        //       be implemented here rather than in _Commands.Default (which
        //       is available to external commands to derive from).  For now,
        //       the primary job of this class is to set the cached command 
        //       flags correctly for all commands in the core command set.
        //
        public Core(
            ICommandData commandData
            )
            : base(commandData)
        {
            this.Flags |= AttributeOps.GetCommandFlags(GetType().BaseType) |
                AttributeOps.GetCommandFlags(this);
        }
        #endregion
    }
}
