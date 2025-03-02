/*
 * WebErrorCallbackBridge.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Interfaces.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Public
{
    [ObjectId("5da739ce-71b8-42f5-ba3a-9c4049e4d8e6")]
    public sealed class WebErrorCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IWebErrorCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private WebErrorCallbackBridge(
            IWebErrorCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode WebErrorCallback(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            WebFlags webFlags,
            int retries,
            int? timeout,
            int? maximumRetries,
            ref object result,
            ref ResultList errors
            )
        {
            if (callback == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid web error callback");
                return ReturnCode.Error;
            }

            return callback.WebError(
                interpreter, clientData, uri, webFlags, retries,
                timeout, maximumRetries, ref result, ref errors);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static WebErrorCallbackBridge Create(
            IWebErrorCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid web error callback";
                return null;
            }

            return new WebErrorCallbackBridge(callback);
        }
        #endregion
    }
}
