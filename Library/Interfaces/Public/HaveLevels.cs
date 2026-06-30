/*
 * HaveLevels.cs --
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
    /// This interface is implemented by entities that track a nesting or
    /// recursion level count, allowing that level to be queried as well as
    /// entered and exited.
    /// </summary>
    [ObjectId("49b275c9-3098-44fb-bd91-a947f6b2b8e2")]
    public interface IHaveLevels
    {
        /// <summary>
        /// Gets the current nesting level for this entity.
        /// </summary>
        long Levels { get; }

        /// <summary>
        /// Increments the nesting level for this entity, marking entry into a
        /// new level.
        /// </summary>
        /// <returns>
        /// The nesting level after it has been incremented.
        /// </returns>
        long EnterLevel();

        /// <summary>
        /// Decrements the nesting level for this entity, marking exit from the
        /// current level.
        /// </summary>
        /// <returns>
        /// The nesting level after it has been decremented.
        /// </returns>
        long ExitLevel();
    }
}
