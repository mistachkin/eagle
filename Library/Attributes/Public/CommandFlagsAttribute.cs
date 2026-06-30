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
    /// <summary>
    /// This attribute is used to associate a set of command flags with the
    /// class, method, field, property, or constructor it is applied to.
    /// </summary>
    [AttributeUsage(CommandFlagsAttribute.Targets, Inherited = false)]
    [ObjectId("115be21d-293c-4f1b-a164-761db12e147c")]
    public sealed class CommandFlagsAttribute : Attribute
    {
        /// <summary>
        /// The set of attribute targets that this attribute may be applied to.
        /// </summary>
        public const AttributeTargets Targets =
            AttributeTargets.Class | AttributeTargets.Method |
            AttributeTargets.Field | AttributeTargets.Property |
            AttributeTargets.Constructor;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified command
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The command flags to associate with the marked element.
        /// </param>
        public CommandFlagsAttribute(CommandFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the command flags.
        /// </summary>
        /// <param name="value">
        /// The command flags, as a string, to associate with the marked
        /// element.  This value must be parsable as a value of the
        /// <see cref="CommandFlags" /> enumeration.
        /// </param>
        public CommandFlagsAttribute(string value)
        {
            flags = (CommandFlags)Enum.Parse(
                typeof(CommandFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The command flags associated with the marked element.
        /// </summary>
        private CommandFlags flags;
        /// <summary>
        /// Gets the command flags associated with the marked element.
        /// </summary>
        public CommandFlags Flags
        {
            get { return flags; }
        }
    }
}
