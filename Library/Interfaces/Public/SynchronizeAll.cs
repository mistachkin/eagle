/*
 * SynchronizeAll.cs --
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
    /// This interface aggregates the per-instance synchronization contract
    /// (<see cref="ISynchronize" />) together with the static (type-level)
    /// synchronization contract (<see cref="ISynchronizeStatic" />), exposing
    /// both kinds of locking through a single interface for entities that
    /// require them.
    /// </summary>
    [ObjectId("86186c86-93a7-47b7-8797-01b15e703ac0")]
    public interface ISynchronizeAll : ISynchronize, ISynchronizeStatic
    {
        // nothing.
    }
}
