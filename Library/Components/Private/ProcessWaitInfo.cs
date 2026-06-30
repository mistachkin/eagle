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
    /// <summary>
    /// This class captures the information needed to start an external process
    /// and then wait for it to exit, including the originating interpreter, the
    /// process and its start information, the output and error log paths, and
    /// the various flags that control the waiting behavior.
    /// </summary>
    [ObjectId("048901bf-df0e-4956-89eb-b4325746a220")]
    internal sealed class ProcessWaitInfo
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class capturing the full set of
        /// parameters describing the process to be started and the manner in
        /// which it should be waited on.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this process wait operation.  This
        /// parameter may be null.
        /// </param>
        /// <param name="startInfo">
        /// The start information used to launch the process.
        /// </param>
        /// <param name="process">
        /// The process being waited on.
        /// </param>
        /// <param name="outputLogPath">
        /// The path of the file to which the standard output of the process is
        /// logged.  This parameter may be null.
        /// </param>
        /// <param name="errorLogPath">
        /// The path of the file to which the standard error of the process is
        /// logged.  This parameter may be null.
        /// </param>
        /// <param name="logTag">
        /// The tag used to identify entries written to the log files.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait for the process to exit,
        /// or null for no timeout.
        /// </param>
        /// <param name="eventFlags">
        /// The flags that control event processing while waiting on the
        /// process.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the waiting should accommodate a user interface (for
        /// example, by processing user interface events).
        /// </param>
        /// <param name="noSleep">
        /// Non-zero to avoid sleeping between checks while waiting on the
        /// process.
        /// </param>
        /// <param name="killOnError">
        /// Non-zero to kill the process if an error is encountered while waiting
        /// on it.
        /// </param>
        /// <param name="background">
        /// Non-zero if the process is being waited on in the background.
        /// </param>
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
        /// <summary>
        /// Stores the interpreter associated with this process wait operation.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter associated with this process wait operation.
        /// </summary>
        public Interpreter Interpreter
        {
            get { return interpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the start information used to launch the process.
        /// </summary>
        private ProcessStartInfo startInfo;
        /// <summary>
        /// Gets the start information used to launch the process.
        /// </summary>
        public ProcessStartInfo StartInfo
        {
            get { return startInfo; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the process being waited on.
        /// </summary>
        private Process process;
        /// <summary>
        /// Gets the process being waited on.
        /// </summary>
        public Process Process
        {
            get { return process; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the path of the file to which the standard output of the
        /// process is logged.
        /// </summary>
        private string outputLogPath;
        /// <summary>
        /// Gets the path of the file to which the standard output of the process
        /// is logged.
        /// </summary>
        public string OutputLogPath
        {
            get { return outputLogPath; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the path of the file to which the standard error of the
        /// process is logged.
        /// </summary>
        private string errorLogPath;
        /// <summary>
        /// Gets the path of the file to which the standard error of the process
        /// is logged.
        /// </summary>
        public string ErrorLogPath
        {
            get { return errorLogPath; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the tag used to identify entries written to the log files.
        /// </summary>
        private string logTag;
        /// <summary>
        /// Gets the tag used to identify entries written to the log files.
        /// </summary>
        public string LogTag
        {
            get { return logTag; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the maximum number of milliseconds to wait for the process to
        /// exit, or null for no timeout.
        /// </summary>
        private int? timeout;
        /// <summary>
        /// Gets the maximum number of milliseconds to wait for the process to
        /// exit, or null for no timeout.
        /// </summary>
        public int? Timeout
        {
            get { return timeout; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags that control event processing while waiting on the
        /// process.
        /// </summary>
        private EventFlags eventFlags;
        /// <summary>
        /// Gets the flags that control event processing while waiting on the
        /// process.
        /// </summary>
        public EventFlags EventFlags
        {
            get { return eventFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the waiting should accommodate a
        /// user interface.
        /// </summary>
        private bool userInterface;
        /// <summary>
        /// Gets a value indicating whether the waiting should accommodate a user
        /// interface.
        /// </summary>
        public bool UserInterface
        {
            get { return userInterface; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether sleeping between checks should be
        /// avoided while waiting on the process.
        /// </summary>
        private bool noSleep;
        /// <summary>
        /// Gets a value indicating whether sleeping between checks should be
        /// avoided while waiting on the process.
        /// </summary>
        public bool NoSleep
        {
            get { return noSleep; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the process should be killed if an
        /// error is encountered while waiting on it.
        /// </summary>
        private bool killOnError;
        /// <summary>
        /// Gets a value indicating whether the process should be killed if an
        /// error is encountered while waiting on it.
        /// </summary>
        public bool KillOnError
        {
            get { return killOnError; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores a value indicating whether the process is being waited on in
        /// the background.
        /// </summary>
        private bool background;
        /// <summary>
        /// Gets a value indicating whether the process is being waited on in the
        /// background.
        /// </summary>
        public bool Background
        {
            get { return background; }
        }
        #endregion
    }
}
