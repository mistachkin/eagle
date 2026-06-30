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
    /// <summary>
    /// This class contains the character and string constants used by the
    /// update component for parsing and formatting.
    /// </summary>
    [Guid("b0aa6264-f071-4ffc-94f3-28bfd6ec4e03")]
    internal static class Characters
    {
        #region Private Constants
        /// <summary>
        /// The primary character that introduces a comment.
        /// </summary>
        public const char Comment = NumberSign;

        /// <summary>
        /// The alternate character that introduces a comment.
        /// </summary>
        public const char AltComment = SemiColon;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The horizontal tab character.
        /// </summary>
        public const char HorizontalTab = '\t';

        /// <summary>
        /// The vertical tab character.
        /// </summary>
        public const char VerticalTab = '\v';

        /// <summary>
        /// The line-feed character.
        /// </summary>
        public const char LineFeed = '\n';

        /// <summary>
        /// The carriage-return character.
        /// </summary>
        public const char CarriageReturn = '\r';

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The colon character.
        /// </summary>
        public const char Colon = ':';

        /// <summary>
        /// The opening curly brace character.
        /// </summary>
        public const char OpenBrace = '{';

        /// <summary>
        /// The closing curly brace character.
        /// </summary>
        public const char CloseBrace = '}';

        /// <summary>
        /// The space character.
        /// </summary>
        public const char Space = ' ';

        /// <summary>
        /// The double-quote character.
        /// </summary>
        public const char DoubleQuote = '"';

        /// <summary>
        /// The number-sign character.
        /// </summary>
        public const char NumberSign = '#';

        /// <summary>
        /// The ampersand character.
        /// </summary>
        public const char Ampersand = '&';

        /// <summary>
        /// The comma character.
        /// </summary>
        public const char Comma = ',';

        /// <summary>
        /// The period character.
        /// </summary>
        public const char Period = '.';

        /// <summary>
        /// The forward slash character.
        /// </summary>
        public const char Slash = '/';

        /// <summary>
        /// The semicolon character.
        /// </summary>
        public const char SemiColon = ';';

        /// <summary>
        /// The backslash character.
        /// </summary>
        public const char Backslash = '\\';

        /// <summary>
        /// The underscore character.
        /// </summary>
        public const char Underscore = '_';

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The carriage-return / line-feed pair, as a string.
        /// </summary>
        public static readonly string CarriageReturnLineFeed =
            CarriageReturn.ToString() + LineFeed.ToString();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The escape sequence used to represent a horizontal tab character.
        /// </summary>
        public const string EscapedHorizontalTab = "&htab;";

        /// <summary>
        /// The escape sequence used to represent a vertical tab character.
        /// </summary>
        public const string EscapedVerticalTab = "&vtab;";

        /// <summary>
        /// The escape sequence used to represent a line-feed character.
        /// </summary>
        public const string EscapedLineFeed = "&lf;";

        /// <summary>
        /// The escape sequence used to represent a carriage-return character.
        /// </summary>
        public const string EscapedCarriageReturn = "&cr;";

        /// <summary>
        /// The escape sequence used to represent an ampersand character.
        /// </summary>
        public const string EscapedAmpersand = "&amp;";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The raw marker used to represent a line-feed character.
        /// </summary>
        public const string RawLineFeed = "<<lf>>";

        /// <summary>
        /// The raw marker used to represent a carriage-return character.
        /// </summary>
        public const string RawCarriageReturn = "<<cr>>";

        /// <summary>
        /// The raw marker used to represent a carriage-return / line-feed pair.
        /// </summary>
        public const string RawCarriageReturnLineFeed = "<<crlf>>";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of hexadecimal characters used to represent a single
        /// byte.
        /// </summary>
        public const int ByteHexChars = 2;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The primary character that introduces a command line switch.
        /// </summary>
        public const char Switch = '-';

        /// <summary>
        /// The alternate character that introduces a command line switch.
        /// </summary>
        public const char AltSwitch = '/';

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of characters that may introduce a command line switch.
        /// </summary>
        public static readonly char[] SwitchChars = {
            Switch, AltSwitch
        };
        #endregion
    }
}
