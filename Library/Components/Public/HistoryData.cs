/*
 * HistoryData.cs --
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
    /// This class holds the configuration controlling command history capture
    /// for an interpreter, namely the number of history levels to retain and
    /// the flags that govern which commands are recorded.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("01ea058e-6e10-41db-b89b-c61f8fe7988e")]
    public sealed class HistoryData : IHistoryData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a history data instance with default settings.
        /// </summary>
        public HistoryData()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a history data instance with the specified number of
        /// history levels and flags.
        /// </summary>
        /// <param name="levels">
        /// The number of history levels to retain.
        /// </param>
        /// <param name="flags">
        /// The flags controlling which commands are recorded in the history.
        /// </param>
        public HistoryData(
            int levels,
            HistoryFlags flags
            )
            : this()
        {
            this.levels = levels;
            this.flags = flags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHistoryData Members
        /// <summary>
        /// Stores the number of history levels to retain.
        /// </summary>
        private int levels;
        /// <summary>
        /// Gets or sets the number of history levels to retain.
        /// </summary>
        public int Levels
        {
            get { return levels; }
            set { levels = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling which commands are recorded in the
        /// history.
        /// </summary>
        private HistoryFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling which commands are recorded in
        /// the history.
        /// </summary>
        public HistoryFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of this history data,
        /// listing its levels and flags.
        /// </summary>
        /// <returns>
        /// A string containing the levels and flags of this history data.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList("levels", levels, "flags", flags);
        }
        #endregion
    }
}
