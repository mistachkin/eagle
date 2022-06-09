/*
 * ObjectFlagsAttribute.cs --
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
    [ObjectId("f9af8412-95bc-44a7-b9d9-9c0dfdec41ac")]
    public sealed class ObjectFlagsAttribute : Attribute
    {
        public ObjectFlagsAttribute(ObjectFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectFlagsAttribute(string value)
        {
            flags = (ObjectFlags)Enum.Parse(
                typeof(ObjectFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private ObjectFlags flags;
        public ObjectFlags Flags
        {
            get { return flags; }
        }
    }
}
