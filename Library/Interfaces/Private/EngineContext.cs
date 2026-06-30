/*
 * EngineContext.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface represents the per-thread engine execution context for an
    /// interpreter, holding the engine's nesting levels, state and feature
    /// flags, policy decisions, cancellation and error state, and other
    /// bookkeeping used while evaluating scripts.
    /// </summary>
    [ObjectId("b665807e-3edf-4c13-967d-44692fa0a8d3")]
    internal interface IEngineContext : IThreadContext
#if DEBUGGER
        , IInteractiveLoopManager
#endif
#if SHELL
        , IShellManager
#endif
    {
        /// <summary>
        /// Gets or sets the extra, context-specific data associated with this
        /// engine context.
        /// </summary>
        IClientData ClientData { get; set; }

        /// <summary>
        /// Gets or sets the current engine evaluation nesting level.
        /// </summary>
        int Levels { get; set; }

        /// <summary>
        /// Gets or sets the maximum engine evaluation nesting level reached.
        /// </summary>
        int MaximumLevels { get; set; }

        /// <summary>
        /// Gets or sets the current trusted evaluation nesting level.
        /// </summary>
        int TrustedLevels { get; set; }

        /// <summary>
        /// Gets or sets the current script evaluation nesting level.
        /// </summary>
        int ScriptLevels { get; set; }

        /// <summary>
        /// Gets or sets the maximum script evaluation nesting level reached.
        /// </summary>
        int MaximumScriptLevels { get; set; }

        /// <summary>
        /// Gets or sets the current script file evaluation nesting level.
        /// </summary>
        int ScriptFileLevels { get; set; }

        /// <summary>
        /// Gets or sets the maximum script file evaluation nesting level
        /// reached.
        /// </summary>
        int MaximumScriptFileLevels { get; set; }

        /// <summary>
        /// Gets or sets the current parser nesting level.
        /// </summary>
        int ParserLevels { get; set; }

        /// <summary>
        /// Gets or sets the maximum parser nesting level reached.
        /// </summary>
        int MaximumParserLevels { get; set; }

        /// <summary>
        /// Gets or sets the current expression evaluation nesting level.
        /// </summary>
        int ExpressionLevels { get; set; }

        /// <summary>
        /// Gets or sets the expression evaluation nesting level upon entry to
        /// the engine.
        /// </summary>
        int EntryExpressionLevels { get; set; }

        /// <summary>
        /// Gets or sets the maximum expression evaluation nesting level
        /// reached.
        /// </summary>
        int MaximumExpressionLevels { get; set; }

        /// <summary>
        /// Gets or sets the previous engine evaluation nesting level.
        /// </summary>
        int PreviousLevels { get; set; }

        /// <summary>
        /// Gets or sets the current catch nesting level.
        /// </summary>
        int CatchLevels { get; set; }

        /// <summary>
        /// Gets or sets the current unknown-command handler nesting level.
        /// </summary>
        int UnknownLevels { get; set; }

        /// <summary>
        /// Gets or sets the current trace nesting level.
        /// </summary>
        int TraceLevels { get; set; }

        /// <summary>
        /// Gets or sets the current sub-command nesting level.
        /// </summary>
        int SubCommandLevels { get; set; }

        /// <summary>
        /// Gets or sets the current setting nesting level.
        /// </summary>
        int SettingLevels { get; set; }

        /// <summary>
        /// Gets or sets the current package nesting level.
        /// </summary>
        int PackageLevels { get; set; }

        /// <summary>
        /// Gets or sets the current package index nesting level.
        /// </summary>
        int PackageIndexLevels { get; set; }

        /// <summary>
        /// Gets or sets the interpreter state flags associated with this engine
        /// context.
        /// </summary>
        InterpreterStateFlags InterpreterStateFlags { get; set; }

        /// <summary>
        /// Gets or sets the package flags associated with this engine context.
        /// </summary>
        PackageFlags PackageFlags { get; set; }

        /// <summary>
        /// Gets or sets the package index flags associated with this engine
        /// context.
        /// </summary>
        PackageIndexFlags PackageIndexFlags { get; set; }

        /// <summary>
        /// Gets or sets the procedure flags associated with this engine
        /// context.
        /// </summary>
        ProcedureFlags ProcedureFlags { get; set; }

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        /// <summary>
        /// Gets or sets the cache flags associated with this engine context.
        /// </summary>
        CacheFlags CacheFlags { get; set; }
#endif

#if ARGUMENT_CACHE
        /// <summary>
        /// Gets or sets the cached argument associated with this engine
        /// context.
        /// </summary>
        Argument CacheArgument { get; set; }
#endif

#if DEBUGGER
        /// <summary>
        /// Gets or sets the current watchpoint nesting level.
        /// </summary>
        int WatchpointLevels { get; set; }

        /// <summary>
        /// Gets or sets the script debugger associated with this engine
        /// context.
        /// </summary>
        IDebugger Debugger { get; set; }
#endif

#if NOTIFY || NOTIFY_OBJECT
        /// <summary>
        /// Gets or sets the current notification nesting level.
        /// </summary>
        int NotifyLevels { get; set; }

        /// <summary>
        /// Gets or sets the notification types associated with this engine
        /// context.
        /// </summary>
        NotifyType NotifyTypes { get; set; }

        /// <summary>
        /// Gets or sets the notification flags associated with this engine
        /// context.
        /// </summary>
        NotifyFlags NotifyFlags { get; set; }
#endif

        /// <summary>
        /// Gets or sets the current security nesting level.
        /// </summary>
        int SecurityLevels { get; set; }

        /// <summary>
        /// Gets or sets the current policy nesting level.
        /// </summary>
        int PolicyLevels { get; set; }

        /// <summary>
        /// Gets or sets the current test nesting level.
        /// </summary>
        int TestLevels { get; set; }

        /// <summary>
        /// Gets or sets the initial policy decision applied to command
        /// execution.
        /// </summary>
        PolicyDecision CommandInitialDecision { get; set; }

        /// <summary>
        /// Gets or sets the initial policy decision applied to script
        /// evaluation.
        /// </summary>
        PolicyDecision ScriptInitialDecision { get; set; }

        /// <summary>
        /// Gets or sets the initial policy decision applied to file evaluation.
        /// </summary>
        PolicyDecision FileInitialDecision { get; set; }

        /// <summary>
        /// Gets or sets the initial policy decision applied to stream
        /// evaluation.
        /// </summary>
        PolicyDecision StreamInitialDecision { get; set; }

        /// <summary>
        /// Gets or sets the final policy decision applied to command execution.
        /// </summary>
        PolicyDecision CommandFinalDecision { get; set; }

        /// <summary>
        /// Gets or sets the final policy decision applied to script evaluation.
        /// </summary>
        PolicyDecision ScriptFinalDecision { get; set; }

        /// <summary>
        /// Gets or sets the final policy decision applied to file evaluation.
        /// </summary>
        PolicyDecision FileFinalDecision { get; set; }

        /// <summary>
        /// Gets or sets the final policy decision applied to stream evaluation.
        /// </summary>
        PolicyDecision StreamFinalDecision { get; set; }

        /// <summary>
        /// Gets or sets the timeout, in milliseconds, used when checking
        /// whether the interpreter is ready.
        /// </summary>
        int? ReadyTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether script evaluation has been
        /// canceled.
        /// </summary>
        bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the evaluation stack is
        /// being unwound.
        /// </summary>
        bool Unwind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether script evaluation has been
        /// halted.
        /// </summary>
        bool Halt { get; set; }

        /// <summary>
        /// Gets or sets the result associated with a script cancellation.
        /// </summary>
        Result CancelResult { get; set; }

        /// <summary>
        /// Gets or sets the result associated with a script halt.
        /// </summary>
        Result HaltResult { get; set; }

#if DEBUGGER
        /// <summary>
        /// Gets or sets a value indicating whether the debugger is in the
        /// process of exiting.
        /// </summary>
        bool IsDebuggerExiting { get; set; }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether a stack overflow condition
        /// has been detected.
        /// </summary>
        bool StackOverflow { get; set; }

#if PREVIOUS_RESULT
        /// <summary>
        /// Gets or sets the result produced by the previous evaluation.
        /// </summary>
        Result PreviousResult { get; set; }
#endif

        /// <summary>
        /// Gets or sets the result associated with the most recent error.
        /// </summary>
        Result LastError { get; set; }

        /// <summary>
        /// Gets or sets the engine flags associated with this engine context.
        /// </summary>
        EngineFlags EngineFlags { get; set; }

        /// <summary>
        /// Gets or sets the current parser state for this engine context.
        /// </summary>
        IParseState ParseState { get; set; }

        /// <summary>
        /// Gets or sets the return code associated with the most recent
        /// evaluation.
        /// </summary>
        ReturnCode ReturnCode { get; set; }

        /// <summary>
        /// Gets or sets the line number where the most recent error occurred.
        /// </summary>
        int ErrorLine { get; set; }

        /// <summary>
        /// Gets or sets the value of the error code for the most recent error.
        /// </summary>
        string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the stack trace information for the most recent error.
        /// </summary>
        string ErrorInfo { get; set; }

        /// <summary>
        /// Gets or sets the number of call frames associated with the most
        /// recent error.
        /// </summary>
        int ErrorFrames { get; set; }

        /// <summary>
        /// Gets or sets the exception associated with the most recent error.
        /// </summary>
        Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the current script location for this engine context.
        /// </summary>
        IScriptLocation ScriptLocation { get; set; }

        /// <summary>
        /// Gets or sets the stack of script locations for this engine context.
        /// </summary>
        ScriptLocationList ScriptLocations { get; set; }

#if SCRIPT_ARGUMENTS
        /// <summary>
        /// Gets or sets the stack of script argument lists for this engine
        /// context.
        /// </summary>
        ArgumentListStack ScriptArguments { get; set; }
#endif

        /// <summary>
        /// Gets or sets the previous process identifier associated with this
        /// engine context.
        /// </summary>
        long PreviousProcessId { get; set; }

        /// <summary>
        /// Gets or sets the collection of active array searches for this engine
        /// context.
        /// </summary>
        ArraySearchDictionary ArraySearches { get; set; }

#if HISTORY
        /// <summary>
        /// Gets or sets the filter used to determine which commands are added
        /// to the engine history.
        /// </summary>
        IHistoryFilter HistoryEngineFilter { get; set; }

        /// <summary>
        /// Gets or sets the engine command history for this engine context.
        /// </summary>
        ClientDataList History { get; set; }
#endif

        /// <summary>
        /// Gets or sets the most recent complaint message for this engine
        /// context.
        /// </summary>
        string Complaint { get; set; }

        /// <summary>
        /// This method requests cancellation of the current script evaluation.
        /// </summary>
        /// <param name="result">
        /// The result to associate with the cancellation.  This parameter may
        /// be null.
        /// </param>
        /// <param name="unwind">
        /// Non-zero to unwind the evaluation stack as part of the cancellation.
        /// </param>
        /// <param name="needResult">
        /// Non-zero if a result is required for the cancellation.
        /// </param>
        /// <returns>
        /// True if the cancellation was requested; otherwise, false.
        /// </returns>
        bool CancelEvaluate(
            Result result, bool unwind, bool needResult);

        /// <summary>
        /// This method marks the beginning of an externally initiated
        /// execution, adjusting the engine flags accordingly.
        /// </summary>
        /// <returns>
        /// The engine flags that were in effect prior to this call.
        /// </returns>
        EngineFlags BeginExternalExecution();

        /// <summary>
        /// This method marks the end of an externally initiated execution,
        /// restoring the previously saved engine flags.
        /// </summary>
        /// <param name="savedEngineFlags">
        /// The engine flags that were in effect prior to the matching call that
        /// began the external execution.
        /// </param>
        /// <returns>
        /// The resulting external execution nesting level.
        /// </returns>
        int EndExternalExecution(EngineFlags savedEngineFlags);

        /// <summary>
        /// This method marks the beginning of a nested execution.
        /// </summary>
        /// <returns>
        /// The previous nesting level value to be restored when the matching
        /// call that ends the nested execution is made.
        /// </returns>
        int BeginNestedExecution();

        /// <summary>
        /// This method marks the end of a nested execution, restoring the
        /// previous nesting level.
        /// </summary>
        /// <param name="savedPreviousLevels">
        /// The previous nesting level value returned by the matching call that
        /// began the nested execution.
        /// </param>
        void EndNestedExecution(int savedPreviousLevels);
    }
}
