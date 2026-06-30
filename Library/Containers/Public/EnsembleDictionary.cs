/*
 * EnsembleDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Interfaces.Public.ISubCommand>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.ISubCommand>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents an ordered mapping of sub-command names to their
    /// associated <see cref="ISubCommand" /> implementations, as used by an
    /// ensemble command.  It maintains a cache of the contained names and
    /// invalidates that cache whenever the contents of the dictionary are
    /// modified.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4c29e083-c823-43a1-9fc0-58d6737f3ce2")]
    public sealed class EnsembleDictionary :
            SomeDictionary, IDictionary<string, ISubCommand>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public EnsembleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the entries
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose sub-command entries are copied into the new
        /// instance.
        /// </param>
        public EnsembleDictionary(
            IDictionary<string, ISubCommand> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified sub-command
        /// names, associating each name with a null sub-command value.
        /// </summary>
        /// <param name="collection">
        /// The collection of sub-command names to add to the new instance.
        /// </param>
        public EnsembleDictionary(
            IEnumerable<string> collection
            )
            : this()
        {
            foreach (string item in collection)
                this.Add(item, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified collection
        /// of name and sub-command pairs.
        /// </summary>
        /// <param name="collection">
        /// The collection of name and sub-command pairs to add to the new
        /// instance.
        /// </param>
        public EnsembleDictionary(
            IEnumerable<KeyValuePair<string, ISubCommand>> collection
            )
            : this()
        {
            foreach (KeyValuePair<string, ISubCommand> pair in collection)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the keys of the specified
        /// dictionary as sub-command names, associating each name with a null
        /// sub-command value.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys are used as the sub-command names for the
        /// new instance.
        /// </param>
        public EnsembleDictionary(
            IDictionary<string, string> dictionary
            )
            : this()
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
                this.Add(pair.Key, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class using previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the new instance.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source and destination of
        /// the serialized data.
        /// </param>
        private EnsembleDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<KeyValuePair<string, ISubCommand>> Overrides
        /// <summary>
        /// This method is not supported and always throws an exception.
        /// </summary>
        /// <param name="item">
        /// The name and sub-command pair that would have been added.
        /// </param>
        void ICollection<KeyValuePair<string, ISubCommand>>.Add(
            KeyValuePair<string, ISubCommand> item
            )
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws an exception.
        /// </summary>
        void ICollection<KeyValuePair<string, ISubCommand>>.Clear()
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws an exception.
        /// </summary>
        /// <param name="item">
        /// The name and sub-command pair that would have been removed.
        /// </param>
        /// <returns>
        /// This method does not return; it always throws an exception.
        /// </returns>
        bool ICollection<KeyValuePair<string, ISubCommand>>.Remove(
            KeyValuePair<string, ISubCommand> item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<string, ISubCommand> Overrides
        /// <summary>
        /// Gets or sets the sub-command associated with the specified name,
        /// clearing the cached names whenever a value is set.
        /// </summary>
        /// <param name="key">
        /// The name of the sub-command to get or set.
        /// </param>
        /// <returns>
        /// The sub-command associated with the specified name.
        /// </returns>
        ISubCommand IDictionary<string, ISubCommand>.this[string key]
        {
            get { return base[key]; /* throw */ }
            set
            {
                ClearCachedNames();

                base[key] = value; /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified name and sub-command to the
        /// dictionary, clearing the cached names beforehand.
        /// </summary>
        /// <param name="key">
        /// The name of the sub-command to add.
        /// </param>
        /// <param name="value">
        /// The sub-command to associate with the specified name.
        /// </param>
        void IDictionary<string, ISubCommand>.Add(
            string key,
            ISubCommand value
            )
        {
            ClearCachedNames();

            base.Add(key, value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the sub-command with the specified name from the
        /// dictionary, clearing the cached names beforehand.
        /// </summary>
        /// <param name="key">
        /// The name of the sub-command to remove.
        /// </param>
        /// <returns>
        /// True if the sub-command was found and removed; otherwise, false.
        /// </returns>
        bool IDictionary<string, ISubCommand>.Remove(
            string key
            )
        {
            ClearCachedNames();

            return base.Remove(key); /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<string, ISubCommand> Overrides
        /// <summary>
        /// Gets or sets the sub-command associated with the specified name,
        /// clearing the cached names whenever a value is set.
        /// </summary>
        /// <param name="key">
        /// The name of the sub-command to get or set.
        /// </param>
        /// <returns>
        /// The sub-command associated with the specified name.
        /// </returns>
        public new ISubCommand this[string key]
        {
            get { return base[key]; /* throw */ }
            set
            {
                ClearCachedNames();

                base[key] = value; /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified name and sub-command to the
        /// dictionary, clearing the cached names beforehand.
        /// </summary>
        /// <param name="key">
        /// The name of the sub-command to add.
        /// </param>
        /// <param name="value">
        /// The sub-command to associate with the specified name.
        /// </param>
        public new void Add(
            string key,
            ISubCommand value
            )
        {
            ClearCachedNames();

            base.Add(key, value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the sub-command with the specified name from the
        /// dictionary, clearing the cached names beforehand.
        /// </summary>
        /// <param name="key">
        /// The name of the sub-command to remove.
        /// </param>
        /// <returns>
        /// True if the sub-command was found and removed; otherwise, false.
        /// </returns>
        public new bool Remove(
            string key
            )
        {
            ClearCachedNames();

            return base.Remove(key); /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<TKey, TValue> Overrides
        /// <summary>
        /// This method removes all sub-commands from the dictionary, clearing
        /// the cached names beforehand.
        /// </summary>
        public new void Clear()
        {
            ClearCachedNames();

            base.Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method clears and discards the cache of contained sub-command
        /// names, if any, so that it will be recomputed on demand.
        /// </summary>
        private void ClearCachedNames()
        {
            if (cachedNames != null)
            {
                cachedNames.Clear();
                cachedNames = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Properties
        /// <summary>
        /// The cache of contained sub-command names, or null if it has not yet
        /// been computed.
        /// </summary>
        private StringDictionary cachedNames;
        /// <summary>
        /// Gets or sets the cache of contained sub-command names, or null if it
        /// has not yet been computed.
        /// </summary>
        internal StringDictionary CachedNames
        {
            get { return cachedNames; }
            set { cachedNames = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method merges the entries from the specified dictionary into
        /// this dictionary, clearing the cached names beforehand.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are merged into this dictionary.  If
        /// this value is null, no entries are merged.
        /// </param>
        /// <param name="force">
        /// Non-zero to overwrite entries that already exist in this dictionary;
        /// otherwise, existing entries are left unchanged.
        /// </param>
        /// <returns>
        /// The number of entries that were added or overwritten, or a negative
        /// value if the specified dictionary was null.
        /// </returns>
        public int Merge(
            IDictionary<string, ISubCommand> dictionary,
            bool force
            )
        {
            ClearCachedNames();

            if (dictionary == null)
                return _Constants.Count.Invalid;

            int count = 0;

            foreach (KeyValuePair<string, ISubCommand> pair in dictionary)
            {
                if (force || !base.ContainsKey(pair.Key)) /* EXEMPT */
                {
                    base[pair.Key] = pair.Value;
                    count++;
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of the contained sub-command names,
        /// optionally filtered by their command flags and by a string pattern.
        /// </summary>
        /// <param name="hasFlags">
        /// The command flags that a sub-command must have in order to be
        /// included, or <see cref="CommandFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="notHasFlags">
        /// The command flags that a sub-command must not have in order to be
        /// included, or <see cref="CommandFlags.None" /> to skip this filter.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require that all of the <paramref name="hasFlags" /> are
        /// present; otherwise, the presence of any one of them is sufficient.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require that all of the <paramref name="notHasFlags" />
        /// are present in order to exclude a sub-command; otherwise, the
        /// presence of any one of them is sufficient to exclude it.
        /// </param>
        /// <param name="pattern">
        /// The string match pattern used to filter the sub-command names, or
        /// null to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, this list receives the matching sub-command names.  If
        /// this value is null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public ReturnCode ToList(
            CommandFlags hasFlags,
            CommandFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList;

            //
            // NOTE: If no flags were supplied, we do not bother filtering on
            //       them.
            //
            if ((hasFlags == CommandFlags.None) &&
                (notHasFlags == CommandFlags.None))
            {
                inputList = new StringList(this.Keys);
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, ISubCommand> pair in this)
                {
                    ISubCommand subCommand = pair.Value;

                    if (subCommand == null)
                        continue;

                    CommandFlags flags = subCommand.CommandFlags;

                    if (((hasFlags == CommandFlags.None) ||
                            FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                        ((notHasFlags == CommandFlags.None) ||
                            !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
                    {
                        inputList.Add(pair.Key);
                    }
                }
            }

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(
                inputList, list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a string containing the contained sub-command
        /// names, separated by spaces and optionally filtered by a string
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The string match pattern used to filter the sub-command names, or
        /// null to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The space-separated list of matching sub-command names.
        /// </returns>
        public string ToString(
            string pattern, bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method builds a string containing all of the contained
        /// sub-command names, separated by spaces.
        /// </summary>
        /// <returns>
        /// The space-separated list of all contained sub-command names.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
