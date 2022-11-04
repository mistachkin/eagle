/*
 * Fcopy.cs --
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
    [ObjectId("172e9e19-c6c3-44ee-9ff7-df5b72c0decd")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Fcopy : Core
    {
        private static readonly int MaximumReadSize = (int)PlatformOps.GetPageSize();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Fcopy(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                    if (arguments.Count >= 3)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-size", null),
                            new Option(null, OptionFlags.MustHaveValue | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-command", null),
                            new Option(typeof(EventFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-eventflags",
                                new Variant(interpreter.EngineEventFlags)),
                            Option.CreateEndOfOptions()
                        });

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 3)
                            code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;

                        if (code == ReturnCode.Ok)
                        {
                            if (argumentIndex == Index.Invalid)
                            {
                                Variant value = null;
                                int size = _Size.Invalid;

                                if (options.IsPresent("-size", ref value))
                                {
                                    size = (int)value.Value;

                                    //
                                    // NOTE: All negative values become "invalid",
                                    //       which means "read until end-of-file".
                                    //
                                    if (size < 0)
                                        size = _Size.Invalid;
                                }

#if MONO_BUILD
#pragma warning disable 219
#endif
                                string command = null; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                                if (options.IsPresent("-command", ref value))
                                    command = value.ToString(); /* NOT YET IMPLEMENTED */

                                EventFlags eventFlags = interpreter.EngineEventFlags;

                                if (options.IsPresent("-eventflags", ref value))
                                    eventFlags = (EventFlags)value.Value;

                                string inputChannelId = arguments[1];
                                IChannel inputChannel = interpreter.GetChannel(inputChannelId, ref result);

                                if (inputChannel != null)
                                {
                                    if (inputChannel.CanRead)
                                    {
                                        Encoding inputEncoding = inputChannel.GetEncoding();

                                        if (inputChannel.NullEncoding || (inputEncoding != null))
                                        {
                                            string outputChannelId = arguments[2];
                                            IChannel outputChannel = interpreter.GetChannel(outputChannelId, ref result);

                                            if (outputChannel != null)
                                            {
                                                if (outputChannel.CanWrite)
                                                {
                                                    Encoding outputEncoding = outputChannel.GetEncoding();

                                                    if (outputChannel.NullEncoding || (outputEncoding != null))
                                                    {
                                                        try
                                                        {
                                                            BinaryWriter binaryWriter = null; /* NOTE: Output channel. */
                                                            int outputBytes = 0;

                                                            //
                                                            // NOTE: Reset the end-of-file indicator here because we may
                                                            //       need to use it to terminate the loop.
                                                            //
                                                            inputChannel.HitEndOfStream = false;

                                                            do
                                                            {
                                                                if (inputChannel.AnyEndOfStream)
                                                                    break;

                                                                ByteList inputBuffer = null;

                                                                int readSize = size;

                                                                if ((readSize != _Size.Invalid) && (readSize > MaximumReadSize))
                                                                    readSize = MaximumReadSize;

                                                                if (readSize == _Size.Invalid)
                                                                    code = inputChannel.Read(null, false, false, ref inputBuffer, ref result);
                                                                else
                                                                    code = inputChannel.Read(readSize, null, false, false, ref inputBuffer, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    //
                                                                    // NOTE: Grab the input byte array from the input
                                                                    //       buffer byte list.
                                                                    //
                                                                    byte[] inputArray = inputBuffer.ToArray();

                                                                    //
                                                                    // NOTE: Update the total input byte count with the
                                                                    //       number of bytes we just read.
                                                                    //
                                                                    if (size != _Size.Invalid)
                                                                        size -= inputArray.Length;

                                                                    if (outputChannel.IsVirtualOutput)
                                                                    {
                                                                        //
                                                                        // NOTE: Virtual output means that we must get
                                                                        //       the text for the input bytes.
                                                                        //
                                                                        string stringValue = null;

                                                                        code = StringOps.GetString(
                                                                            inputEncoding, inputArray, EncodingType.Binary,
                                                                            ref stringValue, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            //
                                                                            // NOTE: The encoding is ignored, because this is
                                                                            //       directly from the input string, which is
                                                                            //       already Unicode.
                                                                            //
                                                                            outputChannel.AppendVirtualOutput(stringValue);

                                                                            //
                                                                            // NOTE: Update the total output byte count with
                                                                            //       the number of bytes we just wrote.
                                                                            //
                                                                            code = StringOps.AddByteCount(
                                                                                outputEncoding, stringValue, EncodingType.Binary,
                                                                                ref outputBytes, ref result);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (binaryWriter == null)
                                                                            binaryWriter = outputChannel.GetBinaryWriter();

                                                                        if (binaryWriter != null)
                                                                        {
                                                                            //
                                                                            // NOTE: Convert the input bytes into output
                                                                            //       bytes based on both the input and
                                                                            //       output encodings, if any.  If both
                                                                            //       encodings are null, the input bytes
                                                                            //       are used verbatim.
                                                                            //
                                                                            byte[] outputArray = null;

                                                                            code = StringOps.ConvertBytes(
                                                                                inputEncoding, outputEncoding, EncodingType.Binary,
                                                                                EncodingType.Binary, inputArray, ref outputArray,
                                                                                ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                //
                                                                                // NOTE: Ready the output channel for "append"
                                                                                //       mode, if necessary.
                                                                                //
                                                                                outputChannel.CheckAppend(); /* throw */

                                                                                //
                                                                                // NOTE: Attempt to write the output bytes to
                                                                                //       the output channel.
                                                                                //
                                                                                binaryWriter.Write(outputArray); /* throw */

#if MONO || MONO_HACKS
                                                                                //
                                                                                // HACK: *MONO* As of Mono 2.8.0, it seems that
                                                                                //       Mono "loses" output unless a flush is
                                                                                //       performed right after a write.  So far,
                                                                                //       this has only been observed for the
                                                                                //       console channels; however, always using
                                                                                //       flush here on Mono shouldn't cause too
                                                                                //       many problems, except a slight loss in
                                                                                //       performance.
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
                                                                                    // NOTE: Check if we should automatically
                                                                                    //       flush the channel after each write
                                                                                    //       done by this command.
                                                                                    //
                                                                                    /* IGNORED */
                                                                                    outputChannel.CheckAutoFlush();
                                                                                }

                                                                                //
                                                                                // NOTE: Update the total output byte count with
                                                                                //       the number of bytes we just wrote.
                                                                                //
                                                                                outputBytes += outputArray.Length;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "failed to get binary writer for channel \"{0}\"",
                                                                                outputChannelId);

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                }

                                                                //
                                                                // NOTE: If any of the above actions failed, bail out of the
                                                                //       copy loop now.
                                                                //
                                                                if (code != ReturnCode.Ok)
                                                                    break;

                                                                //
                                                                // NOTE: Are we done reading input bytes?  If this value is
                                                                //       less than zero, it means we read until end-of-file.
                                                                //       If we have read the specified number of bytes, bail
                                                                //       out.
                                                                //
                                                                if (size == 0)
                                                                    break;

                                                                //
                                                                // NOTE: Check for any pending events in the interpreter and
                                                                //       service them now.
                                                                //
                                                                code = Engine.CheckEvents(interpreter, eventFlags, ref result);

                                                                if (code != ReturnCode.Ok)
                                                                    break;
                                                            }
                                                            while (true);

                                                            //
                                                            // NOTE: Return the number of bytes written to the output
                                                            //       channel.
                                                            //
                                                            if (code == ReturnCode.Ok)
                                                                result = outputBytes;
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
                                                            outputChannelId);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "channel \"{0}\" wasn't opened for writing",
                                                        outputChannelId);

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
                                            result = String.Format(
                                                "failed to get encoding for input channel \"{0}\"",
                                                inputChannelId);

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "channel \"{0}\" wasn't opened for reading",
                                            inputChannelId);

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
                                result = "wrong # args: should be \"fcopy input output ?-size size? ?-command callback?\"";
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"fcopy input output ?-size size? ?-command callback?\"";
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
