/*
 * StringDictionary.cs --
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
using System.Collections.Specialized;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, string>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, string>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to string
    /// values.  It extends the underlying generic dictionary of strings with
    /// helpers for building from and converting to other representations (for
    /// example, flat lists, name/value collections, and pair lists), as well
    /// as for producing filtered string forms of its keys and values.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("28a6f55d-49b5-4f54-bd9b-dba76e1f3016")]
    public sealed class StringDictionary : SomeDictionary
    {
        #region Private Static Data
        /// <summary>
        /// The most recently issued auto-generated key value.  It is
        /// incremented atomically by <see cref="NextId" /> to produce unique
        /// keys for values added without an explicit key.
        /// </summary>
        private static long nextId = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty string dictionary.
        /// </summary>
        public StringDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string dictionary that is initialized with the entries
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public StringDictionary(
            IDictionary<string, string> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string dictionary that is initialized with the entries
        /// copied from the specified non-generic dictionary.  Keys and values
        /// are converted to their string forms.
        /// </summary>
        /// <param name="dictionary">
        /// The non-generic dictionary whose entries are copied into the new
        /// dictionary.  This parameter may be null.
        /// </param>
        public StringDictionary(
            IDictionary dictionary
            )
        {
            Add(dictionary, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string dictionary that is initialized from the
        /// specified list, optionally treating its elements as keys and/or
        /// values.
        /// </summary>
        /// <param name="list">
        /// The list of strings used to populate the new dictionary.
        /// </param>
        /// <param name="keys">
        /// Non-zero if the list elements should be used as keys.  When
        /// <paramref name="values" /> is also non-zero, the list is treated as
        /// a flat sequence of alternating keys and values.
        /// </param>
        /// <param name="values">
        /// Non-zero if the list elements should be used as values.  When
        /// <paramref name="keys" /> is non-zero, the list is treated as a flat
        /// sequence of alternating keys and values; otherwise, each value is
        /// added under an auto-generated unique key.
        /// </param>
        public StringDictionary(
            IList<string> list,
            bool keys,
            bool values
            )
        {
            if (keys)
            {
                if (values)
                    AddKeysAndValues(list, 0);
                else
                    AddKeys(list, 0);
            }
            else if (values)
            {
                AddValues(list, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string dictionary that is initialized from the
        /// specified collection, optionally treating its elements as keys
        /// and/or values.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings used to populate the new dictionary.
        /// </param>
        /// <param name="keys">
        /// Non-zero if the collection elements should be used as keys.  When
        /// <paramref name="values" /> is also non-zero, the collection is
        /// treated as a sequence of alternating keys and values.
        /// </param>
        /// <param name="values">
        /// Non-zero if the collection elements should be used as values.  When
        /// <paramref name="keys" /> is non-zero, the collection is treated as a
        /// sequence of alternating keys and values; otherwise, each value is
        /// added under an auto-generated unique key.
        /// </param>
        public StringDictionary(
            IEnumerable<string> collection,
            bool keys,
            bool values
            )
        {
            if (keys)
            {
                if (values)
                    AddKeysAndValues(collection);
                else
                    AddKeys(collection);
            }
            else if (values)
            {
                AddValues(collection);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string dictionary that is initialized from the
        /// specified collection of string pairs, using the first element of
        /// each pair as the key and the second as the value.
        /// </summary>
        /// <param name="collection">
        /// The collection of string pairs used to populate the new dictionary.
        /// </param>
        public StringDictionary(
            IEnumerable<IPair<string>> collection
            )
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a string dictionary from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private StringDictionary(
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

        #region Private Constructors
        /// <summary>
        /// Constructs an empty string dictionary that is sized to hold the
        /// specified number of entries without further growth.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries the dictionary can contain.
        /// </param>
        internal StringDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Creates a new string dictionary from the string form of a list of
        /// name/value pairs.
        /// </summary>
        /// <param name="value">
        /// The string containing a list of name/value pairs to parse.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero if a duplicate key should be treated as an error instead of
        /// being overwritten.
        /// </param>
        /// <returns>
        /// The newly created string dictionary, or null if the string could
        /// not be parsed.
        /// </returns>
        public static StringDictionary FromString(
            string value,
            bool addOnly
            )
        {
            Result error = null;

            return FromString(value, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string dictionary from the string form of a list of
        /// name/value pairs.
        /// </summary>
        /// <param name="value">
        /// The string containing a list of name/value pairs to parse.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero if a duplicate key should be treated as an error instead of
        /// being overwritten.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The newly created string dictionary, or null if the string could
        /// not be parsed.
        /// </returns>
        public static StringDictionary FromString(
            string value,
            bool addOnly,
            ref Result error
            )
        {
            return FromString(value, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string dictionary from the string form of a list,
        /// optionally treating it as a list of keys only.
        /// </summary>
        /// <param name="value">
        /// The string containing the list to parse.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero if a duplicate key should be treated as an error instead of
        /// being overwritten.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the list elements are keys only (each stored with a null
        /// value); otherwise, the list is treated as alternating name/value
        /// pairs.
        /// </param>
        /// <returns>
        /// The newly created string dictionary, or null if the string could
        /// not be parsed.
        /// </returns>
        public static StringDictionary FromString(
            string value,
            bool addOnly,
            bool keysOnly
            )
        {
            Result error = null;

            return FromString(value, addOnly, keysOnly, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string dictionary from the string form of a list,
        /// optionally treating it as a list of keys only.
        /// </summary>
        /// <param name="value">
        /// The string containing the list to parse.
        /// </param>
        /// <param name="addOnly">
        /// Non-zero if a duplicate key should be treated as an error instead of
        /// being overwritten.
        /// </param>
        /// <param name="keysOnly">
        /// Non-zero if the list elements are keys only (each stored with a null
        /// value); otherwise, the list is treated as alternating name/value
        /// pairs.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The newly created string dictionary, or null if the string could
        /// not be parsed.
        /// </returns>
        public static StringDictionary FromString(
            string value,
            bool addOnly,
            bool keysOnly,
            ref Result error
            )
        {
            StringList list = StringList.FromString(value, ref error);

            if (list == null)
                return null;

            int count = list.Count;

            if (!keysOnly && ((count % 2) != 0))
            {
                error = String.Format(
                    "list of name/value pairs must have an " +
                    "even number of elements, has {0}", count);

                return null;
            }

            StringDictionary dictionary = new StringDictionary();
            int increment = keysOnly ? 1 : 2;

            for (int index = 0; index < count; index += increment)
            {
                string key = list[index];

                if (key == null)
                {
                    error = String.Format(
                        "key at index {0} cannot be null",
                        index);

                    return null;
                }

                if (addOnly && dictionary.ContainsKey(key))
                {
                    error = String.Format(
                        "key {0} at index {1} already exists",
                        FormatOps.WrapOrNull(key), index);

                    return null;
                }

                dictionary[key] = keysOnly ? null : list[index + 1];
            }

            return dictionary;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method atomically increments and returns the next available
        /// auto-generated identifier value.
        /// </summary>
        /// <returns>
        /// The next available identifier value.
        /// </returns>
        private static long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a unique key suitable for storing a value that
        /// was supplied without an explicit key.
        /// </summary>
        /// <returns>
        /// The string form of a newly generated unique key.
        /// </returns>
        private static string GetUniqueKey()
        {
            return NextId().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<string, string> Overrides
        /// <summary>
        /// This method removes all entries from the dictionary and resets the
        /// auto-generated key counter.
        /// </summary>
        public new void Clear()
        {
            base.Clear();

            /* IGNORED */
            Interlocked.Exchange(ref nextId, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the dictionary into an equivalent name/value
        /// collection.
        /// </summary>
        /// <returns>
        /// A new name/value collection containing all of the key/value pairs
        /// from this dictionary.
        /// </returns>
        public NameValueCollection ToNameValueCollection()
        {
            NameValueCollection collection = new NameValueCollection();

            foreach (KeyValuePair<string, string> pair in this)
                collection.Add(pair.Key, pair.Value);

            return collection;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds an entry to the dictionary, converting the
        /// specified key and value objects to their string forms.
        /// </summary>
        /// <param name="key">
        /// The object whose string form is used as the key.
        /// </param>
        /// <param name="value">
        /// The object whose string form is used as the value.
        /// </param>
        public void AddFrom(
            object key,
            object value
            )
        {
            base.Add(
                StringOps.GetStringFromObject(key),
                StringOps.GetStringFromObject(value));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified list, starting at the
        /// given index, as a key with a null value.  Null elements are skipped.
        /// </summary>
        /// <param name="list">
        /// The list of keys to add to the dictionary.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first list element to add.
        /// </param>
        public void AddKeys(
            IList<string> list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                string key = list[index];

                if (key != null)
                    this.Add(key, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified list, starting at the
        /// given index, as a value stored under an auto-generated unique key.
        /// </summary>
        /// <param name="list">
        /// The list of values to add to the dictionary.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first list element to add.
        /// </param>
        public void AddValues(
            IList<string> list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
                this.Add(GetUniqueKey(), list[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the elements of the specified list, starting at the
        /// given index, as alternating key/value pairs.  Existing keys are not
        /// overwritten.
        /// </summary>
        /// <param name="list">
        /// The list of alternating keys and values to add to the dictionary.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first list element (a key) to add.
        /// </param>
        public void AddKeysAndValues(
            IList<string> list,
            int startIndex
            )
        {
            AddKeysAndValues(list, startIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the elements of the specified list, starting at the
        /// given index, as alternating key/value pairs.
        /// </summary>
        /// <param name="list">
        /// The list of alternating keys and values to add to the dictionary.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first list element (a key) to add.
        /// </param>
        /// <param name="merge">
        /// Non-zero if values for keys that already exist should be overwritten;
        /// otherwise, existing keys are left unchanged.
        /// </param>
        public void AddKeysAndValues(
            IList<string> list,
            int startIndex,
            bool merge
            )
        {
            for (int index = startIndex; index < list.Count; index += 2)
            {
                string key = list[index];

                if (key != null)
                {
                    string value = null;

                    if ((index + 1) < list.Count)
                        value = list[index + 1];

                    if (merge || !this.ContainsKey(key))
                        this[key] = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the key/value pairs from the specified dictionary
        /// to this dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are added to this dictionary.
        /// </param>
        /// <param name="merge">
        /// Non-zero if values for keys that already exist should be overwritten;
        /// otherwise, existing keys are left unchanged.
        /// </param>
        public void AddKeysAndValues(
            IDictionary<string, string> dictionary,
            bool merge
            )
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                string key = pair.Key;

                if (merge || !this.ContainsKey(key))
                    this[key] = pair.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified collection as a key
        /// with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of keys to add to the dictionary.
        /// </param>
        public void AddKeys(
            IEnumerable<string> collection
            )
        {
            AddKeys(collection, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified non-generic
        /// collection as a key with a null value.  Elements are converted to
        /// their string forms.
        /// </summary>
        /// <param name="collection">
        /// The collection of keys to add to the dictionary.
        /// </param>
        public void AddKeys(
            IEnumerable collection
            )
        {
            AddKeys(collection, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified collection as a key,
        /// associating each with the specified value.
        /// </summary>
        /// <param name="collection">
        /// The collection of keys to add to the dictionary.
        /// </param>
        /// <param name="value">
        /// The value to associate with each added key.
        /// </param>
        public void AddKeys(
            IEnumerable<string> collection,
            string value
            )
        {
            foreach (string item in collection)
                this.Add(item, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified non-generic
        /// collection as a key, associating each with the specified value.
        /// Elements are converted to their string forms.
        /// </summary>
        /// <param name="collection">
        /// The collection of keys to add to the dictionary.
        /// </param>
        /// <param name="value">
        /// The value to associate with each added key.
        /// </param>
        public void AddKeys(
            IEnumerable collection,
            string value
            )
        {
            foreach (object item in collection)
                this.Add(StringOps.GetStringFromObject(item), value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each element of the specified collection as a value
        /// stored under an auto-generated unique key.
        /// </summary>
        /// <param name="collection">
        /// The collection of values to add to the dictionary.
        /// </param>
        public void AddValues(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                this.Add(GetUniqueKey(), item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the elements of the specified collection as
        /// alternating key/value pairs.  When the collection contains an odd
        /// number of elements, the final key is added with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of alternating keys and values to add to the
        /// dictionary.
        /// </param>
        public void AddKeysAndValues(
            IEnumerable<string> collection
            )
        {
            IEnumerator<string> enumerator = collection.GetEnumerator();

            while (enumerator.MoveNext())
            {
                bool done = false;
                string key = enumerator.Current;
                string value = null;

                if (enumerator.MoveNext())
                    value = enumerator.Current;
                else
                    done = true;

                this.Add(key, value);

                if (done)
                    break;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the entries from the specified non-generic
        /// dictionary to this dictionary.  Keys and values are converted to
        /// their string forms.
        /// </summary>
        /// <param name="dictionary">
        /// The non-generic dictionary whose entries are added to this
        /// dictionary.
        /// </param>
        public void Add(
            IDictionary dictionary
            )
        {
            Add(dictionary, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the entries from the specified non-generic
        /// dictionary to this dictionary.  Keys and values are converted to
        /// their string forms.
        /// </summary>
        /// <param name="dictionary">
        /// The non-generic dictionary whose entries are added to this
        /// dictionary.  This parameter may be null.
        /// </param>
        /// <param name="strict">
        /// Non-zero if a null <paramref name="dictionary" /> should cause an
        /// exception to be thrown; otherwise, a null dictionary is ignored.
        /// </param>
        public void Add(
            IDictionary dictionary,
            bool strict
            )
        {
            if (dictionary == null)
            {
                if (strict)
                    throw new ArgumentNullException("dictionary");

                return;
            }

            foreach (DictionaryEntry entry in dictionary)
            {
                object key = entry.Key;

                if (key == null)
                    throw new NotSupportedException();

                object value = entry.Value;

                this.Add(key.ToString(),
                    (value != null) ? value.ToString() : null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the entries from the specified collection of string
        /// pairs to this dictionary, using the first element of each pair as the
        /// key and the second as the value.
        /// </summary>
        /// <param name="collection">
        /// The collection of string pairs whose entries are added to this
        /// dictionary.
        /// </param>
        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
            foreach (IPair<string> item in collection)
                this.Add(item.X, item.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern, using the specified matching mode.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used to compare each key against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is a
        /// regular expression mode.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
        /// </returns>
        public string KeysToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, string>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing all of the keys of the
        /// dictionary, joined using the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The string used to separate adjacent keys in the result.
        /// </param>
        /// <returns>
        /// The keys of the dictionary formatted as a string.
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
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
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
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which keys are
        /// included.  This parameter may be null to include all keys.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching keys.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
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
        /// This method produces a string containing the values of the
        /// dictionary whose values match the specified pattern, using the
        /// specified matching mode.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used to compare each value against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which values are included.  This parameter
        /// may be null to include all values.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is a
        /// regular expression mode.
        /// </param>
        /// <returns>
        /// The list of matching values formatted as a string.
        /// </returns>
        public string ValuesToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, string>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the values of the
        /// dictionary that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which values are included.  This parameter
        /// may be null to include all values.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching values formatted as a string.
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
        /// This method produces a string containing the values of the
        /// dictionary that match the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which values are
        /// included.  This parameter may be null to include all values.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching values.
        /// </param>
        /// <returns>
        /// The list of matching values formatted as a string.
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
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys (and their values) are
        /// included.  This parameter may be null to include all entries.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, string>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified regular expression
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to select which keys (and their
        /// values) are included.  This parameter may be null to include all
        /// entries.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching keys.
        /// </param>
        /// <returns>
        /// The list of matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, string>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the dictionary into a list of string pairs.
        /// </summary>
        /// <returns>
        /// A new list of string pairs containing all of the key/value pairs
        /// from this dictionary.
        /// </returns>
        public StringPairList ToPairs()
        {
            return ToPairs(null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the entries of the dictionary whose keys match
        /// the specified pattern into a list of string pairs.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys (and their values) are
        /// included.  This parameter may be null to include all entries.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A new list of string pairs containing the matching key/value pairs
        /// from this dictionary.
        /// </returns>
        public StringPairList ToPairs(
            string pattern,
            bool noCase
            )
        {
            StringPairList list = new StringPairList();

            foreach (KeyValuePair<string, string> pair in this)
            {
                if ((pattern == null) ||
                    Parser.StringMatch(null, pair.Key, 0, pattern, 0, noCase))
                {
                    list.Add(pair.Key, pair.Value);
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of the dictionary,
        /// consisting of all of its keys formatted as a string.
        /// </summary>
        /// <returns>
        /// The keys of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
