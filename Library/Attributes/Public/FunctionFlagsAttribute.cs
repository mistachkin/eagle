/*
 * FunctionFlagsAttribute.cs --
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
    /// This attribute is used to associate a set of function flags with the
    /// class it is applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("d9ae9052-5dbb-4b94-940d-c6a69909117c")]
    public sealed class FunctionFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified function
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The function flags to associate with the marked class.
        /// </param>
        public FunctionFlagsAttribute(FunctionFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the function flags.
        /// </summary>
        /// <param name="value">
        /// The function flags, as a string, to associate with the marked
        /// class.  This value must be parsable as a value of the
        /// <see cref="FunctionFlags" /> enumeration.
        /// </param>
        public FunctionFlagsAttribute(string value)
        {
            flags = (FunctionFlags)Enum.Parse(
                typeof(FunctionFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The function flags associated with the marked class.
        /// </summary>
        private FunctionFlags flags;
        /// <summary>
        /// Gets the function flags associated with the marked class.
        /// </summary>
        public FunctionFlags Flags
        {
            get { return flags; }
        }
    }
}
