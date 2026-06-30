/*
 * IntPtrTypeListDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if !CACHE_DICTIONARY
using System.Collections.Generic;
#endif

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

#if CACHE_STATISTICS
using System.Threading;
#endif

using Eagle._Attributes;

#if CACHE_STATISTICS
using Eagle._Components.Private;
using Eagle._Components.Public;
#endif

using Eagle._Containers.Public;

#if CACHE_STATISTICS
using Eagle._Interfaces.Private;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps native pointer values to
    /// instances of <see cref="TypeList" />.  When cache statistics are enabled,
    /// it also tracks per-operation counts via the
    /// <see cref="ICacheCounts" /> interface.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("5bdee0d3-1eb8-47c2-bac8-81b9d178fcd2")]
    internal sealed class IntPtrTypeListDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<IntPtr, TypeList>
#elif FAST_DICTIONARY
        FastDictionary<IntPtr, TypeList>
#else
        Dictionary<IntPtr, TypeList>
#endif
#if CACHE_STATISTICS
        , ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        /// <summary>
        /// The array of per-operation cache counts, indexed by
        /// <see cref="CacheCountType" />.
        /// </summary>
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public IntPtrTypeListDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private IntPtrTypeListDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICacheCounts Members
#if CACHE_STATISTICS
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
        /// This method determines whether any cache counts are present.
        /// </summary>
        /// <returns>
        /// True if cache counts are available; otherwise, false.
        /// </returns>
        public bool HaveCacheCounts()
        {
            if (this.Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the array of current cache counts.
        /// </summary>
        /// <returns>
        /// The array of cache counts, or null if none are available.
        /// </returns>
        public long[] GetCacheCounts()
        {
            return cacheCounts;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all cache counts to zero.
        /// </summary>
        /// <returns>
        /// True if the counts were reset; otherwise, false.
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
        /// This method formats the current cache counts into a human-readable
        /// string.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include counts that are zero in the formatted result.
        /// </param>
        /// <returns>
        /// The formatted string representation of the cache counts.
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
