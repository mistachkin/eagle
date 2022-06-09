/*
 * Upvar.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("399949c6-da0d-4061-bf14-04fcbc8a8c65")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Upvar : Core
    {
        public Upvar(
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
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 3)
                    {
                        ICallFrame otherFrame = null;

                        FrameResult frameResult = interpreter.GetCallFrame(
                            arguments[1], ref otherFrame, ref result);

                        if (frameResult != FrameResult.Invalid)
                        {
                            int count = arguments.Count - ((int)frameResult + 1);

                            if ((count & 1) == 0)
                            {
                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    ICallFrame localFrame = null;

                                    code = interpreter.GetVariableFrameViaResolvers(
                                        LookupFlags.Default, ref localFrame, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        int argumentIndex = ((int)frameResult + 1); // skip "upvar ?level?"

                                        for (; count > 0; count -= 2, argumentIndex += 2)
                                        {
                                            string otherName = arguments[argumentIndex];
                                            string localName = arguments[argumentIndex + 1];

                                            code = ScriptOps.LinkVariable(
                                                interpreter, localFrame, localName,
                                                otherFrame, otherName, ref result);

                                            if (code != ReturnCode.Ok)
                                                break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                result = "wrong # args: should be \"upvar ?level? otherVar localVar ?otherVar localVar ...?\"";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"upvar ?level? otherVar localVar ?otherVar localVar ...?\"";
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
