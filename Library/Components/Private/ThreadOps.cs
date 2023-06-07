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

using ThreadStartPair =
    Eagle._Components.Public.AnyPair<
        System.Threading.ThreadStart, object>;

using WaitCallbackPair =
    Eagle._Components.Public.AnyPair<
        System.Threading.WaitCallback, object>;

namespace Eagle._Components.Private
{
    [ObjectId("b81d425a-8049-4404-92c7-d106402b6bba")]
    internal static class ThreadOps
    {
        #region Private Thread Creation Data
        //
        // NOTE: These static fields are used to keep track of how many
        //       Thread objects have ever been created by this class and
        //       how many work items have been queued by this class.
        //
        private static int createCount;
        private static int createActiveCount;

        private static int queueCount;
        private static int queueActiveCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private NamedEventWaitHandle Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static int useNamedEvents =
            !PlatformOps.IsWindowsOperatingSystem() ? 1 : 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static NamedEventWaitHandleDictionary namedEvents;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private NamedEventWaitHandle Class
        [ObjectId("1c85f028-0e75-4a06-85ce-b81ca169b33e")]
        internal sealed class NamedEventWaitHandle :
                EventWaitHandle, IIdentifierName
        {
            #region Private Data
            private int referenceCount;
            private int closeCount;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
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
            private string name;
            public string Name
            {
                get { CheckDisposed(); return name; }
                set { CheckDisposed(); throw new NotSupportedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            public bool HasMoreThanOneReference()
            {
                CheckDisposed();

                return Interlocked.CompareExchange(
                    ref referenceCount, 0, 0) > 1;
            }

            ///////////////////////////////////////////////////////////////////

            public int AddReference()
            {
                CheckDisposed();

                return Interlocked.Increment(ref referenceCount);
            }

            ///////////////////////////////////////////////////////////////////

            public int RemoveReference()
            {
                CheckDisposed();

                return Interlocked.Decrement(ref referenceCount);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            private void CheckName()
            {
                if (name == null)
                {
                    throw new InvalidOperationException(
                        "event name cannot be null");
                }
            }

            ///////////////////////////////////////////////////////////////////////

            private void SetupName(
                string name /* in */
                )
            {
                this.name = (name != null) ? name : String.Format(
                    "{0}{1}{2}", GetType(), Characters.NumberSign,
                    RuntimeOps.GetHashCode(this));
            }

            ///////////////////////////////////////////////////////////////////////

            private bool IsClosePending()
            {
                return Interlocked.CompareExchange(ref closeCount, 0, 0) > 0;
            }

            ///////////////////////////////////////////////////////////////////

            private void SetClosePending()
            {
                /* IGNORED */
                Interlocked.Increment(ref closeCount);
            }

            ///////////////////////////////////////////////////////////////////

            private void UnsetClosePending()
            {
                /* IGNORED */
                Interlocked.Decrement(ref closeCount);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                CheckDisposed();
                CheckName();

                return StringList.MakeList(base.ToString(), name);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Threading.WaitHandle Overrides
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
            private bool disposed;
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

        private static void EnableOrDisableNamedEvents(
            bool enable /* in */
            )
        {
            /* IGNORED */
            Interlocked.Exchange(
                ref useNamedEvents, enable ? 1 : 0);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static int defaultLockRetries = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default number of times to retry some kind of
        //       operation when a more specific value is not available.
        //       This does NOT include the initial try.
        //
        // HACK: This is purposely not read-only.
        //
        private static int defaultRetries = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the multiplier used for "wait" locks to be acquired,
        //       when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        private static int defaultMonoWaitLockMultiplier = 3;
#endif

        private static int defaultDotNetWaitLockMultiplier = 2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the multiplier used for "hard" locks to be acquired,
        //       when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        private static int defaultMonoHardLockMultiplier = 5;
#endif

        private static int defaultDotNetHardLockMultiplier = 4;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to wait until a lock can
        //       be acquired, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        private static int defaultMonoLockTimeout = 2000;
#endif

        private static int defaultDotNetLockTimeout = 1000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds that an event has to
        //       be signaled, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        private static int defaultMonoEventTimeout = 20000;
#endif

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
        private static int defaultMonoHealthTimeout = 60000;
#endif

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
        private static int defaultMonoScriptTimeout = 10000;
#endif

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
        private static int defaultMonoStartTimeout = 6000;
#endif

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
        private static int defaultMonoInterruptTimeout = 1000;
#endif

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
        private static int defaultMonoJoinTimeout = 6000;
#endif

        private static int defaultDotNetJoinTimeout = 3000;

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        //
        // NOTE: This is the number of milliseconds to wait when contacting a
        //       network, etc, when running on Mono or the .NET Framework.
        //
        // HACK: These are purposely not read-only.
        //
#if MONO || MONO_HACKS
        private static int defaultMonoNetworkTimeout = 10000;
#endif

        private static int defaultDotNetNetworkTimeout = 5000;
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
        private static int defaultMonoUnsafeFinallyTimeout = _Timeout.Infinite;
#endif

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
        private static int defaultMonoSafeFinallyTimeout = 20000;
#endif

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
        private static int defaultMonoDisposeTimeout = 3000;
#endif

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
        private static int defaultMonoFallbackTimeout = 0;
#endif

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
        private static int defaultMonoUnknownTimeout = 0;
#endif

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
        public static int DefaultJoinTimeout = GetDefaultJoinTimeout();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Timeout Helper Methods
        private static int GetDefaultWaitLockMultiplier()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoWaitLockMultiplier;
#endif

            return defaultDotNetWaitLockMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultHardLockMultiplier()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoHardLockMultiplier;
#endif

            return defaultDotNetHardLockMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultLockTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoLockTimeout;
#endif

            return defaultDotNetLockTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultEventTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoEventTimeout;
#endif

            return defaultDotNetEventTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

#if THREADING
        private static int GetDefaultHealthTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoHealthTimeout;
#endif

            return defaultDotNetHealthTimeout;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultScriptTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoScriptTimeout;
#endif

            return defaultDotNetScriptTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultStartTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoStartTimeout;
#endif

            return defaultDotNetStartTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultInterruptTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoInterruptTimeout;
#endif

            return defaultDotNetInterruptTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultJoinTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoJoinTimeout;
#endif

            return defaultDotNetJoinTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        private static int GetDefaultNetworkTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoNetworkTimeout;
#endif

            return defaultDotNetNetworkTimeout;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultUnsafeFinallyTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoUnsafeFinallyTimeout;
#endif

            return defaultDotNetUnsafeFinallyTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultSafeFinallyTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoSafeFinallyTimeout;
#endif

            return defaultDotNetSafeFinallyTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultDisposeTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoDisposeTimeout;
#endif

            return defaultDotNetDisposeTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultFallbackTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoFallbackTimeout;
#endif

            return defaultFallbackTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetDefaultUnknownTimeout()
        {
#if MONO || MONO_HACKS
            if (CommonOps.Runtime.IsMono())
                return defaultMonoUnknownTimeout;
#endif

            return defaultUnknownTimeout;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static TimeoutType BaseTimeoutType(
            TimeoutType timeoutType
            )
        {
            return timeoutType & ~TimeoutType.FlagsMask;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static int GetDefaultTimeout(
            Interpreter interpreter, /* in: OPTIONAL */
            TimeoutType timeoutType  /* in */
            )
        {
            switch (TranslateTimeoutType(
                    interpreter, BaseTimeoutType(timeoutType)))
            {
                case TimeoutType.Fallback:
                    {
                        return GetDefaultFallbackTimeout();
                    }
                case TimeoutType.SoftLock:
                    {
                        return 0; // TODO: No waiting, hard-coded.
                    }
                case TimeoutType.FirmLock:
                    {
                        return GetDefaultLockTimeout();
                    }
                case TimeoutType.WaitLock:
                    {
                        return GetDefaultWaitLockMultiplier() *
                            GetDefaultLockTimeout();
                    }
                case TimeoutType.HardLock:
                    {
                        return GetDefaultHardLockMultiplier() *
                            GetDefaultLockTimeout();
                    }
                case TimeoutType.Event:
                    {
                        return GetDefaultEventTimeout();
                    }
#if THREADING
                case TimeoutType.Health:
                    {
                        return GetDefaultHealthTimeout();
                    }
#endif
                case TimeoutType.Script:
                    {
                        return GetDefaultScriptTimeout();
                    }
                case TimeoutType.Start:
                    {
                        return GetDefaultStartTimeout();
                    }
                case TimeoutType.Interrupt:
                    {
                        return GetDefaultInterruptTimeout();
                    }
                case TimeoutType.Join:
                    {
                        return GetDefaultJoinTimeout();
                    }
#if NETWORK
                case TimeoutType.Network:
                    {
                        return GetDefaultNetworkTimeout();
                    }
#endif
                case TimeoutType.UnsafeFinally:
                    {
                        return GetDefaultUnsafeFinallyTimeout();
                    }
                case TimeoutType.SafeFinally:
                    {
                        return GetDefaultSafeFinallyTimeout();
                    }
                case TimeoutType.Dispose:
                    {
                        return GetDefaultDisposeTimeout();
                    }
                case TimeoutType.Unknown:
                    {
                        return GetDefaultUnknownTimeout();
                    }
                default:
                    {
                        TraceOps.DebugTrace(String.Format(
                            "GetDefaultTimeout: unsupported type {0}",
                            timeoutType), typeof(ThreadOps).Name,
                            TracePriority.ThreadDebug);

                        break;
                    }
            }

            return GetDefaultUnknownTimeout();
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsCurrentPool()
        {
            Thread currentThread = Thread.CurrentThread;

            if (currentThread == null)
                return false;

            return currentThread.IsThreadPoolThread;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStaThread()
        {
            Thread currentThread = Thread.CurrentThread;

            if (currentThread == null)
                return false;

            return (currentThread.GetApartmentState() == ApartmentState.STA);
        }

        ///////////////////////////////////////////////////////////////////////

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
                QueueUserWorkItem(new WaitCallback(start), parameter);
                Interlocked.Increment(ref queueCount);
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
        private static void ThreadStartWrapper(
            object state /* in */
            ) /* System.Threading.WaitCallback */
        {
            ThreadStartPair anyPair = state as ThreadStartPair;

            if (anyPair == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "ThreadStartWrapper: cannot convert state to {0}",
                    MarshalOps.GetErrorTypeName(typeof(ThreadStartPair))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            ThreadStart newCallback = anyPair.X;

            if (newCallback == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "ThreadStartWrapper: missing {0} delegate from {1}",
                    MarshalOps.GetErrorTypeName(typeof(ThreadStart)),
                    MarshalOps.GetErrorTypeName(typeof(ThreadStartPair))),
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

        private static void WaitCallbackWrapper(
            object state /* in */
            ) /* System.Threading.WaitCallback */
        {
            WaitCallbackPair anyPair = state as WaitCallbackPair;

            if (anyPair == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitCallbackWrapper: cannot convert state to {0}",
                    MarshalOps.GetErrorTypeName(typeof(WaitCallbackPair))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            WaitCallback newCallback = anyPair.X;

            if (newCallback == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitCallbackWrapper: missing {0} delegate from {1}",
                    MarshalOps.GetErrorTypeName(typeof(WaitCallback)),
                    MarshalOps.GetErrorTypeName(typeof(WaitCallbackPair))),
                    typeof(ThreadOps).Name, TracePriority.ThreadError);

                return;
            }

            object newState = anyPair.Y;

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

        public static bool QueueUserWorkItem(
            ThreadStart callBack /* in */
            )
        {
            return ThreadPool.QueueUserWorkItem(new WaitCallback(
                ThreadStartWrapper), new ThreadStartPair(callBack));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool QueueUserWorkItem(
            WaitCallback callBack /* in */
            )
        {
            return ThreadPool.QueueUserWorkItem(new WaitCallback(
                WaitCallbackWrapper), new WaitCallbackPair(callBack));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool QueueUserWorkItem(
            WaitCallback callBack, /* in */
            object state           /* in */
            )
        {
            return ThreadPool.QueueUserWorkItem(new WaitCallback(
                WaitCallbackWrapper), new WaitCallbackPair(callBack, state));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WaitHandle Helper Methods
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
        public static EventWaitHandle CreateEvent(
            bool automatic /* in */
            )
        {
            try
            {
                return new EventWaitHandle(
                    false, automatic ? EventResetMode.AutoReset :
                    EventResetMode.ManualReset);
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

        public static EventWaitHandle CreateEvent(
            string name /* in */
            )
        {
            return CreateEvent(name, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle CreateEvent(
            string name,   /* in */
            bool automatic /* in */
            )
        {
            try
            {
                if (ShouldUseNamedEvents())
                {
                    NamedEventWaitHandle @event = new NamedEventWaitHandle(
                        false, automatic ? EventResetMode.AutoReset :
                        EventResetMode.ManualReset, name);

                    AddNamedEventForCreate(name, @event);

                    return @event;
                }
                else
                {
                    return new EventWaitHandle(
                        false, automatic ? EventResetMode.AutoReset :
                        EventResetMode.ManualReset, name);
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

        public static EventWaitHandle CreateEvent(
            bool initialState,   /* in */
            EventResetMode mode, /* in */
            string name,         /* in */
            out bool createdNew  /* out */
            )
        {
            try
            {
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

        public static bool WasAnyEventTimeout(
            int index /* in */
            )
        {
            if (index == WaitHandle.WaitTimeout)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

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
