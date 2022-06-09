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
    [ObjectId("12ef548e-ce36-495a-aefb-821b1c41c470")]
    public interface IHistoryData
    {
        ///////////////////////////////////////////////////////////////////////
        // EXECUTION HISTORY DATA
        ///////////////////////////////////////////////////////////////////////

        int Levels { get; set; }
        HistoryFlags Flags { get; set; }
    }
}
