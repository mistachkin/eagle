/*
 * InteractiveLoopCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("e4612dcc-6531-4155-be07-d0ef0571a4e7")]
    public interface IInteractiveLoopCallback
    {
        ReturnCode InteractiveLoop(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            IInteractiveLoopData loopData,
            ref Result result
            );
    }
}
