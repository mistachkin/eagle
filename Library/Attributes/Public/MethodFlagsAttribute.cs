/*
 * MethodFlagsAttribute.cs --
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
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [ObjectId("ba442aea-abdc-4f15-aef5-e41d6103e3e5")]
    public sealed class MethodFlagsAttribute : Attribute
    {
        public MethodFlagsAttribute(MethodFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodFlagsAttribute(string value)
        {
            flags = (MethodFlags)Enum.Parse(
                typeof(MethodFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private MethodFlags flags;
        public MethodFlags Flags
        {
            get { return flags; }
        }
    }
}
