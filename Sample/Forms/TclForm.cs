/*
 * TclForm.cs --
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
using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;
using TclSample.Commands;

namespace TclSample.Forms
{
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("7d8ce99d-5445-4852-b752-dd2519c3bc47")]
    public sealed partial class TclForm : Form
    {
        #region Private Constants
        #region Tcl Manager / Command Creation Flags
        /// <summary>
        /// These are the flags used to create the Tcl manager component.
        /// </summary>
        private const CreateFlags DefaultCreateFlags =
            CreateFlags.TclManagerUse;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// These are the flags used to create the host for the Tcl manager
        /// component.
        /// </summary>
        private const HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.TclManagerUse;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// These are the extra flags to use when creating the bridged Tcl
        /// command.
        /// </summary>
        private const CommandFlags DefaultCommandFlags = CommandFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The error message when the user attempts to close the Tcl form
        /// while a Tcl script is in progress.
        /// </summary>
        private const string ClosingError =
            "Please cancel the running script(s) prior to closing this form.";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tcl Loader Constants
        /// <summary>
        /// These are the flags used to select and control the mechanism(s)
        /// used to find the instances of Tcl installed on this machine.
        /// </summary>
        private const FindFlags DefaultFindFlags = FindFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// These are the flags used to control the mechanism(s) used to load
        /// an instance of Tcl installed on this machine.
        /// </summary>
        private const LoadFlags DefaultLoadFlags = LoadFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the Tcl version that should be assumed when the version
        /// cannot be deduced from the Tcl library file name itself.
        /// </summary>
        private static readonly Version DefaultUnknownVersion =
            new Version(8, 4);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the minimum acceptable Tcl version when searching for
        /// instances of the Tcl library (null means any).
        /// </summary>
        private static readonly Version DefaultMinimumVersion = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the maximum acceptable Tcl version when searching for
        /// instances of the Tcl library (null means any).
        /// </summary>
        private static readonly Version DefaultMaximumVersion = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// This object is used to synchronize access to the collection of Tcl
        /// forms.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The collection of active Tcl forms known to this assembly.
        /// </summary>
        private static readonly IDictionary<TclForm, object> forms =
            new Dictionary<TclForm, object>();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The command line arguments provided to the Tcl form.  Typically,
        /// these are the same as the command line arguments used to start
        /// the assembly; however, that is not a requirement.
        /// </summary>
        private IEnumerable<string> args;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The Tcl manager object associated with this Tcl form.  Currently,
        /// the only available Tcl manager implementation is the Interpreter
        /// class.
        /// </summary>
        private ITclManager tclManager;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The Tcl interpreter name associated with this Tcl form.  This is
        /// used to lookup the actual Tcl interpreter handle just prior to
        /// evaluating or canceling a script.
        /// </summary>
        private string interpName;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// This private constructor used by the static factory method when
        /// creating a new instance of this form.
        /// </summary>
        private TclForm()
            : base()
        {
            InitializeComponent();

            //
            // NOTE: Setup all the event handlers that we care about.
            //
            this.FormClosing += new FormClosingEventHandler(
                TclForm_FormClosing);

            this.Disposed += new EventHandler(TclForm_Disposed);
            this.btnNew.Click += new EventHandler(btnNew_Click);
            this.btnEvaluate.Click += new EventHandler(btnEvaluate_Click);
            this.btnCancel.Click += new EventHandler(btnCancel_Click);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static TclForm Create(
            IEnumerable<string> args /* in */
            )
        {
            TclForm form = null;
            ITclManager tclManager = null;
            ReturnCode code = ReturnCode.Ok;
            Result result = null;

            try
            {
                form = new TclForm();

                //
                // NOTE: Save the passed in command line arguments for
                //       later.
                //
                form.args = args;

                //
                // NOTE: Create a new Eagle interpreter; currently, this
                //       is necessary to use the Tcl integration features
                //       of Eagle.
                //
                tclManager = Interpreter.Create(args, DefaultCreateFlags,
                    DefaultHostCreateFlags, ref result);

                if (tclManager != null)
                {
                    //
                    // NOTE: Attempt to use our custom Tcl manager host
                    //       implementation, if applicable.
                    //
                    SetupManagerHost(tclManager, true);

                    //
                    // NOTE: Automatically locate and select the "best"
                    //       available build of Tcl, if any.
                    //
                    code = tclManager.LoadTcl(
                        DefaultFindFlags, DefaultLoadFlags, null, null,
                        DefaultMinimumVersion, DefaultMaximumVersion,
                        DefaultUnknownVersion, null, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Grab the name for the newly created
                        //       Tcl interpreter.
                        //
                        string interpName = result;

                        //
                        // NOTE: Create a new command object.  This will
                        //       be used to demonstrate implementing a
                        //       Tcl command in managed code.
                        //
                        ICommand command = Class6.NewCommand("class6",
                            new ClientData(form), DefaultCommandFlags);

                        //
                        // NOTE: Create a Tcl command bridging object.
                        //       This is used to translate inbound
                        //       native calls for a particular command
                        //       to a managed method call.
                        //
                        ITclEntityManager tclEntityManager = tclManager
                            as ITclEntityManager;

                        if (tclEntityManager != null)
                        {
                            code = tclEntityManager.AddTclBridge(
                                command, interpName, command.Name,
                                command.ClientData, false, false,
                                ref result);
                        }
                        else
                        {
                            result = "invalid Tcl entity manager";
                            code = ReturnCode.Error;
                        }

                        //
                        // NOTE: Did we successfully create the bridged
                        //       command?
                        //
                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Save the created Eagle interpreter.
                            //
                            form.tclManager = tclManager;

                            //
                            // NOTE: Save the name of the created Tcl
                            //       interpreter.
                            //
                            form.interpName = interpName;

                            //
                            // NOTE: Keep track of this form in the
                            //       global collection.  This collection
                            //       is primarily used when script
                            //       cancellation needs to be performed
                            //       for every Tcl interpreter we have
                            //       created.
                            //
                            lock (syncRoot)
                            {
                                if (forms != null)
                                    forms.Add(form, null);
                            }
                        }
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (code != ReturnCode.Ok)
                {
                    //
                    // NOTE: We failed to fully create the form.  All
                    //       the resources we managed to successfully
                    //       create must be cleaned up now.
                    //
                    // NOTE: Dispose of the Eagle interpreter now
                    //       just in case the form does not have
                    //       the private variable set correctly.
                    //
                    DisposeManager(ref tclManager); /* throw */

                    if (form != null)
                    {
                        //
                        // NOTE: If the form had an Eagle interpreter,
                        //       it should have already been disposed
                        //       (above) because we use the local
                        //       variable prior to setting the form
                        //       variable.  To be sure that the now
                        //       disposed Eagle interpreter is not
                        //       used during form disposal, we need to
                        //       null out the form variable now.
                        //
                        form.tclManager = null;

                        //
                        // NOTE: Now, dispose any other resources held
                        //       by the form.
                        //
                        form.Dispose();
                        form = null;
                    }
                }
            }

            if (code != ReturnCode.Ok)
                ShowResult(code, result);

            return form;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Form Event Handlers
        /// <summary>
        /// This event handler is called just prior to the form being closed.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the event.
        /// </param>
        private void TclForm_FormClosing(
            object sender,         /* in */
            FormClosingEventArgs e /* in */
            )
        {
            if (!disposed)
            {
                //
                // NOTE: Prevent this instance from being closed if
                //       a script is active.
                //
                if ((tclManager != null) &&
                    tclManager.IsTclInterpreterActive(interpName))
                {
                    e.Cancel = true;

                    ShowResult(ReturnCode.Error, ClosingError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This event handler is called when the form is being disposed.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the event.
        /// </param>
        private void TclForm_Disposed(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            if (!disposed)
            {
                if (tclManager != null)
                {
                    //
                    // NOTE: Stop using our custom Tcl manager
                    //       host implementation, if applicable.
                    //
                    SetupManagerHost(tclManager, false);

                    //
                    // NOTE: Unload the Tcl library and cleanup
                    //       the Tcl objects that we created.
                    //       This is required because it holds
                    //       native resources.  Technically, this
                    //       step is optional if you dispose the
                    //       interpreter (below); however, it is
                    //       good practice.
                    //
                    ReturnCode unloadCode;
                    Result unloadError = null;

                    unloadCode = tclManager.UnloadTcl(
                        UnloadFlags.Default, ref unloadError);

                    if (unloadCode != ReturnCode.Ok)
                        ShowResult(unloadCode, unloadError);

                    //
                    // NOTE: Dispose of the Eagle interpreter now.
                    //       This is required because it may hold
                    //       native resources.
                    //
                    DisposeManager(ref tclManager); /* throw */
                }

                //
                // NOTE: Remove this form from the global
                //       collection.
                //
                lock (syncRoot)
                {
                    if (forms != null)
                        forms.Remove(this);
                }

                //
                // NOTE: This form is now disposed.
                //
                disposed = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This event handler is called when the "New" button is clicked.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the event.
        /// </param>
        private void btnNew_Click(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            if (!disposed)
            {
                //
                // NOTE: Create a new thread.  The new thread will create
                //       another instance of this form.
                //
                Thread thread = Engine.CreateThread(
                    tclManager as Interpreter, CreateFormThreadStart, 0,
                    true, false, true);

                if (thread != null)
                {
                    thread.Name = String.Format(
                        "tclFormThread: {0}", tclManager);

                    thread.Start(args);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This event handler is called when the "Evaluate" button is clicked.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the event.
        /// </param>
        private void btnEvaluate_Click(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            if (!disposed && (tclManager != null))
            {
                //
                // NOTE: Evaluate the contents of the script text box and
                //       place the formatted results into the result text
                //       box.
                //
                ReturnCode code;
                Result result = null;

                code = tclManager.EvaluateTclScript(interpName,
                    txtScript.Text, ref result);

                txtResult.Text = Utility.FormatResult(code, result,
                    tclManager.GetTclInterpreterErrorLine(interpName));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This event handler is called when the "Cancel" button is clicked.
        /// </summary>
        /// <param name="sender">
        /// The source of the event.
        /// </param>
        /// <param name="e">
        /// The data describing the event.
        /// </param>
        private void btnCancel_Click(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            if (!disposed)
            {
                //
                // NOTE: Cancel the active script, if any, for every form
                //       instance we know about.  Please note that we could
                //       surgically cancel the active script for a particular
                //       form instance here also; however, some kind of user
                //       interface to select a particular form instance would
                //       be required in that case.
                //
                ReturnCode code;
                Result error = null;

                code = CancelAll(false, ref error);

                if (code != ReturnCode.Ok)
                    ShowResult(code, error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Forms Helpers
        /// <summary>
        /// Displays the Tcl return code and result to the user via a modal
        /// message box.  No result is returned.
        /// </summary>
        /// <param name="code">
        /// The Tcl return code to display.
        /// </param>
        /// <param name="result">
        /// The Tcl result to display.
        /// </param>
        private static void ShowResult(
            ReturnCode code, /* in */
            Result result    /* in */
            )
        {
            MessageBox.Show(
                Utility.FormatResult(code, result),
                Application.ProductName, MessageBoxButtons.OK,
                Utility.IsSuccess(code, false) ?
                MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to wrap the Tcl manager host with one that captures the
        /// output from the internal DebugOps class.
        /// </summary>
        /// <param name="tclManager">
        /// The Tcl manager object containing the host to wrap.
        /// </param>
        /// <param name="setup">
        /// Non-zero if the Tcl manager host is being wrapped -OR- zero if it
        /// is being unwrapped.
        /// </param>
        private static void SetupManagerHost(
            ITclManager tclManager, /* in */
            bool setup              /* in */
            )
        {
            if (tclManager != null)
            {
                Interpreter interpreter = tclManager as Interpreter;

                if (interpreter != null)
                {
                    ReturnCode hostCode;
                    Result hostError = null;

                    if (setup)
                    {
                        IHost host = null;

                        hostCode = Utility.CopyAndWrapHost(
                            interpreter, typeof(Class10), ref host,
                            ref hostError);

                        if (hostCode == ReturnCode.Ok)
                        {
                            interpreter.Host = host;
                            return;
                        }
                    }
                    else
                    {
                        hostCode = Utility.UnwrapAndDisposeHost(
                            interpreter, ref hostError);

                        if (hostCode == ReturnCode.Ok)
                            return;
                    }

                    ShowResult(hostCode, hostError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes the Tcl manager object and then sets the value of its
        /// parameter to null.  This method may only be called on the primary
        /// thread associated with the specified Tcl manager.
        /// </summary>
        /// <param name="manager">
        /// The Tcl manager object to dispose and then set to null.
        /// </param>
        private static void DisposeManager(
            ref ITclManager tclManager /* in, out */
            )
        {
            if (tclManager != null)
            {
                //
                // NOTE: See if the Tcl manager supports IDisposable (it
                //       should).  If so, dispose it.
                //
                IDisposable disposable = tclManager as IDisposable;

                if (disposable != null)
                {
                    disposable.Dispose();
                    disposable = null;
                }

                tclManager = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to cancel the Tcl script in progress in the all Tcl
        /// interpreters managed by the specified Tcl manager object.  This
        /// method may be called from any thread.
        /// </summary>
        /// <param name="strict">
        /// Non-zero to return an error if the script in progress cannot be
        /// canceled for any of the Tcl interpreters.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will be modified to contain an error
        /// message.
        /// </param>
        /// <returns>
        /// A standard Tcl return code.
        /// </returns>
        private static ReturnCode CancelAll(
            bool strict,     /* in */
            ref Result error /* out */
            )
        {
            lock (syncRoot)
            {
                if (forms != null)
                {
                    try
                    {
                        foreach (TclForm form in forms.Keys)
                        {
                            if (form == null) // NOTE: Redundant?
                                continue;

                            //
                            // NOTE: Grab the Eagle interpreter for this
                            //       form.
                            //
                            ITclManager tclManager = form.tclManager;

                            if (tclManager == null)
                                continue;

                            //
                            // NOTE: Grab the Tcl interpreter name for
                            //       this form.
                            //
                            string interpName = form.interpName;

                            //
                            // NOTE: Cancel the script being evaluated
                            //       for this form instance, if any.
                            //
                            ReturnCode cancelCode;
                            Result cancelError = null;

                            cancelCode = tclManager.CancelTclEvaluate(
                                interpName, null, ref cancelError);

                            if (cancelCode != ReturnCode.Ok)
                            {
                                if (strict)
                                {
                                    //
                                    // NOTE: Strict mode means that we
                                    //       stop upon encountering an
                                    //       error and return that error
                                    //       to the caller.
                                    //
                                    error = cancelError;
                                    return cancelCode;
                                }
                                else
                                {
                                    //
                                    // NOTE: Otherwise, just keep going
                                    //       (after showing the error to
                                    //       the user).
                                    //
                                    ShowResult(cancelCode, cancelError);
                                }
                            }
                        }

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid forms collection";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the starting point for threads created to manage a
        /// new Tcl form and interpreter.
        /// </summary>
        /// <param name="obj">
        /// The Tcl client data to pass to the new Tcl form upon creation.
        /// </param>
        private static void CreateFormThreadStart(
            object obj /* in */
            )
        {
            try
            {
                //
                // NOTE: The data passed to this thread start routine
                //       must be a string array or null.
                //
                IEnumerable<string> args = obj as IEnumerable<string>;
                TclForm form = null;

                try
                {
                    //
                    // NOTE: Create a new instance of the Tcl form, passing
                    //       the arguments we received from the other thread.
                    //
                    form = Create(args);

                    //
                    // NOTE: Upon success, show the form modally.
                    //
                    if (form != null)
                        Application.Run(form);
                }
                finally
                {
                    //
                    // NOTE: Upon completion, successful or otherwise, dispose
                    //       of the form and its contained resources (including
                    //       the Tcl interpreter).
                    //
                    if (form != null)
                    {
                        form.Dispose();
                        form = null;
                    }
                }
            }
            catch (Exception e)
            {
                ShowResult(ReturnCode.Error, e);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// This method is called by all the public methods on this form to
        /// verify that this instance has not been disposed.  An exception will
        /// be thrown if this instance has been disposed and throwing such
        /// exceptions has been enabled for the Tcl manager object associated
        /// with this form.
        /// </summary>
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(
                    tclManager as Interpreter, null))
            {
                throw new ObjectDisposedException(typeof(TclForm).Name);
            }
#endif
        }
        #endregion
    }
}
