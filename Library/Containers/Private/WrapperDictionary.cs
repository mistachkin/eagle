/*
 * WrapperDictionary.cs --
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

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("9ac0fef2-369d-415b-8776-64ee78fcf0a6")]
    internal class WrapperDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
            IDictionary<TKey, TValue> where TValue : IWrapperData
    {
        #region Private Constants
        private static readonly bool AllowZero = true;
        private static readonly string ElementSeparator = Characters.Space.ToString();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private long version;
        private Dictionary<long, TValue> tokens;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public WrapperDictionary()
            : base()
        {
            BumpVersion();
            tokens = new Dictionary<long, TValue>();
        }

        ///////////////////////////////////////////////////////////////////////

        public WrapperDictionary(
            IDictionary<TKey, TValue> dictionary
            )
            : base(dictionary)
        {
            if (!TryCopyVersion(dictionary))
                BumpVersion();

            if (!TryCopyTokens(dictionary))
                tokens = new Dictionary<long, TValue>();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected WrapperDictionary(
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

        #region Version Methods
        private void BumpVersion()
        {
            Interlocked.Increment(ref version);
        }

        ///////////////////////////////////////////////////////////////////////

        private long GetVersion()
        {
            return Interlocked.CompareExchange(ref version, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryCopyVersion(
            IDictionary<TKey, TValue> dictionary
            )
        {
            WrapperDictionary<TKey, TValue> wrapperDictionary =
                dictionary as WrapperDictionary<TKey, TValue>;

            if (wrapperDictionary == null)
                return false;

            Interlocked.Exchange(
                ref version, wrapperDictionary.GetVersion());

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryMatchVersion(
            IDictionary<TKey, TValue> dictionary
            )
        {
            WrapperDictionary<TKey, TValue> wrapperDictionary =
                dictionary as WrapperDictionary<TKey, TValue>;

            if (wrapperDictionary == null)
                return false;

            return GetVersion() == wrapperDictionary.GetVersion();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Token Methods
        private bool TryCopyTokens(
            IDictionary<TKey, TValue> dictionary
            )
        {
            WrapperDictionary<TKey, TValue> wrapperDictionary =
                dictionary as WrapperDictionary<TKey, TValue>;

            if (wrapperDictionary == null)
                return false;

            tokens = new Dictionary<long, TValue>(
                wrapperDictionary.tokens);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Token Lookup Members
        public virtual TValue this[long token]
        {
            get
            {
                if (tokens != null)
                    return tokens[token];

                throw new KeyNotFoundException();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ContainsKey(
            long token
            )
        {
            return (tokens != null) ? tokens.ContainsKey(token) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetValue(
            long token,
            out TValue value
            )
        {
            if (tokens != null)
                return tokens.TryGetValue(token, out value);

            value = default(TValue);
            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Helper Methods
        protected virtual long GetKeyToken(
            TKey key,
            long? token
            )
        {
            if (token != null)
                return (long)token;

            TValue value;

            if (base.TryGetValue(key, out value))
                return EntityOps.GetTokenNoThrow(value);

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual long GetValueToken(
            TValue value,
            long? token
            )
        {
            if (token != null)
                return (long)token;

            return EntityOps.GetTokenNoThrow(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<KeyValuePair<TKey, TValue>> Overrides
        void ICollection<KeyValuePair<TKey, TValue>>.Add(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<TKey, TValue> Overrides
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                if (tokens != null)
                {
                    long oldToken = GetKeyToken(key, null);

                    if (AllowZero || (oldToken != 0))
                        tokens.Remove(oldToken);

                    if (value != null)
                    {
                        long newToken = GetValueToken(value, null);

                        if (AllowZero || (newToken != 0))
                            tokens[newToken] = value;
                    }
                }

                BumpVersion();
                base[key] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary<TKey, TValue>.Add(
            TKey key,
            TValue value
            )
        {
            Add(key, value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<TKey, TValue>.Remove(
            TKey key
            )
        {
            return Remove(key, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<TKey, TValue> Overrides
        public virtual new TValue this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                if (tokens != null)
                {
                    long oldToken = GetKeyToken(key, null);

                    if (AllowZero || (oldToken != 0))
                        tokens.Remove(oldToken);

                    if (value != null)
                    {
                        long newToken = GetValueToken(value, null);

                        if (AllowZero || (newToken != 0))
                            tokens[newToken] = value;
                    }
                }

                BumpVersion();
                base[key] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual new void Add(
            TKey key,
            TValue value
            )
        {
            Add(key, value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual new bool Remove(
            TKey key
            )
        {
            return Remove(key, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<TKey, TValue> Overrides
        public virtual new void Clear()
        {
            if (tokens != null)
                tokens.Clear();

            BumpVersion();
            base.Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual void Add(
            TKey key,
            TValue value,
            long? token
            )
        {
            if ((tokens != null) && (value != null))
            {
                long newToken = GetValueToken(value, token);

                if (AllowZero || (newToken != 0))
                    tokens[newToken] = value;
            }

            BumpVersion();
            base.Add(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Remove(
            TKey key,
            long? token
            )
        {
            if (tokens != null)
            {
                long newToken = GetKeyToken(key, token);

                if (AllowZero || (newToken != 0))
                    /* IGNORED */
                    tokens.Remove(newToken);
            }

            BumpVersion();
            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method can be used to avoid messing with the tokens
        //       during a rename operation (i.e. since only the name itself
        //       should change).
        //
        public virtual bool Rename(
            TKey oldKey,
            TKey newKey
            )
        {
            TValue value;

            if ((newKey != null) && !base.ContainsKey(newKey) &&
                (oldKey != null) && base.TryGetValue(oldKey, out value))
            {
                BumpVersion();
                base.Add(newKey, value);

                BumpVersion();
                return base.Remove(oldKey);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode ToDictionary(
            string pattern,
            bool noCase,
            ref ObjectDictionary dictionary,
            ref Result error
            )
        {
            if (dictionary == null)
                dictionary = new ObjectDictionary();

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                string key = StringOps.GetStringFromObject(pair.Key);

                if ((pattern == null) ||
                    StringOps.Match(null, MatchMode.Glob, key, pattern, noCase))
                {
                    dictionary[key] = pair.Value; /* MERGE */
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public virtual string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                ElementSeparator, pattern, noCase);
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
