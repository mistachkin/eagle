/*
 * Question.cs --
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
    /// This class implements the Eagle <c>?</c> (conditional, ternary)
    /// expression operator, which evaluates its first operand as a boolean
    /// condition and then evaluates and returns either its second ("then")
    /// operand or its third ("else") operand accordingly.  Unlike most
    /// operators, the evaluation logic is implemented directly here rather than
    /// being inherited from a numeric base class, because the unselected branch
    /// must not be evaluated.  See <c>core_language.md</c> for expression and
    /// operator semantics.
    /// </summary>
    [ObjectId("c3ce831f-e157-4df9-8bfa-844b94d1104f")]
    [OperatorFlags(
        OperatorFlags.Special | OperatorFlags.Direct |
        OperatorFlags.Standard | OperatorFlags.Conditional |
        OperatorFlags.Initialize)]
    [Lexeme(Lexeme.Question)]
    [Operands(Arity.Ternary)]
    [ObjectGroup("conditional")]
    [ObjectName(Operators.Question)]
    internal sealed class Question : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message format used when the operator is invoked using
        /// infix syntax with the wrong number of operands.  The first format
        /// placeholder is the operator name and the second is the colon
        /// separator.
        /// </summary>
        private const string InfixSyntaxError =
            "wrong # args: should be \"operand1 {0} operand2 {1} operand3\"";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error message format used when the operator is invoked using
        /// prefix (function-call) syntax with the wrong number of operands.
        /// The single format placeholder is the operator name.
        /// </summary>
        private const string PrefixSyntaxError =
            "wrong # args: should be \"{0} operand1 operand2 operand3\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>?</c> conditional operator.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Question(
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
        /// This method evaluates the <c>?</c> conditional operator.  It
        /// validates the operand count, evaluates the first operand as a boolean
        /// condition, and then evaluates and returns either the second ("then")
        /// operand or the third ("else") operand depending on the result; only
        /// the selected branch is evaluated.
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
        /// The list of arguments for this invocation.  Element zero is the
        /// operator name; element one is the condition expression; element two
        /// is the "then" expression; element three is the "else" expression.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the result of evaluating the selected
        /// branch expression.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the operand count is wrong, the
        /// condition cannot be converted to a boolean, or a branch expression
        /// fails to evaluate, with details placed in
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

            int argumentCount = arguments.Count;

            string localName = (argumentCount > 0) ?
                (string)arguments[0] : this.Name;

            if (argumentCount != (this.Operands + 1))
            {
                if (ExpressionParser.IsOperatorNameOnly(localName))
                {
                    error = String.Format(InfixSyntaxError,
                        FormatOps.OperatorName(localName),
                        Characters.Colon);
                }
                else
                {
                    error = String.Format(PrefixSyntaxError,
                        FormatOps.OperatorName(localName));
                }

                return ReturnCode.Error;
            }

            string errorInfo = "{0}    (\"if\" expression)";
            Result localResult = null; /* REUSED */

            if (interpreter.InternalEvaluateExpressionWithErrorInfo(
                    arguments[1], errorInfo,
                    ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            bool boolValue = false;

            if (Engine.ToBoolean(
                    localResult, interpreter.InternalCultureInfo,
                    ref boolValue, ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            if (boolValue)
            {
                errorInfo = "{0}    (\"then\" expression)";
                localResult = null;

                if (interpreter.InternalEvaluateExpressionWithErrorInfo(
                        arguments[2], errorInfo,
                        ref localResult) == ReturnCode.Ok)
                {
                    value = localResult;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                errorInfo = "{0}    (\"else\" expression)";
                localResult = null;

                if (interpreter.InternalEvaluateExpressionWithErrorInfo(
                        arguments[3], errorInfo,
                        ref localResult) == ReturnCode.Ok)
                {
                    value = localResult;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
        }
        #endregion
    }
}
