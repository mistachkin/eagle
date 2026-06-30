/*
 * ObjectFlagsAttribute.cs --
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
    /// This attribute is used to associate a set of object flags with the
    /// class it is applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("f9af8412-95bc-44a7-b9d9-9c0dfdec41ac")]
    public sealed class ObjectFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified object
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The object flags to associate with the marked class.
        /// </param>
        public ObjectFlagsAttribute(ObjectFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the object flags.
        /// </summary>
        /// <param name="value">
        /// The object flags, as a string, to associate with the marked class.
        /// This value must be parsable as a value of the
        /// <see cref="ObjectFlags" /> enumeration.
        /// </param>
        public ObjectFlagsAttribute(string value)
        {
            flags = (ObjectFlags)Enum.Parse(
                typeof(ObjectFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The object flags associated with the marked class.
        /// </summary>
        private ObjectFlags flags;
        /// <summary>
        /// Gets the object flags associated with the marked class.
        /// </summary>
        public ObjectFlags Flags
        {
            get { return flags; }
        }
    }
}
