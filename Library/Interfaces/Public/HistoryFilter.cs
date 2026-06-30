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

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the criteria used to select which command
    /// execution history entries are included by a history operation, based on
    /// a range of call frame levels and on the flags associated with each
    /// entry.
    /// </summary>
    [ObjectId("8cf5be5e-dae4-4786-b498-c08778e9df48")]
    public interface IHistoryFilter
    {
        ///////////////////////////////////////////////////////////////////////
        // EXECUTION HISTORY FILTER
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the lowest call frame level (inclusive) that an entry
        /// must have in order to match this filter.
        /// </summary>
        int StartLevel { get; set; }
        /// <summary>
        /// Gets or sets the highest call frame level (inclusive) that an entry
        /// must have in order to match this filter.
        /// </summary>
        int StopLevel { get; set; }

        /// <summary>
        /// Gets or sets the flags that an entry must have in order to match
        /// this filter.
        /// </summary>
        HistoryFlags HasFlags { get; set; }
        /// <summary>
        /// Gets or sets the flags that an entry must not have in order to match
        /// this filter.
        /// </summary>
        HistoryFlags NotHasFlags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an entry must have all of
        /// the flags in <see cref="HasFlags" /> (true) or only some of them
        /// (false) in order to match this filter.
        /// </summary>
        bool HasAll { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether an entry must lack all of
        /// the flags in <see cref="NotHasFlags" /> (true) or only some of them
        /// (false) in order to match this filter.
        /// </summary>
        bool NotHasAll { get; set; }
    }
}
