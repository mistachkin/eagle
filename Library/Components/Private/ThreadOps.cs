/*
 * ThreadOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.CompilerServices;

#if !NET_STANDARD_20
using System.Security.AccessControl;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using NamedEventWaitHandleDictionary =
    System.Collections.Generic.Dictionary<string,
        Eagle._Components.Private.ThreadOps.NamedEventWaitHandle>;

using ThreadStartTriplet = Eagle._Components.Public.AnyTriplet<
    System.Threading.ThreadStart, object, System.Threading.EventWaitHandle>;

using WaitCallbackTriplet = Eagle._Components.Public.AnyTriplet<
    System.Threading.WaitCallback, object, System.Threading.EventWaitHandle>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of static helper methods for creating,
    /// starting, and shutting down threads, for queuing work items to the
    /// thread pool, and for creating and manipulating event, wait handle, and
    /// semaphore synchronization primitives.  It also centralizes the default
    /// timeout values used throughout the library.
    /// </summary>
    [ObjectId("b81d425a-8049-4404-92c7-d106402b6bba")]
    internal static class ThreadOps
    {
        #region Private Cached Data
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// Non-zero if the current runtime is Mono.  This value is cached when
        /// this class is first used.
        /// </summary>
        private static bool isMono = CommonOps.Runtime.IsMono();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Thread Creation Data
        //
        // NOTE: These static fields are used to keep track of how many
        //       Thread objects have ever been created by this class and
        //       how many work items have been queued by this class.
        //
        /// <summary>
        /// The total number of threads that have ever been created by this
        /// class.
        /// </summary>
        private static long createCount;
        /// <summary>
        /// The number of threads created by this class that are currently
        /// active.
        /// </summary>
        private static long createActiveCount;

        /// <summary>
        /// The total number of work items that have ever been queued to the
        /// thread pool by this class.
        /// </summary>
        private static long queueCount;
        /// <summary>
        /// The number of work items queued by this class that are currently
        /// active.
        /// </summary>
        private static long queueActiveCount;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The total number of events that have ever been created by this
        /// class.
        /// </summary>
        private static long eventCount;
        /// <summary>
        /// The number of events created by this class that are currently
        /// active.
        /// </summary>
        private static long eventActiveCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private NamedEventWaitHandle Data
        /// <summary>
        /// The object used to synchronize access to the collection of named
        /// events and the associated counters.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, named events are used in place of the standard system
        /// event wait handles.  This defaults to non-zero on non-Windows
        /// operating systems.
        /// </summary>
        private static int useNamedEvents =
            !PlatformOps.IsWindowsOperatingSystem() ? 1 : 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The collection of named events that have been created by this class,
        /// keyed by name.
        /// </summary>
        private static NamedEventWaitHandleDictionary namedEvents;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private NamedEventWaitHandle Class
        /// <summary>
        /// This class represents an event wait handle that is tracked by name
        /// and reference counted by this class.  It is used to emulate named
        /// system events on platforms where they are not natively available.
        /// </summary>
        [ObjectId("1c85f028-0e75-4a06-85ce-b81ca169b33e")]
        internal sealed class NamedEventWaitHandle :
                EventWaitHandle, IIdentifierName
        {
            #region Private Data
            /// <summary>
            /// The number of outstanding references to this named event.
            /// </summary>
            private int referenceCount;
            /// <summary>
            /// The number of pending close operations for this named event.
            /// </summary>
            private int closeCount;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Constructs a new instance of this class with an automatically
            /// generated name.
            /// </summary>
            /// <param name="initialState">
            /// Non-zero if the event should be set initially; otherwise, the
            /// event is initially reset.
            /// </param>
            /// <param name="mode">
            /// The reset behavior (automatic or manual) for this event.
            /// </param>
            public NamedEventWaitHandle(
                bool initialState,  /* in */
                EventResetMode mode /* in */
                )
                : base(initialState, mode)
            {
                SetupName(null);
                AddReference();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Constructs a new instance of this class with the specified name.
            /// </summary>
            /// <param name="initialState">
            /// Non-zero if the event should be set initially; otherwise, the
            /// event is initially reset.
            /// </param>
            /// <param name="mode">
            /// The reset behavior (automatic or manual) for this event.
            /// </param>
            /// <param name="name">
            /// The name of the event, or null to use an automatically generated
            /// name.
            /// </param>
            public NamedEventWaitHandle(
                bool initialState,   /* in */
                EventResetMode mode, /* in */
                string name          /* in */
                )
                : base(initialState, mode, null)
            {
                SetupName(name);
                AddReference();
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Constructs a new instance of this class with the specified name,
            /// indicating whether the event was newly created.
            /// </summary>
            /// <param name="initialState">
            /// Non-zero if the event should be set initially; otherwise, the
            /// event is initially reset.
            /// </param>
            /// <param name="mode">
            /// The reset behavior (automatic or manual) for this event.
            /// </param>
            /// <param name="name">
            /// The name of the event, or null to use an automatically generated
            /// name.
            /// </param>
            /// <param name="createdNew">
            /// Upon return, this is non-zero if the event was created by this
            /// call; otherwise, an existing event was opened.
            /// </param>
            public NamedEventWaitHandle(
                bool initialState,   /* in */
                EventResetMode mode, /* in */
                string name,         /* in */
                out bool createdNew  /* out */
                )
                : base(initialState, mode, null, out createdNew)
            {
                SetupName(name);
                AddReference();
            }

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
            /// <summary>
            /// Constructs a new instance of this class with the specified name
            /// and access control security, indicating whether the event was
            /// newly created.
            /// </summary>
            /// <param name="initialState">
            /// Non-zero if the event should be set initially; otherwise, the
            /// event is initially reset.
            /// </param>
            /// <param name="mode">
            /// The reset behavior (automatic or manual) for this event.
            /// </param>
            /// <param name="name">
            /// The name of the event, or null to use an automatically generated
            /// name.
            /// </param>
            /// <param name="createdNew">
            /// Upon return, this is non-zero if the event was created by this
            /// call; otherwise, an existing event was opened.
            /// </param>
            /// <param name="eventSecurity">
            /// The access control security to apply to the event.
            /// </param>
            public NamedEventWaitHandle(
                bool initialState,                    /* in */
                EventResetMode mode,                  /* in */
                string name,                          /* in */
                out bool createdNew,                  /* out */
                EventWaitHandleSecurity eventSecurity /* in */
                )
                : base(initialState, mode, null, out createdNew, eventSecurity)
            {
                SetupName(name);
                AddReference();
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IIdentifierName Members
            /// <summary>
            /// The name associated with this named event.
            /// </summary>
            private string name;
            /// <summary>
            /// Gets the name associated with this named event; Setting this
            /// property is not supported and always throws
            /// <see cref="System.NotSupportedException" />.
            /// </summary>
            public string Name
            {
                get { CheckDisposed(); return name; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method determines whether this named event currently has
            /// more than one outstanding reference.
            /// </summary>
            /// <returns>
            /// True if this named event has more than one outstanding reference;
            /// otherwise, false.
            /// </returns>
            public bool HasMoreThanOneReference()
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref referenceCount, 0, 0) > 1;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method increments the outstanding reference count for this
            /// named event.
            /// </summary>
            /// <returns>
            /// The new reference count after the increment.
            /// </returns>
            public int AddReference()
            {
                CheckDisposed();

                return Interlocked.Increment(ref referenceCount);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method decrements the outstanding reference count for this
            /// named event.
            /// </summary>
            /// <returns>
            /// The new reference count after the decrement.
            /// </returns>
            public int RemoveReference()
            {
                CheckDisposed();

                return Interlocked.Decrement(ref referenceCount);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            /// <summary>
            /// This method verifies that this named event has a non-null name,
            /// throwing an exception if it does not.
            /// </summary>
            private void CheckName()
            {
                if (name == null)
                {
                    throw new InvalidOperationException(
                        "event name cannot be null");
                }
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method initializes the name of this named event, generating
            /// an automatic name when no name is supplied.
            /// </summary>
            /// <param name="name">
            /// The name of the event, or null to use an automatically generated
            /// name.
            /// </param>
            private void SetupName(
                string name /* in */
                )
            {
                this.name = (name != null) ? name : String.Format(
                    "{0}{1}{2}", GetType(), Characters.NumberSign,
                    RuntimeOps.GetHashCode(this));
            }

            ///////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether a close operation is currently
            /// pending for this named event.
            /// </summary>
            /// <returns>
            /// True if a close operation is pending; otherwise, false.
            /// </returns>
            private bool IsClosePending()
            {
                return Interlocked.CompareExchange(ref closeCount, 0, 0) > 0;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method marks that a close operation is pending for this
            /// named event.
            /// </summary>
            private void SetClosePending()
            {
                /* IGNORED */
                Interlocked.Increment(ref closeCount);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method clears the indication that a close operation is
            /// pending for this named event.
            /// </summary>
            private void UnsetClosePending()
            {
                /* IGNORED */
                Interlocked.Decrement(ref closeCount);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            /// <summary>
            /// This method produces a string representation of this named event.
            /// </summary>
            /// <returns>
            /// A string that combines the base wait handle representation with
            /// the name of this named event.
            /// </returns>
            public override string ToString()
            {
                CheckDisposed();
                CheckName();

                return StringList.MakeList(base.ToString(), name);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Threading.WaitHandle Overrides
            /// <summary>
            /// This method releases one reference to this named event, closing
            /// the underlying wait handle when the final reference is removed.
            /// </summary>
            public override void Close()
            {
                CheckDisposed();

                if (RemoveReference() <= 0)
                {
                    SetClosePending();

                    try
                    {
                        base.Close();
                    }
                    finally
                    {
                        UnsetClosePending();
                    }
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            /// <summary>
            /// Non-zero if this named event has been disposed.
            /// </summary>
            private bool disposed;
            /// <summary>
            /// This method throws an exception if this named event has already
            /// been disposed.
            /// </summary>
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                {
                    throw new ObjectDisposedException(
                        typeof(NamedEventWaitHandle).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method releases the resources used by this named event.
            /// </summary>
            /// <param name="disposing">
            /// Non-zero if this method is being called from the
            /// <see cref="System.IDisposable.Dispose" /> method; zero if it is
            /// being called from the finalizer.
            /// </param>
            protected override void Dispose(
                bool disposing /* in */
                )
            {
                if (IsClosePending() || (RemoveReference() <= 0))
                {
                    try
                    {
                        if (!disposed)
                        {
                            if (disposing)
                            {
                                ////////////////////////////////////
                                // dispose managed resources here...
                                ////////////////////////////////////

                                name = null;
                            }

                            //////////////////////////////////////
                            // release unmanaged resources here...
                            //////////////////////////////////////
                        }
                    }
                    finally
                    {
                        base.Dispose(disposing);

                        disposed = true;
                    }
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds diagnostic information about the threads and named
        /// events managed by this class to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the diagnostic information is added.  If this is
        /// null, no information is added.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control how much detail is included.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (useNamedEvents != 0))
                {
                    localList.Add("UseNamedEvents",
                        useNamedEvents.ToString());
                }

                if (empty || ((namedEvents != null) &&
                    (namedEvents.Count > 0)))
                {
                    localList.Add("NamesEvents",
                        (namedEvents != null) ?
                            namedEvents.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || (createCount != 0))
                    localList.Add("CreateCount", createCount.ToString());

                if (empty || (createActiveCount != 0))
                {
                    localList.Add("CreateActiveCount",
                        createActiveCount.ToString());
                }

                if (empty || (queueCount != 0))
                    localList.Add("QueueCount", queueCount.ToString());

                if (empty || (queueActiveCount != 0))
                {
                    localList.Add("QueueActiveCount",
                        queueActiveCount.ToString());
                }

                if (empty || (eventCount != 0))
                    localList.Add("EventCount", eventCount.ToString());

                if (empty || (eventActiveCount != 0))
                {
                    localList.Add("EventActiveCount",
                        eventActiveCount.ToString());
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Auxiliary Threads & Named Events");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private NamedEventWaitHandle Methods (.NET Standard)
        /// <summary>
        /// This method determines whether named events should be used in place
        /// of the standard system event wait handles.
        /// </summary>
        /// <returns>
        /// True if named events should be used; otherwise, false.
        /// </returns>
        private static bool ShouldUseNamedEvents()
        {
            if (Interlocked.CompareExchange(
                    ref useNamedEvents, 0, 0) > 0)
            {
                return true;
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.UseNamedEvents))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the use of named events.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the use of named events; zero to disable it.
        /// </param>
        private static void EnableOrDisableNamedEvents(
            bool enable /* in */
            )
        {
            /* IGNORED */
            Interlocked.Exchange(
                ref useNamedEvents, enable ? 1 : 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the collection used to track named events
        /// has been created.
        /// </summary>
        private static void InitializeNamedEvents()
        {
            //
            // NOTE: The lock statement used here should be redundant
            //       as all callers should already have the lock held.
            //
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (namedEvents == null)
                    namedEvents = new NamedEventWaitHandleDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to look up an existing named event by name,
        /// adding a reference to it when found.
        /// </summary>
        /// <param name="name">
        /// The name of the event to look up.
        /// </param>
        /// <param name="event">
        /// Upon success, this contains the named event that was found; upon
        /// failure, this is null.
        /// </param>
        /// <returns>
        /// True if a named event with the specified name was found; otherwise,
        /// false.
        /// </returns>
        private static bool TryGetNamedEventForOpen(
            string name,                    /* in */
            out NamedEventWaitHandle @event /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (name == null)
                {
                    @event = null;
                    return false;
                }

                InitializeNamedEvents();

                if (namedEvents.TryGetValue(name, out @event))
                {
                    if (@event != null)
                    {
                        /* IGNORED */
                        @event.AddReference();
                    }
                    else
                    {
                        //
                        // NOTE: This should not be possible.
                        //
                        DebugOps.MaybeBreak();
                    }

                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a newly created named event to the collection used
        /// to track named events.
        /// </summary>
        /// <param name="name">
        /// The name of the event being added.
        /// </param>
        /// <param name="event">
        /// The named event being added.
        /// </param>
        private static void AddNamedEventForCreate(
            string name,                /* in */
            NamedEventWaitHandle @event /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((name == null) || (@event == null))
                    return;

                InitializeNamedEvents();

                namedEvents.Add(name, @event);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes one reference to the specified named event,
        /// removing it from the collection and closing it when the final
        /// reference is released.
        /// </summary>
        /// <param name="event">
        /// The named event to remove and close.  Upon return, this is set to
        /// null when the event has been removed from the collection.
        /// </param>
        /// <returns>
        /// True if the named event was removed from the collection; otherwise,
        /// false.
        /// </returns>
        private static bool MaybeRemoveAndCloseNamedEvent(
            ref NamedEventWaitHandle @event /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (@event == null)
                    return false;

                if (@event.HasMoreThanOneReference())
                {
                    /* IGNORED */
                    @event.RemoveReference();

                    @event = null;
                    return false;
                }

                InitializeNamedEvents();

                string name = @event.Name;
                bool result = true;

                if ((name == null) || !namedEvents.Remove(name))
                    result = false;

                @event.Close();
                @event = null;

                Interlocked.Decrement(ref eventActiveCount);

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Timeout Constants
        //
        // NOTE: This is the default number of times to retry for a lock
        //       to be acquired.  This does NOT include the initial try.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default number of times to retry acquiring a lock.  This does
        /// not include the initial attempt.
        /// </summary>
        private static int defaultLockRetries = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default number of times to retry some kind of
        //       operation when a more specific value is not available.
        //       This does NOT include the initial try.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default number of times to retry an operation when a more
        /// specific value is not available.  This does not include the initial
        /// attempt.
        /// </summary>
        private static int defaultRetries = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default multiplier to apply to all timeouts
        //       and wait times.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default multiplier to apply to all timeouts and wait times, or
        /// null to compute the multiplier based on the current context.
        /// </summary>
        private static int? defaultMultiplier = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the multiplier used for "wait" locks to be acquired,
        //       when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The multiplier used for "wait" locks to be acquired, when running on
        /// Mono.
        /// </summary>
        private static int defaultMonoWaitLockMultiplier = 3;
#endif

        /// <summary>
        /// The multiplier used for "wait" locks to be acquired, when running on
        /// the .NET Framework.
        /// </summary>
        private static int defaultDotNetWaitLockMultiplier = 2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the multiplier used for timeouts when the waiting is
        //       being done from a thread that is NOT the PRIMARY THREAD for
        //       the target interpreter.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The multiplier used for timeouts when the waiting is being done from
        /// a thread that is not the primary thread for the target interpreter.
        /// </summary>
        private static int defaultBackgroundMultiplier = 5;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the multiplier used for "hard" locks to be acquired,
        //       when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The multiplier used for "hard" locks to be acquired, when running on
        /// Mono.
        /// </summary>
        private static int defaultMonoHardLockMultiplier = 5;
#endif

        /// <summary>
        /// The multiplier used for "hard" locks to be acquired, when running on
        /// the .NET Framework.
        /// </summary>
        private static int defaultDotNetHardLockMultiplier = 4;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the multiplier used for engine locks to be acquired,
        //       when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The multiplier used for engine locks to be acquired, when running on
        /// Mono.
        /// </summary>
        private static int defaultMonoEngineLockMultiplier = 1;
#endif

        /// <summary>
        /// The multiplier used for engine locks to be acquired, when running on
        /// the .NET Framework.
        /// </summary>
        private static int defaultDotNetEngineLockMultiplier = 1;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait until a lock can
        //       be acquired, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait until a lock can be acquired,
        /// when running on Mono.
        /// </summary>
        private static int defaultMonoLockTimeout = 2000;
#endif

        /// <summary>
        /// The number of milliseconds to wait until a lock can be acquired,
        /// when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetLockTimeout = 1000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default number of milliseconds to wait before
        //       a readiness operation will fail due to being unable to
        //       acquire the interpreter lock, when running on Mono or the
        //       .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait before a readiness operation will
        /// fail due to being unable to acquire the interpreter lock, when
        /// running on Mono.
        /// </summary>
        private static int defaultMonoReadyTimeout = 4000;
#endif

        /// <summary>
        /// The number of milliseconds to wait before a readiness operation will
        /// fail due to being unable to acquire the interpreter lock, when
        /// running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetReadyTimeout = 2000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds for the engine to wait
        //       until a lock can be acquired, when running on Mono or the
        //       .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds for the engine to wait until a lock can
        /// be acquired, when running on Mono.
        /// </summary>
        private static int defaultMonoEngineLockTimeout = 120000;
#endif

        /// <summary>
        /// The number of milliseconds for the engine to wait until a lock can
        /// be acquired, when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetEngineLockTimeout = 60000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds that an event has to
        //       be signaled, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds that an event has to be signaled, when
        /// running on Mono.
        /// </summary>
        private static int defaultMonoEventTimeout = 20000;
#endif

        /// <summary>
        /// The number of milliseconds that an event has to be signaled, when
        /// running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetEventTimeout = 10000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds that the health thread
        //       should wait between running checks, when running on Mono
        //       or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if THREADING
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds that the health thread should wait
        /// between running checks, when running on Mono.
        /// </summary>
        private static int defaultMonoHealthTimeout = 60000;
#endif

        /// <summary>
        /// The number of milliseconds that the health thread should wait
        /// between running checks, when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetHealthTimeout = 60000;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds that a script has to
        //       complete, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds that a script has to complete, when
        /// running on Mono.
        /// </summary>
        private static int defaultMonoScriptTimeout = 10000;
#endif

        /// <summary>
        /// The number of milliseconds that a script has to complete, when
        /// running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetScriptTimeout = 5000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the minimum number of milliseconds to wait after
        //       starting a thread, when running on Mono or the .NET
        //       Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The minimum number of milliseconds to wait after starting a thread,
        /// when running on Mono.
        /// </summary>
        private static int defaultMonoStartTimeout = 6000;
#endif

        /// <summary>
        /// The minimum number of milliseconds to wait after starting a thread,
        /// when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetStartTimeout = 3000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the minimum number of milliseconds to wait after
        //       interrupting a thread, when running on Mono or the .NET
        //       Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The minimum number of milliseconds to wait after interrupting a
        /// thread, when running on Mono.
        /// </summary>
        private static int defaultMonoInterruptTimeout = 1000;
#endif

        /// <summary>
        /// The minimum number of milliseconds to wait after interrupting a
        /// thread, when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetInterruptTimeout = 500;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait when joining a
        //       thread after interrupting or aborting it, etc, when running
        //       on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait when joining a thread after
        /// interrupting or aborting it, when running on Mono.
        /// </summary>
        private static int defaultMonoJoinTimeout = 6000;
#endif

        /// <summary>
        /// The number of milliseconds to wait when joining a thread after
        /// interrupting or aborting it, when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetJoinTimeout = 3000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait when waiting for
        //       a process to exit before processing events and trying again
        //       (possibly), when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait when waiting for a process to
        /// exit before processing events and trying again, when running on
        /// Mono.
        /// </summary>
        private static int defaultMonoExitTimeout =
            2 * EventManager.MinimumSleepTime;
#endif

        /// <summary>
        /// The number of milliseconds to wait when waiting for a process to
        /// exit before processing events and trying again, when running on the
        /// .NET Framework.
        /// </summary>
        private static int defaultDotNetExitTimeout =
            EventManager.MinimumSleepTime;

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        //
        // NOTE: This is the number of milliseconds to wait when contacting a
        //       network, etc, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait when contacting a network, when
        /// running on Mono.
        /// </summary>
        private static int defaultMonoNetworkTimeout = 40000;
#endif

        /// <summary>
        /// The number of milliseconds to wait when contacting a network, when
        /// running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetNetworkTimeout = 20000;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait when evaluating
        //       finally blocks for [try] in an "unsafe" interpreter, etc,
        //       when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait when evaluating finally blocks
        /// for [try] in an "unsafe" interpreter, when running on Mono.
        /// </summary>
        private static int defaultMonoUnsafeFinallyTimeout = _Timeout.Infinite;
#endif

        /// <summary>
        /// The number of milliseconds to wait when evaluating finally blocks
        /// for [try] in an "unsafe" interpreter, when running on the .NET
        /// Framework.
        /// </summary>
        private static int defaultDotNetUnsafeFinallyTimeout = _Timeout.Infinite;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait when evaluating
        //       finally blocks for [try] in a "safe" interpreter, etc, when
        //       running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait when evaluating finally blocks
        /// for [try] in a "safe" interpreter, when running on Mono.
        /// </summary>
        private static int defaultMonoSafeFinallyTimeout = 20000;
#endif

        /// <summary>
        /// The number of milliseconds to wait when evaluating finally blocks
        /// for [try] in a "safe" interpreter, when running on the .NET
        /// Framework.
        /// </summary>
        private static int defaultDotNetSafeFinallyTimeout = 10000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait when disposing a
        //       thread after interrupting or aborting it, etc, when running
        //       on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The number of milliseconds to wait when disposing a thread after
        /// interrupting or aborting it, when running on Mono.
        /// </summary>
        private static int defaultMonoDisposeTimeout = 3000;
#endif

        /// <summary>
        /// The number of milliseconds to wait when disposing a thread after
        /// interrupting or aborting it, when running on the .NET Framework.
        /// </summary>
        private static int defaultDotNetDisposeTimeout = 1500;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default number of milliseconds to wait when a
        //       more specific value is not available, when running on Mono
        //       or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The default number of milliseconds to wait when a more specific
        /// value is not available, when running on Mono.
        /// </summary>
        private static int defaultMonoFallbackTimeout = 0;
#endif

        /// <summary>
        /// The default number of milliseconds to wait when a more specific
        /// value is not available, when running on the .NET Framework.
        /// </summary>
        private static int defaultFallbackTimeout = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default number of milliseconds to wait when the
        //       specific timeout type is unknown (or unsupported) within the
        //       current context, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        /// <summary>
        /// The default number of milliseconds to wait when the specific timeout
        /// type is unknown or unsupported within the current context, when
        /// running on Mono.
        /// </summary>
        private static int defaultMonoUnknownTimeout = 0;
#endif

        /// <summary>
        /// The default number of milliseconds to wait when the specific timeout
        /// type is unknown or unsupported within the current context, when
        /// running on the .NET Framework.
        /// </summary>
        private static int defaultUnknownTimeout = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Timeout Constants
        //
        // NOTE: This is the number of milliseconds to wait when joining a
        //       thread after interrupting or aborting it, etc.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The number of milliseconds to wait when joining a thread after
        /// interrupting or aborting it.
        /// </summary>
        public static int DefaultJoinTimeout = GetDefaultJoinTimeout();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Timeout Helper Methods
        /// <summary>
        /// This method gets the multiplier used for "wait" locks to be
        /// acquired, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The multiplier used for "wait" locks.
        /// </returns>
        private static int GetDefaultWaitLockMultiplier()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoWaitLockMultiplier;
#endif

            return defaultDotNetWaitLockMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the multiplier used for "hard" locks to be
        /// acquired, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The multiplier used for "hard" locks.
        /// </returns>
        private static int GetDefaultHardLockMultiplier()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoHardLockMultiplier;
#endif

            return defaultDotNetHardLockMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the multiplier used for engine locks to be
        /// acquired, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The multiplier used for engine locks.
        /// </returns>
        private static int GetDefaultEngineLockMultiplier()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoEngineLockMultiplier;
#endif

            return defaultDotNetEngineLockMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the multiplier used for timeouts when the waiting
        /// is being done from a thread that is not the primary thread for the
        /// target interpreter.
        /// </summary>
        /// <returns>
        /// The multiplier used for background threads.
        /// </returns>
        private static int GetDefaultBackgroundMultiplier()
        {
            return defaultBackgroundMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait until a lock can
        /// be acquired, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The lock timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultLockTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoLockTimeout;
#endif

            return defaultDotNetLockTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait before a
        /// readiness operation will fail due to being unable to acquire the
        /// interpreter lock, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The readiness timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultReadyTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoReadyTimeout;
#endif

            return defaultDotNetReadyTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds for the engine to wait
        /// until a lock can be acquired, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The engine lock timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultEngineTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoEngineLockTimeout;
#endif

            return defaultDotNetEngineLockTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds that an event has to be
        /// signaled, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The event timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultEventTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoEventTimeout;
#endif

            return defaultDotNetEventTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

#if THREADING
        /// <summary>
        /// This method gets the number of milliseconds that the health thread
        /// should wait between running checks, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The health timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultHealthTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoHealthTimeout;
#endif

            return defaultDotNetHealthTimeout;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds that a script has to
        /// complete, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The script timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultScriptTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoScriptTimeout;
#endif

            return defaultDotNetScriptTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the minimum number of milliseconds to wait after
        /// starting a thread, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The start timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultStartTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoStartTimeout;
#endif

            return defaultDotNetStartTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the minimum number of milliseconds to wait after
        /// interrupting a thread, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The interrupt timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultInterruptTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoInterruptTimeout;
#endif

            return defaultDotNetInterruptTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait when joining a
        /// thread after interrupting or aborting it, based on the current
        /// runtime.
        /// </summary>
        /// <returns>
        /// The join timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultJoinTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoJoinTimeout;
#endif

            return defaultDotNetJoinTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait when waiting for
        /// a process to exit before processing events and trying again, based on
        /// the current runtime.
        /// </summary>
        /// <returns>
        /// The exit timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultExitTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoExitTimeout;
#endif

            return defaultDotNetExitTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// This method gets the number of milliseconds to wait when contacting
        /// a network, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The network timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultNetworkTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoNetworkTimeout;
#endif

            return defaultDotNetNetworkTimeout;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait when evaluating
        /// finally blocks for [try] in an "unsafe" interpreter, based on the
        /// current runtime.
        /// </summary>
        /// <returns>
        /// The "unsafe" finally timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultUnsafeFinallyTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoUnsafeFinallyTimeout;
#endif

            return defaultDotNetUnsafeFinallyTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait when evaluating
        /// finally blocks for [try] in a "safe" interpreter, based on the
        /// current runtime.
        /// </summary>
        /// <returns>
        /// The "safe" finally timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultSafeFinallyTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoSafeFinallyTimeout;
#endif

            return defaultDotNetSafeFinallyTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of milliseconds to wait when disposing a
        /// thread after interrupting or aborting it, based on the current
        /// runtime.
        /// </summary>
        /// <returns>
        /// The dispose timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultDisposeTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoDisposeTimeout;
#endif

            return defaultDotNetDisposeTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default number of milliseconds to wait when a
        /// more specific value is not available, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The fallback timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultFallbackTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoFallbackTimeout;
#endif

            return defaultFallbackTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default number of milliseconds to wait when the
        /// specific timeout type is unknown or unsupported within the current
        /// context, based on the current runtime.
        /// </summary>
        /// <returns>
        /// The unknown timeout, in milliseconds.
        /// </returns>
        private static int GetDefaultUnknownTimeout()
        {
#if MONO || MONO_HACKS
            if (isMono)
                return defaultMonoUnknownTimeout;
#endif

            return defaultUnknownTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the effective timeout to use, given an
        /// optional specific timeout value, falling back to the default timeout
        /// for the specified operation when necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which may be null.
        /// </param>
        /// <param name="timeout">
        /// The specific timeout value, or null to use the default timeout.  A
        /// negative value is replaced with the default unless infinite timeouts
        /// are allowed for the specified timeout type.
        /// </param>
        /// <param name="timeoutType">
        /// The type of operation the timeout applies to.
        /// </param>
        /// <returns>
        /// The effective timeout, in milliseconds.
        /// </returns>
        private static int GetEffectiveTimeout(
            Interpreter interpreter, /* in: OPTIONAL */
            int? timeout,            /* in */
            TimeoutType timeoutType  /* in */
            )
        {
            //
            // NOTE: Any positive timeout value is allowed.  Both null
            //       and negative values are replaced with the default
            //       value for the specified operation unless the flag
            //       is set to allow negative (infinite) values.
            //
            if (timeout != null)
            {
                int localTimeout = (int)timeout;

                if (localTimeout >= 0)
                {
                    return localTimeout;
                }
                else if (FlagOps.HasFlags(
                        timeoutType, TimeoutType.Infinite, true))
                {
                    return localTimeout;
                }
#if false
                else /* BUGBUG: Hot-path. */
                {
                    TraceOps.DebugTrace(
                        "GetEffectiveTimeout: refused infinite",
                        typeof(ThreadOps).Name,
                        TracePriority.ThreadDebug);
                }
#endif
            }

            return GetDefaultTimeout(interpreter, timeoutType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Timeout Helper Methods
        /// <summary>
        /// This method extracts the base timeout type, with all flag bits
        /// removed.
        /// </summary>
        /// <param name="timeoutType">
        /// The timeout type, possibly including flag bits.
        /// </param>
        /// <returns>
        /// The base timeout type, without any flag bits.
        /// </returns>
        public static TimeoutType BaseTimeoutType(
            TimeoutType timeoutType
            )
        {
            return timeoutType & ~TimeoutType.FlagsMask;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default number of times to retry an operation
        /// of the specified timeout type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which may be null.
        /// </param>
        /// <param name="timeoutType">
        /// The type of operation the retries apply to.
        /// </param>
        /// <returns>
        /// The default number of retries for the specified operation.
        /// </returns>
        public static int GetDefaultRetries(
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType  /* in */
            )
        {
            switch (TranslateTimeoutType(
                    interpreter, BaseTimeoutType(timeoutType)))
            {
                case TimeoutType.SoftLock:
                case TimeoutType.FirmLock:
                case TimeoutType.WaitLock:
                case TimeoutType.HardLock:
                case TimeoutType.EngineLock:
                    {
                        return defaultLockRetries;
                    }
                default:
                    {
                        TraceOps.DebugTrace(String.Format(
                            "GetDefaultRetries: unsupported type {0}",
                            timeoutType), typeof(ThreadOps).Name,
                            TracePriority.ThreadDebug);

                        break;
                    }
            }

            return defaultRetries;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default timeout, in milliseconds, for the
        /// specified operation, applying any applicable multipliers.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which may be null.
        /// </param>
        /// <param name="timeoutType">
        /// The type of operation the timeout applies to.
        /// </param>
        /// <returns>
        /// The default timeout, in milliseconds.
        /// </returns>
        public static int GetDefaultTimeout(
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType  /* in */
            )
        {
            TimeoutType localTimeoutType = TranslateTimeoutType(
                interpreter, BaseTimeoutType(timeoutType));

            int multiplier = 1;

            if (!FlagOps.HasFlags(
                    timeoutType, TimeoutType.NoMultiplier, true))
            {
                if (defaultMultiplier != null)
                {
                    multiplier = (int)defaultMultiplier;
                }
                else
                {
                    if ((interpreter != null) &&
                        !interpreter.IsPrimarySystemThread())
                    {
                        multiplier *= GetDefaultBackgroundMultiplier();
                    }

                    switch (localTimeoutType)
                    {
                        case TimeoutType.WaitLock:
                            {
                                multiplier *= GetDefaultWaitLockMultiplier();
                                break;
                            }
                        case TimeoutType.HardLock:
                            {
                                multiplier *= GetDefaultHardLockMultiplier();
                                break;
                            }
                        case TimeoutType.EngineLock:
                            {
                                multiplier *= GetDefaultEngineLockMultiplier();
                                break;
                            }
                    }
                }
            }

            int timeout;

            switch (localTimeoutType)
            {
                case TimeoutType.Fallback:
                    {
                        timeout = GetDefaultFallbackTimeout();
                        break;
                    }
                case TimeoutType.SoftLock:
                    {
                        timeout = 0; // TODO: No waiting, hard-coded.
                        break;
                    }
                case TimeoutType.FirmLock:
                    {
                        timeout = GetDefaultLockTimeout();
                        break;
                    }
                case TimeoutType.Ready:
                    {
                        timeout = GetDefaultReadyTimeout();
                        break;
                    }
                case TimeoutType.WaitLock:
                    {
                        timeout = GetDefaultLockTimeout();
                        break;
                    }
                case TimeoutType.HardLock:
                    {
                        timeout = GetDefaultLockTimeout();
                        break;
                    }
                case TimeoutType.EngineLock:
                    {
                        timeout = GetDefaultEngineTimeout();
                        break;
                    }
                case TimeoutType.Event:
                    {
                        timeout = GetDefaultEventTimeout();
                        break;
                    }
#if THREADING
                case TimeoutType.Health:
                    {
                        timeout = GetDefaultHealthTimeout();
                        break;
                    }
#endif
                case TimeoutType.Script:
                    {
                        timeout = GetDefaultScriptTimeout();
                        break;
                    }
                case TimeoutType.Start:
                    {
                        timeout = GetDefaultStartTimeout();
                        break;
                    }
                case TimeoutType.Interrupt:
                    {
                        timeout = GetDefaultInterruptTimeout();
                        break;
                    }
                case TimeoutType.Join:
                    {
                        timeout = GetDefaultJoinTimeout();
                        break;
                    }
                case TimeoutType.Exit:
                    {
                        timeout = GetDefaultExitTimeout();
                        break;
                    }
#if NETWORK
                case TimeoutType.Network:
                    {
                        timeout = GetDefaultNetworkTimeout();
                        break;
                    }
#endif
                case TimeoutType.UnsafeFinally:
                    {
                        timeout = GetDefaultUnsafeFinallyTimeout();
                        break;
                    }
                case TimeoutType.SafeFinally:
                    {
                        timeout = GetDefaultSafeFinallyTimeout();
                        break;
                    }
                case TimeoutType.Dispose:
                    {
                        timeout = GetDefaultDisposeTimeout();
                        break;
                    }
                case TimeoutType.Unknown:
                    {
                        timeout = GetDefaultUnknownTimeout();
                        break;
                    }
                default:
                    {
                        TraceOps.DebugTrace(String.Format(
                            "GetDefaultTimeout: unsupported type {0}",
                            timeoutType), typeof(ThreadOps).Name,
                            TracePriority.ThreadDebug);

                        timeout = GetDefaultUnknownTimeout();
                        break;
                    }
            }

            if (timeout > 0)
                timeout *= multiplier;

            return timeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified timeout type, substituting the
        /// configured default for any hard-coded timeout types that require it.
        /// </summary>
        /// <param name="timeoutType">
        /// The timeout type to translate.
        /// </param>
        /// <returns>
        /// The translated timeout type.
        /// </returns>
        private static TimeoutType TranslateTimeoutType(
            TimeoutType timeoutType  /* in */
            )
        {
            //
            // TODO: Update this switch if the list of hard-coded
            //       timeout types requiring translation changes.
            //
            switch (timeoutType)
            {
                case TimeoutType.Finally:
                    {
                        return TimeoutType.DefaultFinally;
                    }
                default:
                    {
                        return timeoutType;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates the specified timeout type, deferring to the
        /// interpreter for translation when one is available, and otherwise
        /// applying the hard-coded translation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to translate the timeout type, which may
        /// be null.
        /// </param>
        /// <param name="timeoutType">
        /// The timeout type to translate.
        /// </param>
        /// <returns>
        /// The translated timeout type.
        /// </returns>
        private static TimeoutType TranslateTimeoutType(
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType  /* in */
            )
        {
            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    //
                    // WARNING: Do not use any HardTryLock variants here due
                    //          to it (potentially) calling into this method
                    //          to obtain its lock timeout.
                    //
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        if (!interpreter.Disposed)
                        {
                            return interpreter.TranslateTimeoutType(
                                timeoutType);
                        }
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "TranslateTimeoutType",
                            typeof(ThreadOps).Name, false,
                            TracePriority.LockWarning2,
                            interpreter.MaybeWhoHasLock());
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return TranslateTimeoutType(timeoutType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the timeout to use for the specified operation,
        /// preferring the specific timeout, then the timeout configured for the
        /// interpreter, and finally the default timeout for the operation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which may be null.
        /// </param>
        /// <param name="timeout">
        /// The specific timeout value, or null to use the interpreter or default
        /// timeout.
        /// </param>
        /// <param name="timeoutType">
        /// The type of operation the timeout applies to.
        /// </param>
        /// <returns>
        /// The timeout, in milliseconds.
        /// </returns>
        public static int GetTimeout(
            Interpreter interpreter, /* in: OPTIONAL */
            int? timeout,            /* in */
            TimeoutType timeoutType  /* in */
            )
        {
            //
            // NOTE: Prefer the specific timeout, then the timeout configured
            //       for the interpreter, and finally the default timeout for
            //       the specified operation.
            //
            if (timeout != null)
            {
                return GetEffectiveTimeout(
                    interpreter, timeout, timeoutType);
            }
            else if (interpreter != null)
            {
                GetTimeoutCallback callback = null;
                int? localTimeout = null;
                bool locked = false;

                try
                {
                    //
                    // WARNING: Do not use any HardTryLock variants here due
                    //          to it (potentially) calling into this method
                    //          to obtain its lock timeout.
                    //
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        if (!interpreter.Disposed)
                        {
                            callback = interpreter.InternalGetTimeoutCallback;

                            localTimeout = interpreter.InternalGetTimeout(
                                timeoutType);
                        }
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "GetTimeout",
                            typeof(ThreadOps).Name, false,
                            TracePriority.LockWarning2,
                            interpreter.MaybeWhoHasLock());

                        if (!FlagOps.HasFlags(
                                timeoutType, TimeoutType.NoFailSafe, true))
                        {
                            localTimeout = interpreter.InternalFallbackTimeout;
                        }
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }

                if (callback != null)
                {
                    try
                    {
                        Result error = null;

                        if (callback(interpreter,
                                timeoutType, ref localTimeout,
                                ref error) != ReturnCode.Ok)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "GetTimeout: callback error = {0}",
                                FormatOps.WrapOrNull(error)),
                                typeof(ThreadOps).Name,
                                TracePriority.CallbackError);
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(ThreadOps).Name,
                            TracePriority.CallbackError);
                    }
                }

                return GetEffectiveTimeout(
                    interpreter, localTimeout, timeoutType);
            }

            return GetDefaultTimeout(interpreter, timeoutType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Thread Helper Methods
        /// <summary>
        /// This method determines whether the specified thread is currently
        /// alive.
        /// </summary>
        /// <param name="thread">
        /// The thread to check, which may be null.
        /// </param>
        /// <returns>
        /// True if the thread is non-null and alive; otherwise, false.
        /// </returns>
        public static bool IsAlive(
            Thread thread
            )
        {
            if (thread == null)
                return false;

            if (!thread.IsAlive)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified thread is the current
        /// thread.
        /// </summary>
        /// <param name="thread">
        /// The thread to check, which may be null.
        /// </param>
        /// <returns>
        /// True if the specified thread is the current thread; otherwise, false.
        /// </returns>
        public static bool IsCurrent(
            Thread thread
            )
        {
            if (thread == null)
                return false;

            Thread currentThread = Thread.CurrentThread;

            if (currentThread == null)
                return false;

            return Object.ReferenceEquals(thread, currentThread);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is a thread pool
        /// thread.
        /// </summary>
        /// <returns>
        /// True if the current thread is a thread pool thread; otherwise, false.
        /// </returns>
        public static bool IsCurrentPool()
        {
            Thread currentThread = Thread.CurrentThread;

            if (currentThread == null)
                return false;

            return currentThread.IsThreadPoolThread;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is in the
        /// single-threaded apartment (STA) state.
        /// </summary>
        /// <returns>
        /// True if the current thread is an STA thread; otherwise, false.
        /// </returns>
        public static bool IsStaThread()
        {
            Thread currentThread = Thread.CurrentThread;

            if (currentThread == null)
                return false;

            return (currentThread.GetApartmentState() == ApartmentState.STA);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and/or starts a thread to run the specified
        /// start delegate, optionally queuing a work item to the thread pool
        /// instead of creating a dedicated thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which may be null.
        /// </param>
        /// <param name="name">
        /// The name to assign to the thread, or null to derive a name from the
        /// start delegate.
        /// </param>
        /// <param name="start">
        /// The delegate that the thread or work item will execute.
        /// </param>
        /// <param name="parameter">
        /// The parameter to pass to the start delegate, which may be null.
        /// </param>
        /// <param name="useThreadPool">
        /// Non-zero to queue a work item to the thread pool instead of creating
        /// a dedicated thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for a newly created thread.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the thread will be used for user-interface purposes.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the active call stack should be associated with the
        /// thread.
        /// </param>
        /// <param name="thread">
        /// Upon return, this contains the thread that was created.  This must be
        /// null on entry when a dedicated thread is being created.
        /// </param>
        public static void CreateAndOrStart(
            Interpreter interpreter,        /* in, optional */
            string name,                    /* in */
            ParameterizedThreadStart start, /* in */
            object parameter,               /* in, optional */
            bool useThreadPool,             /* in */
            int maxStackSize,               /* in */
            bool userInterface,             /* in */
            bool isBackground,              /* in */
            bool useActiveStack,            /* in */
            ref Thread thread               /* in, out */
            ) /* throw */
        {
            //
            // NOTE: Obviously (?), a start delegate is required in
            //       order to create a new thread (or queue a work
            //       item to the thread pool).
            //
            if (start == null)
                throw new ArgumentNullException("start");

            //
            // NOTE: Does the caller want to use the thread pool?  If
            //       so, there will be no thread object created.
            //
            if (useThreadPool)
            {
                /* IGNORED */
                QueueUserWorkItem(
                    new WaitCallback(start), parameter, false);
            }
            else
            {
                //
                // NOTE: Attempt to figure out a reasonable name for
                //       the thread to be created.
                //
                string threadName = null;

                if (name != null)
                {
                    threadName = name;
                }
                else if (start != null)
                {
                    threadName = FormatOps.DelegateMethodName(
                        start, false, false);
                }

                //
                // NOTE: If the thread was already created, throw an
                //       exception, because this should not happen.
                //
                if (thread != null)
                {
                    throw new ScriptException(String.Format(
                        "thread {0} was already created",
                        FormatOps.WrapOrNull(threadName)));
                }

                //
                // NOTE: Next, create the thread using the engine.
                //
                thread = Engine.CreateThread(
                    interpreter, start, maxStackSize, userInterface,
                    isBackground, useActiveStack);

                if (thread != null)
                {
                    thread.Name = String.Format(
                        "{0}.CreateAndOrStart: {1}",
                        typeof(ThreadOps).Name,
                        FormatOps.WrapOrNull(threadName));

                    thread.Start(parameter);
                }
                else
                {
                    throw new ScriptException(String.Format(
                        "thread {0} could not be created",
                        FormatOps.WrapOrNull(threadName)));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to shut down the specified thread, optionally
        /// waiting for it, interrupting it, and aborting it, according to the
        /// specified flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which may be null.
        /// </param>
        /// <param name="timeout">
        /// The timeout to use when waiting for the thread to join, or null to
        /// use the default timeout.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the thread is shut down.
        /// </param>
        /// <param name="thread">
        /// The thread to shut down.  Upon return, this is set to null unless the
        /// flags request that it be preserved.
        /// </param>
        public static void MaybeShutdown(
            Interpreter interpreter, /* in: OPTIONAL */
            int? timeout,            /* in: OPTIONAL */
            ShutdownFlags flags,     /* in */
            ref Thread thread        /* in, out */
            ) /* throw */
        {
            try
            {
                //
                // NOTE: No thread, no problem.
                //
                if (thread == null)
                    return;

                //
                // NOTE: Dead thread, also no problem.
                //
                bool ignoreAlive = FlagOps.HasFlags(
                    flags, ShutdownFlags.IgnoreAlive, true);

                if (!ignoreAlive && !IsAlive(thread))
                {
                    //
                    // NOTE: Thread is confirmed dead, reset it.
                    //
                    thread = null;
                    return;
                }

                bool waitBefore = FlagOps.HasFlags(
                    flags, ShutdownFlags.WaitBefore, true);

                int localTimeout = waitBefore ? GetTimeout(
                    interpreter, timeout, TimeoutType.Join) : 0;

                if (waitBefore && thread.Join(localTimeout))
                {
                    //
                    // NOTE: Thread is confirmed joined, reset it.
                    //
                    thread = null;
                    return;
                }

                //
                // NOTE: Does the caller want to use interrupt (and
                //       maybe stronger methods) to shutdown thread?
                //
                if (!FlagOps.HasFlags(
                        flags, ShutdownFlags.Interrupt, true))
                {
                    return;
                }

                //
                // NOTE: Try to interrupt thread in a somewhat nice
                //       and graceful way.
                //
                thread.Interrupt(); /* throw */

                //
                // NOTE: There are two cases here: 1) the caller set
                //       the waitFirst parameter to true and we have
                //       already (possibly) waited that amount of
                //       time -OR- 2) the caller does not want to
                //       wait at all, but we still want to join the
                //       thread without waiting.  Either way, reset
                //       the timeout to zero.
                //
                bool waitAfter = FlagOps.HasFlags(
                    flags, ShutdownFlags.WaitAfter, true);

                localTimeout = waitAfter ? GetDefaultTimeout(
                    interpreter, TimeoutType.Interrupt) : 0;

                //
                // NOTE: Next, double check if the thread is still
                //       alive (maybe without really waiting).
                //
                if (waitAfter && thread.Join(localTimeout))
                {
                    //
                    // NOTE: Thread is confirmed joined, reset it.
                    //
                    thread = null;
                    return;
                }

                if (!ignoreAlive && !IsAlive(thread))
                {
                    //
                    // NOTE: Thread is confirmed dead, reset it.
                    //
                    thread = null;
                    return;
                }

                //
                // NOTE: Do not abort if caller does not want to.
                //
                if (FlagOps.HasFlags(
                        flags, ShutdownFlags.NoAbort, true))
                {
                    return;
                }

                /* BUGBUG: Leaks? */
                thread.Abort(); /* throw */

                //
                // NOTE: Maybe reset thread after abort.
                //
                if (FlagOps.HasFlags(
                        flags, ShutdownFlags.ResetAbort, true))
                {
                    thread = null;
                }
            }
            finally
            {
                if (!FlagOps.HasFlags(
                        flags, ShutdownFlags.NoReset, true))
                {
                    thread = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ThreadPool Helper Methods
        /// <summary>
        /// This method is the thread pool callback that unwraps the state for a
        /// queued <see cref="System.Threading.ThreadStart" /> delegate, signals
        /// the associated start event, and invokes the delegate.
        /// </summary>
        /// <param name="state">
        /// The state object, which is expected to be a triplet containing the
        /// start delegate and an optional start event.
        /// </param>
        private static void ThreadStartWrapper(
            object state /* in */
            ) /* System.Threading.WaitCallback */
        {
            ThreadStartTriplet anyTriplet = state as ThreadStartTriplet;

            if (anyTriplet == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "ThreadStartWrapper: cannot convert state to {0}",
                    MarshalOps.GetErrorTypeName(typeof(ThreadStartTriplet))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            EventWaitHandle @event = anyTriplet.Z;

            if (@event != null)
                SetEvent(@event);

            ThreadStart newCallback = anyTriplet.X;

            if (newCallback == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "ThreadStartWrapper: missing {0} delegate from {1}",
                    MarshalOps.GetErrorTypeName(typeof(ThreadStart)),
                    MarshalOps.GetErrorTypeName(typeof(ThreadStartTriplet))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            Interlocked.Increment(ref queueActiveCount);

            try
            {
                newCallback(); /* throw */
            }
            finally
            {
                Interlocked.Decrement(ref queueActiveCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the thread pool callback that unwraps the state for a
        /// queued <see cref="System.Threading.WaitCallback" /> delegate, signals
        /// the associated start event, and invokes the delegate with its state.
        /// </summary>
        /// <param name="state">
        /// The state object, which is expected to be a triplet containing the
        /// callback delegate, its state, and an optional start event.
        /// </param>
        private static void WaitCallbackWrapper(
            object state /* in */
            ) /* System.Threading.WaitCallback */
        {
            WaitCallbackTriplet anyTriplet = state as WaitCallbackTriplet;

            if (anyTriplet == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitCallbackWrapper: cannot convert state to {0}",
                    MarshalOps.GetErrorTypeName(typeof(WaitCallbackTriplet))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            EventWaitHandle @event = anyTriplet.Z;

            if (@event != null)
                SetEvent(@event);

            WaitCallback newCallback = anyTriplet.X;

            if (newCallback == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitCallbackWrapper: missing {0} delegate from {1}",
                    MarshalOps.GetErrorTypeName(typeof(WaitCallback)),
                    MarshalOps.GetErrorTypeName(typeof(WaitCallbackTriplet))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            object newState = anyTriplet.Y;

            Interlocked.Increment(ref queueActiveCount);

            try
            {
                newCallback(newState); /* throw */
            }
            finally
            {
                Interlocked.Decrement(ref queueActiveCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified start event to be signaled,
        /// tracing the outcome and the elapsed time.
        /// </summary>
        /// <param name="event">
        /// The start event to wait for.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait.
        /// </param>
        /// <param name="started">
        /// The time at which the queued work item was started, used to compute
        /// the elapsed time for tracing.
        /// </param>
        private static void WaitForStart(
            EventWaitHandle @event, /* in */
            int timeout,            /* in */
            DateTime started        /* in */
            )
        {
            DateTime stopped = TimeOps.GetUtcNow();

            if (WaitEvent(@event, timeout))
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitForStart: success: {0}",
                    FormatOps.TimeSpan(
                        stopped.Subtract(started),
                        true)), typeof(ThreadOps).Name,
                    TracePriority.ThreadDebug);
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitForStart: failure: {0}",
                    FormatOps.TimeSpan(
                        stopped.Subtract(started),
                        true)), typeof(ThreadOps).Name,
                    TracePriority.ThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the queue flags corresponding to the specified
        /// options.
        /// </summary>
        /// <param name="waitForStart">
        /// Non-zero if the caller wants to wait for the queued work item to
        /// start.
        /// </param>
        /// <returns>
        /// The queue flags corresponding to the specified options.
        /// </returns>
        public static QueueFlags GetQueueFlags(
            bool waitForStart /* in */
            )
        {
            QueueFlags result = QueueFlags.Default;

            if (waitForStart)
                result |= QueueFlags.WaitForStart;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified start delegate to the thread pool,
        /// optionally waiting for the work item to start.
        /// </summary>
        /// <param name="callBack">
        /// The start delegate to queue to the thread pool.
        /// </param>
        /// <param name="waitForStart">
        /// Non-zero to wait for the queued work item to start before returning.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            ThreadStart callBack, /* in */
            bool waitForStart     /* in */
            )
        {
            using (EventWaitHandle @event = waitForStart ?
                    CreateEvent(false) : null)
            {
                DateTime started = TimeOps.GetUtcNow();

                try
                {
                    Interlocked.Increment(ref queueCount);

                    return ThreadPool.QueueUserWorkItem(
                        new WaitCallback(ThreadStartWrapper),
                        new ThreadStartTriplet(callBack, null, @event));
                }
                finally
                {
                    if (waitForStart)
                    {
                        WaitForStart(
                            @event, GetDefaultJoinTimeout(), started);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback delegate to the thread
        /// pool, optionally waiting for the work item to start.
        /// </summary>
        /// <param name="callBack">
        /// The callback delegate to queue to the thread pool.
        /// </param>
        /// <param name="waitForStart">
        /// Non-zero to wait for the queued work item to start before returning.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            WaitCallback callBack, /* in */
            bool waitForStart      /* in */
            )
        {
            using (EventWaitHandle @event = waitForStart ?
                    CreateEvent(false) : null)
            {
                DateTime started = TimeOps.GetUtcNow();

                try
                {
                    Interlocked.Increment(ref queueCount);

                    return ThreadPool.QueueUserWorkItem(
                        new WaitCallback(WaitCallbackWrapper),
                        new WaitCallbackTriplet(callBack, null, @event));
                }
                finally
                {
                    if (waitForStart)
                    {
                        WaitForStart(
                            @event, GetDefaultJoinTimeout(), started);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback delegate and state to the
        /// thread pool, optionally waiting for the work item to start.
        /// </summary>
        /// <param name="callBack">
        /// The callback delegate to queue to the thread pool.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback delegate.
        /// </param>
        /// <param name="waitForStart">
        /// Non-zero to wait for the queued work item to start before returning.
        /// </param>
        /// <returns>
        /// True if the work item was successfully queued; otherwise, false.
        /// </returns>
        public static bool QueueUserWorkItem(
            WaitCallback callBack, /* in */
            object state,          /* in */
            bool waitForStart      /* in */
            )
        {
            using (EventWaitHandle @event = waitForStart ?
                    CreateEvent(false) : null)
            {
                DateTime started = TimeOps.GetUtcNow();

                try
                {
                    Interlocked.Increment(ref queueCount);

                    return ThreadPool.QueueUserWorkItem(
                        new WaitCallback(WaitCallbackWrapper),
                        new WaitCallbackTriplet(callBack, state, @event));
                }
                finally
                {
                    if (waitForStart)
                    {
                        WaitForStart(
                            @event, GetDefaultJoinTimeout(), started);
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WaitHandle Helper Methods
        /// <summary>
        /// This method gets the native operating system handle for the specified
        /// wait handle.
        /// </summary>
        /// <param name="waitHandle">
        /// The wait handle whose native handle is to be returned, which may be
        /// null.
        /// </param>
        /// <returns>
        /// The native handle for the wait handle, or
        /// <see cref="System.IntPtr.Zero" /> if it could not be obtained.
        /// </returns>
        public static IntPtr GetHandle(
            WaitHandle waitHandle /* in */
            )
        {
            if (waitHandle != null)
            {
                try
                {
                    return waitHandle.Handle;
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }
            }

            return IntPtr.Zero;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region EventWaitHandle Helper Methods
        /// <summary>
        /// This method maps a flag indicating automatic reset behavior to the
        /// corresponding event reset mode.
        /// </summary>
        /// <param name="automatic">
        /// Non-zero for an automatically resetting event; zero for a manually
        /// resetting event.
        /// </param>
        /// <returns>
        /// The event reset mode corresponding to the specified flag.
        /// </returns>
        private static EventResetMode GetEventResetMode(
            bool automatic /* in */
            )
        {
            return automatic ?
                EventResetMode.AutoReset : EventResetMode.ManualReset;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, unnamed event wait handle.
        /// </summary>
        /// <param name="automatic">
        /// Non-zero for an automatically resetting event; zero for a manually
        /// resetting event.
        /// </param>
        /// <returns>
        /// The newly created event wait handle.
        /// </returns>
        public static EventWaitHandle CreateEvent(
            bool automatic /* in */
            )
        {
            try
            {
                Interlocked.Increment(ref eventCount);
                Interlocked.Increment(ref eventActiveCount);

                return new EventWaitHandle(
                    false, GetEventResetMode(automatic));
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new, manually resetting event wait handle with
        /// the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the event to create.
        /// </param>
        /// <returns>
        /// The newly created event wait handle.
        /// </returns>
        public static EventWaitHandle CreateEvent(
            string name /* in */
            )
        {
            return CreateEvent(name, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new event wait handle with the specified name,
        /// using a named event when configured to do so.
        /// </summary>
        /// <param name="name">
        /// The name of the event to create.
        /// </param>
        /// <param name="automatic">
        /// Non-zero for an automatically resetting event; zero for a manually
        /// resetting event.
        /// </param>
        /// <returns>
        /// The newly created event wait handle.
        /// </returns>
        public static EventWaitHandle CreateEvent(
            string name,   /* in */
            bool automatic /* in */
            )
        {
            try
            {
                Interlocked.Increment(ref eventCount);
                Interlocked.Increment(ref eventActiveCount);

                if (ShouldUseNamedEvents())
                {
                    NamedEventWaitHandle @event = new NamedEventWaitHandle(
                        false, GetEventResetMode(automatic), name);

                    AddNamedEventForCreate(name, @event);

                    return @event;
                }
                else
                {
                    return new EventWaitHandle(
                        false, GetEventResetMode(automatic), name);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new event wait handle with the specified name
        /// and reset behavior, using a named event when configured to do so, and
        /// indicating whether the event was newly created.
        /// </summary>
        /// <param name="initialState">
        /// Non-zero if the event should be set initially; otherwise, the event
        /// is initially reset.
        /// </param>
        /// <param name="mode">
        /// The reset behavior (automatic or manual) for the event.
        /// </param>
        /// <param name="name">
        /// The name of the event to create.
        /// </param>
        /// <param name="createdNew">
        /// Upon return, this is non-zero if the event was created by this call;
        /// otherwise, an existing event was opened.
        /// </param>
        /// <returns>
        /// The newly created or opened event wait handle.
        /// </returns>
        public static EventWaitHandle CreateEvent(
            bool initialState,   /* in */
            EventResetMode mode, /* in */
            string name,         /* in */
            out bool createdNew  /* out */
            )
        {
            try
            {
                Interlocked.Increment(ref eventCount);
                Interlocked.Increment(ref eventActiveCount);

                if (ShouldUseNamedEvents())
                {
                    NamedEventWaitHandle @event = new NamedEventWaitHandle(
                        initialState, mode, name, out createdNew);

                    AddNamedEventForCreate(name, @event);

                    return @event;
                }
                else
                {
                    return new EventWaitHandle(
                        initialState, mode, name, out createdNew);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// This method creates a new event wait handle with the specified name,
        /// reset behavior, and access control security, using a named event when
        /// configured to do so, and indicating whether the event was newly
        /// created.
        /// </summary>
        /// <param name="initialState">
        /// Non-zero if the event should be set initially; otherwise, the event
        /// is initially reset.
        /// </param>
        /// <param name="mode">
        /// The reset behavior (automatic or manual) for the event.
        /// </param>
        /// <param name="name">
        /// The name of the event to create.
        /// </param>
        /// <param name="createdNew">
        /// Upon return, this is non-zero if the event was created by this call;
        /// otherwise, an existing event was opened.
        /// </param>
        /// <param name="eventSecurity">
        /// The access control security to apply to the event.
        /// </param>
        /// <returns>
        /// The newly created or opened event wait handle.
        /// </returns>
        public static EventWaitHandle CreateEvent(
            bool initialState,                    /* in */
            EventResetMode mode,                  /* in */
            string name,                          /* in */
            out bool createdNew,                  /* out */
            EventWaitHandleSecurity eventSecurity /* in */
            )
        {
            try
            {
                Interlocked.Increment(ref eventCount);
                Interlocked.Increment(ref eventActiveCount);

                if (ShouldUseNamedEvents())
                {
                    NamedEventWaitHandle @event = new NamedEventWaitHandle(
                        initialState, mode, name, out createdNew,
                        eventSecurity);

                    AddNamedEventForCreate(name, @event);

                    return @event;
                }
                else
                {
                    return new EventWaitHandle(
                        initialState, mode, name, out createdNew,
                        eventSecurity);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method opens an existing event wait handle with the specified
        /// name, using a named event when configured to do so.
        /// </summary>
        /// <param name="name">
        /// The name of the event to open.
        /// </param>
        /// <returns>
        /// The opened event wait handle, or null if it could not be opened.
        /// </returns>
        public static EventWaitHandle OpenEvent(
            string name /* in */
            )
        {
            EventWaitHandle @event = null;

            try
            {
                if (ShouldUseNamedEvents())
                {
                    NamedEventWaitHandle namedEvent;

                    if (TryGetNamedEventForOpen(name, out namedEvent))
                        @event = namedEvent;
                }
                else
                {
                    @event = EventWaitHandle.OpenExisting(name);
                }
            }
            catch (WaitHandleCannotBeOpenedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError2);

#if DEBUG
                DebugOps.MaybeBreak();
#endif
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

#if (DEBUG || FORCE_TRACE) && VERBOSE
            TraceOps.DebugTrace(String.Format(
                "OpenEvent: {0}, name = {1}",
                (@event != null) ? "success" : "failure",
                FormatOps.WrapOrNull(name)), typeof(ThreadOps).Name,
                TracePriority.EventDebug);
#endif

            return @event;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the specified event wait handle, releasing a
        /// reference to a named event when applicable.
        /// </summary>
        /// <param name="event">
        /// The event wait handle to close.  Upon return, this is set to null
        /// when the event has been closed.
        /// </param>
        public static void CloseEvent(
            ref EventWaitHandle @event /* in, out */
            )
        {
            try
            {
                if (@event != null)
                {
                    NamedEventWaitHandle namedEvent =
                        @event as NamedEventWaitHandle;

                    if (namedEvent != null)
                    {
                        /* IGNORED */
                        MaybeRemoveAndCloseNamedEvent(ref namedEvent);

                        if (namedEvent == null)
                            @event = null;
                    }
                    else
                    {
                        @event.Close();
                        @event = null;

                        Interlocked.Decrement(ref eventActiveCount);
                    }
                }
                else
                {
                    TraceOps.DebugTrace(
                        "CloseEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the specified event wait handle to the
        /// non-signaled state.
        /// </summary>
        /// <param name="event">
        /// The event wait handle to reset.
        /// </param>
        /// <returns>
        /// True if the event was successfully reset; otherwise, false.
        /// </returns>
        public static bool ResetEvent(
            EventWaitHandle @event /* in */
            )
        {
            try
            {
                if (@event != null)
                {
                    return @event.Reset();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "ResetEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the specified event wait handle to the signaled
        /// state.
        /// </summary>
        /// <param name="event">
        /// The event wait handle to set.
        /// </param>
        /// <returns>
        /// True if the event was successfully set; otherwise, false.
        /// </returns>
        public static bool SetEvent(
            EventWaitHandle @event /* in */
            )
        {
            try
            {
                if (@event != null)
                {
                    return @event.Set();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "SetEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits indefinitely for the specified event wait handle to
        /// be signaled.
        /// </summary>
        /// <param name="event">
        /// The event wait handle to wait for.
        /// </param>
        /// <returns>
        /// True if the event was signaled; otherwise, false.
        /// </returns>
        public static bool WaitEvent(
            EventWaitHandle @event /* in */
            )
        {
            try
            {
                if (@event != null)
                {
                    return @event.WaitOne();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified event wait handle to be signaled,
        /// up to the specified timeout.
        /// </summary>
        /// <param name="event">
        /// The event wait handle to wait for.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait.
        /// </param>
        /// <returns>
        /// True if the event was signaled before the timeout elapsed; otherwise,
        /// false.
        /// </returns>
        public static bool WaitEvent(
            EventWaitHandle @event, /* in */
            int timeout             /* in */
            )
        {
            try
            {
                if (@event != null)
                {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40 || NET_STANDARD_20)
                    return @event.WaitOne(timeout);
#else
                    return @event.WaitOne(timeout, false);
#endif
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified event wait handle to be signaled,
        /// up to the specified timeout, re-throwing any exception that occurs
        /// while waiting.
        /// </summary>
        /// <param name="event">
        /// The event wait handle to wait for.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait.
        /// </param>
        /// <returns>
        /// True if the event was signaled before the timeout elapsed; otherwise,
        /// false.
        /// </returns>
        public static bool WaitEventOrThrow(
            EventWaitHandle @event, /* in */
            int timeout             /* in */
            )
        {
            try
            {
                if (@event != null)
                {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40 || NET_STANDARD_20)
                    return @event.WaitOne(timeout);
#else
                    return @event.WaitOne(timeout, false);
#endif
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitEventOrThrow: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for any one of the specified event wait handles to
        /// be signaled, up to the specified timeout.
        /// </summary>
        /// <param name="events">
        /// The array of event wait handles to wait for.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait.
        /// </param>
        /// <returns>
        /// The index of the event that was signaled, or
        /// <see cref="System.Threading.WaitHandle.WaitTimeout" /> if the timeout
        /// elapsed.
        /// </returns>
        public static int WaitAnyEvent(
            EventWaitHandle[] events, /* in */
            int timeout               /* in */
            )
        {
            try
            {
                if (events != null)
                {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40 || NET_STANDARD_20)
                    return EventWaitHandle.WaitAny(events, timeout);
#else
                    return EventWaitHandle.WaitAny(events, timeout, false);
#endif
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitAnyEvent: invalid event",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return WaitHandle.WaitTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified wait result index
        /// indicates that the wait failed.
        /// </summary>
        /// <param name="index">
        /// The wait result index to examine.
        /// </param>
        /// <returns>
        /// True if the index indicates that the wait failed; otherwise, false.
        /// </returns>
        public static bool WasAnyWaitFailed(
            int index /* in */
            )
        {
            if (index == WaitResult.Failed)
                return true;

#if MONO || MONO_HACKS
            if (index == WaitResult.MonoFailed)
                return true;
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified wait result index
        /// indicates that the wait timed out.
        /// </summary>
        /// <param name="index">
        /// The wait result index to examine.
        /// </param>
        /// <returns>
        /// True if the index indicates that the wait timed out; otherwise,
        /// false.
        /// </returns>
        public static bool WasAnyEventTimeout(
            int index /* in */
            )
        {
            if (index == WaitHandle.WaitTimeout)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified wait result index
        /// indicates that an event was signaled.
        /// </summary>
        /// <param name="index">
        /// The wait result index to examine.
        /// </param>
        /// <returns>
        /// True if the index indicates that an event was signaled; otherwise,
        /// false.
        /// </returns>
        public static bool WasAnyEventSignaled(
            int index /* in */
            )
        {
            if ((index != WaitHandle.WaitTimeout) &&
#if MONO || MONO_HACKS
                //
                // HACK: Mono can return WAIT_IO_COMPLETION as the index and
                //       we cannot handle that, see:
                //
                //       https://bugzilla.novell.com/show_bug.cgi?id=549807
                //
                (index != WaitResult.IoCompletion) &&
                //
                // HACK: Mono can return the value 0x7FFFFFFF for WAIT_FAILED
                //       and we cannot handle that.
                //
                (index != WaitResult.MonoFailed)
#else
                true
#endif
                )
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method signals one event wait handle and waits for another, up
        /// to the specified timeout, performing the operation on a worker thread
        /// when necessary to avoid restrictions on STA threads.
        /// </summary>
        /// <param name="signalEvent">
        /// The event wait handle to signal.
        /// </param>
        /// <param name="waitEvent">
        /// The event wait handle to wait for.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait.
        /// </param>
        /// <param name="noStaThread">
        /// Non-zero to perform the operation on the current thread even when it
        /// is an STA thread.
        /// </param>
        /// <returns>
        /// True if the wait event was signaled before the timeout elapsed;
        /// otherwise, false.
        /// </returns>
        public static bool SignalAndWaitEvents(
            EventWaitHandle signalEvent, /* in */
            EventWaitHandle waitEvent,   /* in */
            int timeout,                 /* in */
            bool noStaThread             /* in */
            )
        {
            try
            {
                if (!noStaThread && IsStaThread())
                {
                    //
                    // HACK: We really need this method to work, even for
                    //       STA threads since we do not necessarily have
                    //       control over our execution environment).
                    //
                    bool result = false;

                    Thread thread = new Thread(delegate()
                    {
                        Interlocked.Increment(ref createActiveCount);

                        try
                        {
                            result = EventWaitHandle.SignalAndWait(
                                signalEvent, waitEvent, timeout, false);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref createActiveCount);
                        }
                    });

                    Interlocked.Increment(ref createCount);

                    thread.Name = String.Format(
                        "{0}.SignalAndWaitEvents: {1}, {2}, {3}",
                        typeof(ThreadOps).Name,
                        FormatOps.WrapHashCode(signalEvent),
                        FormatOps.WrapHashCode(waitEvent),
                        timeout); /* throw */

                    thread.Start(); /* throw */
                    thread.Join(); /* throw */

                    return result;
                }
                else
                {
                    return EventWaitHandle.SignalAndWait(
                        signalEvent, waitEvent, timeout, false);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Semaphore Helper Methods
        /// <summary>
        /// This method creates a new semaphore with the specified initial and
        /// maximum counts.
        /// </summary>
        /// <param name="initialCount">
        /// The initial number of requests for the semaphore that can be granted
        /// concurrently.
        /// </param>
        /// <param name="maximumCount">
        /// The maximum number of requests for the semaphore that can be granted
        /// concurrently.
        /// </param>
        /// <returns>
        /// The newly created semaphore.
        /// </returns>
        public static Semaphore CreateSemaphore(
            int initialCount, /* in */
            int maximumCount  /* in */
            )
        {
            try
            {
                return new Semaphore(initialCount, maximumCount);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);

                throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes the specified semaphore.
        /// </summary>
        /// <param name="semaphore">
        /// The semaphore to close.  Upon return, this is set to null when the
        /// semaphore has been closed.
        /// </param>
        public static void CloseSemaphore(
            ref Semaphore semaphore /* in, out */
            )
        {
            try
            {
                if (semaphore != null)
                {
                    semaphore.Close();
                    semaphore = null;
                }
                else
                {
                    TraceOps.DebugTrace(
                        "CloseSemaphore: invalid semaphore",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the specified semaphore once.
        /// </summary>
        /// <param name="semaphore">
        /// The semaphore to release.
        /// </param>
        /// <returns>
        /// The previous count of the semaphore, or
        /// <see cref="Count.Invalid" /> if the semaphore could not be released.
        /// </returns>
        public static int ReleaseSemaphore(
            Semaphore semaphore /* in */
            )
        {
            try
            {
                if (semaphore != null)
                {
                    return semaphore.Release();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "ReleaseSemaphore: invalid semaphore",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return Count.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits indefinitely for the specified semaphore to be
        /// available.
        /// </summary>
        /// <param name="semaphore">
        /// The semaphore to wait for.
        /// </param>
        /// <returns>
        /// True if the semaphore was entered; otherwise, false.
        /// </returns>
        public static bool WaitSemaphore(
            Semaphore semaphore /* in */
            )
        {
            try
            {
                if (semaphore != null)
                {
                    return semaphore.WaitOne();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitSemaphore: invalid semaphore",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified semaphore to be available, up to
        /// the specified timeout.
        /// </summary>
        /// <param name="semaphore">
        /// The semaphore to wait for.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait.
        /// </param>
        /// <returns>
        /// True if the semaphore was entered before the timeout elapsed;
        /// otherwise, false.
        /// </returns>
        public static bool WaitSemaphore(
            Semaphore semaphore, /* in */
            int timeout          /* in */
            )
        {
            try
            {
                if (semaphore != null)
                {
#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40 || NET_STANDARD_20)
                    return semaphore.WaitOne(timeout);
#else
                    return semaphore.WaitOne(timeout, false);
#endif
                }
                else
                {
                    TraceOps.DebugTrace(
                        "WaitSemaphore: invalid semaphore",
                        typeof(ThreadOps).Name,
                        TracePriority.HandleError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ThreadOps).Name,
                    TracePriority.HandleError);
            }

            return false;
        }
        #endregion
    }
}
