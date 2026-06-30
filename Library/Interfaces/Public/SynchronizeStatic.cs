/*
 * SynchronizeStatic.cs --
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
    /// This interface defines the static (type-level) synchronization
    /// contract, exposing non-blocking lock and unlock operations that guard
    /// state shared across all instances of the implementing type.
    /// </summary>
    [ObjectId("df9169f8-ea23-49f3-9f3b-821e85c330ea")]
    public interface ISynchronizeStatic
    {
        ///////////////////////////////////////////////////////////////////////
        // NON-BLOCKING LOCK / UNLOCK
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the static lock without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this will be set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        void StaticTryLock(ref bool locked);
        /// <summary>
        /// Attempts to acquire the static lock, waiting for a default amount
        /// of time for it to become available.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this will be set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        void StaticTryLockWithWait(ref bool locked);
        /// <summary>
        /// Attempts to acquire the static lock, waiting up to the specified
        /// amount of time for it to become available.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time to wait, in milliseconds, for the lock
        /// to become available.
        /// </param>
        /// <param name="locked">
        /// Upon return, this will be set to true if the lock was acquired;
        /// otherwise, false.
        /// </param>
        void StaticTryLock(int timeout, ref bool locked);
        /// <summary>
        /// Releases the static lock if it was previously acquired.
        /// </summary>
        /// <param name="locked">
        /// On input, indicates whether the lock is currently held and should
        /// be released.  Upon return, this will be set to false if the lock
        /// was released.
        /// </param>
        void StaticExitLock(ref bool locked);
    }
}
