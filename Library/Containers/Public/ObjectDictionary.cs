/*
 * ObjectDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using StringPair = System.Collections.Generic.KeyValuePair<string, string>;
using ObjectPair = System.Collections.Generic.KeyValuePair<string, object>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, object>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, object>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents an ordered, string-keyed dictionary of arbitrary
    /// object values.  It extends the underlying dictionary implementation
    /// (either <c>FastDictionary</c> or the standard generic dictionary,
    /// depending on the build) with support for read-only enforcement, nested
    /// dictionary traversal and creation, interpreter-enforced pair and
    /// nesting limits, and conversion to and from the Eagle string list
    /// representation.  It is the object-valued counterpart to
    /// <see cref="StringDictionary" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2327d197-2cd8-440e-babe-1c9bd85a3cd4")]
    public sealed class ObjectDictionary : SomeDictionary, IReadOnly, IViaScript
    {
        #region Private Data
        //
        // NOTE: When this field is non-zero, the overridden ToString method
        //       will include all the keys and values, not just the keys.
        //
        /// <summary>
        /// When non-zero, the overridden <see cref="ToString()" /> method will
        /// include all the keys and values, not just the keys.
        /// </summary>
        private readonly bool isViaScript = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this field is non-zero, the entire dictionary instance
        //       is read-only and cannot be modified in any way.  Any attempt
        //       to modify read-only dictionary instances will result in an
        //       exception being thrown.
        //
        /// <summary>
        /// When non-zero, the entire dictionary instance is read-only and
        /// cannot be modified in any way.  Any attempt to modify a read-only
        /// dictionary instance will result in an exception being thrown.
        /// </summary>
        private bool isReadOnly;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty dictionary instance.
        /// </summary>
        public ObjectDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty dictionary instance with the specified initial
        /// capacity.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of pairs the dictionary can contain before its
        /// internal storage must be resized.
        /// </param>
        public ObjectDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary instance containing copies of the pairs from
        /// the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose pairs are copied into the new instance.
        /// </param>
        public ObjectDictionary(
            IDictionary<string, object> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty dictionary instance that uses the specified key
        /// equality comparer.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public ObjectDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary instance containing copies of the pairs from
        /// the specified dictionary, using the specified key equality
        /// comparer.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose pairs are copied into the new instance.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer for the key type.
        /// </param>
        public ObjectDictionary(
            IDictionary<string, object> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary instance from the specified collection of
        /// values, using the one-based ordinal position of each value (as a
        /// string) for its key.
        /// </summary>
        /// <param name="collection">
        /// The collection of values to add to the new instance.
        /// </param>
        public ObjectDictionary(
            IEnumerable<object> collection
            )
            : this()
        {
            foreach (object item in collection)
                this.Add((this.Count + 1).ToString(), item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary instance containing copies of the entries
        /// from the specified non-generic dictionary, converting each key to
        /// its string representation.
        /// </summary>
        /// <param name="dictionary">
        /// The non-generic dictionary whose entries are copied into the new
        /// instance.
        /// </param>
        public ObjectDictionary(
            IDictionary dictionary
            )
            : this()
        {
            foreach (DictionaryEntry entry in dictionary)
                this.Add(entry.Key.ToString(), entry.Value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty dictionary instance, recording whether it
        /// originated from a script.
        /// </summary>
        /// <param name="isViaScript">
        /// Non-zero if this dictionary originated from a script; this controls
        /// whether the overridden <see cref="ToString()" /> method includes
        /// values in addition to keys.
        /// </param>
        internal ObjectDictionary(
            bool isViaScript /* in */
            )
            : this()
        {
            this.isViaScript = isViaScript;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary instance containing copies of the pairs from
        /// the specified dictionary, recording whether it originated from a
        /// script.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose pairs are copied into the new instance.
        /// </param>
        /// <param name="isViaScript">
        /// Non-zero if this dictionary originated from a script; this controls
        /// whether the overridden <see cref="ToString()" /> method includes
        /// values in addition to keys.
        /// </param>
        internal ObjectDictionary(
            IDictionary<string, object> dictionary,
            bool isViaScript
            )
            : this(dictionary)
        {
            this.isViaScript = isViaScript;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Wrapper Methods
        /// <summary>
        /// This method adds the value obtained from the specified value
        /// container to the dictionary under the specified key, enforcing the
        /// interpreter dictionary pair limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="getValue">
        /// The value container providing the value to add, or null to add a
        /// null value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// True if the value was added; otherwise, false.
        /// </returns>
        private bool InternalAdd(
            Interpreter interpreter, /* in */
            string key,              /* in */
            IGetValue getValue,      /* in */
            ref Result error         /* out */
            )
        {
            object value = null;

            if (getValue != null)
                value = getValue.Value;

            return InternalAdd(interpreter, key, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified value to the dictionary under the
        /// specified key, enforcing the interpreter dictionary pair limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="value">
        /// The value to add.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// True if the value was added; otherwise, false.
        /// </returns>
        private bool InternalAdd(
            Interpreter interpreter, /* in */
            string key,              /* in */
            object value,            /* in */
            ref Result error         /* out */
            )
        {
            long limit = Limits.Unknown;

            if (WouldExceedPairLimit(interpreter, key, ref limit))
            {
                error = String.Format(
                    "would exceed dictionary pair limit {0}", limit);

                return false;
            }

            base.Add(key, value);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the value obtained from the specified value
        /// container to the dictionary under the specified key, or changes the
        /// existing value if the key is already present, enforcing the
        /// interpreter dictionary pair limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="key">
        /// The key under which the value is added or changed.
        /// </param>
        /// <param name="getValue">
        /// The value container providing the value to add or change, or null to
        /// use a null value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// True if the value was added or changed; otherwise, false.
        /// </returns>
        internal bool InternalAddOrChange(
            Interpreter interpreter, /* in */
            string key,              /* in */
            IGetValue getValue,      /* in */
            ref Result error         /* out */
            )
        {
            object value = null;

            if (getValue != null)
                value = getValue.Value;

            return InternalAddOrChange(
                interpreter, key, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified value to the dictionary under the
        /// specified key, or changes the existing value if the key is already
        /// present, enforcing the interpreter dictionary pair limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="key">
        /// The key under which the value is added or changed.
        /// </param>
        /// <param name="value">
        /// The value to add or change.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// True if the value was added or changed; otherwise, false.
        /// </returns>
        internal bool InternalAddOrChange(
            Interpreter interpreter, /* in */
            string key,              /* in */
            object value,            /* in */
            ref Result error         /* out */
            )
        {
            long limit = Limits.Unknown;

            if (WouldExceedPairLimit(interpreter, key, ref limit))
            {
                error = String.Format(
                    "would exceed dictionary pair limit {0}", limit);

                return false;
            }

            PrivateAddOrChange(key, value);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pair with the specified key from the
        /// dictionary, bypassing the read-only check.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="key">
        /// The key of the pair to remove.
        /// </param>
        /// <returns>
        /// True if the pair was found and removed; otherwise, false.
        /// </returns>
        internal bool InternalRemove(
            Interpreter interpreter, /* in: NOT USED */
            string key               /* in */
            )
        {
            return PrivateRemove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all pairs from the dictionary, bypassing the
        /// read-only check.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.  This parameter is
        /// not used.
        /// </param>
        private void InternalClear(
            Interpreter interpreter /* in: NOT USED */
            )
        {
            PrivateClear();
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_STANDARD_21
        /// <summary>
        /// This method attempts to add the specified value to the dictionary
        /// under the specified key, enforcing the interpreter dictionary pair
        /// limit and failing if the key is already present.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.  This parameter is not used by the underlying
        /// add operation.
        /// </param>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="value">
        /// The value to add.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// True if the value was added; otherwise, false.
        /// </returns>
        private bool InternalTryAdd(
            Interpreter interpreter, /* in: NOT USED */
            string key,              /* in */
            object value,            /* in */
            ref Result error         /* out */
            )
        {
            long limit = Limits.Unknown;

            if (WouldExceedPairLimit(interpreter, key, ref limit))
            {
                error = String.Format(
                    "would exceed dictionary pair limit {0}", limit);

                return false;
            }

            return PrivateTryAdd(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pair with the specified key from the
        /// dictionary, returning its value, bypassing the read-only check.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="key">
        /// The key of the pair to remove.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value that was removed; otherwise,
        /// receives the default value.
        /// </param>
        /// <returns>
        /// True if the pair was found and removed; otherwise, false.
        /// </returns>
        private bool InternalRemove(
            Interpreter interpreter, /* in: NOT USED */
            string key,              /* in */
            out object value         /* in */
            )
        {
            return PrivateRemove(key, out value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified value to the dictionary under the
        /// specified key, bypassing all limit and read-only checks.
        /// </summary>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="value">
        /// The value to add.  This parameter may be null.
        /// </param>
        private void PrivateAdd(
            string key,  /* in */
            object value /* in */
            )
        {
            base.Add(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified value to the dictionary under the
        /// specified key, or changes the existing value if the key is already
        /// present, bypassing all limit and read-only checks.
        /// </summary>
        /// <param name="key">
        /// The key under which the value is added or changed.
        /// </param>
        /// <param name="value">
        /// The value to add or change.  This parameter may be null.
        /// </param>
        private void PrivateAddOrChange(
            string key,  /* in */
            object value /* in */
            )
        {
            base[key] = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pair with the specified key from the
        /// dictionary, bypassing the read-only check.
        /// </summary>
        /// <param name="key">
        /// The key of the pair to remove.
        /// </param>
        /// <returns>
        /// True if the pair was found and removed; otherwise, false.
        /// </returns>
        private bool PrivateRemove(
            string key /* in */
            )
        {
            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all pairs from the dictionary, bypassing the
        /// read-only check.
        /// </summary>
        private void PrivateClear()
        {
            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_STANDARD_21
        /// <summary>
        /// This method attempts to add the specified value to the dictionary
        /// under the specified key, failing if the key is already present,
        /// bypassing all limit and read-only checks.
        /// </summary>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="value">
        /// The value to add.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value was added; otherwise, false.
        /// </returns>
        private bool PrivateTryAdd(
            string key,  /* in */
            object value /* in */
            )
        {
            return base.TryAdd(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pair with the specified key from the
        /// dictionary, returning its value, bypassing the read-only check.
        /// </summary>
        /// <param name="key">
        /// The key of the pair to remove.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value that was removed; otherwise,
        /// receives the default value.
        /// </param>
        /// <returns>
        /// True if the pair was found and removed; otherwise, false.
        /// </returns>
        private bool PrivateRemove(
            string key,      /* in */
            out object value /* in */
            )
        {
            return base.Remove(key, out value);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method throws a <see cref="ScriptException" /> if this
        /// dictionary instance is read-only; otherwise, it does nothing.
        /// </summary>
        private void CheckReadOnly()
        {
            if (isReadOnly)
                throw new ScriptException("dictionary is read-only");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this dictionary instance as read-only, preventing
        /// any further modification.
        /// </summary>
        private void MakeReadOnly()
        {
            isReadOnly = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether adding a pair with the specified key
        /// would exceed the interpreter dictionary pair limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is checked, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="key">
        /// The key to be added.  If this key is already present in the
        /// dictionary, the limit cannot be exceeded.
        /// </param>
        /// <param name="limit">
        /// Receives the dictionary pair limit that would be exceeded, if any.
        /// </param>
        /// <returns>
        /// True if adding the pair would exceed the limit; otherwise, false.
        /// </returns>
        private bool WouldExceedPairLimit(
            Interpreter interpreter, /* in */
            string key,              /* in */
            ref long limit           /* in, out */
            )
        {
            if ((key != null) && this.ContainsKey(key))
                return false;

            if (interpreter == null)
                return false;

            limit = interpreter.InternalDictionaryPairLimit;

            if (limit == Limits.Unlimited)
                return false;

            long count = this.Count + 1; // NOTE: Pre-add.

            if (count < 0) /* IMPOSSIBLE */
                return true;

            return (count > limit);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether traversing the inclusive range of
        /// keys between the specified start and stop indexes would exceed the
        /// interpreter dictionary nesting limit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary nesting limit is checked, or null
        /// to skip the limit check.
        /// </param>
        /// <param name="startIndex">
        /// The starting key index of the range to be traversed.
        /// </param>
        /// <param name="stopIndex">
        /// The ending key index of the range to be traversed.
        /// </param>
        /// <param name="limit">
        /// Receives the dictionary nesting limit that would be exceeded, if
        /// any.
        /// </param>
        /// <returns>
        /// True if traversing the range would exceed the limit; otherwise,
        /// false.
        /// </returns>
        private bool WouldExceedNestLimit(
            Interpreter interpreter, /* in */
            int startIndex,          /* in */
            int stopIndex,           /* in */
            ref long limit           /* in, out */
            )
        {
            if (interpreter == null)
                return false;

            limit = interpreter.InternalDictionaryNestLimit;

            if (limit == Limits.Unlimited)
                return false;

            long count = stopIndex - startIndex + 1;

            if (count < 0) /* IMPOSSIBLE (?) */
                return true;

            return (count > limit);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new dictionary instance from the Eagle string
        /// list representation contained in the specified value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="value">
        /// The Eagle string list representation to parse into pairs.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the value contains only keys, with no associated values.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The new dictionary instance upon success; otherwise, null.
        /// </returns>
        private static ObjectDictionary PrivateFromString(
            Interpreter interpreter, /* in */
            string value,            /* in */
            bool viaScript,          /* in */
            bool addOnly,            /* in */
            bool keysOnly,           /* in */
            ref Result error         /* out */
            )
        {
            StringDictionary dictionary1 = StringDictionary.FromString(
                value, addOnly, keysOnly, ref error);

            if (dictionary1 == null)
                return null;

            ObjectDictionary dictionary2 = new ObjectDictionary(viaScript);

            foreach (StringPair pair in dictionary1)
            {
                if (!dictionary2.InternalAddOrChange(
                        interpreter, pair.Key, pair.Value, ref error))
                {
                    return null;
                }
            }

            return dictionary2;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method obtains a dictionary instance from the specified value
        /// container, reusing or caching the dictionary where possible.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="getValue">
        /// The value container providing the value to convert.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The dictionary instance upon success; otherwise, null.
        /// </returns>
        internal static ObjectDictionary FromValue(
            Interpreter interpreter,
            IGetValue getValue,
            bool viaScript,
            bool addOnly,
            ref Result error
            )
        {
            return FromValue(
                interpreter, getValue, viaScript, addOnly, false,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a dictionary instance from the specified value
        /// container, reusing or caching the dictionary where possible.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="getValue">
        /// The value container providing the value to convert.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the value contains only keys, with no associated values.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The dictionary instance upon success; otherwise, null.
        /// </returns>
        private static ObjectDictionary FromValue(
            Interpreter interpreter,
            IGetValue getValue,
            bool viaScript,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            if (getValue == null)
            {
                error = "expected outer value but got null";
                return null;
            }

            object value = getValue.Value;

            if (value == null)
            {
                error = "expected inner value but got null";
                return null;
            }

            ObjectDictionary dictionary = value as ObjectDictionary;

            if (dictionary != null)
                return dictionary;

            ICacheValue cacheValue = getValue as ICacheValue;

            if (cacheValue != null)
            {
                dictionary = cacheValue.GetCacheValue(
                    interpreter, true) as ObjectDictionary;

                if (dictionary != null)
                    return dictionary;
            }

            dictionary = FromString(
                interpreter, StringOps.GetStringFromObject(
                value), viaScript, addOnly, keysOnly, ref error);

            if (dictionary == null)
                return null;

            dictionary.MakeReadOnly();

            ISetValue setValue = getValue as ISetValue;

            if (setValue != null)
                setValue.Value = dictionary;

            if (cacheValue != null)
            {
                /* IGNORED */
                cacheValue.SetCacheValue(
                    interpreter, dictionary, true);
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a dictionary instance from the string
        /// representation of the specified object value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="value">
        /// The object value whose string representation is parsed into pairs.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The new dictionary instance upon success; otherwise, null.
        /// </returns>
        public static ObjectDictionary FromObject(
            Interpreter interpreter,
            object value,
            bool viaScript,
            bool addOnly,
            ref Result error
            )
        {
            return FromObject(
                interpreter, value, viaScript, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a dictionary instance from the string
        /// representation of the specified object value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="value">
        /// The object value whose string representation is parsed into pairs.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the value contains only keys, with no associated values.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The new dictionary instance upon success; otherwise, null.
        /// </returns>
        public static ObjectDictionary FromObject(
            Interpreter interpreter,
            object value,
            bool viaScript,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            string stringValue;

            if (value is string)
                stringValue = (string)value;
            else
                stringValue = StringOps.GetStringFromObject(value);

            return PrivateFromString(
                interpreter, stringValue, viaScript, addOnly, keysOnly,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new dictionary instance from the Eagle string
        /// list representation contained in the specified value, discarding any
        /// error message.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="value">
        /// The Eagle string list representation to parse into pairs.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <returns>
        /// The new dictionary instance upon success; otherwise, null.
        /// </returns>
        private static ObjectDictionary FromString(
            Interpreter interpreter,
            string value,
            bool viaScript,
            bool addOnly
            )
        {
            Result error = null;

            return FromString(
                interpreter, value, viaScript, addOnly, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new dictionary instance from the Eagle string
        /// list representation contained in the specified value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="value">
        /// The Eagle string list representation to parse into pairs.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The new dictionary instance upon success; otherwise, null.
        /// </returns>
        public static ObjectDictionary FromString(
            Interpreter interpreter,
            string value,
            bool viaScript,
            bool addOnly,
            ref Result error
            )
        {
            return FromString(
                interpreter, value, viaScript, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new dictionary instance from the Eagle string
        /// list representation contained in the specified value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="value">
        /// The Eagle string list representation to parse into pairs.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if the resulting dictionary should be flagged as having
        /// originated from a script.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero to treat duplicate keys as an error; otherwise, later
        /// values for a key replace earlier ones.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the value contains only keys, with no associated values.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The new dictionary instance upon success; otherwise, null.
        /// </returns>
        public static ObjectDictionary FromString(
            Interpreter interpreter,
            string value,
            bool viaScript,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            return PrivateFromString(
                interpreter, value, viaScript, addOnly, keysOnly, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a dictionary instance from previously serialized data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context describing the source and destination of the
        /// serialized data.
        /// </param>
        private ObjectDictionary(
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

        #region Public Methods
        /// <summary>
        /// This method adds all the pairs from the specified dictionary to this
        /// dictionary instance.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose pairs are added to this instance.
        /// </param>
        public void Add(
            IDictionary<string, object> dictionary /* in */
            )
        {
            foreach (ObjectPair pair in dictionary)
                this.Add(pair.Key, pair.Value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified sequence of nested
        /// dictionary keys can be traversed starting from this dictionary
        /// instance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="keys">
        /// The ordered sequence of keys identifying the nested path to
        /// traverse.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if any dictionaries materialized during traversal should be
        /// flagged as having originated from a script.
        /// </param>
        /// <returns>
        /// True if the path can be fully traversed; otherwise, false.
        /// </returns>
        public bool CanTraverse(
            Interpreter interpreter, /* in */
            IEnumerable keys,        /* in */
            bool viaScript           /* in */
            )
        {
            object value = null;
            Result error = null;

            return TryTraverse(
                interpreter, keys, viaScript, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method recursively traverses this dictionary instance and its
        /// nested dictionaries, counting the keys that match the specified
        /// pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used for string matching and nested dictionary
        /// parsing, or null.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match keys, or null to match all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="matchAll">
        /// Non-zero if the pattern should be applied at every nesting level;
        /// otherwise, it is applied only at the top level.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if any dictionaries materialized during traversal should be
        /// flagged as having originated from a script.
        /// </param>
        /// <param name="aggressive">
        /// Non-zero to attempt to parse non-dictionary values into nested
        /// dictionaries during traversal.
        /// </param>
        /// <returns>
        /// The number of matching keys found across all nesting levels.
        /// </returns>
        public long TraverseAndCount(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            bool matchAll,           /* in */
            bool viaScript,          /* in */
            bool aggressive          /* in */
            )
        {
            return TraverseAndCount(
                interpreter, 0, pattern, noCase, matchAll, viaScript,
                aggressive);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method recursively traverses this dictionary instance and its
        /// nested dictionaries, counting the keys that match the specified
        /// pattern, tracking the current nesting level.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used for string matching and nested dictionary
        /// parsing, or null.
        /// </param>
        /// <param name="level">
        /// The current nesting level, where zero indicates the top level.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match keys, or null to match all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="matchAll">
        /// Non-zero if the pattern should be applied at every nesting level;
        /// otherwise, it is applied only at the top level.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if any dictionaries materialized during traversal should be
        /// flagged as having originated from a script.
        /// </param>
        /// <param name="aggressive">
        /// Non-zero to attempt to parse non-dictionary values into nested
        /// dictionaries during traversal.
        /// </param>
        /// <returns>
        /// The number of matching keys found across all nesting levels at or
        /// below the specified level.
        /// </returns>
        private long TraverseAndCount(
            Interpreter interpreter, /* in */
            int level,               /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            bool matchAll,           /* in */
            bool viaScript,          /* in */
            bool aggressive          /* in */
            )
        {
            long count = 0;

            foreach (ObjectPair pair in this)
            {
                if (matchAll || (level == 0))
                {
                    if ((pattern != null) && !Parser.StringMatch(
                            interpreter, pair.Key, 0, pattern, 0, noCase))
                    {
                        continue;
                    }
                }

                count++; // NOTE: Another visited (or matching) key.

                ObjectDictionary dictionary = pair.Value as ObjectDictionary;

                if (dictionary == null)
                {
                    if (aggressive)
                    {
                        dictionary = FromString(
                            interpreter, StringOps.GetStringFromObject(
                            pair.Value), viaScript, false);

                        if (dictionary != null)
                            goto recurse;
                    }

                    continue;
                }

            recurse:

                count += dictionary.TraverseAndCount(
                    interpreter, level + 1, pattern, noCase, matchAll,
                    viaScript, aggressive); /* RECURSIVE */
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traverses the specified range of nested dictionary keys
        /// starting from this dictionary instance, creating intermediate nested
        /// dictionaries as needed, and returns the innermost dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair and nesting limits are
        /// enforced, or null to skip the limit checks.
        /// </param>
        /// <param name="keys">
        /// The ordered sequence of keys identifying the nested path to
        /// traverse and create.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first key in the range to traverse.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last key in the range to traverse, or a negative
        /// value to traverse through the final key.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if any dictionaries materialized during traversal should be
        /// flagged as having originated from a script.
        /// </param>
        /// <param name="changeCount">
        /// Receives the running count of intermediate dictionaries created or
        /// changed during traversal, incremented from its incoming value.
        /// </param>
        /// <param name="stopOnNotFound">
        /// On input, non-zero to stop and return null (without setting an
        /// error) upon encountering a missing key; upon such a stop, this is
        /// set to false so the caller can distinguish that case from an error.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// The innermost traversed (or created) dictionary upon success;
        /// otherwise, null.
        /// </returns>
        public ObjectDictionary TraverseAndCreate(
            Interpreter interpreter, /* in */
            IEnumerable keys,        /* in */
            int startIndex,          /* in */
            int stopIndex,           /* in */
            bool viaScript,          /* in */
            ref int changeCount,     /* in, out */
            ref bool stopOnNotFound, /* in, out */
            ref Result error         /* out */
            )
        {
            if (keys == null)
            {
                error = "invalid dictionary key collection";
                return null;
            }

            StringList localKeys = new StringList(keys);
            int count = localKeys.Count;

            if (count == 0)
            {
                error = "empty dictionary key collection";
                return null;
            }

            if (startIndex >= 0)
            {
                if (startIndex >= count)
                {
                    error = String.Format(
                        "start index {0} must be less than count {1}",
                        startIndex, count);

                    return null;
                }
            }
            else
            {
                error = String.Format(
                    "start index {0} cannot be less than zero",
                    startIndex);

                return null;
            }

            if (stopIndex >= 0)
            {
                if (stopIndex >= count)
                {
                    error = String.Format(
                        "stop index {0} must be less than count {1}",
                        stopIndex, count);

                    return null;
                }
            }
            else
            {
                stopIndex = count - 1;
            }

            if (startIndex > stopIndex)
            {
                error = String.Format(
                    "start index {0} must be less than stop index {1}",
                    startIndex, stopIndex);

                return null;
            }

            long limit = Limits.Unknown;

            if (WouldExceedNestLimit(
                    interpreter, startIndex, stopIndex, ref limit))
            {
                error = String.Format(
                    "would exceed dictionary nesting limit {0}", limit);

                return null;
            }

            ObjectDictionary dictionary = this;

            for (int index = startIndex; index <= stopIndex; index++)
            {
                string localKey = localKeys[index];

                if (localKey == null)
                {
                    error = String.Format(
                        "invalid dictionary key #{0}", index + 1);

                    return null;
                }

                object localValue;
                ObjectDictionary localDictionary;

                if (dictionary.TryGetValue(localKey, out localValue))
                {
                    localDictionary = localValue as ObjectDictionary;

                    if (localDictionary != null)
                    {
                        dictionary = localDictionary;
                    }
                    else
                    {
                        localDictionary = FromString(
                            interpreter, StringOps.GetStringFromObject(
                            localValue), viaScript, false, ref error);

                        if (localDictionary == null)
                            return null;

                        if (!dictionary.InternalAddOrChange(
                                interpreter, localKey, localDictionary,
                                ref error))
                        {
                            return null;
                        }

                        dictionary = localDictionary;

                        changeCount++;
                    }
                }
                else if (stopOnNotFound)
                {
                    //
                    // HACK: This is not an error, per se; therefore,
                    //       do not set an error message.  When the
                    //       caller sets the "stopOnNotFound" flag to
                    //       true, they are also expected to check it
                    //       upon return.  When it is false, they can
                    //       simply skip any subsequent (dictionary)
                    //       processing and return success.
                    //
                    stopOnNotFound = false;
                    return null;
                }
                else
                {
                    localDictionary = new ObjectDictionary(
                        dictionary.IsViaScript);

                    if (!dictionary.InternalAddOrChange(
                            interpreter, localKey, localDictionary,
                            ref error))
                    {
                        return null;
                    }

                    dictionary = localDictionary;

                    changeCount++;
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to traverse the specified sequence of nested
        /// dictionary keys starting from this dictionary instance, returning
        /// the value reached at the end of the path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose dictionary pair limit is enforced, or null to
        /// skip the limit check.
        /// </param>
        /// <param name="keys">
        /// The ordered sequence of keys identifying the nested path to
        /// traverse.
        /// </param>
        /// <param name="viaScript">
        /// Non-zero if any dictionaries materialized during traversal should be
        /// flagged as having originated from a script.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value reached at the end of the path,
        /// which may be this dictionary instance when the key sequence is empty
        /// or ends with a null key.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the reason for
        /// the failure.
        /// </param>
        /// <returns>
        /// True if the path was fully traversed; otherwise, false.
        /// </returns>
        public bool TryTraverse(
            Interpreter interpreter, /* in */
            IEnumerable keys,        /* in */
            bool viaScript,          /* in */
            ref object value,        /* out */
            ref Result error         /* out */
            )
        {
            if (keys == null)
            {
                error = "invalid dictionary key collection";
                return false;
            }

            ObjectDictionary dictionary = this;
            StringList localKeys = new StringList(keys);
            int count = localKeys.Count;

            if (count == 0)
            {
                value = dictionary;
                return true;
            }

            string localKey;
            object localValue;

            for (int index = 0; index < count - 1; index++)
            {
                localKey = localKeys[index];

                if (localKey == null)
                    continue;

                if (!dictionary.TryGetValue(localKey, out localValue))
                {
                    error = String.Format(
                        "cannot find work dictionary key {0}",
                        FormatOps.DisplayTraverseList(
                            localKeys.GetRange(0, index + 1)));

                    return false;
                }

                ObjectDictionary savedDictionary = dictionary;

                dictionary = localValue as ObjectDictionary;

                if (dictionary != null)
                    continue;

                dictionary = FromString(
                    interpreter, StringOps.GetStringFromObject(
                    localValue), viaScript, false, ref error);

                if (dictionary == null)
                    return false;

                if (!savedDictionary.InternalAddOrChange(
                        interpreter, localKey, dictionary,
                        ref error))
                {
                    return false;
                }
            }

            localKey = localKeys[count - 1];

            if (localKey == null)
            {
                value = dictionary;
                return true;
            }

            if (!dictionary.TryGetValue(localKey, out localValue))
            {
                error = String.Format(
                    "cannot find final dictionary key {0}",
                    FormatOps.DisplayTraverseList(
                        localKeys.GetRange(0, count)));

                return false;
            }

            value = localValue;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary keys that match the specified pattern using the specified
        /// match mode.
        /// </summary>
        /// <param name="mode">
        /// The match mode used to compare keys against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match keys, or null to match all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when the match mode is regular
        /// expression based.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching keys.
        /// </returns>
        public string KeysToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a list-formatted string of all the dictionary
        /// keys, joined using the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The string used to separate adjacent keys in the result.
        /// </param>
        /// <returns>
        /// The list-formatted string of all keys.
        /// </returns>
        public string KeysToString(
            string separator
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary keys that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match keys, or null to match all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching keys.
        /// </returns>
        public string KeysToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary keys that match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to match keys, or null to match
        /// all keys.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when matching keys.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching keys.
        /// </returns>
        public string KeysToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary values whose keys match the specified pattern using the
        /// specified match mode.
        /// </summary>
        /// <param name="mode">
        /// The match mode used to compare values against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to match values, or null to match all values.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when the match mode is regular
        /// expression based.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching values.
        /// </returns>
        public string ValuesToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary values that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match values, or null to match all values.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching values.
        /// </returns>
        public string ValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary values that match the specified regular expression
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to match values, or null to
        /// match all values.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when matching values.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching values.
        /// </returns>
        public string ValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary keys and their associated values, for keys that match the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match keys, or null to match all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching keys and their values.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary keys and their associated values, for keys that match the
        /// specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to match keys, or null to match
        /// all keys.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when matching keys.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching keys and their values.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, object>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a space-separated, list-formatted string of the
        /// dictionary keys that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to match keys, or null to match all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list-formatted string of matching keys.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadOnly Members
        /// <summary>
        /// Gets a value indicating whether this dictionary instance is
        /// read-only.  When non-zero, any attempt to modify the dictionary will
        /// result in an exception being thrown.
        /// </summary>
#if FAST_DICTIONARY
        public new bool IsReadOnly
#else
        public bool IsReadOnly
#endif
        {
#if FAST_DICTIONARY
            get { return isReadOnly || base.IsReadOnly; }
#else
            get { return isReadOnly; }
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IViaScript Members
        /// <summary>
        /// Gets a value indicating whether this dictionary instance originated
        /// from a script.  When non-zero, the overridden
        /// <see cref="ToString()" /> method includes values in addition to
        /// keys.
        /// </summary>
        public bool IsViaScript
        {
            get { return isViaScript; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this dictionary
        /// instance.  When the dictionary originated from a script, both keys
        /// and values are included; otherwise, only the keys are included.
        /// </summary>
        /// <returns>
        /// The list-formatted string representation of the dictionary.
        /// </returns>
        public override string ToString()
        {
            return isViaScript ?
                KeysAndValuesToString(null, false) :
                ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<TKey, TValue> Overrides
        /// <summary>
        /// This method adds the specified value to the dictionary under the
        /// specified key, throwing if the dictionary is read-only.  When the
        /// value implements the value container interface, the container itself
        /// is stored rather than its wrapped value.
        /// </summary>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="value">
        /// The value to add.  This parameter may be null.
        /// </param>
        public new void Add(
            string key,
            object value
            )
        {
            CheckReadOnly();

            IGetValue getValue = value as IGetValue;

            if (getValue != null)
                PrivateAdd(key, getValue);
            else
                PrivateAdd(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pair with the specified key from the
        /// dictionary, throwing if the dictionary is read-only.
        /// </summary>
        /// <param name="key">
        /// The key of the pair to remove.
        /// </param>
        /// <returns>
        /// True if the pair was found and removed; otherwise, false.
        /// </returns>
        public new bool Remove(
            string key
            )
        {
            CheckReadOnly();

            return PrivateRemove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all pairs from the dictionary, throwing if the
        /// dictionary is read-only.
        /// </summary>
        public new void Clear()
        {
            CheckReadOnly();

            PrivateClear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the value associated with the specified key.  Setting a
        /// value throws if the dictionary is read-only.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is obtained or modified.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
        public new object this[string key]
        {
            get { return base[key]; }
            set
            {
                CheckReadOnly();

                PrivateAddOrChange(key, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_STANDARD_21
        /// <summary>
        /// This method attempts to add the specified value to the dictionary
        /// under the specified key, failing if the key is already present and
        /// throwing if the dictionary is read-only.
        /// </summary>
        /// <param name="key">
        /// The key under which the value is added.
        /// </param>
        /// <param name="value">
        /// The value to add.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value was added; otherwise, false.
        /// </returns>
        public new bool TryAdd(
            string key,
            object value
            )
        {
            CheckReadOnly();

            return PrivateTryAdd(key, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pair with the specified key from the
        /// dictionary, returning its value, throwing if the dictionary is
        /// read-only.
        /// </summary>
        /// <param name="key">
        /// The key of the pair to remove.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value that was removed; otherwise,
        /// receives the default value.
        /// </param>
        /// <returns>
        /// True if the pair was found and removed; otherwise, false.
        /// </returns>
        public new bool Remove(
            string key,
            out object value
            )
        {
            CheckReadOnly();

            return PrivateRemove(key, out value);
        }
#endif
        #endregion
    }
}
