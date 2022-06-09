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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("aa7d8954-f29a-48f4-8fe2-5a20bc61846d")]
    internal sealed class InteractiveContext :
            IInteractiveContext, IDisposable /* optional */
    {
        #region Public Constructors
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
            interactiveInput = null;
            previousInteractiveInput = null;
            interactiveMode = null;
            activeInteractiveLoops = 0;
            totalInteractiveLoops = 0;
            totalInteractiveInputs = 0;

#if SHELL
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

        #region IInteractiveContext Members
#if SHELL
        private Semaphore interactiveLoopSemaphore;
        public Semaphore InteractiveLoopSemaphore
        {
            get { CheckDisposed(); return interactiveLoopSemaphore; }
            set { CheckDisposed(); interactiveLoopSemaphore = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool interactive;
        public bool Interactive
        {
            get { CheckDisposed(); return interactive; }
            set { CheckDisposed(); interactive = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string interactiveInput;
        public string InteractiveInput
        {
            get { CheckDisposed(); return interactiveInput; }
            set { CheckDisposed(); interactiveInput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string previousInteractiveInput;
        public string PreviousInteractiveInput
        {
            get { CheckDisposed(); return previousInteractiveInput; }
            set { CheckDisposed(); previousInteractiveInput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string interactiveMode;
        public string InteractiveMode
        {
            get { CheckDisposed(); return interactiveMode; }
            set { CheckDisposed(); interactiveMode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int activeInteractiveLoops;
        public int ActiveInteractiveLoops
        {
            get { CheckDisposed(); return activeInteractiveLoops; }
            set { CheckDisposed(); activeInteractiveLoops = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int totalInteractiveLoops;
        public int TotalInteractiveLoops
        {
            get { CheckDisposed(); return totalInteractiveLoops; }
            set { CheckDisposed(); totalInteractiveLoops = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int totalInteractiveInputs;
        public int TotalInteractiveInputs
        {
            get { CheckDisposed(); return totalInteractiveInputs; }
            set { CheckDisposed(); totalInteractiveInputs = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        private IShellCallbackData shellCallbackData;
        public IShellCallbackData ShellCallbackData
        {
            get { CheckDisposed(); return shellCallbackData; }
            set { CheckDisposed(); shellCallbackData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IInteractiveLoopData interactiveLoopData;
        public IInteractiveLoopData InteractiveLoopData
        {
            get { CheckDisposed(); return interactiveLoopData; }
            set { CheckDisposed(); interactiveLoopData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IUpdateData updateData;
        public IUpdateData UpdateData
        {
            get { CheckDisposed(); return updateData; }
            set { CheckDisposed(); updateData = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private StringTransformCallback interactiveCommandCallback;
        public StringTransformCallback InteractiveCommandCallback
        {
            get { CheckDisposed(); return interactiveCommandCallback; }
            set { CheckDisposed(); interactiveCommandCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        private IHistoryData historyLoadData;
        public IHistoryData HistoryLoadData
        {
            get { CheckDisposed(); return historyLoadData; }
            set { CheckDisposed(); historyLoadData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryData historySaveData;
        public IHistoryData HistorySaveData
        {
            get { CheckDisposed(); return historySaveData; }
            set { CheckDisposed(); historySaveData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryFilter historyInfoFilter;
        public IHistoryFilter HistoryInfoFilter
        {
            get { CheckDisposed(); return historyInfoFilter; }
            set { CheckDisposed(); historyInfoFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryFilter historyLoadFilter;
        public IHistoryFilter HistoryLoadFilter
        {
            get { CheckDisposed(); return historyLoadFilter; }
            set { CheckDisposed(); historyLoadFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryFilter historySaveFilter;
        public IHistoryFilter HistorySaveFilter
        {
            get { CheckDisposed(); return historySaveFilter; }
            set { CheckDisposed(); historySaveFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string historyFileName;
        public string HistoryFileName
        {
            get { CheckDisposed(); return historyFileName; }
            set { CheckDisposed(); historyFileName = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(InteractiveContext));
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
                    interactiveInput = null;
                    previousInteractiveInput = null;
                    interactiveMode = null;
                    activeInteractiveLoops = 0;
                    totalInteractiveLoops = 0;
                    totalInteractiveInputs = 0;

#if SHELL
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~InteractiveContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
