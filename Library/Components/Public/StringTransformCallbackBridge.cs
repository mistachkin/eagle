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
    /// <summary>
    /// This class wraps an <see cref="IStringTransformCallback" /> instance,
    /// exposing its string-transformation operation as a delegate-compatible
    /// method that may be invoked across application domain boundaries.
    /// </summary>
    [ObjectId("bcc26cf6-b704-437d-8d2a-a71fb0c3f0ff")]
    public sealed class StringTransformCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped callback object whose string-transformation operation
        /// is exposed by this bridge.
        /// </summary>
        private IStringTransformCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class that wraps the specified
        /// string-transformation callback object.
        /// </summary>
        /// <param name="callback">
        /// The callback object whose string-transformation operation is to be
        /// exposed by the newly created bridge.
        /// </param>
        private StringTransformCallbackBridge(
            IStringTransformCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method transforms the specified string value by forwarding it
        /// to the wrapped callback object.
        /// </summary>
        /// <param name="value">
        /// The string value to be transformed.
        /// </param>
        /// <returns>
        /// The transformed string value produced by the wrapped callback
        /// object.
        /// </returns>
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
        /// <summary>
        /// This method creates a new bridge that wraps the specified
        /// string-transformation callback object.
        /// </summary>
        /// <param name="callback">
        /// The callback object whose string-transformation operation is to be
        /// exposed by the newly created bridge.  This value may not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
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
