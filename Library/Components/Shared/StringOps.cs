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
    /// <summary>
    /// This class provides shared, low-level string comparison helper methods
    /// used throughout the Eagle software.  These helpers wrap the standard
    /// string comparison primitives so that consistent, ordinal-based semantics
    /// are applied across all assemblies that include this file.
    /// </summary>
#if EAGLE
    [ObjectId("9c5b6597-aecd-4dce-bcdd-7f8fa94ce6d4")]
#else
    [Guid("9c5b6597-aecd-4dce-bcdd-7f8fa94ce6d4")]
#endif
    internal static class StringOps
    {
        #region Private Constants
        /// <summary>
        /// The case-sensitive comparison type used for binary (ordinal) string
        /// comparisons.
        /// </summary>
        private static readonly StringComparison BinaryComparisonType =
            StringComparison.Ordinal;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The case-insensitive comparison type used for binary (ordinal)
        /// string comparisons.
        /// </summary>
        private static readonly StringComparison BinaryNoCaseComparisonType =
            StringComparison.OrdinalIgnoreCase;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constants
        /// <summary>
        /// The case-sensitive comparison type used for system string
        /// comparisons.
        /// </summary>
        internal static readonly StringComparison SystemComparisonType =
            BinaryComparisonType;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The case-insensitive comparison type used for system string
        /// comparisons.
        /// </summary>
        internal static readonly StringComparison SystemNoCaseComparisonType =
            BinaryNoCaseComparisonType;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method gets the binary (ordinal) string comparison type
        /// corresponding to the specified case sensitivity.
        /// </summary>
        /// <param name="noCase">
        /// Non-zero to select the case-insensitive comparison type.
        /// </param>
        /// <returns>
        /// The binary string comparison type for the requested case
        /// sensitivity.
        /// </returns>
        public static StringComparison GetBinaryComparisonType(
            bool noCase
            )
        {
            return noCase ?
                BinaryNoCaseComparisonType : BinaryComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the system string comparison type corresponding to
        /// the specified case sensitivity.
        /// </summary>
        /// <param name="noCase">
        /// Non-zero to select the case-insensitive comparison type.
        /// </param>
        /// <returns>
        /// The system string comparison type for the requested case
        /// sensitivity.
        /// </returns>
        public static StringComparison GetSystemComparisonType(
            bool noCase
            )
        {
            return noCase ?
                SystemNoCaseComparisonType : SystemComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two strings using the specified comparison
        /// type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The comparison type to use.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number, indicating the
        /// relative order of the two strings.
        /// </returns>
        public static int Compare(
            string left,
            string right,
            StringComparison comparisonType
            )
        {
            return String.Compare(left, right, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two strings are equal using the
        /// specified comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The comparison type to use.
        /// </param>
        /// <returns>
        /// True if the two strings are equal; otherwise, false.
        /// </returns>
        public static bool Equals(
            string left,
            string right,
            StringComparison comparisonType
            )
        {
            return String.Equals(left, right, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares sub-strings of two strings using the specified
        /// comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="leftIndex">
        /// The starting index of the sub-string within the first string.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="rightIndex">
        /// The starting index of the sub-string within the second string.
        /// </param>
        /// <param name="length">
        /// The maximum number of characters to compare.
        /// </param>
        /// <param name="comparisonType">
        /// The comparison type to use.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number, indicating the
        /// relative order of the two sub-strings.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value starts with the
        /// specified prefix, using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to look for.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The comparison type to use.
        /// </param>
        /// <returns>
        /// True if the value starts with the prefix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value starts with the
        /// specified prefix, using the specified culture and case sensitivity.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to look for.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the comparison.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive comparison.
        /// </param>
        /// <returns>
        /// True if the value starts with the prefix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value ends with the
        /// specified suffix, using the specified comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to look for.  This parameter may be null.
        /// </param>
        /// <param name="comparisonType">
        /// The comparison type to use.
        /// </param>
        /// <returns>
        /// True if the value ends with the suffix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value ends with the
        /// specified suffix, using the specified culture and case sensitivity.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to look for.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use for the comparison.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive comparison.
        /// </param>
        /// <returns>
        /// True if the value ends with the suffix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
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
        /// <summary>
        /// This method compares two strings, beginning at the specified index
        /// within each, using the specified comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.
        /// </param>
        /// <param name="leftIndex">
        /// The index within the first string at which to begin the comparison.
        /// </param>
        /// <param name="right">
        /// The second string to compare.
        /// </param>
        /// <param name="rightIndex">
        /// The index within the second string at which to begin the comparison.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing the two strings.
        /// </param>
        /// <returns>
        /// True if the two strings are considered equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether sub-strings of two strings are equal
        /// using the specified comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="leftIndex">
        /// The starting index of the sub-string within the first string.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="rightIndex">
        /// The starting index of the sub-string within the second string.
        /// </param>
        /// <param name="length">
        /// The number of characters to compare.
        /// </param>
        /// <param name="comparisonType">
        /// The comparison type to use.
        /// </param>
        /// <returns>
        /// True if the two sub-strings are equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method compares two strings using the case-sensitive system
        /// comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A negative number, zero, or a positive number, indicating the
        /// relative order of the two strings.
        /// </returns>
        public static int SystemCompare(
            string left,
            string right
            )
        {
            return String.Compare(
                left, right, SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two strings are equal using the
        /// case-sensitive system comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two strings are equal; otherwise, false.
        /// </returns>
        public static bool SystemEquals(
            string left,
            string right
            )
        {
            return Equals(
                left, right, SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two strings are equal using the
        /// case-insensitive system comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two strings are equal; otherwise, false.
        /// </returns>
        public static bool SystemNoCaseEquals(
            string left,
            string right
            )
        {
            return Equals(
                left, right, SystemNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two strings are equal using the
        /// system comparison type for the specified case sensitivity.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive comparison.
        /// </param>
        /// <returns>
        /// True if the two strings are equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether sub-strings of two strings are equal
        /// using the case-sensitive system comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="leftIndex">
        /// The starting index of the sub-string within the first string.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="rightIndex">
        /// The starting index of the sub-string within the second string.
        /// </param>
        /// <param name="length">
        /// The number of characters to compare.
        /// </param>
        /// <returns>
        /// True if the two sub-strings are equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether sub-strings of two strings are equal
        /// using the case-insensitive system comparison type.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  This parameter may be null.
        /// </param>
        /// <param name="leftIndex">
        /// The starting index of the sub-string within the first string.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  This parameter may be null.
        /// </param>
        /// <param name="rightIndex">
        /// The starting index of the sub-string within the second string.
        /// </param>
        /// <param name="length">
        /// The number of characters to compare.
        /// </param>
        /// <returns>
        /// True if the two sub-strings are equal; otherwise, false.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value starts with the
        /// specified prefix, using the case-sensitive system comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to look for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value starts with the prefix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
        public static bool SystemStartsWith(
            string value,
            string prefix
            )
        {
            if ((value == null) || (prefix == null))
                return false;

            return value.StartsWith(prefix, SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value starts with the
        /// specified prefix, using the case-insensitive system comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The prefix to look for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value starts with the prefix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
        public static bool SystemNoCaseStartsWith(
            string value,
            string prefix
            )
        {
            if ((value == null) || (prefix == null))
                return false;

            return value.StartsWith(prefix, SystemNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value ends with the
        /// specified suffix, using the case-sensitive system comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to look for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value ends with the suffix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
        public static bool SystemEndsWith(
            string value,
            string suffix
            )
        {
            if ((value == null) || (suffix == null))
                return false;

            return value.EndsWith(suffix, SystemComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified value ends with the
        /// specified suffix, using the case-insensitive system comparison type.
        /// </summary>
        /// <param name="value">
        /// The string to examine.  This parameter may be null.
        /// </param>
        /// <param name="suffix">
        /// The suffix to look for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the value ends with the suffix; otherwise, false.  Returns
        /// false when either string is null.
        /// </returns>
        public static bool SystemNoCaseEndsWith(
            string value,
            string suffix
            )
        {
            if ((value == null) || (suffix == null))
                return false;

            return value.EndsWith(suffix, SystemNoCaseComparisonType);
        }
        #endregion
    }
}
