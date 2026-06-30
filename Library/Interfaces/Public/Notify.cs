/*
 * Notify.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by an entity that wishes to receive
    /// notifications about events occurring within an interpreter.  It allows
    /// the entity to declare which notification types and flags it is
    /// interested in and to handle each notification as it is delivered.
    /// </summary>
    [ObjectId("41636597-8a41-4601-9328-ff2df87d8f71")]
    public interface INotify
    {
        /// <summary>
        /// Returns the notification types that this entity is interested in
        /// receiving.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the query is being made for.  This
        /// parameter should not be null.
        /// </param>
        /// <returns>
        /// The notification types handled by this entity.
        /// </returns>
        //
        // TODO: Change these to use the IInterpreter type.
        //
        [Throw(true)]
        NotifyType GetTypes(Interpreter interpreter);

        /// <summary>
        /// Returns the notification flags that control how and when this
        /// entity is notified.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the query is being made for.  This
        /// parameter should not be null.
        /// </param>
        /// <returns>
        /// The notification flags handled by this entity.
        /// </returns>
        [Throw(true)]
        NotifyFlags GetFlags(Interpreter interpreter);

        /// <summary>
        /// This method is called by the engine to deliver a notification to
        /// this entity.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the notification originates from.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="eventArgs">
        /// The event arguments describing the notification being delivered.
        /// This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the entity was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the notification, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value produced while
        /// handling the notification.  Upon failure, this must contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode Notify(Interpreter interpreter, IScriptEventArgs eventArgs,
            IClientData clientData, ArgumentList arguments, ref Result result);
    }
}
