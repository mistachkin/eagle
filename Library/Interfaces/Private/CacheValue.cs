/*
 * CacheValue.cs --
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
    /// <summary>
    /// This interface represents an entity that can hold a single cached value
    /// together with the cache generation that the value belongs to.
    /// </summary>
    [ObjectId("5f29ebf4-ab12-4019-982a-915e4c73051c")]
    internal interface ICacheValue
    {
        //
        // WARNING: This property is for private and/or diagnostic use only.
        //
        /// <summary>
        /// Gets the cached value held by this entity.  This property is for
        /// private and/or diagnostic use only.
        /// </summary>
        object CacheValue { get; }

        //
        // WARNING: This property is for private and/or diagnostic use only.
        //
        /// <summary>
        /// Gets the cache generation that the cached value belongs to.  This
        /// property is for private and/or diagnostic use only.
        /// </summary>
        long CacheGeneration { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the cached value, optionally ignoring the
        /// current cache generation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="noGeneration">
        /// Non-zero to return the cached value without regard to the current
        /// cache generation.
        /// </param>
        /// <returns>
        /// The cached value, or null if no valid cached value is available.
        /// </returns>
        object GetCacheValue(
            Interpreter interpreter,
            bool noGeneration
        );

        /// <summary>
        /// This method stores a value into the cache, optionally ignoring the
        /// current cache generation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// The value to store in the cache.  This parameter may be null.
        /// </param>
        /// <param name="noGeneration">
        /// Non-zero to store the value without regard to the current cache
        /// generation.
        /// </param>
        /// <returns>
        /// True if the value was stored; otherwise, false.
        /// </returns>
        bool SetCacheValue(
            Interpreter interpreter,
            object value,
            bool noGeneration
        );
    }
}
