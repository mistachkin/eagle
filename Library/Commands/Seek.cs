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
    /// <summary>
    /// This class implements the Eagle <c>seek</c> command, which changes the
    /// current access position of an open channel by an offset relative to a
    /// specified origin (the start, the current position, or the end).  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("c54bfc27-25d6-4467-9d26-a7868236196e")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Seek : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>seek</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Seek(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>seek</c> command.  It looks up the
        /// channel named by the first argument and repositions its access
        /// point to the given offset relative to the requested origin
        /// (defaulting to the start of the channel when no origin is
        /// supplied).
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
        /// command name; element one is the channel identifier; element two is
        /// the integer offset; an optional element three is the origin (one of
        /// <c>start</c>, <c>current</c>, or <c>end</c>).  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the channel is repositioned
        /// successfully; otherwise, <see cref="ReturnCode.Error" /> when the
        /// wrong number of arguments is supplied, the channel cannot be found,
        /// the offset or origin is invalid, the channel does not support
        /// seeking, the seek operation throws an exception, the interpreter is
        /// null, or the argument list is null, with details placed in
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
                    if ((arguments.Count == 3) || (arguments.Count == 4))
                    {
                        string channelId = arguments[1];
                        IChannel channel = interpreter.InternalGetChannel(channelId, ref result);
                        
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
