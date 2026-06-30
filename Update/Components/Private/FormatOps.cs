/*
 * FormatOps.cs --
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
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for formatting various kinds
    /// of values (e.g. dates, byte arrays, delegates, certificates, cultures,
    /// and lists) into human-readable strings, primarily for display and log
    /// output by the updater.
    /// </summary>
    [Guid("9fe47978-cf5c-43a7-8333-2402bb6649ee")]
    internal static class FormatOps
    {
        #region Private Constants
        /// <summary>
        /// The composite format string used to format a single byte as two
        /// lowercase hexadecimal digits.
        /// </summary>
        private const string ByteHexFormat = "{0:x2}";

        /// <summary>
        /// The composite format string used to format a name and value pair.
        /// </summary>
        private const string NameAndValueFormat = "{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The custom format string used to format a Coordinated Universal Time
        /// (UTC) date and time value.
        /// </summary>
        private const string DateTimeUtcFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";

        /// <summary>
        /// The custom format string used to format a local (non-UTC) date and
        /// time value.
        /// </summary>
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The placeholder string used to represent a null value.
        /// </summary>
        private const string NullValue = "<null>";

        /// <summary>
        /// The placeholder string used to represent an empty value.
        /// </summary>
        private const string EmptyValue = "<empty>";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method converts the specified value to its string
        /// representation, dispatching to a type-specific formatter for known
        /// value kinds and falling back to a quoted display form otherwise.
        /// </summary>
        /// <param name="value">
        /// The value to convert to a string.  This parameter may be null.
        /// </param>
        /// <param name="default">
        /// The string to return when the value is null.
        /// </param>
        /// <returns>
        /// The string representation of the value, or the specified default
        /// when the value is null.
        /// </returns>
        private static string ValueToString(
            object value,
            string @default
            )
        {
            if (value == null)
                return @default;

            if (value is DateTime)
                return DateTimeToString((DateTime)value);

            if (value is byte[])
                return ToHexString((byte[])value);

            if (value is  string[])
                return ListToString((string[])value);

            if (value is  CultureInfo)
                return CultureToString((CultureInfo)value);

            Type type = value.GetType();

            if ((type == typeof(Delegate)) ||
                type.IsSubclassOf(typeof(Delegate)))
            {
                return DelegateToString(value as Delegate);
            }

            return ForDisplay(type, value.ToString());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified string value for display,
        /// wrapping it in quotes or braces as needed unless its type is a value
        /// type, and substituting placeholders for null or empty values.
        /// </summary>
        /// <param name="type">
        /// The type of the original value, used to decide whether the value
        /// should be wrapped.
        /// </param>
        /// <param name="value">
        /// The string value to format for display.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The display form of the value.
        /// </returns>
        private static string ForDisplay(
            Type type,
            string value
            )
        {
            if (value == null)
                return NullValue;

            if (value.Length == 0)
                return EmptyValue;

            char prefix;
            char suffix;

            if (value.IndexOf(Characters.DoubleQuote) != -1)
            {
                prefix = Characters.OpenBrace;
                suffix = Characters.CloseBrace;
            }
            else
            {
                prefix = Characters.DoubleQuote;
                suffix = Characters.DoubleQuote;
            }

            return String.Format(
                type.IsSubclassOf(typeof(ValueType)) ?
                "{0}" : "{1}{0}{2}", value, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time value, using the
        /// UTC format when its kind is UTC and the local format otherwise.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time value to format.
        /// </param>
        /// <returns>
        /// The formatted string representation of the date and time value.
        /// </returns>
        private static string DateTimeToString(
            DateTime dateTime
            )
        {
            return dateTime.ToString(
                (dateTime.Kind == DateTimeKind.Utc) ?
                DateTimeUtcFormat : DateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified delegate as the fully qualified
        /// name of its target method.
        /// </summary>
        /// <param name="delegate">
        /// The delegate to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The fully qualified target method name, or null if the delegate or
        /// its method information is null.
        /// </returns>
        private static string DelegateToString(
            Delegate @delegate
            )
        {
            MethodInfo methodInfo = (@delegate != null) ?
                @delegate.Method : null;

            if (methodInfo != null)
            {
                return String.Format(
                    "{0}{1}{2}", methodInfo.DeclaringType,
                    Type.Delimiter, methodInfo.Name);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region String Formatting Methods
        /// <summary>
        /// This method returns the empty string when the specified value is
        /// null and the value itself otherwise.
        /// </summary>
        /// <param name="value">
        /// The value to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The empty string if the value is null; otherwise, the value.
        /// </returns>
        public static string EmptyIfNull(
            string value
            )
        {
            if (value == null)
                return String.Empty;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified name and value as a single
        /// "name: value" string.
        /// </summary>
        /// <param name="name">
        /// The name portion of the pair.
        /// </param>
        /// <param name="value">
        /// The value portion of the pair.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted name and value string.
        /// </returns>
        public static string NameAndValue(
            string name,
            object value
            )
        {
            return String.Format(
                NameAndValueFormat, name, ValueToString(value, NullValue));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified value to its string
        /// representation, using the null placeholder when the value is null.
        /// </summary>
        /// <param name="value">
        /// The value to convert to a string.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The string representation of the value.
        /// </returns>
        public static string ValueToString(
            object value
            )
        {
            return ValueToString(value, NullValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified value for display, deriving its
        /// type and string form from the value itself.
        /// </summary>
        /// <param name="value">
        /// The value to format for display.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display form of the value.
        /// </returns>
        public static string ForDisplay(
            object value
            )
        {
            return ForDisplay(
                (value != null) ? value.GetType() : typeof(object),
                (value != null) ? value.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified byte array to its lowercase
        /// hexadecimal string representation.
        /// </summary>
        /// <param name="array">
        /// The byte array to convert.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The hexadecimal string representation of the array, or null if the
        /// array is null.
        /// </returns>
        public static string ToHexString(
            byte[] array
            )
        {
            if (array == null)
                return null;

            StringBuilder result = new StringBuilder();

            int length = array.Length;

            for (int index = 0; index < length; index++)
                result.AppendFormat(ByteHexFormat, array[index]);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified X.509 certificate as a string,
        /// returning its full subject when verbose and its simple name
        /// otherwise.
        /// </summary>
        /// <param name="certificate2">
        /// The certificate to format.  This parameter may be null.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to return the full certificate subject; zero to return its
        /// simple name.
        /// </param>
        /// <returns>
        /// The string representation of the certificate, or null if the
        /// certificate is null.
        /// </returns>
        public static string CertificateToString(
            X509Certificate2 certificate2,
            bool verbose
            )
        {
            if (certificate2 != null)
            {
                return verbose ? certificate2.Subject :
                    certificate2.GetNameInfo(X509NameType.SimpleName, false);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified culture as its display name.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The display name of the culture, or null if the culture is null.
        /// </returns>
        public static string CultureToString(
            CultureInfo cultureInfo
            )
        {
            if (cultureInfo != null)
                return cultureInfo.DisplayName;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified collection of strings as a single
        /// space-separated string, skipping any null elements.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The space-separated string representation of the collection.
        /// </returns>
        public static string ListToString(
            IEnumerable<string> collection
            )
        {
            StringBuilder result = new StringBuilder();

            if (collection != null)
            {
                foreach (string item in collection)
                {
                    if (item == null)
                        continue;

                    if (result.Length > 0)
                        result.Append(Characters.Space);

                    result.Append(item);
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method escapes special characters (e.g. ampersands, tabs, and
        /// line breaks) in the specified text so that it can be safely embedded
        /// as notes.
        /// </summary>
        /// <param name="text">
        /// The text to escape.
        /// </param>
        /// <returns>
        /// The text with special characters replaced by their escaped forms.
        /// </returns>
        public static string NotesToString(
            string text
            )
        {
            StringBuilder result = new StringBuilder(text);

            result.Replace(Characters.Ampersand.ToString(),
                Characters.EscapedAmpersand);

            result.Replace(Characters.HorizontalTab.ToString(),
                Characters.EscapedHorizontalTab);

            result.Replace(Characters.VerticalTab.ToString(),
                Characters.EscapedVerticalTab);

            result.Replace(Characters.LineFeed.ToString(),
                Characters.EscapedLineFeed);

            result.Replace(Characters.CarriageReturn.ToString(),
                Characters.EscapedCarriageReturn);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to format the raw (downloaded) "update data" for
        //       ease of reading in the log file, even when using Notepad.
        //
        /// <summary>
        /// This method formats the raw (downloaded) update data for ease of
        /// reading in a log file, replacing carriage returns and line feeds
        /// with visible markers and optionally inserting real line breaks.
        /// </summary>
        /// <param name="text">
        /// The raw text to format.  This parameter may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to also insert real end-of-line sequences after the visible
        /// markers, for display purposes.
        /// </param>
        /// <returns>
        /// The formatted (and trimmed) text, or null if the text is null.
        /// </returns>
        public static string RawDataToString(
            string text,
            bool display
            )
        {
            if (text == null)
                return null;

            StringBuilder result = new StringBuilder();
            string endOfLine = Characters.CarriageReturnLineFeed;
            bool sawCarriageReturn = false;

            foreach (char character in text)
            {
                switch (character)
                {
                    case Characters.CarriageReturn:
                        {
                            sawCarriageReturn = true;
                            break;
                        }
                    case Characters.LineFeed:
                        {
                            if (sawCarriageReturn)
                            {
                                result.Append(
                                    Characters.RawCarriageReturnLineFeed);

                                if (display)
                                    result.Append(endOfLine);

                                sawCarriageReturn = false;
                            }
                            else
                            {
                                result.Append(Characters.RawLineFeed);

                                if (display)
                                    result.Append(endOfLine);
                            }
                            break;
                        }
                    default:
                        {
                            if (sawCarriageReturn)
                            {
                                result.Append(Characters.RawCarriageReturn);

                                if (display)
                                    result.Append(endOfLine);

                                sawCarriageReturn = false;
                            }

                            result.Append(character);
                            break;
                        }
                }
            }

            if (sawCarriageReturn)
            {
                result.Append(Characters.RawCarriageReturn);

                if (display)
                    result.Append(endOfLine);

                sawCarriageReturn = false;
            }

            return result.ToString().Trim();
        }
        #endregion
    }
}
