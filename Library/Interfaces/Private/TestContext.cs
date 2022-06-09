/*
 * TestContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("d770be83-2928-46b5-9762-2b60be1e9da8")]
    internal interface ITestContext : IThreadContext
    {
        Interpreter TargetInterpreter { get; set; }
        long[] Statistics { get; set; }
        StringList Constraints { get; set; }
        IntDictionary KnownBugs { get; set; }
        StringListDictionary Skipped { get; set; }
        StringList Failures { get; set; }
        IntDictionary Counts { get; set; }
        StringList Match { get; set; }
        StringList Skip { get; set; }
        ReturnCodeDictionary ReturnCodeMessages { get; set; }

#if DEBUGGER
        StringDictionary Breakpoints { get; set; }
#endif

        IComparer<string> Comparer { get; set; }
        string Path { get; set; }
        TestOutputType Verbose { get; set; }
        int RepeatCount { get; set; }
        string Current { get; set; }
    }
}
