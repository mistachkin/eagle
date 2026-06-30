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
    /// <summary>
    /// This class implements the Eagle <c>truncate</c> command, which sets the
    /// length of the file backing a seekable, writable channel, optionally to
    /// an explicit length and otherwise to the current channel position.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("ad284000-7b31-4291-9bb5-650e0816e5b4")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("channel")]
    internal sealed class Truncate : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>truncate</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Truncate(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>truncate</c> command.  It resolves the
        /// named channel and sets the length of its underlying file, using the
        /// optional length argument when supplied or the current channel
        /// position otherwise.
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
        /// command name; element one is the channel identifier; an optional
        /// element two supplies the desired length.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, the
        /// channel cannot be resolved, the length cannot be parsed, the channel
        /// is not both seekable and writable, or an exception occurs while
        /// setting the length, with details placed in
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
