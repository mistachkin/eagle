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

namespace Eagle._Components.Private
{
    [ObjectId("e6709468-95a4-405e-8c9c-e0dbd1aa3a88")]
    internal static class ArrayOps
    {
        #region Private Constants
        private static char[] byteSeparators = {
            Characters.HorizontalTab, Characters.LineFeed,
            Characters.VerticalTab, Characters.FormFeed,
            Characters.CarriageReturn, Characters.Space,
            Characters.Comma
        };

        ///////////////////////////////////////////////////////////////////////

        private static Encoding oneByteEncoding = OneByteEncoding.OneByte;
        private static Encoding twoByteEncoding = TwoByteEncoding.TwoByte;

        ///////////////////////////////////////////////////////////////////////

        private static string itemsFieldName = "_items"; /* NOTE: Also Mono. */
        private static string sizeFieldName = "_size"; /* NOTE: Also Mono. */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static bool noReflection = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.WriteEngineInfo method.
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

        public static T[] GetArray<T>(
            List<T> list, /* in */
            bool resize   /* in */
            )
        {
            FieldInfo fieldInfo = null;

            return GetArray<T>(list, resize, ref fieldInfo);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static ReturnCode GetBytesFromList(
            Interpreter interpreter,
            StringList list,
            Encoding encoding,
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

        public static bool Equals(
            byte[] array1,
            byte[] array2
            )
        {
            return Equals(array1, array2, Length.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Equals(
            byte[] array1,
            byte[] array2,
            int length
            )
        {
            return GenericCompareOps<byte>.Equals(array1, array2, length);
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static int GetHashCode(
            byte[] array
            )
        {
            return GenericCompareOps<byte>.GetHashCode(array, Length.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetHashCode(
            byte[] array,
            int length
            )
        {
            return GenericCompareOps<byte>.GetHashCode(array, length);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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

        public static string ToHexadecimalString(
            byte[] array
            )
        {
            return ToHexadecimalString(array, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToHexadecimalString(
            byte[] array,
            bool noCase
            )
        {
            if (array == null)
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();

            int length = array.Length;

            for (int index = 0; index < length; index++)
                builder.Append(FormatOps.Hexadecimal(array[index], false));

            string result = builder.ToString();

            if (noCase && (result != null))
                result = result.ToUpperInvariant();

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

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

            StringBuilder builder = StringOps.NewStringBuilder();

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

            string result = builder.ToString();

            if (noCase && (result != null))
                result = result.ToUpperInvariant();

            return result;
        }
    }
}
