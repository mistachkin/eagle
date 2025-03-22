/*
 * Logx.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NET_40
using System.Numerics;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("1ef4b046-c392-4919-a240-1e085b8efe2d")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logarithmic")]
    internal sealed class Logx : Arguments
    {
        #region Public Constructors
        public Logx(
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

            IVariant variant2 = null;

            if (Value.GetVariant(interpreter,
                    (IGetValue)arguments[2], ValueFlags.AnyVariant,
                    interpreter.InternalCultureInfo, ref variant2,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                if (variant1.IsDouble())
                {
                    value = Math.Log(
                        (double)variant1.Value,
                        (double)variant2.Value);
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    if (variant2.ConvertTo(TypeCode.Double))
                    {
                        value = BigInteger.Log(
                            (BigInteger)variant1.Value,
                            (double)variant2.Value);
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to double",
                            FormatOps.WrapOrNull(arguments[2]));

                        return ReturnCode.Error;
                    }
                }
#endif
                else if (variant1.ConvertTo(TypeCode.Double) &&
                    variant2.ConvertTo(TypeCode.Double))
                {
                    value = Math.Log(
                        (double)variant1.Value,
                        (double)variant2.Value);
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
