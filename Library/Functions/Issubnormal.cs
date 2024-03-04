/*
 * Issubnormal.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("d3dd868f-c9c8-42eb-b258-191c7b2efa94")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.FloatTypes)]
    [ObjectGroup("indicator")]
    internal sealed class Issubnormal : Arguments
    {
        #region Public Constructors
        public Issubnormal(
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

            double doubleValue = 0.0;

            if (Value.GetDouble((IGetValue)arguments[1],
                    interpreter.InternalCultureInfo, ref doubleValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                FloatingPointClass fpClass = MathOps.Classify(
                    doubleValue);

                value = (fpClass == FloatingPointClass.SubNormal);
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
