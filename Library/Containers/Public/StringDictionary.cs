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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("28a6f55d-49b5-4f54-bd9b-dba76e1f3016")]
    public sealed class StringDictionary : Dictionary<string, string>
    {
        #region Private Static Data
        private static long nextId = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringDictionary(
            IDictionary<string, string> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringDictionary(
            IDictionary dictionary
            )
        {
            Add(dictionary, false);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static StringDictionary FromString(
            string value,
            bool addOnly
            )
        {
            Result error = null;

            return FromString(value, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringDictionary FromString(
            string value,
            bool addOnly,
            ref Result error
            )
        {
            return FromString(value, addOnly, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetUniqueKey()
        {
            return NextId().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<string, string> Overrides
        public new void Clear()
        {
            base.Clear();

            /* IGNORED */
            Interlocked.Exchange(ref nextId, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public NameValueCollection ToNameValueCollection()
        {
            NameValueCollection collection = new NameValueCollection();

            foreach (KeyValuePair<string, string> pair in this)
                collection.Add(pair.Key, pair.Value);

            return collection;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void AddValues(
            IList<string> list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
                this.Add(GetUniqueKey(), list[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddKeysAndValues(
            IList<string> list,
            int startIndex
            )
        {
            AddKeysAndValues(list, startIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void AddKeys(
            IEnumerable<string> collection
            )
        {
            AddKeys(collection, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddKeys(
            IEnumerable collection
            )
        {
            AddKeys(collection, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddKeys(
            IEnumerable<string> collection,
            string value
            )
        {
            foreach (string item in collection)
                this.Add(item, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddKeys(
            IEnumerable collection,
            string value
            )
        {
            foreach (object item in collection)
                this.Add(StringOps.GetStringFromObject(item), value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddValues(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                this.Add(GetUniqueKey(), item);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void Add(
            IDictionary dictionary
            )
        {
            Add(dictionary, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
            foreach (IPair<string> item in collection)
                this.Add(item.X, item.Y);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public string KeysToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

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
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList ToPairs()
        {
            return ToPairs(null, false);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
