/*
 * TimeSpan.cs --
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
    [ObjectId("022f38b7-f63c-439d-8329-7c1825e4c4ad")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("conversion")]
    internal sealed class _TimeSpan : Core
    {
        public _TimeSpan(
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
                                if (variant1.IsTimeSpan())
                                {
                                    value = (TimeSpan)variant1.Value; /* NOP */
                                }
                                else if (variant1.IsDouble())
                                {
                                    value = ConversionOps.ToTimeSpan((double)variant1.Value); /* SAFE */
                                }
                                else if (variant1.IsDecimal())
                                {
                                    if (variant1.ConvertTo(typeof(TimeSpan)))
                                    {
                                        value = (TimeSpan)variant1.Value;
                                    }
                                    else
                                    {
                                        error = "time-span value too large to represent";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else if (variant1.IsWideInteger())
                                {
                                    value = ConversionOps.ToTimeSpan((long)variant1.Value);
                                }
                                else if (variant1.IsInteger())
                                {
                                    value = ConversionOps.ToTimeSpan((int)variant1.Value);
                                }
                                else if (variant1.IsBoolean())
                                {
                                    value = ConversionOps.ToTimeSpan((bool)variant1.Value);
                                }
                                else
                                {
                                    error = String.Format(
                                        "unable to convert time-span string {0}",
                                        FormatOps.WrapOrNull(arguments[1]));

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
