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

using System;
using System.Collections.Generic;
using System.IO;

#if NETWORK
using System.Net.Sockets;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using _Length = Eagle._Constants.Length;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("a35ad515-f878-426c-8073-bfc5aee4658e")]
    internal sealed class Channel : IChannel, IDisposable
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static bool DefaultInputNullEncoding = true; // COMPAT: Eagle.
        private static bool DefaultOutputNullEncoding = true; // COMPAT: Eagle.
        private static bool DefaultErrorNullEncoding = true; // COMPAT: Eagle.

        ///////////////////////////////////////////////////////////////////////

        private static readonly CharList EndOfLine =
            ChannelStream.CarriageReturnLineFeedCharList;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly string StdIn = "stdin";
        public static readonly string StdOut = "stdout";
        public static readonly string StdErr = "stderr";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private IChannelContext context; // where is the stream, et al?
        private IChannelContext savedContext; // saved ctx for begin/end.
        private Encoding encoding; // what is the input / output encoding?
        private StringBuilder virtualOutput; // are we capturing output?

        ///////////////////////////////////////////////////////////////////////

        private bool nullEncoding; // allow use of null encoding?
        private bool blockingMode; // are we synchronous?
        private bool appendMode; // are we always in append mode?
        private bool autoFlush; // always flush after a [puts]?
        private bool hitEndOfStream; // did we hit the end of the stream?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Channel()
        {
            this.kind = IdentifierKind.Channel;
            this.id = AttributeOps.GetObjectId(this);
            this.group = AttributeOps.GetObjectGroups(this);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        private Channel(
            TcpListener listener,     /* in */
            ChannelType channelType,  /* in */
            OptionDictionary options, /* in */
            StreamFlags flags,        /* in */
            IClientData clientData    /* in */
            )
            : this()
        {
            this.context = new ChannelContext(new ChannelStream(
                listener, channelType, options, flags));

            this.encoding = null;
            this.nullEncoding = false;
            this.appendMode = false;
            this.autoFlush = false;
            this.clientData = clientData;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private Channel(
            Stream stream,                    /* in */
            ChannelType channelType,          /* in */
            OptionDictionary options,         /* in */
            StreamFlags flags,                /* in */
            StreamTranslation inTranslation,  /* in */
            StreamTranslation outTranslation, /* in */
            Encoding encoding,                /* in */
            bool nullEncoding,                /* in */
            bool appendMode,                  /* in */
            bool autoFlush,                   /* in */
            IClientData clientData            /* in */
            )
            : this()
        {
            this.context = new ChannelContext(new ChannelStream(
                stream, channelType, options, flags, inTranslation,
                outTranslation));

            this.encoding = encoding;
            this.nullEncoding = nullEncoding;
            this.appendMode = appendMode;
            this.autoFlush = autoFlush;
            this.clientData = clientData;
        }

        ///////////////////////////////////////////////////////////////////////

        private Channel(
            ChannelStream stream,  /* in */
            Encoding encoding,     /* in */
            bool nullEncoding,     /* in */
            bool appendMode,       /* in */
            bool autoFlush,        /* in */
            IClientData clientData /* in */
            )
            : this()
        {
            this.context = new ChannelContext(stream);
            this.encoding = encoding;
            this.nullEncoding = nullEncoding;
            this.appendMode = appendMode;
            this.autoFlush = autoFlush;
            this.clientData = clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
#if NETWORK
        public static IChannel CreateListener(
            TcpListener listener,     /* in */
            ChannelType channelType,  /* in */
            OptionDictionary options, /* in */
            StreamFlags flags,        /* in */
            IClientData clientData    /* in */
            )
        {
            return new Channel(
                listener, channelType, options, flags, clientData);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static IChannel CreateInput(
            Stream stream,           /* in */
            ChannelType channelType, /* in */
            StreamFlags streamFlags, /* in */
            Encoding encoding        /* in */
            )
        {
            return new Channel(stream, ChannelType.Input |
                (channelType & ChannelType.FlagMask),
                null, streamFlags, StreamTranslation.auto,
                StreamTranslation.auto, encoding,
                DefaultInputNullEncoding, false, false,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IChannel CreateOutput(
            Stream stream,           /* in */
            ChannelType channelType, /* in */
            StreamFlags streamFlags, /* in */
            Encoding encoding,       /* in */
            bool autoFlush           /* in */
            )
        {
            return new Channel(stream, ChannelType.Output |
                (channelType & ChannelType.FlagMask),
                null, streamFlags, StreamTranslation.auto,
                StreamTranslation.auto, encoding,
                DefaultOutputNullEncoding, false, autoFlush,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IChannel CreateError(
            Stream stream,           /* in */
            ChannelType channelType, /* in */
            StreamFlags streamFlags, /* in */
            Encoding encoding,       /* in */
            bool autoFlush           /* in */
            )
        {
            return new Channel(stream, ChannelType.Error |
                (channelType & ChannelType.FlagMask),
                null, streamFlags, StreamTranslation.auto,
                StreamTranslation.auto, encoding,
                DefaultErrorNullEncoding, false, autoFlush,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IChannel Create(
            ChannelStream stream,  /* in */
            Encoding encoding,     /* in */
            bool nullEncoding,     /* in */
            bool appendMode,       /* in */
            bool autoFlush,        /* in */
            IClientData clientData /* in */
            )
        {
            return new Channel(
                stream, encoding, nullEncoding, appendMode, autoFlush,
                clientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private ChannelStream GetStreamFromContext()
        {
            return (context != null) ? context.ChannelStream : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private ChannelStream PartialCloneStreamFromContext(
            Stream stream
            )
        {
            return (context != null) ?
                context.PartialCloneChannelStream(stream) : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private ByteList TakeBufferFromContext()
        {
            return (context != null) ?
                context.TakeBuffer() : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool GiveBufferToContext(
            ref ByteList buffer /* in, out */
            )
        {
            return (context != null) ?
                context.GiveBuffer(ref buffer) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        private void NewBufferForContext()
        {
            if (context != null) context.NewBuffer();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IChannel Members
        public IChannelContext Context
        {
            get { CheckDisposed(); return context; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HaveSavedContext()
        {
            CheckDisposed();

            return (savedContext != null);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool BeginContext(
            Stream stream,   /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            return PrivateBeginContext(stream, ref savedContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool EndContext(
            bool close,      /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            return PrivateEndContext(close, ref savedContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool BeginContext(
            Stream stream,                    /* in */
            ref IChannelContext savedContext, /* in, out */
            ref Result error                  /* out */
            )
        {
            CheckDisposed();

            return PrivateBeginContext(stream, ref savedContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool EndContext(
            bool close,                       /* in */
            ref IChannelContext savedContext, /* in, out */
            ref Result error                  /* out */
            )
        {
            CheckDisposed();

            return PrivateEndContext(close, ref savedContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CanRead
        {
            get
            {
                CheckDisposed();

                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ? stream.CanRead : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CanSeek
        {
            get
            {
                CheckDisposed();

                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ? stream.CanSeek : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CanWrite
        {
            get
            {
                CheckDisposed();

                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ? stream.CanWrite : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HitEndOfStream
        {
            get { CheckDisposed(); return hitEndOfStream; }
            set { CheckDisposed(); hitEndOfStream = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool EndOfStream
        {
            get
            {
                CheckDisposed();

                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ?
                    (stream.Position >= stream.Length) : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public long Length
        {
            get
            {
                CheckDisposed();

                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ?
                    stream.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetLength(
            long value /* in */
            )
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            if (stream != null)
                stream.SetLength(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public long Position
        {
            get
            {
                CheckDisposed();

                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ? stream.Position : Index.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void GetEndOfLineParameters(
            out CharList endOfLine,       /* out */
            out bool useAnyEndOfLineChar, /* out */
            out bool keepEndOfLineChars   /* out */
            )
        {
            CheckDisposed();

            GetEndOfLineParameters(
                GetStreamFromContext(), out endOfLine,
                out useAnyEndOfLineChar, out keepEndOfLineChars);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Read(
            ref ByteList list, /* in, out */
            ref Result error   /* out */
            )
        {
            CheckDisposed();

            return Read(Count.Invalid, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Read(
            CharList endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            bool keepEndOfLineChars,  /* in */
            ref ByteList list,        /* in, out */
            ref Result error          /* out */
            )
        {
            CheckDisposed();

            return Read(
                Count.Invalid, endOfLine, useAnyEndOfLineChar,
                keepEndOfLineChars, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Read(
            int count,                /* in */
            CharList endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            bool keepEndOfLineChars,  /* in */
            ref ByteList list,        /* in, out */
            ref Result error          /* out */
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Error;
            ChannelStream stream = GetStreamFromContext();

            if (stream != null)
            {
                //
                // NOTE: Allocate enough for the whole file?
                //
                if (list == null)
                {
                    int capacity = stream.ReadCount;

                    if (capacity > 0)
                    {
                        list = new ByteList((int)Math.Min(
                            capacity, ChannelOps.MaximumBufferSize));
                    }
                    else
                    {
                        list = new ByteList();
                    }
                }

                //
                // NOTE: Read from the stream in a loop until we hit a
                //       terminator (typically "end-of-line" or "end-of-
                //       file").
                //
                int readCount = 0;
                bool eolFound = false;
                int eolLength = (endOfLine != null) ? endOfLine.Count : 0;
                int eolIndex = 0;

                do
                {
                    int value = ChannelOps.ReadByte(stream);

                    //
                    // NOTE: Did we hit the end of the stream?
                    //
                    if (value != ChannelStream.EndOfFile)
                    {
                        byte byteValue = ConversionOps.ToByte(value);

                        //
                        // NOTE: Did they supply a valid end-of-line
                        //       sequence to check against?
                        //
                        if ((endOfLine != null) && (eolLength > 0))
                        {
                            //
                            // NOTE: Does the caller want to stop reading
                            //       as soon as any of the supplied end-
                            //       of-line characters are detected?
                            //
                            if (useAnyEndOfLineChar)
                            {
                                //
                                // NOTE: Does the byte match any of the
                                //       supplied end-of-line characters?
                                //
                                if (endOfLine.Contains(
                                        ConversionOps.ToChar(byteValue)))
                                {
                                    eolFound = true;
                                }
                            }
                            else
                            {
                                //
                                // NOTE: Does the byte we just read match
                                //       the next character in the end-of-
                                //       line sequence we were expecting
                                //       to see?
                                //
                                if (byteValue == endOfLine[eolIndex])
                                {
                                    //
                                    // NOTE: Have we just match the last
                                    //       character of the end-of-line
                                    //       sequence?  If so, we have
                                    //       found the end-of-line and we
                                    //       are done.
                                    //
                                    if (++eolIndex == eolLength)
                                    {
                                        //
                                        // NOTE: Hit end-of-line sequence.
                                        //
                                        eolFound = true;
                                    }
                                }
                                else if (eolIndex > 0)
                                {
                                    //
                                    // NOTE: Any bytes previously matched
                                    //       against end-of-line sequence
                                    //       characters no longer count
                                    //       because the end-of-line
                                    //       sequence characters must
                                    //       appear consecutively.
                                    //
                                    eolIndex = 0;
                                }
                            }
                        }

                        //
                        // NOTE: Add the byte (which could potentially be
                        //       part of an end-of-line sequence) to the
                        //       buffer.
                        //
                        list.Add(byteValue);

                        //
                        // NOTE: We just read another byte, keep track.
                        //
                        readCount++;

                        //
                        // NOTE: Now that we have added the byte to the
                        //       buffer, check to see if we hit the end-
                        //       of-line (above).  If so, remove the end-
                        //       of-line seuqnece from the end of the
                        //       buffer and bail out.
                        //
                        if (eolFound)
                        {
                            if (!keepEndOfLineChars)
                            {
                                int bufferLength = list.Count;

                                ChannelOps.RemoveEndOfLine<byte>(
                                    ArrayOps.GetArray<byte>(list, true),
                                    new ByteList(endOfLine),
                                    useAnyEndOfLineChar, ref bufferLength);

                                while (list.Count > bufferLength)
                                    list.RemoveAt(list.Count - 1);
                            }

                            break;
                        }
                    }
                    else
                    {
                        hitEndOfStream = true; /* NOTE: No more data. */
                        break;
                    }
                }
                while ((count == Count.Invalid) || (readCount < count));

                TranslateEndOfLine(
                    StreamDirection.Input, list, ref list); // TEST: This.

                code = ReturnCode.Ok;
            }
            else
            {
                error = "invalid stream";
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ReadBuffer(
            ref ByteList list, /* in, out */
            ref Result error   /* out */
            )
        {
            CheckDisposed();

            return ReadBuffer(Count.Invalid, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ReadBuffer(
            CharList endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            bool keepEndOfLineChars,  /* in */
            ref ByteList list,        /* in, out */
            ref Result error          /* out */
            )
        {
            CheckDisposed();

            return ReadBuffer(
                Count.Invalid, endOfLine, useAnyEndOfLineChar,
                keepEndOfLineChars, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ReadBuffer(
            int count,                /* in */
            CharList endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            bool keepEndOfLineChars,  /* in */
            ref ByteList list,        /* in, out */
            ref Result error          /* out */
            )
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            if (stream == null)
            {
                error = "invalid stream";
                return ReturnCode.Error;
            }

            ByteList buffer = TakeBufferFromContext();

            if (buffer == null)
            {
                error = "invalid buffer";
                return ReturnCode.Error;
            }

            bool populated = stream.PopulateBuffer(
                ref buffer); /* throw */

            if (buffer == null)
            {
                error = "stream buffer was invalidated";
                return ReturnCode.Error;
            }

            byte[] bytes = ArrayOps.GetArray<byte>(buffer, true);

            if (!populated && (bytes.Length == 0))
            {
                GiveBufferToContext(ref buffer);

#if NETWORK
                if (stream.AvailableTimeout == 0)
                    hitEndOfStream = true;
#endif

                error = "no bytes read and none available";
                return ReturnCode.Error;
            }

        retry:

            if (endOfLine != null)
            {
                ByteList newEndOfLine = null;

                TranslateEndOfLine(
                    StreamDirection.Input, new ByteList(endOfLine),
                    ref newEndOfLine);

                int lastIndex = ChannelOps.FindEndOfLine<byte>(bytes,
                    newEndOfLine, 0, count, useAnyEndOfLineChar);

                if (lastIndex == Index.Invalid)
                {
                    endOfLine = null;
                    goto retry;
                }

                if (list == null)
                    list = new ByteList();

                List<byte> localList = list;
                int lastLength = lastIndex;

                if (keepEndOfLineChars)
                    lastLength += newEndOfLine.Count;

                ArrayOps.AppendArray<byte>(
                    bytes, 0, lastLength, ref localList);

                lastIndex += newEndOfLine.Count;

                if (!ArrayOps.SetArray<byte>(
                        buffer, ref bytes, lastIndex))
                {
                    buffer.Clear();
                }

                GiveBufferToContext(ref buffer);

                return ReturnCode.Ok;
            }
            else if (count != Count.Invalid)
            {
                int length = bytes.Length;

                if ((count < 0) || (count >= length))
                    count = length;

                if (list == null)
                    list = new ByteList();

                List<byte> localList = list;

                ArrayOps.AppendArray<byte>(
                    bytes, 0, count, ref localList);

                ArrayOps.SetArray<byte>(
                    buffer, ref bytes, count);

                GiveBufferToContext(ref buffer);

                return ReturnCode.Ok;
            }
            else
            {
                if (buffer.Count > 0)
                {
                    if (list != null)
                    {
                        list.AddRange(buffer);
                        buffer.Clear();
                    }
                    else
                    {
                        list = buffer;
                    }
                }

                buffer = null;

                NewBufferForContext();

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public long Seek(
            long offset,      /* in */
            SeekOrigin origin /* in */
            )
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ?
                stream.Seek(offset, origin) : Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public ChannelStream GetStream()
        {
            CheckDisposed();

            return GetStreamFromContext();
        }

        ///////////////////////////////////////////////////////////////////////

        public Stream GetInnerStream()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ? stream.GetStream() : null;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasReader
        {
            get
            {
                CheckDisposed();

                if (context != null)
                    return context.HasReader;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasWriter
        {
            get
            {
                CheckDisposed();

                if (context != null)
                    return context.HasWriter;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasBuffer
        {
            get
            {
                CheckDisposed();

                if (context != null)
                    return context.HasBuffer;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public BinaryReader GetBinaryReader()
        {
            CheckDisposed();

            if (context != null)
                return context.GetBinaryReader(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public BinaryWriter GetBinaryWriter()
        {
            CheckDisposed();

            if (context != null)
                return context.GetBinaryWriter(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public StreamReader GetStreamReader()
        {
            CheckDisposed();

            if (context != null)
                return context.GetStreamReader(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public StreamWriter GetStreamWriter()
        {
            CheckDisposed();

            if (context != null)
                return context.GetStreamWriter(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public ByteList TakeBuffer()
        {
            CheckDisposed();

            return TakeBufferFromContext();
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GiveBuffer(
            ref ByteList buffer
            )
        {
            CheckDisposed();

            return GiveBufferToContext(ref buffer);
        }

        ///////////////////////////////////////////////////////////////////////

        public void NewBuffer()
        {
            CheckDisposed();

            NewBufferForContext();
        }

        ///////////////////////////////////////////////////////////////////////

        public bool NullEncoding
        {
            get
            {
                CheckDisposed();

                return nullEncoding;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public Encoding GetEncoding()
        {
            CheckDisposed();

            return encoding;
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetEncoding(
            Encoding encoding /* in */
            )
        {
            CheckDisposed();

            if (context != null)
                context.CloseReadersAndWriters(true);

            this.encoding = encoding;
        }

        ///////////////////////////////////////////////////////////////////////

        public StreamTranslation GetInputTranslation()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ?
                stream.InputTranslation : StreamTranslation.auto;
        }

        ///////////////////////////////////////////////////////////////////////

        public StreamTranslation GetOutputTranslation()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ?
                stream.OutputTranslation : StreamTranslation.crlf;
        }

        ///////////////////////////////////////////////////////////////////////

        public StreamTranslationList GetTranslation()
        {
            CheckDisposed();

            StreamTranslationList translation = new StreamTranslationList();
            ChannelStream stream = GetStreamFromContext();

            if (stream != null)
            {
                if (stream.CanRead)
                    translation.Add(stream.InputTranslation);

                if (stream.CanWrite)
                    translation.Add(stream.OutputTranslation);
            }

            return translation;
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetTranslation(
            StreamTranslationList translation /* in */
            )
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            if ((stream != null) &&
                (translation != null) && (translation.Count > 0))
            {
                if (translation.Count >= 2)
                {
                    stream.InputTranslation = translation[0];
                    stream.OutputTranslation = translation[1];
                }
                else
                {
                    stream.InputTranslation = translation[0];
                    stream.OutputTranslation = translation[0];
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public CharList GetInputEndOfLine()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ? stream.InputEndOfLine : EndOfLine;
        }

        ///////////////////////////////////////////////////////////////////////

        public CharList GetOutputEndOfLine()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ? stream.OutputEndOfLine : EndOfLine;
        }

        ///////////////////////////////////////////////////////////////////////

        public void RemoveTrailingEndOfLine(
            ByteList buffer,   /* in */
            CharList endOfLine /* in: NOT USED */
            )
        {
            CheckDisposed();

            if ((buffer != null) && (buffer.Count > 0))
            {
                //
                // HACK: We only remove the trailing end-of-line character
                //       if it is a line-feed (i.e. Unix EOL, COMPAT: Tcl).
                //
                if (buffer[buffer.Count - 1] == Characters.LineFeed)
                    //
                    // NOTE: Remove the final character.
                    //
                    buffer.RemoveAt(buffer.Count - 1);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetBlockingMode()
        {
            CheckDisposed();

            return blockingMode;
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetBlockingMode(
            bool blockingMode /* in */
            )
        {
            CheckDisposed();

            this.blockingMode = blockingMode;
        }

        ///////////////////////////////////////////////////////////////////////

        public void CheckAppend()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            if ((stream != null) && stream.CanSeek && appendMode)
                stream.Seek(0, SeekOrigin.End);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CheckAutoFlush()
        {
            CheckDisposed();

            return autoFlush && Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Flush()
        {
            CheckDisposed();

            if (context != null)
                return context.Flush();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public void Close()
        {
            CheckDisposed();

            if (context != null)
            {
                context.Close();
                context = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsConsoleStream
        {
            get
            {
                CheckDisposed();

#if CONSOLE
                ChannelStream stream = GetStreamFromContext();

                if (stream != null)
                    return stream.IsConsole();
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public object Socket
        {
            get
            {
                CheckDisposed();

#if NETWORK
                ChannelStream stream = GetStreamFromContext();

                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream =
                            innerStream as NetworkStream;

                        if (networkStream != null)
                            return SocketOps.GetSocket(networkStream);
                    }
                }
#endif

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsNetworkStream
        {
            get
            {
                CheckDisposed();

#if NETWORK
                ChannelStream stream = GetStreamFromContext();

                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream =
                            innerStream as NetworkStream;

                        if (networkStream != null)
                            return true;
                    }
                }
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Connected
        {
            get
            {
                CheckDisposed();

#if NETWORK
                ChannelStream stream = GetStreamFromContext();

                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream =
                            innerStream as NetworkStream;

                        if (networkStream != null)
                        {
                            Socket socket = SocketOps.GetSocket(
                                networkStream);

                            if (socket != null)
                                return socket.Connected;
                        }

                    }
                }
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool DataAvailable
        {
            get
            {
                CheckDisposed();

#if NETWORK
                ChannelStream stream = GetStreamFromContext();

                if (stream != null)
                {
                    Stream innerStream = stream.GetStream();

                    if (innerStream != null)
                    {
                        NetworkStream networkStream =
                            innerStream as NetworkStream;

                        if (networkStream != null)
                            return networkStream.DataAvailable;
                    }
                }
#endif

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsVirtualOutput
        {
            get { CheckDisposed(); return (virtualOutput != null); }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by Interpreter class only.
        //
        public StringBuilder VirtualOutput
        {
            get { CheckDisposed(); return virtualOutput; }
            set { CheckDisposed(); virtualOutput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AppendVirtualOutput(
            char value /* in */
            )
        {
            CheckDisposed();

            if (virtualOutput != null)
            {
                virtualOutput.Append(value);
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AppendVirtualOutput(
            string value /* in */
            )
        {
            CheckDisposed();

            if (virtualOutput != null)
            {
                virtualOutput.Append(value);
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AppendVirtualOutput(
            byte[] value /* in */
            )
        {
            CheckDisposed();

            if (value == null)
                return false;

            int length = value.Length;

            if (length == 0)
                return false;

            if (virtualOutput != null)
            {
                for (int index = 0; index < length; index++)
                    virtualOutput.Append(value[index]);

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool PrivateBeginContext(
            Stream stream,                    /* in */
            ref IChannelContext savedContext, /* in, out */
            ref Result error                  /* out */
            )
        {
            if (savedContext != null) // already began?
            {
                error = "cannot begin context, have saved context";
                return false;
            }

            ChannelStream channelStream = PartialCloneStreamFromContext(
                stream);

            if (channelStream == null)
            {
                error = "cannot begin context, could not clone stream";
                return false;
            }

            savedContext = context;
            context = new ChannelContext(channelStream);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateEndContext(
            bool close,                       /* in */
            ref IChannelContext savedContext, /* in, out */
            ref Result error                  /* out */
            )
        {
            if (savedContext == null) // never began?
            {
                error = "cannot end context, no saved context";
                return false;
            }

            if (close)
            {
                IChannelContext oldContext = context;

                if ((oldContext != null) &&
                    !Object.ReferenceEquals(oldContext, savedContext))
                {
                    try
                    {
                        oldContext.Close();
                        oldContext = null;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return false;
                    }
                }
            }

            context = savedContext;
            savedContext = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private void GetEndOfLineParameters(
            ChannelStream stream,         /* in */
            out CharList endOfLine,       /* out */
            out bool useAnyEndOfLineChar, /* out */
            out bool keepEndOfLineChars   /* out */
            )
        {
            if ((stream != null) &&
                (stream.InputTranslation != StreamTranslation.binary))
            {
                endOfLine = stream.InputEndOfLine;
            }
            else
            {
                endOfLine = ChannelStream.LineFeedCharList;
            }

            if (stream != null)
            {
                useAnyEndOfLineChar = stream.UseAnyEndOfLineChar;
                keepEndOfLineChars = stream.KeepEndOfLineChars;
            }
            else
            {
                useAnyEndOfLineChar = false;
                keepEndOfLineChars = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode Read(
            int count,         /* in */
            ref ByteList list, /* in, out */
            ref Result error   /* out */
            )
        {
            CharList endOfLine;
            bool useAnyEndOfLineChar;
            bool keepEndOfLineChars;

            GetEndOfLineParameters(
                GetStreamFromContext(), out endOfLine,
                out useAnyEndOfLineChar, out keepEndOfLineChars);

            return Read(
                count, endOfLine, useAnyEndOfLineChar,
                keepEndOfLineChars, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode ReadBuffer(
            int count,         /* in */
            ref ByteList list, /* in, out */
            ref Result error   /* out */
            )
        {
            CharList endOfLine;
            bool useAnyEndOfLineChar;
            bool keepEndOfLineChars;

            GetEndOfLineParameters(
                GetStreamFromContext(), out endOfLine,
                out useAnyEndOfLineChar, out keepEndOfLineChars);

            return ReadBuffer(
                count, endOfLine, useAnyEndOfLineChar,
                keepEndOfLineChars, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private void TranslateEndOfLine(
            StreamDirection direction, /* in */
            byte[] inputBuffer,        /* in */
            ref byte[] outputBuffer    /* out */
            )
        {
            //
            // NOTE: We require the underlying stream to be
            //       valid because we use it to perform the
            //       configured end-of-line transformations.
            //
            ChannelStream stream = GetStreamFromContext();

            if (stream == null)
                return;

            //
            // NOTE: Is the input array valid?
            //
            if (inputBuffer == null)
            {
                //
                // NOTE: Garbage in, garbage out.
                //       Null list to null list.
                //
                outputBuffer = null;
                return;
            }

            //
            // NOTE: How many bytes are in the array?
            //
            int inputLength = inputBuffer.Length;

            if (inputLength == 0)
            {
                //
                // NOTE: Garbage in, garbage out.
                //       Empty list to empty list.
                //
                outputBuffer = new byte[0];
                return;
            }

            //
            // NOTE: Allocate an output buffer of equal
            //       length to the input buffer.
            //
            byte[] buffer = new byte[inputLength];

            //
            // NOTE: Use the underlying stream to perform
            //       the actual end-of-line transformations
            //       via the buffers we have prepared.  If
            //       the stream direction is neither Input
            //       only nor Output only, we do nothing.
            //
            int outputLength;

            if (direction == StreamDirection.Output)
            {
                outputLength = stream.TranslateOutputEndOfLine(
                    inputBuffer, buffer, 0, inputLength);
            }
            else if (direction == StreamDirection.Input)
            {
                outputLength = stream.TranslateInputEndOfLine(
                    inputBuffer, buffer, 0, inputLength);
            }
            else
            {
                outputLength = _Length.Invalid;
            }

            //
            // NOTE: Did we transform anything?
            //
            if (outputLength != Count.Invalid)
            {
                Array.Resize(ref buffer, outputLength);
                outputBuffer = buffer;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void TranslateEndOfLine(
            StreamDirection direction, /* in */
            ByteList inputList,        /* in */
            ref ByteList outputList    /* out */
            )
        {
            if (inputList == null)
            {
                outputList = null;
                return;
            }

            byte[] inputBuffer = ArrayOps.GetArray<byte>(
                inputList, true);

            byte[] outputBuffer = null;

            TranslateEndOfLine(
                direction, inputBuffer, ref outputBuffer);

            if (outputBuffer != null)
                outputList = new ByteList(outputBuffer);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            if (name != null)
                return name;

            if (context == null)
                return null;

            return context.ToString();
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
                throw new ObjectDisposedException(typeof(Channel).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    context = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Channel()
        {
            Dispose(false);
        }
        #endregion
    }
}
