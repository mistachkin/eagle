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
    /// <summary>
    /// This class implements the Eagle <c>if</c> command, which conditionally
    /// evaluates one of a series of scripts based on the boolean value of its
    /// associated test expressions, optionally falling back to a final
    /// <c>else</c> script when no test succeeds.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("a08efef8-37e2-4abd-8128-b0a16ce2b8a1")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize | CommandFlags.SecuritySdk)]
    [ObjectGroup("conditional")]
    internal sealed class If : Core
    {
        /// <summary>
        /// The literal keyword that optionally introduces the script
        /// associated with a test expression.
        /// </summary>
        private const string Then = "then";

        /// <summary>
        /// The literal keyword that introduces an additional test expression
        /// and script clause.
        /// </summary>
        private const string ElseIf = "elseif";

        /// <summary>
        /// The literal keyword that introduces the fallback script evaluated
        /// when no test expression succeeds.
        /// </summary>
        private const string Else = "else";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the <c>if</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public If(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>if</c> command.  It evaluates the first
        /// test expression and, if true, evaluates its associated script;
        /// otherwise it proceeds through any <c>elseif</c> clauses and finally
        /// the optional <c>else</c> script.  The optional <c>then</c> keyword
        /// may precede each script.
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
        /// command name; the remaining elements form the sequence of test
        /// expressions, optional <c>then</c>/<c>elseif</c>/<c>else</c>
        /// keywords, and scripts.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the evaluated
        /// script, or an empty string when no test expression succeeded and no
        /// <c>else</c> script was present.  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the selected script (if any)
        /// evaluates successfully; otherwise, a non-Ok value such as
        /// <see cref="ReturnCode.Error" /> when an expression or script fails,
        /// the arguments are malformed, the interpreter is null, or the
        /// argument list is null, with details placed in
        /// <paramref name="result" />.  The control-flow values
        /// <see cref="ReturnCode.Break" />, <see cref="ReturnCode.Continue" />,
        /// and <see cref="ReturnCode.Return" /> may be propagated from the
        /// evaluated script.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
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
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (\"if\" then script line {1})",
                                            Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                                }

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
                        {
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"if\" then script line {1})",
                                    Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                        }

                        return code;
                    }
                    else
                    {
                        code = interpreter.EvaluateScript(
                            arguments[index], ref result);

                        if (code == ReturnCode.Error)
                        {
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"if\" else script line {1})",
                                    Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                        }

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
