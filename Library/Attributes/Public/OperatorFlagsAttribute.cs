/*
 * OperatorFlagsAttribute.cs --
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
    /// <see cref="OperatorFlags" /> with the operator class it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("1d563f7a-c512-452a-95a5-c59edab57a83")]
    public sealed class OperatorFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// operator flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to associate with the marked operator.
        /// </param>
        public OperatorFlagsAttribute(OperatorFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// operator flags in their string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the flags to associate with the marked
        /// operator.  An exception is thrown if this value cannot be parsed as
        /// a valid <see cref="OperatorFlags" /> value.
        /// </param>
        public OperatorFlagsAttribute(string value)
        {
            flags = (OperatorFlags)Enum.Parse(
                typeof(OperatorFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags associated with the marked operator.
        /// </summary>
        private OperatorFlags flags;
        /// <summary>
        /// Gets the flags associated with the marked operator.
        /// </summary>
        public OperatorFlags Flags
        {
            get { return flags; }
        }
    }
}
