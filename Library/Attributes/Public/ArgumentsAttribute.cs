/*
 * ArgumentsAttribute.cs --
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
    /// This attribute is used to associate the number of arguments (the arity)
    /// accepted by the class it is applied to, such as a command or function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("7f8636b6-0fc1-411e-9c93-c9a5e9ac8875")]
    public sealed class ArgumentsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified well-known
        /// argument arity.
        /// </summary>
        /// <param name="arity">
        /// The well-known argument arity to associate with the marked class.
        /// </param>
        public ArgumentsAttribute(Arity arity)
        {
            arguments = (int)arity;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified number of
        /// arguments.
        /// </summary>
        /// <param name="arguments">
        /// The number of arguments to associate with the marked class.
        /// </param>
        public ArgumentsAttribute(int arguments)
        {
            this.arguments = arguments;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the number of arguments.
        /// </summary>
        /// <param name="value">
        /// The number of arguments, as a string, to associate with the marked
        /// class.  This value must be parsable as an integer.
        /// </param>
        public ArgumentsAttribute(string value)
        {
            arguments = int.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of arguments associated with the marked class.
        /// </summary>
        private int arguments;
        /// <summary>
        /// Gets the number of arguments associated with the marked class.
        /// </summary>
        public int Arguments
        {
            get { return arguments; }
        }
    }
}
