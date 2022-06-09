/*
 * Incr.cs --
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
    [ObjectId("e620e38f-fe66-42ca-8889-3bcb0db3d62c")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("expression")]
    internal sealed class Incr : Core
    {
        public Incr(
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
                    if ((arguments.Count == 2) || (arguments.Count == 3))
                    {
                        long increment = 1;

                        if (arguments.Count == 3)
                        {
                            code = Value.GetWideInteger2(
                                (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                interpreter.InternalCultureInfo, ref increment, ref result);

                            //
                            // NOTE: Replicate "odd" Tcl behavior regarding error reporting
                            //       for converting the increment value.
                            //
                            if (code == ReturnCode.Error)
                                Engine.AddErrorInformation(interpreter, result,
                                    String.Format("{0}    (reading increment)",
                                        Environment.NewLine));
                        }

                        if (code == ReturnCode.Ok)
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                string name = arguments[1];
                                long value = 0;

                                code = interpreter.GetIntegerVariableValue(VariableFlags.None,
                                    name, ref value, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    value += increment;

                                    code = interpreter.SetIntegerVariableValue(VariableFlags.None,
                                        name, value, ref result);

                                    if (code == ReturnCode.Ok)
                                        result = value;
                                }
                                else if (code == ReturnCode.Error)
                                {
                                    //
                                    // NOTE: Replicate "odd" Tcl behavior regarding error reporting for
                                    //       converting the value to be incremented.
                                    //
                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (reading value of variable to increment)",
                                            Environment.NewLine));
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"incr varName ?increment?\"";
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
