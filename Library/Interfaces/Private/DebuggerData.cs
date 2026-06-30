/*
 * DebuggerData.cs --
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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    //
    // NOTE: This interface is currently private; however, it may be "promoted"
    //       to public at some point.
    //
    /// <summary>
    /// This interface defines the mutable state maintained by the Eagle script
    /// debugger, including its enabled and suspend status, the configured
    /// breakpoint triggers, step counters, and the queued interactive commands
    /// and their results.
    /// </summary>
    [ObjectId("1881f9cb-204d-450b-97b6-184006478bca")]
    internal interface IDebuggerData : IMaybeDisposed, IHaveInterpreter
    {
        /// <summary>
        /// Gets or sets the number of times the debugger has been suspended.
        /// </summary>
        int SuspendCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger is enabled.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the number of active debugger interactive loops.
        /// </summary>
        int Loops { get; set; }

        /// <summary>
        /// Gets or sets the active debugger evaluation count.
        /// </summary>
        int Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger is in
        /// single-step mode.
        /// </summary>
        bool SingleStep { get; set; }

#if DEBUGGER_BREAKPOINTS
        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks on each
        /// parsed token.
        /// </summary>
        bool BreakOnToken { get; set; }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks before
        /// each command execution.
        /// </summary>
        bool BreakOnExecute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks when a
        /// script cancellation occurs.
        /// </summary>
        bool BreakOnCancel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks when an
        /// error occurs.
        /// </summary>
        bool BreakOnError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks when a
        /// return occurs.
        /// </summary>
        bool BreakOnReturn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks when a
        /// test is run.
        /// </summary>
        bool BreakOnTest { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger breaks when an
        /// exit occurs.
        /// </summary>
        bool BreakOnExit { get; set; }

        /// <summary>
        /// Gets or sets the number of steps remaining before the debugger
        /// breaks.
        /// </summary>
        long Steps { get; set; }

        /// <summary>
        /// Gets or sets the set of breakpoint types that are currently enabled.
        /// </summary>
        BreakpointType Types { get; set; }

#if DEBUGGER_BREAKPOINTS
        /// <summary>
        /// Gets or sets the collection of breakpoints, keyed by their script
        /// locations.
        /// </summary>
        BreakpointDictionary Breakpoints { get; set; }
#endif

#if DEBUGGER_ARGUMENTS
        /// <summary>
        /// Gets or sets the arguments associated with the command execution
        /// that triggered the debugger.
        /// </summary>
        ArgumentList ExecuteArguments { get; set; }
#endif

        /// <summary>
        /// Gets or sets the most recent interactive debugger command.
        /// </summary>
        string Command { get; set; }

        /// <summary>
        /// Gets or sets the result associated with the current debugger state.
        /// </summary>
        Result Result { get; set; }

        /// <summary>
        /// Gets or sets the queue of pending interactive debugger commands.
        /// </summary>
        QueueList<string, string> Queue { get; set; }

        /// <summary>
        /// Gets or sets the arguments passed to the debugger callback.
        /// </summary>
        StringList CallbackArguments { get; set; }
    }
}
