/*
 * Class8.cs --
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
using _Functions = Eagle._Functions;

namespace Sample
{
    /// <summary>
    /// Declare a "custom function" class that inherits default functionality
    /// and implements the appropriate interface(s).
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("6e1173a4-60af-43c5-9f06-3477b699cd44")]
    [FunctionFlags(FunctionFlags.Safe)]
    [Arguments(Arity.Unary)]
    internal sealed class Class8 : _Functions.Default
    {
        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom function class.
        /// </summary>
        /// <param name="functionData">
        /// An instance of the function data class containing the properties
        /// used to initialize the new instance of this custom function class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class8(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetFunctionFlags(GetType().BaseType) |
                Utility.GetFunctionFlags(this); /* HIGHLY RECOMMENDED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members (Required)
        /// <summary>
        /// Execute the command and return the appropriate result and return
        /// code.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created, if
        /// any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to this command by the script
        /// being evaluated.
        /// </param>
        /// <param name="value">
        /// Upon success, this must contain the result of the function.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.  Other
        /// return codes may be used to implement custom control structures.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                //
                // NOTE: We require a valid argument list.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != (this.Arguments + 1))
            {
                //
                // NOTE: The number of arguments for this function is incorrect.
                //       Provide a meaningful error message to the user (HIGHLY
                //       RECOMMENDED).
                //
                if (arguments.Count > (this.Arguments + 1))
                {
                    error = String.Format(
                        "too many arguments for math function \"{0}\"",
                        base.Name);
                }
                else
                {
                    error = String.Format(
                        "too few arguments for math function \"{0}\"",
                        base.Name);
                }

                return ReturnCode.Error;
            }

            //
            // NOTE: Declare a "Variant" object to hold the numerical value of
            //       the argument.
            //
            IVariant variant1 = null;

            //
            // NOTE: Attempt to convert the argument to a numerical value.
            //
            ReturnCode code = Value.GetVariant(
                interpreter, arguments[1], ValueFlags.AnyVariant,
                interpreter.CultureInfo, ref variant1,
                ref error);

            if (code != ReturnCode.Ok)
                return code;

            //
            // NOTE: The try block here is optional; however, custom functions
            //       are "officially" discouraged from raising exceptions,
            //       especially if they are allowed to escape the Execute
            //       method.
            //
            try
            {
                if (variant1.IsFloatingPoint() ||
                    variant1.IsFixedPoint() || variant1.IsIntegral())
                {
                    double doubleValue = variant1.IsDouble() ?
                        (double)variant1.Value :
                        Convert.ToDouble(variant1.Value);

                    value = doubleValue * doubleValue * Math.PI;
                }
                else
                {
                    error = String.Format(
                        "unsupported IVariant type for function \"{0}\"",
                        base.Name);

                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = String.Format(
                    "caught math exception: {0}",
                    e);

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
