/*
 * Round2.cs --
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
    /// This class implements the Eagle <c>round2</c> expression function,
    /// which rounds its first numeric argument to the number of fractional
    /// digits specified by its second integer argument.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("3dea331a-1781-44e6-9bb1-5d8aa529fd29")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("rounding")]
    internal sealed class Round2 : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>round2</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Round2(
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
        /// This method evaluates the <c>round2</c> function.  It validates the
        /// arguments using the base implementation, obtains the value to be
        /// rounded and the number of fractional digits, and rounds the value
        /// according to its underlying numeric type.  Double and decimal
        /// values are rounded to the requested number of digits; integer and
        /// boolean values are returned unchanged.
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
        /// function name; element one is the value to be rounded; element two
        /// is the number of fractional digits to round to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the rounded result.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when an argument is missing or not
        /// of a supported type, or a math exception occurs, with details
        /// placed in <paramref name="error" />.
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

            try
            {
                if (variant1.IsDouble())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.
                    //
                    value = Math.Round(
                        (double)variant1.Value, intValue);
                }
                else if (variant1.IsDecimal())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.
                    //
                    value = Math.Round(
                        (decimal)variant1.Value, intValue);
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
