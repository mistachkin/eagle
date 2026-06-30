/*
 * CommonOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides common helper methods shared by the test
    /// application, including helpers for working with Windows Forms control
    /// handles and for reporting errors to the user.
    /// </summary>
    [ObjectId("42eb797c-ab29-4f0b-afba-91728bcb59e6")]
    internal static class CommonOps
    {
        #region Private Constants
        /// <summary>
        /// The reflection binding flags used to read a non-public instance
        /// property.
        /// </summary>
        private const BindingFlags PrivatePropertyBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic |
            BindingFlags.GetProperty;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to combine a product name and version into a
        /// single display string.
        /// </summary>
        internal const string ProductNameAndVersionFormat = "{0} v{1}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default file name used for test file operations.
        /// </summary>
        internal const string TestFileName = "test.txt";

        /// <summary>
        /// The name of the script used to report progress.
        /// </summary>
        internal const string ProgressScriptName = "progress";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The sentinel value that represents an invalid native handle.
        /// </summary>
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method determines whether the specified native handle is
        /// valid (i.e. neither the zero handle nor the invalid handle
        /// sentinel).
        /// </summary>
        /// <param name="handle">
        /// The native handle to check.
        /// </param>
        /// <returns>
        /// True if the handle is valid; otherwise, false.
        /// </returns>
        public static bool IsValidHandle(IntPtr handle)
        {
            return ((handle != IntPtr.Zero) &&
                    (handle != INVALID_HANDLE_VALUE));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the native window handle of the specified
        /// control without forcing the handle to be created.  It uses
        /// reflection to read the internal handle property so that querying
        /// the handle does not require thread affinity.
        /// </summary>
        /// <param name="control">
        /// The control whose native handle is to be obtained.
        /// </param>
        /// <returns>
        /// The native handle of the control, or the zero handle if the control
        /// is null or its handle could not be obtained.
        /// </returns>
        public static IntPtr GetHandle(Control control)
        {
            IntPtr result = IntPtr.Zero;

            if (control != null)
            {
                try
                {
                    //
                    // HACK: This should not be necessary.  However, it does
                    //       appear that a control (including a Form) will not
                    //       allow you to simply query the handle [to check it
                    //       against null] without attempting to automatically
                    //       create it first (which requires thread affinity).
                    //
                    Type type = control.GetType();

                    result = (IntPtr)type.InvokeMember("HandleInternal",
                        PrivatePropertyBindingFlags, null, control, null);
                }
                catch
                {
                    // do nothing.
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports an error to the user by formatting the specified
        /// return code and result into a message and displaying it.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the error.
        /// </param>
        /// <param name="result">
        /// The result (e.g. value or error message) associated with the error.
        /// </param>
        /// <returns>
        /// The dialog result produced by displaying the message.
        /// </returns>
        public static DialogResult Complain(
            ReturnCode code,
            Result result
            )
        {
            return Complain(Utility.FormatResult(code, result));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports an error to the user.  When the session is
        /// interactive, the specified text is shown in an error message box;
        /// otherwise, it is written to the trace output.
        /// </summary>
        /// <param name="text">
        /// The error message text to report.
        /// </param>
        /// <returns>
        /// The dialog result produced by displaying the message, or
        /// <see cref="DialogResult.OK" /> when the session is not interactive.
        /// </returns>
        public static DialogResult Complain(
            string text
            )
        {
            if (SystemInformation.UserInteractive)
            {
                return MessageBox.Show(text, null,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Trace.WriteLine(text);

                return DialogResult.OK;
            }
        }
        #endregion
    }
}
