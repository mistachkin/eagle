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
    /// <summary>
    /// This class provides a marshal-by-reference bridge that adapts an
    /// <see cref="IUnknownCallback" /> implementation, allowing the unknown
    /// command handling callback to be invoked across application domain
    /// boundaries.
    /// </summary>
    [ObjectId("7523f2c8-c7ba-47a8-bffa-97f118ee622a")]
    public sealed class UnknownCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped unknown command handling callback that this bridge
        /// forwards calls to.
        /// </summary>
        private IUnknownCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified unknown command
        /// handling callback.
        /// </summary>
        /// <param name="callback">
        /// The unknown command handling callback to wrap.
        /// </param>
        private UnknownCallbackBridge(
            IUnknownCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards an unknown command lookup to the wrapped
        /// unknown command handling callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the lookup.  This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags in effect for the lookup.
        /// </param>
        /// <param name="name">
        /// The name of the unknown command being looked up.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the unknown command.  This parameter
        /// may be null.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags controlling how the lookup is performed.
        /// </param>
        /// <param name="ambiguous">
        /// Upon success, set to non-zero if the looked-up name was ambiguous.
        /// </param>
        /// <param name="execute">
        /// Upon success, set to the executable entity resolved for the unknown
        /// command, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method creates a new bridge that wraps the specified unknown
        /// command handling callback.
        /// </summary>
        /// <param name="callback">
        /// The unknown command handling callback to wrap.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
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
