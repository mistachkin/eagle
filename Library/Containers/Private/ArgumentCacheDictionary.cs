/*
 * ArgumentCacheDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if CACHE_STATISTICS
using System;
#endif

#if !CACHE_DICTIONARY && !FAST_DICTIONARY
using System.Collections.Generic;
#endif

#if CACHE_STATISTICS
using System.Threading;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if CACHE_STATISTICS
using Eagle._Interfaces.Private;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary used to cache argument objects,
    /// keyed by argument.  When cache statistics are enabled, it also tracks
    /// per-operation counts via the <see cref="ICacheCounts" /> interface.
    /// </summary>
    [ObjectId("5a4eacd4-644d-4145-8bb7-f66e7cb08b9b")]
    internal sealed class ArgumentCacheDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<Argument, Argument>
#elif FAST_DICTIONARY
        FastDictionary<Argument, Argument>
#else
        Dictionary<Argument, Argument>
#endif
#if CACHE_STATISTICS
        , ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        /// <summary>
        /// The array of per-operation cache counts, indexed by the values of
        /// the <see cref="CacheCountType" /> enumeration.
        /// </summary>
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class that has the specified
        /// initial capacity and uses the argument comparer for its keys.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new dictionary can initially store.
        /// </param>
        public ArgumentCacheDictionary(
            int capacity
            )
            : base(capacity, new _Comparers._Argument())
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
#if CACHE_STATISTICS
        /// <summary>
        /// This method increments the cache count of the specified type by one.
        /// </summary>
        /// <param name="type">
        /// The type of cache count to increment.
        /// </param>
        /// <returns>
        /// True if the cache count was incremented; otherwise, false.
        /// </returns>
        public bool IncrementCacheCount(
            CacheCountType type
            )
        {
            if (cacheCounts == null)
                return false;

            int length = cacheCounts.Length;
            int index = (int)type;

            if ((index < 0) || (index >= length))
                return false;

            Interlocked.Increment(ref cacheCounts[index]);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this dictionary has any cache
        /// counts, either because it contains entries or because non-zero
        /// counts have been recorded.
        /// </summary>
        /// <returns>
        /// True if there are cache counts; otherwise, false.
        /// </returns>
        public bool HaveCacheCounts()
        {
            if (this.Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the array of per-operation cache counts.
        /// </summary>
        /// <returns>
        /// The array of cache counts, or null if there are none.
        /// </returns>
        public long[] GetCacheCounts()
        {
            return cacheCounts;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all of the per-operation cache counts to zero.
        /// </summary>
        /// <returns>
        /// True if the cache counts were reset; otherwise, false.
        /// </returns>
        public bool ZeroCacheCounts()
        {
            if (cacheCounts != null)
            {
                int length = cacheCounts.Length;

                if (length > 0)
                {
                    Array.Clear(cacheCounts, 0, length);
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the per-operation cache counts, optionally merging
        /// them with the existing counts.
        /// </summary>
        /// <param name="counts">
        /// The array of cache counts to set or merge.  If this is null and
        /// <paramref name="merge" /> is non-zero, no change is made; if this is
        /// null and <paramref name="merge" /> is zero, the counts are reset.
        /// </param>
        /// <param name="merge">
        /// Non-zero to add the specified counts to the existing counts; zero to
        /// overwrite the existing counts.
        /// </param>
        /// <returns>
        /// True if the cache counts were set; otherwise, false.
        /// </returns>
        public bool SetCacheCounts(
            long[] counts,
            bool merge
            )
        {
            if (counts != null)
            {
                int length = counts.Length;

                if (length >= (int)CacheCountType.SizeOf)
                {
                    if (merge)
                    {
                        if (cacheCounts != null)
                        {
                            //
                            // NOTE: Expand to fit any extra data?
                            //
                            if (cacheCounts.Length < length)
                            {
                                Array.Resize(
                                    ref cacheCounts, length);
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Initialize to fit all data?
                            //
                            cacheCounts = new long[length];
                        }

                        //
                        // NOTE: Merge by adding counts together.
                        //       If the array was just created, we
                        //       end up adding zeros to the new
                        //       counts, which is fine.
                        //
                        for (int index = 0; index < length; index++)
                            cacheCounts[index] += counts[index];
                    }
                    else
                    {
                        //
                        // NOTE: Overwrite?  Ok.
                        //
                        cacheCounts = counts;
                    }

                    return true;
                }
            }
            else if (merge)
            {
                //
                // NOTE: Merge with nothing?  Ok.
                //
                return true;
            }
            else
            {
                //
                // NOTE: Reset?  Ok.
                //
                cacheCounts = null;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string describing the cache counts and the
        /// current state of this dictionary.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include counts that are zero in the resulting string.
        /// </param>
        /// <returns>
        /// The string representation of the cache counts.
        /// </returns>
        public string CacheCountsToString(
            bool empty
            )
        {
            return StringList.MakeList(
                "count", this.Count,
#if CACHE_DICTIONARY
                "maximumCount", this.MaximumCount,
                "maximumAccessCount", this.MaximumAccessCount,
#endif
                FormatOps.CacheCounts(cacheCounts, empty));
        }
#endif
        #endregion
    }
}
