/*
 * WebOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

using SecurityProtocolType = System.Net.SecurityProtocolType;

#if TEST
using _SecurityProtocolType = Eagle._Components.Public.SecurityProtocolType;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("47133ca0-868a-4403-8788-530721d2f302")]
    internal static class WebOps
    {
        #region Private Data
        //
        // HACK: If this is non-zero, any attempt to create a WebClient via
        //       this class will fail, preventing any network access using
        //       the WebClient class.
        //
        private static int offlineLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default timeout for a request, in milliseconds.  If
        //       this value is null, there is no explicit timeout, i.e.
        //       it will be up to the .NET Framework and/or Windows.
        //
        // HACK: This is purposely not read-only.
        //
        private static int? DefaultTimeout = null; /* COMPAT: Eagle beta. */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region TimeoutWebClient Helper Class
        [ObjectId("c0cfe212-92b3-47f9-a1b6-fa0f69f6ff04")]
        private sealed class TimeoutWebClient : WebClient
        {
            #region Private Data
            private int? timeout;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            public TimeoutWebClient(
                int? timeout /* in */
                )
                : base()
            {
                this.timeout = timeout;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Net.WebClient Overrides
            protected override WebRequest GetWebRequest(
                Uri address /* in */
                )
            {
                WebRequest webRequest = base.GetWebRequest(address);

                if (timeout != null)
                    webRequest.Timeout = (int)timeout;

                return webRequest;
            }
            #endregion
        }
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

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            if (empty || (offlineLevels != 0))
                localList.Add("OfflineLevels", offlineLevels.ToString());

            if (empty || (DefaultTimeout != null))
            {
                localList.Add("DefaultTimeout", (DefaultTimeout != null) ?
                    DefaultTimeout.ToString() : FormatOps.DisplayNull);
            }

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("Web Information");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Event Helper Methods
        private static StringList GetAsyncCompletedArguments(
            Uri uri,
            string method,
            byte[] rawData,
            NameValueCollection collection,
            string fileName,
            AsyncCompletedEventArgs eventArgs
            )
        {
            StringList result = new StringList();

            if (uri != null)
            {
                result.Add("uri");
                result.Add(uri.ToString());
            }

            if (method != null)
            {
                result.Add("method");
                result.Add(method);
            }

            if (rawData != null)
            {
                result.Add("rawData");
                result.Add(ArrayOps.ToHexadecimalString(rawData));
            }

            if (collection != null)
            {
                result.Add("collection");
                result.Add(ListOps.FromNameValueCollection(
                    collection, new StringList()).ToString());
            }

            if (fileName != null)
            {
                result.Add("fileName");
                result.Add(fileName);
            }

            if (eventArgs != null)
            {
                bool canceled = eventArgs.Cancelled;

                result.Add("canceled");
                result.Add(canceled.ToString());

                Exception exception = eventArgs.Error;

                if (exception != null)
                {
                    result.Add("exception");
                    result.Add(exception.GetType().ToString());
                    result.Add("error");
                    result.Add(exception.ToString());
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Download Event Handlers
        #region Download Data Event Handlers
        private static void DownloadDataAsyncCompleted(
            object sender,
            DownloadDataCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyPair<WebClient, Uri> anyPair =
                        clientData.Data as IAnyPair<WebClient, Uri>;

                    if (anyPair != null)
                    {
                        WebClient webClient = anyPair.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyPair.Y;
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, null, null, null, null, e));
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Event Handlers
        private static void DownloadFileAsyncCompleted(
            object sender,
            AsyncCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                string fileName = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, string> anyTriplet =
                        clientData.Data as IAnyTriplet<WebClient, Uri, string>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;
                        fileName = anyTriplet.Z;
                    }

                    clientData.Data = null;
                }

                ReturnCode code;
                Result result = null;

                code = callback.Invoke(
                    GetAsyncCompletedArguments(
                        uri, method, null, null, fileName, e),
                    ref result);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(code, result);
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region HTTPS Security Protocol Helper Methods
#if TEST
        public static ReturnCode ProbeSecurityProtocol(
            ref StringList list,
            ref Result error
            )
        {
            _SecurityProtocolType? protocol =
                _Tests.Default.TestProbeSecurityProtocol(ref error);

            if (protocol == null)
                return ReturnCode.Error;

            if (list == null)
                list = new StringList();

            list.Add("probedOk");
            list.Add(((_SecurityProtocolType)protocol).ToString());

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSecurityProtocol(
            ref StringList list,
            ref Result error
            )
        {
            SecurityProtocolType protocol;

            try
            {
                protocol = ServicePointManager.SecurityProtocol;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            ResultList results = null;

            if (_Tests.Default.TestGetSecurityProtocol(
                    ref results) != ReturnCode.Ok)
            {
                error = results;
                return ReturnCode.Error;
            }

            if (list == null)
                list = new StringList();

            list.Add("managerOk");

            list.Add(_Tests.Default.TestSecurityProtocolToString(
                (_SecurityProtocolType)protocol, null, true));

            list.Add("bestOk");
            list.Add(results);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetSecurityProtocol(
            bool obsolete,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Error;
            ResultList results = null; /* REUSED */

            if ((_Tests.Default.TestSetupSecurityProtocol(
                    false, !obsolete, ref results) == ReturnCode.Ok) &&
                (_Tests.Default.TestSetSecurityProtocol(
                    ref results) == ReturnCode.Ok))
            {
                code = ReturnCode.Ok;
            }

            TraceOps.DebugTrace(String.Format(
                "SetSecurityProtocol: code = {0}, results  = {1}",
                code, FormatOps.WrapOrNull(results)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            if (code != ReturnCode.Ok)
                error = results;

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Web Download Methods
        #region WebClient Support Methods
        public static WebClient CreateClient(
            string argument,
            int? timeout,
            ref Result error
            )
        {
            if (InOfflineMode())
            {
                error = String.Format(
                    "cannot create default {0} web client while offline",
                    FormatOps.WrapOrNull(argument));

                return null;
            }
            else if (timeout != null)
            {
                return new TimeoutWebClient(timeout);
            }
            else
            {
                return new WebClient();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static WebClient CreateClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            int? timeout,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                PreWebClientCallback preCallback =
                    interpreter.PreWebClientCallback;

                if (preCallback != null)
                {
                    if (preCallback(
                            interpreter, argument, clientData,
                            ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }
                }

                if (InOfflineMode())
                {
                    error = String.Format(
                        "cannot create {0} web client for " +
                        "interpreter {1} while offline",
                        FormatOps.WrapOrNull(argument),
                        FormatOps.InterpreterNoThrow(
                        interpreter));

                    return null;
                }
                else
                {
                    NewWebClientCallback newCallback =
                        interpreter.NewWebClientCallback;

                    if (newCallback != null)
                    {
                        return newCallback(
                            interpreter, argument, clientData,
                            ref error);
                    }
                }
            }

            return CreateClient(argument, timeout, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download Data Methods
        public static ReturnCode DownloadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            int? timeout,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted)
                {
                    UpdateOps.TryLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                TraceOps.DebugTrace(String.Format(
                    "DownloadData: interpreter = {0}, clientData = {1}, " +
                    "uri = {2}, timeout = {3}, trusted = {4}, " +
                    "wasTrusted = {5}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(clientData),
                    FormatOps.WrapOrNull(uri),
                    FormatOps.WrapOrNull(timeout),
                    trusted, FormatOps.WrapOrNull(wasTrusted)),
                    typeof(WebOps).Name, TracePriority.NetworkDebug);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "DownloadData", clientData,
                            timeout, ref localError))
                    {
                        if (webClient != null)
                        {
                            bytes = webClient.DownloadData(uri);
                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            error = "could not create web client";
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (wasTrusted != null)
                {
                    ReturnCode trustedCode;
                    Result trustedError = null;

                    trustedCode = UpdateOps.SetTrusted(
                        (bool)wasTrusted, ref trustedError);

                    if (trustedCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, trustedCode, trustedError);
                    }
                }

                UpdateOps.ExitLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadDataAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            int? timeout,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "DownloadDataAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "timeout = {5}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(timeout)), typeof(WebOps).Name,
                TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "DownloadDataAsync", clientData,
                            null, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyPair<WebClient, Uri>(
                                    webClient, uri));

                            webClient.DownloadDataCompleted +=
                                new DownloadDataCompletedEventHandler(
                                    DownloadDataAsyncCompleted);

                            /* NO RESULT */
                            webClient.DownloadDataAsync(uri, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if (webClient != null)
                {
                    ObjectOps.TryDisposeOrComplain<WebClient>(
                        interpreter, ref webClient);
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Methods
        public static ReturnCode DownloadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string fileName,
            int? timeout,
            bool trusted,
            ref Result error
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted)
                {
                    UpdateOps.TryLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                TraceOps.DebugTrace(String.Format(
                    "DownloadFile: interpreter = {0}, clientData = {1}, " +
                    "uri = {2}, fileName = {3}, timeout = {4}, " +
                    "trusted = {5}, wasTrusted = {6}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(clientData),
                    FormatOps.WrapOrNull(uri),
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(timeout),
                    trusted, FormatOps.WrapOrNull(wasTrusted)),
                    typeof(WebOps).Name, TracePriority.NetworkDebug);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "DownloadFile", clientData,
                            timeout, ref localError))
                    {
                        if (webClient != null)
                        {
                            /* NO RESULT */
                            webClient.DownloadFile(uri, fileName);

                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            error = "could not create web client";
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (wasTrusted != null)
                {
                    ReturnCode trustedCode;
                    Result trustedError = null;

                    trustedCode = UpdateOps.SetTrusted(
                        (bool)wasTrusted, ref trustedError);

                    if (trustedCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, trustedCode, trustedError);
                    }
                }

                UpdateOps.ExitLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadFileAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string fileName,
            int? timeout,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "DownloadFileAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "fileName = {5}, timeout = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(fileName),
                FormatOps.WrapOrNull(timeout)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "DownloadFileAsync", clientData,
                            null, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri, string>(
                                    webClient, uri, fileName));

                            webClient.DownloadFileCompleted +=
                                new AsyncCompletedEventHandler(
                                    DownloadFileAsyncCompleted);

                            /* NO RESULT */
                            webClient.DownloadFileAsync(
                                uri, fileName, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if (webClient != null)
                {
                    ObjectOps.TryDisposeOrComplain<WebClient>(
                        interpreter, ref webClient);
                }
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Upload Event Handlers
        #region Upload Data Event Handlers
        private static void UploadDataAsyncCompleted(
            object sender,
            UploadDataCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                byte[] rawData = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, byte[]>>
                        anyTriplet = clientData.Data as
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, byte[]>>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        IAnyPair<string, byte[]> anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            rawData = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, method, rawData, null, null, e));
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Event Handlers
        private static void UploadValuesAsyncCompleted(
            object sender,
            UploadValuesCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                NameValueCollection collection = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, NameValueCollection>>
                        anyTriplet = clientData.Data as
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, NameValueCollection>>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        IAnyPair<string, NameValueCollection>
                            anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            collection = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, method, null, collection, null, e));
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Event Handlers
        private static void UploadFileAsyncCompleted(
            object sender,
            UploadFileCompletedEventArgs e
            )
        {
            try
            {
                if (e == null)
                    return;

                ICallback callback = e.UserState as ICallback;

                if (callback == null)
                    return;

                Uri uri = null;
                string method = null;
                string fileName = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, string>>
                        anyTriplet = clientData.Data as
                    IAnyTriplet<WebClient, Uri, IAnyPair<string, string>>;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        IAnyPair<string, string> anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            fileName = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                ReturnCode code;
                Result result = null;

                code = callback.Invoke(
                    GetAsyncCompletedArguments(
                        uri, method, null, null, fileName, e),
                    ref result);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(code, result);
            }
            catch (Exception ex)
            {
                DebugOps.Complain(ReturnCode.Error, ex);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Web Upload Methods
        #region Upload Data Methods
        public static ReturnCode UploadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            byte[] rawData,
            int? timeout,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted)
                {
                    UpdateOps.TryLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                TraceOps.DebugTrace(String.Format(
                    "UploadData: interpreter = {0}, clientData = {1}, " +
                    "uri = {2}, method = {3}, rawData = {4}, " +
                    "timeout = {5}, trusted = {6}, wasTrusted = {7}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(clientData),
                    FormatOps.WrapOrNull(uri),
                    FormatOps.WrapOrNull(method),
                    (rawData != null) ?
                        rawData.Length : Length.Invalid,
                    FormatOps.WrapOrNull(timeout),
                    trusted, FormatOps.WrapOrNull(wasTrusted)),
                    typeof(WebOps).Name, TracePriority.NetworkDebug);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "UploadData", clientData,
                            timeout, ref localError))
                    {
                        if (webClient != null)
                        {
                            bytes = webClient.UploadData(
                                uri, method, rawData);

                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            error = "could not create web client";
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (wasTrusted != null)
                {
                    ReturnCode trustedCode;
                    Result trustedError = null;

                    trustedCode = UpdateOps.SetTrusted(
                        (bool)wasTrusted, ref trustedError);

                    if (trustedCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, trustedCode, trustedError);
                    }
                }

                UpdateOps.ExitLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadDataAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string method,
            byte[] rawData,
            int? timeout,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "UploadDataAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "method = {5}, rawData = {6}, timeout = {7}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                (rawData != null) ?
                    rawData.Length : Length.Invalid,
                FormatOps.WrapOrNull(timeout)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "UploadDataAsync", clientData,
                            timeout, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri,
                                    IAnyPair<string, byte[]>>(
                                        webClient, uri, new AnyPair<string,
                                            byte[]>(method, rawData)));

                            webClient.UploadDataCompleted +=
                                new UploadDataCompletedEventHandler(
                                    UploadDataAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadDataAsync(
                                uri, method, rawData, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if (webClient != null)
                {
                    ObjectOps.TryDisposeOrComplain<WebClient>(
                        interpreter, ref webClient);
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Methods
        public static ReturnCode UploadValues(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            NameValueCollection collection,
            int? timeout,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted)
                {
                    UpdateOps.TryLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                TraceOps.DebugTrace(String.Format(
                    "UploadValues: interpreter = {0}, clientData = {1}, " +
                    "uri = {2}, method = {3}, collection = {4}, " +
                    "timeout = {5}, trusted = {6}, wasTrusted = {7}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(clientData),
                    FormatOps.WrapOrNull(uri),
                    FormatOps.WrapOrNull(method),
                    (collection != null) ?
                        collection.Count : Count.Invalid,
                    FormatOps.WrapOrNull(timeout),
                    trusted, FormatOps.WrapOrNull(wasTrusted)),
                    typeof(WebOps).Name, TracePriority.NetworkDebug);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "UploadValues", clientData,
                            timeout, ref localError))
                    {
                        if (webClient != null)
                        {
                            bytes = webClient.UploadValues(
                                uri, method, collection);

                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            error = "could not create web client";
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (wasTrusted != null)
                {
                    ReturnCode trustedCode;
                    Result trustedError = null;

                    trustedCode = UpdateOps.SetTrusted(
                        (bool)wasTrusted, ref trustedError);

                    if (trustedCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, trustedCode, trustedError);
                    }
                }

                UpdateOps.ExitLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadValuesAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string method,
            NameValueCollection collection,
            int? timeout,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "UploadValuesAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "method = {5}, collection = {6}, timeout = {7}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                (collection != null) ?
                    collection.Count : Count.Invalid,
                FormatOps.WrapOrNull(timeout)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "UploadValuesAsync", clientData,
                            timeout, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri,
                                    IAnyPair<string, NameValueCollection>>(
                                        webClient, uri, new AnyPair<string,
                                            NameValueCollection>(method,
                                                collection)));

                            webClient.UploadValuesCompleted +=
                                new UploadValuesCompletedEventHandler(
                                    UploadValuesAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadValuesAsync(
                                uri, method, collection, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if (webClient != null)
                {
                    ObjectOps.TryDisposeOrComplain<WebClient>(
                        interpreter, ref webClient);
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Methods
        public static ReturnCode UploadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            string fileName,
            int? timeout,
            bool trusted,
            ref Result error
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted)
                {
                    UpdateOps.TryLock(ref locked);

                    if (!locked)
                    {
                        error = "unable to acquire update lock";
                        return ReturnCode.Error;
                    }

                    wasTrusted = UpdateOps.IsTrusted();
                }

                TraceOps.DebugTrace(String.Format(
                    "UploadFile: interpreter = {0}, clientData = {1}, " +
                    "uri = {2}, method = {3}, fileName = {4}, " +
                    "timeout = {5}, trusted = {6}, wasTrusted = {7}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(clientData),
                    FormatOps.WrapOrNull(uri),
                    FormatOps.WrapOrNull(method),
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(timeout),
                    trusted, FormatOps.WrapOrNull(wasTrusted)),
                    typeof(WebOps).Name, TracePriority.NetworkDebug);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "UploadFile", clientData,
                            timeout, ref localError))
                    {
                        if (webClient != null)
                        {
                            /* NO RESULT */
                            webClient.UploadFile(
                                uri, method, fileName);

                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            error = "could not create web client";
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            finally
            {
                if (wasTrusted != null)
                {
                    ReturnCode trustedCode;
                    Result trustedError = null;

                    trustedCode = UpdateOps.SetTrusted(
                        (bool)wasTrusted, ref trustedError);

                    if (trustedCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, trustedCode, trustedError);
                    }
                }

                UpdateOps.ExitLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadFileAsync(
            Interpreter interpreter,
            IClientData clientData,
            StringList arguments,
            CallbackFlags callbackFlags,
            Uri uri,
            string method,
            string fileName,
            int? timeout,
            ref Result error
            )
        {
            TraceOps.DebugTrace(String.Format(
                "UploadFileAsync: interpreter = {0}, clientData = {1}, " +
                "arguments = {2}, callbackFlags = {3}, uri = {4}, " +
                "method = {5}, fileName = {6}, timeout = {7}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(callbackFlags),
                FormatOps.WrapOrNull(uri),
                FormatOps.WrapOrNull(method),
                FormatOps.WrapOrNull(fileName),
                FormatOps.WrapOrNull(timeout)),
                typeof(WebOps).Name, TracePriority.NetworkDebug);

            ReturnCode code = ReturnCode.Ok;
            WebClient webClient = null;

            try
            {
                ICallback callback = CommandCallback.Create(
                    MarshalFlags.Default, callbackFlags,
                    ObjectFlags.Callback, ByRefArgumentFlags.None,
                    interpreter, null, null, arguments, ref error);

                if (callback != null)
                {
                    try
                    {
                        Result localError = null;

                        webClient = CreateClient(
                            interpreter, "UploadFileAsync", clientData,
                            timeout, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new AnyTriplet<WebClient, Uri,
                                    IAnyPair<string, string>>(
                                        webClient, uri, new AnyPair<string,
                                            string>(method, fileName)));

                            webClient.UploadFileCompleted +=
                                new UploadFileCompletedEventHandler(
                                    UploadFileAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadFileAsync(
                                uri, method, fileName, callback);
                        }
                        else if (localError != null)
                        {
                            error = localError;
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            error = "could not create web client";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            finally
            {
                if (webClient != null)
                {
                    ObjectOps.TryDisposeOrComplain<WebClient>(
                        interpreter, ref webClient);
                }
            }

            return code;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Metadata Support Methods
        private static bool IsGoodTimeout(
            int timeout,
            bool allowNone
            )
        {
            if (timeout == _Timeout.Infinite)
                return false;

            if (timeout == _Timeout.None)
                return allowNone;

            if (timeout < _Timeout.Minimum)
                return false;

#if false
            if (timeout > _Timeout.Maximum)
                return false;
#endif

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Metadata Support Methods
        public static bool InOfflineMode()
        {
            return Interlocked.CompareExchange(ref offlineLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetOfflineMode(
            bool offline /* in */
            )
        {
            if (offline)
                Interlocked.Increment(ref offlineLevels);
            else
                Interlocked.Decrement(ref offlineLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int? GetTimeout(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            int timeout; /* REUSED */

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    int? localTimeout = interpreter.InternalGetTimeout(
                        TimeoutType.Network); /* OPTIONAL */

                    if (localTimeout != null)
                    {
                        timeout = (int)localTimeout;

                        if (IsGoodTimeout(timeout, true))
                            return timeout;
                    }
                }
            }

            string value = GlobalConfiguration.GetValue(
                EnvVars.NetworkTimeout, ConfigurationFlags.WebOps);

            if (value != null)
            {
                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                timeout = _Timeout.None;

                if (Value.GetInteger2(value,
                        ValueFlags.AnyInteger, cultureInfo,
                        ref timeout) == ReturnCode.Ok)
                {
                    return timeout;
                }
            }

            return DefaultTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetTimeoutOrDefault(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            int? timeout = GetTimeout(interpreter);

            if (timeout != null)
            {
                int localTimeout = (int)timeout;

                if (IsGoodTimeout(localTimeout, true))
                    return localTimeout;
            }

            return ThreadOps.GetTimeout(
                interpreter, timeout, TimeoutType.Network);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Wrapper Methods
        public static object MakeRequest(
            WebClient webClient,      /* in */
            Uri uri,                  /* in */
            NameValueCollection data, /* in: OPTIONAL */
            IProfilerState profiler,  /* in: OPTIONAL */
            ref Result error          /* out */
            )
        {
            if (webClient == null)
            {
                error = "invalid web client";
                return null;
            }

            if (profiler != null)
                profiler.Start();

            try
            {
                if (data != null)
                    return webClient.UploadValues(uri, data);
                else
                    return webClient.DownloadString(uri);
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
            finally
            {
                if (profiler != null)
                    profiler.Stop();
            }
        }
        #endregion
    }
}
