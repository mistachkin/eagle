/*
 * StringTransformCallbackBridge.cs --
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

namespace Eagle._Components.Public
{
    [ObjectId("bcc26cf6-b704-437d-8d2a-a71fb0c3f0ff")]
    public sealed class StringTransformCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IStringTransformCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private StringTransformCallbackBridge(
            IStringTransformCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public string StringTransformCallback(
            string value
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            return callback.StringTransform(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static StringTransformCallbackBridge Create(
            IStringTransformCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid string transform callback";
                return null;
            }

            return new StringTransformCallbackBridge(callback);
        }
        #endregion
    }
}
