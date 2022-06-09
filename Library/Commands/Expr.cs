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
    [ObjectId("07c8ff7e-4727-4dbb-8aa9-9d8915e16e61")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("expression")]
    internal sealed class Expr : Core
    {
        public Expr(
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
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (\"expr\" body line {1})",
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
