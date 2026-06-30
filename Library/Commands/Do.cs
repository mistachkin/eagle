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
    /// <summary>
    /// This class implements the Eagle <c>do</c> command, which repeatedly
    /// evaluates a body script and then tests an expression, looping while the
    /// expression remains true (the <c>while</c> clause) or until it becomes
    /// true (the <c>until</c> clause).  Because the body is evaluated before
    /// the test, it always runs at least once.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("8ba664e1-10b3-4805-91e4-44ebabcdee15")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("loop")]
    internal sealed class Do : Core
    {
        /// <summary>
        /// The literal name of the optional <c>while</c> clause, which causes
        /// the loop to continue while the test expression is true.  This is
        /// also the default clause when none is supplied.
        /// </summary>
        private const string While = "while";

        /// <summary>
        /// The literal name of the optional <c>until</c> clause, which causes
        /// the loop to continue until the test expression becomes true.
        /// </summary>
        private const string Until = "until";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the <c>do</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Do(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>do</c> command.  It evaluates the body
        /// script and then the test expression, repeating while the
        /// <c>while</c> clause expression stays true or until the <c>until</c>
        /// clause expression becomes true.  The body is always evaluated at
        /// least once, and a <see cref="ReturnCode.Break" /> from the body
        /// terminates the loop normally.
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
        /// command name; element one is the body script; an optional element
        /// supplies the <c>while</c> or <c>until</c> clause keyword; and the
        /// final element is the test expression.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this is reset to an empty result.  Upon failure, this
        /// contains an appropriate error message, such as a wrong number of
        /// arguments, a bad clause keyword, or an exceeded iteration limit.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the loop completes normally;
        /// otherwise, <see cref="ReturnCode.Error" /> (or another non-Ok value
        /// propagated from evaluating the body or test) with details placed in
        /// <paramref name="result" />.
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

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (interpreter.ExitNoThrow)
                                            break;
                                    }
                                    else if (code == ReturnCode.Error)
                                    {
                                        /* IGNORED */
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
