/*
 * Event.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;

namespace Eagle._Components.Public
{
    [ObjectId("d46c23bf-20b3-4f17-8869-126dcbdb265d")]
    public sealed class Event : IEvent, IDisposable
    {
        #region Private Static Data
        //
        // NOTE: This is the counter of how many of these object instances are
        //       created in this AppDomain.  It is never reset.
        //
        private static int createCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the counter of how many of these object instances are
        //       disposed in this AppDomain.  It is never reset.
        //
        private static int disposeCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the name for the EventWaitHandle associated with this
        //       logical event.  It is treated as immutable after being set in
        //       the constructor.
        //
        private readonly string doneEventName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This EventWaitHandle is opened in the constructor and is not
        //       closed until this logical event is disposed.
        //
        private EventWaitHandle doneEvent;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These fields make up the result of this logical event.
        //
        private ReturnCode returnCode;
        private Result result;
        private int errorLine;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Event(
            object syncRoot,         /* in: OPTIONAL */
            Delegate @delegate,      /* in: OPTIONAL */
            EventType type,          /* in */
            EventFlags flags,        /* in */
            EventPriority priority,  /* in */
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in: OPTIONAL */
            DateTime dateTime,       /* in */
            EventCallback callback,  /* in: OPTIONAL */
            long? threadId,          /* in: OPTIONAL */
            IClientData clientData   /* in: OPTIONAL */
            )
        {
            this.syncRoot = syncRoot;
            this.@delegate = @delegate;
            this.type = type;
            this.flags = flags;
            this.priority = priority;
            this.interpreter = interpreter;
            this.name = name;
            this.dateTime = dateTime;
            this.callback = callback;
            this.threadId = threadId;
            this.clientData = clientData;

            //
            // NOTE: Setup inter-thread communication event.
            //
            doneEventName = FormatOps.EventName(interpreter,
                typeof(Event).Name, name, GlobalState.NextEventId(
                interpreter));

            doneEvent = ThreadOps.CreateEvent(doneEventName);

            //
            // NOTE: Setup the initial result state.
            //
            ResetResultData();

            //
            // NOTE: Keep track of how many event objects are created
            //       within this AppDomain.
            //
            Interlocked.Increment(ref createCount);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        internal static IEvent Create(
            object syncRoot,         /* in: OPTIONAL */
            Delegate @delegate,      /* in: OPTIONAL */
            EventType type,          /* in */
            EventFlags flags,        /* in */
            EventPriority priority,  /* in */
            Interpreter interpreter, /* in: OPTIONAL */
            string name,             /* in: OPTIONAL */
            DateTime dateTime,       /* in */
            EventCallback callback,  /* in: OPTIONAL */
            long? threadId,          /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            ref Result error         /* out: NOT USED */
            )
        {
            return new Event(
                syncRoot, @delegate, type, flags, priority, interpreter,
                name, dateTime, callback, threadId, clientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private No-Lock Result Data Methods
        //
        // WARNING: This method assumes the lock is held.
        //
        private void ResetResultData()
        {
            returnCode = ReturnCode.Ok;
            result = null;
            errorLine = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the lock is held.
        //
        private void GetResultData(
            ref ReturnCode returnCode, /* out */
            ref Result result,         /* out */
            ref int errorLine          /* out */
            )
        {
            returnCode = this.returnCode;
            result = this.result;
            errorLine = this.errorLine;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the lock is held.
        //
        private void SetResultData(
            ReturnCode returnCode, /* in */
            Result result,         /* in */
            int errorLine          /* in */
            )
        {
            this.returnCode = returnCode;
            this.result = result;
            this.errorLine = errorLine;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Result Methods
        private void GetResult(
            ref ReturnCode returnCode, /* out */
            ref Result result,         /* out */
            ref int errorLine          /* out */
            )
        {
            object syncRoot = this.syncRoot;

            if (syncRoot != null)
                Monitor.Enter(syncRoot);

            try
            {
                GetResultData(
                    ref returnCode, ref result, ref errorLine);
            }
            finally
            {
                if (syncRoot != null)
                    Monitor.Exit(syncRoot);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool GetResult(
            int timeout,               /* in */
            ref ReturnCode returnCode, /* out */
            ref Result result,         /* out */
            ref int errorLine          /* out */
            )
        {
            object syncRoot = this.syncRoot;

            if ((syncRoot != null) &&
                !Monitor.TryEnter(syncRoot, timeout))
            {
                return false;
            }

            try
            {
                GetResultData(
                    ref returnCode, ref result, ref errorLine);

                return true;
            }
            finally
            {
                if (syncRoot != null)
                    Monitor.Exit(syncRoot);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void ResetResult()
        {
            object syncRoot = this.syncRoot;

            if (syncRoot != null)
                Monitor.Enter(syncRoot);

            try
            {
                ResetResultData();
            }
            finally
            {
                if (syncRoot != null)
                    Monitor.Exit(syncRoot);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool ResetResult(
            int timeout /* in */
            )
        {
            object syncRoot = this.syncRoot;

            if ((syncRoot != null) &&
                !Monitor.TryEnter(syncRoot, timeout))
            {
                return false;
            }

            try
            {
                ResetResultData();

                return true;
            }
            finally
            {
                if (syncRoot != null)
                    Monitor.Exit(syncRoot);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetResult(
            ReturnCode returnCode, /* in */
            Result result,         /* in */
            int errorLine          /* in */
            )
        {
            object syncRoot = this.syncRoot;

            if (syncRoot != null)
                Monitor.Enter(syncRoot);

            try
            {
                SetResultData(returnCode, result, errorLine);
            }
            finally
            {
                if (syncRoot != null)
                    Monitor.Exit(syncRoot);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool SetResult(
            int timeout,           /* in */
            ReturnCode returnCode, /* in */
            Result result,         /* in */
            int errorLine          /* in */
            )
        {
            object syncRoot = this.syncRoot;

            if ((syncRoot != null) &&
                !Monitor.TryEnter(syncRoot, timeout))
            {
                return false;
            }

            try
            {
                SetResultData(returnCode, result, errorLine);

                return true;
            }
            finally
            {
                if (syncRoot != null)
                    Monitor.Exit(syncRoot);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Event Methods
        private bool WaitOnEventHandle(
            ref Result error
            )
        {
            EventWaitHandle doneEvent = null;

            try
            {
                doneEvent = ThreadOps.OpenEvent(doneEventName);

                if (doneEvent != null)
                {
                    if (ThreadOps.WaitEvent(doneEvent))
                    {
                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "infinite wait for event {0} failed",
                            FormatOps.WrapOrNull(doneEventName));
                    }
                }
                else
                {
                    error = String.Format(
                        "cannot open event {0} in order to wait",
                        FormatOps.WrapOrNull(doneEventName));
                }
            }
            finally
            {
                /* NO RESULT */
                ThreadOps.CloseEvent(ref doneEvent);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool WaitOnEventHandle(
            int timeout,
            ref Result error
            )
        {
            EventWaitHandle doneEvent = null;

            try
            {
                doneEvent = ThreadOps.OpenEvent(doneEventName);

                if (doneEvent != null)
                {
                    if (ThreadOps.WaitEvent(
                            doneEvent, timeout))
                    {
                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "timed wait of {0} milliseconds for event {1} failed",
                            timeout, FormatOps.WrapOrNull(doneEventName));
                    }
                }
                else
                {
                    error = String.Format(
                        "cannot open event {0} in order to wait",
                        FormatOps.WrapOrNull(doneEventName));
                }
            }
            finally
            {
                /* NO RESULT */
                ThreadOps.CloseEvent(ref doneEvent);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool ResetEventHandle(
            ref Result error
            )
        {
            EventWaitHandle doneEvent = null;

            try
            {
                doneEvent = ThreadOps.OpenEvent(doneEventName);

                if (doneEvent != null)
                {
                    if (ThreadOps.ResetEvent(doneEvent))
                    {
                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "reset for event {0} failed",
                            FormatOps.WrapOrNull(doneEventName));
                    }
                }
                else
                {
                    error = String.Format(
                        "cannot open event {0} in order to reset",
                        FormatOps.WrapOrNull(doneEventName));
                }
            }
            finally
            {
                /* NO RESULT */
                ThreadOps.CloseEvent(ref doneEvent);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool SetEventHandle(
            ref Result error
            )
        {
            EventWaitHandle doneEvent = null;

            try
            {
                doneEvent = ThreadOps.OpenEvent(doneEventName);

                if (doneEvent != null)
                {
                    if (ThreadOps.SetEvent(doneEvent))
                    {
                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "set for event {0} failed",
                            FormatOps.WrapOrNull(doneEventName));
                    }
                }
                else
                {
                    error = String.Format(
                        "cannot open event {0} in order to set",
                        FormatOps.WrapOrNull(doneEventName));
                }
            }
            finally
            {
                /* NO RESULT */
                ThreadOps.CloseEvent(ref doneEvent);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Members
        internal static int CreateCount
        {
            get
            {
                return Interlocked.CompareExchange(ref createCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal static int DisposeCount
        {
            get
            {
                return Interlocked.CompareExchange(ref disposeCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void MarkDequeued(
            IEvent @event
            )
        {
            ModifyFlags(@event, EventFlags.WasDequeued, true);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void MarkCompleted(
            IEvent @event
            )
        {
            ModifyFlags(@event, EventFlags.WasCompleted, true);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void MarkDequeuedAndCanceled(
            IEvent @event
            )
        {
            ModifyFlags(@event,
                EventFlags.WasDequeued | EventFlags.WasCanceled, true);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void MarkDequeuedAndDiscarded(
            IEvent @event
            )
        {
            ModifyFlags(@event,
                EventFlags.WasDequeued | EventFlags.WasDiscarded, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ModifyFlags(
            IEvent @event,
            EventFlags flags,
            bool add
            )
        {
            if (@event != null)
            {
                Event localEvent = @event as Event;

                if (localEvent != null)
                {
                    if (add)
                        localEvent.Flags |= flags;
                    else
                        localEvent.Flags &= ~flags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal static bool MaybeDispose(
            IEvent @event /* in */
            )
        {
            if (@event != null)
            {
                EventFlags flags = EntityOps.GetFlagsNoThrow(@event);

                if (FlagOps.HasFlags(
                        flags, EventFlags.FireAndForget, true))
                {
                    return Dispose(@event);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static bool Dispose(
            IEvent @event /* in */
            )
        {
            try
            {
                IDisposable disposable = @event as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose(); /* throw */
                    disposable = null;

                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Event).Name,
                    TracePriority.EventError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
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

        #region ISynchronize Members
        private object syncRoot;
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void TryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        public void TryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        public void ExitLock(
            ref bool locked
            )
        {
            if (RuntimeOps.ShouldCheckDisposedOnExitLock(locked)) /* EXEMPT */
                CheckDisposed();

            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEvent Members
        private Delegate @delegate;
        public Delegate Delegate
        {
            get { CheckDisposed(); return @delegate; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventType type;
        public EventType Type
        {
            get { CheckDisposed(); return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags flags;
        public EventFlags Flags
        {
            get { CheckDisposed(); return flags; }
            private set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventPriority priority;
        public EventPriority Priority
        {
            get { CheckDisposed(); return priority; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime dateTime;
        public DateTime DateTime
        {
            get { CheckDisposed(); return dateTime; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventCallback callback;
        public EventCallback Callback
        {
            get { CheckDisposed(); return callback; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long? threadId;
        public long? ThreadId
        {
            get { CheckDisposed(); return threadId; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetResult(
            bool wait,                 /* in */
            ref ReturnCode returnCode, /* out */
            ref Result result,         /* out */
            ref int errorLine,         /* out */
            ref Result error           /* out */
            )
        {
            CheckDisposed();

            try
            {
                if (wait && !WaitOnEventHandle(ref error))
                    return false;

                /* NO RESULT */
                GetResult(ref returnCode, ref result, ref errorLine);

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetResult(
            int timeout,               /* in */
            bool wait,                 /* in */
            ref ReturnCode returnCode, /* out */
            ref Result result,         /* out */
            ref int errorLine,         /* out */
            ref Result error           /* out */
            )
        {
            CheckDisposed();

            try
            {
                if (wait && !WaitOnEventHandle(timeout, ref error))
                    return false;

                if (!GetResult(
                        timeout, ref returnCode, ref result,
                        ref errorLine))
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetResult(
            bool signal,     /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            try
            {
                /* NO RESULT */
                ResetResult();

                if (signal && !ResetEventHandle(ref error))
                    return false;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetResult(
            int timeout,     /* in */
            bool signal,     /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            try
            {
                if (!ResetResult(timeout))
                    return false;

                if (signal && !ResetEventHandle(ref error))
                    return false;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetResult(
            bool signal,           /* in */
            ReturnCode returnCode, /* in */
            Result result,         /* in */
            int errorLine,         /* in */
            ref Result error       /* out */
            )
        {
            CheckDisposed();

            try
            {
                /* NO RESULT */
                SetResult(returnCode, result, errorLine);

                if (signal && !SetEventHandle(ref error))
                    return false;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetResult(
            int timeout,           /* in */
            bool signal,           /* in */
            ReturnCode returnCode, /* in */
            Result result,         /* in */
            int errorLine,         /* in */
            ref Result error       /* out */
            )
        {
            CheckDisposed();

            try
            {
                if (!SetResult(timeout, returnCode, result, errorLine))
                    return false;

                if (signal && !SetEventHandle(ref error))
                    return false;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList ToList()
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            list.Add("name", (name != null) ? name : _String.Null);

            list.Add("interpreter", (interpreter != null) ?
                interpreter.InternalToString() : _String.Null);

            if (!id.Equals(Guid.Empty))
                list.Add("id", id.ToString());

            list.Add("type", type.ToString());
            list.Add("flags", flags.ToString());
            list.Add("priority", priority.ToString());
            list.Add("dateTime", dateTime.ToString());

            list.Add("callback", (callback != null) ?
                callback.ToString() : _String.Null);

            list.Add("threadId", (threadId != null) ?
                ((long)threadId).ToString() : _String.Null);

            list.Add("clientData", (clientData != null) ?
                StringList.MakeList(_ClientData.GetDataTypeName(
                clientData, _String.Null, _String.Proxy, false),
                clientData.Data) : _String.Null);

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return (name != null) ? name : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(Event).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            ) /* throw */
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Dispose of the "done" inter-thread communication
                    //       event.
                    //
                    /* NO RESULT */
                    ThreadOps.CloseEvent(ref doneEvent);

                    //
                    // NOTE: Keep track of how many event objects are disposed
                    //       within this AppDomain.  This is applicable only
                    //       when the object is explicitly disposed, not merely
                    //       finalized.
                    //
                    Interlocked.Increment(ref disposeCount);
                }
#if DEBUG
                else
                {
                    DebugOps.MaybeBreak();
                }
#endif

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Event()
        {
            Dispose(false);
        }
        #endregion
    }
}
