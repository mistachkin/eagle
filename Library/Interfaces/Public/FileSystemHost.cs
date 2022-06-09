/*
 * FileSystemHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("052aa850-a9aa-4034-9709-2f43871e99a7")]
    public interface IFileSystemHost : IInteractiveHost
    {
        HostStreamFlags StreamFlags { get; set; }

        ReturnCode GetStream(
            string path, FileMode mode, FileAccess access,
            FileShare share, int bufferSize, FileOptions options,
            ref HostStreamFlags hostStreamFlags, ref string fullPath,
            ref Stream stream, ref Result error);

        ReturnCode GetData(
            string name, DataFlags dataFlags, ref ScriptFlags scriptFlags,
            ref IClientData clientData, ref Result result);
    }
}
