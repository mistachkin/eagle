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
    /// <summary>
    /// This class represents a dictionary of wrapper objects that, in addition
    /// to the normal key-based lookup, maintains a secondary index keyed by the
    /// unique token associated with each wrapped value.  It also tracks a
    /// version number that is bumped whenever the dictionary is modified.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the keys in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the values in the dictionary.  This type must implement the
    /// <see cref="IWrapperData" /> interface so that its token can be obtained.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("9ac0fef2-369d-415b-8776-64ee78fcf0a6")]
    internal class WrapperDictionary<TKey, TValue> :
#if FAST_DICTIONARY
            FastDictionary<TKey, TValue>,
#else
            Dictionary<TKey, TValue>,
#endif
            IDictionary<TKey, TValue> where TValue : IWrapperData
    {
        #region Private Constants
        /// <summary>
        /// When non-zero, token values equal to zero are permitted to be added
        /// to and removed from the token index; otherwise, zero tokens are
        /// ignored.
        /// </summary>
        private static readonly bool AllowZero = true;

        /// <summary>
        /// The separator placed between elements when building a string
        /// representation of this dictionary.
        /// </summary>
        private static readonly string ElementSeparator = Characters.SpaceString;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The current version number for this dictionary.  This value is
        /// incremented each time the dictionary is modified.
        /// </summary>
        private long version;

        /// <summary>
        /// The secondary index that maps the token of each wrapped value to
        /// that value.
        /// </summary>
        private Dictionary<long, TValue> tokens;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public WrapperDictionary()
            : base()
        {
            BumpVersion();
            tokens = new Dictionary<long, TValue>();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the key-and-value pairs
        /// copied from another dictionary.  When the source is also a wrapper
        /// dictionary, its version and token index are copied as well.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary containing the initial key-and-value pairs to copy
        /// into the newly created dictionary.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary being
        /// constructed.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized data associated with
        /// the dictionary being constructed.
        /// </param>
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
        /// <summary>
        /// This method increments the version number for this dictionary,
        /// indicating that it has been modified.
        /// </summary>
        private void BumpVersion()
        {
            Interlocked.Increment(ref version);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the current version number for this dictionary.
        /// </summary>
        /// <returns>
        /// The current version number for this dictionary.
        /// </returns>
        private long GetVersion()
        {
            return Interlocked.CompareExchange(ref version, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to copy the version number from another
        /// dictionary into this dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose version number should be copied.  This must be
        /// a wrapper dictionary for the copy to take place.
        /// </param>
        /// <returns>
        /// True if the version number was copied; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the version number of another
        /// dictionary matches the version number of this dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose version number should be compared.  This must
        /// be a wrapper dictionary for the comparison to take place.
        /// </param>
        /// <returns>
        /// True if the version numbers match; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to copy the token index from another dictionary
        /// into this dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose token index should be copied.  This must be a
        /// wrapper dictionary for the copy to take place.
        /// </param>
        /// <returns>
        /// True if the token index was copied; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets the value associated with the specified token.
        /// </summary>
        /// <param name="token">
        /// The token of the value to obtain from the token index.
        /// </param>
        /// <returns>
        /// The value associated with the specified token.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a value with the specified token is
        /// present in the token index.
        /// </summary>
        /// <param name="token">
        /// The token to locate in the token index.
        /// </param>
        /// <returns>
        /// True if a value with the specified token is present; otherwise,
        /// false.
        /// </returns>
        public virtual bool ContainsKey(
            long token
            )
        {
            return (tokens != null) ? tokens.ContainsKey(token) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the value associated with the specified
        /// token from the token index.
        /// </summary>
        /// <param name="token">
        /// The token of the value to obtain from the token index.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// token.  Upon failure, receives the default value for the value type.
        /// </param>
        /// <returns>
        /// True if a value with the specified token was found; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method determines the token to use for the specified key.  When
        /// an explicit token is supplied, it is used; otherwise, the token of
        /// the value currently associated with the key is used.
        /// </summary>
        /// <param name="key">
        /// The key whose associated token should be determined.
        /// </param>
        /// <param name="token">
        /// The explicit token to use, or null to use the token of the value
        /// currently associated with the key.
        /// </param>
        /// <returns>
        /// The token to use for the specified key, or zero if no token could be
        /// determined.
        /// </returns>
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

        /// <summary>
        /// This method determines the token to use for the specified value.
        /// When an explicit token is supplied, it is used; otherwise, the token
        /// of the value itself is used.
        /// </summary>
        /// <param name="value">
        /// The value whose token should be determined.
        /// </param>
        /// <param name="token">
        /// The explicit token to use, or null to use the token of the value
        /// itself.
        /// </param>
        /// <returns>
        /// The token to use for the specified value.
        /// </returns>
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
        /// <summary>
        /// Adding a key-and-value pair through this collection interface is not
        /// supported and always throws <see cref="NotSupportedException" />.
        /// </summary>
        /// <param name="item">
        /// The key-and-value pair that would be added.
        /// </param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clearing through this collection interface is not supported and
        /// always throws <see cref="NotSupportedException" />.
        /// </summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removing a key-and-value pair through this collection interface is
        /// not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        /// <param name="item">
        /// The key-and-value pair that would be removed.
        /// </param>
        /// <returns>
        /// This method does not return; it always throws
        /// <see cref="NotSupportedException" />.
        /// </returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<TKey, TValue> Overrides
        /// <summary>
        /// Gets or sets the value associated with the specified key, keeping the
        /// token index synchronized when the value is set.
        /// </summary>
        /// <param name="key">
        /// The key of the value to get or set.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// This method adds the specified key and value to the dictionary,
        /// keeping the token index synchronized.
        /// </summary>
        /// <param name="key">
        /// The key of the value to add.
        /// </param>
        /// <param name="value">
        /// The value to add.
        /// </param>
        void IDictionary<TKey, TValue>.Add(
            TKey key,
            TValue value
            )
        {
            Add(key, value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the value with the specified key from the
        /// dictionary, keeping the token index synchronized.
        /// </summary>
        /// <param name="key">
        /// The key of the value to remove.
        /// </param>
        /// <returns>
        /// True if the value was removed; otherwise, false.
        /// </returns>
        bool IDictionary<TKey, TValue>.Remove(
            TKey key
            )
        {
            return Remove(key, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<TKey, TValue> Overrides
        /// <summary>
        /// Gets or sets the value associated with the specified key, keeping the
        /// token index synchronized when the value is set.
        /// </summary>
        /// <param name="key">
        /// The key of the value to get or set.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// This method adds the specified key and value to the dictionary,
        /// keeping the token index synchronized.
        /// </summary>
        /// <param name="key">
        /// The key of the value to add.
        /// </param>
        /// <param name="value">
        /// The value to add.
        /// </param>
        public virtual new void Add(
            TKey key,
            TValue value
            )
        {
            Add(key, value, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the value with the specified key from the
        /// dictionary, keeping the token index synchronized.
        /// </summary>
        /// <param name="key">
        /// The key of the value to remove.
        /// </param>
        /// <returns>
        /// True if the value was removed; otherwise, false.
        /// </returns>
        public virtual new bool Remove(
            TKey key
            )
        {
            return Remove(key, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<TKey, TValue> Overrides
        /// <summary>
        /// This method removes all keys and values from the dictionary,
        /// including those in the token index.
        /// </summary>
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
        /// <summary>
        /// This method adds the specified key and value to the dictionary,
        /// recording the value in the token index under the specified token.
        /// </summary>
        /// <param name="key">
        /// The key of the value to add.
        /// </param>
        /// <param name="value">
        /// The value to add.
        /// </param>
        /// <param name="token">
        /// The explicit token to use for the value in the token index, or null
        /// to use the token of the value itself.
        /// </param>
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

        /// <summary>
        /// This method removes the value with the specified key from the
        /// dictionary, also removing it from the token index under the
        /// specified token.
        /// </summary>
        /// <param name="key">
        /// The key of the value to remove.
        /// </param>
        /// <param name="token">
        /// The explicit token of the value in the token index, or null to use
        /// the token of the value currently associated with the key.
        /// </param>
        /// <returns>
        /// True if the value was removed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method changes the key associated with a value from an old key
        /// to a new key without altering the token index, since only the name
        /// itself changes during a rename.
        /// </summary>
        /// <param name="oldKey">
        /// The existing key of the value to rename.
        /// </param>
        /// <param name="newKey">
        /// The new key to associate with the value.  This key must not already
        /// be present in the dictionary.
        /// </param>
        /// <returns>
        /// True if the value was renamed; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method copies the key-and-value pairs of this dictionary into
        /// an object dictionary, optionally filtered by a match pattern applied
        /// to the string forms of the keys.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys.  This parameter may be null to
        /// include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="dictionary">
        /// The object dictionary that receives the matching key-and-value
        /// pairs.  When null, a new object dictionary is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method builds a string representation of the keys in this
        /// dictionary, optionally filtered by a match pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys.  This parameter may be null to
        /// include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string representation of the (optionally filtered) keys in this
        /// dictionary.
        /// </returns>
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
        /// <summary>
        /// This method builds a string representation of the keys in this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The string representation of the keys in this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
