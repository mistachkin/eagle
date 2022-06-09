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
    internal sealed class Isunordered : Core
    {
        public Isunordered(
            IFunctionData functionData
            )
            : base(functionData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == (this.Arguments + 1))
                    {
                        double doubleValue1 = 0.0;

                        code = Value.GetDouble(
                            (IGetValue)arguments[1], interpreter.InternalCultureInfo,
                            ref doubleValue1, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            double doubleValue2 = 0.0;

                            code = Value.GetDouble(
                                (IGetValue)arguments[2], interpreter.InternalCultureInfo,
                                ref doubleValue2, ref error);

                            if (code == ReturnCode.Ok)
                            {
                                try
                                {
                                    if ((MathOps.Classify(doubleValue1) == FloatingPointClass.NaN) ||
                                        (MathOps.Classify(doubleValue2) == FloatingPointClass.NaN))
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

                                    error = String.Format(
                                        "caught math exception: {0}",
                                        e);

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (arguments.Count > (this.Arguments + 1))
                            error = String.Format(
                                "too many arguments for math function {0}",
                                FormatOps.WrapOrNull(base.Name));
                        else
                            error = String.Format(
                                "too few arguments for math function {0}",
                                FormatOps.WrapOrNull(base.Name));

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
