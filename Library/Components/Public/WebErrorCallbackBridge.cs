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
    /// <summary>
    /// This class provides a marshal-by-reference bridge that adapts an
    /// <see cref="IWebErrorCallback" /> implementation, allowing the web error
    /// handling callback to be invoked across application domain boundaries.
    /// </summary>
    [ObjectId("5da739ce-71b8-42f5-ba3a-9c4049e4d8e6")]
    public sealed class WebErrorCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped web error handling callback that this bridge forwards
        /// calls to.
        /// </summary>
        private IWebErrorCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified web error handling
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The web error handling callback to wrap.
        /// </param>
        private WebErrorCallbackBridge(
            IWebErrorCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a web error notification to the wrapped web
        /// error handling callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the web operation.  This parameter may
        /// be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the web operation.  This parameter
        /// may be null.
        /// </param>
        /// <param name="uri">
        /// The uniform resource identifier involved in the web operation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="webFlags">
        /// The flags controlling the web operation.
        /// </param>
        /// <param name="retries">
        /// The number of retries that have been attempted so far.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, for the web operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of retries permitted for the web operation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter will be modified to contain the result
        /// produced by the callback, if any.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this parameter will be modified to contain the list of
        /// accumulated error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method creates a new bridge that wraps the specified web error
        /// handling callback.
        /// </summary>
        /// <param name="callback">
        /// The web error handling callback to wrap.  This parameter may not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
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
