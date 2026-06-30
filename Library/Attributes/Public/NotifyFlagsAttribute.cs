/*
 * NotifyFlagsAttribute.cs --
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
    /// This attribute is used to associate a set of notification flags with
    /// the class it is applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("3697138f-bf83-4bfd-97dd-f8597013c98d")]
    public sealed class NotifyFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified
        /// notification flags.
        /// </summary>
        /// <param name="flags">
        /// The notification flags to associate with the marked class.
        /// </param>
        public NotifyFlagsAttribute(NotifyFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the notification flags.
        /// </summary>
        /// <param name="value">
        /// The notification flags, as a string, to associate with the marked
        /// class.  This value must be parsable as a value of the
        /// <see cref="NotifyFlags" /> enumeration.
        /// </param>
        public NotifyFlagsAttribute(string value)
        {
            flags = (NotifyFlags)Enum.Parse(
                typeof(NotifyFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The notification flags associated with the marked class.
        /// </summary>
        private NotifyFlags flags;
        /// <summary>
        /// Gets the notification flags associated with the marked class.
        /// </summary>
        public NotifyFlags Flags
        {
            get { return flags; }
        }
    }
}
