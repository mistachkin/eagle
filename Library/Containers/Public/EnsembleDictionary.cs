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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4c29e083-c823-43a1-9fc0-58d6737f3ce2")]
    public sealed class EnsembleDictionary :
            Dictionary<string, ISubCommand>, IDictionary<string, ISubCommand>
    {
        #region Public Constructors
        public EnsembleDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnsembleDictionary(
            IDictionary<string, ISubCommand> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnsembleDictionary(
            IEnumerable<string> collection
            )
            : this()
        {
            foreach (string item in collection)
                this.Add(item, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public EnsembleDictionary(
            IEnumerable<KeyValuePair<string, ISubCommand>> collection
            )
            : this()
        {
            foreach (KeyValuePair<string, ISubCommand> pair in collection)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        void ICollection<KeyValuePair<string, ISubCommand>>.Add(
            KeyValuePair<string, ISubCommand> item
            )
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        void ICollection<KeyValuePair<string, ISubCommand>>.Clear()
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        bool ICollection<KeyValuePair<string, ISubCommand>>.Remove(
            KeyValuePair<string, ISubCommand> item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<string, ISubCommand> Overrides
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

        void IDictionary<string, ISubCommand>.Add(
            string key,
            ISubCommand value
            )
        {
            ClearCachedNames();

            base.Add(key, value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

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

        public new void Add(
            string key,
            ISubCommand value
            )
        {
            ClearCachedNames();

            base.Add(key, value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

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
        public new void Clear()
        {
            ClearCachedNames();

            base.Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
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
        private StringDictionary cachedNames;
        internal StringDictionary CachedNames
        {
            get { return cachedNames; }
            set { cachedNames = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

        public string ToString(
            string pattern, bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
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
