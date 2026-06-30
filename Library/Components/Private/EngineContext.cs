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
    /// <summary>
    /// This class holds the per-thread execution state used by the Eagle engine,
    /// including the owning interpreter and thread identity, the various nesting
    /// level counters, policy decisions, cancellation and halt state, error
    /// information, script locations and arguments, and assorted callbacks.  It
    /// implements <see cref="IEngineContext" /> and is disposable.
    /// </summary>
    [ObjectId("54a4fa59-05e7-4c54-ad72-d5496758c582")]
    internal sealed class EngineContext :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IEngineContext, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an engine context for the specified interpreter and thread,
        /// initializing all execution state to its default values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this engine context.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread associated with this engine context.
        /// </param>
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

            trustedLevels = 0;

            scriptLevels = 0;
            maximumScriptLevels = 0;

            scriptFileLevels = 0;
            maximumScriptFileLevels = 0;

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
            packageIndexLevels = 0;

            interpreterStateFlags = InterpreterStateFlags.None;

            packageFlags = PackageFlags.None;
            packageIndexFlags = PackageIndexFlags.None;
            procedureFlags = ProcedureFlags.None;

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
            cacheFlags = CacheFlags.None;
#endif

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

            readyTimeout = null;

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

            lastError = null;
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
        /// <summary>
        /// Gets a value indicating whether this engine context has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this engine context is currently being
        /// disposed.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// Stores the interpreter that owns this engine context.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that owns this engine context.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        /// <summary>
        /// Stores the identifier of the thread associated with this engine
        /// context.
        /// </summary>
        private long threadId;
        /// <summary>
        /// Gets the identifier of the thread associated with this engine
        /// context.
        /// </summary>
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
        /// <summary>
        /// Stores the callback invoked to run the interactive loop for this
        /// engine context.
        /// </summary>
        private InteractiveLoopCallback interactiveLoopCallback;
        /// <summary>
        /// Gets or sets the callback invoked to run the interactive loop for
        /// this engine context.
        /// </summary>
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
        /// <summary>
        /// Stores the callback invoked to preview shell arguments for this
        /// engine context.
        /// </summary>
        private PreviewArgumentCallback previewArgumentCallback;
        /// <summary>
        /// Gets or sets the callback invoked to preview shell arguments for
        /// this engine context.
        /// </summary>
        public PreviewArgumentCallback PreviewArgumentCallback
        {
            get { CheckDisposed(); return previewArgumentCallback; }
            set { CheckDisposed(); previewArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback invoked to handle unknown shell arguments for
        /// this engine context.
        /// </summary>
        private UnknownArgumentCallback unknownArgumentCallback;
        /// <summary>
        /// Gets or sets the callback invoked to handle unknown shell arguments
        /// for this engine context.
        /// </summary>
        public UnknownArgumentCallback UnknownArgumentCallback
        {
            get { CheckDisposed(); return unknownArgumentCallback; }
            set { CheckDisposed(); unknownArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback invoked to evaluate a script for this engine
        /// context.
        /// </summary>
        private EvaluateScriptCallback evaluateScriptCallback;
        /// <summary>
        /// Gets or sets the callback invoked to evaluate a script for this
        /// engine context.
        /// </summary>
        public EvaluateScriptCallback EvaluateScriptCallback
        {
            get { CheckDisposed(); return evaluateScriptCallback; }
            set { CheckDisposed(); evaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback invoked to evaluate a file for this engine
        /// context.
        /// </summary>
        private EvaluateFileCallback evaluateFileCallback;
        /// <summary>
        /// Gets or sets the callback invoked to evaluate a file for this
        /// engine context.
        /// </summary>
        public EvaluateFileCallback EvaluateFileCallback
        {
            get { CheckDisposed(); return evaluateFileCallback; }
            set { CheckDisposed(); evaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback invoked to evaluate an encoded file for this
        /// engine context.
        /// </summary>
        private EvaluateEncodedFileCallback evaluateEncodedFileCallback;
        /// <summary>
        /// Gets or sets the callback invoked to evaluate an encoded file for
        /// this engine context.
        /// </summary>
        public EvaluateEncodedFileCallback EvaluateEncodedFileCallback
        {
            get { CheckDisposed(); return evaluateEncodedFileCallback; }
            set { CheckDisposed(); evaluateEncodedFileCallback = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEngineContext Members
        /// <summary>
        /// Stores the client data associated with this engine context.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this engine context.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current execution level count for this engine context.
        /// </summary>
        private int levels;
        /// <summary>
        /// Gets or sets the current execution level count for this engine
        /// context.
        /// </summary>
        public int Levels
        {
            get { CheckDisposed(); return levels; }
            set { CheckDisposed(); levels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the maximum execution level count reached by this engine
        /// context.
        /// </summary>
        private int maximumLevels;
        /// <summary>
        /// Gets or sets the maximum execution level count reached by this
        /// engine context.
        /// </summary>
        public int MaximumLevels
        {
            get { CheckDisposed(); return maximumLevels; }
            set { CheckDisposed(); maximumLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current trusted execution level count for this engine
        /// context.
        /// </summary>
        private int trustedLevels;
        /// <summary>
        /// Gets or sets the current trusted execution level count for this
        /// engine context.
        /// </summary>
        public int TrustedLevels
        {
            get { CheckDisposed(); return trustedLevels; }
            set { CheckDisposed(); trustedLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current script evaluation level count for this engine
        /// context.
        /// </summary>
        private int scriptLevels;
        /// <summary>
        /// Gets or sets the current script evaluation level count for this
        /// engine context.
        /// </summary>
        public int ScriptLevels
        {
            get { CheckDisposed(); return scriptLevels; }
            set { CheckDisposed(); scriptLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the maximum script evaluation level count reached by this
        /// engine context.
        /// </summary>
        private int maximumScriptLevels;
        /// <summary>
        /// Gets or sets the maximum script evaluation level count reached by
        /// this engine context.
        /// </summary>
        public int MaximumScriptLevels
        {
            get { CheckDisposed(); return maximumScriptLevels; }
            set { CheckDisposed(); maximumScriptLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current script file evaluation level count for this
        /// engine context.
        /// </summary>
        private int scriptFileLevels;
        /// <summary>
        /// Gets or sets the current script file evaluation level count for
        /// this engine context.
        /// </summary>
        public int ScriptFileLevels
        {
            get { CheckDisposed(); return scriptFileLevels; }
            set { CheckDisposed(); scriptFileLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the maximum script file evaluation level count reached by
        /// this engine context.
        /// </summary>
        private int maximumScriptFileLevels;
        /// <summary>
        /// Gets or sets the maximum script file evaluation level count reached
        /// by this engine context.
        /// </summary>
        public int MaximumScriptFileLevels
        {
            get { CheckDisposed(); return maximumScriptFileLevels; }
            set { CheckDisposed(); maximumScriptFileLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current parser nesting level count for this engine
        /// context.
        /// </summary>
        private int parserLevels;
        /// <summary>
        /// Gets or sets the current parser nesting level count for this engine
        /// context.
        /// </summary>
        public int ParserLevels
        {
            get { CheckDisposed(); return parserLevels; }
            set { CheckDisposed(); parserLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the maximum parser nesting level count reached by this
        /// engine context.
        /// </summary>
        private int maximumParserLevels;
        /// <summary>
        /// Gets or sets the maximum parser nesting level count reached by this
        /// engine context.
        /// </summary>
        public int MaximumParserLevels
        {
            get { CheckDisposed(); return maximumParserLevels; }
            set { CheckDisposed(); maximumParserLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current expression evaluation level count for this
        /// engine context.
        /// </summary>
        private int expressionLevels;
        /// <summary>
        /// Gets or sets the current expression evaluation level count for this
        /// engine context.
        /// </summary>
        public int ExpressionLevels
        {
            get { CheckDisposed(); return expressionLevels; }
            set { CheckDisposed(); expressionLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the expression evaluation level count upon entry to the
        /// current expression for this engine context.
        /// </summary>
        private int entryExpressionLevels;
        /// <summary>
        /// Gets or sets the expression evaluation level count upon entry to
        /// the current expression for this engine context.
        /// </summary>
        public int EntryExpressionLevels
        {
            get { CheckDisposed(); return entryExpressionLevels; }
            set { CheckDisposed(); entryExpressionLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the maximum expression evaluation level count reached by
        /// this engine context.
        /// </summary>
        private int maximumExpressionLevels;
        /// <summary>
        /// Gets or sets the maximum expression evaluation level count reached
        /// by this engine context.
        /// </summary>
        public int MaximumExpressionLevels
        {
            get { CheckDisposed(); return maximumExpressionLevels; }
            set { CheckDisposed(); maximumExpressionLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the saved execution level count from the enclosing nested
        /// execution for this engine context.
        /// </summary>
        private int previousLevels;
        /// <summary>
        /// Gets or sets the saved execution level count from the enclosing
        /// nested execution for this engine context.
        /// </summary>
        public int PreviousLevels
        {
            get { CheckDisposed(); return previousLevels; }
            set { CheckDisposed(); previousLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current catch nesting level count for this engine
        /// context.
        /// </summary>
        private int catchLevels;
        /// <summary>
        /// Gets or sets the current catch nesting level count for this engine
        /// context.
        /// </summary>
        public int CatchLevels
        {
            get { CheckDisposed(); return catchLevels; }
            set { CheckDisposed(); catchLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current unknown-command handling level count for this
        /// engine context.
        /// </summary>
        private int unknownLevels;
        /// <summary>
        /// Gets or sets the current unknown-command handling level count for
        /// this engine context.
        /// </summary>
        public int UnknownLevels
        {
            get { CheckDisposed(); return unknownLevels; }
            set { CheckDisposed(); unknownLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current trace handling level count for this engine
        /// context.
        /// </summary>
        private int traceLevels;
        /// <summary>
        /// Gets or sets the current trace handling level count for this engine
        /// context.
        /// </summary>
        public int TraceLevels
        {
            get { CheckDisposed(); return traceLevels; }
            set { CheckDisposed(); traceLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current sub-command nesting level count for this engine
        /// context.
        /// </summary>
        private int subCommandLevels;
        /// <summary>
        /// Gets or sets the current sub-command nesting level count for this
        /// engine context.
        /// </summary>
        public int SubCommandLevels
        {
            get { CheckDisposed(); return subCommandLevels; }
            set { CheckDisposed(); subCommandLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current setting evaluation level count for this engine
        /// context.
        /// </summary>
        private int settingLevels;
        /// <summary>
        /// Gets or sets the current setting evaluation level count for this
        /// engine context.
        /// </summary>
        public int SettingLevels
        {
            get { CheckDisposed(); return settingLevels; }
            set { CheckDisposed(); settingLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current package loading level count for this engine
        /// context.
        /// </summary>
        private int packageLevels;
        /// <summary>
        /// Gets or sets the current package loading level count for this
        /// engine context.
        /// </summary>
        public int PackageLevels
        {
            get { CheckDisposed(); return packageLevels; }
            set { CheckDisposed(); packageLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current package index loading level count for this
        /// engine context.
        /// </summary>
        private int packageIndexLevels;
        /// <summary>
        /// Gets or sets the current package index loading level count for this
        /// engine context.
        /// </summary>
        public int PackageIndexLevels
        {
            get { CheckDisposed(); return packageIndexLevels; }
            set { CheckDisposed(); packageIndexLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the interpreter state flags for this engine context.
        /// </summary>
        private InterpreterStateFlags interpreterStateFlags;
        /// <summary>
        /// Gets or sets the interpreter state flags for this engine context.
        /// </summary>
        public InterpreterStateFlags InterpreterStateFlags
        {
            get { CheckDisposed(); return interpreterStateFlags; }
            set { CheckDisposed(); interpreterStateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the package flags for this engine context.
        /// </summary>
        private PackageFlags packageFlags;
        /// <summary>
        /// Gets or sets the package flags for this engine context.
        /// </summary>
        public PackageFlags PackageFlags
        {
            get { CheckDisposed(); return packageFlags; }
            set { CheckDisposed(); packageFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the package index flags for this engine context.
        /// </summary>
        private PackageIndexFlags packageIndexFlags;
        /// <summary>
        /// Gets or sets the package index flags for this engine context.
        /// </summary>
        public PackageIndexFlags PackageIndexFlags
        {
            get { CheckDisposed(); return packageIndexFlags; }
            set { CheckDisposed(); packageIndexFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the procedure flags for this engine context.
        /// </summary>
        private ProcedureFlags procedureFlags;
        /// <summary>
        /// Gets or sets the procedure flags for this engine context.
        /// </summary>
        public ProcedureFlags ProcedureFlags
        {
            get { CheckDisposed(); return procedureFlags; }
            set { CheckDisposed(); procedureFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        /// <summary>
        /// Stores the cache flags for this engine context.
        /// </summary>
        private CacheFlags cacheFlags;
        /// <summary>
        /// Gets or sets the cache flags for this engine context.
        /// </summary>
        public CacheFlags CacheFlags
        {
            get { CheckDisposed(); return cacheFlags; }
            set { CheckDisposed(); cacheFlags = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        /// <summary>
        /// Stores the cached argument used to reduce allocations for this
        /// engine context.
        /// </summary>
        private Argument cacheArgument;
        /// <summary>
        /// Gets or sets the cached argument used to reduce allocations for
        /// this engine context.
        /// </summary>
        public Argument CacheArgument
        {
            get { CheckDisposed(); return cacheArgument; }
            set { CheckDisposed(); cacheArgument = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// Stores the current watchpoint handling level count for this engine
        /// context.
        /// </summary>
        private int watchpointLevels;
        /// <summary>
        /// Gets or sets the current watchpoint handling level count for this
        /// engine context.
        /// </summary>
        public int WatchpointLevels
        {
            get { CheckDisposed(); return watchpointLevels; }
            set { CheckDisposed(); watchpointLevels = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        /// <summary>
        /// Stores the current notification handling level count for this
        /// engine context.
        /// </summary>
        private int notifyLevels;
        /// <summary>
        /// Gets or sets the current notification handling level count for this
        /// engine context.
        /// </summary>
        public int NotifyLevels
        {
            get { CheckDisposed(); return notifyLevels; }
            set { CheckDisposed(); notifyLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the notification types for this engine context.
        /// </summary>
        private NotifyType notifyTypes;
        /// <summary>
        /// Gets or sets the notification types for this engine context.
        /// </summary>
        public NotifyType NotifyTypes
        {
            get { CheckDisposed(); return notifyTypes; }
            set { CheckDisposed(); notifyTypes = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the notification flags for this engine context.
        /// </summary>
        private NotifyFlags notifyFlags;
        /// <summary>
        /// Gets or sets the notification flags for this engine context.
        /// </summary>
        public NotifyFlags NotifyFlags
        {
            get { CheckDisposed(); return notifyFlags; }
            set { CheckDisposed(); notifyFlags = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current security handling level count for this engine
        /// context.
        /// </summary>
        private int securityLevels;
        /// <summary>
        /// Gets or sets the current security handling level count for this
        /// engine context.
        /// </summary>
        public int SecurityLevels
        {
            get { CheckDisposed(); return securityLevels; }
            set { CheckDisposed(); securityLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current policy handling level count for this engine
        /// context.
        /// </summary>
        private int policyLevels;
        /// <summary>
        /// Gets or sets the current policy handling level count for this
        /// engine context.
        /// </summary>
        public int PolicyLevels
        {
            get { CheckDisposed(); return policyLevels; }
            set { CheckDisposed(); policyLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current test handling level count for this engine
        /// context.
        /// </summary>
        private int testLevels;
        /// <summary>
        /// Gets or sets the current test handling level count for this engine
        /// context.
        /// </summary>
        public int TestLevels
        {
            get { CheckDisposed(); return testLevels; }
            set { CheckDisposed(); testLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the initial policy decision for command execution in this
        /// engine context.
        /// </summary>
        private PolicyDecision commandInitialDecision;
        /// <summary>
        /// Gets or sets the initial policy decision for command execution in
        /// this engine context.
        /// </summary>
        public PolicyDecision CommandInitialDecision
        {
            get { CheckDisposed(); return commandInitialDecision; }
            set { CheckDisposed(); commandInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the initial policy decision for script evaluation in this
        /// engine context.
        /// </summary>
        private PolicyDecision scriptInitialDecision;
        /// <summary>
        /// Gets or sets the initial policy decision for script evaluation in
        /// this engine context.
        /// </summary>
        public PolicyDecision ScriptInitialDecision
        {
            get { CheckDisposed(); return scriptInitialDecision; }
            set { CheckDisposed(); scriptInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the initial policy decision for file evaluation in this
        /// engine context.
        /// </summary>
        private PolicyDecision fileInitialDecision;
        /// <summary>
        /// Gets or sets the initial policy decision for file evaluation in
        /// this engine context.
        /// </summary>
        public PolicyDecision FileInitialDecision
        {
            get { CheckDisposed(); return fileInitialDecision; }
            set { CheckDisposed(); fileInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the initial policy decision for stream evaluation in this
        /// engine context.
        /// </summary>
        private PolicyDecision streamInitialDecision;
        /// <summary>
        /// Gets or sets the initial policy decision for stream evaluation in
        /// this engine context.
        /// </summary>
        public PolicyDecision StreamInitialDecision
        {
            get { CheckDisposed(); return streamInitialDecision; }
            set { CheckDisposed(); streamInitialDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the final policy decision for command execution in this
        /// engine context.
        /// </summary>
        private PolicyDecision commandFinalDecision;
        /// <summary>
        /// Gets or sets the final policy decision for command execution in
        /// this engine context.
        /// </summary>
        public PolicyDecision CommandFinalDecision
        {
            get { CheckDisposed(); return commandFinalDecision; }
            set { CheckDisposed(); commandFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the final policy decision for script evaluation in this
        /// engine context.
        /// </summary>
        private PolicyDecision scriptFinalDecision;
        /// <summary>
        /// Gets or sets the final policy decision for script evaluation in
        /// this engine context.
        /// </summary>
        public PolicyDecision ScriptFinalDecision
        {
            get { CheckDisposed(); return scriptFinalDecision; }
            set { CheckDisposed(); scriptFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the final policy decision for file evaluation in this engine
        /// context.
        /// </summary>
        private PolicyDecision fileFinalDecision;
        /// <summary>
        /// Gets or sets the final policy decision for file evaluation in this
        /// engine context.
        /// </summary>
        public PolicyDecision FileFinalDecision
        {
            get { CheckDisposed(); return fileFinalDecision; }
            set { CheckDisposed(); fileFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the final policy decision for stream evaluation in this
        /// engine context.
        /// </summary>
        private PolicyDecision streamFinalDecision;
        /// <summary>
        /// Gets or sets the final policy decision for stream evaluation in
        /// this engine context.
        /// </summary>
        public PolicyDecision StreamFinalDecision
        {
            get { CheckDisposed(); return streamFinalDecision; }
            set { CheckDisposed(); streamFinalDecision = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the timeout, in milliseconds, used when checking whether the
        /// interpreter is ready, or null for none.
        /// </summary>
        private int? readyTimeout;
        /// <summary>
        /// Gets or sets the timeout, in milliseconds, used when checking
        /// whether the interpreter is ready, or null for none.
        /// </summary>
        public int? ReadyTimeout
        {
            get { CheckDisposed(); return readyTimeout; }
            set { CheckDisposed(); readyTimeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether script evaluation has been
        /// canceled for this engine context.
        /// </summary>
        private bool cancel;
        /// <summary>
        /// Gets or sets a value indicating whether script evaluation has been
        /// canceled for this engine context.
        /// </summary>
        public bool Cancel
        {
            get { CheckDisposed(); return cancel; }
            set { CheckDisposed(); cancel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the call stack should be unwound
        /// following cancellation for this engine context.
        /// </summary>
        private bool unwind;
        /// <summary>
        /// Gets or sets a value indicating whether the call stack should be
        /// unwound following cancellation for this engine context.
        /// </summary>
        public bool Unwind
        {
            get { CheckDisposed(); return unwind; }
            set { CheckDisposed(); unwind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether script evaluation has been halted
        /// for this engine context.
        /// </summary>
        private bool halt;
        /// <summary>
        /// Gets or sets a value indicating whether script evaluation has been
        /// halted for this engine context.
        /// </summary>
        public bool Halt
        {
            get { CheckDisposed(); return halt; }
            set { CheckDisposed(); halt = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the result associated with cancellation of script evaluation
        /// for this engine context.
        /// </summary>
        private Result cancelResult;
        /// <summary>
        /// Gets or sets the result associated with cancellation of script
        /// evaluation for this engine context.
        /// </summary>
        public Result CancelResult
        {
            get { CheckDisposed(); return cancelResult; }
            set { CheckDisposed(); cancelResult = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the result associated with halting of script evaluation for
        /// this engine context.
        /// </summary>
        private Result haltResult;
        /// <summary>
        /// Gets or sets the result associated with halting of script
        /// evaluation for this engine context.
        /// </summary>
        public Result HaltResult
        {
            get { CheckDisposed(); return haltResult; }
            set { CheckDisposed(); haltResult = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// Stores a value indicating whether the debugger is in the process of
        /// exiting for this engine context.
        /// </summary>
        private bool isDebuggerExiting;
        /// <summary>
        /// Gets or sets a value indicating whether the debugger is in the
        /// process of exiting for this engine context.
        /// </summary>
        public bool IsDebuggerExiting
        {
            get { CheckDisposed(); return isDebuggerExiting; }
            set { CheckDisposed(); isDebuggerExiting = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether a script stack overflow has been
        /// detected for this engine context.
        /// </summary>
        private bool stackOverflow;
        /// <summary>
        /// Gets or sets a value indicating whether a script stack overflow has
        /// been detected for this engine context.
        /// </summary>
        public bool StackOverflow
        {
            get { CheckDisposed(); return stackOverflow; }
            set { CheckDisposed(); stackOverflow = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// Stores the debugger associated with this engine context.
        /// </summary>
        private IDebugger debugger;
        /// <summary>
        /// Gets or sets the debugger associated with this engine context.
        /// </summary>
        public IDebugger Debugger
        {
            get { CheckDisposed(); return debugger; }
            set { CheckDisposed(); debugger = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if PREVIOUS_RESULT
        /// <summary>
        /// Stores the previous result tracked for this engine context.
        /// </summary>
        private Result previousResult;
        /// <summary>
        /// Gets or sets the previous result tracked for this engine context.
        /// </summary>
        public Result PreviousResult
        {
            get { CheckDisposed(); return previousResult; }
            set { CheckDisposed(); previousResult = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the last error result recorded for this engine context.
        /// </summary>
        private Result lastError;
        /// <summary>
        /// Gets or sets the last error result recorded for this engine
        /// context.
        /// </summary>
        public Result LastError
        {
            get { CheckDisposed(); return lastError; }
            set { CheckDisposed(); lastError = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the engine flags for this engine context.
        /// </summary>
        private EngineFlags engineFlags;
        /// <summary>
        /// Gets or sets the engine flags for this engine context.
        /// </summary>
        public EngineFlags EngineFlags
        {
            get { CheckDisposed(); return engineFlags; }
            set { CheckDisposed(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current parse state for this engine context.
        /// </summary>
        private IParseState parseState;
        /// <summary>
        /// Gets or sets the current parse state for this engine context.
        /// </summary>
        public IParseState ParseState
        {
            get { CheckDisposed(); return parseState; }
            set { CheckDisposed(); parseState = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current return code for this engine context.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// Gets or sets the current return code for this engine context.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get { CheckDisposed(); return returnCode; }
            set { CheckDisposed(); returnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script line number associated with the current error for
        /// this engine context.
        /// </summary>
        private int errorLine;
        /// <summary>
        /// Gets or sets the script line number associated with the current
        /// error for this engine context.
        /// </summary>
        public int ErrorLine
        {
            get { CheckDisposed(); return errorLine; }
            set { CheckDisposed(); errorLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the error code associated with the current error for this
        /// engine context.
        /// </summary>
        private string errorCode;
        /// <summary>
        /// Gets or sets the error code associated with the current error for
        /// this engine context.
        /// </summary>
        public string ErrorCode
        {
            get { CheckDisposed(); return errorCode; }
            set { CheckDisposed(); errorCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the error information associated with the current error for
        /// this engine context.
        /// </summary>
        private string errorInfo;
        /// <summary>
        /// Gets or sets the error information associated with the current
        /// error for this engine context.
        /// </summary>
        public string ErrorInfo
        {
            get { CheckDisposed(); return errorInfo; }
            set { CheckDisposed(); errorInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of error frames associated with the current error
        /// for this engine context.
        /// </summary>
        private int errorFrames;
        /// <summary>
        /// Gets or sets the number of error frames associated with the current
        /// error for this engine context.
        /// </summary>
        public int ErrorFrames
        {
            get { CheckDisposed(); return errorFrames; }
            set { CheckDisposed(); errorFrames = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the exception associated with the current error for this
        /// engine context.
        /// </summary>
        private Exception exception;
        /// <summary>
        /// Gets or sets the exception associated with the current error for
        /// this engine context.
        /// </summary>
        public Exception Exception
        {
            get { CheckDisposed(); return exception; }
            set { CheckDisposed(); exception = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current script location for this engine context.
        /// </summary>
        private IScriptLocation scriptLocation;
        /// <summary>
        /// Gets or sets the current script location for this engine context.
        /// </summary>
        public IScriptLocation ScriptLocation
        {
            get { CheckDisposed(); return scriptLocation; }
            set { CheckDisposed(); scriptLocation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the stack of script locations for this engine context.
        /// </summary>
        private ScriptLocationList scriptLocations;
        /// <summary>
        /// Gets or sets the stack of script locations for this engine context.
        /// </summary>
        public ScriptLocationList ScriptLocations
        {
            get { CheckDisposed(); return scriptLocations; }
            set { CheckDisposed(); scriptLocations = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SCRIPT_ARGUMENTS
        /// <summary>
        /// Stores the stack of script argument lists for this engine context.
        /// </summary>
        private ArgumentListStack scriptArguments;
        /// <summary>
        /// Gets or sets the stack of script argument lists for this engine
        /// context.
        /// </summary>
        public ArgumentListStack ScriptArguments
        {
            get { CheckDisposed(); return scriptArguments; }
            set { CheckDisposed(); scriptArguments = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the previous process identifier tracked for this engine
        /// context.
        /// </summary>
        private long previousProcessId;
        /// <summary>
        /// Gets or sets the previous process identifier tracked for this
        /// engine context.
        /// </summary>
        public long PreviousProcessId
        {
            get { CheckDisposed(); return previousProcessId; }
            set { CheckDisposed(); previousProcessId = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the active array searches for this engine context.
        /// </summary>
        private ArraySearchDictionary arraySearches;
        /// <summary>
        /// Gets or sets the active array searches for this engine context.
        /// </summary>
        public ArraySearchDictionary ArraySearches
        {
            get { CheckDisposed(); return arraySearches; }
            set { CheckDisposed(); arraySearches = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        /// <summary>
        /// Stores the history filter applied to engine-level history for this
        /// engine context.
        /// </summary>
        private IHistoryFilter historyEngineFilter;
        /// <summary>
        /// Gets or sets the history filter applied to engine-level history for
        /// this engine context.
        /// </summary>
        public IHistoryFilter HistoryEngineFilter
        {
            get { CheckDisposed(); return historyEngineFilter; }
            set { CheckDisposed(); historyEngineFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the command history for this engine context.
        /// </summary>
        private ClientDataList history;
        /// <summary>
        /// Gets or sets the command history for this engine context.
        /// </summary>
        public ClientDataList History
        {
            get { CheckDisposed(); return history; }
            set { CheckDisposed(); history = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the most recent complaint recorded for this engine context.
        /// </summary>
        private string complaint;
        /// <summary>
        /// Gets or sets the most recent complaint recorded for this engine
        /// context.
        /// </summary>
        public string Complaint
        {
            get { CheckDisposed(); return complaint; }
            set { CheckDisposed(); complaint = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests cancellation of script evaluation for this engine
        /// context, optionally unwinding the call stack and recording a result.
        /// </summary>
        /// <param name="result">
        /// The result to associate with the cancellation.  This parameter may be
        /// null.
        /// </param>
        /// <param name="unwind">
        /// Non-zero to also request that the call stack be unwound.
        /// </param>
        /// <param name="needResult">
        /// Non-zero to record <paramref name="result" /> as the cancellation
        /// result.
        /// </param>
        /// <returns>
        /// True if the cancellation was requested.
        /// </returns>
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

        /// <summary>
        /// This method begins an external execution, incrementing the execution
        /// level count and setting the external execution engine flag.
        /// </summary>
        /// <returns>
        /// The engine flags, including any stack check flags added for the
        /// external execution.
        /// </returns>
        public EngineFlags BeginExternalExecution()
        {
            CheckDisposed();

            levels++;

            engineFlags |= EngineFlags.ExternalExecution;
            return Engine.AddStackCheckFlags(ref engineFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends an external execution, restoring the stack check
        /// flags, clearing the external execution engine flag, and decrementing
        /// the execution level count.
        /// </summary>
        /// <param name="savedEngineFlags">
        /// The engine flags saved by the matching call to
        /// <see cref="BeginExternalExecution" />, used to restore the stack check
        /// flags.
        /// </param>
        /// <returns>
        /// The execution level count after decrementing.
        /// </returns>
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

        /// <summary>
        /// This method begins a nested execution, saving the previous execution
        /// level count and updating it to the current execution level count.
        /// </summary>
        /// <returns>
        /// The previous execution level count, to be restored by the matching
        /// call to <see cref="EndNestedExecution" />.
        /// </returns>
        public int BeginNestedExecution()
        {
            CheckDisposed();

            int savedPreviousLevels = previousLevels;
            previousLevels = levels;

            return savedPreviousLevels;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a nested execution, restoring the previous execution
        /// level count saved by the matching call to
        /// <see cref="BeginNestedExecution" />.
        /// </summary>
        /// <param name="savedPreviousLevels">
        /// The previous execution level count to restore.
        /// </param>
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
        /// <summary>
        /// Stores a value indicating whether this engine context has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this engine context has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this engine context has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(EngineContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this engine context.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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

                    trustedLevels = 0;

                    scriptLevels = 0;
                    maximumScriptLevels = 0;

                    scriptFileLevels = 0;
                    maximumScriptFileLevels = 0;

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
                    packageIndexLevels = 0;

                    interpreterStateFlags = InterpreterStateFlags.None;

                    packageFlags = PackageFlags.None;
                    packageIndexFlags = PackageIndexFlags.None;
                    procedureFlags = ProcedureFlags.None;

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                    cacheFlags = CacheFlags.None;
#endif

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

                    readyTimeout = null;

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

                    lastError = null;
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
        /// <summary>
        /// This method releases all resources held by this engine context and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this engine context, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~EngineContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
