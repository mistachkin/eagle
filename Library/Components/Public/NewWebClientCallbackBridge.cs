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
    /// <summary>
    /// This class provides a marshal-by-reference bridge that adapts an
    /// <see cref="INewWebClientCallback" /> implementation, allowing the new
    /// web client creation callback to be invoked across application domain
    /// boundaries.
    /// </summary>
    [ObjectId("06776059-2bbb-487d-8fa7-5460b69b059e")]
    public sealed class NewWebClientCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped new web client creation callback that this bridge
        /// forwards calls to.
        /// </summary>
        private INewWebClientCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified new web client creation
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The new web client creation callback to wrap.
        /// </param>
        private NewWebClientCallbackBridge(
            INewWebClientCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a new web client creation request to the
        /// wrapped new web client creation callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the new web client.  This parameter may
        /// be null.
        /// </param>
        /// <param name="argument">
        /// The argument associated with the new web client.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the new web client.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created web client, or null if there is no wrapped
        /// callback.
        /// </returns>
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
        /// <summary>
        /// This method creates a new bridge that wraps the specified new web
        /// client creation callback.
        /// </summary>
        /// <param name="callback">
        /// The new web client creation callback to wrap.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
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
