/*
 * RuleSetData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface exposes the read-only data that describes a rule set,
    /// including its identifier and the comparer used when matching the rules
    /// it contains.
    /// </summary>
    [ObjectId("4a22a525-8403-48e1-b0b1-6ac3b022c8e4")]
    public interface IRuleSetData
    {
        /// <summary>
        /// Gets the unique identifier for this rule set, or null if it has
        /// none.
        /// </summary>
        long? Id { get; }

        /// <summary>
        /// Gets the comparer used by this rule set when matching rules, or null
        /// to use the default comparison.
        /// </summary>
        IComparer<string> Comparer { get; }
    }
}
