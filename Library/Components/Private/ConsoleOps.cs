/*
 * ConsoleOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !CONSOLE
#error "This file cannot be compiled or used properly with console support disabled."
#endif

using System;
using System.IO;

#if !MONO || UNIX
using System.Reflection;
#endif

using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("69b75e27-9fd6-4cbe-844d-9c00002d0088")]
    internal static class ConsoleOps
    {
        #region Private Constants
        //
        // NOTE: Determine if we are running on Mono or .NET Core.  Cache the
        //       results of these checks for later use.
        //
        private static readonly bool isDotNetCore =
            CommonOps.Runtime.IsDotNetCore();

        private static readonly bool isMono =
            CommonOps.Runtime.IsMono();

        private static readonly bool isWindows =
            PlatformOps.IsWindowsOperatingSystem();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The Type object for the internal System.ConsolePal type.
        //       This is used, via reflection, by various methods of this
        //       class.
        //
        private static readonly Type ConsolePalType = isDotNetCore ?
            Type.GetType("System.ConsolePal") : null;

        //
        // NOTE: The Type object for the public System.IO.MonoIO type.  This
        //       is used, via reflection, by various methods of this class.
        //
        private static readonly Type MonoIoType = isMono ?
            Type.GetType("System.IO.MonoIO") : null;

        //
        // NOTE: The type for the System.Console class.  This is used, via
        //       reflection, by various methods of this class.
        //
        private static readonly Type ConsoleType = typeof(System.Console);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the console buffer size used when increasing from
        //       the .NET Framework default of 256 bytes.  There seems to
        //       be some limit less than 32K on what this can be; however,
        //       it is unclear what the exact limit is and where it comes
        //       from.
        //
        // HACK: This is purposely not read-only.
        //
        private static int ConsoleBufferSize = 16384;

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        //
        // NOTE: The type for the System.IO.StreamReader class.  This is used,
        //       via reflection, by various methods of this class.
        //
        private static readonly Type StreamReaderType = typeof(StreamReader);
#endif

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        //
        // NOTE: The Type object for the private System.ConsoleDriver type.
        //       This is used to obtain the System.TermInfoDriver instance.
        //
        private static readonly Type MonoConsoleDriverType = isMono ?
            Type.GetType("System.ConsoleDriver") : null;

        //
        // NOTE: The Type object for the private System.TermInfoDriver type.
        //       This is used to add fake input into the buffer, which can
        //       then be used to cause the Console.ReadLine to return null.
        //
        private static readonly Type MonoTermInfoDriverType = isMono ?
            Type.GetType("System.TermInfoDriver") : null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods (Shared)
        #region Per-Process Console (Setup) Reference Count Support
        public static string GetEnvironmentVariable(
            long processId /* in */
            )
        {
            return ProcessOps.GetEnvironmentVariable(
                EnvVars.EagleLibraryHostsConsole, processId);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CheckAndMaybeModifyReferenceCount(
            bool? increment,       /* in: OPTIONAL */
            out int referenceCount /* out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = ProcessOps.CheckAndMaybeModifyReferenceCount(
                EnvVars.EagleLibraryHostsConsole, null, increment,
                out referenceCount, ref error);

            if (code == ReturnCode.Ok)
            {
                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "CheckAndMaybeModifyReferenceCount: code = {0}, " +
                    "error = {1}", code, FormatOps.WrapOrNull(error)),
                    typeof(ConsoleOps).Name, TracePriority.HostError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSetup()
        {
            int referenceCount;

            return CheckAndMaybeModifyReferenceCount(
                null, out referenceCount) && (referenceCount > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsShared()
        {
            int referenceCount;

            return CheckAndMaybeModifyReferenceCount(
                null, out referenceCount) && (referenceCount > 1);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MarkSetup(
            bool setup /* in */
            )
        {
            int referenceCount;

            if (!CheckAndMaybeModifyReferenceCount(
                    setup, out referenceCount))
            {
                return false;
            }

            if (setup)
                return (referenceCount == 1);
            else
                return (referenceCount <= 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Console Configuration Methods
        private static void MaybeDisableConsole(
            ref bool console
            )
        {
            if (console && GlobalConfiguration.DoesValueExist(
                    EnvVars.NoConsole, ConfigurationFlags.ConsoleOps))
            {
                console = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeDisableVerbose(
            ref bool verbose
            )
        {
            if (verbose && GlobalConfiguration.DoesValueExist(
                    EnvVars.NoVerbose, ConfigurationFlags.ConsoleOps))
            {
                verbose = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Console Output Support
        public static void WriteCore(
            string value
            )
        {
            ConsoleColor savedForegroundColor;

            savedForegroundColor = Console.ForegroundColor; /* throw */

            //
            // TODO: Maybe change the background color here as well?
            //
            Console.ForegroundColor = HostOps.GetHighContrastColor(
                Console.BackgroundColor); /* throw */

            try
            {
                Console.WriteLine(value); /* throw */
            }
            finally
            {
                Console.ForegroundColor = savedForegroundColor; /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteCoreNoThrow(
            string value
            )
        {
            try
            {
                WriteCore(value); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ConsoleOps).Name,
                    TracePriority.HostError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WritePrompt(
            string value
            )
        {
            WriteCoreNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeWritePrompt(
            string value
            )
        {
            MaybeWritePrompt(value, true, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeWritePrompt(
            string value,
            bool console,
            bool verbose
            )
        {
            MaybeDisableConsole(ref console);
            MaybeDisableVerbose(ref verbose);

            if (console && verbose)
                WritePrompt(value);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteError(
            string value
            )
        {
            WriteCoreNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeWriteError(
            string value,
            bool console
            )
        {
            MaybeDisableConsole(ref console);

            if (console)
                WriteError(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteComplaint(
            string value
            )
        {
            WriteCoreNoThrow(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Console Debugging Support
        public static void DebugBreak()
        {
            //
            // NOTE: Display the debugger prompt and then wait for the
            //       interactive user to press any key.
            //
            WritePrompt(String.Format(
                DebugOps.IsBreakDisabled() ? _Constants.Prompt.NoBreak :
                _Constants.Prompt.Debugger, ProcessOps.GetId()));

            try
            {
                Console.ReadKey(true); /* throw */
            }
            catch (InvalidOperationException) // Console.ReadKey
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private static Type GetType(
            bool @private,
            ref Result error
            )
        {
            if (@private)
            {
                if (isDotNetCore)
                {
                    if (ConsolePalType == null)
                        error = "invalid .NET Core private console type";

                    return ConsolePalType;
                }

                if (isMono)
                {
                    if (MonoIoType == null)
                        error = "invalid Mono private console type";

                    return MonoIoType;
                }
            }

            if (ConsoleType == null)
                error = "invalid public system console type";

            return ConsoleType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods (Mono 2.0 - 5.12)
#if UNIX
        //
        // NOTE: This method is used to support the IDebugHost.Cancel method
        //       when running on Unix (Mono).
        //
        public static ReturnCode SimulateEndOfTransmission(
            ref Result error
            )
        {
            if (!isMono)
            {
                //
                // NOTE: This is only supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }

            if (MonoConsoleDriverType == null)
            {
                error = "missing \"System.ConsoleDriver\" type";
                return ReturnCode.Error;
            }

            if (MonoTermInfoDriverType == null)
            {
                error = "missing \"System.TermInfoDriver\" type";
                return ReturnCode.Error;
            }

            try
            {
                FieldInfo fieldInfo = MonoConsoleDriverType.GetField(
                    "driver", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticGetField, true));

                if (fieldInfo == null)
                {
                    error = String.Format(
                        "missing \"{0}{1}driver\" field",
                        FormatOps.RawTypeName(MonoConsoleDriverType),
                        Type.Delimiter);

                    return ReturnCode.Error;
                }

                object driver = fieldInfo.GetValue(null); /* throw */

                /* NO RESULT */
                MonoTermInfoDriverType.InvokeMember(
                    "AddToBuffer", ObjectOps.GetBindingFlags(
                    MetaBindingFlags.PrivateInstanceMethod,
                    true), null, driver, new object[] {
                        (int)Characters.EndOfTransmission
                    });

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods (.NET Framework 2.0 - 4.8.1)
        //
        // NOTE: This method is used to support the [host open], [host close],
        //       and [host screen] sub-commands as well as the IDebugHost.Exit
        //       method.
        //
        public static ReturnCode ResetStreams(
            ChannelType channelType,
            ref Result error
            )
        {
#if !MONO
            if (!isMono)
            {
                Type type = GetType(false, ref error);

                if (type == null)
                    return ReturnCode.Error;

                //
                // HACK: Because the System.Console object in the .NET Framework
                //       provides no means to reset the underlying input/output
                //       streams, we must do it here by force.
                //
                try
                {
                    //
                    // NOTE: Which standard channels do we want to reset?
                    //
                    bool resetInput = FlagOps.HasFlags(
                        channelType, ChannelType.Input, true);

                    bool resetOutput = FlagOps.HasFlags(
                        channelType, ChannelType.Output, true);

                    bool resetError = FlagOps.HasFlags(
                        channelType, ChannelType.Error, true);

                    BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticSetField, true);

                    if (!isDotNetCore)
                    {
                        if (resetInput)
                        {
                            type.InvokeMember(
                                "_consoleInputHandle", bindingFlags,
                                null, null, new object[] { IntPtr.Zero });
                        }

                        if (resetOutput)
                        {
                            type.InvokeMember(
                                "_consoleOutputHandle", bindingFlags,
                                null, null, new object[] { IntPtr.Zero });
                        }
                    }

                    if (resetInput)
                    {
                        type.InvokeMember(
                            isDotNetCore ? "s_in" : "_in", bindingFlags,
                            null, null, new object[] { null });
                    }

                    if (resetOutput)
                    {
                        type.InvokeMember(
                            isDotNetCore ? "s_out" : "_out", bindingFlags,
                            null, null, new object[] { null });
                    }

                    if (resetError)
                    {
                        type.InvokeMember(
                            isDotNetCore ? "s_error" : "_error", bindingFlags,
                            null, null, new object[] { null });
                    }

                    if (isDotNetCore)
                    {
                        if (resetOutput)
                        {
                            type.InvokeMember(
                                "s_isOutTextWriterRedirected", bindingFlags,
                                null, null, new object[] { false });
                        }

                        if (resetError)
                        {
                            type.InvokeMember(
                                "s_isErrorTextWriterRedirected", bindingFlags,
                                null, null, new object[] { false });
                        }

                        if (resetInput)
                        {
                            type.InvokeMember(
                                "_isStdInRedirected", bindingFlags,
                                null, null, new object[] { null });
                        }

                        if (resetOutput)
                        {
                            type.InvokeMember(
                                "_isStdOutRedirected", bindingFlags,
                                null, null, new object[] { null });
                        }

                        if (resetError)
                        {
                            type.InvokeMember(
                                "_isStdErrRedirected", bindingFlags,
                                null, null, new object[] { null });
                        }
                    }

#if NET_40
                    if (!isDotNetCore)
                    {
#if !NET_STANDARD_20
                        if (CommonOps.Runtime.IsFramework45OrHigher())
#endif
                        {
                            if (resetInput)
                            {
                                type.InvokeMember(
                                    "_stdInRedirectQueried", bindingFlags,
                                    null, null, new object[] { false });
                            }

                            if (resetOutput)
                            {
                                type.InvokeMember(
                                    "_stdOutRedirectQueried", bindingFlags,
                                    null, null, new object[] { false });
                            }

                            if (resetError)
                            {
                                type.InvokeMember(
                                    "_stdErrRedirectQueried", bindingFlags,
                                    null, null, new object[] { false });
                            }
                        }
                    }
#endif

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
#endif
            {
                //
                // NOTE: This is not supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used by the Interpreter.PreSetup method when
        //       it is processing the FixConsole host creation flag.
        //
        public static ReturnCode ResetInputBufferSize(
            ref Result error
            )
        {
            return ResetInputBufferSize(ConsoleBufferSize, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: See above.
        //
        private static ReturnCode ResetInputBufferSize(
            int bufferSize,
            ref Result error
            )
        {
#if !MONO
            if (!isMono)
            {
                if (StreamReaderType == null)
                {
                    error = "invalid stream reader type";
                    return ReturnCode.Error;
                }

                try
                {
                    FieldInfo fieldInfo = StreamReaderType.GetField(
                        "_closable", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateInstanceGetField, true));

                    if (fieldInfo == null)
                    {
                        error = String.Format(
                            "missing \"{0}{1}_closable\" field",
                            FormatOps.RawTypeName(StreamReaderType),
                            Type.Delimiter);

                        return ReturnCode.Error;
                    }

                    Stream stream = Console.OpenStandardInput(bufferSize);

                    StreamReader streamReader = new StreamReader(
                        stream, Console.InputEncoding, false, bufferSize);

                    fieldInfo.SetValue(streamReader, false); /* throw */
                    Console.SetIn(streamReader);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
            {
                //
                // NOTE: This is not supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
#else
            return ReturnCode.Ok;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IHost.Discard method of
        //       the console host.
        //
        public static ReturnCode ResetCachedInputRecord(
            ref Result error
            )
        {
#if !MONO
            if (!isMono && isWindows)
            {
                Type type = GetType(true, ref error);

                if (type == null)
                    return ReturnCode.Error;

                try
                {
                    object cachedInputRecord = type.InvokeMember(
                        "_cachedInputRecord", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticGetField, true),
                        null, null, null);

                    if (cachedInputRecord != null)
                    {
                        Marshal.WriteInt16(cachedInputRecord, 0, 0);

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "invalid internal {0} cached input record",
                            FormatOps.TypeName(type));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
#endif
            {
                //
                // NOTE: This is not supported (or necessary) on Mono
                //       -OR- Unix; therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IHost.GetMode,
        //       IHost.SetMode, and IHost.Discard methods of the console host
        //       in addition to the subsystem that detects redirection of the
        //       console channels.
        //
        public static IntPtr GetInputHandle(
            ref Result error
            )
        {
            if (!isDotNetCore || isWindows)
            {
                Type type = GetType(true, ref error);

                if (type == null)
                    return IntPtr.Zero;

                //
                // HACK: Because the System.Console object in the .NET Framework
                //       provides no means to query the underlying input/output
                //       handles, we must do it here by force.
                //
                try
                {
                    string propertyName;

                    if (isDotNetCore)
                        propertyName = "InputHandle";
                    else if (isMono)
                        propertyName = "ConsoleInput";
                    else
                        propertyName = "ConsoleInputHandle";

                    IntPtr handle = (IntPtr)type.InvokeMember(
                        propertyName, isMono ? ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PublicStaticGetProperty, true) :
                        ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateStaticGetProperty, true),
                        null, null, null);

                    if (!RuntimeOps.IsValidHandle(handle))
                    {
                        error = String.Format(
                            "invalid internal {0} input handle",
                            FormatOps.TypeName(type));
                    }

                    return handle;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not implemented";
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IHost.GetMode,
        //       IHost.SetMode, and IHost.Discard methods of the console host
        //       in addition to the subsystem that detects redirection of the
        //       console channels.
        //
        public static IntPtr GetOutputHandle(
            ref Result error
            )
        {
            if (!isDotNetCore || isWindows)
            {
                Type type = GetType(true, ref error);

                if (type == null)
                    return IntPtr.Zero;

                //
                // HACK: Because the System.Console object in the .NET Framework
                //       provides no means to query the underlying input/output
                //       handles, we must do it here by force.
                //
                try
                {
                    string propertyName;

                    if (isDotNetCore)
                        propertyName = "OutputHandle";
                    else if (isMono)
                        propertyName = "ConsoleOutput";
                    else
                        propertyName = "ConsoleOutputHandle";

                    IntPtr handle = (IntPtr)type.InvokeMember(
                        propertyName, isMono ? ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PublicStaticGetProperty, true) :
                        ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateStaticGetProperty, true),
                        null, null, null);

                    if (!RuntimeOps.IsValidHandle(handle))
                    {
                        error = String.Format(
                            "invalid internal {0} output handle",
                            FormatOps.TypeName(type));
                    }

                    return handle;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not implemented";
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IStreamHost.In property
        //       of the console host.
        //
        public static ReturnCode GetInputStream(
            ref Stream stream,
            ref Result error
            )
        {
            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       streams, we must do it here by force.
            //
            try
            {
                TextReader textReader = System.Console.In; /* throw */

                if (textReader == null)
                {
                    error = "invalid system console input text reader";
                    return ReturnCode.Error;
                }

                Type type = textReader.GetType();

                if (type == null)
                {
                    error = String.Format(
                        "invalid {0} input text reader type",
                        FormatOps.TypeName(type));

                    return ReturnCode.Error;
                }

                StreamReader streamReader = type.InvokeMember(
                    isMono ? "reader" : "_in", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateInstanceGetField, true),
                    null, textReader, null) as StreamReader; /* throw */

                if (streamReader != null)
                {
                    stream = streamReader.BaseStream; /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "invalid {0} input stream reader",
                        FormatOps.TypeName(type));
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IStreamHost.Out property
        //       of the console host.
        //
        public static ReturnCode GetOutputStream(
            ref Stream stream,
            ref Result error
            )
        {
            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       streams, we must do it here by force.
            //
            try
            {
                TextWriter textWriter = System.Console.Out; /* throw */

                if (textWriter == null)
                {
                    error = "invalid system console output text writer";
                    return ReturnCode.Error;
                }

                Type type = textWriter.GetType();

                if (type == null)
                {
                    error = String.Format(
                        "invalid {0} output text writer type",
                        FormatOps.TypeName(type));

                    return ReturnCode.Error;
                }

                StreamWriter streamWriter = type.InvokeMember(
                    isMono ? "writer" : "_out", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateInstanceGetField, true),
                    null, textWriter, null) as StreamWriter; /* throw */

                if (streamWriter != null)
                {
                    stream = streamWriter.BaseStream; /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "invalid {0} output stream writer",
                        FormatOps.TypeName(type));
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IStreamHost.Error property
        //       of the console host.
        //
        public static ReturnCode GetErrorStream(
            ref Stream stream,
            ref Result error
            )
        {
            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       streams, we must do it here by force.
            //
            try
            {
                TextWriter textWriter = System.Console.Error; /* throw */

                if (textWriter == null)
                {
                    error = "invalid system console error text writer";
                    return ReturnCode.Error;
                }

                Type type = textWriter.GetType();

                if (type == null)
                {
                    error = String.Format(
                        "invalid {0} error text writer type",
                        FormatOps.TypeName(type));

                    return ReturnCode.Error;
                }

                StreamWriter streamWriter = type.InvokeMember(
                    isMono ? "writer" : "_out", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateInstanceGetField, true),
                    null, textWriter, null) as StreamWriter; /* throw */

                if (streamWriter != null)
                {
                    stream = streamWriter.BaseStream; /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "invalid {0} error stream writer",
                        FormatOps.TypeName(type));
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IHost.Close method of the
        //       console host.
        //
        public static ReturnCode UnhookControlHandler(
            bool strict,
            ref Result error
            )
        {
#if !MONO
            if (!isMono)
            {
                Type type = GetType(false, ref error);

                if (type == null)
                    return ReturnCode.Error;

                //
                // HACK: Because the System.Console object in the .NET Framework
                //       provides no means to unhook it from its native console
                //       callbacks, we must do it here by force.
                //
                try
                {
                    string fieldName;

                    if (isDotNetCore)
                        fieldName = "s_registrar";
                    else
                        fieldName = "_hooker";

                    BindingFlags bindingFlags; /* REUSED */

                    bindingFlags = ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticGetField, true);

                    //
                    // NOTE: First, attempt to grab the private static
                    //       ControlCHooker or ControlCHandlerRegistrar
                    //       field from the static System.Console object.
                    //
                    object hook = null;

                    try
                    {
                        hook = type.InvokeMember(
                            fieldName, bindingFlags, null, null, null);
                    }
                    catch
                    {
                        // do nothing.
                    }

                    if ((hook == null) && isDotNetCore)
                    {
                        //
                        // NOTE: This is the older field name for .NET Core.
                        //       It was in use as of .NET Core 2.0.6 RTM.
                        //
                        fieldName = "_registrar";

                        try
                        {
                            hook = type.InvokeMember(
                                fieldName, bindingFlags, null, null, null);
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }

                    if (hook != null)
                    {
                        //
                        // NOTE: Next, grab and validate the type for the
                        //       ControlCHooker field.
                        //
                        Type hookType = hook.GetType();

                        if (hookType == null)
                        {
                            error = String.Format(
                                "invalid internal {0} hook type",
                                FormatOps.TypeName(type));

                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: Next, call the Unhook method of the returned
                        //       ControlCHooker object so that it will unhook
                        //       itself from its native callbacks.
                        //
                        bindingFlags = ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateInstanceMethod, true);

                        hookType.InvokeMember(
                            isDotNetCore ? "Unregister" : "Unhook",
                            bindingFlags, null, hook, null);

                        //
                        // NOTE: Finally, null out the private static (cached)
                        //       ControlCHooker field inside the System.Console
                        //       object so that it will know when it needs to
                        //       be re-hooked later.
                        //
                        bindingFlags = ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateStaticSetField, true);

                        type.InvokeMember(
                            fieldName, bindingFlags, null, null,
                            new object[] { null });

                        return ReturnCode.Ok;
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            "invalid internal \"{0}{1}{2}\" hook instance",
                            FormatOps.RawTypeName(type), Type.Delimiter,
                            fieldName);
                    }
                    else
                    {
                        //
                        // NOTE: There is no console hook present.
                        //
                        return ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
#endif
            {
                //
                // NOTE: This is not supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
        }
        #endregion
    }
}
