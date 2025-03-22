/*
 * Entier.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Numerics;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("eae9daee-f868-4f7b-9d2f-ec241fb3edb3")]
    [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Entier : Arguments
    {
        #region Public Constructors
        public Entier(
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
                if (variant1.IsDateTime())
                {
                    value = new BigInteger(ConversionOps.ToLong(
                        (DateTime)variant1.Value));
                }
                else if (variant1.IsDouble())
                {
                    value = new BigInteger(Math.Truncate(
                        (double)variant1.Value));
                }
                else if (variant1.IsDecimal())
                {
                    value = new BigInteger(Math.Truncate(
                        (decimal)variant1.Value));
                }
                else if (variant1.IsBigInteger())
                {
                    value = (BigInteger)variant1.Value; /* NOP */
                }
                else if (variant1.IsWideInteger())
                {
                    value = new BigInteger((long)variant1.Value);
                }
                else if (variant1.IsInteger())
                {
                    value = new BigInteger((int)variant1.Value);
                }
                else if (variant1.IsBoolean())
                {
                    value = new BigInteger(ConversionOps.ToInt(
                        (bool)variant1.Value));
                }
                else
                {
                    error = String.Format(
                        "expected big integer but got {0}",
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
