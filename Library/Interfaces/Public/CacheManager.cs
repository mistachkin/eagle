/*
 * CacheManager.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("01a30269-0e47-4158-8451-45316e95cb50")]
    public interface ICacheManager
    {
        ///////////////////////////////////////////////////////////////////////
        // CACHE MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        bool AreCachesEnabled(CacheFlags flags);
        int ClearCaches(CacheFlags flags, bool enable);
        CacheFlags EnableCaches(CacheFlags flags, bool enable);
        CacheFlags ControlCaches(CacheFlags flags, bool enable);
    }
}
