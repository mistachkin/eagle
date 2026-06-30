/*
 * AsynchronousCallbackBridge.cs --
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
    /// This class bridges an asynchronous callback across an application
    /// domain boundary, forwarding asynchronous completion notifications to
    /// the wrapped <see cref="IAsynchronousCallback" /> instance.
    /// </summary>
    [ObjectId("ca9cccc0-56a7-4656-a69b-2b50f1df2b1a")]
    public sealed class AsynchronousCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The asynchronous callback wrapped by this bridge and invoked when
        /// asynchronous completion is signaled.
        /// </summary>
        private IAsynchronousCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that forwards asynchronous callbacks to the
        /// specified callback instance.
        /// </summary>
        /// <param name="callback">
        /// The asynchronous callback to wrap and invoke.
        /// </param>
        private AsynchronousCallbackBridge(
            IAsynchronousCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards an asynchronous completion notification to the
        /// wrapped callback, if any.
        /// </summary>
        /// <param name="context">
        /// The context describing the asynchronous operation that completed.
        /// </param>
        public void AsynchronousCallback(
            IAsynchronousContext context
            )
        {
            if (callback != null)
                callback.Invoke(context);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified
        /// asynchronous callback.
        /// </summary>
        /// <param name="callback">
        /// The asynchronous callback to wrap.  This parameter may not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge instance, or null if it could not be
        /// created.
        /// </returns>
        public static AsynchronousCallbackBridge Create(
            IAsynchronousCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid asynchronous callback";
                return null;
            }

            return new AsynchronousCallbackBridge(callback);
        }
        #endregion
    }
}
