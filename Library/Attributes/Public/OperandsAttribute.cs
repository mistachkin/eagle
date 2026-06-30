/*
 * OperandsAttribute.cs --
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
    /// This class implements an attribute used to declare the number of
    /// operands accepted by the operator class it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("e6def1d1-178f-45ef-b5c1-2c85b0211b34")]
    public sealed class OperandsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// well-known operand arity.
        /// </summary>
        /// <param name="arity">
        /// The well-known arity describing the number of operands accepted by
        /// the marked operator.
        /// </param>
        public OperandsAttribute(Arity arity)
        {
            operands = (int)arity;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified number
        /// of operands.
        /// </summary>
        /// <param name="operands">
        /// The number of operands accepted by the marked operator.
        /// </param>
        public OperandsAttribute(int operands)
        {
            this.operands = operands;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified number
        /// of operands in its string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the number of operands accepted by the
        /// marked operator.  An exception is thrown if this value cannot be
        /// parsed as an integer.
        /// </param>
        public OperandsAttribute(string value)
        {
            operands = int.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of operands accepted by the marked operator.
        /// </summary>
        private int operands;
        /// <summary>
        /// Gets the number of operands accepted by the marked operator.
        /// </summary>
        public int Operands
        {
            get { return operands; }
        }
    }
}
