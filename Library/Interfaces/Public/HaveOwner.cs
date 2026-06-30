/*
 * HaveOwner.cs --
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
    /// This interface is implemented by entities that have an associated
    /// owner object, allowing that owner to be queried and tested.
    /// </summary>
    [ObjectId("3b6ad1c6-4890-4691-b347-8d1c8a396c46")]
    public interface IHaveOwner
    {
        /// <summary>
        /// Gets or sets the owner object associated with this entity.  This
        /// value may be null.
        /// </summary>
        object Owner { get; set; }

        /// <summary>
        /// Determines whether this entity currently has an associated owner.
        /// </summary>
        /// <returns>
        /// True if this entity has an owner; otherwise, false.
        /// </returns>
        bool HasOwner();

        /// <summary>
        /// Determines whether the specified owner is currently busy.
        /// </summary>
        /// <param name="owner">
        /// The owner object to test.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the specified owner is busy; otherwise, false.
        /// </returns>
        bool IsOwnerBusy(object owner);
    }
}
