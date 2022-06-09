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
    [Guid("50ded9bd-a7c3-4764-9e71-e112d16c111c")]
    internal static class FormOps
    {
        #region Windows Forms Methods
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
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
