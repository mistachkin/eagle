/*
 * InteractiveManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that provide support for the
    /// Eagle interactive mode.  It exposes the interactive host, the current
    /// and previous interactive input, the interactive mode, and the callback
    /// used to transform interactive commands.
    /// </summary>
    [ObjectId("95afaaa3-ddc1-423d-9b4a-c6b6464fb689")]
    public interface IInteractiveManager
    {
        ///////////////////////////////////////////////////////////////////////
        // INTERACTIVE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the interactive host used for input and output during
        /// interactive mode.
        /// </summary>
        IInteractiveHost InteractiveHost { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the interpreter is
        /// currently in interactive mode.
        /// </summary>
        bool Interactive { get; set; }

        /// <summary>
        /// Gets or sets the current interactive input.
        /// </summary>
        string InteractiveInput { get; set; }

        /// <summary>
        /// Gets or sets the previous interactive input.
        /// </summary>
        string PreviousInteractiveInput { get; set; }

        /// <summary>
        /// Gets or sets the current interactive mode.
        /// </summary>
        string InteractiveMode { get; set; }

        /// <summary>
        /// Gets or sets the callback used to transform interactive commands
        /// prior to their evaluation.
        /// </summary>
        StringTransformCallback InteractiveCommandCallback { get; set; }
    }
}
