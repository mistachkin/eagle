/*
 * ReadHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if CONSOLE
using System;
#endif

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by interactive hosts that support reading
    /// input.  It extends <see cref="IInteractiveHost" /> with the ability to
    /// read a single character and to read a single key press from the host.
    /// </summary>
    [ObjectId("fc97d189-df06-4f49-800d-cca84ae0bf32")]
    public interface IReadHost : IInteractiveHost
    {
        /// <summary>
        /// Reads a single character from the host.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the character that was read, or a negative
        /// value if the end of the input was reached.
        /// </param>
        /// <returns>
        /// True if the character was read; otherwise, false.
        /// </returns>
        bool Read(ref int value); /* RECOMMENDED */
        /// <summary>
        /// Reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed by
        /// the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
        bool ReadKey(bool intercept, ref IClientData value); /* RECOMMENDED */

#if CONSOLE
        /// <summary>
        /// Reads a single key press from the host.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key press so that it is not displayed by
        /// the host.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the data describing the key that was pressed.
        /// </param>
        /// <returns>
        /// True if the key press was read; otherwise, false.
        /// </returns>
        [Obsolete()]
        bool ReadKey(bool intercept, ref ConsoleKeyInfo value); /* DEPRECATED */
#endif
    }
}
