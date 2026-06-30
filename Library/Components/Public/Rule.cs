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
    /// <summary>
    /// This class represents a single matching rule, pairing a rule type, an
    /// identifier kind, a match mode, and a set of patterns (with their
    /// associated regular expression options and string comparer) so that an
    /// identifier can be tested for inclusion or exclusion.  A rule may be
    /// created directly or parsed from a dictionary-style string via the static
    /// factory methods, and it carries an optional identifier and client data.
    /// It implements <see cref="IRule" /> and is cloneable.
    /// </summary>
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
        /// <summary>
        /// The default value used to determine whether missing dictionary
        /// values are permitted when parsing a rule.
        /// </summary>
        internal static bool DefaultAllowMissing = true;
        /// <summary>
        /// The default value used to determine whether extra (unsupported)
        /// dictionary values are permitted when parsing a rule.
        /// </summary>
        internal static bool DefaultAllowExtra = false;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A shared, pre-built rule instance that carries no values.
        /// </summary>
        internal static readonly IRule Empty = new Rule(
            null, RuleType.None, IdentifierKind.None, MatchMode.None,
            RegexOptions.None, null, null, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The cached collection of all supported dictionary field names, keyed
        /// by name, used to validate parsed rules.
        /// </summary>
        private static StringDictionary allFieldNames = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a rule by copying the values from the specified existing
        /// rule.
        /// </summary>
        /// <param name="rule">
        /// The existing rule whose values are copied.  This parameter may be
        /// null, in which case no values are copied.
        /// </param>
        /// <param name="deepCopy">
        /// Non-zero to make a copy of the pattern list rather than sharing the
        /// existing one.
        /// </param>
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

        /// <summary>
        /// Constructs a rule from the fully specified set of values.
        /// </summary>
        /// <param name="id">
        /// The optional unique identifier of this rule.  This parameter may be
        /// null.
        /// </param>
        /// <param name="type">
        /// The type of this rule.
        /// </param>
        /// <param name="kind">
        /// The identifier kind that this rule applies to.
        /// </param>
        /// <param name="mode">
        /// The match mode used by this rule.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when matching patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns associated with this rule.  This
        /// parameter may be null.
        /// </param>
        /// <param name="comparer">
        /// The optional string comparer used when matching patterns.  This
        /// parameter may be null.
        /// </param>
        /// <param name="deepCopy">
        /// Non-zero to make a copy of the pattern list rather than sharing the
        /// supplied one.
        /// </param>
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
        /// <summary>
        /// This method returns the pattern collection to store on a rule,
        /// optionally making a copy of it.  For use by constructors only.
        /// </summary>
        /// <param name="patterns">
        /// The collection of patterns.  This parameter may be null.
        /// </param>
        /// <param name="deepCopy">
        /// Non-zero to return a copy of the pattern collection rather than the
        /// supplied one.
        /// </param>
        /// <returns>
        /// The pattern collection to store, or null when none was supplied.
        /// </returns>
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
        /// <summary>
        /// This method sets the supplied parameters to the default values used
        /// when parsing a rule.  For use by static factory methods only.
        /// </summary>
        /// <param name="id">
        /// On output, receives the default identifier (null).
        /// </param>
        /// <param name="type">
        /// On output, receives the default rule type.
        /// </param>
        /// <param name="kind">
        /// On output, receives the default identifier kind.
        /// </param>
        /// <param name="mode">
        /// On output, receives the default match mode.
        /// </param>
        /// <param name="regExOptions">
        /// On output, receives the default regular expression options.
        /// </param>
        /// <param name="patterns">
        /// On output, receives the default pattern list (null).
        /// </param>
        /// <param name="comparer">
        /// On output, receives the default string comparer (null).
        /// </param>
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
        /// <summary>
        /// This method populates the cached collection of all supported
        /// dictionary field names, used to validate parsed rules.  For use by
        /// static factory methods only.
        /// </summary>
        /// <param name="force">
        /// Non-zero to repopulate the collection even when it has already been
        /// populated.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the existing collection before repopulating it.
        /// </param>
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
        /// <summary>
        /// This method creates a rule by parsing the specified dictionary-style
        /// string, using the default policy for missing and extra dictionary
        /// values.
        /// </summary>
        /// <param name="text">
        /// The dictionary-style string to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing values.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The newly created rule, or null if the string could not be parsed.
        /// </returns>
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

        /// <summary>
        /// This method creates a rule by parsing the specified dictionary-style
        /// string, using the specified policy for missing and extra dictionary
        /// values.
        /// </summary>
        /// <param name="text">
        /// The dictionary-style string to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing values.  This parameter may be null.
        /// </param>
        /// <param name="allowMissing">
        /// Non-zero to permit required dictionary values to be missing; zero to
        /// fail when a required value is absent.
        /// </param>
        /// <param name="allowExtra">
        /// Non-zero to permit extra (unsupported) dictionary values; zero to
        /// fail when an unsupported value is present.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The newly created rule, or null if the string could not be parsed.
        /// </returns>
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
        /// <summary>
        /// The client data associated with this rule.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this rule.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRuleData Members
        /// <summary>
        /// The optional unique identifier of this rule.
        /// </summary>
        private long? id;
        /// <summary>
        /// Gets the optional unique identifier of this rule.
        /// </summary>
        public long? Id
        {
            get { return id; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type of this rule.
        /// </summary>
        private RuleType type;
        /// <summary>
        /// Gets the type of this rule.
        /// </summary>
        public RuleType Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The identifier kind that this rule applies to.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets the identifier kind that this rule applies to.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return kind; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The match mode used by this rule.
        /// </summary>
        private MatchMode mode;
        /// <summary>
        /// Gets the match mode used by this rule.
        /// </summary>
        public MatchMode Mode
        {
            get { return mode; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression options used when matching patterns.
        /// </summary>
        private RegexOptions regExOptions;
        /// <summary>
        /// Gets the regular expression options used when matching patterns.
        /// </summary>
        public RegexOptions RegExOptions
        {
            get { return regExOptions; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The collection of patterns associated with this rule.
        /// </summary>
        private IEnumerable<string> patterns;
        /// <summary>
        /// Gets the collection of patterns associated with this rule.
        /// </summary>
        public IEnumerable<string> Patterns
        {
            get { return patterns; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The optional string comparer used when matching patterns.
        /// </summary>
        private IComparer<string> comparer;
        /// <summary>
        /// Gets the optional string comparer used when matching patterns.
        /// </summary>
        public IComparer<string> Comparer
        {
            get { return comparer; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a deep copy of this rule, including a copy of its
        /// pattern list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this rule.
        /// </returns>
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
        /// <summary>
        /// This method sets the optional unique identifier of this rule.
        /// </summary>
        /// <param name="id">
        /// The identifier to set.  This parameter may be null.
        /// </param>
        public void SetId(
            long? id /* in */
            )
        {
            this.id = id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this rule's action flags include the
        /// action flags of the specified match mode.
        /// </summary>
        /// <param name="mode">
        /// The match mode whose action flags are tested against this rule's
        /// action flags.
        /// </param>
        /// <returns>
        /// True if the specified mode has no action flags or this rule's action
        /// flags include them; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method builds the dictionary-style string representation of this
        /// rule, including only the values that differ from their defaults.
        /// </summary>
        /// <returns>
        /// The string representation of this rule.
        /// </returns>
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
