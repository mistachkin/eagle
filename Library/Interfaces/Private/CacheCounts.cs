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
    [ObjectId("6b2625db-fb9b-49a1-8bc3-8a1e54707efe")]
    internal interface ICacheCounts
    {
        bool IncrementCacheCount(CacheCountType type);

        bool HaveCacheCounts();
        long[] GetCacheCounts();

        bool ZeroCacheCounts();
        bool SetCacheCounts(long[] counts, bool merge);

        string CacheCountsToString(bool empty);
    }
}
