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
    internal sealed class Log2 : Arguments
    {
        #region Public Constructors
        public Log2(
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

            IVariant variant1 = null;

            if (Value.GetVariant(interpreter,
                    (IGetValue)arguments[1], ValueFlags.AnyVariant,
                    interpreter.InternalCultureInfo, ref variant1,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                if (variant1.IsDouble())
                {
                    value = Math.Log((double)variant1.Value, 2);
                }
                else if (variant1.IsDecimal())
                {
                    if (variant1.ConvertTo(TypeCode.Double))
                    {
                        value = Math.Log((double)variant1.Value, 2);
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert decimal {0} to double",
                            FormatOps.WrapOrNull(arguments[1]));

                        return ReturnCode.Error;
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
                        "unsupported argument type for function {0}",
                        FormatOps.WrapOrNull(base.Name));

                    return ReturnCode.Error;
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
