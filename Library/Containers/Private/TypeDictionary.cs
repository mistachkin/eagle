/*
 * TypeDictionary.cs --
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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("24757152-2455-4f25-8f9e-94d510722fce")]
    internal sealed class TypeDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<string, Type>
#else
        Dictionary<string, Type>
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

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public TypeDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private TypeDictionary(
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

        public bool HaveCacheCounts()
        {
            if (this.Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        public long[] GetCacheCounts()
        {
            return cacheCounts;
        }

        ///////////////////////////////////////////////////////////////////////

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
