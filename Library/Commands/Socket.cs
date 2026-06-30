/*
 * Socket.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Net.Sockets;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>socket</c> command, which opens a
    /// TCP/IP network connection, either as a client connecting to a remote
    /// host and port or, when <c>-server</c> is supplied, as a listening
    /// server that accepts incoming connections.  A successful invocation
    /// registers a channel and returns its identifier.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("2cb67080-894d-4232-a2d9-ae2a65da012e")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("network")]
    internal sealed class Socket : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>socket</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Socket(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>socket</c> command.  It parses the
        /// supported options (for example <c>-server</c>, <c>-myaddr</c>,
        /// <c>-myport</c>, <c>-keepalive</c>, <c>-buffer</c>, and the various
        /// timeout options), then either starts a server socket that listens
        /// for incoming connections or creates a client socket connected to
        /// the given host and port.  On success it registers the resulting
        /// channel with the interpreter and reports its identifier.
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
        /// command name; the remaining elements are the options followed by
        /// the host and port (for a client socket) or the port (for a server
        /// socket).  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the identifier of the newly created
        /// channel.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the channel
        /// identifier placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, an option is invalid, the interpreter or argument
        /// list is null, the channel already exists, or the connection
        /// cannot be established, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 3)
                    {
                        if (interpreter.HasChannels(ref result))
                        {
                            OptionDictionary options =
                                CommandOptions.GetCommandOptions(
                                    CommandOptionType.Socket);

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, true, ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: There must be at least one argument after the options 
                                //       and there can never be more than two.
                                //
                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) >= arguments.Count))
                                {
                                    IVariant value = null;
                                    AddressFamily? addressFamily = null;

                                    if (options.IsPresent("-addressfamily", ref value))
                                        addressFamily = (AddressFamily)value.Value;

                                    bool? keepAlive = Defaults.SocketKeepAlive;

                                    if (options.IsPresent("-keepalive", ref value))
                                        keepAlive = (bool?)value.Value;

                                    string command = null;

                                    if (options.IsPresent("-server", ref value))
                                        command = value.ToString();

                                    string myAddress = null;

                                    if (options.IsPresent("-myaddr", ref value))
                                        myAddress = value.ToString();

                                    string myPort = null;

                                    if (options.IsPresent("-myport", ref value))
                                        myPort = value.ToString();

                                    int buffer = 0;

                                    if (options.IsPresent("-buffer", ref value))
                                        buffer = (int)value.Value;

                                    int? sendTimeout = null;

                                    if (options.IsPresent("-sendtimeout", ref value))
                                        sendTimeout = (int)value.Value;

                                    int? receiveTimeout = null;

                                    if (options.IsPresent("-receivetimeout", ref value))
                                        receiveTimeout = (int)value.Value;

                                    int? availableTimeout = null;

                                    if (options.IsPresent("-availabletimeout", ref value))
                                        availableTimeout = (int)value.Value;

                                    int? readTimeout = null;

                                    if (options.IsPresent("-readtimeout", ref value))
                                        readTimeout = (int)value.Value;

                                    int? writeTimeout = null;

                                    if (options.IsPresent("-writetimeout", ref value))
                                        writeTimeout = (int)value.Value;

                                    TimeoutType timeoutType = TimeoutType.None;

                                    if (options.IsPresent("-timeouttype", ref value))
                                        timeoutType = (TimeoutType)value.Value;

                                    int? timeout = WebOps.GetTimeout(interpreter, timeoutType);

                                    if (options.IsPresent("-timeout", ref value))
                                        timeout = (int)value.Value;

                                    bool asynchronous = false;

                                    if (options.IsPresent("-async"))
                                        asynchronous = true; /* NOT YET IMPLEMENTED */

                                    bool noDelay = false;

                                    if (options.IsPresent("-nodelay"))
                                        noDelay = true;

                                    bool trace = false;

                                    if (options.IsPresent("-trace"))
                                        trace = true;

                                    bool noBuffer = false;

                                    if (options.IsPresent("-nobuffer"))
                                        noBuffer = true;

                                    bool exclusive = true; /* TODO: Good default? */

                                    if (options.IsPresent("-noexclusive"))
                                        exclusive = false;

                                    string channelId = null;

                                    if (options.IsPresent("-channelid", ref value))
                                        channelId = value.ToString();

                                    if ((channelId == null) ||
                                        (interpreter.DoesChannelExist(channelId) != ReturnCode.Ok))
                                    {
                                        if (command != null)
                                        {
                                            if ((argumentIndex + 1) == arguments.Count)
                                            {
                                                if (myPort == null)
                                                {
                                                    StreamFlags streamFlags =
                                                        StreamFlags.ServerSocket;

                                                    if (!noBuffer)
                                                        streamFlags |= StreamFlags.NeedBuffer;

                                                    if (trace)
                                                        streamFlags |= StreamFlags.TraceReadLines;

                                                    code = interpreter.StartServerSocket(
                                                        options, timeout, myAddress,
                                                        arguments[argumentIndex],
                                                        addressFamily, streamFlags,
                                                        availableTimeout, readTimeout,
                                                        writeTimeout, keepAlive, exclusive,
                                                        command, ref result);
                                                }
                                                else
                                                {
                                                    goto wrongNumArgs;
                                                }
                                            }
                                            else
                                            {
                                                goto wrongNumArgs;
                                            }
                                        }
                                        else
                                        {
                                            if ((argumentIndex + 2) == arguments.Count)
                                            {
                                                if (!asynchronous)
                                                {
                                                    TcpClient client = SocketOps.NewTcpClient(
                                                        myAddress, myPort, interpreter.InternalCultureInfo,
                                                        keepAlive, ref addressFamily, ref result);

                                                    if (client != null)
                                                    {
                                                        try
                                                        {
                                                            client.NoDelay = noDelay;

                                                            if (timeout != null)
                                                            {
                                                                client.SendTimeout = (int)timeout;
                                                                client.ReceiveTimeout = (int)timeout;
                                                            }

                                                            if (sendTimeout != null)
                                                                client.SendTimeout = (int)sendTimeout;

                                                            if (receiveTimeout != null)
                                                                client.ReceiveTimeout = (int)receiveTimeout;

                                                            if (buffer != 0)
                                                            {
                                                                client.SendBufferSize = buffer;
                                                                client.ReceiveBufferSize = buffer;
                                                            }

                                                            code = ReturnCode.Ok;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = SocketOps.Connect(
                                                                client, arguments[argumentIndex], arguments[argumentIndex + 1],
                                                                interpreter.InternalCultureInfo, addressFamily, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (channelId == null)
                                                                    channelId = FormatOps.Id("clientSocket", null, interpreter.NextId());

                                                                StreamFlags streamFlags = StreamFlags.ClientSocket;

                                                                if (!noBuffer)
                                                                    streamFlags |= StreamFlags.NeedBuffer;

                                                                if (trace)
                                                                    streamFlags |= StreamFlags.TraceReadLines;

                                                                NetworkStream stream = client.GetStream();

                                                                if (stream != null)
                                                                {
                                                                    if (readTimeout != null)
                                                                        stream.ReadTimeout = (int)readTimeout;

                                                                    if (writeTimeout != null)
                                                                        stream.WriteTimeout = (int)writeTimeout;
                                                                }

                                                                code = interpreter.AddFileOrSocketChannel(
                                                                    channelId, stream, options, streamFlags,
                                                                    availableTimeout, false, false, false,
                                                                    false, new ClientData(client), ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = channelId;
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
                                                    result = "asynchronous sockets are not implemented";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                goto wrongNumArgs;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "can't add \"{0}\": channel already exists",
                                            channelId);

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                    {
                                        result = OptionDictionary.BadOption(
                                            options, arguments[argumentIndex],
                                            !interpreter.InternalIsSafe());

                                        code = ReturnCode.Error;
                                    }
                                    else
                                    {
                                        goto wrongNumArgs;
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
                        goto wrongNumArgs;
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

        wrongNumArgs:
            result = "wrong # args: should be \"socket ?-myaddr addr? ?-myport myport? ?-async? host port\" " + /* SKIP */
                "or \"socket -server command ?-myaddr addr? port\"";

            return ReturnCode.Error;
        }
        #endregion
    }
}
