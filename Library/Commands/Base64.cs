/*
 * Base64.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
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
    [ObjectId("b2cf12bb-e35a-4039-9736-3da91e590777")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("string")]
    internal sealed class Base64 : Core
    {
        public Base64(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "decode", "encode"
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
                                case "decode":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        try
                                                        {
                                                            string stringValue = null;

                                                            code = StringOps.GetString(encoding,
                                                                Convert.FromBase64String(arguments[argumentIndex]),
                                                                EncodingType.Binary, ref stringValue, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = stringValue;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
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
                                                        result = "wrong # args: should be \"base64 decode ?options? string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"base64 decode ?options? string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "encode":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                                Option.CreateEndOfOptions()
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        try
                                                        {
                                                            byte[] bytes = null;

                                                            code = StringOps.GetBytes(
                                                                encoding, arguments[argumentIndex],
                                                                EncodingType.Binary, true, ref bytes,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                result = Convert.ToBase64String(bytes,
                                                                    Base64FormattingOptions.InsertLineBreaks);
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
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
                                                        result = "wrong # args: should be \"base64 encode ?options? string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"base64 encode ?options? string\"";
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
                        result = "wrong # args: should be \"base64 option ?arg ...?\"";
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
