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
    /// <summary>
    /// This class implements the Eagle <c>fblocked</c> command, which reports
    /// whether the most recent input operation on a channel returned less
    /// data than requested because the channel would otherwise have blocked.
    /// See <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("bc243857-822c-41ed-b6f3-32c17530665e")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Fblocked : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>fblocked</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Fblocked(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>fblocked</c> command.  It looks up the
        /// channel named by its single argument and returns a boolean
        /// indicating whether the channel is currently blocked (i.e. a network
        /// stream that has no data available); non-network channels are always
        /// reported as not blocked.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; element one is the channel identifier to query.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains a boolean indicating whether the named
        /// channel is blocked.  Upon failure, this contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the blocked status
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null,
        /// the channel cannot be found, or an exception occurs, with details
        /// placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
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
