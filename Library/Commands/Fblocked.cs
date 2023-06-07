/*
 * Fblocked.cs --
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
    [ObjectId("bc243857-822c-41ed-b6f3-32c17530665e")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Fblocked : Core
    {
        public Fblocked(
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

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"fblocked channelId\"";
                return ReturnCode.Error;
            }

            string channelId = arguments[1];
            IChannel channel = interpreter.InternalGetChannel(channelId, ref result);

            if (channel == null)
                return ReturnCode.Error;

            try
            {
                if (channel.IsNetworkStream)
                    result = !channel.DataAvailable;
                else
                    result = false;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                result = e;
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
