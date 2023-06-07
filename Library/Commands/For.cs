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
    [ObjectId("5ca5bf1e-8f0d-4b3e-a836-3dcf89b990eb")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("loop")]
    internal sealed class For : Core
    {
        public For(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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

                                if (code != ReturnCode.Ok)
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
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"for\" loop-end command)",
                                            Environment.NewLine));
                                    break;
                                }
                                else if (code != ReturnCode.Ok) // TEST: What about break and continue here?
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

                            if ((code == ReturnCode.Ok) && (arguments.Count == 6))
                            {
                                code = interpreter.EvaluateScript(arguments[5], ref result);

                                if (code == ReturnCode.Error)
                                {
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"for\" final command)",
                                            Environment.NewLine));
                                }
                            }

                            if (code == ReturnCode.Break)
                                code = ReturnCode.Ok;

                            if (code == ReturnCode.Ok)
                                Engine.ResetResult(interpreter, ref result);
                        }
                        else if (code == ReturnCode.Error)
                        {
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
