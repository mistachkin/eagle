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
    [ObjectId("1538f714-6a43-4261-87b1-14111fca3a27")]
    public sealed class NewHostCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private INewHostCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private NewHostCallbackBridge(
            INewHostCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
