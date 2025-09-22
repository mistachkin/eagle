/*
 * ProcessWaitInfo.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Diagnostics;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("048901bf-df0e-4956-89eb-b4325746a220")]
    internal sealed class ProcessWaitInfo
    {
        #region Public Constructors
        public ProcessWaitInfo(
            Interpreter interpreter,    /* in */
            ProcessStartInfo startInfo, /* in */
            Process process,            /* in */
            string outputLogPath,       /* in */
            string errorLogPath,        /* in */
            string logTag,              /* in */
            int? timeout,               /* in */
            EventFlags eventFlags,      /* in */
            bool userInterface,         /* in */
            bool noSleep,               /* in */
            bool killOnError,           /* in */
            bool background             /* in */
            )
        {
            this.interpreter = interpreter;
            this.startInfo = startInfo;
            this.process = process;
            this.outputLogPath = outputLogPath;
            this.errorLogPath = errorLogPath;
            this.logTag = logTag;
            this.timeout = timeout;
            this.eventFlags = eventFlags;
            this.userInterface = userInterface;
            this.noSleep = noSleep;
            this.killOnError = killOnError;
            this.background = background;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ProcessStartInfo startInfo;
        public ProcessStartInfo StartInfo
        {
            get { return startInfo; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Process process;
        public Process Process
        {
            get { return process; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string outputLogPath;
        public string OutputLogPath
        {
            get { return outputLogPath; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorLogPath;
        public string ErrorLogPath
        {
            get { return errorLogPath; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string logTag;
        public string LogTag
        {
            get { return logTag; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int? timeout;
        public int? Timeout
        {
            get { return timeout; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags eventFlags;
        public EventFlags EventFlags
        {
            get { return eventFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool userInterface;
        public bool UserInterface
        {
            get { return userInterface; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noSleep;
        public bool NoSleep
        {
            get { return noSleep; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool killOnError;
        public bool KillOnError
        {
            get { return killOnError; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool background;
        public bool Background
        {
            get { return background; }
        }
        #endregion
    }
}
