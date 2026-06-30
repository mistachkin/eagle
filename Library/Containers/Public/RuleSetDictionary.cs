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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Interfaces.Public.IRuleSet>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IRuleSet>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a collection of named rule sets, mapping each
    /// name (a string key) to its <see cref="IRuleSet" /> instance.  It
    /// extends the underlying string-to-<see cref="IRuleSet" /> dictionary
    /// with helpers to build rule sets from their string forms, to merge and
    /// flatten rule sets together, and to render the keys and/or values as
    /// strings.  It is disposable; disposing the collection disposes every
    /// contained rule set.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6b3804d6-cbb3-4830-9cdf-5e3c6ab5f6e6")]
    public sealed class RuleSetDictionary : SomeDictionary, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty rule set dictionary.
        /// </summary>
        public RuleSetDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a rule set dictionary that is pre-populated with the
        /// entries from an existing dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are copied into the new collection.
        /// </param>
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
        /// <summary>
        /// Constructs a rule set dictionary from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// This method disposes every rule set contained in this collection.
        /// Null entries are skipped.  The dictionary entries themselves are
        /// not removed by this method.
        /// </summary>
        private void DisposeAll()
        {
            foreach (RuleSetPair pair in this)
            {
                IRuleSet ruleSet = pair.Value;

                if (ruleSet == null)
                    continue;

                ObjectOps.DisposeOrTrace<IRuleSet>(
                    null, ref ruleSet);

                ruleSet = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method merges a single rule set into the entry identified by
        /// the specified key.  When an entry already exists for the key, the
        /// rules from <paramref name="ruleSet" /> are added to it; otherwise,
        /// <paramref name="ruleSet" /> is stored directly under the key.
        /// </summary>
        /// <param name="key">
        /// The name (key) of the entry to merge into.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set whose rules are merged into the entry.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop merging at the first rule that cannot be added;
        /// zero to continue past such errors.
        /// </param>
        /// <param name="count">
        /// On input, the running total of rules merged so far; on output, it is
        /// increased by the number of rules added by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method creates a rule set dictionary from its string
        /// representation.  The string is first parsed into name/value pairs;
        /// each value is then interpreted as a rule set (either a nested list
        /// or a file reference, depending on <paramref name="ruleSetType" />)
        /// and stored under its associated name.
        /// </summary>
        /// <param name="value">
        /// The string representation to parse into name/value pairs.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when creating the contained rule sets.
        /// </param>
        /// <param name="ruleSetType">
        /// The flags that determine how each value is interpreted when creating
        /// its rule set.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero if duplicate keys encountered while parsing the string
        /// should be added only (i.e. not permitted to overwrite); zero
        /// otherwise.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the string consists of keys only (with no associated
        /// values); zero otherwise.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The newly created rule set dictionary, or null if it could not be
        /// created.
        /// </returns>
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

            bool[] success = new bool[] { false, false };
            RuleSetDictionary result = new RuleSetDictionary();

            try
            {
                RuleSetType baseRuleSetType =
                    ruleSetType & RuleSetType.BaseMask;

                foreach (StringPair pair in dictionary)
                {
                    string text = pair.Value;

                    if (text == null)
                        continue;

                    success[1] = false;

                    IRuleSet ruleSet = null;

                    try
                    {
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

                        success[1] = true;
                    }
                    finally
                    {
                        if (ruleSet != null)
                        {
                            if (!success[1])
                            {
                                ObjectOps.DisposeOrTrace<IRuleSet>(
                                    null, ref ruleSet);
                            }

                            ruleSet = null;
                        }
                    }
                }

                success[0] = true;

                return result;
            }
            catch (Exception e)
            {
                success[0] = false;

                error = e;
                return null;
            }
            finally
            {
                if (result != null)
                {
                    if (!success[0])
                    {
                        ObjectOps.DisposeOrTrace<RuleSetDictionary>(
                            null, ref result);
                    }

                    result = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method merges every rule set from another dictionary into this
        /// collection.  When <paramref name="key" /> is supplied, all of the
        /// source rule sets are merged into that single entry; otherwise, each
        /// source rule set is merged into the entry that shares its key.
        /// </summary>
        /// <param name="key">
        /// The optional name (key) under which all source rule sets should be
        /// merged.  This parameter may be null, in which case each source rule
        /// set is merged under its own key.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary whose rule sets are merged into this collection.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop merging at the first rule that cannot be added;
        /// zero to continue past such errors.
        /// </param>
        /// <param name="count">
        /// On input, the running total of rules merged so far; on output, it is
        /// increased by the number of rules added by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode MergeAll(
            string key,                   /* in: OPTIONAL */
            RuleSetDictionary dictionary, /* in */
            bool stopOnError,             /* in */
            ref int count,                /* in, out */
            ref Result error              /* out */
            )
        {
            CheckDisposed();

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

        /// <summary>
        /// This method combines the rules from the contained rule sets whose
        /// keys match the specified patterns into a single rule set.  When no
        /// rule set is supplied to receive the rules, a new one is created as
        /// needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when matching keys against the patterns.
        /// This parameter may be null.
        /// </param>
        /// <param name="ruleSet">
        /// The optional rule set that receives the combined rules.  This
        /// parameter may be null, in which case a new rule set is created.
        /// </param>
        /// <param name="mode">
        /// The matching mode to use when comparing keys against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The patterns used to select which entries are included.  This
        /// parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="all">
        /// Non-zero if a key must match all of the patterns; zero if matching
        /// any one of them is sufficient.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if key matching should be case-insensitive; zero
        /// otherwise.
        /// </param>
        /// <param name="stopOnError">
        /// Non-zero to stop at the first rule that cannot be added; zero to
        /// continue past such errors.
        /// </param>
        /// <param name="count">
        /// On input, the running total of rules combined so far; on output, it
        /// is increased by the number of rules added by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The rule set containing the combined rules, or null if it could not
        /// be produced.
        /// </returns>
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
            CheckDisposed();

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

                    newRuleSet = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method produces a space-separated string of the keys whose
        /// values match the specified pattern.
        /// </summary>
        /// <param name="mode">
        /// The matching mode to use when comparing against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which entries are included.  This
        /// parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching should be case-insensitive; zero otherwise.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is a
        /// regular expression mode.
        /// </param>
        /// <returns>
        /// The string containing the matching keys.
        /// </returns>
        public string KeysToString(
            MatchMode mode,           /* in */
            string pattern,           /* in */
            bool noCase,              /* in */
            RegexOptions regExOptions /* in */
            )
        {
            CheckDisposed();

            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string of all the keys joined by the
        /// specified separator.
        /// </summary>
        /// <param name="separator">
        /// The separator to place between adjacent keys.
        /// </param>
        /// <returns>
        /// The string containing the keys.
        /// </returns>
        public string KeysToString(
            string separator /* in */
            )
        {
            CheckDisposed();

            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the keys that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null, in which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching should be case-insensitive; zero otherwise.
        /// </param>
        /// <returns>
        /// The string containing the matching keys.
        /// </returns>
        public string KeysToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            CheckDisposed();

            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the keys that
        /// match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which keys are
        /// included.  This parameter may be null, in which case all keys are
        /// included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching.
        /// </param>
        /// <returns>
        /// The string containing the matching keys.
        /// </returns>
        public string KeysToString(
            string pattern,           /* in */
            RegexOptions regExOptions /* in */
            )
        {
            CheckDisposed();

            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the values whose
        /// string forms match the specified pattern.
        /// </summary>
        /// <param name="mode">
        /// The matching mode to use when comparing against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which entries are included.  This
        /// parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching should be case-insensitive; zero otherwise.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is a
        /// regular expression mode.
        /// </param>
        /// <returns>
        /// The string containing the matching values.
        /// </returns>
        public string ValuesToString(
            MatchMode mode,           /* in */
            string pattern,           /* in */
            bool noCase,              /* in */
            RegexOptions regExOptions /* in */
            )
        {
            CheckDisposed();

            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the values that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which values are included.  This
        /// parameter may be null, in which case all values are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching should be case-insensitive; zero otherwise.
        /// </param>
        /// <returns>
        /// The string containing the matching values.
        /// </returns>
        public string ValuesToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            CheckDisposed();

            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the values that
        /// match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which values are
        /// included.  This parameter may be null, in which case all values are
        /// included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching.
        /// </param>
        /// <returns>
        /// The string containing the matching values.
        /// </returns>
        public string ValuesToString(
            string pattern,           /* in */
            RegexOptions regExOptions /* in */
            )
        {
            CheckDisposed();

            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the keys and their
        /// values for the entries that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which entries are included.  This
        /// parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching should be case-insensitive; zero otherwise.
        /// </param>
        /// <returns>
        /// The string containing the matching keys and values.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            CheckDisposed();

            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the keys and their
        /// values for the entries that match the specified regular expression
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which entries are
        /// included.  This parameter may be null, in which case all entries are
        /// included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching.
        /// </param>
        /// <returns>
        /// The string containing the matching keys and values.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,           /* in */
            RegexOptions regExOptions /* in */
            )
        {
            CheckDisposed();

            StringList list = GenericOps<string, IRuleSet>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a space-separated string of the keys that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null, in which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if matching should be case-insensitive; zero otherwise.
        /// </param>
        /// <returns>
        /// The string containing the matching keys.
        /// </returns>
        public string ToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            CheckDisposed();

            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list,
                Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a space-separated string of all the keys in
        /// this collection.
        /// </summary>
        /// <returns>
        /// The string containing all the keys.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this collection has been disposed and is no longer
        /// usable.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an <see cref="ObjectDisposedException" /> if this
        /// collection has been disposed and the engine is configured to throw
        /// on use of disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(RuleSetDictionary).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this collection.  When
        /// disposing, every contained rule set is disposed and the collection
        /// is cleared.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
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

                        DisposeAll();
                        Clear();
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
        /// This method releases all resources used by this collection,
        /// disposing every contained rule set, and suppresses finalization.
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
        /// Finalizes this collection, releasing any resources that were not
        /// already released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~RuleSetDictionary()
        {
            Dispose(false);
        }
        #endregion
    }
}
