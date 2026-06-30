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
    /// <summary>
    /// This class implements the Eagle <c>invoke</c> command, which evaluates
    /// a command in the variable context of another call frame (identified by
    /// an optional level), similar to <c>uplevel</c> but invoking a single
    /// command with its arguments rather than a script body.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("359209a1-55b6-4fe9-a4cd-4e4647f84e57")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("engine")]
    internal sealed class Invoke : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>invoke</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Invoke(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>invoke</c> command.  It resolves the
        /// optional target call frame level, marks any intervening call
        /// frames as necessary, pushes an uplevel call frame, and invokes the
        /// specified command with its arguments in that variable context,
        /// restoring the call stack afterward.
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
        /// command name; an optional element one supplies the call frame
        /// level, followed by the command name to invoke and any arguments to
        /// pass to it.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the invoked
        /// command.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// such as <see cref="ReturnCode.Error" /> when the interpreter is
        /// null, the argument list is null, the wrong number of arguments is
        /// supplied, the target call frame cannot be resolved, or the invoked
        /// command itself fails, with details placed in
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
                    {
                        /* IGNORED */
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format("{0}    (\"invoke\" body line {1})",
                                Environment.NewLine, Interpreter.GetErrorLine(interpreter)));
                    }

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
