/*
 * Logic.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    /// <summary>
    /// This class provides the base implementation for the Eagle logical
    /// expression operators, such as <c>!</c>, <c>&amp;&amp;</c>, <c>||</c>,
    /// <c>^^</c>, <c>&lt;=&gt;</c>, and <c>=&gt;</c>.  Derived operator
    /// classes select the specific operation via the <see cref="Lexeme" />
    /// attribute, and the evaluation logic is provided here by coercing the
    /// operands to boolean (when binary) and calculating the result.  See
    /// <c>core_language.md</c> for expression and operator semantics.
    /// </summary>
    [ObjectId("88a6d706-f3c5-412a-9f4b-3948703e82c1")]
    [Operands(Arity.UnaryAndBinary)]
    [ObjectGroup("core")]
    internal class Logic : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the logic operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Logic(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            if ((operatorData == null) || !FlagOps.HasFlags(
                    operatorData.Flags, OperatorFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetOperatorFlags(GetType().BaseType) |
                    AttributeOps.GetOperatorFlags(this);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates the logic operator.  It extracts one or two
        /// operands from the argument list, coerces them to boolean (when a
        /// second operand is present), and calculates the result for the
        /// operator identified by this instance's <see cref="Lexeme" />.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied when this operator was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation, containing the operator
        /// name followed by its one or two operands.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed result of the operator.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise, <see cref="ReturnCode.Error" />
        /// when the interpreter or argument list is invalid, the operands cannot
        /// be obtained or coerced, or a math exception occurs, with details
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
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            try
            {
                IVariant operand1 = null;
                IVariant operand2 = null;

                if (Value.GetOperandsFromArguments(
                        interpreter, this, arguments, ValueFlags.AnyVariant,
                        interpreter.InternalCultureInfo, false, ref operand1,
                        ref operand2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((operand2 != null) && (Value.FixupVariants(
                        this, operand1, operand2, typeof(bool), typeof(bool),
                        false, false, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                return operand1.Calculate(
                    this, this.Lexeme, operand2, NumberOps.GetRotateBits(
                    interpreter), ref value, ref error);
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format("caught math exception: {0}", e);

                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
