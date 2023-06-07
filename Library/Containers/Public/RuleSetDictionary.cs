/*
 * RuleSetDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

using StringPair = System.Collections.Generic.KeyValuePair<string, string>;

using RuleSetPair = System.Collections.Generic.KeyValuePair<string,
    Eagle._Interfaces.Public.IRuleSet>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6b3804d6-cbb3-4830-9cdf-5e3c6ab5f6e6")]
    public sealed class RuleSetDictionary : Dictionary<string, IRuleSet>
    {
        #region Public Constructors
        public RuleSetDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public RuleSetDictionary(
            IDictionary<string, IRuleSet> dictionary /* in */
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private RuleSetDictionary(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private ReturnCode MergeAll(
            string key,       /* in */
            IRuleSet ruleSet, /* in */
            bool stopOnError, /* in */
            ref int count,    /* in, out */
            ref Result error  /* out */
            )
        {
            if (key == null)
            {
                error = "invalid key";
                return ReturnCode.Error;
            }

            if (ruleSet == null)
            {
                error = "invalid ruleset";
                return ReturnCode.Error;
            }

            IRuleSet oldRuleSet;
            int addCount;

            /* IGNORED */
            TryGetValue(key, out oldRuleSet);

            if (oldRuleSet != null)
            {
                addCount = 0;

                if (!oldRuleSet.AddRules(
                        ruleSet, stopOnError, false,
                        ref addCount, ref error))
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                addCount = ruleSet.CountRules();

                this[key] = ruleSet;
            }

            if (addCount > 0)
                count += addCount;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static RuleSetDictionary FromString(
            string value,            /* in */
            CultureInfo cultureInfo, /* in */
            RuleSetType ruleSetType, /* in */
            bool addOnly,            /* in */
            bool keysOnly,           /* in */
            ref Result error         /* out */
            )
        {
            StringDictionary dictionary = StringDictionary.FromString(
                value, addOnly, keysOnly, ref error);

            if (dictionary == null)
                return null;

            RuleSetDictionary result = new RuleSetDictionary();

            RuleSetType baseRuleSetType =
                ruleSetType & RuleSetType.BaseMask;

            foreach (StringPair pair in dictionary)
            {
                string text = pair.Value;

                if (text == null)
                    continue;

                IRuleSet ruleSet;

                if (baseRuleSetType == RuleSetType.NestedList)
                {
                    ruleSet = RuleSet.Create(
                        text, cultureInfo, ref error);
                }
                else
                {
#if TEST
                    ruleSet = RuleSet.CreateFromFile(
                        text, null, ruleSetType, ref error);
#else
                    error = "not implemented";
                    ruleSet = null;
#endif
                }

                if (ruleSet == null)
                    return null;

                string name = pair.Key;

                if (String.IsNullOrEmpty(name))
                    name = null;

                if (name == null)
                    name = ruleSet.GetName();

                if (name == null)
                {
                    error = String.Format(
                        "no name available for ruleset {0}",
                        FormatOps.WrapOrNull(ruleSet.Id));

                    return null;
                }

                result[name] = ruleSet;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public ReturnCode MergeAll(
            string key,                   /* in: OPTIONAL */
            RuleSetDictionary dictionary, /* in */
            bool stopOnError,             /* in */
            ref int count,                /* in, out */
            ref Result error              /* out */
            )
        {
            if (dictionary == null)
            {
                error = "invalid dictionary";
                return ReturnCode.Error;
            }

            int newCount = 0;

            foreach (RuleSetPair pair in dictionary)
            {
                string localKey = (key != null) ?
                    key : pair.Key;

                IRuleSet ruleSet = pair.Value;

                if (ruleSet == null)
                    continue;

                int addCount = 0;

                if (MergeAll(localKey,
                        ruleSet, stopOnError, ref addCount,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (addCount > 0)
                    newCount += addCount;
            }

            if (newCount > 0)
                count += newCount;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public IRuleSet FlattenAll(
            Interpreter interpreter,      /* in: OPTIONAL */
            IRuleSet ruleSet,             /* in: OPTIONAL */
            MatchMode mode,               /* in */
            IEnumerable<string> patterns, /* in */
            bool all,                     /* in */
            bool noCase,                  /* in */
            bool stopOnError,             /* in */
            ref int count,                /* in, out */
            ref Result error              /* out */
            )
        {
            bool success = false;
            IRuleSet newRuleSet = ruleSet;

            try
            {
                int newCount = 0;

                foreach (RuleSetPair pair in this)
                {
                    IRuleSet item = pair.Value;

                    if (item == null)
                        continue;

                    if ((patterns != null) && !StringOps.MatchAnyOrAll(
                            interpreter, mode, pair.Key, patterns, all,
                            noCase))
                    {
                        continue;
                    }

                    if (newRuleSet == null)
                    {
                        newRuleSet = RuleSet.Create(ref error);

                        if (newRuleSet == null)
                            return null;
                    }

                    int addCount = 0;

                    if (!newRuleSet.AddRules(
                            item, stopOnError, false, ref addCount,
                            ref error))
                    {
                        return null;
                    }

                    if (addCount > 0)
                        newCount += addCount;
                }

                if ((newRuleSet != null) && (newCount > 0))
                    count += newCount;

                return newRuleSet;
            }
            finally
            {
                if (!success && (newRuleSet != null) &&
                    !Object.ReferenceEquals(newRuleSet, ruleSet))
                {
                    ObjectOps.TryDisposeOrTrace<IRuleSet>(
                        ref newRuleSet);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string KeysToString(
            MatchMode mode,           /* in */
            string pattern,           /* in */
            bool noCase,              /* in */
            RegexOptions regExOptions /* in */
            )
        {
            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string separator /* in */
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string pattern,           /* in */
            RegexOptions regExOptions /* in */
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            MatchMode mode,           /* in */
            string pattern,           /* in */
            bool noCase,              /* in */
            RegexOptions regExOptions /* in */
            )
        {
            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,           /* in */
            RegexOptions regExOptions /* in */
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern,           /* in */
            RegexOptions regExOptions /* in */
            )
        {
            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list,
                Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
