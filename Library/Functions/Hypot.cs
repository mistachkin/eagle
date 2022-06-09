/*
 * Hypot.cs --
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
    [ObjectId("c05488d0-87d2-4d01-855b-ccd83b613b8f")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.FloatTypes)]
    [ObjectGroup("trigonometric")]
    internal sealed class Hypot : Core
    {
        public Hypot(
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
                        double[] doubleValue = { 0.0, 0.0 };

                        code = Value.GetDouble(
                            (IGetValue)arguments[1], interpreter.InternalCultureInfo,
                            ref doubleValue[0], ref error);

                        if (code == ReturnCode.Ok)
                        {
                            code = Value.GetDouble(
                                (IGetValue)arguments[2], interpreter.InternalCultureInfo,
                                ref doubleValue[1], ref error);
                        }

                        if (code == ReturnCode.Ok)
                        {
                            try
                            {
                                value = Interpreter.FixIntermediatePrecision(
                                    Math.Sqrt(doubleValue[0] * doubleValue[0] +
                                        doubleValue[1] * doubleValue[1]));
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