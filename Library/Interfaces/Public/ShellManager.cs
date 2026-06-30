/*
 * ShellManager.cs --
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
    /// This interface is implemented by entities that expose the set of
    /// callbacks used by the interactive shell to preview, handle unknown,
    /// and evaluate its command-line arguments.
    /// </summary>
    [ObjectId("0b941a5b-cf8c-40ae-850a-59249899cd37")]
    public interface IShellManager
    {
        /// <summary>
        /// Gets or sets the callback used to preview a command-line argument
        /// before it is otherwise processed.  This value may be null.
        /// </summary>
        PreviewArgumentCallback PreviewArgumentCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback used to handle a command-line argument
        /// that cannot otherwise be handled.  This value may be null.
        /// </summary>
        UnknownArgumentCallback UnknownArgumentCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback used to evaluate a script.  This value
        /// may be null.
        /// </summary>
        EvaluateScriptCallback EvaluateScriptCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback used to evaluate a script contained in a
        /// file.  This value may be null.
        /// </summary>
        EvaluateFileCallback EvaluateFileCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback used to evaluate a script contained in a
        /// file using a specific character encoding.  This value may be null.
        /// </summary>
        EvaluateEncodedFileCallback EvaluateEncodedFileCallback { get; set; }
    }
}
