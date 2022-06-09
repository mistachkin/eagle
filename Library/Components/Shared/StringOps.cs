/*
 * StringOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;

#if !EAGLE
using System.Runtime.InteropServices;
#endif

using Eagle._Attributes;

namespace Eagle._Components.Shared
{
#if EAGLE
    [ObjectId("9c5b6597-aecd-4dce-bcdd-7f8fa94ce6d4")]
#else
    [Guid("9c5b6597-aecd-4dce-bcdd-7f8fa94ce6d4")]
#endif
    internal static class StringOps
    {
        #region Private Constants
        private static readonly StringComparison BinaryComparisonType =
            StringComparison.Ordinal;

        ///////////////////////////////////////////////////////////////////////

        private static readonly StringComparison BinaryNoCaseComparisonType =
            StringComparison.OrdinalIgnoreCase;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constants
        internal static readonly StringComparison SystemComparisonType =
            BinaryComparisonType;

        ///////////////////////////////////////////////////////////////////////

        internal static readonly StringComparison SystemNoCaseComparisonType =
            BinaryNoCaseComparisonType;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static StringComparison GetBinaryComparisonType(
            bool noCase
            )
        {
            return noCase ?
                BinaryNoCaseComparisonType : BinaryComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringComparison GetSystemComparisonType(
            bool noCase
            )
        {
            return noCase ?
                SystemNoCaseComparisonType : SystemComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int Compare(
            string left,
            string right,
            StringComparison comparisonType
            )
        {
            return String.Compare(left, right, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Equals(
            string left,
            string right,
            StringComparison comparisonType
            )
        {
            return String.Equals(left, right, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int Compare(
            string left,
            int leftIndex,
            string right,
            int rightIndex,
            int length,
            StringComparison comparisonType
            )
        {
            return String.Compare(
                left, leftIndex, right, rightIndex, length,
                comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool StartsWith(
            string value,
            string prefix,
            StringComparison comparisonType
            )
        {
            if ((value == null) || (prefix == null))
                return false;

            return value.StartsWith(prefix, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool StartsWith(
            string value,
            string prefix,
            CultureInfo cultureInfo,
            bool noCase
            )
        {
            if ((value == null) || (prefix == null))
                return false;

            return value.StartsWith(prefix, noCase, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool EndsWith(
            string value,
            string suffix,
            StringComparison comparisonType
            )
        {
            if ((value == null) || (suffix == null))
                return false;

            return value.EndsWith(suffix, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool EndsWith(
            string value,
            string suffix,
            CultureInfo cultureInfo,
            bool noCase
            )
        {
            if ((value == null) || (suffix == null))
                return false;

            return value.EndsWith(suffix, noCase, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static bool Equals(
            string left,
            int leftIndex,
            string right,
            int rightIndex,
            StringComparison comparisonType
            )
        {
            if ((left == null) || (right == null))
                return ((left == null) && (right == null));

            int length = Math.Min(left.Length, right.Length);

            return String.Compare(
                left, leftIndex, right, rightIndex, length,
                comparisonType) == 0;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static bool Equals(
            string left,
            int leftIndex,
            string right,
            int rightIndex,
            int length,
            StringComparison comparisonType
            )
        {
            return String.Compare(
                left, leftIndex, right, rightIndex, length,
                comparisonType) == 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int SystemCompare(
            string left,
            string right
            )
        {
            return String.Compare(
                left, right, SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemEquals(
            string left,
            string right
            )
        {
            return Equals(
                left, right, SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemNoCaseEquals(
            string left,
            string right
            )
        {
            return Equals(
                left, right, SystemNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemEquals(
            string left,
            string right,
            bool noCase
            )
        {
            return Equals(
                left, right, noCase ? SystemNoCaseComparisonType :
                SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemEquals(
            string left,
            int leftIndex,
            string right,
            int rightIndex,
            int length
            )
        {
            return String.Compare(
                left, leftIndex, right, rightIndex, length,
                SystemComparisonType) == 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemNoCaseEquals(
            string left,
            int leftIndex,
            string right,
            int rightIndex,
            int length
            )
        {
            return String.Compare(
                left, leftIndex, right, rightIndex, length,
                SystemNoCaseComparisonType) == 0;
        }
        #endregion
    }
}
