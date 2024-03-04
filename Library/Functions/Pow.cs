/*
 * Pow.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("a16093f7-ded1-4fe5-9347-3c95a49de5eb")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.FloatTypes)]
    [ObjectGroup("power")]
    internal sealed class Pow : Arguments
    {
        #region Public Constructors
        public Pow(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            if (base.Execute(
                    interpreter, clientData, arguments, ref value,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            double[] doubleValue = { 0.0, 0.0 };

            if (Value.GetDouble((IGetValue)arguments[1],
                    interpreter.InternalCultureInfo, ref doubleValue[0],
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (Value.GetDouble((IGetValue)arguments[2],
                    interpreter.InternalCultureInfo, ref doubleValue[1],
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                value = Math.Pow(doubleValue[0], doubleValue[1]);
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format("caught math exception: {0}", e);

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
