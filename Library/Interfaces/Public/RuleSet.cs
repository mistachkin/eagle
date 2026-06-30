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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a set of rules.  It composes the read-only
    /// rule set data (<see cref="IRuleSetData" />) with the ability to be
    /// cloned (<see cref="ICloneable" />), and adds operations to query, build,
    /// add, remove, iterate, and apply the rules it contains.
    /// </summary>
    [ObjectId("ab967cb8-e9dd-4855-82c1-4ee5960f0616")]
    public interface IRuleSet : IRuleSetData, ICloneable
    {
        /// <summary>
        /// This method gets the name of this rule set.
        /// </summary>
        /// <returns>
        /// The name of this rule set, or null if it has no identifier.
        /// </returns>
        string GetName();

        /// <summary>
        /// This method determines whether this rule set contains no rules.
        /// </summary>
        /// <returns>
        /// True if this rule set is empty; otherwise, false.
        /// </returns>
        bool IsEmpty();

        /// <summary>
        /// This method marks this rule set as read-only, preventing further
        /// changes to the rules it contains.
        /// </summary>
        void MakeReadOnly();

        /// <summary>
        /// This method gets the number of rules contained in this rule set.
        /// </summary>
        /// <returns>
        /// The number of rules contained in this rule set.
        /// </returns>
        int CountRules();

        /// <summary>
        /// This method removes all rules from this rule set.
        /// </summary>
        void ClearRules();

        /// <summary>
        /// This method creates a copy of every rule contained in this rule set.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The collection of copied rules, or null upon failure.
        /// </returns>
        IEnumerable<IRule> CopyRules(
            ref Result error
        );

        /// <summary>
        /// This method finds the rules in this rule set that match the
        /// specified rule.
        /// </summary>
        /// <param name="rule">
        /// The rule whose values are used as the criteria for matching.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="allowNone">
        /// Non-zero to use enumeration values that are none as matching
        /// criteria; otherwise, such values are ignored for matching purposes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The collection of matching rules, or null upon failure.
        /// </returns>
        IEnumerable<IRule> FindRules(
            IRule rule,
            bool allowNone,
            ref Result error
        );

        /// <summary>
        /// This method builds a rule from the specified values and adds it to
        /// this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the rule to build, e.g. whether it includes or excludes
        /// matching items.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier the rule applies to.
        /// </param>
        /// <param name="mode">
        /// The match mode used by the rule when comparing its patterns.
        /// </param>
        /// <param name="pattern">
        /// The pattern associated with the rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The rule that was built and added, or null upon failure.
        /// </returns>
        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            string pattern,
            ref Result error
        );

        /// <summary>
        /// This method builds a rule from the specified values and adds it to
        /// this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the rule to build, e.g. whether it includes or excludes
        /// matching items.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier the rule applies to.
        /// </param>
        /// <param name="mode">
        /// The match mode used by the rule when comparing its patterns.
        /// </param>
        /// <param name="patterns">
        /// The patterns associated with the rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The rule that was built and added, or null upon failure.
        /// </returns>
        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            IEnumerable<string> patterns,
            ref Result error
        );

        /// <summary>
        /// This method builds a rule from the specified values and adds it to
        /// this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the rule to build, e.g. whether it includes or excludes
        /// matching items.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier the rule applies to.
        /// </param>
        /// <param name="mode">
        /// The match mode used by the rule when comparing its patterns.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used by the rule when its match mode
        /// involves regular expression matching.
        /// </param>
        /// <param name="patterns">
        /// The patterns associated with the rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The rule that was built and added, or null upon failure.
        /// </returns>
        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            RegexOptions regExOptions,
            IEnumerable<string> patterns,
            ref Result error
        );

        /// <summary>
        /// This method builds a rule from the specified values and adds it to
        /// this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the rule to build, e.g. whether it includes or excludes
        /// matching items.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier the rule applies to.
        /// </param>
        /// <param name="mode">
        /// The match mode used by the rule when comparing its patterns.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used by the rule when its match mode
        /// involves regular expression matching.
        /// </param>
        /// <param name="patterns">
        /// The patterns associated with the rule.
        /// </param>
        /// <param name="comparer">
        /// The comparer used by the rule when matching its patterns, or null to
        /// use the default comparison.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The rule that was built and added, or null upon failure.
        /// </returns>
        IRule BuildAndAddRule(
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            RegexOptions regExOptions,
            IEnumerable<string> patterns,
            IComparer<string> comparer,
            ref Result error
        );

        /// <summary>
        /// This method adds the specified rule to this rule set.
        /// </summary>
        /// <param name="rule">
        /// The rule to add.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the rule was added; otherwise, false.
        /// </returns>
        bool AddRule(
            IRule rule,
            ref Result error
        );

        /// <summary>
        /// This method removes the specified rule from this rule set.
        /// </summary>
        /// <param name="rule">
        /// The rule to remove.  This parameter should not be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the rule was removed; otherwise, false.
        /// </returns>
        bool RemoveRule(
            IRule rule,
            ref Result error
        );

        /// <summary>
        /// This method adds the rules contained in the specified rule set to
        /// this rule set.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set whose rules are added.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop adding rules upon encountering the first error;
        /// otherwise, all rules are attempted.
        /// </param>
        /// <param name="moveRules">
        /// Non-zero to move the rules from the source rule set, removing them
        /// from it; otherwise, the rules are copied.
        /// </param>
        /// <param name="count">
        /// On input, the running count of rules processed; on output, it is
        /// incremented by the number of rules processed by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if all rules were added; otherwise, false.
        /// </returns>
        bool AddRules(
            IRuleSet ruleSet,
            bool stopOnError,
            bool moveRules,
            ref int count,
            ref Result error
        );

        /// <summary>
        /// This method adds the specified rules to this rule set.
        /// </summary>
        /// <param name="rules">
        /// The rules to add.  This parameter should not be null.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop adding rules upon encountering the first error;
        /// otherwise, all rules are attempted.
        /// </param>
        /// <param name="count">
        /// On input, the running count of rules processed; on output, it is
        /// incremented by the number of rules processed by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if all rules were added; otherwise, false.
        /// </returns>
        bool AddRules(
            IEnumerable<IRule> rules,
            bool stopOnError,
            ref int count,
            ref Result error
        );

        /// <summary>
        /// This method invokes the specified callback for each rule in this
        /// rule set that matches the specified kind and mode.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke for each matching rule.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context for the iteration.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data to pass to the callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier used to select rules, or null to ignore the
        /// kind when selecting rules.
        /// </param>
        /// <param name="mode">
        /// The match mode used to select rules.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ForEachRule(
            RuleIterationCallback callback,
            Interpreter interpreter,
            IClientData clientData,
            IdentifierKind? kind,
            MatchMode mode,
            ref Result error
        );

        /// <summary>
        /// This method applies the rules in this rule set to the specified
        /// text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier used to select rules, or null to ignore the
        /// kind when selecting rules.
        /// </param>
        /// <param name="mode">
        /// The match mode used to select and apply rules.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <returns>
        /// True if the text was ultimately included by the matching rules;
        /// otherwise, false.
        /// </returns>
        bool ApplyRules(
            Interpreter interpreter,
            IdentifierKind? kind,
            MatchMode mode,
            string text
        );

        /// <summary>
        /// This method applies the rules in this rule set to the specified
        /// text, using the specified initial and default values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for the operation.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier used to select rules, or null to ignore the
        /// kind when selecting rules.
        /// </param>
        /// <param name="mode">
        /// The match mode used to select and apply rules.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <param name="initial">
        /// The initial match value used before any rules are applied, or null
        /// for no initial value.
        /// </param>
        /// <param name="default">
        /// The value returned when the rules do not produce a definite match
        /// result.
        /// </param>
        /// <returns>
        /// The resulting match value, or the default value when the rules do
        /// not produce a definite match result.
        /// </returns>
        bool ApplyRules(
            Interpreter interpreter,
            IdentifierKind? kind,
            MatchMode mode,
            string text,
            bool? initial,
            bool @default
        );

        /// <summary>
        /// This method applies the rules in this rule set to the specified
        /// text, invoking the specified callback for each rule.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke for each rule, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context for the operation.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data to pass to the callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier used to select rules, or null to ignore the
        /// kind when selecting rules.
        /// </param>
        /// <param name="mode">
        /// The match mode used to select and apply rules.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <param name="default">
        /// The value used when the rules do not produce a definite match
        /// result.
        /// </param>
        /// <param name="match">
        /// On input, the initial match value; on output, the resulting match
        /// value, or null when the rules do not produce a definite match
        /// result.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ApplyRules(
            RuleMatchCallback callback,
            Interpreter interpreter,
            IClientData clientData,
            IdentifierKind? kind,
            MatchMode mode,
            string text,
            bool @default,
            ref bool? match,
            ref Result error
        );
    }
}
