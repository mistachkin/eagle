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

namespace Eagle._Components.Private.Tcl
{
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
        private const string tclThreadStartEventPrefix = "threadStart";
        private const string tclThreadDoneEventPrefix = "threadDone";
        private const string tclThreadIdleEventPrefix = "threadIdle";
        private const string tclThreadQueueEventPrefix = "threadQueue";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly bool DefaultCommandNoComplain = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private GCHandle handle; /* TclThread */

        private Interpreter interpreter;
        private ResultCallback callback;
        private IClientData clientData;
        private int timeout;
        private string name;
        private bool generic;
        private bool debug;

        private long threadId;
        private Thread thread;
        private IntPtr interp;
        private bool initialized; /* NOTE: Has an interp ever been created? */
        private bool finalized;   /* NOTE: Has the thread been finalized? */

        private Tcl_CancelEval cancelEval; /* NOTE: Cached to avoid tricky locking issues. */

        private string startEventName;   /* NOTE: Write-once, then read-only. */
        private string doneEventName;    /* NOTE: Write-once, then read-only. */
        private string idleEventName;    /* NOTE: Write-once, then read-only. */
        private string queueEventName;   /* NOTE: Write-once, then read-only. */

        private EventWaitHandle startEvent;
        private EventWaitHandle doneEvent;
        private EventWaitHandle idleEvent;
        private EventWaitHandle queueEvent;

        private IntPtr queueEventData; /* NOTE: Access is synchronized. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private TclThread(
            Interpreter interpreter,
            ResultCallback callback,
            IClientData clientData,
            int timeout,
            string name,
            bool generic,
            bool debug,
            bool start
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
            this.generic = generic;
            this.debug = debug;

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
            thread = Engine.CreateThread(
                interpreter, ThreadStart, 0, true, false, true);

            if (thread != null)
            {
                //
                // NOTE: Give the thread a name.
                //
                thread.Name = this.name;

                //
                // NOTE: Caller requested that the thread be started now?
                //
                if (start)
                    thread.Start();
            }
            else
            {
                throw new ScriptException("could not create Tcl thread");
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        private object syncRoot;
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            PrivateTryLock(timeout, ref locked);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        private void PrivateTryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public long ThreadId
        {
            get { CheckDisposed(); lock (syncRoot) { return threadId; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IntPtr Interp
        {
            get { CheckDisposed(); lock (syncRoot) { return interp; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Initialized
        {
            get { CheckDisposed(); lock (syncRoot) { return initialized; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Finalized
        {
            get { CheckDisposed(); lock (syncRoot) { return finalized; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int Timeout
        {
            get { CheckDisposed(); lock (syncRoot) { return timeout; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string Name
        {
            get { CheckDisposed(); lock (syncRoot) { return name; } }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool IsGeneric
        {
            get { CheckDisposed(); lock (syncRoot) { return generic; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
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
        private bool disposing;
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TclThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
            ReturnCode shutdownCode;
            Result shutdownError = null;

            shutdownCode = Shutdown(
                tclApi, tclBridges, true, true, false, ref shutdownError);

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
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~TclThread() /* throw */
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static TclThread Create(
            Interpreter interpreter,
            ResultCallback callback,
            IClientData clientData,
            int timeout,
            string name,
            bool generic,
            bool debug,
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
                            generic, debug, true);

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
        private void ThreadStart()
        {
            long threadId;
            Interpreter interpreter;
            int timeout;
            bool debug;

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
                // NOTE: Cache the debug mode value for this object in a local variable.
                //
                debug = this.debug;
            }

            if (interpreter != null)
            {
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
                                        ThreadOps.WasAnyEventTimeout(index), true))
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
                                if (eventCode != ReturnCode.Ok) /* RARE */
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
                            // NOTE: Finally, attempt to yield to other running threads.
                            //
                            ReturnCode yieldCode;
                            Result yieldError = null;

                            yieldCode = HostOps.Yield(ref yieldError);

                            //
                            // NOTE: If there was some kind of error just report it and
                            //       continue with the next event.
                            //
                            if (yieldCode != ReturnCode.Ok) /* RARE */
                                DebugOps.Complain(interpreter, yieldCode, yieldError);
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
                                this, interpreter, timeout, debug, index == 0, true);
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

        public ReturnCode Shutdown(
            ITclApi tclApi,
            TclBridgeDictionary tclBridges,
            bool delete,
            bool force,
            bool strict,
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
                        if ((thread != null) && thread.IsAlive)
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
                            if ((code == ReturnCode.Ok) && force)
                            {
                                //
                                // NOTE: Wait a bit for the thread to exit.
                                //
                                if (!thread.Join(ThreadOps.DefaultJoinTimeout))
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
                        else if (strict)
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
            if (!generic && PlatformOps.IsWindowsOperatingSystem())
                return QueueEventWindows(type, flags, data, synchronous, ref result, ref errorLine);
            else
#endif
                return QueueEventGeneric(type, flags, data, synchronous, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                                                            result = "invalid object pair";
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
                                                            result = "invalid object triplet";
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
                                                            result = "invalid object triplet";
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
                                                            result = "invalid object pair";
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

                                                                code = interpreter.AddTclBridge(
                                                                    execute, name, commandName,
                                                                    executeClientData, forceDelete,
                                                                    DefaultCommandNoComplain,
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
                                                            result = "invalid object triplet";
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

                                                                code = interpreter.AddStandardTclBridge(
                                                                    name, commandName, null, forceDelete,
                                                                    DefaultCommandNoComplain, ref result);
                                                            }
                                                            else
                                                            {
                                                                result = "invalid interpreter";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "invalid object pair";
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
                                                                name, commandName, null, ref result);
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
                                                            foreach (KeyValuePair<string, TclBridge> pair in tclBridges)
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
                                                @event)), TimeOps.GetUtcNow(), FormatOps.Performance(
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

                                        if (notifyCode != ReturnCode.Ok)
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
