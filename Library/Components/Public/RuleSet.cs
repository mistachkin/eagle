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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2d251f5e-a5b7-40f2-a991-d06f9bcb78cb")]
    public sealed class RuleSet : IRuleSet, IHaveClientData, IDisposable
    {
        #region Private Data
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private long nextRuleId;
        private RuleDictionary rules;
        private bool readOnly;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IRuleSet Create(
            ref Result error /* out */
            )
        {
            return Create(
                null, null, Rule.DefaultAllowMissing,
                Rule.DefaultAllowExtra, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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

#if TEST
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
        private RuleSet()
            : base()
        {
            Initialize(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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

            ids.Sort(); /* O(N) */

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
        private void CheckReadOnly()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (readOnly)
                    throw new ScriptException("rule set is read-only");
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private StringList ToList()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringList list = new StringList();

                if (rules != null)
                {
                    LongList ids = new LongList(rules.Keys);

                    ids.Sort(); /* O(N) */

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

        private long? MaximumRuleId()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                long? maximumId = null;

                if (rules != null)
                {
                    LongList ids = new LongList(rules.Keys);

                    ids.Sort(); /* O(N) */

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

        private long GetRuleId(
            long? id /* in: OPTIONAL */
            )
        {
            if (id != null)
                return (long)id;

            return NextRuleId();
        }

        ///////////////////////////////////////////////////////////////////////

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

                        if (rule != null)
                            rule = rule.Clone() as IRule;

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

                ids.Sort(); /* O(N) */

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

                ids.Sort(); /* O(N) */

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

                ids.Sort(); /* O(N) */

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
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); lock (syncRoot) { return clientData; } }
            set { CheckDisposed(); lock (syncRoot) { clientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleSetData Members
        private long? id;
        public long? Id
        {
            get { CheckDisposed(); lock (syncRoot) { return id; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private IComparer<string> comparer;
        public IComparer<string> Comparer
        {
            get { CheckDisposed(); lock (syncRoot) { return comparer; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleSet Members
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

        public bool IsEmpty()
        {
            CheckDisposed();

            return GetCount() <= 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public void MakeReadOnly()
        {
            CheckDisposed();

            /* IGNORED */
            SetReadOnly(true);
        }

        ///////////////////////////////////////////////////////////////////////

        public int CountRules()
        {
            CheckDisposed();

            return GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public void ClearRules()
        {
            CheckDisposed();
            CheckReadOnly();

            Initialize(true);
        }

        ///////////////////////////////////////////////////////////////////////

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

                ids.Sort(); /* O(N) */

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

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(RuleSet).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~RuleSet()
        {
            Dispose(false);
        }
        #endregion
    }
}
