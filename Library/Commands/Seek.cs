/*
 * Seek.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("c54bfc27-25d6-4467-9d26-a7868236196e")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Seek : Core
    {
        public Seek(
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
                    if ((arguments.Count == 3) || (arguments.Count == 4))
                    {
                        string channelId = arguments[1];
                        IChannel channel = interpreter.GetChannel(channelId, ref result);
                        
                        if (channel != null)
                        {
                            long offset = 0;

                            code = Value.GetWideInteger2(
                                (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                interpreter.InternalCultureInfo, ref offset, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                SeekOrigin origin = SeekOrigin.Begin;

                                if (arguments.Count >= 4)
                                {
                                    object enumValue = EnumOps.TryParse(
                                        typeof(MapSeekOrigin), arguments[3],
                                        true, true);

                                    if (enumValue is MapSeekOrigin)
                                    {
                                        origin = (SeekOrigin)enumValue;
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "bad origin \"{0}\": must be start, current, or end", 
                                            arguments[3]);

                                        code = ReturnCode.Error;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    try
                                    {
                                        if (channel.CanSeek)
                                        {
                                            channel.Seek(offset, origin);
                                            result = String.Empty;
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "error during seek on \"{0}\": invalid argument", 
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
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"seek channelId offset ?origin?\"";
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
