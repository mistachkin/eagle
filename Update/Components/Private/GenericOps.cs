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
using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides generic helper methods for working with collections
    /// and arrays of comparable elements.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements; it must be comparable to itself.
    /// </typeparam>
    [Guid("33405cbe-4da7-47ca-8411-26f06cf9f6b4")]
    internal static class GenericOps<T> where T : IComparable<T>
    {
        #region Generic Support Methods
        /// <summary>
        /// This method determines whether a collection contains a particular
        /// value, comparing elements using their natural ordering and treating
        /// two null references as equal.
        /// </summary>
        /// <param name="collection">
        /// The collection to search.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The value to search for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the collection contains the value; otherwise, false.
        /// </returns>
        public static bool Contains(
            IEnumerable<T> collection,
            T value
            )
        {
            if (collection == null)
                return false;

            foreach (T item in collection)
            {
                if ((item != null) && (value != null))
                {
                    if (item.CompareTo(value) == 0)
                        return true;
                }
                else if ((item == null) && (value == null))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two arrays contain equal elements in
        /// the same order, comparing elements using their natural ordering and
        /// treating two null arrays (or two null elements) as equal.
        /// </summary>
        /// <param name="array1">
        /// The first array to compare.  This parameter may be null.
        /// </param>
        /// <param name="array2">
        /// The second array to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two arrays are equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            T[] array1,
            T[] array2
            )
        {
            if ((array1 == null) && (array2 == null))
                return true;

            if ((array1 == null) || (array2 == null))
                return false;

            int length = array1.Length;

            if (length != array2.Length)
                return false;

            for (int index = 0; index < length; index++)
            {
                T element1 = array1[index];
                T element2 = array2[index];

                if ((element1 != null) && (element2 != null))
                {
                    if (element1.CompareTo(element2) != 0)
                        return false;
                }
                else if ((element1 != null) || (element2 != null))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
