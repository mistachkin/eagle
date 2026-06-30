/*
 * VariableContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by the per-thread context that holds the
    /// variable-related state for an interpreter, including the call stack and
    /// the various well-known call frames (global, current, procedure, and
    /// uplevel) used during variable resolution.
    /// </summary>
    [ObjectId("d37ec74b-cad9-4cd4-9b5d-c07006f45122")]
    internal interface IVariableContext : IThreadContext
    {
        /// <summary>
        /// Gets or sets the call stack of active call frames for this context.
        /// </summary>
        CallStack CallStack { get; set; }

        /// <summary>
        /// Gets or sets the global call frame for this context.
        /// </summary>
        ICallFrame GlobalFrame { get; set; }

        /// <summary>
        /// Gets or sets the global scope call frame for this context.
        /// </summary>
        ICallFrame GlobalScopeFrame { get; set; }

        /// <summary>
        /// Gets or sets the current (innermost) call frame for this context.
        /// </summary>
        ICallFrame CurrentFrame { get; set; }

        /// <summary>
        /// Gets or sets the call frame of the most recent procedure invocation
        /// for this context.
        /// </summary>
        ICallFrame ProcedureFrame { get; set; }

        /// <summary>
        /// Gets or sets the call frame currently targeted by an uplevel
        /// operation for this context.
        /// </summary>
        ICallFrame UplevelFrame { get; set; }

        /// <summary>
        /// Gets the effective current global call frame for this context.
        /// </summary>
        ICallFrame CurrentGlobalFrame { get; }

        /// <summary>
        /// Gets or sets the variable trace information associated with this
        /// context.
        /// </summary>
        ITraceInfo TraceInfo { get; set; }

        /// <summary>
        /// This method releases the variable-related state held by this
        /// context.
        /// </summary>
        /// <param name="global">
        /// Non-zero to also release the global state; otherwise, zero to
        /// release only the non-global state.
        /// </param>
        void Free(bool global);
    }
}
