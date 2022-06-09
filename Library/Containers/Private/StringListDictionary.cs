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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d4e35240-1862-4fc6-9a6a-56fa059031b5")]
    internal sealed class StringListDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<string, StringList>
#else
        Dictionary<string, StringList>
#endif
#if CACHE_STATISTICS
        , ICacheCounts
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        private long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringListDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public StringListDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringListDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringListDictionary(
            int capacity,
            IEqualityComparer<string> comparer
            )
            : base(capacity, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
            if (this.Count > 0)
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
                Characters.Space.ToString(), null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
