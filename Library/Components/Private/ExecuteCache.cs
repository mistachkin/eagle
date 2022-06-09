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
    [ObjectId("5fcf4ba1-d84c-46fc-84a5-14d0f98e014a")]
    internal sealed class ExecuteCache
#if CACHE_STATISTICS
        : ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif

        private ExecuteDictionary cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ExecuteCache()
        {
            cache = new ExecuteDictionary();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Properties
        public int Count
        {
            get { return (cache != null) ? cache.Count : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
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
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICacheCounts Members
#if CACHE_STATISTICS
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

        public bool HaveCacheCounts()
        {
            if (Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public long[] GetCacheCounts()
        {
            return cacheCounts;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
