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
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _StringPair = System.Collections.Generic.KeyValuePair<string, string>;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("bfeaaf10-14e2-4ed7-ac48-7c111ecda11d")]
    public sealed class Rule : IRule, IHaveClientData
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        internal static bool DefaultAllowMissing = true;
        internal static bool DefaultAllowExtra = false;

        ///////////////////////////////////////////////////////////////////////

        internal static readonly IRule Empty = new Rule(
            null, RuleType.None, IdentifierKind.None, MatchMode.None,
            RegexOptions.None, null, null, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static StringDictionary allFieldNames = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal Rule(
            IRule rule,
            bool deepCopy
            )
        {
            if (rule != null)
            {
                this.id = rule.Id;
                this.type = rule.Type;
                this.kind = rule.Kind;
                this.mode = rule.Mode;
                this.regExOptions = rule.RegExOptions;
                this.patterns = GetPatterns(rule.Patterns, deepCopy);
                this.patterns = rule.Patterns;
                this.comparer = rule.Comparer;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal Rule(
            long? id,
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            RegexOptions regExOptions,
            IEnumerable<string> patterns,
            IComparer<string> comparer,
            bool deepCopy
            )
        {
            this.id = id;
            this.type = type;
            this.kind = kind;
            this.mode = mode;
            this.regExOptions = regExOptions;
            this.patterns = GetPatterns(patterns, deepCopy);
            this.patterns = patterns;
            this.comparer = comparer;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        //
        // WARNING: For use by constructors only.
        //
        private static IEnumerable<string> GetPatterns(
            IEnumerable<string> patterns, /* in: OPTIONAL */
            bool deepCopy                 /* in */
            )
        {
            if (deepCopy)
            {
                return (patterns != null) ?
                    new StringList(patterns) : null;
            }
            else
            {
                return patterns;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by static factory methods only.
        //
        private static void SetDefaultValues(
            out long? id,                  /* out */
            out RuleType type,             /* out */
            out IdentifierKind kind,       /* out */
            out MatchMode mode,            /* out */
            out RegexOptions regExOptions, /* out */
            out StringList patterns,       /* out */
            out IComparer<string> comparer /* out */
            )
        {
            id = null;
            type = RuleType.None;
            kind = IdentifierKind.None;
            mode = MatchMode.None;
            regExOptions = RegexOptions.None;
            patterns = null;
            comparer = null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by static factory methods only.
        //
        private static void InitializeAllFieldNames(
            bool force, /* in */
            bool clear  /* in */
            )
        {
            if (force || (allFieldNames == null))
            {
                if (allFieldNames == null)
                    allFieldNames = new StringDictionary();
                else if (clear)
                    allFieldNames.Clear();

                allFieldNames["id"] = typeof(Int64).Name;
                allFieldNames["type"] = typeof(RuleType).Name;
                allFieldNames["kind"] = typeof(IdentifierKind).Name;
                allFieldNames["mode"] = typeof(MatchMode).Name;
                allFieldNames["regExOptions"] = typeof(RegexOptions).Name;
                allFieldNames["patterns"] = typeof(StringList).Name;
                allFieldNames["comparer"] = typeof(IComparer<string>).Name;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        internal static IRule Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            return Create(
                text, cultureInfo, DefaultAllowMissing,
                DefaultAllowExtra, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IRule Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            bool allowMissing,       /* in */
            bool allowExtra,         /* in */
            ref Result error         /* out */
            )
        {
            StringDictionary dictionary = StringDictionary.FromString(
                text, true, ref error);

            if (dictionary == null)
                return null;

            ///////////////////////////////////////////////////////////////////

            long? id;
            RuleType type;
            IdentifierKind kind;
            MatchMode mode;
            RegexOptions regExOptions;
            StringList patterns;
            IComparer<string> comparer;

            SetDefaultValues(
                out id, out type, out kind, out mode, out regExOptions,
                out patterns, out comparer);

            ///////////////////////////////////////////////////////////////////

            string value; /* REUSED */
            object enumValue; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            if (dictionary.TryGetValue("id", out value))
            {
                long localId = 0;

                if (Value.GetWideInteger2(value,
                        ValueFlags.AnyInteger, cultureInfo,
                        ref localId, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                id = localId;
            }
            else if (!allowMissing)
            {
                error = "missing required dictionary value \"id\"";
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            if (dictionary.TryGetValue("type", out value))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(RuleType), type.ToString(), value,
                    cultureInfo, true, true, true, ref error);

                if (!(enumValue is RuleType))
                    return null;

                type = (RuleType)enumValue;
            }
            else if (!allowMissing)
            {
                error = "missing required dictionary value \"type\"";
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            if (dictionary.TryGetValue("kind", out value))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(IdentifierKind), kind.ToString(), value,
                    cultureInfo, true, true, true, ref error);

                if (!(enumValue is IdentifierKind))
                    return null;

                kind = (IdentifierKind)enumValue;
            }
            else if (!allowMissing)
            {
                error = "missing required dictionary value \"kind\"";
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            if (dictionary.TryGetValue("mode", out value))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(MatchMode), mode.ToString(), value,
                    cultureInfo, true, true, true, ref error);

                if (!(enumValue is MatchMode))
                    return null;

                mode = (MatchMode)enumValue;
            }
            else if (!allowMissing)
            {
                error = "missing required dictionary value \"mode\"";
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            if (dictionary.TryGetValue("regExOptions", out value))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(RegexOptions), regExOptions.ToString(),
                    value, cultureInfo, true, true, true, ref error);

                if (!(enumValue is RegexOptions))
                    return null;

                regExOptions = (RegexOptions)enumValue;
            }
            else if (!allowMissing)
            {
                error = "missing required dictionary value \"regExOptions\"";
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            if (dictionary.TryGetValue("patterns", out value))
            {
                if (ParserOps<string>.SplitList(
                        null, value, 0, Length.Invalid, false,
                        ref patterns, ref error) != ReturnCode.Ok)
                {
                    return null;
                }
            }
            else if (!allowMissing)
            {
                error = "missing required dictionary value \"patterns\"";
                return null;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: This is always optional.
            //
            if (dictionary.TryGetValue("comparer", out value))
            {
                if (!String.IsNullOrEmpty(value))
                {
                    comparer = StringOps.GetComparer(
                        null, value, cultureInfo, ref error);

                    if (comparer == null)
                        return null;
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!allowExtra)
            {
                InitializeAllFieldNames(false, false);

                if (allFieldNames != null)
                {
                    foreach (_StringPair pair in dictionary)
                    {
                        string name = pair.Key;

                        if (name == null) /* IMPOSSIBLE */
                            continue;

                        if (!allFieldNames.ContainsKey(name))
                        {
                            error = String.Format(
                                "unsupported dictionary value {0}",
                                FormatOps.WrapOrNull(name));

                            return null;
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            return new Rule(
                id, type, kind, mode, regExOptions, patterns, comparer,
                false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleData Members
        private long? id;
        public long? Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        private RuleType type;
        public RuleType Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MatchMode mode;
        public MatchMode Mode
        {
            get { return mode; }
        }

        ///////////////////////////////////////////////////////////////////////

        private RegexOptions regExOptions;
        public RegexOptions RegExOptions
        {
            get { return regExOptions; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> patterns;
        public IEnumerable<string> Patterns
        {
            get { return patterns; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IComparer<string> comparer;
        public IComparer<string> Comparer
        {
            get { return comparer; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            //
            // NOTE: Create a "deep copy" of this object; requires
            //       creating a copy of its pattern list (if any)
            //       because lists are not immutable.  Fortunately,
            //       all other rule data is immutable.
            //
            return new Rule(
                id, type, kind, mode, regExOptions, patterns, comparer,
                true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRule Members
        public void SetId(
            long? id /* in */
            )
        {
            this.id = id;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MatchAction(
            MatchMode mode /* in */
            )
        {
            MatchMode localMode = mode & MatchMode.ActionFlagsMask;

            if (localMode == MatchMode.None)
                return true;

            return FlagOps.HasFlags(
                this.mode & MatchMode.ActionFlagsMask, localMode, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            StringList list = new StringList();

            if (id != null)
            {
                list.Add("id");
                list.Add(((long)id).ToString());
            }

            if (type != RuleType.None)
            {
                list.Add("type");
                list.Add(type.ToString());
            }

            if (kind != IdentifierKind.None)
            {
                list.Add("kind");
                list.Add(kind.ToString());
            }

            if (mode != MatchMode.None)
            {
                list.Add("mode");
                list.Add(mode.ToString());
            }

            if (regExOptions != RegexOptions.None)
            {
                list.Add("regExOptions");
                list.Add(regExOptions.ToString());
            }

            if (patterns != null)
            {
                list.Add("patterns");
                list.Add(patterns.ToString());
            }

            if (comparer != null)
            {
                list.Add("comparer");
                list.Add(comparer.ToString());
            }

            return list.ToString();
        }
        #endregion
    }
}
