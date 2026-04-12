/*
 * Gets.cs --
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
    [ObjectId("bfc8553d-5fb7-4f5c-9eba-4957473258ef")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Gets : Core
    {
        #region Private Constants
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"gets ?options? channelId ?varName?\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Gets(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            OptionDictionary options =
                CommandOptions.GetCommandOptions(
                    CommandOptionType.Gets);

            int argumentIndex = Index.Invalid;

            if (interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, false,
                    ref argumentIndex, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if ((argumentIndex == Index.Invalid) ||
                ((argumentIndex + 2) < arguments.Count))
            {
                if ((argumentIndex != Index.Invalid) &&
                    Option.LooksLikeOption(arguments[argumentIndex]))
                {
                    result = OptionDictionary.BadOption(
                        options, arguments[argumentIndex],
                        !interpreter.InternalIsSafe());
                }
                else
                {
                    result = WrongNumArgs;
                }

                return ReturnCode.Error;
            }

            IVariant value = null;
            Encoding encoding = null;

            if (options.IsPresent("-encoding", ref value))
                encoding = (Encoding)value.Value;

            bool useCount = false;

            if (options.IsPresent("-usecount"))
                useCount = true;

            int? count = null;

            if (options.IsPresent("-count", ref value))
                count = (int?)value.Value;

            bool? keepEol = null;

            if (options.IsPresent("-keepeol", ref value))
                keepEol = (bool)value.Value;

            bool noBlock = false;

            if (options.IsPresent("-noblock"))
                noBlock = true;

            string channelId = arguments[argumentIndex];

            IChannel channel = interpreter.InternalGetChannel(
                channelId, ref result);

            if (channel == null)
                return ReturnCode.Error;

            if (encoding == null)
                encoding = channel.GetEncoding();

            if (!channel.NullEncoding && (encoding == null))
            {
                result = String.Format(
                    "failed to get encoding for input channel {0}",
                    FormatOps.WrapOrNull(channelId));

                return ReturnCode.Error;
            }

            try
            {
                CharList endOfLine;
                bool useAnyEndOfLineChar;
                bool keepEndOfLineChars;

                channel.GetEndOfLineParameters(
                    out endOfLine, out useAnyEndOfLineChar,
                    out keepEndOfLineChars);

                if (keepEol != null)
                    keepEndOfLineChars = (bool)keepEol;

                ReturnCode code;
                ByteList buffer;

            retry:

                buffer = null;

                if (count != null)
                {
                    if (noBlock)
                    {
                        code = channel.ReadBuffer((int)count,
                            endOfLine, useAnyEndOfLineChar,
                            keepEndOfLineChars, ref buffer,
                            ref result);
                    }
                    else
                    {
                        code = channel.Read((int)count,
                            endOfLine, useAnyEndOfLineChar,
                            keepEndOfLineChars, ref buffer,
                            ref result);
                    }
                }
                else if (useCount)
                {
                    if (noBlock)
                    {
                        code = channel.ReadBuffer(
                            Count.PrefixSize, null, false,
                            false, ref buffer, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            code = StringOps.GetCount(
                                encoding, interpreter.InternalCultureInfo,
                                ArrayOps.GetArray<byte>(buffer, true),
                                EncodingType.Binary, ref count, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                useCount = false;
                                goto retry;
                            }
                        }
                    }
                    else
                    {
                        code = channel.Read(
                            Count.PrefixSize, null, false,
                            false, ref buffer, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            code = StringOps.GetCount(
                                encoding, interpreter.InternalCultureInfo,
                                ArrayOps.GetArray<byte>(buffer, true),
                                EncodingType.Binary, ref count, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                useCount = false;
                                goto retry;
                            }
                        }
                    }
                }
                else
                {
                    if (noBlock)
                    {
                        code = channel.ReadBuffer(
                            endOfLine, useAnyEndOfLineChar,
                            keepEndOfLineChars, ref buffer,
                            ref result);
                    }
                    else
                    {
                        code = channel.Read(
                            endOfLine, useAnyEndOfLineChar,
                            keepEndOfLineChars, ref buffer,
                            ref result);
                    }
                }

                if (code != ReturnCode.Ok)
                    return code;

                string stringValue = null;

                code = StringOps.GetString(
                    encoding, ArrayOps.GetArray<byte>(buffer, true),
                    EncodingType.Binary, ref stringValue, ref result);

                if (code != ReturnCode.Ok)
                    return code;

                if ((argumentIndex + 1) < arguments.Count)
                {
                    code = interpreter.SetVariableValue(
                        VariableFlags.None,
                        arguments[argumentIndex + 1],
                        stringValue, null, ref result);

                    if (code != ReturnCode.Ok)
                        return code;

                    int length = (stringValue != null) ?
                        stringValue.Length : 0;

                    if (length > 0)
                    {
                        result = length;
                    }
                    else
                    {
                        if (channel.OneEndOfStream)
                            result = ChannelStream.EndOfFile;
                        else
                            result = length; /* ZERO */
                    }
                }
                else
                {
                    result = stringValue;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                result = e;
                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
