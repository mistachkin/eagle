/*
 * Double.cs --
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
    /// This class implements the Eagle <c>double</c> expression function, which
    /// converts its single argument to a double-precision floating-point value.
    /// Date/time, decimal, big-integer, wide-integer, integer, and boolean
    /// inputs are all converted appropriately.  See <c>core_language.md</c> for
    /// expression and function semantics.
    /// </summary>
    [ObjectId("fa5caa7b-48c7-46b0-b674-6aaab441f09f")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Double : Arguments
    {
        /// <summary>
        /// Constructs an instance of the <c>double</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Double(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates the <c>double</c> function.  It validates the
        /// arguments using the base implementation, obtains the single argument
        /// as a variant, and converts it to a double-precision floating-point
        /// value based on its underlying type.
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
        /// function name; element one is the value to convert to a double.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the converted double value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing, cannot
        /// be converted to a double, or a math exception occurs, with details
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

            try
            {
                if (variant1.IsDateTime())
                {
                    value = ConversionOps.ToDouble(
                        (DateTime)variant1.Value);
                }
                else if (variant1.IsDouble())
                {
                    value = (double)variant1.Value; /* NOP */
                }
                else if (variant1.IsDecimal())
                {
                    if (variant1.ConvertTo(TypeCode.Double))
                    {
                        value = (double)variant1.Value;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to double",
                            FormatOps.WrapOrNull(arguments[1]));

                        return ReturnCode.Error;
                    }
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = ConversionOps.ToDouble(
                        (BigInteger)variant1.Value);
                }
#endif
                else if (variant1.IsWideInteger())
                {
                    value = ConversionOps.ToDouble(
                        (long)variant1.Value);
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToDouble(
                        (int)variant1.Value);
                }
                else if (variant1.IsBoolean())
                {
                    value = ConversionOps.ToDouble(
                        (bool)variant1.Value);
                }
                else
                {
                    error = String.Format(
                        "expected floating-point number but got {0}",
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
