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
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("d9ae9052-5dbb-4b94-940d-c6a69909117c")]
    public sealed class FunctionFlagsAttribute : Attribute
    {
        public FunctionFlagsAttribute(FunctionFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public FunctionFlagsAttribute(string value)
        {
            flags = (FunctionFlags)Enum.Parse(
                typeof(FunctionFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private FunctionFlags flags;
        public FunctionFlags Flags
        {
            get { return flags; }
        }
    }
}
