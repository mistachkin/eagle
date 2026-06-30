/*
 * NewProcedureCallbackBridge.cs --
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
    /// This class provides a marshal-by-reference bridge that adapts an
    /// <see cref="INewProcedureCallback" /> implementation, allowing the new
    /// procedure creation callback to be invoked across application domain
    /// boundaries.
    /// </summary>
    [ObjectId("e9f5ccf5-8ca0-46d3-8375-83586ba26f7d")]
    public sealed class NewProcedureCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        /// <summary>
        /// The wrapped new procedure creation callback that this bridge
        /// forwards calls to.
        /// </summary>
        private INewProcedureCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a bridge that wraps the specified new procedure creation
        /// callback.
        /// </summary>
        /// <param name="callback">
        /// The new procedure creation callback to wrap.
        /// </param>
        private NewProcedureCallbackBridge(
            INewProcedureCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method forwards a new procedure creation request to the wrapped
        /// new procedure creation callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the new procedure.  This parameter may
        /// be null.
        /// </param>
        /// <param name="procedureData">
        /// The data used to create the new procedure.  This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created procedure, or null if there is no wrapped
        /// callback.
        /// </returns>
        public IProcedure NewProcedureCallback(
            Interpreter interpreter,
            IProcedureData procedureData,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid new procedure callback";
                return null;
            }

            return callback.NewProcedure(
                interpreter, procedureData, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new bridge that wraps the specified new
        /// procedure creation callback.
        /// </summary>
        /// <param name="callback">
        /// The new procedure creation callback to wrap.  This parameter may not
        /// be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created bridge, or null if it could not be created.
        /// </returns>
        public static NewProcedureCallbackBridge Create(
            INewProcedureCallback callback,
            ref Result error
            )
        {
            if (callback == null)
            {
                error = "invalid new procedure callback";
                return null;
            }

            return new NewProcedureCallbackBridge(callback);
        }
        #endregion
    }
}
