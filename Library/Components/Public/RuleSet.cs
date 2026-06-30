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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using RulePair = System.Collections.Generic.KeyValuePair<
    long, Eagle._Interfaces.Public.IRule>;

using RuleDictionary = System.Collections.Generic.Dictionary<
    long, Eagle._Interfaces.Public.IRule>;

#if TEST
using TestClass = Eagle._Tests.Default;
using RuleSetClientData = Eagle._Tests.Default.RuleSetClientData;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a mutable, thread-safe, ordered collection of
    /// rules used to include or exclude identifiers (for example, commands,
    /// procedures, or variables) based on pattern matching.  Each contained
    /// rule is identified by a unique numeric identifier and is applied in
    /// ascending identifier order.  Rules may be added, removed, queried, and
    /// iterated; the set may be marked read-only to prevent further
    /// modification, and it may be cloned and disposed.  See
    /// <see cref="IRule" /> for the representation of an individual rule.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2d251f5e-a5b7-40f2-a991-d06f9bcb78cb")]
    public sealed class RuleSet : IRuleSet, IHaveClientData, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// rule set.
        /// </summary>
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The most recently assigned automatic rule identifier.  It is
        /// incremented to produce the next unique identifier for a rule that
        /// lacks one.
        /// </summary>
        private long nextRuleId;

        /// <summary>
        /// When non-zero, this rule set is read-only and any attempt to modify
        /// it will raise an exception.
        /// </summary>
        private bool readOnly;

        /// <summary>
        /// The backing dictionary that maps each rule identifier to its
        /// associated rule.
        /// </summary>
        private RuleDictionary rules;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new, empty rule set.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created rule set, or null if it could not be created.
        /// </returns>
        public static IRuleSet Create(
            ref Result error /* out */
            )
        {
            return Create(
                null, null, Rule.DefaultAllowMissing,
                Rule.DefaultAllowExtra, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new rule set, optionally populating it with
        /// rules parsed from the specified list of rule strings.  Any error
        /// encountered while parsing is discarded.
        /// </summary>
        /// <param name="text">
        /// The list of rule strings used to populate the rule set, or null to
        /// create an empty rule set.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the rule strings, or null for the
        /// default culture.
        /// </param>
        /// <returns>
        /// The newly created rule set, or null if it could not be created.
        /// </returns>
        internal static IRuleSet Create(
            string text,            /* in */
            CultureInfo cultureInfo /* in */
            )
        {
            Result error = null; /* NOT USED */

            return Create(
                text, cultureInfo, Rule.DefaultAllowMissing,
                Rule.DefaultAllowExtra, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new rule set, optionally populating it with
        /// rules parsed from the specified list of rule strings.
        /// </summary>
        /// <param name="text">
        /// The list of rule strings used to populate the rule set, or null to
        /// create an empty rule set.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the rule strings, or null for the
        /// default culture.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created rule set, or null if it could not be created.
        /// </returns>
        public static IRuleSet Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            return Create(
                text, cultureInfo, Rule.DefaultAllowMissing,
                Rule.DefaultAllowExtra, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new rule set, optionally populating it with
        /// rules parsed from the specified list of rule strings.
        /// </summary>
        /// <param name="text">
        /// The list of rule strings used to populate the rule set, or null to
        /// create an empty rule set.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the rule strings, or null for the
        /// default culture.
        /// </param>
        /// <param name="allowMissing">
        /// Non-zero to permit rule strings that omit optional fields when
        /// parsing each rule.
        /// </param>
        /// <param name="allowExtra">
        /// Non-zero to permit rule strings that contain extra, unrecognized
        /// fields when parsing each rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created rule set, or null if it could not be created.
        /// </returns>
        public static IRuleSet Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            bool allowMissing,       /* in */
            bool allowExtra,         /* in */
            ref Result error         /* out */
            )
        {
            bool success = false;
            RuleSet ruleSet = null;

            try
            {
                ruleSet = new RuleSet();

                if (text != null)
                {
                    StringList list = null;

                    if (ParserOps<string>.SplitList(
                            null, text, 0, Length.Invalid, false,
                            ref list, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    foreach (string element in list)
                    {
                        if (element == null)
                            continue;

                        IRule rule = Rule.Create(
                            element, cultureInfo, allowMissing,
                            allowExtra, ref error);

                        if (rule == null)
                            return null;

                        rule = ruleSet.Add(rule, ref error);

                        if (rule == null)
                            return null;
                    }
                }

                success = true;
                return ruleSet;
            }
            finally
            {
                if (!success && (ruleSet != null))
                {
                    /* IGNORED */
                    ObjectOps.TryDisposeOrTrace<RuleSet>(
                        ref ruleSet);

                    ruleSet = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a deep copy of the specified rule set.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set to clone.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created copy of the rule set, or null if the specified
        /// rule set was null or could not be cloned.
        /// </returns>
        public static IRuleSet Clone(
            IRuleSet ruleSet /* in */
            )
        {
            return (ruleSet != null) ?
                ruleSet.Clone() as IRuleSet : null;
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method creates a new rule set by loading or defining its rules
        /// from a file and/or a block of text, according to the specified rule
        /// set type.  It is only available in test builds.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file containing the rule set, or null to use the
        /// specified text instead.
        /// </param>
        /// <param name="text">
        /// The text containing the rule set, or null to use the specified
        /// file instead.
        /// </param>
        /// <param name="ruleSetType">
        /// The type of rule set to create, which determines how the file or
        /// text is interpreted.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created rule set, or null if it could not be created.
        /// </returns>
        public static IRuleSet CreateFromFile(
            string fileName,         /* in: OPTIONAL */
            string text,             /* in: OPTIONAL */
            RuleSetType ruleSetType, /* in */
            ref Result error         /* out */
            )
        {
            IRuleSet ruleSet = null;

            RuleSetType baseRuleSetType =
                ruleSetType & RuleSetType.BaseMask;

            if (baseRuleSetType == RuleSetType.CommandFile)
            {
                if (text != null)
                {
                    error = String.Format(
                        "cannot use text for ruleset type {0}",
                        FormatOps.WrapOrNull(ruleSetType));

                    return null;
                }

                if (TestClass.TestLoadRuleSet(fileName,
                        ref ruleSet, ref error) == ReturnCode.Ok)
                {
                    return ruleSet;
                }
                else
                {
                    return null;
                }
            }
            else if (baseRuleSetType == RuleSetType.DefinitionFile)
            {
                RuleSetClientData clientData = new RuleSetClientData();

                if (TestClass.TestDefineRuleSet(
                        fileName, text, clientData, ref ruleSet,
                        ref error) == ReturnCode.Ok)
                {
                    return ruleSet;
                }
                else
                {
                    return null;
                }
            }

            error = String.Format(
                "unsupported ruleset type {0}",
                FormatOps.WrapOrNull(ruleSetType));

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a new, empty rule set.
        /// </summary>
        private RuleSet()
            : this(null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new rule set, either as a deep copy of the specified
        /// rule set or as a freshly initialized, empty rule set.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set to copy, or null to construct an empty rule set.
        /// </param>
        private RuleSet(
            RuleSet ruleSet /* in */
            )
            : base()
        {
            if (ruleSet != null)
                Copy(ruleSet, true);
            else
                Initialize(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method extracts the individual meta-mode flags from the
        /// specified combined match mode value.
        /// </summary>
        /// <param name="mode">
        /// The combined match mode value to examine.
        /// </param>
        /// <param name="stopOnError">
        /// Upon return, this parameter will be non-zero if the match mode
        /// requests that processing stop when an error is encountered.
        /// </param>
        /// <param name="all">
        /// Upon return, this parameter will be non-zero if the match mode
        /// requests that all patterns be considered instead of stopping at the
        /// first match.
        /// </param>
        /// <param name="noCase">
        /// Upon return, this parameter will be non-zero if the match mode
        /// requests case-insensitive matching.
        /// </param>
        private static void ExtractMetaModes(
            MatchMode mode,       /* in */
            out bool stopOnError, /* out */
            out bool all,         /* out */
            out bool noCase       /* out */
            )
        {
            stopOnError = FlagOps.HasFlags(
                mode, MatchMode.StopOnError, true);

            all = FlagOps.HasFlags(
                mode, MatchMode.All, true);

            noCase = FlagOps.HasFlags(
                mode, MatchMode.NoCase, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats a set of rule processing parameters and
        /// statistics into a string suitable for use in diagnostic trace
        /// output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the rule processing, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the rule processing, if any.
        /// </param>
        /// <param name="kind">
        /// The kind of identifier being matched, if any.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect.
        /// </param>
        /// <param name="text">
        /// The text being matched, if any.
        /// </param>
        /// <param name="match">
        /// The overall match result, if any.
        /// </param>
        /// <param name="nopCount">
        /// The number of rules that matched but performed no action.
        /// </param>
        /// <param name="matchCount">
        /// The number of rules that were matched.
        /// </param>
        /// <param name="errorCount">
        /// The number of rules that produced an error.
        /// </param>
        /// <param name="includeCount">
        /// The number of include rules that matched.
        /// </param>
        /// <param name="excludeCount">
        /// The number of exclude rules that matched.
        /// </param>
        /// <param name="stopRule">
        /// The rule that caused processing to stop, if any.
        /// </param>
        /// <param name="errors">
        /// The list of errors accumulated during processing, if any.
        /// </param>
        /// <returns>
        /// The formatted trace string.
        /// </returns>
        private static string FormatTrace(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            IdentifierKind? kind,    /* in */
            MatchMode mode,          /* in */
            string text,             /* in */
            bool? match,             /* in */
            int nopCount,            /* in */
            int matchCount,          /* in */
            int errorCount,          /* in */
            int includeCount,        /* in */
            int excludeCount,        /* in */
            IRule stopRule,          /* in */
            ResultList errors        /* in */
            )
        {
            return String.Format(
                "interpreter = {0}, clientData = {1}, " +
                "kind = {2}, mode = {3}, text = {4}, " +
                "match = {5}, nopCount = {6}, " +
                "matchCount = {7}, errorCount = {8}, " +
                "includeCount = {9}, excludeCount = {10}, " +
                "stopRule = {11}, errors = {12}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(kind), mode,
                FormatOps.WrapOrNull(text),
                FormatOps.WrapOrNull(match), nopCount,
                matchCount, errorCount, includeCount,
                excludeCount, FormatOps.WrapOrNull(stopRule),
                FormatOps.WrapOrNull(errors));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the identifier of each rule in the specified
        /// collection.
        /// </summary>
        /// <param name="rules">
        /// The collection of rules whose identifiers will be cleared.  This
        /// parameter may be null.
        /// </param>
        private static void ResetIds(
            IEnumerable<IRule> rules /* in */
            )
        {
            if (rules != null)
            {
                foreach (IRule rule in rules)
                {
                    if (rule == null)
                        continue;

                    rule.SetId(null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the rules from the specified rule set, in
        /// ascending identifier order, optionally moving them out of the
        /// source rule set rather than copying them.  The identifier of each
        /// returned rule is cleared.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set from which to obtain the rules.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop processing and return null when an invalid or
        /// missing rule is encountered; otherwise, such rules are skipped.
        /// </param>
        /// <param name="moveRules">
        /// Non-zero to remove the rules from the source rule set; zero to
        /// produce a deep copy of them.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this parameter will be modified to contain a list of
        /// one or more error messages.
        /// </param>
        /// <returns>
        /// The collection of rules, or null if the rules could not be
        /// obtained.
        /// </returns>
        private static IEnumerable<IRule> GetRules(
            IRuleSet ruleSet,     /* in */
            bool stopOnError,     /* in */
            bool moveRules,       /* in */
            ref ResultList errors /* in, out */
            )
        {
            if (ruleSet == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid ruleset");
                return null;
            }

            RuleSet localRuleSet = ruleSet as RuleSet;

            if (localRuleSet == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("wrong ruleset sub-type");
                return null;
            }

            RuleDictionary rules = moveRules ?
                localRuleSet.TakeRules() :
                localRuleSet.CloneRules(true);

            if (rules == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("ruleset missing rules");
                return null;
            }

            List<IRule> result = new List<IRule>();
            LongList ids = new LongList(rules.Keys);

            ids.Sort(); /* O(N log N) */

            foreach (long id in ids)
            {
                IRule rule;

                if (!rules.TryGetValue(id, out rule))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "rule #{0} missing", id));

                    if (stopOnError)
                        return null;
                    else
                        continue;
                }

                if (rule == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "rule #{0} invalid", id));

                    if (stopOnError)
                        return null;
                    else
                        continue;
                }

                result.Add(rule);
            }

            ResetIds(result);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method throws an exception if this rule set is read-only.  It
        /// is called at the start of members that modify the rule set.
        /// </summary>
        /// <exception cref="ScriptException">
        /// Thrown when this rule set is read-only.
        /// </exception>
        private void CheckReadOnly()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (readOnly)
                    throw new ScriptException("rule set is read-only");
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the read-only state of this rule set.
        /// </summary>
        /// <param name="readOnly">
        /// Non-zero to make this rule set read-only; zero to make it
        /// modifiable.
        /// </param>
        /// <returns>
        /// The previous read-only state of this rule set.
        /// </returns>
        private bool SetReadOnly(
            bool readOnly /* in */
            )
        {
            bool oldReadOnly;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                oldReadOnly = this.readOnly;
                this.readOnly = readOnly;
            }

            return oldReadOnly;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes this rule set, assigning it a unique
        /// identifier and an empty rule dictionary if it does not already have
        /// them.
        /// </summary>
        /// <param name="force">
        /// Non-zero to reinitialize the identifier and rule dictionary even if
        /// they have already been set.
        /// </param>
        private void Initialize(
            bool force /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (force || (id == null))
                    id = GlobalState.NextRuleSetId();

                if (force || (rules == null))
                {
                    if (rules != null)
                        rules.Clear();

                    rules = new RuleDictionary();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this rule set to its default, empty state,
        /// clearing all rules and associated state.
        /// </summary>
        private void Reset()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Interlocked.Exchange(ref nextRuleId, 0);

                if (rules != null)
                {
                    rules.Clear();
                    rules = null;
                }

                clientData = null;

                id = null;
                comparer = null;
                readOnly = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the state of the specified rule set into this
        /// rule set, replacing any existing state.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set whose state will be copied.  This parameter may be
        /// null, in which case this method does nothing.
        /// </param>
        /// <param name="deepCopy">
        /// Non-zero to deeply clone each rule; zero to share the existing rule
        /// instances.
        /// </param>
        private void Copy(
            RuleSet ruleSet, /* in */
            bool deepCopy    /* in */
            )
        {
            if (ruleSet == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                nextRuleId = ruleSet.nextRuleId;
                rules = ruleSet.CloneRules(deepCopy);
                clientData = ruleSet.clientData;
                id = ruleSet.id;
                comparer = ruleSet.comparer;
                readOnly = ruleSet.readOnly;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list containing the string form of each
        /// rule in this rule set, in ascending identifier order.
        /// </summary>
        /// <returns>
        /// The list of rule strings.
        /// </returns>
        private StringList ToList()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringList list = new StringList();

                if (rules != null)
                {
                    LongList ids = new LongList(rules.Keys);

                    ids.Sort(); /* O(N log N) */

                    foreach (long id in ids)
                    {
                        IRule rule;

                        if (!rules.TryGetValue(id, out rule))
                            continue;

                        if (rule == null)
                            continue;

                        list.Add(rule.ToString());
                    }
                }

                return list;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the largest identifier in use by any rule
        /// in this rule set.
        /// </summary>
        /// <returns>
        /// The largest rule identifier in use, or null if there are no rules
        /// with an identifier.
        /// </returns>
        private long? MaximumRuleId()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                long? maximumId = null;

                if (rules != null)
                {
                    LongList ids = new LongList(rules.Keys);

                    ids.Sort(); /* O(N log N) */

                    foreach (long id in ids)
                    {
                        IRule rule;

                        if (!rules.TryGetValue(id, out rule))
                            continue;

                        if (rule == null)
                            continue;

                        long? ruleId = rule.Id;

                        if (maximumId == null)
                        {
                            if (ruleId != null)
                                maximumId = ruleId;

                            continue;
                        }

                        if ((ruleId != null) &&
                            ((long)ruleId > (long)maximumId))
                        {
                            maximumId = ruleId;
                        }
                    }
                }

                return maximumId;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces the next unique rule identifier, ensuring that
        /// it is greater than any identifier currently in use.
        /// </summary>
        /// <returns>
        /// The next unique rule identifier.
        /// </returns>
        private long NextRuleId()
        {
            long nextId = Interlocked.Increment(ref nextRuleId);
            long? maximumId = MaximumRuleId();

            if ((maximumId == null) || (nextId > (long)maximumId))
                return nextId;

            /* IGNORED */
            Interlocked.CompareExchange(
                ref nextRuleId, (long)maximumId, nextId);

            return Interlocked.Increment(ref nextRuleId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the specified rule identifier, or a newly
        /// generated unique identifier if none was specified.
        /// </summary>
        /// <param name="id">
        /// The desired rule identifier, or null to generate a new unique
        /// identifier.
        /// </param>
        /// <returns>
        /// The resolved rule identifier.
        /// </returns>
        private long GetRuleId(
            long? id /* in: OPTIONAL */
            )
        {
            if (id != null)
                return (long)id;

            return NextRuleId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the identifier of the specified rule, assigning
        /// it a newly generated unique identifier if it does not already have
        /// one.  If no rule is specified, a new unique identifier is
        /// generated.
        /// </summary>
        /// <param name="rule">
        /// The rule whose identifier is desired, or null to generate a new
        /// unique identifier.
        /// </param>
        /// <returns>
        /// The resolved rule identifier.
        /// </returns>
        private long GetRuleId(
            IRule rule /* in: OPTIONAL */
            )
        {
            if (rule != null)
            {
                long? id = rule.Id;

                if (id != null)
                    return (long)id;

                id = NextRuleId();
                rule.SetId(id);

                return (long)id;
            }

            return NextRuleId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the string comparer to use for the specified
        /// rule, preferring the comparer associated with the rule and falling
        /// back to the comparer associated with this rule set.
        /// </summary>
        /// <param name="rule">
        /// The rule whose comparer is preferred, or null to use the comparer
        /// associated with this rule set.
        /// </param>
        /// <returns>
        /// The string comparer to use, which may be null.
        /// </returns>
        private IComparer<string> GetComparer(
            IRule rule /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                IComparer<string> comparer = null;

                if (rule != null)
                    comparer = rule.Comparer;

                if (comparer == null)
                    comparer = this.Comparer;

                return comparer;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of rules in this rule set.
        /// </summary>
        /// <returns>
        /// The number of rules in this rule set, or an invalid count if the
        /// rule dictionary is unavailable.
        /// </returns>
        private int GetCount()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rules == null)
                    return Count.Invalid;

                return rules.Count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the entire rule dictionary from
        /// this rule set, leaving this rule set without any rules.
        /// </summary>
        /// <returns>
        /// The rule dictionary that was removed from this rule set, which may
        /// be null.
        /// </returns>
        private RuleDictionary TakeRules()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                RuleDictionary rules = this.rules;

                this.rules = null;

                return rules;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a copy of the rule dictionary belonging to
        /// this rule set.
        /// </summary>
        /// <param name="deepCopy">
        /// Non-zero to deeply clone each rule; zero to produce a shallow copy
        /// that shares the existing rule instances.
        /// </param>
        /// <returns>
        /// The copied rule dictionary, which may be null when a shallow copy
        /// is requested and this rule set has no rule dictionary.
        /// </returns>
        private RuleDictionary CloneRules(
            bool deepCopy /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                RuleDictionary result;

                if (deepCopy)
                {
                    result = new RuleDictionary();

                    foreach (RulePair pair in rules)
                    {
                        IRule rule = pair.Value;

                        if (rule == null)
                            continue;

                        rule = rule.Clone() as IRule;

                        if (rule == null)
                            continue;

                        result.Add(pair.Key, rule);
                    }
                }
                else
                {
                    result = (rules != null) ?
                        new RuleDictionary(rules) : null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds all rules in this rule set whose properties
        /// exactly match the specified criteria, in ascending identifier
        /// order.  A null criterion is treated as a wildcard that matches any
        /// value.
        /// </summary>
        /// <param name="type">
        /// The rule type to match, or null to match any type.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode to match, or null to match any mode.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to match, or null to match any
        /// options.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns to match, or null to match any
        /// patterns.
        /// </param>
        /// <param name="comparer">
        /// The string comparer to match by type, or null to match any
        /// comparer.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The collection of matching rules, or null if no rules matched or
        /// the rules are unavailable.
        /// </returns>
        private IEnumerable<IRule> FindExact(
            RuleType? type,               /* in: OPTIONAL */
            IdentifierKind? kind,         /* in: OPTIONAL */
            MatchMode? mode,              /* in: OPTIONAL */
            RegexOptions? regExOptions,   /* in: OPTIONAL */
            IEnumerable<string> patterns, /* in: OPTIONAL */
            IComparer<string> comparer,   /* in: OPTIONAL */
            ref Result error              /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rules == null)
                {
                    error = "rules unavailable";
                    return null;
                }

                IList<IRule> matches = null;
                LongList ids = new LongList(rules.Keys);

                ids.Sort(); /* O(N log N) */

                foreach (long id in ids)
                {
                    IRule rule;

                    if (!rules.TryGetValue(id, out rule))
                        continue;

                    if (rule == null)
                        continue;

                    if ((type != null) &&
                        (rule.Type != (RuleType)type))
                    {
                        continue;
                    }

                    if ((kind != null) &&
                        (rule.Kind != (IdentifierKind)kind))
                    {
                        continue;
                    }

                    if ((mode != null) &&
                        (rule.Mode != (MatchMode)mode))
                    {
                        continue;
                    }

                    if ((regExOptions != null) &&
                        (rule.RegExOptions != (RegexOptions)regExOptions))
                    {
                        continue;
                    }

                    if ((patterns != null) &&
                        !ListOps.IEnumerableEquals<string>(
                            rule.Patterns, patterns, null))
                    {
                        continue;
                    }

                    if ((comparer != null) &&
                        !MarshalOps.IsSameObjectType(
                            rule.Comparer, comparer))
                    {
                        continue;
                    }

                    if (matches == null)
                        matches = new List<IRule>();

                    matches.Add(rule);
                }

                if (matches != null)
                {
                    return matches;
                }
                else
                {
                    error = "no matching rules found";
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a new rule from the specified components and
        /// adds it to this rule set.
        /// </summary>
        /// <param name="id">
        /// The identifier to assign to the new rule, or null to generate a new
        /// unique identifier.
        /// </param>
        /// <param name="type">
        /// The type of the new rule.
        /// </param>
        /// <param name="kind">
        /// The identifier kind of the new rule.
        /// </param>
        /// <param name="mode">
        /// The match mode of the new rule.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options of the new rule.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns of the new rule.
        /// </param>
        /// <param name="comparer">
        /// The string comparer of the new rule, which may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly added rule, or null if it could not be added.
        /// </returns>
        private IRule Add(
            long? id,                     /* in */
            RuleType type,                /* in */
            IdentifierKind kind,          /* in */
            MatchMode mode,               /* in */
            RegexOptions regExOptions,    /* in */
            IEnumerable<string> patterns, /* in */
            IComparer<string> comparer,   /* in */
            ref Result error              /* out */
            )
        {
            return Add(new Rule(
                GetRuleId(id), type, kind, mode, regExOptions, patterns,
                comparer, false), ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified rule to this rule set, overwriting
        /// any existing rule with the same identifier.
        /// </summary>
        /// <param name="rule">
        /// The rule to add.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The added rule, or null if it could not be added.
        /// </returns>
        private IRule Add(
            IRule rule,      /* in */
            ref Result error /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rule == null)
                {
                    error = "invalid rule";
                    return null;
                }

                if (rules == null)
                {
                    error = "rules unavailable";
                    return null;
                }

                //
                // HACK: Always overwrite instead of
                //       purely adding, just in case.
                //
                rules[GetRuleId(rule)] = rule;
                return rule;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified rule from this rule set.
        /// </summary>
        /// <param name="rule">
        /// The rule to remove.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the rule was removed; otherwise, false.
        /// </returns>
        private bool Remove(
            IRule rule,      /* in */
            ref Result error /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rule == null)
                {
                    error = "invalid rule";
                    return false;
                }

                if (rules == null)
                {
                    error = "rules unavailable";
                    return false;
                }

                if (!rules.Remove(GetRuleId(rule)))
                {
                    error = "could not remove rule";
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method iterates over the rules in this rule set, in ascending
        /// identifier order, invoking the specified callback for each rule
        /// whose identifier kind matches.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke for each matching rule, or null to merely
        /// count the matching rules.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter to pass to the callback, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback, if any.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect, which controls whether iteration stops
        /// when an error is encountered.
        /// </param>
        /// <param name="matchCount">
        /// Upon return, this parameter will be incremented by the number of
        /// matching rules.
        /// </param>
        /// <param name="errorCount">
        /// Upon return, this parameter will be incremented by the number of
        /// rules for which the callback reported an error.
        /// </param>
        /// <param name="stopRule">
        /// Upon failure, this parameter will be modified to contain the rule
        /// that caused iteration to stop.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this parameter will be modified to contain a list of
        /// one or more error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private ReturnCode Iterate(
            RuleIterationCallback callback, /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in: OPTIONAL */
            IdentifierKind? kind,           /* in */
            MatchMode mode,                 /* in */
            ref int matchCount,             /* in, out */
            ref int errorCount,             /* in, out */
            ref IRule stopRule,             /* out */
            ref ResultList errors           /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rules == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("rules unavailable");
                    return ReturnCode.Error;
                }

                bool stopOnError = FlagOps.HasFlags(
                    mode, MatchMode.StopOnError, true);

                LongList ids = new LongList(rules.Keys);

                ids.Sort(); /* O(N log N) */

                foreach (long id in ids)
                {
                    IRule rule;

                    if (!rules.TryGetValue(id, out rule))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "rule #{0} missing", id));

                        if (stopOnError)
                        {
                            stopRule = Rule.Empty;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (rule == null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "rule #{0} invalid", id));

                        if (stopOnError)
                        {
                            stopRule = Rule.Empty;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    long? ruleId = rule.Id;

                    if ((ruleId == null) ||
                        ((long)ruleId != id))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "rule #{0} mismatches #{1}",
                            FormatOps.WrapOrNull(rule.Id),
                            id));

                        if (stopOnError)
                        {
                            stopRule = Rule.Empty;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if ((kind != null) &&
                        (rule.Kind != (IdentifierKind)kind))
                    {
                        continue;
                    }

                    matchCount++;

                    if (callback == null)
                        continue;

                    if (callback(interpreter,
                            clientData, rule, ref stopOnError,
                            ref errors) != ReturnCode.Ok)
                    {
                        errorCount++;

                        if (stopOnError)
                        {
                            stopRule = rule;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method matches the specified text against the rules in this
        /// rule set, in ascending identifier order, accumulating the overall
        /// include/exclude result.  When the match mode does not request that
        /// all rules be considered, matching stops at the first include or
        /// exclude rule that matches.
        /// </summary>
        /// <param name="callback">
        /// The callback used to perform matching for each rule, or null to use
        /// the default pattern matching.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter to use for matching, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback, if any.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <param name="match">
        /// Upon return, this parameter will be modified to contain the overall
        /// match result, which may be null if no rule matched.
        /// </param>
        /// <param name="nopCount">
        /// Upon return, this parameter will be incremented by the number of
        /// matched rules that performed no action.
        /// </param>
        /// <param name="errorCount">
        /// Upon return, this parameter will be incremented by the number of
        /// rules that produced an error.
        /// </param>
        /// <param name="includeCount">
        /// Upon return, this parameter will be incremented by the number of
        /// include rules that matched.
        /// </param>
        /// <param name="excludeCount">
        /// Upon return, this parameter will be incremented by the number of
        /// exclude rules that matched.
        /// </param>
        /// <param name="stopRule">
        /// Upon return, this parameter will be modified to contain the rule
        /// that caused matching to stop, if any.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this parameter will be modified to contain a list of
        /// one or more error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private ReturnCode Match(
            RuleMatchCallback callback, /* in */
            Interpreter interpreter,    /* in */
            IClientData clientData,     /* in: OPTIONAL */
            IdentifierKind? kind,       /* in */
            MatchMode mode,             /* in */
            string text,                /* in */
            ref bool? match,            /* in, out */
            ref int nopCount,           /* in, out */
            ref int errorCount,         /* in, out */
            ref int includeCount,       /* in, out */
            ref int excludeCount,       /* in, out */
            ref IRule stopRule,         /* out */
            ref ResultList errors       /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rules == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("rules unavailable");
                    return ReturnCode.Error;
                }

                bool stopOnError;
                bool all;
                bool noCase;

                ExtractMetaModes(
                    mode, out stopOnError, out all, out noCase);

                LongList ids = new LongList(rules.Keys);

                ids.Sort(); /* O(N log N) */

                bool? localMatch = match;

                foreach (long id in ids)
                {
                    IRule rule;

                    if (!rules.TryGetValue(id, out rule))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "rule #{0} missing", id));

                        if (stopOnError)
                        {
                            stopRule = Rule.Empty;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (rule == null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "rule #{0} invalid", id));

                        if (stopOnError)
                        {
                            stopRule = Rule.Empty;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    long? ruleId = rule.Id;

                    if ((ruleId == null) ||
                        ((long)ruleId != id))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "rule #{0} mismatches #{1}",
                            FormatOps.WrapOrNull(rule.Id),
                            id));

                        if (stopOnError)
                        {
                            stopRule = Rule.Empty;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if ((kind != null) &&
                        (rule.Kind != (IdentifierKind)kind))
                    {
                        continue;
                    }

                    //
                    // HACK: This can be used by the caller to
                    //       limit candidate rules to a subset
                    //       of those within this set, e.g. to
                    //       include in the interpreter and/or
                    //       flag as "hidden", etc.
                    //
                    if (!rule.MatchAction(mode))
                        continue;

                    ReturnCode ruleCode;
                    bool? ruleMatch = null;
                    Result ruleError = null;

                    if (callback != null)
                    {
                        ruleCode = callback(
                            interpreter, clientData, kind,
                            mode, text, rule, ref ruleMatch,
                            ref errors);
                    }
                    else
                    {
                        bool localRuleMatch = false;

                        ruleCode = StringOps.MatchAnyOrAll(
                            interpreter, rule.Mode, text,
                            rule.Patterns, all, noCase,
                            GetComparer(rule), rule.RegExOptions,
                            ref localRuleMatch, ref ruleError);

                        ruleMatch = localRuleMatch;
                    }

                    if (ruleCode != ReturnCode.Ok)
                    {
                        errorCount++;

                        if (ruleError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "rule #{0} error", id));

                            errors.Add(ruleError);
                        }

                        if (stopOnError)
                        {
                            stopRule = rule;
                            return ReturnCode.Error;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (ruleMatch == null)
                        continue;

                    if ((bool)ruleMatch)
                    {
                        RuleType ruleType = rule.Type;

                        if (ruleType == RuleType.Include)
                        {
                            includeCount++;
                            localMatch = true;

                            if (!all)
                            {
                                stopRule = rule;
                                break;
                            }
                        }
                        else if (ruleType == RuleType.Exclude)
                        {
                            excludeCount++;
                            localMatch = false;

                            if (!all)
                            {
                                stopRule = rule;
                                break;
                            }
                        }
                        else
                        {
                            nopCount++;

                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "rule #{0} is nop", id));

                            if (stopOnError)
                            {
                                stopRule = rule;
                                return ReturnCode.Error;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }

                match = localMatch;
                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The client data associated with this rule set, which may be null.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the client data associated with this rule set.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); lock (syncRoot) { return clientData; } }
            set { CheckDisposed(); lock (syncRoot) { clientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleSetData Members
        /// <summary>
        /// The unique identifier of this rule set, which may be null prior to
        /// initialization.
        /// </summary>
        private long? id;

        /// <summary>
        /// Gets the unique identifier of this rule set.
        /// </summary>
        public long? Id
        {
            get { CheckDisposed(); lock (syncRoot) { return id; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The string comparer associated with this rule set, which may be
        /// null.
        /// </summary>
        private IComparer<string> comparer;

        /// <summary>
        /// Gets the string comparer associated with this rule set.
        /// </summary>
        public IComparer<string> Comparer
        {
            get { CheckDisposed(); lock (syncRoot) { return comparer; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleSet Members
        /// <summary>
        /// This method returns the name of this rule set, which is derived
        /// from its unique identifier.
        /// </summary>
        /// <returns>
        /// The name of this rule set, or null if it has not been assigned an
        /// identifier.
        /// </returns>
        public string GetName()
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (id == null)
                    return null;

                return String.Format("{0}{1}{2}",
                    typeof(RuleSet).Name, Characters.NumberSign, id);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this rule set contains no rules.
        /// </summary>
        /// <returns>
        /// True if this rule set is empty; otherwise, false.
        /// </returns>
        public bool IsEmpty()
        {
            CheckDisposed();

            return GetCount() <= 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this rule set as read-only, preventing any
        /// further modification to it.
        /// </summary>
        public void MakeReadOnly()
        {
            CheckDisposed();

            /* IGNORED */
            SetReadOnly(true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of rules in this rule set.
        /// </summary>
        /// <returns>
        /// The number of rules in this rule set, or an invalid count if the
        /// rules are unavailable.
        /// </returns>
        public int CountRules()
        {
            CheckDisposed();

            return GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all rules from this rule set.
        /// </summary>
        public void ClearRules()
        {
            CheckDisposed();
            CheckReadOnly();

            Initialize(true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a collection of deep copies of the rules in
        /// this rule set, in ascending identifier order.  The identifier of
        /// each copied rule is cleared.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The collection of copied rules, or null if the rules are
        /// unavailable.
        /// </returns>
        public IEnumerable<IRule> CopyRules(
            ref Result error /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rules == null)
                {
                    error = "rules unavailable";
                    return null;
                }

                IList<IRule> result = new List<IRule>();
                LongList ids = new LongList(rules.Keys);

                ids.Sort(); /* O(N log N) */

                foreach (long id in ids)
                {
                    IRule rule;

                    if (!rules.TryGetValue(id, out rule))
                        continue;

                    if (rule == null)
                        continue;

                    IRule newRule = rule.Clone() as IRule;

                    if (newRule == null)
                        continue;

                    newRule.SetId(null);
                    result.Add(newRule);
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds all rules in this rule set that match the
        /// criteria taken from the specified template rule.
        /// </summary>
        /// <param name="rule">
        /// The template rule whose properties are used as the matching
        /// criteria.
        /// </param>
        /// <param name="allowNone">
        /// Non-zero to use the none-valued properties of the template rule as
        /// matching criteria; zero to treat such properties as wildcards that
        /// match any value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The collection of matching rules, or null if no rules matched or
        /// the template rule was invalid.
        /// </returns>
        public IEnumerable<IRule> FindRules(
            IRule rule,      /* in */
            bool allowNone,  /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            if (rule == null)
            {
                error = "invalid rule";
                return null;
            }

            //
            // HACK: When the "allowNone" parameter is false, any
            //       enumeration values that are none will not be
            //       used for matching purposes.  Instead, a null
            //       value will be passed into one of the private
            //       Find*() methods.
            //
            RuleType? type = null;
            IdentifierKind? kind = null;
            MatchMode? mode = null;
            RegexOptions? regExOptions = null;

            if (allowNone)
            {
                type = rule.Type;
                kind = rule.Kind;
                mode = rule.Mode;
                regExOptions = rule.RegExOptions;
            }
            else
            {
                if (rule.Type != RuleType.None)
                    type = rule.Type;

                if (rule.Kind != IdentifierKind.None)
                    kind = rule.Kind;

                if (rule.Mode != MatchMode.None)
                    mode = rule.Mode;

                if (rule.RegExOptions != RegexOptions.None)
                    regExOptions = rule.RegExOptions;
            }

            return FindExact(
                type, kind, mode, regExOptions, rule.Patterns,
                rule.Comparer, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a new rule from the specified components, using
        /// a single pattern and no regular expression options, and adds it to
        /// this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the new rule.
        /// </param>
        /// <param name="kind">
        /// The identifier kind of the new rule.
        /// </param>
        /// <param name="mode">
        /// The match mode of the new rule.
        /// </param>
        /// <param name="pattern">
        /// The single pattern of the new rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly added rule, or null if it could not be added.
        /// </returns>
        public IRule BuildAndAddRule(
            RuleType type,       /* in */
            IdentifierKind kind, /* in */
            MatchMode mode,      /* in */
            string pattern,      /* in */
            ref Result error     /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            return Add(
                null, type, kind, mode, RegexOptions.None,
                new StringList(pattern), null, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a new rule from the specified components, using
        /// a collection of patterns and no regular expression options, and
        /// adds it to this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the new rule.
        /// </param>
        /// <param name="kind">
        /// The identifier kind of the new rule.
        /// </param>
        /// <param name="mode">
        /// The match mode of the new rule.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns of the new rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly added rule, or null if it could not be added.
        /// </returns>
        public IRule BuildAndAddRule(
            RuleType type,                /* in */
            IdentifierKind kind,          /* in */
            MatchMode mode,               /* in */
            IEnumerable<string> patterns, /* in */
            ref Result error              /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            return Add(
                null, type, kind, mode, RegexOptions.None,
                patterns, null, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a new rule from the specified components, using
        /// a collection of patterns and the specified regular expression
        /// options, and adds it to this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the new rule.
        /// </param>
        /// <param name="kind">
        /// The identifier kind of the new rule.
        /// </param>
        /// <param name="mode">
        /// The match mode of the new rule.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options of the new rule.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns of the new rule.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly added rule, or null if it could not be added.
        /// </returns>
        public IRule BuildAndAddRule(
            RuleType type,                /* in */
            IdentifierKind kind,          /* in */
            MatchMode mode,               /* in */
            RegexOptions regExOptions,    /* in */
            IEnumerable<string> patterns, /* in */
            ref Result error              /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            return Add(
                null, type, kind, mode, regExOptions,
                patterns, null, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a new rule from the specified components, using
        /// a collection of patterns, the specified regular expression options,
        /// and the specified string comparer, and adds it to this rule set.
        /// </summary>
        /// <param name="type">
        /// The type of the new rule.
        /// </param>
        /// <param name="kind">
        /// The identifier kind of the new rule.
        /// </param>
        /// <param name="mode">
        /// The match mode of the new rule.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options of the new rule.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns of the new rule.
        /// </param>
        /// <param name="comparer">
        /// The string comparer of the new rule, which may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The newly added rule, or null if it could not be added.
        /// </returns>
        public IRule BuildAndAddRule(
            RuleType type,                /* in */
            IdentifierKind kind,          /* in */
            MatchMode mode,               /* in */
            RegexOptions regExOptions,    /* in */
            IEnumerable<string> patterns, /* in */
            IComparer<string> comparer,   /* in */
            ref Result error              /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            return Add(
                null, type, kind, mode, regExOptions,
                patterns, comparer, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified rule to this rule set.
        /// </summary>
        /// <param name="rule">
        /// The rule to add.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the rule was added; otherwise, false.
        /// </returns>
        public bool AddRule(
            IRule rule,      /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            return Add(rule, ref error) != null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified rule from this rule set.
        /// </summary>
        /// <param name="rule">
        /// The rule to remove.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if the rule was removed; otherwise, false.
        /// </returns>
        public bool RemoveRule(
            IRule rule,      /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            return Remove(rule, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all rules from the specified rule set to this rule
        /// set, optionally moving them out of the source rule set rather than
        /// copying them.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set whose rules will be added.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop adding rules when an error is encountered;
        /// otherwise, processing continues with the remaining rules.
        /// </param>
        /// <param name="moveRules">
        /// Non-zero to remove the rules from the source rule set; zero to add
        /// deep copies of them.
        /// </param>
        /// <param name="count">
        /// Upon return, this parameter will be incremented by the number of
        /// rules that were processed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if all rules were added; otherwise, false.
        /// </returns>
        public bool AddRules(
            IRuleSet ruleSet, /* in */
            bool stopOnError, /* in */
            bool moveRules,   /* in */
            ref int count,    /* in, out */
            ref Result error  /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            IEnumerable<IRule> rules;
            ResultList errors = null;

            rules = GetRules(
                ruleSet, stopOnError, moveRules, ref errors);

            if (rules == null)
            {
                error = errors;
                return false;
            }

            return AddRules(rules, stopOnError, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds all rules from the specified collection to this
        /// rule set.
        /// </summary>
        /// <param name="rules">
        /// The collection of rules to add.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop adding rules when an error is encountered;
        /// otherwise, processing continues with the remaining rules.
        /// </param>
        /// <param name="count">
        /// Upon return, this parameter will be incremented by the number of
        /// rules that were processed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// True if all rules were added; otherwise, false.
        /// </returns>
        public bool AddRules(
            IEnumerable<IRule> rules, /* in */
            bool stopOnError,         /* in */
            ref int count,            /* in, out */
            ref Result error          /* out */
            )
        {
            CheckDisposed();
            CheckReadOnly();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rules == null)
                {
                    error = "invalid rules";
                    return false;
                }

                bool result = true;

                foreach (IRule rule in rules)
                {
                    if (rule == null)
                        continue;

                    if (!AddRule(rule, ref error))
                    {
                        result = false;

                        if (stopOnError)
                            break;
                    }

                    count++;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified callback for each rule in this
        /// rule set whose identifier kind matches.
        /// </summary>
        /// <param name="callback">
        /// The callback to invoke for each matching rule.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter to pass to the callback, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback, if any.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect, which controls whether iteration stops
        /// when an error is encountered.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode ForEachRule(
            RuleIterationCallback callback, /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in: OPTIONAL */
            IdentifierKind? kind,           /* in: OPTIONAL */
            MatchMode mode,                 /* in */
            ref Result error                /* out */
            )
        {
            CheckDisposed();

            int matchCount = 0;
            int errorCount = 0;
            IRule stopRule = null;
            ResultList errors = null;

            if (Iterate(callback, interpreter, clientData,
                    kind, mode, ref matchCount, ref errorCount,
                    ref stopRule, ref errors) == ReturnCode.Ok)
            {
                if (errors != null)
                    error = errors;

                return ReturnCode.Ok;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "ForEachRule: failed, {0}", FormatTrace(
                    interpreter, clientData, kind, mode, null,
                    null, Count.Invalid, matchCount, errorCount,
                    Count.Invalid, Count.Invalid, stopRule,
                    errors)), typeof(RuleSet).Name,
                    TracePriority.RuleError);
            }

            if (errors != null)
                error = errors;

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method matches the specified text against the rules in this
        /// rule set and returns whether the text is ultimately included,
        /// assuming an initial state of excluded and a default of excluded.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for matching, if any.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <returns>
        /// True if the text is included by the rules; otherwise, false.
        /// </returns>
        public bool ApplyRules(
            Interpreter interpreter, /* in */
            IdentifierKind? kind,    /* in */
            MatchMode mode,          /* in */
            string text              /* in */
            )
        {
            CheckDisposed();

            return ApplyRules(
                interpreter, kind, mode, text, false, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method matches the specified text against the rules in this
        /// rule set and returns whether the text is ultimately included, using
        /// the specified initial and default states.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for matching, if any.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <param name="initial">
        /// The initial include/exclude state, or null for no initial state.
        /// </param>
        /// <param name="default">
        /// The value to return when the rules produce no definite result.
        /// </param>
        /// <returns>
        /// True if the text is included by the rules; otherwise, false.  The
        /// default value is returned when the rules produce no definite
        /// result.
        /// </returns>
        public bool ApplyRules(
            Interpreter interpreter, /* in */
            IdentifierKind? kind,    /* in */
            MatchMode mode,          /* in */
            string text,             /* in */
            bool? initial,           /* in */
            bool @default            /* in */
            )
        {
            CheckDisposed();

            bool? match = initial;
            Result error = null;

            if ((ApplyRules(
                    null, interpreter, null, kind,
                    mode, text, @default, ref match,
                    ref error) == ReturnCode.Ok) &&
                (match != null))
            {
                return (bool)match;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method matches the specified text against the rules in this
        /// rule set, using the specified callback, and reports the resulting
        /// include/exclude state.
        /// </summary>
        /// <param name="callback">
        /// The callback used to perform matching for each rule, or null to use
        /// the default pattern matching.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter to use for matching, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback, if any.
        /// </param>
        /// <param name="kind">
        /// The identifier kind to match, or null to match any kind.
        /// </param>
        /// <param name="mode">
        /// The match mode in effect.
        /// </param>
        /// <param name="text">
        /// The text to match against the rules.
        /// </param>
        /// <param name="default">
        /// The default include/exclude state used for diagnostic purposes.
        /// </param>
        /// <param name="match">
        /// Upon return, this parameter will be modified to contain the overall
        /// match result, which may be null if no rule matched.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode ApplyRules(
            RuleMatchCallback callback, /* in */
            Interpreter interpreter,    /* in */
            IClientData clientData,     /* in: OPTIONAL */
            IdentifierKind? kind,       /* in */
            MatchMode mode,             /* in */
            string text,                /* in */
            bool @default,              /* in */
            ref bool? match,            /* in, out */
            ref Result error            /* out */
            )
        {
            CheckDisposed();

            int nopCount = 0;
            int errorCount = 0;
            int includeCount = 0;
            int excludeCount = 0;
            IRule stopRule = null;
            ResultList errors = null;

            if (Match(
                    callback, interpreter, clientData, kind,
                    mode, text, ref match, ref nopCount,
                    ref errorCount, ref includeCount,
                    ref excludeCount, ref stopRule,
                    ref errors) == ReturnCode.Ok)
            {
                if (match != null)
                {
                    if (errors != null)
                        error = errors;

                    return ReturnCode.Ok;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "ApplyRules: no result, {0}", FormatTrace(
                        interpreter, clientData, kind, mode, text,
                        match, nopCount, Count.Invalid, errorCount,
                        includeCount, excludeCount, stopRule,
                        errors)), typeof(RuleSet).Name,
                        TracePriority.RuleDebug);
                }
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "ApplyRules: failed, {0}", FormatTrace(
                    interpreter, clientData, kind, mode, text,
                    match, nopCount, Count.Invalid, errorCount,
                    includeCount, excludeCount, stopRule,
                    errors)), typeof(RuleSet).Name,
                    TracePriority.RuleError);
            }

            if (errors != null)
                error = errors;

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a deep copy of this rule set.
        /// </summary>
        /// <returns>
        /// The newly created copy of this rule set.
        /// </returns>
        public object Clone()
        {
            return new RuleSet(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this rule set,
        /// which is the list of the string forms of its rules.
        /// </summary>
        /// <returns>
        /// The string representation of this rule set.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// When non-zero, this rule set has been disposed and should no longer
        /// be used.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this rule set has already been
        /// disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this rule set has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(RuleSet).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this rule set.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        Reset();
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                // base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this rule set and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this rule set, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~RuleSet()
        {
            Dispose(false);
        }
        #endregion
    }
}
