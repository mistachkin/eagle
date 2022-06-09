/*
 * Round3.cs --
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
    [ObjectId("0d9f9d0e-e809-46bb-948d-e702b994566a")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Ternary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("rounding")]
    internal sealed class Round3 : Core
    {
        public Round3(
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

                        int intValue = 0;

                        if (code == ReturnCode.Ok)
                        {
                            code = Value.GetInteger2(
                                (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                interpreter.InternalCultureInfo, ref intValue, ref error);
                        }

                        MidpointRounding rounding = MidpointRounding.ToEven; // TODO: Good default?

                        if (code == ReturnCode.Ok)
                        {
                            object enumValue = EnumOps.TryParse(
                                typeof(MidpointRounding), arguments[3], true,
                                true, ref error);

                            if (enumValue is MidpointRounding)
                                rounding = (MidpointRounding)enumValue;
                            else
                                code = ReturnCode.Error;
                        }

                        if (code == ReturnCode.Ok)
                        {
                            try
                            {
                                if (variant1.IsDouble())
                                {
                                    //
                                    // NOTE: No FixPrecision, Already rounding.
                                    //
                                    value = Math.Round((double)variant1.Value, intValue, rounding);
                                }
                                else if (variant1.IsDecimal())
                                {
                                    //
                                    // NOTE: No FixPrecision, Already rounding.
                                    //
                                    value = Math.Round((decimal)variant1.Value, intValue, rounding);
                                }
                                else if (variant1.IsWideInteger())
                                {
                                    value = ((long)variant1.Value);
                                }
                                else if (variant1.IsInteger())
                                {
                                    value = ((int)variant1.Value);
                                }
                                else if (variant1.IsBoolean())
                                {
                                    value = ((bool)variant1.Value);
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