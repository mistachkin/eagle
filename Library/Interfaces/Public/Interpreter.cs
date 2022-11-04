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
    [ObjectId("eb34a7f9-199e-4bbb-90b9-7ab5334bcc38")]
    public interface IInterpreter : IMaybeDisposed
    {
        ///////////////////////////////////////////////////////////////////////
        // OBJECT IDENTITY & AFFINITY
        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: THESE PROPERTIES ARE NOT GUARANTEED TO BE ACCURATE OR USEFUL.
        //
        long Id { get; }
        long GroupId { get; }
        long CreateCount { get; }
        DateTime Created { get; }
        long ThreadId { get; }
        Thread Thread { get; }

#if SHELL
        Thread InteractiveThread { get; }
#endif

        bool NoThreadAbort { get; }

        EventWaitHandle VariableEvent { get; }
        EventWaitHandle SetupEvent { get; }

        ///////////////////////////////////////////////////////////////////////

        bool MatchToken(ulong? token);

        ///////////////////////////////////////////////////////////////////////

        [Throw(false)]
        long IdNoThrow { get; }            /* INTERNAL USE ONLY. */

        [Throw(false)]
        long GroupIdNoThrow { get; }       /* INTERNAL USE ONLY. */

        [Throw(false)]
        long CreateCountNoThrow { get; }   /* INTERNAL USE ONLY. */

        [Throw(false)]
        DateTime CreatedNoThrow { get; }   /* INTERNAL USE ONLY. */

        [Throw(false)]
        int GetHashCodeNoThrow();          /* INTERNAL USE ONLY. */

        ///////////////////////////////////////////////////////////////////////

        IRuleSet GetRuleSet();

        ///////////////////////////////////////////////////////////////////////

        AppDomain GetAppDomain();
        string FormatAppDomainId(bool display);
        bool IsSameAppDomain(AppDomain appDomain);

        ///////////////////////////////////////////////////////////////////////

        bool IsPrimaryThread();

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        StrongName GetStrongName();
        Hash GetHash();
#endif

        X509Certificate GetCertificate();

        ///////////////////////////////////////////////////////////////////////

        void DemandStrongName();

#if CAS_POLICY
        void DemandStrongName(ref StrongName strongName);
#endif

        void DemandCertificate();

#if CAS_POLICY && !NET_STANDARD_20
        void DemandCertificate(ref X509Certificate certificate);
#endif

        ///////////////////////////////////////////////////////////////////////
        //
        // NOTE: This method is used to generated "opaque" handle names for a
        //       variety of things.
        //
        long NextId();

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetFramework(Guid? id, FrameworkFlags flags, ref Result result);
        ReturnCode GetContext(ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // HOST & SCRIPT ENVIRONMENT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default script flags used by the engine (i.e. in the
        //       EvaluateFile method) when requesting a script from the
        //       interpreter host.
        //
        ScriptFlags ScriptFlags { get; set; }

        //
        // NOTE: The host could be almost anything, minimally it must be an
        //       IInteractiveHost implementation of some kind.
        //
        IHost Host { get; set; }

        //
        // NOTE: Normally also a System.Reflection.Binder and an implementation
        //       of IScriptBinder.
        //
        IBinder Binder { get; set; }

        bool Quiet { get; set; }

#if POLICY_TRACE
        bool PolicyTrace { get; set; }
#endif

        TraceFilterCallback TraceFilterCallback { get; set; }
        NewCommandCallback NewCommandCallback { get; set; }
        NewProcedureCallback NewProcedureCallback { get; set; }
        MatchCallback MatchCallback { get; set; }
        ReadyCallback ReadyCallback { get; set; }
        GetTimeoutCallback GetTimeoutCallback { get; set; }
        EventCallback PreWaitCallback { get; set; }
        EventCallback PostWaitCallback { get; set; }

#if NETWORK
        PreWebClientCallback PreWebClientCallback { get; set; }
        NewWebClientCallback NewWebClientCallback { get; set; }
#endif

#if THREADING
        HealthCallback HealthCallback { get; set; }
#endif

        string BackgroundError { get; set; }
        string Unknown { get; set; }
        string GlobalUnknown { get; set; }
        string NamespaceUnknown { get; set; }

        UnknownCallback UnknownCallback { get; set; }
        PackageCallback PackageFallback { get; set; }
        string PackageUnknown { get; set; }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These properties are, in both theory and practice, very
        //       closely tied to the precise implementation semantics of the
        //       IHost implementation in use; therefore, they are considered to
        //       be part of the "host environment".
        //
        [Throw(false)]
        bool ExitNoThrow { get; set; }
        [Throw(false)]
        ExitCode ExitCodeNoThrow { get; set; }

        ///////////////////////////////////////////////////////////////////////

        bool Exit { get; set; }
        ExitCode ExitCode { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // DATE & TIME HANDLING
        ///////////////////////////////////////////////////////////////////////

        string DateTimeFormat { get; set; }
        DateTimeKind DateTimeKind { get; set; }
        DateTimeStyles DateTimeStyles { get; set; }
        IEnumerable<string> TimeServers { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // EXPRESSION PRECISION
        ///////////////////////////////////////////////////////////////////////

        int Precision { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // EXECUTION LIMITS
        ///////////////////////////////////////////////////////////////////////

        bool Enabled { get; set; }
        bool ReadOnly { get; set; }
        bool Immutable { get; set; }
        int ReadyLimit { get; set; }
        int RecursionLimit { get; set; }
        int ThreadStackSize { get; set; }
        int ExtraStackSpace { get; set; }

        int ChildLimit { get; set; }

#if CALLBACK_QUEUE
        int CallbackLimit { get; set; }
#endif

        int NamespaceLimit { get; set; }
        int ScopeLimit { get; set; }
        int EventLimit { get; set; }
        int ProcedureLimit { get; set; }
        int VariableLimit { get; set; }
        int ArrayElementLimit { get; set; }

#if RESULT_LIMITS
        int ExecuteResultLimit { get; set; }
        int NestedResultLimit { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // EXECUTION TIMEOUTS
        ///////////////////////////////////////////////////////////////////////

        // [Obsolete()]
        int? FallbackTimeout { get; set; }

        ///////////////////////////////////////////////////////////////////////

        int? GetTimeout(
            TimeoutType timeoutType,
            ref Result error
        );

        bool SetOrUnsetTimeout(
            TimeoutType timeoutType,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////
        // ENGINE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        bool IsBusy { get; }
        bool IsGlobalBusy { get; }

        ///////////////////////////////////////////////////////////////////////

        int Levels { get; } // WARNING: NOT GUARANTEED TO BE ACCURATE OR USEFUL.

        ///////////////////////////////////////////////////////////////////////
        // XML DATA HANDLING
        ///////////////////////////////////////////////////////////////////////

#if XML
        XmlErrorTypes RetryXml { get; set; }
        bool ValidateXml { get; set; }
        bool RelaxedXml { get; set; }
        bool AllXml { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // WATCHDOG SUPPORT
        ///////////////////////////////////////////////////////////////////////

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

        int SleepTime { get; set; }
        IEventManager EventManager { get; }
        EventFlags ServiceEventFlags { get; }

        ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            ref Result error
            );

        ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            ref IEvent @event,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ENTITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        IEntityManager EntityManager { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        IInterpreterManager InterpreterManager { get; }
    }
}
