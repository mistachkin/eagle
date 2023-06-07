/*
 * Do.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Commands
{
    [ObjectId("8ba664e1-10b3-4805-91e4-44ebabcdee15")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("loop")]
    internal sealed class Do : Core
    {
        private const string While = "while";
        private const string Until = "until";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Do(
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
                    if ((arguments.Count == 3) || (arguments.Count == 4))
                    {
                        int index = 2;
                        string clause = arguments[index];
                        bool @while = false;
                        bool until = false;

                        if ((index < arguments.Count) && (clause.Length > 0) &&
                            SharedStringOps.SystemEquals(clause, Do.While))
                        {
                            @while = true;
                            until = false;

                            index++; // skip over optional "while"
                        }
                        else if ((index < arguments.Count) && (clause.Length > 0) &&
                            SharedStringOps.SystemEquals(clause, Do.Until))
                        {
                            @while = false;
                            until = true;

                            index++; // skip over optional "until"
                        }
                        else
                        {
                            //
                            // NOTE: The "while" clause is the default.
                            //
                            @while = true;
                            until = false;
                        }

                        if (index >= arguments.Count)
                        {
                            clause = arguments[index - 1];

                            result = String.Format(
                                "wrong # args: no expression following \"{0}\" argument", 
                                clause);

                            code = ReturnCode.Error;
                        }
                        else if (((index + 1) < arguments.Count) &&
                            ((index + 2) == arguments.Count))
                        {
                            result = String.Format(
                                "wrong # args: bad clause \"{0}\", must be \"{1}\" or \"{2}\"",
                                clause, Do.While, Do.Until);

                            code = ReturnCode.Error;
                        }
                        else if ((index + 1) < arguments.Count)
                        {
                            result = "wrong # args: should be \"do script clause test\"";
                            code = ReturnCode.Error;
                        }

                        if (code == ReturnCode.Ok)
                        {
                            if (@while || until)
                            {
                                //
                                // NOTE: Evaluate script and then check the "test" expression.
                                //
                                int iterationLimit = interpreter.InternalIterationLimit;
                                int iterationCount = 0;

                                string errorInfo = "{0}    (\"do\" test expression)";

                                while (true)
                                {
                                    code = interpreter.EvaluateScript(arguments[1], ref result);

                                    if (code == ReturnCode.Error)
                                    {
                                        Engine.AddErrorInformation(interpreter, result,
                                            String.Format("{0}    (\"do\" body line {1})",
                                                Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                                        break;
                                    }
                                    else if ((code != ReturnCode.Ok) && (code != ReturnCode.Continue))
                                    {
                                        break;
                                    }

                                    code = interpreter.InternalEvaluateExpressionWithErrorInfo(
                                        arguments[index], errorInfo, ref result);

                                    if (code != ReturnCode.Ok)
                                        break;

                                    bool value = false;

                                    code = Engine.ToBoolean(
                                        result, interpreter.InternalCultureInfo, ref value, ref result);

                                    if (code != ReturnCode.Ok)
                                        break;

                                    if ((@while && !value) || (until && value))
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
                                result = "wrong # args: missing clause";
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"do script clause test\"";
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
