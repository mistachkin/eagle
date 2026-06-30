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

using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides various helper methods for working with Windows
    /// Forms user interface elements, including displaying message box
    /// prompts, pumping the message loop, querying control handles, and
    /// manipulating text box contents, while accounting for thread affinity
    /// and non-interactive (automation) scenarios.
    /// </summary>
    [ObjectId("4f00a819-09bc-4f50-99ee-4b159bb78725")]
    internal static class FormOps
    {
        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When this value is non-zero, exceptions caught while pumping the
        /// message loop via the <see cref="DoEvents" /> method will be
        /// re-thrown on Windows operating systems.
        /// </summary>
        private static bool DoEventsReThrow = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the total number of times that Application.DoEvents
        //       has been called in this AppDomain.
        //
        /// <summary>
        /// The total number of times that the <see cref="DoEvents" /> method
        /// has successfully pumped the message loop in this application domain.
        /// </summary>
        private static long DoEventsCount = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current process is running in a
        /// user interactive context.
        /// </summary>
        /// <returns>
        /// True if the current process is running in a user interactive
        /// context; otherwise, false.
        /// </returns>
        public static bool IsUserInteractive()
        {
            return SystemInformation.UserInteractive;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method displays a message box prompt containing "Yes" and
        /// "No" buttons, using the default message box owner window.
        /// </summary>
        /// <param name="text">
        /// The message text to display within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption to display in the title bar of the prompt.
        /// </param>
        /// <param name="default">
        /// The value to return when an explicit answer cannot be obtained,
        /// such as when running in a non-interactive context.
        /// </param>
        /// <returns>
        /// True if the user answered yes, false if the user answered no;
        /// otherwise, the value of the <paramref name="default" /> parameter.
        /// </returns>
        public static bool? YesOrNo(
            string text,    /* in */
            string caption, /* in */
            bool? @default  /* in */
            )
        {
            return YesOrNo(GetMessageBoxOwner(), text, caption, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method displays a message box prompt containing "Yes" and
        /// "No" buttons, owned by the specified window.
        /// </summary>
        /// <param name="owner">
        /// The window that will own the displayed prompt, or null for none.
        /// </param>
        /// <param name="text">
        /// The message text to display within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption to display in the title bar of the prompt.
        /// </param>
        /// <param name="default">
        /// The value to return when an explicit answer cannot be obtained,
        /// such as when running in a non-interactive context.
        /// </param>
        /// <returns>
        /// True if the user answered yes, false if the user answered no;
        /// otherwise, the value of the <paramref name="default" /> parameter.
        /// </returns>
        public static bool? YesOrNo(
            IWin32Window owner, /* in */
            string text,        /* in */
            string caption,     /* in */
            bool? @default      /* in */
            )
        {
            DialogResult? dialogResult;

            if (WindowOps.IsInteractive())
            {
                dialogResult = MessageBox.Show(
                    owner, text, caption, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
            }
            else
            {
                dialogResult = GetPromptResultForAutomation(
                    text, caption, null);
            }

            if (dialogResult != null)
                return ((DialogResult)dialogResult) == DialogResult.Yes;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method displays a message box prompt containing "Yes", "No",
        /// and "Cancel" buttons, using the default message box owner window.
        /// </summary>
        /// <param name="text">
        /// The message text to display within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption to display in the title bar of the prompt.
        /// </param>
        /// <param name="default">
        /// The value to return when an explicit answer cannot be obtained,
        /// such as when running in a non-interactive context.
        /// </param>
        /// <returns>
        /// True if the user answered yes, false if the user answered no, null
        /// if the user cancelled; otherwise, the value of the
        /// <paramref name="default" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method displays a message box prompt containing "Yes", "No",
        /// and "Cancel" buttons, owned by the specified window.
        /// </summary>
        /// <param name="owner">
        /// The window that will own the displayed prompt, or null for none.
        /// </param>
        /// <param name="text">
        /// The message text to display within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption to display in the title bar of the prompt.
        /// </param>
        /// <param name="default">
        /// The value to return when an explicit answer cannot be obtained,
        /// such as when running in a non-interactive context.
        /// </param>
        /// <returns>
        /// True if the user answered yes, false if the user answered no, null
        /// if the user cancelled; otherwise, the value of the
        /// <paramref name="default" /> parameter.
        /// </returns>
        public static bool? YesOrNoOrCancel(
            IWin32Window owner, /* in */
            string text,        /* in */
            string caption,     /* in */
            bool? @default      /* in */
            )
        {
            DialogResult? dialogResult;

            if (WindowOps.IsInteractive())
            {
                dialogResult = MessageBox.Show(
                    owner, text, caption,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
            }
            else
            {
                dialogResult = GetPromptResultForAutomation(
                    text, caption, null);
            }

            if (dialogResult != null)
            {
                switch ((DialogResult)dialogResult)
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

        /// <summary>
        /// This method displays a message box prompt containing "Yes", "No",
        /// and "Cancel" buttons, using the default message box owner window,
        /// and returns the raw dialog result.
        /// </summary>
        /// <param name="text">
        /// The message text to display within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption to display in the title bar of the prompt.
        /// </param>
        /// <param name="default">
        /// The value to return when an explicit answer cannot be obtained,
        /// such as when running in a non-interactive context.
        /// </param>
        /// <returns>
        /// The <see cref="DialogResult" /> value corresponding to the button
        /// selected by the user; otherwise, the value of the
        /// <paramref name="default" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method displays a message box prompt containing "Yes", "No",
        /// and "Cancel" buttons, owned by the specified window, and returns
        /// the raw dialog result.
        /// </summary>
        /// <param name="owner">
        /// The window that will own the displayed prompt, or null for none.
        /// </param>
        /// <param name="text">
        /// The message text to display within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption to display in the title bar of the prompt.
        /// </param>
        /// <param name="default">
        /// The value to return when an explicit answer cannot be obtained,
        /// such as when running in a non-interactive context.
        /// </param>
        /// <returns>
        /// The <see cref="DialogResult" /> value corresponding to the button
        /// selected by the user; otherwise, the value of the
        /// <paramref name="default" /> parameter.
        /// </returns>
        public static DialogResult YesOrNoOrCancel(
            IWin32Window owner,   /* in */
            string text,          /* in */
            string caption,       /* in */
            DialogResult @default /* in */
            )
        {
            DialogResult? dialogResult;

            if (WindowOps.IsInteractive())
            {
                dialogResult = MessageBox.Show(
                    owner, text, caption,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
            }
            else
            {
                dialogResult = GetPromptResultForAutomation(
                    text, caption, null);
            }

            if (dialogResult != null)
                return (DialogResult)dialogResult;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the result that should be used in place of a
        /// message box prompt when running in a non-interactive (automation)
        /// context.
        /// </summary>
        /// <param name="text">
        /// The message text that would have been displayed within the prompt.
        /// </param>
        /// <param name="caption">
        /// The caption that would have been displayed in the title bar of the
        /// prompt.
        /// </param>
        /// <param name="default">
        /// The default value to use when no specific automation result is
        /// available.
        /// </param>
        /// <returns>
        /// The <see cref="DialogResult" /> value to use for automation, or
        /// null if none is available.
        /// </returns>
        private static DialogResult? GetPromptResultForAutomation(
            string text,           /* in */
            string caption,        /* in */
            DialogResult? @default /* in */
            )
        {
            return WindowOps.GetPromptResultForAutomation<DialogResult>(
                text, caption, @default, null, AutomationFlags.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the window that should own any message box
        /// prompts displayed by this class.
        /// </summary>
        /// <returns>
        /// The window to use as the owner of message box prompts, or null if
        /// no suitable owner window is available.
        /// </returns>
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
        /// <summary>
        /// This method formats the specified return code and result and then
        /// reports the resulting message to the user.
        /// </summary>
        /// <param name="code">
        /// The return code to be formatted into the complaint message.
        /// </param>
        /// <param name="result">
        /// The result to be formatted into the complaint message.
        /// </param>
        /// <returns>
        /// The <see cref="DialogResult" /> value produced while reporting the
        /// message to the user.
        /// </returns>
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

        /// <summary>
        /// This method reports the specified message to the user.  When
        /// running interactively on the primary thread, the message is shown
        /// using an error message box; otherwise, it is written to the debug
        /// log.
        /// </summary>
        /// <param name="message">
        /// The message to be reported to the user.
        /// </param>
        /// <returns>
        /// The <see cref="DialogResult" /> value produced while reporting the
        /// message to the user.
        /// </returns>
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

        /// <summary>
        /// This method queries the native window handle associated with the
        /// specified control without forcing the handle to be created.
        /// </summary>
        /// <param name="control">
        /// The control whose native window handle is to be queried.
        /// </param>
        /// <param name="handle">
        /// Upon success, receives the native window handle associated with the
        /// control.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method queries the native handle associated with the specified
        /// menu without forcing the handle to be created.
        /// </summary>
        /// <param name="menu">
        /// The menu whose native handle is to be queried.
        /// </param>
        /// <param name="handle">
        /// Upon success, receives the native handle associated with the menu.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method queries the total number of times that the message loop
        /// has been successfully pumped in this application domain.
        /// </summary>
        /// <returns>
        /// The total number of times that the <see cref="DoEvents" /> method
        /// has successfully pumped the message loop.
        /// </returns>
        public static long GetDoEventsCount()
        {
            return Interlocked.CompareExchange(ref DoEventsCount, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pumps the Windows Forms message loop once, processing
        /// any pending messages, and increments the count of successful
        /// pumping operations.  Exceptions are normally suppressed unless
        /// re-throwing is enabled on a Windows operating system.
        /// </summary>
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
        /// <summary>
        /// This method obtains the <see cref="ISynchronizeInvoke" /> interface
        /// for the specified object, if it is supported.
        /// </summary>
        /// <param name="object">
        /// The object for which the synchronizing invoker is to be obtained.
        /// </param>
        /// <returns>
        /// The <see cref="ISynchronizeInvoke" /> interface for the object, or
        /// null if the object is null or does not support that interface.
        /// </returns>
        private static ISynchronizeInvoke GetInvoker(
            object @object /* in */
            )
        {
            if (@object == null)
                return null;

            return @object as ISynchronizeInvoke;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified callback, marshaling the call to
        /// the thread that owns the supplied synchronizing invoker when that
        /// is required for thread affinity.
        /// </summary>
        /// <param name="synchronizeInvoke">
        /// The synchronizing invoker that owns the thread affinity, used to
        /// marshal the callback when necessary.
        /// </param>
        /// <param name="callback">
        /// The callback delegate to be invoked.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to invoke the callback asynchronously when marshaling is
        /// required; otherwise, the callback is invoked synchronously.
        /// </param>
        /// <param name="args">
        /// The arguments to be passed to the callback when it is invoked.
        /// </param>
        /// <returns>
        /// The value returned by the marshaling operation, or null when the
        /// callback is invoked directly on the current thread.
        /// </returns>
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

        /// <summary>
        /// This method converts the specified object to its string
        /// representation, marshaling the conversion to the owning thread when
        /// the object requires thread affinity.
        /// </summary>
        /// <param name="object">
        /// The object to be converted to a string.
        /// </param>
        /// <param name="display">
        /// Non-zero to format the result for display, including handling of a
        /// null object and wrapping of the resulting string; otherwise, the
        /// raw string value is returned.
        /// </param>
        /// <returns>
        /// The string representation of the object, or null if the conversion
        /// could not be performed.
        /// </returns>
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

        /// <summary>
        /// This method finds the <see cref="Form" /> that contains the
        /// specified control, marshaling the query to the owning thread when
        /// thread affinity is required.
        /// </summary>
        /// <param name="control">
        /// The control whose containing form is to be found.
        /// </param>
        /// <returns>
        /// The form that contains the specified control, or null if it cannot
        /// be determined.
        /// </returns>
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

        /// <summary>
        /// This method toggles the read-only state of the specified text box,
        /// marshaling the change to the owning thread when thread affinity is
        /// required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box whose read-only state is to be toggled.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <returns>
        /// True if the read-only state was successfully toggled; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method selects all of the text in the specified text box,
        /// marshaling the change to the owning thread when thread affinity is
        /// required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box whose text is to be selected.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <returns>
        /// True if the text was successfully selected; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method removes the selection from the text in the specified
        /// text box, marshaling the change to the owning thread when thread
        /// affinity is required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box whose text is to be deselected.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <returns>
        /// True if the text was successfully deselected; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method obtains the text from the specified text box, returning
        /// the selected text when there is a selection and the full text
        /// otherwise.
        /// </summary>
        /// <param name="textBox">
        /// The text box whose text is to be obtained.
        /// </param>
        /// <returns>
        /// The text obtained from the text box, or null if it could not be
        /// obtained.
        /// </returns>
        public static string GetText(
            TextBox textBox /* in */
            )
        {
            bool selected;

            return GetText(textBox, out selected);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the text from the specified text box, returning
        /// the selected text when there is a selection and the full text
        /// otherwise, and reports whether a selection was present.  The query
        /// is marshaled to the owning thread when thread affinity is required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box whose text is to be obtained.
        /// </param>
        /// <param name="selected">
        /// Upon return, non-zero if the returned text came from a non-empty
        /// selection; otherwise, false.
        /// </param>
        /// <returns>
        /// The text obtained from the text box, or null if it could not be
        /// obtained.
        /// </returns>
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

        /// <summary>
        /// This method sets the text of the specified text box, optionally
        /// replacing only the current selection, marshaling the change to the
        /// owning thread when thread affinity is required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box whose text is to be set.
        /// </param>
        /// <param name="text">
        /// The text to be placed into the text box.
        /// </param>
        /// <param name="selected">
        /// Non-zero to replace only the currently selected text; otherwise,
        /// the entire text is replaced.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <returns>
        /// True if the text was successfully set; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method clears all of the text from the specified text box.
        /// </summary>
        /// <param name="textBox">
        /// The text box whose text is to be cleared.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ClearText(
            TextBox textBox,  /* in */
            bool asynchronous /* in */
            )
        {
            Result error = null;

            return ClearText(textBox, asynchronous, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears all of the text from the specified text box,
        /// marshaling the change to the owning thread when thread affinity is
        /// required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box whose text is to be cleared.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method appends the specified text to the contents of the
        /// specified text box, marshaling the change to the owning thread when
        /// thread affinity is required.
        /// </summary>
        /// <param name="textBoxBase">
        /// The text box to which the text is to be appended.
        /// </param>
        /// <param name="text">
        /// The text to be appended to the text box.
        /// </param>
        /// <param name="asynchronous">
        /// Non-zero to perform the change asynchronously when marshaling is
        /// required; otherwise, it is performed synchronously.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// This method obtains the first child control contained within the
        /// specified control.
        /// </summary>
        /// <param name="control">
        /// The control whose first child control is to be obtained.
        /// </param>
        /// <returns>
        /// The first child control of the specified control, or null if it has
        /// no child controls.
        /// </returns>
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
        /// <summary>
        /// This method resizes the specified control to the given size,
        /// suspending and resuming its layout around the change and marshaling
        /// it to the owning thread when thread affinity is required.
        /// </summary>
        /// <param name="control">
        /// The control to be resized.
        /// </param>
        /// <param name="size">
        /// The new size to apply to the control.
        /// </param>
        /// <returns>
        /// True if the control was successfully resized; otherwise, false.
        /// </returns>
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
