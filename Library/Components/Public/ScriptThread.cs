/*
 * ScriptThread.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents a dedicated managed thread that hosts an Eagle
    /// interpreter, allowing scripts to be created, evaluated, and managed on a
    /// thread separate from the one that created it.  It can either create and
    /// own a new interpreter running on its thread or attach to an existing
    /// one, and it provides methods to queue work asynchronously, send scripts
    /// synchronously, signal and wake the thread, cancel running scripts, and
    /// wait for various thread and event conditions.  It implements
    /// <see cref="IScriptThread" /> and is disposable; disposing it attempts a
    /// graceful shutdown of the thread and its interpreter.
    /// </summary>
    [ObjectId("f3bd8b05-282c-4ec8-8c46-c02790fbbb7d")]
    // [ObjectFlags(ObjectFlags.AutoDispose)]
    public sealed class ScriptThread :
            IMaybeDisposed, IScriptThread, IDisposable
    {
        #region Event Input Pair Class (Input-Only)
        /// <summary>
        /// This class represents an immutable, input-only pair of strings used
        /// to convey a script and its associated event name to an event
        /// callback.
        /// </summary>
        [ObjectId("72a69e6c-515a-4567-9d07-874e05b2cf6b")]
        private sealed class EventInputPair :
            AnyPair<string, string>
        {
            /// <summary>
            /// Constructs an event input pair from the specified string values.
            /// </summary>
            /// <param name="x">
            /// The first string value of the pair.
            /// </param>
            /// <param name="y">
            /// The second string value of the pair.
            /// </param>
            public EventInputPair(
                string x,
                string y
                )
                : base(x, y)
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Output Pair Class (Input-Output)
        /// <summary>
        /// This class represents a mutable, input-output pair used to convey
        /// the return code and result produced by evaluating a script back to a
        /// waiting caller.
        /// </summary>
        [ObjectId("25757691-b599-40c1-a485-90b1a3ab87a7")]
        private sealed class EventOutputPair :
            MutableAnyPair<ReturnCode, Result>
        {
            /// <summary>
            /// Constructs an event output pair, optionally allowing its values
            /// to be modified after construction.
            /// </summary>
            /// <param name="mutable">
            /// Non-zero if the values of this pair may be modified after
            /// construction; otherwise, zero.
            /// </param>
            public EventOutputPair(
                bool mutable
                )
                : base(mutable)
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        /// <summary>
        /// The default name used when adding this script thread object into its
        /// interpreter.
        /// </summary>
        private const string scriptThreadObjectName = "thread";

        /// <summary>
        /// The prefix used when constructing the event name for a synchronous
        /// send operation.
        /// </summary>
        private const string scriptThreadSendEventPrefix = "threadSend";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default object option type used when adding CLR objects into the
        /// interpreter.
        /// </summary>
        private static ObjectOptionType DefaultObjectOptionType =
            ObjectOps.GetDefaultObjectOptionType();

        /// <summary>
        /// The default object flags used when adding CLR objects into the
        /// interpreter.
        /// </summary>
        private static ObjectFlags DefaultObjectFlags =
            ObjectOps.GetDefaultObjectFlags();

        /// <summary>
        /// The default thread flags used when creating or attaching a script
        /// thread.
        /// </summary>
        private static ThreadFlags DefaultThreadFlags =
            ThreadFlags.Default;

        /// <summary>
        /// The default interpreter creation flags used when creating a new
        /// interpreter for the script thread.
        /// </summary>
        private static CreateFlags DefaultCreateFlags =
            CreateFlags.ScriptThreadUse;

        /// <summary>
        /// The default host creation flags used when creating a new interpreter
        /// for the script thread.
        /// </summary>
        private static HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.ScriptThreadUse;

        /// <summary>
        /// The default initialization flags used when creating a new
        /// interpreter for the script thread.
        /// </summary>
        private static InitializeFlags DefaultInitializeFlags =
            Defaults.InitializeFlags;

        /// <summary>
        /// The default script flags used when creating a new interpreter for
        /// the script thread.
        /// </summary>
        private static ScriptFlags DefaultScriptFlags =
            Defaults.ScriptFlags;

        /// <summary>
        /// The default interpreter flags used when creating a new interpreter
        /// for the script thread.
        /// </summary>
        private static InterpreterFlags DefaultInterpreterFlags =
            Defaults.InterpreterFlags;

        /// <summary>
        /// The default variable flags used when waiting on the event variable.
        /// </summary>
        private static VariableFlags DefaultEventVariableFlags =
            VariableFlags.None;

        /// <summary>
        /// The default event wait flags used when waiting on the event
        /// variable.
        /// </summary>
        private static EventWaitFlags DefaultEventWaitFlags =
            EventWaitFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default maximum stack size, in bytes, used when creating the
        /// physical thread; zero means the process default is used.
        /// </summary>
        private static int DefaultStackSize = 0;

        /// <summary>
        /// The default timeout, in milliseconds, used during thread creation
        /// and startup; zero means the default join timeout is used.
        /// </summary>
        private static int DefaultTimeout = 0;

        /// <summary>
        /// The default value indicating whether the engine (instead of an event
        /// callback) should be used to evaluate a synchronously sent script.
        /// </summary>
        private static bool DefaultUseEngine = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// The number of script thread instances that are currently running
        /// their thread-start method.
        /// </summary>
        private static int activeCount = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the instance data of this
        /// script thread.
        /// </summary>
        private object syncRoot = new object();

        /// <summary>
        /// The event signaled when the script thread has finished starting up.
        /// </summary>
        private EventWaitHandle startEvent;

        /// <summary>
        /// The event used to wake up the script thread while it is waiting on
        /// its event variable.
        /// </summary>
        private EventWaitHandle wakeUpEvent;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a script thread, assigning it the next available script
        /// thread identifier.
        /// </summary>
        private ScriptThread()
        {
            id = GlobalState.NextScriptThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a script thread from the fully specified set of
        /// interpreter, thread, and behavior parameters.  This is the most
        /// general constructor; it is used internally by the static factory
        /// methods.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach to, or null if a new interpreter
        /// should be created on the script thread.
        /// </param>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used when creating a new interpreter.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags used when creating a new interpreter.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used when creating a new interpreter.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags used when creating a new interpreter.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used when creating a new interpreter.
        /// </param>
        /// <param name="eventVariableFlags">
        /// The variable flags used when waiting on the event variable.
        /// </param>
        /// <param name="eventWaitFlags">
        /// The event wait flags used when waiting on the event variable.
        /// </param>
        /// <param name="args">
        /// The arguments used when creating a new interpreter.  This parameter
        /// may be null.
        /// </param>
        /// <param name="host">
        /// The host used when creating a new interpreter.  This parameter may be
        /// null.
        /// </param>
        /// <param name="script">
        /// The startup script to evaluate on the script thread, or null for no
        /// startup script.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// shutdown.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the thread should be configured for user-interface use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the thread should be a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the created interpreter should be pushed onto the active
        /// interpreter stack while the script thread runs.
        /// </param>
        /// <param name="quiet">
        /// Non-zero if the interpreter should suppress certain error reporting.
        /// </param>
        /// <param name="noBackgroundError">
        /// Non-zero to disable background error processing for the interpreter.
        /// </param>
        /// <param name="useSelf">
        /// Non-zero if this script thread object should be added into its own
        /// interpreter.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress complaints (i.e. emit a trace instead) when an
        /// error is encountered.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic tracing.
        /// </param>
        /// <param name="debug">
        /// Non-zero to enable debug behavior.
        /// </param>
        /// <param name="usePool">
        /// Non-zero to run the script thread using the thread pool instead of a
        /// dedicated physical thread.
        /// </param>
        /// <param name="purgeGlobal">
        /// Non-zero to also purge global context information when the thread
        /// exits.
        /// </param>
        /// <param name="noAbort">
        /// Non-zero to prevent the thread from ever being forcibly aborted.
        /// </param>
        private ScriptThread(
            Interpreter interpreter,
            string name,
            ThreadFlags threadFlags,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            VariableFlags eventVariableFlags,
            EventWaitFlags eventWaitFlags,
            IEnumerable<string> args,
            IHost host,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            bool quiet,
            bool noBackgroundError,
            bool useSelf,
            bool noComplain,
            bool verbose,
            bool debug,
            bool usePool,
            bool purgeGlobal,
            bool noAbort
            )
            : this()
        {
            this.interpreter = interpreter;
            this.name = name;
            this.threadFlags = threadFlags;
            this.createFlags = createFlags;
            this.hostCreateFlags = hostCreateFlags;
            this.initializeFlags = initializeFlags;
            this.scriptFlags = scriptFlags;
            this.interpreterFlags = interpreterFlags;
            this.args = args;
            this.host = host;
            this.script = script;
            this.varName = varName;
            this.maxStackSize = maxStackSize;
            this.timeout = timeout;
            this.userInterface = userInterface;
            this.isBackground = isBackground;
            this.useActiveStack = useActiveStack;
            this.quiet = quiet;
            this.noBackgroundError = noBackgroundError;
            this.useSelf = useSelf;
            this.eventVariableFlags = eventVariableFlags;
            this.eventWaitFlags = eventWaitFlags;
            this.noComplain = noComplain;
            this.verbose = verbose;
            this.debug = debug;
            this.usePool = usePool;
            this.purgeGlobal = purgeGlobal;
            this.noAbort = noAbort;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a script thread that attaches to the specified,
        /// existing interpreter, using the default stack size.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach the script thread to.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Attach(
            Interpreter interpreter,
            string varName,
            ref Result error
            )
        {
            return Attach(
                interpreter, varName, DefaultStackSize, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script thread that attaches to the specified,
        /// existing interpreter, using the specified stack size.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach the script thread to.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Attach(
            Interpreter interpreter,
            string varName,
            int maxStackSize,
            ref Result error
            )
        {
            return Attach(
                interpreter, varName, maxStackSize, DefaultTimeout,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script thread that attaches to the specified,
        /// existing interpreter, using the specified stack size and timeout.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach the script thread to.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Attach(
            Interpreter interpreter,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Attach(
                interpreter, null, null, varName, maxStackSize, timeout,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that attaches to the
        /// specified, existing interpreter, using the specified thread flags and
        /// stack size.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach the script thread to.
        /// </param>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Attach(
            Interpreter interpreter,
            string name,
            ThreadFlags? threadFlags,
            string varName,
            int maxStackSize,
            ref Result error
            )
        {
            return Attach(
                interpreter, name, threadFlags, varName, maxStackSize,
                DefaultTimeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that attaches to the
        /// specified, existing interpreter, using the specified thread flags,
        /// stack size, and timeout.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach the script thread to.
        /// </param>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Attach(
            Interpreter interpreter,
            string name,
            ThreadFlags? threadFlags,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Attach(
                interpreter, name, threadFlags, null, varName,
                maxStackSize, timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that attaches to the
        /// specified, existing interpreter, optionally evaluating a startup
        /// script, using the specified thread flags, stack size, and timeout.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach the script thread to.
        /// </param>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="script">
        /// The startup script to evaluate on the script thread, or null for no
        /// startup script.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Attach(
            Interpreter interpreter,
            string name,
            ThreadFlags? threadFlags,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                interpreter, name, threadFlags, null, null, null, null,
                null, null, null, script, varName, maxStackSize, timeout,
                true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script thread that owns a newly created
        /// interpreter, using the default stack size for the process.
        /// </summary>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            string varName,
            ref Result error
            )
        {
            //
            // NOTE: Create a ScriptThread object using the default stack size
            //       for the process.
            //
            return Create(varName, DefaultStackSize, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script thread that owns a newly created
        /// interpreter, using the specified stack size.
        /// </summary>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            string varName,
            int maxStackSize,
            ref Result error
            )
        {
            return Create(
                null, null, null, null, null, null, null, varName,
                maxStackSize, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a script thread that owns a newly created
        /// interpreter, using the specified stack size and timeout.
        /// </summary>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                null, null, null, null, null, null, null, varName,
                maxStackSize, timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that owns a newly created
        /// interpreter, using the specified creation flags and stack size.
        /// </summary>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used when creating a new interpreter,
        /// or null to use the default flags.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used when creating a new interpreter, or
        /// null to use the default flags.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags used when creating a new interpreter, or null to use
        /// the default flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            HostCreateFlags? hostCreateFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            string varName,
            int maxStackSize,
            ref Result error
            )
        {
            return Create(
                name, threadFlags, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags,
                varName, maxStackSize, DefaultTimeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that owns a newly created
        /// interpreter, using the specified creation flags, stack size, and
        /// timeout.
        /// </summary>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used when creating a new interpreter,
        /// or null to use the default flags.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used when creating a new interpreter, or
        /// null to use the default flags.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags used when creating a new interpreter, or null to use
        /// the default flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            HostCreateFlags? hostCreateFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                name, threadFlags, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags,
                null, null, null, varName, maxStackSize, timeout,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that owns a newly created
        /// interpreter, using the specified creation flags, arguments, host,
        /// startup script, stack size, and timeout.
        /// </summary>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used when creating a new interpreter,
        /// or null to use the default flags.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used when creating a new interpreter, or
        /// null to use the default flags.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags used when creating a new interpreter, or null to use
        /// the default flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="args">
        /// The arguments used when creating a new interpreter.  This parameter
        /// may be null.
        /// </param>
        /// <param name="host">
        /// The host used when creating a new interpreter.  This parameter may be
        /// null.
        /// </param>
        /// <param name="script">
        /// The startup script to evaluate on the script thread, or null for no
        /// startup script.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            HostCreateFlags? hostCreateFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            IEnumerable<string> args,
            IHost host,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                null, name, threadFlags, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags, args,
                host, script, varName, maxStackSize, timeout,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a named script thread that either attaches to
        /// the specified, existing interpreter or, if none is supplied, owns a
        /// newly created one, using the specified creation flags, arguments,
        /// host, startup script, stack size, and timeout.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach to, or null if a new interpreter
        /// should be created on the script thread.
        /// </param>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used when creating a new interpreter,
        /// or null to use the default flags.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used when creating a new interpreter, or
        /// null to use the default flags.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags used when creating a new interpreter, or null to use
        /// the default flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="args">
        /// The arguments used when creating a new interpreter.  This parameter
        /// may be null.
        /// </param>
        /// <param name="host">
        /// The host used when creating a new interpreter.  This parameter may be
        /// null.
        /// </param>
        /// <param name="script">
        /// The startup script to evaluate on the script thread, or null for no
        /// startup script.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        public static IScriptThread Create(
            Interpreter interpreter,
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            HostCreateFlags? hostCreateFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            IEnumerable<string> args,
            IHost host,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                interpreter, name, threadFlags, createFlags, hostCreateFlags,
                initializeFlags, scriptFlags, interpreterFlags, args, host,
                script, varName, maxStackSize, timeout, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method implements the core logic for creating a script thread,
        /// either creating a new interpreter or attaching to an existing one,
        /// optionally starting the thread (or queuing it to the thread pool) and
        /// waiting for it to start.  All of the other static factory methods
        /// ultimately delegate to this method.
        /// </summary>
        /// <param name="interpreter">
        /// The existing interpreter to attach to, or null if a new interpreter
        /// should be created on the script thread.
        /// </param>
        /// <param name="name">
        /// The name of the script thread.  This parameter may be null.
        /// </param>
        /// <param name="threadFlags">
        /// The flags controlling the behavior of the script thread, or null to
        /// use the default flags.
        /// </param>
        /// <param name="createFlags">
        /// The interpreter creation flags used when creating a new interpreter,
        /// or null to use the default flags.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The host creation flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used when creating a new interpreter, or
        /// null to use the default flags.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags used when creating a new interpreter, or null to use
        /// the default flags.
        /// </param>
        /// <param name="interpreterFlags">
        /// The interpreter flags used when creating a new interpreter, or null
        /// to use the default flags.
        /// </param>
        /// <param name="args">
        /// The arguments used when creating a new interpreter.  This parameter
        /// may be null.
        /// </param>
        /// <param name="host">
        /// The host used when creating a new interpreter.  This parameter may be
        /// null.
        /// </param>
        /// <param name="script">
        /// The startup script to evaluate on the script thread, or null for no
        /// startup script.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on, or null for no wait.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, used during thread creation and
        /// startup.
        /// </param>
        /// <param name="viaAttach">
        /// Non-zero if the script thread is being created in order to attach to
        /// an existing interpreter rather than create a new one.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created script thread, or null if it could not be created.
        /// </returns>
        private static IScriptThread Create(
            Interpreter interpreter,
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            HostCreateFlags? hostCreateFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            IEnumerable<string> args,
            IHost host,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            bool viaAttach,
            ref Result error
            )
        {
            try
            {
                //
                // NOTE: *WARNING* If the "usePool" option is specified, the
                //       "maxStackSize", "userInterface", "isBackground", and
                //       "start" parameters are effectively ignored.
                //
                bool throwOnDisposed;
                bool safe;
                bool noHidden;
                bool userInterface;
                bool isBackground;
                bool useActiveStack;
                bool quiet;
                bool noBackgroundError;
                bool useSelf;
                bool noComplain;
                bool verbose;
                bool debug;
                bool usePool;
                bool purgeGlobal;
                bool start;
                bool noAbort;
                bool attach;

                ThreadFlags localThreadFlags = GetThreadFlags(
                    threadFlags, viaAttach, out throwOnDisposed,
                    out safe, out noHidden, out userInterface,
                    out isBackground, out useActiveStack, out quiet,
                    out noBackgroundError, out useSelf, out noComplain,
                    out verbose, out debug, out usePool, out purgeGlobal,
                    out start, out noAbort, out attach);

                CreateFlags localCreateFlags = GetCreateFlags(
                    createFlags, throwOnDisposed, safe, noHidden);

                HostCreateFlags localHostCreateFlags = GetHostCreateFlags(
                    hostCreateFlags);

                InitializeFlags localInitializeFlags = GetInitializeFlags(
                    initializeFlags);

                ScriptFlags localScriptFlags = GetScriptFlags(scriptFlags);

                InterpreterFlags localInterpreterFlags = GetInterpreterFlags(
                    interpreterFlags);

                VariableFlags localEventVariableFlags = GetEventVariableFlags(
                    null, localThreadFlags);

                EventWaitFlags localEventWaitFlags = GetEventWaitFlags(
                    null, localThreadFlags);

                if (timeout == DefaultTimeout)
                    timeout = ThreadOps.DefaultJoinTimeout;

                ScriptThread scriptThread = new ScriptThread(
                    interpreter, name, localThreadFlags,
                    localCreateFlags, localHostCreateFlags,
                    localInitializeFlags, localScriptFlags,
                    localInterpreterFlags, localEventVariableFlags,
                    localEventWaitFlags, args, host, script, varName,
                    maxStackSize, timeout, userInterface, isBackground,
                    useActiveStack, quiet, noBackgroundError,
                    useSelf, noComplain, verbose, debug, usePool,
                    purgeGlobal, noAbort);

                EventWaitHandle startEvent = scriptThread.startEvent =
                    ThreadOps.CreateEvent(false);

                scriptThread.wakeUpEvent = ThreadOps.CreateEvent(true);

                //
                // NOTE: Grab the timeout value from the created script thread,
                //       in case it was changed during the creation process.
                //       When this was written (2013-12-18), it was impossible
                //       for this value to change; however, this may change in
                //       the future.
                //
                timeout = scriptThread.PrivateTimeout;

                //
                // NOTE: Determine if an actual, physical thread needs to be
                //       created now (i.e. otherwise, the thread pool will be
                //       used).
                //
                if (usePool)
                {
                    ReturnCode code = ReturnCode.Ok;

                    try
                    {
                        if (Engine.QueueWorkItem(
                                scriptThread.GetWaitCallback(attach),
                                null, ThreadOps.GetQueueFlags(false)))
                        {
                            if (ThreadOps.WaitEvent(startEvent, timeout))
                            {
                                return scriptThread;
                            }
                            else
                            {
                                /* NO RESULT */
                                scriptThread.PrivateSignalAndSleep(timeout);

                                error = String.Format(
                                    "script thread startup timeout of {0} milliseconds",
                                    timeout);

                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "could not queue work item";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (ThreadAbortException e)
                    {
                        Thread.ResetAbort();

                        error = e;
                        code = ReturnCode.Error;
                    }
                    catch (ThreadInterruptedException e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                    finally
                    {
                        if ((code != ReturnCode.Ok) &&
                            (scriptThread != null))
                        {
                            try
                            {
                                scriptThread.Dispose(); /* throw */
                                scriptThread = null;
                            }
                            catch (Exception e)
                            {
                                PrivateComplain(noComplain, String.Format(
                                    "could not dispose script thread: {0}", e));
                            }
                        }
                    }
                }
                else
                {
                    Thread thread = scriptThread.thread = Engine.CreateThread(
                        interpreter, scriptThread.GetThreadStart(attach),
                        maxStackSize, userInterface, isBackground, useActiveStack);

                    if (thread != null)
                    {
                        thread.Name = GetThreadName(scriptThread);

                        if (start)
                        {
                            ReturnCode code = ReturnCode.Ok;

                            try
                            {
                                thread.Start();

                                if (ThreadOps.WaitEvent(startEvent, timeout))
                                {
                                    return scriptThread;
                                }
                                else
                                {
                                    /* NO RESULT */
                                    scriptThread.PrivateSignalAndSleep(timeout);

                                    /* NO RESULT */
                                    InterruptOrAbortThread(
                                        thread, timeout, verbose, noAbort);

                                    error = String.Format(
                                        "script thread startup timeout of {0} milliseconds",
                                        timeout);

                                    code = ReturnCode.Error;
                                }
                            }
                            catch (ThreadAbortException e)
                            {
                                Thread.ResetAbort();

                                error = e;
                                code = ReturnCode.Error;
                            }
                            catch (ThreadInterruptedException e)
                            {
                                error = e;
                                code = ReturnCode.Error;
                            }
                            catch (Exception e)
                            {
                                error = e;
                                code = ReturnCode.Error;
                            }
                            finally
                            {
                                if ((code != ReturnCode.Ok) &&
                                    (scriptThread != null))
                                {
                                    try
                                    {
                                        scriptThread.Dispose(); /* throw */
                                        scriptThread = null;
                                    }
                                    catch (Exception e)
                                    {
                                        PrivateComplain(noComplain, String.Format(
                                            "could not dispose script thread: {0}", e));
                                    }
                                }
                            }
                        }
                        else
                        {
                            return scriptThread;
                        }
                    }
                    else
                    {
                        error = "could not create script thread";
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this script thread has been disposed.
        /// </summary>
        public bool Disposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */
                // CheckRestricted(); /* EXEMPT */

                return PrivateIsDisposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this script thread is currently in
        /// the process of being disposed.
        /// </summary>
        public bool Disposing
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */
                // CheckRestricted(); /* EXEMPT */

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        /// <summary>
        /// The interpreter hosted by this script thread.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Gets the interpreter hosted by this script thread.
        /// </summary>
        public Interpreter Interpreter
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return interpreter;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptThread Members
        #region Public Properties
        #region Owned Resource Properties
        /// <summary>
        /// The physical thread on which this script thread runs.
        /// </summary>
        private Thread thread;

        /// <summary>
        /// Gets the physical thread on which this script thread runs.
        /// </summary>
        public Thread Thread
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return thread;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Identity & Affinity Properties
        /// <summary>
        /// The unique identifier of this script thread.
        /// </summary>
        private long id;

        /// <summary>
        /// Gets the unique identifier of this script thread.
        /// </summary>
        public long Id
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateId;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of this script thread.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets the name of this script thread.
        /// </summary>
        public string Name
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This defines an instance property in order to read a static
        //       field.
        //
        /// <summary>
        /// Gets the number of script thread instances that are currently running
        /// their thread-start method.
        /// </summary>
        public int ActiveCount
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return Interlocked.CompareExchange(ref activeCount, 0, 0);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Thread Creation & Startup Properties
        /// <summary>
        /// The flags controlling the behavior of this script thread.
        /// </summary>
        private ThreadFlags threadFlags;

        /// <summary>
        /// Gets the flags controlling the behavior of this script thread.
        /// </summary>
        public ThreadFlags ThreadFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return threadFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The maximum stack size, in bytes, used when creating the physical
        /// thread.
        /// </summary>
        private int maxStackSize;

        /// <summary>
        /// Gets the maximum stack size, in bytes, used when creating the
        /// physical thread.
        /// </summary>
        public int MaxStackSize
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return maxStackSize;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The timeout, in milliseconds, used during thread creation and
        /// shutdown.
        /// </summary>
        private int timeout;

        /// <summary>
        /// Gets the timeout, in milliseconds, used during thread creation and
        /// shutdown.
        /// </summary>
        public int Timeout
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateTimeout;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Thread Creation Properties
        /// <summary>
        /// When non-zero, the thread is configured for user-interface use.
        /// </summary>
        private bool userInterface;

        /// <summary>
        /// Gets a value indicating whether the thread is configured for
        /// user-interface use.
        /// </summary>
        public bool UserInterface
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return userInterface;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the thread runs as a background thread.
        /// </summary>
        private bool isBackground;

        /// <summary>
        /// Gets a value indicating whether the thread runs as a background
        /// thread.
        /// </summary>
        public bool IsBackground
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return isBackground;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Creation & Startup Properties
        /// <summary>
        /// The arguments used when creating the interpreter for this script
        /// thread.
        /// </summary>
        private IEnumerable<string> args;

        /// <summary>
        /// Gets the arguments used when creating the interpreter for this script
        /// thread.
        /// </summary>
        public IEnumerable<string> Args
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return args;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The host used when creating the interpreter for this script thread.
        /// </summary>
        private IHost host;

        /// <summary>
        /// Gets the host used when creating the interpreter for this script
        /// thread.
        /// </summary>
        public IHost Host
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return host;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter creation flags used when creating the interpreter for
        /// this script thread.
        /// </summary>
        private CreateFlags createFlags;

        /// <summary>
        /// Gets the interpreter creation flags used when creating the
        /// interpreter for this script thread.
        /// </summary>
        public CreateFlags CreateFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return createFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The host creation flags used when creating the interpreter for this
        /// script thread.
        /// </summary>
        private HostCreateFlags hostCreateFlags;

        /// <summary>
        /// Gets the host creation flags used when creating the interpreter for
        /// this script thread.
        /// </summary>
        public HostCreateFlags HostCreateFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return hostCreateFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The initialization flags used when creating the interpreter for this
        /// script thread.
        /// </summary>
        private InitializeFlags initializeFlags;

        /// <summary>
        /// Gets the initialization flags used when creating the interpreter for
        /// this script thread.
        /// </summary>
        public InitializeFlags InitializeFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return initializeFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script flags used when creating the interpreter for this script
        /// thread.
        /// </summary>
        private ScriptFlags scriptFlags;

        /// <summary>
        /// Gets the script flags used when creating the interpreter for this
        /// script thread.
        /// </summary>
        public ScriptFlags ScriptFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return scriptFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter flags used when creating the interpreter for this
        /// script thread.
        /// </summary>
        private InterpreterFlags interpreterFlags;

        /// <summary>
        /// Gets the interpreter flags used when creating the interpreter for
        /// this script thread.
        /// </summary>
        public InterpreterFlags InterpreterFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return interpreterFlags;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Handling Properties
        /// <summary>
        /// When non-zero, this script thread object is added into its own
        /// interpreter.
        /// </summary>
        private bool useSelf;

        /// <summary>
        /// Gets a value indicating whether this script thread object is added
        /// into its own interpreter.
        /// </summary>
        public bool UseSelf
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return useSelf;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the created interpreter is pushed onto the active
        /// interpreter stack while the script thread runs.
        /// </summary>
        private bool useActiveStack;

        /// <summary>
        /// Gets a value indicating whether the created interpreter is pushed
        /// onto the active interpreter stack while the script thread runs.
        /// </summary>
        public bool UseActiveStack
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return useActiveStack;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Handling Properties
        /// <summary>
        /// When non-zero, the interpreter suppresses certain error reporting.
        /// </summary>
        private bool quiet;

        /// <summary>
        /// Gets a value indicating whether the interpreter suppresses certain
        /// error reporting.
        /// </summary>
        public bool Quiet
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return quiet;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, background error processing is disabled for the
        /// interpreter.
        /// </summary>
        private bool noBackgroundError;

        /// <summary>
        /// Gets a value indicating whether background error processing is
        /// disabled for the interpreter.
        /// </summary>
        public bool NoBackgroundError
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return noBackgroundError;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Handling Properties
        /// <summary>
        /// The startup script evaluated on this script thread, or null for no
        /// startup script.
        /// </summary>
        private IScript script;

        /// <summary>
        /// Gets the startup script evaluated on this script thread, or null for
        /// no startup script.
        /// </summary>
        public IScript Script // NOTE: For no startup script, use null.
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return script;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the variable this script thread waits on, or null for no
        /// wait.
        /// </summary>
        private string varName;

        /// <summary>
        /// Gets the name of the variable this script thread waits on, or null
        /// for no wait.
        /// </summary>
        public string VarName // NOTE: For no wait, use null.
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return varName;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The event wait flags used when waiting on the event variable.
        /// </summary>
        private EventWaitFlags eventWaitFlags;

        /// <summary>
        /// Gets the event wait flags used when waiting on the event variable.
        /// </summary>
        public EventWaitFlags EventWaitFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return eventWaitFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The variable flags used when waiting on the event variable.
        /// </summary>
        private VariableFlags eventVariableFlags;

        /// <summary>
        /// Gets the variable flags used when waiting on the event variable.
        /// </summary>
        public VariableFlags EventVariableFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return eventVariableFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, complaints are suppressed (and emitted as traces
        /// instead) when an error is encountered.
        /// </summary>
        private bool noComplain;

        /// <summary>
        /// Gets a value indicating whether complaints are suppressed (and
        /// emitted as traces instead) when an error is encountered.
        /// </summary>
        public bool NoComplain
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return noComplain;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Diagnostic Read-Write Properties
        /// <summary>
        /// When non-zero, verbose diagnostic tracing is enabled.
        /// </summary>
        private bool verbose;

        /// <summary>
        /// Gets or sets a value indicating whether verbose diagnostic tracing is
        /// enabled.
        /// </summary>
        public bool Verbose
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return verbose;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    verbose = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, debug behavior is enabled.
        /// </summary>
        private bool debug;

        /// <summary>
        /// Gets or sets a value indicating whether debug behavior is enabled.
        /// </summary>
        public bool Debug
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return debug;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    debug = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The return code most recently associated with this script thread.
        /// </summary>
        private ReturnCode returnCode;

        /// <summary>
        /// Gets or sets the return code most recently associated with this
        /// script thread.
        /// </summary>
        public ReturnCode ReturnCode
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return returnCode;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    returnCode = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The result most recently associated with this script thread.
        /// </summary>
        private Result result;

        /// <summary>
        /// Gets or sets the result most recently associated with this script
        /// thread.
        /// </summary>
        public Result Result
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return result;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    result = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Thread State Properties
        /// <summary>
        /// Gets a value indicating whether the underlying physical thread is
        /// currently alive.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateIsAlive;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the hosted interpreter is currently
        /// busy.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateIsBusy;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this script thread has been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */
                // CheckRestricted(); /* EXEMPT */

                return PrivateIsDisposed;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Disposal & Purging Properties
        /// <summary>
        /// When non-zero, the script thread runs using the thread pool instead
        /// of a dedicated physical thread.
        /// </summary>
        private bool usePool;

        /// <summary>
        /// Gets or sets a value indicating whether the script thread runs using
        /// the thread pool instead of a dedicated physical thread.
        /// </summary>
        public bool UsePool
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return usePool;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    usePool = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, global context information is also purged when the
        /// thread exits.
        /// </summary>
        private bool purgeGlobal;

        /// <summary>
        /// Gets or sets a value indicating whether global context information is
        /// also purged when the thread exits.
        /// </summary>
        public bool PurgeGlobal
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return purgeGlobal;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    purgeGlobal = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the thread is never forcibly aborted.
        /// </summary>
        private bool noAbort;

        /// <summary>
        /// Gets or sets a value indicating whether the thread is never forcibly
        /// aborted.
        /// </summary>
        public bool NoAbort
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return noAbort;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    noAbort = value;
                }
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Thread State Methods
        /// <summary>
        /// This method attempts to start the underlying physical thread for this
        /// script thread.
        /// </summary>
        /// <returns>
        /// True if the thread was started (or is already alive); otherwise,
        /// false.
        /// </returns>
        public bool Start()
        {
            CheckDisposed();
            CheckRestricted();

            try
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                }

                if (thread == null)
                    return false;
                else if (ThreadOps.IsAlive(thread))
                    return true;

                thread.Start(); /* throw */
                return true;
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not start thread: {0}", e));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to stop this script thread by interrupting it.
        /// </summary>
        /// <returns>
        /// True if the thread was stopped (or is not alive); otherwise, false.
        /// </returns>
        public bool Stop()
        {
            CheckDisposed();
            CheckRestricted();

            return Stop(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to stop this script thread, optionally aborting
        /// it forcibly.
        /// </summary>
        /// <param name="force">
        /// Non-zero to forcibly abort the thread (unless aborting is disabled);
        /// zero to merely interrupt it.
        /// </param>
        /// <returns>
        /// True if the thread was stopped (or is not alive); otherwise, false.
        /// </returns>
        public bool Stop(
            bool force
            )
        {
            CheckDisposed();
            CheckRestricted();

            bool noAbort = PrivateIsNoAbort(); /* REDUNDANT */

            try
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                    noAbort = PrivateIsNoAbort(); /* REFRESH */
                }

                if (thread == null)
                    return false;
                else if (!ThreadOps.IsAlive(thread))
                    return true;

                //
                // NOTE: If the NoAbort thread flag is set, we NEVER
                //       call the Abort() method.
                //
                if (force && !noAbort)
                    thread.Abort(); /* BUGBUG: Leaks? */
                else
                    thread.Interrupt(); /* throw */

                return true;
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not {0} thread: {1}", force && !noAbort ?
                    "abort" : "interrupt", e));
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region CLR Object Integration Methods
        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, using the default object option type and
        /// flags.  Any failure is reported as a complaint.
        /// </summary>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (which is also reported as a complaint).
        /// </returns>
        public ReturnCode AddObject(
            object value
            )
        {
            CheckDisposed();
            CheckRestricted();

            ReturnCode code;
            Result result = null;

            code = AddObject(value, ref result);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, using the default object option type and
        /// flags.
        /// </summary>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the added
        /// object; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode AddObject(
            object value,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(value, true, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, optionally creating a command alias for it.
        /// </summary>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the added
        /// object; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode AddObject(
            object value,
            bool alias,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                DefaultObjectOptionType, value, alias, false, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, using the specified object option type and
        /// optionally creating a command alias for it.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type used when adding the object.
        /// </param>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the added
        /// object; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            object value,
            bool alias,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                objectOptionType, DefaultObjectFlags, value, alias, false,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, using the specified object option type and
        /// optionally creating a command alias (and alias reference) for it.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type used when adding the object.
        /// </param>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the added object.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add a reference to the alias for the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the added
        /// object; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                objectOptionType, DefaultObjectFlags, value, alias,
                aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, using the specified object option type and
        /// object flags and optionally creating a command alias (and alias
        /// reference) for it.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type used when adding the object.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when adding the object.
        /// </param>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the added object.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add a reference to the alias for the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the added
        /// object; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                objectOptionType, null, objectFlags, value, alias,
                aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified CLR object into the interpreter hosted
        /// by this script thread, using the specified object option type, object
        /// name, and object flags and optionally creating a command alias (and
        /// alias reference) for it.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type used when adding the object.
        /// </param>
        /// <param name="objectName">
        /// The name to use for the added object, or null to generate one
        /// automatically.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when adding the object.
        /// </param>
        /// <param name="value">
        /// The CLR object to add into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the added object.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add a reference to the alias for the added object.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the added
        /// object; upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            string objectName,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return FixupReturnValue(
                null, null, objectOptionType, objectName,
                objectFlags, value, alias, aliasReference,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Synchronous Wait Methods
        /// <summary>
        /// This method waits indefinitely for this script thread to finish
        /// starting up.
        /// </summary>
        /// <returns>
        /// True if the thread started; otherwise, false.
        /// </returns>
        public bool WaitForStart()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForStart(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for this script
        /// thread to finish starting up.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread started; otherwise, false.
        /// </returns>
        public bool WaitForStart(
            int timeout
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForStart(timeout, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for this script
        /// thread to finish starting up, optionally treating a missing start
        /// event as a failure.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of a start event as a failure; zero to
        /// treat it as success.
        /// </param>
        /// <returns>
        /// True if the thread started; otherwise, false.
        /// </returns>
        public bool WaitForStart(
            int timeout,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForStart(timeout, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits indefinitely for this script thread to end.
        /// </summary>
        /// <returns>
        /// True if the thread ended; otherwise, false.
        /// </returns>
        public bool WaitForEnd()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEnd(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for this script
        /// thread to end.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread ended; otherwise, false.
        /// </returns>
        public bool WaitForEnd(
            int timeout
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEnd(timeout, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for this script
        /// thread to end, optionally treating a missing thread as a failure.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of a running thread as a failure; zero
        /// to treat it as success.
        /// </param>
        /// <returns>
        /// True if the thread ended; otherwise, false.
        /// </returns>
        public bool WaitForEnd(
            int timeout,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForEnd(timeout, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits indefinitely for the event queue of this script
        /// thread's interpreter to become empty.
        /// </summary>
        /// <returns>
        /// True if the event queue became empty; otherwise, false.
        /// </returns>
        public bool WaitForEmpty()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEmpty(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for the event queue
        /// of this script thread's interpreter to become empty.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the event queue became empty; otherwise, false.
        /// </returns>
        public bool WaitForEmpty(
            int timeout
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEmpty(timeout, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for the event queue
        /// of this script thread's interpreter to become empty.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an incomplete wait as a failure; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the event queue became empty; otherwise, false.
        /// </returns>
        public bool WaitForEmpty(
            int timeout,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEmpty(timeout, false, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for the event queue
        /// of this script thread's interpreter to become empty, optionally also
        /// waiting until the queue is idle.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="idle">
        /// Non-zero to also wait until the event queue is idle.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an incomplete wait as a failure; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the event queue became empty; otherwise, false.
        /// </returns>
        public bool WaitForEmpty(
            int timeout,
            bool idle,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForEmpty(timeout, idle, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits indefinitely for an event to be enqueued in this
        /// script thread's interpreter.
        /// </summary>
        /// <returns>
        /// True if an event was enqueued; otherwise, false.
        /// </returns>
        public bool WaitForEvent()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEvent(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for an event to be
        /// enqueued in this script thread's interpreter.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if an event was enqueued; otherwise, false.
        /// </returns>
        public bool WaitForEvent(
            int timeout
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEvent(timeout, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for an event to be
        /// enqueued in this script thread's interpreter.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an incomplete wait as a failure; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if an event was enqueued; otherwise, false.
        /// </returns>
        public bool WaitForEvent(
            int timeout,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEvent(timeout, false, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for an event to be
        /// enqueued in this script thread's interpreter, optionally also waiting
        /// until the queue is idle.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="idle">
        /// Non-zero to also wait until the event queue is idle.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an incomplete wait as a failure; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if an event was enqueued; otherwise, false.
        /// </returns>
        public bool WaitForEvent(
            int timeout,
            bool idle,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForEvent(timeout, idle, strict);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Callback Methods
        /// <summary>
        /// This method queues the specified callback for asynchronous execution
        /// on this script thread, scheduled for immediate execution.
        /// </summary>
        /// <param name="callback">
        /// The event callback to be invoked on the script thread.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback was successfully queued; otherwise, false.
        /// </returns>
        public bool Queue(
            EventCallback callback,
            IClientData clientData
            )
        {
            CheckDisposed();

            return Queue(TimeOps.GetUtcNow(), callback, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified callback for asynchronous execution
        /// on this script thread, scheduled for the specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time at which the callback should be executed.
        /// </param>
        /// <param name="callback">
        /// The event callback to be invoked on the script thread.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback was successfully queued; otherwise, false.
        /// </returns>
        public bool Queue(
            DateTime dateTime,
            EventCallback callback,
            IClientData clientData
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = interpreter.QueueEvent(
                dateTime, callback, clientData, GetEventFlags(),
                ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Evaluation Methods
        /// <summary>
        /// This method queues the specified script for asynchronous evaluation
        /// on this script thread, scheduled for immediate execution.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate on the script thread.
        /// </param>
        /// <returns>
        /// True if the script was successfully queued; otherwise, false.
        /// </returns>
        public bool Queue(
            string text
            )
        {
            CheckDisposed();

            return Queue(TimeOps.GetUtcNow(), text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues the specified script for asynchronous evaluation
        /// on this script thread, scheduled for the specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time at which the script should be evaluated.
        /// </param>
        /// <param name="text">
        /// The script to evaluate on the script thread.
        /// </param>
        /// <returns>
        /// True if the script was successfully queued; otherwise, false.
        /// </returns>
        public bool Queue(
            DateTime dateTime,
            string text
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = interpreter.QueueScript(
                dateTime, text, GetEventFlags(), ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the specified script asynchronously on this
        /// script thread, invoking the specified callback upon completion.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate on the script thread.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke when the asynchronous evaluation completes.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the script was successfully queued; otherwise, false.
        /// </returns>
        public bool Queue(
            string text,
            AsynchronousCallback callback,
            IClientData clientData
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = interpreter.EvaluateScript(text, callback, clientData,
                ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Synchronous Evaluation Methods
        /// <summary>
        /// This method sends the specified script to this script thread for
        /// synchronous evaluation, waiting indefinitely for the result.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate on the script thread.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of evaluating the script, or an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode Send(
            string text,
            ref Result result
            )
        {
            CheckDisposed();

            return Send(text, DefaultUseEngine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sends the specified script to this script thread for
        /// synchronous evaluation, waiting indefinitely for the result.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate on the script thread.
        /// </param>
        /// <param name="useEngine">
        /// Non-zero to evaluate the script using the engine; zero to evaluate it
        /// via an event callback.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of evaluating the script, or an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode Send(
            string text,
            bool useEngine,
            ref Result result
            )
        {
            CheckDisposed();

            return Send(text, _Timeout.Infinite, useEngine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sends the specified script to this script thread for
        /// synchronous evaluation, waiting up to the specified timeout for the
        /// result.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate on the script thread.
        /// </param>
        /// <param name="timeout">
        /// The maximum time to wait for the result, in milliseconds.
        /// </param>
        /// <param name="useEngine">
        /// Non-zero to evaluate the script using the engine; zero to evaluate it
        /// via an event callback.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of evaluating the script, or an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public ReturnCode Send(
            string text,
            int timeout,
            bool useEngine,
            ref Result result
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            string eventName = FormatOps.EventName(interpreter,
                scriptThreadSendEventPrefix, null, GlobalState.NextEventId(
                interpreter));

            using (EventWaitHandle @event = ThreadOps.CreateEvent(eventName))
            {
                if (useEngine)
                {
                    EventOutputPair eventOutputPair =
                        new EventOutputPair(true);

                    IClientData clientData = new ClientData(
                        new AnyPair<EventWaitHandle, EventOutputPair>(
                            @event, eventOutputPair));

                    if (Queue(
                            text, ScriptAsynchronousCallback, clientData))
                    {
                        if (ThreadOps.WaitEvent(@event, timeout))
                        {
                            result = eventOutputPair.Y;
                            code = eventOutputPair.X;
                        }
                        else
                        {
                            result = String.Format(
                                "engine script timeout of {0} milliseconds",
                                timeout);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "could not queue script to engine thread";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    EventInputPair eventInputPair =
                        new EventInputPair(text, eventName);

                    EventOutputPair eventOutputPair =
                        new EventOutputPair(true);

                    IClientData clientData = new ClientData(
                        new AnyPair<EventInputPair, EventOutputPair>(
                            eventInputPair, eventOutputPair));

                    if (Queue(
                            TimeOps.GetUtcNow(), ScriptEventCallback,
                            clientData))
                    {
                        if (ThreadOps.WaitEvent(@event, timeout))
                        {
                            result = eventOutputPair.Y;
                            code = eventOutputPair.X;
                        }
                        else
                        {
                            result = String.Format(
                                "event script timeout of {0} milliseconds",
                                timeout);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "could not queue script to event thread";
                        code = ReturnCode.Error;
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Signaling Methods
        /// <summary>
        /// This method signals this script thread by setting its associated
        /// event variable to the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to assign to the event variable.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the variable was successfully set; otherwise, false.
        /// </returns>
        public bool Signal(
            string value
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateSignal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wakes up this script thread if it is currently waiting on
        /// its event variable.
        /// </summary>
        /// <returns>
        /// True if the wake-up event was successfully set; otherwise, false.
        /// </returns>
        public bool WakeUp()
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWakeUp();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Cancellation Methods
        /// <summary>
        /// This method requests cancellation of any script currently being
        /// evaluated by this script thread.  Any failure is reported as a
        /// complaint.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags controlling the cancellation behavior.
        /// </param>
        /// <returns>
        /// True if the cancellation was successfully requested; otherwise,
        /// false.
        /// </returns>
        public bool Cancel(
            CancelFlags cancelFlags
            )
        {
            CheckDisposed();
            CheckRestricted();

            Result error = null;

            return Cancel(cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method requests cancellation of any script currently being
        /// evaluated by this script thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags controlling the cancellation behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the cancellation was successfully requested; otherwise,
        /// false.
        /// </returns>
        public bool Cancel(
            CancelFlags cancelFlags,
            ref Result error
            )
        {
            CheckDisposed();
            CheckRestricted();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;

            code = interpreter.InternalCancelAnyEvaluateNoContext(
                null, cancelFlags, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets any pending cancellation state for this script
        /// thread.  Any failure is reported as a complaint.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags controlling the cancellation reset behavior.
        /// </param>
        /// <returns>
        /// True if the cancellation state was successfully reset; otherwise,
        /// false.
        /// </returns>
        public bool ResetCancel(
            CancelFlags cancelFlags
            )
        {
            CheckDisposed();
            CheckRestricted();

            Result error = null;

            return ResetCancel(cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets any pending cancellation state for this script
        /// thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags controlling the cancellation reset behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the cancellation state was successfully reset; otherwise,
        /// false.
        /// </returns>
        public bool ResetCancel(
            CancelFlags cancelFlags,
            ref Result error
            )
        {
            CheckDisposed();
            CheckRestricted();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;

            code = Engine.ResetCancel(interpreter, cancelFlags, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cleanup Methods
        /// <summary>
        /// This method requests cleanup of any per-thread state associated with
        /// this script thread's interpreter.
        /// </summary>
        /// <returns>
        /// True if the cleanup was performed; otherwise, false.
        /// </returns>
        public bool Cleanup()
        {
            CheckDisposed();
            CheckRestricted();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            return interpreter.MaybeDisposeThread();
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this script thread,
        /// including its identifier and thread flags.
        /// </summary>
        /// <returns>
        /// A string representation of this script thread.
        /// </returns>
        public override string ToString()
        {
            long id;
            ThreadFlags threadFlags;

            lock (syncRoot)
            {
                id = this.id;
                threadFlags = this.threadFlags;
            }

            return StringList.MakeList("id", id, "threadFlags", threadFlags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method resolves the effective thread flags for a script thread,
        /// using the supplied flags or the default flags, and decomposes them
        /// into the individual boolean switches used during creation and
        /// execution.
        /// </summary>
        /// <param name="threadFlags">
        /// The requested thread flags, or null to use the default flags.
        /// </param>
        /// <param name="viaAttach">
        /// Non-zero if the script thread is being created in order to attach to
        /// an existing interpreter.
        /// </param>
        /// <param name="throwOnDisposed">
        /// Upon return, this is non-zero if the interpreter should throw when
        /// used after being disposed.
        /// </param>
        /// <param name="safe">
        /// Upon return, this is non-zero if the interpreter should be created in
        /// "safe" mode.
        /// </param>
        /// <param name="noHidden">
        /// Upon return, this is non-zero if hidden commands should not be created
        /// when in safe mode.
        /// </param>
        /// <param name="userInterface">
        /// Upon return, this is non-zero if the thread should be configured for
        /// user-interface use.
        /// </param>
        /// <param name="isBackground">
        /// Upon return, this is non-zero if the thread should be a background
        /// thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Upon return, this is non-zero if the created interpreter should be
        /// pushed onto the active interpreter stack.
        /// </param>
        /// <param name="quiet">
        /// Upon return, this is non-zero if the interpreter should suppress
        /// certain error reporting.
        /// </param>
        /// <param name="noBackgroundError">
        /// Upon return, this is non-zero if background error processing should be
        /// disabled.
        /// </param>
        /// <param name="useSelf">
        /// Upon return, this is non-zero if the script thread object should be
        /// added into its own interpreter.
        /// </param>
        /// <param name="noComplain">
        /// Upon return, this is non-zero if complaints should be suppressed.
        /// </param>
        /// <param name="verbose">
        /// Upon return, this is non-zero if verbose diagnostic tracing should be
        /// enabled.
        /// </param>
        /// <param name="debug">
        /// Upon return, this is non-zero if debug behavior should be enabled.
        /// </param>
        /// <param name="usePool">
        /// Upon return, this is non-zero if the thread pool should be used
        /// instead of a dedicated physical thread.
        /// </param>
        /// <param name="purgeGlobal">
        /// Upon return, this is non-zero if global context information should
        /// also be purged when the thread exits.
        /// </param>
        /// <param name="start">
        /// Upon return, this is non-zero if the thread should be started
        /// immediately after creation.
        /// </param>
        /// <param name="noAbort">
        /// Upon return, this is non-zero if the thread should never be forcibly
        /// aborted.
        /// </param>
        /// <param name="attach">
        /// Upon return, this is non-zero if the script thread should attach to an
        /// existing interpreter.
        /// </param>
        /// <returns>
        /// The resolved thread flags.
        /// </returns>
        private static ThreadFlags GetThreadFlags(
            ThreadFlags? threadFlags,
            bool viaAttach,
            out bool throwOnDisposed,
            out bool safe,
            out bool noHidden,
            out bool userInterface,
            out bool isBackground,
            out bool useActiveStack,
            out bool quiet,
            out bool noBackgroundError,
            out bool useSelf,
            out bool noComplain,
            out bool verbose,
            out bool debug,
            out bool usePool,
            out bool purgeGlobal,
            out bool start,
            out bool noAbort,
            out bool attach
            )
        {
            ThreadFlags result;

            if (threadFlags != null)
                result = (ThreadFlags)threadFlags;
            else
                result = DefaultThreadFlags;

            throwOnDisposed = FlagOps.HasFlags(
                result, ThreadFlags.ThrowOnDisposed, true);

            safe = FlagOps.HasFlags(result, ThreadFlags.Safe, true);

            noHidden = FlagOps.HasFlags(
                result, ThreadFlags.NoHidden, true);

            userInterface = FlagOps.HasFlags(
                result, ThreadFlags.UserInterface, true);

            isBackground = FlagOps.HasFlags(
                result, ThreadFlags.IsBackground, true);

            useActiveStack = FlagOps.HasFlags(
                result, ThreadFlags.UseActiveStack, true);

            quiet = FlagOps.HasFlags(result, ThreadFlags.Quiet, true);

            noBackgroundError = FlagOps.HasFlags(
                result, ThreadFlags.NoBackgroundError, true);

            useSelf = FlagOps.HasFlags(result, ThreadFlags.UseSelf, true);

            noComplain = FlagOps.HasFlags(
                result, ThreadFlags.NoComplain, true);

            verbose = FlagOps.HasFlags(result, ThreadFlags.Verbose, true);
            debug = FlagOps.HasFlags(result, ThreadFlags.Debug, true);

            ///////////////////////////////////////////////////////////////////
            // NOTE: The following are used during Create() / Attach() -AND-
            //       in the ThreadStart() methods.
            ///////////////////////////////////////////////////////////////////

            usePool = FlagOps.HasFlags(result, ThreadFlags.UsePool, true);

            purgeGlobal = FlagOps.HasFlags(
                result, ThreadFlags.PurgeGlobal, true);

            ///////////////////////////////////////////////////////////////////
            // NOTE: The following are used during Create() only.
            ///////////////////////////////////////////////////////////////////

            start = FlagOps.HasFlags(result, ThreadFlags.Start, true);
            noAbort = FlagOps.HasFlags(result, ThreadFlags.NoAbort, true);

            ///////////////////////////////////////////////////////////////////

            attach = viaAttach ||
                FlagOps.HasFlags(result, ThreadFlags.Attach, true);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective interpreter creation flags, using
        /// the supplied flags verbatim or adjusting the default flags according
        /// to the specified boolean switches.
        /// </summary>
        /// <param name="createFlags">
        /// The requested interpreter creation flags, or null to derive them from
        /// the default flags and switches.
        /// </param>
        /// <param name="throwOnDisposed">
        /// Non-zero if the interpreter should throw when used after being
        /// disposed.
        /// </param>
        /// <param name="safe">
        /// Non-zero if the interpreter should be created in "safe" mode.
        /// </param>
        /// <param name="noHidden">
        /// Non-zero if hidden commands should not be created when in safe mode.
        /// </param>
        /// <returns>
        /// The resolved interpreter creation flags.
        /// </returns>
        private static CreateFlags GetCreateFlags(
            CreateFlags? createFlags,
            bool throwOnDisposed,
            bool safe,
            bool noHidden
            )
        {
            CreateFlags result;

            if (createFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (CreateFlags)createFlags;
            }
            else
            {
                //
                // NOTE: Start with the default flags and adjust them according
                //       to the boolean switches specified by the caller.
                //
                result = DefaultCreateFlags;

                if (throwOnDisposed)
                    result |= CreateFlags.ThrowOnDisposed;
                else
                    result &= ~CreateFlags.ThrowOnDisposed;

                if (safe)
                {
                    if (noHidden)
                        result |= CreateFlags.Safe;
                    else
                        result |= CreateFlags.SafeAndHideUnsafe;
                }
                else
                {
                    if (noHidden)
                        result &= ~CreateFlags.Safe;
                    else
                        result &= ~CreateFlags.SafeAndHideUnsafe;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective host creation flags, using the
        /// supplied flags verbatim or the default flags.
        /// </summary>
        /// <param name="hostCreateFlags">
        /// The requested host creation flags, or null to use the default flags.
        /// </param>
        /// <returns>
        /// The resolved host creation flags.
        /// </returns>
        private static HostCreateFlags GetHostCreateFlags(
            HostCreateFlags? hostCreateFlags
            )
        {
            HostCreateFlags result;

            if (hostCreateFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (HostCreateFlags)hostCreateFlags;
            }
            else
            {
                //
                // NOTE: Start with the default flags and adjust them according
                //       to the boolean switches specified by the caller.
                //
                result = DefaultHostCreateFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective initialization flags, using the
        /// supplied flags verbatim or the default flags.
        /// </summary>
        /// <param name="initializeFlags">
        /// The requested initialization flags, or null to use the default flags.
        /// </param>
        /// <returns>
        /// The resolved initialization flags.
        /// </returns>
        private static InitializeFlags GetInitializeFlags(
            InitializeFlags? initializeFlags
            )
        {
            InitializeFlags result;

            if (initializeFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (InitializeFlags)initializeFlags;
            }
            else
            {
                //
                // NOTE: Use the default flags.
                //
                result = DefaultInitializeFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective script flags, using the supplied
        /// flags verbatim or the default flags.
        /// </summary>
        /// <param name="scriptFlags">
        /// The requested script flags, or null to use the default flags.
        /// </param>
        /// <returns>
        /// The resolved script flags.
        /// </returns>
        private static ScriptFlags GetScriptFlags(
            ScriptFlags? scriptFlags
            )
        {
            ScriptFlags result;

            if (scriptFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (ScriptFlags)scriptFlags;
            }
            else
            {
                //
                // NOTE: Use the default flags.
                //
                result = DefaultScriptFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective interpreter flags, using the
        /// supplied flags verbatim or the default flags.
        /// </summary>
        /// <param name="interpreterFlags">
        /// The requested interpreter flags, or null to use the default flags.
        /// </param>
        /// <returns>
        /// The resolved interpreter flags.
        /// </returns>
        private static InterpreterFlags GetInterpreterFlags(
            InterpreterFlags? interpreterFlags
            )
        {
            InterpreterFlags result;

            if (interpreterFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (InterpreterFlags)interpreterFlags;
            }
            else
            {
                //
                // NOTE: Use the default flags.
                //
                result = DefaultInterpreterFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective variable flags used when waiting
        /// on the event variable.
        /// </summary>
        /// <param name="eventVariableFlags">
        /// The requested variable flags, or null to use the default flags.
        /// </param>
        /// <param name="threadFlags">
        /// The thread flags associated with the script thread.
        /// </param>
        /// <returns>
        /// The resolved variable flags.
        /// </returns>
        private static VariableFlags GetEventVariableFlags(
            VariableFlags? eventVariableFlags,
            ThreadFlags threadFlags
            )
        {
            VariableFlags result = (eventVariableFlags != null) ?
                (VariableFlags)eventVariableFlags : DefaultEventVariableFlags;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the effective event wait flags used when waiting
        /// on the event variable, incorporating any relevant thread flags.
        /// </summary>
        /// <param name="eventWaitFlags">
        /// The requested event wait flags, or null to use the default flags.
        /// </param>
        /// <param name="threadFlags">
        /// The thread flags from which additional event wait flags are derived.
        /// </param>
        /// <returns>
        /// The resolved event wait flags.
        /// </returns>
        private static EventWaitFlags GetEventWaitFlags(
            EventWaitFlags? eventWaitFlags,
            ThreadFlags threadFlags
            )
        {
            EventWaitFlags result = (eventWaitFlags != null) ?
                (EventWaitFlags)eventWaitFlags : DefaultEventWaitFlags;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.NoCancel, true))
                result |= EventWaitFlags.NoCancel;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.StopOnError, true))
                result |= EventWaitFlags.StopOnError;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.ErrorOnEmpty, true))
                result |= EventWaitFlags.ErrorOnEmpty;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.NoComplain, true))
                result |= EventWaitFlags.NoComplain;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.Trace, true))
                result |= EventWaitFlags.Trace;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.FollowLink, true))
                result |= EventWaitFlags.FollowLink;

#if NATIVE && TCL
            if (FlagOps.HasFlags(threadFlags, ThreadFlags.TclThread, true))
                result |= EventWaitFlags.TclDoOneEvent;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.TclWaitEvent, true))
                result |= EventWaitFlags.TclWaitEvent;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.TclAllEvents, true))
                result |= EventWaitFlags.TclAllEvents;
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a suitable name for the physical thread of the
        /// specified script thread, using its explicit name if available or a
        /// generated one otherwise.
        /// </summary>
        /// <param name="scriptThread">
        /// The script thread for which to obtain a thread name.
        /// </param>
        /// <returns>
        /// The thread name, or null if the specified script thread is null.
        /// </returns>
        private static string GetThreadName(
            ScriptThread scriptThread
            )
        {
            if (scriptThread == null)
                return null;

            string name = scriptThread.PrivateName;

            if (name != null)
                return name;

            return String.Format("scriptThread#{0}", scriptThread.PrivateId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a failure result, either as a complaint or (when
        /// complaints are suppressed) as a diagnostic trace.  This overload is
        /// for use by the static factory methods.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to emit a diagnostic trace instead of a complaint.
        /// </param>
        /// <param name="result">
        /// The result describing the failure.
        /// </param>
        private static void PrivateComplain( /* NOTE: For Create() only. */
            bool noComplain,
            Result result
            )
        {
            PrivateComplain(null, noComplain, ReturnCode.Error, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a failure, either as a complaint or (when
        /// complaints are suppressed) as a diagnostic trace.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the failure, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to emit a diagnostic trace instead of a complaint.
        /// </param>
        /// <param name="code">
        /// The return code describing the failure.
        /// </param>
        /// <param name="result">
        /// The result describing the failure.
        /// </param>
        private static void PrivateComplain(
            Interpreter interpreter,
            bool noComplain,
            ReturnCode code,
            Result result
            )
        {
            if (noComplain)
            {
                TraceOps.DebugTrace(interpreter, String.Format(
                    "PrivateComplain: {0}", ResultOps.Format(
                    code, result)), typeof(ScriptThread).Name,
                    TracePriority.ScriptThreadError, 1);

                return;
            }

            DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method implements the event callback used to evaluate a script
        /// synchronously on the script thread and convey its result back to the
        /// waiting caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter on which to evaluate the script.
        /// </param>
        /// <param name="clientData">
        /// The client data containing the input and output pair for the
        /// operation.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of evaluating the script, or an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode ScriptEventCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                result = "invalid clientData";
                return ReturnCode.Error;
            }

            IAnyPair<EventInputPair, EventOutputPair> anyPair =
                clientData.Data as IAnyPair<EventInputPair, EventOutputPair>;

            if (anyPair == null)
            {
                result = "clientData is not a pair";
                return ReturnCode.Error;
            }

            EventInputPair eventInputPair = anyPair.X;

            if (eventInputPair == null)
            {
                result = "invalid event input pair";
                return ReturnCode.Error;
            }

            EventOutputPair eventOutputPair = anyPair.Y;

            if (eventOutputPair == null)
            {
                result = "invalid event output pair";
                return ReturnCode.Error;
            }

            string eventName = eventInputPair.Y;

            using (EventWaitHandle @event = ThreadOps.OpenEvent(eventName))
            {
                try
                {
                    result = eventOutputPair.Y;

                    eventOutputPair.X = interpreter.EvaluateScript(
                        eventInputPair.X, ref result); /* EXEMPT */

                    eventOutputPair.Y = result;

                    return eventOutputPair.X;
                }
                finally
                {
                    if (@event != null)
                    {
                        /* IGNORED */
                        ThreadOps.SetEvent(@event);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method implements the asynchronous callback used to convey the
        /// return code and result of a script evaluation back to the waiting
        /// caller and signal its associated event.
        /// </summary>
        /// <param name="context">
        /// The asynchronous context containing the result and the client data
        /// for the operation.
        /// </param>
        private static void ScriptAsynchronousCallback(
            IAsynchronousContext context
            )
        {
            if (context == null)
                return;

            IClientData clientData = context.ClientData;

            if (clientData == null)
                return;

            IAnyPair<EventWaitHandle, EventOutputPair> anyPair =
                clientData.Data as IAnyPair<EventWaitHandle, EventOutputPair>;

            if (anyPair == null)
                return;

            EventOutputPair eventOutputPair = anyPair.Y;

            if (eventOutputPair == null)
                return;

            EventWaitHandle @event = anyPair.X; /* NOT OWNED */

            try
            {
                eventOutputPair.X = context.ReturnCode;
                eventOutputPair.Y = context.Result;
            }
            finally
            {
                if (@event != null)
                {
                    /* IGNORED */
                    ThreadOps.SetEvent(@event);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marshals the specified value into a result for the
        /// interpreter, optionally creating a command alias and an alias
        /// reference.  This static overload validates the interpreter before
        /// delegating to the marshalling helper.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter into which the value is being marshalled.
        /// </param>
        /// <param name="currentOptions">
        /// The current options for the operation.  This parameter may be null.
        /// </param>
        /// <param name="aliasOptions">
        /// The options to use for any created alias.  This parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type used when marshalling the value.
        /// </param>
        /// <param name="objectName">
        /// The name to use for the marshalled object, or null to generate one
        /// automatically.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when marshalling the value.
        /// </param>
        /// <param name="value">
        /// The value to marshal into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the marshalled value.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add a reference to the alias for the marshalled value.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the
        /// marshalled value; upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            OptionDictionary currentOptions,
            OptionDictionary aliasOptions,
            ObjectOptionType objectOptionType,
            string objectName,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                result = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return MarshalOps.FixupReturnValue(
                interpreter, interpreter.InternalBinder,
                interpreter.InternalCultureInfo, null, objectFlags,
                currentOptions, aliasOptions, objectOptionType,
                objectName, null, value, true, false, alias,
                aliasReference, false, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the specified script using the specified
        /// interpreter, validating the interpreter beforehand.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter on which to evaluate the script.
        /// </param>
        /// <param name="script">
        /// The script to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the result of evaluating the script, or an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private static ReturnCode EvaluateScript(
            Interpreter interpreter,
            IScript script,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                result = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(script, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to interrupt, and if necessary forcibly abort,
        /// the specified physical thread, waiting up to the specified timeout for
        /// it to exit.
        /// </summary>
        /// <param name="thread">
        /// The physical thread to interrupt or abort.  This parameter may be
        /// null.
        /// </param>
        /// <param name="timeout">
        /// The maximum time to wait for the thread to exit, in milliseconds.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic tracing.
        /// </param>
        /// <param name="noAbort">
        /// Non-zero to prevent the thread from being forcibly aborted.
        /// </param>
        private static void InterruptOrAbortThread(
            Thread thread,
            int timeout,
            bool verbose,
            bool noAbort
            )
        {
            //
            // NOTE: Finally, check if the physical thread is still alive.
            //       If so, try to interrupt and/or abort it.
            //
            if (thread == null)
                return;

            string threadName = FormatOps.DisplayThread(thread);

            if (!ThreadOps.IsAlive(thread))
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "InterruptOrAbortThread: script thread " +
                        "with [{0}] is already dead", threadName),
                        typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }

                thread = null;
                return;
            }

            try
            {
                //
                // NOTE: The thread is still alive and it must die now.
                //       Wait for a bit (now that the interpreter has
                //       been disposed) and then forcibly abort it if
                //       necessary.
                //
                thread.Interrupt(); /* throw */

                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "InterruptOrAbortThread: interrupted " +
                        "script thread with [{0}]", threadName),
                        typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }

                if (thread.Join(timeout))
                {
                    if (verbose)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "InterruptOrAbortThread: joined script thread " +
                            "with [{0}] and with timeout {1}", threadName,
                            timeout), typeof(ScriptThread).Name,
                            TracePriority.ScriptThreadDebug);
                    }
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "InterruptOrAbortThread: failed to join " +
                        "script thread with [{0}] and with timeout {1}",
                        threadName, timeout), typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadError);

                    //
                    // NOTE: If the NoAbort thread flag is set, we NEVER
                    //       call the Abort() method.
                    //
                    if (!ThreadOps.IsAlive(thread))
                    {
                        if (verbose)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "InterruptOrAbortThread: script thread " +
                                "with [{0}] is not alive", threadName),
                                typeof(ScriptThread).Name,
                                TracePriority.ScriptThreadDebug);
                        }
                    }
                    else if (!noAbort)
                    {
                        thread.Abort(); /* BUGBUG: Leaks? */

                        TraceOps.DebugTrace(String.Format(
                            "InterruptOrAbortThread: aborted script " +
                            "thread with [{0}]", threadName),
                            typeof(ScriptThread).Name,
                            TracePriority.ScriptThreadDebug);
                    }
                    else
                    {
                        if (verbose)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "InterruptOrAbortThread: skipped aborting " +
                                "script thread with [{0}]", threadName),
                                typeof(ScriptThread).Name,
                                TracePriority.ScriptThreadDebug);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing much we can do here
                //       except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(ScriptThread).Name,
                    TracePriority.ScriptThreadError);
            }

            thread = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method determines whether this script thread is currently marked
        /// as restricted (i.e. most of its methods are not permitted).
        /// </summary>
        /// <returns>
        /// True if this script thread is restricted; otherwise, false.
        /// </returns>
        private bool IsRestricted()
        {
            ThreadFlags threadFlags;

            lock (syncRoot)
            {
                threadFlags = this.threadFlags;
            }

            return FlagOps.HasFlags(threadFlags,
                ThreadFlags.Restricted, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method throws an exception if this script thread is currently
        /// marked as restricted.
        /// </summary>
        private void CheckRestricted() /* throw */
        {
            if (IsRestricted())
                throw new ScriptException("method access denied");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this script thread as restricted or unrestricted.
        /// </summary>
        /// <param name="restricted">
        /// Non-zero to mark this script thread as restricted; zero to mark it as
        /// unrestricted.
        /// </param>
        private void MarkRestricted(
            bool restricted
            )
        {
            lock (syncRoot)
            {
                if (restricted)
                    threadFlags |= ThreadFlags.Restricted;
                else
                    threadFlags &= ~ThreadFlags.Restricted;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the event flags to use when queuing events to the
        /// interpreter hosted by this script thread.
        /// </summary>
        /// <returns>
        /// The event flags to use when queuing events.
        /// </returns>
        private EventFlags GetEventFlags()
        {
            Interpreter interpreter;
            bool debug;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
                debug = this.debug;
            }

            return EventOps.GetQueueEventFlags((interpreter != null) ?
                interpreter.QueueEventFlags : EventFlags.None, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the unique identifier of this script thread, without performing
        /// any disposal or restriction checks.
        /// </summary>
        private long PrivateId /* NOTE: For Create(). */
        {
            get { lock (syncRoot) { return id; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the name of this script thread, without performing any disposal
        /// or restriction checks.
        /// </summary>
        private string PrivateName /* NOTE: For Create(). */
        {
            get { lock (syncRoot) { return name; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the timeout, in milliseconds, of this script thread, without
        /// performing any disposal or restriction checks.
        /// </summary>
        private int PrivateTimeout /* NOTE: For Create(). */
        {
            get { lock (syncRoot) { return timeout; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying physical thread is
        /// currently alive, without performing any disposal or restriction
        /// checks.
        /// </summary>
        private bool PrivateIsAlive
        {
            get
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                }

                return ThreadOps.IsAlive(thread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the hosted interpreter is currently
        /// busy, without performing any disposal or restriction checks.
        /// </summary>
        internal bool PrivateIsBusy /* NOTE: For Interpreter.IsOwnerBusy(). */
        {
            get
            {
                Interpreter interpreter;

                lock (syncRoot)
                {
                    interpreter = this.interpreter;
                }

                return (interpreter != null) && interpreter.InternalIsBusy;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this script thread has been disposed.
        /// </summary>
        private bool PrivateIsDisposed
        {
            get
            {
                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by Signal(), PrivateSignalAndSleep() and Shutdown()
        //       only.
        //
        /// <summary>
        /// This method signals this script thread by setting its associated
        /// event variable to the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to assign to the event variable.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the variable was successfully set; otherwise, false.
        /// </returns>
        private bool PrivateSignal(
            string value
            )
        {
            Interpreter interpreter;
            string varName;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
                varName = this.varName;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (interpreter.Disposed)
                    return false;

                code = interpreter.SetVariableValue(
                    VariableFlags.None, varName, value, null, ref error);
            }

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method wakes up this script thread if it is currently waiting on
        /// its event variable.
        /// </summary>
        /// <returns>
        /// True if the wake-up event was successfully set; otherwise, false.
        /// </returns>
        private bool PrivateWakeUp() /* NOTE: For Shutdown(). */
        {
            EventWaitHandle wakeUpEvent;

            lock (syncRoot)
            {
                wakeUpEvent = this.wakeUpEvent;
            }

            if (wakeUpEvent == null)
                return false;

            return ThreadOps.SetEvent(wakeUpEvent);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for this script
        /// thread to finish starting up.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of a start event as a failure; zero to
        /// treat it as success.
        /// </param>
        /// <returns>
        /// True if the thread started; otherwise, false.
        /// </returns>
        private bool PrivateWaitForStart( /* NOTE: For WaitForStart(). */
            int timeout,
            bool strict
            )
        {
            try
            {
                EventWaitHandle startEvent;

                lock (syncRoot)
                {
                    startEvent = this.startEvent;
                }

                if (startEvent == null)
                    return !strict;

                return ThreadOps.WaitEvent(startEvent, timeout);
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not wait for thread start: {0}", e));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for this script
        /// thread to end.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of a running thread as a failure; zero
        /// to treat it as success.
        /// </param>
        /// <returns>
        /// True if the thread ended; otherwise, false.
        /// </returns>
        private bool PrivateWaitForEnd( /* NOTE: For Shutdown(). */
            int timeout,
            bool strict
            )
        {
            try
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                }

                if (thread == null)
                    return !strict;
                else if (!ThreadOps.IsAlive(thread))
                    return true;

                return thread.Join(timeout); /* throw */
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not wait for thread join: {0}", e));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for the event queue
        /// of this script thread's interpreter to become empty, optionally also
        /// waiting until the queue is idle.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="idle">
        /// Non-zero to also wait until the event queue is idle.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an incomplete wait as a failure; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the event queue became empty; otherwise, false.
        /// </returns>
        private bool PrivateWaitForEmpty( /* NOTE: For WaitForEmpty(). */
            int timeout,
            bool idle,
            bool strict
            )
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (!EventOps.ManagerIsOk(eventManager))
                return false;

            ReturnCode code;
            Result error = null;

            code = eventManager.WaitForEmptyQueue(
                timeout, idle, strict, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits, up to the specified timeout, for an event to be
        /// enqueued in this script thread's interpreter, optionally also waiting
        /// until the queue is idle.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="idle">
        /// Non-zero to also wait until the event queue is idle.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat an incomplete wait as a failure; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if an event was enqueued; otherwise, false.
        /// </returns>
        private bool PrivateWaitForEvent( /* NOTE: For WaitForEvent(). */
            int timeout,
            bool idle,
            bool strict
            )
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (!EventOps.ManagerIsOk(eventManager))
                return false;

            ReturnCode code;
            Result error = null;

            code = eventManager.WaitForEventEnqueued(
                timeout, idle, strict, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by Create() only.
        //
        /// <summary>
        /// This method signals this script thread and then sleeps for the
        /// specified timeout, in an attempt to allow a newly created script
        /// thread to exit.
        /// </summary>
        /// <param name="timeout">
        /// The time to sleep after signaling, in milliseconds.
        /// </param>
        private void PrivateSignalAndSleep(
            int timeout
            )
        {
            //
            // HACK: Attempt to get the newly created script thread to exit
            //       by signaling its variable (if possible).
            //
            /* IGNORED */
            PrivateSignal(null);

            /* IGNORED */
            HostOps.ThreadSleep(timeout); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a failure for this script thread, either as a
        /// complaint or (when complaints are suppressed) as a diagnostic trace.
        /// </summary>
        /// <param name="code">
        /// The return code describing the failure.
        /// </param>
        /// <param name="result">
        /// The result describing the failure.
        /// </param>
        private void PrivateComplain(
            ReturnCode code,
            Result result
            )
        {
            Interpreter interpreter;
            bool noComplain;

            lock (syncRoot)
            {
                noComplain = this.noComplain;
                interpreter = this.interpreter;
            }

            PrivateComplain(interpreter, noComplain, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this script thread is configured to
        /// never be forcibly aborted.
        /// </summary>
        /// <returns>
        /// True if this script thread should never be forcibly aborted;
        /// otherwise, false.
        /// </returns>
        private bool PrivateIsNoAbort() /* NO-LOCK */
        {
            /* lock (syncRoot) */ { return this.noAbort; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marshals the specified value into a result for the
        /// interpreter hosted by this script thread, optionally creating a
        /// command alias and an alias reference.
        /// </summary>
        /// <param name="currentOptions">
        /// The current options for the operation.  This parameter may be null.
        /// </param>
        /// <param name="aliasOptions">
        /// The options to use for any created alias.  This parameter may be null.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type used when marshalling the value.
        /// </param>
        /// <param name="objectName">
        /// The name to use for the marshalled object, or null to generate one
        /// automatically.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags used when marshalling the value.
        /// </param>
        /// <param name="value">
        /// The value to marshal into the interpreter.
        /// </param>
        /// <param name="alias">
        /// Non-zero to create a command alias for the marshalled value.
        /// </param>
        /// <param name="aliasReference">
        /// Non-zero to add a reference to the alias for the marshalled value.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the name of (or a reference to) the
        /// marshalled value; upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        private ReturnCode FixupReturnValue(
            OptionDictionary currentOptions,
            OptionDictionary aliasOptions,
            ObjectOptionType objectOptionType,
            string objectName,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                result = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return FixupReturnValue(
                interpreter, currentOptions, aliasOptions,
                objectOptionType, objectName, objectFlags,
                value, alias, aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the specified variable in the specified
        /// interpreter to change, subject to the configured event wait and
        /// variable flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter containing the variable to wait on.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to wait on.
        /// </param>
        /// <param name="microseconds">
        /// The amount of time to wait, in microseconds.
        /// </param>
        /// <param name="limit">
        /// The maximum number of events to process while waiting.
        /// </param>
        /// <param name="notReady">
        /// Upon return, this is non-zero if the wait could not be performed
        /// because the interpreter was not ready.
        /// </param>
        /// <param name="timedOut">
        /// Upon return, this is non-zero if the wait timed out.
        /// </param>
        /// <param name="changed">
        /// Upon return, this is non-zero if the variable was changed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private ReturnCode WaitVariable(
            Interpreter interpreter,
            string varName,
            long microseconds,
            int limit,
            ref bool notReady,
            ref bool timedOut,
            ref bool changed,
            ref Result error
            )
        {
            EventWaitHandle wakeUpEvent;
            VariableFlags eventVariableFlags;
            EventWaitFlags eventWaitFlags;

            lock (syncRoot)
            {
                wakeUpEvent = this.wakeUpEvent;
                eventVariableFlags = this.eventVariableFlags;
                eventWaitFlags = this.eventWaitFlags;
            }

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                error = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return interpreter.WaitVariable(
                eventWaitFlags, eventVariableFlags, varName,
                microseconds, null, limit, wakeUpEvent,
                ref notReady, ref timedOut, ref changed,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the thread-pool work item callback to use for
        /// this script thread, based on whether it is attaching to an existing
        /// interpreter or creating a new one.
        /// </summary>
        /// <param name="attach">
        /// Non-zero if the script thread is attaching to an existing
        /// interpreter; otherwise, zero.
        /// </param>
        /// <returns>
        /// The work item callback to use.
        /// </returns>
        private WaitCallback GetWaitCallback(
            bool attach
            )
        {
            return attach ?
                (WaitCallback)AttachThreadStart :
                (WaitCallback)CreateThreadStart;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the parameterized thread-start delegate to use
        /// for this script thread, based on whether it is attaching to an
        /// existing interpreter or creating a new one.
        /// </summary>
        /// <param name="attach">
        /// Non-zero if the script thread is attaching to an existing
        /// interpreter; otherwise, zero.
        /// </param>
        /// <returns>
        /// The thread-start delegate to use.
        /// </returns>
        private ParameterizedThreadStart GetThreadStart(
            bool attach
            )
        {
            return attach ?
                (ParameterizedThreadStart)AttachThreadStart :
                (ParameterizedThreadStart)CreateThreadStart;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits a diagnostic trace indicating that the specified
        /// variable in the specified interpreter was not changed.
        /// </summary>
        /// <param name="threadName">
        /// The name of the script thread emitting the trace.
        /// </param>
        /// <param name="varName">
        /// The name of the variable that was not changed.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter containing the variable.
        /// </param>
        private static void EmitNotChangedTrace(
            string threadName,
            string varName,
            Interpreter interpreter
            )
        {
            TraceOps.DebugTrace(interpreter, String.Format(
                "{0}: variable {1} in interpreter {2} was not changed",
                FormatOps.WrapOrNull(threadName),
                FormatOps.WrapOrNull(varName),
                FormatOps.InterpreterNoThrow(interpreter)),
                typeof(ScriptThread).Name,
                TracePriority.ScriptDebug, 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method implements the thread entry point used when this script
        /// thread creates and owns its own interpreter, evaluating the startup
        /// script and/or waiting on the event variable as configured.
        /// </summary>
        /// <param name="obj">
        /// The state object passed to the thread entry point; it is not used.
        /// </param>
        private void CreateThreadStart(
            object obj
            )
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                IEnumerable<string> args;
                IHost host;
                CreateFlags createFlags;
                HostCreateFlags hostCreateFlags;
                InitializeFlags initializeFlags;
                ScriptFlags scriptFlags;
                InterpreterFlags interpreterFlags;
                bool quiet;
                bool noBackgroundError;
                bool useSelf;
                bool useActiveStack;

#if THREADING
                bool usePool;
                bool purgeGlobal;
#endif

                EventWaitHandle startEvent;
                IScript script;
                string varName;

                lock (syncRoot)
                {
                    args = this.args;
                    host = this.host;
                    createFlags = this.createFlags;
                    hostCreateFlags = this.hostCreateFlags;
                    initializeFlags = this.initializeFlags;
                    scriptFlags = this.scriptFlags;
                    interpreterFlags = this.interpreterFlags;
                    quiet = this.quiet;
                    noBackgroundError = this.noBackgroundError;
                    useSelf = this.useSelf;
                    useActiveStack = this.useActiveStack;

#if THREADING
                    usePool = this.usePool;
                    purgeGlobal = this.purgeGlobal;
#endif

                    startEvent = this.startEvent;
                    script = this.script;
                    varName = this.varName;
                }

#if THREADING
                Interpreter purgeInterpreter = null;
#endif

                bool setEvent = false;
                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    //
                    // BUGBUG: If this interpreter creation takes too long, the
                    //         thread that started this thread will timeout and
                    //         dispose of this object.  Depending on the timing
                    //         of that disposal, the newly created interpreter
                    //         may end up being disposed as well (i.e. if the
                    //         "interpreter" field is updated before our parent
                    //         thread calls Dispose).  Technically, this thread
                    //         creation process contains several potential race
                    //         conditions; however, they are well-known and can
                    //         now be easily detected by either thread.
                    //
                    using (Interpreter interpreter = Interpreter.Create(
                            args, createFlags, hostCreateFlags, initializeFlags,
                            scriptFlags, interpreterFlags, host, ref result))
                    {
                        if (interpreter != null)
                        {
#if THREADING
                            purgeInterpreter = interpreter;
#endif

                            interpreter.Owner = this;
                            interpreter.Quiet = quiet;

                            interpreter.SetNoBackgroundError(
                                noBackgroundError);

                            lock (syncRoot)
                            {
                                this.interpreter = interpreter;
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }

                        if (startEvent != null)
                        {
                            setEvent = ThreadOps.SetEvent(startEvent);

                            if (!setEvent)
                            {
                                result = "failed to set start event";
                                code = ReturnCode.Error;

                                goto done;
                            }
                        }

                        if (useSelf)
                        {
                            code = FixupReturnValue(
                                interpreter, null, null,
                                DefaultObjectOptionType,
                                scriptThreadObjectName,
                                ObjectFlags.NoDispose, this,
                                true, false, ref result);

                            if (code != ReturnCode.Ok)
                                goto done;
                        }

                        if (useActiveStack)
                            GlobalState.PushActiveInterpreter(interpreter);

                        try
                        {
                            if (script != null)
                            {
                                code = EvaluateScript(
                                    interpreter, script,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;
                            }

                            if (varName != null)
                            {
                                bool notReady = false;
                                bool timedOut = false;
                                bool changed = false;

                                code = WaitVariable(
                                    interpreter, varName, 0, 0,
                                    ref notReady, ref timedOut,
                                    ref changed, ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                if (!changed)
                                {
                                    EmitNotChangedTrace(
                                        GetThreadName(this),
                                        varName, interpreter);
                                }
                            }
                        }
                        finally
                        {
                            if (useActiveStack)
                                GlobalState.PopActiveInterpreter();
                        }

                        //
                        // NOTE: At this point, if we successfully created an
                        //       interpreter in this method it will be disposed
                        //       when the containing using block was exited;
                        //       therefore, null out the interpreter contained
                        //       in the class field so that external callers do
                        //       not try to use it or dispose it [again].
                        //
                        lock (syncRoot)
                        {
                            this.interpreter = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }

            done:

                //
                // NOTE: Make sure that when we fail to create an interpreter
                //       we set the "start event" for this thread so that any
                //       threads waiting are released (i.e. we don't want them
                //       to wait forever).
                //
                if (!setEvent && (startEvent != null))
                {
                    /* IGNORED */
                    ThreadOps.SetEvent(startEvent);
                }

                //
                // NOTE: If we failed for any reason, complain about it now
                //       since we have no means of returning a direct result
                //       to our creator.
                //
                if (code != ReturnCode.Ok)
                    PrivateComplain(code, result);

#if THREADING
                //
                // NOTE: Finally, be sure to re-purge all context information
                //       about this thread from the context manager since the
                //       thread is now exiting.
                //
                ContextManager.Purge(purgeInterpreter, !usePool, purgeGlobal);
#endif
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method implements the thread entry point used when this script
        /// thread attaches to an existing interpreter, evaluating the startup
        /// script and/or waiting on the event variable as configured.
        /// </summary>
        /// <param name="obj">
        /// The state object passed to the thread entry point; it is not used.
        /// </param>
        private void AttachThreadStart(
            object obj
            )
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                bool quiet;
                bool noBackgroundError;
                bool useSelf;
                bool useActiveStack;

#if THREADING
                bool usePool;
                bool purgeGlobal;
#endif

                EventWaitHandle startEvent;
                IScript script;
                string varName;

                lock (syncRoot)
                {
                    quiet = this.quiet;
                    noBackgroundError = this.noBackgroundError;
                    useSelf = this.useSelf;
                    useActiveStack = this.useActiveStack;

#if THREADING
                    usePool = this.usePool;
                    purgeGlobal = this.purgeGlobal;
#endif

                    startEvent = this.startEvent;
                    script = this.script;
                    varName = this.varName;
                }

#if THREADING
                Interpreter purgeInterpreter = null;
#endif

                bool setEvent = false;
                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    Interpreter interpreter;

                    lock (syncRoot)
                    {
                        interpreter = this.interpreter;
                    }

                    if (interpreter != null)
                    {
#if THREADING
                        purgeInterpreter = interpreter;
#endif

                        interpreter.Owner = this;
                        interpreter.Quiet = quiet;

                        interpreter.SetNoBackgroundError(
                            noBackgroundError);
                    }
                    else
                    {
                        result = "invalid interpreter";
                        code = ReturnCode.Error;

                        goto done;
                    }

                    if (startEvent != null)
                    {
                        setEvent = ThreadOps.SetEvent(startEvent);

                        if (!setEvent)
                        {
                            result = "failed to set start event";
                            code = ReturnCode.Error;

                            goto done;
                        }
                    }

                    if (useSelf)
                    {
                        code = FixupReturnValue(
                            interpreter, null, null,
                            DefaultObjectOptionType,
                            scriptThreadObjectName,
                            ObjectFlags.NoDispose, this,
                            true, false, ref result);

                        if (code != ReturnCode.Ok)
                            goto done;
                    }

                    if (useActiveStack)
                        GlobalState.PushActiveInterpreter(interpreter);

                    try
                    {
                        if (script != null)
                        {
                            code = EvaluateScript(
                                interpreter, script,
                                ref result);

                            if (code != ReturnCode.Ok)
                                goto done;
                        }

                        if (varName != null)
                        {
                            bool notReady = false;
                            bool timedOut = false;
                            bool changed = false;

                            code = WaitVariable(
                                interpreter, varName, 0, 0,
                                ref notReady, ref timedOut,
                                ref changed, ref result);

                            if (code != ReturnCode.Ok)
                                goto done;

                            if (!changed)
                            {
                                EmitNotChangedTrace(
                                    GetThreadName(this),
                                    varName, interpreter);
                            }
                        }
                    }
                    finally
                    {
                        if (useActiveStack)
                            GlobalState.PopActiveInterpreter();
                    }

                    lock (syncRoot)
                    {
                        this.interpreter = null;
                    }
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }

            done:

                //
                // NOTE: Make sure that when we fail to attach an interpreter
                //       we set the "start event" for this thread so that any
                //       threads waiting are released (i.e. we don't want them
                //       to wait forever).
                //
                if (!setEvent && (startEvent != null))
                {
                    /* IGNORED */
                    ThreadOps.SetEvent(startEvent);
                }

                //
                // NOTE: If we failed for any reason, complain about it now
                //       since we have no means of returning a direct result
                //       to our creator.
                //
                if (code != ReturnCode.Ok)
                    PrivateComplain(code, result);

#if THREADING
                //
                // NOTE: Finally, be sure to re-purge all context information
                //       about this thread from the context manager since the
                //       thread is now exiting.
                //
                ContextManager.Purge(purgeInterpreter, !usePool, purgeGlobal);
#endif
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Disposal Helper Methods
        /// <summary>
        /// This method determines whether the underlying physical thread is
        /// dead, also returning its display name.
        /// </summary>
        /// <param name="threadName">
        /// Upon return, this contains the display name of the underlying thread.
        /// </param>
        /// <returns>
        /// True if the underlying thread is dead (or does not exist); otherwise,
        /// false.
        /// </returns>
        private bool IsDead(
            ref string threadName
            ) /* NO-LOCK */
        {
            threadName = FormatOps.DisplayThread(thread);
            return !ThreadOps.IsAlive(thread);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to gracefully shut down this script thread by
        /// signaling its event variable, waking it up, and then waiting for it
        /// to exit.
        /// </summary>
        /// <param name="value">
        /// The value to assign to the event variable when signaling.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum time to wait for the thread to exit, in milliseconds.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic tracing.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of a running thread as a failure; zero
        /// to treat it as success.
        /// </param>
        private void Shutdown(
            string value,
            int timeout,
            bool verbose,
            bool strict
            ) /* NO-LOCK */
        {
            //
            // NOTE: If the thread is already dead, there is nothing that
            //       we need to do.
            //
            string threadName = null;

            if (IsDead(ref threadName))
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Shutdown: script thread with [{0}] is already dead",
                        threadName), typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }

                return;
            }

            //
            // BUGFIX: First, try to nicely shutdown the thread by setting
            //         its associated variable and then waiting a bit for
            //         the thread to exit.
            //
            if (PrivateSignal(value) &&
                PrivateWakeUp() && PrivateWaitForEnd(timeout, strict))
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Shutdown: signal {0}, wake up, and join script " +
                        "thread with [{1}] and with timeout {2} success",
                        FormatOps.WrapOrNull(value), threadName, timeout),
                        typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "Shutdown: signal {0}, wake up, and join script " +
                    "thread with [{1}] and with timeout {2} failure",
                    FormatOps.WrapOrNull(value), threadName, timeout),
                    typeof(ScriptThread).Name,
                    TracePriority.ScriptThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of the interpreter hosted by this script thread,
        /// if any, logging any failure.
        /// </summary>
        private void DisposeInterpreter()
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter != null)
            {
                try
                {
                    interpreter.Dispose(); /* throw */
                    interpreter = null;
                }
                catch (Exception e)
                {
                    //
                    // NOTE: Nothing much we can do here
                    //       except log the failure.
                    //
                    TraceOps.DebugTrace(
                        e, typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadError);
                }
            }

            lock (syncRoot)
            {
                this.interpreter = interpreter;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this script thread has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// This method throws an exception if this script thread has been
        /// disposed and the interpreter is configured to throw on disposed.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(ScriptThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this script thread,
        /// attempting a graceful thread shutdown, disposing of the interpreter,
        /// and closing the event handles.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the <c>Dispose</c>
        /// method; zero if it is being called from the finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(ScriptThread).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: First, try graceful thread shutdown.
                    //
                    /* NO RESULT */
                    Shutdown(null, timeout, verbose, false);

                    //
                    // NOTE: Next, dispose of the interpreter.  This should
                    //       cause the ThreadStart method to exit if it has
                    //       not done so already.
                    //
                    /* NO RESULT */
                    DisposeInterpreter();

                    //
                    // NOTE: Finally, try to interrupt or abort the running
                    //       thread.
                    //
                    /* NO RESULT */
                    InterruptOrAbortThread(
                        thread, timeout, verbose, PrivateIsNoAbort());

                    //
                    // NOTE: Close the event handles that we created.
                    //
                    /* NO RESULT */
                    ThreadOps.CloseEvent(ref wakeUpEvent);

                    /* NO RESULT */
                    ThreadOps.CloseEvent(ref startEvent);
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
        /// This method releases all resources used by this script thread.
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
        /// Finalizes this script thread, releasing any unmanaged resources.
        /// </summary>
        ~ScriptThread()
        {
            Dispose(false);
        }
        #endregion
    }
}
