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
    /// <summary>
    /// This class represents a single Eagle I/O channel (e.g. a file, socket,
    /// console stream, or a captured "virtual" output buffer).  It wraps an
    /// underlying <see cref="ChannelStream" /> (held indirectly via an
    /// <see cref="IChannelContext" />) and layers Tcl-compatible behavior on
    /// top of it: configurable text encoding, end-of-line translation,
    /// blocking versus non-blocking mode, append mode, automatic flushing, and
    /// line-oriented buffered reading.  Instances are created through the
    /// static factory methods rather than directly.
    /// </summary>
    [ObjectId("a35ad515-f878-426c-8073-bfc5aee4658e")]
    internal sealed class Channel : IChannel, IDisposable
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default value indicating whether a null encoding is permitted
        /// for newly created input channels.
        /// </summary>
        private static bool DefaultInputNullEncoding = true; // COMPAT: Eagle.
        /// <summary>
        /// The default value indicating whether a null encoding is permitted
        /// for newly created output channels.
        /// </summary>
        private static bool DefaultOutputNullEncoding = true; // COMPAT: Eagle.
        /// <summary>
        /// The default value indicating whether a null encoding is permitted
        /// for newly created error channels.
        /// </summary>
        private static bool DefaultErrorNullEncoding = true; // COMPAT: Eagle.

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default end-of-line character sequence (carriage-return /
        /// line-feed) used when no underlying stream is available.
        /// </summary>
        private static readonly CharList EndOfLine =
            ChannelStream.CarriageReturnLineFeedCharList;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        /// <summary>
        /// The well-known name of the standard input channel.
        /// </summary>
        public static readonly string StdIn = "stdin";
        /// <summary>
        /// The well-known name of the standard output channel.
        /// </summary>
        public static readonly string StdOut = "stdout";
        /// <summary>
        /// The well-known name of the standard error channel.
        /// </summary>
        public static readonly string StdErr = "stderr";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The active channel context, which holds the underlying stream and
        /// its associated read/write buffers.
        /// </summary>
        private IChannelContext context; // where is the stream, et al?
        /// <summary>
        /// The channel context saved by a begin-context operation, to be
        /// restored by the matching end-context operation.
        /// </summary>
        private IChannelContext savedContext; // saved ctx for begin/end.
        /// <summary>
        /// The text encoding used when reading from or writing to this
        /// channel.
        /// </summary>
        private Encoding encoding; // what is the input / output encoding?
        /// <summary>
        /// The buffer used to capture "virtual" output for this channel, or
        /// null if output is not being captured.
        /// </summary>
        private StringBuilder virtualOutput; // are we capturing output?

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if a null encoding is permitted for this channel.
        /// </summary>
        private bool nullEncoding; // allow use of null encoding?
        /// <summary>
        /// Non-zero if this channel operates in blocking (synchronous) mode.
        /// </summary>
        private bool blockingMode; // are we synchronous?
        /// <summary>
        /// Non-zero if this channel is always in append mode (i.e. writes seek
        /// to the end of the stream first).
        /// </summary>
        private bool appendMode; // are we always in append mode?
        /// <summary>
        /// Non-zero if this channel is automatically flushed after each write
        /// operation.
        /// </summary>
        private bool autoFlush; // always flush after a [puts]?
        /// <summary>
        /// Non-zero if buffered data should be ignored when determining whether
        /// the end-of-stream has been reached.
        /// </summary>
        private bool rawEndOfStream; // ignore buffer data for end-of-stream?
        /// <summary>
        /// Non-zero if the end-of-stream has been hit on this channel.
        /// </summary>
        private bool hitEndOfStream; // did we hit the end-of-stream?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty channel, initializing its identity (kind,
        /// identifier, and groups).  The other constructor overloads delegate
        /// to it.
        /// </summary>
        private Channel()
        {
            this.kind = IdentifierKind.Channel;
            this.id = AttributeOps.GetObjectId(this);
            this.group = AttributeOps.GetObjectGroups(this);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// Constructs a channel that wraps a listening network socket.
        /// </summary>
        /// <param name="listener">
        /// The TCP listener that this channel will wrap.
        /// </param>
        /// <param name="channelType">
        /// The flags describing the type of this channel.
        /// </param>
        /// <param name="options">
        /// The options associated with the underlying channel stream, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the underlying channel stream.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this channel, if any.  This
        /// parameter may be null.
        /// </param>
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
            this.rawEndOfStream = false;
            this.clientData = clientData;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a channel that wraps the specified stream, configuring
        /// its translation, encoding, and mode settings.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream that this channel will wrap.
        /// </param>
        /// <param name="channelType">
        /// The flags describing the type of this channel.
        /// </param>
        /// <param name="options">
        /// The options associated with the underlying channel stream, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the underlying channel stream.
        /// </param>
        /// <param name="inTranslation">
        /// The end-of-line translation to use when reading from the stream.
        /// </param>
        /// <param name="outTranslation">
        /// The end-of-line translation to use when writing to the stream.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for this channel, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="nullEncoding">
        /// Non-zero if a null encoding is permitted for this channel.
        /// </param>
        /// <param name="appendMode">
        /// Non-zero if this channel is always in append mode.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero if this channel is automatically flushed after each write
        /// operation.
        /// </param>
        /// <param name="rawEndOfStream">
        /// Non-zero if buffered data should be ignored when determining whether
        /// the end-of-stream has been reached.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this channel, if any.  This
        /// parameter may be null.
        /// </param>
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
            bool rawEndOfStream,              /* in */
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
            this.rawEndOfStream = rawEndOfStream;
            this.clientData = clientData;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a channel that wraps an existing channel stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying channel stream that this channel will wrap.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for this channel, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="nullEncoding">
        /// Non-zero if a null encoding is permitted for this channel.
        /// </param>
        /// <param name="appendMode">
        /// Non-zero if this channel is always in append mode.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero if this channel is automatically flushed after each write
        /// operation.
        /// </param>
        /// <param name="rawEndOfStream">
        /// Non-zero if buffered data should be ignored when determining whether
        /// the end-of-stream has been reached.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this channel, if any.  This
        /// parameter may be null.
        /// </param>
        private Channel(
            ChannelStream stream,  /* in */
            Encoding encoding,     /* in */
            bool nullEncoding,     /* in */
            bool appendMode,       /* in */
            bool autoFlush,        /* in */
            bool rawEndOfStream,   /* in */
            IClientData clientData /* in */
            )
            : this()
        {
            this.context = new ChannelContext(stream);
            this.encoding = encoding;
            this.nullEncoding = nullEncoding;
            this.appendMode = appendMode;
            this.autoFlush = autoFlush;
            this.rawEndOfStream = rawEndOfStream;
            this.clientData = clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
#if NETWORK
        /// <summary>
        /// This method creates a new channel that wraps a listening network
        /// socket.
        /// </summary>
        /// <param name="listener">
        /// The TCP listener that the new channel will wrap.
        /// </param>
        /// <param name="channelType">
        /// The flags describing the type of the new channel.
        /// </param>
        /// <param name="options">
        /// The options associated with the underlying channel stream, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the underlying channel stream.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new channel, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created channel.
        /// </returns>
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

        /// <summary>
        /// This method creates a new input channel that wraps the specified
        /// stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream that the new channel will wrap.
        /// </param>
        /// <param name="channelType">
        /// The flags describing the type of the new channel; only the flag bits
        /// are honored, as the input designation is added automatically.
        /// </param>
        /// <param name="streamFlags">
        /// The flags controlling the behavior of the underlying channel stream.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the new channel, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created input channel.
        /// </returns>
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
                false, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new output channel that wraps the specified
        /// stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream that the new channel will wrap.
        /// </param>
        /// <param name="channelType">
        /// The flags describing the type of the new channel; only the flag bits
        /// are honored, as the output designation is added automatically.
        /// </param>
        /// <param name="streamFlags">
        /// The flags controlling the behavior of the underlying channel stream.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the new channel, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero if the new channel is automatically flushed after each
        /// write operation.
        /// </param>
        /// <returns>
        /// The newly created output channel.
        /// </returns>
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
                false, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new error channel that wraps the specified
        /// stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream that the new channel will wrap.
        /// </param>
        /// <param name="channelType">
        /// The flags describing the type of the new channel; only the flag bits
        /// are honored, as the error designation is added automatically.
        /// </param>
        /// <param name="streamFlags">
        /// The flags controlling the behavior of the underlying channel stream.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the new channel, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero if the new channel is automatically flushed after each
        /// write operation.
        /// </param>
        /// <returns>
        /// The newly created error channel.
        /// </returns>
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
                false, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new channel that wraps an existing channel
        /// stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying channel stream that the new channel will wrap.
        /// </param>
        /// <param name="encoding">
        /// The text encoding to use for the new channel, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="nullEncoding">
        /// Non-zero if a null encoding is permitted for the new channel.
        /// </param>
        /// <param name="appendMode">
        /// Non-zero if the new channel is always in append mode.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero if the new channel is automatically flushed after each
        /// write operation.
        /// </param>
        /// <param name="rawEndOfStream">
        /// Non-zero if buffered data should be ignored when determining whether
        /// the end-of-stream has been reached.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the new channel, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created channel.
        /// </returns>
        public static IChannel Create(
            ChannelStream stream,  /* in */
            Encoding encoding,     /* in */
            bool nullEncoding,     /* in */
            bool appendMode,       /* in */
            bool autoFlush,        /* in */
            bool rawEndOfStream,   /* in */
            IClientData clientData /* in */
            )
        {
            return new Channel(
                stream, encoding, nullEncoding, appendMode,
                autoFlush, rawEndOfStream, clientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// seeking.
        /// </summary>
        private bool PrivateCanSeek
        {
            get
            {
                ChannelStream stream = GetStreamFromContext();

                return (stream != null) ? stream.CanSeek : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the active channel context has a
        /// non-empty buffer and is not configured to ignore buffered data for
        /// end-of-stream purposes.
        /// </summary>
        private bool HasNoneEmptyBufferForContext
        {
            get
            {
                if (!rawEndOfStream &&
                    HasContext && !HasEmptyBufferForContext)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the end-of-stream has been
        /// hit on this channel.
        /// </summary>
        private bool PrivateHitEndOfStream
        {
            get { return hitEndOfStream; }
            set { hitEndOfStream = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream's current
        /// position is at or beyond its length (i.e. the end-of-stream).
        /// </summary>
        private bool PrivateEndOfStream
        {
            get
            {
                ChannelStream stream = GetStreamFromContext();

                if (stream == null)
                    return false;

                return (stream.Position >= stream.Length);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the end-of-stream has been reached,
        /// considering both the underlying stream's position (when seekable)
        /// and whether the end-of-stream flag has already been set.
        /// </summary>
        private bool PrivateAnyEndOfStream
        {
            get
            {
                ChannelStream stream = GetStreamFromContext();

                if (stream == null)
                    return false;

                if (stream.CanSeek &&
                    (stream.Position >= stream.Length))
                {
                    return true;
                }

                if (hitEndOfStream)
                    return true;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the end-of-stream has been reached,
        /// using the underlying stream's position when it is seekable and
        /// otherwise the end-of-stream flag.
        /// </summary>
        private bool PrivateOneEndOfStream
        {
            get
            {
                ChannelStream stream = GetStreamFromContext();

                if (stream == null)
                    return false;

                if (stream.CanSeek)
                    return (stream.Position >= stream.Length);
                else
                    return hitEndOfStream;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the active channel context has a
        /// buffer.
        /// </summary>
        private bool PrivateHasBuffer
        {
            get
            {
                if (context == null)
                    return false;

                return context.HasBuffer;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this channel has an active channel
        /// context.
        /// </summary>
        private bool HasContext
        {
            get { return (context != null); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the active channel context has an
        /// empty buffer.
        /// </summary>
        private bool HasEmptyBufferForContext
        {
            get
            {
                return (context != null) ?
                    context.HasEmptyBuffer : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the channel stream associated with the active
        /// channel context.
        /// </summary>
        /// <returns>
        /// The channel stream from the active context, or null if there is no
        /// active context.
        /// </returns>
        private ChannelStream GetStreamFromContext()
        {
            return (context != null) ? context.ChannelStream : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method partially clones the channel stream associated with the
        /// active channel context, substituting the specified inner stream.
        /// </summary>
        /// <param name="stream">
        /// The inner stream to use for the cloned channel stream.
        /// </param>
        /// <returns>
        /// The partially cloned channel stream, or null if there is no active
        /// context.
        /// </returns>
        private ChannelStream PartialCloneStreamFromContext(
            Stream stream /* in */
            )
        {
            return (context != null) ?
                context.PartialCloneChannelStream(stream) : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards the buffered data and line endings held by the
        /// active channel context.
        /// </summary>
        /// <returns>
        /// The total number of bytes discarded, or <see cref="Count.Invalid" />
        /// if there is no active context.
        /// </returns>
        private int DiscardForContext()
        {
            int result;

            if (context != null)
            {
                result = 0;
                result += context.DiscardBuffer();
                result += context.DiscardLineEndings();
            }
            else
            {
                result = Count.Invalid;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the active channel context by taking (and
        /// discarding) its buffer and line endings.
        /// </summary>
        private void ResetForContext()
        {
            if (context != null)
            {
                /* IGNORED */
                context.TakeBuffer();

                /* IGNORED */
                context.TakeLineEndings();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method takes ownership of the buffer and line endings held by
        /// the active channel context.
        /// </summary>
        /// <param name="buffer">
        /// Upon return, receives the buffer taken from the active context, or
        /// null if there is no active context.
        /// </param>
        /// <param name="lineEndings">
        /// Upon return, receives the line endings taken from the active
        /// context, or null if there is no active context.
        /// </param>
        private void TakeFromContext(
            out ByteList buffer,
            out IntList lineEndings
            )
        {
            if (context != null)
            {
                buffer = context.TakeBuffer();
                lineEndings = context.TakeLineEndings();
            }
            else
            {
                buffer = null;
                lineEndings = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gives ownership of the specified buffer and line endings
        /// back to the active channel context.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to give to the active context.
        /// </param>
        /// <param name="lineEndings">
        /// The line endings to give to the active context.
        /// </param>
        /// <returns>
        /// True if the buffer was accepted by the active context; otherwise,
        /// false.
        /// </returns>
        private bool GiveToContext(
            ref ByteList buffer,    /* in, out */
            ref IntList lineEndings /* in, out */
            )
        {
            if (context != null)
            {
                if (context.GiveBuffer(ref buffer))
                {
                    /* IGNORED */
                    context.GiveLineEndings(
                        ref lineEndings);

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method allocates fresh buffer and line-ending storage for the
        /// active channel context.
        /// </summary>
        private void NewForContext()
        {
            if (context != null)
            {
                context.NewBuffer();
                context.NewLineEndings();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this channel.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this channel.
        /// </summary>
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this channel.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this channel.
        /// </summary>
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this channel.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this channel.
        /// </summary>
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this channel.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this channel.
        /// </summary>
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this channel.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this channel.
        /// </summary>
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this channel.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this channel.
        /// </summary>
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IChannel Members
        /// <summary>
        /// Gets the active channel context for this channel.
        /// </summary>
        public IChannelContext Context
        {
            get { CheckDisposed(); return context; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this channel currently has a saved
        /// channel context (i.e. a begin-context operation is in effect).
        /// </summary>
        /// <returns>
        /// True if this channel has a saved context; otherwise, false.
        /// </returns>
        public bool HaveSavedContext()
        {
            CheckDisposed();

            return (savedContext != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a new channel context that wraps the specified
        /// stream, saving the current context internally so it can later be
        /// restored.
        /// </summary>
        /// <param name="stream">
        /// The inner stream to use for the new channel context.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the context was begun successfully; otherwise, false.
        /// </returns>
        public bool BeginContext(
            Stream stream,   /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            return PrivateBeginContext(stream, ref savedContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends the current channel context, restoring the context
        /// that was saved internally by the matching begin-context operation.
        /// </summary>
        /// <param name="close">
        /// Non-zero to close the current (ending) context before restoring the
        /// saved context.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the context was ended successfully; otherwise, false.
        /// </returns>
        public bool EndContext(
            bool close,      /* in */
            ref Result error /* out */
            )
        {
            CheckDisposed();

            return PrivateEndContext(close, ref savedContext, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a new channel context that wraps the specified
        /// stream, saving the current context into the supplied variable so it
        /// can later be restored.
        /// </summary>
        /// <param name="stream">
        /// The inner stream to use for the new channel context.
        /// </param>
        /// <param name="savedContext">
        /// Upon success, receives the context that was saved (i.e. the prior
        /// active context).  This parameter must be null on entry.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the context was begun successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method ends the current channel context, restoring the context
        /// from the supplied saved-context variable.
        /// </summary>
        /// <param name="close">
        /// Non-zero to close the current (ending) context before restoring the
        /// saved context.
        /// </param>
        /// <param name="savedContext">
        /// The previously saved context to restore.  Upon success, this is
        /// cleared to null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the context was ended successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// reading.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// seeking.
        /// </summary>
        public bool CanSeek
        {
            get
            {
                CheckDisposed();

                return PrivateCanSeek;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// writing.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether the end-of-stream has been
        /// hit on this channel.  When buffered data remains, the get accessor
        /// returns false.
        /// </summary>
        public bool HitEndOfStream
        {
            get
            {
                CheckDisposed();

                if (HasNoneEmptyBufferForContext)
                    return false;

                return PrivateHitEndOfStream;
            }
            set
            {
                CheckDisposed();

                PrivateHitEndOfStream = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream's current
        /// position is at or beyond its length.  When buffered data remains,
        /// this returns false.
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                CheckDisposed();

                if (HasNoneEmptyBufferForContext)
                    return false;

                return PrivateEndOfStream;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the end-of-stream has been reached,
        /// considering both the underlying stream's position and the
        /// end-of-stream flag.  When buffered data remains, this returns false.
        /// </summary>
        public bool AnyEndOfStream
        {
            get
            {
                CheckDisposed();

                if (HasNoneEmptyBufferForContext)
                    return false;

                return PrivateAnyEndOfStream;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the end-of-stream has been reached,
        /// using the underlying stream's position when seekable and otherwise
        /// the end-of-stream flag.  When buffered data remains, this returns
        /// false.
        /// </summary>
        public bool OneEndOfStream
        {
            get
            {
                CheckDisposed();

                if (HasNoneEmptyBufferForContext)
                    return false;

                return PrivateOneEndOfStream;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in bytes, of the underlying stream, or
        /// <see cref="_Constants.Length.Invalid" /> if there is no underlying
        /// stream.
        /// </summary>
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

        /// <summary>
        /// This method sets the length, in bytes, of the underlying stream.
        /// If there is no underlying stream, this method does nothing.
        /// </summary>
        /// <param name="value">
        /// The desired length, in bytes, of the underlying stream.
        /// </param>
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

        /// <summary>
        /// Gets the current position, in bytes, within the underlying stream,
        /// or <see cref="Index.Invalid" /> if there is no underlying stream.
        /// </summary>
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

        /// <summary>
        /// This method gets the end-of-line parameters currently in effect for
        /// this channel, based on the underlying stream's configuration.
        /// </summary>
        /// <param name="endOfLine">
        /// Upon return, receives the end-of-line character sequence to use.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Upon return, receives a value indicating whether any of the
        /// end-of-line characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Upon return, receives a value indicating whether the end-of-line
        /// characters are retained in the returned data.
        /// </param>
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

        /// <summary>
        /// This method reads a single line of bytes from the underlying stream
        /// using the end-of-line parameters currently in effect for this
        /// channel.
        /// </summary>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Read(
            ref ByteList list, /* in, out */
            ref Result error   /* out */
            )
        {
            CheckDisposed();

            return Read(Count.Invalid, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of bytes from the underlying stream
        /// using the specified end-of-line parameters.
        /// </summary>
        /// <param name="endOfLine">
        /// The end-of-line character sequence to detect.  This parameter may be
        /// null.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any of the end-of-line characters terminates a line;
        /// otherwise, the full sequence must be matched consecutively.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the returned data.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method reads bytes directly from the underlying stream, one
        /// byte at a time, until a count limit or end-of-line sequence is
        /// reached or the end-of-stream is hit, then applies the configured
        /// input end-of-line translation.
        /// </summary>
        /// <param name="count">
        /// The maximum number of bytes to read, or <see cref="Count.Invalid" />
        /// to read until an end-of-line or end-of-stream is encountered.
        /// </param>
        /// <param name="endOfLine">
        /// The end-of-line character sequence to detect.  This parameter may be
        /// null.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any of the end-of-line characters terminates a line;
        /// otherwise, the full sequence must be matched consecutively.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the returned data.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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
                bool eosFound = false;
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
                        //
                        // NOTE: The End-Of-Stream has been hit.  Set both
                        //       instance state flag and the local variable.
                        //
                        PrivateHitEndOfStream = true;
                        eosFound = true;

                        break;
                    }
                }
                while ((count == Count.Invalid) || (readCount < count));

                //
                // BUGFIX: When the stream has been fully consumed, signal end-of-
                //         stream to the translation so that a lone carriage-return
                //         as the FINAL byte is emitted rather than deferred (via
                //         StreamFlags.NeedLineFeed) for a follow-up read that will
                //         never come -- which silently dropped that trailing
                //         carriage-return under "crlf" / "platform" translation.
                //         This covers BOTH hitting end-of-stream while reading AND
                //         a counted read whose last byte happens to be the final
                //         byte of a seekable stream (where [eof] then becomes true
                //         and no follow-up read occurs).  While more input may yet
                //         arrive (a counted read that is not at end-of-stream), the
                //         carriage-return is correctly left deferred for the next
                //         read.  (COMPAT: Tcl; F47)
                //
                ByteList newList = null;

                if (!eosFound && PrivateOneEndOfStream)
                    eosFound = true;

                StreamDirection direction = StreamDirection.Input;

                if (eosFound)
                    direction |= StreamDirection.EndOfStream;

                TranslateEndOfLine(direction, list, ref newList);

                list = newList;
                code = ReturnCode.Ok;
            }
            else
            {
                error = "invalid stream";
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of bytes via the channel's buffered
        /// reading mechanism, using the end-of-line parameters currently in
        /// effect for this channel.
        /// </summary>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode ReadBuffer(
            ref ByteList list, /* in, out */
            ref Result error   /* out */
            )
        {
            CheckDisposed();

            return ReadBuffer(Count.Invalid, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single line of bytes via the channel's buffered
        /// reading mechanism, using the specified end-of-line parameters.
        /// </summary>
        /// <param name="endOfLine">
        /// The end-of-line character sequence to detect.  This parameter may be
        /// null.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any of the end-of-line characters terminates a line;
        /// otherwise, the full sequence must be matched consecutively.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the returned data.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method reads bytes via the channel's buffered reading
        /// mechanism, populating the context buffer from the underlying stream
        /// as needed and extracting either a complete line, a fixed count of
        /// bytes, or all currently buffered bytes.
        /// </summary>
        /// <param name="count">
        /// The maximum number of bytes to read, or <see cref="Count.Invalid" />
        /// to read a single line (when an end-of-line is supplied) or all
        /// buffered bytes.
        /// </param>
        /// <param name="endOfLine">
        /// The end-of-line character sequence to detect.  This parameter may be
        /// null.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero if any of the end-of-line characters terminates a line;
        /// otherwise, the full sequence must be matched consecutively.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Non-zero to retain the end-of-line characters in the returned data.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

            ByteList buffer;
            IntList lineEndings;

            TakeFromContext(out buffer, out lineEndings);

            if (buffer == null)
            {
                error = "invalid buffer";
                return ReturnCode.Error;
            }

            bool populated;
            bool ignoreLineEnding = false;

        repopulate:

            populated = stream.PopulateBuffer(
                ignoreLineEnding, useAnyEndOfLineChar, ref buffer,
                ref lineEndings); /* throw */

            if (buffer == null)
            {
                error = "stream buffer was invalidated";
                return ReturnCode.Error;
            }

            byte[] bytes = ArrayOps.GetArray<byte>(buffer, true);

            if (!populated &&
                ((bytes == null) || (bytes.Length == 0)))
            {
                if (!ignoreLineEnding && PrivateAnyEndOfStream)
                {
                    ignoreLineEnding = true;
                    lineEndings = null;

                    goto repopulate;
                }

                GiveToContext(ref buffer, ref lineEndings);

#if NETWORK
                if (stream.AvailableTimeout == 0)
                    PrivateHitEndOfStream = true;
#endif

                error = "no bytes read and none available";
                return ReturnCode.Error;
            }

        retry:

            List<byte> localList; /* REUSED */

            if (endOfLine != null)
            {
                ByteList newEndOfLine = null;

                TranslateEndOfLine(StreamDirection.InputEolOnly,
                    new ByteList(endOfLine), ref newEndOfLine);

                int lastIndex;

                if (lineEndings != null)
                {
                    lastIndex = ChannelOps.FindEndOfLine(lineEndings);
                }
                else
                {
                    lastIndex = ChannelOps.FindEndOfLine<byte>(bytes,
                        newEndOfLine, 0, count, useAnyEndOfLineChar);
                }

                if (lastIndex == Index.Invalid)
                {
                    endOfLine = null;
                    goto retry;
                }

                if (list == null)
                    list = new ByteList();

                localList = list;

                int lastLength = lastIndex;

                if (keepEndOfLineChars)
                    lastLength += newEndOfLine.Count;

                /* IGNORED */
                ArrayOps.AppendArray<byte>(
                    bytes, 0, lastLength, ref localList);

                lastIndex += newEndOfLine.Count;

                if (ArrayOps.SetArray<byte>(
                        buffer, ref bytes, lastIndex))
                {
                    /* NO RESULT */
                    ListOps.Adjust(
                        lineEndings, -lastIndex,
                        Index.Invalid, null);
                }
                else
                {
                    buffer.Clear();

                    if (lineEndings != null)
                        lineEndings.Clear();
                }

                GiveToContext(ref buffer, ref lineEndings);
            }
            else if (count != Count.Invalid)
            {
                int length = bytes.Length;

                if ((count < 0) || (count >= length))
                    count = length;

                if (list == null)
                    list = new ByteList();

                localList = list;

                /* IGNORED */
                ArrayOps.AppendArray<byte>(
                    bytes, 0, count, ref localList);

                if (ArrayOps.SetArray<byte>(
                        buffer, ref bytes, count))
                {
                    /* NO RESULT */
                    ListOps.Adjust(
                        lineEndings, -count,
                        Index.Invalid, null);
                }
                else
                {
                    buffer.Clear();

                    if (lineEndings != null)
                        lineEndings.Clear();
                }

                GiveToContext(ref buffer, ref lineEndings);
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
                lineEndings = null;

                NewForContext();
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the current position within the underlying
        /// stream.
        /// </summary>
        /// <param name="offset">
        /// The byte offset relative to <paramref name="origin" />.
        /// </param>
        /// <param name="origin">
        /// The reference point from which <paramref name="offset" /> is
        /// applied.
        /// </param>
        /// <returns>
        /// The new position within the stream, or <see cref="Index.Invalid" />
        /// if there is no underlying stream.
        /// </returns>
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

        /// <summary>
        /// This method gets the channel stream associated with this channel.
        /// </summary>
        /// <returns>
        /// The channel stream, or null if there is no active context.
        /// </returns>
        public ChannelStream GetStream()
        {
            CheckDisposed();

            return GetStreamFromContext();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the inner (wrapped) stream associated with this
        /// channel.
        /// </summary>
        /// <returns>
        /// The inner stream, or null if there is no underlying stream.
        /// </returns>
        public Stream GetInnerStream()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ? stream.GetStream() : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the active channel context has an
        /// associated reader.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the active channel context has an
        /// associated writer.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the active channel context has a
        /// buffer.
        /// </summary>
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

        /// <summary>
        /// This method gets a binary reader for this channel, using the
        /// channel's configured encoding.
        /// </summary>
        /// <returns>
        /// The binary reader, or null if there is no active context.
        /// </returns>
        public BinaryReader GetBinaryReader()
        {
            CheckDisposed();

            if (context != null)
                return context.GetBinaryReader(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a binary writer for this channel, using the
        /// channel's configured encoding.
        /// </summary>
        /// <returns>
        /// The binary writer, or null if there is no active context.
        /// </returns>
        public BinaryWriter GetBinaryWriter()
        {
            CheckDisposed();

            if (context != null)
                return context.GetBinaryWriter(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a stream reader for this channel, using the
        /// channel's configured encoding.
        /// </summary>
        /// <returns>
        /// The stream reader, or null if there is no active context.
        /// </returns>
        public StreamReader GetStreamReader()
        {
            CheckDisposed();

            if (context != null)
                return context.GetStreamReader(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a stream writer for this channel, using the
        /// channel's configured encoding.
        /// </summary>
        /// <returns>
        /// The stream writer, or null if there is no active context.
        /// </returns>
        public StreamWriter GetStreamWriter()
        {
            CheckDisposed();

            if (context != null)
                return context.GetStreamWriter(encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discards the buffered data and line endings held by this
        /// channel.
        /// </summary>
        /// <returns>
        /// The total number of bytes discarded, or <see cref="Count.Invalid" />
        /// if there is no active context.
        /// </returns>
        public int DiscardBuffered()
        {
            CheckDisposed();

            return DiscardForContext();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the buffered data and line endings held by this
        /// channel.
        /// </summary>
        public void ResetBuffered()
        {
            CheckDisposed();

            ResetForContext();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method takes ownership of the buffer and line endings held by
        /// this channel.
        /// </summary>
        /// <param name="buffer">
        /// Upon return, receives the buffer taken from this channel, or null if
        /// there is no active context.
        /// </param>
        /// <param name="lineEndings">
        /// Upon return, receives the line endings taken from this channel, or
        /// null if there is no active context.
        /// </param>
        public void TakeBuffered(
            out ByteList buffer,
            out IntList lineEndings
            )
        {
            CheckDisposed();

            TakeFromContext(out buffer, out lineEndings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gives ownership of the specified buffer and line endings
        /// back to this channel.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to give to this channel.
        /// </param>
        /// <param name="lineEndings">
        /// The line endings to give to this channel.
        /// </param>
        /// <returns>
        /// True if the buffer was accepted; otherwise, false.
        /// </returns>
        public bool GiveBuffered(
            ref ByteList buffer,
            ref IntList lineEndings
            )
        {
            CheckDisposed();

            return GiveToContext(ref buffer, ref lineEndings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method allocates fresh buffer and line-ending storage for this
        /// channel.
        /// </summary>
        public void NewBuffered()
        {
            CheckDisposed();

            NewForContext();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether a null encoding is permitted for
        /// this channel.
        /// </summary>
        public bool NullEncoding
        {
            get
            {
                CheckDisposed();

                return nullEncoding;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the text encoding used by this channel.
        /// </summary>
        /// <returns>
        /// The text encoding, or null if no encoding is configured.
        /// </returns>
        public Encoding GetEncoding()
        {
            CheckDisposed();

            return encoding;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the text encoding used by this channel, closing any
        /// existing readers and writers so the new encoding takes effect.
        /// </summary>
        /// <param name="encoding">
        /// The text encoding to use for this channel, if any.  This parameter
        /// may be null.
        /// </param>
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

        /// <summary>
        /// This method gets the end-of-line translation used when reading from
        /// this channel.
        /// </summary>
        /// <returns>
        /// The input translation, or <see cref="StreamTranslation.auto" /> if
        /// there is no underlying stream.
        /// </returns>
        public StreamTranslation GetInputTranslation()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ?
                stream.InputTranslation : StreamTranslation.auto;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the end-of-line translation used when writing to
        /// this channel.
        /// </summary>
        /// <returns>
        /// The output translation, or <see cref="StreamTranslation.crlf" /> if
        /// there is no underlying stream.
        /// </returns>
        public StreamTranslation GetOutputTranslation()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ?
                stream.OutputTranslation : StreamTranslation.crlf;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the end-of-line translations in effect for this
        /// channel, including the input translation (when readable) and the
        /// output translation (when writable).
        /// </summary>
        /// <returns>
        /// A list containing the applicable translations.
        /// </returns>
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

        /// <summary>
        /// This method sets the end-of-line translations for this channel.  A
        /// single supplied translation is applied to both input and output; two
        /// or more apply the first to input and the second to output.
        /// </summary>
        /// <param name="translation">
        /// The list of translations to apply.  This parameter may be null or
        /// empty, in which case no change is made.
        /// </param>
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

        /// <summary>
        /// This method gets the end-of-line character sequence used when
        /// reading from this channel.
        /// </summary>
        /// <returns>
        /// The input end-of-line sequence, or the default end-of-line sequence
        /// if there is no underlying stream.
        /// </returns>
        public CharList GetInputEndOfLine()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ? stream.InputEndOfLine : EndOfLine;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the end-of-line character sequence used when
        /// writing to this channel.
        /// </summary>
        /// <returns>
        /// The output end-of-line sequence, or the default end-of-line sequence
        /// if there is no underlying stream.
        /// </returns>
        public CharList GetOutputEndOfLine()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            return (stream != null) ? stream.OutputEndOfLine : EndOfLine;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a trailing end-of-line character from the
        /// specified buffer.  Only a trailing line-feed is removed (i.e. Unix
        /// end-of-line, COMPAT: Tcl).
        /// </summary>
        /// <param name="buffer">
        /// The buffer from which to remove the trailing end-of-line character.
        /// This parameter may be null.
        /// </param>
        /// <param name="endOfLine">
        /// The end-of-line character sequence.  This parameter is not used.
        /// </param>
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

        /// <summary>
        /// This method gets a value indicating whether this channel operates in
        /// blocking (synchronous) mode.
        /// </summary>
        /// <returns>
        /// True if this channel is in blocking mode; otherwise, false.
        /// </returns>
        public bool GetBlockingMode()
        {
            CheckDisposed();

            return blockingMode;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets whether this channel operates in blocking
        /// (synchronous) mode.
        /// </summary>
        /// <param name="blockingMode">
        /// Non-zero to place this channel in blocking mode.
        /// </param>
        public void SetBlockingMode(
            bool blockingMode /* in */
            )
        {
            CheckDisposed();

            this.blockingMode = blockingMode;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method seeks to the end of the underlying stream when this
        /// channel is in append mode and the stream supports seeking.
        /// </summary>
        public void CheckAppend()
        {
            CheckDisposed();

            ChannelStream stream = GetStreamFromContext();

            if ((stream != null) && stream.CanSeek && appendMode)
                stream.Seek(0, SeekOrigin.End);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes this channel when it is configured for automatic
        /// flushing.
        /// </summary>
        /// <returns>
        /// True if automatic flushing is enabled and the flush succeeded;
        /// otherwise, false.
        /// </returns>
        public bool CheckAutoFlush()
        {
            CheckDisposed();

            return autoFlush && Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered output of this channel to the
        /// underlying stream.
        /// </summary>
        /// <returns>
        /// True if the flush succeeded; otherwise, false.
        /// </returns>
        public bool Flush()
        {
            CheckDisposed();

            if (context != null)
                return context.Flush();

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes this channel, releasing its active context.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the underlying stream is a console
        /// stream.
        /// </summary>
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

        /// <summary>
        /// Gets the underlying network socket associated with this channel, or
        /// null if this channel does not wrap a network stream.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the underlying stream is a network
        /// stream.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the underlying network socket is
        /// connected.  Returns false when this channel does not wrap a network
        /// stream.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether data is available to be read from
        /// the underlying network stream.  Returns false when this channel does
        /// not wrap a network stream.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether this channel is capturing "virtual"
        /// output.
        /// </summary>
        public bool IsVirtualOutput
        {
            get { CheckDisposed(); return (virtualOutput != null); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the buffer used to capture "virtual" output for this
        /// channel, or null if output is not being captured.
        /// </summary>
        //
        // NOTE: For use by Interpreter class only.
        //
        public StringBuilder VirtualOutput
        {
            get { CheckDisposed(); return virtualOutput; }
            set { CheckDisposed(); virtualOutput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a single character to this channel's captured
        /// "virtual" output.
        /// </summary>
        /// <param name="value">
        /// The character to append.
        /// </param>
        /// <returns>
        /// True if the character was appended (i.e. output is being captured);
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method appends a string to this channel's captured "virtual"
        /// output.
        /// </summary>
        /// <param name="value">
        /// The string to append.
        /// </param>
        /// <returns>
        /// True if the string was appended (i.e. output is being captured);
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method appends the bytes of the specified array to this
        /// channel's captured "virtual" output.
        /// </summary>
        /// <param name="value">
        /// The byte array to append.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the bytes were appended (i.e. output is being captured and
        /// the array is non-null and non-empty); otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method begins a new channel context that wraps a partial clone
        /// of the current channel stream (substituting the specified inner
        /// stream), saving the prior context into the supplied variable.
        /// </summary>
        /// <param name="stream">
        /// The inner stream to use for the new channel context.
        /// </param>
        /// <param name="savedContext">
        /// Upon success, receives the prior active context.  This parameter
        /// must be null on entry.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the context was begun successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method ends the current channel context, optionally closing it,
        /// and restores the context from the supplied saved-context variable.
        /// </summary>
        /// <param name="close">
        /// Non-zero to close the current (ending) context before restoring the
        /// saved context.
        /// </param>
        /// <param name="savedContext">
        /// The previously saved context to restore.  Upon success, this is
        /// cleared to null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the context was ended successfully; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the end-of-line parameters for the specified
        /// channel stream, falling back to sensible defaults when the stream is
        /// null or configured for binary translation.
        /// </summary>
        /// <param name="stream">
        /// The channel stream whose end-of-line parameters are requested.  This
        /// parameter may be null.
        /// </param>
        /// <param name="endOfLine">
        /// Upon return, receives the end-of-line character sequence to use.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Upon return, receives a value indicating whether any of the
        /// end-of-line characters terminates a line.
        /// </param>
        /// <param name="keepEndOfLineChars">
        /// Upon return, receives a value indicating whether the end-of-line
        /// characters are retained in the returned data.
        /// </param>
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

        /// <summary>
        /// This method reads bytes directly from the underlying stream using
        /// the end-of-line parameters currently in effect for this channel.
        /// </summary>
        /// <param name="count">
        /// The maximum number of bytes to read, or <see cref="Count.Invalid" />
        /// to read until an end-of-line or end-of-stream is encountered.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method reads bytes via the channel's buffered reading mechanism
        /// using the end-of-line parameters currently in effect for this
        /// channel.
        /// </summary>
        /// <param name="count">
        /// The maximum number of bytes to read, or <see cref="Count.Invalid" />
        /// to read a single line or all buffered bytes.
        /// </param>
        /// <param name="list">
        /// Upon return, receives the bytes that were read; the bytes are
        /// appended to an existing list when one is supplied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method applies the configured end-of-line translation to the
        /// specified input byte array, producing a translated output byte
        /// array.  The translation direction (input or output) is taken from
        /// <paramref name="direction" />.
        /// </summary>
        /// <param name="direction">
        /// The stream direction (and any extra flags, such as end-of-stream)
        /// that governs the translation to perform.
        /// </param>
        /// <param name="inputBuffer">
        /// The input byte array to translate.  This parameter may be null.
        /// </param>
        /// <param name="outputBuffer">
        /// Upon return, receives the translated output byte array; this is null
        /// when the input is null, and empty when the input is empty or the
        /// direction is invalid.
        /// </param>
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
            // NOTE: What is the base stream direction (i.e.
            //       without any extra flags)?
            //
            StreamDirection baseDirection =
                direction & StreamDirection.BaseMask;

            //
            // NOTE: The output length is based on the input
            //       length; for output, it may require each
            //       line-ending to be doubled.
            //
            int outputLength;

            if (baseDirection == StreamDirection.Output)
            {
                outputLength = ChannelOps.EstimateOutputCount(
                    inputBuffer, 0, inputLength);
            }
            else if (baseDirection == StreamDirection.Input)
            {
                outputLength = inputLength;
            }
            else
            {
                //
                // TODO: This is an invalid base direction.
                //       For now, garbage in, garbage out.
                //
                outputBuffer = new byte[0];
                return;
            }

            //
            // NOTE: Allocate an output buffer.
            //
            // BUGFIX: For input translation, reserve one extra byte: a carriage-
            //         return deferred at a previous read-buffer boundary (the
            //         StreamFlags.NeedLineFeed mechanism) is emitted at the FRONT
            //         of this translation without consuming an input byte, so the
            //         translated output can be (inputLength + 1) bytes.  The buffer
            //         is resized down to the actual translated length below.
            //         (COMPAT: Tcl; F47)
            //
            byte[] buffer = (baseDirection == StreamDirection.Input) ?
                new byte[outputLength + 1] : new byte[outputLength];

            //
            // NOTE: Use the underlying stream to perform
            //       the actual end-of-line transformations
            //       via the buffers we have prepared.  If
            //       the stream direction is neither Input
            //       only nor Output only, we do nothing.
            //
            if (baseDirection == StreamDirection.Output)
            {
                outputLength = stream.TranslateOutputEndOfLine(
                    inputBuffer, buffer, direction, 0, inputLength);
            }
            else if (baseDirection == StreamDirection.Input)
            {
                outputLength = stream.TranslateInputEndOfLine(
                    inputBuffer, buffer, direction, 0, inputLength);
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

        /// <summary>
        /// This method applies the configured end-of-line translation to the
        /// specified input byte list, producing a translated output byte list.
        /// The translation direction (input or output) is taken from
        /// <paramref name="direction" />.
        /// </summary>
        /// <param name="direction">
        /// The stream direction (and any extra flags, such as end-of-stream)
        /// that governs the translation to perform.
        /// </param>
        /// <param name="inputList">
        /// The input byte list to translate.  This parameter may be null.
        /// </param>
        /// <param name="outputList">
        /// Upon return, receives the translated output byte list; the
        /// translated bytes are appended to an existing list when one is
        /// supplied.
        /// </param>
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
            {
                if (outputList != null)
                    outputList.AddRange(outputBuffer);
                else
                    outputList = new ByteList(outputBuffer);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this channel, using
        /// its name when set, otherwise the string representation of its active
        /// context.
        /// </summary>
        /// <returns>
        /// The channel name, the active context's string representation, or
        /// null when neither is available.
        /// </returns>
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
        /// <summary>
        /// This method releases all resources held by this channel and
        /// suppresses finalization.
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
        /// Stores a value indicating whether this channel has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this channel has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this channel has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Channel).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this channel.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// Finalizes this channel, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Channel()
        {
            Dispose(false);
        }
        #endregion
    }
}
