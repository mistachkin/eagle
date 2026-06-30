/*
 * Max.cs --
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
    /// <summary>
    /// This class implements the Eagle <c>max</c> expression function, which
    /// returns the largest of its two or more numeric arguments.  The
    /// comparison is performed using the widest common numeric type of the
    /// arguments.  See <c>core_language.md</c> for expression and function
    /// semantics.
    /// </summary>
    [ObjectId("44e2cc2a-f2a8-445f-83d6-cb4c4ec60cd8")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Any)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("aggregate")]
    internal sealed class Max : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>max</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Max(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates the <c>max</c> function.  It validates the
        /// arguments using the base implementation, fixes up the arguments to a
        /// common numeric type, and returns the largest of them.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied when this function was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// function name; the remaining elements are the values to compare.
        /// At least two values are required.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the largest of the argument values.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when fewer than two arguments are
        /// supplied, an argument is not numeric or of an unsupported type, or a
        /// math exception occurs, with details placed in
        /// <paramref name="error" />.
        /// </returns>
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
                        variant1.Value = Math.Max(
                            (double)variant1.Value,
                            (double)variant2.Value);
                    }
                    else if (variant1.IsDecimal())
                    {
                        variant1.Value = Math.Max(
                            (decimal)variant1.Value,
                            (decimal)variant2.Value);
                    }
#if NET_40
                    else if (variant1.IsBigInteger())
                    {
                        variant1.Value = BigInteger.Max(
                            (BigInteger)variant1.Value,
                            (BigInteger)variant2.Value);
                    }
#endif
                    else if (variant1.IsWideInteger())
                    {
                        variant1.Value = Math.Max(
                            (long)variant1.Value,
                            (long)variant2.Value);
                    }
                    else if (variant1.IsInteger())
                    {
                        variant1.Value = Math.Max(
                            (int)variant1.Value,
                            (int)variant2.Value);
                    }
                    else if (variant1.IsBoolean())
                    {
                        variant1.Value = Math.Max(
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
