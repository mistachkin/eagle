/*
 * CommandFlagsAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Components.Public;

namespace Eagle._Attributes
{
    [AttributeUsage(CommandFlagsAttribute.Targets, Inherited = false)]
    [ObjectId("115be21d-293c-4f1b-a164-761db12e147c")]
    public sealed class CommandFlagsAttribute : Attribute
    {
        public const AttributeTargets Targets =
            AttributeTargets.Class | AttributeTargets.Method;

        ///////////////////////////////////////////////////////////////////////

        public CommandFlagsAttribute(CommandFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public CommandFlagsAttribute(string value)
        {
            flags = (CommandFlags)Enum.Parse(
                typeof(CommandFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private CommandFlags flags;
        public CommandFlags Flags
        {
            get { return flags; }
        }
    }
}
