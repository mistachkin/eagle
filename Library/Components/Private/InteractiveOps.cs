/*
 * InteractiveOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SHELL && INTERACTIVE_COMMANDS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
#endif

using Eagle._Attributes;

#if SHELL && INTERACTIVE_COMMANDS
using Eagle._Components.Public;
using Eagle._Components.Shared;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
#endif

#if HISTORY || (SHELL && INTERACTIVE_COMMANDS)
using Eagle._Interfaces.Public;
#endif

#if SHELL && INTERACTIVE_COMMANDS
using _Public = Eagle._Components.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
#endif

using SharedStringOps = Eagle._Components.Shared.StringOps;

using CommandFlagsDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Public.CommandFlags>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the support routines used to implement the
    /// interactive shell commands available within the interactive loop,
    /// including command access control, command resolution, and command
    /// dispatch.
    /// </summary>
    [ObjectId("3d994484-cb72-4c34-acbb-f74fd0509f14")]
    internal static class InteractiveOps
    {
        #region Private Constants
#if SHELL && INTERACTIVE_COMMANDS
#if NATIVE && TCL
        //
        // NOTE: Used by the interpreter host to set its title based on the
        //       currently selected evaluation mode.
        //
        /// <summary>
        /// The title text used by the interpreter host when the native Tcl
        /// evaluation mode is currently selected.
        /// </summary>
        private static readonly string TclInteractiveMode = "native Tcl mode";

        /// <summary>
        /// The title text used by the interpreter host when the (default)
        /// Eagle evaluation mode is currently selected; a null value
        /// indicates that no special title text is used.
        /// </summary>
        private static readonly string EagleInteractiveMode = null;
#endif

        ///////////////////////////////////////////////////////////////////////////

        //
        // NOTE: When one of the IInformationHost.Write* methods fails (i.e. it
        //       returns false), it is typically because there is not enough
        //       space left to write the complete output using the currently
        //       selected style (e.g. the internal call to WriteBox failed).
        //
        /// <summary>
        /// The error message issued when an attempt to write formatted
        /// information to the interpreter host fails, typically because
        /// there is not enough space left to write the complete output
        /// using the currently selected style.
        /// </summary>
        private static readonly string HostWriteInfoError =
            "failed to write formatted information to host " +
            "(perhaps there is no space left?), please use " +
            "the [host clear] command and try again";

        ///////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default script variable name used (i.e. in the current
        //       scope) to hold the result of the last interactive command.  It is
        //       used by the interactive "#sresult" command.
        //
        /// <summary>
        /// The default script variable name, within the current scope, used
        /// to hold the result of the last interactive command; it is used by
        /// the interactive "#sresult" command.
        /// </summary>
        private static readonly string DefaultResultVarName = "__result";

        ///////////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default, should a "safe" interpreter be allowed to execute an
        //       interactive command that is not also considered "safe"?
        //
        /// <summary>
        /// The default value indicating whether a "safe" interpreter should
        /// be allowed to execute an interactive command that is not also
        /// considered "safe".
        /// </summary>
        private static readonly bool DefaultAllowAllCommands = false;
#endif

        ///////////////////////////////////////////////////////////////////////////

#if HISTORY && SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// The default file name used when loading or saving the interactive
        /// command history.
        /// </summary>
        private static readonly string DefaultHistoryFileName =
            "history" + FileExtension.Script;

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default history data used when loading the interactive command
        /// history; a null value indicates that no specific data is used.
        /// </summary>
        private static readonly IHistoryData DefaultHistoryLoadData = null;

        /// <summary>
        /// The default history data used when saving the interactive command
        /// history; a null value indicates that no specific data is used.
        /// </summary>
        private static readonly IHistoryData DefaultHistorySaveData = null;

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default history filter used when loading the interactive
        /// command history; a null value indicates that no filtering is
        /// performed.
        /// </summary>
        private static readonly IHistoryFilter DefaultHistoryLoadFilter = null;

        /// <summary>
        /// The default history filter used when saving the interactive
        /// command history; a null value indicates that no filtering is
        /// performed.
        /// </summary>
        private static readonly IHistoryFilter DefaultHistorySaveFilter = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Private Data
#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// The object used to synchronize access to the static state of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The global override controlling whether "safe" interpreters may
        /// execute all interactive commands; a null value indicates that the
        /// default value should be used.
        /// </summary>
        private static bool? alwaysAllowAllCommands;

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached mapping of interactive command names to their
        /// associated command flags; it remains null until it is lazily
        /// initialized.
        /// </summary>
        private static CommandFlagsDictionary allCommandFlags;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Interactive Command Access Control Methods
#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// This method determines whether all interactive commands should
        /// always be allowed to execute, even from within a "safe"
        /// interpreter.
        /// </summary>
        /// <returns>
        /// True if all interactive commands should always be allowed to
        /// execute; otherwise, false.
        /// </returns>
        private static bool ShouldAlwaysAllowAllCommands()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (alwaysAllowAllCommands == null)
                    alwaysAllowAllCommands = DefaultAllowAllCommands;

                return (bool)alwaysAllowAllCommands;
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the global override that allows
        /// all interactive commands to be executed from within "safe"
        /// interpreters, optionally displaying a status prompt.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to display a status prompt, if any.
        /// May be null.
        /// </param>
        /// <param name="enable">
        /// Non-zero to allow all interactive commands to be executed from
        /// within "safe" interpreters; zero to disallow it.
        /// </param>
        private static void EnableAlwaysAllowAllCommands(
            Interpreter interpreter, /* in */
            bool enable              /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((alwaysAllowAllCommands == null) ||
                    ((bool)alwaysAllowAllCommands != enable))
                {
                    alwaysAllowAllCommands = enable;

                    if (interpreter != null)
                    {
                        try
                        {
                            IInteractiveHost interactiveHost =
                                interpreter.GetInteractiveHost();

                            ShellOps.WritePrompt(interactiveHost, String.Format(
                                "{0} all interactive commands from within " +
                                "\"safe\" interpreters", enable ? "enabled" :
                                "disabled"));
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(InteractiveOps).Name,
                                TracePriority.ShellError);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the lock is already held.
        //
        /// <summary>
        /// This method populates the cache that maps interactive command
        /// names to their associated command flags, using the public static
        /// methods of the specified type.  This method assumes the lock is
        /// already held.
        /// </summary>
        /// <param name="type">
        /// The type whose public static methods are scanned for their
        /// associated command flags.
        /// </param>
        /// <param name="force">
        /// Non-zero to rebuild the cache even if it has already been
        /// populated.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear any existing cached entries before (re)populating
        /// the cache.
        /// </param>
        /// <param name="merge">
        /// Non-zero to overwrite an existing cache entry for a command name
        /// that is already present; zero to keep the entry already present.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode InitializeCommandFlags(
            Type type,
            bool force,
            bool clear,
            bool merge,
            ref Result error
            )
        {
            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (!force && (allCommandFlags != null))
                return ReturnCode.Ok;

            if (clear && (allCommandFlags != null))
                allCommandFlags.Clear();

            try
            {
                MemberInfo[] memberInfos = type.GetMethods(
                    ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PublicStaticMethod, true));

                if (memberInfos == null)
                {
                    error = String.Format(
                        "methods unavailable for type {0}",
                        FormatOps.TypeName(type));

                    return ReturnCode.Error;
                }

                foreach (MemberInfo memberInfo in memberInfos)
                {
                    if (memberInfo == null)
                        continue;

                    string memberName = memberInfo.Name;

                    if (memberName == null)
                        continue;

                    memberName = memberName.TrimStart(
                        Characters.Underscore);

                    CommandFlags commandFlags = AttributeOps.GetCommandFlags(
                        memberInfo);

                    if (allCommandFlags == null)
                        allCommandFlags = new CommandFlagsDictionary();

                    if (merge || !allCommandFlags.ContainsKey(memberName))
                        allCommandFlags[memberName] = commandFlags;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the lock is already held.
        //
        /// <summary>
        /// This method attempts to look up the command flags associated with
        /// the specified interactive command name.  This method assumes the
        /// lock is already held.
        /// </summary>
        /// <param name="command">
        /// The interactive command name to look up.
        /// </param>
        /// <param name="commandFlags">
        /// Upon success, receives the command flags associated with the
        /// specified command name; upon failure, receives
        /// <see cref="CommandFlags.None" />.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the command flags were found; otherwise, false.
        /// </returns>
        private static bool TryGetCommandFlags(
            string command,                /* in */
            out CommandFlags commandFlags, /* out */
            ref Result error               /* out */
            )
        {
            if (allCommandFlags == null) /* NOTE: Impossible? */
            {
                commandFlags = CommandFlags.None;
                error = "all interactive commands are missing their marks";

                return false;
            }

            if (!allCommandFlags.TryGetValue(command, out commandFlags))
            {
                error = String.Format(
                    "interactive command {0} is missing its marks",
                    FormatOps.WrapOrNull(command));

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interactive command
        /// is allowed to execute within the specified interpreter, taking
        /// into account whether the interpreter is "safe" and whether the
        /// global override allowing all commands is in effect.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the interactive command would be
        /// executed.  May be null.
        /// </param>
        /// <param name="command">
        /// The name of the interactive command to check.  It may include the
        /// interactive command prefix.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about why access was denied.
        /// </param>
        /// <returns>
        /// True if the interactive command is allowed to execute; otherwise,
        /// false.
        /// </returns>
        public static bool IsAccessAllowed(
            Interpreter interpreter, /* in */
            string command,          /* in */
            ref Result error         /* out */
            )
        {
            //
            // NOTE: If we are dealing with a "safe" interpreter, make sure
            //       the specified interactive command is marked as "safe"
            //       -UNLESS- the global override for this subsystem is set
            //       to non-zero.
            //
            if ((interpreter == null) || !interpreter.InternalIsSafe() ||
                ShouldAlwaysAllowAllCommands())
            {
                return true;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (command == null)
                {
                    error = "invalid interactive command name";
                    return false;
                }

                command = command.TrimStart(
                    ShellOps.InteractiveCommandPrefixChar);

                if (InitializeCommandFlags(
                        typeof(InteractiveOps.Commands), false, false,
                        false, ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                CommandFlags commandFlags;

                if (!TryGetCommandFlags(command, out commandFlags, ref error))
                    return false;

                if (!EntityOps.IsSafe(commandFlags))
                {
                    error = String.Format(
                        "interactive command {0} is not considered \"safe\"",
                        FormatOps.WrapOrNull(command));

                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a previously captured interactive command
        /// access error to the specified interactive host, clearing it
        /// afterward.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to which the access error should be written.
        /// May be null.
        /// </param>
        /// <param name="accessError">
        /// The access error to write.  Upon return, this is reset to null
        /// when it has been written.
        /// </param>
        private static void WriteAccessError(
            IInteractiveHost interactiveHost, /* in */
            ref Result accessError            /* in, out */
            )
        {
            if ((interactiveHost != null) && (accessError != null))
            {
                ShellOps.WriteResult(
                    interactiveHost, ReturnCode.Error, accessError, 0);

                accessError = null;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Interactive Command Helper Methods
#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// This method determines whether interactive command dispatch
        /// tracing is currently enabled for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose interactive loop flags are checked.
        /// May be null.
        /// </param>
        /// <returns>
        /// True if interactive command tracing is enabled; otherwise, false.
        /// </returns>
        private static bool ShouldTraceCommand(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return false;

            InteractiveLoopFlags interactiveLoopFlags =
                interpreter.InternalInteractiveLoopFlags;

            /* EXEMPT */
            return FlagOps.HasFlags(interactiveLoopFlags,
                InteractiveLoopFlags.TraceCommand, true);
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits a diagnostic trace message describing an
        /// interactive command dispatch operation, when such tracing is
        /// enabled.
        /// </summary>
        /// <param name="tracePrefix">
        /// A short label describing the point at which this trace message is
        /// being emitted.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context associated with the interactive command.
        /// May be null.
        /// </param>
        /// <param name="text">
        /// The raw interactive command text being processed.
        /// </param>
        /// <param name="command">
        /// The name of the interactive command being checked, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the interactive command, if any.
        /// </param>
        /// <param name="usePrefix">
        /// Non-zero if the interactive command prefix is being applied to the
        /// command name.
        /// </param>
        /// <param name="exact">
        /// Non-zero if exact interactive command name matching is being used.
        /// </param>
        /// <param name="verbose">
        /// Non-zero if verbose diagnostic output is enabled.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the interactive command, if any.
        /// </param>
        /// <param name="accessError">
        /// The access error associated with the interactive command, if any.
        /// </param>
        /// <param name="code">
        /// The return code associated with the interactive command, if any.
        /// </param>
        /// <param name="result">
        /// The result associated with the interactive command, if any.
        /// </param>
        private static void TraceCommand(
            string tracePrefix,      /* in */
            Interpreter interpreter, /* in */
            string text,             /* in */
            string command,          /* in */
            IClientData clientData,  /* in */
            bool usePrefix,          /* in */
            bool exact,              /* in */
            bool verbose,            /* in */
            ArgumentList arguments,  /* in */
            Result accessError,      /* in */
            ReturnCode code,         /* in */
            Result result            /* in */
            )
        {
            if (!ShouldTraceCommand(interpreter))
                return;

            TraceOps.DebugTrace(interpreter, String.Format(
                "TraceCommand: {0}, interpreter = {1}, text = {2}, " +
                "command = {3}, clientData = {4}, usePrefix = {5}, " +
                "exact = {6}, verbose = {7}, arguments = {8}, " +
                "accessError = {9}, code = {10}, result = {11}",
                tracePrefix, FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(true, true, text),
                FormatOps.WrapOrNull(command),
                FormatOps.WrapOrNull(clientData), usePrefix, exact,
                verbose, FormatOps.WrapOrNull(true, true, arguments),
                FormatOps.WrapOrNull(accessError), code,
                FormatOps.WrapOrNull(true, true, result)),
                typeof(InteractiveOps).Name, TracePriority.ShellDebug, 1);
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to resolve the first argument as an
        /// interactive command, locating the entity that would execute it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve the interactive command.
        /// May be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments whose first element is the interactive command name
        /// to resolve.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when resolving the interactive command.
        /// </param>
        /// <param name="name">
        /// Upon success, receives the resolved interactive command name.
        /// </param>
        /// <param name="execute">
        /// Upon success, receives the entity capable of executing the
        /// resolved interactive command.
        /// </param>
        /// <returns>
        /// True if the interactive command was resolved; otherwise, false.
        /// </returns>
        private static bool ResolveCommand(
            Interpreter interpreter, /* in */
            ArgumentList arguments,  /* in */
            EngineFlags engineFlags, /* in */
            ref string name,         /* out */
            ref IExecute execute     /* out */
            )
        {
            if (interpreter == null)
                return false;

            if ((arguments == null) || (arguments.Count == 0))
                return false;

            //
            // NOTE: If the interactive command prefix is not valid (null -OR-
            //       zero length) we cannot resolve or execute any interactive
            //       commands.
            //
            string prefix = ShellOps.InteractiveCommandPrefix;

            if (String.IsNullOrEmpty(prefix))
                return false;

            //
            // NOTE: Extract the name of the interactive command to resolve
            //       and execute.  It must start with the interactive command
            //       prefix (e.g. "#") in order to be properly resolved and
            //       executed.
            //
            name = ScriptOps.MakeCommandName(arguments[0]);

            if ((name == null) || !name.StartsWith(
                    prefix, SharedStringOps.SystemNoCaseComparisonType))
            {
                return false;
            }

            //
            // NOTE: If the name of the interactive command consists only of
            //       the interactive command prefix itself then try for an
            //       exact match; otherwise, partial non-ambiguous prefix
            //       matching will be used.  See if this interactive command
            //       has been officially registered with the interpreter (i.e.
            //       it is an interactive extension command, which may also
            //       shadow an existing built-in interactive command).  Not
            //       finding the interactive command is not an error;
            //       therefore, the error message is ignored.
            //
            if (name.Length <= prefix.Length)
                engineFlags |= EngineFlags.ExactMatch;

            if (interpreter.InternalGetIExecuteViaResolvers(
                    engineFlags, name, arguments, LookupFlags.NoVerbose,
                    ref execute) == ReturnCode.Ok)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the engine flags, interactive engine flags,
        /// substitution flags, event flags, expression flags, and result
        /// limits to their default values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to obtain the default result limits.
        /// May be null.
        /// </param>
        /// <param name="engineFlags">
        /// Upon return, receives the default engine flags.
        /// </param>
        /// <param name="interactiveCommandsEnabled">
        /// Upon return, receives a value indicating whether interactive
        /// commands are enabled.
        /// </param>
        /// <param name="interactiveEngineFlags">
        /// Upon return, receives the default interactive engine flags.
        /// </param>
        /// <param name="interactiveSubstitutionFlags">
        /// Upon return, receives the default interactive substitution flags.
        /// </param>
        /// <param name="interactiveEventFlags">
        /// Upon return, receives the default interactive event flags.
        /// </param>
        /// <param name="interactiveExpressionFlags">
        /// Upon return, receives the default interactive expression flags.
        /// </param>
        /// <param name="interactiveExecuteResultLimit">
        /// Upon return, receives the default result length limit used for
        /// interactive command execution.
        /// </param>
        /// <param name="interactiveNestedResultLimit">
        /// Upon return, receives the default result length limit used for
        /// nested interactive command execution.
        /// </param>
        public static void ResetFlagsAndLimits(
            Interpreter interpreter,                            /* in */
            out EngineFlags engineFlags,                        /* out */
            out bool interactiveCommandsEnabled,                /* out */
            out EngineFlags interactiveEngineFlags,             /* out */
            out SubstitutionFlags interactiveSubstitutionFlags, /* out */
            out EventFlags interactiveEventFlags,               /* out */
            out ExpressionFlags interactiveExpressionFlags      /* out */
#if RESULT_LIMITS
            , out int interactiveExecuteResultLimit             /* out */
            , out int interactiveNestedResultLimit              /* out */
#endif
            )
        {
            engineFlags = EngineFlags.None;
            interactiveCommandsEnabled = true;
            interactiveEngineFlags = EngineFlags.None;
            interactiveSubstitutionFlags = SubstitutionFlags.Default;
            interactiveEventFlags = EventFlags.Default;
            interactiveExpressionFlags = ExpressionFlags.Default;

#if RESULT_LIMITS
            Interpreter.GetDefaultResultLimits(
                interpreter, out interactiveExecuteResultLimit,
                out interactiveNestedResultLimit);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////

#if RESULT_LIMITS
        /// <summary>
        /// This method queries the current engine flags, interactive engine
        /// flags, substitution flags, event flags, and expression flags for
        /// the specified interpreter, discarding the associated result
        /// limits.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to query.  May be null.
        /// </param>
        /// <param name="engineFlags">
        /// Upon return, receives the current engine flags.
        /// </param>
        /// <param name="interactiveCommandsEnabled">
        /// Upon return, receives a value indicating whether interactive
        /// commands are enabled.
        /// </param>
        /// <param name="interactiveEngineFlags">
        /// Upon return, receives the current interactive engine flags.
        /// </param>
        /// <param name="interactiveSubstitutionFlags">
        /// Upon return, receives the current interactive substitution flags.
        /// </param>
        /// <param name="interactiveEventFlags">
        /// Upon return, receives the current interactive event flags.
        /// </param>
        /// <param name="interactiveExpressionFlags">
        /// Upon return, receives the current interactive expression flags.
        /// </param>
        private static void QueryFlagsAndLimits(
            Interpreter interpreter,                            /* in */
            out EngineFlags engineFlags,                        /* out */
            out bool interactiveCommandsEnabled,                /* out */
            out EngineFlags interactiveEngineFlags,             /* out */
            out SubstitutionFlags interactiveSubstitutionFlags, /* out */
            out EventFlags interactiveEventFlags,               /* out */
            out ExpressionFlags interactiveExpressionFlags      /* out */
            )
        {
            int interactiveExecuteResultLimit;
            int interactiveNestedResultLimit;

            QueryFlagsAndLimits(
                interpreter, out engineFlags, out interactiveCommandsEnabled,
                out interactiveEngineFlags, out interactiveSubstitutionFlags,
                out interactiveEventFlags, out interactiveExpressionFlags,
                out interactiveExecuteResultLimit,
                out interactiveNestedResultLimit);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the current engine flags, interactive engine
        /// flags, substitution flags, event flags, expression flags, and
        /// result limits for the specified interpreter, falling back to the
        /// default values when no interpreter is available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to query.  May be null.
        /// </param>
        /// <param name="engineFlags">
        /// Upon return, receives the current engine flags.
        /// </param>
        /// <param name="interactiveCommandsEnabled">
        /// Upon return, receives a value indicating whether interactive
        /// commands are enabled.
        /// </param>
        /// <param name="interactiveEngineFlags">
        /// Upon return, receives the current interactive engine flags.
        /// </param>
        /// <param name="interactiveSubstitutionFlags">
        /// Upon return, receives the current interactive substitution flags.
        /// </param>
        /// <param name="interactiveEventFlags">
        /// Upon return, receives the current interactive event flags.
        /// </param>
        /// <param name="interactiveExpressionFlags">
        /// Upon return, receives the current interactive expression flags.
        /// </param>
        /// <param name="interactiveExecuteResultLimit">
        /// Upon return, receives the current result length limit used for
        /// interactive command execution.
        /// </param>
        /// <param name="interactiveNestedResultLimit">
        /// Upon return, receives the current result length limit used for
        /// nested interactive command execution.
        /// </param>
        private static void QueryFlagsAndLimits(
            Interpreter interpreter,                            /* in */
            out EngineFlags engineFlags,                        /* out */
            out bool interactiveCommandsEnabled,                /* out */
            out EngineFlags interactiveEngineFlags,             /* out */
            out SubstitutionFlags interactiveSubstitutionFlags, /* out */
            out EventFlags interactiveEventFlags,               /* out */
            out ExpressionFlags interactiveExpressionFlags      /* out */
#if RESULT_LIMITS
            , out int interactiveExecuteResultLimit             /* out */
            , out int interactiveNestedResultLimit              /* out */
#endif
            )
        {
            if (interpreter != null)
            {
                interpreter.QueryInteractiveFlagsAndLimits(
                    out engineFlags, out interactiveCommandsEnabled,
                    out interactiveEngineFlags, out interactiveSubstitutionFlags,
                    out interactiveEventFlags, out interactiveExpressionFlags
#if RESULT_LIMITS
                    , out interactiveExecuteResultLimit
                    , out interactiveNestedResultLimit
#endif
                );
            }
            else
            {
                ResetFlagsAndLimits(
                    interpreter, out engineFlags, out interactiveCommandsEnabled,
                    out interactiveEngineFlags, out interactiveSubstitutionFlags,
                    out interactiveEventFlags, out interactiveExpressionFlags
#if RESULT_LIMITS
                    , out interactiveExecuteResultLimit
                    , out interactiveNestedResultLimit
#endif
                );
            }
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to resolve and execute the specified
        /// arguments as an interactive extension command, using the
        /// interactive engine, substitution, event, and expression flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve and execute the
        /// interactive command.  May be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments whose first element is the interactive command name
        /// to execute.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the interactive command, if any.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require exact interactive command name matching.
        /// </param>
        /// <param name="code">
        /// Upon return, receives the return code produced by executing the
        /// interactive command.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result produced by executing the
        /// interactive command.
        /// </param>
        /// <returns>
        /// True if an interactive extension command was resolved and
        /// executed; otherwise, false.
        /// </returns>
        private static bool ExecuteCommand(
            Interpreter interpreter, /* in */
            ArgumentList arguments,  /* in */
            IClientData clientData,  /* in */
            bool exact,              /* in */
            ref ReturnCode code,     /* out */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
                return false;

            EngineFlags engineFlags;
            bool interactiveCommandsEnabled;
            EngineFlags interactiveEngineFlags;
            SubstitutionFlags interactiveSubstitutionFlags;
            EventFlags interactiveEventFlags;
            ExpressionFlags interactiveExpressionFlags;

#if RESULT_LIMITS
            int interactiveExecuteResultLimit;
            int interactiveNestedResultLimit;
#endif

            QueryFlagsAndLimits(
                interpreter, out engineFlags, out interactiveCommandsEnabled,
                out interactiveEngineFlags, out interactiveSubstitutionFlags,
                out interactiveEventFlags, out interactiveExpressionFlags
#if RESULT_LIMITS
                , out interactiveExecuteResultLimit
                , out interactiveNestedResultLimit
#endif
            );

            if (!interactiveCommandsEnabled)
                return false;

            EngineFlags resolveEngineFlags = Interpreter.GetResolveEngineFlags(
                interactiveEngineFlags, exact);

            string name = null;
            IExecute execute = null;

            if (!ResolveCommand(
                    interpreter, arguments, resolveEngineFlags, ref name,
                    ref execute))
            {
                return false;
            }

            ICallFrame frame = interpreter.NewTrackingCallFrame(
                StringList.MakeList("interactive", name),
                CallFrameFlags.Interactive);

            interpreter.PushAutomaticCallFrame(frame);

            try
            {
                //
                // NOTE: Save the current engine flags and then enable
                //       the external execution flags.
                //
                EngineFlags savedEngineFlags = Engine.AddStackCheckFlags(
                    ref interactiveEngineFlags);

                try
                {
                    //
                    // NOTE: Execute the command using the interactive
                    //       engine and substitution flags with the
                    //       interactive engine flags having been
                    //       modified to include the flags necessary
                    //       for external command execution (i.e.
                    //       command execution outside of the engine).
                    //
                    code = Engine.Execute(
                        name, execute, interpreter, Engine.GetClientData(
                            interpreter, clientData, false), arguments,
                        interactiveEngineFlags, interactiveSubstitutionFlags,
                        interactiveEventFlags, interactiveExpressionFlags,
#if RESULT_LIMITS
                        interactiveExecuteResultLimit,
#endif
                        ref result);
                }
                finally
                {
                    //
                    // NOTE: Restore the saved engine flags, masking off
                    //       the external execution flags as necessary.
                    //
                    Engine.RemoveStackCheckFlags(
                        savedEngineFlags, ref interactiveEngineFlags);
                }
            }
            finally
            {
                //
                // NOTE: Pop the original call frame that we pushed
                //       above and any intervening scope call frames
                //       that may be leftover (i.e. they were not
                //       explicitly closed).
                //
                /* IGNORED */
                interpreter.PopScopeCallFramesAndOneMore();
            }

            //
            // NOTE: Yes, we just executed an interactive extension
            //       command; therefore, prevent the default handling
            //       of the built-in interactive command, if any.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally performs substitution on the specified
        /// text when it appears to contain an interactive command and
        /// substitution has not been disabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to perform substitution.  May be
        /// null.
        /// </param>
        /// <param name="text">
        /// The interactive command text on which substitution may be
        /// performed.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags used to determine whether substitution is
        /// disabled.
        /// </param>
        /// <param name="interactiveEngineFlags">
        /// The interactive engine flags used to perform substitution and to
        /// determine whether substitution is disabled.
        /// </param>
        /// <param name="interactiveSubstitutionFlags">
        /// The interactive substitution flags used to perform substitution.
        /// </param>
        /// <param name="interactiveEventFlags">
        /// The interactive event flags used to perform substitution.
        /// </param>
        /// <param name="interactiveExpressionFlags">
        /// The interactive expression flags used to perform substitution.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit a diagnostic trace message upon failure.  Upon
        /// return, this may be reset to zero so that the error is only
        /// reported once per interactive command entered.
        /// </param>
        /// <returns>
        /// True if substitution succeeded or was not required; otherwise,
        /// false.
        /// </returns>
        private static bool MaybeSubstituteString(
            Interpreter interpreter,                        /* in */
            string text,                                    /* in */
            EngineFlags engineFlags,                        /* in */
            EngineFlags interactiveEngineFlags,             /* in */
            SubstitutionFlags interactiveSubstitutionFlags, /* in */
            EventFlags interactiveEventFlags,               /* in */
            ExpressionFlags interactiveExpressionFlags,     /* in */
            ref bool verbose                                /* in, out */
            )
        {
            //
            // NOTE: To do anything with substitution, we require a valid
            //       interpreter context.
            //
            if (interpreter == null)
                return true;

            //
            // NOTE: Only perform substitution if the text looks like it
            //       contains an interactive command.
            //
            if (!ShellOps.LooksLikeAnyInteractiveCommand(text))
                return true;

            //
            // NOTE: Has substitution been disabled by the interpreter or
            //       via the interactive substitution flags?
            //
            if (EngineFlagOps.HasNoSubstitute(engineFlags) ||
                EngineFlagOps.HasNoSubstitute(interactiveEngineFlags) ||
                (interactiveSubstitutionFlags == SubstitutionFlags.None))
            {
                return true;
            }

            //
            // NOTE: Perform substitions witihin text using the specified
            //       flags.
            //
            ReturnCode code;
            Result result = null;

            code = Engine.SubstituteString(
                interpreter, text, interactiveEngineFlags,
                interactiveSubstitutionFlags, interactiveEventFlags,
                interactiveExpressionFlags, ref result);

            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: Ok, replace the original command text with the
                //       substituted result.
                //
                text = result;

                //
                // NOTE: Finally, indicate to the caller we succeeded.
                //
                return true;
            }
            else if (verbose)
            {
                //
                // NOTE: Only show this error once per actual interactive
                //       command entered, not per call to this method.
                //
                verbose = false;

                TraceOps.DebugTrace(String.Format(
                    "MaybeSubstituteString: code = {0}, result = {1}",
                    code, FormatOps.WrapOrNull(result)),
                    typeof(InteractiveOps).Name, TracePriority.ShellError);
            }

            //
            // NOTE: Finally, indicate to the caller we failed.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////

#if HISTORY
        /// <summary>
        /// This method conditionally adds the specified interactive command
        /// arguments to the command history of the interpreter, when history
        /// recording is enabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose command history may be updated.  May
        /// be null.
        /// </param>
        /// <param name="arguments">
        /// The interactive command arguments to add to the command history.
        /// </param>
        private static void MaybeAddCommandToHistory(
            Interpreter interpreter, /* in */
            ArgumentList arguments   /* in */
            )
        {
            if ((interpreter != null) && interpreter.History)
            {
                interpreter.AddHistory(arguments,
                    interpreter.InternalLevels, HistoryFlags.Interactive);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the first element of the specified
        /// argument list matches either of the specified interactive command
        /// names.
        /// </summary>
        /// <param name="arguments">
        /// The argument list whose first element is compared against the
        /// command names.
        /// </param>
        /// <param name="normalCommand">
        /// The normal interactive command name to match against.
        /// </param>
        /// <param name="systemCommand">
        /// The system interactive command name to match against.
        /// </param>
        /// <returns>
        /// True if the first argument matches one of the specified command
        /// names; otherwise, false.
        /// </returns>
        private static bool MatchCommand(
            IList arguments,
            string normalCommand,
            string systemCommand
            )
        {
            if ((arguments != null) && (arguments.Count > 0))
            {
                string argument = StringOps.GetStringFromObject(arguments[0]);

                if (SharedStringOps.SystemNoCaseEquals(argument, normalCommand) ||
                    SharedStringOps.SystemNoCaseEquals(argument, systemCommand))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interactive command
        /// text is permitted to execute, taking into account whether
        /// interactive commands are enabled and whether the text looks like
        /// an interactive system command.
        /// </summary>
        /// <param name="text">
        /// The interactive command text to check.
        /// </param>
        /// <param name="interactiveCommandsEnabled">
        /// Non-zero if interactive commands are currently enabled.
        /// </param>
        /// <returns>
        /// True if the interactive command is permitted to execute;
        /// otherwise, false.
        /// </returns>
        private static bool CanExecuteCommand(
            string text,
            bool interactiveCommandsEnabled
            )
        {
            if (interactiveCommandsEnabled ||
                !ShellOps.LooksLikeInteractiveSystemCommand(text))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the specified text represents the
        /// specified interactive command, dispatching it as an external
        /// interactive command when appropriate.  This overload uses local
        /// default values for the additional tracking parameters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the interactive command is
        /// checked.  May be null.
        /// </param>
        /// <param name="text">
        /// The raw interactive command text to check.
        /// </param>
        /// <param name="command">
        /// The name of the interactive command to check for, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the interactive command, if any.
        /// </param>
        /// <param name="usePrefix">
        /// Non-zero to apply the interactive command prefix to the command
        /// name being checked.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require exact interactive command name matching.
        /// </param>
        /// <returns>
        /// True if the interactive command was matched or handled; otherwise,
        /// false.
        /// </returns>
        public static bool CheckCommand(
            Interpreter interpreter, /* in */
            string text,             /* in */
            string command,          /* in */
            IClientData clientData,  /* in */
            bool usePrefix,          /* in */
            bool exact               /* in */
            )
        {
            bool verbose = false; /* NOTE: Mask substitution errors. */
            ArgumentList arguments = null;
            Result accessError = null;
            ReturnCode code = ReturnCode.Ok;
            Result result = null;

            return CheckCommand(
                interpreter, text, command, clientData, usePrefix, exact,
                ref verbose, ref arguments, ref accessError, ref code,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the specified text represents the
        /// specified interactive command, optionally performing substitution,
        /// dispatching it as an external interactive command, and recording
        /// it in the command history when appropriate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the interactive command is
        /// checked.  May be null.
        /// </param>
        /// <param name="text">
        /// The raw interactive command text to check.
        /// </param>
        /// <param name="command">
        /// The name of the interactive command to check for, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the interactive command, if any.
        /// </param>
        /// <param name="usePrefix">
        /// Non-zero to apply the interactive command prefix to the command
        /// name being checked.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require exact interactive command name matching.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit diagnostic trace and substitution error messages.
        /// Upon return, this may be updated to suppress repeated error
        /// reporting.
        /// </param>
        /// <param name="arguments">
        /// The pre-existing interactive command arguments, if any.  Upon
        /// return, this may receive the arguments parsed from the interactive
        /// command text.
        /// </param>
        /// <param name="accessError">
        /// Upon return, receives information about an interactive command
        /// access error, if any.
        /// </param>
        /// <param name="code">
        /// Upon return, receives the return code produced by executing the
        /// interactive command, if any.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result produced by executing the
        /// interactive command, if any.
        /// </param>
        /// <returns>
        /// True if the interactive command was matched or handled; otherwise,
        /// false.
        /// </returns>
        private static bool CheckCommand(
            Interpreter interpreter,    /* in */
            string text,                /* in */
            string command,             /* in */
            IClientData clientData,     /* in */
            bool usePrefix,             /* in */
            bool exact,                 /* in */
            ref bool verbose,           /* in, out */
            ref ArgumentList arguments, /* in, out */
            ref Result accessError,     /* out */
            ref ReturnCode code,        /* out */
            ref Result result           /* out */
            )
        {
            TraceCommand(
                "entered", interpreter,
                text, command, clientData, usePrefix, exact,
                verbose, arguments, accessError, code, result);

            if (String.IsNullOrEmpty(text))
                return false;

            EngineFlags engineFlags;
            bool interactiveCommandsEnabled;
            EngineFlags interactiveEngineFlags;
            SubstitutionFlags interactiveSubstitutionFlags;
            EventFlags interactiveEventFlags;
            ExpressionFlags interactiveExpressionFlags;

            QueryFlagsAndLimits(
                interpreter, out engineFlags, out interactiveCommandsEnabled,
                out interactiveEngineFlags, out interactiveSubstitutionFlags,
                out interactiveEventFlags, out interactiveExpressionFlags);

            //
            // NOTE: Make sure the substitution step succeeded, if any.
            //
            if (!MaybeSubstituteString(
                    interpreter, text, engineFlags, interactiveEngineFlags,
                    interactiveSubstitutionFlags, interactiveEventFlags,
                    interactiveExpressionFlags, ref verbose))
            {
                return false;
            }

            //
            // NOTE: Are we checking for a specific command?
            //
            if (!String.IsNullOrEmpty(command))
            {
                //
                // NOTE: Does the text end in a new line character?
                //       If so, strip it off now.
                //
                if (text[text.Length - 1] == Characters.NewLine)
                    text = text.Substring(0, text.Length - 1);

                //
                // NOTE: Does the caller want to prefix the command to
                //       check for with the interactive command prefix?
                //       This is used so that the caller does not have
                //       to hard-code the prefix inline in their calls
                //       to this method.
                //
                string normalCommand;
                string systemCommand;

                ShellOps.NormalizeInteractiveCommand(
                    command, usePrefix, out normalCommand, out systemCommand);

                //
                // NOTE: Did the caller supply pre-existing arguments?
                //       If so, check the command name (i.e. the first
                //       argument) against the command to check for.
                //
                if (arguments != null)
                {
                    if (MatchCommand(arguments, normalCommand, systemCommand))
                    {
                        //
                        // NOTE: Attempt to handle the interactive command
                        //       as an external interactive command.  Since
                        //       this method was called with the name of a
                        //       pre-existing command to check against, this
                        //       will be an overridden built-in interactive
                        //       command if it exists.
                        //
                        if (CanExecuteCommand(
                                arguments[0], interactiveCommandsEnabled))
                        {
                            bool executed = ExecuteCommand(
                                interpreter, arguments, clientData, exact,
                                ref code, ref result);

#if HISTORY
                            //
                            // NOTE: Add the command to the history even if
                            //       it was not executed.  If the command was
                            //       just executed, it has NOT been recorded
                            //       yet (because ExecuteCommand
                            //       does not handle command history).  If
                            //       the command was NOT executed, we assume
                            //       it will be by the caller since the
                            //       command name does match the one they
                            //       specified.
                            //
                            MaybeAddCommandToHistory(interpreter, arguments);
#endif

                            if (!executed)
                            {
                                TraceCommand(
                                    "executed command with arguments", interpreter,
                                    text, command, clientData, usePrefix, exact,
                                    verbose, arguments, accessError, code, result);
                            }

                            if (!executed && !IsAccessAllowed(
                                interpreter, arguments[0], ref accessError))
                            {
                                // do nothing.
                            }

                            return !executed;
                        }

#if HISTORY
                        MaybeAddCommandToHistory(interpreter, arguments);
#endif

                        TraceCommand(
                            "matched command with arguments", interpreter,
                            text, command, clientData, usePrefix, exact,
                            verbose, arguments, accessError, code, result);

                        if (!IsAccessAllowed(
                                interpreter, arguments[0], ref accessError))
                        {
                            // do nothing.
                        }

                        return true;
                    }
                }
                else
                {
                    //
                    // NOTE: The interactive command, like all other commands,
                    //       must be a well-formed list.  Split it into its
                    //       elements now.
                    //
                    StringList list = null;

                    if (ParserOps<string>.SplitList(
                            interpreter, text, 0, Length.Invalid, true,
                            ref list) == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Save the caller some work by giving them
                        //       all the arguments for the interactive
                        //       command in a list.
                        //
                        arguments = new ArgumentList(list, ArgumentFlags.None);

                        //
                        // NOTE: Compare the first element of the list (i.e.
                        //       the command name) against the command to
                        //       check for.
                        //
                        if (MatchCommand(list, normalCommand, systemCommand))
                        {
                            //
                            // NOTE: Attempt to handle the interactive command
                            //       as an external interactive command.  Since
                            //       this method was called with the name of a
                            //       pre-existing command to check against, this
                            //       will be an overridden built-in interactive
                            //       command if it exists.
                            //
                            if (CanExecuteCommand(
                                    list[0], interactiveCommandsEnabled))
                            {
                                bool executed = ExecuteCommand(
                                    interpreter, arguments, clientData, exact,
                                    ref code, ref result);

#if HISTORY
                                //
                                // NOTE: Add the command to the history even if
                                //       it was not executed.  If the command was
                                //       just executed, it has NOT been recorded
                                //       yet (because ExecuteCommand
                                //       does not handle command history).  If
                                //       the command was NOT executed, we assume
                                //       it will be by the caller since the
                                //       command name does match the one they
                                //       specified.
                                //
                                MaybeAddCommandToHistory(interpreter, arguments);
#endif

                                if (!executed)
                                {
                                    TraceCommand(
                                        "executed command", interpreter,
                                        text, command, clientData, usePrefix, exact,
                                        verbose, arguments, accessError, code, result);
                                }

                                if (!executed && !IsAccessAllowed(
                                        interpreter, list[0], ref accessError))
                                {
                                    // do nothing.
                                }

                                return !executed;
                            }

#if HISTORY
                            MaybeAddCommandToHistory(interpreter, arguments);
#endif

                            TraceCommand(
                                "matched command", interpreter,
                                text, command, clientData, usePrefix, exact,
                                verbose, arguments, accessError, code, result);

                            if (!IsAccessAllowed(
                                    interpreter, list[0], ref accessError))
                            {
                                // do nothing.
                            }

                            return true;
                        }
                    }
                }
            }
            else if (usePrefix)
            {
                //
                // NOTE: Just return non-zero if this looks like an
                //       interactive command.
                //
                bool hasPrefix = ShellOps.LooksLikeAnyInteractiveCommand(
                    text);

                if (hasPrefix)
                {
                    TraceCommand(
                        "matched prefix", interpreter,
                        text, command, clientData, usePrefix, exact,
                        verbose, arguments, accessError, code, result);
                }

                return hasPrefix;
            }
            else if (CanExecuteCommand(
                    text, interactiveCommandsEnabled) &&
                ShellOps.LooksLikeAnyInteractiveCommand(text))
            {
                //
                // NOTE: Does the text end in a new line character?
                //       If so, strip it off now.
                //
                if (text[text.Length - 1] == Characters.NewLine)
                    text = text.Substring(0, text.Length - 1);

                //
                // NOTE: Did the caller supply pre-existing arguments?
                //       If so, check the command name (i.e. the first
                //       argument) against the command to check for.
                //
                if (arguments != null)
                {
                    //
                    // NOTE: Attempt to handle the interactive command
                    //       as an external interactive command.  Since
                    //       this method was not called with the name of
                    //       a pre-existing command to check against,
                    //       this will be an interactive extension
                    //       command if it exists.
                    //
                    bool executed = ExecuteCommand(
                        interpreter, arguments, clientData, exact,
                        ref code, ref result);

                    if (executed)
                    {
#if HISTORY
                        MaybeAddCommandToHistory(interpreter, arguments);
#endif

                        TraceCommand(
                            "executed arguments", interpreter,
                            text, command, clientData, usePrefix, exact,
                            verbose, arguments, accessError, code, result);
                    }

                    return executed;
                }
                else
                {
                    //
                    // NOTE: The interactive command, like all other commands,
                    //       must be a well-formed list.  Split it into its
                    //       elements now.
                    //
                    StringList list = null;

                    if (ParserOps<string>.SplitList(
                            interpreter, text, 0, Length.Invalid, true,
                            ref list) == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Save the caller some work by giving them
                        //       all the arguments for the interactive
                        //       command in a list.
                        //
                        arguments = new ArgumentList(list, ArgumentFlags.None);

                        //
                        // NOTE: Attempt to handle the interactive command
                        //       as an external interactive command.  Since
                        //       this method was not called with the name of
                        //       a pre-existing command to check against,
                        //       this will be an interactive extension
                        //       command if it exists.
                        //
                        bool executed = ExecuteCommand(
                            interpreter, arguments, clientData, exact,
                            ref code, ref result);

                        if (executed)
                        {
#if HISTORY
                            MaybeAddCommandToHistory(interpreter, arguments);
#endif

                            TraceCommand(
                                "executed text", interpreter,
                                text, command, clientData, usePrefix, exact,
                                verbose, arguments, accessError, code, result);
                        }

                        return executed;
                    }
                }
            }

            //
            // NOTE: No, it is not the command they are checking for OR
            //       we had some failure during the substitution process
            //       and skipped further checking.
            //
            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Interactive Command Dispatch Method
#if SHELL && INTERACTIVE_COMMANDS
        //
        // TODO: Yes, this method has 32 parameters, a lot of which are "ref".
        //       Yes, it's too long, and uses a bunch of "if/else" statements
        //       for something that should be a lookup table.  This needs to
        //       be refactored.  That being said, it is tightly coupled to the
        //       interactive loop, by design, because it needs to modify state
        //       variables that are local to the interactive loop itself when
        //       performing (some of the) interactive commands.
        //
        /// <summary>
        /// This method dispatches a single interactive command, executing the
        /// appropriate built-in or external interactive command and updating
        /// the interactive loop state accordingly.  It is tightly coupled to
        /// the interactive loop, by design, because it must modify state
        /// variables that are local to that loop.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the interactive command is
        /// dispatched.  May be null.
        /// </param>
        /// <param name="loopData">
        /// The interactive loop data describing the current state of the
        /// interactive loop.
        /// </param>
        /// <param name="refresh">
        /// Non-null to override whether the interactive prompt should be
        /// refreshed; null to use the default behavior.
        /// </param>
        /// <param name="noCommand">
        /// Non-zero to skip dispatching the interactive command itself.
        /// </param>
        /// <param name="trace">
        /// Non-zero to enable diagnostic tracing of the interactive command
        /// dispatch.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host used for input and output.  Upon return, this
        /// may be updated to reflect a changed host.
        /// </param>
        /// <param name="text">
        /// The raw interactive command text to dispatch.  Upon return, this
        /// may be updated.
        /// </param>
        /// <param name="savedText">
        /// The previously saved interactive command text.  Upon return, this
        /// may be updated.
        /// </param>
        /// <param name="tclsh">
        /// Non-zero if the native Tcl shell evaluation mode is active.  Upon
        /// return, this may be updated.
        /// </param>
        /// <param name="savedTclsh">
        /// Upon return, receives the saved native Tcl shell evaluation mode,
        /// if any.
        /// </param>
        /// <param name="localEngineFlags">
        /// The engine flags local to the interactive loop.  Upon return,
        /// these may be updated.
        /// </param>
        /// <param name="localSubstitutionFlags">
        /// The substitution flags local to the interactive loop.  Upon
        /// return, these may be updated.
        /// </param>
        /// <param name="localEventFlags">
        /// The event flags local to the interactive loop.  Upon return, these
        /// may be updated.
        /// </param>
        /// <param name="localExpressionFlags">
        /// The expression flags local to the interactive loop.  Upon return,
        /// these may be updated.
        /// </param>
        /// <param name="localHeaderFlags">
        /// The header flags local to the interactive loop.  Upon return,
        /// these may be updated.
        /// </param>
        /// <param name="localDetailFlags">
        /// The detail flags local to the interactive loop.  Upon return,
        /// these may be updated.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require exact interactive command name matching.  Upon
        /// return, this may be updated.
        /// </param>
        /// <param name="canceled">
        /// Upon return, receives a value indicating whether the script in
        /// progress was canceled.
        /// </param>
        /// <param name="notReady">
        /// Upon return, receives a value indicating whether the interpreter
        /// is not ready to continue.
        /// </param>
        /// <param name="parseError">
        /// Upon return, receives information about a parsing error, if any.
        /// </param>
        /// <param name="localErrorLine">
        /// The error line number local to the interactive loop.  Upon return,
        /// this may be updated.
        /// </param>
        /// <param name="haveErrorLine">
        /// Upon return, receives a value indicating whether an error line
        /// number is available.
        /// </param>
        /// <param name="startedGcThread">
        /// Upon return, receives a value indicating whether a garbage
        /// collection thread was started.
        /// </param>
        /// <param name="tclInterpName">
        /// The name of the native Tcl interpreter to use, if any.  Upon
        /// return, this may be updated.
        /// </param>
        /// <param name="done">
        /// Upon return, receives a value indicating whether the interactive
        /// loop should exit.
        /// </param>
        /// <param name="previous">
        /// The value indicating whether the previous interactive command
        /// produced output.  Upon return, this may be updated.
        /// </param>
        /// <param name="show">
        /// Upon return, receives a value indicating whether the interactive
        /// command result should be displayed.
        /// </param>
        /// <param name="forceCancel">
        /// The value indicating whether script cancellation should be forced.
        /// Upon return, this may be updated.
        /// </param>
        /// <param name="forceHalt">
        /// The value indicating whether the interpreter halt should be
        /// forced.  Upon return, this may be updated.
        /// </param>
        /// <param name="localCode">
        /// The return code local to the interactive loop.  Upon return, this
        /// may be updated with the return code produced by the interactive
        /// command.
        /// </param>
        /// <param name="localResult">
        /// The result local to the interactive loop.  Upon return, this may
        /// be updated with the result produced by the interactive command.
        /// </param>
        /// <param name="result">
        /// Upon return, this may be updated with the overall result of the
        /// interactive command dispatch.
        /// </param>
        /// <returns>
        /// True if the interactive command was processed (i.e. recognized and
        /// handled); otherwise, false.
        /// </returns>
        public static bool DispatchCommand(
            Interpreter interpreter,                      /* in */
            IInteractiveLoopData loopData,                /* in, out */
            bool? refresh,                                /* in */
            bool noCommand,                               /* in */
            bool trace,                                   /* in */
            ref IInteractiveHost interactiveHost,         /* in, out */
            ref string text,                              /* in, out */
            ref string savedText,                         /* in, out */
            ref bool tclsh,                               /* in, out */
            ref bool? savedTclsh,                         /* out */
            ref EngineFlags localEngineFlags,             /* in, out */
            ref SubstitutionFlags localSubstitutionFlags, /* in, out */
            ref EventFlags localEventFlags,               /* in, out */
            ref ExpressionFlags localExpressionFlags,     /* in, out */
            ref HeaderFlags localHeaderFlags,             /* in, out */
            ref DetailFlags localDetailFlags,             /* in, out */
            ref bool exact,                               /* in, out */
            ref bool canceled,                            /* out */
            ref bool notReady,                            /* out */
            ref Result parseError,                        /* out */
            ref int localErrorLine,                       /* in, out */
            ref bool haveErrorLine,                       /* out */
            ref bool startedGcThread,                     /* out */
            ref string tclInterpName,                     /* in, out */
            ref bool done,                                /* out */
            ref bool previous,                            /* in, out */
            ref bool show,                                /* out */
            ref bool forceCancel,                         /* in, out */
            ref bool forceHalt,                           /* in, out */
            ref ReturnCode localCode,                     /* in, out */
            ref Result localResult,                       /* in, out */
            ref Result result                             /* in, out */
            )
        {
            #region Parameter Check
            if (loopData == null)
            {
                localResult = "invalid interactive loop data";
                localCode = ReturnCode.Error;

                return true; /* COMMAND PROCESSED */
            }

            bool exit = loopData.Exit;
            ReturnCode code = loopData.Code;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interactive Command Local Variables
            //
            // NOTE: Setup the local interactive command processing variables
            //       used when resolving and dispatching interactive commands.
            //
            bool debugVerbose = true;
            ArgumentList debugArguments = null;
            Result accessError = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interactive Commands
            #region "Special" Interactive Commands
            //
            // NOTE: This first interactive command check is designed to
            //       allow any overridden interactive commands to be executed
            //       properly (e.g. "#show") even if they do not actually
            //       represent a built-in interactive command (e.g. "#foo").
            //       This call modifies the local return code and result.
            //       The result of the interactive command execution will be
            //       displayed using the normal mechanisms, after this huge
            //       "if/else" block (below).
            //
            if (CheckCommand(
                    interpreter, text, null, loopData.ClientData,
                    false, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                #region Interactive Extension Command
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                //
                // NOTE: Do nothing.
                //
                return true; /* COMMAND PROCESSED */
                #endregion
            }
            else if (CheckCommand(
                    interpreter, text, "nop", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                #region Interactive Nop Command
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                //
                // NOTE: This built-in interactive command is specifically
                //       designed to do nothing.
                //
                return true; /* COMMAND PROCESSED */
                #endregion
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region "Normal" Interactive Commands
            #region Previously "Special" Interactive Commands
            else if (CheckCommand(
                    interpreter, text, "go", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.go(
                    loopData.Debug, ref done, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "run", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.run(
                    interpreter, loopData.Debug, ref done,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "break", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands._break(
                    interpreter, debugArguments, loopData.Token,
                    loopData.TraceInfo, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, localHeaderFlags,
                    loopData.ClientData, loopData.Arguments,
                    ref done, ref localCode, ref localResult,
                    ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "halt", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.halt(
                    interpreter, loopData.Debug, ref done,
                    ref localCode, ref localResult,
                    ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "done", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands._done(
                    debugArguments, ref done, ref localCode,
                    ref localResult, ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "exact", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands._exact(
                    ref exact, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "exit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.exit(
                    ref exit, ref localCode, ref localResult);

                if (exit)
                    loopData.SetExit();

                return true; /* COMMAND PROCESSED */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Other "Normal" Interactive Commands
            else if (CheckCommand(
                    interpreter, text, "cmd", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult) ||
                CheckCommand(
                    interpreter, text, "cmd.exe", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult) ||
                CheckCommand(
                    interpreter, text, "cmd", loopData.ClientData,
                    false, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult) ||
                CheckCommand(
                    interpreter, text, "cmd.exe", loopData.ClientData,
                    false, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.cmd(
                    interpreter, debugArguments, localEventFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "intsec", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.intsec(
                    interpreter, interactiveHost as IDebugHost,
                    debugArguments, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "fmkeys", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.fmkeys(
                    interpreter, interactiveHost as IDebugHost,
                    debugArguments, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tclshrc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tclshrc(
                    interpreter, interactiveHost as IFileSystemHost,
                    debugArguments, localEventFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "website", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.website(
                    interpreter, localEventFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "reset", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.reset(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "useattach", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.useattach(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "useforce", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.useforce(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "color", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.color(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "exceptions", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.exceptions(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "testgc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.testgc(
                    interpreter, debugArguments, ref startedGcThread,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "hcancel", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.hcancel(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "hexit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.hexit(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "stable", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.stable(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "check", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                localErrorLine = 0;

                Commands.check(
                    interpreter, debugArguments, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, ref localCode,
                    ref localResult, ref localErrorLine);

                haveErrorLine = true;
                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "eval", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.eval(
                    text, debugArguments, ref savedText,
                    ref tclsh, ref savedTclsh,
                    ref show, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "again", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.again(
                    interpreter, ref previous, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "help", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.help(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ihelp", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ihelp(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "usage", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.usage(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "version", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.version(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "args", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.args(
                    loopData.Args, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ainfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ainfo(
                    interpreter, interactiveHost, loopData.Code,
                    loopData.BreakpointType, loopData.BreakpointName,
                    loopData.Arguments, result, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "npinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.npinfo(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "clearq", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.clearq(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "oinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.oinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "vinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.vinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "complaint", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.complaint(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "cuinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.cuinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "testinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.testinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "toinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.toinfo(
                    interpreter, interactiveHost, loopData.Token,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tcancel", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tcancel(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tcode", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tcode(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "toldvalue", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.toldvalue(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tnewvalue", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tnewvalue(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tinfo(
                    interpreter, interactiveHost, debugArguments,
                    loopData.TraceInfo, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "stack", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.stack(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "finfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.finfo(
                    interpreter, interactiveHost, loopData.EngineFlags,
                    loopData.SubstitutionFlags, loopData.EventFlags,
                    loopData.ExpressionFlags, loopData.HeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "lfinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.lfinfo(
                    interpreter, interactiveHost, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, localHeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "frinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.frinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "einfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.einfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "cinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.cinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "eninfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.eninfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "sinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.sinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "histfile", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.histfile(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "histinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.histinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "histclear", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.histclear(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "histload", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.histload(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "histsave", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.histsave(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "hinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.hinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "iinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.iinfo(
                    interpreter, interactiveHost, result,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "fresc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.fresc(
                    ref forceCancel, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "fresh", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.fresh(
                    ref forceHalt, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "resc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.resc(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "resh", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.resh(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "rehash", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.rehash(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "deval", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                localErrorLine = 0;

                Commands.deval(
                    interpreter, debugArguments, ref localCode,
                    ref localResult, ref localErrorLine);

                haveErrorLine = true;
                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dsubst", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dsubst(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "paused", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.paused(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "pause", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.pause(
                    interpreter, debugArguments, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "unpause", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.unpause(
                    interpreter, debugArguments, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "suspend", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.suspend(
                    interpreter, loopData.Debug, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "resume", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.resume(
                    interpreter, loopData.Debug, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "about", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.about(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "chans", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.chans(
                    interpreter, interactiveHost as IStreamHost,
                    debugArguments, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "init", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.init(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dpath", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dpath(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "cancel", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.cancel(
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "test", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult) ||
                CheckCommand(
                    interpreter, text, "ptest", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                localErrorLine = 0;

                Commands.test(
                    interpreter, debugArguments, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, ref localCode,
                    ref localResult, ref localErrorLine);

                haveErrorLine = true;
                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "trustclr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.trustclr(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "trustdir", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.trustdir(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "testdir", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.testdir(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "purge", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.purge(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "restc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.restc(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "restm", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.restm(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "restv", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.restv(
                    interpreter, loopData.Args, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "vout", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.vout(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "relimit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.relimit(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "rlimit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.rlimit(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ntypes", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ntypes(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "nflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.nflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "hflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.hflags(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Debug, ref localHeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dflags(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Debug, ref localDetailFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "lhflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.lhflags(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Debug, ref localHeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ldflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ldflags(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Debug, ref localDetailFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "cflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.cflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dcflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dcflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "scflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.scflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dscflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dscflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "iflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.iflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "diflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.diflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "itflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.itflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dtflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dtflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "spaflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.spaflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "pflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.pflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "sprflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.sprflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ceflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ceflags(
                    interpreter, debugArguments,
                    ref localEngineFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "seflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.seflags(
                    interpreter, debugArguments,
                    ref localEngineFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "evflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.evflags(
                    interpreter, debugArguments,
                    ref localEventFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "exflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.exflags(
                    interpreter, debugArguments,
                    ref localExpressionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ieflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ieflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "ievflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.ievflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "iexflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.iexflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "leflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.leflags(
                    interpreter, debugArguments,
                    ref localEngineFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "levflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.levflags(
                    interpreter, debugArguments, ref localEventFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "lexflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.lexflags(
                    interpreter, debugArguments,
                    ref localExpressionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "sflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.sflags(
                    interpreter, debugArguments,
                    ref localSubstitutionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "isflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.isflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "izflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.izflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "dizflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.dizflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "lsflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.lsflags(
                    interpreter, debugArguments,
                    ref localSubstitutionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "step", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.step(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "style", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.style(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "canexit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.canexit(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "show", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.show(
                    interpreter, interactiveHost, debugArguments,
                    loopData, result, localHeaderFlags, localCode,
                    localResult, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "overr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.overr(
                    interpreter, interactiveHost, debugArguments,
                    ref show, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "prevr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.prevr(
                    interpreter, interactiveHost, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "nextr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.nextr(
                    interpreter, interactiveHost, localCode,
                    localResult, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "fresr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.fresr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult, ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "resr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.resr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult, ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "clearr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.clearr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "nullr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.nullr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "copyr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.copyr(
                    interactiveHost, loopData.Code, result, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "setr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.setr(
                    interactiveHost, localCode, localResult,
                    ref show, ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "mover", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.mover(
                    interactiveHost, ref show, ref localCode,
                    ref localResult, ref code, ref result);

                loopData.SetCode(code);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "lrinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.lrinfo(
                    interpreter, interactiveHost, localCode,
                    localResult, localErrorLine, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "grinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.grinfo(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Code, result, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "rinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.rinfo(
                    interpreter, interactiveHost, localCode,
                    localResult, localErrorLine, loopData.Code, result,
                    ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "sresult", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.sresult(
                    interpreter, interactiveHost, debugArguments,
                    localCode, localResult, localErrorLine,
                    loopData.Code, result, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tclsh", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tclsh(interpreter,
                    interactiveHost, ref tclsh, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "tclinterp", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.tclinterp(
                    debugArguments, ref tclInterpName, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (CheckCommand(
                    interpreter, text, "queue", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref accessError, ref localCode, ref localResult))
            {
                if (accessError != null)
                {
                    WriteAccessError(interactiveHost, ref accessError);
                    return true; /* COMMAND PROCESSED */
                }

                Commands.queue(
                    interpreter, loopData, refresh, noCommand,
                    trace, loopData.Debug, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, loopData.ClientData,
                    forceCancel, forceHalt, ref interactiveHost,
                    ref savedText, ref done, ref previous,
                    ref canceled, ref text, ref notReady,
                    ref parseError, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            #endregion
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////

            return false; /* COMMAND NOT PROCESSED */
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Interactive Command Implementation Class
#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// This class contains the implementation methods for the built-in
        /// interactive shell commands.
        /// </summary>
        [ObjectId("bc7c0ee9-8677-4416-b1c4-e47437b209cb")]
        internal static class Commands
        {
            #region Public Interactive Command Methods
            #region Special Interactive Command Methods
            /// <summary>
            /// This method implements the "nop" interactive command, which
            /// performs no action and exists only to carry command flags.
            /// </summary>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void nop() /* NOTE: Needed for flags. */
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "go" interactive command, which,
            /// when debugging, exits the nested interactive loop so that script
            /// evaluation can resume; otherwise, an error is reported.
            /// </summary>
            /// <param name="debug">
            /// Non-zero if the interpreter is currently being debugged.
            /// </param>
            /// <param name="done">
            /// Upon return, set to non-zero to indicate that the nested
            /// interactive loop should be exited.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void go(
                bool debug,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: In debug mode, we simply break out of
                //       the loop; otherwise, an error message
                //       is displayed.
                //
                if (debug)
                {
                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;

                    //
                    // NOTE: Set the "done" flag for this
                    //       nested interactive loop now.
                    //       This code used to simply use
                    //       a C# "break" statement here;
                    //       however, this is much cleaner
                    //       because it permits the extra
                    //       tasks performed at the bottom
                    //       of this loop to be completed.
                    //       Also, it allows the code for
                    //       this interactive command to
                    //       reside outside of the main
                    //       InteractiveLoop method.
                    //
                    done = true;
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}go\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "run" interactive command, which,
            /// when debugging, disables all debugging features and exits the
            /// nested interactive loop so that the script can run at full speed;
            /// otherwise, an error is reported.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interpreter is currently being debugged.
            /// </param>
            /// <param name="done">
            /// Upon return, set to non-zero to indicate that the nested
            /// interactive loop should be exited.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the result of the interactive command or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void run(
                Interpreter interpreter,
                bool debug,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In debug mode, we simply disable further
                //       stepping and break out of the loop;
                //       otherwise, an error message is displayed.
                //
                if (debug)
                {
                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, false,
                            ref localDebugger, ref localResult))
                    {
                        //
                        // FIXME: Yes, this is somewhat confusing.
                        //        Why does the "#run" command call
                        //        the IDebugger.Reset method?
                        //
                        //        From the perspective of the
                        //        IDebugger interface itself, the
                        //        Reset method clears all the
                        //        internal debugging state
                        //        (basically resetting it to null
                        //        and zero).
                        //
                        //        However, from the perspective of
                        //        the interactive loop, this command
                        //        (which is named "#run") is used to
                        //        disable all debugging features and
                        //        run the script being evaluated at
                        //        full speed.
                        //
                        localCode = localDebugger.Reset(
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Set the "done" flag for this
                            //       nested interactive loop now.
                            //       This code used to simply use
                            //       a C# "break" statement here;
                            //       however, this is much cleaner
                            //       because it permits the extra
                            //       tasks performed at the bottom
                            //       of this loop to be completed.
                            //       Also, it allows the code for
                            //       this interactive command to
                            //       reside outside of the main
                            //       InteractiveLoop method.
                            //
                            done = true;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}run\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "break" interactive command, which
            /// breaks into the debugger by starting a nested interactive loop.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.
            /// </param>
            /// <param name="token">
            /// The script token associated with the current breakpoint, if any.
            /// </param>
            /// <param name="traceInfo">
            /// The trace information associated with the current breakpoint, if
            /// any.
            /// </param>
            /// <param name="localEngineFlags">
            /// The engine flags to use when entering the nested interactive
            /// loop.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// The substitution flags to use when entering the nested
            /// interactive loop.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use when entering the nested interactive
            /// loop.
            /// </param>
            /// <param name="localExpressionFlags">
            /// The expression flags to use when entering the nested interactive
            /// loop.
            /// </param>
            /// <param name="localHeaderFlags">
            /// The header flags to use when entering the nested interactive
            /// loop.
            /// </param>
            /// <param name="clientData">
            /// The client data associated with the interactive command, if any.
            /// </param>
            /// <param name="arguments">
            /// The arguments associated with the interactive command, if any.
            /// </param>
            /// <param name="done">
            /// Upon return, set to non-zero to indicate that the nested
            /// interactive loop should be exited.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the result of the interactive command or
            /// error information.
            /// </param>
            /// <param name="code">
            /// Upon return, receives the overall return code to propagate to
            /// the caller.
            /// </param>
            /// <param name="result">
            /// Upon return, receives the overall result to propagate to the
            /// caller.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void _break(
                Interpreter interpreter,
                ArgumentList debugArguments,
                IToken token,
                ITraceInfo traceInfo,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                HeaderFlags localHeaderFlags,
                IClientData clientData,
                ArgumentList arguments,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
#if DEBUGGER
                IDebugger localDebugger = null;

                if (Engine.CheckDebugger(interpreter, false,
                        ref localDebugger, ref localResult))
                {
#if PREVIOUS_RESULT
                    //
                    // NOTE: At this point, the result of the
                    //       previous command may still be
                    //       untouched and will be displayed
                    //       verbatim upon entry into the
                    //       interactive loop.
                    //
                    localResult = Result.Copy(
                        Interpreter.GetPreviousResult(interpreter),
                        ResultFlags.CopyObject); /* COPY */
#endif

                    //
                    // NOTE: Break into the debugger by
                    //       starting a nested interactive
                    //       loop.
                    //
                    localCode = DebuggerOps.Breakpoint(
                        localDebugger, interpreter,
                        new InteractiveLoopData(localCode,
                        BreakpointType.Demand, debugArguments[0],
                        token, traceInfo, localEngineFlags,
                        localSubstitutionFlags, localEventFlags,
                        localExpressionFlags, localHeaderFlags,
                        clientData, arguments), ref localResult);

                    //
                    // FIXME: If there were no other failures in
                    //        the interactive loop, perhaps we
                    //        should reflect the previous result?
                    //        Better logic here may be needed.
                    //
                    if ((localCode == ReturnCode.Ok) &&
                        (localResult != null))
                    {
                        localCode = localResult.ReturnCode;
                    }
                    else if (interpreter.ActiveInteractiveLoops > 1)
                    {
                        //
                        // BUGFIX: If the interpreter has been
                        //         halted then we need to break
                        //         out of this loop and any
                        //         nested interactive loops
                        //         (except the outermost one).
                        //
                        result = localResult;
                        code = localCode;

                        //
                        // NOTE: Set the "done" flag for this
                        //       nested interactive loop now.
                        //       This code used to simply use
                        //       a C# "break" statement here;
                        //       however, this is much cleaner
                        //       because it permits the extra
                        //       tasks performed at the bottom
                        //       of this loop to be completed.
                        //       Also, it allows the code for
                        //       this interactive command to
                        //       reside outside of the main
                        //       InteractiveLoop method.
                        //
                        done = true;
                    }
                }
                else
                {
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "halt" interactive command, which,
            /// when debugging, halts evaluation and exits the interactive
            /// loop(s) returning failure to the caller; otherwise, an error is
            /// reported.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interpreter is currently being debugged.
            /// </param>
            /// <param name="done">
            /// Upon return, set to non-zero to indicate that the nested
            /// interactive loop should be exited.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the result of the interactive command or
            /// error information.
            /// </param>
            /// <param name="code">
            /// Upon return, receives the overall return code to propagate to
            /// the caller.
            /// </param>
            /// <param name="result">
            /// Upon return, receives the overall result to propagate to the
            /// caller.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void halt(
                Interpreter interpreter,
                bool debug,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: In debug mode, we simply break out of
                //       the loop and return failure to the
                //       caller; otherwise, an error message
                //       is displayed.
                //
                if (debug)
                {
                    //
                    // NOTE: Prevent further trips through the
                    //       interpreter and the interactive
                    //       loop(s).
                    //
                    localResult = Result.Copy(
                        Engine.InterpreterHaltedError,
                        ResultFlags.CopyValue);

                    localCode = Engine.HaltEvaluate(
                        interpreter, localResult,
                        CancelFlags.InteractiveManualHalt,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        result = localResult; /* TRANSFER */
                        code = ReturnCode.Error;

                        //
                        // NOTE: Set the "done" flag for this
                        //       nested interactive loop now.
                        //       This code used to simply use
                        //       a C# "break" statement here;
                        //       however, this is much cleaner
                        //       because it permits the extra
                        //       tasks performed at the bottom
                        //       of this loop to be completed.
                        //       Also, it allows the code for
                        //       this interactive command to
                        //       reside outside of the main
                        //       InteractiveLoop method.
                        //
                        done = true;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}halt\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "done" interactive command, which
            /// exits the nested interactive loop and returns the specified (or
            /// current) return code and result to the caller.
            /// </summary>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional second and third elements specify the return code and
            /// result to return to the caller.
            /// </param>
            /// <param name="done">
            /// Upon return, set to non-zero to indicate that the nested
            /// interactive loop should be exited.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives error information on failure.
            /// </param>
            /// <param name="code">
            /// Upon return, receives the return code to return to the caller.
            /// </param>
            /// <param name="result">
            /// Upon return, receives the result to return to the caller.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void _done(
                ArgumentList debugArguments,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: We simply break out of the loop and return
                //       the specified (or current) return code and
                //       result to the caller.
                //
                localCode = ReturnCode.Ok;

                //
                // NOTE: Check for the optional argument containing
                //       the return code.
                //
                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParse(
                        typeof(ReturnCode), debugArguments[1],
                        true, true);

                    //
                    // NOTE: Was the argument a valid return code?
                    //
                    if (enumValue is ReturnCode)
                    {
                        code = (ReturnCode)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(null,
                            "return code value", debugArguments[1],
                            Enum.GetNames(typeof(ReturnCode)), null,
                            null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Check for the optional argument containing
                //       the new result.
                //
                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3))
                {
                    result = debugArguments[2];
                }

                //
                // NOTE: If we succeeded in all the previous steps,
                //       bail out of this nested interactive loop.
                //
                if (localCode == ReturnCode.Ok)
                {
                    //
                    // NOTE: Set the "done" flag for this
                    //       nested interactive loop now.
                    //       This code used to simply use
                    //       a C# "break" statement here;
                    //       however, this is much cleaner
                    //       because it permits the extra
                    //       tasks performed at the bottom
                    //       of this loop to be completed.
                    //       Also, it allows the code for
                    //       this interactive command to
                    //       reside outside of the main
                    //       InteractiveLoop method.
                    //
                    done = true;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "exact" interactive command, which
            /// toggles exact matching of interactive command names.
            /// </summary>
            /// <param name="exact">
            /// On input, the current exact command name matching setting; upon
            /// return, receives the toggled value.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new exact matching
            /// setting.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void _exact(
                ref bool exact,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                exact = !exact;

                localResult = String.Format(
                    "exact matching {0}", exact ? "enabled" : "disabled");

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "exit" interactive command, which
            /// causes the process to exit.
            /// </summary>
            /// <param name="exit">
            /// Upon return, set to non-zero to indicate that the process should
            /// exit.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating that an interactive
            /// exit was requested.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void exit(
                ref bool exit,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Exit the hard way.
                //
                exit = true;

                localResult = "interactive exit";
                localCode = ReturnCode.Ok;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////

            #region Normal Interactive Command Methods
            /// <summary>
            /// This method implements the "cmd" interactive command, which
            /// launches a child operating system command processor (i.e.
            /// ComSpec) shell for debugging.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// elements at index one and beyond comprise the command line to
            /// execute.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use when executing the child process.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void cmd(
                Interpreter interpreter,
                ArgumentList debugArguments,
                EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Launch a child "cmd.exe" (really ComSpec) shell for
                //       debugging.
                //
                string fileName = GlobalConfiguration.GetValue(
                    EnvVars.ComSpec, ConfigurationFlags.InteractiveOps |
                    ConfigurationFlags.NativePathValue);

                if (!String.IsNullOrEmpty(fileName))
                {
                    //
                    // HACK: Create a command line that can be understood by the
                    //       operating system command processor, using its quoting
                    //       rules, not Tcl's.  Unfortunately, all the underlying
                    //       arguments for this "special" interactive command have
                    //       already been parsed from the raw input string using
                    //       Tcl's standard list quoting rules; therefore, a great
                    //       deal of care must be taken by the interactive user to
                    //       construct a command line that can survive both sets of
                    //       quoting rules.  Also, we must be sure to skip the
                    //       interactive command name itself (i.e. there was a long
                    //       standing bug here because we were not doing that).
                    //
                    // EXAMPLE:
                    //
                    //       #cmd /c [info nameofexecutable] -eval "set x 2; puts $x"
                    //       (this requires "#isflags +Commands")
                    //
                    string execArguments;
                    bool done = false;

                    execArguments = RuntimeOps.BuildCommandLine(
                        interpreter, ArgumentList.GetRangeAsStringList(
                        debugArguments, 1), null, false, false, false,
                        ref done, ref localResult);

                    if (done)
                        return;

                    if (execArguments != null)
                    {
                        localCode = ProcessOps.ExecuteProcess(
                            interpreter, fileName, execArguments, null,
                            localEventFlags, ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = String.Empty;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot execute shell, environment variable \"{0}\" not set",
                        EnvVars.ComSpec);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "intsec" interactive command, which
            /// enables or disables security for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugHost">
            /// The debug host used to display the result of the operation, if
            /// any.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional second element specifies whether security should be
            /// enabled and the optional third element specifies whether the
            /// change should be forced.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives null on success or error information on
            /// failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void intsec(
                Interpreter interpreter,
                IDebugHost debugHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;
                localResult = null;

                bool savedSecurity = interpreter.HasSecurity();
                bool security = !savedSecurity; /* TOGGLE */

                if (debugArguments.Count >= 2)
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref security,
                        ref localResult);
                }

                bool force = false;

                if (debugArguments.Count >= 3)
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref force,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    localCode = ScriptOps.EnableOrDisableSecurity(
                        interpreter, security, force, ref localResult);

                    if ((localCode == ReturnCode.Ok) && (debugHost != null))
                    {
                        debugHost.WriteResult(localCode, String.Format(
                            "Interpreter {0} security {1}{2}{3} while {4}.",
                            FormatOps.InterpreterNoThrow(interpreter),
                            security == savedSecurity ? "still " : "now ",
                            security ? "enabled" : "disabled",
                            force ? " forcibly" : String.Empty,
                            interpreter.InternalIsSafe() ?
                                "\"safe\"" : "\"unsafe\""), true);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "fmkeys" interactive command, which
            /// fetches and merges the key ring for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugHost">
            /// The debug host used to display the result of the operation, if
            /// any.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional second element specifies whether the operation should be
            /// forced.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives null on success or error information on
            /// failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void fmkeys(
                Interpreter interpreter,
                IDebugHost debugHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;
                localResult = null;

                bool force = false;

                if (debugArguments.Count >= 2)
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref force,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    localCode = ScriptOps.FetchAndMergeKeyRing(
                        interpreter, force, ref localResult);

                    if ((localCode == ReturnCode.Ok) && (debugHost != null))
                    {
                        debugHost.WriteResult(localCode, String.Format(
                            "Interpreter {0} key ring {1}fetched and " +
                            "merged while {2}.",
                            FormatOps.InterpreterNoThrow(interpreter),
                            force ? "forcibly " : String.Empty,
                            interpreter.InternalIsSafe() ?
                                "\"safe\"" : "\"unsafe\""), true);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tclshrc" interactive command, which
            /// launches a text editor for editing the shell startup file.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="fileSystemHost">
            /// The file system host used to locate the shell startup file.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use when executing the editor process.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tclshrc(
                Interpreter interpreter,
                IFileSystemHost fileSystemHost,
                ArgumentList debugArguments,
                EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Launch a text editor (e.g. "notepad.exe") for editing the
                //       shell startup file (e.g. "~/tclshrc.tcl").
                //
                string fileName = GlobalConfiguration.GetValue(
                    EnvVars.Editor, ConfigurationFlags.InteractiveOps |
                    ConfigurationFlags.NativePathValue);

                //
                // NOTE: The editor environment variable is not set.  On Windows,
                //       just default to using "notepad[.exe]".
                //
                if (String.IsNullOrEmpty(fileName) &&
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    fileName = EnvVars.EditorValue;
                }

                if (!String.IsNullOrEmpty(fileName))
                {
                    string name = TclVars.Core.RunCommandsFileName;

                    bool direct = (interpreter != null) ?
                        interpreter.IsInitializeDirect() : false;

                    ScriptFlags scriptFlags = ScriptFlags.Interactive |
                        ScriptFlags.ApplicationOptionalFile |
                        ScriptFlags.UserOptionalFile;

                    IClientData clientData = ClientData.Empty;
                    ResultList errors = null;

                    localCode = ScriptOps.GetStartup(
                            interpreter, fileSystemHost, name,
                            direct, ref scriptFlags, ref clientData,
                            ref localResult, ref errors);

                    if (localCode == ReturnCode.Ok)
                    {
                        string text = localResult;

                        if (!String.IsNullOrEmpty(text))
                        {
                            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.File, true))
                            {
                                if (PathOps.IsRemoteUri(text) || File.Exists(text))
                                {
                                    if (debugArguments.Count > 1)
                                        debugArguments.Insert(1, text);
                                    else
                                        debugArguments.Add(text);

                                    string execArguments;
                                    bool done = false;

                                    execArguments = RuntimeOps.BuildCommandLine(
                                        interpreter, ArgumentList.GetRangeAsStringList(
                                        debugArguments, 1), null, false, false, false,
                                        ref done, ref localResult);

                                    if (done)
                                        return;

                                    if (execArguments != null)
                                    {
                                        localCode = ProcessOps.ExecuteProcess(
                                            interpreter, fileName, execArguments, null,
                                            localEventFlags, true, ref localResult);

                                        if (localCode == ReturnCode.Ok)
                                            localResult = String.Empty;
                                    }
                                    else
                                    {
                                        localCode = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    localResult = String.Format(
                                        "the provided \"{0}\" script file \"{1}\" is not " +
                                        "a valid remote uri and does not exist locally",
                                        name, text);

                                    localCode = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                localResult = String.Format(
                                    "the \"{0}\" script is not a file",
                                    name);

                                localCode = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            localResult = String.Format(
                                "the \"{0}\" script is invalid or has no content",
                                name);

                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = errors;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot execute editor, environment variable \"{0}\" not set",
                        EnvVars.Editor);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "website" interactive command, which
            /// opens the assembly web site using the default shell handler.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use when launching the web site.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void website(
                Interpreter interpreter,
                EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                Uri uri = SharedAttributeOps.GetAssemblyUri(
                    GlobalState.GetAssembly());

                if (uri != null)
                {
                    localCode = ProcessOps.ShellExecuteProcess(
                        interpreter, uri.ToString(), null, null,
                        localEventFlags, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = String.Empty;
                }
                else
                {
                    localResult = "uri not available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "reset" interactive command, which
            /// resets the internal debugging state to its initial defaults.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the debugger was reset
            /// on success or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void reset(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                IDebugger localDebugger = null;

                if (Engine.CheckDebugger(
                        interpreter, true, ref localDebugger, ref localResult))
                {
                    //
                    // FIXME: Yes, this is somewhat confusing.  Why not call the
                    //        IDebugger.Reset method here?
                    //
                    //        From the perspective of the IDebugger interface itself,
                    //        the Initialize method sets the internal debugging state
                    //        to its initial defaults and the Reset method clears all
                    //        the internal debugging state (basically resetting it
                    //        to null and zero).
                    //
                    //        However, from the perspective of the interactive loop,
                    //        this command (which is named "#reset") is used to reset
                    //        the internal debugging state of the IDebugger interface
                    //        to its initial default state.  Without this command, it
                    //        would be very difficult to re-enable debugging features
                    //        after using the "#suspend" command followed by the "#go"
                    //        command.
                    //
                    localCode = localDebugger.Initialize(ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = "debugger reset";
                }
                else
                {
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "useattach" interactive command,
            /// which toggles the attach setting of the default interactive host.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host whose attach setting should be toggled.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new attach setting
            /// on success or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void useattach(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    //
                    // NOTE: Get the current attach setting and then toggle it.
                    //
                    defaultHost.UseAttach = !defaultHost.UseAttach;

                    localResult = String.Format(
                        "attach {0}",
                        ConversionOps.ToEnabled(defaultHost.UseAttach));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "useforce" interactive command,
            /// which toggles the force setting of the default interactive host.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host whose force setting should be toggled.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new force setting
            /// on success or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void useforce(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    //
                    // NOTE: Get the current force setting and then toggle it.
                    //
                    defaultHost.UseForce = !defaultHost.UseForce;

                    localResult = String.Format(
                        "force {0}",
                        ConversionOps.ToEnabled(defaultHost.UseForce));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "color" interactive command, which
            /// toggles colorized output for the interactive host.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host whose color setting should be toggled.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new color setting
            /// on success or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void color(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IColorHost colorHost = interactiveHost as IColorHost;

                if (colorHost != null)
                {
                    //
                    // NOTE: Get the current color setting and then toggle it.
                    //
                    colorHost.NoColor = !colorHost.NoColor;

                    localResult = String.Format(
                        "color {0}",
                        ConversionOps.ToEnabled(!colorHost.NoColor));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "exceptions" interactive command,
            /// which toggles the display of exception details by the default
            /// interactive host.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host whose exceptions setting should be toggled.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new exceptions
            /// setting on success or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void exceptions(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    //
                    // NOTE: Get the current exceptions setting and then toggle it.
                    //
                    defaultHost.Exceptions = !defaultHost.Exceptions;

                    localResult = String.Format(
                        "exceptions {0}",
                        ConversionOps.ToEnabled(defaultHost.Exceptions));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "testgc" interactive command, which
            /// starts or stops the test garbage collection thread.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// second element specifies whether the test garbage collection
            /// thread should be started.
            /// </param>
            /// <param name="startedGcThread">
            /// Upon return, receives non-zero if the test garbage collection
            /// thread was started or zero if it was stopped.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void testgc(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref bool startedGcThread,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    bool start = false;

                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref start,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            if (Interpreter.IsDeletedOrDisposed(
                                    interpreter, false, ref localResult))
                            {
                                localCode = ReturnCode.Error;
                            }
                            else
                            {
                                if (start)
                                {
                                    localCode = interpreter.StartTestGcThread(
                                        true, true, true, ref localResult);

                                    if (localCode == ReturnCode.Ok)
                                        startedGcThread = true;
                                }
                                else
                                {
                                    localCode = interpreter.InterruptTestGcThread(
                                        null, interpreter.InternalNoThreadAbort,
                                        true, ref localResult);

                                    if (localCode == ReturnCode.Ok)
                                        startedGcThread = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}testgc start\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "hcancel" interactive command, which
            /// queues a work item to cancel the interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating whether the work item
            /// was queued or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void hcancel(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                try
                {
                    IAnyPair<int, bool> anyPair = new AnyPair<int, bool>(
                        TestOps.hostWorkItemDelay, TestOps.hostWorkItemForce);

                    if (Engine.QueueWorkItem(
                            interpreter, TestOps.HostCancelThreadStart,
                            anyPair, ThreadOps.GetQueueFlags(false)))
                    {
                        localResult = "queued host cancel work item";
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = "failed to queue host cancel work item";
                        localCode = ReturnCode.Error;
                    }
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "hexit" interactive command, which
            /// queues a work item to exit the interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating whether the work item
            /// was queued or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void hexit(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                try
                {
                    IAnyPair<int, bool> anyPair = new AnyPair<int, bool>(
                        TestOps.hostWorkItemDelay, TestOps.hostWorkItemForce);

                    if (Engine.QueueWorkItem(
                            interpreter, TestOps.HostExitThreadStart,
                            anyPair, ThreadOps.GetQueueFlags(false)))
                    {
                        localResult = "queued host exit work item";
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = "failed to queue host exit work item";
                        localCode = ReturnCode.Error;
                    }
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "stable" interactive command, which
            /// queries or sets whether the update check uses the stable release
            /// path.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional second element specifies whether the stable update path
            /// should be used.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the update path and query value on success
            /// or error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void stable(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    bool stable = false;

                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref stable,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        string localValue = RuntimeOps.GetUpdatePathAndQuery(
                            GlobalState.GetAssemblyUpdateVersion(), stable,
                            null);

                        Result localError = null;

                        localCode = interpreter.SetVariableValue2(
                            VariableFlags.GlobalOnly, Vars.Platform.Name,
                            Vars.Platform.UpdatePathAndQueryName, localValue,
                            ref localError);

                        if (localCode == ReturnCode.Ok)
                            localResult = localValue;
                        else
                            localResult = localError;
                    }
                }
                else
                {
                    Result localValue = null;
                    Result localError = null;

                    localCode = interpreter.GetVariableValue2(
                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                        Vars.Platform.UpdatePathAndQueryName,
                        ref localValue, ref localError);

                    if (localCode == ReturnCode.Ok)
                        localResult = localValue;
                    else
                        localResult = localError;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "check" interactive command, which
            /// checks for available updates to the script engine.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional trailing elements specify the various options that
            /// control how the update check is performed.
            /// </param>
            /// <param name="localEngineFlags">
            /// The engine flags to use when evaluating the update check script.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// The substitution flags to use when evaluating the update check
            /// script.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use when evaluating the update check script.
            /// </param>
            /// <param name="localExpressionFlags">
            /// The expression flags to use when evaluating the update check
            /// script.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the result of the update check or error
            /// information.
            /// </param>
            /// <param name="localErrorLine">
            /// Upon return, receives the line number where a script error
            /// occurred, or zero if none.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void check(
                Interpreter interpreter,
                ArgumentList debugArguments,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult,
                ref int localErrorLine
                )
            {
                localCode = ReturnCode.Ok;

                bool wantScripts = false; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref wantScripts,
                        ref localResult);
                }

                bool quiet = true; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref quiet,
                        ref localResult);
                }

                bool prompt = true; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref prompt,
                        ref localResult);
                }

                bool automatic = true; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref automatic,
                        ref localResult);
                }

                //
                // NOTE: Default to checking for new releases only.
                //
                ActionType actionType = ActionType.Default;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 6) &&
                    !String.IsNullOrEmpty(debugArguments[5]))
                {
                    object enumValue = EnumOps.TryParse(
                        typeof(ActionType), debugArguments[5],
                        true, true);

                    if (enumValue is ActionType)
                    {
                        actionType = (ActionType)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(
                            null, "action type value", debugArguments[5],
                            Enum.GetNames(typeof(ActionType)), null, null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Default to the Windows setup packages.
                //
                ReleaseType releaseType = ReleaseType.Setup;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 7) &&
                    !String.IsNullOrEmpty(debugArguments[6]))
                {
                    object enumValue = EnumOps.TryParse(
                        typeof(ReleaseType), debugArguments[6],
                        true, true);

                    if (enumValue is ReleaseType)
                    {
                        releaseType = (ReleaseType)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(
                            null, "release type value", debugArguments[6],
                            Enum.GetNames(typeof(ReleaseType)), null, null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Default to the script engine itself.
                //
                UpdateType updateType = UpdateType.Engine;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 8) &&
                    !String.IsNullOrEmpty(debugArguments[7]))
                {
                    object enumValue = EnumOps.TryParse(
                        typeof(UpdateType), debugArguments[7],
                        true, true);

                    if (enumValue is UpdateType)
                    {
                        updateType = (UpdateType)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(
                            null, "release type value", debugArguments[7],
                            Enum.GetNames(typeof(UpdateType)), null, null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Evaluate the script we use to check for updates
                //       to the script engine.  If the proc has been
                //       redefined, this may not actually do anything.
                //
                if (localCode == ReturnCode.Ok)
                {
                    localCode = ShellOps.CheckForUpdate(
                        interpreter, new UpdateData((string)null,
                            actionType, releaseType, updateType,
                            wantScripts, quiet, prompt, automatic),
                        localEngineFlags, localSubstitutionFlags,
                        localEventFlags, localExpressionFlags,
                        ref localErrorLine, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "eval" interactive command, which
            /// queues the remainder of the input text to be evaluated as a
            /// normal script during the next iteration of the interactive loop.
            /// </summary>
            /// <param name="text">
            /// The raw interactive input text being processed.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// elements at index one and beyond comprise the script to be
            /// evaluated.
            /// </param>
            /// <param name="savedText">
            /// Upon return, receives the input text to evaluate during the next
            /// iteration of the interactive loop.
            /// </param>
            /// <param name="tclsh">
            /// On input, the current tclsh emulation mode setting; upon return,
            /// it may be set to false so that the saved text is not evaluated
            /// using tclsh emulation mode.
            /// </param>
            /// <param name="savedTclsh">
            /// Upon return, receives the saved tclsh emulation mode setting to
            /// be restored after the saved text is evaluated, or null if none.
            /// </param>
            /// <param name="show">
            /// Upon return, set to false to indicate that the result should not
            /// be displayed.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void eval(
                string text,
                ArgumentList debugArguments,
                ref string savedText,
                ref bool tclsh,
                ref bool? savedTclsh,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    //
                    // NOTE: Make sure that the command to be evaluated is not an
                    //       interactive one.  This restriction may seem somewhat
                    //       arbitrary; however, it does prevent an endless input
                    //       loop involving *this* interactive command.
                    //
                    Argument debugArgument = debugArguments[1];

                    if (!ShellOps.LooksLikeAnyInteractiveCommand(debugArgument))
                    {
                        //
                        // NOTE: Try to strip the leading "#eval " command from
                        //       the original input text.
                        //
                        string localText = ShellOps.StripInteractiveCommand(text);

                        if (!String.IsNullOrEmpty(localText))
                        {
                            //
                            // NOTE: We are not actually doing anything, do not
                            //       display the result.
                            //
                            show = false;

                            //
                            // NOTE: Stuff the [now modified] command into the
                            //       saved input text for use during the next
                            //       iteration of the primary loop.
                            //
                            savedText = localText;

                            //
                            // NOTE: Make sure that the saved command text is not
                            //       evaluated using the "tclsh emulation mode"
                            //       by saving the associated flag and then
                            //       forcing it to false.  The flag will be
                            //       restored after the saved command text is
                            //       evaluated.
                            //
                            savedTclsh = tclsh;
                            tclsh = false;
                        }
                        else
                        {
                            localResult = "nothing to evaluate";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = "evaluation of interactive commands is disabled";
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}eval arg ?arg ...?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "again" interactive command, which
            /// replays the previously entered interactive input.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="previous">
            /// On input, non-zero if playback of the previous interactive input
            /// is enabled; upon return, it is set to false so that this command
            /// is not recorded as the previous interactive input.
            /// </param>
            /// <param name="show">
            /// Upon return, set to false to indicate that the result should not
            /// be displayed.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives error information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void again(
                Interpreter interpreter,
                ref bool previous,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                if (previous)
                {
                    //
                    // NOTE: Do not record this command (i.e. "#again") as the
                    //       previous interactive input.
                    //
                    previous = false;

                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, false,
                            ref localDebugger, ref localResult))
                    {
                        string localText = interpreter.PreviousInteractiveInput;

                        if (localText != null)
                            localText = localText.Trim();

                        if (!String.IsNullOrEmpty(localText))
                        {
                            //
                            // NOTE: We are not actually doing anything, do not
                            //       display the result.
                            //
                            show = false;

                            //
                            // NOTE: Set the debugger command to the previously
                            //       entered interactive command.
                            //
                            localDebugger.Command = localText;
                        }
                        else
                        {
                            localResult = "no previous interactive input exists";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = "playback of previous interactive input is disabled";
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "help" interactive command, which
            /// displays help for the interactive commands.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command, used
            /// to select the help topics to display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the help text or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void help(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                int stable = 0;

                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]) &&
                    (Value.GetInteger2(
                        debugArguments[1], ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo,
                        ref stable) == ReturnCode.Ok))
                {
                    string suffix = null;

                    if ((debugArguments.Count >= 3) &&
                        !String.IsNullOrEmpty(debugArguments[2]))
                    {
                        suffix = debugArguments[2];
                    }

                    if ((stable > 0) && (stable < 2))
                    {
                        string localValue = RuntimeOps.GetUpdatePathAndQuery(
                            GlobalState.GetAssemblyUpdateVersion(), null,
                            suffix);

                        localCode = interpreter.SetVariableValue2(
                            VariableFlags.GlobalOnly, Vars.Platform.Name,
                            Vars.Platform.UpdatePathAndQueryName, localValue,
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = String.Empty;
                    }
                    else
                    {
                        localResult = "invalid, unstable argument";
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Invoke (interactive) command help using
                    //       exactly the specified arguments.
                    //
                    localCode = HelpOps.WriteInteractiveHelp(
                        interpreter, debugArguments, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ihelp" interactive command, which
            /// displays help for the specified topic while excluding
            /// interactive-only commands and groups.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional second element specifies the help topic to display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the help text or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ihelp(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Invoke (interactive) command help for the specified
                //       topic using options that avoid looking up commands
                //       and groups that are interactive-only (e.g. built-in
                //       interactive commands and their groups).
                //
                string topic = null;

                if ((debugArguments != null) &&
                    (debugArguments.Count >= 2))
                {
                    topic = StringOps.NullIfEmpty(debugArguments[1]);
                }

                TextFlags textFlags = HelpOps.GetDefaultTextFlags();
                bool found = false; /* NOT USED */

                localCode = HelpOps.WriteInteractiveHelp(
                    interpreter, topic, StringOps.DefaultMatchMode,
                    textFlags, false, false, false, false, true,
                    true, false, null, ref found, ref localResult);
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "usage" interactive command, which
            /// displays the command line syntax for the shell.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional trailing elements specify which portions of the usage
            /// information to display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the usage information or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void usage(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool showBanner = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showBanner,
                        ref localResult);
                }

                bool showLegalese = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showLegalese,
                        ref localResult);
                }

                bool showOptions = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showOptions,
                        ref localResult);
                }

                bool showEnvironment = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showEnvironment,
                        ref localResult);
                }

                bool compactMode = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 6) &&
                    !String.IsNullOrEmpty(debugArguments[5]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[5], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref compactMode,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    //
                    // NOTE: Show the command line syntax.
                    //
                    localCode = HelpOps.WriteUsage(
                        interpreter, null, showBanner, showLegalese,
                        showOptions, showEnvironment, compactMode,
                        ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "version" interactive command, which
            /// displays version and related information about the shell.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// optional trailing elements specify which portions of the version
            /// information to display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the version information or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void version(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool showBanner = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showBanner,
                        ref localResult);
                }

                bool showLegalese = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showLegalese,
                        ref localResult);
                }

                bool showSource = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showSource,
                        ref localResult);
                }

                bool showUpdate = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showUpdate,
                        ref localResult);
                }

                bool showContext = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 6) &&
                    !String.IsNullOrEmpty(debugArguments[5]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[5], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showContext,
                        ref localResult);
                }

                bool showPlugins = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 7) &&
                    !String.IsNullOrEmpty(debugArguments[6]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[6], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showPlugins,
                        ref localResult);
                }

                bool showCertificate = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 8) &&
                    !String.IsNullOrEmpty(debugArguments[7]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[7], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showCertificate,
                        ref localResult);
                }

                bool showOptions = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 9) &&
                    !String.IsNullOrEmpty(debugArguments[8]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[8], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref showOptions,
                        ref localResult);
                }

                bool compactMode = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 10) &&
                    !String.IsNullOrEmpty(debugArguments[9]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[9], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref compactMode,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    //
                    // NOTE: Show the detailed library version.
                    //
                    localCode = HelpOps.WriteVersion(
                        interpreter, showBanner, showLegalese, showSource,
                        showUpdate, showContext, showPlugins, showCertificate,
                        showOptions, compactMode, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "args" interactive command, which
            /// displays the command line arguments, if any, that were supplied
            /// to the shell.
            /// </summary>
            /// <param name="args">
            /// The collection of command line arguments to display, or null if
            /// none are available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the list of command line arguments or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void args(
                IEnumerable<string> args,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Show the command line arguments, if any.
                //
                if (args != null)
                {
                    localResult = StringList.MakeList(args);
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = "no shell arguments available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ainfo" interactive command, which
            /// displays detailed information about the arguments associated
            /// with the current breakpoint.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the argument information.
            /// </param>
            /// <param name="code">
            /// The return code associated with the current breakpoint.
            /// </param>
            /// <param name="breakpointType">
            /// The type of breakpoint currently being processed.
            /// </param>
            /// <param name="breakpointName">
            /// The name of the breakpoint currently being processed.
            /// </param>
            /// <param name="arguments">
            /// The list of arguments associated with the current breakpoint.
            /// </param>
            /// <param name="result">
            /// The result associated with the current breakpoint.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ainfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode code,
                BreakpointType breakpointType,
                string breakpointName,
                ArgumentList arguments,
                Result result,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteArgumentInfo(
                            interpreter, code, breakpointType,
                            breakpointName, arguments, result,
                            HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "npinfo" interactive command, which
            /// displays diagnostic information about the loaded native Tcl
            /// interpreters.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void npinfo(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NATIVE && TCL && NATIVE_PACKAGE
                NativePackage.DebugTclInterpreters(interpreter, null, true);

                localResult = String.Empty;
                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "clearq" interactive command, which
            /// clears all pending callbacks from the interpreter callback
            /// queue.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the callback queue
            /// was cleared or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void clearq(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if CALLBACK_QUEUE
                localCode = interpreter.ClearCallbackQueue(ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "callback queue cleared";
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "oinfo" interactive command, which
            /// displays detailed information about an opaque object handle.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the object information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one specifies the name of the object to
            /// display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void oinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    IObject @object = null;

                    localCode = interpreter.GetObject(
                        debugArguments[1], LookupFlags.Default,
                        ref @object, ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        IInformationHost informationHost =
                            interactiveHost as IInformationHost;

                        if ((informationHost != null) &&
                            !AppDomainOps.IsTransparentProxy(informationHost))
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteObjectInfo(
                                    interpreter, @object,
                                    HostOps.GetDetailFlags(interpreter),
                                    true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                HostOps.NoFeatureError,
                                typeof(IInformationHost).Name);

                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}oinfo name\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "vinfo" interactive command, which
            /// displays detailed information about a variable.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the variable information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one specifies the name of the variable to
            /// display and the element at index two, when present, specifies
            /// the detail flags to use.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void vinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    if (AppDomainOps.IsSame(interpreter))
                    {
                        //
                        // BUGFIX: We do not want to be prevented from examining
                        //         the variable due to script cancellation, etc.
                        //
                        // BUGFIX: This should also be exempt from lock checking.
                        //
                        VariableFlags flags = VariableFlags.NoElement |
                            VariableFlags.NoReady | VariableFlags.NoUsable;

                        IVariable variable = null;

                        localCode = interpreter.GetVariableViaResolversWithSplit(
                            debugArguments[1], ref flags, ref variable,
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                            if ((localCode == ReturnCode.Ok) &&
                                (debugArguments.Count >= 3) &&
                                !String.IsNullOrEmpty(debugArguments[2]))
                            {
                                object enumValue = EnumOps.TryParseFlags(
                                    interpreter, typeof(DetailFlags),
                                    detailFlags.ToString(), debugArguments[2],
                                    interpreter.InternalCultureInfo, true, true, true,
                                    ref localResult);

                                if (enumValue is DetailFlags)
                                {
                                    detailFlags = (DetailFlags)enumValue;

                                    localCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    localCode = ReturnCode.Error;
                                }
                            }

                            if (localCode == ReturnCode.Ok)
                            {
                                IInformationHost informationHost =
                                    interactiveHost as IInformationHost;

                                if ((informationHost != null) &&
                                    !AppDomainOps.IsTransparentProxy(informationHost))
                                {
                                    informationHost.SavePosition();

                                    if (!informationHost.WriteVariableInfo(
                                            interpreter, variable, detailFlags,
                                            true))
                                    {
                                        informationHost.WriteResultLine(
                                            ReturnCode.Error, HostWriteInfoError);
                                    }

                                    informationHost.RestorePosition(true);

                                    localResult = String.Empty;
                                    localCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    localResult = String.Format(
                                        HostOps.NoFeatureError,
                                        typeof(IInformationHost).Name);

                                    localCode = ReturnCode.Error;
                                }
                            }
                        }
                    }
                    else
                    {
                        localResult = "wrong application domain";
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}vinfo name ?flags?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "complaint" interactive command,
            /// which displays the most recent complaint recorded by the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the complaint information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void complaint(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteComplaintInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "cuinfo" interactive command, which
            /// displays custom information associated with the interactive
            /// host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the custom information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void cuinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteCustomInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dinfo" interactive command, which
            /// displays detailed information about the script debugger.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the debugger information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the detail flags
            /// to use.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                localCode = ReturnCode.Ok;

                DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(DetailFlags),
                        detailFlags.ToString(), debugArguments[1],
                        interpreter.InternalCultureInfo, true, true, true,
                        ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        detailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        informationHost.SavePosition();

                        if (!informationHost.WriteDebuggerInfo(
                                interpreter, detailFlags, true))
                        {
                            informationHost.WriteResultLine(
                                ReturnCode.Error, HostWriteInfoError);
                        }

                        informationHost.RestorePosition(true);

                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "testinfo" interactive command, which
            /// displays detailed information about the testing subsystem.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the test information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void testinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteTestInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "toinfo" interactive command, which
            /// displays detailed information about a token.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the token information.
            /// </param>
            /// <param name="token">
            /// The token whose information is to be displayed, or null if none
            /// is available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void toinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                IToken token,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Show the token, if any.
                //
                if (token != null)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if ((informationHost != null) &&
                        !AppDomainOps.IsTransparentProxy(informationHost))
                    {
                        informationHost.SavePosition();

                        if (!informationHost.WriteTokenInfo(
                                interpreter, token,
                                HostOps.GetDetailFlags(interpreter),
                                true))
                        {
                            informationHost.WriteResultLine(
                                ReturnCode.Error, HostWriteInfoError);
                        }

                        informationHost.RestorePosition(true);

                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = "no token information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tcancel" interactive command, which
            /// displays and optionally modifies the cancellation flag
            /// associated with the current trace operation.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the boolean
            /// cancellation value to apply.
            /// </param>
            /// <param name="traceInfo">
            /// The trace information for the current trace operation, or null
            /// if none is available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting cancellation value or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tcancel(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        bool cancel = false;

                        localCode = Value.GetBoolean2(
                            debugArguments[1], ValueFlags.AnyBoolean,
                            interpreter.InternalCultureInfo, ref cancel,
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                            traceInfo.Cancel = cancel;
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                        localResult = traceInfo.Cancel;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tcode" interactive command, which
            /// displays and optionally modifies the return code associated with
            /// the current trace operation.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the return code
            /// value to apply.
            /// </param>
            /// <param name="traceInfo">
            /// The trace information for the current trace operation, or null
            /// if none is available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting return code or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tcode(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        object enumValue = EnumOps.TryParse(
                            typeof(ReturnCode), debugArguments[1],
                            true, true);

                        if (enumValue is ReturnCode)
                        {
                            traceInfo.ReturnCode = (ReturnCode)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = ScriptOps.BadValue(
                                null, "return code value", debugArguments[1],
                                Enum.GetNames(typeof(ReturnCode)), null, null);

                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                        localResult = traceInfo.ReturnCode;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "toldvalue" interactive command,
            /// which displays and optionally modifies the old value associated
            /// with the current trace operation.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the old value to
            /// apply.
            /// </param>
            /// <param name="traceInfo">
            /// The trace information for the current trace operation, or null
            /// if none is available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting old value or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void toldvalue(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if (debugArguments.Count >= 2)
                        traceInfo.OldValue = debugArguments[1];

                    localResult = StringOps.GetResultFromObject(
                        traceInfo.OldValue);

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tnewvalue" interactive command,
            /// which displays and optionally modifies the new value associated
            /// with the current trace operation.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the new value to
            /// apply.
            /// </param>
            /// <param name="traceInfo">
            /// The trace information for the current trace operation, or null
            /// if none is available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting new value or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tnewvalue(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if (debugArguments.Count >= 2)
                        traceInfo.NewValue = debugArguments[1];

                    localResult = StringOps.GetResultFromObject(
                        traceInfo.NewValue);

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tinfo" interactive command, which
            /// displays detailed information about the current trace operation.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the trace information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the detail flags
            /// to use.
            /// </param>
            /// <param name="traceInfo">
            /// The trace information for the current trace operation, or null
            /// if none is available.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(DetailFlags),
                        detailFlags.ToString(), debugArguments[1],
                        interpreter.InternalCultureInfo, true, true, true,
                        ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        detailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    if (FlagOps.HasFlags(
                            detailFlags, DetailFlags.TraceCached, true) ||
                        (traceInfo != null))
                    {
                        IInformationHost informationHost =
                            interactiveHost as IInformationHost;

                        if ((informationHost != null) &&
                            !AppDomainOps.IsTransparentProxy(informationHost))
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteTraceInfo(
                                    interpreter, traceInfo, detailFlags,
                                    true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                HostOps.NoFeatureError,
                                typeof(IInformationHost).Name);

                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = "no trace information available";
                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "stack" interactive command, which
            /// displays the interpreter call stack.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the call stack.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the maximum number
            /// of frames to display; the element at index two specifies the
            /// detail flags to use; and the element at index three specifies
            /// whether to display detailed call frame information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void stack(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                CallStack callStack = interpreter.CallStack;

                if (callStack != null)
                {
                    localCode = ReturnCode.Ok;

                    int limit = 0;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        localCode = Value.GetInteger2(
                            (IGetValue)debugArguments[1], ValueFlags.AnyInteger,
                            interpreter.InternalCultureInfo, ref limit, ref localResult);
                    }

                    DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 3) &&
                        !String.IsNullOrEmpty(debugArguments[2]))
                    {
                        object enumValue = EnumOps.TryParseFlags(
                            interpreter, typeof(DetailFlags),
                            detailFlags.ToString(), debugArguments[2],
                            interpreter.InternalCultureInfo, true, true, true,
                            ref localResult);

                        if (enumValue is DetailFlags)
                        {
                            detailFlags = (DetailFlags)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localCode = ReturnCode.Error;
                        }
                    }

                    bool info = false;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 4) &&
                        !String.IsNullOrEmpty(debugArguments[3]))
                    {
                        localCode = Value.GetBoolean2(
                            debugArguments[3], ValueFlags.AnyBoolean,
                            interpreter.InternalCultureInfo, ref info,
                            ref localResult);
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        IInformationHost informationHost =
                            interactiveHost as IInformationHost;

                        if ((informationHost != null) &&
                            !AppDomainOps.IsTransparentProxy(informationHost))
                        {
                            if (info)
                            {
                                informationHost.SavePosition();

                                if (!informationHost.WriteCallStackInfo(
                                        interpreter, callStack, limit,
                                        detailFlags, true))
                                {
                                    informationHost.WriteResultLine(
                                        ReturnCode.Error, HostWriteInfoError);
                                }

                                informationHost.RestorePosition(true);
                            }
                            else
                            {
                                // informationHost.SavePosition();

                                if (!informationHost.WriteCallStack(
                                        interpreter, callStack, limit,
                                        detailFlags, true))
                                {
                                    informationHost.WriteResultLine(
                                        ReturnCode.Error, HostWriteInfoError);
                                }

                                // informationHost.RestorePosition(true);
                            }

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                HostOps.NoFeatureError,
                                typeof(IInformationHost).Name);

                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = "no call stack available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "finfo" interactive command, which
            /// displays the engine, substitution, event, expression, and header
            /// flags currently in effect.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the flag information.
            /// </param>
            /// <param name="engineFlags">
            /// The engine flags to display.
            /// </param>
            /// <param name="substitutionFlags">
            /// The substitution flags to display.
            /// </param>
            /// <param name="eventFlags">
            /// The event flags to display.
            /// </param>
            /// <param name="expressionFlags">
            /// The expression flags to display.
            /// </param>
            /// <param name="headerFlags">
            /// The header flags to display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void finfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                EngineFlags engineFlags,
                SubstitutionFlags substitutionFlags,
                EventFlags eventFlags,
                ExpressionFlags expressionFlags,
                HeaderFlags headerFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteFlagInfo(
                            interpreter, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, headerFlags,
                            HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "lfinfo" interactive command, which
            /// displays the local engine, substitution, event, expression, and
            /// header flags currently in effect.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the flag information.
            /// </param>
            /// <param name="localEngineFlags">
            /// The local engine flags to display.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// The local substitution flags to display.
            /// </param>
            /// <param name="localEventFlags">
            /// The local event flags to display.
            /// </param>
            /// <param name="localExpressionFlags">
            /// The local expression flags to display.
            /// </param>
            /// <param name="localHeaderFlags">
            /// The local header flags to display.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void lfinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                HeaderFlags localHeaderFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteFlagInfo(
                            interpreter, localEngineFlags,
                            localSubstitutionFlags, localEventFlags,
                            localExpressionFlags, localHeaderFlags,
                            HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "frinfo" interactive command, which
            /// displays detailed information about a call frame.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the call frame information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one specifies the call frame level or index and
            /// the element at index two, when present, specifies the detail
            /// flags to use.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void frinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    localCode = ReturnCode.Ok;

                    DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 3) &&
                        !String.IsNullOrEmpty(debugArguments[2]))
                    {
                        object enumValue = EnumOps.TryParseFlags(
                            interpreter, typeof(DetailFlags),
                            detailFlags.ToString(), debugArguments[2],
                            interpreter.InternalCultureInfo, true, true, true,
                            ref localResult);

                        if (enumValue is DetailFlags)
                        {
                            detailFlags = (DetailFlags)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localCode = ReturnCode.Error;
                        }
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        CallStack callStack = interpreter.CallStack;

                        if (callStack != null)
                        {
                            ICallFrame frame = null;

                            if (FlagOps.HasFlags(detailFlags,
                                    DetailFlags.CallStackAllFrames, true))
                            {
                                int index = 0;

                                localCode = Value.GetInteger2(
                                    (IGetValue)debugArguments[1],
                                    ValueFlags.AnyInteger,
                                    interpreter.InternalCultureInfo, ref index,
                                    ref localResult);

                                if (localCode == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Verify that the index is within the
                                    //        bounds of the call stack.
                                    //
                                    int count = callStack.Count;

                                    if ((index >= 0) && (index < count))
                                    {
                                        frame = callStack[index];
                                    }
                                    else
                                    {
                                        localResult = String.Format("invalid " +
                                            "call frame index (there {0} {1} {2})",
                                            (count == 1) ? "is" : "are", count,
                                            (count == 1) ? "frame" : "frames");

                                        localCode = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                FrameResult frameResult = interpreter.GetCallFrame(
                                    debugArguments[1], ref frame, ref localResult);

                                if (frameResult != FrameResult.Invalid)
                                    localCode = ReturnCode.Ok;
                                else
                                    localCode = ReturnCode.Error;
                            }

                            if (localCode == ReturnCode.Ok)
                            {
                                IInformationHost informationHost =
                                    interactiveHost as IInformationHost;

                                if ((informationHost != null) &&
                                    !AppDomainOps.IsTransparentProxy(informationHost))
                                {
                                    informationHost.SavePosition();

                                    if (!informationHost.WriteCallFrameInfo(
                                            interpreter, frame, detailFlags,
                                            true))
                                    {
                                        informationHost.WriteResultLine(
                                            ReturnCode.Error, HostWriteInfoError);
                                    }

                                    informationHost.RestorePosition(true);

                                    localResult = String.Empty;
                                    localCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    localResult = String.Format(
                                        HostOps.NoFeatureError,
                                        typeof(IInformationHost).Name);

                                    localCode = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            localResult = "no call stack available";
                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}frinfo level ?flags?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "einfo" interactive command, which
            /// displays detailed information about the script engine.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the engine information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void einfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteEngineInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "cinfo" interactive command, which
            /// displays detailed information about the interpreter control
            /// state.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the control information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void cinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteControlInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "eninfo" interactive command, which
            /// displays detailed information about the entities defined in the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the entity information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void eninfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteEntityInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "sinfo" interactive command, which
            /// displays detailed information about the native stack.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the stack information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies whether the native
            /// stack pointers should be refreshed before the information is
            /// displayed.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void sinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool refresh = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref refresh,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        if (refresh)
                        {
#if NATIVE
                            /* NO RESULT */
                            RuntimeOps.RefreshNativeStackPointers(true);

                            localCode = RuntimeOps.CheckForStackSpace(
                                interpreter);

                            if (localCode != ReturnCode.Ok)
                                localResult = "check for stack space failed";
#else
                            localResult = "not implemented";
                            localCode = ReturnCode.Error;
#endif
                        }

                        if (localCode == ReturnCode.Ok)
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteStackInfo(
                                    interpreter, HostOps.GetDetailFlags(interpreter),
                                    true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "histfile" interactive command, which
            /// displays and optionally modifies the file name used for command
            /// history.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the history file
            /// name to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the history file name or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void histfile(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                if (debugArguments.Count >= 2)
                    interpreter.HistoryFileName = debugArguments[1];

                localResult = interpreter.HistoryFileName;
                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "histinfo" interactive command, which
            /// displays the recorded command history.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the history information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void histinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    IHistoryFilter historyFilter = interpreter.HistoryInfoFilter;

                    if (historyFilter == null)
                        historyFilter = HistoryOps.DefaultInfoFilter;

                    if (!informationHost.WriteHistoryInfo(
                            interpreter, historyFilter,
                            HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "histclear" interactive command,
            /// which clears all recorded command history.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the history was
            /// cleared or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void histclear(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                localCode = interpreter.ClearHistory(null, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "history cleared";
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "histload" interactive command, which
            /// loads recorded command history from a file.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the file name to
            /// load the history from.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the history was
            /// loaded or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void histload(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                string fileName = interpreter.HistoryFileName;

                if (fileName == null)
                    fileName = DefaultHistoryFileName;

                if (debugArguments.Count >= 2)
                    fileName = debugArguments[1];

                IHistoryData historyData = interpreter.HistoryLoadData;

                if (historyData == null)
                    historyData = DefaultHistoryLoadData;

                IHistoryFilter historyFilter = interpreter.HistoryLoadFilter;

                if (historyFilter == null)
                    historyFilter = DefaultHistoryLoadFilter;

                localCode = interpreter.LoadHistory(
                    null, fileName, historyData, historyFilter, false,
                    ref localResult);

                if (localCode == ReturnCode.Ok)
                {
                    localResult = String.Format(
                        "history loaded from \"{0}\"",
                        fileName);
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "histsave" interactive command, which
            /// saves the recorded command history to a file.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the file name to
            /// save the history to.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the history was saved
            /// or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void histsave(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                string fileName = interpreter.HistoryFileName;

                if (fileName == null)
                    fileName = DefaultHistoryFileName;

                if (debugArguments.Count >= 2)
                    fileName = debugArguments[1];

                IHistoryData historyData = interpreter.HistorySaveData;

                if (historyData == null)
                    historyData = DefaultHistorySaveData;

                IHistoryFilter historyFilter = interpreter.HistorySaveFilter;

                if (historyFilter == null)
                    historyFilter = DefaultHistorySaveFilter;

                localCode = interpreter.SaveHistory(
                    null, fileName, historyData, historyFilter, false,
                    ref localResult);

                if (localCode == ReturnCode.Ok)
                {
                    localResult = String.Format(
                        "history saved to \"{0}\"",
                        fileName);
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "hinfo" interactive command, which
            /// displays detailed information about the interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the host information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the detail flags
            /// to use.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void hinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(DetailFlags),
                        detailFlags.ToString(), debugArguments[1],
                        interpreter.InternalCultureInfo, true, true, true,
                        ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        detailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        informationHost.SavePosition();

                        if (!informationHost.WriteHostInfo(
                                interpreter, detailFlags, true))
                        {
                            informationHost.WriteResultLine(
                                ReturnCode.Error, HostWriteInfoError);
                        }

                        informationHost.RestorePosition(true);

                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "iinfo" interactive command, which
            /// displays detailed information about the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the interpreter
            /// information.
            /// </param>
            /// <param name="result">
            /// The global result to be marked and included in the displayed
            /// information.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives an empty string on success or error
            /// information on failure.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void iinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                Result result,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (localResult != null)
                    localResult.Flags |= ResultFlags.Local;

                if (result != null)
                    result.Flags |= ResultFlags.Global;

                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteInterpreterInfo(
                            interpreter, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "fresc" interactive command, which
            /// toggles the flag that forces script cancellation to be reset.
            /// </summary>
            /// <param name="forceCancel">
            /// Upon return, receives the toggled value of the flag that forces
            /// script cancellation to be reset.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the new state of the
            /// flag or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void fresc(
                ref bool forceCancel,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                forceCancel = !forceCancel; // NOTE: TOGGLE.

                localResult = String.Format(
                    "force reset cancel {0}",
                    ConversionOps.ToEnabled(forceCancel));

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "fresh" interactive command, which
            /// toggles the flag that forces the halt state to be reset.
            /// </summary>
            /// <param name="forceHalt">
            /// Upon return, receives the toggled value of the flag that forces
            /// the halt state to be reset.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the new state of the
            /// flag or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void fresh(
                ref bool forceHalt,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                forceHalt = !forceHalt; // NOTE: TOGGLE.

                localResult = String.Format(
                    "force reset halt {0}",
                    ConversionOps.ToEnabled(forceHalt));

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "resc" interactive command, which
            /// resets the script cancellation flags for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies whether the
            /// cancellation flags should be reset globally.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating whether the cancel
            /// flags were reset or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void resc(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool global = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref global,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    CancelFlags cancelFlags =
                        CancelFlags.InteractiveManualResetCancel;

                    if (global)
                        cancelFlags |= CancelFlags.Global;

                    bool reset = false;

                    localCode = Engine.ResetCancel(
                        interpreter, cancelFlags, ref reset, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = String.Format(
                            "cancel flags {0}", reset ? "reset" : "not reset");
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "resh" interactive command, which
            /// resets the halt flags for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies whether the halt
            /// flags should be reset globally.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating whether the halt
            /// flags were reset or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void resh(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool global = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref global,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    CancelFlags cancelFlags =
                        CancelFlags.InteractiveManualResetHalt;

                    if (global)
                        cancelFlags |= CancelFlags.Global;

                    bool reset = false;

                    localCode = Engine.ResetHalt(
                        interpreter, cancelFlags, ref reset, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = String.Format(
                            "halt flags {0}", reset ? "reset" : "not reset");
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "rehash" interactive command, which
            /// reloads the user-specific host profile and re-initializes the
            /// host color settings.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host whose profile and color settings are to be
            /// reloaded.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the host profile
            /// name and the element at index two specifies the name of the
            /// encoding to use when reading the profile file.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message indicating the host profile was
            /// reloaded or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void rehash(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Profile profileHost = interactiveHost as _Hosts.Profile;

                if (profileHost != null)
                {
                    //
                    // BUGFIX: Force the currently loaded color settings
                    //         to be re-initialized based on the NoColor
                    //         setting.
                    //
                    profileHost.InitializeColors();

                    //
                    // NOTE: Reset the profile name if necessary.
                    //
                    if (debugArguments.Count >= 2)
                        profileHost.Profile = debugArguments[1];

                    //
                    // NOTE: Figure out the encoding that should be
                    //       used when reading the profile file.
                    //
                    string encodingName = null;

                    if (debugArguments.Count >= 3)
                        encodingName = debugArguments[2];

                    Encoding encoding = null;

                    if (encodingName != null)
                    {
                        localCode = interpreter.GetEncoding(
                            encodingName, LookupFlags.Default, ref encoding,
                            ref localResult);
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Reload any user-specific host profile here.
                        //
                        Type hostType = AppDomainOps.MaybeGetType(profileHost,
                            typeof(_Hosts.Profile));

                        string fileName = profileHost.HostProfileFileName;

                        //
                        // NOTE: Using this interactive command overrides the
                        //       "NoProfile" option, if set, and forces the
                        //       profile to be loaded.
                        //
                        CultureInfo cultureInfo = null;

                        if (interpreter != null)
                            cultureInfo = interpreter.InternalCultureInfo;

                        if (SettingsOps.LoadForHost(
                                interpreter, profileHost, hostType,
                                encoding, fileName, cultureInfo,
                                _Hosts.Default.HostPropertyBindingFlags,
                                true, ref localResult))
                        {
                            localResult = String.Format(
                                "host profile \"{0}\" reloaded",
                                fileName);

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                "colors initialized to defaults; " +
                                "failed to reload host profile: {0}",
                                localResult);

                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Profile).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "deval" interactive command, which
            /// evaluates one or more arguments as a script within the isolated
            /// debugger interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// elements at index one and beyond comprise the script to be
            /// evaluated.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the result of evaluating the script or
            /// error information.
            /// </param>
            /// <param name="localErrorLine">
            /// Upon return, receives the line number where a script error
            /// occurred, or zero if none.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void deval(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult,
                ref int localErrorLine
                )
            {
#if DEBUGGER
                if (debugArguments.Count >= 2)
                {
                    Interpreter debugInterpreter = null;

                    if (Engine.CheckDebuggerInterpreter(
                            interpreter, false, ref debugInterpreter,
                            ref localResult))
                    {
                        localErrorLine = 0;

                        if (debugArguments.Count == 2)
                            localCode = debugInterpreter.EvaluateScript(
                                debugArguments[1], ref localResult,
                                ref localErrorLine);
                        else
                            localCode = debugInterpreter.EvaluateScript(
                                debugArguments, 1, ref localResult,
                                ref localErrorLine);
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}deval arg ?arg ...?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dsubst" interactive command, which
            /// performs backslash, command, and variable substitutions on a
            /// string within the isolated debugger interpreter, honoring the
            /// "-nobackslashes", "-nocommands", and "-novariables" options.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command,
            /// including any options and the string to be substituted.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the substituted string or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dsubst(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                if (debugArguments.Count >= 2)
                {
                    Interpreter debugInterpreter = null;

                    if (Engine.CheckDebuggerInterpreter(interpreter, false,
                            ref debugInterpreter, ref localResult))
                    {
                        OptionDictionary options =
                            CommandOptions.GetCommandOptions(
                                CommandOptionType.Debugger_Dsubst);

                        int argumentIndex = Index.Invalid;

                        localCode = interpreter.GetOptions(
                            options, debugArguments, 0, 1, Index.Invalid, false,
                            ref argumentIndex, ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 1) == debugArguments.Count))
                            {
                                SubstitutionFlags debugSubstitutionFlags =
                                    SubstitutionFlags.Default;

                                if (options.IsPresent("-nobackslashes"))
                                    debugSubstitutionFlags &= ~SubstitutionFlags.Backslashes;

                                if (options.IsPresent("-nocommands"))
                                    debugSubstitutionFlags &= ~SubstitutionFlags.Commands;

                                if (options.IsPresent("-novariables"))
                                    debugSubstitutionFlags &= ~SubstitutionFlags.Variables;

                                localCode = debugInterpreter.SubstituteString(
                                    debugArguments[argumentIndex],
                                    debugSubstitutionFlags, ref localResult);
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(debugArguments[argumentIndex]))
                                {
                                    localResult = OptionDictionary.BadOption(
                                        options, debugArguments[argumentIndex],
                                        !interpreter.InternalIsSafe());
                                }
                                else
                                {
                                    localResult = String.Format(
                                        "wrong # args: should be \"{0}dsubst " +
                                        "?-nobackslashes? ?-nocommands? " +
                                        "?-novariables? string\"",
                                        ShellOps.InteractiveCommandPrefix);
                                }

                                localCode = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}dsubst " +
                        "?-nobackslashes? ?-nocommands? ?-novariables? string\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "paused" interactive command, which
            /// lists the interactive loops that are currently paused.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the list of paused interactive loops or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void paused(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                StringList list = null;

                localCode = interpreter.ListPausedInteractiveLoops(
                    ref list, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = list;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "pause" interactive command, which
            /// pauses an interactive loop, optionally identified by thread
            /// identifier and application domain identifier, and then waits
            /// for it to be unpaused.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command,
            /// which may specify the thread identifier, application domain
            /// identifier, wait timeout in microseconds, and quiet flag.
            /// </param>
            /// <param name="show">
            /// Upon return, may be set to indicate whether prompt and related
            /// output should be shown, based on the optional quiet argument.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void pause(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                long threadId = GlobalState.GetCurrentSystemThreadId();

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetWideInteger2(
                        (IGetValue)debugArguments[1],
                        ValueFlags.AnyWideInteger,
                        interpreter.InternalCultureInfo,
                        ref threadId, ref localResult);
                }

                int appDomainId = AppDomainOps.GetCurrentId();

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetInteger2(
                        (IGetValue)debugArguments[2],
                        ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo,
                        ref appDomainId, ref localResult);
                }

                long microseconds = ShellOps.PauseMicroseconds;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetWideInteger2(
                        (IGetValue)debugArguments[3],
                        ValueFlags.AnyWideInteger,
                        interpreter.InternalCultureInfo,
                        ref microseconds, ref localResult);
                }

                bool quiet = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref quiet,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        show = !quiet;
                }

                if (localCode == ReturnCode.Ok)
                {
                    localCode = interpreter.PauseInteractiveLoop(
                        appDomainId, threadId, ref localResult);
                }

                if ((localCode == ReturnCode.Ok) &&
                    (threadId == GlobalState.GetCurrentSystemThreadId()) &&
                    (appDomainId == AppDomainOps.GetCurrentId()))
                {
                    localCode = ShellOps.WaitPausedInteractiveLoop(
                        interpreter, appDomainId, threadId,
                        microseconds, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "unpause" interactive command, which
            /// unpauses an interactive loop, optionally identified by thread
            /// identifier and application domain identifier.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command,
            /// which may specify the thread identifier, application domain
            /// identifier, and quiet flag.
            /// </param>
            /// <param name="show">
            /// Upon return, may be set to indicate whether prompt and related
            /// output should be shown, based on the optional quiet argument.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void unpause(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                long threadId = GlobalState.GetCurrentSystemThreadId();

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetWideInteger2(
                        (IGetValue)debugArguments[1],
                        ValueFlags.AnyWideInteger,
                        interpreter.InternalCultureInfo,
                        ref threadId, ref localResult);
                }

                int appDomainId = AppDomainOps.GetCurrentId();

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetInteger2(
                        (IGetValue)debugArguments[2],
                        ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo,
                        ref appDomainId, ref localResult);
                }

                bool quiet = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref quiet,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        show = !quiet;
                }

                if (localCode == ReturnCode.Ok)
                {
                    localCode = interpreter.UnpauseInteractiveLoop(
                        appDomainId, threadId, true, false,
                        false, true, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "suspend" interactive command, which
            /// suspends debugger stepping when debugging is active.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interactive loop is currently in debug mode;
            /// when false, the command reports an error.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void suspend(
                Interpreter interpreter,
                bool debug,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In debug mode, we simply suspend debugging (stepping);
                //       otherwise, an error message is displayed.
                //
                if (debug)
                {
                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, true,
                            ref localDebugger, ref localResult))
                    {
                        localCode = localDebugger.Suspend(ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = "debugger suspended";
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}suspend\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "resume" interactive command, which
            /// resumes debugger stepping when debugging is active.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interactive loop is currently in debug mode;
            /// when false, the command reports an error.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void resume(
                Interpreter interpreter,
                bool debug,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In debug mode, we simply resume debugging (stepping);
                //       otherwise, an error message is displayed.
                //
                if (debug)
                {
                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, true,
                            ref localDebugger, ref localResult))
                    {
                        localCode = localDebugger.Resume(ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = "debugger resumed";
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}resume\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "about" interactive command, which
            /// displays the banner and legal information for the interpreter
            /// and, optionally, configures the stable update path and query
            /// string.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to write the banner and related
            /// output.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the desired
            /// stability level and the element at index two, when present,
            /// specifies an optional suffix.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void about(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (HelpOps.WriteBanner(
                        interpreter, false, false, false, false,
                        false, true, false, false, false, true) &&
                    (interactiveHost != null) && interactiveHost.WriteLine() &&
                    HelpOps.WriteLegalese(interpreter, false))
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        ReturnCode localCommandCode;
                        Result localCommandResult = null;

                        int stable = 0;

                        localCommandCode = Value.GetInteger2(
                            (IGetValue)debugArguments[1],
                            ValueFlags.AnyInteger,
                            interpreter.InternalCultureInfo,
                            ref stable, ref localCommandResult);

                        string suffix = null;

                        if ((debugArguments.Count >= 3) &&
                            !String.IsNullOrEmpty(debugArguments[2]))
                        {
                            suffix = debugArguments[2];
                        }

                        if ((localCommandCode == ReturnCode.Ok) &&
                            (stable > 0) && (stable < 2))
                        {
                            string localValue = RuntimeOps.GetUpdatePathAndQuery(
                                GlobalState.GetAssemblyUpdateVersion(), null,
                                suffix);

                            localCommandCode = interpreter.SetVariableValue2(
                                VariableFlags.GlobalOnly, Vars.Platform.Name,
                                Vars.Platform.UpdatePathAndQueryName, localValue,
                                ref localCommandResult);

                            if (localCommandCode == ReturnCode.Ok)
                                localResult = String.Empty;
                            else
                                localResult = localCommandResult;

                            localCode = localCommandCode;
                        }
                        else
                        {
                            ResultList errors = new ResultList();

                            errors.Add("invalid, unstable argument");

                            if (localCommandResult != null)
                                errors.Add(localCommandResult);

                            localResult = errors;
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                }
                else
                {
                    localResult = "failed to display banner and/or license";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "chans" interactive command, which
            /// verifies or restores the standard input, output, and error
            /// channels for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="streamHost">
            /// The stream host providing the underlying standard channel
            /// streams.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, indicates whether existing
            /// channels should be replaced.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void chans(
                Interpreter interpreter,
                IStreamHost streamHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool replace = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref replace,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    ChannelType channelType = ChannelType.StandardChannels;

                    if (replace)
                        channelType |= ChannelType.AllowExist;

                    localCode = interpreter.ModifyStandardChannels(
                        streamHost, null, channelType, ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    localResult = String.Format(
                        "standard channels {0}",
                        replace ? "restored" : "present");
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "init" interactive command, which
            /// (re)initializes the interpreter or its shell, optionally
            /// forcing the operation.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, indicates whether the shell
            /// should be initialized and the element at index two, when
            /// present, indicates whether the operation should be forced.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void init(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool shell = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref shell,
                        ref localResult);
                }

                bool force = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref force,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    if (shell)
                    {
                        localCode = interpreter.InitializeShell(
                            force, ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            localResult = String.Format(
                                "shell {0}initialized",
                                force ? "force " : String.Empty);
                        }
                    }
                    else
                    {
                        localCode = interpreter.Initialize(
                            force, ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            localResult = String.Format(
                                "{0}initialized",
                                force ? "force " : String.Empty);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dpath" interactive command, which
            /// displays the various paths used by the interpreter, as selected
            /// by the optional debug path flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the debug path
            /// flags controlling which paths are displayed.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dpath(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                DebugPathFlags debugPathFlags = DebugPathFlags.Default;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(DebugPathFlags),
                        debugPathFlags.ToString(), debugArguments[1],
                        interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is DebugPathFlags)
                    {
                        debugPathFlags = (DebugPathFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    GlobalState.DisplayPaths(interpreter, debugPathFlags);
                    localResult = String.Empty;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "cancel" interactive command, which
            /// initiates script cancellation, roughly simulating a console
            /// interrupt (Ctrl-C).
            /// </summary>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void cancel(
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if CONSOLE
                try
                {
                    //
                    // NOTE: Roughly simulate hitting Ctrl-C
                    //       on the console.
                    //
                    /* NO RESULT */
                    Interpreter.MaybeShowPromptAndAllCancel(
                        null, false);

                    localResult = "cancellation initiated";
                    localCode = ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "test" (and "ptest") interactive
            /// command, which runs the test suite, optionally filtered by a
            /// pattern, using an extra search path, over either the default or
            /// plugin test files.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command,
            /// which may specify the test name pattern, an all flag, and an
            /// extra search path.
            /// </param>
            /// <param name="localEngineFlags">
            /// The engine flags to use while running the tests.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// The substitution flags to use while running the tests.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use while running the tests.
            /// </param>
            /// <param name="localExpressionFlags">
            /// The expression flags to use while running the tests.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            /// <param name="localErrorLine">
            /// Upon return, receives the line number where a script error
            /// occurred, or zero if none.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void test(
                Interpreter interpreter,
                ArgumentList debugArguments,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult,
                ref int localErrorLine
                )
            {
                string pattern = null;

                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    pattern = debugArguments[1];
                }

                localCode = ReturnCode.Ok;

                bool all = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref all,
                        ref localResult);
                }

                string extraPath = null;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    extraPath = debugArguments[3];
                }

                if (localCode == ReturnCode.Ok)
                {
                    localErrorLine = 0;

                    localCode = TestOps.ShellMain(
                        interpreter, pattern, extraPath, localEngineFlags,
                        localSubstitutionFlags, localEventFlags,
                        localExpressionFlags, SharedStringOps.SystemEquals(
                            debugArguments[0], String.Format("{0}ptest",
                                ShellOps.InteractiveCommandPrefix)) ?
                                TestPathType.Plugins : TestPathType.Default,
                        all, ref localResult, ref localErrorLine);
                }

                if (localCode == ReturnCode.Ok)
                    localResult = String.Empty;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "trustclr" interactive command,
            /// which clears the list of trusted directories maintained by the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void trustclr(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (Interpreter.IsDeletedOrDisposed(
                            interpreter, false, ref localResult))
                    {
                        localCode = ReturnCode.Error;
                    }
                    else
                    {
                        StringList trustedPaths = interpreter.InternalTrustedPaths;

                        if (trustedPaths != null)
                        {
                            trustedPaths.Clear();

                            localResult = "trusted directory list cleared";
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = "trusted directory list not available";
                            localCode = ReturnCode.Error;
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "trustdir" interactive command,
            /// which adds a directory to, and then returns, the list of
            /// trusted directories maintained by the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the directory to
            /// add to the trusted list.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void trustdir(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (Interpreter.IsDeletedOrDisposed(
                            interpreter, false, ref localResult))
                    {
                        localCode = ReturnCode.Error;
                    }
                    else
                    {
                        StringList trustedPaths = interpreter.InternalTrustedPaths;

                        if (trustedPaths != null)
                        {
                            if (debugArguments.Count >= 2)
                                trustedPaths.Add(debugArguments[1]);

                            localResult = trustedPaths;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = "trusted directory list not available";
                            localCode = ReturnCode.Error;
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "testdir" interactive command, which
            /// sets and/or reports the manual test path along with the
            /// effective base, library, and plugin test paths.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the manual test
            /// path to use.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void testdir(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                    interpreter.TestPath = debugArguments[1];

                localResult = StringList.MakeList(
                    String.Format("manual test path is \"{0}\"",
                        interpreter.TestPath),
                    String.Format("effective base test path is \"{0}\"",
                        TestOps.GetPath(interpreter, TestPathType.None)),
                    String.Format("effective library test path is \"{0}\"",
                        TestOps.GetPath(interpreter, TestPathType.Library)),
                    String.Format("effective plugin test path is \"{0}\"",
                        TestOps.GetPath(interpreter, TestPathType.Plugins)));

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "purge" interactive command, which
            /// purges unused call frames from the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void purge(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = CallFrameOps.Purge(interpreter, ref localResult);
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "restc" interactive command, which
            /// restores the core plugin for the interpreter, optionally in
            /// strict and/or verbose mode.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, indicates strict mode and
            /// the element at index two, when present, indicates verbose mode.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void restc(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool strict = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref strict,
                        ref localResult);
                }

                bool verbose = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref verbose,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                    localCode = interpreter.RestoreCorePlugin(
                        strict, verbose, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "core plugin restored";
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "restm" interactive command, which
            /// restores the monitor plugin for the interpreter, optionally in
            /// strict and/or verbose mode.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, indicates strict mode and
            /// the element at index two, when present, indicates verbose mode.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void restm(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NOTIFY && NOTIFY_ARGUMENTS
                localCode = ReturnCode.Ok;

                bool strict = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref strict,
                        ref localResult);
                }

                bool verbose = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref verbose,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                    localCode = interpreter.RestoreMonitorPlugin(
                        strict, verbose, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "monitor plugin restored";
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "restv" interactive command, which
            /// restores the core variables and platform-related variables for
            /// the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="args">
            /// The collection of command-line style arguments used when
            /// setting up the core variables.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void restv(
                Interpreter interpreter,
                IEnumerable<string> args,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                StringList autoPathList = GlobalState.GetAutoPathList(
                    interpreter, false);

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    if (Interpreter.IsDeletedOrDisposed(
                            interpreter, false, ref localResult))
                    {
                        localCode = ReturnCode.Error;
                    }
                    else
                    {
                        localCode = interpreter.SetupMinimumVariables(
                            autoPathList, ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localCode = interpreter.SetupVariables(
                                interpreter.CreateFlags, args, false,
                                ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localCode = interpreter.SetupMinimumPlatform(
                                ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localCode = interpreter.SetupPlatform(
                                interpreter.CreateFlags, false, ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = "core variables restored";
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "vout" interactive command, which
            /// enables or disables virtual output for a channel, or returns
            /// the captured virtual output for it.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the channel
            /// identifier and the element at index two, when present,
            /// indicates whether virtual output should be enabled.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void vout(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                string channelId = StandardChannel.Output;

                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    channelId = debugArguments[1];
                }

                if ((debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    bool enabled = false;

                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref enabled,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        localCode = interpreter.InternalSetChannelVirtualOutput(
                            channelId, enabled, ref localResult);
                    }
                }
                else
                {
                    StringBuilder builder = null;

                    localCode = interpreter.InternalGetChannelVirtualOutput(
                        channelId, true, ref builder, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = builder;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "relimit" interactive command, which
            /// sets and/or returns the readiness check limit for the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the new readiness
            /// check limit.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void relimit(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    int readyLimit = 0;

                    localCode = Value.GetInteger2(
                        (IGetValue)debugArguments[1], ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo, ref readyLimit,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        interpreter.ReadyLimit = readyLimit;
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ReadyLimit;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "rlimit" interactive command, which
            /// sets and/or returns the recursion limit for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the new recursion
            /// limit.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the command result or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void rlimit(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    int recursionLimit = 0;

                    localCode = Value.GetInteger2(
                        (IGetValue)debugArguments[1], ValueFlags.AnyInteger,
                        interpreter.InternalCultureInfo, ref recursionLimit,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        interpreter.RecursionLimit = recursionLimit;
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.RecursionLimit;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ntypes" interactive command, which
            /// sets and/or returns the notification types enabled for the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the notification
            /// type value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting notification types or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ntypes(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NOTIFY || NOTIFY_OBJECT
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(NotifyType),
                        interpreter.NotifyTypes.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is NotifyFlags)
                    {
                        interpreter.NotifyTypes = (NotifyType)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.NotifyTypes;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "nflags" interactive command, which
            /// sets and/or returns the notification flags for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the notification
            /// flag value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting notification flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void nflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NOTIFY || NOTIFY_OBJECT
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(NotifyFlags),
                        interpreter.NotifyFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is NotifyFlags)
                    {
                        interpreter.NotifyFlags = (NotifyFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.NotifyFlags;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "hflags" interactive command, which
            /// sets and/or returns the interactive header flags for the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used when computing the effective header
            /// flags.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the header flag
            /// value(s) to apply.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interactive loop is currently in debug mode.
            /// </param>
            /// <param name="localHeaderFlags">
            /// Upon return, receives the updated header flags when they are
            /// changed by the command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting header flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void hflags(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                bool debug,
                ref HeaderFlags localHeaderFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(HeaderFlags),
                        DebuggerOps.GetHeaderFlags(interactiveHost,
                            interpreter.HeaderFlags | HeaderFlags.User,
                            debug, false, false, true).ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is HeaderFlags)
                    {
                        localHeaderFlags = (HeaderFlags)enumValue;
                        interpreter.HeaderFlags = localHeaderFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.HeaderFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dflags" interactive command, which
            /// sets and/or returns the interactive detail flags for the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host associated with the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the detail flag
            /// value(s) to apply.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interactive loop is currently in debug mode.
            /// </param>
            /// <param name="localDetailFlags">
            /// Upon return, receives the updated detail flags when they are
            /// changed by the command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting detail flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dflags(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                bool debug,
                ref DetailFlags localDetailFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(DetailFlags),
                        (interpreter.DetailFlags | DetailFlags.User).ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        localDetailFlags = (DetailFlags)enumValue;
                        interpreter.DetailFlags = localDetailFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DetailFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "lhflags" interactive command, which
            /// sets and/or returns the local interactive loop header flags,
            /// without modifying the interpreter-wide header flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used when computing the effective header
            /// flags.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the header flag
            /// value(s) to apply.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interactive loop is currently in debug mode.
            /// </param>
            /// <param name="localHeaderFlags">
            /// The local header flags to modify; upon return, receives the
            /// updated header flags when they are changed by the command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting header flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void lhflags(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                bool debug,
                ref HeaderFlags localHeaderFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(HeaderFlags),
                        DebuggerOps.GetHeaderFlags(interactiveHost,
                            localHeaderFlags, debug, false, false,
                            true).ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is HeaderFlags)
                    {
                        localHeaderFlags = (HeaderFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localHeaderFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ldflags" interactive command, which
            /// sets and/or returns the local interactive loop detail flags,
            /// without modifying the interpreter-wide detail flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used when computing the effective detail
            /// flags.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the detail flag
            /// value(s) to apply.
            /// </param>
            /// <param name="debug">
            /// Non-zero if the interactive loop is currently in debug mode.
            /// </param>
            /// <param name="localDetailFlags">
            /// The local detail flags to modify; upon return, receives the
            /// updated detail flags when they are changed by the command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting detail flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ldflags(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                bool debug,
                ref DetailFlags localDetailFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(DetailFlags),
                        DebuggerOps.GetDetailFlags(interactiveHost,
                            localDetailFlags, debug, false, false,
                            true).ToString(), debugArguments[1],
                        interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        localDetailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localDetailFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "cflags" interactive command, which
            /// sets and/or returns the creation flags for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the creation flag
            /// value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting creation flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void cflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(CreateFlags),
                        interpreter.CreateFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is CreateFlags)
                    {
                        interpreter.CreateFlags = (CreateFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.CreateFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dcflags" interactive command, which
            /// sets and/or returns the default creation flags for the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the default
            /// creation flag value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting default creation flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dcflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(CreateFlags),
                        interpreter.DefaultCreateFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is CreateFlags)
                    {
                        interpreter.DefaultCreateFlags = (CreateFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultCreateFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "scflags" interactive command, which
            /// sets and/or returns the script flags for the interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the script flag
            /// value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting script flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void scflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ScriptFlags),
                        interpreter.ScriptFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ScriptFlags)
                    {
                        interpreter.ScriptFlags = (ScriptFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ScriptFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dscflags" interactive command,
            /// which sets and/or returns the default script flags for the
            /// interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the default
            /// script flag value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting default script flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dscflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ScriptFlags),
                        interpreter.DefaultScriptFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ScriptFlags)
                    {
                        interpreter.DefaultScriptFlags = (ScriptFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultScriptFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "iflags" interactive command, which
            /// sets and/or returns the interpreter flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the interpreter
            /// flag value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting interpreter flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void iflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(InterpreterFlags),
                        interpreter.InterpreterFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InterpreterFlags)
                    {
                        interpreter.InterpreterFlags =
                            (InterpreterFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InterpreterFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "diflags" interactive command, which
            /// displays and optionally modifies the default interpreter flags
            /// for the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting default interpreter flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void diflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(InterpreterFlags),
                        interpreter.DefaultInterpreterFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InterpreterFlags)
                    {
                        interpreter.DefaultInterpreterFlags =
                            (InterpreterFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultInterpreterFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "itflags" interactive command, which
            /// displays and optionally modifies the interpreter test flags for
            /// the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting interpreter test flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void itflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(InterpreterTestFlags),
                        interpreter.InterpreterTestFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InterpreterTestFlags)
                    {
                        interpreter.InterpreterTestFlags =
                            (InterpreterTestFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InterpreterTestFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dtflags" interactive command, which
            /// displays and optionally modifies the default interpreter test
            /// flags for the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting default interpreter test
            /// flags or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dtflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(InterpreterTestFlags),
                        interpreter.DefaultInterpreterTestFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InterpreterTestFlags)
                    {
                        interpreter.DefaultInterpreterTestFlags =
                            (InterpreterTestFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultInterpreterTestFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "spaflags" interactive command, which
            /// displays and optionally modifies the shared package flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting shared package flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void spaflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(PackageFlags),
                        interpreter.SharedPackageFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is PackageFlags)
                    {
                        interpreter.SharedPackageFlags = (PackageFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.SharedPackageFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "sprflags" interactive command, which
            /// displays and optionally modifies the shared procedure flags for
            /// the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting shared procedure flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void sprflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ProcedureFlags),
                        interpreter.SharedProcedureFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ProcedureFlags)
                    {
                        interpreter.SharedProcedureFlags = (ProcedureFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.SharedProcedureFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "pflags" interactive command, which
            /// displays and optionally modifies the plugin flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting plugin flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void pflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(PluginFlags),
                        interpreter.PluginFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is PluginFlags)
                    {
                        interpreter.PluginFlags = (PluginFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.PluginFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ceflags" interactive command, which
            /// displays and optionally modifies the context engine flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localEngineFlags">
            /// Upon return, receives the updated engine flags when the context
            /// engine flags are successfully modified.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting context engine flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ceflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EngineFlags localEngineFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EngineFlags),
                        interpreter.ContextEngineFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        localEngineFlags = (EngineFlags)enumValue;
                        interpreter.ContextEngineFlags = localEngineFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ContextEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "seflags" interactive command, which
            /// displays and optionally modifies the shared engine flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localEngineFlags">
            /// Upon return, receives the updated engine flags when the shared
            /// engine flags are successfully modified.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting shared engine flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void seflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EngineFlags localEngineFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EngineFlags),
                        interpreter.SharedEngineFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        localEngineFlags = (EngineFlags)enumValue;
                        interpreter.SharedEngineFlags = localEngineFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.SharedEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "evflags" interactive command, which
            /// displays and optionally modifies the engine event flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localEventFlags">
            /// Upon return, receives the updated event flags when the engine
            /// event flags are successfully modified.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting engine event flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void evflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EventFlags),
                        interpreter.EngineEventFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EventFlags)
                    {
                        localEventFlags = (EventFlags)enumValue;
                        interpreter.EngineEventFlags = localEventFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.EngineEventFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "exflags" interactive command, which
            /// displays and optionally modifies the expression flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localExpressionFlags">
            /// Upon return, receives the updated expression flags when the
            /// expression flags are successfully modified.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting expression flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void exflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ExpressionFlags),
                        interpreter.ExpressionFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ExpressionFlags)
                    {
                        localExpressionFlags = (ExpressionFlags)enumValue;
                        interpreter.ExpressionFlags = localExpressionFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ExpressionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ieflags" interactive command, which
            /// displays and optionally modifies the interactive engine flags for
            /// the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting interactive engine flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ieflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EngineFlags),
                        interpreter.InteractiveEngineFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        interpreter.InteractiveEngineFlags =
                            (EngineFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "ievflags" interactive command, which
            /// displays and optionally modifies the interactive event flags for
            /// the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting interactive event flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void ievflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EventFlags),
                        interpreter.InteractiveEventFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EventFlags)
                    {
                        interpreter.InteractiveEventFlags = (EventFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveEventFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "iexflags" interactive command, which
            /// displays and optionally modifies the interactive expression flags
            /// for the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting interactive expression flags
            /// or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void iexflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ExpressionFlags),
                        interpreter.InteractiveExpressionFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ExpressionFlags)
                    {
                        interpreter.InteractiveExpressionFlags =
                            (ExpressionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveExpressionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "leflags" interactive command, which
            /// displays and optionally modifies the local (interactive loop)
            /// engine flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localEngineFlags">
            /// The local engine flags to display and, when requested, modify.
            /// Upon return, receives the updated engine flags.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting local engine flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void leflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EngineFlags localEngineFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EngineFlags),
                        localEngineFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        localEngineFlags = (EngineFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "levflags" interactive command, which
            /// displays and optionally modifies the local (interactive loop)
            /// event flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localEventFlags">
            /// The local event flags to display and, when requested, modify.
            /// Upon return, receives the updated event flags.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting local event flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void levflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(EventFlags),
                        localEventFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EventFlags)
                    {
                        localEventFlags = (EventFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localEventFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "lexflags" interactive command, which
            /// displays and optionally modifies the local (interactive loop)
            /// expression flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localExpressionFlags">
            /// The local expression flags to display and, when requested,
            /// modify.  Upon return, receives the updated expression flags.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting local expression flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void lexflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(ExpressionFlags),
                        localExpressionFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ExpressionFlags)
                    {
                        localExpressionFlags = (ExpressionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localExpressionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "sflags" interactive command, which
            /// displays and optionally modifies the substitution flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// Upon return, receives the updated substitution flags when the
            /// substitution flags are successfully modified.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting substitution flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void sflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref SubstitutionFlags localSubstitutionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(SubstitutionFlags),
                        interpreter.SubstitutionFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is SubstitutionFlags)
                    {
                        localSubstitutionFlags = (SubstitutionFlags)enumValue;
                        interpreter.SubstitutionFlags = localSubstitutionFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.SubstitutionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "isflags" interactive command, which
            /// displays and optionally modifies the interactive substitution
            /// flags for the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting interactive substitution
            /// flags or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void isflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(SubstitutionFlags),
                        interpreter.InteractiveSubstitutionFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is SubstitutionFlags)
                    {
                        interpreter.InteractiveSubstitutionFlags =
                            (SubstitutionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveSubstitutionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "izflags" interactive command, which
            /// displays and optionally modifies the initialize flags for the
            /// specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting initialize flags or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void izflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(InitializeFlags),
                        interpreter.InitializeFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InitializeFlags)
                    {
                        interpreter.InitializeFlags =
                            (InitializeFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InitializeFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "dizflags" interactive command, which
            /// displays and optionally modifies the default initialize flags for
            /// the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting default initialize flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void dizflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(InitializeFlags),
                        interpreter.DefaultInitializeFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InitializeFlags)
                    {
                        interpreter.DefaultInitializeFlags =
                            (InitializeFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultInitializeFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "lsflags" interactive command, which
            /// displays and optionally modifies the local (interactive loop)
            /// substitution flags.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the flag value(s)
            /// to apply.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// The local substitution flags to display and, when requested,
            /// modify.  Upon return, receives the updated substitution flags.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting local substitution flags or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void lsflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref SubstitutionFlags localSubstitutionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlags(
                        interpreter, typeof(SubstitutionFlags),
                        localSubstitutionFlags.ToString(),
                        debugArguments[1], interpreter.InternalCultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is SubstitutionFlags)
                    {
                        localSubstitutionFlags = (SubstitutionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localSubstitutionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "step" interactive command, which
            /// toggles the single step flag of the script debugger associated
            /// with the specified interpreter.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new single step
            /// state or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void step(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In any mode, toggle the debug single step flag.
                //
                IDebugger localDebugger = null;

                if (Engine.CheckDebugger(interpreter, false,
                        ref localDebugger, ref localResult))
                {
                    localDebugger.SingleStep =
                        !localDebugger.SingleStep; // NOTE: TOGGLE.

                    localResult = String.Format("single step {0}",
                        ConversionOps.ToEnabled(localDebugger.SingleStep));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "style" interactive command, which
            /// displays and optionally modifies the output style of the default
            /// interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host whose output style is to be displayed or
            /// modified.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the output style
            /// value(s) to apply.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting output style or error
            /// information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void style(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        object enumValue = EnumOps.TryParseFlags(
                            interpreter, typeof(OutputStyle),
                            defaultHost.OutputStyle.ToString(), debugArguments[1],
                            interpreter.InternalCultureInfo, true, true, true,
                            ref localResult);

                        if (enumValue is OutputStyle)
                        {
                            defaultHost.OutputStyle = (OutputStyle)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                        localResult = defaultHost.OutputStyle;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "canexit" interactive command, which
            /// toggles whether the interactive host is permitted to exit the
            /// process.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host whose exit capability is to be toggled.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new exit state or
            /// error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void canexit(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IProcessHost processHost = interactiveHost as IProcessHost;

                if (processHost != null)
                {
                    if (FlagOps.HasFlags(
                            processHost.GetHostFlags(), HostFlags.Exit, true))
                    {
                        processHost.CanExit = !processHost.CanExit;

                        localResult = String.Format(
                            "exit {0}",
                            ConversionOps.ToEnabled(processHost.CanExit));

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError, HostFlags.Exit);

                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IProcessHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "show" interactive command, which
            /// displays the current debug information, including the local or
            /// global return code and result, via the interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the debug information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// elements at index one and two, when present, control whether the
            /// local result is shown and whether empty values are included.
            /// </param>
            /// <param name="loopData">
            /// The interactive loop data providing additional context for the
            /// information being displayed.  This parameter may be null.
            /// </param>
            /// <param name="result">
            /// The global result to display.  This parameter may be null.
            /// </param>
            /// <param name="localHeaderFlags">
            /// The header flags used when writing the debug header.
            /// </param>
            /// <param name="localCode">
            /// The local return code to display.
            /// </param>
            /// <param name="localResult">
            /// The local result to display.  This parameter may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void show(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                IInteractiveLoopData loopData,
                Result result,
                HeaderFlags localHeaderFlags,
                ReturnCode localCode,
                Result localResult,
                ref bool show
                )
            {
                //
                // NOTE: Since one of the primary features of this interactive
                //       command is to display the local or global return code and
                //       result, we do not want to make any significant changes to
                //       it, ever; therefore, use new local variables to hold the
                //       results of this operation.
                //
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                bool local = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref local,
                        ref localCommandResult);
                }

                bool empty = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref empty,
                        ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    if (localResult != null)
                        localResult.Flags |= ResultFlags.Local;

                    if (result != null)
                        result.Flags |= ResultFlags.Global;

                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    bool proxy = AppDomainOps.IsTransparentProxy(informationHost);

                    if (informationHost != null)
                    {
                        //
                        // NOTE: Display the current debug information (even
                        //       when not in debug mode).
                        //
                        informationHost.WriteHeader(
                            interpreter, new InteractiveLoopData(
                            loopData, local || (loopData == null) ? localCode :
                            loopData.Code, !proxy && (loopData != null) ?
                            loopData.Token : null, !proxy &&
                            (loopData != null) ? loopData.TraceInfo : null,
                            DebuggerOps.GetHeaderFlags(
                                interactiveHost, localHeaderFlags,
                                (loopData != null) ? loopData.Debug : false,
                                true, empty, true) & ~HeaderFlags.AllPrompt),
                            local ? localResult : result);
                    }
                    else
                    {
                        localCommandResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCommandCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "overr" interactive command, which
            /// overrides the local return code and/or result based on the
            /// supplied options.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command,
            /// including the supported options (for example, -code and -result).
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the overridden local return code, when the
            /// -code option is supplied.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the overridden local result, when the
            /// -result option is supplied.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void overr(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                OptionDictionary options =
                    CommandOptions.GetCommandOptions(
                        CommandOptionType.Debugger_Overr);

                int argumentIndex = Index.Invalid;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2))
                {
                    localCommandCode = interpreter.GetOptions(options,
                        debugArguments, 0, 1, Index.Invalid, false,
                        ref argumentIndex, ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    if (argumentIndex == Index.Invalid)
                    {
                        StringList list = new StringList();
                        IVariant value = null;

                        if (options.IsPresent("-code", ref value))
                        {
                            localCode = (ReturnCode)value.Value;

                            list.Add(String.Format("return code set to {0}",
                                localCode));
                        }
                        else
                        {
                            list.Add("return code unchanged");
                        }

                        if (options.IsPresent("-result", ref value))
                        {
                            localResult = value.ToString();

                            list.Add(String.Format("result set to {0}",
                                FormatOps.WrapOrNull(true, true, localResult)));
                        }
                        else
                        {
                            list.Add("result unchanged");
                        }

                        localCommandResult = list;
                        localCommandCode = ReturnCode.Ok;
                    }
                    else
                    {
                        if ((argumentIndex != Index.Invalid) &&
                            Option.LooksLikeOption(debugArguments[argumentIndex]))
                        {
                            localCommandResult = OptionDictionary.BadOption(
                                options, debugArguments[argumentIndex],
                                !interpreter.InternalIsSafe());
                        }
                        else
                        {
                            localCommandResult = String.Format(
                                "wrong # args: should be \"{0}overr ?options?\"",
                                ShellOps.InteractiveCommandPrefix);
                        }

                        localCommandCode = ReturnCode.Error;
                    }
                }

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       set it (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "prevr" interactive command, which
            /// rewinds the local result and return code to the previously saved
            /// result, when available.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code of the previous result, or
            /// error information when no previous result is available.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a copy of the previous result, or error
            /// information when no previous result is available.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void prevr(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if PREVIOUS_RESULT
                Result previousResult = Interpreter.GetPreviousResult(
                    interpreter);

                if (previousResult != null)
                {
                    //
                    // NOTE: Set the local result equal to the previous result.
                    //
                    localResult = Result.Copy(
                        previousResult, ResultFlags.CopyObject); /* COPY */

                    localCode = previousResult.ReturnCode;

                    if (interactiveHost != null)
                    {
                        interactiveHost.WriteResultLine(
                            ReturnCode.Ok, "result rewound");
                    }
                }
                else if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(ReturnCode.Error,
                        "no previous result");
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       set it (i.e. they already know what it is).
                //
                show = false;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "nextr" interactive command, which
            /// saves the current local result and return code as the previous
            /// result.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="localCode">
            /// The local return code to save as part of the previous result.
            /// </param>
            /// <param name="localResult">
            /// The local result to save as the previous result.  This parameter
            /// may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void nextr(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                ref bool show
                )
            {
#if PREVIOUS_RESULT
                if (localResult != null)
                {
                    Result previousResult = Result.Copy(
                        localResult, localCode, ResultFlags.CopyObject); /* COPY */

                    //
                    // NOTE: Set the previous result equal to the local result.
                    //
                    Interpreter.SetPreviousResult(interpreter, previousResult);

                    if (interactiveHost != null)
                    {
                        interactiveHost.WriteResultLine(
                            ReturnCode.Ok, "previous result set");
                    }
                }
                else if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(ReturnCode.Error,
                        "no result");
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       set it (i.e. they already know what it is).
                //
                show = false;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "fresr" interactive command, which
            /// fully resets the local and global results in-place, setting both
            /// return codes to <see cref="ReturnCode.Ok" />.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives <see cref="ReturnCode.Ok" />.
            /// </param>
            /// <param name="localResult">
            /// The local result to reset in-place.  Upon return, receives the
            /// reset local result.
            /// </param>
            /// <param name="code">
            /// Upon return, receives <see cref="ReturnCode.Ok" />.
            /// </param>
            /// <param name="result">
            /// The global result to reset in-place.  Upon return, receives the
            /// reset global result.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void fresr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Fully reset the local and global results.
                //
                if (localResult == null)
                    localResult = String.Empty; /* SET */

                localResult.Reset(ResultFlags.ResetObject); /* RESET */
                localCode = ReturnCode.Ok;

                if (result == null)
                    result = String.Empty; /* SET */

                result.Reset(ResultFlags.ResetObject); /* RESET */
                code = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result reset (in-place)");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "resr" interactive command, which
            /// resets the local and global results, setting both return codes to
            /// <see cref="ReturnCode.Ok" />.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives <see cref="ReturnCode.Ok" />.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the reset (empty) local result.
            /// </param>
            /// <param name="code">
            /// Upon return, receives <see cref="ReturnCode.Ok" />.
            /// </param>
            /// <param name="result">
            /// Upon return, receives the reset (empty) global result.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void resr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Reset the local and global results.
                //
                localResult = String.Empty;
                localCode = ReturnCode.Ok;

                result = Result.Copy(
                    localResult, ResultFlags.CopyObject); /* COPY */

                code = localCode;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result reset");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "clearr" interactive command, which
            /// clears the local result, setting its return code to
            /// <see cref="ReturnCode.Ok" />.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives <see cref="ReturnCode.Ok" />.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the cleared (empty) local result.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void clearr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Clears the local result.
                //
                localResult = String.Empty;
                localCode = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result cleared");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "nullr" interactive command, which
            /// sets the local result to null and its return code to
            /// <see cref="ReturnCode.Ok" />.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives <see cref="ReturnCode.Ok" />.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives null.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void nullr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Nulls the local result.
                //
                localResult = null;
                localCode = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result nulled");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "copyr" interactive command, which
            /// copies the global result and return code into the local result
            /// and return code.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="code">
            /// The global return code to copy into the local return code.
            /// </param>
            /// <param name="result">
            /// The global result to copy into the local result.  This parameter
            /// may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the copied global return code.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a copy of the global result.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void copyr(
                IInteractiveHost interactiveHost,
                ReturnCode code,
                Result result,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Set the local result equal to the global result.
                //
                localResult = Result.Copy(
                    result, ResultFlags.CopyObject); /* COPY */

                localCode = code;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result copied");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "setr" interactive command, which
            /// copies the local result and return code into the global result
            /// and return code.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="localCode">
            /// The local return code to copy into the global return code.
            /// </param>
            /// <param name="localResult">
            /// The local result to copy into the global result.  This parameter
            /// may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="code">
            /// Upon return, receives the copied local return code.
            /// </param>
            /// <param name="result">
            /// Upon return, receives a copy of the local result.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void setr(
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                ref bool show,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Set the global result equal to the local result.
                //
                result = Result.Copy(
                    localResult, ResultFlags.CopyObject); /* COPY */

                code = localCode;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result set");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "mover" interactive command, which
            /// moves the local result and return code into the global result and
            /// return code, then resets the local result.
            /// </summary>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives <see cref="ReturnCode.Ok" /> after the
            /// local result has been moved and reset.
            /// </param>
            /// <param name="localResult">
            /// The local result to move.  Upon return, receives the reset
            /// (empty) local result.
            /// </param>
            /// <param name="code">
            /// Upon return, receives the moved local return code.
            /// </param>
            /// <param name="result">
            /// Upon return, receives the moved local result.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void mover(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Set the global result equal to the local result
                //       and then reset the local result.
                //
                result = localResult; /* MOVE */
                code = localCode;

                localResult = String.Empty;
                localCode = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result transferred");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "lrinfo" interactive command, which
            /// displays detailed information about the local result via the
            /// interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the local result
            /// information.
            /// </param>
            /// <param name="localCode">
            /// The local return code whose information is to be displayed.
            /// </param>
            /// <param name="localResult">
            /// The local result whose information is to be displayed.  This
            /// parameter may be null.
            /// </param>
            /// <param name="localErrorLine">
            /// The error line number associated with the local result.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void lrinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                int localErrorLine,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteResultInfo(
                            "LocalResultInfo", localCode, localResult,
                            localErrorLine, HostOps.GetDetailFlags(interpreter),
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);
                }
                else
                {
                    localCommandResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCommandCode = ReturnCode.Error;
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "grinfo" interactive command, which
            /// displays detailed information about the global result (optionally
            /// including the previous result) via the interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the global result
            /// information.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, indicates whether the
            /// previous result information should also be displayed.
            /// </param>
            /// <param name="code">
            /// The global return code whose information is to be displayed.
            /// </param>
            /// <param name="result">
            /// The global result whose information is to be displayed.  This
            /// parameter may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void grinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ReturnCode code,
                Result result,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                bool localPrevious = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref localPrevious,
                        ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        if (localPrevious)
                        {
                            // informationHost.SavePosition();

                            if (!informationHost.WriteAllResultInfo(
                                    code, result,
                                    Interpreter.GetErrorLine(interpreter),
#if PREVIOUS_RESULT
                                    Interpreter.GetPreviousResult(interpreter),
#else
                                    null,
#endif
                                    HostOps.GetDetailFlags(interpreter), true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            // informationHost.RestorePosition(true);
                        }
                        else
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteResultInfo(
                                    "GlobalResultInfo", code, result,
                                    Interpreter.GetErrorLine(interpreter),
                                    HostOps.GetDetailFlags(interpreter),
                                    true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);
                        }

                        //
                        // NOTE: Skip displaying the local result since we may have
                        //       just shown it.
                        //
                        show = false;
                    }
                    else
                    {
                        localCommandResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCommandCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "rinfo" interactive command, which
            /// displays both the global result and the local result via the
            /// interactive host.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the result information.
            /// </param>
            /// <param name="localCode">
            /// The local return code whose information is to be displayed.
            /// </param>
            /// <param name="localResult">
            /// The local result whose information is to be displayed.  This
            /// parameter may be null.
            /// </param>
            /// <param name="localErrorLine">
            /// The error line number associated with the local result.
            /// </param>
            /// <param name="code">
            /// The global return code whose information is to be displayed.
            /// </param>
            /// <param name="result">
            /// The global result whose information is to be displayed.  This
            /// parameter may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void rinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                int localErrorLine,
                ReturnCode code,
                Result result,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                IDebugHost debugHost = interactiveHost as IDebugHost;

                if (debugHost != null)
                {
                    // debugHost.SavePosition();

                    if (!debugHost.WriteResult("global result: ", code,
                            result, Interpreter.GetErrorLine(interpreter),
                            true))
                    {
                        debugHost.WriteResultLine(ReturnCode.Ok,
                            "no global result available");
                    }

                    if (!debugHost.WriteResult("local result: ", localCode,
                            localResult, localErrorLine, true))
                    {
                        debugHost.WriteResultLine(ReturnCode.Ok,
                            "no local result available");
                    }

                    // debugHost.RestorePosition(true);
                }
                else
                {
                    localCommandResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IDebugHost).Name);

                    localCommandCode = ReturnCode.Error;
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "sresult" interactive command, which
            /// stores the local or global result into a script variable.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to display the outcome of the command.
            /// </param>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the variable name;
            /// the element at index two, when present, indicates whether the
            /// global result should be stored instead of the local result.
            /// </param>
            /// <param name="localCode">
            /// The local return code used when formatting the local result.
            /// </param>
            /// <param name="localResult">
            /// The local result to store.  This parameter may be null.
            /// </param>
            /// <param name="localErrorLine">
            /// The error line number associated with the local result.
            /// </param>
            /// <param name="code">
            /// The global return code used when formatting the global result.
            /// </param>
            /// <param name="result">
            /// The global result to store.  This parameter may be null.
            /// </param>
            /// <param name="show">
            /// Upon return, receives false to indicate that the local result has
            /// already been displayed and should not be shown again.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Safe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void sresult(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ReturnCode localCode,
                Result localResult,
                int localErrorLine,
                ReturnCode code,
                Result result,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                string varName = DefaultResultVarName;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2))
                {
                    varName = debugArguments[1];
                }

                bool global = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.InternalCultureInfo, ref global,
                        ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    string varValue;

                    if (global)
                    {
                        varValue = (code == ReturnCode.Ok) ?
                            (string)result : ResultOps.Format(
                                code, result, Interpreter.GetErrorLine(
                                interpreter));
                    }
                    else
                    {
                        varValue = (localCode == ReturnCode.Ok) ?
                            (string)localResult : ResultOps.Format(
                                localCode, localResult, localErrorLine);
                    }

                    localCommandCode = interpreter.SetVariableValue(
                        VariableFlags.None, varName, varValue,
                        ref localCommandResult);
                }

                //
                // BUGFIX: We do not show the result value; however, we do need
                //         to show that *something* was just done.
                //
                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, (localCommandCode == ReturnCode.Ok) ?
                        (Result)String.Format("{0} result stored to variable \"{1}\"",
                        global ? "global" : "local", varName) : localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we just stored it
                //       into a script variable (i.e. they can easily determine
                //       what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tclsh" interactive command, which
            /// toggles the native Tcl evaluation mode for the interactive loop
            /// and refreshes the interactive host title.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host whose title is refreshed when the mode
            /// changes.
            /// </param>
            /// <param name="tclsh">
            /// The current native Tcl evaluation mode flag.  Upon return,
            /// receives the toggled mode flag.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the new native Tcl
            /// evaluation mode or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tclsh(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref bool tclsh,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NATIVE && TCL
                tclsh = !tclsh; // NOTE: TOGGLE.

                interpreter.InteractiveMode = tclsh ?
                    TclInteractiveMode : EagleInteractiveMode;

                if (interactiveHost != null)
                    interactiveHost.RefreshTitle();

                localResult = String.Format(
                    "native tcl evaluation mode {0}",
                    ConversionOps.ToEnabled(tclsh));

                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "tclinterp" interactive command, which
            /// displays and optionally sets the name of the target Tcl
            /// interpreter to use.
            /// </summary>
            /// <param name="debugArguments">
            /// The list of arguments supplied to the interactive command.  The
            /// element at index one, when present, specifies the target Tcl
            /// interpreter name; an empty string selects any parent Tcl
            /// interpreter.
            /// </param>
            /// <param name="tclInterpName">
            /// The current target Tcl interpreter name.  Upon return, receives
            /// the updated name, or null when any parent Tcl interpreter should
            /// be used.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives the resulting target Tcl interpreter name
            /// or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void tclinterp(
                ArgumentList debugArguments,
                ref string tclInterpName,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NATIVE && TCL
                if (debugArguments.Count >= 2)
                {
                    tclInterpName = debugArguments[1];

                    //
                    // NOTE: In this context an empty string means
                    //       "use any parent Tcl interpreter" (i.e.
                    //       the old default behavior).
                    //
                    if (tclInterpName.Length == 0)
                        tclInterpName = null;
                }

                localResult = tclInterpName;
                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method implements the "queue" interactive command, which
            /// reads a complete logical line of interactive input and queues it
            /// for asynchronous evaluation as an event on a background thread.
            /// </summary>
            /// <param name="interpreter">
            /// The interpreter context for the interactive command.
            /// </param>
            /// <param name="loopData">
            /// The interactive loop data providing context for reading input.
            /// When null, the command fails.
            /// </param>
            /// <param name="refresh">
            /// Non-zero to refresh the interactive prompt before reading input,
            /// zero to suppress it, or null to use the default behavior.
            /// </param>
            /// <param name="noCommand">
            /// Non-zero to disable interactive command processing while reading
            /// input.
            /// </param>
            /// <param name="trace">
            /// Non-zero to enable tracing while reading input.
            /// </param>
            /// <param name="debug">
            /// Non-zero to enable debug behavior while reading input.
            /// </param>
            /// <param name="localEngineFlags">
            /// The engine flags to use when reading input and creating the
            /// queued script.
            /// </param>
            /// <param name="localSubstitutionFlags">
            /// The substitution flags to use when reading input and creating the
            /// queued script.
            /// </param>
            /// <param name="localEventFlags">
            /// The event flags to use when creating the queued script.
            /// </param>
            /// <param name="localExpressionFlags">
            /// The expression flags to use when creating the queued script.
            /// </param>
            /// <param name="clientData">
            /// The client data to pass along while reading input.  This
            /// parameter may be null.
            /// </param>
            /// <param name="forceCancel">
            /// Non-zero to force cancellation while reading input.
            /// </param>
            /// <param name="forceHalt">
            /// Non-zero to force a halt while reading input.
            /// </param>
            /// <param name="interactiveHost">
            /// The interactive host used to read input.  Upon return, receives
            /// the (possibly updated) interactive host.
            /// </param>
            /// <param name="savedText">
            /// The previously saved partial input text.  Upon return, receives
            /// the updated saved text.
            /// </param>
            /// <param name="done">
            /// Upon return, receives true if the interactive loop should
            /// terminate; otherwise, false.
            /// </param>
            /// <param name="previous">
            /// Upon return, receives a value indicating whether the previous
            /// input should be reused.
            /// </param>
            /// <param name="canceled">
            /// Upon return, receives true if reading the input was canceled;
            /// otherwise, false.
            /// </param>
            /// <param name="text">
            /// Upon return, receives the complete logical line of input that was
            /// read.
            /// </param>
            /// <param name="notReady">
            /// Upon return, receives true if the input was not ready to be
            /// processed; otherwise, false.
            /// </param>
            /// <param name="parseError">
            /// Upon return, receives any parse error encountered while reading
            /// the input.
            /// </param>
            /// <param name="localCode">
            /// Upon return, receives the return code indicating success or
            /// failure of the interactive command.
            /// </param>
            /// <param name="localResult">
            /// Upon return, receives a message describing the outcome of the
            /// queue operation or error information.
            /// </param>
            [CommandFlags(CommandFlags.Core | CommandFlags.Unsafe |
                CommandFlags.NonStandard | CommandFlags.Interactive)]
            public static void queue(
                Interpreter interpreter,
                IInteractiveLoopData loopData,
                bool? refresh,
                bool noCommand,
                bool trace,
                bool debug,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                IClientData clientData,
                bool forceCancel,
                bool forceHalt,
                ref IInteractiveHost interactiveHost,
                ref string savedText,
                ref bool done,
                ref bool previous,
                ref bool canceled,
                ref string text,
                ref bool notReady,
                ref Result parseError,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Invoke the method directly responsible for getting a
                //       complete [logical] line of interactive input.
                //
                if (loopData == null)
                {
                    localResult = "invalid interactive loop data";
                    localCode = ReturnCode.Error;

                    return;
                }

                bool exit = loopData.Exit;

                Interpreter.GetInteractiveInput(interpreter, refresh,
#if INTERACTIVE_COMMANDS
                    noCommand,
#endif
                    trace, debug, true, localEngineFlags, localSubstitutionFlags,
                    clientData, forceCancel, forceHalt, ref interactiveHost,
                    ref savedText, ref exit, ref done, ref previous,
                    out canceled, out text, out notReady, out parseError);

                if (exit)
                    loopData.SetExit();

                //
                // NOTE: Do they still want to queue up an event?
                //
                if (!done && !canceled && !notReady)
                {
                    if ((interpreter != null) &&
                        !interpreter.IsInteractiveInputEnabled())
                    {
                        /* NO RESULT */
                        interpreter.BufferInteractiveInput(text, false);

                        localResult = "queue input buffered";
                        localCode = ReturnCode.Ok;
                    }
                    else if (!String.IsNullOrEmpty(text) && (text.Trim().Length > 0))
                    {
                        string name = FormatOps.Id(String.Format(
                            "loop{0}", interpreter.ActiveInteractiveLoops),
                            null, interpreter.NextId());

                        IScript script = Script.Create(
                            name, null, null, ScriptTypes.Queue, text,
                            TimeOps.GetUtcNow(), EngineMode.EvaluateScript,
                            ScriptFlags.None, localEngineFlags,
                            localSubstitutionFlags, localEventFlags,
                            localExpressionFlags, ClientData.Empty);

                        Thread queueThread = Engine.CreateThread(interpreter,
                            _Public.EventManager.QueueEventThreadStart, 0,
                            true, false, true);

                        if (queueThread != null)
                        {
                            queueThread.Name = String.Format(
                                "queueThread: {0}",
                                FormatOps.InterpreterNoThrow(interpreter));

                            queueThread.Start(new AnyPair<Interpreter, IScript>(
                                interpreter, script));

                            localResult = "queue thread started";
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = "could not create queue thread";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                }
                else
                {
                    localResult = "queue event canceled";
                    localCode = ReturnCode.Error;
                }
            }
            #endregion
            #endregion
        }
#endif
        #endregion
    }
}
