/*
 * TypeListFlagsAttribute.cs --
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
    [ObjectId("d7aacda2-e5bd-43bd-9b8a-d78a0ec4fca5")]
    public sealed class TypeListFlagsAttribute : Attribute
    {
        public TypeListFlagsAttribute(TypeListFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public TypeListFlagsAttribute(string value)
        {
            flags = (TypeListFlags)Enum.Parse(
                typeof(TypeListFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private TypeListFlags flags;
        public TypeListFlags Flags
        {
            get { return flags; }
        }
    }
}
