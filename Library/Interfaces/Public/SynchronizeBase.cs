/*
 * SynchronizeBase.cs --
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
    [ObjectId("e18a2abd-88e6-4c1a-b25b-e1dfb6fc1a53")]
    public interface ISynchronizeBase
    {
        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONIZATION ROOT
        ///////////////////////////////////////////////////////////////////////

        object SyncRoot { get; } /* WARNING: For primary AppDomain use only. */
    }
}
