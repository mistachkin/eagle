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
    [ObjectId("cbfb281c-d7a7-43e2-9e24-6f4148ee1242")]
    internal interface IChannel : IIdentifier
    {
        IChannelContext Context { get; }

        bool HaveSavedContext();

        bool BeginContext(Stream stream, ref Result error);
        bool EndContext(bool close, ref Result error);

        bool BeginContext(
            Stream stream,
            ref IChannelContext savedContext,
            ref Result error
        );

        bool EndContext(
            bool close,
            ref IChannelContext savedContext,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        bool CanRead { get; }
        bool CanSeek { get; }
        bool CanWrite { get; }

        ///////////////////////////////////////////////////////////////////////

        bool HitEndOfStream { get; set; }
        bool EndOfStream { get; }

        bool AnyEndOfStream { get; }
        bool OneEndOfStream { get; }

        ///////////////////////////////////////////////////////////////////////

        long Length { get; }
        void SetLength(long value);

        long Position { get; }

        ///////////////////////////////////////////////////////////////////////

        void GetEndOfLineParameters(
            out CharList endOfLine,
            out bool useAnyEndOfLineChar,
            out bool keepEndOfLineChars
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode Read(ref ByteList list, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        ReturnCode Read(
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode Read(
            int count,
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode ReadBuffer(ref ByteList list, ref Result error);

        ///////////////////////////////////////////////////////////////////////

        ReturnCode ReadBuffer(
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode ReadBuffer(
            int count,
            CharList endOfLine,
            bool useAnyEndOfLineChar,
            bool keepEndOfLineChars,
            ref ByteList list,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        long Seek(long offset, SeekOrigin origin);

        ///////////////////////////////////////////////////////////////////////

        ChannelStream GetStream();
        Stream GetInnerStream();

        bool HasReader { get; }
        bool HasWriter { get; }
        bool HasBuffer { get; }

        BinaryReader GetBinaryReader();
        BinaryWriter GetBinaryWriter();
        StreamReader GetStreamReader();
        StreamWriter GetStreamWriter();

        ///////////////////////////////////////////////////////////////////////

        int DiscardBuffered();
        void ResetBuffered();
        void TakeBuffered(out ByteList buffer, out IntList lineEndings);
        bool GiveBuffered(ref ByteList buffer, ref IntList lineEndings);
        void NewBuffered();

        ///////////////////////////////////////////////////////////////////////

        bool NullEncoding { get; }

        Encoding GetEncoding();
        void SetEncoding(Encoding encoding);

        ///////////////////////////////////////////////////////////////////////

        StreamTranslation GetInputTranslation();
        StreamTranslation GetOutputTranslation();

        StreamTranslationList GetTranslation();
        void SetTranslation(StreamTranslationList translation);

        CharList GetInputEndOfLine();
        CharList GetOutputEndOfLine();

        void RemoveTrailingEndOfLine(ByteList buffer, CharList endOfLine);

        ///////////////////////////////////////////////////////////////////////

        bool GetBlockingMode();
        void SetBlockingMode(bool blockingMode);

        ///////////////////////////////////////////////////////////////////////

        void CheckAppend();
        bool CheckAutoFlush();

        ///////////////////////////////////////////////////////////////////////

        bool Flush();
        void Close();

        ///////////////////////////////////////////////////////////////////////

        bool IsConsoleStream { get; }

        ///////////////////////////////////////////////////////////////////////

        object Socket { get; }
        bool IsNetworkStream { get; }
        bool Connected { get; }
        bool DataAvailable { get; }

        ///////////////////////////////////////////////////////////////////////

        bool IsVirtualOutput { get; }
        StringBuilder VirtualOutput { get; set; }

        bool AppendVirtualOutput(char value);
        bool AppendVirtualOutput(string value);
        bool AppendVirtualOutput(byte[] value);
    }
}
