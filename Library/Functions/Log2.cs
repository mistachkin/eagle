/*
 * Log2.cs --
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
    [ObjectId("495e138a-6c52-46e6-84dd-ed72514f0b50")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logarithmic")]
    internal sealed class Log2 : Core
    {
        public Log2(
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
                        Variant variant1 = null;

                        code = Value.GetVariant(interpreter,
                            (IGetValue)arguments[1], ValueFlags.AnyVariant,
                            interpreter.InternalCultureInfo, ref variant1, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            try
                            {
                                if (variant1.IsDouble())
                                {
                                    value = Math.Log((double)variant1.Value, 2);
                                }
                                else if (variant1.IsDecimal())
                                {
                                    if (variant1.ConvertTo(typeof(double)))
                                    {
                                        value = Math.Log((double)variant1.Value, 2);
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "could not convert decimal {0} to double",
                                            FormatOps.WrapOrNull(arguments[1]));

                                        code = ReturnCode.Error;
                                    }
                                }
                                else if (variant1.IsWideInteger())
                                {
                                    value = MathOps.Log2((long)variant1.Value);
                                }
                                else if (variant1.IsInteger())
                                {
                                    value = MathOps.Log2((int)variant1.Value);
                                }
                                else if (variant1.IsBoolean())
                                {
                                    value = MathOps.Log2(
                                        ConversionOps.ToInt((bool)variant1.Value));
                                }
                                else
                                {
                                    error = String.Format(
                                        "unsupported variant type for function {0}",
                                        FormatOps.WrapOrNull(base.Name));

                                    code = ReturnCode.Error;
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
