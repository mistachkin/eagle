/*
 * NewWebClientCallbackBridge.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Net;
using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("06776059-2bbb-487d-8fa7-5460b69b059e")]
    public sealed class NewWebClientCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private INewWebClientCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private NewWebClientCallbackBridge(
            INewWebClientCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public WebClient NewWebClientCallback(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid new web client callback";
                return null;
            }

            return callback.NewWebClient(
                interpreter, argument, clientData, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static NewWebClientCallbackBridge Create(
            INewWebClientCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid new web client callback";
                return null;
            }

            return new NewWebClientCallbackBridge(callback);
        }
        #endregion
    }
}
