/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("91de9380-c952-4fef-8839-4bde3f6f16a4")]
    [FunctionFlags(FunctionFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default, IExecute
    {
        #region Public Constructors
        //
        // NOTE: In the future, behavior specific to functions in the core will
        //       be implemented here rather than in _Functions.Default (which
        //       is available to external functions to derive from).  For now,
        //       the primary job of this class is to set the cached function
        //       flags correctly for all functions in the core function set.
        //
        public Core(
            IFunctionData functionData
            )
            : base(functionData)
        {
            this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                AttributeOps.GetFunctionFlags(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code;
            Argument value = null;
            Result error = null;

            code = Execute(
                interpreter, clientData, arguments, ref value, ref error);

            if (code == ReturnCode.Ok)
                result = value;
            else
                result = error;

            return code;
        }
        #endregion
    }
}
