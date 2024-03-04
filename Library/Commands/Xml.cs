/*
 * Xml.cs --
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

#if SERIALIZATION
using System.Text;
#endif

using System.Xml;
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
    [ObjectId("ab8802bd-bcfd-4042-8e75-ea85c4a67959")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Xml : Core
    {
        public Xml(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        //
        // TODO: In the future, there may be a LOT more functionality here 
        //       because this is intended to be the central clearinghouse 
        //       for all Xml related commands.
        //
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "deserialize", "foreach", "serialize", "validate"
        });

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

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
                                case "deserialize":
                                    {
                                        if (arguments.Count >= 4)
                                        {
#if SERIALIZATION
                                            OptionDictionary options = ObjectOps.GetDeserializeOptions();

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    bool verbose;
                                                    bool strictType;
                                                    bool noCase;

                                                    ObjectOps.ProcessGetTypeOptions(
                                                        options, out verbose, out strictType, out noCase);

                                                    Type returnType;
                                                    ObjectFlags objectFlags;
                                                    string objectName;
                                                    string interpName;
                                                    bool create;
                                                    bool dispose;
                                                    bool alias;
                                                    bool aliasRaw;
                                                    bool aliasAll;
                                                    bool aliasReference;
                                                    bool toString;

                                                    ObjectOps.ProcessFixupReturnValueOptions(
                                                        options, null, out returnType, out objectFlags,
                                                        out objectName, out interpName, out create,
                                                        out dispose, out alias, out aliasRaw, out aliasAll,
                                                        out aliasReference, out toString);

                                                    if (noCase)
                                                        objectFlags |= ObjectFlags.NoCase;

                                                    IVariant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        CultureInfo cultureInfo = interpreter.InternalCultureInfo;
                                                        Type objectType = null;
                                                        ResultList errors = null;

                                                        code = Value.GetAnyType(interpreter,
                                                            arguments[argumentIndex], null, interpreter.GetAppDomain(),
                                                            Value.GetTypeValueFlags(strictType, verbose, noCase),
                                                            cultureInfo, ref objectType, ref errors);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            byte[] bytes = null;

                                                            code = StringOps.GetBytes(
                                                                encoding, arguments[argumentIndex + 1],
                                                                EncodingType.Xml, true, ref bytes,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                object @object = null;

                                                                code = XmlOps.Deserialize(
                                                                    objectType, bytes, ref @object, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    ObjectOptionType objectOptionType =
                                                                        ObjectOptionType.Deserialize |
                                                                        ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                                    code = MarshalOps.FixupReturnValue(
                                                                        interpreter, interpreter.InternalBinder,
                                                                        cultureInfo, returnType, objectFlags, options,
                                                                        ObjectOps.GetInvokeOptions(objectOptionType),
                                                                        objectOptionType, objectName, interpName,
                                                                        @object, create, dispose, alias,
                                                                        aliasReference, toString, ref result);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (errors == null)
                                                                errors = new ResultList();

                                                            errors.Insert(0, String.Format(
                                                                "type \"{0}\" not found",
                                                                arguments[argumentIndex]));

                                                            result = errors;
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
                                                        result = "wrong # args: should be \"xml deserialize ?options? type xml\"";
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
                                            result = "wrong # args: should be \"xml deserialize ?options? type xml\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "foreach":
                                    {
                                        if (arguments.Count >= 5)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-file", null),
                                                new Option(null, OptionFlags.MustHaveDictionaryValue, Index.Invalid, Index.Invalid, "-namespaces", null),
                                                new Option(null, OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-xpaths", null)
                                            }, ObjectOps.GetFixupReturnValueOptions());

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 3) == arguments.Count))
                                                {
                                                    Type returnType;
                                                    ObjectFlags objectFlags;
                                                    string objectName;
                                                    string interpName;
                                                    bool create;
                                                    bool dispose;
                                                    bool alias;
                                                    bool aliasRaw;
                                                    bool aliasAll;
                                                    bool aliasReference;
                                                    bool toString;

                                                    ObjectOps.ProcessFixupReturnValueOptions(
                                                        options, null, out returnType, out objectFlags,
                                                        out objectName, out interpName, out create,
                                                        out dispose, out alias, out aliasRaw, out aliasAll,
                                                        out aliasReference, out toString);

                                                    IVariant value = null;
                                                    StringDictionary namespaces = null;

                                                    if (options.IsPresent("-namespaces", ref value))
                                                        namespaces = (StringDictionary)value.Value;

                                                    StringList xpaths = null;

                                                    if (options.IsPresent("-xpaths", ref value))
                                                        xpaths = (StringList)value.Value;

                                                    bool file = false;

                                                    if (options.IsPresent("-file"))
                                                        file = true;

                                                    CultureInfo cultureInfo = interpreter.InternalCultureInfo;
                                                    string[] namespaceNames = null;
                                                    Uri[] namespaceUris = null;

                                                    if ((namespaces == null) || XmlOps.GetNamespaceArrays(
                                                            namespaces, cultureInfo, out namespaceNames,
                                                            out namespaceUris, ref result) == ReturnCode.Ok)
                                                    {
                                                        string xml = arguments[argumentIndex + 1];

                                                        XmlDocument document = null;

                                                        code = file ?
                                                            XmlOps.LoadFile(xml, ref document, ref result) :
                                                            XmlOps.LoadString(xml, ref document, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            XmlNodeList nodeList = null;

                                                            if (XmlOps.GetNodeList(
                                                                    document, namespaceNames, namespaceUris, xpaths,
                                                                    ref nodeList, ref result) == ReturnCode.Ok)
                                                            {
                                                                int iterationLimit = interpreter.InternalIterationLimit;
                                                                int iterationCount = 0;

                                                                ObjectOptionType objectOptionType = ObjectOptionType.ForEach |
                                                                    ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                                string varName = arguments[argumentIndex];
                                                                string body = arguments[argumentIndex + 2];
                                                                IScriptLocation location = arguments[argumentIndex + 2];

                                                                foreach (XmlNode node in nodeList)
                                                                {
                                                                    Result localResult = null; /* REUSED */

                                                                    code = MarshalOps.FixupReturnValue(
                                                                        interpreter, interpreter.InternalBinder,
                                                                        cultureInfo, returnType, objectFlags, options,
                                                                        ObjectOps.GetInvokeOptions(objectOptionType),
                                                                        objectOptionType, objectName, interpName,
                                                                        node, create, dispose, alias,
                                                                        aliasReference, toString, ref localResult);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        Engine.AddErrorInformation(interpreter, localResult,
                                                                            String.Format("{0}    (adding xml {1} object handle \"{2}\")",
                                                                                Environment.NewLine, subCommand, FormatOps.Ellipsis(varName)));

                                                                        result = localResult;
                                                                        break;
                                                                    }

                                                                    string newObjectName = localResult;

                                                                    localResult = null;

                                                                    code = interpreter.SetVariableValue(
                                                                        varName, newObjectName, ref localResult);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        ReturnCode removeCode;
                                                                        bool removeDispose = dispose;
                                                                        Result removeResult = null;

                                                                        removeCode = interpreter.MaybeRemoveObject(
                                                                            newObjectName, null, true, true, ref removeDispose,
                                                                            ref removeResult);

                                                                        if (removeCode != ReturnCode.Ok)
                                                                            DebugOps.Complain(interpreter, removeCode, removeResult);

                                                                        Engine.AddErrorInformation(interpreter, localResult,
                                                                            String.Format("{0}    (setting xml {1} loop variable \"{2}\")",
                                                                                Environment.NewLine, subCommand, FormatOps.Ellipsis(varName)));

                                                                        result = localResult;
                                                                        break;
                                                                    }

                                                                    localResult = null;

                                                                    code = interpreter.EvaluateScript(body, location, ref localResult);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        if (code == ReturnCode.Continue)
                                                                        {
                                                                            code = ReturnCode.Ok;
                                                                        }
                                                                        else if (code == ReturnCode.Break)
                                                                        {
                                                                            code = ReturnCode.Ok;
                                                                            break;
                                                                        }
                                                                        else if (code == ReturnCode.Error)
                                                                        {
                                                                            Engine.AddErrorInformation(interpreter, localResult,
                                                                                String.Format("{0}    (\"xml {1}\" body line {2})",
                                                                                    Environment.NewLine, subCommand, Interpreter.GetErrorLine(interpreter)));

                                                                            result = localResult;
                                                                            break;
                                                                        }
                                                                        else
                                                                        {
                                                                            break;
                                                                        }
                                                                    }

                                                                    if ((iterationLimit != Limits.Unlimited) &&
                                                                        (++iterationCount > iterationLimit))
                                                                    {
                                                                        result = String.Format(
                                                                            "iteration limit {0} exceeded",
                                                                            iterationLimit);

                                                                        code = ReturnCode.Error;
                                                                        break;
                                                                    }
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                    Engine.ResetResult(interpreter, ref result);
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
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
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(
                                                            options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"xml foreach ?options? varName xml body\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"xml foreach ?options? varName xml body\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "serialize":
                                    {
                                        if (arguments.Count >= 4)
                                        {
#if SERIALIZATION
                                            OptionDictionary options = ObjectOps.GetSerializeOptions();

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    bool noCase = false;

                                                    if (options.IsPresent("-nocase"))
                                                        noCase = true;

                                                    bool strictType = false;

                                                    if (options.IsPresent("-stricttype"))
                                                        strictType = true;

                                                    bool verbose = false;

                                                    if (options.IsPresent("-verbose"))
                                                        verbose = true;

                                                    IVariant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        CultureInfo cultureInfo = interpreter.InternalCultureInfo;
                                                        Type objectType = null;
                                                        ResultList errors = null;

                                                        code = Value.GetAnyType(interpreter,
                                                            arguments[argumentIndex], null, interpreter.GetAppDomain(),
                                                            Value.GetTypeValueFlags(strictType, verbose, noCase),
                                                            cultureInfo, ref objectType, ref errors);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            IObject @object = null;

                                                            code = interpreter.GetObject(
                                                                arguments[argumentIndex + 1], LookupFlags.Default,
                                                                ref @object, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                byte[] bytes = null;

                                                                code = XmlOps.Serialize(
                                                                    (@object != null) ? @object.Value : null,
                                                                    objectType, null, ref bytes, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    string stringValue = null;

                                                                    code = StringOps.GetString(
                                                                        encoding, bytes, EncodingType.Xml,
                                                                        ref stringValue, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                        result = stringValue;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (errors == null)
                                                                errors = new ResultList();

                                                            errors.Insert(0, String.Format(
                                                                "type \"{0}\" not found",
                                                                arguments[argumentIndex]));

                                                            result = errors;
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
                                                        result = "wrong # args: should be \"xml serialize ?options? type object\"";
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
                                            result = "wrong # args: should be \"xml serialize ?options? type object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "validate":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            XmlDocument document = null;

                                            code = XmlOps.LoadString(
                                                arguments[3], ref document,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = XmlOps.Validate(
                                                    arguments[2], document,
                                                    false, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"xml validate schemaXml documentXml\"";
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
                        result = "wrong # args: should be \"xml option ?arg ...?\"";
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
