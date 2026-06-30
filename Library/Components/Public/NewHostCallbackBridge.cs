/*
 * NewHostCallbackBridge.cs --
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
    /// <see cref="INewHostCallback" /> implementation, allowing the new host
    /// creation callback to be invoked across application domain boundaries.
    /// </summary>
    [ObjectId("1538f714-6a43-4261-87b1-14111fca3a27")]
    public sealed class NewHostCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped new host creation callback that this bridge forwards
        /// calls to.
        /// </summary>
        private INewHostCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified new host creation
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The new host creation callback to wrap.
        /// </param>
        private NewHostCallbackBridge(
            INewHostCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a new host creation request to the wrapped new
        /// host creation callback.
        /// </summary>
        /// <param name="hostData">
        /// The data used to create the new host.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created host, or null if there is no wrapped callback.
        /// </returns>
        public IHost NewHostCallback(
            IHostData hostData
            )
        {
            if (callback == null)
                return null;

            return callback.NewHost(hostData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified new host
        /// creation callback.
        /// </summary>
        /// <param name="callback">
        /// The new host creation callback to wrap.  This parameter may not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
        public static NewHostCallbackBridge Create(
            INewHostCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid new host callback";
                return null;
            }

            return new NewHostCallbackBridge(callback);
        }
        #endregion
    }
}
