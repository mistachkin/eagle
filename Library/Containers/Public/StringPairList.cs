/*
 * StringPairList.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a mutable, ordered list of string pairs, where
    /// each element is an <see cref="IPair{T}" /> of strings (a name/value or
    /// key/value pair).  It derives from the generic list of string pairs and
    /// additionally behaves like a string list (via <see cref="IStringList" />),
    /// a single flattened value (via <see cref="IGetValue" />), and a simple
    /// name-keyed dictionary (via <see cref="IHaveDictionary{T}" />).  It
    /// provides numerous helpers for adding strings, string builders, pairs,
    /// and other collections, as well as for rendering the list to its string
    /// or raw-string form and for parsing a list back from a string.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("fa3c3c95-bcc7-4c71-ab7f-94e534f9aad2")]
    public sealed class StringPairList : List<IPair<string>>,
            IStringList, IGetValue, IHaveDictionary<string>
    {
        #region Private Constants
        /// <summary>
        /// The default separator string used between elements when rendering
        /// the list to its string form and no explicit separator is set.
        /// </summary>
        private static readonly string DefaultSeparator =
            Characters.SpaceString;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The default value indicating whether empty elements are included
        /// when rendering the list to its string form or filtering it to a new
        /// list.
        /// </summary>
        public static readonly bool DefaultEmpty = true;

        /// <summary>
        /// The sentinel string returned to indicate that a requested named
        /// value was not found in the list.
        /// </summary>
        public static readonly string NotFound = String.Copy(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty list of string pairs.
        /// </summary>
        public StringPairList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs containing the elements copied
        /// from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of string pairs whose elements are copied into the
        /// new list.
        /// </param>
        public StringPairList(
            IEnumerable<IPair<string>> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty list of string pairs that has the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements the new list can initially store without
        /// resizing.
        /// </param>
        public StringPairList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs containing the specified string
        /// pairs.
        /// </summary>
        /// <param name="pairs">
        /// The string pairs used to populate the new list.
        /// </param>
        public StringPairList(
            params IPair<string>[] pairs
            )
            : base(pairs)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs from the elements of the specified
        /// string array, starting at the specified index.
        /// </summary>
        /// <param name="array">
        /// The array of strings used to populate the new list.
        /// </param>
        /// <param name="startIndex">
        /// The index within the array at which to begin adding strings.
        /// </param>
        public StringPairList(
            string[] array,
            int startIndex
            )
        {
            Add(array, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs from the specified strings.
        /// </summary>
        /// <param name="strings">
        /// The strings used to populate the new list.
        /// </param>
        public StringPairList(
            params string[] strings
            )
        {
            Add(strings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs from the strings in the specified
        /// collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings used to populate the new list.
        /// </param>
        public StringPairList(
            IEnumerable<string> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs from the string builders in the
        /// specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of string builders used to populate the new list.
        /// </param>
        public StringPairList(
            IEnumerable<StringBuilder> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of string pairs from the key/value pairs of the
        /// specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are used to populate the new
        /// list.
        /// </param>
        public StringPairList(
            IDictionary<string, string> dictionary
            )
        {
            Add(dictionary);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveDictionary<String> Members
        /// <summary>
        /// Gets the value (the Y component) of the first pair whose name (the X
        /// component) matches the specified name, using an ordinal comparison.
        /// </summary>
        /// <param name="name">
        /// The name to search for.  If this parameter is null, the value of the
        /// first pair is returned.
        /// </param>
        /// <returns>
        /// The value of the first matching pair, or <see cref="NotFound" /> if
        /// no matching pair exists.
        /// </returns>
        public string GetNamedValue(
            string name
            )
        {
            int count = base.Count;

            for (int index = 0; index < count; index++)
            {
                IPair<string> pair = base[index];

                if ((name == null) || SharedStringOps.Equals(
                        pair.X, name, StringComparison.Ordinal))
                {
                    return pair.Y;
                }
            }

            return NotFound;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the value (the Y component) of the first pair whose name (the X
        /// component) matches the specified name, using an ordinal comparison.
        /// If no matching pair exists, a new pair is appended to the list.
        /// </summary>
        /// <param name="name">
        /// The name to search for and, when adding, the name of the new pair.
        /// If this parameter is null, the first pair is updated.
        /// </param>
        /// <param name="value">
        /// The value to store in the matching or newly added pair.
        /// </param>
        public void SetNamedValue(
            string name,
            string value
            )
        {
            int count = base.Count;

            for (int index = 0; index < count; index++)
            {
                IPair<string> pair = base[index];

                if ((name == null) || SharedStringOps.Equals(
                        pair.X, name, StringComparison.Ordinal))
                {
                    base[index] = new Pair<string>(name, value);
                    return;
                }
            }

            base.Add(new Pair<string>(name, value));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue Members
        //
        // NOTE: This must call ToString to provide a "flattened" value
        //       because this is a mutable class.
        //
        /// <summary>
        /// Gets the flattened value of this list, which is its string form.
        /// </summary>
        public object Value
        {
            get { return ToString(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in characters, of the string form of this list, or
        /// an invalid length when the string form is null.
        /// </summary>
        public int Length
        {
            get
            {
                string stringValue = ToString();

                return (stringValue != null) ?
                    stringValue.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the string form of this list.
        /// </summary>
        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a new list of string pairs that is a shallow copy of this
        /// list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new StringPairList(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStringList Members
        #region Properties
        /// <summary>
        /// The separator string placed between elements when rendering this
        /// list to its string form.
        /// </summary>
        private string separator;

        /// <summary>
        /// Gets or sets the separator string placed between elements when
        /// rendering this list to its string form.
        /// </summary>
        public string Separator
        {
            get { return separator; }
            set { separator = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE
        /// <summary>
        /// The cache key associated with this list, used by the list cache.
        /// </summary>
        private string cacheKey;

        /// <summary>
        /// Gets or sets the cache key associated with this list, used by the
        /// list cache.
        /// </summary>
        public string CacheKey
        {
            get { return cacheKey; }
            set { cacheKey = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Contains Methods
        /// <summary>
        /// Determines whether this list contains a pair whose name (the X
        /// component) matches the specified key, using the specified string
        /// comparison.
        /// </summary>
        /// <param name="key">
        /// The key to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison used to compare each pair name with the
        /// specified key.
        /// </param>
        /// <returns>
        /// True if a matching pair is found; otherwise, false.
        /// </returns>
        public bool ContainsKey(
            string key,
            StringComparison comparisonType
            )
        {
            int count = base.Count;

            if (count == 0)
                return false;

            for (int index = 0; index < count; index++)
            {
                IPair<string> pair = base[index];

                if (pair == null)
                    continue;

                if (SharedStringOps.Equals(
                        pair.X, key, comparisonType))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Get Methods
        /// <summary>
        /// Gets the string form of the pair at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the pair to retrieve.
        /// </param>
        /// <returns>
        /// The string form of the pair at the specified index.
        /// </returns>
        public string GetItem(
            int index
            )
        {
            return this[index].ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the pair at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the pair to retrieve.
        /// </param>
        /// <returns>
        /// The pair at the specified index.
        /// </returns>
        public IPair<string> GetPair(
            int index
            )
        {
            return this[index];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Insert / Add Methods
        /// <summary>
        /// Inserts the specified string into this list at the specified index,
        /// wrapping it in a string pair.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the string should be inserted.
        /// </param>
        /// <param name="item">
        /// The string to insert.  If this parameter is null, a null pair is
        /// inserted.
        /// </param>
        public void Insert(
            int index,
            string item
            )
        {
            if (item != null)
                base.Insert(index, new StringPair(item));
            else
                base.Insert(index, (IPair<string>)null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified string to the end of this list, wrapping it in a
        /// string pair.
        /// </summary>
        /// <param name="item">
        /// The string to add.  If this parameter is null, a null pair is added.
        /// </param>
        public void Add(
            string item
            )
        {
            if (item != null)
                base.Add(new StringPair(item));
            else
                base.Add((IPair<string>)null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a new string pair, with the specified key and value, to the end
        /// of this list.
        /// </summary>
        /// <param name="key">
        /// The key (the X component) of the new pair.
        /// </param>
        /// <param name="value">
        /// The value (the Y component) of the new pair.
        /// </param>
        public void Add(
            string key,
            string value
            )
        {
            base.Add(new StringPair(key, value));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a new string pair, with the specified key and value, to the end
        /// of this list, optionally normalizing the white-space in the value
        /// and/or truncating it with an ellipsis.
        /// </summary>
        /// <param name="key">
        /// The key (the X component) of the new pair.
        /// </param>
        /// <param name="value">
        /// The value (the Y component) of the new pair.
        /// </param>
        /// <param name="normalize">
        /// Non-zero to normalize the white-space within the value before it is
        /// added.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the value with an ellipsis before it is added.
        /// </param>
        public void Add(
            string key,
            string value,
            bool normalize,
            bool ellipsis
            )
        {
            string localValue = value;

            if (normalize)
            {
                localValue = StringOps.NormalizeWhiteSpace(
                    localValue, Characters.Space,
                    WhiteSpaceFlags.FormattedUse);
            }

            if (ellipsis)
                localValue = FormatOps.Ellipsis(localValue);

            base.Add(new StringPair(key, localValue));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a new string pair, with the specified key and a value formed by
        /// joining the specified collection of strings into a list, to the end
        /// of this list.
        /// </summary>
        /// <param name="key">
        /// The key (the X component) of the new pair.
        /// </param>
        /// <param name="value">
        /// The collection of strings used to form the value (the Y component)
        /// of the new pair.  If this parameter is null, the value is null.
        /// </param>
        public void Add(
            string key,
            IEnumerable<string> value
            )
        {
            base.Add(new StringPair(key,
                (value != null) ? StringList.MakeList(value) : null));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of the specified string builder to the end of
        /// this list.
        /// </summary>
        /// <param name="item">
        /// The string builder whose string form is added.  If this parameter is
        /// null, a null pair is added.
        /// </param>
        public void Add(
            StringBuilder item
            )
        {
            this.Add((item != null) ? item.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified string array, starting at the
        /// specified index, to the end of this list.
        /// </summary>
        /// <param name="array">
        /// The array of strings whose elements are added.
        /// </param>
        /// <param name="startIndex">
        /// The index within the array at which to begin adding strings.
        /// </param>
        public void Add(
            string[] array,
            int startIndex
            )
        {
            for (int index = startIndex; index < array.Length; index++)
                Add(array[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of each element of the specified list, starting
        /// at the specified index, to the end of this list.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are added.
        /// </param>
        /// <param name="startIndex">
        /// The index within the list at which to begin adding elements.
        /// </param>
        public void Add(
            IList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                object item = list[index];

                if (item != null)
                    Add(item.ToString());
                else
                    Add((string)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the pairs of the specified string list, starting at the
        /// specified index, to the end of this list.
        /// </summary>
        /// <param name="list">
        /// The string list whose pairs are added.
        /// </param>
        /// <param name="startIndex">
        /// The index within the list at which to begin adding pairs.
        /// </param>
        public void Add(
            IStringList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                IPair<string> item = list.GetPair(index);

                if (item != null)
                    Add(item);
                else
                    Add((IPair<string>)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the strings in the specified collection to the end of this
        /// list.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings whose elements are added.
        /// </param>
        public void Add(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string builders in the specified collection to the end of
        /// this list.
        /// </summary>
        /// <param name="collection">
        /// The collection of string builders whose elements are added.
        /// </param>
        public void Add(
            IEnumerable<StringBuilder> collection
            )
        {
            foreach (StringBuilder item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the key/value pairs of the specified dictionary to the end of
        /// this list.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are added.
        /// </param>
        public void Add(
            IDictionary<string, string> dictionary
            )
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of each argument in the specified collection to
        /// the end of this list.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments whose elements are added.
        /// </param>
        public void Add(
            IEnumerable<Argument> collection
            )
        {
            foreach (Argument item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of each result in the specified collection to
        /// the end of this list.
        /// </summary>
        /// <param name="collection">
        /// The collection of results whose elements are added.
        /// </param>
        public void Add(
            IEnumerable<Result> collection
            )
        {
            foreach (Result item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string pairs in the specified collection to the end of this
        /// list.
        /// </summary>
        /// <param name="collection">
        /// The collection of string pairs whose elements are added.
        /// </param>
        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
            foreach (IPair<string> item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds each string in the specified collection, after transforming it
        /// with the specified callback, to the end of this list.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each string before it is added.
        /// </param>
        /// <param name="collection">
        /// The collection of strings whose transformed elements are added.
        /// </param>
        public void Add(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds each argument in the specified collection, after transforming it
        /// with the specified callback, to the end of this list.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each argument before it is added.
        /// </param>
        /// <param name="collection">
        /// The collection of arguments whose transformed elements are added.
        /// </param>
        public void Add(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
            foreach (Argument item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds each result in the specified collection, after transforming it
        /// with the specified callback, to the end of this list.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each result before it is added.
        /// </param>
        /// <param name="collection">
        /// The collection of results whose transformed elements are added.
        /// </param>
        public void Add(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
            foreach (Result item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of the specified object to the end of this
        /// list.
        /// </summary>
        /// <param name="item">
        /// The object whose string form is added.  If this parameter is null, a
        /// null pair is added.
        /// </param>
        public void AddObject(
            object item
            )
        {
            if (item != null)
            {
                base.Add(new StringPair(
                    StringOps.GetStringFromObject(item)));
            }
            else
            {
                base.Add((IPair<string>)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a new string pair, formed from the string forms of the
        /// specified key and value objects, to the end of this list.
        /// </summary>
        /// <param name="key">
        /// The object whose string form becomes the key (the X component) of
        /// the new pair.
        /// </param>
        /// <param name="value">
        /// The object whose string form becomes the value (the Y component) of
        /// the new pair.
        /// </param>
        public void AddObjects(
            object key,
            object value
            )
        {
            base.Add(new StringPair(
                StringOps.GetStringFromObject(key),
                StringOps.GetStringFromObject(value)));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method adds a null item if the final item currently in
        //       the list is not null -OR- the list is empty.  It returns true
        //       if an item was actually added.
        //
        /// <summary>
        /// Adds a null item to the end of this list if the list is empty or the
        /// final item currently in the list is not null.
        /// </summary>
        /// <returns>
        /// True if a null item was added; otherwise, false.
        /// </returns>
        public bool MaybeAddNull()
        {
            int count = base.Count;

            if (count == 0)
            {
                base.Add(null);
                return true;
            }

            IPair<string> item = base[count - 1];

            if (item == null)
                return false;

            base.Add(null);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Appends null items to the end of this list until it contains at least
        /// the specified number of items.
        /// </summary>
        /// <param name="count">
        /// The minimum number of items the list should contain.
        /// </param>
        /// <returns>
        /// True if the list contains exactly the specified number of items after
        /// the operation; otherwise, false.
        /// </returns>
        public bool MaybeFillWithNull(
            int count
            )
        {
            while (base.Count < count)
                base.Add(null);

            return (base.Count == count);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the strings in the specified collection to the end of this list,
        /// if the collection is not null.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings whose elements are added.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The number of strings added, or an invalid count if the collection
        /// was null.
        /// </returns>
        public int MaybeAddRange(
            IEnumerable<string> collection
            )
        {
            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (string item in collection)
            {
                Add(item);
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string pairs in the specified collection to the end of this
        /// list, if the collection is not null.
        /// </summary>
        /// <param name="collection">
        /// The collection of string pairs whose elements are added.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The number of pairs added, or an invalid count if the collection was
        /// null.
        /// </returns>
        public int MaybeAddRange(
            IEnumerable<IPair<string>> collection
            )
        {
            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (IPair<string> item in collection)
            {
                base.Add(item);
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds a new string pair, with the specified key and a value formed
        /// from the raw-string rendering of the specified string list, to the
        /// end of this list, if the value list is not null.
        /// </summary>
        /// <param name="key">
        /// The key (the X component) of the new pair.
        /// </param>
        /// <param name="value">
        /// The string list whose raw-string form becomes the value (the Y
        /// component) of the new pair.  This parameter may be null.
        /// </param>
        /// <param name="separator">
        /// The separator string used when rendering the value list to its
        /// raw-string form.
        /// </param>
        /// <returns>
        /// True if a pair was added; otherwise, false.
        /// </returns>
        public bool MaybeAddRawString(
            string key,
            IStringList value,
            string separator
            )
        {
            if (value == null)
                return false;

            Add(key, value.ToRawString(separator, separator));
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// Renders this list to its string form, optionally including empty
        /// elements.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include elements whose key and value are both empty;
        /// otherwise, such elements are omitted.
        /// </param>
        /// <returns>
        /// The string form of this list.
        /// </returns>
        public string ToString(
            bool empty
            )
        {
            return ToString(null, empty, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders the elements of this list matching the specified pattern to
        /// the string form.
        /// </summary>
        /// <param name="pattern">
        /// The match pattern used to select which elements are included.  If
        /// this parameter is null, all elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match.
        /// </param>
        /// <returns>
        /// The string form of the matching elements of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders the elements of this list matching the specified pattern to
        /// the string form, optionally including empty elements.
        /// </summary>
        /// <param name="pattern">
        /// The match pattern used to select which elements are included.  If
        /// this parameter is null, all elements are included.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include elements whose key and value are both empty;
        /// otherwise, such elements are omitted.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match.
        /// </param>
        /// <returns>
        /// The string form of the matching elements of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            string separator = Separator; /* PROPERTY */

            if (separator == null)
                separator = DefaultSeparator;

            return ToString(separator, pattern, empty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders the elements of this list matching the specified pattern to
        /// the string form, using the specified separator between elements.
        /// </summary>
        /// <param name="separator">
        /// The separator string placed between elements.
        /// </param>
        /// <param name="pattern">
        /// The match pattern used to select which elements are included.  If
        /// this parameter is null, all elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match.
        /// </param>
        /// <returns>
        /// The string form of the matching elements of this list.
        /// </returns>
        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            return ToString(separator, pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders the elements of this list matching the specified pattern to
        /// the string form, using the specified separator between elements and
        /// optionally including empty elements.
        /// </summary>
        /// <param name="separator">
        /// The separator string placed between elements.
        /// </param>
        /// <param name="pattern">
        /// The match pattern used to select which elements are included.  If
        /// this parameter is null, all elements are included.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include elements whose key and value are both empty;
        /// otherwise, such elements are omitted.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match.
        /// </param>
        /// <returns>
        /// The string form of the matching elements of this list.
        /// </returns>
        public string ToString(
            string separator,
            string pattern,
            bool empty,
            bool noCase
            )
        {
            if (empty)
            {
                return ParserOps<IPair<string>>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
            else
            {
                StringPairList result = new StringPairList();

                foreach (IPair<string> element in this)
                {
                    if (element == null)
                        continue;

                    if (String.IsNullOrEmpty(element.X) &&
                        String.IsNullOrEmpty(element.Y))
                    {
                        continue;
                    }

                    result.Add(element);
                }

                return ParserOps<IPair<string>>.ListToString(
                    result, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders this list to its raw-string form by concatenating the key
        /// and value of each element, with no separators.
        /// </summary>
        /// <returns>
        /// The raw-string form of this list.
        /// </returns>
        public string ToRawString()
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (IPair<string> element in this)
            {
                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append((string)null);
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders this list to its raw-string form by concatenating the key
        /// and value of each element, placing the specified separator between
        /// elements.
        /// </summary>
        /// <param name="separator">
        /// The separator string placed between elements.
        /// </param>
        /// <returns>
        /// The raw-string form of this list.
        /// </returns>
        public string ToRawString(
            string separator
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (IPair<string> element in this)
            {
                if (result.Length > 0)
                    result.Append(separator);

                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append((string)null);
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Renders this list to its raw-string form by concatenating the key
        /// and value of each element, placing the first separator between
        /// elements and the second separator between each element's key and
        /// value.
        /// </summary>
        /// <param name="separator1">
        /// The separator string placed between elements.
        /// </param>
        /// <param name="separator2">
        /// The separator string placed between each element's key and value.
        /// </param>
        /// <returns>
        /// The raw-string form of this list.
        /// </returns>
        public string ToRawString(
            string separator1,
            string separator2
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (IPair<string> element in this)
            {
                if (result.Length > 0)
                    result.Append(separator1);

                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(separator2);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append(separator2);
                    result.Append((string)null);
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToList Methods
        /// <summary>
        /// Creates a new string list containing the elements of this list.
        /// </summary>
        /// <returns>
        /// The newly created string list.
        /// </returns>
        public IStringList ToList()
        {
            return new StringList(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string list containing the elements of this list that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The match pattern used to select which elements are included.  If
        /// this parameter is null, all elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match.
        /// </param>
        /// <returns>
        /// The newly created string list containing the matching elements.
        /// </returns>
        public IStringList ToList(
            string pattern,
            bool noCase
            )
        {
            return ToList(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string list containing the elements of this list that
        /// match the specified pattern, optionally including empty elements.
        /// </summary>
        /// <param name="pattern">
        /// The match pattern used to select which elements are included.  If
        /// this parameter is null, all elements are included.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include elements whose key and value are both empty;
        /// otherwise, such elements are omitted.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive pattern match.
        /// </param>
        /// <returns>
        /// The newly created string list containing the matching elements, or
        /// null if the filtering operation fails.
        /// </returns>
        public IStringList ToList(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            StringPairList inputList;
            StringPairList outputList = new StringPairList();

            if (empty)
            {
                inputList = this;
            }
            else
            {
                inputList = new StringPairList();

                foreach (IPair<string> element in this)
                {
                    if (element == null)
                        continue;

                    if (String.IsNullOrEmpty(element.X) &&
                        String.IsNullOrEmpty(element.Y))
                    {
                        continue;
                    }

                    inputList.Add(element);
                }
            }

            ReturnCode code;
            Result error = null;

            code = GenericOps<IPair<string>>.FilterList(
                inputList, outputList, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);

            if (code != ReturnCode.Ok)
            {
                DebugOps.Complain(code, error);

                //
                // TODO: Return null in the error case here?
                //
                outputList = null;
            }

            return outputList;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Factory Methods
        /// <summary>
        /// Creates a new list of string pairs by parsing the specified string
        /// as a list of sub-lists, where each sub-list provides a key and an
        /// optional value.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// </param>
        /// <returns>
        /// The newly created list of string pairs, or null if parsing fails.
        /// </returns>
        public static StringPairList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new list of string pairs by parsing the specified string
        /// as a list of sub-lists, where each sub-list provides a key and an
        /// optional value.
        /// </summary>
        /// <param name="value">
        /// The string to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives an error message that explains
        /// why parsing failed.
        /// </param>
        /// <returns>
        /// The newly created list of string pairs, or null if parsing fails.
        /// </returns>
        public static StringPairList FromString(
            string value,
            ref Result error
            )
        {
            //
            // TODO: *PERF* We cannot have this call to SplitList perform any
            //       caching because we do not know exactly what the resulting
            //       list will be used for.
            //
            StringList list1 = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, _Constants.Length.Invalid,
                    false, ref list1, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            StringPairList list2 = null;

            if (list1 != null)
            {
                list2 = new StringPairList();

                foreach (string element1 in list1)
                {
                    if (String.IsNullOrEmpty(element1))
                    {
                        list2.Add((IPair<string>)null);
                        continue;
                    }

                    StringList subList1 = null;

                    if (ParserOps<string>.SplitList(
                            null, element1, 0, _Constants.Length.Invalid,
                            false, ref subList1, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    int count = subList1.Count;

                    if (count == 0)
                        continue;

                    string localKey = subList1[0];

                    if (String.IsNullOrEmpty(localKey))
                        localKey = null;

                    string localValue = null;

                    if (count >= 2)
                    {
                        localValue = subList1[1];

                        if (String.IsNullOrEmpty(localValue))
                            localValue = null;
                    }

                    list2.Add(localKey, localValue);
                }
            }

            return list2;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a list of string pairs for the specified collection, reusing
        /// the collection itself if it is already a list of string pairs and
        /// creating a new list otherwise.
        /// </summary>
        /// <param name="collection">
        /// The collection of string pairs to return or copy.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The specified collection if it is already a list of string pairs, a
        /// new list of string pairs containing its elements otherwise, or null
        /// if the collection was null.
        /// </returns>
        public static StringPairList MaybeCreate(
            IEnumerable<IPair<string>> collection
            )
        {
            if (collection == null)
                return null;

            if (collection is StringPairList)
                return (StringPairList)collection;

            return new StringPairList(collection);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Renders this list to its default string form.
        /// </summary>
        /// <returns>
        /// The string form of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(DefaultEmpty);
        }
        #endregion
    }
}
