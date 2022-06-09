/*
 * Min.cs --
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
    [ObjectId("04716a06-d00c-433a-914b-8eb4769ad74c")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.None)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("aggregate")]
    internal sealed class Min : Core
    {
        public Min(
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
                    if (arguments.Count >= 2)
                    {
                        Variant variant1 = null;

                        code = Value.GetVariant(interpreter,
                            (IGetValue)arguments[1], ValueFlags.AnyVariant,
                            interpreter.InternalCultureInfo, ref variant1, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                            {
                                Variant variant2 = null;

                                code = Value.GetVariant(interpreter,
                                    (IGetValue)arguments[argumentIndex], ValueFlags.AnyVariant,
                                    interpreter.InternalCultureInfo, ref variant2, ref error);

                                if (code != ReturnCode.Ok)
                                    break;

                                code = Value.FixupVariants(
                                    this, variant1, variant2, null, null, false, false,
                                    ref error);

                                if (code != ReturnCode.Ok)
                                    break;

                                try
                                {
                                    if (variant1.IsDouble())
                                    {
                                        variant1.Value = Interpreter.FixIntermediatePrecision(
                                            Math.Min((double)variant1.Value, (double)variant2.Value));
                                    }
                                    else if (variant1.IsDecimal())
                                    {
                                        variant1.Value = Interpreter.FixIntermediatePrecision(
                                            Math.Min((decimal)variant1.Value, (decimal)variant2.Value));
                                    }
                                    else if (variant1.IsWideInteger())
                                    {
                                        variant1.Value = Math.Min(
                                            (long)variant1.Value, (long)variant2.Value);
                                    }
                                    else if (variant1.IsInteger())
                                    {
                                        variant1.Value = Math.Min(
                                            (int)variant1.Value, (int)variant2.Value);
                                    }
                                    else if (variant1.IsBoolean())
                                    {
                                        variant1.Value = Math.Min(
                                            ConversionOps.ToInt((bool)variant1.Value),
                                            ConversionOps.ToInt((bool)variant2.Value));
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
                                    break;
                                }
                            }

                            if (code == ReturnCode.Ok)
                            {
                                value = Argument.GetOrCreate(
                                    interpreter, variant1,
                                    interpreter.HasNoCacheArgument());
                            }
                        }
                    }
                    else
                    {
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
