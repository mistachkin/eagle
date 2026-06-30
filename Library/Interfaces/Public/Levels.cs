/*
 * Levels.cs --
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
    /// This interface is implemented by entities that track a nesting level,
    /// providing the current level along with the means to enter and exit a
    /// level.
    /// </summary>
    [ObjectId("d998fcd4-340d-4c0b-9b80-413001bb1cd7")]
    public interface ILevels
    {
        /// <summary>
        /// Gets the current nesting level.
        /// </summary>
        int Levels { get; }

        /// <summary>
        /// Enters a new nesting level.
        /// </summary>
        /// <returns>
        /// The nesting level after it has been incremented.
        /// </returns>
        int EnterLevel();

        /// <summary>
        /// Exits the current nesting level.
        /// </summary>
        /// <returns>
        /// The nesting level after it has been decremented.
        /// </returns>
        int ExitLevel();
    }
}
