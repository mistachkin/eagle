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
    [Guid("33405cbe-4da7-47ca-8411-26f06cf9f6b4")]
    internal static class GenericOps<T> where T : IComparable<T>
    {
        #region Generic Support Methods
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
