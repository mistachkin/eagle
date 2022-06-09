/*
 * RuleSet.cs --
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
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("ab967cb8-e9dd-4855-82c1-4ee5960f0616")]
    public interface IRuleSet
    {
        IComparer<string> Comparer { get; set; }

        bool IsEmpty();

        void MakeReadOnly();

        void ClearRules();

        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            string pattern,
            ref Result error
        );

        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            IEnumerable<string> patterns,
            ref Result error
        );

        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            RegexOptions regExOptions,
            IEnumerable<string> patterns,
            ref Result error
        );

        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            RegexOptions regExOptions,
            IEnumerable<string> patterns,
            IComparer<string> comparer,
            ref Result error
        );

        bool AddRule(
            IRule rule,
            ref Result error
        );

        bool RemoveRule(
            IRule rule,
            ref Result error
        );

        ReturnCode ForEachRule(
            RuleIterationCallback callback,
            Interpreter interpreter,
            IdentifierKind? kind,
            MatchMode mode,
            ref Result error
        );

        bool ApplyRules(
            Interpreter interpreter,
            IdentifierKind? kind,
            string text
        );

        bool ApplyRules(
            Interpreter interpreter,
            IdentifierKind? kind,
            MatchMode mode,
            string text,
            bool? initial,
            bool @default
        );

        ReturnCode ApplyRules(
            RuleMatchCallback callback,
            Interpreter interpreter,
            IdentifierKind? kind,
            MatchMode mode,
            string text,
            bool @default,
            ref bool? match,
            ref Result error
        );
    }
}
