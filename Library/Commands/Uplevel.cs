/*
 * Uplevel.cs --
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
    [ObjectId("bafa3e2b-b26c-4552-9365-f9089d757e33")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("control")]
    internal sealed class Uplevel : Core
    {
        public Uplevel(
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

            if (arguments.Count < 2)
            {
                result = "wrong # args: should be \"uplevel ?level? arg ?arg ...?\"";
                return ReturnCode.Error;
            }

            ReturnCode code;
            int currentLevel = 0;

            code = interpreter.GetInfoLevel(
                CallFrameOps.InfoLevelSubCommand, ref currentLevel,
                ref result);

            if (code != ReturnCode.Ok)
                return code;

            bool mark = false;
            bool absolute = false;
            bool super = false;
            int level = 0;
            ICallFrame currentFrame = null;
            ICallFrame otherFrame = null;

            FrameResult frameResult = interpreter.GetCallFrame(
                arguments[1], ref mark, ref absolute, ref super,
                ref level, ref currentFrame, ref otherFrame,
                ref result);

            if (frameResult == FrameResult.Invalid)
                return ReturnCode.Error;

            int argumentIndex = ((int)frameResult + 1);

            //
            // BUGFIX: The argument count needs to be checked again here.
            //
            if (argumentIndex >= arguments.Count)
            {
                result = "wrong # args: should be \"uplevel ?level? arg ?arg ...?\"";
                return ReturnCode.Error;
            }

            if (mark)
            {
                code = CallFrameOps.MarkMatching(
                    interpreter.CallStack, interpreter.CurrentFrame,
                    absolute, level, CallFrameFlags.Variables,
                    CallFrameFlags.Invisible | CallFrameFlags.NoVariables,
                    CallFrameFlags.Invisible, false, false, true,
                    ref result);
            }

            if (code == ReturnCode.Ok)
            {
                try
                {
                    string name = StringList.MakeList("uplevel", arguments[1]);

                    ICallFrame newFrame = interpreter.NewUplevelCallFrame(
                        name, currentLevel, CallFrameFlags.None, mark,
                        currentFrame, otherFrame);

                    ICallFrame savedFrame = null;

                    interpreter.PushUplevelCallFrame(
                        currentFrame, newFrame, true, ref savedFrame);

                    if ((argumentIndex + 1) >= arguments.Count)
                        code = interpreter.EvaluateScript(
                            arguments[argumentIndex], ref result);
                    else
                        code = interpreter.EvaluateScript(
                            arguments, argumentIndex, ref result);

                    if (code == ReturnCode.Error)
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format("{0}    (\"uplevel\" body line {1})",
                                Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

                    //
                    // NOTE: Pop the original call frame that we pushed above and
                    //       any intervening scope call frames that may be leftover
                    //       (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopUplevelCallFrame(
                        currentFrame, newFrame, ref savedFrame);
                }
                finally
                {
                    if (mark)
                    {
                        //
                        // NOTE: We should not get an error at this point from
                        //       unmarking the call frames; however, if we do get
                        //       one, we need to complain loudly about it because
                        //       that means the interpreter state has probably been
                        //       corrupted somehow.
                        //
                        ReturnCode markCode;
                        Result markResult = null;

                        markCode = CallFrameOps.MarkMatching(
                            interpreter.CallStack, interpreter.CurrentFrame,
                            absolute, level, CallFrameFlags.Variables,
                            CallFrameFlags.NoVariables, CallFrameFlags.Invisible,
                            false, false, false, ref markResult);

                        if (markCode != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, markCode, markResult);
                    }
                }
            }

            return code;
        }
        #endregion
    }
}
