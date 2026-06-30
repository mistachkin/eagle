/*
 * Synchronize.cs --
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
    /// <summary>
    /// This interface is implemented by entities that support synchronized
    /// access via a lock.  It extends <see cref="ISynchronizeBase" /> with
    /// non-blocking lock and unlock operations.
    /// </summary>
    [ObjectId("86534481-590f-49d7-9115-3b3f48580157")]
    public interface ISynchronize : ISynchronizeBase
    {
        ///////////////////////////////////////////////////////////////////////
        // NON-BLOCKING LOCK / UNLOCK
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the lock without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, it is set to false.
        /// </param>
        void TryLock(ref bool locked);
        /// <summary>
        /// Attempts to acquire the lock, waiting for the default amount of
        /// time if it is not immediately available.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, it is set to false.
        /// </param>
        void TryLockWithWait(ref bool locked);
        /// <summary>
        /// Attempts to acquire the lock without blocking and without throwing
        /// an exception on failure.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, it is set to false.
        /// </param>
        void TryLockNoThrow(ref bool locked);
        /// <summary>
        /// Attempts to acquire the lock, waiting up to the specified amount of
        /// time if it is not immediately available.
        /// </summary>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait for the lock to become
        /// available.
        /// </param>
        /// <param name="locked">
        /// Upon return, this is set to true if the lock was acquired;
        /// otherwise, it is set to false.
        /// </param>
        void TryLock(int timeout, ref bool locked);
        /// <summary>
        /// Releases the lock if it was previously acquired.
        /// </summary>
        /// <param name="locked">
        /// On input, indicates whether the lock is currently held; upon
        /// return, this is set to false if the lock was released.
        /// </param>
        void ExitLock(ref bool locked);
    }
}
