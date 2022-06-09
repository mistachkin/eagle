/*
 * Fconfigure.cs --
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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    [ObjectId("fde0d977-c772-4db3-9d81-3fa24d760166")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Fconfigure : Core
    {
        public Fconfigure(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

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
                        string channelId = arguments[1];
                        IChannel channel = interpreter.GetChannel(channelId, ref result);

                        if (channel != null)
                        {
                            try
                            {
                                if (arguments.Count >= 4)
                                {
                                    OptionDictionary options = new OptionDictionary(
                                        new IOption[] {
                                        new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-blocking", null),
                                        new Option(null, OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                        new Option(null, OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-translation", null)
                                    });

                                    int argumentIndex = Index.Invalid;

                                    code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (argumentIndex == Index.Invalid)
                                        {
                                            Variant value = null;
                                            bool? blockingMode = null;

                                            if (options.IsPresent("-blocking", ref value))
                                                blockingMode = (bool)value.Value;

                                            Encoding encoding = null;

                                            if (options.IsPresent("-encoding", ref value))
                                                encoding = (Encoding)value.Value;

                                            StringList translationNames = null;

                                            if (options.IsPresent("-translation", ref value))
                                                translationNames = (StringList)value.Value;

                                            StreamTranslationList translation = null;

                                            if (translationNames != null)
                                            {
                                                if ((translationNames.Count == 1) || (translationNames.Count == 2))
                                                {
                                                    translation = new StreamTranslationList();

                                                    foreach (string translationName in translationNames)
                                                    {
                                                        object enumValue = EnumOps.TryParse(
                                                            typeof(StreamTranslation), translationName,
                                                            true, true);

                                                        if (enumValue is StreamTranslation)
                                                        {
                                                            translation.Add((StreamTranslation)enumValue);
                                                        }
                                                        else
                                                        {
                                                            result = ScriptOps.BadValue(
                                                                null, "value for -translation", translationName,
                                                                Enum.GetNames(typeof(StreamTranslation)),
                                                                null, null);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "bad value for -translation: must be a one or two element list";
                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (blockingMode != null)
                                                    channel.SetBlockingMode((bool)blockingMode);

                                                if (encoding != null)
                                                    channel.SetEncoding(encoding);

                                                if (translation != null)
                                                    channel.SetTranslation(translation);

                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"fconfigure channelId ?optionName? ?value?\"";
                                            code = ReturnCode.Error;
                                        }
                                    }
                                }
                                else if (arguments.Count == 3)
                                {
                                    OptionDictionary options = new OptionDictionary(
                                        new IOption[] {
                                        new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-blocking", null),
                                        new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-encoding", null),
                                        new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-translation", null)
                                    });

                                    int argumentIndex = Index.Invalid;

                                    code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (argumentIndex == Index.Invalid)
                                        {
                                            StringList list = new StringList();

                                            if (options.IsPresent("-blocking"))
                                                list.Add(channel.GetBlockingMode().ToString());

                                            if (options.IsPresent("-encoding"))
                                            {
                                                Encoding encoding = channel.GetEncoding();

                                                if (encoding != null)
                                                    list.Add(encoding.WebName);
                                                else
                                                    list.Add(StringOps.NullEncodingName);
                                            }

                                            if (options.IsPresent("-translation"))
                                            {
                                                StreamTranslationList translation = channel.GetTranslation();

                                                if (translation != null)
                                                    list.Add(translation.ToString());
                                                else
                                                    list.Add((string)null);
                                            }

                                            if (list.Count > 1)
                                                result = list;
                                            else if (list.Count == 1)
                                                result = list[0];
                                            else
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"fconfigure channelId ?optionName? ?value?\"";
                                            code = ReturnCode.Error;
                                        }
                                    }
                                }
                                else
                                {
                                    Encoding encoding = channel.GetEncoding();
                                    StreamTranslationList translation = channel.GetTranslation();

                                    result = StringList.MakeList(
                                        "-blocking", channel.GetBlockingMode(),
                                        "-encoding", (encoding != null) ?
                                            encoding.WebName : StringOps.NullEncodingName,
                                        "-translation", translation);
                                }
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
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"fconfigure channelId ?optionName? ?value?\"";
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
