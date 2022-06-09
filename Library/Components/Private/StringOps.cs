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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("517405a1-bb12-4694-b937-30cb46b7c263")]
    internal static class StringOps
    {
        #region String Comparison Type Constants
        private static readonly StringComparison UserComparisonType =
            StringComparison.CurrentCulture;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly StringComparison UserNoCaseComparisonType =
            StringComparison.CurrentCultureIgnoreCase;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly StringComparer DefaultStringComparer =
            StringComparer.CurrentCulture;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Encoding Constants
        private const int CharMaxAscii = 0x7F;

        internal static readonly string NullEncodingName = _String.Null;
        internal static readonly string BinaryEncodingName = "binary";

        internal static readonly string ChannelEncodingName = "channelDefault";
        internal static readonly string DefaultEncodingName = "default";
        internal static readonly string SystemEncodingName = "systemDefault";
        internal static readonly string TclEncodingName = "tclDefault";
        internal static readonly string TextEncodingName = "textDefault";
        internal static readonly string ScriptEncodingName = "scriptDefault";
        internal static readonly string XmlEncodingName = "xmlDefault";

        private static readonly Encoding SystemEncoding = new UnicodeEncoding(false, false);

        //
        // WARNING: For use by the [encoding system] sub-command only.
        //
        internal static readonly string SystemEncodingWebName = (SystemEncoding != null) ?
            SystemEncoding.WebName : null;

        private static readonly Encoding DefaultEncoding = CoreUtf8Encoding.CoreUtf8;

        private static readonly Encoding XmlEncoding = CoreUtf8Encoding.CoreUtf8;

        private static readonly Encoding BinaryEncoding = OneByteEncoding.OneByte;

        private static readonly Encoding TextEncoding = DefaultEncoding;

        //
        // NOTE: This encoding appears to be functionally identical to the Tcl
        //       encoding "cp1252", which is their default channel encoding on
        //       Windows.
        //
        private static readonly Encoding ChannelEncoding = GetEncoding(
            "iso-8859-1");

        private static readonly Encoding TclEncoding = ChannelEncoding;
        private static readonly Encoding ScriptEncoding = TclEncoding;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Match Modes & Regular Expression Constants
        //
        // HACK: This is purposely not read-only.
        //
        private static string TextReplacementToken = "%text%";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly MatchMode DefaultMapMatchMode = MatchMode.Exact; // COMPAT: Tcl.

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly MatchMode DefaultMatchMode = MatchMode.Glob; // COMPAT: Tcl.
        internal static readonly MatchMode DefaultSwitchMatchMode = MatchMode.Exact; // COMPAT: Tcl.
        internal static readonly MatchMode DefaultResultMatchMode = MatchMode.Exact; // COMPAT: Tcl.

        internal static readonly MatchMode DefaultObjectMatchMode = DefaultMatchMode;
        internal static readonly MatchMode DefaultUnloadMatchMode = MatchMode.Exact;

#if SHELL && INTERACTIVE_COMMANDS && XML
        private static readonly Regex TwoOrMoreWhiteSpaceRegEx = RegExOps.Create("\\s{2,}");
#endif

        internal static readonly RegexOptions DefaultRegExOptions = RegexOptions.None;
        internal static readonly RegexOptions DefaultRegExTestOptions = RegexOptions.Singleline; /* COMPAT: Tcl. */
        internal static readonly RegexOptions DefaultRegExSyntaxOptions = RegexOptions.Singleline; /* COMPAT: Tcl. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region [format] Command Constants
        //
        // HACK: This is purposely not read-only.
        //
        private static int DoubleDefaultPrecision = 6; /* For 'E' / 'e', 'F' / 'f', and 'G' / 'g'. */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static int MinimumExponentLength = 2;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static char[] ExponentPrefixChars = {
            Characters.E, Characters.e
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string mixedXpgError = "cannot mix \"%\" and \"%n$\" conversion specifiers";
        private const string OverflowError = "max size for a Tcl value exceeded";

        private const string BinaryPrefix = "0b";
        private const string DecimalPrefix = "0d";
        private const string LegacyOctalPrefix = "0";
        private const string OctalPrefix = "0o";
        private const string HexadecimalPrefix = "0x";

        private static readonly string[] BadIndexError = {
            "not enough arguments for all format specifiers",
            "\"%n$\" argument index out of range"
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Character List Constants
        private static readonly char[] switchChars = {
            Characters.MinusSign, Characters.Slash
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char[] SubPatternChars = {
            Characters.OpenBrace, Characters.CloseBrace
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Excludes characters covered by PathOps.HasPathWildcard().
        //
        private static readonly char[] StringMatchWildcardChars = {
            Characters.OpenBracket,
            Characters.Backslash,
            Characters.CloseBracket
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static readonly CharList switchCharList = new CharList(switchChars);
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Radix Regular Expressions
        private const int Base26GroupsPerLine = 25;

        private static readonly Regex base26RegEx = RegExOps.Create(
            "^[A-Z\\s]*$", RegexOptions.IgnoreCase);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Regex base64RegEx = RegExOps.Create(
            "^[0-9A-Z+/\\r\\n]*={0,2}$", RegexOptions.IgnoreCase);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region StringBuilder Sizing Constants
        //
        // HACK: Calculate the number of bytes that all CLR objects require,
        //       regardless of any other data (fields) that they may contain.
        //
        //       General equation (based on various Internet sources):
        //
        //       SyncBlock (DWORD) + MethodTable (PTR)
        //
        //       Since, by all reports, the initial DWORD is padded for the
        //       64-bit runtime, just use the size of two IntPtr objects.
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        private static int ObjectOverhead = (2 * IntPtr.Size); /* 8 or 16 */

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: Calculate the number of bytes that all CLR String objects
        //       require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       CharLength (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        private static int StringOverhead = sizeof(uint); /* 4 */
#else
        //
        // HACK: Calculate the number of bytes that all CLR String objects
        //       require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       ByteLength (DWORD) + CharLength (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        private static int StringOverhead = (2 * sizeof(uint)); /* 8 */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: Calculate the number of bytes that all CLR StringBuilder
        //       objects require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       ChunkChars (OBJPTR) + ChunkPrevious (OBJPTR) +
        //       ChunkLength (DWORD) + ChunkOffset (DWORD) +
        //       MaxCapacity (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        private static int StringBuilderOverhead =
            (2 * IntPtr.Size) + (3 * sizeof(uint)); /* 20 or 28 */
#else
        //
        // HACK: Calculate the number of bytes that all CLR StringBuilder
        //       objects require, regardless of their actual length.
        //
        //       General equation (based on various Internet sources):
        //
        //       Thread (PTR) + String (OBJPTR) + MaxCapacity (DWORD)
        //
        //       Given the nature of the CLR, this number is approximate, at
        //       best (and will likely be wrong in subsequent versions).
        //
        private static int StringBuilderOverhead =
            (2 * IntPtr.Size) + (1 * sizeof(uint)); /* 12 or 20 */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: How many StringBuilder objects do we want to try and fit on a
        //       single page in memory.  Given the nature of the CLR, this is
        //       approximate, at best (and will likely be wrong in subsequent
        //       versions).
        //
#if NET_40
        private static int StringBuildersPerPage = 28;
#else
        private static int StringBuildersPerPage = 32;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: This is the default initial capacity for the StringBuilder
        //         objects that we create.  Changing this value can have a
        //         significant impact on the performance of the entire library;
        //         therefore, we should try to figure out the "optimal" value
        //         for it.  Unfortunately, so far, no value has proven to be
        //         ideal in all circumstances; therefore, this field has been
        //         changed from read-only to read-write so that it can be
        //         overridden at runtime [via reflection] as a last resort.
        //
        private static int DefaultCapacity = GetStringBuilderDefaultCapacity();
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This list maps the byte-order-mark sequences to their corresponding
        //       encodings.  Linear searching is used for detection; therefore, this
        //       list should be kept very small.
        //
        private static IList<IAnyPair<byte[], Encoding>> preambleEncodings = null;

        //
        // NOTE: This is the minimum number of bytes needed for the byte-order-mark
        //       sequences handled by this class.
        //
        private static int minimumPreambleSize = 0;

        //
        // NOTE: This is the maximum number of bytes needed for the byte-order-mark
        //       sequences handled by this class.
        //
        private static int maximumPreambleSize = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool IsLogicallyEmpty(
            string value
            )
        {
            int length;

            return IsLogicallyEmpty(value, out length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsLogicallyEmpty(
            string value,
            out int length
            )
        {
            int localLength;

            if (IsNullOrEmpty(value, out localLength))
            {
                length = localLength;
                return true;
            }

            string trimValue = value.Trim();

            if (IsNullOrEmpty(trimValue, out localLength))
            {
                length = localLength;
                return true;
            }

            length = localLength;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int GetStringBuilderDefaultCapacity()
        {
            return ((int)PlatformOps.GetPageSize() - (StringBuildersPerPage *
                (ObjectOverhead + StringOverhead + StringBuilderOverhead))) /
                (sizeof(char) * StringBuildersPerPage);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static string GetStringFromObject(
            object @object
            )
        {
            return GetStringFromObject(@object, null, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static Argument GetArgumentFromObject(
            object @object
            )
        {
            return GetArgumentFromObject(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static Result GetResultFromObject(
            object @object
            )
        {
            return GetResultFromObject(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static StringBuilder GetStringBuilder(
            IHaveStringBuilder haveStringBuilder
            )
        {
            if (haveStringBuilder == null)
                return null;

            return haveStringBuilder.Builder;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IHaveStringBuilder NewIHaveStringBuilder()
        {
            return NewIHaveStringBuilder(NewStringBuilder());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IHaveStringBuilder NewIHaveStringBuilder(
            StringBuilder builder
            )
        {
            return new StringBuilderWrapper(builder);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                return NewIHaveStringBuilder(NewStringBuilder((string)@object));

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
                return NewStringBuilder((string)@object);

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

            return create ? NewStringBuilder() : null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder()
        {
            return NewStringBuilder((string)null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder(
            int capacity
            )
        {
            return NewStringBuilder((StringBuilder)null, capacity);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder(
            string value
            )
        {
            return NewStringBuilder(value, DefaultCapacity);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder(
            string value,
            int capacity
            )
        {
            return NewStringBuilder(null, value, Index.Invalid, Length.Invalid, capacity);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static StringBuilder NewStringBuilder(
            StringBuilder result
            )
        {
            return NewStringBuilder(result, null, Index.Invalid, Length.Invalid);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder CopyStringBuilder(
            StringBuilder value
            )
        {
            if (value == null)
                return null;

            StringBuilder result = new StringBuilder(value.Length);

            result.Append(value);

            return result;

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder(
            StringBuilder result,
            int capacity
            )
        {
            if (capacity < DefaultCapacity)
                capacity = DefaultCapacity;

            if (result == null)
                return new StringBuilder(capacity);

            result.Length = 0;
            result.EnsureCapacity(capacity);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder(
            string value,
            int startIndex,
            int length
            )
        {
            return NewStringBuilder(null, value, startIndex, length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringBuilder NewStringBuilder(
            StringBuilder result,
            string value,
            int startIndex,
            int length
            )
        {
            return NewStringBuilder(result, value, startIndex, length, DefaultCapacity);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringBuilder NewStringBuilder(
            StringBuilder result,
            string value,
            int startIndex,
            int length,
            int capacity
            )
        {
            if (value == null)
            {
                if (length != Length.Invalid)
                    capacity = Math.Max(length, capacity);

                if (capacity < DefaultCapacity)
                    capacity = DefaultCapacity;

                if (result == null)
                    return new StringBuilder(capacity);

                result.Length = 0;
                result.EnsureCapacity(capacity);

                return result;
            }

            if ((startIndex != Index.Invalid) && (length != Length.Invalid))
            {
                capacity = Math.Max(length, capacity);

                if (capacity < DefaultCapacity)
                    capacity = DefaultCapacity;

                if (result == null)
                    return new StringBuilder(value, startIndex, length, capacity);

                result.Length = 0;
                result.EnsureCapacity(capacity);
                result.Append(value, startIndex, length);

                return result;
            }
            else
            {
                capacity = Math.Max(value.Length, capacity);

                if (capacity < DefaultCapacity)
                    capacity = DefaultCapacity;

                if (result == null)
                    return new StringBuilder(value, capacity);

                result.Length = 0;
                result.EnsureCapacity(capacity);
                result.Append(value);

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string[] SplitLines(
            string value,
            bool empty
            )
        {
            if (value == null)
                return null;

            StringList lines = new StringList();
            int length = value.Length;
            StringBuilder line = NewStringBuilder(length);

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
            {
                lines.Add(line);
                line.Length = 0;
            }

            return lines.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            StringBuilder builder = NewStringBuilder(length);
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

            value = builder.ToString();
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            StringBuilder builder = NewStringBuilder(length);
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

            value = builder.ToString();
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static Encoding GuessEncoding(
            byte[] bytes
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

                    if (ArrayOps.Equals(bytes, preamble, preamble.Length))
                        return anyPair.Y;
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Encoding GuessOrGetEncoding(
            byte[] bytes,
            EncodingType type
            )
        {
            Encoding encoding = GuessEncoding(bytes);

            if (encoding != null)
                return encoding;

            return GetEncoding(type);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Encoding GetEncoding(
            string name
            )
        {
            Result error = null;

            return GetEncoding(name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static Encoding GetEncoding(
            EncodingType type
            )
        {
            switch (type)
            {
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
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsDefaultEncodingName(
            string name /* in */
            )
        {
            return String.IsNullOrEmpty(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            if (encoding == null)
            {
                error = "invalid encoding";
                return ReturnCode.Error;
            }

            try
            {
                bytes = encoding.GetBytes(value);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            if (encoding == null)
            {
                error = "invalid encoding";
                return ReturnCode.Error;
            }

            try
            {
                value = encoding.GetString(bytes);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool UserEquals(
            string left,
            string right
            )
        {
            return SharedStringOps.Equals(
                left, right, UserComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool UserNoCaseEquals(
            string left,
            string right
            )
        {
            return SharedStringOps.Equals(
                left, right, UserNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool CharIsWord(
            char character
            )
        {
            return Char.IsLetterOrDigit(character) ||
                (Char.GetUnicodeCategory(character) == UnicodeCategory.ConnectorPunctuation);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsAsciiDigit(
            char character
            )
        {
            return (character >= Characters.Zero) && (character <= Characters.Nine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsAsciiAlpha(
            char character
            )
        {
            return ((character >= Characters.A) && (character <= Characters.Z)) ||
                ((character >= Characters.a) && (character <= Characters.z));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsAsciiAlphaOrDigit(
            char character
            )
        {
            return CharIsAsciiAlpha(character) || CharIsAsciiDigit(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsAscii(
            char character
            )
        {
            return character <= CharMaxAscii;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsGraph(
            char character
            )
        {
            return CharIsPrint(character) && !Char.IsWhiteSpace(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool CharIsAsciiHexadecimal(
            char character
            )
        {
            return CharIsAsciiDigit(character) ||
                ((character >= Characters.A) && (character <= Characters.F)) ||
                ((character >= Characters.a) && (character <= Characters.f));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsIdentifierZero( /* NOTE: First C# identifier character. */
            char character
            )
        {
            return (character == Characters.Underscore) || Char.IsLetter(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CharIsIdentifierOnePlus( /* NOTE: Subsequent C# identifier characters. */
            char character
            )
        {
            return (character == Characters.Underscore) || Char.IsLetterOrDigit(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static string ToBase26String(
            byte[] array,
            Base26FormattingOption options /* IGNORED */
            )
        {
            if (array != null)
            {
                int length = array.Length;
                StringBuilder result = NewStringBuilder(length * 2);

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

                return result.ToString();
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsMultiLine(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            return (value.IndexOfAny(
                Characters.LineTerminatorChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static bool IsBase64(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text) && (base64RegEx != null))
            {
                Match match = base64RegEx.Match(text.Trim());

                if ((match != null) && match.Success)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetBytesFromString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (IsBase64(value))
            {
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
                return ArrayOps.GetBytesFromString(
                    value, cultureInfo, ref bytes, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NormalizeListSeparators(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = NewStringBuilder(value);

            builder.Replace(Characters.Comma, Characters.Space);
            builder.Replace(Characters.SemiColon, Characters.Space);

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
            StringBuilder builder = NewStringBuilder(text);

            //
            // NOTE: Using the created string builder, modify it in-place
            //       to normalize all line-endings to the convention that
            //       is required by the script evaluation engine.
            //
            FixupLineEndings(builder);

            //
            // NOTE: Return the resulting string to the caller.
            //
            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static void FixupDisplayLineEndings(
            StringBuilder builder,
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
            else
            {
                //
                // NOTE: Otherwise (non-Unicode), just use the pilcrow
                //       character to replace each line-ending character.
                //
                builder.Replace(
                    Characters.LineFeed, Characters.SectionSign);

                builder.Replace(
                    Characters.CarriageReturn, Characters.PilcrowSign);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static void StripNewLine(
            ref string value /* in, out */
            )
        {
            StringComparison comparisonType =
                SharedStringOps.SystemComparisonType;

            if (!String.IsNullOrEmpty(value) &&
                (Environment.NewLine != null) && value.EndsWith(
                    Environment.NewLine, comparisonType))
            {
                value = value.Substring(0,
                    value.Length - Environment.NewLine.Length);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                        Characters.Comma.ToString() + Characters.Space.ToString(),
                        null, false);

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
        private static bool CharIsSwitch( /* NOT USED */
            char character
            )
        {
            return (switchCharList != null) && switchCharList.Contains(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static bool CharIsDigit( /* NOT USED */
            char character
            )
        {
            return Char.GetUnicodeCategory(character) == UnicodeCategory.DecimalDigitNumber;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static string StrReplace(
            string text,
            string oldValue,
            string newValue,
            StringComparison comparisonType,
            int maximum,
            ref int count
            ) /* NOT USED */
        {
            StringBuilder result = NewStringBuilder();

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

            return result.ToString();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
            bool evaluate = FlagOps.HasFlags(
                mode, MatchMode.Evaluate, true);

            mode &= ~MatchMode.FlagsMask;

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
                            oldLength = patternLength;
                            oldValue = pattern;
                            newValue = replacement;

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
                                oldLength = replacementLength;

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

                                oldValue = text.Substring(
                                    startIndex, matchIndex + 1);

                                newValue = replacement;

                                if (append &&
                                    (matchIndex > startIndex))
                                {
                                    if (builder == null)
                                        builder = NewStringBuilder();

                                    builder.Append(
                                        text, startIndex,
                                        matchIndex - startIndex);
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
            StringBuilder builder = NewStringBuilder(text);

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
                                    subSpec, true, true,
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

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
            StringBuilder builder = NewStringBuilder(length);

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
                        false, true, ref oldLength, ref oldValue,
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

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static string StrRepeat(
            int count,
            char character
            )
        {
            if (count <= 0)
                return String.Empty;

            StringBuilder result = NewStringBuilder();

            result.EnsureCapacity(count);
            result.Append(character, count);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string StrRepeat(
            int count,
            string text
            )
        {
            if (count <= 0)
                return String.Empty;

            StringBuilder result = NewStringBuilder();

            if (!String.IsNullOrEmpty(text))
            {
                result.EnsureCapacity(count * text.Length);

                while (count-- > 0)
                    result.Append(text);
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PadCenter(
            string text,
            int length,
            char character
            )
        {
            StringBuilder result = NewStringBuilder();

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

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public static string ToTitle(
            string text,
            CultureInfo cultureInfo
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
                    result = Char.ToUpper(firstCharacter,
                        cultureInfo) + secondToEnd.ToLower(
                        cultureInfo);
                }
                else
#endif
                {
                    result = Char.ToUpper(firstCharacter) +
                        secondToEnd.ToLower();
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ToLowerInitial(
            string text,
            CultureInfo cultureInfo
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
                    result = Char.ToLower(firstCharacter,
                        cultureInfo) + secondToEnd;
                }
                else
#endif
                {
                    result = Char.ToLower(firstCharacter) +
                        secondToEnd;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Excludes characters covered by PathOps.HasPathWildcard().
        //
        public static bool HasStringMatchWildcard(
            string value
            )
        {
            return (value != null) &&
                (StringMatchWildcardChars != null) &&
                (value.IndexOfAny(StringMatchWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasStringMatchChar(
            string text
            )
        {
            return (text != null) &&
                (text.IndexOfAny(Characters.StringMatchReservedChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string RemoveWhiteSpace(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringBuilder result = NewStringBuilder(text.Length);

                for (int index = 0; index < text.Length; index++)
                {
                    switch (text[index])
                    {
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

                return result.ToString();
            }
            else
            {
                return text;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS && XML
        public static string CollapseWhiteSpace(
            string text
            )
        {
            if (String.IsNullOrEmpty(text) ||
                (TwoOrMoreWhiteSpaceRegEx == null))
            {
                return text;
            }

            return TwoOrMoreWhiteSpaceRegEx.Replace(text,
                Characters.Space.ToString());
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        //
        // WARNING: This function must not change the length of the string.
        //          Each character that gets replaced must be replaced by
        //          another character, not a string.
        //
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
            StringBuilder builder = NewStringBuilder(text);

            //
            // NOTE: Using the created string builder, modify it in-place
            //       to normalize all line-endings to the convention that
            //       is required by the script evaluation engine.
            //
            FixupWhiteSpace(builder, fallback, flags);

            //
            // NOTE: Return the resulting string to the caller.
            //
            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

                                builder[index] = Characters.VisualHorizontalTab;
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
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !MONO && NATIVE && WINDOWS
        [MethodImpl(
            MethodImplOptions.NoInlining
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
            | MethodImplOptions.NoOptimization
#endif
        )]
        public static ReturnCode ZeroString(
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

            if (CommonOps.Runtime.IsMono())
            {
                noComplain = true;
                error = "not implemented";

                return ReturnCode.Error;
            }

            if (CommonOps.Runtime.IsDotNetCore3x() ||
                CommonOps.Runtime.IsDotNetCore5xOr6x())
            {
                noComplain = true;
                error = "not implemented";

                return ReturnCode.Error;
            }

            GCHandle handle = NativeOps.GetInvalidGCHandle();

            try
            {
                handle = GCHandle.Alloc(value, GCHandleType.Pinned);

                if (handle.IsAllocated)
                {
                    /* m_firstChar */
                    IntPtr pMemory = handle.AddrOfPinnedObject();

                    if (pMemory != IntPtr.Zero)
                    {
                        int length;

                        if (CommonOps.Runtime.IsFramework40())
                        {
                            /* m_stringLength */
                            length = Marshal.ReadInt32(pMemory,
                                -sizeof(int));
                        }
                        else
                        {
                            /* m_arrayLength */
                            length = Marshal.ReadInt32(pMemory,
                                -(sizeof(int) * 2));
                        }

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
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void AppendWithComma(
            string message,
            ref StringBuilder result
            )
        {
            if (result == null)
                result = NewStringBuilder();

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

            StringBuilder localResult = NewStringBuilder();

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

                segment = NewStringBuilder(arguments[argumentIndex]);

                if (character == Characters.i)
                    character = Characters.d;

                int intValue = 0;
                long longValue = 0;
                double doubleValue = 0;

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

                            segment = NewStringBuilder(ConversionOps.ToChar(code).ToString());
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

                            segment = NewStringBuilder();
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

                                        StringBuilder bytes = NewStringBuilder(numDigits);
                                        bytes.Length = numDigits;

                                        toAppend = length = (int)numDigits;

                                        while (numDigits-- > 0)
                                        {
                                            int digitOffset = (int)(bits % (ulong)radix);

                                            bytes[numDigits] = (digitOffset > 9) ?
                                                (char)(Characters.a + digitOffset - Parser.DecimalRadix) :
                                                (char)(Characters.Zero + digitOffset);

                                            bits /= (ulong)radix;
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
                                StringBuilder spec = NewStringBuilder();

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
                                segment = NewStringBuilder();

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
                                StringBuilder spec = NewStringBuilder();

                                spec.Append(Characters.OpenBrace);
                                spec.Append(Characters.Zero);
                                spec.Append(Characters.Colon);
                                spec.Append(character);

                                int usePrecision = gotPrecision ?
                                    precision : DoubleDefaultPrecision;

                                spec.Append(usePrecision);
                                spec.Append(Characters.CloseBrace);

                                segment = NewStringBuilder();

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

                                                segment = NewStringBuilder(
                                                    segmentString.Length);

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

                                                segment = NewStringBuilder(
                                                    segmentString);
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
                                            0, Characters.Space.ToString(),
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
                            segment = NewStringBuilder(
                                segment.ToString().ToUpper());

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
                    StringBuilder subPattern = NewStringBuilder();

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
                                subPattern : NewStringBuilder());

                            subPattern = NewStringBuilder();
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
                            subPattern : NewStringBuilder());

                        subPattern = NewStringBuilder();
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
                        subPattern = NewStringBuilder();

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

        public static StringComparison GetUserComparisonType(
            bool noCase
            )
        {
            return noCase ?
                UserNoCaseComparisonType : UserComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                    error = "invlid match callback";
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

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
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
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
