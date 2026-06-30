/*
 * ChannelOps.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the static helper methods and shared constants used
    /// to create, configure, and read from the standard input, output, and
    /// error channels backing an interpreter host, including buffer sizing,
    /// line-ending detection, and stream selection.
    /// </summary>
    [ObjectId("3430c2bd-19ec-408b-bb26-a9fa3905807c")]
    internal static class ChannelOps
    {
        #region Private Constants
#if NET_40 && CONSOLE
        /// <summary>
        /// The runtime type of the internal console stream used by the .NET
        /// Framework, looked up by name; this may be null when the type cannot
        /// be located.
        /// </summary>
        private static readonly Type ConsoleStreamType = Type.GetType(
            "System.IO.__ConsoleStream");
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default size, in bytes, used when allocating a channel buffer.
        /// </summary>
        public static int DefaultBufferSize = 4096; // 4KB

        /// <summary>
        /// The maximum allowed size, in bytes, for a channel buffer.
        /// </summary>
        public static int MaximumBufferSize = 4194304; // 4MB

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: What is the use case for this field?
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the underlying stream is required to be available
        /// when fetching a standard channel stream; otherwise, a missing
        /// stream may be tolerated.
        /// </summary>
        public static bool StrictGetStream = false;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The byte value of the carriage-return line-ending character.
        /// </summary>
        public const byte CarriageReturn = (byte)Characters.CarriageReturn;

        /// <summary>
        /// The byte value of the line-feed line-ending character.
        /// </summary>
        public const byte LineFeed = (byte)Characters.LineFeed;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The byte value of the newline character emitted by <c>[puts]</c>.
        /// </summary>
        public const byte NewLine = (byte)Characters.NewLine; /* [puts] */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
#if NET_40 && CONSOLE
        /// <summary>
        /// This method returns the inner stream wrapped by a channel stream,
        /// when applicable.
        /// </summary>
        /// <param name="stream">
        /// The stream to unwrap; this is expected to be a channel stream.
        /// </param>
        /// <returns>
        /// The inner stream wrapped by the channel stream, or null if the
        /// supplied stream is not a channel stream.
        /// </returns>
        public static Stream GetInnerStream(
            Stream stream
            )
        {
            ChannelStream channelStream = stream as ChannelStream;

            if (channelStream == null)
                return null;

            return channelStream.GetStream();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the workaround for the internal
        /// console stream should be applied when reading from the supplied
        /// stream.
        /// </summary>
        /// <param name="stream">
        /// The stream to examine; this is expected to be a channel stream
        /// wrapping the standard input console stream.
        /// </param>
        /// <returns>
        /// True if the inner stream is the internal console stream and the
        /// workaround should be applied; otherwise, false.
        /// </returns>
        public static bool NeedConsoleStreamHack(
            Stream stream
            )
        {
            if (stream == null)
                return false;

            Stream innerStream = GetInnerStream(stream);

            if (innerStream == null)
                return false;

            if (ConsoleStreamType == null)
                return false;

            Type streamType = innerStream.GetType();

            if (streamType == null)
                return false;

            return Object.ReferenceEquals(streamType, ConsoleStreamType);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes line-ending statistics for the supplied buffer
        /// and emits them via the diagnostic trace facility.
        /// </summary>
        /// <param name="type">
        /// A descriptive label identifying the kind of stream being traced.
        /// </param>
        /// <param name="stream">
        /// The stream associated with the buffer, used only for identification
        /// in the trace output.
        /// </param>
        /// <param name="buffer">
        /// The buffer of bytes to scan for line-ending characters; if this is
        /// null, the method does nothing.
        /// </param>
        /// <param name="count">
        /// The number of bytes of interest within the buffer.
        /// </param>
        /// <param name="priority">
        /// The trace priority to use when emitting the diagnostic message.
        /// </param>
        public static void TraceLineEndings(
            string type,           /* in */
            Stream stream,         /* in */
            byte[] buffer,         /* in */
            int count,             /* in */
            TracePriority priority /* in */
            )
        {
            if (buffer == null)
                return;

            int[] statistics = { 0, 0, 0, 0, 0 };
            int length = buffer.Length;

            statistics[(int)BufferStats.Length] = length;

            byte previousByteValue = 0;

            for (int index = 0; index < length; index++)
            {
                byte currentByteValue = buffer[index];

                switch (currentByteValue)
                {
                    case CarriageReturn:
                        {
                            //
                            // NOTE: So far, we only "know" about this
                            //       carriage-return; therefore, treat
                            //       it as unpaired (raw).
                            //
                            statistics[(int)BufferStats.CrCount]++;
                            break;
                        }
                    case LineFeed:
                        {
                            //
                            // NOTE: Convert raw carriage-return into
                            //       carriage-return, line-feed pair;
                            //       otherwise, keep track of the raw
                            //       line-feed.
                            //
                            if (previousByteValue == CarriageReturn)
                            {
                                statistics[(int)BufferStats.CrCount]--;
                                statistics[(int)BufferStats.CrLfCount]++;
                            }
                            else
                            {
                                statistics[(int)BufferStats.LfCount]++;
                            }
                            break;
                        }
                }

                previousByteValue = currentByteValue;
            }

            //
            // NOTE: Calculate the total number of logical lines
            //       in the buffer.  This is simply the total of
            //       raw carriage-returns, raw line-feeds, and
            //       carriage-returns, line-feeds pairs.
            //
            foreach (BufferStats index in new BufferStats[] {
                    BufferStats.CrCount, BufferStats.LfCount,
                    BufferStats.CrLfCount
                })
            {
                statistics[(int)BufferStats.LineCount] +=
                    statistics[(int)index];
            }

            TraceOps.DebugTrace(String.Format(
                "TraceLineEndings: {0} stream = {1}, {2}",
                FormatOps.MaybeNull(type),
                RuntimeOps.GetHashCode(stream),
                FormatOps.TheBufferStats(statistics)),
                typeof(ChannelOps).Name, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method estimates the number of bytes that would be produced
        /// when writing the specified region of the buffer, accounting for the
        /// possible doubling of line-ending characters.
        /// </summary>
        /// <param name="buffer">
        /// The buffer of bytes to be examined.
        /// </param>
        /// <param name="offset">
        /// The zero-based index within the buffer at which to begin examining
        /// bytes.
        /// </param>
        /// <param name="count">
        /// The number of bytes within the buffer to examine.
        /// </param>
        /// <returns>
        /// The estimated number of bytes that would be produced on output.
        /// </returns>
        public static int EstimateOutputCount(
            byte[] buffer, /* in */
            int offset,    /* in */
            int count      /* in */
            )
        {
            int result = count;

            for (int index = offset; index < offset + count; index++)
            {
                char character = (char)buffer[index];

                if ((character == Characters.CarriageReturn) ||
                    (character == Characters.LineFeed))
                {
                    //
                    // NOTE: Every line terminator may double.
                    //
                    result += 2;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a single byte from the supplied stream, applying
        /// the internal console stream workaround when necessary.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from; if this is null, end-of-file is returned.
        /// </param>
        /// <returns>
        /// The byte read, as a non-negative integer, or the end-of-file
        /// sentinel when no more data is available.
        /// </returns>
        public static int ReadByte(
            Stream stream
            )
        {
#if NET_40 && CONSOLE
            if (NeedConsoleStreamHack(stream))
                return Console.Read();
#endif

            ///////////////////////////////////////////////////////////////////

            if (stream == null)
                return ChannelStream.EndOfFile;

            ///////////////////////////////////////////////////////////////////

            //
            // BUGBUG: This seems to intermittently produce garbage
            //         (i.e. for the first character) when reading
            //         from the console standard input channel when
            //         running on the .NET Framework 4.0 or higher.
            //         Initial research reveals that this may be
            //         caused by the WaitForAvailableConsoleInput
            //         method.
            //
            // HACK: Hopefully, the NeedConsoleStreamHack() handling
            //       above should work around this issue.
            //
            return stream.ReadByte();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the first valid end-of-line index from the
        /// supplied list of candidate indexes.
        /// </summary>
        /// <param name="lineEndings">
        /// The list of candidate end-of-line indexes to scan; if this is null
        /// or contains no valid index, the invalid-index sentinel is returned.
        /// </param>
        /// <returns>
        /// The first valid end-of-line index found, or the invalid-index
        /// sentinel when none is present.
        /// </returns>
        public static int FindEndOfLine(
            IntList lineEndings /* in */
            )
        {
            if (lineEndings != null)
            {
                int count = lineEndings.Count;

                if (count > 0)
                {
                    for (int index = 0; index < count; index++)
                    {
                        int eolIndex = lineEndings[index];

                        if (eolIndex != Index.Invalid)
                            return eolIndex;
                    }
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches a buffer for an end-of-line sequence, either
        /// matching any single end-of-line element or the entire end-of-line
        /// sequence in order.
        /// </summary>
        /// <typeparam name="T">
        /// The element type of the buffer and end-of-line sequence.
        /// </typeparam>
        /// <param name="buffer">
        /// The buffer to search; if this is null, the invalid-index sentinel
        /// is returned.
        /// </param>
        /// <param name="endOfLine">
        /// The end-of-line sequence (or set of characters) to search for; if
        /// this is null, the invalid-index sentinel is returned.
        /// </param>
        /// <param name="bufferStartIndex">
        /// The zero-based index within the buffer at which to begin searching;
        /// the invalid-index sentinel selects the start of the buffer.
        /// </param>
        /// <param name="bufferLength">
        /// The number of elements within the buffer to consider; the
        /// invalid-index sentinel selects the entire buffer.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// When true, the position of the first occurrence of any end-of-line
        /// element is returned; otherwise, the position of the first complete
        /// end-of-line sequence is returned.
        /// </param>
        /// <returns>
        /// The zero-based index of the matched end-of-line, or the
        /// invalid-index sentinel when no match is found.
        /// </returns>
        public static int FindEndOfLine<T>(
            T[] buffer,              /* in */
            IList<T> endOfLine,      /* in */
            int bufferStartIndex,    /* in */
            int bufferLength,        /* in */
            bool useAnyEndOfLineChar /* in */
            )
        {
            if ((buffer == null) || (endOfLine == null))
                return Index.Invalid;

            int localBufferLength = buffer.Length;

            if (bufferLength == Index.Invalid)
                bufferLength = localBufferLength;

            if ((bufferLength < 0) ||
                (bufferLength > localBufferLength))
            {
                return Index.Invalid;
            }

            if (bufferStartIndex == Index.Invalid)
                bufferStartIndex = 0;

            if ((bufferStartIndex < 0) ||
                (bufferStartIndex >= bufferLength))
            {
                return Index.Invalid;
            }

            int eolFoundIndex = Index.Invalid;
            int eolLength = endOfLine.Count;

            if (useAnyEndOfLineChar)
            {
                int eolIndex = 0;

                while (eolIndex < eolLength)
                {
                    int bufferIndex = Array.IndexOf(
                        buffer, endOfLine[eolIndex],
                        bufferStartIndex);

                    if (bufferIndex != Index.Invalid)
                    {
                        eolFoundIndex = bufferIndex;
                        break;
                    }
                    else
                    {
                        eolIndex++;
                    }
                }
            }
            else
            {
                int bufferIndex = bufferStartIndex;

                while (bufferIndex < bufferLength)
                {
                    int eolIndex = 0;

                    bufferIndex = Array.IndexOf(
                        buffer, endOfLine[eolIndex],
                        bufferIndex);

                    if (bufferIndex == Index.Invalid)
                        break;

                    int bufferSavedIndex = bufferIndex;
                    bool eolOk = true;

                    while ((bufferIndex < bufferLength) &&
                           (eolIndex < eolLength))
                    {
                        if (buffer[bufferIndex].Equals(
                                endOfLine[eolIndex]))
                        {
                            eolIndex++;
                            bufferIndex++;
                        }
                        else
                        {
                            eolOk = false;
                            break;
                        }
                    }

                    if (eolOk)
                    {
                        eolFoundIndex = bufferSavedIndex;
                        break;
                    }
                    else
                    {
                        bufferIndex++;
                    }
                }
            }

            return eolFoundIndex;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a trailing end-of-line from the buffer by
        /// adjusting the supplied buffer length, either trimming any trailing
        /// end-of-line elements or the entire trailing end-of-line sequence.
        /// </summary>
        /// <typeparam name="T">
        /// The element type of the buffer and end-of-line sequence.
        /// </typeparam>
        /// <param name="buffer">
        /// The buffer to trim; if this is null, no action is taken.
        /// </param>
        /// <param name="endOfLine">
        /// The end-of-line sequence (or set of characters) to remove; if this
        /// is null, no action is taken.
        /// </param>
        /// <param name="useAnyEndOfLineChar">
        /// When true, all trailing end-of-line elements are trimmed; otherwise,
        /// a single trailing end-of-line sequence is removed.
        /// </param>
        /// <param name="bufferLength">
        /// The number of valid elements in the buffer; upon return, this is
        /// reduced to exclude the removed trailing end-of-line.
        /// </param>
        public static void RemoveEndOfLine<T>(
            T[] buffer,               /* in */
            IList<T> endOfLine,       /* in */
            bool useAnyEndOfLineChar, /* in */
            ref int bufferLength      /* in, out */
            )
        {
            if ((buffer != null) &&
                (endOfLine != null) && (bufferLength > 0))
            {
                int bufferIndex; /* REUSED */

                if (useAnyEndOfLineChar)
                {
                    bufferIndex = bufferLength - 1;

                    while (bufferIndex >= 0)
                    {
                        if (endOfLine.Contains(
                                buffer[bufferIndex]))
                        {
                            bufferIndex--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    bufferLength = bufferIndex + 1;
                }
                else
                {
                    int eolLength = endOfLine.Count;

                    if (bufferLength >= eolLength)
                    {
                        int eolIndex = 0;
                        bool eolOk = true;

                        bufferIndex = bufferLength - eolLength;

                        while ((bufferIndex < bufferLength) &&
                               (eolIndex < eolLength))
                        {
                            if (buffer[bufferIndex].Equals(
                                    endOfLine[eolIndex]))
                            {
                                eolIndex++;
                                bufferIndex++;
                            }
                            else
                            {
                                eolOk = false;
                                break;
                            }
                        }

                        if (eolOk)
                            bufferLength -= eolLength;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the standard input, output, or error stream from
        /// the supplied stream host based on the requested channel type.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host from which the standard channel stream is obtained.
        /// </param>
        /// <param name="channelType">
        /// The channel type indicating which standard stream to select; only
        /// the standard-channel bits are honored.
        /// </param>
        /// <param name="useCurrent">
        /// When true, the host's current stream for the channel is used;
        /// otherwise, the host's default stream is used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about why the stream could not be
        /// obtained.
        /// </param>
        /// <returns>
        /// The selected stream, or null if no matching stream is available.
        /// </returns>
        public static Stream GetStream(
            IStreamHost streamHost,
            ChannelType channelType,
            bool useCurrent,
            ref Result error
            )
        {
            Stream stream = null;
            Result localError = null;

            try
            {
                channelType &= ChannelType.StandardChannels;

                switch (channelType)
                {
                    case ChannelType.Input:
                        {
                            stream = useCurrent ? streamHost.In :
                                streamHost.DefaultIn;

                            break;
                        }
                    case ChannelType.Output:
                        {
                            stream = useCurrent ? streamHost.Out :
                                streamHost.DefaultOut;

                            break;
                        }
                    case ChannelType.Error:
                        {
                            stream = useCurrent ? streamHost.Error :
                                streamHost.DefaultError;

                            break;
                        }
                    default:
                        {
                            localError = String.Format(
                                "unsupported stream channel type {0}",
                                channelType);

                            break;
                        }
                }
            }
            catch (Exception e)
            {
                localError = e;
            }

            if (stream != null)
                return stream;

            error = localError;
            return stream;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default stream flags appropriate for the
        /// current operating system platform.
        /// </summary>
        /// <returns>
        /// The stream flags to use; on non-Windows platforms, any end-of-line
        /// character is permitted to terminate an input line.
        /// </returns>
        public static StreamFlags GetStreamFlags()
        {
            //
            // HACK: We (may?) have no clean way to determine if
            //       the stdin stream has additional data to read;
            //       therefore, we need to allow any end-of-line
            //       character to terminate an input line -OR- we
            //       can run into problems wherever the end-of-line
            //       sequence differs.
            //
            return PlatformOps.IsWindowsOperatingSystem() ?
                StreamFlags.None : StreamFlags.UseAnyEndOfLineChar;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an input channel backed by the standard input
        /// stream of the supplied stream host.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host providing the input stream and input encoding; if
        /// this is null, the method fails.
        /// </param>
        /// <param name="channelType">
        /// Additional channel type flags to combine with the input channel
        /// type.
        /// </param>
        /// <param name="streamFlags">
        /// The stream flags to associate with the new channel.
        /// </param>
        /// <param name="useCurrent">
        /// When true, the host's current input stream is used; otherwise, the
        /// host's default input stream is used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about why the channel could not be
        /// created.
        /// </param>
        /// <returns>
        /// The newly created input channel, or null on failure.
        /// </returns>
        public static IChannel CreateInput(
            IStreamHost streamHost,
            ChannelType channelType,
            StreamFlags streamFlags,
            bool useCurrent,
            ref Result error
            )
        {
            if (streamHost == null)
            {
                error = "interpreter host not available";
                return null;
            }

            Stream stream = GetStream(
                streamHost, ChannelType.Input | channelType,
                useCurrent, ref error);

            if (stream == null)
                return null;

            Encoding encoding = streamHost.InputEncoding;

            if (encoding == null)
            {
                error = "invalid input encoding";
                return null;
            }

            return Channel.CreateInput(
                stream, channelType, streamFlags, encoding);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an output channel backed by the standard output
        /// stream of the supplied stream host.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host providing the output stream and output encoding; if
        /// this is null, the method fails.
        /// </param>
        /// <param name="channelType">
        /// Additional channel type flags to combine with the output channel
        /// type.
        /// </param>
        /// <param name="streamFlags">
        /// The stream flags to associate with the new channel.
        /// </param>
        /// <param name="useCurrent">
        /// When true, the host's current output stream is used; otherwise, the
        /// host's default output stream is used.
        /// </param>
        /// <param name="autoFlush">
        /// When true, the channel flushes its output automatically after each
        /// write.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about why the channel could not be
        /// created.
        /// </param>
        /// <returns>
        /// The newly created output channel, or null on failure.
        /// </returns>
        public static IChannel CreateOutput(
            IStreamHost streamHost,
            ChannelType channelType,
            StreamFlags streamFlags,
            bool useCurrent,
            bool autoFlush,
            ref Result error
            )
        {
            if (streamHost == null)
            {
                error = "interpreter host not available";
                return null;
            }

            Stream stream = GetStream(
                streamHost, ChannelType.Output | channelType,
                useCurrent, ref error);

            if (stream == null)
                return null;

            Encoding encoding = streamHost.OutputEncoding;

            if (encoding == null)
            {
                error = "invalid output encoding";
                return null;
            }

            return Channel.CreateOutput(
                stream, channelType, streamFlags, encoding, autoFlush);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an error channel backed by the standard error
        /// stream of the supplied stream host.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host providing the error stream and error encoding; if
        /// this is null, the method fails.
        /// </param>
        /// <param name="channelType">
        /// Additional channel type flags to combine with the error channel
        /// type.
        /// </param>
        /// <param name="streamFlags">
        /// The stream flags to associate with the new channel.
        /// </param>
        /// <param name="useCurrent">
        /// When true, the host's current error stream is used; otherwise, the
        /// host's default error stream is used.
        /// </param>
        /// <param name="autoFlush">
        /// When true, the channel flushes its output automatically after each
        /// write.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about why the channel could not be
        /// created.
        /// </param>
        /// <returns>
        /// The newly created error channel, or null on failure.
        /// </returns>
        public static IChannel CreateError(
            IStreamHost streamHost,
            ChannelType channelType,
            StreamFlags streamFlags,
            bool useCurrent,
            bool autoFlush,
            ref Result error
            )
        {
            if (streamHost == null)
            {
                error = "interpreter host not available";
                return null;
            }

            Stream stream = GetStream(
                streamHost, ChannelType.Error | channelType,
                useCurrent, ref error);

            if (stream == null)
                return null;

            Encoding encoding = streamHost.ErrorEncoding;

            if (encoding == null)
            {
                error = "invalid error encoding";
                return null;
            }

            return Channel.CreateError(
                stream, channelType, streamFlags, encoding, autoFlush);
        }
        #endregion
    }
}
