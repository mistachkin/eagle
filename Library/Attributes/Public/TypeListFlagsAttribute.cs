/*
 * TypeListFlagsAttribute.cs --
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
    /// This class implements an attribute used to associate a set of
    /// <see cref="TypeListFlags" /> with the class it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("d7aacda2-e5bd-43bd-9b8a-d78a0ec4fca5")]
    public sealed class TypeListFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified type
        /// list flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to associate with the marked type.
        /// </param>
        public TypeListFlagsAttribute(TypeListFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified type
        /// list flags in their string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the flags to associate with the marked
        /// type.  An exception is thrown if this value cannot be parsed as a
        /// valid <see cref="TypeListFlags" /> value.
        /// </param>
        public TypeListFlagsAttribute(string value)
        {
            flags = (TypeListFlags)Enum.Parse(
                typeof(TypeListFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags associated with the marked type.
        /// </summary>
        private TypeListFlags flags;
        /// <summary>
        /// Gets the flags associated with the marked type.
        /// </summary>
        public TypeListFlags Flags
        {
            get { return flags; }
        }
    }
}
