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
    internal sealed class _TimeSpan : Arguments
    {
        #region Public Constructors
        public _TimeSpan(
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
                if (variant1.IsTimeSpan())
                {
                    value = (TimeSpan)variant1.Value; /* NOP */
                }
                else if (variant1.IsDouble())
                {
                    value = ConversionOps.ToTimeSpan(
                        (double)variant1.Value); /* SAFE */
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
                        return ReturnCode.Error;
                    }
                }
                else if (variant1.IsWideInteger())
                {
                    value = ConversionOps.ToTimeSpan(
                        (long)variant1.Value);
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToTimeSpan(
                        (int)variant1.Value);
                }
                else if (variant1.IsBoolean())
                {
                    value = ConversionOps.ToTimeSpan(
                        (bool)variant1.Value);
                }
                else
                {
                    error = String.Format(
                        "unable to convert time-span string {0}",
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
