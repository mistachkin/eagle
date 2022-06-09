/*
 * HistoryManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("4304d8a6-41f3-4143-bd4e-7fbc82a56642")]
    public interface IHistoryManager
    {
        ///////////////////////////////////////////////////////////////////////
        // EXECUTION HISTORY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        bool History { get; set; }

        int HistoryLimit { get; set; }

        bool HasHistory(ref Result error);

        IHistoryData HistoryLoadData { get; set; }
        IHistoryData HistorySaveData { get; set; }

        IHistoryFilter HistoryEngineFilter { get; set; }
        IHistoryFilter HistoryInfoFilter { get; set; }
        IHistoryFilter HistoryLoadFilter { get; set; }
        IHistoryFilter HistorySaveFilter { get; set; }

        string HistoryFileName { get; set; }

        ReturnCode ClearHistory(
            IHistoryFilter historyFilter,
            ref Result error
            );

        ReturnCode AddHistory(
            ArgumentList arguments,
            IHistoryData historyData,
            IHistoryFilter historyFilter,
            ref Result error
            );

        ReturnCode LoadHistory(
            Encoding encoding,
            string fileName,
            IHistoryData historyData,
            IHistoryFilter historyFilter,
            bool strict,
            ref Result error
            );

        ReturnCode SaveHistory(
            Encoding encoding,
            string fileName,
            IHistoryData historyData,
            IHistoryFilter historyFilter,
            bool strict,
            ref Result error
            );
    }
}
