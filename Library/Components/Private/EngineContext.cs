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

#if SCRIPT_ARGUMENTS
using System.Collections.Generic;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if DEBUGGER || SHELL
using Eagle._Components.Public.Delegates;
#endif

using Eagle._Containers.Private;

#if SCRIPT_ARGUMENTS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("54a4fa59-05e7-4c54-ad72-d5496758c582")]
    internal sealed class EngineContext :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IEngineContext, IDisposable
    {
        #region Public Constructors
        public EngineContext(
            Interpreter interpreter,
            long threadId
            )
        {
            this.interpreter = interpreter;
            this.threadId = threadId;

            ///////////////////////////////////////////////////////////////////

            clientData = null;

            levels = 0;
            maximumLevels = 0;

            scriptLevels = 0;
            maximumScriptLevels = 0;

            parserLevels = 0;
            maximumParserLevels = 0;

            expressionLevels = 0;
            entryExpressionLevels = 0;
            maximumExpressionLevels = 0;

            previousLevels = 0;
            catchLevels = 0;
            unknownLevels = 0;
            traceLevels = 0;
            subCommandLevels = 0;
            settingLevels = 0;
            packageLevels = 0;

#if ARGUMENT_CACHE
            cacheArgument = Argument.InternalCreate();
#endif

#if DEBUGGER
            watchpointLevels = 0;
#endif

#if NOTIFY || NOTIFY_OBJECT
            notifyLevels = 0;
            notifyTypes = NotifyType.None;
            notifyFlags = NotifyFlags.None;
#endif

            securityLevels = 0;
            policyLevels = 0;
            testLevels = 0;

            commandInitialDecision = PolicyDecision.None;
            scriptInitialDecision = PolicyDecision.None;
            fileInitialDecision = PolicyDecision.None;
            streamInitialDecision = PolicyDecision.None;

            commandFinalDecision = PolicyDecision.None;
            scriptFinalDecision = PolicyDecision.None;
            fileFinalDecision = PolicyDecision.None;
            streamFinalDecision = PolicyDecision.None;

            cancel = false;
            unwind = false;
            halt = false;

            cancelResult = null;
            haltResult = null;

#if DEBUGGER
            isDebuggerExiting = false;
#endif

            stackOverflow = false;

#if DEBUGGER
            debugger = null;
            interactiveLoopCallback = null;
#endif

#if SHELL
            previewArgumentCallback = null;
            unknownArgumentCallback = null;
            evaluateScriptCallback = null;
            evaluateFileCallback = null;
            evaluateEncodedFileCallback = null;
#endif

#if PREVIOUS_RESULT
            previousResult = null;
#endif

            engineFlags = EngineFlags.None;

            parseState = null;

            returnCode = ReturnCode.Ok;

            errorLine = 0;
            errorCode = null;
            errorInfo = null;
            errorFrames = 0;
            exception = null;

            scriptLocation = null;
            scriptLocations = new ScriptLocationList();

#if SCRIPT_ARGUMENTS
            scriptArguments = new ArgumentListStack();
#endif

            previousProcessId = 0;

            arraySearches = new ArraySearchDictionary();

#if HISTORY
            historyEngineFilter = null;
            history = new ClientDataList();
#endif

            complaint = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { return false; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        private long threadId;
        public long ThreadId
        {
            get
            {
                //
                // NOTE: *EXEMPT* Hot path.
                //
                // CheckDisposed();

                return threadId;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveLoopManager Members
#if DEBUGGER
        private InteractiveLoopCallback interactiveLoopCallback;
        public InteractiveLoopCallback InteractiveLoopCallback
        {
            get { CheckDisposed(); return interactiveLoopCallback; }
            set { CheckDisposed(); interactiveLoopCallback = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IShellManager Members
#if SHELL
        private PreviewArgumentCallback previewArgumentCallback;
        public PreviewArgumentCallback PreviewArgumentCallback
        {
            get { CheckDisposed(); return previewArgumentCallback; }
            set { CheckDisposed(); previewArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private UnknownArgumentCallback unknownArgumentCallback;
        public UnknownArgumentCallback UnknownArgumentCallback
        {
            get { CheckDisposed(); return unknownArgumentCallback; }
            set { CheckDisposed(); unknownArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateScriptCallback evaluateScriptCallback;
        public EvaluateScriptCallback EvaluateScriptCallback
        {
            get { CheckDisposed(); return evaluateScriptCallback; }
            set { CheckDisposed(); evaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateFileCallback evaluateFileCallback;
        public EvaluateFileCallback EvaluateFileCallback
        {
            get { CheckDisposed(); return evaluateFileCallback; }
            set { CheckDisposed(); evaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateEncodedFileCallback evaluateEncodedFileCallback;
        public EvaluateEncodedFileCallback EvaluateEncodedFileCallback
        {
            get { CheckDisposed(); return evaluateEncodedFileCallback; }
            set { CheckDisposed(); evaluateEncodedFileCallback = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEngineContext Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int levels;
        public int Levels
        {
            get { CheckDisposed(); return levels; }
            set { CheckDisposed(); levels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int maximumLevels;
        public int MaximumLevels
        {
            get { CheckDisposed(); return maximumLevels; }
            set { CheckDisposed(); maximumLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int scriptLevels;
        public int ScriptLevels
        {
            get { CheckDisposed(); return scriptLevels; }
            set { CheckDisposed(); scriptLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int maximumScriptLevels;
        public int MaximumScriptLevels
        {
            get { CheckDisposed(); return maximumScriptLevels; }
            set { CheckDisposed(); maximumScriptLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int parserLevels;
        public int ParserLevels
        {
            get { CheckDisposed(); return parserLevels; }
            set { CheckDisposed(); parserLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int maximumParserLevels;
        public int MaximumParserLevels
        {
            get { CheckDisposed(); return maximumParserLevels; }
            set { CheckDisposed(); maximumParserLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int expressionLevels;
        public int ExpressionLevels
        {
            get { CheckDisposed(); return expressionLevels; }
            set { CheckDisposed(); expressionLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int entryExpressionLevels;
        public int EntryExpressionLevels
        {
            get { CheckDisposed(); return entryExpressionLevels; }
            set { CheckDisposed(); entryExpressionLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int maximumExpressionLevels;
        public int MaximumExpressionLevels
        {
            get { CheckDisposed(); return maximumExpressionLevels; }
            set { CheckDisposed(); maximumExpressionLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int previousLevels;
        public int PreviousLevels
        {
            get { CheckDisposed(); return previousLevels; }
            set { CheckDisposed(); previousLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int catchLevels;
        public int CatchLevels
        {
            get { CheckDisposed(); return catchLevels; }
            set { CheckDisposed(); catchLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int unknownLevels;
        public int UnknownLevels
        {
            get { CheckDisposed(); return unknownLevels; }
            set { CheckDisposed(); unknownLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int traceLevels;
        public int TraceLevels
        {
            get { CheckDisposed(); return traceLevels; }
            set { CheckDisposed(); traceLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int subCommandLevels;
        public int SubCommandLevels
        {
            get { CheckDisposed(); return subCommandLevels; }
            set { CheckDisposed(); subCommandLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int settingLevels;
        public int SettingLevels
        {
            get { CheckDisposed(); return settingLevels; }
            set { CheckDisposed(); settingLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int packageLevels;
        public int PackageLevels
        {
            get { CheckDisposed(); return packageLevels; }
            set { CheckDisposed(); packageLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        private Argument cacheArgument;
        public Argument CacheArgument
        {
            get { CheckDisposed(); return cacheArgument; }
            set { CheckDisposed(); cacheArgument = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private int watchpointLevels;
        public int WatchpointLevels
        {
            get { CheckDisposed(); return watchpointLevels; }
            set { CheckDisposed(); watchpointLevels = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        private int notifyLevels;
        public int NotifyLevels
        {
            get { CheckDisposed(); return notifyLevels; }
            set { CheckDisposed(); notifyLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private NotifyType notifyTypes;
        public NotifyType NotifyTypes
        {
            get { CheckDisposed(); return notifyTypes; }
            set { CheckDisposed(); notifyTypes = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private NotifyFlags notifyFlags;
        public NotifyFlags NotifyFlags
        {
            get { CheckDisposed(); return notifyFlags; }
            set { CheckDisposed(); notifyFlags = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private int securityLevels;
        public int SecurityLevels
        {
            get { CheckDisposed(); return securityLevels; }
            set { CheckDisposed(); securityLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int policyLevels;
        public int PolicyLevels
        {
            get { CheckDisposed(); return policyLevels; }
            set { CheckDisposed(); policyLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int testLevels;
        public int TestLevels
        {
            get { CheckDisposed(); return testLevels; }
            set { CheckDisposed(); testLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision commandInitialDecision;
        public PolicyDecision CommandInitialDecision
        {
            get { CheckDisposed(); return commandInitialDecision; }
            set { CheckDisposed(); commandInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision scriptInitialDecision;
        public PolicyDecision ScriptInitialDecision
        {
            get { CheckDisposed(); return scriptInitialDecision; }
            set { CheckDisposed(); scriptInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision fileInitialDecision;
        public PolicyDecision FileInitialDecision
        {
            get { CheckDisposed(); return fileInitialDecision; }
            set { CheckDisposed(); fileInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision streamInitialDecision;
        public PolicyDecision StreamInitialDecision
        {
            get { CheckDisposed(); return streamInitialDecision; }
            set { CheckDisposed(); streamInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision commandFinalDecision;
        public PolicyDecision CommandFinalDecision
        {
            get { CheckDisposed(); return commandFinalDecision; }
            set { CheckDisposed(); commandFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision scriptFinalDecision;
        public PolicyDecision ScriptFinalDecision
        {
            get { CheckDisposed(); return scriptFinalDecision; }
            set { CheckDisposed(); scriptFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision fileFinalDecision;
        public PolicyDecision FileFinalDecision
        {
            get { CheckDisposed(); return fileFinalDecision; }
            set { CheckDisposed(); fileFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PolicyDecision streamFinalDecision;
        public PolicyDecision StreamFinalDecision
        {
            get { CheckDisposed(); return streamFinalDecision; }
            set { CheckDisposed(); streamFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool cancel;
        public bool Cancel
        {
            get { CheckDisposed(); return cancel; }
            set { CheckDisposed(); cancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool unwind;
        public bool Unwind
        {
            get { CheckDisposed(); return unwind; }
            set { CheckDisposed(); unwind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool halt;
        public bool Halt
        {
            get { CheckDisposed(); return halt; }
            set { CheckDisposed(); halt = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result cancelResult;
        public Result CancelResult
        {
            get { CheckDisposed(); return cancelResult; }
            set { CheckDisposed(); cancelResult = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result haltResult;
        public Result HaltResult
        {
            get { CheckDisposed(); return haltResult; }
            set { CheckDisposed(); haltResult = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private bool isDebuggerExiting;
        public bool IsDebuggerExiting
        {
            get { CheckDisposed(); return isDebuggerExiting; }
            set { CheckDisposed(); isDebuggerExiting = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool stackOverflow;
        public bool StackOverflow
        {
            get { CheckDisposed(); return stackOverflow; }
            set { CheckDisposed(); stackOverflow = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private IDebugger debugger;
        public IDebugger Debugger
        {
            get { CheckDisposed(); return debugger; }
            set { CheckDisposed(); debugger = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if PREVIOUS_RESULT
        private Result previousResult;
        public Result PreviousResult
        {
            get { CheckDisposed(); return previousResult; }
            set { CheckDisposed(); previousResult = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public EngineFlags EngineFlags
        {
            get { CheckDisposed(); return engineFlags; }
            set { CheckDisposed(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IParseState parseState;
        public IParseState ParseState
        {
            get { CheckDisposed(); return parseState; }
            set { CheckDisposed(); parseState = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            get { CheckDisposed(); return returnCode; }
            set { CheckDisposed(); returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int errorLine;
        public int ErrorLine
        {
            get { CheckDisposed(); return errorLine; }
            set { CheckDisposed(); errorLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorCode;
        public string ErrorCode
        {
            get { CheckDisposed(); return errorCode; }
            set { CheckDisposed(); errorCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorInfo;
        public string ErrorInfo
        {
            get { CheckDisposed(); return errorInfo; }
            set { CheckDisposed(); errorInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int errorFrames;
        public int ErrorFrames
        {
            get { CheckDisposed(); return errorFrames; }
            set { CheckDisposed(); errorFrames = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Exception exception;
        public Exception Exception
        {
            get { CheckDisposed(); return exception; }
            set { CheckDisposed(); exception = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScriptLocation scriptLocation;
        public IScriptLocation ScriptLocation
        {
            get { CheckDisposed(); return scriptLocation; }
            set { CheckDisposed(); scriptLocation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptLocationList scriptLocations;
        public ScriptLocationList ScriptLocations
        {
            get { CheckDisposed(); return scriptLocations; }
            set { CheckDisposed(); scriptLocations = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SCRIPT_ARGUMENTS
        private ArgumentListStack scriptArguments;
        public ArgumentListStack ScriptArguments
        {
            get { CheckDisposed(); return scriptArguments; }
            set { CheckDisposed(); scriptArguments = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private long previousProcessId;
        public long PreviousProcessId
        {
            get { CheckDisposed(); return previousProcessId; }
            set { CheckDisposed(); previousProcessId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArraySearchDictionary arraySearches;
        public ArraySearchDictionary ArraySearches
        {
            get { CheckDisposed(); return arraySearches; }
            set { CheckDisposed(); arraySearches = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        private IHistoryFilter historyEngineFilter;
        public IHistoryFilter HistoryEngineFilter
        {
            get { CheckDisposed(); return historyEngineFilter; }
            set { CheckDisposed(); historyEngineFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ClientDataList history;
        public ClientDataList History
        {
            get { CheckDisposed(); return history; }
            set { CheckDisposed(); history = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private string complaint;
        public string Complaint
        {
            get { CheckDisposed(); return complaint; }
            set { CheckDisposed(); complaint = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CancelEvaluate(
            Result result,
            bool unwind,
            bool needResult
            )
        {
            CheckDisposed();

            this.cancel = true;

            if (unwind)
                this.unwind = true;

            if (needResult)
                this.cancelResult = result;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public EngineFlags BeginExternalExecution()
        {
            CheckDisposed();

            levels++;

            engineFlags |= EngineFlags.ExternalExecution;
            return Engine.AddStackCheckFlags(ref engineFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public int EndExternalExecution(
            EngineFlags savedEngineFlags
            )
        {
            CheckDisposed();

            Engine.RemoveStackCheckFlags(savedEngineFlags, ref engineFlags);
            engineFlags &= ~EngineFlags.ExternalExecution;

            return --levels;
        }

        ///////////////////////////////////////////////////////////////////////

        public int BeginNestedExecution()
        {
            CheckDisposed();

            int savedPreviousLevels = previousLevels;
            previousLevels = levels;

            return savedPreviousLevels;
        }

        ///////////////////////////////////////////////////////////////////////

        public void EndNestedExecution(
            int savedPreviousLevels
            )
        {
            CheckDisposed();

            previousLevels = savedPreviousLevels;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(EngineContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: disposing = {0}, interpreter = {1}, disposed = {2}",
                disposing, FormatOps.InterpreterNoThrow(interpreter), disposed),
                typeof(EngineContext).Name, TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    interpreter = null; /* NOT OWNED: Do not dispose. */
                    threadId = 0;

                    ///////////////////////////////////////////////////////////

#if DEBUGGER
                    interactiveLoopCallback = null;
#endif

                    ///////////////////////////////////////////////////////////

#if SHELL
                    previewArgumentCallback = null;
                    unknownArgumentCallback = null;
                    evaluateScriptCallback = null;
                    evaluateFileCallback = null;
                    evaluateEncodedFileCallback = null;
#endif

                    ///////////////////////////////////////////////////////////

                    clientData = null;

                    levels = 0;
                    maximumLevels = 0;

                    scriptLevels = 0;
                    maximumScriptLevels = 0;

                    parserLevels = 0;
                    maximumParserLevels = 0;

                    expressionLevels = 0;
                    entryExpressionLevels = 0;
                    maximumExpressionLevels = 0;

                    previousLevels = 0;
                    catchLevels = 0;
                    unknownLevels = 0;
                    traceLevels = 0;
                    subCommandLevels = 0;
                    settingLevels = 0;
                    packageLevels = 0;

#if ARGUMENT_CACHE
                    cacheArgument = null;
#endif

#if DEBUGGER
                    watchpointLevels = 0;
#endif

#if NOTIFY || NOTIFY_OBJECT
                    notifyLevels = 0;
                    notifyTypes = NotifyType.None;
                    notifyFlags = NotifyFlags.None;
#endif

                    securityLevels = 0;
                    policyLevels = 0;
                    testLevels = 0;

                    commandInitialDecision = PolicyDecision.None;
                    scriptInitialDecision = PolicyDecision.None;
                    fileInitialDecision = PolicyDecision.None;
                    streamInitialDecision = PolicyDecision.None;

                    commandFinalDecision = PolicyDecision.None;
                    scriptFinalDecision = PolicyDecision.None;
                    fileFinalDecision = PolicyDecision.None;
                    streamFinalDecision = PolicyDecision.None;

                    cancel = false;
                    unwind = false;
                    halt = false;

                    cancelResult = null;
                    haltResult = null;

#if DEBUGGER
                    isDebuggerExiting = false;
#endif

                    stackOverflow = false;

#if DEBUGGER
                    if (debugger != null)
                    {
                        IDisposable disposable = debugger as IDisposable;

                        if (disposable != null)
                        {
                            disposable.Dispose();
                            disposable = null;
                        }

                        debugger = null;
                    }
#endif

#if PREVIOUS_RESULT
                    previousResult = null;
#endif

                    engineFlags = EngineFlags.None;
                    parseState = null;
                    returnCode = ReturnCode.Ok;
                    errorLine = 0;
                    errorCode = null;
                    errorInfo = null;
                    errorFrames = 0;
                    exception = null;
                    scriptLocation = null;

                    ///////////////////////////////////////////////////////////

                    if (scriptLocations != null)
                    {
                        scriptLocations.Clear();
                        scriptLocations = null;
                    }

                    ///////////////////////////////////////////////////////////

#if SCRIPT_ARGUMENTS
                    if (scriptArguments != null)
                    {
                        scriptArguments.Clear();
                        scriptArguments = null;
                    }
#endif

                    ///////////////////////////////////////////////////////////

                    previousProcessId = 0;

                    ///////////////////////////////////////////////////////////

                    if (arraySearches != null)
                    {
                        arraySearches.Clear();
                        arraySearches = null;
                    }

                    ///////////////////////////////////////////////////////////

#if HISTORY
                    historyEngineFilter = null;

                    if (history != null)
                    {
                        history.Clear();
                        history = null;
                    }
#endif

                    ///////////////////////////////////////////////////////////

                    complaint = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~EngineContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
