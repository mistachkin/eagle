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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

#if NET_40
using System.Numerics;
#endif

#if NATIVE && WINDOWS
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Encodings;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using SBF = Eagle._Components.Private.StringBuilderFactory;
using ObjectPair = System.Collections.Generic.KeyValuePair<string, object>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class is an internal static utility/helper class that provides a
    /// wide variety of string operations used throughout Eagle, including
    /// string comparison and equality, encoding detection and conversion,
    /// character classification, whitespace and line-ending normalization,
    /// the engines that implement the <c>[format]</c>, <c>[scan]</c>,
    /// <c>[string map]</c>, and <c>[string match]</c> commands, and
    /// base16/base26/base64 helper routines.
    /// </summary>
    [ObjectId("517405a1-bb12-4694-b937-30cb46b7c263")]
    internal static class StringOps
    {
        #region String Comparison Type Constants
        /// <summary>
        /// The default <see cref="StringComparison" /> used for culture-aware,
        /// case-sensitive string comparisons performed on behalf of the user.
        /// </summary>
        private static readonly StringComparison UserComparisonType =
            StringComparison.CurrentCulture;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default <see cref="StringComparison" /> used for culture-aware,
        /// case-insensitive string comparisons performed on behalf of the user.
        /// </summary>
        private static readonly StringComparison UserNoCaseComparisonType =
            StringComparison.CurrentCultureIgnoreCase;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default <see cref="StringComparer" /> used when an ordering or
        /// equality comparer for strings is needed.
        /// </summary>
        private static readonly StringComparer DefaultStringComparer =
            StringComparer.CurrentCulture;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default <see cref="CultureInfo" /> (the invariant culture) used
        /// for culture-sensitive string operations.
        /// </summary>
        private static readonly CultureInfo DefaultCultureInfo = CultureInfo.InvariantCulture;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region String Case Conversion Constants
        /// <summary>
        /// The name of the <c>ToLower</c> method, used when locating a
        /// case-conversion method via reflection.
        /// </summary>
        private const string ToLowerMethodName = "ToLower";
        /// <summary>
        /// The name of the <c>ToTitle</c> method, used when locating a
        /// case-conversion method via reflection.
        /// </summary>
        private const string ToTitleMethodName = "ToTitle";
        /// <summary>
        /// The name of the <c>ToUpper</c> method, used when locating a
        /// case-conversion method via reflection.
        /// </summary>
        private const string ToUpperMethodName = "ToUpper";
        /// <summary>
        /// The suffix (<c>Invariant</c>) appended to a case-conversion method
        /// name to select its culture-invariant variant.
        /// </summary>
        private const string InvariantMethodSuffix = "Invariant";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Encoding Constants
        /// <summary>
        /// The composite format string used to render a count as an eight-digit,
        /// zero-padded hexadecimal value.
        /// </summary>
        private static string CountFormat = "{0:X8}";

        /// <summary>
        /// The maximum character value (<c>0x7F</c>) considered to be a standard
        /// ASCII character.
        /// </summary>
        private const int CharMaxAscii = 0x7F;

        /// <summary>
        /// The name used to refer to the null encoding sentinel.
        /// </summary>
        internal static readonly string NullEncodingName = _String.Null;
        /// <summary>
        /// The name used to refer to the binary (one-byte) encoding.
        /// </summary>
        internal static readonly string BinaryEncodingName = "binary";

        /// <summary>
        /// The name used to refer to the default channel encoding.
        /// </summary>
        internal static readonly string ChannelEncodingName = "channelDefault";
        /// <summary>
        /// The name used to refer to the default encoding.
        /// </summary>
        internal static readonly string DefaultEncodingName = "default";
        /// <summary>
        /// The name used to refer to the system default encoding.
        /// </summary>
        internal static readonly string SystemEncodingName = "systemDefault";
        /// <summary>
        /// The name used to refer to the default Tcl encoding.
        /// </summary>
        internal static readonly string TclEncodingName = "tclDefault";
        /// <summary>
        /// The name used to refer to the default text encoding.
        /// </summary>
        internal static readonly string TextEncodingName = "textDefault";
        /// <summary>
        /// The name used to refer to the default script encoding.
        /// </summary>
        internal static readonly string ScriptEncodingName = "scriptDefault";
        /// <summary>
        /// The name used to refer to the default XML encoding.
        /// </summary>
        internal static readonly string XmlEncodingName = "xmlDefault";
        /// <summary>
        /// The name used to refer to the snippet encoding.
        /// </summary>
        internal static readonly string SnippetEncodingName = "snippetEncoding";

        /// <summary>
        /// The null encoding sentinel, used to represent the absence of an
        /// encoding.
        /// </summary>
        private static readonly Encoding NullEncoding = null;
        /// <summary>
        /// The system default encoding (little-endian Unicode without a
        /// byte-order mark).
        /// </summary>
        private static readonly Encoding SystemEncoding = new UnicodeEncoding(false, false);

        //
        // WARNING: For use by the [encoding system] sub-command only.
        //
        /// <summary>
        /// The IANA registered (web) name of the system default encoding, or null
        /// if it is not available.
        /// </summary>
        internal static readonly string SystemEncodingWebName = (SystemEncoding != null) ?
            SystemEncoding.WebName : null;

        /// <summary>
        /// The default encoding (the Eagle core UTF-8 encoding) used when no other
        /// encoding is specified.
        /// </summary>
        private static readonly Encoding DefaultEncoding = CoreUtf8Encoding.CoreUtf8;

        /// <summary>
        /// The encoding (the Eagle core UTF-8 encoding) used when reading or
        /// writing XML data.
        /// </summary>
        private static readonly Encoding XmlEncoding = CoreUtf8Encoding.CoreUtf8;

        /// <summary>
        /// The encoding (a one-byte encoding) used to treat string data as raw
        /// binary bytes.
        /// </summary>
        private static readonly Encoding BinaryEncoding = OneByteEncoding.OneByte;

        /// <summary>
        /// The encoding used when reading or writing text data.
        /// </summary>
        private static readonly Encoding TextEncoding = DefaultEncoding;

        //
        // NOTE: This encoding appears to be functionally identical to the Tcl
        //       encoding "cp1252", which is their default channel encoding on
        //       Windows.
        //
        /// <summary>
        /// The default encoding (<c>iso-8859-1</c>) used for I/O channels.
        /// </summary>
        private static readonly Encoding ChannelEncoding = GetEncoding(
            "iso-8859-1");

        /// <summary>
        /// The default encoding used for Tcl compatibility.
        /// </summary>
        private static readonly Encoding TclEncoding = ChannelEncoding;
        /// <summary>
        /// The default encoding used when reading or writing script files.
        /// </summary>
        private static readonly Encoding ScriptEncoding = TclEncoding;
        /// <summary>
        /// The default encoding used when reading or writing script snippets.
        /// </summary>
        private static readonly Encoding SnippetEncoding = ScriptEncoding;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The code page identifier for the UTF-8 encoding.
        /// </summary>
        private const int Utf8CodePage = 65001;              /* UTF-8 */
        /// <summary>
        /// The code page identifier for the little-endian UTF-16 encoding.
        /// </summary>
        private const int Utf16LittleEndianCodePage = 1200;  /* UTF-16LE */
        /// <summary>
        /// The code page identifier for the big-endian UTF-16 encoding.
        /// </summary>
        private const int Utf16BigEndianCodePage = 1201;     /* UTF-16BE */
        /// <summary>
        /// The code page identifier for the little-endian UTF-32 encoding.
        /// </summary>
        private const int Utf32LittleEndianCodePage = 12000; /* UTF-32LE */
        /// <summary>
        /// The code page identifier for the big-endian UTF-32 encoding.
        /// </summary>
        private const int Utf32BigEndianCodePage = 12001;    /* UTF-32BE */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Match Modes & Regular Expression Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The placeholder token (<c>%text%</c>) substituted with replacement text
        /// during string processing.
        /// </summary>
        private static string TextReplacementToken = "%text%";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default <see cref="MatchMode" /> used by the <c>[string map]</c>
        /// command.
        /// </summary>
        internal static readonly MatchMode DefaultMapMatchMode = MatchMode.Exact; // COMPAT: Tcl.

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default <see cref="MatchMode" /> used for general string matching.
        /// </summary>
        internal static readonly MatchMode DefaultMatchMode = MatchMode.Glob; // COMPAT: Tcl.
        /// <summary>
        /// The default <see cref="MatchMode" /> used by the <c>[switch]</c>
        /// command.
        /// </summary>
        internal static readonly MatchMode DefaultSwitchMatchMode = MatchMode.Exact; // COMPAT: Tcl.
        /// <summary>
        /// The default <see cref="MatchMode" /> used when matching result values.
        /// </summary>
        internal static readonly MatchMode DefaultResultMatchMode = MatchMode.Exact; // COMPAT: Tcl.

        /// <summary>
        /// The default <see cref="MatchMode" /> used when matching object names.
        /// </summary>
        internal static readonly MatchMode DefaultObjectMatchMode = DefaultMatchMode;
        /// <summary>
        /// The default <see cref="MatchMode" /> used when matching names during
        /// unload operations.
        /// </summary>
        internal static readonly MatchMode DefaultUnloadMatchMode = MatchMode.Exact;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS && XML
        /// <summary>
        /// The regular expression quantifier (<c>{2,}</c>) matching two or more
        /// occurrences of the preceding element.
        /// </summary>
        private static readonly string TwoOrMoreQuantifier = "{2,}";

        /// <summary>
        /// The regular expression used to match runs of two or more whitespace
        /// characters.
        /// </summary>
        private static readonly Regex TwoOrMoreWhiteSpaceRegEx = RegExOps.Create(
            String.Format("\\s{0}", TwoOrMoreQuantifier));

        /// <summary>
        /// The regular expression used to match runs of two or more space
        /// characters.
        /// </summary>
        private static readonly Regex TwoOrMoreSpaceRegEx = RegExOps.Create(
            String.Format("[ ]{0}", TwoOrMoreQuantifier));
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default <see cref="RegexOptions" /> used when creating regular
        /// expressions.
        /// </summary>
        internal static readonly RegexOptions DefaultRegExOptions = RegexOptions.None;
        /// <summary>
        /// The default <see cref="RegexOptions" /> used when testing strings
        /// against regular expressions.
        /// </summary>
        internal static readonly RegexOptions DefaultRegExTestOptions = RegexOptions.Singleline; /* COMPAT: Tcl. */
        /// <summary>
        /// The default <see cref="RegexOptions" /> used when parsing regular
        /// expression syntax.
        /// </summary>
        internal static readonly RegexOptions DefaultRegExSyntaxOptions = RegexOptions.Singleline; /* COMPAT: Tcl. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region [format] Command Constants
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default precision (number of digits) used when formatting
        /// floating-point values with the <c>E</c>, <c>F</c>, and <c>G</c>
        /// conversion specifiers.
        /// </summary>
        private static int DoubleDefaultPrecision = 6; /* For 'E' / 'e', 'F' / 'f', and 'G' / 'g'. */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The minimum number of digits used when formatting the exponent of a
        /// floating-point value.
        /// </summary>
        private static int MinimumExponentLength = 2;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The characters (<c>E</c> and <c>e</c>) that introduce the exponent
        /// portion of a formatted floating-point value.
        /// </summary>
        private static char[] ExponentPrefixChars = {
            Characters.E, Characters.e
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error message used when positional (<c>%n$</c>) and non-positional
        /// (<c>%</c>) conversion specifiers are mixed.
        /// </summary>
        private const string mixedXpgError = "cannot mix \"%\" and \"%n$\" conversion specifiers";
        /// <summary>
        /// The error message used when a formatted value exceeds the maximum
        /// allowed size.
        /// </summary>
        private const string OverflowError = "max size for a Tcl value exceeded";

        /// <summary>
        /// The prefix (<c>0b</c>) that denotes a binary (base-2) integer literal.
        /// </summary>
        private const string BinaryPrefix = "0b";
        /// <summary>
        /// The prefix (<c>0d</c>) that denotes a decimal (base-10) integer
        /// literal.
        /// </summary>
        private const string DecimalPrefix = "0d";
        /// <summary>
        /// The legacy prefix (<c>0</c>) that denotes an octal (base-8) integer
        /// literal.
        /// </summary>
        private const string LegacyOctalPrefix = "0";
        /// <summary>
        /// The prefix (<c>0o</c>) that denotes an octal (base-8) integer literal.
        /// </summary>
        private const string OctalPrefix = "0o";
        /// <summary>
        /// The prefix (<c>0x</c>) that denotes a hexadecimal (base-16) integer
        /// literal.
        /// </summary>
        private const string HexadecimalPrefix = "0x";

        /// <summary>
        /// The error messages used when a <c>[format]</c> argument index is
        /// missing or out of range.
        /// </summary>
        private static readonly string[] BadIndexError = {
            "not enough arguments for all format specifiers",
            "\"%n$\" argument index out of range"
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Character List Constants
        /// <summary>
        /// The characters (the minus sign and the slash) recognized as
        /// command-line switch prefixes.
        /// </summary>
        private static readonly char[] switchChars = {
            Characters.MinusSign, Characters.Slash
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The characters (the open and close braces) that delimit a sub-pattern.
        /// </summary>
        private static readonly char[] SubPatternChars = {
            Characters.OpenBrace, Characters.CloseBrace
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Excludes characters covered by PathOps.HasPathWildcard().
        //
        /// <summary>
        /// The characters treated as wildcards by the <c>[string match]</c>
        /// command, excluding those covered by
        /// <c>PathOps.HasPathWildcard</c>.
        /// </summary>
        private static readonly char[] StringMatchWildcardChars = {
            Characters.OpenBracket,
            Characters.Backslash,
            Characters.CloseBracket
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// The list of characters that are recognized as command-line switch
        /// prefixes.
        /// </summary>
        private static readonly CharList switchCharList = new CharList(switchChars);
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Radix Regular Expressions
        /// <summary>
        /// The regular expression used to match a hexadecimal SHA-1 hash value
        /// with a leading <c>0x</c> prefix.
        /// </summary>
        internal static readonly Regex sha1HashValueRegEx = RegExOps.Create(
            "^0x[0-9a-f]{40}$");

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to match a hexadecimal SHA-512 hash value
        /// with a leading <c>0x</c> prefix.
        /// </summary>
        internal static readonly Regex sha512HashValueRegEx = RegExOps.Create(
            "^0x[0-9a-f]{128}$");

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to validate a base16 (hexadecimal) encoded
        /// string.
        /// </summary>
        private static readonly Regex base16RegEx = RegExOps.Create(
            "^(?:0x)?(?:[0-9A-F][0-9A-F])*$", RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of base26 character groups emitted per line when formatting
        /// base26 output.
        /// </summary>
        private const int Base26GroupsPerLine = 25;

        /// <summary>
        /// The regular expression used to validate a base26 encoded string.
        /// </summary>
        private static readonly Regex base26RegEx = RegExOps.Create(
            "^(?:[A-Z\\s][A-Z\\s])*$", RegexOptions.IgnoreCase |
            RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to validate a base64 encoded string.
        /// </summary>
        private static readonly Regex base64RegEx = RegExOps.Create(
            "^(?:[0-9A-Z+/]{4})*(?:[0-9A-Z+/]{3}=|[0-9A-Z+/]{2}==)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The regular expression used to validate a whitespace-separated list of
        /// hexadecimal byte values.
        /// </summary>
        private static readonly Regex hexadecimalBytesRegEx = RegExOps.Create(
            "^(?:0x[0-9A-F]{2}(?:\\s+0x[0-9A-F]{2})*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the static data of this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This list maps the byte-order-mark sequences to their corresponding
        //       encodings.  Linear searching is used for detection; therefore, this
        //       list should be kept very small.
        //
        /// <summary>
        /// The list that maps byte-order-mark byte sequences to their
        /// corresponding encodings, used for encoding detection.
        /// </summary>
        private static IList<IAnyPair<byte[], Encoding>> preambleEncodings = null;

        //
        // NOTE: This is the minimum number of bytes needed for the byte-order-mark
        //       sequences handled by this class.
        //
        /// <summary>
        /// The minimum number of bytes required to detect any of the supported
        /// byte-order-mark sequences.
        /// </summary>
        private static int minimumPreambleSize = 0;

        //
        // NOTE: This is the maximum number of bytes needed for the byte-order-mark
        //       sequences handled by this class.
        //
        /// <summary>
        /// The maximum number of bytes required to detect any of the supported
        /// byte-order-mark sequences.
        /// </summary>
        private static int maximumPreambleSize = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value is null or
        /// empty and reports its length to the caller.
        /// </summary>
        /// <param name="value">
        /// The string value to check.  This value may be null.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the length of the specified string value, or
        /// an invalid length when the string value is null.
        /// </param>
        /// <returns>
        /// True if the specified string value is null or empty; otherwise,
        /// false.
        /// </returns>
        public static bool IsNullOrEmpty(
            string value,
            out int length
            )
        {
            if (value == null)
            {
                length = Length.Invalid;
                return true;
            }

            int localLength = value.Length;

            length = localLength;
            return (localLength == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value is
        /// logically empty, i.e. null, empty, or consisting only of whitespace
        /// characters.
        /// </summary>
        /// <param name="value">
        /// The string value to check.  This value may be null.
        /// </param>
        /// <returns>
        /// True if the specified string value is logically empty; otherwise,
        /// false.
        /// </returns>
        public static bool IsLogicallyEmpty(
            string value
            )
        {
            int length;

            return IsLogicallyEmpty(value, out length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value is
        /// logically empty, i.e. null, empty, or consisting only of whitespace
        /// characters, and reports its length to the caller.
        /// </summary>
        /// <param name="value">
        /// The string value to check.  This value may be null.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the length of the specified string value, or
        /// an invalid length when the string value is null.
        /// </param>
        /// <returns>
        /// True if the specified string value is logically empty; otherwise,
        /// false.
        /// </returns>
        private static bool IsLogicallyEmpty(
            string value,
            out int length
            )
        {
            string trimValue; /* NOT USED */

            return IsLogicallyEmpty(value, out trimValue, out length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value is
        /// logically empty, i.e. null, empty, or consisting only of whitespace
        /// characters, and reports its trimmed value to the caller.
        /// </summary>
        /// <param name="value">
        /// The string value to check.  This value may be null.
        /// </param>
        /// <param name="trimValue">
        /// Upon success, receives the specified string value with leading and
        /// trailing whitespace removed, or null when the string value is null.
        /// </param>
        /// <returns>
        /// True if the specified string value is logically empty; otherwise,
        /// false.
        /// </returns>
        public static bool IsLogicallyEmpty(
            string value,
            out string trimValue
            )
        {
            int length; /* NOT USED */

            return IsLogicallyEmpty(value, out trimValue, out length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value is
        /// logically empty, i.e. null, empty, or consisting only of whitespace
        /// characters, and reports both its trimmed value and length to the
        /// caller.
        /// </summary>
        /// <param name="value">
        /// The string value to check.  This value may be null.
        /// </param>
        /// <param name="trimValue">
        /// Upon success, receives the specified string value with leading and
        /// trailing whitespace removed, or null when the string value is null.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the length of the specified string value, or
        /// an invalid length when the string value is null.
        /// </param>
        /// <returns>
        /// True if the specified string value is logically empty; otherwise,
        /// false.
        /// </returns>
        private static bool IsLogicallyEmpty(
            string value,
            out string trimValue,
            out int length
            )
        {
            int localLength;

            if (IsNullOrEmpty(value, out localLength))
            {
                trimValue = null;
                length = localLength;

                return true;
            }

            string localTrimValue = value.Trim();

            if (IsNullOrEmpty(localTrimValue, out localLength))
            {
                trimValue = localTrimValue;
                length = localLength;

                return true;
            }

            trimValue = localTrimValue;
            length = localLength;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain a string representation from the
        /// specified object value, handling the various string-like wrapper
        /// types supported by the engine.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a string representation from.  Supported
        /// values include a string, a string builder, a string builder wrapper,
        /// an argument, a result, or an interpreter.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the string representation of the specified
        /// object value.  Upon failure, this value is set to null.
        /// </param>
        /// <returns>
        /// True if a string representation was obtained from the specified
        /// object value; otherwise, false.
        /// </returns>
        public static bool TryGetStringFromObject(
            object @object,
            out string result
            )
        {
            if (@object is string)
            {
                result = (string)@object;
                return true;
            }

            if (@object is StringBuilder)
            {
                result = @object.ToString();
                return true;
            }

            if (@object is IHaveStringBuilder)
            {
                result = @object.ToString();
                return true;
            }

            if (@object is Argument)
            {
                result = ((Argument)@object).String;
                return true;
            }

            if (@object is Result)
            {
                result = ((Result)@object).String;
                return true;
            }

            if (@object is Interpreter)
            {
                result = ((Interpreter)@object).InternalToString();
                return true;
            }

            result = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a string representation from the specified object
        /// value, falling back to its string representation when necessary.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a string representation from.  This value
        /// may be null.
        /// </param>
        /// <returns>
        /// The string representation of the specified object value, or null if
        /// one cannot be obtained.
        /// </returns>
        public static string GetStringFromObject(
            object @object
            )
        {
            return GetStringFromObject(@object, null, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a string representation from the specified object
        /// value, optionally falling back to its string representation, and
        /// returning a default value when no representation can be obtained.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a string representation from.  This value
        /// may be null.
        /// </param>
        /// <param name="default">
        /// The default string value to return when a string representation
        /// cannot be obtained.  This value may be null.
        /// </param>
        /// <param name="toStringOk">
        /// Non-zero to fall back to the string representation of the specified
        /// object value when it is not one of the natively supported types.
        /// </param>
        /// <returns>
        /// The string representation of the specified object value, or the
        /// default value if one cannot be obtained.
        /// </returns>
        public static string GetStringFromObject(
            object @object,
            string @default,
            bool toStringOk
            )
        {
            string result;

            if (TryGetStringFromObject(@object, out result))
                return result;

            if (toStringOk && (@object != null))
                return @object.ToString();

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a string representation from the specified object
        /// value, treating it as a collection of strings when it is enumerable
        /// and not one of the natively supported string-like types.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a string representation from.  This value
        /// may be null.
        /// </param>
        /// <returns>
        /// The string representation of the specified object value, or the
        /// list representation of its elements when it is an enumerable
        /// collection.
        /// </returns>
        public static string GetStringsFromObject(
            object @object
            )
        {
            string result;

            if (TryGetStringFromObject(@object, out result))
                return result;

            IEnumerable collection = @object as IEnumerable;

            if (collection == null)
                return GetStringFromObject(@object);

            StringList list = new StringList();

            foreach (object item in collection)
                list.Add(GetStringFromObject(item));

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains an argument from the specified object value,
        /// converting the various string-like and enumerable types supported by
        /// the engine as necessary.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain an argument from.  This value may be null.
        /// </param>
        /// <returns>
        /// The argument for the specified object value, or null if one cannot
        /// be obtained.
        /// </returns>
        public static Argument GetArgumentFromObject(
            object @object
            )
        {
            return GetArgumentFromObject(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains an argument from the specified object value,
        /// converting the various string-like and enumerable types supported by
        /// the engine as necessary, with control over how a disposed object is
        /// handled.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain an argument from.  This value may be null.
        /// </param>
        /// <param name="throwOnDisposed">
        /// Non-zero to re-throw any object disposed exception encountered while
        /// converting the specified object value; otherwise, null is returned
        /// in that case.
        /// </param>
        /// <returns>
        /// The argument for the specified object value, or null if one cannot
        /// be obtained.
        /// </returns>
        private static Argument GetArgumentFromObject(
            object @object,
            bool throwOnDisposed
            )
        {
            try
            {
                if (@object is Argument)
                    return (Argument)@object;

                if (Object.ReferenceEquals(
                        @object, null)) /* (@object == null) */
                {
                    Argument argument = Argument.Null;

                    if ((argument != null) &&
                        Object.ReferenceEquals(argument.Value, null))
                    {
                        return argument;
                    }

                    return Argument.InternalCreate();
                }

                if (Object.ReferenceEquals(@object, String.Empty))
                {
                    Argument argument = Argument.Empty;

                    if ((argument != null) &&
                        Object.ReferenceEquals(argument.Value, String.Empty))
                    {
                        return argument;
                    }

                    return Argument.InternalCreate(String.Empty);
                }

                ///////////////////////////////////////////////////////////////

                if (@object is string)
                {
                    return Argument.FromString((string)@object);
                }
                else if (@object is StringBuilder)
                {
                    return Argument.FromStringBuilder((StringBuilder)@object);
                }
                else if (@object is IHaveStringBuilder)
                {
                    return Argument.FromStringBuilder(
                        GetStringBuilder((IHaveStringBuilder)@object));
                }
                else if (@object is IEnumerable<string>)
                {
                    if (@object is IStringList)
                        return Argument.FromList((IStringList)@object);
                    else
                        return Argument.FromList(new StringList(@object));
                }
                else
                {
                    return Argument.FromString(@object.ToString());
                }
            }
            catch (ObjectDisposedException)
            {
                if (throwOnDisposed)
                    throw;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a result from the specified object value,
        /// converting the various string-like and enumerable types supported by
        /// the engine as necessary.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a result from.  This value may be null.
        /// </param>
        /// <returns>
        /// The result for the specified object value, or null if one cannot be
        /// obtained.
        /// </returns>
        public static Result GetResultFromObject(
            object @object
            )
        {
            return GetResultFromObject(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a result from the specified object value,
        /// converting the various string-like and enumerable types supported by
        /// the engine as necessary, with control over how a disposed object is
        /// handled.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a result from.  This value may be null.
        /// </param>
        /// <param name="throwOnDisposed">
        /// Non-zero to re-throw any object disposed exception encountered while
        /// converting the specified object value; otherwise, null is returned
        /// in that case.
        /// </param>
        /// <returns>
        /// The result for the specified object value, or null if one cannot be
        /// obtained.
        /// </returns>
        public static Result GetResultFromObject(
            object @object,
            bool throwOnDisposed
            )
        {
            try
            {
                if (@object is Result)
                    return (Result)@object;

                if (Object.ReferenceEquals(
                        @object, null)) /* (@object == null) */
                {
                    Result result = Result.Null;

                    if ((result != null) &&
                        Object.ReferenceEquals(result.Value, null))
                    {
                        return result;
                    }

                    return Result.FromString(null);
                }

                if (Object.ReferenceEquals(@object, String.Empty))
                {
                    Result result = Result.Empty;

                    if ((result != null) &&
                        Object.ReferenceEquals(result.Value, String.Empty))
                    {
                        return result;
                    }

                    return Result.FromString(String.Empty);
                }

                ///////////////////////////////////////////////////////////////

                if (@object is string)
                {
                    return Result.FromString((string)@object);
                }
                else if (@object is StringBuilder)
                {
                    return Result.FromStringBuilder((StringBuilder)@object);
                }
                else if (@object is IHaveStringBuilder)
                {
                    return Result.FromStringBuilder(
                        GetStringBuilder((IHaveStringBuilder)@object));
                }
                else if (@object is IEnumerable<string>)
                {
                    if (@object is IStringList)
                        return Result.FromList((IStringList)@object);
                    else
                        return Result.FromList(new StringList(@object));
                }
                else
                {
                    return Result.FromString(@object.ToString()); /* throw */
                }
            }
            catch (ObjectDisposedException)
            {
                if (throwOnDisposed)
                    throw;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the underlying string builder from the specified
        /// string builder wrapper.
        /// </summary>
        /// <param name="haveStringBuilder">
        /// The string builder wrapper to obtain the underlying string builder
        /// from.  This value may be null.
        /// </param>
        /// <returns>
        /// The underlying string builder, or null when the specified string
        /// builder wrapper is null.
        /// </returns>
        private static StringBuilder GetStringBuilder(
            IHaveStringBuilder haveStringBuilder
            )
        {
            if (haveStringBuilder == null)
                return null;

            return haveStringBuilder.Builder;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new string builder wrapper backed by a new,
        /// empty string builder.
        /// </summary>
        /// <returns>
        /// The newly created string builder wrapper.
        /// </returns>
        public static IHaveStringBuilder NewIHaveStringBuilder()
        {
            return NewIHaveStringBuilder(SBF.CreateNoCache()); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new string builder wrapper backed by the
        /// specified string builder.
        /// </summary>
        /// <param name="builder">
        /// The string builder to wrap.  This value may be null.
        /// </param>
        /// <returns>
        /// The newly created string builder wrapper.
        /// </returns>
        private static IHaveStringBuilder NewIHaveStringBuilder(
            StringBuilder builder
            )
        {
            return new StringBuilderWrapper(builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains an object that exposes a string builder for the
        /// specified object value, unwrapping any supported wrapper types as
        /// necessary.
        /// </summary>
        /// <param name="object">
        /// The object value to obtain a string builder wrapper for.  Supported
        /// values include an existing string builder wrapper, a string builder,
        /// a string, an argument, a result, or any other object (in which case
        /// its string representation is used).
        /// </param>
        /// <param name="create">
        /// Non-zero to create and return a new, empty string builder wrapper
        /// when the specified object value is null.
        /// </param>
        /// <returns>
        /// The string builder wrapper for the specified object value, or null
        /// if one cannot be obtained and creation was not requested.
        /// </returns>
        public static IHaveStringBuilder GetIHaveStringBuilderFromObject(
            object @object,
            bool create
            )
        {
        retry:

            if (@object is IHaveStringBuilder)
                return (IHaveStringBuilder)@object;

            if (@object is StringBuilder)
                return NewIHaveStringBuilder((StringBuilder)@object);

            if (@object is string)
            {
                return NewIHaveStringBuilder(
                    SBF.CreateNoCache((string)@object)); /* EXEMPT */
            }

            if (@object is Argument)
            {
                @object = ((Argument)@object).Value;
                goto retry;
            }

            if (@object is Result)
            {
                @object = ((Result)@object).Value;
                goto retry;
            }

            if (@object != null)
            {
                @object = GetStringFromObject(@object);
                goto retry;
            }

            return create ? NewIHaveStringBuilder() : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method obtains a string builder representing the specified
        /// object, unwrapping known wrapper types and converting other values to
        /// their string forms as necessary.
        /// </summary>
        /// <param name="object">
        /// The object to obtain a string builder for.  This parameter may be
        /// null.
        /// </param>
        /// <param name="create">
        /// Non-zero to create and return an empty string builder when the
        /// object is null; otherwise, null is returned in that case.
        /// </param>
        /// <returns>
        /// A string builder representing the object, or null if one could not
        /// be obtained and creation was not requested.
        /// </returns>
        public static StringBuilder GetStringBuilderFromObject(
            object @object,
            bool create
            )
        {
        retry:

            if (@object is StringBuilder)
                return (StringBuilder)@object;

            if (@object is IHaveStringBuilder)
                return GetStringBuilder((IHaveStringBuilder)@object);

            if (@object is string)
                return SBF.CreateNoCache((string)@object); /* EXEMPT */

            if (@object is Argument)
            {
                @object = ((Argument)@object).Value;
                goto retry;
            }

            if (@object is Result)
            {
                @object = ((Result)@object).Value;
                goto retry;
            }

            if (@object != null)
            {
                @object = GetStringFromObject(@object);
                goto retry;
            }

            return create ? SBF.CreateNoCache() : null; /* EXEMPT */
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new string builder that contains a copy of the
        /// contents of the specified string builder.
        /// </summary>
        /// <param name="value">
        /// The string builder whose contents are to be copied.
        /// </param>
        /// <returns>
        /// A new string builder containing a copy of the specified contents, or
        /// null if the specified string builder is null.
        /// </returns>
        public static StringBuilder CopyStringBuilder(
            StringBuilder value
            )
        {
            if (value == null)
                return null;

            StringBuilder result = SBF.Create(value.Length);

            result.Append(value);

            return result;

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified string value into an array of lines,
        /// recognizing carriage-return, line-feed, and carriage-return/line-feed
        /// line endings.
        /// </summary>
        /// <param name="value">
        /// The string value to split into lines.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include empty lines in the resulting array.
        /// </param>
        /// <returns>
        /// The array of lines, or null if the specified string value is null.
        /// </returns>
        private static string[] SplitLines(
            string value,
            bool empty
            )
        {
            if (value == null)
                return null;

            StringList lines = new StringList();
            int length = value.Length;
            StringBuilder line = SBF.Create(length);

            for (int index = 0; index < length; index++)
            {
                char character = value[index];

                int nextIndex = index + 1;
                char? nextCharacter = null;

                if (nextIndex < length)
                {
                    nextCharacter = value[nextIndex];

                    if ((nextCharacter != Characters.CarriageReturn) &&
                        (nextCharacter != Characters.LineFeed))
                    {
                        nextCharacter = null;
                    }
                }

                switch (character)
                {
                    case Characters.CarriageReturn:
                    case Characters.LineFeed:
                        {
                            if (empty || (line.Length > 0))
                            {
                                lines.Add(line);
                                line.Length = 0;
                            }

                            if ((nextCharacter != null) &&
                                (nextCharacter != character))
                            {
                                index++;
                            }
                            break;
                        }
                    default:
                        {
                            line.Append(character);
                            break;
                        }
                }
            }

            if (line.Length > 0)
                lines.Add(StringBuilderCache.GetStringAndRelease(ref line));

            return lines.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes blank lines and comment lines from the specified
        /// string value, normalizing the remaining lines to use carriage-return/
        /// line-feed line endings.
        /// </summary>
        /// <param name="trimAll">
        /// Non-zero to trim leading and trailing whitespace from every retained
        /// line; otherwise, the original (untrimmed) lines are retained verbatim.
        /// </param>
        /// <param name="value">
        /// Upon input, the string value to process.  Upon success, this parameter
        /// will be modified to contain the resulting string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode RemoveBlanksAndComments(
            bool trimAll,     /* in */
            ref string value, /* in, out */
            ref Result error  /* out */
            )
        {
            if (value == null)
            {
                error = "invalid value";
                return ReturnCode.Error;
            }

            int length = value.Length;

            if (length == 0)
            {
                error = "empty value";
                return ReturnCode.Error;
            }

            StringBuilder builder = SBF.Create(length);
            string[] lines = SplitLines(value, false);

            if (lines == null)
            {
                error = "could not split string";
                return ReturnCode.Error;
            }

            foreach (string line in lines)
            {
                if (line == null) /* cannot use null lines */
                    continue;

                string trimLine = line.Trim(); /* remove spaces */

                if (String.IsNullOrEmpty(trimLine)) /* no blank lines */
                    continue;

                if (trimLine[0] == Characters.Comment) /* no comment lines */
                    continue;

                //
                // NOTE: Use the original line verbatim as this method is
                //       exclusively concerned with removing blank lines
                //       and comments (i.e. it should not mutate any other
                //       content).  This does not apply if the trim-all
                //       flag is specified by the caller.  In that case,
                //       all lines are trimmed of whitespace.
                //
                builder.Append(trimAll ? trimLine : line);

                //
                // NOTE: Normalize to "DOS" line-endings.
                //
                builder.Append(Characters.CarriageReturn);
                builder.Append(Characters.LineFeed);
            }

            value = StringBuilderCache.GetStringAndRelease(ref builder);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the data contained within the comment lines of
        /// the specified string value, discarding all non-comment lines and the
        /// leading comment character of each comment line, normalizing the
        /// result to use carriage-return/line-feed line endings.
        /// </summary>
        /// <param name="value">
        /// Upon input, the string value to process.  Upon success, this parameter
        /// will be modified to contain the resulting string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ExtractDataFromComments(
            ref string value, /* in, out */
            ref Result error  /* out */
            )
        {
            if (value == null)
            {
                error = "invalid value";
                return ReturnCode.Error;
            }

            int length = value.Length;

            if (length == 0)
            {
                error = "empty value";
                return ReturnCode.Error;
            }

            StringBuilder builder = SBF.Create(length);
            string[] lines = SplitLines(value, false);

            if (lines == null)
            {
                error = "could not split string";
                return ReturnCode.Error;
            }

            foreach (string line in lines)
            {
                if (line == null) /* cannot use null lines */
                    continue;

                string trimLine = line.Trim(); /* remove spaces */
                int trimLength;

                if (IsNullOrEmpty(trimLine, out trimLength)) /* no blank lines */
                    continue;

                if (trimLine[0] != Characters.Comment) /* comment lines only */
                    continue;

                //
                // NOTE: Capture the entire remaining portion of the line,
                //       including spacing.  Basically, this just strips
                //       the leading comment character.
                //
                builder.Append(trimLine, 1, trimLength - 1);

                //
                // NOTE: Normalize to "DOS" line-endings.
                //
                builder.Append(Characters.CarriageReturn);
                builder.Append(Characters.LineFeed);
            }

            value = StringBuilderCache.GetStringAndRelease(ref builder);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the cached collection of preamble encodings and
        /// resets the associated minimum and maximum preamble sizes.
        /// </summary>
        /// <returns>
        /// The number of cached items that were cleared.
        /// </returns>
        public static int ClearPreambleEncodings()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (preambleEncodings != null)
                {
                    result += preambleEncodings.Count;

                    preambleEncodings.Clear();
                    preambleEncodings = null;
                }

                if (minimumPreambleSize != 0)
                {
                    result++;

                    minimumPreambleSize = 0;
                }

                if (maximumPreambleSize != 0)
                {
                    result++;

                    maximumPreambleSize = 0;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the preamble (byte-order mark) associated with
        /// the specified encoding.
        /// </summary>
        /// <param name="encoding">
        /// The encoding whose preamble is to be obtained.
        /// </param>
        /// <returns>
        /// The preamble bytes for the specified encoding, or null if the
        /// encoding is null or its preamble cannot be obtained.
        /// </returns>
        public static byte[] GetPreamble(
            Encoding encoding
            )
        {
            if (encoding != null)
            {
                try
                {
                    return encoding.GetPreamble();
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes, if necessary, the cached collection of
        /// preamble encodings along with the associated minimum and maximum
        /// preamble sizes.
        /// </summary>
        private static void InitializePreambleEncodings()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (preambleEncodings == null)
                {
                    preambleEncodings = new List<IAnyPair<byte[], Encoding>>();

                    foreach (Encoding encoding in new Encoding[] {
                            new UTF8Encoding(true, false),    /* UTF-8 with BOM */
                            new UnicodeEncoding(false, true), /* UTF-16-LE with BOM */
                            new UnicodeEncoding(true, true),  /* UTF-16-BE with BOM */
                            new UTF32Encoding(false, true),   /* UTF-32-LE with BOM */
                            new UTF32Encoding(true, true)     /* UTF-32-BE with BOM */
                        })
                    {
                        byte[] preamble = GetPreamble(encoding);

                        if (preamble == null)
                            continue;

                        int preambleSize = preamble.Length;

                        if (preambleSize == 0)
                            continue;

                        if ((minimumPreambleSize == 0) ||
                            (preambleSize < minimumPreambleSize))
                        {
                            minimumPreambleSize = preambleSize;
                        }

                        if ((maximumPreambleSize == 0) ||
                            (preambleSize > maximumPreambleSize))
                        {
                            maximumPreambleSize = preambleSize;
                        }

                        preambleEncodings.Add(
                            new AnyPair<byte[], Encoding>(preamble, encoding));
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the minimum and maximum sizes, in bytes, of the
        /// known encoding preambles.
        /// </summary>
        /// <param name="minimumSize">
        /// Upon success, this parameter will be modified to contain the minimum
        /// preamble size, in bytes.
        /// </param>
        /// <param name="maximumSize">
        /// Upon success, this parameter will be modified to contain the maximum
        /// preamble size, in bytes.
        /// </param>
        public static void GetPreambleSizes(
            ref int minimumSize,
            ref int maximumSize
            )
        {
            InitializePreambleEncodings();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                minimumSize = minimumPreambleSize;
                maximumSize = maximumPreambleSize;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to guess the encoding of the specified array of
        /// bytes by matching its leading bytes against the known encoding
        /// preambles (byte-order marks).
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes whose encoding is to be guessed.
        /// </param>
        /// <param name="preambleSize">
        /// Upon success, this parameter will be modified to contain the size, in
        /// bytes, of the matched preamble.
        /// </param>
        /// <returns>
        /// The guessed encoding, or null if no matching preamble is found.
        /// </returns>
        private static Encoding GuessEncoding(
            byte[] bytes,
            ref int preambleSize
            )
        {
            InitializePreambleEncodings();

            if (bytes == null)
                return null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (preambleEncodings == null)
                    return null;

                foreach (IAnyPair<byte[], Encoding> anyPair in preambleEncodings)
                {
                    byte[] preamble = anyPair.X;

                    if (preamble == null)
                        continue;

                    int localPreambleSize = preamble.Length;

                    if (ArrayOps.Equals(bytes, preamble, localPreambleSize))
                    {
                        preambleSize = localPreambleSize;
                        return anyPair.Y;
                    }
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to guess the encoding of the specified array of
        /// bytes from its preamble (byte-order mark), falling back to the
        /// encoding associated with the specified encoding type when no preamble
        /// is recognized.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes whose encoding is to be guessed.
        /// </param>
        /// <param name="type">
        /// The encoding type whose associated encoding is to be used when no
        /// preamble is recognized.
        /// </param>
        /// <returns>
        /// The guessed or fallback encoding, or null if none can be determined.
        /// </returns>
        public static Encoding GuessOrGetEncoding(
            byte[] bytes,
            EncodingType type
            )
        {
            int preambleSize = 0;

            return GuessOrGetEncoding(bytes, type, ref preambleSize);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to guess the encoding of the specified array of
        /// bytes from its preamble (byte-order mark), falling back to the
        /// encoding associated with the specified encoding type when no preamble
        /// is recognized.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes whose encoding is to be guessed.
        /// </param>
        /// <param name="type">
        /// The encoding type whose associated encoding is to be used when no
        /// preamble is recognized.
        /// </param>
        /// <param name="preambleSize">
        /// Upon success, this parameter will be modified to contain the size, in
        /// bytes, of the matched preamble, when applicable.
        /// </param>
        /// <returns>
        /// The guessed or fallback encoding, or null if none can be determined.
        /// </returns>
        public static Encoding GuessOrGetEncoding(
            byte[] bytes,
            EncodingType type,
            ref int preambleSize
            )
        {
            Encoding encoding = GuessEncoding(
                bytes, ref preambleSize);

            if (encoding != null)
                return encoding;

            return GetEncoding(type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the encoding associated with the specified
        /// encoding name.
        /// </summary>
        /// <param name="name">
        /// The name of the encoding to obtain.
        /// </param>
        /// <returns>
        /// The encoding associated with the specified name, or null if it cannot
        /// be obtained.
        /// </returns>
        public static Encoding GetEncoding(
            string name
            )
        {
            Result error = null;

            return GetEncoding(name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the encoding associated with the specified
        /// encoding name.
        /// </summary>
        /// <param name="name">
        /// The name of the encoding to obtain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// The encoding associated with the specified name, or null if it cannot
        /// be obtained.
        /// </returns>
        public static Encoding GetEncoding(
            string name,
            ref Result error
            )
        {
            if (name != null)
            {
                try
                {
                    return Encoding.GetEncoding(name); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid encoding name";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the encoding associated with the specified
        /// encoding type.
        /// </summary>
        /// <param name="type">
        /// The encoding type whose associated encoding is to be obtained.
        /// </param>
        /// <returns>
        /// The encoding associated with the specified encoding type, or null if
        /// the encoding type is not recognized.
        /// </returns>
        public static Encoding GetEncoding(
            EncodingType type
            )
        {
            switch (type)
            {
                case EncodingType.Null:
                    return NullEncoding;
                case EncodingType.System:
                    return SystemEncoding;
                case EncodingType.Default:
                    return DefaultEncoding;
                case EncodingType.Binary:
                    return BinaryEncoding;
                case EncodingType.Tcl:
                    return TclEncoding;
                case EncodingType.Channel:
                    return ChannelEncoding;
                case EncodingType.Text:
                    return TextEncoding;
                case EncodingType.Script:
                    return ScriptEncoding;
                case EncodingType.Xml:
                    return XmlEncoding;
                case EncodingType.Policy:
                    return TextEncoding;
                case EncodingType.Profile:
                    return TextEncoding;
                case EncodingType.Syntax:
                    return TextEncoding;
#if HISTORY
                case EncodingType.History:
                    return TextEncoding;
#endif
                case EncodingType.Base64:
                    return TextEncoding;
                case EncodingType.RemoteUri:
                    return TextEncoding;
                case EncodingType.UnknownUri:
                    return TextEncoding;
                case EncodingType.FileSystem:
                    return TextEncoding;
                case EncodingType.Snippet:
                    return SnippetEncoding;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified encoding name refers to
        /// the default encoding.
        /// </summary>
        /// <param name="name">
        /// The encoding name to check.
        /// </param>
        /// <returns>
        /// True if the specified encoding name refers to the default encoding;
        /// otherwise, false.
        /// </returns>
        public static bool IsDefaultEncodingName(
            string name /* in */
            )
        {
            return String.IsNullOrEmpty(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified dictionary with all of the
        /// encodings known to the system, keyed by their names.
        /// </summary>
        /// <param name="encodings">
        /// Upon input, the dictionary to populate; if it is null, a new
        /// dictionary is created.  Upon return, this parameter contains the
        /// system encodings keyed by their names.
        /// </param>
        public static void GetSystemEncodings(
            ref EncodingDictionary encodings /* in, out */
            )
        {
            if (encodings == null)
                encodings = new EncodingDictionary();

            EncodingInfo[] systemEncodings = Encoding.GetEncodings();

            if (systemEncodings != null)
            {
                foreach (EncodingInfo encodingInfo in systemEncodings)
                {
                    if (encodingInfo == null)
                        continue;

                    encodings[encodingInfo.Name] = encodingInfo.GetEncoding();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the number of bytes required to encode the
        /// specified string value using the specified encoding (or the encoding
        /// associated with the specified encoding type) and adds that number to
        /// the running byte count.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use.  If this value is null, the encoding associated
        /// with the specified encoding type is used instead.
        /// </param>
        /// <param name="value">
        /// The string value whose encoded byte count is to be computed.
        /// </param>
        /// <param name="type">
        /// The encoding type to use when no explicit encoding is specified.
        /// </param>
        /// <param name="byteCount">
        /// Upon success, this value is increased by the number of bytes required
        /// to encode the specified string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode AddByteCount(
            Encoding encoding, /* in */
            string value,      /* in */
            EncodingType type, /* in */
            ref int byteCount, /* in, out */
            ref Result error   /* out */
            )
        {
            if (String.IsNullOrEmpty(value))
                return ReturnCode.Ok;

            if (encoding == null)
                encoding = GetEncoding(type);

            if (encoding == null)
            {
                error = "invalid encoding";
                return ReturnCode.Error;
            }

            try
            {
                byteCount += encoding.GetByteCount(value);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method encodes the specified string value into an array of bytes
        /// using the specified encoding (or the encoding associated with the
        /// specified encoding type).
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use.  If this value is null, the encoding associated
        /// with the specified encoding type is used instead.  If no encoding can
        /// be determined, the value is treated as Base64.
        /// </param>
        /// <param name="value">
        /// The string value to encode into bytes.
        /// </param>
        /// <param name="type">
        /// The encoding type to use when no explicit encoding is specified.
        /// </param>
        /// <param name="errorOnNull">
        /// Non-zero if a null string value should be treated as an error.
        /// </param>
        /// <param name="bytes">
        /// Upon success, this parameter will be modified to contain the resulting
        /// array of bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetBytes(
            Encoding encoding, /* in */
            string value,      /* in */
            EncodingType type, /* in */
            bool errorOnNull,  /* in */
            ref byte[] bytes,  /* out */
            ref Result error   /* out */
            )
        {
            if (value == null)
            {
                bytes = null;

                if (errorOnNull)
                {
                    error = "invalid value";
                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            if (value.Length == 0)
            {
                bytes = new byte[0];
                return ReturnCode.Ok;
            }

            if (encoding == null)
                encoding = GetEncoding(type);

            try
            {
                if (encoding != null)
                    bytes = encoding.GetBytes(value);
                else
                    bytes = Convert.FromBase64String(value);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes a count prefix from the specified array of bytes
        /// using the specified encoding (or the encoding associated with the
        /// specified encoding type) and parses it as a hexadecimal integer.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use.  If this value is null, the encoding associated
        /// with the specified encoding type is used instead.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information.  This parameter is not
        /// used.
        /// </param>
        /// <param name="bytes">
        /// The array of bytes containing the encoded count string.
        /// </param>
        /// <param name="type">
        /// The encoding type to use when no explicit encoding is specified.
        /// </param>
        /// <param name="count">
        /// Upon success, this parameter will be modified to contain the parsed
        /// count value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetCount(
            Encoding encoding,       /* in */
            CultureInfo cultureInfo, /* in: NOT USED */
            byte[] bytes,            /* in */
            EncodingType type,       /* in */
            ref int? count,          /* out */
            ref Result error         /* out */
            )
        {
            if (bytes == null)
            {
                error = "expected count string, got null";
                return ReturnCode.Error;
            }

            if (bytes.Length == 0)
            {
                error = "expected count string, got empty";
                return ReturnCode.Error;
            }

            if (encoding == null)
                encoding = GetEncoding(type);

            if (encoding == null)
            {
                error = "missing encoding for count string";
                return ReturnCode.Error;
            }

            try
            {
                string value = encoding.GetString(bytes);

                if (value == null)
                {
                    error = "got null count string";
                    return ReturnCode.Error;
                }

                int length = value.Length;

                if (length != Count.PrefixSize)
                {
                    error = String.Format(
                        "count string characters: have {0}, " +
                        "want {1}", length, Count.PrefixSize);

                    return ReturnCode.Error;
                }

                value = value.Substring(0, length - 1);

                int localCount;

                if (int.TryParse(
                        value, NumberStyles.AllowHexSpecifier,
                        DefaultCultureInfo, out localCount))
                {
                    count = localCount;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "count string must be hexadecimal: {0}",
                        value);
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a count prefix for the specified string value
        /// and appends it, followed by a space character, to the specified string
        /// builder.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use.  If this value is null, the encoding associated
        /// with the specified encoding type is used instead.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information.  This parameter is not
        /// used.
        /// </param>
        /// <param name="value">
        /// The string value whose encoded byte count is to be computed.
        /// </param>
        /// <param name="type">
        /// The encoding type to use when no explicit encoding is specified.
        /// </param>
        /// <param name="builder">
        /// Upon success, this string builder will have the count prefix appended
        /// to it.  If this value is null, a new string builder will be created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode AppendCount(
            Encoding encoding,         /* in */
            CultureInfo cultureInfo,   /* in: NOT USED */
            string value,              /* in */
            EncodingType type,         /* in */
            ref StringBuilder builder, /* in, out */
            ref Result error           /* out */
            )
        {
            if (value == null)
            {
                error = "invalid string";
                return ReturnCode.Error;
            }

            string countFormat = CountFormat;

            if (countFormat == null)
            {
                error = "count format string unavailable";
                return ReturnCode.Error;
            }

            if (encoding == null)
                encoding = GetEncoding(type);

            if (encoding == null)
            {
                error = "missing encoding for count string";
                return ReturnCode.Error;
            }

            int count = encoding.GetByteCount(value);

            string countString = String.Format(
                DefaultCultureInfo, countFormat, count);

            if (countString == null)
            {
                error = "expected count string, got null";
                return ReturnCode.Error;
            }

            if (builder == null)
            {
                int capacity = Count.PrefixSize;

                capacity += countString.Length;
                capacity += 1 /* Space */ + count;

                builder = SBF.Create(capacity);
            }

            builder.Append(countString);
            builder.Append(Characters.Space);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes the specified array of bytes into a string value
        /// using the specified encoding (or the encoding associated with the
        /// specified encoding type).
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use.  If this value is null, the encoding associated
        /// with the specified encoding type is used instead.
        /// </param>
        /// <param name="bytes">
        /// The array of bytes to decode into a string value.
        /// </param>
        /// <param name="type">
        /// The encoding type to use when no explicit encoding is specified.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the resulting
        /// string value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetString(
            Encoding encoding, /* in */
            byte[] bytes,      /* in */
            EncodingType type, /* in */
            ref string value   /* out */
            )
        {
            Result error = null;

            return GetString(
                encoding, bytes, type, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes the specified array of bytes into a string value
        /// using the specified encoding (or the encoding associated with the
        /// specified encoding type).  If no encoding can be determined, the bytes
        /// are treated as Base64.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use.  If this value is null, the encoding associated
        /// with the specified encoding type is used instead.
        /// </param>
        /// <param name="bytes">
        /// The array of bytes to decode into a string value.
        /// </param>
        /// <param name="type">
        /// The encoding type to use when no explicit encoding is specified.
        /// </param>
        /// <param name="value">
        /// Upon success, this parameter will be modified to contain the resulting
        /// string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetString(
            Encoding encoding, /* in */
            byte[] bytes,      /* in */
            EncodingType type, /* in */
            ref string value,  /* out */
            ref Result error   /* out */
            )
        {
            if (bytes == null)
            {
                value = null;
                return ReturnCode.Ok;
            }

            if (bytes.Length == 0)
            {
                value = String.Empty;
                return ReturnCode.Ok;
            }

            if (encoding == null)
                encoding = GetEncoding(type);

            try
            {
                if (encoding != null)
                {
                    value = encoding.GetString(bytes);
                }
                else
                {
                    value = Convert.ToBase64String(bytes,
                        Base64FormattingOptions.InsertLineBreaks);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string value from one encoding to
        /// another by first encoding it to bytes using the input encoding and
        /// then decoding those bytes back into a string using the output
        /// encoding.
        /// </summary>
        /// <param name="inputEncoding">
        /// The encoding to use when encoding the input value to bytes.  If this
        /// value is null, the encoding associated with the fallback input type is
        /// used instead.
        /// </param>
        /// <param name="outputEncoding">
        /// The encoding to use when decoding the bytes into the output value.  If
        /// this value is null, the encoding associated with the fallback output
        /// type is used instead.
        /// </param>
        /// <param name="fallbackInputType">
        /// The encoding type to use when no explicit input encoding is specified.
        /// </param>
        /// <param name="fallbackOutputType">
        /// The encoding type to use when no explicit output encoding is
        /// specified.
        /// </param>
        /// <param name="inputValue">
        /// The string value to convert.
        /// </param>
        /// <param name="outputValue">
        /// Upon success, this parameter will be modified to contain the converted
        /// string value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ConvertString(
            Encoding inputEncoding,
            Encoding outputEncoding,
            EncodingType fallbackInputType,
            EncodingType fallbackOutputType,
            string inputValue,
            ref string outputValue,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(inputValue))
            {
                outputValue = inputValue;
                return ReturnCode.Ok;
            }

            //
            // NOTE: If both the input and output encodings are null, this
            //       would be a conversion from the fallback encoding to
            //       the fallback encoding, which may be useless.
            //
            if ((inputEncoding == null) && (outputEncoding == null) &&
                (fallbackInputType == fallbackOutputType))
            {
                error = String.Format(
                    "cannot covert string from encoding {0} to encoding {1}",
                    fallbackInputType, fallbackOutputType);

                return ReturnCode.Error;
            }

            ReturnCode code;
            byte[] bytes = null;

            code = GetBytes(
                inputEncoding, inputValue, fallbackInputType,
                true, ref bytes, ref error);

            if (code != ReturnCode.Ok)
                return code;

            code = GetString(
                outputEncoding, bytes, fallbackOutputType,
                ref outputValue, ref error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified array of bytes from one encoding to
        /// another by first decoding it into a string using the input encoding
        /// and then encoding that string back into bytes using the output
        /// encoding.
        /// </summary>
        /// <param name="inputEncoding">
        /// The encoding to use when decoding the input bytes into a string.  If
        /// this value is null, the encoding associated with the fallback input
        /// type is used instead.
        /// </param>
        /// <param name="outputEncoding">
        /// The encoding to use when encoding the string into the output bytes.
        /// If this value is null, the encoding associated with the fallback
        /// output type is used instead.
        /// </param>
        /// <param name="fallbackInputType">
        /// The encoding type to use when no explicit input encoding is specified.
        /// </param>
        /// <param name="fallbackOutputType">
        /// The encoding type to use when no explicit output encoding is
        /// specified.
        /// </param>
        /// <param name="inputBytes">
        /// The array of bytes to convert.
        /// </param>
        /// <param name="outputBytes">
        /// Upon success, this parameter will be modified to contain the converted
        /// array of bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ConvertBytes(
            Encoding inputEncoding,
            Encoding outputEncoding,
            EncodingType fallbackInputType,
            EncodingType fallbackOutputType,
            byte[] inputBytes,
            ref byte[] outputBytes,
            ref Result error
            )
        {
            if (inputBytes == null)
            {
                outputBytes = null;
                return ReturnCode.Ok;
            }

            if (inputBytes.Length == 0)
            {
                outputBytes = new byte[0];
                return ReturnCode.Ok;
            }

            //
            // NOTE: If both the input and output encodings are null, this
            //       would be a conversion from the fallback encoding to
            //       the fallback encoding, which may be useless.
            //
            if ((inputEncoding == null) && (outputEncoding == null) &&
                (fallbackInputType == fallbackOutputType))
            {
                error = String.Format(
                    "cannot covert bytes from encoding {0} to encoding {1}",
                    fallbackInputType, fallbackOutputType);

                return ReturnCode.Error;
            }

            ReturnCode code;
            string value = null;

            code = GetString(
                inputEncoding, inputBytes, fallbackInputType,
                ref value, ref error);

            if (code != ReturnCode.Ok)
                return code;

            code = GetBytes(
                outputEncoding, value, fallbackOutputType,
                true, ref outputBytes, ref error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the specified string for the last occurrence of
        /// the specified substring, using the specified string comparison type.
        /// </summary>
        /// <param name="haystack">
        /// The string value to search within.
        /// </param>
        /// <param name="needle">
        /// The substring to search for.
        /// </param>
        /// <param name="comparisonType">
        /// The rules to use when comparing the strings.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of the substring within
        /// the string, or a negative number if it is not found.
        /// </returns>
        public static int LastIndexOf(
            string haystack,
            string needle,
            StringComparison comparisonType
            )
        {
            if (haystack == null)
                throw new ArgumentNullException("haystack");

            if (needle == null)
                throw new ArgumentNullException("needle");

#if MONO || MONO_HACKS
            //
            // HACK: *MONO* Apparently, some older versions of Mono do
            //       not gracefully handle empty strings that call into
            //       some of their LastIndexOf method overloads (i.e.
            //       those that omit the startIndex parameter).
            //
            if (haystack.Length == 0)
                return (needle.Length == 0) ? 0 : Index.Invalid;
#endif

            return haystack.LastIndexOf(needle, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all leading switch characters from the specified
        /// string value.
        /// </summary>
        /// <param name="text">
        /// The string value from which to remove leading switch characters.
        /// </param>
        /// <param name="count">
        /// Upon success, this parameter will be modified to contain the number of
        /// leading switch characters that were removed.
        /// </param>
        /// <returns>
        /// The string value with all leading switch characters removed.
        /// </returns>
        public static string TrimSwitchChars(
            string text,
            ref int count
            )
        {
            string result = text;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: Remove all leading switch chars.
                //
                result = result.TrimStart(switchChars);

                //
                // NOTE: How many chars were removed?
                //
                count = text.Length - result.Length;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified switch, comparing only the leading portion of the switch up
        /// to the length of the text, using a case-insensitive comparison.
        /// </summary>
        /// <param name="text">
        /// The text to compare against the switch.
        /// </param>
        /// <param name="switch">
        /// The switch to be matched.
        /// </param>
        /// <returns>
        /// True if the text matches the leading portion of the switch; otherwise,
        /// false.
        /// </returns>
        public static bool MatchSwitch(
            string text,
            string @switch
            )
        {
            if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(@switch))
                return false;

            return SharedStringOps.SystemNoCaseEquals(
                text, 0, @switch, 0, text.Length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the two specified string values are
        /// equal, using the comparison type configured for user-level string
        /// comparisons.
        /// </summary>
        /// <param name="left">
        /// The first string value to compare.
        /// </param>
        /// <param name="right">
        /// The second string value to compare.
        /// </param>
        /// <returns>
        /// True if the two string values are equal; otherwise, false.
        /// </returns>
        public static bool UserEquals(
            string left,
            string right
            )
        {
            return SharedStringOps.Equals(
                left, right, UserComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the two specified string values are
        /// equal, using the case-insensitive comparison type configured for
        /// user-level string comparisons.
        /// </summary>
        /// <param name="left">
        /// The first string value to compare.
        /// </param>
        /// <param name="right">
        /// The second string value to compare.
        /// </param>
        /// <returns>
        /// True if the two string values are equal; otherwise, false.
        /// </returns>
        public static bool UserNoCaseEquals(
            string left,
            string right
            )
        {
            return SharedStringOps.Equals(
                left, right, UserNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character satisfies the
        /// condition represented by the specified callback.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <param name="callback">
        /// The callback used to test the character.  If this value is null, the
        /// character is considered to not satisfy the condition.
        /// </param>
        /// <returns>
        /// True if the character satisfies the condition represented by the
        /// callback; otherwise, false.
        /// </returns>
        private static bool CharIs(
            char character,
            CharIsCallback callback
            )
        {
            if (callback != null)
                return callback(character);
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is a word
        /// character (i.e. a letter, a digit, or connector punctuation).
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is a word character; otherwise, false.
        /// </returns>
        public static bool CharIsWord(
            char character
            )
        {
            return Char.IsLetterOrDigit(character) ||
                (Char.GetUnicodeCategory(character) == UnicodeCategory.ConnectorPunctuation);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is an ASCII
        /// decimal digit.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is an ASCII decimal digit; otherwise, false.
        /// </returns>
        public static bool CharIsAsciiDigit(
            char character
            )
        {
            return (character >= Characters.Zero) && (character <= Characters.Nine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is an ASCII
        /// alphabetic character.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is an ASCII alphabetic character; otherwise,
        /// false.
        /// </returns>
        public static bool CharIsAsciiAlpha(
            char character
            )
        {
            return ((character >= Characters.A) && (character <= Characters.Z)) ||
                ((character >= Characters.a) && (character <= Characters.z));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is an ASCII
        /// alphabetic character or an ASCII decimal digit.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is an ASCII alphabetic character or an ASCII
        /// decimal digit; otherwise, false.
        /// </returns>
        public static bool CharIsAsciiAlphaOrDigit(
            char character
            )
        {
            return CharIsAsciiAlpha(character) || CharIsAsciiDigit(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is an ASCII
        /// character.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is an ASCII character; otherwise, false.
        /// </returns>
        public static bool CharIsAscii(
            char character
            )
        {
            return character <= CharMaxAscii;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is a printable
        /// character that is not white-space (i.e. a visible graphic character).
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is a visible graphic character; otherwise,
        /// false.
        /// </returns>
        public static bool CharIsGraph(
            char character
            )
        {
            return CharIsPrint(character) && !Char.IsWhiteSpace(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is one of the
        /// characters reserved by the Tcl language syntax.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is a reserved character; otherwise, false.
        /// </returns>
        public static bool CharIsReserved(
            char character
            )
        {
            switch (character)
            {
                case Characters.QuotationMark:
                case Characters.NumberSign:
                case Characters.DollarSign:
                case Characters.SemiColon:
                case Characters.OpenBracket:
                case Characters.Backslash:
                case Characters.CloseBracket:
                case Characters.OpenBrace:
                case Characters.CloseBrace:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is a printable
        /// character.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is a printable character; otherwise, false.
        /// </returns>
        public static bool CharIsPrint(
            char character
            )
        {
            if (Char.IsLetterOrDigit(character) || Char.IsWhiteSpace(character))
            {
                return true;
            }
            else
            {
                switch (Char.GetUnicodeCategory(character))
                {
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.EnclosingMark:
                    case UnicodeCategory.LetterNumber:
                    case UnicodeCategory.OtherNumber:
                    case UnicodeCategory.ConnectorPunctuation:
                    case UnicodeCategory.DashPunctuation:
                    case UnicodeCategory.OpenPunctuation:
                    case UnicodeCategory.ClosePunctuation:
                    case UnicodeCategory.InitialQuotePunctuation:
                    case UnicodeCategory.FinalQuotePunctuation:
                    case UnicodeCategory.OtherPunctuation:
                    case UnicodeCategory.MathSymbol:
                    case UnicodeCategory.CurrencySymbol:
                    case UnicodeCategory.ModifierSymbol:
                    case UnicodeCategory.OtherSymbol:
                        return true;
                    default:
                        return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is an ASCII
        /// hexadecimal digit.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is an ASCII hexadecimal digit; otherwise, false.
        /// </returns>
        public static bool CharIsAsciiHexadecimal(
            char character
            )
        {
            return CharIsAsciiDigit(character) ||
                ((character >= Characters.A) && (character <= Characters.F)) ||
                ((character >= Characters.a) && (character <= Characters.f));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is valid as the
        /// first character of a C# identifier.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is valid as the first character of a C#
        /// identifier; otherwise, false.
        /// </returns>
        public static bool CharIsIdentifierZero( /* NOTE: First C# identifier character. */
            char character
            )
        {
            return (character == Characters.Underscore) || Char.IsLetter(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is valid as a
        /// subsequent (non-first) character of a C# identifier.
        /// </summary>
        /// <param name="character">
        /// The character to test.
        /// </param>
        /// <returns>
        /// True if the character is valid as a subsequent character of a C#
        /// identifier; otherwise, false.
        /// </returns>
        public static bool CharIsIdentifierOnePlus( /* NOTE: Subsequent C# identifier characters. */
            char character
            )
        {
            return (character == Characters.Underscore) || Char.IsLetterOrDigit(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decodes the specified hexavigesimal (base-26) string into
        /// an array of bytes.  All white-space characters are removed prior to
        /// decoding.
        /// </summary>
        /// <param name="text">
        /// The hexavigesimal (base-26) string to decode.
        /// </param>
        /// <returns>
        /// The resulting array of bytes, or null if the string is null, empty, or
        /// cannot be decoded.
        /// </returns>
        public static byte[] FromBase26String(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                //
                // NOTE: Remove all white-space characters from the string.
                //
                text = RemoveWhiteSpace(text);

                //
                // NOTE: Now, get the number of real characters remaining.
                //
                int length = text.Length;

                //
                // NOTE: The number of real characters must be divisible by
                //       two because we need two characters to form one byte.
                //
                if ((length % 2) == 0)
                {
                    byte[] result = new byte[length / 2];

                    for (int index = 0; index < length; index += 2)
                    {
                        long value = 0;

                        if (Parser.ParseHexavigesimal(text, index, 2, ref value) != 2)
                            return null;

                        if ((value < byte.MinValue) || (value > byte.MaxValue))
                            return null;

                        result[index / 2] = ConversionOps.ToByte(value);
                    }

                    return result;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method encodes the specified array of bytes into a hexavigesimal
        /// (base-26) string, optionally inserting line breaks and spaces.
        /// </summary>
        /// <param name="array">
        /// The array of bytes to encode.
        /// </param>
        /// <param name="options">
        /// The formatting options that control the insertion of line breaks and
        /// spaces.
        /// </param>
        /// <returns>
        /// The resulting hexavigesimal (base-26) string, or null if the array is
        /// null.
        /// </returns>
        public static string ToBase26String(
            byte[] array,
            Base26FormattingOption options /* IGNORED */
            )
        {
            if (array != null)
            {
                int length = array.Length;
                StringBuilder result = SBF.Create(length * 2);

                for (int index = 0; index < length; index++)
                {
                    result.Append(FormatOps.Hexavigesimal(array[index], 2));

                    if ((((index + 1) % Base26GroupsPerLine) == 0) &&
                        FlagOps.HasFlags(options,
                            Base26FormattingOption.InsertLineBreaks, true))
                    {
                        result.Append(Environment.NewLine);
                    }
                    else if (((index + 1) < length) &&
                        FlagOps.HasFlags(options,
                            Base26FormattingOption.InsertSpaces, true))
                    {
                        result.Append(Characters.Space);
                    }
                }

                return StringBuilderCache.GetStringAndRelease(ref result);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a string of spaces representing the leading
        /// white-space indentation present in the specified string value,
        /// starting at the specified index and reduced by the specified number of
        /// indent spaces.
        /// </summary>
        /// <param name="value">
        /// The string value to examine for leading white-space.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index at which to begin examining the string value.
        /// </param>
        /// <param name="indentSpaces">
        /// The number of spaces to subtract from the computed indentation.
        /// </param>
        /// <param name="indent">
        /// Upon success, this parameter may be modified to contain the computed
        /// string of indentation spaces.
        /// </param>
        private static void CalculateIndent(
            string value,
            int startIndex,
            int indentSpaces,
            ref string indent
            )
        {
            if (value != null)
            {
                int index = startIndex;
                int length = value.Length;

                for (; index < length; index++)
                {
                    if (!Parser.IsWhiteSpace(
                            value[index]))
                    {
                        index--; /* NON-SPACE */
                        break;
                    }
                }

                if (index > startIndex)
                {
                    index -= indentSpaces;

                    if (index > startIndex)
                    {
                        indent = StrRepeat(
                            index - startIndex,
                            Characters.Space);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value contains
        /// more than one logical line (i.e. whether it contains a line-ending
        /// character or sequence).
        /// </summary>
        /// <param name="value">
        /// The string value to examine.
        /// </param>
        /// <returns>
        /// True if the value contains a line-ending; otherwise, false.
        /// </returns>
        public static bool IsMultiLine(
            string value
            )
        {
            int indentSpaces = 0;
            string newLine = null;
            string indent = null;

            return IsMultiLine(
                value, indentSpaces, ref newLine, ref indent);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string value contains
        /// more than one logical line and, if so, reports the line-ending that
        /// was detected along with the leading indentation for the next line.
        /// </summary>
        /// <param name="value">
        /// The string value to examine.
        /// </param>
        /// <param name="indentSpaces">
        /// The number of indentation spaces to remove when calculating the
        /// resulting indentation string.
        /// </param>
        /// <param name="newLine">
        /// Upon success, this contains the line-ending character or sequence
        /// that was detected within the value.
        /// </param>
        /// <param name="indent">
        /// Upon success, this contains the indentation string calculated for
        /// the line following the detected line-ending.
        /// </param>
        /// <returns>
        /// True if the value contains a line-ending; otherwise, false.
        /// </returns>
        public static bool IsMultiLine(
            string value,
            int indentSpaces,
            ref string newLine,
            ref string indent
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            int startIndex = value.IndexOf(
                Characters.DosNewLine);

            if (startIndex != Index.Invalid)
            {
                newLine = Characters.DosNewLine;

                CalculateIndent(
                    value, startIndex, indentSpaces,
                    ref indent);

                return true;
            }

            startIndex = value.IndexOf(
                Characters.AcornOsNewLine);

            if (startIndex != Index.Invalid)
            {
                newLine = Characters.AcornOsNewLine;

                CalculateIndent(
                    value, startIndex, indentSpaces,
                    ref indent);

                return true;
            }

            startIndex = value.IndexOfAny(
                Characters.LineTerminatorChars);

            if (startIndex == Index.Invalid)
                return false;

            newLine = value[startIndex].ToString();

            CalculateIndent(
                value, startIndex, indentSpaces,
                ref indent);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the string contained within the
        /// specified string builder contains more than one logical line and, if
        /// so, reports the line-ending that was detected along with the leading
        /// indentation for the next line.
        /// </summary>
        /// <param name="builder">
        /// The string builder whose contents should be examined.
        /// </param>
        /// <param name="indentSpaces">
        /// The number of indentation spaces to remove when calculating the
        /// resulting indentation string.
        /// </param>
        /// <param name="newLine">
        /// Upon success, this contains the line-ending character or sequence
        /// that was detected within the contents.
        /// </param>
        /// <param name="indent">
        /// Upon success, this contains the indentation string calculated for
        /// the line following the detected line-ending.
        /// </param>
        /// <returns>
        /// True if the contents contain a line-ending; otherwise, false.
        /// </returns>
        public static bool IsMultiLine(
            StringBuilder builder,
            int indentSpaces,
            ref string newLine,
            ref string indent
            )
        {
            if (builder == null)
                return false;

            return IsMultiLine(
                builder.ToString(), indentSpaces, ref newLine,
                ref indent);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text appears to be a
        /// SHA1 hash value.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text appears to be a SHA1 hash value; otherwise, false.
        /// </returns>
        public static bool IsSha1HashValue(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text) && (sha1HashValueRegEx != null))
            {
                Match match = sha1HashValueRegEx.Match(text);

                if ((match != null) && match.Success)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text appears to be a
        /// SHA512 hash value.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text appears to be a SHA512 hash value; otherwise,
        /// false.
        /// </returns>
        public static bool IsSha512HashValue(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text) && (sha512HashValueRegEx != null))
            {
                Match match = sha512HashValueRegEx.Match(text);

                if ((match != null) && match.Success)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text appears to be a
        /// base-16 (hexadecimal) encoded value.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text appears to be base-16 encoded; otherwise, false.
        /// </returns>
        public static bool IsBase16(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text) && (base16RegEx != null))
            {
                Match match = base16RegEx.Match(text.Trim());

                if ((match != null) && match.Success)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text appears to be a
        /// base-26 encoded value.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text appears to be base-26 encoded; otherwise, false.
        /// </returns>
        public static bool IsBase26(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text) && (base26RegEx != null))
            {
                Match match = base26RegEx.Match(text.Trim());

                if ((match != null) && match.Success)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the length of the specified text after removing
        /// all whitespace characters, replacing the text with its whitespace
        /// stripped form when any whitespace is present.
        /// </summary>
        /// <param name="text">
        /// On input, the text to examine; on output, the text with all
        /// whitespace characters removed when any were present.
        /// </param>
        /// <returns>
        /// The number of non-whitespace characters in the text, or an invalid
        /// length when the text is null.
        /// </returns>
        private static int LengthWithoutSpaces(
            ref string text /* in, out */
            )
        {
            if (text == null)
                return Length.Invalid;

            int length = text.Length;

            if (text.IndexOfAny(
                    Characters.WhiteSpaceChars) == Index.Invalid)
            {
                return length;
            }

            int result = 0;
            StringBuilder builder = SBF.Create(length);

            for (int index = 0; index < length; index++)
            {
                char character = text[index];

                if (Parser.IsWhiteSpace(character))
                    continue;

                builder.Append(character);
                result++;
            }

            text = StringBuilderCache.GetStringAndRelease(ref builder);
            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text appears to be a
        /// base-64 encoded value.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text appears to be base-64 encoded; otherwise, false.
        /// </returns>
        public static bool IsBase64(
            string text
            )
        {
            int length;

            if (!IsNullOrEmpty(text, out length))
            {
                //
                // HACK: In the worst cases, we have to calculate the
                //       length without any whitespace, which can be
                //       expensive because the entire string must be
                //       (re-)examined to exclude non-whitespace.
                //
                if ((LengthWithoutSpaces(ref text) % 4) == 0) /* O(N) */
                {
                    if (base64RegEx != null)
                    {
                        Match match = base64RegEx.Match(text);

                        if ((match != null) && match.Success)
                            return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text appears to be a
        /// delimited string of hexadecimal byte values.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text appears to be hexadecimal bytes; otherwise, false.
        /// </returns>
        private static bool IsHexadecimalBytes(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text) && (hexadecimalBytesRegEx != null))
            {
                Match match = hexadecimalBytesRegEx.Match(text.Trim());

                if ((match != null) && match.Success)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string value into an array of
        /// bytes, automatically detecting the encoding format (delimited
        /// hexadecimal bytes, base-16, base-64, or a GUID).
        /// </summary>
        /// <param name="value">
        /// The string value to convert into bytes.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture information to use when parsing the value.
        /// </param>
        /// <param name="bytes">
        /// Upon success, this contains the array of bytes produced from the
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        public static ReturnCode GetBytesFromString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (IsHexadecimalBytes(value))
            {
                return ArrayOps.GetBytesFromDelimitedString(
                    value, cultureInfo, ref bytes, ref error);
            }
            else if (IsBase16(value))
            {
                return ArrayOps.GetBytesFromHexadecimalString(
                    value, cultureInfo, ref bytes, ref error);
            }
            else if (IsBase64(value))
            {
                //
                // HACK: Given that the IsBase64 method returned
                //       true, this block should not actually be
                //       able to throw an exception.
                //
                try
                {
                    bytes = Convert.FromBase64String(value);
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                Guid guid = Guid.Empty;

                if (Value.GetGuid(value, cultureInfo,
                        ref guid) == ReturnCode.Ok)
                {
                    bytes = guid.ToByteArray();
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "unknown bytes string format";
                    return ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes the list separators within the specified
        /// string value by replacing comma and semicolon characters with space
        /// characters.
        /// </summary>
        /// <param name="value">
        /// The string value whose list separators should be normalized.
        /// </param>
        /// <returns>
        /// The string value with its list separators normalized to spaces.
        /// </returns>
        public static string NormalizeListSeparators(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = SBF.Create(value);

            builder.Replace(Characters.Comma, Characters.Space);
            builder.Replace(Characters.SemiColon, Characters.Space);

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes all line-endings within the specified text to
        /// the convention required by the script evaluation engine.
        /// </summary>
        /// <param name="text">
        /// The text whose line-endings should be normalized.
        /// </param>
        /// <returns>
        /// The text with all line-endings normalized.
        /// </returns>
        public static string NormalizeLineEndings(
            string text
            )
        {
            //
            // NOTE: If the original script string is null or empty, just
            //       return it verbatim.
            //
            if (String.IsNullOrEmpty(text))
                return text;

            //
            // NOTE: Create a string builder instance based on the script
            //       text.
            //
            StringBuilder builder = SBF.Create(text);

            //
            // NOTE: Using the created string builder, modify it in-place
            //       to normalize all line-endings to the convention that
            //       is required by the script evaluation engine.
            //
            FixupLineEndings(builder);

            //
            // NOTE: Return the resulting string to the caller.
            //
            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method modifies the contents of the specified string builder
        /// in-place, normalizing all line-endings to the Unix line-ending
        /// convention.
        /// </summary>
        /// <param name="builder">
        /// The string builder whose contents should be modified in-place.
        /// </param>
        public static void FixupLineEndings(
            StringBuilder builder
            )
        {
            //
            // NOTE: If the original string builer is null or empty,
            //       just return.
            //
            if ((builder == null) || (builder.Length == 0))
                return;

            //
            // HACK: Change the end-of-line character or sequence for
            //       this platform to the Unix end-of-line character.
            //       We have to do this because we filled the entire
            //       buffer with one call and there was no opportunity
            //       to discriminate on a per-character basis as there
            //       is when reading the entire stream.
            //
            builder.Replace(
                Characters.DosNewLine, Characters.LineFeedString);

            //
            // HACK: Also, in case non-standard "reversed" end-of-line
            //       sequences are present, handle them as well.  This
            //       will likely be an extremely rare case.  They will
            //       only seen on antique BBC Micro hardware and in an
            //       operating system known as RISC OS, both of which
            //       originated from a company named Acorn Computers
            //       Ltd in the 1980s.
            //
            builder.Replace(
                Characters.AcornOsNewLine, Characters.LineFeedString);

            //
            // HACK: To support the Mac end-of-line convention we need
            //       to replace the carriage-return character with the
            //       line-feed character (i.e. the Unix end-of-line
            //       character).  This will result in carriage-return,
            //       line-feed pairs (if there were any left) turning
            //       into two consecutive line-feeds; however, when
            //       using a StringBuilder, this is the only really
            //       efficient way.
            //
            builder.Replace(
                Characters.CarriageReturn, Characters.LineFeed);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method modifies the contents of the specified string builder
        /// in-place, replacing each line-ending character with a visible
        /// representation suitable for display.
        /// </summary>
        /// <param name="builder">
        /// The string builder whose contents should be modified in-place.
        /// </param>
        /// <param name="extended">
        /// Non-zero to use extended (non-Unicode) characters to represent the
        /// line-ending characters.
        /// </param>
        /// <param name="unicode">
        /// Non-zero to use Unicode arrow characters to represent the
        /// line-ending characters; this takes precedence over the extended
        /// parameter.
        /// </param>
        public static void FixupDisplayLineEndings(
            StringBuilder builder,
            bool extended,
            bool unicode
            )
        {
            //
            // NOTE: If the original string builer is null or empty,
            //       just return.
            //
            if ((builder == null) || (builder.Length == 0))
                return;

            if (unicode)
            {
                //
                // NOTE: Since the caller requested the use of Unicode
                //       characters, use the appropriate arrow character to
                //       replace each line-ending character.
                //
                builder.Replace(
                    Characters.LineFeed, Characters.DownwardsArrow);

                builder.Replace(
                    Characters.CarriageReturn, Characters.LeftwardsArrow);
            }
            else if (extended)
            {
                //
                // NOTE: Otherwise (non-Unicode), just use the extended
                //       characters to replace each line-ending character.
                //
                builder.Replace(
                    Characters.LineFeed, Characters.SectionSign);

                builder.Replace(
                    Characters.CarriageReturn, Characters.PilcrowSign);
            }
            else
            {
                //
                // NOTE: Otherwise (nothing?), fallback to using the space
                //       character to replace each line-ending character.
                //
                builder.Replace(
                    Characters.LineFeed, Characters.Space);

                builder.Replace(
                    Characters.CarriageReturn, Characters.Space);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures that the specified string value uses carriage
        /// return, line-feed pairs for its line-endings, inserting the missing
        /// carriage returns when the value contains line-feeds but no carriage
        /// returns.
        /// </summary>
        /// <param name="value">
        /// The string value whose line-endings should be modified.
        /// </param>
        /// <returns>
        /// The string value with carriage returns added as needed.
        /// </returns>
        public static string ForceCarriageReturns(
            string value
            )
        {
            string result = value;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: If the string contains line feeds and no carriage
                //       returns, then it must be modified to include the
                //       "missing" carriage returns.
                //
                if ((result.IndexOf(
                        Characters.CarriageReturn) == Index.Invalid) &&
                    (result.IndexOf(
                        Characters.LineFeed) != Index.Invalid))
                {
                    return result.Replace(
                        Characters.UnixNewLine, Characters.DosNewLine);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a single trailing platform line-ending from the
        /// specified string value, when present.
        /// </summary>
        /// <param name="value">
        /// On input, the string value to examine; on output, the value with a
        /// single trailing platform line-ending removed when present.
        /// </param>
        public static void StripNewLine(
            ref string value /* in, out */
            )
        {
            if (String.IsNullOrEmpty(value))
                return;

            string newLine = Environment.NewLine;

            if (newLine == null) /* IMPOSSIBLE */
                return;

            if (SharedStringOps.SystemEndsWith(value, newLine))
            {
                value = value.Substring(
                    0, value.Length - newLine.Length);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified text into a comma-separated list
        /// of values suitable for use with the enumeration parsing facilities
        /// (e.g. for flags fields).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when splitting the text into a list.
        /// </param>
        /// <param name="text">
        /// The text to convert into a comma-separated list.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the resulting comma-separated list; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public static ReturnCode StringToEnumList(
            Interpreter interpreter,
            string text,
            ref Result result
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        interpreter, text, 0, Length.Invalid, true,
                        ref list, ref result) == ReturnCode.Ok)
                {
                    //
                    // NOTE: Make friendly to Enum.Parse for flags fields.
                    //
                    result = list.ToString(
                        Characters.CommaSpaceString, null, false);

                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                //
                // NOTE: Empty list, OK.
                //
                result = String.Empty;

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method determines whether the specified character is recognized
        /// as a command-line switch prefix.
        /// </summary>
        /// <param name="character">
        /// The character to examine.
        /// </param>
        /// <returns>
        /// True if the character is a command-line switch prefix; otherwise,
        /// false.
        /// </returns>
        private static bool CharIsSwitch( /* NOT USED */
            char character
            )
        {
            return (switchCharList != null) && switchCharList.Contains(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is an
        /// alphabetic letter.
        /// </summary>
        /// <param name="character">
        /// The character to examine.
        /// </param>
        /// <returns>
        /// True if the character is an alphabetic letter; otherwise, false.
        /// </returns>
        private static bool CharIsAlpha( /* NOT USED */
            char character
            )
        {
            switch (Char.GetUnicodeCategory(character))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is a decimal
        /// digit.
        /// </summary>
        /// <param name="character">
        /// The character to examine.
        /// </param>
        /// <returns>
        /// True if the character is a decimal digit; otherwise, false.
        /// </returns>
        private static bool CharIsDigit( /* NOT USED */
            char character
            )
        {
            return Char.GetUnicodeCategory(character) == UnicodeCategory.DecimalDigitNumber;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the substring of the specified string between
        /// the specified first and last character indexes, inclusive, clamping
        /// the indexes to the bounds of the string and swapping them when they
        /// are out of order.
        /// </summary>
        /// <param name="text">
        /// The string to extract the substring from.
        /// </param>
        /// <param name="firstIndex">
        /// The index of the first character to include in the substring.
        /// </param>
        /// <param name="lastIndex">
        /// The index of the last character to include in the substring.
        /// </param>
        /// <returns>
        /// The extracted substring, or the original string when it is null or
        /// empty.
        /// </returns>
        private static string Slice( /* NOT USED */
            string text,
            int firstIndex,
            int lastIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (firstIndex < 0)
                    firstIndex = 0;

                if (lastIndex >= text.Length)
                    lastIndex = text.Length - 1;

                if (firstIndex > lastIndex)
                {
                    int swap = firstIndex;
                    firstIndex = lastIndex;
                    lastIndex = swap;
                }

                return text.Substring(firstIndex, lastIndex - firstIndex + 1);
            }

            return text;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces occurrences of a substring within a string with
        /// another substring, using the specified comparison type and honoring
        /// an optional maximum number of replacements.
        /// </summary>
        /// <param name="text">
        /// The string to perform the replacements on.
        /// </param>
        /// <param name="oldValue">
        /// The substring to search for and replace.
        /// </param>
        /// <param name="newValue">
        /// The substring to substitute for each occurrence of the old value.
        /// </param>
        /// <param name="comparisonType">
        /// The type of string comparison to use when searching for the old
        /// value.
        /// </param>
        /// <param name="maximum">
        /// The maximum number of replacements to perform, or a value less than
        /// or equal to zero to replace all occurrences.
        /// </param>
        /// <param name="count">
        /// Upon return, this parameter is incremented by the number of
        /// replacements that were performed.
        /// </param>
        /// <returns>
        /// The resulting string after the replacements have been performed.
        /// </returns>
        private static string StrReplace(
            string text,
            string oldValue,
            string newValue,
            StringComparison comparisonType,
            int maximum,
            ref int count
            ) /* NOT USED */
        {
            StringBuilder result = SBF.Create();

            if (!String.IsNullOrEmpty(text))
            {
                if (!String.IsNullOrEmpty(oldValue))
                {
                    int index = 0;
                    int oldIndex = text.IndexOf(oldValue, index, comparisonType);

                    while ((index < text.Length) || (oldIndex != Index.Invalid))
                    {
                        if (oldIndex != Index.Invalid)
                        {
                            //
                            // NOTE: Did we skip some initial portion of the original
                            //       string that we still need to append to the result?
                            //
                            if (oldIndex > index)
                            {
                                //
                                // NOTE: Append the original characters in the string
                                //       between where the last portion of the original
                                //       string we handled ends and the next substring
                                //       to replace begins.
                                //
                                result.Append(text, index, oldIndex - index);

                                //
                                // NOTE: Advance the origianl string index beyond what
                                //       we just appended.
                                //
                                index += (oldIndex - index);
                            }

                            //
                            // NOTE: Append the new substring to replace the one we
                            //       were looking for.
                            //
                            if (!String.IsNullOrEmpty(newValue))
                                result.Append(newValue);

                            //
                            // NOTE: We replaced another instance of the substring.
                            //
                            count++;

                            //
                            // NOTE: Skip past the substring we replaced in the original
                            //       string.
                            //
                            index += oldValue.Length;

                            //
                            // NOTE: Do we want to replace all instances of the substring
                            //       to replace?
                            //
                            if ((maximum <= 0) || (count < maximum))
                            {
                                //
                                // NOTE: Find the next instance of the substring to
                                //       replace.
                                //
                                oldIndex = text.IndexOf(oldValue, index, comparisonType);
                            }
                            else
                            {
                                //
                                // NOTE: Terminate the loop prematurely because they do
                                //       not want to replace all instances of the
                                //       substring.
                                //
                                oldIndex = Index.Invalid;
                            }

                            //
                            // NOTE: Is there going to be another substring to replace
                            //       (during the next loop iteration)?
                            //
                            if (oldIndex != Index.Invalid)
                            {
                                //
                                // NOTE: Append the original characters in the string
                                //       between where the substring to replace ended
                                //       and the next substring to replace begins.
                                //
                                result.Append(text, index, oldIndex - index);

                                //
                                // NOTE: Advance the origianl string index beyond what
                                //       we just appended.
                                //
                                index += (oldIndex - index);
                            }
                        }
                        else
                        {
                            //
                            // NOTE: We should now be done replacing the substring.
                            //       Append the rest of the original string verbatim.
                            //
                            result.Append(text, index, text.Length - index);

                            //
                            // NOTE: Update the index to reflect the fact that we just
                            //       added the entire remainder of the original string.
                            //
                            index += (text.Length - index);
                        }
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether all (or any) of the characters in the
        /// specified string satisfy the predicate represented by the supplied
        /// callback.
        /// </summary>
        /// <param name="value">
        /// The string to be checked.  May be null or empty.
        /// </param>
        /// <param name="callback">
        /// The per-character predicate used to test each character of the string.
        /// </param>
        /// <param name="not">
        /// Non-zero to invert the sense of the per-character predicate.
        /// </param>
        /// <param name="any">
        /// Non-zero to require that at least one character satisfy the predicate;
        /// zero to require that all characters satisfy it.
        /// </param>
        /// <param name="nullOrEmpty">
        /// The value to return when the specified string is null or empty.
        /// </param>
        /// <param name="failIndex">
        /// Upon failure, this receives the zero-based index of the first character
        /// that did not satisfy the predicate.
        /// </param>
        /// <returns>
        /// True if the string satisfies the (possibly inverted) predicate subject to
        /// the specified semantics; otherwise, false.
        /// </returns>
        public static bool StringIs(
            string value,            /* in */
            CharIsCallback callback, /* in */
            bool not,                /* in */
            bool any,                /* in */
            bool nullOrEmpty,        /* in */
            ref int failIndex        /* out */
            )
        {
            int length;

            if (IsNullOrEmpty(value, out length))
                return nullOrEmpty;

            int ok = 0;
            int? notIndex = null;

            for (int index = 0; index < length; index++)
            {
                if (CharIs(value[index], callback) == not)
                {
                    if (notIndex == null)
                        notIndex = index;
                }
                else
                {
                    ok++;
                }
            }

            if (notIndex != null)
                failIndex = (int)notIndex;

            return (not || any) ? (ok > 0) : (ok == length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether all (or any) of the characters in the
        /// specified string satisfy the predicate(s) represented by the supplied
        /// callbacks, using one callback for the first character and another callback
        /// for the remaining characters.
        /// </summary>
        /// <param name="value">
        /// The string to be checked.  May be null or empty.
        /// </param>
        /// <param name="zeroCallback">
        /// The per-character predicate used to test the first character of the
        /// string.
        /// </param>
        /// <param name="onePlusCallback">
        /// The per-character predicate used to test the characters after the first
        /// one.  This parameter is optional and may be null, in which case the first
        /// callback is used for all characters.
        /// </param>
        /// <param name="not">
        /// Non-zero to invert the sense of the per-character predicate.
        /// </param>
        /// <param name="any">
        /// Non-zero to require that at least one character satisfy the predicate;
        /// zero to require that all characters satisfy it.
        /// </param>
        /// <param name="nullOrEmpty">
        /// The value to return when the specified string is null or empty.
        /// </param>
        /// <param name="failIndex">
        /// Upon failure, this receives the zero-based index of the first character
        /// that did not satisfy the predicate.
        /// </param>
        /// <returns>
        /// True if the string satisfies the (possibly inverted) predicate subject to
        /// the specified semantics; otherwise, false.
        /// </returns>
        public static bool StringIs(
            string value,                   /* in */
            CharIsCallback zeroCallback,    /* in */
            CharIsCallback onePlusCallback, /* in: OPTIONAL */
            bool not,                       /* in */
            bool any,                       /* in */
            bool nullOrEmpty,               /* in */
            ref int failIndex               /* out */
            )
        {
            int length;

            if (IsNullOrEmpty(value, out length))
                return nullOrEmpty;

            int ok = 0;
            int? notIndex = null;
            CharIsCallback callback = zeroCallback;

            for (int index = 0; index < length; index++)
            {
                if (CharIs(value[index], callback) == not)
                {
                    if (notIndex == null)
                        notIndex = index;
                }
                else
                {
                    ok++;
                }

                if ((index == 0) && (onePlusCallback != null))
                    callback = onePlusCallback;
            }

            if (notIndex != null)
                failIndex = (int)notIndex;

            return (not || any) ? (ok > 0) : (ok == length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the length of the longest string within the
        /// specified array of strings.
        /// </summary>
        /// <param name="values">
        /// The array of strings to examine.  May be null, and individual elements
        /// may be null.
        /// </param>
        /// <returns>
        /// The length of the longest non-null string, or an invalid length if there
        /// are no non-null strings.
        /// </returns>
        public static int GetMaximumLength(
            params string[] values
            )
        {
            int maximumLength = Length.Invalid;

            if (values != null)
            {
                foreach (string value in values)
                {
                    if (value == null)
                        continue;

                    int length = value.Length;

                    if ((maximumLength == Length.Invalid) ||
                        (length > maximumLength))
                    {
                        maximumLength = length;
                    }
                }
            }

            return maximumLength;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the specified replacement string as a script using
        /// the supplied interpreter, replacing it with the script result and
        /// recalculating its length.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate the replacement script.  If null, no
        /// evaluation is performed.
        /// </param>
        /// <param name="replacement">
        /// Upon input, the replacement script to evaluate.  Upon success, this
        /// receives the result of evaluating that script.
        /// </param>
        /// <param name="replacementLength">
        /// Upon input, the length of the replacement string.  Upon success, this
        /// receives the length of the evaluated replacement string.
        /// </param>
        /// <returns>
        /// True if the replacement was evaluated successfully (or no evaluation was
        /// necessary); otherwise, false.
        /// </returns>
        private static bool EvaluateScriptReplacement(
            Interpreter interpreter,  /* in */
            ref string replacement,   /* in, out */
            ref int replacementLength /* in, out */
            )
        {
            if (interpreter != null)
            {
                if (interpreter.EvaluateScriptOrBackgroundError(
                        ref replacement) != ReturnCode.Ok)
                {
                    //
                    // NOTE: Evaluation of replacement script
                    //       failed somehow, skip it.
                    //
                    return false;
                }

                replacementLength = (replacement != null) ?
                    replacement.Length : 0;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to match the specified pattern against the specified
        /// text at the given starting index, optionally performing a replacement.
        /// This overload discards the matched and replacement values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate replacement scripts, if any.  May be
        /// null.
        /// </param>
        /// <param name="mode">
        /// The matching mode (e.g. exact or regular expression) to use, possibly
        /// combined with matching flags.
        /// </param>
        /// <param name="text">
        /// The text to be searched.  May be null or empty.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index within the text at which to begin matching.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against the text.
        /// </param>
        /// <param name="patternLength">
        /// The length of the pattern, in characters.
        /// </param>
        /// <param name="replacement">
        /// The replacement value to use when a match is found and replacement is
        /// requested.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison semantics to use for exact matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use for regular expression matching.
        /// </param>
        /// <param name="subSpec">
        /// Non-zero to translate regular expression substitution specifications
        /// within the replacement value.
        /// </param>
        /// <param name="replace">
        /// Non-zero to perform the replacement within the string builder.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the matched (or skipped) text to the string builder.
        /// </param>
        /// <param name="oldLength">
        /// Upon success, this receives the length of the text that was matched (or
        /// replaced).
        /// </param>
        /// <param name="builder">
        /// Upon input, the string builder being built.  Upon success, it may be
        /// modified to contain the replacement or appended text.  May be null.
        /// </param>
        /// <returns>
        /// True if the pattern was matched at the specified starting index;
        /// otherwise, false.
        /// </returns>
        private static bool MatchForStrMap(
            Interpreter interpreter,         /* in */
            MatchMode mode,                  /* in */
            string text,                     /* in */
            int startIndex,                  /* in */
            string pattern,                  /* in */
            int patternLength,               /* in */
            string replacement,              /* in */
            StringComparison comparisonType, /* in */
            RegexOptions regExOptions,       /* in */
            bool subSpec,                    /* in */
            bool replace,                    /* in */
            bool append,                     /* in */
            ref int oldLength,               /* out */
            ref StringBuilder builder        /* in, out */
            )
        {
            string oldValue = null;
            string newValue = null;

            return MatchForStrMap(interpreter,
                mode, text, startIndex, pattern, patternLength, replacement,
                comparisonType, regExOptions, subSpec, replace, append,
                ref oldLength, ref oldValue, ref newValue, ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to match the specified pattern against the specified
        /// text at the given starting index, optionally performing a replacement,
        /// also reporting the matched and replacement values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate replacement scripts, if any.  May be
        /// null.
        /// </param>
        /// <param name="mode">
        /// The matching mode (e.g. exact or regular expression) to use, possibly
        /// combined with matching flags.
        /// </param>
        /// <param name="text">
        /// The text to be searched.  May be null or empty.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index within the text at which to begin matching.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against the text.
        /// </param>
        /// <param name="patternLength">
        /// The length of the pattern, in characters.
        /// </param>
        /// <param name="replacement">
        /// The replacement value to use when a match is found and replacement is
        /// requested.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison semantics to use for exact matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use for regular expression matching.
        /// </param>
        /// <param name="subSpec">
        /// Non-zero to translate regular expression substitution specifications
        /// within the replacement value.
        /// </param>
        /// <param name="replace">
        /// Non-zero to perform the replacement within the string builder.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the matched (or skipped) text to the string builder.
        /// </param>
        /// <param name="oldLength">
        /// Upon success, this receives the length of the text that was matched (or
        /// replaced).
        /// </param>
        /// <param name="oldValue">
        /// Upon success, this receives the value that was matched.
        /// </param>
        /// <param name="newValue">
        /// Upon success, this receives the replacement value.
        /// </param>
        /// <param name="builder">
        /// Upon input, the string builder being built.  Upon success, it may be
        /// modified to contain the replacement or appended text.  May be null.
        /// </param>
        /// <returns>
        /// True if the pattern was matched at the specified starting index;
        /// otherwise, false.
        /// </returns>
        private static bool MatchForStrMap(
            Interpreter interpreter,         /* in */
            MatchMode mode,                  /* in */
            string text,                     /* in */
            int startIndex,                  /* in */
            string pattern,                  /* in */
            int patternLength,               /* in */
            string replacement,              /* in */
            StringComparison comparisonType, /* in */
            RegexOptions regExOptions,       /* in */
            bool subSpec,                    /* in */
            bool replace,                    /* in */
            bool append,                     /* in */
            ref int oldLength,               /* out */
            ref string oldValue,             /* out */
            ref string newValue,             /* out */
            ref StringBuilder builder        /* in, out */
            )
        {
            bool evaluate = FlagOps.HasFlags(mode, MatchMode.Evaluate, true);

            //
            // BUGFIX: Also strip the complex-mode bits (e.g. Evaluate, which was
            //         already extracted just above) before validating the simple
            //         mode.  They are NOT part of FlagsMask, so leaving them set
            //         made the (mode != Exact && mode != RegExp) guard below fail
            //         for [string map -eval] (et al), silently turning it into a
            //         no-op.  See FINDINGS F52.
            //
            mode &= ~(MatchMode.FlagsMask | MatchMode.ComplexModeMask);

            if ((mode != MatchMode.Exact) && (mode != MatchMode.RegExp))
                return false;

            if (String.IsNullOrEmpty(text))
                return false;

            int textLength = text.Length;

            if ((startIndex < 0) || (startIndex >= textLength))
                return false;

            int replacementLength = 0;

            if (replacement != null)
                replacementLength = replacement.Length;

            switch (mode)
            {
                case MatchMode.Exact:
                    {
                        if (SharedStringOps.Equals(
                                text, startIndex, pattern, 0,
                                patternLength, comparisonType))
                        {
                            if (evaluate && !EvaluateScriptReplacement(
                                    interpreter, ref replacement,
                                    ref replacementLength))
                            {
                                return false;
                            }

                            if (replace && (builder != null))
                            {
                                oldLength = replacementLength;

                                oldValue = text.Substring(
                                    startIndex, patternLength);

                                newValue = replacement;

                                builder.Remove(
                                    startIndex, patternLength);

                                builder.Insert(
                                    startIndex, replacement);
                            }
                            else
                            {
                                oldLength = patternLength;
                                oldValue = pattern;
                                newValue = replacement;

                                if (append && (patternLength > 0))
                                {
                                    if (builder == null)
                                        builder = SBF.CreateNoCache(); /* EXEMPT */

                                    builder.Append(
                                        text, startIndex, patternLength);
                                }
                            }

                            return true;
                        }
                        break;
                    }
                case MatchMode.RegExp:
                    {
                        Regex regEx = RegExOps.Create(
                            pattern, regExOptions); /* throw */

                        if (regEx == null)
                            break;

                        Match match = regEx.Match(
                            text, startIndex); /* throw */

                        int matchIndex;
                        int matchLength;
                        string matchValue;

                        if (RegExOps.GetMatchSuccess(
                                match, 0, out matchIndex,
                                out matchLength, out matchValue))
                        {
                            //
                            // BUGFIX: The non-replace (char-by-char) [string map]
                            //         driver (StrMap) advances one position at a
                            //         time and expects a match ANCHORED at the
                            //         current index.  A regular-expression match
                            //         can occur FORWARD of startIndex; honoring it
                            //         here made StrMap drop the skipped prefix and
                            //         garble 2-or-more matches.  Treat a
                            //         non-anchored match as "no match here" and let
                            //         the driver advance to the match position on
                            //         its own.  (The replace driver, StrMultiMap,
                            //         wants forward matching and is handled in the
                            //         "replace" branch below.)  See FINDINGS F53.
                            //
                            if (!replace && (matchIndex != startIndex))
                                break;

                            if (subSpec)
                            {
                                replacement = RegExOps.TranslateSubSpec(
                                    regEx, match, replacement);

                                replacementLength = (replacement != null) ?
                                    replacement.Length : 0;
                            }

                            if (evaluate && !EvaluateScriptReplacement(
                                    interpreter, ref replacement,
                                    ref replacementLength))
                            {
                                return false;
                            }

                            if (replace && (builder != null))
                            {
                                //
                                // BUGFIX: The replace driver (StrMultiMap)
                                //         advances its start index by oldLength.
                                //         A regular-expression match can occur
                                //         FORWARD of startIndex, so the advance
                                //         must cover the skipped prefix plus the
                                //         replacement; otherwise the next search
                                //         re-scans the skipped text (and part of
                                //         the just-inserted replacement), causing
                                //         over-wrapping on 2-or-more matches.
                                //         (For exact matches matchIndex ==
                                //         startIndex, so this is unchanged.)  See
                                //         FINDINGS F54.
                                //
                                oldLength = (matchIndex - startIndex) +
                                    replacementLength;

                                oldValue = text.Substring(
                                    matchIndex, matchLength);

                                newValue = replacement;

                                builder.Remove(
                                    matchIndex, matchLength);

                                builder.Insert(
                                    matchIndex, replacement);
                            }
                            else
                            {
                                oldLength = (matchIndex -
                                    startIndex) + matchLength;

                                //
                                // BUGFIX: The matched text is the regular
                                //         expression match value, not a
                                //         "startIndex .. matchIndex + 1" slice
                                //         (which was the wrong length, used the
                                //         wrong endpoint, and could even throw).
                                //         oldValue feeds StrMap's over-quota
                                //         passthrough.  See FINDINGS F53.
                                //
                                oldValue = matchValue;

                                newValue = replacement;

                                if (append &&
                                    (matchIndex > startIndex))
                                {
                                    if (builder == null)
                                        builder = SBF.CreateNoCache(); /* EXEMPT */

                                    builder.Append(
                                        text, startIndex, matchIndex - startIndex);
                                }
                            }

                            return true;
                        }
                        break;
                    }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to match any of the specified patterns against the
        /// specified text at the given starting index, optionally performing a
        /// replacement using the first pattern that matches.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate replacement scripts, if any.  May be
        /// null.
        /// </param>
        /// <param name="mode">
        /// The matching mode (e.g. exact or regular expression) to use, possibly
        /// combined with matching flags.
        /// </param>
        /// <param name="text">
        /// The text to be searched.  May be null or empty.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index within the text at which to begin matching.
        /// </param>
        /// <param name="patterns">
        /// The list of pattern/replacement pairs to attempt to match.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison semantics to use for exact matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use for regular expression matching.
        /// </param>
        /// <param name="allowEmpty">
        /// Non-zero to allow empty patterns to be matched; zero to skip them.
        /// </param>
        /// <param name="subSpace">
        /// Non-zero to translate regular expression substitution specifications
        /// within the replacement value.
        /// </param>
        /// <param name="replace">
        /// Non-zero to perform the replacement within the string builder.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the matched (or skipped) text to the string builder.
        /// </param>
        /// <param name="oldLength">
        /// Upon success, this receives the length of the text that was matched (or
        /// replaced).
        /// </param>
        /// <param name="oldValue">
        /// Upon success, this receives the value that was matched.
        /// </param>
        /// <param name="newValue">
        /// Upon success, this receives the replacement value.
        /// </param>
        /// <param name="builder">
        /// Upon input, the string builder being built.  Upon success, it may be
        /// modified to contain the replacement or appended text.  May be null.
        /// </param>
        /// <returns>
        /// True if any of the patterns matched at the specified starting index;
        /// otherwise, false.
        /// </returns>
        private static bool StrInMap(
            Interpreter interpreter,         /* in */
            MatchMode mode,                  /* in */
            string text,                     /* in */
            int startIndex,                  /* in */
            StringPairList patterns,         /* in */
            StringComparison comparisonType, /* in */
            RegexOptions regExOptions,       /* in */
            bool allowEmpty,                 /* in */
            bool subSpace,                   /* in */
            bool replace,                    /* in */
            bool append,                     /* in */
            ref int oldLength,               /* out */
            ref string oldValue,             /* out */
            ref string newValue,             /* out */
            ref StringBuilder builder        /* in, out */
            )
        {
            if ((patterns == null) || (patterns.Count == 0))
                return false;

            foreach (StringPair pair in patterns)
            {
                string pattern = pair.X;

                if (pattern == null)
                    continue;

                int patternLength = pattern.Length;

                if (!allowEmpty && (patternLength == 0))
                    continue;

                string replacement = pair.Y;

                try
                {
                    if (MatchForStrMap(
                            interpreter, mode, text,
                            startIndex, pattern,
                            patternLength, replacement,
                            comparisonType, regExOptions,
                            subSpace, replace, append,
                            ref oldLength, ref oldValue,
                            ref newValue, ref builder))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(StringOps).Name,
                        TracePriority.StringError);

                    break;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method repeatedly replaces each pattern in the specified list with
        /// its associated replacement value throughout the specified text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate replacement scripts, if any.  May be
        /// null.
        /// </param>
        /// <param name="mode">
        /// The matching mode (e.g. exact or regular expression) to use, possibly
        /// combined with matching flags.
        /// </param>
        /// <param name="text">
        /// The text to be searched.  May be null or empty.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index within the text at which to begin matching.
        /// </param>
        /// <param name="patterns">
        /// The list of pattern/replacement pairs to apply to the text.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison semantics to use for exact matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use for regular expression matching.
        /// </param>
        /// <param name="maximum">
        /// The maximum number of replacements to perform, or an invalid count for no
        /// limit.
        /// </param>
        /// <param name="subSpec">
        /// Non-zero to translate regular expression substitution specifications
        /// within the replacement values.
        /// </param>
        /// <param name="count">
        /// Upon input, the number of replacements performed so far.  Upon return,
        /// this is incremented by the number of replacements performed by this
        /// method.
        /// </param>
        /// <returns>
        /// The text after all applicable replacements have been performed.
        /// </returns>
        public static string StrMultiMap(
            Interpreter interpreter,         /* in */
            MatchMode mode,                  /* in */
            string text,                     /* in */
            int startIndex,                  /* in */
            StringPairList patterns,         /* in */
            StringComparison comparisonType, /* in */
            RegexOptions regExOptions,       /* in */
            int maximum,                     /* in */
            bool subSpec,                    /* in */
            ref int count                    /* in, out */
            )
        {
            StringBuilder builder = SBF.Create(text);

            if (patterns != null)
            {
                foreach (StringPair pair in patterns)
                {
                    string pattern = pair.X;

                    if (pattern == null)
                        continue;

                    int patternLength = pattern.Length;

                    if (patternLength == 0)
                        continue;

                    string replacement = pair.Y;

                    if (replacement == null)
                        continue;

                    int localStartIndex = startIndex;

                    while (true)
                    {
                        try
                        {
                            if ((maximum != Count.Invalid) &&
                                (count >= maximum))
                            {
                                break;
                            }

                            int oldLength = 0;

                            if (MatchForStrMap(interpreter,
                                    mode, builder.ToString(),
                                    localStartIndex, pattern,
                                    patternLength, replacement,
                                    comparisonType, regExOptions,
                                    subSpec, true, false,
                                    ref oldLength, ref builder))
                            {
                                localStartIndex += oldLength;
                            }
                            else
                            {
                                break;
                            }

                            count++;
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(StringOps).Name,
                                TracePriority.StringError);

                            break;
                        }
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces each pattern in the specified list with its
        /// associated replacement value throughout the specified text.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate replacement scripts, if any.  May be
        /// null.
        /// </param>
        /// <param name="mode">
        /// The matching mode (e.g. exact or regular expression) to use, possibly
        /// combined with matching flags.
        /// </param>
        /// <param name="text">
        /// The text to be searched.  May be null or empty.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index within the text at which to begin matching.
        /// </param>
        /// <param name="patterns">
        /// The list of pattern/replacement pairs to apply to the text.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison semantics to use for exact matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use for regular expression matching.
        /// </param>
        /// <param name="maximum">
        /// The maximum number of replacements to perform, or an invalid count for no
        /// limit.
        /// </param>
        /// <param name="subSpec">
        /// Non-zero to translate regular expression substitution specifications
        /// within the replacement values.
        /// </param>
        /// <returns>
        /// The text after all applicable replacements have been performed.
        /// </returns>
        public static string StrMap(
            Interpreter interpreter,         /* in */
            MatchMode mode,                  /* in */
            string text,                     /* in */
            int startIndex,                  /* in */
            StringPairList patterns,         /* in */
            StringComparison comparisonType, /* in */
            RegexOptions regExOptions,       /* in */
            int maximum,                     /* in */
            bool subSpec                     /* in */
            )
        {
            int count = 0;

            return StrMap(
                interpreter, mode, text, startIndex, patterns,
                comparisonType, regExOptions, maximum, subSpec,
                ref count);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces each pattern in the specified list with its
        /// associated replacement value throughout the specified text, also
        /// reporting the number of replacements performed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate replacement scripts, if any.  May be
        /// null.
        /// </param>
        /// <param name="mode">
        /// The matching mode (e.g. exact or regular expression) to use, possibly
        /// combined with matching flags.
        /// </param>
        /// <param name="text">
        /// The text to be searched.  May be null or empty.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based index within the text at which to begin matching.
        /// </param>
        /// <param name="patterns">
        /// The list of pattern/replacement pairs to apply to the text.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison semantics to use for exact matching.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use for regular expression matching.
        /// </param>
        /// <param name="maximum">
        /// The maximum number of replacements to perform, or an invalid count for no
        /// limit.
        /// </param>
        /// <param name="subSpec">
        /// Non-zero to translate regular expression substitution specifications
        /// within the replacement values.
        /// </param>
        /// <param name="count">
        /// Upon input, the number of replacements performed so far.  Upon return,
        /// this is incremented by the number of replacements performed by this
        /// method.
        /// </param>
        /// <returns>
        /// The text after all applicable replacements have been performed.
        /// </returns>
        public static string StrMap(
            Interpreter interpreter,         /* in */
            MatchMode mode,                  /* in */
            string text,                     /* in */
            int startIndex,                  /* in */
            StringPairList patterns,         /* in */
            StringComparison comparisonType, /* in */
            RegexOptions regExOptions,       /* in */
            int maximum,                     /* in */
            bool subSpec,                    /* in */
            ref int count                    /* in, out */
            )
        {
            //
            // BUGFIX: These are not errors, just return their original
            //         string verbatim.
            //
            if (String.IsNullOrEmpty(text))
                return text;

            if ((patterns == null) || (patterns.Count == 0))
                return text;

            int length = text.Length;
            int index, index2;
            StringBuilder builder = SBF.Create(length);

            for (index = startIndex, index2 = startIndex; index < length; index++)
            {
                int oldLength = 0;
                string oldValue = null;
                string newValue = null;

                //
                // NOTE: Attempt to match at the current location any of
                //       the 'old' (from) values in our map.
                //
                if (StrInMap(
                        interpreter, mode, text, index, patterns,
                        comparisonType, regExOptions, false, subSpec,
                        false, false, ref oldLength, ref oldValue,
                        ref newValue, ref builder))
                {
                    //
                    // NOTE: Cannot handle the string to replace being
                    //       empty.
                    //
                    if (oldLength > 0)
                    {
                        //
                        // NOTE: Have we skipped over anything in the
                        //       original string since we last matched?
                        //       If so, we need to append it to the
                        //       result prior to doing anything else.
                        //
                        if (index2 != index)
                        {
                            //
                            // NOTE: Append the portion or the original
                            //       string between where we are now
                            //       and where our last match was to the
                            //       result.
                            //
                            builder.Append(text.Substring(
                                index2, index - index2));

                            //
                            // NOTE: Advance just beyond this match in
                            //       the original string.
                            //
                            index2 = (index + oldLength);
                        }
                        else
                        {
                            //
                            // NOTE: Advance just beyond this match in
                            //       the original string.
                            //
                            index2 += oldLength;
                        }

                        //
                        // NOTE: Make sure we have not met our quota for
                        //       replacements yet.
                        //
                        if ((maximum == Count.Invalid) ||
                            (count < maximum))
                        {
                            //
                            // NOTE: Append the 'new' (to) value from
                            //       the map for the 'old' (from) value
                            //       we just matched to the result.
                            //
                            builder.Append(newValue);

                            //
                            // NOTE: We just replaced another old value
                            //       with a new value.
                            //
                            count++;
                        }
                        else
                        {
                            //
                            // NOTE: Append the 'old' (from) value from
                            //       the map because we have already
                            //       reached our replacement quota.
                            //
                            builder.Append(oldValue);
                        }

                        //
                        // NOTE: Set the loop index to be one less than
                        //       our next index to attempt matching at
                        //       because the outer for loop will
                        //       increment it.  This prevents us from
                        //       having to use specialized logic in the
                        //       more general case of there being no
                        //       match while still ensuring that we do
                        //       eventually get to the end of the
                        //       original string.
                        //
                        index = (index2 - 1);
                    }
                }
            }

            //
            // NOTE: Append any final unmapped portion of the original
            //       string to the result.
            //
            if (index2 != index)
                builder.Append(text.Substring(index2, index - index2));

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reverses the order of the characters in the specified string.
        /// </summary>
        /// <param name="text">
        /// The string to be reversed.  May be null or empty.
        /// </param>
        /// <returns>
        /// The specified string with its characters in reverse order.
        /// </returns>
        public static string StrReverse(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                char[] chars = text.ToCharArray();
                Array.Reverse(chars);
                return new string(chars);
            }

            return text;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a string consisting of the specified character
        /// repeated enough times to pad the specified value out to the given total
        /// count of characters.
        /// </summary>
        /// <param name="value">
        /// The value whose length is subtracted from the requested count.  May be
        /// null.
        /// </param>
        /// <param name="count">
        /// The total number of characters; the length of the value is subtracted
        /// from this to determine how many characters to repeat.
        /// </param>
        /// <param name="character">
        /// The character to repeat.
        /// </param>
        /// <returns>
        /// A string composed of the repeated character.
        /// </returns>
        public static string StrRepeat(
            string value,
            int count,
            char character
            )
        {
            int length = (value != null) ?
                value.Length : 0;

            if (length > 0)
                count -= length;

            return StrRepeat(count, character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a string consisting of the specified character
        /// repeated the specified number of times.
        /// </summary>
        /// <param name="count">
        /// The number of times to repeat the character.  Values less than or equal
        /// to zero produce an empty string.
        /// </param>
        /// <param name="character">
        /// The character to repeat.
        /// </param>
        /// <returns>
        /// A string composed of the repeated character.
        /// </returns>
        public static string StrRepeat(
            int count,
            char character
            )
        {
            if (count <= 0)
                return String.Empty;

            StringBuilder result = SBF.Create();

            result.EnsureCapacity(count);
            result.Append(character, count);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a string consisting of the specified text repeated
        /// the specified number of times.
        /// </summary>
        /// <param name="count">
        /// The number of times to repeat the text.  Values less than or equal to
        /// zero produce an empty string.
        /// </param>
        /// <param name="text">
        /// The text to repeat.  May be null or empty.
        /// </param>
        /// <returns>
        /// A string composed of the repeated text.
        /// </returns>
        public static string StrRepeat(
            int count,
            string text
            )
        {
            if (count <= 0)
                return String.Empty;

            StringBuilder result = SBF.Create();

            if (!String.IsNullOrEmpty(text))
            {
                result.EnsureCapacity(count * text.Length);

                while (count-- > 0)
                    result.Append(text);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method centers the specified text within a field of the given length
        /// by padding both sides with the specified character.
        /// </summary>
        /// <param name="text">
        /// The text to be centered.  May be null or empty.
        /// </param>
        /// <param name="length">
        /// The total length of the resulting padded string.
        /// </param>
        /// <param name="character">
        /// The character used to pad either side of the text.
        /// </param>
        /// <returns>
        /// The text centered within a field of the specified length.
        /// </returns>
        public static string PadCenter(
            string text,
            int length,
            char character
            )
        {
            StringBuilder result = SBF.Create();

            if (!String.IsNullOrEmpty(text))
            {
                int textLength = text.Length;
                int halfLength = ((length - textLength) / 2);
                int extraLength = ((length - textLength) % 2);

                result.Append(StrRepeat(halfLength, character));
                result.Append(text);

                result.Append(StrRepeat(
                    halfLength + extraLength, character));
            }
            else
            {
                result.Append(StrRepeat(length, character));
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string comparer that corresponds to the specified
        /// string comparison semantics.
        /// </summary>
        /// <param name="comparisonType">
        /// The string comparison semantics for which a matching comparer is
        /// required.
        /// </param>
        /// <returns>
        /// The <see cref="StringComparer" /> corresponding to the specified
        /// comparison semantics, or the default comparer if it is not recognized.
        /// </returns>
        public static StringComparer GetStringComparer(
            StringComparison comparisonType
            )
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return StringComparer.CurrentCulture;
                case StringComparison.CurrentCultureIgnoreCase:
                    return StringComparer.CurrentCultureIgnoreCase;
                case StringComparison.InvariantCulture:
                    return StringComparer.InvariantCulture;
                case StringComparison.InvariantCultureIgnoreCase:
                    return StringComparer.InvariantCultureIgnoreCase;
                case StringComparison.Ordinal:
                    return StringComparer.Ordinal;
                case StringComparison.OrdinalIgnoreCase:
                    return StringComparer.OrdinalIgnoreCase;
                default:
                    return DefaultStringComparer;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally appends the string representation of the
        /// specified value to the specified string builder, optionally preceding it
        /// with a space and/or applying title-casing and a format string.
        /// </summary>
        /// <param name="builder">
        /// The string builder to append to.  If null, nothing is done.
        /// </param>
        /// <param name="format">
        /// An optional composite format string used to format the value.  May be
        /// null, in which case the value is appended verbatim.
        /// </param>
        /// <param name="value">
        /// The value to append.  If null or its string representation is empty,
        /// nothing is done.
        /// </param>
        /// <param name="withSpace">
        /// Non-zero to append a space before the value.
        /// </param>
        /// <param name="toTitle">
        /// Non-zero to convert the value to title case before appending it.
        /// </param>
        public static void MaybeAppend(
            StringBuilder builder, /* in */
            string format,         /* in: OPTIONAL */
            object value,          /* in */
            bool withSpace,        /* in */
            bool toTitle           /* in */
            )
        {
            if (builder == null)
                return;

            if (value == null)
                return;

            string valueString = value.ToString();

            if (String.IsNullOrEmpty(valueString))
                return;

            if (withSpace)
                builder.Append(Characters.Space);

            if (toTitle)
                valueString = ToTitle(valueString, null, null);

            if (format != null)
                builder.AppendFormat(format, valueString);
            else
                builder.Append(valueString);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally rewrites the specified case-changing sub-command
        /// name to its culture-invariant variant.
        /// </summary>
        /// <param name="subCommand">
        /// Upon input, the sub-command name to examine.  Upon success, this receives
        /// the name with the invariant suffix appended.
        /// </param>
        /// <param name="invariant">
        /// Non-zero to request the culture-invariant variant; zero to leave the name
        /// unchanged.  May be null to use the default behavior.
        /// </param>
        /// <returns>
        /// True if the sub-command name was rewritten to its invariant variant;
        /// otherwise, false.
        /// </returns>
        public static bool MaybeMutateCaseMethodName(
            ref string subCommand,
            bool? invariant
            )
        {
            if (String.IsNullOrEmpty(subCommand))
                return false;

            //
            // TODO: Good default?
            //
            if ((invariant != null) && !(bool)invariant)
                return false;

            if (!SharedStringOps.SystemNoCaseEquals(
                    subCommand, ToLowerMethodName) &&
                !SharedStringOps.SystemNoCaseEquals(
                    subCommand, ToTitleMethodName) &&
                !SharedStringOps.SystemNoCaseEquals(
                    subCommand, ToUpperMethodName))
            {
                return false;
            }

            subCommand += InvariantMethodSuffix;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the first character of the specified string to
        /// upper-case (i.e. "title" case) and the remaining characters to
        /// lower-case, using the indicated culture and case-folding semantics.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.  May be null or empty.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to perform the case conversion.  May be null to use
        /// the default culture-specific or invariant semantics.
        /// </param>
        /// <param name="invariant">
        /// Non-zero (or null) to use invariant case-folding when no culture is
        /// available; otherwise, the current culture is used.
        /// </param>
        /// <returns>
        /// The converted string, or the original string when it is null or
        /// empty.
        /// </returns>
        public static string ToTitle(
            string text,
            CultureInfo cultureInfo,
            bool? invariant
            )
        {
            string result = text;

            if (!String.IsNullOrEmpty(result))
            {
                char firstCharacter = result[0];

                string secondToEnd = (result.Length > 1) ?
                    result.Substring(1) : String.Empty;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                if (cultureInfo != null)
                {
                    result = Char.ToUpper(firstCharacter, cultureInfo) +
                        secondToEnd.ToLower(cultureInfo);
                }
                else
#endif
                {
                    //
                    // TODO: Good default?
                    //
                    if ((invariant == null) || (bool)invariant)
                    {
                        result = Char.ToUpperInvariant(firstCharacter) +
                            secondToEnd.ToLowerInvariant();
                    }
                    else
                    {
                        result = Char.ToUpper(firstCharacter) +
                            secondToEnd.ToLower();
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the first character of the specified string to
        /// lower-case, leaving the remaining characters unchanged, using the
        /// indicated culture and case-folding semantics.
        /// </summary>
        /// <param name="text">
        /// The string to be converted.  May be null or empty.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to perform the case conversion.  May be null to use
        /// the default culture-specific or invariant semantics.
        /// </param>
        /// <param name="invariant">
        /// Non-zero (or null) to use invariant case-folding when no culture is
        /// available; otherwise, the current culture is used.
        /// </param>
        /// <returns>
        /// The converted string, or the original string when it is null or
        /// empty.
        /// </returns>
        public static string ToLowerInitial(
            string text,
            CultureInfo cultureInfo,
            bool? invariant
            )
        {
            string result = text;

            if (!String.IsNullOrEmpty(result))
            {
                char firstCharacter = result[0];

                string secondToEnd = (result.Length > 1) ?
                    result.Substring(1) : String.Empty;

#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                if (cultureInfo != null)
                {
                    result = Char.ToLower(
                        firstCharacter, cultureInfo) + secondToEnd;
                }
                else
#endif
                {
                    //
                    // TODO: Good default?
                    //
                    if ((invariant == null) || (bool)invariant)
                    {
                        result = Char.ToLowerInvariant(firstCharacter) +
                            secondToEnd;
                    }
                    else
                    {
                        result = Char.ToLower(firstCharacter) +
                            secondToEnd;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Excludes characters covered by PathOps.HasPathWildcard().
        //
        /// <summary>
        /// This method determines whether the specified string contains any of
        /// the characters considered to be string-matching wildcard characters.
        /// </summary>
        /// <param name="value">
        /// The string to be checked.  May be null.
        /// </param>
        /// <returns>
        /// True if the string contains at least one string-matching wildcard
        /// character; otherwise, false.
        /// </returns>
        public static bool HasStringMatchWildcard(
            string value
            )
        {
            return (value != null) &&
                (StringMatchWildcardChars != null) &&
                (value.IndexOfAny(StringMatchWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string contains any of
        /// the characters reserved for use by the string-matching engine.
        /// </summary>
        /// <param name="text">
        /// The string to be checked.  May be null.
        /// </param>
        /// <returns>
        /// True if the string contains at least one reserved string-matching
        /// character; otherwise, false.
        /// </returns>
        public static bool HasStringMatchChar(
            string text
            )
        {
            return (text != null) &&
                (text.IndexOfAny(Characters.StringMatchReservedChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all white-space and control characters from the
        /// specified string.
        /// </summary>
        /// <param name="text">
        /// The string to be processed.  May be null or empty.
        /// </param>
        /// <returns>
        /// A copy of the string with all white-space and control characters
        /// removed, or the original string when it is null or empty.
        /// </returns>
        private static string RemoveWhiteSpace(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringBuilder result = SBF.Create(text.Length);

                for (int index = 0; index < text.Length; index++)
                {
                    switch (text[index])
                    {
                        case Characters.Null:
                        case Characters.Bell:
                        case Characters.Backspace:
                        case Characters.HorizontalTab:
                        case Characters.LineFeed:
                        case Characters.VerticalTab:
                        case Characters.FormFeed:
                        case Characters.CarriageReturn:
                        case Characters.Space:
                            {
                                //
                                // NOTE: Do nothing (i.e. skip this character).
                                //
                                break;
                            }
                        default:
                            {
                                result.Append(text[index]);
                                break;
                            }
                    }
                }

                return StringBuilderCache.GetStringAndRelease(ref result);
            }
            else
            {
                return text;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS && XML
        /// <summary>
        /// This method builds (or selects a cached) regular expression suitable
        /// for collapsing runs of white-space characters, honoring the specified
        /// flags that indicate which kinds of white-space should be preserved.
        /// </summary>
        /// <param name="textFlags">
        /// The flags used to control which categories of white-space characters
        /// are matched by the resulting regular expression.
        /// </param>
        /// <returns>
        /// The regular expression used to collapse white-space, or null when no
        /// white-space categories are eligible for collapsing.
        /// </returns>
        private static Regex GetRegExForCollapseWhiteSpace(
            TextFlags textFlags /* in */
            )
        {
            if (FlagOps.HasFlags(
                    textFlags, TextFlags.KeepNothing, true))
            {
                return TwoOrMoreWhiteSpaceRegEx;
            }
            else if (FlagOps.HasFlags(
                    textFlags, TextFlags.KeepNonSpaces, true))
            {
                return TwoOrMoreSpaceRegEx;
            }
            else
            {
                StringBuilder builder = SBF.Create();

                if (!FlagOps.HasFlags(
                        textFlags, TextFlags.KeepHorizontalTabs, true))
                {
                    builder.Append(Characters.Backslash_t);
                }

                if (!FlagOps.HasFlags(
                        textFlags, TextFlags.KeepLineFeeds, true))
                {
                    builder.Append(Characters.Backslash_n);
                }

                if (!FlagOps.HasFlags(
                        textFlags, TextFlags.KeepVerticalTabs, true))
                {
                    builder.Append(Characters.Backslash_v);
                }

                if (!FlagOps.HasFlags(
                        textFlags, TextFlags.KeepFormFeeds, true))
                {
                    builder.Append(Characters.Backslash_f);
                }

                if (!FlagOps.HasFlags(
                        textFlags, TextFlags.KeepCarriageReturns, true))
                {
                    builder.Append(Characters.Backslash_r);
                }

                if (!FlagOps.HasFlags(
                        textFlags, TextFlags.KeepSpaces, true))
                {
                    builder.Append(Characters.Space);
                }

                if (builder.Length > 0)
                {
                    builder.Insert(0, Characters.OpenBracket);
                    builder.Append(Characters.CloseBracket);
                    builder.Append(TwoOrMoreQuantifier);

                    return RegExOps.Create(
                        StringBuilderCache.GetStringAndRelease(
                        ref builder));
                }
                else
                {
                    /* IGNORED */
                    StringBuilderCache.Release(ref builder);

                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collapses runs of white-space characters within the
        /// specified string into single spaces, honoring the specified flags.
        /// </summary>
        /// <param name="text">
        /// The string to be processed.  May be null or empty.
        /// </param>
        /// <param name="textFlags">
        /// The flags used to control which categories of white-space characters
        /// are collapsed and whether escape sequences are processed.
        /// </param>
        /// <returns>
        /// The string with eligible white-space collapsed, or the original
        /// string when no collapsing is performed.
        /// </returns>
        public static string CollapseWhiteSpace(
            string text,
            TextFlags textFlags
            )
        {
            if (FlagOps.HasFlags(textFlags, TextFlags.NoCollapse, true))
                return text;

            Regex regEx = GetRegExForCollapseWhiteSpace(textFlags);

            if (String.IsNullOrEmpty(text) || (regEx == null))
                return text;

            string result = regEx.Replace(text, Characters.SpaceString);

            if (!String.IsNullOrEmpty(result) &&
                FlagOps.HasFlags(textFlags, TextFlags.AllowEscapes, true))
            {
                StringBuilder builder = SBF.Create(result);

                UnescapeWhiteSpace(builder);

                return StringBuilderCache.GetStringAndRelease(
                    ref builder);
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces literal white-space characters within the
        /// specified string with their backslash escape sequences.
        /// </summary>
        /// <param name="text">
        /// The string to be processed.  Upon return, contains the string with
        /// its white-space characters replaced by escape sequences.
        /// </param>
        private static void EscapeWhiteSpace(
            ref string text /* in, out */
            )
        {
            StringBuilder builder = SBF.Create(text);

            EscapeWhiteSpace(builder);

            text = StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces, in-place, literal white-space characters within
        /// the specified string builder with their backslash escape sequences.
        /// </summary>
        /// <param name="builder">
        /// The string builder to be modified in-place.  May be null, in which
        /// case this method does nothing.
        /// </param>
        private static void EscapeWhiteSpace(
            StringBuilder builder
            )
        {
            if (builder == null)
                return;

            builder.Replace(Characters.HorizontalTabString, Characters.Backslash_t_String);
            builder.Replace(Characters.LineFeedString, Characters.Backslash_n_String);
            builder.Replace(Characters.VerticalTabString, Characters.Backslash_v_String);
            builder.Replace(Characters.FormFeedString, Characters.Backslash_f_String);
            builder.Replace(Characters.CarriageReturnString, Characters.Backslash_r_String);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces backslash white-space escape sequences within
        /// the specified string with their literal white-space characters.
        /// </summary>
        /// <param name="text">
        /// The string to be processed.  Upon return, contains the string with
        /// its escape sequences replaced by literal white-space characters.
        /// </param>
        public static void UnescapeWhiteSpace(
            ref string text /* in, out */
            )
        {
            StringBuilder builder = SBF.Create(text);

            UnescapeWhiteSpace(builder);

            text = StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces, in-place, backslash white-space escape
        /// sequences within the specified string builder with their literal
        /// white-space characters.
        /// </summary>
        /// <param name="builder">
        /// The string builder to be modified in-place.  May be null, in which
        /// case this method does nothing.
        /// </param>
        private static void UnescapeWhiteSpace(
            StringBuilder builder
            )
        {
            if (builder == null)
                return;

            builder.Replace(Characters.Backslash_t_String, Characters.HorizontalTabString);
            builder.Replace(Characters.Backslash_n_String, Characters.LineFeedString);
            builder.Replace(Characters.Backslash_v_String, Characters.VerticalTabString);
            builder.Replace(Characters.Backslash_f_String, Characters.FormFeedString);
            builder.Replace(Characters.Backslash_r_String, Characters.CarriageReturnString);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string can be
        /// represented using a single byte per character when encoded with the
        /// specified encoding.
        /// </summary>
        /// <param name="encoding">
        /// The encoding used to evaluate the string.  May be null.
        /// </param>
        /// <param name="value">
        /// The string to be evaluated.  May be null or empty.
        /// </param>
        /// <param name="default">
        /// The value to return when the encoding is null or the string is null
        /// or empty.
        /// </param>
        /// <returns>
        /// True if the string is representable using a single byte per
        /// character; otherwise, false.
        /// </returns>
        public static bool IsSingleByte(
            Encoding encoding,
            string value,
            bool @default
            )
        {
            if (encoding == null)
                return @default;

            if (encoding.IsSingleByte)
                return true;

            int length;

            if (IsNullOrEmpty(value, out length))
                return @default;

            return length == encoding.GetByteCount(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current environment appears to be
        /// configured for a Unicode (UTF-8) locale, based on the relevant
        /// environment variables.
        /// </summary>
        /// <returns>
        /// True if the environment indicates a Unicode (UTF-8) locale;
        /// otherwise, false.
        /// </returns>
        public static bool IsUnicodeEncoding()
        {
            foreach (string name in new string[] { 
                    EnvVars.Language, /* POSIX (?) */
                    EnvVars.LocaleAll /* POSIX (?) */
                })
            {
                if (name == null)
                    continue;

                string value = CommonOps.Environment.GetVariable(name);

                if (value == null)
                    continue;

                if (value.IndexOf(EnvVars.Utf8Value,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Checks whether the specified encoding can represent
        //       Unicode characters for display purposes.  This is
        //       distinct from IsSingleByte, which checks encoding
        //       byte width, not display width.  In a modern terminal,
        //       box-drawing and other Unicode glyphs render as
        //       single-width characters regardless of their multi-byte
        //       representation in UTF-8.
        //
        /// <summary>
        /// This method determines whether the specified encoding represents a
        /// Unicode encoding (e.g. UTF-8, UTF-16, or UTF-32) for the purpose of
        /// displaying Unicode characters.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to be evaluated.  May be null.
        /// </param>
        /// <returns>
        /// True if the encoding represents a Unicode encoding; otherwise,
        /// false.
        /// </returns>
        public static bool IsUnicodeEncoding(
            Encoding encoding /* in */
            )
        {
            if (encoding == null)
                return false;

            //
            // NOTE: Check type first (i.e. the fast path
            //       for well-known encoding classes).
            //
            if ((encoding is UnicodeEncoding) ||
                (encoding is UTF8Encoding) ||
                (encoding is UTF32Encoding))
            {
                return true;
            }

            //
            // NOTE: Fall back to checking by code page or
            //       web name.  On .NET Core, the Console
            //       OutputEncoding property may return a
            //       wrapper type (e.g. OSEncoding) that
            //       is not one of the well-known classes
            //       but still represents UTF-8.
            //
            try
            {
                int codePage = encoding.CodePage; /* throw? */

                switch (codePage)
                {
                    case Utf8CodePage:
                    case Utf16LittleEndianCodePage:
                    case Utf16BigEndianCodePage:
                    case Utf32LittleEndianCodePage:
                    case Utf32BigEndianCodePage:
                        {
                            return true;
                        }
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: This function must not change the length of the string.
        //          Each character that gets replaced must be replaced by
        //          another character, not a string.
        //
        /// <summary>
        /// This method normalizes the white-space characters within the
        /// specified string, replacing each one in-place without changing the
        /// overall length of the string.
        /// </summary>
        /// <param name="text">
        /// The string to be normalized.  May be null or empty.
        /// </param>
        /// <param name="fallback">
        /// The character used to replace white-space characters that have no
        /// other suitable replacement.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how white-space characters are normalized.
        /// </param>
        /// <returns>
        /// The normalized string, or the original string when it is null or
        /// empty.
        /// </returns>
        public static string NormalizeWhiteSpace(
            string text,
            char fallback,
            WhiteSpaceFlags flags
            )
        {
            //
            // NOTE: If the original script string is null or empty, just
            //       return it verbatim.
            //
            if (String.IsNullOrEmpty(text))
                return text;

            //
            // NOTE: Create a string builder instance based on the script
            //       text.
            //
            StringBuilder builder = SBF.Create(text);

            //
            // NOTE: Using the created string builder, modify it in-place
            //       to normalize all line-endings to the convention that
            //       is required by the script evaluation engine.
            //
            FixupWhiteSpace(builder, fallback, flags);

            //
            // NOTE: Return the resulting string to the caller.
            //
            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes, in-place, the white-space characters within
        /// the specified string builder according to the specified flags.
        /// </summary>
        /// <param name="builder">
        /// The string builder to be modified in-place.  May be null, in which
        /// case this method does nothing.
        /// </param>
        /// <param name="fallback">
        /// The character used to replace white-space characters that have no
        /// other suitable replacement.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how white-space characters are normalized.
        /// </param>
        public static void FixupWhiteSpace(
            StringBuilder builder,
            char fallback,
            WhiteSpaceFlags flags
            )
        {
            if (builder == null)
                return;

            int length = builder.Length;

            if (length == 0)
                return;

            bool simple = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Simple, true);

            bool extended = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Extended, true);

            bool unicode = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Unicode, true);

            bool noArrows = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.NoArrows, true);

            bool @null = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Null, true);

            bool bell = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Bell, true);

            bool backspace = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Backspace, true);

            bool horizontalTab = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.HorizontalTab, true);

            bool lineFeed = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.LineFeed, true);

            bool verticalTab = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.VerticalTab, true);

            bool formFeed = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.FormFeed, true);

            bool carriageReturn = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.CarriageReturn, true);

            bool space = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Space, true);

            bool clean = FlagOps.HasFlags(
                flags, WhiteSpaceFlags.Clean, true);

            //
            // NOTE: Replace all tabs, carriage-returns, line-feeds, etc.
            //
            if (simple)
            {
                //
                // NOTE: In this mode all white-space characters, except
                //       the space character itself, are replaced by the
                //       fallback character (which is typically a space
                //       character).
                //
                for (int index = 0; index < length; index++)
                {
                    switch (builder[index])
                    {
                        case Characters.Null:           /* TERMINATOR */
                            {
                                if (!@null)
                                    continue;

                                break;
                            }
                        case Characters.Bell:           /* AUDIBLE */
                            {
                                if (!bell)
                                    continue;

                                break;
                            }
                        case Characters.Backspace:      /* HORIZONTAL */
                            {
                                if (!backspace)
                                    continue;

                                break;
                            }
                        case Characters.HorizontalTab:  /* HORIZONTAL */
                            {
                                if (!horizontalTab)
                                    continue;

                                break;
                            }
                        case Characters.LineFeed:       /* VERTICAL */
                            {
                                if (!lineFeed)
                                    continue;

                                break;
                            }
                        case Characters.VerticalTab:    /* VERTICAL */
                            {
                                if (!verticalTab)
                                    continue;

                                break;
                            }
                        case Characters.FormFeed:       /* VERTICAL */
                            {
                                if (!formFeed)
                                    continue;

                                break;
                            }
                        case Characters.CarriageReturn: /* VERTICAL */
                            {
                                if (!carriageReturn)
                                    continue;

                                break;
                            }
                        case Characters.Space:          /* HORIZONTAL */
                            {
                                if (!space)
                                    continue;

                                break;
                            }
                        case Characters.VisualNull:          /* <== U+0000 */
                        case Characters.VisualHorizontalTab: /* <== U+0009 */
                        case Characters.SectionSign:         /* <== U+000A */
                        case Characters.VisualVerticalTab:   /* <== U+000B */
                        case Characters.VisualFormFeed:      /* <== U+000C */
                        case Characters.PilcrowSign:         /* <== U+000D */
                        case Characters.VisualSpace:         /* <== U+0020 */
                            {
                                if (!clean || extended)
                                    continue;

                                break;
                            }
                        case Characters.BellSymbol:          /* <== U+0007 */
                        case Characters.BackspaceSymbol:     /* <== U+0008 */
                        case Characters.FullBlock:           /* <== U+0020 */
                            {
                                if (!clean || unicode)
                                    continue;

                                break;
                            }
                        case Characters.DownwardsArrow:      /* <== U+000A */
                        case Characters.LeftwardsArrow:      /* <== U+000D */
                            {
                                if (!clean || (unicode && !noArrows))
                                    continue;

                                break;
                            }
                        default:
                            {
                                continue;
                            }
                    }

                    builder[index] = fallback;
                }
            }
            else
            {
                for (int index = 0; index < length; index++)
                {
                    switch (builder[index])
                    {
                        case Characters.Null:           /* TERMINATOR */
                            {
                                if (!@null)
                                    continue;

                                if (unicode)
                                {
                                    //
                                    // TODO: This will likely not show up
                                    //       correctly in the console window;
                                    //       however, it's a bit better than
                                    //       nothing.
                                    //
                                    builder[index] = Characters.Angzarr;
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.VisualNull;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.Bell:           /* AUDIBLE */
                            {
                                if (!bell)
                                    continue;

                                if (unicode)
                                {
                                    //
                                    // TODO: This will likely not show up
                                    //       correctly in the console window;
                                    //       however, it's a bit better than
                                    //       emitting an audible bell in most
                                    //       circumstances.
                                    //
                                    builder[index] = Characters.BellSymbol;
                                }
                                else
                                {
                                    //
                                    // NOTE: There is no real suitable
                                    //       non-Unicode replacement for the
                                    //       bell character; therefore, use
                                    //       the fallback.
                                    //
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.Backspace:      /* HORIZONTAL */
                            {
                                if (!backspace)
                                    continue;

                                if (unicode)
                                {
                                    //
                                    // TODO: This will likely not show up
                                    //       correctly in the console window;
                                    //       however, it's a bit better than
                                    //       erasing the previous character
                                    //       in most circumstances.
                                    //
                                    builder[index] = Characters.BackspaceSymbol;
                                }
                                else
                                {
                                    //
                                    // NOTE: There is no real suitable
                                    //       non-Unicode replacement (i.e. a
                                    //       character that does not change
                                    //       how the console lays out text)
                                    //       for the backspace character;
                                    //       therefore, use the fallback.
                                    //
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.HorizontalTab:  /* HORIZONTAL */
                            {
                                if (!horizontalTab)
                                    continue;

                                if (extended)
                                {
                                    builder[index] = Characters.VisualHorizontalTab;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.LineFeed:       /* VERTICAL */
                            {
                                if (!lineFeed)
                                    continue;

                                if (unicode && !noArrows)
                                {
                                    //
                                    // NOTE: This means "advance to next
                                    //       line" in this context.
                                    //
                                    builder[index] = Characters.DownwardsArrow;
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.SectionSign;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.VerticalTab:    /* VERTICAL */
                            {
                                if (!verticalTab)
                                    continue;

                                if (extended)
                                {
                                    //
                                    // NOTE: This should be fine since its
                                    //       literal inclusion does not seem
                                    //       to change how the console lays
                                    //       out the text.
                                    //
                                    builder[index] = Characters.VisualVerticalTab;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.FormFeed:       /* VERTICAL */
                            {
                                if (!formFeed)
                                    continue;

                                if (extended)
                                {
                                    //
                                    // NOTE: This should be fine since its
                                    //       literal inclusion does not seem
                                    //       to change how the console lays
                                    //       out the text.
                                    //
                                    builder[index] = Characters.VisualFormFeed;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.CarriageReturn: /* VERTICAL */
                            {
                                if (!carriageReturn)
                                    continue;

                                if (unicode && !noArrows)
                                {
                                    //
                                    // NOTE: This means "reset to leftmost
                                    //       position" in this context.
                                    //
                                    builder[index] = Characters.LeftwardsArrow;
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.PilcrowSign;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.Space:          /* HORIZONTAL */
                            {
                                if (!space)
                                    continue;

                                if (unicode)
                                {
                                    //
                                    // NOTE: With Unicode, we can make the
                                    //       spaces more easily visible.
                                    //
                                    builder[index] = Characters.FullBlock;
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.VisualSpace;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.VisualNull:          /* <== U+0000 */
                            {
                                if (!clean)
                                    continue;

                                if (unicode)
                                {
                                    //
                                    // TODO: This will likely not show up
                                    //       correctly in the console window;
                                    //       however, it's a bit better than
                                    //       nothing.
                                    //
                                    builder[index] = Characters.Angzarr;
                                }
                                else if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.VisualHorizontalTab: /* <== U+0009 */
                            {
                                if (!clean)
                                    continue;

                                if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.SectionSign:         /* <== U+000A */
                            {
                                if (!clean)
                                    continue;

                                if (unicode && !noArrows)
                                {
                                    //
                                    // NOTE: This means "advance to next
                                    //       line" in this context.
                                    //
                                    builder[index] = Characters.DownwardsArrow;
                                }
                                else if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.VisualVerticalTab:   /* <== U+000B */
                            {
                                if (!clean)
                                    continue;

                                if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.VisualFormFeed:      /* <== U+000C */
                            {
                                if (!clean)
                                    continue;

                                if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.PilcrowSign:         /* <== U+000D */
                            {
                                if (!clean)
                                    continue;

                                if (unicode && !noArrows)
                                {
                                    //
                                    // NOTE: This means "reset to leftmost
                                    //       position" in this context.
                                    //
                                    builder[index] = Characters.LeftwardsArrow;
                                }
                                else if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.VisualSpace:         /* <== U+0020 */
                            {
                                if (!clean)
                                    continue;

                                if (unicode)
                                {
                                    //
                                    // NOTE: With Unicode, we can make the
                                    //       spaces more easily visible.
                                    //
                                    builder[index] = Characters.FullBlock;
                                }
                                else if (extended)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.BellSymbol:          /* <== U+0007 */
                            {
                                if (!clean)
                                    continue;

                                if (unicode)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.BackspaceSymbol:     /* <== U+0008 */
                            {
                                if (!clean)
                                    continue;

                                if (unicode)
                                {
                                    continue; /* NOP */
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.FullBlock:           /* <== U+0020 */
                            {
                                if (!clean)
                                    continue;

                                if (unicode)
                                {
                                    continue; /* NOP */
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.VisualSpace;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.DownwardsArrow:      /* <== U+000A */
                            {
                                if (!clean)
                                    continue;

                                if (unicode && !noArrows)
                                {
                                    continue; /* NOP */
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.SectionSign;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                        case Characters.LeftwardsArrow:      /* <== U+000D */
                            {
                                if (!clean)
                                    continue;

                                if (unicode && !noArrows)
                                {
                                    continue; /* NOP */
                                }
                                else if (extended)
                                {
                                    builder[index] = Characters.PilcrowSign;
                                }
                                else
                                {
                                    builder[index] = fallback;
                                }
                                break;
                            }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the character at the specified index within the
        /// specified string, or a null character when the string is null or the
        /// index is out of range.
        /// </summary>
        /// <param name="value">
        /// The string from which to obtain the character.  May be null.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the character to obtain.
        /// </param>
        /// <returns>
        /// The character at the specified index, or a null character when the
        /// string is null or the index is out of range.
        /// </returns>
        private static char CharOrNull(
            string value,
            int index
            )
        {
            if (value == null)
                return Characters.Null;

            if ((index >= 0) && (index < value.Length))
                return value[index];

            return Characters.Null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns null when the specified string is null or empty;
        /// otherwise, it returns the original string.
        /// </summary>
        /// <param name="value">
        /// The string to be checked.  May be null or empty.
        /// </param>
        /// <returns>
        /// Null when the string is null or empty; otherwise, the original
        /// string.
        /// </returns>
        public static string NullIfEmpty(
            string value
            )
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return null;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified left sub-command name
        /// is equal to, or a leading prefix of, the specified right sub-command
        /// name.
        /// </summary>
        /// <param name="left">
        /// The first sub-command name to compare.  May be null.
        /// </param>
        /// <param name="right">
        /// The second sub-command name to compare against.  May be null.
        /// </param>
        /// <returns>
        /// True if the sub-command names are considered equal; otherwise,
        /// false.
        /// </returns>
        public static bool SubCommandEquals(
            string left,
            string right
            )
        {
            if (left != null)
            {
                if (right != null)
                {
                    int leftLength = left.Length;
                    int rightLength = right.Length;

                    if (leftLength == 0)
                    {
                        return (rightLength == 0);
                    }
                    else
                    {
                        if (leftLength <= rightLength)
                        {
                            if (SharedStringOps.SystemEquals(
                                    left, 0, right, 0, leftLength))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return (right == null);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        /// <summary>
        /// This method determines whether the two specified strings are equal,
        /// treating two null strings as equal.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.  May be null.
        /// </param>
        /// <param name="right">
        /// The second string to compare.  May be null.
        /// </param>
        /// <returns>
        /// True if the strings are equal; otherwise, false.
        /// </returns>
        public static bool StringEquals(
            string left,
            string right
            )
        {
            if ((left == null) && (right == null))
                return true;

            if ((left == null) || (right == null))
                return false;

            return SharedStringOps.SystemEquals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the two specified values are equal,
        /// comparing them as strings when both are strings and as objects
        /// otherwise; two null values are considered equal.
        /// </summary>
        /// <param name="left">
        /// The first value to compare.  May be null.
        /// </param>
        /// <param name="right">
        /// The second value to compare.  May be null.
        /// </param>
        /// <returns>
        /// True if the values are equal; otherwise, false.
        /// </returns>
        public static bool StringOrObjectEquals(
            object left,
            object right
            )
        {
            if ((left == null) && (right == null))
                return true;

            if ((left == null) || (right == null))
                return false;

            if ((left is string) && (right is string))
                return SharedStringOps.SystemEquals((string)left, (string)right);

            if ((left is string) || (right is string))
                return false;

            return Object.Equals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates a hash code for the specified value, handling
        /// string values and dictionaries of objects specially.
        /// </summary>
        /// <param name="value">
        /// The value for which a hash code is calculated.  May be null.
        /// </param>
        /// <returns>
        /// The calculated hash code for the specified value, or zero when it is
        /// null.
        /// </returns>
        public static int StringOrObjectHashCode(
            object value
            )
        {
            if (value is string)
                return ((string)value).GetHashCode();

            ObjectDictionary dictionary = value as ObjectDictionary;

            if (dictionary != null)
            {
                int result = dictionary.Count;

                foreach (ObjectPair pair in dictionary)
                {
                    string localKey = pair.Key;

                    if (localKey != null)
                        result ^= localKey.GetHashCode();

                    object localValue = pair.Value;

                    if (localValue != null)
                        result ^= localValue.GetHashCode();
                }

                return result;
            }

            return (value != null) ? value.GetHashCode() : 0;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !MONO && NATIVE && WINDOWS
        /// <summary>
        /// This method overwrites the in-memory character buffer of the
        /// specified string with zeros, scrubbing its (potentially sensitive)
        /// contents in place.  The caller must own the string exclusively; it
        /// must not be interned or otherwise shared.
        /// </summary>
        /// <param name="value">
        /// The string whose backing storage is to be overwritten with zeros.
        /// </param>
        /// <param name="noComplain">
        /// Upon failure, this value is set to true when the caller should not
        /// report the associated error; otherwise, it is left unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// Upon success, it is left unchanged.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        [MethodImpl(
            MethodImplOptions.NoInlining
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
            | MethodImplOptions.NoOptimization
#endif
        )]
        private static ReturnCode ZeroString(
            string value,
            ref bool noComplain,
            ref Result error
            )
        {
            if (value == null)
            {
                noComplain = true;
                error = "invalid string";

                return ReturnCode.Error;
            }

            //
            // NOTE: The number of characters to scrub comes from the managed
            //       String.Length property.  This is the correct amount, and
            //       the reasoning is worth stating because this method depends
            //       on it:
            //
            //       A System.String stores exactly Length characters of content,
            //       immediately followed by a single U+0000 terminator (the CLR
            //       guarantees strings are both length-prefixed AND null
            //       terminated).  The allocated character buffer is therefore
            //       Length + 1 chars.  The caller's SECRET occupies precisely
            //       the first Length chars; the trailing terminator is always
            //       zero and contains no secret.  So zeroing Length chars erases
            //       all of the sensitive data.  (Length is NOT, in general,
            //       required to equal the raw buffer allocation -- it does not
            //       include the terminator, and on very old CLRs a redundant
            //       capacity field once existed -- but any bytes past index
            //       Length are never part of the string's value, so not
            //       scrubbing them does not leak the secret.  See "Q1" citations
            //       below).
            //
            //       This replaces a previous version-specific trick that read
            //       the length field at a NEGATIVE offset from the pinned first
            //       character; that offset differed between the CLR 2.x layout
            //       (m_arrayLength then m_stringLength) and the CLR 4.x layout
            //       (m_stringLength only) and was wrong on .NET Core / .NET 5+.
            //       String.Length needs no layout assumptions and is correct
            //       everywhere; verified to wipe content on .NET Framework,
            //       the .NET runtime, and Mono.
            //
            //       Field layout / "zero terminated" / buffer == Length + 1:
            //       .NET (modern), String.cs ("_stringLength", "_firstChar",
            //       "strings are both null-terminated and length-prefixed"):
            //
            //       https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/String.cs
            //
            //       .NET Framework reference source, string.cs ("m_stringLength",
            //       "m_firstChar", "map directly onto ... EE StringObject",
            //       "always zero terminated"):
            //
            //       https://github.com/microsoft/referencesource/blob/main/mscorlib/system/string.cs
            //
            //       Mono uses that same reference source for its corlib String:
            //
            //       https://github.com/mono/mono/blob/main/mcs/class/referencesource/mscorlib/system/string.cs
            //
            // WARNING: This mutates the storage of a System.String in place.  Do
            //          NOT pass an interned or otherwise shared string (e.g. a
            //          string literal); doing so would corrupt every other
            //          reference to the same instance.  The caller must own the
            //          string exclusively.
            //
            int length = value.Length;

            if (length <= 0)
                return ReturnCode.Ok;

            GCHandle handle = NativeOps.GetInvalidGCHandle();

            try
            {
                handle = GCHandle.Alloc(value, GCHandleType.Pinned);

                if (handle.IsAllocated)
                {
                    //
                    // NOTE: For a PINNED String, GCHandle.AddrOfPinnedObject()
                    //       returns the address of the first character (m_firstChar
                    //       / _firstChar), i.e. the start of the contiguous char
                    //       buffer -- NOT the object header.  This is a documented,
                    //       stable contract on every supported runtime (it is how
                    //       the previous code located the buffer too, and it has
                    //       been empirically confirmed here on the .NET runtime and
                    //       Mono, where it equals the JIT "fixed (char* = str)"
                    //       pointer).  Because the buffer is contiguous chars,
                    //       zeroing length * sizeof(char) bytes from this pointer
                    //       clears exactly the string content.
                    //
                    //       AddrOfPinnedObject special-cases String (returns
                    //       GetRawStringData(), the first char) and arrays:
                    //
                    //       https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/GCHandle.cs
                    //
                    //       This also holds on LEGACY Mono (its own hand-written
                    //       corlib, before it adopted the MS reference source).
                    //       Verified at the mono-2.0 tag: MonoString is
                    //       { MonoObject object; gint32 length; gunichar2 chars[]; }
                    //       and the GCHandle GetAddrOfPinnedObject icall returns
                    //       mono_string_chars((MonoString*)obj) == &chars[0] for
                    //       strings (mono_array_addr for arrays).  String.Length
                    //       returns that same "length" field, so both assumptions
                    //       hold back to (at least) Mono 2.0:
                    //
                    //       https://github.com/mono/mono/blob/mono-2.0/mono/metadata/gc.c (GetAddrOfPinnedObject)
                    //       https://github.com/mono/mono/blob/mono-2.0/mono/metadata/object.h (MonoString, mono_string_chars)
                    //
                    IntPtr pMemory = handle.AddrOfPinnedObject(); /* m_firstChar */

                    if (pMemory != IntPtr.Zero)
                    {
                        return NativeOps.ZeroMemory(
                            pMemory, (uint)(length * sizeof(char)),
                            ref error);
                    }
                    else
                    {
                        error = "could not get address of pinned string";
                    }
                }
                else
                {
                    error = "could not allocate pinned string";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method overwrites the in-memory character buffer of the
        /// specified string with zeros, emitting a diagnostic trace message
        /// when the operation fails.
        /// </summary>
        /// <param name="value">
        /// The string whose backing storage is to be overwritten with zeros.
        /// </param>
        /// <returns>
        /// True if the string was successfully zeroed; otherwise, false.
        /// </returns>
        public static bool ZeroStringOrTrace(
            string value
            )
        {
            ReturnCode code;
            bool noComplain = false;
            Result error = null;

            code = ZeroString(value, ref noComplain, ref error);

            if (!noComplain && (code != ReturnCode.Ok))
            {
                TraceOps.DebugTrace(String.Format(
                    "ZeroString: error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(StringOps).Name,
                    TracePriority.CleanupError);
            }

            return (code == ReturnCode.Ok);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region The [scan] Engine
        /// <summary>
        /// This method implements the core engine for the [scan] command,
        /// extracting values from the input string according to the specified
        /// format string.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="input">
        /// The input string to be scanned.
        /// </param>
        /// <param name="format">
        /// The format string that controls how the input is scanned.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments containing the variable names to be assigned.
        /// </param>
        /// <param name="firstVarIndex">
        /// The index, into the argument list, of the first variable name.
        /// </param>
        /// <param name="inline">
        /// Non-zero to return the scanned values as a list; otherwise, the
        /// scanned values are stored into the named variables.
        /// </param>
        /// <param name="result">
        /// Upon success, this parameter receives the scanned value list or the
        /// number of conversions performed.  Upon failure, it receives an error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode DoScan(
            Interpreter interpreter, /* in */
            string input,            /* in */
            string format,           /* in */
            ArgumentList arguments,  /* in */
            int firstVarIndex,       /* in */
            bool inline,             /* in */
            ref Result result        /* out */
            )
        {
            CultureInfo cultureInfo = interpreter.InternalCultureInfo;

            int inputLength = (input != null) ? input.Length : 0;
            int formatLength = (format != null) ? format.Length : 0;

            int inputIndex = 0;
            int formatIndex = 0;

            int nextVarIndex = firstVarIndex;
            int conversions = 0;
            int assignments = 0;

            bool underflow = false;
            bool sawXpg = false;

            //
            // NOTE: Accumulates the scanned values for the "inline" form (and,
            //       for positional [XPG] specifiers, indexed by position).
            //
            StringList values = new StringList();

            //
            // NOTE: In the variable form (no positional specifiers), the number
            //       of supplied variable names must match the number of
            //       non-suppressed conversion specifiers (this is a static
            //       property of the format, checked up front like Tcl does).
            //
            if (!inline)
            {
                bool formatHasXpg;

                int specifierCount = CountSpecifiers(format, out formatHasXpg);

                if (!formatHasXpg)
                {
                    int variableCount = arguments.Count - firstVarIndex;

                    if (variableCount > specifierCount)
                    {
                        result = "variable is not assigned by any conversion specifiers";
                        return ReturnCode.Error;
                    }
                    else if (specifierCount > variableCount)
                    {
                        result = "different numbers of variable names and field specifiers";
                        return ReturnCode.Error;
                    }
                }
            }

            while (formatIndex < formatLength)
            {
                char formatChar = format[formatIndex];

                //
                // NOTE: A whitespace run in the format matches any (possibly
                //       empty) whitespace run in the input.
                //
                if (Char.IsWhiteSpace(formatChar))
                {
                    formatIndex++;

                    while ((inputIndex < inputLength) &&
                            Char.IsWhiteSpace(input[inputIndex]))
                    {
                        inputIndex++;
                    }

                    continue;
                }

                //
                // NOTE: A non-conversion (literal) character must match the
                //       input exactly; otherwise scanning stops.
                //
                if (formatChar != Characters.PercentSign)
                {
                    if ((inputIndex < inputLength) &&
                            (input[inputIndex] == formatChar))
                    {
                        inputIndex++;
                        formatIndex++;
                        continue;
                    }

                    break;
                }

                //
                // NOTE: A conversion specifier; consume the percent sign.
                //
                formatIndex++;

                if (formatIndex >= formatLength)
                {
                    result = "bad scan format: trailing \"%\"";
                    return ReturnCode.Error;
                }

                formatChar = format[formatIndex];

                //
                // NOTE: A literal "%%" matches a single percent sign.
                //
                if (formatChar == Characters.PercentSign)
                {
                    if ((inputIndex < inputLength) &&
                            (input[inputIndex] == Characters.PercentSign))
                    {
                        inputIndex++;
                        formatIndex++;
                        continue;
                    }

                    break;
                }

                //
                // NOTE: Optional XPG positional specifier (e.g. "%2$d"); the
                //       leading digits are a position only when followed by a
                //       dollar sign, otherwise they are the field width below.
                //
                int position = Index.Invalid;

                {
                    int savedIndex = formatIndex;
                    int number = 0;
                    bool haveNumber = false;

                    while ((formatIndex < formatLength) &&
                            Char.IsDigit(format[formatIndex]))
                    {
                        number = (number * Parser.DecimalRadix) +
                            (format[formatIndex] - Characters.Zero);

                        haveNumber = true;
                        formatIndex++;
                    }

                    if (haveNumber && (formatIndex < formatLength) &&
                            (format[formatIndex] == Characters.DollarSign))
                    {
                        position = number;
                        formatIndex++;
                        sawXpg = true;
                    }
                    else
                    {
                        formatIndex = savedIndex;
                    }
                }

                //
                // NOTE: Optional assignment-suppression flag.
                //
                bool suppress = false;

                if ((formatIndex < formatLength) &&
                        (format[formatIndex] == Characters.Asterisk))
                {
                    suppress = true;
                    formatIndex++;
                }

                //
                // NOTE: Optional maximum field width.
                //
                int width = Width.Invalid;

                {
                    int number = 0;
                    bool haveNumber = false;

                    while ((formatIndex < formatLength) &&
                            Char.IsDigit(format[formatIndex]))
                    {
                        number = (number * Parser.DecimalRadix) +
                            (format[formatIndex] - Characters.Zero);

                        haveNumber = true;
                        formatIndex++;
                    }

                    if (haveNumber)
                        width = number;
                }

                //
                // NOTE: Optional (ignored) size modifiers, for Tcl source
                //       compatibility.
                //
                while ((formatIndex < formatLength) &&
                        ((format[formatIndex] == Characters.l) ||
                         (format[formatIndex] == Characters.h) ||
                         (format[formatIndex] == Characters.L)))
                {
                    formatIndex++;
                }

                if (formatIndex >= formatLength)
                {
                    result = "bad scan format: missing conversion character";
                    return ReturnCode.Error;
                }

                char conversion = format[formatIndex];
                formatIndex++;

                //
                // NOTE: The character set conversion carries its own spec; it
                //       is gathered here so the engine can match against it.
                //
                string charSet = null;

                if (conversion == Characters.OpenBracket)
                {
                    int setEnd = ParseCharSet(format, formatIndex);

                    if (setEnd == Index.Invalid)
                    {
                        result = "bad scan format: unmatched \"[\" in format string";
                        return ReturnCode.Error;
                    }

                    charSet = format.Substring(
                        formatIndex, setEnd - formatIndex);

                    formatIndex = setEnd + 1; /* skip the closing "]". */
                }

                //
                // NOTE: Conversions other than character, character-set, and
                //       count skip leading whitespace in the input.
                //
                if ((conversion != Characters.c) &&
                    (conversion != Characters.OpenBracket) &&
                    (conversion != Characters.n))
                {
                    while ((inputIndex < inputLength) &&
                            Char.IsWhiteSpace(input[inputIndex]))
                    {
                        inputIndex++;
                    }
                }

                //
                // NOTE: The character-count conversion neither consumes input
                //       nor counts as a conversion; it just reports progress.
                //
                if (conversion == Characters.n)
                {
                    if (!suppress)
                    {
                        string countValue = inputIndex.ToString(
                            CultureInfo.InvariantCulture);

                        if (!StoreValue(
                                interpreter, arguments, firstVarIndex,
                                ref nextVarIndex, inline, sawXpg, position,
                                countValue, values, ref assignments,
                                ref result))
                        {
                            return ReturnCode.Error;
                        }
                    }

                    continue;
                }

                //
                // NOTE: End-of-input reached before this conversion could match
                //       anything; remember it for the return-value rule below.
                //
                if (inputIndex >= inputLength)
                {
                    underflow = true;
                    break;
                }

                string scanned = null;
                bool matched = false;

                switch (conversion)
                {
                    case Characters.d:
                    case Characters.i:
                    case Characters.o:
                    case Characters.x:
                    case Characters.X:
                    case Characters.u:
                    case Characters.b:
                        {
                            matched = ScanInteger(
                                input, ref inputIndex, width, conversion,
                                cultureInfo, out scanned);

                            break;
                        }
                    case Characters.c:
                        {
                            //
                            // NOTE: A single character; the result is its
                            //       integer code (no leading whitespace skip).
                            //
                            int code = input[inputIndex];
                            inputIndex++;

                            scanned = code.ToString(
                                CultureInfo.InvariantCulture);

                            matched = true;
                            break;
                        }
                    case Characters.e:
                    case Characters.E:
                    case Characters.f:
                    case Characters.g:
                    case Characters.G:
                        {
                            matched = ScanReal(
                                input, ref inputIndex, width, cultureInfo,
                                out scanned);

                            break;
                        }
                    case Characters.s:
                        {
                            matched = ScanString(
                                input, ref inputIndex, width, out scanned);

                            break;
                        }
                    default:
                        {
                            if (charSet != null)
                            {
                                matched = ScanCharSet(
                                    input, ref inputIndex, width, charSet,
                                    out scanned);
                            }
                            else
                            {
                                result = String.Format(
                                    "bad scan conversion character \"%{0}\"",
                                    conversion);

                                return ReturnCode.Error;
                            }

                            break;
                        }
                }

                //
                // NOTE: A failed conversion stops scanning (matching Tcl); the
                //       return value reflects what was assigned so far.
                //
                if (!matched)
                    break;

                conversions++;

                if (!suppress)
                {
                    if (!StoreValue(
                            interpreter, arguments, firstVarIndex,
                            ref nextVarIndex, inline, sawXpg, position,
                            scanned, values, ref assignments, ref result))
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            //
            // NOTE: Build the return value.  The inline form yields the list of
            //       scanned values; the variable form yields the number of
            //       successful (non-suppressed) conversions, or -1 if the input
            //       was exhausted before any conversion matched.
            //
            if (inline)
            {
                result = values;
            }
            else if (underflow && (conversions == 0))
            {
                result = Index.Invalid;
            }
            else
            {
                result = assignments;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region [scan] Conversion Helpers
        /// <summary>
        /// This method stores a single scanned value, either by appending it to
        /// the inline result list or by assigning it to the appropriate
        /// variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments containing the variable names to be assigned.
        /// </param>
        /// <param name="firstVarIndex">
        /// The index, into the argument list, of the first variable name.
        /// </param>
        /// <param name="nextVarIndex">
        /// Upon success, this value is advanced to the index of the next
        /// variable name to be assigned.  Upon failure, it is left unchanged.
        /// </param>
        /// <param name="inline">
        /// Non-zero to append the value to the result list; otherwise, the
        /// value is assigned to a variable.
        /// </param>
        /// <param name="sawXpg">
        /// Non-zero if a positional [XPG] specifier was seen.
        /// </param>
        /// <param name="position">
        /// The one-based position for a positional specifier, or a value less
        /// than one when there is no associated position.
        /// </param>
        /// <param name="value">
        /// The scanned value to be stored.
        /// </param>
        /// <param name="values">
        /// The list used to accumulate values for the inline form.
        /// </param>
        /// <param name="assignments">
        /// Upon success, this value is incremented to reflect the stored value.
        /// Upon failure, it is left unchanged.
        /// </param>
        /// <param name="result">
        /// Upon failure, this parameter receives an error message.  Upon
        /// success, it is left unchanged.
        /// </param>
        /// <returns>
        /// True if the value was successfully stored; otherwise, false.
        /// </returns>
        private static bool StoreValue(
            Interpreter interpreter, /* in */
            ArgumentList arguments,  /* in */
            int firstVarIndex,       /* in */
            ref int nextVarIndex,    /* in, out */
            bool inline,             /* in */
            bool sawXpg,             /* in */
            int position,            /* in */
            string value,            /* in */
            StringList values,       /* in, out */
            ref int assignments,     /* in, out */
            ref Result result        /* out */
            )
        {
            if (inline)
            {
                //
                // NOTE: For positional specifiers the value lands at its slot
                //       (one-based), padding any skipped slots with the empty
                //       string; otherwise it is simply appended.
                //
                if (sawXpg && (position > 0))
                {
                    while (values.Count < position)
                        values.Add(String.Empty);

                    values[position - 1] = value;
                }
                else
                {
                    values.Add(value);
                }

                assignments++;
                return true;
            }

            //
            // NOTE: Variable form; positional specifiers select the n-th
            //       variable name argument, otherwise the next one in order.
            //
            int varIndex;

            if (sawXpg && (position > 0))
                varIndex = (firstVarIndex + position) - 1;
            else
                varIndex = nextVarIndex++;

            if (varIndex >= arguments.Count)
            {
                result = "different numbers of variable names and field specifiers";
                return false;
            }

            Result error = null;

            if (interpreter.SetVariableValue(VariableFlags.None,
                    arguments[varIndex], value, null,
                    ref error) != ReturnCode.Ok)
            {
                result = error;
                return false;
            }

            assignments++;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans an integer value from the input string, starting
        /// at the specified index, using the radix implied by the conversion
        /// character.
        /// </summary>
        /// <param name="input">
        /// The input string being scanned.
        /// </param>
        /// <param name="inputIndex">
        /// Upon success, this value is advanced past the scanned integer.  Upon
        /// failure, it is left unchanged.
        /// </param>
        /// <param name="width">
        /// The maximum field width, or an invalid width when there is no limit.
        /// </param>
        /// <param name="conversion">
        /// The conversion character that selects the radix and signedness.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the integer value.
        /// </param>
        /// <param name="scanned">
        /// Upon success, this parameter receives the scanned value formatted as
        /// a string.  Upon failure, it is set to null.
        /// </param>
        /// <returns>
        /// True if an integer value was successfully scanned; otherwise, false.
        /// </returns>
        private static bool ScanInteger(
            string input,           /* in */
            ref int inputIndex,     /* in, out */
            int width,              /* in */
            char conversion,        /* in */
            CultureInfo cultureInfo, /* in */
            out string scanned      /* out */
            )
        {
            scanned = null;

            int length = input.Length;
            int start = inputIndex;
            int limit = (width != Width.Invalid) ? (start + width) : length;

            if (limit > length)
                limit = length;

            int index = start;

            //
            // NOTE: Optional sign (handled here so the magnitude can be parsed
            //       with an explicit radix below).
            //
            bool negative = false;

            if ((index < limit) &&
                ((input[index] == Characters.PlusSign) ||
                 (input[index] == Characters.MinusSign)))
            {
                negative = (input[index] == Characters.MinusSign);
                index++;
            }

            //
            // NOTE: Determine the radix from the conversion character; "%i"
            //       autodetects it from any "0x"/leading-"0" prefix.
            //
            int radix;

            if ((conversion == Characters.o))
                radix = Parser.OctalRadix;
            else if ((conversion == Characters.x) || (conversion == Characters.X))
                radix = Parser.HexadecimalRadix;
            else if ((conversion == Characters.b))
                radix = Parser.BinaryRadix;
            else
                radix = Parser.DecimalRadix;

            //
            // NOTE: Consume an optional base prefix where it is meaningful, so
            //       the parsed magnitude is just the bare digits.
            //
            if ((conversion == Characters.x) || (conversion == Characters.X) ||
                (conversion == Characters.i))
            {
                if (((index + 1) < limit) && (input[index] == Characters.Zero) &&
                        ((input[index + 1] == Characters.x) ||
                         (input[index + 1] == Characters.X)))
                {
                    radix = Parser.HexadecimalRadix;
                    index += 2;
                }
                else if ((conversion == Characters.i) && (index < limit) &&
                        (input[index] == Characters.Zero))
                {
                    radix = Parser.OctalRadix;
                }
            }

            int digitsStart = index;

            while ((index < limit) && IsRadixDigit(input[index], radix))
                index++;

            if (index == digitsStart)
                return false;

            //
            // NOTE: Re-attach the canonical radix prefix (the input may or may
            //       not have had one) so the value parser autodetects the radix.
            //
            string token = RadixPrefix(radix) +
                input.Substring(digitsStart, index - digitsStart);

            Result error = null;

            if (conversion == Characters.u)
            {
                ulong unsignedValue = 0;

                if (Value.GetUnsignedWideInteger2(
                        token, ValueFlags.AnyWideInteger, cultureInfo,
                        ref unsignedValue, ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                inputIndex = index;

                scanned = unsignedValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            else
            {
                long value = 0;

                if (Value.GetWideInteger2(
                        token, ValueFlags.AnyWideInteger, cultureInfo,
                        ref value, ref error) != ReturnCode.Ok)
                {
                    return false;
                }

                if (negative)
                    value = -value;

                inputIndex = index;

                scanned = value.ToString(CultureInfo.InvariantCulture);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans a floating-point value from the input string,
        /// starting at the specified index.
        /// </summary>
        /// <param name="input">
        /// The input string being scanned.
        /// </param>
        /// <param name="inputIndex">
        /// Upon success, this value is advanced past the scanned value.  Upon
        /// failure, it is left unchanged.
        /// </param>
        /// <param name="width">
        /// The maximum field width, or an invalid width when there is no limit.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the floating-point value.
        /// </param>
        /// <param name="scanned">
        /// Upon success, this parameter receives the scanned value formatted as
        /// a string.  Upon failure, it is set to null.
        /// </param>
        /// <returns>
        /// True if a floating-point value was successfully scanned; otherwise,
        /// false.
        /// </returns>
        private static bool ScanReal(
            string input,           /* in */
            ref int inputIndex,     /* in, out */
            int width,              /* in */
            CultureInfo cultureInfo, /* in */
            out string scanned      /* out */
            )
        {
            scanned = null;

            int length = input.Length;
            int start = inputIndex;
            int limit = (width != Width.Invalid) ? (start + width) : length;

            if (limit > length)
                limit = length;

            int index = start;

            if ((index < limit) &&
                ((input[index] == Characters.PlusSign) ||
                 (input[index] == Characters.MinusSign)))
            {
                index++;
            }

            bool haveDigit = false;

            while ((index < limit) && Char.IsDigit(input[index]))
            {
                haveDigit = true;
                index++;
            }

            if ((index < limit) && (input[index] == Characters.Period))
            {
                index++;

                while ((index < limit) && Char.IsDigit(input[index]))
                {
                    haveDigit = true;
                    index++;
                }
            }

            if (!haveDigit)
                return false;

            //
            // NOTE: Optional decimal exponent.
            //
            if ((index < limit) &&
                ((input[index] == Characters.e) || (input[index] == Characters.E)))
            {
                int exponentIndex = index + 1;

                if ((exponentIndex < limit) &&
                    ((input[exponentIndex] == Characters.PlusSign) ||
                     (input[exponentIndex] == Characters.MinusSign)))
                {
                    exponentIndex++;
                }

                if ((exponentIndex < limit) &&
                        Char.IsDigit(input[exponentIndex]))
                {
                    index = exponentIndex;

                    while ((index < limit) && Char.IsDigit(input[index]))
                        index++;
                }
            }

            string text = input.Substring(start, index - start);
            double doubleValue = 0.0;

            Result error = null;

            if (Value.GetDouble(
                    text, cultureInfo, ref doubleValue,
                    ref error) != ReturnCode.Ok)
            {
                return false;
            }

            inputIndex = index;

            //
            // NOTE: Render the value using Eagle's canonical double form so the
            //       result is self-consistent with [expr].
            //
            Result temporary = doubleValue;
            scanned = temporary.ToString();
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans a run of non-whitespace characters from the input
        /// string, starting at the specified index.
        /// </summary>
        /// <param name="input">
        /// The input string being scanned.
        /// </param>
        /// <param name="inputIndex">
        /// Upon success, this value is advanced past the scanned characters.
        /// Upon failure, it is left unchanged.
        /// </param>
        /// <param name="width">
        /// The maximum field width, or an invalid width when there is no limit.
        /// </param>
        /// <param name="scanned">
        /// Upon success, this parameter receives the scanned characters.  Upon
        /// failure, it is set to null.
        /// </param>
        /// <returns>
        /// True if at least one character was successfully scanned; otherwise,
        /// false.
        /// </returns>
        private static bool ScanString(
            string input,        /* in */
            ref int inputIndex,  /* in, out */
            int width,           /* in */
            out string scanned   /* out */
            )
        {
            scanned = null;

            int length = input.Length;
            int start = inputIndex;
            int limit = (width != Width.Invalid) ? (start + width) : length;

            if (limit > length)
                limit = length;

            int index = start;

            while ((index < limit) && !Char.IsWhiteSpace(input[index]))
                index++;

            if (index == start)
                return false;

            inputIndex = index;

            scanned = input.Substring(start, index - start);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans a run of characters that belong to the specified
        /// character set from the input string, starting at the specified
        /// index.
        /// </summary>
        /// <param name="input">
        /// The input string being scanned.
        /// </param>
        /// <param name="inputIndex">
        /// Upon success, this value is advanced past the scanned characters.
        /// Upon failure, it is left unchanged.
        /// </param>
        /// <param name="width">
        /// The maximum field width, or an invalid width when there is no limit.
        /// </param>
        /// <param name="charSet">
        /// The character set specification that determines which characters are
        /// matched.
        /// </param>
        /// <param name="scanned">
        /// Upon success, this parameter receives the scanned characters.  Upon
        /// failure, it is set to null.
        /// </param>
        /// <returns>
        /// True if at least one character was successfully scanned; otherwise,
        /// false.
        /// </returns>
        private static bool ScanCharSet(
            string input,        /* in */
            ref int inputIndex,  /* in, out */
            int width,           /* in */
            string charSet,      /* in */
            out string scanned   /* out */
            )
        {
            scanned = null;

            int length = input.Length;
            int start = inputIndex;
            int limit = (width != Width.Invalid) ? (start + width) : length;

            if (limit > length)
                limit = length;

            int index = start;

            while ((index < limit) &&
                    CharSetContains(charSet, input[index]))
            {
                index++;
            }

            if (index == start)
                return false;

            inputIndex = index;

            scanned = input.Substring(start, index - start);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates the end of a character set specification within
        /// the specified format string, accounting for the special handling of
        /// a leading negation or close bracket.
        /// </summary>
        /// <param name="format">
        /// The format string containing the character set specification.
        /// </param>
        /// <param name="formatIndex">
        /// The index, into the format string, of the first character of the
        /// character set specification.
        /// </param>
        /// <returns>
        /// The index of the closing bracket that terminates the character set,
        /// or an invalid index when the set is unterminated.
        /// </returns>
        private static int ParseCharSet(
            string format,   /* in */
            int formatIndex  /* in */
            )
        {
            int length = format.Length;
            int index = formatIndex;

            //
            // NOTE: A leading "^" negates the set and is not a member.
            //
            if ((index < length) && (format[index] == Characters.CircumflexAccent))
                index++;

            //
            // NOTE: A "]" in the first position (after any "^") is a literal
            //       member, not the terminator.
            //
            if ((index < length) && (format[index] == Characters.CloseBracket))
                index++;

            while (index < length)
            {
                if (format[index] == Characters.CloseBracket)
                    return index;

                index++;
            }

            return Index.Invalid; /* unterminated set. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is matched by
        /// the specified character set specification, honoring negation and
        /// character ranges.
        /// </summary>
        /// <param name="charSet">
        /// The character set specification to test against.
        /// </param>
        /// <param name="character">
        /// The character to test for membership.
        /// </param>
        /// <returns>
        /// True if the character is matched by the set; otherwise, false.
        /// </returns>
        private static bool CharSetContains(
            string charSet, /* in */
            char character  /* in */
            )
        {
            int length = charSet.Length;
            int index = 0;

            bool negated = false;

            if ((index < length) && (charSet[index] == Characters.CircumflexAccent))
            {
                negated = true;
                index++;
            }

            bool found = false;

            while (index < length)
            {
                char setChar = charSet[index];

                //
                // NOTE: A range "a-z" applies when the hyphen is between two
                //       characters; a trailing/leading hyphen is literal.
                //
                if ((setChar == Characters.MinusSign) &&
                    (index > 0) && ((index + 1) < length))
                {
                    char low = charSet[index - 1];
                    char high = charSet[index + 1];

                    if ((character > low) && (character <= high))
                        found = true;

                    index += 2;
                    continue;
                }

                if (character == setChar)
                    found = true;

                index++;
            }

            return negated ? !found : found;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the textual prefix associated with the specified
        /// numeric radix (e.g. "0x" for hexadecimal, "0o" for octal, or "0b"
        /// for binary).
        /// </summary>
        /// <param name="radix">
        /// The numeric radix for which the textual prefix is returned.
        /// </param>
        /// <returns>
        /// The textual prefix associated with the specified radix, or an empty
        /// string when the radix has no associated prefix.
        /// </returns>
        private static string RadixPrefix(
            int radix /* in */
            )
        {
            if (radix == Parser.HexadecimalRadix)
                return Characters.Zero.ToString() + Characters.x;
            else if (radix == Parser.OctalRadix)
                return Characters.Zero.ToString() + Characters.o;
            else if (radix == Parser.BinaryRadix)
                return Characters.Zero.ToString() + Characters.b;
            else
                return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the number of conversion specifiers present in the
        /// specified scan format string, skipping literal "%%" sequences,
        /// assignment-suppressed specifiers, field widths, size modifiers, and
        /// character-set specifications.
        /// </summary>
        /// <param name="format">
        /// The scan format string whose conversion specifiers are counted.
        /// </param>
        /// <param name="sawXpg">
        /// Upon return, this parameter is set to non-zero if at least one XPG
        /// positional specifier was encountered; otherwise, it is set to zero.
        /// </param>
        /// <returns>
        /// The number of variable-consuming conversion specifiers found in the
        /// specified format string.
        /// </returns>
        private static int CountSpecifiers(
            string format,    /* in */
            out bool sawXpg   /* out */
            )
        {
            sawXpg = false;

            int count = 0;
            int length = format.Length;
            int index = 0;

            while (index < length)
            {
                if (format[index] != Characters.PercentSign)
                {
                    index++;
                    continue;
                }

                index++; /* consume the percent sign. */

                if (index >= length)
                    break;

                //
                // NOTE: A literal "%%" is not a conversion specifier.
                //
                if (format[index] == Characters.PercentSign)
                {
                    index++;
                    continue;
                }

                //
                // NOTE: Skip an XPG positional specifier (just note that one
                //       was seen).
                //
                {
                    int savedIndex = index;
                    bool haveNumber = false;

                    while ((index < length) && Char.IsDigit(format[index]))
                    {
                        haveNumber = true;
                        index++;
                    }

                    if (haveNumber && (index < length) &&
                            (format[index] == Characters.DollarSign))
                    {
                        sawXpg = true;
                        index++;
                    }
                    else
                    {
                        index = savedIndex;
                    }
                }

                //
                // NOTE: Skip the assignment-suppression flag (a suppressed
                //       specifier consumes no variable).
                //
                bool suppress = false;

                if ((index < length) && (format[index] == Characters.Asterisk))
                {
                    suppress = true;
                    index++;
                }

                //
                // NOTE: Skip the field width and any size modifiers.
                //
                while ((index < length) && Char.IsDigit(format[index]))
                    index++;

                while ((index < length) &&
                        ((format[index] == Characters.l) ||
                         (format[index] == Characters.h) ||
                         (format[index] == Characters.L)))
                {
                    index++;
                }

                if (index >= length)
                    break;

                char conversion = format[index];
                index++;

                //
                // NOTE: Skip over a character-set specification.
                //
                if (conversion == Characters.OpenBracket)
                {
                    int setEnd = ParseCharSet(format, index);

                    if (setEnd == Index.Invalid)
                        break;

                    index = setEnd + 1;
                }

                if (!suppress)
                    count++;
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified character is a valid
        /// digit for the specified numeric radix, treating both upper-case and
        /// lower-case letters as digit values beyond nine.
        /// </summary>
        /// <param name="character">
        /// The character to evaluate.
        /// </param>
        /// <param name="radix">
        /// The numeric radix against which the character is evaluated.
        /// </param>
        /// <returns>
        /// True if the specified character is a valid digit for the specified
        /// radix; otherwise, false.
        /// </returns>
        private static bool IsRadixDigit(
            char character, /* in */
            int radix       /* in */
            )
        {
            int digitValue;

            if ((character >= Characters.Zero) && (character <= Characters.Nine))
                digitValue = character - Characters.Zero;
            else if ((character >= Characters.a) && (character <= Characters.z))
                digitValue = (character - Characters.a) + Parser.DecimalRadix;
            else if ((character >= Characters.A) && (character <= Characters.Z))
                digitValue = (character - Characters.A) + Parser.DecimalRadix;
            else
                return false;

            return digitValue < radix;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified message to the specified string
        /// builder, separating it from any existing content with a comma and a
        /// space, creating a new string builder when one is not supplied.
        /// </summary>
        /// <param name="message">
        /// The message to append.  When null or empty, nothing is appended.
        /// </param>
        /// <param name="result">
        /// The string builder to which the message is appended.  When null, a
        /// new string builder is created and stored here.
        /// </param>
        public static void AppendWithComma(
            string message,
            ref StringBuilder result
            )
        {
            if (result == null)
                result = SBF.CreateNoCache(); /* EXEMPT */

            if (!String.IsNullOrEmpty(message))
            {
                if (result.Length > 0)
                {
                    result.Append(Characters.Comma);
                    result.Append(Characters.Space);
                }

                result.Append(message);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes the exponent portion of the specified
        /// formatted numeric segment by stripping superfluous leading zeros and
        /// padding the exponent to the minimum required length.
        /// </summary>
        /// <param name="segment">
        /// The string builder containing the formatted numeric segment to fix
        /// up.  When null, nothing is done.
        /// </param>
        /// <param name="positiveSign">
        /// The string used to represent a positive sign in the exponent.  This
        /// parameter may be null.
        /// </param>
        /// <param name="negativeSign">
        /// The string used to represent a negative sign in the exponent.  This
        /// parameter may be null.
        /// </param>
        private static void FixupExponentSuffix(
            StringBuilder segment,
            string positiveSign,
            string negativeSign
            )
        {
            if ((segment == null) || (ExponentPrefixChars == null))
                return;

            int length = segment.Length;
            int startIndex = length - 1;

            for (; startIndex >= 0; startIndex--)
            {
                char character = segment[startIndex];

                if (Array.IndexOf(
                        ExponentPrefixChars, character) != Index.Invalid)
                {
                    break;
                }
            }

            if (startIndex >= 0)
            {
                int nextIndex = startIndex + 1; /* NOTE: Skip "E" or "e". */

                if ((positiveSign != null) || (negativeSign != null))
                {
                    string segmentString = segment.ToString();

                    if ((positiveSign != null) && segmentString.Substring(
                            nextIndex).StartsWith(positiveSign))
                    {
                        nextIndex += positiveSign.Length;
                        goto signDone;
                    }
                    else if ((negativeSign != null) && segmentString.Substring(
                            nextIndex).StartsWith(negativeSign))
                    {
                        nextIndex += negativeSign.Length;
                        goto signDone;
                    }
                }

                nextIndex++; /* NOTE: Skip "+" or "-". */

            signDone:

                int endIndex = nextIndex;

                for (; endIndex < length; endIndex++)
                {
                    if (segment[endIndex] == Characters.Zero)
                        continue;

                    break;
                }

                if (endIndex > nextIndex)
                {
                    segment.Remove(
                        nextIndex, endIndex - nextIndex);

                    int zeros = MinimumExponentLength;

                    zeros -= segment.Length - nextIndex;

                    if (zeros > 0)
                    {
                        segment.Insert(
                            nextIndex, Characters.Zero.ToString(),
                            zeros);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified arguments according to the
        /// specified format string, mimicking the behavior of the Tcl [format]
        /// command, and appends the resulting text to the specified string
        /// builder.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to evaluate formatting flags and limits.
        /// This parameter may be null.
        /// </param>
        /// <param name="format">
        /// The format string describing how the supplied arguments are
        /// converted to text.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be formatted according to the format string.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used to perform culture-sensitive formatting.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// The string builder to which the formatted text is appended.  Upon
        /// success, it contains the formatted output; when null, a new string
        /// builder is created and stored here.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error that
        /// was encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode AppendWithFormat(
            Interpreter interpreter,
            string format,
            ArgumentList arguments,
            CultureInfo cultureInfo,
            ref StringBuilder result,
            ref Result error
            )
        {
            bool legacyOctal = ScriptOps.HasFlags(
                interpreter, InterpreterFlags.LegacyOctal, true);

#if NATIVE
            bool usePrintfForDouble = ScriptOps.HasFlags(
                interpreter, InterpreterFlags.UsePrintfForDouble,
                true);
#endif

            int spanIndex = 0;
            string message = null;
            int numBytes = 0;
            int argumentIndex = 0;
            bool gotXpg = false;
            bool gotSequential = false;
            int originalLength;
            int limit;

            StringBuilder localResult = SBF.CreateNoCache(); /* EXEMPT */

            originalLength = localResult.Length;
            limit = localResult.MaxCapacity - originalLength;

#if RESULT_LIMITS
            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    int executeResultLimit = interpreter.InternalExecuteResultLimit;

                    if ((executeResultLimit > 0) && (executeResultLimit < limit))
                        limit = executeResultLimit;
                }
            }
#endif

            int formatLength = format.Length;
            int index = 0;

            while (index < formatLength)
            {
                int endIndex = 0;
                bool gotMinus;
                bool gotHash;
                bool gotZero;
                bool gotSpace;
                bool gotPlus;
                bool gotSlash;
                bool sawFlag;

                int width;
                bool gotPrecision;
                int precision;
                bool useByte;
                bool useShort;
                bool useWide;
                bool useBig;

                bool newXpg;
                int numChars;
                int segmentLimit;
                int segmentNumBytes;

                StringBuilder segment;
                char character;
                bool skipPadding;

                character = CharOrNull(format, index);
                index++;

                if (character != Characters.PercentSign)
                {
                    numBytes++;
                    continue;
                }

                if (numBytes > 0)
                {
                    if (numBytes > limit)
                    {
                        message = OverflowError;
                        goto errorMessage;
                    }

                    localResult.Append(format, spanIndex, numBytes);
                    limit -= numBytes;
                    numBytes = 0;
                }

                /*
                 * Saw a % : process the format specifier.
                 *
                 * Step 0. Handle special case of escaped format marker (i.e., %%).
                 */

                character = CharOrNull(format, index);

                if (character == Characters.PercentSign)
                {
                    spanIndex = index;
                    numBytes = 1;
                    index++;
                    continue;
                }

                /*
                 * Step 1. XPG3 position specifier
                 */

                newXpg = false;

                if (Char.IsDigit(character))
                {
                    int position = Parser.ParseInteger(
                        format, index, formatLength, Parser.DecimalRadix,
                        true, true, true, legacyOctal, ref endIndex);

                    if (CharOrNull(format, endIndex) == Characters.DollarSign)
                    {
                        newXpg = true;
                        argumentIndex = position - 1;
                        index = endIndex + 1;
                        character = CharOrNull(format, index);
                    }
                }

                if (newXpg)
                {
                    if (gotSequential)
                    {
                        message = mixedXpgError;
                        goto errorMessage;
                    }
                    gotXpg = true;
                }
                else
                {
                    if (gotXpg)
                    {
                        message = mixedXpgError;
                        goto errorMessage;
                    }
                    gotSequential = true;
                }

                if ((argumentIndex < 0) || (argumentIndex >= arguments.Count))
                {
                    message = BadIndexError[ConversionOps.ToInt(gotXpg)];
                    goto errorMessage;
                }

                /*
                 * Step 2. Set of flags.
                 */

                gotMinus = gotHash = gotZero = gotSpace = gotPlus = gotSlash = false;
                sawFlag = true;

                do
                {
                    switch (character)
                    {
                        case Characters.MinusSign:
                            gotMinus = true;
                            break;
                        case Characters.NumberSign:
                            gotHash = true;
                            break;
                        case Characters.Zero:
                            gotZero = true;
                            break;
                        case Characters.Space:
                            gotSpace = true;
                            break;
                        case Characters.PlusSign:
                            gotPlus = true;
                            break;
                        case Characters.Slash:
                            gotSlash = true;
                            break;
                        default:
                            sawFlag = false;
                            break;
                    }

                    if (sawFlag)
                    {
                        index++;
                        character = CharOrNull(format, index);
                    }
                } while (sawFlag);

                /*
                 * Step 3. Minimum field width.
                 */

                width = 0;

                if (Char.IsDigit(character))
                {
                    width = Parser.ParseInteger(
                        format, index, formatLength, Parser.DecimalRadix,
                        true, true, true, legacyOctal, ref endIndex);

                    index = endIndex;
                    character = CharOrNull(format, index);
                }
                else if (character == Characters.Asterisk)
                {
                    if (argumentIndex >= (arguments.Count - 1))
                    {
                        message = BadIndexError[ConversionOps.ToInt(gotXpg)];
                        goto errorMessage;
                    }

                    if (Value.GetInteger2(
                            (IGetValue)arguments[argumentIndex],
                            ValueFlags.AnyInteger, cultureInfo,
                            ref width, ref error) != ReturnCode.Ok)
                    {
                        goto error;
                    }

                    if (width < 0)
                    {
                        width = -width;
                        gotMinus = true;
                    }

                    argumentIndex++;

                    index++;
                    character = CharOrNull(format, index);
                }

                if (width > limit)
                {
                    message = OverflowError;
                    goto errorMessage;
                }

                /*
                 * Step 4. Precision.
                 */

                gotPrecision = false; precision = 0;

                if (character == Characters.Period)
                {
                    gotPrecision = true;
                    index++;
                    character = CharOrNull(format, index);
                }

                if (Char.IsDigit(character))
                {
                    precision = Parser.ParseInteger(
                        format, index, formatLength, Parser.DecimalRadix,
                        true, true, true, legacyOctal, ref endIndex);

                    index = endIndex;
                    character = CharOrNull(format, index);
                }
                else if (character == Characters.Asterisk)
                {
                    if (argumentIndex >= (arguments.Count - 1))
                    {
                        message = BadIndexError[ConversionOps.ToInt(gotXpg)];
                        goto errorMessage;
                    }

                    if (Value.GetInteger2(
                            (IGetValue)arguments[argumentIndex],
                            ValueFlags.AnyInteger, cultureInfo,
                            ref precision, ref error) != ReturnCode.Ok)
                    {
                        goto error;
                    }

                    /*
                     * TODO: Check this truncation logic.
                     */

                    if (precision < 0)
                        precision = 0;

                    argumentIndex++;
                    index++;
                    character = CharOrNull(format, index);
                }

                /*
                 * Step 5. Length modifier.
                 */

                useByte = useShort = useWide = useBig = false;

                if (character == Characters.y) /* SBYTE */
                {
                    useByte = true;

                    index++;
                    character = CharOrNull(format, index);
                }
                else if (character == Characters.h) /* SHORT */
                {
                    index++;
                    character = CharOrNull(format, index);

                    if (character == Characters.h) /* SBYTE */
                    {
                        useByte = true;

                        index++;
                        character = CharOrNull(format, index);
                    }
                    else
                    {
                        useShort = true;
                    }
                }
                else if (character == Characters.l) /* LONG */
                {
                    index++;
                    character = CharOrNull(format, index);

                    if (character == Characters.l) /* BIGNUM */
                    {
                        useBig = true;

                        index++;
                        character = CharOrNull(format, index);
                    }
                    else
                    {
                        useWide = true;
                    }
                }

                index++;
                spanIndex = index;

                /*
                 * Step 6. The actual conversion character.
                 */

                skipPadding = false;

                segment = SBF.CreateNoCache(arguments[argumentIndex]); /* EXEMPT */

                if (character == Characters.i)
                    character = Characters.d;

                int intValue = 0;
                long longValue = 0;

#if NET_40
                BigInteger bigIntegerValue = BigInteger.Zero;
#endif

                double doubleValue = 0.0;

                switch (character)
                {
                    case Characters.Null:
                        {
                            message = "format string ended in middle of field specifier";
                            goto errorMessage;
                        }
                    case Characters.s:
                        {
                            numChars = segment.Length;

                            if (gotPrecision && (precision < numChars))
                                segment.Length = precision;

                            break;
                        }
                    case Characters.c:
                        {
                            int code = 0;

                            if (Value.GetInteger2(segment.ToString(),
                                    ValueFlags.AnyInteger | ValueFlags.AnySignedness,
                                    cultureInfo, ref code, ref error) != ReturnCode.Ok)
                            {
                                goto error;
                            }

                            segment = SBF.CreateNoCache(
                                ConversionOps.ToChar(code).ToString()); /* EXEMPT */

                            break;
                        }
                    case Characters.u:
                        {
                            if (useBig)
                            {
                                message = "unsigned bignum format is invalid";
                                goto errorMessage;
                            }

                            goto case Characters.d;
                        }
                    case Characters.d:
                    case Characters.o:
                    case Characters.x:
                    case Characters.X:
                    case Characters.b:
                        {
                            sbyte sbyteValue = 0; /* Silence compiler warning; only defined and
                                                   * used when useByte is true. */
                            short shortValue = 0; /* Silence compiler warning; only defined and
                                                   * used when useShort is true. */
                            int toAppend;
                            bool isNegative = false;

                            if (gotSlash)
                            {
                                if (Value.GetDouble(
                                        segment.ToString(), cultureInfo, ref doubleValue,
                                        ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }

                                if (useWide)
                                {
                                    //
                                    // HACK: Grab the exact bits of the double
                                    //       and use those for the long value.
                                    //
                                    longValue = BitConverter.DoubleToInt64Bits(
                                        doubleValue);

                                    isNegative = (longValue < 0);
                                }
#if NET_40
                                else if (useBig)
                                {
                                    //
                                    // HACK: Grab the exact bits of the double and
                                    //       use those for the BigInteger value.
                                    //
                                    bigIntegerValue = new BigInteger(
                                        BitConverter.DoubleToInt64Bits(doubleValue));

                                    isNegative = (bigIntegerValue < 0);
                                }
#endif
                                else
                                {
                                    //
                                    // HACK: Grab the exact bits of the double
                                    //       and use those for the int value.
                                    //
                                    intValue = ConversionOps.ToInt(
                                        BitConverter.DoubleToInt64Bits(doubleValue));

                                    isNegative = (intValue < 0);
                                }
                            }
#if NET_40
                            else if (useBig)
                            {
                                if (Value.GetBigInteger2(segment.ToString(),
                                        ValueFlags.AnyInteger, cultureInfo, ref bigIntegerValue,
                                        ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }

                                isNegative = (bigIntegerValue < 0);
                            }
#endif
                            else if (useWide)
                            {
                                if (Value.GetWideInteger2(segment.ToString(),
                                        ValueFlags.AnyWideInteger | ValueFlags.AnySignedness,
                                        cultureInfo, ref longValue, ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }

                                isNegative = (longValue < 0);
                            }
                            else if (Value.GetInteger2(segment.ToString(),
                                    ValueFlags.AnyInteger | ValueFlags.AnySignedness,
                                    cultureInfo, ref intValue, ref error) != ReturnCode.Ok)
                            {
                                if (Value.GetWideInteger2(segment.ToString(),
                                        ValueFlags.AnyWideInteger | ValueFlags.AnySignedness,
                                        cultureInfo, ref longValue) != ReturnCode.Ok)
                                {
                                    goto error;
                                }
                                else
                                {
                                    intValue = ConversionOps.ToInt(longValue);
                                }

                                if (useByte)
                                {
                                    sbyteValue = ConversionOps.ToSByte(intValue);
                                    isNegative = (sbyteValue < 0);
                                }
                                else if (useShort)
                                {
                                    shortValue = ConversionOps.ToShort(intValue);
                                    isNegative = (shortValue < 0);
                                }
                                else
                                {
                                    isNegative = (intValue < 0);
                                }
                            }
                            else if (useByte)
                            {
                                sbyteValue = ConversionOps.ToSByte(intValue);
                                isNegative = (sbyteValue < 0);
                            }
                            else if (useShort)
                            {
                                shortValue = ConversionOps.ToShort(intValue);
                                isNegative = (shortValue < 0);
                            }
                            else
                            {
                                isNegative = (intValue < 0);
                            }

                            segment = SBF.CreateNoCache(); /* EXEMPT */
                            segmentLimit = segment.MaxCapacity;

                            if ((isNegative || gotPlus || gotSpace) &&
                                (useBig || (character == Characters.d)))
                            {
                                segment.Append(isNegative ? Characters.MinusSign :
                                    (gotPlus ? Characters.PlusSign : Characters.Space));

                                segmentLimit--;
                            }

                            if (gotHash)
                            {
                                switch (character)
                                {
                                    case Characters.b:
                                        {
                                            segment.Append(BinaryPrefix);
                                            segmentLimit -= BinaryPrefix.Length;
                                            break;
                                        }
                                    case Characters.o:
                                        {
                                            if (legacyOctal)
                                            {
                                                segment.Append(LegacyOctalPrefix);
                                                segmentLimit -= LegacyOctalPrefix.Length;
                                            }
                                            else
                                            {
                                                segment.Append(OctalPrefix);
                                                segmentLimit -= OctalPrefix.Length;
                                            }
                                            break;
                                        }
                                    case Characters.d:
                                        {
                                            segment.Append(DecimalPrefix);
                                            segmentLimit -= DecimalPrefix.Length;
                                            break;
                                        }
                                    case Characters.x:
                                    case Characters.X:
                                        {
                                            segment.Append(HexadecimalPrefix);
                                            segmentLimit -= HexadecimalPrefix.Length;
                                            break;
                                        }
                                }
                            }

                            switch (character)
                            {
                                case Characters.d:
                                    {
                                        int length;
                                        string bytes;
                                        int byteIndex = 0;

                                        if (useByte)
                                            bytes = sbyteValue.ToString();
                                        else if (useShort)
                                            bytes = shortValue.ToString();
                                        else if (useWide)
                                            bytes = longValue.ToString();
#if NET_40
                                        else if (useBig)
                                            bytes = bigIntegerValue.ToString();
#endif
                                        else
                                            bytes = intValue.ToString();

                                        length = bytes.Length;

                                        /*
                                         * Already did the sign above.
                                         */

                                        if (bytes[0] == Characters.MinusSign)
                                        {
                                            length--;
                                            byteIndex++;
                                        }

                                        toAppend = length;

                                        /*
                                         * Canonical decimal string reps for integers are composed
                                         * entirely of one-byte encoded characters, so "length" is the
                                         * number of chars.
                                         */

                                        if (gotPrecision)
                                        {
                                            if (length < precision)
                                                segmentLimit -= (precision - length);

                                            while (length < precision)
                                            {
                                                segment.Append(Characters.Zero);
                                                length++;
                                            }
                                            gotZero = false;
                                        }

                                        if (gotZero)
                                        {
                                            length += segment.Length;

                                            if (length < width)
                                                segmentLimit -= (width - length);

                                            while (length < width)
                                            {
                                                segment.Append(Characters.Zero);
                                                length++;
                                            }
                                        }

                                        if (toAppend > segmentLimit)
                                        {
                                            message = OverflowError;
                                            goto errorMessage;
                                        }

                                        segment.Append(bytes, byteIndex, toAppend);
                                        break;
                                    }
                                case Characters.u:
                                case Characters.o:
                                case Characters.x:
                                case Characters.X:
                                case Characters.b:
                                    {
                                        ulong bits = 0;

#if NET_40
                                        BigInteger bigBits = BigInteger.Zero;
#endif

                                        int numDigits = 0;
                                        int length, radix = Parser.HexadecimalRadix;

                                        if (character == Characters.u)
                                            radix = Parser.DecimalRadix;
                                        else if (character == Characters.o)
                                            radix = Parser.OctalRadix;
                                        else if (character == Characters.b)
                                            radix = Parser.BinaryRadix;

                                        if (useByte)
                                        {
                                            byte byteValue = ConversionOps.ToByte(sbyteValue);

                                            bits = byteValue;
                                            while (byteValue != 0)
                                            {
                                                numDigits++;
                                                byteValue /= (byte)radix;
                                            }
                                        }
                                        else if (useShort)
                                        {
                                            ushort ushortValue = ConversionOps.ToUShort(shortValue);

                                            bits = ushortValue;
                                            while (ushortValue != 0)
                                            {
                                                numDigits++;
                                                ushortValue /= (ushort)radix;
                                            }
                                        }
                                        else if (useWide)
                                        {
                                            ulong ulongValue = ConversionOps.ToULong(longValue);

                                            bits = ulongValue;
                                            while (ulongValue != 0)
                                            {
                                                numDigits++;
                                                ulongValue /= (ulong)radix;
                                            }
                                        }
#if NET_40
                                        else if (useBig)
                                        {
                                            BigInteger ubigIntegerValue = BigInteger.Abs(
                                                bigIntegerValue);

                                            bigBits = ubigIntegerValue;
                                            while (ubigIntegerValue != 0)
                                            {
                                                numDigits++;
                                                ubigIntegerValue /= (ulong)radix;
                                            }
                                        }
#endif
                                        else
                                        {
                                            uint uintValue = ConversionOps.ToUInt(intValue);

                                            bits = uintValue;
                                            while (uintValue != 0)
                                            {
                                                numDigits++;
                                                uintValue /= (uint)radix;
                                            }
                                        }

                                        /*
                                         * Need to be sure zero becomes "0", not "".
                                         */

                                        if ((numDigits == 0) &&
                                            !((character == Characters.o) && gotHash))
                                        {
                                            numDigits = 1;
                                        }

                                        StringBuilder bytes = SBF.CreateNoCache(
                                            numDigits); /* EXEMPT */

                                        bytes.Length = numDigits;

                                        toAppend = length = (int)numDigits;

#if NET_40
                                        if (useBig)
                                        {
                                            while (numDigits-- > 0)
                                            {
                                                int digitOffset = (int)(bigBits % (ulong)radix);

                                                bytes[numDigits] = (digitOffset > 9) ?
                                                    (char)(Characters.a + digitOffset - Parser.DecimalRadix) :
                                                    (char)(Characters.Zero + digitOffset);

                                                bigBits /= (ulong)radix;
                                            }
                                        }
                                        else
#endif
                                        {
                                            while (numDigits-- > 0)
                                            {
                                                int digitOffset = (int)(bits % (ulong)radix);

                                                bytes[numDigits] = (digitOffset > 9) ?
                                                    (char)(Characters.a + digitOffset - Parser.DecimalRadix) :
                                                    (char)(Characters.Zero + digitOffset);

                                                bits /= (ulong)radix;
                                            }
                                        }

                                        if (gotPrecision)
                                        {
                                            if (length < precision)
                                                segmentLimit -= (precision - length);

                                            while (length < precision)
                                            {
                                                segment.Append(Characters.Zero);
                                                length++;
                                            }

                                            gotZero = false;
                                        }

                                        if (gotZero)
                                        {
                                            length += segment.Length;

                                            if (length < width)
                                                segmentLimit -= (width - length);

                                            while (length < width)
                                            {
                                                segment.Append(Characters.Zero);
                                                length++;
                                            }
                                        }

                                        if (toAppend > segmentLimit)
                                        {
                                            message = OverflowError;
                                            goto errorMessage;
                                        }

                                        segment.Append(bytes);
                                        break;
                                    }
                            }
                            break;
                        }
                    case Characters.e:
                    case Characters.E:
                    case Characters.f:
                    case Characters.g:
                    case Characters.G:
                        {
                            if (gotSlash)
                            {
                                if (useWide)
                                {
                                    if (Value.GetWideInteger2(segment.ToString(),
                                            ValueFlags.AnyWideInteger | ValueFlags.AnySignedness,
                                            cultureInfo, ref longValue, ref error) != ReturnCode.Ok)
                                    {
                                        goto error;
                                    }

                                    doubleValue = BitConverter.Int64BitsToDouble(
                                        longValue);
                                }
#if NET_40
                                else if (useBig)
                                {
                                    if (Value.GetBigInteger2(segment.ToString(),
                                            ValueFlags.AnyInteger, cultureInfo, ref bigIntegerValue,
                                            ref error) != ReturnCode.Ok)
                                    {
                                        goto error;
                                    }

                                    doubleValue = BitConverter.Int64BitsToDouble(
                                        (long)bigIntegerValue);
                                }
#endif
                                else
                                {
                                    if (Value.GetInteger2(segment.ToString(),
                                            ValueFlags.AnyInteger | ValueFlags.AnySignedness,
                                            cultureInfo, ref intValue, ref error) != ReturnCode.Ok)
                                    {
                                        if (Value.GetWideInteger2(segment.ToString(),
                                                ValueFlags.AnyWideInteger | ValueFlags.AnySignedness,
                                                cultureInfo, ref longValue) != ReturnCode.Ok)
                                        {
                                            goto error;
                                        }
                                        else
                                        {
                                            intValue = ConversionOps.ToInt(longValue);
                                        }
                                    }

                                    doubleValue = BitConverter.Int64BitsToDouble(
                                        ConversionOps.ToLong(intValue));
                                }
                            }
                            else
                            {
                                if (Value.GetDouble(
                                        segment.ToString(), cultureInfo, ref doubleValue,
                                        ref error) != ReturnCode.Ok)
                                {
                                    /* TODO: Figure out ACCEPT_NAN here */
                                    goto error;
                                }
                            }

                            string positiveSign = null;
                            string negativeSign = null;

#if NATIVE
                            if (usePrintfForDouble)
                            {
                                StringBuilder spec = SBF.CreateNoCache(); /* EXEMPT */

                                spec.Append(Characters.PercentSign);

                                if (gotMinus)
                                    spec.Append(Characters.MinusSign);

                                if (gotHash)
                                    spec.Append(Characters.NumberSign);

                                if (gotZero)
                                    spec.Append(Characters.Zero);

                                if (gotSpace)
                                    spec.Append(Characters.Space);

                                if (gotPlus)
                                    spec.Append(Characters.PlusSign);

                                if (width > 0)
                                    spec.AppendFormat("{0}", width);

                                if (gotPrecision)
                                {
                                    spec.AppendFormat(
                                        "{0}{1}", Characters.Period,
                                        precision);
                                }

                                /*
                                 * Don't pass length modifiers!
                                 */

                                spec.Append(character);
                                segment = SBF.CreateNoCache(); /* EXEMPT */

                                /*
                                 * NOTE: When compiled with native code enabled,
                                 *       use the native function snprintf(), or
                                 *       some variation thereof, as exported by
                                 *       the MSVCRT on Windows or libc on Unix.
                                 */

                                if (NativeOps.PrintDouble(
                                        segment, spec.ToString(), doubleValue,
                                        ref error) != ReturnCode.Ok)
                                {
                                    goto error;
                                }
                            }
                            else
#endif
                            {
                                StringBuilder spec = SBF.CreateNoCache(); /* EXEMPT */

                                spec.Append(Characters.OpenBrace);
                                spec.Append(Characters.Zero);
                                spec.Append(Characters.Colon);
                                spec.Append(character);

                                int usePrecision = gotPrecision ?
                                    precision : DoubleDefaultPrecision;

                                spec.Append(usePrecision);
                                spec.Append(Characters.CloseBrace);

                                segment = SBF.CreateNoCache(); /* EXEMPT */

                                segment.AppendFormat(
                                    spec.ToString(), doubleValue);

                                positiveSign = Characters.PlusSign.ToString();
                                negativeSign = Characters.MinusSign.ToString();

                                string decimalSeparator = Characters.Period.ToString();

                                if (cultureInfo != null)
                                {
                                    NumberFormatInfo numberFormat = cultureInfo.NumberFormat;

                                    if (numberFormat != null)
                                    {
                                        positiveSign = numberFormat.PositiveSign;
                                        negativeSign = numberFormat.NegativeSign;
                                        decimalSeparator = numberFormat.NumberDecimalSeparator;
                                    }
                                }

                                if (gotPlus && (doubleValue >= 0))
                                    segment.Insert(0, positiveSign);

                                bool hasSign = false;
                                string segmentString = segment.ToString();

                                if (segmentString.StartsWith(positiveSign) ||
                                    segmentString.StartsWith(negativeSign))
                                {
                                    hasSign = true;
                                }

                                bool isGeneral = (character == Characters.G) ||
                                    (character == Characters.g);

                                if (gotHash || isGeneral)
                                {
                                    int indexOfDecimalSeparator = segmentString.IndexOf(
                                        decimalSeparator);

                                    int indexOfExponent = (ExponentPrefixChars != null) ?
                                        segmentString.IndexOfAny(ExponentPrefixChars) :
                                        Index.Invalid;

                                    if (indexOfDecimalSeparator != Index.Invalid)
                                    {
                                        if (!gotHash && isGeneral)
                                        {
                                            if (indexOfExponent != Index.Invalid)
                                            {
                                                string mantissa = segmentString.Substring(
                                                    0, indexOfExponent);

                                                string exponent = segmentString.Substring(
                                                    indexOfExponent);

                                                mantissa = mantissa.TrimEnd(
                                                    Characters.Zero);

                                                int mantissaLength = mantissa.Length;

                                                if (mantissa.EndsWith(
                                                        decimalSeparator))
                                                {
                                                    mantissa = mantissa.Substring(
                                                        0, mantissaLength - 1);
                                                }

                                                segment = SBF.CreateNoCache(
                                                    segmentString.Length); /* EXEMPT */

                                                segment.Append(mantissa);
                                                segment.Append(exponent);
                                            }
                                            else
                                            {
                                                segmentString = segmentString.TrimEnd(
                                                    Characters.Zero);

                                                int segmentLength = segmentString.Length;

                                                if (segmentString.EndsWith(
                                                        decimalSeparator))
                                                {
                                                    segmentString = segmentString.Substring(
                                                        0, segmentLength - 1);
                                                }

                                                segment = SBF.CreateNoCache(
                                                    segmentString); /* EXEMPT */
                                            }
                                        }
                                    }
                                    else if (gotHash)
                                    {
                                        int zeros = usePrecision;

                                        if (indexOfExponent != Index.Invalid)
                                        {
                                            zeros -= indexOfExponent;

                                            segment.Insert(
                                                indexOfExponent, String.Format(
                                                "{0}{1}", decimalSeparator,
                                                (zeros > 0) ? StrRepeat(zeros,
                                                Characters.Zero) : String.Empty));
                                        }
                                        else
                                        {
                                            zeros -= segment.Length;

                                            segment.Append(decimalSeparator);

                                            if (zeros > 0)
                                            {
                                                segment.Append(
                                                    Characters.Zero, zeros);
                                            }
                                        }
                                    }
                                }

                                FixupExponentSuffix(
                                    segment, positiveSign, negativeSign);

                                if (segment.Length < width)
                                {
                                    if (gotMinus)
                                    {
                                        segment.Append(Characters.Space,
                                            (width - segment.Length));
                                    }
                                    else if (gotZero)
                                    {
                                        segment.Insert(hasSign &&
                                            (segment.Length > 0) ? 1 : 0,
                                            Characters.Zero.ToString(),
                                            (width - segment.Length));
                                    }
                                    else
                                    {
                                        segment.Insert(
                                            0, Characters.SpaceString,
                                            (width - segment.Length));
                                    }
                                }
                                else if (gotSpace && !hasSign)
                                {
                                    segment.Insert(0, Characters.Space);
                                }

                                skipPadding = true;
                            }
                            break;
                        }
                    default:
                        {
                            message = String.Format(
                                "bad field specifier \"{0}\"",
                                character);

                            goto errorMessage;
                        }
                }

                switch (character)
                {
                    // case Characters.E:
                    // case Characters.G:
                    case Characters.X:
                        {
                            segment = SBF.CreateNoCache(
                                segment.ToString().ToUpper()); /* EXEMPT */

                            break;
                        }
                }

                char padding = gotZero ?
                    Characters.Zero : Characters.Space;

                numChars = segment.Length;

                if (!skipPadding)
                {
                    if (!gotMinus)
                    {
                        if (numChars < width)
                            limit -= (width - numChars);

                        while (numChars < width)
                        {
                            localResult.Append(padding);
                            numChars++;
                        }
                    }
                }

                segmentNumBytes = segment.Length;

                if (segmentNumBytes > limit)
                {
                    message = OverflowError;
                    goto errorMessage;
                }

                localResult.Append(segment);
                limit -= segmentNumBytes;

                if (!skipPadding)
                {
                    if (numChars < width)
                        limit -= (width - numChars);

                    while (numChars < width)
                    {
                        localResult.Append(padding);
                        numChars++;
                    }
                }

                argumentIndex += ConversionOps.ToInt(gotSequential);
            }

            if (numBytes > 0)
            {
                if (numBytes > limit)
                {
                    message = OverflowError;
                    goto errorMessage;
                }

                localResult.Append(format, spanIndex, numBytes);
                limit -= numBytes;
                numBytes = 0;
            }

            result = localResult;
            return ReturnCode.Ok;

        errorMessage:
            error = message;

        error:
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method applies the specified prefix and/or suffix to each of
        /// the sub-patterns in the specified list, when the associated handling
        /// flags are enabled.  If there are no sub-patterns and prefix/suffix
        /// handling is enabled, a single sub-pattern consisting of the prefix
        /// and suffix will be created.
        /// </summary>
        /// <param name="subPatterns">
        /// The list of sub-patterns to be modified in place.
        /// </param>
        /// <param name="prefix">
        /// The prefix string to be inserted at the start of each sub-pattern,
        /// or null if there is no prefix.
        /// </param>
        /// <param name="suffix">
        /// The suffix string to be appended to the end of each sub-pattern, or
        /// null if there is no suffix.
        /// </param>
        /// <param name="withPrefix">
        /// Non-zero to enable handling of the prefix.
        /// </param>
        /// <param name="withSuffix">
        /// Non-zero to enable handling of the suffix.
        /// </param>
        private static void FixupSubPatterns(
            IList<StringBuilder> subPatterns,
            string prefix,
            string suffix,
            bool withPrefix,
            bool withSuffix
            )
        {
            //
            // NOTE: Is sub-pattern prefix and/or suffix handling enabled?
            //
            if ((withPrefix && (prefix != null)) ||
                (withSuffix && (suffix != null)))
            {
                //
                // NOTE: If necessary, add the prefix and suffix we found to
                //       each of the sub-patterns.
                //
                if (subPatterns != null)
                {
                    foreach (StringBuilder subPattern in subPatterns)
                    {
                        if (subPattern == null)
                            continue;

                        if (withPrefix && (prefix != null))
                            subPattern.Insert(0, prefix);

                        if (withSuffix && (suffix != null))
                            subPattern.Append(suffix);
                    }
                }
                //
                // NOTE: *SPECIAL* If there are no sub-pattern fragments, make
                //       sure there is at least one sub-pattern, consisting of
                //       the prefix and suffix.
                //
                else
                {
                    StringBuilder subPattern = SBF.CreateNoCache(); /* EXEMPT */

                    if (withPrefix && (prefix != null))
                        subPattern.Append(prefix);

                    if (withSuffix && (suffix != null))
                        subPattern.Append(suffix);

                    subPatterns = new List<StringBuilder>();
                    subPatterns.Add(subPattern);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified pattern string into its component
        /// sub-patterns, expanding any nested brace-enclosed alternatives into
        /// a flattened list of patterns.
        /// </summary>
        /// <param name="pattern">
        /// The pattern string to be split into sub-patterns.
        /// </param>
        /// <param name="startIndex">
        /// The index within the pattern string where processing should begin.
        /// </param>
        /// <param name="empty">
        /// Non-zero to allow empty sub-pattern fragments to be included in the
        /// resulting list.
        /// </param>
        /// <param name="subPatterns">
        /// Upon success, this list will contain the resulting sub-patterns.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SplitSubPatterns(
            string pattern,             /* in */
            int startIndex,             /* in */
            bool empty,                 /* in */
            ref StringList subPatterns, /* in, out */
            ref Result error            /* out */
            )
        {
            string prefix = null;
            string suffix = null;
            IList<IList<StringBuilder>> allSubPatterns = null;

            while (true)
            {
                string localPrefix = null;
                string localSuffix = null;
                IList<StringBuilder> localSubPatterns = null;
                int stopIndex = Index.Invalid;

                if (SplitSubPatterns(
                        pattern, startIndex, empty, true, false,
                        false, ref localPrefix, ref localSuffix,
                        ref localSubPatterns, ref stopIndex,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                bool withPrefix = false;

                if (localSubPatterns == null)
                {
                    if (localSuffix != null)
                    {
                        //
                        // NOTE: The suffix was set for this loop iteration and
                        //       takes over the existing outer suffix, replace
                        //       it.
                        //
                        suffix = localSuffix;
                    }
                    else if (pattern != null)
                    {
                        //
                        // NOTE: The suffix was not set for this loop iteration;
                        //       however, this is now the final loop iteration
                        //       and there might be additional pattern content,
                        //       use it.
                        //
                        suffix = pattern.Substring(
                            startIndex, pattern.Length - startIndex);
                    }

                    break;
                }

                if (prefix != null)
                {
                    //
                    // NOTE: The prefix was set for this loop iteration, do not
                    //       simply throw it away.
                    //
                    withPrefix = true;
                }
                else if (localPrefix != null)
                {
                    //
                    // NOTE: The prefix was set for this loop iteration and the
                    //       outer prefix is not set, replace it.
                    //
                    prefix = localPrefix;
                }

                FixupSubPatterns(
                    localSubPatterns, localPrefix, localSuffix, withPrefix,
                    false);

                if (allSubPatterns == null)
                    allSubPatterns = new List<IList<StringBuilder>>();

                allSubPatterns.Add(localSubPatterns);
                startIndex = stopIndex + 1;
            }

            if (allSubPatterns != null)
            {
                IList<StringBuilder> localSubPatterns = null;

                if (ListOps.Combine(
                        allSubPatterns, ref localSubPatterns,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                FixupSubPatterns(
                    localSubPatterns, prefix, suffix, true, true);

                subPatterns = ListOps.Flatten(localSubPatterns);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans the specified pattern string, starting at the
        /// given index, and extracts the sub-patterns contained within the
        /// first level of brace-enclosed alternatives, along with the prefix
        /// and suffix surrounding them.
        /// </summary>
        /// <param name="pattern">
        /// The pattern string to be scanned for sub-patterns.
        /// </param>
        /// <param name="startIndex">
        /// The index within the pattern string where scanning should begin.
        /// </param>
        /// <param name="empty">
        /// Non-zero to allow empty sub-pattern fragments to be included in the
        /// resulting list.
        /// </param>
        /// <param name="firstOnly">
        /// Non-zero to stop scanning after the first complete brace-enclosed
        /// sub-pattern list has been processed.
        /// </param>
        /// <param name="withPrefix">
        /// Non-zero to enable handling of the prefix when fixing up the
        /// extracted sub-patterns.
        /// </param>
        /// <param name="withSuffix">
        /// Non-zero to enable handling of the suffix when fixing up the
        /// extracted sub-patterns.
        /// </param>
        /// <param name="prefix">
        /// Upon success, this will contain the portion of the pattern prior to
        /// the first open brace.
        /// </param>
        /// <param name="suffix">
        /// Upon success, this will contain the portion of the pattern after the
        /// last close brace.
        /// </param>
        /// <param name="subPatterns">
        /// Upon success, this list will contain the extracted sub-patterns.
        /// </param>
        /// <param name="stopIndex">
        /// Upon success, this will contain the index within the pattern string
        /// where scanning stopped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode SplitSubPatterns(
            string pattern,                       /* in */
            int startIndex,                       /* in */
            bool empty,                           /* in */
            bool firstOnly,                       /* in */
            bool withPrefix,                      /* in */
            bool withSuffix,                      /* in */
            ref string prefix,                    /* in, out */
            ref string suffix,                    /* in, out */
            ref IList<StringBuilder> subPatterns, /* in, out */
            ref int stopIndex,                    /* out */
            ref Result error                      /* out */
            )
        {
            //
            // NOTE: A null pattern is never allowed, return an error.
            //
            if (pattern == null)
            {
                error = "invalid pattern";
                return ReturnCode.Error;
            }

            //
            // NOTE: An empty pattern is OK, do nothing, and let the caller
            //       handle it.
            //
            int length = pattern.Length;

            if (length == 0)
                return ReturnCode.Ok;

            //
            // NOTE: If there are no sub-patterns, skip this method, and let
            //       the caller handle it.
            //
            if (pattern.IndexOfAny(SubPatternChars, startIndex) == Index.Invalid)
                return ReturnCode.Ok;

            //
            // NOTE: The local list of sub-patterns.  This will become the
            //       final result to be returned to the caller upon success.
            //
            IList<StringBuilder> localSubPatterns = null;

            //
            // NOTE: Initialize various state variables used inside (and after)
            //       the loop to assist in keeping track of the sub-patterns.
            //
            StringBuilder subPattern = null; /* Being built, no prefix/suffix. */
            int prefixIndex = Index.Invalid; /* Index of outer open brace. */
            string localPrefix = null;       /* Before first open brace. */
            string localSuffix = null;       /* After last close brace. */
            int levels = 0;                  /* Brace nesting level. */
            bool quoted = false;             /* True when escape active. */

            //
            // NOTE: Process each character in the pattern string, starting at
            //       the specified location.  This loop will terminate early if
            //       an error is found -OR- the "firstOnly" parameter is set.
            //       In that case, this method will return an appropriate error
            //       message to the caller.
            //
            int index = startIndex;

            for (; index < length; index++)
            {
                //
                // NOTE: Grab the current character within the pattern.
                //
                char character = pattern[index];

                //
                // NOTE: Was the previous character the start of an escape?  If
                //       so, treat this character as a normal one, by appending
                //       it verbatim (i.e. at the bottom of the loop body).
                //
                if (quoted)
                {
                    quoted = false;
                }
                //
                // NOTE: Is the current character the start of an escape?  If
                //       so, set the flag and then treat it as a character, by
                //       appending it verbatim (i.e. at the bottom of the loop
                //       body).
                //
                else if (character == Characters.Backslash)
                {
                    quoted = true;
                }
                //
                // NOTE: Is the current character the start of a sub-pattern
                //       list?
                //
                else if (character == Characters.OpenBrace)
                {
                    //
                    // NOTE: Keep track of how many nested sub-pattern lists
                    //       are active by adding one here.
                    //
                    levels++;

                    //
                    // NOTE: The first outermost list is treated specially.
                    //       The starting index for it is saved so that it
                    //       may be used later to calculate the sub-pattern
                    //       prefix.
                    //
                    if (levels == 1)
                    {
                        if (prefixIndex == Index.Invalid)
                            prefixIndex = index;

                        continue;
                    }
                }
                //
                // NOTE: Is the current character the end of a sub-pattern
                //       list?
                //
                else if (character == Characters.CloseBrace)
                {
                    //
                    // NOTE: Keep track of how many nested sub-pattern lists
                    //       are active by removing one here.
                    //
                    levels--;

                    //
                    // NOTE: If the nesting level falls below zero, the pattern
                    //       is malformed.
                    //
                    if (levels < 0)
                    {
                        error = "unmatched close-brace in pattern";
                        return ReturnCode.Error;
                    }
                    //
                    // NOTE: If the nesting level is exactly zero, we are ready
                    //       to calculate the sub-pattern prefix and suffix,
                    //       which must later be applied to every sub-pattern.
                    //
                    else if (levels == 0)
                    {
                        //
                        // NOTE: Grab the entire portion of the pattern prior
                        //       to the very first open brace.
                        //
                        if (localPrefix == null)
                        {
                            localPrefix = pattern.Substring(
                                startIndex, prefixIndex - startIndex);
                        }

                        //
                        // NOTE: Grab the entire portion of the pattern after
                        //       what we believe is the last close brace.  If
                        //       an additional close brace is found after this
                        //       point, either an error will be generated and
                        //       this suffix will never be used -OR- that new
                        //       suffix will replace this one.
                        //
                        localSuffix = pattern.Substring(
                            index + 1, length - index - 1);

                        //
                        // NOTE: Has there been any sub-pattern content since
                        //       the last comma or open brace?
                        //
                        if (empty ||
                            ((subPattern != null) && (subPattern.Length > 0)))
                        {
                            if (localSubPatterns == null)
                                localSubPatterns = new List<StringBuilder>();

                            localSubPatterns.Add((subPattern != null) ?
                                subPattern : SBF.CreateNoCache()); /* EXEMPT */

                            subPattern = SBF.CreateNoCache(); /* EXEMPT */
                        }

                        //
                        // NOTE: Empty sub-pattern fragment, skip it.
                        //
                        if (firstOnly)
                            break;
                        else
                            continue;
                    }
                }
                //
                // NOTE: When processing the first level of braces, handling
                //       the comma by creating another sub-pattern from all
                //       the content we have seen since the last comma or
                //       open brace.
                //
                else if ((levels == 1) && (character == Characters.Comma))
                {
                    //
                    // NOTE: Has there been any sub-pattern content since
                    //       the last comma or open brace?
                    //
                    if (empty ||
                        ((subPattern != null) && (subPattern.Length > 0)))
                    {
                        if (localSubPatterns == null)
                            localSubPatterns = new List<StringBuilder>();

                        localSubPatterns.Add((subPattern != null) ?
                            subPattern : SBF.CreateNoCache()); /* EXEMPT */

                        subPattern = SBF.CreateNoCache(); /* EXEMPT */
                    }

                    continue;
                }

                //
                // NOTE: Skip the entire prefix (i.e. the portion of the
                //       pattern prior to the outer open brace), without
                //       regard to how many levels exist.
                //
                if (prefixIndex != Index.Invalid)
                {
                    if (subPattern == null)
                        subPattern = SBF.CreateNoCache(); /* EXEMPT */

                    subPattern.Append(character);
                }
            }

            //
            // NOTE: If there are any open braces active, the pattern is
            //       malformed.
            //
            if (levels > 0)
            {
                error = "unmatched open-brace in pattern";
                return ReturnCode.Error;
            }

            //
            // NOTE: Is sub-pattern prefix and/or suffix handling enabled?
            //
            FixupSubPatterns(
                localSubPatterns, localPrefix, localSuffix, withPrefix,
                withSuffix);

            //
            // NOTE: Success, commit changes to the variables provided by
            //       the caller and return.
            //
            prefix = localPrefix;
            suffix = localSuffix;

            if (localSubPatterns != null)
            {
                if (subPatterns != null)
                {
                    GenericOps<StringBuilder>.AddRange(
                        subPatterns, localSubPatterns);
                }
                else
                {
                    subPatterns = new List<StringBuilder>(
                        localSubPatterns);
                }
            }

            stopIndex = index;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the user-configured string comparison type that
        /// is appropriate for the requested case sensitivity.
        /// </summary>
        /// <param name="noCase">
        /// Non-zero to return the case-insensitive comparison type; otherwise,
        /// the case-sensitive comparison type is returned.
        /// </param>
        /// <returns>
        /// The user-configured <see cref="StringComparison" /> value to use.
        /// </returns>
        private static StringComparison GetUserComparisonType(
            bool noCase
            )
        {
            return noCase ?
                UserNoCaseComparisonType : UserComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string comparison type to use, selecting
        /// between the user-configured comparison type and the system comparison
        /// type based on the specified interpreter flags.
        /// </summary>
        /// <param name="interpreterFlags">
        /// The interpreter flags that determine whether the user-configured
        /// (culture) comparison type should be used.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to return a case-insensitive comparison type; otherwise, a
        /// case-sensitive comparison type is returned.
        /// </param>
        /// <returns>
        /// The selected <see cref="StringComparison" /> value to use.
        /// </returns>
        public static StringComparison GetComparisonType(
            InterpreterFlags interpreterFlags,
            bool noCase
            )
        {
            /* EXEMPT */
            if (FlagOps.HasFlags(interpreterFlags,
                    InterpreterFlags.UseCultureForOperators, true))
            {
                return GetUserComparisonType(noCase);
            }
            else
            {
                return SharedStringOps.GetSystemComparisonType(noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string comparison type to use, selecting
        /// between path, system, and user-configured comparison types based on
        /// the specified match mode.
        /// </summary>
        /// <param name="mode">
        /// The match mode flags that determine which comparison type should be
        /// used.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to return a case-insensitive comparison type; otherwise, a
        /// case-sensitive comparison type is returned.
        /// </param>
        /// <returns>
        /// The selected <see cref="StringComparison" /> value to use.
        /// </returns>
        private static StringComparison GetComparisonType(
            MatchMode mode,
            bool noCase
            )
        {
            if (FlagOps.HasFlags(mode, MatchMode.PathString, true))
                return PathOps.GetComparisonType();
            else if (FlagOps.HasFlags(mode, MatchMode.SystemString, true))
                return SharedStringOps.GetSystemComparisonType(noCase);
            else
                return GetUserComparisonType(noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type implements the
        /// named generic interface, optionally verifying that its generic type
        /// arguments match the specified ones.
        /// </summary>
        /// <param name="type">
        /// The type to be checked for the named generic interface.
        /// </param>
        /// <param name="typeName">
        /// The name of the generic interface to look for.
        /// </param>
        /// <param name="typeArguments">
        /// The array of generic type arguments that the interface must match,
        /// or null to skip this verification.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the type implements the named generic interface and any
        /// specified generic type arguments match; otherwise, false.
        /// </returns>
        private static bool HasGenericInterface(
            Type type,
            string typeName,
            Type[] typeArguments,
            ref Result error
            )
        {
            if (type == null)
            {
                error = "invalid type";
                return false;
            }

            Type interfaceType = type.GetInterface(typeName);

            if (interfaceType == null)
            {
                error = "type is not a string comparer";
                return false;
            }

            if (typeArguments != null)
            {
                Type[] interfaceArguments = interfaceType.GetGenericArguments();

                if (interfaceArguments == null)
                {
                    error = "invalid interface generic arguments";
                    return false;
                }

                int length = interfaceArguments.Length;

                if (length != typeArguments.Length)
                {
                    error = "wrong number of interface generic arguments";
                    return false;
                }

                for (int index = 0; index < length; index++)
                {
                    if (!MarshalOps.IsSameType(
                            interfaceArguments[index], typeArguments[index]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an instance of the string comparer named by the
        /// specified value, verifying that the resolved type implements the
        /// <see cref="IComparer{T}" /> interface for strings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="value">
        /// The string naming the comparer type to be created.  An empty value
        /// selects the default string comparer.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when resolving the comparer type.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The created string comparer, or null if it could not be created.
        /// </returns>
        public static IComparer<string> GetComparer(
            Interpreter interpreter,
            string value,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (value == null)
            {
                error = "invalid comparer value";
                return null;
            }

            if (value.Length == 0)
                return Comparer<string>.Default;

            Type type = null;
            ResultList errors = null;

            if (Value.GetAnyType(null,
                    value, null, null, Value.GetTypeValueFlags(
                    false, false, false), cultureInfo, ref type,
                    ref errors) != ReturnCode.Ok)
            {
                error = errors;
                return null;
            }

            if (type == null)
            {
                error = "invalid comparer type";
                return null;
            }

            if (!HasGenericInterface(
                    type, typeof(IComparer<string>).Name,
                    new Type[] { typeof(string) }, ref error))
            {
                return null;
            }

            try
            {
                object @object = Activator.CreateInstance(type);

                if (@object == null)
                {
                    error = "could not create string comparer";
                    return null;
                }

                if (!(@object is IComparer<string>))
                {
                    error = "object is not a string comparer";
                    return null;
                }

                return @object as IComparer<string>;
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces all occurrences of the text replacement token
        /// within the specified pattern with the specified text, applying any
        /// quoting or list formatting indicated by the match mode.
        /// </summary>
        /// <param name="pattern">
        /// The pattern string that may contain the text replacement token.
        /// </param>
        /// <param name="text">
        /// The text to substitute for the replacement token, or null to remove
        /// the token.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how the substituted text should
        /// be formatted (e.g. raw, quoted, or as a list element).
        /// </param>
        /// <returns>
        /// The pattern string with the text replacement token replaced, or the
        /// original pattern if there was nothing to replace.
        /// </returns>
        private static string ReplaceMatchText(
            string pattern,
            string text,
            MatchMode mode
            )
        {
            if (!String.IsNullOrEmpty(pattern))
            {
                string token = TextReplacementToken;

                if (!String.IsNullOrEmpty(token))
                {
                    if (text == null)
                        return pattern.Replace(token, null);

                    mode &= MatchMode.TextTokenFlagsMask;

                    switch (mode)
                    {
                        case MatchMode.TextTokenRaw:
                            {
                                return pattern.Replace(token, text);
                            }
                        case MatchMode.TextTokenQuote:
                            {
                                return pattern.Replace(
                                    token, Parser.Quote(text));
                            }
                        case MatchMode.TextTokenList:
                            {
                                return pattern.Replace(
                                    token, StringList.MakeList(text));
                            }
                    }
                }
            }

            return pattern;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified pattern, using the specified match mode and case
        /// sensitivity.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how the pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match the text against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <returns>
        /// True if the text matches the pattern; otherwise, false.
        /// </returns>
        public static bool Match(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase
            )
        {
            return Match(interpreter, mode, text, pattern, noCase, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified pattern, using the specified match mode, case sensitivity,
        /// and string comparer.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how the pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match the text against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="comparer">
        /// The string comparer to use when matching, or null to use the default
        /// comparison behavior.
        /// </param>
        /// <returns>
        /// True if the text matches the pattern; otherwise, false.
        /// </returns>
        private static bool Match(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase,
            IComparer<string> comparer
            )
        {
            RegexOptions regExOptions = noCase ?
                RegexOptions.IgnoreCase : RegexOptions.None;

            return Match(
                interpreter, mode, text, pattern, noCase, comparer,
                regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified pattern, using the specified match mode, case sensitivity,
        /// string comparer, and regular expression options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how the pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match the text against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="comparer">
        /// The string comparer to use when matching, or null to use the default
        /// comparison behavior.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the match mode specifies
        /// regular expression matching.
        /// </param>
        /// <returns>
        /// True if the text matches the pattern; otherwise, false.
        /// </returns>
        public static bool Match(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase,
            IComparer<string> comparer,
            RegexOptions regExOptions
            )
        {
            bool match = false;
            Result error = null;

            if (Match(
                    interpreter, mode, text, pattern, noCase, comparer,
                    regExOptions, ref match, ref error) == ReturnCode.Ok)
            {
                return match;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified pattern, using the specified match mode and case
        /// sensitivity, returning detailed error information on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how the pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match the text against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the text matched the pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode Match(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase,
            ref bool match,
            ref Result error
            )
        {
            RegexOptions regExOptions = noCase ?
                RegexOptions.IgnoreCase : RegexOptions.None;

            return Match(
                interpreter, mode, text, pattern, noCase, null, regExOptions,
                ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches the
        /// specified pattern, using the specified match mode, case sensitivity,
        /// string comparer, and regular expression options, returning detailed
        /// error information on failure.  When the match mode specifies
        /// sub-pattern handling, the pattern may contain multiple sub-patterns
        /// that are matched in an OR-wise fashion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how the pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match the text against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="comparer">
        /// The string comparer to use when matching, or null to use the default
        /// comparison behavior.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the match mode specifies
        /// regular expression matching.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the text matched the pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode Match(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase,
            IComparer<string> comparer,
            RegexOptions regExOptions,
            ref bool match,
            ref Result error
            )
        {
            if (FlagOps.HasFlags(mode, MatchMode.SubPattern, true))
            {
                //
                // NOTE: There may be any number of "sub-patterns" to match
                //       against in an OR-wise fashion.
                //
                bool empty = FlagOps.HasFlags(
                    mode, MatchMode.EmptySubPattern, true);

                StringList subPatterns = null;

                if (SplitSubPatterns(
                        pattern, 0, empty, ref subPatterns,
                        ref error) == ReturnCode.Ok)
                {
                    if (subPatterns != null)
                    {
                        foreach (string subPattern in subPatterns)
                        {
                            if (Match( /* RECURSION */
                                    interpreter, mode, text, subPattern,
                                    noCase, comparer, regExOptions,
                                    ref match, ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            if (match)
                                break;
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: There is exactly one pattern to match against.
                        //
                        return MatchCore(
                            interpreter, mode, text, pattern, noCase, comparer,
                            regExOptions, ref match, ref error);
                    }
                }
                else
                {
                    //
                    // NOTE: Unable to split the sub-patterns.
                    //
                    return ReturnCode.Error;
                }
            }
            else
            {
                //
                // NOTE: There is exactly one pattern to match against.
                //
                return MatchCore(
                    interpreter, mode, text, pattern, noCase, comparer,
                    regExOptions, ref match, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs the core matching of a single pattern against
        /// the specified text, dispatching to the appropriate matching strategy
        /// (e.g. exact, sub-string, glob, regular expression, numeric, or
        /// script-based) based on the specified match mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine which matching strategy is used.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match the text against.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.  This may be overridden by certain match mode
        /// flags.
        /// </param>
        /// <param name="comparer">
        /// The string comparer to use when matching, or null to use the default
        /// comparison behavior.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the match mode specifies
        /// regular expression matching.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the text matched the pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode MatchCore(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            bool noCase,
            IComparer<string> comparer,
            RegexOptions regExOptions,
            ref bool match,
            ref Result error
            )
        {
            Result localResult; /* REUSED */

            if (FlagOps.HasFlags(mode, MatchMode.ForceCase, true))
                noCase = false;
            else if (FlagOps.HasFlags(mode, MatchMode.NoCase, true))
                noCase = true;

            if (FlagOps.HasFlags(mode, MatchMode.TextToken, true))
                pattern = ReplaceMatchText(pattern, text, mode);

            if (FlagOps.HasFlags(mode, MatchMode.Callback, false))
            {
                MatchCallback matchCallback = (interpreter != null) ?
                    interpreter.InternalMatchCallback : null;

                if (matchCallback != null)
                {
                    return matchCallback(
                        interpreter, mode, text, pattern, new ClientData(
                        new ObjectList(noCase, comparer, regExOptions)),
                        ref match, ref error);
                }
                else
                {
                    error = "invalid match callback";
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Exact, false))
            {
                if (comparer != null)
                {
                    //
                    // NOTE: This might be case-sensitive.  It depends on the
                    //       exact implementation of the IComparer.
                    //
                    if (comparer.Compare(text, pattern) == 0 /* EQUALS */)
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                    }
                }
                else
                {
                    //
                    // NOTE: Exact matching using the String.Compare method
                    //       (may be case-sensitive, depending on the noCase
                    //       parameter value).
                    //
                    StringComparison comparisonType = GetComparisonType(
                        mode, noCase);

                    if (SharedStringOps.Equals(text, pattern, comparisonType))
                        match = true;
                    else
                        match = false;
                }

                return ReturnCode.Ok;
            }
            else if (FlagOps.HasFlags(mode, MatchMode.SubString, false))
            {
                //
                // NOTE: Prefix matching using the String.Compare method
                //       (may be case-sensitive, depending on the noCase
                //       parameter value).
                //
                int length = (pattern != null) ? pattern.Length : 0;

                StringComparison comparisonType = GetComparisonType(
                    mode, noCase);

                if (SharedStringOps.Equals(
                        text, 0, pattern, 0, length, comparisonType))
                {
                    match = true;
                }
                else
                {
                    match = false;
                }

                return ReturnCode.Ok;
            }
#if NETWORK
            else if (FlagOps.HasFlags(mode, MatchMode.CIDR, false))
            {
                IpFlags ipFlags = IpFlags.Default;

                if (FlagOps.HasFlags(
                        mode, MatchMode.StopOnError, false))
                {
                    ipFlags |= IpFlags.StopOnError;
                }

                bool? maybeMatch; /* REUSED */

                if (FlagOps.HasFlags(mode, MatchMode.ListPattern, false))
                {
                    StringList listValue = null;

                    if (ParserOps<string>.SplitList(
                            interpreter, pattern, 0, Length.Invalid, true,
                            ref listValue, ref error) == ReturnCode.Ok)
                    {
                        maybeMatch = SocketOps.MatchViaCIDR(
                            text, listValue, ipFlags, ref error);

                        if (maybeMatch != null)
                        {
                            match = (bool)maybeMatch;
                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    maybeMatch = SocketOps.MatchViaCIDR(
                        text, pattern, ipFlags, ref error);

                    if (maybeMatch != null)
                    {
                        match = (bool)maybeMatch;
                        return ReturnCode.Ok;
                    }
                }
            }
#endif
            else if (FlagOps.HasFlags(mode, MatchMode.Glob, false))
            {
                //
                // NOTE: Glob matching using the StringMatch method (may be
                //       case-sensitive, depending on the noCase parameter
                //       value).
                //
                if (Parser.StringMatch(
                        interpreter, text, 0, pattern, 0, noCase))
                {
                    match = true;
                }
                else
                {
                    match = false;
                }

                return ReturnCode.Ok;
            }
            else if (FlagOps.HasFlags(mode, MatchMode.RegExp, false))
            {
                try
                {
                    if ((text != null) && (pattern != null) &&
                        Regex.IsMatch(text, pattern, regExOptions))
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                    }

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Integer, false))
            {
                int textIntValue = 0;
                int patternIntValue = 0;

                if ((Value.GetInteger2(
                        text, ValueFlags.AnyInteger, null,
                        ref textIntValue, ref error) == ReturnCode.Ok) &&
                    (Value.GetInteger2(
                        pattern, ValueFlags.AnyInteger, null,
                        ref patternIntValue, ref error) == ReturnCode.Ok))
                {
                    if (textIntValue == patternIntValue)
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                    }

                    return ReturnCode.Ok;
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Decimal, false))
            {
                decimal textDecValue = decimal.Zero;
                decimal patternDecValue = decimal.Zero;

                if ((Value.GetDecimal(
                        text, ValueFlags.AnyDecimal, null,
                        ref textDecValue, ref error) == ReturnCode.Ok) &&
                    (Value.GetDecimal(
                        pattern, ValueFlags.AnyDecimal, null,
                        ref patternDecValue, ref error) == ReturnCode.Ok))
                {
                    if (MathOps.AboutEquals(textDecValue, patternDecValue))
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                    }

                    return ReturnCode.Ok;
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Double, false))
            {
                double textDblValue = 0.0;
                double patternDblValue = 0.0;

                if ((Value.GetDouble(
                        text, ValueFlags.AnyDouble, null,
                        ref textDblValue, ref error) == ReturnCode.Ok) &&
                    (Value.GetDouble(
                        pattern, ValueFlags.AnyDouble, null,
                        ref patternDblValue, ref error) == ReturnCode.Ok))
                {
                    if (MathOps.AboutEquals(textDblValue, patternDblValue))
                    {
                        match = true;
                    }
                    else
                    {
                        match = false;
                    }

                    return ReturnCode.Ok;
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Evaluate, false))
            {
                if (interpreter != null)
                {
                    localResult = null;

                    if (interpreter.EvaluateScript(
                            pattern, ref localResult) == ReturnCode.Ok)
                    {
                        if (Value.GetBoolean6(
                                localResult, ValueFlags.AnyBoolean,
                                interpreter.InternalCultureInfo,
                                ref match, ref error) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        error = localResult;
                    }
                }
                else
                {
                    error = "invalid interpreter";
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Expression, false))
            {
                if (interpreter != null)
                {
                    localResult = null;

                    if (interpreter.EvaluateExpression(
                            pattern, ref localResult) == ReturnCode.Ok)
                    {
                        if (Value.GetBoolean6(
                                localResult, ValueFlags.AnyBoolean,
                                interpreter.InternalCultureInfo,
                                ref match, ref error) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        error = localResult;
                    }
                }
                else
                {
                    error = "invalid interpreter";
                }
            }
            else if (FlagOps.HasFlags(mode, MatchMode.Substitute, false))
            {
                if (interpreter != null)
                {
                    localResult = null;

                    if (interpreter.SubstituteString(
                            pattern, ref localResult) == ReturnCode.Ok)
                    {
                        if (Value.GetBoolean6(
                                localResult, ValueFlags.AnyBoolean,
                                interpreter.InternalCultureInfo,
                                ref match, ref error) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        error = localResult;
                    }
                }
                else
                {
                    error = "invalid interpreter";
                }
            }
            else
            {
                error = "cannot match, no supported mode found";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches any or all
        /// of the specified patterns, using the specified match mode and case
        /// sensitivity.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how each pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns to match the text against.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that the text match all of the patterns; zero to
        /// require that the text match any of the patterns.  This may be
        /// overridden by certain match mode flags.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <returns>
        /// True if the text matches the patterns according to the requested
        /// any-or-all semantics; otherwise, false.
        /// </returns>
        public static bool MatchAnyOrAll(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            IEnumerable<string> patterns,
            bool all,
            bool noCase
            )
        {
            bool match = false;
            Result error = null;

            if (MatchAnyOrAll(
                    interpreter, mode, text, patterns, all, noCase,
                    ref match, ref error) == ReturnCode.Ok)
            {
                return match;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches any or all
        /// of the specified patterns, using the specified match mode and case
        /// sensitivity.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how each pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns to match the text against.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that the text match all of the patterns; zero to
        /// require that the text match any of the patterns.  This may be
        /// overridden by certain match mode flags.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the text matched the patterns
        /// according to the requested any-or-all semantics.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode MatchAnyOrAll(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            IEnumerable<string> patterns,
            bool all,
            bool noCase,
            ref bool match
            )
        {
            Result error = null;

            return MatchAnyOrAll(
                interpreter, mode, text, patterns, all, noCase, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches any or all
        /// of the specified patterns, using the specified match mode and case
        /// sensitivity, returning detailed error information on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how each pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns to match the text against.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that the text match all of the patterns; zero to
        /// require that the text match any of the patterns.  This may be
        /// overridden by certain match mode flags.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the text matched the patterns
        /// according to the requested any-or-all semantics.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode MatchAnyOrAll(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            IEnumerable<string> patterns,
            bool all,
            bool noCase,
            ref bool match,
            ref Result error
            )
        {
            RegexOptions regExOptions = noCase ?
                RegexOptions.IgnoreCase : RegexOptions.None;

            return MatchAnyOrAll(
                interpreter, mode, text, patterns, all, noCase, null,
                regExOptions, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text matches any or all
        /// of the specified patterns, using the specified match mode, case
        /// sensitivity, string comparer, and regular expression options,
        /// returning detailed error information on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any, associated with this operation.
        /// </param>
        /// <param name="mode">
        /// The match mode flags that determine how each pattern is interpreted.
        /// </param>
        /// <param name="text">
        /// The text to be matched against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of patterns to match the text against.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that the text match all of the patterns; zero to
        /// require that the text match any of the patterns.  This may be
        /// overridden by certain match mode flags.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match; otherwise, the match
        /// is case-sensitive.
        /// </param>
        /// <param name="comparer">
        /// The string comparer to use when matching, or null to use the default
        /// comparison behavior.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the match mode specifies
        /// regular expression matching.
        /// </param>
        /// <param name="match">
        /// Upon success, this will be non-zero if the text matched the patterns
        /// according to the requested any-or-all semantics.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode MatchAnyOrAll(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            IEnumerable<string> patterns,
            bool all,
            bool noCase,
            IComparer<string> comparer,
            RegexOptions regExOptions,
            ref bool match,
            ref Result error
            )
        {
            try
            {
                if (patterns != null)
                {
                    if (FlagOps.HasFlags(
                            mode, MatchMode.Any, true))
                    {
                        all = false;
                    }
                    else if (FlagOps.HasFlags(
                            mode, MatchMode.All, true))
                    {
                        all = true;
                    }

                    bool localMatch = false;
                    ReturnCode code = ReturnCode.Ok;

                    foreach (string pattern in patterns)
                    {
                        code = Match(
                            interpreter, mode, text, pattern,
                            noCase, comparer, regExOptions,
                            ref localMatch, ref error);

                        if (code != ReturnCode.Ok)
                            break;

                        if (!all && localMatch)
                            break;
                        else if (all && !localMatch)
                            break;
                    }

                    if (code == ReturnCode.Ok)
                        match = localMatch;

                    return code;
                }
                else
                {
                    error = "invalid pattern list";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
    }
}
