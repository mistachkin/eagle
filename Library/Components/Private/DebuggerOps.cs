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
    /// <summary>
    /// This class provides the collection of static helper methods that support
    /// the Eagle script debugger, including creation of debugger interpreters,
    /// breakpoint and watchpoint handling, command queue access, and header and
    /// detail flag management.
    /// </summary>
    [ObjectId("438a61ad-907d-4089-a80c-c6d5e7edac05")]
    internal static class DebuggerOps
    {
        #region Private Constants
#if DEBUGGER
        /// <summary>
        /// The engine flags used when entering the interactive loop for an
        /// active debugger; these mask off the debugger-related flags and enable
        /// interactive mode.
        /// </summary>
        private static readonly EngineFlags InteractiveEngineFlags =
            EngineFlags.NoDebuggerMask | EngineFlags.Interactive;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debugger Interpreter Support Methods
#if DEBUGGER
        /// <summary>
        /// This method creates a new isolated debugger interpreter using the
        /// specified options, complaining if the creation fails.
        /// </summary>
        /// <param name="culture">
        /// The culture to use for the new interpreter, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags that control how the new interpreter is created.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the interpreter host is created.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags that control how the new interpreter is initialized.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags that control how scripts are located and loaded by the new
        /// interpreter.
        /// </param>
        /// <param name="interpreterFlags">
        /// The miscellaneous flags that control the behavior of the new
        /// interpreter.
        /// </param>
        /// <param name="pluginFlags">
        /// The flags that control how plugins are loaded by the new interpreter.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which the new interpreter should be created,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="host">
        /// The host to use (or clone) for the new interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="libraryPath">
        /// The script library path for the new interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of automatic package search paths for the new interpreter,
        /// if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created interpreter, or null if creation failed.
        /// </returns>
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

        /// <summary>
        /// This method creates a new isolated debugger interpreter using the
        /// specified options, returning any error message through the supplied
        /// result.
        /// </summary>
        /// <param name="culture">
        /// The culture to use for the new interpreter, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags that control how the new interpreter is created.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the interpreter host is created.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags that control how the new interpreter is initialized.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags that control how scripts are located and loaded by the new
        /// interpreter.
        /// </param>
        /// <param name="interpreterFlags">
        /// The miscellaneous flags that control the behavior of the new
        /// interpreter.
        /// </param>
        /// <param name="pluginFlags">
        /// The flags that control how plugins are loaded by the new interpreter.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which the new interpreter should be created,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="host">
        /// The host to use (or clone) for the new interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="libraryPath">
        /// The script library path for the new interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of automatic package search paths for the new interpreter,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, receives an error message describing why the
        /// interpreter could not be created.
        /// </param>
        /// <returns>
        /// The newly created interpreter, or null if creation failed.
        /// </returns>
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
        /// <summary>
        /// This method handles a variable watchpoint by breaking into the
        /// interactive debugger loop.
        /// </summary>
        /// <param name="debugger">
        /// The debugger associated with the interpreter, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that hit the watchpoint.
        /// </param>
        /// <param name="loopData">
        /// The interactive loop data describing the context of the watchpoint.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result produced by the interactive loop.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified breakpoint types match
        /// the required breakpoint types, optionally appending a human-readable
        /// description of the comparison.
        /// </summary>
        /// <param name="flags">
        /// The breakpoint types that are currently present.
        /// </param>
        /// <param name="hasFlags">
        /// The breakpoint types that are required for a match.
        /// </param>
        /// <param name="enabled">
        /// When non-null, indicates whether all of the required types must be
        /// present (true) or any of them (false); when null, all of the required
        /// types must be present and verbose output is forced.  This parameter
        /// may be null.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to append a description of the comparison to
        /// <paramref name="builder" />.
        /// </param>
        /// <param name="builder">
        /// The string builder that receives the optional description, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the breakpoint types match the required types; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method handles a breakpoint by entering the interactive
        /// debugger loop for the specified interpreter, invoking the configured
        /// interactive loop callback when present.
        /// </summary>
        /// <param name="debugger">
        /// The debugger associated with the interpreter, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that hit the breakpoint.
        /// </param>
        /// <param name="loopData">
        /// The interactive loop data describing the context of the breakpoint.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result produced by the interactive loop, or
        /// an error message upon failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                        interpreter, null, ReadyFlags.ViaDebugger, ref result);
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
        /// <summary>
        /// This method queries whether the debugger associated with the
        /// specified interpreter is configured to break into the interactive
        /// loop when a script is canceled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debugger should be queried, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ignoreEnabled">
        /// Non-zero to query the setting even when the debugger is not currently
        /// enabled.
        /// </param>
        /// <returns>
        /// True if the debugger should break on cancellation; otherwise, false.
        /// </returns>
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

#if DEBUGGER
        /// <summary>
        /// This method appends the queued and pending interactive commands from
        /// the debugger associated with the specified interpreter to the
        /// supplied command list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debugger commands should be dumped, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="ignoreEnabled">
        /// Non-zero to dump the commands even when the debugger is not currently
        /// enabled.
        /// </param>
        /// <param name="commands">
        /// The command list to append the queued and pending commands to; it is
        /// created when null and commands are available.
        /// </param>
        public static void DumpCommands(
            Interpreter interpreter, /* in */
            bool ignoreEnabled,      /* in */
            ref StringList commands  /* in, out */
            )
        {
            if (interpreter != null)
            {
                IDebugger debugger = interpreter.Debugger;

                if ((debugger != null) &&
                    (ignoreEnabled || debugger.Enabled))
                {
                    Result result = null;

                    if ((debugger.DumpCommands(
                            ref result) == ReturnCode.Ok) &&
                        (result != null))
                    {
                        StringList list = result.Value as StringList;

                        if (list != null)
                        {
                            if (commands == null)
                                commands = new StringList();

                            commands.Add("queue");
                            commands.Add(list.ToString());
                        }
                    }

                    string command = debugger.Command;

                    if (command != null)
                    {
                        if (commands == null)
                            commands = new StringList();

                        commands.Add("command");
                        commands.Add(command);
                    }
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the next interactive command for the debugger
        /// associated with the specified interpreter, enforcing one-time
        /// semantics and falling back to the debugger command queue.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debugger command should be retrieved, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="ignoreEnabled">
        /// Non-zero to retrieve the command even when the debugger is not
        /// currently enabled.
        /// </param>
        /// <returns>
        /// The next interactive command, or null if none is available.
        /// </returns>
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

                        if ((queue != null) && !queue.IsEmpty)
                            result = queue.Dequeue();
                    }
                }
            }
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the effective interactive header display flags,
        /// applying the default flags when requested, toggling the active
        /// debugger flag, and optionally enabling empty content display.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host used to obtain the default header flags, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="headerFlags">
        /// The current header flags to start from.
        /// </param>
        /// <param name="debug">
        /// Non-zero if a debugger is currently active.
        /// </param>
        /// <param name="show">
        /// Non-zero to set or unset the active debugger flag based on
        /// <paramref name="debug" />.
        /// </param>
        /// <param name="empty">
        /// Non-zero to enable display of empty content.
        /// </param>
        /// <param name="default">
        /// Non-zero to initialize the header flags to their default value when
        /// they have not yet been set up.
        /// </param>
        /// <returns>
        /// The resulting header display flags.
        /// </returns>
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

        /// <summary>
        /// This method computes the effective interactive detail display flags,
        /// applying the default flags when requested, toggling the active
        /// debugger flag, and optionally enabling empty content display.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host used to obtain the default detail flags, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="detailFlags">
        /// The current detail flags to start from.
        /// </param>
        /// <param name="debug">
        /// Non-zero if a debugger is currently active.
        /// </param>
        /// <param name="show">
        /// Non-zero to set or unset the active debugger flag based on
        /// <paramref name="debug" />.
        /// </param>
        /// <param name="empty">
        /// Non-zero to enable display of empty content.
        /// </param>
        /// <param name="default">
        /// Non-zero to initialize the detail flags to their default value when
        /// they have not yet been set up.
        /// </param>
        /// <returns>
        /// The resulting detail display flags.
        /// </returns>
        public static DetailFlags GetDetailFlags(
            IInteractiveHost interactiveHost,
            DetailFlags detailFlags,
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
                    detailFlags, DetailFlags.Invalid, true))
            {
                //
                // NOTE: Remove the "these flags have not been setup before"
                //       indicator flag.
                //
                detailFlags &= ~DetailFlags.Invalid;

                //
                // NOTE: Add the default header flags for the interactive
                //       host.  If the interactive host is not available,
                //       fallback on the system default header flags.
                //
                DetailFlags defaultDetailFlags = DetailFlags.Default;

                if (interactiveHost != null)
                {
                    detailFlags |= HostOps.GetDetailFlags(
                        interactiveHost, defaultDetailFlags);
                }
                else
                {
                    detailFlags |= defaultDetailFlags;
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
                    detailFlags |= DetailFlags.Debug;
                }
                else
                {
                    //
                    // NOTE: Unset the active debugger flag.
                    //
                    detailFlags &= ~DetailFlags.Debug;
                }
            }

            //
            // NOTE: Show empty content?
            //
            if (empty)
                detailFlags |= DetailFlags.EmptyContent;

            return detailFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the engine flags appropriate for entering the
        /// interactive loop, accounting for whether a debugger is active.
        /// </summary>
        /// <param name="debug">
        /// Non-zero if a debugger is currently active.
        /// </param>
        /// <returns>
        /// The engine flags to use for the interactive loop.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified interpreter is able to
        /// hit breakpoints of the given type under the given engine flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check, if any.  This parameter may be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags that may suppress breakpoints.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type to check for.
        /// </param>
        /// <returns>
        /// True if the interpreter can hit breakpoints of the specified type;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the script library path of the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose library path should be returned, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The library path of the interpreter, or null if the interpreter is
        /// null.
        /// </returns>
        public static string GetLibraryPath(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.LibraryPath;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of automatic package search paths of the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose automatic path list should be returned, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The automatic path list of the interpreter, or null if the
        /// interpreter is null.
        /// </returns>
        public static StringList GetAutoPathList(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.AutoPathList;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new debugger instance using the specified
        /// options.
        /// </summary>
        /// <param name="isolated">
        /// Non-zero to create the debugger with its own isolated interpreter.
        /// </param>
        /// <param name="culture">
        /// The culture to use for the debugger interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags that control how the debugger interpreter is created.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the debugger interpreter host is created.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags that control how the debugger interpreter is initialized.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags that control how scripts are located and loaded by the
        /// debugger interpreter.
        /// </param>
        /// <param name="interpreterFlags">
        /// The miscellaneous flags that control the behavior of the debugger
        /// interpreter.
        /// </param>
        /// <param name="pluginFlags">
        /// The flags that control how plugins are loaded by the debugger
        /// interpreter.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which the debugger interpreter should be
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="host">
        /// The host to use (or clone) for the debugger interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="libraryPath">
        /// The script library path for the debugger interpreter, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of automatic package search paths for the debugger
        /// interpreter, if any.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created debugger instance.
        /// </returns>
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

        /// <summary>
        /// This method stores a copy of the specified return code and result on
        /// the debugger associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose debugger result should be set, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="code">
        /// The return code to associate with the copied result.
        /// </param>
        /// <param name="result">
        /// The result to copy onto the debugger.  This parameter may be null.
        /// </param>
        /// <param name="ignoreEnabled">
        /// Non-zero to set the result even when the debugger is not currently
        /// enabled.
        /// </param>
        /// <returns>
        /// True if the result was set on the debugger; otherwise, false.
        /// </returns>
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
