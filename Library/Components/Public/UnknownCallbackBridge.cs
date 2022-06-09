/*
 * UnknownCallbackBridge.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("7523f2c8-c7ba-47a8-bffa-97f118ee622a")]
    public sealed class UnknownCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IUnknownCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private UnknownCallbackBridge(
            IUnknownCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode Unknown(
            Interpreter interpreter,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid unknown callback";
                return ReturnCode.Error;
            }

            return callback.Unknown(
                interpreter, engineFlags, name, arguments, lookupFlags,
                ref ambiguous, ref execute, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static UnknownCallbackBridge Create(
            IUnknownCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid unknown callback";
                return null;
            }

            return new UnknownCallbackBridge(callback);
        }
        #endregion
    }
}
