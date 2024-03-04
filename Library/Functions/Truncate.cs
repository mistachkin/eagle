/*
 * Truncate.cs --
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
    [ObjectId("fd0e647e-beeb-408b-9ebf-b6d9e09d10d9")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NonIntegralTypes)]
    [ObjectGroup("rounding")]
    internal sealed class Truncate : Arguments
    {
        #region Public Constructors
        public Truncate(
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
                    value = Math.Truncate((double)variant1.Value);
                }
                else if (variant1.ConvertTo(TypeCode.Decimal))
                {
                    value = Math.Truncate((decimal)variant1.Value);
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
