/*
 * Atan2.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    /// <summary>
    /// This class implements the Eagle <c>atan2</c> expression function, which
    /// returns the angle, in radians, whose tangent is the quotient of its two
    /// numeric arguments, using their signs to determine the quadrant of the
    /// result.  See <c>core_language.md</c> for expression and function
    /// semantics.
    /// </summary>
    [ObjectId("72402dfb-8dfd-40fb-9529-033ea0cd7b18")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.FloatTypes)]
    [ObjectGroup("trigonometric")]
    internal sealed class Atan2 : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>atan2</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Atan2(
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
        /// This method evaluates the <c>atan2</c> function.  It validates the
        /// arguments using the base implementation, converts both arguments to
        /// doubles, and produces the angle whose tangent is their quotient,
        /// honoring the signs of both arguments to select the proper quadrant.
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
        /// function name; element one is the ordinate (y) value and element two
        /// is the abscissa (x) value.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed angle, in radians.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when an argument is missing or not
        /// numeric, or a math exception occurs, with details placed in
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

            double[] doubleValue = { 0.0, 0.0 };

            if (Value.GetDouble((IGetValue)arguments[1],
                    interpreter.InternalCultureInfo, ref doubleValue[0],
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (Value.GetDouble((IGetValue)arguments[2],
                    interpreter.InternalCultureInfo, ref doubleValue[1],
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                value = Math.Atan2(doubleValue[0], doubleValue[1]);
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
