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
    /// <summary>
    /// This class provides helper methods that support command history,
    /// principally the matching logic that decides whether a given history
    /// entry passes a history filter based on its call stack level and history
    /// flags.
    /// </summary>
    [ObjectId("7055e2b5-abcb-4c8e-9b48-fb294e75c99e")]
    internal static class HistoryOps
    {
        #region Private Constants
        /// <summary>
        /// The default history filter used when writing the interpreter header
        /// information; a null value indicates that no filtering is applied.
        /// </summary>
        //
        // NOTE: Used by "_Hosts.Default.WriteHeader".
        //
        internal static readonly IHistoryFilter DefaultInfoFilter = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command History Support Methods
        /// <summary>
        /// This method determines whether the command history entry represented
        /// by the specified client data matches the specified history filter,
        /// comparing the entry's call stack level and history flags against the
        /// filter's level range and required and excluded flags.
        /// </summary>
        /// <param name="clientData">
        /// The client data for the history entry to test; this is expected to be
        /// a <see cref="HistoryClientData" /> instance.  This parameter may be
        /// null.
        /// </param>
        /// <param name="historyFilter">
        /// The history filter to apply.  When null, any
        /// <see cref="HistoryClientData" /> entry is considered a match.
        /// </param>
        /// <returns>
        /// True if the history entry matches the filter; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the command history entry represented
        /// by the specified history data matches the specified history filter,
        /// extracting the entry's call stack level and history flags and
        /// delegating to the level-and-flags matching logic.
        /// </summary>
        /// <param name="historyData">
        /// The history data for the entry to test.  When null, the invalid
        /// level and no flags are used.
        /// </param>
        /// <param name="historyFilter">
        /// The history filter to apply.  When null, the entry is considered a
        /// match.
        /// </param>
        /// <returns>
        /// True if the history entry matches the filter; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a command history entry with the
        /// specified call stack level and history flags matches the specified
        /// history filter, comparing the level against the filter's level range
        /// and the flags against the filter's required and excluded flags.
        /// </summary>
        /// <param name="levels">
        /// The call stack level of the history entry to test.
        /// </param>
        /// <param name="flags">
        /// The history flags of the history entry to test.
        /// </param>
        /// <param name="historyFilter">
        /// The history filter to apply.  When null, the entry is considered a
        /// match.
        /// </param>
        /// <returns>
        /// True if the history entry matches the filter; otherwise, false.
        /// </returns>
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
