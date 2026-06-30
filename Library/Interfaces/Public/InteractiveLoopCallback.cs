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
    /// <summary>
    /// This interface is implemented by an entity that can supply a custom
    /// implementation of the interactive loop, allowing an embedding
    /// application to replace the default read-evaluate-print behavior.
    /// </summary>
    [ObjectId("e4612dcc-6531-4155-be07-d0ef0571a4e7")]
    public interface IInteractiveLoopCallback
    {
        /// <summary>
        /// This method is called to run the interactive loop for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the interactive loop is running in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="loopData">
        /// The data that controls how the interactive loop behaves.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the final result of the loop.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode InteractiveLoop(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            IInteractiveLoopData loopData,
            ref Result result
            );
    }
}
