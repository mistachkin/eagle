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
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private, internal helper methods used to
    /// support the console host, including per-process console setup
    /// reference counting, console configuration, console output, and the
    /// low-level (reflection-based) manipulation of the underlying
    /// System.Console state across the various supported runtimes (the .NET
    /// Framework, .NET Core, and Mono).
    /// </summary>
    [ObjectId("69b75e27-9fd6-4cbe-844d-9c00002d0088")]
    internal static class ConsoleOps
    {
        #region Private Constants
        //
        // NOTE: Determine if we are running on Mono or .NET Core.  Cache the
        //       results of these checks for later use.
        //
        /// <summary>
        /// Non-zero if the current runtime is .NET Core, as determined once
        /// and cached for later use.
        /// </summary>
        private static readonly bool isDotNetCore =
            CommonOps.Runtime.IsDotNetCore();

        /// <summary>
        /// Non-zero if the current runtime is Mono, as determined once and
        /// cached for later use.
        /// </summary>
        private static readonly bool isMono =
            CommonOps.Runtime.IsMono();

        /// <summary>
        /// Non-zero if the current operating system is Windows, as determined
        /// once and cached for later use.
        /// </summary>
        private static readonly bool isWindows =
            PlatformOps.IsWindowsOperatingSystem();

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        //
        // NOTE: When this is set to non-zero, the native Win32 API will
        //       be used to write to the console; otherwise, the managed
        //       System.Console class will be used.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the native Win32 API is used to write to the
        /// console; otherwise, the managed System.Console class is used.
        /// </summary>
        private static bool useNativeConsole = false;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The Type object for the internal System.ConsolePal type.
        //       This is used, via reflection, by various methods of this
        //       class.
        //
        /// <summary>
        /// The Type object for the internal System.ConsolePal type.  This is
        /// used, via reflection, by various methods of this class.
        /// </summary>
        private static readonly Type ConsolePalType = isDotNetCore ?
            Type.GetType("System.ConsolePal") : null;

        //
        // NOTE: The Type object for the public System.IO.MonoIO type.  This
        //       is used, via reflection, by various methods of this class.
        //
        /// <summary>
        /// The Type object for the public System.IO.MonoIO type.  This is
        /// used, via reflection, by various methods of this class.
        /// </summary>
        private static readonly Type MonoIoType = isMono ?
            Type.GetType("System.IO.MonoIO") : null;

        //
        // NOTE: The type for the System.Console class.  This is used, via
        //       reflection, by various methods of this class.
        //
        /// <summary>
        /// The Type object for the System.Console class.  This is used, via
        /// reflection, by various methods of this class.
        /// </summary>
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
        /// <summary>
        /// The console buffer size used when increasing from the .NET
        /// Framework default of 256 bytes.
        /// </summary>
        private static int ConsoleBufferSize = 16384;

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        //
        // NOTE: The type for the System.IO.StreamReader class.  This is used,
        //       via reflection, by various methods of this class.
        //
        /// <summary>
        /// The Type object for the System.IO.StreamReader class.  This is
        /// used, via reflection, by various methods of this class.
        /// </summary>
        private static readonly Type StreamReaderType = typeof(StreamReader);
#endif

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        //
        // NOTE: The Type object for the private System.ConsoleDriver type.
        //       This is used to obtain the System.TermInfoDriver instance.
        //
        /// <summary>
        /// The Type object for the private System.ConsoleDriver type.  This is
        /// used to obtain the System.TermInfoDriver instance.
        /// </summary>
        private static readonly Type MonoConsoleDriverType = isMono ?
            Type.GetType("System.ConsoleDriver") : null;

        //
        // NOTE: The Type object for the private System.TermInfoDriver type.
        //       This is used to add fake input into the buffer, which can
        //       then be used to cause the Console.ReadLine to return null.
        //
        /// <summary>
        /// The Type object for the private System.TermInfoDriver type.  This
        /// is used to add fake input into the buffer, which can then be used
        /// to cause the Console.ReadLine to return null.
        /// </summary>
        private static readonly Type MonoTermInfoDriverType = isMono ?
            Type.GetType("System.TermInfoDriver") : null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods (Shared)
        #region Per-Process Console (Setup) Reference Count Support
        /// <summary>
        /// This method queries the per-process environment variable used to
        /// track the console setup reference count for the specified process.
        /// </summary>
        /// <param name="processId">
        /// The identifier of the process whose console reference count
        /// environment variable is to be queried.
        /// </param>
        /// <returns>
        /// The value of the environment variable, or null if it does not
        /// exist.
        /// </returns>
        public static string GetEnvironmentVariable(
            long processId /* in */
            )
        {
            return ProcessOps.GetEnvironmentVariable(
                EnvVars.EagleLibraryHostsConsole, processId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries and, optionally, modifies the per-process
        /// console setup reference count.
        /// </summary>
        /// <param name="increment">
        /// Non-zero to increment the reference count, zero to decrement it, or
        /// null to query it without modification.  This parameter is optional.
        /// </param>
        /// <param name="referenceCount">
        /// Upon return, this contains the resulting console setup reference
        /// count.
        /// </param>
        /// <returns>
        /// True if the reference count was successfully queried (and modified,
        /// if requested); otherwise, false.
        /// </returns>
        private static bool CheckAndMaybeModifyReferenceCount(
            bool? increment,        /* in: OPTIONAL */
            out long referenceCount /* out */
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

        /// <summary>
        /// This method determines whether the per-process console has been
        /// set up at least once.
        /// </summary>
        /// <returns>
        /// True if the console setup reference count is greater than zero;
        /// otherwise, false.
        /// </returns>
        public static bool IsSetup()
        {
            long referenceCount;

            return CheckAndMaybeModifyReferenceCount(
                null, out referenceCount) && (referenceCount > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the per-process console is shared
        /// among more than one consumer.
        /// </summary>
        /// <returns>
        /// True if the console setup reference count is greater than one;
        /// otherwise, false.
        /// </returns>
        public static bool IsShared()
        {
            long referenceCount;

            return CheckAndMaybeModifyReferenceCount(
                null, out referenceCount) && (referenceCount > 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments or decrements the per-process console setup
        /// reference count and reports whether the transition to (or from) the
        /// set up state occurred.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to increment the reference count (marking the console as
        /// set up); zero to decrement it (marking the console as no longer set
        /// up).
        /// </param>
        /// <returns>
        /// When marking setup, true if this was the first reference (the count
        /// became one); when un-marking setup, true if this was the last
        /// reference (the count became zero or less); otherwise, false.
        /// </returns>
        public static bool MarkSetup(
            bool setup /* in */
            )
        {
            long referenceCount;

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
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method enables the console flag when it is not already enabled
        /// and the associated environment variable is present in the global
        /// configuration.
        /// </summary>
        /// <param name="console">
        /// On entry, the current value of the console flag.  Upon return, this
        /// is set to non-zero when the console environment variable is present;
        /// otherwise, it is left unchanged.
        /// </param>
        public static void MaybeEnableConsole( /* NOT USED */
            ref bool console /* in, out */
            )
        {
            if (!console && GlobalConfiguration.DoesValueExist(
                    EnvVars.Console, ConfigurationFlags.ConsoleOps))
            {
                console = true;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disables the console flag when the associated "no
        /// console" configuration value is present.
        /// </summary>
        /// <param name="console">
        /// On input, the current console flag; upon return, this is set to
        /// zero when the "no console" configuration value exists.
        /// </param>
        public static void MaybeDisableConsole(
            ref bool console /* in, out */
            )
        {
            if (console && GlobalConfiguration.DoesValueExist(
                    EnvVars.NoConsole, ConfigurationFlags.ConsoleOps))
            {
                console = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method enables the verbose flag when it is not already enabled
        /// and the associated environment variable is present in the global
        /// configuration.
        /// </summary>
        /// <param name="verbose">
        /// On entry, the current value of the verbose flag.  Upon return, this
        /// is set to non-zero when the verbose environment variable is present;
        /// otherwise, it is left unchanged.
        /// </param>
        public static void MaybeEnableVerbose( /* NOT USED */
            ref bool verbose /* in, out */
            )
        {
            if (!verbose && GlobalConfiguration.DoesValueExist(
                    EnvVars.Verbose, ConfigurationFlags.ConsoleOps))
            {
                verbose = true;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disables the verbose flag when the associated "no
        /// verbose" configuration value is present.
        /// </summary>
        /// <param name="verbose">
        /// On input, the current verbose flag; upon return, this is set to
        /// zero when the "no verbose" configuration value exists.
        /// </param>
        public static void MaybeDisableVerbose(
            ref bool verbose /* in, out */
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
#if NATIVE && WINDOWS
        /// <summary>
        /// This method determines whether the native Win32 API should be used
        /// to write to the console.
        /// </summary>
        /// <returns>
        /// True if the native Win32 API should be used; otherwise, false.
        /// </returns>
        public static bool ShouldUseNative()
        {
            return useNativeConsole;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets whether the native Win32 API should be used to
        /// write to the console.
        /// </summary>
        /// <param name="useNative">
        /// Non-zero to use the native Win32 API to write to the console; zero
        /// to use the managed System.Console class.
        /// </param>
        public static void SetUseNative(
            bool useNative /* in */
            )
        {
            useNativeConsole = useNative;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a value to the console using the native Win32
        /// API, optionally followed by a line terminator.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to be written to the console.
        /// </typeparam>
        /// <param name="value">
        /// The value to be written to the console.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the value; otherwise,
        /// zero.
        /// </param>
        public static void WriteNative<T>(
            T value,     /* in */
            bool newLine /* in */
            )
        {
            Result error = null;

            if (NativeConsole.WriteString<T>(
                    value, newLine, ref error) != ReturnCode.Ok)
            {
                throw new Exception(error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line terminator to the console using the
        /// native Win32 API.
        /// </summary>
        public static void WriteNativeLine()
        {
            Result error = null;

            if (NativeConsole.WriteLine(ref error) != ReturnCode.Ok)
                throw new Exception(error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line of text to the console using a high
        /// contrast foreground color, restoring the previous foreground color
        /// afterward.  Any failure is allowed to propagate to the caller.
        /// </summary>
        /// <param name="value">
        /// The text to be written to the console.
        /// </param>
        public static void WriteCore(
            string value /* in */
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
#if NATIVE && WINDOWS
                if (ShouldUseNative())
                {
                    WriteNative(value, true); /* throw */
                    return;
                }
#endif

                Console.WriteLine(value); /* throw */
            }
            finally
            {
                Console.ForegroundColor = savedForegroundColor; /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line of text to the console, catching and
        /// tracing any exception so that no failure is propagated to the
        /// caller.
        /// </summary>
        /// <param name="value">
        /// The text to be written to the console.
        /// </param>
        private static void WriteCoreNoThrow(
            string value /* in */
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

        //
        // NOTE: Stores the most recently written prompt
        //       text for use by LineEditor (readline)
        //       which needs the prompt string to properly
        //       redraw on history navigation.
        //
        /// <summary>
        /// Stores the most recently written prompt text for use by LineEditor
        /// (readline), which needs the prompt string to properly redraw on
        /// history navigation.
        /// </summary>
        private static string lastPrompt = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the most recently written prompt text.
        /// </summary>
        public static string LastPrompt
        {
            get { return lastPrompt; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the specified prompt text as the most recently
        /// written prompt and then writes it to the console.
        /// </summary>
        /// <param name="value">
        /// The prompt text to be recorded and written to the console.
        /// </param>
        public static void WritePrompt(
            string value /* in */
            )
        {
            lastPrompt = value;
            WriteCoreNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified prompt text to the console,
        /// subject to the relevant configuration values, using the default
        /// console and verbose flags.
        /// </summary>
        /// <param name="value">
        /// The prompt text to be written to the console.
        /// </param>
        public static void MaybeWritePrompt(
            string value /* in */
            )
        {
            MaybeWritePrompt(value, true, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified prompt text to the console,
        /// subject to the "no write prompt" configuration value and the
        /// effective console and verbose flags.
        /// </summary>
        /// <param name="value">
        /// The prompt text to be written to the console.
        /// </param>
        /// <param name="console">
        /// Non-zero to permit writing to the console; this may be further
        /// disabled by the "no console" configuration value.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to permit verbose output; this may be further disabled by
        /// the "no verbose" configuration value.
        /// </param>
        public static void MaybeWritePrompt(
            string value, /* in */
            bool console, /* in */
            bool verbose  /* in */
            )
        {
            if (GlobalConfiguration.DoesValueExist(
                    EnvVars.NoWritePrompt, GlobalConfiguration.GetFlags(
                    ConfigurationFlags.ConsoleOps, verbose)))
            {
                return;
            }

            MaybeDisableConsole(ref console);
            MaybeDisableVerbose(ref verbose);

            if (console && verbose)
                WritePrompt(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified error text to the console without
        /// propagating any failure to the caller.
        /// </summary>
        /// <param name="value">
        /// The error text to be written to the console.
        /// </param>
        private static void WriteError(
            string value /* in */
            )
        {
            WriteCoreNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified error text to the console, subject
        /// to the effective console flag.
        /// </summary>
        /// <param name="value">
        /// The error text to be written to the console.
        /// </param>
        /// <param name="console">
        /// Non-zero to permit writing to the console; this may be further
        /// disabled by the "no console" configuration value.
        /// </param>
        public static void MaybeWriteError(
            string value, /* in */
            bool console  /* in */
            )
        {
            MaybeDisableConsole(ref console);

            if (console)
                WriteError(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified complaint text to the console
        /// without propagating any failure to the caller.
        /// </summary>
        /// <param name="value">
        /// The complaint text to be written to the console.
        /// </param>
        public static void WriteComplaint(
            string value /* in */
            )
        {
            WriteCoreNoThrow(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Console Debugging Support
        /// <summary>
        /// This method displays the debugger prompt on the console and then
        /// waits for the interactive user to press any key.
        /// </summary>
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

        /// <summary>
        /// This method returns the Type object for the system console,
        /// selecting the appropriate private or public type based on the
        /// current runtime.
        /// </summary>
        /// <param name="private">
        /// Non-zero to return the runtime-specific private console type (the
        /// .NET Core System.ConsolePal type or the Mono System.IO.MonoIO
        /// type); zero to return the public System.Console type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The selected console Type object, or null if it could not be
        /// resolved.
        /// </returns>
        private static Type GetType(
            bool @private,   /* in */
            ref Result error /* out */
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

        #region System.Console Support Methods (Mono 2.0 - 6.12)
#if UNIX
        /// <summary>
        /// This method simulates an end-of-transmission on Unix by writing a
        /// newline to "/dev/tty", which breaks out of a blocking
        /// Console.ReadLine or readline call.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode PrivateSimulateEndOfTransmission(
            ref Result error /* out */
            )
        {
            try
            {
                //
                // NOTE: On .NET Core on Unix, write newline to
                //       "/dev/tty" to break out of the blocking
                //       Console.ReadLine or readline call.  This
                //       is the Unix equivalent of the Windows
                //       SimulateReturnKey approach.
                //
                using (StreamWriter writer = new StreamWriter(
                        new FileStream("/dev/tty", FileMode.Open,
                        FileAccess.Write)))
                {
                    writer.WriteLine();
                    writer.Flush();
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ConsoleOps).Name,
                    TracePriority.ConsoleError);

                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to support the IDebugHost.Cancel method
        //       when running on Unix (Mono).
        //
        /// <summary>
        /// This method simulates an end-of-transmission in order to support
        /// the IDebugHost.Cancel method when running on Unix (Mono).
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SimulateEndOfTransmission(
            ref Result error /* out */
            )
        {
            if (!isMono)
            {
                if (!PlatformOps.IsWindowsOperatingSystem())
                    return PrivateSimulateEndOfTransmission(ref error);

                //
                // NOTE: On Windows (non-Mono), this will be handled by
                //       SimulateReturnKey via PostMessage; therefore,
                //       just fake success.
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
        /// <summary>
        /// This method forcibly resets the underlying input, output, and/or
        /// error streams of the system console.  It is used to support the
        /// [host open], [host close], and [host screen] sub-commands as well
        /// as the IDebugHost.Exit method.
        /// </summary>
        /// <param name="channelType">
        /// The standard console channels (input, output, and/or error) to be
        /// reset.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ResetStreams(
            ChannelType channelType, /* in */
            ref Result error         /* out */
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
        /// <summary>
        /// This method resets the console input buffer size to the default
        /// console buffer size.  It is used by the Interpreter.PreSetup method
        /// when it is processing the FixConsole host creation flag.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ResetInputBufferSize(
            ref Result error /* out */
            )
        {
            return ResetInputBufferSize(ConsoleBufferSize, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: See above.
        //
        /// <summary>
        /// This method resets the console input buffer size to the specified
        /// number of bytes by forcibly replacing the underlying console input
        /// stream reader.
        /// </summary>
        /// <param name="bufferSize">
        /// The desired console input buffer size, in bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ResetInputBufferSize(
            int bufferSize,  /* in */
            ref Result error /* out */
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

                    bool success = false;
                    Stream stream = null;
                    StreamReader streamReader = null;

                    try
                    {
                        stream = Console.OpenStandardInput(bufferSize);

                        streamReader = new StreamReader(
                            stream, Console.InputEncoding, false, bufferSize);

                        fieldInfo.SetValue(streamReader, false); /* throw */
                        Console.SetIn(streamReader);

                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            if (streamReader != null)
                            {
                                streamReader.Close();
                                streamReader = null;
                            }

                            if (stream != null)
                            {
                                stream.Close();
                                stream = null;
                            }
                        }
                    }

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
        /// <summary>
        /// This method resets the internal cached input record of the system
        /// console.  It is used to support the IHost.Discard method of the
        /// console host.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ResetCachedInputRecord(
            ref Result error /* out */
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
        /// <summary>
        /// This method retrieves the native input handle of the system
        /// console by force.  It is used to support the IHost.GetMode,
        /// IHost.SetMode, and IHost.Discard methods of the console host in
        /// addition to the subsystem that detects redirection of the console
        /// channels.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The native console input handle, or IntPtr.Zero on failure.
        /// </returns>
        public static IntPtr GetInputHandle(
            ref Result error /* out */
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
        /// <summary>
        /// This method retrieves the native output handle of the system
        /// console by force.  It is used to support the IHost.GetMode,
        /// IHost.SetMode, and IHost.Discard methods of the console host in
        /// addition to the subsystem that detects redirection of the console
        /// channels.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The native console output handle, or IntPtr.Zero on failure.
        /// </returns>
        public static IntPtr GetOutputHandle(
            ref Result error /* out */
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
        /// <summary>
        /// This method retrieves the underlying input stream of the system
        /// console by force.  It is used to support the IStreamHost.In
        /// property of the console host.
        /// </summary>
        /// <param name="stream">
        /// Upon success, this contains the underlying console input stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetInputStream(
            ref Stream stream, /* out */
            ref Result error   /* out */
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
        /// <summary>
        /// This method retrieves the underlying output stream of the system
        /// console by force.  It is used to support the IStreamHost.Out
        /// property of the console host.
        /// </summary>
        /// <param name="stream">
        /// Upon success, this contains the underlying console output stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetOutputStream(
            ref Stream stream, /* out */
            ref Result error   /* out */
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
        /// <summary>
        /// This method retrieves the underlying error stream of the system
        /// console by force.  It is used to support the IStreamHost.Error
        /// property of the console host.
        /// </summary>
        /// <param name="stream">
        /// Upon success, this contains the underlying console error stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetErrorStream(
            ref Stream stream, /* out */
            ref Result error   /* out */
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
        // NOTE: This method is used to support the IHost.Close method
        //       of the console host.
        //
        /// <summary>
        /// This method forcibly unhooks the system console from its native
        /// control handler (Ctrl-C) callbacks.  It is used to support the
        /// IHost.Close method of the console host.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to treat the absence of any console hook as an error; zero
        /// to treat it as success.
        /// </param>
        /// <param name="list">
        /// When supplied, this receives diagnostic name/value pairs describing
        /// the hook types, field names, and method names that were used.  This
        /// parameter is optional.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode UnhookControlHandler(
            bool strict,     /* in */
            StringList list, /* in: OPTIONAL */
            ref Result error /* out */
            )
        {
#if !MONO
            if (!isMono)
            {
                Type type = GetType(false, ref error);

                if (type == null)
                    return ReturnCode.Error;

                //
                // HACK: Because the System.Console object in the .NET
                //       Framework provides no means to unhook it from
                //       its native console callbacks, we must do it
                //       here by force.
                //
                try
                {
                    string fieldName1;
                    string fieldName2;

                    if (isDotNetCore)
                    {
                        //
                        // HACK: These are current as of .NET 6.
                        //
                        fieldName1 = "s_sigIntRegistration";
                        fieldName2 = "s_sigQuitRegistration";
                    }
                    else
                    {
                        fieldName1 = "_hooker";
                        fieldName2 = null;
                    }

                    BindingFlags bindingFlags; /* REUSED */

                    bindingFlags = ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticGetField, true);

                    //
                    // NOTE: First, attempt to grab the private static
                    //       ControlCHooker or ControlCHandlerRegistrar
                    //       field from static System.Console object.
                    //
                    object hook1 = null;

                    if (fieldName1 != null)
                    {
                        try
                        {
                            hook1 = type.InvokeMember(
                                fieldName1, bindingFlags, null, null,
                                null);
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }

                    object hook2 = null;

                    if (fieldName2 != null)
                    {
                        try
                        {
                            hook2 = type.InvokeMember(
                                fieldName2, bindingFlags, null, null,
                                null);
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }

                    if ((hook1 == null) && isDotNetCore)
                    {
                        //
                        // NOTE: This is the older field name for .NET
                        //       Core.  It was in use as of .NET 5 RTM.
                        //
                        fieldName1 = "s_registrar";
                        fieldName2 = null;

                        try
                        {
                            hook1 = type.InvokeMember(
                                fieldName1, bindingFlags, null, null,
                                null);
                        }
                        catch
                        {
                            // do nothing.
                        }

                        hook2 = null;
                    }

                    if ((hook1 == null) && isDotNetCore)
                    {
                        //
                        // NOTE: This is the older field name for .NET
                        //       Core.  It was in use as of .NET Core
                        //       2.0.6 RTM.
                        //
                        fieldName1 = "_registrar";
                        fieldName2 = null;

                        try
                        {
                            hook1 = type.InvokeMember(
                                fieldName1, bindingFlags, null, null,
                                null);
                        }
                        catch
                        {
                            // do nothing.
                        }

                        hook2 = null;
                    }

                    if ((hook1 != null) || (hook2 != null))
                    {
                        //
                        // NOTE: Next, grab and validate types for
                        //       the ControlCHooker fields.
                        //
                        Type hook1Type = null;

                        if (hook1 != null)
                        {
                            hook1Type = hook1.GetType();

                            if (hook1Type == null)
                            {
                                error = String.Format(
                                    "invalid internal {0} hook #1 type",
                                    FormatOps.TypeName(type));

                                return ReturnCode.Error;
                            }
                        }

                        Type hook2Type = null;

                        if (hook2 != null)
                        {
                            hook2Type = hook2.GetType();

                            if (hook2Type == null)
                            {
                                error = String.Format(
                                    "invalid internal {0} hook #2 type",
                                    FormatOps.TypeName(type));

                                return ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Next, call the appropriate method of
                        //       the returned object so that it will
                        //       unhook itself from native callbacks.
                        //
                        string methodName1 = isDotNetCore ?
                            "Unregister" : "Unhook";

                        string methodName2 = methodName1;

                        bindingFlags = ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateInstanceMethod,
                            true);

                        if ((hook1Type != null) && (hook1 != null))
                        {
                            hook1Type.InvokeMember(
                                methodName1, bindingFlags, null, hook1,
                                null);

                            if (list != null)
                            {
                                list.Add("hook1Type");
                                list.Add(FormatOps.RawTypeName(hook1Type));
                                list.Add("methodName1");
                                list.Add(methodName1);
                            }
                        }

                        if ((hook2Type != null) && (hook2 != null))
                        {
                            hook2Type.InvokeMember(
                                methodName2, bindingFlags, null, hook2,
                                null);

                            if (list != null)
                            {
                                list.Add("hook2Type");
                                list.Add(FormatOps.RawTypeName(hook2Type));
                                list.Add("methodName2");
                                list.Add(methodName2);
                            }
                        }

                        //
                        // NOTE: Finally, null out the private static
                        //       (cached) ControlCHooker field inside
                        //       the System.Console object so that it
                        //       will know to re-hook later.
                        //
                        bindingFlags = ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateStaticSetField,
                            true);

                        if (fieldName1 != null)
                        {
                            type.InvokeMember(
                                fieldName1, bindingFlags, null, null,
                                new object[] { null });

                            if (list != null)
                            {
                                list.Add("fieldName1");
                                list.Add(fieldName1);
                            }
                        }

                        if (fieldName2 != null)
                        {
                            type.InvokeMember(
                                fieldName2, bindingFlags, null, null,
                                new object[] { null });

                            if (list != null)
                            {
                                list.Add("fieldName2");
                                list.Add(fieldName2);
                            }
                        }

                        return ReturnCode.Ok;
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            "missing internal {0} hook instance(s): " +
                            "{1} and/or {2} using binding flags {3}",
                            FormatOps.RawTypeName(type),
                            FormatOps.MaybeNull(fieldName1),
                            FormatOps.MaybeNull(fieldName2),
                            FormatOps.WrapOrNull(bindingFlags));
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
