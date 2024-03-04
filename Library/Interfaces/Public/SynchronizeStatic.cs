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
    [ObjectId("df9169f8-ea23-49f3-9f3b-821e85c330ea")]
    public interface ISynchronizeStatic
    {
        ///////////////////////////////////////////////////////////////////////
        // NON-BLOCKING LOCK / UNLOCK
        ///////////////////////////////////////////////////////////////////////

        void StaticTryLock(ref bool locked);
        void StaticTryLockWithWait(ref bool locked);
        void StaticTryLock(int timeout, ref bool locked);
        void StaticExitLock(ref bool locked);
    }
}
