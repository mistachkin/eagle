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
    /// <summary>
    /// This class implements the Eagle <c>gets</c> command, which reads the
    /// next line from a channel, optionally storing it into a variable and
    /// returning the number of characters read.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("bfc8553d-5fb7-4f5c-9eba-4957473258ef")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Gets : Core
    {
        #region Private Constants
        /// <summary>
        /// The error message used when this command is invoked with the wrong
        /// number of arguments.
        /// </summary>
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"gets ?options? channelId ?varName?\"";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>gets</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method executes the <c>gets</c> command.  It parses any
        /// options, locates the input channel named by the
        /// <c>channelId</c> argument, reads the next line from it honoring
        /// the requested encoding and end-of-line handling, and either stores
        /// the line into the optional variable or returns it directly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; it is followed by optional command options, the
        /// required channel identifier, and an optional variable name to
        /// receive the line that was read.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, when no variable name is supplied this contains the
        /// line that was read; when a variable name is supplied this contains
        /// the number of characters read, or
        /// <see cref="ChannelStream.EndOfFile" /> when end-of-stream is
        /// reached with no characters.  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter is null, the
        /// argument list is null or has the wrong number of arguments, an
        /// option is invalid, the channel cannot be found, its encoding
        /// cannot be determined, or an error occurs while reading or storing
        /// the line, with details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
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
