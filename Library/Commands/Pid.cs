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
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>pid</c> command, which returns the
    /// process identifier of the current process or, optionally, information
    /// associated with a specified channel.  See <c>core_language.md</c> for
    /// the command syntax and semantics.
    /// </summary>
    [ObjectId("0c787fdb-c8ee-4bee-b4a0-08cb212e4db6")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Pid : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>pid</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Pid(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>pid</c> command.  With no extra
        /// argument, it returns the process identifier of the current
        /// process.  When the literal <c>previous</c> is supplied, it returns
        /// the previous process identifier tracked by the interpreter.  When a
        /// channel identifier is supplied, the channel is validated and an
        /// empty string is returned, since Unix-style command pipelines are
        /// not supported.
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
        /// command name; an optional element one supplies either the literal
        /// <c>previous</c> or a channel identifier.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the process identifier, the previous
        /// process identifier, or an empty string (for a channel identifier).
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the requested
        /// identifier placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the specified channel cannot be resolved, the
        /// interpreter is null, or the argument list is null, with details
        /// placed in <paramref name="result" />.
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
                    if ((arguments.Count == 1) || (arguments.Count == 2))
                    {
                        if (arguments.Count == 2)
                        {
                            string channelId = arguments[1];

                            if (SharedStringOps.SystemEquals(channelId, "previous"))
                            {
                                result = interpreter.PreviousProcessId;
                                code = ReturnCode.Ok;
                            }
                            else
                            {
                                IChannel channel = interpreter.InternalGetChannel(
                                    channelId, ref result);

                                if (channel != null)
                                {
                                    //
                                    // STUB: This does not work like native Tcl
                                    //       because we do not support Unix-style
                                    //       command pipelines.
                                    //
                                    result = String.Empty;
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    code = ReturnCode.Error;
                                }
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
