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
    [ObjectId("a67a921f-1c4e-4055-91d2-cfdf1fc8f613")]
    public interface IDebugManager
    {
        ///////////////////////////////////////////////////////////////////////
        // DIAGNOSTICS & DEBUGGING
        ///////////////////////////////////////////////////////////////////////

        bool Debug { get; set; }

#if DEBUGGER
        bool SingleStep { get; set; }
        bool IsDebuggerAvailable();
        bool IsDebuggerActive();
#endif

        TextWriter TraceTextWriter { get; set; }
        bool TraceTextWriterOwned { get; set; }

        TextWriter DebugTextWriter { get; set; }
        bool DebugTextWriterOwned { get; set; }
    }
}
