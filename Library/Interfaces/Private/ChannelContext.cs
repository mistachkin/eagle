/*
 * ChannelContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface represents the mutable per-context state of an Eagle I/O
    /// channel, including its channel stream, the binary and text readers and
    /// writers layered over that stream, and the read buffer together with its
    /// line-ending positions.
    /// </summary>
    [ObjectId("6ac8bae1-d952-4df3-862e-6242f1695ed1")]
    internal interface IChannelContext : IThreadContext, IDisposable
    {
        /// <summary>
        /// Gets the channel stream associated with this context.
        /// </summary>
        ChannelStream ChannelStream { get; }

        /// <summary>
        /// Gets the binary reader associated with this context, if any.
        /// </summary>
        BinaryReader BinaryReader { get; }

        /// <summary>
        /// Gets the binary writer associated with this context, if any.
        /// </summary>
        BinaryWriter BinaryWriter { get; }

        /// <summary>
        /// Gets the stream reader associated with this context, if any.
        /// </summary>
        StreamReader StreamReader { get; }

        /// <summary>
        /// Gets the stream writer associated with this context, if any.
        /// </summary>
        StreamWriter StreamWriter { get; }

        /// <summary>
        /// Gets a value indicating whether this context has an associated
        /// reader.
        /// </summary>
        bool HasReader { get; }

        /// <summary>
        /// Gets a value indicating whether this context has an associated
        /// writer.
        /// </summary>
        bool HasWriter { get; }

        /// <summary>
        /// Gets a value indicating whether this context has a read buffer.
        /// </summary>
        bool HasBuffer { get; }

        /// <summary>
        /// Gets a value indicating whether this context has a read buffer that
        /// is empty.
        /// </summary>
        bool HasEmptyBuffer { get; }

        /// <summary>
        /// This method returns the binary reader for this context, creating it
        /// using the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the reader, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The binary reader for this context.
        /// </returns>
        BinaryReader GetBinaryReader(Encoding encoding);

        /// <summary>
        /// This method returns the binary writer for this context, creating it
        /// using the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the writer, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The binary writer for this context.
        /// </returns>
        BinaryWriter GetBinaryWriter(Encoding encoding);

        /// <summary>
        /// This method returns the stream reader for this context, creating it
        /// using the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the reader, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The stream reader for this context.
        /// </returns>
        StreamReader GetStreamReader(Encoding encoding);

        /// <summary>
        /// This method returns the stream writer for this context, creating it
        /// using the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the writer, if needed.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The stream writer for this context.
        /// </returns>
        StreamWriter GetStreamWriter(Encoding encoding);

        /// <summary>
        /// This method discards the read buffer for this context.
        /// </summary>
        /// <returns>
        /// The number of buffered bytes that were discarded.
        /// </returns>
        int DiscardBuffer();

        /// <summary>
        /// This method removes and returns the read buffer for this context.
        /// </summary>
        /// <returns>
        /// The buffered bytes, or null if no buffer is present.
        /// </returns>
        ByteList TakeBuffer();

        /// <summary>
        /// This method gives a read buffer to this context.
        /// </summary>
        /// <param name="buffer">
        /// The buffered bytes to give to this context.
        /// </param>
        /// <returns>
        /// True if the buffer was accepted; otherwise, false.
        /// </returns>
        bool GiveBuffer(ref ByteList buffer);

        /// <summary>
        /// This method establishes a new, empty read buffer for this context.
        /// </summary>
        void NewBuffer();

        /// <summary>
        /// This method discards the buffered line-ending positions for this
        /// context.
        /// </summary>
        /// <returns>
        /// The number of line-ending positions that were discarded.
        /// </returns>
        int DiscardLineEndings();

        /// <summary>
        /// This method removes and returns the buffered line-ending positions
        /// for this context.
        /// </summary>
        /// <returns>
        /// The buffered line-ending positions, or null if none are present.
        /// </returns>
        IntList TakeLineEndings();

        /// <summary>
        /// This method gives buffered line-ending positions to this context.
        /// </summary>
        /// <param name="lineEndings">
        /// The buffered line-ending positions to give to this context.
        /// </param>
        /// <returns>
        /// True if the line-ending positions were accepted; otherwise, false.
        /// </returns>
        bool GiveLineEndings(ref IntList lineEndings);

        /// <summary>
        /// This method establishes a new, empty set of buffered line-ending
        /// positions for this context.
        /// </summary>
        void NewLineEndings();

        /// <summary>
        /// This method creates a partial clone of this context's channel stream
        /// that wraps the specified stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to be wrapped by the cloned channel stream.
        /// </param>
        /// <returns>
        /// The newly created channel stream.
        /// </returns>
        ChannelStream PartialCloneChannelStream(Stream stream);

        /// <summary>
        /// This method flushes any buffered output for this context.
        /// </summary>
        /// <returns>
        /// True if the flush succeeded; otherwise, false.
        /// </returns>
        bool Flush();

        /// <summary>
        /// This method closes the readers and writers associated with this
        /// context.
        /// </summary>
        /// <param name="preventClose">
        /// Non-zero to prevent the underlying stream from being closed when
        /// closing the readers and writers.
        /// </param>
        void CloseReadersAndWriters(bool preventClose);

        /// <summary>
        /// This method closes this context and releases its resources.
        /// </summary>
        void Close();
    }
}
