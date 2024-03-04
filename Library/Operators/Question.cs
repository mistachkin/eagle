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
        private const string InfixSyntaxError =
            "wrong # args: should be \"operand1 {0} operand2 {1} operand3\"";

        ///////////////////////////////////////////////////////////////////////

        private const string PrefixSyntaxError =
            "wrong # args: should be \"{0} operand1 operand2 operand3\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
