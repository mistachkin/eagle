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
    [ObjectId("0c2c603d-1cf9-49bc-9faf-415818a8e942")]
    internal sealed class ChannelContext : IChannelContext, IDisposable
    {
        #region Private Data
        private int disposeCount;

        ///////////////////////////////////////////////////////////////////////

        private ByteList buffer;
        private IntList lineEndings;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ChannelContext()
        {
            SetupThreadId();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        private void SetupThreadId()
        {
            if (threadId != 0)
                return;

            threadId = GlobalState.GetCurrentSystemThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public Interpreter Interpreter
        {
            get { CheckDisposed(); throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        private long threadId;
        public long ThreadId
        {
            get { CheckDisposed(); return threadId; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        private ChannelStream channelStream;
        public ChannelStream ChannelStream
        {
            get { CheckDisposed(); return channelStream; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BinaryReader binaryReader;
        public BinaryReader BinaryReader
        {
            get { CheckDisposed(); return binaryReader; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BinaryWriter binaryWriter;
        public BinaryWriter BinaryWriter
        {
            get { CheckDisposed(); return binaryWriter; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StreamReader streamReader;
        public StreamReader StreamReader
        {
            get { CheckDisposed(); return streamReader; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StreamWriter streamWriter;
        public StreamWriter StreamWriter
        {
            get { CheckDisposed(); return streamWriter; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasReader
        {
            get
            {
                CheckDisposed();

                return (binaryReader != null) || (streamReader != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasWriter
        {
            get
            {
                CheckDisposed();

                return (binaryWriter != null) || (streamWriter != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasBuffer
        {
            get
            {
                CheckDisposed();

                return (buffer != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public ByteList TakeBuffer()
        {
            CheckDisposed();

            ByteList buffer = this.buffer;

            this.buffer = null;

            return buffer;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public IntList TakeLineEndings()
        {
            CheckDisposed();

            IntList lineEndings = this.lineEndings;

            this.lineEndings = null;

            return lineEndings;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public void CloseReadersAndWriters(
            bool preventClose /* in */
            )
        {
            CheckDisposed();

            PrivateCloseReadersAndWriters(preventClose);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
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
        ~ChannelContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
