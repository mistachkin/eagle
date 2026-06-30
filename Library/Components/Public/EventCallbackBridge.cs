/*
 * EventCallbackBridge.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class bridges an Eagle event callback across an application domain
    /// boundary, forwarding event notifications to a wrapped
    /// <see cref="IEventCallback" /> implementation.
    /// </summary>
    [ObjectId("e0c4e7d4-7ac4-457f-b73a-9d5837b0c84b")]
    public sealed class EventCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// Stores the event callback wrapped by this bridge.
        /// </summary>
        private IEventCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an event callback bridge that wraps the specified event
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The event callback to wrap.
        /// </param>
        private EventCallbackBridge(
            IEventCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards an event notification to the wrapped event
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the event, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the event, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result of the event callback, or an error
        /// message if no event callback is available.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code (for
        /// example, ReturnCode.Error when there is no wrapped event callback).
        /// </returns>
        public ReturnCode EventCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (callback == null)
            {
                result = "invalid event callback";
                return ReturnCode.Error;
            }

            return callback.Event(
                interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new event callback bridge that wraps the
        /// specified event callback.
        /// </summary>
        /// <param name="callback">
        /// The event callback to wrap.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the bridge
        /// could not be created.
        /// </param>
        /// <returns>
        /// A new event callback bridge wrapping the specified callback, or null
        /// if the callback is invalid.
        /// </returns>
        public static EventCallbackBridge Create(
            IEventCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid event callback";
                return null;
            }

            return new EventCallbackBridge(callback);
        }
        #endregion
    }
}
