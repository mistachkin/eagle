/*
 * RuleData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface exposes the read-only data that describes a single rule,
    /// including its identifier, type, the kind of identifier it applies to,
    /// its match mode, its regular expression options, its patterns, and the
    /// comparer used for matching.
    /// </summary>
    [ObjectId("c274c1b6-e844-4577-adb9-e213a80d82f6")]
    public interface IRuleData
    {
        /// <summary>
        /// Gets the unique identifier for this rule, or null if it has none.
        /// </summary>
        long? Id { get; }

        /// <summary>
        /// Gets the type of this rule, e.g. whether it includes or excludes
        /// matching items.
        /// </summary>
        RuleType Type { get; }

        /// <summary>
        /// Gets the kind of identifier this rule applies to.
        /// </summary>
        IdentifierKind Kind { get; }

        /// <summary>
        /// Gets the match mode used by this rule when comparing its patterns
        /// against candidate text.
        /// </summary>
        MatchMode Mode { get; }

        /// <summary>
        /// Gets the regular expression options used by this rule when its match
        /// mode involves regular expression matching.
        /// </summary>
        RegexOptions RegExOptions { get; }

        /// <summary>
        /// Gets the patterns associated with this rule.
        /// </summary>
        IEnumerable<string> Patterns { get; }

        /// <summary>
        /// Gets the comparer used by this rule when matching its patterns, or
        /// null to use the default comparison.
        /// </summary>
        IComparer<string> Comparer { get; }
    }
}
