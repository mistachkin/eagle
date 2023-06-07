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
    [ObjectId("41713d5d-1147-4395-9863-92e45a9f28dc")]
    internal static class ListOps
    {
        #region Private Data
        //
        // HACK: This is purposely not read-only.
        //
        private static int canGetOrCopyList = 1; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int getListCount;
        private static int copyListCount;
        private static int nonCollectionCount;
        private static int nullOrStringCount;
        private static int skipListCount;
        private static int splitListCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildInterpreterInfoList method.
        //
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

        public static void Adjust(
            IntList list,
            int adjustment
            )
        {
            Adjust(list, adjustment, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static string Concat(params string[] strings)
        {
            return (strings != null) ? Concat(new StringList(strings)) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list)
        {
            return Concat(list, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex)
        {
            return (list != null) ? Concat(list, startIndex, list.Count - 1) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex, int stopIndex)
        {
            return Concat(list, startIndex, stopIndex, Characters.Space.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex, string separator)
        {
            return (list != null) ? Concat(list, startIndex, list.Count - 1,
                (separator != null) ? separator : Characters.Space.ToString()) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static int GetMaximumLength(
            IEnumerable<string> collection
            )
        {
            int result = Length.Invalid;

            if (collection != null)
            {
                foreach (string item in collection)
                {
                    if (item == null)
                        continue;

                    if (item.Length > result)
                        result = item.Length;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetMaximumLength(
            IList list,
            string format,
            int limit
            )
        {
            int result = Length.Invalid;

            if (list != null)
            {
                foreach (object element in list)
                {
                    if (element != null)
                    {
                        IToString toString = element as IToString;
                        string value;

                        if (toString != null)
                            value = toString.ToString(format);
                        else
                            value = element.ToString();

                        if (!String.IsNullOrEmpty(value))
                        {
                            if ((result == Length.Invalid) ||
                                (value.Length > result))
                            {
                                result = value.Length;
                            }
                        }
                    }
                }

                //
                // NOTE: Reduce to the maximum limit allowed by the caller.
                //
                if ((result != Length.Invalid) &&
                    (limit != Length.Invalid) &&
                    (result > limit))
                {
                    result = limit;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static StringList GetUniqueElements(
            StringList list /* in */
            )
        {
            if ((list == null) || (list.Count == 0))
                return list;

            StringList localList = new StringList();
            _StringDictionary localNames = new _StringDictionary();

            foreach (string element in list)
            {
                //
                // HACK: Any null or empty list element are
                //       always skipped (and never added).
                //
                if (String.IsNullOrEmpty(element))
                    continue;

                //
                // NOTE: If this element was seen before,
                //       skip it now.
                //
                if (localNames.ContainsKey(element))
                    continue;

                //
                // NOTE: First, mark the element as "seen";
                //       then, add it to the local list.
                //
                localNames[element] = null;
                localList.Add(element);
            }

            return localList;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
    }
}
