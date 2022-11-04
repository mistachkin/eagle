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

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("bfeaaf10-14e2-4ed7-ac48-7c111ecda11d")]
    public sealed class Rule : IRule
    {
        #region Private Constants
        internal static readonly IRule Empty = new Rule(
            null, RuleType.None, IdentifierKind.None, MatchMode.None,
            RegexOptions.None, null, null);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal Rule(
            long? id,
            RuleType type,
            IdentifierKind kind,
            MatchMode mode,
            RegexOptions regExOptions,
            IEnumerable<string> patterns,
            IComparer<string> comparer
            )
        {
            this.id = id;
            this.type = type;
            this.kind = kind;
            this.mode = mode;
            this.regExOptions = regExOptions;
            this.patterns = patterns;
            this.comparer = comparer;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        internal static IRule Create(
            string text,             /* in */
            CultureInfo cultureInfo, /* in */
            bool allowMissing,       /* in */
            ref Result error         /* out */
            )
        {
            StringDictionary dictionary = StringDictionary.FromString(
                text, true, ref error);

            if (dictionary == null)
                return null;

            string value; /* REUSED */
            object enumValue; /* REUSED */

            long? id = null;
            RuleType type = RuleType.None;
            IdentifierKind kind = IdentifierKind.None;
            MatchMode mode = MatchMode.None;
            RegexOptions regExOptions = RegexOptions.None;
            StringList patterns = null;
            IComparer<string> comparer = null;

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

            return new Rule(
                id, type, kind, mode, regExOptions, patterns, comparer);
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
                id, type, kind, mode, regExOptions, (patterns != null) ?
                    new StringList(patterns) : null, comparer);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRule Members
        public void SetId(long? id)
        {
            this.id = id;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(
                "id", id, "type", type, "kind", kind, "mode", mode,
                "regExOptions", regExOptions, "patterns", patterns,
                "comparer", comparer);
        }
        #endregion
    }
}
