/*
 * HostOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using ResourcePair = Eagle._Components.Public.AnyPair<
    string, System.Resources.ResourceManager>;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of private static helper methods used
    /// to interact with interpreter hosts, including locating the host for an
    /// interpreter, building and emitting interactive prompts, sleeping and
    /// yielding the current thread, querying and combining host and detail
    /// flags, reading scripts and streams via the host, and creating, wrapping,
    /// and disposing of host instances.
    /// </summary>
    [ObjectId("1b0d1e7d-957b-4151-b31f-598393251442")]
    internal static class HostOps
    {
        #region Private Constants
        #region Interactive Prompt Defaults
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The minimum number of active interactive loops required before the
        /// active loops prefix is added to the interactive prompt.
        /// </summary>
        private static int MinimumPrefixLoops = 1;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default text used for the primary (top-level) interactive
        /// prompt.
        /// </summary>
        private const string PrimaryPrompt = "% ";

        /// <summary>
        /// The default text used for the continuation interactive prompt, shown
        /// when more input is needed to complete a command.
        /// </summary>
        private const string ContinuePrompt = ">\t";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the active interactive loops prefix
        /// for the interactive prompt.
        /// </summary>
        private const string LoopsPrefixFormat = "(a:{0}) ==> ";

        /// <summary>
        /// The format string used to build the command count prefix for the
        /// interactive prompt.
        /// </summary>
        private const string CountPrefixFormat = "[c:{0}] ";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The prefix added to the interactive prompt when interactive
        /// debugging is active.
        /// </summary>
        private const string DebugPrefix = "(debug) ";

        /// <summary>
        /// The prefix added to the interactive prompt when there are queued
        /// events.
        /// </summary>
        private const string QueuePrefix = "^ ";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of default prompt strings, indexed by the integer value of
        /// a <see cref="PromptType" />.
        /// </summary>
        private static readonly StringList DefaultPrompts = new StringList(
            new string[] { null, PrimaryPrompt, ContinuePrompt }
        );

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The format string used to build the interpreter identifier prefix
        /// for the interactive prompt.
        /// </summary>
        private static string IdPrefixFormat = "i:{0} ";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Host Timeouts
        //
        // HACK: This is not read-only.
        //
        /// <summary>
        /// The number of milliseconds to wait when attempting to acquire the
        /// interpreter lock in order to obtain its host.
        /// </summary>
        private static int GetTimeout = 2000; /* TODO: Good default? */

        //
        // HACK: This is not read-only.
        //
        /// <summary>
        /// The number of milliseconds to wait when attempting to acquire the
        /// interpreter lock in order to obtain its interactive host.
        /// </summary>
        private static int InteractiveGetTimeout = 2000; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Mode Formatting
        /// <summary>
        /// The format string used to display the current interactive mode.
        /// </summary>
        private const string InteractiveModeFormat = "- [{0}]";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Host Colors
        //
        // NOTE: These are used by the GetHighContrastColor method.  Normally,
        //       they are set to white (light) and black (dark); however, they
        //       can be overridden.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The console color used as the high-contrast color for dark colors;
        /// normally white.
        /// </summary>
        private static ConsoleColor highContrastLightColor = ConsoleColor.White;

        /// <summary>
        /// The console color used as the high-contrast color for light colors;
        /// normally black.
        /// </summary>
        private static ConsoleColor highContrastDarkColor = ConsoleColor.Black;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Console
#if CONSOLE && NATIVE && WINDOWS
        //
        // NOTE: This is the minimum size for the console history buffer.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The minimum size for the native console history buffer.
        /// </summary>
        private static uint MinimumHistoryBufferSize = 200;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this is set to non-zero, the native Win32 API will
        //       be used to write to the console; otherwise, the managed
        //       System.Console class will be used.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the native Win32 API is used to write to the console;
        /// otherwise, the managed System.Console class is used.
        /// </summary>
        private static bool useNativeConsole = false;
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The error message format used when the interpreter host lacks
        /// support for a required feature.
        /// </summary>
        public static readonly string NoFeatureError =
            "interpreter host lacks support for the \"{0}\" feature";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: The total number of outstanding calls into Thread.Sleep in
        //       this AppDomain.
        //
        /// <summary>
        /// The total number of outstanding calls into Thread.Sleep in this
        /// application domain.
        /// </summary>
        private static int pendingSleepCount;

        //
        // NOTE: The total number of outstanding calls into Thread.Yield in
        //       this AppDomain.
        //
        /// <summary>
        /// The total number of outstanding calls into Thread.Yield in this
        /// application domain.
        /// </summary>
        private static int pendingYieldCount;

        //
        // NOTE: The total number of milliseconds slept in this AppDomain.
        //
        /// <summary>
        /// The total number of milliseconds slept in this application domain.
        /// </summary>
        private static long totalSleepMilliseconds;

        //
        // NOTE: The total number of calls to yield in this AppDomain.
        //
        /// <summary>
        /// The total number of calls to yield in this application domain.
        /// </summary>
        private static long totalYieldCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds host-related introspection information to the
        /// specified list.
        /// </summary>
        /// <param name="list">
        /// The list that host information is added to.  This may be modified.
        /// </param>
        /// <param name="detailFlags">
        /// The detail flags that control which information is included.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();
            int intValue; /* REUSED */
            long longValue; /* REUSED */

            intValue = Interlocked.CompareExchange(
                ref pendingSleepCount, 0, 0);

            if (empty || (intValue != 0))
            {
                localList.Add("PendingSleepCount",
                    intValue.ToString());
            }

            intValue = Interlocked.CompareExchange(
                ref pendingYieldCount, 0, 0);

            if (empty || (intValue != 0))
            {
                localList.Add("PendingYieldCount",
                    intValue.ToString());
            }

            longValue = Interlocked.CompareExchange(
                ref totalSleepMilliseconds, 0, 0);

            if (empty || (longValue != 0))
            {
                localList.Add("TotalSleepMilliseconds",
                    longValue.ToString());
            }

            longValue = Interlocked.CompareExchange(
                ref totalYieldCount, 0, 0);

            if (empty || (longValue != 0))
            {
                localList.Add("TotalYieldCount",
                    longValue.ToString());
            }

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("Host Information");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Support Methods
        /// <summary>
        /// This method attempts to obtain the host associated with the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host is to be obtained.
        /// </param>
        /// <returns>
        /// The host associated with the interpreter, or null if it is not
        /// available.
        /// </returns>
        private static IHost TryGet(
            Interpreter interpreter /* in */
            )
        {
            IHost host = null;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (!locked)
                    {
                        TraceOps.LockTrace(
                            "TryGet(1)",
                            typeof(HostOps).Name, false,
                            TracePriority.LockWarning2,
                            interpreter.MaybeWhoHasLock());

                        int timeout = GetTimeout; /* NO-LOCK */

                        if (timeout >= 0)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryGet: retrying " +
                                "for {0} milliseconds...",
                                timeout), typeof(HostOps).Name,
                                TracePriority.HostDebug);

                            interpreter.InternalTryLock(
                                timeout, ref locked); /* TRANSACTIONAL */

                            if (!locked)
                            {
                                TraceOps.LockTrace(
                                    "TryGet(2)",
                                    typeof(HostOps).Name, false,
                                    TracePriority.LockError,
                                    interpreter.MaybeWhoHasLock());
                            }
                        }
                    }

                    if (locked)
                    {
                        //
                        // BUGFIX: Prevent a race condition between grabbing
                        //         the host and the interpreter being disposed.
                        //         This is necessary because we are called in
                        //         the critical code path of both the Wait and
                        //         WaitVariable methods.
                        //
                        if (!interpreter.Disposed)
                            host = interpreter.InternalHost;
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return host;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the interactive host associated with
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive host is to be obtained.
        /// </param>
        /// <param name="promptFlags">
        /// Upon success, receives the prompt flags associated with the
        /// interpreter.
        /// </param>
        /// <returns>
        /// The interactive host associated with the interpreter, or null if it
        /// is not available.
        /// </returns>
        private static IInteractiveHost TryGetInteractive(
            Interpreter interpreter,    /* in */
            ref PromptFlags promptFlags /* out */
            )
        {
            IInteractiveHost interactiveHost = null;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (!locked)
                    {
                        TraceOps.LockTrace(
                            "TryGetInteractive(1)",
                            typeof(HostOps).Name, false,
                            TracePriority.LockWarning2,
                            interpreter.MaybeWhoHasLock());

                        int timeout = InteractiveGetTimeout; /* NO-LOCK */

                        if (timeout >= 0)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryGetInteractive: retrying " +
                                "for {0} milliseconds...",
                                timeout), typeof(HostOps).Name,
                                TracePriority.HostDebug);

                            interpreter.InternalTryLock(
                                timeout, ref locked); /* TRANSACTIONAL */

                            if (!locked)
                            {
                                TraceOps.LockTrace(
                                    "TryGetInteractive(2)",
                                    typeof(HostOps).Name, false,
                                    TracePriority.LockError,
                                    interpreter.MaybeWhoHasLock());
                            }
                        }
                    }

                    if (locked)
                    {
                        //
                        // BUGFIX: Prevent a race condition between grabbing
                        //         the host and the interpreter being disposed.
                        //         This is necessary because we are called in
                        //         the critical code path of both the Wait and
                        //         WaitVariable methods.
                        //
                        if (!interpreter.Disposed)
                        {
                            interactiveHost = interpreter.GetInteractiveHost();

                            ///////////////////////////////////////////////////

#if SHELL
                            promptFlags = interpreter.InternalPromptFlags;
#else
                            promptFlags = PromptFlags.Default;
#endif
                        }
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return interactiveHost;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of active interactive loops for the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.
        /// </param>
        /// <returns>
        /// The number of active interactive loops, or zero if it cannot be
        /// determined.
        /// </returns>
        public static int TryGetInteractiveLoops(
            Interpreter interpreter /* in */
            )
        {
            try
            {
                return interpreter.ActiveInteractiveLoops;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.HostError);
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method may adjust the specified prompt flags to include the
        /// interpreter identifier.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose identifier is used.
        /// </param>
        /// <param name="promptFlags">
        /// The prompt flags to adjust.  This may be modified.
        /// </param>
        /// <param name="id">
        /// The interpreter identifier.  This may be modified.
        /// </param>
        public static void MaybeAdjustPromptFlags(
            Interpreter interpreter,     /* in */
            ref PromptFlags promptFlags, /* in, out */
            ref long id                  /* in, out */
            )
        {
            //
            // NOTE: If available, grab the integer identifier for
            //       the interpreter as this will help the end users
            //       to identity which interpreter is emitting the
            //       prompt.
            //
            if (interpreter != null)
            {
                id = interpreter.IdNoThrow;

                if (id > 1) /* HACK: Omit Id for primary. */
                    promptFlags |= PromptFlags.Interpreter;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the default interactive prompt string for the
        /// specified prompt type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the prompt is being built for.
        /// </param>
        /// <param name="type">
        /// The type of prompt to build.
        /// </param>
        /// <param name="promptFlags">
        /// The prompt flags that control which prefixes are added.
        /// </param>
        /// <param name="id">
        /// The interpreter identifier used by the identifier prefix.
        /// </param>
        /// <param name="count">
        /// The command count used by the command count prefix.
        /// </param>
        /// <returns>
        /// The default prompt string.
        /// </returns>
        public static string GetDefaultPrompt(
            Interpreter interpreter, /* in */
            PromptType type,         /* in */
            PromptFlags promptFlags, /* in */
            long id,                 /* in */
            int count                /* in */
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

            if (((int)type >= 0) && ((int)type < DefaultPrompts.Count))
            {
                builder.Append(DefaultPrompts[(int)type]);

                if (FlagOps.HasFlags(
                        promptFlags, PromptFlags.CommandCount, true))
                {
                    builder.Insert(0, String.Format(CountPrefixFormat, count));
                }

                if (FlagOps.HasFlags(
                        promptFlags, PromptFlags.Queue, true))
                {
                    builder.Insert(0, QueuePrefix);
                }

                if (FlagOps.HasFlags(
                        promptFlags, PromptFlags.Debug, true))
                {
                    builder.Insert(0, DebugPrefix);
                }

                if (FlagOps.HasFlags(
                        promptFlags, PromptFlags.Interpreter, true))
                {
                    builder.Insert(0, String.Format(IdPrefixFormat, id));
                }

                if ((interpreter != null) && FlagOps.HasFlags(
                        promptFlags, PromptFlags.ActiveLoops, true))
                {
                    int loops = TryGetInteractiveLoops(interpreter);

                    if (loops > MinimumPrefixLoops)
                    {
                        builder.Insert(0,
                            String.Format(LoopsPrefixFormat, loops));
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the formatted interactive mode string for the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.
        /// </param>
        /// <returns>
        /// The formatted interactive mode string, or null if there is no
        /// interactive mode.
        /// </returns>
        public static string GetInteractiveMode(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            string interactiveMode = interpreter.InteractiveMode;

            if (!String.IsNullOrEmpty(interactiveMode))
                return String.Format(InteractiveModeFormat, interactiveMode);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified detail flags request
        /// empty content.
        /// </summary>
        /// <param name="detailFlags">
        /// The detail flags to check.
        /// </param>
        /// <returns>
        /// True if empty content is requested; otherwise, false.
        /// </returns>
        public static bool HasEmptyContent(
            DetailFlags detailFlags /* in */
            )
        {
            return FlagOps.HasFlags(
                detailFlags, DetailFlags.EmptyContent, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified detail flags request
        /// verbose content.
        /// </summary>
        /// <param name="detailFlags">
        /// The detail flags to check.
        /// </param>
        /// <returns>
        /// True if verbose content is requested; otherwise, false.
        /// </returns>
        public static bool HasVerboseContent(
            DetailFlags detailFlags /* in */
            )
        {
            return FlagOps.HasFlags(
                detailFlags, DetailFlags.VerboseContent, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the effective detail flags for the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query.
        /// </param>
        /// <returns>
        /// The detail flags for the interpreter, or the standard detail flags
        /// if none are available.
        /// </returns>
        public static DetailFlags GetDetailFlags(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter != null)
            {
                DetailFlags detailFlags = interpreter.DetailFlags;

                if (detailFlags != DetailFlags.Invalid)
                    return detailFlags;
            }

            return DetailFlags.Standard;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified header flags into their
        /// corresponding detail flags.
        /// </summary>
        /// <param name="headerFlags">
        /// The header flags to translate.
        /// </param>
        /// <param name="detailFlags">
        /// The detail flags to add to.  This may be modified.
        /// </param>
        public static void HeaderFlagsToDetailFlags(
            HeaderFlags headerFlags,    /* in */
            ref DetailFlags detailFlags /* in, out */
            )
        {
            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.EmptySection, true))
            {
                detailFlags |= DetailFlags.EmptySection;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.EmptyContent, true))
            {
                detailFlags |= DetailFlags.EmptyContent;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.CallStackAllFrames, true))
            {
                detailFlags |= DetailFlags.CallStackAllFrames;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.DebuggerBreakpoints, true))
            {
                detailFlags |= DetailFlags.DebuggerBreakpoints;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.EngineNative, true))
            {
                detailFlags |= DetailFlags.EngineNative;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostDimensions, true))
            {
                detailFlags |= DetailFlags.HostDimensions;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostFormatting, true))
            {
                detailFlags |= DetailFlags.HostFormatting;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostColors, true))
            {
                detailFlags |= DetailFlags.HostColors;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostNames, true))
            {
                detailFlags |= DetailFlags.HostNames;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.TraceCached, true))
            {
                detailFlags |= DetailFlags.TraceCached;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.VariableLinks, true))
            {
                detailFlags |= DetailFlags.VariableLinks;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.VariableSearches, true))
            {
                detailFlags |= DetailFlags.VariableSearches;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.VariableElements, true))
            {
                detailFlags |= DetailFlags.VariableElements;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: All interpreter members used by this method MUST be safe
        //          to use after the interpreter has been disposed.
        //
        /// <summary>
        /// This method builds a list of introspection information describing
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to describe.
        /// </param>
        /// <param name="name">
        /// The name used for the section heading, or null to omit the heading.
        /// </param>
        /// <param name="detailFlags">
        /// The detail flags that control which information is included.
        /// </param>
        /// <param name="list">
        /// The list that interpreter information is added to.  This may be
        /// modified.
        /// </param>
        /// <returns>
        /// True if the information was built successfully; otherwise, false.
        /// </returns>
        public static bool BuildInterpreterInfoList(
            Interpreter interpreter, /* in */
            string name,             /* in */
            DetailFlags detailFlags, /* in */
            ref StringPairList list  /* in, out */
            )
        {
            if (list == null)
                list = new StringPairList();

            bool empty = HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            try
            {
                if (interpreter == null)
                {
                    if (empty)
                        localList.Add("Id", FormatOps.DisplayNull);

                    return true;
                }

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    localList.Add("Id", interpreter.IdNoThrow.ToString());

                    localList.Add("Created",
                        FormatOps.Iso8601FullDateTime(interpreter.CreatedNoThrow));

                    if (empty || interpreter.Disposed)
                    {
                        localList.Add("Disposed",
                            interpreter.Disposed.ToString());
                    }

                    if (empty || interpreter.Deleted)
                    {
                        localList.Add("Deleted",
                            interpreter.Deleted.ToString());
                    }

                    if (empty || interpreter.InternalExit)
                    {
                        localList.Add("Exit",
                            interpreter.InternalExit.ToString());
                    }

                    if (empty ||
                        (interpreter.InternalExitCode != ResultOps.SuccessExitCode()))
                    {
                        localList.Add("ExitCode",
                            interpreter.InternalExitCode.ToString());
                    }
                }

                return true;
            }
            finally
            {
                if (localList.Count > 0)
                {
                    if (name != null)
                    {
                        list.Add((IPair<string>)null);
                        list.Add((name.Length > 0) ? name : "Interpreter");
                        list.Add((IPair<string>)null);
                    }

                    list.Add(localList);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Sleep Support Methods
        /// <summary>
        /// This method causes the current thread to sleep for the specified
        /// number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ThreadSleep(
            int milliseconds, /* in */
            ref Result error  /* out */
            ) /* THREAD-SAFE */
        {
            Exception exception = null;

            return ThreadSleep(milliseconds, ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to sleep for the specified
        /// number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ThreadSleep(
            int milliseconds,       /* in */
            ref Exception exception /* out */
            ) /* THREAD-SAFE */
        {
            Result error = null;

            return ThreadSleep(milliseconds, ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to sleep for the specified
        /// number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ThreadSleep(
            int milliseconds,        /* in */
            ref Exception exception, /* out */
            ref Result error         /* out */
            ) /* THREAD-SAFE */
        {
            try
            {
                ThreadSleep(milliseconds); /* throw */
                return ReturnCode.Ok;
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                exception = e;
                error = e;
            }
            catch (ThreadInterruptedException e)
            {
                exception = e;
                error = e;
            }
            catch (Exception e)
            {
                exception = e;
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to sleep for the specified
        /// number of milliseconds, updating the related diagnostic counters.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        public static void ThreadSleep(
            int milliseconds /* in */
            ) /* THREAD-SAFE */
        {
            Interlocked.Increment(ref pendingSleepCount);

            try
            {
                Thread.Sleep(milliseconds); /* throw */

                /* IGNORED */
                Interlocked.Add(
                    ref totalSleepMilliseconds, milliseconds);
            }
            finally
            {
                Interlocked.Decrement(ref pendingSleepCount);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Yield Support Methods
        /// <summary>
        /// This method causes the current thread to yield its remaining time
        /// slice.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ThreadYield(
            ref Result error /* out */
            ) /* THREAD-SAFE */
        {
            Exception exception = null;

            return ThreadYield(ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to yield its remaining time
        /// slice.
        /// </summary>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ThreadYield(
            ref Exception exception, /* out */
            ref Result error         /* out */
            ) /* THREAD-SAFE */
        {
            try
            {
                ThreadYield(); /* throw */
                return ReturnCode.Ok;
            }
#if !NET_40
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                exception = e;
                error = e;
            }
            catch (ThreadInterruptedException e)
            {
                exception = e;
                error = e;
            }
#endif
            catch (Exception e)
            {
                exception = e;
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the current thread to yield its remaining time
        /// slice, updating the related diagnostic counters.
        /// </summary>
        public static void ThreadYield() /* THREAD-SAFE */
        {
            Interlocked.Increment(ref pendingYieldCount);

            try
            {
#if NET_40
                //
                // NOTE: Available on the .NET Framework 4.0+ only.
                //
                Thread.Yield(); /* throw */
#else
                //
                // NOTE: Do something "fake" but useful here.
                //
                Thread.Sleep(1); /* throw */
#endif

                /* IGNORED */
                Interlocked.Increment(ref totalYieldCount);
            }
            finally
            {
                Interlocked.Decrement(ref pendingYieldCount);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Yield / Sleep Support Methods
        /// <summary>
        /// This method extracts the yield type and millisecond count encoded
        /// within the specified integer value.
        /// </summary>
        /// <param name="value">
        /// The encoded value to decode.
        /// </param>
        /// <param name="type">
        /// Upon return, receives the decoded yield type.
        /// </param>
        /// <param name="milliseconds">
        /// Upon return, receives the decoded number of milliseconds.
        /// </param>
        private static void YieldTypeAndMilliseconds(
            int value,           /* in */
            out YieldType type,  /* out */
            out int milliseconds /* out */
            )
        {
            type = ((YieldType)value & YieldType.Mask);
            milliseconds = (value & (int)~type);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally yields the current thread and/or sleeps
        /// for a period of time, based on the specified encoded value.
        /// </summary>
        /// <param name="value">
        /// The encoded value specifying the yield type and number of
        /// milliseconds.
        /// </param>
        public static void MaybeThreadYieldAndOrSleep(
            int value /* in */
            )
        {
            YieldType type;
            int milliseconds;

            YieldTypeAndMilliseconds(value, out type, out milliseconds);

            if ((type == YieldType.Both) || (type == YieldType.Pure))
            {
                /* NO RESULT */
                ThreadYield();
            }

            if ((type == YieldType.Both) || (type == YieldType.Sleep))
            {
                /* NO RESULT */
                ThreadSleep(milliseconds);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods for [host *] Sub-Commands
        //
        // BUGBUG: This only works for interpreters that are known from this
        //         AppDomain.
        //
        /// <summary>
        /// This method resets the standard channels for all interpreters known
        /// to this application domain.
        /// </summary>
        /// <param name="channelType">
        /// The type of standard channels to reset.
        /// </param>
        public static void ResetAllInterpreterStandardChannels(
            ChannelType channelType /* in */
            )
        {
            IEnumerable<Interpreter> interpreters =
                GlobalState.GetInterpreters();

            if (interpreters == null)
                return;

            channelType |= ChannelType.AllowExist |
                ChannelType.UseCurrent | ChannelType.UseHost;

            foreach (Interpreter interpreter in interpreters)
            {
                if (interpreter == null)
                    continue;

                ReturnCode code;
                Result error = null;

                code = interpreter.ModifyStandardChannels(
                    null, null, channelType, ref error);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, error);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Wrapper Methods
        #region Exit Support Methods
        /// <summary>
        /// This method sets the host exiting flag for the host(s) associated
        /// with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host(s) should be flagged.
        /// </param>
        /// <param name="exiting">
        /// Non-zero to indicate the host is exiting; otherwise, zero.
        /// </param>
        public static void SetExiting(
            Interpreter interpreter, /* in */
            bool exiting             /* in */
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is already fully disposed, just
                //       do nothing.
                //
                if (interpreter.Disposed)
                    return;

                ///////////////////////////////////////////////////////////////

                SetExiting(
                    interpreter, interpreter.InternalHost, null, false,
                    exiting);

                ///////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
                SetExiting(
                    interpreter, interpreter.IsolatedHost, null, true,
                    exiting);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the host exiting flag for the specified process
        /// host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the host, used for diagnostics.
        /// </param>
        /// <param name="processHost">
        /// The process host whose exiting flag should be set.
        /// </param>
        /// <param name="hostName">
        /// The name of the host, used for diagnostics.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if the host is an isolated host; otherwise, zero.
        /// </param>
        /// <param name="exiting">
        /// Non-zero to indicate the host is exiting; otherwise, zero.
        /// </param>
        public static void SetExiting(
            Interpreter interpreter,  /* in */
            IProcessHost processHost, /* in */
            string hostName,          /* in */
            bool isolated,            /* in */
            bool exiting              /* in */
            )
        {
            //
            // BUGFIX: Disposal ordering issue.  There is no need to set (or
            //         unset) the host "exiting" flag if it has been disposed.
            //
            try
            {
                if ((processHost != null) && !IsDisposed(processHost) &&
                    FlagOps.HasFlags(
                        processHost.GetHostFlags(), HostFlags.Exit, true))
                {
                    processHost.Exiting = exiting;
                }
            }
            catch (Exception e)
            {
                DebugOps.Complain(
                    interpreter, ReturnCode.Error, String.Format(
                    "caught exception while {0} {1}host {2}: {3}",
                    exiting ? "exiting" : "unexiting", isolated ?
                    "isolated " : String.Empty, FormatOps.WrapOrNull(
                    hostName), e));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sleep Support Methods
        /// <summary>
        /// This method causes the host associated with the specified
        /// interpreter to sleep for the specified number of milliseconds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host should sleep.
        /// </param>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Sleep(
            Interpreter interpreter, /* in */
            int milliseconds,        /* in */
            ref Result error         /* out */
            ) /* THREAD-SAFE */
        {
            return Sleep(
                TryGet(interpreter), milliseconds, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the specified thread host to sleep for the
        /// specified number of milliseconds.
        /// </summary>
        /// <param name="threadHost">
        /// The thread host that should sleep.
        /// </param>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Sleep(
            IThreadHost threadHost, /* in */
            int milliseconds,       /* in */
            ref Result error        /* out */
            ) /* THREAD-SAFE */
        {
            return Sleep(
                threadHost, milliseconds, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the specified thread host to sleep for the
        /// specified number of milliseconds, optionally falling back to
        /// sleeping the current thread.
        /// </summary>
        /// <param name="threadHost">
        /// The thread host that should sleep.
        /// </param>
        /// <param name="milliseconds">
        /// The number of milliseconds to sleep.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require host support for sleeping; otherwise, zero to
        /// fall back to sleeping the current thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode Sleep(
            IThreadHost threadHost, /* in */
            int milliseconds,       /* in */
            bool strict,            /* in */
            ref Result error        /* out */
            )
        {
            if (threadHost != null)
            {
                try
                {
                    if (FlagOps.HasFlags(
                            threadHost.GetHostFlags(), HostFlags.Sleep,
                            true))
                    {
                        if (threadHost.Sleep(milliseconds))
                            return ReturnCode.Ok;
                        else
                            error = "host sleep failed";
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            NoFeatureError, HostFlags.Sleep);
                    }
                    else
                    {
                        return ThreadSleep(milliseconds, ref error);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else if (strict)
            {
                error = "interpreter host not available";
            }
            else
            {
                return ThreadSleep(milliseconds, ref error);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Yield Support Methods
        /// <summary>
        /// This method causes the host associated with the specified
        /// interpreter to yield the current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host should yield.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require host support for yielding; otherwise, zero to
        /// fall back to yielding the current thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Yield(
            Interpreter interpreter, /* in */
            bool strict,             /* in */
            ref Result error         /* out */
            )
        {
            return Yield(
                TryGet(interpreter), strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method causes the specified thread host to yield the current
        /// thread, optionally falling back to yielding the current thread
        /// directly.
        /// </summary>
        /// <param name="threadHost">
        /// The thread host that should yield.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require host support for yielding; otherwise, zero to
        /// fall back to yielding the current thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode Yield(
            IThreadHost threadHost, /* in */
            bool strict,            /* in */
            ref Result error        /* out */
            )
        {
            if (threadHost != null)
            {
                try
                {
                    if (FlagOps.HasFlags(
                            threadHost.GetHostFlags(), HostFlags.Yield,
                            true))
                    {
                        if (threadHost.Yield())
                            return ReturnCode.Ok;
                        else
                            error = "host yield failed";
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            NoFeatureError, HostFlags.Yield);
                    }
                    else
                    {
                        return ThreadYield(ref error);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else if (strict)
            {
                error = "interpreter host not available";
            }
            else
            {
                return ThreadYield(ref error);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Console Support Methods
#if CONSOLE
#if NATIVE && WINDOWS
        /// <summary>
        /// This method determines whether the native Win32 console API should
        /// be used for console output.
        /// </summary>
        /// <returns>
        /// True if the native console should be used; otherwise, false.
        /// </returns>
        public static bool ShouldUseNative()
        {
            return useNativeConsole;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets whether the native Win32 console API should be used
        /// for console output.
        /// </summary>
        /// <param name="useNative">
        /// Non-zero to use the native console; otherwise, zero.
        /// </param>
        public static void SetUseNative(
            bool useNative /* in */
            )
        {
            useNativeConsole = useNative;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the console, without a
        /// trailing line terminator.
        /// </summary>
        /// <param name="value">
        /// The value to write.
        /// </param>
        private static void ConsoleWrite(
            string value /* in */
            )
        {
#if NATIVE && WINDOWS
            if (ShouldUseNative())
            {
                ConsoleOps.WriteNative<string>(value, false); /* throw */
                return;
            }
#endif

            Console.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the console, followed by a
        /// line terminator.
        /// </summary>
        /// <param name="value">
        /// The value to write.
        /// </param>
        private static void ConsoleWriteLine(
            string value /* in */
            )
        {
#if NATIVE && WINDOWS
            if (ShouldUseNative())
            {
                ConsoleOps.WriteNative<string>(value, true); /* throw */
                return;
            }
#endif

            Console.WriteLine(value);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Support Methods
        /// <summary>
        /// This method writes the specified value, followed by a line
        /// terminator, to the specified interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to.
        /// </param>
        /// <param name="value">
        /// The value to write, or null to write an empty line.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
        public static bool WriteLine(
            IInteractiveHost interactiveHost, /* in */
            string value                      /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return (value != null) ?
                        interactiveHost.WriteLine(value) :
                        interactiveHost.WriteLine();
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code and result, followed by
        /// a line terminator, to the specified interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <returns>
        /// True if the value was written successfully; otherwise, false.
        /// </returns>
        public static bool WriteResultLine(
            IInteractiveHost interactiveHost, /* in */
            ReturnCode code,                  /* in */
            Result result                     /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.WriteResultLine(code, result);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value to the specified interactive
        /// host, falling back to the console when no interactive host is
        /// available.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to, if any.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        public static void WriteOrConsole(
            IInteractiveHost interactiveHost, /* in */
            string value                      /* in */
            )
        {
            if (interactiveHost != null)
                interactiveHost.Write(value);
#if CONSOLE
            else
                ConsoleWrite(value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified value, followed by a line
        /// terminator, to the specified interactive host, falling back to the
        /// console when no interactive host is available.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to, if any.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        public static void WriteLineOrConsole(
            IInteractiveHost interactiveHost, /* in */
            string value                      /* in */
            )
        {
            if (interactiveHost != null)
                interactiveHost.WriteLine(value);
#if CONSOLE
            else
                ConsoleWriteLine(value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code and result to the
        /// console, complaining about them if no console output is available.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        public static void WriteConsoleOrComplain(
            ReturnCode code, /* in */
            Result result    /* in */
            )
        {
            WriteConsoleOrComplain(code, result, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes the specified return code and result to the
        /// console, complaining about them if no console output is available.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The error line number associated with the result.
        /// </param>
        public static void WriteConsoleOrComplain(
            ReturnCode code, /* in */
            Result result,   /* in */
            int errorLine    /* in */
            )
        {
#if CONSOLE
            try
            {
                ConsoleWriteLine(ResultOps.Format(code, result, errorLine));
            }
            catch
#endif
            {
                //
                // NOTE: Either there is no System.Console support available
                //       -OR- it somehow failed to produce output.  Complain
                //       about the original issue.
                //
                DebugOps.Complain(code, result);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        /// <summary>
        /// This method determines whether the specified type is, or derives
        /// from, the null host type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is a null host type; otherwise, false.
        /// </returns>
        private static bool IsNullType(
            Type type /* in */
            )
        {
            return (type != null) &&
                ((type == typeof(_Hosts.Null)) ||
                type.IsSubclassOf(typeof(_Hosts.Null)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interactive host has
        /// been disposed.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to check.
        /// </param>
        /// <returns>
        /// True if the interactive host has been disposed; otherwise, false.
        /// </returns>
        public static bool IsDisposed(
            IInteractiveHost interactiveHost /* in */
            )
        {
            if (interactiveHost == null)
                return false;

            if (ObjectOps.IsDisposed(interactiveHost))
                return true;

            try
            {
                /* IGNORED */
                interactiveHost.IsOpen(); /* throw */

                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUG && VERBOSE
        /// <summary>
        /// This method emits a diagnostic trace describing the specified
        /// interactive host context.
        /// </summary>
        /// <param name="prefix">
        /// The prefix used to identify the trace message.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the trace.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host associated with the trace.
        /// </param>
        /// <param name="interactiveLoops">
        /// The number of active interactive loops.
        /// </param>
        /// <param name="priority">
        /// The trace priority to use, or null to use the default priority.
        /// </param>
        public static void EmitTrace(
            string prefix,
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            int interactiveLoops,             /* in */
            TracePriority? priority           /* in */
            )
        {
            string name = null;
            Guid id = Guid.Empty;

            if (interactiveHost != null)
            {
                name = interactiveHost.Name;

                if ((name == null) &&
                    !AppDomainOps.IsTransparentProxy(interactiveHost))
                {
                    name = FormatOps.TypeName(interactiveHost, false);
                }

                id = interactiveHost.Id;
            }

            TraceOps.DebugTrace(interpreter, String.Format(
                "{0}: interpreter = {1}, interactiveHost = {2} ({3}), " +
                "interactiveLoops = {4}",
                prefix, FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(name), id, interactiveLoops),
                typeof(HostOps).Name, (priority != null) ?
                    (TracePriority)priority : TracePriority.ShellDebug2, 1);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the interactive host for the
        /// specified interpreter is open.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive host is checked.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to force a refresh of the interactive host, zero to prevent
        /// it, or null to decide automatically.
        /// </param>
        /// <param name="hostFlags">
        /// The host flags to use or refresh.  This may be modified.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host to use or refresh.  This may be modified.
        /// </param>
        /// <returns>
        /// True if the interactive host is open; otherwise, false.
        /// </returns>
        public static bool IsOpen(
            Interpreter interpreter,             /* in */
            bool? refresh,                       /* in */
            ref HostFlags hostFlags,             /* in, out */
            ref IInteractiveHost interactiveHost /* out */
            )
        {
            PromptFlags promptFlags = PromptFlags.None; /* NOT USED */

            return IsOpen(
                interpreter, refresh, ref hostFlags,
                ref promptFlags, ref interactiveHost);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the interactive host for the
        /// specified interpreter is open.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive host is checked.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to force a refresh of the interactive host, zero to prevent
        /// it, or null to decide automatically.
        /// </param>
        /// <param name="hostFlags">
        /// The host flags to use or refresh.  This may be modified.
        /// </param>
        /// <param name="promptFlags">
        /// The prompt flags to use or refresh.  This may be modified.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host to use or refresh.  This may be modified.
        /// </param>
        /// <returns>
        /// True if the interactive host is open; otherwise, false.
        /// </returns>
        public static bool IsOpen(
            Interpreter interpreter,             /* in */
            bool? refresh,                       /* in */
            ref HostFlags hostFlags,             /* in, out */
            ref PromptFlags promptFlags,         /* in, out */
            ref IInteractiveHost interactiveHost /* out */
            )
        {
            bool localRefresh;
            HostFlags localHostFlags;
            PromptFlags localPromptFlags = PromptFlags.None;
            IInteractiveHost localInteractiveHost;

            if (refresh != null)
            {
                localRefresh = (bool)refresh;

                TraceOps.DebugTrace(String.Format(
                    "IsOpen: interactive host refresh manually {0}",
                    localRefresh ? "enabled" : "disabled"),
                    typeof(HostOps).Name, TracePriority.HostDebug);
            }
            else
            {
                //
                // HACK: Do not refresh the interactive host for the
                //       caller if it resides in another AppDomain.
                //       This design decision may need to be revised
                //       at a later time.
                //
                localInteractiveHost = TryGetInteractive(
                    interpreter, ref localPromptFlags);

#if DEBUG && VERBOSE
                EmitTrace(
                    "IsOpen(refresh1)", interpreter,
                    localInteractiveHost, TryGetInteractiveLoops(
                    interpreter), TracePriority.HostDebug);
#endif

                if (localInteractiveHost == null)
                {
                    localRefresh = false;

                    TraceOps.DebugTrace(
                        "IsOpen: fetched interactive host is invalid",
                        typeof(HostOps).Name, TracePriority.HostDebug);
                }
                else if (IsDisposed(localInteractiveHost))
                {
                    localRefresh = false;

                    TraceOps.DebugTrace(
                        "IsOpen: fetched interactive host is disposed",
                        typeof(HostOps).Name, TracePriority.HostDebug);
                }
                else
                {
                    if (AppDomainOps.MatchIsTransparentProxy(
                            localInteractiveHost, interactiveHost, true))
                    {
                        localRefresh = true;
                    }
                    else
                    {
                        localRefresh = false;
                    }

#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "IsOpen: interactive host transparent proxy {0}",
                        localRefresh ? "match" : "mismatch"),
                        typeof(HostOps).Name, TracePriority.HostDebug);
#endif
                }

                localInteractiveHost = null;
            }

            if (localRefresh)
            {
                localHostFlags = HostFlags.None; /* reset inside try */

                localInteractiveHost = TryGetInteractive(
                    interpreter, ref localPromptFlags);

#if DEBUG && VERBOSE
                EmitTrace(
                    "IsOpen(refresh2)", interpreter,
                    localInteractiveHost, TryGetInteractiveLoops(
                    interpreter), TracePriority.HostDebug);
#endif
            }
            else
            {
                localHostFlags = hostFlags;
                localPromptFlags = promptFlags;
                localInteractiveHost = interactiveHost;
            }

            bool success = false;

            try
            {
                string isNewOrOld = localRefresh ? "new" : "old";

                if (localInteractiveHost == null)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsOpen: {0} interactive host not available",
                        isNewOrOld), typeof(HostOps).Name,
                        TracePriority.HostError);

                    return false;
                }

                if (IsDisposed(localInteractiveHost))
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsOpen: {0} interactive host is disposed",
                        isNewOrOld), typeof(HostOps).Name,
                        TracePriority.HostError);

                    return false;
                }

                if (localRefresh)
                {
                    /* throw */
                    localHostFlags = localInteractiveHost.GetHostFlags();
                }

                //
                // HACK: Is the interactive host in an "error state" due
                //       to being unable to read or write?  This is used
                //       to detect the lack of a real, usable console.
                //
                if (FlagOps.HasFlags(
                        localHostFlags, HostFlags.ExceptionMask, false))
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsOpen: {0} interactive host in error state",
                        isNewOrOld), typeof(HostOps).Name,
                        TracePriority.HostError);

                    return false;
                }

                if (localInteractiveHost.IsOpen()) /* throw */
                {
#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "IsOpen: {0} interactive host is open",
                        isNewOrOld), typeof(HostOps).Name,
                        TracePriority.HostDebug);
#endif

                    success = true;
                    return true;
                }

                if (localInteractiveHost.IsInputRedirected()) /* throw */
                {
#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "IsOpen: {0} interactive host is input redirected",
                        isNewOrOld), typeof(HostOps).Name,
                        TracePriority.HostDebug);
#endif

                    success = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.HostError);
            }
            finally
            {
                if (localRefresh && success)
                {
                    hostFlags = localHostFlags;
                    promptFlags = localPromptFlags;
                    interactiveHost = localInteractiveHost;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method may adjust the specified prompt flags based on the
        /// debugging and queue states.
        /// </summary>
        /// <param name="debug">
        /// Non-zero if interactive debugging is active.
        /// </param>
        /// <param name="queue">
        /// Non-zero if there are queued events.
        /// </param>
        /// <param name="promptFlags">
        /// The prompt flags to adjust.  This may be modified.
        /// </param>
        public static void MaybeAdjustPromptFlags(
            bool debug,                 /* in */
            bool queue,                 /* in */
            ref PromptFlags promptFlags /* in, out */
            )
        {
            //
            // NOTE: Set the prompt flags based on the
            //       parameters specified by our caller.
            //
            if (debug) promptFlags |= PromptFlags.Debug;
            if (queue) promptFlags |= PromptFlags.Queue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the header flags from the specified interactive
        /// host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to query.
        /// </param>
        /// <param name="default">
        /// The header flags to return if the interactive host is not available
        /// or fails.
        /// </param>
        /// <returns>
        /// The header flags from the interactive host, or the default value.
        /// </returns>
        public static HeaderFlags GetHeaderFlags(
            IInteractiveHost interactiveHost, /* in */
            HeaderFlags @default              /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.GetHeaderFlags(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the detail flags from the specified interactive
        /// host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to query.
        /// </param>
        /// <param name="default">
        /// The detail flags to return if the interactive host is not available
        /// or fails.
        /// </param>
        /// <returns>
        /// The detail flags from the interactive host, or the default value.
        /// </returns>
        public static DetailFlags GetDetailFlags(
            IInteractiveHost interactiveHost, /* in */
            DetailFlags @default              /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.GetDetailFlags(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the host flags from the specified interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to query.
        /// </param>
        /// <returns>
        /// The host flags from the interactive host, or
        /// <see cref="HostFlags.None" /> if it is not available or fails.
        /// </returns>
        public static HostFlags GetHostFlags(
            IInteractiveHost interactiveHost /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.GetHostFlags(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return HostFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether input for the specified interactive
        /// host is redirected.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to query.
        /// </param>
        /// <returns>
        /// True if input is redirected; otherwise, false.
        /// </returns>
        public static bool IsInputRedirected(
            IInteractiveHost interactiveHost /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.IsInputRedirected(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of nested read levels for the specified
        /// interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to query.
        /// </param>
        /// <returns>
        /// The number of nested read levels, or zero if it cannot be
        /// determined.
        /// </returns>
        public static int GetReadLevels(
            IInteractiveHost interactiveHost /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.ReadLevels; /* NON-SHARED ONLY */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a high-contrast console color suitable for use
        /// against the specified background color.
        /// </summary>
        /// <param name="color">
        /// The background color to obtain a high-contrast color for.
        /// </param>
        /// <returns>
        /// The high-contrast color, or <see cref="_ConsoleColor.None" /> if the
        /// specified color is not recognized.
        /// </returns>
        public static ConsoleColor GetHighContrastColor(
            ConsoleColor color /* in */
            )
        {
            switch (color)
            {
                case ConsoleColor.Black:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.DarkBlue:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.DarkGreen:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.DarkCyan:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.DarkRed:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.DarkMagenta:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.DarkYellow:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.Gray:
                    {
                        return highContrastDarkColor;
                    }
                case ConsoleColor.DarkGray:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.Blue:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.Green:
                    {
                        return highContrastDarkColor;
                    }
                case ConsoleColor.Cyan:
                    {
                        return highContrastDarkColor;
                    }
                case ConsoleColor.Red:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.Magenta:
                    {
                        return highContrastLightColor;
                    }
                case ConsoleColor.Yellow:
                    {
                        return highContrastDarkColor;
                    }
                case ConsoleColor.White:
                    {
                        return highContrastDarkColor;
                    }
                default:
                    {
                        return _ConsoleColor.None;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the foreground and/or background colors with the
        /// specified name from the specified color host.
        /// </summary>
        /// <param name="colorHost">
        /// The color host to query.
        /// </param>
        /// <param name="name">
        /// The name of the color pair to obtain.
        /// </param>
        /// <param name="foreground">
        /// Non-zero to obtain the foreground color.
        /// </param>
        /// <param name="background">
        /// Non-zero to obtain the background color.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require host support for colors.
        /// </param>
        /// <param name="foregroundColor">
        /// Upon success, receives the foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the background color.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetColors(
            IColorHost colorHost,             /* in */
            string name,                      /* in */
            bool foreground,                  /* in */
            bool background,                  /* in */
            bool strict,                      /* in */
            ref ConsoleColor foregroundColor, /* out */
            ref ConsoleColor backgroundColor, /* out */
            ref Result error                  /* out */
            )
        {
            ReturnCode code;

            //
            // NOTE: Is the interpreter host available (to make color
            //       decisions)?
            //
            if (colorHost != null)
            {
                try
                {
                    //
                    // NOTE: If a "Null"-typed interpreter host is being used
                    //       or the host does not support colors, just skip
                    //       this step.
                    //
                    Type hostType = AppDomainOps.MaybeGetTypeOrNull(colorHost);

                    if (!IsNullType(hostType) && FlagOps.HasFlags(
                            colorHost.GetHostFlags(), HostFlags.NonMonochromeMask,
                            false))
                    {
                        code = colorHost.GetColors(
                            null, name, foreground, background,
                            ref foregroundColor, ref backgroundColor,
                            ref error);
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            NoFeatureError, HostFlags.NonMonochromeMask);

                        code = ReturnCode.Error;
                    }
                    else
                    {
                        code = ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    code = ReturnCode.Error;
                }
            }
            else if (strict)
            {
                error = "interpreter host not available";
                code = ReturnCode.Error;
            }
            else
            {
                code = ReturnCode.Ok;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Support Methods
#if SHELL
        /// <summary>
        /// This method sets the title of the specified interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose title should be set.
        /// </param>
        /// <param name="value">
        /// The title value to set.
        /// </param>
        /// <returns>
        /// True if the title was set successfully; otherwise, false.
        /// </returns>
        public static bool SetTitle(
            IInteractiveHost interactiveHost, /* in */
            string value                      /* in */
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    interactiveHost.Title = value;
                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method (re)loads the host profile settings for the specified
        /// interactive host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the host, if any.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host whose profile should be loaded.
        /// </param>
        /// <param name="profile">
        /// The name of the profile to load.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when reading the profile, or null for the
        /// default.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode LoadProfile(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            string profile,                   /* in */
            Encoding encoding,                /* in */
            ref Result error                  /* out */
            )
        {
            _Hosts.Profile profileHost = interactiveHost as _Hosts.Profile;

            if (profileHost == null)
            {
                error = String.Format(
                    NoFeatureError, typeof(_Hosts.Profile).Name);

                return ReturnCode.Error;
            }

            //
            // NOTE: Now, we can grab the dynamically constructed host
            //       profile file name and use it to reload the profile.
            //       First, we set the name of the profile, which is
            //       indirectly used as input to the profile file name.
            //
            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            profileHost.Profile = profile;

            if (SettingsOps.LoadForHost(
                    interpreter, profileHost, AppDomainOps.MaybeGetType(
                    profileHost, typeof(_Hosts.Profile)), encoding,
                    profileHost.HostProfileFileName, cultureInfo,
                    _Hosts.Default.HostPropertyBindingFlags, false,
                    ref error))
            {
                return ReturnCode.Ok;
            }
            else
            {
                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Library Support Methods
        /// <summary>
        /// This method determines whether the specified script flags forbid
        /// obtaining a script from the host.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to check.
        /// </param>
        /// <param name="error">
        /// Upon a positive result, receives an error message.
        /// </param>
        /// <returns>
        /// True if obtaining a script from the host is forbidden; otherwise,
        /// false.
        /// </returns>
        public static bool HasNoHost(
            ScriptFlags scriptFlags, /* in */
            ref Result error         /* out */
            )
        {
            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.NoHost, true))
            {
                error = "forbidden from getting script from host";
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the named script from the specified
        /// file system host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the script is being obtained for.
        /// </param>
        /// <param name="fileSystemHost">
        /// The file system host to obtain the script from.
        /// </param>
        /// <param name="name">
        /// The name of the script to obtain.
        /// </param>
        /// <param name="direct">
        /// Non-zero to attempt the fast path via the core library resource
        /// manager first.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags to use.  This may be modified.
        /// </param>
        /// <param name="clientData">
        /// The client data to use.  This may be modified.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the script; upon failure, receives an error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetScript(
            Interpreter interpreter,        /* in */
            IFileSystemHost fileSystemHost, /* in */
            string name,                    /* in */
            bool direct,                    /* in */
            ref ScriptFlags scriptFlags,    /* in, out */
            ref IClientData clientData,     /* in, out */
            ref Result result               /* out */
            )
        {
            if (HasNoHost(scriptFlags, ref result))
                return ReturnCode.Error;

            if (fileSystemHost != null)
            {
                ScriptFlags localScriptFlags; /* REUSED */

                try
                {
                    HostFlags hostFlags = fileSystemHost.GetHostFlags();

                    //
                    // HACK: When "direct" mode is being used, always attempt
                    //       to use the core library resource manager first.
                    //       This is intended to be the "super-fast" path.
                    //
                    if (direct && (name != null))
                    {
#if ISOLATED_PLUGINS
                        if (!FlagOps.HasFlags(hostFlags, HostFlags.Isolated, true))
#endif
                        {
                            if (FlagOps.HasFlags(hostFlags, HostFlags.Data, true))
                            {
                                _Hosts.File fileHost = fileSystemHost as _Hosts.File;

                                if (fileHost != null)
                                {
                                    ResourcePair anyPair = new ResourcePair(
                                        GlobalState.GetAssemblyLocation(),
                                        fileHost.LibraryResourceManager);

                                    StringDictionary uniqueResourceNames =
                                        new StringDictionary(1);

                                    uniqueResourceNames.Add(name, null);

                                    DataFlags dataFlags = CombineDataFlags(
                                        interpreter, name, scriptFlags,
                                        DataFlags.Script);

                                    EngineFlags engineFlags =
                                        fileHost.GetEngineFlagsForReadScriptStream(
                                            interpreter, dataFlags, scriptFlags);

                                    ResultList errors = null;

                                    localScriptFlags = scriptFlags;

                                    if (fileHost.GetDataViaResourceManager( /* throw */
                                            interpreter, name, anyPair,
                                            uniqueResourceNames, engineFlags,
                                            dataFlags, false, false, null,
                                            ref localScriptFlags, ref clientData,
                                            ref result, ref errors) == ReturnCode.Ok)
                                    {
                                        scriptFlags = localScriptFlags;
                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "GetScript: direct failure: {0}",
                                            FormatOps.WrapOrNull(errors)),
                                            typeof(HostOps).Name,
                                            TracePriority.ResourceError);
                                    }
                                }
                            }
                        }
                    }

#if ISOLATED_PLUGINS
                    //
                    // HACK: If the current interpreter host is running
                    //       in an isolated application domain, use the
                    //       "backup" core host instead.
                    //
                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Isolated, true))
                    {
                        IFileSystemHost isolatedFileSystemHost = interpreter.IsolatedHost;

                        if (isolatedFileSystemHost != null)
                        {
                            HostFlags isolatedHostFlags = isolatedFileSystemHost.GetHostFlags();

                            if (FlagOps.HasFlags(
                                    isolatedHostFlags, HostFlags.Data, true))
                            {
                                localScriptFlags = scriptFlags;

                                if (isolatedFileSystemHost.GetData( /* throw */
                                        name, CombineDataFlags(
                                            interpreter, name, localScriptFlags,
                                            DataFlags.Script),
                                        ref localScriptFlags, ref clientData,
                                        ref result) == ReturnCode.Ok)
                                {
                                    scriptFlags = localScriptFlags;
                                    return ReturnCode.Ok;
                                }

                                return ReturnCode.Error;
                            }
                        }
                    }
#endif

                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Data, true))
                    {
                        localScriptFlags = scriptFlags;

                        if (fileSystemHost.GetData( /* throw */
                                name, CombineDataFlags(
                                    interpreter, name, localScriptFlags,
                                    DataFlags.Script),
                                ref localScriptFlags, ref clientData,
                                ref result) == ReturnCode.Ok)
                        {
                            scriptFlags = localScriptFlags;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                    else
                    {
                        result = "interpreter host does not have script support";
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);

                    result = e;
                }
            }
            else
            {
                result = "interpreter host not available";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Channel Support Methods
        /// <summary>
        /// This method attempts to obtain a stream for the specified path via
        /// the specified file system host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the stream is being obtained for.
        /// </param>
        /// <param name="fileSystemHost">
        /// The file system host to obtain the stream from.
        /// </param>
        /// <param name="path">
        /// The path of the file to open.
        /// </param>
        /// <param name="mode">
        /// The file mode to use.
        /// </param>
        /// <param name="access">
        /// The file access to use.
        /// </param>
        /// <param name="share">
        /// The file sharing to use.
        /// </param>
        /// <param name="bufferSize">
        /// The buffer size to use.
        /// </param>
        /// <param name="options">
        /// The file options to use.
        /// </param>
        /// <param name="strict">
        /// Non-zero to require host support for streams.
        /// </param>
        /// <param name="hostStreamFlags">
        /// The host stream flags to use.  This may be modified.
        /// </param>
        /// <param name="fullPath">
        /// Upon success, receives the full path of the opened file.
        /// </param>
        /// <param name="stream">
        /// Upon success, receives the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetStream(
            Interpreter interpreter,             /* in */
            IFileSystemHost fileSystemHost,      /* in */
            string path,                         /* in */
            FileMode mode,                       /* in */
            FileAccess access,                   /* in */
            FileShare share,                     /* in */
            int bufferSize,                      /* in */
            FileOptions options,                 /* in */
            bool strict,                         /* in */
            ref HostStreamFlags hostStreamFlags, /* in, out */
            ref string fullPath,                 /* out */
            ref Stream stream,                   /* out */
            ref Result error                     /* out */
            )
        {
            if (fileSystemHost != null)
            {
                try
                {
                    HostFlags hostFlags = fileSystemHost.GetHostFlags();

#if ISOLATED_PLUGINS
                    //
                    // HACK: If the current interpreter host is running
                    //       in an isolated application domain, use the
                    //       "backup" core host instead.
                    //
                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Isolated, true))
                    {
                        IFileSystemHost isolatedFileSystemHost = interpreter.IsolatedHost;

                        if (isolatedFileSystemHost != null)
                        {
                            HostFlags isolatedHostFlags = isolatedFileSystemHost.GetHostFlags();

                            if (FlagOps.HasFlags(
                                    isolatedHostFlags, HostFlags.Stream, true))
                            {
                                HostStreamFlags localHostStreamFlags =
                                    hostStreamFlags | isolatedFileSystemHost.StreamFlags;

                                ReturnCode code = isolatedFileSystemHost.GetStream(
                                    path, mode, access, share, bufferSize,
                                    options, ref localHostStreamFlags, ref fullPath,
                                    ref stream, ref error);

                                if (code == ReturnCode.Ok)
                                    hostStreamFlags = localHostStreamFlags;

                                return code;
                            }
                        }
                    }
#endif

                    if (FlagOps.HasFlags(hostFlags, HostFlags.Stream, true))
                    {
                        HostStreamFlags localHostStreamFlags =
                            hostStreamFlags | fileSystemHost.StreamFlags;

                        ReturnCode code = fileSystemHost.GetStream(
                            path, mode, access, share, bufferSize, options,
                            ref localHostStreamFlags, ref fullPath, ref stream,
                            ref error);

                        if (code == ReturnCode.Ok)
                            hostStreamFlags = localHostStreamFlags;

                        return code;
                    }
                    else if (strict)
                    {
                        error = "interpreter host does not have stream support";
                    }
                    else
                    {
                        return RuntimeOps.NewStream(
                            interpreter, path, mode, access, share, bufferSize,
                            options, ref hostStreamFlags, ref fullPath, ref stream,
                            ref error);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "interpreter host not available";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Data Support Methods
        /// <summary>
        /// This method gets the configured data flags, optionally restricting
        /// them for a restricted interpreter.
        /// </summary>
        /// <param name="culture">
        /// The name of the culture used to parse the configured value.
        /// </param>
        /// <param name="specific">
        /// Non-zero to require a culture-specific match.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used to determine whether to apply
        /// restrictions.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose configuration lookups.
        /// </param>
        /// <returns>
        /// The configured data flags.
        /// </returns>
        public static DataFlags GetDataFlags(
            string culture,          /* in */
            bool specific,           /* in */
            CreateFlags createFlags, /* in */
            bool verbose             /* in */
            )
        {
            DataFlags dataFlags = Defaults.DataFlags;

            string value = GlobalConfiguration.GetValue(
                EnvVars.DataFlags, GlobalConfiguration.GetFlags(
                ConfigurationFlags.Interpreter, verbose));

            if (!String.IsNullOrEmpty(value))
            {
                object enumValue;
                Result error = null;

                enumValue = EnumOps.TryParseFlags(
                    null, typeof(DataFlags), dataFlags.ToString(),
                    value, RuntimeOps.GetCultureInfo(culture,
                    specific), true, true, true, ref error);

                if (enumValue is DataFlags)
                {
                    //
                    // HACK: Do not allow a "safe" interpreter
                    //       to use "unsafe" data flags, e.g.
                    //       those other than the ones needed
                    //       for diagnostic tracing.
                    //
                    DataFlags localDataFlags = (DataFlags)enumValue;

                    if (Interpreter.InternalIsRestricted(createFlags))
                        localDataFlags &= DataFlags.SafeMask;

                    dataFlags = localDataFlags;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetDataFlags: error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(HostOps).Name,
                        TracePriority.EnumError);
                }
            }

            return dataFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the specified data flags with those of the
        /// interpreter and any implied by the script flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose data flags should be combined, if any.
        /// </param>
        /// <param name="name">
        /// The name of the data being requested.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags that may imply additional data flags.
        /// </param>
        /// <param name="dataFlags">
        /// The base data flags to combine.
        /// </param>
        /// <returns>
        /// The combined data flags.
        /// </returns>
        public static DataFlags CombineDataFlags(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ScriptFlags scriptFlags, /* in */
            DataFlags dataFlags      /* in */
            )
        {
            DataFlags result = dataFlags;

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    result |= interpreter.DataFlagsNoLock;
                }
            }

            //
            // HACK: For the "lib/Eagle1.0/vendor.eagle" core script library
            //       file, attempt to search all parent directories until it
            //       is found.  The search may still fail to locate the file;
            //       however, this gives "vendors" the ability to more easily
            //       customize its location, while still being "relative" to
            //       the application directory.
            //
            // NOTE: Removed the special case file name handling here and now
            //       rely ONLY upon the script flags.  This should provide a
            //       robust pre-check without "hard-coding" a set of expected
            //       "vendor" file names.
            //
            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.Vendor, true))
                result |= DataFlags.SearchParents;

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
        /// <summary>
        /// This method builds the host creation flags from the specified
        /// options.
        /// </summary>
        /// <param name="hostCreateFlags">
        /// The base host creation flags supplied by the caller.
        /// </param>
        /// <param name="useAttach">
        /// Non-zero to attach to an existing console.
        /// </param>
        /// <param name="useForce">
        /// Non-zero to force console creation.
        /// </param>
        /// <param name="noColor">
        /// Non-zero to disable host color support.
        /// </param>
        /// <param name="noTitle">
        /// Non-zero to disable setting the host title.
        /// </param>
        /// <param name="noIcon">
        /// Non-zero to disable setting the host icon.
        /// </param>
        /// <param name="noProfile">
        /// Non-zero to disable loading the host profile.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to disable cancellation support.
        /// </param>
        /// <returns>
        /// The resulting host creation flags.
        /// </returns>
        public static HostCreateFlags GetCreateFlags(
            HostCreateFlags hostCreateFlags, /* in */
            bool useAttach,                  /* in */
            bool useForce,                   /* in */
            bool noColor,                    /* in */
            bool noTitle,                    /* in */
            bool noIcon,                     /* in */
            bool noProfile,                  /* in */
            bool noCancel                    /* in */
            )
        {
            HostCreateFlags result = Defaults.HostCreateFlags;

            if (useAttach)
                result |= HostCreateFlags.UseAttach;
            else
                result &= ~HostCreateFlags.UseAttach;

            if (useForce)
                result |= HostCreateFlags.UseForce;
            else
                result &= ~HostCreateFlags.UseForce;

            if (noColor)
                result |= HostCreateFlags.NoColor;
            else
                result &= ~HostCreateFlags.NoColor;

            if (noTitle)
                result |= HostCreateFlags.NoTitle;
            else
                result &= ~HostCreateFlags.NoTitle;

            if (noIcon)
                result |= HostCreateFlags.NoIcon;
            else
                result &= ~HostCreateFlags.NoIcon;

            if (noProfile)
                result |= HostCreateFlags.NoProfile;
            else
                result &= ~HostCreateFlags.NoProfile;

            if (noCancel)
                result |= HostCreateFlags.NoCancel;
            else
                result &= ~HostCreateFlags.NoCancel;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains several host-related properties from the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to obtain the properties from.
        /// </param>
        /// <param name="hostCreateFlags">
        /// Upon return, receives the host creation flags.
        /// </param>
        /// <param name="host">
        /// Upon return, receives the interpreter host.
        /// </param>
        /// <param name="profile">
        /// Upon return, receives the host profile name.
        /// </param>
        /// <param name="cultureInfo">
        /// Upon return, receives the interpreter culture.
        /// </param>
        /// <param name="resourceManager">
        /// Upon return, receives the interpreter resource manager.
        /// </param>
        /// <param name="binder">
        /// Upon return, receives the interpreter binder.
        /// </param>
        private static void GetProperties(
            Interpreter interpreter,             /* in */
            out HostCreateFlags hostCreateFlags, /* out */
            out IHost host,                      /* out */
            out string profile,                  /* out */
            out CultureInfo cultureInfo,         /* out */
            out ResourceManager resourceManager, /* out */
            out IBinder binder                   /* out */
            )
        {
            hostCreateFlags = interpreter.HostCreateFlags; /* throw */
            host = interpreter.InternalHost; /* throw */
            profile = (host != null) ? host.Profile : null; /* throw */
            cultureInfo = interpreter.InternalCultureInfo; /* throw */
            resourceManager = interpreter.ResourceManager; /* throw */
            binder = interpreter.InternalBinder; /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new host data container with the specified
        /// type name.
        /// </summary>
        /// <param name="typeName">
        /// The type name of the host the data is for.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created host data.
        /// </returns>
        public static IHostData NewData(
            string typeName,                /* in */
            HostCreateFlags hostCreateFlags /* in */
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, null, null,
                null, hostCreateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new host data container with the specified
        /// type name and interpreter.
        /// </summary>
        /// <param name="typeName">
        /// The type name of the host the data is for.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter the host data is for.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created host data.
        /// </returns>
        public static IHostData NewData(
            string typeName,                /* in */
            Interpreter interpreter,        /* in */
            HostCreateFlags hostCreateFlags /* in */
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, interpreter,
                null, null, hostCreateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new host data container with the specified
        /// type name, interpreter, resource manager, and profile.
        /// </summary>
        /// <param name="typeName">
        /// The type name of the host the data is for.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter the host data is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created host data.
        /// </returns>
        private static IHostData NewData(
            string typeName,                 /* in */
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags  /* in */
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, interpreter,
                resourceManager, profile, hostCreateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new custom host using the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The callback used to create the host.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// The newly created host, or null if it could not be created.
        /// </returns>
        public static IHost NewCustom(
            NewHostCallback callback,        /* in */
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags, /* in */
            ref Result error                 /* out */
            )
        {
            if (callback == null)
                return null;

            try
            {
                IHost host = callback(NewData(
                    null, interpreter, resourceManager, profile,
                    hostCreateFlags)); /* throw */

                if (host != null)
                {
                    //
                    // NOTE: Dynamic fixup.  Since this host was created
                    //       via the new host callback delegate, it will
                    //       [most likely] not have a valid type name;
                    //       therefore, attempt to see if host derives
                    //       from the core host and then check the type
                    //       name and fill it in now, if necessary.
                    //
                    _Hosts.Profile profileHost = host as _Hosts.Profile;

                    if ((profileHost != null) &&
                        (profileHost.TypeName == null))
                    {
                        Type hostType = AppDomainOps.MaybeGetType(
                            profileHost, typeof(_Hosts.Profile));

                        profileHost.TypeName = hostType.Name;
                    }
                }

                return host;
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// This method creates a new console host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created console host.
        /// </returns>
        public static IHost NewConsole(
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags  /* in */
            )
        {
            return new _Hosts.Console(NewData(
                typeof(_Hosts.Console).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new diagnostic host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created diagnostic host.
        /// </returns>
        public static IHost NewDiagnostic(
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags  /* in */
            )
        {
            return new _Hosts.Diagnostic(NewData(
                typeof(_Hosts.Diagnostic).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new fake host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created fake host.
        /// </returns>
        public static IHost NewFake(
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags  /* in */
            )
        {
            return new _Hosts.Fake(NewData(
                typeof(_Hosts.Fake).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new null host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <returns>
        /// The newly created null host.
        /// </returns>
        public static IHost NewNull(
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags  /* in */
            )
        {
            return new _Hosts.Null(NewData(
                typeof(_Hosts.Null).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new wrapper host around the specified base
        /// host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <param name="baseHost">
        /// The base host to wrap.
        /// </param>
        /// <param name="baseHostOwned">
        /// Non-zero if the wrapper host should take ownership of the base host.
        /// </param>
        /// <returns>
        /// The newly created wrapper host.
        /// </returns>
        private static _Hosts.Wrapper NewWrapper(
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags, /* in */
            IHost baseHost,                  /* in */
            bool baseHostOwned               /* in */
            )
        {
            return new _Hosts.Wrapper(NewData(
                typeof(_Hosts.Wrapper).Name, interpreter, resourceManager,
                profile, hostCreateFlags), baseHost, baseHostOwned);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wraps the specified base host in a wrapper host,
        /// disposing of the base host if wrapping fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter the host is for.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager the host should use.
        /// </param>
        /// <param name="profile">
        /// The profile name the host should use.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags to use.
        /// </param>
        /// <param name="baseHost">
        /// The base host to wrap.  This may be modified.
        /// </param>
        /// <param name="baseHostOwned">
        /// Non-zero if the base host is owned.  This may be modified.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        public static void WrapOrDispose(
            Interpreter interpreter,         /* in */
            ResourceManager resourceManager, /* in */
            string profile,                  /* in */
            HostCreateFlags hostCreateFlags, /* in */
            ref IHost baseHost,              /* in, out */
            ref bool baseHostOwned,          /* in, out */
            ref Result error                 /* out */
            )
        {
            _Hosts.Wrapper wrapperHost = NewWrapper(
                interpreter, resourceManager, profile, hostCreateFlags,
                baseHost, baseHostOwned);

            if (wrapperHost != null)
            {
                baseHost = wrapperHost;
                baseHostOwned = false;

                return;
            }

            if ((baseHost != null) && baseHostOwned)
            {
#if ISOLATED_PLUGINS
                /* IGNORED */
                AppDomainOps.MaybeClearIsolatedHost(interpreter);
#endif

                IDisposable disposable = baseHost as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose(); /* throw */
                    disposable = null;
                }

                baseHost = null;
                baseHostOwned = false;
            }

            error = "could not create wrapper host";
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new host of the specified type that copies and
        /// wraps the current interpreter host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose host is copied and wrapped.
        /// </param>
        /// <param name="type">
        /// The type of host to create.
        /// </param>
        /// <param name="host">
        /// Upon success, receives the newly created host.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CopyAndWrap(
            Interpreter interpreter, /* in */
            Type type,               /* in */
            ref IHost host,          /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            IHost newHost = null;
            object newObject = null;

            try
            {
                HostCreateFlags hostCreateFlags;
                IHost oldHost;
                string profile;
                CultureInfo cultureInfo;
                ResourceManager resourceManager;
                IBinder binder;

                GetProperties(
                    interpreter, out hostCreateFlags, out oldHost,
                    out profile, out cultureInfo, out resourceManager,
                    out binder);

                if (oldHost == null)
                {
                    error = "interpreter host not available";
                    return ReturnCode.Error;
                }

                BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                    MetaBindingFlags.ObjectDefault, true);

                Type[] typeArray = {
                    typeof(IHostData), typeof(IHost), typeof(bool)
                };

                TypeList types = new TypeList(typeArray);

                ConstructorInfo constructorInfo = type.GetConstructor(
                    bindingFlags, binder as Binder, typeArray, null); /* throw */

                if (constructorInfo == null)
                {
                    //
                    // BUGFIX: If the configured binder returns null (e.g.
                    //         due to it having the NoDefaultBinder flag,
                    //         etc), fallback to using the default binder
                    //         for the CLR instead.
                    //
                    constructorInfo = type.GetConstructor(
                        bindingFlags, null, typeArray, null); /* throw */
                }

                if (constructorInfo == null)
                {
                    error = String.Format(
                        "type \"{0}\" has no constructors matching " +
                        "parameter types \"{1}\" and binding flags \"{2}\"",
                        type.FullName, types, bindingFlags);

                    return ReturnCode.Error;
                }

                IHostData hostData = NewData(
                    type.Name, interpreter, resourceManager, profile,
                    hostCreateFlags);

                newObject = constructorInfo.Invoke(
                    bindingFlags, binder as Binder,
                    new object[] { hostData, oldHost, false }, cultureInfo);

                if (newObject != null)
                {
                    newHost = newObject as IHost;
                }
                else
                {
                    error = String.Format(
                        "could not create an instance of type \"{0}\"",
                        type.FullName);
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                //
                // NOTE: If we created an instance of the specified type and
                //       it cannot be used as an IHost, dispose of it now.
                //
                if ((newObject != null) && (newHost == null))
                {
                    ObjectOps.TryDisposeOrComplain<object>(
                        interpreter, ref newObject);

                    newObject = null;
                }
            }

            if (newHost != null)
            {
                host = newHost;
                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "type \"{0}\" mismatch, cannot convert to type \"{1}\"",
                    type.FullName, typeof(IHost));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unwraps and disposes of the interpreter wrapper host,
        /// restoring its base host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose wrapper host is unwrapped.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode UnwrapAndDispose(
            Interpreter interpreter, /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                IHost host = interpreter.InternalHost; /* throw */

                if (host == null)
                {
                    error = "interpreter host not available";
                    return ReturnCode.Error;
                }

                _Hosts.Wrapper wrapperHost = host as _Hosts.Wrapper;

                if (wrapperHost == null)
                {
                    error = String.Format(
                        NoFeatureError, typeof(_Hosts.Wrapper).Name);

                    return ReturnCode.Error;
                }

                IHost baseHost = wrapperHost.BaseHost; /* throw */
                bool baseHostOwned = wrapperHost.BaseHostOwned; /* throw */

                wrapperHost.Dispose(); /* throw */
                wrapperHost = null;

                interpreter.Host = baseHostOwned ? null : baseHost; /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Console Wrapper Methods
#if CONSOLE
        /// <summary>
        /// This method complains that a native console operation is not
        /// implemented, unless quiet operation is requested.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to complain on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void NotImplemented(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
            if (quiet)
                return;

            DebugOps.Complain(
                interpreter, ReturnCode.Error, "not implemented");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the native console, if supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void CloseNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                Result error = null;

                code = NativeConsole.Close(ref error);

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens or attaches the native console, if supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="forceConsole">
        /// Non-zero to force opening a new console.
        /// </param>
        /// <param name="attachConsole">
        /// Non-zero to attach to an existing parent console.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void OpenNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool forceConsole,       /* in */
            bool attachConsole,      /* in */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                bool? attached = null;
                Result error = null;

                code = NativeConsole.AttachOrOpen(
                    forceConsole, attachConsole, ref attached,
                    ref error);

                if ((code == ReturnCode.Ok) &&
                    NativeConsole.ShouldPreventClose(attached))
                {
                    code = NativeConsole.PreventClose(ref error);
                }

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method prevents the native console from being closed, if
        /// supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void NoCloseNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                Result error = null;

                code = NativeConsole.PreventClose(ref error);

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the native console input buffer size, if
        /// supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void FixNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                Result error = null;

                code = ConsoleOps.ResetInputBufferSize(ref error);

                if (code == ReturnCode.Ok)
                {
                    /* NO RESULT */
                    ResetAllInterpreterStandardChannels(
                        ChannelType.Input);
                }

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens the native console handles, if supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void HookNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                Result error = null;

                code = NativeConsole.MaybeOpenHandles(ref error);

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method switches the native console to a new active screen
        /// buffer, if supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void PushNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                Result error = null;

                code = NativeConsole.MaybeChangeToNewActiveScreenBuffer(
                    ref error);

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets up the native console history buffer, if supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void HistoryNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                Result error = null;

                code = NativeConsole.SetupHistory(
                    MinimumHistoryBufferSize, ref error);

                if ((code != ReturnCode.Ok) && !quiet)
                    DebugOps.Complain(interpreter, code, error);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables the use of the native console for output, if
        /// supported.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        private static void WriteNativeConsole(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                /* NO RESULT */
                SetUseNative(true);

                /* NO RESULT */
                ConsoleOps.SetUseNative(true);

                return;
            }
#endif

            NotImplemented(interpreter, quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the native console is open.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform the operation on behalf of, if any.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress complaining.
        /// </param>
        /// <returns>
        /// True if the native console is open; otherwise, false.
        /// </returns>
        public static bool IsNativeConsoleOpen(
            Interpreter interpreter, /* in: OPTIONAL */
            bool quiet               /* in */
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
                return NativeConsole.IsOpen();
#endif

            NotImplemented(interpreter, quiet);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs native console setup based on the specified
        /// host creation flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to perform setup on behalf of, if any.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags that control which setup steps are
        /// performed.
        /// </param>
        public static void SetupNativeConsole(
            Interpreter interpreter,        /* in: OPTIONAL */
            HostCreateFlags hostCreateFlags /* in */
            )
        {
            bool quiet = FlagOps.HasFlags(
                hostCreateFlags, HostCreateFlags.QuietConsole, true);

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.CloseConsole, true))
            {
                /* NO RESULT */
                CloseNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.OpenConsole, true))
            {
                /* NO RESULT */
                OpenNativeConsole(interpreter,
                    FlagOps.HasFlags(hostCreateFlags,
                        HostCreateFlags.ForceConsole, true),
                    FlagOps.HasFlags(hostCreateFlags,
                        HostCreateFlags.AttachConsole, true), quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.NoCloseConsole, true))
            {
                /* NO RESULT */
                NoCloseNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.FixConsole, true))
            {
                /* NO RESULT */
                FixNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.HookConsole, true))
            {
                /* NO RESULT */
                HookNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.PushConsole, true))
            {
                /* NO RESULT */
                PushNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.HistoryConsole, true))
            {
                /* NO RESULT */
                HistoryNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.WriteConsole, true))
            {
                /* NO RESULT */
                WriteNativeConsole(interpreter, quiet);
            }
        }
#endif
        #endregion
    }
}
