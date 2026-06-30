/*
 * StatusFormOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if DRAWING
using System.Drawing;
using System.Drawing.Text;
using System.IO;
#endif

using System.Threading;
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NATIVE && WINDOWS && TEST
using Win32Window = Eagle._Tests.Default.Win32Window;
#endif

using ThreadTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    Eagle._Components.Public.Interpreter, bool?, bool?>;

using FormEventResultTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    bool?, bool?, Eagle._Components.Public.ReturnCode?>;

using SFCD = Eagle._Components.Private.StatusFormOps.StatusFormClientData;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the static methods used to create, manage, update,
    /// and tear down the optional Windows Forms based status form (and its
    /// background status thread) associated with an interpreter.
    /// </summary>
    [ObjectId("903b723a-5915-475b-a75b-f6f5ae1879a1")]
    internal static class StatusFormOps
    {
        #region StatusFormClientData Helper Class
        /// <summary>
        /// This class holds the client data carried by a status form, associating
        /// it with its interpreter and the event used to signal that the
        /// associated status thread should stop.
        /// </summary>
        [ObjectId("83575c8c-7d93-4679-9427-ce4fbdf613c7")]
        internal sealed class StatusFormClientData : ClientData, IGetInterpreter
        {
            #region Public Constructors
            /// <summary>
            /// Constructs an instance of this class using the specified opaque data,
            /// interpreter, and done event.
            /// </summary>
            /// <param name="data">
            /// The opaque, caller-defined data to associate with this client data.
            /// This parameter may be null.
            /// </param>
            /// <param name="interpreter">
            /// The interpreter to associate with this client data. This parameter may
            /// be null.
            /// </param>
            /// <param name="doneEvent">
            /// The event used to signal that the associated status thread should
            /// stop. This parameter may be null.
            /// </param>
            public StatusFormClientData(
                object data,              /* in: OPTIONAL */
                Interpreter interpreter,  /* in: OPTIONAL */
                EventWaitHandle doneEvent /* in: OPTIONAL */
                )
                : base(data)
            {
                this.interpreter = interpreter;
                this.doneEvent = doneEvent;
            }
            #endregion

            //////////////////////////////////////////////////////////////////

            #region Public Properties
            /// <summary>
            /// The event used to signal that the status thread associated with this
            /// client data should stop, or null if there is none.
            /// </summary>
            private EventWaitHandle doneEvent;
            /// <summary>
            /// Gets the event used to signal that the status thread associated with
            /// this client data should stop.
            /// </summary>
            public EventWaitHandle DoneEvent
            {
                get { return doneEvent; }
            }
            #endregion

            //////////////////////////////////////////////////////////////////

            #region IGetInterpreter Members
            /// <summary>
            /// The interpreter associated with this client data, or null if there is
            /// none.
            /// </summary>
            private Interpreter interpreter;
            /// <summary>
            /// Gets the interpreter associated with this client data.
            /// </summary>
            public Interpreter Interpreter
            {
                get { return interpreter; }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Keyboard Event Handlers Helper Class
        /// <summary>
        /// This class contains the keyboard event handler callbacks used by the
        /// status form, along with the method used to populate the keyboard event
        /// map with them.
        /// </summary>
        [ObjectId("533e3415-7235-428c-b59a-e5873129d70d")]
        private static class KeyEventCallbacks
        {
            #region Private Static Data
            //
            // TODO: This should cause the e.Handled property to be set to
            //       true -AND- the e.SuppressKeyPress property to be left
            //       alone.
            //
            /// <summary>
            /// The default result triplet returned by the keyboard event handler
            /// callbacks in this class; it leaves the handled state alone while
            /// requesting that the key press be suppressed.
            /// </summary>
            private static FormEventResultTriplet DefaultResult =
                new AnyTriplet<bool?, bool?, ReturnCode?>(null, true, null);
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method populates the specified keyboard event map with the
            /// keyboard event handler callbacks provided by this class, creating the
            /// map when necessary.
            /// </summary>
            /// <param name="keyEventMap">
            /// Upon input, the keyboard event map to populate; when null, a new map
            /// is created. Upon output, this parameter receives the populated
            /// keyboard event map.
            /// </param>
            public static void Initialize(
                ref KeyOps.KeyEventMap keyEventMap /* in, out */
                )
            {
                if (keyEventMap == null)
                    keyEventMap = KeyOps.KeyEventMap.Create();

                TraceErrorCallback callback = new TraceErrorCallback(
                        delegate(Result error)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Initialize: error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(KeyEventCallbacks).Name,
                        TracePriority.UserInterfaceError);
                });

                Result localError; /* REUSED */

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.A,
                        new FormEventCallback(SelectText),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.D,
                        new FormEventCallback(DeselectText),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.R,
                        new FormEventCallback(ToggleReadOnlyText),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.Delete,
                        new FormEventCallback(ClearText),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.K,
                        new FormEventCallback(StopThread),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.E,
                        new FormEventCallback(EvaluateText),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Alt, Keys.E,
                        new FormEventCallback(EvaluateText),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.F4,
                        new FormEventCallback(DisposeInterpreter),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }

#if CONSOLE
                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.F3,
                        new FormEventCallback(ConsoleCancelEventHandler),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }
#endif

#if SHELL
                localError = null;

                if (!keyEventMap.Add(
                        EventType.KeyUp, Keys.Control, Keys.F2,
                        new FormEventCallback(CreateInteractiveLoopThread),
                        ref localError))
                {
                    if (callback != null)
                        callback(localError);
                }
#endif
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Eagle._Components.Public.Delegates.FormEventCallback Methods
            /// <summary>
            /// This method handles the keyboard event used to select all of the text
            /// within the status form text box.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet SelectText(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                /* IGNORED */
                FormOps.SelectText(
                    GetTextBoxFromSender(sender), false);

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method handles the keyboard event used to deselect all of the
            /// text within the status form text box.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet DeselectText(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                /* IGNORED */
                FormOps.DeselectText(
                    GetTextBoxFromSender(sender), false);

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method handles the keyboard event used to toggle the read-only
            /// state of the status form text box.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet ToggleReadOnlyText(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                /* IGNORED */
                FormOps.ToggleReadOnlyText(
                    GetTextBoxFromSender(sender), false);

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method handles the keyboard event used to clear the text within
            /// the status form text box, after prompting the user for confirmation.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet ClearText(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                Interpreter interpreter = GetInterpreterFromSender(
                    sender, false);

                if (interpreter == null)
                    return DefaultResult;

                IWin32Window owner = GetWin32WindowFromSender(sender);

                bool? dialogResult = FormOps.YesOrNo(
                    owner, String.Format(clearPromptFormat,
                    FormatOps.InterpreterNoThrow(interpreter)),
                    GlobalState.GetPackageName(), false);

                if ((dialogResult != null) && (bool)dialogResult)
                {
                    /* IGNORED */
                    FormOps.ClearText(
                        GetTextBoxFromSender(sender), false);
                }

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method handles the keyboard event used to close the status form
            /// (stopping its status thread), after prompting the user for
            /// confirmation.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet StopThread(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                Interpreter interpreter = GetInterpreterFromSender(
                    sender, false);

                if (interpreter == null)
                    return DefaultResult;

                IWin32Window owner = GetWin32WindowFromSender(sender);

                bool? dialogResult = FormOps.YesOrNo(
                    owner, String.Format(closePromptFormat,
                    FormatOps.InterpreterNoThrow(interpreter)),
                    GlobalState.GetPackageName(), false);

                if ((dialogResult != null) && (bool)dialogResult)
                    StopThreadOrMaybeComplain(interpreter, false);

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method handles the keyboard event used to evaluate the script
            /// contained in the status form text box, replacing the text with the
            /// formatted result.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet EvaluateText(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                TextBox textBox = GetTextBoxFromSender(sender);

                if (textBox == null)
                    return DefaultResult;

                Interpreter interpreter = GetInterpreterFromSender(
                    sender, false);

                if (interpreter == null)
                    return DefaultResult;

                string text;
                bool selected;

                text = FormOps.GetText(textBox, out selected);

                if (text == null)
                    return DefaultResult;

                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    code = interpreter.EvaluateScript(
                        text, ref result);
                }
                catch (Exception ex)
                {
                    result = ex;
                    code = ReturnCode.Error;
                }
                finally
                {
                    FormOps.SetText(textBox, Utility.FormatResult(
                        code, result), selected, false);
                }

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method handles the keyboard event used to dispose of the
            /// interpreter associated with the status form, after prompting the user
            /// for confirmation.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet DisposeInterpreter(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                Interpreter interpreter = GetInterpreterFromSender(
                    sender, false);

                if (interpreter == null)
                    return DefaultResult;

                IWin32Window owner = GetWin32WindowFromSender(sender);

                bool? dialogResult = FormOps.YesOrNo(
                    owner, String.Format(disposePromptFormat,
                    FormatOps.InterpreterNoThrow(interpreter)),
                    GlobalState.GetPackageName(), false);

                if ((dialogResult != null) && (bool)dialogResult)
                {
                    try
                    {
                        interpreter.Dispose(); /* throw */
                        interpreter = null;
                    }
                    catch (Exception ex)
                    {
                        TraceOps.DebugTrace(
                            ex, typeof(StatusFormOps).Name,
                            TracePriority.CleanupError);
                    }
                }

                return DefaultResult;
            }

            ///////////////////////////////////////////////////////////////////

#if CONSOLE
            /// <summary>
            /// This method handles the keyboard event used to cancel all running
            /// scripts in the interpreter, after prompting the user for confirmation.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet ConsoleCancelEventHandler(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                IWin32Window owner = GetWin32WindowFromSender(sender);

                bool? dialogResult = FormOps.YesOrNo(
                    owner, String.Format(cancelPromptFormat),
                    GlobalState.GetPackageName(), false);

                if ((dialogResult != null) && (bool)dialogResult)
                {
                    /* NO RESULT */
                    Interpreter.MaybeShowPromptAndAllCancel(
                        sender, false);
                }

                return DefaultResult;
            }
#endif

            ///////////////////////////////////////////////////////////////////

#if SHELL
            /// <summary>
            /// This method handles the keyboard event used to start a new interactive
            /// loop thread for the interpreter, after prompting the user for
            /// confirmation.
            /// </summary>
            /// <param name="eventType">
            /// The type of keyboard event being handled.
            /// </param>
            /// <param name="sender">
            /// The object that raised the keyboard event. This parameter may be null.
            /// </param>
            /// <param name="e">
            /// The data associated with the keyboard event. This parameter may be
            /// null.
            /// </param>
            /// <returns>
            /// </returns>
            private static FormEventResultTriplet CreateInteractiveLoopThread(
                EventType eventType, /* in */
                object sender,       /* in */
                EventArgs e          /* in */
                )
            {
                IWin32Window owner = GetWin32WindowFromSender(sender);

                bool? dialogResult = FormOps.YesOrNo(
                    owner, String.Format(shellPromptFormat),
                    GlobalState.GetPackageName(), false);

                if ((dialogResult != null) && (bool)dialogResult)
                {
                    Thread thread;
                    Result error = null;

                    thread = ShellOps.CreateInteractiveLoopThread(
                        GetInterpreterFromSender(sender, false),
                        InteractiveLoopData.Create(), true, ref error);

                    if (thread == null)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "CreateInteractiveLoopThread: error = {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(KeyEventCallbacks).Name,
                            TracePriority.StatusError);
                    }
                }

                return DefaultResult;
            }
#endif
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The format string used to construct the title text of the status form.
        /// </summary>
        private const string NameFormat =
            "Status: {0} interpreter {1}, process {2}, thread {3}, domain {4}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to build the confirmation prompt shown before
        /// clearing the status form.
        /// </summary>
        private const string clearPromptFormat =
            "clear status form {0}: are you sure?";

        /// <summary>
        /// The format string used to build the confirmation prompt shown before
        /// closing the status form.
        /// </summary>
        private const string closePromptFormat =
            "close status form {0}: are you sure?";

        /// <summary>
        /// The format string used to build the confirmation prompt shown before
        /// disposing of the interpreter.
        /// </summary>
        private const string disposePromptFormat =
            "dispose of interpreter {0} immediately: are you sure?";

        /// <summary>
        /// The format string used to build the confirmation prompt shown before
        /// canceling all running scripts.
        /// </summary>
        private const string cancelPromptFormat =
            "immediately cancel all running scripts: are you sure?";

        /// <summary>
        /// The format string used to build the confirmation prompt shown before
        /// starting a new interactive loop thread.
        /// </summary>
        private const string shellPromptFormat =
            "start new interactive loop thread: are you sure?";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The expected primary status level for the status thread.
        /// </summary>
        private const int PrimaryLevels = 1;
        /// <summary>
        /// The expected secondary status level used when updating the status form
        /// text box.
        /// </summary>
        private const int SecondaryLevels = 3;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// Non-zero while the process is still running; this value is decremented
        /// to zero when the process (or AppDomain) is exiting.
        /// </summary>
        private static int ProcessRunning = 1;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The number of microseconds to wait during each iteration of the status
        /// thread loop.
        /// </summary>
        private static int LoopWaitMicroseconds = 15000; // 15ms
        /// <summary>
        /// The number of milliseconds to wait after submitting a status update
        /// request.
        /// </summary>
        private static int RequestWaitMilliseconds = 100;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The number of milliseconds to sleep between iterations while waiting
        /// for the status form to be disposed.
        /// </summary>
        private static int DisposeSleepMilliseconds = 25;
        /// <summary>
        /// The minimum number of milliseconds to wait for the status form to be
        /// disposed, or null if there is no minimum.
        /// </summary>
        private static int? DisposeMinimumMilliseconds = null;
        /// <summary>
        /// The maximum number of milliseconds to wait for the status form to be
        /// disposed, or null if there is no maximum.
        /// </summary>
        private static int? DisposeMaximumMilliseconds = 500;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When true, failures encountered while stopping the status thread are
        /// not reported.
        /// </summary>
        private static bool NoComplain = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When true, Win32 events are always processed before waiting during
        /// each iteration of the status thread loop.
        /// </summary>
        private static bool ForcePreEvents = false;
        /// <summary>
        /// When true, the status form ignores the done event and stays open.
        /// </summary>
        private static bool ForceStayOpen = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When true, diagnostic tracing is enabled for the waits performed by
        /// the status thread.
        /// </summary>
        private static bool TraceWait = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default width, in pixels, of the status form.
        /// </summary>
        private static int DefaultWidth = 600;
        /// <summary>
        /// The default height, in pixels, of the status form.
        /// </summary>
        private static int DefaultHeight = 300;
        /// <summary>
        /// The multiplier applied to a font size when producing a larger font.
        /// </summary>
        private static int BiggerFontSizeMultiplier = 2;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default font size, in points, used for the status form text box.
        /// </summary>
        private static float DefaultFontSize = 8.25f;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default top-most setting used for the status form.
        /// </summary>
        private static bool DefaultTopMost = false;
        /// <summary>
        /// The default setting that indicates whether the status form may be
        /// closed by the user.
        /// </summary>
        private static bool DefaultCanClose = false;

        ///////////////////////////////////////////////////////////////////////

#if DRAWING
        //
        // NOTE: These are purposely set to the "legacy" defaults,
        //       which may not be ideal in high-DPI environments.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When true, the status form uses DPI-based automatic scaling.
        /// </summary>
        private static bool UseAutoScaleDpi = false;
        /// <summary>
        /// The DPI dimensions to force for automatic scaling, or null to detect
        /// them.
        /// </summary>
        private static SizeF? ForceAutoScaleDpi = null;
        /// <summary>
        /// The DPI dimensions to use for automatic scaling when they cannot be
        /// detected, or null if there are none.
        /// </summary>
        private static SizeF? FallbackAutoScaleDpi = null;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// When true, the active interpreter is used as a fallback when no
        /// interpreter can be located for a sender.
        /// </summary>
        private static bool UseActiveInterpreter = false;
        /// <summary>
        /// When true, the keyboard hot-keys associated with the status form are
        /// enabled.
        /// </summary>
        private static bool AllowHotKeys = false;
        /// <summary>
        /// When true, a missing status done event name is treated as an error
        /// when stopping the status thread.
        /// </summary>
        private static bool StrictStopThread = false;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && TEST
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The native window type used as the owner of the message boxes shown by
        /// the status form.
        /// </summary>
        private static NativeWindowType ownerWindowType =
            NativeWindowType.None;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to synchronize access to the static class
        //       data defined below this point.
        //
        /// <summary>
        /// The object used to synchronize access to the static class data defined
        /// below it.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the set of keyboard mappings for this AppDomain.
        //
        /// <summary>
        /// The set of keyboard event mappings for this AppDomain.
        /// </summary>
        private static KeyOps.KeyEventMap keyEventMap;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Initialization Support Methods
        /// <summary>
        /// This method performs static, one-time initialization for this class.
        /// </summary>
        public static void Initialize()
        {
            InitializeKeyEventCallbacks();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the keyboard event map used by the status form
        /// with its associated keyboard event handler callbacks.
        /// </summary>
        private static void InitializeKeyEventCallbacks()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                KeyEventCallbacks.Initialize(ref keyEventMap);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the Interpreter.GetHostInterpreterInfo method.
        //
        /// <summary>
        /// This method adds rows describing the current status form settings to
        /// the specified list, for introspection purposes.
        /// </summary>
        /// <param name="list">
        /// Upon input, the list to which the descriptive rows are added. Upon
        /// output, this list contains the added rows. This parameter may be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail to include.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            int localProcessRunning = Interlocked.CompareExchange(
                ref ProcessRunning, 0, 0);

            if (empty || (localProcessRunning != 0))
            {
                localList.Add("ProcessRunning",
                    localProcessRunning.ToString());
            }

            if (empty || (LoopWaitMicroseconds != 0))
            {
                localList.Add("LoopWaitMicroseconds",
                    LoopWaitMicroseconds.ToString());
            }

            if (empty || (RequestWaitMilliseconds != 0))
            {
                localList.Add("RequestWaitMilliseconds",
                    RequestWaitMilliseconds.ToString());
            }

            if (empty || (DisposeSleepMilliseconds != 0))
                localList.Add("DisposeSleepMilliseconds",
                    DisposeSleepMilliseconds.ToString());

            if (empty || (DisposeMinimumMilliseconds != null))
                localList.Add("DisposeMinimumMilliseconds",
                    (DisposeMinimumMilliseconds != null) ?
                        ((int)DisposeMinimumMilliseconds).ToString() :
                        FormatOps.DisplayNull);

            if (empty || (DisposeMaximumMilliseconds != null))
                localList.Add("DisposeMaximumMilliseconds",
                    (DisposeMaximumMilliseconds != null) ?
                        ((int)DisposeMaximumMilliseconds).ToString() :
                        FormatOps.DisplayNull);

            if (empty || NoComplain)
                localList.Add("NoComplain", NoComplain.ToString());

            if (empty || ForcePreEvents)
                localList.Add("ForcePreEvents", ForcePreEvents.ToString());

            if (empty || ForceStayOpen)
                localList.Add("ForceStayOpen", ForceStayOpen.ToString());

            if (empty || TraceWait)
                localList.Add("TraceWait", TraceWait.ToString());

            if (empty || (DefaultWidth != 0))
                localList.Add("DefaultWidth", DefaultWidth.ToString());

            if (empty || (DefaultHeight != 0))
                localList.Add("DefaultHeight", DefaultHeight.ToString());

            if (empty || (BiggerFontSizeMultiplier != 0))
                localList.Add("BiggerFontSizeMultiplier",
                    BiggerFontSizeMultiplier.ToString());

            if (empty || (DefaultFontSize != 0.0f))
                localList.Add("DefaultFontSize", DefaultFontSize.ToString());

            if (empty || DefaultTopMost)
                localList.Add("DefaultTopMost", DefaultTopMost.ToString());

            if (empty || DefaultCanClose)
                localList.Add("DefaultCanClose", DefaultCanClose.ToString());

#if DRAWING
            if (empty || UseAutoScaleDpi)
                localList.Add("UseAutoScaleDpi", UseAutoScaleDpi.ToString());

            if (empty || (ForceAutoScaleDpi != null))
                localList.Add("ForceAutoScaleDpi",
                    (ForceAutoScaleDpi != null) ?
                        ((SizeF)ForceAutoScaleDpi).ToString() :
                        FormatOps.DisplayNull);

            if (empty || (FallbackAutoScaleDpi != null))
                localList.Add("FallbackAutoScaleDpi",
                    (FallbackAutoScaleDpi != null) ?
                        ((SizeF)FallbackAutoScaleDpi).ToString() :
                        FormatOps.DisplayNull);
#endif

            if (empty || UseActiveInterpreter)
            {
                localList.Add("UseActiveInterpreter",
                    UseActiveInterpreter.ToString());
            }

            if (empty || AllowHotKeys)
                localList.Add("AllowHotKeys", AllowHotKeys.ToString());

            if (empty || StrictStopThread)
                localList.Add("StrictStopThread", StrictStopThread.ToString());

#if NATIVE && WINDOWS && TEST
            if (empty || (ownerWindowType != NativeWindowType.None))
                localList.Add("OwnerWindowType", ownerWindowType.ToString());
#endif

            if (localList.Count > 0)
            {
                list.Add((IPair<string>)null);
                list.Add("Status Form");
                list.Add((IPair<string>)null);
                list.Add(localList);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IKeyEventManager Support Methods
        /// <summary>
        /// This method gets the keyboard event map for this AppDomain.
        /// </summary>
        /// <returns>
        /// The keyboard event map for this AppDomain, or null if there is none.
        /// </returns>
        public static KeyOps.KeyEventMap GetKeyEventMap()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return keyEventMap;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the keyboard event map for this AppDomain.
        /// </summary>
        /// <param name="keyEventMap">
        /// The keyboard event map to use for this AppDomain. This parameter may
        /// be null.
        /// </param>
        public static void SetKeyEventMap(
            KeyOps.KeyEventMap keyEventMap /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StatusFormOps.keyEventMap = keyEventMap;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method saves the current keyboard event map, optionally resetting
        /// it to a new, empty map.
        /// </summary>
        /// <param name="reset">
        /// Non-zero to reset the keyboard event map to a new, empty map after
        /// saving it.
        /// </param>
        /// <param name="savedKeyEventMap">
        /// Upon output, this parameter receives the saved keyboard event map.
        /// </param>
        public static void SaveKeyEventMap(
            bool reset,                 /* in */
            ref object savedKeyEventMap /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                savedKeyEventMap = keyEventMap;

                if (reset)
                    keyEventMap = KeyOps.KeyEventMap.Create();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores a previously saved keyboard event map.
        /// </summary>
        /// <param name="savedKeyEventMap">
        /// Upon input, the keyboard event map to restore. Upon output, this
        /// parameter is reset to null.
        /// </param>
        public static void RestoreKeyEventMap(
            ref object savedKeyEventMap /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                keyEventMap = savedKeyEventMap as KeyOps.KeyEventMap;
                savedKeyEventMap = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Handler Helper Methods
        /// <summary>
        /// This method gets the status form text box associated with the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status form text box is needed. This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The status form text box for the interpreter, or null if there is
        /// none.
        /// </returns>
        public static TextBox GetTextBox(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.StatusObject as TextBox;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the window to use as the owner for the message boxes
        /// shown by the status form, based on the specified sender.
        /// </summary>
        /// <param name="sender">
        /// The object that originated the request. This parameter may be null.
        /// </param>
        /// <returns>
        /// The window to use as the message box owner, or null if there is none.
        /// </returns>
        private static IWin32Window GetWin32WindowFromSender(
            object sender /* in */
            )
        {
#if NATIVE && WINDOWS && TEST
            if (ownerWindowType != NativeWindowType.None)
            {
                IntPtr handle = IntPtr.Zero;
                Result error = null;

                if (WindowOps.GetNativeWindow(ownerWindowType,
                        ref handle, ref error) == ReturnCode.Ok)
                {
                    return new Win32Window(handle);
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetWin32WindowFromSender: error = {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(StatusFormOps).Name,
                        TracePriority.UserInterfaceError);
                }
            }
#endif

            //
            // TODO: Perhaps consider using an alternate method of
            //       getting a message box owner here?
            //
            return GetFormFromSender(sender, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to locate the interpreter associated with the
        /// specified sender.
        /// </summary>
        /// <param name="sender">
        /// The object from which to locate the interpreter. This parameter may be
        /// null.
        /// </param>
        /// <param name="noForm">
        /// Non-zero to skip locating the interpreter via the status form
        /// associated with the sender.
        /// </param>
        /// <returns>
        /// The interpreter associated with the sender, or null if one cannot be
        /// located.
        /// </returns>
        private static Interpreter GetInterpreterFromSender(
            object sender, /* in */
            bool noForm    /* in */
            )
        {
            Interpreter interpreter = sender as Interpreter;

            if (interpreter != null)
                return interpreter;

            IGetInterpreter getInterpreter = sender as IGetInterpreter;

            if (getInterpreter != null)
            {
                interpreter = getInterpreter.Interpreter;

                if (interpreter != null)
                    return interpreter;
            }

            if (!noForm)
            {
                Form form = GetFormFromSender(sender, true);

                if (form != null)
                {
                    SFCD clientData = form.Tag as SFCD;

                    if (clientData != null)
                    {
                        interpreter = clientData.Interpreter;

                        if (interpreter != null)
                            return interpreter;
                    }
                }
            }

            TraceOps.DebugTrace(String.Format(
                "GetInterpreterFromSender: no interpreter found via {0}",
                FormOps.ToString(sender, true)), typeof(StatusFormOps).Name,
                TracePriority.UserInterfaceError);

            return UseActiveInterpreter ? Interpreter.GetActive() : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to locate the status form associated with the
        /// specified sender.
        /// </summary>
        /// <param name="sender">
        /// The object from which to locate the status form. This parameter may be
        /// null.
        /// </param>
        /// <param name="noInterpreter">
        /// Non-zero to skip locating the status form via the interpreter
        /// associated with the sender.
        /// </param>
        /// <returns>
        /// The status form associated with the sender, or null if one cannot be
        /// located.
        /// </returns>
        private static Form GetFormFromSender(
            object sender,     /* in */
            bool noInterpreter /* in */
            )
        {
            Form form = sender as Form;

            if (form != null)
                return form;

            if (!noInterpreter)
            {
                Interpreter interpreter = GetInterpreterFromSender(
                    sender, true);

                if (interpreter != null)
                {
                    TextBox textBox = GetTextBox(interpreter);

                    if (textBox != null)
                    {
                        form = FormOps.FindForm(textBox);

                        if (form != null)
                            return form;
                    }
                }
            }

            TraceOps.DebugTrace(String.Format(
                "GetFormFromSender: no form found via {0}",
                FormOps.ToString(sender, true)), typeof(StatusFormOps).Name,
                TracePriority.UserInterfaceError);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to locate the status form text box associated
        /// with the specified sender.
        /// </summary>
        /// <param name="sender">
        /// The object from which to locate the status form text box. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The status form text box associated with the sender, or null if one
        /// cannot be located.
        /// </returns>
        private static TextBox GetTextBoxFromSender(
            object sender /* in */
            )
        {
            TextBox textBox = sender as TextBox;

            if (textBox != null)
                return textBox;

            Form form = GetFormFromSender(sender, false);

            if (form != null)
            {
                textBox = FormOps.GetFirstControl(form) as TextBox;

                if (textBox != null)
                    return textBox;
            }

            Interpreter interpreter = GetInterpreterFromSender(
                sender, false);

            if (interpreter != null)
            {
                textBox = GetTextBox(interpreter);

                if (textBox != null)
                    return textBox;
            }

            TraceOps.DebugTrace(String.Format(
                "GetTextBoxFromSender: no text box found via {0}",
                FormOps.ToString(sender, true)), typeof(StatusFormOps).Name,
                TracePriority.UserInterfaceError);

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Windows.Forms.Form Event Handlers
        /// <summary>
        /// This method handles the key-up event raised by the status form,
        /// dispatching it through the configured keyboard event handlers.
        /// </summary>
        /// <param name="sender">
        /// The object that originated this event. This parameter may be null.
        /// </param>
        /// <param name="e">
        /// The data associated with the key-up event. This parameter may be null.
        /// </param>
        private static void HandleKeyUp(
            object sender, /* in */
            KeyEventArgs e /* in */
            )
        {
            if (!AllowHotKeys)
                return;

            Interpreter interpreter = GetInterpreterFromSender(
                sender, false);

            int chainCount = 0;
            ReturnCode chainCode = ReturnCode.Ok;
            Result chainError = null;

            /* NO RESULT */
            KeyOps.ChainEventHandlers(
                EventType.KeyUp, sender, e, null, null,
                ref chainCount, ref chainCode, ref chainError,
                (interpreter != null) ?
                    interpreter.GetKeyEventMap() : null,
                StatusFormOps.GetKeyEventMap());

            if (chainCode != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "HandleKeyUp: count = {0}, code = {1}, " +
                    "error = {2}", chainCount, chainCode,
                    FormatOps.WrapOrNull(chainError)),
                    typeof(StatusFormOps).Name,
                    TracePriority.UserInterfaceError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the event raised when the status form has been
        /// closed, stopping the associated status thread.
        /// </summary>
        /// <param name="sender">
        /// The object that originated this event. This parameter may be null.
        /// </param>
        /// <param name="e">
        /// The data associated with the form-closed event. This parameter may be
        /// null.
        /// </param>
        private static void HandleClosed(
            object sender,        /* in */
            FormClosedEventArgs e /* in */
            )
        {
            //
            // HACK: If the status thread is getting read to close
            //       the form, skip doing anything else.
            //
            Interpreter interpreter = GetInterpreterFromSender(
                sender, false);

            if (interpreter == null)
                return;

            StopThreadOrMaybeComplain(interpreter, null, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the event raised when the status form has been
        /// disposed, marking the status of the active interpreter as disposed.
        /// </summary>
        /// <param name="sender">
        /// The object that originated this event. This parameter may be null.
        /// </param>
        /// <param name="e">
        /// The data associated with this event. This parameter may be null.
        /// </param>
        private static void HandleDisposed(
            object sender,
            EventArgs e
            )
        {
            Interpreter interpreter = GlobalState.GetActiveInterpreterOnly();

            if (interpreter == null)
                return;

            /* IGNORED */
            interpreter.MarkStatusDisposed();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Process Event Handlers
        /// <summary>
        /// This method records that the process (or AppDomain) is exiting by
        /// decrementing the running process count.
        /// </summary>
        public static void Exit()
        {
            int localProcessRunning = Interlocked.Decrement(
                ref ProcessRunning);

            TraceOps.DebugTrace(String.Format(
                "Exit: ProcessRunning IS NOW {0}", localProcessRunning),
                typeof(StatusFormOps).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Threading.ParameterizedThreadStart Callbacks
        /// <summary>
        /// This method is the entry point for the status thread; it creates the
        /// status form and runs its message loop until the status thread is
        /// signaled to stop.
        /// </summary>
        /// <param name="obj">
        /// The state object passed to the thread; it is expected to be a triplet
        /// carrying the interpreter and the optional close and top-most settings.
        /// This parameter may be null.
        /// </param>
        private static void ThreadStart(
            object obj /* in */
            ) /* System.Threading.ParameterizedThreadStart */
        {
            EventWaitHandle startEvent = null;
            EventWaitHandle doneEvent = null;

            try
            {
                ThreadTriplet anyTriplet = obj as ThreadTriplet;

                if (anyTriplet == null)
                    return;

                Interpreter interpreter = anyTriplet.X;

                if (interpreter == null)
                    return;

                string startEventName = interpreter.StatusStartEventName;

                if (startEventName == null)
                    return;

                string doneEventName = interpreter.StatusDoneEventName;

                if (doneEventName == null)
                    return;

                startEvent = ThreadOps.OpenEvent(startEventName);

                if (startEvent == null)
                    return;

                doneEvent = ThreadOps.CreateEvent(doneEventName);

                if (doneEvent == null)
                    return;

                bool canClose = (anyTriplet.Y != null) ?
                    (bool)anyTriplet.Y : DefaultCanClose;

                bool topMost = (anyTriplet.Z != null) ?
                    (bool)anyTriplet.Z : DefaultTopMost;

                Form form = null;

                try
                {
                    TextBox textBox = null;
                    SFCD clientData = null;

                    try
                    {
                        clientData = new SFCD(
                            null, interpreter, doneEvent);

                        Result error = null;

                        if (Create(
                                GetText(interpreter), clientData,
                                DefaultFontSize, canClose, topMost,
                                true, ref form, ref textBox,
                                ref error) != ReturnCode.Ok)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "ThreadStart: error = {0}",
                                FormatOps.WrapOrNull(error)),
                                typeof(StatusFormOps).Name,
                                TracePriority.StatusError);

                            return;
                        }

                        clientData.Data = textBox;

                        if (!interpreter.MaybeSetStatusClientData(
                                clientData))
                        {
                            TraceOps.DebugTrace(
                                "ThreadStart: cannot set status client data",
                                typeof(StatusFormOps).Name,
                                TracePriority.StatusError);
                        }

                        if (!interpreter.MaybeSetStatusObject(
                                textBox))
                        {
                            TraceOps.DebugTrace(
                                "ThreadStart: cannot set status object",
                                typeof(StatusFormOps).Name,
                                TracePriority.StatusError);
                        }

                        int levels = interpreter.EnterStatusLevel();

                        try
                        {
                            if (levels == PrimaryLevels)
                            {
                                EventWaitHandle localStartEvent = startEvent;
                                bool timedOut = false;

                                while (Interlocked.CompareExchange(
                                        ref ProcessRunning, 0, 0) > 0)
                                {
                                    /* IGNORED */
                                    interpreter.EnterStatusLevel();

                                    try
                                    {
                                        //
                                        // HACK: The first time we get to this
                                        //       point, signal the event that
                                        //       indicates to our creator that
                                        //       this thread is fully started.
                                        //
                                        if (localStartEvent != null)
                                        {
                                            ThreadOps.SetEvent(localStartEvent);
                                            localStartEvent = null;
                                        }

                                        //
                                        // HACK: When operating in "fail-safe"
                                        //       mode, be 100% sure that Win32
                                        //       events are always processed.
                                        //
                                        if (ForcePreEvents || timedOut)
                                        {
                                            /* IGNORED */
                                            WindowOps.ProcessEvents(
                                                interpreter);
                                        }

                                        //
                                        // HACK: When operating in "fail-safe"
                                        //       mode, ignore the "done" event
                                        //       and stay open.  Also, ignore
                                        //       any timeouts that occur when
                                        //       checking the interpreter for
                                        //       readiness.
                                        //
                                        timedOut = false;
                                        error = null; /* NOT USED */

                                        if ((EventOps.Wait(
                                                interpreter, ForceStayOpen ?
                                                    null : doneEvent,
                                                LoopWaitMicroseconds, null,
                                                true, false, true, false,
                                                TraceWait, ref timedOut,
                                                ref error) != ReturnCode.Ok) &&
                                            !timedOut)
                                        {
                                            break;
                                        }

                                        //
                                        // HACK: If a timeout was hit checking
                                        //       interpreter readiness, still
                                        //       check if the "done" event has
                                        //       been signaled; however, we do
                                        //       not want to block.  This does
                                        //       not apply in "fail-safe" mode.
                                        //
                                        if (!ForceStayOpen && timedOut &&
                                            ThreadOps.WaitEvent(doneEvent, 0))
                                        {
                                            break;
                                        }
                                    }
                                    finally
                                    {
                                        /* IGNORED */
                                        interpreter.ExitStatusLevel();
                                    }

                                    /* IGNORED */
                                    interpreter.AnotherStatusIteration();
                                }
                            }
                            else
                            {
                                //
                                // NOTE: This should never be hit.
                                //
                                TraceOps.DebugTrace(String.Format(
                                    "ThreadStart: invalid primary " +
                                    "status level {0}, must be {1}",
                                    levels, PrimaryLevels),
                                    typeof(StatusFormOps).Name,
                                    TracePriority.StatusError);
                            }
                        }
                        finally
                        {
                            /* IGNORED */
                            interpreter.ExitStatusLevel();
                        }
                    }
                    finally
                    {
                        if (!interpreter.MaybeResetStatusObject(
                                textBox, true))
                        {
                            TraceOps.DebugTrace(
                                "ThreadStart: cannot reset status object",
                                typeof(StatusFormOps).Name,
                                TracePriority.StatusError);
                        }

                        if (!interpreter.MaybeResetStatusClientData(
                                clientData, true))
                        {
                            TraceOps.DebugTrace(
                                "ThreadStart: cannot reset status client data",
                                typeof(StatusFormOps).Name,
                                TracePriority.StatusError);
                        }
                    }
                }
                finally
                {
                    if (form != null)
                    {
                        //
                        // HACK: Abuse the active interpreter stack for
                        //       this thread here so the HandleDisposed
                        //       method can locate this interpreter.
                        //
                        GlobalState.PushActiveInterpreter(interpreter);

                        try
                        {
                            form.Tag = null; /* NOTE: No recursion. */
                            form.Close();
                            form = null;
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(StatusFormOps).Name,
                                TracePriority.CleanupError);
                        }
                        finally
                        {
                            GlobalState.PopActiveInterpreter();
                        }

                        WaitOnDisposed(
                            interpreter, DisposeSleepMilliseconds,
                            GetDisposeMinimumMilliseconds(),
                            GetDisposeMaximumMilliseconds());
                    }

                    //
                    // NOTE: The status thread for this interpreter
                    //       must be reset (to null) now.  Also, we
                    //       may as well reset the associated "done"
                    //       event name as well.
                    //
                    if (interpreter.MaybeResetStatusThread(
                            Thread.CurrentThread, true))
                    {
                        /* RESET */
                        interpreter.StatusStartEventName = null;

                        /* RESET */
                        interpreter.StatusDoneEventName = null;
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "ThreadStart: cannot reset status thread",
                            typeof(StatusFormOps).Name,
                            TracePriority.StatusError);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (ThreadInterruptedException)
            {
                // do nothing.
            }
            catch (InterpreterDisposedException)
            {
                // do nothing.
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(StatusFormOps).Name,
                    TracePriority.ThreadError);
            }
            finally
            {
                ThreadOps.CloseEvent(ref doneEvent);
                ThreadOps.CloseEvent(ref startEvent);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Exit Handler Support Methods
        /// <summary>
        /// This method registers the handler used to detect when the AppDomain or
        /// process is exiting, unless that behavior has been disabled via
        /// configuration.
        /// </summary>
        private static void AddExitedEventHandler()
        {
            if (!GlobalConfiguration.DoesValueExist(
                    "No_StatusFormOps_Exited",
                    ConfigurationFlags.SetupOps))
            {
                AppDomain appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    if (!AppDomainOps.IsDefault(appDomain))
                    {
                        appDomain.DomainUnload -= StatusFormOps_Exited;
                        appDomain.DomainUnload += StatusFormOps_Exited;
                    }
                    else
                    {
                        appDomain.ProcessExit -= StatusFormOps_Exited;
                        appDomain.ProcessExit += StatusFormOps_Exited;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the handler used to detect when the AppDomain or
        /// process is exiting.
        /// </summary>
        private static void RemoveExitedEventHandler()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= StatusFormOps_Exited;
                else
                    appDomain.ProcessExit -= StatusFormOps_Exited;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method handles the AppDomain unload or process exit event by
        /// recording that the process is exiting and removing itself as a
        /// handler.
        /// </summary>
        /// <param name="sender">
        /// The object that originated this event. This parameter may be null.
        /// </param>
        /// <param name="e">
        /// The data associated with this event. This parameter may be null.
        /// </param>
        private static void StatusFormOps_Exited(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            /* NO RESULT */
            Exit();

            /* NO RESULT */
            RemoveExitedEventHandler();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Threading.Thread Methods
        /// <summary>
        /// This method constructs the name of the start or done event used to
        /// coordinate with the status thread for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the event name is constructed. This
        /// parameter may be null.
        /// </param>
        /// <param name="done">
        /// Non-zero to construct the name of the done event; otherwise, the name
        /// of the start event is constructed.
        /// </param>
        /// <returns>
        /// The constructed event name.
        /// </returns>
        private static string GetEventName(
            Interpreter interpreter, /* in */
            bool done                /* in */
            )
        {
            return FormatOps.EventName(interpreter, String.Format(
                "statusFormThread{0}", done ? "Done" : "Start"),
                null, GlobalState.NextEventId(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs the name to use for the status thread
        /// associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the thread name is constructed. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The constructed thread name, or null if the interpreter is null.
        /// </returns>
        private static string GetThreadName(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return String.Format(
                "statusFormThread: {0}", FormatOps.InterpreterNoThrow(
                interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the optional status form start flags from the
        /// specified client data.
        /// </summary>
        /// <param name="clientData">
        /// The client data containing the packed start flags. This parameter may
        /// be null.
        /// </param>
        /// <param name="canClose">
        /// Upon output, this parameter receives the optional value indicating
        /// whether the status form may be closed by the user.
        /// </param>
        /// <param name="topMost">
        /// Upon output, this parameter receives the optional top-most value for
        /// the status form.
        /// </param>
        /// <param name="allowHotKeys">
        /// Upon output, this parameter receives the optional value indicating
        /// whether the keyboard hot-keys are enabled.
        /// </param>
        public static void GetStartFlags(
            IClientData clientData, /* in */
            out bool? canClose,     /* out */
            out bool? topMost,      /* out */
            out bool? allowHotKeys  /* out */
            )
        {
            bool?[] args = null;

            if (!ClientData.TryUnpack<bool?>(clientData, true, out args))
            {
                canClose = null;
                topMost = null;
                allowHotKeys = null;

                return;
            }

            /* IGNORED */
            ArrayOps.TryGet<bool?>(args, 0, out canClose);

            /* IGNORED */
            ArrayOps.TryGet<bool?>(args, 1, out topMost);

            /* IGNORED */
            ArrayOps.TryGet<bool?>(args, 2, out allowHotKeys);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the status thread for the specified
        /// interpreter is still alive and making progress.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status thread is checked. This parameter may be
        /// null.
        /// </param>
        /// <param name="timeout">
        /// The number of milliseconds to wait while checking the status thread
        /// for progress.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the status thread appears to be alive;
        /// otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CheckThread(
            Interpreter interpreter, /* in */
            int timeout,             /* in */
            ref Result error         /* out */
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                long iterations = interpreter.GetStatusIterations();

                if (!MaybeWaitFor(
                        interpreter, timeout, TraceWait, ref error))
                {
                    return ReturnCode.Error;
                }

                if (iterations == interpreter.GetStatusIterations())
                {
                    error = String.Format(
                        "status thread appears dead after {0} milliseconds",
                        timeout);

                    return ReturnCode.Error;
                }

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
        /// This method creates and starts the status thread (and its status form)
        /// for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the status thread is started. This parameter
        /// may be null.
        /// </param>
        /// <param name="timeout">
        /// The number of milliseconds to wait for the status thread to signal
        /// that it has started, or null to not wait.
        /// </param>
        /// <param name="canClose">
        /// The optional value indicating whether the status form may be closed by
        /// the user.
        /// </param>
        /// <param name="topMost">
        /// The optional top-most value for the status form.
        /// </param>
        /// <param name="allowHotKeys">
        /// The optional value indicating whether the keyboard hot-keys are
        /// enabled.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the status thread was started
        /// successfully; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode StartThread(
            Interpreter interpreter, /* in */
            int? timeout,            /* in */
            bool? canClose,          /* in */
            bool? topMost,           /* in */
            bool? allowHotKeys,      /* in */
            ref Result error         /* out */
            )
        {
            EventWaitHandle startEvent = null;

            try
            {
                AddExitedEventHandler();

                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (!locked)
                    {
                        error = "unable to acquire lock";
                        return ReturnCode.Error;
                    }

                    Thread thread = interpreter.StatusThread;

                    if (thread != null)
                    {
                        error = "status thread already started";
                        return ReturnCode.Error;
                    }

                    string startEventName = GetEventName(interpreter, false);

                    if (startEventName == null)
                    {
                        error = "invalid status start event name";
                        return ReturnCode.Error;
                    }

                    string doneEventName = GetEventName(interpreter, true);

                    if (doneEventName == null)
                    {
                        error = "invalid status done event name";
                        return ReturnCode.Error;
                    }

                    startEvent = ThreadOps.CreateEvent(startEventName);

                    if (startEvent == null)
                    {
                        error = String.Format(
                            "cannot create start event {0}",
                            FormatOps.WrapOrNull(startEventName));

                        return ReturnCode.Error;
                    }

                    bool success = false;

                    try
                    {
                        thread = Engine.CreateThread(
                            interpreter, ThreadStart, 0, true, false, true);

                        if (thread == null)
                        {
                            error = "failed to create status thread";
                            return ReturnCode.Error;
                        }

                        if (interpreter.MaybeSetStatusThread(thread))
                        {
                            success = true;

                            /* SET */
                            interpreter.StatusStartEventName = startEventName;

                            /* SET */
                            interpreter.StatusDoneEventName = doneEventName;
                        }
                        else
                        {
                            error = "cannot set status thread";
                            return ReturnCode.Error;
                        }

                        //
                        // HACK: If the status hot-keys are enabled,
                        //       make sure the top-most bit will be
                        //       set for the new form as long as the
                        //       system default top-most value would
                        //       still have been used.  This is very
                        //       useful, because message boxes used
                        //       for confirmation prompts should be
                        //       top-most for security reasons.
                        //
                        if (allowHotKeys != null)
                            AllowHotKeys = (bool)allowHotKeys;

                        if ((topMost == null) && AllowHotKeys)
                            topMost = true;

                        thread.Name = GetThreadName(interpreter);

                        /* IGNORED */
                        interpreter.ResetStatusDisposed();

                        thread.Start(
                            new AnyTriplet<Interpreter, bool?, bool?>(
                                interpreter, canClose, topMost));

                        if ((timeout != null) && !ThreadOps.WaitEvent(
                                startEvent, (int)timeout))
                        {
                            error = String.Format(
                                "status thread start timeout of {0} " +
                                "milliseconds", timeout);

                            return ReturnCode.Error;
                        }

                        return ReturnCode.Ok;
                    }
                    finally
                    {
                        if (!success && (thread != null))
                        {
                            /* NO RESULT */
                            ThreadOps.MaybeShutdown(
                                interpreter, null,
                                ShutdownFlags.Status,
                                ref thread);
                        }
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                ThreadOps.CloseEvent(ref startEvent);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the minimum number of milliseconds to wait for the
        /// status form to be disposed.
        /// </summary>
        /// <returns>
        /// The minimum number of milliseconds to wait, or null if there is no
        /// minimum.
        /// </returns>
        private static int? GetDisposeMinimumMilliseconds()
        {
            //
            // TODO: Use Mono detection here?
            //
            return DisposeMinimumMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the maximum number of milliseconds to wait for the
        /// status form to be disposed.
        /// </summary>
        /// <returns>
        /// The maximum number of milliseconds to wait, or null if there is no
        /// maximum.
        /// </returns>
        private static int? GetDisposeMaximumMilliseconds()
        {
            //
            // TODO: Use Mono detection here?
            //
            return DisposeMaximumMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether an operation on the status form should
        /// be performed synchronously.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the operation. This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// The optional, explicit synchronous setting; when null, the setting is
        /// inferred from the interpreter.
        /// </param>
        /// <returns>
        /// True if the operation should be performed synchronously; otherwise,
        /// false.
        /// </returns>
        private static bool GetSynchronous(
            Interpreter interpreter, /* in: OPTIONAL */
            bool? synchronous        /* in: OPTIONAL */
            )
        {
            if (synchronous != null)
                return (bool)synchronous;

            if (interpreter != null)
                return !interpreter.IsStatusThread();

            return false; /* FAIL-SAFE: No deadlock. */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes events on the current thread until the status
        /// form has been disposed or the configured time limits are reached.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status form is being disposed. This parameter
        /// may be null.
        /// </param>
        /// <param name="sleepMilliseconds">
        /// The number of milliseconds to sleep between iterations, or a negative
        /// value to skip sleeping.
        /// </param>
        /// <param name="minimumMilliseconds">
        /// The minimum number of milliseconds to wait, or null if there is no
        /// minimum.
        /// </param>
        /// <param name="maximumMilliseconds">
        /// The maximum number of milliseconds to wait, or null if there is no
        /// maximum.
        /// </param>
        private static void WaitOnDisposed(
            Interpreter interpreter,  /* in */
            int sleepMilliseconds,    /* in */
            int? minimumMilliseconds, /* in */
            int? maximumMilliseconds  /* in */
            )
        {
            //
            // HACK: This block is primarily to make Mono happier
            //       while closing the form and make sure it ends
            //       up being removed from the screen.  Basically
            //       it keeps processing events (on this thread)
            //       until the status form is disposed.
            //
            // BUGFIX: If the entire AppDomain is unloading, stop
            //         looping as soon as possible.
            //
            string exitStatus = null;
            bool disposed = false;
            long milliseconds = 0;
            long iterations = 0;

            while (true)
            {
                //
                // NOTE: Either the entire AppDomain is being
                //       unloaded -OR- we should pretend like
                //       it is.
                //
                if (AppDomainOps.IsStoppingSoon())
                {
                    exitStatus = "domain unloaded";
                    break;
                }

                //
                // NOTE: Has the Disposed event fired yet?
                //
                if (!disposed &&
                    interpreter.CheckStatusDisposed())
                {
                    disposed = true;

                    if ((minimumMilliseconds == null) ||
                        (milliseconds >= (int)minimumMilliseconds))
                    {
                        break;
                    }
                }

                //
                // NOTE: Process all events on this thread
                //       and then sleep to prevent looping
                //       too fast.  Keep track of how much
                //       time has elapsed in this loop and
                //       bail when the timeout is exceeded.
                //
                GlobalState.PushActiveInterpreter(interpreter);

                try
                {
                    /* IGNORED */
                    WindowOps.ProcessEvents(interpreter);
                }
                finally
                {
                    GlobalState.PopActiveInterpreter();
                }

                //
                // NOTE: Sleep for a bit after processing
                //       the events?
                //
                if (sleepMilliseconds >= 0)
                {
                    try
                    {
                        /* NO RESULT */
                        HostOps.ThreadSleep(
                            sleepMilliseconds); /* throw */

                        milliseconds += sleepMilliseconds;
                    }
                    catch (ThreadAbortException)
                    {
                        Thread.ResetAbort();

                        exitStatus = "sleep aborted";
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        exitStatus = "sleep interrupted";
                        break;
                    }
                    catch (Exception)
                    {
                        //
                        // HACK: This should not happen.
                        //
                        exitStatus = "sleep exception";
                        break;
                    }
                }

                //
                // NOTE: We processed more events.  Keep a
                //       count of how many loop iterations
                //       are performed.
                //
                iterations++;

                //
                // NOTE: Honor the minimum and/or maximum
                //       milliseconds to wait passed by the
                //       caller.
                //
                if ((minimumMilliseconds == null) ||
                    (milliseconds >= (int)minimumMilliseconds))
                {
                    if ((maximumMilliseconds == null) ||
                        (milliseconds >= (int)maximumMilliseconds))
                    {
                        exitStatus = "form timed out";
                        break;
                    }
                }
            }

            if (disposed)
            {
                if (!String.IsNullOrEmpty(exitStatus))
                {
                    exitStatus = String.Format(
                        "form disposed and {0}", exitStatus);
                }
                else
                {
                    exitStatus = "form disposed";
                }
            }

            TracePriority priority = disposed ?
                TracePriority.StatusDebug : TracePriority.StatusError;

            TraceOps.DebugTrace(String.Format(
                "WaitOnDisposed: {0} after {1} milliseconds in {2} " +
                "iterations", exitStatus, milliseconds, iterations),
                typeof(StatusFormOps).Name, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops the status thread for the specified interpreter,
        /// discarding any error information. This method overload delegates to
        /// the primary overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status thread is stopped. This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// The optional value indicating whether to wait for the status thread to
        /// exit.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the status thread was stopped
        /// successfully; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode StopThread(
            Interpreter interpreter, /* in */
            bool? synchronous        /* in */
            )
        {
            Result error = null;

            return StopThread(interpreter, synchronous, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops the status thread for the specified interpreter,
        /// optionally waiting for it to exit.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status thread is stopped. This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// The optional value indicating whether to wait for the status thread to
        /// exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the status thread was stopped
        /// successfully; otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode StopThread(
            Interpreter interpreter, /* in */
            bool? synchronous,       /* in */
            ref Result error         /* out */
            )
        {
            EventWaitHandle doneEvent = null;

            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (!locked)
                    {
                        error = "unable to acquire lock";
                        return ReturnCode.Error;
                    }

                    bool localSynchronous = GetSynchronous(
                        interpreter, synchronous);

                    Thread thread = interpreter.StatusThread;

                    if (localSynchronous && (thread != null) &&
                        (thread == Thread.CurrentThread))
                    {
                        error = "cannot wait synchronously on current thread";
                        return ReturnCode.Error;
                    }

                    string doneEventName = interpreter.StatusDoneEventName;

                    if (doneEventName == null)
                    {
                        if (thread != null)
                        {
                            //
                            // HACK: This "error condition" is not super
                            //       important; however, it can be nice
                            //       to see a trace message when it has
                            //       been hit, since it should be quite
                            //       rare.  Meanwhile, it also purposely
                            //       updates the callers error message
                            //       so they can see it too, even upon a
                            //       nominal "successful" result.
                            //
                            error = "invalid status done event name";

                            TraceOps.DebugTrace(String.Format(
                                "StopThread: error = {0}",
                                FormatOps.WrapOrNull(error)),
                                typeof(StatusFormOps).Name,
                                TracePriority.StatusDebug);

                            return StrictStopThread ?
                                ReturnCode.Error : ReturnCode.Ok;
                        }
                        else
                        {
                            return ReturnCode.Ok;
                        }
                    }

                    bool success = false;

                    /* DISABLE */
                    interpreter.StatusDoneEventName = null;

                    try
                    {
                        doneEvent = ThreadOps.CreateEvent(doneEventName);

                        if (doneEvent == null)
                        {
                            error = String.Format(
                                "cannot create done event {0}",
                                FormatOps.WrapOrNull(doneEventName));

                            return ReturnCode.Error;
                        }

                        if (ThreadOps.SetEvent(doneEvent))
                        {
                            success = true;
                        }
                        else
                        {
                            error = String.Format(
                                "failed to signal done event {0}",
                                FormatOps.WrapOrNull(doneEventName));

                            return ReturnCode.Error;
                        }

                        if (localSynchronous)
                        {
                            try
                            {
                                /* NO RESULT */
                                ThreadOps.MaybeShutdown(
                                    interpreter, null,
                                    ShutdownFlags.Status,
                                    ref thread);
                            }
                            finally
                            {
                                /* IGNORED */
                                interpreter.MaybeResetStatusThread(
                                    thread, true);
                            }

                            //
                            // HACK: At this point, we SHOULD only
                            //       be waiting for the thread to
                            //       exit, so we can drop the lock.
                            //
                            interpreter.InternalExitLock(
                                ref locked); /* TRANSACTIONAL */

                            //
                            // HACK: On Mono, always wait a minimum of
                            //       one second prior to considering a
                            //       status form as fully disposed, so
                            //       that it actually gets removed from
                            //       the screen.
                            //
                            WaitOnDisposed(
                                interpreter, DisposeSleepMilliseconds,
                                GetDisposeMinimumMilliseconds(),
                                GetDisposeMaximumMilliseconds());
                        }

                        return ReturnCode.Ok;
                    }
                    finally
                    {
                        if (!success)
                        {
                            interpreter.InternalHardTryLock(
                                ref locked); /* TRANSACTIONAL */

                            //
                            // HACK: At this point, it does not
                            //       really matter if we got the
                            //       lock again, because we are
                            //       handling a rare (?) failure
                            //       and we need to restore the
                            //       previous state.
                            //
                            if (!locked)
                            {
                                TraceOps.LockTrace("StopThread",
                                    typeof(StatusFormOps).Name,
                                    false, TracePriority.LockWarning,
                                    interpreter.MaybeWhoHasLock());
                            }

                            //
                            // WARNING: This method will only reset
                            //          the "done" event name if it
                            //          is still null at this point.
                            //
                            interpreter.MaybeRestoreStatusDoneEventName(
                                doneEventName);
                        }
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                /* NO RESULT */
                ThreadOps.CloseEvent(ref doneEvent);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops the status thread for the specified interpreter,
        /// complaining about any failure unless complaints have been disabled.
        /// This method overload delegates to the primary overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status thread is stopped. This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// The optional value indicating whether to wait for the status thread to
        /// exit.
        /// </param>
        private static void StopThreadOrMaybeComplain(
            Interpreter interpreter, /* in */
            bool? synchronous        /* in */
            )
        {
            /* NO RESULT */
            StopThreadOrMaybeComplain(
                interpreter, synchronous, NoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops the status thread for the specified interpreter,
        /// optionally complaining about any failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status thread is stopped. This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// The optional value indicating whether to wait for the status thread to
        /// exit.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress complaints about any failure encountered while
        /// stopping the status thread.
        /// </param>
        private static void StopThreadOrMaybeComplain(
            Interpreter interpreter, /* in */
            bool? synchronous,       /* in */
            bool noComplain          /* in */
            )
        {
            ReturnCode code;
            Result error = null;

            code = StopThread(interpreter, synchronous, ref error);

            if (!noComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(interpreter, code, error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status Text Handling Methods
        /// <summary>
        /// This method waits for the specified interpreter to become ready,
        /// tracing any error that occurs. This method overload delegates to the
        /// primary overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to wait for. This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The number of milliseconds to wait, or zero to not wait.
        /// </param>
        /// <param name="trace">
        /// Non-zero to enable diagnostic tracing for the wait.
        /// </param>
        /// <returns>
        /// True if the wait succeeded; otherwise, false.
        /// </returns>
        private static bool MaybeWaitFor(
            Interpreter interpreter, /* in */
            int timeout,             /* in */
            bool trace               /* in */
            )
        {
            Result error = null;

            if (MaybeWaitFor(
                    interpreter, timeout, trace, ref error))
            {
                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "MaybeWaitFor: error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(StatusFormOps).Name,
                    TracePriority.StatusError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified interpreter to become ready.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to wait for. This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The number of milliseconds to wait, or zero to not wait.
        /// </param>
        /// <param name="trace">
        /// Non-zero to enable diagnostic tracing for the wait.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// True if the wait succeeded; otherwise, false.
        /// </returns>
        private static bool MaybeWaitFor(
            Interpreter interpreter, /* in */
            int timeout,             /* in */
            bool trace,              /* in */
            ref Result error         /* out */
            )
        {
            if (timeout != 0)
            {
                if (EventOps.Wait(interpreter, null,
                        PerformanceOps.GetMicrosecondsFromMilliseconds(
                        timeout), null, true, false, false, false, trace,
                        ref error) != ReturnCode.Ok)
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the text within the status form text box for the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status form text box is cleared. This parameter
        /// may be null.
        /// </param>
        /// <param name="synchronous">
        /// The optional value indicating whether the operation should be
        /// performed synchronously.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, <see
        /// cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Clear(
            Interpreter interpreter, /* in */
            bool? synchronous,       /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            int levels = interpreter.EnterStatusLevel();

            try
            {
                //
                // HACK: This check is not foolproof; the status
                //       thread may exit and prevent this thread
                //       from using the text box via its message
                //       loop *after* this check and *before* we
                //       actually interact with the text box.
                //       In that particular case, everything is
                //       fine because you cannot logically clear
                //       a status text box that no longer exists.
                //       Furthermore, the FormOps methods are
                //       hardened against exceptions, including
                //       those involving WinForm disposal.
                //
                if (levels == SecondaryLevels)
                {
                    return FormOps.ClearText(GetTextBox(
                        interpreter), !GetSynchronous(
                        interpreter, synchronous),
                        ref error);
                }
                else
                {
                    error = String.Format(
                        "invalid secondary status level {0}, " +
                        "must be {1}", levels, SecondaryLevels);

                    return ReturnCode.Error;
                }
            }
            finally
            {
                /* IGNORED */
                interpreter.ExitStatusLevel();

                /* NO RESULT */
                MaybeWaitFor(interpreter, Interlocked.CompareExchange(
                    ref RequestWaitMilliseconds, 0, 0), TraceWait);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified text to the status form text box for
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose status form text box is appended to. This
        /// parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text to append to the status form text box. This parameter may be
        /// null.
        /// </param>
        /// <param name="synchronous">
        /// The optional value indicating whether the operation should be
        /// performed synchronously.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, <see
        /// cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode Report(
            Interpreter interpreter, /* in */
            string text,             /* in */
            bool? synchronous,       /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            int levels = interpreter.EnterStatusLevel();

            try
            {
                //
                // HACK: This check is not foolproof; the status
                //       thread may exit and prevent this thread
                //       from using the text box via its message
                //       loop *after* this check and *before* we
                //       actually interact with the text box.
                //       In that particular case, everything is
                //       fine because you cannot logically clear
                //       a status text box that no longer exists.
                //       Furthermore, the FormOps methods are
                //       hardened against exceptions, including
                //       those involving WinForm disposal.
                //
                if (levels == SecondaryLevels)
                {
                    return FormOps.AppendToText(GetTextBox(
                        interpreter), text, !GetSynchronous(
                        interpreter, synchronous), ref error);
                }
                else
                {
                    error = String.Format(
                        "invalid secondary status level {0}, " +
                        "must be {1}", levels, SecondaryLevels);

                    return ReturnCode.Error;
                }
            }
            finally
            {
                /* IGNORED */
                interpreter.ExitStatusLevel();

                /* NO RESULT */
                MaybeWaitFor(interpreter, Interlocked.CompareExchange(
                    ref RequestWaitMilliseconds, 0, 0), TraceWait);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods (System.Windows.Forms)
        /// <summary>
        /// This method creates and configures the text box used by the status
        /// form.
        /// </summary>
        /// <param name="emSize">
        /// The em-size, in points, used for the text box font.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// The newly created text box, or null if it could not be created.
        /// </returns>
        private static TextBox CreateTextBox(
            float emSize,    /* in */
            ref Result error /* out */
            )
        {
            bool success = false;
            TextBox textBox = null;

            try
            {
                textBox = new TextBox();

                textBox.ReadOnly = true;
                textBox.AutoSize = false;
                textBox.Multiline = true;
                textBox.WordWrap = true;
                textBox.ScrollBars = ScrollBars.Both;
                textBox.Dock = DockStyle.Fill;

#if DRAWING
                textBox.Font = MakeFont(
                    textBox.Font, new FontFamily(
                    GenericFontFamilies.Monospace),
                    emSize);
#endif

                success = true;
                return textBox;
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }
            finally
            {
                if (!success && (textBox != null))
                {
                    textBox.Dispose();
                    textBox = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs the title text used for the status form
        /// associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the title text is constructed. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The constructed title text.
        /// </returns>
        private static string GetText(
            Interpreter interpreter /* in */
            )
        {
            return String.Format(
                NameFormat, GlobalState.GetPackageNameNoCase(),
                FormatOps.InterpreterNoThrow(interpreter, false),
                ProcessOps.GetId(), GlobalState.GetCurrentSystemThreadId(),
                AppDomainOps.GetCurrentId());
        }

        ///////////////////////////////////////////////////////////////////////

#if DRAWING
        /// <summary>
        /// This method determines the DPI dimensions to use for automatic scaling
        /// of the status form.
        /// </summary>
        /// <param name="control">
        /// The control used to detect the DPI dimensions, when necessary. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The DPI dimensions to use for automatic scaling, or null if none could
        /// be determined.
        /// </returns>
        private static SizeF? GetAutoScaleDpi(
            Control control /* in */
            )
        {
            SizeF? result = ForceAutoScaleDpi;

            if (result != null)
                return result;

            result = FallbackAutoScaleDpi;

            if (control == null)
                return result;

            using (Graphics graphics = control.CreateGraphics())
            {
                if (graphics == null)
                    return result;

                return new SizeF(graphics.DpiX, graphics.DpiY);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates the status form (and its text box), optionally
        /// showing it.
        /// </summary>
        /// <param name="text">
        /// The title text for the status form. This parameter may be null.
        /// </param>
        /// <param name="tag">
        /// The opaque object to associate with the status form. This parameter
        /// may be null.
        /// </param>
        /// <param name="emSize">
        /// The em-size, in points, used for the text box font.
        /// </param>
        /// <param name="canClose">
        /// Non-zero if the status form may be closed by the user.
        /// </param>
        /// <param name="topMost">
        /// Non-zero to make the status form top-most.
        /// </param>
        /// <param name="show">
        /// Non-zero to show the status form after creating it.
        /// </param>
        /// <param name="form">
        /// Upon success, this parameter receives the created status form.
        /// </param>
        /// <param name="textBox">
        /// Upon success, this parameter receives the created status form text
        /// box.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, <see
        /// cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode Create(
            string text,         /* in: OPTIONAL */
            object tag,          /* in: OPTIONAL */
            float emSize,        /* in */
            bool canClose,       /* in */
            bool topMost,        /* in */
            bool show,           /* in */
            ref Form form,       /* out */
            ref TextBox textBox, /* out */
            ref Result error     /* out */
            )
        {
            bool[] success = { false, false };
            TextBox localTextBox = null;
            Form localForm = null;

            try
            {
                localTextBox = CreateTextBox(emSize, ref error);

                if (localTextBox == null)
                    return ReturnCode.Error;

                localForm = new Form();

#if DRAWING
                if (UseAutoScaleDpi)
                {
                    SizeF? dpi = GetAutoScaleDpi(localForm);

                    if (dpi != null)
                    {
                        localForm.AutoScaleDimensions = (SizeF)dpi;
                        localForm.AutoScaleMode = AutoScaleMode.Dpi;
                    }
                }
#endif

                localForm.SuspendLayout();
                localForm.Controls.Add(localTextBox);
                success[0] = true;

                localForm.Text = text;
                localForm.Tag = tag;
                localForm.KeyPreview = true;

                localForm.Width = DefaultWidth;
                localForm.Height = DefaultHeight;
                localForm.TopMost = topMost;

                localForm.KeyUp += new KeyEventHandler(HandleKeyUp);

                localForm.FormClosed += new FormClosedEventHandler(
                    HandleClosed);

                localForm.Disposed += new EventHandler(HandleDisposed);

#if NATIVE && WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    IntPtr hWnd = localForm.Handle;

                    if (!canClose &&
                        !WindowOps.PreventWindowClose(hWnd, ref error))
                    {
                        return ReturnCode.Error;
                    }

#if DRAWING
                    using (Stream stream = AssemblyOps.GetIconStream())
                    {
                        if (stream != null)
                            localForm.Icon = new Icon(stream);
                    }
#endif
                }
#endif

                localForm.ResumeLayout();

                if (show)
                    localForm.Show();

                form = localForm;
                textBox = localTextBox;

                success[1] = true;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
            finally
            {
                if (!success[0] && (localTextBox != null))
                {
                    localTextBox.Dispose();
                    localTextBox = null;
                }

                if (!success[1] && (localForm != null))
                {
                    localForm.Dispose();
                    localForm = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Drawing.Font Methods
#if DRAWING
        /// <summary>
        /// This method computes a larger font size based on the specified font or
        /// em-size.
        /// </summary>
        /// <param name="font">
        /// The font whose size is used when no em-size is supplied. This
        /// parameter may be null.
        /// </param>
        /// <param name="emSize">
        /// The optional em-size, in points, to use as the basis for the
        /// computation.
        /// </param>
        /// <returns>
        /// The computed larger font size, in points.
        /// </returns>
        private static float BiggerFontSize(
            Font font,    /* in */
            float? emSize /* in */
            )
        {
            if (emSize != null)
                return (float)emSize * BiggerFontSizeMultiplier;

            if (font == null)
                return DefaultFontSize;

            return font.Size * BiggerFontSizeMultiplier;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a font from the specified font, family, and em-
        /// size.
        /// </summary>
        /// <param name="font">
        /// The font whose style and other attributes are reused, when supplied.
        /// This parameter may be null.
        /// </param>
        /// <param name="family">
        /// The font family to use, when supplied. This parameter may be null.
        /// </param>
        /// <param name="emSize">
        /// The em-size, in points, for the new font.
        /// </param>
        /// <returns>
        /// The newly created font.
        /// </returns>
        private static Font MakeFont(
            Font font,         /* in */
            FontFamily family, /* in */
            float emSize       /* in */
            )
        {
            if (font != null)
            {
                return new Font(
                    family, BiggerFontSize(font, emSize),
                    font.Style, font.Unit, font.GdiCharSet,
                    font.GdiVerticalFont);
            }
            else if (family != null)
            {
                return new Font(
                    family, emSize, FontStyle.Regular,
                    GraphicsUnit.Point, 0, false);
            }
            else
            {
                return new Font(
                    FontFamily.GenericSansSerif, emSize,
                    FontStyle.Regular, GraphicsUnit.Point,
                    0, false);
            }
        }
#endif
        #endregion
    }
}
