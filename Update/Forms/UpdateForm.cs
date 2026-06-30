/*
 * UpdateForm.cs --
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using Eagle._Components.Private;
using Eagle._Components.Private.Delegates;
using Eagle._Tool;
using _Shared = Eagle._Components.Shared;

namespace Eagle._Forms
{
    /// <summary>
    /// This class implements the main Windows Forms user interface for the
    /// Eagle updater tool.  It downloads release metadata over HTTP(S), locates
    /// the release matching the current configuration, verifies signatures and
    /// hashes, downloads and extracts the release archive, replaces the
    /// installed files, and (optionally) re-launches the updater.  It manages a
    /// background status queue, progress reporting, and several keyboard
    /// shortcuts, and supports a "silent" (and optionally invisible) mode of
    /// operation.  The matching designer-generated partial class supplies the
    /// form controls referenced here.
    /// </summary>
    [Guid("1c3cf092-e060-4423-8f8d-d3fccb110635")]
    internal partial class UpdateForm : Form
    {
        #region Private Constants
        /// <summary>
        /// The default text displayed in the title bar of the updater form.
        /// </summary>
        private const string DefaultFormText = "Eagle Updater";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the Eagle shell executable, launched as an external
        /// process in response to the appropriate keyboard shortcut.
        /// </summary>
        private const string EagleShellCommand = "EagleShell.exe";

        /// <summary>
        /// The file name of the Notepad executable, used to view the configured
        /// log file in response to the appropriate keyboard shortcut.
        /// </summary>
        private const string NotepadCommand = "Notepad.exe";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The category name used when emitting trace and diagnostic messages
        /// from this class; it is the simple name of the type.
        /// </summary>
        private static readonly string TraceCategory = typeof(UpdateForm).Name;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The composite format string used to build the form title bar text
        /// from the base text, version, status text, and administrator marker.
        /// </summary>
        private const string FormTextFormat = "{0}{1}{2}{3}";

        /// <summary>
        /// The composite format string used to build a trace message from its
        /// sequence number, time stamp, and message text.
        /// </summary>
        private const string TraceFormat = "#{0} @ {1}: {2}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The composite format string used to display download progress as the
        /// number of bytes received, the total number of bytes, and the current
        /// transfer rate.
        /// </summary>
        private const string PercentMessage =
            "{0:N0} of {1:N0} bytes, {2:N2} bytes per second";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interval, in milliseconds, between ticks of the status queue
        /// timer used to flush queued status messages to the user interface.
        /// </summary>
        private const int StatusTimerInterval = 200;  /* milliseconds */

        /// <summary>
        /// The interval, in milliseconds, to wait before automatically starting
        /// an update when running in silent mode.
        /// </summary>
        private const int SilentTimerInterval = 5000; /* milliseconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The configuration controlling the behavior of this updater instance,
        /// including the target release, directories, and feature flags.
        /// </summary>
        private Configuration configuration;

        /// <summary>
        /// The assembly being updated (and whose version is reported), as
        /// obtained from the configuration.  This field may be null.
        /// </summary>
        private Assembly assembly;

        /// <summary>
        /// The queue of pending status messages awaiting display in the user
        /// interface; it is drained by the status queue timer.
        /// </summary>
        private Queue<string> statusQueue;

        /// <summary>
        /// The timer that periodically flushes queued status messages from
        /// <see cref="statusQueue" /> to the status label.
        /// </summary>
        private System.Windows.Forms.Timer statusTimer;

        /// <summary>
        /// The one-shot timer used in silent mode to automatically begin the
        /// update after a delay.
        /// </summary>
        private System.Windows.Forms.Timer silentTimer;

        /// <summary>
        /// The time at which the current download started, used to compute the
        /// elapsed time and transfer rate.
        /// </summary>
        private DateTime started;

        /// <summary>
        /// The HTTP user agent string sent with download requests.
        /// </summary>
        private string userAgent;

        /// <summary>
        /// The web client used to perform the asynchronous downloads of the
        /// release data and release files.  This field may be null until the
        /// first download is started.
        /// </summary>
        private UpdateWebClient client;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an updater form, initializing its controls, wiring up the
        /// form, button, and timer event handlers, registering the remote
        /// certificate validation callback, and starting the status queue timer.
        /// </summary>
        private UpdateForm()
        {
            #region Control Setup
            InitializeComponent(); /* throw */

            EnableCancelButton(true);
            EnableUpdateButton(true, true);
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Event Setup
            this.FormClosing += UpdateForm_FormClosing;
            this.Disposed += UpdateForm_Disposed;
            this.KeyUp += UpdateForm_KeyUp;
            this.Shown += UpdateForm_Shown;

            ///////////////////////////////////////////////////////////////////

            btnUpdate.Click += btnUpdate_Click;
            btnCancel.Click += btnCancel_Click;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region HTTPS Setup
            ServicePointManager.ServerCertificateValidationCallback +=
                SecurityOps.RemoteCertificateValidationCallback;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Status Queue & Timer Setup
            statusQueue = new Queue<string>();

            ///////////////////////////////////////////////////////////////////

            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Tick += statusTimer_Tick;
            statusTimer.Interval = StatusTimerInterval;

            ///////////////////////////////////////////////////////////////////

            statusTimer.Start();
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Silent Update Timer Setup
            silentTimer = new System.Windows.Forms.Timer();
            silentTimer.Tick += silentTimer_Tick;
            silentTimer.Interval = SilentTimerInterval;
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an updater form for the specified configuration.  This
        /// constructor delegates to the parameterless constructor, then hooks
        /// the configuration's trace callback, captures its assembly, and
        /// computes the user agent string.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the behavior of this updater instance.
        /// This parameter may be null.
        /// </param>
        private UpdateForm(
            Configuration configuration
            )
            : this()
        {
            if (configuration != null)
            {
                configuration.TraceCallback = QueueTrace;
                assembly = configuration.Assembly;
            }

            this.configuration = configuration;
            this.userAgent = GetUserAgent();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Creates a new updater form for the specified configuration, provided
        /// that configuration is present and valid.
        /// </summary>
        /// <param name="configuration">
        /// The configuration controlling the behavior of the updater instance.
        /// This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives a message describing why the
        /// updater form could not be created.
        /// </param>
        /// <returns>
        /// The newly created updater form, or null if the configuration is
        /// missing or invalid.
        /// </returns>
        public static UpdateForm Create(
            Configuration configuration,
            ref string error
            )
        {
            if ((configuration == null) || !configuration.IsValid)
            {
                error = "Invalid configuration.";
                return null;
            }

            return new UpdateForm(configuration);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        /// <summary>
        /// Builds the HTTP user agent string for download requests, using the
        /// version of the assembly being updated when available and falling
        /// back to the default user agent version otherwise.
        /// </summary>
        /// <returns>
        /// The formatted user agent string.
        /// </returns>
        private string GetUserAgent()
        {
            Version version = Defaults.UserAgentVersion;

            if (assembly != null)
            {
                AssemblyName assemblyName = assembly.GetName();

                if (assemblyName != null)
                {
                    Version assemblyVersion = assemblyName.Version;

                    if (assemblyVersion != null)
                        version = assemblyVersion;
                }
            }

            return String.Format(
                Defaults.UserAgentFormat, Defaults.UserAgentName, version);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the configured log file can be launched in an
        /// external viewer.
        /// </summary>
        /// <returns>
        /// True if a configuration with a non-empty log file name is present;
        /// otherwise, false.
        /// </returns>
        private bool CanLaunchLogFile()
        {
            return (configuration != null) &&
                !String.IsNullOrEmpty(configuration.LogFileName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the Eagle shell can be launched as an external
        /// process from the configured core directory.
        /// </summary>
        /// <returns>
        /// True if a configuration with a non-empty core directory is present;
        /// otherwise, false.
        /// </returns>
        private bool CanLaunchEagleShell()
        {
            return (configuration != null) &&
                !String.IsNullOrEmpty(configuration.CoreDirectory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether an Eagle interactive loop can be started on a
        /// thread hosted within this process.
        /// </summary>
        /// <returns>
        /// True if a configuration is present and its shell feature is enabled;
        /// otherwise, false.
        /// </returns>
        private bool CanStartEagleThread()
        {
            return (configuration != null) && configuration.Shell;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes the updater form on its owning user interface thread,
        /// marshaling the call as necessary.
        /// </summary>
        private void SafeClose()
        {
            FormOps.BeginInvoke(btnUpdate, new DelegateWithNoArgs(delegate()
            {
                Close();
            }), true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables the update button on the user interface thread
        /// and, optionally, queues a corresponding status message.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the update button; zero to disable it.
        /// </param>
        /// <param name="status">
        /// Non-zero to also queue a status message reflecting the new state.
        /// </param>
        private void EnableUpdateButton(
            bool enable,
            bool status
            )
        {
            FormOps.BeginInvoke(btnUpdate, new DelegateWithNoArgs(delegate()
            {
                btnUpdate.Enabled = enable;
            }), true);

            if (status)
                QueueStatus(enable ? "Ready." : "Please wait...");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables the cancel button on the user interface thread
        /// and emits a trace message reflecting the new state.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the cancel button; zero to disable it.
        /// </param>
        private void EnableCancelButton(
            bool enable
            )
        {
            FormOps.BeginInvoke(btnCancel, new DelegateWithNoArgs(delegate()
            {
                btnCancel.Enabled = enable;
            }), true);

            Trace(String.Format("Cancellation {0}.",
                enable ? "enabled" : "disabled"));
        }

        ///////////////////////////////////////////////////////////////////////

        #region Progress Bar Methods
        /// <summary>
        /// Sets the progress bar position and text to reflect the specified
        /// completion fraction, marshaling the update to the user interface
        /// thread.
        /// </summary>
        /// <param name="value">
        /// The completion fraction, between zero and one inclusive, used to set
        /// the progress bar value and its percentage text.
        /// </param>
        private void SetProgressPercent(
            double value
            )
        {
            FormOps.BeginInvoke(prbUpdate, new DelegateWithNoArgs(delegate()
            {
                prbUpdate.Value = (int)(value * prbUpdate.Maximum);
                prbUpdate.Text = String.Format("{0:0.00%}", value);
            }), true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Works around a Windows progress bar animation issue on Windows Vista
        /// and later by momentarily adjusting the progress bar maximum so that
        /// its value jumps immediately to completion rather than animating.
        /// </summary>
        private void ProgressBarHack()
        {
            //
            // HACK: This is required due to the progress bar animation issue
            //       documented on the Microsoft MSDN forums in the post with
            //       the ID "ecf86925-9272-4ba4-b8c9-5a1958bc284a".  Also, see
            //       StackOverflow question ID #2217688.
            //
            if (VersionOps.IsWindowsVistaOrHigher())
            {
                FormOps.BeginInvoke(prbUpdate, new DelegateWithNoArgs(delegate()
                {
                    prbUpdate.Value = prbUpdate.Maximum;
                    prbUpdate.Maximum--;
                    prbUpdate.Maximum++; // BUGFIX: Restore previous maximum.
                }), true);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status Helper Methods
        #region Form Text Methods
        /// <summary>
        /// Builds and sets the title bar text of the updater form from the
        /// supplied version, status text, and administrator marker, marshaling
        /// the update to the user interface thread.
        /// </summary>
        /// <param name="version">
        /// The version to include in the title bar text, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="text">
        /// The additional status text to include in the title bar, if any.  This
        /// parameter may be null or empty.
        /// </param>
        /// <param name="isAdministrator">
        /// Non-zero to append an administrator marker to the title bar text.
        /// </param>
        private void SetFormText(
            Version version,
            string text,
            bool isAdministrator
            )
        {
            text = String.Format(FormTextFormat,
                DefaultFormText, (version != null) ?
                    String.Format(" v{0}", version) : String.Empty,
                !String.IsNullOrEmpty(text) ?
                    String.Format(" - {0}", text) : String.Empty,
                isAdministrator ?
                    " (Administrator)" : String.Empty);

            FormOps.BeginInvoke(this, new DelegateWithNoArgs(delegate()
            {
                this.Text = text;
            }), true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Support Methods
        /// <summary>
        /// Emits a trace message describing the specified exception, using the
        /// trace category for this class.
        /// </summary>
        /// <param name="exception">
        /// The exception to trace.  This parameter may be null.
        /// </param>
        private static void Trace(
            Exception exception
            )
        {
            TraceOps.Trace(null, exception, TraceCategory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Emits a trace message, prefixed with a sequence number and time
        /// stamp, using the trace category for this class.  Empty messages are
        /// ignored.
        /// </summary>
        /// <param name="message">
        /// The message text to trace.  This parameter may be null or empty.
        /// </param>
        private static void Trace(
            string message
            )
        {
            if (!String.IsNullOrEmpty(message))
            {
                TraceOps.Trace(null, String.Format(
                        TraceFormat, TraceOps.NextId(),
                        TraceOps.TimeStamp(TraceOps.GetNow()),
                        message),
                    TraceCategory);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status URI Methods
        /// <summary>
        /// Builds the default text for the URI status label, describing the
        /// configured target core file along with its release and build types.
        /// </summary>
        /// <returns>
        /// The default URI status text, or null if the configuration is missing
        /// or invalid.
        /// </returns>
        private string GetDefaultStatusUri()
        {
            if ((configuration == null) || !configuration.IsValid)
                return null;

            return String.Format(
                "Target: \"{0}\" ({1} {2})", configuration.CoreFileName,
                configuration.ReleaseType, configuration.BuildType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Traces and displays the specified message in the URI status label,
        /// marshaling the update to the user interface thread.
        /// </summary>
        /// <param name="message">
        /// The message to trace and display in the URI status label.  This
        /// parameter may be null.
        /// </param>
        private void UpdateStatusUri(
            string message
            )
        {
            Trace(message);

            FormOps.BeginInvoke(lblUri, new DelegateWithNoArgs(delegate()
            {
                lblUri.Text = message;
            }), true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status Percent Methods
        /// <summary>
        /// Resets the elapsed time baseline to the current time, used to measure
        /// the duration and transfer rate of a download.
        /// </summary>
        private void ResetElapsedTime()
        {
            started = TraceOps.GetNow();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes the time elapsed since the last call to
        /// <see cref="ResetElapsedTime" />.
        /// </summary>
        /// <returns>
        /// The interval between the current time and the elapsed time baseline.
        /// </returns>
        private TimeSpan GetElapsedTime()
        {
            DateTime now = TraceOps.GetNow();

            return now.Subtract(started);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Displays the specified message in the percent status label,
        /// marshaling the update to the user interface thread.
        /// </summary>
        /// <param name="message">
        /// The message to display in the percent status label.  This parameter
        /// may be null.
        /// </param>
        private void UpdateStatusPercent(
            string message
            )
        {
            FormOps.BeginInvoke(lblPercent, new DelegateWithNoArgs(delegate()
            {
                lblPercent.Text = message;
            }), true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region General Status Methods
        /// <summary>
        /// Resets the selected status displays to their default (empty) state.
        /// </summary>
        /// <param name="message">
        /// Non-zero to clear the general status message.
        /// </param>
        /// <param name="percent">
        /// Non-zero to reset the progress bar and clear the percent status.
        /// </param>
        /// <param name="uri">
        /// Non-zero to reset the URI status label to its default text.
        /// </param>
        private void ResetStatus(
            bool message,
            bool percent,
            bool uri
            )
        {
            if (message)
                UpdateStatus(null);

            if (percent)
            {
                SetProgressPercent(0);
                UpdateStatusPercent(null);
            }

            if (uri)
                UpdateStatusUri(GetDefaultStatusUri());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Displays the specified message in the general status label,
        /// marshaling the update to the user interface thread.
        /// </summary>
        /// <param name="message">
        /// The message to display in the general status label.  This parameter
        /// may be null.
        /// </param>
        private void UpdateStatus(
            string message
            )
        {
            FormOps.BeginInvoke(lblUpdate, new DelegateWithNoArgs(delegate()
            {
                lblUpdate.Text = message;
            }), true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Traces the specified message and also displays it in the general
        /// status label.
        /// </summary>
        /// <param name="message">
        /// The message to trace and display.  This parameter may be null.
        /// </param>
        private void TraceAndUpdateStatus(
            string message
            )
        {
            Trace(message);
            UpdateStatus(message);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status Queue Methods
        /// <summary>
        /// Serves as the trace callback for the configuration, forwarding the
        /// message to the status queue as a trace-only message.
        /// </summary>
        /// <param name="message">
        /// The message to trace.  This parameter may be null.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.  This parameter is
        /// not used.
        /// </param>
        private void QueueTrace(
            string message,
            string category /* NOT USED */
            )
        {
            QueueStatus(message, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Traces the specified message and enqueues it for display in the
        /// general status label.
        /// </summary>
        /// <param name="message">
        /// The message to trace and enqueue.  This parameter may be null.
        /// </param>
        private void QueueStatus(
            string message
            )
        {
            QueueStatus(message, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Traces the specified message and, unless requested to trace only,
        /// enqueues it for display in the general status label.
        /// </summary>
        /// <param name="message">
        /// The message to trace and (optionally) enqueue.  This parameter may
        /// be null.
        /// </param>
        /// <param name="traceOnly">
        /// Non-zero to trace the message only, without enqueuing it for display.
        /// </param>
        private void QueueStatus(
            string message,
            bool traceOnly
            )
        {
            Trace(message);

            if (!traceOnly)
            {
                Queue<string> queue = statusQueue;

                if (queue != null)
                {
                    lock (queue)
                    {
                        queue.Enqueue(message);
                    }
                }
            }
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Form Event Methods
        /// <summary>
        /// Handles the form closing event by stopping the status queue timer.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the form closing event.
        /// </param>
        private void UpdateForm_FormClosing(
            object sender,
            FormClosingEventArgs e
            )
        {
            if (statusTimer != null)
                statusTimer.Stop();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the form disposed event by disposing of the status queue
        /// timer and the web client, if any.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the disposed event.
        /// </param>
        private void UpdateForm_Disposed(
            object sender,
            EventArgs e
            )
        {
            if (statusTimer != null)
            {
                statusTimer.Dispose();
                statusTimer = null;
            }

            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the form key up event, implementing the updater keyboard
        /// shortcuts: F1 displays the help message; Ctrl-A shows the license
        /// text; Ctrl-E launches the external Eagle shell; Ctrl-L launches the
        /// log file in Notepad; Ctrl-R resets to the default core directory;
        /// Ctrl-T selects a core directory; and Ctrl-F2 starts an internal Eagle
        /// interactive loop thread.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the key up event.  This parameter may be
        /// null.
        /// </param>
        private void UpdateForm_KeyUp(
            object sender,
            KeyEventArgs e
            )
        {
            if (e == null)
                return;

            //
            // NOTE: *SHORTCUT* F1, display command help message.
            //
            if (!e.Shift && !e.Control && !e.Alt && (e.KeyCode == Keys.F1))
            {
                e.SuppressKeyPress = true;

                TraceOps.ShowMessage(
                    configuration, assembly, String.Format(
                    "F1: Shows this help message.{0}" +
                    "Ctrl-A: Shows the license text.{0}" +
                    (CanLaunchEagleShell() ?
                        "Ctrl-E: Launches the configured Eagle Shell.{0}" :
                        String.Empty) +
                    (CanLaunchLogFile() ?
                        "Ctrl-L: Launches the log file in Notepad.{0}" :
                        String.Empty) +
                    "Ctrl-R: Resets to the default core directory.{0}" +
                    "Ctrl-T: Sets the selected core directory.{0}" +
                    (CanStartEagleThread() ?
                        "Ctrl-F2: Starts an Eagle interactive loop thread.{0}" :
                        String.Empty),
                    Environment.NewLine), TraceCategory,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //
            // NOTE: *SHORTCUT* CTRL-A, show the license text.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.A))
            {
                e.SuppressKeyPress = true;

                string licenseText;

#if OFFICIAL_BINARY
                licenseText = _Shared.BinaryLicense.Text;
#else
                licenseText = _Shared.SourceLicense.Text;
#endif

                TraceOps.ShowMessage(
                    configuration, assembly, String.Format(
                    "{0}{0}{1}{0}", Environment.NewLine,
                    licenseText), TraceCategory,
                    MessageBoxButtons.OK, MessageBoxIcon.None);
            }

            //
            // NOTE: *SHORTCUT* CTRL-E, launch the configured Eagle shell
            //       as an external process.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.E))
            {
                e.SuppressKeyPress = true;

                //
                // NOTE: Launch Notepad (or some other text editor?) with
                //       the configured log file name.
                //
                if (CanLaunchEagleShell())
                {
                    try
                    {
                        Process.Start(
                            Path.Combine(configuration.CoreDirectory,
                            EagleShellCommand)); /* throw */
                    }
                    catch (Exception ex)
                    {
                        Trace(ex);

                        Program.Fail(configuration, null,
                            "Caught exception launching external shell.",
                            TraceCategory);
                    }
                }
            }

            //
            // NOTE: *SHORTCUT* CTRL-L, launch Notepad with the log file.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.L))
            {
                e.SuppressKeyPress = true;

                //
                // NOTE: Launch Notepad (or some other text editor?) with
                //       the configured log file name.
                //
                if (CanLaunchLogFile())
                {
                    try
                    {
                        Process.Start(
                            NotepadCommand, String.Format("\"{0}\"",
                            configuration.LogFileName)); /* throw */
                    }
                    catch (Exception ex)
                    {
                        Trace(ex);

                        Program.Fail(configuration, null,
                            "Caught exception launching log file viewer.",
                            TraceCategory);
                    }
                }
            }

            //
            // NOTE: *SHORTCUT* CTRL-R, resets the core directory for the
            //       update to the default.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.R))
            {
                e.SuppressKeyPress = true;

                if (configuration != null)
                {
                    try
                    {
                        QueueStatus("User reset core directory.", true);

                        configuration.ResetReleaseTypeAndCoreDirectory();
                        configuration.Dump();

                        ResetStatus(true, true, true);
                    }
                    catch (Exception ex)
                    {
                        Trace(ex);

                        Program.Fail(configuration, null,
                            "Caught exception resetting core directory.",
                            TraceCategory);
                    }
                }
            }

            //
            // NOTE: *SHORTCUT* CTRL-T, resets the core directory for the
            //       update to one selected by the user.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.T))
            {
                e.SuppressKeyPress = true;

                if (configuration != null)
                {
                    try
                    {
                        using (FolderBrowserDialog dialog =
                                new FolderBrowserDialog())
                        {
                            dialog.ShowNewFolderButton = false;

                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                string directory = dialog.SelectedPath;

                                QueueStatus(String.Format(
                                    "User selected core directory \"{0}\".",
                                    directory), true);

                                configuration.ResetReleaseType();
                                configuration.SetCoreDirectory(directory);
                                configuration.Dump();

                                ResetStatus(true, true, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace(ex);

                        Program.Fail(configuration, null,
                            "Caught exception setting core directory.",
                            TraceCategory);
                    }
                }
            }

            //
            // NOTE: *SHORTCUT* CTRL-F2, launch the Eagle interactive loop
            //       in this process if we have been configured to allow it.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.F2))
            {
                e.SuppressKeyPress = true;

                //
                // NOTE: Launch the Eagle Shell (hosted in this process).
                //
                // WARNING: This will prevent the updater from actually
                //          replacing any in-use files.
                //
                if (CanStartEagleThread())
                {
#if NATIVE && WINDOWS
                    string error = null;

                    if (ConsoleEx.TryOpen(ref error))
#endif
                    {
                        try
                        {
                            //
                            // NOTE: Since the Eagle interactive loop would
                            //       block this thread, create a new thread
                            //       to run it.
                            //
                            Thread thread = new Thread(delegate()
                            {
                                try
                                {
                                    int exitCode = ShellOps.ShellMain(
                                        configuration.ShellArgs);

                                    QueueStatus(String.Format(
                                        "Shell thread returned code {0}.",
                                        exitCode), true);
                                }
                                catch (Exception ex)
                                {
                                    Trace(ex);

                                    Program.Fail(configuration, null,
                                        "Caught exception from shell.",
                                        TraceCategory);
                                }
                            });

                            thread.SetApartmentState(ApartmentState.STA);
                            thread.Start();
                        }
                        catch (Exception ex)
                        {
                            Trace(ex);

                            Program.Fail(configuration, null,
                                "Caught exception starting internal shell.",
                                TraceCategory);
                        }
                    }
#if NATIVE && WINDOWS
                    else
                    {
                        Trace(error);

                        TraceOps.ShowMessage(
                            configuration, assembly,
                            String.Format("Cannot open console: {0}", error),
                            TraceCategory, MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
#endif
                }
                else
                {
                    TraceOps.ShowMessage(
                        configuration, assembly,
                        "Invalid configuration or internal shell not enabled.",
                        TraceCategory, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the form shown event by setting the title bar text, tracing
        /// diagnostic information about the environment and updater, resetting
        /// the status displays, and (when configured for silent mode) hiding the
        /// form as needed and starting the silent update timer.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the shown event.
        /// </param>
        private void UpdateForm_Shown(
            object sender,
            EventArgs e
            )
        {
            if ((configuration != null) && configuration.IsValid)
            {
                Version version = VersionOps.GetAssemblyVersion(assembly);

                bool isAdministrator = SecurityOps.IsAdministrator();

                SetFormText(
                    version, configuration.SubjectName, isAdministrator);

                Trace(String.Format("Running in process {0} and thread {1}.",
                    ShellOps.GetProcessId(), AppDomain.GetCurrentThreadId()));

                Trace(String.Format("Operating system is \"{0}\".",
                    Environment.OSVersion));

                Trace(String.Format("Runtime is \"{0}\" version \"{1}\".",
                    VersionOps.GetRuntimeName(), Environment.Version));

                Trace(String.Format("Updater version is \"{0}\".",
                    version));

                Trace(String.Format("{0} {1} is \"{2}\\{3}\".",
                    Environment.UserInteractive ?
                        "Interactive" : "Non-interactive",
                    isAdministrator ? "administrator" : "user",
                    Environment.UserDomainName, Environment.UserName));

                Trace(String.Format("Form location is {0}, form size is {1}.",
                    this.Location, this.Size));

                Trace(String.Format("Internal Eagle Shell appears to be {0}.",
                    CanStartEagleThread() ? "available" : "unavailable"));

                Trace(String.Format("External Eagle Shell appears to be {0}.",
                    CanLaunchEagleShell() ? "available" : "unavailable"));

                Trace(String.Format(
                    "External log file viewer appears to be {0}.",
                    CanLaunchLogFile() ? "available" : "unavailable"));

                ResetStatus(true, true, true);

                if (configuration.Silent && (silentTimer != null))
                {
                    QueueStatus("Silent mode activated.");
                    EnableUpdateButton(false, true);

                    if (configuration.Invisible)
                    {
                        QueueStatus("Invisible mode activated.");
                        Hide();
                    }
                    else
                    {
                        Refresh();
                    }

                    QueueStatus(String.Format(
                        "Silent update will start in {0} milliseconds...",
                        SilentTimerInterval));

                    silentTimer.Start();
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Timer Event Methods
        #region Status Queue Timer Event Methods
        /// <summary>
        /// Handles the status queue timer tick by dequeuing a single pending
        /// status message, if any, and displaying it in the general status
        /// label.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the timer tick event.
        /// </param>
        private void statusTimer_Tick(
            object sender,
            EventArgs e
            )
        {
            Queue<string> queue = statusQueue;

            if (queue != null)
            {
                lock (queue)
                {
                    if (queue.Count > 0)
                        UpdateStatus(queue.Dequeue());
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Silent Update Timer Event Methods
        /// <summary>
        /// Handles the one-shot silent update timer tick by stopping the timer
        /// and starting the update as though the update button had been clicked.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the timer tick event.
        /// </param>
        private void silentTimer_Tick(
            object sender,
            EventArgs e
            )
        {
            if (silentTimer != null)
                silentTimer.Stop(); /* ONE-SHOT */

            btnUpdate_Click(sender, e);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Button Event Methods
        /// <summary>
        /// Handles the cancel button click by canceling the pending silent
        /// update timer and any in-progress download; if there is nothing to
        /// cancel, it closes the updater form instead.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the click event.
        /// </param>
        private void btnCancel_Click(
            object sender,
            EventArgs e
            )
        {
            bool canceled = false; /* NOTE: Did we cancel something yet? */

            ///////////////////////////////////////////////////////////////////

            #region Cancel Silent Update Timer
            if ((silentTimer != null) && silentTimer.Enabled)
            {
                silentTimer.Stop();
                canceled = true;

                QueueStatus("Silent update canceled.");
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Cancel Pending Download
            if ((client != null) && client.IsBusy)
            {
                client.CancelAsync();
                canceled = true;

                QueueStatus("Download canceled.");
            }
            else if (canceled)
            {
                //
                // NOTE: At this point, we know the silent update timer has
                //       been stopped and there was no download in progress;
                //       therefore, re-enable the update button.
                //
                EnableUpdateButton(true, false);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Close Updater (Maybe)
            if (!canceled)
            {
                Close();
                canceled = true; /* NOTE: Redundant. */

                Trace("User closed."); /* NOTE: No status queue available. */
            }
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the update button click by creating the web client (if
        /// necessary), building the release data URI from the configuration, and
        /// starting the asynchronous download of the release metadata.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data associated with the click event.
        /// </param>
        private void btnUpdate_Click(
            object sender,
            EventArgs e
            )
        {
            bool downloading = false;

            EnableUpdateButton(false, true);

            try
            {
                if ((configuration != null) && configuration.IsValid)
                {
                    if (client == null)
                    {
                        client = new UpdateWebClient(userAgent);

                        Trace(String.Format("Created web client \"{0}\" " +
                            "with user agent \"{1}\".", client.GetType(),
                            client.UserAgent));

                        client.DownloadProgressChanged +=
                                client_DownloadProgressChanged;

                        client.DownloadDataCompleted +=
                                client_DownloadDataCompleted;

                        client.DownloadFileCompleted +=
                                client_DownloadFileCompleted;
                    }

                    Uri uri;

                    if (Uri.TryCreate(configuration.BaseUri,
                            configuration.GetPathAndQuery(), out uri))
                    {
                        ResetStatus(false, true, false);

                        TraceAndUpdateStatus("Downloading release data...");
                        UpdateStatusUri(uri.ToString());

                        ResetElapsedTime();

                        client.DownloadDataAsync(uri);
                        downloading = true;

                        EnableCancelButton(true);
                    }
                }
                else
                {
                    QueueStatus("Cannot update, invalid configuration.");
                }
            }
            finally
            {
                //
                // NOTE: If we did not get to the point where we started the
                //       actual data download, re-enable the update button.
                //
                if (!downloading)
                    EnableUpdateButton(true, true);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WebClient Event Methods
        /// <summary>
        /// Handles the web client download progress changed event by updating
        /// the progress bar and the percent status with the bytes received, the
        /// total bytes, and the current transfer rate.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the download progress.  This parameter may be
        /// null.
        /// </param>
        private void client_DownloadProgressChanged(
            object sender,
            DownloadProgressChangedEventArgs e
            )
        {
            if (e == null)
                return;

            double value = 0;

            if (e.TotalBytesToReceive > 0)
            {
                value = (double)
                    e.BytesReceived / e.TotalBytesToReceive;
            }

            if (value < 0.0)
                value = 0.0;
            else if (value > 1.0)
                value = 1.0;

            SetProgressPercent(value);

            TimeSpan elapsed = GetElapsedTime();

            long kilobytesPerSecond = 0;

            if (elapsed.TotalSeconds > 0)
            {
                kilobytesPerSecond = (long)
                    (e.BytesReceived / elapsed.TotalSeconds);
            }

            UpdateStatusPercent(String.Format(
                PercentMessage, e.BytesReceived, e.TotalBytesToReceive,
                kilobytesPerSecond));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the web client download data completed event by parsing the
        /// downloaded release metadata on a background thread, locating the
        /// release matching the configuration, confirming the update with the
        /// user when required, and starting the asynchronous download of the
        /// matching release file.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the completed data download, including the
        /// downloaded bytes or any error.  This parameter may be null.
        /// </param>
        private void client_DownloadDataCompleted(
            object sender,
            DownloadDataCompletedEventArgs e
            )
        {
            if (e == null)
                return;

            new Thread(delegate()
            {
                try
                {
                    EnableCancelButton(false);

                    if (e.Cancelled)
                    {
                        QueueStatus("Data download canceled.");
                        goto done;
                    }

                    Exception exception = e.Error;

                    if (exception != null)
                    {
                        Trace(exception);
                        QueueStatus("Data download failed.");
                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    ProgressBarHack();
                    QueueStatus("Data download completed.");

                    QueueStatus(String.Format("Total elapsed time was {0}.",
                        GetElapsedTime()), true);

                    QueueStatus(lblPercent.Text, true);

                    ///////////////////////////////////////////////////////////

                    if ((configuration == null) || !configuration.IsValid)
                    {
                        QueueStatus("Invalid configuration.");
                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    byte[] result = e.Result;

                    if (result == null)
                    {
                        QueueStatus("No data was downloaded.");
                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    string text = Defaults.Encoding.GetString(result);

                    //
                    // NOTE: Figure out the number of raw bytes and characters
                    //       that we received.  These numbers should always be
                    //       the same as we should not be seeing any Unicode
                    //       code points in the response data that need to be
                    //       encoded using more than one byte in UTF8 (i.e. the
                    //       default encoding for this application).
                    //
                    int[] length = {
                        result.Length, (text != null) ? text.Length : 0
                    };

                    QueueStatus(String.Format(
                        "Raw downloaded data ({0} bytes, {1} characters, " +
                        "\"{2}\" encoding):{3}{3}{4}{3}", length[0], length[1],
                        Defaults.Encoding.WebName, Environment.NewLine,
                        FormatOps.RawDataToString(text, true)), true);

                    if (String.IsNullOrEmpty(text))
                    {
                        QueueStatus("Release data is invalid.");
                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    IEqualityComparer<Configuration> comparer = null;
                    IDictionary<Configuration, Release> releases = null;
                    int[] protocolCounts = null;
                    string error = null;

                    if (!Release.ParseData(
                            configuration, text, ref comparer, ref releases,
                            ref protocolCounts, ref error))
                    {
                        QueueStatus("Failed to parse release data.");
                        QueueStatus(error, true);

                        goto done;
                    }

                    QueueStatus(String.Format(
                        "Parsed release data, found {0} release(s), {1} " +
                        "build item(s), {2} script item(s), {3} self " +
                        "item(s), {4} plugin item(s), and {5} other item(s).",
                        releases.Count,
                        ((protocolCounts != null) &&
                            (protocolCounts.Length >= 1)) ?
                                protocolCounts[0] : 0,
                        ((protocolCounts != null) &&
                            (protocolCounts.Length >= 2)) ?
                                protocolCounts[1] : 0,
                        ((protocolCounts != null) &&
                            (protocolCounts.Length >= 3)) ?
                                protocolCounts[2] : 0,
                        ((protocolCounts != null) &&
                            (protocolCounts.Length >= 4)) ?
                                protocolCounts[3] : 0,
                        ((protocolCounts != null) &&
                            (protocolCounts.Length >= 5)) ?
                                protocolCounts[4] : 0));

                    ///////////////////////////////////////////////////////////

                    QueueStatus(String.Format(
                        "Checking for release matching protocol Id " +
                        "\"{0}\", public key token \"{1}\", name \"{2}\", " +
                        "and culture \"{3}\"...", configuration.ProtocolId,
                        FormatOps.ToHexString(configuration.PublicKeyToken),
                        configuration.Name, FormatOps.CultureToString(
                            configuration.Culture)), true);

                    Release release = Release.FindSelf(
                        configuration, releases, false, false, false);

                    if (release == null)
                    {
                        release = Release.Find(
                            configuration, releases, false, false, false);
                    }

                    if (release == null)
                    {
                        QueueStatus("No release matching configuration.");
                        goto done;
                    }

                    int releaseId = release.Id;

                    QueueStatus(String.Format(
                        "Found release #{0} matching configuration.",
                        releaseId));

                    release.Dump();

                    if (!release.IsValid)
                    {
                        QueueStatus(String.Format(
                            "Release #{0} is invalid.", releaseId));

                        goto done;
                    }

                    QueueStatus(String.Format(
                        "Release #{0} is valid.", releaseId));

                    if (!release.IsGreater)
                    {
                        QueueStatus(String.Format(
                            "Release #{0} is not newer.", releaseId));

                        goto done;
                    }

                    //
                    // NOTE: Everything checks out, prepare to start the file
                    //       download.
                    //
                    QueueStatus(String.Format(
                        "Release #{0} is newer.", releaseId));

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: Check if the release is for the updater itself.
                    //
                    if (release.IsSelf)
                    {
                        QueueStatus(String.Format(
                            "Release #{0} is for the updater itself.",
                            releaseId));
                    }

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: If we need to prompt for confirmation to actually
                    //       perform the update, do that now.
                    //
                    if (configuration.Confirm)
                    {
                        string message = String.Format(
                            "An updated release, {0}, is available." +
                            "{1}{1}Do you wish to proceed?", release,
                            Environment.NewLine);

                        if (TraceOps.ShowMessage(
                                configuration, assembly, message,
                                TraceCategory, MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) != DialogResult.Yes)
                        {
                            QueueStatus(
                                "Update not confirmed at user request.");

                            goto done;
                        }
                    }

                    //
                    // NOTE: Check for and display any release notes.  Also,
                    //       if there are any release notes, prompt the user
                    //       to continue with the update.
                    //
                    string notes = release.Notes;

                    if (!String.IsNullOrEmpty(notes))
                    {
                        if (TraceOps.ShowMessage(
                                configuration, assembly, notes,
                                TraceCategory, MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                DialogResult.Yes) != DialogResult.Yes)
                        {
                            QueueStatus(
                                "Update canceled via notes at user request.");

                            goto done;
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    Uri uri = release.CreateUri(
                        configuration.BuildType, configuration.ReleaseType);

                    QueueStatus(String.Format(
                        "Remote URI to download is \"{0}\".", uri), true);

                    string downloadDirectory = Path.Combine(
                        Path.GetTempPath(),
                        Guid.NewGuid().ToString());

                    QueueStatus(String.Format(
                        "Local download directory is \"{0}\".",
                        downloadDirectory), true);

                    ///////////////////////////////////////////////////////////

                    try
                    {
                        Directory.CreateDirectory(
                            downloadDirectory); /* throw */

                        QueueStatus(
                            "Local download directory created.", true);
                    }
                    catch (Exception ex)
                    {
                        Trace(ex);

                        QueueStatus(
                            "Failed to create local download directory.");

                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    //
                    // HACK: For now, always assume the last segment is the
                    //       actual file name.
                    //
                    string fileName = Path.Combine(downloadDirectory,
                        uri.Segments[uri.Segments.Length - 1]);

                    QueueStatus(String.Format(
                        "Local download file is \"{0}\".", fileName), true);

                    EnableUpdateButton(false, true);

                    TraceAndUpdateStatus(String.Format(
                        "Downloading release #{0} file...", releaseId));

                    UpdateStatusUri(uri.ToString());

                    ResetElapsedTime();

                    client.DownloadFileAsync(uri, fileName,
                        new AnyPair<Release, string>(release, fileName));

                    EnableCancelButton(true);
                    return;

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: This label is reached only upon failure.
                    //
                done:

                    ResetStatus(false, true, true);

                    EnableCancelButton(true);
                    EnableUpdateButton(true, false);
                }
                catch (Exception ex)
                {
                    Trace(ex);

                    Program.Fail(configuration, null,
                        "Caught exception in data download handler.",
                        TraceCategory);
                }
            }).Start();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the web client download file completed event by, on a
        /// background thread, verifying the signature and hashes of the
        /// downloaded release file, extracting it, replacing the installed files
        /// in the target directory, cleaning up the temporary directories, and
        /// (optionally) re-launching the updater or closing in silent mode.  If
        /// the release is for the updater itself, the downloaded file replaces
        /// the updater assembly and the updater is re-executed.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the completed file download, including the user
        /// state and any error.  This parameter may be null.
        /// </param>
        private void client_DownloadFileCompleted(
            object sender,
            AsyncCompletedEventArgs e
            )
        {
            if (e == null)
                return;

            new Thread(delegate()
            {
                try
                {
                    bool reCheck = false;
                    bool silent = false;

                    EnableCancelButton(false);

                    if (e.Cancelled)
                    {
                        QueueStatus("File download canceled.");
                        goto done;
                    }

                    Exception exception = e.Error;

                    if (exception != null)
                    {
                        Trace(exception);
                        QueueStatus("File download failed.");
                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    ProgressBarHack();
                    QueueStatus("File download completed.");

                    QueueStatus(String.Format("Total elapsed time was {0}.",
                        GetElapsedTime()), true);

                    QueueStatus(lblPercent.Text, true);

                    ///////////////////////////////////////////////////////////

                    if ((configuration == null) || !configuration.IsValid)
                    {
                        QueueStatus("Invalid configuration.");
                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: Next, set the value of the 'silent' flag from
                    //       the configuration.
                    //
                    silent = configuration.Silent;

                    ///////////////////////////////////////////////////////////

                    AnyPair<Release, string> anyPair =
                        e.UserState as AnyPair<Release, string>;

                    if (anyPair == null)
                    {
                        QueueStatus("State handle is invalid.");
                        goto done;
                    }

                    Release release = anyPair.X;

                    if (release == null)
                    {
                        QueueStatus(
                            "State handle contains an invalid release.");

                        goto done;
                    }

                    string releaseFileName = anyPair.Y;

                    if (String.IsNullOrEmpty(releaseFileName))
                    {
                        QueueStatus(
                            "State handle contains an invalid file name.");

                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    string releaseDirectory = Path.GetDirectoryName(
                        releaseFileName);

                    QueueStatus(String.Format(
                        "Release directory is \"{0}\".", releaseDirectory),
                        true);

                    string releaseFileNameOnly = Path.GetFileName(
                        releaseFileName);

                    QueueStatus(String.Format(
                        "Checking signature on release file \"{0}\"...",
                        releaseFileNameOnly));

                    if (configuration.SubjectName != null)
                        QueueStatus(String.Format(
                            "Certificate subject must match \"{0}\".",
                            configuration.SubjectName), true);

                    ///////////////////////////////////////////////////////////

                    string error = null;

                    if (configuration.HasFlags(SignatureFlags.Release, true))
                    {
                        X509Certificate2 releaseCertificate2 = null;

                        if (!configuration.VerifyFileCertificate(
                                releaseFileName, false,
                                !configuration.Invisible,
                                ref releaseCertificate2, ref error))
                        {
                            QueueStatus(error, true);

                            QueueStatus(
                                "Signature on release file is missing, " +
                                "invalid, or untrusted.");

                            goto done;
                        }

                        QueueStatus(String.Format(
                            "Signature on release file verified, signed " +
                            "by \"{0}\".", FormatOps.CertificateToString(
                            releaseCertificate2, false)));
                    }

                    ///////////////////////////////////////////////////////////

                    if (release.IsSelf)
                    {
                        if (FileOps.Copy(
                                configuration, releaseFileName,
                                assembly.Location, true, true, ref error))
                        {
                            try
                            {
                                QueueStatus(
                                    "Copied release file to assembly " +
                                    "location, attempting to re-execute " +
                                    "updater...");

                                Configuration.StartAsSelf(assembly,
                                    true); /* throw */

                                SafeClose();
                                return;
                            }
                            catch (Exception ex)
                            {
                                Trace(ex);

                                QueueStatus("Failed to re-execute updater.");
                            }
                        }
                        else
                        {
                            QueueStatus(error, true);

                            QueueStatus("Could not copy release file to " +
                                "updater assembly location.");
                        }

                        goto done;
                    }

                    ///////////////////////////////////////////////////////////

                    string extractDirectory = Path.Combine(
                        Path.GetTempPath(), Guid.NewGuid().ToString());

                    QueueStatus(String.Format(
                        "Extract directory is \"{0}\".", extractDirectory),
                        true);

                    string extractCommand = String.Format(
                        configuration.CommandFormat, releaseFileName,
                        extractDirectory);

                    string extractArguments = String.Format(
                        configuration.ArgumentFormat, extractDirectory,
                        releaseFileName);

                    QueueStatus(String.Format(
                        "Executing command \"{0}\" with arguments: {1}",
                        extractCommand, extractArguments), true);

                    Process extractProcess = Process.Start(
                        extractCommand, extractArguments); /* throw */

                    QueueStatus(String.Format(
                        "New process Id is {0}, waiting forever for exit...",
                        extractProcess.Id), true);

                    extractProcess.WaitForExit();

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: Verify hashes for the new core file name.
                    //
                    string coreFileNameOnly = Path.GetFileName(
                        configuration.CoreFileName);

                    string newCoreFileName = FileOps.GetFirstName(
                        configuration, extractDirectory, null,
                        coreFileNameOnly, true, false, true, false);

                    if (newCoreFileName == null)
                    {
                        QueueStatus(String.Format(
                            "Core file \"{0}\" not found in extract directory.",
                            coreFileNameOnly));

                        goto done;
                    }

                    QueueStatus(String.Format(
                        "Found new core file name \"{0}\" in extract directory.",
                        newCoreFileName), true);

                    ///////////////////////////////////////////////////////////

                    if (configuration.HasFlags(SignatureFlags.Core, true))
                    {
                        X509Certificate2 newCoreCertificate2 = null;

                        if (!configuration.VerifyFileCertificate(
                                newCoreFileName, false, false,
                                ref newCoreCertificate2, ref error))
                        {
                            QueueStatus(error, true);

                            QueueStatus(
                                "New core file signature is missing, " +
                                "invalid, or untrusted.");

                            goto done;
                        }

                        QueueStatus(String.Format(
                            "New core file signature verified, signed by " +
                            "\"{0}\".", FormatOps.CertificateToString(
                            newCoreCertificate2, false)));
                    }

                    ///////////////////////////////////////////////////////////

                    bool strongName = configuration.CoreIsAssembly &&
                        configuration.HasFlags(StrongNameExFlags.Core, true);

                    if (!release.VerifyFile(
                            configuration, newCoreFileName, strongName))
                    {
                        QueueStatus("New core file could not be verified.");
                        goto done;
                    }

                    QueueStatus("New core file verified.");

                    ///////////////////////////////////////////////////////////

                    string newCoreDirectory = Path.GetDirectoryName(
                        newCoreFileName);

                    string directoryOffset =
                        newCoreDirectory.Substring(extractDirectory.Length);

                    QueueStatus(String.Format(
                        "Offset from extract directory (at index {0}) is " +
                        "\"{1}\".", extractDirectory.Length, directoryOffset),
                        true);

                    if (String.IsNullOrEmpty(directoryOffset))
                    {
                        QueueStatus(
                            "Offset from extract directory is unusable.");

                        goto done;
                    }

                    string baseDirectoryOffset = FileOps.GetBasePathFromOffset(
                        directoryOffset);

                    QueueStatus(String.Format(
                        "Base offset from extract directory is \"{0}\".",
                        baseDirectoryOffset), true);

                    if (String.IsNullOrEmpty(baseDirectoryOffset))
                    {
                        QueueStatus(
                            "Base offset from extract directory is unusable.");

                        goto done;
                    }

                    string coreDirectory = configuration.CoreDirectory;

                    //
                    // NOTE: Make sure the target directory structure conforms
                    //       [at least minimally] to our expectations.
                    //
                    if (!FileOps.MatchSuffix(coreDirectory, directoryOffset))
                    {
                        QueueStatus(
                            "Offset from extract directory does not match " +
                            "core directory.");

                        goto done;
                    }

                    string targetDirectory = coreDirectory.Substring(0,
                        coreDirectory.Length - directoryOffset.Length);

                    //
                    // BUGFIX: If the target directory was actually the root of
                    //         the volume, make sure it ends with a backslash
                    //         (i.e. in order to prevent the current directory
                    //         from being used as the basis for querying lists
                    //         of files).
                    //
                    targetDirectory = FileOps.CannotBeDriveLetterAndColon(
                        targetDirectory);

                    QueueStatus(String.Format(
                        "Target directory is \"{0}\".", targetDirectory),
                        true);

                    ///////////////////////////////////////////////////////////

                    if (!Configuration.DeleteInUse(configuration, ref error))
                        QueueStatus(error, true);

                    ///////////////////////////////////////////////////////////

                    if (!FileOps.ProcessAll(
                            configuration, extractDirectory, targetDirectory,
                            baseDirectoryOffset, false, false, ref error))
                    {
                        QueueStatus(error, true);

                        QueueStatus(
                            "The list of file names in the extract " +
                            "directory does not match the list of file " +
                            "names in the target directory (release type " +
                            "mismatch).");

                        goto done;
                    }

                    QueueStatus("All release files are present.");

                    if (!FileOps.ProcessAll(
                            configuration, extractDirectory, targetDirectory,
                            baseDirectoryOffset, true, true, ref error))
                    {
                        QueueStatus(error, true);
                        QueueStatus("Failed to process release files.");

                        goto done;
                    }

                    QueueStatus("All release files were processed.");

                    ///////////////////////////////////////////////////////////

                    if (!configuration.WhatIf)
                    {
                        try
                        {
                            Directory.Delete(
                                extractDirectory, true); /* throw */

                            QueueStatus(String.Format(
                                "Extract directory \"{0}\" deleted.",
                                extractDirectory), true);
                        }
                        catch (Exception ex)
                        {
                            Trace(ex);

                            QueueStatus("Failed to delete extract directory.");
                            goto done;
                        }
                    }
                    else
                    {
                        QueueStatus("Skipped deleting extract directory.");
                    }

                    ///////////////////////////////////////////////////////////

                    if (!configuration.WhatIf)
                    {
                        try
                        {
                            Directory.Delete(
                                releaseDirectory, true); /* throw */

                            QueueStatus(String.Format(
                                "Release directory \"{0}\" deleted.",
                                releaseDirectory), true);
                        }
                        catch (Exception ex)
                        {
                            Trace(ex);

                            QueueStatus("Failed to delete release directory.");
                            goto done;
                        }
                    }
                    else
                    {
                        QueueStatus("Skipped deleting release directory.");
                    }

                    ///////////////////////////////////////////////////////////

                    QueueStatus("Update complete.");

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: Next, set the value of the 're-check' flag from
                    //       the configuration.  This flag is only (possibly)
                    //       set to a non-zero value upon the update process
                    //       completing successfully; otherwise, it is always
                    //       zero and the updater is not re-launched to check
                    //       for subsequent updates.
                    //
                    reCheck = configuration.ReCheck;

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: This label is reached upon success or failure.
                    //
                done:

                    ResetStatus(false, true, true);

                    EnableCancelButton(true);
                    EnableUpdateButton(true, false);

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: If the update was completed successfully, prompt
                    //       the user to exit and [re-]launch this tool so we
                    //       can check for another [subsequent] update (e.g.
                    //       for this tool itself, script updates for the core
                    //       library, etc).
                    //
                    if (reCheck)
                    {
                        string message = "Launch updater again in order to " +
                            "check for further updates?";

                        if (TraceOps.ShowMessage(
                                configuration, assembly, message,
                                TraceCategory, MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                DialogResult.Yes) == DialogResult.Yes)
                        {
                            try
                            {
                                QueueStatus(
                                    "Attempting to re-execute updater in " +
                                    "order to check for further updates...");

                                Configuration.StartAsSelf(assembly,
                                    false); /* throw */

                                SafeClose();
                                return;
                            }
                            catch (Exception ex)
                            {
                                Trace(ex);

                                QueueStatus("Failed to re-execute updater.");
                            }
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    if (silent)
                    {
                        TraceAndUpdateStatus("Closing due to silent mode...");
                        SafeClose();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace(ex);

                    Program.Fail(configuration, null,
                        "Caught exception in file download handler.",
                        TraceCategory);
                }
            }).Start();
        }
        #endregion
    }
}
