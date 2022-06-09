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
    [ObjectId("ea131bd8-0f7b-4d9a-ae16-5f41648320a3")]
    public interface IExecuteTrace
    {
        //
        // TODO: Change this to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode Execute(
            BreakpointType breakpointType, Interpreter interpreter,
            ITraceInfo traceInfo, ref Result result);
    }
}
