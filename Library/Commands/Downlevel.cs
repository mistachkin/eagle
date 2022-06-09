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
    [ObjectId("aa52dc76-1f0e-4a35-9ab9-eb52a7e6416f")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("control")]
    internal sealed class Downlevel : Core
    {
        public Downlevel(
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
                Engine.AddErrorInformation(interpreter, result,
                    String.Format("{0}    (\"downlevel\" body line {1})",
                        Environment.NewLine, Interpreter.GetErrorLine(interpreter)));

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
