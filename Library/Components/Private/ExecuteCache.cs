/*
 * ExecuteCache.cs --
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
using System.Threading;
#endif

using Eagle._Attributes;

#if CACHE_STATISTICS
using Eagle._Components.Public;
#endif

using Eagle._Containers.Private;

#if CACHE_STATISTICS
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class implements a simple cache that maps command (or other
    /// executable entity) names to their associated <see cref="IExecute" />
    /// instances.  When compiled with cache statistics support, it also
    /// tracks usage counts for the various cache operations.
    /// </summary>
    [ObjectId("5fcf4ba1-d84c-46fc-84a5-14d0f98e014a")]
    internal sealed class ExecuteCache
#if CACHE_STATISTICS
        : ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        /// <summary>
        /// The array of usage counts, indexed by <see cref="CacheCountType" />,
        /// recording how many times each cache operation has been performed.
        /// </summary>
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif

        /// <summary>
        /// The underlying dictionary that maps each name to its associated
        /// <see cref="IExecute" /> instance.
        /// </summary>
        private ExecuteDictionary cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty cache with a freshly created backing
        /// dictionary.
        /// </summary>
        public ExecuteCache()
        {
            cache = new ExecuteDictionary();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the number of entries currently contained in the cache.
        /// </summary>
        public int Count
        {
            get { return (cache != null) ? cache.Count : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method removes all entries from the cache.
        /// </summary>
        public void Clear()
        {
            if (cache != null)
            {
                cache.Clear();

#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Clear]);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to look up the cached <see cref="IExecute" />
        /// instance associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the entry to look up.  If this parameter is null, the
        /// lookup fails.
        /// </param>
        /// <param name="validate">
        /// Non-zero to require that the cached instance be non-null in order
        /// for the lookup to succeed; otherwise, zero.
        /// </param>
        /// <param name="execute">
        /// Upon success, this contains the cached <see cref="IExecute" />
        /// instance associated with the name; otherwise, its value is
        /// unspecified.
        /// </param>
        /// <returns>
        /// Non-zero if a matching entry was found in the cache; otherwise,
        /// zero.
        /// </returns>
        public bool TryGet(
            string name,
            bool validate,
            ref IExecute execute
            )
        {
            if ((cache != null) && (name != null))
            {
                if (cache.TryGetValue(name, out execute))
                {
                    if (!validate || (execute != null))
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.Found]);
#endif

                        return true;
                    }
                }
            }

#if CACHE_STATISTICS
            Interlocked.Increment(
                ref cacheCounts[(int)CacheCountType.NotFound]);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a new entry to the cache or updates the existing
        /// entry associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the entry to add or update.  If this parameter is null,
        /// the operation fails.
        /// </param>
        /// <param name="execute">
        /// The <see cref="IExecute" /> instance to associate with the name.
        /// This parameter may be null.
        /// </param>
        /// <param name="invalidate">
        /// Non-zero to first clear the entire cache before adding the entry;
        /// otherwise, zero.
        /// </param>
        /// <returns>
        /// Non-zero if the entry was added or updated; otherwise, zero.
        /// </returns>
        public bool AddOrUpdate(
            string name,
            IExecute execute,
            bool invalidate
            )
        {
            if ((cache != null) && (name != null))
            {
                if (invalidate)
                {
                    cache.Clear();

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Clear]);
#endif
                }
                else if (cache.ContainsKey(name))
                {
                    cache[name] = execute;

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Change]);
#endif

                    return true;
                }

                cache.Add(name, execute);

#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Add]);
#endif

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method renames a cache entry by removing the entry associated
        /// with the old name and adding an entry under the new name.
        /// </summary>
        /// <param name="oldName">
        /// The current name of the entry to rename.  If this parameter is
        /// null, the operation fails.
        /// </param>
        /// <param name="newName">
        /// The new name to associate with the entry.  If this parameter is
        /// null, the operation fails.
        /// </param>
        /// <param name="execute">
        /// The <see cref="IExecute" /> instance to associate with the new
        /// name.  This parameter may be null.
        /// </param>
        /// <param name="invalidate">
        /// Non-zero to first clear the entire cache instead of removing only
        /// the entry associated with the old name; otherwise, zero.
        /// </param>
        /// <returns>
        /// Non-zero if the entry was renamed; otherwise, zero.
        /// </returns>
        public bool Rename(
            string oldName,
            string newName,
            IExecute execute,
            bool invalidate
            )
        {
            if ((cache != null) && (oldName != null) && (newName != null))
            {
                if (invalidate)
                {
                    cache.Clear();

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Clear]);
#endif
                }
                else if (cache.ContainsKey(oldName))
                {
                    if (cache.Remove(oldName))
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.Remove]);
#endif
                    }
                    else
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.NoRemove]);
#endif
                    }
                }

                cache.Add(newName, execute);

#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Add]);
#endif

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the entry associated with the specified name
        /// from the cache.
        /// </summary>
        /// <param name="name">
        /// The name of the entry to remove.  If this parameter is null, the
        /// operation fails.
        /// </param>
        /// <param name="invalidate">
        /// Non-zero to clear the entire cache instead of removing only the
        /// entry associated with the name; otherwise, zero.
        /// </param>
        /// <returns>
        /// Non-zero if the cache was cleared or the entry was removed;
        /// otherwise, zero.
        /// </returns>
        public bool Remove(
            string name,
            bool invalidate
            )
        {
            if ((cache != null) && (name != null))
            {
                if (invalidate)
                {
                    cache.Clear();

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Clear]);
#endif

                    return true;
                }
                else if (cache.ContainsKey(name))
                {
                    if (cache.Remove(name))
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.Remove]);
#endif

                        return true;
                    }
                    else
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.NoRemove]);
#endif
                    }
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICacheCounts Members
#if CACHE_STATISTICS
        /// <summary>
        /// This method increments the usage count associated with the
        /// specified cache operation type.
        /// </summary>
        /// <param name="type">
        /// The <see cref="CacheCountType" /> identifying which usage count to
        /// increment.
        /// </param>
        /// <returns>
        /// Non-zero if the usage count was incremented; otherwise, zero.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any cache usage counts (or cache
        /// entries) are currently present.
        /// </summary>
        /// <returns>
        /// Non-zero if the cache contains entries or any usage count is
        /// non-zero; otherwise, zero.
        /// </returns>
        public bool HaveCacheCounts()
        {
            if (Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the array of cache usage counts, indexed by
        /// <see cref="CacheCountType" />.
        /// </summary>
        /// <returns>
        /// The array of cache usage counts, or null if none are available.
        /// </returns>
        public long[] GetCacheCounts()
        {
            return cacheCounts;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all cache usage counts to zero.
        /// </summary>
        /// <returns>
        /// Non-zero if the usage counts were reset; otherwise, zero.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the cache usage counts, either by merging the
        /// specified counts into the existing counts or by overwriting them.
        /// </summary>
        /// <param name="counts">
        /// The array of usage counts to apply.  If this parameter is null, the
        /// existing counts are either left unchanged (when merging) or cleared
        /// (otherwise).
        /// </param>
        /// <param name="merge">
        /// Non-zero to add the specified counts to the existing counts;
        /// otherwise, the existing counts are replaced by the specified
        /// counts.
        /// </param>
        /// <returns>
        /// Non-zero if the usage counts were updated; otherwise, zero.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the current cache entry count and usage counts
        /// into a human-readable string.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include usage counts that are zero in the resulting
        /// string; otherwise, zero.
        /// </param>
        /// <returns>
        /// A string containing the cache entry count and the formatted usage
        /// counts.
        /// </returns>
        public string CacheCountsToString(
            bool empty
            )
        {
            return StringList.MakeList(
                "count", Count, FormatOps.CacheCounts(cacheCounts, empty));
        }
#endif
        #endregion
    }
}
