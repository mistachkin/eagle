/*
 * ListOps.cs --
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
using System.Globalization;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using _StringDictionary = Eagle._Containers.Public.StringDictionary;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for manipulating lists and
    /// related collections, including converting between collection types,
    /// concatenating elements, computing lengths, selecting sub-list elements,
    /// finding and counting duplicates, combining and flattening lists, and
    /// generating permutations.
    /// </summary>
    [ObjectId("41713d5d-1147-4395-9863-92e45a9f28dc")]
    internal static class ListOps
    {
        #region Private Data
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When positive, the list operations may return an existing list, or a
        /// copy of one, instead of always splitting a value into a new list.
        /// </summary>
        private static int canGetOrCopyList = 1; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of times a value was converted to a list directly from a
        /// dictionary.
        /// </summary>
        private static int toDictionaryCount;
        /// <summary>
        /// The number of times an existing list was returned without copying.
        /// </summary>
        private static int getListCount;
        /// <summary>
        /// The number of times a new list was created by copying a collection.
        /// </summary>
        private static int copyListCount;
        /// <summary>
        /// The number of times a value that was not a collection was encountered.
        /// </summary>
        private static int nonCollectionCount;
        /// <summary>
        /// The number of times a null or string value was encountered.
        /// </summary>
        private static int nullOrStringCount;
        /// <summary>
        /// The number of times the get-or-copy optimization was skipped.
        /// </summary>
        private static int skipListCount;
        /// <summary>
        /// The number of times a value was split into a new list.
        /// </summary>
        private static int splitListCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildInterpreterInfoList method.
        //
        /// <summary>
        /// This method appends introspection information describing the list
        /// operation counters to the specified list, honoring the supplied detail
        /// flags.
        /// </summary>
        /// <param name="list">
        /// The list to append the information to.  This parameter may be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling the level of detail to include.
        /// </param>
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();
            int value; /* REUSED */

            value = Interlocked.CompareExchange(ref canGetOrCopyList, 0, 0);

            if (empty || (value != 0))
            {
                localList.Add("CanGetOrCopyList",
                    canGetOrCopyList > 0 ? "enabled" : "disabled");
            }

            value = Interlocked.CompareExchange(ref toDictionaryCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("ToDictionaryCount", value.ToString());

            value = Interlocked.CompareExchange(ref getListCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("GetListCount", value.ToString());

            value = Interlocked.CompareExchange(ref copyListCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("CopyListCount", value.ToString());

            value = Interlocked.CompareExchange(ref nonCollectionCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("NonCollectionCount", value.ToString());

            value = Interlocked.CompareExchange(ref nullOrStringCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("NullOrStringCount", value.ToString());

            value = Interlocked.CompareExchange(ref skipListCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("SkipListCount", value.ToString());

            value = Interlocked.CompareExchange(ref splitListCount, 0, 0);

            if (empty || (value != 0))
                localList.Add("SplitListCount", value.ToString());

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("List Operations");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a new list containing the elements of the specified
        /// collection in reverse order.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are reversed.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A new list with the elements in reverse order, or null when the
        /// collection is null.
        /// </returns>
        public static StringList Reverse(
            IEnumerable<string> collection
            )
        {
            if (collection == null)
                return null;

            StringList result = new StringList(
                collection);

            result.Reverse(); /* O(N) */

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified element to the specified list, creating
        /// the list first when necessary.
        /// </summary>
        /// <param name="element">
        /// The element to add.
        /// </param>
        /// <param name="list">
        /// The list to add to.  When null, a new list is created and stored here.
        /// </param>
        public static void Add(
            int element,
            ref IntList list
            )
        {
            if (list == null)
                list = new IntList();

            list.Add(element);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified adjustment to every element of the
        /// specified list.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are adjusted.  This parameter may be null.
        /// </param>
        /// <param name="adjustment">
        /// The amount to add to each element.
        /// </param>
        public static void Adjust(
            IntList list,
            int adjustment
            )
        {
            Adjust(list, adjustment, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified adjustment to every element of the
        /// specified list, clamping each result to the optional minimum and maximum
        /// bounds.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are adjusted.  This parameter may be null.
        /// </param>
        /// <param name="adjustment">
        /// The amount to add to each element.
        /// </param>
        /// <param name="minimum">
        /// The minimum allowed value for an element, or null for no minimum.
        /// </param>
        /// <param name="maximum">
        /// The maximum allowed value for an element, or null for no maximum.
        /// </param>
        public static void Adjust(
            IntList list,
            int adjustment,
            int? minimum,
            int? maximum
            )
        {
            if (list != null)
            {
                int count = list.Count;

                for (int index = 0; index < count; index++)
                {
                    int value = list[index];

                    value += adjustment;

                    if ((minimum != null) &&
                        (value < (int)minimum))
                    {
                        value = (int)minimum;
                    }

                    if ((maximum != null) &&
                        (value > (int)maximum))
                    {
                        value = (int)maximum;
                    }

                    list[index] = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates and normalizes the specified start and stop
        /// indexes against the given bounds.
        /// </summary>
        /// <param name="lowerBound">
        /// The lowest valid index.
        /// </param>
        /// <param name="upperBound">
        /// The highest valid index.
        /// </param>
        /// <param name="startIndex">
        /// The start index to validate.  A negative value is replaced with the
        /// lower bound.
        /// </param>
        /// <param name="stopIndex">
        /// The stop index to validate.  A negative value is replaced with the upper
        /// bound.
        /// </param>
        /// <returns>
        /// True if the resulting indexes are valid and in order; otherwise, false.
        /// </returns>
        public static bool CheckStartAndStopIndex(
            int lowerBound,
            int upperBound,
            ref int startIndex,
            ref int stopIndex
            )
        {
            Result error = null;

            return CheckStartAndStopIndex(lowerBound, upperBound,
                ref startIndex, ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates and normalizes the specified start and stop
        /// indexes against the given bounds.
        /// </summary>
        /// <param name="lowerBound">
        /// The lowest valid index.
        /// </param>
        /// <param name="upperBound">
        /// The highest valid index.
        /// </param>
        /// <param name="startIndex">
        /// The start index to validate.  A negative value is replaced with the
        /// lower bound.
        /// </param>
        /// <param name="stopIndex">
        /// The stop index to validate.  A negative value is replaced with the upper
        /// bound.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the resulting indexes are valid and in order; otherwise, false.
        /// </returns>
        public static bool CheckStartAndStopIndex(
            int lowerBound,
            int upperBound,
            ref int startIndex,
            ref int stopIndex,
            ref Result error
            )
        {
            bool result = false;

            if (startIndex < 0)
                startIndex = lowerBound;

            if (stopIndex < 0)
                stopIndex = upperBound;

            if ((startIndex >= lowerBound) && (startIndex <= upperBound))
            {
                if ((stopIndex >= lowerBound) && (stopIndex <= upperBound))
                {
                    if (startIndex <= stopIndex)
                    {
                        result = true;
                    }
                    else
                    {
                        error = "start index is greater than stop index";
                    }
                }
                else
                {
                    error = "stop index is out of bounds";
                }
            }
            else
            {
                error = "start index is out of bounds";
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified dictionary into a flat list of
        /// alternating keys and values.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A flat list of alternating keys and values, or null when the dictionary
        /// is null.
        /// </returns>
        private static StringList ToList(
            IDictionary dictionary
            )
        {
            if (dictionary == null)
                return null;

            StringList list = new StringList();

            foreach (DictionaryEntry entry in dictionary)
            {
                list.Add(StringOps.GetStringFromObject(entry.Key));
                list.Add(StringOps.GetStringFromObject(entry.Value));
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a list from the specified value, either by returning
        /// an existing list, copying a collection, or splitting a string into a
        /// list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when splitting a string into a list.  This
        /// parameter may be null.
        /// </param>
        /// <param name="getValue">
        /// The value container to obtain a list from.  This parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the caller guarantees it will only read from the returned
        /// list, permitting an existing list to be returned without copying.
        /// </param>
        /// <param name="list">
        /// Upon success, this is set to the resulting list.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetOrCopyOrSplitList(
            Interpreter interpreter,
            IGetValue getValue,
            bool readOnly,
            ref StringList list
            )
        {
            Result error = null;

            return GetOrCopyOrSplitList(
                interpreter, getValue, readOnly, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a list from the specified value, either by returning
        /// an existing list, copying a collection, or splitting a string into a
        /// list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when splitting a string into a list.  This
        /// parameter may be null.
        /// </param>
        /// <param name="getValue">
        /// The value container to obtain a list from.  This parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the caller guarantees it will only read from the returned
        /// list, permitting an existing list to be returned without copying.
        /// </param>
        /// <param name="list">
        /// Upon success, this is set to the resulting list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetOrCopyOrSplitList(
            Interpreter interpreter,
            IGetValue getValue,
            bool readOnly,
            ref StringList list,
            ref Result error
            )
        {
            if (getValue == null)
            {
                error = "cannot split null value into list";
                return ReturnCode.Error;
            }

            if (Interlocked.CompareExchange(ref canGetOrCopyList, 0, 0) > 0)
            {
                object value = getValue.Value;

                if ((value != null) && !(value is string))
                {
                    IEnumerable collection = value as IEnumerable;

                    if (collection != null)
                    {
                        IViaScript viaScript = value as IViaScript;

                        if ((viaScript != null) && viaScript.IsViaScript)
                        {
                            IDictionary dictionary = value as IDictionary;

                            if (dictionary != null)
                            {
                                list = ToList(dictionary); /* DEEP-COPY */

                                /* IGNORED */
                                Interlocked.Increment(ref toDictionaryCount);

                                return ReturnCode.Ok;
                            }
                        }

                        //
                        // NOTE: If the caller can guarantee that it will
                        //       only read from the returned list, we can
                        //       return it verbatim (i.e. if it's already
                        //       a StringList); otherwise, create a brand
                        //       new list, using the specified collection
                        //       of objects.
                        //
                        if (readOnly && (collection is StringList))
                        {
                            list = (StringList)collection;

                            /* IGNORED */
                            Interlocked.Increment(ref getListCount);
                        }
                        else
                        {
                            list = new StringList(collection);

                            /* IGNORED */
                            Interlocked.Increment(ref copyListCount);
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        /* IGNORED */
                        Interlocked.Increment(ref nonCollectionCount);
                    }
                }
                else
                {
                    /* IGNORED */
                    Interlocked.Increment(ref nullOrStringCount);
                }
            }
            else
            {
                /* IGNORED */
                Interlocked.Increment(ref skipListCount);
            }

            /* IGNORED */
            Interlocked.Increment(ref splitListCount);

            return ParserOps<string>.SplitList(
                interpreter, getValue.String, 0, Length.Invalid,
                readOnly, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates the specified strings into a single string,
        /// separated by spaces.
        /// </summary>
        /// <param name="strings">
        /// The strings to concatenate.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The concatenated string, or an empty string when no strings are
        /// supplied.
        /// </returns>
        public static string Concat(params string[] strings)
        {
            return (strings != null) ? Concat(new StringList(strings)) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates the elements of the specified list into a
        /// single string, separated by spaces.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are concatenated.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The concatenated string, or an empty string when the list is null.
        /// </returns>
        public static string Concat(IList list)
        {
            return Concat(list, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates the elements of the specified list, beginning
        /// at the specified start index, into a single string separated by spaces.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are concatenated.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element to include.
        /// </param>
        /// <returns>
        /// The concatenated string, or an empty string when the list is null.
        /// </returns>
        public static string Concat(IList list, int startIndex)
        {
            return (list != null) ? Concat(list, startIndex, list.Count - 1) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates the elements of the specified list, within the
        /// specified range of indexes, into a single string separated by spaces.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are concatenated.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element to include.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last element to include.
        /// </param>
        /// <returns>
        /// The concatenated string, or an empty string when the list is null.
        /// </returns>
        public static string Concat(IList list, int startIndex, int stopIndex)
        {
            return Concat(list, startIndex, stopIndex, Characters.SpaceString);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates the elements of the specified list, beginning
        /// at the specified start index, into a single string separated by the
        /// specified separator.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are concatenated.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element to include.
        /// </param>
        /// <param name="separator">
        /// The separator placed between elements.  When null, a single space is
        /// used.
        /// </param>
        /// <returns>
        /// The concatenated string, or an empty string when the list is null.
        /// </returns>
        public static string Concat(IList list, int startIndex, string separator)
        {
            return (list != null) ? Concat(list, startIndex, list.Count - 1,
                (separator != null) ? separator : Characters.SpaceString) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method concatenates the elements of the specified list, within the
        /// specified range of indexes, into a single string separated by the
        /// specified separator, trimming surrounding white-space from each element
        /// and skipping empty ones.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are concatenated.  This parameter may be null.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element to include.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last element to include.
        /// </param>
        /// <param name="separator">
        /// The separator placed between elements.
        /// </param>
        /// <returns>
        /// The concatenated string.
        /// </returns>
        public static string Concat(IList list, int startIndex, int stopIndex, string separator)
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (list != null)
            {
                if (CheckStartAndStopIndex(
                        0, list.Count - 1, ref startIndex, ref stopIndex))
                {
                    //
                    // NOTE: This function joins each of its arguments together
                    //       with spaces after trimming leading and trailing
                    //       white-space from each of them. If all the arguments
                    //       are lists, this has the same effect as concatenating
                    //       them into a single list. It permits any number of
                    //       arguments; if no args are supplied, the result is an
                    //       empty string.
                    //
                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        object element = list[index];

                        if (element == null)
                            continue;

                        string value = element.ToString();

                        if (String.IsNullOrEmpty(value))
                            continue;

                        value = value.Trim();

                        if (String.IsNullOrEmpty(value))
                            continue;

                        if (result.Length > 0)
                            result.Append(separator);

                        result.Append(value);
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified item to its string representation,
        /// using the specified format when the item supports it.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the item to convert.
        /// </typeparam>
        /// <param name="format">
        /// The format to use when the item supports formatted conversion.
        /// </param>
        /// <param name="item">
        /// The item to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the item, or null when the item is null.
        /// </returns>
        private static string ToString<T>(
            string format,
            T item
            )
        {
            if (item == null)
                return null;

            IToString toString = item as IToString;

            return (toString != null) ?
                toString.ToString(format) : item.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the combined length of the string representations
        /// of the elements of the specified list, beginning at the specified start
        /// index and counting only those that meet the specified minimum length.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list.
        /// </typeparam>
        /// <param name="list">
        /// The list whose elements are measured.  This parameter may be null.
        /// </param>
        /// <param name="format">
        /// The format used to convert each element to a string.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element to measure.
        /// </param>
        /// <param name="minimum">
        /// The minimum length an element must have to be counted.
        /// </param>
        /// <returns>
        /// The combined length of the qualifying element string representations.
        /// </returns>
        public static int GetTotalLength<T>(
            IList<T> list,
            string format,
            int startIndex,
            int minimum
            )
        {
            int result = 0;

            if (list != null)
            {
                int count = list.Count;

                for (int index = startIndex; index < count; index++)
                {
                    T item = list[index];

                    if (item == null)
                        continue;

                    string itemString = ToString<T>(format, item);

                    if (itemString == null)
                        continue;

                    int length = itemString.Length;

                    if (length < minimum)
                        continue;

                    result += length;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the maximum length among the string representations
        /// of the elements of the specified collection.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collection.
        /// </typeparam>
        /// <param name="collection">
        /// The collection whose elements are measured.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The maximum element string length, or an invalid length when the
        /// collection is null or empty.
        /// </returns>
        public static int GetMaximumLength<T>(
            IEnumerable<T> collection
            )
        {
            return GetMaximumLength<T>(collection, "{0}");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the maximum length among the string representations
        /// of the elements of the specified collection, using the specified
        /// format.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collection.
        /// </typeparam>
        /// <param name="collection">
        /// The collection whose elements are measured.  This parameter may be
        /// null.
        /// </param>
        /// <param name="format">
        /// The format used to convert each element to a string.
        /// </param>
        /// <returns>
        /// The maximum element string length, or an invalid length when the
        /// collection is null or empty.
        /// </returns>
        private static int GetMaximumLength<T>(
            IEnumerable<T> collection,
            string format
            )
        {
            int maximum = Length.Invalid;

            if (collection != null)
            {
                foreach (T item in collection)
                {
                    if (item == null)
                        continue;

                    string itemString = ToString<T>(
                        format, item);

                    if (itemString == null)
                        continue;

                    int length = itemString.Length;

                    if ((maximum == Length.Invalid) ||
                        (length > maximum))
                    {
                        maximum = length;
                    }
                }
            }

            return maximum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the maximum length among the string representations
        /// of the elements of the specified list, capped at the specified limit.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are measured.  This parameter may be null.
        /// </param>
        /// <param name="format">
        /// The format used to convert each element to a string.
        /// </param>
        /// <param name="limit">
        /// The maximum length to return, or an invalid length for no cap.
        /// </param>
        /// <returns>
        /// The maximum element string length, capped at the limit, or an invalid
        /// length when the list is null or empty.
        /// </returns>
        public static int GetMaximumLength(
            IList list,
            string format,
            int limit
            )
        {
            int maximum = Length.Invalid;

            if (list != null)
            {
                foreach (object element in list)
                {
                    if (element == null)
                        continue;

                    string value = ToString<object>(
                        format, element);

                    if (value == null)
                        continue;

                    int length = value.Length;

                    if ((maximum == Length.Invalid) ||
                        (length > maximum))
                    {
                        maximum = length;
                    }
                }

                //
                // NOTE: Reduce to the maximum limit
                //       allowed by the caller.
                //
                if ((maximum != Length.Invalid) &&
                    (limit != Length.Invalid) &&
                    (maximum > limit))
                {
                    maximum = limit;
                }
            }

            return maximum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method walks the specified text as a nested list, following the
        /// chain of indexes described by the index text, and returns the indexes
        /// that were resolved.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to split the lists.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The text to interpret as a (possibly nested) list.
        /// </param>
        /// <param name="indexText">
        /// The list of indexes to follow into the nested list.
        /// </param>
        /// <param name="clear">
        /// Non-zero to replace the supplied index list rather than append to it.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the indexes.
        /// </param>
        /// <param name="indexList">
        /// Upon success, this contains the resolved indexes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SelectFromSubList(
            Interpreter interpreter,
            string text,
            string indexText,
            bool clear,
            CultureInfo cultureInfo,
            ref IntList indexList,
            ref Result error
            )
        {
            string value = null;

            return SelectFromSubList(interpreter, text, indexText, clear, cultureInfo,
                ref value, ref indexList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method walks the specified text as a nested list, following the
        /// chain of indexes described by the index text, and returns the selected
        /// element value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to split the lists.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The text to interpret as a (possibly nested) list.
        /// </param>
        /// <param name="indexText">
        /// The list of indexes to follow into the nested list.
        /// </param>
        /// <param name="clear">
        /// Non-zero to replace the supplied index list rather than append to it.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the indexes.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the selected element value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SelectFromSubList(
            Interpreter interpreter,
            string text,
            string indexText,
            bool clear,
            CultureInfo cultureInfo,
            ref string value,
            ref Result error
            )
        {
            IntList indexList = null;

            return SelectFromSubList(interpreter, text, indexText, clear, cultureInfo,
                ref value, ref indexList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method walks the specified text as a nested list, following the
        /// chain of indexes described by the index text, returning both the
        /// selected element value and the indexes that were resolved.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to split the lists.  This parameter may be
        /// null.
        /// </param>
        /// <param name="text">
        /// The text to interpret as a (possibly nested) list.
        /// </param>
        /// <param name="indexText">
        /// The list of indexes to follow into the nested list.
        /// </param>
        /// <param name="clear">
        /// Non-zero to replace the supplied index list rather than append to it.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the indexes.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the selected element value.
        /// </param>
        /// <param name="indexList">
        /// Upon success, this contains the resolved indexes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode SelectFromSubList(
            Interpreter interpreter,
            string text,
            string indexText,
            bool clear,
            CultureInfo cultureInfo,
            ref string value,
            ref IntList indexList,
            ref Result error
            )
        {
            ReturnCode code;

            if (!String.IsNullOrEmpty(indexText))
            {
                StringList indexTextList = null;

                code = ParserOps<string>.SplitList(
                    interpreter, indexText, 0, Length.Invalid, true,
                    ref indexTextList, ref error);

                if (code == ReturnCode.Ok)
                {
                    if (indexTextList.Count > 0)
                    {
                        StringList list = null;

                        code = ParserOps<string>.SplitList(
                            interpreter, text, 0, Length.Invalid, true,
                            ref list, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            string localValue = null;
                            IntList localIndexList = new IntList();

                            for (int index = 0; index < indexTextList.Count; index++)
                            {
                                int listIndex = Index.Invalid;

                                code = Value.GetIndex(
                                    indexTextList[index], list.Count,
                                    ValueFlags.AnyIndex, cultureInfo,
                                    ref listIndex, ref error);

                                if (code != ReturnCode.Ok)
                                    break;

                                if ((listIndex < 0) ||
                                    (listIndex >= list.Count) ||
                                    (list[listIndex] == null))
                                {
                                    error = String.Format(
                                        "element {0} missing from sublist \"{1}\"",
                                        listIndex, list.ToString());

                                    code = ReturnCode.Error;
                                    break;
                                }

                                localValue = list[listIndex];
                                localIndexList.Add(listIndex);

                                StringList subList = null;

                                code = ParserOps<string>.SplitList(
                                    interpreter, list[listIndex], 0,
                                    Length.Invalid, true, ref subList,
                                    ref error);

                                if (code == ReturnCode.Ok)
                                    list = subList;
                                else
                                    break;
                            }

                            if (code == ReturnCode.Ok)
                            {
                                value = localValue;

                                if (clear || (indexList == null))
                                    indexList = localIndexList;
                                else
                                    indexList.AddRange(localIndexList);
                            }
                        }
                    }
                    else
                    {
                        value = text;

                        if (clear || (indexList == null))
                            indexList = new IntList();
                    }
                }
            }
            else
            {
                value = text;

                if (clear || (indexList == null))
                    indexList = new IntList();

                code = ReturnCode.Ok;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method prepares the two elements to be compared during a sort,
        /// optionally selecting sub-list elements via the specified index text and
        /// swapping them when the sort is descending and not pattern-based.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when selecting sub-list elements.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ascending">
        /// Non-zero if the sort is in ascending order.
        /// </param>
        /// <param name="indexText">
        /// The index text used to select sub-list elements, or null to compare the
        /// elements directly.
        /// </param>
        /// <param name="leftOnly">
        /// Non-zero to select a sub-list element only from the left element.
        /// </param>
        /// <param name="pattern">
        /// Non-zero if the comparison is pattern-based.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when selecting sub-list elements.
        /// </param>
        /// <param name="left">
        /// The left element to compare, updated in place.
        /// </param>
        /// <param name="right">
        /// The right element to compare, updated in place.
        /// </param>
        public static void GetElementsToCompare(
            Interpreter interpreter,
            bool ascending,
            string indexText,
            bool leftOnly,
            bool pattern,
            CultureInfo cultureInfo,
            ref string left,
            ref string right
            )
        {
            if (indexText != null)
            {
                string leftValue = null;
                Result error = null;

                if (SelectFromSubList(interpreter, left, indexText, false,
                        cultureInfo, ref leftValue, ref error) == ReturnCode.Ok)
                {
                    if (leftOnly)
                    {
                        left = leftValue;
                    }
                    else
                    {
                        string rightValue = null;

                        if (SelectFromSubList(interpreter, right, indexText, false,
                                cultureInfo, ref rightValue, ref error) == ReturnCode.Ok)
                        {
                            left = leftValue;
                            right = rightValue;
                        }
                    }
                }

                //
                // HACK: This is somewhat sub-optimal.  It relies upon the
                //       error message *ONLY* being set upon a failures of
                //       the SelectFromSubList method or any of its called
                //       methods.  Within the ParserOps class, a small bug
                //       of setting the error message based on the lack of
                //       a native utility library caused this condition to
                //       be triggered wrongly.
                //
                if (error != null)
                    throw new ScriptException(error);
            }

            if (!ascending && !pattern)
            {
                string swap = left;
                left = right;
                right = swap;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two values are equal, according to the
        /// specified comparer or the default comparer for the type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the values to compare.
        /// </typeparam>
        /// <param name="comparer">
        /// The comparer used to compare the values, or null to use the default
        /// comparer.
        /// </param>
        /// <param name="left">
        /// The first value to compare.
        /// </param>
        /// <param name="right">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// True if the two values are equal; otherwise, false.
        /// </returns>
        public static bool ComparerEquals<T>(
            IComparer<T> comparer,
            T left,
            T right
            )
        {
            if (comparer != null)
                return (comparer.Compare(left, right) == 0 /* EQUAL */);
            else
                return Comparer<T>.Default.Compare(left, right) == 0 /* EQUAL */;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash code for the specified value that is
        /// consistent with equality, optionally ignoring case for string values.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value.
        /// </typeparam>
        /// <param name="comparer">
        /// The comparer associated with the value.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to compute a hash code for.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to ignore case when the value is a string.
        /// </param>
        /// <returns>
        /// A hash code for the specified value.
        /// </returns>
        public static int ComparerGetHashCode<T>(
            IComparer<T> comparer,
            T value,
            bool noCase
            )
        {
            //
            // NOTE: The only thing that we must guarantee here,
            //       according to the MSDN documentation for
            //       IEqualityComparer, is that for two given
            //       strings, if Equals return true then the two
            //       strings must hash to the same value.
            //
            if (value == null)
                throw new ArgumentNullException("value");

            string stringValue = value as string;

            if (stringValue != null)
            {
                return noCase ?
                    stringValue.ToLower().GetHashCode() :
                    stringValue.GetHashCode();
            }
            else
            {
                return value.GetHashCode();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified list element should be
        /// skipped because it is null or empty, or because it has already been
        /// seen.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary tracking the elements seen so far.  This parameter may be
        /// null.
        /// </param>
        /// <param name="key">
        /// The element to test.
        /// </param>
        /// <returns>
        /// True if the element should be skipped; otherwise, false.
        /// </returns>
        private static bool ShouldSkipElement( /* O(1) */
            _StringDictionary dictionary, /* in */
            string key                    /* in */
            )
        {
            //
            // HACK: Any null / empty list element are
            //       always skipped (and never added).
            //
            if (String.IsNullOrEmpty(key))
                return true;

            //
            // NOTE: If this element was seen before,
            //       skip it now.
            //
            if ((dictionary != null) && dictionary.ContainsKey(key))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a new list containing the unique, non-empty elements
        /// of the specified list, preserving their original order.
        /// </summary>
        /// <param name="list">
        /// The list whose unique elements are returned.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A new list of the unique elements, or the original list when it is null
        /// or empty.
        /// </returns>
        public static StringList GetUniqueElements( /* O(N) */
            StringList list /* in */
            )
        {
            return GetUniqueElements(list, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a new list containing the unique elements of the
        /// specified list, using the specified callback to decide which elements
        /// are considered duplicates.
        /// </summary>
        /// <param name="list">
        /// The list whose unique elements are returned.  This parameter may be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The callback used to determine whether an element is a duplicate, or
        /// null to use the default behavior.
        /// </param>
        /// <returns>
        /// A new list of the unique elements, or the original list when it is null
        /// or empty.
        /// </returns>
        public static StringList GetUniqueElements( /* O(N) */
            StringList list,                      /* in */
            UniqueStringCallback<string> callback /* in */
            )
        {
            return (callback != null) ?
                GetUniqueElementsViaCallback(list, callback) :
                GetUniqueElementsViaDefault(list);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a new list containing the unique elements of the
        /// specified list, using the specified callback to decide which elements
        /// are considered duplicates.
        /// </summary>
        /// <param name="list">
        /// The list whose unique elements are returned.  This parameter may be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The callback used to determine whether an element is a duplicate.
        /// </param>
        /// <returns>
        /// A new list of the unique elements, or the original list when it is null,
        /// empty, or no callback is supplied.
        /// </returns>
        private static StringList GetUniqueElementsViaCallback( /* O(N) */
            StringList list,                      /* in */
            UniqueStringCallback<string> callback /* in */
            )
        {
            if ((list == null) || (list.Count == 0) || (callback == null))
                return list;

            StringList result = new StringList();
            _StringDictionary dictionary = new _StringDictionary();

            foreach (string element in list)
            {
                bool? contains = callback(list, dictionary, element);

                if (contains != null)
                {
                    if ((bool)contains)
                        continue;
                }
                else
                {
                    if (ShouldSkipElement(dictionary, element))
                        continue;
                }

                dictionary[element] = null;
                result.Add(element);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a new list containing the unique, non-empty elements
        /// of the specified list, using the default duplicate-detection behavior.
        /// </summary>
        /// <param name="list">
        /// The list whose unique elements are returned.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// A new list of the unique elements, or the original list when it is null
        /// or empty.
        /// </returns>
        private static StringList GetUniqueElementsViaDefault( /* O(N) */
            StringList list /* in */
            )
        {
            if ((list == null) || (list.Count == 0))
                return list;

            StringList result = new StringList();
            _StringDictionary dictionary = new _StringDictionary();

            foreach (string element in list)
            {
                if (ShouldSkipElement(dictionary, element))
                    continue;

                dictionary[element] = null;
                result.Add(element);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the recorded duplicate count for the specified
        /// value, locating it within the specified dictionary using a linear search
        /// driven by the specified comparer.
        /// </summary>
        /// <param name="comparer">
        /// The comparer used to match the value.  This parameter may be null.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary mapping values to their duplicate counts.  This parameter
        /// may be null.
        /// </param>
        /// <param name="value">
        /// The value whose duplicate count is returned.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The duplicate count for the value, or zero when it is not found.
        /// </returns>
        public static int GetDuplicateCount( /* O(N) */
            IComparer<string> comparer,
            IntDictionary duplicates,
            string value
            )
        {
            //
            // HACK: Since the ContainsKey method of the Dictionary object
            //       insists on using both the Equals and GetHashCode methods
            //       of the custom IEqualityComparer interface we provide
            //       to find the key, we must resort to a linear search
            //       because we cannot reasonably implement the GetHashCode
            //       method in terms of the Compare method in a semantically
            //       compatible way.
            //
            int result = 0;

            if ((comparer != null) && (duplicates != null) && (value != null))
            {
                foreach (string element in duplicates.Keys)
                {
                    if (comparer.Compare(element, value) == 0 /* EQUAL */)
                    {
                        //
                        // NOTE: Found the key value, get the count.
                        //
                        result = duplicates[element];
                        break;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the duplicate count for the specified value within
        /// the specified dictionary, adding the value when it is not already
        /// present.
        /// </summary>
        /// <param name="comparer">
        /// The comparer used to match the value.  This parameter may be null.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary mapping values to their duplicate counts.  This parameter
        /// may be null.
        /// </param>
        /// <param name="value">
        /// The value whose duplicate count is recorded.  This parameter may be
        /// null.
        /// </param>
        /// <param name="count">
        /// The duplicate count to record.
        /// </param>
        /// <returns>
        /// True if the count was recorded; otherwise, false.
        /// </returns>
        public static bool SetDuplicateCount( /* O(N) */
            IComparer<string> comparer,
            IntDictionary duplicates,
            string value,
            int count
            )
        {
            //
            // HACK: Since the ContainsKey method of the Dictionary object
            //       insists on using both the Equals and GetHashCode methods
            //       of the custom IEqualityComparer interface we provide
            //       to find the key, we must resort to a linear search
            //       because we cannot reasonably implement the GetHashCode
            //       method in terms of the Compare method in a semantically
            //       compatible way.
            //
            if ((comparer != null) && (duplicates != null) && (value != null))
            {
                foreach (string element in duplicates.Keys)
                {
                    if (comparer.Compare(element, value) == 0 /* EQUAL */)
                    {
                        //
                        // NOTE: Found the key value, set the count.
                        //
                        duplicates[element] = count;
                        return true;
                    }
                }

                //
                // NOTE: The value was not found in the dictionary,
                //       add it now.
                //
                duplicates.Add(value, count);
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the duplicate count for the left element when a
        /// unique sort encounters two equal but distinct elements, guarding against
        /// re-entrancy using the specified level counter.
        /// </summary>
        /// <param name="comparer">
        /// The comparer used to match elements.  This parameter may be null.
        /// </param>
        /// <param name="duplicates">
        /// The dictionary mapping elements to their duplicate counts.  This
        /// parameter may be null.
        /// </param>
        /// <param name="left">
        /// The left element being compared.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The right element being compared.  This parameter may be null.
        /// </param>
        /// <param name="unique">
        /// Non-zero if the sort is removing duplicate elements.
        /// </param>
        /// <param name="result">
        /// The result of comparing the two elements.
        /// </param>
        /// <param name="levels">
        /// The counter tracking the number of active duplicate-count updates, used
        /// to prevent re-entrant processing.
        /// </param>
        public static void UpdateDuplicateCount( /* 2 * O(N) */
            IComparer<string> comparer,
            IntDictionary duplicates,
            string left,
            string right,
            bool unique,
            int result,
            ref int levels
            ) /* throw */
        {
            if (unique && (result == 0 /* EQUAL */))
            {
                if ((duplicates != null) && (left != null) && (right != null))
                {
                    //
                    // NOTE: Skip instances where the sort algorithm is actually
                    //       having us compare the exact same string.
                    //
                    if (!Object.ReferenceEquals(left, right))
                    {
                        //
                        // NOTE: Only continue if we are not already processing
                        //       duplicate counts already.
                        //
                        if (Interlocked.Increment(ref levels) == 1)
                        {
                            try
                            {
                                //
                                // NOTE: Search for all the list elements that are duplicates
                                //       of the left element.  This is an O(N) operation in
                                //       the worst case (i.e. if every element in the list is
                                //       a duplicate of the provided left element).
                                //
                                int count = GetDuplicateCount(comparer, duplicates, left);

                                if (count != Count.Invalid)
                                    //
                                    // NOTE: Set the duplicate count of the first list element
                                    //       that is a duplicate of the provided left element.
                                    //       This is an O(N) operation in the worst case (i.e.
                                    //       if the last element in the list is the first
                                    //       duplicate of the provided left element).
                                    //
                                    if (!SetDuplicateCount(comparer, duplicates, left, ++count))
                                        throw new ScriptException(String.Format(
                                            "failed to update duplicate count for element \"{0}\"",
                                            left));
                            }
                            finally
                            {
                                //
                                // NOTE: Even if we are throwing an exception, we want
                                //       to keep the number of active levels at the
                                //       correct value.
                                //
                                Interlocked.Decrement(ref levels);
                            }
                        }
                        else
                        {
                            //
                            // NOTE: When we incremented the number of active levels it
                            //       resulted in a value higher than one; notwithstanding
                            //       that state of affairs, we still need to decremenet
                            //       the number of active levels because we did successfully
                            //       increment it.
                            //
                            Interlocked.Decrement(ref levels);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified name/value collection into a flat
        /// list of alternating names and values.
        /// </summary>
        /// <param name="collection">
        /// The name/value collection to convert.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The list whose elements seed the result, or null to start with an empty
        /// list.
        /// </param>
        /// <returns>
        /// A flat list of alternating names and values, or null when both the
        /// collection and the seed list are null.
        /// </returns>
        public static IList FromNameValueCollection(
            NameValueCollection collection,
            IList @default
            )
        {
            IList result = (@default != null) ?
                new StringList(@default) : null;

            if (collection != null)
            {
                if (result == null)
                    result = new StringList();

                int count = collection.Count;

                for (int index = 0; index < count; index++)
                {
                    result.Add(collection.GetKey(index));
                    result.Add(collection.Get(index));
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified flat list of alternating names and
        /// values into a name/value collection.
        /// </summary>
        /// <param name="list">
        /// The flat list of alternating names and values to convert.  This
        /// parameter may be null.
        /// </param>
        /// <param name="default">
        /// The collection to add to, or null to create a new collection.
        /// </param>
        /// <returns>
        /// The resulting name/value collection, which may be the supplied
        /// collection when no list is provided.
        /// </returns>
        public static NameValueCollection ToNameValueCollection(
            IList list,
            NameValueCollection @default
            )
        {
            NameValueCollection result = @default;

            if (list != null)
            {
                if (result == null)
                    result = new NameValueCollection();

                int count = list.Count;

                for (int index = 0; index < count; index += 2)
                {
                    object element = null;
                    string name = null;
                    string value = null;

                    element = list[index];

                    name = (element != null) ?
                        element.ToString() : null;

                    if ((index + 1) < count)
                    {
                        element = list[index + 1];

                        value = (element != null) ?
                            element.ToString() : null;
                    }

                    result.Add(name, value);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the cross-product combination of the specified
        /// lists of string builders, appending the result to the specified list.
        /// </summary>
        /// <param name="lists">
        /// The lists to combine.  This parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list to append the combined result to.  When null, a new list is
        /// created and stored here.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Combine(
            IList<IList<StringBuilder>> lists, /* in */
            ref IList<StringBuilder> list,     /* in, out */
            ref Result error                   /* out */
            )
        {
            if (lists == null)
            {
                error = "invalid list of lists";
                return ReturnCode.Error;
            }

            if (lists.Count == 0)
            {
                error = "no lists in list";
                return ReturnCode.Error;
            }

            IList<StringBuilder> list1 = lists[0];

            if (lists.Count > 1)
            {
                for (int index = 1; index < lists.Count; index++)
                {
                    IList<StringBuilder> list2 = lists[index];
                    IList<StringBuilder> list3 = null;

                    if (Combine(
                            list1, list2, ref list3, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    list1 = list3;
                }
            }

            if (list != null)
                GenericOps<StringBuilder>.AddRange(list, list1);
            else
                list = new List<StringBuilder>(list1);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the cross-product combination of two lists of
        /// string builders, appending each pairwise concatenation to the result
        /// list.
        /// </summary>
        /// <param name="list1">
        /// The first list to combine.  This parameter may be null.
        /// </param>
        /// <param name="list2">
        /// The second list to combine.  This parameter may be null.
        /// </param>
        /// <param name="list3">
        /// The list to append the combined result to.  When null, a new list is
        /// created and stored here.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode Combine(
            IList<StringBuilder> list1,     /* in */
            IList<StringBuilder> list2,     /* in */
            ref IList<StringBuilder> list3, /* in, out */
            ref Result error                /* out */
            )
        {
            if (list1 == null)
            {
                if (list2 == null)
                {
                    error = "cannot combine, neither list is valid";
                    return ReturnCode.Error;
                }

                if (list3 != null)
                    GenericOps<StringBuilder>.AddRange(list3, list2);
                else
                    list3 = new List<StringBuilder>(list2);
            }
            else if (list2 == null)
            {
                if (list3 != null)
                    GenericOps<StringBuilder>.AddRange(list3, list1);
                else
                    list3 = new List<StringBuilder>(list1);
            }
            else
            {
                if ((list1.Count > 0) || (list2.Count > 0))
                {
                    if (list3 == null)
                        list3 = new List<StringBuilder>();
                }

                foreach (StringBuilder element1 in list1)
                {
                    foreach (StringBuilder element2 in list2)
                    {
                        int capacity = 0;

                        if (element1 != null)
                            capacity += element1.Length;

                        if (element2 != null)
                            capacity += element2.Length;

                        StringBuilder element3 = StringBuilderFactory.CreateNoCache(
                            capacity); /* EXEMPT */

                        element3.Append(element1);
                        element3.Append(element2);

                        list3.Add(element3);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified list of string builders into a list
        /// of their string representations.
        /// </summary>
        /// <param name="list">
        /// The list of string builders to flatten.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A list of the string representations, or null when the list is null.
        /// </returns>
        public static StringList Flatten(
            IList<StringBuilder> list
            )
        {
            if (list == null)
                return null;

            StringList result = new StringList();

            foreach (StringBuilder element in list)
                result.Add(element);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally adds a copy of the specified permutation to
        /// the result, when the optional callback accepts it.
        /// </summary>
        /// <param name="callback">
        /// The callback used to decide whether to keep the permutation, or null to
        /// keep every permutation.
        /// </param>
        /// <param name="list">
        /// The permutation to consider.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The list of accepted permutations.  When null, a new list is created and
        /// stored here.
        /// </param>
        private static void HandlePermuteResult(
            ListTransformCallback callback, /* in */
            IList<string> list,             /* in */
            ref IList<IList<string>> result /* in, out */
            )
        {
            if (list == null)
                return;

            if ((callback == null) || callback(list))
            {
                if (result == null)
                    result = new List<IList<string>>();

                result.Add(new StringList(list)); /* COPY */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates all permutations of the elements of the specified
        /// list, optionally filtering them through the specified callback.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are permuted.  This parameter may be null.
        /// </param>
        /// <param name="callback">
        /// The callback used to filter the permutations, or null to keep every
        /// permutation.
        /// </param>
        /// <returns>
        /// A list containing the accepted permutations, or null when the list is
        /// null or no permutations are accepted.
        /// </returns>
        public static IList<IList<string>> Permute(
            IList<string> list,
            ListTransformCallback callback
            )
        {
            IList<IList<string>> result = null;

            if (list != null)
            {
                IList<string> localList = new StringList(list); /* COPY */

                HandlePermuteResult(callback, localList, ref result);

                int count = localList.Count;
                int[] indexes = new int[count + 1];
                int index1 = 1;

                while (index1 < count)
                {
                    if (indexes[index1] < index1)
                    {
                        int index2 = index1 % 2 * indexes[index1];
                        string temporary = localList[index2];

                        localList[index2] = localList[index1];
                        localList[index1] = temporary;

                        HandlePermuteResult(callback, localList, ref result);

                        indexes[index1]++;
                        index1 = 1;
                    }
                    else
                    {
                        indexes[index1] = 0;
                        index1++;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes an order-independent hash code for the specified
        /// collection by combining the hash codes of its elements.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collection.
        /// </typeparam>
        /// <param name="collection">
        /// The collection whose hash code is computed.  This parameter may be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The callback used to compute the hash code of each element, or null to
        /// use the element's own hash code.
        /// </param>
        /// <returns>
        /// An order-independent hash code for the collection.
        /// </returns>
        public static int IEnumerableHashCode<T>(
            IEnumerable<T> collection,
            GetHashCodeCallback<T> callback
            )
        {
            int result = 0;

            if (collection != null)
            {
                foreach (T item in collection)
                {
                    if (item == null)
                        continue;

                    if (callback != null)
                        result ^= callback(item);
                    else
                        result ^= item.GetHashCode();
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two collections contain equal elements in
        /// the same order, using the specified callback or the elements' own
        /// comparison.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collections.
        /// </typeparam>
        /// <param name="collection1">
        /// The first collection to compare.  This parameter may be null.
        /// </param>
        /// <param name="collection2">
        /// The second collection to compare.  This parameter may be null.
        /// </param>
        /// <param name="callback">
        /// The callback used to compare elements, or null to use each element's own
        /// comparison.
        /// </param>
        /// <returns>
        /// True if the collections contain equal elements in the same order;
        /// otherwise, false.
        /// </returns>
        public static bool IEnumerableEquals<T>(
            IEnumerable<T> collection1,
            IEnumerable<T> collection2,
            CompareCallback<T> callback
            )
        {
            if ((collection1 == null) || (collection2 == null))
                return ((collection1 == null) && (collection2 == null));

            if (Object.ReferenceEquals(collection1, collection2))
                return true;

            IEnumerator<T> enumerator1 = collection1.GetEnumerator();
            IEnumerator<T> enumerator2 = collection2.GetEnumerator();

            if ((enumerator1 == null) || (enumerator2 == null))
                return false;

            while (true)
            {
                bool moveNext1 = enumerator1.MoveNext();
                bool moveNext2 = enumerator2.MoveNext();

                if (!moveNext1 || !moveNext2)
                    return moveNext1 == moveNext2;

                if (callback != null)
                {
                    if (callback(
                            enumerator1.Current,
                            enumerator2.Current) != 0)
                    {
                        return false;
                    }
                }
                else
                {
                    IComparable<T> comparable1 =
                        enumerator1.Current as IComparable<T>;

                    if (comparable1 == null)
                        return false;

                    if (comparable1.CompareTo(
                            enumerator2.Current) != 0)
                    {
                        return false;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the specified collection when it is a writable list.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collection.
        /// </typeparam>
        /// <param name="collection">
        /// The collection to clear.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the collection was a writable list and was cleared; otherwise,
        /// false.
        /// </returns>
        public static bool IEnumerableClearList<T>(
            IEnumerable<T> collection
            )
        {
            if (collection == null)
                return false;

            IList<T> list = collection as IList<T>;

            if (list == null)
                return false;

            list.Clear();
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified item to the specified collection when it
        /// is a writable list.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collection.
        /// </typeparam>
        /// <param name="collection">
        /// The collection to add to.  This parameter may be null.
        /// </param>
        /// <param name="item">
        /// The item to add.
        /// </param>
        /// <returns>
        /// True if the collection was a writable list and the item was added;
        /// otherwise, false.
        /// </returns>
        public static bool IEnumerableAddToList<T>(
            IEnumerable<T> collection,
            T item
            )
        {
            if (collection == null)
                return false;

            IList<T> list = collection as IList<T>;

            if (list == null)
                return false;

            list.Add(item);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the elements of the second collection to the first
        /// collection when the first collection is a list.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collections.
        /// </typeparam>
        /// <param name="collection1">
        /// The collection to append to.  This parameter may be null.
        /// </param>
        /// <param name="collection2">
        /// The collection whose elements are appended.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the elements were appended; otherwise, false.
        /// </returns>
        public static bool IEnumerableAddRangeToList<T>(
            IEnumerable<T> collection1,
            IEnumerable<T> collection2
            )
        {
            if ((collection1 == null) || (collection2 == null))
                return false;

            List<T> list = collection1 as List<T>;

            if (list == null)
                return false;

            list.AddRange(collection2);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sorts the specified collection in place when it is a list.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the collection.
        /// </typeparam>
        /// <param name="collection">
        /// The collection to sort.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the collection was sorted; otherwise, false.
        /// </returns>
        public static bool IEnumerableSortList<T>(
            IEnumerable<T> collection
            )
        {
            if (collection == null)
                return false;

            List<T> list = collection as List<T>;

            list.Sort();
            return true;
        }
    }
}
