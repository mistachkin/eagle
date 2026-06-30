/*
 * Round3.cs --
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
    /// <summary>
    /// This class implements the Eagle <c>round</c> expression function in its
    /// three-argument form, which rounds its first numeric argument to the
    /// number of fractional digits given by the second argument using the
    /// midpoint rounding mode named by the third argument.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("0d9f9d0e-e809-46bb-948d-e702b994566a")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Ternary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("rounding")]
    internal sealed class Round3 : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>round</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Round3(
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
        /// This method evaluates the <c>round</c> function.  It validates the
        /// arguments using the base implementation, obtains the value to be
        /// rounded, the integer number of fractional digits, and the
        /// <see cref="MidpointRounding" /> mode, and then rounds the value
        /// accordingly.  Floating-point and decimal values are rounded to the
        /// requested number of digits; integer and boolean values are returned
        /// unchanged.
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
        /// function name; element one is the value to round; element two is the
        /// number of fractional digits to round to; element three is the
        /// midpoint rounding mode.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the rounded value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when an argument is missing, cannot
        /// be converted, has an unsupported type, or a math exception occurs,
        /// with details placed in <paramref name="error" />.
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

            int intValue = 0;

            if (Value.GetInteger2(
                    (IGetValue)arguments[2], ValueFlags.AnyInteger,
                    interpreter.InternalCultureInfo, ref intValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            object enumValue = EnumOps.TryParse(
                typeof(MidpointRounding), arguments[3], true, true,
                ref error);

            if (!(enumValue is MidpointRounding))
                return ReturnCode.Error;

            MidpointRounding rounding = (MidpointRounding)enumValue;

            try
            {
                if (variant1.IsDouble())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.
                    //
                    value = Math.Round(
                        (double)variant1.Value, intValue, rounding);
                }
                else if (variant1.IsDecimal())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.
                    //
                    value = Math.Round(
                        (decimal)variant1.Value, intValue, rounding);
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
