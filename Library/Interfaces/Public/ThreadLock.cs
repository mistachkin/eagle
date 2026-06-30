/*
 * ThreadLock.cs --
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
    /// This interface is implemented by entities that support an explicit,
    /// thread-aware lock, tracking which thread (if any) currently owns the
    /// lock and allowing it to be acquired, released, and queried.
    /// </summary>
    [ObjectId("723276de-6bb2-4067-9fb8-e54e73b23ac2")]
    public interface IThreadLock
    {
        /// <summary>
        /// Gets or sets the identifier of the thread that currently owns the
        /// lock, or null if the lock is not currently held.
        /// </summary>
        long? ThreadId { get; set; }

        /// <summary>
        /// Determines whether the lock is currently held by the calling
        /// thread.
        /// </summary>
        /// <returns>
        /// True if the lock is held by this thread; otherwise, false.
        /// </returns>
        bool IsLocked(); // NOTE: By *this* thread.

        /// <summary>
        /// Acquires the lock for the calling thread.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the lock was acquired; otherwise, false.
        /// </returns>
        bool Lock(ref Result error);
        /// <summary>
        /// Releases the lock previously acquired by the calling thread.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the lock was released; otherwise, false.
        /// </returns>
        bool Unlock(ref Result error);

        /// <summary>
        /// Releases the lock if it is currently held, without failing when the
        /// lock is already unlocked.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the lock was released or was already unlocked; otherwise,
        /// false.
        /// </returns>
        bool MaybeUnlock(ref Result error); // NOTE: Does not fail if already unlocked.

        /// <summary>
        /// Determines whether this lock is currently in a usable state.
        /// </summary>
        /// <returns>
        /// True if the lock is usable; otherwise, false.
        /// </returns>
        bool IsUsable();
        /// <summary>
        /// Determines whether this lock is currently in a usable state.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the lock is usable; otherwise, false.
        /// </returns>
        bool IsUsable(ref Result error);
    }
}
