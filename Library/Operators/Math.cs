/*
 * Math.cs --
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
    /// This class provides the base implementation for the Eagle arithmetic
    /// and bitwise expression operators (such as <c>+</c>, <c>-</c>, <c>*</c>,
    /// <c>/</c>, <c>%</c>, <c>&lt;&lt;</c>, and <c>&gt;&gt;</c>).  Concrete
    /// operator classes derive from it and select their specific behavior via
    /// the <c>[Lexeme(...)]</c> attribute; the numeric evaluation itself is
    /// performed here by extracting the operands and delegating to
    /// <see cref="IMath.Calculate" />.  See <c>core_language.md</c> for
    /// expression and operator semantics.
    /// </summary>
    [ObjectId("d3f508da-d35c-49e5-ae5d-45ae12764f82")]
    [Operands(Arity.UnaryAndBinary)]
    [ObjectGroup("core")]
    internal class Math : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the arithmetic/bitwise operator base
        /// class.  When attribute-based flags are not suppressed, the operator
        /// flags from the derived type and its base type are merged into this
        /// instance.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Math(
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
        /// This method evaluates an arithmetic or bitwise operator.  It
        /// extracts one or two operands from the supplied arguments, fixes up
        /// the operand variants so they share a compatible numeric type, and
        /// then performs the calculation appropriate for this operator's
        /// lexeme.
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
        /// The list of arguments for this invocation, which provides the one
        /// or two operands for the operator.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed result of the operator.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter or argument
        /// list is invalid, an operand cannot be obtained or converted, or a
        /// math exception occurs, with details placed in
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
                        this, operand1, operand2, null, null, false, false,
                        ref error) != ReturnCode.Ok))
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
