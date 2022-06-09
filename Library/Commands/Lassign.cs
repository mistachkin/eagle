/*
 * Lassign.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("b19de186-5d3e-4543-82fe-2d3b9355dc4a")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lassign : Core
    {
        public Lassign(
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
                        StringList list = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], true, ref list, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int argumentIndex;

                            for (argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                            {
                                string value;

                                if ((argumentIndex - 2) < list.Count)
                                    value = list[argumentIndex - 2];
                                else
                                    value = String.Empty;

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, arguments[argumentIndex],
                                    value, null, ref result);

                                if (code != ReturnCode.Ok)
                                    break;
                            }

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex - 2) < list.Count)
                                    result = StringList.GetRange(list, argumentIndex - 2);
                                else
                                    result = String.Empty;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lassign list varName ?varName ...?\"";
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
