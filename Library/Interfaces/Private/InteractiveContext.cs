/*
 * InteractiveContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by the per-thread context that holds the
    /// state associated with an interpreter's interactive (shell) use.  In
    /// addition to the per-thread state it composes
    /// (<see cref="IThreadContext" />), it tracks the interactive input
    /// buffer, interactive loop nesting and totals, the optional shell command
    /// arguments and callbacks, and command-history loading, saving, and
    /// filtering.
    /// </summary>
    [ObjectId("690d1c76-7901-4a6d-b710-602a33968669")]
    internal interface IInteractiveContext : IThreadContext
    {
#if SHELL
        /// <summary>
        /// Gets or sets the semaphore used to coordinate entry into and exit
        /// from the interactive loop.
        /// </summary>
        Semaphore InteractiveLoopSemaphore { get; set; }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether the interpreter is
        /// currently in interactive (shell) mode.
        /// </summary>
        bool Interactive { get; set; }

        /// <summary>
        /// Gets or sets the current nesting level of interactively evaluated
        /// scripts.
        /// </summary>
        int InteractiveScriptLevels { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether interactive input is
        /// currently enabled.
        /// </summary>
        MaybeEnableType InteractiveInputEnabled { get; set; }

        /// <summary>
        /// Gets or sets the buffer that accumulates the interactive input read
        /// so far.
        /// </summary>
        StringBuilder InteractiveInputBuffer { get; set; }

        /// <summary>
        /// Gets or sets the most recently read line of interactive input.
        /// </summary>
        string InteractiveInput { get; set; }

        /// <summary>
        /// Gets or sets the previously read line of interactive input.
        /// </summary>
        string PreviousInteractiveInput { get; set; }

        /// <summary>
        /// Gets or sets the current interactive mode name.
        /// </summary>
        string InteractiveMode { get; set; }

        /// <summary>
        /// Gets or sets the number of interactive loops that are currently
        /// active (i.e. nested).
        /// </summary>
        int ActiveInteractiveLoops { get; set; }

        /// <summary>
        /// Gets or sets the total number of interactive loops that have been
        /// entered.
        /// </summary>
        int TotalInteractiveLoops { get; set; }

        /// <summary>
        /// Gets or sets the total number of interactive inputs that have been
        /// read.
        /// </summary>
        int TotalInteractiveInputs { get; set; }

#if SHELL
        /// <summary>
        /// Gets or sets the saved copy of the shell command-line arguments.
        /// </summary>
        IList<string> SavedShellArguments { get; set; }

        /// <summary>
        /// Gets or sets the current shell command-line arguments.
        /// </summary>
        IList<string> ShellArguments { get; set; }

        /// <summary>
        /// Gets or sets the data describing the shell callbacks in effect.
        /// </summary>
        IShellCallbackData ShellCallbackData { get; set; }

        /// <summary>
        /// Gets or sets the data describing the active interactive loop.
        /// </summary>
        IInteractiveLoopData InteractiveLoopData { get; set; }

        /// <summary>
        /// Gets or sets the data describing a pending or in-progress update
        /// operation.
        /// </summary>
        IUpdateData UpdateData { get; set; }
#endif

        /// <summary>
        /// Gets or sets the callback used to transform interactive command
        /// input before it is processed.
        /// </summary>
        StringTransformCallback InteractiveCommandCallback { get; set; }

#if HISTORY
        /// <summary>
        /// Gets or sets the data controlling how command history is loaded.
        /// </summary>
        IHistoryData HistoryLoadData { get; set; }

        /// <summary>
        /// Gets or sets the data controlling how command history is saved.
        /// </summary>
        IHistoryData HistorySaveData { get; set; }

        /// <summary>
        /// Gets or sets the filter applied when querying command history
        /// information.
        /// </summary>
        IHistoryFilter HistoryInfoFilter { get; set; }

        /// <summary>
        /// Gets or sets the filter applied when loading command history.
        /// </summary>
        IHistoryFilter HistoryLoadFilter { get; set; }

        /// <summary>
        /// Gets or sets the filter applied when saving command history.
        /// </summary>
        IHistoryFilter HistorySaveFilter { get; set; }

        /// <summary>
        /// Gets or sets the file name used to persist command history.
        /// </summary>
        string HistoryFileName { get; set; }
#endif
    }
}
