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
    [ObjectId("935c117f-fba9-4dd6-b3a5-ff2ea4240a10")]
    internal sealed class TestContext : ITestContext, IDisposable
    {
        #region Public Constructors
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

            comparer = null;
            path = null;
            verbose = TestOutputType.Default;
            repeatCount = Count.Invalid;
            current = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { return false; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        private long threadId;
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
        private Interpreter targetInterpreter;
        public Interpreter TargetInterpreter
        {
            get { CheckDisposed(); return targetInterpreter; }
            set { CheckDisposed(); targetInterpreter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long[] statistics;
        public long[] Statistics
        {
            get { CheckDisposed(); return statistics; }
            set { CheckDisposed(); statistics = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList constraints;
        public StringList Constraints
        {
            get { CheckDisposed(); return constraints; }
            set { CheckDisposed(); constraints = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IntDictionary knownBugs;
        public IntDictionary KnownBugs
        {
            get { CheckDisposed(); return knownBugs; }
            set { CheckDisposed(); knownBugs = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringListDictionary skipped;
        public StringListDictionary Skipped
        {
            get { CheckDisposed(); return skipped; }
            set { CheckDisposed(); skipped = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList failures;
        public StringList Failures
        {
            get { CheckDisposed(); return failures; }
            set { CheckDisposed(); failures = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IntDictionary counts;
        public IntDictionary Counts
        {
            get { CheckDisposed(); return counts; }
            set { CheckDisposed(); counts = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList match;
        public StringList Match
        {
            get { CheckDisposed(); return match; }
            set { CheckDisposed(); match = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList skip;
        public StringList Skip
        {
            get { CheckDisposed(); return skip; }
            set { CheckDisposed(); skip = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCodeDictionary returnCodeMessages;
        public ReturnCodeDictionary ReturnCodeMessages
        {
            get { CheckDisposed(); return returnCodeMessages; }
            set { CheckDisposed(); returnCodeMessages = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private StringDictionary breakpoints;
        public StringDictionary Breakpoints
        {
            get { CheckDisposed(); return breakpoints; }
            set { CheckDisposed(); breakpoints = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private IComparer<string> comparer;
        public IComparer<string> Comparer
        {
            get { CheckDisposed(); return comparer; }
            set { CheckDisposed(); comparer = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string path;
        public string Path
        {
            get { CheckDisposed(); return path; }
            set { CheckDisposed(); path = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TestOutputType verbose;
        public TestOutputType Verbose
        {
            get { CheckDisposed(); return verbose; }
            set { CheckDisposed(); verbose = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int repeatCount;
        public int RepeatCount
        {
            get { CheckDisposed(); return repeatCount; }
            set { CheckDisposed(); repeatCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string current;
        public string Current
        {
            get { CheckDisposed(); return current; }
            set { CheckDisposed(); current = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, false))
                throw new InterpreterDisposedException(typeof(TestContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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

                    comparer = null;
                    path = null;
                    verbose = TestOutputType.None;
                    repeatCount = 0;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~TestContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
