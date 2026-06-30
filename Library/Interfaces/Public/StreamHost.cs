/*
 * StreamHost.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by interactive hosts that expose their
    /// underlying input, output, and error streams along with the encodings
    /// used for them.  It extends <see cref="IInteractiveHost" /> with the
    /// ability to query the default streams, get or set the active streams
    /// and encodings, reset them, and detect output redirection.
    /// </summary>
    [ObjectId("9180bb9e-b41a-4d17-be3a-4e8f75313020")]
    public interface IStreamHost : IInteractiveHost
    {
        /// <summary>
        /// Gets the default input stream for this host.
        /// </summary>
        Stream DefaultIn { get; }
        /// <summary>
        /// Gets the default output stream for this host.
        /// </summary>
        Stream DefaultOut { get; }
        /// <summary>
        /// Gets the default error stream for this host.
        /// </summary>
        Stream DefaultError { get; }

        /// <summary>
        /// Gets or sets the active input stream for this host.
        /// </summary>
        Stream In { get; set; }
        /// <summary>
        /// Gets or sets the active output stream for this host.
        /// </summary>
        Stream Out { get; set; }
        /// <summary>
        /// Gets or sets the active error stream for this host.
        /// </summary>
        Stream Error { get; set; }

        /// <summary>
        /// Gets or sets the encoding used for the input stream.
        /// </summary>
        Encoding InputEncoding { get; set; }
        /// <summary>
        /// Gets or sets the encoding used for the output stream.
        /// </summary>
        Encoding OutputEncoding { get; set; }
        /// <summary>
        /// Gets or sets the encoding used for the error stream.
        /// </summary>
        Encoding ErrorEncoding { get; set; }

        /// <summary>
        /// Resets the active input stream to its default.
        /// </summary>
        /// <returns>
        /// True if the input stream was reset; otherwise, false.
        /// </returns>
        bool ResetIn();
        /// <summary>
        /// Resets the active output stream to its default.
        /// </summary>
        /// <returns>
        /// True if the output stream was reset; otherwise, false.
        /// </returns>
        bool ResetOut();
        /// <summary>
        /// Resets the active error stream to its default.
        /// </summary>
        /// <returns>
        /// True if the error stream was reset; otherwise, false.
        /// </returns>
        bool ResetError();

        /// <summary>
        /// Determines whether the output stream for this host has been
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the output stream has been redirected; otherwise, false.
        /// </returns>
        bool IsOutputRedirected();
        /// <summary>
        /// Determines whether the error stream for this host has been
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the error stream has been redirected; otherwise, false.
        /// </returns>
        bool IsErrorRedirected();

        /// <summary>
        /// Sets up the input, output, and error channels for this host.
        /// </summary>
        /// <returns>
        /// True if the channels were set up successfully; otherwise, false.
        /// </returns>
        bool SetupChannels();
    }
}
