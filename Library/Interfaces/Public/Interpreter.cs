/*
 * Interpreter.cs --
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
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

#if CAS_POLICY
using System.Security.Policy;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents an Eagle script interpreter, the central
    /// object that owns the state required to parse and evaluate scripts,
    /// including its commands, procedures, variables, namespaces, host, and
    /// the various limits and callbacks that govern execution.  It composes
    /// the lifetime tracking provided by <see cref="IMaybeDisposed" /> and
    /// exposes the public surface used by hosting applications to configure
    /// and interact with an interpreter.  See <c>core_language.md</c> for an
    /// overview of how scripts are evaluated.
    /// </summary>
    [ObjectId("eb34a7f9-199e-4bbb-90b9-7ab5334bcc38")]
    public interface IInterpreter : IMaybeDisposed
    {
        ///////////////////////////////////////////////////////////////////////
        // OBJECT IDENTITY & AFFINITY
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the unique identifier assigned to this interpreter.
        /// </summary>
        //
        // WARNING: THESE PROPERTIES ARE NOT GUARANTEED TO BE ACCURATE OR USEFUL.
        //
        long Id { get; }
        /// <summary>
        /// Gets the identifier of the group, if any, that this interpreter
        /// belongs to.
        /// </summary>
        long GroupId { get; }
        /// <summary>
        /// Gets the number of interpreters that had been created at the time
        /// this interpreter was created.
        /// </summary>
        long CreateCount { get; }
        /// <summary>
        /// Gets the number of times this interpreter has been disposed.
        /// </summary>
        long DisposeCount { get; }
        /// <summary>
        /// Gets the total number of operations performed by this interpreter.
        /// </summary>
        long OperationCount { get; }
        /// <summary>
        /// Gets the total number of commands executed by this interpreter.
        /// </summary>
        long CommandCount { get; }
        /// <summary>
        /// Gets the total number of times unknown command handling has been
        /// triggered by this interpreter.
        /// </summary>
        long UnknownCount { get; }
        /// <summary>
        /// Gets the managed thread identifier of the thread that created this
        /// interpreter.
        /// </summary>
        long ThreadId { get; }
        /// <summary>
        /// Gets the thread that created this interpreter.
        /// </summary>
        Thread Thread { get; }

#if SHELL
        /// <summary>
        /// Gets the thread that is running the interactive loop for this
        /// interpreter, if any.
        /// </summary>
        Thread InteractiveThread { get; }
#endif

        /// <summary>
        /// Gets a value indicating whether thread abort handling is disabled
        /// for this interpreter.
        /// </summary>
        bool NoThreadAbort { get; }

        /// <summary>
        /// Gets the event used to signal when a variable in this interpreter
        /// changes.
        /// </summary>
        EventWaitHandle VariableEvent { get; }
        /// <summary>
        /// Gets the event used to signal when setup of this interpreter is
        /// complete.
        /// </summary>
        EventWaitHandle SetupEvent { get; }

        ///////////////////////////////////////////////////////////////////////
        // SCRIPT & EXECUTION LIMITS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the maximum number of operations that this interpreter
        /// may perform before execution is halted.
        /// </summary>
        long OperationLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of commands that this interpreter
        /// may execute before execution is halted.
        /// </summary>
        long CommandLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of times unknown command handling
        /// may be triggered before execution is halted.
        /// </summary>
        long UnknownLimit { get; set; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the maximum number of name/value pairs permitted when
        /// processing a dictionary.
        /// </summary>
        long DictionaryPairLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum nesting depth permitted when processing a
        /// dictionary.
        /// </summary>
        long DictionaryNestLimit { get; set; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Searches for an installed commercial license matching the specified
        /// name.
        /// </summary>
        /// <param name="name">
        /// The name of the commercial license to search for.
        /// </param>
        /// <param name="id">
        /// Upon success, receives the unique identifier of the matching
        /// commercial license, if one was found; otherwise, receives null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if a matching commercial license was found; otherwise, false.
        /// </returns>
        bool LookForCommercialLicense(
            string name, out Guid? id, ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified token matches the security token
        /// associated with this interpreter.
        /// </summary>
        /// <param name="token">
        /// The token to compare against this interpreter, or null.
        /// </param>
        /// <returns>
        /// True if the specified token matches; otherwise, false.
        /// </returns>
        bool MatchToken(ulong? token);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the flags that were used to create the state of this
        /// interpreter.
        /// </summary>
        CreateStateFlags CreateStateFlags { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the unique identifier assigned to this interpreter without
        /// throwing an exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        long IdNoThrow { get; }            /* INTERNAL USE ONLY. */

        /// <summary>
        /// Gets the group identifier of this interpreter without throwing an
        /// exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        long GroupIdNoThrow { get; }       /* INTERNAL USE ONLY. */

        /// <summary>
        /// Gets the create count of this interpreter without throwing an
        /// exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        long CreateCountNoThrow { get; }   /* INTERNAL USE ONLY. */

        /// <summary>
        /// Gets the date and time when this interpreter was created without
        /// throwing an exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        DateTime CreatedNoThrow { get; }   /* INTERNAL USE ONLY. */

        /// <summary>
        /// Gets the dispose count of this interpreter without throwing an
        /// exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        long DisposeCountNoThrow { get; }  /* INTERNAL USE ONLY. */

        /// <summary>
        /// Gets the date and time when this interpreter was disposed without
        /// throwing an exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        DateTime DisposedNoThrow { get; }  /* INTERNAL USE ONLY. */

        /// <summary>
        /// Returns the hash code for this interpreter without throwing an
        /// exception if the interpreter has been disposed.
        /// </summary>
        /// <returns>
        /// The hash code for this interpreter.
        /// </returns>
        [Throw(false)]
        int GetHashCodeNoThrow();          /* INTERNAL USE ONLY. */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the date and time when this interpreter was last accessed
        /// without throwing an exception if the interpreter has been disposed,
        /// or null if it has not been accessed.
        /// </summary>
        [Throw(false)]
        DateTime? LastAccessedNoThrow { get; }

        /// <summary>
        /// Determines whether this interpreter has been accessed within the
        /// specified number of seconds.
        /// </summary>
        /// <param name="maximumSeconds">
        /// The maximum number of seconds permitted to have elapsed since this
        /// interpreter was last accessed.
        /// </param>
        /// <returns>
        /// True if this interpreter was last accessed within the specified
        /// number of seconds; otherwise, false.
        /// </returns>
        bool CheckLastAccessed(long maximumSeconds);
        /// <summary>
        /// Updates the time at which this interpreter was last accessed to the
        /// current date and time.
        /// </summary>
        void UpdateLastAccessed();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the rule set associated with this interpreter, if any.
        /// </summary>
        /// <returns>
        /// The <see cref="IRuleSet" /> associated with this interpreter, or
        /// null if there is none.
        /// </returns>
        IRuleSet GetRuleSet();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the application domain that this interpreter is running in.
        /// </summary>
        /// <returns>
        /// The <see cref="AppDomain" /> that this interpreter is running in.
        /// </returns>
        AppDomain GetAppDomain();
        /// <summary>
        /// Formats the identifier of the application domain that this
        /// interpreter is running in.
        /// </summary>
        /// <param name="display">
        /// Non-zero to format the identifier for display purposes.
        /// </param>
        /// <returns>
        /// The formatted application domain identifier.
        /// </returns>
        string FormatAppDomainId(bool display);
        /// <summary>
        /// Determines whether the specified application domain is the same as
        /// the one that this interpreter is running in.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to compare against the one that this
        /// interpreter is running in.
        /// </param>
        /// <returns>
        /// True if the specified application domain is the same as the one
        /// that this interpreter is running in; otherwise, false.
        /// </returns>
        bool IsSameAppDomain(AppDomain appDomain);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the calling thread is the primary thread for
        /// this interpreter.
        /// </summary>
        /// <returns>
        /// True if the calling thread is the primary thread for this
        /// interpreter; otherwise, false.
        /// </returns>
        bool IsPrimaryThread();

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        /// <summary>
        /// Returns the strong name associated with this interpreter, if any.
        /// </summary>
        /// <returns>
        /// The <see cref="StrongName" /> associated with this interpreter, or
        /// null if there is none.
        /// </returns>
        StrongName GetStrongName();
        /// <summary>
        /// Returns the hash associated with this interpreter, if any.
        /// </summary>
        /// <returns>
        /// The <see cref="Hash" /> associated with this interpreter, or null
        /// if there is none.
        /// </returns>
        Hash GetHash();
#endif

        /// <summary>
        /// Returns the certificate associated with this interpreter, if any.
        /// </summary>
        /// <returns>
        /// The <see cref="X509Certificate" /> associated with this
        /// interpreter, or null if there is none.
        /// </returns>
        X509Certificate GetCertificate();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Verifies that this interpreter has an associated strong name,
        /// throwing an exception if it does not.
        /// </summary>
        void DemandStrongName();

#if CAS_POLICY
        /// <summary>
        /// Verifies that this interpreter has an associated strong name,
        /// throwing an exception if it does not.
        /// </summary>
        /// <param name="strongName">
        /// Upon return, receives the strong name associated with this
        /// interpreter.
        /// </param>
        void DemandStrongName(ref StrongName strongName);
#endif

        /// <summary>
        /// Verifies that this interpreter has an associated certificate,
        /// throwing an exception if it does not.
        /// </summary>
        void DemandCertificate();

#if CAS_POLICY && !NET_STANDARD_20
        /// <summary>
        /// Verifies that this interpreter has an associated certificate,
        /// throwing an exception if it does not.
        /// </summary>
        /// <param name="certificate">
        /// Upon return, receives the certificate associated with this
        /// interpreter.
        /// </param>
        void DemandCertificate(ref X509Certificate certificate);
#endif

        ///////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns the next available identifier for use by this interpreter.
        /// </summary>
        /// <returns>
        /// The next available identifier.
        /// </returns>
        //
        // NOTE: This method is used to generated "opaque" handle names for a
        //       variety of things.
        //
        long NextId();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queries information about the framework for this interpreter.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the framework to query, or null to query
        /// the default framework.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the framework information is queried.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the requested framework information; upon
        /// failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode GetFramework(Guid? id, FrameworkFlags flags, ref Result result);
        /// <summary>
        /// Queries the execution context for the calling thread within this
        /// interpreter.
        /// </summary>
        /// <param name="result">
        /// Upon success, receives the requested context information; upon
        /// failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode GetContext(ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // HOST & SCRIPT ENVIRONMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the timeout, in milliseconds, used when checking
        /// whether this interpreter is ready, or null to use the default.
        /// </summary>
        int? ReadyTimeout { get; set; }

        /// <summary>
        /// Gets or sets the default data flags used by the engine when
        /// requesting a script from the interpreter host.
        /// </summary>
        //
        // NOTE: The default data and script flags used by the engine (i.e.
        //       in the EvaluateFile method) when requesting a script from
        //       the interpreter host.
        //
        DataFlags DataFlags { get; set; }
        /// <summary>
        /// Gets or sets the default script flags used by the engine when
        /// requesting a script from the interpreter host.
        /// </summary>
        ScriptFlags ScriptFlags { get; set; }

#if DATA
        /// <summary>
        /// Gets or sets the bundle manager associated with this interpreter.
        /// </summary>
        IBundleManager BundleManager { get; set; }
#endif

        /// <summary>
        /// Gets or sets the host associated with this interpreter.
        /// </summary>
        //
        // NOTE: The host could be almost anything, minimally it must be an
        //       IInteractiveHost implementation of some kind.
        //
        IHost Host { get; set; }

        /// <summary>
        /// Gets or sets the binder used by this interpreter for marshalling
        /// values to and from managed types.
        /// </summary>
        //
        // NOTE: Normally also a System.Reflection.Binder and an implementation
        //       of IScriptBinder.
        //
        IBinder Binder { get; set; }

        /// <summary>
        /// Gets or sets the object used to provide entropy for this
        /// interpreter.
        /// </summary>
        IProvideEntropy ProvideEntropy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this interpreter should
        /// suppress non-essential output.
        /// </summary>
        bool Quiet { get; set; }

#if POLICY_TRACE
        /// <summary>
        /// Gets or sets a value indicating whether policy decisions should be
        /// traced for this interpreter.
        /// </summary>
        bool PolicyTrace { get; set; }
#endif

        /// <summary>
        /// Gets or sets the callback invoked when this interpreter needs to be
        /// locked.
        /// </summary>
        LockCallback LockCallback { get; set; }
        /// <summary>
        /// Gets or sets the number of times that an attempt to lock this
        /// interpreter should be retried.
        /// </summary>
        int LockRetries { get; set; }
        /// <summary>
        /// Gets or sets the current lock nesting level for this interpreter.
        /// </summary>
        int LockLevel { get; set; }

        /// <summary>
        /// Gets or sets the callback used to filter trace output produced by
        /// this interpreter.
        /// </summary>
        TraceFilterCallback TraceFilterCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when a new command is added to
        /// this interpreter.
        /// </summary>
        NewCommandCallback NewCommandCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when a new procedure is added to
        /// this interpreter.
        /// </summary>
        NewProcedureCallback NewProcedureCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback used to perform custom matching for this
        /// interpreter.
        /// </summary>
        MatchCallback MatchCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when this interpreter checks
        /// whether it is ready.
        /// </summary>
        ReadyCallback ReadyCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback used to obtain a timeout value for this
        /// interpreter.
        /// </summary>
        GetTimeoutCallback GetTimeoutCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked before this interpreter waits on
        /// an event.
        /// </summary>
        EventCallback PreWaitCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked after this interpreter waits on
        /// an event.
        /// </summary>
        EventCallback PostWaitCallback { get; set; }

#if NETWORK
        /// <summary>
        /// Gets or sets the callback invoked before a web client is created
        /// for this interpreter.
        /// </summary>
        PreWebClientCallback PreWebClientCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when a new web client is created
        /// for this interpreter.
        /// </summary>
        NewWebClientCallback NewWebClientCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked to perform a web transfer for
        /// this interpreter.
        /// </summary>
        WebTransferCallback WebTransferCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when a web transfer for this
        /// interpreter encounters an error.
        /// </summary>
        WebErrorCallback WebErrorCallback { get; set; }
#endif

#if THREADING
        /// <summary>
        /// Gets or sets the callback invoked to check the health of this
        /// interpreter.
        /// </summary>
        HealthCallback HealthCallback { get; set; }
#endif

        /// <summary>
        /// Gets or sets the script used to handle background errors raised by
        /// this interpreter.
        /// </summary>
        string BackgroundError { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // UNKNOWN HANDLING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the name of the command used to handle unknown
        /// commands for this interpreter.
        /// </summary>
        string Unknown { get; set; }
        /// <summary>
        /// Gets or sets the name of the command used to handle unknown
        /// commands at the global level for this interpreter.
        /// </summary>
        string GlobalUnknown { get; set; }
        /// <summary>
        /// Gets or sets the name of the command used to handle unknown
        /// commands within a namespace for this interpreter.
        /// </summary>
        string NamespaceUnknown { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when this interpreter encounters
        /// an unknown command.
        /// </summary>
        UnknownCallback UnknownCallback { get; set; }
        /// <summary>
        /// Gets or sets the callback invoked when a requested package cannot be
        /// found by this interpreter.
        /// </summary>
        PackageCallback PackageFallback { get; set; }
        /// <summary>
        /// Gets or sets the name of the command used to handle unknown packages
        /// for this interpreter.
        /// </summary>
        string PackageUnknown { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // OUTPUT HANDLING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the output margin, in characters, used by this interpreter.
        /// </summary>
        int OutputMargin { get; }
        /// <summary>
        /// Gets the output width, in characters, used by this interpreter.
        /// </summary>
        int OutputWidth { get; }
        /// <summary>
        /// Gets the output height, in characters, used by this interpreter.
        /// </summary>
        int OutputHeight { get; }

        ///////////////////////////////////////////////////////////////////////
        // EXIT HANDLING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this interpreter has been
        /// requested to exit, without throwing an exception if the interpreter
        /// has been disposed.
        /// </summary>
        //
        // NOTE: These properties are, in both theory and practice, very
        //       closely tied to the precise implementation semantics of the
        //       IHost implementation in use; therefore, they are considered to
        //       be part of the "host environment".
        //
        [Throw(false)]
        bool ExitNoThrow { get; set; }
        /// <summary>
        /// Gets or sets the exit code for this interpreter, without throwing an
        /// exception if the interpreter has been disposed.
        /// </summary>
        [Throw(false)]
        ExitCode ExitCodeNoThrow { get; set; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this interpreter has been
        /// requested to exit.
        /// </summary>
        bool Exit { get; set; }
        /// <summary>
        /// Gets or sets the exit code for this interpreter.
        /// </summary>
        ExitCode ExitCode { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // DATE & TIME HANDLING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the format string used when converting date and time
        /// values to and from strings in this interpreter.
        /// </summary>
        string DateTimeFormat { get; set; }
        /// <summary>
        /// Gets or sets the kind used when interpreting date and time values in
        /// this interpreter.
        /// </summary>
        DateTimeKind DateTimeKind { get; set; }
        /// <summary>
        /// Gets or sets the styles used when parsing date and time values in
        /// this interpreter.
        /// </summary>
        DateTimeStyles DateTimeStyles { get; set; }
        /// <summary>
        /// Gets or sets the list of time servers used by this interpreter.
        /// </summary>
        IEnumerable<string> TimeServers { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // EXPRESSION PRECISION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the number of significant digits used when converting
        /// floating-point values to strings in this interpreter.
        /// </summary>
        int Precision { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // EXECUTION LIMITS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether this interpreter is enabled
        /// for execution.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this interpreter is
        /// read-only.
        /// </summary>
        bool ReadOnly { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this interpreter is
        /// immutable.
        /// </summary>
        bool Immutable { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of nested ready checks permitted for
        /// this interpreter.
        /// </summary>
        int ReadyLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum recursion depth permitted for this
        /// interpreter.
        /// </summary>
        int RecursionLimit { get; set; }
        /// <summary>
        /// Gets or sets the stack size, in bytes, used for threads created by
        /// this interpreter.
        /// </summary>
        int ThreadStackSize { get; set; }
        /// <summary>
        /// Gets or sets the amount of extra stack space, in bytes, required by
        /// this interpreter.
        /// </summary>
        int ExtraStackSpace { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of child interpreters permitted for
        /// this interpreter.
        /// </summary>
        int ChildLimit { get; set; }

#if CALLBACK_QUEUE
        /// <summary>
        /// Gets or sets the maximum number of queued callbacks permitted for
        /// this interpreter.
        /// </summary>
        int CallbackLimit { get; set; }
#endif

        /// <summary>
        /// Gets or sets the maximum number of loop iterations permitted for
        /// this interpreter.
        /// </summary>
        int IterationLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of namespaces permitted for this
        /// interpreter.
        /// </summary>
        int NamespaceLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of scopes permitted for this
        /// interpreter.
        /// </summary>
        int ScopeLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of events permitted for this
        /// interpreter.
        /// </summary>
        int EventLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of procedures permitted for this
        /// interpreter.
        /// </summary>
        int ProcedureLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of variables permitted for this
        /// interpreter.
        /// </summary>
        int VariableLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of array elements permitted for this
        /// interpreter.
        /// </summary>
        int ArrayElementLimit { get; set; }

#if RESULT_LIMITS
        /// <summary>
        /// Gets or sets the maximum length permitted for the result of a single
        /// execution in this interpreter.
        /// </summary>
        int ExecuteResultLimit { get; set; }
        /// <summary>
        /// Gets or sets the maximum length permitted for a nested result in
        /// this interpreter.
        /// </summary>
        int NestedResultLimit { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // EXECUTION TIMEOUTS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the fallback timeout, in milliseconds, used by this
        /// interpreter when no other timeout is available, or null if there is
        /// none.
        /// </summary>
        // [Obsolete()]
        int? FallbackTimeout { get; set; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the timeout value of the specified type for this
        /// interpreter.
        /// </summary>
        /// <param name="timeoutType">
        /// The type of timeout to return.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// The timeout value, in milliseconds, of the specified type, or null
        /// if there is none.
        /// </returns>
        int? GetTimeout(
            TimeoutType timeoutType,
            ref Result error
        );

        /// <summary>
        /// Sets or unsets the timeout value of the specified type for this
        /// interpreter.
        /// </summary>
        /// <param name="timeoutType">
        /// The type of timeout to set or unset.
        /// </param>
        /// <param name="timeout">
        /// The timeout value, in milliseconds, to set, or null to unset the
        /// timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the timeout was set or unset successfully; otherwise, false.
        /// </returns>
        bool SetOrUnsetTimeout(
            TimeoutType timeoutType,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////
        // ENGINE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether this interpreter is currently busy
        /// executing a script.
        /// </summary>
        bool IsBusy { get; }
        /// <summary>
        /// Gets a value indicating whether this interpreter is currently busy
        /// executing a script at the global level.
        /// </summary>
        bool IsGlobalBusy { get; }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current evaluation nesting level for this interpreter.
        /// </summary>
        int Levels { get; } // WARNING: NOT GUARANTEED TO BE ACCURATE OR USEFUL.
        /// <summary>
        /// Gets the current trusted evaluation nesting level for this
        /// interpreter.
        /// </summary>
        int TrustedLevels { get; } // WARNING: NOT GUARANTEED TO BE ACCURATE OR USEFUL.

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of active executions in progress within this
        /// interpreter.
        /// </summary>
        int ActiveCount { get; } // WARNING: NOT GUARANTEED TO BE ACCURATE OR USEFUL.

        ///////////////////////////////////////////////////////////////////////
        // XML DATA HANDLING
        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// Gets or sets the types of XML errors that should cause an operation
        /// to be retried by this interpreter.
        /// </summary>
        XmlErrorTypes RetryXml { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether XML should be validated by
        /// this interpreter.
        /// </summary>
        bool ValidateXml { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether relaxed XML processing
        /// should be used by this interpreter.
        /// </summary>
        bool RelaxedXml { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether all XML processing options
        /// should be enabled for this interpreter.
        /// </summary>
        bool AllXml { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // WATCHDOG SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to lock this interpreter and request that it exit.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock,
        /// or null to use the default.
        /// </param>
        /// <returns>
        /// True if the interpreter was locked and the exit was requested
        /// successfully; otherwise, false.
        /// </returns>
        bool TryLockAndExit(int? timeout);

#if THREADING
        /// <summary>
        /// Gets or sets the status reported by the watchdog for this
        /// interpreter, or null if there is none.
        /// </summary>
        CheckStatus? WatchdogStatus { get; set; }
#endif

        /// <summary>
        /// Performs the specified watchdog control operation for this
        /// interpreter.
        /// </summary>
        /// <param name="watchdogType">
        /// The type of watchdog to control.
        /// </param>
        /// <param name="watchdogOperation">
        /// The control operation to perform on the watchdog.
        /// </param>
        /// <param name="clientData">
        /// The extra data to supply to the watchdog operation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the
        /// operation to complete, or null to use the default.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of the operation; upon failure,
        /// receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode WatchdogControl(
            WatchdogType watchdogType,
            WatchdogOperation watchdogOperation,
            IClientData clientData,
            int? timeout,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // EVENT QUEUE MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, that this
        /// interpreter sleeps while servicing its event queue.
        /// </summary>
        int SleepTime { get; set; }
        /// <summary>
        /// Gets the event manager associated with this interpreter.
        /// </summary>
        IEventManager EventManager { get; }
        /// <summary>
        /// Gets the flags used when servicing the event queue for this
        /// interpreter.
        /// </summary>
        EventFlags ServiceEventFlags { get; }

        /// <summary>
        /// Queues a script for asynchronous evaluation by this interpreter at
        /// or after the specified date and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time at or after which the script should be evaluated.
        /// </param>
        /// <param name="text">
        /// The text of the script to evaluate.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            ref Result error
            );

        /// <summary>
        /// Queues a script for asynchronous evaluation by this interpreter at
        /// or after the specified date and time, returning the event that
        /// represents the queued script.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time at or after which the script should be evaluated.
        /// </param>
        /// <param name="text">
        /// The text of the script to evaluate.
        /// </param>
        /// <param name="event">
        /// Upon success, receives the event that represents the queued script.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            ref IEvent @event,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ENTITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the entity manager associated with this interpreter.
        /// </summary>
        IEntityManager EntityManager { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the interpreter manager associated with this interpreter.
        /// </summary>
        IInterpreterManager InterpreterManager { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD & LIFETIME MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes of the per-thread state maintained by this interpreter for
        /// the calling thread.
        /// </summary>
        /// <returns>
        /// True if the per-thread state was disposed successfully; otherwise,
        /// false.
        /// </returns>
        bool DisposeThread();
    }
}
