/*
 * CacheCounts.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface defines methods used to track and report the per-type
    /// cache hit, miss, and related counts for an entity that participates in
    /// one of the Eagle engine caches.
    /// </summary>
    [ObjectId("6b2625db-fb9b-49a1-8bc3-8a1e54707efe")]
    internal interface ICacheCounts
    {
        /// <summary>
        /// This method increments the cache count associated with the
        /// specified cache count type.
        /// </summary>
        /// <param name="type">
        /// The category of cache count to increment.
        /// </param>
        /// <returns>
        /// True if the count was incremented; otherwise, false.
        /// </returns>
        bool IncrementCacheCount(CacheCountType type);

        /// <summary>
        /// This method determines whether any cache counts are present.
        /// </summary>
        /// <returns>
        /// True if cache counts are available; otherwise, false.
        /// </returns>
        bool HaveCacheCounts();

        /// <summary>
        /// This method returns the array of current cache counts.
        /// </summary>
        /// <returns>
        /// The array of cache counts, or null if none are available.
        /// </returns>
        long[] GetCacheCounts();

        /// <summary>
        /// This method resets all cache counts to zero.
        /// </summary>
        /// <returns>
        /// True if the counts were reset; otherwise, false.
        /// </returns>
        bool ZeroCacheCounts();

        /// <summary>
        /// This method replaces or merges the cache counts with the supplied
        /// values.
        /// </summary>
        /// <param name="counts">
        /// The array of cache count values to use.
        /// </param>
        /// <param name="merge">
        /// Non-zero to add the supplied values to the existing counts; zero to
        /// replace the existing counts.
        /// </param>
        /// <returns>
        /// True if the counts were set; otherwise, false.
        /// </returns>
        bool SetCacheCounts(long[] counts, bool merge);

        /// <summary>
        /// This method formats the current cache counts into a human-readable
        /// string.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include counts that are zero in the formatted result.
        /// </param>
        /// <returns>
        /// The formatted string representation of the cache counts.
        /// </returns>
        string CacheCountsToString(bool empty);
    }
}
