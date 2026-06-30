/*
 * HavePlugin.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that are associated with a
    /// particular plugin, such as the plugin that provided or owns them.
    /// </summary>
    [ObjectId("a5a70378-2683-489c-9770-a048fd46be9a")]
    public interface IHavePlugin
    {
        /// <summary>
        /// Gets or sets the <see cref="IPlugin" /> associated with this
        /// entity.  This value may be null.
        /// </summary>
        IPlugin Plugin { get; set; }
    }
}
