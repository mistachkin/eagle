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
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the Eagle string list type, which
    /// represents an ordered list of strings (optionally treated as key and
    /// value pairs) using Tcl list semantics.  It defines methods to add,
    /// query, and convert the list, including conversions to and from its
    /// string representation.
    /// </summary>
    [ObjectId("ce1b08f7-14d5-49ac-a89c-2aec29d80226")]
    public interface IStringList : ICloneable
    {
        /// <summary>
        /// Gets or sets the separator string used between elements when the
        /// list is converted to its string representation.
        /// </summary>
        string Separator { get; set; }

#if LIST_CACHE
        /// <summary>
        /// Gets or sets the key used to cache the string representation of
        /// this list.
        /// </summary>
        string CacheKey { get; set; }
#endif

        /// <summary>
        /// Gets the number of elements contained in the list.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Determines whether the list contains the specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate in the list.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The <see cref="StringComparison" /> value that controls how the key
        /// is compared.
        /// </param>
        /// <returns>
        /// True if the list contains the key; otherwise, false.
        /// </returns>
        bool ContainsKey(string key, StringComparison comparisonType);

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get.
        /// </param>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        string GetItem(int index);
        /// <summary>
        /// Gets the element at the specified index as a key and value pair.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to get.
        /// </param>
        /// <returns>
        /// The element at the specified index, as a key and value pair.
        /// </returns>
        IPair<string> GetPair(int index);

        /// <summary>
        /// Inserts an element into the list at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the element should be inserted.
        /// </param>
        /// <param name="item">
        /// The element to insert.  This parameter may be null.
        /// </param>
        void Insert(int index, string item);

        /// <summary>
        /// Adds the specified element to the end of the list.
        /// </summary>
        /// <param name="item">
        /// The element to add.  This parameter may be null.
        /// </param>
        void Add(string item);
        /// <summary>
        /// Adds the specified key and value, as two elements, to the end of
        /// the list.
        /// </summary>
        /// <param name="key">
        /// The key element to add.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value element to add.  This parameter may be null.
        /// </param>
        void Add(string key, string value);
        /// <summary>
        /// Adds the specified key and value, as two elements, to the end of
        /// the list, optionally normalizing the value and applying ellipsis.
        /// </summary>
        /// <param name="key">
        /// The key element to add.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value element to add.  This parameter may be null.
        /// </param>
        /// <param name="normalize">
        /// Non-zero to normalize whitespace within the value before adding it.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate the value with an ellipsis if it is too long.
        /// </param>
        void Add(string key, string value, bool normalize, bool ellipsis);
        /// <summary>
        /// Adds the specified key, followed by its associated values, to the
        /// end of the list.
        /// </summary>
        /// <param name="key">
        /// The key element to add.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The values to add following the key.  This parameter may be null.
        /// </param>
        void Add(string key, IEnumerable<string> value);
        /// <summary>
        /// Adds the string value of the specified string builder to the end of
        /// the list.
        /// </summary>
        /// <param name="item">
        /// The string builder whose value is to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(StringBuilder item);

        /// <summary>
        /// Adds the elements of the specified array, starting at the specified
        /// index, to the end of the list.
        /// </summary>
        /// <param name="array">
        /// The array whose elements are to be added.  This parameter may be
        /// null.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index in the array at which to begin adding elements.
        /// </param>
        void Add(string[] array, int startIndex);
        /// <summary>
        /// Adds the elements of the specified list, starting at the specified
        /// index, to the end of the list.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are to be added.  This parameter may be
        /// null.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index in the source list at which to begin adding
        /// elements.
        /// </param>
        void Add(IList list, int startIndex);
        /// <summary>
        /// Adds the elements of the specified string list, starting at the
        /// specified index, to the end of the list.
        /// </summary>
        /// <param name="list">
        /// The string list whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index in the source list at which to begin adding
        /// elements.
        /// </param>
        void Add(IStringList list, int startIndex);
        /// <summary>
        /// Adds the elements of the specified collection to the end of the
        /// list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(IEnumerable<string> collection);
        /// <summary>
        /// Adds the string values of the elements of the specified collection
        /// of string builders to the end of the list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(IEnumerable<StringBuilder> collection);
        /// <summary>
        /// Adds the keys and values of the specified dictionary, as pairs of
        /// elements, to the end of the list.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys and values are to be added.  This
        /// parameter may be null.
        /// </param>
        void Add(IDictionary<string, string> dictionary);
        /// <summary>
        /// Adds the string values of the elements of the specified collection
        /// of arguments to the end of the list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(IEnumerable<Argument> collection);
        /// <summary>
        /// Adds the string values of the elements of the specified collection
        /// of results to the end of the list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(IEnumerable<Result> collection);
        /// <summary>
        /// Adds the keys and values of the elements of the specified
        /// collection of pairs, as pairs of elements, to the end of the list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(IEnumerable<IPair<string>> collection);

        /// <summary>
        /// Adds the elements of the specified collection to the end of the
        /// list, transforming each element using the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The <see cref="StringTransformCallback" /> applied to each element
        /// before it is added.  This parameter may be null.
        /// </param>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(StringTransformCallback callback, IEnumerable<string> collection);
        /// <summary>
        /// Adds the string values of the elements of the specified collection
        /// of arguments to the end of the list, transforming each element
        /// using the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The <see cref="StringTransformCallback" /> applied to each element
        /// before it is added.  This parameter may be null.
        /// </param>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(StringTransformCallback callback, IEnumerable<Argument> collection);
        /// <summary>
        /// Adds the string values of the elements of the specified collection
        /// of results to the end of the list, transforming each element using
        /// the specified callback.
        /// </summary>
        /// <param name="callback">
        /// The <see cref="StringTransformCallback" /> applied to each element
        /// before it is added.  This parameter may be null.
        /// </param>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        void Add(StringTransformCallback callback, IEnumerable<Result> collection);

        /// <summary>
        /// Adds a null element to the end of the list if it does not already
        /// end with one.
        /// </summary>
        /// <returns>
        /// True if a null element was added; otherwise, false.
        /// </returns>
        bool MaybeAddNull();
        /// <summary>
        /// Appends null elements to the list until it contains at least the
        /// specified number of elements.
        /// </summary>
        /// <param name="count">
        /// The minimum number of elements the list should contain.
        /// </param>
        /// <returns>
        /// True if one or more null elements were added; otherwise, false.
        /// </returns>
        bool MaybeFillWithNull(int count);

        /// <summary>
        /// Adds the elements of the specified collection to the end of the
        /// list, if the collection is not null.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The number of elements that were added.
        /// </returns>
        int MaybeAddRange(IEnumerable<string> collection);
        /// <summary>
        /// Adds the keys and values of the elements of the specified
        /// collection of pairs to the end of the list, if the collection is
        /// not null.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are to be added.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The number of elements that were added.
        /// </returns>
        int MaybeAddRange(IEnumerable<IPair<string>> collection);

        /// <summary>
        /// Adds the specified key, followed by the raw string representation of
        /// the specified value, to the end of the list.
        /// </summary>
        /// <param name="key">
        /// The key element to add.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The string list whose raw string representation is to be added.
        /// This parameter may be null.
        /// </param>
        /// <param name="separator">
        /// The separator string to use when building the raw string
        /// representation of the value.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the key and value were added; otherwise, false.
        /// </returns>
        bool MaybeAddRawString(string key, IStringList value, string separator);

        /// <summary>
        /// Converts the list to its string representation.
        /// </summary>
        /// <param name="empty">
        /// Non-zero to include empty elements in the resulting string.
        /// </param>
        /// <returns>
        /// The string representation of the list.
        /// </returns>
        string ToString(bool empty);
        /// <summary>
        /// Converts the elements of the list that match the specified pattern
        /// to a string representation.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which elements are included, or null to
        /// include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the matching elements.
        /// </returns>
        string ToString(string pattern, bool noCase);
        /// <summary>
        /// Converts the elements of the list that match the specified pattern
        /// to a string representation.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which elements are included, or null to
        /// include all elements.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include empty elements in the resulting string.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the matching elements.
        /// </returns>
        string ToString(string pattern, bool empty, bool noCase);
        /// <summary>
        /// Converts the elements of the list that match the specified pattern
        /// to a string representation, using the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The separator string to place between elements, or null to use the
        /// default separator.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which elements are included, or null to
        /// include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the matching elements.
        /// </returns>
        string ToString(string separator, string pattern, bool noCase);
        /// <summary>
        /// Converts the elements of the list that match the specified pattern
        /// to a string representation, using the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The separator string to place between elements, or null to use the
        /// default separator.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which elements are included, or null to
        /// include all elements.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include empty elements in the resulting string.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the matching elements.
        /// </returns>
        string ToString(string separator, string pattern, bool empty, bool noCase);

        /// <summary>
        /// Converts the list to a raw string, without applying any list
        /// element quoting.
        /// </summary>
        /// <returns>
        /// The raw string representation of the list.
        /// </returns>
        string ToRawString();
        /// <summary>
        /// Converts the list to a raw string, without applying any list
        /// element quoting, using the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The separator string to place between elements, or null to use the
        /// default separator.
        /// </param>
        /// <returns>
        /// The raw string representation of the list.
        /// </returns>
        string ToRawString(string separator);
        /// <summary>
        /// Converts the list to a raw string, without applying any list
        /// element quoting, using the specified pair of separators.
        /// </summary>
        /// <param name="separator1">
        /// The separator string to place between the key and value of a pair.
        /// This parameter may be null.
        /// </param>
        /// <param name="separator2">
        /// The separator string to place between elements.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The raw string representation of the list.
        /// </returns>
        string ToRawString(string separator1, string separator2);

        /// <summary>
        /// Returns a copy of this list as a new string list.
        /// </summary>
        /// <returns>
        /// A new string list containing the elements of this list.
        /// </returns>
        IStringList ToList();
        /// <summary>
        /// Returns a new string list containing the elements of this list that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which elements are included, or null to
        /// include all elements.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A new string list containing the matching elements.
        /// </returns>
        IStringList ToList(string pattern, bool noCase);
        /// <summary>
        /// Returns a new string list containing the elements of this list that
        /// match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which elements are included, or null to
        /// include all elements.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include empty elements in the resulting list.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A new string list containing the matching elements.
        /// </returns>
        IStringList ToList(string pattern, bool empty, bool noCase);
    }
}
