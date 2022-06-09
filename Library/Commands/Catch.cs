/*
 * Catch.cs --
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

namespace Eagle._Commands
{
    [ObjectId("382b9037-4351-47c7-a9a6-e66bcbfe284d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("control")]
    internal sealed class Catch : Core
    {
        public Catch(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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

            if ((arguments.Count < 2) || (arguments.Count > 4))
            {
                result = ScriptOps.WrongNumberOfArguments(this, 1,
                    arguments, "script ?resultVarName? ?optionsVarName?");

                return ReturnCode.Error;
            }

            string name = StringList.MakeList(this.Name);

            ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                CallFrameFlags.Catch);

            interpreter.PushAutomaticCallFrame(frame);

            EngineFlags engineFlags = interpreter.EngineFlags;

            if (EngineFlagOps.HasNoResetError(engineFlags))
                engineFlags |= EngineFlags.ErrorAlreadyLogged;

            ReturnCode code;
            Result localResult = null;
            int errorLine = 0;

            /* IGNORED */
            interpreter.EnterCatchLevel();

            try
            {
                int savedPreviousLevels = interpreter.BeginNestedExecution();

                try
                {
                    code = interpreter.EvaluateScript(
                        arguments[1], engineFlags, ref localResult,
                        ref errorLine);
                }
                finally
                {
                    interpreter.EndNestedExecution(savedPreviousLevels);
                }
            }
            finally
            {
                if (interpreter.ExitCatchLevel() == 0)
                {
                    if (ScriptOps.HasFlags(interpreter,
                            InterpreterFlags.CatchResetCancel, true))
                    {
                        CancelFlags cancelFlags = CancelFlags.CatchBlock;

                        if (ScriptOps.HasFlags(interpreter,
                                InterpreterFlags.CatchResetGlobalCancel, true))
                        {
                            cancelFlags |= CancelFlags.Global;
                        }

                        ReturnCode resetCode;
                        Result resetError = null;

                        resetCode = Engine.ResetCancel(
                            interpreter, cancelFlags, ref resetError);

                        if (resetCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, resetCode, resetError);
                        }
                    }

                    if (ScriptOps.HasFlags(interpreter,
                            InterpreterFlags.CatchResetExit, true))
                    {
                        bool exit = interpreter.ExitNoThrow;

                        if (exit)
                            interpreter.ExitNoThrow = false;
                    }
                }
            }

            //
            // BUGFIX: Prevent messing up the custom errorInfo from the [error]
            //         command by checking to see if the "error already logged"
            //         flag has been set.
            //
            if (code == ReturnCode.Error)
            {
                engineFlags = interpreter.EngineFlags;

                if (!EngineFlagOps.HasErrorAlreadyLogged(engineFlags) &&
                    !EngineFlagOps.HasNoResetError(engineFlags))
                {
                    Engine.AddErrorInformation(interpreter, localResult,
                        String.Format("{0}    (\"catch\" body line {1})",
                            Environment.NewLine, errorLine));
                }
            }

            //
            // NOTE: Pop the original call frame that we pushed above and any
            //       intervening scope call frames that may be leftover (i.e.
            //       they were not explicitly closed).
            //
            /* IGNORED */
            interpreter.PopScopeCallFramesAndOneMore();

            //
            // NOTE: The result of this command is the integer conversion of
            //       the return code received from the evaluated script.
            //
            Engine.ResetResult(interpreter, ref result);
            result = ConversionOps.ToInt(code);
            result.ReturnCode = code; /* NOTE: For ease of use. */

            //
            // NOTE: See if the caller wants to save the result and/or error
            //       message in a variable.
            //
            if (arguments.Count >= 3)
            {
                Result error = null;

                code = interpreter.SetVariableValue2(
                    VariableFlags.NoReady, arguments[2], (localResult != null) ?
                        localResult.Value : null, null, ref error);

                if (code != ReturnCode.Ok)
                {
                    Engine.ResetResult(interpreter, ref result);

                    result = String.Format(
                        "couldn't save command result in variable: {0}",
                        error);

                    return code;
                }
            }

            //
            // NOTE: See if the caller wants to save the "return options" in a
            //       variable.
            //
            if (arguments.Count >= 4)
            {
                StringList list = new StringList();
                Result error = null;

                if (result.ReturnCode == ReturnCode.Return)
                {
                    int level = 0;

                    code = interpreter.GetInfoLevel(
                        null, ref level, ref error);

                    if (code != ReturnCode.Ok)
                    {
                        Engine.ResetResult(interpreter, ref error);

                        result = String.Format(
                            "couldn't get current level: {0}", error);

                        return code;
                    }

                    list.Add("-code",
                        ((int)interpreter.ReturnCode).ToString());

                    list.Add("-level", level.ToString());
                }
                else
                {
                    list.Add("-code", result.String);
                    list.Add("-level", Value.ZeroString);
                }

                if (result.ReturnCode == ReturnCode.Error)
                {
                    Result errorCode = null;
                    Result errorInfo = null;
                    ResultList errors = null;

                    code = interpreter.InternalCopyErrorInformation(
                        VariableFlags.None, true, ref errorCode, ref errorInfo,
                        ref errors);

                    if (code != ReturnCode.Ok)
                    {
                        Engine.ResetResult(interpreter, ref result);

                        result = errors;
                        return code;
                    }

                    list.Add("-errorcode", errorCode);
                    list.Add("-errorinfo", errorInfo);
                    list.Add("-errorline", errorLine.ToString());
                }

                code = interpreter.SetVariableValue(
                    VariableFlags.NoReady, arguments[3], list.ToString(), null,
                    ref error);

                if (code != ReturnCode.Ok)
                {
                    Engine.ResetResult(interpreter, ref result);

                    result = String.Format(
                        "couldn't save return options in variable: {0}",
                        error);

                    return code;
                }
            }

            //
            // NOTE: We are "catching" (masking) the error; therefore, do not
            //       propogate it.
            //
            return ReturnCode.Ok;
        }
        #endregion
    }
}
