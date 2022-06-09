/*
 * DebuggerOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if DEBUGGER
using System;
using System.Text;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if DEBUGGER
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("438a61ad-907d-4089-a80c-c6d5e7edac05")]
    internal static class DebuggerOps
    {
        #region Private Constants
#if DEBUGGER
        private static readonly EngineFlags InteractiveEngineFlags =
            EngineFlags.NoBreakpoint | EngineFlags.Interactive;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debugger Interpreter Support Methods
#if DEBUGGER
        public static Interpreter CreateInterpreter(
            string culture,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            PluginFlags pluginFlags,
            AppDomain appDomain,
            IHost host,
            string libraryPath,
            StringList autoPathList
            )
        {
            Result result = null;

            Interpreter interpreter = CreateInterpreter(
                culture, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, interpreterFlags, pluginFlags, appDomain,
                host, libraryPath, autoPathList, ref result);

            if (interpreter == null)
                DebugOps.Complain(interpreter, ReturnCode.Error, result);

            return interpreter;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Interpreter CreateInterpreter(
            string culture,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            PluginFlags pluginFlags,
            AppDomain appDomain,
            IHost host,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            //
            // NOTE: First, mask off flags that we know to be invalid for all
            //       debugger interpreters.  Next, add flags to force a cloned
            //       interpreter host to be created and used.  Finally, create
            //       an isolated debugging interpreter with the right set of
            //       options.
            //
            createFlags &= ~CreateFlags.NonDebuggerUse;
            hostCreateFlags |= HostCreateFlags.Clone;

            return Interpreter.Create(
                culture, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, interpreterFlags, pluginFlags, appDomain,
                host, libraryPath, autoPathList, ref result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debugger Watchpoint Support Methods
#if DEBUGGER
        public static ReturnCode Watchpoint(
            IDebugger debugger,
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
            return Breakpoint(debugger, interpreter, loopData, ref result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debugger Breakpoint Support Methods
#if DEBUGGER
        public static bool MatchBreakpointTypes(
            BreakpointType flags,
            BreakpointType hasFlags,
            bool? enabled,
            bool verbose,
            StringBuilder builder
            )
        {
            bool result = false;

            if (hasFlags == BreakpointType.None)
            {
                result = (flags == BreakpointType.None);

                if ((verbose || (enabled == null)) && (builder != null))
                {
                    builder.AppendLine(String.Format(
                        "debugger types are {0}", result ? "all missing" :
                        "present"));
                }

                return result;
            }

            bool all = (enabled == null) || (bool)enabled;

            result = FlagOps.HasFlags(flags, hasFlags, all);

            if ((verbose || (enabled == null)) && (builder != null))
            {
                builder.AppendLine(String.Format(
                    "debugger types are {0}", result ? String.Format(
                    "{0}present", all ? String.Empty : "all ") :
                    String.Format("{0}missing", all ? String.Empty :
                    "all ")));
            }

            return all ? result : !result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Breakpoint(
            IDebugger debugger,
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (!interpreter.InternalInteractive)
            {
                result = "cannot break into interactive loop";
                return ReturnCode.Error;
            }

            if (debugger != null)
            {
                /* IGNORED */
                debugger.EnterLoop();
            }

            try
            {
                ReturnCode code;

                InteractiveLoopCallback interactiveLoopCallback =
                    interpreter.InteractiveLoopCallback;

                if (interactiveLoopCallback != null)
                {
                    code = interactiveLoopCallback(
                        interpreter, new InteractiveLoopData(loopData, true),
                        ref result);
                }
                else
                {
#if SHELL
                    //
                    // NOTE: This is the only place in the debugger subsystem
                    //       where the InteractiveLoop method may be called.
                    //       All other methods in the Debugger class and/or
                    //       any external classes that desire the interactive
                    //       debugging functionality should call this method.
                    //
                    code = Interpreter.InteractiveLoop(
                        interpreter, new InteractiveLoopData(loopData, true),
                        ref result);
#else
                    result = "not implemented";
                    code = ReturnCode.Error;
#endif
                }

                //
                // NOTE: Only check (or update) the interpreter state at this
                //       point if the interpreter is still usable (i.e. it is
                //       not disposed) -AND- the interactive loop returned a
                //       successful result.
                //
                if ((code == ReturnCode.Ok) && Engine.IsUsableNoLock(interpreter))
                {
                    //
                    // NOTE: Upon exiting the interactive loop, temporarily
                    //       prevent the engine from checking interpreter
                    //       readiness.  This is used to avoid potentially
                    //       breaking back into the interactive loop due to
                    //       breakpoints caused by script cancellation, etc.
                    //
                    interpreter.IsDebuggerExiting = true;

                    //
                    // BUGFIX: In case interactive user uses [exit], et al,
                    //         that invalidates the readiness state of the
                    //         interpreter, it should be re-checked here.
                    //
                    code = Interpreter.EngineReady(
                        interpreter, ReadyFlags.ViaDebugger, ref result);
                }

                return code;
            }
            finally
            {
                if (debugger != null)
                {
                    /* IGNORED */
                    debugger.ExitLoop();
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debugger General Support Methods
        public static bool GetBreakOnCancel(
            Interpreter interpreter,
            bool ignoreEnabled
            )
        {
#if DEBUGGER
            if (interpreter != null)
            {
                IDebugger debugger = interpreter.Debugger;

                if ((debugger != null) &&
                    (ignoreEnabled || debugger.Enabled))
                {
                    return debugger.BreakOnCancel;
                }
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetCommand(
            Interpreter interpreter,
            bool ignoreEnabled
            )
        {
            string result = null;

#if DEBUGGER
            if (interpreter != null)
            {
                IDebugger debugger = interpreter.Debugger;

                if ((debugger != null) &&
                    (ignoreEnabled || debugger.Enabled))
                {
                    //
                    // NOTE: Enforce "one-time" semantics.
                    //
                    result = debugger.Command;

                    if (result != null)
                    {
                        debugger.Command = null;
                    }
                    else
                    {
                        //
                        // NOTE: *NEW* Fallback to looking in the debugger
                        //       command queue for interactive commands.
                        //
                        QueueList<string, string> queue = debugger.Queue;

                        if ((queue != null) && (queue.Count > 0))
                            result = queue.Dequeue();
                    }
                }
            }
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static HeaderFlags GetHeaderFlags(
            IInteractiveHost interactiveHost,
            HeaderFlags headerFlags,
            bool debug,
            bool show,
            bool empty,
            bool @default
            )
        {
            //
            // NOTE: If we are in debug mode and no header display flags have
            //       been explicitly set for the interpreter, initialize them
            //       to the default value.
            //
            if (@default && FlagOps.HasFlags(
                    headerFlags, HeaderFlags.Invalid, true))
            {
                //
                // NOTE: Remove the "these flags have not been setup before"
                //       indicator flag.
                //
                headerFlags &= ~HeaderFlags.Invalid;

                //
                // NOTE: Add the default header flags for the interactive
                //       host.  If the interactive host is not available,
                //       fallback on the system default header flags.
                //
                HeaderFlags defaultHeaderFlags = HeaderFlags.Default;

                if (interactiveHost != null)
                {
                    headerFlags |= HostOps.GetHeaderFlags(
                        interactiveHost, defaultHeaderFlags);
                }
                else
                {
                    headerFlags |= defaultHeaderFlags;
                }
            }

            //
            // NOTE: Only modify (set or unset) the active debugger flag if we
            //       have been told to do so; otherwise, the active debugger
            //       flag may have been manually changed and should be left
            //       alone.
            //
            if (show)
            {
                //
                // NOTE: Is there an active debugger?
                //
                if (debug)
                {
                    //
                    // NOTE: Set the active debugger flag.
                    //
                    headerFlags |= HeaderFlags.Debug;
                }
                else
                {
                    //
                    // NOTE: Unset the active debugger flag.
                    //
                    headerFlags &= ~HeaderFlags.Debug;
                }
            }

            //
            // NOTE: Show empty content?
            //
            if (empty)
                headerFlags |= HeaderFlags.EmptyContent;

            return headerFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static EngineFlags GetEngineFlags(
            bool debug
            )
        {
#if DEBUGGER
            if (debug)
                return InteractiveEngineFlags;
#endif

            return EngineFlags.Interactive;
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        public static bool CanHitBreakpoints(
            Interpreter interpreter,
            EngineFlags engineFlags,
            BreakpointType breakpointType
            )
        {
            if ((interpreter == null) || interpreter.Disposed)
                return false;

            if (EngineFlagOps.HasNoBreakpoint(engineFlags))
                return false;

            return interpreter.CanHitBreakpoints(breakpointType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetLibraryPath(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.LibraryPath;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetAutoPathList(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.AutoPathList;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IDebugger Create(
            bool isolated,
            string culture,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            PluginFlags pluginFlags,
            AppDomain appDomain,
            IHost host,
            string libraryPath,
            StringList autoPathList
            )
        {
            return new Debugger(
                isolated, culture, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags,
                pluginFlags, appDomain, host, libraryPath,
                autoPathList);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool SetResult(
            Interpreter interpreter,
            ReturnCode code,
            Result result,
            bool ignoreEnabled
            )
        {
#if DEBUGGER
            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (!interpreter.Disposed)
                    {
                        IDebugger debugger = interpreter.Debugger;

                        if ((debugger != null) && !debugger.Disposed &&
                            (ignoreEnabled || debugger.Enabled))
                        {
                            //
                            // NOTE: Enforce "copy" semantics.
                            //
                            debugger.Result = Result.Copy(result,
                                code, ResultFlags.CopyObject); /* COPY */

                            return true;
                        }
                    }
                }
            }
#endif

            return false;
        }
        #endregion
    }
}
