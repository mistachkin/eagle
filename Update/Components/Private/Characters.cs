/*
 * Characters.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    [Guid("b0aa6264-f071-4ffc-94f3-28bfd6ec4e03")]
    internal static class Characters
    {
        #region Private Constants
        public const char Comment = NumberSign;
        public const char AltComment = SemiColon;

        ///////////////////////////////////////////////////////////////////////

        public const char HorizontalTab = '\t';
        public const char VerticalTab = '\v';
        public const char LineFeed = '\n';
        public const char CarriageReturn = '\r';

        ///////////////////////////////////////////////////////////////////////

        public const char Colon = ':';
        public const char OpenBrace = '{';
        public const char CloseBrace = '}';
        public const char Space = ' ';
        public const char DoubleQuote = '"';
        public const char NumberSign = '#';
        public const char Ampersand = '&';
        public const char Comma = ',';
        public const char Period = '.';
        public const char Slash = '/';
        public const char SemiColon = ';';
        public const char Backslash = '\\';
        public const char Underscore = '_';

        ///////////////////////////////////////////////////////////////////////

        public static readonly string CarriageReturnLineFeed =
            CarriageReturn.ToString() + LineFeed.ToString();

        ///////////////////////////////////////////////////////////////////////

        public const string EscapedHorizontalTab = "&htab;";
        public const string EscapedVerticalTab = "&vtab;";
        public const string EscapedLineFeed = "&lf;";
        public const string EscapedCarriageReturn = "&cr;";
        public const string EscapedAmpersand = "&amp;";

        ///////////////////////////////////////////////////////////////////////

        public const string RawLineFeed = "<<lf>>";
        public const string RawCarriageReturn = "<<cr>>";
        public const string RawCarriageReturnLineFeed = "<<crlf>>";

        ///////////////////////////////////////////////////////////////////////

        public const int ByteHexChars = 2;

        ///////////////////////////////////////////////////////////////////////

        public const char Switch = '-';
        public const char AltSwitch = '/';

        ///////////////////////////////////////////////////////////////////////

        public static readonly char[] SwitchChars = {
            Switch, AltSwitch
        };
        #endregion
    }
}
