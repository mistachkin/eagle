/*
 * ExecuteTrace.cs --
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
    /// <summary>
    /// This interface is implemented by entities that participate in variable
    /// and command tracing.  It defines the single entry point,
    /// <see cref="Execute" />, that the engine invokes when a trace fires.
    /// </summary>
    [ObjectId("ea131bd8-0f7b-4d9a-ae16-5f41648320a3")]
    public interface IExecuteTrace
    {
        /// <summary>
        /// This method is called by the engine when the associated trace
        /// fires.  It reports its outcome both through the returned
        /// <see cref="ReturnCode" /> and through the <paramref name="result" />
        /// parameter.
        /// </summary>
        /// <param name="breakpointType">
        /// The type of breakpoint or trace operation that caused this method
        /// to be invoked.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context this trace is executing in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The information describing the trace operation being performed,
        /// including the affected variable and operation details.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// trace.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        //
        // TODO: Change this to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode Execute(
            BreakpointType breakpointType, Interpreter interpreter,
            ITraceInfo traceInfo, ref Result result);
    }
}
