/*
 * HistoryFilter.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a set of criteria used to select which command
    /// history entries are visible to an operation, based on call-stack levels
    /// and history flags.  It implements <see cref="IHistoryFilter" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a785bf4d-3017-4d2b-b148-564aaa63e904")]
    public sealed class HistoryFilter : IHistoryFilter
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty history filter with all criteria left at their
        /// default values.
        /// </summary>
        public HistoryFilter()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a history filter from the fully specified set of
        /// level-range and flag criteria.
        /// </summary>
        /// <param name="startLevel">
        /// The lowest call-stack level included by this filter.
        /// </param>
        /// <param name="stopLevel">
        /// The highest call-stack level included by this filter.
        /// </param>
        /// <param name="hasFlags">
        /// The history flags that a history entry must have in order to be
        /// included by this filter.
        /// </param>
        /// <param name="notHasFlags">
        /// The history flags that a history entry must not have in order to be
        /// included by this filter.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero if a history entry must have all of the flags specified by
        /// <paramref name="hasFlags" /> (instead of any of them).
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero if a history entry must lack all of the flags specified by
        /// <paramref name="notHasFlags" /> (instead of any of them).
        /// </param>
        public HistoryFilter(
            int startLevel,
            int stopLevel,
            HistoryFlags hasFlags,
            HistoryFlags notHasFlags,
            bool hasAll,
            bool notHasAll
            )
            : this()
        {
            this.startLevel = startLevel;
            this.stopLevel = stopLevel;
            this.hasFlags = hasFlags;
            this.notHasFlags = notHasFlags;
            this.hasAll = hasAll;
            this.notHasAll = notHasAll;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHistoryFilter Members
        /// <summary>
        /// The lowest call-stack level included by this filter.
        /// </summary>
        private int startLevel;
        /// <summary>
        /// Gets or sets the lowest call-stack level included by this filter.
        /// </summary>
        public int StartLevel
        {
            get { return startLevel; }
            set { startLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The highest call-stack level included by this filter.
        /// </summary>
        private int stopLevel;
        /// <summary>
        /// Gets or sets the highest call-stack level included by this filter.
        /// </summary>
        public int StopLevel
        {
            get { return stopLevel; }
            set { stopLevel = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The history flags that a history entry must have in order to be
        /// included by this filter.
        /// </summary>
        private HistoryFlags hasFlags;
        /// <summary>
        /// Gets or sets the history flags that a history entry must have in
        /// order to be included by this filter.
        /// </summary>
        public HistoryFlags HasFlags
        {
            get { return hasFlags; }
            set { hasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The history flags that a history entry must not have in order to be
        /// included by this filter.
        /// </summary>
        private HistoryFlags notHasFlags;
        /// <summary>
        /// Gets or sets the history flags that a history entry must not have in
        /// order to be included by this filter.
        /// </summary>
        public HistoryFlags NotHasFlags
        {
            get { return notHasFlags; }
            set { notHasFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if a history entry must have all of the flags specified by
        /// <see cref="HasFlags" /> (instead of any of them).
        /// </summary>
        private bool hasAll;
        /// <summary>
        /// Gets or sets a value indicating whether a history entry must have
        /// all of the flags specified by <see cref="HasFlags" /> (instead of
        /// any of them).
        /// </summary>
        public bool HasAll
        {
            get { return hasAll; }
            set { hasAll = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if a history entry must lack all of the flags specified by
        /// <see cref="NotHasFlags" /> (instead of any of them).
        /// </summary>
        private bool notHasAll;
        /// <summary>
        /// Gets or sets a value indicating whether a history entry must lack
        /// all of the flags specified by <see cref="NotHasFlags" /> (instead
        /// of any of them).
        /// </summary>
        public bool NotHasAll
        {
            get { return notHasAll; }
            set { notHasAll = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this history filter,
        /// suitable for use in diagnostic output.
        /// </summary>
        /// <returns>
        /// A string containing the name and value of each filter criterion.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(
                "startLevel", startLevel, "stopLevel", stopLevel, "stopLevel",
                "hasFlags", hasFlags, "notHasFlags", notHasFlags, "hasAll",
                hasAll, "notHasAll", notHasAll);
        }
        #endregion
    }
}
