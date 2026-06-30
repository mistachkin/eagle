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
    /// <summary>
    /// This interface defines the lowest-level synchronization contract,
    /// exposing the object used as the lock root for an entity.  Higher-level
    /// synchronization interfaces build upon it.
    /// </summary>
    [ObjectId("e18a2abd-88e6-4c1a-b25b-e1dfb6fc1a53")]
    public interface ISynchronizeBase
    {
        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONIZATION ROOT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the object that should be used to synchronize access to this
        /// entity, e.g. via the <c>lock</c> statement.  This object should not
        /// be null.  WARNING: For primary application domain use only.
        /// </summary>
        object SyncRoot { get; } /* WARNING: For primary AppDomain use only. */
    }
}
