/*
 * ParseOps.cs --
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
using System.Runtime.InteropServices;
using System.Text;
using Eagle._Components.Shared;

namespace Eagle._Components.Private
{
    [Guid("c6fc40a1-3e57-4b9d-9eb4-caaaf889ce4d")]
    internal static class ParseOps
    {
        #region Private Constants
        private const string InvariantCultureName = "invariant"; // COMPAT: Eagle.
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Argument Parsing Methods
        public static byte[] HexString(
            string text
            )
        {
            if (String.IsNullOrEmpty(text))
                return null;

            if (text.Length % Characters.ByteHexChars != 0)
                return null;

            byte[] result = new byte[text.Length / Characters.ByteHexChars];

            for (int index = 0;
                    index < text.Length;
                    index += Characters.ByteHexChars)
            {
                if (!byte.TryParse(
                        text.Substring(index, Characters.ByteHexChars),
                        NumberStyles.HexNumber, null,
                        out result[index / Characters.ByteHexChars]))
                {
                    return null;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? Boolean(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                bool value;

                if (bool.TryParse(text, out value))
                    return value;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int? Integer(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                int value;

                if (int.TryParse(text, out value))
                    return value;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static object Enum(
            Type enumType,
            string text,
            bool noCase
            )
        {
            if ((enumType == null) || !enumType.IsEnum)
                return null;

            if (!String.IsNullOrEmpty(text))
            {
                try
                {
                    return System.Enum.Parse(enumType, text, noCase);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool NameAndBuildType(
            string text,
            bool strict,
            bool noCase,
            ref string name,
            ref BuildType? buildType
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string[] parts = text.Split(
                    new char[] { Characters.Underscore },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts != null)
                {
                    int length = parts.Length;

                    if (length < 1)
                    {
                        if (strict)
                            return false;

                        return true;
                    }

                    string localName = parts[0];

                    if (length < 2)
                    {
                        if (strict)
                            return false;

                        name = localName;
                        return true;
                    }

                    object enumValue = Enum(
                        typeof(BuildType), parts[1], noCase);

                    if (!(enumValue is BuildType))
                        return false;

                    BuildType localBuildType = (BuildType)enumValue;

                    name = localName;
                    buildType = localBuildType;

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static CultureInfo Culture(
            string text
            )
        {
            if (text != null) // NOTE: Empty string allowed.
            {
                try
                {
                    if (StringOps.SystemEquals(text, InvariantCultureName))
                        return CultureInfo.InvariantCulture;

                    return new CultureInfo(text);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version Version(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                try
                {
                    return new Version(text);
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static DateTime? DateTime(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                DateTime dateTime;

                if (System.DateTime.TryParse(text, out dateTime))
                    return dateTime;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri Uri(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                Uri uri;

                if (System.Uri.TryCreate(text, UriKind.Absolute, out uri))
                    return uri;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool BuildTypeAndReleaseType(
            string text,
            bool strict,
            bool noCase,
            ref BuildType buildType,
            ref ReleaseType releaseType
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string[] parts = text.Split(
                    new char[] { Characters.Comma, Characters.Space },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts != null)
                {
                    int length = parts.Length;

                    if (length < 1)
                    {
                        if (strict)
                            return false;

                        return true;
                    }

                    object enumValue = Enum(
                        typeof(BuildType), parts[0], noCase);

                    if (!(enumValue is BuildType))
                        return false;

                    BuildType localBuildType = (BuildType)enumValue;

                    if (length < 2)
                    {
                        if (strict)
                            return false;

                        buildType = localBuildType;
                        return true;
                    }

                    /* REUSED */
                    enumValue = Enum(
                        typeof(ReleaseType), parts[1], noCase);

                    if (!(enumValue is ReleaseType))
                        return false;

                    ReleaseType localReleaseType = (ReleaseType)enumValue;

                    buildType = localBuildType;
                    releaseType = localReleaseType;

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string Notes(
            string text
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringBuilder result = new StringBuilder(text);

                result = result.Replace(
                    Characters.EscapedHorizontalTab,
                    Characters.HorizontalTab.ToString());

                result = result.Replace(
                    Characters.EscapedVerticalTab,
                    Characters.VerticalTab.ToString());

                result = result.Replace(
                    Characters.EscapedLineFeed,
                    Characters.LineFeed.ToString());

                result = result.Replace(
                    Characters.EscapedCarriageReturn,
                    Characters.CarriageReturn.ToString());

                result = result.Replace(
                    Characters.EscapedAmpersand,
                    Characters.Ampersand.ToString());

                return result.ToString();
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Line Support Methods
        public static string[] CommandLine(
            string text
            )
        {
            if (text == null)
                return null;

            List<string> args = new List<string>();
            int length = text.Length;
            int index = 0;

            while (true)
            {
                if (index >= length)
                    break;

                while (Char.IsWhiteSpace(text[index]))
                    index++;

                if (index >= length)
                    break;

                StringBuilder arg = new StringBuilder();
                bool quote = false;

                while (true)
                {
                    bool copy = true;
                    int slashes = 0;

                    while ((index < length) &&
                        (text[index] == Characters.Backslash))
                    {
                        index++;
                        slashes++;
                    }

                    if ((index < length) &&
                        (text[index] == Characters.DoubleQuote))
                    {
                        if ((slashes % 2) == 0)
                        {
                            if (quote)
                            {
                                int nextIndex = index + 1;

                                if ((nextIndex < length) &&
                                    (text[nextIndex] == Characters.DoubleQuote))
                                {
                                    index++;
                                }
                                else
                                {
                                    copy = false;
                                }
                            }
                            else
                            {
                                copy = false;
                            }

                            quote = !quote;
                        }

                        slashes /= 2;
                    }

                    while (slashes-- > 0)
                        arg.Append(Characters.Backslash);

                    if (index >= text.Length)
                        break;

                    if (!quote && Char.IsWhiteSpace(text[index]))
                        break;

                    if (copy)
                        arg.Append(text[index]);

                    index++;
                }

                args.Add(arg.ToString());
            }

            return args.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CheckOption(
            ref string arg
            )
        {
            string result = arg;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: Remove all leading switch chars.
                //
                result = result.TrimStart(Characters.SwitchChars);

                //
                // NOTE: How many chars were removed?
                //
                int count = arg.Length - result.Length;

                //
                // NOTE: Was there at least one?
                //
                if (count > 0)
                {
                    //
                    // NOTE: Ok, replace their original
                    //       argument.
                    //
                    arg = result;

                    //
                    // NOTE: Yes, this is a switch.
                    //
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MatchOption(
            string arg,
            string option
            )
        {
            if (String.IsNullOrEmpty(arg) || String.IsNullOrEmpty(option))
                return false;

            return StringOps.SystemNoCaseEquals(arg, 0, option, 0, arg.Length);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static bool IsHelpOption(
            string arg
            )
        {
            if (String.IsNullOrEmpty(arg))
                return false;

            return StringOps.SystemNoCaseEquals(arg, "help") ||
                StringOps.SystemNoCaseEquals(arg, "?");
        }
#endif
        #endregion
        #endregion
    }
}
