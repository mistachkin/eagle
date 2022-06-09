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
    [ObjectId("6ac8bae1-d952-4df3-862e-6242f1695ed1")]
    internal interface IChannelContext : IThreadContext, IDisposable
    {
        ChannelStream ChannelStream { get; }

        BinaryReader BinaryReader { get; }
        BinaryWriter BinaryWriter { get; }
        StreamReader StreamReader { get; }
        StreamWriter StreamWriter { get; }

        bool HasReader { get; }
        bool HasWriter { get; }
        bool HasBuffer { get; }

        BinaryReader GetBinaryReader(Encoding encoding);
        BinaryWriter GetBinaryWriter(Encoding encoding);
        StreamReader GetStreamReader(Encoding encoding);
        StreamWriter GetStreamWriter(Encoding encoding);

        ByteList TakeBuffer();
        bool GiveBuffer(ref ByteList buffer);
        void NewBuffer();

        ChannelStream PartialCloneChannelStream(Stream stream);

        bool Flush();

        void CloseReadersAndWriters(bool preventClose);
        void Close();
    }
}
