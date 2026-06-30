/*
 * Expr.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>expr</c> command, which evaluates
    /// its arguments as a mathematical/logical expression and returns the
    /// resulting value.  See <c>core_language.md</c> for the command syntax
    /// and the expression semantics.
    /// </summary>
    [ObjectId("07c8ff7e-4727-4dbb-8aa9-9d8915e16e61")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("expression")]
    internal sealed class Expr : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>expr</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Expr(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>expr</c> command.  It concatenates its
        /// arguments into a single expression, evaluates that expression in a
        /// dedicated expression call frame, and returns the resulting value.
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
        /// command name; the remaining elements form the expression to be
        /// evaluated.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value produced by evaluating the
        /// expression.  Upon failure, this contains an appropriate error
        /// message, including the body line annotation for expression errors.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the expression is evaluated
        /// successfully, with its value placed in <paramref name="result" />;
        /// otherwise, <see cref="ReturnCode.Error" /> when the wrong number of
        /// arguments is supplied, the interpreter is null, the argument list
        /// is null, or expression evaluation fails, with details placed in
        /// <paramref name="result" />.
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
                    if (arguments.Count >= 2)
                    {
                        string name = StringList.MakeList("expr");

                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                            CallFrameFlags.Expression);

                        interpreter.PushAutomaticCallFrame(frame);

                        //
                        // FIXME: The expression parser does not know the line where 
                        //        the error happened unless it evaluates a command 
                        //        contained within the expression.
                        //
                        Interpreter.SetErrorLine(interpreter, 0);

                        if (arguments.Count == 2)
                            code = interpreter.EvaluateExpression(arguments[1], ref result);
                        else
                            code = interpreter.EvaluateExpression(arguments, 1, ref result);

                        if (code == ReturnCode.Error)
                        {
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"expr\" body line {1})",
                                    Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                        }

                        //
                        // NOTE: Pop the original call frame that we pushed above and 
                        //       any intervening scope call frames that may be leftover 
                        //       (i.e. they were not explicitly closed).
                        //
                        /* IGNORED */
                        interpreter.PopScopeCallFramesAndOneMore();
                    }
                    else
                    {
                        result = "wrong # args: should be \"expr arg ?arg ...?\"";
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
