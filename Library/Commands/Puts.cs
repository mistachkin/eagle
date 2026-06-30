/*
 * Puts.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>puts</c> command, which writes a
    /// string to a channel (by default the standard output channel),
    /// optionally appending a trailing newline.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("646d87e5-b37f-46e4-a8d7-1b8e70234d93")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Puts : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>puts</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Puts(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>puts</c> command.  It parses any
        /// options (for example <c>-nonewline</c> and <c>-encoding</c>),
        /// resolves the target channel (defaulting to standard output),
        /// converts the supplied string using the channel encoding, and
        /// writes it to the channel, appending a newline unless suppressed.
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
        /// command name; the remaining elements are any options, the optional
        /// channel identifier, and the string to be written.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the string is written
        /// successfully; otherwise, <see cref="ReturnCode.Error" /> when the
        /// wrong number of arguments is supplied, an option or channel is
        /// invalid, the interpreter is null, the argument list is null, or an
        /// exception occurs during writing, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    int argumentCount = arguments.Count;

                    if (argumentCount >= 2)
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Puts);

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) >= argumentCount))
                            {
                                bool useCount = false;

                                if (options.IsPresent("-usecount"))
                                    useCount = true;

                                bool useObject = false;

                                if (options.IsPresent("-useobject"))
                                    useObject = true;

                                bool newLine = true;

                                if (options.IsPresent("-nonewline"))
                                    newLine = false;

                                IVariant value = null;
                                Encoding encoding = null;

                                if (options.IsPresent("-encoding", ref value))
                                    encoding = (Encoding)value.Value;

                                string channelId = Channel.StdOut;

                                if ((argumentIndex + 1) < argumentCount)
                                    channelId = arguments[argumentIndex];

                                IChannel channel = interpreter.InternalGetChannel(channelId, ref result);

                                if (channel != null)
                                {
                                    if (encoding == null)
                                        encoding = channel.GetEncoding();

                                    if (channel.NullEncoding || (encoding != null))
                                    {
                                        StringBuilder builder; /* REUSED */
                                        int outputIndex = argumentCount - 1;
                                        Argument outputArgument = arguments[outputIndex];
                                        string outputString;

                                        if (newLine)
                                        {
                                            if (useCount)
                                            {
                                                builder = null;

                                                code = StringOps.AppendCount(
                                                    encoding, null, outputArgument,
                                                    EncodingType.Text, ref builder,
                                                    ref result);

                                                if (code != ReturnCode.Ok)
                                                    goto done;

                                                builder.Append(outputArgument);
                                            }
                                            else
                                            {
                                                builder = StringBuilderFactory.Create(
                                                    outputArgument);
                                            }

                                            builder.Append(
                                                ConversionOps.ToChar(ChannelOps.NewLine));

                                            outputString = StringBuilderCache.GetStringAndRelease(
                                                ref builder);
                                        }
                                        else
                                        {
                                            if (useCount)
                                            {
                                                builder = null;

                                                code = StringOps.AppendCount(
                                                    encoding, null, outputArgument,
                                                    EncodingType.Text, ref builder,
                                                    ref result);

                                                if (code != ReturnCode.Ok)
                                                    goto done;

                                                builder.Append(outputArgument);

                                                outputString = StringBuilderCache.GetStringAndRelease(
                                                    ref builder);
                                            }
                                            else
                                            {
                                                outputString = outputArgument;
                                            }
                                        }

                                        try
                                        {
                                            if (channel.IsVirtualOutput)
                                            {
                                                if (useObject)
                                                {
                                                    //
                                                    // NOTE: The encoding is ignored, because this is
                                                    //       going to perform automatic type detection
                                                    //       and the underlying channel is responsible
                                                    //       for any value conversions.
                                                    //
                                                    code = interpreter.AppendObjectAsVirtualOutput(
                                                        outputString, LookupFlags.Default, channel,
                                                        ref result);
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: The encoding is ignored, because this is
                                                    //       directly from the input string, which is
                                                    //       already Unicode.
                                                    //
                                                    channel.AppendVirtualOutput(outputString);
                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                BinaryWriter binaryWriter = channel.GetBinaryWriter();

                                                if (binaryWriter != null)
                                                {
                                                    byte[] bytes = null;

                                                    if (useObject)
                                                    {
                                                        code = interpreter.GetObjectAsBytes(
                                                            encoding, outputString, LookupFlags.Default,
                                                            ref bytes, ref result);
                                                    }
                                                    else
                                                    {
                                                        code = StringOps.GetBytes(
                                                            encoding, outputString, EncodingType.Binary,
                                                            true, ref bytes, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        channel.CheckAppend();

#if CONSOLE
                                                        if (channel.IsConsoleStream)
                                                        {
                                                            int offset = 0;
                                                            int count = bytes.Length;

                                                            while (count > 0)
                                                            {
                                                                int writeCount = Math.Min(
                                                                    count, _Hosts.Console.SafeWriteSize);

                                                                binaryWriter.Write(bytes, offset, writeCount);

                                                                offset += writeCount;
                                                                count -= writeCount;
                                                            }
                                                        }
                                                        else
#endif
                                                        {
                                                            binaryWriter.Write(bytes);
                                                        }

#if MONO || MONO_HACKS
                                                        //
                                                        // HACK: *MONO* As of Mono 2.8.0, it seems that Mono "loses"
                                                        //       output unless a flush is performed right after a
                                                        //       write.  So far, this has only been observed for the
                                                        //       console channels; however, always using flush here
                                                        //       on Mono shouldn't cause too many problems, except a
                                                        //       slight loss in performance.
                                                        //       https://bugzilla.novell.com/show_bug.cgi?id=645193
                                                        //
                                                        if (CommonOps.Runtime.IsMono())
                                                        {
                                                            binaryWriter.Flush(); /* throw */
                                                        }
                                                        else
#endif
                                                        {
                                                            //
                                                            // NOTE: Check if we should automatically flush the channel
                                                            //       after each "logical" write done by this command.
                                                            //
                                                            /* IGNORED */
                                                            channel.CheckAutoFlush();
                                                        }

                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "failed to get binary writer for channel \"{0}\"",
                                                        channelId);

                                                    code = ReturnCode.Error;
                                                }
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
                                        result = String.Format(
                                            "failed to get encoding for output channel \"{0}\"", 
                                            channelId);

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
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex], !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    result = "wrong # args: should be \"puts ?-nonewline? ?channelId? string\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"puts ?-nonewline? ?channelId? string\"";
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

        done:

            return code;
        }
        #endregion
    }
}
