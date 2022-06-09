/*
 * VariableContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("ab2f80d9-1157-4211-87ea-828e4be68626")]
    internal sealed class VariableContext : IVariableContext, IDisposable
    {
        #region Public Constructors
        public VariableContext(
            Interpreter interpreter,
            long threadId,
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame globalScopeFrame,
            ICallFrame currentFrame,
            ICallFrame procedureFrame,
            ICallFrame uplevelFrame,
            ITraceInfo traceInfo
            )
        {
            this.interpreter = interpreter;
            this.threadId = threadId;

            ///////////////////////////////////////////////////////////////////

            this.callStack = callStack;
            this.globalFrame = globalFrame;
            this.globalScopeFrame = globalScopeFrame;
            this.currentFrame = currentFrame;
            this.procedureFrame = procedureFrame;
            this.uplevelFrame = uplevelFrame;
            this.traceInfo = traceInfo;
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

        #region IVariableContext Members
        private CallStack callStack;
        public CallStack CallStack
        {
            get { CheckDisposed(); return callStack; }
            set { CheckDisposed(); callStack = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame globalFrame;
        public ICallFrame GlobalFrame
        {
            get { CheckDisposed(); return globalFrame; }
            set { CheckDisposed(); globalFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame globalScopeFrame;
        public ICallFrame GlobalScopeFrame
        {
            get { CheckDisposed(); return globalScopeFrame; }
            set { CheckDisposed(); globalScopeFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame currentFrame;
        public ICallFrame CurrentFrame
        {
            get { CheckDisposed(); return currentFrame; }
            set { CheckDisposed(); currentFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame procedureFrame;
        public ICallFrame ProcedureFrame
        {
            get { CheckDisposed(); return procedureFrame; }
            set { CheckDisposed(); procedureFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame uplevelFrame;
        public ICallFrame UplevelFrame
        {
            get { CheckDisposed(); return uplevelFrame; }
            set { CheckDisposed(); uplevelFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public ICallFrame CurrentGlobalFrame
        {
            get
            {
                CheckDisposed();

                if (globalScopeFrame != null)
                    return globalScopeFrame;

                return globalFrame;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ITraceInfo traceInfo;
        public ITraceInfo TraceInfo
        {
            get { CheckDisposed(); return traceInfo; }
            set { CheckDisposed(); traceInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Free(
            bool global
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Free: called, global = {0}, interpreter = {1}, disposed = {2}",
                global, FormatOps.InterpreterNoThrow(interpreter), disposed),
                typeof(VariableContext).Name, TracePriority.CleanupDebug);

            ///////////////////////////////////////////////////////////////////

            interpreter = null; /* NOT OWNED: Do not dispose. */
            threadId = 0;

            ///////////////////////////////////////////////////////////////////

            if (traceInfo != null)
                traceInfo = null;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SPECIAL CASE* We cannot dispose the current call stack
            //       unless we are [also] disposing of the interpreter itself;
            //       therefore, use the special Free method here instead of the
            //       Dispose method.  The Free method is guaranteed to do the
            //       right thing with regard to the global call frame (assuming
            //       the "global" parameter is correct).
            //
            if (callStack != null)
            {
                callStack.Free(global);
                callStack = null;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SPECIAL CASE* We cannot dispose the uplevel call frame
            //       unless we are [also] disposing of the interpreter itself.
            //
            if (uplevelFrame != null)
            {
                uplevelFrame.Free(global);
                uplevelFrame = null;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SPECIAL CASE* We cannot dispose the procedure call frame
            //       unless we are [also] disposing of the interpreter itself.
            //
            if (procedureFrame != null)
            {
                procedureFrame.Free(global);
                procedureFrame = null;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SPECIAL CASE* We cannot dispose the current call frame
            //       unless we are [also] disposing of the interpreter itself.
            //
            if (currentFrame != null)
            {
                currentFrame.Free(global);
                currentFrame = null;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SPECIAL CASE* We cannot dispose the uplevel call frame
            //       unless we are [also] disposing of the interpreter itself.
            //       If this is really a named scope call frame -AND- we are
            //       being disposed, it should have already been cleaned up by
            //       this point; therefore, this should be a no-op.
            //
            if (globalScopeFrame != null)
            {
                globalScopeFrame.Free(global);
                globalScopeFrame = null;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SPECIAL CASE* We cannot dispose the global call frame
            //       unless we are [also] disposing of the interpreter itself.
            //
            if (globalFrame != null)
            {
                globalFrame.Free(global);
                globalFrame = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(VariableContext));
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
                typeof(VariableContext).Name, TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    Free(true);
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
        ~VariableContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
