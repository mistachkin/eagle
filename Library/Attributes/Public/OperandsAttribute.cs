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
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("e6def1d1-178f-45ef-b5c1-2c85b0211b34")]
    public sealed class OperandsAttribute : Attribute
    {
        public OperandsAttribute(Arity arity)
        {
            operands = (int)arity;
        }

        ///////////////////////////////////////////////////////////////////////

        public OperandsAttribute(int operands)
        {
            this.operands = operands;
        }

        ///////////////////////////////////////////////////////////////////////

        public OperandsAttribute(string value)
        {
            operands = int.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private int operands;
        public int Operands
        {
            get { return operands; }
        }
    }
}
