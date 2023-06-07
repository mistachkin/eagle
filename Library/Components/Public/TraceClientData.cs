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
    [ObjectId("6fc17841-7678-43d9-8ffe-ed34a204464e")]
    public sealed class TraceClientData : AnyClientData
    {
        #region Private Data
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public TraceClientData()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public TraceClientData(
            object data /* in */
            )
            : base(data)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
        private TraceListenerCollection listeners;
        public TraceListenerCollection Listeners
        {
            get { CheckDisposed(); lock (syncRoot) { return listeners; } }
            set { CheckDisposed(); lock (syncRoot) { listeners = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private string logName;
        public string LogName
        {
            get { CheckDisposed(); lock (syncRoot) { return logName; } }
            set { CheckDisposed(); lock (syncRoot) { logName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private string logFileName;
        public string LogFileName
        {
            get { CheckDisposed(); lock (syncRoot) { return logFileName; } }
            set { CheckDisposed(); lock (syncRoot) { logFileName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private Encoding logEncoding;
        public Encoding LogEncoding
        {
            get { CheckDisposed(); lock (syncRoot) { return logEncoding; } }
            set { CheckDisposed(); lock (syncRoot) { logEncoding = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private LogFlags? logFlags;
        public LogFlags? LogFlags
        {
            get { CheckDisposed(); lock (syncRoot) { return logFlags; } }
            set { CheckDisposed(); lock (syncRoot) { logFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> enabledCategories;
        public IEnumerable<string> EnabledCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return enabledCategories; } }
            set { CheckDisposed(); lock (syncRoot) { enabledCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> disabledCategories;
        public IEnumerable<string> DisabledCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return disabledCategories; } }
            set { CheckDisposed(); lock (syncRoot) { disabledCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> penaltyCategories;
        public IEnumerable<string> PenaltyCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return penaltyCategories; } }
            set { CheckDisposed(); lock (syncRoot) { penaltyCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> bonusCategories;
        public IEnumerable<string> BonusCategories
        {
            get { CheckDisposed(); lock (syncRoot) { return bonusCategories; } }
            set { CheckDisposed(); lock (syncRoot) { bonusCategories = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private TraceStateType stateType;
        public TraceStateType StateType
        {
            get { CheckDisposed(); lock (syncRoot) { return stateType; } }
            set { CheckDisposed(); lock (syncRoot) { stateType = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private TracePriority? priorities;
        public TracePriority? Priorities
        {
            get { CheckDisposed(); lock (syncRoot) { return priorities; } }
            set { CheckDisposed(); lock (syncRoot) { priorities = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private string formatString;
        public string FormatString
        {
            get { CheckDisposed(); lock (syncRoot) { return formatString; } }
            set { CheckDisposed(); lock (syncRoot) { formatString = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private int? formatIndex;
        public int? FormatIndex
        {
            get { CheckDisposed(); lock (syncRoot) { return formatIndex; } }
            set { CheckDisposed(); lock (syncRoot) { formatIndex = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool? forceEnabled;
        public bool? ForceEnabled
        {
            get { CheckDisposed(); lock (syncRoot) { return forceEnabled; } }
            set { CheckDisposed(); lock (syncRoot) { forceEnabled = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool resetSystem;
        public bool ResetSystem
        {
            get { CheckDisposed(); lock (syncRoot) { return resetSystem; } }
            set { CheckDisposed(); lock (syncRoot) { resetSystem = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool resetListeners;
        public bool ResetListeners
        {
            get { CheckDisposed(); lock (syncRoot) { return resetListeners; } }
            set { CheckDisposed(); lock (syncRoot) { resetListeners = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool trace;
        public bool Trace
        {
            get { CheckDisposed(); lock (syncRoot) { return trace; } }
            set { CheckDisposed(); lock (syncRoot) { trace = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool debug;
        public bool Debug
        {
            get { CheckDisposed(); lock (syncRoot) { return debug; } }
            set { CheckDisposed(); lock (syncRoot) { debug = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool verbose;
        public bool Verbose
        {
            get { CheckDisposed(); lock (syncRoot) { return verbose; } }
            set { CheckDisposed(); lock (syncRoot) { verbose = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useDefault;
        public bool UseDefault
        {
            get { CheckDisposed(); lock (syncRoot) { return useDefault; } }
            set { CheckDisposed(); lock (syncRoot) { useDefault = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useConsole;
        public bool UseConsole
        {
            get { CheckDisposed(); lock (syncRoot) { return useConsole; } }
            set { CheckDisposed(); lock (syncRoot) { useConsole = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useNative;
        public bool UseNative
        {
            get { CheckDisposed(); lock (syncRoot) { return useNative; } }
            set { CheckDisposed(); lock (syncRoot) { useNative = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool rawLogFile;
        public bool RawLogFile
        {
            get { CheckDisposed(); lock (syncRoot) { return rawLogFile; } }
            set { CheckDisposed(); lock (syncRoot) { rawLogFile = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private ResultList results;
        public ResultList Results
        {
            get { CheckDisposed(); lock (syncRoot) { return results; } }
            set { CheckDisposed(); lock (syncRoot) { results = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(TraceClientData).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
