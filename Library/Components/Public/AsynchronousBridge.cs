/*
 * AsynchronousBridge.cs --
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
    [ObjectId("ca9cccc0-56a7-4656-a69b-2b50f1df2b1a")]
    public sealed class AsynchronousBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IAsynchronousCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private AsynchronousBridge(
            IAsynchronousCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
        public static AsynchronousBridge Create(
            IAsynchronousCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid asynchronous callback";
                return null;
            }

            return new AsynchronousBridge(callback);
        }
        #endregion
    }
}
