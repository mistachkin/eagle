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

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the data used to control how command execution
    /// history is recorded for an interpreter, including the number of call
    /// frame levels to capture and the flags that govern history processing.
    /// </summary>
    [ObjectId("12ef548e-ce36-495a-aefb-821b1c41c470")]
    public interface IHistoryData
    {
        ///////////////////////////////////////////////////////////////////////
        // EXECUTION HISTORY DATA
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the number of call frame levels of execution history
        /// to record.
        /// </summary>
        int Levels { get; set; }
        /// <summary>
        /// Gets or sets the flags that control how execution history is
        /// recorded and processed.
        /// </summary>
        HistoryFlags Flags { get; set; }
    }
}
