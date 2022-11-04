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

#if SHELL
using System.Text;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using ResourcePair = Eagle._Components.Public.AnyPair<
    string, System.Resources.ResourceManager>;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("1b0d1e7d-957b-4151-b31f-598393251442")]
    internal static class HostOps
    {
        #region Private Constants
        #region Interactive Prompt Defaults
        private const string PrimaryPrompt = "% ";
        private const string ContinuePrompt = ">\t";

        ///////////////////////////////////////////////////////////////////////

        private const string DebugPrefix = "(debug) ";
        private const string QueuePrefix = "^ ";

        ///////////////////////////////////////////////////////////////////////

        private static readonly StringList DefaultPrompts = new StringList(
            new string[] { null, PrimaryPrompt, ContinuePrompt }
        );

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string PromptWithIdFormat = "i:{0} {1}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Host Timeouts
        //
        // HACK: This is not read-only.
        //
        private static int GetTimeout = 2000; /* TODO: Good default? */

        //
        // HACK: This is not read-only.
        //
        private static int InteractiveGetTimeout = 2000; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Mode Formatting
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
        private static ConsoleColor highContrastLightColor = ConsoleColor.White;
        private static ConsoleColor highContrastDarkColor = ConsoleColor.Black;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly string NoFeatureError =
            "interpreter host lacks support for the \"{0}\" feature";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Support Methods
        private static IHost TryGet(
            Interpreter interpreter
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
                        TraceOps.DebugTrace(
                            "TryGet: could not lock interpreter",
                            typeof(HostOps).Name, TracePriority.LockError);

                        int timeout = GetTimeout; /* NO-LOCK */

                        if (timeout >= 0)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryGet: retry in {0} milliseconds",
                                timeout), typeof(HostOps).Name,
                                TracePriority.HostDebug);

                            interpreter.InternalTryLock(
                                timeout, ref locked); /* TRANSACTIONAL */
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

        private static IInteractiveHost TryGetInteractive(
            Interpreter interpreter
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
                        TraceOps.DebugTrace(
                            "TryGetInteractive: could not lock interpreter",
                            typeof(HostOps).Name, TracePriority.LockError);

                        int timeout = InteractiveGetTimeout; /* NO-LOCK */

                        if (timeout >= 0)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryGetInteractive: retry in {0} milliseconds",
                                timeout), typeof(HostOps).Name,
                                TracePriority.HostDebug);

                            interpreter.InternalTryLock(
                                timeout, ref locked); /* TRANSACTIONAL */
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
                            interactiveHost = interpreter.GetInteractiveHost();
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

        public static int TryGetInteractiveLoops(
            Interpreter interpreter
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

        public static string GetDefaultPrompt(
            PromptType type,
            PromptFlags flags,
            long id
            )
        {
            string result = null;

            if (((int)type >= 0) && ((int)type < DefaultPrompts.Count))
            {
                result = DefaultPrompts[(int)type];

                if ((result != null) &&
                    FlagOps.HasFlags(flags, PromptFlags.Queue, true))
                {
                    result = QueuePrefix + result;
                }

                if ((result != null) &&
                    FlagOps.HasFlags(flags, PromptFlags.Debug, true))
                {
                    result = DebugPrefix + result;
                }

                if ((result != null) &&
                    FlagOps.HasFlags(flags, PromptFlags.Interpreter, true))
                {
                    result = String.Format(PromptWithIdFormat, id, result);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetInteractiveMode(
            Interpreter interpreter
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

        public static bool HasEmptyContent(
            DetailFlags detailFlags
            )
        {
            return FlagOps.HasFlags(
                detailFlags, DetailFlags.EmptyContent, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasVerboseContent(
            DetailFlags detailFlags
            )
        {
            return FlagOps.HasFlags(
                detailFlags, DetailFlags.VerboseContent, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static DetailFlags GetDetailFlags(
            Interpreter interpreter
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

        public static void HeaderFlagsToDetailFlags(
            HeaderFlags headerFlags,
            ref DetailFlags detailFlags
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
        public static bool BuildInterpreterInfoList(
            Interpreter interpreter,
            string name,
            DetailFlags detailFlags,
            ref StringPairList list
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
        public static ReturnCode ThreadSleepOrMaybeComplain(
            int milliseconds,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            ReturnCode code;
            Result error = null;

            code = ThreadSleep(milliseconds, ref error);

            if (!noComplain && (code != ReturnCode.Ok))
            {
#if DEBUG && VERBOSE
                DebugOps.Complain(code, error);
#else
                TraceOps.DebugTrace(String.Format(
                    "ThreadSleepOrMaybeComplain: code = {0}, error = {1}",
                    code, FormatOps.WrapOrNull(error)), typeof(HostOps).Name,
                    TracePriority.ThreadError);
#endif
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ThreadSleep(
            int milliseconds
            ) /* THREAD-SAFE */
        {
            Exception exception = null;
            Result error = null;

            return ThreadSleep(milliseconds, ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ThreadSleep(
            int milliseconds,
            ref Result error
            ) /* THREAD-SAFE */
        {
            Exception exception = null;

            return ThreadSleep(milliseconds, ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ThreadSleep(
            int milliseconds,
            ref Exception exception
            ) /* THREAD-SAFE */
        {
            Result error = null;

            return ThreadSleep(milliseconds, ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ThreadSleep(
            int milliseconds,
            ref Exception exception,
            ref Result error
            ) /* THREAD-SAFE */
        {
            try
            {
                Thread.Sleep(milliseconds);

                return ReturnCode.Ok;
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.ThreadError2);

                exception = e;
                error = e;
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.ThreadError2);

                exception = e;
                error = e;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.ThreadError);

                exception = e;
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Yield Support Methods
        public static ReturnCode YieldOrMaybeComplain() /* THREAD-SAFE */
        {
            ReturnCode code;
            Result error = null;

            code = Yield(ref error);

            if (code != ReturnCode.Ok)
            {
#if DEBUG && VERBOSE
                DebugOps.Complain(code, error);
#else
                TraceOps.DebugTrace(String.Format(
                    "YieldOrMaybeComplain: code = {0}, error = {1}", code,
                    FormatOps.WrapOrNull(error)), typeof(HostOps).Name,
                    TracePriority.ThreadError);
#endif
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Yield(
            ref Result error
            ) /* THREAD-SAFE */
        {
            Exception exception = null;

            return Yield(ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Yield(
            ref Exception exception,
            ref Result error
            ) /* THREAD-SAFE */
        {
            try
            {
#if NET_40
                Thread.Yield(); /* NOTE: .NET Framework 4.0+ only. */
#else
                Thread.Sleep(0);
#endif

                return ReturnCode.Ok;
            }
#if !NET_40
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.ThreadError2);

                exception = e;
                error = e;
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.ThreadError2);

                exception = e;
                error = e;
            }
#endif
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.ThreadError);

                exception = e;
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods for [host screen] Sub-Command
        //
        // BUGBUG: This only works for interpreters that are known from this
        //         AppDomain.
        //
        public static void ResetAllInterpreterStandardInputChannels()
        {
            IEnumerable<Interpreter> interpreters = GlobalState.GetInterpreters();

            if (interpreters == null)
                return;

            foreach (Interpreter interpreter in interpreters)
            {
                if (interpreter == null)
                    continue;

                ReturnCode code;
                Result error = null;

                code = interpreter.ModifyStandardChannels(
                    null, null, ChannelType.Input | ChannelType.AllowExist |
                    ChannelType.UseCurrent | ChannelType.UseHost, ref error);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: This only works for interpreters that are known from this
        //         AppDomain.
        //
        public static void ResetAllInterpreterStandardOutputChannels()
        {
            IEnumerable<Interpreter> interpreters = GlobalState.GetInterpreters();

            if (interpreters == null)
                return;

            foreach (Interpreter interpreter in interpreters)
            {
                if (interpreter == null)
                    continue;

                ReturnCode code;
                Result error = null;

                code = interpreter.ModifyStandardChannels(
                    null, null, ChannelType.Output | ChannelType.Error |
                    ChannelType.AllowExist | ChannelType.UseCurrent |
                    ChannelType.UseHost, ref error);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, error);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Wrapper Methods
        #region Exit Support Methods
        public static void SetExiting(
            Interpreter interpreter,
            bool exiting
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

        public static void SetExiting(
            Interpreter interpreter,
            IProcessHost processHost,
            string hostName,
            bool isolated,
            bool exiting
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
        public static ReturnCode Sleep(
            Interpreter interpreter,
            int milliseconds,
            ref Result error
            ) /* THREAD-SAFE */
        {
            return Sleep(
                TryGet(interpreter), milliseconds, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Sleep(
            IThreadHost threadHost,
            int milliseconds,
            ref Result error
            ) /* THREAD-SAFE */
        {
            return Sleep(
                threadHost, milliseconds, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Sleep(
            IThreadHost threadHost,
            int milliseconds,
            bool strict,
            ref Result error
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
        public static ReturnCode YieldOrMaybeComplain(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            ReturnCode code;
            Result error = null;

            code = Yield(TryGet(interpreter), false, ref error);

            if (code != ReturnCode.Ok)
            {
#if DEBUG && VERBOSE
                DebugOps.Complain(interpreter, code, error);
#else
                TraceOps.DebugTrace(String.Format(
                    "YieldOrMaybeComplain: code = {0}, error = {1}", code,
                    FormatOps.WrapOrNull(error)), typeof(HostOps).Name,
                    TracePriority.ThreadError);
#endif
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Yield(
            IThreadHost threadHost,
            bool strict,
            ref Result error
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
                        return Yield(ref error);
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
                return Yield(ref error);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Support Methods
        public static bool WriteLine(
            IInteractiveHost interactiveHost,
            string value
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

        public static bool WriteResultLine(
            IInteractiveHost interactiveHost,
            ReturnCode code,
            Result result
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

        public static void WriteOrConsole(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
                interactiveHost.Write(value);
#if CONSOLE
            else
                Console.Write(value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteLineOrConsole(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
                interactiveHost.WriteLine(value);
#if CONSOLE
            else
                Console.WriteLine(value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteConsoleOrComplain(
            ReturnCode code,
            Result result
            )
        {
            WriteConsoleOrComplain(code, result, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteConsoleOrComplain(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
#if CONSOLE
            try
            {
                Console.WriteLine(ResultOps.Format(code, result, errorLine));
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
        private static bool IsNullType(
            Type type
            )
        {
            return (type != null) &&
                ((type == typeof(_Hosts.Null)) ||
                type.IsSubclassOf(typeof(_Hosts.Null)));
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static void EmitTrace(
            string prefix,
            Interpreter interpreter,
            IInteractiveHost interactiveHost,
            int interactiveLoops,
            TracePriority? priority
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

        public static bool IsOpen(
            Interpreter interpreter,
            bool? refresh,
            ref HostFlags hostFlags,
            ref IInteractiveHost interactiveHost
            )
        {
            bool localRefresh;
            HostFlags localHostFlags;
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
                localInteractiveHost = TryGetInteractive(interpreter);

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
                localInteractiveHost = TryGetInteractive(interpreter);

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
                    interactiveHost = localInteractiveHost;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static HeaderFlags GetHeaderFlags(
            IInteractiveHost interactiveHost,
            HeaderFlags @default
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

        public static HostFlags GetHostFlags(
            IInteractiveHost interactiveHost
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

        public static bool IsInputRedirected(
            IInteractiveHost interactiveHost
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

        public static int GetReadLevels(
            IInteractiveHost interactiveHost
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

        public static ConsoleColor GetHighContrastColor(
            ConsoleColor color
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

        public static ReturnCode GetColors(
            IColorHost colorHost,
            string name,
            bool foreground,
            bool background,
            bool strict,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
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
        public static bool SetTitle(
            IInteractiveHost interactiveHost,
            string value
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

        public static ReturnCode LoadProfile(
            Interpreter interpreter,
            IInteractiveHost interactiveHost,
            string profile,
            Encoding encoding,
            ref Result error
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
        public static bool HasNoHost(
            ScriptFlags scriptFlags,
            ref Result error
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

        public static ReturnCode GetScript(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string name,
            bool direct,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            )
        {
            if (HasNoHost(scriptFlags, ref result))
                return ReturnCode.Error;

            if (fileSystemHost != null)
            {
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

                                    DataFlags dataFlags = DataFlags.Script;

                                    EngineFlags engineFlags =
                                        fileHost.GetEngineFlagsForReadScriptStream(
                                            interpreter, dataFlags, scriptFlags);

                                    ResultList errors = null;

                                    if (fileHost.GetDataViaResourceManager(
                                            interpreter, name, anyPair,
                                            uniqueResourceNames, engineFlags,
                                            dataFlags, false, false, null,
                                            ref scriptFlags, ref clientData,
                                            ref result, ref errors) == ReturnCode.Ok)
                                    {
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
                                return isolatedFileSystemHost.GetData(
                                    name, DataFlags.Script, ref scriptFlags,
                                    ref clientData, ref result); /* throw */
                            }
                        }
                    }
#endif

                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Data, true))
                    {
                        return fileSystemHost.GetData(
                            name, DataFlags.Script, ref scriptFlags,
                            ref clientData, ref result); /* throw */
                    }
                    else
                    {
                        result = "interpreter host does not have script support";
                    }
                }
                catch (Exception e)
                {
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
        public static ReturnCode GetStream(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            bool strict,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
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

        #region Class Factory Methods
        public static HostCreateFlags GetCreateFlags(
            HostCreateFlags hostCreateFlags,
            bool useAttach,
            bool noColor,
            bool noTitle,
            bool noIcon,
            bool noProfile,
            bool noCancel
            )
        {
            HostCreateFlags result = Defaults.HostCreateFlags;

            if (useAttach)
                result |= HostCreateFlags.UseAttach;
            else
                result &= ~HostCreateFlags.UseAttach;

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

        private static void GetProperties(
            Interpreter interpreter,
            out HostCreateFlags hostCreateFlags,
            out IHost host,
            out string profile,
            out CultureInfo cultureInfo,
            out ResourceManager resourceManager,
            out IBinder binder
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

        public static IHostData NewData(
            string typeName,
            HostCreateFlags hostCreateFlags
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, null, null,
                null, hostCreateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IHostData NewData(
            string typeName,
            Interpreter interpreter,
            HostCreateFlags hostCreateFlags
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, interpreter,
                null, null, hostCreateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IHostData NewData(
            string typeName,
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, interpreter,
                resourceManager, profile, hostCreateFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IHost NewCustom(
            NewHostCallback callback,
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags,
            ref Result error
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
        public static IHost NewConsole(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags
            )
        {
            return new _Hosts.Console(NewData(
                typeof(_Hosts.Console).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static IHost NewDiagnostic(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags
            )
        {
            return new _Hosts.Diagnostic(NewData(
                typeof(_Hosts.Diagnostic).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        public static IHost NewFake(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags
            )
        {
            return new _Hosts.Fake(NewData(
                typeof(_Hosts.Fake).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        public static IHost NewNull(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags
            )
        {
            return new _Hosts.Null(NewData(
                typeof(_Hosts.Null).Name, interpreter, resourceManager,
                profile, hostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        private static _Hosts.Wrapper NewWrapper(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags,
            IHost baseHost,
            bool baseHostOwned
            )
        {
            return new _Hosts.Wrapper(NewData(
                typeof(_Hosts.Wrapper).Name, interpreter, resourceManager,
                profile, hostCreateFlags), baseHost, baseHostOwned);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode CopyAndWrap(
            Interpreter interpreter,
            Type type,
            ref IHost host,
            ref Result error
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

        public static ReturnCode UnwrapAndDispose(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                IHost host = interpreter.Host; /* throw */

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
        private static void CloseNativeConsole(
            Interpreter interpreter,
            bool quiet
            )
        {
            ReturnCode consoleCode;
            Result consoleError = null;

#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                consoleCode = NativeConsole.Close(ref consoleError);
            }
            else
            {
                consoleError = "not implemented";
                consoleCode = ReturnCode.Error;
            }
#else
            consoleError = "not implemented";
            consoleCode = ReturnCode.Error;
#endif

            if (!quiet && (consoleCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, consoleCode, consoleError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void OpenNativeConsole(
            Interpreter interpreter,
            bool forceConsole,
            bool attachConsole,
            bool quiet
            )
        {
            ReturnCode consoleCode;
            Result consoleError = null;

#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                bool? attached = null;

                consoleCode = NativeConsole.AttachOrOpen(
                    forceConsole, attachConsole, ref attached,
                    ref consoleError);

                if ((consoleCode == ReturnCode.Ok) &&
                    NativeConsole.ShouldPreventClose(attached))
                {
                    consoleCode = NativeConsole.PreventClose(
                        ref consoleError);
                }
            }
            else
            {
                consoleError = "not implemented";
                consoleCode = ReturnCode.Error;
            }
#else
            consoleError = "not implemented";
            consoleCode = ReturnCode.Error;
#endif

            if (!quiet && (consoleCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, consoleCode, consoleError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void NoCloseNativeConsole(
            Interpreter interpreter,
            bool quiet
            )
        {
            ReturnCode consoleCode;
            Result consoleError = null;

#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                consoleCode = NativeConsole.PreventClose(
                    ref consoleError);
            }
            else
            {
                consoleError = "not implemented";
                consoleCode = ReturnCode.Error;
            }
#else
            consoleError = "not implemented";
            consoleCode = ReturnCode.Error;
#endif

            if (!quiet && (consoleCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, consoleCode, consoleError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void FixNativeConsole(
            Interpreter interpreter,
            bool quiet
            )
        {
            ReturnCode consoleCode;
            Result consoleError = null;

#if NATIVE && WINDOWS
            consoleCode = ConsoleOps.ResetInputBufferSize(
                ref consoleError);

            if (consoleCode == ReturnCode.Ok)
                ResetAllInterpreterStandardInputChannels();
#else
            consoleError = "not implemented";
            consoleCode = ReturnCode.Error;
#endif

            if (!quiet && (consoleCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, consoleCode, consoleError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void HookNativeConsole(
            Interpreter interpreter,
            bool quiet
            )
        {
            ReturnCode consoleCode;
            Result consoleError = null;

#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                consoleCode = NativeConsole.MaybeOpenHandles(
                    ref consoleError);
            }
            else
            {
                consoleError = "not implemented";
                consoleCode = ReturnCode.Error;
            }
#else
            consoleError = "not implemented";
            consoleCode = ReturnCode.Error;
#endif

            if (!quiet && (consoleCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, consoleCode, consoleError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void PushNativeConsole(
            Interpreter interpreter,
            bool quiet
            )
        {
            ReturnCode consoleCode;
            Result consoleError = null;

#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                consoleCode = NativeConsole.MaybeChangeToNewActiveScreenBuffer(
                    ref consoleError);
            }
            else
            {
                consoleError = "not implemented";
                consoleCode = ReturnCode.Error;
            }
#else
            consoleError = "not implemented";
            consoleCode = ReturnCode.Error;
#endif

            if (!quiet && (consoleCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, consoleCode, consoleError);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetupNativeConsole(
            Interpreter interpreter,
            HostCreateFlags hostCreateFlags
            )
        {
            bool quiet = FlagOps.HasFlags(
                hostCreateFlags, HostCreateFlags.QuietConsole, true);

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.CloseConsole, true))
            {
                CloseNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.OpenConsole, true))
            {
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
                NoCloseNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.FixConsole, true))
            {
                FixNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.HookConsole, true))
            {
                HookNativeConsole(interpreter, quiet);
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    hostCreateFlags, HostCreateFlags.PushConsole, true))
            {
                PushNativeConsole(interpreter, quiet);
            }
        }
#endif
        #endregion
    }
}
