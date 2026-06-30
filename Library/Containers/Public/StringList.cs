/*
 * StringList.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;

#if LIST_CACHE
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents an ordered, mutable collection of strings that is
    /// also the canonical in-memory representation of a Tcl list within Eagle.
    /// In addition to the normal list operations inherited from
    /// <see cref="List{T}" />, it knows how to convert itself to and from the
    /// well-formed string (list) representation, to build name/value style
    /// lists, and to filter and transform its elements.  Individual elements
    /// may be null.  When the <c>CACHE_STRINGLIST_TOSTRING</c> feature is
    /// enabled, the most recently produced string form may be cached and is
    /// transparently invalidated whenever the list is modified.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("17df73b1-f419-498d-aeca-4be513e25310")]
    public sealed class StringList : List<string>, IList<string>,
            ICollection<string>, IStringList, IGetValue,
            IHaveDictionary<string>
#if LIST_CACHE
            , IReadOnly
#endif
    {
        #region Private Static Data
#if CACHE_STRINGLIST_TOSTRING && CACHE_STATISTICS
        /// <summary>
        /// The per-type cache hit and miss counters, indexed by
        /// <see cref="CacheCountType" />, for the cached string form of this
        /// list class.
        /// </summary>
        private static long[] cacheCounts =
            new long[(int)CacheCountType.SizeOf];
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The element separator used by default when converting this list to
        /// its string form (a single space).
        /// </summary>
        private static readonly string DefaultSeparator =
            Characters.SpaceString;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The default value used to indicate whether empty elements should be
        /// included when converting this list to its string form.
        /// </summary>
        public static readonly bool DefaultEmpty = true;

        /// <summary>
        /// The canonical string representation of a single empty list element
        /// (a pair of braces).
        /// </summary>
        public static readonly string EmptyElement =
            Characters.OpenBrace.ToString() + Characters.CloseBrace.ToString();

        /// <summary>
        /// The sentinel string returned when a named value lookup fails to
        /// locate a matching name.  Reference equality may be used to detect
        /// this value.
        /// </summary>
        public static readonly string NotFound = String.Copy(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if CACHE_STRINGLIST_TOSTRING
        /// <summary>
        /// The cached string form of this list, or null when no cached value
        /// is currently available.
        /// </summary>
        private string @string; /* CACHE */
#endif

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE
        /// <summary>
        /// When non-zero, this list is read-only and attempts to modify it
        /// should be rejected by its callers.
        /// </summary>
        private bool isReadOnly;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty list.
        /// </summary>
        public StringList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty list with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements the new list can initially store without
        /// resizing.
        /// </param>
        public StringList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified object.  If the object is
        /// itself enumerable its elements are added; otherwise, the object is
        /// added as a single element.  A null object adds a single null
        /// element.
        /// </summary>
        /// <param name="value">
        /// The object whose value(s) will populate the new list.  This
        /// parameter may be null.
        /// </param>
        public StringList(
            object value
            )
        {
            AddObjectOrObjects(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection, converting each
        /// element to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects to add to the new list.
        /// </param>
        public StringList(
            IEnumerable collection
            )
        {
            AddObjects(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection, converting each
        /// element to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects to add to the new list.
        /// </param>
        /// <param name="null">
        /// Non-zero to add null elements for any null values in the
        /// collection; otherwise, null values are skipped.
        /// </param>
        public StringList(
            IEnumerable collection,
            bool @null
            )
        {
            AddObjects(collection, @null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of characters, where
        /// each character becomes a single-character element.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters to add to the new list.
        /// </param>
        public StringList(
            IEnumerable<char> collection
            )
        {
            AddChars(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list containing the elements copied from the specified
        /// collection of strings.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to copy into the new list.
        /// </param>
        public StringList(
            IEnumerable<string> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of string builders,
        /// converting each to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of string builders to add to the new list.
        /// </param>
        public StringList(
            IEnumerable<StringBuilder> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of name/value pairs,
        /// adding the name and value of each pair as two consecutive elements.
        /// </summary>
        /// <param name="collection">
        /// The collection of name/value pairs to add to the new list.
        /// </param>
        public StringList(
            IEnumerable<IPair<string>> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of arguments,
        /// converting each to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments to add to the new list.
        /// </param>
        public StringList(
            IEnumerable<Argument> collection
            )
        {
            AddObjects(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of results,
        /// converting each to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of results to add to the new list.
        /// </param>
        public StringList(
            IEnumerable<Result> collection
            )
        {
            AddObjects(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the elements of the specified list starting
        /// at the given index, converting each element to its string form.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added to the new list.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add to the new list.
        /// </param>
        public StringList(
            IList list,
            int startIndex
            )
        {
            Add(list, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the elements of the specified string list
        /// starting at the given index.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added to the new list.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add to the new list.
        /// </param>
        public StringList(
            IList<string> list,
            int startIndex
            )
        {
            Add(list, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the elements of the specified array starting
        /// at the given index.
        /// </summary>
        /// <param name="array">
        /// The source array whose elements will be added to the new list.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="array" />, to
        /// add to the new list.
        /// </param>
        public StringList(
            string[] array,
            int startIndex
            )
        {
            Add(array, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list containing the specified strings.
        /// </summary>
        /// <param name="strings">
        /// The strings to copy into the new list.
        /// </param>
        public StringList(
            params string[] strings
            )
            : base(strings)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of strings, applying
        /// the specified transform callback to each element before adding it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each element prior to adding it.
        /// </param>
        /// <param name="collection">
        /// The collection of strings to transform and add to the new list.
        /// </param>
        public StringList(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
            Add(callback, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of arguments,
        /// applying the specified transform callback to the string form of each
        /// element before adding it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each element prior to adding it.
        /// </param>
        /// <param name="collection">
        /// The collection of arguments to transform and add to the new list.
        /// </param>
        public StringList(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
            Add(callback, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list from the specified collection of results, applying
        /// the specified transform callback to the string form of each element
        /// before adding it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each element prior to adding it.
        /// </param>
        /// <param name="collection">
        /// The collection of results to transform and add to the new list.
        /// </param>
        public StringList(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
            Add(callback, collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list by concatenating the elements of two collections
        /// of strings, skipping any null values.
        /// </summary>
        /// <param name="collection1">
        /// The first collection of strings to add to the new list.
        /// </param>
        /// <param name="collection2">
        /// The second collection of strings to add to the new list.
        /// </param>
        public StringList(
            IEnumerable<string> collection1,
            IEnumerable<string> collection2
            )
        {
            AddRange(collection1, false);
            AddRange(collection2, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        /// <summary>
        /// Constructs a list from the elements of the specified string list
        /// starting at the given index, optionally trimming surrounding white
        /// space from each element.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added to the new list.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add to the new list.
        /// </param>
        /// <param name="noTrim">
        /// Non-zero to add each element verbatim; otherwise, surrounding white
        /// space is trimmed from each element before adding it.
        /// </param>
        internal StringList( /* NOTE: For use by PrivateShellMainCore only. */
            IList<string> list,
            int startIndex,
            bool noTrim
            )
        {
            Add(list, startIndex, noTrim);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Static Methods
        #region Factory Methods
#if LIST_CACHE
        /// <summary>
        /// Creates a new, empty list and marks it as read-only when requested.
        /// </summary>
        /// <param name="readOnly">
        /// Non-zero to mark the new list as read-only; otherwise, zero.
        /// </param>
        /// <returns>
        /// The newly created list.
        /// </returns>
        internal static StringList MaybeReadOnly(
            bool readOnly
            )
        {
            StringList list = new StringList();

            list.isReadOnly = readOnly;

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new list from the specified collection and marks it as
        /// read-only when requested.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to copy into the new list.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero to mark the new list as read-only; otherwise, zero.
        /// </param>
        /// <returns>
        /// The newly created list.
        /// </returns>
        internal static StringList MaybeReadOnly(
            IEnumerable<string> collection,
            bool readOnly
            )
        {
            StringList list = new StringList(collection);

            list.isReadOnly = readOnly;

            return list;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveDictionary<String> Members
        /// <summary>
        /// Locates the value associated with the specified name, treating this
        /// list as a sequence of alternating name and value elements.
        /// </summary>
        /// <param name="name">
        /// The name to locate.  When null, the value following the first
        /// name is returned.
        /// </param>
        /// <returns>
        /// The value associated with the specified name, or
        /// <see cref="NotFound" /> if no matching name is found.
        /// </returns>
        public string GetNamedValue(
            string name
            )
        {
            int count = base.Count;

            for (int index = 0; index < count; index += 2)
            {
                if ((name == null) || SharedStringOps.Equals(
                        base[index], name, StringComparison.Ordinal))
                {
                    index++;

                    if (index < count)
                        return base[index];
                }
            }

            return NotFound;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the value associated with the specified name, treating this
        /// list as a sequence of alternating name and value elements.  If the
        /// name already exists its value is replaced; otherwise, the name and
        /// value are appended (or inserted) so the pairing is preserved.
        /// </summary>
        /// <param name="name">
        /// The name whose value should be set.  When null, the value following
        /// the first name is set.
        /// </param>
        /// <param name="value">
        /// The value to associate with the specified name.
        /// </param>
        public void SetNamedValue(
            string name,
            string value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int count = base.Count;

            for (int index = 0; index < count; index += 2)
            {
                if ((name == null) || SharedStringOps.Equals(
                        base[index], name, StringComparison.Ordinal))
                {
                    index++;

                    if (index < count)
                        base[index] = value;
                    else
                        base.Add(value);

                    return;
                }
            }

            if ((count % 2) == 0)
            {
                base.Add(name);
                base.Add(value);
            }
            else
            {
                base.Insert(0, value);
                base.Insert(0, name);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue Members
        //
        // NOTE: This must call ToString to provide a "flattened" value
        //       because this is a mutable class.
        //
        /// <summary>
        /// Gets the flattened value of this list, which is its string (list)
        /// form.
        /// </summary>
        public object Value
        {
            get { return ToString(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in characters, of the string form of this list, or
        /// an invalid length when that string form is null.
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
        /// Gets the string (list) form of this list.
        /// </summary>
        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadOnly Members
#if LIST_CACHE
        /// <summary>
        /// Gets a value indicating whether this list is read-only.  True if the
        /// list is read-only; otherwise, false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a new list that is a shallow copy of this list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new StringList(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStringList Members
        #region Properties
        /// <summary>
        /// The element separator used by this list when converting to its
        /// string form, or null to use the default separator.
        /// </summary>
        private string separator;

        /// <summary>
        /// Gets or sets the element separator used by this list when converting
        /// to its string form.  When null, the default separator is used.
        /// </summary>
        public string Separator
        {
            get { return separator; }
            set { separator = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE
        /// <summary>
        /// The cache key associated with this list, or null when it is not
        /// participating in the list cache.
        /// </summary>
        private string cacheKey;

        /// <summary>
        /// Gets or sets the cache key associated with this list within the list
        /// cache.
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
        /// Determines whether the specified key is present in this list,
        /// treating it as a sequence of alternating name and value elements and
        /// examining only the name elements.
        /// </summary>
        /// <param name="key">
        /// The key (name) to locate.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when matching.
        /// </param>
        /// <returns>
        /// True if the specified key is present; otherwise, false.
        /// </returns>
        public bool ContainsKey(
            string key,
            StringComparison comparisonType
            )
        {
            int count = base.Count;

            if (count == 0)
                return false;

            for (int index = 0; index < count; index += 2)
            {
                if (SharedStringOps.Equals(
                        base[index], key, comparisonType))
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
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get.
        /// </param>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        public string GetItem(
            int index
            )
        {
            return this[index];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the element at the specified index, wrapped as a name/value
        /// pair whose name is the element and whose value is null.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get.
        /// </param>
        /// <returns>
        /// A name/value pair containing the element at the specified index.
        /// </returns>
        public IPair<string> GetPair(
            int index
            )
        {
            return new StringPair(this[index]);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Insert / Add Methods
        /// <summary>
        /// Adds the specified key and value to this list as two consecutive
        /// elements.
        /// </summary>
        /// <param name="key">
        /// The key element to add.
        /// </param>
        /// <param name="value">
        /// The value element to add.
        /// </param>
        public void Add(
            string key,
            string value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(key);
            base.Add(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified key and value to this list as two consecutive
        /// elements, optionally normalizing white space in the value and/or
        /// truncating it with an ellipsis.
        /// </summary>
        /// <param name="key">
        /// The key element to add.
        /// </param>
        /// <param name="value">
        /// The value element to add.
        /// </param>
        /// <param name="normalize">
        /// Non-zero to normalize white space within the value before adding it.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the value with an ellipsis before adding it.
        /// </param>
        public void Add(
            string key,
            string value,
            bool normalize,
            bool ellipsis
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(key);

            string localValue = value;

            if (normalize)
            {
                localValue = StringOps.NormalizeWhiteSpace(
                    localValue, Characters.Space,
                    WhiteSpaceFlags.FormattedUse);
            }

            if (ellipsis)
                localValue = FormatOps.Ellipsis(localValue);

            base.Add(localValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified key and value collection to this list as two
        /// consecutive elements, where the value element is the string (list)
        /// form of the collection.
        /// </summary>
        /// <param name="key">
        /// The key element to add.
        /// </param>
        /// <param name="value">
        /// The collection of strings whose string (list) form is added as the
        /// value element.  This parameter may be null.
        /// </param>
        public void Add(
            string key,
            IEnumerable<string> value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(key);
            base.Add((value != null) ? StringList.MakeList(value) : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of the specified string builder to this list as
        /// a single element.
        /// </summary>
        /// <param name="item">
        /// The string builder to add.  This parameter may be null, in which
        /// case a null element is added.
        /// </param>
        public void Add(
            StringBuilder item
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add((item != null) ? item.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified array, starting at the given
        /// index, to this list.
        /// </summary>
        /// <param name="array">
        /// The source array whose elements will be added.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="array" />, to
        /// add.
        /// </param>
        public void Add(
            string[] array,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < array.Length; index++)
                base.Add(array[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified list, starting at the given
        /// index, to this list, converting each element to its string form.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add.
        /// </param>
        public void Add(
            IList list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < list.Count; index++)
                base.Add(StringOps.GetStringFromObject(list[index]));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified string list, starting at the
        /// given index, to this list.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add.
        /// </param>
        public void Add(
            IStringList list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < list.Count; index++)
                base.Add(list.GetItem(index));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified string list, starting at the
        /// given index, to this list.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add.
        /// </param>
        public void Add(
            IList<string> list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            for (int index = startIndex; index < list.Count; index++)
                base.Add(list[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds up to the specified number of elements of the given list,
        /// starting at the given index, to this list.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add.
        /// </param>
        /// <param name="count">
        /// The maximum number of elements to add.
        /// </param>
        /// <returns>
        /// The requested count of elements.
        /// </returns>
        public int Add(
            IList<string> list,
            int startIndex,
            int count
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            //
            // TODO: Why does this look wrong?
            //
            int listCount = list.Count;
            int stopIndex = startIndex + count;

            if (stopIndex > (listCount - 1))
                stopIndex = listCount - 1;

            for (int index = startIndex; index <= stopIndex; index++)
                base.Add(list[index]);

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of strings to this
        /// list.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings whose elements will be added.
        /// </param>
        public void Add(
            IEnumerable<string> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of string builders to
        /// this list, converting each to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of string builders whose elements will be added.
        /// </param>
        public void Add(
            IEnumerable<StringBuilder> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the entries of the specified dictionary to this list, where
        /// each entry contributes its key and value as two consecutive
        /// elements.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries will be added.
        /// </param>
        public void Add(
            IDictionary<string, string> dictionary
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of arguments to this
        /// list, converting each to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments whose elements will be added.
        /// </param>
        public void Add(
            IEnumerable<Argument> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of results to this
        /// list, converting each to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of results whose elements will be added.
        /// </param>
        public void Add(
            IEnumerable<Result> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of name/value pairs to
        /// this list, where each pair contributes its name and value as two
        /// consecutive elements.
        /// </summary>
        /// <param name="collection">
        /// The collection of name/value pairs whose elements will be added.
        /// </param>
        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (IPair<string> item in collection)
                Add(item.X, item.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of strings to this
        /// list, applying the specified transform callback to each element
        /// before adding it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each element prior to adding it.
        /// </param>
        /// <param name="collection">
        /// The collection of strings whose elements will be transformed and
        /// added.
        /// </param>
        public void Add(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (string item in collection)
                base.Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of arguments to this
        /// list, applying the specified transform callback to the string form
        /// of each element before adding it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each element prior to adding it.
        /// </param>
        /// <param name="collection">
        /// The collection of arguments whose elements will be transformed and
        /// added.
        /// </param>
        public void Add(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (Argument item in collection)
                base.Add(callback(StringOps.GetStringFromObject(item)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of results to this
        /// list, applying the specified transform callback to the string form
        /// of each element before adding it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to transform each element prior to adding it.
        /// </param>
        /// <param name="collection">
        /// The collection of results whose elements will be transformed and
        /// added.
        /// </param>
        public void Add(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (Result item in collection)
                base.Add(callback(StringOps.GetStringFromObject(item)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of the specified object to this list as a
        /// single element.
        /// </summary>
        /// <param name="item">
        /// The object to add.  This parameter may be null.
        /// </param>
        public void AddObject(
            object item
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(StringOps.GetStringFromObject(item));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string forms of the specified key and value objects to this
        /// list as two consecutive elements.
        /// </summary>
        /// <param name="key">
        /// The key object to add.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value object to add.  This parameter may be null.
        /// </param>
        public void AddObjects(
            object key,
            object value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(StringOps.GetStringFromObject(key));
            base.Add(StringOps.GetStringFromObject(value));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method adds a null item if the final item currently in
        //       the list is not null -OR- the list is empty.  It returns true
        //       if an item was actually added.
        //
        /// <summary>
        /// Adds a null element to this list when the last element is not
        /// already null, or when the list is empty.
        /// </summary>
        /// <returns>
        /// True if a null element was added; otherwise, false.
        /// </returns>
        public bool MaybeAddNull()
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int count = base.Count;

            if (count == 0)
            {
                base.Add(null);
                return true;
            }

            string item = base[count - 1];

            if (item == null)
                return false;

            base.Add(null);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Appends null elements to this list until it contains at least the
        /// specified number of elements.
        /// </summary>
        /// <param name="count">
        /// The minimum number of elements the list should contain.
        /// </param>
        /// <returns>
        /// True if the list contains exactly the specified number of elements
        /// after filling; otherwise, false.
        /// </returns>
        public bool MaybeFillWithNull(
            int count
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            while (base.Count < count)
                base.Add(null);

            return (base.Count == count);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection of strings to this
        /// list when the collection is non-null.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings whose elements will be added.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The number of elements added, or an invalid count when the
        /// collection is null.
        /// </returns>
        public int MaybeAddRange(
            IEnumerable<string> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (string item in collection)
            {
                base.Add(item);
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the string form of each name/value pair in the specified
        /// collection to this list as a single element, when the collection is
        /// non-null.
        /// </summary>
        /// <param name="collection">
        /// The collection of name/value pairs whose elements will be added.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The number of elements added, or an invalid count when the
        /// collection is null.
        /// </returns>
        public int MaybeAddRange(
            IEnumerable<IPair<string>> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            int result = _Constants.Count.Invalid;

            if (collection == null)
                return result;

            result = 0;

            foreach (IPair<string> item in collection)
            {
                base.Add(
                    (item != null) ? item.ToString() : null);

                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified key together with the raw (unformatted) string
        /// form of the specified value list as two consecutive elements, when
        /// the value list is non-null.
        /// </summary>
        /// <param name="key">
        /// The key element to add.
        /// </param>
        /// <param name="value">
        /// The value list whose raw string form is added as the value element.
        /// This parameter may be null.
        /// </param>
        /// <param name="separator">
        /// The separator used between elements when building the raw string
        /// form of the value list.
        /// </param>
        /// <returns>
        /// True if the key and value were added; otherwise, false.
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
        /// Converts this list to its string (list) form using the configured
        /// separator.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include empty elements in the result; otherwise, empty
        /// elements are omitted.
        /// </param>
        /// <returns>
        /// The string (list) form of this list.
        /// </returns>
        public string ToString(
            bool empty
            )
        {
            return ToString(null, empty, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the matching elements of this list to its string (list)
        /// form using the configured separator.
        /// </summary>
        /// <param name="pattern">
        /// A pattern used to filter the elements included in the result, or
        /// null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string (list) form of the matching elements.
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
        /// Converts the matching elements of this list to its string (list)
        /// form using the configured separator, or the default separator when
        /// none is configured.
        /// </summary>
        /// <param name="pattern">
        /// A pattern used to filter the elements included in the result, or
        /// null to include all elements.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include empty elements in the result; otherwise, empty
        /// elements are omitted.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string (list) form of the matching elements.
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
        /// Converts the matching elements of this list to its string (list)
        /// form using the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The separator placed between elements in the result.
        /// </param>
        /// <param name="pattern">
        /// A pattern used to filter the elements included in the result, or
        /// null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string (list) form of the matching elements.
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
        /// Converts the matching elements of this list to its string (list)
        /// form using the specified separator.  When the cached string feature
        /// is enabled and the requested options permit it, a previously cached
        /// result may be returned and a freshly computed result may be cached.
        /// </summary>
        /// <param name="separator">
        /// The separator placed between elements in the result.
        /// </param>
        /// <param name="pattern">
        /// A pattern used to filter the elements included in the result, or
        /// null to include all elements.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include empty elements in the result; otherwise, empty
        /// elements are omitted.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The string (list) form of the matching elements.
        /// </returns>
        public string ToString(
            string separator,
            string pattern,
            bool empty,
            bool noCase
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            bool canUseCachedString = CanUseCachedString(
                separator, pattern, empty, noCase);

            if (canUseCachedString && (@string != null))
            {
#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Hit]);
#endif

                return @string;
            }

#if CACHE_STATISTICS
            Interlocked.Increment(
                ref cacheCounts[(int)CacheCountType.Miss]);
#endif
#endif

            if (empty)
            {
                string result = ParserOps<string>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);

#if CACHE_STRINGLIST_TOSTRING
                if (canUseCachedString)
                    @string = result;
#endif

                return result;
            }
            else
            {
                StringList result = new StringList();

                foreach (string element in this)
                {
                    if (String.IsNullOrEmpty(element))
                        continue;

                    result.Add(element);
                }

                return ParserOps<string>.ListToString(
                    result, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the raw (unformatted) string form of this list by directly
        /// concatenating all elements with no separator and no list quoting.
        /// </summary>
        /// <returns>
        /// The concatenated raw string form of this list.
        /// </returns>
        public string ToRawString()
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (string element in this)
                result.Append(element);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the raw (unformatted) string form of this list by
        /// concatenating all elements with the specified separator placed
        /// between them and no list quoting.
        /// </summary>
        /// <param name="separator">
        /// The separator placed between consecutive elements.
        /// </param>
        /// <returns>
        /// The concatenated raw string form of this list.
        /// </returns>
        public string ToRawString(
            string separator
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (string element in this)
            {
                if (result.Length > 0)
                    result.Append(separator);

                result.Append(element);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the raw (unformatted) string form of this list, where the
        /// first separator is placed between elements and the second separator
        /// is prepended to every element.
        /// </summary>
        /// <param name="separator1">
        /// The separator placed between consecutive elements.
        /// </param>
        /// <param name="separator2">
        /// The separator prepended to each element.
        /// </param>
        /// <returns>
        /// The concatenated raw string form of this list.
        /// </returns>
        public string ToRawString(
            string separator1,
            string separator2
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (string element in this)
            {
                if (result.Length > 0)
                    result.Append(separator1);

                result.Append(separator2);
                result.Append(element);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToList Methods
        /// <summary>
        /// Creates a new string list containing the same elements as this list.
        /// </summary>
        /// <returns>
        /// The newly created list.
        /// </returns>
        public IStringList ToList()
        {
            return new StringList(this); /* NOTE: Gee, that was easy. */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string list containing the elements of this list that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// A pattern used to filter the elements included in the result, or
        /// null to include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The newly created list of matching elements.
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
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// A pattern used to filter the elements included in the result, or
        /// null to include all elements.
        /// </param>
        /// <param name="empty">
        /// Non-zero to consider empty elements; otherwise, empty elements are
        /// omitted before filtering.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <returns>
        /// The newly created list of matching elements, or null when the filter
        /// operation fails.
        /// </returns>
        public IStringList ToList(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            StringList inputList;
            StringList outputList = new StringList();

            if (empty)
            {
                inputList = this;
            }
            else
            {
                inputList = new StringList();

                foreach (string element in this)
                {
                    if (String.IsNullOrEmpty(element))
                        continue;

                    inputList.Add(element);
                }
            }

            ReturnCode code;
            Result error = null;

            code = GenericOps<string>.FilterList(
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

        #region Public Methods
        #region Add Methods
        /// <summary>
        /// Adds the two specified elements followed by any additional strings to
        /// this list.
        /// </summary>
        /// <param name="item1">
        /// The first element to add.
        /// </param>
        /// <param name="item2">
        /// The second element to add.
        /// </param>
        /// <param name="strings">
        /// Any additional elements to add after the first two.
        /// </param>
        public void Add(
            string item1,
            string item2,
            params string[] strings
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            Add(item1, item2);
            Add(strings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified collection of characters to this list, where each
        /// character becomes a single-character element.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters whose elements will be added.
        /// </param>
        public void AddChars(
            IEnumerable<char> collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            foreach (char item in collection)
                base.Add(item.ToString());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection to this list,
        /// converting each element to its string form and including null
        /// elements.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects whose elements will be added.
        /// </param>
        public void AddObjects(
            IEnumerable collection
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection to this list,
        /// converting each element to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects whose elements will be added.
        /// </param>
        /// <param name="null">
        /// Non-zero to add null elements for any null values in the
        /// collection; otherwise, null values are skipped.
        /// </param>
        public void AddObjects(
            IEnumerable collection,
            bool @null
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            AddRange(collection, @null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified object to this list.  If the object is itself
        /// enumerable its elements are added; otherwise, the object is added as
        /// a single element.  A null object adds a single null element.
        /// </summary>
        /// <param name="value">
        /// The object whose value(s) will be added.  This parameter may be
        /// null.
        /// </param>
        public void AddObjectOrObjects(
            object value
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (value != null)
            {
                IEnumerable enumerable = value as IEnumerable;

                if (enumerable != null)
                    AddObjects(enumerable, true);
                else
                    AddObjects(new object[] { value }, true);
            }
            else
            {
                base.Add(null);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Range Methods
        /// <summary>
        /// Inserts the elements of the specified list, starting at the given
        /// source index, into this list at the specified position, converting
        /// each element to its string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based position in this list at which the elements should be
        /// inserted.
        /// </param>
        /// <param name="list">
        /// The source list whose elements will be inserted.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// insert.
        /// </param>
        public void InsertRange(
            int index,
            IList list,
            int startIndex
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.InsertRange(index, new StringList(list, startIndex));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Search Methods
        /// <summary>
        /// Determines whether this list contains the specified element, using
        /// the given string comparison rules.
        /// </summary>
        /// <param name="item">
        /// The element to locate.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when matching.
        /// </param>
        /// <returns>
        /// True if the specified element is present; otherwise, false.
        /// </returns>
        public bool Contains(
            string item,
            StringComparison comparisonType
            )
        {
            int count = base.Count;

            if (count == 0)
                return false;

            for (int index = 0; index < count; index++)
            {
                if (SharedStringOps.Equals(
                        base[index], item, comparisonType))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Searches for the specified element starting at the given index, using
        /// the given string comparison rules.
        /// </summary>
        /// <param name="value">
        /// The element to locate.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin the search.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when matching.
        /// </param>
        /// <returns>
        /// The zero-based index of the first matching element, or an invalid
        /// index if no match is found.
        /// </returns>
        public int IndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType
            )
        {
            for (int index = startIndex; index < base.Count; index++)
            {
                if (SharedStringOps.Equals(
                        base[index], value, comparisonType))
                {
                    return index;
                }
            }

            return Index.Invalid;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        /// <summary>
        /// Adds the specified element to this list unless it is null and null
        /// elements are not allowed.
        /// </summary>
        /// <param name="item">
        /// The element to add.  This parameter may be null.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to allow a null element to be added; otherwise, a null
        /// element is silently skipped.
        /// </param>
        internal void MaybeAdd(
            string item,
            bool allowNull
            )
        {
            if (!allowNull && (item == null))
                return;

#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes duplicate (and null) elements from this list, leaving only
        /// the distinct elements.
        /// </summary>
        internal void MakeUnique()
        {
            IEnumerable<string> collection = CopyUnique();

#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            base.Clear();

            if (collection != null)
                base.AddRange(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        #region Copy Methods
        /// <summary>
        /// Builds a new collection containing the distinct, non-null elements of
        /// this list, preserving first-seen order.
        /// </summary>
        /// <returns>
        /// A collection of the distinct elements of this list.
        /// </returns>
        private IEnumerable<string> CopyUnique()
        {
            StringDictionary dictionary = new StringDictionary();

            foreach (string element in this)
            {
                if (element == null) continue;
                dictionary[element] = null;
            }

            return new StringList(dictionary.Keys);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        /// <summary>
        /// Adds the specified element to this list, optionally trimming
        /// surrounding white space from it first.
        /// </summary>
        /// <param name="item">
        /// The element to add.  This parameter may be null.
        /// </param>
        /// <param name="noTrim">
        /// Non-zero to add the element verbatim; otherwise, surrounding white
        /// space is trimmed before adding it.
        /// </param>
        private void MaybeTrimAndAdd(
            string item,
            bool noTrim
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (!noTrim && (item != null))
                item = item.Trim();

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the value obtained from the specified value container to this
        /// list.  If that value is enumerable its elements are added;
        /// otherwise, the string form of the value is added as a single
        /// element.  A null container adds a single null element.
        /// </summary>
        /// <param name="getValue">
        /// The value container whose value will be added.  This parameter may
        /// be null.
        /// </param>
        private void Add(
            IGetValue getValue
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (getValue != null)
            {
                if (AddRange(getValue.Value as IEnumerable, true))
                    return;

                base.Add(getValue.String);
                return;
            }

            base.Add(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified collection to this list,
        /// converting each element to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects whose elements will be added.  This
        /// parameter may be null.
        /// </param>
        /// <param name="null">
        /// Non-zero to add null elements for any null values in the
        /// collection; otherwise, null values are skipped.
        /// </param>
        /// <returns>
        /// True if the collection was non-null and was processed; otherwise,
        /// false.
        /// </returns>
        private bool AddRange(
            IEnumerable collection,
            bool @null
            )
        {
#if CACHE_STRINGLIST_TOSTRING
            InvalidateCachedString(false);
#endif

            if (collection == null)
                return false;

            IEnumerable<string> localCollection =
                collection as IEnumerable<string>;

            //
            // HACK: This is an optimization and there MAY be null
            //       values hiding in the collection; therefore,
            //       only use the optimization if null values are
            //       requested.
            //
            if (@null && (localCollection != null))
            {
                base.AddRange(localCollection);
                return true;
            }

            foreach (object item in collection)
            {
                if (item != null)
                    base.Add(StringOps.GetStringFromObject(item));
                else if (@null)
                    base.Add(null);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the elements of the specified list, starting at the given
        /// index, to this list, optionally trimming surrounding white space
        /// from each element.
        /// </summary>
        /// <param name="list">
        /// The source list whose elements will be added.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element, within <paramref name="list" />, to
        /// add.
        /// </param>
        /// <param name="noTrim">
        /// Non-zero to add each element verbatim; otherwise, surrounding white
        /// space is trimmed from each element before adding it.
        /// </param>
        private void Add(
            IList<string> list,
            int startIndex,
            bool noTrim
            )
        {
            for (int index = startIndex; index < list.Count; index++)
                MaybeTrimAndAdd(list[index], noTrim);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Argument Handling Methods
        /// <summary>
        /// Returns the range of the specified list starting at the given index,
        /// or null when that range would be empty (that is, when it would
        /// contain only a single empty element).
        /// </summary>
        /// <param name="list">
        /// The source list.  This parameter may be null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include, or an invalid index to
        /// start from the beginning of the list.
        /// </param>
        /// <returns>
        /// The requested range of the list, or null when the list is null, the
        /// index is out of range, or the resulting range would be empty.
        /// </returns>
        public static StringList NullIfEmpty(
            StringList list,
            int firstIndex
            )
        {
            if (list == null)
                return null;

            if (firstIndex == Index.Invalid)
                firstIndex = 0;

            if ((firstIndex < 0) || (firstIndex >= list.Count))
                return null;

            //
            // NOTE: If there are elements beyond the first index or the
            //       element at the first index is not empty, then return
            //       the range starting from the first index; otherwise,
            //       return null.
            //
            if (((firstIndex + 1) < list.Count) ||
                !String.IsNullOrEmpty(list[firstIndex]))
            {
                return GetRange(list, firstIndex);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Range Methods
        /// <summary>
        /// Creates a new string list from the elements of the specified list
        /// starting at the given index and continuing to the end of the list.
        /// </summary>
        /// <param name="list">
        /// The source list.  This parameter may be null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include, or an invalid index to
        /// start from the beginning of the list.
        /// </param>
        /// <returns>
        /// The newly created range, or null when the list is null.
        /// </returns>
        public static StringList GetRange(
            IList list,
            int firstIndex
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string list from the elements of the specified list
        /// starting at the given index and continuing to the end of the list.
        /// </summary>
        /// <param name="list">
        /// The source list.  This parameter may be null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include, or an invalid index to
        /// start from the beginning of the list.
        /// </param>
        /// <param name="nullIfEmpty">
        /// Non-zero to return null instead of an empty range.
        /// </param>
        /// <returns>
        /// The newly created range, or null when the list is null or the range
        /// would be empty and <paramref name="nullIfEmpty" /> is non-zero.
        /// </returns>
        public static StringList GetRange(
            IList list,
            int firstIndex,
            bool nullIfEmpty
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid,
                nullIfEmpty);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string list from the elements of the specified list
        /// between the given first and last indexes, inclusive.
        /// </summary>
        /// <param name="list">
        /// The source list.  This parameter may be null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include, or an invalid index to
        /// start from the beginning of the list.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element to include, or an invalid index to
        /// continue to the end of the list.
        /// </param>
        /// <returns>
        /// The newly created range, or null when the list is null.
        /// </returns>
        public static StringList GetRange(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            return GetRange(list, firstIndex, lastIndex, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new string list from the elements of the specified list
        /// between the given first and last indexes, inclusive, converting each
        /// element to its string form.
        /// </summary>
        /// <param name="list">
        /// The source list.  This parameter may be null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element to include, or an invalid index to
        /// start from the beginning of the list.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element to include, or an invalid index to
        /// continue to the end of the list.
        /// </param>
        /// <param name="nullIfEmpty">
        /// Non-zero to return null instead of an empty range.
        /// </param>
        /// <returns>
        /// The newly created range, or null when the list is null or the range
        /// would be empty and <paramref name="nullIfEmpty" /> is non-zero.
        /// </returns>
        public static StringList GetRange(
            IList list,
            int firstIndex,
            int lastIndex,
            bool nullIfEmpty
            )
        {
            if (list == null)
                return null;

            StringList range = null;

            if (firstIndex == Index.Invalid)
                firstIndex = 0;

            if (lastIndex == Index.Invalid)
                lastIndex = list.Count - 1;

            if ((!nullIfEmpty ||
                    ((list.Count > 0) && ((lastIndex - firstIndex) > 0))))
            {
                range = new StringList();

                for (int index = firstIndex; index <= lastIndex; index++)
                    range.Add(StringOps.GetStringFromObject(list[index]));
            }

            return range;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// Builds the string (list) form of the specified strings.
        /// </summary>
        /// <param name="strings">
        /// The strings to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified strings.
        /// </returns>
        public static string MakeList(
            params string[] strings
            )
        {
            return MakeList((IEnumerable<string>)strings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified collection of
        /// strings.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified collection.
        /// </returns>
        public static string MakeList(
            IEnumerable<string> collection
            )
        {
            return new StringList(collection).ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified strings, omitting any
        /// null values.
        /// </summary>
        /// <param name="strings">
        /// The strings to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified strings, without null
        /// values.
        /// </returns>
        public static string MakeListWithoutNulls(
            params string[] strings
            )
        {
            return MakeListWithoutNulls((IEnumerable<string>)strings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified collection of
        /// strings, omitting any null values.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified collection, without null
        /// values.
        /// </returns>
        public static string MakeListWithoutNulls(
            IEnumerable<string> collection
            )
        {
            return new StringList(collection, false).ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified objects, converting
        /// each to its string form.
        /// </summary>
        /// <param name="objects">
        /// The objects to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified objects.
        /// </returns>
        public static string MakeList(
            params object[] objects
            )
        {
            return MakeList((IEnumerable)objects);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified collection,
        /// converting each element to its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified collection.
        /// </returns>
        public static string MakeList(
            IEnumerable collection
            )
        {
            return new StringList(collection).ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified objects, converting
        /// each to its string form and omitting any null values.
        /// </summary>
        /// <param name="objects">
        /// The objects to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified objects, without null
        /// values.
        /// </returns>
        public static string MakeListWithoutNulls(
            params object[] objects
            )
        {
            return MakeListWithoutNulls((IEnumerable)objects);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the string (list) form of the specified collection,
        /// converting each element to its string form and omitting any null
        /// values.
        /// </summary>
        /// <param name="collection">
        /// The collection of objects to convert into a list.
        /// </param>
        /// <returns>
        /// The string (list) form of the specified collection, without null
        /// values.
        /// </returns>
        public static string MakeListWithoutNulls(
            IEnumerable collection
            )
        {
            return new StringList(collection, false).ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        /// <summary>
        /// Parses the specified string (list) value into a new string list.
        /// </summary>
        /// <param name="value">
        /// The string (list) value to parse.
        /// </param>
        /// <returns>
        /// The newly created list, or null when the value cannot be parsed as a
        /// well-formed list.
        /// </returns>
        public static StringList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses the specified string (list) value into a new string list.
        /// </summary>
        /// <param name="value">
        /// The string (list) value to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that prevented the
        /// value from being parsed.
        /// </param>
        /// <returns>
        /// The newly created list, or null when the value cannot be parsed as a
        /// well-formed list.
        /// </returns>
        public static StringList FromString(
            string value,
            ref Result error
            )
        {
            //
            // TODO: *PERF* We cannot have this call to SplitList perform any
            //       caching because we do not know exactly what the resulting
            //       list will be used for.
            //
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, _Constants.Length.Invalid,
                    false, ref list, ref error) != ReturnCode.Ok)
            {
                list = null;
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the specified collection as a string list, reusing it
        /// directly when it is already a string list and creating a new list
        /// otherwise.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to obtain a string list for.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The collection as a string list, or null when the collection is
        /// null.
        /// </returns>
        public static StringList MaybeCreate(
            IEnumerable<string> collection
            )
        {
            if (collection == null)
                return null;

            if (collection is StringList)
                return (StringList)collection;

            return new StringList(collection);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this list to its string (list) form using the configured
        /// separator and the default handling of empty elements.
        /// </summary>
        /// <returns>
        /// The string (list) form of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(DefaultEmpty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cached String Helper Methods
#if CACHE_STRINGLIST_TOSTRING
        /// <summary>
        /// Discards any cached string form of this list so it will be recomputed
        /// on the next request.
        /// </summary>
        /// <param name="children">
        /// Reserved for future use; this parameter is currently ignored.
        /// </param>
        private void InvalidateCachedString(
            bool children /* NOT USED */
            )
        {
            @string = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the cached string form of this list may be used
        /// (or populated) for the specified set of conversion options.
        /// </summary>
        /// <param name="separator">
        /// The separator that would be used for the conversion.
        /// </param>
        /// <param name="pattern">
        /// The filter pattern that would be used for the conversion, or null
        /// when none.
        /// </param>
        /// <param name="empty">
        /// Non-zero when empty elements would be included in the conversion.
        /// </param>
        /// <param name="noCase">
        /// Non-zero when case-insensitive pattern matching would be used.
        /// </param>
        /// <returns>
        /// True if the cached string may be used for these options; otherwise,
        /// false.
        /// </returns>
        private static bool CanUseCachedString(
            string separator,
            string pattern,
            bool empty,
            bool noCase
            )
        {
            if (!Parser.IsListSeparator(separator))
                return false;

            if (pattern != null)
                return false;

            if (!empty)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        /// <summary>
        /// Determines whether any cache hit or miss counts have been recorded
        /// for the cached string form of this list class.
        /// </summary>
        /// <returns>
        /// True if any cache counts have been recorded; otherwise, false.
        /// </returns>
        public static bool HaveCacheCounts()
        {
            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the cache hit and miss counts for the cached string form of
        /// this list class into a human-readable string.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include counters whose value is zero in the result.
        /// </param>
        /// <returns>
        /// The formatted cache counts.
        /// </returns>
        public static string CacheCountsToString(bool empty)
        {
            return FormatOps.CacheCounts(cacheCounts, empty);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Explicit ICollection<string> Overrides
        /// <summary>
        /// Adds the specified element to this list, invalidating the cached
        /// string form.
        /// </summary>
        /// <param name="item">
        /// The element to add.
        /// </param>
        void ICollection<string>.Add(
            string item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all elements from this list, invalidating the cached string
        /// form.
        /// </summary>
        void ICollection<string>.Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the first occurrence of the specified element from this list,
        /// invalidating the cached string form.
        /// </summary>
        /// <param name="item">
        /// The element to remove.
        /// </param>
        /// <returns>
        /// True if an element was removed; otherwise, false.
        /// </returns>
        bool ICollection<string>.Remove(
            string item
            )
        {
            InvalidateCachedString(false);

            return base.Remove(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<string> Overrides
        /// <summary>
        /// Adds the specified element to this list, invalidating the cached
        /// string form.
        /// </summary>
        /// <param name="item">
        /// The element to add.
        /// </param>
        public new void Add(
            string item
            )
        {
            InvalidateCachedString(false);

            base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all elements from this list, invalidating the cached string
        /// form.
        /// </summary>
        public new void Clear()
        {
            InvalidateCachedString(false);

            base.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the first occurrence of the specified element from this list,
        /// invalidating the cached string form.
        /// </summary>
        /// <param name="item">
        /// The element to remove.
        /// </param>
        /// <returns>
        /// True if an element was removed; otherwise, false.
        /// </returns>
        public new bool Remove(
            string item
            )
        {
            InvalidateCachedString(false);

            return base.Remove(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IList<string> Overrides
        /// <summary>
        /// Inserts the specified element at the given index, invalidating the
        /// cached string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the element should be inserted.
        /// </param>
        /// <param name="item">
        /// The element to insert.
        /// </param>
        void IList<string>.Insert(
            int index,
            string item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the element at the given index, invalidating the cached
        /// string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to remove.
        /// </param>
        void IList<string>.RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the element at the specified index, invalidating the
        /// cached string form when set.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get or set.
        /// </param>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        string IList<string>.this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IList<string> Overrides
        /// <summary>
        /// Inserts the specified element at the given index, invalidating the
        /// cached string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the element should be inserted.
        /// </param>
        /// <param name="item">
        /// The element to insert.
        /// </param>
        public new void Insert(
            int index,
            string item
            )
        {
            InvalidateCachedString(false);

            base.Insert(index, item); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the element at the given index, invalidating the cached
        /// string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to remove.
        /// </param>
        public new void RemoveAt(
            int index
            )
        {
            InvalidateCachedString(false);

            base.RemoveAt(index); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the element at the specified index, invalidating the
        /// cached string form when set.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get or set.
        /// </param>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        public new string this[int index]
        {
            get { return base[index]; /* throw */ }
            set { InvalidateCachedString(false); base[index] = value; /* throw */ }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List<string> Overrides
        /// <summary>
        /// Adds the elements of the specified collection to the end of this
        /// list, invalidating the cached string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings whose elements will be added.
        /// </param>
        public new void AddRange(
            IEnumerable<string> collection
            )
        {
            InvalidateCachedString(false);

            base.AddRange(collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Inserts the elements of the specified collection into this list at
        /// the given index, invalidating the cached string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the elements should be inserted.
        /// </param>
        /// <param name="collection">
        /// The collection of strings whose elements will be inserted.
        /// </param>
        public new void InsertRange(
            int index,
            IEnumerable<string> collection
            )
        {
            InvalidateCachedString(false);

            base.InsertRange(index, collection); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes all elements that match the specified predicate from this
        /// list, invalidating the cached string form.
        /// </summary>
        /// <param name="match">
        /// The predicate that determines which elements to remove.
        /// </param>
        /// <returns>
        /// The number of elements removed.
        /// </returns>
        public new int RemoveAll(
            Predicate<string> match
            )
        {
            InvalidateCachedString(false);

            return base.RemoveAll(match); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Removes the specified range of elements from this list, invalidating
        /// the cached string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the first element to remove.
        /// </param>
        /// <param name="count">
        /// The number of elements to remove.
        /// </param>
        public new void RemoveRange(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.RemoveRange(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reverses the order of all elements in this list, invalidating the
        /// cached string form.
        /// </summary>
        public new void Reverse()
        {
            InvalidateCachedString(false);

            base.Reverse(); /* O(N) */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reverses the order of the specified range of elements in this list,
        /// invalidating the cached string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the first element in the range to reverse.
        /// </param>
        /// <param name="count">
        /// The number of elements in the range to reverse.
        /// </param>
        public new void Reverse(
            int index,
            int count
            )
        {
            InvalidateCachedString(false);

            base.Reverse(index, count); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the elements in this list using the default comparer,
        /// invalidating the cached string form.
        /// </summary>
        public new void Sort()
        {
            InvalidateCachedString(false);

            base.Sort();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the elements in this list using the specified comparison,
        /// invalidating the cached string form.
        /// </summary>
        /// <param name="comparison">
        /// The comparison used to order the elements.
        /// </param>
        public new void Sort(
            Comparison<string> comparison
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparison); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the elements in this list using the specified comparer,
        /// invalidating the cached string form.
        /// </summary>
        /// <param name="comparer">
        /// The comparer used to order the elements, or null to use the default
        /// comparer.
        /// </param>
        public new void Sort(
            IComparer<string> comparer
            )
        {
            InvalidateCachedString(false);

            base.Sort(comparer); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sorts the specified range of elements in this list using the
        /// specified comparer, invalidating the cached string form.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the first element in the range to sort.
        /// </param>
        /// <param name="count">
        /// The number of elements in the range to sort.
        /// </param>
        /// <param name="comparer">
        /// The comparer used to order the elements, or null to use the default
        /// comparer.
        /// </param>
        public new void Sort(
            int index,
            int count,
            IComparer<string> comparer)
        {
            InvalidateCachedString(false);

            base.Sort(index, count, comparer); /* throw */
        }
        #endregion
#endif
        #endregion
    }
}
