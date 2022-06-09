/*
 * TestForm.cs --
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
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using Eagle._Shell;

namespace Eagle._Forms
{
    [ObjectId("5e06983b-5e46-4ea7-8056-e4bb600f28d1")]
    public partial class TestForm : Form
    {
        #region Private Data
        #region Service Thread Data
#if DAEMON
        private ManualResetEvent serviceDone;
        private Thread serviceThread;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Integration Data
        private Interpreter interpreter;
        private ArgumentList arguments;
        private string script;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private int exiting;
        private int exited;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private TestForm()
            : base()
        {
            InitializeComponent();

            //
            // NOTE: Setup the form events that we care about.
            //
            this.FormClosing += new FormClosingEventHandler(TestForm_FormClosing);
            this.Disposed += new EventHandler(TestForm_Disposed);
            this.KeyDown += new KeyEventHandler(TestForm_KeyDown);

            //
            // NOTE: Set the caption of the form.
            //
            this.Text = String.Format(
                CommonOps.ProductNameAndVersionFormat,
                Application.ProductName,
                Application.ProductVersion);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public TestForm(
            Interpreter interpreter,
            IEnumerable<string> args
            )
            : this()
        {
            this.interpreter = interpreter;
            this.arguments = new ArgumentList(args);

            //
            // NOTE: Default the input file name if it was not supplied
            //       by the caller.
            //
            if (this.arguments.Count == 0)
                this.arguments.Add(Path.Combine(
                    Utility.GetBinaryPath(), CommonOps.TestFileName));

            txtFileName.Text = this.arguments[0];

#if DAEMON
            //
            // NOTE: We are acting as a host in dedicated daemon mode (i.e. if
            //       DAEMON is defined), start processing events now until the
            //       interpreter is no longer "ready" (i.e. script canceled,
            //       application exiting, machine on fire, etc).
            //
            AsyncRestartEvents();
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Form Event Handlers
        private void TestForm_KeyDown(
            object sender,
            KeyEventArgs e
            )
        {
            //
            // NOTE: The keystroke CTRL-F5 will open the host window
            //       for the script engine.
            //
            if (!e.Shift && e.Control && !e.Alt && (e.KeyCode == Keys.F5))
            {
                ReturnCode code;
                Result error = null;

                code = Test.StartupInteractiveLoopThread(ref error);

                if (code != ReturnCode.Ok)
                    CommonOps.Complain(code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void TestForm_FormClosing(
            object sender,
            FormClosingEventArgs e
            )
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                //
                // NOTE: Cancel this close event because we need to handle
                //       it in a different way.
                //
                e.Cancel = true;

                //
                // NOTE: Queue an asynchronous event that will cause the
                //       application to exit.  Since we cannot really accept
                //       failure as an option at this point (i.e. we must be
                //       able to exit), fallback to using the thread pool if
                //       the engine fails to queue the work item.
                //
                if (!Engine.QueueWorkItem(interpreter, AsyncExit, null))
                {
                    /* IGNORED */
                    Utility.QueueUserWorkItem(AsyncExit, null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void TestForm_Disposed(
            object sender,
            EventArgs e
            )
        {
#if DAEMON
            //
            // NOTE: Stop the event servicing thread if it is running.
            //
            AsyncStopEvents();

            //
            // NOTE: Cleanup the resources used by the event servicing
            //       thread.
            //
            AsyncFinalizeEvents();
#endif

            //
            // NOTE: This form is now disposed.
            //
            disposed = true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Methods
        private void AsyncExit(
            object state
            )
        {
            if (Interlocked.Increment(ref exiting) == 1)
            {
                if (Interlocked.Increment(ref exited) == 1)
                {
                    ReturnCode code;
                    Result error = null;

                    if (interpreter != null)
                    {
                        IDebugHost debugHost = interpreter.Host;

                        if (debugHost != null)
                        {
#if NOTIFY || NOTIFY_OBJECT
                            //
                            // NOTE: Globally mask off the test plugin
                            //       notification flags because at this point
                            //       we do not want any kind of re-entrancy,
                            //       asynchronous or otherwise.
                            //
                            interpreter.GlobalNotifyTypes &=
                                ~Utility.GetNotifyTypes(typeof(_Plugins.TestForm));

                            interpreter.GlobalNotifyFlags &=
                                ~Utility.GetNotifyFlags(typeof(_Plugins.TestForm));
#endif

                            //
                            // NOTE: Cancel anything being evaluated in the
                            //       interpreter.
                            //
                            code = Engine.CancelEvaluate(
                                interpreter, "test form exit",
                                CancelFlags.UnwindAndNotify, ref error);

                            //
                            // NOTE: Mark the interpreter as "exited".
                            //
                            interpreter.Exit = true;

                            //
                            // NOTE: Break out of any attempt to read a line of
                            //       input from the user if the interpreter has
                            //       an interactive loop going.
                            //
                            if ((code == ReturnCode.Ok) && Test.HaveInteractiveLoop())
                                code = debugHost.Cancel(false, ref error);

                            //
                            // NOTE: If we failed for some reason, I suppose
                            //       we want to be able to retry later.
                            //
                            if (code == ReturnCode.Ok)
                            {
                                AsyncDispose();
                            }
#if NOTIFY || NOTIFY_OBJECT
                            else
                            {
                                interpreter.GlobalNotifyFlags |=
                                    Utility.GetNotifyFlags(typeof(_Plugins.TestForm));

                                interpreter.GlobalNotifyTypes |=
                                    Utility.GetNotifyTypes(typeof(_Plugins.TestForm));
                            }
#endif
                        }
                        else
                        {
                            error = "interpreter host not available";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = "invalid interpreter";
                        code = ReturnCode.Error;
                    }

                    //
                    // NOTE: If we did not succeed, at least attempt to provide
                    //       hints as to why.
                    //
                    if (code != ReturnCode.Ok)
                    {
                        //
                        // NOTE: Update the status with our async failure.
                        //
                        AsyncAppendStatusText(Utility.FormatResult(code, error), true);

                        //
                        // NOTE: We did not actually manage to exit, reset
                        //       the exited indicator now.
                        //
                        Interlocked.Decrement(ref exited);
                    }
                }
                else
                {
                    //
                    // NOTE: We do not want the exited indicator to have a
                    //       value higher than one (i.e. it is a logical
                    //       boolean); therefore, decrement it now.
                    //
                    Interlocked.Decrement(ref exited);
                }
            }

            Interlocked.Decrement(ref exiting);
        }

        ///////////////////////////////////////////////////////////////////////

#if !DAEMON
        private void AsyncProcessEvents(
            object state
            )
        {
            bool stopOnError = false;

            if (state != null)
            {
                try
                {
                    stopOnError = (bool)state;
                }
                catch
                {
                    // do nothing.
                }
            }

            ReturnCode code;
            Result result = null;

            if (interpreter != null)
            {
                IEventManager eventManager = interpreter.EventManager;

                if ((eventManager != null) && !eventManager.Disposed)
                {
                    code = eventManager.ProcessEvents(
                        interpreter.ServiceEventFlags, 0, stopOnError,
                        false, ref result);
                }
                else
                {
                    result = "event manager not available";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            if (code != ReturnCode.Ok)
            {
                //
                // NOTE: Update the status with our async failure.
                //
                AsyncAppendStatusText(
                    Utility.FormatResult(code, result), true);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DAEMON
        private void AsyncServiceEvents(
            object state
            )
        {
            ReturnCode code;
            Result result;

            if (interpreter != null)
            {
                IEventManager eventManager = interpreter.EventManager;

                if ((eventManager != null) && !eventManager.Disposed)
                {
                    //
                    // NOTE: Keep processing asynchronous events until we are
                    //       done.
                    //
                    ManualResetEvent done = serviceDone;

#if !MONO && !MONO_HACKS && (NET_20_SP2 || NET_40 || NET_STANDARD_20)
                    while ((done != null) && !done.WaitOne(0))
#else
                    while ((done != null) && !done.WaitOne(0, false))
#endif
                    {
                        //
                        // NOTE: Attempt to process all pending events stopping
                        //       if an error is encountered.
                        //
                        result = null;

                        code = eventManager.ProcessEvents(
                            interpreter.ServiceEventFlags, EventPriority.Service,
                            null, 0, true, false, ref result);

                        //
                        // NOTE: If we encountered an error processing events,
                        //       break out of the loop and return the error
                        //       code and result to the caller.  Alternatively,
                        //       we could report the error and continue to
                        //       process events if we are running in dedicated
                        //       daemon mode (i.e. if DAEMON is defined).
                        //
                        if (code != ReturnCode.Ok)
                        {
                            AsyncAppendStatusText(
                                Utility.FormatResult(code, result), true);
                        }

                        //
                        // NOTE: We always yield to other running threads.
                        //       This also gives them an opportunity to cancel
                        //       the script in progress on this thread and/or
                        //       update the variable we are waiting for.
                        //
                        result = null;

                        if (!eventManager.Sleep(
                                SleepType.Service, true, ref result))
                        {
                            AsyncAppendStatusText(
                                Utility.FormatResult(ReturnCode.Error, result),
                                true);
                        }
                    }

                    //
                    // NOTE: Servicing completed successfully.
                    //
                    return;
                }
                else
                {
                    result = "event manager not available";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            //
            // NOTE: For some reason we could not service any events.  Report
            //       the reason.
            //
            if (code != ReturnCode.Ok)
                AsyncAppendStatusText(Utility.FormatResult(code, result), true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public void AsyncDispose()
        {
            CheckDisposed();

            if (CommonOps.IsValidHandle(CommonOps.GetHandle(this))) // HACK: Remove?
                BeginInvoke(new SimpleDelegate(Dispose));
        }

        ///////////////////////////////////////////////////////////////////////

#if DAEMON
        private void AsyncStopEvents()
        {
            if (serviceDone == null)
                return;

            if ((serviceThread == null) || !serviceThread.IsAlive)
                return;

            serviceDone.Set();
            serviceThread.Join();
            serviceThread = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private void AsyncFinalizeEvents()
        {
            if (serviceDone != null)
            {
                serviceDone.Close();
                serviceDone = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void AsyncRestartEvents()
        {
            //
            // NOTE: Initialize the service thread event, if necessary.
            //
            if (serviceDone == null)
                serviceDone = new ManualResetEvent(false);

            //
            // NOTE: Stop the service thread just in case it has already
            //       been started.
            //
            AsyncStopEvents();

            //
            // NOTE: Create a new service thread and start it now.
            //
            serviceThread = Engine.CreateThread(
                AsyncServiceEvents, 0, true, false, true);

            if (serviceThread != null)
            {
                serviceThread.Name = "serviceEventsThread";
                serviceThread.Start(null);
            }
            else
            {
                CommonOps.Complain("could not create service thread");
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public void AsyncScriptCompleted(
            string text,
            Result result
            )
        {
            CheckDisposed();

            BeginInvoke(new ScriptCompletedDelegate(ScriptCompleted),
                new object[] { text, result });
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncScriptCanceled()
        {
            CheckDisposed();

            BeginInvoke(new ScriptCanceledDelegate(ScriptCanceled));
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncClearTestItems()
        {
            CheckDisposed();

            BeginInvoke(new ClearTestItemsDelegate(ClearTestItems));
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncAddTestItem(
            string item
            )
        {
            CheckDisposed();

            BeginInvoke(new AddTestItemDelegate(AddTestItem),
                new object[] { item });
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncClearStatusText()
        {
            CheckDisposed();

            BeginInvoke(new ClearStatusTextDelegate(ClearStatusText));
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncAppendStatusText(
            string text,
            bool newLine
            )
        {
            CheckDisposed();

            BeginInvoke(new AppendStatusTextDelegate(AppendStatusText),
                new object[] { text, newLine });
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncSetProgressValue(
            int value
            )
        {
            CheckDisposed();

            BeginInvoke(new SetProgressValueDelegate(SetProgressValue),
                new object[] { value });
        }

        ///////////////////////////////////////////////////////////////////////

        public void AsyncEvaluateScript(
            string text,
            Result synchronizedResult
            )
        {
            CheckDisposed();

            BeginInvoke(new EvaluateScriptDelegate(EvaluateScript),
                new object[] { text, synchronizedResult });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Primary-Only Thread Methods
        private void EnableControls(
            bool enable
            )
        {
            btnSelectFileName.Enabled = enable;
            btnRun.Enabled = enable;
            btnSave.Enabled = enable;
        }

        ///////////////////////////////////////////////////////////////////////

        private void ScriptCompleted(
            string text,
            Result result
            )
        {
            //
            // NOTE: Make sure that it was the test script that has been
            //       evaluated.
            //
            if (Object.ReferenceEquals(text, script) ||
                Utility.SystemStringEquals(text, script))
            {
                if ((result == null) || (result.ReturnCode == ReturnCode.Ok))
                    AppendStatusText("The script was evaluated.", true);

                ScriptEnded();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void ScriptCanceled()
        {
            //
            // NOTE: Check to see if the interpreter appears to be ready to
            //       receive new scripts to evaluate.
            //
            if ((interpreter != null) && (interpreter.Levels == 0))
            {
                AppendStatusText("The script has been canceled.", true);

                ScriptEnded();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void ScriptEnded()
        {
            //
            // NOTE: Reset the text of the script that was being evaluated.
            //
            script = null;

            //
            // NOTE: Re-enable the controls that we disabled before evaluating
            //       the script.
            //
            EnableControls(true);
        }

        ///////////////////////////////////////////////////////////////////////

        private void ClearTestItems()
        {
            lstTest.Items.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        private int AddTestItem(string item)
        {
            int result = lstTest.Items.Add(item);
            int count = lstTest.Items.Count;

            lstTest.SetSelected(count - 1, true);
            lstTest.SetSelected(count - 1, false);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private void ClearStatusText()
        {
            txtStatus.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        private void AppendStatusText(
            string text,
            bool newLine
            )
        {
            txtStatus.AppendText(text);

            if (newLine)
                txtStatus.AppendText(Environment.NewLine);
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetProgressValue(
            int value
            )
        {
            prbProgress.Value = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private void EvaluateScript(
            string text,
            Result synchronizedResult
            )
        {
            ReturnCode code;
            Result result = null;

            if (interpreter != null)
            {
                code = interpreter.EvaluateScript(text, ref result); /* EXEMPT */
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            Utility.SetSynchronizedResult(synchronizedResult, code, result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Control Event Handlers
        private void btnRun_Click(
            object sender,
            EventArgs e
            )
        {
            EnableControls(false);

            ReturnCode code;

            ScriptFlags scriptFlags = ScriptFlags.UserRequiredFile;
            IClientData clientData = ClientData.Empty;

            Result result = null;

            code = interpreter.GetScript(
                CommonOps.ProgressScriptName, ref scriptFlags, ref clientData,
                ref result);

            if (code == ReturnCode.Ok)
            {
                string text = result;

                if (Utility.HasFlags(scriptFlags, ScriptFlags.File, true))
                    code = Engine.ReadScriptFile(
                        interpreter, text, ref text, ref result);

                if (code == ReturnCode.Ok)
                {
                    code = interpreter.SetArguments(
                        new StringList(arguments), ref result);

                    if (code == ReturnCode.Ok)
                    {
                        code = interpreter.QueueScript(
                            Utility.GetUtcNow(), text, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Set the internal script (for tracking
                            //       purposes) to the script we just queued.
                            //
                            script = result;

#if !DAEMON
                            //
                            // NOTE: Start asynchronously processing queued
                            //       events in the interpreter until the queue
                            //       is empty if we are already processing
                            //       events due to being in dedicated daemon
                            //       mode (i.e. if DAEMON is defined).
                            //
                            /* IGNORED */
                            Engine.QueueWorkItem(
                                interpreter, AsyncProcessEvents, false);
#endif
                        }
                    }
                }
            }

            if (code != ReturnCode.Ok)
                CommonOps.Complain(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        private void btnCancel_Click(
            object sender,
            EventArgs e
            )
        {
            ReturnCode code;
            Result error = null;

            code = Engine.CancelEvaluate(
                interpreter, null, CancelFlags.UnwindAndNotify, ref error);

            if (code != ReturnCode.Ok)
                CommonOps.Complain(code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        private void btnSelectFileName_Click(
            object sender,
            EventArgs e
            )
        {
            ofdTest.InitialDirectory = Utility.GetBinaryPath();
            ofdTest.ShowDialog(this);
        }

        ///////////////////////////////////////////////////////////////////////

        private void btnSave_Click(
            object sender,
            EventArgs e
            )
        {
            sfdTest.InitialDirectory = Utility.GetBinaryPath();
            sfdTest.ShowDialog(this);
        }

        ///////////////////////////////////////////////////////////////////////

        private void ofdTest_FileOk(
            object sender,
            CancelEventArgs e
            )
        {
            string fileName = ofdTest.FileName;

            if (arguments.Count == 0)
                arguments.Add(fileName);
            else
                arguments[0] = fileName;

            txtFileName.Text = fileName;
        }

        ///////////////////////////////////////////////////////////////////////

        private void sfdTest_FileOk(
            object sender,
            CancelEventArgs e
            )
        {
            string fileName = sfdTest.FileName;

            try
            {
                File.WriteAllText(fileName, txtStatus.Text);

                AppendStatusText(String.Format(
                    "Saved results to file \"{0}\"", fileName), true);
            }
            catch (Exception ex)
            {
                string error = String.Format(
                    "Could not save results to file \"{0}\".{1}{2}",
                    fileName, Environment.NewLine, ex);

                AppendStatusText(error, true);

                CommonOps.Complain(error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(TestForm).Name);
#endif
        }
        #endregion
    }
}
