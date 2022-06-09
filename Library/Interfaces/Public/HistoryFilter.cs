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
    [ObjectId("8cf5be5e-dae4-4786-b498-c08778e9df48")]
    public interface IHistoryFilter
    {
        ///////////////////////////////////////////////////////////////////////
        // EXECUTION HISTORY FILTER
        ///////////////////////////////////////////////////////////////////////

        int StartLevel { get; set; }
        int StopLevel { get; set; }

        HistoryFlags HasFlags { get; set; }
        HistoryFlags NotHasFlags { get; set; }

        bool HasAll { get; set; }
        bool NotHasAll { get; set; }
    }
}
