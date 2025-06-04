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
using System.IO;
using System.Net;

#if WEB
using System.Web;

#if NET_STANDARD_20 && NET_CORE_REFERENCES
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
#endif
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using PerfOps = Eagle._Components.Private.PerformanceOps;

using SecurityProtocolType = System.Net.SecurityProtocolType;

#if TEST
using _SecurityProtocolType = Eagle._Components.Public.SecurityProtocolType;

#if NETWORK
using ScriptWebClient = Eagle._Tests.Default.ScriptWebClient;
#endif
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
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
        // HACK: This is purposely not read-only.
        //
        private static int offlineLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: *MAJOR* If this is non-zero, all requests may be retried
        //       UP TO this number of retries.  By default, this is zero,
        //       because there may be significant unintended consequences
        //       to this aggressive retry behavior.
        //
        // HACK: This is purposely not read-only.
        //
        private static int maximumRetries = 0;

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
        // NOTE: The default timeout for a sleep, which is normally used
        //       only between retrying a specific request.
        //
        // HACK: This is purposely not read-only.
        //
        private static int? DefaultSleepTime = null; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultViaClient = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultNoProtocol = false;

        ///////////////////////////////////////////////////////////////////////

#if TEST && NETWORK
        //
        // HACK: This is purposely not read-only.
        //
        private static string ScriptWebClientText = "::scriptWebClient";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region TagAndTimeoutWebClient Helper Class
        [ObjectId("c0cfe212-92b3-47f9-a1b6-fa0f69f6ff04")]
        private sealed class TagAndTimeoutWebClient : WebClient
        {
            #region Public Constructors
            public TagAndTimeoutWebClient(
                string tag,  /* in */
                int? timeout /* in */
                )
                : base()
            {
                this.tag = tag;
                this.timeout = timeout;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Properties
            private string tag;
            public string Tag
            {
                get { return tag; }
            }

            ///////////////////////////////////////////////////////////////////

            private int? timeout;
            public int? Timeout
            {
                get { return timeout; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            private static void MaybeSetTagHeader(
                WebRequest webRequest, /* in */
                string tag             /* in */
                )
            {
                if (String.IsNullOrEmpty(tag))
                    return;

                if (webRequest == null)
                    return;

                WebHeaderCollection headers = webRequest.Headers;

                if (headers == null)
                    return;

                headers[WebHeaders.Tag] = tag;
            }

            ///////////////////////////////////////////////////////////////////

            private static void MaybeSetVersionHeader(
                WebRequest webRequest /* in */
                )
            {
                string version = RuntimeOps.GetVersion(
                    VersionFlags.Default);

                if (String.IsNullOrEmpty(version))
                    return;

                if (webRequest == null)
                    return;

                WebHeaderCollection headers = webRequest.Headers;

                if (headers == null)
                    return;

                headers[WebHeaders.Version] = version;
            }

            ///////////////////////////////////////////////////////////////////

            private static void MaybeSetUserAgent(
                WebRequest webRequest, /* in */
                string tag             /* in */
                )
            {
                if (String.IsNullOrEmpty(tag))
                    return;

                HttpWebRequest httpWebRequest =
                    webRequest as HttpWebRequest;

                if (httpWebRequest == null)
                    return;

                string value = httpWebRequest.UserAgent;

                if (value != null)
                {
                    value = String.Format(
                        "{0}{1}{2}", value,
                        Characters.Space, tag);
                }
                else
                {
                    value = tag;
                }

                httpWebRequest.UserAgent = value;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Net.WebClient Overrides
            protected override WebRequest GetWebRequest(
                Uri address /* in */
                )
            {
                WebRequest webRequest = base.GetWebRequest(address);

                MaybeSetTagHeader(webRequest, tag);
                MaybeSetVersionHeader(webRequest);
                MaybeSetUserAgent(webRequest, tag);

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
            int count; /* REUSED */

            count = Interlocked.CompareExchange(ref offlineLevels, 0, 0);

            if (empty || (count != 0))
                localList.Add("OfflineLevels", count.ToString());

            count = Interlocked.CompareExchange(ref maximumRetries, 0, 0);

            if (empty || (count != 0))
                localList.Add("MaximumRetries", count.ToString());

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

        #region Private Error Helper Methods
        private static void MaybeAddError(
            ref ResultList errors, /* in, out */
            Result error           /* in: OPTIONAL */
            )
        {
            if (error != null)
            {
                if (errors == null)
                    errors = new ResultList();

                //
                // NOTE: Avoid duplicates here by first
                //       checking for an existing exact
                //       match.
                //
                if (errors.Find(error) == Index.Invalid)
                    errors.Add(error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static Result PrepareErrors(
            ResultList errors, /* in */
            int retries        /* in */
            )
        {
            if (errors != null)
            {
                if (retries > 0)
                    retries--;

                errors.Insert(0, String.Format(
                    "Retried web request {0} time(s).",
                    retries));
            }

            return errors;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Event Helper Methods
        private static StringList GetAsyncCompletedArguments(
            Uri uri,                          /* in: OPTIONAL */
            string method,                    /* in: OPTIONAL */
            byte[] rawData,                   /* in: OPTIONAL */
            NameValueCollection data,         /* in: OPTIONAL */
            string fileName,                  /* in: OPTIONAL */
            AsyncCompletedEventArgs eventArgs /* in: OPTIONAL */
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
            bool force,      /* in */
            bool obsolete,   /* in */
            ref Result error /* out */
            )
        {
            ReturnCode code = ReturnCode.Error;
            ResultList results = null; /* REUSED */

            if ((_Tests.Default.TestSetupSecurityProtocol(
                    force, !obsolete, ref results) == ReturnCode.Ok) &&
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

        #region Public Engine Helper Methods
        //
        // WARNING: This method is called directly by the engine.
        //
        public static Stream OpenScriptStream(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in */
            Uri uri,                 /* in */
            int? maximumRetries,     /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Stream stream;
                Result localError = null;

                stream = OpenScriptStreamOnce(
                    interpreter, clientData, uri, timeout,
                    ref localError);

                if (stream != null)
                    return stream;

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.OpenScriptStream;

                    object result = null;

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return result as Stream;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return null;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return new MemoryStream();
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Stream OpenScriptStreamOnce(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            int? timeout,            /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            using (WebClientData webClientData = new WebClientData())
            {
                webClientData.ClientData = clientData;
                webClientData.Uri = uri;
                webClientData.Timeout = timeout;
                webClientData.ViaClient = DefaultViaClient;

                WebTransferCallback callback = GetTransferCallback(
                    interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.OpenScriptStream;

                    if (InvokeTransferCallback(
                            callback, interpreter,
                            webFlags, webClientData,
                            ref error) == ReturnCode.Ok)
                    {
                        if (webClientData.ViaClient)
                            goto viaClient;

                        return webClientData.Stream;
                    }
                    else
                    {
                        return null;
                    }
                }

            viaClient:

                return OpenScriptStreamViaClient(
                    interpreter, clientData, uri, timeout, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static Stream OpenScriptStreamViaClient(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            int? timeout,            /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            try
            {
                Result localError = null;

                using (WebClient webClient = CreateClient(
                        interpreter, "OpenScriptStream",
                        clientData, timeout, ref localError))
                {
                    if (webClient != null)
                    {
                        return webClient.OpenRead(uri);
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
                TraceOps.DebugTrace(
                    e, typeof(WebOps).Name,
                    TracePriority.NetworkError);

                error = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Web Download / Upload Helper Methods
        #region WebClient Support Methods
        private static WebClient CreateClient(
            string argument, /* in */
            string tag,      /* in */
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
            else
            {
                if ((tag != null) || (timeout != null))
                {
                    TraceOps.DebugTrace("CreateClient",
                        null, typeof(WebOps).Name,
                        TracePriority.NetworkDebug,
                        true, "argument", argument,
                        "tag", tag, "timeout", timeout);

                    return new TagAndTimeoutWebClient(
                        tag, timeout);
                }
                else
                {
                    return new WebClient();
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private static WebTransferCallback GetTransferCallback(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            return (interpreter != null) ?
                interpreter.WebTransferCallback : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode InvokeTransferCallback(
            WebTransferCallback callback, /* in */
            Interpreter interpreter,      /* in: OPTIONAL */
            WebFlags webFlags,            /* in */
            IClientData clientData,       /* in: OPTIONAL */
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

                return callback( /* throw */
                    interpreter, webFlags, clientData, ref error);
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

        ///////////////////////////////////////////////////////////////////////

        private static WebErrorCallback GetErrorCallback(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            return (interpreter != null) ?
                interpreter.WebErrorCallback : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode InvokeErrorCallback(
            WebErrorCallback callback, /* in */
            Interpreter interpreter,   /* in */
            IClientData clientData,    /* in */
            Uri uri,                   /* in */
            WebFlags webFlags,         /* in */
            int retries,               /* in */
            int? timeout,              /* in */
            int? maximumRetries,       /* in */
            ref object result,         /* in, out */
            ref ResultList errors      /* in, out */
            )
        {
            try
            {
                TraceOps.DebugTrace("InvokeErrorCallback", null,
                    typeof(WebOps).Name, TracePriority.NetworkDebug2,
                    true, "callback", callback, "interpreter",
                    interpreter, "webFlags", webFlags, "retries",
                    retries, "clientData", clientData, "uri", uri,
                    "timeout", timeout, "maximumRetries",
                    maximumRetries, "result", result, "errors",
                    errors);

                if (callback == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid web error callback");
                    return ReturnCode.Error;
                }

                return callback( /* throw */
                    interpreter, clientData, uri, webFlags,
                    retries, timeout, maximumRetries, ref result,
                    ref errors);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(WebOps).Name,
                    TracePriority.NetworkError);

                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static int? GetTimeout(
            WebClient webClient /* in: OPTIONAL */
            )
        {
            if (webClient == null)
                return null;

            TagAndTimeoutWebClient localWebClient =
                webClient as TagAndTimeoutWebClient;

            if (localWebClient == null)
                return null;

            return localWebClient.Timeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetMillisecondsForRetry(
            int retries,            /* in */
            int maximumMilliseconds /* in */
            )
        {
            int milliseconds = (DefaultSleepTime != null) ?
                (int)DefaultSleepTime :            /* e.g. 500ms */
                4 * EventManager.MinimumSleepTime; /* e.g. 200ms */

            milliseconds *= retries;

            if (milliseconds < 0)
                milliseconds = 0;

            if (milliseconds > maximumMilliseconds)
                milliseconds = maximumMilliseconds;

            return milliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetTagEnvVarName(
            Interpreter interpreter, /* in: OPTIONAL */
            ContextIdType type       /* in */
            )
        {
            string format;
            long id;

            switch (type & ContextIdType.TypeMask)
            {
                case ContextIdType.Global:
                    {
                        format = EnvVars.WebClientTagFormat2;
                        id = 0; /* NOT USED (?) */
                        break;
                    }
                case ContextIdType.ParentProcess:
                    {
                        format = EnvVars.WebClientTagFormat1;
                        id = ProcessOps.GetParentId();
                        break;
                    }
                case ContextIdType.Process:
                    {
                        format = EnvVars.WebClientTagFormat1;
                        id = ProcessOps.GetId();
                        break;
                    }
                case ContextIdType.AppDomain:
                    {
                        format = EnvVars.WebClientTagFormat1;
                        id = AppDomainOps.GetCurrentId();
                        break;
                    }
                case ContextIdType.Thread:
                    {
                        format = EnvVars.WebClientTagFormat1;
                        id = GlobalState.GetCurrentSystemThreadId();
                        break;
                    }
                case ContextIdType.Interpreter:
                    {
                        if (interpreter != null)
                        {
                            format = EnvVars.WebClientTagFormat1;
                            id = interpreter.IdNoThrow;
                            break;
                        }
                        goto default;
                    }
                case ContextIdType.Context:
                    {
                        if (interpreter != null)
                        {
                            Result context = null;

                            if (interpreter.InternalGetContext(
                                    ref context) == ReturnCode.Ok)
                            {
                                format = EnvVars.WebClientTagFormat1;
                                id = (long)context.Value;
                                break;
                            }
                        }
                        goto default;
                    }
                default:
                    {
                        return null;
                    }
            }

            return String.Format(format, id);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool UnsetTagEnvVarValue(
            Interpreter interpreter, /* in: OPTIONAL */
            ContextIdType type       /* in */
            )
        {
            return CommonOps.Environment.UnsetVariable(
                GetTagEnvVarName(interpreter, type));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Web Download / Upload Helper Methods
        public static string GetTagEnvVarValue(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            foreach (string envVarName in new string[] {
                    GetTagEnvVarName(
                        interpreter, ContextIdType.Thread),
                    GetTagEnvVarName(
                        interpreter, ContextIdType.Process),
                    GetTagEnvVarName(
                        interpreter, ContextIdType.ParentProcess),
                    GetTagEnvVarName(
                        interpreter, ContextIdType.Global)
                })
            {
                string tag = CommonOps.Environment.GetVariable(
                    envVarName);

                if (String.IsNullOrEmpty(tag))
                    continue;

                return tag;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTagEnvVarValue(
            Interpreter interpreter, /* in: OPTIONAL */
            ContextIdType type       /* in */
            )
        {
            return CommonOps.Environment.GetVariable(
                GetTagEnvVarName(interpreter, type));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetTagEnvVarValue(
            Interpreter interpreter, /* in: OPTIONAL */
            ContextIdType type,      /* in */
            string tag               /* in: OPTIONAL */
            )
        {
            return CommonOps.Environment.SetVariable(
                GetTagEnvVarName(interpreter, type), tag);
        }

        ///////////////////////////////////////////////////////////////////////

#if WEB
        public static bool TrySetTagEnvVarValue(
            Interpreter interpreter, /* in: OPTIONAL */
            HttpRequest request,     /* in */
            ContextIdType type       /* in */
            )
        {
            bool maybeUnset = FlagOps.HasFlags(
                type, ContextIdType.MaybeUnset, true);

            if (request == null)
            {
                if (maybeUnset)
                    return UnsetTagEnvVarValue(interpreter, type);

                return false;
            }

#if NET_STANDARD_20
            IHeaderDictionary headers = request.Headers;
#else
            NameValueCollection headers = request.Headers;
#endif

            if (headers == null)
            {
                if (maybeUnset)
                    return UnsetTagEnvVarValue(interpreter, type);

                return false;
            }

            string tag = headers[WebHeaders.Tag];

            if (String.IsNullOrEmpty(tag))
            {
                if (maybeUnset)
                    return UnsetTagEnvVarValue(interpreter, type);

                return false;
            }

            return SetTagEnvVarValue(interpreter, type, tag);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static void SleepForRetry(
            Interpreter interpreter, /* in: OPTIONAL */
            EventWaitHandle @event,  /* in: OPTIONAL */
            int retries              /* in */
            )
        {
            int milliseconds = GetMillisecondsForRetry(retries,
                GetTimeoutOrDefault(interpreter, TimeoutType.Network));

            if (interpreter != null)
            {
                long microseconds =
                    PerfOps.GetMicrosecondsFromMilliseconds(
                        milliseconds);

                Result error = null;

                if (EventOps.Wait(
                        interpreter, @event, microseconds,
                        microseconds, true, false, false,
                        false, false, ref error) != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "SleepForRetry: milliseconds = {0}, " +
                        "error = {1}", milliseconds,
                        FormatOps.WrapOrNull(
                            true, false, error)),
                        typeof(WebOps).Name,
                        TracePriority.NetworkError);
                }
            }
            else
            {
                /* NO RESULT */
                HostOps.ThreadSleep(milliseconds);
            }
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

        #region Public Web Download Methods
        #region WebClient Support Methods
        public static WebClient CreateClient(
            Interpreter interpreter, /* in: OPTIONAL */
            string argument,         /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            return CreateClient(
                interpreter, argument, clientData,
                GetTagEnvVarValue(interpreter),
                timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static WebClient CreateClient(
            Interpreter interpreter, /* in: OPTIONAL */
            string argument,         /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            string tag,              /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
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
                            interpreter, ref  argument,
                            ref clientData, ref timeout,
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

#if TEST && NETWORK
                    if (interpreter.UseScriptWebClient())
                    {
                        return ScriptWebClient.Create(
                            interpreter, ScriptWebClientText,
                            argument, null, ref error);
                    }
#endif
                }
            }

            return CreateClient(argument, tag, timeout, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download Data Methods
        public static ReturnCode DownloadData(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            int? maximumRetries,     /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (DownloadDataOnce(
                        interpreter, clientData, uri,
                        timeout, trusted, ref bytes,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadData;

                    object result = null;

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                bytes = result as byte[];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                bytes = new byte[0];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadDataAsync(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            int? maximumRetries,         /* in: OPTIONAL */
            int? timeout,                /* in: OPTIONAL */
            ref Result error             /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (DownloadDataAsyncOnce(
                        interpreter, clientData, arguments,
                        callbackFlags, uri, timeout,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadDataAsynchronous;

                    object result = null; /* NOT USED */

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Methods
        public static ReturnCode DownloadFile(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string fileName,         /* in */
            int? maximumRetries,     /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (DownloadFileOnce(
                        interpreter, clientData, uri,
                        fileName, timeout, trusted,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadFile;

                    object result = null; /* NOT USED */

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadFileAsync(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string fileName,             /* in */
            int? maximumRetries,         /* in: OPTIONAL */
            int? timeout,                /* in: OPTIONAL */
            ref Result error             /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (DownloadFileAsyncOnce(
                        interpreter, clientData, arguments,
                        callbackFlags, uri, fileName, timeout,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.DownloadFileAsynchronous;

                    object result = null; /* NOT USED */

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Web Download Methods
        #region Download Data Via Client Methods
        private static ReturnCode DownloadDataOnce(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
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

        private static ReturnCode DownloadDataAsyncOnce(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            int? timeout,                /* in: OPTIONAL */
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

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DownloadDataViaClient(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted != null)
                {
                    UpdateOps.TryTrustedLock(ref locked);

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
                        (bool)trusted, ref error) != ReturnCode.Ok))
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

                UpdateOps.ExitTrustedLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DownloadDataAsyncViaClient(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            int? timeout,                /* in: OPTIONAL */
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

                    webClient = null;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Download File Via Client Methods
        private static ReturnCode DownloadFileOnce(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string fileName,         /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
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

        private static ReturnCode DownloadFileAsyncOnce(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string fileName,             /* in */
            int? timeout,                /* in: OPTIONAL */
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

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DownloadFileViaClient(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string fileName,         /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref Result error         /* out */
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted != null)
                {
                    UpdateOps.TryTrustedLock(ref locked);

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
                        (bool)trusted, ref error) != ReturnCode.Ok))
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

                UpdateOps.ExitTrustedLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode DownloadFileAsyncViaClient(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string fileName,             /* in */
            int? timeout,                /* in: OPTIONAL */
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

                    webClient = null;
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
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string method,           /* in */
            byte[] rawData,          /* in */
            int? maximumRetries,     /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (UploadDataOnce(
                        interpreter, clientData, uri, method,
                        rawData, timeout, trusted, ref bytes,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadData;

                    object result = null;

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                bytes = result as byte[];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                bytes = new byte[0];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadDataAsync(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            byte[] rawData,              /* in */
            int? maximumRetries,         /* in: OPTIONAL */
            int? timeout,                /* in: OPTIONAL */
            ref Result error             /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (UploadDataAsyncOnce(
                        interpreter, clientData, arguments,
                        callbackFlags, uri, method, rawData,
                        timeout, ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadDataAsynchronous;

                    object result = null; /* NOT USED */

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Methods
        public static ReturnCode UploadValues(
            Interpreter interpreter,  /* in: OPTIONAL */
            IClientData clientData,   /* in: OPTIONAL */
            Uri uri,                  /* in */
            string method,            /* in */
            NameValueCollection data, /* in */
            int? maximumRetries,      /* in: OPTIONAL */
            int? timeout,             /* in: OPTIONAL */
            bool? trusted,            /* in: OPTIONAL */
            ref byte[] bytes,         /* out */
            ref Result error          /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (UploadValuesOnce(
                        interpreter, clientData, uri,
                        method, data, timeout, trusted,
                        ref bytes, ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadValues;

                    object result = null;

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                bytes = result as byte[];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                bytes = new byte[0];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadValuesAsync(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            NameValueCollection data,    /* in */
            int? maximumRetries,         /* in: OPTIONAL */
            int? timeout,                /* in: OPTIONAL */
            ref Result error             /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (UploadValuesAsyncOnce(
                        interpreter, clientData, arguments,
                        callbackFlags, uri, method, data,
                        timeout, ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadValuesAsynchronous;

                    object result = null; /* NOT USED */

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Methods
        public static ReturnCode UploadFile(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string method,           /* in */
            string fileName,         /* in */
            int? maximumRetries,     /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (UploadFileOnce(
                        interpreter, clientData, uri, method,
                        fileName, timeout, trusted, ref bytes,
                        ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadFile;

                    object result = null;

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                bytes = result as byte[];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                bytes = new byte[0];
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadFileAsync(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            int? maximumRetries,         /* in: OPTIONAL */
            int? timeout,                /* in: OPTIONAL */
            ref Result error             /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                Result localError = null;

                if (UploadFileAsyncOnce(
                        interpreter, clientData, arguments,
                        callbackFlags, uri, method, fileName,
                        timeout, ref localError) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.UploadFileAsynchronous;

                    object result = null; /* NOT USED */

                    switch (InvokeErrorCallback(
                            callback, interpreter, clientData,
                            uri, webFlags, retries, timeout,
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return ReturnCode.Error;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return ReturnCode.Ok;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Web Upload Methods
        #region Upload Data Via Client Methods
        private static ReturnCode UploadDataOnce(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string method,           /* in */
            byte[] rawData,          /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
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

        private static ReturnCode UploadDataAsyncOnce(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            byte[] rawData,              /* in */
            int? timeout,                /* in: OPTIONAL */
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

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadDataViaClient(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string method,           /* in */
            byte[] rawData,          /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted != null)
                {
                    UpdateOps.TryTrustedLock(ref locked);

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
                        (bool)trusted, ref error) != ReturnCode.Ok))
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

                UpdateOps.ExitTrustedLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadDataAsyncViaClient(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            byte[] rawData,              /* in */
            int? timeout,                /* in: OPTIONAL */
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

                    webClient = null;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload Values Via Client Methods
        private static ReturnCode UploadValuesOnce(
            Interpreter interpreter,  /* in: OPTIONAL */
            IClientData clientData,   /* in: OPTIONAL */
            Uri uri,                  /* in */
            string method,            /* in */
            NameValueCollection data, /* in */
            int? timeout,             /* in: OPTIONAL */
            bool? trusted,            /* in */
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

        private static ReturnCode UploadValuesAsyncOnce(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            NameValueCollection data,    /* in */
            int? timeout,                /* in: OPTIONAL */
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

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadValuesViaClient(
            Interpreter interpreter,  /* in: OPTIONAL */
            IClientData clientData,   /* in: OPTIONAL */
            Uri uri,                  /* in */
            string method,            /* in */
            NameValueCollection data, /* in */
            int? timeout,             /* in: OPTIONAL */
            bool? trusted,            /* in */
            ref byte[] bytes,         /* out */
            ref Result error          /* out */
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted != null)
                {
                    UpdateOps.TryTrustedLock(ref locked);

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
                        (bool)trusted, ref error) != ReturnCode.Ok))
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

                UpdateOps.ExitTrustedLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadValuesAsyncViaClient(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            NameValueCollection data,    /* in */
            int? timeout,                /* in: OPTIONAL */
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

                    webClient = null;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Upload File Via Client Methods
        private static ReturnCode UploadFileOnce(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string method,           /* in */
            string fileName,         /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
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

                        bytes = webClientData.Bytes;
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
                    ref bytes, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadFileAsyncOnce(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            int? timeout,                /* in: OPTIONAL */
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

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadFileViaClient(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            Uri uri,                 /* in */
            string method,           /* in */
            string fileName,         /* in */
            int? timeout,            /* in: OPTIONAL */
            bool? trusted,           /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            bool locked = false;
            bool? wasTrusted = null;

            try
            {
                if (trusted != null)
                {
                    UpdateOps.TryTrustedLock(ref locked);

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
                        (bool)trusted, ref error) != ReturnCode.Ok))
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
                            bytes = webClient.UploadFile(
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

                UpdateOps.ExitTrustedLock(ref locked);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode UploadFileAsyncViaClient(
            Interpreter interpreter,     /* in: OPTIONAL */
            IClientData clientData,      /* in: OPTIONAL */
            StringList arguments,        /* in: OPTIONAL */
            CallbackFlags callbackFlags, /* in */
            Uri uri,                     /* in */
            string method,               /* in */
            string fileName,             /* in */
            int? timeout,                /* in: OPTIONAL */
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

                    webClient = null;
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

        public static bool GetDefaultNoProtocol()
        {
            return DefaultNoProtocol;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetMaximumRetries()
        {
            return Interlocked.CompareExchange(ref maximumRetries, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int SetMaximumRetries(
            int retries /* in */
            )
        {
            return Interlocked.Exchange(ref maximumRetries, retries);
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
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType, /* in */
            int? timeout             /* in: OPTIONAL */
            )
        {
            if (timeout != null)
            {
                int localTimeout = (int)timeout;

                if (IsGoodTimeout(localTimeout, true))
                    return localTimeout;
            }

            return GetTimeout(interpreter, timeoutType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int? GetTimeout(
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType  /* in */
            )
        {
            int timeout; /* REUSED */

            if (interpreter != null)
            {
                int? localTimeout = interpreter.InternalGetTimeout(
                    timeoutType); /* OPTIONAL */

                if (localTimeout != null)
                {
                    timeout = (int)localTimeout;

                    if (IsGoodTimeout(timeout, true))
                        return timeout;
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
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType  /* in */
            )
        {
            int? timeout = GetTimeout(interpreter, timeoutType);

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
            Interpreter interpreter,  /* in: OPTIONAL */
            WebClient webClient,      /* in */
            Uri uri,                  /* in */
            int? maximumRetries,      /* in: OPTIONAL */
            NameValueCollection data, /* in: OPTIONAL */
            IProfilerState profiler,  /* in: OPTIONAL */
            bool raw,                 /* in */
            ref Result error          /* out */
            )
        {
            int localMaximumRetries = (maximumRetries != null) ?
                (int)maximumRetries : GetMaximumRetries();

            int retries = 0;
            ResultList errors = null;

            while (true)
            {
                //
                // TODO: If timedOut when check if TLS is
                //       broken due to Windows 11, etc, and retry?
                //
                object stringOrBytes;
                Result localError = null;

                stringOrBytes = MakeRequestOnce(
                    interpreter, webClient, uri, data,
                    profiler, raw, ref localError);

                if (stringOrBytes != null)
                    return stringOrBytes;

                MaybeAddError(ref errors, localError);

                WebErrorCallback callback = GetErrorCallback(interpreter);

                if (callback != null)
                {
                    WebFlags webFlags = WebFlags.MakeRequest;

                    if (data != null)
                        webFlags |= WebFlags.Values;
                    else
                        webFlags |= WebFlags.String;

                    object result = null;

                    switch (InvokeErrorCallback(callback,
                            interpreter, new ClientData(data),
                            uri, webFlags, 0, GetTimeout(webClient),
                            maximumRetries, ref result, ref errors))
                    {
                        case ReturnCode.Ok:
                            {
                                //
                                // NOTE: This return code means
                                //       that the callback says
                                //       it succeeded and valid
                                //       data is being returned.
                                //
                                return result;
                            }
                        case ReturnCode.Error:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fail right now
                                //       by returning null and
                                //       the error collection.
                                //
                                error = errors;
                                return null;
                            }
                        case ReturnCode.Return:
                            {
                                //
                                // NOTE: This return code means
                                //       that we fake "success"
                                //       by returning an empty
                                //       result.
                                //
                                // NOTE: When asynchronous, it
                                //       this will be the same
                                //       as "Ok".
                                //
                                return raw ?
                                    (object)new byte[0] :
                                    (object)String.Empty;
                            }
                        case ReturnCode.Break:
                            {
                                //
                                // NOTE: This return code means
                                //       that we bump the retry
                                //       count and continue with
                                //       default handling.
                                //
                                retries++;
                                break;
                            }
                        case ReturnCode.Continue:
                            {
                                //
                                // NOTE: This return code means
                                //       the callback didn't do
                                //       anything substantive
                                //       and we should continue
                                //       with default handling.
                                //
                                break;
                            }
                    }
                }

                if ((localMaximumRetries <= 0) ||
                    (++retries > localMaximumRetries))
                {
                    break;
                }

                /* NO RESULT */
                SleepForRetry(interpreter, null, retries);
            }

            if (errors != null)
                error = PrepareErrors(errors, retries);

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Wrapper Methods
        private static object MakeRequestOnce(
            Interpreter interpreter,  /* in: NOT USED */
            WebClient webClient,      /* in */
            Uri uri,                  /* in */
            NameValueCollection data, /* in: OPTIONAL */
            IProfilerState profiler,  /* in: OPTIONAL */
            bool raw,                 /* in */
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
                else if (raw)
                    return webClient.DownloadData(uri);
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
