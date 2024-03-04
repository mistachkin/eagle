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
    internal sealed class Min : Arguments
    {
        #region Public Constructors
        public Min(
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
            int argumentCount = 0;

            if (base.Execute(interpreter,
                    clientData, arguments, ref argumentCount,
                    ref value, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (argumentCount < 2)
            {
                error = String.Format(
                    "too few arguments for math function {0}",
                    FormatOps.WrapOrNull(base.Name));

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

            for (int argumentIndex = 2;
                    argumentIndex < argumentCount; argumentIndex++)
            {
                IVariant variant2 = null;

                if (Value.GetVariant(interpreter,
                        (IGetValue)arguments[argumentIndex],
                        ValueFlags.AnyVariant,
                        interpreter.InternalCultureInfo,
                        ref variant2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (Value.FixupVariants(
                        this, variant1, variant2, null, null,
                        false, false, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                try
                {
                    if (variant1.IsDouble())
                    {
                        variant1.Value = Math.Min(
                            (double)variant1.Value,
                            (double)variant2.Value);
                    }
                    else if (variant1.IsDecimal())
                    {
                        variant1.Value = Math.Min(
                            (decimal)variant1.Value,
                            (decimal)variant2.Value);
                    }
                    else if (variant1.IsWideInteger())
                    {
                        variant1.Value = Math.Min(
                            (long)variant1.Value,
                            (long)variant2.Value);
                    }
                    else if (variant1.IsInteger())
                    {
                        variant1.Value = Math.Min(
                            (int)variant1.Value,
                            (int)variant2.Value);
                    }
                    else if (variant1.IsBoolean())
                    {
                        variant1.Value = Math.Min(
                            ConversionOps.ToInt(
                                (bool)variant1.Value),
                            ConversionOps.ToInt(
                                (bool)variant2.Value));
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
            }

            value = Argument.GetOrCreate(
                interpreter, variant1, interpreter.HasNoCacheArgument());

            return ReturnCode.Ok;
        }
        #endregion
    }
}
