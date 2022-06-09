/*
 * Wide.cs --
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
    [ObjectId("150aae30-2234-411b-8cac-a13c942aeee9")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Wide : Core
    {
        public Wide(
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
                                if (variant1.IsDateTime())
                                {
                                    value = ConversionOps.ToLong((DateTime)variant1.Value);
                                }
                                else if (variant1.IsDouble())
                                {
                                    variant1.Value = Math.Truncate((double)variant1.Value);

                                    if (variant1.ConvertTo(typeof(long)))
                                    {
                                        value = (long)variant1.Value;
                                    }
                                    else
                                    {
                                        error = "wide integer value too large to represent";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else if (variant1.IsDecimal())
                                {
                                    variant1.Value = Math.Truncate((decimal)variant1.Value);

                                    if (variant1.ConvertTo(typeof(long)))
                                    {
                                        value = (long)variant1.Value;
                                    }
                                    else
                                    {
                                        error = "wide integer value too large to represent";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else if (variant1.IsWideInteger())
                                {
                                    value = (long)variant1.Value; /* NOP */
                                }
                                else if (variant1.IsInteger())
                                {
                                    value = ConversionOps.ToLong((int)variant1.Value);
                                }
                                else if (variant1.IsBoolean())
                                {
                                    value = ConversionOps.ToLong((bool)variant1.Value);
                                }
                                else
                                {
                                    error = String.Format(
                                        "expected wide integer but got {0}",
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
