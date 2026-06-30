/*
 * TraceClientData.cs --
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
using System.Diagnostics;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class encapsulates the client data used to configure the tracing
    /// subsystem, including the trace listeners, logging options, and category
    /// filters.
    /// </summary>
    [ObjectId("6fc17841-7678-43d9-8ffe-ed34a204464e")]
    public sealed class TraceClientData : AnyClientData
    {
        #region Private Data
        /// <summary>
        /// Stores the object used to synchronize access to the state of this
        /// instance.
        /// </summary>
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        public TraceClientData()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="data">
        /// The data to be associated with this instance.
        /// </param>
        public TraceClientData(
            object data /* in */
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="data">
        /// The data to be associated with this instance.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero if the associated data should be read-only.
        /// </param>
        public TraceClientData(
            object data,  /* in */
            bool readOnly /* in */
            )
            : base(data, readOnly)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores the collection of trace listeners.
        /// </summary>
        private TraceListenerCollection listeners;
        /// <summary>
        /// Gets or sets the collection of trace listeners.
        /// </summary>
        public TraceListenerCollection Listeners
        {
            get { CheckDisposed(); lock (syncRoot) { return listeners; } }
            set { CheckDisposed(); lock (syncRoot) { listeners = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the log.
        /// </summary>
        private string logName;
        /// <summary>
        /// Gets or sets the name of the log.
        /// </summary>
        public string LogName
        {
            get { CheckDisposed(); lock (syncRoot) { return logName; } }
            set { CheckDisposed(); lock (syncRoot) { logName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the file name of the log.
        /// </summary>
        private string logFileName;
        /// <summary>
        /// Gets or sets the file name of the log.
        /// </summary>
        public string LogFileName
        {
            get { CheckDisposed(); lock (syncRoot) { return logFileName; } }
            set { CheckDisposed(); lock (syncRoot) { logFileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the encoding used for the log.
        /// </summary>
        private Encoding logEncoding;
        /// <summary>
        /// Gets or sets the encoding used for the log.
        /// </summary>
        public Encoding LogEncoding
        {
            get { CheckDisposed(); lock (syncRoot) { return logEncoding; } }
            set { CheckDisposed(); lock (syncRoot) { logEncoding = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags used for the log.
        /// </summary>
        private LogFlags? logFlags;
        /// <summary>
        /// Gets or sets the flags used for the log.
        /// </summary>
        public LogFlags? LogFlags
        {
            get { CheckDisposed(); lock (syncRoot) { return logFlags; } }
            set { CheckDisposed(); lock (syncRoot) { logFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the trace categories that are enabled.
        /// </summary>
        private IEnumerable<string> enabledCategories;
        /// <summary>
        /// Gets or sets the trace categories that are enabled.
        /// </summary>
        public IEnumerable<string> EnabledCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return enabledCategories; } }
            set { CheckDisposed(); lock (syncRoot) { enabledCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the trace categories that are disabled.
        /// </summary>
        private IEnumerable<string> disabledCategories;
        /// <summary>
        /// Gets or sets the trace categories that are disabled.
        /// </summary>
        public IEnumerable<string> DisabledCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return disabledCategories; } }
            set { CheckDisposed(); lock (syncRoot) { disabledCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the trace categories that incur a priority penalty.
        /// </summary>
        private IEnumerable<string> penaltyCategories;
        /// <summary>
        /// Gets or sets the trace categories that incur a priority penalty.
        /// </summary>
        public IEnumerable<string> PenaltyCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return penaltyCategories; } }
            set { CheckDisposed(); lock (syncRoot) { penaltyCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the trace categories that receive a priority bonus.
        /// </summary>
        private IEnumerable<string> bonusCategories;
        /// <summary>
        /// Gets or sets the trace categories that receive a priority bonus.
        /// </summary>
        public IEnumerable<string> BonusCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return bonusCategories; } }
            set { CheckDisposed(); lock (syncRoot) { bonusCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the trace state type.
        /// </summary>
        private TraceStateType stateType;
        /// <summary>
        /// Gets or sets the trace state type.
        /// </summary>
        public TraceStateType StateType
        {
            get { CheckDisposed(); lock (syncRoot) { return stateType; } }
            set { CheckDisposed(); lock (syncRoot) { stateType = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the trace priority flags.
        /// </summary>
        private TracePriority? priorities;
        /// <summary>
        /// Gets or sets the trace priority flags.
        /// </summary>
        public TracePriority? Priorities
        {
            get { CheckDisposed(); lock (syncRoot) { return priorities; } }
            set { CheckDisposed(); lock (syncRoot) { priorities = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the format string used for trace output.
        /// </summary>
        private string formatString;
        /// <summary>
        /// Gets or sets the format string used for trace output.
        /// </summary>
        public string FormatString
        {
            get { CheckDisposed(); lock (syncRoot) { return formatString; } }
            set { CheckDisposed(); lock (syncRoot) { formatString = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the index of the format string used for trace output.
        /// </summary>
        private int? formatIndex;
        /// <summary>
        /// Gets or sets the index of the format string used for trace output.
        /// </summary>
        public int? FormatIndex
        {
            get { CheckDisposed(); lock (syncRoot) { return formatIndex; } }
            set { CheckDisposed(); lock (syncRoot) { formatIndex = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether tracing is forcibly enabled.
        /// </summary>
        private bool? forceEnabled;
        /// <summary>
        /// Gets or sets a value indicating whether tracing is forcibly enabled.
        /// </summary>
        public bool? ForceEnabled
        {
            get { CheckDisposed(); lock (syncRoot) { return forceEnabled; } }
            set { CheckDisposed(); lock (syncRoot) { forceEnabled = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the tracing system should be reset.
        /// </summary>
        private bool resetSystem;
        /// <summary>
        /// Gets or sets a value indicating whether the tracing system should be
        /// reset.
        /// </summary>
        public bool ResetSystem
        {
            get { CheckDisposed(); lock (syncRoot) { return resetSystem; } }
            set { CheckDisposed(); lock (syncRoot) { resetSystem = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the trace listeners should be
        /// reset.
        /// </summary>
        private bool resetListeners;
        /// <summary>
        /// Gets or sets a value indicating whether the trace listeners should be
        /// reset.
        /// </summary>
        public bool ResetListeners
        {
            get { CheckDisposed(); lock (syncRoot) { return resetListeners; } }
            set { CheckDisposed(); lock (syncRoot) { resetListeners = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether tracing is enabled.
        /// </summary>
        private bool trace;
        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// </summary>
        public bool Trace
        {
            get { CheckDisposed(); lock (syncRoot) { return trace; } }
            set { CheckDisposed(); lock (syncRoot) { trace = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether debugging is enabled.
        /// </summary>
        private bool debug;
        /// <summary>
        /// Gets or sets a value indicating whether debugging is enabled.
        /// </summary>
        public bool Debug
        {
            get { CheckDisposed(); lock (syncRoot) { return debug; } }
            set { CheckDisposed(); lock (syncRoot) { debug = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether verbose output is enabled.
        /// </summary>
        private bool verbose;
        /// <summary>
        /// Gets or sets a value indicating whether verbose output is enabled.
        /// </summary>
        public bool Verbose
        {
            get { CheckDisposed(); lock (syncRoot) { return verbose; } }
            set { CheckDisposed(); lock (syncRoot) { verbose = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the default trace listener should
        /// be used.
        /// </summary>
        private bool useDefault;
        /// <summary>
        /// Gets or sets a value indicating whether the default trace listener
        /// should be used.
        /// </summary>
        public bool UseDefault
        {
            get { CheckDisposed(); lock (syncRoot) { return useDefault; } }
            set { CheckDisposed(); lock (syncRoot) { useDefault = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the console should be used for
        /// trace output.
        /// </summary>
        private bool useConsole;
        /// <summary>
        /// Gets or sets a value indicating whether the console should be used
        /// for trace output.
        /// </summary>
        public bool UseConsole
        {
            get { CheckDisposed(); lock (syncRoot) { return useConsole; } }
            set { CheckDisposed(); lock (syncRoot) { useConsole = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether native output should be used for
        /// tracing.
        /// </summary>
        private bool useNative;
        /// <summary>
        /// Gets or sets a value indicating whether native output should be used
        /// for tracing.
        /// </summary>
        public bool UseNative
        {
            get { CheckDisposed(); lock (syncRoot) { return useNative; } }
            set { CheckDisposed(); lock (syncRoot) { useNative = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the log file should be written
        /// using raw output.
        /// </summary>
        private bool rawLogFile;
        /// <summary>
        /// Gets or sets a value indicating whether the log file should be
        /// written using raw output.
        /// </summary>
        public bool RawLogFile
        {
            get { CheckDisposed(); lock (syncRoot) { return rawLogFile; } }
            set { CheckDisposed(); lock (syncRoot) { rawLogFile = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the status form should be used.
        /// </summary>
        private bool useStatusForm;
        /// <summary>
        /// Gets or sets a value indicating whether the status form should be
        /// used.
        /// </summary>
        public bool UseStatusForm
        {
            get { CheckDisposed(); lock (syncRoot) { return useStatusForm; } }
            set { CheckDisposed(); lock (syncRoot) { useStatusForm = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether status indicators should be used.
        /// </summary>
        private bool? useIndicators;
        /// <summary>
        /// Gets or sets a value indicating whether status indicators should be
        /// used.
        /// </summary>
        public bool? UseIndicators
        {
            get { CheckDisposed(); lock (syncRoot) { return useIndicators; } }
            set { CheckDisposed(); lock (syncRoot) { useIndicators = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether status indicators should be written
        /// using raw output.
        /// </summary>
        private bool rawIndicators;
        /// <summary>
        /// Gets or sets a value indicating whether status indicators should be
        /// written using raw output.
        /// </summary>
        public bool RawIndicators
        {
            get { CheckDisposed(); lock (syncRoot) { return rawIndicators; } }
            set { CheckDisposed(); lock (syncRoot) { rawIndicators = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the trace listeners should be
        /// reported.
        /// </summary>
        private bool seeListeners;
        /// <summary>
        /// Gets or sets a value indicating whether the trace listeners should be
        /// reported.
        /// </summary>
        public bool SeeListeners
        {
            get { CheckDisposed(); lock (syncRoot) { return seeListeners; } }
            set { CheckDisposed(); lock (syncRoot) { seeListeners = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of results accumulated by this instance.
        /// </summary>
        private ResultList results;
        /// <summary>
        /// Gets or sets the list of results accumulated by this instance.
        /// </summary>
        public ResultList Results
        {
            get { CheckDisposed(); lock (syncRoot) { return results; } }
            set { CheckDisposed(); lock (syncRoot) { results = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method populates the collection of trace listeners with the
        /// default set of listeners, if it has not already been populated.
        /// </summary>
        public void PopulateListeners()
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (listeners == null)
                    listeners = DebugOps.GetListeners(debug);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds a result to the list of results accumulated by this
        /// instance, creating the list if necessary.
        /// </summary>
        /// <param name="result">
        /// The result to be added.  If this value is null, no result is added.
        /// </param>
        public void AddResult(
            Result result
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (results == null)
                    results = new ResultList();

                if (result != null)
                    results.Add(result);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this instance has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this instance has already been
        /// disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(TraceClientData).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the IDisposable.Dispose
        /// method; zero if being called from the finalizer.
        /// </param>
        protected override void Dispose(
            bool disposing /* in */
            )
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            listeners = null; /* NOT OWNED */

                            ////////////////////////////////

                            logName = null;
                            logFileName = null;
                            logEncoding = null;
                            logFlags = null;
                            enabledCategories = null;
                            disabledCategories = null;
                            penaltyCategories = null;
                            bonusCategories = null;
                            stateType = TraceStateType.None;
                            priorities = null;
                            formatString = null;
                            formatIndex = null;
                            forceEnabled = null;
                            resetSystem = false;
                            resetListeners = false;
                            trace = false;
                            debug = false;
                            verbose = false;
                            useDefault = false;
                            useConsole = false;
                            useNative = false;
                            rawLogFile = false;
                            useIndicators = null;
                            rawIndicators = false;
                            seeListeners = false;

                            ////////////////////////////////

                            if (results != null)
                            {
                                results.Clear();
                                results = null;
                            }
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
