/*
 * StringBuilderCache.cs --
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
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    //
    // WARNING: This class and some of the concepts it uses were (heavily?)
    //          inspired by the class of the same name within the Microsoft
    //          Reference Source for the .NET Framework 4.8, with the file
    //          name "System\Text\StringBuilderCache.cs" and located at the
    //          following URI:
    //
    //          https://referencesource.microsoft.com/#mscorlib/system/text/stringbuildercache.cs
    //
    [ObjectId("b1a3c0c3-208b-4ea8-994e-9328f15526b0")]
    internal static class StringBuilderCache
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static int MinimumCapacity = 32;
        private static int MaximumCapacity = 0; // unlimited
        private static int DefaultCapacity = MinimumCapacity;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this is set to something greater than zero, it will be
        //       used as the capacity for all cache slots; otherwise, their
        //       capacity will be based on the cache slot index.
        //
        private static int FixedCapacity = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to sleep on the thread
        //       used to optimize StringBuilder instances, etc.
        //
        private static int ThreadMilliseconds = 10000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This field keeps track of the number of pending capacity
        //       optimization threads running.  There SHOULD only be zero
        //       or one of these at a time.
        //
        private static int ThreadPending = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: When this field is greater than zero, use of this cache is
        //       enabled; otherwise, it is disabled and instances cannot be
        //       acquired from it -OR- released to it.
        //
        private static int enableCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this field is greater than zero, garbage collection
        //       may be repeatedly attempted by the capacity optimization
        //       thread.
        //
        private static int collectCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This field keeps track of the event used to stop capacity
        //       optimization threads.
        //
        private static EventWaitHandle ThreadStopEvent = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This static field is not per-thread, which means it should
        //       be faster to access than the one embedded within the .NET
        //       Framework (i.e. per-thread storage is "expensive") and it
        //       will be impossible to have its cached instances "orphaned"
        //       on threads that exit without clearing their instance(s).
        //
        // NOTE: When the FixedCapacity field value is zero (or less), the
        //       minimum capacity for each slot will be defined as follows,
        //       with N as the slot index:
        //
        //                  2 ** (N + log2(MinimumCapacity))
        //
        private static readonly StringBuilder[] instances = {
            null, null, null, null, null, null, null, null
        };

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        //
        // NOTE: These fields are used to keep track of per-slot statistics
        //       for this cache.
        //
        private static readonly long[] instanceAcquireCounts = {
               0,    0,    0,    0,    0,    0,    0,    0
        };

        private static readonly long[] instanceReleaseCounts = {
               0,    0,    0,    0,    0,    0,    0,    0
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These fields are used to keep track of overall statistics
        //       for this cache.
        //
        private static long acquireCount = 0;
        private static long noAcquireCount = 0;
        private static long allocateCount = 0;
        private static long releaseCount = 0;
        private static long noReleaseCount = 0;
        private static long clearCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Total number of non-array fields used for statistics by
        //       this class.
        //
        private static readonly int overallCountLength = 6;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static bool TryPopulateAt(
            int index /* in */
            )
        {
            StringBuilder instance = Interlocked.CompareExchange(
                ref instances[index], null, null);

            if (instance == null)
            {
                bool success = false;

                try
                {
                    int capacity = IndexToCapacity(index);

                    if (capacity > 0)
                    {
                        instance = AllocateNew(
                            null, Index.Invalid, Length.Invalid,
                            capacity);

                        if ((instance != null) && Object.ReferenceEquals(
                                null, Interlocked.CompareExchange(
                                ref instances[index], instance, null)) &&
                            Object.ReferenceEquals(
                                instance, Interlocked.CompareExchange(
                                ref instances[index], null, null)))
                        {
                            success = true;
                            return true;
                        }
                    }
                }
                finally
                {
                    if (!success && (instance != null))
                    {
                        ClearExisting(instance);
                        instance = null;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryClearAt(
            int index /* in */
            )
        {
            //
            // HACK: Just acquire the StringBuilder instance
            //       from the cache and never return it, and
            //       clear out its contents.
            //
            StringBuilder builder = null;

            if (TryAcquireFrom(index, ref builder))
            {
                ClearExisting(builder);
                builder = null;

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryAcquireFrom(
            int index,                /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            if (builder == null)
            {
                StringBuilder instance = Interlocked.CompareExchange(
                    ref instances[index], null, null);

                if ((instance != null) && Object.ReferenceEquals(
                        instance, Interlocked.CompareExchange(
                        ref instances[index], null, instance)))
                {
#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref instanceAcquireCounts[index]);
#endif

                    builder = instance;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryReleaseTo(
            int index,                /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            if (builder != null)
            {
                if (Object.ReferenceEquals(Interlocked.CompareExchange(
                        ref instances[index], builder, null), null))
                {
#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref instanceReleaseCounts[index]);
#endif

                    builder = null;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int CapacityToIndex(
            int capacity /* in */
            )
        {
            if (FixedCapacity > 0)
            {
                return 0;
            }
            else
            {
                if ((capacity <= 0) ||
                    (capacity == MinimumCapacity))
                {
                    return 0;
                }

                int offset = (MinimumCapacity > 0) ?
                    MathOps.Log2(MinimumCapacity) : 0;

                return MathOps.Log2(capacity) - offset;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static int IndexToCapacity(
            int index /* in */
            )
        {
            if (FixedCapacity > 0)
            {
                return FixedCapacity;
            }
            else
            {
                int offset = (MinimumCapacity > 0) ?
                    MathOps.Log2(MinimumCapacity) : 0;

                ulong? capacity = MathOps.Pow2(index + offset);

                if (capacity == null)
                    return 0;

                if (((ulong)capacity < 0) ||
                    ((ulong)capacity > int.MaxValue))
                {
                    return 0;
                }

                return (int)capacity;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryAcquire(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            int startIndex = CapacityToIndex(capacity);
            int length = instances.Length; /* SAFE: READ-ONLY */

            for (int index = startIndex; index < length; index++)
                if (TryAcquireFrom(index, ref builder))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryRelease(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            int startIndex = CapacityToIndex(capacity);
            int length = instances.Length; /* SAFE: READ-ONLY */

            for (int index = startIndex; index < length; index++)
                if (TryReleaseTo(index, ref builder))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int TryPopulate()
        {
            int count = 0;
            int length = instances.Length; /* SAFE: READ-ONLY */

            for (int index = 0; index < length; index++)
                if (TryPopulateAt(index))
                    count++;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int TryClear()
        {
            int count = 0;
            int length = instances.Length; /* SAFE: READ-ONLY */

            for (int index = 0; index < length; index++)
                if (TryClearAt(index))
                    count++;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        private static EventWaitHandle MaybeCreateStopEvent()
        {
            if (Interlocked.CompareExchange(
                    ref ThreadStopEvent, null, null) == null)
            {
                bool success = false;
                EventWaitHandle newEvent = null;

                try
                {
                    newEvent = ThreadOps.CreateEvent(false);

                    if ((Interlocked.CompareExchange(
                            ref ThreadStopEvent, newEvent,
                            null) == null) &&
                        (Interlocked.CompareExchange(
                            ref ThreadStopEvent, null,
                            null) == newEvent))
                    {
                        success = true;
                    }
                }
                finally
                {
                    if (!success && (newEvent != null))
                        ThreadOps.CloseEvent(ref newEvent);
                }
            }

            return Interlocked.CompareExchange(
                ref ThreadStopEvent, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        private static int TryOptimize()
        {
            int count = 0;
            int length = instances.Length; /* SAFE: READ-ONLY */

            for (int index = 0; index < length; index++)
            {
                StringBuilder builder = null;

                try
                {
                    if (TryAcquireFrom(index, ref builder))
                    {
                        int capacity = IndexToCapacity(index);

                        if ((capacity > 0) &&
                            CheckCapacity(builder, capacity))
                        {
                            count++;
                        }
                    }
                }
                finally
                {
                    if (builder != null)
                    {
                        if (!TryReleaseTo(index, ref builder))
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryOptimize: cannot release {0}",
                                RuntimeOps.GetHashCode(builder)),
                                typeof(StringBuilderCache).Name,
                                TracePriority.CleanupError2);
                        }
                    }
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        private static long? TryCollect()
        {
            if (Interlocked.CompareExchange(ref collectCount, 0, 0) > 0)
                return ObjectOps.GetTotalMemory(true);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TracePriority GetTracePriority(
            int count1, /* in */
            int count2  /* in */
            )
        {
            TracePriority priority = TracePriority.PerformanceDebug2;

            if (count1 > 0)
                TraceOps.ExternalAdjustTracePriority(ref priority, 1);

            if (count2 > 0)
                TraceOps.ExternalAdjustTracePriority(ref priority, 1);

            return priority;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ThreadStart(
            object obj /* in */
            )
        {
            EventWaitHandle stopEvent = null;

            try
            {
                stopEvent = obj as EventWaitHandle;

                if (stopEvent == null)
                    return;

                while (true)
                {
                    if (AppDomainOps.IsStoppingSoon())
                        break;

                    if (ThreadOps.WaitEvent(
                            stopEvent, ThreadMilliseconds))
                    {
                        ThreadOps.ResetEvent(stopEvent);
                        break;
                    }

                    int count1 = TryOptimize();
                    int count2 = TryPopulate();
                    long? count3 = TryCollect();

                    TraceOps.DebugTrace(
                        "ThreadStart", null,
                        typeof(StringBuilderCache).Name,
                        GetTracePriority(count1, count2),
                        false, "TryOptimize", count1,
                        "TryPopulate", count2, "TryCollect",
                        count3);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (ThreadInterruptedException)
            {
                // do nothing.
            }
            catch (InterpreterDisposedException)
            {
                // do nothing.
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(StringBuilderCache).Name,
                    TracePriority.ThreadError);
            }
            finally
            {
                Interlocked.Decrement(ref ThreadPending);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ClearExisting(
            StringBuilder builder /* in */
            )
        {
            if (builder != null)
                builder.Length = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CheckCapacity(
            StringBuilder builder, /* in */
            int? capacity          /* in */
            )
        {
            if ((builder != null) && (capacity != null))
            {
                int oldCapacity = builder.Capacity;
                int newCapacity = (int)capacity;

                if ((newCapacity > oldCapacity) &&
                    (builder.EnsureCapacity(newCapacity) == newCapacity))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AppendExisting(
            StringBuilder builder, /* in */
            string value,          /* in */
            int startIndex,        /* in: OPTIONAL */
            int length             /* in: OPTIONAL */
            )
        {
            if ((builder != null) && (value != null))
            {
                if ((startIndex != Index.Invalid) &&
                    (length != Length.Invalid))
                {
                    builder.Append(
                        value, startIndex, length);
                }
                else
                {
                    builder.Append(value);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringBuilder AllocateNew(
            string value,   /* in: OPTIONAL */
            int startIndex, /* in: OPTIONAL */
            int length,     /* in: OPTIONAL */
            int? capacity   /* in: OPTIONAL */
            )
        {
            if (value != null)
            {
                if ((startIndex != Index.Invalid) &&
                    (length != Length.Invalid))
                {
                    //
                    // HACK: This code assumes a capacity of zero is
                    //       valid and will end up using the system
                    //       "default" capacity, which is correct in
                    //       the .NET Framework 4.x on Windows.
                    //
                    return (capacity != null) ?
                        new StringBuilder(
                            value, startIndex, length, (int)capacity) :
                        new StringBuilder(
                            value, startIndex, length, 0);
                }
                else
                {
                    return (capacity != null) ?
                        new StringBuilder(value, (int)capacity) :
                        new StringBuilder(value);
                }
            }
            else
            {
                return (capacity != null) ?
                    new StringBuilder((int)capacity) :
                    new StringBuilder();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static StringBuilder Acquire(
            string value,   /* in: OPTIONAL */
            int startIndex, /* in: OPTIONAL */
            int length,     /* in: OPTIONAL */
            int? capacity   /* in: OPTIONAL */
            )
        {
            if (Interlocked.CompareExchange(ref enableCount, 0, 0) > 0)
            {
                if ((capacity == null) ||
                    (((MinimumCapacity <= 0) ||
                        ((int)capacity >= MinimumCapacity)) &&
                    (((MaximumCapacity <= 0) ||
                        ((int)capacity <= MaximumCapacity)))))
                {
                    bool success = false;
                    StringBuilder builder = null;

                    int localCapacity = (capacity != null) ?
                        (int)capacity : DefaultCapacity;

                    try
                    {
                        if (TryAcquire(localCapacity, ref builder))
                        {
                            if (builder != null)
                            {
#if CACHE_STATISTICS
                                Interlocked.Increment(ref acquireCount);
#endif

                                ClearExisting(builder);

                                CheckCapacity(builder, localCapacity);

                                AppendExisting(
                                    builder, value, startIndex, length);

                                success = true;

                                return builder;
                            }
                        }
                    }
                    finally
                    {
                        if (!success && (builder != null))
                        {
                            if (!TryRelease(localCapacity, ref builder))
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "Acquire: cannot release {0}",
                                    RuntimeOps.GetHashCode(builder)),
                                    typeof(StringBuilderCache).Name,
                                    TracePriority.CleanupError2);
                            }
                        }
                    }
                }

#if CACHE_STATISTICS
                Interlocked.Increment(ref noAcquireCount);
#endif
            }

#if CACHE_STATISTICS
            Interlocked.Increment(ref allocateCount);
#endif

            return AllocateNew(value, startIndex, length, capacity);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Release(
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            if (builder != null)
            {
                if (Interlocked.CompareExchange(ref enableCount, 0, 0) > 0)
                {
                    int capacity = builder.Capacity;

                    if ((MinimumCapacity <= 0) ||
                        (capacity >= MinimumCapacity))
                    {
                        if ((MaximumCapacity <= 0) ||
                            (capacity <= MaximumCapacity))
                        {
                            if (TryRelease(capacity, ref builder))
                            {
#if CACHE_STATISTICS
                                Interlocked.Increment(ref releaseCount);
#endif

                                return true;
                            }
                        }
                    }

#if CACHE_STATISTICS
                    Interlocked.Increment(ref noReleaseCount);
#endif
                }

                ClearExisting(builder);
                builder = null;

#if CACHE_STATISTICS
                Interlocked.Increment(ref clearCount);
#endif
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetStringAndRelease(
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            string result = null;

            if (builder != null)
            {
                result = builder.ToString();

                /* IGNORED */
                Release(ref builder);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeEnable(
            bool? enable /* in: OPTIONAL */
            )
        {
            if (enable != null)
            {
                if ((bool)enable)
                    return Interlocked.Increment(ref enableCount) > 0;
                else
                    return Interlocked.Decrement(ref enableCount) <= 0;
            }

            return Interlocked.CompareExchange(ref enableCount, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeCollect(
            bool? enable /* in: OPTIONAL */
            )
        {
            if (enable != null)
            {
                if ((bool)enable)
                    return Interlocked.Increment(ref collectCount) > 0;
                else
                    return Interlocked.Decrement(ref collectCount) <= 0;
            }

            return Interlocked.CompareExchange(ref collectCount, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeEnableThread(
            bool? enable /* in: OPTIONAL */
            )
        {
            if (enable != null)
            {
                EventWaitHandle stopEvent = MaybeCreateStopEvent();

                if ((bool)enable)
                {
                    if (Interlocked.Increment(ref ThreadPending) == 1)
                    {
                        bool success = false;

                        try
                        {
                            success = ThreadPool.QueueUserWorkItem(
                                ThreadStart, stopEvent);
                        }
                        finally
                        {
                            if (!success)
                                Interlocked.Decrement(ref ThreadPending);
                        }
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(
                            ref ThreadPending, 0, 0) > 0)
                    {
                        return ThreadOps.SetEvent(stopEvent);
                    }
                }
            }

            return Interlocked.CompareExchange(ref ThreadPending, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int Clear()
        {
            return TryClear();
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public static void ZeroCounts()
        {
            Interlocked.Exchange(ref acquireCount, 0);
            Interlocked.Exchange(ref noAcquireCount, 0);
            Interlocked.Exchange(ref allocateCount, 0);
            Interlocked.Exchange(ref releaseCount, 0);
            Interlocked.Exchange(ref noReleaseCount, 0);
            Interlocked.Exchange(ref clearCount, 0);

            int length; /* REUSED */

            if (instanceAcquireCounts != null)
            {
                length = instanceAcquireCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    Interlocked.Exchange(
                        ref instanceAcquireCounts[index], 0);
                }
            }

            if (instanceReleaseCounts != null)
            {
                length = instanceReleaseCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    Interlocked.Exchange(
                        ref instanceReleaseCounts[index], 0);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeSaveCounts(
            CacheFlags flags,                                   /* in */
            bool move,                                          /* in */
            ref Dictionary<CacheFlags, long[]> savedCacheCounts /* in, out */
            )
        {
            if (savedCacheCounts == null)
                savedCacheCounts = new Dictionary<CacheFlags, long[]>();

            int length; /* REUSED */

            length = overallCountLength;

            if (instanceAcquireCounts != null)
                length += instanceAcquireCounts.Length;

            if (instanceReleaseCounts != null)
                length += instanceReleaseCounts.Length;

            long[] counts = new long[length];

            counts[0] = Interlocked.CompareExchange(ref acquireCount, 0, 0);
            counts[1] = Interlocked.CompareExchange(ref noAcquireCount, 0, 0);
            counts[2] = Interlocked.CompareExchange(ref allocateCount, 0, 0);
            counts[3] = Interlocked.CompareExchange(ref releaseCount, 0, 0);
            counts[4] = Interlocked.CompareExchange(ref noReleaseCount, 0, 0);
            counts[5] = Interlocked.CompareExchange(ref clearCount, 0, 0);

            int offset = overallCountLength;

            if (instanceAcquireCounts != null)
            {
                length = instanceAcquireCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    counts[offset++] = Interlocked.CompareExchange(
                        ref instanceAcquireCounts[index], 0, 0);
                }
            }

            if (instanceReleaseCounts != null)
            {
                length = instanceReleaseCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    counts[offset++] = Interlocked.CompareExchange(
                        ref instanceReleaseCounts[index], 0, 0);
                }
            }

            savedCacheCounts[flags] = counts;

            if (move)
                ZeroCounts();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeRestoreCounts(
            CacheFlags flags,                                   /* in */
            bool merge,                                         /* in */
            bool move,                                          /* in */
            ref Dictionary<CacheFlags, long[]> savedCacheCounts /* in, out */
            )
        {
            if (savedCacheCounts == null)
                return false;

            long[] counts;

            if (!savedCacheCounts.TryGetValue(flags, out counts))
                return false;

            if (counts == null)
                return false;

            int wantLength = overallCountLength;

            if (instanceAcquireCounts != null)
                wantLength += instanceAcquireCounts.Length;

            if (instanceReleaseCounts != null)
                wantLength += instanceReleaseCounts.Length;

            int haveLength = counts.Length;

            if (haveLength < wantLength)
                return false;

            if (merge)
            {
                Interlocked.Add(ref acquireCount, counts[0]);
                Interlocked.Add(ref noAcquireCount, counts[1]);
                Interlocked.Add(ref allocateCount, counts[2]);
                Interlocked.Add(ref releaseCount, counts[3]);
                Interlocked.Add(ref noReleaseCount, counts[4]);
                Interlocked.Add(ref clearCount, counts[5]);
            }
            else
            {
                Interlocked.Exchange(ref acquireCount, counts[0]);
                Interlocked.Exchange(ref noAcquireCount, counts[1]);
                Interlocked.Exchange(ref allocateCount, counts[2]);
                Interlocked.Exchange(ref releaseCount, counts[3]);
                Interlocked.Exchange(ref noReleaseCount, counts[4]);
                Interlocked.Exchange(ref clearCount, counts[5]);
            }

            int offset = overallCountLength;
            int length; /* REUSED */

            if (instanceAcquireCounts != null)
            {
                length = instanceAcquireCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    if (merge)
                    {
                        Interlocked.Add(
                            ref instanceAcquireCounts[index],
                            counts[offset++]);
                    }
                    else
                    {
                        Interlocked.Exchange(
                            ref instanceAcquireCounts[index],
                            counts[offset++]);
                    }
                }
            }

            if (instanceReleaseCounts != null)
            {
                length = instanceReleaseCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    if (merge)
                    {
                        Interlocked.Add(
                            ref instanceReleaseCounts[index],
                            counts[offset++]);
                    }
                    else
                    {
                        Interlocked.Exchange(
                            ref instanceReleaseCounts[index],
                            counts[offset++]);
                    }
                }
            }

            if (move)
            {
                if (savedCacheCounts.Remove(flags) &&
                    (savedCacheCounts.Count == 0))
                {
                    savedCacheCounts.Clear();
                    savedCacheCounts = null;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CountsToList(
            StringPairList list, /* in, out */
            bool summaryOnly,    /* in */
            bool empty           /* in */
            )
        {
            if (list == null)
                return;

            long count; /* REUSED */
            int length; /* REUSED */

            count = Interlocked.CompareExchange(ref acquireCount, 0, 0);

            if (empty || (count != 0))
                list.Add("AcquireCount", count.ToString());

            count = Interlocked.CompareExchange(ref noAcquireCount, 0, 0);

            if (empty || (count != 0))
                list.Add("NoAcquireCount", count.ToString());

            count = Interlocked.CompareExchange(ref releaseCount, 0, 0);

            if (empty || (count != 0))
                list.Add("ReleaseCount", count.ToString());

            count = Interlocked.CompareExchange(ref noReleaseCount, 0, 0);

            if (empty || (count != 0))
                list.Add("NoReleaseCount", count.ToString());

            count = Interlocked.CompareExchange(ref clearCount, 0, 0);

            if (empty || (count != 0))
                list.Add("ClearCount", count.ToString());

            if (!summaryOnly)
            {
                length = instanceAcquireCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    count = Interlocked.CompareExchange(
                        ref instanceAcquireCounts[index], 0, 0);

                    if (empty || (count != 0))
                    {
                        list.Add(String.Format(
                            "InstanceAcquireCount[{0}]", index),
                            count.ToString());
                    }
                }

                length = instanceReleaseCounts.Length; /* SAFE: READ-ONLY */

                for (int index = 0; index < length; index++)
                {
                    count = Interlocked.CompareExchange(
                        ref instanceReleaseCounts[index], 0, 0);

                    if (empty || (count != 0))
                    {
                        list.Add(String.Format(
                            "InstanceReleaseCount[{0}]", index),
                            count.ToString());
                    }
                }
            }
        }
#endif
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

            bool empty = HostOps.HasEmptyContent(detailFlags);
            bool verbose = HostOps.HasVerboseContent(detailFlags);
            StringPairList localList = new StringPairList();
            long count; /* REUSED */
            int length; /* REUSED */

            count = Interlocked.CompareExchange(ref enableCount, 0, 0);

            if (empty || (count != 0))
                localList.Add("EnableCount", count.ToString());

            count = Interlocked.CompareExchange(ref collectCount, 0, 0);

            if (empty || (count != 0))
                localList.Add("CollectCount", count.ToString());

            count = Interlocked.CompareExchange(ref MinimumCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("MinimumCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref MaximumCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("MaximumCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref DefaultCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("DefaultCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref ThreadPending, 0, 0);

            if (empty || (count != 0))
                localList.Add("ThreadPending", count.ToString());

            count = Interlocked.CompareExchange(ref ThreadMilliseconds, 0, 0);

            if (empty || (count != 0))
                localList.Add("ThreadMilliseconds", count.ToString());

            length = instances.Length; /* SAFE: READ-ONLY */

            for (int index = 0; index < length; index++)
            {
                StringBuilder builder = null;

                try
                {
                    if (TryAcquireFrom(index, ref builder))
                    {
                        if (empty || (builder != null))
                        {
                            localList.Add(String.Format(
                                "Instance[{0}]", index),
                                (builder != null) ?
                                    builder.Capacity.ToString() :
                                    FormatOps.DisplayNull);
                        }
                    }
                    else if (empty)
                    {
                        localList.Add(String.Format(
                            "Instance[{0}]", index),
                            FormatOps.DisplayNone);
                    }
                }
                finally
                {
                    if (!TryReleaseTo(index, ref builder))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "AddInfo: cannot release {0}",
                            RuntimeOps.GetHashCode(builder)),
                            typeof(StringBuilderCache).Name,
                            TracePriority.CleanupError2);
                    }
                }
            }

#if CACHE_STATISTICS
            CountsToList(localList, !verbose, empty);
#endif

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("StringBuilder Cache");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion
    }
}
