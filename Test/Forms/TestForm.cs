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
    /// <summary>
    /// This class implements a Windows Forms based test harness for the Eagle
    /// script engine.  It hosts an <see cref="Interpreter" />, lets the user
    /// select, run, cancel, and save the output of a test script, and bridges
    /// between the user-interface thread and the interpreter's evaluation and
    /// event-processing threads using a family of asynchronous helper methods.
    /// The portion of the form defined here is the hand-written code-behind;
    /// the control declarations and layout reside in the generated designer
    /// partial.
    /// </summary>
    [ObjectId("5e06983b-5e46-4ea7-8056-e4bb600f28d1")]
    public partial class TestForm : Form
    {
        #region Private Data
        #region Service Thread Data
#if DAEMON
        /// <summary>
        /// The event used to signal the dedicated event servicing thread that
        /// it should stop processing events and exit.
        /// </summary>
        private ManualResetEvent serviceDone;

        /// <summary>
        /// The dedicated background thread that continuously services pending
        /// interpreter events while running in dedicated daemon mode.
        /// </summary>
        private Thread serviceThread;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Integration Data
        /// <summary>
        /// The script engine interpreter used by this form to evaluate the
        /// selected test script and to process its queued events.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// The list of arguments to pass to the test script.  The first
        /// argument is treated as the name of the test file to evaluate.
        /// </summary>
        private ArgumentList arguments;

        /// <summary>
        /// The text of the script currently being evaluated, retained so that
        /// completion and cancellation notifications can be matched against it.
        /// This field may be null when no script is being evaluated.
        /// </summary>
        private string script;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A counter, manipulated atomically, used to guard against re-entrant
        /// attempts to begin the application exit sequence.
        /// </summary>
        private int exiting;

        /// <summary>
        /// A counter, manipulated atomically, used as a logical boolean that
        /// records whether the application exit sequence has already succeeded.
        /// </summary>
        private int exited;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an empty test form instance.  This constructor initializes
        /// the designer-generated components, wires up the form events of
        /// interest, and sets the form caption.  It is chained to by the public
        /// constructor.
        /// </summary>
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
        /// <summary>
        /// Constructs a test form instance bound to the specified interpreter
        /// and command-line arguments.  When no arguments are supplied, the
        /// default test file name is used.  When compiled in dedicated daemon
        /// mode, this constructor also starts the background event servicing.
        /// </summary>
        /// <param name="interpreter">
        /// The script engine interpreter that this form will use to evaluate
        /// the test script.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the test script; the first element is used
        /// as the name of the test file.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Handles the key-down event for the form.  When the CTRL-F5 keystroke
        /// is detected, this method starts the interactive loop thread for the
        /// script engine, complaining if that attempt fails.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this key event.
        /// </param>
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

        /// <summary>
        /// Handles the form-closing event.  When the user is closing the form,
        /// this method cancels the default close, then queues an asynchronous
        /// event to exit the application, falling back to the thread pool if the
        /// engine is unable to queue the work item.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this form-closing event.
        /// </param>
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
                if (!Engine.QueueWorkItem(
                        interpreter, AsyncExit, null, QueueFlags.Default))
                {
                    /* IGNORED */
                    Utility.QueueUserWorkItem(AsyncExit, null);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the disposed event for the form.  When compiled in dedicated
        /// daemon mode, this method stops and finalizes the event servicing
        /// thread; in all cases it marks the form as disposed.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this event.
        /// </param>
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
        /// <summary>
        /// Performs the application exit sequence.  Using atomic guards to
        /// prevent re-entrancy, this method cancels any script being evaluated,
        /// marks the interpreter as exited, breaks out of any interactive loop,
        /// and disposes the form.  On failure it reports the reason via the
        /// status text and resets its exited indicator so a later retry is
        /// possible.  This method is intended to be invoked as a queued work
        /// item.
        /// </summary>
        /// <param name="state">
        /// The optional state object supplied when the work item was queued;
        /// it is not used.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Processes all pending interpreter events one time.  This method is
        /// used when not compiled in dedicated daemon mode and is intended to be
        /// invoked as a queued work item.  Any failure is reported via the
        /// status text.
        /// </summary>
        /// <param name="state">
        /// The optional state object supplied when the work item was queued;
        /// when it can be interpreted as a boolean it indicates whether event
        /// processing should stop on the first error.  This parameter may be
        /// null.
        /// </param>
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
        /// <summary>
        /// Continuously services pending interpreter events until signaled to
        /// stop.  This method is used when compiled in dedicated daemon mode and
        /// is intended to run as the body of the dedicated event servicing
        /// thread, yielding to other threads between iterations.  Any failure is
        /// reported via the status text.
        /// </summary>
        /// <param name="state">
        /// The optional state object supplied when the thread was started; it is
        /// not used.  This parameter may be null.
        /// </param>
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

        /// <summary>
        /// Asynchronously disposes this form.  If the form still has a valid
        /// window handle, this method marshals a disposal call onto the
        /// user-interface thread.
        /// </summary>
        public void AsyncDispose()
        {
            CheckDisposed();

            if (CommonOps.IsValidHandle(CommonOps.GetHandle(this))) // HACK: Remove?
                BeginInvoke(new SimpleDelegate(Dispose));
        }

        ///////////////////////////////////////////////////////////////////////

#if DAEMON
        /// <summary>
        /// Stops the dedicated event servicing thread, if it is running, by
        /// signaling its stop event and waiting for the thread to terminate.
        /// </summary>
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

        /// <summary>
        /// Releases the resources used by the dedicated event servicing thread,
        /// closing and clearing the stop event.
        /// </summary>
        private void AsyncFinalizeEvents()
        {
            if (serviceDone != null)
            {
                serviceDone.Close();
                serviceDone = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restarts the dedicated event servicing thread.  This method
        /// initializes the stop event if necessary, stops any existing servicing
        /// thread, then creates and starts a fresh servicing thread, complaining
        /// if the thread cannot be created.
        /// </summary>
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

        /// <summary>
        /// Asynchronously notifies the form that a script has completed, by
        /// marshaling the completion handling onto the user-interface thread.
        /// </summary>
        /// <param name="text">
        /// The text of the script that completed.
        /// </param>
        /// <param name="result">
        /// The result produced by evaluating the script.  This parameter may be
        /// null.
        /// </param>
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

        /// <summary>
        /// Asynchronously notifies the form that a script has been canceled, by
        /// marshaling the cancellation handling onto the user-interface thread.
        /// </summary>
        public void AsyncScriptCanceled()
        {
            CheckDisposed();

            BeginInvoke(new ScriptCanceledDelegate(ScriptCanceled));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously clears the list of test items, by marshaling the clear
        /// operation onto the user-interface thread.
        /// </summary>
        public void AsyncClearTestItems()
        {
            CheckDisposed();

            BeginInvoke(new ClearTestItemsDelegate(ClearTestItems));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously adds an item to the list of test items, by marshaling
        /// the add operation onto the user-interface thread.
        /// </summary>
        /// <param name="item">
        /// The test item to add to the list.
        /// </param>
        public void AsyncAddTestItem(
            string item
            )
        {
            CheckDisposed();

            BeginInvoke(new AddTestItemDelegate(AddTestItem),
                new object[] { item });
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously clears the status text, by marshaling the clear
        /// operation onto the user-interface thread.
        /// </summary>
        public void AsyncClearStatusText()
        {
            CheckDisposed();

            BeginInvoke(new ClearStatusTextDelegate(ClearStatusText));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously appends text to the status text, by marshaling the
        /// append operation onto the user-interface thread.
        /// </summary>
        /// <param name="text">
        /// The text to append to the status text.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a trailing line terminator after the text.
        /// </param>
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

        /// <summary>
        /// Asynchronously sets the value of the progress bar, by marshaling the
        /// set operation onto the user-interface thread.
        /// </summary>
        /// <param name="value">
        /// The new value for the progress bar.
        /// </param>
        public void AsyncSetProgressValue(
            int value
            )
        {
            CheckDisposed();

            BeginInvoke(new SetProgressValueDelegate(SetProgressValue),
                new object[] { value });
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously evaluates a script, by marshaling the evaluation onto
        /// the user-interface thread.
        /// </summary>
        /// <param name="text">
        /// The text of the script to evaluate.
        /// </param>
        /// <param name="synchronizedResult">
        /// The result object used to communicate the return code and result of
        /// the evaluation back to a waiting caller.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Enables or disables the controls that should not be available while a
        /// script is being evaluated.  This method must be called on the
        /// user-interface thread.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the affected controls; zero to disable them.
        /// </param>
        private void EnableControls(
            bool enable
            )
        {
            btnSelectFileName.Enabled = enable;
            btnRun.Enabled = enable;
            btnSave.Enabled = enable;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles completion of a script on the user-interface thread.  When
        /// the completed script matches the one being tracked, this method
        /// reports success via the status text and ends the script.
        /// </summary>
        /// <param name="text">
        /// The text of the script that completed.
        /// </param>
        /// <param name="result">
        /// The result produced by evaluating the script.  This parameter may be
        /// null.
        /// </param>
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

        /// <summary>
        /// Handles cancellation of a script on the user-interface thread.  When
        /// the interpreter appears ready to receive new scripts, this method
        /// reports the cancellation via the status text and ends the script.
        /// </summary>
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

        /// <summary>
        /// Performs the common work of ending a script on the user-interface
        /// thread, resetting the tracked script text and re-enabling the
        /// controls that were disabled while the script ran.
        /// </summary>
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

        /// <summary>
        /// Clears all entries from the test items list.  This method must be
        /// called on the user-interface thread.
        /// </summary>
        private void ClearTestItems()
        {
            lstTest.Items.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds an item to the test items list and briefly selects it so that it
        /// is scrolled into view.  This method must be called on the
        /// user-interface thread.
        /// </summary>
        /// <param name="item">
        /// The test item to add to the list.
        /// </param>
        /// <returns>
        /// The zero-based index at which the item was added.
        /// </returns>
        private int AddTestItem(string item)
        {
            int result = lstTest.Items.Add(item);
            int count = lstTest.Items.Count;

            lstTest.SetSelected(count - 1, true);
            lstTest.SetSelected(count - 1, false);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the status text box.  This method must be called on the
        /// user-interface thread.
        /// </summary>
        private void ClearStatusText()
        {
            txtStatus.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Appends text to the status text box, optionally followed by a line
        /// terminator.  This method must be called on the user-interface thread.
        /// </summary>
        /// <param name="text">
        /// The text to append to the status text box.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a trailing line terminator after the text.
        /// </param>
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

        /// <summary>
        /// Sets the value of the progress bar.  This method must be called on
        /// the user-interface thread.
        /// </summary>
        /// <param name="value">
        /// The new value for the progress bar.
        /// </param>
        private void SetProgressValue(
            int value
            )
        {
            prbProgress.Value = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates a script using the interpreter and communicates the outcome
        /// back through the supplied synchronized result.  This method must be
        /// called on the user-interface thread.
        /// </summary>
        /// <param name="text">
        /// The text of the script to evaluate.
        /// </param>
        /// <param name="synchronizedResult">
        /// The result object used to communicate the return code and result of
        /// the evaluation back to a waiting caller.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// Handles the click event for the run button.  This method disables the
        /// affected controls, obtains and (when necessary) reads the selected
        /// test script, sets the script arguments, queues the script for
        /// evaluation, and tracks it; when not in dedicated daemon mode it also
        /// queues asynchronous event processing.  Any failure is reported.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this event.
        /// </param>
        private void btnRun_Click(
            object sender,
            EventArgs e
            )
        {
            EnableControls(false);

            ReturnCode code;

            ScriptFlags scriptFlags = ScriptFlags.UserRequiredFile;

            scriptFlags |= interpreter.ScriptFlags;

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

        /// <summary>
        /// Handles the click event for the cancel button, canceling any script
        /// currently being evaluated by the interpreter and complaining if the
        /// cancellation attempt fails.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this event.
        /// </param>
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

        /// <summary>
        /// Handles the click event for the select-file-name button, showing the
        /// open-file dialog rooted at the binary path so the user can choose a
        /// test file.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this event.
        /// </param>
        private void btnSelectFileName_Click(
            object sender,
            EventArgs e
            )
        {
            ofdTest.InitialDirectory = Utility.GetBinaryPath();
            ofdTest.ShowDialog(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the click event for the save button, showing the save-file
        /// dialog rooted at the binary path so the user can choose where to save
        /// the results.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this event.
        /// </param>
        private void btnSave_Click(
            object sender,
            EventArgs e
            )
        {
            sfdTest.InitialDirectory = Utility.GetBinaryPath();
            sfdTest.ShowDialog(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the file-ok event for the open-file dialog, recording the
        /// chosen file name as the first script argument and displaying it in the
        /// file name text box.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this cancelable event.
        /// </param>
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

        /// <summary>
        /// Handles the file-ok event for the save-file dialog, writing the
        /// current status text to the chosen file and reporting either success
        /// or the error encountered via the status text.
        /// </summary>
        /// <param name="sender">
        /// The object that raised this event.
        /// </param>
        /// <param name="e">
        /// The data associated with this cancelable event.
        /// </param>
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
        /// <summary>
        /// Non-zero if this form has been disposed and is no longer usable.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Throws an exception if this form has been disposed.  When compiled
        /// with the throw-on-disposed behavior enabled and the interpreter is
        /// configured to honor it, this method throws
        /// <see cref="ObjectDisposedException" />.
        /// </summary>
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
