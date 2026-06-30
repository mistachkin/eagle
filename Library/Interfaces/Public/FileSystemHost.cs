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
    /// <summary>
    /// This interface is implemented by the portion of a host that provides
    /// file-system and data access for an Eagle interpreter.  It extends the
    /// interactive host contract (<see cref="IInteractiveHost" />) with the
    /// ability to open streams and to fetch named data (for example, scripts)
    /// on behalf of the engine.
    /// </summary>
    [ObjectId("052aa850-a9aa-4034-9709-2f43871e99a7")]
    public interface IFileSystemHost : IInteractiveHost
    {
        /// <summary>
        /// Gets or sets the flags that control how this host opens and manages
        /// streams.
        /// </summary>
        HostStreamFlags StreamFlags { get; set; }

        /// <summary>
        /// This method opens a stream for the specified path on behalf of the
        /// engine.
        /// </summary>
        /// <param name="path">
        /// The path of the file or resource to open.
        /// </param>
        /// <param name="mode">
        /// The mode used when opening the stream (for example, create or
        /// open).
        /// </param>
        /// <param name="access">
        /// The access requested for the stream (for example, read or write).
        /// </param>
        /// <param name="share">
        /// The sharing mode permitted for the stream.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer to use for the stream.
        /// </param>
        /// <param name="options">
        /// The additional options used when opening the stream.
        /// </param>
        /// <param name="hostStreamFlags">
        /// On input, the flags that influence how the stream is opened; on
        /// output, the flags describing the stream that was opened.
        /// </param>
        /// <param name="fullPath">
        /// Upon return, this contains the fully qualified path of the stream
        /// that was opened.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode GetStream(
            string path, FileMode mode, FileAccess access,
            FileShare share, int bufferSize, FileOptions options,
            ref HostStreamFlags hostStreamFlags, ref string fullPath,
            ref Stream stream, ref Result error);

        /// <summary>
        /// This method fetches the named data (for example, a script) on
        /// behalf of the engine.
        /// </summary>
        /// <param name="name">
        /// The name of the data to fetch.
        /// </param>
        /// <param name="dataFlags">
        /// The flags that control how the data is located and fetched.
        /// </param>
        /// <param name="scriptFlags">
        /// On input, the flags that influence how the data is fetched; on
        /// output, the flags describing the data that was fetched.
        /// </param>
        /// <param name="clientData">
        /// On input, the extra data supplied for the request, if any; on
        /// output, the extra data associated with the fetched data, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the fetched data.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode GetData(
            string name, DataFlags dataFlags, ref ScriptFlags scriptFlags,
            ref IClientData clientData, ref Result result);
    }
}
