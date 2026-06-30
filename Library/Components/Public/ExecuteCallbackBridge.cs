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
    /// <summary>
    /// This class bridges an Eagle execute callback across an application
    /// domain boundary, forwarding execution requests to a wrapped
    /// <see cref="IExecute" /> implementation.
    /// </summary>
    [ObjectId("1dca16ef-c448-478d-a255-fc03f54b7a13")]
    public sealed class ExecuteCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// Stores the execute callback wrapped by this bridge.
        /// </summary>
        private IExecute callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an execute callback bridge that wraps the specified
        /// execute callback.
        /// </summary>
        /// <param name="callback">
        /// The execute callback to wrap.
        /// </param>
        private ExecuteCallbackBridge(
            IExecute callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards an execution request to the wrapped execute
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the execution, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the execution, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments for the execution, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result of the execution, or an error
        /// message if no execute callback is available.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success; otherwise, an error return code (for
        /// example, ReturnCode.Error when there is no wrapped execute
        /// callback).
        /// </returns>
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
        /// <summary>
        /// This method creates a new execute callback bridge that wraps the
        /// specified execute callback.
        /// </summary>
        /// <param name="callback">
        /// The execute callback to wrap.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the bridge
        /// could not be created.
        /// </param>
        /// <returns>
        /// A new execute callback bridge wrapping the specified callback, or
        /// null if the callback is invalid.
        /// </returns>
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
