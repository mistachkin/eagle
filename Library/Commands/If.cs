/*
 * If.cs --
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
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Commands
{
    [ObjectId("a08efef8-37e2-4abd-8128-b0a16ce2b8a1")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize | CommandFlags.SecuritySdk)]
    [ObjectGroup("conditional")]
    internal sealed class If : Core
    {
        private const string Then = "then";
        private const string ElseIf = "elseif";
        private const string Else = "else";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public If(
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
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    //
                    // if {<expr>} then {<script>} 
                    // [elseif {<expr>} then {<script>}] 
                    // ...
                    // [else {<script>}]
                    //
                    int index = 1; // skip command name (i.e. "if")
                    int thenScriptIndex = 0;
                    string clause;

                    while (true)
                    {
                        if (index >= arguments.Count)
                        {
                            clause = arguments[index - 1];

                            result = String.Format(
                                "wrong # args: no expression after \"{0}\" argument",
                                clause);

                            return ReturnCode.Error;
                        }

                        bool value = false;

                        if (thenScriptIndex == 0)
                        {
                            string errorInfo = "{0}    (\"if\" test expression)";
                            Result localResult = null;

                            code = interpreter.InternalEvaluateExpressionWithErrorInfo(
                                arguments[index], errorInfo, ref localResult);

                            if (code == ReturnCode.Ok)
                            {
                                code = Engine.ToBoolean(
                                    localResult, interpreter.InternalCultureInfo,
                                    ref value, ref localResult);

                                if (code != ReturnCode.Ok)
                                {
                                    result = localResult;
                                    return code;
                                }
                            }
                            else
                            {
                                result = localResult;
                                return code;
                            }
                        }

                        index++;

                        if (index >= arguments.Count)
                        {
                            clause = arguments[index - 1];

                            result = String.Format(
                                "wrong # args: no script following \"{0}\" argument",
                                clause);

                            return ReturnCode.Error;
                        }

                        clause = arguments[index];

                        if ((index < arguments.Count) &&
                            SharedStringOps.SystemEquals(clause, If.Then))
                        {
                            index++; // skip over optional "then"
                        }

                        if (index >= arguments.Count)
                        {
                            clause = arguments[index - 1];

                            result = String.Format(
                                "wrong # args: no script following \"{0}\" argument",
                                clause);

                            return ReturnCode.Error;
                        }

                        if (value)
                        {
                            thenScriptIndex = index;
                            value = false;
                        }

                        index++;

                        if (index >= arguments.Count)
                        {
                            if (thenScriptIndex != 0)
                            {
                                code = interpreter.EvaluateScript(
                                    arguments[thenScriptIndex], ref result);

                                if (code == ReturnCode.Error)
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"if\" then script line {1})",
                                            Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                                return code;
                            }

                            result = String.Empty;
                            return ReturnCode.Ok;
                        }

                        clause = arguments[index];

                        if (SharedStringOps.SystemEquals(clause, If.ElseIf))
                        {
                            index++;
                            continue;
                        }

                        break;
                    }

                    if (SharedStringOps.SystemEquals(clause, If.Else))
                    {
                        index++;

                        if (index >= arguments.Count)
                        {
                            result = String.Format(
                                "wrong # args: no script following \"{0}\" argument",
                                clause);

                            return ReturnCode.Error;
                        }
                    }

                    if (index < (arguments.Count - 1))
                    {
                        result = String.Format(
                            "wrong # args: extra words after \"{0}\" clause in \"{1}\" command",
                            clause, arguments[0]);

                        return ReturnCode.Error;
                    }

                    if (thenScriptIndex != 0)
                    {
                        code = interpreter.EvaluateScript(
                            arguments[thenScriptIndex], ref result);

                        if (code == ReturnCode.Error)
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"if\" then script line {1})",
                                    Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                        return code;
                    }
                    else
                    {
                        code = interpreter.EvaluateScript(
                            arguments[index], ref result);

                        if (code == ReturnCode.Error)
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"if\" else script line {1})",
                                    Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                        return code;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
