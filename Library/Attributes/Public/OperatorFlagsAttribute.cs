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
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("1d563f7a-c512-452a-95a5-c59edab57a83")]
    public sealed class OperatorFlagsAttribute : Attribute
    {
        public OperatorFlagsAttribute(OperatorFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public OperatorFlagsAttribute(string value)
        {
            flags = (OperatorFlags)Enum.Parse(
                typeof(OperatorFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private OperatorFlags flags;
        public OperatorFlags Flags
        {
            get { return flags; }
        }
    }
}
