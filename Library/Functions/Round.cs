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
    /// <summary>
    /// This class implements the Eagle <c>round</c> expression function, which
    /// rounds its single numeric argument to the nearest integral value, using
    /// round-half-away-from-zero semantics for non-integral numeric types and
    /// returning integral types unchanged.  See <c>core_language.md</c> for
    /// expression and function semantics.
    /// </summary>
    [ObjectId("ef06256a-9a2e-41eb-88bb-7a34f14bbe21")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("rounding")]
    internal sealed class Round : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>round</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>round</c> function.  It validates the
        /// arguments using the base implementation, converts the single
        /// argument to a variant, and rounds it to the nearest integral value.
        /// Double and decimal values are rounded using
        /// <see cref="MidpointRounding.AwayFromZero" /> so that halves round
        /// away from zero (e.g. <c>round(2.5)</c> is <c>3</c> and
        /// <c>round(-2.5)</c> is <c>-3</c>), matching Tcl; wide integer,
        /// integer, and boolean values are already integral and are returned
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
        /// function name; element one is the value to be rounded.  This
        /// parameter should not be null.
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
        /// <see cref="ReturnCode.Error" /> when the argument is missing, not
        /// numeric, of an unsupported type, or a math exception occurs, with
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
                if (variant1.IsDouble())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.  Tcl's round()
                    //       rounds halves AWAY FROM ZERO (e.g. round(2.5) == 3,
                    //       round(-2.5) == -3); the default Math.Round uses
                    //       banker's rounding (to-even), which is NOT Tcl
                    //       compatible.  Use the round-3 function for other
                    //       rounding modes.
                    //
                    value = Math.Round((double)variant1.Value,
                        MidpointRounding.AwayFromZero);
                }
                else if (variant1.IsDecimal())
                {
                    //
                    // NOTE: No FixPrecision, Already rounding.  See the note
                    //       above regarding round-half-away-from-zero.
                    //
                    value = Math.Round((decimal)variant1.Value,
                        MidpointRounding.AwayFromZero);
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
