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
    [ObjectId("3430c2bd-19ec-408b-bb26-a9fa3905807c")]
    internal static class ChannelOps
    {
        #region Private Constants
#if NET_40 && CONSOLE
        private static readonly Type ConsoleStreamType = Type.GetType(
            "System.IO.__ConsoleStream");
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        //
        // HACK: These are purposely not read-only.
        //
        public static int DefaultBufferSize = 4096; // 4KB
        public static int MaximumBufferSize = 4194304; // 4MB

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: What is the use case for this field?
        //
        // HACK: These are purposely not read-only.
        //
        public static bool StrictGetStream = false;

        ///////////////////////////////////////////////////////////////////////

        public const byte CarriageReturn = (byte)Characters.CarriageReturn;
        public const byte LineFeed = (byte)Characters.LineFeed;

        ///////////////////////////////////////////////////////////////////////

        public const byte NewLine = (byte)Characters.NewLine; /* [puts] */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
#if NET_40 && CONSOLE
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
