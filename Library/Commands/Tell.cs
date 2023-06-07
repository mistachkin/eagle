/*
 * Tell.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("d7e8903f-20d8-48ac-b454-33ab805006f6")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Tell : Core
    {
        public Tell(
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
                    if (arguments.Count == 2)
                    {
                        string channelId = arguments[1];
                        IChannel channel = interpreter.InternalGetChannel(channelId, ref result);
                        
                        if (channel != null)
                        {
                            try
                            {
                                if (channel.CanSeek)
                                    result = channel.Position;
                                else
                                    result = _Position.Invalid; // COMPAT: Tcl.
                            }
                            catch (Exception e)
                            {
                                Engine.SetExceptionErrorCode(interpreter, e);

                                result = e;
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
                        result = "wrong # args: should be \"tell channelId\"";
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
