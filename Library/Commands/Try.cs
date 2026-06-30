/*
 * Try.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>try</c> command, which evaluates a
    /// body script and, optionally, a <c>finally</c> block that is always
    /// evaluated afterward regardless of how the body completes.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("3bc552ff-e29e-4855-a208-30517268c60d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("control")]
    internal sealed class Try : Core
    {
        /// <summary>
        /// The literal keyword that must precede the optional finally block
        /// script in the argument list.
        /// </summary>
        private const string Finally = "finally";

        /// <summary>
        /// Constructs an instance of the <c>try</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Try(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>try</c> command.  It evaluates the body
        /// script within a tracking call frame and, when a <c>finally</c>
        /// block is supplied, always evaluates that block afterward, honoring
        /// the interpreter flags that govern cancel, exit, and timeout
        /// handling during the finally block.  The overall result and return
        /// code are normally those of the body, unless the finally block
        /// fails, in which case they are those of the finally block.
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
        /// two must be the literal <c>finally</c> keyword followed by element
        /// three, the finally block script.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the body script (or the
        /// finally block, when applicable).  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The return code of the body script when the finally block (if any)
        /// succeeds; otherwise, the return code of the finally block.
        /// <see cref="ReturnCode.Error" /> is returned when the wrong number
        /// of arguments is supplied, the third argument is not the literal
        /// <c>finally</c> keyword, the interpreter is null, or the argument
        /// list is null, with details placed in <paramref name="result" />.
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
                    // try {<tryBody>} 
                    // [finally {<finallyBody>}]
                    //
                    if ((arguments.Count == 2) || (arguments.Count == 4))
                    {
                        if ((arguments.Count < 3) ||
                            SharedStringOps.SystemEquals(arguments[2], Try.Finally))
                        {
                            string name = StringList.MakeList("try");

                            ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                CallFrameFlags.Try);

                            interpreter.PushAutomaticCallFrame(frame);

                            ReturnCode tryCode;
                            Result tryResult = null;

                            tryCode = interpreter.EvaluateScript(arguments[1], ref tryResult);

                            if (tryCode == ReturnCode.Error)
                            {
                                /* IGNORED */
                                Engine.AddErrorInformation(interpreter, tryResult,
                                    String.Format("{0}    (\"try\" body line {1})",
                                        Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                            }

                            //
                            // NOTE: Pop the original call frame that we pushed above and 
                            //       any intervening scope call frames that may be leftover 
                            //       (i.e. they were not explicitly closed).
                            //
                            /* IGNORED */
                            interpreter.PopScopeCallFramesAndOneMore();

                            Result finallyResult = null;
                            ReturnCode finallyCode = ReturnCode.Ok;

                            if (arguments.Count == 4)
                            {
                                name = StringList.MakeList("finally");

                                frame = interpreter.NewTrackingCallFrame(name,
                                    CallFrameFlags.Finally);

                                interpreter.PushAutomaticCallFrame(frame);

                                //
                                // BUGFIX: Preserve any and all existing error related 
                                //         information during evaluation of the finally 
                                //         block.
                                //
                                Engine.SetNoResetError(interpreter, true);

                                //
                                // NOTE: If there was an error during the try block as well,
                                //       keep them somewhat organized in the final error 
                                //       information.
                                //
                                if (tryCode == ReturnCode.Error)
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, null,
                                        String.Format("{0}    ... continued ...",
                                            Environment.NewLine));
                                }

                                //
                                // NOTE: If the appropriate flag is set, call into the
                                //       Engine.ResetCancel method (with "force" enabled)
                                //       prior to evaluating the finally block script.
                                //       It should be noted here that even though the
                                //       return code of this call is checked by the code,
                                //       it basically cannot fail at this point.
                                //
                                Result canceledResult = null;
                                bool canceled = false;
                                bool unwound = false;
                                bool resetCancel = false;

                                //
                                // NOTE: If the appropriate flag is set, reset the Exit
                                //       property prior to evaluating the finally block
                                //       script.
                                //
                                bool exit = false;
                                bool resetExit = false;

                                try
                                {
                                    if (ScriptOps.HasFlags(interpreter,
                                            InterpreterFlags.FinallyResetCancel, true))
                                    {
                                        ReturnCode resetCode;
                                        Result resetError = null;

                                        resetCode = Engine.ResetCancel(
                                            interpreter, CancelFlags.TryBlock, ref canceledResult,
                                            ref canceled, ref unwound, ref resetCancel,
                                            ref resetError);

                                        if (resetCode != ReturnCode.Ok)
                                            DebugOps.Complain(interpreter, resetCode, resetError);
                                    }

                                    if (ScriptOps.HasFlags(interpreter,
                                            InterpreterFlags.FinallyResetExit, true))
                                    {
                                        exit = interpreter.ExitNoThrow;

                                        if (exit)
                                        {
                                            interpreter.ExitNoThrow = false;
                                            resetExit = true;
                                        }
                                    }

                                    ReturnCode timeoutCode;
                                    Thread timeoutThread = null;
                                    Result timeoutResult = null;

                                    timeoutCode = interpreter.StartFinallyTimeoutThread(
                                        null, TimeoutFlags.FinallyTimeout, false,
                                        ref timeoutThread, ref timeoutResult);

                                    if (timeoutCode != ReturnCode.Ok)
                                        DebugOps.Complain(interpreter, timeoutCode, timeoutResult);

                                    try
                                    {
                                        //
                                        // NOTE: Evaluate the finally block.
                                        //
                                        finallyCode = interpreter.EvaluateFinallyScript(
                                            arguments[3], ref finallyResult);
                                    }
                                    finally
                                    {
                                        timeoutCode = interpreter.InterruptFinallyTimeoutThread(
                                            timeoutThread, null, interpreter.InternalNoThreadAbort,
                                            false, ref timeoutResult);

                                        if (timeoutCode != ReturnCode.Ok)
                                            DebugOps.Complain(interpreter, timeoutCode, timeoutResult);
                                    }
                                }
                                finally
                                {
                                    if (exit && resetExit)
                                    {
                                        if (ScriptOps.HasFlags(interpreter,
                                                InterpreterFlags.FinallyRestoreExit, true))
                                        {
                                            interpreter.ExitNoThrow = true;
                                        }
                                    }

                                    if ((canceled || unwound) && resetCancel)
                                    {
                                        if (ScriptOps.HasFlags(interpreter,
                                                InterpreterFlags.FinallyRestoreCancel, true))
                                        {
                                            CancelFlags cancelFlags = CancelFlags.FinallyBlock;

                                            if (unwound)
                                                cancelFlags |= CancelFlags.Unwind;

                                            ReturnCode cancelCode;
                                            Result cancelError = null;

                                            cancelCode = Engine.CancelEvaluate(
                                                interpreter, canceledResult, cancelFlags,
                                                ref cancelError);

                                            if (cancelCode != ReturnCode.Ok)
                                                DebugOps.Complain(interpreter, cancelCode, cancelError);
                                        }
                                    }
                                }

                                if (finallyCode == ReturnCode.Error)
                                {
                                    /* IGNORED */
                                    Engine.AddErrorInformation(interpreter, finallyResult,
                                        String.Format("{0}    (\"finally\" body line {1})",
                                            Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                                }

                                //
                                // NOTE: Restore normal result reset semantics.
                                //
                                Engine.SetNoResetError(interpreter, false);

                                //
                                // NOTE: Pop the original call frame that we pushed above and 
                                //       any intervening scope call frames that may be leftover 
                                //       (i.e. they were not explicitly closed).
                                //
                                /* IGNORED */
                                interpreter.PopScopeCallFramesAndOneMore();
                            }

                            //
                            // NOTE: Initially, the overall command return code and result 
                            //       is that of the try block; however, if the finally block 
                            //       fails, that will be the return code and result.
                            //
                            if (finallyCode == ReturnCode.Ok)
                            {
                                result = tryResult;
                                code = tryCode;
                            }
                            else
                            {
                                result = finallyResult;
                                code = finallyCode;
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "expected \"finally\" but got \"{0}\"",
                                arguments[2]);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"try script ?finally script?\"";
                        code = ReturnCode.Error;
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
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
