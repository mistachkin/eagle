/*
 * SynchronizeSimple.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    //
    // WARNING: This interface is being deprecated, please do not use it, or
    //          any methods from it, for new code.
    //
    /// <summary>
    /// This interface defines a simple, blocking lock/unlock synchronization
    /// contract for an entity.  WARNING: This interface is being deprecated;
    /// please do not use it, or any methods from it, for new code.
    /// </summary>
    [ObjectId("66e23dba-6d3a-4e6a-b215-300f1d2d1c82")]
    public interface ISynchronizeSimple
    {
        /// <summary>
        /// Attempts to acquire the lock for this entity without blocking.
        /// </summary>
        /// <returns>
        /// True if the lock was acquired; otherwise, false.
        /// </returns>
        bool TryLock();
        /// <summary>
        /// Acquires the lock for this entity, blocking until it becomes
        /// available.
        /// </summary>
        void Lock();
        /// <summary>
        /// Releases the lock for this entity previously acquired via
        /// <see cref="TryLock" /> or <see cref="Lock" />.
        /// </summary>
        void Unlock();
    }
}
