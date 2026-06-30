/*
 * ParameterIndexAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

namespace Eagle._Attributes
{
    /// <summary>
    /// This class implements an attribute used to associate a parameter index
    /// with the field it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    [ObjectId("dcc2a37d-263a-472a-91df-2c37d8a303b9")]
    public sealed class ParameterIndexAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// parameter index.
        /// </summary>
        /// <param name="index">
        /// The parameter index to associate with the marked field.
        /// </param>
        public ParameterIndexAttribute(int index)
        {
            this.index = index;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// parameter index in its string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the parameter index to associate with
        /// the marked field.  An exception is thrown if this value cannot be
        /// parsed as an integer.
        /// </param>
        public ParameterIndexAttribute(string value)
        {
            index = int.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The parameter index associated with the marked field.
        /// </summary>
        private int index;
        /// <summary>
        /// Gets the parameter index associated with the marked field.
        /// </summary>
        public int Index
        {
            get { return index; }
        }
    }
}
