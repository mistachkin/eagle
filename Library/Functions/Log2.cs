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
    /// This class implements the Eagle <c>log2</c> expression function, which
    /// returns the base-2 logarithm of its single numeric argument.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("495e138a-6c52-46e6-84dd-ed72514f0b50")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logarithmic")]
    internal sealed class Log2 : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>log2</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>log2</c> function.  It validates the
        /// arguments using the base implementation, converts the single
        /// argument to a numeric variant, and produces its base-2 logarithm
        /// using the appropriate handling for double, decimal, big integer,
        /// wide integer, integer, and boolean values.
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
        /// function name; element one is the value whose base-2 logarithm is
        /// computed.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed base-2 logarithm.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing, not
        /// numeric, of an unsupported type, cannot be converted, or a math
        /// exception occurs, with details placed in
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
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = MathOps.Log2((BigInteger)variant1.Value);
                }
#endif
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
