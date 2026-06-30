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
    /// <summary>
    /// This class holds the per-thread variable scoping state for an Eagle
    /// interpreter.  It tracks the call stack together with the special call
    /// frames (global, global scope, current, procedure, and uplevel) that
    /// define which variables are visible during evaluation, along with the
    /// owning interpreter, the thread identifier it belongs to, and the trace
    /// information used while resolving variables.  It implements
    /// <see cref="IVariableContext" /> and is disposable; disposing or freeing
    /// a context releases the call frames it manages.
    /// </summary>
    [ObjectId("ab2f80d9-1157-4211-87ea-828e4be68626")]
    internal sealed class VariableContext : IVariableContext, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a variable context from the owning interpreter, the
        /// thread it belongs to, the call stack, and the set of special call
        /// frames that establish variable scoping for that thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this variable context.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread this variable context belongs to.
        /// </param>
        /// <param name="callStack">
        /// The call stack associated with this variable context.
        /// </param>
        /// <param name="globalFrame">
        /// The global call frame for this variable context.
        /// </param>
        /// <param name="globalScopeFrame">
        /// The global scope call frame for this variable context, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="currentFrame">
        /// The current call frame for this variable context.
        /// </param>
        /// <param name="procedureFrame">
        /// The procedure call frame for this variable context, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="uplevelFrame">
        /// The uplevel call frame for this variable context, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="traceInfo">
        /// The trace information associated with this variable context, if any.
        /// This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Gets a value indicating whether this variable context has been
        /// disposed.  True if it has been disposed; otherwise, false.
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
        /// Gets a value indicating whether this variable context is in the
        /// process of being disposed.  True if it is being disposed; otherwise,
        /// false.
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
        /// The interpreter that owns this variable context.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that owns this variable context.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        /// <summary>
        /// The identifier of the thread this variable context belongs to.
        /// </summary>
        private long threadId;
        /// <summary>
        /// Gets the identifier of the thread this variable context belongs to.
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

        #region IVariableContext Members
        /// <summary>
        /// The call stack associated with this variable context.
        /// </summary>
        private CallStack callStack;
        /// <summary>
        /// Gets or sets the call stack associated with this variable context.
        /// </summary>
        public CallStack CallStack
        {
            get { CheckDisposed(); return callStack; }
            set { CheckDisposed(); callStack = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The global call frame for this variable context.
        /// </summary>
        private ICallFrame globalFrame;
        /// <summary>
        /// Gets or sets the global call frame for this variable context.
        /// </summary>
        public ICallFrame GlobalFrame
        {
            get { CheckDisposed(); return globalFrame; }
            set { CheckDisposed(); globalFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The global scope call frame for this variable context, if any.
        /// </summary>
        private ICallFrame globalScopeFrame;
        /// <summary>
        /// Gets or sets the global scope call frame for this variable context.
        /// </summary>
        public ICallFrame GlobalScopeFrame
        {
            get { CheckDisposed(); return globalScopeFrame; }
            set { CheckDisposed(); globalScopeFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The current call frame for this variable context.
        /// </summary>
        private ICallFrame currentFrame;
        /// <summary>
        /// Gets or sets the current call frame for this variable context.
        /// </summary>
        public ICallFrame CurrentFrame
        {
            get { CheckDisposed(); return currentFrame; }
            set { CheckDisposed(); currentFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The procedure call frame for this variable context, if any.
        /// </summary>
        private ICallFrame procedureFrame;
        /// <summary>
        /// Gets or sets the procedure call frame for this variable context.
        /// </summary>
        public ICallFrame ProcedureFrame
        {
            get { CheckDisposed(); return procedureFrame; }
            set { CheckDisposed(); procedureFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The uplevel call frame for this variable context, if any.
        /// </summary>
        private ICallFrame uplevelFrame;
        /// <summary>
        /// Gets or sets the uplevel call frame for this variable context.
        /// </summary>
        public ICallFrame UplevelFrame
        {
            get { CheckDisposed(); return uplevelFrame; }
            set { CheckDisposed(); uplevelFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the effective current global call frame for this variable
        /// context.  This is the global scope call frame when one is present;
        /// otherwise, it is the global call frame.
        /// </summary>
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

        /// <summary>
        /// The trace information associated with this variable context, if any.
        /// </summary>
        private ITraceInfo traceInfo;
        /// <summary>
        /// Gets or sets the trace information associated with this variable
        /// context.
        /// </summary>
        public ITraceInfo TraceInfo
        {
            get { CheckDisposed(); return traceInfo; }
            set { CheckDisposed(); traceInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the call frames and other resources managed by
        /// this variable context without disposing of the interpreter that owns
        /// it.  The contained call stack and call frames are freed (rather than
        /// disposed) so the global call frame is handled correctly based on the
        /// <paramref name="global" /> parameter.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the global call frame should also be freed; this should
        /// only be done when the interpreter itself is being disposed.
        /// </param>
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
        /// <summary>
        /// Non-zero if this variable context has been disposed and is no longer
        /// usable.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an <see cref="InterpreterDisposedException" /> if
        /// this variable context has been disposed and the owning interpreter
        /// is configured to throw on disposed access.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(VariableContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of the resources used by this variable context,
        /// freeing the managed call frames and call stack when disposing.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
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
        /// <summary>
        /// This method disposes of all resources used by this variable context.
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
        /// Finalizes this variable context, releasing any resources that were
        /// not explicitly disposed.
        /// </summary>
        ~VariableContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
