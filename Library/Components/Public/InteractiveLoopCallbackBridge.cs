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
    [ObjectId("38b540e6-2fb7-4c3c-a468-8b19fddb5ef1")]
    public sealed class InteractiveLoopCallbackBridge :
        ScriptMarshalByRefObject
    {
        #region Private Data
        private IInteractiveLoopCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private InteractiveLoopCallbackBridge(
            IInteractiveLoopCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
