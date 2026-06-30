/*
 * String.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by string-like entities and exposes a
    /// subset of the standard string operations (searching, slicing, trimming,
    /// etc.) common to the Argument and Result classes, allowing them to be
    /// manipulated uniformly without exposing their backing storage.
    /// </summary>
    [ObjectId("4c20e212-18ea-4321-802d-11f636e4c6e4")]
    internal interface IString
    {
        //
        // NOTE: This interface only contains the bare minimum requirements 
        //       common to the Argument and Result classes.  It may need to 
        //       be added to later.
        //
        /// <summary>
        /// This method reports the index of the first occurrence of the
        /// specified value within this string.
        /// </summary>
        /// <param name="value">
        /// The string to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when searching.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of
        /// <paramref name="value" />, or -1 if it is not found.
        /// </returns>
        int IndexOf(string value, StringComparison comparisonType);

        /// <summary>
        /// This method reports the index of the first occurrence of the
        /// specified value within this string, starting the search at the
        /// specified index.
        /// </summary>
        /// <param name="value">
        /// The string to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin the search.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when searching.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of
        /// <paramref name="value" /> at or after
        /// <paramref name="startIndex" />, or -1 if it is not found.
        /// </returns>
        int IndexOf(string value, int startIndex, StringComparison comparisonType);

        /// <summary>
        /// This method reports the index of the last occurrence of the
        /// specified value within this string.
        /// </summary>
        /// <param name="value">
        /// The string to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when searching.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of
        /// <paramref name="value" />, or -1 if it is not found.
        /// </returns>
        int LastIndexOf(string value, StringComparison comparisonType);

        /// <summary>
        /// This method reports the index of the last occurrence of the
        /// specified value within this string, searching backward from the
        /// specified index.
        /// </summary>
        /// <param name="value">
        /// The string to search for.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin the backward search.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when searching.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of
        /// <paramref name="value" /> at or before
        /// <paramref name="startIndex" />, or -1 if it is not found.
        /// </returns>
        int LastIndexOf(string value, int startIndex, StringComparison comparisonType);

        /// <summary>
        /// This method determines whether this string begins with the
        /// specified value.
        /// </summary>
        /// <param name="value">
        /// The string to compare against the start of this string.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when comparing.
        /// </param>
        /// <returns>
        /// True if this string begins with <paramref name="value" />;
        /// otherwise, false.
        /// </returns>
        bool StartsWith(string value, StringComparison comparisonType);

        /// <summary>
        /// This method determines whether this string ends with the specified
        /// value.
        /// </summary>
        /// <param name="value">
        /// The string to compare against the end of this string.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when comparing.
        /// </param>
        /// <returns>
        /// True if this string ends with <paramref name="value" />; otherwise,
        /// false.
        /// </returns>
        bool EndsWith(string value, StringComparison comparisonType);

        /// <summary>
        /// This method extracts a substring beginning at the specified index
        /// and continuing to the end of this string.
        /// </summary>
        /// <param name="startIndex">
        /// The zero-based index at which the substring begins.
        /// </param>
        /// <returns>
        /// The substring beginning at <paramref name="startIndex" />.
        /// </returns>
        string Substring(int startIndex);

        /// <summary>
        /// This method extracts a substring of the specified length beginning
        /// at the specified index.
        /// </summary>
        /// <param name="startIndex">
        /// The zero-based index at which the substring begins.
        /// </param>
        /// <param name="length">
        /// The number of characters to include in the substring.
        /// </param>
        /// <returns>
        /// The substring of <paramref name="length" /> characters beginning at
        /// <paramref name="startIndex" />.
        /// </returns>
        string Substring(int startIndex, int length);

        /// <summary>
        /// This method determines whether the specified value occurs within
        /// this string.
        /// </summary>
        /// <param name="value">
        /// The string to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison rules to use when searching.
        /// </param>
        /// <returns>
        /// True if <paramref name="value" /> occurs within this string;
        /// otherwise, false.
        /// </returns>
        bool Contains(string value, StringComparison comparisonType);

        /// <summary>
        /// This method returns a new string in which all occurrences of a
        /// specified value are replaced with another specified value.
        /// </summary>
        /// <param name="oldValue">
        /// The string to be replaced.
        /// </param>
        /// <param name="newValue">
        /// The string to replace all occurrences of
        /// <paramref name="oldValue" /> with.
        /// </param>
        /// <returns>
        /// A new string with every occurrence of <paramref name="oldValue" />
        /// replaced by <paramref name="newValue" />.
        /// </returns>
        string Replace(string oldValue, string newValue);

        /// <summary>
        /// This method returns a new string with all leading and trailing
        /// white-space characters removed.
        /// </summary>
        /// <returns>
        /// The trimmed string.
        /// </returns>
        string Trim();

        /// <summary>
        /// This method returns a new string with all leading and trailing
        /// occurrences of the specified characters removed.
        /// </summary>
        /// <param name="trimChars">
        /// The array of characters to remove, or null to remove white-space
        /// characters.
        /// </param>
        /// <returns>
        /// The trimmed string.
        /// </returns>
        string Trim(char[] trimChars);

        /// <summary>
        /// This method returns a new string with all leading occurrences of
        /// the specified characters removed.
        /// </summary>
        /// <param name="trimChars">
        /// The array of characters to remove, or null to remove white-space
        /// characters.
        /// </param>
        /// <returns>
        /// The trimmed string.
        /// </returns>
        string TrimStart(char[] trimChars);

        /// <summary>
        /// This method returns a new string with all trailing occurrences of
        /// the specified characters removed.
        /// </summary>
        /// <param name="trimChars">
        /// The array of characters to remove, or null to remove white-space
        /// characters.
        /// </param>
        /// <returns>
        /// The trimmed string.
        /// </returns>
        string TrimEnd(char[] trimChars);

        /// <summary>
        /// This method copies the characters of this string into a new
        /// character array.
        /// </summary>
        /// <returns>
        /// A character array containing the characters of this string.
        /// </returns>
        char[] ToCharArray();
    }
}
