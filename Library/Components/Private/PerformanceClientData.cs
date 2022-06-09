/*
 * PerformanceClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("3d7efb07-3dc5-4b14-b75f-f681358459c9")]
    internal sealed class PerformanceClientData : ClientData
    {
        #region Private Constructors
        private PerformanceClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public PerformanceClientData(
            string operation,
            bool quiet
            )
            : this(null, operation, 0, 0, 0, 0, quiet)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PerformanceClientData(
            object data,
            string operation,
            long startCount,
            long stopCount,
            long iterations,
            double microseconds,
            bool quiet
            )
            : this(data)
        {
            this.operation = operation;
            this.startCount = startCount;
            this.stopCount = stopCount;
            this.iterations = iterations;
            this.microseconds = microseconds;
            this.quiet = quiet;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private string operation;
        public string Operation
        {
            get { return operation; }
            set { operation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long startCount;
        public long StartCount
        {
            get { return startCount; }
            set { startCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long stopCount;
        public long StopCount
        {
            get { return stopCount; }
            set { stopCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long iterations;
        public long Iterations
        {
            get { return iterations; }
            set { iterations = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private double microseconds;
        public double Microseconds
        {
            get { return microseconds; }
            set { microseconds = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool quiet;
        public bool Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Reset(
            bool all
            )
        {
            startCount = 0;
            stopCount = 0;
            iterations = 0;

            if (all)
                microseconds = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public void Start()
        {
            Start(1);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Start(
            long iterations
            )
        {
            Reset(false);

            this.iterations += iterations;
            startCount = PerformanceOps.GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public void Stop()
        {
            stopCount = PerformanceOps.GetCount();

            microseconds += PerformanceOps.GetMicroseconds(
                startCount, stopCount, iterations, false); /* EXEMPT */

            if (!quiet)
                Report();
        }

        ///////////////////////////////////////////////////////////////////////

        public void Report()
        {
            TraceOps.DebugTrace(String.Format(
                "Report: completed operation {0} in {1}",
                FormatOps.WrapOrNull(operation), this),
                typeof(PerformanceClientData).Name,
                TracePriority.TestDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return FormatOps.Performance(microseconds);
        }
        #endregion
    }
}
