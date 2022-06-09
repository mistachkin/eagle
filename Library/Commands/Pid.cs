/*
 * Pid.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("0c787fdb-c8ee-4bee-b4a0-08cb212e4db6")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Pid : Core
    {
        public Pid(
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
                    if ((arguments.Count == 1) || (arguments.Count == 2))
                    {
                        if (arguments.Count == 2)
                        {
                            string channelId = arguments[1];
                            IChannel channel = interpreter.GetChannel(channelId, ref result);

                            if (channel != null)
                            {
                                //
                                // STUB: This does not actually work.
                                //
                                result = String.Empty; 
                                code = ReturnCode.Ok;
                            }
                            else
                            {
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = ProcessOps.GetId();
                            code = ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"pid ?channelId?\"";
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
