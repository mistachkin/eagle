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
    /// <summary>
    /// This class implements the Eagle <c>entier</c> expression function,
    /// which converts its single argument to an arbitrary-precision integer,
    /// truncating any fractional part toward zero.  See <c>core_language.md</c>
    /// for expression and function semantics.
    /// </summary>
    [ObjectId("eae9daee-f868-4f7b-9d2f-ec241fb3edb3")]
    [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Entier : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>entier</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>entier</c> function.  It validates the
        /// arguments using the base implementation, obtains the single
        /// argument as a variant, and converts it to an arbitrary-precision
        /// <see cref="BigInteger" />.  Floating-point and decimal values are
        /// truncated toward zero; date/time, integer, wide integer, big
        /// integer, and boolean values are converted directly.
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
        /// function name; element one is the value to convert to an integer.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the resulting
        /// <see cref="BigInteger" /> value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing or
        /// cannot be converted to an integer, or a math exception occurs, with
        /// details placed in <paramref name="error" />.
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
