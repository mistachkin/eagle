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

#if SERIALIZATION
using System.Text;
#endif

using System.Xml;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;

#if SERIALIZATION
using Eagle._Constants;
#endif

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
            "deserialize", "serialize", "validate"
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

                                                    Variant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Type objectType = null;
                                                        ResultList errors = null;

                                                        code = Value.GetAnyType(interpreter,
                                                            arguments[argumentIndex], null, interpreter.GetAppDomain(),
                                                            Value.GetTypeValueFlags(strictType, verbose, noCase),
                                                            interpreter.InternalCultureInfo, ref objectType, ref errors);

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
                                                                        interpreter, interpreter.InternalBinder, interpreter.InternalCultureInfo,
                                                                        returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                        objectOptionType), objectOptionType, objectName, interpName,
                                                                        @object, create, dispose, alias, aliasReference, toString,
                                                                        ref result);
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

                                                    Variant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Type objectType = null;
                                                        ResultList errors = null;

                                                        code = Value.GetAnyType(interpreter,
                                                            arguments[argumentIndex], null, interpreter.GetAppDomain(),
                                                            Value.GetTypeValueFlags(strictType, verbose, noCase),
                                                            interpreter.InternalCultureInfo, ref objectType, ref errors);

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
