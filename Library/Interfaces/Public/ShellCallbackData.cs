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
    [ObjectId("790e4880-1893-4de4-b8e4-f0b59ebcdc0b")]
    public interface IShellCallbackData :
        IIdentifier, IShellManager
#if DEBUGGER
        , IInteractiveLoopManager
#endif
    {
        bool WhatIf { get; set; }
        bool StopOnUnknown { get; set; }

        void CheckForPreExisting();

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
