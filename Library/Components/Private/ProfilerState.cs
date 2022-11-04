/*
 * ProfilerState.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    ///////////////////////////////////////////////////////////////////////////
    //
    // NOTE: When attempting to diagnose a temporary performance issue, the
    //       following approach works well:
    //
    //       First, somewhere in the class being measured, add the following
    //       declarations, adjusting the total number of array items to match
    //       the total number of discrete code sections being measured:
    //
    //       static ProfilerState profiler = ProfilerState.Create();
    //       static double[] microseconds = { 0, 0, ..., 0 };
    //
    //       Then, add the following method call immediately before each
    //       section of code being measured:
    //
    //       profiler.Start();
    //
    //       Finally, add the following method call immediately after each
    //       section of code being measured, incrementing the array index
    //       by one for each subsequent section of code being measured:
    //
    //       microseconds[0] += profiler.Stop();
    //
    //       To easily access the results, the following script command may
    //       be used, where <className> is the namespace qualified name of
    //       the class being measured:
    //
    //       object invoke -flags +NonPublic <className> microseconds
    //
    ///////////////////////////////////////////////////////////////////////////
    [ObjectId("9262b13e-53d9-4f79-9548-a82c3ec30f1a")]
    internal sealed class ProfilerState :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IProfilerState
    {
        #region Private Data
        private object syncRoot;

        ///////////////////////////////////////////////////////////////////////

        private double microseconds;
        private Stack<long> startCounts;

        ///////////////////////////////////////////////////////////////////////

        private double totalMicroseconds;
        private long totalStarts;
        private long totalStops;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ProfilerState()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IProfilerState Create()
        {
            bool dispose = true;

            return Create(null, null, false, false, ref dispose);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IProfilerState Create(
            Interpreter interpreter, /* in */
            ref bool dispose         /* out */
            )
        {
            return Create(interpreter, null, false, false, ref dispose);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IProfilerState CreateWithTotalsFrom(
            IProfilerState profiler /* in */
            )
        {
            bool dispose = true;

            return Create(null, profiler, true, false, ref dispose);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IProfilerState Create(
            Interpreter interpreter, /* in */
            IProfilerState profiler, /* in */
            bool shared,             /* in */
            bool autoStart,          /* in */
            ref bool dispose         /* out */
            )
        {
            ProfilerState localProfiler = ReuseFrom(
                interpreter) as ProfilerState;

            if (localProfiler != null)
            {
                //
                // NOTE: The "shared" value is implicitly
                //       considered true for this case as
                //       we are reusing a profiler from
                //       the specified interpreter.  Also,
                //       since this profiler is shared, it
                //       must not be disposed by the caller.
                //
                /* IGNORED */
                localProfiler.TryEnableLocking();

                dispose = false;
            }
            else
            {
                localProfiler = new ProfilerState();

                if (shared)
                {
                    /* IGNORED */
                    localProfiler.TryEnableLocking();
                }
            }

            /* IGNORED */
            CopyTotals(profiler, localProfiler);

            if (autoStart)
                localProfiler.Start();

            return localProfiler;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static IProfilerState ReuseFrom(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter != null)
            {
                IProfilerState profiler;

                if (interpreter.TryReuseProfiler(out profiler))
                    return profiler;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CopyTotals(
            IProfilerState source, /* in */
            IProfilerState target  /* in */
            )
        {
            ProfilerState localSource = source as ProfilerState;
            ProfilerState localTarget = target as ProfilerState;

            if ((localSource != null) && (localTarget != null))
            {
                double totalMicroseconds;
                long totalStarts;
                long totalStops;

                localSource.GetTotals(
                    out totalMicroseconds, out totalStarts,
                    out totalStops);

                localTarget.SetTotals(
                    totalMicroseconds, totalStarts, totalStops);

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // WARNING: *DESIGN* The actions of this method cannot be undone.
        //          Once locking is enabled for this object, it must stay
        //          enabled.
        //
        // NOTE: This method cannot actually "fail", despite its possible
        //       false return values because both points within the method
        //       where false can be returned can only occur if locking has
        //       (already?) been enabled by another thread.  To summarize,
        //       upon exiting this method, locking will ALWAYS be enabled,
        //       even though any particular invocation this method may not
        //       change any state and may return false.
        //
        private bool TryEnableLocking()
        {
            object oldSyncRoot = Interlocked.CompareExchange(
                ref syncRoot, null, null);

            if (oldSyncRoot != null)
                return false; /* NOTE: Locking already enabled? */

            object newSyncRoot = new object();

            oldSyncRoot = Interlocked.CompareExchange(
                ref syncRoot, newSyncRoot, null); /* TRANSACTIONAL */

            return (oldSyncRoot == null);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        //
        // WARNING: Do not use this method, it is here only to serve as
        //          a reference.
        //
        private bool TryDisableLocking()
        {
            object oldSyncRoot = Interlocked.CompareExchange(
                ref syncRoot, null, null);

            if (oldSyncRoot == null)
                return false; /* NOTE: Locking already disabled? */

            Monitor.Enter(oldSyncRoot);

            object newSyncRoot = Interlocked.CompareExchange(
                ref syncRoot, null, oldSyncRoot); /* TRANSACTIONAL */

            Monitor.Exit(oldSyncRoot);

            return Object.ReferenceEquals(newSyncRoot, oldSyncRoot);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private bool MaybeLock(
            ref bool locked /* out */
            )
        {
            object localSyncRoot = Interlocked.CompareExchange(
                ref syncRoot, null, null);

            if (localSyncRoot == null)
                return true;

            Monitor.Enter(localSyncRoot);
            locked = true;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private void MaybeUnlock(
            ref bool locked /* out */
            )
        {
            if (!locked)
                return;

            object localSyncRoot = Interlocked.CompareExchange(
                ref syncRoot, null, null);

            if (localSyncRoot == null)
                return;

            Monitor.Exit(localSyncRoot);
            locked = false;
        }

        ///////////////////////////////////////////////////////////////////////

        private void GetTotals(
            out double totalMicroseconds, /* out */
            out long totalStarts,         /* out */
            out long totalStops           /* out */
            )
        {
            totalMicroseconds = 0.0;
            totalStarts = 0;
            totalStops = 0;

            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return;

            try
            {
                totalMicroseconds = this.totalMicroseconds;
                totalStarts = this.totalStarts;
                totalStops = this.totalStops;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetTotals(
            double totalMicroseconds, /* in */
            long totalStarts,         /* in */
            long totalStops           /* in */
            )
        {
            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return;

            try
            {
                this.totalMicroseconds = totalMicroseconds;
                this.totalStarts = totalStarts;
                this.totalStops = totalStops;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void YetAnotherStart(
            long startCount /* in */
            )
        {
            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return;

            try
            {
                if (startCounts == null)
                    startCounts = new Stack<long>();

                startCounts.Push(startCount);
                totalStarts++;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool TryBeginYetAnotherStop(
            out long startCount /* out */
            )
        {
            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
            {
                startCount = 0;
                return false;
            }

            try
            {
                if ((startCounts == null) || (startCounts.Count == 0))
                {
                    startCount = 0;
                    return false;
                }

                startCount = startCounts.Pop();
                return true;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void EndYetAnotherStop()
        {
            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return;

            try
            {
                totalMicroseconds += microseconds;
                totalStops++;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void Reset(
            bool withTotals /* in */
            )
        {
            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return;

            try
            {
                microseconds = 0.0;
                startCounts = null;

                if (withTotals)
                {
                    totalMicroseconds = 0.0;
                    totalStarts = 0;
                    totalStops = 0;
                }
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return false; /* UNSUPPORTED */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProfilerState Members
        public long? GetMilliseconds()
        {
            CheckDisposed();

            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return null;

            try
            {
                return PerformanceOps.GetMillisecondsFromMicroseconds(
                    ConversionOps.ToLong(Math.Round(
                        microseconds))); /* throw */
            }
            catch
            {
                return null;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Start()
        {
            CheckDisposed();

            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return;

            try
            {
                long startCount = 0;

                ProfileOps.Start(ref startCount, ref microseconds);

                YetAnotherStart(startCount);
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public double Stop()
        {
            CheckDisposed();

            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return 0.0;

            try
            {
                long startCount;

                if (!TryBeginYetAnotherStop(out startCount))
                    return 0.0;

                ProfileOps.Stop(startCount, ref microseconds);

                EndYetAnotherStop();
                return microseconds;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public double Stop(
            bool obfuscate /* in */
            )
        {
            CheckDisposed();

            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return 0.0;

            try
            {
                long startCount;

                if (!TryBeginYetAnotherStop(out startCount))
                    return 0.0;

                ProfileOps.Stop(
                    startCount, obfuscate, ref microseconds);

                EndYetAnotherStop();
                return microseconds;
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList()
        {
            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return null;

            StringPairList result = new StringPairList();

            try
            {
                result.Add("microseconds", microseconds.ToString());
                result.Add("totalMicroseconds", totalMicroseconds.ToString());
                result.Add("totalStarts", totalStarts.ToString());
                result.Add("totalStops", totalStops.ToString());
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            // CheckDisposed(); /* EXEMPT: Engine EXIT PATH. */

            bool locked = false;

            if (!MaybeLock(ref locked)) /* TRANSACTIONAL */
                return null;

            try
            {
                return FormatOps.PerformanceMicroseconds(microseconds);
            }
            finally
            {
                MaybeUnlock(ref locked); /* TRANSACTIONAL */
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

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(ProfilerState).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    Reset(true);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~ProfilerState()
        {
            Dispose(false);
        }
        #endregion
    }
}
