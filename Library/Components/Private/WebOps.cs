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
    /// <summary>
    /// This class provides the private helper methods used by the Eagle core
    /// to perform network operations via the <see cref="WebClient" /> class,
    /// including downloading data and files, uploading data, values, and files
    /// (synchronously and asynchronously), opening script streams over the
    /// network, managing offline mode and request retry behavior, and
    /// configuring request timeouts and the HTTPS security protocol.
    /// </summary>
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
        /// <summary>
        /// When non-zero, any attempt to create a <see cref="WebClient" /> via
        /// this class will fail, preventing any network access using the
        /// <see cref="WebClient" /> class.
        /// </summary>
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
        /// <summary>
        /// When non-zero, any request may be retried up to this number of
        /// times.  By default, this is zero, because there may be significant
        /// unintended consequences to this aggressive retry behavior.
        /// </summary>
        private static int maximumRetries = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default timeout for a request, in milliseconds.  If
        //       this value is null, there is no explicit timeout, i.e.
        //       it will be up to the .NET Framework and/or Windows.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default timeout for a request, in milliseconds.  When this
        /// value is null, there is no explicit timeout; it will be up to the
        /// .NET Framework and/or the operating system.
        /// </summary>
        private static int? DefaultTimeout = null; /* COMPAT: Eagle beta. */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default timeout for a sleep, which is normally used
        //       only between retrying a specific request.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default timeout for a sleep, in milliseconds, which is normally
        /// used only between retrying a specific request.
        /// </summary>
        private static int? DefaultSleepTime = null; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, web transfer operations are performed via a
        /// <see cref="WebClient" /> by default, even when a transfer callback
        /// has been configured.
        /// </summary>
        private static bool DefaultViaClient = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the HTTPS security protocol is not configured by
        /// default prior to making a request.
        /// </summary>
        private static bool DefaultNoProtocol = false;

        ///////////////////////////////////////////////////////////////////////

#if TEST && NETWORK
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The name of the script-based <see cref="WebClient" /> object used
        /// for testing purposes.
        /// </summary>
        private static string ScriptWebClientText = "::scriptWebClient";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region TagAndTimeoutWebClient Helper Class
        /// <summary>
        /// This class represents a <see cref="WebClient" /> that adds custom
        /// request headers (a tag, the engine version, and a user-agent
        /// suffix) to each outgoing request and optionally applies a request
        /// timeout.
        /// </summary>
        [ObjectId("c0cfe212-92b3-47f9-a1b6-fa0f69f6ff04")]
        private sealed class TagAndTimeoutWebClient : WebClient
        {
            #region Public Constructors
            /// <summary>
            /// Constructs a new instance of this class.
            /// </summary>
            /// <param name="tag">
            /// The tag to add to each outgoing request, via custom request
            /// headers, or null to add no tag.
            /// </param>
            /// <param name="timeout">
            /// The timeout, in milliseconds, to apply to each outgoing
            /// request, or null to apply no explicit timeout.
            /// </param>
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
            /// <summary>
            /// The tag to add to each outgoing request, via custom request
            /// headers, or null if there is no tag.
            /// </summary>
            private string tag;
            /// <summary>
            /// Gets the tag added to each outgoing request, via custom request
            /// headers, or null if there is no tag.
            /// </summary>
            public string Tag
            {
                get { return tag; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The timeout, in milliseconds, applied to each outgoing request,
            /// or null if there is no explicit timeout.
            /// </summary>
            private int? timeout;
            /// <summary>
            /// Gets the timeout, in milliseconds, applied to each outgoing
            /// request, or null if there is no explicit timeout.
            /// </summary>
            public int? Timeout
            {
                get { return timeout; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            /// <summary>
            /// Adds the specified tag to the request headers of the specified
            /// web request, unless the tag or web request is invalid.
            /// </summary>
            /// <param name="webRequest">
            /// The web request to modify.  When null, no action is taken.
            /// </param>
            /// <param name="tag">
            /// The tag to add to the request headers.  When null or empty, no
            /// action is taken.
            /// </param>
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

            /// <summary>
            /// Adds the engine version to the request headers of the specified
            /// web request, unless the version is unavailable or the web
            /// request is invalid.
            /// </summary>
            /// <param name="webRequest">
            /// The web request to modify.  When null, no action is taken.
            /// </param>
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

            /// <summary>
            /// Appends the specified tag to the user-agent of the specified
            /// web request, when it is an <see cref="HttpWebRequest" />, unless
            /// the tag is invalid.
            /// </summary>
            /// <param name="webRequest">
            /// The web request to modify.  When this is not an
            /// <see cref="HttpWebRequest" />, no action is taken.
            /// </param>
            /// <param name="tag">
            /// The tag to append to the user-agent.  When null or empty, no
            /// action is taken.
            /// </param>
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
            /// <summary>
            /// Creates and returns the <see cref="WebRequest" /> for the
            /// specified <see cref="Uri" />, adding the configured custom
            /// request headers and applying the configured request timeout.
            /// </summary>
            /// <param name="address">
            /// The <see cref="Uri" /> of the resource being requested.
            /// </param>
            /// <returns>
            /// The created <see cref="WebRequest" />.
            /// </returns>
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
        /// <summary>
        /// This method adds rows describing the current web-related state of
        /// this class to the specified list.  It is used when building the list
        /// of engine information.
        /// </summary>
        /// <param name="list">
        /// The list to add the web information to.  When null, no action is
        /// taken.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included.
        /// </param>
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
        /// <summary>
        /// This method adds the specified error to the specified error list,
        /// creating the list if necessary and avoiding the addition of an exact
        /// duplicate.
        /// </summary>
        /// <param name="errors">
        /// The error list to add the error to.  When null, a new list is
        /// created.
        /// </param>
        /// <param name="error">
        /// The error to add.  When null, no action is taken.
        /// </param>
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

        /// <summary>
        /// This method inserts a summary message describing the number of
        /// retries at the start of the specified error list.
        /// </summary>
        /// <param name="errors">
        /// The error list to modify.  When null, no action is taken.
        /// </param>
        /// <param name="retries">
        /// The number of times the web request was retried.
        /// </param>
        /// <returns>
        /// The error list, as a result, or null if there were no errors.
        /// </returns>
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
        /// <summary>
        /// This method builds the list of name/value arguments that describe a
        /// completed asynchronous web operation, for use when firing an event
        /// handler or invoking a callback.
        /// </summary>
        /// <param name="uri">
        /// The <see cref="Uri" /> associated with the operation, or null if it
        /// is not applicable.
        /// </param>
        /// <param name="method">
        /// The HTTP method associated with the operation, or null if it is not
        /// applicable.
        /// </param>
        /// <param name="rawData">
        /// The raw data associated with the operation, or null if it is not
        /// applicable.
        /// </param>
        /// <param name="data">
        /// The name/value collection associated with the operation, or null if
        /// it is not applicable.
        /// </param>
        /// <param name="fileName">
        /// The file name associated with the operation, or null if it is not
        /// applicable.
        /// </param>
        /// <param name="eventArgs">
        /// The event arguments describing the completed operation, or null if
        /// they are not applicable.
        /// </param>
        /// <returns>
        /// The list of name/value arguments describing the completed operation.
        /// </returns>
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
        /// <summary>
        /// This method probes for the best available HTTPS security protocol
        /// and adds the result to the specified list.
        /// </summary>
        /// <param name="list">
        /// Upon success, receives the name/value pairs describing the probed
        /// security protocol.  When null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method queries the currently configured HTTPS security
        /// protocol, together with the best available protocol, and adds the
        /// results to the specified list.
        /// </summary>
        /// <param name="list">
        /// Upon success, receives the name/value pairs describing the current
        /// and best security protocols.  When null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method configures the HTTPS security protocol for use by
        /// subsequent web requests.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the security protocol to be reconfigured even if
        /// it appears to have already been set up.
        /// </param>
        /// <param name="obsolete">
        /// Non-zero to permit obsolete security protocols to be included.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method opens a stream for reading a script from the specified
        /// <see cref="Uri" />, retrying the request and consulting any
        /// configured web error callback as necessary.  It is called directly
        /// by the engine.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the script to open.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The opened stream upon success, or null upon failure.
        /// </returns>
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

        /// <summary>
        /// This method attempts, exactly once, to open a stream for reading a
        /// script from the specified <see cref="Uri" />, first consulting any
        /// configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the script to open.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The opened stream upon success, or null upon failure.
        /// </returns>
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

        /// <summary>
        /// This method opens a stream for reading a script from the specified
        /// <see cref="Uri" /> using a <see cref="WebClient" />.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the script to open.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The opened stream upon success, or null upon failure.
        /// </returns>
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
        /// <summary>
        /// This method creates a new <see cref="WebClient" />, optionally one
        /// that adds a tag and applies a timeout, unless this class is
        /// currently in offline mode.
        /// </summary>
        /// <param name="argument">
        /// A description of the operation requesting the web client, used for
        /// diagnostic purposes.
        /// </param>
        /// <param name="tag">
        /// The tag to add to each outgoing request, via custom request headers,
        /// or null to add no tag.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to apply to each outgoing request, or
        /// null to apply no explicit timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The created <see cref="WebClient" /> upon success, or null upon
        /// failure.
        /// </returns>
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

        /// <summary>
        /// This method returns the web transfer callback configured for the
        /// specified interpreter, if any.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <returns>
        /// The configured web transfer callback, or null if there is none.
        /// </returns>
        private static WebTransferCallback GetTransferCallback(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            return (interpreter != null) ?
                interpreter.WebTransferCallback : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified web transfer callback, trapping
        /// and reporting any exception it raises.
        /// </summary>
        /// <param name="callback">
        /// The web transfer callback to invoke.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="webFlags">
        /// The flags describing the web operation being performed.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method returns the web error callback configured for the
        /// specified interpreter, if any.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <returns>
        /// The configured web error callback, or null if there is none.
        /// </returns>
        private static WebErrorCallback GetErrorCallback(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            return (interpreter != null) ?
                interpreter.WebErrorCallback : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified web error callback, trapping and
        /// reporting any exception it raises.
        /// </summary>
        /// <param name="callback">
        /// The web error callback to invoke.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> associated with the failed request.
        /// </param>
        /// <param name="webFlags">
        /// The flags describing the web operation being performed.
        /// </param>
        /// <param name="retries">
        /// The number of times the request has been retried so far.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null if there is none.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="result">
        /// Upon success, may receive the result produced by the callback.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered so far, which may be added to by the
        /// callback.  When null, a new list is created as needed.
        /// </param>
        /// <returns>
        /// The return code produced by the callback, which controls how the
        /// caller proceeds.
        /// </returns>
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

        /// <summary>
        /// This method returns the timeout, in milliseconds, associated with
        /// the specified web client, when it is a tag-and-timeout web client.
        /// </summary>
        /// <param name="webClient">
        /// The web client to query.  When null, or not a tag-and-timeout web
        /// client, null is returned.
        /// </param>
        /// <returns>
        /// The timeout, in milliseconds, or null if there is none.
        /// </returns>
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

        /// <summary>
        /// This method computes the number of milliseconds to sleep before the
        /// specified retry attempt, scaling with the retry count and clamping
        /// to the specified maximum.
        /// </summary>
        /// <param name="retries">
        /// The current retry attempt number, used to scale the sleep time.
        /// </param>
        /// <param name="maximumMilliseconds">
        /// The maximum number of milliseconds to sleep.
        /// </param>
        /// <returns>
        /// The number of milliseconds to sleep before the retry.
        /// </returns>
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

        /// <summary>
        /// This method builds the name of the environment variable used to
        /// store the web client tag for the specified context identifier type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which identifier (e.g.
        /// process, thread, interpreter, etc.) is used to form the variable
        /// name.
        /// </param>
        /// <returns>
        /// The environment variable name, or null if one cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method unsets the environment variable used to store the web
        /// client tag for the specified context identifier type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment variable
        /// to unset.
        /// </param>
        /// <returns>
        /// True if the environment variable was unset; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method returns the web client tag value by checking the
        /// thread, process, parent process, and global context environment
        /// variables, in that order, returning the first non-empty value
        /// found.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <returns>
        /// The first non-empty web client tag value found, or null if none was
        /// found.
        /// </returns>
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

        /// <summary>
        /// This method returns the web client tag value stored in the
        /// environment variable for the specified context identifier type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment variable
        /// to query.
        /// </param>
        /// <returns>
        /// The web client tag value, or null if there is none.
        /// </returns>
        public static string GetTagEnvVarValue(
            Interpreter interpreter, /* in: OPTIONAL */
            ContextIdType type       /* in */
            )
        {
            return CommonOps.Environment.GetVariable(
                GetTagEnvVarName(interpreter, type));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the environment variable used to store the web
        /// client tag for the specified context identifier type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment variable
        /// to set.
        /// </param>
        /// <param name="tag">
        /// The web client tag value to store, or null to clear it.
        /// </param>
        /// <returns>
        /// True if the environment variable was set; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to set the web client tag environment variable
        /// for the specified context identifier type from the tag request
        /// header of the specified HTTP request, optionally unsetting it when
        /// no tag is available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="request">
        /// The HTTP request whose tag header is used.  When null, the variable
        /// may be unset.
        /// </param>
        /// <param name="type">
        /// The context identifier type that selects which environment variable
        /// to set or unset.
        /// </param>
        /// <returns>
        /// True if the environment variable was set or unset; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method sleeps for the amount of time appropriate to the
        /// specified retry attempt, using the interpreter event subsystem when
        /// an interpreter is available, or a plain thread sleep otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null to use a plain thread sleep.
        /// </param>
        /// <param name="event">
        /// The event that, when signaled, can interrupt the wait, or null for
        /// none.
        /// </param>
        /// <param name="retries">
        /// The current retry attempt number, used to scale the sleep time.
        /// </param>
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
        /// <summary>
        /// This method handles completion of an asynchronous data download,
        /// disposing the associated web client and firing the configured
        /// callback event handler with the completion arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the completed data download.
        /// </param>
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
        /// <summary>
        /// This method handles completion of an asynchronous file download,
        /// disposing the associated web client and invoking the configured
        /// callback with the completion arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the completed file download.
        /// </param>
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
        /// <summary>
        /// This method creates a new <see cref="WebClient" /> for the specified
        /// interpreter, using the configured web client tag environment
        /// variable value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="argument">
        /// A description of the operation requesting the web client, used for
        /// diagnostic purposes.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to apply to each outgoing request, or
        /// null to apply no explicit timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The created <see cref="WebClient" /> upon success, or null upon
        /// failure.
        /// </returns>
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

        /// <summary>
        /// This method creates a new <see cref="WebClient" /> for the specified
        /// interpreter, consulting any pre-create and new-client callbacks and
        /// honoring offline mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="argument">
        /// A description of the operation requesting the web client, used for
        /// diagnostic purposes.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="tag">
        /// The tag to add to each outgoing request, via custom request headers,
        /// or null to add no tag.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to apply to each outgoing request, or
        /// null to apply no explicit timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The created <see cref="WebClient" /> upon success, or null upon
        /// failure.
        /// </returns>
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
        /// <summary>
        /// This method downloads the data at the specified <see cref="Uri" />,
        /// retrying the request and consulting any configured web error
        /// callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the downloaded data.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous download of the data at the
        /// specified <see cref="Uri" />, retrying the request and consulting
        /// any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the download finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method downloads the resource at the specified
        /// <see cref="Uri" /> to a local file, retrying the request and
        /// consulting any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous download of the resource at the
        /// specified <see cref="Uri" /> to a local file, retrying the request
        /// and consulting any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the download finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method attempts, exactly once, to download the data at the
        /// specified <see cref="Uri" />, first consulting any configured web
        /// transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the downloaded data.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method attempts, exactly once, to begin an asynchronous
        /// download of the data at the specified <see cref="Uri" />, first
        /// consulting any configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the download finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method downloads the data at the specified <see cref="Uri" />
        /// using a <see cref="WebClient" />, optionally adjusting the update
        /// trust setting for the duration of the download.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the downloaded data.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous download of the data at the
        /// specified <see cref="Uri" /> using a <see cref="WebClient" />,
        /// wiring up the completion event handler and callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the download finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the data to download.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method attempts, exactly once, to download the resource at the
        /// specified <see cref="Uri" /> to a local file, first consulting any
        /// configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method attempts, exactly once, to begin an asynchronous
        /// download of the resource at the specified <see cref="Uri" /> to a
        /// local file, first consulting any configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the download finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method downloads the resource at the specified
        /// <see cref="Uri" /> to a local file using a <see cref="WebClient" />,
        /// optionally adjusting the update trust setting for the duration of
        /// the download.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the download with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous download of the resource at the
        /// specified <see cref="Uri" /> to a local file using a
        /// <see cref="WebClient" />, wiring up the completion event handler and
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the download finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the resource to download.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to write the downloaded resource to.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method handles completion of an asynchronous data upload,
        /// disposing the associated web client and firing the configured
        /// callback event handler with the completion arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the completed data upload.
        /// </param>
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
        /// <summary>
        /// This method handles completion of an asynchronous values upload,
        /// disposing the associated web client and firing the configured
        /// callback event handler with the completion arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the completed values upload.
        /// </param>
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
        /// <summary>
        /// This method handles completion of an asynchronous file upload,
        /// disposing the associated web client and invoking the configured
        /// callback with the completion arguments.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The event arguments describing the completed file upload.
        /// </param>
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
        /// <summary>
        /// This method uploads the specified raw data to the specified
        /// <see cref="Uri" />, retrying the request and consulting any
        /// configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous upload of the specified raw data
        /// to the specified <see cref="Uri" />, retrying the request and
        /// consulting any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method uploads the specified name/value collection to the
        /// specified <see cref="Uri" />, retrying the request and consulting
        /// any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous upload of the specified
        /// name/value collection to the specified <see cref="Uri" />, retrying
        /// the request and consulting any configured web error callback as
        /// necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method uploads the specified local file to the specified
        /// <see cref="Uri" />, retrying the request and consulting any
        /// configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the file to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous upload of the specified local
        /// file to the specified <see cref="Uri" />, retrying the request and
        /// consulting any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the file to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to upload.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method attempts, exactly once, to upload the specified raw data
        /// to the specified <see cref="Uri" />, first consulting any configured
        /// web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method attempts, exactly once, to begin an asynchronous upload
        /// of the specified raw data to the specified <see cref="Uri" />, first
        /// consulting any configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method uploads the specified raw data to the specified
        /// <see cref="Uri" /> using a <see cref="WebClient" />, optionally
        /// adjusting the update trust setting for the duration of the upload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous upload of the specified raw data
        /// to the specified <see cref="Uri" /> using a <see cref="WebClient" />,
        /// wiring up the completion event handler and callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the data to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="rawData">
        /// The raw data to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method attempts, exactly once, to upload the specified
        /// name/value collection to the specified <see cref="Uri" />, first
        /// consulting any configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method attempts, exactly once, to begin an asynchronous upload
        /// of the specified name/value collection to the specified
        /// <see cref="Uri" />, first consulting any configured web transfer
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method uploads the specified name/value collection to the
        /// specified <see cref="Uri" /> using a <see cref="WebClient" />,
        /// optionally adjusting the update trust setting for the duration of
        /// the upload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous upload of the specified
        /// name/value collection to the specified <see cref="Uri" /> using a
        /// <see cref="WebClient" />, wiring up the completion event handler and
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the values to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method attempts, exactly once, to upload the specified local
        /// file to the specified <see cref="Uri" />, first consulting any
        /// configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the file to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method attempts, exactly once, to begin an asynchronous upload
        /// of the specified local file to the specified <see cref="Uri" />,
        /// first consulting any configured web transfer callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the file to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method uploads the specified local file to the specified
        /// <see cref="Uri" /> using a <see cref="WebClient" />, optionally
        /// adjusting the update trust setting for the duration of the upload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the file to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to perform the upload with the update trust setting
        /// temporarily changed, or null to leave it unchanged.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the response data returned by the server.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous upload of the specified local
        /// file to the specified <see cref="Uri" /> using a
        /// <see cref="WebClient" />, wiring up the completion event handler and
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The extra data associated with this request, if any.
        /// </param>
        /// <param name="arguments">
        /// The arguments used to construct the completion callback that is
        /// invoked when the upload finishes.
        /// </param>
        /// <param name="callbackFlags">
        /// The flags used when constructing the completion callback.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> to upload the file to.
        /// </param>
        /// <param name="method">
        /// The HTTP method to use for the upload.
        /// </param>
        /// <param name="fileName">
        /// The name of the local file to upload.
        /// </param>
        /// <param name="timeout">
        /// The request timeout, in milliseconds, or null to use the configured
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified timeout value is a
        /// valid, usable request timeout.
        /// </summary>
        /// <param name="timeout">
        /// The timeout value, in milliseconds, to check.
        /// </param>
        /// <param name="allowNone">
        /// Non-zero to treat the "none" timeout sentinel as valid.
        /// </param>
        /// <returns>
        /// True if the timeout value is valid; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether this class is currently in offline
        /// mode, in which case the creation of web clients is prevented.
        /// </summary>
        /// <returns>
        /// True if this class is in offline mode; otherwise, false.
        /// </returns>
        public static bool InOfflineMode()
        {
            return Interlocked.CompareExchange(ref offlineLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default value indicating whether the HTTPS
        /// security protocol should be left unconfigured prior to making a
        /// request.
        /// </summary>
        /// <returns>
        /// True if the security protocol should not be configured by default;
        /// otherwise, false.
        /// </returns>
        public static bool GetDefaultNoProtocol()
        {
            return DefaultNoProtocol;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the configured default maximum number of times a
        /// web request may be retried.
        /// </summary>
        /// <returns>
        /// The configured maximum number of retries.
        /// </returns>
        public static int GetMaximumRetries()
        {
            return Interlocked.CompareExchange(ref maximumRetries, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the configured default maximum number of times a
        /// web request may be retried.
        /// </summary>
        /// <param name="retries">
        /// The new maximum number of retries.
        /// </param>
        /// <returns>
        /// The previous maximum number of retries.
        /// </returns>
        public static int SetMaximumRetries(
            int retries /* in */
            )
        {
            return Interlocked.Exchange(ref maximumRetries, retries);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables offline mode by incrementing or
        /// decrementing the offline level count.
        /// </summary>
        /// <param name="offline">
        /// Non-zero to enter offline mode (increment the level); zero to leave
        /// offline mode (decrement the level).
        /// </param>
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

        /// <summary>
        /// This method returns the request timeout to use, preferring the
        /// specified timeout when it is valid and otherwise falling back to the
        /// configured timeout for the specified timeout type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="timeoutType">
        /// The type of timeout to obtain when no valid explicit timeout is
        /// provided.
        /// </param>
        /// <param name="timeout">
        /// An explicit timeout, in milliseconds, to prefer when valid, or null
        /// for none.
        /// </param>
        /// <returns>
        /// The timeout to use, in milliseconds, or null if there is none.
        /// </returns>
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

        /// <summary>
        /// This method returns the configured request timeout for the specified
        /// timeout type, checking the interpreter, the global configuration,
        /// and finally the configured default.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="timeoutType">
        /// The type of timeout to obtain.
        /// </param>
        /// <returns>
        /// The timeout to use, in milliseconds, or null if there is none.
        /// </returns>
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

        /// <summary>
        /// This method returns the configured request timeout for the specified
        /// timeout type, falling back to a non-null default value derived from
        /// the thread subsystem when no valid configured timeout is available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="timeoutType">
        /// The type of timeout to obtain.
        /// </param>
        /// <returns>
        /// The timeout to use, in milliseconds.
        /// </returns>
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
        /// <summary>
        /// This method performs a web request using the specified
        /// <see cref="WebClient" />, either uploading values, downloading raw
        /// data, or downloading a string, retrying the request and consulting
        /// any configured web error callback as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="webClient">
        /// The web client to use to perform the request.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the request.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// configured default.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.  When non-null, the request is
        /// a values upload; otherwise, it is a download.
        /// </param>
        /// <param name="profiler">
        /// The profiler used to time the request, or null for none.
        /// </param>
        /// <param name="raw">
        /// Non-zero to download raw data; zero to download a string.  Only used
        /// when no values are being uploaded.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The response from the request -- a byte array or a string -- upon
        /// success, or null upon failure.
        /// </returns>
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
        /// <summary>
        /// This method performs a web request exactly once using the specified
        /// <see cref="WebClient" />, optionally timing it with a profiler.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter is not used.
        /// </param>
        /// <param name="webClient">
        /// The web client to use to perform the request.
        /// </param>
        /// <param name="uri">
        /// The <see cref="Uri" /> of the request.
        /// </param>
        /// <param name="data">
        /// The name/value collection to upload.  When non-null, the request is
        /// a values upload; otherwise, it is a download.
        /// </param>
        /// <param name="profiler">
        /// The profiler used to time the request, or null for none.
        /// </param>
        /// <param name="raw">
        /// Non-zero to download raw data; zero to download a string.  Only used
        /// when no values are being uploaded.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The response from the request -- a byte array or a string -- upon
        /// success, or null upon failure.
        /// </returns>
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
