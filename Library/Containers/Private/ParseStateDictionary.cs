/*
 * ParseStateDictionary.cs --
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

#if !CACHE_DICTIONARY
using System.Collections.Generic;
#endif

#if CACHE_STATISTICS
using System.Threading;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if CACHE_STATISTICS
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary of parser states, keyed by name.  It
    /// extends the configured backing dictionary (cache, fast, or standard)
    /// with a type name suitable for use within Eagle, the ability to be
    /// converted to the Eagle list format, and, when enabled, the tracking of
    /// cache usage statistics.
    /// </summary>
    [ObjectId("40feae99-acda-40ed-9ddd-0d37c75a0859")]
    internal sealed class ParseStateDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<string, IParseState>
#elif FAST_DICTIONARY
        FastDictionary<string, IParseState>
#else
        Dictionary<string, IParseState>
#endif
#if CACHE_STATISTICS
        , ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        /// <summary>
        /// The array of cache usage counts, indexed by
        /// <see cref="CacheCountType" />.
        /// </summary>
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ParseStateDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICacheCounts Members
#if CACHE_STATISTICS
        /// <summary>
        /// Increments the cache usage count of the specified type by one.
        /// </summary>
        /// <param name="type">
        /// The type of cache usage count to increment.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this dictionary has any cache usage information,
        /// either entries or non-zero counts.
        /// </summary>
        /// <returns>
        /// True if there is cache usage information; otherwise, false.
        /// </returns>
        public bool HaveCacheCounts()
        {
            if (this.Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the array of cache usage counts maintained by this dictionary.
        /// </summary>
        /// <returns>
        /// The array of cache usage counts, indexed by
        /// <see cref="CacheCountType" />.
        /// </returns>
        public long[] GetCacheCounts()
        {
            return cacheCounts;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets all of the cache usage counts maintained by this dictionary
        /// to zero.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the cache usage counts maintained by this dictionary, either by
        /// merging with or overwriting the existing counts.
        /// </summary>
        /// <param name="counts">
        /// The array of cache usage counts to set, or null to reset the counts
        /// when not merging.
        /// </param>
        /// <param name="merge">
        /// Non-zero to add the supplied counts to the existing counts; zero to
        /// overwrite the existing counts.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the cache usage information for this dictionary to a string
        /// in the Eagle list format.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include counts whose value is zero in the result.
        /// </param>
        /// <returns>
        /// The string, in the Eagle list format, that represents the cache
        /// usage information for this dictionary.
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys, or null to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string, in the Eagle list format, that represents the matching
        /// keys of this dictionary.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format.
        /// </summary>
        /// <returns>
        /// The string, in the Eagle list format, that represents the keys of
        /// this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
