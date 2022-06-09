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

namespace Eagle._Components.Private
{
    [ObjectId("903b723a-5915-475b-a75b-f6f5ae1879a1")]
    internal static class StatusFormOps
    {
        #region Keyboard Event Handlers Helper Class
        [ObjectId("533e3415-7235-428c-b59a-e5873129d70d")]
        private static class KeyEventCallbacks
        {
            #region Private Static Data
            //
            // TODO: This should cause the e.Handled property to be set to
            //       true -AND- the e.SuppressKeyPress property to be left
            //       alone.
            //
            private static FormEventResultTriplet DefaultResult =
                new AnyTriplet<bool?, bool?, ReturnCode?>(
                    null, true, null);
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
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
                    Interpreter.ConsoleCancelEventHandler(sender, null);
                }

                return DefaultResult;
            }
#endif

            ///////////////////////////////////////////////////////////////////

#if SHELL
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
                        InteractiveLoopData.Create(), true,
                        ref error);

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
        private const string NameFormat =
            "Status: {0} interpreter {1}, process {2}, thread {3}, domain {4}";

        ///////////////////////////////////////////////////////////////////////

        private const string clearPromptFormat =
            "clear status form {0}: are you sure?";

        private const string closePromptFormat =
            "close status form {0}: are you sure?";

        private const string disposePromptFormat =
            "dispose of interpreter {0} immediately: are you sure?";

        private const string cancelPromptFormat =
            "immediately cancel all running scripts: are you sure?";

        private const string shellPromptFormat =
            "start new interactive loop thread: are you sure?";

        ///////////////////////////////////////////////////////////////////////

        private const int PrimaryLevels = 1;
        private const int SecondaryLevels = 3;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: This is purposely not read-only.
        //
        private static int ProcessRunning = 1;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static int LoopWaitMicroseconds = 50000;
        private static int RequestWaitMilliseconds = 100;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static int DisposeSleepMilliseconds = 100;
        private static int? DisposeMinimumMilliseconds = 1000;
        private static int? DisposeMaximumMilliseconds = 2000;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool NoComplain = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static bool ForcePreEvents = false;
        private static bool ForceStayOpen = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static int DefaultWidth = 600;
        private static int DefaultHeight = 300;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static float DefaultFontSize = 8.25f;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static bool DefaultTopMost = false;
        private static bool DefaultCanClose = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static bool UseActiveInterpreter = false;
        private static bool AllowHotKeys = false;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && TEST
        //
        // HACK: This is purposely not read-only.
        //
        private static NativeWindowType ownerWindowType =
            NativeWindowType.None;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to synchronize access to the static class
        //       data defined below this point.
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the set of keyboard mappings for this AppDomain.
        //
        private static KeyOps.KeyEventMap keyEventMap;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Initialization Support Methods
        public static void Initialize()
        {
            InitializeKeyEventCallbacks();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

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

            int localProcessRunning = Interlocked.CompareExchange(
                ref ProcessRunning, 0, 0);

            if (empty || (localProcessRunning != 0))
            {
                localList.Add("ProcessRunning",
                    localProcessRunning.ToString());
            }

            if (empty || NoComplain)
                localList.Add("NoComplain", NoComplain.ToString());

            if (empty || ForcePreEvents)
                localList.Add("ForcePreEvents", ForcePreEvents.ToString());

            if (empty || ForceStayOpen)
                localList.Add("ForceStayOpen", ForceStayOpen.ToString());

            if (empty || (DefaultWidth != 0))
                localList.Add("DefaultWidth", DefaultWidth.ToString());

            if (empty || (DefaultHeight != 0))
                localList.Add("DefaultHeight", DefaultHeight.ToString());

            if (empty || (DefaultFontSize != 0.0f))
                localList.Add("DefaultFontSize", DefaultFontSize.ToString());

            if (empty || DefaultTopMost)
                localList.Add("DefaultTopMost", DefaultTopMost.ToString());

            if (empty || DefaultCanClose)
                localList.Add("DefaultCanClose", DefaultCanClose.ToString());

            if (empty || UseActiveInterpreter)
            {
                localList.Add("UseActiveInterpreter",
                    UseActiveInterpreter.ToString());
            }

            if (empty || AllowHotKeys)
                localList.Add("AllowHotKeys", AllowHotKeys.ToString());

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
        public static KeyOps.KeyEventMap GetKeyEventMap()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return keyEventMap;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static TextBox GetTextBox(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.StatusObject as TextBox;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    interpreter = form.Tag as Interpreter;

                    if (interpreter != null)
                        return interpreter;
                }
            }

            TraceOps.DebugTrace(String.Format(
                "GetInterpreterFromSender: no interpreter found via {0}",
                FormOps.ToString(sender, true)), typeof(StatusFormOps).Name,
                TracePriority.UserInterfaceError);

            return UseActiveInterpreter ? Interpreter.GetActive() : null;
        }

        ///////////////////////////////////////////////////////////////////////

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
#if DRAWING
        private static void HandleResize(
            object sender, /* in */
            EventArgs e    /* in */
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

            Form form = GetFormFromSender(sender, false);

            if (form == null)
                return;

            FormOps.ResizeControl(
                FormOps.GetFirstControl(form), form.ClientSize);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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

            StopThreadOrMaybeComplain(interpreter, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

                    try
                    {
                        Result error = null;

                        if (Create(
                                GetText(interpreter), interpreter,
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

                                        if ((EventOps.Wait(interpreter,
                                                ForceStayOpen ? null : doneEvent,
                                                LoopWaitMicroseconds, null, true,
                                                false, true, false, ref timedOut,
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

        private static void StatusFormOps_Exited(
            object sender, /* in */
            EventArgs e    /* in */
            )
        {
            /* NO RESULT */
            Exit();

            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= StatusFormOps_Exited;
                else
                    appDomain.ProcessExit -= StatusFormOps_Exited;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Threading.Thread Methods
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
                        interpreter, timeout, ref error))
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

                            interpreter.StatusStartEventName = startEventName;
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
                    interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
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

        private static int? GetDisposeMinimumMilliseconds()
        {
            //
            // TODO: Use Mono detection here?
            //
            return DisposeMinimumMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int? GetDisposeMaximumMilliseconds()
        {
            //
            // TODO: Use Mono detection here?
            //
            return DisposeMaximumMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

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
                /* IGNORED */
                WindowOps.ProcessEvents(interpreter);

                //
                // NOTE: Sleep for a bit after processing
                //       the events?
                //
                if (sleepMilliseconds >= 0)
                {
                    /* NO RESULT */
                    Thread.Sleep(sleepMilliseconds);
                    milliseconds += sleepMilliseconds;
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

            TracePriority priority = disposed ?
                TracePriority.StatusDebug : TracePriority.StatusError;

            TraceOps.DebugTrace(String.Format("WaitOnDisposed: " +
                "{0} after {1} milliseconds in {2} iterations",
                disposed ? "form disposed" : exitStatus, milliseconds,
                iterations), typeof(StatusFormOps).Name, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode StopThread(
            Interpreter interpreter, /* in */
            bool synchronous         /* in */
            )
        {
            Result error = null;

            return StopThread(interpreter, synchronous, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode StopThread(
            Interpreter interpreter, /* in */
            bool synchronous,        /* in */
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

                    string doneEventName = interpreter.StatusDoneEventName;

                    if (doneEventName == null)
                    {
                        error = "invalid status done event name";
                        return ReturnCode.Error;
                    }

                    bool success = false;

                    interpreter.StatusDoneEventName = null;

                    try
                    {
                        doneEvent = ThreadOps.OpenEvent(doneEventName);

                        if (doneEvent == null)
                        {
                            error = String.Format(
                                "cannot signal done event {0}",
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

                        if (synchronous)
                        {
                            Thread thread = interpreter.StatusThread;

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
                            /* RESTORE */
                            interpreter.StatusDoneEventName = doneEventName;
                        }
                    }
                }
                finally
                {
                    interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
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

        private static void StopThreadOrMaybeComplain(
            Interpreter interpreter, /* in */
            bool synchronous         /* in */
            )
        {
            ReturnCode code;
            Result error = null;

            code = StopThread(interpreter, synchronous, ref error);

            if (!NoComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(interpreter, code, error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Status Text Handling Methods
        private static bool MaybeWaitFor(
            Interpreter interpreter, /* in */
            int timeout              /* in */
            )
        {
            Result error = null;

            if (MaybeWaitFor(
                    interpreter, timeout, ref error))
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

        private static bool MaybeWaitFor(
            Interpreter interpreter, /* in */
            int timeout,             /* in */
            ref Result error         /* out */
            )
        {
            if (timeout != 0)
            {
                if (EventOps.Wait(interpreter, null,
                        PerformanceOps.GetMicroseconds(
                        timeout), null, true, false, false,
                        false, ref error) != ReturnCode.Ok)
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Clear(
            Interpreter interpreter, /* in */
            bool asynchronous,       /* in */
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
                //
                if (levels == SecondaryLevels)
                {
                    return FormOps.ClearText(
                        GetTextBox(interpreter),
                        asynchronous, ref error);
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
                    ref RequestWaitMilliseconds, 0, 0));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Report(
            Interpreter interpreter, /* in */
            string text,             /* in */
            bool asynchronous,       /* in */
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
                //
                if (levels == SecondaryLevels)
                {
                    return FormOps.AppendToText(
                        GetTextBox(interpreter), text,
                        asynchronous, ref error);
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
                    ref RequestWaitMilliseconds, 0, 0));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods (System.Windows.Forms)
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
                textBox.AutoSize = true;
                textBox.Multiline = true;
                textBox.ScrollBars = ScrollBars.Both;

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

                localForm.SuspendLayout();
                localForm.Controls.Add(localTextBox);
                success[0] = true;

                localForm.Text = text;
                localForm.Tag = tag;
                localForm.KeyPreview = true;

                localForm.Width = DefaultWidth;
                localForm.Height = DefaultHeight;
                localForm.TopMost = topMost;

#if DRAWING
                FormOps.ResizeControl(
                    localTextBox, localForm.ClientSize);

                localForm.Resize += new EventHandler(HandleResize);
#endif

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
                    Icon icon = new Icon(
                        AssemblyOps.GetIconStream());

                    localForm.Icon = icon;
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
                    localForm.Close();
                    localForm = null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Drawing.Font Methods
#if DRAWING
        private static float BiggerFontSize(
            Font font /* in */
            )
        {
            return (font != null) ? font.Size * 2 : DefaultFontSize;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Font MakeFont(
            Font font,         /* in */
            FontFamily family, /* in */
            float emSize       /* in */
            )
        {
            if (font != null)
            {
                return new Font(
                    family, BiggerFontSize(font),
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
