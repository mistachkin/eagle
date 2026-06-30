/*
 * CallbackQueueManager.cs --
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
    /// This interface is implemented by entities that maintain a queue of
    /// pending callbacks.  It provides methods to check for the presence of
    /// the callback queue and to manage its contents by clearing, enqueuing,
    /// and dequeuing <see cref="ICallback" /> instances.
    /// </summary>
    [ObjectId("a3a33f2f-c360-445b-bee6-02547eed0a0d")]
    public interface ICallbackQueueManager
    {
        ///////////////////////////////////////////////////////////////////////
        // CALLBACK QUEUE CHECKING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the callback queue is present.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the callback queue is present; otherwise, false.
        /// </returns>
        bool HasCallbackQueue(
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // CALLBACK QUEUE MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all pending callbacks from the callback queue.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ClearCallbackQueue(
            ref Result error
            );

        /// <summary>
        /// Adds a callback to the end of the callback queue.
        /// </summary>
        /// <param name="callback">
        /// The callback to add to the queue.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EnqueueCallback(
            ICallback callback,
            ref Result error
            );

        /// <summary>
        /// Removes a callback from the front of the callback queue.
        /// </summary>
        /// <param name="callback">
        /// Upon success, receives the callback removed from the queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode DequeueCallback(
            ref ICallback callback,
            ref Result error
            );
    }
}
