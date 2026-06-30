/*
 * NetworkVariable.cs --
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
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using StringDictionary = Eagle._Containers.Public.StringDictionary;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class implements a variable backed by a network service, exposing
    /// a remote key/value store as an Eagle array variable.  Array element
    /// reads, writes, and unsets, along with existence, count, and enumeration
    /// queries, are translated into web requests issued against a configured
    /// base URI and handled via a variable trace callback.
    /// </summary>
    [ObjectId("26cc91be-98bb-4e2c-932c-625724699d3e")]
    public sealed class NetworkVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ISupportVariable, IDisposable
    {
        #region Private Constants
        #region Network Request Parameter Names
        /// <summary>
        /// The default name of the network request parameter used to convey
        /// the API key.
        /// </summary>
        private static string DefaultApiKeyParameterName = "apiKey";
        /// <summary>
        /// The default name of the network request parameter used to convey
        /// the requested method (operation).
        /// </summary>
        private static string DefaultMethodParameterName = "method";
        /// <summary>
        /// The default name of the network request parameter used to convey
        /// the matching pattern.
        /// </summary>
        private static string DefaultPatternParameterName = "pattern";
        /// <summary>
        /// The default name of the network request parameter used to convey
        /// whether pattern matching should be case-insensitive.
        /// </summary>
        private static string DefaultNoCaseParameterName = "noCase";
        /// <summary>
        /// The default name of the network request parameter used to convey
        /// the array element name.
        /// </summary>
        private static string DefaultNameParameterName = "name";
        /// <summary>
        /// The default name of the network request parameter used to convey
        /// the array element value.
        /// </summary>
        private static string DefaultValueParameterName = "value";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Parameter Values
        /// <summary>
        /// The default value for the case-insensitive matching parameter when
        /// one is not explicitly supplied.
        /// </summary>
        private static bool DefaultNoCaseParameterValue = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Method Names
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default method name used to query whether an array element
        /// exists.
        /// </summary>
        private static string DefaultExistMethodName = "exist";
        /// <summary>
        /// The default method name used to query the number of array elements.
        /// </summary>
        private static string DefaultCountMethodName = "count";
        /// <summary>
        /// The default method name used to query the array element names.
        /// </summary>
        private static string DefaultNamesMethodName = "names";
        /// <summary>
        /// The default method name used to query the array element values.
        /// </summary>
        private static string DefaultValuesMethodName = "values";
        /// <summary>
        /// The default method name used to query all array element names and
        /// values.
        /// </summary>
        private static string DefaultAllMethodName = "all";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default method name used to get the value of an array element.
        /// </summary>
        private static string DefaultGetMethodName = "get";
        /// <summary>
        /// The default method name used to set the value of an array element.
        /// </summary>
        private static string DefaultSetMethodName = "set";
        /// <summary>
        /// The default method name used to unset an array element.
        /// </summary>
        private static string DefaultUnsetMethodName = "unset";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default method name used to purge an array element.
        /// </summary>
        private static string DefaultPurgeMethodName = "purge";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Handling
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The maximum length, in characters, of an array element name that may
        /// be conveyed via the query string before the request must instead be
        /// sent via upload.
        /// </summary>
        private static int QueryStringLength = 256;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default value indicating whether the configured callback should
        /// be used to create the network client.
        /// </summary>
        private static bool DefaultUseNewNetworkClientCallback = true;
        /// <summary>
        /// The default value indicating whether the network client should be
        /// cached and reused across requests.
        /// </summary>
        private static bool DefaultUseCachedWebClient = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the inputs of each web request are emitted via the
        /// tracing subsystem.
        /// </summary>
        private static bool TraceRequestInput = false;
        /// <summary>
        /// When non-zero, the elapsed time of each web request is emitted via
        /// the tracing subsystem.
        /// </summary>
        private static bool TraceRequestTime = false;
        /// <summary>
        /// When non-zero, the output of each web request is emitted via the
        /// tracing subsystem.
        /// </summary>
        private static bool TraceRequestOutput = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Response Processing
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The prefix used when synthesizing names for anonymous values
        /// returned by an array values query.
        /// </summary>
        private static string AnonymousValuePrefix = "NoName";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The leading element value indicating that a network response
        /// represents success.
        /// </summary>
        private static string OkValue = "OK";
        /// <summary>
        /// The leading element value indicating that a network response
        /// represents an error.
        /// </summary>
        private static string ErrorValue = "ERROR";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The cached network client, if any, reused across requests when
        /// caching is enabled.
        /// </summary>
        private WebClient cachedWebClient;
        /// <summary>
        /// When non-zero, the network client is cached and reused across
        /// requests.
        /// </summary>
        private bool useCachedWebClient;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the configured callback is used to create the network
        /// client.
        /// </summary>
        private bool useNewNetworkClientCallback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class with its default settings.
        /// This constructor is used by the other constructor overload.
        /// </summary>
        private NetworkVariable()
        {
            cachedWebClient = null;
            useCachedWebClient = DefaultUseCachedWebClient;
            useNewNetworkClientCallback = DefaultUseNewNetworkClientCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class from the fully specified set of
        /// network request settings.  This constructor delegates to the default
        /// constructor.
        /// </summary>
        /// <param name="newNetworkClientCallback">
        /// The callback used to create the network client, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="argument">
        /// The opaque argument to pass to the network client callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the network client callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="baseUri">
        /// The base URI against which network requests are issued.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry a failed network request, or
        /// null to use the default.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use for network requests and responses, or
        /// null to use the default.
        /// </param>
        /// <param name="apiKeyParameterName">
        /// The name of the network request parameter used to convey the API
        /// key, or null to use the default.
        /// </param>
        /// <param name="apiKeyParameterValue">
        /// The value of the API key to convey with each network request.  This
        /// parameter may be null.
        /// </param>
        /// <param name="methodParameterName">
        /// The name of the network request parameter used to convey the
        /// requested method, or null to use the default.
        /// </param>
        /// <param name="patternParameterName">
        /// The name of the network request parameter used to convey the
        /// matching pattern, or null to use the default.
        /// </param>
        /// <param name="noCaseParameterName">
        /// The name of the network request parameter used to convey whether
        /// pattern matching should be case-insensitive, or null to use the
        /// default.
        /// </param>
        /// <param name="nameParameterName">
        /// The name of the network request parameter used to convey the array
        /// element name, or null to use the default.
        /// </param>
        /// <param name="valueParameterName">
        /// The name of the network request parameter used to convey the array
        /// element value, or null to use the default.
        /// </param>
        /// <param name="permissions">
        /// The set of operations that are permitted on this variable.
        /// </param>
        private NetworkVariable(
            NewNetworkClientCallback newNetworkClientCallback, /* in */
            string argument,                                   /* in */
            IClientData clientData,                            /* in */
            Uri baseUri,                                       /* in */
            int? maximumRetries,                               /* in */
            int? timeout,                                      /* in */
            Encoding encoding,                                 /* in */
            string apiKeyParameterName,                        /* in */
            string apiKeyParameterValue,                       /* in */
            string methodParameterName,                        /* in */
            string patternParameterName,                       /* in */
            string noCaseParameterName,                        /* in */
            string nameParameterName,                          /* in */
            string valueParameterName,                         /* in */
            BreakpointType permissions                         /* in */
            )
            : this()
        {
            this.newNetworkClientCallback = newNetworkClientCallback;
            this.argument = argument;
            this.clientData = clientData;
            this.baseUri = baseUri;
            this.maximumRetries = maximumRetries;
            this.timeout = timeout;
            this.encoding = encoding;
            this.apiKeyParameterName = apiKeyParameterName;
            this.apiKeyParameterValue = apiKeyParameterValue;
            this.methodParameterName = methodParameterName;
            this.patternParameterName = patternParameterName;
            this.noCaseParameterName = noCaseParameterName;
            this.nameParameterName = nameParameterName;
            this.valueParameterName = valueParameterName;
            this.permissions = permissions;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new instance of this class from the fully
        /// specified set of network request settings.
        /// </summary>
        /// <param name="newNetworkClientCallback">
        /// The callback used to create the network client, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="argument">
        /// The opaque argument to pass to the network client callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the network client callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="baseUri">
        /// The base URI against which network requests are issued.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry a failed network request, or
        /// null to use the default.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use for network requests and responses, or
        /// null to use the default.
        /// </param>
        /// <param name="apiKeyParameterName">
        /// The name of the network request parameter used to convey the API
        /// key, or null to use the default.
        /// </param>
        /// <param name="apiKeyParameterValue">
        /// The value of the API key to convey with each network request.  This
        /// parameter may be null.
        /// </param>
        /// <param name="methodParameterName">
        /// The name of the network request parameter used to convey the
        /// requested method, or null to use the default.
        /// </param>
        /// <param name="patternParameterName">
        /// The name of the network request parameter used to convey the
        /// matching pattern, or null to use the default.
        /// </param>
        /// <param name="noCaseParameterName">
        /// The name of the network request parameter used to convey whether
        /// pattern matching should be case-insensitive, or null to use the
        /// default.
        /// </param>
        /// <param name="nameParameterName">
        /// The name of the network request parameter used to convey the array
        /// element name, or null to use the default.
        /// </param>
        /// <param name="valueParameterName">
        /// The name of the network request parameter used to convey the array
        /// element value, or null to use the default.
        /// </param>
        /// <param name="permissions">
        /// The set of operations that are permitted on this variable.
        /// </param>
        /// <returns>
        /// The newly created network variable.
        /// </returns>
        public static NetworkVariable Create(
            NewNetworkClientCallback newNetworkClientCallback, /* in */
            string argument,                                   /* in */
            IClientData clientData,                            /* in */
            Uri baseUri,                                       /* in */
            int? maximumRetries,                               /* in */
            int? timeout,                                      /* in */
            Encoding encoding,                                 /* in */
            string apiKeyParameterName,                        /* in */
            string apiKeyParameterValue,                       /* in */
            string methodParameterName,                        /* in */
            string patternParameterName,                       /* in */
            string noCaseParameterName,                        /* in */
            string nameParameterName,                          /* in */
            string valueParameterName,                         /* in */
            BreakpointType permissions                         /* in */
            )
        {
            return new NetworkVariable(
                newNetworkClientCallback, argument,
                clientData, baseUri, maximumRetries,
                timeout, encoding,
                apiKeyParameterName, apiKeyParameterValue,
                methodParameterName, patternParameterName,
                noCaseParameterName, nameParameterName,
                valueParameterName, permissions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        #region Public Properties
        /// <summary>
        /// The callback used to create the network client, if any.
        /// </summary>
        private NewNetworkClientCallback newNetworkClientCallback;
        /// <summary>
        /// Gets the callback used to create the network client, if any.
        /// </summary>
        public NewNetworkClientCallback NewNetworkClientCallback
        {
            get { CheckDisposed(); return newNetworkClientCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The opaque argument passed to the network client callback, if any.
        /// </summary>
        private string argument;
        /// <summary>
        /// Gets the opaque argument passed to the network client callback, if
        /// any.
        /// </summary>
        public string Argument
        {
            get { CheckDisposed(); return argument; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The client data passed to the network client callback, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets the client data passed to the network client callback, if any.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base URI against which network requests are issued.
        /// </summary>
        private Uri baseUri;
        /// <summary>
        /// Gets the base URI against which network requests are issued.
        /// </summary>
        public Uri BaseUri
        {
            get { CheckDisposed(); return baseUri; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </summary>
        private int? timeout;
        /// <summary>
        /// Gets the network request timeout, in milliseconds, or null to use
        /// the default.
        /// </summary>
        public int? Timeout
        {
            get { CheckDisposed(); return timeout; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The maximum number of times to retry a failed network request, or
        /// null to use the default.
        /// </summary>
        private int? maximumRetries;
        /// <summary>
        /// Gets the maximum number of times to retry a failed network request,
        /// or null to use the default.
        /// </summary>
        public int? MaximumRetries
        {
            get { CheckDisposed(); return maximumRetries; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The character encoding used for network requests and responses, or
        /// null to use the default.
        /// </summary>
        private Encoding encoding;
        /// <summary>
        /// Gets the character encoding used for network requests and responses,
        /// or null to use the default.
        /// </summary>
        public Encoding Encoding
        {
            get { CheckDisposed(); return encoding; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the network request parameter used to convey the API
        /// key, or null to use the default.
        /// </summary>
        private string apiKeyParameterName;
        /// <summary>
        /// Gets the name of the network request parameter used to convey the
        /// API key, or null to use the default.
        /// </summary>
        public string ApiKeyParameterName
        {
            get { CheckDisposed(); return apiKeyParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The value of the API key conveyed with each network request, if any.
        /// </summary>
        private string apiKeyParameterValue;
        /// <summary>
        /// Gets the value of the API key conveyed with each network request, if
        /// any.
        /// </summary>
        public string ApiKeyParameterValue
        {
            get { CheckDisposed(); return apiKeyParameterValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the network request parameter used to convey the
        /// requested method, or null to use the default.
        /// </summary>
        private string methodParameterName;
        /// <summary>
        /// Gets the name of the network request parameter used to convey the
        /// requested method, or null to use the default.
        /// </summary>
        public string MethodParameterName
        {
            get { CheckDisposed(); return methodParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the network request parameter used to convey the
        /// matching pattern, or null to use the default.
        /// </summary>
        private string patternParameterName;
        /// <summary>
        /// Gets the name of the network request parameter used to convey the
        /// matching pattern, or null to use the default.
        /// </summary>
        public string PatternParameterName
        {
            get { CheckDisposed(); return patternParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the network request parameter used to convey whether
        /// pattern matching should be case-insensitive, or null to use the
        /// default.
        /// </summary>
        private string noCaseParameterName;
        /// <summary>
        /// Gets the name of the network request parameter used to convey
        /// whether pattern matching should be case-insensitive, or null to use
        /// the default.
        /// </summary>
        public string NoCaseParameterName
        {
            get { CheckDisposed(); return noCaseParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the network request parameter used to convey the array
        /// element name, or null to use the default.
        /// </summary>
        private string nameParameterName;
        /// <summary>
        /// Gets the name of the network request parameter used to convey the
        /// array element name, or null to use the default.
        /// </summary>
        public string NameParameterName
        {
            get { CheckDisposed(); return nameParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the network request parameter used to convey the array
        /// element value, or null to use the default.
        /// </summary>
        private string valueParameterName;
        /// <summary>
        /// Gets the name of the network request parameter used to convey the
        /// array element value, or null to use the default.
        /// </summary>
        public string ValueParameterName
        {
            get { CheckDisposed(); return valueParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of operations that are permitted on this variable.
        /// </summary>
        private BreakpointType permissions;
        /// <summary>
        /// Gets the set of operations that are permitted on this variable.
        /// </summary>
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
        /// <summary>
        /// This method adds this network-backed array variable to the specified
        /// interpreter, installing the trace callback that handles its
        /// operations.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to which the variable should be added.
        /// </param>
        /// <param name="name">
        /// The name of the variable to add.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode AddVariable(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddVariable(VariableFlags.Array, name,
                new TraceList(new TraceCallback[] { TraceCallback }),
                true, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Helper Methods
        /// <summary>
        /// This method builds a list of name/value pairs describing the
        /// configuration of this network variable, suitable for introspection.
        /// </summary>
        /// <returns>
        /// A list of name/value pairs describing this network variable.
        /// </returns>
        public StringPairList ToList()
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            string methodName = FormatOps.DelegateMethodName(
                newNetworkClientCallback, false, false);

            if (newNetworkClientCallback != null)
                list.Add("newNetworkClientCallback", methodName);

            if (argument != null)
                list.Add("argument", argument);

            if (clientData != null)
                list.Add("clientData", clientData.ToString());

            if (baseUri != null)
                list.Add("baseUri", baseUri.ToString());

            if (encoding != null)
                list.Add("encoding", encoding.WebName);

            if (apiKeyParameterName != null)
                list.Add("apiKeyParameterName", apiKeyParameterName);

            if (apiKeyParameterValue != null)
                list.Add("apiKeyParameterValue", apiKeyParameterValue);

            if (methodParameterName != null)
                list.Add("methodParameterName", methodParameterName);

            if (patternParameterName != null)
                list.Add("patternParameterName", patternParameterName);

            if (noCaseParameterName != null)
                list.Add("noCaseParameterName", noCaseParameterName);

            if (nameParameterName != null)
                list.Add("nameParameterName", nameParameterName);

            if (valueParameterName != null)
                list.Add("valueParameterName", valueParameterName);

            list.Add("permissions", permissions.ToString());

            return list;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISupportVariable Members
        /// <summary>
        /// This method determines whether the named array element exists,
        /// querying the network service to do so.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the array element to check for existence.
        /// </param>
        /// <returns>
        /// True if the array element exists; otherwise, false.
        /// </returns>
        public bool DoesExist(
            Interpreter interpreter, /* in */
            string name              /* in */
            )
        {
            CheckDisposed();

            return DoesExistViaNetwork(
                interpreter, name, MaximumRetries, Timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the number of array
        /// elements.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The number of array elements, or null if the count could not be
        /// determined.
        /// </returns>
        public long? GetCount(
            Interpreter interpreter, /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            long count = 0;

            if (GetCountViaNetwork(
                    interpreter, MaximumRetries, Timeout,
                    ref count, ref error) == ReturnCode.Ok)
            {
                return count;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the array element names
        /// and/or values, returning them as a dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the array element names in the result.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the array element values in the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A dictionary of the array element names and/or values, or null if the
        /// query could not be completed.
        /// </returns>
        public ObjectDictionary GetList(
            Interpreter interpreter, /* in */
            bool names,              /* in */
            bool values,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaNetwork(
                    interpreter, null, false, names, values,
                    MaximumRetries, Timeout, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                return dictionary;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the array element names
        /// and/or values that match the specified pattern, returning them as a
        /// dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match array element names, or null to match all
        /// of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the array element names in the result.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the array element values in the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A dictionary of the matching array element names and/or values, or
        /// null if the query could not be completed.
        /// </returns>
        public ObjectDictionary GetList(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            bool names,              /* in */
            bool values,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaNetwork(
                    interpreter, pattern, noCase, names, values,
                    MaximumRetries, Timeout, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                return dictionary;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the array element names
        /// matching the specified pattern and formats them as a string list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="mode">
        /// The matching mode to use when filtering the array element names.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match array element names, or null to match all
        /// of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is
        /// regular-expression based.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A string containing the matching array element names, or null if the
        /// query could not be completed.
        /// </returns>
        public string KeysToString(
            Interpreter interpreter,   /* in */
            MatchMode mode,            /* in */
            string pattern,            /* in */
            bool noCase,               /* in */
            RegexOptions regExOptions, /* in */
            ref Result error           /* out */
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaNetwork(
                    interpreter, pattern, noCase, true, false,
                    MaximumRetries, Timeout, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, false, mode, pattern, null,
                    null, null, null, noCase, regExOptions) as StringList;

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.SpaceString, null, false);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the array element names
        /// and values matching the specified pattern and formats them as a
        /// string list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match array element names, or null to match all
        /// of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A string containing the matching array element names and values, or
        /// null if the query could not be completed.
        /// </returns>
        public string KeysAndValuesToString(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaNetwork(
                    interpreter, pattern, noCase, true, true,
                    MaximumRetries, Timeout, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, true, StringOps.DefaultMatchMode,
                    pattern, null, null, null, null, noCase, RegexOptions.None)
                    as StringList;

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.SpaceString, null, false);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Method
        /// <summary>
        /// This method is the variable trace callback that handles operations
        /// on the network-backed array variable.  It translates supported get,
        /// set, and unset operations into web requests and conveys the result
        /// back through the trace information.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation that triggered this trace callback.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information describing the operation; it is also used to
        /// convey the outcome back to the caller.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the value produced by the operation; upon
        /// failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        [MethodFlags(
            MethodFlags.VariableTrace | MethodFlags.System |
            MethodFlags.NoAdd)]
        private ReturnCode TraceCallback(
            BreakpointType breakpointType, /* in */
            Interpreter interpreter,       /* in */
            ITraceInfo traceInfo,          /* in, out */
            ref Result result              /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                result = "invalid trace";
                return ReturnCode.Error;
            }

            IVariable variable = traceInfo.Variable;

            if (variable == null)
            {
                result = "invalid variable";
                return ReturnCode.Error;
            }

            //
            // NOTE: *SPECIAL* Ignore the index when we initially add the
            //       variable since we do not perform any trace actions during
            //       add anyhow.
            //
            if (breakpointType == BreakpointType.BeforeVariableAdd)
                return traceInfo.ReturnCode;

            //
            // NOTE: Check if we support the requested operation at all.
            //
            if ((breakpointType != BreakpointType.BeforeVariableGet) &&
                (breakpointType != BreakpointType.BeforeVariableSet) &&
                (breakpointType != BreakpointType.BeforeVariableUnset))
            {
                result = "unsupported operation";
                return ReturnCode.Error;
            }

            //
            // NOTE: *WARNING* Empty array element names are allowed, please do
            //       not change this to "!String.IsNullOrEmpty".
            //
            string varName = traceInfo.Index;

            if (varName != null)
            {
                //
                // NOTE: Check if we are allowing this type of operation.  This
                //       does not apply if the entire variable is being removed
                //       from the interpreter (i.e. for "unset" operations when
                //       the index is null).
                //
                if (!HasFlags(breakpointType, true))
                {
                    result = "permission denied";
                    return ReturnCode.Error;
                }

                WebClient webClient = null;
                bool dispose = true;

                try
                {
                    webClient = MaybeCreateWebClient(
                        interpreter, Timeout, ref dispose,
                        ref result);

                    if (webClient == null)
                        return ReturnCode.Error;

                    string varValue = StringOps.GetStringFromObject(
                        traceInfo.NewValue);

                    string text = PerformWebRequest(interpreter,
                        webClient, breakpointType, traceInfo.Flags,
                        null, DefaultNoCaseParameterValue, varName,
                        varValue, MaximumRetries, ref result);

                    if (text == null)
                        return ReturnCode.Error;

                    switch (breakpointType)
                    {
                        case BreakpointType.BeforeVariableGet:
                            {
                                result = text;

                                traceInfo.ReturnCode = ReturnCode.Ok;
                                traceInfo.Cancel = true;
                                break;
                            }
                        case BreakpointType.BeforeVariableSet:
                            {
                                result = text;

                                EntityOps.SetUndefined(variable, false);
                                EntityOps.SetDirty(variable, true);

                                traceInfo.ReturnCode = ReturnCode.Ok;
                                traceInfo.Cancel = true;
                                break;
                            }
                        case BreakpointType.BeforeVariableUnset:
                            {
                                result = text;

                                EntityOps.SetDirty(variable, true);

                                traceInfo.ReturnCode = ReturnCode.Ok;
                                traceInfo.Cancel = true;
                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    Engine.SetExceptionErrorCode(interpreter, e);

                    result = e;
                    traceInfo.ReturnCode = ReturnCode.Error;
                }
                finally
                {
                    if (webClient != null)
                    {
                        if (dispose)
                        {
                            ObjectOps.TryDisposeOrComplain<WebClient>(
                                interpreter, ref webClient);
                        }

                        webClient = null;
                    }
                }

                return traceInfo.ReturnCode;
            }
            else if (breakpointType == BreakpointType.BeforeVariableUnset)
            {
                //
                // NOTE: They want to unset the entire net array.  I guess
                //       this should be allowed, it is in Tcl.  Also, make
                //       sure it is purged from the call frame so that it
                //       cannot be magically restored with this trace
                //       callback in place.
                //
                traceInfo.Flags &= ~VariableFlags.NoRemove;

                //
                // NOTE: Ok, allow the variable removal.
                //
                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We (this trace procedure) expect the variable
                //       to always be an array.
                //
                result = FormatOps.MissingElementName(
                    breakpointType, variable.Name, true);

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this network
        /// variable, based on its configuration.
        /// </summary>
        /// <returns>
        /// A string representation of this network variable.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Flags Helper Methods
        /// <summary>
        /// This method determines whether the configured permissions include
        /// the specified operation flags.
        /// </summary>
        /// <param name="hasFlags">
        /// The operation flags to test for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be present; zero
        /// to require that any of them be present.
        /// </param>
        /// <returns>
        /// True if the configured permissions include the specified flags;
        /// otherwise, false.
        /// </returns>
        private bool HasFlags(
            BreakpointType hasFlags, /* in */
            bool all                 /* in */
            )
        {
            return FlagOps.HasFlags(permissions, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Helper Methods (Static)
        /// <summary>
        /// This method maps an operation, and its associated variable flags, to
        /// the network method name that implements it.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation for which a method name is needed.
        /// </param>
        /// <param name="variableFlags">
        /// The variable flags associated with the operation, used (for example)
        /// to distinguish an unset from a purge.
        /// </param>
        /// <returns>
        /// The method name implementing the specified operation, or null if the
        /// operation is not supported.
        /// </returns>
        private static string GetMethodName(
            BreakpointType breakpointType, /* in */
            VariableFlags variableFlags    /* in */
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableExist:
                    {
                        return DefaultExistMethodName;
                    }
                case BreakpointType.BeforeVariableCount:
                    {
                        return DefaultCountMethodName;
                    }
                case BreakpointType.BeforeVariableGet:
                    {
                        return DefaultGetMethodName;
                    }
                case BreakpointType.BeforeVariableSet:
                    {
                        return DefaultSetMethodName;
                    }
                case BreakpointType.BeforeVariableUnset:
                    {
                        if (FlagOps.HasFlags(
                                variableFlags, VariableFlags.Purge,
                                true))
                        {
                            return DefaultPurgeMethodName;
                        }
                        else
                        {
                            return DefaultUnsetMethodName;
                        }
                    }
                case BreakpointType.BeforeVariableArrayNames:
                    {
                        return DefaultNamesMethodName;
                    }
                case BreakpointType.BeforeVariableArrayValues:
                    {
                        return DefaultValuesMethodName;
                    }
                case BreakpointType.BeforeVariableArrayGet:
                    {
                        return DefaultAllMethodName;
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a request for the specified operation
        /// should be sent via upload (request body) rather than via the query
        /// string.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being requested.
        /// </param>
        /// <param name="name">
        /// The array element name involved in the operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the request should be sent via upload; otherwise, false.
        /// </returns>
        private static bool ShouldRequestViaUpload(
            BreakpointType breakpointType, /* in */
            string name                    /* in */
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableSet:
                    {
                        return true;
                    }
                default:
                    {
                        if ((name != null) &&
                            (name.Length > QueryStringLength))
                        {
                            return true;
                        }

                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified operation requires the
        /// array element name parameter to be supplied.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being requested.
        /// </param>
        /// <returns>
        /// True if the operation requires the name parameter; otherwise, false.
        /// </returns>
        private static bool NeedNameParameter(
            BreakpointType breakpointType /* in */
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableExist:
                case BreakpointType.BeforeVariableGet:
                case BreakpointType.BeforeVariableSet:
                case BreakpointType.BeforeVariableUnset:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified operation requires the
        /// array element value parameter to be supplied.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being requested.
        /// </param>
        /// <returns>
        /// True if the operation requires the value parameter; otherwise,
        /// false.
        /// </returns>
        private static bool NeedValueParameter(
            BreakpointType breakpointType /* in */
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableSet:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the culture information associated with the
        /// specified interpreter, for use when parsing network responses.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose culture information is needed.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The culture information associated with the interpreter, or null if
        /// the interpreter is null.
        /// </returns>
        private static CultureInfo GetCultureInfo(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.InternalCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method synthesizes a name for an anonymous value at the
        /// specified position, incorporating the value's hash code.
        /// </summary>
        /// <param name="index">
        /// The position of the value within the list of anonymous values.
        /// </param>
        /// <param name="value">
        /// The value for which a name is being synthesized.
        /// </param>
        /// <returns>
        /// The synthesized name for the anonymous value.
        /// </returns>
        private static string FormatName(
            int index,   /* in */
            string value /* in */
            )
        {
            return String.Format(
                "${0}_#{1}_@{2}", AnonymousValuePrefix, index,
                RuntimeOps.GetHashCode(value));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates a dictionary from a flat list returned by the
        /// network service, interpreting the list according to the specified
        /// operation.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation that produced the list, which determines how
        /// the list is interpreted (names only, values only, or name/value
        /// pairs).
        /// </param>
        /// <param name="list">
        /// The flat list of names and/or values to populate the dictionary
        /// from.  This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary to populate; it is created if it is null.
        /// </param>
        private static void PopulateDictionary(
            BreakpointType breakpointType,  /* in */
            StringList list,                /* in */
            ref ObjectDictionary dictionary /* in, out */
            )
        {
            if (list == null)
                return;

            if (dictionary == null)
                dictionary = new ObjectDictionary();

            int count = list.Count;

            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableArrayNames:
                    {
                        for (int index = 0; index < count; index++)
                        {
                            string name = list[index];

                            if (name == null)
                                name = String.Empty;

                            dictionary[name] = null;
                        }
                        break;
                    }
                case BreakpointType.BeforeVariableArrayValues:
                    {
                        for (int index = 0; index < count; index++)
                        {
                            string value = list[index];
                            string name = FormatName(index, value);

                            dictionary[name] = value;
                        }
                        break;
                    }
                case BreakpointType.BeforeVariableArrayGet:
                    {
                        for (int index = 0; index < count; index += 2)
                        {
                            string name = list[index];

                            if (name == null)
                                name = String.Empty;

                            string value = null;

                            if ((index + 1) < count)
                                value = list[index + 1];

                            dictionary[name] = value;
                        }
                        break;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Helper Methods (Instance)
        #region WebClient Cache Helper Methods (Instance)
        /// <summary>
        /// This method returns the cached network client, if caching is enabled
        /// and a cached client is available.
        /// </summary>
        /// <param name="dispose">
        /// Upon return, set to zero when the cached client is returned (since
        /// the caller must not dispose it).
        /// </param>
        /// <returns>
        /// The cached network client, or null if none is available.
        /// </returns>
        private WebClient GetCachedWebClient(
            ref bool dispose /* out */
            )
        {
            if (useCachedWebClient && (cachedWebClient != null))
            {
                dispose = false;
                return cachedWebClient;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method caches the specified network client for reuse, if
        /// caching is enabled.
        /// </summary>
        /// <param name="webClient">
        /// The network client to cache.  This parameter may be null.
        /// </param>
        /// <param name="dispose">
        /// Upon return, set to zero when the client has been cached (since
        /// ownership is retained by this instance).
        /// </param>
        private void SetCachedWebClient(
            WebClient webClient, /* in */
            ref bool dispose     /* out */
            )
        {
            if (useCachedWebClient && (webClient != null))
            {
                cachedWebClient = webClient;
                dispose = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WebClient Helper Methods (Instance)
        /// <summary>
        /// This method determines whether the configured callback should be
        /// used to create the network client.
        /// </summary>
        /// <returns>
        /// True if the callback should be used; otherwise, false.
        /// </returns>
        private bool ShouldUseNewNetworkClientCallback()
        {
            return useNewNetworkClientCallback &&
                (newNetworkClientCallback != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a network client by invoking the configured
        /// callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.  This parameter is not used by this method.
        /// </param>
        /// <param name="dispose">
        /// Upon return, indicates whether the caller is responsible for
        /// disposing the returned client.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The newly created network client, or null on failure.
        /// </returns>
        private WebClient CreateWebClientViaCallback(
            Interpreter interpreter, /* in */
            int? timeout,            /* in: NOT USED */
            ref bool dispose,        /* out */
            ref Result error         /* out */
            )
        {
            if (newNetworkClientCallback == null)
            {
                error = "invalid new network client callback";
                return null;
            }

            object networkClient = newNetworkClientCallback(
                interpreter, argument, clientData, ref error);

            WebClient webClient = networkClient as WebClient;

            if (webClient != null)
            {
                SetCachedWebClient(webClient, ref dispose);
                return webClient;
            }
            else
            {
                error = String.Format(
                    "could not convert network client type {0} to {1}",
                    FormatOps.TypeName(networkClient),
                    FormatOps.TypeName(typeof(WebClient)));

                /* IGNORED */
                ObjectOps.TryDisposeOrTrace<object>(
                    ref networkClient);

                networkClient = null;

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a network client using the facilities of the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to create the network client.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <param name="dispose">
        /// Upon return, indicates whether the caller is responsible for
        /// disposing the returned client.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The newly created network client, or null on failure.
        /// </returns>
        private WebClient CreateWebClientViaInterpreter(
            Interpreter interpreter, /* in */
            int? timeout,            /* in */
            ref bool dispose,        /* out */
            ref Result error         /* out */
            )
        {
            WebClient webClient = WebOps.CreateClient(
                interpreter, argument, clientData, WebOps.GetTimeout(
                interpreter, TimeoutType.Network, timeout), ref error);

            SetCachedWebClient(webClient, ref dispose);
            return webClient;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a network client, preferring the cached client
        /// when available and otherwise creating one via the callback or the
        /// interpreter, as configured.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <param name="dispose">
        /// Upon return, indicates whether the caller is responsible for
        /// disposing the returned client.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// A network client, or null on failure.
        /// </returns>
        private WebClient MaybeCreateWebClient(
            Interpreter interpreter, /* in */
            int? timeout,            /* in */
            ref bool dispose,        /* out */
            ref Result error         /* out */
            )
        {
            WebClient webClient = GetCachedWebClient(ref dispose);

            if (webClient != null)
                return webClient;

            if (ShouldUseNewNetworkClientCallback())
            {
                return CreateWebClientViaCallback(
                    interpreter, timeout, ref dispose, ref error);
            }
            else
            {
                return CreateWebClientViaInterpreter(
                    interpreter, timeout, ref dispose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the request URI, and any associated upload data,
        /// for the specified operation and parameters.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of operation being requested.
        /// </param>
        /// <param name="variableFlags">
        /// The variable flags associated with the operation.
        /// </param>
        /// <param name="pattern">
        /// The matching pattern to include in the request, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="name">
        /// The array element name to include in the request, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// The array element value to include in the request, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="uri">
        /// Upon success, receives the request URI.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the upload data for the request, or null when
        /// the request is conveyed entirely via the URI.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True on success; otherwise, false.
        /// </returns>
        private bool TryBuildUri(
            BreakpointType breakpointType, /* in */
            VariableFlags variableFlags,   /* in */
            string pattern,                /* in */
            bool noCase,                   /* in */
            string name,                   /* in */
            string value,                  /* in */
            ref Uri uri,                   /* out */
            ref NameValueCollection data,  /* out */
            ref Result error               /* out */
            )
        {
            if (baseUri == null)
            {
                error = "invalid base uri";
                return false;
            }

            string methodName = GetMethodName(breakpointType, variableFlags);

            if (methodName == null)
            {
                error = String.Format(
                    "no method available for operation {0}",
                    breakpointType);

                return false;
            }

            Uri localUri;

            if (ShouldRequestViaUpload(breakpointType, name))
            {
                NameValueCollection collection;

                localUri = baseUri;
                collection = HttpUtility.ParseQueryString(String.Empty);

                collection.Add((apiKeyParameterName != null) ?
                    apiKeyParameterName : DefaultApiKeyParameterName,
                    apiKeyParameterValue);

                collection.Add((methodParameterName != null) ?
                    methodParameterName : DefaultMethodParameterName,
                    methodName);

                if (pattern != null)
                {
                    collection.Add((patternParameterName != null) ?
                        patternParameterName : DefaultPatternParameterName,
                        pattern);

                    collection.Add((noCaseParameterName != null) ?
                        noCaseParameterName : DefaultNoCaseParameterName,
                        noCase.ToString());
                }

                if (NeedNameParameter(breakpointType))
                {
                    collection.Add((nameParameterName != null) ?
                        nameParameterName : DefaultNameParameterName,
                        name);
                }

                if (NeedValueParameter(breakpointType))
                {
                    collection.Add((valueParameterName != null) ?
                        valueParameterName : DefaultValueParameterName,
                        value);
                }

                uri = localUri;
                data = collection;

                return true;
            }
            else
            {
                StringDictionary dictionary = new StringDictionary();

                dictionary.Add((apiKeyParameterName != null) ?
                    apiKeyParameterName : DefaultApiKeyParameterName,
                    apiKeyParameterValue);

                dictionary.Add((methodParameterName != null) ?
                    methodParameterName : DefaultMethodParameterName,
                    methodName);

                if (pattern != null)
                {
                    dictionary.Add((patternParameterName != null) ?
                        patternParameterName : DefaultPatternParameterName,
                        pattern);

                    dictionary.Add((noCaseParameterName != null) ?
                        noCaseParameterName : DefaultNoCaseParameterName,
                        noCase.ToString());
                }

                if (NeedNameParameter(breakpointType))
                {
                    dictionary.Add((nameParameterName != null) ?
                        nameParameterName : DefaultNameParameterName,
                        name);
                }

                if (NeedValueParameter(breakpointType))
                {
                    dictionary.Add((valueParameterName != null) ?
                        valueParameterName : DefaultValueParameterName,
                        value);
                }

                StringBuilder builder = null;

                PathOps.QueryFromDictionary(
                    dictionary, encoding, ref builder);

                if (builder != null)
                {
                    builder.Insert(0, Characters.QuestionMark);

                    localUri = PathOps.TryCombineUris(
                        baseUri, StringBuilderCache.GetStringAndRelease(
                        ref builder), encoding, UriComponents.AbsoluteUri,
                        UriFormat.Unescaped, UriFlags.None, ref error);

                    if (localUri == null)
                        return false;
                }
                else
                {
                    localUri = baseUri;
                }

                uri = localUri;
                data = null;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs a web request for the specified operation,
        /// using the supplied network client, and decodes the response.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="webClient">
        /// The network client to use for the request.
        /// </param>
        /// <param name="breakpointType">
        /// The type of operation being requested.
        /// </param>
        /// <param name="variableFlags">
        /// The variable flags associated with the operation.
        /// </param>
        /// <param name="pattern">
        /// The matching pattern to include in the request, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="name">
        /// The array element name to include in the request, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// The array element value to include in the request, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The decoded response text, or null on failure.
        /// </returns>
        private string PerformWebRequest(
            Interpreter interpreter,       /* in */
            WebClient webClient,           /* in */
            BreakpointType breakpointType, /* in */
            VariableFlags variableFlags,   /* in */
            string pattern,                /* in */
            bool noCase,                   /* in */
            string name,                   /* in */
            string value,                  /* in */
            int? maximumRetries,           /* in */
            ref Result error               /* out */
            )
        {
            if (webClient == null)
            {
                error = "invalid web client";
                return null;
            }

            Uri uri = null;
            NameValueCollection data = null;

            if (!TryBuildUri(
                    breakpointType, variableFlags, pattern, noCase,
                    name, value, ref uri, ref data, ref error))
            {
                return null;
            }

            if (TraceRequestInput)
            {
                TraceOps.DebugTrace(String.Format(
                    "PerformWebRequest: interpreter = {0}, " +
                    "webClient = {1}, breakpointType = {2}, " +
                    "variableFlags = {3}, uri = {4}, data = {5}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(webClient),
                    FormatOps.WrapOrNull(breakpointType),
                    FormatOps.WrapOrNull(variableFlags),
                    FormatOps.WrapOrNull(uri),
                    FormatOps.WrapOrNull(true, false, data)),
                    typeof(NetworkVariable).Name,
                    TracePriority.NetworkDebug);
            }

            IProfilerState profiler = null;
            bool dispose = true;

            try
            {
                if (TraceRequestTime)
                {
                    profiler = ProfilerState.Create(
                        interpreter, ref dispose);
                }

                Result localError; /* REUSED */

                if (data != null)
                {
#if TEST
                    localError = null;

                    if (WebOps.SetSecurityProtocol(
                            false, false, ref localError) != ReturnCode.Ok)
                    {
                        if (localError != null)
                            error = localError;
                        else
                            error = "could not set security protocol (1)";

                        return null;
                    }
#endif

                    localError = null;

                    byte[] bytes = WebOps.MakeRequest(
                        interpreter, webClient, uri, maximumRetries,
                        data, profiler, false, ref localError) as byte[];

                    if (TraceRequestTime)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "PerformWebRequest: received {0} in {1}",
                            FormatOps.DisplayByteLength(bytes),
                            profiler), typeof(NetworkVariable).Name,
                            TracePriority.NetworkDebug);
                    }

                    if (bytes == null)
                    {
                        if (localError != null)
                            error = localError;
                        else
                            error = "missing response bytes";

                        return null;
                    }

                    return DecodeWebResult(
                        interpreter, bytes, ref error);
                }
                else
                {
#if TEST
                    localError = null;

                    if (WebOps.SetSecurityProtocol(
                            false, false, ref localError) != ReturnCode.Ok)
                    {
                        if (localError != null)
                            error = localError;
                        else
                            error = "could not set security protocol (2)";

                        return null;
                    }
#endif

                    localError = null;

                    string text = WebOps.MakeRequest(
                        interpreter, webClient, uri, maximumRetries,
                        data, profiler, false, ref localError) as string;

                    if (TraceRequestTime)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "PerformWebRequest: received {0} in {1}",
                            FormatOps.DisplayStringLength(text),
                            profiler), typeof(NetworkVariable).Name,
                            TracePriority.NetworkDebug);
                    }

                    if (text == null)
                    {
                        if (localError != null)
                            error = localError;
                        else
                            error = "missing response text";

                        return null;
                    }

                    return DecodeWebResult(
                        interpreter, text, ref error);
                }
            }
            finally
            {
                if (profiler != null)
                {
                    if (dispose)
                    {
                        ObjectOps.TryDisposeOrComplain<IProfilerState>(
                            interpreter, ref profiler);
                    }

                    profiler = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the effective length of a web result, excluding
        /// any single trailing newline sequence.
        /// </summary>
        /// <param name="text">
        /// The web result text to measure.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the effective length of the text, excluding a
        /// trailing newline sequence.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// True on success; otherwise, false.
        /// </returns>
        private bool GetWebResultLength(
            string text,     /* in */
            ref int length,  /* out */
            ref Result error /* out */
            )
        {
            if (text == null)
            {
                error = "invalid text";
                return false;
            }

            //
            // HACK: Remove a final "\r\n", "\r", or "\n" from the
            //       web result prior to any further processing.
            //
            int localLength = text.Length;

            if (localLength > 0)
            {
                char character1 = text[localLength - 1];

                if (localLength > 1)
                {
                    char character2 = text[localLength - 2];

                    if ((character2 == Characters.CarriageReturn) &&
                        (character1 == Characters.LineFeed))
                    {
                        localLength -= 2;
                        goto done;
                    }
                }

                if (character1 == Characters.CarriageReturn)
                {
                    localLength -= 1;
                    goto done;
                }

                if (character1 == Characters.LineFeed)
                {
                    localLength -= 1;
                    goto done;
                }
            }

        done:

            length = localLength;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes a textual web result, validating its leading
        /// status element and extracting the payload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="text">
        /// The web result text to decode.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem; for
        /// an error response, this receives the error conveyed by the service.
        /// </param>
        /// <returns>
        /// The decoded payload, or null on failure.
        /// </returns>
        private string DecodeWebResult(
            Interpreter interpreter, /* in */
            string text,             /* in */
            ref Result error         /* out */
            )
        {
            if (TraceRequestOutput)
            {
                TraceOps.DebugTrace(String.Format(
                    "DecodeWebResult: interpreter = {0}, text = {1}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(true, false, text)),
                    typeof(NetworkVariable).Name,
                    TracePriority.NetworkDebug);
            }

            int length = Length.Invalid;

            if (!GetWebResultLength(text, ref length, ref error))
                return null;

            StringList list = null;

            if (ParserOps<string>.SplitList(
                    interpreter, text, 0, length, true,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            if (list.Count < 2)
            {
                error = "result must have at least 2 elements";
                return null;
            }

            if ((ErrorValue != null) &&
                SharedStringOps.SystemEquals(list[0], ErrorValue))
            {
                error = list[1];
                return null;
            }
            else if ((OkValue != null) &&
                !SharedStringOps.SystemEquals(list[0], OkValue))
            {
                error = String.Format(
                    "overall result must be {0}",
                    FormatOps.WrapOrNull(OkValue));

                return null;
            }

            return HttpUtility.HtmlDecode(list[1]);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes a binary web result, converting it to text using
        /// the configured encoding (or Base64 when no encoding is configured)
        /// before decoding the payload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="bytes">
        /// The web result bytes to decode.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// The decoded payload, or null on failure.
        /// </returns>
        private string DecodeWebResult(
            Interpreter interpreter, /* in */
            byte[] bytes,            /* in */
            ref Result error         /* out */
            )
        {
            if (bytes == null)
            {
                error = "invalid byte array";
                return null;
            }

            if (encoding != null)
            {
                return DecodeWebResult(
                    interpreter, encoding.GetString(bytes),
                    ref error);
            }
            else
            {
                return DecodeWebResult(
                    interpreter, Convert.ToBase64String(bytes,
                    Base64FormattingOptions.InsertLineBreaks),
                    ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Variable Operation Helper Methods
        //
        // TODO: This method is not allowed to "fail"?  This seems like a
        //       design flaw.
        //
        /// <summary>
        /// This method determines whether the named array element exists by
        /// querying the network service.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the array element to check for existence.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// default.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <returns>
        /// True if the array element exists; otherwise, false.
        /// </returns>
        private bool DoesExistViaNetwork( /* CANARY */
            Interpreter interpreter, /* in */
            string name,             /* in */
            int? maximumRetries,     /* in */
            int? timeout             /* in */
            )
        {
            bool success = false;
            Result error = null;

            try
            {
                if (!HasFlags(
                        BreakpointType.BeforeVariableExist, true))
                {
                    error = "permission denied";
                    return false;
                }

                WebClient webClient = null;
                bool dispose = true;
                bool result = false;

                try
                {
                    webClient = MaybeCreateWebClient(
                        interpreter, timeout, ref dispose,
                        ref error);

                    if (webClient == null)
                        return false;

                    string text = PerformWebRequest(
                        interpreter, webClient,
                        BreakpointType.BeforeVariableExist,
                        VariableFlags.None, null,
                        DefaultNoCaseParameterValue, name,
                        null, maximumRetries, ref error);

                    if (text == null)
                        return false;

                    if (Value.GetBoolean2(
                            text, ValueFlags.AnyBoolean,
                            GetCultureInfo(interpreter),
                            ref result, ref error) == ReturnCode.Ok)
                    {
                        success = true;
                    }
                }
                finally
                {
                    if (webClient != null)
                    {
                        if (dispose)
                        {
                            ObjectOps.TryDisposeOrComplain<WebClient>(
                                interpreter, ref webClient);
                        }

                        webClient = null;
                    }
                }

                return result;
            }
            finally
            {
                if (!success)
                {
                    TraceOps.DebugTrace(String.Format(
                        "DoesExistViaNetwork: error = {0}", error),
                        typeof(NetworkVariable).Name,
                        TracePriority.NetworkError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the number of array
        /// elements.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// default.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <param name="count">
        /// Upon success, receives the number of array elements.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode GetCountViaNetwork(
            Interpreter interpreter, /* in */
            int? maximumRetries,     /* in */
            int? timeout,            /* in */
            ref long count,          /* in */
            ref Result error         /* out */
            )
        {
            if (!HasFlags(
                    BreakpointType.BeforeVariableCount, true))
            {
                error = "permission denied";
                return ReturnCode.Error;
            }

            WebClient webClient = null;
            bool dispose = true;

            try
            {
                webClient = MaybeCreateWebClient(
                    interpreter, timeout, ref dispose,
                    ref error);

                if (webClient == null)
                    return ReturnCode.Error;

                string text = PerformWebRequest(
                    interpreter, webClient,
                    BreakpointType.BeforeVariableCount,
                    VariableFlags.None, null,
                    DefaultNoCaseParameterValue, null,
                    null, maximumRetries, ref error);

                if (text == null)
                    return ReturnCode.Error;

                return Value.GetWideInteger2(
                    text, ValueFlags.AnyWideInteger,
                    GetCultureInfo(interpreter),
                    ref count, ref error);
            }
            finally
            {
                if (webClient != null)
                {
                    if (dispose)
                    {
                        ObjectOps.TryDisposeOrComplain<WebClient>(
                            interpreter, ref webClient);
                    }

                    webClient = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the network service for the array element names
        /// and/or values matching the specified pattern and populates a
        /// dictionary with the results.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="pattern">
        /// The matching pattern to include in the request, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="names">
        /// Non-zero to include the array element names in the result.
        /// </param>
        /// <param name="values">
        /// Non-zero to include the array element values in the result.
        /// </param>
        /// <param name="maximumRetries">
        /// The maximum number of times to retry the request, or null to use the
        /// default.
        /// </param>
        /// <param name="timeout">
        /// The network request timeout, in milliseconds, or null to use the
        /// default.
        /// </param>
        /// <param name="dictionary">
        /// Upon success, receives the dictionary of array element names and/or
        /// values; it is created if it is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode GetListViaNetwork(
            Interpreter interpreter,         /* in */
            string pattern,                  /* in */
            bool noCase,                     /* in */
            bool names,                      /* in */
            bool values,                     /* in */
            int? maximumRetries,             /* in */
            int? timeout,                    /* in */
            ref ObjectDictionary dictionary, /* out */
            ref Result error                 /* out */
            )
        {
            if (dictionary == null)
                dictionary = new ObjectDictionary();

            BreakpointType breakpointType = ScriptOps.GetBreakpointType(
                names, values);

            if (breakpointType == BreakpointType.None)
                return ReturnCode.Ok;

            if (!HasFlags(breakpointType, true))
            {
                error = "permission denied";
                return ReturnCode.Error;
            }

            WebClient webClient = null;
            bool dispose = true;

            try
            {
                webClient = MaybeCreateWebClient(
                    interpreter, timeout, ref dispose,
                    ref error);

                if (webClient == null)
                    return ReturnCode.Error;

                string text = PerformWebRequest(
                    interpreter, webClient, breakpointType,
                    VariableFlags.None, pattern, noCase,
                    null, null, maximumRetries, ref error);

                if (text == null)
                    return ReturnCode.Error;

                StringList list = null;

                if (ParserOps<string>.SplitList(
                        interpreter, text, 0, Length.Invalid, true,
                        ref list, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                PopulateDictionary(
                    breakpointType, list, ref dictionary);

                return ReturnCode.Ok;
            }
            finally
            {
                if (webClient != null)
                {
                    if (dispose)
                    {
                        ObjectOps.TryDisposeOrComplain<WebClient>(
                            interpreter, ref webClient);
                    }

                    webClient = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this network variable has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this network variable has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this network variable has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(NetworkVariable).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this network variable.
        /// It implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    /* IGNORED */
                    ObjectOps.TryDisposeOrTrace<WebClient>(
                        ref cachedWebClient);

                    cachedWebClient = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this network variable and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this network variable, releasing any resources that were
        /// not released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~NetworkVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
