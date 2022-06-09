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
    [Guid("1c3cf092-e060-4423-8f8d-d3fccb110635")]
    internal partial class UpdateForm : Form
    {
        #region Private Constants
        private const string DefaultFormText = "Eagle Updater";

        ///////////////////////////////////////////////////////////////////////

        private const string EagleShellCommand = "EagleShell.exe";
        private const string NotepadCommand = "Notepad.exe";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string TraceCategory = typeof(UpdateForm).Name;

        ///////////////////////////////////////////////////////////////////////

        private const string FormTextFormat = "{0}{1}{2}{3}";
        private const string TraceFormat = "#{0} @ {1}: {2}";

        ///////////////////////////////////////////////////////////////////////

        private const string PercentMessage =
            "{0:N0} of {1:N0} bytes, {2:N2} bytes per second";

        ///////////////////////////////////////////////////////////////////////

        private const int StatusTimerInterval = 200;  /* milliseconds */
        private const int SilentTimerInterval = 5000; /* milliseconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private Configuration configuration;
        private Assembly assembly;
        private Queue<string> statusQueue;
        private System.Windows.Forms.Timer statusTimer;
        private System.Windows.Forms.Timer silentTimer;
        private DateTime started;
        private string userAgent;
        private UpdateWebClient client;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
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

        private bool CanLaunchLogFile()
        {
            return (configuration != null) &&
                !String.IsNullOrEmpty(configuration.LogFileName);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool CanLaunchEagleShell()
        {
            return (configuration != null) &&
                !String.IsNullOrEmpty(configuration.CoreDirectory);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool CanStartEagleThread()
        {
            return (configuration != null) && configuration.Shell;
        }

        ///////////////////////////////////////////////////////////////////////

        private void SafeClose()
        {
            FormOps.BeginInvoke(btnUpdate, new DelegateWithNoArgs(delegate()
            {
                Close();
            }), true);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static void Trace(
            Exception exception
            )
        {
            TraceOps.Trace(null, exception, TraceCategory);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void Trace(
            string message
            )
        {
            if (!String.IsNullOrEmpty(message))
            {
                TraceOps.Trace(null, String.Format(
                        TraceFormat, TraceOps.NextId(),
                        TraceOps.TimeStamp(DateTime.UtcNow), message),
                    TraceCategory);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status URI Methods
        private string GetDefaultStatusUri()
        {
            if ((configuration == null) || !configuration.IsValid)
                return null;

            return String.Format(
                "Target: \"{0}\" ({1} {2})", configuration.CoreFileName,
                configuration.ReleaseType, configuration.BuildType);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void ResetElapsedTime()
        {
            started = DateTime.UtcNow;
        }

        ///////////////////////////////////////////////////////////////////////

        private TimeSpan GetElapsedTime()
        {
            return DateTime.UtcNow.Subtract(started);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void QueueTrace(
            string message,
            string category /* NOT USED */
            )
        {
            QueueStatus(message, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private void QueueStatus(
            string message
            )
        {
            QueueStatus(message, false);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void UpdateForm_FormClosing(
            object sender,
            FormClosingEventArgs e
            )
        {
            if (statusTimer != null)
                statusTimer.Stop();
        }

        ///////////////////////////////////////////////////////////////////////

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

                TraceOps.ShowMessage(
                    configuration, assembly, String.Format(
                    "{0}{0}{1}{0}", Environment.NewLine,
                    _Shared.License.Text), TraceCategory,
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
