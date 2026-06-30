/*
 * Downlevel.cs --
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
    /// This class implements the Eagle <c>downlevel</c> command, which
    /// evaluates a script body in a freshly pushed "downlevel" call frame and
    /// then pops that frame (along with any intervening scope frames left
    /// open) before returning.  See <c>core_language.md</c> for the command
    /// syntax and semantics.
    /// </summary>
    [ObjectId("aa52dc76-1f0e-4a35-9ab9-eb52a7e6416f")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("control")]
    internal sealed class Downlevel : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>downlevel</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Downlevel(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>downlevel</c> command.  It pushes a new
        /// downlevel call frame, evaluates the supplied script body (either a
        /// single argument or the concatenation of the remaining arguments) in
        /// that frame, and then pops the pushed frame and any leftover scope
        /// frames before returning the outcome of the evaluation.
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
        /// command name; the remaining elements supply the script body to be
        /// evaluated in the downlevel call frame.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of evaluating the script
        /// body.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the script body evaluates
        /// successfully; otherwise, a non-Ok value (e.g.
        /// <see cref="ReturnCode.Error" /> when the interpreter is null, the
        /// argument list is null, the wrong number of arguments is supplied, or
        /// the script body itself fails) with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                result = "wrong # args: should be \"downlevel arg ?arg ...?\"";
                return ReturnCode.Error;
            }

            string name = StringList.MakeList("downlevel");
            ICallFrame frame = interpreter.NewDownlevelCallFrame(name);

            interpreter.PushAutomaticCallFrame(frame);

            ReturnCode code;

            if (arguments.Count == 2)
                code = interpreter.EvaluateScript(arguments[1], ref result);
            else
                code = interpreter.EvaluateScript(arguments, 1, ref result);

            if (code == ReturnCode.Error)
            {
                /* IGNORED */
                Engine.AddErrorInformation(interpreter, result,
                    String.Format("{0}    (\"downlevel\" body line {1})",
                        Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
            }

            //
            // NOTE: Pop the original call frame that we pushed above and
            //       any intervening scope call frames that may be leftover
            //       (i.e. they were not explicitly closed).
            //
            /* IGNORED */
            interpreter.PopScopeCallFramesAndOneMore();

            return code;
        }
        #endregion
    }
}
