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
    [ObjectId("86534481-590f-49d7-9115-3b3f48580157")]
    public interface ISynchronize
    {
        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONIZATION ROOT
        ///////////////////////////////////////////////////////////////////////

        object SyncRoot { get; } /* WARNING: For primary AppDomain use only. */

        ///////////////////////////////////////////////////////////////////////
        // NON-BLOCKING LOCK / UNLOCK
        ///////////////////////////////////////////////////////////////////////

        void TryLock(ref bool locked);
        void TryLockWithWait(ref bool locked);
        void TryLock(int timeout, ref bool locked);
        void ExitLock(ref bool locked);
    }
}
