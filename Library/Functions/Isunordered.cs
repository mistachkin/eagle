/*
 * Isunordered.cs --
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
    [ObjectId("8f18b59c-eca3-4729-930a-d697c3e4c485")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.FloatTypes)]
    [ObjectGroup("indicator")]
    internal sealed class Isunordered : Arguments
    {
        #region Public Constructors
        public Isunordered(
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

            double doubleValue1 = 0.0;

            if (Value.GetDouble((IGetValue)arguments[1],
                    interpreter.InternalCultureInfo, ref doubleValue1,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            double doubleValue2 = 0.0;

            if (Value.GetDouble((IGetValue)arguments[2],
                    interpreter.InternalCultureInfo, ref doubleValue2,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                FloatingPointClass fpClass1 = MathOps.Classify(
                    doubleValue1);

                FloatingPointClass fpClass2 = MathOps.Classify(
                    doubleValue2);

                if ((fpClass1 == FloatingPointClass.NaN) ||
                    (fpClass2 == FloatingPointClass.NaN))
                {
                    value = true;
                }
                else
                {
                    value = false;
                }
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
