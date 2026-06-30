/*
 * InteractiveLoopCallbackBridge.cs --
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
    /// This class provides a bridge that adapts an
    /// <see cref="IInteractiveLoopCallback" /> so that it may be invoked
    /// across an application domain boundary.  It derives from
    /// <see cref="ScriptMarshalByRefObject" /> and forwards interactive loop
    /// callbacks to the wrapped callback instance.
    /// </summary>
    [ObjectId("38b540e6-2fb7-4c3c-a468-8b19fddb5ef1")]
    public sealed class InteractiveLoopCallbackBridge :
        ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The interactive loop callback wrapped by this bridge.
        /// </summary>
        private IInteractiveLoopCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified interactive loop
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The interactive loop callback to wrap.
        /// </param>
        private InteractiveLoopCallbackBridge(
            IInteractiveLoopCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards an interactive loop callback to the wrapped
        /// callback instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the interactive loop.
        /// </param>
        /// <param name="loopData">
        /// The data describing the state of the interactive loop.
        /// </param>
        /// <param name="result">
        /// Upon success, the result produced by the wrapped callback.  Upon
        /// failure, an error message describing why the callback could not be
        /// invoked.
        /// </param>
        /// <returns>
        /// The return code produced by the wrapped callback, or
        /// <see cref="ReturnCode.Error" /> if there is no wrapped callback.
        /// </returns>
        public ReturnCode InteractiveLoopCallback(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
            if (callback == null)
            {
                result = "invalid interactive loop callback";
                return ReturnCode.Error;
            }

            return callback.InteractiveLoop(interpreter, loopData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified
        /// interactive loop callback.
        /// </summary>
        /// <param name="callback">
        /// The interactive loop callback to wrap.
        /// </param>
        /// <param name="error">
        /// Upon failure, an error message describing why the bridge could not
        /// be created.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
        public static InteractiveLoopCallbackBridge Create(
            IInteractiveLoopCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid interactive loop callback";
                return null;
            }

            return new InteractiveLoopCallbackBridge(callback);
        }
        #endregion
    }
}
