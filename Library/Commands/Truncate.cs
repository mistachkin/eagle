/*
 * Truncate.cs --
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
    [ObjectId("ad284000-7b31-4291-9bb5-650e0816e5b4")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("channel")]
    internal sealed class Truncate : Core
    {
        public Truncate(
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
                        string channelId = arguments[1];
                        IChannel channel = interpreter.InternalGetChannel(channelId, ref result);
                        
                        if (channel != null)
                        {
                            long length = Length.Invalid;

                            if (arguments.Count == 3)
                            {
                                code = Value.GetWideInteger2(
                                    (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                    interpreter.InternalCultureInfo, ref length, ref result);
                            }

                            if (code == ReturnCode.Ok)
                            {
                                try
                                {
                                    if (channel.CanSeek && channel.CanWrite)
                                    {
                                        if (length == Length.Invalid)
                                            //
                                            // NOTE: Length not specified, "truncate" at current position.
                                            //
                                            length = channel.Position + 1;

                                        channel.SetLength(length);
                                        result = String.Empty;
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "error during truncate on \"{0}\": invalid argument", 
                                            channelId);

                                        code = ReturnCode.Error;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Engine.SetExceptionErrorCode(interpreter, e);

                                    result = e;
                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"truncate channelId ?length?\"";
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
