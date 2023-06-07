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

using DownloadDataPair = Eagle._Components.Public.AnyPair<
    System.Net.WebClient, System.Uri>;

using DownloadFileTriplet = Eagle._Components.Public.AnyTriplet<
    System.Net.WebClient, System.Uri, string>;

using UploadDataPair = Eagle._Components.Public.AnyPair<string, byte[]>;

using UploadValuesPair = Eagle._Components.Public.AnyPair<
    string, System.Collections.Specialized.NameValueCollection>;

using UploadFilePair = Eagle._Components.Public.AnyPair<string, string>;

using UploadDataTriplet = Eagle._Components.Public.AnyTriplet<
    System.Net.WebClient, System.Uri, Eagle._Components.Public.AnyPair<
        string, byte[]>>;

using UploadValuesTriplet = Eagle._Components.Public.AnyTriplet<
    System.Net.WebClient, System.Uri, Eagle._Components.Public.AnyPair<
        string, System.Collections.Specialized.NameValueCollection>>;

using UploadFileTriplet = Eagle._Components.Public.AnyTriplet<
    System.Net.WebClient, System.Uri, Eagle._Components.Public.AnyPair<
        string, string>>;

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

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultViaClient = false;
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
            Uri uri,                          /* in */
            string method,                    /* in */
            byte[] rawData,                   /* in */
            NameValueCollection data,         /* in */
            string fileName,                  /* in */
            AsyncCompletedEventArgs eventArgs /* in */
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

            if (data != null)
            {
                result.Add("data");
                result.Add(ListOps.FromNameValueCollection(
                    data, new StringList()).ToString());
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
            object sender,                   /* in */
            DownloadDataCompletedEventArgs e /* in */
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
                    DownloadDataPair anyPair =
                        clientData.Data as DownloadDataPair;

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
            object sender,            /* in */
            AsyncCompletedEventArgs e /* in */
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
                    DownloadFileTriplet anyTriplet =
                        clientData.Data as DownloadFileTriplet;

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
            ref StringList list, /* out */
            ref Result error     /* out */
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
            ref StringList list, /* out */
            ref Result error     /* out */
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
            bool obsolete,   /* in */
            ref Result error /* out */
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

            TraceOps.DebugTrace(
                "SetSecurityProtocol", null, typeof(WebOps).Name,
                TracePriority.NetworkDebug, false, "code", code,
                "results", results);

            if (code != ReturnCode.Ok)
                error = results;

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Web Download / Upload Helper Methods
        private static WebTransferCallback GetTransferCallback(
            Interpreter interpreter /* in */
            )
        {
            return (interpreter != null) ?
                interpreter.WebTransferCallback : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode InvokeTransferCallback(
            WebTransferCallback callback, /* in */
            Interpreter interpreter,      /* in */
            WebFlags webFlags,            /* in */
            IClientData clientData,       /* in */
            ref Result error              /* out */
            )
        {
            try
            {
                TraceOps.DebugTrace("InvokeTransferCallback", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug2,
                    true, "callback", callback, "interpreter",
                    interpreter, "webFlags", webFlags, "clientData",
                    clientData, "error", error);

                if (callback == null)
                {
                    error = "invalid web transfer callback";
                    return ReturnCode.Error;
                }

                return callback(
                    interpreter, webFlags, clientData,
                    ref error); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(WebOps).Name,
                    TracePriority.NetworkError);

                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Web Download Methods
        #region WebClient Support Methods
        public static WebClient CreateClient(
            string argument, /* in */
            int? timeout,    /* in */
            ref Result error /* out */
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
            Interpreter interpreter, /* in */
            string argument,         /* in */
            IClientData clientData,  /* in */
            int? timeout,            /* in */
            ref Result error         /* out */
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
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Uri = uri;
                webClientData.Timeout = timeout;
                webClientData.Trusted = trusted;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadData;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        bytes = webClientData.Bytes;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return DownloadDataViaClient(interpreter,
                    webClientData.ClientData, webClientData.Uri,
                    webClientData.Timeout, webClientData.Trusted,
                    ref bytes, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadDataAsync(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Arguments = arguments;
                webClientData.CallbackFlags = callbackFlags;
                webClientData.Uri = uri;
                webClientData.Timeout = timeout;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadDataAsynchronous;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return DownloadDataAsyncViaClient(interpreter,
                    webClientData.ClientData, webClientData.Arguments,
                    webClientData.CallbackFlags, webClientData.Uri,
                    webClientData.Timeout, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Methods
        public static ReturnCode DownloadFile(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            string fileName,         /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref Result error         /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Uri = uri;
                webClientData.FileName = fileName;
                webClientData.Timeout = timeout;
                webClientData.Trusted = trusted;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadFile;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return DownloadFileViaClient(interpreter,
                    webClientData.ClientData, webClientData.Uri,
                    webClientData.FileName, webClientData.Timeout,
                    webClientData.Trusted, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadFileAsync(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string fileName,             /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Arguments = arguments;
                webClientData.CallbackFlags = callbackFlags;
                webClientData.Uri = uri;
                webClientData.FileName = fileName;
                webClientData.Timeout = timeout;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadFileAsynchronous;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return DownloadFileAsyncViaClient(interpreter,
                    webClientData.ClientData, webClientData.Arguments,
                    webClientData.CallbackFlags, webClientData.Uri,
                    webClientData.FileName, webClientData.Timeout,
                    ref error);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Web Download Methods
        #region Download Data Via Client Methods
        private static ReturnCode DownloadDataViaClient(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
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

                TraceOps.DebugTrace("DownloadDataViaClient", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug,
                    true, "interpreter", interpreter, "clientData",
                    clientData, "uri", uri, "timeout", timeout,
                    "trusted", trusted, "wasTrusted", wasTrusted);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "DownloadDataViaClient",
                            clientData, timeout, ref localError))
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

        private static ReturnCode DownloadDataAsyncViaClient(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            TraceOps.DebugTrace("DownloadDataAsyncViaClient", null,
                typeof(WebOps).Name, TracePriority.NetworkDebug,
                true, "interpreter", interpreter, "clientData",
                clientData, "arguments", arguments, "callbackFlags",
                callbackFlags, "uri", uri, "timeout", timeout);

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
                            interpreter, "DownloadDataAsyncViaClient",
                            clientData, null, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new DownloadDataPair(webClient, uri));

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

        #region Download File Via Client Methods
        private static ReturnCode DownloadFileViaClient(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            string fileName,         /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref Result error         /* out */
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

                TraceOps.DebugTrace("DownloadFileViaClient", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug,
                    true, "interpreter", interpreter, "clientData",
                    clientData, "uri", uri, "fileName", fileName,
                    "timeout", timeout, "trusted", trusted,
                    "wasTrusted", wasTrusted);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "DownloadFileViaClient",
                            clientData, timeout, ref localError))
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

        private static ReturnCode DownloadFileAsyncViaClient(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string fileName,             /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            TraceOps.DebugTrace("DownloadFileAsyncViaClient", null,
                typeof(WebOps).Name, TracePriority.NetworkDebug,
                true, "interpreter", interpreter, "clientData",
                clientData, "arguments", arguments, "callbackFlags",
                callbackFlags, "uri", uri, "fileName", fileName,
                "timeout", timeout);

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
                            interpreter, "DownloadFileAsyncViaClient",
                            clientData, null, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new DownloadFileTriplet(
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
            object sender,                 /* in */
            UploadDataCompletedEventArgs e /* in */
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
                    UploadDataTriplet anyTriplet =
                        clientData.Data as UploadDataTriplet;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        UploadDataPair anyPair = anyTriplet.Z;

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
            object sender,                   /* in */
            UploadValuesCompletedEventArgs e /* in */
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
                NameValueCollection data = null;
                IClientData clientData = callback.ClientData;

                if (clientData != null)
                {
                    UploadValuesTriplet anyTriplet =
                        clientData.Data as UploadValuesTriplet;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        UploadValuesPair anyPair = anyTriplet.Z;

                        if (anyPair != null)
                        {
                            method = anyPair.X;
                            data = anyPair.Y;
                        }
                    }

                    clientData.Data = null;
                }

                /* NO RESULT */
                callback.FireEventHandler(sender, e,
                    GetAsyncCompletedArguments(
                        uri, method, null, data, null, e));
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
            object sender,                 /* in */
            UploadFileCompletedEventArgs e /* in */
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
                    UploadFileTriplet anyTriplet =
                        clientData.Data as UploadFileTriplet;

                    if (anyTriplet != null)
                    {
                        WebClient webClient = anyTriplet.X;

                        if (webClient != null)
                        {
                            webClient.Dispose();
                            webClient = null;
                        }

                        uri = anyTriplet.Y;

                        UploadFilePair anyPair = anyTriplet.Z;

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
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            string method,           /* in */
            byte[] rawData,          /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Uri = uri;
                webClientData.Method = method;
                webClientData.RawData = rawData;
                webClientData.Timeout = timeout;
                webClientData.Trusted = trusted;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadData;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        bytes = webClientData.Bytes;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return UploadDataViaClient(interpreter,
                    webClientData.ClientData, webClientData.Uri,
                    webClientData.Method, webClientData.RawData,
                    webClientData.Timeout, webClientData.Trusted,
                    ref bytes, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadDataAsync(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            byte[] rawData,              /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Arguments = arguments;
                webClientData.CallbackFlags = callbackFlags;
                webClientData.Uri = uri;
                webClientData.Method = method;
                webClientData.RawData = rawData;
                webClientData.Timeout = timeout;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadDataAsynchronous;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return UploadDataAsyncViaClient(interpreter,
                    webClientData.ClientData, webClientData.Arguments,
                    webClientData.CallbackFlags, webClientData.Uri,
                    webClientData.Method, webClientData.RawData,
                    webClientData.Timeout, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Methods
        public static ReturnCode UploadValues(
            Interpreter interpreter,  /* in */
            IClientData clientData,   /* in */
            Uri uri,                  /* in */
            string method,            /* in */
            NameValueCollection data, /* in */
            int? timeout,             /* in */
            bool trusted,             /* in */
            ref byte[] bytes,         /* out */
            ref Result error          /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Uri = uri;
                webClientData.Method = method;
                webClientData.Data = data;
                webClientData.Timeout = timeout;
                webClientData.Trusted = trusted;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadValues;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        bytes = webClientData.Bytes;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return UploadValuesViaClient(interpreter,
                    webClientData.ClientData, webClientData.Uri,
                    webClientData.Method, webClientData.Data,
                    webClientData.Timeout, webClientData.Trusted,
                    ref bytes, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadValuesAsync(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            NameValueCollection data,    /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Arguments = arguments;
                webClientData.CallbackFlags = callbackFlags;
                webClientData.Uri = uri;
                webClientData.Method = method;
                webClientData.Data = data;
                webClientData.Timeout = timeout;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadValuesAsynchronous;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return UploadValuesAsyncViaClient(interpreter,
                    webClientData.ClientData, webClientData.Arguments,
                    webClientData.CallbackFlags, webClientData.Uri,
                    webClientData.Method, webClientData.Data,
                    webClientData.Timeout, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Methods
        public static ReturnCode UploadFile(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            string method,           /* in */
            string fileName,         /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref Result error         /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Uri = uri;
                webClientData.Method = method;
                webClientData.FileName = fileName;
                webClientData.Timeout = timeout;
                webClientData.Trusted = trusted;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadFile;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return UploadFileViaClient(interpreter,
                    webClientData.ClientData, webClientData.Uri,
                    webClientData.Method, webClientData.FileName,
                    webClientData.Timeout, webClientData.Trusted,
                    ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadFileAsync(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Arguments = arguments;
                webClientData.CallbackFlags = callbackFlags;
                webClientData.Uri = uri;
                webClientData.Method = method;
                webClientData.FileName = fileName;
                webClientData.Timeout = timeout;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadFileAsynchronous;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }

            viaClient:

                return UploadFileAsyncViaClient(interpreter,
                    webClientData.ClientData, webClientData.Arguments,
                    webClientData.CallbackFlags, webClientData.Uri,
                    webClientData.Method, webClientData.FileName,
                    webClientData.Timeout, ref error);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Web Upload Methods
        #region Upload Data Via Client Methods
        private static ReturnCode UploadDataViaClient(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            string method,           /* in */
            byte[] rawData,          /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
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

                TraceOps.DebugTrace("UploadDataViaClient", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug,
                    true, "interpreter", interpreter, "clientData",
                    clientData, "uri", uri, "method", method,
                    "rawData", (rawData != null) ? rawData.Length :
                    Length.Invalid, "timeout", timeout, "trusted",
                    trusted, "wasTrusted", wasTrusted);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "UploadDataViaClient",
                            clientData, timeout, ref localError))
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

        private static ReturnCode UploadDataAsyncViaClient(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            byte[] rawData,              /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            TraceOps.DebugTrace("UploadDataAsyncViaClient", null,
                typeof(WebOps).Name, TracePriority.NetworkDebug,
                true, "interpreter", interpreter, "clientData",
                clientData, "arguments", arguments, "callbackFlags",
                callbackFlags, "uri", uri, "method", method,
                "rawData", (rawData != null) ? rawData.Length :
                Length.Invalid, "timeout", timeout);

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
                            interpreter, "UploadDataAsyncViaClient",
                            clientData, timeout, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new UploadDataTriplet(webClient, uri,
                                    new UploadDataPair(method, rawData)));

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

        #region Upload Values Via Client Methods
        private static ReturnCode UploadValuesViaClient(
            Interpreter interpreter,  /* in */
            IClientData clientData,   /* in */
            Uri uri,                  /* in */
            string method,            /* in */
            NameValueCollection data, /* in */
            int? timeout,             /* in */
            bool trusted,             /* in */
            ref byte[] bytes,         /* out */
            ref Result error          /* out */
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

                TraceOps.DebugTrace("UploadValuesViaClient", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug,
                    true, "interpreter", interpreter, "clientData",
                    clientData, "uri", uri, "method", method, "data",
                    (data != null) ? data.Count : Count.Invalid,
                    "timeout", timeout, "wasTrusted", wasTrusted);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "UploadValuesViaClient",
                            clientData, timeout, ref localError))
                    {
                        if (webClient != null)
                        {
                            bytes = webClient.UploadValues(
                                uri, method, data);

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

        private static ReturnCode UploadValuesAsyncViaClient(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            NameValueCollection data,    /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            TraceOps.DebugTrace("UploadValuesAsyncViaClient", null,
                typeof(WebOps).Name, TracePriority.NetworkDebug,
                true, "interpreter", interpreter, "clientData",
                clientData, "arguments", arguments, "callbackFlags",
                callbackFlags, "uri", uri, "method", method, "data",
                (data != null) ? data.Count : Count.Invalid,
                "timeout", timeout);

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
                            interpreter, "UploadValuesAsyncViaClient",
                            clientData, timeout, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new UploadValuesTriplet(webClient, uri,
                                    new UploadValuesPair(method, data)));

                            webClient.UploadValuesCompleted +=
                                new UploadValuesCompletedEventHandler(
                                    UploadValuesAsyncCompleted);

                            /* NO RESULT */
                            webClient.UploadValuesAsync(
                                uri, method, data, callback);
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

        #region Upload File Via Client Methods
        private static ReturnCode UploadFileViaClient(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            string method,           /* in */
            string fileName,         /* in */
            int? timeout,            /* in */
            bool trusted,            /* in */
            ref Result error         /* out */
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

                TraceOps.DebugTrace("UploadFileViaClient", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug,
                    true, "interpreter", interpreter, "clientData",
                    clientData, "uri", uri, "method", method,
                    "fileName", fileName, "timeout", timeout,
                    "trusted", trusted, "wasTrusted", wasTrusted);

                if ((wasTrusted != null) && (UpdateOps.SetTrusted(
                        true, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                try
                {
                    Result localError = null;

                    using (WebClient webClient = CreateClient(
                            interpreter, "UploadFileViaClient",
                            clientData, timeout, ref localError))
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

        private static ReturnCode UploadFileAsyncViaClient(
            Interpreter interpreter,     /* in */
            IClientData clientData,      /* in */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            int? timeout,                /* in */
            ref Result error             /* out */
            )
        {
            TraceOps.DebugTrace("UploadFileAsyncViaClient", null,
                typeof(WebOps).Name, TracePriority.NetworkDebug,
                true, "interpreter", interpreter, "clientData",
                clientData, "arguments", arguments, "callbackFlags",
                callbackFlags, "uri", uri, "method", method,
                "fileName", fileName, "timeout", timeout);

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
                            interpreter, "UploadFileAsyncViaClient",
                            clientData, timeout, ref localError);

                        if (webClient != null)
                        {
                            callback.ClientData = new ClientData(
                                new UploadFileTriplet(webClient, uri,
                                    new UploadFilePair(method, fileName)));

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
            int timeout,   /* in */
            bool allowNone /* in */
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
