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
    /// <summary>
    /// This interface is implemented by the per-thread context that holds the
    /// state for the Eagle test suite, including the target interpreter, test
    /// statistics and counts, the active constraints, known bugs, skipped and
    /// failed tests, matching and skipping patterns, and various other settings
    /// that control how tests are run and reported.
    /// </summary>
    [ObjectId("d770be83-2928-46b5-9762-2b60be1e9da8")]
    internal interface ITestContext : IThreadContext
    {
        /// <summary>
        /// Gets or sets the interpreter that tests are being run against.
        /// </summary>
        Interpreter TargetInterpreter { get; set; }

        /// <summary>
        /// Gets or sets the array of accumulated test statistics.
        /// </summary>
        long[] Statistics { get; set; }

        /// <summary>
        /// Gets or sets the list of currently active test constraints.
        /// </summary>
        StringList Constraints { get; set; }

        /// <summary>
        /// Gets or sets the collection of known bugs, keyed by test name.
        /// </summary>
        IntDictionary KnownBugs { get; set; }

        /// <summary>
        /// Gets or sets the collection of skipped tests, keyed by the reason
        /// they were skipped.
        /// </summary>
        StringListDictionary Skipped { get; set; }

        /// <summary>
        /// Gets or sets the list of tests that have failed.
        /// </summary>
        StringList Failures { get; set; }

        /// <summary>
        /// Gets or sets the collection of test result counts, keyed by category.
        /// </summary>
        IntDictionary Counts { get; set; }

        /// <summary>
        /// Gets or sets the list of patterns identifying which tests to run.
        /// </summary>
        StringList Match { get; set; }

        /// <summary>
        /// Gets or sets the list of patterns identifying which tests to skip.
        /// </summary>
        StringList Skip { get; set; }

        /// <summary>
        /// Gets or sets the collection of messages associated with particular
        /// return codes.
        /// </summary>
        ReturnCodeDictionary ReturnCodeMessages { get; set; }

#if DEBUGGER
        /// <summary>
        /// Gets or sets the collection of script debugger breakpoints, keyed by
        /// name.
        /// </summary>
        StringDictionary Breakpoints { get; set; }
#endif

        /// <summary>
        /// Gets or sets the collection of test hooks, keyed by name.
        /// </summary>
        StringDictionary Hooks { get; set; }

        /// <summary>
        /// Gets or sets the string comparer used when matching and ordering
        /// test names.
        /// </summary>
        IComparer<string> Comparer { get; set; }

        /// <summary>
        /// Gets or sets the path associated with the current test run.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Gets or sets the verbosity level controlling how much test output is
        /// produced.
        /// </summary>
        TestOutputType Verbose { get; set; }

        /// <summary>
        /// Gets or sets the number of times each test should be repeated.
        /// </summary>
        int RepeatCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the test that ran immediately before the
        /// current one.
        /// </summary>
        string Previous { get; set; }

        /// <summary>
        /// Gets or sets the name of the test that is currently running.
        /// </summary>
        string Current { get; set; }
    }
}
