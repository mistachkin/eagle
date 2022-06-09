/*
 * ChannelStream.cs --
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

#if NETWORK
using System.Net.Sockets;
#endif

#if REMOTING
using System.Runtime.Remoting;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("b9b9bfc0-b902-4476-afb9-116ddec7a779")]
    internal class ChannelStream : Stream /* BASE CLASS NOT USED */
    {
        #region Public Constants
        public const byte NewLine = (byte)Characters.NewLine; /* [puts] */

        ///////////////////////////////////////////////////////////////////////

        public static readonly int EndOfFile = -1; /* [gets], et al */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private const byte CarriageReturn = (byte)Characters.CarriageReturn;
        private const byte LineFeed = (byte)Characters.LineFeed;

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        //
        // HACK: This is purposely not read-only.
        //
        private static int MinimumPollTimeout = 25; /* milliseconds */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region End-of-Line Static Data
        internal static readonly CharList LineFeedCharList =
            new CharList(new char[] { Characters.LineFeed });

        ///////////////////////////////////////////////////////////////////////

        private static readonly CharList CarriageReturnCharList =
            new CharList(new char[] { Characters.CarriageReturn });

        ///////////////////////////////////////////////////////////////////////

        internal static readonly CharList CarriageReturnLineFeedCharList =
            new CharList(new char[] {
            Characters.CarriageReturn, Characters.LineFeed
        });
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private ChannelType channelType;
        private OptionDictionary options; // ORIGINAL options when opening.
        private Stream stream;
        private StreamFlags flags;
        private StreamTranslation inTranslation;
        private StreamTranslation outTranslation;

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        private int? availableTimeout;

        ///////////////////////////////////////////////////////////////////////

        private TcpListener listener;
        private Socket socket;
        private int timeout;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ChannelStream()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
#if NETWORK
        internal ChannelStream(
            TcpListener listener,     /* in */
            ChannelType channelType,  /* in */
            OptionDictionary options, /* in */
            StreamFlags flags         /* in */
            )
            : this()
        {
            this.listener = listener;
            this.channelType = channelType;
            this.options = options;
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        internal ChannelStream(
            Socket socket,                   /* in */
            int timeout,                     /* in */
            ChannelType channelType,         /* in */
            OptionDictionary options,        /* in */
            StreamFlags flags,               /* in */
            StreamTranslation inTranslation, /* in */
            StreamTranslation outTranslation /* in */
            )
            : this()
        {
            this.socket = socket;
            this.timeout = timeout;
            this.channelType = channelType;
            this.options = options;
            this.flags = flags;
            this.inTranslation = inTranslation;
            this.outTranslation = outTranslation;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        internal ChannelStream(
            Stream stream,                   /* in */
            ChannelType channelType,         /* in */
            OptionDictionary options,        /* in */
            StreamFlags flags,               /* in */
            StreamTranslation inTranslation, /* in */
            StreamTranslation outTranslation /* in */
            )
            : this()
        {
            this.stream = stream;
            this.channelType = channelType;
            this.options = options;
            this.flags = flags;
            this.inTranslation = inTranslation;
            this.outTranslation = outTranslation;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Partial Clone Methods
        public ChannelStream PartialClone(
            Stream stream /* in */
            )
        {
            CheckDisposed();

            return new ChannelStream(
                stream, this.channelType, this.options, this.flags,
                this.inTranslation, this.outTranslation);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Data Accessor Members
#if NETWORK
        public virtual TcpListener GetListener()
        {
            CheckDisposed();

            return listener;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Socket GetSocket()
        {
            CheckDisposed();

            return socket;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public virtual OptionDictionary GetOptions()
        {
            CheckDisposed();

            return options;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual Stream GetStream()
        {
            CheckDisposed();

            return stream;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Channel Type Members
#if CONSOLE
        public virtual bool IsConsole()
        {
            CheckDisposed();

            return FlagOps.HasFlags(
                channelType, ChannelType.Console, true);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region General Stream Flags Members
        public virtual bool HasFlags(
            StreamFlags flags, /* in */
            bool all           /* in */
            )
        {
            CheckDisposed();

            if (all)
                return ((this.flags & flags) == flags);
            else
                return ((this.flags & flags) != StreamFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StreamFlags SetFlags(
            StreamFlags flags, /* in */
            bool set           /* in */
            )
        {
            CheckDisposed();

            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool PreventClose
        {
            get
            {
                CheckDisposed();

                return HasFlags(StreamFlags.PreventClose, true);
            }
            set
            {
                CheckDisposed();

                SetFlags(StreamFlags.PreventClose, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool NeedBuffer
        {
            get
            {
                CheckDisposed();

                return HasFlags(StreamFlags.NeedBuffer, true);
            }
            set
            {
                CheckDisposed();

                SetFlags(StreamFlags.NeedBuffer, value);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region End-of-Line Translation Members
        public virtual StreamTranslation InputTranslation
        {
            get { CheckDisposed(); return inTranslation; }
            set { CheckDisposed(); inTranslation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation OutputTranslation
        {
            get { CheckDisposed(); return outTranslation; }
            set { CheckDisposed(); outTranslation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentInputTranslation()
        {
            CheckDisposed();

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                //
                // NOTE: Always assume cr/lf on windows.
                //
                return StreamTranslation.crlf;
            }
            else
            {
                //
                // FIXME: Assumes Unix.
                //
                return StreamTranslation.lf;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentInputTranslation(
            StreamTranslation translation /* in */
            )
        {
            CheckDisposed();

            return (translation == StreamTranslation.environment) ?
                GetEnvironmentInputTranslation() : translation;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentOutputTranslation()
        {
            CheckDisposed();

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                //
                // NOTE: Always use cr/lf on windows.
                //
                return StreamTranslation.protocol;
            }
            else
            {
                //
                // FIXME: Assumes Unix.
                //
                return StreamTranslation.lf;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual StreamTranslation GetEnvironmentOutputTranslation(
            StreamTranslation translation /* in */
            )
        {
            CheckDisposed();

            return (translation == StreamTranslation.environment) ?
                GetEnvironmentOutputTranslation() : translation;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual CharList InputEndOfLine
        {
            get
            {
                CheckDisposed();

                switch (GetEnvironmentInputTranslation(inTranslation))
                {
                    case StreamTranslation.lf:
                        return LineFeedCharList;
                    case StreamTranslation.cr:
                        return CarriageReturnCharList;
                    case StreamTranslation.crlf:
                    case StreamTranslation.platform:
                    case StreamTranslation.auto:
                        return CarriageReturnLineFeedCharList;
                    default:
                        return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool UseAnyEndOfLineChar
        {
            get
            {
                CheckDisposed();

                return HasFlags(StreamFlags.UseAnyEndOfLineChar, true);
            }
            set
            {
                CheckDisposed();

                SetFlags(StreamFlags.UseAnyEndOfLineChar, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool KeepEndOfLineChars
        {
            get
            {
                CheckDisposed();

                return HasFlags(StreamFlags.KeepEndOfLineChars, true);
            }
            set
            {
                CheckDisposed();

                SetFlags(StreamFlags.KeepEndOfLineChars, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual CharList OutputEndOfLine
        {
            get
            {
                CheckDisposed();

                switch (GetEnvironmentOutputTranslation(outTranslation))
                {
                    case StreamTranslation.lf:
                        return LineFeedCharList;
                    case StreamTranslation.cr:
                        return CarriageReturnCharList;
                    case StreamTranslation.crlf:
                    case StreamTranslation.platform:
                    case StreamTranslation.auto:
                        return CarriageReturnLineFeedCharList;
                    default:
                        return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal virtual int TranslateInputEndOfLine(
            byte[] inBuffer,  /* in */
            byte[] outBuffer, /* in, out */
            int offset,       /* in */
            int count         /* in */
            )
        {
            switch (GetEnvironmentInputTranslation(inTranslation))
            {
                case StreamTranslation.binary:
                case StreamTranslation.lf:
                case StreamTranslation.protocol:
                    {
                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, count);

                        return count;
                    }
                case StreamTranslation.cr:
                    {
                        int newCount = offset + count;

                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, count);

                        for (int outIndex = offset; outIndex < newCount;
                                outIndex++)
                        {
                            if (outBuffer[outIndex] == CarriageReturn)
                                outBuffer[outIndex] = NewLine;
                        }

                        return count;
                    }
                case StreamTranslation.crlf:
                case StreamTranslation.platform:
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        if (HasFlags(StreamFlags.NeedLineFeed, true) &&
                            (inIndex < newCount))
                        {
                            if (inBuffer[inIndex] == LineFeed)
                            {
                                outBuffer[outIndex++] = NewLine;
                            }
                            else
                            {
                                outBuffer[outIndex++] = inBuffer[inIndex++];
                            }

                            SetFlags(StreamFlags.NeedLineFeed, false);
                        }

                        for (; inIndex < newCount; )
                        {
                            if (inBuffer[inIndex] == CarriageReturn)
                            {
                                if (++inIndex >= newCount)
                                {
                                    SetFlags(StreamFlags.NeedLineFeed, true);
                                }
                                else if (inBuffer[inIndex] == LineFeed)
                                {
                                    outBuffer[outIndex++] = inBuffer[inIndex++];
                                }
                                else
                                {
                                    //
                                    // NOTE: This is a carriage-return (?).
                                    //
                                    outBuffer[outIndex++] = inBuffer[inIndex - 1];
                                }
                            }
                            else
                            {
                                outBuffer[outIndex++] = inBuffer[inIndex++];
                            }
                        }

                        return outIndex;
                    }
                case StreamTranslation.auto:
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        if (HasFlags(StreamFlags.SawCarriageReturn, true) &&
                            (inIndex < newCount))
                        {
                            if (inBuffer[inIndex] == LineFeed)
                                inIndex++;

                            SetFlags(StreamFlags.SawCarriageReturn, false);
                        }

                        for (; inIndex < newCount; )
                        {
                            if (inBuffer[inIndex] == CarriageReturn)
                            {
                                if (++inIndex >= newCount)
                                {
                                    SetFlags(
                                        StreamFlags.SawCarriageReturn, true);
                                }
                                else if (inBuffer[inIndex] == LineFeed)
                                {
                                    inIndex++;
                                }

                                outBuffer[outIndex++] = NewLine;
                            }
                            else
                            {
                                outBuffer[outIndex++] = inBuffer[inIndex++];
                            }
                        }

                        return outIndex;
                    }
                default:
                    {
                        return 0;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal virtual int TranslateOutputEndOfLine(
            byte[] inBuffer,  /* in */
            byte[] outBuffer, /* in, out */
            int offset,       /* in */
            int count         /* in */
            )
        {
            switch (GetEnvironmentOutputTranslation(outTranslation))
            {
                case StreamTranslation.binary:
                case StreamTranslation.lf:
                    {
                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, count);

                        return count;
                    }
                case StreamTranslation.cr:
                    {
                        int newCount = offset + count;

                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, count);

                        for (int outIndex = offset; outIndex < newCount;
                                outIndex++)
                        {
                            if (outBuffer[outIndex] == LineFeed)
                                outBuffer[outIndex] = CarriageReturn;
                        }

                        return count;
                    }
                case StreamTranslation.crlf:
                case StreamTranslation.platform:
                case StreamTranslation.auto:
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        for (; inIndex < newCount; )
                        {
                            if (inBuffer[inIndex] == LineFeed)
                                outBuffer[outIndex++] = CarriageReturn;

                            outBuffer[outIndex++] = inBuffer[inIndex++];
                        }

                        return outIndex;
                    }
                case StreamTranslation.protocol: /* NOTE: Enforce CR/LF. */
                    {
                        int newCount = offset + count;
                        int inIndex = offset;
                        int outIndex = 0;

                        for (; inIndex < newCount; )
                        {
                            //
                            // NOTE: Have we seen an unpaired carriage-return?
                            //
                            bool sawCarriageReturn = HasFlags(
                                StreamFlags.SawCarriageReturn, true);

                            //
                            // NOTE: Is the current character carriage-return?
                            //
                            if (inBuffer[inIndex] == CarriageReturn)
                            {
                                //
                                // NOTE: If we have already seen an unpaired
                                //       carriage-return we need to add a
                                //       line-feed now before doing anything
                                //       else to complete the pairing.
                                //
                                if (sawCarriageReturn)
                                    outBuffer[outIndex++] = LineFeed;

                                //
                                // NOTE: Emit the input character (which is
                                //       a carriage-return).
                                //
                                outBuffer[outIndex++] = inBuffer[inIndex++];

                                //
                                // NOTE: We just emitted an unpaired carriage-
                                //       return.  If there are more characters
                                //       to process, we can just set the flag
                                //       to indicate an unpaired carriage-
                                //       return; otherwise, we must emit the
                                //       line-feed now to complete the pairing
                                //       because there are no more characters.
                                //
                                if (inIndex >= newCount)
                                {
                                    outBuffer[outIndex++] = LineFeed;
                                }
                                else
                                {
                                    SetFlags(
                                        StreamFlags.SawCarriageReturn, true);
                                }
                            }
                            //
                            // NOTE: Otherwise, is current character line-feed?
                            //
                            else if (inBuffer[inIndex] == LineFeed)
                            {
                                //
                                // NOTE: If we have not seen an unpaired
                                //       carriage-return yet, we need to add
                                //       one now for the pairing to be complete
                                //       when we emit the line-feed below.
                                //
                                if (!sawCarriageReturn)
                                    outBuffer[outIndex++] = CarriageReturn;

                                //
                                // NOTE: Emit the input character (which is
                                //       line-feed) to complete the pairing.
                                //
                                outBuffer[outIndex++] = inBuffer[inIndex++];

                                //
                                // NOTE: Now, if we had previously seen an
                                //       unpaired carriage-return, reset the
                                //       flag now because we just completed
                                //       the pairing.
                                //
                                if (sawCarriageReturn)
                                {
                                    SetFlags(
                                        StreamFlags.SawCarriageReturn, false);
                                }
                            }
                            else
                            {
                                //
                                // NOTE: If we have seen an unpaired carriage-
                                //       return we need to add a line-feed now
                                //       before doing anything else to complete
                                //       the pairing.
                                //
                                if (sawCarriageReturn)
                                    outBuffer[outIndex++] = LineFeed;

                                //
                                // NOTE: Emit the input character.
                                //
                                outBuffer[outIndex++] = inBuffer[inIndex++];

                                //
                                // NOTE: Now, if we had previously seen an
                                //       unpaired carriage-return, reset the
                                //       flag now because we completed the
                                //       pairing above.
                                //
                                if (sawCarriageReturn)
                                {
                                    SetFlags(
                                        StreamFlags.SawCarriageReturn, false);
                                }
                            }
                        }

                        return outIndex;
                    }
                default:
                    {
                        return 0;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.IO.Stream Overrides
        public override IAsyncResult BeginRead(
            byte[] buffer,          /* in, out */
            int offset,             /* in */
            int count,              /* in */
            AsyncCallback callback, /* in */
            object state
            )
        {
            CheckDisposed();

            return stream.BeginRead(
                buffer, offset, count, callback, state);
        }

        ///////////////////////////////////////////////////////////////////////

        public override IAsyncResult BeginWrite(
            byte[] buffer,          /* in */
            int offset,             /* in */
            int count,              /* in */
            AsyncCallback callback, /* in */
            object state            /* in */
            )
        {
            CheckDisposed();

            return stream.BeginWrite(
                buffer, offset, count, callback, state);
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool CanRead
        {
            get { CheckDisposed(); return stream.CanRead; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool CanSeek
        {
            get { CheckDisposed(); return stream.CanSeek; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool CanTimeout
        {
            get { CheckDisposed(); return stream.CanTimeout; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool CanWrite
        {
            get { CheckDisposed(); return stream.CanWrite; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override void Close()
        {
            CheckDisposed();

            if (!PreventClose)
            {
#if NETWORK
                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }

                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close(timeout);
                    socket = null;
                }
#endif

                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        protected override WaitHandle CreateWaitHandle()
        {
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        public override int EndRead(
            IAsyncResult asyncResult /* in */
            )
        {
            CheckDisposed();

            return stream.EndRead(asyncResult);
        }

        ///////////////////////////////////////////////////////////////////////

        public override void EndWrite(
            IAsyncResult asyncResult /* in */
            )
        {
            CheckDisposed();

            stream.EndWrite(asyncResult);
        }

        ///////////////////////////////////////////////////////////////////////

        public override void Flush()
        {
            CheckDisposed();

            stream.Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        public override long Length
        {
            get { CheckDisposed(); return stream.Length; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override long Position
        {
            get { CheckDisposed(); return stream.Position; }
            set { CheckDisposed(); stream.Position = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override int Read(
            byte[] buffer, /* in, out */
            int offset,    /* in */
            int count      /* in */
            )
        {
            CheckDisposed();

            if (inTranslation != StreamTranslation.binary)
            {
                byte[] input = new byte[count];

                int newCount = stream.Read(input, 0, count);

                return TranslateInputEndOfLine(
                    input, buffer, offset, newCount);
            }
            else
            {
                return stream.Read(buffer, offset, count);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public override int ReadByte()
        {
            CheckDisposed();

            return stream.ReadByte();
        }

        ///////////////////////////////////////////////////////////////////////

        public override int ReadTimeout
        {
            get { CheckDisposed(); return stream.ReadTimeout; }
            set { CheckDisposed(); stream.ReadTimeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override long Seek(
            long offset,      /* in */
            SeekOrigin origin /* in */
            )
        {
            CheckDisposed();

            return stream.Seek(offset, origin);
        }

        ///////////////////////////////////////////////////////////////////////

        public override void SetLength(
            long value /* in */
            )
        {
            CheckDisposed();

            stream.SetLength(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public override void Write(
            byte[] buffer, /* in */
            int offset,    /* in */
            int count      /* in */
            )
        {
            CheckDisposed();

            if (outTranslation != StreamTranslation.binary)
            {
                int oldCount = offset + count;
                int newCount = count;

                for (int inIndex = offset; inIndex < oldCount; inIndex++)
                {
                    char character = (char)buffer[inIndex];

                    if ((character == Characters.CarriageReturn) ||
                        (character == Characters.LineFeed))
                    {
                        //
                        // NOTE: Every line terminator may double.
                        //
                        newCount += 2;
                    }
                }

                byte[] output = new byte[newCount];

                newCount = TranslateOutputEndOfLine(
                    buffer, output, offset, count);

                stream.Write(output, 0, newCount);
            }
            else
            {
                stream.Write(buffer, offset, count);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public override void WriteByte(
            byte value /* in */
            )
        {
            CheckDisposed();

            stream.WriteByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int WriteTimeout
        {
            get { CheckDisposed(); return stream.WriteTimeout; }
            set { CheckDisposed(); stream.WriteTimeout = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Net.Sockets.NetworkStream Members
        public virtual bool DataAvailable
        {
            get
            {
                CheckDisposed();

#if NETWORK
                NetworkStream networkStream = stream as NetworkStream;

                if (networkStream == null)
                    return false;

                return networkStream.DataAvailable;
#else
                return false;
#endif
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ChannelStream Members
        public virtual int Available
        {
            get
            {
                CheckDisposed();

#if NETWORK
                NetworkStream networkStream = stream as NetworkStream;

                if (networkStream != null)
                {
                    Socket socket = SocketOps.GetSocket(networkStream);

                    if (socket != null)
                    {
                        int count = socket.Available;

                        if (count > 0)
                            return count;

                        int? timeout = PollTimeout;

                        if ((timeout != null) && socket.Poll(
                                (int)PerformanceOps.GetMicroseconds(
                                (int)timeout), SelectMode.SelectRead))
                        {
                            count = socket.Available;
                        }

                        return count;
                    }
                }
#endif

                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        private bool TryGetAvailableTimeout(
            out int availableTimeout
            )
        {
            if (this.availableTimeout != null)
            {
                availableTimeout = (int)this.availableTimeout;
                return true;
            }
            else
            {
                availableTimeout = 0;
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int? AvailableTimeout
        {
            get { CheckDisposed(); return availableTimeout; }
            set { CheckDisposed(); availableTimeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int? PollTimeout /* NOTE: Consume one timeout "chunk". */
        {
            get
            {
                CheckDisposed();

                int availableTimeout;

                if (!TryGetAvailableTimeout(out availableTimeout))
                    return null;

                try
                {
                    int pollTimeout = MinimumPollTimeout;

                    if (availableTimeout >= pollTimeout)
                    {
                        availableTimeout -= pollTimeout;
                    }
                    else
                    {
                        pollTimeout = availableTimeout;
                        availableTimeout = 0;
                    }

                    return pollTimeout;
                }
                finally
                {
                    AvailableTimeout = availableTimeout;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public virtual int ReadCount
        {
            get
            {
                CheckDisposed();

                //
                // NOTE: Only attempt to query the length of seekable
                //       streams.
                //
                if ((stream != null) && stream.CanSeek)
                    return (int)stream.Length;

                //
                // NOTE: Otherwise, if there is a specific number of
                //       bytes available, use that.
                //
                int count = Available;

                if (count != 0)
                    return count;

                //
                // NOTE: In this context, a return value of zero is
                //       used to indicate that there is not an exact
                //       number of bytes that need to be read (i.e.
                //       read one byte at a time until end-of-line,
                //       end-of-file, etc).
                //
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool PopulateBuffer(
            ref ByteList buffer /* in, out */
            )
        {
            CheckDisposed();

            if (stream == null)
                return false;

            int inCount = ReadCount; /* EXPENSIVE */

            if (inCount == 0)
                return false;

            byte[] inBytes = new byte[inCount];

            int outCount = stream.Read(inBytes, 0, inCount);

            if (outCount == 0)
                return false;

            Array.Resize(ref inBytes, outCount);

            byte[] outBytes = new byte[outCount];

            outCount = TranslateInputEndOfLine(
                inBytes, outBytes, 0, outCount);

            Array.Resize(ref outBytes, outCount);

            if (buffer != null)
                buffer.AddRange(outBytes);
            else
                buffer = new ByteList(outBytes);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(
            object obj /* in */
            )
        {
            CheckDisposed();

            return stream.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            CheckDisposed();

            return stream.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            CheckDisposed();

            return stream.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.MarshalByRefObject Overrides
#if REMOTING
        public override ObjRef CreateObjRef(
            Type requestedType /* in */
            )
        {
            CheckDisposed();

            return stream.CreateObjRef(requestedType);
        }

        ///////////////////////////////////////////////////////////////////////

        public override object InitializeLifetimeService()
        {
            CheckDisposed();

            return stream.InitializeLifetimeService();
        }
#endif
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
                    typeof(ChannelStream).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void Dispose(
            bool disposing /* in */
            )
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        Close();
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                //
                // NOTE: This is not necessary because
                //       we do not use our base class.
                //
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~ChannelStream()
        {
            Dispose(false);
        }
        #endregion
    }
}
