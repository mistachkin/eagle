/*
 * FormOps.cs --
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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides helper methods for interacting with Windows Forms
    /// objects, such as marshaling delegate invocations onto the user interface
    /// thread.
    /// </summary>
    [Guid("50ded9bd-a7c3-4764-9e71-e112d16c111c")]
    internal static class FormOps
    {
        #region Windows Forms Methods
        /// <summary>
        /// This method determines whether the control associated with the
        /// specified synchronization object has already been disposed.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The synchronization object to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the associated control has been disposed; otherwise, false.
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

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method invokes the specified delegate using the supplied
        /// synchronization context, discarding any returned value.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object used to marshal the delegate invocation onto its
        /// associated thread, if necessary.
        /// </param>
        /// <param name="method">
        /// The delegate to invoke.
        /// </param>
        /// <param name="strict">
        /// When non-zero, the invocation fails if the synchronization object
        /// has been disposed.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the delegate when it is invoked.
        /// </param>
        /// <returns>
        /// True if the delegate was invoked; otherwise, false.
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
        /// This method invokes the specified delegate using the supplied
        /// synchronization context, returning any resulting value.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The object used to marshal the delegate invocation onto its
        /// associated thread, if necessary.
        /// </param>
        /// <param name="method">
        /// The delegate to invoke.
        /// </param>
        /// <param name="strict">
        /// When non-zero, the invocation fails if the synchronization object
        /// has been disposed.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the value returned by the invoked delegate.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the delegate when it is invoked.
        /// </param>
        /// <returns>
        /// True if the delegate was invoked; otherwise, false.
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
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method asynchronously invokes the specified delegate on the
        /// thread associated with the specified synchronization object,
        /// discarding the resulting asynchronous handle.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The synchronization object whose thread the delegate is invoked on.
        /// This parameter may be null.
        /// </param>
        /// <param name="method">
        /// The delegate to invoke.
        /// </param>
        /// <param name="strict">
        /// Non-zero to skip the invocation when the associated control has
        /// already been disposed.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the delegate.
        /// </param>
        /// <returns>
        /// True if the delegate was invoked; otherwise, false.
        /// </returns>
        public static bool BeginInvoke(
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
        /// This method asynchronously invokes the specified delegate on the
        /// thread associated with the specified synchronization object,
        /// returning the resulting asynchronous handle.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The synchronization object whose thread the delegate is invoked on.
        /// This parameter may be null.
        /// </param>
        /// <param name="method">
        /// The delegate to invoke.
        /// </param>
        /// <param name="strict">
        /// Non-zero to skip the invocation when the associated control has
        /// already been disposed.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the asynchronous result handle for the
        /// invocation, or null if the delegate was invoked synchronously.
        /// </param>
        /// <param name="args">
        /// The arguments to pass to the delegate.
        /// </param>
        /// <returns>
        /// True if the delegate was invoked; otherwise, false.
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

                if (synchronizeInvoke.InvokeRequired)
                {
                    result = synchronizeInvoke.BeginInvoke(method, args);
                }
                else
                {
                    method.DynamicInvoke(args);
                    result = null;
                }

                return true;
            }

            return false;
        }
        #endregion
    }
}
