/*
 * Open.cs --
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
    [ObjectId("04da529a-10c9-4e92-b45e-cb6ae50e2c3d")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("fileSystem")]
    internal sealed class Open : Core
    {
        public Open(
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
                        string fileName = arguments[1];

                        if (interpreter.HasChannels(ref result))
                        {
                            MapOpenAccess access = MapOpenAccess.Default;
                            int permissions = 0; // NOTE: This is ONLY parsed, NOT used for opening the file.
                            string type = null;

                            if (arguments.Count >= 3)
                            {
                                Result enumString = arguments[2];

                                if (!String.IsNullOrEmpty(enumString))
                                    //
                                    // HACK: Translate illegal mode char "+" to what our Enum uses.
                                    //       This strategy will backfire later if we ever decide to
                                    //       allow parsing of the access mode as "flags" (via GetOptions).
                                    //
                                    enumString = enumString.Replace(Characters.PlusSign.ToString(), "Plus");

                                code = StringOps.StringToEnumList(interpreter, enumString, ref enumString);

                                if (code == ReturnCode.Ok)
                                {
                                    object enumValue = EnumOps.TryParse(
                                        typeof(MapOpenAccess), enumString,
                                        true, true);

                                    if (enumValue is MapOpenAccess)
                                    {
                                        access = (MapOpenAccess)enumValue;
                                    }
                                    else
                                    {
                                        enumString = ScriptOps.BadValue(
                                            "invalid", "access mode", arguments[2],
                                            Enum.GetNames(typeof(MapOpenAccess)), null, null);

                                        code = ReturnCode.Error;
                                    }
                                }

                                if (code != ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Transfer local result from above and add to the error info.
                                    //
                                    result = enumString;

                                    Engine.AddErrorInformation(interpreter, result,
                                        String.Format("{0}    (while processing open access modes \"{1}\")",
                                            Environment.NewLine, FormatOps.Ellipsis(arguments[2])));
                                }
                            }

                            if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                            {
                                code = Value.GetInteger2(
                                    (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                    interpreter.InternalCultureInfo, ref permissions, ref result);
                            }

                            if (code == ReturnCode.Ok)
                            {
                                if (arguments.Count >= 5)
                                    type = arguments[4];

                                OptionDictionary options = new OptionDictionary(
                                    new IOption[] {
#if CONSOLE
                                    new Option(null, OptionFlags.None, 1, Index.Invalid, "-stdin", null),
                                    new Option(null, OptionFlags.None, 1, Index.Invalid, "-stdout", null),
                                    new Option(null, OptionFlags.None, 1, Index.Invalid, "-stderr", null),
#else
                                    new Option(null, OptionFlags.Unsupported, 1, Index.Invalid, "-stdin", null),
                                    new Option(null, OptionFlags.Unsupported, 1, Index.Invalid, "-stdout", null),
                                    new Option(null, OptionFlags.Unsupported, 1, Index.Invalid, "-stderr", null),
#endif
                                    new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-channelid", null),
                                    new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-buffersize", null),
                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nullencoding", null),
                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-autoflush", null),
                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-rawendofstream", null),
                                    new Option(typeof(HostStreamFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-streamflags",
                                        new Variant(HostStreamFlags.Default)),
                                    new Option(typeof(FileOptions), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-options",
                                        new Variant(FileOptions.None)),
                                    new Option(typeof(FileShare), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-share",
                                        new Variant(FileShare.Read))
                                });

                                int argumentIndex = Index.Invalid;

                                if (arguments.Count > 5)
                                    code = interpreter.GetOptions(options, arguments, 0, 5, Index.Invalid, true, ref argumentIndex, ref result);
                                else
                                    code = ReturnCode.Ok;

                                if (code == ReturnCode.Ok)
                                {
                                    if (argumentIndex == Index.Invalid)
                                    {
                                        IVariant value = null;
                                        string channelId = null;

                                        if (options.IsPresent("-channelid", ref value))
                                            channelId = value.ToString();

                                        if ((channelId == null) ||
                                            (interpreter.DoesChannelExist(channelId) != ReturnCode.Ok))
                                        {
#if CONSOLE
                                            if (options.IsPresent("-stdin"))
                                            {
                                                //
                                                // NOTE: Enforce the proper access for the standard input
                                                //       channel.
                                                //
                                                if (access == MapOpenAccess.RdOnly)
                                                {
                                                    try
                                                    {
                                                        IStreamHost streamHost = interpreter.Host;

                                                        //
                                                        // NOTE: *WARNING* This option causes the "fileName",
                                                        //       "access", "permissions", and "type" arguments
                                                        //       to be ignored.
                                                        //
                                                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            if (streamHost.In == null)
                                                            {
                                                                int? bufferSize = null;

                                                                if (options.IsPresent("-buffersize", ref value))
                                                                    bufferSize = (int)value.Value;

                                                                streamHost.In = (bufferSize != null) ?
                                                                    Console.OpenStandardInput((int)bufferSize) :
                                                                    Console.OpenStandardInput();
                                                            }
                                                        }

                                                        code = interpreter.ModifyStandardChannels(
                                                            streamHost, channelId, ChannelType.Input |
                                                            ChannelType.ErrorOnExist, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = (channelId != null) ? channelId : StandardChannel.Input;
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
                                                        "illegal access mode \"{0}\", standard input " +
                                                        "can only be opened using access mode \"{1}\"",
                                                        access, MapOpenAccess.RdOnly);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else if (options.IsPresent("-stdout"))
                                            {
                                                //
                                                // NOTE: Enforce the proper access for the standard output
                                                //       channel.
                                                //
                                                if (access == MapOpenAccess.WrOnly)
                                                {
                                                    try
                                                    {
                                                        IStreamHost streamHost = interpreter.Host;

                                                        //
                                                        // NOTE: *WARNING* This option causes the "fileName",
                                                        //       "access", "permissions", and "type" arguments
                                                        //       to be ignored.
                                                        //
                                                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            if (streamHost.Out == null)
                                                            {
                                                                int? bufferSize = null;

                                                                if (options.IsPresent("-buffersize", ref value))
                                                                    bufferSize = (int)value.Value;

                                                                streamHost.Out = (bufferSize != null) ?
                                                                    Console.OpenStandardOutput((int)bufferSize) :
                                                                    Console.OpenStandardOutput();
                                                            }
                                                        }

                                                        code = interpreter.ModifyStandardChannels(
                                                            streamHost, channelId, ChannelType.Output |
                                                            ChannelType.ErrorOnExist, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = (channelId != null) ? channelId : StandardChannel.Output;
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
                                                        "illegal access mode \"{0}\", standard output " +
                                                        "can only be opened using access mode \"{1}\"",
                                                        access, MapOpenAccess.WrOnly);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else if (options.IsPresent("-stderr"))
                                            {
                                                //
                                                // NOTE: Enforce the proper access for the standard error
                                                //       channel.
                                                //
                                                if (access == MapOpenAccess.WrOnly)
                                                {
                                                    try
                                                    {
                                                        IStreamHost streamHost = interpreter.Host;

                                                        //
                                                        // NOTE: *WARNING* This option causes the "fileName",
                                                        //       "access", "permissions", and "type" arguments
                                                        //       to be ignored.
                                                        //
                                                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                        {
                                                            if (streamHost.Error == null)
                                                            {
                                                                int? bufferSize = null;

                                                                if (options.IsPresent("-buffersize", ref value))
                                                                    bufferSize = (int)value.Value;

                                                                streamHost.Error = (bufferSize != null) ?
                                                                    Console.OpenStandardError((int)bufferSize) :
                                                                    Console.OpenStandardError();
                                                            }
                                                        }

                                                        code = interpreter.ModifyStandardChannels(
                                                            streamHost, channelId, ChannelType.Error |
                                                            ChannelType.ErrorOnExist, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = (channelId != null) ? channelId : StandardChannel.Error;
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
                                                        "illegal access mode \"{0}\", standard error " +
                                                        "can only be opened using access mode \"{1}\"",
                                                        access, MapOpenAccess.WrOnly);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
#endif
                                            {
                                                Stream stream = null;
                                                bool nullEncoding = false;
                                                bool autoFlush = false;
                                                bool rawEndOfStream = false;

                                                switch (type)
                                                {
                                                    case null:                  /* FALL-THROUGH */
                                                    case /* String.Empty */ "": /* FALL-THROUGH */
                                                    case "file":
                                                        {
                                                            try
                                                            {
                                                                HostStreamFlags hostStreamFlags = HostStreamFlags.OpenCommand;
                                                                FileAccess fileAccess = FileOps.FileAccessFromAccess(access);
                                                                FileMode fileMode = FileOps.FileModeFromAccess(access);
                                                                FileShare fileShare = FileShare.Read;

                                                                if (options.IsPresent("-streamflags", ref value))
                                                                    hostStreamFlags = (HostStreamFlags)value.Value;

                                                                if (options.IsPresent("-share", ref value))
                                                                    fileShare = (FileShare)value.Value;

                                                                int bufferSize = ChannelOps.DefaultBufferSize;

                                                                if (options.IsPresent("-buffersize", ref value))
                                                                    bufferSize = (int)value.Value;

                                                                FileOptions fileOptions = FileOptions.None;

                                                                if (options.IsPresent("-options", ref value))
                                                                    fileOptions = (FileOptions)value.Value;

                                                                if (options.IsPresent("-nullencoding"))
                                                                    nullEncoding = true;

                                                                if (options.IsPresent("-autoflush"))
                                                                    autoFlush = true;

                                                                if (options.IsPresent("-rawendofstream"))
                                                                    rawEndOfStream = true;

                                                                bool seekToEof = false;

                                                                //
                                                                // HACK: Check for special case where they want to Append
                                                                //       and Read/ReadWrite.
                                                                //
                                                                if (((fileAccess == FileAccess.Read) ||
                                                                     (fileAccess == FileAccess.ReadWrite)) &&
                                                                    (FlagOps.HasFlags(access, MapOpenAccess.SeekToEof, true) ||
                                                                     FlagOps.HasFlags(access, MapOpenAccess.Append, true)))
                                                                {
                                                                    seekToEof = true;
                                                                }

                                                                code = interpreter.GetStream(
                                                                    fileName, fileMode, fileAccess, fileShare, bufferSize,
                                                                    fileOptions, ChannelOps.StrictGetStream, ref hostStreamFlags,
                                                                    ref stream, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if ((stream != null) && seekToEof)
                                                                        stream.Seek(0, SeekOrigin.End);
                                                                }
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                                result = e;
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    default:
                                                        {
                                                            result = String.Format(
                                                                "unsupported channel type \"{0}\"",
                                                                type);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                }

                                                //
                                                // NOTE: Did we manage to open the file successfully?
                                                //
                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (channelId == null)
                                                        channelId = FormatOps.Id("file", null, interpreter.NextId());

                                                    code = interpreter.AddFileOrSocketChannel(
                                                        channelId, stream, options, StreamFlags.None,
                                                        null, nullEncoding, FlagOps.HasFlags(access,
                                                        MapOpenAccess.Append, true), autoFlush,
                                                        rawEndOfStream, null, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = channelId;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "can't add \"{0}\": channel already exists",
                                                channelId);

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = "wrong # args: should be \"open fileName ?access? ?permissions? ?type? ?options?\"";
                                        code = ReturnCode.Error;
                                    }
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
                        result = "wrong # args: should be \"open fileName ?access? ?permissions? ?type? ?options?\"";
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
