/*
 * Pow.cs --
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
    /// This class implements the Eagle <c>pow</c> expression function, which
    /// returns its first numeric argument raised to the power of its second
    /// numeric argument.  See <c>core_language.md</c> for expression and
    /// function semantics.
    /// </summary>
    [ObjectId("a16093f7-ded1-4fe5-9347-3c95a49de5eb")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("power")]
    internal sealed class Pow : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>pow</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Pow(
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
        /// This method evaluates the <c>pow</c> function.  It validates the
        /// arguments using the base implementation and obtains a numeric
        /// variant for each.  When the base is a floating-point value, or when
        /// both arguments can be converted to doubles, the result is computed
        /// via <see cref="Math.Pow(double, double)" />; when the base is a big
        /// integer and the exponent can be converted to a 32-bit integer, the
        /// result is computed via the big-integer power routine.
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
        /// function name; element one is the base value; element two is the
        /// exponent value.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed power.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when an argument is missing, cannot
        /// be converted to a supported numeric type, or a math exception
        /// occurs, with details placed in <paramref name="error" />.
        /// </returns>
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
                    value = Math.Pow(
                        (double)variant1.Value,
                        (double)variant2.Value);
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    if (variant2.ConvertTo(TypeCode.Int32))
                    {
                        value = BigInteger.Pow(
                            (BigInteger)variant1.Value,
                            (int)variant2.Value);
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to integer",
                            FormatOps.WrapOrNull(arguments[2]));

                        return ReturnCode.Error;
                    }
                }
#endif
                else if (variant1.ConvertTo(TypeCode.Double) &&
                    variant2.ConvertTo(TypeCode.Double))
                {
                    value = Math.Pow(
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
