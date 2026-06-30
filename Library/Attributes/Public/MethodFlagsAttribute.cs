/*
 * MethodFlagsAttribute.cs --
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
    /// This attribute is used to associate a set of method flags with the
    /// method it is applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [ObjectId("ba442aea-abdc-4f15-aef5-e41d6103e3e5")]
    public sealed class MethodFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified method
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The method flags to associate with the marked method.
        /// </param>
        public MethodFlagsAttribute(MethodFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the method flags.
        /// </summary>
        /// <param name="value">
        /// The method flags, as a string, to associate with the marked
        /// method.  This value must be parsable as a value of the
        /// <see cref="MethodFlags" /> enumeration.
        /// </param>
        public MethodFlagsAttribute(string value)
        {
            flags = (MethodFlags)Enum.Parse(
                typeof(MethodFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The method flags associated with the marked method.
        /// </summary>
        private MethodFlags flags;
        /// <summary>
        /// Gets the method flags associated with the marked method.
        /// </summary>
        public MethodFlags Flags
        {
            get { return flags; }
        }
    }
}
