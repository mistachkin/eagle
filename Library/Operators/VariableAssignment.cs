/*
 * VariableAssignment.cs --
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
    /// This class implements the Eagle <c>:=</c> (variable assignment)
    /// expression operator, which evaluates its right-hand operand and stores
    /// the resulting value into the variable named by its left-hand operand,
    /// yielding the assigned value.  This is a non-standard, assignment operator
    /// that derives from the <see cref="Core" /> base class and supplies its own
    /// evaluation logic, selected by the <see cref="Lexeme.VariableAssignment" />
    /// lexeme.  See <c>core_language.md</c> for expression and operator
    /// semantics.
    /// </summary>
    [ObjectId("e46b3b67-cae1-4142-8c4f-af6cd776ced6")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Assignment)]
    [Lexeme(Lexeme.VariableAssignment)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("assignment")]
    [ObjectName(Operators.VariableAssignment)]
    internal sealed class VariableAssignment : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message format used when a "safe" interpreter attempts to
        /// use this operator.  The single format placeholder is replaced with
        /// the name of the operator.
        /// </summary>
        private const string SafeError =
            "permission denied: safe interpreter cannot use operator {0}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>:=</c> variable assignment operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public VariableAssignment(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates the <c>:=</c> variable assignment operator.  It
        /// extracts its two operands from the argument list, treats the first
        /// operand as a variable name and the second operand as the value to
        /// assign, stores that value into the named variable, and returns the
        /// assigned value.  Use of this operator is denied in a "safe"
        /// interpreter.
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
        /// name and its two operands: the target variable name and the value to
        /// assign.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the value that was assigned to the
        /// variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the assigned value
        /// placed in <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter or argument list
        /// is invalid, the interpreter is "safe", the operands cannot be
        /// obtained, the variable cannot be set, or an exception occurs, with
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

            if (interpreter.InternalIsSafeOrSdk())
            {
                //
                // BUGBUG: Technically, this is not quite 100%
                //         accurate.  If this interpreter was
                //         created for use by the license -OR-
                //         security SDK, it may not be "safe";
                //         however, to keep things simple, just
                //         use that error message.
                //
                error = String.Format(
                    SafeError, FormatOps.WrapOrNull(base.Name));

                return ReturnCode.Error;
            }

            try
            {
                IVariant operand1 = null;
                IVariant operand2 = null;

                if (Value.GetOperandsFromArguments(interpreter,
                        this, arguments, ValueFlags.String, ValueFlags.None,
                        interpreter.InternalCultureInfo, false, ref operand1,
                        ref operand2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (interpreter.SetVariableValue2(
                        VariableFlags.None, null, (string)operand1.Value,
                        operand2.Value, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                value = Argument.InternalCreate(operand2);
                return ReturnCode.Ok;
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
