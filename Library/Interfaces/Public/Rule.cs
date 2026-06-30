/*
 * Rule.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a single rule within a rule set.  It composes
    /// the read-only rule data (<see cref="IRuleData" />) with the ability to
    /// be cloned (<see cref="ICloneable" />), and adds operations to assign its
    /// identifier and to test its action flags.
    /// </summary>
    [ObjectId("83ce9774-7280-40bc-8cf2-134b19b657f7")]
    public interface IRule : IRuleData, ICloneable
    {
        /// <summary>
        /// This method sets the unique identifier for this rule.
        /// </summary>
        /// <param name="id">
        /// The identifier to assign to this rule.  This parameter may be null
        /// to indicate that this rule has no identifier.
        /// </param>
        void SetId(long? id);

        /// <summary>
        /// This method determines whether the action flags of this rule match
        /// the action flags specified by the given mode.
        /// </summary>
        /// <param name="mode">
        /// The match mode whose action flags are tested against the action
        /// flags of this rule.
        /// </param>
        /// <returns>
        /// True if the action flags match; otherwise, false.
        /// </returns>
        bool MatchAction(MatchMode mode);
    }
}
