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
    /// <summary>
    /// This class provides a marshal-by-reference bridge that adapts an
    /// <see cref="IWebTransferCallback" /> implementation, allowing the web
    /// transfer handling callback to be invoked across application domain
    /// boundaries.
    /// </summary>
    [ObjectId("038f366f-b8ad-4b7a-8c09-8e0ad1da4edd")]
    public sealed class WebTransferCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped web transfer handling callback that this bridge forwards
        /// calls to.
        /// </summary>
        private IWebTransferCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified web transfer handling
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The web transfer handling callback to wrap.
        /// </param>
        private WebTransferCallbackBridge(
            IWebTransferCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a web transfer notification to the wrapped web
        /// transfer handling callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the web transfer.  This parameter may be
        /// null.
        /// </param>
        /// <param name="webFlags">
        /// The flags controlling the web transfer.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the web transfer.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode WebTransferCallback(
            Interpreter interpreter,
            WebFlags webFlags,
            IClientData clientData,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid web transfer callback";
                return ReturnCode.Error;
            }

            return callback.WebTransfer(
                interpreter, webFlags, clientData,
                ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified web
        /// transfer handling callback.
        /// </summary>
        /// <param name="callback">
        /// The web transfer handling callback to wrap.  This parameter may not
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
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
