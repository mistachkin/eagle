/*
 * While.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>while</c> command, which repeatedly
    /// evaluates a body script for as long as a test expression remains true.
    /// See <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("eccc438a-2bfe-4a5d-b3e5-555809bd7bc8")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("loop")]
    internal sealed class While : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>while</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public While(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>while</c> command.  It evaluates the
        /// test expression and, while the expression is true, evaluates the
        /// body script, honoring the <c>break</c> and <c>continue</c>
        /// control-flow results as well as the configured iteration limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; element one is the test expression and element two is
        /// the body script to evaluate on each iteration.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty result.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the loop completes normally or is
        /// terminated by a <c>break</c>; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the test expression or body script fails, the
        /// iteration limit is exceeded, the interpreter is null, or the
        /// argument list is null, with details placed in
        /// <paramref name="result" />.  Other non-Ok control-flow values from
        /// the body script may also be returned.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == 3)
                    {
                        bool value = false;

                        //
                        // NOTE: Evaluate and check the "test" expression.
                        //
                        int iterationLimit = interpreter.InternalIterationLimit;
                        int iterationCount = 0;

                        string errorInfo = "{0}    (\"while\" test expression)";

                        while (true)
                        {
                            code = interpreter.InternalEvaluateExpressionWithErrorInfo(
                                arguments[1], errorInfo, ref result);

                            if ((code != ReturnCode.Ok) || interpreter.ExitNoThrow)
                                break;

                            code = Engine.ToBoolean(result, interpreter.InternalCultureInfo,
                                ref value, ref result);

                            if (code != ReturnCode.Ok)
                                break;

                            if (!value)
                                break;

                            code = interpreter.EvaluateScript(arguments[2], ref result);

                            if (code == ReturnCode.Error)
                            {
                                /* IGNORED */
                                Engine.AddErrorInformation(interpreter, result,
                                    String.Format("{0}    (\"while\" body line {1})",
                                        Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                                break;
                            }
                            else if ((code != ReturnCode.Ok) && (code != ReturnCode.Continue))
                                break;

                            if ((iterationLimit != Limits.Unlimited) &&
                                (++iterationCount > iterationLimit))
                            {
                                result = String.Format(
                                    "iteration limit {0} exceeded",
                                    iterationLimit);

                                code = ReturnCode.Error;
                                break;
                            }
                        }

                        if (code == ReturnCode.Break)
                            code = ReturnCode.Ok;

                        if (code == ReturnCode.Ok)
                            Engine.ResetResult(interpreter, ref result);
                    }
                    else
                    {
                        result = "wrong # args: should be \"while test script\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
