/*
 * DateTime.cs --
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
    [ObjectId("132e9b03-7a92-4fab-bba6-50176432741c")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("conversion")]
    internal sealed class _DateTime : Arguments
    {
        #region Public Constructors
        public _DateTime(
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

            DateTimeKind dateTimeKind = interpreter.DateTimeKind;

            try
            {
                if (variant1.IsDateTime())
                {
                    value = (DateTime)variant1.Value; /* NOP */
                }
                else if (variant1.IsDouble())
                {
                    value = ConversionOps.ToDateTime(
                        (double)variant1.Value, dateTimeKind); /* SAFE */
                }
                else if (variant1.IsDecimal())
                {
                    if (variant1.ConvertTo(TypeCode.DateTime))
                    {
                        value = (DateTime)variant1.Value;
                    }
                    else
                    {
                        error = "date-time value too large to represent";
                        return ReturnCode.Error;
                    }
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = ConversionOps.ToDateTime(
                        (BigInteger)variant1.Value, dateTimeKind);
                }
#endif
                else if (variant1.IsWideInteger())
                {
                    value = ConversionOps.ToDateTime(
                        (long)variant1.Value, dateTimeKind);
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToDateTime(
                        (int)variant1.Value, dateTimeKind);
                }
                else if (variant1.IsBoolean())
                {
                    value = ConversionOps.ToDateTime(
                        (bool)variant1.Value, dateTimeKind);
                }
                else
                {
                    error = String.Format(
                        "unable to convert date-time string {0}",
                        FormatOps.WrapOrNull(arguments[1]));

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
