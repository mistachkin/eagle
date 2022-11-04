/*
 * SocketOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("71b14766-48a0-45d5-9254-640fde03509d")]
    internal static class SocketOps
    {
        #region Private Constants
        //
        // HACK: These are no longer read-only.
        //
        private static int? MinimumSocketPollTimeout = 500; /* microseconds */
        private static int? MaximumSocketPollTimeout = null; /* microseconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static PropertyInfo networkStreamSocket;
        private static PropertyInfo tcpListenerActive;
        private static PropertyInfo socketCleanedUp;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this is non-zero, any attempt to create a WebClient via
        //       this class will fail, preventing any network access using
        //       the WebClient class.
        //
        private static int offlineLevels = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Introspection Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (networkStreamSocket != null))
                {
                    localList.Add("NetworkStreamSocket",
                        FormatOps.MemberName(networkStreamSocket));
                }

                if (empty || (tcpListenerActive != null))
                {
                    localList.Add("TcpListenerActive",
                        FormatOps.MemberName(tcpListenerActive));
                }

                if (empty || (socketCleanedUp != null))
                {
                    localList.Add("SocketCleanedUp",
                        FormatOps.MemberName(socketCleanedUp));
                }

                if (empty || (offlineLevels != 0))
                    localList.Add("OfflineLevels", offlineLevels.ToString());

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Socket Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Diagnostic Methods
        public static ReturnCode Ping(
            string hostNameOrAddress,
            int timeout,
            ref IPStatus status,
            ref long roundtripTime,
            ref Result error
            )
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(
                        hostNameOrAddress, timeout); /* throw */

                    status = reply.Status;
                    roundtripTime = reply.RoundtripTime;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Network Client Methods
        public static TcpClient NewTcpClient(
            string hostNameOrAddress,
            string portNameOrNumber,
            CultureInfo cultureInfo,
            AddressFamily addressFamily,
            ref Result error
            )
        {
            IPAddress address = GetIpAddress(
                hostNameOrAddress, addressFamily, false, ref error);

            if (address == null)
                return null;

            int port = GetPortNumber(
                portNameOrNumber, cultureInfo, false, ref error);

            if (port == Port.Invalid)
                return null;

            return new TcpClient(new IPEndPoint(address, port));
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Connect(
            TcpClient client,
            string hostNameOrAddress,
            string portNameOrNumber,
            CultureInfo cultureInfo,
            AddressFamily addressFamily,
            ref Result error
            )
        {
            IPAddress address = GetIpAddress(
                hostNameOrAddress, addressFamily, true, ref error);

            if (address == null)
                return ReturnCode.Error;

            int port = GetPortNumber(
                portNameOrNumber, cultureInfo, true, ref error);

            if (port == Port.Invalid)
                return ReturnCode.Error;

            try
            {
                client.Connect(new IPEndPoint(address, port));

                TraceOps.DebugTrace(String.Format(
                    "Connect: SUCCESS {0} ==> {1}",
                    FormatOps.NetworkHostAndPort(
                        hostNameOrAddress, portNameOrNumber),
                    FormatOps.IpAddressAndPort(address, port)),
                    typeof(SocketOps).Name, TracePriority.NetworkDebug2);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;

                TraceOps.DebugTrace(String.Format(
                    "Connect: FAILURE {0} ==> {1}: {2}",
                    FormatOps.NetworkHostAndPort(
                        hostNameOrAddress, portNameOrNumber),
                    FormatOps.IpAddressAndPort(address, port),
                    FormatOps.WrapOrNull(error)),
                    typeof(SocketOps).Name, TracePriority.NetworkError);

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Network Object Introspection Methods
        public static Socket GetSocket(
            NetworkStream stream
            )
        {
            try
            {
                PropertyInfo propertyInfo;

                ///////////////////////////////////////////////////////////////

                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked
                    //       as "protected"; however, we need to know this
                    //       information and we do not want to derive a
                    //       custom class to get it; therefore, just use
                    //       reflection.  We cache the PropertyInfo object
                    //       so that we do not need to look it up more than
                    //       once.
                    //
                    if (networkStreamSocket == null)
                    {
                        //
                        // HACK: As of the .NET 5.0 runtime (apparently),
                        //       this property is now public; however, we
                        //       still use reflection
                        //
                        MetaBindingFlags metaBindingFlags;

                        if (CommonOps.Runtime.IsDotNetCore5xOr6x())
                            metaBindingFlags = MetaBindingFlags.SocketPublic;
                        else
                            metaBindingFlags = MetaBindingFlags.SocketPrivate;

                        networkStreamSocket =
                            typeof(NetworkStream).GetProperty(
                                "Socket", ObjectOps.GetBindingFlags(
                                    metaBindingFlags, true));
                    }

                    propertyInfo = networkStreamSocket;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if ((propertyInfo != null) && (stream != null))
                    return propertyInfo.GetValue(stream, null) as Socket;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError2);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsListenerActive(
            TcpListener listener,
            bool @default
            )
        {
            try
            {
                PropertyInfo propertyInfo;

                ///////////////////////////////////////////////////////////////

                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked
                    //       as "protected"; however, we need to know this
                    //       information and we do not want to derive a
                    //       custom class to get it; therefore, just use
                    //       reflection.  We cache the PropertyInfo object
                    //       so that we do not need to look it up more than
                    //       once.
                    //
                    if (tcpListenerActive == null)
                    {
                        tcpListenerActive = typeof(TcpListener).GetProperty(
                            "Active", ObjectOps.GetBindingFlags(
                                MetaBindingFlags.SocketPrivate, true));
                    }

                    propertyInfo = tcpListenerActive;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if ((propertyInfo != null) && (listener != null))
                    return (bool)propertyInfo.GetValue(listener, null);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError2);
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCleanedUp(
            Socket socket,
            bool @default
            )
        {
            try
            {
                PropertyInfo propertyInfo;

                ///////////////////////////////////////////////////////////////

                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked
                    //       as "internal"; however, we need to know this
                    //       information.  Therefore, just use reflection.
                    //       We cache the PropertyInfo object so that we
                    //       do not need to look it up more than once.
                    //
                    if (socketCleanedUp == null)
                    {
                        //
                        // HACK: The name of this property was changed in
                        //       the timeframe of .NET 5.0.
                        //
                        socketCleanedUp = typeof(Socket).GetProperty(
                            CommonOps.Runtime.IsDotNetCore5xOr6x() ?
                                "Disposed" : "CleanedUp",
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.SocketPrivate, true));
                    }

                    propertyInfo = socketCleanedUp;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if ((propertyInfo != null) && (socket != null))
                    return (bool)propertyInfo.GetValue(socket, null);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError2);
            }

            return @default;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Network Server Methods
        private static void GetRemoteEndPoint(
            TcpClient client,
            out IPEndPoint endPoint,
            ref Result error
            )
        {
            endPoint = null;

            try
            {
                if (client == null)
                {
                    error = "invalid client";
                    return;
                }

                Socket socket = client.Client; /* throw */

                if (socket == null)
                {
                    error = "invalid client socket";
                    return;
                }

                endPoint = socket.RemoteEndPoint as IPEndPoint; /* throw */

                if (endPoint == null)
                {
                    error = "invalid remote endpoint";
                    return;
                }
            }
            catch (Exception e)
            {
                error = e;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Network Server Methods
        private static ReturnCode GetServerScript(
            TcpClient client,
            string channelId,
            string text,
            ref StringList list,
            ref Result error
            )
        {
            IPEndPoint endPoint;

            GetRemoteEndPoint(client, out endPoint, ref error);

            if (endPoint == null)
                return ReturnCode.Error;

            StringList localList = new StringList();

            localList.Add(text);
            localList.Add(channelId);
            localList.Add(StringOps.GetStringFromObject(endPoint.Address));
            localList.Add(StringOps.GetStringFromObject(endPoint.Port));

            list = localList;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TcpListener NewTcpListener(
            string hostNameOrAddress,
            string portNameOrNumber,
            CultureInfo cultureInfo,
            AddressFamily addressFamily,
            ref Result error
            )
        {
            try
            {
                IPAddress address = null;

                if (hostNameOrAddress != null)
                {
                    address = GetIpAddress(
                        hostNameOrAddress, addressFamily, true, ref error);
                }

                if ((hostNameOrAddress == null) || (address != null))
                {
                    int port = GetPortNumber(
                        portNameOrNumber, cultureInfo, true, ref error);

                    if (port != Port.Invalid)
                    {
                        TcpListener listener = (address != null) ?
                            new TcpListener(address, port) :
                            new TcpListener(port);

                        TraceOps.DebugTrace(String.Format(
                            "NewTcpListener: {0} ==> {1}",
                            FormatOps.NetworkHostAndPort(
                                hostNameOrAddress, portNameOrNumber),
                            FormatOps.IpAddressAndPort(address, port)),
                            typeof(SocketOps).Name,
                            TracePriority.NetworkDebug2);

                        return listener;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeExclusiveAddressUse(
            TcpListener listener,
            bool exclusive
            )
        {
            try
            {
                //
                // NOTE: Mono does not support this feature on Unix.
                //
                if (!CommonOps.Runtime.IsMono() ||
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    listener.ExclusiveAddressUse = exclusive; /* throw */
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Mono 2.0/2.2 does not support this feature.
                //
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetPollTimeout(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            int milliseconds;

            if (interpreter != null)
            {
                milliseconds = interpreter.GetSleepTime(
                    SleepType.Socket); /* REFRESH */
            }
            else
            {
                milliseconds = EventManager.DefaultSleepTime;
            }

            return PerformanceOps.GetMicrosecondsFromMilliseconds(
                milliseconds, MinimumSocketPollTimeout,
                MaximumSocketPollTimeout);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeHandleServerError(
            Interpreter interpreter,    /* in: OPTIONAL */
            SocketClientData clientData /* in */
            )
        {
            if (clientData == null)
                return;

            ReturnCode code = clientData.ReturnCode;

            if (code != ReturnCode.Ok)
            {
                Result result = clientData.Result;

                TraceOps.DebugTrace(String.Format(
                    "MaybeHandleServerError: interpreter = {0}, " +
                    "code = {1}, result = {2}",
                    FormatOps.InterpreterNoThrow(interpreter), code,
                    FormatOps.WrapOrNull(result)),
                    typeof(SocketOps).Name,
                    TracePriority.NetworkError);

                /* IGNORED */
                EventOps.HandleBackgroundError(
                    interpreter, code, result);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Server Thread Support Methods
        public static void ServerThreadStart(
            object obj /* in, out */
            ) /* System.Threading.ParameterizedThreadStart */
        {
            DateTime now = TimeOps.GetUtcNow();

            try
            {
                SocketClientData clientData = obj as SocketClientData;

                if (clientData == null)
                    return; /* NOTE: There is no event to set. */

                bool setEvent = false;
                EventWaitHandle localEvent = clientData.Event;

                try
                {
                    Interpreter interpreter = clientData.Interpreter;

                    if (interpreter == null)
                    {
                        clientData.Result = "invalid interpreter";
                        clientData.ReturnCode = ReturnCode.Error;

                        return;
                    }

                    interpreter.EnterSocketThread();

                    try
                    {
                        Result result; /* REUSED */
                        TcpListener listener;

                        result = null;

                        listener = NewTcpListener(
                            clientData.Address, clientData.Port,
                            interpreter.InternalCultureInfo,
                            clientData.AddressFamily, ref result);

                        if (listener == null)
                        {
                            clientData.Result = result;
                            clientData.ReturnCode = ReturnCode.Error;

                            return;
                        }

                        /* NO RESULT */
                        MaybeExclusiveAddressUse(
                            listener, clientData.Exclusive);

                        //
                        // NOTE: So far, so good, so start listening...
                        //       This may raise an exception, e.g. if a
                        //       port is already in use, etc.
                        //
                        listener.Start(); /* throw */

                        try
                        {
                            Socket socket = listener.Server;

                            if (socket == null)
                            {
                                clientData.Result = "missing server socket";
                                clientData.ReturnCode = ReturnCode.Error;

                                return;
                            }

                            bool channelAdded = false;
                            string channelId = null;

                            try
                            {
                                //
                                // NOTE: Add the "listener" channel to the
                                //       interpreter.
                                //
                                /* NO RESULT */
                                AddServerAndSetChannel(
                                    interpreter, clientData, listener,
                                    ref channelId, ref channelAdded);

                                //
                                // NOTE: At this point, attempt to signal
                                //       the caller to receive the return
                                //       code and result that indicate our
                                //       success entering the server loop.
                                //
                                setEvent = ThreadOps.SetEvent(localEvent);

                                if (!setEvent && (localEvent != null))
                                {
                                    TraceOps.DebugTrace(
                                        "ServerThreadStart: " +
                                        "FAILED TO SIGNAL PARENT",
                                        typeof(SocketOps).Name,
                                        TracePriority.NetworkWarning);
                                }

                                //
                                // NOTE: The listener channel could not
                                //       be added to the interpreter?
                                //       Basically, this should almost
                                //       never happen, i.e. except for
                                //       during interpreter disposal,
                                //       etc.
                                //
                                if (clientData.ReturnCode != ReturnCode.Ok)
                                    return;

                                //
                                // NOTE: Poll the listener for incoming
                                //       connections.  For an incoming
                                //       connection, accept a TcpClient
                                //       and queue the supplied command
                                //       to be evaluated.
                                //
                                TraceOps.DebugTrace(
                                    "ServerThreadStart: STARTED",
                                    typeof(SocketOps).Name,
                                    TracePriority.NetworkDebug);

                                while (true)
                                {
                                    //
                                    // NOTE: If the underlying socket has been
                                    //       cleaned up (i.e. the other thread
                                    //       called [close] on it), then bail
                                    //       out now.
                                    //
                                    if (IsCleanedUp(socket, true))
                                    {
                                        TraceOps.DebugTrace(
                                            "ServerThreadStart: server " +
                                            "socket cleaned up (outer)",
                                            typeof(SocketOps).Name,
                                            TracePriority.NetworkDebug);

                                        break;
                                    }

                                    //
                                    // NOTE: If the TCP listener is no longer
                                    //       active then bail out now.
                                    //
                                    if (!IsListenerActive(listener, false))
                                    {
                                        TraceOps.DebugTrace(
                                            "ServerThreadStart: " +
                                            "listener inactive (outer)",
                                            typeof(SocketOps).Name,
                                            TracePriority.NetworkDebug);

                                        break;
                                    }

                                    int timeout = GetPollTimeout(interpreter);

                                    while (true)
                                    {
                                        if (!socket.Poll(
                                                timeout, SelectMode.SelectRead))
                                        {
                                            break;
                                        }

                                        if (IsCleanedUp(socket, true))
                                        {
                                            TraceOps.DebugTrace(
                                                "ServerThreadStart: server " +
                                                "socket cleaned up (inner)",
                                                typeof(SocketOps).Name,
                                                TracePriority.NetworkDebug);

                                            break;
                                        }

                                        if (!IsListenerActive(listener, false))
                                        {
                                            TraceOps.DebugTrace(
                                                "ServerThreadStart: " +
                                                "listener inactive (inner)",
                                                typeof(SocketOps).Name,
                                                TracePriority.NetworkDebug);

                                            break;
                                        }

                                        //
                                        // NOTE: Attempt to accept the client
                                        //       connection and deal with it.
                                        //
                                        AddClientAndQueueScript(
                                            interpreter, clientData,
                                            listener.AcceptTcpClient());

                                        MaybeHandleServerError(
                                            interpreter, clientData);
                                    }
                                }

                                TraceOps.DebugTrace(
                                    "ServerThreadStart: STOPPED",
                                    typeof(SocketOps).Name,
                                    TracePriority.NetworkDebug);
                            }
                            finally
                            {
                                if (channelAdded && (channelId != null) &&
                                    interpreter.InternalHasChannels())
                                {
                                    ReturnCode removeCode;
                                    Result removeError = null;

                                    removeCode = interpreter.RemoveChannel(
                                        channelId, ChannelType.None, false,
                                        false, false, ref removeError);

                                    if (removeCode == ReturnCode.Ok)
                                    {
                                        channelAdded = false;
                                    }
                                    else
                                    {
                                        DebugOps.Complain(
                                            interpreter, removeCode,
                                            removeError);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            //
                            // NOTE: Stop listening for incoming clients,
                            //       we are done.  This call is (probably)
                            //       pointless because the only known way
                            //       we can exit the loop is by externally
                            //       stopping its channel; however, this
                            //       should be fairly harmless.
                            //
                            listener.Stop(); /* throw */
                        }
                    }
                    finally
                    {
                        interpreter.ExitSocketThread();
                    }
                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();

                    clientData.Result = e;
                    clientData.ReturnCode = ReturnCode.Error;
                }
                catch (ThreadInterruptedException e)
                {
                    clientData.Result = e;
                    clientData.ReturnCode = ReturnCode.Error;
                }
                catch (Exception e)
                {
                    clientData.Result = e;
                    clientData.ReturnCode = ReturnCode.Error;
                }
                finally
                {
                    if (!setEvent)
                        ThreadOps.SetEvent(localEvent);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (ThreadInterruptedException)
            {
                // do nothing.
            }
            finally
            {
                TraceOps.DebugTrace(String.Format(
                    "ServerThreadStart: TIME {0}",
                    TimeOps.GetUtcNow().Subtract(now)),
                    typeof(SocketOps).Name,
                    TracePriority.NetworkDebug2);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddServerAndSetChannel(
            Interpreter interpreter,
            SocketClientData clientData,
            TcpListener listener,
            ref string channelId,
            ref bool channelAdded
            )
        {
            if (clientData == null)
                return;

            ReturnCode code = ReturnCode.Ok; /* REUSED */
            Result result = null; /* REUSED */

            try
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;

                    return;
                }

                channelId = FormatOps.Id("listenSocket", null,
                    interpreter.NextId()); /* COMPAT: Eagle beta. */

                result = null;

                code = interpreter.AddTcpListenerChannel(
                    channelId, ChannelType.None, listener,
                    clientData, ref channelAdded, ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddServerAndSetChannel: " +
                        "could not add channel {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    // return; /* REDUNDANT */
                }

                result = channelId;
            }
            finally
            {
                clientData.Result = result;
                clientData.ReturnCode = code;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddClientAndQueueScript(
            Interpreter interpreter,
            SocketClientData clientData,
            TcpClient client
            )
        {
            if (clientData == null)
                return;

            ReturnCode code = ReturnCode.Ok; /* REUSED */
            Result result = null; /* REUSED */

            try
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;

                    return;
                }

                //
                // NOTE: Create unique Id for client channel.
                //
                string channelId = FormatOps.Id(
                    "serverSocket", null,
                    interpreter.NextId()); /* COMPAT: Eagle beta. */

                //
                // NOTE: Grab underlying network stream and
                //       setup the read/write timeouts.
                //
                NetworkStream stream = client.GetStream();

                clientData.MaybeSetTimeouts(stream);

                //
                // NOTE: Add the new channel for this client
                //       to the interpreter.
                //
                result = null;

                code = interpreter.AddFileOrSocketChannel(channelId,
                    stream, clientData.Options, clientData.StreamFlags,
                    clientData.AvailableTimeout, false, false, false,
                    false, new ClientData(client), ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddClientAndQueueScript: " +
                        "could not add channel {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    return;
                }

                //
                // NOTE: Construct and queue full script when
                //       a new client connection is accepted,
                //       based on the original script fragment
                //       used by the caller.
                //
                StringList list = null;

                result = null;

                code = GetServerScript(
                    client, channelId, clientData.Text,
                    ref list, ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddClientAndQueueScript: " +
                        "could not get script {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    return;
                }

                result = null;

                code = interpreter.QueueScript(
                    TimeOps.GetUtcNow(), list.ToString(),
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddClientAndQueueScript: " +
                        "could not queue script {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    // return; /* REDUNDANT */
                }
            }
            finally
            {
                clientData.Result = result;
                clientData.ReturnCode = code;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Network Address Methods
        private static bool MakeSureNotOffline(
            string hostNameOrAddress,    /* in */
            AddressFamily addressFamily, /* in */
            ref Result error             /* out */
            )
        {
            if (Interlocked.CompareExchange(ref offlineLevels, 0, 0) > 0)
            {
                error = String.Format(
                    "cannot resolve {0} IP address {1} while offline",
                    FormatOps.WrapOrNull(addressFamily),
                    FormatOps.NetworkHostAndPort(hostNameOrAddress, null));

                return false;
            }
            else
            {
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IPAddress GetIpAddress(
            string hostNameOrAddress,    /* in */
            AddressFamily addressFamily, /* in */
            bool strict,                 /* in */
            ref Result error             /* out */
            )
        {
            IPAddress result = null;
            Result localError = null;

            if (!String.IsNullOrEmpty(hostNameOrAddress) &&
                MakeSureNotOffline(
                    hostNameOrAddress, addressFamily, ref localError))
            {
                if (!IPAddress.TryParse(hostNameOrAddress, out result))
                {
                    try
                    {
                        //
                        // NOTE: Attempt to resolve the host name to one
                        //       or more IP addresses. This is required
                        //       even for things like "localhost", etc.
                        //
                        IPAddress[] addresses = Dns.GetHostAddresses(
                            hostNameOrAddress);

                        if (addresses != null)
                        {
                            int length = addresses.Length;

                            for (int index = 0; index < length; index++)
                            {
                                IPAddress address = addresses[index];

                                if (address == null)
                                    continue;

                                if (address.AddressFamily == addressFamily)
                                {
                                    result = address;
                                    break;
                                }
                            }

                            if (result == null)
                            {
                                localError = String.Format(
                                    "no {0} IP address was found for {1}",
                                    FormatOps.WrapOrNull(addressFamily),
                                    FormatOps.NetworkHostAndPort(
                                        hostNameOrAddress, null));
                            }
                        }
                        else
                        {
                            localError = String.Format(
                                "no IP addresses were found for {0}",
                                FormatOps.NetworkHostAndPort(
                                    hostNameOrAddress, null));
                        }
                    }
                    catch (Exception e)
                    {
                        localError = e;
                    }
                }
            }
            else if (strict)
            {
                if (localError == null)
                    localError = "invalid host name or IP address";
            }
            else
            {
                result = IPAddress.Any;
            }

            if (localError != null)
                error = localError;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetPortNumber(
            string portNameOrNumber, /* in */
            CultureInfo cultureInfo, /* in */
            bool strict,             /* in */
            ref Result error         /* out */
            )
        {
            ResultList errors = null;

            if (!String.IsNullOrEmpty(portNameOrNumber))
            {
                int port = Port.Invalid;
                Result localError; /* REUSED */

                localError = null;

                if (Value.GetInteger2(
                        portNameOrNumber, ValueFlags.AnyInteger,
                        cultureInfo, ref port,
                        ref localError) == ReturnCode.Ok)
                {
                    return port;
                }

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                ///////////////////////////////////////////////////////////////

#if NATIVE
                //
                // NOTE: Lookup the service name using getservbyname()
                //       API; the .NET Framework does not expose this
                //       functionality; therefore, use P/Invoke to do
                //       it ourselves.
                //
                int? nativePort;

                localError = null;

                nativePort = NativeSocket.GetPortNumberByNameAndProtocol(
                    portNameOrNumber, null, ref localError);

                if (nativePort != null)
                    return (int)nativePort;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
#endif
            }
            else if (!strict)
            {
                return Port.Automatic;
            }

            if (errors != null)
                error = errors;

            return Port.Invalid;
        }
        #endregion
    }
}
