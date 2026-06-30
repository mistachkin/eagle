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
    /// <summary>
    /// This class represents a single logical event managed by the Eagle event
    /// manager.  It carries the metadata describing the event (its type, flags,
    /// priority, scheduled time, target thread, and optional callback or
    /// delegate) together with the result produced when the event is processed.
    /// It uses an <see cref="EventWaitHandle" /> to support inter-thread
    /// signaling so that one thread may wait for the result produced by another.
    /// </summary>
    [ObjectId("d46c23bf-20b3-4f17-8869-126dcbdb265d")]
    public sealed class Event : IEvent, IDisposable
    {
        #region Private Static Data
        //
        // NOTE: This is the counter of how many of these object instances are
        //       created in this AppDomain.  It is never reset.
        //
        /// <summary>
        /// The number of these object instances that have been created within
        /// this application domain.  It is never reset.
        /// </summary>
        private static int createCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the counter of how many of these object instances are
        //       disposed in this AppDomain.  It is never reset.
        //
        /// <summary>
        /// The number of these object instances that have been disposed within
        /// this application domain.  It is never reset.
        /// </summary>
        private static int disposeCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the name for the EventWaitHandle associated with this
        //       logical event.  It is treated as immutable after being set in
        //       the constructor.
        //
        /// <summary>
        /// The name of the <see cref="EventWaitHandle" /> associated with this
        /// logical event.  It is treated as immutable after being set in the
        /// constructor.
        /// </summary>
        private readonly string doneEventName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This EventWaitHandle is opened in the constructor and is not
        //       closed until this logical event is disposed.
        //
        /// <summary>
        /// The <see cref="EventWaitHandle" /> used to signal completion of this
        /// logical event.  It is opened in the constructor and is not closed
        /// until this logical event is disposed.
        /// </summary>
        private EventWaitHandle doneEvent;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These fields make up the result of this logical event.
        //
        /// <summary>
        /// The return code component of the result of this logical event.
        /// </summary>
        private ReturnCode returnCode;
        /// <summary>
        /// The result value (or error message) component of the result of this
        /// logical event.
        /// </summary>
        private Result result;
        /// <summary>
        /// The error line component of the result of this logical event.
        /// </summary>
        private int errorLine;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new instance of this logical event, opening its
        /// inter-thread communication event and initializing its result to a
        /// well-known reset state.
        /// </summary>
        /// <param name="syncRoot">
        /// The object used to synchronize access to this event.  This parameter
        /// may be null, in which case no locking is performed.
        /// </param>
        /// <param name="delegate">
        /// The delegate associated with this event, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="type">
        /// The type of this event.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of this event.
        /// </param>
        /// <param name="priority">
        /// The priority of this event.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context associated with this event.  This parameter
        /// may be null.
        /// </param>
        /// <param name="name">
        /// The name of this event.  This parameter may be null.
        /// </param>
        /// <param name="dateTime">
        /// The date and time at which this event is scheduled to be processed.
        /// </param>
        /// <param name="callback">
        /// The callback to be invoked when this event is processed, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread associated with this event, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this event.  This parameter may be
        /// null.
        /// </param>
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
        /// <summary>
        /// This method creates a new logical event with the specified
        /// properties.
        /// </summary>
        /// <param name="syncRoot">
        /// The object used to synchronize access to the new event.  This
        /// parameter may be null, in which case no locking is performed.
        /// </param>
        /// <param name="delegate">
        /// The delegate associated with the new event, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="type">
        /// The type of the new event.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the new event.
        /// </param>
        /// <param name="priority">
        /// The priority of the new event.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context associated with the new event.  This
        /// parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name of the new event.  This parameter may be null.
        /// </param>
        /// <param name="dateTime">
        /// The date and time at which the new event is scheduled to be
        /// processed.
        /// </param>
        /// <param name="callback">
        /// The callback to be invoked when the new event is processed, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread associated with the new event, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the new event.  This parameter may
        /// be null.
        /// </param>
        /// <param name="error">
        /// This parameter is reserved for future use and is not currently
        /// used.
        /// </param>
        /// <returns>
        /// The newly created logical event.
        /// </returns>
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
        /// <summary>
        /// This method resets the result data of this logical event to its
        /// well-known initial state.  It assumes the lock is held.
        /// </summary>
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
        /// <summary>
        /// This method retrieves the current result data of this logical event.
        /// It assumes the lock is held.
        /// </summary>
        /// <param name="returnCode">
        /// Upon return, this contains the return code component of the result.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result value (or error message)
        /// component of the result.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this contains the error line component of the result.
        /// </param>
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
        /// <summary>
        /// This method stores the specified result data on this logical event.
        /// It assumes the lock is held.
        /// </summary>
        /// <param name="returnCode">
        /// The return code component of the result.
        /// </param>
        /// <param name="result">
        /// The result value (or error message) component of the result.
        /// </param>
        /// <param name="errorLine">
        /// The error line component of the result.
        /// </param>
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
        /// <summary>
        /// This method retrieves the current result data of this logical event,
        /// acquiring the synchronization lock for the duration of the
        /// operation.
        /// </summary>
        /// <param name="returnCode">
        /// Upon return, this contains the return code component of the result.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result value (or error message)
        /// component of the result.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this contains the error line component of the result.
        /// </param>
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

        /// <summary>
        /// This method retrieves the current result data of this logical event,
        /// attempting to acquire the synchronization lock within the specified
        /// timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the
        /// synchronization lock to be acquired.
        /// </param>
        /// <param name="returnCode">
        /// Upon return, this contains the return code component of the result.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result value (or error message)
        /// component of the result.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this contains the error line component of the result.
        /// </param>
        /// <returns>
        /// True if the lock was acquired and the result data was retrieved;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method resets the result data of this logical event to its
        /// well-known initial state, acquiring the synchronization lock for the
        /// duration of the operation.
        /// </summary>
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

        /// <summary>
        /// This method resets the result data of this logical event to its
        /// well-known initial state, attempting to acquire the synchronization
        /// lock within the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the
        /// synchronization lock to be acquired.
        /// </param>
        /// <returns>
        /// True if the lock was acquired and the result data was reset;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method stores the specified result data on this logical event,
        /// acquiring the synchronization lock for the duration of the
        /// operation.
        /// </summary>
        /// <param name="returnCode">
        /// The return code component of the result.
        /// </param>
        /// <param name="result">
        /// The result value (or error message) component of the result.
        /// </param>
        /// <param name="errorLine">
        /// The error line component of the result.
        /// </param>
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

        /// <summary>
        /// This method stores the specified result data on this logical event,
        /// attempting to acquire the synchronization lock within the specified
        /// timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the
        /// synchronization lock to be acquired.
        /// </param>
        /// <param name="returnCode">
        /// The return code component of the result.
        /// </param>
        /// <param name="result">
        /// The result value (or error message) component of the result.
        /// </param>
        /// <param name="errorLine">
        /// The error line component of the result.
        /// </param>
        /// <returns>
        /// True if the lock was acquired and the result data was stored;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method opens the inter-thread communication event for this
        /// logical event and waits indefinitely for it to be signaled.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the event was successfully opened and signaled; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method opens the inter-thread communication event for this
        /// logical event and waits up to the specified timeout for it to be
        /// signaled.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the event
        /// to be signaled.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the event was successfully opened and signaled within the
        /// timeout; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method opens the inter-thread communication event for this
        /// logical event and resets it to the non-signaled state.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the event was successfully opened and reset; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method opens the inter-thread communication event for this
        /// logical event and sets it to the signaled state.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the event was successfully opened and set; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets the number of these object instances that have been created
        /// within this application domain.
        /// </summary>
        internal static int CreateCount
        {
            get
            {
                return Interlocked.CompareExchange(ref createCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of these object instances that have been disposed
        /// within this application domain.
        /// </summary>
        internal static int DisposeCount
        {
            get
            {
                return Interlocked.CompareExchange(ref disposeCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified event as having been dequeued.
        /// </summary>
        /// <param name="event">
        /// The event to mark.  This parameter may be null.
        /// </param>
        internal static void MarkDequeued(
            IEvent @event
            )
        {
            ModifyFlags(@event, EventFlags.WasDequeued, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified event as having been completed.
        /// </summary>
        /// <param name="event">
        /// The event to mark.  This parameter may be null.
        /// </param>
        internal static void MarkCompleted(
            IEvent @event
            )
        {
            ModifyFlags(@event, EventFlags.WasCompleted, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified event as having been both dequeued
        /// and canceled.
        /// </summary>
        /// <param name="event">
        /// The event to mark.  This parameter may be null.
        /// </param>
        internal static void MarkDequeuedAndCanceled(
            IEvent @event
            )
        {
            ModifyFlags(@event,
                EventFlags.WasDequeued | EventFlags.WasCanceled, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified event as having been both dequeued
        /// and discarded.
        /// </summary>
        /// <param name="event">
        /// The event to mark.  This parameter may be null.
        /// </param>
        internal static void MarkDequeuedAndDiscarded(
            IEvent @event
            )
        {
            ModifyFlags(@event,
                EventFlags.WasDequeued | EventFlags.WasDiscarded, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds or removes the specified flags on the specified
        /// event, if it is an instance of this class.
        /// </summary>
        /// <param name="event">
        /// The event whose flags are to be modified.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags to add or remove.
        /// </param>
        /// <param name="add">
        /// Non-zero to add the specified flags; zero to remove them.
        /// </param>
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

        /// <summary>
        /// This method disposes the specified event, but only if it is flagged
        /// as fire-and-forget.
        /// </summary>
        /// <param name="event">
        /// The event to conditionally dispose.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the event was disposed; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method disposes the specified event if it implements
        /// <see cref="IDisposable" />, swallowing any exception that is thrown
        /// during disposal.
        /// </summary>
        /// <param name="event">
        /// The event to dispose.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the event was disposed successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// The name of this logical event.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this logical event.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The identifier kind of this logical event.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this logical event.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The globally unique identifier of this logical event.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this logical event.
        /// </summary>
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this logical event.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this logical event.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The group of this logical event.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this logical event.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this logical event.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this logical event.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// The interpreter context associated with this logical event.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter context associated with this logical event.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronizeBase Members
        /// <summary>
        /// The object used to synchronize access to this logical event.
        /// </summary>
        private object syncRoot;
        /// <summary>
        /// Gets the object used to synchronize access to this logical event.
        /// </summary>
        public object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// logical event without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// successfully acquired by the calling thread.
        /// </param>
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

        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// logical event, waiting up to the configured wait-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// successfully acquired by the calling thread.
        /// </param>
        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// logical event without blocking and without throwing an exception if
        /// this logical event has been disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// successfully acquired by the calling thread.
        /// </param>
        public void TryLockNoThrow(
            ref bool locked
            )
        {
            // CheckDisposed(); /* EXEMPT */

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the synchronization lock for this
        /// logical event, waiting up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock to
        /// be acquired.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the lock was
        /// successfully acquired by the calling thread.
        /// </param>
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

        /// <summary>
        /// This method releases the synchronization lock for this logical
        /// event, if it is currently held by the calling thread.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the lock is held by the calling thread; upon
        /// return, this parameter will be zero once the lock has been released.
        /// </param>
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
        /// <summary>
        /// The delegate associated with this logical event, if any.
        /// </summary>
        private Delegate @delegate;
        /// <summary>
        /// Gets the delegate associated with this logical event, if any.
        /// </summary>
        public Delegate Delegate
        {
            get { CheckDisposed(); return @delegate; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type of this logical event.
        /// </summary>
        private EventType type;
        /// <summary>
        /// Gets the type of this logical event.
        /// </summary>
        public EventType Type
        {
            get { CheckDisposed(); return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling the behavior of this logical event.
        /// </summary>
        private EventFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling the behavior of this logical
        /// event.
        /// </summary>
        public EventFlags Flags
        {
            get { CheckDisposed(); return flags; }
            private set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The priority of this logical event.
        /// </summary>
        private EventPriority priority;
        /// <summary>
        /// Gets the priority of this logical event.
        /// </summary>
        public EventPriority Priority
        {
            get { CheckDisposed(); return priority; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The date and time at which this logical event is scheduled to be
        /// processed.
        /// </summary>
        private DateTime dateTime;
        /// <summary>
        /// Gets the date and time at which this logical event is scheduled to
        /// be processed.
        /// </summary>
        public DateTime DateTime
        {
            get { CheckDisposed(); return dateTime; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The callback to be invoked when this logical event is processed, if
        /// any.
        /// </summary>
        private EventCallback callback;
        /// <summary>
        /// Gets the callback to be invoked when this logical event is
        /// processed, if any.
        /// </summary>
        public EventCallback Callback
        {
            get { CheckDisposed(); return callback; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The identifier of the thread associated with this logical event, if
        /// any.
        /// </summary>
        private long? threadId;
        /// <summary>
        /// Gets the identifier of the thread associated with this logical
        /// event, if any.
        /// </summary>
        public long? ThreadId
        {
            get { CheckDisposed(); return threadId; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the result of this logical event, optionally
        /// waiting indefinitely for the event to be signaled beforehand.
        /// </summary>
        /// <param name="wait">
        /// Non-zero to wait for the event to be signaled before retrieving the
        /// result; otherwise, zero.
        /// </param>
        /// <param name="returnCode">
        /// Upon success, this contains the return code component of the result.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result value (or error message)
        /// component of the result.
        /// </param>
        /// <param name="errorLine">
        /// Upon success, this contains the error line component of the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was successfully retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the result of this logical event, optionally
        /// waiting up to the specified timeout for the event to be signaled
        /// beforehand.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the event
        /// to be signaled and for the synchronization lock to be acquired.
        /// </param>
        /// <param name="wait">
        /// Non-zero to wait for the event to be signaled before retrieving the
        /// result; otherwise, zero.
        /// </param>
        /// <param name="returnCode">
        /// Upon success, this contains the return code component of the result.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result value (or error message)
        /// component of the result.
        /// </param>
        /// <param name="errorLine">
        /// Upon success, this contains the error line component of the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was successfully retrieved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method resets the result of this logical event to its
        /// well-known initial state, optionally resetting the inter-thread
        /// communication event afterward.
        /// </summary>
        /// <param name="signal">
        /// Non-zero to reset the inter-thread communication event to the
        /// non-signaled state; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was successfully reset; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method resets the result of this logical event to its
        /// well-known initial state, attempting to acquire the synchronization
        /// lock within the specified timeout and optionally resetting the
        /// inter-thread communication event afterward.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the
        /// synchronization lock to be acquired.
        /// </param>
        /// <param name="signal">
        /// Non-zero to reset the inter-thread communication event to the
        /// non-signaled state; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was successfully reset; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method stores the specified result on this logical event,
        /// optionally signaling the inter-thread communication event afterward.
        /// </summary>
        /// <param name="signal">
        /// Non-zero to set the inter-thread communication event to the signaled
        /// state; otherwise, zero.
        /// </param>
        /// <param name="returnCode">
        /// The return code component of the result.
        /// </param>
        /// <param name="result">
        /// The result value (or error message) component of the result.
        /// </param>
        /// <param name="errorLine">
        /// The error line component of the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was successfully stored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method stores the specified result on this logical event,
        /// attempting to acquire the synchronization lock within the specified
        /// timeout and optionally signaling the inter-thread communication
        /// event afterward.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the
        /// synchronization lock to be acquired.
        /// </param>
        /// <param name="signal">
        /// Non-zero to set the inter-thread communication event to the signaled
        /// state; otherwise, zero.
        /// </param>
        /// <param name="returnCode">
        /// The return code component of the result.
        /// </param>
        /// <param name="result">
        /// The result value (or error message) component of the result.
        /// </param>
        /// <param name="errorLine">
        /// The error line component of the result.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the result was successfully stored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method produces a list of name/value pairs describing this
        /// logical event, suitable for diagnostic display.
        /// </summary>
        /// <returns>
        /// A <see cref="StringPairList" /> containing the details of this
        /// logical event.
        /// </returns>
        public StringPairList ToList()
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            list.Add("name", (name != null) ? name : _String.Null);

            list.Add("interpreter", (interpreter != null) ?
                interpreter.IdNoThrow.ToString() : _String.Null);

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
        /// <summary>
        /// This method returns the string form of this logical event, which is
        /// its name, or an empty string when it has no name.
        /// </summary>
        /// <returns>
        /// The string form of this logical event.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return (name != null) ? name : String.Empty;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this logical event has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this logical event has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this logical event has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(Event).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this logical event.  It
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
        /// <summary>
        /// This method releases all resources held by this logical event and
        /// suppresses finalization.
        /// </summary>
        public void Dispose() /* throw */
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this logical event, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Event()
        {
            Dispose(false);
        }
        #endregion
    }
}
