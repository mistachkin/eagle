/*
 * ExecuteCallbackBridge.cs --
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
    [ObjectId("1dca16ef-c448-478d-a255-fc03f54b7a13")]
    public sealed class ExecuteCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private IExecute callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ExecuteCallbackBridge(
            IExecute callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode ExecuteCallback(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (callback == null)
            {
                result = "invalid execute callback";
                return ReturnCode.Error;
            }

            return callback.Execute(
                interpreter, clientData, arguments, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ExecuteCallbackBridge Create(
            IExecute callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid execute callback";
                return null;
            }

            return new ExecuteCallbackBridge(callback);
        }
        #endregion
    }
}
