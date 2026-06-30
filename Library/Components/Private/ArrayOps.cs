/*
 * ArrayOps.cs --
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
using System.Globalization;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Encodings;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of static helper methods used to
    /// operate on arrays and lists, including splitting, copying, resizing,
    /// comparing, converting to and from hexadecimal or delimited strings, and
    /// (optionally, via reflection) accessing the internal backing storage of a
    /// generic list.
    /// </summary>
    [ObjectId("e6709468-95a4-405e-8c9c-e0dbd1aa3a88")]
    internal static class ArrayOps
    {
        #region Private Constants
        /// <summary>
        /// The set of characters used to separate individual byte values when
        /// parsing a delimited string of bytes.
        /// </summary>
        private static char[] byteSeparators = {
            Characters.HorizontalTab, Characters.LineFeed,
            Characters.VerticalTab, Characters.FormFeed,
            Characters.CarriageReturn, Characters.Space,
            Characters.Comma
        };

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The encoding used to convert a string into a sequence of single-byte
        /// values (i.e. one byte per character).
        /// </summary>
        private static Encoding oneByteEncoding = OneByteEncoding.OneByte;

        /// <summary>
        /// The encoding used to convert a string into a sequence of two-byte
        /// values (i.e. two bytes per character).
        /// </summary>
        private static Encoding twoByteEncoding = TwoByteEncoding.TwoByte;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the private field that holds the backing array of
        /// elements within a generic list, as used by the .NET Framework and
        /// Mono.
        /// </summary>
        private static string itemsFieldName = "_items"; /* NOTE: Also Mono. */

        /// <summary>
        /// The name of the private field that holds the element count within a
        /// generic list, as used by the .NET Framework and Mono.
        /// </summary>
        private static string sizeFieldName = "_size"; /* NOTE: Also Mono. */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// When non-zero, reflection is not used to access the internal backing
        /// storage of a generic list; instead, the slower public methods are
        /// always used.
        /// </summary>
        private static bool noReflection = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.WriteEngineInfo method.
        //
        /// <summary>
        /// This method adds rows describing the current state of the array
        /// operations subsystem to the specified list, for use when displaying
        /// diagnostic information.
        /// </summary>
        /// <param name="list">
        /// The list to which the diagnostic rows will be added.  If this value
        /// is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control how much detail is included in the output.
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

            if (empty || noReflection)
                localList.Add("NoReflection", noReflection.ToString());

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("Array Operations");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits an array into two sub-arrays at the first element
        /// that compares equal to the specified split element.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the array.
        /// </typeparam>
        /// <param name="array">
        /// The array to be split.
        /// </param>
        /// <param name="split">
        /// The element value at which to split the array.  The first matching
        /// element is not included in either resulting sub-array.
        /// </param>
        /// <param name="left">
        /// Upon success, receives the elements that occur before the matching
        /// element; otherwise, this is set to null.
        /// </param>
        /// <param name="right">
        /// Upon success, receives the elements that occur after the matching
        /// element; otherwise, this is set to null.
        /// </param>
        /// <returns>
        /// True if a matching element was found and the array was split;
        /// otherwise, false.
        /// </returns>
        public static bool SplitOnOne<T>(
            T[] array,    /* in */
            T split,      /* in */
            out T[] left, /* out */
            out T[] right /* out */
            ) where T : IComparable<T>
        {
            left = null;
            right = null;

            if (array == null)
                return false;

            if (split == null)
                return false;

            int length = array.Length;

            if (length <= 0)
                return false;

            for (int index = 0; index < length; index++)
            {
                T element = array[index];

                if (element == null)
                    continue;

                if (element.CompareTo(split) == 0)
                {
                    int leftStartIndex = 0;
                    int leftLength = index;

                    int rightStartIndex = index + 1;
                    int rightLength = length - rightStartIndex;

                    left = new T[leftLength];
                    right = new T[rightLength];

                    Array.Copy(
                        array, leftStartIndex, left, 0, leftLength);

                    Array.Copy(
                        array, rightStartIndex, right, 0, rightLength);

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the array referred to by the specified typed
        /// reference with the elements of the specified collection.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the array and collection.
        /// </typeparam>
        /// <param name="arrayReference">
        /// A typed reference to the array variable to be initialized.  Upon
        /// return, the referenced array is set to a new array containing the
        /// elements of <paramref name="collection" />, or null when the
        /// collection is null.
        /// </param>
        /// <param name="collection">
        /// The collection whose elements are used to populate the array.  If
        /// this value is null, the referenced array is set to null.
        /// </param>
        public static void Initialize<T>(
            TypedReference arrayReference, /* in, out */
            IEnumerable<T> collection      /* in */
            )
        {
            T[] array = (collection != null) ?
                new List<T>(collection).ToArray() : null;

            __refvalue(arrayReference, T[]) = array;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates, via reflection, the private field that holds
        /// the backing array of elements for a generic list of the specified
        /// type.
        /// </summary>
        /// <param name="type">
        /// The type of the list (or one of its base types) to search for the
        /// items field.
        /// </param>
        /// <param name="itemsFieldInfo">
        /// Upon success, receives the reflected field information for the items
        /// field; otherwise, this is set to null.
        /// </param>
        private static void GetFieldInfo(
            Type type,                   /* in */
            out FieldInfo itemsFieldInfo /* out */
            )
        {
            FieldInfo sizeFieldInfo;

            GetFieldInfos(
                type, false, out itemsFieldInfo, out sizeFieldInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates, via reflection, the private fields that hold
        /// the backing array of elements and (optionally) the element count for
        /// a generic list of the specified type, walking up the type hierarchy
        /// as needed.
        /// </summary>
        /// <param name="type">
        /// The type of the list (or one of its base types) to search for the
        /// fields.
        /// </param>
        /// <param name="needSize">
        /// When non-zero, the size field is also required and located;
        /// otherwise, only the items field is located.
        /// </param>
        /// <param name="itemsFieldInfo">
        /// Upon success, receives the reflected field information for the items
        /// field; otherwise, this is set to null.
        /// </param>
        /// <param name="sizeFieldInfo">
        /// Upon success, receives the reflected field information for the size
        /// field when <paramref name="needSize" /> is non-zero; otherwise, this
        /// is set to null.
        /// </param>
        private static void GetFieldInfos(
            Type type,                    /* in */
            bool needSize,                /* in */
            out FieldInfo itemsFieldInfo, /* out */
            out FieldInfo sizeFieldInfo   /* out */
            )
        {
            itemsFieldInfo = null;
            sizeFieldInfo = null;

            if (type != null)
            {
                try
                {
                    Type localType = type;

                    while (localType != null)
                    {
                        itemsFieldInfo = localType.GetField(
                            itemsFieldName, ObjectOps.GetBindingFlags(
                            MetaBindingFlags.Items, true));

                        if (needSize)
                        {
                            sizeFieldInfo = localType.GetField(
                                sizeFieldName, ObjectOps.GetBindingFlags(
                                MetaBindingFlags.Size, true));

                            if ((itemsFieldInfo != null) &&
                                (sizeFieldInfo != null))
                            {
                                break;
                            }
                        }
                        else if (itemsFieldInfo != null)
                        {
                            break;
                        }

                        localType = localType.BaseType;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(ArrayOps).Name,
                        TracePriority.MarshalError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified number of elements from the
        /// beginning of a list, replacing it with a new list that contains the
        /// remaining elements.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list.
        /// </typeparam>
        /// <param name="list">
        /// The list to consume from.  Upon success, this is replaced with a new
        /// list containing the remaining elements.
        /// </param>
        /// <param name="count">
        /// The number of elements to remove from the beginning of the list.
        /// </param>
        /// <returns>
        /// True if the elements were consumed; otherwise, false.
        /// </returns>
        public static bool Consume<T>(
            ref List<T> list, /* in, out */
            int count         /* in */
            )
        {
            if (list == null)
                return false;

            int oldCount = list.Count;

            if ((count <= 0) || (count > oldCount))
                return false;

            int newCount = oldCount - count;

            List<T> newList = new List<T>(newCount);

            newList.AddRange(
                list.GetRange(count, newCount));

            list = newList;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified number of elements from the
        /// beginning of an array, replacing it with a new array that contains
        /// the remaining elements.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the array.
        /// </typeparam>
        /// <param name="array">
        /// The array to consume from.  Upon success, this is replaced with a
        /// new array containing the remaining elements.
        /// </param>
        /// <param name="count">
        /// The number of elements to remove from the beginning of the array.
        /// </param>
        /// <returns>
        /// True if the elements were consumed; otherwise, false.
        /// </returns>
        public static bool Consume<T>(
            ref T[] array, /* in, out */
            int count      /* in */
            )
        {
            if (array == null)
                return false;

            int length = array.Length;

            if ((count <= 0) || (count > length))
                return false;

            int newLength = length - count;

            T[] newArray = new T[newLength];

            Array.Copy(
                array, count, newArray, 0, newLength);

            array = newArray;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the internal backing array of the specified list to
        /// (a possibly offset portion of) the specified array.  When the start
        /// index is non-zero, the array is first reduced to the elements at and
        /// beyond that index.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list and array.
        /// </typeparam>
        /// <param name="list">
        /// The list whose backing array is to be set.
        /// </param>
        /// <param name="array">
        /// The array to install as the backing storage of the list.  Upon
        /// success, this may be set to null or to the reduced array.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first array element to use.  When zero, the entire
        /// array is used.
        /// </param>
        /// <returns>
        /// True if the backing array was set; otherwise, false.
        /// </returns>
        public static bool SetArray<T>(
            List<T> list,  /* in, out */
            ref T[] array, /* in, out */
            int startIndex /* in */
            )
        {
            if (startIndex != 0)
            {
                if (array == null)
                    return false;

                int oldLength = array.Length;

                if ((startIndex < 0) || (startIndex >= oldLength))
                    return false;

                int newLength = oldLength - startIndex;

                if (newLength < 0 || (newLength >= oldLength))
                    return false;

                T[] newArray = new T[newLength];

                Array.Copy(
                    array, startIndex, newArray, 0, newLength);

                if (SetArray<T>(list, ref newArray))
                {
                    array = newArray;
                    return true;
                }

                return false;
            }
            else
            {
                return SetArray<T>(list, ref array);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the internal backing array of the specified list to
        /// the specified array, using reflection when possible and falling back
        /// to the slower public methods otherwise.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list and array.
        /// </typeparam>
        /// <param name="list">
        /// The list whose backing array is to be set.
        /// </param>
        /// <param name="array">
        /// The array to install as the backing storage of the list.  Upon
        /// success, this is set to null.
        /// </param>
        /// <returns>
        /// True if the backing array was set; otherwise, false.
        /// </returns>
        public static bool SetArray<T>(
            List<T> list, /* in, out */
            ref T[] array /* in, out */
            )
        {
            if ((list == null) || (array == null))
                return false;

            if (noReflection)
                goto fallback;

            FieldInfo itemsFieldInfo;
            FieldInfo sizeFieldInfo;

            GetFieldInfos(list.GetType(),
                true, out itemsFieldInfo, out sizeFieldInfo);

            if ((itemsFieldInfo == null) || (sizeFieldInfo == null))
                return false;

            bool success = false;
            T[] savedItems = null;
            int? savedSize = null;

            try
            {
                savedItems = itemsFieldInfo.GetValue(list) as T[];
                savedSize = (int)sizeFieldInfo.GetValue(list);

                int length = array.Length;

                itemsFieldInfo.SetValue(list, array);
                sizeFieldInfo.SetValue(list, length);

                array = null;
                success = true;

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ArrayOps).Name,
                    TracePriority.MarshalError);
            }
            finally
            {
                if (!success &&
                    (savedItems != null) && (savedSize != null))
                {
                    try
                    {
                        itemsFieldInfo.SetValue(list, savedItems);
                        sizeFieldInfo.SetValue(list, savedSize);
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(ArrayOps).Name,
                            TracePriority.CleanupError);
                    }
                }
            }

            //
            // NOTE: Use the slow way of doing things.  This should
            //       always work.
            //
        fallback:

            list.Clear();
            list.AddRange(array);

            array = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the internal backing array of the specified list,
        /// optionally resizing it to match the element count of the list.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list.
        /// </typeparam>
        /// <param name="list">
        /// The list whose backing array is to be returned.
        /// </param>
        /// <param name="resize">
        /// When non-zero, the returned array is resized to the element count of
        /// the list.
        /// </param>
        /// <returns>
        /// The backing array of the list, or null when the list is null.
        /// </returns>
        public static T[] GetArray<T>(
            List<T> list, /* in */
            bool resize   /* in */
            )
        {
            FieldInfo fieldInfo = null;

            return GetArray<T>(list, resize, ref fieldInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the internal backing array of the specified list,
        /// optionally resizing it and caching the reflected field information
        /// for subsequent calls.  When reflection is disabled or fails, the
        /// slower public methods are used instead.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the list.
        /// </typeparam>
        /// <param name="list">
        /// The list whose backing array is to be returned.
        /// </param>
        /// <param name="resize">
        /// When non-zero, the returned array is resized to the element count of
        /// the list.
        /// </param>
        /// <param name="fieldInfo">
        /// The cached reflected field information for the items field.  When
        /// null upon entry, it is looked up and, upon return, set to the located
        /// field information so it may be reused.
        /// </param>
        /// <returns>
        /// The backing array of the list, or null when the list is null.
        /// </returns>
        public static T[] GetArray<T>(
            List<T> list,           /* in */
            bool resize,            /* in */
            ref FieldInfo fieldInfo /* in, out */
            )
        {
            if (list == null)
                return null;

            if (noReflection)
                goto fallback;

            FieldInfo localFieldInfo;

            if (fieldInfo != null)
            {
                localFieldInfo = fieldInfo; /* CACHED? */
            }
            else
            {
                GetFieldInfo(
                    list.GetType(), out localFieldInfo);

                fieldInfo = localFieldInfo;
            }

            if (localFieldInfo != null)
            {
                try
                {
                    T[] array = localFieldInfo.GetValue(list) as T[];

                    if (array != null)
                    {
                        if (resize)
                            Array.Resize(ref array, list.Count);

                        return array;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(ArrayOps).Name,
                        TracePriority.MarshalError);
                }
            }

            //
            // NOTE: Use the slow way of doing things.  This should
            //       always work.
            //
        fallback:

            return list.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a range of elements from the specified array to
        /// the specified list, creating the list when it does not yet exist.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the array and list.
        /// </typeparam>
        /// <param name="array">
        /// The array containing the elements to append.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first array element to append.
        /// </param>
        /// <param name="length">
        /// The number of array elements to append, or
        /// <see cref="Length.Invalid" /> to use the full length of the array.
        /// </param>
        /// <param name="list">
        /// The list to append the elements to.  When null, a new list is
        /// created and returned via this parameter.
        /// </param>
        /// <returns>
        /// True if the range was valid and the elements were appended;
        /// otherwise, false.
        /// </returns>
        public static bool AppendArray<T>(
            T[] array,       /* in */
            int startIndex,  /* in */
            int length,      /* in */
            ref List<T> list /* in, out */
            )
        {
            if (array == null)
                return false;

            int oldLength = array.Length;

            if ((startIndex < 0) || (startIndex >= oldLength))
                return false;

            if (length == Length.Invalid)
                length = oldLength;

            if ((length < 0) || (length > oldLength))
                return false;

            int stopIndex = startIndex + length;

            if ((stopIndex < 0) || (stopIndex >= oldLength))
                return false;

            if ((startIndex == 0) && (length == oldLength))
            {
                if (list != null)
                    list.AddRange(array);
                else
                    list = new List<T>(array);
            }
            else if (length > 0)
            {
                T[] newArray = new T[length];

                Array.Copy(
                    array, startIndex, newArray, 0, length);

                if (list != null)
                    list.AddRange(newArray);
                else
                    list = new List<T>(newArray);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bounds of a (possibly multi-dimensional)
        /// array, initializing the per-rank lower bounds, lengths, and starting
        /// indexes used to iterate over all of its elements.
        /// </summary>
        /// <param name="array">
        /// The array whose bounds are to be computed.
        /// </param>
        /// <param name="rank">
        /// The number of dimensions to use.  When zero upon entry, it is set to
        /// the rank of the array.
        /// </param>
        /// <param name="lowerBounds">
        /// Upon success, receives the lower bound of each dimension.  When
        /// non-null upon entry, it is resized; otherwise, it is allocated.
        /// </param>
        /// <param name="lengths">
        /// Upon success, receives the length of each dimension.  When non-null
        /// upon entry, it is resized; otherwise, it is allocated.
        /// </param>
        /// <param name="indexes">
        /// Upon success, receives the initial per-dimension indexes (set to the
        /// lower bounds).  When non-null upon entry, it is resized; otherwise,
        /// it is allocated.
        /// </param>
        /// <returns>
        /// True if the bounds were computed; otherwise, false.
        /// </returns>
        public static bool GetBounds(
            Array array,           /* in */
            ref int rank,          /* in, out */
            ref int[] lowerBounds, /* in, out */
            ref int[] lengths,     /* in, out */
            ref int[] indexes      /* in, out */
            )
        {
            if (array == null)
                return false;

            if (rank == 0)
                rank = array.Rank;

            if (rank <= 0)
                return false;

            if (lowerBounds != null)
                Array.Resize(ref lowerBounds, rank);
            else
                lowerBounds = new int[rank];

            if (lengths != null)
                Array.Resize(ref lengths, rank);
            else
                lengths = new int[rank];

            if (indexes != null)
                Array.Resize(ref indexes, rank);
            else
                indexes = new int[rank];

            //
            // NOTE: Setup all the lower bounds, lengths, and indexes to
            //       their initial states.
            //
            for (int rankIndex = 0; rankIndex < rank; rankIndex++)
            {
                //
                // NOTE: Get the bounds for each rank because we must
                //       iterate over all the elements in the array.
                //
                lowerBounds[rankIndex] = array.GetLowerBound(rankIndex);
                lengths[rankIndex] = array.GetLength(rankIndex);

                //
                // NOTE: Always set initial indexes to the lower bound.
                //
                indexes[rankIndex] = lowerBounds[rankIndex];
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value of the array element at the specified
        /// index, returning a default value when the array is null or the index
        /// is out of range.
        /// </summary>
        /// <param name="array">
        /// The array to read from.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the element to read.
        /// </param>
        /// <param name="default">
        /// The value to return when the array is null or the index is out of
        /// range.
        /// </param>
        /// <returns>
        /// The value of the element at the specified index, or
        /// <paramref name="default" /> when it cannot be read.
        /// </returns>
        public static object GetValue(
            Array array,
            int index,
            object @default
            )
        {
            if (array == null)
                return @default;

            if ((index < 0) || (index >= array.Length))
                return @default;

            return array.GetValue(index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method advances the per-dimension indexes used to iterate over
        /// the elements of a (possibly multi-dimensional) array, carrying from
        /// the least significant rank to the most significant rank as needed.
        /// </summary>
        /// <param name="rank">
        /// The number of dimensions over which to iterate.
        /// </param>
        /// <param name="lowerBounds">
        /// The lower bound of each dimension.
        /// </param>
        /// <param name="lengths">
        /// The length of each dimension.
        /// </param>
        /// <param name="indexes">
        /// The current per-dimension indexes, which are advanced in place by
        /// this method.
        /// </param>
        /// <returns>
        /// True if the indexes were advanced; false when there are no more
        /// elements (i.e. the final iteration has completed), which is expected
        /// and not technically a failure.
        /// </returns>
        public static bool IncrementIndexes(
            int rank,          /* in */
            int[] lowerBounds, /* in */
            int[] lengths,     /* in */
            int[] indexes      /* in, out */
            )
        {
#if false
            if ((lowerBounds == null) || (lengths == null) ||
                (indexes == null))
            {
                return false;
            }

            if ((lowerBounds.Length != lengths.Length) ||
                (lowerBounds.Length != indexes.Length))
            {
                return false;
            }

            if ((rank <= 0) || (rank > lowerBounds.Length))
                return false;
#endif

            //
            // NOTE: Determine the index of the "least significant" rank.
            //
            int rankIndex = rank - 1;

            //
            // NOTE: Keep going forever (i.e. until the loop is terminated
            //       from within).
            //
            while (true)
            {
                //
                // NOTE: Can the index of the current rank NOT be advanced
                //       without overflowing its bounds?
                //
                if (indexes[rankIndex] >=
                        (lowerBounds[rankIndex] + lengths[rankIndex] - 1))
                {
                    //
                    // NOTE: Ok, there would be an overflow; therefore, reset
                    //       the index of the current rank to its lower bound
                    //       and then advance to the next rank.
                    //
                    if (rankIndex > 0)
                    {
                        indexes[rankIndex] = lowerBounds[rankIndex];
                        rankIndex--;
                    }
                    else
                    {
                        //
                        // NOTE: No more ranks.  This condition is expected to
                        //       occur during the last iteration of loops in
                        //       the caller therefore, this is not technically
                        //       a "failure", per se.
                        //
                        return false;
                    }
                }

                //
                // NOTE: Increment the index for the current rank and return
                //       success.
                //
                indexes[rankIndex]++;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the value of the array element at the
        /// specified index without throwing when the array is null or the index
        /// is out of range.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the array.
        /// </typeparam>
        /// <param name="array">
        /// The array to read from.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the element to read.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value of the element at the specified
        /// index; otherwise, this is set to the default value of the element
        /// type.
        /// </param>
        /// <returns>
        /// True if the element was read; otherwise, false.
        /// </returns>
        public static bool TryGet<T>(
            T[] array,
            int index,
            out T value
            )
        {
            if (array == null)
            {
                value = default(T);
                return false;
            }

            if ((index < 0) || (index >= array.Length))
            {
                value = default(T);
                return false;
            }

            value = array[index];
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a shallow copy of the specified byte array.
        /// </summary>
        /// <param name="bytes">
        /// The byte array to copy.
        /// </param>
        /// <returns>
        /// A new byte array containing the same bytes, or null when the input
        /// array is null.
        /// </returns>
        public static byte[] Copy(
            byte[] bytes
            )
        {
            if (bytes == null)
                return null;

            int length = bytes.Length;
            byte[] result = new byte[length]; /* throw */

            Array.Copy(bytes, result, length);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a deep copy of a (possibly multi-dimensional)
        /// array, preserving its element type, rank, lower bounds, and lengths.
        /// </summary>
        /// <param name="array">
        /// The array to copy.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// A new array that is a deep copy of the input array, or null on
        /// failure.
        /// </returns>
        public static Array DeepCopy(
            Array array,     /* in */
            ref Result error /* out */
            )
        {
            if (array == null)
            {
                error = "invalid existing array";
                return null;
            }

            Type type = array.GetType();

            if (type == null)
            {
                error = "invalid array type";
                return null;
            }

            Type elementType = type.GetElementType();

            if (elementType == null)
            {
                error = "invalid array element type";
                return null;
            }

            int rank = 0;
            int[] lowerBounds = null;
            int[] lengths = null;
            int[] indexes = null;

            if (!GetBounds(
                    array, ref rank, ref lowerBounds,
                    ref lengths, ref indexes))
            {
                error = String.Format(
                    "could not get bounds for rank {0} array",
                    rank);

                return null;
            }

            try
            {
                Array localArray = Array.CreateInstance(
                    elementType, lengths, lowerBounds);

                int length = array.Length;

                for (int unused = 0; unused < length; unused++)
                {
                    localArray.SetValue(
                        array.GetValue(indexes), indexes);

                    IncrementIndexes(
                        rank, lowerBounds, lengths, indexes);
                }

                return localArray;
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a hexadecimal string into an array of bytes,
        /// optionally tolerating a leading "0x" prefix.
        /// </summary>
        /// <param name="value">
        /// The hexadecimal string to convert.  It must contain an even number
        /// of characters.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing each pair of hexadecimal digits.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the array of bytes parsed from the string.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetBytesFromHexadecimalString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (value == null)
            {
                error = "invalid string";
                return ReturnCode.Error;
            }

            int length = value.Length;

            if ((length % 2) != 0)
            {
                error = "string must have an even number of characters";
                return ReturnCode.Error;
            }

            int offset = 0;

            if ((length >= 2) && (value[0] == Characters.Zero) &&
                ((value[1] == Characters.X) || (value[1] == Characters.x)))
            {
                offset += 2;
            }

            byte[] localBytes = new byte[(length - offset) / 2];

            for (int index = 0; (index + offset) < length; index += 2)
            {
                byte byteValue = 0;
                Result localError = null;

                if (Value.GetByte2(String.Format(
                        "0x{0}", value.Substring(index + offset, 2)),
                        ValueFlags.AnyByte, cultureInfo, ref byteValue,
                        ref localError) == ReturnCode.Ok)
                {
                    localBytes[index / 2] = byteValue;
                }
                else
                {
                    error = localError;
                    return ReturnCode.Error;
                }
            }

            bytes = localBytes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a string of delimited byte values into an array
        /// of bytes, splitting on whitespace and comma characters.
        /// </summary>
        /// <param name="value">
        /// The delimited string of byte values to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing each byte value.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the array of bytes parsed from the string.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetBytesFromDelimitedString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (value == null)
            {
                error = "invalid string";
                return ReturnCode.Error;
            }

            string[] values = value.Split(
                byteSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (values == null)
            {
                error = "could not split string";
                return ReturnCode.Error;
            }

            int length = values.Length;
            byte[] localBytes = new byte[length];

            for (int index = 0; index < length; index++)
            {
                byte byteValue = 0;
                Result localError = null;

                if (Value.GetByte2(
                        values[index], ValueFlags.AnyByte, cultureInfo,
                        ref byteValue, ref localError) == ReturnCode.Ok)
                {
                    localBytes[index] = byteValue;
                }
                else
                {
                    error = localError;
                    return ReturnCode.Error;
                }
            }

            bytes = localBytes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// This method converts a list of values into an array of bytes.  When
        /// the list contains a single element that names an opaque object, the
        /// underlying byte array or encoded string of that object is used;
        /// otherwise, each element is parsed as an individual byte value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to resolve a single-element list as an opaque
        /// object and to supply the culture for parsing.  This value may be
        /// null.
        /// </param>
        /// <param name="list">
        /// The list of values to convert.
        /// </param>
        /// <param name="encoding">
        /// The encoding used when the single resolved object is a string.  When
        /// null, the encoding for <paramref name="type" /> is used.
        /// </param>
        /// <param name="type">
        /// The encoding type used to select an encoding when
        /// <paramref name="encoding" /> is null.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the array of bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetBytesFromList(
            Interpreter interpreter,
            StringList list,
            Encoding encoding,
            EncodingType type,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (list == null)
            {
                error = "invalid list";
                return ReturnCode.Error;
            }

            if (list.Count == 0)
            {
                bytes = new byte[0];
                return ReturnCode.Ok;
            }

            if ((list.Count == 1) && (interpreter != null))
            {
                IObject @object = null;

                if (interpreter.GetObject(
                        list[0], LookupFlags.NoVerbose,
                        ref @object) == ReturnCode.Ok)
                {
                    object value = @object.Value;

                    if (value == null)
                    {
                        bytes = null;
                        return ReturnCode.Ok;
                    }
                    else if (value is byte[])
                    {
                        bytes = (byte[])value;
                        return ReturnCode.Ok;
                    }
                    else if (value is string)
                    {
                        if (encoding == null)
                            encoding = StringOps.GetEncoding(type);

                        if (encoding != null)
                        {
                            bytes = encoding.GetBytes((string)value);
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "invalid encoding";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "object \"{0}\" type mismatch, have {1}, want {2}",
                            list[0], FormatOps.TypeName(value),
                            FormatOps.TypeName(typeof(byte[])));

                        return ReturnCode.Error;
                    }
                }
            }

            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            byte[] localBytes = new byte[list.Count];

            for (int index = 0; index < list.Count; index++)
            {
                if (Value.GetByte2(
                        list[index], ValueFlags.AnyByte,
                        cultureInfo, ref localBytes[index],
                        ref error) != ReturnCode.Ok)
                {
                    error = String.Format(
                        "bad byte value at index {0}: {1}",
                        index, error);

                    return ReturnCode.Error;
                }
            }

            bytes = localBytes;
            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects a random element from a one-dimensional array,
        /// using the interpreter random number generator when available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose random number generator is used.  When null,
        /// the global runtime random number generator is used instead.
        /// </param>
        /// <param name="array">
        /// The one-dimensional, non-empty array from which to select an element.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the randomly selected element value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SelectRandomValue(
            Interpreter interpreter, /* in: may be NULL. */
            Array array,             /* in */
            ref object value,        /* out */
            ref Result error         /* out */
            )
        {
            if (array == null)
            {
                error = "invalid array";
                return ReturnCode.Error;
            }

            if (array.Rank != 1)
            {
                error = "array must be one-dimensional";
                return ReturnCode.Error;
            }

            if (array.Length == 0)
            {
                error = "array cannot be empty";
                return ReturnCode.Error;
            }

            try
            {
                ulong randomNumber;

                if (interpreter != null)
                    randomNumber = interpreter.GetRandomNumber(); /* throw */
                else
                    randomNumber = RuntimeOps.GetRandomNumber(); /* throw */

                int index = ConversionOps.ToInt(randomNumber %
                    ConversionOps.ToULong(array.LongLength));

                value = array.GetValue(index); /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of an array, optionally starting at the
        /// specified index so that the leading elements are omitted.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the array.
        /// </typeparam>
        /// <param name="array">
        /// The array to copy.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first element to copy.  When less than or equal to
        /// zero, the entire array is copied.
        /// </param>
        /// <returns>
        /// A new array containing the copied elements, or null when the input
        /// array is null or the start index is beyond the end of the array.
        /// </returns>
        public static T[] Copy<T>(
            T[] array,
            int startIndex
            )
        {
            if (array == null)
                return null;

            T[] result;
            int length = array.Length;

            if (startIndex <= 0)
            {
                result = new T[length];
                Array.Copy(array, result, length);

                return result;
            }

            if (startIndex >= length)
                return null;

            length -= startIndex;
            result = new T[length];

            if (length > 0)
                Array.Copy(array, startIndex, result, 0, length);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts an array of nullable value-type elements into
        /// an array of the corresponding non-nullable elements, substituting a
        /// default value for any null entries.
        /// </summary>
        /// <typeparam name="T">
        /// The value type of the array elements.
        /// </typeparam>
        /// <param name="array">
        /// The array of nullable elements to convert.
        /// </param>
        /// <param name="default">
        /// The value used in place of any null element.
        /// </param>
        /// <returns>
        /// A new array of non-nullable elements, or null when the input array
        /// is null.
        /// </returns>
        public static T[] ToNonNullable<T>(
            T?[] array,
            T @default
            ) where T : struct
        {
            if (array == null)
                return null;

            Array result = Array.CreateInstance(
                typeof(T), array.Length);

            for (int index = array.GetLowerBound(0);
                    index <= array.GetUpperBound(0); index++)
            {
                if (array[index] != null)
                    result.SetValue(array[index], index);
                else
                    result.SetValue(@default, index);
            }

            return (T[])result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two byte arrays contain the same
        /// sequence of bytes over their full lengths.
        /// </summary>
        /// <param name="array1">
        /// The first byte array to compare.
        /// </param>
        /// <param name="array2">
        /// The second byte array to compare.
        /// </param>
        /// <returns>
        /// True if the arrays are equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            byte[] array1,
            byte[] array2
            )
        {
            return Equals(array1, array2, Length.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two byte arrays contain the same
        /// sequence of bytes over the specified number of leading bytes.
        /// </summary>
        /// <param name="array1">
        /// The first byte array to compare.
        /// </param>
        /// <param name="array2">
        /// The second byte array to compare.
        /// </param>
        /// <param name="length">
        /// The number of leading bytes to compare, or
        /// <see cref="Length.Invalid" /> to compare the full lengths.
        /// </param>
        /// <returns>
        /// True if the arrays are equal over the compared bytes; otherwise,
        /// false.
        /// </returns>
        public static bool Equals(
            byte[] array1,
            byte[] array2,
            int length
            )
        {
            return GenericCompareOps<byte>.Equals(array1, array2, length);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two byte arrays contain the same
        /// sequence of bytes over the specified range.
        /// </summary>
        /// <param name="array1">
        /// The first byte array to compare.
        /// </param>
        /// <param name="array2">
        /// The second byte array to compare.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first byte to compare in each array.
        /// </param>
        /// <param name="length">
        /// The number of bytes to compare, or <see cref="Length.Invalid" /> to
        /// compare through the end of the arrays.
        /// </param>
        /// <returns>
        /// True if the arrays are equal over the compared range; otherwise,
        /// false.
        /// </returns>
        public static bool Equals(
            byte[] array1,
            byte[] array2,
            int startIndex,
            int length
            )
        {
            return GenericCompareOps<byte>.Equals(
                array1, array2, startIndex, length);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the index of the first occurrence of one byte
        /// array within another byte array.
        /// </summary>
        /// <param name="array1">
        /// The byte array to search within.
        /// </param>
        /// <param name="array2">
        /// The byte array to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of
        /// <paramref name="array2" /> within <paramref name="array1" />, or
        /// <see cref="Index.Invalid" /> when it is not found.
        /// </returns>
        public static int IndexOf(
            byte[] array1,
            byte[] array2
            )
        {
            if ((array1 == null) || (array2 == null))
                return Index.Invalid;

            int length1 = array1.Length;
            int length2 = array2.Length;

            if ((length1 == 0) || (length2 == 0))
                return Index.Invalid;
            else if (length2 > length1)
                return Index.Invalid;

            if (length1 == length2)
            {
                for (int index0 = 0; index0 < length1; index0++)
                    if (array1[index0] != array2[index0])
                        return Index.Invalid;

                return 0;
            }
            else
            {
                int index1 = 0;
                int index2;

                while (true)
                {
                    index1 = Array.IndexOf(array1, array2[0], index1);

                    if (index1 == Index.Invalid)
                        return Index.Invalid;

                    if ((index1 + length2) > length1)
                        return Index.Invalid;

                    int savedIndex1 = index1;

                    index1++;
                    index2 = 1;

                    for (; index2 < length2; index1++, index2++)
                        if (array1[index1] != array2[index2])
                            break;

                    if (index2 == length2)
                        return savedIndex1;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        /// <summary>
        /// This method computes a hash code for a byte array over its full
        /// length.
        /// </summary>
        /// <param name="array">
        /// The byte array to hash.
        /// </param>
        /// <returns>
        /// The computed hash code for the byte array.
        /// </returns>
        public static int GetHashCode(
            byte[] array
            )
        {
            return GenericCompareOps<byte>.GetHashCode(array, Length.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash code for a byte array over the specified
        /// number of leading bytes.
        /// </summary>
        /// <param name="array">
        /// The byte array to hash.
        /// </param>
        /// <param name="length">
        /// The number of leading bytes to include in the hash, or
        /// <see cref="Length.Invalid" /> to include the full length.
        /// </param>
        /// <returns>
        /// The computed hash code for the byte array.
        /// </returns>
        public static int GetHashCode(
            byte[] array,
            int length
            )
        {
            return GenericCompareOps<byte>.GetHashCode(array, length);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string contains any
        /// character that requires two bytes to represent, returning the
        /// two-byte encoded bytes as a side effect.
        /// </summary>
        /// <param name="value">
        /// The string to examine.
        /// </param>
        /// <param name="bytes">
        /// Receives the two-byte encoded bytes of the string.
        /// </param>
        /// <returns>
        /// True if the string contains at least one two-byte character;
        /// otherwise, false.
        /// </returns>
        private static bool HasTwoByteCharacter(
            string value,
            ref byte[] bytes
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            if (twoByteEncoding == null)
                return false;

            bytes = twoByteEncoding.GetBytes(value);

            if (bytes == null)
                return false;

            int length = bytes.Length;

            if (length == 0)
                return false;

            if ((length % 2) != 0)
                return false;

            int zeroOffset = (bytes[0] != 0) ? 1 : 0;

            for (int index = 0; index < length; index += 2)
                if (bytes[index + zeroOffset] != 0)
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a string into its hexadecimal representation,
        /// using a two-byte encoding when the string contains any two-byte
        /// characters and a one-byte encoding otherwise.
        /// </summary>
        /// <param name="value">
        /// The string to convert.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the string, or null when it cannot
        /// be produced.
        /// </returns>
        public static string ToHexadecimalString(
            string value
            )
        {
            byte[] bytes = null;

            if (HasTwoByteCharacter(value, ref bytes))
                return ToHexadecimalString(bytes);
            else if (oneByteEncoding != null)
                return ToHexadecimalString(oneByteEncoding.GetBytes(value));
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts each string in a collection into its
        /// hexadecimal representation and returns the results as a list-formatted
        /// string.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to convert.
        /// </param>
        /// <returns>
        /// A list-formatted string of the hexadecimal representations, or null
        /// when the collection is null.
        /// </returns>
        public static string ToHexadecimalString(
            IEnumerable<string> collection
            )
        {
            if (collection == null)
                return null;

            StringList list = new StringList();

            foreach (string item in collection)
                list.Add(ToHexadecimalString(item));

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a byte array into its hexadecimal string
        /// representation using lowercase digits.
        /// </summary>
        /// <param name="array">
        /// The byte array to convert.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the byte array, or null when the
        /// array is null.
        /// </returns>
        public static string ToHexadecimalString(
            byte[] array
            )
        {
            return ToHexadecimalString(array, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a byte array into its hexadecimal string
        /// representation, optionally forcing the result to uppercase.
        /// </summary>
        /// <param name="array">
        /// The byte array to convert.
        /// </param>
        /// <param name="noCase">
        /// When non-zero, the resulting string is converted to uppercase.
        /// </param>
        /// <returns>
        /// The hexadecimal representation of the byte array, or null when the
        /// array is null.
        /// </returns>
        public static string ToHexadecimalString(
            byte[] array,
            bool noCase
            )
        {
            if (array == null)
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

            int length = array.Length;

            for (int index = 0; index < length; index++)
                builder.Append(FormatOps.Hexadecimal(array[index], false));

            string result = StringBuilderCache.GetStringAndRelease(
                ref builder);

            if (noCase && (result != null))
                result = result.ToUpperInvariant();

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a byte array into a string by treating each
        /// byte as the character code of a single character.
        /// </summary>
        /// <param name="array">
        /// The byte array to convert.
        /// </param>
        /// <returns>
        /// The raw string built from the bytes, or null when the array is null.
        /// </returns>
        public static string ToRawString(
            byte[] array
            )
        {
            if (array == null)
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

            int length = array.Length;

            for (int index = 0; index < length; index++)
                builder.Append(ConversionOps.ToChar(array[index]));

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts a byte array into a string by formatting each
        /// byte value, optionally separating the values with spaces and forcing
        /// the result to uppercase.
        /// </summary>
        /// <param name="array">
        /// The byte array to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when formatting each byte value.  This value may be
        /// null.
        /// </param>
        /// <param name="format">
        /// The numeric format string applied to each byte value.  This value
        /// may be null.
        /// </param>
        /// <param name="spaces">
        /// When non-zero, a space is inserted between successive byte values.
        /// </param>
        /// <param name="noCase">
        /// When non-zero, the resulting string is converted to uppercase.
        /// </param>
        /// <returns>
        /// The formatted string built from the bytes, or null when the array is
        /// null.
        /// </returns>
        public static string ToString(
            byte[] array,
            CultureInfo cultureInfo,
            string format,
            bool spaces,
            bool noCase
            )
        {
            if (array == null)
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

            int length = array.Length;

            for (int index = 0; index < length; index++)
            {
                if (spaces && (index > 0))
                    builder.Append(Characters.Space);

                byte value = array[index];

                if (cultureInfo != null)
                {
                    builder.Append((format != null) ?
                        value.ToString(format, cultureInfo) :
                        value.ToString(cultureInfo));
                }
                else
                {
                    builder.Append((format != null) ?
                        value.ToString(format) :
                        value.ToString());
                }
            }

            string result = StringBuilderCache.GetStringAndRelease(
                ref builder);

            if (noCase && (result != null))
                result = result.ToUpperInvariant();

            return result;
        }
    }
}
