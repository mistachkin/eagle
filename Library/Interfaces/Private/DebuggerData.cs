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
    [ObjectId("1881f9cb-204d-450b-97b6-184006478bca")]
    internal interface IDebuggerData : IMaybeDisposed, IHaveInterpreter
    {
        int SuspendCount { get; set; }
        bool Enabled { get; set; }
        int Loops { get; set; }
        int Active { get; set; }
        bool SingleStep { get; set; }

#if BREAKPOINTS
        bool BreakOnToken { get; set; }
#endif

        bool BreakOnExecute { get; set; }
        bool BreakOnCancel { get; set; }
        bool BreakOnError { get; set; }
        bool BreakOnReturn { get; set; }
        bool BreakOnTest { get; set; }
        bool BreakOnExit { get; set; }
        long Steps { get; set; }
        BreakpointType Types { get; set; }

#if BREAKPOINTS
        BreakpointDictionary Breakpoints { get; set; }
#endif

#if DEBUGGER_ARGUMENTS
        ArgumentList ExecuteArguments { get; set; }
#endif

        string Command { get; set; }
        Result Result { get; set; }
        QueueList<string, string> Queue { get; set; }

        StringList CallbackArguments { get; set; }
    }
}
