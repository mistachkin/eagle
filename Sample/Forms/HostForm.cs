/*
 * HostForm.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.ComponentModel;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace TclSample.Forms
{
    #region Host [Windows Forms] Class
    /// <summary>
    /// This class implements a sample Windows Forms host window that displays
    /// a log of host activity and provides system tray (notification icon)
    /// integration for an Eagle interpreter.
    /// </summary>
    [ObjectId("96cb52f9-7f65-4184-a62f-51b2bd231319")]
    public sealed partial class HostForm : Form
    {
        #region Private Constants
        //
        // NOTE: This is the name of an optional script variable that can be
        //       used to cause this form to start minimized.
        //
        /// <summary>
        /// The name of an optional script variable that, when present, causes
        /// this form to start minimized.
        /// </summary>
        private static readonly string MinimizedVariableName =
            typeof(HostForm).FullName + "_Minimized";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the format used when adding log entris to our text
        //       box.
        //
        /// <summary>
        /// The format string used when adding log entries to the text box.
        /// </summary>
        private const string LogEntryFormat = "{0:000000}: [{1}]: {2}";

        //
        // NOTE: This is the DateTime format used when adding log entries to
        //       our text box.
        //
        /// <summary>
        /// The date and time format string used when adding log entries to the
        /// text box.
        /// </summary>
        private const string TimeStampFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the interpreter that owns this form instance.
        //
        /// <summary>
        /// The interpreter that owns this form instance and is used for all
        /// script evaluation and error reporting.
        /// </summary>
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this class.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this form instance and is used for all
        /// script evaluation and error reporting.
        /// </param>
        public HostForm(
            Interpreter interpreter
            )
        {
            //
            // NOTE: Call the automatically generated code used to initialize
            //       the Windows Forms properties of this object.
            //
            InitializeComponent();

            //
            // NOTE: Save the Eagle interpreter to be used for all script
            //       evaluation and error reporting.
            //
            this.interpreter = interpreter;

            //
            // NOTE: Register the event handlers.
            //
            this.Shown += new EventHandler(HostForm_Shown);
            this.Resize += new EventHandler(HostForm_Resize);

            this.FormClosing += new FormClosingEventHandler(
                HostForm_FormClosing);

            this.Disposed += new EventHandler(HostForm_Disposed);
            notHost.Click += new EventHandler(notHost_Click);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method closes the form in a thread-safe manner, marshaling the
        /// close request onto the user-interface thread if necessary.
        /// </summary>
        public void SafeClose()
        {
            CheckDisposed();

            CommonOps.Invoke(this, new DelegateWithNoArgs(delegate()
            {
                Close();
            }), true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the log text box in a thread-safe manner.
        /// </summary>
        /// <returns>
        /// True if the log text box was successfully cleared; otherwise,
        /// false.
        /// </returns>
        public bool SafeClearLog()
        {
            CheckDisposed();

            return CommonOps.SetText(txtLog, null, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a formatted log entry to the log text box in a
        /// thread-safe manner.
        /// </summary>
        /// <param name="text">
        /// The text of the log entry to be appended.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the log entry;
        /// otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the log entry was successfully appended; otherwise, false.
        /// </returns>
        public bool SafeAppendLog(
            string text,
            bool newLine
            )
        {
            CheckDisposed();

            return SafeAppendText(FormatLogEntry(text), newLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method returns the current date and time formatted using the
        /// standard timestamp format.
        /// </summary>
        /// <param name="utc">
        /// Non-zero to use the current coordinated universal time (UTC); zero
        /// to use the current local time.
        /// </param>
        /// <returns>
        /// The formatted current date and time string.
        /// </returns>
        private static string GetNowString(
            bool utc
            )
        {
            return (utc ? Utility.GetUtcNow() :
                Utility.GetNow()).ToString(TimeStampFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified text as a log entry, prefixing it
        /// with the current thread identifier and timestamp.
        /// </summary>
        /// <param name="text">
        /// The text to be formatted as a log entry.
        /// </param>
        /// <returns>
        /// The formatted log entry string.
        /// </returns>
        private static string FormatLogEntry(
            string text
            )
        {
            return String.Format(LogEntryFormat,
                Utility.GetCurrentThreadId(), GetNowString(true), text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified text to the log text box in a
        /// thread-safe manner.
        /// </summary>
        /// <param name="text">
        /// The text to be appended.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the text; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the text was successfully appended; otherwise, false.
        /// </returns>
        private bool SafeAppendText(
            string text,
            bool newLine
            )
        {
            return CommonOps.AppendText(txtLog, text, newLine, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the notification icon state needs to
        /// be updated to match the current window state.
        /// </summary>
        /// <returns>
        /// True if the notification icon state does not currently match the
        /// window state and needs to be updated; otherwise, false.
        /// </returns>
        private bool NeedToSetNotifyIcon()
        {
            //
            // NOTE: If we are minimized, the ShowInTaskbar property should be
            //       false; otherwise, it should be true.  This method returns
            //       a non-zero value if the previous truth statement does NOT
            //       currently hold true.
            //
            if (this.WindowState == FormWindowState.Minimized)
                return this.ShowInTaskbar;
            else
                return !this.ShowInTaskbar;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the visibility of the notification icon and the
        /// taskbar presence of the form based on whether the form is
        /// minimized.
        /// </summary>
        /// <param name="minimized">
        /// Non-zero if the form is minimized, which makes the notification icon
        /// visible and hides the form from the taskbar; otherwise, zero.
        /// </param>
        private void SetNotifyIcon(
            bool minimized
            )
        {
            if (minimized)
            {
                notHost.Visible = true;
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
                notHost.Visible = false;
            }

            SafeAppendText(FormatLogEntry(String.Format(
                "SetNotifyIcon: set to {0}", minimized ?
                    "visible" : "not visible")), true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Event Handlers
        /// <summary>
        /// This method handles the event raised when the form is first shown.
        /// It optionally starts the form minimized based on a script variable.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The data for the event.
        /// </param>
        private void HostForm_Shown(
            object sender,
            EventArgs e
            )
        {
            try
            {
                //
                // NOTE: Start the hot-key manager form minimized?
                //
                if (interpreter == null)
                    return;

                if (interpreter.DoesVariableExist(VariableFlags.None,
                        MinimizedVariableName) == ReturnCode.Ok)
                {
                    this.WindowState = FormWindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                Utility.Complain(interpreter, ReturnCode.Error, ex);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the event raised when the form is resized.  It
        /// updates the notification icon state to match the current window
        /// state when necessary.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The data for the event.
        /// </param>
        private void HostForm_Resize(
            object sender,
            EventArgs e
            )
        {
            //
            // NOTE: This event handler MUST only deal with "size" changes that
            //       require modification of the notification icon state.
            //
            if (!NeedToSetNotifyIcon())
                return;

            //
            // NOTE: Make sure the system tray icon is visible if and only if
            //       the current window state is minimized.
            //
            /* NO RESULT */
            SetNotifyIcon(this.WindowState == FormWindowState.Minimized);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the event raised when the form is about to
        /// close.  It ensures that the resources for the notification icon are
        /// released properly.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The data for the event.
        /// </param>
        private void HostForm_FormClosing(
            object sender,
            FormClosingEventArgs e
            )
        {
            //
            // NOTE: Before closing, make sure that the resources for the
            //       notification icon get disposed of properly.
            //
            SetNotifyIcon(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the event raised when the form has been
        /// disposed.  It notifies the parent interpreter host that this form
        /// should no longer be used.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The data for the event.
        /// </param>
        private void HostForm_Disposed(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            if (!disposed)
            {
                //
                // NOTE: Attempt to "notify" our [parent] interpreter host
                //       that this host form is being disposed and should
                //       no longer be used.
                //
                if ((interpreter != null) && !interpreter.Disposed)
                {
                    IHost host = interpreter.Host;

                    if (host != null)
                    {
                        Class10 class10 = host as Class10;

                        if (class10 != null)
                            class10.ResetHostForm();
                    }
                }

                //
                // NOTE: This form is now disposed.
                //
                disposed = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the event raised when the notification icon is
        /// clicked.  It restores the form to its normal state when it is
        /// currently minimized.
        /// </summary>
        /// <param name="sender">
        /// The object that raised the event.
        /// </param>
        /// <param name="e">
        /// The data for the event.
        /// </param>
        private void notHost_Click(
            object sender,
            EventArgs e
            )
        {
            //
            // NOTE: If the current window state is minimized, reset it to
            //       normal.  This will cause the Resize event to be fired,
            //       thus hiding this system tray icon.
            //
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object instance has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this object instance has been
        /// disposed and the interpreter is configured to throw in that case.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(HostForm).Name);
#endif
        }
        #endregion
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Private [Windows Forms] Delegates
    /// <summary>
    /// This delegate represents a callback that accepts no arguments and
    /// returns no value, used to marshal work onto the user-interface thread.
    /// </summary>
    [ObjectId("2bd360fa-5ee4-467d-afd9-6e3bbc6607ea")]
    internal delegate void DelegateWithNoArgs();
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Static [Windows Forms] Helper Class
    /// <summary>
    /// This class provides static helper methods for performing Windows Forms
    /// operations in a thread-safe manner.
    /// </summary>
    [ObjectId("9c4e3626-ad02-4f9a-b4c4-e9eeb1802ffe")]
    internal static class CommonOps
    {
        #region Windows Forms Threading Methods
        /// <summary>
        /// This method determines whether the specified object is a control
        /// that has already been disposed.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object to be checked, which may be a control.
        /// </param>
        /// <returns>
        /// True if the specified object is a control that has been disposed;
        /// otherwise, false.
        /// </returns>
        private static bool IsDisposed(
            ISynchronizeInvoke synchronizeInvoke
            )
        {
            if (synchronizeInvoke != null)
            {
                Control control = synchronizeInvoke as Control;

                if ((control != null) && control.IsDisposed)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the text of the specified control in a thread-safe
        /// manner, marshaling the operation onto the user-interface thread.
        /// </summary>
        /// <param name="control">
        /// The control whose text is to be set.
        /// </param>
        /// <param name="text">
        /// The text to be set on the control.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the operation asynchronously; zero to perform it
        /// synchronously.
        /// </param>
        /// <returns>
        /// True if the operation was successfully marshaled; otherwise, false.
        /// </returns>
        public static bool SetText(
            Control control,
            string text,
            bool asynchronous
            )
        {
            if (asynchronous)
            {
                return BeginInvoke(control, new DelegateWithNoArgs(delegate()
                {
                    control.Text = text;
                }), true);
            }
            else
            {
                return Invoke(control, new DelegateWithNoArgs(delegate()
                {
                    control.Text = text;
                }), true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends text to the specified text box in a thread-safe
        /// manner, marshaling the operation onto the user-interface thread.
        /// </summary>
        /// <param name="textBox">
        /// The text box to which the text is to be appended.
        /// </param>
        /// <param name="text">
        /// The text to be appended.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the text; otherwise,
        /// zero.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the operation asynchronously; zero to perform it
        /// synchronously.
        /// </param>
        /// <returns>
        /// True if the operation was successfully marshaled; otherwise, false.
        /// </returns>
        public static bool AppendText(
            TextBoxBase textBox,
            string text,
            bool newLine,
            bool asynchronous
            )
        {
            if (asynchronous)
            {
                return BeginInvoke(textBox, new DelegateWithNoArgs(delegate()
                {
                    textBox.AppendText(text);

                    if (newLine)
                        textBox.AppendText(Environment.NewLine);
                }), true);
            }
            else
            {
                return Invoke(textBox, new DelegateWithNoArgs(delegate()
                {
                    textBox.AppendText(text);

                    if (newLine)
                        textBox.AppendText(Environment.NewLine);
                }), true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method synchronously invokes the specified delegate, marshaling
        /// the call onto the user-interface thread when necessary.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object used to marshal the call onto the user-interface thread.
        /// </param>
        /// <param name="method">
        /// The delegate to be invoked.
        /// </param>
        /// <param name="strict">
        /// Non-zero to fail when the target object has already been disposed;
        /// otherwise, zero.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the delegate.
        /// </param>
        /// <returns>
        /// True if the delegate was successfully invoked; otherwise, false.
        /// </returns>
        public static bool Invoke(
            ISynchronizeInvoke synchronizeInvoke,
            Delegate method,
            bool strict,
            params object[] args
            )
        {
            object result = null;

            return Invoke(synchronizeInvoke, method, strict, ref result, args);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method synchronously invokes the specified delegate, marshaling
        /// the call onto the user-interface thread when necessary and returning
        /// the value produced by the delegate.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object used to marshal the call onto the user-interface thread.
        /// </param>
        /// <param name="method">
        /// The delegate to be invoked.
        /// </param>
        /// <param name="strict">
        /// Non-zero to fail when the target object has already been disposed;
        /// otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the value produced by invoking the delegate.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the delegate.
        /// </param>
        /// <returns>
        /// True if the delegate was successfully invoked; otherwise, false.
        /// </returns>
        private static bool Invoke(
            ISynchronizeInvoke synchronizeInvoke,
            Delegate method,
            bool strict,
            ref object result,
            params object[] args
            )
        {
            if (synchronizeInvoke != null)
            {
                if (strict && IsDisposed(synchronizeInvoke))
                    return false;

                if (synchronizeInvoke.InvokeRequired)
                    result = synchronizeInvoke.Invoke(method, args);
                else
                    result = method.DynamicInvoke(args);

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method asynchronously invokes the specified delegate,
        /// marshaling the call onto the user-interface thread.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object used to marshal the call onto the user-interface thread.
        /// </param>
        /// <param name="method">
        /// The delegate to be invoked.
        /// </param>
        /// <param name="strict">
        /// Non-zero to fail when the target object has already been disposed;
        /// otherwise, zero.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the delegate.
        /// </param>
        /// <returns>
        /// True if the delegate was successfully scheduled for invocation;
        /// otherwise, false.
        /// </returns>
        private static bool BeginInvoke(
            ISynchronizeInvoke synchronizeInvoke,
            Delegate method,
            bool strict,
            params object[] args
            )
        {
            IAsyncResult result = null;

            return BeginInvoke(
                synchronizeInvoke, method, strict, ref result, args);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method asynchronously invokes the specified delegate,
        /// marshaling the call onto the user-interface thread and returning the
        /// associated asynchronous result.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object used to marshal the call onto the user-interface thread.
        /// </param>
        /// <param name="method">
        /// The delegate to be invoked.
        /// </param>
        /// <param name="strict">
        /// Non-zero to fail when the target object has already been disposed;
        /// otherwise, zero.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the asynchronous result associated with the
        /// scheduled invocation.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the delegate.
        /// </param>
        /// <returns>
        /// True if the delegate was successfully scheduled for invocation;
        /// otherwise, false.
        /// </returns>
        private static bool BeginInvoke(
            ISynchronizeInvoke synchronizeInvoke,
            Delegate method,
            bool strict,
            ref IAsyncResult result,
            params object[] args
            )
        {
            if (synchronizeInvoke != null)
            {
                if (strict && IsDisposed(synchronizeInvoke))
                    return false;

                result = synchronizeInvoke.BeginInvoke(method, args);
                return true;
            }

            return false;
        }
        #endregion
    }
    #endregion
}
