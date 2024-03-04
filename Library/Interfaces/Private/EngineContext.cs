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
    [ObjectId("b665807e-3edf-4c13-967d-44692fa0a8d3")]
    internal interface IEngineContext : IThreadContext
#if DEBUGGER
        , IInteractiveLoopManager
#endif
#if SHELL
        , IShellManager
#endif
    {
        IClientData ClientData { get; set; }

        int Levels { get; set; }
        int MaximumLevels { get; set; }

        int TrustedLevels { get; set; }
        int ScriptLevels { get; set; }
        int MaximumScriptLevels { get; set; }

        int ParserLevels { get; set; }
        int MaximumParserLevels { get; set; }

        int ExpressionLevels { get; set; }
        int EntryExpressionLevels { get; set; }
        int MaximumExpressionLevels { get; set; }

        int PreviousLevels { get; set; }
        int CatchLevels { get; set; }
        int UnknownLevels { get; set; }
        int TraceLevels { get; set; }
        int SubCommandLevels { get; set; }
        int SettingLevels { get; set; }
        int PackageLevels { get; set; }

        InterpreterStateFlags InterpreterStateFlags { get; set; }

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        CacheFlags CacheFlags { get; set; }
#endif

#if ARGUMENT_CACHE
        Argument CacheArgument { get; set; }
#endif

#if DEBUGGER
        int WatchpointLevels { get; set; }
        IDebugger Debugger { get; set; }
#endif

#if NOTIFY || NOTIFY_OBJECT
        int NotifyLevels { get; set; }
        NotifyType NotifyTypes { get; set; }
        NotifyFlags NotifyFlags { get; set; }
#endif

        int SecurityLevels { get; set; }
        int PolicyLevels { get; set; }
        int TestLevels { get; set; }

        PolicyDecision CommandInitialDecision { get; set; }
        PolicyDecision ScriptInitialDecision { get; set; }
        PolicyDecision FileInitialDecision { get; set; }
        PolicyDecision StreamInitialDecision { get; set; }

        PolicyDecision CommandFinalDecision { get; set; }
        PolicyDecision ScriptFinalDecision { get; set; }
        PolicyDecision FileFinalDecision { get; set; }
        PolicyDecision StreamFinalDecision { get; set; }

        bool Cancel { get; set; }
        bool Unwind { get; set; }
        bool Halt { get; set; }

        Result CancelResult { get; set; }
        Result HaltResult { get; set; }

#if DEBUGGER
        bool IsDebuggerExiting { get; set; }
#endif

        bool StackOverflow { get; set; }

#if PREVIOUS_RESULT
        Result PreviousResult { get; set; }
#endif

        EngineFlags EngineFlags { get; set; }

        IParseState ParseState { get; set; }

        ReturnCode ReturnCode { get; set; }

        int ErrorLine { get; set; }
        string ErrorCode { get; set; }
        string ErrorInfo { get; set; }
        int ErrorFrames { get; set; }
        Exception Exception { get; set; }

        IScriptLocation ScriptLocation { get; set; }
        ScriptLocationList ScriptLocations { get; set; }

#if SCRIPT_ARGUMENTS
        ArgumentListStack ScriptArguments { get; set; }
#endif

        long PreviousProcessId { get; set; }

        ArraySearchDictionary ArraySearches { get; set; }

#if HISTORY
        IHistoryFilter HistoryEngineFilter { get; set; }
        ClientDataList History { get; set; }
#endif

        string Complaint { get; set; }

        bool CancelEvaluate(
            Result result, bool unwind, bool needResult);

        EngineFlags BeginExternalExecution();
        int EndExternalExecution(EngineFlags savedEngineFlags);

        int BeginNestedExecution();
        void EndNestedExecution(int savedPreviousLevels);
    }
}
