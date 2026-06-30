/*
 * Sign.cs --
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
    /// This class implements the Eagle <c>sign</c> expression function, which
    /// returns an integer indicating the sign of its single numeric argument:
    /// <c>-1</c> when the value is negative, <c>0</c> when it is zero, and
    /// <c>1</c> when it is positive.  See <c>core_language.md</c> for
    /// expression and function semantics.
    /// </summary>
    [ObjectId("4b1c325e-caa3-419b-9657-80ff0f070fb7")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("component")]
    internal sealed class Sign : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>sign</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Sign(
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
        /// This method evaluates the <c>sign</c> function.  It validates the
        /// arguments using the base implementation, interprets the single
        /// argument as a number, and produces an integer indicating its sign
        /// (<c>-1</c>, <c>0</c>, or <c>1</c>).  Floating-point, decimal,
        /// big-integer, wide-integer, integer, and boolean argument types are
        /// supported.
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
        /// function name; element one is the value whose sign is computed.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed sign of the argument.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing, of an
        /// unsupported type, or a math exception occurs, with details placed in
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
                    value = Math.Sign((double)variant1.Value);
                }
                else if (variant1.IsDecimal())
                {
                    value = Math.Sign((decimal)variant1.Value);
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = ((BigInteger)variant1.Value).Sign;
                }
#endif
                else if (variant1.IsWideInteger())
                {
                    value = Math.Sign((long)variant1.Value);
                }
                else if (variant1.IsInteger())
                {
                    value = Math.Sign((int)variant1.Value);
                }
                else if (variant1.IsBoolean())
                {
                    value = Math.Sign(
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
