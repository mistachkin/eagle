/*
 * StringBuilderFactory.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if CACHE_STATISTICS
using System.Collections.Generic;
#endif

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
    /// <summary>
    /// This class provides factory methods used to create, and optionally
    /// cache and reuse, <see cref="StringBuilder" /> instances on behalf of
    /// the rest of the library.  It also calculates the various capacity
    /// values used when those objects are created and, when compiled with
    /// support for cache statistics, tracks usage counters for introspection.
    /// </summary>
    [ObjectId("22202ba0-a742-4cba-bd99-b4b714840476")]
    internal static class StringBuilderFactory
    {
        #region Private Constants
        //
        // HACK: Calculate the number of bytes that all CLR objects require,
        //       regardless of any other data (fields) that they may contain.
        //
        //       General equation (based on various Internet sources):
        //
        //       SyncBlock (DWORD) + MethodTable (PTR)
        //
        //       Since, by all reports, the initial DWORD is padded for the
        //       64-bit runtime, just use the size of two IntPtr objects.
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        /// <summary>
        /// The approximate number of bytes of overhead required by every CLR
        /// object, regardless of any other data that it may contain.
        /// </summary>
        private static int ObjectOverhead = (2 * IntPtr.Size); /* 8 or 16 */

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: Calculate the number of bytes that all CLR String objects
        //       require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       CharLength (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        /// <summary>
        /// The approximate number of bytes of overhead required by every CLR
        /// <see cref="String" /> object, regardless of its actual length.
        /// </summary>
        private static int StringOverhead = sizeof(uint); /* 4 */
#else
        //
        // HACK: Calculate the number of bytes that all CLR String objects
        //       require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       ByteLength (DWORD) + CharLength (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        /// <summary>
        /// The approximate number of bytes of overhead required by every CLR
        /// <see cref="String" /> object, regardless of its actual length.
        /// </summary>
        private static int StringOverhead = (2 * sizeof(uint)); /* 8 */
#endif

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: Calculate the number of bytes that all CLR StringBuilder
        //       objects require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       ChunkChars (OBJPTR) + ChunkPrevious (OBJPTR) +
        //       ChunkLength (DWORD) + ChunkOffset (DWORD) +
        //       MaxCapacity (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        /// <summary>
        /// The approximate number of bytes of overhead required by every CLR
        /// <see cref="StringBuilder" /> object, regardless of its actual
        /// length.
        /// </summary>
        private static int Overhead =
            (2 * IntPtr.Size) + (3 * sizeof(uint)); /* 20 or 28 */
#else
        //
        // HACK: Calculate the number of bytes that all CLR StringBuilder
        //       objects require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       Thread (PTR) + String (OBJPTR) + MaxCapacity (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        /// <summary>
        /// The approximate number of bytes of overhead required by every CLR
        /// <see cref="StringBuilder" /> object, regardless of its actual
        /// length.
        /// </summary>
        private static int Overhead =
            (2 * IntPtr.Size) + (1 * sizeof(uint)); /* 12 or 20 */
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: How many StringBuilder objects do we want to try and fit on a
        //       single page in memory.  Given the nature of the CLR, this is
        //       approximate, at best (and will likely be wrong in subsequent
        //       versions).
        //
#if NET_40
        /// <summary>
        /// The approximate number of <see cref="StringBuilder" /> objects that
        /// should fit on a single page of memory.
        /// </summary>
        private static int PerPage = 28;
#else
        /// <summary>
        /// The approximate number of <see cref="StringBuilder" /> objects that
        /// should fit on a single page of memory.
        /// </summary>
        private static int PerPage = 32;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: These are the minimum and default initial capacities for
        //         StringBuilder objects created by this class.  This value
        //         can have a significant impact on the performance of the
        //         entire library; therefore, we should try to figure out
        //         the "optimal" value for it.  Unfortunately, so far, no
        //         value has proven to be optimal in all circumstances;
        //         therefore, this field has been changed from read-only to
        //         read-write so that it can be overridden at runtime [via
        //         reflection] as a last resort.
        //
        /// <summary>
        /// The minimum initial capacity used for <see cref="StringBuilder" />
        /// objects created by this class.
        /// </summary>
        private static int MinimumCapacity = GetMinimumCapacity();
        /// <summary>
        /// The default initial capacity used for <see cref="StringBuilder" />
        /// objects created by this class.
        /// </summary>
        private static int DefaultCapacity = MinimumCapacity;
        /// <summary>
        /// The initial capacity used for <see cref="StringBuilder" /> objects
        /// created by this class when the platform page size cannot be
        /// determined.
        /// </summary>
        private static int FallbackCapacity = 50; // TODO: Good default?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if CACHE_STATISTICS
        /// <summary>
        /// The number of <see cref="StringBuilder" /> objects created by this
        /// class.
        /// </summary>
        private static long createCount = 0;
        /// <summary>
        /// The number of <see cref="StringBuilder" /> objects created by this
        /// class without using the shared cache.
        /// </summary>
        private static long noCacheCount = 0;
        /// <summary>
        /// The number of <see cref="StringBuilder" /> objects reset and reused
        /// by this class.
        /// </summary>
        private static long reuseCount = 0;
        /// <summary>
        /// The total length, in characters, of all values used to initialize
        /// <see cref="StringBuilder" /> objects created by this class.
        /// </summary>
        private static long totalLength = 0;
        /// <summary>
        /// The total capacity requested for all <see cref="StringBuilder" />
        /// objects created by this class.
        /// </summary>
        private static long totalCapacity = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The smallest capacity requested for any <see cref="StringBuilder" />
        /// object created by this class.
        /// </summary>
        private static long seenMinimumCapacity = Count.Invalid;
        /// <summary>
        /// The largest capacity requested for any <see cref="StringBuilder" />
        /// object created by this class.
        /// </summary>
        private static long seenMaximumCapacity = Count.Invalid;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Total number of fields used for statistics by this class.
        //
        /// <summary>
        /// The total number of fields used for statistics by this class.
        /// </summary>
        private static readonly int overallCountLength = 7;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Calculates the minimum initial capacity to use for
        /// <see cref="StringBuilder" /> objects created by this class, based on
        /// the platform page size and the estimated per-object overhead.
        /// </summary>
        /// <returns>
        /// The calculated minimum capacity, or the fallback capacity if the
        /// platform page size cannot be determined.
        /// </returns>
        private static int GetMinimumCapacity()
        {
            uint pageSize = PlatformOps.GetPageSize();

            if (pageSize > 0)
            {
                return ((int)pageSize - (PerPage * (ObjectOverhead +
                    StringOverhead + Overhead))) / (sizeof(char) *
                    PerPage);
            }
            else
            {
                return FallbackCapacity;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Tracks the smallest and largest requested capacities (when compiled
        /// with cache statistics support) and raises the requested capacity to
        /// the configured minimum, if necessary.
        /// </summary>
        /// <param name="capacity">
        /// On input, the requested capacity; upon return, the possibly adjusted
        /// capacity.
        /// </param>
        private static void CheckAndMaybeAdjustCapacity(
            ref int capacity /* in, out */
            )
        {
#if CACHE_STATISTICS
            if ((seenMinimumCapacity < 0) ||
                (capacity < seenMinimumCapacity))
            {
                seenMinimumCapacity = capacity;
            }

            if ((seenMaximumCapacity < 0) ||
                (capacity > seenMaximumCapacity))
            {
                seenMaximumCapacity = capacity;
            }
#endif

            if ((MinimumCapacity > 0) &&
                (capacity < MinimumCapacity))
            {
                capacity = MinimumCapacity;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the specified <see cref="StringBuilder" /> to an empty state
        /// and ensures it has at least the specified capacity, so that it can
        /// be reused.
        /// </summary>
        /// <param name="result">
        /// The <see cref="StringBuilder" /> to reset.
        /// </param>
        /// <param name="capacity">
        /// The minimum capacity to ensure.
        /// </param>
        private static void ResetWithCapacity(
            StringBuilder result, /* in */
            int capacity          /* in */
            )
        {
            result.Length = 0;
            result.EnsureCapacity(capacity);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        #region Create with Cache
        /// <summary>
        /// Creates a <see cref="StringBuilder" /> with the default capacity,
        /// using the shared cache if possible.
        /// </summary>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create()
        {
            return Create(
                null, null, Index.Invalid, Length.Invalid, DefaultCapacity,
                false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a <see cref="StringBuilder" /> with the specified capacity,
        /// using the shared cache if possible.
        /// </summary>
        /// <param name="capacity">
        /// The requested initial capacity.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create(
            int capacity /* in */
            )
        {
            return Create(
                null, null, Index.Invalid, Length.Invalid, capacity,
                false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a <see cref="StringBuilder" /> initialized with the specified
        /// value and the default capacity, using the shared cache if possible.
        /// </summary>
        /// <param name="value">
        /// The initial value.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create(
            string value /* in: OPTIONAL */
            )
        {
            return Create(
                null, value, Index.Invalid, Length.Invalid, DefaultCapacity,
                false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a <see cref="StringBuilder" /> initialized with the specified
        /// value and capacity, using the shared cache if possible.
        /// </summary>
        /// <param name="value">
        /// The initial value.  This parameter may be null.
        /// </param>
        /// <param name="capacity">
        /// The requested initial capacity.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create(
            string value, /* in: OPTIONAL */
            int capacity  /* in */
            )
        {
            return Create(
                null, value, Index.Invalid, Length.Invalid, capacity,
                false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets and reuses the specified <see cref="StringBuilder" /> with the
        /// specified capacity, or creates a new one (using the shared cache if
        /// possible) when none is supplied.
        /// </summary>
        /// <param name="result">
        /// The existing <see cref="StringBuilder" /> to reuse.  This parameter
        /// may be null.
        /// </param>
        /// <param name="capacity">
        /// The requested initial capacity.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create(
            StringBuilder result, /* in: OPTIONAL */
            int capacity          /* in */
            )
        {
            return Create(
                result, null, Index.Invalid, Length.Invalid, capacity,
                false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a <see cref="StringBuilder" /> initialized with a substring
        /// of the specified value and the default capacity, using the shared
        /// cache if possible.
        /// </summary>
        /// <param name="value">
        /// The value from which to take the initial substring.  This parameter
        /// may be null.
        /// </param>
        /// <param name="startIndex">
        /// The starting index of the substring within the value.
        /// </param>
        /// <param name="length">
        /// The length of the substring.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create(
            string value,   /* in: OPTIONAL */
            int startIndex, /* in */
            int length      /* in */
            )
        {
            return Create(
                null, value, startIndex, length, DefaultCapacity,
                false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Create without Cache
        /// <summary>
        /// Creates a <see cref="StringBuilder" /> with the default capacity,
        /// bypassing the shared cache.
        /// </summary>
        /// <returns>
        /// The created <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder CreateNoCache()
        {
            return Create(
                null, null, Index.Invalid, Length.Invalid, DefaultCapacity,
                true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a <see cref="StringBuilder" /> with the specified capacity,
        /// bypassing the shared cache.
        /// </summary>
        /// <param name="capacity">
        /// The requested initial capacity.
        /// </param>
        /// <returns>
        /// The created <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder CreateNoCache(
            int capacity /* in */
            )
        {
            return Create(
                null, null, Index.Invalid, Length.Invalid, capacity,
                true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a <see cref="StringBuilder" /> initialized with the specified
        /// value and the default capacity, bypassing the shared cache.
        /// </summary>
        /// <param name="value">
        /// The initial value.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The created <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder CreateNoCache(
            string value /* in: OPTIONAL */
            )
        {
            return Create(
                null, value, Index.Invalid, Length.Invalid, DefaultCapacity,
                true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets and reuses the specified <see cref="StringBuilder" /> with the
        /// specified capacity, or creates a new one (bypassing the shared cache)
        /// when none is supplied.
        /// </summary>
        /// <param name="result">
        /// The existing <see cref="StringBuilder" /> to reuse.  This parameter
        /// may be null.
        /// </param>
        /// <param name="capacity">
        /// The requested initial capacity.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder CreateNoCache(
            StringBuilder result, /* in: OPTIONAL */
            int capacity          /* in */
            )
        {
            return Create(
                result, null, Index.Invalid, Length.Invalid, capacity,
                true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates, reuses, or acquires from the shared cache a
        /// <see cref="StringBuilder" /> with the requested capacity, optionally
        /// initializing it with all or part of the specified value.  This is
        /// the core implementation to which the other overloads delegate, and
        /// it maintains the cache statistics when they are enabled.
        /// </summary>
        /// <param name="result">
        /// The existing <see cref="StringBuilder" /> to reset and reuse.  This
        /// parameter may be null, in which case a new instance is created or
        /// acquired.
        /// </param>
        /// <param name="value">
        /// The value used to initialize the result.  This parameter may be
        /// null.
        /// </param>
        /// <param name="startIndex">
        /// The starting index of the substring within the value, or
        /// <see cref="Index.Invalid" /> to use the entire value.
        /// </param>
        /// <param name="length">
        /// The length of the substring within the value, or
        /// <see cref="Length.Invalid" /> to use the entire value.
        /// </param>
        /// <param name="capacity">
        /// The requested initial capacity.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass the shared cache and always create a new
        /// instance.
        /// </param>
        /// <returns>
        /// The created or reused <see cref="StringBuilder" />.
        /// </returns>
        public static StringBuilder Create(
            StringBuilder result, /* in: OPTIONAL */
            string value,         /* in: OPTIONAL */
            int startIndex,       /* in */
            int length,           /* in */
            int capacity,         /* in */
            bool noCache          /* in */
            )
        {
            if (value == null)
            {
                if (length != Length.Invalid)
                {
#if CACHE_STATISTICS
                    Interlocked.Add(ref totalLength, length);
#endif

                    capacity = Math.Max(length, capacity);
                }

                CheckAndMaybeAdjustCapacity(ref capacity);

                if (result == null)
                {
#if CACHE_STATISTICS
                    Interlocked.Increment(ref createCount);
                    Interlocked.Add(ref totalCapacity, capacity);
#endif

                    if (noCache)
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(ref noCacheCount);
#endif

                        return new StringBuilder(capacity);
                    }
                    else
                    {
                        return StringBuilderCache.Acquire(
                            null, Index.Invalid, Length.Invalid,
                            capacity);
                    }
                }

#if CACHE_STATISTICS
                Interlocked.Increment(ref reuseCount);
#endif

                ResetWithCapacity(result, capacity);

                return result;
            }

            if ((startIndex != Index.Invalid) &&
                (length != Length.Invalid))
            {
#if CACHE_STATISTICS
                Interlocked.Add(ref totalLength, length);
#endif

                capacity = Math.Max(length, capacity);

                CheckAndMaybeAdjustCapacity(ref capacity);

                if (result == null)
                {
#if CACHE_STATISTICS
                    Interlocked.Increment(ref createCount);
                    Interlocked.Add(ref totalCapacity, capacity);
#endif

                    if (noCache)
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(ref noCacheCount);
#endif

                        return new StringBuilder(
                            value, startIndex, length, capacity);
                    }
                    else
                    {
                        return StringBuilderCache.Acquire(
                            value, startIndex, length, capacity);
                    }
                }

#if CACHE_STATISTICS
                Interlocked.Increment(ref reuseCount);
#endif

                ResetWithCapacity(result, capacity);

                result.Append(value, startIndex, length);

                return result;
            }
            else
            {
                length = value.Length;

#if CACHE_STATISTICS
                Interlocked.Add(ref totalLength, length);
#endif

                capacity = Math.Max(length, capacity);

                CheckAndMaybeAdjustCapacity(ref capacity);

                if (result == null)
                {
#if CACHE_STATISTICS
                    Interlocked.Increment(ref createCount);
                    Interlocked.Add(ref totalCapacity, capacity);
#endif

                    if (noCache)
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(ref noCacheCount);
#endif

                        return new StringBuilder(value, capacity);
                    }
                    else
                    {
                        return StringBuilderCache.Acquire(
                            value, Index.Invalid, Length.Invalid,
                            capacity);
                    }
                }

#if CACHE_STATISTICS
                Interlocked.Increment(ref reuseCount);
#endif

                ResetWithCapacity(result, capacity);

                result.Append(value);

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
#if CACHE_STATISTICS
        /// <summary>
        /// Resets all of the cache statistics counters maintained by this class
        /// to their initial values.
        /// </summary>
        public static void ZeroCounts()
        {
            Interlocked.Exchange(ref createCount, 0);
            Interlocked.Exchange(ref noCacheCount, 0);
            Interlocked.Exchange(ref reuseCount, 0);
            Interlocked.Exchange(ref totalLength, 0);
            Interlocked.Exchange(ref totalCapacity, 0);
            Interlocked.Exchange(ref seenMinimumCapacity, Count.Invalid);
            Interlocked.Exchange(ref seenMaximumCapacity, Count.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Saves a snapshot of the current cache statistics counters into the
        /// supplied dictionary, keyed by the specified cache flags, optionally
        /// resetting the counters afterward.
        /// </summary>
        /// <param name="flags">
        /// The cache flags used as the key under which the snapshot is stored.
        /// </param>
        /// <param name="move">
        /// Non-zero to reset the counters to their initial values after the
        /// snapshot has been saved.
        /// </param>
        /// <param name="savedCacheCounts">
        /// On input, the dictionary into which the snapshot is stored, created
        /// if null; upon return, contains the saved snapshot.
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

            int length = overallCountLength;
            long[] counts = new long[length];

            counts[0] = Interlocked.CompareExchange(ref createCount, 0, 0);
            counts[1] = Interlocked.CompareExchange(ref noCacheCount, 0, 0);
            counts[2] = Interlocked.CompareExchange(ref reuseCount, 0, 0);
            counts[3] = Interlocked.CompareExchange(ref totalLength, 0, 0);
            counts[4] = Interlocked.CompareExchange(ref totalCapacity, 0, 0);
            counts[5] = Interlocked.CompareExchange(ref seenMinimumCapacity, 0, 0);
            counts[6] = Interlocked.CompareExchange(ref seenMaximumCapacity, 0, 0);

            savedCacheCounts[flags] = counts;

            if (move)
                ZeroCounts();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restores a previously saved snapshot of the cache statistics
        /// counters from the supplied dictionary, optionally merging it with
        /// the current counters and removing it from the dictionary.
        /// </summary>
        /// <param name="flags">
        /// The cache flags used as the key under which the snapshot was stored.
        /// </param>
        /// <param name="merge">
        /// Non-zero to add the saved counts to the current counters; zero to
        /// overwrite the current counters with the saved counts.
        /// </param>
        /// <param name="move">
        /// Non-zero to remove the snapshot from the dictionary after it has
        /// been restored.
        /// </param>
        /// <param name="savedCacheCounts">
        /// On input, the dictionary from which the snapshot is restored; upon
        /// return, possibly updated to remove the restored snapshot.
        /// </param>
        /// <returns>
        /// True if the snapshot was restored; otherwise, false.
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
            int haveLength = counts.Length;

            if (haveLength < wantLength)
                return false;

            if (merge)
            {
                Interlocked.Add(ref createCount, counts[0]);
                Interlocked.Add(ref noCacheCount, counts[1]);
                Interlocked.Add(ref reuseCount, counts[2]);
                Interlocked.Add(ref totalLength, counts[3]);
                Interlocked.Add(ref totalCapacity, counts[4]);
                Interlocked.Add(ref seenMinimumCapacity, counts[5]);
                Interlocked.Add(ref seenMaximumCapacity, counts[6]);
            }
            else
            {
                Interlocked.Exchange(ref createCount, counts[0]);
                Interlocked.Exchange(ref noCacheCount, counts[1]);
                Interlocked.Exchange(ref reuseCount, counts[2]);
                Interlocked.Exchange(ref totalLength, counts[3]);
                Interlocked.Exchange(ref totalCapacity, counts[4]);
                Interlocked.Exchange(ref seenMinimumCapacity, counts[5]);
                Interlocked.Exchange(ref seenMaximumCapacity, counts[6]);
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
        /// Adds the current cache statistics counters, as name/value pairs, to
        /// the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the counters are added.  This parameter may be
        /// null, in which case this method does nothing.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include counters whose value is zero; otherwise, only
        /// non-zero counters are included.
        /// </param>
        public static void CountsToList(
            StringPairList list, /* in, out */
            bool empty           /* in */
            )
        {
            if (list == null)
                return;

            long count; /* REUSED */

            count = Interlocked.CompareExchange(ref createCount, 0, 0);

            if (empty || (count != 0))
                list.Add("CreateCount", count.ToString());

            count = Interlocked.CompareExchange(ref noCacheCount, 0, 0);

            if (empty || (count != 0))
                list.Add("NoCacheCount", count.ToString());

            count = Interlocked.CompareExchange(ref reuseCount, 0, 0);

            if (empty || (count != 0))
                list.Add("ReuseCount", count.ToString());

            count = Interlocked.CompareExchange(ref totalLength, 0, 0);

            if (empty || (count != 0))
                list.Add("TotalLength", count.ToString());

            count = Interlocked.CompareExchange(ref totalCapacity, 0, 0);

            if (empty || (count != 0))
                list.Add("TotalCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref seenMinimumCapacity, 0, 0);

            if (empty || (count != 0))
                list.Add("SeenMinimumCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref seenMaximumCapacity, 0, 0);

            if (empty || (count != 0))
                list.Add("SeenMaximumCapacity", count.ToString());
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// Adds introspection information about this factory, including its
        /// configured capacities and (when enabled) cache statistics, to the
        /// specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the information is added.  This parameter may be
        /// null, in which case this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling the level of detail included.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();
            long count; /* REUSED */

            count = Interlocked.CompareExchange(ref MinimumCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("MinimumCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref DefaultCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("DefaultCapacity", count.ToString());

            count = Interlocked.CompareExchange(ref FallbackCapacity, 0, 0);

            if (empty || (count != 0))
                localList.Add("FallbackCapacity", count.ToString());

#if CACHE_STATISTICS
            CountsToList(localList, empty);
#endif

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("StringBuilder Factory");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion
    }
}
