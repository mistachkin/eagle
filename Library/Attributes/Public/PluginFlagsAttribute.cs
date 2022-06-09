/*
 * PluginFlagsAttribute.cs --
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
    [ObjectId("6959784c-57b8-47fe-aac7-3d007e7ba979")]
    public sealed class PluginFlagsAttribute : Attribute
    {
        public PluginFlagsAttribute(PluginFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public PluginFlagsAttribute(string value)
        {
            flags = (PluginFlags)Enum.Parse(
                typeof(PluginFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private PluginFlags flags;
        public PluginFlags Flags
        {
            get { return flags; }
        }
    }
}
