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
    [Guid("9fe47978-cf5c-43a7-8333-2402bb6649ee")]
    internal static class FormatOps
    {
        #region Private Constants
        private const string ByteHexFormat = "{0:x2}";
        private const string NameAndValueFormat = "{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        private const string DateTimeUtcFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        ///////////////////////////////////////////////////////////////////////

        private const string NullValue = "<null>";
        private const string EmptyValue = "<empty>";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
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

        private static string DateTimeToString(
            DateTime dateTime
            )
        {
            return dateTime.ToString(
                (dateTime.Kind == DateTimeKind.Utc) ?
                DateTimeUtcFormat : DateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string EmptyIfNull(
            string value
            )
        {
            if (value == null)
                return String.Empty;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NameAndValue(
            string name,
            object value
            )
        {
            return String.Format(
                NameAndValueFormat, name, ValueToString(value, NullValue));
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ValueToString(
            object value
            )
        {
            return ValueToString(value, NullValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ForDisplay(
            object value
            )
        {
            return ForDisplay(
                (value != null) ? value.GetType() : typeof(object),
                (value != null) ? value.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string CultureToString(
            CultureInfo cultureInfo
            )
        {
            if (cultureInfo != null)
                return cultureInfo.DisplayName;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
