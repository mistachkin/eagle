/*
 * Debugger.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("9be2b241-bee5-428f-9df8-df354ef63ea2")]
    internal sealed class Debugger :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDebugger, IDisposable
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static bool DefaultEnabled = true;
        private static BreakpointType DefaultTypes = BreakpointType.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Enumerations
        [ObjectId("b53c003a-bb94-4c10-900c-7403dc36c2a8")]
        private enum Context
        {
            Current = 0,     // context for current value of property.
            Saved = 1,       // context for saved value of property.
            First = Current, // first context slot for property value.
            Last = Saved     // last context slot for property value.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private InterruptCallback interruptCallback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Debugger(
            bool isolated,
            string culture,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            PluginFlags pluginFlags,
            AppDomain appDomain,
            IHost host,
            string libraryPath,
            StringList autoPathList
            )
        {
            interpreter = isolated ? DebuggerOps.CreateInterpreter(
                culture, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, interpreterFlags, pluginFlags, appDomain,
                host, libraryPath, autoPathList) : null;

            ReturnCode code;
            Result error = null;

            code = Initialize(ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Callback Methods
        private ReturnCode InterruptCallback(
            Interpreter interpreter, /* NOTE: Parent interpreter. */
            InterruptType interruptType,
            IClientData clientData,
            ref Result error
            ) /* throw */
        {
            //
            // NOTE: If the are no callback arguments configured, just skip it
            //       and return success.
            //
            StringList arguments = CallbackArguments;

            if (arguments == null) /* NOTE: Disabled? */
                return ReturnCode.Ok;

            Interpreter debugInterpreter = this.interpreter;

            if (debugInterpreter == null)
            {
                error = "debugger interpreter not available";
                return ReturnCode.Error;
            }

            //
            // NOTE: *WARNING* This is a cross-interpreter call, do NOT dispose
            //       the parent interpreter because we do not own it.  This is
            //       guaranteed by using the NoDispose object flag (indirectly)
            //       here.
            //
            ICallback callback = CommandCallback.Create(
                MarshalFlags.Default, CallbackFlags.Default,
                ObjectFlags.Callback, ByRefArgumentFlags.None,
                debugInterpreter, clientData, null, new StringList(
                arguments), ref error);

            if (callback == null)
                return ReturnCode.Error;

            try
            {
                callback.FireEventHandler(this,
                    RuntimeOps.GetInterruptEventArgs(interpreter,
                        interruptType, clientData) as EventArgs);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static BreakpointType GetDefaultTypes(
            bool? enabled,
            bool tokens
            )
        {
            if ((enabled == null) || (bool)enabled)
                return GetDefaultTypes(tokens);

            return BreakpointType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static BreakpointType GetDefaultTypes(
            bool tokens
            )
        {
            BreakpointType types = DefaultTypes;

            if (tokens)
                types |= BreakpointType.Token;

            return types;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void Initialize()
        {
            enabled = new bool[] { DefaultEnabled, false };
            loops = new int[] { 0, 0 };
            active = new int[] { 0, 0 };
            singleStep = new bool[] { false, false };

#if DEBUGGER_BREAKPOINTS
            breakOnToken = new bool[] { false, false };
#endif

            breakOnExecute = new bool[] { false, false };
            breakOnCancel = new bool[] { false, false };
            breakOnError = new bool[] { false, false };
            breakOnReturn = new bool[] { false, false };
            breakOnTest = new bool[] { false, false };
            breakOnExit = new bool[] { false, false };
            steps = new long[] { 0, 0 };

            types = new BreakpointType[] {
                GetDefaultTypes(DefaultEnabled, false),
                GetDefaultTypes(false, false)
            };

#if DEBUGGER_BREAKPOINTS
            breakpoints = new BreakpointDictionary[] {
                new BreakpointDictionary(), null
            };
#endif

#if DEBUGGER_ARGUMENTS
            executeArguments = new ArgumentList[] { null, null };
#endif

            command = new string[] { null, null };
            result = new Result[] { null, null };
            queue = new QueueList<string, string>[] { null, null };

            callbackArguments = new StringList[] { null, null };
        }

        ///////////////////////////////////////////////////////////////////////

        private void Reset(Context target)
        {
            enabled[(int)target] = false;
            loops[(int)target] = 0;
            active[(int)target] = 0;
            singleStep[(int)target] = false;

#if DEBUGGER_BREAKPOINTS
            breakOnToken[(int)target] = false;
#endif

            breakOnExecute[(int)target] = false;
            breakOnCancel[(int)target] = false;
            breakOnError[(int)target] = false;
            breakOnReturn[(int)target] = false;
            breakOnTest[(int)target] = false;
            breakOnExit[(int)target] = false;
            steps[(int)target] = 0;
            types[(int)target] = BreakpointType.None;

#if DEBUGGER_BREAKPOINTS
            breakpoints[(int)target] = null;
#endif

#if DEBUGGER_ARGUMENTS
            executeArguments[(int)target] = null;
#endif

            command[(int)target] = null;
            result[(int)target] = null;
            queue[(int)target] = null;

            callbackArguments[(int)target] = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private void Copy(Context source, Context target)
        {
            enabled[(int)target] = enabled[(int)source];
            loops[(int)target] = loops[(int)source];
            active[(int)target] = active[(int)source];
            singleStep[(int)target] = singleStep[(int)source];

#if DEBUGGER_BREAKPOINTS
            breakOnToken[(int)target] = breakOnToken[(int)source];
#endif

            breakOnExecute[(int)target] = breakOnExecute[(int)source];
            breakOnCancel[(int)target] = breakOnCancel[(int)source];
            breakOnError[(int)target] = breakOnError[(int)source];
            breakOnReturn[(int)target] = breakOnReturn[(int)source];
            breakOnTest[(int)target] = breakOnTest[(int)source];
            breakOnExit[(int)target] = breakOnExit[(int)source];
            steps[(int)target] = steps[(int)source];
            types[(int)target] = types[(int)source];

#if DEBUGGER_BREAKPOINTS
            breakpoints[(int)target] = breakpoints[(int)source];
#endif

#if DEBUGGER_ARGUMENTS
            executeArguments[(int)target] = executeArguments[(int)source];
#endif

            command[(int)target] = command[(int)source];
            result[(int)target] = result[(int)source];
            queue[(int)target] = queue[(int)source];

            callbackArguments[(int)target] = callbackArguments[(int)source];
        }

        ///////////////////////////////////////////////////////////////////////

        private void ForceResume()
        {
            suspendCount = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private int EnterSuspend()
        {
            return ++suspendCount;
        }

        ///////////////////////////////////////////////////////////////////////

        private int ExitSuspend()
        {
            return --suspendCount;
        }

        ///////////////////////////////////////////////////////////////////////

        private QueueList<string, string> GetQueue()
        {
            QueueList<string, string> queue = Queue;

            if (queue == null)
            {
                queue = new QueueList<string, string>();
                Queue = queue;
            }

            return queue;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetQueueKey()
        {
            //
            // HACK: Use something better here?
            //
            return GlobalState.NextId().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get
            {
                //
                // NOTE: Obviously, this would be pointless.
                //
                // CheckDisposed();

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get
            {
                //
                // NOTE: Obviously, this would also be pointless.
                //
                // CheckDisposed();

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter; /* out-of-band debug interpreter */
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebuggerData Members
        private int suspendCount;
        public int SuspendCount
        {
            get { CheckDisposed(); return suspendCount; }
            set { CheckDisposed(); suspendCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] enabled;
        public bool Enabled
        {
            get { CheckDisposed(); return enabled[(int)Context.Current]; }
            set { CheckDisposed(); enabled[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Special flag for to detect how many integeractive loops are
        //       in use by the debugger.
        //
        private int[] loops;
        public int Loops
        {
            get { CheckDisposed(); return loops[(int)Context.Current]; }
            set { CheckDisposed(); loops[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Special flag for scripts to use to detect they are being
        //       executed while in the interactive debugger.
        //
        private int[] active;
        public int Active
        {
            get { CheckDisposed(); return active[(int)Context.Current]; }
            set { CheckDisposed(); active[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] singleStep;
        public bool SingleStep
        {
            get { CheckDisposed(); return singleStep[(int)Context.Current]; }
            set { CheckDisposed(); singleStep[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER_BREAKPOINTS
        private bool[] breakOnToken;
        public bool BreakOnToken
        {
            get
            {
                CheckDisposed();

                return breakOnToken[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnToken[(int)Context.Current] = value;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool[] breakOnExecute;
        public bool BreakOnExecute
        {
            get
            {
                CheckDisposed();

                return breakOnExecute[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnExecute[(int)Context.Current] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] breakOnCancel;
        public bool BreakOnCancel
        {
            get
            {
                CheckDisposed();

                return breakOnCancel[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnCancel[(int)Context.Current] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] breakOnError;
        public bool BreakOnError
        {
            get
            {
                CheckDisposed();

                return breakOnError[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnError[(int)Context.Current] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] breakOnReturn;
        public bool BreakOnReturn
        {
            get
            {
                CheckDisposed();

                return breakOnReturn[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnReturn[(int)Context.Current] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] breakOnTest;
        public bool BreakOnTest
        {
            get
            {
                CheckDisposed();

                return breakOnTest[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnTest[(int)Context.Current] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool[] breakOnExit;
        public bool BreakOnExit
        {
            get
            {
                CheckDisposed();

                return breakOnExit[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakOnExit[(int)Context.Current] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private long[] steps;
        public long Steps
        {
            get { CheckDisposed(); return steps[(int)Context.Current]; }
            set { CheckDisposed(); steps[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType[] types;
        public BreakpointType Types
        {
            get { CheckDisposed(); return types[(int)Context.Current]; }
            set { CheckDisposed(); types[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER_BREAKPOINTS
        private BreakpointDictionary[] breakpoints;
        public BreakpointDictionary Breakpoints
        {
            get
            {
                CheckDisposed();

                return breakpoints[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                breakpoints[(int)Context.Current] = value;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER_ARGUMENTS
        private ArgumentList[] executeArguments;
        public ArgumentList ExecuteArguments
        {
            get
            {
                CheckDisposed();

                return executeArguments[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                executeArguments[(int)Context.Current] = value;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private string[] command;
        public string Command
        {
            get { CheckDisposed(); return command[(int)Context.Current]; }
            set { CheckDisposed(); command[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result[] result;
        public Result Result
        {
            get { CheckDisposed(); return result[(int)Context.Current]; }
            set { CheckDisposed(); result[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private QueueList<string, string>[] queue;
        public QueueList<string, string> Queue
        {
            get { CheckDisposed(); return queue[(int)Context.Current]; }
            set { CheckDisposed(); queue[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList[] callbackArguments;
        public StringList CallbackArguments
        {
            get
            {
                CheckDisposed();

                return callbackArguments[(int)Context.Current];
            }
            set
            {
                CheckDisposed();

                callbackArguments[(int)Context.Current] = value;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugger Members
        public void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            bool empty = HostOps.HasEmptyContent(detailFlags);

            if (empty || (suspendCount > 0))
                list.Add("SuspendCount", suspendCount.ToString());

            if (empty || Enabled)
                list.Add("Enabled", Enabled.ToString());

            if (empty || (Loops > 0))
                list.Add("Loops", Loops.ToString());

            if (empty || (Active > 0))
                list.Add("Active", Active.ToString());

            if (empty || SingleStep)
                list.Add("SingleStep", SingleStep.ToString());

#if DEBUGGER_BREAKPOINTS
            if (empty || BreakOnToken)
                list.Add("BreakOnToken", BreakOnToken.ToString());
#endif

            if (empty || BreakOnExecute)
                list.Add("BreakOnExecute", BreakOnExecute.ToString());

            if (empty || BreakOnCancel)
                list.Add("BreakOnCancel", BreakOnCancel.ToString());

            if (empty || BreakOnError)
                list.Add("BreakOnError", BreakOnError.ToString());

            if (empty || BreakOnReturn)
                list.Add("BreakOnReturn", BreakOnReturn.ToString());

            if (empty || BreakOnTest)
                list.Add("BreakOnTest", BreakOnTest.ToString());

            if (empty || BreakOnExit)
                list.Add("BreakOnExit", BreakOnExit.ToString());

            if (empty || (Steps > 0))
                list.Add("Steps", Steps.ToString());

            if (empty || (Types != BreakpointType.None))
                list.Add("Types", Types.ToString());

#if DEBUGGER_BREAKPOINTS
            BreakpointDictionary breakpoints = Breakpoints;

            if (empty || ((breakpoints != null) && (breakpoints.Count > 0)))
                list.Add("Breakpoints", (breakpoints != null) ?
                    breakpoints.Count.ToString() : FormatOps.DisplayNull);
#endif

#if DEBUGGER_ARGUMENTS
            ArgumentList executeArguments = ExecuteArguments;

            if (empty || (executeArguments != null))
                list.Add("ExecuteArguments", (executeArguments != null) ?
                    executeArguments.ToString(ToStringFlags.NameAndValue,
                    null, false) : FormatOps.DisplayNull);
#endif

            if (empty || !String.IsNullOrEmpty(Command))
                list.Add("Command", FormatOps.DisplayString(
                    FormatOps.ReplaceNewLines(FormatOps.NormalizeNewLines(
                        Command))));

            if (empty || !String.IsNullOrEmpty(Result))
                list.Add("Result", FormatOps.DisplayString(
                    FormatOps.ReplaceNewLines(FormatOps.NormalizeNewLines(
                        Result))));

            QueueList<string, string> queue = Queue;

            if (empty || ((queue != null) && (queue.Count > 0)))
                list.Add("Queue", (queue != null) ?
                    queue.Count.ToString() : FormatOps.DisplayNull);

            StringList callbackArguments = CallbackArguments;

            if (empty || ((callbackArguments != null) &&
                (callbackArguments.Count > 0)))
            {
                list.Add("CallbackArguments", (callbackArguments != null) ?
                    callbackArguments.ToString() : FormatOps.DisplayNull);
            }

            if (interpreter != null)
            {
                interpreter.GetHostDebuggerInfo(ref list, detailFlags);
            }
            else if (empty)
            {
                list.Add((IPair<string>)null);
                list.Add("Interpreter");
                list.Add((IPair<string>)null);
                list.Add("Id", FormatOps.DisplayNull);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void CheckCallbacks(
            Interpreter interpreter /* NOTE: Parent interpreter. */
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (Interpreter.IsDeletedOrDisposed(interpreter, false))
                    return;

                InterruptCallback oldInterruptCallback =
                    interpreter.InterruptCallback;

                StringList arguments = CallbackArguments;

                if (arguments != null) /* NOTE: Enabled? */
                {
                    if (oldInterruptCallback != null)
                        return;

                    if (interruptCallback == null)
                    {
                        interruptCallback = new InterruptCallback(
                            InterruptCallback);
                    }
                }
                else
                {
                    if (interruptCallback != null)
                        interruptCallback = null;

                    if (oldInterruptCallback == null)
                        return;
                }

                interpreter.InterruptCallback = interruptCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int EnterLoop()
        {
            CheckDisposed();

            return ++this.loops[(int)Context.Current];
        }

        ///////////////////////////////////////////////////////////////////////

        public int ExitLoop()
        {
            CheckDisposed();

            return --this.loops[(int)Context.Current];
        }

        ///////////////////////////////////////////////////////////////////////

        public int SetActive(
            bool active
            )
        {
            CheckDisposed();

            if (active)
                return ++this.active[(int)Context.Current];
            else if (this.active[(int)Context.Current] > 0)
                return --this.active[(int)Context.Current];
            else
                return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public long NextStep()
        {
            CheckDisposed();

            if (steps[(int)Context.Current] > 0)
                return --steps[(int)Context.Current];
            else
                return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MaybeNextStep()
        {
            CheckDisposed();

            long nextSteps = steps[(int)Context.Current];

            if (nextSteps == 0)
                return false;

            nextSteps--;
            steps[(int)Context.Current] = nextSteps;

            return (nextSteps == 0);
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER_BREAKPOINTS
        public ReturnCode GetBreakpointList(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref IStringList list,
            ref Result error
            )
        {
            CheckDisposed();

            BreakpointDictionary breakpoints = Breakpoints;

            if (breakpoints != null)
            {
                list = breakpoints.ToList(pattern, noCase);
                return ReturnCode.Ok;
            }
            else
            {
                error = "breakpoints not available";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode MatchBreakpoint(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match
            )
        {
            CheckDisposed();

            Result error = null;

            return MatchBreakpoint(
                interpreter, location, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode MatchBreakpoint(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match,
            ref Result error
            )
        {
            CheckDisposed();

            if (location != null)
            {
                if (ScriptLocation.Check(interpreter, location, false))
                {
                    string fileName = location.FileName;

                    if (fileName != null)
                    {
                        fileName = ScriptLocation.NormalizeFileName(
                            interpreter, fileName);
                    }

                    //
                    // NOTE: *WARNING: Empty file names are allowed here, please
                    //       do not change this to !String.IsNullOrEmpty.
                    //
                    if (fileName != null)
                    {
                        BreakpointDictionary breakpoints = Breakpoints;

                        if (breakpoints != null)
                        {
                            ScriptLocationIntDictionary scriptLocations;

                            if (breakpoints.TryGetValue(
                                    interpreter, fileName,
                                    out scriptLocations))
                            {
                                return scriptLocations.Match(
                                    interpreter, location, ref match,
                                    ref error);
                            }
                            else
                            {
                                //
                                // NOTE: It was not found.
                                //
                                match = false;
                                return ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            error = "breakpoints not available";
                        }
                    }
                    else
                    {
                        error = "invalid script location file name";
                    }
                }
                else
                {
                    error = "bad script location";
                }
            }
            else
            {
                error = "invalid script location";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ClearBreakpoint(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match,
            ref Result error
            )
        {
            CheckDisposed();

            if (location != null)
            {
                if (ScriptLocation.Check(interpreter, location, false))
                {
                    string fileName = location.FileName;

                    if (fileName != null)
                    {
                        fileName = ScriptLocation.NormalizeFileName(
                            interpreter, fileName);
                    }

                    //
                    // NOTE: *WARNING: Empty file names are allowed here, please
                    //       do not change this to !String.IsNullOrEmpty.
                    //
                    if (fileName != null)
                    {
                        BreakpointDictionary breakpoints = Breakpoints;

                        if (breakpoints != null)
                        {
                            ScriptLocationIntDictionary scriptLocations;

                            if (breakpoints.TryGetValue(
                                    interpreter, fileName,
                                    out scriptLocations))
                            {
                                return scriptLocations.Clear(
                                    interpreter, location, ref match,
                                    ref error);
                            }
                            else
                            {
                                //
                                // NOTE: It was not already found.
                                //
                                match = false;
                                return ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            error = "breakpoints not available";
                        }
                    }
                    else
                    {
                        error = "invalid script location file name";
                    }
                }
                else
                {
                    error = "bad script location";
                }
            }
            else
            {
                error = "invalid script location";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode SetBreakpoint(
            Interpreter interpreter,
            IScriptLocation location,
            ref bool match,
            ref Result error
            )
        {
            CheckDisposed();

            if (location != null)
            {
                if (ScriptLocation.Check(interpreter, location, false))
                {
                    string fileName = location.FileName;

                    if (fileName != null)
                    {
                        fileName = ScriptLocation.NormalizeFileName(
                            interpreter, fileName);
                    }

                    //
                    // NOTE: *WARNING: Empty file names are allowed here, please
                    //       do not change this to !String.IsNullOrEmpty.
                    //
                    if (fileName != null)
                    {
                        BreakpointDictionary breakpoints = Breakpoints;

                        if (breakpoints != null)
                        {
                            ScriptLocationIntDictionary scriptLocations;

                            if (breakpoints.TryGetValue(
                                    interpreter, fileName,
                                    out scriptLocations))
                            {
                                return scriptLocations.Set(
                                    interpreter, location, ref match,
                                    ref error);
                            }
                            else
                            {
                                breakpoints.Add(fileName,
                                    ScriptLocationIntDictionary.Create(
                                        interpreter, location));

                                //
                                // NOTE: It was not already found.
                                //
                                match = false;
                                return ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            error = "breakpoints not available";
                        }
                    }
                    else
                    {
                        error = "invalid script location file name";
                    }
                }
                else
                {
                    error = "bad script location";
                }
            }
            else
            {
                error = "invalid script location";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Initialize(
            ref Result error /* NOT USED */
            )
        {
            CheckDisposed();

            Initialize();
            ForceResume();

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Reset(
            ref Result error /* NOT USED */
            )
        {
            CheckDisposed();

            for (Context context = Context.First;
                    context <= Context.Last; context++)
            {
                Reset(context);
            }

            ForceResume();

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Suspend(
            ref Result error /* NOT USED */
            )
        {
            CheckDisposed();

            if (EnterSuspend() == 1)
            {
                Copy(Context.Current, Context.Saved);
                Reset(Context.Current);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Resume(
            ref Result error /* NOT USED */
            )
        {
            CheckDisposed();

            if (ExitSuspend() == 0)
            {
                Copy(Context.Saved, Context.Current);
                Reset(Context.Saved);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode DumpCommands(
            ref Result result
            )
        {
            CheckDisposed();

            QueueList<string, string> queue = GetQueue();

            if (queue == null)
            {
                result = "debugger command queue not available";
                return ReturnCode.Error;
            }

            result = GenericOps<string>.EnumerableToString(
                queue.Values, ToStringFlags.None, Characters.Space.ToString(),
                null, false);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ClearCommands(
            ref Result error
            )
        {
            CheckDisposed();

            QueueList<string, string> queue = GetQueue();

            if (queue == null)
            {
                error = "debugger command queue not available";
                return ReturnCode.Error;
            }

            queue.Clear();
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode EnqueueCommand(
            string text,
            ref Result error
            )
        {
            CheckDisposed();

            QueueList<string, string> queue = GetQueue();

            if (queue == null)
            {
                error = "debugger command queue not available";
                return ReturnCode.Error;
            }

            queue.Add(GetQueueKey(), text);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Watchpoint(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
            CheckDisposed();

            return DebuggerOps.Watchpoint(
                this, interpreter, loopData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Breakpoint(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            ref Result result
            )
        {
            CheckDisposed();

            return DebuggerOps.Breakpoint(
                this, interpreter, loopData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Debugger));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Dispose of the isolated debugger interpreter
                    //       here, if applicable.  The parent interpreter
                    //       (i.e. the one being debugged) is *NOT* owned
                    //       by us and should never be disposed via this
                    //       method.
                    //
                    if (interpreter != null)
                    {
                        interpreter.Dispose();
                        interpreter = null;
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Debugger()
        {
            Dispose(false);
        }
        #endregion
    }
}
