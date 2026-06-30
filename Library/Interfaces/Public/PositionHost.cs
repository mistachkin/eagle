/*
 * PositionHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by interactive hosts that support
    /// querying and changing the position of the cursor.  It extends
    /// <see cref="IInteractiveHost" /> with methods to get, set, and reset
    /// both the current position and the default position.
    /// </summary>
    [ObjectId("30810314-8094-406d-a023-21c785b4681e")]
    public interface IPositionHost : IInteractiveHost
    {
        /// <summary>
        /// Resets the current position to its default value.
        /// </summary>
        /// <returns>
        /// True if the position was reset; otherwise, false.
        /// </returns>
        bool ResetPosition();

        /// <summary>
        /// Gets the current position.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the zero-based column (horizontal)
        /// coordinate of the current position.
        /// </param>
        /// <param name="top">
        /// Upon success, receives the zero-based row (vertical) coordinate
        /// of the current position.
        /// </param>
        /// <returns>
        /// True if the position was obtained; otherwise, false.
        /// </returns>
        bool GetPosition(ref int left, ref int top);
        /// <summary>
        /// Sets the current position.
        /// </summary>
        /// <param name="left">
        /// The zero-based column (horizontal) coordinate to set as the
        /// current position.
        /// </param>
        /// <param name="top">
        /// The zero-based row (vertical) coordinate to set as the current
        /// position.
        /// </param>
        /// <returns>
        /// True if the position was set; otherwise, false.
        /// </returns>
        bool SetPosition(int left, int top);

        /// <summary>
        /// Gets the default position.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the zero-based column (horizontal)
        /// coordinate of the default position.
        /// </param>
        /// <param name="top">
        /// Upon success, receives the zero-based row (vertical) coordinate
        /// of the default position.
        /// </param>
        /// <returns>
        /// True if the default position was obtained; otherwise, false.
        /// </returns>
        bool GetDefaultPosition(ref int left, ref int top);
        /// <summary>
        /// Sets the default position.
        /// </summary>
        /// <param name="left">
        /// The zero-based column (horizontal) coordinate to set as the
        /// default position.
        /// </param>
        /// <param name="top">
        /// The zero-based row (vertical) coordinate to set as the default
        /// position.
        /// </param>
        /// <returns>
        /// True if the default position was set; otherwise, false.
        /// </returns>
        bool SetDefaultPosition(int left, int top);
    }
}
