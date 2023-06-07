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
    [ObjectId("26cc91be-98bb-4e2c-932c-625724699d3e")]
    public sealed class NetworkVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDisposable
    {
        #region Private Constants
        #region Network Request Parameter Names
        private static string DefaultApiKeyParameterName = "apiKey";
        private static string DefaultMethodParameterName = "method";
        private static string DefaultPatternParameterName = "pattern";
        private static string DefaultNoCaseParameterName = "noCase";
        private static string DefaultNameParameterName = "name";
        private static string DefaultValueParameterName = "value";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Parameter Values
        private static bool DefaultNoCaseParameterValue = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Method Names
        //
        // HACK: These are purposely not read-only.
        //
        private static string DefaultExistMethodName = "exist";
        private static string DefaultCountMethodName = "count";
        private static string DefaultNamesMethodName = "names";
        private static string DefaultValuesMethodName = "values";
        private static string DefaultAllMethodName = "all";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string DefaultGetMethodName = "get";
        private static string DefaultSetMethodName = "set";
        private static string DefaultUnsetMethodName = "unset";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string DefaultPurgeMethodName = "purge";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Request Handling
        //
        // HACK: This is purposely not read-only.
        //
        private static int QueryStringLength = 256;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static bool DefaultUseNewNetworkClientCallback = true;
        private static bool DefaultUseCachedWebClient = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static bool TraceRequestInput = false;
        private static bool TraceRequestTime = false;
        private static bool TraceRequestOutput = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Network Response Processing
        //
        // HACK: This is purposely not read-only.
        //
        private static string AnonymousValuePrefix = "NoName";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string OkValue = "OK";
        private static string ErrorValue = "ERROR";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private WebClient cachedWebClient;
        private bool useCachedWebClient;

        ///////////////////////////////////////////////////////////////////////

        private bool useNewNetworkClientCallback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private NetworkVariable()
        {
            cachedWebClient = null;
            useCachedWebClient = DefaultUseCachedWebClient;
            useNewNetworkClientCallback = DefaultUseNewNetworkClientCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        private NetworkVariable(
            NewNetworkClientCallback newNetworkClientCallback, /* in */
            string argument,                                   /* in */
            IClientData clientData,                            /* in */
            Uri baseUri,                                       /* in */
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
        public static NetworkVariable Create(
            NewNetworkClientCallback newNetworkClientCallback, /* in */
            string argument,                                   /* in */
            IClientData clientData,                            /* in */
            Uri baseUri,                                       /* in */
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
                clientData, baseUri, encoding,
                apiKeyParameterName, apiKeyParameterValue,
                methodParameterName, patternParameterName,
                noCaseParameterName, nameParameterName,
                valueParameterName, permissions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        #region Public Properties
        private NewNetworkClientCallback newNetworkClientCallback;
        public NewNetworkClientCallback NewNetworkClientCallback
        {
            get { CheckDisposed(); return newNetworkClientCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string argument;
        public string Argument
        {
            get { CheckDisposed(); return argument; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri baseUri;
        public Uri BaseUri
        {
            get { CheckDisposed(); return baseUri; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding encoding;
        public Encoding Encoding
        {
            get { CheckDisposed(); return encoding; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string apiKeyParameterName;
        public string ApiKeyParameterName
        {
            get { CheckDisposed(); return apiKeyParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string apiKeyParameterValue;
        public string ApiKeyParameterValue
        {
            get { CheckDisposed(); return apiKeyParameterValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string methodParameterName;
        public string MethodParameterName
        {
            get { CheckDisposed(); return methodParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string patternParameterName;
        public string PatternParameterName
        {
            get { CheckDisposed(); return patternParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string noCaseParameterName;
        public string NoCaseParameterName
        {
            get { CheckDisposed(); return noCaseParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string nameParameterName;
        public string NameParameterName
        {
            get { CheckDisposed(); return nameParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string valueParameterName;
        public string ValueParameterName
        {
            get { CheckDisposed(); return valueParameterName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType permissions;
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Sub-Command Helper Methods
        public bool DoesExist(
            Interpreter interpreter, /* in */
            string name              /* in */
            )
        {
            CheckDisposed();

            return DoesExistViaNetwork(interpreter, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public long? GetCount(
            Interpreter interpreter, /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            long count = 0;

            if (GetCountViaNetwork(
                    interpreter, ref count, ref error) == ReturnCode.Ok)
            {
                return count;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    ref dictionary, ref error) == ReturnCode.Ok)
            {
                return dictionary;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    ref dictionary, ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, false, mode, pattern, null,
                    null, null, null, noCase, regExOptions) as StringList;

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), null, false);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    ref dictionary, ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, true, StringOps.DefaultMatchMode,
                    pattern, null, null, null, null, noCase, RegexOptions.None)
                    as StringList;

                return ParserOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), null, false);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
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

        #region Trace Callback Method
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
            CheckDisposed();

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
                        interpreter, ref dispose, ref result);

                    if (webClient == null)
                        return ReturnCode.Error;

                    string varValue = StringOps.GetStringFromObject(
                        traceInfo.NewValue);

                    string text = PerformWebRequest(interpreter,
                        webClient, breakpointType, traceInfo.Flags,
                        null, DefaultNoCaseParameterValue, varName,
                        varValue, ref result);

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
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Flags Helper Methods
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

        private static CultureInfo GetCultureInfo(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.InternalCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool ShouldUseNewNetworkClientCallback()
        {
            return useNewNetworkClientCallback &&
                (newNetworkClientCallback != null);
        }

        ///////////////////////////////////////////////////////////////////////

        private WebClient CreateWebClientViaCallback(
            Interpreter interpreter, /* in */
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

        private WebClient CreateWebClientViaInterpreter(
            Interpreter interpreter, /* in */
            ref bool dispose,        /* out */
            ref Result error         /* out */
            )
        {
            WebClient webClient = WebOps.CreateClient(
                interpreter, argument, clientData,
                WebOps.GetTimeout(interpreter), ref error);

            SetCachedWebClient(webClient, ref dispose);
            return webClient;
        }

        ///////////////////////////////////////////////////////////////////////

        private WebClient MaybeCreateWebClient(
            Interpreter interpreter, /* in */
            ref bool dispose,        /* out */
            ref Result error         /* out */
            )
        {
            WebClient webClient = GetCachedWebClient(
                ref dispose);

            if (webClient != null)
                return webClient;

            if (ShouldUseNewNetworkClientCallback())
            {
                return CreateWebClientViaCallback(
                    interpreter, ref dispose, ref error);
            }
            else
            {
                return CreateWebClientViaInterpreter(
                    interpreter, ref dispose, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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

        private string PerformWebRequest(
            Interpreter interpreter,       /* in */
            WebClient webClient,           /* in */
            BreakpointType breakpointType, /* in */
            VariableFlags variableFlags,   /* in */
            string pattern,                /* in */
            bool noCase,                   /* in */
            string name,                   /* in */
            string value,                  /* in */
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
                            false, ref localError) != ReturnCode.Ok)
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
                        webClient, uri, data, profiler,
                        ref localError) as byte[];

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
                            false, ref localError) != ReturnCode.Ok)
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
                        webClient, uri, data, profiler,
                        ref localError) as string;

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
        private bool DoesExistViaNetwork( /* CANARY */
            Interpreter interpreter, /* in */
            string name              /* in */
            )
        {
            bool success = false;
            Result error = null;

            try
            {
                if (!HasFlags(BreakpointType.BeforeVariableExist, true))
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
                        interpreter, ref dispose, ref error);

                    if (webClient == null)
                        return false;

                    string text = PerformWebRequest(
                        interpreter, webClient,
                        BreakpointType.BeforeVariableExist,
                        VariableFlags.None, null,
                        DefaultNoCaseParameterValue, name,
                        null, ref error);

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

        private ReturnCode GetCountViaNetwork(
            Interpreter interpreter, /* in */
            ref long count,          /* in */
            ref Result error         /* out */
            )
        {
            if (!HasFlags(BreakpointType.BeforeVariableCount, true))
            {
                error = "permission denied";
                return ReturnCode.Error;
            }

            WebClient webClient = null;
            bool dispose = true;

            try
            {
                webClient = MaybeCreateWebClient(
                    interpreter, ref dispose, ref error);

                if (webClient == null)
                    return ReturnCode.Error;

                string text = PerformWebRequest(
                    interpreter, webClient,
                    BreakpointType.BeforeVariableCount,
                    VariableFlags.None, null,
                    DefaultNoCaseParameterValue, null,
                    null, ref error);

                if (text == null)
                    return ReturnCode.Error;

                return Value.GetWideInteger2(
                    text, ValueFlags.AnyWideInteger, GetCultureInfo(
                    interpreter), ref count, ref error);
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

        private ReturnCode GetListViaNetwork(
            Interpreter interpreter,         /* in */
            string pattern,                  /* in */
            bool noCase,                     /* in */
            bool names,                      /* in */
            bool values,                     /* in */
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
                    interpreter, ref dispose, ref error);

                if (webClient == null)
                    return ReturnCode.Error;

                string text = PerformWebRequest(interpreter,
                    webClient, breakpointType, VariableFlags.None,
                    pattern, noCase, null, null, ref error);

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
        private bool disposed;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~NetworkVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
