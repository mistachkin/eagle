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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using Eagle._Components.Private;
using Eagle._Constants;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("bfeaaf10-14e2-4ed7-ac48-7c111ecda11d")]
    public sealed class Rule : IRule
    {
        #region Private Constants
        private const int RequiredFieldCount = 6;
        private const int OptionalFieldCount = 7;

        ///////////////////////////////////////////////////////////////////////

        internal static readonly IRule Empty = new Rule(
            0, RuleType.None, IdentifierKind.None, MatchMode.None,
            RegexOptions.None, null, null);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal Rule(
            long id,
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
            string value,            /* in */
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, false,
                    ref list, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            int count = list.Count;

            if (count < RequiredFieldCount)
            {
                error = String.Format(
                    "needs at least {0} elements, have {1}",
                    RequiredFieldCount, count);

                return null;
            }

            if (count > OptionalFieldCount)
            {
                error = String.Format(
                    "needs at most {0} elements, have {1}",
                    OptionalFieldCount, count);

                return null;
            }

            long id = 0;

            if (Value.GetWideInteger2(list[0],
                    ValueFlags.AnyInteger, cultureInfo,
                    ref id, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            object enumValue; /* REUSED */
            RuleType type = RuleType.None;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(RuleType), type.ToString(),
                list[1], cultureInfo, true, true, true,
                ref error);

            if (!(enumValue is RuleType))
                return null;

            type = (RuleType)enumValue;

            IdentifierKind kind = IdentifierKind.None;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(IdentifierKind), kind.ToString(),
                list[2], cultureInfo, true, true, true,
                ref error);

            if (!(enumValue is IdentifierKind))
                return null;

            kind = (IdentifierKind)enumValue;

            MatchMode mode = MatchMode.None;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(MatchMode), mode.ToString(),
                list[3], cultureInfo, true, true, true,
                ref error);

            if (!(enumValue is MatchMode))
                return null;

            mode = (MatchMode)enumValue;

            RegexOptions regExOptions = RegexOptions.None;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(RegexOptions), regExOptions.ToString(),
                list[4], cultureInfo, true, true, true, ref error);

            if (!(enumValue is RegexOptions))
                return null;

            regExOptions = (RegexOptions)enumValue;

            StringList patterns = null;

            if (ParserOps<string>.SplitList(
                    null, list[5], 0, Length.Invalid, false,
                    ref patterns, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            IComparer<string> comparer = null;

            if (count >= 7)
            {
                if (!String.IsNullOrEmpty(list[6]))
                {
                    comparer = StringOps.GetComparer(
                        null, list[6], cultureInfo, ref error);

                    if (comparer == null)
                        return null;
                }
            }

            return new Rule(
                id, type, kind, mode, regExOptions, patterns,
                comparer);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRule Members
        private long id;
        public long Id
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

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(
                id, type, kind, mode, regExOptions, patterns,
                comparer);
        }
        #endregion
    }
}
