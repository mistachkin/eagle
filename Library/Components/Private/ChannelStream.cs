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
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class wraps an underlying <see cref="Stream" /> (and, optionally, a
    /// socket or listener) used by an Eagle channel.  It augments the wrapped
    /// stream with end-of-line translation, channel-specific buffering, and
    /// flag-based behavior, delegating the bulk of the standard
    /// <see cref="Stream" /> operations to the wrapped stream.  The
    /// <see cref="Stream" /> base class itself is not otherwise used.
    /// </summary>
    [ObjectId("b9b9bfc0-b902-4476-afb9-116ddec7a779")]
    internal class ChannelStream : Stream /* BASE CLASS NOT USED */
    {
        #region Public Constants
        /// <summary>
        /// The sentinel value returned to indicate end-of-file (for example, by
        /// <c>gets</c> and related operations).
        /// </summary>
        public static readonly int EndOfFile = -1; /* [gets], et al */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
#if NETWORK
        /// <summary>
        /// The minimum amount of time, in milliseconds, to wait when polling a
        /// socket for available data.
        /// </summary>
        //
        // HACK: This is purposely not read-only.
        //
        private static int MinimumPollTimeout = 25; /* milliseconds */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region End-of-Line Static Data
        /// <summary>
        /// The end-of-line sequence consisting of a single line-feed character.
        /// </summary>
        internal static readonly CharList LineFeedCharList =
            new CharList(new char[] { Characters.LineFeed });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The end-of-line sequence consisting of a single carriage-return
        /// character.
        /// </summary>
        private static readonly CharList CarriageReturnCharList =
            new CharList(new char[] { Characters.CarriageReturn });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The end-of-line sequence consisting of a carriage-return followed by
        /// a line-feed character.
        /// </summary>
        internal static readonly CharList CarriageReturnLineFeedCharList =
            new CharList(new char[] {
            Characters.CarriageReturn, Characters.LineFeed
        });
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The type of channel associated with this stream.
        /// </summary>
        private ChannelType channelType;
        /// <summary>
        /// The original options that were in effect when this stream was
        /// opened.
        /// </summary>
        private OptionDictionary options; // ORIGINAL options when opening.
        /// <summary>
        /// The underlying stream wrapped by this stream, to which most
        /// operations are delegated.
        /// </summary>
        private Stream stream;
        /// <summary>
        /// The buffer used to hold partially read data that has not yet been
        /// returned to the caller.
        /// </summary>
        private ChannelStreamBuffer readBuffer;
        /// <summary>
        /// The flags controlling the behavior of this stream.
        /// </summary>
        private StreamFlags flags;
        /// <summary>
        /// The end-of-line translation mode applied to data read from this
        /// stream.
        /// </summary>
        private StreamTranslation inTranslation;
        /// <summary>
        /// The end-of-line translation mode applied to data written to this
        /// stream.
        /// </summary>
        private StreamTranslation outTranslation;

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        /// <summary>
        /// The total amount of time, in milliseconds, remaining to wait for
        /// data to become available, if any.
        /// </summary>
        private int? availableTimeout;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The socket listener associated with this stream, if any.
        /// </summary>
        private TcpListener listener;
        /// <summary>
        /// The socket associated with this stream, if any.
        /// </summary>
        private Socket socket;
        /// <summary>
        /// The amount of time, in milliseconds, to wait when closing the
        /// socket.
        /// </summary>
        private int timeout;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty instance of this class, allocating the read
        /// buffer.  This constructor is used by the other constructor overloads.
        /// </summary>
        private ChannelStream()
            : base()
        {
            readBuffer = new ChannelStreamBuffer();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
#if NETWORK
        /// <summary>
        /// Constructs an instance of this class that wraps a socket listener.
        /// </summary>
        /// <param name="listener">
        /// The socket listener associated with this stream.
        /// </param>
        /// <param name="channelType">
        /// The type of channel associated with this stream.
        /// </param>
        /// <param name="options">
        /// The original options that were in effect when this stream was
        /// opened.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of this stream.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class that wraps a socket.
        /// </summary>
        /// <param name="socket">
        /// The socket associated with this stream.
        /// </param>
        /// <param name="timeout">
        /// The amount of time, in milliseconds, to wait when closing the
        /// socket.
        /// </param>
        /// <param name="channelType">
        /// The type of channel associated with this stream.
        /// </param>
        /// <param name="options">
        /// The original options that were in effect when this stream was
        /// opened.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of this stream.
        /// </param>
        /// <param name="inTranslation">
        /// The end-of-line translation mode applied to data read from this
        /// stream.
        /// </param>
        /// <param name="outTranslation">
        /// The end-of-line translation mode applied to data written to this
        /// stream.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class that wraps an underlying stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream wrapped by this stream, to which most
        /// operations are delegated.
        /// </param>
        /// <param name="channelType">
        /// The type of channel associated with this stream.
        /// </param>
        /// <param name="options">
        /// The original options that were in effect when this stream was
        /// opened.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of this stream.
        /// </param>
        /// <param name="inTranslation">
        /// The end-of-line translation mode applied to data read from this
        /// stream.
        /// </param>
        /// <param name="outTranslation">
        /// The end-of-line translation mode applied to data written to this
        /// stream.
        /// </param>
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
        /// <summary>
        /// This method creates a new stream that shares the channel type,
        /// options, flags, and translation modes of this stream but wraps a
        /// different underlying stream.
        /// </summary>
        /// <param name="stream">
        /// The underlying stream to be wrapped by the new stream.
        /// </param>
        /// <returns>
        /// The newly created stream.
        /// </returns>
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
        /// <summary>
        /// This method returns the socket listener associated with this stream.
        /// </summary>
        /// <returns>
        /// The socket listener associated with this stream, or null if there is
        /// none.
        /// </returns>
        public virtual TcpListener GetListener()
        {
            CheckDisposed();

            return listener;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the socket associated with this stream.
        /// </summary>
        /// <returns>
        /// The socket associated with this stream, or null if there is none.
        /// </returns>
        public virtual Socket GetSocket()
        {
            CheckDisposed();

            return socket;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the original options that were in effect when
        /// this stream was opened.
        /// </summary>
        /// <returns>
        /// The original options that were in effect when this stream was
        /// opened.
        /// </returns>
        public virtual OptionDictionary GetOptions()
        {
            CheckDisposed();

            return options;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the underlying stream wrapped by this stream.
        /// </summary>
        /// <returns>
        /// The underlying stream wrapped by this stream, or null if there is
        /// none.
        /// </returns>
        public virtual Stream GetStream()
        {
            CheckDisposed();

            return stream;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Channel Type Members
#if CONSOLE
        /// <summary>
        /// This method determines whether this stream is associated with a
        /// console channel.
        /// </summary>
        /// <returns>
        /// True if this stream is associated with a console channel; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the flags for this stream include the
        /// specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be present; zero
        /// to require that any of them be present.
        /// </param>
        /// <returns>
        /// True if the flags for this stream include the specified flags subject
        /// to <paramref name="all" />; otherwise, false.
        /// </returns>
        public virtual bool HasFlags(
            StreamFlags flags, /* in */
            bool all           /* in */
            )
        {
            CheckDisposed();

            return PrivateHasFlags(flags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the specified flags for this stream.
        /// </summary>
        /// <param name="flags">
        /// The flags to set or clear.
        /// </param>
        /// <param name="set">
        /// Non-zero to set the specified flags; zero to clear them.
        /// </param>
        /// <returns>
        /// The flags for this stream after the change.
        /// </returns>
        public virtual StreamFlags SetFlags(
            StreamFlags flags, /* in */
            bool set           /* in */
            )
        {
            CheckDisposed();

            return PrivateSetFlags(flags, set);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether closing this stream is
        /// prevented.
        /// </summary>
        public virtual bool PreventClose
        {
            get
            {
                CheckDisposed();

                return PrivateHasFlags(
                    StreamFlags.PreventClose, true);
            }
            set
            {
                CheckDisposed();

                PrivateSetFlags(
                    StreamFlags.PreventClose, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this stream requires the use
        /// of its read buffer.
        /// </summary>
        public virtual bool NeedBuffer
        {
            get
            {
                CheckDisposed();

                return PrivateHasFlags(
                    StreamFlags.NeedBuffer, true);
            }
            set
            {
                CheckDisposed();

                PrivateSetFlags(
                    StreamFlags.NeedBuffer, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the flags controlling the behavior of this stream.
        /// </summary>
        private StreamFlags PrivateFlags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the flags for this stream include the
        /// specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags be present; zero
        /// to require that any of them be present.
        /// </param>
        /// <returns>
        /// True if the flags for this stream include the specified flags subject
        /// to <paramref name="all" />; otherwise, false.
        /// </returns>
        private bool PrivateHasFlags(
            StreamFlags flags, /* in */
            bool all           /* in */
            )
        {
            if (all)
                return ((this.flags & flags) == flags);
            else
                return ((this.flags & flags) != StreamFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the specified flags for this stream.
        /// </summary>
        /// <param name="flags">
        /// The flags to set or clear.
        /// </param>
        /// <param name="set">
        /// Non-zero to set the specified flags; zero to clear them.
        /// </param>
        /// <returns>
        /// The flags for this stream after the change.
        /// </returns>
        private StreamFlags PrivateSetFlags(
            StreamFlags flags, /* in */
            bool set           /* in */
            )
        {
            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears all of the line-ending flags in the specified set
        /// of flags.
        /// </summary>
        /// <param name="flags">
        /// The set of flags to modify.  Upon return, all of the line-ending
        /// flags will have been cleared.
        /// </param>
        private void ResetLineEndingFlags(
            ref StreamFlags flags /* in, out */
            )
        {
            flags &= ~StreamFlags.LineEndingMask;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears all of the extra line-ending flags in the
        /// specified set of flags.
        /// </summary>
        /// <param name="flags">
        /// The set of flags to modify.  Upon return, all of the extra
        /// line-ending flags will have been cleared.
        /// </param>
        private void ResetExtraLineEndingFlags(
            ref StreamFlags flags /* in, out */
            )
        {
            flags &= ~StreamFlags.ExtraLineEndingMask;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region End-of-Line Translation Members
        /// <summary>
        /// Gets or sets the end-of-line translation mode applied to data read
        /// from this stream.
        /// </summary>
        public virtual StreamTranslation InputTranslation
        {
            get { CheckDisposed(); return inTranslation; }
            set { CheckDisposed(); inTranslation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the end-of-line translation mode applied to data written
        /// to this stream.
        /// </summary>
        public virtual StreamTranslation OutputTranslation
        {
            get { CheckDisposed(); return outTranslation; }
            set { CheckDisposed(); outTranslation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the end-of-line translation mode appropriate for
        /// reading on the current operating system environment.
        /// </summary>
        /// <returns>
        /// The end-of-line translation mode appropriate for reading on the
        /// current operating system environment.
        /// </returns>
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

        /// <summary>
        /// This method resolves the specified end-of-line translation mode for
        /// reading, substituting the environment-appropriate mode when the
        /// environment mode is specified.
        /// </summary>
        /// <param name="translation">
        /// The end-of-line translation mode to resolve.
        /// </param>
        /// <returns>
        /// The environment-appropriate translation mode when
        /// <paramref name="translation" /> is the environment mode; otherwise,
        /// <paramref name="translation" /> unchanged.
        /// </returns>
        public virtual StreamTranslation GetEnvironmentInputTranslation(
            StreamTranslation translation /* in */
            )
        {
            CheckDisposed();

            return (translation == StreamTranslation.environment) ?
                GetEnvironmentInputTranslation() : translation;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the end-of-line translation mode appropriate for
        /// writing on the current operating system environment.
        /// </summary>
        /// <returns>
        /// The end-of-line translation mode appropriate for writing on the
        /// current operating system environment.
        /// </returns>
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

        /// <summary>
        /// This method resolves the specified end-of-line translation mode for
        /// writing, substituting the environment-appropriate mode when the
        /// environment mode is specified.
        /// </summary>
        /// <param name="translation">
        /// The end-of-line translation mode to resolve.
        /// </param>
        /// <returns>
        /// The environment-appropriate translation mode when
        /// <paramref name="translation" /> is the environment mode; otherwise,
        /// <paramref name="translation" /> unchanged.
        /// </returns>
        public virtual StreamTranslation GetEnvironmentOutputTranslation(
            StreamTranslation translation /* in */
            )
        {
            CheckDisposed();

            return (translation == StreamTranslation.environment) ?
                GetEnvironmentOutputTranslation() : translation;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the end-of-line sequence used when reading from this stream,
        /// based on the input translation mode in effect.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether any end-of-line character is
        /// recognized as a line ending when reading from this stream.
        /// </summary>
        public virtual bool UseAnyEndOfLineChar
        {
            get
            {
                CheckDisposed();

                return PrivateHasFlags(
                    StreamFlags.UseAnyEndOfLineChar, true);
            }
            set
            {
                CheckDisposed();

                PrivateSetFlags(
                    StreamFlags.UseAnyEndOfLineChar, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether end-of-line characters are
        /// kept when reading from this stream.
        /// </summary>
        public virtual bool KeepEndOfLineChars
        {
            get
            {
                CheckDisposed();

                return PrivateHasFlags(
                    StreamFlags.KeepEndOfLineChars, true);
            }
            set
            {
                CheckDisposed();

                PrivateSetFlags(
                    StreamFlags.KeepEndOfLineChars, value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the end-of-line sequence used when writing to this stream, based
        /// on the output translation mode in effect.
        /// </summary>
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

        /// <summary>
        /// This method applies input end-of-line translation to a buffer of
        /// bytes, writing the translated bytes into the output buffer.
        /// </summary>
        /// <param name="inBuffer">
        /// The buffer containing the bytes to be translated.
        /// </param>
        /// <param name="outBuffer">
        /// The buffer that receives the translated bytes.
        /// </param>
        /// <param name="direction">
        /// The direction flags controlling how the translation is performed.
        /// </param>
        /// <param name="offset">
        /// The offset, within the output buffer, at which to begin writing the
        /// translated bytes.
        /// </param>
        /// <param name="inCount">
        /// The number of bytes from the input buffer to translate.
        /// </param>
        /// <returns>
        /// The number of bytes written to the output buffer.
        /// </returns>
        public int TranslateInputEndOfLine(
            byte[] inBuffer,           /* in */
            byte[] outBuffer,          /* in, out */
            StreamDirection direction, /* in */
            int offset,                /* in */
            int inCount                /* in */
            )
        {
            IntList lineEndings = null;

            return TranslateInputEndOfLine(
                inBuffer, outBuffer, direction, offset, inCount,
                ref lineEndings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method applies input end-of-line translation to a buffer of
        /// bytes, writing the translated bytes into the output buffer and
        /// collecting the positions of the line endings.  The flags for this
        /// stream are saved and restored around the translation.
        /// </summary>
        /// <param name="inBuffer">
        /// The buffer containing the bytes to be translated.
        /// </param>
        /// <param name="outBuffer">
        /// The buffer that receives the translated bytes.
        /// </param>
        /// <param name="direction">
        /// The direction flags controlling how the translation is performed.
        /// </param>
        /// <param name="offset">
        /// The offset, within the output buffer, at which to begin writing the
        /// translated bytes.
        /// </param>
        /// <param name="inCount">
        /// The number of bytes from the input buffer to translate.
        /// </param>
        /// <param name="lineEndings">
        /// The list that receives the positions of the line endings within the
        /// translated output.
        /// </param>
        /// <returns>
        /// The number of bytes written to the output buffer.
        /// </returns>
        private int TranslateInputEndOfLine(
            byte[] inBuffer,           /* in */
            byte[] outBuffer,          /* in, out */
            StreamDirection direction, /* in */
            int offset,                /* in */
            int inCount,               /* in */
            ref IntList lineEndings    /* in, out */
            )
        {
            StreamFlags flags = PrivateFlags;

            try
            {
                return TranslateInputEndOfLine(
                    inBuffer, outBuffer, direction, offset,
                    inCount, ref lineEndings, ref flags);
            }
            finally
            {
                PrivateFlags = flags;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method applies input end-of-line translation to a buffer of
        /// bytes, writing the translated bytes into the output buffer, collecting
        /// the positions of the line endings, and updating the supplied flags to
        /// account for line endings that span buffer boundaries.
        /// </summary>
        /// <param name="inBuffer">
        /// The buffer containing the bytes to be translated.
        /// </param>
        /// <param name="outBuffer">
        /// The buffer that receives the translated bytes.
        /// </param>
        /// <param name="direction">
        /// The direction flags controlling how the translation is performed.
        /// </param>
        /// <param name="offset">
        /// The offset, within the output buffer, at which to begin writing the
        /// translated bytes.
        /// </param>
        /// <param name="inCount">
        /// The number of bytes from the input buffer to translate.
        /// </param>
        /// <param name="lineEndings">
        /// The list that receives the positions of the line endings within the
        /// translated output.
        /// </param>
        /// <param name="flags">
        /// The flags used and updated during translation; upon return, they
        /// reflect any line-ending state that spans buffer boundaries.
        /// </param>
        /// <returns>
        /// The number of bytes written to the output buffer.
        /// </returns>
        protected virtual int TranslateInputEndOfLine(
            byte[] inBuffer,           /* in */
            byte[] outBuffer,          /* in, out */
            StreamDirection direction, /* in */
            int offset,                /* in */
            int inCount,               /* in */
            ref IntList lineEndings,   /* in, out */
            ref StreamFlags flags      /* in, out */
            )
        {
            bool ignoreFlags = FlagOps.HasFlags(
                direction, StreamDirection.IgnoreFlags, true);

            bool endOfStream = FlagOps.HasFlags(
                direction, StreamDirection.EndOfStream, true);

            bool anyEndOfLine = FlagOps.HasFlags(
                direction, StreamDirection.AnyEndOfLine, true);

            switch (GetEnvironmentInputTranslation(inTranslation))
            {
                case StreamTranslation.binary:
                case StreamTranslation.lf:
                case StreamTranslation.protocol:
                    {
                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, inCount);

                        return inCount;
                    }
                case StreamTranslation.cr:
                    {
                        int newCount = offset + inCount;

                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, inCount);

                        for (int outIndex = offset; outIndex < newCount;
                                outIndex++)
                        {
                            if (outBuffer[outIndex] == ChannelOps.CarriageReturn)
                            {
                                ListOps.Add(outIndex, ref lineEndings);
                                outBuffer[outIndex] = ChannelOps.NewLine;
                            }
                            else if (anyEndOfLine &&
                                (outBuffer[outIndex] == ChannelOps.LineFeed))
                            {
                                ListOps.Add(outIndex, ref lineEndings);
                            }
                        }

                        return inCount;
                    }
                case StreamTranslation.crlf:
                case StreamTranslation.platform:
                    {
                        int newCount = offset + inCount;
                        int inIndex = offset;
                        int outIndex = 0;

                        if (!ignoreFlags && FlagOps.HasFlags(
                                flags, StreamFlags.NeedLineFeed, true))
                        {
                            //
                            // NOTE: A carriage-return was the LAST byte of a
                            //       previous chunk and its output was DEFERRED
                            //       (this flag) so that we could see whether a
                            //       line-feed begins the next chunk -- i.e. so
                            //       that a carriage-return / line-feed pair that
                            //       straddles a read-buffer boundary collapses to
                            //       a single newline (just like one wholly inside
                            //       a single chunk, handled by the main loop
                            //       below).  Decide its fate now.
                            //
                            if (inIndex < newCount)
                            {
                                if (inBuffer[inIndex] == ChannelOps.LineFeed)
                                {
                                    //
                                    // BUGFIX: This line feed is being consumed;
                                    //         therefore, it should not be copied
                                    //         (again) in the main loop (below),
                                    //         even if that main loop is entered
                                    //         the next time this method is used.
                                    //
                                    inIndex++;

                                    ListOps.Add(outIndex, ref lineEndings);
                                    outBuffer[outIndex++] = ChannelOps.NewLine;
                                }
                                else
                                {
                                    //
                                    // BUGFIX: The deferred carriage-return was
                                    //         NOT followed by a line-feed; it is
                                    //         a "naked" carriage-return straddling
                                    //         the read-buffer boundary.  It MUST be
                                    //         emitted now (exactly as the in-buffer
                                    //         naked carriage-return path below does)
                                    //         or it is silently LOST -- the cause of
                                    //         intermittently dropped carriage-returns
                                    //         when a lone CR happened to land on a
                                    //         channel read-buffer boundary.  The next
                                    //         byte is left for the main loop.
                                    //         (COMPAT: Tcl; F47)
                                    //
                                    flags |= StreamFlags.ExtraCarriageReturn;

                                    if (anyEndOfLine)
                                        ListOps.Add(outIndex, ref lineEndings);

                                    outBuffer[outIndex++] = ChannelOps.CarriageReturn;
                                }

                                flags &= ~StreamFlags.NeedLineFeed;
                            }
                            else if (endOfStream)
                            {
                                //
                                // BUGFIX: There is no next byte AND no more input
                                //         is coming, so the deferred carriage-return
                                //         can never be part of a pair; emit it as a
                                //         naked carriage-return now (otherwise it is
                                //         lost at end-of-stream).  (COMPAT: Tcl; F47)
                                //
                                flags |= StreamFlags.ExtraCarriageReturn;

                                if (anyEndOfLine)
                                    ListOps.Add(outIndex, ref lineEndings);

                                outBuffer[outIndex++] = ChannelOps.CarriageReturn;

                                flags &= ~StreamFlags.NeedLineFeed;
                            }

                            //
                            // BUGFIX: Otherwise, this was an EMPTY read (no bytes
                            //         and not end-of-stream, e.g. a non-blocking
                            //         channel that would block); the carriage-return
                            //         must REMAIN deferred -- clearing the flag here
                            //         (as was previously done unconditionally) would
                            //         drop it.  (COMPAT: Tcl; F47)
                            //
                        }

                        for (; inIndex < newCount; )
                        {
                            if (inBuffer[inIndex] == ChannelOps.CarriageReturn)
                            {
                                if (++inIndex >= newCount)
                                {
                                    if (anyEndOfLine)
                                        ListOps.Add(outIndex, ref lineEndings);

                                    if (endOfStream)
                                    {
                                        //
                                        // NOTE: This is a carriage-return (?) -AND-
                                        //       there is no more input coming; so,
                                        //       include it.
                                        //
                                        // BUGFIX: Do NOT also set NeedLineFeed here:
                                        //         the carriage-return is being emitted
                                        //         now, so there is nothing left to
                                        //         defer.  Leaving it set caused a
                                        //         spurious duplicate carriage-return
                                        //         to be emitted by the deferred-CR
                                        //         handler (above) on a subsequent
                                        //         (empty, end-of-stream) read.
                                        //         (COMPAT: Tcl; F47)
                                        //
                                        outBuffer[outIndex++] = inBuffer[inIndex - 1];
                                        flags |= StreamFlags.LastCarriageReturn;
                                    }
                                    else if (!ignoreFlags)
                                    {
                                        //
                                        // NOTE: The carriage-return is the last byte
                                        //       of this chunk; defer its output until
                                        //       the next chunk reveals whether a line-
                                        //       feed follows (collapse to newline) or
                                        //       not (naked carriage-return).
                                        //
                                        flags |= StreamFlags.NeedLineFeed;
                                    }
                                }
                                else if (inBuffer[inIndex] == ChannelOps.LineFeed)
                                {
                                    ListOps.Add(outIndex, ref lineEndings);
                                    outBuffer[outIndex++] = inBuffer[inIndex++];
                                }
                                else
                                {
                                    if (!ignoreFlags)
                                    {
                                        //
                                        // NOTE: This is a "naked" carriage-return
                                        //       without a following line-feed?
                                        //
                                        flags |= StreamFlags.ExtraCarriageReturn;
                                    }

                                    //
                                    // NOTE: This is a carriage-return (?).
                                    //
                                    if (anyEndOfLine)
                                        ListOps.Add(outIndex, ref lineEndings);

                                    outBuffer[outIndex++] = inBuffer[inIndex - 1];
                                }
                            }
                            else
                            {
                                if (inBuffer[inIndex] == ChannelOps.LineFeed)
                                {
                                    if (!ignoreFlags)
                                    {
                                        //
                                        // NOTE: This is a "naked" line-feed without
                                        //       a preceding carriage-return?
                                        //
                                        flags |= StreamFlags.ExtraLineFeed;
                                    }

                                    ListOps.Add(Index.Invalid, ref lineEndings);
                                }

                                outBuffer[outIndex++] = inBuffer[inIndex++];
                            }
                        }

                        return outIndex;
                    }
                case StreamTranslation.auto:
                    {
                        int newCount = offset + inCount;
                        int inIndex = offset;
                        int outIndex = 0;

                        if (!ignoreFlags && FlagOps.HasFlags(
                                flags, StreamFlags.SawCarriageReturn, true))
                        {
                            if (inIndex < newCount)
                            {
                                if (inBuffer[inIndex] == ChannelOps.LineFeed)
                                {
                                    //
                                    // BUGFIX: This line feed is being consumed;
                                    //         therefore, it should not be copied
                                    //         (again) in the main loop (below),
                                    //         even if that main loop is entered
                                    //         the next time this method is used.
                                    //
                                    inIndex++;
                                }
                            }

                            flags &= ~StreamFlags.SawCarriageReturn;
                        }

                        for (; inIndex < newCount; )
                        {
                            if (inBuffer[inIndex] == ChannelOps.CarriageReturn)
                            {
                                if (++inIndex >= newCount)
                                {
                                    if (!ignoreFlags)
                                        flags |= StreamFlags.SawCarriageReturn;
                                }
                                else if (inBuffer[inIndex] == ChannelOps.LineFeed)
                                {
                                    inIndex++;
                                }

                                ListOps.Add(outIndex, ref lineEndings);
                                outBuffer[outIndex++] = ChannelOps.NewLine;
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

        /// <summary>
        /// This method applies output end-of-line translation to a buffer of
        /// bytes, writing the translated bytes into the output buffer.  The flags
        /// for this stream are saved and restored around the translation.
        /// </summary>
        /// <param name="inBuffer">
        /// The buffer containing the bytes to be translated.
        /// </param>
        /// <param name="outBuffer">
        /// The buffer that receives the translated bytes.
        /// </param>
        /// <param name="direction">
        /// The direction flags controlling how the translation is performed.
        /// </param>
        /// <param name="offset">
        /// The offset, within the output buffer, at which to begin writing the
        /// translated bytes.
        /// </param>
        /// <param name="inCount">
        /// The number of bytes from the input buffer to translate.
        /// </param>
        /// <returns>
        /// The number of bytes written to the output buffer.
        /// </returns>
        public int TranslateOutputEndOfLine(
            byte[] inBuffer,           /* in */
            byte[] outBuffer,          /* in, out */
            StreamDirection direction, /* in */
            int offset,                /* in */
            int inCount                /* in */
            )
        {
            StreamFlags flags = PrivateFlags;

            try
            {
                return TranslateOutputEndOfLine(
                    inBuffer, outBuffer, direction, offset,
                    inCount, ref flags);
            }
            finally
            {
                PrivateFlags = flags;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method applies output end-of-line translation to a buffer of
        /// bytes, writing the translated bytes into the output buffer and
        /// updating the supplied flags to account for line endings that span
        /// buffer boundaries.
        /// </summary>
        /// <param name="inBuffer">
        /// The buffer containing the bytes to be translated.
        /// </param>
        /// <param name="outBuffer">
        /// The buffer that receives the translated bytes.
        /// </param>
        /// <param name="direction">
        /// The direction flags controlling how the translation is performed.
        /// </param>
        /// <param name="offset">
        /// The offset, within the output buffer, at which to begin writing the
        /// translated bytes.
        /// </param>
        /// <param name="inCount">
        /// The number of bytes from the input buffer to translate.
        /// </param>
        /// <param name="flags">
        /// The flags used and updated during translation; upon return, they
        /// reflect any line-ending state that spans buffer boundaries.
        /// </param>
        /// <returns>
        /// The number of bytes written to the output buffer.
        /// </returns>
        protected virtual int TranslateOutputEndOfLine(
            byte[] inBuffer,           /* in */
            byte[] outBuffer,          /* in, out */
            StreamDirection direction, /* in */
            int offset,                /* in */
            int inCount,               /* in */
            ref StreamFlags flags      /* in, out */
            )
        {
            bool ignoreFlags = FlagOps.HasFlags(
                direction, StreamDirection.IgnoreFlags, true);

            switch (GetEnvironmentOutputTranslation(outTranslation))
            {
                case StreamTranslation.binary:
                case StreamTranslation.lf:
                    {
                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, inCount);

                        return inCount;
                    }
                case StreamTranslation.cr:
                    {
                        int newCount = offset + inCount;

                        Array.Copy(
                            inBuffer, 0, outBuffer, offset, inCount);

                        for (int outIndex = offset; outIndex < newCount;
                                outIndex++)
                        {
                            if (outBuffer[outIndex] == ChannelOps.LineFeed)
                                outBuffer[outIndex] = ChannelOps.CarriageReturn;
                        }

                        return inCount;
                    }
                case StreamTranslation.crlf:
                case StreamTranslation.platform:
                case StreamTranslation.auto:
                    {
                        int newCount = offset + inCount;
                        int inIndex = offset;
                        int outIndex = 0;

                        for (; inIndex < newCount; )
                        {
                            if (inBuffer[inIndex] == ChannelOps.LineFeed)
                                outBuffer[outIndex++] = ChannelOps.CarriageReturn;

                            outBuffer[outIndex++] = inBuffer[inIndex++];
                        }

                        return outIndex;
                    }
                case StreamTranslation.protocol: /* NOTE: Enforce CR/LF. */
                    {
                        int newCount = offset + inCount;
                        int inIndex = offset;
                        int outIndex = 0;

                        for (; inIndex < newCount; )
                        {
                            //
                            // NOTE: Have we seen an unpaired carriage-return?
                            //
                            bool sawCarriageReturn;

                            if (ignoreFlags)
                            {
                                sawCarriageReturn = false;
                            }
                            else
                            {
                                sawCarriageReturn = FlagOps.HasFlags(
                                    flags, StreamFlags.SawCarriageReturn, true);
                            }

                            //
                            // NOTE: Is the current character carriage-return?
                            //
                            if (inBuffer[inIndex] == ChannelOps.CarriageReturn)
                            {
                                //
                                // NOTE: If we have already seen an unpaired
                                //       carriage-return we need to add a
                                //       line-feed now before doing anything
                                //       else to complete the pairing.
                                //
                                if (sawCarriageReturn)
                                    outBuffer[outIndex++] = ChannelOps.LineFeed;

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
                                    outBuffer[outIndex++] = ChannelOps.LineFeed;
                                }
                                else if (!ignoreFlags)
                                {
                                    flags |= StreamFlags.SawCarriageReturn;
                                }
                            }
                            //
                            // NOTE: Otherwise, is current character line-feed?
                            //
                            else if (inBuffer[inIndex] == ChannelOps.LineFeed)
                            {
                                //
                                // NOTE: If we have not seen an unpaired
                                //       carriage-return yet, we need to add
                                //       one now for the pairing to be complete
                                //       when we emit the line-feed below.
                                //
                                if (!sawCarriageReturn)
                                    outBuffer[outIndex++] = ChannelOps.CarriageReturn;

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
                                if (!ignoreFlags && sawCarriageReturn)
                                    flags &= ~StreamFlags.SawCarriageReturn;
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
                                    outBuffer[outIndex++] = ChannelOps.LineFeed;

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
                                if (!ignoreFlags && sawCarriageReturn)
                                    flags &= ~StreamFlags.SawCarriageReturn;
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
        /// <summary>
        /// This method begins an asynchronous read operation, delegating to the
        /// underlying stream.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that receives the bytes read from the stream.
        /// </param>
        /// <param name="offset">
        /// The offset, within the buffer, at which to begin storing the bytes
        /// read.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the asynchronous read operation completes.
        /// This parameter may be null.
        /// </param>
        /// <param name="state">
        /// The user-provided object that distinguishes this asynchronous read
        /// request from others.  This parameter may be null.
        /// </param>
        /// <returns>
        /// An object that represents the asynchronous read operation.
        /// </returns>
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

        /// <summary>
        /// This method begins an asynchronous write operation, delegating to the
        /// underlying stream.
        /// </summary>
        /// <param name="buffer">
        /// The buffer containing the bytes to write to the stream.
        /// </param>
        /// <param name="offset">
        /// The offset, within the buffer, at which to begin reading the bytes to
        /// write.
        /// </param>
        /// <param name="count">
        /// The number of bytes to write.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the asynchronous write operation
        /// completes.  This parameter may be null.
        /// </param>
        /// <param name="state">
        /// The user-provided object that distinguishes this asynchronous write
        /// request from others.  This parameter may be null.
        /// </param>
        /// <returns>
        /// An object that represents the asynchronous write operation.
        /// </returns>
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

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// reading.
        /// </summary>
        public override bool CanRead
        {
            get { CheckDisposed(); return stream.CanRead; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { CheckDisposed(); return stream.CanSeek; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// timing out.
        /// </summary>
        public override bool CanTimeout
        {
            get { CheckDisposed(); return stream.CanTimeout; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports
        /// writing.
        /// </summary>
        public override bool CanWrite
        {
            get { CheckDisposed(); return stream.CanWrite; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes this stream.  Unless closing is prevented, it
        /// stops the socket listener, shuts down and closes the socket, and
        /// closes the underlying stream, as applicable.
        /// </summary>
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

        /// <summary>
        /// This method is not supported by this stream.
        /// </summary>
        /// <returns>
        /// This method does not return; it always throws an exception.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Always thrown, because this method is not supported.
        /// </exception>
        [Obsolete()]
        protected override WaitHandle CreateWaitHandle()
        {
            throw new NotImplementedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for a pending asynchronous read operation to
        /// complete, delegating to the underlying stream.
        /// </summary>
        /// <param name="asyncResult">
        /// The object that represents the pending asynchronous read operation.
        /// </param>
        /// <returns>
        /// The number of bytes read from the stream.
        /// </returns>
        public override int EndRead(
            IAsyncResult asyncResult /* in */
            )
        {
            CheckDisposed();

            return stream.EndRead(asyncResult);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for a pending asynchronous write operation to
        /// complete, delegating to the underlying stream.
        /// </summary>
        /// <param name="asyncResult">
        /// The object that represents the pending asynchronous write operation.
        /// </param>
        public override void EndWrite(
            IAsyncResult asyncResult /* in */
            )
        {
            CheckDisposed();

            stream.EndWrite(asyncResult);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method flushes any buffered data to the underlying stream,
        /// delegating to the underlying stream.
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();

            stream.Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the length, in bytes, of the underlying stream.
        /// </summary>
        public override long Length
        {
            get { CheckDisposed(); return stream.Length; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the current position within the underlying stream.
        /// </summary>
        public override long Position
        {
            get { CheckDisposed(); return stream.Position; }
            set { CheckDisposed(); stream.Position = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a sequence of bytes from the underlying stream,
        /// applying input end-of-line translation.  The flags for this stream
        /// are saved and restored around the operation.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that receives the bytes read from the stream.
        /// </param>
        /// <param name="offset">
        /// The offset, within the buffer, at which to begin storing the bytes
        /// read.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <returns>
        /// The number of bytes read into the buffer.
        /// </returns>
        public override int Read(
            byte[] buffer, /* in, out */
            int offset,    /* in */
            int count      /* in */
            )
        {
            CheckDisposed();

            StreamFlags flags = PrivateFlags;

            try
            {
                return Read(
                    buffer, offset, count, ref flags);
            }
            finally
            {
                PrivateFlags = flags;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a sequence of bytes from the underlying stream,
        /// applying input end-of-line translation and updating the supplied flags
        /// to account for line endings that span read-buffer boundaries.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that receives the bytes read from the stream.
        /// </param>
        /// <param name="offset">
        /// The offset, within the buffer, at which to begin storing the bytes
        /// read.
        /// </param>
        /// <param name="count">
        /// The maximum number of bytes to read.
        /// </param>
        /// <param name="flags">
        /// The flags used and updated during the read; upon return, they reflect
        /// any line-ending state that spans read-buffer boundaries.
        /// </param>
        /// <returns>
        /// The number of bytes read into the buffer.
        /// </returns>
        protected virtual int Read(
            byte[] buffer,        /* in, out */
            int offset,           /* in */
            int count,            /* in */
            ref StreamFlags flags /* in, out */
            )
        {
            int newCount; /* REUSED */

            if (inTranslation != StreamTranslation.binary)
            {
                //
                // BUGFIX: A carriage-return deferred at a previous read-buffer
                //         boundary (StreamFlags.NeedLineFeed) is emitted at the
                //         FRONT of the translation below WITHOUT consuming an
                //         input byte, so the translated output can be one byte
                //         longer than the bytes read.  Reserve that one slot in
                //         the caller's (fixed-size) buffer by reading at most one
                //         fewer byte from the underlying stream.  (COMPAT: Tcl;
                //         F47)
                //
                bool needLineFeed = FlagOps.HasFlags(
                    flags, StreamFlags.NeedLineFeed, true);

                int readCount = count;

                if (needLineFeed && (readCount > 0))
                    readCount--;

                if (needLineFeed && (count > 0) && (readCount == 0))
                {
                    //
                    // NOTE: The caller's buffer has room for exactly one byte
                    //       and a carriage-return is still deferred; emit it (as
                    //       a naked carriage-return) so forward progress is made
                    //       and the caller does not mistake a zero-length result
                    //       for end-of-stream.  (A carriage-return / line-feed
                    //       pair split across a single-byte read cannot be
                    //       collapsed here, but real readers never request a
                    //       single byte at a time.)
                    //
                    flags &= ~StreamFlags.NeedLineFeed;
                    flags |= StreamFlags.ExtraCarriageReturn;

                    buffer[offset] = ChannelOps.CarriageReturn;
                    return 1;
                }

                byte[] input = new byte[readCount];

                newCount = stream.Read(input, 0, readCount);

                if (FlagOps.HasFlags(
                        flags, StreamFlags.TraceReadLines, true))
                {
                    ChannelOps.TraceLineEndings("text", this,
                        input, newCount, TracePriority.Highest);
                }

                IntList lineEndings = null; /* NOT USED */

                return TranslateInputEndOfLine(
                    input, buffer, StreamDirection.None,
                    offset, newCount, ref lineEndings,
                    ref flags);
            }
            else
            {
                newCount = stream.Read(buffer, offset, count);

                if (FlagOps.HasFlags(
                        flags, StreamFlags.TraceReadLines, true))
                {
                    ChannelOps.TraceLineEndings("binary", this,
                        buffer, newCount, TracePriority.Highest);
                }

                return newCount;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single byte from the underlying stream,
        /// delegating to the underlying stream.
        /// </summary>
        /// <returns>
        /// The byte read from the stream, or <see cref="EndOfFile" /> if the end
        /// of the stream has been reached.
        /// </returns>
        public override int ReadByte()
        {
            CheckDisposed();

            return stream.ReadByte();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, that a read
        /// operation on the underlying stream will block before timing out.
        /// </summary>
        public override int ReadTimeout
        {
            get { CheckDisposed(); return stream.ReadTimeout; }
            set { CheckDisposed(); stream.ReadTimeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the position within the underlying stream,
        /// delegating to the underlying stream.
        /// </summary>
        /// <param name="offset">
        /// The offset, relative to <paramref name="origin" />, at which to set
        /// the new position.
        /// </param>
        /// <param name="origin">
        /// The reference point used to obtain the new position.
        /// </param>
        /// <returns>
        /// The new position within the stream.
        /// </returns>
        public override long Seek(
            long offset,      /* in */
            SeekOrigin origin /* in */
            )
        {
            CheckDisposed();

            return stream.Seek(offset, origin);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the length of the underlying stream, delegating to
        /// the underlying stream.
        /// </summary>
        /// <param name="value">
        /// The desired length, in bytes, of the underlying stream.
        /// </param>
        public override void SetLength(
            long value /* in */
            )
        {
            CheckDisposed();

            stream.SetLength(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a sequence of bytes to the underlying stream,
        /// applying output end-of-line translation unless the output translation
        /// mode is binary.
        /// </summary>
        /// <param name="buffer">
        /// The buffer containing the bytes to write to the stream.
        /// </param>
        /// <param name="offset">
        /// The offset, within the buffer, at which to begin reading the bytes to
        /// write.
        /// </param>
        /// <param name="count">
        /// The number of bytes to write.
        /// </param>
        public override void Write(
            byte[] buffer, /* in */
            int offset,    /* in */
            int count      /* in */
            )
        {
            CheckDisposed();

            if (outTranslation != StreamTranslation.binary)
            {
                int newCount = ChannelOps.EstimateOutputCount(
                    buffer, offset, count);

                byte[] output = new byte[newCount];

                newCount = TranslateOutputEndOfLine(
                    buffer, output, StreamDirection.None,
                    offset, count);

                stream.Write(output, 0, newCount);
            }
            else
            {
                stream.Write(buffer, offset, count);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a single byte to the underlying stream, delegating
        /// to the underlying stream.
        /// </summary>
        /// <param name="value">
        /// The byte to write to the stream.
        /// </param>
        public override void WriteByte(
            byte value /* in */
            )
        {
            CheckDisposed();

            stream.WriteByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, that a write
        /// operation on the underlying stream will block before timing out.
        /// </summary>
        public override int WriteTimeout
        {
            get { CheckDisposed(); return stream.WriteTimeout; }
            set { CheckDisposed(); stream.WriteTimeout = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Net.Sockets.NetworkStream Members
        /// <summary>
        /// Gets a value indicating whether data is available to be read from the
        /// underlying network stream.  This property is always false when the
        /// underlying stream is not a network stream.
        /// </summary>
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
        /// <summary>
        /// Gets the number of bytes available to be read from the underlying
        /// stream.  For a network stream, this may poll the socket up to the
        /// configured timeout.  Returns zero when no count can be determined.
        /// </summary>
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
                                (int)PerformanceOps.GetMicrosecondsFromMilliseconds(
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
        /// <summary>
        /// This method attempts to obtain the total amount of time, in
        /// milliseconds, remaining to wait for data to become available.
        /// </summary>
        /// <param name="availableTimeout">
        /// Upon success, receives the total amount of time, in milliseconds,
        /// remaining to wait; upon failure, receives zero.
        /// </param>
        /// <returns>
        /// True if a timeout value is available; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Gets or sets the total amount of time, in milliseconds, remaining to
        /// wait for data to become available, if any.
        /// </summary>
        public virtual int? AvailableTimeout
        {
            get { CheckDisposed(); return availableTimeout; }
            set { CheckDisposed(); availableTimeout = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the amount of time, in milliseconds, to wait during the next
        /// poll, consuming that amount from the total time remaining to wait.
        /// Returns null when no timeout value is available.
        /// </summary>
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

        /// <summary>
        /// Gets the number of bytes that should be read from the underlying
        /// stream.  For a seekable stream, this is its length; otherwise, it is
        /// the number of bytes currently available.  A value of zero indicates
        /// that there is no exact number of bytes to read.
        /// </summary>
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

        /// <summary>
        /// This method reads from the underlying stream and appends the
        /// translated bytes to the supplied buffer, also collecting the positions
        /// of the line endings.  The flags for this stream are saved and restored
        /// around the operation.
        /// </summary>
        /// <param name="ignoreLineEnding">
        /// Non-zero to treat the data as the end of the stream and not defer
        /// trailing carriage-returns; zero to defer line endings that may span
        /// read-buffer boundaries.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero to recognize any end-of-line character as a line ending.
        /// </param>
        /// <param name="buffer">
        /// The buffer to which the translated bytes are appended; if null, a new
        /// buffer is allocated.
        /// </param>
        /// <param name="lineEndings">
        /// The list that receives the positions of the line endings within the
        /// buffer.
        /// </param>
        /// <returns>
        /// True if any data was read and translated into the buffer; otherwise,
        /// false.
        /// </returns>
        public virtual bool PopulateBuffer(
            bool ignoreLineEnding,    /* in */
            bool useAnyEndOfLineChar, /* in: TODO */
            ref ByteList buffer,      /* in, out */
            ref IntList lineEndings   /* in, out */
            )
        {
            CheckDisposed();

            StreamFlags flags = PrivateFlags;

            try
            {
                return PopulateBuffer(
                    ignoreLineEnding, useAnyEndOfLineChar,
                    ref buffer, ref lineEndings, ref flags);
            }
            finally
            {
                PrivateFlags = flags;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads from the underlying stream and appends the
        /// translated bytes to the supplied buffer, collecting the positions of
        /// the line endings and updating the supplied flags to account for line
        /// endings that span read-buffer boundaries.  When the buffer ends with a
        /// deferred carriage-return, the data is returned to the read buffer and
        /// no data is produced.
        /// </summary>
        /// <param name="ignoreLineEnding">
        /// Non-zero to treat the data as the end of the stream and not defer
        /// trailing carriage-returns; zero to defer line endings that may span
        /// read-buffer boundaries.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// Non-zero to recognize any end-of-line character as a line ending.
        /// </param>
        /// <param name="buffer">
        /// The buffer to which the translated bytes are appended; if null, a new
        /// buffer is allocated.
        /// </param>
        /// <param name="lineEndings">
        /// The list that receives the positions of the line endings within the
        /// buffer.
        /// </param>
        /// <param name="flags">
        /// The flags used and updated during the operation; upon return, they
        /// reflect any line-ending state that spans read-buffer boundaries.
        /// </param>
        /// <returns>
        /// True if any data was read and translated into the buffer; otherwise,
        /// false.
        /// </returns>
        protected virtual bool PopulateBuffer(
            bool ignoreLineEnding,    /* in */
            bool useAnyEndOfLineChar, /* in: TODO */
            ref ByteList buffer,      /* in, out */
            ref IntList lineEndings,  /* in, out */
            ref StreamFlags flags     /* in, out */
            )
        {
            if (stream == null)
                return false;

            int readBufferCount = 0;

            if (readBuffer != null)
                readBufferCount = readBuffer.GetCount();

            int readStreamCount = ReadCount; /* EXPENSIVE */

            if ((readStreamCount == 0) && (readBufferCount == 0))
                return false;

            byte[] readBytes = new byte[readStreamCount];
            int outCount;

            if (readStreamCount > 0)
            {
                outCount = stream.Read(
                    readBytes, 0, readStreamCount);

                if (FlagOps.HasFlags(
                        flags, StreamFlags.TraceReadLines, true))
                {
                    ChannelOps.TraceLineEndings("buffer", this,
                        readBytes, outCount, TracePriority.Highest);
                }
            }
            else
            {
                outCount = 0;
            }

            Array.Resize(ref readBytes, outCount);

            byte[] inBytes;

            if ((readBuffer != null) && (readBufferCount > 0))
            {
                /* NO RESULT */
                readBuffer.Append(readBytes);

                /* IGNORED */
                readBuffer.Take(out inBytes);

                if (inBytes == null)
                    return false;

                outCount = inBytes.Length;
            }
            else
            {
                inBytes = readBytes;
            }

            /* NO RESULT */
            ResetExtraLineEndingFlags(ref flags);

            StreamDirection direction = StreamDirection.None;

            if (ignoreLineEnding)
                direction |= StreamDirection.EndOfStream;

            if (useAnyEndOfLineChar)
                direction |= StreamDirection.AnyEndOfLine;

            //
            // BUGFIX: Reserve one extra byte: a carriage-return deferred at a
            //         previous read-buffer boundary (StreamFlags.NeedLineFeed) is
            //         emitted at the FRONT of this translation without consuming an
            //         input byte, so the output can be (outCount + 1) bytes.  The
            //         buffer is resized down to the actual translated length below.
            //         (COMPAT: Tcl; F47)
            //
            byte[] outBytes = new byte[outCount + 1];
            IntList localLineEndings = null;

            int translateCount = TranslateInputEndOfLine(
                inBytes, outBytes, direction, 0, outCount,
                ref localLineEndings, ref flags);

            //
            // BUGFIX: If the last (final?) character in the buffer
            //         is a carriage-return -AND- we care about the
            //         line-endings, this is a failure.
            //
            // BUGFIX: If the buffer ends with the first character
            //         of a carriage-return / line-feed pair (i.e.
            //         a carriage-return), then it is not ready to
            //         return yet.
            //
            if (!ignoreLineEnding && (FlagOps.HasFlags(
                    flags, StreamFlags.NeedLineFeed, true) ||
                FlagOps.HasFlags(
                    flags, StreamFlags.LastCarriageReturn, true)))
            {
                if (readBuffer != null)
                {
                    /* NO RESULT */
                    readBuffer.Append(inBytes);

                    //
                    // HACK: Since the result of end-of-line
                    //       translation is being discarded,
                    //       also reset the associated flags.
                    //
                    /* NO RESULT */
                    ResetLineEndingFlags(ref flags);
                }

                return false;
            }

            Array.Resize(ref outBytes, translateCount);

            if (buffer != null)
            {
                ListOps.Adjust(
                    localLineEndings, buffer.Count);

                buffer.AddRange(outBytes);
            }
            else
            {
                buffer = new ByteList(outBytes);
            }

            if (localLineEndings != null)
            {
                if (lineEndings != null)
                    lineEndings.AddRange(localLineEndings);
                else
                    lineEndings = localLineEndings;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is equal to the
        /// underlying stream, delegating to the underlying stream.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with the underlying stream.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the specified object is equal to the underlying stream;
        /// otherwise, false.
        /// </returns>
        public override bool Equals(
            object obj /* in */
            )
        {
            CheckDisposed();

            return stream.Equals(obj);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the hash code of the underlying stream,
        /// delegating to the underlying stream.
        /// </summary>
        /// <returns>
        /// The hash code of the underlying stream.
        /// </returns>
        public override int GetHashCode()
        {
            CheckDisposed();

            return stream.GetHashCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of the underlying stream,
        /// delegating to the underlying stream.
        /// </summary>
        /// <returns>
        /// A string representation of the underlying stream.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return stream.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.MarshalByRefObject Overrides
#if REMOTING
        /// <summary>
        /// This method creates an object that contains the information required
        /// to generate a proxy for the underlying stream, delegating to the
        /// underlying stream.
        /// </summary>
        /// <param name="requestedType">
        /// The type of the object that the new object reference will reference.
        /// </param>
        /// <returns>
        /// The information required to generate a proxy for the underlying
        /// stream.
        /// </returns>
        public override ObjRef CreateObjRef(
            Type requestedType /* in */
            )
        {
            CheckDisposed();

            return stream.CreateObjRef(requestedType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains a lifetime service object to control the lifetime
        /// policy for the underlying stream, delegating to the underlying stream.
        /// </summary>
        /// <returns>
        /// The object used to control the lifetime policy for the underlying
        /// stream.
        /// </returns>
        public override object InitializeLifetimeService()
        {
            CheckDisposed();

            return stream.InitializeLifetimeService();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this stream has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this stream has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this stream has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
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

        /// <summary>
        /// This method releases the resources held by this stream.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from <c>Dispose</c> (i.e.
        /// deterministically); zero if it is being called from the finalizer.
        /// When non-zero, managed resources are released.
        /// </param>
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
        /// <summary>
        /// Finalizes this stream, releasing any resources that were not released
        /// by an explicit call to <c>Dispose</c>.
        /// </summary>
        ~ChannelStream()
        {
            Dispose(false);
        }
        #endregion
    }
}
