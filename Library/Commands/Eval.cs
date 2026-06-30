/*
 * Eval.cs --
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
    /// This class implements the Eagle <c>eval</c> command, which concatenates
    /// its arguments into a single script and evaluates that script in the
    /// current context, returning the result of the evaluation.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("2c86a842-a633-4863-a5d4-14f36f1365ed")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class Eval : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>eval</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Eval(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>eval</c> command.  It builds a script
        /// from its arguments (a single argument is used as-is; multiple
        /// arguments are concatenated), evaluates that script within a new
        /// tracking call frame, and returns the result of the evaluation.
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
        /// command name; elements one and beyond supply the script (or script
        /// fragments) to evaluate.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by evaluating the
        /// script.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" /> when the wrong number of
        /// arguments is supplied, the interpreter is null, the argument list
        /// is null, or the evaluated script fails) with details placed in
        /// <paramref name="result" />.  The control-flow values
        /// <see cref="ReturnCode.Break" />, <see cref="ReturnCode.Continue" />,
        /// and <see cref="ReturnCode.Return" /> may also be propagated from the
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
                    if (arguments.Count >= 2)
                    {
                        string name = StringList.MakeList("eval");

                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                            CallFrameFlags.Evaluate);

                        interpreter.PushAutomaticCallFrame(frame);

                        if (arguments.Count == 2)
                            code = interpreter.EvaluateScript(arguments[1], ref result);
                        else
                            code = interpreter.EvaluateScript(arguments, 1, ref result);

                        if (code == ReturnCode.Error)
                        {
                            /* IGNORED */
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"eval\" body line {1})",
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
                        result = "wrong # args: should be \"eval arg ?arg ...?\"";
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
