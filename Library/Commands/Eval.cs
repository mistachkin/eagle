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
    [ObjectId("2c86a842-a633-4863-a5d4-14f36f1365ed")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class Eval : Core
    {
        public Eval(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"eval\" body line {1})",
                                    Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

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
