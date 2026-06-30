/*
 * InteractiveHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    //
    // NOTE: This interface represents the absolute mimimum requirements for a
    //       custom host to be usable by the interactive loop.  Nothing should
    //       be added to this interface.
    //
    /// <summary>
    /// This interface represents the absolute minimum requirements for a custom
    /// host to be usable by the interactive loop.  It composes the standard
    /// identity contract (<see cref="IIdentifier" />) and supplies the members
    /// needed to manage the title, read input, write output, and query the
    /// state of the host during interactive processing.
    /// </summary>
    [ObjectId("8eba5a3b-6a51-465b-b0a4-32cdfd568970")]
    public interface IInteractiveHost : IIdentifier
    {
        /// <summary>
        /// This method is called when interactive processing is about to begin
        /// at the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the start of processing; on
        /// output, the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode BeginProcessing(
            int levels, ref string text, ref Result error);

        /// <summary>
        /// This method is called when interactive processing is about to end at
        /// the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="text">
        /// On input, the text associated with the end of processing; on output,
        /// the possibly modified text.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode EndProcessing(
            int levels, ref string text, ref Result error);

        /// <summary>
        /// This method is called when interactive processing has completed at
        /// the specified nesting level.
        /// </summary>
        /// <param name="levels">
        /// The current interactive nesting level.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode DoneProcessing(int levels, ref Result error);

        /// <summary>
        /// Gets or sets the current window or console title used by this host.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// This method updates the host's window or console title to reflect
        /// its current value.
        /// </summary>
        /// <returns>
        /// True if the title was refreshed; otherwise, false.
        /// </returns>
        bool RefreshTitle();
        /// <summary>
        /// This method determines whether the host's interactive input has been
        /// redirected (for example, from a file or pipe).
        /// </summary>
        /// <returns>
        /// True if the input is redirected; otherwise, false.
        /// </returns>
        bool IsInputRedirected();

        /// <summary>
        /// This method displays a prompt of the specified type and reports the
        /// flags that resulted from displaying it.
        /// </summary>
        /// <param name="type">
        /// The type of prompt to display (for example, a normal or
        /// continuation prompt).
        /// </param>
        /// <param name="flags">
        /// On input, the flags that control how the prompt is displayed; on
        /// output, the flags that resulted from displaying it.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode Prompt(
            PromptType type, ref PromptFlags flags, ref Result error);

        /// <summary>
        /// This method determines whether the host's interactive resources are
        /// currently open.
        /// </summary>
        /// <returns>
        /// True if the host is open; otherwise, false.
        /// </returns>
        bool IsOpen();
        /// <summary>
        /// This method pauses interactive processing, typically waiting for the
        /// user to acknowledge before continuing.
        /// </summary>
        /// <returns>
        /// True if the host was paused; otherwise, false.
        /// </returns>
        bool Pause();
        /// <summary>
        /// This method flushes any buffered host output.
        /// </summary>
        /// <returns>
        /// True if the output was flushed; otherwise, false.
        /// </returns>
        bool Flush();

        /// <summary>
        /// This method returns the flags that control which header sections the
        /// host displays.
        /// </summary>
        /// <returns>
        /// The current header flags for this host.
        /// </returns>
        HeaderFlags GetHeaderFlags();
        /// <summary>
        /// This method returns the flags that control how much detail the host
        /// includes in its output.
        /// </summary>
        /// <returns>
        /// The current detail flags for this host.
        /// </returns>
        DetailFlags GetDetailFlags();
        /// <summary>
        /// This method returns the flags that describe the capabilities and
        /// configuration of this host.
        /// </summary>
        /// <returns>
        /// The current host flags for this host.
        /// </returns>
        HostFlags GetHostFlags();

        /// <summary>
        /// Gets the current nesting level of read operations in progress on
        /// this host.
        /// </summary>
        int ReadLevels { get; }
        /// <summary>
        /// Gets the current nesting level of write operations in progress on
        /// this host.
        /// </summary>
        int WriteLevels { get; }

        /// <summary>
        /// This method reads a single line of interactive input from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, this is set to the line of input that was read.
        /// </param>
        /// <returns>
        /// True if a line was read; otherwise, false.
        /// </returns>
        bool ReadLine(ref string value);

        /// <summary>
        /// This method writes a single character to the host output.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <returns>
        /// True if the character was written; otherwise, false.
        /// </returns>
        bool Write(char value);
        /// <summary>
        /// This method writes a string to the host output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <returns>
        /// True if the string was written; otherwise, false.
        /// </returns>
        bool Write(string value);

        /// <summary>
        /// This method writes an end-of-line to the host output.
        /// </summary>
        /// <returns>
        /// True if the end-of-line was written; otherwise, false.
        /// </returns>
        bool WriteLine();
        /// <summary>
        /// This method writes a string followed by an end-of-line to the host
        /// output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        bool WriteLine(string value);

        /// <summary>
        /// This method writes a formatted representation of a result, followed
        /// by an end-of-line, to the host output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        bool WriteResultLine(ReturnCode code, Result result);
        /// <summary>
        /// This method writes a formatted representation of a result, including
        /// an error line number, followed by an end-of-line, to the host
        /// output.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result value to write.
        /// </param>
        /// <param name="errorLine">
        /// The script line number associated with an error, or zero if not
        /// applicable.
        /// </param>
        /// <returns>
        /// True if the line was written; otherwise, false.
        /// </returns>
        bool WriteResultLine(ReturnCode code, Result result, int errorLine);
    }
}
