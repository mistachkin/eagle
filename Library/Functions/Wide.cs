/*
 * Wide.cs --
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
    /// This class implements the Eagle <c>wide</c> expression function, which
    /// converts its single argument to a wide (64-bit) integer value.  Date and
    /// time values are converted to their underlying tick count, floating-point
    /// and decimal values are truncated toward zero, and boolean, integer, and
    /// wide integer values are widened as appropriate.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("150aae30-2234-411b-8cac-a13c942aeee9")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Wide : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>wide</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Wide(
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
        /// This method evaluates the <c>wide</c> function.  It validates the
        /// arguments using the base implementation, obtains a variant from the
        /// single argument, and converts it to a wide (64-bit) integer.  Date
        /// and time values yield their tick count, floating-point and decimal
        /// values are truncated toward zero before conversion, and boolean,
        /// integer, big integer, and wide integer values are widened directly.
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
        /// function name; element one is the value converted to a wide integer.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the resulting wide integer value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing or
        /// cannot be converted, the value is too large to represent, or a math
        /// exception occurs, with details placed in <paramref name="error" />.
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
                    value = ConversionOps.ToLong(
                        (DateTime)variant1.Value);
                }
                else if (variant1.IsDouble())
                {
                    variant1.Value = Math.Truncate(
                        (double)variant1.Value);

                    if (variant1.ConvertTo(TypeCode.Int64))
                    {
                        value = (long)variant1.Value;
                    }
                    else
                    {
                        error = "wide integer value too large to represent";
                        return ReturnCode.Error;
                    }
                }
                else if (variant1.IsDecimal())
                {
                    variant1.Value = Math.Truncate(
                        (decimal)variant1.Value);

                    if (variant1.ConvertTo(TypeCode.Int64))
                    {
                        value = (long)variant1.Value;
                    }
                    else
                    {
                        error = "wide integer value too large to represent";
                        return ReturnCode.Error;
                    }
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = ConversionOps.ToLong(
                        (BigInteger)variant1.Value);
                }
#endif
                else if (variant1.IsWideInteger())
                {
                    value = (long)variant1.Value; /* NOP */
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToLong(
                        (int)variant1.Value);
                }
                else if (variant1.IsBoolean())
                {
                    value = ConversionOps.ToLong(
                        (bool)variant1.Value);
                }
                else
                {
                    error = String.Format(
                        "expected wide integer but got {0}",
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
