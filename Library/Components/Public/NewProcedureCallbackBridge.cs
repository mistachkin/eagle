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
    [ObjectId("e9f5ccf5-8ca0-46d3-8375-83586ba26f7d")]
    public sealed class NewProcedureCallbackBridge : ScriptMarshalByRefObject
    {
        #region Private Data
        private INewProcedureCallback callback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private NewProcedureCallbackBridge(
            INewProcedureCallback callback
            )
        {
            this.callback = callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
