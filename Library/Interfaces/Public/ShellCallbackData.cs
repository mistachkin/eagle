/*
 * ShellCallbackData.cs --
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
    /// This interface is implemented by entities that hold the set of
    /// callbacks used by the interactive shell, together with the associated
    /// processing options.  It also supports saving and restoring any
    /// pre-existing callbacks.
    /// </summary>
    [ObjectId("790e4880-1893-4de4-b8e4-f0b59ebcdc0b")]
    public interface IShellCallbackData :
        IIdentifier, IShellManager
#if DEBUGGER
        , IInteractiveLoopManager
#endif
    {
        /// <summary>
        /// Gets or sets a value indicating whether shell argument processing
        /// should be simulated only, without performing the underlying
        /// actions.
        /// </summary>
        bool WhatIf { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether shell argument processing
        /// should stop upon encountering an unknown argument.
        /// </summary>
        bool StopOnUnknown { get; set; }

        /// <summary>
        /// Checks for any pre-existing shell callbacks and records them so
        /// that they may be restored later.
        /// </summary>
        void CheckForPreExisting();

        /// <summary>
        /// Sets the new shell callbacks to use or, optionally, restores the
        /// pre-existing ones.
        /// </summary>
        /// <param name="previewArgumentCallback">
        /// The callback used to preview a command-line argument, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="unknownArgumentCallback">
        /// The callback used to handle an unknown command-line argument, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="evaluateScriptCallback">
        /// The callback used to evaluate a script, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="evaluateFileCallback">
        /// The callback used to evaluate a script contained in a file, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="evaluateEncodedFileCallback">
        /// The callback used to evaluate a script contained in a file using a
        /// specific character encoding, if any.  This parameter may be null.
        /// </param>
        /// <param name="interactiveLoopCallback">
        /// The callback used to run the interactive loop, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="resetPreExisting">
        /// If true, the pre-existing callbacks are restored instead of
        /// setting the supplied ones.
        /// </param>
        void SetNewOrResetPreExisting(
            PreviewArgumentCallback previewArgumentCallback,
            UnknownArgumentCallback unknownArgumentCallback,
            EvaluateScriptCallback evaluateScriptCallback,
            EvaluateFileCallback evaluateFileCallback,
            EvaluateEncodedFileCallback evaluateEncodedFileCallback,
#if DEBUGGER
            InteractiveLoopCallback interactiveLoopCallback,
#endif
            bool resetPreExisting
        );
    }
}
