/*
 * Close.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>close</c> command, which closes a
    /// previously opened channel (for example one created by <c>open</c> or
    /// <c>socket</c>), removing it from the interpreter and releasing its
    /// underlying resources.  See <c>core_language.md</c> for the command
    /// syntax and semantics.
    /// </summary>
    [ObjectId("0f2baa09-c2f7-4241-8435-5e407b7243d2")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Close : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>close</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Close(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>close</c> command.  It accepts the
        /// identifier of a channel to close, removes that channel from the
        /// interpreter, and releases the resources associated with it.
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
        /// command name; element one supplies the identifier of the channel to
        /// close.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced while closing the
        /// channel (typically an empty string).  Upon failure, this contains
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the channel is closed successfully;
        /// otherwise, <see cref="ReturnCode.Error" /> when the wrong number of
        /// arguments is supplied, the interpreter is null, the argument list is
        /// null, or the channel cannot be removed, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == 2)
                    {
                        code = interpreter.RemoveChannel(
                            arguments[1], ChannelType.None, false, true, true,
                            ref result);
                    }
                    else
                    {
                        result = "wrong # args: should be \"close channelId\"";
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
