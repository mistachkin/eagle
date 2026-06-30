/*
 * For.cs --
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
    /// This class implements the Eagle <c>for</c> command, which provides a
    /// general looping construct composed of a start script, a test
    /// expression, a next script, and a body script, with an optional final
    /// script.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("5ca5bf1e-8f0d-4b3e-a836-3dcf89b990eb")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("loop")]
    internal sealed class For : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>for</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public For(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>for</c> command.  It evaluates the
        /// start script once, then repeatedly evaluates the test expression
        /// and, while it remains true, evaluates the body script followed by
        /// the next script; an optional final script is evaluated once the
        /// loop completes normally.
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
        /// command name; elements one through four are the start script, the
        /// test expression, the next script, and the body script; an optional
        /// element five supplies the final script.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this is reset to an empty result.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the loop completes normally
        /// (including when terminated by a <c>break</c> within the body);
        /// otherwise, a non-Ok value such as <see cref="ReturnCode.Error" />
        /// (e.g. when the wrong number of arguments is supplied, the
        /// interpreter is null, the argument list is null, the iteration limit
        /// is exceeded, or one of the evaluated scripts or the test expression
        /// fails) or a control-flow value propagated out of the body, with
        /// details placed in <paramref name="result" />.
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
                    //
                    // for <start> <test> <next> <body> [end]
                    //
                    if ((arguments.Count == 5) || (arguments.Count == 6))
                    {
                        code = interpreter.EvaluateScript(arguments[1], ref result);

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Evaluate and check the "test" expression.
                            //
                            int iterationLimit = interpreter.InternalIterationLimit;
                            int iterationCount = 0;

                            string errorInfo = "{0}    (\"for\" test expression)";

                            while (true)
                            {
                                Engine.ResetResult(interpreter, ref result);

                                //
                                // NOTE: Evaluate the test expression.
                                //
                                code = interpreter.InternalEvaluateExpressionWithErrorInfo(
                                    arguments[2], errorInfo, ref result);

                                if ((code != ReturnCode.Ok) || interpreter.ExitNoThrow)
                                    break;

                                bool value = false;

                                code = Engine.ToBoolean(
                                    result, interpreter.InternalCultureInfo,
                                    ref value, ref result);

                                if (code != ReturnCode.Ok)
                                    break;

                                if (!value)
                                    break;

                                //
                                // NOTE: Evaluate the "body" script.
                                //
                                code = interpreter.EvaluateScript(arguments[4], ref result);

                                if (code == ReturnCode.Error)
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"for\" body line {1})",
                                            Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                                    break;
                                }
                                else if ((code != ReturnCode.Ok) && (code != ReturnCode.Continue))
                                {
                                    break;
                                }

                                //
                                // NOTE: Evaluate the "next" script.
                                //
                                code = interpreter.EvaluateScript(arguments[3], ref result);

                                if (code == ReturnCode.Error)
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"for\" loop-end command)",
                                            Environment.NewLine));

                                    break;
                                }
                                else if (code != ReturnCode.Ok) // TEST: What about break and continue here?
                                {
                                    break;
                                }

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

                            if ((code == ReturnCode.Ok) && (arguments.Count == 6))
                            {
                                code = interpreter.EvaluateScript(arguments[5], ref result);

                                if (code == ReturnCode.Error)
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"for\" final command)",
                                            Environment.NewLine));
                                }
                            }

                            if (code == ReturnCode.Break)
                                code = ReturnCode.Ok;

                            if (code == ReturnCode.Ok)
                            {
                                /* IGNORED */
                                Engine.ResetResult(interpreter, ref result);
                            }
                        }
                        else if (code == ReturnCode.Error)
                        {
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"for\" initial command)",
                                    Environment.NewLine));
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"for start test next script ?end?\"";
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
