/*
 * StringListDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION || CACHE_STATISTICS
using System;
#endif

using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;

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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to lists of
    /// strings (<see cref="StringList" />).  It extends the underlying generic
    /// dictionary with helpers for merging values, filtering, producing a
    /// string form of its entries, and (optionally) tracking cache usage
    /// statistics.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d4e35240-1862-4fc6-9a6a-56fa059031b5")]
    internal sealed class StringListDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<string, StringList>
#elif FAST_DICTIONARY
        FastDictionary<string, StringList>
#else
        Dictionary<string, StringList>
#endif
#if CACHE_STATISTICS
        , ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        /// <summary>
        /// The array of cache usage counts, indexed by the values of the
        /// <see cref="CacheCountType" /> enumeration.
        /// </summary>
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty string list dictionary.
        /// </summary>
        public StringListDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty string list dictionary with the specified
        /// initial capacity, optionally using the shared string comparer.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries the dictionary can contain before
        /// resizing is required.
        /// </param>
        /// <param name="cache">
        /// Non-zero to use the shared string comparer suitable for cache use;
        /// otherwise, the default comparer is used.
        /// </param>
        public StringListDictionary(
            int capacity,
            bool cache
            )
            : base(capacity, cache ? new _Comparers.StringObject() : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// Constructs a string list dictionary that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public StringListDictionary(
            IDictionary<string, StringList> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an empty string list dictionary that uses the specified
        /// equality comparer for its keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public StringListDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty string list dictionary with the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries the dictionary can contain before
        /// resizing is required.
        /// </param>
        public StringListDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty string list dictionary with the specified
        /// initial capacity that uses the specified equality comparer for its
        /// keys.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries the dictionary can contain before
        /// resizing is required.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public StringListDictionary(
            int capacity,
            IEqualityComparer<string> comparer
            )
            : base(capacity, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string list dictionary that is initialized with the
        /// entries copied from the specified dictionary and uses the specified
        /// equality comparer for its keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public StringListDictionary(
            IDictionary<string, StringList> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a string list dictionary from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private StringListDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method adds the specified key and value to the dictionary and
        /// returns the resulting value stored for that key.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The list of strings to associate with the specified key.
        /// </param>
        /// <param name="reserved">
        /// This parameter is reserved for future use and is ignored.
        /// </param>
        /// <returns>
        /// The value stored in the dictionary for the specified key.
        /// </returns>
        public StringList Add(
            string key,
            StringList value,
            bool reserved
            )
        {
            Add(key, value);

            return this[key];
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method merges the specified list of strings into the entry
        /// associated with the specified key.  If the key is not already
        /// present, a new entry is added; otherwise, the specified values are
        /// appended to the existing list.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to merge into.
        /// </param>
        /// <param name="value">
        /// The list of strings to merge into the entry.  This parameter may be
        /// null, in which case no values are merged.
        /// </param>
        public void Merge(
            string key,
            StringList value
            )
        {
            StringList oldValue;

            if (TryGetValue(key, out oldValue))
            {
                if (value != null)
                {
                    if (oldValue != null)
                        oldValue.AddRange(value);
                    else
                        this[key] = new StringList(value);
                }
            }
            else
            {
                Add(key, (value != null) ?
                    new StringList(value) : null);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new dictionary that contains only the entries
        /// whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select the keys that are included in the result.
        /// This parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The newly created dictionary containing the matching entries.
        /// </returns>
        public StringListDictionary Filter(
            string pattern,
            bool noCase
            )
        {
            StringListDictionary dictionary = new StringListDictionary();

            foreach (KeyValuePair<string, StringList> pair in this)
            {
                if ((pattern == null) ||
                    Parser.StringMatch(null, pair.Key, 0, pattern, 0, noCase))
                {
                    dictionary.Add(pair.Key, pair.Value);
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys that are included in the result.
        /// This parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The matching keys and values formatted as a string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, StringList>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the keys and values
        /// of the dictionary.
        /// </summary>
        /// <returns>
        /// The keys and values of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
