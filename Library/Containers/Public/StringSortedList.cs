/*
 * StringSortedList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// This class represents a sorted list that maps string keys to string
    /// values, ordered by key.  It extends the generic
    /// <see cref="SortedList{TKey, TValue}" /> with string-specific keys and
    /// values and adds convenience constructors and methods for populating the
    /// list from various Eagle collection types, extracting sub-ranges, and
    /// formatting the keys as a Tcl-style list string.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c574dd78-aa93-4496-9269-30a7f9f3652b")]
    public sealed class StringSortedList : SortedList<string, string>
    {
        /// <summary>
        /// Constructs an empty sorted list using the default comparer for the
        /// string keys.
        /// </summary>
        public StringSortedList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty sorted list using the specified comparer to
        /// order the string keys.
        /// </summary>
        /// <param name="comparer">
        /// The comparer to use when ordering the keys, or null to use the
        /// default comparer.
        /// </param>
        public StringSortedList(
            IComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list that contains the key/value pairs copied
        /// from the specified dictionary, using the default comparer for the
        /// string keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new sorted
        /// list.
        /// </param>
        public StringSortedList(
            IDictionary<string, string> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list that contains the key/value pairs copied
        /// from the specified dictionary, using the specified comparer to order
        /// the string keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new sorted
        /// list.
        /// </param>
        /// <param name="comparer">
        /// The comparer to use when ordering the keys, or null to use the
        /// default comparer.
        /// </param>
        public StringSortedList(
            IDictionary<string, string> dictionary,
            IComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list whose keys are the single-character strings
        /// for each character in the specified collection.  Each key is added
        /// with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters used to populate the keys of the new
        /// sorted list.
        /// </param>
        public StringSortedList(
            IEnumerable<char> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list whose keys are the keys of the specified
        /// collection of sub-command key/value pairs.  Each key is added with a
        /// null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of sub-command key/value pairs whose keys are used to
        /// populate the new sorted list.
        /// </param>
        public StringSortedList(
            IEnumerable<KeyValuePair<string, ISubCommand>> collection
            )
            : this()
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list whose keys are the strings in the specified
        /// collection.  Each key is added with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings used to populate the keys of the new
        /// sorted list.
        /// </param>
        public StringSortedList(
            IEnumerable<string> collection
            )
            : this()
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list whose keys are the string representations of
        /// the arguments in the specified collection.  Each key is added with a
        /// null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments used to populate the keys of the new
        /// sorted list.
        /// </param>
        public StringSortedList(
            IEnumerable<Argument> collection
            )
            : this()
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a sorted list whose keys are the string representations of
        /// the elements of the specified list, starting at the specified index.
        /// Each key is added with a null value.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are used to populate the keys of the new
        /// sorted list.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element of <paramref name="list" /> to add.
        /// </param>
        public StringSortedList(
            IList list,
            int startIndex
            )
            : this()
        {
            Add(list, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the single-character string for each character in
        /// the specified collection to this sorted list as a key, each with a
        /// null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters whose single-character strings are
        /// added as keys.
        /// </param>
        public void Add(
            IEnumerable<char> collection
            )
        {
            foreach (char item in collection)
                this.Add(item.ToString(), null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the strings in the specified collection to this
        /// sorted list as keys, each with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to add as keys.
        /// </param>
        /// <param name="strict">
        /// Non-zero to add every element, including null elements; zero to skip
        /// null elements.
        /// </param>
        public void Add(
            IEnumerable<string> collection,
            bool strict
            )
        {
            foreach (string item in collection)
            {
                if (!strict && (item == null))
                    continue;

                this.Add(item, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the keys of the specified collection of sub-command
        /// key/value pairs to this sorted list as keys, each with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of sub-command key/value pairs whose keys are added.
        /// </param>
        /// <param name="strict">
        /// Non-zero to add the key of every pair, including pairs with a null
        /// key; zero to skip pairs with a null key.
        /// </param>
        public void Add(
            IEnumerable<KeyValuePair<string, ISubCommand>> collection,
            bool strict
            )
        {
            foreach (KeyValuePair<string, ISubCommand> item in collection)
            {
                if (!strict && (item.Key == null))
                    continue;

                this.Add(item.Key, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the string representations of the arguments in the
        /// specified collection to this sorted list as keys, each with a null
        /// value.
        /// </summary>
        /// <param name="collection">
        /// The collection of arguments to add as keys.
        /// </param>
        /// <param name="strict">
        /// Non-zero to add every element, including null elements; zero to skip
        /// null elements.
        /// </param>
        public void Add(
            IEnumerable<Argument> collection,
            bool strict
            )
        {
            foreach (Argument item in collection)
            {
                if (!strict && (item == null))
                    continue;

                this.Add(item, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the string representations of the elements of the
        /// specified list, starting at the specified index, to this sorted list
        /// as keys, each with a null value.  Null elements are skipped.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are added as keys.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element of <paramref name="list" /> to add.
        /// </param>
        public void Add(
            IList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                if (list[index] == null)
                    continue;

                this.Add(list[index].ToString(), null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new sorted list whose keys are the string
        /// representations of the elements of the specified list, from the
        /// specified first index through the end of the list.  Null elements
        /// are skipped.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are used to populate the new sorted list, or
        /// null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element of <paramref name="list" /> to
        /// include.
        /// </param>
        /// <returns>
        /// The new sorted list, or null if <paramref name="list" /> is null.
        /// </returns>
        public static StringSortedList GetRange(
            IList list,
            int firstIndex
            )
        {
            return GetRange(list, firstIndex,
                (list != null) ? (list.Count - 1) : Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new sorted list whose keys are the string
        /// representations of the elements of the specified list, from the
        /// specified first index through the specified last index, inclusive.
        /// Null elements are skipped.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are used to populate the new sorted list, or
        /// null.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first element of <paramref name="list" /> to
        /// include.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last element of <paramref name="list" /> to include.
        /// </param>
        /// <returns>
        /// The new sorted list, or null if <paramref name="list" /> is null.
        /// </returns>
        public static StringSortedList GetRange(
            IList list,
            int firstIndex,
            int lastIndex
            )
        {
            StringSortedList range = null;

            if (list != null)
            {
                range = new StringSortedList();

                for (int index = firstIndex; index <= lastIndex; index++)
                {
                    if (list[index] == null)
                        continue;

                    range.Add(list[index].ToString(), null);
                }
            }

            return range;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the keys of this sorted list as a Tcl-style list
        /// string, optionally including only the keys that match the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included, or null
        /// to include every key.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching; zero to
        /// perform case-sensitive pattern matching.
        /// </param>
        /// <returns>
        /// The keys of this sorted list formatted as a Tcl-style list string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<string>.ListToString(
                this.Keys, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method formats the keys of this sorted list as a Tcl-style list
        /// string, including every key.
        /// </summary>
        /// <returns>
        /// The keys of this sorted list formatted as a Tcl-style list string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
