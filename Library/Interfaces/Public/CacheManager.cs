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
    /// <summary>
    /// This interface defines the contract for managing the various internal
    /// caches used by the interpreter.  It provides methods to query whether
    /// caches are enabled, to clear caches, and to enable, disable, or
    /// otherwise control caches, in each case selected by a set of cache flags.
    /// </summary>
    [ObjectId("01a30269-0e47-4158-8451-45316e95cb50")]
    public interface ICacheManager
    {
        ///////////////////////////////////////////////////////////////////////
        // CACHE MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the caches selected by the specified flags are
        /// enabled.
        /// </summary>
        /// <param name="flags">
        /// The flags that select which caches to check.
        /// </param>
        /// <returns>
        /// True if the selected caches are enabled; otherwise, false.
        /// </returns>
        bool AreCachesEnabled(CacheFlags flags);
        /// <summary>
        /// Clears the caches selected by the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that select which caches to clear.
        /// </param>
        /// <param name="enable">
        /// Non-zero to (re-)enable the selected caches after clearing them.
        /// </param>
        /// <returns>
        /// The number of cache entries that were cleared.
        /// </returns>
        int ClearCaches(CacheFlags flags, bool enable);
        /// <summary>
        /// Enables or disables the caches selected by the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that select which caches to enable or disable.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable the selected caches; zero to disable them.
        /// </param>
        /// <returns>
        /// The cache flags in effect after the operation.
        /// </returns>
        CacheFlags EnableCaches(CacheFlags flags, bool enable);
        /// <summary>
        /// Controls the caches selected by the specified flags, adjusting their
        /// configuration.
        /// </summary>
        /// <param name="flags">
        /// The flags that select which caches to control and how.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable the selected behavior; zero to disable it.
        /// </param>
        /// <returns>
        /// The cache flags in effect after the operation.
        /// </returns>
        CacheFlags ControlCaches(CacheFlags flags, bool enable);
    }
}
