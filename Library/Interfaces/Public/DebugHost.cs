/*
 * DebugHost.cs --
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
using Eagle._Components.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by hosts that support debugging-related
    /// output and control operations.  It extends
    /// <see cref="IInteractiveHost" /> with members to clone the host, query
    /// host test flags, cancel or exit interpreter activity, and write debug,
    /// error, and result text to the host.
    /// </summary>
    [ObjectId("56c86c61-db4e-43ba-8bc9-938247ec95e6")]
    public interface IDebugHost : IInteractiveHost
    {
        /// <summary>
        /// Creates a copy of this host.
        /// </summary>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        IHost Clone();

        /// <summary>
        /// Creates a copy of this host for use with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the cloned host will be associated with.
        /// </param>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        IHost Clone(Interpreter interpreter); // TODO: Change this to use the IInterpreter type.

        /// <summary>
        /// Gets the <see cref="HostTestFlags" /> that describe the testing
        /// capabilities of this host.
        /// </summary>
        /// <returns>
        /// The host test flags for this host.
        /// </returns>
        HostTestFlags GetTestFlags();

        /// <summary>
        /// Requests that the current script evaluation be canceled.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly cancel evaluation even if cancellation has
        /// been disabled or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Cancel(bool force, ref Result error);

        /// <summary>
        /// Requests that the interpreter exit.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly request the exit even if it has been disabled
        /// or is otherwise being prevented.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Exit(bool force, ref Result error);

        /// <summary>
        /// Writes a line terminator to the debug output of the host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebugLine();

        /// <summary>
        /// Writes the specified string, followed by a line terminator, to the
        /// debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebugLine(string value);

        /// <summary>
        /// Writes the specified character to the debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(char value);

        /// <summary>
        /// Writes the specified character to the debug output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the character.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(char value, bool newLine);

        /// <summary>
        /// Writes the specified character a number of times to the debug
        /// output of the host, using the specified colors and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the debug output.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(char value, int count, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// Writes the specified string to the debug output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(string value);

        /// <summary>
        /// Writes the specified string to the debug output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(string value, bool newLine);

        /// <summary>
        /// Writes the specified string to the debug output of the host, using
        /// the specified foreground color and optionally followed by a line
        /// terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(string value, bool newLine,
            ConsoleColor foregroundColor);

        /// <summary>
        /// Writes the specified string to the debug output of the host, using
        /// the specified colors and optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the debug output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteDebug(string value, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// Writes a line terminator to the error output of the host.
        /// </summary>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteErrorLine();

        /// <summary>
        /// Writes the specified string, followed by a line terminator, to the
        /// error output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteErrorLine(string value);

        /// <summary>
        /// Writes the specified character to the error output of the host.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(char value);

        /// <summary>
        /// Writes the specified character to the error output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the character.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(char value, bool newLine);

        /// <summary>
        /// Writes the specified character a number of times to the error
        /// output of the host, using the specified colors and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The character to write to the error output.
        /// </param>
        /// <param name="count">
        /// The number of times to write the character.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the characters.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(char value, int count, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// Writes the specified string to the error output of the host.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(string value);

        /// <summary>
        /// Writes the specified string to the error output of the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(string value, bool newLine);

        /// <summary>
        /// Writes the specified string to the error output of the host, using
        /// the specified foreground color and optionally followed by a line
        /// terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(string value, bool newLine,
            ConsoleColor foregroundColor);

        /// <summary>
        /// Writes the specified string to the error output of the host, using
        /// the specified colors and optionally followed by a line terminator.
        /// </summary>
        /// <param name="value">
        /// The string to write to the error output.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the string.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteError(string value, bool newLine,
            ConsoleColor foregroundColor, ConsoleColor backgroundColor);

        /// <summary>
        /// Writes the specified return code and result to the host,
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteResult(ReturnCode code, Result result, bool newLine);

        /// <summary>
        /// Writes the specified return code and result to the host,
        /// optionally without additional formatting and optionally followed
        /// by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteResult(ReturnCode code, Result result, bool raw,
            bool newLine);

        /// <summary>
        /// Writes the specified return code, result, and error line to the
        /// host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteResult(ReturnCode code, Result result, int errorLine,
            bool newLine);

        /// <summary>
        /// Writes the specified return code, result, and error line to the
        /// host, optionally without additional formatting and optionally
        /// followed by a line terminator.
        /// </summary>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteResult(ReturnCode code, Result result, int errorLine,
            bool raw, bool newLine);

        /// <summary>
        /// Writes the specified prefix, return code, result, and error line
        /// to the host, optionally followed by a line terminator.
        /// </summary>
        /// <param name="prefix">
        /// The string to write before the result.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteResult(string prefix, ReturnCode code, Result result,
            int errorLine, bool newLine);

        /// <summary>
        /// Writes the specified prefix, return code, result, and error line
        /// to the host, optionally without additional formatting and
        /// optionally followed by a line terminator.
        /// </summary>
        /// <param name="prefix">
        /// The string to write before the result.
        /// </param>
        /// <param name="code">
        /// The return code to write.
        /// </param>
        /// <param name="result">
        /// The result to write.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <param name="raw">
        /// Non-zero to write the result without any additional formatting.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to write a line terminator after the result.
        /// </param>
        /// <returns>
        /// True if the text was written successfully; otherwise, false.
        /// </returns>
        bool WriteResult(string prefix, ReturnCode code, Result result,
            int errorLine, bool raw, bool newLine);
    }
}
