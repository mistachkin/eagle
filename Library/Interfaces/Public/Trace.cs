/*
 * Trace.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by variable trace handlers that can be
    /// attached to an interpreter.  It is an aggregate that composes the trace
    /// identity and metadata (<see cref="ITraceData" />), dynamic-execute trace
    /// callback support (<see cref="IDynamicExecuteTrace" />), the trace
    /// execution entry point (<see cref="IExecuteTrace" />), and one-time setup
    /// support (<see cref="ISetup" />).
    /// </summary>
    [ObjectId("cd57ef9d-7923-490d-bbc5-d708d9e67d2c")]
    public interface ITrace : ITraceData, IDynamicExecuteTrace, IExecuteTrace, ISetup
    {
        // nothing.
    }
}
