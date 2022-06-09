/*
 * Script.cs --
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
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Cmdlets
{
    [ObjectId("c01dc0d1-1ec8-4ec1-ad82-ad500ed86f33")]
    public abstract class Script : Cmdlet, IMaybeDisposed, IDisposable
    {
        #region Private Constants
        //
        // NOTE: By default, no console.
        //
        private static readonly bool DefaultConsole = false;

        //
        // NOTE: By default:
        //
        //       1. We want to initialize the script library.
        //       2. We want to throw an exception if disposed objects
        //          are accessed.
        //       3. We want to throw an exception if interpreter
        //          creation fails.
        //       4. We want to have only directories that actually
        //          exist in the auto-path.
        //       5. We want to provide a "safe" subset of commands.
        //
        private static readonly CreateFlags DefaultCreateFlags =
            CreateFlags.SafeEmbeddedUse;

        //
        // NOTE: By default:
        //
        //       1. We do not want to change the console title.
        //       2. We do not want to change the console icon.
        //       3. We do not want to intercept the Ctrl-C keypress.
        //
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.SafeEmbeddedUse;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        //
        // NOTE: These are the object flags to be used when adding the opaque
        //       object handle to the interpreter that will refer to the cmdlet
        //       instance associated with that interpreter.
        //
        public static readonly ObjectFlags CmdletObjectFlags =
            ObjectFlags.Locked | ObjectFlags.NoDispose;

        //
        // NOTE: This is the default [object] command option type used when
        //       creating command aliases (i.e. to the [object] command) and
        //       it determines which options will be supported by each such
        //       alias.
        //
        public static readonly ObjectOptionType CmdletObjectOptionType =
            ObjectOptionType.Default;

        //
        // NOTE: This is the opaque object handle name that will refer to the
        //       cmdlet instance associated with the interpreter being used by
        //       that same cmdlet instance.
        //
        public static readonly string CmdletObjectName = "__cmdlet";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private WriteCallback flagsCallback;
        private WriteCallback stateCallback;
        private WriteCallback parameterCallback;
        private TraceListener listener;
        private string preInitialize;
        private CreateFlags createFlags;
        private HostCreateFlags hostCreateFlags;
        private InitializeFlags initializeFlags;
        private ScriptFlags scriptFlags;
        private InterpreterFlags interpreterFlags;
        private EngineFlags engineFlags;
        private SubstitutionFlags substitutionFlags;
        private EventFlags eventFlags;
        private ExpressionFlags expressionFlags;
        private bool console;
        private bool force;
        private bool exceptions;
        private bool policies;
        private bool deny;
        private bool metaCommand;
        private string text;
        private IEnumerable<string> args;
        private Interpreter interpreter;
        private long token; /* NOTE: For [cmdlet] meta-command. */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Delegates
        //
        // NOTE: This is used to facilitate dynamically determing whether
        //       output should be sent to WriteWarning, WriteCommandDetail,
        //       WriteVerbose, or WriteDebug.
        //
        protected internal delegate void WriteCallback(string text);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        protected Script()
        {
            //
            // NOTE: Setup the default delegate used for writing detailed
            //       interpreter creation flags information (i.e. just prior
            //       to the interpreter being created).
            //
            flagsCallback = WriteDebug;

            //
            // NOTE: Setup the default delegate used for writing detailed
            //       cmdlet state/lifecycle information.
            //
            stateCallback = WriteVerbose;

            //
            // NOTE: Setup the default delegate used for writing detailed
            //       script parameter information.
            //
            parameterCallback = WriteVerbose;

            //
            // NOTE: Can we assume that the console host is available?
            //
            console = NeedConsole(DefaultConsole);

            //
            // NOTE: Setup our default trace listeners now.
            //
            SetupTraceListeners(true, console, false);

            //
            // NOTE: By default, no pre-init script is needed.
            //
            preInitialize = null;

            //
            // NOTE: By default, use our default creation, initialization,
            //       script, and interpreter behavior flags.
            //
            createFlags = DefaultCreateFlags;
            hostCreateFlags = DefaultHostCreateFlags;
            initializeFlags = InitializeFlags.Default;
            scriptFlags = ScriptFlags.Default;
            interpreterFlags = InterpreterFlags.Default;

            //
            // NOTE: By default, we do not want any special evaluation flags.
            //
            engineFlags = EngineFlags.None;

            //
            // NOTE: By default, we want all the substitution flags.
            //
            substitutionFlags = SubstitutionFlags.Default;

            //
            // NOTE: By default, we want to handle events targeted at the
            //       engine.
            //
            eventFlags = EventFlags.Default;

            //
            // NOTE: By default, we want all the expression flags.
            //
            expressionFlags = ExpressionFlags.Default;

            //
            // NOTE: By default, we do not want to force processing to
            //       continue.
            //
            force = false;

            //
            // NOTE: By default, we do not want to allow "exceptional"
            //       (non-Ok) success return codes.
            //
            exceptions = false;

            //
            // NOTE: By default, we do not add the custom command execution
            //       policies.
            //
            policies = false;

            //
            // NOTE: By default, we do not want to deny command execution for
            //       commands approved by the built-in security policies.
            //
            deny = false;

            //
            // NOTE: By default, we do not want the [cmdlet] meta-command to
            //       be available for use by evaluated expressions, scripts,
            //       etc.
            //
            metaCommand = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposing
        {
            get { return false; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(Script).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    ReturnCode code;
                    Result error = null;

                    code = DisposeInterpreter(ref error);

                    if (code != ReturnCode.Ok)
                        WriteErrorRecordNoThrow(code, error);

                    SetupTraceListeners(false, console, false);
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
        ~Script()
        {
            Dispose(false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cmdlet Parameters
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Text
            )]
        [Alias(
            _Constants.Parameter.Expression,
            _Constants.Parameter.String,
            _Constants.Parameter.Script,
            _Constants.Parameter.File,
            _Constants.Parameter.FileName
            )]
        public string Text
        {
            get { CheckDisposed(); return text; }
            set { CheckDisposed(); text = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Args
            )]
        public string Args
        {
            get
            {
                CheckDisposed();

                if (args == null)
                    return null;

                return new StringList(args).ToString();
            }
            set
            {
                CheckDisposed();

                StringList list;
                Result error = null;

                list = StringList.FromString(value, ref error);

                if (list == null)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException((Result)error),
                        _Constants.ErrorId.CouldNotSetArguments,
                        ErrorCategory.NotSpecified, null));
                }

                args = list;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.PreInitialize
            )]
        public string PreInitialize
        {
            get { CheckDisposed(); return preInitialize; }
            set { CheckDisposed(); preInitialize = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.CreateFlags
            )]
        public CreateFlags CreateFlags
        {
            get { CheckDisposed(); return createFlags; }
            set { CheckDisposed(); createFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.HostCreateFlags
            )]
        public HostCreateFlags HostCreateFlags
        {
            get { CheckDisposed(); return hostCreateFlags; }
            set { CheckDisposed(); hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.InitializeFlags
            )]
        public InitializeFlags InitializeFlags
        {
            get { CheckDisposed(); return initializeFlags; }
            set { CheckDisposed(); initializeFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.ScriptFlags
            )]
        public ScriptFlags ScriptFlags
        {
            get { CheckDisposed(); return scriptFlags; }
            set { CheckDisposed(); scriptFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.InterpreterFlags
            )]
        public InterpreterFlags InterpreterFlags
        {
            get { CheckDisposed(); return interpreterFlags; }
            set { CheckDisposed(); interpreterFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.EngineFlags
            )]
        public EngineFlags EngineFlags
        {
            get { CheckDisposed(); return engineFlags; }
            set { CheckDisposed(); engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.SubstitutionFlags
            )]
        public SubstitutionFlags SubstitutionFlags
        {
            get { CheckDisposed(); return substitutionFlags; }
            set { CheckDisposed(); substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.EventFlags
            )]
        public EventFlags EventFlags
        {
            get { CheckDisposed(); return eventFlags; }
            set { CheckDisposed(); eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.ExpressionFlags
            )]
        public ExpressionFlags ExpressionFlags
        {
            get { CheckDisposed(); return expressionFlags; }
            set { CheckDisposed(); expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Console
            )]
        public SwitchParameter Console
        {
            get { CheckDisposed(); return console; }
            set
            {
                CheckDisposed();

                Result error = null;

                if ((SetupTraceListeners(
                        false, console, true, ref error) == ReturnCode.Ok) &&
                    (value && SetupTraceListeners(
                        true, value, true, ref error) == ReturnCode.Ok))
                {
                    console = value;
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException((Result)error),
                        _Constants.ErrorId.CouldNotSetupTraceListeners,
                        ErrorCategory.NotSpecified, null));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Unsafe
            )]
        public SwitchParameter Unsafe
        {
            get
            {
                CheckDisposed();

                return !Utility.HasFlags(createFlags, CreateFlags.Safe, true);
            }
            set
            {
                CheckDisposed();

                //
                // NOTE: We disallow modifying the creation flags if
                //       the interpreter has already been created.
                //
                if (interpreter == null)
                {
                    if (value)
                        createFlags &= ~CreateFlags.SafeAndHideUnsafe;
                    else
                        createFlags |= CreateFlags.SafeAndHideUnsafe;
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException(),
                        _Constants.ErrorId.AlreadyCreatedInterpreter,
                        ErrorCategory.NotSpecified, null));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Standard
            )]
        public SwitchParameter Standard
        {
            get
            {
                CheckDisposed();

                return Utility.HasFlags(
                    createFlags, CreateFlags.Standard, true);
            }
            set
            {
                CheckDisposed();

                //
                // NOTE: We disallow modifying the creation flags if
                //       the interpreter has already been created.
                //
                if (interpreter == null)
                {
                    if (value)
                        createFlags |= CreateFlags.StandardAndHideNonStandard;
                    else
                        createFlags &= ~CreateFlags.StandardAndHideNonStandard;
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException(),
                        _Constants.ErrorId.AlreadyCreatedInterpreter,
                        ErrorCategory.NotSpecified, null));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Force
            )]
        public SwitchParameter Force
        {
            get { CheckDisposed(); return force; }
            set { CheckDisposed(); force = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Exceptions
            )]
        public SwitchParameter Exceptions
        {
            get { CheckDisposed(); return exceptions; }
            set { CheckDisposed(); exceptions = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Policies
            )]
        public SwitchParameter Policies
        {
            get { CheckDisposed(); return policies; }
            set { CheckDisposed(); policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.Deny
            )]
        public SwitchParameter Deny
        {
            get { CheckDisposed(); return deny; }
            set { CheckDisposed(); deny = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = _Constants.HelpMessage.MetaCommand
            )]
        public SwitchParameter MetaCommand
        {
            get { CheckDisposed(); return metaCommand; }
            set { CheckDisposed(); metaCommand = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Properties
        protected internal WriteCallback FlagsCallback
        {
            get { return flagsCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal WriteCallback StateCallback
        {
            get { return stateCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal WriteCallback ParameterCallback
        {
            get { return parameterCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal TraceListener Listener
        {
            get { return listener; }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal Interpreter Interpreter
        {
            get { return interpreter; }
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal LongList Tokens
        {
            get { return new LongList(new long[] { token }); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static bool NeedConsole(
            bool @default
            )
        {
            //
            // HACK: By default, assume that a console-based host is not
            //       available.  Then, attempt to check and see if the user
            //       believes that one is available.  We use this very clumsy
            //       method because PowerShell does not seem to expose an easy
            //       way for us to determine if we have a console-like host
            //       available to output diagnostic [and other] information to.
            //
            try
            {
                if (@default)
                {
                    if (Utility.GetEnvironmentVariable(
                            EnvVars.NoConsole, true, false) != null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (Utility.GetEnvironmentVariable(
                            EnvVars.Console, true, false) != null)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return @default;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Helper Methods
        #region Trace Listener Helper Methods
        protected ReturnCode SetupTraceListeners(
            bool setup,
            bool console,
            bool strict
            )
        {
            Result error = null;

            return SetupTraceListeners(setup, console, strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SetupTraceListeners(
            bool setup,
            bool console,
            bool strict,
            ref Result error
            )
        {
            try
            {
                if (setup)
                {
                    //
                    // NOTE: Add our trace listener to the collections for
                    //       trace and debug output.
                    //
                    if (listener == null)
                    {
                        listener = Utility.NewTraceListener(
                            Utility.GetTraceListenerType(console), null,
                            ref error);

                        if (listener != null)
                        {
                            /* IGNORED */
                            Utility.AddTraceListener(listener, false);

                            /* IGNORED */
                            Utility.AddTraceListener(listener, true);

                            return ReturnCode.Ok; // NOTE: Success.
                        }
                    }
                    else if (strict)
                    {
                        error = "trace listeners already setup";
                    }
                    else
                    {
                        return ReturnCode.Ok; // NOTE: Fake success.
                    }
                }
                else
                {
                    //
                    // NOTE: Remove and dispose our trace listeners now.
                    //
                    if (listener != null)
                    {
                        /* IGNORED */
                        Utility.RemoveTraceListener(listener, true);

                        /* IGNORED */
                        Utility.RemoveTraceListener(listener, false);

                        listener.Dispose();
                        listener = null;

                        return ReturnCode.Ok; // NOTE: Success.
                    }
                    else if (strict)
                    {
                        error = "trace listeners not setup";
                    }
                    else
                    {
                        return ReturnCode.Ok; // NOTE: Fake success.
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Helper Methods
        protected void DisposeInterpreter()
        {
            ReturnCode code;
            Result error = null;

            code = DisposeInterpreter(ref error);

            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: Report that the interpreter has been disposed.
                //
                WriteState(_Constants.Verbose.InterpreterDisposed);
            }
            else
            {
                //
                // NOTE: Report that the interpreter disposal error.
                //
                WriteErrorRecord(code, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected ReturnCode DisposeInterpreter(
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                try
                {
                    //
                    // NOTE: Ok, now dispose and clear the interpreter.
                    //
                    interpreter.Dispose(); /* throw */
                    interpreter = null;
                }
                catch (Exception e)
                {
                    error = e;
                    code = ReturnCode.Error;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Meta-Command Helper Methods
        protected virtual IClientData GetMetaCommandClientData()
        {
            return new ClientData(this);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ICommand GetMetaCommand(
            Interpreter interpreter
            )
        {
            Type type = typeof(_Commands.Cmdlet);
            IClientData clientData = GetMetaCommandClientData();
            CommandFlags commandFlags = CommandFlags.None;

            if ((interpreter != null) && interpreter.IsSafe())
                commandFlags |= CommandFlags.Hidden;

            return new _Commands.Cmdlet(new CommandData(
                type.Name.ToLowerInvariant(), null, null, clientData,
                type.FullName, commandFlags, null, 0));
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode AddMetaCommand(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (token != 0)
                return ReturnCode.Ok;

            ICommand command = GetMetaCommand(interpreter);

            if (command == null)
                return ReturnCode.Ok;

            ReturnCode code;
            Result result = null;

            code = interpreter.AddCommand(
                command, null, ref token, ref result);

            if (code != ReturnCode.Ok)
                error = result;

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal virtual ReturnCode RemoveMetaCommand(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (token == 0)
                return ReturnCode.Ok;

            ReturnCode code;
            Result result = null;

            code = interpreter.RemoveCommand(
                token, null, ref result);

            if (code != ReturnCode.Ok)
                error = result;

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Error Helper Methods (no-throw)
        protected ReturnCode WriteErrorRecordNoThrow(
            ReturnCode code,
            Result result
            )
        {
            Result error = null;

            return WriteErrorRecordNoThrow(code, result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode WriteErrorRecordNoThrow(
            ReturnCode code,
            Result result,
            ref Result error
            )
        {
            try
            {
                WriteErrorRecord(code, result); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;

                try
                {
                    Trace.WriteLine(Utility.FormatResult(
                        code, result)); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Verbose Helper Methods (no-throw)
        protected ReturnCode WriteVerboseNoThrow(
            string text
            )
        {
            Result error = null;

            return WriteVerboseNoThrow(text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode WriteVerboseNoThrow(
            string text,
            ref Result error
            )
        {
            try
            {
                WriteVerbose(text); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;

                try
                {
                    Trace.WriteLine(text); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Create Flags Helper Methods (BeginProcessing)
        protected ReturnCode WriteFlags(
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags
            )
        {
            return WriteFlags(
                flagsCallback, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, interpreterFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        protected ReturnCode WriteFlags(
            WriteCallback callback,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags
            )
        {
            Result error = null;

            return WriteFlags(
                callback, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, interpreterFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode WriteFlags(
            WriteCallback callback,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            ref Result error
            )
        {
            if (callback != null)
            {
                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.CreateFlags,
                    _Constants.Parameter.CreateFlags,
                    createFlags.ToString()));

                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.HostCreateFlags,
                    _Constants.Parameter.HostCreateFlags,
                    hostCreateFlags.ToString()));

                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.InitializeFlags,
                    _Constants.Parameter.InitializeFlags,
                    initializeFlags.ToString()));

                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.ScriptFlags,
                    _Constants.Parameter.ScriptFlags,
                    scriptFlags.ToString()));

                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.InterpreterFlags,
                    _Constants.Parameter.InterpreterFlags,
                    interpreterFlags.ToString()));

                //
                // NOTE: If we got this far, we totally succeeded.
                //
                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid write callback";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write State Helper Methods (BeginProcessing / EndProcessing)
        protected ReturnCode WriteState(string text)
        {
            return WriteState(stateCallback, text);
        }

        ///////////////////////////////////////////////////////////////////////

        protected ReturnCode WriteState(
            WriteCallback callback,
            string text
            )
        {
            Result error = null;

            return WriteState(callback, text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode WriteState(
            WriteCallback callback,
            string text,
            ref Result error
            )
        {
            if (callback != null)
            {
                callback(text);

                //
                // NOTE: If we got this far, we totally succeeded.
                //
                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid write callback";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Parameters Helper Methods (ProcessRecord)
        protected ReturnCode WriteParameters()
        {
            return WriteParameters(parameterCallback);
        }

        ///////////////////////////////////////////////////////////////////////

        protected ReturnCode WriteParameters(
            WriteCallback callback
            )
        {
            Result error = null;

            return WriteParameters(callback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode WriteParameters(
            WriteCallback callback,
            ref Result error
            )
        {
            if (callback != null)
            {
                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.EngineFlags,
                    _Constants.Parameter.EngineFlags,
                    EngineFlags.ToString()));

                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.SubstitutionFlags,
                    _Constants.Parameter.SubstitutionFlags,
                    SubstitutionFlags.ToString()));

                callback(FormatOps.FlagsMessage(
                    _Constants.Prefix.EventFlags,
                    _Constants.Parameter.EventFlags,
                    EventFlags.ToString()));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Console, false, Console));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Unsafe, false, Unsafe));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Standard, false, Standard));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Force, false, Force));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Exceptions, true, Exceptions));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Policies, true, Policies));

                callback(FormatOps.EnabledMessage(
                    _Constants.Parameter.Deny, true, Deny));

                callback(String.Format(
                    _Constants.Verbose.TraceListener, listener));

                //
                // NOTE: If we got this far, we totally succeeded.
                //
                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid write callback";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Helper Methods (ProcessRecord)
        protected virtual void WriteObjectRecord(
            ReturnCode code,
            Result result
            )
        {
            if (interpreter != null)
            {
                IObject @object = null;

                if (interpreter.GetObject(result, LookupFlags.NoVerbose,
                        ref @object) == ReturnCode.Ok)
                {
                    //
                    // NOTE: The result is an opaque object handle, write
                    //       the underlying object itself to the pipeline.
                    //
                    WriteObject(@object.Value);

                    return;
                }
            }

            //
            // NOTE: *FALLBACK* Just write the result (as a string) to the
            //       pipeline.
            //
            WriteObject((string)result);
        }

        ///////////////////////////////////////////////////////////////////////

        protected internal virtual void WriteErrorRecord(
            ReturnCode code,
            Result result
            )
        {
            WriteError(new ErrorRecord(
                new ScriptException(code, result),
                _Constants.ErrorId.ScriptError,
                ErrorCategory.NotSpecified,
                text));
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void WriteVerboseRecord(
            Type type,
            string noun
            )
        {
            WriteVerbose(FormatOps.ScriptMessage(type, noun, text));
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode WriteVerbosePipelineStopping(
            ReturnCode code,
            Result result
            )
        {
            //
            // NOTE: Report that the pipeline processing has been stopped.
            //
            return WriteVerboseNoThrow(String.Format(
                _Constants.Verbose.PipelineStopping,
                Utility.FormatResult(code, result)));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Precondition Checking Methods (ProcessRecord)
        protected virtual bool CanProcessRecord(
            ref ErrorRecord errorRecord
            )
        {
            if (interpreter != null)
            {
                if (!interpreter.Disposed)
                {
                    if (text != null)
                    {
                        //
                        // NOTE: Yes, we have all the prerequisites for
                        //       record processing.
                        //
                        return true;
                    }
                    else
                    {
                        errorRecord = new ErrorRecord(
                            new ScriptException(),
                            _Constants.ErrorId.InvalidScript,
                            ErrorCategory.NotSpecified,
                            text);
                    }
                }
                else
                {
                    errorRecord = new ErrorRecord(
                        new ScriptException(),
                        _Constants.ErrorId.DisposedInterpreter,
                        ErrorCategory.NotSpecified,
                        text);
                }
            }
            else
            {
                errorRecord = new ErrorRecord(
                    new ScriptException(),
                    _Constants.ErrorId.InvalidInterpreter,
                    ErrorCategory.NotSpecified,
                    text);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Postcondition Checking Methods (ProcessRecord)
        protected virtual bool IsSuccess(
            ReturnCode code
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Engine Helper Methods (ProcessRecord)
        protected virtual ReturnCode EvaluateExpression(
            ref Result result
            )
        {
            return Engine.EvaluateExpression(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateScript(
            ref Result result
            )
        {
            return Engine.EvaluateScript(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateFile(
            ref Result result
            )
        {
            return Engine.EvaluateFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteString(
            ref Result result
            )
        {
            return Engine.SubstituteString(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteFile(
            ref Result result
            )
        {
            return Engine.SubstituteFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Management.Automation.Cmdlet Overrides
        public override string GetResourceString(
            string baseName,
            string resourceId
            )
        {
            CheckDisposed();

            //
            // NOTE: Report that we are entering this method.
            //
            WriteVerbose(String.Format(
                _Constants.Verbose.Entered,
                MethodBase.GetCurrentMethod().Name));

            try
            {
                //
                // TODO: Change this to use the GetString method of the
                //       interpreter?
                //
                return base.GetResourceString(baseName, resourceId);
            }
            finally
            {
                //
                // NOTE: Report that we are exiting this method.
                //
                WriteVerbose(String.Format(
                    _Constants.Verbose.Exited,
                    MethodBase.GetCurrentMethod().Name));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void BeginProcessing()
        {
            CheckDisposed(); /* EXEMPT */

            //
            // NOTE: Report that we are entering this method.
            //
            WriteVerbose(String.Format(
                _Constants.Verbose.Entered,
                MethodBase.GetCurrentMethod().Name));

            try
            {
                //
                // NOTE: Get the effective interpreter creation flags from the
                //       environment, etc.
                //
                createFlags = Interpreter.GetStartupCreateFlags(args,
                    createFlags, OptionOriginFlags.Standard, console, true);

                hostCreateFlags = Interpreter.GetStartupHostCreateFlags(args,
                    hostCreateFlags, OptionOriginFlags.Standard, console, true);

                //
                // NOTE: Show the exact interpreter creation flags.  This may
                //       cause a confirmation prompt to be displayed under
                //       certain circumstances (i.e. if the cmdlet was invoked
                //       with the "-debug" option).
                //
                WriteFlags(
                    createFlags, hostCreateFlags, initializeFlags, scriptFlags,
                    interpreterFlags);

                //
                // NOTE: Check if "safe" mode has been enabled for the
                //       interpreter to be created.
                //
                if (Utility.HasFlags(createFlags, CreateFlags.Safe, true))
                    WriteState(_Constants.Verbose.SafeMode);
                else
                    WriteState(_Constants.Verbose.UnsafeMode);

                //
                // NOTE: If enabled, we use a custom command execution policy
                //       that prompts the user to allow potentially "unsafe"
                //       commands to be executed.
                //
                PolicyList localPolicies = null;

                if (policies)
                {
                    localPolicies = new PolicyList(
                        new ExecuteCallback[] {
                            _Policies._Cmdlet.PolicyCallback
                    });

                    WriteState(_Constants.Verbose.PoliciesEnabled);
                }
                else
                {
                    WriteState(_Constants.Verbose.PoliciesDisabled);
                }

                //
                // NOTE: Show the configured pre-init script, if any.
                //
                if (preInitialize != null)
                {
                    WriteState(String.Format(
                        _Constants.Verbose.PreInitializeScript,
                        preInitialize));
                }
                else
                {
                    WriteState(_Constants.Verbose.PreInitializeNone);
                }

                //
                // NOTE: Create an interpreter using the creation flags and
                //       policies that were set by the user (or the defaults).
                //       We also need a reference to this object to handle
                //       policy decisions in our custom policy callback during
                //       interpreter creation and initialization.
                //
                Result result = null;

                interpreter = Interpreter.Create(
                    args, createFlags, hostCreateFlags, initializeFlags,
                    scriptFlags, interpreterFlags, null, this, null, null,
                    localPolicies, null, preInitialize, null, null,
                    ref result); /* throw */

                if (interpreter != null)
                {
                    WriteState(_Constants.Verbose.InterpreterCreated);

                    ReturnCode code = Interpreter.ProcessStartupOptions(
                        interpreter, args, createFlags,
                        OptionOriginFlags.Standard, console, true,
                        ref result);

                    if (code == ReturnCode.Ok)
                    {
                        code = Utility.FixupReturnValue(
                            interpreter, null, CmdletObjectFlags,
                            null, CmdletObjectOptionType, CmdletObjectName,
                            this, true, false, ref result);
                    }

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: If the policy object property for the new
                        //       interpreter still refers to this instance,
                        //       reset it now.  From this point onward, the
                        //       named opaque object handle we just added
                        //       will be used by the cmdlet policy engine
                        //       to refer back to this cmdlet instance.
                        //
                        if (Object.ReferenceEquals(
                                interpreter.PolicyObject, this))
                        {
                            interpreter.PolicyObject = null;
                        }

                        WriteState(_Constants.Verbose.InterpreterSetup);

                        if (metaCommand)
                        {
                            code = AddMetaCommand(interpreter, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                WriteState(_Constants.Verbose.MetaCommandAdded);
                            }
                            else
                            {
                                ThrowTerminatingError(new ErrorRecord(
                                    new ScriptException(code, result),
                                    _Constants.ErrorId.CouldNotAddMetaCommand,
                                    ErrorCategory.NotSpecified, null));
                            }
                        }
                    }
                    else
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            new ScriptException(code, result),
                            _Constants.ErrorId.CouldNotSetupInterpreter,
                            ErrorCategory.NotSpecified, null));
                    }
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException((Result)result),
                        _Constants.ErrorId.CouldNotCreateInterpreter,
                        ErrorCategory.NotSpecified, null));
                }
            }
            catch (Exception e)
            {
                DisposeInterpreter();

                ThrowTerminatingError(new ErrorRecord(
                    new ScriptException((Result)e),
                    _Constants.ErrorId.CreateException,
                    ErrorCategory.NotSpecified, null));
            }
            finally
            {
                //
                // NOTE: Report that we are exiting this method.
                //
                WriteVerbose(String.Format(
                    _Constants.Verbose.Exited,
                    MethodBase.GetCurrentMethod().Name));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void EndProcessing()
        {
            CheckDisposed(); /* EXEMPT */

            //
            // NOTE: Report that we are entering this method.
            //
            WriteVerbose(String.Format(
                _Constants.Verbose.Entered,
                MethodBase.GetCurrentMethod().Name));

            try
            {
                ReturnCode code;
                Result error = null;

                //
                // NOTE: Dispose of the interpreter now.
                //
                code = DisposeInterpreter(ref error);

                //
                // NOTE: Report that the interpreter has been disposed.
                //
                if (code == ReturnCode.Ok)
                    WriteState(_Constants.Verbose.InterpreterDisposed);

                //
                // NOTE: Reset the static confirmation data because we do not
                //       currently want it to "stick" between interpreters.
                //
                _Policies._Cmdlet.Reset();

                //
                // NOTE: Report that the cmdlet policy has been reset.
                //
                WriteState(_Constants.Verbose.PoliciesReset);

                //
                // NOTE: If the interpreter could not be disposed properly,
                //       throw an error now.
                //
                if (code != ReturnCode.Ok)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException(code, error),
                        _Constants.ErrorId.CouldNotDisposeInterpreter,
                        ErrorCategory.NotSpecified, text));
                }
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ScriptException((Result)e),
                    _Constants.ErrorId.DisposeException,
                    ErrorCategory.NotSpecified, null));
            }
            finally
            {
                //
                // NOTE: Report that we are exiting this method.
                //
                WriteVerbose(String.Format(
                    _Constants.Verbose.Exited,
                    MethodBase.GetCurrentMethod().Name));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void StopProcessing()
        {
            CheckDisposed(); /* EXEMPT */

            //
            // NOTE: Report that we are entering this method.
            //
            WriteVerbose(String.Format(
                _Constants.Verbose.Entered,
                MethodBase.GetCurrentMethod().Name));

            try
            {
                if (interpreter != null)
                {
                    ReturnCode code;
                    Result error = null;

                    code = Engine.CancelEvaluate(
                        interpreter, null, CancelFlags.UnwindAndNotify,
                        ref error);

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Report that processing has been stopped.  This
                        //       may internally throw an exception because
                        //       the WriteVerbose method is technically (i.e.
                        //       according to the public documentation) not
                        //       allowed to be called from inside this method
                        //       (i.e. StopProcessing).  The exact behavior may
                        //       have been changed between version 1.0 and 2.0
                        //       of PowerShell.
                        //
                        WriteVerboseNoThrow(
                            _Constants.Verbose.ProcessingStopped);

                        //
                        // NOTE: Dispose of the interpreter now.
                        //
                        code = DisposeInterpreter(ref error);

                        //
                        // NOTE: Report that the interpreter has been disposed.
                        //
                        if (code == ReturnCode.Ok)
                            WriteVerboseNoThrow(
                                _Constants.Verbose.InterpreterDisposed);

                        //
                        // NOTE: Reset the static confirmation data because we
                        //       do not currently want it to "stick" between
                        //       interpreters.
                        //
                        _Policies._Cmdlet.Reset();

                        //
                        // NOTE: Report that the cmdlet policy has been reset.
                        //
                        WriteVerboseNoThrow(_Constants.Verbose.PoliciesReset);

                        //
                        // NOTE: If the interpreter could not be disposed
                        //       properly, throw an error now.
                        //
                        if (code != ReturnCode.Ok)
                        {
                            ThrowTerminatingError(new ErrorRecord(
                                new ScriptException(code, error),
                                _Constants.ErrorId.CouldNotDisposeInterpreter,
                                ErrorCategory.NotSpecified, text));
                        }
                    }
                    else
                    {
                        ThrowTerminatingError(new ErrorRecord(
                            new ScriptException(code, error),
                            _Constants.ErrorId.CancelError,
                            ErrorCategory.NotSpecified, text));
                    }
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ScriptException(),
                        _Constants.ErrorId.InvalidInterpreter,
                        ErrorCategory.NotSpecified, text));
                }
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ScriptException((Result)e),
                    _Constants.ErrorId.CancelException,
                    ErrorCategory.NotSpecified, text));
            }
            finally
            {
                //
                // NOTE: Report that we are exiting this method.
                //
                WriteVerbose(String.Format(
                    _Constants.Verbose.Exited,
                    MethodBase.GetCurrentMethod().Name));
            }
        }
        #endregion
    }
}
