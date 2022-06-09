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
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("7f8636b6-0fc1-411e-9c93-c9a5e9ac8875")]
    public sealed class ArgumentsAttribute : Attribute
    {
        public ArgumentsAttribute(Arity arity)
        {
            arguments = (int)arity;
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentsAttribute(int arguments)
        {
            this.arguments = arguments;
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentsAttribute(string value)
        {
            arguments = int.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private int arguments;
        public int Arguments
        {
            get { return arguments; }
        }
    }
}
