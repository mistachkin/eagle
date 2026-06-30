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
    /// <summary>
    /// This class provides a container for client data used to measure the
    /// performance of a named operation, tracking the start and stop counts, the
    /// number of iterations, and the elapsed time in microseconds.
    /// </summary>
    [ObjectId("3d7efb07-3dc5-4b14-b75f-f681358459c9")]
    internal sealed class PerformanceClientData : ClientData
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class wrapping the specified opaque
        /// data payload.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class for the named operation, with
        /// all counters reset to their initial values.
        /// </summary>
        /// <param name="operation">
        /// The name of the operation being measured.  This parameter may be
        /// null.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress automatic reporting of results when measurement
        /// stops.
        /// </param>
        public PerformanceClientData(
            string operation,
            bool quiet
            )
            : this(null, operation, 0, 0, 0, 0, quiet)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class for the named operation, with the
        /// specified opaque data payload and the supplied initial counter
        /// values.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        /// <param name="operation">
        /// The name of the operation being measured.  This parameter may be
        /// null.
        /// </param>
        /// <param name="startCount">
        /// The initial performance counter value recorded when measurement
        /// started.
        /// </param>
        /// <param name="stopCount">
        /// The initial performance counter value recorded when measurement
        /// stopped.
        /// </param>
        /// <param name="iterations">
        /// The initial number of iterations being measured.
        /// </param>
        /// <param name="microseconds">
        /// The initial accumulated elapsed time, in microseconds.
        /// </param>
        /// <param name="quiet">
        /// Non-zero to suppress automatic reporting of results when measurement
        /// stops.
        /// </param>
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
        /// <summary>
        /// Stores the name of the operation being measured.
        /// </summary>
        private string operation;
        /// <summary>
        /// Gets or sets the name of the operation being measured.
        /// </summary>
        public string Operation
        {
            get { return operation; }
            set { operation = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the performance counter value recorded when measurement
        /// started.
        /// </summary>
        private long startCount;
        /// <summary>
        /// Gets or sets the performance counter value recorded when measurement
        /// started.
        /// </summary>
        public long StartCount
        {
            get { return startCount; }
            set { startCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the performance counter value recorded when measurement
        /// stopped.
        /// </summary>
        private long stopCount;
        /// <summary>
        /// Gets or sets the performance counter value recorded when measurement
        /// stopped.
        /// </summary>
        public long StopCount
        {
            get { return stopCount; }
            set { stopCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the number of iterations being measured.
        /// </summary>
        private long iterations;
        /// <summary>
        /// Gets or sets the number of iterations being measured.
        /// </summary>
        public long Iterations
        {
            get { return iterations; }
            set { iterations = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the accumulated elapsed time, in microseconds.
        /// </summary>
        private double microseconds;
        /// <summary>
        /// Gets or sets the accumulated elapsed time, in microseconds.
        /// </summary>
        public double Microseconds
        {
            get { return microseconds; }
            set { microseconds = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether automatic reporting of results is
        /// suppressed when measurement stops.
        /// </summary>
        private bool quiet;
        /// <summary>
        /// Gets or sets a value indicating whether automatic reporting of
        /// results is suppressed when measurement stops.
        /// </summary>
        public bool Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method resets the counters associated with this object to their
        /// initial values.
        /// </summary>
        /// <param name="all">
        /// Non-zero to also reset the accumulated elapsed time, in microseconds;
        /// zero to reset only the start count, stop count, and iterations.
        /// </param>
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

        /// <summary>
        /// This method begins a performance measurement for a single iteration.
        /// </summary>
        public void Start()
        {
            Start(1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins a performance measurement for the specified number
        /// of iterations, recording the starting performance counter value.
        /// </summary>
        /// <param name="iterations">
        /// The number of iterations to add to those being measured.
        /// </param>
        public void Start(
            long iterations
            )
        {
            Reset(false);

            this.iterations += iterations;
            startCount = PerformanceOps.GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a performance measurement, recording the stopping
        /// performance counter value, accumulating the elapsed time, and
        /// reporting the results unless quiet operation is enabled.
        /// </summary>
        public void Stop()
        {
            stopCount = PerformanceOps.GetCount();

            microseconds += PerformanceOps.GetMicrosecondsFromCount(
                startCount, stopCount, iterations, false); /* EXEMPT */

            if (!quiet)
                Report();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a diagnostic trace message describing the
        /// completed operation and its measured performance.
        /// </summary>
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
        /// <summary>
        /// This method returns a string representation of the accumulated
        /// elapsed time associated with this object.
        /// </summary>
        /// <returns>
        /// A string containing the accumulated elapsed time, in microseconds.
        /// </returns>
        public override string ToString()
        {
            return FormatOps.PerformanceMicroseconds(microseconds);
        }
        #endregion
    }
}
