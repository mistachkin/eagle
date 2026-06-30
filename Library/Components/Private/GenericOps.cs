/*
 * GenericOps.cs --
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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    #region GenericOpsData Class
    /// <summary>
    /// This class holds the mutable configuration data shared by the generic
    /// operation helper classes in this file.
    /// </summary>
    [ObjectId("66257ec2-3bae-4571-9161-4905ea5c569a")]
    internal static class GenericOpsData
    {
        #region Private Data
        /// <summary>
        /// When non-zero, keys and values that implement
        /// <see cref="IFormattable" /> are formatted using that interface when
        /// a custom format string is supplied.
        /// </summary>
        public static bool UseFormattable = true;
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////////

    #region GenericCompareOps<T> Class
    /// <summary>
    /// This class provides generic helper methods for comparing and hashing
    /// arrays of comparable elements.
    /// </summary>
    /// <typeparam name="T">
    /// The element type, which must be comparable to itself.
    /// </typeparam>
    [ObjectId("a7570998-6e18-4ac0-aac7-87000158c37a")]
    internal static class GenericCompareOps<T> where T : IComparable<T>
    {
        #region Private Constants
#if ARGUMENT_CACHE
        /// <summary>
        /// The hash code returned for a null or empty array.
        /// </summary>
        private static readonly int DefaultHashCode = 0;

        /// <summary>
        /// The hash code returned when the requested length exceeds the number
        /// of available elements.
        /// </summary>
        private static readonly int InvalidHashCode = Length.Invalid;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two arrays are equal, comparing up to
        /// the specified number of elements.
        /// </summary>
        /// <param name="array1">
        /// The first array to compare.
        /// </param>
        /// <param name="array2">
        /// The second array to compare.
        /// </param>
        /// <param name="length">
        /// The number of elements to compare; a negative value requires the
        /// arrays to be exactly the same size.
        /// </param>
        /// <returns>
        /// True if the arrays are considered equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T[] array1,
            T[] array2,
            int length
            )
        {
            int compare = 0;

            return Equals(array1, array2, length, ref compare);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two arrays are equal, comparing up to
        /// the specified number of elements starting at the specified index.
        /// </summary>
        /// <param name="array1">
        /// The first array to compare.
        /// </param>
        /// <param name="array2">
        /// The second array to compare.
        /// </param>
        /// <param name="startIndex">
        /// The index at which to begin comparing; a negative value starts at
        /// the first element.
        /// </param>
        /// <param name="length">
        /// The number of elements to compare; a negative value requires the
        /// arrays to be exactly the same size.
        /// </param>
        /// <returns>
        /// True if the arrays are considered equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T[] array1,
            T[] array2,
            int startIndex,
            int length
            )
        {
            int compare = 0;
            int failIndex = Index.Invalid;

            return Equals(array1, array2, startIndex, length, ref compare, ref failIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two arrays are equal, comparing up to
        /// the specified number of elements and reporting the relative ordering
        /// at the first differing element.
        /// </summary>
        /// <param name="array1">
        /// The first array to compare.
        /// </param>
        /// <param name="array2">
        /// The second array to compare.
        /// </param>
        /// <param name="length">
        /// The number of elements to compare; a negative value requires the
        /// arrays to be exactly the same size.
        /// </param>
        /// <param name="compare">
        /// Upon return, set to the result of comparing the first differing pair
        /// of elements, or zero if the arrays are equal.
        /// </param>
        /// <returns>
        /// True if the arrays are considered equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T[] array1,
            T[] array2,
            int length,
            ref int compare
            )
        {
            int failIndex = Index.Invalid;

            return Equals(array1, array2, length, ref compare, ref failIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two arrays are equal, comparing up to
        /// the specified number of elements and reporting both the relative
        /// ordering and the index of the first differing element.
        /// </summary>
        /// <param name="array1">
        /// The first array to compare.
        /// </param>
        /// <param name="array2">
        /// The second array to compare.
        /// </param>
        /// <param name="length">
        /// The number of elements to compare; a negative value requires the
        /// arrays to be exactly the same size.
        /// </param>
        /// <param name="compare">
        /// Upon return, set to the result of comparing the first differing pair
        /// of elements, or zero if the arrays are equal.
        /// </param>
        /// <param name="failIndex">
        /// Upon return, set to the index of the first differing element, or an
        /// invalid index if the arrays are equal.
        /// </param>
        /// <returns>
        /// True if the arrays are considered equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T[] array1,
            T[] array2,
            int length,
            ref int compare,
            ref int failIndex
            )
        {
            return Equals(array1, array2, Index.Invalid, length, ref compare, ref failIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two arrays are equal, comparing up to
        /// the specified number of elements starting at the specified index and
        /// reporting both the relative ordering and the index of the first
        /// differing element.  A null element is considered less than a non-null
        /// element.
        /// </summary>
        /// <param name="array1">
        /// The first array to compare.
        /// </param>
        /// <param name="array2">
        /// The second array to compare.
        /// </param>
        /// <param name="startIndex">
        /// The index at which to begin comparing; a negative value starts at
        /// the first element.
        /// </param>
        /// <param name="length">
        /// The number of elements to compare; a negative value requires the
        /// arrays to be exactly the same size.
        /// </param>
        /// <param name="compare">
        /// Upon return, set to the result of comparing the first differing pair
        /// of elements, or zero if the arrays are equal.
        /// </param>
        /// <param name="failIndex">
        /// Upon return, set to the index of the first differing element, or an
        /// invalid index if the arrays are equal.
        /// </param>
        /// <returns>
        /// True if the arrays are considered equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T[] array1,
            T[] array2,
            int startIndex,
            int length,
            ref int compare,
            ref int failIndex
            )
        {
            if ((array1 == null) && (array2 == null))
                return true;

            if ((array1 == null) || (array2 == null))
                return false;

            int localLength;

            if (length < 0)
            {
                //
                // NOTE: Use "automatic" handling.  Arrays must be exactly
                //       the same size.
                //
                localLength = array1.Length;

                if (localLength != array2.Length)
                    return false;
            }
            else if (length == 0)
            {
                //
                // NOTE: Yes, I guess that zero bytes are equal.
                //
                return true;
            }
            else if ((length > array1.Length) || (length > array2.Length))
            {
                //
                // NOTE: Using prefix handling; however, both arrays must
                //       have at least the specified number of bytes to
                //       compare.
                //
                return false;
            }
            else
            {
                //
                // NOTE: Using prefix handling and both arrays do have at
                //       least the specified number of bytes to compare;
                //       therefore, use that to limit the loop.
                //
                localLength = length;
            }

            if (startIndex < 0)
            {
                //
                // NOTE: Use "automatic" handling.  Start at first index.
                //
                startIndex = 0;
            }
            else if (startIndex >= localLength)
            {
                //
                // NOTE: This index is out of bounds, fail now.
                //
                return false;
            }

            for (int index = startIndex; index < localLength; index++)
            {
                T element1 = array1[index];
                T element2 = array2[index];

                if ((element1 != null) && (element2 != null))
                {
                    int localCompare = element1.CompareTo(element2);

                    if (localCompare != 0)
                    {
                        compare = localCompare;
                        failIndex = index;

                        return false;
                    }
                }
                else if (element1 != null)
                {
                    compare = 1; // element1 is greater than null.
                    failIndex = index;

                    return false;
                }
                else if (element2 != null)
                {
                    compare = -1; // element2 is greater than null.
                    failIndex = index;

                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        /// <summary>
        /// This method computes a combined hash code for up to the specified
        /// number of elements of the specified array by combining the per-
        /// element hash codes.
        /// </summary>
        /// <param name="array">
        /// The array whose elements are to be hashed.
        /// </param>
        /// <param name="length">
        /// The number of elements to hash; a negative value hashes all
        /// elements.
        /// </param>
        /// <returns>
        /// The combined hash code, or an invalid hash code if the requested
        /// length exceeds the number of available elements.
        /// </returns>
        public static int GetHashCode(
            T[] array,
            int length
            )
        {
            int result = DefaultHashCode;

            if (array == null)
                return result;

            int localLength;

            if (length < 0)
            {
                //
                // NOTE: Ok, hash all elements.
                //
                localLength = array.Length;
            }
            else if (length == 0)
            {
                //
                // NOTE: Ok, hash zero elements.
                //
                return result;
            }
            else if (length > array.Length)
            {
                //
                // NOTE: Error, not enough elements.
                //
                return InvalidHashCode;
            }
            else
            {
                //
                // NOTE: Ok, hash exactly X elements.
                //
                localLength = length;
            }

            for (int index = 0; index < length; index++)
            {
                T element = array[index];

                if (element == null)
                    continue;

                result ^= element.GetHashCode();
            }

            return result;
        }
#endif
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////////

    #region GenericOps<T> Class
    /// <summary>
    /// This class provides generic helper methods for manipulating arrays,
    /// lists, dictionaries, and individual values of a single element type,
    /// including argument popping, equality, hashing, and string formatting.
    /// </summary>
    /// <typeparam name="T">
    /// The element type operated upon.
    /// </typeparam>
    [ObjectId("7f613001-c787-4e55-acde-eeb35834d5d0")]
    internal static class GenericOps<T>
    {
        /// <summary>
        /// This method removes and returns the first element of the specified
        /// array, replacing the array with a smaller one (or null when it
        /// becomes empty).
        /// </summary>
        /// <param name="array">
        /// On input, the array to pop the first element from.  Upon return,
        /// refers to the remaining elements, or null if none remain.
        /// </param>
        /// <returns>
        /// The first element of the array, or the default value of the element
        /// type if the array is null or empty.
        /// </returns>
        public static T PopFirstArgument(
            ref T[] array
            )
        {
            if (array != null)
            {
                int length = array.Length;

                if (length > 0)
                {
                    length--; /* one less element */

                    T result = array[0]; /* extract first element */

                    if (length > 0)
                    {
                        /* new length is one less */
                        T[] newArray = new T[length];

                        /* copy array, skip first element */
                        /* length has already been adjusted down */
                        Array.Copy(array, 1, newArray, 0, length);

                        /* replace original array */
                        array = newArray;
                    }
                    else
                    {
                        /* no arguments left */
                        array = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the first element of the specified
        /// list, replacing the list with a smaller one (or null when it becomes
        /// empty).  The list type is assumed to have a constructor accepting a
        /// single integer capacity argument.
        /// </summary>
        /// <param name="list">
        /// On input, the list to pop the first element from.  Upon return,
        /// refers to the remaining elements, or null if none remain.
        /// </param>
        /// <returns>
        /// The first element of the list, or the default value of the element
        /// type if the list is null or empty.
        /// </returns>
        public static T PopFirstArgument(
            ref IList<T> list
            )
        {
            if (list != null)
            {
                //
                // WARNING: This method assumes that the list type has a
                //          constructor with exactly one integer [capacity]
                //          argument.
                //
                Type type = list.GetType();
                int count = list.Count;

                if (count > 0)
                {
                    count--; /* one less element */

                    T result = list[0]; /* extract first element */

                    if (count > 0)
                    {
                        /* new count is one less */
                        IList<T> newList = Activator.CreateInstance(
                            type, new object[] { count }) as IList<T>;

                        /* copy list, skip first element */
                        /* count has already been adjusted down */
                        for (int index = 1; index < count + 1; index++)
                            newList.Add(list[index]);

                        /* replace original array */
                        list = newList;
                    }
                    else
                    {
                        /* no arguments left */
                        list = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the last element of the specified
        /// array, replacing the array with a smaller one (or null when it
        /// becomes empty).
        /// </summary>
        /// <param name="array">
        /// On input, the array to pop the last element from.  Upon return,
        /// refers to the remaining elements, or null if none remain.
        /// </param>
        /// <returns>
        /// The last element of the array, or the default value of the element
        /// type if the array is null or empty.
        /// </returns>
        public static T PopLastArgument(
            ref T[] array
            )
        {
            if (array != null)
            {
                int length = array.Length;

                if (length > 0)
                {
                    length--; /* one less element */

                    T result = array[length]; /* extract last element */

                    if (length > 0)
                    {
                        /* new length is one less */
                        T[] newArray = new T[length];

                        /* copy array, skip last element */
                        /* length has already been adjusted down */
                        Array.Copy(array, 0, newArray, 0, length);

                        /* replace original array */
                        array = newArray;
                    }
                    else
                    {
                        /* no arguments left */
                        array = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the last element of the specified
        /// list, replacing the list with a smaller one (or null when it becomes
        /// empty).  The list type is assumed to have a constructor accepting a
        /// single integer capacity argument.
        /// </summary>
        /// <param name="list">
        /// On input, the list to pop the last element from.  Upon return,
        /// refers to the remaining elements, or null if none remain.
        /// </param>
        /// <returns>
        /// The last element of the list, or the default value of the element
        /// type if the list is null or empty.
        /// </returns>
        public static T PopLastArgument(
            ref IList<T> list
            )
        {
            if (list != null)
            {
                //
                // WARNING: This method assumes that the list type has a
                //          constructor with exactly one integer [capacity]
                //          argument.
                //
                Type type = list.GetType();
                int count = list.Count;

                if (count > 0)
                {
                    count--; /* one less element */

                    T result = list[count]; /* extract last element */

                    if (count > 0)
                    {
                        /* new count is one less */
                        IList<T> newList = Activator.CreateInstance(
                            type, new object[] { count }) as IList<T>;

                        /* copy list, skip last element */
                        /* count has already been adjusted down */
                        for (int index = 0; index < count; index++)
                            newList.Add(list[index]);

                        /* replace original array */
                        list = newList;
                    }
                    else
                    {
                        /* no arguments left */
                        list = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two values are equal, using the
        /// specified equality comparer when one is supplied and the default
        /// equality logic otherwise.
        /// </summary>
        /// <param name="equalityComparer">
        /// The equality comparer to use, or null to use the default equality
        /// logic.
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
        public static bool EqualityComparerEquals(
            IEqualityComparer<T> equalityComparer,
            T left,
            T right
            )
        {
            return (equalityComparer != null) ?
                equalityComparer.Equals(left, right) : Equals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash code for the specified value, using the
        /// specified equality comparer when one is supplied and the default
        /// hashing logic otherwise.
        /// </summary>
        /// <param name="equalityComparer">
        /// The equality comparer to use, or null to use the default hashing
        /// logic.
        /// </param>
        /// <param name="value">
        /// The value to hash.
        /// </param>
        /// <returns>
        /// The hash code for the specified value.
        /// </returns>
        public static int EqualityComparerGetHashCode(
            IEqualityComparer<T> equalityComparer,
            T value
            )
        {
            return (equalityComparer != null) ?
                equalityComparer.GetHashCode(value) : GetHashCode(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two values are equal using the
        /// default equality logic, treating two null values as equal.
        /// </summary>
        /// <param name="left">
        /// The first value to compare.
        /// </param>
        /// <param name="right">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// True if the two values are equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T left,
            T right
            )
        {
            if ((left != null) && (right != null))
                return left.Equals(right);
            else
                return (left == null) && (right == null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash code for the specified value using the
        /// default hashing logic.
        /// </summary>
        /// <param name="value">
        /// The value to hash.
        /// </param>
        /// <returns>
        /// The hash code for the specified value, or zero if it is null.
        /// </returns>
        public static int GetHashCode(
            T value
            )
        {
            return (value != null) ? value.GetHashCode() : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string representation of the specified
        /// value, or a fallback string when the value is null.
        /// </summary>
        /// <param name="value">
        /// The value to convert to a string.
        /// </param>
        /// <param name="default">
        /// The fallback string to return when the value is null.
        /// </param>
        /// <returns>
        /// The string representation of the value, or the fallback string when
        /// the value is null.
        /// </returns>
        public static string ToString(
            T value,
            string @default
            )
        {
            return (value != null) ? value.ToString() : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends all elements of the specified collection to the
        /// specified list.  It is intended to exactly mimic the
        /// <c>List&lt;T&gt;.AddRange</c> method for arbitrary list types.
        /// </summary>
        /// <param name="list">
        /// The list to which the elements are appended.
        /// </param>
        /// <param name="collection">
        /// The collection of elements to append.
        /// </param>
        //
        // NOTE: This method should exactly mimic the List<T>.AddRange()
        //       method.
        //
        public static void AddRange(
            IList<T> list,
            IEnumerable<T> collection
            )
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (collection == null)
                throw new ArgumentNullException("collection");

            foreach (T item in collection)
                list.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified list into a string list,
        /// optionally skipping null elements and elements whose string
        /// representation is null or empty.
        /// </summary>
        /// <param name="list">
        /// The list to convert.
        /// </param>
        /// <param name="skipNull">
        /// Non-zero to skip null elements.
        /// </param>
        /// <param name="skipEmpty">
        /// Non-zero to skip elements whose string representation is null or
        /// empty.
        /// </param>
        /// <returns>
        /// A string list containing the converted elements, or null if the
        /// specified list is null.
        /// </returns>
        private static StringList ToStringList(
            IList<T> list,
            bool skipNull,
            bool skipEmpty
            )
        {
            if (list == null)
                return null;

            StringList result = new StringList(list.Count);

            foreach (T element in list)
            {
                if (skipNull && (element == null))
                    continue;

                string value = (element != null) ?
                    element.ToString() : null;

                if (skipEmpty && String.IsNullOrEmpty(value))
                    continue;

                result.Add(value);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces an English-style enumeration of the keys of the
        /// specified dictionary, using the specified separator, prefix, and
        /// suffix.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys are enumerated.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive items.
        /// </param>
        /// <param name="prefix">
        /// The prefix used in conjunction with the suffix before the final
        /// item.
        /// </param>
        /// <param name="suffix">
        /// The conjunction (for example, "and" or "or") placed before the final
        /// item.
        /// </param>
        /// <returns>
        /// The English-style enumeration of the dictionary keys.
        /// </returns>
        public static string DictionaryToEnglish(
            IDictionary<T, T> dictionary,
            string separator,
            string prefix,
            string suffix
            )
        {
            IList<T> list = (dictionary != null) ?
                new List<T>(dictionary.Keys) : null;

            return ListToEnglish(
                list, separator, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces an English-style enumeration of the keys of the
        /// specified dictionary, using the specified separator, prefix, suffix,
        /// and per-item value prefix and suffix.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys are enumerated.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive items.
        /// </param>
        /// <param name="prefix">
        /// The prefix used in conjunction with the suffix before the final
        /// item.
        /// </param>
        /// <param name="suffix">
        /// The conjunction (for example, "and" or "or") placed before the final
        /// item.
        /// </param>
        /// <param name="valuePrefix">
        /// The string prepended to each individual item.
        /// </param>
        /// <param name="valueSuffix">
        /// The string appended to each individual item.
        /// </param>
        /// <returns>
        /// The English-style enumeration of the dictionary keys.
        /// </returns>
        public static string DictionaryToEnglish(
            IDictionary<T, T> dictionary,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            IList<T> list = (dictionary != null) ?
                new List<T>(dictionary.Keys) : null;

            return ListToEnglish(
                list, separator, prefix, suffix, valuePrefix,
                valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces an English-style enumeration of the elements of
        /// the specified list, using the specified separator, prefix, and
        /// suffix.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are enumerated.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive items.
        /// </param>
        /// <param name="prefix">
        /// The prefix used in conjunction with the suffix before the final
        /// item.
        /// </param>
        /// <param name="suffix">
        /// The conjunction (for example, "and" or "or") placed before the final
        /// item.
        /// </param>
        /// <returns>
        /// The English-style enumeration of the list elements.
        /// </returns>
        public static string ListToEnglish(
            IList<T> list,
            string separator,
            string prefix,
            string suffix
            )
        {
            return ListToEnglish(
                list, separator, prefix, suffix, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces an English-style enumeration of the elements of
        /// the specified list, using the specified separator, prefix, suffix,
        /// and per-item value prefix and suffix.  Null and empty elements are
        /// skipped.
        /// </summary>
        /// <param name="list">
        /// The list whose elements are enumerated.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive items.
        /// </param>
        /// <param name="prefix">
        /// The prefix used in conjunction with the suffix before the final
        /// item.
        /// </param>
        /// <param name="suffix">
        /// The conjunction (for example, "and" or "or") placed before the final
        /// item.
        /// </param>
        /// <param name="valuePrefix">
        /// The string prepended to each individual item.
        /// </param>
        /// <param name="valueSuffix">
        /// The string appended to each individual item.
        /// </param>
        /// <returns>
        /// The English-style enumeration of the list elements.
        /// </returns>
        public static string ListToEnglish(
            IList<T> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            StringBuilder result = StringBuilderFactory.Create();
            StringList localList = ToStringList(list, true, true);

            if (localList != null)
            {
                int count = localList.Count;

                if (count > 0)
                {
                    bool havePrefix = !String.IsNullOrEmpty(prefix);

                    bool haveSeparator = !String.IsNullOrEmpty(
                        separator);

                    bool haveSuffix = !String.IsNullOrEmpty(suffix);

                    bool haveValuePrefix = !String.IsNullOrEmpty(
                        valuePrefix);

                    bool haveValueSuffix = !String.IsNullOrEmpty(
                        valueSuffix);

                    for (int index = 0; index < count; index++)
                    {
                        string value = localList[index];
                        bool usedSeparator = false;

                        if ((index > 0) && (count > 2) &&
                            haveSeparator)
                        {
                            result.Append(separator);
                            usedSeparator = true;
                        }

                        if ((index == (count - 1)) &&
                            (count > 1) && haveSuffix)
                        {
                            if (havePrefix && !usedSeparator)
                                result.Append(prefix);

                            result.Append(suffix);
                        }

                        if (haveValuePrefix)
                            result.Append(valuePrefix);

                        result.Append(value);

                        if (haveValueSuffix)
                            result.Append(valueSuffix);
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the elements of the input list, within the
        /// specified index range, that match the specified regular expression
        /// pattern into the output list, preserving the original element type.
        /// </summary>
        /// <param name="inputList">
        /// The list whose elements are filtered.
        /// </param>
        /// <param name="outputList">
        /// The list to which matching elements are added.
        /// </param>
        /// <param name="startIndex">
        /// The starting index of the range to consider.
        /// </param>
        /// <param name="stopIndex">
        /// The stopping index of the range to consider.
        /// </param>
        /// <param name="toStringFlags">
        /// The flags used when converting each element to its string
        /// representation for matching.
        /// </param>
        /// <param name="regExPattern">
        /// The regular expression pattern to match against; null matches every
        /// element.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when compiling the pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode FilterList(
            IList<T> inputList,
            IList<T> outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string regExPattern,
            RegexOptions regExOptions,
            ref Result error
            )
        {
            if (inputList == null)
            {
                error = "invalid input list";
                return ReturnCode.Error;
            }

            if (outputList == null)
            {
                error = "invalid output list";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Skip everything if the input list is empty.
            //
            int count = inputList.Count;

            if (count == 0)
                return ReturnCode.Ok;

            if (!ListOps.CheckStartAndStopIndex(
                    0, count - 1, ref startIndex, ref stopIndex,
                    ref error))
            {
                return ReturnCode.Error;
            }

            Regex regEx = null;

            try
            {
                if (regExPattern != null)
                {
                    regEx = RegExOps.Create(
                        regExPattern, regExOptions); /* throw */
                }
            }
            catch
            {
                // do nothing.
            }

            if ((regExPattern != null) && (regEx == null))
            {
                error = "invalid regular expression";
                return ReturnCode.Error;
            }

            for (int index = startIndex; index <= stopIndex; index++)
            {
                T element = inputList[index];
                string elementString;

                if (element != null)
                {
                    if (toStringFlags != ToStringFlags.None)
                    {
                        IToString toString = element as IToString;

                        if (toString != null)
                        {
                            elementString = toString.ToString(
                                toStringFlags);
                        }
                        else
                        {
                            elementString = element.ToString();
                        }
                    }
                    else
                    {
                        elementString = element.ToString();
                    }
                }
                else
                {
                    elementString = String.Empty;
                }

                //
                // NOTE: Match the string representation and add
                //       the original element and not the string
                //       representation because this is a generic
                //       list, not a StringList.
                //
                if ((regEx == null) || regEx.IsMatch(elementString))
                    outputList.Add(element);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the elements of the input list, within the
        /// specified index range, that match the specified string pattern into
        /// the output list, preserving the original element type.
        /// </summary>
        /// <param name="inputList">
        /// The list whose elements are filtered.
        /// </param>
        /// <param name="outputList">
        /// The list to which matching elements are added.
        /// </param>
        /// <param name="startIndex">
        /// The starting index of the range to consider.
        /// </param>
        /// <param name="stopIndex">
        /// The stopping index of the range to consider.
        /// </param>
        /// <param name="toStringFlags">
        /// The flags used when converting each element to its string
        /// representation for matching.
        /// </param>
        /// <param name="pattern">
        /// The string match pattern to match against; null matches every
        /// element.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode FilterList(
            IList<T> inputList,
            IList<T> outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string pattern,
            bool noCase,
            ref Result error
            )
        {
            if (inputList == null)
            {
                error = "invalid input list";
                return ReturnCode.Error;
            }

            if (outputList == null)
            {
                error = "invalid output list";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Skip everything if the input list is empty.
            //
            int count = inputList.Count;

            if (count == 0)
                return ReturnCode.Ok;

            if (!ListOps.CheckStartAndStopIndex(
                    0, count - 1, ref startIndex, ref stopIndex,
                    ref error))
            {
                return ReturnCode.Error;
            }

            for (int index = startIndex; index <= stopIndex; index++)
            {
                T element = inputList[index];
                string elementString;

                if (element != null)
                {
                    if (toStringFlags != ToStringFlags.None)
                    {
                        IToString toString = element as IToString;

                        if (toString != null)
                        {
                            elementString = toString.ToString(
                                toStringFlags);
                        }
                        else
                        {
                            elementString = element.ToString();
                        }
                    }
                    else
                    {
                        elementString = element.ToString();
                    }
                }
                else
                {
                    elementString = String.Empty;
                }

                //
                // NOTE: Match the string representation and add
                //       the original element and not the string
                //       representation because this is a generic
                //       list, not a StringList.
                //
                if ((pattern == null) || StringOps.Match(
                        null, StringOps.DefaultMatchMode,
                        elementString, pattern, noCase))
                {
                    outputList.Add(element);
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the elements of the specified enumerable that
        /// match the specified string pattern into a properly quoted Tcl list
        /// string, joined by the specified separator.
        /// </summary>
        /// <param name="list">
        /// The enumerable whose elements are converted.
        /// </param>
        /// <param name="toStringFlags">
        /// The flags used when converting each element to its string
        /// representation.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive elements.
        /// </param>
        /// <param name="pattern">
        /// The string match pattern to match against; null matches every
        /// element.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The list string containing the matching elements.
        /// </returns>
        public static string EnumerableToString(
            IEnumerable<T> list,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (list != null)
            {
                bool once = false;

                foreach (T element in list)
                {
                    string elementString;

                    if (element != null)
                    {
                        if (toStringFlags != ToStringFlags.None)
                        {
                            IToString toString = element as IToString;

                            if (toString != null)
                            {
                                elementString = toString.ToString(
                                    toStringFlags);
                            }
                            else
                            {
                                elementString = element.ToString();
                            }
                        }
                        else
                        {
                            elementString = element.ToString();
                        }
                    }
                    else
                    {
                        elementString = String.Empty;
                    }

                    if ((pattern == null) || StringOps.Match(
                            null, StringOps.DefaultMatchMode,
                            elementString, pattern, noCase))
                    {
                        ListElementFlags flags = once ?
                            ListElementFlags.None :
                            ListElementFlags.DontQuoteHash;

                        once = true;

                        int length = elementString.Length;

                        Parser.ScanElement(
                            /* null, */ elementString, 0,
                            length, ref flags);

                        if ((result.Length > 0) &&
                            !String.IsNullOrEmpty(separator))
                        {
                            result.Append(separator);
                        }

                        Parser.ConvertElement(
                            /* null, */ elementString, 0,
                            length, flags, ref result);
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the elements of the specified enumerable that
        /// match the specified regular expression pattern into a properly quoted
        /// Tcl list string, joined by the specified separator.
        /// </summary>
        /// <param name="list">
        /// The enumerable whose elements are converted.
        /// </param>
        /// <param name="toStringFlags">
        /// The flags used when converting each element to its string
        /// representation.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive elements.
        /// </param>
        /// <param name="regExPattern">
        /// The regular expression pattern to match against; null matches every
        /// element.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when compiling the pattern.
        /// </param>
        /// <returns>
        /// The list string containing the matching elements.
        /// </returns>
        public static string EnumerableToString(
            IEnumerable<T> list,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (list != null)
            {
                Regex regEx = null;

                if (regExPattern != null)
                {
                    regEx = RegExOps.Create(
                        regExPattern, regExOptions); /* throw */
                }

                bool once = false;

                foreach (T element in list)
                {
                    string elementString;

                    if (element != null)
                    {
                        if (toStringFlags != ToStringFlags.None)
                        {
                            IToString toString = element as IToString;

                            if (toString != null)
                            {
                                elementString = toString.ToString(
                                    toStringFlags);
                            }
                            else
                            {
                                elementString = element.ToString();
                            }
                        }
                        else
                        {
                            elementString = element.ToString();
                        }
                    }
                    else
                    {
                        elementString = String.Empty;
                    }

                    if ((regEx == null) || regEx.IsMatch(elementString))
                    {
                        ListElementFlags flags = once ?
                            ListElementFlags.None :
                            ListElementFlags.DontQuoteHash;

                        once = true;

                        int length = elementString.Length;

                        Parser.ScanElement(
                            /* null, */ elementString, 0,
                            length, ref flags);

                        if ((result.Length > 0) &&
                            !String.IsNullOrEmpty(separator))
                        {
                            result.Append(separator);
                        }

                        Parser.ConvertElement(
                            /* null, */ elementString, 0,
                            length, flags, ref result);
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////////

    #region GenericOps<T1, T2> Class
    /// <summary>
    /// This class provides generic helper methods for extracting, matching,
    /// formatting, and combining the keys and values of dictionaries whose keys
    /// and values may be of distinct types.
    /// </summary>
    /// <typeparam name="T1">
    /// The dictionary key type.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The dictionary value type.
    /// </typeparam>
    [ObjectId("58b15ba6-0517-4179-ac20-5e63efae31f3")]
    internal static class GenericOps<T1, T2>
    {
        /// <summary>
        /// This method combines the keys and/or values of the specified
        /// dictionaries into a single string list.
        /// </summary>
        /// <param name="pairs">
        /// Non-zero to produce a list of key/value pairs; otherwise, a flat
        /// list is produced.
        /// </param>
        /// <param name="keys">
        /// Non-zero to include dictionary keys.
        /// </param>
        /// <param name="values">
        /// Non-zero to include dictionary values.
        /// </param>
        /// <param name="dictionaries">
        /// The dictionaries whose keys and/or values are combined.
        /// </param>
        /// <returns>
        /// A string list containing the combined keys and/or values.
        /// </returns>
        public static IStringList Combine(
            bool pairs,
            bool keys,
            bool values,
            params IDictionary<T1, T2>[] dictionaries
            )
        {
            IStringList list = pairs ?
                (IStringList)new StringPairList() : new StringList();

            foreach (IDictionary<T1, T2> dictionary in dictionaries)
            {
                if (dictionary == null)
                    continue;

                list.Add(KeysAndValues(
                    dictionary, pairs, keys, values, MatchMode.None,
                    null, null, null, null, null, false), 0);
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the key at the specified positional
        /// index within the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary to query.
        /// </param>
        /// <param name="index">
        /// The zero-based positional index of the key to retrieve.
        /// </param>
        /// <param name="key">
        /// Upon success, receives the key found at the specified index.
        /// </param>
        /// <returns>
        /// True if a key was found at the specified index; otherwise, false.
        /// </returns>
        public static bool TryGetKeyAtIndex(
            IDictionary<T1, T2> dictionary,
            int index,
            ref T1 key
            )
        {
            bool result = false;

            if (dictionary != null)
            {
                List<T1> keys = new List<T1>(dictionary.Keys);

                if (keys != null)
                {
                    if ((index >= 0) && (index < keys.Count))
                    {
                        key = keys[index];
                        result = true;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the value whose key is at the
        /// specified positional index within the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary to query.
        /// </param>
        /// <param name="index">
        /// The zero-based positional index of the key whose value is to be
        /// retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value found at the specified index.
        /// </param>
        /// <returns>
        /// True if a value was found at the specified index; otherwise, false.
        /// </returns>
        public static bool TryGetValueAtIndex(
            IDictionary<T1, T2> dictionary,
            int index,
            ref T2 value
            )
        {
            bool result = false;

            if (dictionary != null)
            {
                List<T1> keys = new List<T1>(dictionary.Keys);

                if (keys != null)
                {
                    if ((index >= 0) && (index < keys.Count))
                    {
                        value = dictionary[keys[index]];
                        result = true;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the keys and/or values of the specified
        /// dictionary into a string list, optionally filtering by key and value
        /// patterns and applying custom key and value formats.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys and/or values are extracted.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to produce a list of key/value pairs; otherwise, a flat
        /// list is produced.
        /// </param>
        /// <param name="keys">
        /// Non-zero to include dictionary keys.
        /// </param>
        /// <param name="values">
        /// Non-zero to include dictionary values.
        /// </param>
        /// <param name="mode">
        /// The matching mode used when applying the patterns.
        /// </param>
        /// <param name="keyPattern">
        /// The pattern matched against each key; null matches every key.
        /// </param>
        /// <param name="valuePattern">
        /// The pattern matched against each value; null matches every value.
        /// </param>
        /// <param name="keyFormat">
        /// The custom format applied to each key; null to use the default
        /// string representation.
        /// </param>
        /// <param name="valueFormat">
        /// The custom format applied to each value; null to use the default
        /// string representation.
        /// </param>
        /// <param name="formatProvider">
        /// The format provider used when applying the custom formats.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// A string list containing the matching keys and/or values.
        /// </returns>
        public static IStringList KeysAndValues(
            IDictionary<T1, T2> dictionary,
            bool pairs,
            bool keys,
            bool values,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool noCase
            )
        {
            return KeysAndValues(
                dictionary, pairs, keys, values, mode, keyPattern,
                valuePattern, keyFormat, valueFormat,
                formatProvider, noCase, RegexOptions.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the keys and/or values of the specified
        /// dictionary into a string list, optionally filtering by key and value
        /// patterns, applying custom key and value formats, and honoring the
        /// specified regular expression options.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys and/or values are extracted.
        /// </param>
        /// <param name="pairs">
        /// Non-zero to produce a list of key/value pairs; otherwise, a flat
        /// list is produced.
        /// </param>
        /// <param name="keys">
        /// Non-zero to include dictionary keys.
        /// </param>
        /// <param name="values">
        /// Non-zero to include dictionary values.
        /// </param>
        /// <param name="mode">
        /// The matching mode used when applying the patterns.
        /// </param>
        /// <param name="keyPattern">
        /// The pattern matched against each key; null matches every key.
        /// </param>
        /// <param name="valuePattern">
        /// The pattern matched against each value; null matches every value.
        /// </param>
        /// <param name="keyFormat">
        /// The custom format applied to each key; null to use the default
        /// string representation.
        /// </param>
        /// <param name="valueFormat">
        /// The custom format applied to each value; null to use the default
        /// string representation.
        /// </param>
        /// <param name="formatProvider">
        /// The format provider used when applying the custom formats.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when the matching mode is a
        /// regular expression mode.
        /// </param>
        /// <returns>
        /// A string list containing the matching keys and/or values, or null if
        /// the dictionary is null.
        /// </returns>
        public static IStringList KeysAndValues(
            IDictionary<T1, T2> dictionary,
            bool pairs,
            bool keys,
            bool values,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            if (dictionary != null)
            {
                IStringList list = pairs ?
                    (IStringList)new StringPairList() : new StringList();

                foreach (KeyValuePair<T1, T2> pair in dictionary)
                {
                    //
                    // NOTE: Assume there will be a match and then attempt to
                    //       prove otherwise.
                    //
                    bool match = true;

                    ///////////////////////////////////////////////////////////////////////////////////

                    string keyString;

                    if (match) /* REDUNDANT: In case code moves around. */
                    {
                        object key = pair.Key;

                        if (key != null)
                        {
                            //
                            // NOTE: Has a custom format been specified by the
                            //       caller for the key(s)?
                            //
                            if (keyFormat != null)
                            {
                                //
                                // NOTE: Attempt to treat the key as a formattable
                                //       object.  If we do not succeed, simply use
                                //       normal ToString.
                                //
                                IFormattable formattable =
                                    GenericOpsData.UseFormattable ?
                                        key as IFormattable : null;

                                if (formattable != null) /* REDUNDANT? */
                                {
                                    keyString = formattable.ToString(
                                        keyFormat, formatProvider);
                                }
                                else if (!MarshalOps.ToStringCache.TryFormat(key,
                                        keyFormat, MarshalOps.GetActiveBinder(),
                                        formatProvider as CultureInfo,
                                        out keyString))
                                {
                                    //
                                    // BUGBUG: This is actually "wrong" because it
                                    //         ignores the format string; however,
                                    //         there is no nice way to use it when
                                    //         the type is not hard-coded, i.e. a
                                    //         one argument ToString() method is
                                    //         NOT part of any built-in interface
                                    //         used by the .NET Framework.
                                    //
                                    keyString = key.ToString();
                                }
                            }
                            else
                            {
                                //
                                // NOTE: No custom formatting for the key, just
                                //       ToString it.
                                //
                                keyString = key.ToString();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Nothing much we can do here, the key is null;
                            //       therefore, the key string is null.
                            //
                            keyString = null;
                        }

                        //
                        // NOTE: Do we need to match against the key pattern, if any?
                        //       If the key pattern is null, we match everything.
                        //
                        if (keyPattern != null)
                        {
                            match = StringOps.Match(
                                null, mode, keyString, keyPattern, noCase,
                                null, regExOptions);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: No need to get a key string for this key, there is
                        //       no match.
                        //
                        keyString = null;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    string valueString;

                    if (match)
                    {
                        object value = pair.Value;

                        if (value != null)
                        {
                            //
                            // NOTE: Has a custom format been specified by the
                            //       caller for the value(s)?
                            //
                            if (valueFormat != null)
                            {
                                //
                                // NOTE: Attempt to treat the value as a formattable
                                //       object.  If we do not succeed, simply use
                                //       normal ToString.
                                //
                                IFormattable formattable =
                                    GenericOpsData.UseFormattable ?
                                        value as IFormattable : null;

                                if (formattable != null) /* REDUNDANT? */
                                {
                                    valueString = formattable.ToString(
                                        valueFormat, formatProvider);
                                }
                                else if (!MarshalOps.ToStringCache.TryFormat(value,
                                        valueFormat, MarshalOps.GetActiveBinder(),
                                        formatProvider as CultureInfo,
                                        out valueString))
                                {
                                    //
                                    // BUGBUG: This is actually "wrong" because it
                                    //         ignores the format string; however,
                                    //         there is no nice way to use it when
                                    //         the type is not hard-coded, i.e. a
                                    //         one argument ToString() method is
                                    //         NOT part of any built-in interface
                                    //         used by the .NET Framework.
                                    //
                                    valueString = value.ToString();
                                }
                            }
                            else
                            {
                                //
                                // NOTE: No custom formatting for the value, just
                                //       ToString it.
                                //
                                valueString = value.ToString();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Nothing much we can do here, the value is null;
                            //       therefore, the value string is null.
                            //
                            valueString = null;
                        }

                        //
                        // NOTE: Do we need to match against the value pattern, if any?
                        //       If the value pattern is null, we match everything.
                        //
                        if (valuePattern != null)
                        {
                            match = StringOps.Match(
                                null, mode, valueString, valuePattern, noCase,
                                null, regExOptions);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: No need to get a value string for this value, there is
                        //       no match.
                        //
                        valueString = null;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    //
                    // NOTE: Did we match the key and/or value strings using the
                    //       selected matching mode, pattern(s), and format(s)?
                    //
                    if (match)
                    {
                        //
                        // NOTE: Do the want the corresponding values as well
                        //       as the keys?
                        //
                        if (keys && values)
                            list.Add(keyString, valueString);
                        else if (keys)
                            list.Add(keyString);
                        else if (values)
                            list.Add(valueString);
                    }
                }

                return list;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the keys and/or values of the specified
        /// dictionary into a result that is either a string dictionary (when
        /// both keys and values are requested) or a string list, optionally
        /// filtering by a single pattern matched against the key, the value, or
        /// both.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose keys and/or values are extracted.
        /// </param>
        /// <param name="keys">
        /// Non-zero to include dictionary keys.
        /// </param>
        /// <param name="values">
        /// Non-zero to include dictionary values.
        /// </param>
        /// <param name="mode">
        /// The matching mode used when applying the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern matched against the selected match string; null matches
        /// every entry.
        /// </param>
        /// <param name="keyFormat">
        /// The custom format applied to each key; null to use the default
        /// string representation.
        /// </param>
        /// <param name="valueFormat">
        /// The custom format applied to each value; null to use the default
        /// string representation.
        /// </param>
        /// <param name="formatProvider">
        /// The format provider used when applying the custom formats.
        /// </param>
        /// <param name="matchKey">
        /// Non-zero to include the key in the string that the pattern is matched
        /// against.
        /// </param>
        /// <param name="matchValue">
        /// Non-zero to include the value in the string that the pattern is
        /// matched against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options used when the matching mode is a
        /// regular expression mode.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the resulting string dictionary or string
        /// list; upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode KeysAndValues(
            IDictionary<T1, T2> dictionary,
            bool keys,
            bool values,
            MatchMode mode,
            string pattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool matchKey,
            bool matchValue,
            bool noCase,
            RegexOptions regExOptions,
            ref Result result
            )
        {
            if (dictionary == null)
            {
                result = "invalid dictionary";
                return ReturnCode.Error;
            }

            if (!keys && !values)
            {
                result = String.Empty;
                return ReturnCode.Ok;
            }

            StringDictionary localDictionary = (keys && values) ?
                new StringDictionary() : null;

            StringList localList = (localDictionary == null) ?
                new StringList() : null;

            foreach (KeyValuePair<T1, T2> pair in dictionary)
            {
                //
                // NOTE: Assume there will be a match and then attempt to
                //       prove otherwise.
                //
                bool match = true;

                ///////////////////////////////////////////////////////////////////////////////////////

                string keyString;

                if (match) /* REDUNDANT: In case code moves around. */
                {
                    object key = pair.Key;

                    if (key != null)
                    {
                        //
                        // NOTE: Has a custom format been specified by the
                        //       caller for the key(s)?
                        //
                        if (keyFormat != null)
                        {
                            //
                            // NOTE: Attempt to treat the key as a formattable
                            //       object.  If we do not succeed, simply use
                            //       normal ToString.
                            //
                            IFormattable formattable =
                                GenericOpsData.UseFormattable ?
                                    key as IFormattable : null;

                            if (formattable != null) /* REDUNDANT? */
                            {
                                keyString = formattable.ToString(
                                    keyFormat, formatProvider);
                            }
                            else if (!MarshalOps.ToStringCache.TryFormat(key,
                                    keyFormat, MarshalOps.GetActiveBinder(),
                                    formatProvider as CultureInfo,
                                    out keyString))
                            {
                                //
                                // BUGBUG: This is actually "wrong" because it
                                //         ignores the format string; however,
                                //         there is no nice way to use it when
                                //         the type is not hard-coded, i.e. a
                                //         one argument ToString() method is
                                //         NOT part of any built-in interface
                                //         used by the .NET Framework.
                                //
                                keyString = key.ToString();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: No custom formatting for the key, just
                            //       ToString it.
                            //
                            keyString = key.ToString();
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Nothing much we can do here, the key is null;
                        //       therefore, the key string is null.
                        //
                        keyString = null;
                    }
                }
                else
                {
                    //
                    // NOTE: No need to get a key string for this key, there is
                    //       no match.
                    //
                    keyString = null;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                string valueString;

                if (match)
                {
                    object value = pair.Value;

                    if (value != null)
                    {
                        //
                        // NOTE: Has a custom format been specified by the
                        //       caller for the value(s)?
                        //
                        if (valueFormat != null)
                        {
                            //
                            // NOTE: Attempt to treat the value as a formattable
                            //       object.  If we do not succeed, simply use
                            //       normal ToString.
                            //
                            IFormattable formattable =
                                GenericOpsData.UseFormattable ?
                                    value as IFormattable : null;

                            if (formattable != null) /* REDUNDANT? */
                            {
                                valueString = formattable.ToString(
                                    valueFormat, formatProvider);
                            }
                            else if (!MarshalOps.ToStringCache.TryFormat(value,
                                    valueFormat, MarshalOps.GetActiveBinder(),
                                    formatProvider as CultureInfo,
                                    out valueString))
                            {
                                //
                                // BUGBUG: This is actually "wrong" because it
                                //         ignores the format string; however,
                                //         there is no nice way to use it when
                                //         the type is not hard-coded, i.e. a
                                //         one argument ToString() method is
                                //         NOT part of any built-in interface
                                //         used by the .NET Framework.
                                //
                                valueString = value.ToString();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: No custom formatting for the value, just
                            //       ToString it.
                            //
                            valueString = value.ToString();
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Nothing much we can do here, the value is null;
                        //       therefore, the value string is null.
                        //
                        valueString = null;
                    }
                }
                else
                {
                    //
                    // NOTE: No need to get a value string for this value, there is
                    //       no match.
                    //
                    valueString = null;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Do we need to match against the a pattern, if any?
                //       If the pattern is null, we match everything.
                //
                if (pattern != null)
                {
                    string matchString;

                    if (matchKey)
                    {
                        if (matchValue)
                        {
                            matchString = String.Format(
                                "{0} {1}", keyString, valueString);
                        }
                        else
                        {
                            matchString = keyString;
                        }
                    }
                    else if (matchValue)
                    {
                        matchString = valueString;
                    }
                    else
                    {
                        matchString = null;
                    }

                    if (matchString != null)
                    {
                        match = StringOps.Match(
                            null, mode, matchString, pattern, noCase, null,
                            regExOptions);
                    }
                    else
                    {
                        //
                        // NOTE: Nothing to match, just skip it.
                        //
                        match = false;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Did we match the key and/or value strings using the
                //       selected matching mode, pattern(s), and format(s)?
                //
                if (match)
                {
                    //
                    // NOTE: Do the want the corresponding values as well
                    //       as the keys?
                    //
                    if (localDictionary != null)
                    {
                        if (keyString != null)
                            localDictionary.Add(keyString, valueString);
                    }
                    else if (localList != null)
                    {
                        if (keys)
                            localList.Add(keyString);

                        if (values)
                            localList.Add(valueString);
                    }
                }
            }

            if (localDictionary != null)
                result = localDictionary;
            else
                result = localList;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the key/value pairs of the specified
        /// dictionary, within the specified index range and matching the
        /// specified regular expression pattern, into a properly quoted Tcl list
        /// string joined by the specified separator.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary to convert.
        /// </param>
        /// <param name="startIndex">
        /// The starting index of the range of keys to consider.
        /// </param>
        /// <param name="stopIndex">
        /// The stopping index of the range of keys to consider.
        /// </param>
        /// <param name="toStringFlags">
        /// The flags used when converting each key and value to its string
        /// representation.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive key/value pairs.
        /// </param>
        /// <param name="regExPattern">
        /// The regular expression pattern matched against each pair; null
        /// matches every pair.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when compiling the pattern.
        /// </param>
        /// <returns>
        /// The list string containing the matching key/value pairs.
        /// </returns>
        public static string DictionaryToString(
            IDictionary<T1, T2> dictionary,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (dictionary != null)
            {
                List<T1> list = new List<T1>(dictionary.Keys);
                int count = list.Count;

                if (ListOps.CheckStartAndStopIndex(
                        0, count - 1, ref startIndex, ref stopIndex))
                {
                    Regex regEx = null;

                    if (regExPattern != null)
                    {
                        regEx = RegExOps.Create(
                            regExPattern, regExOptions); /* throw */
                    }

                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        T1 keyElement = list[index];
                        string elementString;

                        if (keyElement != null)
                        {
                            string keyElementString;

                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = keyElement as IToString;

                                if (toString != null)
                                {
                                    keyElementString = toString.ToString(
                                        toStringFlags);
                                }
                                else
                                {
                                    keyElementString = keyElement.ToString();
                                }
                            }
                            else
                            {
                                keyElementString = keyElement.ToString();
                            }

                            T2 valueElement = dictionary[keyElement];
                            string valueElementString;

                            if (valueElement != null)
                            {
                                if (toStringFlags != ToStringFlags.None)
                                {
                                    IToString toString = valueElement as IToString;

                                    if (toString != null)
                                    {
                                        valueElementString = toString.ToString(
                                            toStringFlags);
                                    }
                                    else
                                    {
                                        valueElementString = valueElement.ToString();
                                    }
                                }
                                else
                                {
                                    valueElementString = valueElement.ToString();
                                }
                            }
                            else
                            {
                                valueElementString = String.Empty;
                            }

                            elementString = StringList.MakeList(
                                keyElementString, valueElementString);
                        }
                        else
                        {
                            elementString = String.Empty;
                        }

                        if ((regEx == null) || regEx.IsMatch(elementString))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None :
                                ListElementFlags.DontQuoteHash;

                            int length = elementString.Length;

                            Parser.ScanElement(
                                /* null, */ elementString, 0,
                                length, ref flags);

                            if ((result.Length > 0) &&
                                !String.IsNullOrEmpty(separator))
                            {
                                result.Append(separator);
                            }

                            Parser.ConvertElement(
                                /* null, */ elementString, 0,
                                length, flags, ref result);
                        }
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the key/value pairs of the specified
        /// dictionary, within the specified index range and matching the
        /// specified string pattern, into a properly quoted Tcl list string
        /// joined by the specified separator.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary to convert.
        /// </param>
        /// <param name="startIndex">
        /// The starting index of the range of keys to consider.
        /// </param>
        /// <param name="stopIndex">
        /// The stopping index of the range of keys to consider.
        /// </param>
        /// <param name="toStringFlags">
        /// The flags used when converting each key and value to its string
        /// representation.
        /// </param>
        /// <param name="separator">
        /// The separator placed between successive key/value pairs.
        /// </param>
        /// <param name="pattern">
        /// The string match pattern matched against each pair; null matches
        /// every pair.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The list string containing the matching key/value pairs.
        /// </returns>
        public static string DictionaryToString(
            IDictionary<T1, T2> dictionary,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (dictionary != null)
            {
                List<T1> list = new List<T1>(dictionary.Keys);
                int count = list.Count;

                if (ListOps.CheckStartAndStopIndex(
                        0, count - 1, ref startIndex, ref stopIndex))
                {
                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        string elementString;
                        T1 keyElement = list[index];

                        if (keyElement != null)
                        {
                            string keyElementString;

                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = keyElement as IToString;

                                if (toString != null)
                                {
                                    keyElementString = toString.ToString(
                                        toStringFlags);
                                }
                                else
                                {
                                    keyElementString = keyElement.ToString();
                                }
                            }
                            else
                            {
                                keyElementString = keyElement.ToString();
                            }

                            T2 valueElement = dictionary[keyElement];
                            string valueElementString;

                            if (valueElement != null)
                            {
                                if (toStringFlags != ToStringFlags.None)
                                {
                                    IToString toString = valueElement as IToString;

                                    if (toString != null)
                                    {
                                        valueElementString = toString.ToString(
                                            toStringFlags);
                                    }
                                    else
                                    {
                                        valueElementString = valueElement.ToString();
                                    }
                                }
                                else
                                {
                                    valueElementString = valueElement.ToString();
                                }
                            }
                            else
                            {
                                valueElementString = String.Empty;
                            }

                            elementString = StringList.MakeList(
                                keyElementString, valueElementString);
                        }
                        else
                        {
                            elementString = String.Empty;
                        }

                        if ((pattern == null) || StringOps.Match(
                                null, StringOps.DefaultMatchMode,
                                elementString, pattern, noCase))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None :
                                ListElementFlags.DontQuoteHash;

                            int length = elementString.Length;

                            Parser.ScanElement(
                                /* null, */ elementString, 0,
                                length, ref flags);

                            if ((result.Length > 0) &&
                                !String.IsNullOrEmpty(separator))
                            {
                                result.Append(separator);
                            }

                            Parser.ConvertElement(
                                /* null, */ elementString, 0,
                                length, flags, ref result);
                        }
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
    }
    #endregion
}
