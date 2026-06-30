/*
 * TimeSpan.cs --
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
    /// This class implements the Eagle <c>timespan</c> expression function,
    /// which converts its single argument into a time-span value.  An argument
    /// that is already a time-span is returned unchanged; integer,
    /// wide-integer, and big-integer arguments are interpreted as a tick count;
    /// a boolean argument yields a one-tick or zero-tick span; a decimal
    /// argument is converted via the standard type conversion; and a double
    /// argument is reinterpreted from its raw bit pattern.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("022f38b7-f63c-439d-8329-7c1825e4c4ad")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("conversion")]
    internal sealed class _TimeSpan : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>timespan</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public _TimeSpan(
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
        /// This method evaluates the <c>timespan</c> function.  It validates
        /// the arguments using the base implementation, obtains the single
        /// argument as a variant, and converts it to a time-span value.  A
        /// value that is already a time-span is used as-is; double, decimal,
        /// big-integer, wide-integer, integer, and boolean values are
        /// converted into a corresponding time-span.
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
        /// function name; element one is the value to convert into a time-span.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the resulting time-span value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing, cannot
        /// be converted to a time-span, is too large to represent, or a math
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
                if (variant1.IsTimeSpan())
                {
                    value = (TimeSpan)variant1.Value; /* NOP */
                }
                else if (variant1.IsDouble())
                {
                    value = ConversionOps.ToTimeSpan(
                        (double)variant1.Value); /* SAFE */
                }
                else if (variant1.IsDecimal())
                {
                    if (variant1.ConvertTo(typeof(TimeSpan)))
                    {
                        value = (TimeSpan)variant1.Value;
                    }
                    else
                    {
                        error = "time-span value too large to represent";
                        return ReturnCode.Error;
                    }
                }
#if NET_40
                else if (variant1.IsBigInteger())
                {
                    value = ConversionOps.ToTimeSpan(
                        (BigInteger)variant1.Value);
                }
#endif
                else if (variant1.IsWideInteger())
                {
                    value = ConversionOps.ToTimeSpan(
                        (long)variant1.Value);
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToTimeSpan(
                        (int)variant1.Value);
                }
                else if (variant1.IsBoolean())
                {
                    value = ConversionOps.ToTimeSpan(
                        (bool)variant1.Value);
                }
                else
                {
                    error = String.Format(
                        "unable to convert time-span string {0}",
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
