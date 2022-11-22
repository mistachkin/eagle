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

#if DRAWING
using System.Drawing;
#endif

using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Components.Private
{
    [ObjectId("4f00a819-09bc-4f50-99ee-4b159bb78725")]
    internal static class FormOps
    {
        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        private static bool DoEventsReThrow = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the total number of times that Application.DoEvents
        //       has been called in this AppDomain.
        //
        private static long DoEventsCount = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUserInteractive()
        {
            return SystemInformation.UserInteractive;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? YesOrNo(
            string text,    /* in */
            string caption, /* in */
            bool? @default  /* in */
            )
        {
            return YesOrNo(GetMessageBoxOwner(), text, caption, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? YesOrNo(
            IWin32Window owner, /* in */
            string text,        /* in */
            string caption,     /* in */
            bool? @default      /* in */
            )
        {
            if (WindowOps.IsInteractive())
            {
                return MessageBox.Show(
                    owner, text, caption, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? YesOrNoOrCancel(
            string text,    /* in */
            string caption, /* in */
            bool? @default  /* in */
            )
        {
            return YesOrNoOrCancel(
                GetMessageBoxOwner(), text, caption, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? YesOrNoOrCancel(
            IWin32Window owner, /* in */
            string text,        /* in */
            string caption,     /* in */
            bool? @default      /* in */
            )
        {
            if (WindowOps.IsInteractive())
            {
                DialogResult dialogResult = MessageBox.Show(
                    owner, text, caption, MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (dialogResult)
                {
                    case DialogResult.Yes:
                        return true;
                    case DialogResult.No:
                        return false;
                    case DialogResult.Cancel:
                        return null;
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static DialogResult YesOrNoOrCancel(
            string text,          /* in */
            string caption,       /* in */
            DialogResult @default /* in */
            )
        {
            return YesOrNoOrCancel(
                GetMessageBoxOwner(), text, caption, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static DialogResult YesOrNoOrCancel(
            IWin32Window owner,   /* in */
            string text,          /* in */
            string caption,       /* in */
            DialogResult @default /* in */
            )
        {
            if (WindowOps.IsInteractive())
            {
                return MessageBox.Show(
                    owner, text, caption, MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IWin32Window GetMessageBoxOwner()
        {
#if NATIVE && WINDOWS && TEST
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                IntPtr handle = WindowOps.GetIconWindow();

                if (handle != IntPtr.Zero)
                    return new _Tests.Default.Win32Window(handle);
            }
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static DialogResult Complain( /* NOT USED */
            ReturnCode code, /* in */
            Result result    /* in */
            )
        {
            return Complain(ResultOps.Format(code, result));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static DialogResult Complain(
            string message /* in */
            )
        {
            if (WindowOps.IsInteractive() && GlobalState.IsPrimaryThread())
            {
                return MessageBox.Show(
                    message, Application.ProductName, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                DebugOps.Log(String.Format(
                    "{0}{1}", message, Environment.NewLine));

                return DialogResult.OK;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetHandle(
            Control control,   /* in */
            ref IntPtr handle, /* out */
            ref Result error   /* out */
            )
        {
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

                    handle = (IntPtr)type.InvokeMember(
                        "HandleInternal", ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateInstanceGetProperty,
                        true), null, control, null);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid control";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetHandle(
            Menu menu,         /* in */
            ref IntPtr handle, /* out */
            ref Result error   /* out */
            )
        {
            if (menu != null)
            {
                try
                {
                    //
                    // HACK: This should not be necessary.  However, it does
                    //       appear that a menu will not allow you to simply
                    //       query the handle [to check it against null]
                    //       without attempting to automatically create it
                    //       first (which requires thread affinity).
                    //
                    Type type = menu.GetType();

                    handle = (IntPtr)type.InvokeMember(
                        "handle", ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateInstanceGetField,
                        true), null, menu, null);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid menu";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetDoEventsCount()
        {
            return Interlocked.CompareExchange(ref DoEventsCount, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DoEvents()
        {
            try
            {
                Application.DoEvents(); /* throw */
                Interlocked.Increment(ref DoEventsCount);
            }
            catch
            {
                if (DoEventsReThrow &&
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    throw;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region ISynchronizeInvoke Helper Methods
        private static ISynchronizeInvoke GetInvoker(
            object @object /* in */
            )
        {
            if (@object == null)
                return null;

            return @object as ISynchronizeInvoke;
        }

        ///////////////////////////////////////////////////////////////////////

        private static object DoCallback(
            ISynchronizeInvoke synchronizeInvoke, /* in */
            GenericCallback callback,             /* in */
            bool asynchronous,                    /* in */
            params object[] args                  /* in */
            )
        {
            if (synchronizeInvoke.InvokeRequired)
            {
                if (asynchronous)
                    return synchronizeInvoke.BeginInvoke(callback, args);
                else
                    return synchronizeInvoke.Invoke(callback, args);
            }
            else
            {
                callback();
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToString(
            object @object, /* in */
            bool display    /* in */
            )
        {
            if (@object == null)
                return display ? FormatOps.DisplayNull : null;

            try
            {
                string result = null;

                GenericCallback callback = new GenericCallback(delegate()
                {
                    //
                    // TODO: Maybe this should just use the ToString
                    //       method directly?
                    //
                    result = StringOps.GetStringFromObject(@object);
                });

                ISynchronizeInvoke synchronizeInvoke = GetInvoker(@object);

                if (synchronizeInvoke != null)
                {
                    /* IGNORED */
                    DoCallback(synchronizeInvoke, callback, false);
                }
                else
                {
                    //
                    // TODO: Maybe this should just use the ToString
                    //       method directly?
                    //
                    result = StringOps.GetStringFromObject(@object);
                }

                return display ? FormatOps.WrapOrNull(result) : result;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static Form FindForm(
            Control control /* in */
            )
        {
            if (control == null)
                return null;

            try
            {
                Form form = null;

                GenericCallback callback = new GenericCallback(delegate()
                {
                    form = control.FindForm();
                });

                /* IGNORED */
                DoCallback(control, callback, false);

                return form;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ToggleReadOnlyText(
            TextBoxBase textBoxBase, /* in */
            bool asynchronous        /* in */
            )
        {
            if (textBoxBase == null)
                return false;

            try
            {
                GenericCallback callback = new GenericCallback(delegate()
                {
                    textBoxBase.ReadOnly = !textBoxBase.ReadOnly;
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, asynchronous);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SelectText(
            TextBoxBase textBoxBase, /* in */
            bool asynchronous        /* in */
            )
        {
            if (textBoxBase == null)
                return false;

            try
            {
                GenericCallback callback = new GenericCallback(delegate()
                {
                    textBoxBase.SelectAll();
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, asynchronous);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DeselectText(
            TextBoxBase textBoxBase, /* in */
            bool asynchronous        /* in */
            )
        {
            if (textBoxBase == null)
                return false;

            try
            {
                GenericCallback callback = new GenericCallback(delegate()
                {
                    textBoxBase.DeselectAll();
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, asynchronous);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetText(
            TextBox textBox /* in */
            )
        {
            bool selected;

            return GetText(textBox, out selected);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetText(
            TextBoxBase textBoxBase, /* in */
            out bool selected        /* out */
            )
        {
            selected = false;

            if (textBoxBase == null)
                return null;

            try
            {
                string text = null;
                bool localSelected = false;

                GenericCallback callback = new GenericCallback(delegate()
                {
                    text = textBoxBase.SelectedText;

                    if (!String.IsNullOrEmpty(text))
                        localSelected = true;
                    else
                        text = textBoxBase.Text;
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, false);

                selected = localSelected;
                return text;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetText(
            TextBoxBase textBoxBase, /* in */
            string text,             /* in */
            bool selected,           /* in */
            bool asynchronous        /* in */
            )
        {
            if (textBoxBase == null)
                return false;

            try
            {
                GenericCallback callback = new GenericCallback(delegate()
                {
                    if (selected)
                        textBoxBase.SelectedText = text;
                    else
                        textBoxBase.Text = text;
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, asynchronous);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FormOps).Name,
                    TracePriority.UserInterfaceError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ClearText(
            TextBox textBox,  /* in */
            bool asynchronous /* in */
            )
        {
            Result error = null;

            return ClearText(textBox, asynchronous, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ClearText(
            TextBoxBase textBoxBase, /* in */
            bool asynchronous,       /* in */
            ref Result error         /* out */
            )
        {
            if (textBoxBase == null)
            {
                error = "invalid text box";
                return ReturnCode.Error;
            }

            try
            {
                GenericCallback callback = new GenericCallback(delegate()
                {
                    textBoxBase.Clear();
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, asynchronous);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AppendToText(
            TextBoxBase textBoxBase, /* in */
            string text,             /* in */
            bool asynchronous,       /* in */
            ref Result error         /* out */
            )
        {
            if (textBoxBase == null)
            {
                error = "invalid text box";
                return ReturnCode.Error;
            }

            try
            {
                GenericCallback callback = new GenericCallback(delegate()
                {
                    textBoxBase.AppendText(text);
                });

                /* IGNORED */
                DoCallback(textBoxBase, callback, asynchronous);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Control GetFirstControl(
            Control control /* in */
            )
        {
            if (control == null)
                return null;

            Control.ControlCollection controls = control.Controls;

            if ((controls == null) || (controls.Count == 0))
                return null;

            return controls[0];
        }

        ///////////////////////////////////////////////////////////////////////

#if DRAWING
        public static bool ResizeControl(
            Control control, /* in */
            Size size        /* in */
            )
        {
            if (control == null)
                return false;

            GenericCallback callback = new GenericCallback(delegate()
            {
                control.SuspendLayout();
                control.Size = size;
                control.ResumeLayout();
            });

            /* IGNORED */
            DoCallback(control, callback, false);

            return true;
        }
#endif
    }
}
