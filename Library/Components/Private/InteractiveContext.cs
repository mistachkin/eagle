/*
 * InteractiveContext.cs --
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
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class holds the per-thread state associated with an Eagle
    /// interpreter's interactive (shell) loop, such as whether the
    /// interpreter is interactive, the pending interactive input, the
    /// interactive mode, loop and input counters, and the optional shell,
    /// history, and update related data.  It implements
    /// <see cref="IInteractiveContext" /> and is disposable.
    /// </summary>
    [ObjectId("aa7d8954-f29a-48f4-8fe2-5a20bc61846d")]
    internal sealed class InteractiveContext :
            IInteractiveContext, IDisposable /* optional */
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an interactive context for the specified interpreter and
        /// thread, initializing all interactive state to its default values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this interactive context.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread associated with this interactive
        /// context.
        /// </param>
        /// <param name="interactiveLoopSemaphore">
        /// The semaphore used to coordinate access to the interactive loop.
        /// This parameter may be null.
        /// </param>
        public InteractiveContext(
            Interpreter interpreter,
            long threadId
#if SHELL
            , Semaphore interactiveLoopSemaphore
#endif
            )
        {
            this.interpreter = interpreter;
            this.threadId = threadId;

            ///////////////////////////////////////////////////////////////////

#if SHELL
            this.interactiveLoopSemaphore = interactiveLoopSemaphore;
#endif

            ///////////////////////////////////////////////////////////////////

            interactive = false;
            interactiveScriptLevels = 0;
            interactiveInputEnabled = MaybeEnableType.False;
            interactiveInputBuffer = null;
            interactiveInput = null;
            previousInteractiveInput = null;
            interactiveMode = null;
            activeInteractiveLoops = 0;
            totalInteractiveLoops = 0;
            totalInteractiveInputs = 0;

#if SHELL
            savedShellArguments = null;
            shellArguments = null;
            shellCallbackData = null;
            interactiveLoopData = null;
            updateData = null;
#endif

            interactiveCommandCallback = null;

#if HISTORY
            historyLoadData = null;
            historySaveData = null;

            historyInfoFilter = null;
            historyLoadFilter = null;
            historySaveFilter = null;

            historyFileName = null;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this interactive context has been
        /// disposed.
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
        /// Gets a value indicating whether this interactive context is
        /// currently in the process of being disposed; this property always
        /// returns zero for this interactive context.
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
        /// Stores the interpreter that owns this interactive context.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that owns this interactive context.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        /// <summary>
        /// Stores the identifier of the thread associated with this interactive
        /// context.
        /// </summary>
        private long threadId;
        /// <summary>
        /// Gets the identifier of the thread associated with this interactive
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

        #region IInteractiveContext Members
#if SHELL
        /// <summary>
        /// Stores the semaphore used to coordinate access to the interactive
        /// loop.
        /// </summary>
        private Semaphore interactiveLoopSemaphore;
        /// <summary>
        /// Gets or sets the semaphore used to coordinate access to the
        /// interactive loop.
        /// </summary>
        public Semaphore InteractiveLoopSemaphore
        {
            get { CheckDisposed(); return interactiveLoopSemaphore; }
            set { CheckDisposed(); interactiveLoopSemaphore = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the interpreter is currently
        /// interactive.
        /// </summary>
        private bool interactive;
        /// <summary>
        /// Gets or sets a value indicating whether the interpreter is currently
        /// interactive.
        /// </summary>
        public bool Interactive
        {
            get { CheckDisposed(); return interactive; }
            set { CheckDisposed(); interactive = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of nested script levels currently active within
        /// the interactive loop.
        /// </summary>
        private int interactiveScriptLevels;
        /// <summary>
        /// Gets or sets the number of nested script levels currently active
        /// within the interactive loop.
        /// </summary>
        public int InteractiveScriptLevels
        {
            get { CheckDisposed(); return interactiveScriptLevels; }
            set { CheckDisposed(); interactiveScriptLevels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether interactive input is enabled.
        /// </summary>
        private MaybeEnableType interactiveInputEnabled;
        /// <summary>
        /// Gets or sets a value indicating whether interactive input is
        /// enabled.
        /// </summary>
        public MaybeEnableType InteractiveInputEnabled
        {
            get { CheckDisposed(); return interactiveInputEnabled; }
            set { CheckDisposed(); interactiveInputEnabled = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the buffer used to accumulate pending interactive input.
        /// </summary>
        private StringBuilder interactiveInputBuffer;
        /// <summary>
        /// Gets or sets the buffer used to accumulate pending interactive
        /// input.
        /// </summary>
        public StringBuilder InteractiveInputBuffer
        {
            get { CheckDisposed(); return interactiveInputBuffer; }
            set { CheckDisposed(); interactiveInputBuffer = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current interactive input.
        /// </summary>
        private string interactiveInput;
        /// <summary>
        /// Gets or sets the current interactive input.
        /// </summary>
        public string InteractiveInput
        {
            get { CheckDisposed(); return interactiveInput; }
            set { CheckDisposed(); interactiveInput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the previous interactive input.
        /// </summary>
        private string previousInteractiveInput;
        /// <summary>
        /// Gets or sets the previous interactive input.
        /// </summary>
        public string PreviousInteractiveInput
        {
            get { CheckDisposed(); return previousInteractiveInput; }
            set { CheckDisposed(); previousInteractiveInput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current interactive mode.
        /// </summary>
        private string interactiveMode;
        /// <summary>
        /// Gets or sets the current interactive mode.
        /// </summary>
        public string InteractiveMode
        {
            get { CheckDisposed(); return interactiveMode; }
            set { CheckDisposed(); interactiveMode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of interactive loops currently active.
        /// </summary>
        private int activeInteractiveLoops;
        /// <summary>
        /// Gets or sets the number of interactive loops currently active.
        /// </summary>
        public int ActiveInteractiveLoops
        {
            get { CheckDisposed(); return activeInteractiveLoops; }
            set { CheckDisposed(); activeInteractiveLoops = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the total number of interactive loops that have been entered.
        /// </summary>
        private int totalInteractiveLoops;
        /// <summary>
        /// Gets or sets the total number of interactive loops that have been
        /// entered.
        /// </summary>
        public int TotalInteractiveLoops
        {
            get { CheckDisposed(); return totalInteractiveLoops; }
            set { CheckDisposed(); totalInteractiveLoops = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the total number of interactive inputs that have been
        /// processed.
        /// </summary>
        private int totalInteractiveInputs;
        /// <summary>
        /// Gets or sets the total number of interactive inputs that have been
        /// processed.
        /// </summary>
        public int TotalInteractiveInputs
        {
            get { CheckDisposed(); return totalInteractiveInputs; }
            set { CheckDisposed(); totalInteractiveInputs = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// Stores the previously saved shell command-line arguments.
        /// </summary>
        private IList<string> savedShellArguments;
        /// <summary>
        /// Gets or sets the previously saved shell command-line arguments.
        /// </summary>
        public IList<string> SavedShellArguments
        {
            get { CheckDisposed(); return savedShellArguments; }
            set { CheckDisposed(); savedShellArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the current shell command-line arguments.
        /// </summary>
        private IList<string> shellArguments;
        /// <summary>
        /// Gets or sets the current shell command-line arguments.
        /// </summary>
        public IList<string> ShellArguments
        {
            get { CheckDisposed(); return shellArguments; }
            set { CheckDisposed(); shellArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback data used while processing shell arguments.
        /// </summary>
        private IShellCallbackData shellCallbackData;
        /// <summary>
        /// Gets or sets the callback data used while processing shell
        /// arguments.
        /// </summary>
        public IShellCallbackData ShellCallbackData
        {
            get { CheckDisposed(); return shellCallbackData; }
            set { CheckDisposed(); shellCallbackData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the data associated with the active interactive loop.
        /// </summary>
        private IInteractiveLoopData interactiveLoopData;
        /// <summary>
        /// Gets or sets the data associated with the active interactive loop.
        /// </summary>
        public IInteractiveLoopData InteractiveLoopData
        {
            get { CheckDisposed(); return interactiveLoopData; }
            set { CheckDisposed(); interactiveLoopData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the data used when checking for or applying updates.
        /// </summary>
        private IUpdateData updateData;
        /// <summary>
        /// Gets or sets the data used when checking for or applying updates.
        /// </summary>
        public IUpdateData UpdateData
        {
            get { CheckDisposed(); return updateData; }
            set { CheckDisposed(); updateData = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the callback used to transform interactive commands prior to
        /// their execution.
        /// </summary>
        private StringTransformCallback interactiveCommandCallback;
        /// <summary>
        /// Gets or sets the callback used to transform interactive commands
        /// prior to their execution.
        /// </summary>
        public StringTransformCallback InteractiveCommandCallback
        {
            get { CheckDisposed(); return interactiveCommandCallback; }
            set { CheckDisposed(); interactiveCommandCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        /// <summary>
        /// Stores the data used when loading interactive command history.
        /// </summary>
        private IHistoryData historyLoadData;
        /// <summary>
        /// Gets or sets the data used when loading interactive command history.
        /// </summary>
        public IHistoryData HistoryLoadData
        {
            get { CheckDisposed(); return historyLoadData; }
            set { CheckDisposed(); historyLoadData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the data used when saving interactive command history.
        /// </summary>
        private IHistoryData historySaveData;
        /// <summary>
        /// Gets or sets the data used when saving interactive command history.
        /// </summary>
        public IHistoryData HistorySaveData
        {
            get { CheckDisposed(); return historySaveData; }
            set { CheckDisposed(); historySaveData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the filter used when querying interactive command history.
        /// </summary>
        private IHistoryFilter historyInfoFilter;
        /// <summary>
        /// Gets or sets the filter used when querying interactive command
        /// history.
        /// </summary>
        public IHistoryFilter HistoryInfoFilter
        {
            get { CheckDisposed(); return historyInfoFilter; }
            set { CheckDisposed(); historyInfoFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the filter used when loading interactive command history.
        /// </summary>
        private IHistoryFilter historyLoadFilter;
        /// <summary>
        /// Gets or sets the filter used when loading interactive command
        /// history.
        /// </summary>
        public IHistoryFilter HistoryLoadFilter
        {
            get { CheckDisposed(); return historyLoadFilter; }
            set { CheckDisposed(); historyLoadFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the filter used when saving interactive command history.
        /// </summary>
        private IHistoryFilter historySaveFilter;
        /// <summary>
        /// Gets or sets the filter used when saving interactive command
        /// history.
        /// </summary>
        public IHistoryFilter HistorySaveFilter
        {
            get { CheckDisposed(); return historySaveFilter; }
            set { CheckDisposed(); historySaveFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the file used to load and save interactive
        /// command history.
        /// </summary>
        private string historyFileName;
        /// <summary>
        /// Gets or sets the name of the file used to load and save interactive
        /// command history.
        /// </summary>
        public string HistoryFileName
        {
            get { CheckDisposed(); return historyFileName; }
            set { CheckDisposed(); historyFileName = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this interactive context has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this interactive context has
        /// already been disposed.  It is called at the start of most members to
        /// guard against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this interactive context has been disposed and the
        /// engine is configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(InteractiveContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this interactive
        /// context.  It implements the standard dispose pattern.
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
                typeof(InteractiveContext).Name, TracePriority.CleanupDebug);

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

#if SHELL
                    interactiveLoopSemaphore = null; /* NOT OWNED */
#endif

                    ///////////////////////////////////////////////////////////

                    interactive = false;
                    interactiveScriptLevels = 0;
                    interactiveInputEnabled = MaybeEnableType.False;
                    interactiveInputBuffer = null;
                    interactiveInput = null;
                    previousInteractiveInput = null;
                    interactiveMode = null;
                    activeInteractiveLoops = 0;
                    totalInteractiveLoops = 0;
                    totalInteractiveInputs = 0;

#if SHELL
                    savedShellArguments = null;
                    shellArguments = null;
                    shellCallbackData = null;
                    interactiveLoopData = null;
                    updateData = null;
#endif

                    interactiveCommandCallback = null;

#if HISTORY
                    historyLoadData = null;
                    historySaveData = null;

                    historyInfoFilter = null;
                    historyLoadFilter = null;
                    historySaveFilter = null;

                    historyFileName = null;
#endif
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
        /// This method releases all resources held by this interactive context
        /// and suppresses finalization.
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
        /// Finalizes this interactive context, releasing any resources that
        /// were not released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~InteractiveContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
