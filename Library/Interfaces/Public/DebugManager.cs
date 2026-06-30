/*
 * DebugManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by the component that manages
    /// diagnostics and debugging support for an interpreter, including the
    /// debug enable state, single-step control, and the text writers used for
    /// trace and debug output.
    /// </summary>
    [ObjectId("a67a921f-1c4e-4055-91d2-cfdf1fc8f613")]
    public interface IDebugManager
    {
        ///////////////////////////////////////////////////////////////////////
        // DIAGNOSTICS & DEBUGGING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether debugging support is
        /// enabled.
        /// </summary>
        bool Debug { get; set; }

#if DEBUGGER
        /// <summary>
        /// Gets or sets a value indicating whether the script debugger is
        /// operating in single-step mode.
        /// </summary>
        bool SingleStep { get; set; }

        /// <summary>
        /// Determines whether the script debugger is available.
        /// </summary>
        /// <returns>
        /// True if the script debugger is available; otherwise, false.
        /// </returns>
        bool IsDebuggerAvailable();

        /// <summary>
        /// Determines whether the script debugger is active.
        /// </summary>
        /// <returns>
        /// True if the script debugger is active; otherwise, false.
        /// </returns>
        bool IsDebuggerActive();
#endif

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> used for trace output.
        /// </summary>
        TextWriter TraceTextWriter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the trace text writer is
        /// owned by this manager and should be disposed by it.
        /// </summary>
        bool TraceTextWriterOwned { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> used for debug output.
        /// </summary>
        TextWriter DebugTextWriter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the debug text writer is
        /// owned by this manager and should be disposed by it.
        /// </summary>
        bool DebugTextWriterOwned { get; set; }
    }
}
