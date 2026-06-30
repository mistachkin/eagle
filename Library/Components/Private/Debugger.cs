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
    /// <summary>
    /// This class provides the script debugging facilities for an Eagle
    /// interpreter -- it tracks the debugger state (such as whether debugging
    /// is enabled, the active breakpoint types, the single-step and break-on
    /// conditions, and the pending command queue), optionally hosts an isolated
    /// out-of-band interpreter used to evaluate debugger commands, and manages
    /// breakpoints and interrupt callbacks for the interpreter being debugged.
    /// Most of its state is held in paired slots so that the current values can
    /// be saved and restored across suspend and resume operations.  It
    /// implements <see cref="IDebugger" /> and is disposable; disposing it
    /// releases the isolated debugger interpreter, if any.
    /// </summary>
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
        /// <summary>
        /// The default value used to indicate whether debugging is enabled for
        /// a newly created or freshly initialized debugger.
        /// </summary>
        private static bool DefaultEnabled = true;
        /// <summary>
        /// The default set of breakpoint types enabled for a newly created or
        /// freshly initialized debugger.
        /// </summary>
        private static BreakpointType DefaultTypes = BreakpointType.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Enumerations
        /// <summary>
        /// This enumeration identifies which slot of a paired debugger property
        /// value is being accessed -- the current value or the value saved
        /// across a suspend operation.
        /// </summary>
        [ObjectId("b53c003a-bb94-4c10-900c-7403dc36c2a8")]
        private enum Context
        {
            /// <summary>
            /// The context for the current value of a property.
            /// </summary>
            Current = 0,     // context for current value of property.
            /// <summary>
            /// The context for the saved value of a property.
            /// </summary>
            Saved = 1,       // context for saved value of property.
            /// <summary>
            /// The first context slot for a property value.
            /// </summary>
            First = Current, // first context slot for property value.
            /// <summary>
            /// The last context slot for a property value.
            /// </summary>
            Last = Saved     // last context slot for property value.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The interrupt callback delegate currently installed on the
        /// interpreter being debugged, or null if no callback is installed.
        /// </summary>
        private InterruptCallback interruptCallback;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a debugger, optionally creating an isolated out-of-band
        /// interpreter to evaluate debugger commands, and then initializes the
        /// debugger state.
        /// </summary>
        /// <param name="isolated">
        /// Non-zero to create an isolated debugger interpreter using the
        /// remaining parameters; zero to omit the debugger interpreter.
        /// </param>
        /// <param name="culture">
        /// The culture to use for the isolated debugger interpreter.  This
        /// parameter may be null.
        /// </param>
        /// <param name="createFlags">
        /// The flags used to create the isolated debugger interpreter.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags used to create the host for the isolated debugger
        /// interpreter.
        /// </param>
        /// <param name="initializeFlags">
        /// The flags used to initialize the isolated debugger interpreter.
        /// </param>
        /// <param name="scriptFlags">
        /// The flags used when evaluating the initialization scripts for the
        /// isolated debugger interpreter.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags for the isolated debugger interpreter.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags for the isolated debugger interpreter.
        /// </param>
        /// <param name="appDomain">
        /// The application domain in which to create the isolated debugger
        /// interpreter.  This parameter may be null.
        /// </param>
        /// <param name="host">
        /// The host to use for the isolated debugger interpreter.  This
        /// parameter may be null.
        /// </param>
        /// <param name="libraryPath">
        /// The script library path for the isolated debugger interpreter.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of automatic package search paths for the isolated debugger
        /// interpreter.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method is invoked by the interpreter being debugged when an
        /// interrupt occurs; it dispatches the configured callback arguments
        /// as a command in the debugger interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The parent interpreter (i.e. the one being debugged) that raised the
        /// interrupt.
        /// </param>
        /// <param name="interruptType">
        /// The type of interrupt that occurred.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the interrupt, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method returns the default set of breakpoint types, honoring
        /// the supplied enabled state.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero (or null) to return the default breakpoint types; zero to
        /// return no breakpoint types.
        /// </param>
        /// <param name="tokens">
        /// Non-zero to include token breakpoints in the returned set of
        /// breakpoint types.
        /// </param>
        /// <returns>
        /// The default set of breakpoint types, or
        /// <see cref="BreakpointType.None" /> when <paramref name="enabled" />
        /// is zero.
        /// </returns>
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

        /// <summary>
        /// This method returns the default set of breakpoint types.
        /// </summary>
        /// <param name="tokens">
        /// Non-zero to include token breakpoints in the returned set of
        /// breakpoint types.
        /// </param>
        /// <returns>
        /// The default set of breakpoint types.
        /// </returns>
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
        /// <summary>
        /// This method allocates and initializes the paired state arrays that
        /// back the debugger properties, resetting them to their default
        /// values.
        /// </summary>
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

        /// <summary>
        /// This method resets the debugger state stored in the specified
        /// context slot to its default values.
        /// </summary>
        /// <param name="target">
        /// The context slot whose stored state is to be reset.
        /// </param>
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

        /// <summary>
        /// This method copies the debugger state stored in one context slot
        /// into another context slot.
        /// </summary>
        /// <param name="source">
        /// The context slot from which the stored state is to be copied.
        /// </param>
        /// <param name="target">
        /// The context slot into which the stored state is to be copied.
        /// </param>
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

        /// <summary>
        /// This method forcibly resumes the debugger by resetting its suspend
        /// count to zero.
        /// </summary>
        private void ForceResume()
        {
            suspendCount = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments and returns the debugger suspend count.
        /// </summary>
        /// <returns>
        /// The new suspend count after incrementing.
        /// </returns>
        private int EnterSuspend()
        {
            return ++suspendCount;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements and returns the debugger suspend count.
        /// </summary>
        /// <returns>
        /// The new suspend count after decrementing.
        /// </returns>
        private int ExitSuspend()
        {
            return --suspendCount;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the debugger command queue, creating and storing
        /// a new empty queue if one does not already exist.
        /// </summary>
        /// <returns>
        /// The debugger command queue.
        /// </returns>
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

        /// <summary>
        /// This method returns a unique key suitable for adding an entry to the
        /// debugger command queue.
        /// </summary>
        /// <returns>
        /// A unique queue key.
        /// </returns>
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
        /// <summary>
        /// Gets a value indicating whether this debugger has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                //
                // NOTE: Obviously, this would be pointless.
                //
                // CheckDisposed(); /* EXEMPT */

                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this debugger is currently in the
        /// process of being disposed; this property always returns zero for
        /// this debugger.
        /// </summary>
        public bool Disposing
        {
            get
            {
                //
                // NOTE: Obviously, this would also be pointless.
                //
                // CheckDisposed(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The isolated, out-of-band interpreter used to evaluate debugger
        /// commands, or null if no debugger interpreter is in use.
        /// </summary>
        private Interpreter interpreter; /* out-of-band debug interpreter */
        /// <summary>
        /// Gets or sets the isolated, out-of-band interpreter used to evaluate
        /// debugger commands.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebuggerData Members
        /// <summary>
        /// The number of times the debugger has been suspended without a
        /// corresponding resume; zero indicates the debugger is not suspended.
        /// </summary>
        private int suspendCount;
        /// <summary>
        /// Gets or sets the number of times the debugger has been suspended
        /// without a corresponding resume.
        /// </summary>
        public int SuspendCount
        {
            get { CheckDisposed(); return suspendCount; }
            set { CheckDisposed(); suspendCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The paired state slots indicating whether debugging is enabled.
        /// </summary>
        private bool[] enabled;
        /// <summary>
        /// Gets or sets a value indicating whether debugging is enabled.
        /// </summary>
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
        /// <summary>
        /// The paired state slots tracking how many interactive loops are in
        /// use by the debugger.
        /// </summary>
        private int[] loops;
        /// <summary>
        /// Gets or sets the number of interactive loops in use by the debugger.
        /// </summary>
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
        /// <summary>
        /// The paired state slots used by scripts to detect that they are being
        /// executed while in the interactive debugger.
        /// </summary>
        private int[] active;
        /// <summary>
        /// Gets or sets the value used by scripts to detect that they are being
        /// executed while in the interactive debugger.
        /// </summary>
        public int Active
        {
            get { CheckDisposed(); return active[(int)Context.Current]; }
            set { CheckDisposed(); active[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The paired state slots indicating whether single-step mode is
        /// enabled.
        /// </summary>
        private bool[] singleStep;
        /// <summary>
        /// Gets or sets a value indicating whether single-step mode is enabled.
        /// </summary>
        public bool SingleStep
        {
            get { CheckDisposed(); return singleStep[(int)Context.Current]; }
            set { CheckDisposed(); singleStep[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER_BREAKPOINTS
        /// <summary>
        /// The paired state slots indicating whether execution should break on
        /// each token.
        /// </summary>
        private bool[] breakOnToken;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break on
        /// each token.
        /// </summary>
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

        /// <summary>
        /// The paired state slots indicating whether execution should break
        /// before executing a command.
        /// </summary>
        private bool[] breakOnExecute;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break
        /// before executing a command.
        /// </summary>
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

        /// <summary>
        /// The paired state slots indicating whether execution should break on
        /// script cancellation.
        /// </summary>
        private bool[] breakOnCancel;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break on
        /// script cancellation.
        /// </summary>
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

        /// <summary>
        /// The paired state slots indicating whether execution should break on
        /// an error.
        /// </summary>
        private bool[] breakOnError;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break on an
        /// error.
        /// </summary>
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

        /// <summary>
        /// The paired state slots indicating whether execution should break on
        /// a return.
        /// </summary>
        private bool[] breakOnReturn;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break on a
        /// return.
        /// </summary>
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

        /// <summary>
        /// The paired state slots indicating whether execution should break on
        /// a test.
        /// </summary>
        private bool[] breakOnTest;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break on a
        /// test.
        /// </summary>
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

        /// <summary>
        /// The paired state slots indicating whether execution should break on
        /// an exit.
        /// </summary>
        private bool[] breakOnExit;
        /// <summary>
        /// Gets or sets a value indicating whether execution should break on an
        /// exit.
        /// </summary>
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

        /// <summary>
        /// The paired state slots holding the number of execution steps
        /// remaining before the debugger breaks.
        /// </summary>
        private long[] steps;
        /// <summary>
        /// Gets or sets the number of execution steps remaining before the
        /// debugger breaks.
        /// </summary>
        public long Steps
        {
            get { CheckDisposed(); return steps[(int)Context.Current]; }
            set { CheckDisposed(); steps[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The paired state slots holding the set of breakpoint types that are
        /// currently active.
        /// </summary>
        private BreakpointType[] types;
        /// <summary>
        /// Gets or sets the set of breakpoint types that are currently active.
        /// </summary>
        public BreakpointType Types
        {
            get { CheckDisposed(); return types[(int)Context.Current]; }
            set { CheckDisposed(); types[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER_BREAKPOINTS
        /// <summary>
        /// The paired state slots holding the collection of configured
        /// breakpoints.
        /// </summary>
        private BreakpointDictionary[] breakpoints;
        /// <summary>
        /// Gets or sets the collection of configured breakpoints.
        /// </summary>
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
        /// <summary>
        /// The paired state slots holding the argument list passed to the
        /// debugger when execution breaks.
        /// </summary>
        private ArgumentList[] executeArguments;
        /// <summary>
        /// Gets or sets the argument list passed to the debugger when execution
        /// breaks.
        /// </summary>
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

        /// <summary>
        /// The paired state slots holding the most recent debugger command
        /// text.
        /// </summary>
        private string[] command;
        /// <summary>
        /// Gets or sets the most recent debugger command text.
        /// </summary>
        public string Command
        {
            get { CheckDisposed(); return command[(int)Context.Current]; }
            set { CheckDisposed(); command[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The paired state slots holding the most recent debugger command
        /// result.
        /// </summary>
        private Result[] result;
        /// <summary>
        /// Gets or sets the most recent debugger command result.
        /// </summary>
        public Result Result
        {
            get { CheckDisposed(); return result[(int)Context.Current]; }
            set { CheckDisposed(); result[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The paired state slots holding the queue of pending debugger
        /// commands.
        /// </summary>
        private QueueList<string, string>[] queue;
        /// <summary>
        /// Gets or sets the queue of pending debugger commands.
        /// </summary>
        public QueueList<string, string> Queue
        {
            get { CheckDisposed(); return queue[(int)Context.Current]; }
            set { CheckDisposed(); queue[(int)Context.Current] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The paired state slots holding the argument list used when firing
        /// the debugger interrupt callback; a null value disables the callback.
        /// </summary>
        private StringList[] callbackArguments;
        /// <summary>
        /// Gets or sets the argument list used when firing the debugger
        /// interrupt callback.
        /// </summary>
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
        /// <summary>
        /// This method appends name/value pairs describing the current debugger
        /// state to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the descriptive name/value pairs are added.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling how much detail is included in the added
        /// information.
        /// </param>
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

            if (empty || ((queue != null) && !queue.IsEmpty))
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

        /// <summary>
        /// This method installs or removes the debugger interrupt callback on
        /// the interpreter being debugged, based on whether callback arguments
        /// are currently configured.
        /// </summary>
        /// <param name="interpreter">
        /// The parent interpreter (i.e. the one being debugged) on which the
        /// interrupt callback is to be installed or removed.  This parameter may
        /// be null.
        /// </param>
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

        /// <summary>
        /// This method increments and returns the count of interactive loops in
        /// use by the debugger.
        /// </summary>
        /// <returns>
        /// The new interactive loop count after incrementing.
        /// </returns>
        public int EnterLoop()
        {
            CheckDisposed();

            return ++this.loops[(int)Context.Current];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements and returns the count of interactive loops in
        /// use by the debugger.
        /// </summary>
        /// <returns>
        /// The new interactive loop count after decrementing.
        /// </returns>
        public int ExitLoop()
        {
            CheckDisposed();

            return --this.loops[(int)Context.Current];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments or decrements the count used by scripts to
        /// detect that they are being executed while in the interactive
        /// debugger.
        /// </summary>
        /// <param name="active">
        /// Non-zero to increment the active count; zero to decrement it (but not
        /// below zero).
        /// </param>
        /// <returns>
        /// The new active count.
        /// </returns>
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

        /// <summary>
        /// This method decrements the number of execution steps remaining
        /// before the debugger breaks, without dropping below zero.
        /// </summary>
        /// <returns>
        /// The number of execution steps remaining after decrementing.
        /// </returns>
        public long NextStep()
        {
            CheckDisposed();

            if (steps[(int)Context.Current] > 0)
                return --steps[(int)Context.Current];
            else
                return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the number of execution steps remaining
        /// before the debugger breaks and indicates whether the count has just
        /// reached zero.
        /// </summary>
        /// <returns>
        /// True if the step count was non-zero and has now reached zero;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method retrieves the list of configured breakpoints, optionally
        /// filtered by a pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the breakpoints are being listed.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter the breakpoints.  This parameter
        /// may be null to list all breakpoints.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="list">
        /// Upon success, this contains the list of matching breakpoints.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a breakpoint is configured at the
        /// specified script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the breakpoint is being matched.
        /// </param>
        /// <param name="location">
        /// The script location to test against the configured breakpoints.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint is configured at the
        /// specified location; otherwise, zero.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method determines whether a breakpoint is configured at the
        /// specified script location, returning an error message on failure.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the breakpoint is being matched.
        /// </param>
        /// <param name="location">
        /// The script location to test against the configured breakpoints.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint is configured at the
        /// specified location; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method clears any breakpoint configured at the specified script
        /// location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the breakpoint is being cleared.
        /// </param>
        /// <param name="location">
        /// The script location whose breakpoint is to be cleared.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint was present at the
        /// specified location and has been cleared; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method sets a breakpoint at the specified script location.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the breakpoint is being set.
        /// </param>
        /// <param name="location">
        /// The script location at which the breakpoint is to be set.
        /// </param>
        /// <param name="match">
        /// Upon success, this is non-zero if a breakpoint was already present at
        /// the specified location; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method initializes the debugger state and forcibly resumes the
        /// debugger.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.  This
        /// parameter is not currently used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method resets the debugger state in every context slot and
        /// forcibly resumes the debugger.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.  This
        /// parameter is not currently used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method suspends the debugger, saving the current state and
        /// resetting it the first time the suspend count reaches one.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.  This
        /// parameter is not currently used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method resumes the debugger, restoring the previously saved
        /// state when the suspend count reaches zero.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.  This
        /// parameter is not currently used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the contents of the debugger command queue.
        /// </summary>
        /// <param name="result">
        /// Upon success, this contains the list of queued debugger commands;
        /// upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

            result = new StringList(queue.Values);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all commands from the debugger command queue.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method adds a single command to the debugger command queue.
        /// </summary>
        /// <param name="text">
        /// The command text to add to the queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method adds the contents of a multi-line buffer to the debugger
        /// command queue, one queue entry per line.
        /// </summary>
        /// <param name="text">
        /// The buffer text to split into lines and add to the queue.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public ReturnCode EnqueueBuffer(
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

            string[] lines = text.Split(Characters.NewLine);

            if (lines == null)
            {
                error = "could not split text into lines";
                return ReturnCode.Error;
            }

            foreach (string line in lines)
                queue.Add(GetQueueKey(), line);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enters the debugger as a result of a watchpoint being
        /// triggered, running the interactive debugger loop.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the watchpoint was triggered.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop to run.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the interactive loop; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method enters the debugger as a result of a breakpoint being
        /// hit, running the interactive debugger loop.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which the breakpoint was hit.
        /// </param>
        /// <param name="loopData">
        /// The data describing the interactive loop to run.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the interactive loop; upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// Stores a value indicating whether this debugger has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this debugger has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this debugger has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Debugger));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this debugger.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this debugger and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this debugger, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Debugger()
        {
            Dispose(false);
        }
        #endregion
    }
}
