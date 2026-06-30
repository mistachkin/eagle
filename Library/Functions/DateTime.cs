/*
 * DateTime.cs --
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
    /// This class implements the Eagle <c>datetime</c> expression function,
    /// which converts its single argument to a date-time value.  Existing
    /// date-time values are passed through unchanged, while numeric values
    /// (double, decimal, big integer, wide integer, integer, or boolean) are
    /// interpreted relative to the interpreter's configured
    /// <see cref="DateTimeKind" /> using the appropriate conversion.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("132e9b03-7a92-4fab-bba6-50176432741c")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("conversion")]
    internal sealed class _DateTime : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>datetime</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _DateTime(
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
        /// This method evaluates the <c>datetime</c> function.  It validates the
        /// arguments using the base implementation, obtains the single argument
        /// as a variant, and converts it to a date-time value.  Date-time
        /// arguments are used as-is; numeric arguments are converted relative to
        /// the interpreter's <see cref="DateTimeKind" /> via
        /// <see cref="ConversionOps" />.
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
        /// function name; element one is the value converted to a date-time.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the resulting date-time value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing, cannot
        /// be converted to a date-time, or a math exception occurs, with details
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

            DateTimeKind dateTimeKind = interpreter.DateTimeKind;

            try
            {
                if (variant1.IsDateTime())
                {
                    value = (DateTime)variant1.Value; /* NOP */
                }
                else if (variant1.IsDouble())
                {
                    value = ConversionOps.ToDateTime(
                        (double)variant1.Value, dateTimeKind); /* SAFE */
                }
                else if (variant1.IsDecimal())
                {
                    if (variant1.ConvertTo(TypeCode.DateTime))
                    {
                        value = (DateTime)variant1.Value;
                    }
                    else
                    {
                        error = "date-time value too large to represent";
                        return ReturnCode.Error;
                    }
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = ConversionOps.ToDateTime(
                        (BigInteger)variant1.Value, dateTimeKind);
                }
#endif
                else if (variant1.IsWideInteger())
                {
                    value = ConversionOps.ToDateTime(
                        (long)variant1.Value, dateTimeKind);
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToDateTime(
                        (int)variant1.Value, dateTimeKind);
                }
                else if (variant1.IsBoolean())
                {
                    value = ConversionOps.ToDateTime(
                        (bool)variant1.Value, dateTimeKind);
                }
                else
                {
                    error = String.Format(
                        "unable to convert date-time string {0}",
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
