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

using RuleDictionary = System.Collections.Generic.Dictionary<
    long, Eagle._Interfaces.Public.IRule>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2d251f5e-a5b7-40f2-a991-d06f9bcb78cb")]
    public sealed class RuleSet : IRuleSet, IDisposable
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultAllowMissing = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private long nextId;
        private RuleDictionary rules;
        private IComparer<string> comparer;
        private bool readOnly;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IRuleSet Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            return Create(
                text, cultureInfo, DefaultAllowMissing, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IRuleSet Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            bool allowMissing,       /* in */
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
                            ref error);

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
                    ObjectOps.TryDisposeOrTrace<RuleSet>(
                        ref ruleSet);

                    ruleSet = null;
                }
            }
        }
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
                "interpreter = {0}, kind = {1}, mode = {2}, " +
                "text = {3}, match = {4}, nopCount = {5}, " +
                "matchCount = {6}, errorCount = {7}, " +
                "includeCount = {8}, excludeCount = {9}, " +
                "stopRule = {10}, errors = {11}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(kind), mode,
                FormatOps.WrapOrNull(text),
                FormatOps.WrapOrNull(match), nopCount,
                matchCount, errorCount, includeCount,
                excludeCount, FormatOps.WrapOrNull(stopRule),
                FormatOps.WrapOrNull(errors));
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
                Interlocked.Exchange(ref nextId, 0);

                if (rules != null)
                {
                    rules.Clear();
                    rules = null;
                }

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

        private long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        ///////////////////////////////////////////////////////////////////////

        private long GetId(
            long? id /* in: OPTIONAL */
            )
        {
            if (id != null)
                return (long)id;

            return NextId();
        }

        ///////////////////////////////////////////////////////////////////////

        private long GetId(
            IRule rule /* in: OPTIONAL */
            )
        {
            if (rule != null)
            {
                long? id = rule.Id;

                if (id != null)
                    return (long)id;

                id = NextId();
                rule.SetId(id);

                return (long)id;
            }

            return NextId();
        }

        ///////////////////////////////////////////////////////////////////////

        private IComparer<string> GetComparer(
            IRule rule /* in: OPTIONAL */
            )
        {
            IComparer<string> comparer = null;

            if (rule != null)
                comparer = rule.Comparer;

            if (comparer == null)
                comparer = this.Comparer;

            return comparer;
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
                GetId(id), type, kind, mode, regExOptions, patterns,
                comparer), ref error);
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
                rules[GetId(rule)] = rule;
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

                if (!rules.Remove(GetId(rule)))
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

                    if (callback(
                            interpreter, rule, ref stopOnError,
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

                    ReturnCode ruleCode;
                    bool? ruleMatch = null;
                    Result ruleError = null;

                    if (callback != null)
                    {
                        ruleCode = callback(
                            interpreter, kind, mode, text, rule,
                            ref ruleMatch, ref errors);
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

        #region IRuleSetData Members
        public IComparer<string> Comparer
        {
            get { CheckDisposed(); return comparer; }
            set { CheckDisposed(); comparer = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleSet Members
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
            IEnumerable<IRule> rules, /* in */
            bool stopOnError,         /* in */
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
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ForEachRule(
            RuleIterationCallback callback,
            Interpreter interpreter,
            IdentifierKind? kind,
            MatchMode mode,
            ref Result error
            )
        {
            CheckDisposed();

            int matchCount = 0;
            int errorCount = 0;
            IRule stopRule = null;
            ResultList errors = null;

            if (Iterate(callback, interpreter,
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
                    interpreter, kind, mode, null, null,
                    Count.Invalid, matchCount, errorCount,
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
            string text              /* in */
            )
        {
            CheckDisposed();

            return ApplyRules(
                interpreter, kind, MatchMode.RuleSetMask,
                text, false, false);
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
                    null, interpreter, kind, mode, text, @default,
                    ref match, ref error) == ReturnCode.Ok) &&
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
            IdentifierKind? kind,       /* in */
            MatchMode mode,             /* in */
            string text,                /* in */
            bool @default,              /* in */
            ref bool? match,            /* in */
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
                    callback, interpreter, kind, mode, text,
                    ref match, ref nopCount, ref errorCount,
                    ref includeCount, ref excludeCount,
                    ref stopRule, ref errors) == ReturnCode.Ok)
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
                        interpreter, kind, mode, text, match,
                        nopCount, Count.Invalid, errorCount,
                        includeCount, excludeCount, stopRule,
                        errors)), typeof(RuleSet).Name,
                        TracePriority.RuleDebug);
                }
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "ApplyRules: failed, {0}", FormatTrace(
                    interpreter, kind, mode, text, match,
                    nopCount, Count.Invalid, errorCount,
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
