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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class represents the per-thread input/output state associated with
    /// a single Eagle channel.  It owns the underlying
    /// <see cref="ChannelStream" />, the lazily created binary and text readers
    /// and writers layered over that stream, and the pending byte buffer and
    /// line-ending offsets used while reading.  It implements
    /// <see cref="IChannelContext" /> and is disposable; disposing the context
    /// closes the readers, writers, and underlying stream it owns.
    /// </summary>
    [ObjectId("0c2c603d-1cf9-49bc-9faf-415818a8e942")]
    internal sealed class ChannelContext : IChannelContext, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The number of times this object has been disposed.
        /// </summary>
        private int disposeCount;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The pending buffer of bytes that have been read from the channel but
        /// not yet consumed.  This field may be null.
        /// </summary>
        private ByteList buffer;

        /// <summary>
        /// The list of buffer offsets at which line endings were detected.
        /// This field may be null.
        /// </summary>
        private IntList lineEndings;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty channel context and captures the identifier of
        /// the current thread.  This is the most basic constructor; the public
        /// constructor delegates to it.
        /// </summary>
        private ChannelContext()
        {
            SetupThreadId();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a channel context that wraps the specified channel
        /// stream.
        /// </summary>
        /// <param name="channelStream">
        /// The channel stream that this context will own and provide
        /// input/output access to.
        /// </param>
        public ChannelContext(
            ChannelStream channelStream /* in */
            )
            : this()
        {
            this.channelStream = channelStream;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method records the identifier of the current system thread as
        /// the owning thread of this context, unless it has already been set.
        /// </summary>
        private void SetupThreadId()
        {
            if (threadId != 0)
                return;

            threadId = GlobalState.GetCurrentSystemThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes any binary or text readers and writers that have
        /// been opened over the channel stream, optionally preventing the
        /// underlying stream itself from being closed as a side effect of
        /// closing those readers and writers.
        /// </summary>
        /// <param name="preventClose">
        /// Non-zero to prevent the underlying channel stream from being closed
        /// while the readers and writers are closed; the stream's prior setting
        /// is restored afterward.
        /// </param>
        private void PrivateCloseReadersAndWriters(
            bool preventClose /* in */
            )
        {
            bool? savedPreventClose = null;

            if (channelStream != null)
            {
                //
                // NOTE: Here we workaround a "design flaw" in the .NET
                //       Framework by preventing the stream itself from
                //       being closed merely by closing any readers and
                //       writers that we may have open.
                //
                savedPreventClose = channelStream.PreventClose;
                channelStream.PreventClose = preventClose;
            }

            try
            {
                if (streamWriter != null)
                {
                    streamWriter.Close();
                    streamWriter = null;
                }

                if (streamReader != null)
                {
                    streamReader.Close();
                    streamReader = null;
                }

                if (binaryWriter != null)
                {
                    binaryWriter.Close();
                    binaryWriter = null;
                }

                if (binaryReader != null)
                {
                    binaryReader.Close();
                    binaryReader = null;
                }
            }
            finally
            {
                if ((channelStream != null) && (savedPreventClose != null))
                {
                    //
                    // NOTE: Restore the ability of the stream itself to
                    //       actually be closed.  This is part of the
                    //       workaround mentioned above and is necessary
                    //       only because the .NET Framework is broken
                    //       with regard to StreamReader/Writer objects.
                    //
                    channelStream.PreventClose = (bool)savedPreventClose;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// Gets the interpreter associated with this channel context.  This
        /// member is not supported and always throws
        /// <see cref="NotImplementedException" />.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        /// <summary>
        /// The identifier of the thread that created this channel context.
        /// </summary>
        private long threadId;

        /// <summary>
        /// Gets the identifier of the thread that created this channel context.
        /// </summary>
        public long ThreadId
        {
            get { CheckDisposed(); return threadId; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this object has been disposed.  True
        /// if this object has been disposed; otherwise, false.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this object is currently being
        /// disposed.  True if disposal of this object is in progress;
        /// otherwise, false.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return Interlocked.CompareExchange(
                    ref disposeCount, 0, 0) > 0;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IChannelContext Members
        /// <summary>
        /// The channel stream owned by this context.  This field may be null.
        /// </summary>
        private ChannelStream channelStream;

        /// <summary>
        /// Gets the channel stream owned by this context.
        /// </summary>
        public ChannelStream ChannelStream
        {
            get { CheckDisposed(); return channelStream; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The binary reader layered over the channel stream.  This field may
        /// be null until first requested.
        /// </summary>
        private BinaryReader binaryReader;

        /// <summary>
        /// Gets the binary reader layered over the channel stream, if any.
        /// </summary>
        public BinaryReader BinaryReader
        {
            get { CheckDisposed(); return binaryReader; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The binary writer layered over the channel stream.  This field may
        /// be null until first requested.
        /// </summary>
        private BinaryWriter binaryWriter;

        /// <summary>
        /// Gets the binary writer layered over the channel stream, if any.
        /// </summary>
        public BinaryWriter BinaryWriter
        {
            get { CheckDisposed(); return binaryWriter; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The text stream reader layered over the channel stream.  This field
        /// may be null until first requested.
        /// </summary>
        private StreamReader streamReader;

        /// <summary>
        /// Gets the text stream reader layered over the channel stream, if any.
        /// </summary>
        public StreamReader StreamReader
        {
            get { CheckDisposed(); return streamReader; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The text stream writer layered over the channel stream.  This field
        /// may be null until first requested.
        /// </summary>
        private StreamWriter streamWriter;

        /// <summary>
        /// Gets the text stream writer layered over the channel stream, if any.
        /// </summary>
        public StreamWriter StreamWriter
        {
            get { CheckDisposed(); return streamWriter; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this context has an open binary or
        /// text reader.  True if a reader is open; otherwise, false.
        /// </summary>
        public bool HasReader
        {
            get
            {
                CheckDisposed();

                return (binaryReader != null) || (streamReader != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this context has an open binary or
        /// text writer.  True if a writer is open; otherwise, false.
        /// </summary>
        public bool HasWriter
        {
            get
            {
                CheckDisposed();

                return (binaryWriter != null) || (streamWriter != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this context has an allocated pending
        /// buffer.  True if a buffer has been allocated; otherwise, false.
        /// </summary>
        public bool HasBuffer
        {
            get
            {
                CheckDisposed();

                return (buffer != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the pending buffer is missing or
        /// empty.  True if there is no buffer or the buffer contains no bytes;
        /// otherwise, false.
        /// </summary>
        public bool HasEmptyBuffer
        {
            get
            {
                CheckDisposed();

                ByteList buffer = this.buffer;

                if (buffer == null)
                    return true;

                return (buffer.Count == 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the binary reader for the channel stream,
        /// creating it on first use with the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the binary reader.  This parameter
        /// may be null, in which case a default encoding is used.
        /// </param>
        /// <returns>
        /// The binary reader for the channel stream, or null if there is no
        /// channel stream.
        /// </returns>
        public BinaryReader GetBinaryReader(
            Encoding encoding /* in */
            )
        {
            CheckDisposed();

            if ((channelStream != null) && (binaryReader == null))
            {
                if (encoding != null)
                {
                    binaryReader = new BinaryReader(
                        channelStream, encoding);
                }
                else
                {
                    binaryReader = new BinaryReader(
                        channelStream);
                }
            }

            return binaryReader;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the binary writer for the channel stream,
        /// creating it on first use with the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the binary writer.  This parameter
        /// may be null, in which case a default encoding is used.
        /// </param>
        /// <returns>
        /// The binary writer for the channel stream, or null if there is no
        /// channel stream.
        /// </returns>
        public BinaryWriter GetBinaryWriter(
            Encoding encoding /* in */
            )
        {
            CheckDisposed();

            if ((channelStream != null) && (binaryWriter == null))
            {
                if (encoding != null)
                {
                    binaryWriter = new BinaryWriter(
                        channelStream, encoding);
                }
                else
                {
                    binaryWriter = new BinaryWriter(
                        channelStream);
                }
            }

            return binaryWriter;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the text stream reader for the channel stream,
        /// creating it on first use with the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the stream reader.  This parameter
        /// may be null, in which case a default encoding is used.
        /// </param>
        /// <returns>
        /// The text stream reader for the channel stream, or null if there is
        /// no channel stream.
        /// </returns>
        public StreamReader GetStreamReader(
            Encoding encoding /* in */
            )
        {
            CheckDisposed();

            if ((channelStream != null) && (streamReader == null))
            {
                if (encoding != null)
                {
                    streamReader = new StreamReader(
                        channelStream, encoding);
                }
                else
                {
                    streamReader = new StreamReader(
                        channelStream);
                }

                //
                // BUGBUG: Why does the .NET Framework reset the position
                //         to be the end of the stream upon creating a
                //         stream reader or writer on the stream?
                //
                // if (!seekBegin && streamReader.BaseStream.CanSeek)
                // {
                //     streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                //     seekBegin = true;
                // }
            }

            return streamReader;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the text stream writer for the channel stream,
        /// creating it on first use with the specified encoding if necessary.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use when creating the stream writer.  This parameter
        /// may be null, in which case a default encoding is used.
        /// </param>
        /// <returns>
        /// The text stream writer for the channel stream, or null if there is
        /// no channel stream.
        /// </returns>
        public StreamWriter GetStreamWriter(
            Encoding encoding /* in */
            )
        {
            CheckDisposed();

            if ((channelStream != null) && (streamWriter == null))
            {
                if (encoding != null)
                {
                    streamWriter = new StreamWriter(
                        channelStream, encoding);
                }
                else
                {
                    streamWriter = new StreamWriter(
                        channelStream);
                }

                //
                // BUGBUG: Why does the .NET Framework reset the position
                //         to be the end of the stream upon creating a
                //         stream reader or writer on the stream?
                //
                // if (!seekBegin && streamWriter.BaseStream.CanSeek)
                // {
                //     streamWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                //     seekBegin = true;
                // }
            }

            return streamWriter;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the pending buffer, discarding any bytes it
        /// contains.
        /// </summary>
        /// <returns>
        /// The number of bytes that were in the buffer prior to clearing it, or
        /// <see cref="Count.Invalid" /> if there is no buffer.
        /// </returns>
        public int DiscardBuffer()
        {
            CheckDisposed();

            ByteList buffer = this.buffer;

            if (buffer == null)
                return Count.Invalid;

            int result = buffer.Count;

            buffer.Clear();

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the pending buffer from this context and returns
        /// it to the caller, leaving this context without a buffer.
        /// </summary>
        /// <returns>
        /// The pending buffer that was held by this context, or null if there
        /// was no buffer.
        /// </returns>
        public ByteList TakeBuffer()
        {
            CheckDisposed();

            ByteList buffer = this.buffer;

            this.buffer = null;

            return buffer;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method installs the specified buffer as the pending buffer for
        /// this context, discarding and clearing any previously held buffer.
        /// </summary>
        /// <param name="buffer">
        /// On input, the buffer to install as the pending buffer.  On output,
        /// this is set to null when the buffer is accepted.
        /// </param>
        /// <returns>
        /// True if the supplied buffer was accepted and installed; otherwise,
        /// false.
        /// </returns>
        public bool GiveBuffer(
            ref ByteList buffer /* in, out */
            )
        {
            CheckDisposed();

            if (buffer != null)
            {
                ByteList savedBuffer = this.buffer;

                this.buffer = buffer;
                buffer = null;

                if (savedBuffer != null)
                {
                    savedBuffer.Clear();
                    savedBuffer = null;
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method allocates a fresh, empty pending buffer for this context,
        /// discarding and clearing any previously held buffer.
        /// </summary>
        public void NewBuffer()
        {
            CheckDisposed();

            ByteList savedBuffer = this.buffer;

            this.buffer = new ByteList();

            if (savedBuffer != null)
            {
                savedBuffer.Clear();
                savedBuffer = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the list of recorded line-ending offsets,
        /// discarding any entries it contains.
        /// </summary>
        /// <returns>
        /// The number of line-ending offsets that were recorded prior to
        /// clearing the list, or <see cref="Count.Invalid" /> if there is no
        /// list.
        /// </returns>
        public int DiscardLineEndings()
        {
            CheckDisposed();

            IntList lineEndings = this.lineEndings;

            if (lineEndings == null)
                return Count.Invalid;

            int result = lineEndings.Count;

            lineEndings.Clear();

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the list of recorded line-ending offsets from
        /// this context and returns it to the caller, leaving this context
        /// without such a list.
        /// </summary>
        /// <returns>
        /// The list of line-ending offsets that was held by this context, or
        /// null if there was no list.
        /// </returns>
        public IntList TakeLineEndings()
        {
            CheckDisposed();

            IntList lineEndings = this.lineEndings;

            this.lineEndings = null;

            return lineEndings;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method installs the specified list as the recorded line-ending
        /// offsets for this context, discarding and clearing any previously held
        /// list.
        /// </summary>
        /// <param name="lineEndings">
        /// On input, the list of line-ending offsets to install.  On output,
        /// this is set to null when the list is accepted.
        /// </param>
        /// <returns>
        /// True if the supplied list was accepted and installed; otherwise,
        /// false.
        /// </returns>
        public bool GiveLineEndings(
            ref IntList lineEndings
            )
        {
            CheckDisposed();

            if (lineEndings != null)
            {
                IntList savedLineEndings = this.lineEndings;

                this.lineEndings = lineEndings;
                lineEndings = null;

                if (savedLineEndings != null)
                {
                    savedLineEndings.Clear();
                    savedLineEndings = null;
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method allocates a fresh, empty list of line-ending offsets for
        /// this context, discarding and clearing any previously held list.
        /// </summary>
        public void NewLineEndings()
        {
            CheckDisposed();

            IntList savedLineEndings = this.lineEndings;

            this.lineEndings = new IntList();

            if (savedLineEndings != null)
            {
                savedLineEndings.Clear();
                savedLineEndings = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a partial clone of this context's channel stream
        /// that wraps the specified underlying stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream that the cloned channel stream will wrap.
        /// </param>
        /// <returns>
        /// A partial clone of this context's channel stream, or null if there
        /// is no channel stream.
        /// </returns>
        public ChannelStream PartialCloneChannelStream(
            Stream stream /* in */
            )
        {
            CheckDisposed();

            if (channelStream == null)
                return null;

            return channelStream.PartialClone(stream);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any open writers and the underlying channel
        /// stream, provided the stream supports writing.
        /// </summary>
        /// <returns>
        /// True if at least one writer was flushed; otherwise, false.
        /// </returns>
        public bool Flush()
        {
            CheckDisposed();

            bool flushed = false;

            if ((channelStream != null) && channelStream.CanWrite)
            {
                if (binaryWriter != null)
                {
                    binaryWriter.Flush();
                    flushed = true;
                }

                if (streamWriter != null)
                {
                    streamWriter.Flush();
                    flushed = true;
                }

                //
                // NOTE: Finally, flush the stream itself.
                //
                channelStream.Flush();
            }

            return flushed;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes any open binary or text readers and writers,
        /// optionally preventing the underlying channel stream itself from being
        /// closed as a side effect.
        /// </summary>
        /// <param name="preventClose">
        /// Non-zero to prevent the underlying channel stream from being closed
        /// while the readers and writers are closed.
        /// </param>
        public void CloseReadersAndWriters(
            bool preventClose /* in */
            )
        {
            CheckDisposed();

            PrivateCloseReadersAndWriters(preventClose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes any open readers and writers and then closes the
        /// underlying channel stream itself.
        /// </summary>
        public void Close()
        {
            CheckDisposed();

            PrivateCloseReadersAndWriters(true);

            if (channelStream != null)
            {
                channelStream.Close();
                channelStream = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this channel context,
        /// which is the string representation of its underlying channel stream.
        /// </summary>
        /// <returns>
        /// The string representation of the underlying channel stream, or null
        /// if there is no channel stream.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            if (channelStream == null)
                return null;

            return channelStream.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources owned by this channel context,
        /// closing its readers, writers, and underlying channel stream, and
        /// suppresses finalization of this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws <see cref="ObjectDisposedException" /> if this
        /// object has been disposed and the engine is configured to throw on
        /// access to disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(ChannelContext).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this channel context.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.  When non-zero, managed resources are released in addition
        /// to unmanaged resources.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            if (!disposed)
            {
                if (Interlocked.Increment(ref disposeCount) == 1)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        PrivateCloseReadersAndWriters(true); /* throw */

                        ///////////////////////////////////////////////////////

                        if (channelStream != null)
                        {
                            channelStream.Dispose(); /* throw */
                            channelStream = null;
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this channel context, releasing any unmanaged resources it
        /// still owns.
        /// </summary>
        ~ChannelContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
