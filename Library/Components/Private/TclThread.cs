/*
 * TclThread.cs --
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
using System.Runtime.InteropServices;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Private.Tcl.Delegates;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

#if MONO || MONO_HACKS
using Eagle._Constants;
#endif

using Eagle._Containers.Private.Tcl;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private.Tcl;
using Eagle._Interfaces.Public;

using TclBridgePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Components.Private.Tcl.TclBridge>;

namespace Eagle._Components.Private.Tcl
{
    /// <summary>
    /// This class hosts a native Tcl interpreter on a dedicated managed thread
    /// and provides the inter-thread plumbing needed to drive it from other
    /// threads.  Because native Tcl interpreters have strict thread affinity,
    /// all operations against the wrapped interpreter (creation, evaluation,
    /// substitution, variable access, command bridging, cancellation, and
    /// deletion) are marshaled to the owning thread as queued events; on
    /// Windows these are delivered via native asynchronous procedure calls
    /// (APCs), while other platforms use a generic event-signaling mechanism.
    /// The thread runs an event loop that processes both the native Tcl event
    /// loop and the queued requests, signaling named "start", "done", "idle",
    /// and "queue" events for coordination.  It implements
    /// <see cref="ISynchronize" /> and is disposable; disposing the object
    /// gracefully shuts down (and, if requested, deletes) the wrapped Tcl
    /// interpreter and terminates the owning thread.
    /// </summary>
    [ObjectId("8fb7faec-3d8b-4e44-ad88-3e2b9627eca9")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclThread : ISynchronize, IDisposable
    {
        #region Private Constants
        //
        // NOTE: Event names for Tcl worker threads.
        //
        /// <summary>
        /// The name prefix used when building the named event signaled once the
        /// Tcl worker thread is ready to start receiving events.
        /// </summary>
        private const string tclThreadStartEventPrefix = "threadStart";

        /// <summary>
        /// The name prefix used when building the named event signaled once the
        /// Tcl worker thread is done (i.e. should exit its event loop).
        /// </summary>
        private const string tclThreadDoneEventPrefix = "threadDone";

        /// <summary>
        /// The name prefix used when building the named event signaled to make
        /// the Tcl worker thread process any pending idle events.
        /// </summary>
        private const string tclThreadIdleEventPrefix = "threadIdle";

        /// <summary>
        /// The name prefix used when building the named event signaled to make
        /// the Tcl worker thread process a queued inter-thread request.
        /// </summary>
        private const string tclThreadQueueEventPrefix = "threadQueue";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default value indicating whether failures encountered while
        /// adding bridged Tcl commands should be suppressed (i.e. not result in
        /// a complaint).  This is intentionally not read-only so that it may be
        /// adjusted at runtime.
        /// </summary>
        private static bool DefaultCommandNoComplain = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The garbage collector handle used to keep this object pinned (alive)
        /// in memory for as long as it is referenced by native code, until it is
        /// disposed.
        /// </summary>
        private GCHandle handle; /* TclThread */

        /// <summary>
        /// The Eagle interpreter that owns this object and on whose behalf the
        /// wrapped native Tcl interpreter is created and managed.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// The optional callback invoked to notify interested parties when a
        /// queued event has been processed.  This field may be null.
        /// </summary>
        private ResultCallback callback;

        /// <summary>
        /// The optional client data passed along to the notification callback.
        /// This field may be null.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// The timeout, in milliseconds, used when waiting for events and for
        /// queued event results.
        /// </summary>
        private int timeout;

        /// <summary>
        /// The name associated with this object and its managed thread.
        /// </summary>
        private string name;

        /// <summary>
        /// The flags controlling the creation and behavior of this object and
        /// its managed thread.
        /// </summary>
        private TclThreadFlags flags;

        /// <summary>
        /// The native (operating system) identifier of the managed thread that
        /// owns the wrapped Tcl interpreter.
        /// </summary>
        private long threadId;

        /// <summary>
        /// The managed thread that owns and runs the event loop for the wrapped
        /// Tcl interpreter.
        /// </summary>
        private Thread thread;

        /// <summary>
        /// The opaque native handle to the wrapped Tcl interpreter, or
        /// <see cref="IntPtr.Zero" /> if none currently exists.
        /// </summary>
        private IntPtr interp;

        /// <summary>
        /// Non-zero if a Tcl interpreter has been created at least once for this
        /// object.
        /// </summary>
        private bool initialized; /* NOTE: Has an interp ever been created? */

        /// <summary>
        /// Non-zero if the owning thread has been finalized (i.e. the wrapped
        /// Tcl interpreter can no longer service requests).
        /// </summary>
        private bool finalized;   /* NOTE: Has the thread been finalized? */

        /// <summary>
        /// The cached script cancellation delegate, captured at construction to
        /// avoid tricky locking issues when cancellation is later requested.
        /// This field may be null.
        /// </summary>
        private Tcl_CancelEval cancelEval; /* NOTE: Cached to avoid tricky locking issues. */

        /// <summary>
        /// The name of the named "start" event used for inter-thread
        /// communication.  This field is written once and then treated as
        /// read-only.
        /// </summary>
        private string startEventName;   /* NOTE: Write-once, then read-only. */

        /// <summary>
        /// The name of the named "done" event used for inter-thread
        /// communication.  This field is written once and then treated as
        /// read-only.
        /// </summary>
        private string doneEventName;    /* NOTE: Write-once, then read-only. */

        /// <summary>
        /// The name of the named "idle" event used for inter-thread
        /// communication.  This field is written once and then treated as
        /// read-only.
        /// </summary>
        private string idleEventName;    /* NOTE: Write-once, then read-only. */

        /// <summary>
        /// The name of the named "queue" event used for inter-thread
        /// communication.  This field is written once and then treated as
        /// read-only.
        /// </summary>
        private string queueEventName;   /* NOTE: Write-once, then read-only. */

        /// <summary>
        /// The wait handle for the named "start" event, signaled once the worker
        /// thread is ready to start receiving events.
        /// </summary>
        private EventWaitHandle startEvent;

        /// <summary>
        /// The wait handle for the named "done" event, signaled to request that
        /// the worker thread exit its event loop.
        /// </summary>
        private EventWaitHandle doneEvent;

        /// <summary>
        /// The wait handle for the named "idle" event, signaled to request that
        /// the worker thread process pending idle events.
        /// </summary>
        private EventWaitHandle idleEvent;

        /// <summary>
        /// The wait handle for the named "queue" event, signaled to request that
        /// the worker thread process a queued inter-thread request.
        /// </summary>
        private EventWaitHandle queueEvent;

        /// <summary>
        /// The opaque data (a garbage collector handle to the pending event)
        /// associated with the next queued request for the worker thread.
        /// Access to this field is synchronized.
        /// </summary>
        private IntPtr queueEventData; /* NOTE: Access is synchronized. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this class, allocating the garbage
        /// collector handle that keeps it alive, creating the named inter-thread
        /// communication events, and creating (and optionally starting) the
        /// managed thread that will own the wrapped Tcl interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The Eagle interpreter that owns this object.
        /// </param>
        /// <param name="callback">
        /// The optional callback invoked when a queued event has been processed.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data passed along to the notification callback.
        /// This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used when waiting for events and for
        /// queued event results.
        /// </param>
        /// <param name="name">
        /// The name to associate with this object and its managed thread.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the creation and behavior of this object and
        /// its managed thread.
        /// </param>
        private TclThread(
            Interpreter interpreter,
            ResultCallback callback,
            IClientData clientData,
            int timeout,
            string name,
            TclThreadFlags flags
            )
        {
            //
            // NOTE: Create an object to be used for synchronizing access
            //       to this object.
            //
            syncRoot = new object();

            //
            // NOTE: Lock this object in memory until we are disposed.
            //
            handle = GCHandle.Alloc(this, GCHandleType.Normal); /* throw */

            //
            // NOTE: Setup the information we need from the thread that
            //       we are going to create (below).
            //
            this.interpreter = interpreter;
            this.callback = callback;
            this.clientData = clientData;
            this.timeout = timeout;
            this.name = name;
            this.flags = flags;

            //
            // NOTE: Cache the script cancellation delegate for later use
            //       to prevent some locking issues.
            //
            cancelEval = GetCancelEvaluateDelegate(interpreter);

            //
            // NOTE: Setup the names of the named events to be used for
            //       inter-thread communication.
            //
            startEventName = FormatOps.EventName(interpreter,
                tclThreadStartEventPrefix, null,
                GlobalState.NextEventId(interpreter));

            doneEventName = FormatOps.EventName(interpreter,
                tclThreadDoneEventPrefix, null,
                GlobalState.NextEventId(interpreter));

            idleEventName = FormatOps.EventName(interpreter,
                tclThreadIdleEventPrefix, null,
                GlobalState.NextEventId(interpreter));

            queueEventName = FormatOps.EventName(interpreter,
                tclThreadQueueEventPrefix, null,
                GlobalState.NextEventId(interpreter));

            //
            // NOTE: Setup the named events to be used for inter-thread
            //       communication.
            //
            startEvent = ThreadOps.CreateEvent(startEventName);
            doneEvent = ThreadOps.CreateEvent(doneEventName);
            idleEvent = ThreadOps.CreateEvent(idleEventName);
            queueEvent = ThreadOps.CreateEvent(queueEventName);

            //
            // NOTE: Create the managed thread for this object.
            //
            thread = Engine.CreateThread(interpreter, ThreadStart, 0,
                FlagOps.HasFlags(flags, TclThreadFlags.UserInterface, true),
                FlagOps.HasFlags(flags, TclThreadFlags.IsBackground, true),
                FlagOps.HasFlags(flags, TclThreadFlags.UseActiveStack, true));

            if (thread != null)
            {
                //
                // NOTE: Give the thread a name.
                //
                thread.Name = this.name;

                //
                // NOTE: Caller requested that the thread be started now?
                //
                if (FlagOps.HasFlags(flags, TclThreadFlags.Start, true))
                    thread.Start();
            }
            else
            {
                throw new ScriptException("could not create Tcl thread");
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronizeBase Members
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// object.
        /// </summary>
        private object syncRoot;

        /// <summary>
        /// Gets the object used to synchronize access to the mutable state of
        /// this object.
        /// </summary>
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /// <summary>
        /// This method attempts to acquire an exclusive lock on this object,
        /// without waiting.
        /// </summary>
        /// <param name="locked">
        /// Upon success, this parameter will be set to non-zero if the lock was
        /// acquired; otherwise, it will be set to zero.
        /// </param>
        public void TryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire an exclusive lock on this object,
        /// waiting up to the configured wait-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon success, this parameter will be set to non-zero if the lock was
        /// acquired; otherwise, it will be set to zero.
        /// </param>
        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            PrivateTryLock(ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock),
                ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire an exclusive lock on this object,
        /// without waiting and without checking whether this object has been
        /// disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon success, this parameter will be set to non-zero if the lock was
        /// acquired; otherwise, it will be set to zero.
        /// </param>
        public void TryLockNoThrow(
            ref bool locked
            )
        {
            // CheckDisposed(); /* EXEMPT */

            PrivateTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire an exclusive lock on this object,
        /// waiting up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock to
        /// be acquired.
        /// </param>
        /// <param name="locked">
        /// Upon success, this parameter will be set to non-zero if the lock was
        /// acquired; otherwise, it will be set to zero.
        /// </param>
        public void TryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateTryLock(timeout, ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases an exclusive lock previously acquired on this
        /// object.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the lock is currently held; upon return, this
        /// parameter will be set to zero if the lock was released.
        /// </param>
        public void ExitLock(
            ref bool locked
            )
        {
            if (RuntimeOps.ShouldCheckDisposedOnExitLock(locked)) /* EXEMPT */
                CheckDisposed();

            PrivateExitLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private
        /// <summary>
        /// This method attempts to acquire an exclusive lock on this object,
        /// without waiting and without checking whether this object has been
        /// disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon success, this parameter will be set to non-zero if the lock was
        /// acquired; otherwise, it will be set to zero.
        /// </param>
        private void PrivateTryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire an exclusive lock on this object,
        /// waiting up to the specified timeout and without checking whether this
        /// object has been disposed.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock to
        /// be acquired.
        /// </param>
        /// <param name="locked">
        /// Upon success, this parameter will be set to non-zero if the lock was
        /// acquired; otherwise, it will be set to zero.
        /// </param>
        private void PrivateTryLock(
            int timeout,
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases an exclusive lock previously acquired on this
        /// object, without checking whether this object has been disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the lock is currently held; upon return, this
        /// parameter will be set to zero if the lock was released.
        /// </param>
        private void PrivateExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Data Accessor Members
        /// <summary>
        /// Gets the native (operating system) identifier of the managed thread
        /// that owns the wrapped Tcl interpreter.
        /// </summary>
        public long ThreadId
        {
            get { CheckDisposed(); lock (syncRoot) { return threadId; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the opaque native handle to the wrapped Tcl interpreter, or
        /// <see cref="IntPtr.Zero" /> if none currently exists.
        /// </summary>
        public IntPtr Interp
        {
            get { CheckDisposed(); lock (syncRoot) { return interp; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether a Tcl interpreter has been created at
        /// least once for this object.
        /// </summary>
        public bool Initialized
        {
            get { CheckDisposed(); lock (syncRoot) { return initialized; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the owning thread has been finalized
        /// (i.e. the wrapped Tcl interpreter can no longer service requests).
        /// </summary>
        public bool Finalized
        {
            get { CheckDisposed(); lock (syncRoot) { return finalized; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the timeout, in milliseconds, used when waiting for events and
        /// for queued event results.
        /// </summary>
        public int Timeout
        {
            get { CheckDisposed(); lock (syncRoot) { return timeout; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the name associated with this object and its managed thread.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); lock (syncRoot) { return name; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this object uses the generic (i.e.
        /// non-Windows-specific) event queueing mechanism.
        /// </summary>
        public bool IsGeneric
        {
            get { CheckDisposed(); lock (syncRoot) { return PrivateIsGeneric; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private
        /// <summary>
        /// Gets a value indicating whether this object uses the generic (i.e.
        /// non-Windows-specific) event queueing mechanism, without checking
        /// whether this object has been disposed.
        /// </summary>
        private bool PrivateIsGeneric
        {
            get { return FlagOps.HasFlags(flags, TclThreadFlags.Generic, true); }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method waits up to the specified timeout for the worker thread
        /// to signal that it is ready to start receiving events.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the "start"
        /// event to be signaled.
        /// </param>
        /// <returns>
        /// True if the "start" event was signaled within the timeout; otherwise,
        /// false.
        /// </returns>
        public bool WaitForStart(
            int timeout
            )
        {
            CheckDisposed();

            EventWaitHandle startEvent = null;

            try
            {
                startEvent = ThreadOps.OpenEvent(startEventName); /* throw */

                if (startEvent == null)
                    return false;

                return ThreadOps.WaitEvent(startEvent, timeout);
            }
            finally
            {
                ThreadOps.CloseEvent(ref startEvent);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits up to the specified timeout for the worker thread
        /// to signal that it is done (i.e. has exited its event loop).
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the "done"
        /// event to be signaled.
        /// </param>
        /// <returns>
        /// True if the "done" event was signaled within the timeout; otherwise,
        /// false.
        /// </returns>
        public bool WaitForDone(
            int timeout
            )
        {
            CheckDisposed();

            EventWaitHandle doneEvent = null;

            try
            {
                doneEvent = ThreadOps.OpenEvent(doneEventName); /* throw */

                if (doneEvent == null)
                    return false;

                return ThreadOps.WaitEvent(doneEvent, timeout);
            }
            finally
            {
                ThreadOps.CloseEvent(ref doneEvent);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits up to the specified timeout for the worker thread
        /// to signal that it has processed pending idle events.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the "idle"
        /// event to be signaled.
        /// </param>
        /// <returns>
        /// True if the "idle" event was signaled within the timeout; otherwise,
        /// false.
        /// </returns>
        public bool WaitForIdle(
            int timeout
            )
        {
            CheckDisposed();

            EventWaitHandle idleEvent = null;

            try
            {
                idleEvent = ThreadOps.OpenEvent(idleEventName); /* throw */

                if (idleEvent == null)
                    return false;

                return ThreadOps.WaitEvent(idleEvent, timeout);
            }
            finally
            {
                ThreadOps.CloseEvent(ref idleEvent);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits up to the specified timeout for the worker thread
        /// to acknowledge a queued inter-thread request.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the "queue"
        /// event to be signaled.
        /// </param>
        /// <returns>
        /// True if the "queue" event was signaled within the timeout; otherwise,
        /// false.
        /// </returns>
        public bool WaitForQueue(
            int timeout
            )
        {
            CheckDisposed();

            EventWaitHandle queueEvent = null;

            try
            {
                queueEvent = ThreadOps.OpenEvent(queueEventName); /* throw */

                if (queueEvent == null)
                    return false;

                return ThreadOps.WaitEvent(queueEvent, timeout);
            }
            finally
            {
                ThreadOps.CloseEvent(ref queueEvent);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this object, which is
        /// the string form of the wrapped Tcl interpreter handle.
        /// </summary>
        /// <returns>
        /// The string representation of the wrapped Tcl interpreter handle.
        /// </returns>
        public override string ToString()
        {
            // CheckDisposed(); /* EXEMPT: During disposal. */

            IntPtr interp;

            lock (syncRoot)
            {
                interp = this.interp;
            }

            return interp.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object is currently in the process of being disposed
        /// (used to prevent re-entrancy).
        /// </summary>
        private bool disposing;

        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an <see cref="ObjectDisposedException" /> if this
        /// object has been disposed and the interpreter is configured to throw
        /// when disposed objects are used.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TclThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of the resources used by this object, attempting
        /// to gracefully shut down (and delete) the wrapped Tcl interpreter,
        /// closing the inter-thread communication events, releasing the garbage
        /// collector handle, and clearing the remaining state.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            ) /* throw */
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(TclThread).Name,
                TracePriority.CleanupDebug);

            //
            // BUGFIX: These must be read prior to grabbing and holding
            //         the lock; otherwise, a deadlock could occur.
            //
            ITclApi tclApi = GetTclApi();
            TclBridgeDictionary tclBridges = GetTclBridges();

            //
            // NOTE: This was fundamentally broken.  The lock CANNOT be
            //       held while the Shutdown() method is being called;
            //       otherwise, the other thread cannot process the
            //       event (i.e. via the EventCallback method) because
            //       it cannot obtain the lock to fetch the private
            //       fields.
            //
            // NOTE: Attempt to shutdown the Tcl interpreter thread now
            //       (if it is still alive).
            //
            TclThreadFlags localFlags;

            lock (syncRoot)
            {
                localFlags = this.flags;
            }

            ReturnCode shutdownCode;
            Result shutdownError = null;

            shutdownCode = Shutdown(
                tclApi, tclBridges, localFlags | TclThreadFlags.DeleteUse,
                ref shutdownError);

            if (shutdownCode != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "Dispose: shutdownCode = {0}, shutdownError = {1}",
                    shutdownCode, FormatOps.WrapOrNull(shutdownError)),
                    typeof(TclThread).Name, TracePriority.ThreadError);
            }

            lock (syncRoot)
            {
                if (!disposed)
                {
                    if (!this.disposing)
                    {
                        //
                        // NOTE: We are now disposing this object (prevent
                        //       re-entrancy).
                        //
                        this.disposing = true;

                        //
                        // NOTE: This method should not normally throw;
                        //       however, if it does we do not want our
                        //       disposing flag to be stuck set to true.
                        //
                        try
                        {
                            //if (disposing)
                            //{
                            //    ////////////////////////////////////
                            //    // dispose managed resources here...
                            //    ////////////////////////////////////
                            //}

                            //////////////////////////////////////
                            // release unmanaged resources here...
                            //////////////////////////////////////

                            //
                            // NOTE: Dispose of the "start" inter-thread
                            //       communication event.
                            //
                            // BUGBUG: This managed object may already be
                            //         disposed?
                            //
                            ThreadOps.CloseEvent(ref startEvent);

                            //
                            // NOTE: Dispose of the "done" inter-thread
                            //       communication event.
                            //
                            // BUGBUG: This managed object may already be
                            //         disposed?
                            //
                            ThreadOps.CloseEvent(ref doneEvent);

                            //
                            // NOTE: Dispose of the "idle" inter-thread
                            //       communication event.
                            //
                            // BUGBUG: This managed object may already be
                            //         disposed?
                            //
                            ThreadOps.CloseEvent(ref idleEvent);

                            //
                            // NOTE: Dispose of the "queue" inter-thread
                            //       communication event.
                            //
                            // BUGBUG: This managed object may already be
                            //         disposed?
                            //
                            ThreadOps.CloseEvent(ref queueEvent);

                            //
                            // NOTE: If necessary, release the GCHandle that
                            //       is keeping this object alive.
                            //
                            // BUGBUG: This managed object may already be
                            //         disposed?
                            //
                            if (handle.IsAllocated)
                                handle.Free();

                            //
                            // NOTE: We do not own these objects; therefore,
                            //       we just null out the references to them
                            //       (in case we are the only thing keeping
                            //       them alive).
                            //
                            interpreter = null;
                            callback = null;
                            clientData = null;

                            //
                            // NOTE: Clear out our miscellaneous data fields.
                            //
                            timeout = 0;
                            name = null;
                            flags = TclThreadFlags.None;

                            //
                            // NOTE: Zero out our Tcl interpreter.  We do not
                            //       delete it because we cannot do that on an
                            //       arbitrary GC thread.
                            //
                            interp = IntPtr.Zero;

                            //
                            // NOTE: Clear the handle for the created thread.
                            //
                            thread = null;

                            //
                            // NOTE: Zero the Id of the created thread.
                            //
                            threadId = 0;

                            //
                            // NOTE: Clear our cached script cancellation
                            //       delegate.
                            //
                            cancelEval = null;

                            //
                            // NOTE: This object is now disposed.
                            //
                            disposed = true;
                        }
                        finally
                        {
                            //
                            // NOTE: We are no longer disposing this object.
                            //
                            this.disposing = false;
                        }
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method disposes of the resources used by this object and
        /// suppresses finalization.
        /// </summary>
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes an instance of this class, releasing any resources that
        /// were not explicitly disposed.
        /// </summary>
        ~TclThread() /* throw */
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        /// <summary>
        /// This method creates a new instance of this class, which is capable of
        /// creating a native Tcl interpreter on a new managed thread and
        /// processing requests pertaining to it.
        /// </summary>
        /// <param name="interpreter">
        /// The Eagle interpreter that will own the created object.
        /// </param>
        /// <param name="callback">
        /// The optional callback invoked when a queued event has been processed.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data passed along to the notification callback.
        /// This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used when waiting for events and for
        /// queued event results.
        /// </param>
        /// <param name="name">
        /// The name to associate with the created object and its managed thread.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the creation and behavior of the created object
        /// and its managed thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The newly created object, or null if it could not be created.
        /// </returns>
        public static TclThread Create(
            Interpreter interpreter,
            ResultCallback callback,
            IClientData clientData,
            int timeout,
            string name,
            TclThreadFlags flags,
            ref Result error
            )
        {
            //
            // NOTE: Create and return a TclThread object that is capable of
            //       creating a Tcl interpreter on a new thread and processing
            //       requests pertaining to it.
            //
            if (interpreter != null)
            {
                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                if (TclApi.CheckModule(tclApi, ref error))
                {
                    //
                    // NOTE: Create a TclThread object to handle the command
                    //       callbacks from Tcl.
                    //
                    ReturnCode code = ReturnCode.Ok;
                    TclThread result = null;

                    try
                    {
                        result = new TclThread(
                            interpreter, callback, clientData, timeout, name,
                            flags);

                        //
                        // NOTE: Success, return the newly created [and running]
                        //       thread.
                        //
                        return result;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                    finally
                    {
                        if ((code != ReturnCode.Ok) &&
                            (result != null))
                        {
                            //
                            // NOTE: Dispose and clear the partially created TclThread
                            //       object because the Tcl command creation failed.
                            //       This can throw an exception if the command token
                            //       is valid and we cannot manage to delete it; however,
                            //       since Tcl command creation is the very last step
                            //       above, this corner case should be rare.
                            //
                            result.Dispose(); /* throw */
                            result = null;
                        }
                    }
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Thread Procedure Helper Methods
        /// <summary>
        /// This method determines whether the native Tcl event notifier for the
        /// specified thread is usable (i.e. the thread has been initialized and
        /// has not been finalized).
        /// </summary>
        /// <param name="thread">
        /// The object whose notifier usability is being checked.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// True if the notifier is usable; otherwise, false.
        /// </returns>
        private static bool IsNotifierUsable(
            TclThread thread,
            ref Result error
            )
        {
            if (thread == null)
                return false;

            bool locked = false;

            try
            {
                thread.PrivateTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (!thread.initialized) /* FIELD */
                    {
                        error = "thread is not initialized";
                        return false;
                    }

                    if (thread.finalized) /* FIELD */
                    {
                        error = "thread is finalized";
                        return false;
                    }

                    return true;
                }
                else
                {
                    //
                    // NOTE: If the thread lock cannot be obtained,
                    //       we cannot check its initialized flag.
                    //
                    error = "unable to obtain thread lock";
                    return false;
                }
            }
            finally
            {
                thread.PrivateExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method may throw, must be executed from within a
        //       try/catch block.
        //
        /// <summary>
        /// This method processes any pending events in the native Tcl event loop
        /// for the specified thread, provided its notifier is usable.
        /// </summary>
        /// <param name="thread">
        /// The object whose native Tcl events are being processed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The Eagle interpreter associated with the wrapped Tcl interpreter.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used when processing native Tcl events.
        /// </param>
        /// <param name="debug">
        /// Non-zero to emit diagnostic trace output.
        /// </param>
        /// <param name="noTrace">
        /// Non-zero to suppress the trace output emitted when the notifier is not
        /// usable.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress complaints about errors encountered while
        /// processing native Tcl events.
        /// </param>
        /// <returns>
        /// True if the Tcl API object was valid (i.e. the interpreter was not
        /// disposed) and event processing may continue; otherwise, false.
        /// </returns>
        private static bool DoOneEvent(
            TclThread thread,
            Interpreter interpreter,
            int timeout,
            bool debug,
            bool noTrace,
            bool noComplain
            )
        {
            ITclApi tclApi = TclApi.GetTclApi(interpreter);

            if (tclApi == null)
                return false;

            Result usableError = null;

            if (!IsNotifierUsable(thread, ref usableError))
            {
                if (debug && !noTrace)
                {
                    long threadId = GlobalState.GetCurrentNativeThreadId();

                    TraceOps.DebugTrace(threadId, String.Format(
                        "cannot process native Tcl events, " +
                        "thread {0} notifier is not usable: {1}",
                        threadId, FormatOps.WrapOrNull(usableError)),
                        typeof(Tcl_DoOneEvent).Name,
                        TracePriority.EventError);
                }

                return true;
            }

            //
            // NOTE: Process all the pending events in the Tcl event loop;
            //       we simulate passing the real (not a locally cached
            //       copy) Tcl API object reference here so that it can be
            //       freed by the called method if necessary.
            //
            ReturnCode eventCode;
            Result eventError = null;

            eventCode = TclWrapper.DoOneEvent(
                interpreter, timeout, false, true, true, ref tclApi,
                ref eventError);

            TclApi.SetTclApi(interpreter, tclApi);

            //
            // NOTE: If there was some kind of error just report it and
            //       continue with the next event.
            //
            if (!noComplain && (eventCode != ReturnCode.Ok))
                DebugOps.Complain(interpreter, eventCode, eventError);

            //
            // NOTE: If we get to this point, the Tcl API object was valid
            //       upon entry into this method (i.e. and the interpreter
            //       itself was not disposed); therefore, return true.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the locally opened inter-thread communication
        /// event wait handles.  It is only called from the thread procedure.
        /// </summary>
        /// <param name="startEvent">
        /// The "start" event wait handle to close.  Upon return, this parameter
        /// will be set to null.
        /// </param>
        /// <param name="doneEvent">
        /// The "done" event wait handle to close.  Upon return, this parameter
        /// will be set to null.
        /// </param>
        /// <param name="idleEvent">
        /// The "idle" event wait handle to close.  Upon return, this parameter
        /// will be set to null.
        /// </param>
        /// <param name="queueEvent">
        /// The "queue" event wait handle to close.  Upon return, this parameter
        /// will be set to null.
        /// </param>
        private static void CloseEvents( /* NOTE: Only called from ThreadStart. */
            ref EventWaitHandle startEvent,
            ref EventWaitHandle doneEvent,
            ref EventWaitHandle idleEvent,
            ref EventWaitHandle queueEvent
            )
        {
            ThreadOps.CloseEvent(ref queueEvent);
            ThreadOps.CloseEvent(ref idleEvent);
            ThreadOps.CloseEvent(ref doneEvent);
            ThreadOps.CloseEvent(ref startEvent);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens the named inter-thread communication event wait
        /// handles for use by the thread procedure.  It is only called from the
        /// thread procedure.
        /// </summary>
        /// <param name="startEvent">
        /// Upon success, this parameter will be set to the opened "start" event
        /// wait handle.
        /// </param>
        /// <param name="doneEvent">
        /// Upon success, this parameter will be set to the opened "done" event
        /// wait handle.
        /// </param>
        /// <param name="idleEvent">
        /// Upon success, this parameter will be set to the opened "idle" event
        /// wait handle.
        /// </param>
        /// <param name="queueEvent">
        /// Upon success, this parameter will be set to the opened "queue" event
        /// wait handle.
        /// </param>
        /// <returns>
        /// True if all four event wait handles were opened successfully;
        /// otherwise, false.
        /// </returns>
        private bool OpenEvents( /* NOTE: Only called from ThreadStart. */
            ref EventWaitHandle startEvent,
            ref EventWaitHandle doneEvent,
            ref EventWaitHandle idleEvent,
            ref EventWaitHandle queueEvent
            )
        {
            long threadId;

            lock (syncRoot)
            {
                threadId = this.threadId;
            }

            startEvent = ThreadOps.OpenEvent(startEventName); /* throw */

            if (startEvent == null)
            {
                TraceOps.DebugTrace(threadId,
                    "invalid \"start\" wait handle object",
                    typeof(Tcl_ThreadStart).Name,
                    TracePriority.HandleError);

                return false;
            }

            doneEvent = ThreadOps.OpenEvent(doneEventName); /* throw */

            if (doneEvent == null)
            {
                TraceOps.DebugTrace(threadId,
                    "invalid \"done\" wait handle object",
                    typeof(Tcl_ThreadStart).Name,
                    TracePriority.HandleError);

                return false;
            }

            idleEvent = ThreadOps.OpenEvent(idleEventName); /* throw */

            if (idleEvent == null)
            {
                TraceOps.DebugTrace(threadId,
                    "invalid \"idle\" wait handle object",
                    typeof(Tcl_ThreadStart).Name,
                    TracePriority.HandleError);

                return false;
            }

            queueEvent = ThreadOps.OpenEvent(queueEventName); /* throw */

            if (queueEvent == null)
            {
                TraceOps.DebugTrace(threadId,
                    "invalid \"queue\" wait handle object",
                    typeof(Tcl_ThreadStart).Name,
                    TracePriority.HandleError);

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the Tcl API object associated with the Eagle
        /// interpreter that owns this object.
        /// </summary>
        /// <returns>
        /// The associated Tcl API object, or null if none is available.
        /// </returns>
        private ITclApi GetTclApi()
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            return TclApi.GetTclApi(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the collection of bridged Tcl commands associated
        /// with the wrapped Tcl interpreter.
        /// </summary>
        /// <returns>
        /// The collection of bridged Tcl commands, or null if none is available.
        /// </returns>
        private TclBridgeDictionary GetTclBridges()
        {
            Interpreter interpreter;
            IntPtr interp;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
                interp = this.interp;
            }

            return (interpreter != null) ?
                interpreter.GetTclBridges(interp, null, null) : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Thread Procedure
        /// <summary>
        /// This method is the procedure for the managed thread that owns the
        /// wrapped Tcl interpreter.  It opens the inter-thread communication
        /// events, signals that it is ready, and then runs the main event loop,
        /// dispatching queued requests and processing the native Tcl (and, on
        /// Windows, the Windows message) event loop until it is asked to exit.
        /// </summary>
        private void ThreadStart()
        {
            long threadId;
            Interpreter interpreter;
            int timeout;
            TclThreadFlags flags;

            lock (syncRoot)
            {
                //
                // BUGBUG: Setup the thread Id for this object.  Hopefully, this will
                //         not change during the lifetime of this thread; however, if
                //         it does we may still be able to periodically "refresh" it
                //         in the loop below.
                //
                this.threadId = GlobalState.GetCurrentNativeThreadId();

                //
                // NOTE: Cache the thread Id for this object in a local variable.  This
                //       is only used for tracing; therefore, its value is non-critical.
                //
                threadId = this.threadId;

                //
                // NOTE: Cache the interpreter for this object in a local variable.
                //
                interpreter = this.interpreter;

                //
                // NOTE: Cache the wait timeout value for this object in a local variable.
                //
                timeout = this.timeout;

                //
                // NOTE: Cache the various flags used within the main loop below.
                //
                flags = this.flags;
            }

            if (interpreter != null)
            {
                bool noYield = FlagOps.HasFlags(flags, TclThreadFlags.NoYield, true);
                bool debug = FlagOps.HasFlags(flags, TclThreadFlags.Debug, true);
                bool noComplain = FlagOps.HasFlags(flags, TclThreadFlags.NoComplain, true);

                EventWaitHandle startEvent = null;
                EventWaitHandle doneEvent = null;
                EventWaitHandle idleEvent = null;
                EventWaitHandle queueEvent = null;

                try
                {
                    if (OpenEvents(
                            ref startEvent, ref doneEvent, ref idleEvent, ref queueEvent))
                    {
                        //
                        // NOTE: This thread is now ready to start receiving events.  Signal
                        //       the start event if this has not been done already.
                        //
                        ThreadOps.SetEvent(startEvent);

                        //
                        // NOTE: Setup our array of wait handles.  The first one is always
                        //       the "we are done" event (i.e. exit the thread).  The next
                        //       one is the "process pending events" event.
                        //
                        // WARNING: Do not change the ordering of these events.  Also, this
                        //          ordering must match up exactly with the TclThreadEvent
                        //          enumeration.
                        //
                        EventWaitHandle[] events = new EventWaitHandle[] {
                            /* 0 */ doneEvent, /* 1 */ idleEvent, /* 2 */ queueEvent
                        };

                        //
                        // NOTE: Cache the (constant) length of the event array.
                        //
                        int eventLength = events.Length;

                        //
                        // NOTE: The following loop will (generally) keep going until
                        //       this index is zero (exit) or negative (invalid).
                        //
                        int index;

                        //
                        // HACK: For now, avoid ever using the new overload(s) of the
                        //       EventWaitHandle.WaitAny method; otherwise, Mono crashes.
                        //
                        while (true)
                        {
                            //
                            // NOTE: Attempt to wait for one of our events (or a timeout,
                            //       etc).
                            //
                            index = ThreadOps.WaitAnyEvent(events, timeout);

                            //
                            // NOTE: Check if the index has a known "invalid" value,
                            //       which would mean that the wait operation failed.
                            //
                            if (ThreadOps.WasAnyWaitFailed(index))
                            {
                                if (debug)
                                {
                                    TraceOps.DebugTrace(threadId, String.Format(
                                        "could not wait for any event, " +
                                        "failed wait result is {0}, exiting...",
                                        FormatOps.WaitResult(eventLength, index)),
                                        typeof(Tcl_ThreadStart).Name,
                                        TracePriority.ThreadError);
                                }

                                break;
                            }

                            //
                            // NOTE: Negative means something serious went wrong because
                            //       well-known "failure" values should have been caught
                            //       by the WasAnyWaitFailed check, above.  This means
                            //       that an unrecognized negative value was returned.
                            //       This should not happen unless there is a bug in the
                            //       .NET Framework (unlikely) or Mono (more likely).
                            //
                            if (index < 0)
                            {
                                if (debug)
                                {
                                    TraceOps.DebugTrace(threadId, String.Format(
                                        "could not wait for any event, " +
                                        "negative wait result is {0}, exiting...",
                                        FormatOps.WaitResult(eventLength, index)),
                                        typeof(Tcl_ThreadStart).Name,
                                        TracePriority.ThreadError);
                                }

                                break;
                            }

                            //
                            // NOTE: Zero means the "doneEvent" was signaled.
                            //
                            if (index == 0)
                            {
                                if (debug)
                                {
                                    TraceOps.DebugTrace(threadId,
                                        "done event was signaled synchronously, exiting...",
                                        typeof(Tcl_ThreadStart).Name,
                                        TracePriority.EventDebug);
                                }

                                break;
                            }

                            //
                            // NOTE: If debug mode, show which event handle was signaled,
                            //       if any (i.e. it could simply be a timeout).
                            //
                            if (debug)
                            {
                                TraceOps.DebugTrace(threadId, String.Format(
                                    "positive event wait result is {0}",
                                    FormatOps.WaitResult(eventLength, index)),
                                    typeof(Tcl_ThreadStart).Name,
                                    TracePriority.EventDebug);
                            }

                            //
                            // NOTE: Figure out which event was signaled, if any.  If none,
                            //       just leave it null.
                            //
                            bool signaled = ThreadOps.WasAnyEventSignaled(index);
                            EventWaitHandle @event = null;

                            //
                            // NOTE: Reset the event (most likely the "idle" event) for next
                            //       time, if necessary.
                            //
                            if (signaled)
                            {
                                @event = events[index];
                                ThreadOps.ResetEvent(@event);
                            }

                            //
                            // NOTE: Check to see if there is an event waiting for us to
                            //       process.
                            //
                            if (signaled && Object.ReferenceEquals(@event, queueEvent))
                            {
                                IntPtr data;

                                //
                                // NOTE: Get the event data pertaining this to this event in a
                                //       thread-safe manner.  At the same time, make sure we do
                                //       not pickup stale event data in the future by zeroing it
                                //       out now.
                                //
                                lock (syncRoot)
                                {
                                    data = queueEventData;
                                    queueEventData = IntPtr.Zero;
                                }

                                /* NO RESULT */
                                EventCallback(data);
                            }
                            else if (ThreadOps.WaitEvent(doneEvent, 0)) /* NOTE: Is signaled? */
                            {
                                //
                                // NOTE: The event to delete the interpreter was probably
                                //       delivered out-of-band via a native APC, exit now.
                                //
                                if (debug)
                                {
                                    TraceOps.DebugTrace(threadId,
                                        "done event was signaled asynchronously, exiting...",
                                        typeof(Tcl_ThreadStart).Name,
                                        TracePriority.EventDebug);
                                }

                                break;
                            }
                            else
                            {
                                //
                                // NOTE: This should be set to non-zero prior to the end of this
                                //       block to exit the enclosing loop.
                                //
                                bool done = false;

                                //
                                // NOTE: Process any pending events in the Tcl event loop now.
                                //
                                if (!DoOneEvent(
                                        this, interpreter, timeout, debug,
                                        ThreadOps.WasAnyEventTimeout(index), noComplain))
                                {
                                    done = true;
                                }

#if WINFORMS
                                ReturnCode eventCode;
                                Result eventError = null;

                                //
                                // NOTE: If necessary, process all Windows messages from the queue.
                                //
                                eventCode = WindowOps.ProcessEvents(interpreter, ref eventError);

                                //
                                // NOTE: If there was some kind of error just report it and
                                //       continue with the next event.
                                //
                                if (!noComplain && (eventCode != ReturnCode.Ok)) /* RARE */
                                    DebugOps.Complain(interpreter, eventCode, eventError);
#endif

                                //
                                // NOTE: If the Tcl API object was invalid and/or the interpreter
                                //       was disposed, exit the enclosing loop now.
                                //
                                if (done)
                                {
                                    if (debug)
                                    {
                                        TraceOps.DebugTrace(threadId,
                                            "unable to process native Tcl events, exiting...",
                                            typeof(Tcl_ThreadStart).Name,
                                            TracePriority.EventError);
                                    }

                                    break;
                                }
                            }

                            //
                            // NOTE: Finally, maybe attempt to yield to other running threads.
                            //
                            if (!noYield)
                            {
                                ReturnCode yieldCode;
                                Result yieldError = null;

                                yieldCode = HostOps.ThreadYield(ref yieldError);

                                //
                                // NOTE: If there was some kind of error just report it and
                                //       continue with the next event.
                                //
                                if (!noComplain && (yieldCode != ReturnCode.Ok)) /* RARE */
                                    DebugOps.Complain(interpreter, yieldCode, yieldError);
                            }
                        }

                        //
                        // NOTE: Show which event handle was finally signaled for debugging
                        //       purposes.
                        //
                        if (debug)
                        {
                            TraceOps.DebugTrace(threadId, String.Format(
                                "final event wait result is {0}",
                                FormatOps.WaitResult(eventLength, index)),
                                typeof(Tcl_ThreadStart).Name,
                                TracePriority.EventDebug);
                        }

                        //
                        // NOTE: Are we are being gracefully shutdown?  If the event index
                        //       is valid then we are; otherwise, something undefined may
                        //       have happened and we want to exit this thread as fast as
                        //       possible.
                        //
                        if (!ThreadOps.WasAnyWaitFailed(index))
                        {
                            //
                            // NOTE: Process any "final" events in the Tcl event loop
                            //       pertaining to this thread before we exit; we simulate
                            //       passing the real (not a locally cached copy) Tcl API
                            //       object reference here so that it can be freed by the
                            //       called method if necessary.  For the "done" event,
                            //       ignore failures of this method due to the Tcl thread
                            //       being finalized.
                            //
                            /* IGNORED */
                            DoOneEvent(
                                this, interpreter, timeout, debug, index == 0, noComplain);
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(threadId,
                        e, typeof(Tcl_ThreadStart).Name,
                        TracePriority.ThreadError);
                }
                finally
                {
                    CloseEvents(
                        ref startEvent, ref doneEvent, ref idleEvent, ref queueEvent);
                }
            }
            else
            {
                TraceOps.DebugTrace(threadId,
                    "invalid interpreter",
                    typeof(Tcl_ThreadStart).Name,
                    TracePriority.MarshalError);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Event Queueing Members
        /// <summary>
        /// This method determines whether an event of the specified type has
        /// thread affinity (i.e. must be processed on the thread that owns the
        /// wrapped Tcl interpreter).
        /// </summary>
        /// <param name="type">
        /// The type of event being checked.
        /// </param>
        /// <returns>
        /// True if events of the specified type have thread affinity; otherwise,
        /// false.
        /// </returns>
        private static bool HasThreadAffinity(
            EventType type
            )
        {
            //
            // NOTE: These are the only event types that we can successfully
            //       process on any thread (i.e. they have no thread affinity).
            //
            return (type != EventType.Cancel) && (type != EventType.Unwind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to shut down the worker thread and, if
        /// requested, delete the wrapped Tcl interpreter.  It queues an interp
        /// deletion event (when applicable), signals the "done" event so the
        /// worker thread exits, and optionally waits for (or aborts) the thread.
        /// </summary>
        /// <param name="tclApi">
        /// The Tcl API object associated with the wrapped Tcl interpreter.  This
        /// parameter may be null.
        /// </param>
        /// <param name="tclBridges">
        /// The collection of bridged Tcl commands to dispose when deleting the
        /// interpreter.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the shutdown behavior (for example, whether to
        /// delete the interpreter, wait for the thread to end, or abort it).
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode Shutdown(
            ITclApi tclApi,
            TclBridgeDictionary tclBridges,
            TclThreadFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            Interpreter interpreter;
            long threadId;
            Thread thread;
            IntPtr interp;

            lock (syncRoot)
            {
                //
                // NOTE: Cache the interpreter for this object in a local variable.
                //
                interpreter = this.interpreter;

                //
                // NOTE: Cache the managed thread for this object in a local variable.
                //
                thread = this.thread;

                //
                // NOTE: Cache the thread Id for this object in a local variable.
                //
                threadId = this.threadId;

                //
                // NOTE: Cache the Tcl interpreter for this object in a local variable.
                //
                interp = this.interp;
            }

            if (interpreter != null)
            {
                bool delete = FlagOps.HasFlags(flags, TclThreadFlags.Delete, true);
                bool errorOnDead = FlagOps.HasFlags(flags, TclThreadFlags.ErrorOnDead, true);
                bool waitForEnd = FlagOps.HasFlags(flags, TclThreadFlags.WaitForEnd, true);
                bool noAbort = FlagOps.HasFlags(flags, TclThreadFlags.NoAbort, true);

                if (tclApi != null)
                {
                    ReturnCode code = ReturnCode.Ok;

                    if (delete && (interp != IntPtr.Zero))
                    {
                        //
                        // NOTE: This must be asynchronous otherwise we can end up in a
                        //       deadlock with the primary thread if this object is being
                        //       disposed.
                        //
                        code = QueueEvent(EventType.Dispose, EventFlags.Immediate,
                            tclBridges, false, ref error);

                        //
                        // NOTE: If the event was queued successfully then we need to wait
                        //       a bit for the interpreter deletion to be processed.
                        //
                        if (code == ReturnCode.Ok)
                        {
                            EventWaitHandle doneEvent = null;

                            try
                            {
                                doneEvent = ThreadOps.OpenEvent(doneEventName); /* throw */

                                if (doneEvent != null)
                                {
                                    //
                                    // NOTE: Wait for the thread to signal that the Tcl interpreter
                                    //       has been deleted (and that Tcl_FinalizeThread has been
                                    //       called).
                                    //
                                    // TODO: Figure out the best thing to do here.
                                    //
                                    // HACK: For now, avoid ever using the new overload(s) of this
                                    //       method; otherwise, Mono crashes.
                                    //
                                    // TODO: Maybe use "ThreadOps.DefaultJoinTimeout" for the
                                    //       timeout here?
                                    //
                                    if (!ThreadOps.WaitEvent(doneEvent))
                                    {
                                        error = "timeout waiting for Tcl interpreter thread to exit";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "cannot open \"done\" event wait handle object after queued delete";
                                    code = ReturnCode.Error;
                                }
                            }
                            finally
                            {
                                ThreadOps.CloseEvent(ref doneEvent);
                            }
                        }
                    }

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: If necessary, terminate the thread that is holding on to
                        //       the Tcl interpreter.
                        //
                        if (ThreadOps.IsAlive(thread))
                        {
                            EventWaitHandle doneEvent = null;

                            try
                            {
                                //
                                // NOTE: If necessary, set the "done" event so that the Tcl
                                //       interpreter thread will exit (i.e. for the case that
                                //       we did not delete the Tcl interpreter because it was
                                //       not necessary or we were not requested to do so).
                                //
                                doneEvent = ThreadOps.OpenEvent(doneEventName); /* throw */

                                if (doneEvent != null)
                                {
                                    //
                                    // NOTE: Trigger interpreter thread exit.
                                    //
                                    ThreadOps.SetEvent(doneEvent);
                                }
                                else
                                {
                                    error = "cannot open \"done\" event wait handle object during dispose";
                                    code = ReturnCode.Error;
                                }
                            }
                            catch (Exception e)
                            {
                                error = e;
                                code = ReturnCode.Error;
                            }
                            finally
                            {
                                ThreadOps.CloseEvent(ref doneEvent);
                            }

                            //
                            // NOTE: Are we forcing the issue here (i.e. to make sure the
                            //       thread is exited cleanly or aborted)?
                            //
                            if ((code == ReturnCode.Ok) && waitForEnd)
                            {
                                //
                                // NOTE: Wait a bit for the thread to exit.
                                //
                                if (!thread.Join(ThreadOps.DefaultJoinTimeout) &&
                                    ThreadOps.IsAlive(thread) && !noAbort)
                                {
                                    //
                                    // NOTE: Finally, just abort it.
                                    //
                                    thread.Abort(); /* BUGBUG: Leaks? */

                                    //
                                    // NOTE: Complain about the fact that we aborted.
                                    //
                                    TraceOps.DebugTrace(threadId,
                                        "aborted Tcl interpreter thread",
                                        typeof(Tcl_ThreadStart).Name,
                                        TracePriority.ThreadError);
                                }

                                //
                                // NOTE: We should no longer need this thread handle.
                                //
                                thread = null;
                            }

                            return code;
                        }
                        else if (errorOnDead)
                        {
                            error = "invalid or dead Tcl interpreter thread";
                        }
                        else
                        {
                            //
                            // NOTE: The thread is already dead and we do not care.
                            //
                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    error = "invalid Tcl API object";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues an event of the specified type for processing by
        /// the worker thread (or, for events without thread affinity, processes
        /// it on the current thread).
        /// </summary>
        /// <param name="type">
        /// The type of event to queue.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the event is processed.
        /// </param>
        /// <param name="data">
        /// The optional data associated with the event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero to wait for the event to be processed and return its result;
        /// zero to queue the event and return immediately.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will be set to the result of the event
        /// (when processed synchronously) or to an error message upon failure.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode QueueEvent(
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result
            )
        {
            CheckDisposed();

            int errorLine = 0;

            return QueueEvent(type, flags, data, synchronous, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues an event of the specified type for processing by
        /// the worker thread, selecting the Windows-specific or generic event
        /// queueing mechanism as appropriate.
        /// </summary>
        /// <param name="type">
        /// The type of event to queue.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the event is processed.
        /// </param>
        /// <param name="data">
        /// The optional data associated with the event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero to wait for the event to be processed and return its result;
        /// zero to queue the event and return immediately.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will be set to the result of the event
        /// (when processed synchronously) or to an error message upon failure.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be set to the line number where an
        /// error occurred during script evaluation, if applicable.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode QueueEvent(
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result,
            ref int errorLine
            )
        {
            CheckDisposed();

#if WINDOWS
            if (!PrivateIsGeneric && PlatformOps.IsWindowsOperatingSystem())
                return QueueEventWindows(type, flags, data, synchronous, ref result, ref errorLine);
            else
#endif
                return QueueEventGeneric(type, flags, data, synchronous, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues an event of the specified type for processing by
        /// the worker thread using the generic (i.e. non-Windows-specific)
        /// event-signaling mechanism.  Events without thread affinity are
        /// processed directly on the current thread.
        /// </summary>
        /// <param name="type">
        /// The type of event to queue.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the event is processed.
        /// </param>
        /// <param name="data">
        /// The optional data associated with the event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero to wait for the event to be processed and return its result;
        /// zero to queue the event and return immediately.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will be set to the result of the event
        /// (when processed synchronously) or to an error message upon failure.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be set to the line number where an
        /// error occurred during script evaluation, if applicable.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private ReturnCode QueueEventGeneric(
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result,
            ref int errorLine
            )
        {
            Interpreter interpreter;
            int timeout;
            string name;

            lock (syncRoot)
            {
                //
                // NOTE: Cache the interpreter for this object in a local variable.
                //
                interpreter = this.interpreter;

                //
                // NOTE: Cache the wait timeout value for this object in a local variable.
                //
                timeout = this.timeout;

                //
                // NOTE: Cache the name for this object in a local variable.
                //
                name = this.name;
            }

            if (interpreter != null)
            {
                EventWaitHandle queueEvent = null;

                try
                {
                    queueEvent = ThreadOps.OpenEvent(queueEventName); /* throw */

                    if (queueEvent != null)
                    {
                        bool queued = false;
                        bool useCurrentThread = !HasThreadAffinity(type);
                        EventFlags extraEventFlags = EventFlags.None;

                        if (!useCurrentThread && !synchronous)
                            extraEventFlags |= EventFlags.FireAndForget;

                        IEvent @event = null;

                        try
                        {
                            Result error = null;

                            @event = Event.Create(
                                new object(), null, type,
                                flags | extraEventFlags |
                                    (useCurrentThread ?
                                        EventFlags.Direct | EventFlags.SameThread :
                                        EventFlags.InterThread) |
                                    (synchronous ?
                                        EventFlags.Synchronous : EventFlags.Asynchronous) |
                                        EventFlags.External,
                                EventPriority.TclThread, interpreter, FormatOps.Id(
                                name, null, interpreter.NextId()), TimeOps.GetUtcNow(),
                                null, null, new ClientData(data), ref error);

                            if (@event == null)
                            {
                                result = error;
                                return ReturnCode.Error;
                            }

                            GCHandle handle = GCHandle.Alloc(@event, GCHandleType.Normal); /* throw */

                            try
                            {
                                if (useCurrentThread)
                                {
                                    //
                                    // NOTE: Simply execute the callback directly since it needs to be
                                    //       done on this thread.
                                    //
                                    /* NO RESULT */
                                    EventCallback(GCHandle.ToIntPtr(handle));

                                    //
                                    // NOTE: Technically, we did not queue the event to the other thread;
                                    //       however, this variable is only used to detect if this method
                                    //       needs to free the event data.  In this case, the event data
                                    //       was freed by the called method.
                                    //
                                    queued = true;
                                }
                                else
                                {
                                    //
                                    // NOTE: Set the event data pertaining this to this event in a
                                    //       thread-safe manner.
                                    //
                                    lock (syncRoot)
                                    {
                                        queueEventData = GCHandle.ToIntPtr(handle);
                                    }

                                    //
                                    // NOTE: Signal the worker thread that an event is ready.  The
                                    //       worker thread will use the event data we set into the
                                    //       instance variable (above).
                                    //
                                    if (ThreadOps.SetEvent(queueEvent))
                                        queued = true;
                                }

                                //
                                // NOTE: Attempt to signal the queued event.
                                //
                                if (queued)
                                {
                                    ReturnCode code = ReturnCode.Ok;

                                    //
                                    // NOTE: If we queued the event to the current thread, always
                                    //       wait.  This allows the APC to actually be executed.
                                    //
                                    if (useCurrentThread || synchronous)
                                    {
                                        if (!@event.GetResult(
                                                timeout, true, ref code, ref result,
                                                ref errorLine, ref result))
                                        {
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = String.Empty;
                                    }

                                    return code;
                                }
                                else
                                {
                                    result = String.Format(
                                        "cannot queue {0} {1} event to thread",
                                        synchronous ? "synchronous" : "asynchronous",
                                        type);
                                }
                            }
                            finally
                            {
                                //
                                // NOTE: If we did not manage to queue the event to the other
                                //       thread then we own this handle and must free it.
                                //
                                if (!queued &&
                                    handle.IsAllocated)
                                {
                                    handle.Free();
                                }
                            }
                        }
                        finally
                        {
                            if (@event != null)
                            {
                                if (useCurrentThread || synchronous)
                                {
                                    /* IGNORED */
                                    Event.Dispose(@event);
                                }

                                @event = null;
                            }
                        }
                    }
                    else
                    {
                        result = "cannot open \"queue\" event wait handle object";
                    }
                }
                catch (Exception e)
                {
                    result = e;
                }
                finally
                {
                    ThreadOps.CloseEvent(ref queueEvent);
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINDOWS
        /// <summary>
        /// This method queues an event of the specified type for processing by
        /// the worker thread on Windows, using a native asynchronous procedure
        /// call (APC).  Events without thread affinity are queued to (and
        /// processed on) the current thread.
        /// </summary>
        /// <param name="type">
        /// The type of event to queue.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the event is processed.
        /// </param>
        /// <param name="data">
        /// The optional data associated with the event.  This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// Non-zero to wait for the event to be processed and return its result;
        /// zero to queue the event and return immediately.
        /// </param>
        /// <param name="result">
        /// Upon return, this parameter will be set to the result of the event
        /// (when processed synchronously) or to an error message upon failure.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this parameter will be set to the line number where an
        /// error occurred during script evaluation, if applicable.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private ReturnCode QueueEventWindows(
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result,
            ref int errorLine
            )
        {
            bool useCurrentThread = !HasThreadAffinity(type);
            Interpreter interpreter;
            int timeout;
            string name;
            long threadId;

            lock (syncRoot)
            {
                //
                // NOTE: Cache the interpreter for this object in a local variable.
                //
                interpreter = this.interpreter;

                //
                // NOTE: Cache the wait timeout value for this object in a local variable.
                //
                timeout = this.timeout;

                //
                // NOTE: Cache the name for this object in a local variable.
                //
                name = this.name;

                //
                // NOTE: Cache the thread Id for this object in a local variable.
                //       If we should process the event on the current thread,
                //       do so.
                //
                if (useCurrentThread)
                    threadId = GlobalState.GetCurrentNativeThreadId();
                else
                    threadId = this.threadId;
            }

            if (interpreter != null)
            {
                IntPtr thread = IntPtr.Zero;

                try
                {
                    Result error = null; /* REUSED */

                    thread = NativeOps.OpenThread(
                        NativeOps.UnsafeNativeMethods.THREAD_SET_CONTEXT, false,
                        ConversionOps.ToUInt(threadId), ref error); /* throw */

                    if (NativeOps.IsValidHandle(thread))
                    {
                        bool queued = false;
                        EventFlags extraEventFlags = EventFlags.None;

                        if (!useCurrentThread && !synchronous)
                            extraEventFlags |= EventFlags.FireAndForget;

                        IEvent @event = null;

                        try
                        {
                            error = null;

                            @event = Event.Create(
                                new object(), new ApcCallback(EventCallback), type,
                                flags | extraEventFlags | EventFlags.Queued |
                                    (useCurrentThread ?
                                        EventFlags.SameThread : EventFlags.InterThread) |
                                    (synchronous ?
                                        EventFlags.Synchronous : EventFlags.Asynchronous) |
                                    EventFlags.External,
                                EventPriority.TclThread, interpreter, FormatOps.Id(
                                name, null, interpreter.NextId()), TimeOps.GetUtcNow(),
                                null, null, new ClientData(data), ref error);

                            if (@event == null)
                            {
                                result = error;
                                return ReturnCode.Error;
                            }

                            GCHandle handle = GCHandle.Alloc(@event, GCHandleType.Normal); /* throw */

                            try
                            {
                                error = null;

                                if (NativeOps.QueueUserApc(
                                        @event.Delegate as ApcCallback, thread, GCHandle.ToIntPtr(handle),
                                        ref error)) /* throw */
                                {
                                    queued = true;

                                    ReturnCode code = ReturnCode.Ok;

                                    //
                                    // NOTE: If we queued the event to the current thread, always
                                    //       wait.  This allows the APC to actually be executed.
                                    //
                                    if (useCurrentThread || synchronous)
                                    {
                                        if (!@event.GetResult(
                                                timeout, true, ref code, ref result,
                                                ref errorLine, ref result))
                                        {
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = String.Empty;
                                    }

                                    return code;
                                }
                                else
                                {
                                    result = String.Format(
                                        "cannot queue {0} {1} event to thread, QueueUserApc({2}) failed: {3}",
                                        synchronous ? "synchronous" : "asynchronous",
                                        type, threadId, FormatOps.WrapOrNull(error));
                                }
                            }
                            finally
                            {
                                //
                                // NOTE: If we did not manage to queue the event to the other
                                //       thread then we own this handle and must free it.
                                //
                                if (!queued &&
                                    handle.IsAllocated)
                                {
                                    handle.Free();
                                }
                            }
                        }
                        finally
                        {
                            if (@event != null)
                            {
                                if (useCurrentThread || synchronous)
                                {
                                    /* IGNORED */
                                    Event.Dispose(@event);
                                }

                                @event = null;
                            }
                        }
                    }
                    else if (error != null)
                    {
                        result = error;
                    }
                    else
                    {
                        result = String.Format(
                            "could not open native thread {0}",
                            threadId);
                    }
                }
                catch (Exception e)
                {
                    result = e;
                }
                finally
                {
                    //
                    // NOTE: If we managed to open the thread, close the handle
                    //       to it now.
                    //
                    if (NativeOps.IsValidHandle(thread))
                    {
                        try
                        {
                            NativeOps.UnsafeNativeMethods.CloseHandle(
                                thread); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(TclThread).Name,
                                TracePriority.ThreadError);
                        }

                        thread = IntPtr.Zero;
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Script Cancellation Helper Members
        /// <summary>
        /// This method gets the native Tcl script cancellation delegate
        /// associated with the specified Eagle interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The Eagle interpreter whose Tcl API object provides the cancellation
        /// delegate.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The script cancellation delegate, or null if none is available.
        /// </returns>
        private static Tcl_CancelEval GetCancelEvaluateDelegate(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                ITclApi tclApi = TclApi.GetTclApi(interpreter);

                if (tclApi != null)
                {
                    lock (tclApi.SyncRoot)
                    {
                        return tclApi.CancelEval;
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests cancellation of the script currently being
        /// evaluated by the specified Tcl interpreter, using the supplied
        /// cancellation delegate, without acquiring any locks.  It may be called
        /// from any thread (per TIP #285).
        /// </summary>
        /// <param name="cancelEval">
        /// The native Tcl script cancellation delegate to invoke.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interp">
        /// The opaque native handle to the Tcl interpreter whose script
        /// evaluation should be cancelled.
        /// </param>
        /// <param name="unwind">
        /// Non-zero to fully unwind the script in progress; zero to cancel it
        /// without unwinding.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be set to an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode CancelEvaluateNoLock(
            Tcl_CancelEval cancelEval, /* in */
            IntPtr interp,             /* in */
            bool unwind,               /* in */
            ref Result error           /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (cancelEval != null)
            {
                //
                // NOTE: Do not use tclApi.CheckInterp here because this function
                //       is allowed to be called from any thread (per TIP #285).
                //
                if (interp != IntPtr.Zero)
                {
                    Tcl_EvalFlags flags = Tcl_EvalFlags.TCL_EVAL_NONE;

                    if (unwind)
                        flags |= Tcl_EvalFlags.TCL_CANCEL_UNWIND;

                    code = cancelEval(interp, IntPtr.Zero, IntPtr.Zero, flags);

                    if (code != ReturnCode.Ok)
                        error = "attempt to cancel eval failed";
                }
                else
                {
                    error = "invalid Tcl interpreter";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "Tcl script cancellation is not available";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Event Callbacks
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // **** WARNING *****  BEGIN CODE DIRECTLY CALLED BY THE NATIVE WIN32 API  ***** WARNING **** /
        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the callback invoked (directly by the native Win32 API
        /// via an asynchronous procedure call, or directly by the worker thread)
        /// to process a single queued event.  It rehydrates the event from the
        /// supplied garbage collector handle, dispatches it according to its
        /// type (interpreter creation/deletion, evaluation, substitution,
        /// variable access, command bridging, cancellation, and so on), records
        /// the result, and invokes the notification callback when applicable.
        /// </summary>
        /// <param name="data">
        /// The opaque pointer to the garbage collector handle referencing the
        /// event to process.
        /// </param>
        private void EventCallback(IntPtr data)
        {
            Interpreter interpreter;
            int timeout;
            ResultCallback callback;
            IClientData clientData;
            string name;
            long threadId;
            IntPtr interp;
            bool finalized;
            Tcl_CancelEval cancelEval;

            lock (syncRoot)
            {
                //
                // NOTE: Cache the interpreter for this object in a local variable.
                //
                interpreter = this.interpreter;

                //
                // NOTE: Cache the wait timeout value for this object in a local variable.
                //
                timeout = this.timeout;

                //
                // NOTE: Cache the notification callback for this object in a local
                //       variable.
                //
                callback = this.callback;

                //
                // NOTE: Cache the client data for this object in a local variable.
                //
                clientData = this.clientData;

                //
                // NOTE: Cache the name for this object in a local variable.
                //
                name = this.name;

                //
                // NOTE: Cache the thread Id for this object in a local variable.
                //
                threadId = this.threadId;

                //
                // NOTE: Cache the Tcl interpreter for this object in a local variable.
                //
                interp = this.interp;

                //
                // NOTE: Cache the finalized flag for this object in a local variable.
                //
                finalized = this.finalized;

                //
                // NOTE: Cache the Tcl script cancellation delegate for this object in
                //       a local variable.
                //
                cancelEval = this.cancelEval;
            }

            if (!finalized)
            {
                try
                {
                    //
                    // NOTE: Grab the Tcl API object associated with this instance.
                    //
                    ITclApi tclApi = GetTclApi();

                    //
                    // NOTE: Rehydrate the handle from the data that Windows just
                    //       passed us.
                    //
                    GCHandle handle = GCHandle.FromIntPtr(data); /* throw */

                    try
                    {
                        //
                        // NOTE: Make sure the handle has a valid target.
                        //
                        if (handle.IsAllocated && (handle.Target != null))
                        {
                            //
                            // NOTE: Attempt to cast the handle to an IEvent object; if this
                            //       fails, we cannot continue to handle this call.
                            //
                            IEvent @event = null;

                            try
                            {
                                @event = handle.Target as IEvent;

                                if (@event != null)
                                {
                                    //
                                    // NOTE: Grab the behavioral flags for this event.
                                    //
                                    EventFlags eventFlags = @event.Flags;

                                    //
                                    // NOTE: Check for and process the debug flag for this event.
                                    //
                                    bool eventDebug = FlagOps.HasFlags(
                                        eventFlags, EventFlags.Debug, true);

                                    bool eventTiming = FlagOps.HasFlags(
                                        eventFlags, EventFlags.Timing, true);

                                    bool noCallback = FlagOps.HasFlags(
                                        eventFlags, EventFlags.NoCallback, true);

                                    bool noNotify = FlagOps.HasFlags(
                                        eventFlags, EventFlags.NoNotify, true);

                                    bool noComplain = FlagOps.HasFlags(
                                        eventFlags, EventFlags.NoComplain, true);

                                    if (eventDebug)
                                    {
                                        TraceOps.DebugTrace(threadId, String.Format(
                                            "received event {0} at {1} on thread {2}: {3}",
                                            FormatOps.DisplayName(EntityOps.GetNameNoThrow(@event)),
                                            TimeOps.GetUtcNow(), GlobalState.GetCurrentNativeThreadId(),
                                            FormatOps.WrapOrNull(EntityOps.ToListNoThrow(@event))),
                                            typeof(Tcl_EventCallback).Name, TracePriority.EventDebug);
                                    }

                                    ReturnCode code = ReturnCode.Ok;
                                    Result result = null;
                                    int errorLine = 0;

                                    //
                                    // NOTE: Grab all the key event data that we will almost always need.
                                    //
                                    EventType eventType = @event.Type;
                                    EventCallback eventCallback = @event.Callback;
                                    Interpreter eventInterpreter = @event.Interpreter;
                                    IClientData eventClientData = @event.ClientData;

                                    //
                                    // NOTE: If a valid callback was supplied and we are not processing a
                                    //       callback type event, invoke the callback prior to processing
                                    //       the event.
                                    //
                                    if (!noCallback &&
                                        (eventType != EventType.Callback) && (eventCallback != null))
                                    {
                                        code = eventCallback(
                                            (eventInterpreter != null) ? eventInterpreter : interpreter,
                                            (eventClientData != null) ? eventClientData : clientData,
                                            ref result);
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        IClientData performanceClientData = null;

                                        switch (eventType)
                                        {
                                            case EventType.None:
                                                {
                                                    //
                                                    // NOTE: Do nothing, return empty result.
                                                    //
                                                    break;
                                                }
                                            case EventType.Idle:
                                                {
                                                    //
                                                    // NOTE: Process pending events, if any.
                                                    //
                                                    EventWaitHandle idleEvent = null;

                                                    try
                                                    {
                                                        idleEvent = ThreadOps.OpenEvent(idleEventName); /* throw */

                                                        if (idleEvent != null)
                                                        {
                                                            //
                                                            // NOTE: Trigger Tcl interpreter thread to process events.
                                                            //
                                                            ThreadOps.SetEvent(idleEvent);
                                                        }
                                                        else
                                                        {
                                                            result = "cannot open \"idle\" event wait handle object";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                    finally
                                                    {
                                                        ThreadOps.CloseEvent(ref idleEvent);
                                                    }
                                                    break;
                                                }
                                            case EventType.Callback:
                                                {
                                                    if (eventCallback != null)
                                                    {
                                                        code = eventCallback(
                                                            (eventInterpreter != null) ? eventInterpreter : interpreter,
                                                            (eventClientData != null) ? eventClientData : clientData,
                                                            ref result);
                                                    }
                                                    else
                                                    {
                                                        result = "invalid callback";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.Create:
                                                {
                                                    code = TclWrapper.CreateInterpreter(
                                                        tclApi, true, true, false, ref interp, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: We must now update the Tcl interpreter for this object.
                                                        //
                                                        lock (syncRoot)
                                                        {
                                                            this.interp = interp;
                                                            this.initialized = true;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case EventType.Delete:
                                                {
                                                    if (TclWrapper.GetInterpActive(tclApi, interp))
                                                    {
                                                        performanceClientData = new PerformanceClientData(
                                                            "CancelTclEvaluate", !eventTiming);

                                                        code = TclWrapper.CancelEvaluate(
                                                            tclApi, interp, null,
                                                            TclWrapper.GetCancelEvaluateFlags(true),
                                                            ref performanceClientData, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = TclWrapper.DeleteInterpreter(
                                                            tclApi, false, ref interp, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        lock (syncRoot)
                                                        {
                                                            //
                                                            // NOTE: We must now update the Tcl interpreter for this
                                                            //       object.
                                                            //
                                                            this.interp = interp;

                                                            //
                                                            // NOTE: This object is now finalized (we can no longer
                                                            //       handle Tcl requests).
                                                            //
                                                            this.finalized = true;

                                                            //
                                                            // NOTE: The script cancellation delegate cannot be used
                                                            //       any longer (and it should no longer be necessary).
                                                            //
                                                            this.cancelEval = null;
                                                        }

                                                        //
                                                        // NOTE: We must now terminate the thread as far as Tcl is
                                                        //       concerned because we are going to be exiting it
                                                        //       shortly.
                                                        //
                                                        Tcl_FinalizeThread finalizeThread = null;

                                                        if (tclApi != null)
                                                        {
                                                            lock (tclApi.SyncRoot)
                                                            {
                                                                finalizeThread = tclApi.FinalizeThread;
                                                            }
                                                        }

                                                        if (finalizeThread != null)
                                                            finalizeThread();

                                                        EventWaitHandle doneEvent = null;

                                                        try
                                                        {
                                                            doneEvent = ThreadOps.OpenEvent(doneEventName); /* throw */

                                                            if (doneEvent != null)
                                                            {
                                                                //
                                                                // NOTE: Trigger interpreter thread exit.
                                                                //
                                                                ThreadOps.SetEvent(doneEvent);

                                                                //
                                                                // NOTE: Success, return an empty result.
                                                                //
                                                                result = String.Empty;
                                                            }
                                                            else
                                                            {
                                                                result = "cannot open \"done\" event wait handle object during delete";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            result = e;
                                                            code = ReturnCode.Error;
                                                        }
                                                        finally
                                                        {
                                                            ThreadOps.CloseEvent(ref doneEvent);
                                                        }
                                                    }
                                                    break;
                                                }
                                            case EventType.Expression:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        IAnyPair<bool, string> anyPair =
                                                            eventClientData.Data as
                                                            IAnyPair<bool, string>;

                                                        if (anyPair != null)
                                                        {
                                                            bool exceptions = anyPair.X;
                                                            string text = anyPair.Y;

                                                            performanceClientData = new PerformanceClientData(
                                                                "EvaluateTclExpression", !eventTiming);

                                                            code = TclWrapper.EvaluateExpression(
                                                                tclApi, interp, text, exceptions,
                                                                ref performanceClientData, ref result);

                                                            if (code == ReturnCode.Error)
                                                                errorLine = TclWrapper.GetErrorLine(tclApi, interp);
                                                        }
                                                        else
                                                        {
                                                            result = "invalid event pair";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.Evaluate:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        IAnyTriplet<Tcl_EvalFlags, bool, string> anyTriplet =
                                                            eventClientData.Data as
                                                            IAnyTriplet<Tcl_EvalFlags, bool, string>;

                                                        if (anyTriplet != null)
                                                        {
                                                            Tcl_EvalFlags flags = anyTriplet.X;
                                                            bool exceptions = anyTriplet.Y;
                                                            string text = anyTriplet.Z;

                                                            performanceClientData = new PerformanceClientData(
                                                                "EvaluateTclScript", !eventTiming);

                                                            code = TclWrapper.EvaluateScript(
                                                                tclApi, interp, text, flags, exceptions,
                                                                ref performanceClientData, ref result);

                                                            if (code == ReturnCode.Error)
                                                                errorLine = TclWrapper.GetErrorLine(tclApi, interp);
                                                        }
                                                        else
                                                        {
                                                            result = "invalid event triplet";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.SimpleEvaluate:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        string text = eventClientData.Data as string;

                                                        performanceClientData = new PerformanceClientData(
                                                            "SimpleEvaluateTclScript", !eventTiming);

                                                        code = TclWrapper.EvaluateScript(
                                                            tclApi, interp, text, Tcl_EvalFlags.TCL_EVAL_NONE,
                                                            (interpreter != null) ? interpreter.TclExceptions :
                                                                TclApi.DefaultExceptions,
                                                            ref performanceClientData, ref result);

                                                        if (code == ReturnCode.Error)
                                                            errorLine = TclWrapper.GetErrorLine(tclApi, interp);
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.Substitute:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        IAnyTriplet<Tcl_SubstFlags, bool, string> anyTriplet =
                                                            eventClientData.Data as
                                                            IAnyTriplet<Tcl_SubstFlags, bool, string>;

                                                        if (anyTriplet != null)
                                                        {
                                                            Tcl_SubstFlags flags = anyTriplet.X;
                                                            bool exceptions = anyTriplet.Y;
                                                            string text = anyTriplet.Z;

                                                            performanceClientData = new PerformanceClientData(
                                                                "SubstituteTclString", !eventTiming);

                                                            code = TclWrapper.SubstituteString(
                                                                tclApi, interp, text, flags, exceptions,
                                                                ref performanceClientData, ref result);

                                                            if (code == ReturnCode.Error)
                                                                errorLine = TclWrapper.GetErrorLine(tclApi, interp);
                                                        }
                                                        else
                                                        {
                                                            result = "invalid event triplet";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.Cancel:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        Result cancelResult = eventClientData.Data as Result;

                                                        if (cancelResult != null)
                                                        {
                                                            performanceClientData = new PerformanceClientData(
                                                                "CancelTclEvaluate", !eventTiming);

                                                            code = TclWrapper.CancelEvaluate(
                                                                tclApi, interp, cancelResult,
                                                                TclWrapper.GetCancelEvaluateFlags(false),
                                                                ref performanceClientData, ref result);
                                                        }
                                                        else
                                                        {
                                                            code = CancelEvaluateNoLock(
                                                                cancelEval, interp, false, ref result);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.Unwind:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        Result cancelResult = eventClientData.Data as Result;

                                                        if (cancelResult != null)
                                                        {
                                                            performanceClientData = new PerformanceClientData(
                                                                "UnwindTclEvaluate", !eventTiming);

                                                            code = TclWrapper.CancelEvaluate(
                                                                tclApi, interp, cancelResult,
                                                                TclWrapper.GetCancelEvaluateFlags(true),
                                                                ref performanceClientData, ref result);
                                                        }
                                                        else
                                                        {
                                                            code = CancelEvaluateNoLock(
                                                                cancelEval, interp, true, ref result);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.ResetCancel:
                                                {
                                                    code = TclWrapper.ResetCancellation(
                                                        tclApi, interp, false, ref result);

                                                    break;
                                                }
                                            case EventType.GetVariable:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        string varName = eventClientData.Data as string;

                                                        code = TclWrapper.GetVariable(
                                                            tclApi, interp, Tcl_VarFlags.TCL_VAR_NONE, varName,
                                                            ref result, ref result);
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.SetVariable:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        IPair<string> pair =
                                                            eventClientData.Data as IPair<string>;

                                                        if (pair != null)
                                                        {
                                                            string varName = pair.X;
                                                            string varValue = pair.Y;

                                                            //
                                                            // NOTE: Initially, the result is the new value to
                                                            //       set.  This may be modified via Tcl variable
                                                            //       traces or in the event of an error.
                                                            //
                                                            result = varValue;

                                                            code = TclWrapper.SetVariable(
                                                                tclApi, interp, Tcl_VarFlags.TCL_VAR_NONE, varName,
                                                                ref result, ref result);
                                                        }
                                                        else
                                                        {
                                                            result = "invalid event pair";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.UnsetVariable:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        string varName = eventClientData.Data as string;

                                                        code = TclWrapper.UnsetVariable(
                                                            tclApi, interp, Tcl_VarFlags.TCL_VAR_NONE, varName,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.AddCommand:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        IAnyTriplet<string, IExecute, bool> anyTriplet =
                                                            eventClientData.Data as
                                                            IAnyTriplet<string, IExecute, bool>;

                                                        if (anyTriplet != null)
                                                        {
                                                            if (interpreter != null)
                                                            {
                                                                string commandName = anyTriplet.X;
                                                                IExecute execute = anyTriplet.Y;
                                                                bool forceDelete = anyTriplet.Z;
                                                                IClientData executeClientData;

                                                                /* IGNORED */
                                                                ClientData.TryGet(execute, false,
                                                                    out executeClientData);

                                                                TclCommandFlags flags = TclCommandFlags.None;

                                                                if (forceDelete)
                                                                    flags |= TclCommandFlags.ForceDelete;

                                                                if (DefaultCommandNoComplain)
                                                                    flags |= TclCommandFlags.NoComplain;

                                                                code = interpreter.AddTclBridge(
                                                                    execute, name, commandName,
                                                                    executeClientData, flags,
                                                                    ref result);
                                                            }
                                                            else
                                                            {
                                                                result = "invalid interpreter";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "invalid event triplet";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.AddStandardCommand:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        IAnyPair<string, bool> anyPair =
                                                            eventClientData.Data as IAnyPair<string, bool>;

                                                        if (anyPair != null)
                                                        {
                                                            if (interpreter != null)
                                                            {
                                                                string commandName = anyPair.X;
                                                                bool forceDelete = anyPair.Y;
                                                                TclCommandFlags flags = TclCommandFlags.None;

                                                                if (forceDelete)
                                                                    flags |= TclCommandFlags.ForceDelete;

                                                                if (DefaultCommandNoComplain)
                                                                    flags |= TclCommandFlags.NoComplain;

                                                                code = interpreter.AddStandardTclBridge(
                                                                    name, commandName, null, flags,
                                                                    ref result);
                                                            }
                                                            else
                                                            {
                                                                result = "invalid interpreter";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "invalid event pair";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.RemoveCommand:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        if (interpreter != null)
                                                        {
                                                            string commandName = eventClientData.Data as string;

                                                            code = interpreter.RemoveTclBridge(
                                                                name, commandName, null, TclCommandFlags.None,
                                                                ref result);
                                                        }
                                                        else
                                                        {
                                                            result = "invalid interpreter";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid clientData";
                                                        code = ReturnCode.Error;
                                                    }
                                                    break;
                                                }
                                            case EventType.GetResult:
                                                {
                                                    result = StringList.MakeList(
                                                        ReturnCode.Invalid, TclWrapper.GetResultAsString(
                                                        tclApi, interp));

                                                    code = ReturnCode.Ok;
                                                    break;
                                                }
                                            case EventType.Dispose:
                                                {
                                                    if (eventClientData != null)
                                                    {
                                                        TclBridgeDictionary tclBridges =
                                                            eventClientData.Data as TclBridgeDictionary;

                                                        if (tclBridges != null)
                                                        {
                                                            foreach (TclBridgePair pair in tclBridges)
                                                            {
                                                                TclBridge tclBridge = pair.Value;

                                                                if (tclBridge == null)
                                                                    continue;

                                                                tclBridge.Dispose();
                                                            }

                                                            tclBridges.Clear();
                                                        }
                                                    }
                                                    goto case EventType.Delete;
                                                }
                                            default:
                                                {
                                                    //
                                                    // NOTE: Nothing we can do here except log the failure.
                                                    //
                                                    result = String.Format(
                                                        "unknown event type \"{0}\"",
                                                        eventType);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }
                                        }

                                        //
                                        // NOTE: If the debug flag is set, show the result of the event.
                                        //
                                        if (eventDebug)
                                        {
                                            TraceOps.DebugTrace(threadId, String.Format(
                                                "completed event {0} at {1} in {2} on thread {3}: " +
                                                "code = {4}, result = {5}",
                                                FormatOps.DisplayName(EntityOps.GetNameNoThrow(
                                                @event)), TimeOps.GetUtcNow(), FormatOps.PerformanceMicroseconds(
                                                performanceClientData),
                                                GlobalState.GetCurrentNativeThreadId(), code,
                                                FormatOps.WrapOrNull(true, true, result)),
                                                typeof(Tcl_EventCallback).Name,
                                                TracePriority.EventDebug);
                                        }
                                    }
                                    else
                                    {
                                        TraceOps.DebugTrace(threadId,
                                            "callback error, event skipped",
                                            typeof(Tcl_EventCallback).Name,
                                            TracePriority.CallbackError);
                                    }

                                    //
                                    // NOTE: Attempt to set the result of this event and signal
                                    //       that it is ready.
                                    //
                                    Result error = null;

                                    if (!@event.SetResult(
                                            timeout, true, code, result, errorLine, ref error))
                                    {
                                        TraceOps.DebugTrace(threadId, String.Format(
                                            "cannot set event result (timeout of {0} milliseconds?)",
                                            timeout), typeof(Tcl_EventCallback).Name,
                                            TracePriority.ThreadError);
                                    }

                                    //
                                    // NOTE: If a valid notification callback was supplied, invoke
                                    //       it now.
                                    //
                                    if (!noNotify && (callback != null))
                                    {
                                        ReturnCode notifyCode;
                                        Result notifyResult = null;

                                        notifyCode = callback(
                                            eventInterpreter, clientData, eventClientData,
                                            @event, code, result, errorLine, ref notifyResult);

                                        if (!noComplain && (notifyCode != ReturnCode.Ok))
                                        {
                                            DebugOps.Complain(
                                                eventInterpreter, notifyCode, notifyResult);
                                        }
                                    }
                                }
                                else
                                {
                                    //
                                    // NOTE: What now?  We have no way of communicating at this
                                    //       point.
                                    //
                                    TraceOps.DebugTrace(threadId,
                                        "invalid event object",
                                        typeof(Tcl_EventCallback).Name,
                                        TracePriority.MarshalError);
                                }
                            }
                            finally
                            {
                                if (@event != null)
                                {
                                    /* IGNORED */
                                    Event.MaybeDispose(@event);
                                    @event = null;
                                }
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Again, nothing we can do at this point.
                            //
                            TraceOps.DebugTrace(threadId,
                                "invalid GC handle",
                                typeof(Tcl_EventCallback).Name,
                                TracePriority.MarshalError);
                        }
                    }
                    finally
                    {
                        //
                        // NOTE: If this handle was actually allocated then we must free
                        //       it (i.e. the sending thread assumes that we will free it).
                        //
                        if (handle.IsAllocated)
                            handle.Free();
                    }
                }
                catch (Exception e)
                {
                    //
                    // NOTE: Nothing we can do here except log the failure.
                    //
                    TraceOps.DebugTrace(threadId,
                        e, typeof(Tcl_EventCallback).Name,
                        TracePriority.MarshalError);
                }
            }
            else
            {
                //
                // NOTE: Reject the request because Tcl has already been finalized
                //       for this thread (which means we may be exiting shortly).
                //
                TraceOps.DebugTrace(threadId,
                    "already finalized",
                    typeof(Tcl_EventCallback).Name,
                    TracePriority.MarshalError);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // ***** WARNING *****  END CODE DIRECTLY CALLED BY THE NATIVE WIN32 API  ***** WARNING ***** /
        ///////////////////////////////////////////////////////////////////////////////////////////////
        #endregion
    }
}
