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

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the per-thread context that holds the state for the
    /// Eagle test suite, including the target interpreter, test statistics and
    /// counts, the active constraints, known bugs, skipped and failed tests,
    /// matching and skipping patterns, and various other settings that control
    /// how tests are run and reported.  It implements
    /// <see cref="ITestContext" /> and is disposable.
    /// </summary>
    [ObjectId("935c117f-fba9-4dd6-b3a5-ff2ea4240a10")]
    internal sealed class TestContext : ITestContext, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a test context for the specified interpreter and thread,
        /// initializing the test statistics, collections, and settings to their
        /// default values.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this test context.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread that owns this test context.
        /// </param>
        public TestContext(
            Interpreter interpreter,
            long threadId
            )
        {
            this.interpreter = interpreter;
            this.threadId = threadId;

            ///////////////////////////////////////////////////////////////////

            targetInterpreter = null;
            statistics = new long[(int)TestInformationType.SizeOf];
            constraints = new StringList();
            knownBugs = new IntDictionary();
            skipped = new StringListDictionary();
            failures = new StringList();
            counts = new IntDictionary();
            match = new StringList();
            skip = new StringList();
            returnCodeMessages = TestOps.GetReturnCodeMessages();

            ///////////////////////////////////////////////////////////////////

#if DEBUGGER
            breakpoints = new StringDictionary();
#endif

            hooks = new StringDictionary();
            comparer = null;
            path = null;
            verbose = TestOutputType.Default;
            repeatCount = Count.Invalid;
            previous = null;
            current = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this test context has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this test context is currently in
        /// the process of being disposed; this property always returns zero for
        /// this test context.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// Stores the interpreter that owns this test context.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter that owns this test context.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        /// <summary>
        /// Stores the identifier of the thread that owns this test context.
        /// </summary>
        private long threadId;
        /// <summary>
        /// Gets the identifier of the thread that owns this test context.
        /// </summary>
        public long ThreadId
        {
            get
            {
                //
                // NOTE: *EXEMPT* Hot path.
                //
                // CheckDisposed();

                return threadId;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITestContext Members
        /// <summary>
        /// Stores the interpreter that tests are being run against.
        /// </summary>
        private Interpreter targetInterpreter;
        /// <summary>
        /// Gets or sets the interpreter that tests are being run against.
        /// </summary>
        public Interpreter TargetInterpreter
        {
            get { CheckDisposed(); return targetInterpreter; }
            set { CheckDisposed(); targetInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the array of accumulated test statistics.
        /// </summary>
        private long[] statistics;
        /// <summary>
        /// Gets or sets the array of accumulated test statistics.
        /// </summary>
        public long[] Statistics
        {
            get { CheckDisposed(); return statistics; }
            set { CheckDisposed(); statistics = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of currently active test constraints.
        /// </summary>
        private StringList constraints;
        /// <summary>
        /// Gets or sets the list of currently active test constraints.
        /// </summary>
        public StringList Constraints
        {
            get { CheckDisposed(); return constraints; }
            set { CheckDisposed(); constraints = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of known bugs, keyed by test name.
        /// </summary>
        private IntDictionary knownBugs;
        /// <summary>
        /// Gets or sets the collection of known bugs, keyed by test name.
        /// </summary>
        public IntDictionary KnownBugs
        {
            get { CheckDisposed(); return knownBugs; }
            set { CheckDisposed(); knownBugs = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of skipped tests, keyed by the reason they
        /// were skipped.
        /// </summary>
        private StringListDictionary skipped;
        /// <summary>
        /// Gets or sets the collection of skipped tests, keyed by the reason
        /// they were skipped.
        /// </summary>
        public StringListDictionary Skipped
        {
            get { CheckDisposed(); return skipped; }
            set { CheckDisposed(); skipped = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of tests that have failed.
        /// </summary>
        private StringList failures;
        /// <summary>
        /// Gets or sets the list of tests that have failed.
        /// </summary>
        public StringList Failures
        {
            get { CheckDisposed(); return failures; }
            set { CheckDisposed(); failures = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of test result counts, keyed by category.
        /// </summary>
        private IntDictionary counts;
        /// <summary>
        /// Gets or sets the collection of test result counts, keyed by
        /// category.
        /// </summary>
        public IntDictionary Counts
        {
            get { CheckDisposed(); return counts; }
            set { CheckDisposed(); counts = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of patterns identifying which tests to run.
        /// </summary>
        private StringList match;
        /// <summary>
        /// Gets or sets the list of patterns identifying which tests to run.
        /// </summary>
        public StringList Match
        {
            get { CheckDisposed(); return match; }
            set { CheckDisposed(); match = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of patterns identifying which tests to skip.
        /// </summary>
        private StringList skip;
        /// <summary>
        /// Gets or sets the list of patterns identifying which tests to skip.
        /// </summary>
        public StringList Skip
        {
            get { CheckDisposed(); return skip; }
            set { CheckDisposed(); skip = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of messages associated with particular return
        /// codes.
        /// </summary>
        private ReturnCodeDictionary returnCodeMessages;
        /// <summary>
        /// Gets or sets the collection of messages associated with particular
        /// return codes.
        /// </summary>
        public ReturnCodeDictionary ReturnCodeMessages
        {
            get { CheckDisposed(); return returnCodeMessages; }
            set { CheckDisposed(); returnCodeMessages = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// Stores the collection of script debugger breakpoints, keyed by name.
        /// </summary>
        private StringDictionary breakpoints;
        /// <summary>
        /// Gets or sets the collection of script debugger breakpoints, keyed by
        /// name.
        /// </summary>
        public StringDictionary Breakpoints
        {
            get { CheckDisposed(); return breakpoints; }
            set { CheckDisposed(); breakpoints = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the collection of test hooks, keyed by name.
        /// </summary>
        private StringDictionary hooks;
        /// <summary>
        /// Gets or sets the collection of test hooks, keyed by name.
        /// </summary>
        public StringDictionary Hooks
        {
            get { CheckDisposed(); return hooks; }
            set { CheckDisposed(); hooks = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the string comparer used when matching and ordering test
        /// names.
        /// </summary>
        private IComparer<string> comparer;
        /// <summary>
        /// Gets or sets the string comparer used when matching and ordering
        /// test names.
        /// </summary>
        public IComparer<string> Comparer
        {
            get { CheckDisposed(); return comparer; }
            set { CheckDisposed(); comparer = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the path associated with the current test run.
        /// </summary>
        private string path;
        /// <summary>
        /// Gets or sets the path associated with the current test run.
        /// </summary>
        public string Path
        {
            get { CheckDisposed(); return path; }
            set { CheckDisposed(); path = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the verbosity level controlling how much test output is
        /// produced.
        /// </summary>
        private TestOutputType verbose;
        /// <summary>
        /// Gets or sets the verbosity level controlling how much test output is
        /// produced.
        /// </summary>
        public TestOutputType Verbose
        {
            get { CheckDisposed(); return verbose; }
            set { CheckDisposed(); verbose = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of times each test should be repeated.
        /// </summary>
        private int repeatCount;
        /// <summary>
        /// Gets or sets the number of times each test should be repeated.
        /// </summary>
        public int RepeatCount
        {
            get { CheckDisposed(); return repeatCount; }
            set { CheckDisposed(); repeatCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the test that ran immediately before the current
        /// one.
        /// </summary>
        private string previous;
        /// <summary>
        /// Gets or sets the name of the test that ran immediately before the
        /// current one.
        /// </summary>
        public string Previous
        {
            get { CheckDisposed(); return previous; }
            set { CheckDisposed(); previous = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the test that is currently running.
        /// </summary>
        private string current;
        /// <summary>
        /// Gets or sets the name of the test that is currently running.
        /// </summary>
        public string Current
        {
            get { CheckDisposed(); return current; }
            set { CheckDisposed(); current = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this test context has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this test context has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this test context has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(TestContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this test context.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: disposing = {0}, interpreter = {1}, disposed = {2}",
                disposing, FormatOps.InterpreterNoThrow(interpreter), disposed),
                typeof(TestContext).Name, TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    interpreter = null; /* NOT OWNED: Do not dispose. */
                    threadId = 0;

                    ///////////////////////////////////////////////////////////

                    targetInterpreter = null; /* NOT OWNED: Do not dispose. */

                    ///////////////////////////////////////////////////////////

                    statistics = null;

                    ///////////////////////////////////////////////////////////

                    if (constraints != null)
                    {
                        constraints.Clear();
                        constraints = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (knownBugs != null)
                    {
                        knownBugs.Clear();
                        knownBugs = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (skipped != null)
                    {
                        skipped.Clear();
                        skipped = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (failures != null)
                    {
                        failures.Clear();
                        failures = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (counts != null)
                    {
                        counts.Clear();
                        counts = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (match != null)
                    {
                        match.Clear();
                        match = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (skip != null)
                    {
                        skip.Clear();
                        skip = null;
                    }

                    ///////////////////////////////////////////////////////////

                    if (returnCodeMessages != null)
                    {
                        returnCodeMessages.Clear();
                        returnCodeMessages = null;
                    }

                    ///////////////////////////////////////////////////////////

#if DEBUGGER
                    if (breakpoints != null)
                    {
                        breakpoints.Clear();
                        breakpoints = null;
                    }
#endif

                    ///////////////////////////////////////////////////////////

                    if (hooks != null)
                    {
                        hooks.Clear();
                        hooks = null;
                    }

                    ///////////////////////////////////////////////////////////

                    comparer = null;
                    path = null;
                    verbose = TestOutputType.None;
                    repeatCount = 0;
                    previous = null;
                    current = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this test context and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this test context, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~TestContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
