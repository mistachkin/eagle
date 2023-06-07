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
    [ObjectId("e0c4e7d4-7ac4-457f-b73a-9d5837b0c84b")]
    public sealed class EventCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IEventCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private EventCallbackBridge(
            IEventCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
