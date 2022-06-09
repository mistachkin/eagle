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
    [ObjectId("96cb52f9-7f65-4184-a62f-51b2bd231319")]
    public sealed partial class HostForm : Form
    {
        #region Private Constants
        //
        // NOTE: This is the name of an optional script variable that can be
        //       used to cause this form to start minimized.
        //
        private static readonly string MinimizedVariableName =
            typeof(HostForm).FullName + "_Minimized";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the format used when adding log entris to our text
        //       box.
        //
        private const string LogEntryFormat = "{0:000000}: [{1}]: {2}";

        //
        // NOTE: This is the DateTime format used when adding log entries to
        //       our text box.
        //
        private const string TimeStampFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the interpreter that owns this form instance.
        //
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public void SafeClose()
        {
            CheckDisposed();

            CommonOps.Invoke(this, new DelegateWithNoArgs(delegate()
            {
                Close();
            }), true);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SafeClearLog()
        {
            CheckDisposed();

            return CommonOps.SetText(txtLog, null, true);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static string GetNowString(
            bool utc
            )
        {
            return (utc ? Utility.GetUtcNow() :
                Utility.GetNow()).ToString(TimeStampFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string FormatLogEntry(
            string text
            )
        {
            return String.Format(LogEntryFormat,
                Utility.GetCurrentThreadId(), GetNowString(true), text);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool SafeAppendText(
            string text,
            bool newLine
            )
        {
            return CommonOps.AppendText(txtLog, text, newLine, true);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool disposed;
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
    [ObjectId("2bd360fa-5ee4-467d-afd9-6e3bbc6607ea")]
    internal delegate void DelegateWithNoArgs();
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    #region Static [Windows Forms] Helper Class
    [ObjectId("9c4e3626-ad02-4f9a-b4c4-e9eeb1802ffe")]
    internal static class CommonOps
    {
        #region Windows Forms Threading Methods
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
