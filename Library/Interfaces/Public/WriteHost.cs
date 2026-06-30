/*
 * WriteHost.cs --
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
using Eagle._Containers.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by interpreter hosts that support writing
    /// characters and strings to their output, optionally with a trailing
    /// newline, a repeat count, and/or specific foreground and background
    /// colors.  It extends <see cref="IInteractiveHost" /> with the output
    /// portion of the host contract.
    /// </summary>
    [ObjectId("6232cc30-2233-4248-9b4e-4e8bb71bb66a")]
    public interface IWriteHost : IInteractiveHost
    {
        /// <summary>
        /// Writes a single character to the host output, optionally followed
        /// by a newline.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        bool Write(char value, bool newLine);
        /// <summary>
        /// Writes a single character to the host output the specified number
        /// of times.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        bool Write(char value, int count);
        /// <summary>
        /// Writes a single character to the host output the specified number
        /// of times, optionally followed by a newline.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the characters.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        bool Write(char value, int count, bool newLine);
        /// <summary>
        /// Writes a single character to the host output the specified number
        /// of times, optionally followed by a newline, using the specified
        /// foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        bool Write(char value, int count, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);
        /// <summary>
        /// Writes a single character to the host output using the specified
        /// foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        bool Write(char value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// Writes a string to the host output using the specified foreground
        /// color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool Write(string value, ConsoleColor foregroundColor);
        /// <summary>
        /// Writes a string to the host output using the specified foreground
        /// and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool Write(string value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// Writes a string to the host output, optionally followed by a
        /// newline.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool Write(string value, bool newLine);
        /// <summary>
        /// Writes a string to the host output, optionally followed by a
        /// newline, using the specified foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool Write(string value, bool newLine, ConsoleColor foregroundColor);
        /// <summary>
        /// Writes a string to the host output, optionally followed by a
        /// newline, using the specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool Write(string value, bool newLine, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);

        /// <summary>
        /// Writes a formatted list of name and value pairs to the host output,
        /// optionally followed by a newline, using the specified foreground
        /// and background colors.
        /// </summary>
        /// <param name="list">
        /// The list of name and value pairs to write.  This parameter may be
        /// null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a newline after the formatted output.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the formatted output was written; otherwise, false.
        /// </returns>
        bool WriteFormat(StringPairList list, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// Writes a string followed by a newline to the host output using the
        /// specified foreground color.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool WriteLine(string value, ConsoleColor foregroundColor);
        /// <summary>
        /// Writes a string followed by a newline to the host output using the
        /// specified foreground and background colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.  This parameter may be null.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool WriteLine(string value, ConsoleColor foregroundColor,
            ConsoleColor backgroundColor);
    }
}
