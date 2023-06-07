/*
 * Uri.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NETWORK
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("ca27d807-1636-4d17-bbf2-ebbe91aed44f")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("network")]
    internal sealed class _Uri : Core
    {
        public _Uri(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "compare", "create", "download", "escape", "get", "host",
            "isvalid", "join", "offline", "parse", "ping", "post",
            "scheme", "security", "softwareupdates", "time",
            "unescape", "upload"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary allowedSubCommands = new EnsembleDictionary(
            PolicyOps.AllowedUriSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "compare":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(UriKind), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-kind", null),
                                                new Option(typeof(UriComponents), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-components",
                                                    new Variant(UriComponents.AbsoluteUri)),
                                                new Option(typeof(UriFormat), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-format", null),
                                                new Option(typeof(StringComparison), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-comparison", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    UriKind uriKind = UriKind.Absolute; // FIXME: Good default?

                                                    if (options.IsPresent("-kind", ref value))
                                                        uriKind = (UriKind)value.Value;

                                                    UriComponents uriComponents = UriComponents.AbsoluteUri; // FIXME: Good default?

                                                    if (options.IsPresent("-components", ref value))
                                                        uriComponents = (UriComponents)value.Value;

                                                    UriFormat uriFormat = UriFormat.UriEscaped; // FIXME: Good default?

                                                    if (options.IsPresent("-format", ref value))
                                                        uriFormat = (UriFormat)value.Value;

                                                    bool noCase = false;

                                                    if (options.IsPresent("-nocase"))
                                                        noCase = true;

                                                    StringComparison comparisonType =
                                                        SharedStringOps.GetBinaryComparisonType(noCase);

                                                    if (options.IsPresent("-comparison", ref value))
                                                        comparisonType = (StringComparison)value.Value;

                                                    Uri uri1 = null;

                                                    code = Value.GetUri(
                                                        arguments[argumentIndex], uriKind,
                                                        interpreter.InternalCultureInfo, ref uri1,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Uri uri2 = null;

                                                        code = Value.GetUri(
                                                            arguments[argumentIndex + 1], uriKind,
                                                            interpreter.InternalCultureInfo, ref uri2,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            result = Uri.Compare(
                                                                uri1, uri2, uriComponents, uriFormat,
                                                                comparisonType);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"uri compare ?options? uri1 uri2\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri compare ?options? uri1 uri2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-username", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-password", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-port", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-path", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-query", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-fragment", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 4)
                                                code = interpreter.GetOptions(options, arguments, 0, 4, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    Variant value = null;
                                                    string userName = null;

                                                    if (options.IsPresent("-username", ref value))
                                                        userName = value.ToString();

                                                    string password = null;

                                                    if (options.IsPresent("-password", ref value))
                                                        password = value.ToString();

                                                    int port = Port.Invalid;

                                                    if (options.IsPresent("-port", ref value))
                                                        port = (int)value.Value;

                                                    string path = null;

                                                    if (options.IsPresent("-path", ref value))
                                                        path = value.ToString();

                                                    string query = null;

                                                    if (options.IsPresent("-query", ref value))
                                                        query = value.ToString();

                                                    string fragment = null;

                                                    if (options.IsPresent("-fragment", ref value))
                                                        fragment = value.ToString();

                                                    try
                                                    {
                                                        UriBuilder uriBuilder =
                                                            new UriBuilder(arguments[2], arguments[3]);

                                                        if (userName != null)
                                                            uriBuilder.UserName = userName;

                                                        if (password != null)
                                                            uriBuilder.Password = password;

                                                        if (port != Port.Invalid)
                                                            uriBuilder.Port = port;

                                                        if (path != null)
                                                            uriBuilder.Path = path;

                                                        if (query != null)
                                                            uriBuilder.Query = query;

                                                        if (fragment != null)
                                                            uriBuilder.Fragment = fragment;

                                                        result = uriBuilder.ToString();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"uri create scheme host ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri create scheme host ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "download":
                                case "get":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            bool isMethod = SharedStringOps.SystemEquals(subCommand, "get");

#if NETWORK
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-timeout", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-callback", null),
                                                new Option(typeof(CallbackFlags), OptionFlags.Unsafe | OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid,
                                                    "-callbackflags", new Variant(CallbackFlags.Default)),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-inline", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noinline", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-trusted", null),
#if TEST
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noprotocol", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-obsolete", null),
#else
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-noprotocol", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-obsolete", null),
#endif
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-webclientdata", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) <= arguments.Count) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    Variant value = null;
                                                    IClientData localClientData = clientData;

                                                    if (options.IsPresent("-webclientdata", ref value))
                                                    {
                                                        IObject @object = (IObject)value.Value;

                                                        if (@object != null)
                                                        {
                                                            localClientData = _Public.ClientData.WrapOrReplace(
                                                                localClientData, @object.Value);
                                                        }
                                                        else
                                                        {
                                                            result = "option value has invalid data";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        int? timeout = WebOps.GetTimeout(interpreter);

                                                        if (options.IsPresent("-timeout", ref value))
                                                            timeout = (int)value.Value;

                                                        StringList callbackArguments = null;

                                                        if (options.IsPresent("-callback", ref value))
                                                            callbackArguments = (StringList)value.Value;

                                                        CallbackFlags callbackFlags = CallbackFlags.Default;

                                                        if (options.IsPresent("-callbackflags", ref value))
                                                            callbackFlags = (CallbackFlags)value.Value;

                                                        bool inline = isMethod;

                                                        if (options.IsPresent("-inline"))
                                                            inline = true;

                                                        if (options.IsPresent("-noinline"))
                                                            inline = false;

                                                        bool trusted = false;

                                                        if (options.IsPresent("-trusted"))
                                                            trusted = true;

#if TEST
                                                        bool noProtocol = false;

                                                        if (options.IsPresent("-noprotocol"))
                                                            noProtocol = true;

                                                        bool obsolete = false;

                                                        if (options.IsPresent("-obsolete"))
                                                            obsolete = true;
#endif

                                                        Encoding encoding = null;

                                                        if (options.IsPresent("-encoding", ref value))
                                                            encoding = (Encoding)value.Value;

                                                        Uri uri = null;

                                                        code = Value.GetUri(arguments[argumentIndex], UriKind.Absolute,
                                                            interpreter.InternalCultureInfo, ref uri, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (!interpreter.InternalIsSafe() ||
                                                                PolicyOps.IsTrustedUri(interpreter, uri, ref result))
                                                            {
                                                                string argument = null;

                                                                if ((argumentIndex + 2) == arguments.Count)
                                                                    argument = PathOps.GetNativePath(arguments[argumentIndex + 1]);

                                                                if (inline)
                                                                {
                                                                    //
                                                                    // NOTE: Do nothing.
                                                                    //
                                                                }
#if !NET_STANDARD_20 && !MONO
                                                                else if (!CommonOps.Runtime.IsMono())
                                                                {
                                                                    FilePermission permissions = FilePermission.Write |
                                                                        FilePermission.NotExists | FilePermission.File;

                                                                    code = FileOps.VerifyPath(argument, permissions, ref result);
                                                                }
#endif
                                                                else if (String.IsNullOrEmpty(argument))
                                                                {
                                                                    result = "invalid path";
                                                                    code = ReturnCode.Error;
                                                                }

#if TEST
                                                                if ((code == ReturnCode.Ok) && !noProtocol)
                                                                    code = WebOps.SetSecurityProtocol(obsolete, ref result);
#endif

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (inline)
                                                                    {
                                                                        //
                                                                        // NOTE: Is this an asynchronous request?
                                                                        //
                                                                        if (callbackArguments != null)
                                                                        {
                                                                            //
                                                                            // NOTE: The "-trusted" option is not supported for
                                                                            //       asynchronous downloads.  Instead, use the
                                                                            //       [uri softwareupdates] sub-command before
                                                                            //       and after (i.e. to allow for proper saving
                                                                            //       and restoring of the current trust setting).
                                                                            //
                                                                            if (!trusted)
                                                                            {
                                                                                code = WebOps.DownloadDataAsync(
                                                                                    interpreter, localClientData, callbackArguments,
                                                                                    callbackFlags, uri, timeout, ref result);
                                                                            }
                                                                            else
                                                                            {
                                                                                result = "-trusted cannot be used with -callback option";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            byte[] responseBytes = null;

                                                                            code = WebOps.DownloadData(
                                                                                interpreter, localClientData, uri, timeout, trusted,
                                                                                ref responseBytes, ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                string stringValue = null;

                                                                                code = StringOps.GetString(
                                                                                    encoding, responseBytes,
                                                                                    EncodingType.RemoteUri,
                                                                                    ref stringValue, ref result);

                                                                                if (code == ReturnCode.Ok)
                                                                                    result = stringValue;
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        //
                                                                        // NOTE: Is this an asynchronous request?
                                                                        //
                                                                        if (callbackArguments != null)
                                                                        {
                                                                            //
                                                                            // NOTE: The "-trusted" option is not supported for
                                                                            //       asynchronous downloads.  Instead, use the
                                                                            //       [uri softwareupdates] sub-command before
                                                                            //       and after (i.e. to allow for proper saving
                                                                            //       and restoring of the current trust setting).
                                                                            //
                                                                            if (!trusted)
                                                                            {
                                                                                code = WebOps.DownloadFileAsync(
                                                                                    interpreter, localClientData, callbackArguments,
                                                                                    callbackFlags, uri, argument, timeout, ref result);
                                                                            }
                                                                            else
                                                                            {
                                                                                result = "-trusted cannot be used with -callback option";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            code = WebOps.DownloadFile(
                                                                                interpreter, localClientData, uri, argument, timeout,
                                                                                trusted, ref result);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "wrong # args: should be \"{0} {1} ?options? uri ?argument?\"",
                                                            this.Name, subCommand);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? uri ?argument?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "escape":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            object enumValue = EnumOps.TryParse(
                                                typeof(UriEscapeType), arguments[2],
                                                true, true);

                                            if (enumValue is UriEscapeType)
                                            {
                                                UriEscapeType type = (UriEscapeType)enumValue;

                                                if (type == UriEscapeType.None)
                                                {
                                                    //
                                                    // NOTE: Ok, do nothing.
                                                    //
                                                    result = arguments[3];
                                                    code = ReturnCode.Ok;
                                                }
                                                else if (type == UriEscapeType.Uri)
                                                {
                                                    //
                                                    // NOTE: Escape an entire URI.
                                                    //
                                                    result = Uri.EscapeUriString(arguments[3]);
                                                    code = ReturnCode.Ok;
                                                }
                                                else if (type == UriEscapeType.Data)
                                                {
                                                    //
                                                    // NOTE: Escape data for use inside a URI.
                                                    //
                                                    result = Uri.EscapeDataString(arguments[3]);
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "uri escape type", arguments[2],
                                                        Enum.GetNames(typeof(UriEscapeType)), null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = ScriptOps.BadValue(
                                                    null, "uri escape type", arguments[2],
                                                    Enum.GetNames(typeof(UriEscapeType)), null, null);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri escape type string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "host":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = Uri.CheckHostName(arguments[2]);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri host uri\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isvalid":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            UriKind uriKind = UriKind.Absolute; // FIXME: Good default?

                                            if (arguments.Count == 4)
                                            {
                                                object enumValue = EnumOps.TryParse(
                                                    typeof(UriKind), arguments[3],
                                                    true, true);

                                                if (enumValue is UriKind)
                                                {
                                                    uriKind = (UriKind)enumValue;

                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "bad uri kind", arguments[3],
                                                        Enum.GetNames(typeof(UriKind)), null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            //
                                            // NOTE: Only continue if the supplied UriKind was valid.
                                            //
                                            if (code == ReturnCode.Ok)
                                            {
#if MONO_LEGACY
                                                try
                                                {
#endif
                                                    result = Uri.IsWellFormedUriString(arguments[2], uriKind);
#if MONO_LEGACY
                                                }
                                                catch
                                                {
                                                    result = false;
                                                }
#endif
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri isvalid uri ?kind?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "join":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            result = PathOps.CombinePath(
                                                true, arguments.GetRange(2, arguments.Count - 2));
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri join name ?name ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "offline":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if NETWORK
                                            if (arguments.Count == 3)
                                            {
                                                bool offline = false;

                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.InternalCultureInfo,
                                                    ref offline, ref result);

                                                if (code == ReturnCode.Ok)
                                                    WebOps.SetOfflineMode(offline);
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = WebOps.InOfflineMode();
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri offline ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "parse":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                UriBuilder uriBuilder =
                                                    new UriBuilder(arguments[2]);

                                                result = StringList.MakeList(
                                                    "-scheme", uriBuilder.Scheme,
                                                    "-host", uriBuilder.Host,
                                                    "-port", uriBuilder.Port,
                                                    "-username", uriBuilder.UserName,
                                                    "-password", uriBuilder.Password,
                                                    "-path", uriBuilder.Path,
                                                    "-query", uriBuilder.Query,
                                                    "-fragment", uriBuilder.Fragment);

                                                code = ReturnCode.Ok;
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri parse uri\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ping":
                                    {
                                        if (arguments.Count == 4)
                                        {
#if NETWORK
                                            int timeout = _Timeout.None;

                                            code = Value.GetInteger2(
                                                (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                interpreter.InternalCultureInfo, ref timeout,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Uri uri = null;
                                                IPStatus status = IPStatus.Unknown;
                                                long roundtripTime = 0;

                                                if (Value.GetUri(
                                                        arguments[2], UriKind.Absolute,
                                                        interpreter.InternalCultureInfo,
                                                        ref uri, ref result) == ReturnCode.Ok)
                                                {
                                                    DateTime now = TimeOps.GetUtcNow();

                                                    try
                                                    {
                                                        byte[] bytes = null; /* NOT USED */

                                                        code = WebOps.DownloadData(
                                                            interpreter, clientData, uri,
                                                            timeout, false, ref bytes,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            status = IPStatus.Success;
                                                        }
                                                        else if (result != null)
                                                        {
                                                            WebException e = result.Value as WebException;

                                                            if ((e != null) &&
                                                                (e.Status == WebExceptionStatus.Timeout))
                                                            {
                                                                status = IPStatus.TimedOut;
                                                                code = ReturnCode.Ok;
                                                            }
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        roundtripTime = ConversionOps.ToLong(
                                                            TimeOps.GetUtcNow().Subtract(
                                                                now).TotalMilliseconds);
                                                    }
                                                }
                                                else
                                                {
                                                    code = SocketOps.Ping(
                                                        arguments[2], timeout, ref status,
                                                        ref roundtripTime, ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = StringList.MakeList(
                                                        status, roundtripTime,
                                                        "milliseconds");
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri ping hostOrUri timeout\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "scheme":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = Uri.CheckSchemeName(arguments[2]);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri scheme name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "security":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            StringList list = new StringList();

                                            list.Separator = String.Format(
                                                "{0}{0}", Characters.LineFeed);

#if NETWORK
                                            list.Add("offline");
                                            list.Add(WebOps.InOfflineMode().ToString());

                                            list.Add("timeout");
                                            list.Add(WebOps.GetTimeout(interpreter).ToString());

#if TEST
                                            Result error; /* REUSED */

                                            error = null;

                                            if (WebOps.ProbeSecurityProtocol(
                                                    ref list, ref error) != ReturnCode.Ok)
                                            {
                                                list.Add("probedError");
                                                list.Add(error);
                                            }

                                            error = null;

                                            if (WebOps.GetSecurityProtocol(
                                                    ref list, ref error) != ReturnCode.Ok)
                                            {
                                                list.Add("getError");
                                                list.Add(error);
                                            }
#endif

                                            UpdateOps.GetStatus(ref list);
#endif

                                            result = list;
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri security\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "softwareupdates":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
#if NETWORK
                                            if (arguments.Count == 2)
                                            {
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                bool trusted = false;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref trusted,
                                                        ref result);
                                                }

                                                bool exclusive = false;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.InternalCultureInfo, ref exclusive,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    bool wasTrusted = UpdateOps.IsTrusted();
                                                    bool wasExclusive = UpdateOps.IsExclusive();

                                                    if ((trusted != wasTrusted) ||
                                                        (exclusive != wasExclusive))
                                                    {
                                                        if ((code == ReturnCode.Ok) &&
                                                            (trusted != wasTrusted))
                                                        {
                                                            code = UpdateOps.SetTrusted(
                                                                trusted, ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) &&
                                                            (exclusive != wasExclusive))
                                                        {
                                                            code = UpdateOps.SetExclusive(
                                                                exclusive, ref result);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "software update certificate is already {0}{1}",
                                                            wasTrusted ? "trusted" : "untrusted",
                                                            wasExclusive ? " exclusively" : String.Empty);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                result = String.Format(
                                                    "software update certificate is {0}{1}",
                                                    UpdateOps.IsTrusted() ? "trusted" : "untrusted",
                                                    UpdateOps.IsExclusive() ? " exclusively" : String.Empty);
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri softwareupdates ?trusted? ?exclusive?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "time":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if NETWORK
                                            DateTime localNow = TimeOps.GetUtcNow();
                                            string response = null;

                                            if (ScriptOps.QueryRemoteTime(
                                                    interpreter, clientData, null, WebOps.GetTimeout(
                                                    interpreter), ref response, ref result) == ReturnCode.Ok)
                                            {
                                                StringList list = null;

                                                if (ParserOps<string>.SplitList(
                                                        interpreter, response, 0, Length.Invalid, true,
                                                        ref list, ref result) == ReturnCode.Ok)
                                                {
                                                    if (list.Count >= 2)
                                                    {
                                                        if (SharedStringOps.Equals(
                                                                list[0], "OK", StringComparison.Ordinal))
                                                        {
                                                            double value = 0.0; /* milliseconds */

                                                            if (Value.GetDouble(
                                                                    list[1], ValueFlags.AnyDouble,
                                                                    interpreter.InternalCultureInfo,
                                                                    ref value, ref result) == ReturnCode.Ok)
                                                            {
                                                                DateTime remoteNow = DateTime.MinValue;
                                                                string units = null;

                                                                TimeOps.UnixMillisecondsOrSecondsToDateTime(
                                                                    value, ref remoteNow, ref value, ref units);

                                                                result = StringList.MakeList(
                                                                    "localNow", FormatOps.Iso8601DateTimeSeconds(localNow),
                                                                    "remoteNow", FormatOps.Iso8601DateTimeSeconds(remoteNow),
                                                                    "remoteRawValue", list[1],
                                                                    "remoteValue", value,
                                                                    "remoteUnits", units,
                                                                    "remoteDifference", remoteNow.Subtract(localNow));

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "time server response is not \"OK\"";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "time server response needs at least 2 elements";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri time\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unescape":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = Uri.UnescapeDataString(arguments[2]);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri unescape string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "upload":
                                case "post":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            bool isMethod = SharedStringOps.SystemEquals(subCommand, "post");

#if NETWORK
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-timeout", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-method", null),
                                                new Option(null, OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-data", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-callback", null),
                                                new Option(typeof(CallbackFlags), OptionFlags.Unsafe | OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid,
                                                    "-callbackflags", new Variant(CallbackFlags.Default)),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-inline", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noinline", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-raw", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-trusted", null),
#if TEST
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noprotocol", null),
                                                new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-obsolete", null),
#else
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-noprotocol", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-obsolete", null),
#endif
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                                new Option(null, OptionFlags.Unsafe | OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-webclientdata", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) <= arguments.Count) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    Variant value = null;
                                                    IClientData localClientData = clientData;

                                                    if (options.IsPresent("-webclientdata", ref value))
                                                    {
                                                        IObject @object = (IObject)value.Value;

                                                        if (@object != null)
                                                        {
                                                            localClientData = _Public.ClientData.WrapOrReplace(
                                                                localClientData, @object.Value);
                                                        }
                                                        else
                                                        {
                                                            result = "option value has invalid data";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        int? timeout = WebOps.GetTimeout(interpreter);

                                                        if (options.IsPresent("-timeout", ref value))
                                                            timeout = (int)value.Value;

                                                        string method = null;

                                                        if (options.IsPresent("-method", ref value))
                                                            method = value.ToString();

                                                        StringList listData = null;

                                                        if (options.IsPresent("-data", ref value))
                                                            listData = (StringList)value.Value;

                                                        StringList callbackArguments = null;

                                                        if (options.IsPresent("-callback", ref value))
                                                            callbackArguments = (StringList)value.Value;

                                                        CallbackFlags callbackFlags = CallbackFlags.Default;

                                                        if (options.IsPresent("-callbackflags", ref value))
                                                            callbackFlags = (CallbackFlags)value.Value;

                                                        bool inline = isMethod;

                                                        if (options.IsPresent("-inline"))
                                                            inline = true;

                                                        if (options.IsPresent("-noinline"))
                                                            inline = false;

                                                        bool raw = false;

                                                        if (options.IsPresent("-raw"))
                                                            raw = true;

                                                        bool trusted = false;

                                                        if (options.IsPresent("-trusted"))
                                                            trusted = true;

#if TEST
                                                        bool noProtocol = false;

                                                        if (options.IsPresent("-noprotocol"))
                                                            noProtocol = true;

                                                        bool obsolete = false;

                                                        if (options.IsPresent("-obsolete"))
                                                            obsolete = true;
#endif

                                                        Encoding encoding = null;

                                                        if (options.IsPresent("-encoding", ref value))
                                                            encoding = (Encoding)value.Value;

                                                        Uri uri = null;

                                                        code = Value.GetUri(arguments[argumentIndex], UriKind.Absolute,
                                                            interpreter.InternalCultureInfo, ref uri, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (!interpreter.InternalIsSafe() ||
                                                                PolicyOps.IsTrustedUri(interpreter, uri, ref result))
                                                            {
                                                                string argument = null;

                                                                if ((argumentIndex + 2) == arguments.Count)
                                                                    argument = PathOps.GetNativePath(arguments[argumentIndex + 1]);

                                                                if (inline)
                                                                {
                                                                    //
                                                                    // NOTE: Do nothing.
                                                                    //
                                                                }
#if !NET_STANDARD_20 && !MONO
                                                                else if (!CommonOps.Runtime.IsMono())
                                                                {
                                                                    FilePermission permissions = FilePermission.Read |
                                                                        FilePermission.Exists | FilePermission.File;

                                                                    code = FileOps.VerifyPath(argument, permissions, ref result);
                                                                }
#endif
                                                                else if (String.IsNullOrEmpty(argument))
                                                                {
                                                                    result = "invalid path";
                                                                    code = ReturnCode.Error;
                                                                }

#if TEST
                                                                if ((code == ReturnCode.Ok) && !noProtocol)
                                                                    code = WebOps.SetSecurityProtocol(obsolete, ref result);
#endif

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (inline)
                                                                    {
                                                                        //
                                                                        // NOTE: Is this an asynchronous request?
                                                                        //
                                                                        if (callbackArguments != null)
                                                                        {
                                                                            //
                                                                            // NOTE: The "-trusted" option is not supported for
                                                                            //       asynchronous uploads.  Instead, use the
                                                                            //       [uri softwareupdates] sub-command before
                                                                            //       and after (i.e. to allow for proper saving
                                                                            //       and restoring of the current trust setting).
                                                                            //
                                                                            if (!trusted)
                                                                            {
                                                                                if (raw)
                                                                                {
                                                                                    byte[] requestBytes = null;

                                                                                    code = ArrayOps.GetBytesFromList(
                                                                                        interpreter, listData, encoding,
                                                                                        ref requestBytes, ref result);

                                                                                    if (code == ReturnCode.Ok)
                                                                                    {
                                                                                        code = WebOps.UploadDataAsync(
                                                                                            interpreter, localClientData, callbackArguments,
                                                                                            callbackFlags, uri, method, requestBytes, timeout,
                                                                                            ref result);

                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = WebOps.UploadValuesAsync(
                                                                                        interpreter, localClientData, callbackArguments,
                                                                                        callbackFlags, uri, method,
                                                                                        ListOps.ToNameValueCollection(
                                                                                            listData, new NameValueCollection()),
                                                                                        timeout, ref result);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                result = "-trusted cannot be used with -callback option";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            byte[] responseBytes = null;

                                                                            if (raw)
                                                                            {
                                                                                byte[] requestBytes = null;

                                                                                code = ArrayOps.GetBytesFromList(
                                                                                    interpreter, listData, encoding,
                                                                                    ref requestBytes, ref result);

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    code = WebOps.UploadData(
                                                                                        interpreter, localClientData, uri, method,
                                                                                        requestBytes, timeout, trusted, ref responseBytes,
                                                                                        ref result);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = WebOps.UploadValues(
                                                                                    interpreter, localClientData, uri, method,
                                                                                    ListOps.ToNameValueCollection(
                                                                                        listData, new NameValueCollection()),
                                                                                    timeout, trusted, ref responseBytes, ref result);
                                                                            }

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                string stringValue = null;

                                                                                code = StringOps.GetString(
                                                                                    encoding, responseBytes,
                                                                                    EncodingType.RemoteUri,
                                                                                    ref stringValue, ref result);

                                                                                if (code == ReturnCode.Ok)
                                                                                    result = stringValue;
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        //
                                                                        // NOTE: Is this an asynchronous request?
                                                                        //
                                                                        if (callbackArguments != null)
                                                                        {
                                                                            //
                                                                            // NOTE: The "-trusted" option is not supported for
                                                                            //       asynchronous uploads.  Instead, use the
                                                                            //       [uri softwareupdates] sub-command before
                                                                            //       and after (i.e. to allow for proper saving
                                                                            //       and restoring of the current trust setting).
                                                                            //
                                                                            if (!trusted)
                                                                            {
                                                                                code = WebOps.UploadFileAsync(
                                                                                    interpreter, localClientData, callbackArguments,
                                                                                    callbackFlags, uri, method,
                                                                                    argument, timeout, ref result);
                                                                            }
                                                                            else
                                                                            {
                                                                                result = "-trusted cannot be used with -callback option";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            code = WebOps.UploadFile(
                                                                                interpreter, localClientData, uri, method, argument,
                                                                                timeout, trusted, ref result);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "wrong # args: should be \"{0} {1} ?options? uri ?argument?\"",
                                                            this.Name, subCommand);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"{0} {1} ?options? uri ?argument?\"",
                                                this.Name, subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"uri option ?arg ...?\"";
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
