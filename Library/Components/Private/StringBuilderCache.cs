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
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    /// <summary>
    /// This class implements a process-wide, lock-free cache of reusable
    /// <see cref="StringBuilder" /> instances, organized into a fixed set of
    /// capacity-based slots.  It allows transient <see cref="StringBuilder" />
    /// instances to be acquired from and released back to the cache, reducing
    /// the number of allocations performed when building strings.  An optional
    /// background thread may be used to populate, optimize, and trim the cached
    /// instances over time.
    /// </summary>
    [ObjectId("b1a3c0c3-208b-4ea8-994e-9328f15526b0")]
    internal static class StringBuilderCache
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The minimum capacity, in characters, that a cached
        /// <see cref="StringBuilder" /> instance may have; a value of zero or
        /// less indicates there is no minimum.
        /// </summary>
        private static int MinimumCapacity = 32;
        /// <summary>
        /// The maximum capacity, in characters, that a cached
        /// <see cref="StringBuilder" /> instance may have; a value of zero or
        /// less indicates there is no maximum (i.e. unlimited).
        /// </summary>
        private static int MaximumCapacity = 0; // unlimited
        /// <summary>
        /// The default capacity, in characters, used when a
        /// <see cref="StringBuilder" /> instance is requested without an
        /// explicit capacity.
        /// </summary>
        private static int DefaultCapacity = MinimumCapacity;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this is set to something greater than zero, it will be
        //       used as the capacity for all cache slots; otherwise, their
        //       capacity will be based on the cache slot index.
        //
        /// <summary>
        /// When greater than zero, this value is used as the capacity for all
        /// cache slots; otherwise, the capacity of each slot is based on the
        /// slot index.
        /// </summary>
        private static int FixedCapacity = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the number of milliseconds to sleep on the thread
        //       used to optimize StringBuilder instances, etc.
        //
        /// <summary>
        /// The number of milliseconds to sleep on the thread used to optimize
        /// <see cref="StringBuilder" /> instances, etc.
        /// </summary>
        private static int ThreadMilliseconds = 10000;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This field keeps track of the number of pending capacity
        //       optimization threads running.  There SHOULD only be zero
        //       or one of these at a time.
        //
        /// <summary>
        /// The number of pending capacity optimization threads currently
        /// running.  There should only be zero or one of these at a time.
        /// </summary>
        private static int ThreadPending = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this field is set to positive, all attempts to acquire
        //       or release a (previously cached?) StringBuilder instances
        //       will start searching at its index value; otherwise, these
        //       operations will start with an index based on their stated
        //       capacity.
        //
        /// <summary>
        /// When set to a non-negative value, all attempts to acquire or release
        /// a <see cref="StringBuilder" /> instance will start searching at this
        /// index; otherwise, those operations start with an index based on their
        /// stated capacity.
        /// </summary>
        private static int PreferStartIndex = -1;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is the extra offset value to add when converting an
        //       index value to a capacity value, i.e. a value of one will
        //       cause the capacity to be increased by a factor of two.
        //
        /// <summary>
        /// The extra offset value to add when converting an index value to a
        /// capacity value, e.g. a value of one will cause the capacity to be
        /// increased by a factor of two.
        /// </summary>
        private static int ExtraOffset = 1;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this field is false, only a slot index matching the
        //       requested capacity will be acquired from / released to;
        //       otherwise, any slot index may be used.
        //
        /// <summary>
        /// When false, only a slot index matching the requested capacity will
        /// be acquired from or released to; otherwise, any slot index may be
        /// used.
        /// </summary>
        private static bool TryForAny = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: When this field is greater than zero, use of this cache is
        //       enabled; otherwise, it is disabled and instances cannot be
        //       acquired from it -OR- released to it.
        //
        /// <summary>
        /// When greater than zero, use of this cache is enabled; otherwise, it
        /// is disabled and instances cannot be acquired from it nor released to
        /// it.
        /// </summary>
        private static int enableCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this field is greater than zero, garbage collection
        //       may be repeatedly attempted by the capacity optimization
        //       thread.
        //
        /// <summary>
        /// When greater than zero, garbage collection may be repeatedly
        /// attempted by the capacity optimization thread.
        /// </summary>
        private static int collectCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This field keeps track of the event used to stop capacity
        //       optimization threads.
        //
        /// <summary>
        /// The event used to stop capacity optimization threads.
        /// </summary>
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
        //                  2 ** (N + log2(MinimumCapacity) + ExtraOffset)
        //
        /// <summary>
        /// The array of cached <see cref="StringBuilder" /> instances, with one
        /// slot per capacity class.  Each slot holds at most one cached instance
        /// at a time.
        /// </summary>
        private static readonly StringBuilder[] instances = {
            null, null, null, null, null, null, null, null
        };

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        //
        // NOTE: These fields are used to keep track of per-slot statistics
        //       for this cache.
        //
        /// <summary>
        /// The per-slot count of <see cref="StringBuilder" /> instances that
        /// have been acquired from this cache.
        /// </summary>
        private static readonly long[] instanceAcquireCounts = {
               0,    0,    0,    0,    0,    0,    0,    0
        };

        /// <summary>
        /// The per-slot count of <see cref="StringBuilder" /> instances that
        /// have been released back to this cache.
        /// </summary>
        private static readonly long[] instanceReleaseCounts = {
               0,    0,    0,    0,    0,    0,    0,    0
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These fields are used to keep track of overall statistics
        //       for this cache.
        //
        /// <summary>
        /// The total number of <see cref="StringBuilder" /> instances that have
        /// been acquired from this cache.
        /// </summary>
        private static long acquireCount = 0;
        /// <summary>
        /// The total number of times a <see cref="StringBuilder" /> instance
        /// could not be acquired from this cache.
        /// </summary>
        private static long noAcquireCount = 0;
        /// <summary>
        /// The total number of <see cref="StringBuilder" /> instances that have
        /// been freshly allocated instead of being acquired from this cache.
        /// </summary>
        private static long allocateCount = 0;
        /// <summary>
        /// The total number of <see cref="StringBuilder" /> instances that have
        /// been released back to this cache.
        /// </summary>
        private static long releaseCount = 0;
        /// <summary>
        /// The total number of times a <see cref="StringBuilder" /> instance
        /// could not be released back to this cache.
        /// </summary>
        private static long noReleaseCount = 0;
        /// <summary>
        /// The total number of <see cref="StringBuilder" /> instances that have
        /// been cleared instead of being released back to this cache.
        /// </summary>
        private static long clearCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Total number of non-array fields used for statistics by
        //       this class.
        //
        /// <summary>
        /// The total number of non-array fields used for statistics by this
        /// class.
        /// </summary>
        private static readonly int overallCountLength = 6;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method temporarily disables use of this cache, saving the
        /// previous enabled count so it can be restored later.
        /// </summary>
        /// <param name="savedEnableCount">
        /// Upon success, receives the previous enabled count of this cache;
        /// upon failure, the value stored here is unspecified.
        /// </param>
        /// <returns>
        /// True if the cache was successfully disabled; otherwise, false.
        /// </returns>
        private static bool BeginNoCache(
            out int savedEnableCount /* out */
            )
        {
            savedEnableCount = Interlocked.CompareExchange(
                ref enableCount, 0, 0);

            if (Interlocked.CompareExchange(ref enableCount,
                    0, savedEnableCount) == savedEnableCount)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method re-enables use of this cache by restoring the enabled
        /// count previously saved by <c>BeginNoCache</c>.
        /// </summary>
        /// <param name="savedEnableCount">
        /// On input, the enabled count to restore.  Upon success, this value is
        /// reset to zero; upon failure, it is left unchanged.
        /// </param>
        /// <returns>
        /// True if the enabled count was successfully restored; otherwise,
        /// false.
        /// </returns>
        private static bool EndNoCache(
            ref int savedEnableCount /* in, out */
            )
        {
            if (Interlocked.CompareExchange(ref enableCount,
                    savedEnableCount, 0) == 0)
            {
                savedEnableCount = 0;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to populate the cache slot at the specified
        /// index with a newly allocated <see cref="StringBuilder" /> instance,
        /// if that slot is currently empty.
        /// </summary>
        /// <param name="index">
        /// The cache slot index to populate.
        /// </param>
        /// <returns>
        /// True if a new instance was allocated and stored in the slot;
        /// otherwise, false.
        /// </returns>
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
                    int capacity = IndexToCapacity(index, false);

                    if (capacity > 0)
                    {
                        instance = AllocateNew(
                            null, Index.Invalid, Length.Invalid, capacity);

                        if ((instance != null) && Object.ReferenceEquals(
                                null, Interlocked.CompareExchange(
                                ref instances[index], instance, null)))
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

        /// <summary>
        /// This method attempts to acquire the <see cref="StringBuilder" />
        /// instance from the cache slot at the specified index and clear its
        /// contents, discarding it instead of returning it to the cache.
        /// </summary>
        /// <param name="index">
        /// The cache slot index to clear.
        /// </param>
        /// <returns>
        /// True if an instance was acquired and cleared; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to acquire a <see cref="StringBuilder" />
        /// instance from the cache slot at the specified index.
        /// </summary>
        /// <param name="index">
        /// The cache slot index to acquire from.
        /// </param>
        /// <param name="builder">
        /// On input, this should be null; if it is already non-null, no
        /// acquisition is performed.  Upon success, receives the acquired
        /// <see cref="StringBuilder" /> instance; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if an instance was acquired from the slot; otherwise, false.
        /// </returns>
        private static bool TryAcquireFrom(
            int index,                /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            if (builder == null)
            {
                StringBuilder instance = Interlocked.Exchange(
                    ref instances[index], null);

                if (instance != null)
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

        /// <summary>
        /// This method attempts to release a <see cref="StringBuilder" />
        /// instance back to the cache slot at the specified index, if that slot
        /// is currently empty.
        /// </summary>
        /// <param name="index">
        /// The cache slot index to release to.
        /// </param>
        /// <param name="builder">
        /// On input, the <see cref="StringBuilder" /> instance to release.  Upon
        /// success, this value is reset to null; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if the instance was released to the slot; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method computes the base index offset derived from the minimum
        /// capacity, used when converting between capacity and slot index
        /// values.
        /// </summary>
        /// <returns>
        /// The base index offset, or zero if there is no minimum capacity.
        /// </returns>
        private static int GetIndexOffset()
        {
            return (MinimumCapacity > 0) ?
                MathOps.Log2(MinimumCapacity) : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified capacity, in characters, into the
        /// corresponding cache slot index.
        /// </summary>
        /// <param name="capacity">
        /// The capacity, in characters, to convert.
        /// </param>
        /// <returns>
        /// The cache slot index corresponding to the specified capacity.
        /// </returns>
        private static int CapacityToIndex(
            int capacity /* in */
            )
        {
            return MathOps.Log2(capacity) - GetIndexOffset() - ExtraOffset;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified cache slot index into the
        /// corresponding capacity, in characters.
        /// </summary>
        /// <param name="index">
        /// The cache slot index to convert.
        /// </param>
        /// <returns>
        /// The capacity, in characters, corresponding to the specified slot
        /// index, or null if it could not be computed.
        /// </returns>
        private static ulong? IndexToCapacity(
            int index /* in */
            )
        {
            return MathOps.Pow2(index + GetIndexOffset() + ExtraOffset);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the cache slot index at which operations for
        /// the specified capacity should begin, taking into account any fixed
        /// capacity or preferred starting index.
        /// </summary>
        /// <param name="capacity">
        /// The capacity, in characters, to convert.
        /// </param>
        /// <param name="release">
        /// Non-zero if the resulting index will be used for a release
        /// operation.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The cache slot index at which to begin operations for the specified
        /// capacity.
        /// </returns>
        private static int CapacityToIndex(
            int capacity, /* in */
            bool release  /* in: NOT USED */
            )
        {
            int startIndex = 0; /* REUSED */
            int length = GetLength();

            if (length == 0)
                return startIndex;

            if (FixedCapacity > 0)
                return startIndex;

            startIndex = Interlocked.CompareExchange(
                ref PreferStartIndex, 0, 0);

            if ((startIndex >= 0) && (startIndex < length))
                return startIndex;

            if ((capacity <= 0) || (capacity == MinimumCapacity))
                return 0;

            startIndex = CapacityToIndex(capacity);

            if ((startIndex < 0) || (startIndex >= length))
                startIndex = 0;

            return startIndex;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the capacity, in characters, associated with
        /// the specified cache slot index, taking into account any fixed
        /// capacity.
        /// </summary>
        /// <param name="index">
        /// The cache slot index to convert.
        /// </param>
        /// <param name="release">
        /// Non-zero if the resulting capacity will be used for a release
        /// operation.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The capacity, in characters, associated with the specified slot
        /// index, or zero if it could not be computed.
        /// </returns>
        private static int IndexToCapacity(
            int index,   /* in */
            bool release /* in: NOT USED */
            )
        {
            if (FixedCapacity > 0)
            {
                return FixedCapacity;
            }
            else
            {
                ulong? capacity = IndexToCapacity(index);

                if (capacity == null)
                    return 0;

                if ((ulong)capacity > int.MaxValue)
                    return 0;

                return (int)capacity;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of slots in this cache.
        /// </summary>
        /// <returns>
        /// The number of slots in this cache.
        /// </returns>
        private static int GetLength()
        {
            return instances.Length; /* SAFE: READ-ONLY */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire a <see cref="StringBuilder" />
        /// instance suitable for the specified capacity, using either the first
        /// matching slot or any available slot depending on configuration.
        /// </summary>
        /// <param name="capacity">
        /// The desired capacity, in characters.
        /// </param>
        /// <param name="builder">
        /// On input, this should be null.  Upon success, receives the acquired
        /// <see cref="StringBuilder" /> instance; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if an instance was acquired; otherwise, false.
        /// </returns>
        private static bool TryAcquire(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            return TryForAny ?
                TryAcquireAny(capacity, ref builder) :
                TryAcquireFirst(capacity, ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire a <see cref="StringBuilder" />
        /// instance from the first cache slot matching the specified capacity.
        /// </summary>
        /// <param name="capacity">
        /// The desired capacity, in characters.
        /// </param>
        /// <param name="builder">
        /// On input, this should be null.  Upon success, receives the acquired
        /// <see cref="StringBuilder" /> instance; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if an instance was acquired; otherwise, false.
        /// </returns>
        private static bool TryAcquireFirst(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            int index = CapacityToIndex(capacity, false);

            if (TryAcquireFrom(index, ref builder))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire a <see cref="StringBuilder" />
        /// instance from any cache slot at or after the one matching the
        /// specified capacity.
        /// </summary>
        /// <param name="capacity">
        /// The desired capacity, in characters.
        /// </param>
        /// <param name="builder">
        /// On input, this should be null.  Upon success, receives the acquired
        /// <see cref="StringBuilder" /> instance; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if an instance was acquired; otherwise, false.
        /// </returns>
        private static bool TryAcquireAny(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            int startIndex = CapacityToIndex(capacity, false);
            int length = GetLength();

            for (int index = startIndex; index < length; index++)
                if (TryAcquireFrom(index, ref builder))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to release a <see cref="StringBuilder" />
        /// instance suitable for the specified capacity, using either the first
        /// matching slot or any available slot depending on configuration.
        /// </summary>
        /// <param name="capacity">
        /// The capacity, in characters, of the instance being released.
        /// </param>
        /// <param name="builder">
        /// On input, the <see cref="StringBuilder" /> instance to release.  Upon
        /// success, this value is reset to null; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if the instance was released; otherwise, false.
        /// </returns>
        private static bool TryRelease(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            return TryForAny ?
                TryReleaseAny(capacity, ref builder) :
                TryReleaseFirst(capacity, ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to release a <see cref="StringBuilder" />
        /// instance to the first cache slot matching the specified capacity.
        /// </summary>
        /// <param name="capacity">
        /// The capacity, in characters, of the instance being released.
        /// </param>
        /// <param name="builder">
        /// On input, the <see cref="StringBuilder" /> instance to release.  Upon
        /// success, this value is reset to null; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if the instance was released; otherwise, false.
        /// </returns>
        private static bool TryReleaseFirst(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            int index = CapacityToIndex(capacity, true);

            if (TryReleaseTo(index, ref builder))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to release a <see cref="StringBuilder" />
        /// instance to any cache slot at or after the one matching the specified
        /// capacity.
        /// </summary>
        /// <param name="capacity">
        /// The capacity, in characters, of the instance being released.
        /// </param>
        /// <param name="builder">
        /// On input, the <see cref="StringBuilder" /> instance to release.  Upon
        /// success, this value is reset to null; upon failure, it is left
        /// unchanged.  This parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// True if the instance was released; otherwise, false.
        /// </returns>
        private static bool TryReleaseAny(
            int capacity,             /* in */
            ref StringBuilder builder /* in, out: OPTIONAL */
            )
        {
            int startIndex = CapacityToIndex(capacity, true);
            int length = GetLength();

            for (int index = startIndex; index < length; index++)
                if (TryReleaseTo(index, ref builder))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to populate every empty cache slot with a newly
        /// allocated <see cref="StringBuilder" /> instance.
        /// </summary>
        /// <returns>
        /// The number of cache slots that were populated.
        /// </returns>
        private static long TryPopulate()
        {
            long count = 0;
            int length = GetLength();

            for (int index = 0; index < length; index++)
                if (TryPopulateAt(index))
                    count++;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to clear and discard the cached
        /// <see cref="StringBuilder" /> instance from every cache slot.
        /// </summary>
        /// <returns>
        /// The number of cache slots that were cleared.
        /// </returns>
        private static int TryClear()
        {
            int count = 0;
            int length = GetLength();

            for (int index = 0; index < length; index++)
                if (TryClearAt(index))
                    count++;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the event used to stop capacity optimization
        /// threads, creating it first if it does not already exist.
        /// </summary>
        /// <returns>
        /// The event used to stop capacity optimization threads, or null if it
        /// could not be created.
        /// </returns>
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

        /// <summary>
        /// This method attempts to optimize each cached
        /// <see cref="StringBuilder" /> instance by ensuring it has at least the
        /// capacity associated with its slot.
        /// </summary>
        /// <returns>
        /// The number of cached instances that were optimized.
        /// </returns>
        private static long TryOptimize()
        {
            long count = 0;
            int length = GetLength();

            for (int index = 0; index < length; index++)
            {
                StringBuilder builder = null;

                try
                {
                    if (TryAcquireFrom(index, ref builder))
                    {
                        int capacity = IndexToCapacity(index, false);

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
                            DebugTraceAlwaysNoCache(String.Format(
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

        /// <summary>
        /// This method conditionally forces a full garbage collection when
        /// collection has been enabled for this cache.
        /// </summary>
        /// <returns>
        /// The total number of bytes believed to be allocated after collection,
        /// or null if collection was not performed.
        /// </returns>
        private static long? TryCollect()
        {
            if (Interlocked.CompareExchange(ref collectCount, 0, 0) > 0)
                return ObjectOps.GetTotalMemory(true);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a trace priority that is escalated based on
        /// whether the specified counts are greater than zero.
        /// </summary>
        /// <param name="count1">
        /// The first count to consider; a positive value escalates the
        /// resulting priority.
        /// </param>
        /// <param name="count2">
        /// The second count to consider; a positive value escalates the
        /// resulting priority.
        /// </param>
        /// <returns>
        /// The computed <see cref="TracePriority" /> value.
        /// </returns>
        private static TracePriority GetTracePriority(
            long count1, /* in */
            long count2  /* in */
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

        /// <summary>
        /// This method emits a diagnostic trace message with use of this cache
        /// temporarily disabled, to avoid re-entrancy while tracing.
        /// </summary>
        /// <param name="message">
        /// The trace message to emit.
        /// </param>
        /// <param name="category">
        /// The category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The priority associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTraceAlwaysNoCache(
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            int savedEnableCount;

            /* IGNORED */
            BeginNoCache(out savedEnableCount);

            try
            {
                TraceOps.DebugTraceAlways(message, category, priority);
            }
            finally
            {
                /* IGNORED */
                EndNoCache(ref savedEnableCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits a diagnostic trace of the specified exception with
        /// use of this cache temporarily disabled, to avoid re-entrancy while
        /// tracing.
        /// </summary>
        /// <param name="exception">
        /// The exception to trace.
        /// </param>
        /// <param name="category">
        /// The category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The priority associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTraceAlwaysNoCache(
            Exception exception,   /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            int savedEnableCount;

            /* IGNORED */
            BeginNoCache(out savedEnableCount);

            try
            {
                TraceOps.DebugTraceAlways(exception, category, priority);
            }
            finally
            {
                /* IGNORED */
                EndNoCache(ref savedEnableCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits a formatted diagnostic trace message with use of
        /// this cache temporarily disabled, to avoid re-entrancy while tracing.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method associated with the trace message.
        /// </param>
        /// <param name="message">
        /// The trace message to emit.
        /// </param>
        /// <param name="category">
        /// The category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The priority associated with the trace message.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to allow the formatted parameter values to be truncated with
        /// an ellipsis.
        /// </param>
        /// <param name="parameters">
        /// The optional array of parameter values to include in the trace
        /// message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        private static void DebugTraceNoCache(
            string methodName,         /* in */
            string message,            /* in */
            string category,           /* in */
            TracePriority priority,    /* in */
            bool ellipsis,             /* in */
            params object[] parameters /* in */
            )
        {
            int savedEnableCount;

            /* IGNORED */
            BeginNoCache(out savedEnableCount);

            try
            {
                TraceOps.DebugTrace(
                    methodName, message, category, priority, ellipsis,
                    parameters);
            }
            finally
            {
                /* IGNORED */
                EndNoCache(ref savedEnableCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the entry point for the background thread that
        /// periodically optimizes, populates, and trims the cached
        /// <see cref="StringBuilder" /> instances until it is signaled to stop.
        /// </summary>
        /// <param name="obj">
        /// The state object passed to the thread, which must be the
        /// <see cref="EventWaitHandle" /> used to signal the thread to stop.
        /// </param>
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

                ulong count0 = 0;
                TracePriority priority; /* REUSED */

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

                    long count1 = TryOptimize();

                    if (count1 > 0)
                        count0 += (ulong)count1;

                    long count2 = TryPopulate();

                    if (count2 > 0)
                        count0 += (ulong)count2;

                    long? count3 = TryCollect();

                    if ((count3 != null) && ((long)count3 > 0))
                        count0++; /* NOTE: We did something. */

#if DEBUG && VERBOSE
                    priority = GetTracePriority(count1, count2);

                    DebugTraceNoCache(
                        "ThreadStart", null,
                        typeof(StringBuilderCache).Name,
                        priority, false, "TryOptimize",
                        count1, "TryPopulate", count2,
                        "TryCollect", count3);
#endif
                }

                priority = (count0 > 0) ?
                    TracePriority.CacheDebug2 :
                    TracePriority.CacheDebug;

                DebugTraceNoCache(
                    "ThreadStart", null,
                    typeof(StringBuilderCache).Name,
                    priority, false, "TryTotal",
                    count0);
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
                DebugTraceAlwaysNoCache(
                    e, typeof(StringBuilderCache).Name,
                    TracePriority.ThreadError);
            }
            finally
            {
                Interlocked.Decrement(ref ThreadPending);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the contents of the specified
        /// <see cref="StringBuilder" /> instance, if it is not null.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder" /> instance to clear.  This parameter
        /// may be null.
        /// </param>
        private static void ClearExisting(
            StringBuilder builder /* in */
            )
        {
            if (builder != null)
                builder.Length = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the specified <see cref="StringBuilder" />
        /// instance has at least the specified capacity, growing it if
        /// necessary.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder" /> instance to check.  This parameter
        /// may be null.
        /// </param>
        /// <param name="capacity">
        /// The minimum required capacity, in characters.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the capacity was grown to meet the requirement; otherwise,
        /// false.
        /// </returns>
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
                    (builder.EnsureCapacity(newCapacity) >= newCapacity))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends all or a portion of the specified string value
        /// to the specified <see cref="StringBuilder" /> instance.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder" /> instance to append to.  This
        /// parameter may be null.
        /// </param>
        /// <param name="value">
        /// The string value to append.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The starting character index within the value to append from.  This
        /// parameter is optional.
        /// </param>
        /// <param name="length">
        /// The number of characters to append from the value.  This parameter is
        /// optional.
        /// </param>
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

        /// <summary>
        /// This method allocates a new <see cref="StringBuilder" /> instance,
        /// optionally initialized with all or a portion of the specified string
        /// value and the specified capacity.
        /// </summary>
        /// <param name="value">
        /// The initial string value.  This parameter is optional and may be
        /// null.
        /// </param>
        /// <param name="startIndex">
        /// The starting character index within the value.  This parameter is
        /// optional.
        /// </param>
        /// <param name="length">
        /// The number of characters from the value to use.  This parameter is
        /// optional.
        /// </param>
        /// <param name="capacity">
        /// The initial capacity, in characters.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <returns>
        /// The newly allocated <see cref="StringBuilder" /> instance.
        /// </returns>
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
        /// <summary>
        /// This method acquires a <see cref="StringBuilder" /> instance, reusing
        /// one from the cache when possible and otherwise allocating a new one,
        /// optionally initialized with all or a portion of the specified string
        /// value and the specified capacity.
        /// </summary>
        /// <param name="value">
        /// The initial string value.  This parameter is optional and may be
        /// null.
        /// </param>
        /// <param name="startIndex">
        /// The starting character index within the value.  This parameter is
        /// optional.
        /// </param>
        /// <param name="length">
        /// The number of characters from the value to use.  This parameter is
        /// optional.
        /// </param>
        /// <param name="capacity">
        /// The desired capacity, in characters.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <returns>
        /// A <see cref="StringBuilder" /> instance, either reused from the cache
        /// or newly allocated.
        /// </returns>
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
                                DebugTraceAlwaysNoCache(String.Format(
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

        /// <summary>
        /// This method releases the specified <see cref="StringBuilder" />
        /// instance back to the cache when possible, otherwise clearing and
        /// discarding it.
        /// </summary>
        /// <param name="builder">
        /// On input, the <see cref="StringBuilder" /> instance to release.  Upon
        /// return, this value is reset to null.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the instance was released back to the cache; otherwise,
        /// false.
        /// </returns>
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
#if DEBUG && VERBOSE
                            else
                            {
                                DebugTraceAlwaysNoCache(String.Format(
                                    "Release: cannot release {0}",
                                    RuntimeOps.GetHashCode(builder)),
                                    typeof(StringBuilderCache).Name,
                                    TracePriority.CleanupError2);
                            }
#endif
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

        /// <summary>
        /// This method returns the string contents of the specified
        /// <see cref="StringBuilder" /> instance and then releases it back to
        /// the cache.
        /// </summary>
        /// <param name="builder">
        /// On input, the <see cref="StringBuilder" /> instance whose contents
        /// are returned.  Upon return, this value is reset to null.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <returns>
        /// The string contents of the instance, or null if it was null.
        /// </returns>
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

        /// <summary>
        /// This method enables or disables use of this cache, or queries its
        /// current enabled state.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the cache, zero to disable it, or null to query
        /// its current enabled state without changing it.  This parameter is
        /// optional.
        /// </param>
        /// <returns>
        /// True if the cache is enabled after the operation; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method enables or disables garbage collection by the capacity
        /// optimization thread, or queries its current state.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable collection, zero to disable it, or null to query
        /// its current state without changing it.  This parameter is optional.
        /// </param>
        /// <returns>
        /// True if collection is enabled after the operation; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method starts or stops the background capacity optimization
        /// thread, or queries whether one is currently pending.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to start the thread, zero to stop it, or null to query
        /// whether one is currently pending without changing it.  This parameter
        /// is optional.
        /// </param>
        /// <returns>
        /// True if a capacity optimization thread is pending after the
        /// operation; otherwise, false.
        /// </returns>
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
                            success = ThreadOps.QueueUserWorkItem(
                                ThreadStart, stopEvent, false);
                        }
                        finally
                        {
                            if (!success)
                                Interlocked.Decrement(ref ThreadPending);
                        }
                    }
                    else
                    {
                        Interlocked.Decrement(ref ThreadPending);
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

        /// <summary>
        /// This method clears and discards all cached
        /// <see cref="StringBuilder" /> instances from this cache.
        /// </summary>
        /// <returns>
        /// The number of cached instances that were cleared.
        /// </returns>
        public static int Clear()
        {
            return TryClear();
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        /// <summary>
        /// This method resets all overall and per-slot statistics counters for
        /// this cache to zero.
        /// </summary>
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

        /// <summary>
        /// This method saves a snapshot of all overall and per-slot statistics
        /// counters for this cache, optionally resetting them afterward.
        /// </summary>
        /// <param name="flags">
        /// The cache flags used as the key under which the snapshot is stored.
        /// </param>
        /// <param name="move">
        /// Non-zero to reset the counters to zero after saving them.
        /// </param>
        /// <param name="savedCacheCounts">
        /// On input, the dictionary of saved counter snapshots, which is created
        /// if null.  Upon return, contains the saved snapshot keyed by the
        /// specified flags.
        /// </param>
        /// <returns>
        /// True if the snapshot was saved; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method restores a previously saved snapshot of the statistics
        /// counters for this cache, optionally merging it with the current
        /// values and optionally removing the snapshot afterward.
        /// </summary>
        /// <param name="flags">
        /// The cache flags used as the key under which the snapshot was stored.
        /// </param>
        /// <param name="merge">
        /// Non-zero to add the saved values to the current counters; otherwise,
        /// the current counters are overwritten with the saved values.
        /// </param>
        /// <param name="move">
        /// Non-zero to remove the snapshot after restoring it.
        /// </param>
        /// <param name="savedCacheCounts">
        /// On input, the dictionary of saved counter snapshots.  Upon return, it
        /// may have the restored snapshot removed when requested.
        /// </param>
        /// <returns>
        /// True if a snapshot was found and restored; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method adds the overall and, optionally, per-slot statistics
        /// counters for this cache to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the statistics are added.  This parameter may be
        /// null, in which case nothing is added.
        /// </param>
        /// <param name="summaryOnly">
        /// Non-zero to add only the overall counters, omitting the per-slot
        /// counters.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include counters whose value is zero.
        /// </param>
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
        /// <summary>
        /// This method adds diagnostic information about this cache, including
        /// its configuration, current slot contents, and statistics, to the
        /// specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the information is added.  This parameter may be
        /// null, in which case nothing is added.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control the amount of detail included.
        /// </param>
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

            count = Interlocked.CompareExchange(ref FixedCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("FixedCapacity", count.ToString());

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

            count = Interlocked.CompareExchange(ref PreferStartIndex, 0, 0);

            if (empty || (count != 0))
                localList.Add("PreferStartIndex", count.ToString());

            length = GetLength();

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
                    if ((builder != null) &&
                        !TryReleaseTo(index, ref builder))
                    {
                        DebugTraceAlwaysNoCache(String.Format(
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
