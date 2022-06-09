/*
 * HistoryOps.cs --
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
using Eagle._Constants;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("7055e2b5-abcb-4c8e-9b48-fb294e75c99e")]
    internal static class HistoryOps
    {
        #region Private Constants
        //
        // NOTE: Used by "_Hosts.Default.WriteHeader".
        //
        internal static readonly IHistoryFilter DefaultInfoFilter = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command History Support Methods
        public static bool MatchData(
            IClientData clientData,
            IHistoryFilter historyFilter
            )
        {
            HistoryClientData historyClientData = clientData as HistoryClientData;

            if (historyClientData != null)
            {
                if (historyFilter != null)
                {
                    if ((historyFilter.StartLevel != Level.Invalid) &&
                        (historyClientData.Levels < historyFilter.StartLevel))
                    {
                        return false;
                    }

                    if ((historyFilter.StopLevel != Level.Invalid) &&
                        (historyClientData.Levels > historyFilter.StopLevel))
                    {
                        return false;
                    }

                    if ((historyFilter.HasFlags != HistoryFlags.None) &&
                        !FlagOps.HasFlags(historyClientData.Flags,
                            historyFilter.HasFlags, historyFilter.HasAll))
                    {
                        return false;
                    }

                    if ((historyFilter.NotHasFlags != HistoryFlags.None) &&
                        FlagOps.HasFlags(historyClientData.Flags,
                            historyFilter.NotHasFlags, historyFilter.NotHasAll))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool MatchData(
            IHistoryData historyData,
            IHistoryFilter historyFilter
            )
        {
            int levels;
            HistoryFlags flags;

            if (historyData != null)
            {
                levels = historyData.Levels;
                flags = historyData.Flags;
            }
            else
            {
                levels = Level.Invalid;
                flags = HistoryFlags.None;
            }

            return MatchData(levels, flags, historyFilter);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static bool MatchData(
            int levels,
            HistoryFlags flags,
            IHistoryFilter historyFilter
            )
        {
            if (historyFilter != null)
            {
                if ((historyFilter.StartLevel != Level.Invalid) &&
                    (levels < historyFilter.StartLevel))
                {
                    return false;
                }

                if ((historyFilter.StopLevel != Level.Invalid) &&
                    (levels > historyFilter.StopLevel))
                {
                    return false;
                }

                if ((historyFilter.HasFlags != HistoryFlags.None) &&
                    !FlagOps.HasFlags(flags, historyFilter.HasFlags,
                        historyFilter.HasAll))
                {
                    return false;
                }

                if ((historyFilter.NotHasFlags != HistoryFlags.None) &&
                    FlagOps.HasFlags(flags, historyFilter.NotHasFlags,
                        historyFilter.NotHasAll))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
