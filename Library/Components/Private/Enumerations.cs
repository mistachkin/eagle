/*
 * Enumerations.cs --
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

namespace Eagle._Components.Private
{
    /// <summary>
    /// This enumeration specifies which part of a dictionary entry a filtering operation should match against.
    /// </summary>
    [ObjectId("8c591d9f-b9f1-46f0-86ba-48d9cabdd6ca")]
    internal enum DictionaryFilterType
    {
        // None = 0x0,
        // Invalid = 0x1,

        /// <summary>
        /// Match against the key of each dictionary entry.
        /// </summary>
        Key = 0x100,
        /// <summary>
        /// Match against the value of each dictionary entry.
        /// </summary>
        Value = 0x200,
        /// <summary>
        /// Match by evaluating a script against each dictionary entry.
        /// </summary>
        Script = 0x400
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies which kinds of command and operation counts are included when measuring interpreter activity.
    /// </summary>
    [Flags()]
    [ObjectId("f8def65f-3589-4d35-ac93-e6f31cadf86f")]
    internal enum CommandCountType
    {
        /// <summary>
        /// No count types are selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Include the count of expression operations.
        /// </summary>
        OperationCount = 0x2,
        /// <summary>
        /// Include the count of executed commands.
        /// </summary>
        CommandCount = 0x4,
        /// <summary>
        /// Include the count of unknown-command dispatches.
        /// </summary>
        UnknownCount = 0x8,

        /// <summary>
        /// Marker bit selecting the legacy counting behavior.
        /// </summary>
        ForLegacy = 0x10000000,
        /// <summary>
        /// Marker bit selecting the default counting behavior.
        /// </summary>
        ForDefault = 0x20000000,

        /// <summary>
        /// Includes the operation, command, and unknown counts.
        /// </summary>
        All = OperationCount | CommandCount | UnknownCount,
        /// <summary>
        /// The legacy counting behavior (command count only).
        /// </summary>
        Legacy = CommandCount | ForLegacy,
        /// <summary>
        /// The default counting behavior (all counts).
        /// </summary>
        Default = All | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies which external tool is used to query the set of installed system updates.
    /// </summary>
    [Flags()]
    [ObjectId("232a2243-4899-4455-b6b6-9cc151a4ca56")]
    internal enum GetInstalledUpdatesType
    {
        /// <summary>
        /// No method is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Use the wmic.exe tool to query installed updates.
        /// </summary>
        WmiCommand = 0x2, // Use the "wmic.exe" tool.
        /// <summary>
        /// Use the PowerShell.exe tool to query installed updates.
        /// </summary>
        PowerShell = 0x4  // Use the "PowerShell.exe" tool.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how values are escaped, including the position within the value and assorted escaping options.
    /// </summary>
    [Flags()]
    [ObjectId("752678c1-c688-4f2d-89a1-01ae342c72f0")]
    internal enum EscapeMode
    {
        /// <summary>
        /// No escaping options are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Escape at the start of the value.
        /// </summary>
        Start = 0x100,
        /// <summary>
        /// Escape in the middle of the value.
        /// </summary>
        Middle = 0x200,
        /// <summary>
        /// Escape at the end of the value.
        /// </summary>
        End = 0x400,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Quote the entire value.
        /// </summary>
        QuoteAll = 0x1000,
        /// <summary>
        /// Escape the value for use by the command processor.
        /// </summary>
        ForProcessor = 0x2000,
        /// <summary>
        /// Suppress errors encountered while escaping.
        /// </summary>
        NoComplain = 0x4000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering all of the escaping option flags.
        /// </summary>
        FlagsMask = QuoteAll | ForProcessor | NoComplain,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default escaping mode (no options).
        /// </summary>
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the individual fields that make up a script bundle record.
    /// </summary>
    [ObjectId("67126d39-f60d-4ee3-b436-af3c78436030")]
    internal enum BundleField
    {
        /// <summary>
        /// The unique identifier of the bundle.
        /// </summary>
        Id = 0,
        /// <summary>
        /// The language of the bundle.
        /// </summary>
        Language,
        /// <summary>
        /// The sequence number of the bundle.
        /// </summary>
        Sequence,
        /// <summary>
        /// The vendor of the bundle.
        /// </summary>
        Vendor,
        /// <summary>
        /// The hash algorithm used by the bundle.
        /// </summary>
        HashAlgorithm,
        /// <summary>
        /// The isolation level of the bundle.
        /// </summary>
        IsolationLevel,
        /// <summary>
        /// The security level of the bundle.
        /// </summary>
        SecurityLevel,
        /// <summary>
        /// The security flags of the bundle.
        /// </summary>
        SecurityFlags,
        /// <summary>
        /// The rule set associated with the bundle.
        /// </summary>
        RuleSet,
        /// <summary>
        /// The block type of the bundle.
        /// </summary>
        BlockType,
        /// <summary>
        /// The full name of the bundle.
        /// </summary>
        FullName,
        /// <summary>
        /// The group of the bundle.
        /// </summary>
        Group,
        /// <summary>
        /// The description of the bundle.
        /// </summary>
        Description,
        /// <summary>
        /// The time stamp of the bundle.
        /// </summary>
        TimeStamp,
        /// <summary>
        /// The public key token of the bundle.
        /// </summary>
        PublicKeyToken,
        /// <summary>
        /// The script text of the bundle.
        /// </summary>
        Text,
        /// <summary>
        /// The signature of the bundle.
        /// </summary>
        Signature,
        /// <summary>
        /// The total number of bundle fields.
        /// </summary>
        Count = 17
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies the kind of peer involved in a channel or connection operation.
    /// </summary>
    [Flags()]
    [ObjectId("4c3b273f-6e21-46be-a4b6-dfc4470a10f4")]
    internal enum PeerType
    {
        /// <summary>
        /// No peer type is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// A normal peer.
        /// </summary>
        Normal = 0x100,

        /// <summary>
        /// A peer that accepts no arguments.
        /// </summary>
        NoArguments = 0x1000,
        /// <summary>
        /// A peer represented by an opaque object handle.
        /// </summary>
        Object = 0x4000,
        /// <summary>
        /// A peer represented by an alias.
        /// </summary>
        Alias = 0x8000,

        /// <summary>
        /// The default peer type (none).
        /// </summary>
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the kind of context (interpreter or command) associated with an operation.
    /// </summary>
    [Flags()]
    [ObjectId("c3fbb06f-39b3-4cf7-8d4a-3ba94b6691f2")]
    internal enum ContextType : ulong
    {
        /// <summary>
        /// No context type is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        //
        // WARNING: Do not change these names, including
        //          their (lower) casing.  They are used
        //          directly in error messages.
        //
        /// <summary>
        /// An interpreter context.
        /// </summary>
        interpreter = 0x20000000,
        /// <summary>
        /// A command context.
        /// </summary>
        command = 0x40000000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies the outcome of a test, such as passed, failed, or disabled.
    /// </summary>
    [Flags()]
    [ObjectId("37642c03-7be0-4f32-86d8-05345880f8c4")]
    internal enum TestResultType
    {
        /// <summary>
        /// No test result type is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// The test has not yet completed.
        /// </summary>
        Pending = 0x1000,
        /// <summary>
        /// The test passed.
        /// </summary>
        Passed = 0x2000,
        /// <summary>
        /// The test failed.
        /// </summary>
        Failed = 0x4000,
        /// <summary>
        /// The test is disabled.
        /// </summary>
        Disabled = 0x8000,
        /// <summary>
        /// The test was ignored.
        /// </summary>
        Ignored = 0x10000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies when a test hook runs and how it is matched and managed.
    /// </summary>
    [Flags()]
    [ObjectId("62cbe58f-73dd-4f14-b139-f5daeac54883")]
    internal enum TestHookType
    {
        /// <summary>
        /// No hook type is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Run the hook before the test.
        /// </summary>
        Before = 0x100,
        /// <summary>
        /// Run the hook after the test.
        /// </summary>
        After = 0x200,

        /// <summary>
        /// Match the test name exactly.
        /// </summary>
        ExactMatch = 0x1000,
        /// <summary>
        /// Match against any test name.
        /// </summary>
        AnyMatch = 0x2000,
        /// <summary>
        /// Match the test name without regard to case.
        /// </summary>
        NoCase = 0x4000,
        /// <summary>
        /// Allow an existing hook to be overwritten.
        /// </summary>
        AllowOverwrite = 0x8000,

        /// <summary>
        /// Marker bit selecting the default hook behavior.
        /// </summary>
        ForDefault = 0x100000,

        /// <summary>
        /// Bit mask covering the hook timing (before and after) flags.
        /// </summary>
        TypeMask = Before | After,
        /// <summary>
        /// Bit mask covering the hook matching and management flags.
        /// </summary>
        FlagMask = ExactMatch | NoCase | AllowOverwrite,

        /// <summary>
        /// The default hook type (before, default).
        /// </summary>
        Default = Before | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
    /// <summary>
    /// This enumeration controls how security descriptor (SDDL) access rules are enumerated and modified.
    /// </summary>
    [Flags()]
    [ObjectId("be6327bf-27b6-4d04-b794-4526d0b712c1")]
    internal enum SddlFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Include explicitly defined access rules.
        /// </summary>
        IncludeExplicit = 0x10000,
        /// <summary>
        /// Include inherited access rules.
        /// </summary>
        IncludeInherited = 0x20000,
        /// <summary>
        /// Skip access rules with rights that cannot be interpreted.
        /// </summary>
        SkipBadRights = 0x40000,

        /// <summary>
        /// Remove the matching access rules.
        /// </summary>
        Remove = 0x10000000,
        /// <summary>
        /// Return the access rules as a list.
        /// </summary>
        ToList = 0x20000000,

        /// <summary>
        /// Remove all explicit and inherited access rules.
        /// </summary>
        RemoveAll = IncludeExplicit | IncludeInherited | Remove,

        /// <summary>
        /// The default flags (none).
        /// </summary>
        Default = None
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies how the current thread yields execution to other threads.
    /// </summary>
    [Flags()]
    [ObjectId("e5654615-73b1-4a95-8c5a-26daf8f64098")]
    internal enum YieldType
    {
        /// <summary>
        /// Do nothing.
        /// </summary>
        None = 0x0,    /* Do nothing. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1, /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use both Thread.Yield and Thread.Sleep.
        /// </summary>
        Both = 0x2,    /* Use Thread.Yield() and Thread.Sleep(N) */
        /// <summary>
        /// Use Thread.Yield only.
        /// </summary>
        Pure = 0x4,    /* Use Thread.Yield() */
        /// <summary>
        /// Use Thread.Sleep only.
        /// </summary>
        Sleep = 0x8,   /* Use Thread.Sleep(N) */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering all non-zero yield values.
        /// </summary>
        Mask = Invalid | Both | Pure | Sleep, /* All non-zero values. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use both yielding methods with the maximum sleep interval.
        /// </summary>
        Maximum = Both | 0x40, /* 64 milliseconds */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default yield type.
        /// </summary>
        Default = Pure
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration provides contextual indicator flags describing the environment and origin of a trace message.
    /// </summary>
    [Flags()]
    [ObjectId("1a846c0c-3b67-4a2b-b218-1292b9afadfe")]
    internal enum TraceIndicatorFlags : ulong
    {
        /// <summary>
        /// No indicator flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        //
        // HACK: These values were stolen from the TraceListenerType
        //       enumeration and must be manually kept synchronized.
        //
        /// <summary>
        /// The default trace listener is present.
        /// </summary>
        HaveDefault = 0x1000,
        /// <summary>
        /// The console trace listener is present.
        /// </summary>
        HaveConsole = 0x2000,
        /// <summary>
        /// The native trace listener is present.
        /// </summary>
        HaveNative = 0x4000,
        /// <summary>
        /// The raw log file trace listener is present.
        /// </summary>
        HaveRawLogFile = 0x8000,
        /// <summary>
        /// The test log file trace listener is present.
        /// </summary>
        HaveTestLogFile = 0x10000,
        /// <summary>
        /// The buffered trace listener is present.
        /// </summary>
        HaveBuffered = 0x20000,

        //
        // NOTE: When set, this means the calling (external?) method
        //       is external to the Eagle core library.
        //
        /// <summary>
        /// The calling method is external to the Eagle core library.
        /// </summary>
        External = 0x100000,

        //
        // NOTE: When set, this means the calling (external?) method
        //       is contained within an Eagle binary plugin.
        //
        /// <summary>
        /// The calling method is contained within an Eagle binary plugin.
        /// </summary>
        FromPlugin = 0x200000,

        //
        // NOTE: When set, this means the calling (external?) method
        //       is contained within an external SDK integration.
        //
        /// <summary>
        /// The calling method is contained within an external SDK integration.
        /// </summary>
        FromSdk = 0x400000,

        //
        // NOTE: When set, this means the calling (external?) method
        //       is a wrapper around Utility.DebugTrace, et al.
        //
        /// <summary>
        /// The calling method is a wrapper around the tracing utility methods.
        /// </summary>
        ViaWrapper = 0x800000,

        //
        // NOTE: When set, this means the current thread (the one
        //       performing the tracing) is a background thread.
        //
        /// <summary>
        /// The current thread is a background thread.
        /// </summary>
        Background = 0x1000000,

        //
        // NOTE: When set, this means the current thread (the one
        //       performing the tracing) is a thread-pool thread.
        //
        /// <summary>
        /// The current thread is a thread-pool thread.
        /// </summary>
        ThreadPool = 0x2000000,

        //
        // NOTE: When set, this means the current thread (the one
        //       performing the tracing) matches the primary thread
        //       for the interpreter.
        //
        /// <summary>
        /// The current thread is the primary thread for the interpreter.
        /// </summary>
        PrimaryThread = 0x4000000,

        //
        // NOTE: When set, this means that there is no interpreter
        //       context available within the tracing subsystem.
        //
        /// <summary>
        /// No interpreter context is available within the tracing subsystem.
        /// </summary>
        NoInterpreter = 0x8000000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies the behavior to use when a requested item cannot be found.
    /// </summary>
    [ObjectId("fe64b30e-2c31-4a1c-925b-8556a3fd2229")]
    internal enum IfNotFoundType
    {
        /// <summary>
        /// No special handling.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Return a null value when the item is not found.
        /// </summary>
        Null = 0x2,
        /// <summary>
        /// Return an unknown indicator when the item is not found.
        /// </summary>
        Unknown = 0x4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
    /// <summary>
    /// This enumeration specifies optional behaviors for the interactive shell loop.
    /// </summary>
    [Flags()]
    [ObjectId("cac467bd-9d4a-46b2-aeb3-4a790523b8df")]
    internal enum InteractiveLoopFlags
    {
        /// <summary>
        /// No special handling.
        /// </summary>
        None = 0x0,           /* No special handling. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,        /* Invalid, do not use. */
        /// <summary>
        /// The interactive loop should not modify the timeout thread.
        /// </summary>
        NoTimeout = 0x2,      /* The interactive loop should not mess with
                               * the timeout thread. */
        /// <summary>
        /// Trace interactive input processing.
        /// </summary>
        TraceInput = 0x4,     /* Trace interactive input processing.
                               * Used by the interactive loop. */
        /// <summary>
        /// Do not use the semaphore when handling interactive input.
        /// </summary>
        NoSemaphore = 0x8,    /* Do not use the semaphore when dealing with
                               * interactive input from within the
                               * * interactive loop. */
        /// <summary>
        /// Wait indefinitely for the interactive semaphore to become available.
        /// </summary>
        WaitSemaphore = 0x10, /* Wait (forever) for the interactive
                               * semaphore to be available.  If this flag
                               * is not set, the inability to obtain the
                               * interactive semaphore will bail out of
                               * the interactive loop. */
        /// <summary>
        /// Do not refresh the interactive host before reading input.
        /// </summary>
        NoRefreshHost = 0x20, /* Do not refresh the interactive host when
                               * preparing to read interactive input from
                               * within the interactive loop. */
        /// <summary>
        /// Trace interactive commands.
        /// </summary>
        TraceCommand = 0x40,  /* Trace interactive commands.  Used by the
                               * interactive loop. */
        /// <summary>
        /// Dump the debugger override command and its queue of commands.
        /// </summary>
        DumpCommands = 0x80,  /* Dump the debugger "override" command
                               * and its queue of commands.  Used by the
                               * interactive loop. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Marker bit selecting the default interactive loop behavior.
        /// </summary>
        ForDefault = 0x100000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default interactive loop flags.
        /// </summary>
        Default = ForDefault
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration represents a tri-state enabled setting (false, true, or automatic).
    /// </summary>
    [ObjectId("b374f974-c19e-4706-b323-033c4d09fb34")]
    internal enum MaybeEnableType
    {
        /// <summary>
        /// The feature is disabled.
        /// </summary>
        False = 0,
        /// <summary>
        /// The feature is enabled.
        /// </summary>
        True = 1,
        /// <summary>
        /// The feature is enabled or disabled automatically.
        /// </summary>
        Automatic = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: This enumeration is for use by the WindowOps class only.
    //          PLEASE DO NOT USE.  It is subject to change at any time.
    //
    /// <summary>
    /// This enumeration specifies how the user-interactive state of the process is determined; reserved for use by the WindowOps class only.
    /// </summary>
    [ObjectId("509325bb-f329-413f-b201-f1e4dc5e8246")]
    internal enum UserInteractiveType
    {
        /// <summary>
        /// Not user-interactive.
        /// </summary>
        False = 0,

        /// <summary>
        /// User-interactive.
        /// </summary>
        True = 1,

        /// <summary>
        /// Not user-interactive.
        /// </summary>
        No = 0,

        /// <summary>
        /// User-interactive.
        /// </summary>
        Yes = 1,

        /// <summary>
        /// Not user-interactive.
        /// </summary>
        Off = 0,

        /// <summary>
        /// User-interactive.
        /// </summary>
        On = 1,

        /// <summary>
        /// Not user-interactive.
        /// </summary>
        Disable = 0,

        /// <summary>
        /// User-interactive.
        /// </summary>
        Enable = 1,

        /// <summary>
        /// Not user-interactive.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// User-interactive.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Defer the decision to the next determination method.
        /// </summary>
        Continue = 2,

        /// <summary>
        /// Use the fallback determination method.
        /// </summary>
        Fallback = 3,
        /// <summary>
        /// Determine the value from the environment.
        /// </summary>
        Environment = 4,
        /// <summary>
        /// Determine the value using Windows Forms.
        /// </summary>
        WinForms = 5,
        /// <summary>
        /// Determine the value using the framework.
        /// </summary>
        Framework = 6,

        /// <summary>
        /// Determine the value from the interpreter.
        /// </summary>
        Interpreter = 7,
        /// <summary>
        /// Determine the value from the interpreter when otherwise false.
        /// </summary>
        InterpreterIfFalse = 8,
        /// <summary>
        /// Determine the value from the interpreter when otherwise true.
        /// </summary>
        InterpreterIfTrue = 9,

        /// <summary>
        /// Optionally determine the value from the interpreter.
        /// </summary>
        MaybeInterpreter = 10,
        /// <summary>
        /// Optionally determine the value from the interpreter when otherwise false.
        /// </summary>
        MaybeInterpreterIfFalse = 11,
        /// <summary>
        /// Optionally determine the value from the interpreter when otherwise true.
        /// </summary>
        MaybeInterpreterIfTrue = 12
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: This enumeration is for use by the WinTrustOps class only.
    //          PLEASE DO NOT USE.  It is subject to change at any time.
    //
    /// <summary>
    /// This enumeration provides the parameter values used to populate the WINTRUST_DATA structure; reserved for use by the WinTrustOps class only.
    /// </summary>
    [Flags()]
    [ObjectId("cf362264-3ad5-4edf-9e57-e77482586361")]
    internal enum TrustValues : uint
    {
#if NATIVE && WINDOWS
        // WINTRUST_DATA.dwUIChoice

        /// <summary>
        /// Display all user interface prompts (WINTRUST_DATA.dwUIChoice).
        /// </summary>
        [ParameterIndex(0)]
        WTD_UI_ALL = 1,

        /// <summary>
        /// Display no user interface prompts (WINTRUST_DATA.dwUIChoice).
        /// </summary>
        [ParameterIndex(0)]
        WTD_UI_NONE = 2,

        /// <summary>
        /// Display user interface prompts only for failures (WINTRUST_DATA.dwUIChoice).
        /// </summary>
        [ParameterIndex(0)]
        WTD_UI_NOBAD = 3,

        /// <summary>
        /// Display user interface prompts only for successes (WINTRUST_DATA.dwUIChoice).
        /// </summary>
        [ParameterIndex(0)]
        WTD_UI_NOGOOD = 4,

        ///////////////////////////////////////////////////////////////////////////////////////////

        // WINTRUST_DATA.fdwRevocationChecks

        /// <summary>
        /// Perform no revocation checking (WINTRUST_DATA.fdwRevocationChecks).
        /// </summary>
        [ParameterIndex(1)]
        WTD_REVOKE_NONE = 0,

        /// <summary>
        /// Perform revocation checking on the whole certificate chain (WINTRUST_DATA.fdwRevocationChecks).
        /// </summary>
        [ParameterIndex(1)]
        WTD_REVOKE_WHOLECHAIN = 1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        // WINTRUST_DATA.dwProvFlags

        /// <summary>
        /// No provider flags (WINTRUST_DATA.dwProvFlags).
        /// </summary>
        [ParameterIndex(2)]
        WTD_NONE = 0x0,

        /// <summary>
        /// Use the legacy Internet Explorer 4 trust behavior.
        /// </summary>
        [ParameterIndex(2)]
        WTD_USE_IE4_TRUST_FLAG = 0x1,

        /// <summary>
        /// Do not use the legacy Internet Explorer 4 chain behavior.
        /// </summary>
        [ParameterIndex(2)]
        WTD_NO_IE4_CHAIN_FLAG = 0x2,

        /// <summary>
        /// Do not check certificate policy usage.
        /// </summary>
        [ParameterIndex(2)]
        WTD_NO_POLICY_USAGE_FLAG = 0x4,

        /// <summary>
        /// Perform no revocation checking.
        /// </summary>
        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_NONE = 0x10,

        /// <summary>
        /// Perform revocation checking on the end certificate only.
        /// </summary>
        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_END_CERT = 0x20,

        /// <summary>
        /// Perform revocation checking on the entire chain.
        /// </summary>
        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_CHAIN = 0x40,

        /// <summary>
        /// Perform revocation checking on the chain, excluding the root.
        /// </summary>
        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x80,

        /// <summary>
        /// Apply Software Restriction Policies (SAFER) trust checking.
        /// </summary>
        [ParameterIndex(2)]
        WTD_SAFER_FLAG = 0x100,

        /// <summary>
        /// Verify the hash only, without checking the publisher.
        /// </summary>
        [ParameterIndex(2)]
        WTD_HASH_ONLY_FLAG = 0x200,

        /// <summary>
        /// Use the default operating system version checking.
        /// </summary>
        [ParameterIndex(2)]
        WTD_USE_DEFAULT_OSVER_CHECK = 0x400,

        /// <summary>
        /// Apply lifetime signing semantics to the signature.
        /// </summary>
        [ParameterIndex(2)]
        WTD_LIFETIME_SIGNING_FLAG = 0x800,

        /// <summary>
        /// Retrieve revocation information from the local cache only.
        /// </summary>
        [ParameterIndex(2)]
        WTD_CACHE_ONLY_URL_RETRIEVAL = 0x1000,

        /// <summary>
        /// Disable the use of the MD2 and MD4 hashing algorithms.
        /// </summary>
        [ParameterIndex(2)]
        WTD_DISABLE_MD2_MD4 = 0x2000,

        /// <summary>
        /// Apply Mark-of-the-Web trust semantics.
        /// </summary>
        [ParameterIndex(2)]
        WTD_MOTW = 0x4000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        // WINTRUST_DATA.dwUIContext

        /// <summary>
        /// The verification is for executing a file (WINTRUST_DATA.dwUIContext).
        /// </summary>
        [ParameterIndex(3)]
        WTD_UICONTEXT_EXECUTE = 0,

        /// <summary>
        /// The verification is for installing a file (WINTRUST_DATA.dwUIContext).
        /// </summary>
        [ParameterIndex(3)]
        WTD_UICONTEXT_INSTALL = 1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is the number of "uint" parameters needed to fill up the
        //       WINTRUST_DATA structure passed into the WinVerifyTrust API.
        //
        /// <summary>
        /// The number of unsigned integer parameters needed to fill the WINTRUST_DATA structure.
        /// </summary>
        PARAMETER_COUNT = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the individual statistics collected while scanning a text buffer.
    /// </summary>
    [ObjectId("5ec24744-3bc0-4787-9ab5-0ee4a7a2d3ac")]
    internal enum BufferStats
    {
        /// <summary>
        /// The total length of the buffer.
        /// </summary>
        Length = 0,
        /// <summary>
        /// The number of carriage-return characters.
        /// </summary>
        CrCount = 1,
        /// <summary>
        /// The number of line-feed characters.
        /// </summary>
        LfCount = 2,
        /// <summary>
        /// The number of carriage-return and line-feed pairs.
        /// </summary>
        CrLfCount = 3,
        /// <summary>
        /// The number of lines.
        /// </summary>
        LineCount = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how strong-name and certificate verification is performed.
    /// </summary>
    [Flags()]
    [ObjectId("2c24c473-85b2-48d4-b9cb-d6741c7c9870")]
    internal enum VerifyFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Verify assemblies in the global assembly cache.
        /// </summary>
        GlobalAssemblyCache = 0x2,
        /// <summary>
        /// Ignore null items during verification.
        /// </summary>
        IgnoreNull = 0x4,

        /// <summary>
        /// Stop verification at the first error.
        /// </summary>
        StopOnError = 0x8,
        /// <summary>
        /// Stop verification at the first null item.
        /// </summary>
        StopOnNull = 0x10,

        /// <summary>
        /// Verify the entire certificate chain.
        /// </summary>
        VerifyChain = 0x20,
        /// <summary>
        /// Do not verify the certificate chain.
        /// </summary>
        NoVerifyChain = 0x40,
        /// <summary>
        /// Include verbose results.
        /// </summary>
        VerboseResults = 0x80,

        /// <summary>
        /// Marker bit selecting the default verification behavior.
        /// </summary>
        ForDefault = 0x1000,
        /// <summary>
        /// Marker bit selecting the maximum verification behavior.
        /// </summary>
        ForMaximum = 0x2000,

        /// <summary>
        /// Bit mask covering the stop-on-error and stop-on-null flags.
        /// </summary>
        StopMask = StopOnError | StopOnNull,
        /// <summary>
        /// Bit mask covering the maximum verification option flags.
        /// </summary>
        MaximumMask = GlobalAssemblyCache | VerifyChain | VerboseResults,
        /// <summary>
        /// Bit mask covering all verification option flags.
        /// </summary>
        AllMask = StopMask | MaximumMask | NoVerifyChain | VerboseResults,

        /// <summary>
        /// The maximum verification behavior.
        /// </summary>
        Maximum = MaximumMask | ForMaximum,
        /// <summary>
        /// The default verification behavior.
        /// </summary>
        Default = StopMask | ForDefault,
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how the set of debug search paths is gathered and filtered.
    /// </summary>
    [ObjectId("35f46c6a-66ce-4de5-9979-553a915b5965")]
    internal enum DebugPathFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Return all matching debug paths.
        /// </summary>
        GetAll = 0x2,
        /// <summary>
        /// Apply the path filter.
        /// </summary>
        UseFilter = 0x4,
        /// <summary>
        /// Include only paths that exist.
        /// </summary>
        ExistingOnly = 0x8,
        /// <summary>
        /// Include only unique paths.
        /// </summary>
        UniqueOnly = 0x10,

        /// <summary>
        /// The automatic behavior (filtered, existing, and unique only).
        /// </summary>
        Automatic = UseFilter | ExistingOnly | UniqueOnly,

        /// <summary>
        /// The default flags (none).
        /// </summary>
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies whether a variable operation sets or unsets a value.
    /// </summary>
    [ObjectId("c8c9371b-e788-4b2b-a13b-0bd22f07d69b")]
    internal enum SetDirection
    {
        /// <summary>
        /// No direction is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Set the value.
        /// </summary>
        Set = 0x200,
        /// <summary>
        /// Unset the value.
        /// </summary>
        Unset = 0x400
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
    /// <summary>
    /// This enumeration specifies the kind of package install or uninstall operation to perform.
    /// </summary>
    [Flags()]
    [ObjectId("dd744742-ccd6-4a21-877f-4684d249d5fe")]
    internal enum PkgInstallType : ulong
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Install the package.
        /// </summary>
        Install = 0x1000,
        /// <summary>
        /// Uninstall the package.
        /// </summary>
        Uninstall = 0x2000,

        /// <summary>
        /// Perform the operation temporarily.
        /// </summary>
        Temporary = 0x10000,
        /// <summary>
        /// Perform the operation persistently.
        /// </summary>
        Persistent = 0x20000,

        /// <summary>
        /// Marker bit selecting the default behavior.
        /// </summary>
        ForDefault = 0x10000000,

        /// <summary>
        /// Bit mask covering the install and uninstall actions.
        /// </summary>
        ActionMask = Install | Uninstall,

        /// <summary>
        /// The default install type.
        /// </summary>
        Default = None | ForDefault
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls which version components are included when formatting version information.
    /// </summary>
    [Flags()]
    [ObjectId("a1767e9d-8382-46b5-a544-f14a73cd5122")]
    internal enum VersionFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Include the vendor version.
        /// </summary>
        Vendor = 0x100,
        /// <summary>
        /// Include the core library version.
        /// </summary>
        Core = 0x200,
        /// <summary>
        /// Include the plugin versions.
        /// </summary>
        Plugins = 0x400,

        /// <summary>
        /// Allow null version components.
        /// </summary>
        AllowNull = 0x1000,

        /// <summary>
        /// Marker bit selecting the setup behavior.
        /// </summary>
        ForSetup = 0x10000,
        /// <summary>
        /// Marker bit selecting the default behavior.
        /// </summary>
        ForDefault = 0x20000,

        /// <summary>
        /// The version components used during setup.
        /// </summary>
        Setup = Vendor | Core | ForSetup,

        /// <summary>
        /// The default version components.
        /// </summary>
        Default = Vendor | Core | Plugins | AllowNull | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
    /// <summary>
    /// This enumeration controls the interactive shell kiosk mode; also semi-public via the command line.
    /// </summary>
    [Flags()]
    [ObjectId("e9e91ce3-1054-4930-8091-edf6c481e546")]
    internal enum KioskFlags /* NOTE: Actually, semi-public via command line. */
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Enable kiosk mode.
        /// </summary>
        Enable = 0x2,
        /// <summary>
        /// Use the command line arguments in kiosk mode.
        /// </summary>
        UseArgv = 0x4,
        /// <summary>
        /// The default flags (none).
        /// </summary>
        Default = None
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how interpreter update (check) processing is performed.
    /// </summary>
    [Flags()]
    [ObjectId("d62ac868-4c57-44fd-86e9-77df6fdae7ae")]
    internal enum UpdateFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Run idle tasks as part of the update.
        /// </summary>
        IdleTasks = 0x1000,
        /// <summary>
        /// Run pre-queue update processing.
        /// </summary>
        PreQueue = 0x2000,
        /// <summary>
        /// Queue the update.
        /// </summary>
        Queue = 0x4000,
        /// <summary>
        /// Run post-queue update processing.
        /// </summary>
        PostQueue = 0x8000,
        /// <summary>
        /// Count the pending updates.
        /// </summary>
        Count = 0x10000,
        /// <summary>
        /// Emit trace messages during the update.
        /// </summary>
        Trace = 0x20000,

        /// <summary>
        /// Marker bit selecting the default behavior.
        /// </summary>
        ForDefault = 0x40000,

        /// <summary>
        /// The default update flags.
        /// </summary>
        Default = Queue | PostQueue | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
    /// <summary>
    /// This enumeration specifies the kind of native package control operation to perform.
    /// </summary>
    [ObjectId("ac07a839-b5d8-420f-81de-55f4faf6591c")]
    internal enum PackageControlType
    {
        /// <summary>
        /// No control type is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Require the package.
        /// </summary>
        Require = 0x2
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration mirrors the Windows VER_NT_* product type values from WinNT.h.
    /// </summary>
    [ObjectId("ca4a4834-0084-460e-a78a-55d0bec42d96")]
    internal enum VER_PRODUCT_TYPE : byte /* NOTE: From "WinNT.h". */
    {
        /// <summary>
        /// The product type cannot be queried; possibly Linux or another non-Windows system.
        /// </summary>
        VER_NT_UNKNOWN = 0xFF,          /* Cannot query: possibly Linux, etc. */
        /// <summary>
        /// The product type has not yet been queried.
        /// </summary>
        VER_NT_NONE = 0x0,              /* Not yet queried. */
        /// <summary>
        /// A Windows workstation edition.
        /// </summary>
        VER_NT_WORKSTATION = 0x1,       /* Windows 2000/XP/7/8/8.1/10/11, etc. */
        /// <summary>
        /// A Windows Server domain controller.
        /// </summary>
        VER_NT_DOMAIN_CONTROLLER = 0x2, /* Windows Server -AND- Domain Controller */
        /// <summary>
        /// A Windows Server edition.
        /// </summary>
        VER_NT_SERVER = 0x3             /* Windows Server */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
    /// <summary>
    /// This enumeration specifies which set of XML attributes a list operation should include.
    /// </summary>
    [Flags()]
    [ObjectId("bbbeed12-5900-470a-b546-16b04009c870")]
    internal enum XmlAttributeListType
    {
        /// <summary>
        /// No attributes are selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Include engine-defined attributes.
        /// </summary>
        Engine = 0x2,
        /// <summary>
        /// Include required attributes.
        /// </summary>
        Required = 0x4,
        /// <summary>
        /// Include all attributes.
        /// </summary>
        All = 0x8
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies sets of binding flags that are resolved lazily and cached as meta member types.
    /// </summary>
    [ObjectId("4af85b57-4920-4117-9a07-d8c647e8ee3c")]
    internal enum MetaMemberTypes
    {
        /// <summary>
        /// Cached member type used by the EnumOps class for flags enumerations.
        /// </summary>
        FlagsEnum = 0,                  /* EnumOps */
        /// <summary>
        /// Cached member type used by the MarshalOps class for unsafe objects.
        /// </summary>
        UnsafeObject = 1,               /* MarshalOps */
        /// <summary>
        /// Cached member type used by the ObjectOps class for default objects.
        /// </summary>
        ObjectDefault = 2,              /* ObjectOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering the meta member type index.
        /// </summary>
        IndexMask = 0x3                 /* 0b11 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies sets of reflection binding flags that are resolved lazily and cached as meta binding flags.
    /// </summary>
    [ObjectId("0d971c64-33bd-4247-b88b-47fbc833c4c5")]
    internal enum MetaBindingFlags
    {
        /// <summary>
        /// Cached binding flags for creating an instance via a private constructor (ObjectOps).
        /// </summary>
        PrivateCreateInstance = 0,      /* ObjectOps */
        /// <summary>
        /// Cached binding flags for private instance members (MarshalOps).
        /// </summary>
        PrivateInstance = 1,            /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a private instance field (MarshalOps).
        /// </summary>
        PrivateInstanceGetField = 2,    /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a private instance property (MarshalOps).
        /// </summary>
        PrivateInstanceGetProperty = 3, /* MarshalOps */
        /// <summary>
        /// Cached binding flags for a private instance method (MarshalOps).
        /// </summary>
        PrivateInstanceMethod = 4,      /* MarshalOps */
        /// <summary>
        /// Cached binding flags for private static members (MarshalOps).
        /// </summary>
        PrivateStatic = 5,              /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a private static field (MarshalOps).
        /// </summary>
        PrivateStaticGetField = 6,      /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a private static property (MarshalOps).
        /// </summary>
        PrivateStaticGetProperty = 7,   /* MarshalOps */
        /// <summary>
        /// Cached binding flags for a private static method (MarshalOps).
        /// </summary>
        PrivateStaticMethod = 8,        /* MarshalOps */
        /// <summary>
        /// Cached binding flags for setting a private static field (MarshalOps).
        /// </summary>
        PrivateStaticSetField = 9,      /* MarshalOps */
        /// <summary>
        /// Cached binding flags for setting a private static property (MarshalOps).
        /// </summary>
        PrivateStaticSetProperty = 10,  /* MarshalOps */
        /// <summary>
        /// Cached binding flags for creating an instance via a public constructor (ObjectOps).
        /// </summary>
        PublicCreateInstance = 11,      /* ObjectOps */
        /// <summary>
        /// Cached binding flags for public instance members (DelegateOps).
        /// </summary>
        PublicInstance = 12,            /* DelegateOps */
        /// <summary>
        /// Cached binding flags for getting a public instance field (MarshalOps).
        /// </summary>
        PublicInstanceGetField = 13,    /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a public instance property (MarshalOps).
        /// </summary>
        PublicInstanceGetProperty = 14, /* MarshalOps */
        /// <summary>
        /// Cached binding flags for a public instance method (MarshalOps).
        /// </summary>
        PublicInstanceMethod = 15,      /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a public static field (MarshalOps).
        /// </summary>
        PublicStaticGetField = 16,      /* MarshalOps */
        /// <summary>
        /// Cached binding flags for getting a public static property (MarshalOps).
        /// </summary>
        PublicStaticGetProperty = 17,   /* MarshalOps */
        /// <summary>
        /// Cached binding flags for a public static method (MarshalOps).
        /// </summary>
        PublicStaticMethod = 18,        /* MarshalOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached default binding flags (MarshalOps).
        /// </summary>
        Default = 19,                   /* MarshalOps */
        /// <summary>
        /// Cached binding flags for an enumeration field (MarshalOps).
        /// </summary>
        EnumField = 20,                 /* MarshalOps */
        /// <summary>
        /// Cached binding flags for host information (MarshalOps).
        /// </summary>
        HostInfo = 21,                  /* MarshalOps */
        /// <summary>
        /// Cached binding flags for listing properties (MarshalOps).
        /// </summary>
        ListProperties = 22,            /* MarshalOps */
        /// <summary>
        /// Cached binding flags for loose method matching (MarshalOps).
        /// </summary>
        LooseMethod = 23,               /* MarshalOps */
        /// <summary>
        /// Cached binding flags for a nested object (MarshalOps).
        /// </summary>
        NestedObject = 24,              /* MarshalOps */
        /// <summary>
        /// Cached binding flags for an unsafe object (MarshalOps).
        /// </summary>
        UnsafeObject = 25,              /* MarshalOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached binding flags for the application domain identifier (AppDomainOps).
        /// </summary>
        DomainId = 26,                  /* AppDomainOps */
        /// <summary>
        /// Cached binding flags for the legacy CAS policy enabled state (AppDomainOps).
        /// </summary>
        IsLegacyCasPolicyEnabled = 27,  /* AppDomainOps */
        /// <summary>
        /// Cached binding flags for a flags enumeration (EnumOps).
        /// </summary>
        FlagsEnum = 28,                 /* EnumOps */
        /// <summary>
        /// Cached binding flags for a byte buffer (FileOps).
        /// </summary>
        ByteBuffer = 29,                /* FileOps */
        /// <summary>
        /// Cached binding flags for a host property (the default host).
        /// </summary>
        HostProperty = 30,              /* _Hosts.Default */
        /// <summary>
        /// Cached binding flags for array items (ArrayOps).
        /// </summary>
        Items = 31,                     /* ArrayOps */
        /// <summary>
        /// Cached binding flags for array size (ArrayOps).
        /// </summary>
        Size = 32,                      /* ArrayOps */
        /// <summary>
        /// Cached binding flags for the disposed field (ObjectOps).
        /// </summary>
        DisposedField = 33,             /* ObjectOps */
        /// <summary>
        /// Cached binding flags for the disposed property (ObjectOps).
        /// </summary>
        DisposedProperty = 34,          /* ObjectOps */
        /// <summary>
        /// Cached binding flags for guru-level object access (ObjectOps).
        /// </summary>
        Guru = 35,                      /* ObjectOps */
        /// <summary>
        /// Cached binding flags for raw member invocation (ObjectOps).
        /// </summary>
        InvokeRaw = 36,                 /* ObjectOps */
        /// <summary>
        /// Cached default binding flags for objects (ObjectOps).
        /// </summary>
        ObjectDefault = 37,             /* ObjectOps */
        /// <summary>
        /// Cached binding flags for a delegate (RuntimeOps).
        /// </summary>
        Delegate = 38,                  /* RuntimeOps */
        /// <summary>
        /// Cached binding flags for private socket members (SocketOps).
        /// </summary>
        SocketPrivate = 39,             /* SocketOps */
        /// <summary>
        /// Cached binding flags for public socket members (SocketOps).
        /// </summary>
        SocketPublic = 40,              /* SocketOps */
        /// <summary>
        /// Cached binding flags used by the trace plugin.
        /// </summary>
        Trace = 41,                     /* _Plugins.Trace */
        /// <summary>
        /// Cached binding flags used by the transfer helper.
        /// </summary>
        TransferHelper = 42,            /* TransferHelper */
        /// <summary>
        /// Cached binding flags for interpreter settings (SettingsOps).
        /// </summary>
        InterpreterSettings = 43,       /* SettingsOps */
        /// <summary>
        /// Cached binding flags for the default type lookup (MarshalOps).
        /// </summary>
        TypeDefaultLookup = 44,         /* MarshalOps (System.Type) */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached binding flags for a dynamic method handle (HookOps).
        /// </summary>
        DynamicMethodHandle = 45,       /* HookOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering the meta binding flags index.
        /// </summary>
        IndexMask = 0x3F                /* 0b111111 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how timeout handling is performed and which subsystem the timeout applies to.
    /// </summary>
    [Flags()]
    [ObjectId("c819b429-f7d5-4a4e-8bc2-9d633506f5a5")]
    internal enum TimeoutFlags
    {
        /// <summary>
        /// Nothing; do not use.
        /// </summary>
        None = 0x0,          /* Nothing, do not use. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,       /* Invalid, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved1 = 0x2,     /* Reserved, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved2 = 0x4,     /* Reserved, do not use. */

        /// <summary>
        /// Treat the timeout operation as interactive, allowing prompting and altering cancellation.
        /// </summary>
        Interactive = 0x100, /* Treat the current timeout
                              * operation as "interactive".
                              * This allows an interative
                              * user to be prompted -AND-
                              * may alter how any script
                              * cancellation is performed. */

        /// <summary>
        /// Allow the interpreter primary thread to be forcibly interrupted.
        /// </summary>
        Interrupt = 0x200,   /* Allow the primary thread
                              * associated with the target
                              * interpreter to be forcibly
                              * interrupted. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// For use by the WatchdogControl method only.
        /// </summary>
        ForWatchdogControl = 0x1000, /* For use by the
                                      * WatchdogControl
                                      * method only. */

        /// <summary>
        /// For use by the interactive loop methods only.
        /// </summary>
        ForInteractiveLoop = 0x2000, /* For use by the
                                      * interactive loop
                                      * methods only. */

        /// <summary>
        /// For use by the general script timeout thread.
        /// </summary>
        ForTimeout = 0x4000,         /* For use by the
                                      * general script
                                      * timeout thread.
                                      */

        /// <summary>
        /// For use by the try/finally script timeout thread.
        /// </summary>
        ForFinallyTimeout = 0x8000,  /* For use by the
                                      * try/finally
                                      * script timeout
                                      * thread. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The general script timeout behavior.
        /// </summary>
        Timeout = Reserved1 | ForTimeout,
        /// <summary>
        /// The try/finally script timeout behavior.
        /// </summary>
        FinallyTimeout = Reserved1 | ForFinallyTimeout,
        /// <summary>
        /// The interactive loop timeout behavior.
        /// </summary>
        InteractiveLoop = Reserved1 | Interactive | ForInteractiveLoop,
        /// <summary>
        /// The watchdog control timeout behavior.
        /// </summary>
        WatchdogControl = Reserved1 | ForWatchdogControl,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default timeout flags.
        /// </summary>
        Default = Reserved2 | None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies which object namespaces (CLR and/or Eagle) are considered by an operation.
    /// </summary>
    [Flags()]
    [ObjectId("34220ff8-dcff-4821-98d8-c71c74008a8f")]
    internal enum ObjectNamespace
    {
        /// <summary>
        /// Nothing; do not use.
        /// </summary>
        None = 0x0,            /* Nothing, do not use. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,         /* Invalid, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved1 = 0x2,       /* Reserved, do not use. */
        /// <summary>
        /// The namespace cannot be determined.
        /// </summary>
        Unknown = 0x4,         /* Cannot determine...? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// For use by the Interpreter class.
        /// </summary>
        ForInterpreter = 0x10, /* For use by Interpreter class. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A CLR namespace.
        /// </summary>
        Clr = 0x100,           /* e.g. namespaces of Object, etc. */
        /// <summary>
        /// An Eagle namespace.
        /// </summary>
        Eagle = 0x200,         /* e.g. namespaces of Interpreter,
                                * StringList, etc. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default object namespaces.
        /// </summary>
        Default = Clr | Eagle | ForInterpreter,
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the managed runtime hosting the current process.
    /// </summary>
    [Flags()]
    [ObjectId("c72796c0-479e-4613-bfca-8a55ec264629")]
    internal enum RuntimeName
    {
        /// <summary>
        /// Nothing; do not use.
        /// </summary>
        None = 0x0,      /* Nothing, do not use. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,   /* Invalid, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved1 = 0x2, /* Reserved, do not use. */
        /// <summary>
        /// The runtime cannot be determined.
        /// </summary>
        Unknown = 0x4,   /* Cannot determine...? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The Windows .NET Framework runtime.
        /// </summary>
        NetFx = 0x10,      /* Windows (only) .NET Framework 2.x, 4.x, etc. */
        /// <summary>
        /// The cross-platform Mono runtime.
        /// </summary>
        Mono = 0x20,       /* Cross-platform Mono, 2.x or higher. */
        /// <summary>
        /// The cross-platform .NET Core runtime.
        /// </summary>
        DotNetCore = 0x40, /* Cross-platform .NET Core, 2.0 or higher. */
        /// <summary>
        /// The cross-platform .NET runtime.
        /// </summary>
        DotNet = 0x80,     /* Cross-platform .NET, 5.0 or higher. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default runtime name.
        /// </summary>
        Default = NetFx | Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how the user home and related directories are resolved.
    /// </summary>
    [Flags()]
    [ObjectId("11e25134-9e1e-4823-95d4-675109cb3dc8")]
    internal enum HomeFlags
    {
        /// <summary>
        /// Nothing; do not use.
        /// </summary>
        None = 0x0,           /* Nothing, do not use. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,        /* Invalid, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved1 = 0x2,      /* Reserved, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved2 = 0x4,      /* Reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Use the legacy HOME environment variable.
        /// </summary>
        Legacy = 0x100,        /* This will always use the legacy "HOME"
                                * environment variable. */
        /// <summary>
        /// Use the XDG data home and data directories environment variables.
        /// </summary>
        Data = 0x200,          /* Currently, this uses the "XDG_DATA_HOME"
                                * and "XDG_DATA_DIRS" environment variables,
                                * in that order. */
        /// <summary>
        /// Use the XDG configuration home and configuration directories environment variables.
        /// </summary>
        Configuration = 0x400, /* Currently, this uses the "XDG_CONFIG_HOME"
                                * and "XDG_CONFIG_DIRS" environment variables,
                                * in that order. */
        /// <summary>
        /// Use the XDG cloud home and cloud directories environment variables.
        /// </summary>
        Cloud = 0x800,         /* Currently, this uses the "XDG_CLOUD_HOME"
                                * and "XDG_CLOUD_DIRS" environment variables,
                                * in that order. */
        /// <summary>
        /// Use the XDG startup home and startup directories environment variables.
        /// </summary>
        Startup = 0x1000,      /* Currently, this uses the "XDG_STARTUP_HOME"
                                * and "XDG_STARTUP_DIRS" environment variables,
                                * in that order. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// All of the returned directories must exist.
        /// </summary>
        Exists = 0x4000,       /* All the returned directories must exist. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering the reserved flags.
        /// </summary>
        ReservedMask = Reserved1 | Reserved2,
        /// <summary>
        /// Bit mask covering the reserved flags and the exists flag.
        /// </summary>
        FlagsMask = ReservedMask | Exists,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering all data-related home sources.
        /// </summary>
        AnyDataMask = Legacy | Data | Cloud | Startup,
        /// <summary>
        /// Bit mask covering all configuration-related home sources.
        /// </summary>
        AnyConfigurationMask = Legacy | Configuration | Cloud | Startup,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask used when getting the setup home directories.
        /// </summary>
        SetupHomeGetMask = (AnyDataMask & ~(Cloud | Startup)) | Exists | Reserved1,
        /// <summary>
        /// Bit mask used when setting the setup home directories.
        /// </summary>
        SetupHomeSetMask = Legacy | Reserved1, /* COMPAT: Tcl */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering all home directory sources.
        /// </summary>
        AnyMask = Legacy | Data | Configuration | Cloud | Startup
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
    /// <summary>
    /// This enumeration identifies the kind of native window targeted by an operation.
    /// </summary>
    [Flags()]
    [ObjectId("51c62d47-1e53-4679-a713-6ca0a8b46dad")]
    internal enum NativeWindowType
    {
        /// <summary>
        /// No window type is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// The active window.
        /// </summary>
        Active = 0x100,
        /// <summary>
        /// The console window.
        /// </summary>
        Console = 0x200,
        /// <summary>
        /// The foreground window.
        /// </summary>
        Foreground = 0x400,
        /// <summary>
        /// The shell window.
        /// </summary>
        Shell = 0x800,
        /// <summary>
        /// The desktop window.
        /// </summary>
        Desktop = 0x1000,
        /// <summary>
        /// The terminal window.
        /// </summary>
        Terminal = 0x2000,
        /// <summary>
        /// The input window.
        /// </summary>
        Input = 0x4000,
        /// <summary>
        /// The window icon.
        /// </summary>
        Icon = 0x8000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies flags used when creating a console screen buffer.
    /// </summary>
    [Flags()]
    [ObjectId("69660685-2e73-4194-aea9-6ea6cd1e5b13")]
    internal enum ConsoleScreenBufferFlags
    {
        /// <summary>
        /// Create a text-mode console screen buffer.
        /// </summary>
        CONSOLE_TEXTMODE_BUFFER = 1
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && TCL_THREADS
    /// <summary>
    /// This enumeration identifies the kind of event processed by a native Tcl worker thread.
    /// </summary>
    [ObjectId("581367c7-c7ee-44d1-8583-8fa3fb8e08a5")]
    internal enum TclThreadEvent
    {
        //
        // WARNING: The ordering of these values must match those
        //          in the ThreadStart() method of the TclThread
        //          class.
        //
        /// <summary>
        /// The worker thread should finish.
        /// </summary>
        DoneEvent = 0x0,
        /// <summary>
        /// The worker thread should perform idle processing.
        /// </summary>
        IdleEvent = 0x1,
        /// <summary>
        /// The worker thread should process its queue.
        /// </summary>
        QueueEvent = 0x2
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls which categories of cache information are reported or processed.
    /// </summary>
    [Flags()]
    [ObjectId("0cf808e6-2239-4308-b3ac-d880a7439603")]
    internal enum CacheInformationFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Include cache settings.
        /// </summary>
        Settings = 0x2,
        /// <summary>
        /// Include cache memory usage.
        /// </summary>
        Memory = 0x4,
        /// <summary>
        /// Include cache statistics.
        /// </summary>
        Statistics = 0x8,
        /// <summary>
        /// Include cache state.
        /// </summary>
        State = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Marker bit selecting the debug behavior.
        /// </summary>
        ForDebug = 0x20,
        /// <summary>
        /// Marker bit selecting the initialize behavior.
        /// </summary>
        ForInitialize = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cache information reported for debugging.
        /// </summary>
        Debug = Settings | Memory | Statistics | State |
                ForDebug,

        /// <summary>
        /// The cache information used during initialization.
        /// </summary>
        Initialize = State | ForInitialize
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration specifies whether a configuration value is queried, set, or unset.
    /// </summary>
    [Flags()]
    [ObjectId("7a2e9b30-2671-4f43-806a-745d500713ac")]
    internal enum ConfigurationOperation
    {
        /// <summary>
        /// No operation is selected.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Query a configuration value.
        /// </summary>
        Get = 0x2,
        /// <summary>
        /// Set a configuration value.
        /// </summary>
        Set = 0x4,
        /// <summary>
        /// Unset a configuration value.
        /// </summary>
        Unset = 0x8
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how configuration values are located, interpreted, and which subsystem they apply to.
    /// </summary>
    [Flags()]
    [ObjectId("dbc4da96-c186-4a57-b0ec-8f4a6929f18c")]
    internal enum ConfigurationFlags
    {
        /// <summary>
        /// No special handling.
        /// </summary>
        None = 0x0,                        /* No special handling. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,                     /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Operate on values that are not prefixed with the package name.
        /// </summary>
        Unprefixed = 0x2,                  /* Check, modify, or remove values
                                            * that are NOT prefixed with the
                                            * package name. */
        /// <summary>
        /// Operate on values that are prefixed with the package name.
        /// </summary>
        Prefixed = 0x4,                    /* Check, modify, or remove values
                                            * that are prefixed with the
                                            * package name. */
        /// <summary>
        /// Expand contained environment variables.
        /// </summary>
        Expand = 0x8,                      /* Expand contained environment
                                            * variables. */
        /// <summary>
        /// Emit diagnostic messages.
        /// </summary>
        Verbose = 0x10,                    /* Emit diagnostic messages. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Operate on environment variables.
        /// </summary>
        Environment = 0x20,                /* Check, modify, or remove
                                            * environment variables. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Operate on loaded application settings.
        /// </summary>
        AppSettings = 0x40,                /* Check, modify, or remove
                                            * loaded AppSettings. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Skip checking for the unprefixed environment variable.
        /// </summary>
        SkipUnprefixedEnvironment = 0x80,  /* Skipping checking for the
                                            * unprefixed environment
                                            * variable. */
        /// <summary>
        /// Skip checking for the unprefixed application setting.
        /// </summary>
        SkipUnprefixedAppSettings = 0x100, /* Skipping checking for the
                                            * unprefixed AppSetting. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Only set when checking whether a value exists; for internal use only.
        /// </summary>
        ExistOnly = 0x200,                 /* Only set when checking if a
                                            * value exists.  Internal use
                                            * only. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Require the value to be a valid list; otherwise, a null value is returned.
        /// </summary>
        ListValue = 0x400,                 /* Make sure the value is a
                                            * valid list; otherwise, a
                                            * null value is returned. */
        /// <summary>
        /// Require the value to be a glob pattern.
        /// </summary>
        PatternValue = 0x800,              /* Make sure the value is a
                                            * glob pattern.  Combine with
                                            * the "ListValue" flag for a
                                            * list of glob patterns. */
        /// <summary>
        /// Require the value to be a native path.
        /// </summary>
        NativePathValue = 0x1000,          /* Make sure the value is a
                                            * native path.  Combine with
                                            * the "ListValue" flag for a
                                            * list of native paths. */

        /// <summary>
        /// Require the value to be a list of glob patterns.
        /// </summary>
        PatternListValue = ListValue | PatternValue,
        /// <summary>
        /// Require the value to be a list of native paths.
        /// </summary>
        NativePathListValue = ListValue | NativePathValue,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering the standard configuration sources.
        /// </summary>
        StandardMask = Unprefixed | Prefixed | Environment | AppSettings,
        /// <summary>
        /// Bit mask of configuration sources used by the Result class.
        /// </summary>
        ResultMask = StandardMask & ~(Prefixed | AppSettings),
        /// <summary>
        /// Bit mask of configuration sources used by the Utility class.
        /// </summary>
        UtilityMask = StandardMask & ~Prefixed,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        /// <summary>
        /// For use by the CacheConfiguration class.
        /// </summary>
        ForCacheConfiguration = 0x2000,    /* For use by the CacheConfiguration class. */
#endif

#if CONSOLE
        /// <summary>
        /// For use by the ConsoleOps class.
        /// </summary>
        ForConsoleOps = 0x4000,            /* For use by the ConsoleOps class. */
#endif

        /// <summary>
        /// For use by the GlobalState class.
        /// </summary>
        ForGlobalState = 0x8000,           /* For use by the GlobalState class. */
        /// <summary>
        /// For use by the InteractiveOps class.
        /// </summary>
        ForInteractiveOps = 0x10000,       /* For use by the InteractiveOps class. */
        /// <summary>
        /// For use by the Interpreter class.
        /// </summary>
        ForInterpreter = 0x20000,          /* For use by the Interpreter class. */

#if NATIVE && WINDOWS
        /// <summary>
        /// For use by the NativeConsole class.
        /// </summary>
        ForNativeConsole = 0x40000,        /* For use by the NativeConsole class. */
#endif

#if NATIVE && TCL && NATIVE_PACKAGE
        /// <summary>
        /// For use by the NativePackage class.
        /// </summary>
        ForNativePackage = 0x80000,        /* For use by the NativePackage class. */
#endif

#if NATIVE
        /// <summary>
        /// For use by the NativeStack class.
        /// </summary>
        ForNativeStack = 0x100000,         /* For use by the NativeStack class. */
#endif

#if NATIVE && NATIVE_UTILITY
        /// <summary>
        /// For use by the NativeUtility class.
        /// </summary>
        ForNativeUtility = 0x200000,       /* For use by the NativeUtility class. */
#endif

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// For use by the PackageOps class.
        /// </summary>
        ForPackageOps = 0x400000,          /* For use by the PackageOps class. */
#endif

        /// <summary>
        /// For use by the PathOps class.
        /// </summary>
        ForPathOps = 0x800000,             /* For use by the PathOps class. */
        /// <summary>
        /// For use by the SetupOps class.
        /// </summary>
        ForSetupOps = 0x1000000,           /* For use by the SetupOps class. */
        /// <summary>
        /// For use by the Utility class.
        /// </summary>
        ForUtility = 0x2000000,            /* For use by the Utility class. */
        /// <summary>
        /// For use by the ScriptOps class.
        /// </summary>
        ForScriptOps = 0x4000000,          /* For use by the ScriptOps class. */
        /// <summary>
        /// For use by the Result class.
        /// </summary>
        ForResult = 0x8000000,             /* For use by the Result class. */
        /// <summary>
        /// For use by the WebOps class.
        /// </summary>
        ForWebOps = 0x10000000,            /* For use by the WebOps class. */

#if NATIVE && WINDOWS
        /// <summary>
        /// For use by the WinTrustOps class.
        /// </summary>
        ForWinTrustOps = 0x20000000,       /* For use by the WinTrustOps class. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        /// <summary>
        /// The configuration flags used by the CacheConfiguration class.
        /// </summary>
        CacheConfiguration = StandardMask | ForCacheConfiguration,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        /// <summary>
        /// The configuration flags used by the ConsoleOps class.
        /// </summary>
        ConsoleOps = StandardMask | ForConsoleOps,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the GlobalState class.
        /// </summary>
        GlobalState = StandardMask | ForGlobalState,
        /// <summary>
        /// The GlobalState configuration flags without the prefixed flag.
        /// </summary>
        GlobalStateNoPrefix = GlobalState & ~Prefixed,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the InteractiveOps class.
        /// </summary>
        InteractiveOps = StandardMask | ForInteractiveOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the Interpreter class.
        /// </summary>
        Interpreter = StandardMask | ForInterpreter,
        /// <summary>
        /// The verbose configuration flags used by the Interpreter class.
        /// </summary>
        InterpreterVerbose = Verbose | Interpreter,
        /// <summary>
        /// The Interpreter configuration flags that skip the unprefixed environment variable.
        /// </summary>
        InterpreterSkipUnprefixedEnvironment = Interpreter | SkipUnprefixedEnvironment,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// The configuration flags used by the PackageOps class.
        /// </summary>
        PackageOps = StandardMask | ForPackageOps,
        /// <summary>
        /// The PackageOps configuration flags without the prefixed flag.
        /// </summary>
        PackageOpsNoPrefix = PackageOps & ~Prefixed,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the PathOps class.
        /// </summary>
        PathOps = StandardMask | ForPathOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the SetupOps class.
        /// </summary>
        SetupOps = StandardMask | ForSetupOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the ScriptOps class.
        /// </summary>
        ScriptOps = StandardMask | ForScriptOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the Result class.
        /// </summary>
        Result = ResultMask | ForResult,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the Utility class.
        /// </summary>
        Utility = UtilityMask | ForUtility,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The configuration flags used by the WebOps class.
        /// </summary>
        WebOps = StandardMask | ForWebOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// The configuration flags used by the WinTrustOps class.
        /// </summary>
        WinTrustOps = StandardMask | ForWinTrustOps,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// The configuration flags used by the NativeConsole class.
        /// </summary>
        NativeConsole = StandardMask | ForNativeConsole,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
        /// <summary>
        /// The configuration flags used by the NativePackage class.
        /// </summary>
        NativePackage = StandardMask | ForNativePackage,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// The configuration flags used by the NativeStack class.
        /// </summary>
        NativeStack = StandardMask | ForNativeStack,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        /// <summary>
        /// The configuration flags used by the NativeUtility class.
        /// </summary>
        NativeUtility = StandardMask | ForNativeUtility,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default configuration flags.
        /// </summary>
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how garbage collection is requested, including collection, compaction, and waiting behavior.
    /// </summary>
    [Flags()]
    [ObjectId("27e1fa59-a2fc-44db-8c52-155c6b18fbed")]
    internal enum GarbageFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved1 = 0x2,
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved2 = 0x4,
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved3 = 0x8,
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved4 = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Never perform a garbage collection.
        /// </summary>
        NeverCollect = 0x20,
        /// <summary>
        /// Always perform a garbage collection.
        /// </summary>
        AlwaysCollect = 0x40,
        /// <summary>
        /// Perform a garbage collection if appropriate.
        /// </summary>
        MaybeCollect = 0x80,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        /// <summary>
        /// Never compact the large object heap.
        /// </summary>
        NeverCompact = 0x100,
        /// <summary>
        /// Always compact the large object heap.
        /// </summary>
        AlwaysCompact = 0x200,
        /// <summary>
        /// Compact the large object heap if appropriate.
        /// </summary>
        MaybeCompact = 0x400,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Never wait for pending finalizers.
        /// </summary>
        NeverWait = 0x800,
        /// <summary>
        /// Always wait for pending finalizers.
        /// </summary>
        AlwaysWait = 0x1000,
        /// <summary>
        /// Wait for pending finalizers if appropriate.
        /// </summary>
        MaybeWait = 0x2000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        /// <summary>
        /// Compact the large object heap when supported by the runtime.
        /// </summary>
        WhenPossibleCompact = AlwaysCompact,
        /// <summary>
        /// Compact the large object heap when supported and appropriate.
        /// </summary>
        MaybeWhenPossibleCompact = MaybeCompact,
#else
        WhenPossibleCompact = None,
        MaybeWhenPossibleCompact = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The garbage collection behavior used after a command.
        /// </summary>
        ForCommand = Reserved1 | AlwaysCollect |
                     WhenPossibleCompact | AlwaysWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The garbage collection behavior used by the engine.
        /// </summary>
        ForEngine = Reserved2 | AlwaysCollect |
                    WhenPossibleCompact | NeverWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The garbage collection behavior used during unload.
        /// </summary>
        ForUnload = Reserved3 | AlwaysCollect |
                    WhenPossibleCompact | AlwaysWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default garbage collection behavior.
        /// </summary>
        Default = Reserved4 | MaybeCollect |
                  MaybeWhenPossibleCompact | MaybeWait
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration controls how a time duration is formatted into a human-readable form.
    /// </summary>
    [Flags()]
    [ObjectId("a46be931-fb82-4bbd-9dfd-0e67a9914af0")]
    internal enum DurationFlags : ulong
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,

        /// <summary>
        /// Format the duration in a human-readable form.
        /// </summary>
        Human = 0x1000,
        /// <summary>
        /// Include the names of the time units.
        /// </summary>
        WithNames = 0x2000,
        /// <summary>
        /// Use plural unit names only.
        /// </summary>
        PluralOnly = 0x4000,
        /// <summary>
        /// Format the duration as a list.
        /// </summary>
        AsList = 0x8000,

        /// <summary>
        /// Include weeks in the duration.
        /// </summary>
        IncludeWeeks = 0x10000,
        /// <summary>
        /// Include months in the duration.
        /// </summary>
        IncludeMonths = 0x20000,
        /// <summary>
        /// Include milliseconds in the duration.
        /// </summary>
        IncludeMilliseconds = 0x40000,

        /// <summary>
        /// Approximate the length of months.
        /// </summary>
        ApproximateMonths = 0x80000,
        /// <summary>
        /// Approximate the length of years.
        /// </summary>
        ApproximateYears = 0x100000,

        /// <summary>
        /// Count leap days in the duration.
        /// </summary>
        CountLeapDays = 0x200000,

        /// <summary>
        /// Do not join the duration components.
        /// </summary>
        NoJoin = 0x400000,
        /// <summary>
        /// Do not include a prefix.
        /// </summary>
        NoPrefix = 0x800000,
        /// <summary>
        /// Do not include a suffix.
        /// </summary>
        NoSuffix = 0x1000000,

        /// <summary>
        /// Format the duration precisely.
        /// </summary>
        Precise = 0x2000000,
        /// <summary>
        /// Include the iteration count.
        /// </summary>
        IncludeIterations = 0x4000000,

        /// <summary>
        /// Marker bit selecting the default behavior.
        /// </summary>
        ForDefault = 0x80000000,

        /// <summary>
        /// The full human-readable duration format.
        /// </summary>
        FullHuman = Human | WithNames | IncludeWeeks | IncludeMonths |
                    ApproximateMonths | CountLeapDays,

        /// <summary>
        /// The default duration flags.
        /// </summary>
        Default = ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies the kind of object being disposed and the phase of the disposal process.
    /// </summary>
    [Flags()]
    [ObjectId("2fc84796-ec90-471c-ad91-6d5f218c3102")]
    internal enum DisposalPhase : ulong
    {
        /// <summary>
        /// No special handling.
        /// </summary>
        None = 0x0,                      // No special handling.
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,                   // Invalid, do not use.
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved = 0x2,                  // Reserved, do not use.
        /// <summary>
        /// A native plugin, command, or similar entity.
        /// </summary>
        Native = 0x4,                    // Native plugin, command, etc.
        /// <summary>
        /// A managed plugin, command, or similar entity.
        /// </summary>
        Managed = 0x8,                   // Managed plugin, command, etc.
        /// <summary>
        /// A non-system plugin, command, or similar entity.
        /// </summary>
        User = 0x10,                     // Non-system plugin, command, etc.
        /// <summary>
        /// A system plugin, command, or similar entity.
        /// </summary>
        System = 0x20,                   // System plugin, command, etc.

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// An IPlugin object.
        /// </summary>
        Plugin = 0x100,                  // An IPlugin object.
        /// <summary>
        /// An ICommand object.
        /// </summary>
        Command = 0x200,                 // An ICommand object.
        /// <summary>
        /// An IFunction object.
        /// </summary>
        Function = 0x400,                // An IFunction object.
        /// <summary>
        /// An IOperator object.
        /// </summary>
        Operator = 0x800,                // An IOperator object.
        /// <summary>
        /// An INamespace object.
        /// </summary>
        Namespace = 0x1000,              // An INamespace object.
        /// <summary>
        /// An IResolver object.
        /// </summary>
        Resolver = 0x2000,               // An IResolver object.
        /// <summary>
        /// An IPolicy object.
        /// </summary>
        Policy = 0x4000,                 // An IPolicy object.
        /// <summary>
        /// An ITrace object.
        /// </summary>
        Trace = 0x8000,                  // An ITrace object.
        /// <summary>
        /// An IEventManager object.
        /// </summary>
        EventManager = 0x10000,          // An IEventManager object.
        /// <summary>
        /// A random number generator object.
        /// </summary>
        RandomNumberGenerator = 0x20000, // An RNG of some kind.
        /// <summary>
        /// An IDebugger object.
        /// </summary>
        Debugger = 0x40000,              // An IDebugger object.
        /// <summary>
        /// An ICallFrame (scope) object.
        /// </summary>
        Scope = 0x80000,                 // An ICallFrame object.
        /// <summary>
        /// An IAlias object.
        /// </summary>
        Alias = 0x100000,                // An IAlias object.
        /// <summary>
        /// An ADO.NET database object.
        /// </summary>
        Database = 0x200000,             // An ADO.NET object.
        /// <summary>
        /// A channel or encoding object.
        /// </summary>
        Channel = 0x400000,              // A channel or encoding object.
        /// <summary>
        /// An IObject or related object.
        /// </summary>
        Object = 0x800000,               // An IObject or related object.
        /// <summary>
        /// A trusted type, URI, path, or similar item.
        /// </summary>
        Trusted = 0x1000000,             // A trusted type, URI, path, etc.
        /// <summary>
        /// An IProcedure object.
        /// </summary>
        Procedure = 0x2000000,           // An IProcedure object.
        /// <summary>
        /// An IExecute object.
        /// </summary>
        Execute = 0x4000000,             // An IExecute object.
        /// <summary>
        /// An ICallback object.
        /// </summary>
        Callback = 0x8000000,            // An ICallback object.
        /// <summary>
        /// An IPackage object.
        /// </summary>
        Package = 0x10000000,            // An IPackage object.
        /// <summary>
        /// A System.Threading.Thread object.
        /// </summary>
        Thread = 0x20000000,             // System.Threading.Thread object.
        /// <summary>
        /// An IInterpreter object.
        /// </summary>
        Interpreter = 0x40000000,        // An IInterpreter object.
        /// <summary>
        /// A System.AppDomain object.
        /// </summary>
        AppDomain = 0x80000000,          // System.AppDomain object.
        /// <summary>
        /// A native delegate or module.
        /// </summary>
        NativeLibrary = 0x100000000,     // A native delegate or module.
        /// <summary>
        /// The native Tcl integration subsystem.
        /// </summary>
        NativeTcl = 0x200000000,         // Native Tcl integration subsystem.
        /// <summary>
        /// Another delegate-based callback.
        /// </summary>
        Delegate = 0x400000000,          // Other delegate-based callbacks.

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposal phase zero.
        /// </summary>
        Phase0 = 0x1000000000,
        /// <summary>
        /// Disposal phase one.
        /// </summary>
        Phase1 = 0x2000000000,
        /// <summary>
        /// Disposal phase two.
        /// </summary>
        Phase2 = 0x4000000000,
        /// <summary>
        /// Disposal phase three.
        /// </summary>
        Phase3 = 0x8000000000,
        /// <summary>
        /// Disposal phase four.
        /// </summary>
        Phase4 = 0x10000000000,
        /// <summary>
        /// Disposal phase five.
        /// </summary>
        Phase5 = 0x20000000000,
        /// <summary>
        /// Disposal phase six.
        /// </summary>
        Phase6 = 0x40000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Bit mask covering the variable-related object kinds.
        /// </summary>
        VariableMask = Namespace | Resolver | Trace,

        /// <summary>
        /// Bit mask covering the non-base object kinds.
        /// </summary>
        NonBaseMask = Function | Operator | EventManager |
                      RandomNumberGenerator | Debugger |
                      Scope | Alias | Database | Channel |
                      Object | Trusted | Procedure | Execute |
                      Callback | Package | Thread | Interpreter |
                      AppDomain | NativeLibrary | NativeTcl |
                      Delegate,

        /// <summary>
        /// Bit mask covering all object kinds.
        /// </summary>
        All = Plugin | Command | Function | Operator |
              Namespace | Resolver | Policy | Trace |
              EventManager | RandomNumberGenerator |
              Debugger | Scope | Alias | Database |
              Channel | Object | Trusted | Procedure |
              Execute | Callback | Package | Thread |
              Interpreter | AppDomain | NativeLibrary |
              NativeTcl | Delegate,

        /// <summary>
        /// Bit mask combining phase zero with its associated object kinds.
        /// </summary>
        Phase0Mask = Phase0 | Reserved,
        /// <summary>
        /// Bit mask combining phase one with its associated object kinds.
        /// </summary>
        Phase1Mask = Phase1 | Native | User,
        /// <summary>
        /// Bit mask combining phase two with its associated object kinds.
        /// </summary>
        Phase2Mask = Phase2 | Native | System,
        /// <summary>
        /// Bit mask combining phase three with its associated object kinds.
        /// </summary>
        Phase3Mask = Phase3 | Managed | User,
        /// <summary>
        /// Bit mask combining phase four with its associated object kinds.
        /// </summary>
        Phase4Mask = Phase4 | Managed | System
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
    //
    // WARNING: Reserved as a placeholder by the core library to represent all
    //          non-flags enumerated types defined by plugins loaded into
    //          isolated application domains.  Do not modify.
    //
    /// <summary>
    /// This enumeration is a reserved placeholder representing all non-flags enumerated types defined by plugins loaded into isolated application domains.
    /// </summary>
    [ObjectId("9829bd1e-25bb-4445-bc00-5ae4dbcd8ab5")]
    internal enum StubEnum
    {
        //
        // HACK: Every enum type must have at least one value and zero is
        //       always implicitly allowed anyhow [by the CLR]; therefore,
        //       this just formalizes that behavior.
        //
        /// <summary>
        /// The only value; present because every enumeration must define at least one value.
        /// </summary>
        None = 0x0
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: Reserved as a placeholder by the core library to represent all
    //          flags enumerated types defined by plugins loaded into isolated
    //          application domains.  Do not modify.
    //
    /// <summary>
    /// This enumeration is a reserved placeholder representing all flags enumerated types defined by plugins loaded into isolated application domains.
    /// </summary>
    [Flags()]
    [ObjectId("a4e04c3a-bd77-426e-8149-9da823537be2")]
    internal enum StubFlagsEnum
    {
        //
        // HACK: Every enum type must have at least one value and zero is
        //       always implicitly allowed anyhow [by the CLR]; therefore,
        //       this just formalizes that behavior.
        //
        /// <summary>
        /// The only value; present because every enumeration must define at least one value.
        /// </summary>
        None = 0x0
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration classifies a floating-point value as defined by TIP #521.
    /// </summary>
    [ObjectId("609f71c2-9bad-4561-9725-71b47eb928cb")]
    internal enum FloatingPointClass /* TIP #521 */
    {
        /// <summary>
        /// The value is not a number.
        /// </summary>
        NaN = 0,
        /// <summary>
        /// The value is infinite.
        /// </summary>
        Infinite = 1,
        /// <summary>
        /// The value is zero.
        /// </summary>
        Zero = 2,
        /// <summary>
        /// The value is subnormal (denormalized).
        /// </summary>
        SubNormal = 3,
        /// <summary>
        /// The value is a normal number.
        /// </summary>
        Normal = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration represents the transient per-interpreter (and sometimes per-thread) state flags used internally by the engine.
    /// </summary>
    [Flags()]
    [ObjectId("956f972f-4c63-4009-a142-98e1765fc752")]
    internal enum InterpreterStateFlags : ulong
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0x0,                        /* No flags. */
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 0x1,                     /* Invalid, do not use. */
        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved1 = 0x2,                   /* Reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The interpreter is pending cleanup when the current evaluation stack is unwound.
        /// </summary>
        PendingCleanup = 0x4,              /* Interpreter is pending cleanup when
                                            * the current evaluation stack is
                                            * unwound (delete all commands,
                                            * procedures, and global variables).
                                            * This flag is used by the namespace
                                            * deletion subsystem. */
        /// <summary>
        /// The interpreter is shared with an external component and must not be disposed.
        /// </summary>
        Shared = 0x8,                      /* The interpreter is shared with an
                                            * external component and must not be
                                            * disposed.  This flag is used by the
                                            * [interp shareinterp] sub-command. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Skip all command and file policy checks to prevent unwanted mutual recursion.
        /// </summary>
        PendingPolicies = 0x10,            /* Skip all command and file policy
                                            * checks?  This is used internally to
                                            * prevent unwanted mutual recursion. */
        /// <summary>
        /// Skip all variable traces to prevent unwanted mutual recursion.
        /// </summary>
        PendingTraces = 0x20,              /* Skip all variable traces?  This is
                                            * used internally to prevent unwanted
                                            * mutual recursion. */
        /// <summary>
        /// Skip searching for package indexes to prevent unwanted mutual recursion.
        /// </summary>
        PendingPackageIndexes = 0x40,      /* Skip searching for package indexes.
                                            * This flag prevents a package index
                                            * that modifies the auto-path from
                                            * triggering a nested package index
                                            * search.  This is used internally to
                                            * prevent unwanted mutual recursion. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Security was successfully enabled by the EnableOrDisableSecurity method.
        /// </summary>
        SecurityWasEnabled = 0x80,         /* The ScriptOps.EnableOrDisableSecurity
                                            * method successfully enabled security.
                                            */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUG
        /// <summary>
        /// Throw an exception if the call stack appears to be in an invalid state.
        /// </summary>
        StrictCallStack = 0x100,           /* Throw an exception if the call stack
                                            * appears to be in an invalid state? */
#endif
        /// <summary>
        /// Keep track of all script locations, not just those pushed by the source command.
        /// </summary>
        ScriptLocation = 0x200,            /* Keep track of all script locations;
                                            * if not set, only those pushed by
                                            * [source] are tracked. */
        /// <summary>
        /// Throw an exception when asked to push or pop a script location that is not available.
        /// </summary>
        StrictScriptLocation = 0x400,      /* Throw an exception if called upon
                                            * to push or pop a script location
                                            * when they are not available (i.e.
                                            * null). */
#if DEBUGGER && DEBUGGER_BREAKPOINTS
        /// <summary>
        /// Per-thread: keep track of argument locations.
        /// </summary>
        ArgumentLocation = 0x800,          /* PER-THREAD: Keep track of Argument
                                            *             locations. */
        /// <summary>
        /// Per-thread: do not modify the argument-location flag automatically.
        /// </summary>
        ArgumentLocationLock = 0x1000,     /* PER-THREAD: Do not modify the
                                            *             ArgumentLocation flag
                                            *             automatically (e.g. via the
                                            *             [source] command, etc). */
#endif
#if SCRIPT_ARGUMENTS
        /// <summary>
        /// Keep track of script argument lists for all nested command invocations.
        /// </summary>
        ScriptArguments = 0x2000,          /* Keep track of script argument lists for
                                            * all nested command invocations. */
        /// <summary>
        /// Throw an exception when asked to push or pop a script argument list that is not available.
        /// </summary>
        StrictScriptArguments = 0x4000,    /* Throw an exception if called upon to push
                                            * or pop a script argument list when they
                                            * are not available (i.e. null). */
#endif
        /// <summary>
        /// The profiler instance may be reused for non-engine operations.
        /// </summary>
        ReUseProfiler = 0x8000,            /* The profiler instance associated with the
                                            * interpreter may be reused for non-engine
                                            * operations. */
#if ISOLATED_PLUGINS
        /// <summary>
        /// Do not notify plugins loaded into isolated application domains of interpreter events.
        /// </summary>
        NoIsolatedNotify = 0x10000,        /* Any plugins that are loaded into isolated
                                            * application domains should not be notified
                                            * of any interpreter events. */
#endif
#if SHELL
        /// <summary>
        /// The interactive shell is currently operating in kiosk mode.
        /// </summary>
        KioskLock = 0x20000,               /* The interactive shell is currently operating
                                            * in "kiosk" mode. */
        /// <summary>
        /// Use the interpreter argv before reentering the interactive loop.
        /// </summary>
        KioskArgv = 0x40000,               /* Grab the $argv from the interpreter and make
                                            * use of it before reentering the interactive
                                            * loop. */
#endif
        /// <summary>
        /// The interpreter is important and may consume more resources for higher performance.
        /// </summary>
        HighPriority = 0x80000,            /* This interpreter instance is important to
                                            * the overall application or process.  This
                                            * flag MAY cause the interpreter to consume
                                            * more resources in the pursuit of a higher
                                            * level of performance. */
        /// <summary>
        /// Skip adding the object trace callback when the value is not an opaque object handle.
        /// </summary>
        AutoTraceObject = 0x100000,        /* Skip adding the ObjectTraceCallback to the
                                            * list of traces for a variable if the value
                                            * does not currently represent an opaque
                                            * object handle. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace text writer is owned by the interpreter and should be disposed.
        /// </summary>
        TraceTextWriterOwned = 0x200000,   /* The TraceTextWriter is owned by the current
                                            * interpreter and should be disposed. */
        /// <summary>
        /// The debug text writer is owned by the interpreter and should be disposed.
        /// </summary>
        DebugTextWriterOwned = 0x400000,   /* The DebugTextWriter is owned by the current
                                            * interpreter and should be disposed. */
        /// <summary>
        /// Prevent the interpreter from actually being disposed.
        /// </summary>
        NoDispose = 0x800000,              /* Prevent the interpreter from actually being
                                            * disposed. */

#if SHELL
        /// <summary>
        /// Perform shell script library initialization when entering the interactive loop.
        /// </summary>
        InitializeShell = 0x2000000,       /* Perform shell script library initialization
                                            * when entering the interactive loop. */
        /// <summary>
        /// Disable use of the host ReadLine method by the interactive loop.
        /// </summary>
        ReadLineDisabled = 0x4000000,      /* Disable use of the IHost.ReadLine method for
                                            * use by the interactive loop.  This means the
                                            * interactive loop will not read any input from
                                            * the interactive user, i.e. any input must be
                                            * pre-queued via the IDebugger interface. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Emit trace messages when the state flags are queried.
        /// </summary>
        TraceStateQuery = 0x8000000,       /* SPECIAL: Emit trace messages when the state
                                            * flags are being queried. */
        /// <summary>
        /// Emit trace messages when the state flags are changed.
        /// </summary>
        TraceStateChange = 0x10000000,     /* SPECIAL: Emit trace messages when the state
                                            * flags are being changed. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Mark all newly created packages as temporary.
        /// </summary>
        TemporaryPackages = 0x20000000,    /* When set, all newly created packages should
                                            * be marked as temporary. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A script file from the core script library is being evaluated.
        /// </summary>
        LibraryScriptPending = 0x40000000, /* When set, a script (file) from the core script
                                            * library is being evaluated. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The trusted remote script bundle was successfully evaluated.
        /// </summary>
        TrustedRemoteOk = 0x80000000,      /* When set, the trusted remote script bundle has
                                            * been successfully evaluated. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// Prevent the debugger from exiting.
        /// </summary>
        NoExit = 0x100000000,
        /// <summary>
        /// Ignore the debugger enabled state.
        /// </summary>
        IgnoreEnabled = 0x200000000,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Marker bit selecting the default state flags.
        /// </summary>
        ForDefaultUse = 0x400000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reserved; do not use.
        /// </summary>
        Reserved = 0x8000000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUG
        /// <summary>
        /// Optionally enable strict call stack checking, depending on the build.
        /// </summary>
        MaybeStrictCallStack = StrictCallStack | Reserved1,
#else
        MaybeStrictCallStack = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        /// <summary>
        /// Optionally suppress isolated plugin notifications, depending on the build.
        /// </summary>
        MaybeNoIsolatedNotify = NoIsolatedNotify | Reserved1,
#else
        MaybeNoIsolatedNotify = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// Optionally initialize the shell, depending on the build.
        /// </summary>
        MaybeInitializeShell = InitializeShell | Reserved1,
#else
        MaybeInitializeShell = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default interpreter state flags.
        /// </summary>
        Default = (MaybeStrictCallStack | ReUseProfiler |
                   TraceTextWriterOwned | DebugTextWriterOwned |
                   MaybeNoIsolatedNotify | MaybeInitializeShell |
                   TraceStateChange | ForDefaultUse) & ~Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This enumeration identifies each built-in command and sub-command that has an associated set of cached options.
    /// </summary>
    [ObjectId("543b0a27-c906-470b-91a6-8532843342c1")]
    internal enum CommandOptionType
    {
        /// <summary>
        /// No command options are selected.
        /// </summary>
        None = 0,
        /// <summary>
        /// Invalid; this value is reserved and must not be used.
        /// </summary>
        Invalid = 1,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                               [after] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [after idle] command.
        /// </summary>
        After_Idle = 2,
        /// <summary>
        /// Cached options for the [after info] command.
        /// </summary>
        After_Info = 3,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                               [array] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [array copy] command.
        /// </summary>
        Array_Copy = 4,
        /// <summary>
        /// Cached options for the [array random] command.
        /// </summary>
        Array_Random = 5,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [base64] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [base64 decode] command.
        /// </summary>
        Base64_Decode = 6,
        /// <summary>
        /// Cached options for the [base64 encode] command.
        /// </summary>
        Base64_Encode = 7,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                            [callback] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

#if CALLBACK_QUEUE
        /// <summary>
        /// Cached options for the [callback dequeue] command.
        /// </summary>
        Callback_Dequeue = 8,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [clock] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [clock days] command.
        /// </summary>
        Clock_Days = 9,
        /// <summary>
        /// Cached options for the [clock clicks] command.
        /// </summary>
        Clock_Clicks = 10,
        /// <summary>
        /// Cached options for the [clock duration] command.
        /// </summary>
        Clock_Duration = 11,
        /// <summary>
        /// Cached options for the [clock file time] command.
        /// </summary>
        Clock_FileTime = 12,
        /// <summary>
        /// Cached options for the [clock format] command.
        /// </summary>
        Clock_Format = 13,
        /// <summary>
        /// Cached options for the [clock now] command.
        /// </summary>
        Clock_Now = 14,
        /// <summary>
        /// Cached options for the [clock scan] command.
        /// </summary>
        Clock_Scan = 15,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [debug] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [debug break] command.
        /// </summary>
        Debug_Break = 16,
        /// <summary>
        /// Cached options for the [debug emergency] command.
        /// </summary>
        Debug_Emergency = 17,
        /// <summary>
        /// Cached options for the [debug hook] command.
        /// </summary>
        Debug_Hook = 18,

#if DEBUGGER
        /// <summary>
        /// Cached options for the [debug iqueue] command.
        /// </summary>
        Debug_Iqueue = 19,
#endif

        /// <summary>
        /// Cached options for the [debug log] command.
        /// </summary>
        Debug_Log = 20,
        /// <summary>
        /// Cached options for the [debug secure eval] command.
        /// </summary>
        Debug_SecureEval = 21,
        /// <summary>
        /// Cached options for the [debug set] command.
        /// </summary>
        Debug_Set = 22,

#if SHELL
        /// <summary>
        /// Cached options for the [debug shell] command.
        /// </summary>
        Debug_Shell = 23,
#endif

#if DEBUGGER
        /// <summary>
        /// Cached options for the [debug subst] command.
        /// </summary>
        Debug_Subst = 24,
#endif

        /// <summary>
        /// Cached options for the [debug trace] command.
        /// </summary>
        Debug_Trace = 25,
        /// <summary>
        /// Cached options for the [debug variable] command.
        /// </summary>
        Debug_Variable = 26,

#if PREVIOUS_RESULT
        /// <summary>
        /// Cached options for the [debug exception] command.
        /// </summary>
        Debug_Exception = 27,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                           [debugger] command options
        //                        (from InteractiveOps commands)
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [debugger dsubst] command.
        /// </summary>
        Debugger_Dsubst = 28,
        /// <summary>
        /// Cached options for the [debugger overr] command.
        /// </summary>
        Debugger_Overr = 29,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [exec] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [exec] command.
        /// </summary>
        Exec = 30,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [exit] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [exit] command.
        /// </summary>
        Exit = 31,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                          [fconfigure] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [fconfigure set] command.
        /// </summary>
        Fconfigure_Set = 32,
        /// <summary>
        /// Cached options for the [fconfigure query] command.
        /// </summary>
        Fconfigure_Query = 33,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [fcopy] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [fcopy] command.
        /// </summary>
        Fcopy = 34,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [file] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [file cleanup] command.
        /// </summary>
        File_Cleanup = 35,
        /// <summary>
        /// Cached options for the [file copy] command.
        /// </summary>
        File_Copy = 36,
        /// <summary>
        /// Cached options for the [file delete] command.
        /// </summary>
        File_Delete = 37,
        /// <summary>
        /// Cached options for the [file glob] command.
        /// </summary>
        File_Glob = 38,
        /// <summary>
        /// Cached options for the [file information] command.
        /// </summary>
        File_Information = 39,
        /// <summary>
        /// Cached options for the [file normalize] command.
        /// </summary>
        File_Normalize = 40,
        /// <summary>
        /// Cached options for the [file object id] command.
        /// </summary>
        File_ObjectId = 41,
        /// <summary>
        /// Cached options for the [file rename] command.
        /// </summary>
        File_Rename = 42,

#if !NET_STANDARD_20 && !MONO
        /// <summary>
        /// Cached options for the [file sddl] command.
        /// </summary>
        File_Sddl = 43,
#endif

        /// <summary>
        /// Cached options for the [file under] command.
        /// </summary>
        File_Under = 44,
        /// <summary>
        /// Cached options for the [file version] command.
        /// </summary>
        File_Version = 45,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [gets] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [gets] command.
        /// </summary>
        Gets = 46,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [glob] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [glob] command.
        /// </summary>
        Glob = 47,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [hash] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [hash keyed] command.
        /// </summary>
        Hash_Keyed = 48,
        /// <summary>
        /// Cached options for the [hash mac] command.
        /// </summary>
        Hash_Mac = 49,
        /// <summary>
        /// Cached options for the [hash normal] command.
        /// </summary>
        Hash_Normal = 50,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [host] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [host beep] command.
        /// </summary>
        Host_Beep = 51,
        /// <summary>
        /// Cached options for the [host color] command.
        /// </summary>
        Host_Color = 52,

#if CONSOLE && NATIVE && WINDOWS
        /// <summary>
        /// Cached options for the [host font] command.
        /// </summary>
        Host_Font = 53,
#endif

        /// <summary>
        /// Cached options for the [host named color] command.
        /// </summary>
        Host_NamedColor = 54,
        /// <summary>
        /// Cached options for the [host position] command.
        /// </summary>
        Host_Position = 55,
        /// <summary>
        /// Cached options for the [host reset] command.
        /// </summary>
        Host_Reset = 56,
        /// <summary>
        /// Cached options for the [host size] command.
        /// </summary>
        Host_Size = 57,
        /// <summary>
        /// Cached options for the [host write box] command.
        /// </summary>
        Host_WriteBox = 58,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [info] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [info commands] command.
        /// </summary>
        Info_Commands = 59,
        /// <summary>
        /// Cached options for the [info functions] command.
        /// </summary>
        Info_Functions = 60,
        /// <summary>
        /// Cached options for the [info loaded] command.
        /// </summary>
        Info_Loaded = 61,
        /// <summary>
        /// Cached options for the [info operators] command.
        /// </summary>
        Info_Operators = 62,
        /// <summary>
        /// Cached options for the [info sub commands] command.
        /// </summary>
        Info_SubCommands = 63,
        /// <summary>
        /// Cached options for the [info vars] command.
        /// </summary>
        Info_Vars = 64,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [interp] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [interp add commands] command.
        /// </summary>
        Interp_AddCommands = 65,
        /// <summary>
        /// Cached options for the [interp cancel] command.
        /// </summary>
        Interp_Cancel = 66,
        /// <summary>
        /// Cached options for the [interp create] command.
        /// </summary>
        Interp_Create = 67,
        /// <summary>
        /// Cached options for the [interp invoke hidden] command.
        /// </summary>
        Interp_InvokeHidden = 68,
        /// <summary>
        /// Cached options for the [interp policy] command.
        /// </summary>
        Interp_Policy = 69,
        /// <summary>
        /// Cached options for the [interp queue] command.
        /// </summary>
        Interp_Queue = 70,
        /// <summary>
        /// Cached options for the [interp read or get script file] command.
        /// </summary>
        Interp_ReadOrGetScriptFile = 71,
        /// <summary>
        /// Cached options for the [interp rename] command.
        /// </summary>
        Interp_Rename = 72,
        /// <summary>
        /// Cached options for the [interp reset cancel] command.
        /// </summary>
        Interp_ResetCancel = 73,
        /// <summary>
        /// Cached options for the [interp service] command.
        /// </summary>
        Interp_Service = 74,
        /// <summary>
        /// Cached options for the [interp source] command.
        /// </summary>
        Interp_Source = 75,
        /// <summary>
        /// Cached options for the [interp stub] command.
        /// </summary>
        Interp_Stub = 76,
        /// <summary>
        /// Cached options for the [interp sub command] command.
        /// </summary>
        Interp_SubCommand = 77,
        /// <summary>
        /// Cached options for the [interp subst] command.
        /// </summary>
        Interp_Subst = 78,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [kill] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [kill] command.
        /// </summary>
        Kill = 79,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                            [library] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [library call] command.
        /// </summary>
        Library_Call = 80,

#if EMIT && NATIVE && LIBRARY
        /// <summary>
        /// Cached options for the [library declare] command.
        /// </summary>
        Library_Declare = 81,
        /// <summary>
        /// Cached options for the [library load] command.
        /// </summary>
        Library_Load = 82,
        /// <summary>
        /// Cached options for the [library resolve] command.
        /// </summary>
        Library_Resolve = 83,
        /// <summary>
        /// Cached options for the [library unresolve] command.
        /// </summary>
        Library_Unresolve = 84,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [load] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [load] command.
        /// </summary>
        Load = 85,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                            [lsearch] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [lsearch] command.
        /// </summary>
        Lsearch = 86,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [lsort] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [lsort] command.
        /// </summary>
        Lsort = 87,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                           [namespace] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [namespace1 export] command.
        /// </summary>
        Namespace1_Export = 88,
        /// <summary>
        /// Cached options for the [namespace1 import] command.
        /// </summary>
        Namespace1_Import = 89,
        /// <summary>
        /// Cached options for the [namespace1 which] command.
        /// </summary>
        Namespace1_Which = 90,
        /// <summary>
        /// Cached options for the [namespace2 export] command.
        /// </summary>
        Namespace2_Export = 91,
        /// <summary>
        /// Cached options for the [namespace2 import] command.
        /// </summary>
        Namespace2_Import = 92,
        /// <summary>
        /// Cached options for the [namespace2 which] command.
        /// </summary>
        Namespace2_Which = 93,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [object] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [object alias] command.
        /// </summary>
        Object_Alias = 94,
        /// <summary>
        /// Cached options for the [object callback] command.
        /// </summary>
        Object_Callback = 95,
        /// <summary>
        /// Cached options for the [object certificate] command.
        /// </summary>
        Object_Certificate = 96,
        /// <summary>
        /// Cached options for the [object cleanup] command.
        /// </summary>
        Object_Cleanup = 97,
        /// <summary>
        /// Cached options for the [object create] command.
        /// </summary>
        Object_Create = 98,
        /// <summary>
        /// Cached options for the [object declare] command.
        /// </summary>
        Object_Declare = 99,
        /// <summary>
        /// Cached options for the [object dispose] command.
        /// </summary>
        Object_Dispose = 100,
        /// <summary>
        /// Cached options for the [object fixup return value] command.
        /// </summary>
        Object_FixupReturnValue = 101,
        /// <summary>
        /// Cached options for the [object for each] command.
        /// </summary>
        Object_ForEach = 102,
        /// <summary>
        /// Cached options for the [object get] command.
        /// </summary>
        Object_Get = 103,
        /// <summary>
        /// Cached options for the [object import] command.
        /// </summary>
        Object_Import = 104,
        /// <summary>
        /// Cached options for the [object invoke] command.
        /// </summary>
        Object_Invoke = 105,
        /// <summary>
        /// Cached options for the [object invoke all] command.
        /// </summary>
        Object_InvokeAll = 106,
        /// <summary>
        /// Cached options for the [object invoke only] command.
        /// </summary>
        Object_InvokeOnly = 107,
        /// <summary>
        /// Cached options for the [object invoke raw] command.
        /// </summary>
        Object_InvokeRaw = 108,
        /// <summary>
        /// Cached options for the [object invoke raw only] command.
        /// </summary>
        Object_InvokeRawOnly = 109,
        /// <summary>
        /// Cached options for the [object invoke shared] command.
        /// </summary>
        Object_InvokeShared = 110,
        /// <summary>
        /// Cached options for the [object invoke shared only] command.
        /// </summary>
        Object_InvokeSharedOnly = 111,
        /// <summary>
        /// Cached options for the [object is disposed] command.
        /// </summary>
        Object_IsDisposed = 112,
        /// <summary>
        /// Cached options for the [object is null] command.
        /// </summary>
        Object_IsNull = 113,
        /// <summary>
        /// Cached options for the [object is of type] command.
        /// </summary>
        Object_IsOfType = 114,
        /// <summary>
        /// Cached options for the [object load] command.
        /// </summary>
        Object_Load = 115,
        /// <summary>
        /// Cached options for the [object members] command.
        /// </summary>
        Object_Members = 116,
        /// <summary>
        /// Cached options for the [object search] command.
        /// </summary>
        Object_Search = 117,
        /// <summary>
        /// Cached options for the [object simple callback] command.
        /// </summary>
        Object_SimpleCallback = 118,
        /// <summary>
        /// Cached options for the [object type] command.
        /// </summary>
        Object_Type = 119,
        /// <summary>
        /// Cached options for the [object unalias namespace] command.
        /// </summary>
        Object_UnaliasNamespace = 120,
        /// <summary>
        /// Cached options for the [object undeclare] command.
        /// </summary>
        Object_Undeclare = 121,
        /// <summary>
        /// Cached options for the [object unimport] command.
        /// </summary>
        Object_Unimport = 122,
        /// <summary>
        /// Cached options for the [object verify all] command.
        /// </summary>
        Object_VerifyAll = 123,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [open] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [open] command.
        /// </summary>
        Open = 124,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                            [package] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [package absent] command.
        /// </summary>
        Package_Absent = 125,
        /// <summary>
        /// Cached options for the [package alias] command.
        /// </summary>
        Package_Alias = 126,
        /// <summary>
        /// Cached options for the [package present] command.
        /// </summary>
        Package_Present = 127,
        /// <summary>
        /// Cached options for the [package require] command.
        /// </summary>
        Package_Require = 128,
        /// <summary>
        /// Cached options for the [package scan pre options] command.
        /// </summary>
        Package_ScanPreOptions = 129,
        /// <summary>
        /// Cached options for the [package scan] command.
        /// </summary>
        Package_Scan = 130,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [parse] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [parse command] command.
        /// </summary>
        Parse_Command = 131,
        /// <summary>
        /// Cached options for the [parse expression] command.
        /// </summary>
        Parse_Expression = 132,
        /// <summary>
        /// Cached options for the [parse options] command.
        /// </summary>
        Parse_Options = 133,
        /// <summary>
        /// Cached options for the [parse script] command.
        /// </summary>
        Parse_Script = 134,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [puts] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [puts] command.
        /// </summary>
        Puts = 135,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [read] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [read] command.
        /// </summary>
        Read = 136,
        /// <summary>
        /// Cached options for the [read only] command.
        /// </summary>
        Read_Only = 137,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [regexp] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [regexp] command.
        /// </summary>
        Regexp = 138,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [regsub] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [regsub] command.
        /// </summary>
        Regsub = 139,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [rename] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [rename] command.
        /// </summary>
        Rename = 140,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [return] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [return] command.
        /// </summary>
        Return = 141,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [scope] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [scope close] command.
        /// </summary>
        Scope_Close = 142,
        /// <summary>
        /// Cached options for the [scope create] command.
        /// </summary>
        Scope_Create = 143,
        /// <summary>
        /// Cached options for the [scope eval] command.
        /// </summary>
        Scope_Eval = 144,
        /// <summary>
        /// Cached options for the [scope global] command.
        /// </summary>
        Scope_Global = 145,
        /// <summary>
        /// Cached options for the [scope lock] command.
        /// </summary>
        Scope_Lock = 146,
        /// <summary>
        /// Cached options for the [scope open] command.
        /// </summary>
        Scope_Open = 147,
        /// <summary>
        /// Cached options for the [scope unlock] command.
        /// </summary>
        Scope_Unlock = 148,
        /// <summary>
        /// Cached options for the [scope update] command.
        /// </summary>
        Scope_Update = 149,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [socket] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [socket] command.
        /// </summary>
        Socket = 150,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [source] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [source] command.
        /// </summary>
        Source = 151,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [split] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [split] command.
        /// </summary>
        Split = 152,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [sql] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Cached options for the [sql execute] command.
        /// </summary>
        Sql_Execute = 153,
        /// <summary>
        /// Cached options for the [sql execute only] command.
        /// </summary>
        Sql_ExecuteOnly = 154,
        /// <summary>
        /// Cached options for the [sql open pre options] command.
        /// </summary>
        Sql_OpenPreOptions = 155,
        /// <summary>
        /// Cached options for the [sql open] command.
        /// </summary>
        Sql_Open = 156,
        /// <summary>
        /// Cached options for the [sql transaction] command.
        /// </summary>
        Sql_Transaction = 157,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [string] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [string equal] command.
        /// </summary>
        String_Equal = 158,
        /// <summary>
        /// Cached options for the [string ends] command.
        /// </summary>
        String_Ends = 159,
        /// <summary>
        /// Cached options for the [string first] command.
        /// </summary>
        String_First = 160,
        /// <summary>
        /// Cached options for the [string format] command.
        /// </summary>
        String_Format = 161,
        /// <summary>
        /// Cached options for the [string is] command.
        /// </summary>
        String_Is = 162,
        /// <summary>
        /// Cached options for the [string last] command.
        /// </summary>
        String_Last = 163,
        /// <summary>
        /// Cached options for the [string map] command.
        /// </summary>
        String_Map = 164,
        /// <summary>
        /// Cached options for the [string match] command.
        /// </summary>
        String_Match = 165,
        /// <summary>
        /// Cached options for the [string starts] command.
        /// </summary>
        String_Starts = 166,
        /// <summary>
        /// Cached options for the [string to lower] command.
        /// </summary>
        String_ToLower = 167,
        /// <summary>
        /// Cached options for the [string to title] command.
        /// </summary>
        String_ToTitle = 168,
        /// <summary>
        /// Cached options for the [string to upper] command.
        /// </summary>
        String_ToUpper = 169,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [subst] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [subst] command.
        /// </summary>
        Subst = 170,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [switch] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [switch] command.
        /// </summary>
        Switch = 171,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [tcl] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// Cached options for the [tcl cancel] command.
        /// </summary>
        Tcl_Cancel = 172,
        /// <summary>
        /// Cached options for the [tcl create] command.
        /// </summary>
        Tcl_Create = 173,
        /// <summary>
        /// Cached options for the [tcl evaluate] command.
        /// </summary>
        Tcl_Evaluate = 174,
        /// <summary>
        /// Cached options for the [tcl expr] command.
        /// </summary>
        Tcl_Expr = 175,
        /// <summary>
        /// Cached options for the [tcl find] command.
        /// </summary>
        Tcl_Find = 176,
        /// <summary>
        /// Cached options for the [tcl interp create] command.
        /// </summary>
        Tcl_InterpCreate = 177,
        /// <summary>
        /// Cached options for the [tcl load] command.
        /// </summary>
        Tcl_Load = 178,
        /// <summary>
        /// Cached options for the [tcl queue] command.
        /// </summary>
        Tcl_Queue = 179,
        /// <summary>
        /// Cached options for the [tcl record and eval] command.
        /// </summary>
        Tcl_RecordAndEval = 180,
        /// <summary>
        /// Cached options for the [tcl reset cancel] command.
        /// </summary>
        Tcl_ResetCancel = 181,
        /// <summary>
        /// Cached options for the [tcl select] command.
        /// </summary>
        Tcl_Select = 182,
        /// <summary>
        /// Cached options for the [tcl source] command.
        /// </summary>
        Tcl_Source = 183,
        /// <summary>
        /// Cached options for the [tcl subst] command.
        /// </summary>
        Tcl_Subst = 184,
        /// <summary>
        /// Cached options for the [tcl update] command.
        /// </summary>
        Tcl_Update = 185,
        /// <summary>
        /// Cached options for the [tcl version range] command.
        /// </summary>
        Tcl_VersionRange = 186,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [test2] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [test2] command.
        /// </summary>
        Test2 = 187,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                        [test] command options (Default.cs)
        ///////////////////////////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// Cached options for the [test create with rules] command.
        /// </summary>
        Test_CreateWithRules = 188,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [time] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [time] command.
        /// </summary>
        Time = 189,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [unload] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [unload] command.
        /// </summary>
        Unload = 190,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [unset] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [unset] command.
        /// </summary>
        Unset = 191,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [uri] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [uri compare] command.
        /// </summary>
        Uri_Compare = 192,
        /// <summary>
        /// Cached options for the [uri create] command.
        /// </summary>
        Uri_Create = 193,

#if NETWORK
        /// <summary>
        /// Cached options for the [uri get] command.
        /// </summary>
        Uri_Get = 194,
        /// <summary>
        /// Cached options for the [uri post] command.
        /// </summary>
        Uri_Post = 195,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                             [vwait] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cached options for the [vwait] command.
        /// </summary>
        Vwait = 196,

        ///////////////////////////////////////////////////////////////////////////////////////////
        //                              [xml] command options
        ///////////////////////////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        /// <summary>
        /// Cached options for the [xml deserialize] command.
        /// </summary>
        Xml_Deserialize = 197,
        /// <summary>
        /// Cached options for the [xml serialize] command.
        /// </summary>
        Xml_Serialize = 198,
#endif

        /// <summary>
        /// Cached options for the [xml for each] command.
        /// </summary>
        Xml_ForEach = 199
    }
}
