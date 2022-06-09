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
    [ObjectId("dcecceed-2dba-4784-8047-65fe0e8d3ddc")]
    [CommandFlags(CommandFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default
    {
        #region Public Constructors
        public Core(
            ISubCommandData subCommandData
            )
            : base(subCommandData)
        {
            this.CommandFlags |=
                AttributeOps.GetCommandFlags(GetType().BaseType) |
                AttributeOps.GetCommandFlags(this);
        }
        #endregion
    }
}
