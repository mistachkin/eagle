/*
 * EventCallback.cs --
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
    /// This interface is implemented by entities that can be invoked as the
    /// handler for an asynchronous interpreter event.  It defines the single
    /// entry point, <see cref="Event" />, that the engine calls when the
    /// associated event is processed.
    /// </summary>
    [ObjectId("b7e3be5e-440c-4486-9b18-0a9c45c6a4d3")]
    public interface IEventCallback
    {
        /// <summary>
        /// This method is called by the engine to process the event for this
        /// entity.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this event is being processed in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the event was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by
        /// processing the event.  Upon failure, this must contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Event(
            Interpreter interpreter, // TODO: Change to use the IInterpreter type.
            IClientData clientData,
            ref Result result
        );
    }
}
