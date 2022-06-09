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
    [ObjectId("42eb797c-ab29-4f0b-afba-91728bcb59e6")]
    internal static class CommonOps
    {
        #region Private Constants
        private const BindingFlags PrivatePropertyBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic |
            BindingFlags.GetProperty;

        ///////////////////////////////////////////////////////////////////////

        internal const string ProductNameAndVersionFormat = "{0} v{1}";

        ///////////////////////////////////////////////////////////////////////

        internal const string TestFileName = "test.txt";
        internal const string ProgressScriptName = "progress";

        ///////////////////////////////////////////////////////////////////////

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        public static bool IsValidHandle(IntPtr handle)
        {
            return ((handle != IntPtr.Zero) &&
                    (handle != INVALID_HANDLE_VALUE));
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static DialogResult Complain(
            ReturnCode code,
            Result result
            )
        {
            return Complain(Utility.FormatResult(code, result));
        }

        ///////////////////////////////////////////////////////////////////////

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
