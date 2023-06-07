/*
 * Flush.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("5a54aacb-f9b0-4b26-9f72-44ae4aa82441")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Flush : Core
    {
        public Flush(
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
                            //
                            // BUGFIX: If the channel has no writer, make sure it
                            //         will have one before the Flush method call.
                            //
                            if (!channel.HasWriter)
                                /* IGNORED */
                                channel.GetStreamWriter();

                            try
                            {
                                if (channel.Flush())
                                {
                                    result = String.Empty;
                                }
                                else
                                {
                                    result = String.Format(
                                        "channel \"{0}\" wasn't opened for writing",
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
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"flush channelId\"";
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
