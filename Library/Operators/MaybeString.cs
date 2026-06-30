/*
 * MaybeString.cs --
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
    /// This class provides the base implementation for Eagle binary expression
    /// operators whose operands may be treated as either numbers or strings
    /// (for example, the equality and relational comparison operators).  It
    /// first attempts a numeric calculation and, when the operands cannot be
    /// coerced to compatible numeric variants, falls back to a string
    /// comparison.  Concrete operators derive from this class and are selected
    /// by their associated lexeme.  See <c>core_language.md</c> for expression
    /// and operator semantics.
    /// </summary>
    [ObjectId("e45ff099-9913-449f-bd60-33b928f81538")]
    [Operands(Arity.Binary)]
    [ObjectGroup("core")]
    internal class MaybeString : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>MaybeString</c> operator base
        /// class.  When attribute-based flags are not suppressed, the operator
        /// flags from the base type and this type are merged into the flags for
        /// this instance.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public MaybeString(
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
        /// This method evaluates an operator derived from <c>MaybeString</c>.
        /// It extracts the two operands from the arguments and attempts to fix
        /// them up as compatible numeric variants; when that succeeds it
        /// performs the numeric calculation for the current lexeme, otherwise
        /// it fixes the operands up as strings and performs a string comparison
        /// for the current lexeme.
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
        /// The list of arguments for this invocation, containing the two
        /// operands for this binary operator.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the result of the numeric calculation
        /// or string comparison.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" />, with details placed in
        /// <paramref name="error" />, when the interpreter or argument list is
        /// invalid, the operands cannot be obtained or fixed up, or a math
        /// exception occurs.
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

                if (Value.FixupVariants(this, operand1,
                        operand2, null, null, false, false) == ReturnCode.Ok)
                {
                    return operand1.Calculate(
                        this, this.Lexeme, operand2, NumberOps.GetRotateBits(
                        interpreter), ref value, ref error);
                }
                else
                {
                    //
                    // NOTE: Fine, try to treat the operands as strings.
                    //
                    if (Value.FixupStringVariants(this,
                            operand1, operand2, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    return operand1.StringCompare(
                        this, this.Lexeme, operand2, this.ComparisonType,
                        ref value, ref error);
                }
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
