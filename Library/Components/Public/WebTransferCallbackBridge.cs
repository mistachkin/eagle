/*
 * WebTransferCallbackBridge.cs --
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
    [ObjectId("038f366f-b8ad-4b7a-8c09-8e0ad1da4edd")]
    public sealed class WebTransferCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IWebTransferCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private WebTransferCallbackBridge(
            IWebTransferCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode WebTransferCallback(
            Interpreter interpreter,
            WebFlags flags,
            IClientData clientData,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid new web client callback";
                return ReturnCode.Error;
            }

            return callback.WebTransfer(
                interpreter, flags, clientData, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static WebTransferCallbackBridge Create(
            IWebTransferCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid web transfer callback";
                return null;
            }

            return new WebTransferCallbackBridge(callback);
        }
        #endregion
    }
}
