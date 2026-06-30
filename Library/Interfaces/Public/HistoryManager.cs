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
    /// <summary>
    /// This interface defines the contract for managing command execution
    /// history for an interpreter.  It exposes the settings that control
    /// whether and how history is recorded, the data and filters used when
    /// recording, querying, loading, and saving history, and the methods used
    /// to add, clear, load, and save history entries.
    /// </summary>
    [ObjectId("4304d8a6-41f3-4143-bd4e-7fbc82a56642")]
    public interface IHistoryManager
    {
        ///////////////////////////////////////////////////////////////////////
        // EXECUTION HISTORY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether command execution history is
        /// being recorded.
        /// </summary>
        bool History { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of history entries to retain.
        /// </summary>
        int HistoryLimit { get; set; }

        /// <summary>
        /// This method determines whether any command execution history is
        /// currently available.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if history is available; otherwise, false.
        /// </returns>
        bool HasHistory(ref Result error);

        /// <summary>
        /// Gets or sets the data used to control how history is processed when
        /// it is loaded.
        /// </summary>
        IHistoryData HistoryLoadData { get; set; }
        /// <summary>
        /// Gets or sets the data used to control how history is processed when
        /// it is saved.
        /// </summary>
        IHistoryData HistorySaveData { get; set; }

        /// <summary>
        /// Gets or sets the filter used to select which entries are recorded by
        /// the engine.
        /// </summary>
        IHistoryFilter HistoryEngineFilter { get; set; }
        /// <summary>
        /// Gets or sets the filter used to select which entries are reported
        /// when history information is queried.
        /// </summary>
        IHistoryFilter HistoryInfoFilter { get; set; }
        /// <summary>
        /// Gets or sets the filter used to select which entries are processed
        /// when history is loaded.
        /// </summary>
        IHistoryFilter HistoryLoadFilter { get; set; }
        /// <summary>
        /// Gets or sets the filter used to select which entries are processed
        /// when history is saved.
        /// </summary>
        IHistoryFilter HistorySaveFilter { get; set; }

        /// <summary>
        /// Gets or sets the name of the file used to load and save history
        /// entries.
        /// </summary>
        string HistoryFileName { get; set; }

        /// <summary>
        /// This method removes command execution history entries that match the
        /// specified filter.
        /// </summary>
        /// <param name="historyFilter">
        /// The filter that selects which entries are cleared.  When null, all
        /// entries are cleared.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode ClearHistory(
            IHistoryFilter historyFilter,
            ref Result error
            );

        /// <summary>
        /// This method adds a command execution history entry for the specified
        /// arguments.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments that make up the history entry.
        /// </param>
        /// <param name="historyData">
        /// The data that controls how the entry is recorded.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that determines whether the entry should be recorded.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode AddHistory(
            ArgumentList arguments,
            IHistoryData historyData,
            IHistoryFilter historyFilter,
            ref Result error
            );

        /// <summary>
        /// This method loads command execution history entries from a file.
        /// </summary>
        /// <param name="encoding">
        /// The character encoding used to read the file.  When null, a default
        /// encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read history entries from.
        /// </param>
        /// <param name="historyData">
        /// The data that controls how the loaded entries are processed.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which loaded entries are processed.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat any problem encountered during loading as an
        /// error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        ReturnCode LoadHistory(
            Encoding encoding,
            string fileName,
            IHistoryData historyData,
            IHistoryFilter historyFilter,
            bool strict,
            ref Result error
            );

        /// <summary>
        /// This method saves command execution history entries to a file.
        /// </summary>
        /// <param name="encoding">
        /// The character encoding used to write the file.  When null, a default
        /// encoding is used.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to write history entries to.
        /// </param>
        /// <param name="historyData">
        /// The data that controls how the saved entries are processed.
        /// </param>
        /// <param name="historyFilter">
        /// The filter that selects which entries are saved.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat any problem encountered during saving as an
        /// error.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
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
