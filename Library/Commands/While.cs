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
    [ObjectId("eccc438a-2bfe-4a5d-b3e5-555809bd7bc8")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("loop")]
    internal sealed class While : Core
    {
        public While(
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

                            if (code != ReturnCode.Ok)
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
