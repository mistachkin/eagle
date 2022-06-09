/*
 * Invoke.cs --
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
    [ObjectId("359209a1-55b6-4fe9-a4cd-4e4647f84e57")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("engine")]
    internal sealed class Invoke : Core
    {
        public Invoke(
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
                result = "wrong # args: should be \"invoke ?level? cmd ?arg ...?\"";
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
                result = "wrong # args: should be \"invoke ?level? cmd ?arg ...?\"";
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
                    string name = StringList.MakeList("invoke", arguments[1],
                        arguments[argumentIndex]);

                    ICallFrame newFrame = interpreter.NewUplevelCallFrame(
                        name, currentLevel, CallFrameFlags.None, mark,
                        currentFrame, otherFrame);

                    ICallFrame savedFrame = null;

                    interpreter.PushUplevelCallFrame(
                        currentFrame, newFrame, true, ref savedFrame);

                    code = interpreter.Invoke(
                        arguments[argumentIndex], clientData,
                        ArgumentList.GetRange(arguments, argumentIndex),
                        ref result);

                    if (code == ReturnCode.Error)
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format("{0}    (\"invoke\" body line {1})",
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
