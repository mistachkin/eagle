/*
 * Channel.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface represents an Eagle I/O channel, wrapping an underlying
    /// stream together with its encoding, end-of-line translation, buffering,
    /// and reader/writer state.  It is the private contract behind the channels
    /// manipulated by the [open], [socket], [read], [puts], and related script
    /// commands.
    /// </summary>
    [ObjectId("cbfb281c-d7a7-43e2-9e24-6f4148ee1242")]
    internal interface IChannel : IIdentifier
    {
        /// <summary>
        /// Gets the current channel context, which holds the active stream and
        /// its associated readers, writers, and buffers.
        /// </summary>
        IChannelContext Context { get; }

        /// <summary>
        /// This method determines whether this channel has a saved (previously
        /// pushed) context.
        /// </summary>
        /// <returns>
        /// True if a saved context is present; otherwise, false.
        /// </returns>
        bool HaveSavedContext();

        /// <summary>
        /// This method begins a new channel context using the specified stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to use for the new context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was begun; otherwise, false.
        /// </returns>
        bool BeginContext(Stream stream, ref Result error);

        /// <summary>
        /// This method ends the current channel context.
        /// </summary>
        /// <param name="close">
        /// Non-zero to close the underlying stream as part of ending the
        /// context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was ended; otherwise, false.
        /// </returns>
        bool EndContext(bool close, ref Result error);

        /// <summary>
        /// This method begins a new channel context using the specified stream,
        /// saving the previously active context.
        /// </summary>
        /// <param name="stream">
        /// The stream to use for the new context.
        /// </param>
        /// <param name="savedContext">
        /// Upon success, this receives the channel context that was active
        /// prior to this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was begun; otherwise, false.
        /// </returns>
        bool BeginContext(
            Stream stream,
            ref IChannelContext savedContext,
            ref Result error
        );

        /// <summary>
        /// This method ends the current channel context and restores a
        /// previously saved context.
        /// </summary>
        /// <param name="close">
        /// Non-zero to close the underlying stream as part of ending the
        /// context.
        /// </param>
        /// <param name="savedContext">
        /// The previously saved channel context to restore.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was ended; otherwise, false.
        /// </returns>
        bool EndContext(
            bool close,
            ref IChannelContext savedContext,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the channel supports reading.
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// Gets a value indicating whether the channel supports seeking.
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// Gets a value indicating whether the channel supports writing.
        /// </summary>
        bool CanWrite { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the end of the stream has
        /// been reached.
        /// </summary>
        bool HitEndOfStream { get; set; }

        /// <summary>
        /// Gets a value indicating whether the channel is currently at the end
        /// of the stream.
        /// </summary>
        bool EndOfStream { get; }

        /// <summary>
        /// Gets a value indicating whether any indication of end-of-stream has
        /// been observed for this channel.
        /// </summary>
        bool AnyEndOfStream { get; }

        /// <summary>
        /// Gets a value indicating whether a single end-of-stream condition has
        /// been observed for this channel.
        /// </summary>
        bool OneEndOfStream { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in bytes, of the underlying stream.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// This method sets the length of the underlying stream.
        /// </summary>
        /// <param name="value">
        /// The desired length, in bytes, of the underlying stream.
        /// </param>
        void SetLength(long value);

        /// <summary>
        /// Gets the current position within the underlying stream.
        /// </summary>
        long Position { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the end-of-line handling parameters currently
        /// in effect for this channel.
        /// </summary>
        /// <param name="endOfLine">
        /// Upon return, this receives the list of characters that mark the end
        /// of a line.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Upon return, this is non-zero if any one of the end-of-line
        /// characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Upon return, this is non-zero if the end-of-line characters are
        /// retained in the data that is read.
        /// </param>
        void GetEndOfLineParameters(
            out CharList endOfLine,
            out bool useAnyEndOfLineChar,
            out bool keepEndOfLineChars
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads from the channel into a list of bytes.
        /// </summary>
        /// <param name="list">
        /// Upon success, this receives the bytes that were read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Read(ref ByteList list, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a line from the channel using the specified
        /// end-of-line handling parameters.
        /// </summary>
        /// <param name="endOfLine">
        /// The list of characters that mark the end of a line.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any one of the end-of-line characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the data that is
        /// read.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the bytes that were read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Read(
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads up to the specified number of bytes from the
        /// channel using the specified end-of-line handling parameters.
        /// </summary>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <param name="endOfLine">
        /// The list of characters that mark the end of a line.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any one of the end-of-line characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the data that is
        /// read.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the bytes that were read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Read(
            int count,
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads from the channel into a list of bytes using the
        /// internal buffering layer.
        /// </summary>
        /// <param name="list">
        /// Upon success, this receives the bytes that were read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ReadBuffer(ref ByteList list, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a line from the channel using the internal
        /// buffering layer and the specified end-of-line handling parameters.
        /// </summary>
        /// <param name="endOfLine">
        /// The list of characters that mark the end of a line.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any one of the end-of-line characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the data that is
        /// read.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the bytes that were read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ReadBuffer(
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads up to the specified number of bytes from the
        /// channel using the internal buffering layer and the specified
        /// end-of-line handling parameters.
        /// </summary>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <param name="endOfLine">
        /// The list of characters that mark the end of a line.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any one of the end-of-line characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the data that is
        /// read.
        /// </param>
        /// <param name="list">
        /// Upon success, this receives the bytes that were read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ReadBuffer(
            int count,
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the position within the underlying stream.
        /// </summary>
        /// <param name="offset">
        /// The byte offset relative to the origin.
        /// </param>
        /// <param name="origin">
        /// The reference point used to obtain the new position.
        /// </param>
        /// <returns>
        /// The new position within the underlying stream.
        /// </returns>
        long Seek(long offset, SeekOrigin origin);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the channel stream associated with this channel.
        /// </summary>
        /// <returns>
        /// The channel stream, or null if none is available.
        /// </returns>
        ChannelStream GetStream();

        /// <summary>
        /// This method returns the innermost underlying stream associated with
        /// this channel.
        /// </summary>
        /// <returns>
        /// The underlying stream, or null if none is available.
        /// </returns>
        Stream GetInnerStream();

        /// <summary>
        /// Gets a value indicating whether this channel has an associated
        /// reader.
        /// </summary>
        bool HasReader { get; }

        /// <summary>
        /// Gets a value indicating whether this channel has an associated
        /// writer.
        /// </summary>
        bool HasWriter { get; }

        /// <summary>
        /// Gets a value indicating whether this channel has buffered data.
        /// </summary>
        bool HasBuffer { get; }

        /// <summary>
        /// This method returns the binary reader associated with this channel.
        /// </summary>
        /// <returns>
        /// The binary reader, or null if none is available.
        /// </returns>
        BinaryReader GetBinaryReader();

        /// <summary>
        /// This method returns the binary writer associated with this channel.
        /// </summary>
        /// <returns>
        /// The binary writer, or null if none is available.
        /// </returns>
        BinaryWriter GetBinaryWriter();

        /// <summary>
        /// This method returns the stream reader associated with this channel.
        /// </summary>
        /// <returns>
        /// The stream reader, or null if none is available.
        /// </returns>
        StreamReader GetStreamReader();

        /// <summary>
        /// This method returns the stream writer associated with this channel.
        /// </summary>
        /// <returns>
        /// The stream writer, or null if none is available.
        /// </returns>
        StreamWriter GetStreamWriter();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards any buffered data for this channel.
        /// </summary>
        /// <returns>
        /// The number of buffered bytes that were discarded.
        /// </returns>
        int DiscardBuffered();

        /// <summary>
        /// This method resets the buffering state for this channel.
        /// </summary>
        void ResetBuffered();

        /// <summary>
        /// This method removes and returns the buffered data and line-ending
        /// positions for this channel.
        /// </summary>
        /// <param name="buffer">
        /// Upon return, this receives the buffered bytes.
        /// </param>
        /// <param name="lineEndings">
        /// Upon return, this receives the buffered line-ending positions.
        /// </param>
        void TakeBuffered(out ByteList buffer, out IntList lineEndings);

        /// <summary>
        /// This method gives buffered data and line-ending positions to this
        /// channel.
        /// </summary>
        /// <param name="buffer">
        /// The buffered bytes to give to the channel.
        /// </param>
        /// <param name="lineEndings">
        /// The buffered line-ending positions to give to the channel.
        /// </param>
        /// <returns>
        /// True if the buffered data was accepted; otherwise, false.
        /// </returns>
        bool GiveBuffered(ref ByteList buffer, ref IntList lineEndings);

        /// <summary>
        /// This method establishes a new, empty buffer for this channel.
        /// </summary>
        void NewBuffered();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this channel uses a null (i.e. raw
        /// byte) encoding.
        /// </summary>
        bool NullEncoding { get; }

        /// <summary>
        /// This method returns the text encoding used by this channel.
        /// </summary>
        /// <returns>
        /// The encoding used by this channel, or null if none is set.
        /// </returns>
        Encoding GetEncoding();

        /// <summary>
        /// This method sets the text encoding used by this channel.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use for this channel.  This parameter may be null.
        /// </param>
        void SetEncoding(Encoding encoding);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the end-of-line translation applied to input on
        /// this channel.
        /// </summary>
        /// <returns>
        /// The input end-of-line translation.
        /// </returns>
        StreamTranslation GetInputTranslation();

        /// <summary>
        /// This method returns the end-of-line translation applied to output on
        /// this channel.
        /// </summary>
        /// <returns>
        /// The output end-of-line translation.
        /// </returns>
        StreamTranslation GetOutputTranslation();

        /// <summary>
        /// This method returns the list of end-of-line translations configured
        /// for this channel.
        /// </summary>
        /// <returns>
        /// The list of end-of-line translations.
        /// </returns>
        StreamTranslationList GetTranslation();

        /// <summary>
        /// This method sets the list of end-of-line translations for this
        /// channel.
        /// </summary>
        /// <param name="translation">
        /// The list of end-of-line translations to use.
        /// </param>
        void SetTranslation(StreamTranslationList translation);

        /// <summary>
        /// This method returns the end-of-line character sequence used for
        /// input on this channel.
        /// </summary>
        /// <returns>
        /// The input end-of-line character sequence.
        /// </returns>
        CharList GetInputEndOfLine();

        /// <summary>
        /// This method returns the end-of-line character sequence used for
        /// output on this channel.
        /// </summary>
        /// <returns>
        /// The output end-of-line character sequence.
        /// </returns>
        CharList GetOutputEndOfLine();

        /// <summary>
        /// This method removes any trailing end-of-line characters from the
        /// specified buffer.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to remove trailing end-of-line characters from.
        /// </param>
        /// <param name="endOfLine">
        /// The end-of-line character sequence to remove.
        /// </param>
        void RemoveTrailingEndOfLine(ByteList buffer, CharList endOfLine);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a value indicating whether this channel is in
        /// blocking mode.
        /// </summary>
        /// <returns>
        /// True if the channel is in blocking mode; otherwise, false.
        /// </returns>
        bool GetBlockingMode();

        /// <summary>
        /// This method sets whether this channel operates in blocking mode.
        /// </summary>
        /// <param name="blockingMode">
        /// Non-zero to place the channel in blocking mode; zero for
        /// non-blocking mode.
        /// </param>
        void SetBlockingMode(bool blockingMode);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ensures the channel is positioned for appending, when
        /// applicable.
        /// </summary>
        void CheckAppend();

        /// <summary>
        /// This method performs an automatic flush of this channel when the
        /// auto-flush setting is enabled.
        /// </summary>
        /// <returns>
        /// True if the channel was flushed; otherwise, false.
        /// </returns>
        bool CheckAutoFlush();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered output for this channel to the
        /// underlying stream.
        /// </summary>
        /// <returns>
        /// True if the flush succeeded; otherwise, false.
        /// </returns>
        bool Flush();

        /// <summary>
        /// This method closes this channel and releases its underlying
        /// resources.
        /// </summary>
        void Close();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream is a console
        /// stream.
        /// </summary>
        bool IsConsoleStream { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the underlying socket associated with this channel, if any.
        /// </summary>
        object Socket { get; }

        /// <summary>
        /// Gets a value indicating whether the underlying stream is a network
        /// stream.
        /// </summary>
        bool IsNetworkStream { get; }

        /// <summary>
        /// Gets a value indicating whether the underlying network connection is
        /// currently connected.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Gets a value indicating whether data is available to be read from
        /// the underlying stream.
        /// </summary>
        bool DataAvailable { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this channel writes to a virtual
        /// (in-memory) output buffer.
        /// </summary>
        bool IsVirtualOutput { get; }

        /// <summary>
        /// Gets or sets the virtual (in-memory) output buffer for this channel.
        /// </summary>
        StringBuilder VirtualOutput { get; set; }

        /// <summary>
        /// This method appends a single character to this channel's virtual
        /// output buffer.
        /// </summary>
        /// <param name="value">
        /// The character to append.
        /// </param>
        /// <returns>
        /// True if the character was appended; otherwise, false.
        /// </returns>
        bool AppendVirtualOutput(char value);

        /// <summary>
        /// This method appends a string to this channel's virtual output
        /// buffer.
        /// </summary>
        /// <param name="value">
        /// The string to append.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the string was appended; otherwise, false.
        /// </returns>
        bool AppendVirtualOutput(string value);

        /// <summary>
        /// This method appends an array of bytes to this channel's virtual
        /// output buffer.
        /// </summary>
        /// <param name="value">
        /// The array of bytes to append.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the bytes were appended; otherwise, false.
        /// </returns>
        bool AppendVirtualOutput(byte[] value);
    }
}
