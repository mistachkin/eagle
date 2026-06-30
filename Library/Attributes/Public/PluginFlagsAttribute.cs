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
    /// <summary>
    /// This class implements an attribute used to associate a set of
    /// <see cref="PluginFlags" /> with the plugin class it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("6959784c-57b8-47fe-aac7-3d007e7ba979")]
    public sealed class PluginFlagsAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified plugin
        /// flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to associate with the marked plugin.
        /// </param>
        public PluginFlagsAttribute(PluginFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified plugin
        /// flags in their string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the flags to associate with the marked
        /// plugin.  An exception is thrown if this value cannot be parsed as a
        /// valid <see cref="PluginFlags" /> value.
        /// </param>
        public PluginFlagsAttribute(string value)
        {
            flags = (PluginFlags)Enum.Parse(
                typeof(PluginFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags associated with the marked plugin.
        /// </summary>
        private PluginFlags flags;
        /// <summary>
        /// Gets the flags associated with the marked plugin.
        /// </summary>
        public PluginFlags Flags
        {
            get { return flags; }
        }
    }
}
