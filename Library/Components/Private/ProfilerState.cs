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
    /// <summary>
    /// This class provides a simple, optionally thread-safe profiler used to
    /// measure the elapsed time spent within discrete sections of code.  It
    /// supports nested start and stop calls (via an internal stack of start
    /// counts) and accumulates running totals across multiple measurements.
    /// It implements <see cref="IProfilerState" />.
    /// </summary>
    [ObjectId("9262b13e-53d9-4f79-9548-a82c3ec30f1a")]
    internal sealed class ProfilerState :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IProfilerState
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// instance.  When null, locking is disabled.
        /// </summary>
        private object syncRoot;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The elapsed time, in microseconds, measured for the most recently
        /// completed start-and-stop measurement.
        /// </summary>
        private double microseconds;
        /// <summary>
        /// The stack of pending start counts, one per outstanding start, used
        /// to support nested measurements.
        /// </summary>
        private Stack<long> startCounts;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The running total elapsed time, in microseconds, accumulated across
        /// all measurements.
        /// </summary>
        private double totalMicroseconds;
        /// <summary>
        /// The running total number of start operations performed.
        /// </summary>
        private long totalStarts;
        /// <summary>
        /// The running total number of stop operations performed.
        /// </summary>
        private long totalStops;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty instance with locking disabled and all totals
        /// set to their default values.
        /// </summary>
        private ProfilerState()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Creates a new, unshared profiler instance.
        /// </summary>
        /// <returns>
        /// The newly created profiler instance.
        /// </returns>
        public static IProfilerState Create()
        {
            bool dispose = true;

            return Create(null, null, false, false, ref dispose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a profiler instance for the specified interpreter, reusing
        /// an existing shared profiler from that interpreter when available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose existing profiler should be reused, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="dispose">
        /// Upon return, set to non-zero if the caller is responsible for
        /// disposing the returned profiler, or zero if it is shared and must
        /// not be disposed by the caller.
        /// </param>
        /// <returns>
        /// The reused or newly created profiler instance.
        /// </returns>
        public static IProfilerState Create(
            Interpreter interpreter, /* in */
            ref bool dispose         /* out */
            )
        {
            return Create(interpreter, null, false, false, ref dispose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new, shared profiler instance whose running totals are
        /// copied from the specified existing profiler.
        /// </summary>
        /// <param name="profiler">
        /// The profiler whose running totals should be copied into the new
        /// instance.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created profiler instance.
        /// </returns>
        public static IProfilerState CreateWithTotalsFrom(
            IProfilerState profiler /* in */
            )
        {
            bool dispose = true;

            return Create(null, profiler, true, false, ref dispose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates or reuses a profiler instance, optionally enabling locking,
        /// copying running totals from another profiler, and starting a
        /// measurement immediately.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose existing profiler should be reused, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="profiler">
        /// The profiler whose running totals should be copied into the result.
        /// This parameter may be null.
        /// </param>
        /// <param name="shared">
        /// Non-zero to enable locking on a newly created profiler so that it
        /// may be shared safely between threads.
        /// </param>
        /// <param name="autoStart">
        /// Non-zero to start a measurement on the result before returning it.
        /// </param>
        /// <param name="dispose">
        /// Upon return, set to non-zero if the caller is responsible for
        /// disposing the returned profiler, or zero if it is shared and must
        /// not be disposed by the caller.
        /// </param>
        /// <returns>
        /// The reused or newly created profiler instance.
        /// </returns>
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
        /// <summary>
        /// Attempts to obtain a reusable profiler instance from the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose reusable profiler should be obtained.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The reusable profiler instance, or null if none is available.
        /// </returns>
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

        /// <summary>
        /// Copies the running totals from one profiler instance to another.
        /// </summary>
        /// <param name="source">
        /// The profiler whose running totals should be read.  This parameter
        /// may be null.
        /// </param>
        /// <param name="target">
        /// The profiler whose running totals should be written.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the totals were copied; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Enables locking for this instance, in a thread-safe manner, so that
        /// access to its mutable state is synchronized.  Once enabled, locking
        /// cannot be disabled.
        /// </summary>
        /// <returns>
        /// True if this invocation enabled locking; false if locking was
        /// already enabled (by this or another thread).  In all cases, locking
        /// will be enabled upon return.
        /// </returns>
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
        /// <summary>
        /// Disables locking for this instance, in a thread-safe manner.  This
        /// method is unused and exists only to serve as a reference.
        /// </summary>
        /// <returns>
        /// True if this invocation disabled locking; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Acquires the synchronization lock for this instance, if locking is
        /// enabled.
        /// </summary>
        /// <param name="locked">
        /// Upon return, set to non-zero if the lock was acquired by this call
        /// and must be released later.
        /// </param>
        /// <returns>
        /// Always returns true.
        /// </returns>
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

        /// <summary>
        /// Releases the synchronization lock for this instance, if it was
        /// previously acquired.
        /// </summary>
        /// <param name="locked">
        /// On input, non-zero if the lock was acquired and should be released;
        /// upon return, set to zero once the lock has been released.
        /// </param>
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

        /// <summary>
        /// Gets the running totals accumulated by this instance.
        /// </summary>
        /// <param name="totalMicroseconds">
        /// Upon return, set to the running total elapsed time, in microseconds.
        /// </param>
        /// <param name="totalStarts">
        /// Upon return, set to the running total number of start operations.
        /// </param>
        /// <param name="totalStops">
        /// Upon return, set to the running total number of stop operations.
        /// </param>
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

        /// <summary>
        /// Sets the running totals accumulated by this instance.
        /// </summary>
        /// <param name="totalMicroseconds">
        /// The running total elapsed time, in microseconds.
        /// </param>
        /// <param name="totalStarts">
        /// The running total number of start operations.
        /// </param>
        /// <param name="totalStops">
        /// The running total number of stop operations.
        /// </param>
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

        /// <summary>
        /// Records the start of a new measurement by pushing the supplied start
        /// count and incrementing the running total of start operations.
        /// </summary>
        /// <param name="startCount">
        /// The performance counter value captured at the start of the
        /// measurement.
        /// </param>
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

        /// <summary>
        /// Begins the completion of a measurement by popping the most recently
        /// pushed start count, if any.
        /// </summary>
        /// <param name="startCount">
        /// Upon return, set to the popped start count, or zero if there was no
        /// outstanding start.
        /// </param>
        /// <returns>
        /// True if an outstanding start count was available and popped;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Completes a measurement by adding the most recently measured elapsed
        /// time to the running total and incrementing the running total of stop
        /// operations.
        /// </summary>
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

        /// <summary>
        /// Resets the current measurement state of this instance and,
        /// optionally, its running totals.
        /// </summary>
        /// <param name="withTotals">
        /// Non-zero to also reset the running totals to their default values.
        /// </param>
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
        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
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
        /// Gets a value indicating whether this instance is in the process of
        /// being disposed.  This is not supported and always returns false.
        /// </summary>
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
        /// <summary>
        /// Gets the elapsed time of the most recently completed measurement,
        /// rounded to whole milliseconds.
        /// </summary>
        /// <returns>
        /// The elapsed time, in milliseconds, or null if it could not be
        /// computed.
        /// </returns>
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

        /// <summary>
        /// Starts a new (possibly nested) measurement by capturing the current
        /// performance counter value.
        /// </summary>
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

        /// <summary>
        /// Stops the most recently started measurement and returns its elapsed
        /// time.
        /// </summary>
        /// <returns>
        /// The elapsed time, in microseconds, of the completed measurement, or
        /// zero if there was no outstanding start.
        /// </returns>
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

        /// <summary>
        /// Stops the most recently started measurement and returns its elapsed
        /// time, optionally obfuscating the result.
        /// </summary>
        /// <param name="obfuscate">
        /// Non-zero to obfuscate the measured elapsed time.
        /// </param>
        /// <returns>
        /// The elapsed time, in microseconds, of the completed measurement, or
        /// zero if there was no outstanding start.
        /// </returns>
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

        /// <summary>
        /// Builds a list of name/value pairs describing the current measurement
        /// state and running totals of this instance.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs, or null if the list could not be
        /// built.
        /// </returns>
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
        /// <summary>
        /// Returns a string representation of the most recently measured
        /// elapsed time.
        /// </summary>
        /// <returns>
        /// The formatted elapsed time, in microseconds, or null if it could
        /// not be formatted.
        /// </returns>
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
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// When non-zero, this instance has been disposed and should no longer
        /// be used.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws <see cref="ObjectDisposedException" /> if this
        /// instance has already been disposed and the interpreter is configured
        /// to throw on disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(ProfilerState).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
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
        /// <summary>
        /// Finalizes this instance, releasing any unmanaged resources.
        /// </summary>
        ~ProfilerState()
        {
            Dispose(false);
        }
        #endregion
    }
}
