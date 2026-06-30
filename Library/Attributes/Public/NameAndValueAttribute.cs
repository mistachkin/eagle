/*
 * NameAndValueAttribute.cs --
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
    /// This attribute is used to associate an arbitrary name and value pair
    /// with the class, property, or field it is applied to.  It may be applied
    /// more than once to the same element.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Property |
        AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    [ObjectId("b94b9a58-deae-480b-8a5a-074ecd857fbb")]
    public sealed class NameAndValueAttribute : Attribute
    {
        #region Private Data
        /// <summary>
        /// The name associated with the marked element.
        /// </summary>
        private string name;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The value associated with the marked element.
        /// </summary>
        private string value;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified name and
        /// value pair.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the marked element.
        /// </param>
        /// <param name="value">
        /// The value to associate with the marked element.
        /// </param>
        public NameAndValueAttribute(
            string name, /* in */
            string value /* in */
            )
        {
            this.name = name;
            this.value = value;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the name associated with the marked element.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the value associated with the marked element.
        /// </summary>
        public string Value
        {
            get { return value; }
        }
        #endregion
    }
}
