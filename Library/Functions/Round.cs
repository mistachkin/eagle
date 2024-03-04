/*
 * Round.cs --
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
    [ObjectId("ef06256a-9a2e-41eb-88bb-7a34f14bbe21")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("rounding")]
    internal sealed class Round : Arguments
    {
        #region Public Constructors
        public Round(
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
                    //
                    // NOTE: No FixPrecision, Already rounding.
                    //
                    value = Math.Round((double)variant1.Value);
                }
                else if (variant1.IsDecimal())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.
                    //
                    value = Math.Round((decimal)variant1.Value);
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
