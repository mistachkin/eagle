/*
 * HaveExecutionPolicy.cs --
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
    /// This interface is implemented by entities that expose read-write access
    /// to an associated execution policy, including both the policy type and
    /// the execution policy itself.
    /// </summary>
    [ObjectId("fec373c0-eb9e-48ce-bc39-cf52eff96c29")]
    public interface IHaveExecutionPolicy
    {
        /// <summary>
        /// Gets or sets the kind of policy associated with this entity.  This
        /// value may be null if no policy type has been specified.
        /// </summary>
        PolicyType? PolicyType { get; set; }
        /// <summary>
        /// Gets or sets the execution policy associated with this entity.  This
        /// value may be null if no execution policy has been specified.
        /// </summary>
        ExecutionPolicy? ExecutionPolicy { get; set; }
    }
}
