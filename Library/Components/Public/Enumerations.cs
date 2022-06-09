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
using System.IO;
using Eagle._Attributes;

namespace Eagle._Components.Public
{
    [ObjectId("97b63d06-7e69-428d-ab85-4dac39c609c3")]
    public enum RuleType
    {
        None = 0x0,
        Invalid = 0x1,
        Include = 0x2,
        Exclude = 0x4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("438d2aba-7779-49ee-8924-e300d24aeb84")]
    public enum TraceListenerType
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = 0x1000,
        Console = 0x2000,
        Native = 0x4000,
        LogFile = 0x8000,
        Buffered = 0x10000,
        Automatic = 0x20000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CoreMask = Default | Console,
        TestMask = Native | LogFile | Buffered
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
    [Flags()]
    [ObjectId("3b746783-4aa8-4d04-a41c-1b51c7c7164e")]
    public enum SecurityProtocolType
    {
        None = 0x0,
        SystemDefault = 0x0000,
        Ssl2 = 0x000C,
        Ssl3 = 0x0030,
        Tls = Tls10,
        Tls10 = 0x00C0,
        Tls11 = 0x0300,
        Tls12 = 0x0C00,
        Tls13 = 0x3000
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1d87ea2d-45e0-40e6-a156-deb687245be1")]
    public enum DelegateFlags
    {
        None = 0x0,                 /* Do nothing. */
        Invalid = 0x1,              /* Do not use. */
        UseEngine = 0x2,            /* LEGACY: Directly execute the delegate with
                                     * its parameters, while completely avoiding
                                     * core marshaller handling. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Public = 0x100,             /* Include public members when creating any
                                     * sub-commands. */
        NonPublic = 0x200,          /* Include non-public members when creating
                                     * any sub-commands. */
        Instance = 0x400,           /* Include instance members when creating
                                     * any sub-commands. */
        Static = 0x800,             /* Include static members when creating any
                                     * sub-commands. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowDuplicate = 0x1000,    /* Do not raise an error if any duplicate
                                     * delegate name is seen. */
        OverwriteExisting = 0x2000, /* When a duplicate delegate name is seen,
                                     * replace it. */
        FailOnNone = 0x4000,        /* Fail if no methods are available. */
        NoComplain = 0x8000,        /* Do not stop when an error is hit. */
        Verbose = 0x10000,          /* Enable verbose error reporting when
                                     * attempting to find methods. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseCallOptions = 0x20000,   /* Allow the same options permitted by the
                                     * [library call] sub-command to be used. */
        UseReturnOptions = 0x40000, /* If the returned value ends up being an
                                     * opaque object handle, permit it to use
                                     * the same options as the [object invoke]
                                     * sub-command. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        LookupObjects = 0x100000,   /* Pre-process the sub-command arguments
                                     * looking for (and translating) opaque
                                     * object handles to use as the delegate
                                     * parameters. */
        MakeIntoObject = 0x200000,  /* If the type of the delegate result is
                                     * not directly supported by the Result
                                     * class, post-process it to an opaque
                                     * object handle. */
        WrapReturnType = 0x400000,  /* If the type of the delegate result is
                                     * not directly supported by the Result
                                     * class, forcibly wrap it anyhow. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        LegacyMask = UseCallOptions | UseReturnOptions,
        CommonMask = AllowDuplicate | Verbose,
        PublicInstanceMask = Public | Instance | CommonMask,
        PublicStaticMask = Public | Static | CommonMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
    [Flags()]
    [ObjectId("13c70899-66d8-40f7-994a-fbbfa0a2d12c")]
    public enum XmlErrorTypes
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForInterpreter = 0x2,
        ForUnsafe = 0x4,
        ForSafe = 0x8,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoError = 0x10,
        NoException = 0x20,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Unknown = 0x40,
        Generic = 0x80,

        LoadXml = 0x100,
        LoadFile = 0x200,

        Schema = 0x400,
        Validate = 0x800,

        Encoding = 0x1000,
        StrictEncoding = 0x2000,
        FlattenText = 0x4000,

        Xpath = 0x8000,
        Nodes = 0x10000,
        Empty = 0x20000,

        Xslt = 0x40000,

        Create = 0x80000,
        Policy = 0x100000,
        Read = 0x200000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Unknown | Generic | LoadXml |
              LoadFile | Schema | Validate |
              Encoding | StrictEncoding | FlattenText |
              Xpath | Nodes | Empty | Xslt |
              Create | Policy | Read,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeTypes = LoadXml | Encoding | StrictEncoding | FlattenText | Empty,
        UnsafeTypes = All & ~(Validate | Policy | Read),

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeInterpreter = ForInterpreter | ForSafe | SafeTypes,
        UnsafeInterpreter = ForInterpreter | ForUnsafe | UnsafeTypes,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("58693c8e-fbed-4557-a97d-3f889265a07f")]
    public enum PluginLoaderFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Setup = 0x100,
        Verbose = 0x200,
        Type = 0x400,

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        Resources = 0x800,
#endif

        StopOnError = 0x1000,
        IgnoreEmpty = 0x2000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Load = Setup | Type,
        Preview = Setup | Type,

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ResourcesOnly = Setup | Resources | StopOnError | IgnoreEmpty,
#endif

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("4397666b-2769-4bdb-adef-be837e330fda")]
    public enum SleepType
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Service = 0x2,
        Variable = 0x4,
        Process = 0x8,
        Socket = 0x10,
        Script = 0x20,
        Heartbeat = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclWrapper = 0x100,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        User0 = 0x1000,
        User1 = 0x2000,
        User2 = 0x4000,
        User3 = 0x8000,
        User4 = 0x10000,
        User5 = 0x20000,
        User6 = 0x40000,
        User7 = 0x80000,
        User8 = 0x100000,
        User9 = 0x200000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("bdeab90b-2ce1-4af2-8647-b16006c81d43")]
    public enum ArgumentPhase
    {
        None = 0x0,      /* No special handling. */
        Invalid = 0x1,   /* Invalid, do not use. */
        Reserved = 0x2,  /* Reserved, do not use. */

        Phase0 = 0x100,  /* Argument handling to determine overall command
                          * line option. */
        Phase1 = 0x200,  /* Gathering of arguments to pass to the code that
                          * is handling the current command line option. */
        Phase2 = 0x400,  /* Not yet used.*/
        Phase3 = 0x800,  /* Not yet used.*/
        Phase4 = 0x1000, /* Not yet used.*/
        Phase5 = 0x2000  /* Not yet used.*/
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6551aa70-516a-4e7c-8cee-cdffcb1c0e10")]
    public enum PathCallbackType
    {
        None = 0x0,
        Invalid = 0x1,
        GetTempFileName = 0x2,
        GetTempPath = 0x4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX || UNSAFE)
    [Flags()]
    [ObjectId("3668a1e8-bbda-4e86-b648-484fcc8ff474")]
    public enum NativeCallbackType
    {
        None = 0x0,
        Invalid = 0x1,
        IsMainThread = 0x2,
        GetStackPointer = 0x4,
        GetStackAllocated = 0x8,
        GetStackMaximum = 0x10,
        UnixGetStackMaximum = 0x20
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1799ca3f-bf68-4b2f-b22a-5456a3e393db")]
    public enum FrameworkFlags
    {
        None = 0x0,
        Invalid = 0x1,

        BuiltIn = 0x2,
        External = 0x4,
        Test = 0x8,

        NonPublic = 0x10,
        Verbose = 0x20,

        Instance = 0x40,
        Static = 0x80,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = NonPublic | Verbose | Static
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
    [Flags()]
    [ObjectId("19b08291-256f-4ca0-beff-4bcaf74f7e23")]
    public enum BufferedTraceFlags
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TakeOwnership = 0x2,
        EmptyOnClose = 0x4,
        IgnoreEmptyOnClose = 0x8,
        NeverFlush = 0x10,
        FlushOnClose = 0x20,
        EmptyOnFlush = 0x40,
        FlushOnEmpty = 0x80,
        BufferingDisabled = 0x100,
        CoalesceDuplicates = 0x200,
        ConsecutiveDuplicates = 0x400,
        NoRepeatedCount = 0x800,
        FormatWithId = 0x1000,
        Reserved1 = 0x2000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = EmptyOnClose | FlushOnClose | EmptyOnFlush |
                  Reserved1
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("17219a26-f14b-4643-82a0-b915c73dcd37")]
    public enum TracePriority : ulong
    {
        None = 0x0,                               // do not use.
        Invalid = 0x1,                            // reserved, do not use.

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: It should be noted here that these are always
        //       (currently) treated as flags by this library;
        //       hence, their relative values have no meaning.
        //       Still, keeping them in order is makes it easy
        //       to change this later.
        //
        #region Core Priority Values
        Lowest = 0x2,
        Lower = 0x4,
        Low = 0x8,
        MediumLow = 0x10,
        Medium = 0x20,
        MediumHigh = 0x40,
        High = 0x80,
        Higher = 0x100,
        Highest = 0x200,
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Core Type Values
        Emergency = 0x400,                        // system error / immediate attention.
        Fatal = 0x800,                            // unrecoverable error / exception.
        Error = 0x1000,                           // error / exception message.
        Warning = 0x2000,                         // warning / unusual condition message.
        Inform = 0x4000,                          // informational message.
        Debug = 0x8000,                           // debug / diagnostic  message.
        Verbose = 0x10000,                        // debug / extra information, noisy.
        Demand = 0x20000,                         // on-demand via script command, etc.
        External = 0x40000,                       // message external to library.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Core Flag Values
        EnableDateTimeFlag = 0x80000,             // add DateTime to trace messages.
        EnablePriorityFlag = 0x100000,            // add Priority to trace messages.
        EnableServerNameFlag = 0x200000,          // add AppDomain to trace messages.
        EnableTestNameFlag = 0x400000,            // add test name to trace messages.
        EnableAppDomainFlag = 0x800000,           // add AppDomain to trace messages.
        EnableInterpreterFlag = 0x1000000,        // add Interpreter to trace messages.
        DisableInterpreterFlag = 0x2000000,       // remove Interpreter from trace messages.
        EnableThreadIdFlag = 0x4000000,           // add ThreadId to trace messages.
        EnableMethodFlag = 0x8000000,             // add Method to trace messages.
        EnableStackFlag = 0x10000000,             // add StackTrace to trace messages.
        EnableExtraNewLinesFlag = 0x20000000,     // surround trace messages with new
                                                  // lines.

        ///////////////////////////////////////////////////////////////////////////////////////////

        EnableMinimumFormatFlag = 0x40000000,     // use the minimal trace format string.
        EnableMediumLowFormatFlag = 0x80000000,   // use the medium low trace format string.
        EnableMediumFormatFlag = 0x100000000,     // use the medium trace format string.
        EnableMediumHighFormatFlag = 0x200000000, // use the medium high trace format string.
        EnableMaximumFormatFlag = 0x400000000,    // use the maximal trace format string.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Category Flag Values
        CategoryPenalty = 0x800000000,            // decrease priority for any listed category.
        CategoryBonus = 0x1000000000,             // increase priority for any listed category.

        ///////////////////////////////////////////////////////////////////////////////////////////

        DenyNullCategory = 0x2000000000,          // deny trace messages with a null category.
        AllowNullCategory = 0x4000000000,         // allow trace messages with a null category.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Application / User Values (External Use Only)
        User0 = 0x8000000000,                     // reserved for third-party use.
        User1 = 0x10000000000,                    // reserved for third-party use.
        User2 = 0x20000000000,                    // reserved for third-party use.
        User3 = 0x40000000000,                    // reserved for third-party use.
        User4 = 0x80000000000,                    // reserved for third-party use.
        User5 = 0x100000000000,                   // reserved for third-party use.
        User6 = 0x200000000000,                   // reserved for third-party use.
        User7 = 0x400000000000,                   // reserved for third-party use.
        User8 = 0x800000000000,                   // reserved for third-party use.
        User9 = 0x1000000000000,                  // reserved for third-party use.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Error Values
        LowError = Low | Error,                   // for external use only.
        MediumError = Medium | Error,             // for external use only.
        HighError = High | Error,                 // for external use only.

        ///////////////////////////////////////////////////////////////////////////////////////////

        TestError2 = Lower | Error,               // test suite infrastructure, etc.

        UserInterfaceError = Low | Error,         // WinForms exception, etc.
        LockError2 = Low | Error,                 // unable to acquire required lock
        InternalError = Low | Error,              // miscellaneous unclassified
        StringError = Low | Error,                // string manipulation, etc.
        SecurityError2 = Low | Error,             // signatures, certificates, etc.
        CacheError = Low | Error,                 // cache configuration, etc.
        NativeError3 = Low | Error,               // native code and interop

        HostError = MediumLow | Error,            // interpreter hosts, etc.
        ConsoleError = MediumLow | Error,         // built-in console host, etc.
        ScriptError = MediumLow | Error,          // high-level script evaluation
        StartupError2 = MediumLow | Error,        // library / interpreter startup.
        NativeError2 = MediumLow | Error,         // native code and interop
        PathError2 = MediumLow | Error,           // path discovery and building
        NetworkError2 = MediumLow | Error,        // data transfer over network, etc.
        PluginError2 = MediumLow | Error,         // from IPlugin members, etc.
        InterpreterError = MediumLow | Error,     // interpreter handling, etc.
        GetDataError2 = MediumLow | Error,        // _Hosts.Core.GetData(), et al.

        PlatformError = Medium | Error,           // operating system call, etc.
        PathError = Medium | Error,               // path discovery and building
        FileSystemError = Medium | Error,         // file system error, etc.
        CallbackError = Medium | Error,           // user-defined callback issue
        EngineError = Medium | Error,             // low-level script evaluation
        RemotingError = Medium | Error,           // remoting and serialization
        ConsoleError2 = Medium | Error,           // interpreter hosts, etc.
        VariableError = Medium | Error,           // low-level variable handling
        SyntaxError = Medium | Error,             // built-in command syntax help.
        IoError = Medium | Error,                 // stream interaction, file I/O, etc.
        DataError = Medium | Error,               // database interaction, etc.
        ResourceError = Medium | Error,           // managed assembly resources, etc.
        InternalError2 = Medium | Error,          // miscellaneous unclassified
        GetDataError = Medium | Error,            // _Hosts.Core.GetData(), et al.

        MarshalError = MediumHigh | Error,        // core marshaller, binder, etc.
        NativeError = MediumHigh | Error,         // native code and interop
        ScriptError2 = MediumHigh | Error,        // high-level script evaluation
        PerformanceError = MediumHigh | Error,    // for [time], etc.
        PackageError = MediumHigh | Error,        // high-level package handling, etc.

        EngineError2 = High | Error,              // low-level script evaluation
        EventError = High | Error,                // event manager and processing
        HandleError = High | Error,               // handles, native and managed
        ConversionError = High | Error,           // value conversion, etc.
        PackageError2 = High | Error,             // high-level package handling, etc.
        RuleError = High | Error,                 // rule set, etc.

        LockError = Higher | Error,               // unable to acquire required lock
        ThreadError = Higher | Error,             // thread exceptions, timeout, etc.
        ScriptThreadError = Higher | Error,       // ScriptThread exceptions, etc.
        NetworkError = Higher | Error,            // data transfer over network, etc.
        ProcessError = Higher | Error,            // process handling, [exec], etc.

        NamespaceError = Highest | Error,         // internal namespace processing, etc.
        TestError = Highest | Error,              // test suite infrastructure, etc.
        CleanupError = Highest | Error,           // object disposal and cleanup.
        StartupError = Highest | Error,           // library / interpreter startup.
        ShellError = Highest | Error,             // interactive shell and loop.
        SecurityError = Highest | Error,          // signatures, certificates, etc.
        ComplainError = Highest | Error,          // from DebugOps.Complain.
        StatusError = Highest | Error,            // from StatusFormOps, etc.
        PluginError = Highest | Error,            // from IPlugin members, etc.
        HealthError = Highest | Error,            // from health checks.
        InternalError3 = Highest | Error,         // miscellaneous unclassified
        PackageError3 = Highest | Error,          // high-level package handling, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Warning Values
        CleanupWarning2 = Lower | Warning,        // object disposal and cleanup.

        FileSystemWarning = Medium | Warning,     // file system warning, etc.
        MarshalWarning = Medium | Warning,        // core marshaller, binder, etc.

        LockWarning = High | Warning,             // unable to acquire optional lock

        CleanupWarning = Highest | Warning,       // object disposal and cleanup.
        NetworkWarning = Highest | Warning,       // data transfer over network, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Debugging Values
        LowDebug = Low | Debug,                   // for external use only.
        MediumDebug = Medium | Debug,             // for external use only.
        HighDebug = High | Debug,                 // for external use only.

        ///////////////////////////////////////////////////////////////////////////////////////////

        ConsoleDebug = Lowest | Debug,            // built-in console host, etc.
        PathDebug = Lowest | Debug,               // path discovery and building
        LoopDebug = Lowest | Debug,               // interactive loop, etc.
        EngineDebug = Lowest | Debug,             // low-level script evaluation
        StartupDebug2 = Lowest | Debug,           // library / interpreter startup.
        NetworkDebug4 = Lowest | Debug,           // data transfer over network, etc.
        SecurityDebug3 = Lowest | Debug,          // signatures, certificates, etc.
        GetDataDebug2 = Lowest | Debug,           // _Hosts.Core.GetData(), et al.

        ShellDebug = Lower | Debug,               // interactive shell and loop.
        HostDebug = Lower | Debug,                // interpreter host, console, etc.
        GetDataDebug = Lower | Debug,             // _Hosts.Core.GetData(), et al.
        CacheDebug = Lower | Debug,               // cache configuration, etc.

        AddDebug = Low | Debug,                   // opaque object handle addition.
        RemoveDebug = Low | Debug,                // opaque object handle removal.
        EventDebug = Low | Debug,                 // event manager and processing
        ScriptDebug = Low | Debug,                // high-level script evaluation
        LowThreadDebug = Low | Debug,             // thread exceptions, timeout, etc.
        ValueDebug = Low | Debug,                 // expression values, etc.
        PackageDebug3 = Low | Debug,              // high-level package handling, etc.
        NetworkDebug3 = Low | Debug,              // data transfer over network, etc.

        CleanupDebug = MediumLow | Debug,         // object disposal and cleanup.
        StartupDebug = MediumLow | Debug,         // library / interpreter startup.
        NativeDebug2 = MediumLow | Debug,         // native code and interop (details)
        MarshalDebug2 = MediumLow | Debug,        // core marshaller, binder, etc (details).
        ThreadDebug2 = MediumLow | Debug,         // thread exceptions, timeout, etc.
        PlatformDebug2 = MediumLow | Debug,       // operating system call, etc.
        PackageDebug2 = MediumLow | Debug,        // high-level package handling, etc.
        PolicyDebug2 = MediumLow | Debug,         // policy context approvals, etc.
        FileSystemDebug = MediumLow | Debug,      // file system related, etc.

        DisposalDebug = Medium | Debug,           // interpreter, etc disposal.
        PlatformDebug = Medium | Debug,           // operating system call, etc.
        NativeDebug = Medium | Debug,             // native code and interop (summary)
        MarshalDebug = Medium | Debug,            // core marshaller, binder, etc (summary).
        EnumDebug = Medium | Debug,               // enum / flag handling, etc.
        DataDebug = Medium | Debug,               // database interaction, etc.
        HostDebug2 = Medium | Debug,              // interpreter host, console, etc.
        NetworkDebug2 = Medium | Debug,           // data transfer over network, etc.
        RemotingDebug2 = Medium | Debug,          // remoting and serialization
        ChannelDebug = Medium | Debug,            // input / output channels, etc.
        PolicyDebug = Medium | Debug,             // policy context approvals, etc.
        StatusDebug = Medium | Debug,             // from StatusFormOps, etc.
        PerformanceDebug = Medium | Debug,        // for [time], etc.
        ShellDebug3 = Medium | Debug,             // high-level interactive shell and loop.
        ScriptDebug3 = Medium | Debug,            // high-level script evaluation
        PluginDebug = Medium | Debug,             // plugin loader (high-level), etc.
        SecurityDebug2 = Medium | Debug,          // signatures, certificates, etc.
        ThreadDebug4 = Medium | Debug,            // thread exceptions, timeout, etc.
        RemotingDebug = Medium | Debug,           // remoting and serialization
        CleanupDebug3 = Medium | Debug,           // object disposal and cleanup.

        StartupDebug3 = MediumHigh | Debug,       // library / interpreter startup.
        ThreadDebug = MediumHigh | Debug,         // thread exceptions, timeout, etc.
        TestDebug = MediumHigh | Debug,           // test suite infrastructure, etc.
        ScriptDebug2 = MediumHigh | Debug,        // high-level script evaluation
        ScriptThreadDebug = MediumHigh | Debug,   // ScriptThread debugging, etc.
        ShellDebug2 = MediumHigh | Debug,         // high-level interactive shell and loop.
        MarshalDebug3 = MediumHigh | Debug,       // core marshaller, binder, etc (details).
        SetupDebug = MediumHigh | Debug,          // setup instances, auto-update, etc.
        HostDebug3 = MediumHigh | Debug,          // interpreter host, console, etc.
        ProcessDebug = MediumHigh | Debug,        // process handling, [exec], etc.
        RemotingDebug3 = MediumHigh | Debug,      // remoting and serialization
        PackageDebug4 = MediumHigh | Debug,       // high-level package handling, etc.

        PackageDebug = High | Debug,              // high-level package handling, etc.
        RuleDebug = High | Debug,                 // rule set, etc.

        CreateDebug = Highest | Debug,            // object creation, etc.
        EnvironmentDebug = Highest | Debug,       // environment variables, etc.
        CleanupDebug2 = Highest | Debug,          // object disposal and cleanup.
        NetworkDebug = Highest | Debug,           // data transfer over network, etc.
        SecurityDebug = Highest | Debug,          // signatures, certificates, etc.
        PackageDebug5 = Highest | Debug,          // high-level package handling, etc.
        ThreadDebug3 = Highest | Debug,           // thread exceptions, timeout, etc.
        TestDebug2 = Highest | Debug,             // test suite infrastructure, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Other Values
        CommandDebug = MediumLow | Debug | Demand,  // related to script command, etc.
        Command = Medium | Demand,                  // related to script command, etc.
        CommandError = MediumHigh | Error | Demand, // related to script command, etc.
        CommandError2 = MediumHigh | Error,         // related to script command, etc.
        CommandDebug2 = MediumHigh | Debug,         // related to script command, etc.

        ///////////////////////////////////////////////////////////////////////////////////////////

        PolicyTrace = High | Inform | Demand,     // related to policy tracing, etc.
        PolicyError = Highest | Error | Demand,   // related to policy tracing, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Conditional Values
        //
        // HACK: For the "Debug" build, enable those messages by default;
        //       otherwise, disable them by default.
        //
#if DEBUG || FORCE_TRACE
        MaybeDebug = Debug,
#else
        MaybeDebug = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: For the "Debug" build with the VERBOSE option, enable
        //       those messages by default; otherwise, disable them by
        //       default.
        //
#if (DEBUG || FORCE_TRACE) && VERBOSE
        MaybeVerbose = Verbose,
#else
        MaybeVerbose = None,
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Useful Mask Values
        HigherAndUpMask = Higher | Highest,
        HighAndUpMask = High | HigherAndUpMask,

        MediumHighAndUpMask = MediumHigh | HighAndUpMask,
        MediumAndUpMask = Medium | MediumHighAndUpMask,
        MediumLowAndUpMask = MediumLow | MediumAndUpMask,

        LowAndUpMask = Low | MediumLowAndUpMask,
        LowerAndUpMask = Lower | LowAndUpMask,
        LowestAndUpMask = Lowest | LowerAndUpMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        HighestAndDownMask = Highest | HigherAndDownMask,
        HigherAndDownMask = Higher | HighAndDownMask,
        HighAndDownMask = High | MediumHighAndDownMask,

        MediumHighAndDownMask = MediumHigh | MediumAndDownMask,
        MediumAndDownMask = Medium | MediumLowAndDownMask,
        MediumLowAndDownMask = MediumLow | LowAndDownMask,

        LowAndDownMask = Low | LowerAndDownMask,
        LowerAndDownMask = Lower | Lowest,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TroubleshootingMask = EnableStackFlag | EnableMaximumFormatFlag,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyFormatMask = EnableMinimumFormatFlag | EnableMediumLowFormatFlag |
                        EnableMediumFormatFlag | EnableMediumHighFormatFlag |
                        EnableMaximumFormatFlag,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyPriorityMask = LowestAndUpMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        MaybeAnyCoreTypeMask = Emergency | Fatal | Error |
                               Warning | Inform | MaybeDebug |
                               MaybeVerbose | Demand | External,

        AnyCoreTypeMask = Emergency | Fatal | Error |
                          Warning | Inform | Debug |
                          Verbose | Demand | External,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the TraceOps.HasPriorities method only.
        //
        HasPrioritiesMask = AnyPriorityMask | AnyCoreTypeMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NullCategoryMask = DenyNullCategory | AllowNullCategory,

        ///////////////////////////////////////////////////////////////////////////////////////////

        LowPrioritiesMask = LowAndUpMask | AnyCoreTypeMask,
        MediumPrioritiesMask = MediumAndUpMask | AnyCoreTypeMask,
        HighPrioritiesMask = HighAndUpMask | AnyCoreTypeMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyCoreFlagMask = EnableDateTimeFlag | EnablePriorityFlag |
                          EnableServerNameFlag | EnableTestNameFlag |
                          EnableAppDomainFlag | EnableInterpreterFlag |
                          DisableInterpreterFlag | EnableThreadIdFlag |
                          EnableMethodFlag | EnableStackFlag |
                          EnableExtraNewLinesFlag | EnableMinimumFormatFlag |
                          EnableMediumLowFormatFlag | EnableMediumFormatFlag |
                          EnableMediumHighFormatFlag | EnableMaximumFormatFlag,

        AnyCoreCategoryMask = CategoryPenalty | CategoryBonus |
                              DenyNullCategory | AllowNullCategory,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyUserTypeMask = User0 | User1 | User2 |
                          User3 | User4 | User5 |
                          User6 | User7 | User8 |
                          User9,

        ///////////////////////////////////////////////////////////////////////////////////////////

        MaybeAnyTypeMask = MaybeAnyCoreTypeMask | AnyUserTypeMask,

        AnyTypeMask = AnyCoreTypeMask | AnyUserTypeMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyPriorityOrTypeMask = AnyPriorityMask | AnyTypeMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyMask = AnyPriorityMask | AnyCoreFlagMask | AnyCoreCategoryMask | AnyTypeMask,
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Suggested Default Priority & Mask Values
        Default = Medium,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultDebug = Default | Debug,
        DefaultWarning = Default | Warning,
        DefaultError = Default | Error | Fatal | Emergency,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultLimitMask = MediumLowAndDownMask, /* For TraceLimits use only. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultMask = MediumAndUpMask | MaybeAnyTypeMask
        #endregion
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d3362813-f130-471a-ba95-4c562b893c4f")]
    public enum TraceStateType : ulong
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Initialized = 0x10,
        ForceListeners = 0x20,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Possible = 0x40,
        ResetPossible = 0x80,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Enabled = 0x100,
        ResetEnabled = 0x200,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FilterCallback = 0x400,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Limits = 0x800,
        ResetLimits = 0x1000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Priorities = 0x2000,
        ResetPriorities = 0x4000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Priority = 0x8000,
        ResetPriority = 0x10000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Categories = 0x20000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EnabledCategories = 0x40000,
        DisabledCategories = 0x80000,
        PenaltyCategories = 0x100000,
        BonusCategories = 0x200000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NullCategories = 0x400000,
        ResetNullCategories = 0x800000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Format = 0x1000000,
        ResetFormat = 0x2000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FormatFlags = 0x4000000,
        ResetFormatFlags = 0x8000000,
        VerboseFlags = 0x10000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FullContext = 0x20000000,
        ResetFullContext = 0x40000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Environment = 0x80000000,
        Force = 0x100000000,
        Reset = 0x200000000,
        OverrideEnvironment = 0x400000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForCommand = 0x800000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        PossibleMask = Possible | ResetPossible,
        EnabledMask = Enabled | ResetEnabled,
        LimitsMask = Limits | ResetLimits,
        PrioritiesMask = Priorities | ResetPriorities,
        PriorityMask = Priority | ResetPriority,
        NullCategoriesMask = NullCategories | ResetNullCategories,
        FormatMask = Format | ResetFormat,
        FormatFlagsMask = FormatFlags | ResetFormatFlags | VerboseFlags,
        FullContextMask = FullContext | ResetFullContext,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CategoryTypeMask = EnabledCategories | DisabledCategories |
                           PenaltyCategories | BonusCategories,

        AllCategoriesMask = Categories | CategoryTypeMask | NullCategoriesMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: Update this value if any other internal state ends up with
        //       an associated environment variable.
        //
        EnvironmentMask = Categories | CategoryTypeMask | Priorities | Priority,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ResetMask = ResetPossible | ResetEnabled | ResetLimits |
                    ResetPriorities | ResetPriority | ResetNullCategories |
                    ResetFormat | ResetFormatFlags | ResetFullContext,

        SpecialMask = Environment | Force | Reset | OverrideEnvironment,

        AllMask = Initialized | ForceListeners | PossibleMask |
                  EnabledMask | FilterCallback | LimitsMask |
                  PrioritiesMask | PriorityMask | AllCategoriesMask |
                  FormatMask | FormatFlagsMask | FullContextMask |
                  Environment | Force,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SdkEnableMask = AllMask & ~(VerboseFlags | ResetMask),
        SdkDisableMask = AllMask & ~(VerboseFlags | SpecialMask),

        ///////////////////////////////////////////////////////////////////////////////////////////

        NonStandardMask = ForceListeners | Priority | FormatMask |
                          FormatFlagsMask | SpecialMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardMask = AllMask & ~NonStandardMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TraceCommand = Default | ForCommand,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = PossibleMask | EnabledMask | LimitsMask | PrioritiesMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ae23c5f4-34e3-4c0a-9298-c305d861cb00")]
    public enum TraceCategoryType
    {
        None = 0x0,        /* There is no trace category type. */
        Invalid = 0x1,     /* Invalid, do not use. */

        Enabled = 0x2,     /* Is the trace category enabled? */

        Disabled = 0x4,    /* Is the trace category disabled? */

        Penalty = 0x8,     /* Does the trace category have a penalty
                            * on its priority? */

        Bonus = 0x10,      /* Does the trace category have a bonus
                            * on its priority? */

        ForDefault = 0x20, /* The default (legacy) trace category is
                            * being used. */

        BaseMask = Enabled | Disabled | Penalty | Bonus,

        Default = Enabled | ForDefault /* LEGACY: The default category type is
                                        * used to determine if the category is
                                        * enabled. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6b85bd1a-f812-476a-bda8-cabbf2c2c7f0")]
    public enum InfoPathType
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,

        NativeProcess = 0x10,
        NativeBinary = 0x20,
        NativeLibrary = 0x40,
        NativeExternals = 0x80,
        ScriptLibraryBase = 0x100,

        Full = 0x200,
        Local = 0x400,
        NoProcessor = 0x800,
        AlternateName = 0x1000,

        TypeMask = NativeProcess | NativeBinary | NativeLibrary |
                   NativeExternals | ScriptLibraryBase,

        FlagsMask = Full | Local | NoProcessor | AlternateName,

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("36aabcbf-061b-4524-bc93-d427b6f62caf")]
    public enum DebugEmergencyLevel
    {
        None = 0x0,                /* No special handling. */
        Invalid = 0x1,             /* Invalid, do not use. */
        Reserved1 = 0x2,           /* Reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Disposed = 0x10,           /* Dispose existing debugger, if any. */
        Disabled = 0x20,           /* Disable debugger, if present. */

        Enabled = 0x40,            /* Enable debugger, if present. */
        Created = 0x80,            /* Create new debugger, if necessary. */

        Break = 0x100,             /* Break into debugger, if present. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Tokens = 0x1000,           /* Enable everything needed to make
                                    * token breakpoints work. */
        ScriptArguments = 0x2000,  /* Enable everything needed to make
                                    * nested script argument tracking
                                    * work. */
        Isolated = 0x4000,         /* Enable isolated interpreter for new
                                    * debugger, if applicable. */
        IgnoreModifiable = 0x8000, /* When creating or disposing of the
                                    * debugger (if necessary), ignore the
                                    * immutable flag for the interpreter. */
        Verbose = 0x10000,         /* Enable more diagnostic output. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForCommonUse = 0x100000,   /* Used to indicate flag values that
                                    * are commonly used with instances
                                    * of this enumeration. */
        ForEnabledUse = 0x200000,  /* Used to indicate flag values that
                                    * are commonly used when enabling
                                    * the debugger. */
        ForDisabledUse = 0x400000, /* Used to indicate flag values that
                                    * are commonly used when disabling
                                    * the debugger. */
        ForOverrideUse = 0x800000, /* Used to indicate flag values that
                                    * are commonly used when debugging
                                    * a script on-demand. */
        ForDefaultUse = 0x1000000, /* Used to indicate flag values that
                                    * are used by default. */
        ForNowUse = 0x2000000,     /* Used to indicate flag values that
                                    * are used with the special "now"
                                    * value. */
        ForLaterUse = 0x4000000,   /* Used to indicate flag values that
                                    * are used with the special "later"
                                    * value. */
        ForFullUse = 0x8000000,    /* Used to indicate flag values that
                                    * are used with the special "full"
                                    * value. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CommonMask = ForCommonUse | IgnoreModifiable,
        EnabledMask = ForEnabledUse | Enabled | Created,
        DisabledMask = ForDisabledUse | Disposed | Disabled,
        OverrideMask = ForOverrideUse | Enabled | Created | Break,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FullMask = Enabled | Created | Break | Tokens |
                   ScriptArguments | IgnoreModifiable | Verbose,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = Disposed | Disabled | Enabled |
                    Created | Break | Tokens | ScriptArguments |
                    Isolated | IgnoreModifiable | Verbose,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Override = ForOverrideUse | CommonMask | OverrideMask,
        Now = ForNowUse | CommonMask | EnabledMask,
        Later = ForLaterUse | CommonMask | DisabledMask,
        Full = ForFullUse | CommonMask | FullMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = ForDefaultUse | CommonMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if !CONSOLE
    [ObjectId("81605ffc-3647-472c-acd4-5d79b9434ea0")]
    public enum ConsoleColor /* COMPAT: .NET Framework. */
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c661b7a4-c315-4ed7-b790-ea7a3505ff8a")]
    public enum TimeoutType
    {
        None = 0x0,       /* None, implicit only, do not use. */
        Invalid = 0x1,    /* Explicitly invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Lock = 0x2,       /* Used when attempting to acquire
                           * a lock on something, e.g. the
                           * interpreter. */

        Event = 0x4,      /* Used when dealing with script
                           * events sent to the dedicated
                           * script event thread.  To avoid
                           * smashing runaway script errors
                           * from a script, the associated
                           * timeout value should be higher
                           * than the script timeout. */

        Script = 0x8,     /* Used to avoid having runaway
                           * scripts.  Script evaluation
                           * will be canceled after the
                           * associated timeout value has
                           * elapsed. */

        Start = 0x10,     /* Used when making sure a thread
                           * has been started. */

        Interrupt = 0x20, /* Used when making sure a thread
                           * has been interrupted. */

        Join = 0x40,      /* Used when making sure a thread
                           * has exited.  Generally, the
                           * associated timeout value need
                           * not be too high because any
                           * pending scripts and/or other
                           * user-defined code should have
                           * already been canceled. */

        Dispose = 0x80    /* Used when disposing a thread
                           * -OR- event of some kind. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("99b147d8-cb56-40dc-a7ca-bc6ded108bad")]
    public enum CreationFlagTypes
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CurrentCreateFlags = 0x10,
        CurrentHostCreateFlags = 0x20,
        CurrentInitializeFlags = 0x40,
        CurrentScriptFlags = 0x80,
        CurrentInterpreterFlags = 0x100,
        CurrentPluginFlags = 0x200,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultCreateFlags = 0x1000,
        DefaultHostCreateFlags = 0x2000,
        DefaultInitializeFlags = 0x4000,
        DefaultScriptFlags = 0x8000,
        DefaultInterpreterFlags = 0x10000,
        DefaultPluginFlags = 0x20000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FallbackCreateFlags = 0x100000,
        FallbackHostCreateFlags = 0x200000,
        FallbackInitializeFlags = 0x400000,
        FallbackScriptFlags = 0x800000,
        FallbackInterpreterFlags = 0x1000000,
        FallbackPluginFlags = 0x2000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved1 = 0x10000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SettingsFlags = (AllCurrentFlags & ~(CurrentCreateFlags | CurrentHostCreateFlags)) |
                        (AllDefaultFlags & ~(DefaultCreateFlags | DefaultHostCreateFlags)) |
                        AllFallbackFlags,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllCurrentFlags = CurrentCreateFlags | CurrentHostCreateFlags |
                          CurrentInitializeFlags | CurrentScriptFlags |
                          CurrentInterpreterFlags | CurrentPluginFlags,

        AllDefaultFlags = DefaultCreateFlags | DefaultHostCreateFlags |
                          DefaultInitializeFlags | DefaultScriptFlags |
                          DefaultInterpreterFlags | DefaultPluginFlags,

        AllFallbackFlags = FallbackCreateFlags | FallbackHostCreateFlags |
                           FallbackInitializeFlags | FallbackScriptFlags |
                           FallbackInterpreterFlags | FallbackPluginFlags,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = AllDefaultFlags |
#if NET_STANDARD_20
                  CurrentHostCreateFlags | /* HACK: Disable color for children, etc. */
#endif
                  Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
    [Flags()]
    [ObjectId("dc69c8a9-5970-4317-a5e3-fe980076cd22")]
    public enum TestResolveFlags
    {
        None = 0x0,                    /* No special handling. */
        Invalid = 0x1,                 /* Invalid, do not use. */
        AlwaysUseNamespaceFrame = 0x2, /* Always return the call frame
                                        * associated with the configured
                                        * namespace, if any. */
        NextUseNamespaceFrame = 0x4,   /* For the next call, return the call
                                        * frame associated with the configured
                                        * namespace, if any. */
        HandleGlobalOnly = 0x8,        /* Do not skip handling the call frame
                                        * lookup because the global-only flag
                                        * is set. */
        HandleAbsolute = 0x10,         /* Do not skip handling the call frame
                                        * lookup because the name is absolute.
                                        */
        HandleQualified = 0x20,        /* Do not skip handling the call frame
                                        * lookup because the name is qualified.
                                        */
        EnableLogging = 0x40,          /* Enable logging of all test IResolve
                                        * interface method calls. */

        Default = None
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9f719570-729f-4234-8282-0face8230536")]
    public enum TypeListFlags
    {
        None = 0x0,
        Invalid = 0x1,

        IntegerTypes = 0x2,
        FloatTypes = 0x4,
        StringTypes = 0x8,
        NumberTypes = 0x10,
        IntegralTypes = 0x20,
        NonIntegralTypes = 0x40,
        OtherTypes = 0x80,
        AllTypes = 0x100
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1bae8baa-330b-459a-a82f-97babf5c8c3f")]
    public enum WhiteSpaceFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Simple = 0x2,
        Extended = 0x4, /* Use "Extended ASCII?" */
        Unicode = 0x8,
        NoArrows = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Bell = 0x20,
        Backspace = 0x40,
        HorizontalTab = 0x80,
        LineFeed = 0x100,
        VerticalTab = 0x200,
        FormFeed = 0x400,
        CarriageReturn = 0x800,
        Space = 0x1000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForFormatted = 0x2000,
        ForVariable = 0x4000,
        ForBox = 0x8000,
        ForTest = 0x10000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InvisibleCharactersMask = Bell | Backspace | HorizontalTab |
                                  LineFeed | VerticalTab | FormFeed |
                                  CarriageReturn,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Technically, the space character is visible.
        //
        AllCharactersMask = InvisibleCharactersMask | Space,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForMask = ForFormatted | ForTest | ForVariable | ForBox,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FormattedUse = InvisibleCharactersMask | ForFormatted,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VariableUse = Simple | InvisibleCharactersMask | ForVariable,

        ///////////////////////////////////////////////////////////////////////////////////////////

        BoxUse = Extended | Unicode | InvisibleCharactersMask | ForBox,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TestUse = Extended | Unicode | AllCharactersMask | ForTest
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("37766464-c48b-4fbf-b00f-5239d86c0583")]
    public enum DetectFlags
    {
        None = 0x0,                 /* Nothing. */
        Invalid = 0x1,              /* Invalid, do not use. */
        Assembly = 0x2,             /* Use the specified assembly as the basis of
                                     * the core script library detection. */
        Environment = 0x4,          /* Use the process environment variables as the
                                     * basis the core script library detection. */
        Setup = 0x8,                /* Use the installed instance as the basis of
                                     * the core script library detection. */
        BaseDirectory = 0x10,       /* Use the secondary name for the sub-directory
                                     * containing the "lib" sub-directory. */
        Directory = 0x20,           /* Use the primary name for the sub-directory
                                     * containing the "lib" sub-directory. */
        AssemblyNameVersion = 0x40, /* Use the assembly name version number. */
        AssemblyVersion = 0x80,     /* Use the assembly version number. */
        PackageVersion = 0x100,     /* Use the package version number. */
        NoVersion = 0x200,          /* Try without any version number. */
        DetectOnly = 0x400,         /* Do not change any global state. */
        Verbose = 0x800,            /* Provide extra information to the caller
                                     * upon failure. */

        All = Assembly | Environment | Setup |
              BaseDirectory | Directory | AssemblyNameVersion |
              AssemblyVersion | PackageVersion | NoVersion,

        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9cb44f96-4dfe-4852-8f61-6046940d1758")]
    public enum PackageType
    {
        None = 0x0,        /* No package. */
        Invalid = 0x1,     /* Invalid, do not use. */
        Library = 0x2,     /* The script library package. */
        Test = 0x4,        /* The test suite package. */
        Host = 0x8,        /* Host-defined package. */
        Automatic = 0x10,  /* Attempt to automatically figure it out. */
        Default = 0x20,    /* Default package, for internal use only. */

        Mask = Library | Test | Host | Automatic,

        Any = Library | Test | Host /* Any known package type will work. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("dad4b809-342f-4da3-b533-7acdc54bae9e")]
    public enum EncodingType
    {
        None = 0x0,          /* None, do not use. */
        Invalid = 0x1,       /* Invalid, do not use. */
        System = 0x2,        /* Unicode */
        Default = 0x4,       /* UTF-8 */
        Binary = 0x8,        /* OneByte */
        Tcl = 0x10,          /* ISO-8859-1 */
        Channel = 0x20,      /* ISO-8859-1 */
        Text = 0x40,         /* UTF-8 */
        Script = 0x80,       /* UTF-8 */
        Xml = 0x100,         /* UTF-8 */
        Policy = 0x200,      /* UTF-8 */
        Profile = 0x400,     /* UTF-8 */

#if HISTORY
        History = 0x800,     /* UTF-8 */
#endif

        Base64 = 0x1000,     /* UTF-8 */
        RemoteUri = 0x2000,  /* UTF-8 */
        UnknownUri = 0x4000, /* UTF-8 */
        FileSystem = 0x8000  /* UTF-8 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
    //
    // WARNING: Values may be added, modified, or removed from this enumeration
    //          at any time.
    //
    [Flags()]
    [ObjectId("7642aad2-c1d7-4ac5-80d0-f5e2d1db9bbf")]
    public enum CacheFlags : ulong
    {
        None = 0x0,           /* Nothing. */
        Invalid = 0x1,        /* Invalid, do not use. */
        Argument = 0x2,       /* Operate on the Argument object cache. */
        StringList = 0x4,     /* Operate on the StringList cache. */
        IParseState = 0x8,    /* Operate on the IParseState cache. */
        IExecute = 0x10,      /* Operate on the IExecute cache. */
        Type = 0x20,          /* Operate on the Type cache. */
        ComTypeList = 0x40,   /* Operate on the COM TypeList cache. */
        Miscellaneous = 0x80, /* Operate on other, internal caches. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Hidden = 0x100,       /* Magical hidden flag, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForceTrim = 0x200,       /* Forcibly trim the specified caches if necessary,
                                  * ignorning any limits on how many items may be
                                  * trimmed at once. */
        Unlock = 0x400,          /* Unlock the specified caches. */
        Lock = 0x800,            /* Lock the specified caches. */
        DisableOnLock = 0x1000,  /* When locking a cache, disable it as well. */
        Reset = 0x2000,          /* Create the specified caches if necessary -AND-
                                  * reset their settings back to their originally
                                  * configured values. */
        Clear = 0x4000,          /* Empty the specified caches. */
        FullClear = 0x8000,      /* Force all subsystems fully cleared. */
#if CACHE_DICTIONARY
        SetProperties = 0x10000, /* Configure various properties of the caches. */
#endif
#if CACHE_STATISTICS
        KeepCounts = 0x20000,    /* When resetting the cache, keep its counts. */
        ZeroCounts = 0x40000,    /* When resetting the cache, zero its counts. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to enable more aggressive trimming
        //       of the associated cache, when appropriate (typically when
        //       under heavy load).
        //
        ForceTrimArgument = 0x100000,
        ForceTrimStringList = 0x200000,
        ForceTrimIParseState = 0x400000,
        ForceTrimIExecute = 0x800000, /* NOT YET IMPLEMENTED */
        ForceTrimType = 0x1000000,
        ForceTrimComTypeList = 0x2000000,
        ForceTrimMiscellaneous = 0x4000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to prevent the automatic enabling,
        //       disabling, or clearing of the associated cache, when it
        //       would have been appropriate (typically when under heavy
        //       load).
        //
        LockArgument = 0x8000000,
        LockStringList = 0x10000000,
        LockIParseState = 0x20000000,
        LockIExecute = 0x40000000, /* NOT YET IMPLEMENTED */
        LockType = 0x80000000,
        LockComTypeList = 0x100000000,
        LockMiscellaneous = 0x200000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to automatically create the
        //       associated cache -AND- reset its settings back to
        //       the originally configured values when appropriate
        //       (typically when under heavy load).
        //
        ResetArgument = 0x400000000,
        ResetStringList = 0x800000000,
        ResetIParseState = 0x1000000000,
        ResetIExecute = 0x2000000000,
        ResetType = 0x4000000000,
        ResetComTypeList = 0x8000000000,
        ResetMiscellaneous = 0x10000000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to automatically clear the
        //       associated cache when appropriate (typically when
        //       under heavy load).
        //
        ClearArgument = 0x20000000000,
        ClearStringList = 0x40000000000,
        ClearIParseState = 0x80000000000,
        ClearIExecute = 0x100000000000,
        ClearType = 0x200000000000,
        ClearComTypeList = 0x400000000000,
        ClearMiscellaneous = 0x800000000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags represent the various origins of the
        //       data used to fetch (or create) cached arguments.
        //       By default, they are currently all enabled.  It
        //       is now possible to disable one or more origins
        //       by changing the cache flags for the interpreter,
        //       which may improve performance in some rare use
        //       cases.
        //
        ForResult = 0x1000000000000,
        ForResultWithLocation = 0x2000000000000,
        ForVariant = 0x4000000000000,
        ForProcedure = 0x8000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        HiddenIExecute = IExecute | Hidden,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        MaybeSetProperties = SetProperties,
#else
        MaybeSetProperties = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        MaybeKeepCounts = KeepCounts,
        MaybeZeroCounts = ZeroCounts,
#else
        MaybeKeepCounts = None,
        MaybeZeroCounts = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ObjectMask = Argument | StringList | IParseState |
                     IExecute | Type | ComTypeList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForMask = ForResult | ForResultWithLocation | ForVariant |
                  ForProcedure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagMask = Hidden | ForceTrim | Unlock | Lock |
                   DisableOnLock | Reset | Clear |
                   MaybeSetProperties | MaybeKeepCounts,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForceTrimMask = ForceTrimArgument | ForceTrimStringList | ForceTrimIParseState |
                        ForceTrimIExecute | ForceTrimType | ForceTrimComTypeList,

        LockMask = LockArgument | LockStringList | LockIParseState |
                   LockIExecute | LockType | LockComTypeList,

        ResetMask = ResetArgument | ResetStringList | ResetIParseState |
                    ResetIExecute | ResetType | ResetComTypeList,

        ClearMask = ClearArgument | ClearStringList | ClearIParseState |
                    ClearIExecute | ClearType | ClearComTypeList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultForceTrimMask = ForceTrimArgument | ForceTrimStringList |
                               ForceTrimIParseState,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Do not automatically clear the StringList cache when under
        //       heavy load because it is very expensive to refill.
        //
        DefaultClearMask = ClearMask & ~ClearStringList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: Are these good defaults?
        //
        Default = ObjectMask | Miscellaneous | ForMask |
                  DefaultForceTrimMask | ResetMask |
                  DefaultClearMask | MaybeSetProperties |
                  MaybeKeepCounts
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1938facf-a73c-43d6-82d3-95a6d6cd0b63")]
    public enum HostStreamFlags
    {
        None = 0x0,                  /* No special handling. */
        Invalid = 0x1,               /* Invalid, do not use. */
        LoadedPlugins = 0x2,         /* Check loaded plugins for the stream. */
        CallingAssembly = 0x4,       /* NOT USED: Check the calling assembly for the stream. */
        EntryAssembly = 0x8,         /* Check the entry assembly for the stream. */
        ExecutingAssembly = 0x10,    /* Check the executing assembly for the stream. */
        ResolveFullPath = 0x20,      /* Resolve the file name to a fully qualified path. */
        AssemblyQualified = 0x40,    /* The returned resolved full path should include
                                      * the assembly location. */
        PreferFileSystem = 0x80,     /* Check the file system before checking any
                                      * assemblies. */
        SkipFileSystem = 0x100,      /* Skip checking the file system. */
        Script = 0x200,              /* From the script engine, etc. */
        Open = 0x400,                /* From the [open] command, etc. */
        FoundViaPlugin = 0x800,      /* Stream was opened from an assembly resource. */
        FoundViaAssembly = 0x1000,   /* Stream was opened from an assembly resource. */
        FoundViaFileSystem = 0x2000, /* Stream was opened from the file system. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        OptionMask = ResolveFullPath | AssemblyQualified | PreferFileSystem | SkipFileSystem,
        AssemblyMask = CallingAssembly | EntryAssembly | ExecutingAssembly,
        FoundMask = FoundViaPlugin | FoundViaAssembly | FoundViaFileSystem,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EngineScript = Default | ResolveFullPath | Script,
        OpenCommand = Default | Open
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("e24c641b-f0fe-4e52-b5f5-31a80acbbf52")]
    public enum ActionType
    {
        None = 0x0,                      /* do nothing. */
        Invalid = 0x1,                   /* invalid, do not use. */
        CheckForUpdate = 0x2,            /* check for an update; however, do not
                                          * download it. */
        FetchUpdate = 0x4,               /* check for an update and download it
                                          * if necessary. */
        RunUpdateAndExit = 0x8,          /* run the external update tool and then
                                          * exit. */
        DownloadAndExtractUpdate = 0x10, /* download the update and extract it to
                                          * the specified directory. */

        Default = CheckForUpdate
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("fd610430-d4f3-4525-b72a-e5746136884f")]
    public enum HostWriteType
    {
        None = 0,
        Invalid = 1,
        Normal = 2,
        Debug = 3,
        Error = 4,
        Flush = 5, /* Flush() only, no Write*(). */

        //
        // NOTE: External callers (i.e. those calling from outside of the core
        //       library) must NOT rely on the semantics of the "Default" value
        //       here.
        //
        Default = Normal
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("07486dfd-5b8b-41f1-ada1-264f908f0a3d")]
    public enum HostColor
    {
        Invalid = -2,
        None = -1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("927338b0-c8d2-459d-9188-c85baf807990")]
    public enum Base26FormattingOption
    {
        None = 0x0,
        InsertLineBreaks = 0x1, // line break every 74 characters.
        InsertSpaces = 0x2,     // insert one space every 2 characters.

        Default = InsertLineBreaks | InsertSpaces
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6a65da98-d7ab-4ebc-a65b-a8db748340ce")]
    public enum InterpreterType
    {
        None = 0x0,
        Invalid = 0x1,
        Eagle = 0x2,
#if NATIVE && TCL
        Tcl = 0x4,
#if TCL_THREADS
        TclThread = 0x8,
#endif
#endif

        Parent = 0x10,
        Child = 0x20,
        Nested = 0x40,
        Token = 0x80,

        Default = Eagle | Parent | Child | Nested | Token
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6b5ff78b-3562-4cb3-882b-2cdaf9963184")]
    public enum EventType
    {
        None = 0,                /* Do nothing. */
        Idle = 1,                /* Process any pending events. */
        Callback = 2,            /* The event represents an EventCallback
                                  * delegate. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        PreviewKeyDown = 3,
        KeyDown = 4,
        KeyPress = 5,
        KeyUp = 6,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        Create = 7,              /* Create the interpreter. */
        Delete = 8,              /* Delete the interpreter. */
        Expression = 9,          /* Evaluate an expression (contained in the
                                  * ClientData). */
        Evaluate = 10,           /* Evaluate a script (in the triplet contained
                                  * in the ClientData). */
        SimpleEvaluate = 11,     /* Evaluate a script (contained in the
                                  * ClientData). */
        Substitute = 12,         /* Perform substitutions on a string (contained
                                  * in the ClientData). */
        Cancel = 13,             /* Cancel the script in progress (error message
                                  * contained in the ClientData). */
        Unwind = 14,             /* Unwind the script in progress (error message
                                  * contained in the ClientData). */
        ResetCancel = 15,        /* Reset the cancel and unwind flags for the Tcl
                                  * interpreter. */
        GetVariable = 16,        /* Get the value of a variable (name contained
                                  * in the ClientData). */
        SetVariable = 17,        /* Set the value of a variable (name/value pair
                                  * contained in the ClientData). */
        UnsetVariable = 18,      /* Unset a variable (name contained in the
                                  * ClientData). */
        AddCommand = 19,         /* Add an IExecute to the interpreter
                                  * (name/ICommand pair contained in the
                                  * ClientData). */
        AddStandardCommand = 20, /* Add a standard bridge between the Eagle
                                  * [eval] command and the Tcl [eagle] command. */
        RemoveCommand = 21,      /* Remove an IExecute from the interpreter
                                  * (name contained in the ClientData). */
        GetResult = 22,          /* Fetch the interpreter result, if any. */
        Dispose = 23             /* Fully dispose of all Tcl thread resources. */
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ab21fc28-02d8-4401-a88d-faafbedbd5bf")]
    public enum EngineMode
    {
        None = 0x0,
        Invalid = 0x1,
        EvaluateExpression = 0x2,
        EvaluateScript = 0x4,
        EvaluateFile = 0x8,
        SubstituteString = 0x10,
        SubstituteFile = 0x20
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("5b186e18-6b14-4f13-a685-6e812e301467")]
    public enum ShutdownFlags
    {
        None = 0x0,         /* No special handling. */
        Invalid = 0x1,      /* Invalid, do not use. */
        Reserved1 = 0x2,    /* Reserved, do not use. */
        IgnoreAlive = 0x4,  /* Invalid, do not use. */
        Interrupt = 0x8,    /* Attempt to interrupt the thread. */
        WaitBefore = 0x10,  /* Wait for a bit before doing anything else. */
        WaitAfter = 0x20,   /* Wait for a bit after attempting an interrupt. */
        NoAbort = 0x40,     /* Avoid aborting the thread. */
        NoReset = 0x80,     /* Only reset thread when it dies. */
        ResetAbort = 0x100, /* Reset thread after abort. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForCancel = 0x200,          /* Operation related to script cancellation. */
        ForStatus = 0x400,          /* Related to the status object. */
        ForTimeout = 0x800,         /* Related to an operational timeout. */
        ForTimeoutAndWait = 0x1000, /* Related to an operational timeout. */
        ForScriptEvent = 0x2000,    /* Related to scripted event processing. */
        ForScriptTimeout = 0x4000,  /* Related to a script timeout. */
        ForExternal = 0x8000,       /* Caller is external to the library. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Cancel = Interrupt | NoAbort | ForCancel | Reserved1,
        Status = Interrupt | WaitAfter | NoAbort | NoReset | ForStatus | Reserved1,
        Timeout = Cancel | WaitBefore | ForTimeout | Reserved1,
        TimeoutAndWait = Timeout | WaitAfter | ForTimeoutAndWait | Reserved1,
        ScriptEvent = ForScriptEvent | Reserved1,
        ScriptTimeout = Interrupt | WaitAfter | ForScriptTimeout | Reserved1,
        External = ForExternal | Reserved1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Interrupt | Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7ea5444a-0ee3-4b33-bd80-be64722e86fb")]
    public enum ThreadFlags
    {
        None = 0x0,                /* No special handling. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Invalid = 0x1,             /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ThrowOnDisposed = 0x2,     /* Throw exceptions when attempting to access a
                                    * disposed interpreter. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Safe = 0x4,                /* Create interpreter as "safe". */
        NoHidden = 0x8,            /* Omit "unsafe" commands instead of hiding them. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UserInterface = 0x10,      /* Thread must be able to use WinForms and/or WPF. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        IsBackground = 0x20,       /* Thread will not prevent the process from
                                    * terminating. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseActiveStack = 0x40,     /* Thread will make use of the active interpreter
                                    * stack. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Quiet = 0x80,              /* The interpreter will be set to "quiet" mode. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoBackgroundError = 0x100, /* The background error handling for the created
                                    * interpreter will be disabled. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseSelf = 0x200,           /* Create an aliased object named "thread" that
                                    * can be used to access the ScriptThread object
                                    * itself from inside the contained interpreter. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoCancel = 0x400,          /* Script cancellation will not cause the thread
                                    * to exit. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        StopOnError = 0x800,       /* Script errors will cause the thread to exit. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ErrorOnEmpty = 0x1000,     /* An empty event queue will cause the thread to
                                    * exit. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclThread = 0x2000,        /* Should native Tcl events be processed? */
        TclWaitEvent = 0x4000,     /* Wait for a Tcl event?  This should almost
                                    * never be used. */
        TclAllEvents = 0x8000,     /* Process all native Tcl events each time? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoComplain = 0x10000,      /* Disable popup messages for errors that cannot
                                    * be reported any other way.  The errors will
                                    * still be logged to the active trace listners,
                                    * if any. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Verbose = 0x20000,         /* Enable more diagnostic output for key lifecycle
                                    * events (e.g. shutdown). */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Debug = 0x40000,           /* Enable debugging mode.  Currently, sets the
                                    * "EventFlags.Debug" flag for all events queued
                                    * via the interpreter event manager (i.e. not
                                    * those events handled directly by the engine). */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UsePool = 0x80000,         /* Instead of creating a real managed thread,
                                    * use the thread pool.  This prevents various
                                    * other flags from doing anything, including the
                                    * "UserInterface", "IsBackground", and "Start"
                                    * flags. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Start = 0x100000,          /* Start the thread prior to returning from the
                                    * "Create" method instead of waiting until the
                                    * "Start" method is called. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoAbort = 0x200000,        /* Disable all use of the Thread.Abort() method. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Attach = 0x400000,         /* Use an existing interpreter instead of creating
                                    * one. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        PurgeGlobal = 0x800000,    /* Purge the global call frame when the script
                                    * thread is exiting. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Trace = 0x1000000,         /* Enable tracing of key events, e.g. [vwait], etc. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        FollowLink = 0x2000000,    /* Follow variable links when using [vwait]. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Restricted = 0x4000000,    /* Only permit use of the Send() and Queue() methods. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CommonUse = NoComplain | Start | UseActiveStack | PurgeGlobal,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VariableUse = Trace | FollowLink,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardUse = CommonUse | ThrowOnDisposed,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InterfaceUse = StandardUse | UserInterface,
        ServiceUse = StandardUse | NoCancel,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclInterfaceUse = StandardUse | UserInterface | TclThread | TclAllEvents,
        TclServiceUse = StandardUse | NoCancel | TclThread | TclAllEvents,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        TaskUse = StandardUse | UseSelf,
        SafeTaskUse = TaskUse | Safe,
        RestrictedTaskUse = TaskUse | Safe | Restricted,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyUse = InterfaceUse | ServiceUse | TaskUse,
        AnySafeUse = InterfaceUse | ServiceUse | TaskUse | Safe,
        AnyRestrictedUse = InterfaceUse | ServiceUse | TaskUse | Safe | Restricted,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the ScriptThread.Attach (static) methods only.
        //
        AttachUse = (InterfaceUse & ~PurgeGlobal) | Attach,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = VariableUse | InterfaceUse // TODO: Good default?
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    // [Flags()]
    [ObjectId("66f29e4a-2adf-4641-956a-23505d82bbaa")]
    public enum EventPriority
    {
        Automatic = -1,         /* Automatically assign event priority based on flags. */
        None = 0x0,             /* Do not use. */
        Invalid = 0x1,          /* Do not use. */
        Highest = 0x2,          /* The highest possible event priority. */
        High = 0x4,
        Medium = 0x8,           /* The standard event priority. */
        Low = 0x10,
        Lowest = 0x20,          /* The lowest possible event priority. */

        Immediate = Highest,    /* The priority used by events that should execute
                                 * even if the event manager is not actively in a
                                 * wait operation (a.k.a. "engine events"). */
        Idle = Lowest,          /* The event priority used by [after idle]. */
        After = Medium,         /* The event priority used by [after XXXX <script>]. */
        Normal = Medium,        /* The default event priority used by QueueEvent in
                                 * auto-detection mode when none of the other event
                                 * flags match. */

        CheckEvents = Default,  /* The event priority used by by the Engine.CheckEvents
                                 * method. */
        Service = Default,      /* The event priority used by [interp service]. */
        Update = Default,       /* The event priority used by [update]. */
        WaitVariable = Default, /* The event priority used by [vwait]. */
        QueueEvent = Default,   /* The event priority used by the QueueEvent method(s). */
        QueueScript = Default,  /* The event priority used by the QueueScript method(s). */

#if NATIVE && TCL
        TclThread = Default,    /* The event priority used by the TclThread class. */
#endif

        Default = Automatic     /* The default event priority. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("42ababff-a006-48c3-8e9e-38aae6936a02")]
    public enum WatchdogType
    {
        None = 0x0,          /* No special handling. */
        Invalid = 0x1,       /* Invalid, do not use. */

        Timeout = 0x100,     /* Manage the timeout watchdog thread. */
        Health = 0x200,      /* Manage the health watchdog thread. */

        ForDefault = 0x1000, /* The default watchdog type is being
                              * used. */

        TypeMask = Timeout | Health,

        Default = Timeout | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("39223dda-bcf9-4d91-af70-7e2c1c59d9b0")]
    public enum WatchdogOperation
    {
        None = 0x0,     /* No special handling. */
        Invalid = 0x1,  /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Check = 0x2,    /* Is the watchdog thread active? */
        Fetch = 0x4,    /* Fetch the watchdog thread. */
        Start = 0x8,    /* Start the watchdog thread. */
        Stop = 0x10,    /* Stop the watchdog thread. */
        Restart = 0x20, /* Restart the watchdog thread. */
        Attach = 0x40,  /* Attach thread to watchdog. */
        Detach = 0x80,  /* Detach thread from watchdog. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveStart = 0x200, /* Can the watchdog thread query the
                                   * interactive user? */
        ForceStart = 0x400,       /* Force watchdog thread to start even
                                   * when the timeout is infinite? */
        MustBeAlive = 0x800,      /* Check IsAlive prior to considering
                                   * an existing watchdog thread to be
                                   * started. */
        StrictStart = 0x1000,     /* Fail starting the watchdog thread
                                   * if it is already running. */
        StrictStop = 0x2000,      /* Fail stopping the watchdog thread
                                   * if it is not running. */
        NoAbort = 0x4000,         /* Avoid using the Thread.Abort()
                                   * method. */
        NoName = 0x8000,          /* Do not check the name of attached
                                   * watchdog threads. */
        Verbose = 0x10000,        /* Retain all result messages. */
        Interrupt = 0x20000,      /* Enable interrupting the primary
                                   * thread for the target interpreer */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CheckAndFlags = Check | MustBeAlive,
        StartAndFlags = Start | ForceStart | MustBeAlive | StrictStart,
        StopAndFlags = Stop | StrictStop,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = InteractiveStart | ForceStart | MustBeAlive |
                    StrictStart | StrictStop | NoAbort | NoName |
                    Verbose | Interrupt
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7344c9d6-999e-4730-9aad-0c7aee4502f7")]
    public enum EventFlags
    {
        None = 0x0,            /* No special handling. */
        Invalid = 0x1,         /* Invalid, do not use. */
        Queued = 0x2,          /* This event is being queued for execution later. */
        Direct = 0x4,          /* This event is being executed immediately. */
        UnknownThread = 0x8,   /* This event is being handled by an unknown thread (could be
                                * a worker thread, the main thread, or a thread-pool thread). */
        SameThread = 0x10,     /* This event is being handled by the same thread that
                                * created it. */
        InterThread = 0x20,    /* This event is being handled by a thread other than the
                                * one that created it. */
        Internal = 0x40,       /* This event is directed at the interpreter. */
        External = 0x80,       /* This event is not directed at the interpreter. */
        After = 0x100,         /* This event originated from the [after] command and must
                                * not be executed until a wait is initiated by a script. */
        Immediate = 0x200,     /* This event should be executed as soon as the interpreter
                                * can safely do so. */
        Idle = 0x400,          /* This event should be executed as soon as the interpreter
                                * can safely do so and is idle. */
        Synchronous = 0x800,   /* The code that created this event is blocking until it
                                * completes. */
        Asynchronous = 0x1000, /* The code that created this event queued it and continued
                                * executing. */
        Debug = 0x2000,        /* This event should produce debugging diagnostics. */
        Timing = 0x4000,       /* This event should produce timing diagnostics. */
        NoCallback = 0x8000,   /* Skip executing a callback if one exists. */
        NoIdle = 0x10000,      /* Skip processing idle events. */
        NoNotify = 0x20000,    /* Skip notifying the caller of event completion.  This is
                                * only used by the native Tcl integration subsystem. */
        IdleIfEmpty = 0x40000, /* Only process idle events if the queue is otherwise empty. */
        Interpreter = 0x80000, /* For queued scripts only, prefer the event flags from the
                                * interpreter instead of the ones provided with the script
                                * itself. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoBgError = 0x100000,      /* Skip invoking background error handler for this event. */
        FireAndForget = 0x200000,  /* The IEvent object may be disposed after the event has
                                    * been serviced (i.e. the caller does NOT need to obtain
                                    * the result).  For now, this is for core library use
                                    * only.  Please do not use it. */
        WasDequeued = 0x400000,    /* The event was somehow removed from the event queue. */
        WasCompleted = 0x800000,   /* The event was executed.  This does not imply that it
                                    * was "successful". */
        WasCanceled = 0x1000000,   /* The event was canceled somehow.  This currently implies
                                    * that it MAY have been removed from the event queue as
                                    * well. */
        WasDiscarded = 0x2000000,  /* The event was discarded somehow.  This currently implies
                                    * that it MAY have been removed from the event queue as
                                    * well. */
        DisposeThread = 0x4000000, /* The thread local data should be disposed after the event
                                    * is complete. */
        GreedyThread = 0x8000000,  /* The event should be processed even if it has a non-null
                                    * thread identifier -AND- the current call is processing
                                    * non-thread specific events (i.e. [vwait] called without
                                    * a -thread option value). */

        ///////////////////////////////////////////////////////////////////////////////////////////

        DequeueMask = After | Immediate | Idle, /* The flags that modify the event dequeuing
                                                 * behavior. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Engine = Immediate,          /* The types of events that are checked for and executed
                                      * during script evaluation and command execution (by the
                                      * engine).  This flag is designed for use by the engine
                                      * only (i.e. it is not for use by external applications
                                      * and plugins).  These events are NEVER guaranteed to be
                                      * serviced on any particular thread. */
        Service = Immediate,         /* The types of events that are checked for and executed
                                      * during ServiceEvents and related methods.  These events
                                      * are NEVER guaranteed to be serviced on any particular
                                      * thread. */
        Queue = Immediate,           /* The types of events that are queued, by default, via
                                      * QueueScript.  These events are NEVER guaranteed to be
                                      * serviced on any particular thread. */
        Wait = After | Immediate,    /* The types of events that are checked for and serviced
                                      * during the [vwait] and [update] commands.  This flag
                                      * is designed for use by [vwait] and [update] only (i.e.
                                      * it is not for use by external applications and
                                      * plugins).  Normally, [after] events are guaranteed to
                                      * be serviced on the primary thread for the interpreter;
                                      * however, that guarantee does NOT apply if external
                                      * applications and plugins use this flag. */
        All = After | Immediate,     /* Service all events.  Great care should be used with
                                      * this flag because scripts queued by [after] will not
                                      * necessarily be serviced at the next [vwait].  Also,
                                      * events are not guaranteed to be serviced on any
                                      * particular thread if this flag is used. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Engine
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8d4d663a-6e43-4ccf-993c-238aebe4a617")]
    public enum CancelFlags : ulong
    {
        None = 0x0,                         /* Use default handling. */
        Invalid = 0x1,                      /* Invalid, do not use. */

#if NOTIFY
        Notify = 0x2,                       /* Enable notification callbacks. */
#endif

        Cancel = 0x4,                       /* Cancel the script being evaluated.
                                             * This is not currently used. */
        Unwind = 0x8,                       /* Completely unwind the script call
                                             * stack during script cancellation. */
        Global = 0x10,                      /* Check (or set) state globally, for
                                             * all threads. */
        Local = 0x20,                       /* Check (or set) state locally, for
                                             * the current thread only. */
        Other = 0x40,                       /* Check (or set) state locally, for
                                             * the threads OTHER than the current
                                             * one only. */
        ResetGlobal = 0x80,                 /* Reset the global cancel flag(s)? */
        ResetLocal = 0x100,                 /* Reset the local cancel flag(s)? */
        ResetAll = 0x200,                   /* Reset the global and/or local cancel
                                             * flags even if one of them has already
                                             * been reset. */
        NeedResult = 0x400,                 /* Update result parameter sent by the
                                             * caller to reflect the new interpreter
                                             * state. */
        IgnorePending = 0x800,              /* When resetting the script cancellation
                                             * state, ignore the number of pending
                                             * evaluations -OR- interactive loops. */
        FailPending = 0x1000,               /* When resetting the script cancellation
                                             * state, treat pending evaluations -OR-
                                             * interactive loops as an error. */
        StopOnError = 0x2000,               /* When attempting to cancel more than
                                             * one script evaluation, stop on the
                                             * first error encountered. */
        UseThreadInterrupt = 0x4000,        /* Also attempt to interrupt the primary
                                             * thread for the interpreter. */
        UseThreadAbort = 0x8000,            /* WARNING: *DANGEROUS* Also attempt to
                                             * abort the primary thread for the
                                             * interpreter. */
        WaitForThread = 0x10000,            /* Wait for the thread after attempting
                                             * to interrupt it. */

#if SHELL
        UseInteractiveThread = 0x20000,     /* Use the interactive thread, if present,
                                             * instead of the primary thread. */
#endif

#if NATIVE && TCL
        NoNativeTcl = 0x40000,              /* Skip dealing with any native Tcl
                                             * interpreters that may be present. */
#endif

#if DEBUGGER && DEBUGGER_ENGINE
        NoBreakpoint = 0x80000,             /* Skip checking for any script
                                             * breakpoints. */
#endif

        NoComplain = 0x100000,              /* Skip complaining about being unable
                                             * to cancel (e.g. due to interpreter
                                             * disposal). */
        NoBusy = 0x200000,                  /* Ignore the busyness state of the
                                             * interpreter. */
        NoLock = 0x400000,                  /* Attempt to avoid the need to acquire
                                             * the interpreter lock. */
        TryLock = 0x800000,                 /* Attempt to acquire the interpreter
                                             * lock via the TryEnter method. */

#if SHELL
        UnpauseInteractiveLoop = 0x1000000, /* Forcibly "unpause" the primary
                                             * interactive loop. */
        AllInteractiveLoops = 0x2000000,    /* Apply handling to all interactive
                                             * loops. */
#endif

        AllInterpreters = 0x4000000,        /* Attempt to initiate the script
                                             * cancellation operation on all
                                             * known interpreters. */

        AllContexts = 0x8000000,            /* Attempt to initiate the script
                                             * cancellation operation on all
                                             * known interpreter contexts.  This
                                             * flag is not yet implemented. */

        WaitForLock = 0x10000000,           /* When the interpreter lock is needed,
                                             * wait a bit before giving up. */
        ForceContext = 0x20000000,          /* Force the creation of the necessary
                                             * thread context(s), e.g. an engine
                                             * context, prior to setting any local
                                             * script cancellation flags. */

        NoCancel = 0x40000000,              /* Do not actually cancel anything.
                                             * This flag is only used by the
                                             * DisposeInterpreters method. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForDebug = 0x80000000,              /* For use with the [debug] command
                                             * only. */
        ForInterp = 0x100000000,            /* For use with the [interp] command
                                             * only. */
        ForTest2 = 0x200000000,             /* For use with the [test2] command
                                             * only. */
        ForTime = 0x400000000,              /* For use with the [time] command
                                             * only. */
        ForTry = 0x800000000,               /* For use with the [try] command
                                             * only. */
        ForCatch = 0x1000000000,            /* For use with the [catch] command
                                             * only. */
        ForEngine = 0x2000000000,           /* For use by the Engine class only. */
        ForReady = 0x4000000000,            /* For use by the Interpreter.Ready
                                             * method only. */
        ForInteractive = 0x8000000000,      /* For use by the interactive loop
                                             * only. */
        ForExternal = 0x10000000000,        /* For use by external (i.e. outside
                                             * of the core library) components
                                             * only. */
        ForBgError = 0x20000000000,         /* For use by the event manager only. */
        ForCommandCallback = 0x40000000000, /* For use by CommandCallback class
                                             * only. */
        ForScriptTimeout = 0x80000000000,   /* For use by the RuntimeOps class
                                             * only. */
        ForScriptEvent = 0x100000000000,    /* For use by the ScriptEventState
                                             * class only. */
        ForScriptThread = 0x200000000000,   /* For use with the ScriptThread
                                             * class only. */
        ForSettings = 0x400000000000,       /* For use by the LoadSettingsViaFile
                                             * method only. */
        ForTimeout = 0x800000000000,        /* For use by the timeout threads. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if THREADING
        UseGlobalAndLocal = Global | Local,
        UseGlobalOrLocal = Local,
        ResetGlobalAndLocal = ResetGlobal | ResetLocal,
        ResetGlobalOrLocal = ResetLocal,
#else
        UseGlobalAndLocal = Global,
        UseGlobalOrLocal = Global,
        ResetGlobalAndLocal = ResetGlobal,
        ResetGlobalOrLocal = ResetGlobal,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        CheckLocalState = UseGlobalOrLocal | ResetGlobalOrLocal,
        CheckGlobalState = UseGlobalAndLocal | ResetGlobalAndLocal,

#if NOTIFY
        NoState = Notify,
        SetLocalState = Notify | UseGlobalOrLocal | NeedResult | IgnorePending,
        SetGlobalState = Notify | UseGlobalAndLocal | NeedResult | IgnorePending,
        ResetLocalState = Notify | UseGlobalOrLocal | NeedResult,
        ResetGlobalState = Notify | UseGlobalAndLocal | NeedResult,
#else
        NoState = None,
        SetLocalState = UseGlobalOrLocal | NeedResult | IgnorePending,
        SetGlobalState = UseGlobalAndLocal | NeedResult | IgnorePending,
        ResetLocalState = UseGlobalOrLocal | NeedResult,
        ResetGlobalState = UseGlobalAndLocal | NeedResult,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: These two values are for external use only.
        //
        UnwindAndNotify = SetGlobalState | Unwind | ForExternal, /* EXEMPT */
        IgnorePendingAndNotify = ResetGlobalState | IgnorePending | ForExternal, /* EXEMPT */

        ///////////////////////////////////////////////////////////////////////////////////////////

        DebugHalt = SetLocalState | ForDebug,
        InterpCancel = SetLocalState | ForInterp,
        ScriptTimeout = SetLocalState | Unwind | NoComplain | NoLock | ForScriptTimeout,
        Timeout = SetLocalState | Unwind | NoComplain | NoLock | ForTimeout,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InterpResetCancel = ResetLocalState | ForInterp,
        Test2 = ResetLocalState | IgnorePending | ForTest2,
        Time = ResetLocalState | IgnorePending | ForTime,
        BgError = ResetLocalState | IgnorePending | ForBgError,
        CommandCallback = ResetLocalState | FailPending | ForCommandCallback,
        DebugSecureEval = ResetLocalState | IgnorePending | ForDebug,
        Settings = ResetGlobalState | IgnorePending | ForSettings,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TryBlock = ResetLocalState | IgnorePending | ForTry,
        CatchBlock = ResetLocalState | IgnorePending | ForCatch,
        FinallyBlock = SetLocalState | ForTry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Engine = ResetLocalState | ForEngine,
        Ready = CheckGlobalState | NeedResult | ForReady,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ScriptEvent = ResetGlobalState | IgnorePending | ForScriptEvent,
        ScriptThread = ResetGlobalState | IgnorePending | ForScriptThread,

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region For Interactive Use Only
        ShellResetCancel = ResetGlobalState | ForInteractive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveIsHalted = CheckGlobalState | NeedResult | ForInteractive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveManualHalt = SetLocalState | IgnorePending | ForInteractive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveManualResetCancel = ResetGlobalState | IgnorePending | ForInteractive,
        InteractiveManualResetHalt = ResetGlobalState | IgnorePending | ForInteractive,
        InteractiveAutomaticResetHalt = ResetGlobalState | ForInteractive,
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveCancelEvent = NoState |
#if SHELL
            UnpauseInteractiveLoop |
#endif
            ForInteractive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveCancelThread = NoState |
            UseThreadInterrupt |
#if SHELL
            UseInteractiveThread |
#endif
            ForInteractive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("70cc84ac-f876-438f-a06e-fa2ef80e3bf8")]
    public enum TrustFlags
    {
        None = 0x0,                /* Use default handling. */
        Invalid = 0x1,             /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Shared = 0x2,               /* Allow other threads to use the interpreter
                                     * while evaluating the script (DANGEROUS).  Use
                                     * with great care, if ever. */
        WithEvents = 0x4,           /* Allow asynchronous events to be processed via
                                     * the event manger while evaluating the script
                                     * (DANGEROUS).  Use with great care, if ever. */
        MarkTrusted = 0x8,          /* Temporarily mark the interpreter as "trusted". */
        AllowUnsafe = 0x10,         /* Permit "trusted" evaluation even for "unsafe"
                                     * interpreters (i.e. those that are already marked
                                     * as "trusted"). */
        NoIgnoreHidden = 0x20,      /* Do not enable execution of hidden commands. */
        ViaCoreLibrary = 0x40,      /* For use by the core library only. */
        UseSecurityLevels = 0x80,   /* Increment/decrement the SecurityLevels when
                                     * evaluating the script. */
        PushScriptLocation = 0x100, /* Wrap the script evaluation in calls to push and
                                     * pop the script location (file name). */
        WithScopeFrame = 0x200,     /* Create and push a new [scope] frame prior to
                                     * evaluating the script. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        NoIsolatedPlugins = 0x400, /* Prevent plugins from being loaded in isolated
                                    * application domains. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Temporarily mark the interpreter as "trusted" unless this is
        //       unnecessary because the interpreter is already "trusted".
        //
        MaybeMarkTrusted = MarkTrusted | AllowUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used when evaluating the Harpy / Badge security
        //       script fragments that are used to enable or disable script
        //       signing policies and core script certificates.  This is only
        //       done in response to the "-security" command line option -OR-
        //       by calling the ScriptOps.EnableOrDisableSecurity method.
        //
        SecurityPackage = MaybeMarkTrusted | ViaCoreLibrary | UseSecurityLevels,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None         /* WARNING: Do not change this value. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("15024598-4868-466e-a7cd-f6aa1069b4dc")]
    public enum Arity
    {
        Automatic = -3,      /* Use the value of the ArgumentsAttribute or the OperandsAttribute
                              * to determine the arity of the function or operator, respectively. */
        UnaryAndBinary = -2, /* This operator can accept one or two operands. */
        None = -1,           /* This function or operator can accept any number of arguments or
                              * operands. */
        Nullary = 0,
        Unary = 1,
        Binary = 2,
        Ternary = 3,
        Quaternary = 4,
        Quinary = 5,
        Senary = 6,
        Septenary = 7,
        Octary = 8,
        Nonary = 9
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e609aaf6-8e07-434b-ad07-758ad9e824ec")]
    public enum OperatorFlags
    {
        None = 0x0,
        Invalid = 0x1,             /* Invalid, do not use. */
        Core = 0x2,                /* This operator is part of the core
                                    * operator set. */
        Special = 0x4,             /* This operator requires special handling
                                    * by the expression engine. */
        Direct = 0x8,              /* The expression engine handles this
                                    * operator directly (i.e. without using an
                                    * IOperator class). */
        Breakpoint = 0x10,         /* Break into debugger before execution. */
        Disabled = 0x20,           /* The operator may not be executed. */
        Hidden = 0x40,             /* By default, the operator will not be
                                    * visible in the results of [info
                                    * operators]. */
        Standard = 0x80,           /* The operator is largely (or completely)
                                    * compatible with an identically named
                                    * operator from Tcl/Tk 8.4, 8.5, and/or
                                    * 8.6. */
        NonStandard = 0x100,       /* The operator is not present in Tcl/Tk
                                    * 8.4, 8.5, and/or 8.6 -OR- it is
                                    * completely incompatible with an
                                    * identically named operator in Tcl/Tk 8.4,
                                    * 8.5, and/or 8.6. */
        NoPopulate = 0x200,        /* The operator will not be returned by the
                                    * plugin manager. */
        NoTclMathOperator = 0x400, /* Disable adding the command to the
                                    * "tcl::mathop" namespace. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Arithmetic = 0x800,        /* Addition, subtraction, multiplication,
                                    * division, exponentiation, remainder. */
        Relational = 0x1000,       /* Equal to, not equal to, less than,
                                    * greater than, etc. */
        Conditional = 0x2000,      /* If-then-else, etc. */
        Logical = 0x4000,          /* Logical "and", "or", "not", "xor",
                                    * etc. */
        Bitwise = 0x8000,          /* Bitwise "and", "or", "not", "xor",
                                    * shift, rotate, etc. */
        Assignment = 0x10000,      /* Variable assignment operators. */
        String = 0x20000,          /* All the string-only operators. */
        List = 0x40000,            /* All the list-only operators. */
        Initialize = 0x80000,      /* This operator is needed in order to be
                                    * able to initialize the minimal script
                                    * library, i.e. "init.eagle". */
        SecuritySdk = 0x100000,    /* This operator is needed in order to use
                                    * the baseline security SDK. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        SubsetMask = Arithmetic | Relational | Conditional |
                     Logical | Bitwise | Assignment | String |
                     List,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are added to an operator when the parent interpreter
        //       is made "standard".
        //
        DisabledAndHidden = Disabled | Hidden
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("506fa37d-c5c2-4eb8-a4e0-27303cb669f0")]
    public enum MakeFlags
    {
        None = 0x0,              /* Use default handling. */
        Invalid = 0x1,           /* Invalid, do not use. */
        IncludeCommands = 0x2,   /* Disable and/or hide commands. */
        IncludeProcedures = 0x4, /* Disable and/or hide procedures. */
        IncludeFunctions = 0x8,  /* Disable and/or hide functions. */
        IncludeOperators = 0x10, /* Disable and/or hide operators. */
        IncludeVariables = 0x20, /* Disable and/or hide operators. */
        IncludeLibrary = 0x40,   /* Evaluate the associated library
                                  * script, if any. */
        ResetValue = 0x80,       /* Reset the value of all removed
                                  * variables. */

#if !MONO && NATIVE && WINDOWS
        ZeroString = 0x100,      /* Enable forcibly zeroing strings
                                  * that may contain "sensitive" data?
                                  * WARNING: THIS IS NOT GUARANTEED TO
                                  * WORK RELIABLY ON ALL PLATFORMS.
                                  * EXTREME CARE SHOULD BE EXERCISED
                                  * WHEN HANDLING ANY SENSITIVE DATA,
                                  * INCLUDING TESTING THAT THIS FLAG
                                  * WORKS WITHIN THE SPECIFIC TARGET
                                  * APPLICATION AND ON THE SPECIFIC
                                  * TARGET PLATFORM. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ResetValueAndZeroString = ResetValue |
#if !MONO && NATIVE && WINDOWS
                                  ZeroString,
#else
                                  None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeLibrary = IncludeCommands | IncludeProcedures |
                      IncludeVariables | IncludeLibrary |
                      ResetValueAndZeroString,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeAll = SafeLibrary | IncludeFunctions,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardLibrary = IncludeCommands,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardAll = StandardLibrary | IncludeFunctions |
                      IncludeOperators,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeShell = SafeAll,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardShell = StandardAll,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeEvaluate = SafeAll & ~(IncludeVariables | IncludeLibrary | ResetValueAndZeroString),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("93f26486-9fd2-4705-9da6-172afd1b77e5")]
    public enum FunctionFlags
    {
        None = 0x0,
        Invalid = 0x1,              /* Invalid, do not use. */
        Core = 0x2,                 /* This function is part of the core
                                     * function set. */
        Special = 0x4,              /* This function requires special handling
                                     * by the expression engine. */
        NoPopulate = 0x8,           /* The function will not be returned by the
                                     * plugin manager. */
        ReadOnly = 0x10,            /* The function may not be modified nor
                                     * removed. */
        NoToken = 0x20,             /* Skip handling of the command token via
                                     * the associated plugin. */
        Breakpoint = 0x40,          /* Break into debugger before execution. */
        Disabled = 0x80,            /* The function may not be executed. */
        Hidden = 0x100,             /* By default, the function will not be
                                     * visible in the results of [info functions]. */
        Safe = 0x200,               /* Function is "safe" to execute for
                                     * partially trusted and/or untrusted
                                     * scripts. */
        Unsafe = 0x400,             /* Function is NOT "safe" to execute for
                                     * partially trusted and/or untrusted
                                     * scripts. */
        Standard = 0x800,           /* The function is largely (or completely)
                                     * compatible with an identically named
                                     * function from Tcl/Tk 8.4, 8.5, and/or
                                     * 8.6. */
        NonStandard = 0x1000,       /* The function is not present in Tcl/Tk
                                     * 8.4, 8.5, and/or 8.6 -OR- it is
                                     * completely incompatible with an
                                     * identically named function in Tcl/Tk
                                     * 8.4, 8.5, and/or 8.6. */
        Obsolete = 0x2000,          /* The function has been superseded and
                                     * should not be used for new development. */
        NoTclMathFunction = 0x4000, /* Disable adding the command to the
                                     * "tcl::mathfunc" namespace. */

        //
        // NOTE: This flag mask is only used for testing the core library.
        //
        ForTestUse = Obsolete | NoTclMathFunction,

        //
        // NOTE: These flags are added to a function when the parent interpreter
        //       is made "safe" or "standard".
        //
        DisabledAndHidden = Disabled | Hidden
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a227c466-4785-46ad-9000-9e3e8deff07f")]
    public enum OutputStyle
    {
        Invalid = -1,
        None = 0x0,
        ReversedText = 0x1,
        ReversedBorder = 0x2,
        Formatted = 0x4,
        Boxed = 0x8,
        Normal = 0x10,
        Debug = 0x20,
        Error = 0x40,

        FlagMask = ReversedText | ReversedBorder,
        BaseMask = Formatted | Boxed,
        TypeMask = Normal | Debug | Error,

        All = FlagMask | BaseMask | TypeMask,
        Default = Boxed | Normal
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e15a27e3-8f64-462c-95f5-21690ab6fcd1")]
    public enum PathTranslationType
    {
        None = 0x0,
        Unix = 0x1,
        Windows = 0x2,
        Native = 0x4,
        NonNative = 0x8,
        Default = Unix
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d26b161b-178c-44b1-a68c-08a4e6a4de94")]
    public enum PathFlags
    {
        None = 0x0,                  /* No special handling.  This is also the
                                      * normal default. */
        Invalid = 0x1,               /* Invalid, do not use. */
        NoShared = 0x2,              /* Skip checking for the shared path being
                                      * overridden.  When this flag is not set,
                                      * the shared path is only used when it
                                      * has been manually set (i.e. it defaults
                                      * to null). */
        Root = 0x4,                  /* Use the root path, which is the "lib"
                                      * directory right beneath the currently
                                      * effective base path for the Eagle core
                                      * library. */
        NoBinary = 0x8,              /* Disable falling back on the path that
                                      * contains the binary associated with
                                      * the current process.  Normally, this
                                      * flag being set will cause the Eagle
                                      * core library assembly path to be used
                                      * instead. */
        Local = 0x10,                /* Used primary for Unix.  Prefers to use
                                      * the "/usr/local/lib" path prefix over
                                      * the "/usr/lib" path prefix. */
        Verbatim = 0x20,             /* Skip modifying the path, e.g. to remove
                                      * path segments that are "well-known" at
                                      * build-time.  Examples include the path
                                      * segments like "netstandard2.0",
                                      * "BuildTasks", etc. */
        NoFullPath = 0x40,           /* Skip attempting to fully normalize the
                                      * returned path.  This may allow relative
                                      * paths to be returned. */
        LibExists = 0x80,            /* The non-package specific portion of the
                                      * path returned must actually exist. */
        Absolute = 0x100,            /* Do not use the package name and version
                                      * to construct the final path unless the
                                      * parent portion of the path was valid. */
        NoInitialize = 0x200,        /* Do not initialize the shared binary
                                      * path before querying it.  This flag is
                                      * for internal use only. */
        ForceInitialize = 0x400,     /* Forcibly initialize the shared binary
                                      * path before querying it.  This flag is
                                      * for internal use only. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        RawSerialNumber = 0x800,     /* Skip hashing the elements of the volume
                                      * serial number for a given path. */
        StableSerialNumber = 0x1000, /* Skip including any volatile portions of
                                      * the volume serial number for a given
                                      * path. */
        NoSerialNumber = 0x2000,     /* Skip including any of the volume serial
                                      * number information for a given path. */
        PerUser = 0x4000,            /* Attempt to include per-user information
                                      * when extracting metadata from candidate
                                      * paths. */
        NoRegistry = 0x8000,         /* Avoid using the Windows registry. */
        RegistryOnly = 0x10000,      /* Only use the Windows registry. */
        SerialNumberOnly = 0x20000,  /* Only use the volume serial number. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForDefault = 0x40000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region WARNING: FOR HARPY USE ONLY
        ForHarpy = 0x80000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        RootOnly = NoShared | Root | ForHarpy,
        AssemblyOnly = NoShared | NoBinary | ForHarpy,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used by Harpy to calculate the unique identifier
        //       that it will then consider as the current "machine".
        //
        MachineForHarpy = PerUser |
#if DEBUG
                          StableSerialNumber |
#endif
                          ForHarpy,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used by Harpy when provisioning "machine" locked
        //       license certificates.
        //
        VerifyForHarpy = MachineForHarpy | NoSerialNumber | ForHarpy,
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b0e68899-9337-4d3b-8691-a2f5588cc987")]
    public enum PathComparisonType
    {
        None = 0x0,
        String = 0x1,       /* Treat as plain strings with String.Compare. */
        DeepestFirst = 0x2, /* File names with more segments sort first.  Ties
                             * are broken with String.Compare. */
        DeepestLast = 0x4,  /* File names with more segments sort last.  Ties
                             * are broken with String.Compare. */
        BuiltIn = 0x8,      /* Use the default .NET Framework sorting. */

        Default = String
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("799013e1-6b95-4586-bde4-4bbfadbc986d")]
    public enum ChannelType
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Input = 0x2,
        Output = 0x4,
        Error = 0x8,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowExist = 0x10,
        AllowProxy = 0x20,

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeginContext = 0x40,
        EndContext = 0x80,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ErrorOnExist = 0x100,
        ErrorOnNotExist = 0x200,
        ErrorOnNull = 0x400,
        ErrorOnProxy = 0x800,

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseCurrent = 0x1000,
        UseHost = 0x2000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SkipGetStream = 0x4000,
        CloseOnEndContext = 0x8000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        Console = 0x10000,

        FlagMask = AllowExist | AllowProxy | BeginContext |
                   EndContext | ErrorOnExist | ErrorOnNotExist |
                   ErrorOnNull | ErrorOnProxy | UseCurrent |
                   UseHost | SkipGetStream | CloseOnEndContext |
                   Console,
#else
        FlagMask = AllowExist | AllowProxy | BeginContext |
                   EndContext | ErrorOnExist | ErrorOnNotExist |
                   ErrorOnNull | ErrorOnProxy | UseCurrent |
                   UseHost | SkipGetStream | CloseOnEndContext,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardChannels = Input | Output | Error
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("de027927-2975-4c1d-98bb-53036202db09")]
    public enum TraceFlags
    {
        None = 0x0,     /* unspecified trace type, use default handling. */
        Invalid = 0x1,  /* invalid, do not use. */
        ReadOnly = 0x2, /* The trace cannot be removed. */
        Disabled = 0x4, /* The trace is disabled and will not be invoked. */
        NoToken = 0x8,  /* Skip handling of the trace token via the associated plugin. */
        Global = 0x10   /* The trace is interpreter-wide. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("734721a6-0f3d-4379-b80c-394616285bf5")]
    public enum PolicyType
    {
        None = 0x0,
        Invalid = 0x1,
        Unknown = 0x2,
        Script = 0x4,
        File = 0x8,
        Stream = 0x10,
        License = 0x20,
        KeyPair = 0x40,
        Trace = 0x80,
        Other = 0x100
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1eafc4d0-b42a-401c-a6a9-6c08940df3bf")]
    public enum PolicyFlags
    {
        None = 0x0,               /* unspecified policy type, use default handling. */
        Invalid = 0x1,            /* invalid, do not use. */
        ReadOnly = 0x2,           /* The policy cannot be removed. */
        Disabled = 0x4,           /* The policy is disabled and will not be invoked. */
        NoToken = 0x8,            /* Skip handling of the policy token via the associated plugin. */
        ForEngine = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeforePlugin = 0x20,      /* being invoked prior to loading a plugin assembly. */
        BeforeScript = 0x40,      /* being invoked prior to returning an IScript object
                                   * created from an external source. */
        BeforeFile = 0x80,        /* being invoked prior to reading a script file. */
        BeforeStream = 0x100,     /* being invoked prior to reading a script stream. */
        BeforeCommand = 0x200,    /* being invoked prior to the execution of a command. */
        BeforeSubCommand = 0x400, /* being invoked prior to the execution of a sub-command. */
        BeforeProcedure = 0x800,  /* being invoked prior to the execution of a procedure. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AfterFile = 0x1000,       /* being invoked after reading a script file. */
        AfterStream = 0x2000,     /* being invoked after reading a script stream. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeforeNoHash = 0x4000,    /* skip any hashing of the file and/or stream content. */
        AfterNoHash = 0x8000,     /* skip any hashing of the file and/or stream content. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Callback = 0x10000,       /* invokes a user callback. */
        Directory = 0x20000,      /* checks a directory against a list. */
        Script = 0x40000,         /* evaluates a user script. */
        SubCommand = 0x80000,     /* checks a sub-command against a list. */
        Type = 0x100000,          /* checks a type against a list. */
        Uri = 0x200000,           /* checks a uri against a list. */
        SplitList = 0x400000,     /* policy script is a list. */
        Arguments = 0x800000,     /* append command arguments to policy script prior to
                                   * evaluation. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeforeAny = BeforePlugin | BeforeScript | BeforeFile | BeforeStream |
                    BeforeCommand | BeforeSubCommand | BeforeProcedure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AfterAny = AfterFile | AfterStream,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = BeforeNoHash | AfterNoHash,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EngineBeforePlugin = ForEngine | BeforePlugin,
        EngineBeforeScript = ForEngine | BeforeScript,
        EngineBeforeFile = ForEngine | BeforeFile | BeforeNoHash,
        EngineBeforeStream = ForEngine | BeforeStream | BeforeNoHash,
        EngineBeforeCommand = ForEngine | BeforeCommand,
        EngineBeforeSubCommand = ForEngine | BeforeSubCommand,
        EngineBeforeProcedure = ForEngine | BeforeProcedure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EngineAfterFile = ForEngine | AfterFile,
        EngineAfterStream = ForEngine | AfterStream
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("4d68dbeb-d698-4b67-94dd-8e497b3e0e11")]
    public enum PolicyDecision
    {
        None = 0,
        Undecided = 1,
        Denied = 2,
        Approved = 3,
        Default = Denied
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("333bce33-eb11-4c7b-b7b9-e48bd6a1ac38")]
    public enum PathType : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Unknown = 0x2,

        ForceWindows = 0x10,
        ForceUnix = 0x20,

        AllowDrive = 0x40, /* Win32 */
        DisallowDrive = 0x80, /* Win32 */

        AllowExtended = 0x100, /* Win32 */
        DisallowExtended = 0x200, /* Win32 */

        Normal = 0x1000,
        Device = 0x2000,
        Extended = 0x4000, /* Win32 */
        Unc = 0x8000, /* Win32 */
        Uri = 0x10000,

        Path = 0x100000,
        Directory = 0x200000,
        File = 0x400000,
        Component = 0x800000, /* single part only */

        Relative = 0x1000000,
        VolumeRelative = 0x2000000, /* Win32 */
        Absolute = 0x4000000,

        Verify = 0x10000000,

        ForValidName = 0x20000000, /* for [file validname] sub-command */
        ForCleanup = 0x40000000,   /* for [file cleanup] sub-command */

        Cleanup = File | ForCleanup,
        Default = ForValidName
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e0b341df-fc31-40c9-954a-d84cb8a09217")]
    public enum UriFlags
    {
        None = 0x0,       /* No special handling. */
        Invalid = 0x1,    /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowFile = 0x2,  /* Allow URIs with the FILE scheme. */
        AllowHttp = 0x4,  /* Allow URIs with the HTTP scheme. */
        AllowHttps = 0x8, /* Allow URIs with the HTTPS scheme. */
        AllowFtp = 0x10,  /* Allow URIs with the FTP scheme. */
        NoHost = 0x20,    /* Do not query the host property. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        WasFile = 0x100,
        WasHttp = 0x200,
        WasHttps = 0x400,
        WasFtp = 0x800,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved1 = 0x1000,
        Reserved2 = 0x2000,
        Reserved3 = 0x4000,
        Reserved4 = 0x8000,
        Reserved5 = 0x10000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags only work with TryCombineUris.
        //
        UseFormat = 0x20000,       /* Use specified UriFormat. */
        Normalize = 0x40000,       /* Convert path backslash to slash. */
        NoSeparators = 0x80000,    /* Skip using path separators when
                                    * combining path portions of the
                                    * URIs. */
        OneSeparator = 0x100000,   /* Trim Unix path separators when
                                    * combining path portions of the
                                    * URIs. */
        BothSeparators = 0x200000, /* Trim both path separators when
                                    * combining path portions of the
                                    * URIs. */
        BasePath = 0x400000,       /* Only consider the path portion
                                    * of the base URI, discarding the
                                    * path portion of the relative
                                    * URI. */
        RelativePath = 0x800000,   /* Only consider the path portion
                                    * of the relative URI, discarding
                                    * the path portion of the base
                                    * URI. */
        PreferBaseUri = 0x1000000, /* Prefer fragment from baseUri. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowMask = AllowFile | AllowHttp | AllowHttps | AllowFtp,
        WasMask = WasFile | WasHttp | WasHttps | WasFtp,

        ///////////////////////////////////////////////////////////////////////////////////////////

        LocalOnlyMask = Reserved1 | AllowFile,
        WasLocalMask = Reserved1 | WasFile,

        ///////////////////////////////////////////////////////////////////////////////////////////

        WebOnlyMask = Reserved2 | AllowHttp | AllowHttps,
        WasWebMask = Reserved2 | WasHttp | WasHttps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InsecureOnlyMask = Reserved3 | AllowHttp | AllowFtp,
        WasInsecureMask = Reserved3 | WasHttp | WasFtp,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SecureOnlyMask = Reserved4 | AllowHttps,
        WasSecureMask = Reserved4 | WasHttps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Reserved5 | WebOnlyMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e68f81aa-f0b1-414a-8bab-1bf5626d0727")]
    public enum ExecutionPolicy : ulong
    {
        None = 0x0,                              /* Skip policy check, effectively
                                                  * the same as allowing all. */
        Invalid = 0x1,                           /* Invalid, do not use. */
        Undefined = 0x2,                         /* The policy has not been explicilty
                                                  * set and is not defined.  This value
                                                  * should not be returned by a public
                                                  * API.  Nor should it be included in
                                                  * a value that is explicitly set. */
        /* Reserved2 = 0x4, */                   /* Reserved for future use, do not
                                                  * use. */
        AllowNone = 0x8,                         /* No files are allowed to be read
                                                  * and/or evaluated. */
        AllowSignedOnly = 0x10,                  /* Only files that are signed and
                                                  * trusted are allowed to be read
                                                  * and/or evaluated. */
        AllowAny = 0x20,                         /* All files are allowed to be read
                                                  * and/or evaluated. */
        SkipExists = 0x40,                       /* Bypass all file-exists checking
                                                  * (i.e. for non-URIs). */
        ValidateXml = 0x80,                      /* Validate the XML against the XSD
                                                  * schema. */
        MatchSubject = 0x100,                    /* Enforce the certificate subject
                                                  * matching the X509 subject on the
                                                  * associated assembly. */
        MatchSubjectPrefix = 0x200,              /* The X509 subject and simple
                                                  * (subject) name should be matched
                                                  * using prefix semantics as well. */
        MatchSubjectSimpleName = 0x400,          /* The X509 simple (subject) name
                                                  * should also be checked. */
        CheckExpiry = 0x800,                     /* Enforce certificate expiration
                                                  * dates. */
        CheckEntityType = 0x1000,                /* Enforce certificate entity types. */
        VerifyString = 0x2000,                   /* Treat the content to be verified as
                                                  * a string. */
        VerifyFile = 0x4000,                     /* Treat the content to be verified as
                                                  * a file. */
        CheckPublicKeyToken = 0x8000,            /* Make sure the public key tokens
                                                  * match. */
        AllowAssemblyPublicKey = 0x10000,        /* The public keys used to sign the
                                                  * assembly may be used. */
        AllowEmbeddedPublicKey = 0x20000,        /* Public keys embedded within the
                                                  * assembly as resources may be used.
                                                  */
        AllowRingPublicKey = 0x40000,            /* Public keys present on the trusted
                                                  * key ring(s) may be used. */
        AllowAnyPublicKey = 0x80000,             /* Any public key present in the
                                                  * assembly may be used; otherwise,
                                                  * only the named public key may be
                                                  * used. */
        TrustSignedOnly = 0x100000,              /* Files that are signed and trusted
                                                  * are evaluated with full permissions,
                                                  * even in a "safe" interpreter. */
        CheckDomains = 0x200000,                 /* Enforce domain restrictions. */
        CheckQuantity = 0x400000,                /* Enforce certificate quantities. */
        ProtectQuantity = 0x800000,              /* Protect certificate quantities. */
        PerMachine = 0x1000000,                  /* Protect data on a per-machine basis.
                                                  */
        AllowEmbedded = 0x2000000,               /* Permit the certificate to be
                                                  * embedded within the data. */
        SkipFile = 0x4000000,                    /* Do not check the native file system
                                                  * for certificate data. */
        SkipHost = 0x8000000,                    /* Do not check the interpreter host
                                                  * for certificate data. */
        SkipRenewedName = 0x10000000,            /* Do not check the file name based on
                                                  * the hash value of the contained data
                                                  * for its certificate data as renewed
                                                  * by the server. */
        SkipHashName = 0x20000000,               /* Do not check the file name based on
                                                  * the hash value of the contained data
                                                  * for its certificate data. */
        SkipPlainName = 0x40000000,              /* Do not check the file name based on
                                                  * the original file name for its
                                                  * certificate data. */
        SaveApprovedData = 0x80000000,           /* Keep track of the data associated
                                                  * with approved policy checks. */
        EnforceKeyGroup = 0x100000000,           /* Make sure that a key is only used
                                                  * in conjunction with its associated
                                                  * assemblies. */
        EnforceKeyUsage = 0x200000000,           /* Make sure that a key is only used
                                                  * in compliance with its declared key
                                                  * usage. */
        NoLoadKeyRings = 0x400000000,            /* Do not load key rings when running
                                                  * the policy checks. */
        IgnoreKeyRingError = 0x800000000,        /* Ignore all errors when loading key
                                                  * rings. */
        ExplicitOnly = 0x1000000000,             /* Do not consider any implicit policy
                                                  * data. */
        PreferEmbedded = 0x2000000000,           /* Prefer embedded policy data over
                                                  * external policy data. */
        SkipThisAssembly = 0x4000000000,         /* Disable special handling for the
                                                  * license certificate associated with
                                                  * the Harpy assembly itself. */
        SkipThisStream = 0x8000000000,           /* Disable use of the default stream
                                                  * when searching for available license
                                                  * certificates. */
        NoRenewKeyRings = 0x10000000000,         /* Disable loading trusted key rings
                                                  * returned by the certificate renewal
                                                  * server. */
        NoGetKeyPairs = 0x20000000000,           /* Skip fetching matching key pairs
                                                  * from the trusted key ring. */
        CheckRevocation = 0x40000000000,         /* Enforce checking of the revocation
                                                  * list(s). */
        CacheKeyRings = 0x80000000000,           /* Enable caching expensive resources
                                                  * when loading key rings. */
        IsolateKeyRings = 0x100000000000,        /* Enable application domain isolation
                                                  * when loading key rings. */
        EnableTracing = 0x200000000000,          /* Enable diagnostic messages when
                                                  * handling policy decisions. */
        AppendTracing = 0x400000000000,          /* Enable appending to the tracing log
                                                  * file, if it already exists. */
        SharedTracing = 0x800000000000,          /* Enable the trace file to be shared
                                                  * by multiple interpreters. */
        ForceTracing = 0x1000000000000,          /* Treat all diagnostic information as
                                                  * high priority. */
        VerboseTracing = 0x2000000000000,        /* Enable extra diagnostic information
                                                  * when loading key rings, checking
                                                  * policies, etc. */
        AutoTraceFile = 0x4000000000000,         /* Enable setting up a trace file name
                                                  * automatically. */
        FullTracing = 0x8000000000000,           /* Do not shorten (e.g. via ellipsis,
                                                  * etc) any trace information. */
        ResetTracing = 0x10000000000000,         /* Attempt to forcibly reset the tracing
                                                  * subsystem prior to emitting diagnostic
                                                  * messages. */
        AllowRemoteUri = 0x20000000000000,       /* Allow certificates to be loaded from
                                                  * remote URIs. */
        LooksLikeXml = 0x40000000000000,         /* Prior to selecting any certificate,
                                                  * sanity check that it looks like an
                                                  * XML document. */
        PreValidateXml = 0x80000000000000,       /* Prior to selecting any certificate,
                                                  * attempt validate it against the XSD
                                                  * schema.  This only applies when more
                                                  * that one certificate is available to
                                                  * choose from. */
        PrimaryKeyRingOnly = 0x100000000000000,  /* Only load the primary key script key
                                                  * ring when bootstrapping. */
        SpecificKeyRingOnly = 0x200000000000000, /* Skip loading non-specific key rings
                                                  * when bootstrapping. */
        AutoAcquire = 0x400000000000000,         /* Allow certificates to be acquired
                                                  * from any available source, without
                                                  * any interaction. */
        CacheAcquire = 0x800000000000000,        /* Enable caching expensive resources
                                                  * when acquiring certificates. */
        TraceKeyRings = 0x1000000000000000,      /* Enable policy tracing for the key
                                                  * ring loader. */
        UseApprovedData = 0x2000000000000000,    /* Double-check that a key pair was
                                                  * approved prior to using it. */
        MaybeNoFileSearch = 0x4000000000000000,  /* Do not search for certificate files,
                                                  * only consider the one specified, if
                                                  * any. */
        DisableCreation = 0x8000000000000000,    /* Persistently forbid any [further]
                                                  * interpreters from being created in
                                                  * the current process. */

        BasePolicyMask = AllowNone | AllowSignedOnly | AllowAny /* Mask for the "base"
                                                                 * policy values. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8f608be1-003c-496b-95c5-9c827a815764")]
    public enum HostCreateFlags : ulong
    {
        None = 0x0,                   /* No special creation behavior. */
        Invalid = 0x1,                /* Invalid, do not use. */
        Disable = 0x2,                /* Do not create an interpreter host? */
        Clone = 0x4,                  /* Use a Clone(this) of the provided
                                       * host, NOT the host itself. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoConsole = 0x8,              /* Do not create a console-based host? */
        NoDiagnostic = 0x10,          /* Do not create a debugging host? */
        NoNull = 0x20,                /* Do not create a do-nothing host? */
        NoFake = 0x40,                /* Do not create an exception throwing host? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoVerbose = 0x80,             /* Disable use of verbose mode by default? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseWrapper = 0x100,           /* Put a wrapper around the host used. */
        OwnWrapper = 0x200,           /* Let the wrapper own the host used. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoDispose = 0x400,            /* Do not call Dispose on the host? */
        UseAttach = 0x800,            /* Attempt to attach to existing console,
                                       * if applicable. */
        NoColor = 0x1000,             /* Limit colors to grayscale, if
                                       * applicable? */
        NoTitle = 0x2000,             /* Do not change the console title, if
                                       * applicable? */
        NoIcon = 0x4000,              /* Do not change the console icon, if
                                       * applicable? */
        NoProfile = 0x8000,           /* Do not load the host profile? */
        NoCancel = 0x10000,           /* Do not setup or teardown the script
                                       * cancellation user interface if
                                       * applicable (i.e. via key press)? */
        Echo = 0x20000,               /* Enable echo for interactive input,
                                       * if applicable. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        CloseConsole = 0x40000,       /* First, close the console in the current
                                       * process. */
        OpenConsole = 0x80000,        /* Next, open [or attach?] a console in the
                                       * current process. */
        ForceConsole = 0x100000,      /* Finally, open [or attach?] a console even
                                       * if it appears to be open already. */
        AttachConsole = 0x200000,     /* Allow an existing console in the parent
                                       * process to be attached. */
        NoCloseConsole = 0x400000,    /* Attempt to make sure that the native
                                       * console window cannot be closed. */
        FixConsole = 0x800000,        /* Perform various fixes to the console in
                                       * order to make it integrate better with
                                       * Eagle (e.g. resize the input buffer). */
        HookConsole = 0x1000000,      /* Greedily open the native console handles
                                       * when a console-based interpreter host is
                                       * created. */
        PushConsole = 0x2000000,      /* Before creating the first console-based
                                       * host for an interpreter, push its screen
                                       * buffer onto the stack, thus saving it
                                       * for later. */
        NoNativeConsole = 0x4000000,  /* Avoid calling into the NativeConsole
                                       * class during interpreter creation. */
        QuietConsole = 0x8000000,     /* Do not complain if NativeConsole methods
                                       * fail during interpreter creation. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Debug = 0x10000000,           /* Enable debug-mode for the host (NOT USED). */
        ReplaceNewLines = 0x20000000, /* When displaying a Result object, replace
                                       * all line-ending characters with visible
                                       * placeholder characters. */
        Ellipsis = 0x40000000,        /* When displaying a Result object, truncate
                                       * it as the default result formatting limit,
                                       * and append the string "..." to it. */
        Exceptions = 0x80000000,      /* When displaying a Result object, allow
                                       * non-standard return codes -AND- determine
                                       * if they represent success or error. */
        Display = 0x100000000,        /* When displaying a Result object, make sure
                                       * that null and empty strings are represented
                                       * using a string suitable for display instead
                                       * of an empty string. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CanExit = 0x200000000,        /* Non-zero if the host should allow for its
                                       * associated interpreter to be flagged as
                                       * "exited". */
        CanForceExit = 0x400000000,   /* Non-zero if the host should allow for its
                                       * associated interpreter to be forcibly
                                       * flagged as "exited". */
        Exiting = 0x800000000,        /* Non-zero when the associated interpreter
                                       * is exiting. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseLibrary = 0x1000000000,    /* Prefer host scripts over those on the
                                       * file system (i.e. when embedded script
                                       * library is available).  This flag can be
                                       * useful if (core library) scripts on the
                                       * file system have been modified and may
                                       * contain errors. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        Debugger = 0x2000000000,      /* Indicates that the script debugger may
                                       * make use of this host. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        MustCreate = 0x4000000000,    /* The host must be created.  If the callback
                                       * fails, use fallback semantics. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        ResetIsolated = 0x8000000000, /* The isolated host mube be reset after the
                                       * host is set. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ResourceManager = 0x10000000000,            /* Initialize various resource
                                                     * managers. */
        ApplicationResourceManager = 0x20000000000, /* Initialize application resource
                                                     * managers. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForShellUse = 0x40000000000,
        ForCoreShellUse = 0x80000000000,

#if NATIVE && TCL && NATIVE_PACKAGE
        ForNativeUse = 0x100000000000,
#endif

        ForNestedUse = 0x200000000000,

#if NATIVE && TCL
        ForTclManagerUse = 0x400000000000,
#endif

        ForSettingsUse = 0x800000000000,
        ForSafeSettingsUse = 0x1000000000000,

        ForTestUse = 0x2000000000000,

        ForEmbeddedUse = 0x4000000000000,
        ForSafeEmbeddedUse = 0x8000000000000,

        ForSingleUse = 0x10000000000000,
        ForSafeSingleUse = 0x20000000000000,

        ForScriptThreadUse = 0x40000000000000,
        ForPluginUse = 0x80000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CommonUse = ResourceManager, /* Included in all flag sets. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        TypeMask = NoConsole | NoDiagnostic |
                   NoNull | NoFake,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        ConsoleUse = FixConsole | HookConsole |
                     PushConsole,

        ConsoleMask = CloseConsole | OpenConsole |
                      ForceConsole | AttachConsole |
                      NoCloseConsole | FixConsole |
                      HookConsole | PushConsole |
                      NoNativeConsole | QuietConsole,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        DebuggerUse = Debugger,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ShellUse = CommonUse |
            ApplicationResourceManager |
#if DEBUGGER
            DebuggerUse |
#endif
            ForShellUse,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CoreShellUse = (ShellUse & ~ForShellUse) | ForCoreShellUse,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
        NativeUse = CommonUse | EmbeddedUse |
#if DEBUGGER
            DebuggerUse |
#endif
            ForNativeUse,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NestedUse = CommonUse | ForNestedUse,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclManagerUse = CommonUse | ForTclManagerUse,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        SettingsUse = CommonUse | ForSettingsUse,
        SafeSettingsUse = (SettingsUse & ~ForSettingsUse) | ForSafeSettingsUse,

        TestUse = CommonUse | ForTestUse,

        EmbeddedUse = CommonUse | NoTitle | NoIcon | NoCancel | ForEmbeddedUse,
        SafeEmbeddedUse = (EmbeddedUse & ~ForEmbeddedUse) | ForSafeEmbeddedUse,

        SingleUse = (EmbeddedUse & ~ForEmbeddedUse) | ForSingleUse,
        SafeSingleUse = (SingleUse & ~ForSingleUse) | ForSafeSingleUse,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FastEmbeddedUse = EmbeddedUse | NoProfile,
        FastSingleUse = SingleUse | NoProfile,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ScriptThreadUse = CommonUse |
#if DEBUGGER
            DebuggerUse |
#endif
            ForScriptThreadUse,

        ///////////////////////////////////////////////////////////////////////////////////////////

        PluginUse = (EmbeddedUse & ~ForEmbeddedUse) | NoProfile | ForPluginUse,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = ShellUse
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("94812861-d6dd-46fb-b9c0-9ffcce6f8a63")]
    public enum DisableFlags
    {
        None = 0x0,
        Invalid = 0x1,

        Override = 0x100,
        Persistent = 0x200,
        Quiet = 0x400,

        ForDefault = 0x1000,
        ForSdk = 0x2000,
        ForDemand = 0x4000,

        Demand = ForDemand,
        Sdk = Override | ForSdk,

        Default = ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("41dae319-8562-4466-bb7a-3f4ff0f5a620")]
    public enum CreateFlags : ulong
    {
        #region Standard Placeholder Values
        None = 0x0,                /* No special creation behavior. */
        Invalid = 0x1,             /* Invalid, do not use. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Flags Control Values
        GetFlags = 0x2,            /* Modify the specified creation flags and
                                    * host creation flags using their associated
                                    * static methods. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Managed Debugging Control
        MeasureTime = 0x4,         /* Measure elapsed time for the Create()
                                    * method. */
        BreakOnCreate = 0x8,       /* Break into managed debugger immediately
                                    * on Create()? */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region General Mode Control
        Debug = 0x10,              /* Interpreter should be created in "debug"
                                    * mode? */
        Verbose = 0x20,            /* Verbose mode enabled.  This will result
                                    * in more diagnostic output, possibly to
                                    * the System.Console. */
        Interactive = 0x40,        /* Force interactive mode upon creation?
                                    * The [debug break] sub-command will not
                                    * break into the interactive loop without
                                    * this flag being set. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Auto-Path Control
        StrictAutoPath = 0x80,     /* Candidate directories for the "auto_path"
                                    * must actually exist? */
        ShowAutoPath = 0x100,      /* Show all auto-path search information? */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Pre-Setup Control
        UseNamespaces = 0x200,     /* Enable Tcl 8.4+ compatible namespace
                                    * support for created interpreter. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Post-Setup Control
        SetArguments = 0x400,      /* Make sure the "argc" and "argv" global
                                    * variables are set even if specified
                                    * managed argument array is null. */
        Startup = 0x800,           /* Process startup options from environment,
                                    * etc. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Script Library Control
        Initialize = 0x1000,       /* Initialize core script library during
                                    * creation?  Without this flag, all core
                                    * script library procedures will be
                                    * unavailable. */
        IgnoreOnError = 0x2000,    /* Just ignore initialization errors?  This
                                    * should rarely, if ever, be necessary. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Exception Control
        ThrowOnDisposed = 0x4000,  /* Throw exceptions when disposed objects
                                    * are accessed?  Highly recommended. */
        ThrowOnError = 0x8000,     /* Throw exception on initialization fail?
                                    * Recommended. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Core Command Set Control
        //
        // NOTE: *WARNING* For embedders, more control over interpreter
        //       creation.  These flags should not be combined with the
        //       "Initialize" flag because using any of these flags will
        //       almost certainly cause script library initialization to
        //       fail.
        //
        Safe = 0x10000,                 /* Include only "safe" commands and
                                         * remove all "unsafe" commands from
                                         * the created interpreter. */
        HideUnsafe = 0x20000,           /* Hide all "unsafe" commands instead
                                         * of removing them? */
        Standard = 0x40000,             /* Include only commands that are part
                                         * of the "Tcl Standard" and remove
                                         * those that are not. */
        HideNonStandard = 0x80000,      /* Hide commands that are not part of
                                         * the "Tcl Standard" instead of
                                         * removing them? */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Positive Compile-Time Feature Control (Enable Non-Default)
#if DEBUGGER
        Debugger = 0x100000,            /* Create script debugger? */
        DebuggerInterpreter = 0x200000, /* Also create an isolated debugger
                                         * interpreter? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclReadOnly = 0x400000,         /* Initially put native Tcl integration
                                         * subsystem into read-only mode. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        TclWrapper = 0x800000,          /* Enable Tcl wrapper mode.  All Tcl
                                         * interpreters will be assumed to have
                                         * been created on the main thread.
                                         * This is designed for a very narrow
                                         * use case and should generally not be
                                         * used.  If you need it, you will know
                                         * for certain that you need it. */
#endif
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        IsolatePlugins = 0x1000000,     /* Initially, force all plugins to be
                                         * loaded into isolated application
                                         * domains. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ProbePlugins = 0x2000000,       /* Attempt to probe possible plugin
                                         * assemblies to discover packages
                                         * they may contain, etc? */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Negative Optional Feature Control (Disable Default)
        #region Compile-Time Feature Control (May Be Unavailable)
#if NATIVE && NATIVE_UTILITY
        NoNativeUtility = 0x4000000,          /* Do not load the native utility
                                               * library?  If this creation flag
                                               * is required (e.g. to prevent a
                                               * crash on Mono), it may be wise
                                               * to disable the native utility
                                               * via the associated environment
                                               * variable ("NoNativeUtility")
                                               * instead. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        NoObjectPlugin = 0x8000000,           /* Skip adding the static object
                                               * notify plugin (i.e. no object
                                               * reference counting)?  Use of
                                               * this flag is NOT recommdned. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if TEST_PLUGIN || DEBUG
        NoTestPlugin = 0x10000000,            /* Skip adding the static test
                                               * plugin? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_ARGUMENTS
        NoMonitorPlugin = 0x20000000,         /* Skip adding the static monitor
                                               * plugin? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        NoTclTransfer = 0x40000000,           /* Skip transferring any "dead"
                                               * native Tcl resources to the
                                               * newly created interpreter. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        NoPopulateOsExtra = 0x80000000,       /* Skip asynchronously (fully)
                                               * populating the "osExtra" element
                                               * of the "tcl_platform" array? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        NoGlobalNotify = 0x100000000,         /* Initially, disable most "global"
                                               * notifications. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if APPDOMAINS && ISOLATED_INTERPRETERS
        NoUseEntryAssembly = 0x200000000,     /* Disable resetting the value of the
                                               * entry assembly when creating a new
                                               * AppDomain. */
        OptionalEntryAssembly = 0x400000000,  /* Ignore exceptions when forcibly
                                               * refreshing the entry assembly in
                                               * created AppDomains. */
        VerifyCoreAssembly = 0x800000000,     /* Make sure the AppDomain base
                                               * directory contains the core
                                               * library assembly? */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoLibrary = 0x1000000000,             /* Skip core script library
                                               * initialization. */
        NoShellLibrary = 0x2000000000,        /* Skip shell script library
                                               * initialization. */
        NoChannels = 0x4000000000,            /* Skip creating the standard
                                               * channels? */
        NoPlugins = 0x8000000000,             /* Skip adding all standard
                                               * plugins (i.e. no commands,
                                               * no object support)?  This
                                               * flag is for DSL use and/or
                                               * very advanced users only. */
        NoCorePlugin = 0x10000000000,         /* Skip adding static core
                                               * plugin (i.e. no commands)?
                                               * This flag is for DSL use
                                               * and/or very advanced users
                                               * only. */
        NoCommands = 0x20000000000,           /* Skip adding commands.  This is
                                               * designed for applications that
                                               * add their own commands and do
                                               * not want ANY of the core command
                                               * set(s) in their interpreter(s).
                                               * It should be very rarely used. */
        NoVariables = 0x40000000000,          /* Skip adding the standard
                                               * variables? */
        NoPlatform = 0x80000000000,           /* Skip adding platform-related
                                               * variables? */
        NoObjectIds = 0x100000000000,         /* Skip adding the "objectIds"
                                               * element in the "eagle_platform"
                                               * array (can be very large)? */
        NoHome = 0x200000000000,              /* Skip adding "env(HOME)" variable
                                               * (if needed)? */
        NoObjects = 0x400000000000,           /* Skip adding standard objects? */
        NoOperators = 0x800000000000,         /* Skip adding standard expression
                                               * operators?  This flag is for DSL
                                               * use and/or very advanced users
                                               * only. */
        NoFunctions = 0x1000000000000,        /* Skip adding standard math
                                               * functions? */
        NoRandom = 0x2000000000000,           /* Skip creating random number
                                               * generator(s). */
        NoCorePolicies = 0x4000000000000,     /* Skip adding built-in policies.
                                               * This flag severely limits what
                                               * it is possible to do within a
                                               * "safe" interpreter (i.e. no
                                               * sub-command will be allowed to
                                               * execute, even those that are
                                               * "safe", if they belong to a core
                                               * ensemble command marked as
                                               * "unsafe"). */
        NoCoreTraces = 0x8000000000000,       /* Skip adding built-in traces.
                                               * Please note that setting this
                                               * flag will completely disable
                                               * opaque object handle reference
                                               * count tracking, among other
                                               * things. */
        NoDefaultBinder = 0x10000000000000,   /* Skip using Type.DefaultBinder. */
        NoConfiguration = 0x20000000000000,   /* Skip copying static configuration
                                               * into newly created AppDomains. */
#if ISOLATED_PLUGINS
        NoPluginPreview = 0x40000000000000,   /* Automatic set the NoPreview flag
                                               * when initializing plugin flags
                                               * for the new interpreter. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        SecuritySdk = 0x80000000000000,       /* For use by the security SDK (i.e.
                                               * Harpy) only. */
        LicenseSdk = 0x100000000000000,       /* For use by the license SDK (i.e.
                                               * Harpy) only. */
        MinimumVariables = 0x200000000000000, /* Only setting variables that are
                                               * required for proper operation of
                                               * the core script library. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Special Flags
        IfNecessary = 0x2000000000000000,     /* WARNING: When this flag is used,
                                               * a new interpreter may not be
                                               * created; instead, the first one
                                               * available in the AppDomain will
                                               * be returned instead.  This flag
                                               * will take into account the "safe"
                                               * flag when deciding whether or not
                                               * to create a new interpreter. */
        NoDispose = 0x4000000000000000,       /* WARNING: Disable disposal of the
                                               * newly created interpreter until
                                               * the SetDisposalEnabled method is
                                               * used to enable it. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000,        /* Reserved value, do not use. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Useful Combinations
        //
        // NOTE: If one of these flags is set, some variables may be
        //       missing.
        //
        NoVariablesMask = NoVariables | MinimumVariables,
        NoPlatformMask = NoVariablesMask | NoPlatform,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If one of these flags is set, the interpreter was
        //       created for some kind of SDK-based usage, i.e. it
        //       is not fully general purpose.
        //
        SdkMask = SecuritySdk | LicenseSdk,
        SafeOrSdkMask = Safe | SdkMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are forbidden for use with created "child"
        //       interpreters.
        //
        NoChildUseMask = ThrowOnError | IfNecessary,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for "safe" mode.
        //
        SafeAndHideUnsafe = Safe | HideUnsafe,

        //
        // NOTE: Typical flags for "standard" mode.
        //
        StandardAndHideNonStandard = Standard | HideNonStandard,

        //
        // NOTE: For core library use only.
        //
        CoreCommandSetMask = SafeAndHideUnsafe | StandardAndHideNonStandard,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Common flags for embedding.
        //
        CommonUse = ThrowOnDisposed |
                    StrictAutoPath |
#if TEST_PLUGIN && !DEBUG
                    NoTestPlugin |
#endif
#if NOTIFY && NOTIFY_ARGUMENTS
                    NoMonitorPlugin |
#endif
#if USE_NAMESPACES
                    UseNamespaces |
#endif
#if !NET_STANDARD_20
                    NoPopulateOsExtra |
#endif
#if APPDOMAINS && ISOLATED_INTERPRETERS
                    OptionalEntryAssembly |
#endif
                    NoDefaultBinder |
                    None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Completely disable initialization (and loading) of
        //       the core script library and shell script library.
        //
        NoLibraryUse = NoLibrary | NoShellLibrary,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags severely limit how useful the built-in
        //       [object] command can be.
        //
        BareObjectUse = NoCoreTraces | NoObjects | NoObjectPlugin,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for "DSL-style" embedding while still
        //       retaining most functionality of the core command set.
        //
        LeanAndMeanUse = CommonUse |
#if NOTIFY || NOTIFY_OBJECT
                         NoGlobalNotify |
#endif
#if NATIVE && NATIVE_UTILITY
                         NoNativeUtility |
#endif
                         NoObjectIds |
                         NoHome |
#if NATIVE && TCL
                         NoTclTransfer |
#endif
                         NoConfiguration,

        SafeLeanAndMeanUse = LeanAndMeanUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for "DSL-style" embedded without (full)
        //       support for "safe" interpreters, the built-in [object]
        //       command, standard channels, the core / shell script
        //       libraries, and pre-set global variables.  Using this
        //       set of flags is recommended for experts only.
        //
        SuperLeanAndMeanUse = LeanAndMeanUse | BareObjectUse |
                              NoLibraryUse | MinimumVariables |
                              NoChannels | NoCorePolicies,

        SafeSuperLeanAndMeanUse = SuperLeanAndMeanUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the same as SuperLeanAndMeanUse (above) except
        //       that it also omits initialization of the random number
        //       generators.
        //
        UltraLeanAndMeanUse = SuperLeanAndMeanUse | NoRandom,
        SafeUltraLeanAndMeanUse = UltraLeanAndMeanUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by CreateInterpreterForSettings only.
        //
        StaticDataUse = (UltraLeanAndMeanUse & ~NoCorePolicies) |
#if ISOLATED_PLUGINS
                        NoPluginPreview,
#else
                        None,
#endif

        SafeStaticDataUse = StaticDataUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for "DSL-style" embedding.
        //
        BareUse = CommonUse | ThrowOnError | NoCorePlugin,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        //
        // NOTE: Flags used when creating the isolated script debugger
        //       [interpreter].
        //
        DebuggerUse = Debugger,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Flags forbidden from being used for isolated script debugger
        //       interpreters.
        //
        NonDebuggerUse = Debugger | ThrowOnError,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for shell.
        //
        ShellUse = CommonUse |
#if DEBUGGER
                   DebuggerUse |
#endif
                   ThrowOnError,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for the core library shell.
        //
        CoreShellUse = ShellUse & ~ThrowOnError,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
        //
        // NOTE: Standard flags for the interpreter used with the Eagle Package
        //       for Tcl.  Since we have no control over when, where, and which
        //       thread is used to shutdown Eagle, avoid throwing exceptions
        //       about objects already having been disposed.
        //
        NativeUse = (CommonUse & ~ThrowOnDisposed) |
#if DEBUGGER
                    DebuggerUse |
#endif
                    Initialize |
                    SetArguments,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for nested interps.
        //
        NestedUse = CommonUse |
#if NATIVE && TCL
                    NoTclTransfer |
#endif
                    Initialize,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        //
        // NOTE: For use with the ITclManager interface.
        //
        TclManagerUse = CommonUse | Initialize,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the ScriptOps.LoadApplicationSettings method only.
        //
        SettingsUse = CommonUse | Initialize,
        SafeSettingsUse = SettingsUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for isolated test interps.
        //
        TestUse = CommonUse | Initialize,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for use by extensions (to other systems)
        //       that require an embedded interpreter.
        //
        EmbeddedUse = CommonUse | Initialize | ThrowOnError,
        SafeEmbeddedUse = EmbeddedUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for "single-use" interpreters
        //       created by the engine.
        //
        SingleUse = EmbeddedUse & ~ThrowOnError,
        SafeSingleUse = SingleUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        FastSingleUse = LeanAndMeanUse | Initialize | NoPluginPreview,
        FastSafeSingleUse = SafeLeanAndMeanUse | Initialize | NoPluginPreview,
#else
        FastSingleUse = LeanAndMeanUse | Initialize,
        FastSafeSingleUse = SafeLeanAndMeanUse | Initialize,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for use by ScriptThread objects.
        //
        ScriptThreadUse = CommonUse |
#if DEBUGGER
                          DebuggerUse |
#endif
                          Initialize,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for all applications and plugins.
        //
        Default = ShellUse | Initialize
        #endregion
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d6d43ba2-93f9-438d-a837-f6a641479e90")]
    public enum SdkType
    {
        None = 0x0,
        Invalid = 0x1,

        Initialize = 0x1000,
        Security = 0x2000,
        License = 0x4000,

        AnySdkMask = Security | License,
        AnyMask = Initialize | AnySdkMask,

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("36e6651c-734e-48f9-9a44-aec7bfc6f000")]
    public enum CloneFlags
    {
        None = 0x0,
        Invalid = 0x1,

        WithFrames = 0x100,
        WithValues = 0x200,
        WithTraces = 0x400,
        WithLocks = 0x800,

        FireTraces = 0x10000,
        AllowSpecial = 0x20000,

        ScopeMask = WithValues | WithTraces | FireTraces,

        AllMask = WithFrames | WithValues | WithTraces |
                  WithLocks | FireTraces | AllowSpecial
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("78754dfb-f802-4db2-9782-aaa3e69562a7")]
    public enum InitializeFlags : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Initialization = 0x1000,
        Safe = 0x2000,
        Test = 0x4000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Embedding = 0x10000,
        Vendor = 0x20000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Security = 0x100000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Shell = 0x1000000,
        Startup1 = 0x2000000,
        Startup2 = 0x4000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SetAutoPath = 0x100000000,
        GlobalAutoPath = 0x200000000,
        NoTraceAutoPath = 0x400000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        LibraryPath = 0x800000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Direct = 0x2000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        Isolated = 0x4000000000,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Health = 0x8000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN
        MaybeEmbedding = None,
#else
        MaybeEmbedding = Embedding,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN
        MaybeVendor = None,
#else
        MaybeVendor = Vendor,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN
        MaybeSecurity = Security,
#else
        MaybeSecurity = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN && ISOLATED_PLUGINS
        MaybeIsolated = Isolated,
#else
        MaybeIsolated = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Startup = Startup1 | Startup2,
        ShellOrStartup = Shell | Startup,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Library = Initialization | Safe | Test | Embedding | Vendor | Startup,
        ShellLibrary = Shell | Startup,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Minimal = Initialization | MaybeSecurity | MaybeIsolated,
        AutoPath = SetAutoPath | GlobalAutoPath,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Minimal | MaybeEmbedding | MaybeVendor |
                  Shell | Startup | AutoPath | LibraryPath
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("33e3eecb-1fe4-4d05-9642-fb5e187a5219")]
    public enum RuntimeOptionOperation
    {
        None = 0x0,
        Invalid = 0x1,
        Has = 0x2,
        Get = 0x4,
        Clear = 0x8,
        Add = 0x10,
        Remove = 0x20,
        Set = 0x40
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6271c3cb-0599-4c92-9d3e-916ba0a35536")]
    public enum EngineAttribute : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Name = 0x2,
        Culture = 0x4,
        Version = 0x8,
        PatchLevel = 0x10,
        Release = 0x20,
        SourceId = 0x40,
        SourceTimeStamp = 0x80,
        Configuration = 0x100,
        Tag = 0x200,
        Text = 0x400,
        TimeStamp = 0x800,
        CompileOptions = 0x1000,
        CSharpOptions = 0x2000,
        Uri = 0x4000,
        PublicKey = 0x8000,
        PublicKeyToken = 0x10000,
        ModuleVersionId = 0x20000,
        RuntimeOptions = 0x40000,
        ObjectIds = 0x80000,
        ImageRuntimeVersion = 0x100000,
        StrongName = 0x200000,
        StrongNameTag = 0x400000,
        Hash = 0x800000,
        Certificate = 0x1000000,
        UpdateBaseUri = 0x2000000,
        UpdatePathAndQuery = 0x4000000,
        DownloadBaseUri = 0x8000000,
        ScriptBaseUri = 0x10000000,
        AuxiliaryBaseUri = 0x20000000,
        TargetFramework = 0x40000000,
        NativeUtility = 0x80000000,
        InterpreterTimeStamp = 0x100000000,
        Vendor = 0x200000000,
        Suffix = 0x400000000,
        TextOrSuffix = 0x800000000,
        Default = Name,
        Reserved = 0x8000000000000000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("03316c9c-7871-4250-b28c-dc91412800ec")]
    public enum PairComparison
    {
        None = 0x0,
        Invalid = 0x1,
        LXRX = 0x2,
        LXRY = 0x4,
        LYRX = 0x8,
        LYRY = 0x10
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("2ace1c51-30ec-49a3-96a9-d453a1b2db1d")]
    public enum Priority
    {
        //
        // NOTE: All other positive integer values are also allowed in fields of this type.
        //
        Lowest = -2,
        None = -1,
        Highest = 0,
        Default = Highest
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("32619532-8777-4ddd-b430-7c867eba098e")]
    public enum Sequence
    {
        Invalid = -1,
        None = 0,

        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
        Fifth = 5,
        Sixth = 6,
        Seventh = 7,
        Eighth = 8,
        Nine = 9,
        Tenth = 10
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7a592738-e87b-4a8e-a0e5-730a71691f16")]
    public enum ObjectOptionType : ulong
    {
        None = 0x0,                     /* No options. */
        Invalid = 0x1,                  /* Invalid, do not use. */
        Alias = 0x2,                    /* For [object alias]. */
        Call = 0x4,                     /* For [library call]. */
        Callback = 0x8,                 /* For ConversionOps.ToCommandCallback method. */
        Certificate = 0x10,             /* For [library certificate] and [object certificate]. */
        Cleanup = 0x20,                 /* For [object cleanup]. */
        Create = 0x40,                  /* For [object create]. */
        Declare = 0x80,                 /* For [object declare]. */
        Delegate = 0x100,               /* For the ObjectOps.InvokeDelegate method. */

#if CALLBACK_QUEUE
        Dequeue = 0x200,                /* For [callback dequeue]. */
#endif

#if XML && SERIALIZATION
        Deserialize = 0x400,            /* For [xml deserialize]. */
#endif

        Dispose = 0x800,                /* For [object dispose]. */

#if NATIVE && TCL
        Evaluate = 0x1000,              /* For [tcl eval]. */
#endif

#if PREVIOUS_RESULT
        Exception = 0x2000,             /* For [debug exception]. */
#endif

#if DATA
        Execute = 0x4000,               /* For [sql execute]. */
#endif

        FireCallback = 0x8000,          /* For the CommandCallback class. */
        FixupReturnValue = 0x10000,     /* For the MarshalOps.FixupReturnValue method(s). */
        ForEach = 0x20000,              /* For [object foreach]. */
        Get = 0x40000,                  /* For [object get]. */
        Import = 0x80000,               /* For [object import]. */
        Invoke = 0x100000,              /* For [object invoke]. */
        InvokeRaw = 0x200000,           /* For [object invokeraw]. */
        InvokeAll = 0x400000,           /* For [object invokeall]. */
        InvokeShared = 0x800000,        /* For [object invoke] / [object invokeraw]. */
        IsNull = 0x1000000,             /* For [object isnull]. */
        IsOfType = 0x2000000,           /* For [object isoftype]. */
        Load = 0x4000000,               /* For [object load]. */
        Members = 0x8000000,            /* For [object members]. */
        Read = 0x10000000,              /* For [read]. */
        Search = 0x20000000,            /* For [object search]. */

#if XML && SERIALIZATION
        Serialize = 0x40000000,         /* For [xml serialize]. */
#endif

        SimpleCallback = 0x80000000,    /* For ConversionOps.ToCommandCallback method. */
        Type = 0x100000000,             /* For [object type]. */
        UnaliasNamespace = 0x200000000, /* For [object unaliasnamespace]. */
        Undeclare = 0x400000000,        /* For [object undeclare]. */
        Unimport = 0x800000000,         /* For [object unimport]. */
        Untype = 0x1000000000,          /* For [object untype]. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For the [object invoke*] sub-commands only.
        //
        ObjectInvokeOptionMask = Invoke | InvokeRaw | InvokeAll,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For the [library call] and [object invoke*]
        //       sub-commands only.
        //
        InvokeOptionMask = Call | ObjectInvokeOptionMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For all the [object] related sub-commands that are
        //       designed primarily to create object instances AS
        //       WELL AS their associated opaque object handles.
        //       The resulting opaque object handles may be disposed
        //       automatically (i.e. their IDispose.Dispose() method
        //       may be called just prior to their associated opaque
        //       object handles being removed from the containing
        //       interpreter context).  It should be noted here that
        //       using the "-create" flag to any of the [object]
        //       related sub-commands will trigger the automatic
        //       disposal behavior, even if that sub-command is not
        //       listed here.
        //
        CreateOptionMask = Create |
#if XML && SERIALIZATION
                           Deserialize |
#endif
                           Get |
                           Load,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For the [object] sub-commands only.
        //
        SubCommandMask = Alias | Certificate | Cleanup | Create |
                         Declare | Dispose | ForEach | Get |
                         Import | Invoke | InvokeRaw | InvokeAll |
                         IsNull | IsOfType | Load | Members | Search |
                         Type | UnaliasNamespace | Undeclare | Unimport |
                         Untype,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For the non-[object] commands / sub-commands only.
        //
        OtherCommandMask = Call |
            Certificate | // [library certificate]
#if CALLBACK_QUEUE
            Dequeue |
#endif
#if XML && SERIALIZATION
            Deserialize |
#endif
#if NATIVE && TCL
            Evaluate |
#endif
#if PREVIOUS_RESULT
            Exception |
#endif
#if DATA
            Execute |
#endif
            Read |
#if XML && SERIALIZATION
            Serialize |
#endif
            None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: All core marshaller related options.
        //
        ObjectMask = SubCommandMask | FixupReturnValue | InvokeShared,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default option type (i.e. [object invoke]).
        //
        Default = Invoke
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("242b38e9-3f23-4fbf-90a4-fbde0ad758af")]
    public enum OptionOriginFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Interactive = 0x2,
        CommandLine = 0x4,
        Environment = 0x8,
        Configuration = 0x10,
        Registry = 0x20,
        Default = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Override = 0x80,
        Remove = 0x100,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Any = Interactive | CommandLine | Environment |
              Configuration | Registry | Default,

        AnyOrOverride = Any | Override,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Create = AnyOrOverride,
        Shell = AnyOrOverride,
        Standard = AnyOrOverride,
        Plugin = AnyOrOverride,

#if NATIVE && TCL && NATIVE_PACKAGE
        NativePackage = AnyOrOverride | Remove,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
    [Flags()]
    [ObjectId("a3cceec2-cbd8-4a84-ae6c-fe2a8e0a334d")]
    public enum FindFlags : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        PreCallback = 0x2,
        PostCallback = 0x4,
        SpecificPath = 0x8,
        ScriptPath = 0x10,
        Environment = 0x20,
        AutoPath = 0x40,
        PackageBinaryPath = 0x80,
        PackageLibraryPath = 0x100,
        PackagePath = 0x200,
        EntryAssembly = 0x400,
        ExecutingAssembly = 0x800,
        BinaryPath = 0x1000,
        Registry = 0x2000,
        SearchPath = 0x4000,
        ExternalsPath = 0x8000,
        PeerPath = 0x10000,

#if UNIX
        LibraryPath = 0x20000,
        LocalLibraryPath = 0x40000,
#endif

        EvaluateScript = 0x80000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoOperatingSystem = 0x200000,
        FindArchitecture = 0x400000,
        GetArchitecture = 0x800000,
        MatchArchitecture = 0x1000000,
        RecursivePaths = 0x2000000,
        ZeroComponents = 0x4000000,
        RefreshAutoPath = 0x8000000,
        OverwriteBuilds = 0x10000000,
        TrustedOnly = 0x20000000,
        Trusted = 0x40000000,
        FileVersion = 0x80000000,
        AlternateName = 0x100000000,
        RefreshEvaluateScript = 0x200000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ExtraNamePatternList = 0x400000000,
        PrimaryNamePatternList = 0x800000000,
        SecondaryNamePatternList = 0x1000000000,
        OtherNamePatternList = 0x2000000000,

        ExtraVersionPatternList = 0x4000000000,
        PrimaryVersionPatternList = 0x8000000000,
        SecondaryVersionPatternList = 0x10000000000, /* NOT USED? */
        OtherVersionPatternList = 0x20000000000, /* NOT USED? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Part0 = 0x40000000000,
        Part1 = 0x80000000000,
        Part2 = 0x100000000000,
        Part3 = 0x200000000000,
        Part4 = 0x400000000000,
        Part5 = 0x800000000000,
        Part6 = 0x1000000000000,
        Part7 = 0x2000000000000,
        Part8 = 0x4000000000000,
        Part9 = 0x8000000000000,
        PartX = 0x10000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VerboseLooksLike = 0x20000000000000,
        VerboseExtractBuild = 0x40000000000000,
        VerboseRegistry = 0x80000000000000,
        VerboseSelect = 0x100000000000000,
        VerbosePath = 0x200000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        LocationMask = PreCallback | PostCallback | SpecificPath |
                       ScriptPath | Environment | AutoPath |
                       PackageBinaryPath | PackageLibraryPath | PackagePath |
                       EntryAssembly | ExecutingAssembly | BinaryPath |
                       Registry | SearchPath | ExternalsPath | PeerPath |
#if UNIX
                       LibraryPath | LocalLibraryPath |
#endif
                       EvaluateScript,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Architecture = FindArchitecture | GetArchitecture | MatchArchitecture,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = NoOperatingSystem | Architecture |
                    RecursivePaths | ZeroComponents |
                    RefreshAutoPath | OverwriteBuilds |
                    TrustedOnly | Trusted | FileVersion |
                    AlternateName | RefreshEvaluateScript |
                    Part0 | Part1 | Part2 | Part3 |
                    Part4 | Part5 | Part6 | Part7 |
                    Part8 | Part9 | PartX,

        ///////////////////////////////////////////////////////////////////////////////////////////

        PatternMask = ExtraNamePatternList | PrimaryNamePatternList |
                      SecondaryNamePatternList | OtherNamePatternList |
                      ExtraVersionPatternList | PrimaryVersionPatternList |
                      SecondaryVersionPatternList | OtherVersionPatternList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VerboseMask = VerboseLooksLike | VerboseExtractBuild | VerboseRegistry |
                      VerboseSelect | VerbosePath,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Callback = PreCallback | PostCallback,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = LocationMask | PatternMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Typical = All & ~(EvaluateScript | OtherNamePatternList | OtherVersionPatternList),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Standard = Typical | Architecture | TrustedOnly,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Typical
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8b71bb91-aa6b-45e6-97ea-d9e702b3ce92")]
    public enum LoadFlags
    {
        None = 0x0,             /* no special unload flags. */
        Reserved = 0x1,         /* reserved, do not use. */
        SetDllDirectory = 0x2,  /* call SetDllDirectory prior to loading. */

#if TCL_THREADED
        IgnoreThreaded = 0x4,   /* allow non-threaded builds to be loaded. */
#endif

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("473819a9-4470-410c-b5d2-14b690e64c83")]
    public enum UnloadFlags
    {
        None = 0x0,           /* no special unload flags. */
        Reserved = 0x1,       /* reserved, do not use. */
        NoInterpThread = 0x2, /* skip Tcl interpreter thread validation. */
        NoInterpActive = 0x4, /* skip checking if the Tcl interpreter is
                               * active. */
        ReleaseModule = 0x8,  /* release the module reference for the Tcl
                               * library. */
        ExitHandler = 0x10,   /* delete the exit handler. */
        Finalize = 0x20,      /* call the Tcl_Finalize delegate if possible. */
        FreeLibrary = 0x40,   /* free the operating system module handle if the
                               * reference count reaches zero. */

        //
        // NOTE: The following composite flag values are used as
        //       specific points in the TclWrapper code.
        //
        FromExitHandler = ReleaseModule | ExitHandler,

        FromDoOneEvent = ExitHandler | Finalize | FreeLibrary,

        FromLoad = NoInterpActive | ReleaseModule | ExitHandler |
                   Finalize | FreeLibrary,

        FromThread = Default,

        //
        // NOTE: About 99.9% of all external callers should use
        //       this composite flag value [and no other values].
        //
        Default = ReleaseModule | ExitHandler | Finalize | FreeLibrary
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if EMIT && NATIVE && LIBRARY
    [Flags()]
    [ObjectId("fd1eb082-ecbd-48c6-80d5-2ed2e06ce130")]
    public enum ModuleFlags
    {
        None = 0x0,
        NoUnload = 0x1, /* Do not call FreeLibrary when the reference
                         * count reaches zero. */
        NoRemove = 0x2  /* Do not allow the module to be removed from
                         * the interpreter. */
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ba60001b-81d5-4e38-8dec-3fcb1b80e39d")]
    public enum LoadType /* Used with the [object load] -type option. */
    {
        Invalid = -1,         /* Reserved, do not use. */
        None = 0x0,           /* Does not actually load anything. */
        PartialName = 0x1,    /* Assembly.Load using a partial name. */
        FullName = 0x2,       /* Assembly.Load using the full name. */
        File = 0x4,           /* Assembly.LoadFrom using a path and file name. */
        Bytes = 0x8,          /* Assembly.Load using an array of bytes (base64 encoded). */
        Stream = 0x10,        /* Assembly.Load using an array of bytes (from stream). */

        Default = PartialName /* Default load type, currently by name. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("95df58aa-6b4b-4fbb-98bf-948b034f912e")]
    public enum PromptType
    {
        Invalid = -1, /* invalid, do not use. */
        None = 0,     /* no prompt will be displayed. */
        Start = 1,    /* this is the type for a normal prompt. */
        Continue = 2  /* this is the type for a continued prompt, when additional
                       * input is required to complete a script. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f76751ef-752b-4a0b-adf6-fe26384d6686")]
    public enum PromptFlags
    {
        None = 0x0,         /* no special handling, normal start or continue
                             * prompt. */
        Invalid = 0x1,      /* invalid, do not use. */
        Debug = 0x8,        /* when set, this prompt is for the interactive
                             * debugger. */
        Queue = 0x10,       /* when set, this prompt is for queued (async)
                             * input mode. */
        Interpreter = 0x20, /* when set, make sure the interpreter Id is
                             * shown. */
        Done = 0x40         /* when set, it means that the host successfully
                             * displayed a prompt. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("5a23c5d2-f882-4b9b-bb97-66171b9e7e7a")]
    public enum FrameResult
    {
        Invalid = -1,
        Default = 0,
        Specific = 1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("4f0a702e-5bc5-4a4f-9d4a-1f0f13fc2d23")]
    public enum TestResult
    {
        Invalid = -1,
        Unknown = 0,
        Skipped = 1,
        Disabled = 2,
        Passed = 3,
        Failed = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("cb59bbb6-06b1-4d4a-82e1-93d1c0f54db9")]
    public enum UriEscapeType
    {
        Invalid = -1,
        None = 0,
        Uri = 1,
        Data = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
    [ObjectId("d1b2c836-5532-4af1-a5bd-879c3587727d")]
    public enum ControlEvent : uint /* COMPAT: Win32. */
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_RESERVED1_EVENT = 3,
        CTRL_RESERVED2_EVENT = 4,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2156b4a5-731c-4eeb-99a6-796de4af31b4")]
    public enum FormatMessageFlags /* COMPAT: Win32. */
    {
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x200,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x1000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("332c5315-7a93-44c7-9431-f4a915315a0d")]
    public enum FileAccessMask /* COMPAT: Win32. */
    {
        //
        //  Define the access mask as a longword sized structure divided up as
        //  follows:
        //
        //       3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
        //       1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
        //      +---------------+---------------+-------------------------------+
        //      |G|G|G|G|Res'd|A| StandardRights|         SpecificRights        |
        //      |R|W|E|A|     |S|               |                               |
        //      +-+-------------+---------------+-------------------------------+
        //
        //      typedef struct _ACCESS_MASK {
        //          WORD SpecificRights;
        //          BYTE StandardRights;
        //          BYTE AccessSystemAcl : 1;
        //          BYTE Reserved : 3;
        //          BYTE GenericAll : 1;
        //          BYTE GenericExecute : 1;
        //          BYTE GenericWrite : 1;
        //          BYTE GenericRead : 1;
        //      } ACCESS_MASK;
        //
        //  but to make life simple for programmer's we'll allow them to specify
        //  a desired access mask by simply OR'ing together mulitple single rights
        //  and treat an access mask as a DWORD.  For example
        //
        //      DesiredAccess = DELETE | READ_CONTROL
        //
        //  So we'll declare ACCESS_MASK as DWORD
        //

        NONE = 0x0,
        DELETE = 0x10000,
        READ_CONTROL = 0x20000,
        WRITE_DAC = 0x40000,
        WRITE_OWNER = 0x80000,
        SYNCHRONIZE = 0x100000,

        STANDARD_RIGHTS_REQUIRED = 0xF0000,
        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
        STANDARD_RIGHTS_ALL = 0x1F0000,
        SPECIFIC_RIGHTS_ALL = 0xFFFF,

        //
        // AccessSystemAcl access type
        //

        ACCESS_SYSTEM_SECURITY = 0x1000000,

        //
        // MaximumAllowed access type
        //

        MAXIMUM_ALLOWED = 0x2000000,

        //
        //  These are the generic rights.
        //

        GENERIC_READ = unchecked((int)0x80000000),
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000,
        GENERIC_READ_WRITE = GENERIC_READ | GENERIC_WRITE,

        //
        // Define access rights to files and directories
        //

        FILE_NONE = 0x0,
        FILE_READ_DATA = 0x1,            // file & pipe
        FILE_LIST_DIRECTORY = 0x1,       // directory
        FILE_WRITE_DATA = 0x2,           // file & pipe
        FILE_ADD_FILE = 0x2,             // directory
        FILE_APPEND_DATA = 0x4,          // file
        FILE_ADD_SUBDIRECTORY = 0x4,     // directory
        FILE_CREATE_PIPE_INSTANCE = 0x4, // named pipe
        FILE_READ_EA = 0x8,              // file & directory
        FILE_WRITE_EA = 0x10,            // file & directory
        FILE_EXECUTE = 0x20,             // file
        FILE_TRAVERSE = 0x20,            // directory
        FILE_DELETE_CHILD = 0x40,        // directory
        FILE_READ_ATTRIBUTES = 0x80,     // all
        FILE_WRITE_ATTRIBUTES = 0x100,   // all

        FILE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
                          SYNCHRONIZE |
                          0x1FF,

        FILE_GENERIC_READ = STANDARD_RIGHTS_READ |
                            FILE_READ_DATA |
                            FILE_READ_ATTRIBUTES |
                            FILE_READ_EA |
                            SYNCHRONIZE,

        FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE |
                             FILE_WRITE_DATA |
                             FILE_WRITE_ATTRIBUTES |
                             FILE_WRITE_EA |
                             FILE_APPEND_DATA |
                             SYNCHRONIZE,

        FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE |
                               FILE_READ_ATTRIBUTES |
                               FILE_EXECUTE |
                               SYNCHRONIZE
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6a35577d-d03a-4eb7-bda8-f876fd763a75")]
    public enum OperatingSystemId /* COMPAT: System.PlatformID. */
    {
        Win32s = PlatformID.Win32S,
        Windows9x = PlatformID.Win32Windows,
        WindowsNT = PlatformID.Win32NT,
        WindowsCE = PlatformID.WinCE,
        Unix = PlatformID.Unix,
#if NET_20_SP2 || NET_40 || NET_STANDARD_20
        Xbox = PlatformID.Xbox,     /* .NET 2.0 SP2+ only. */
        Darwin = PlatformID.MacOSX, /* .NET 2.0 SP2+ only. */
#else
        Xbox = 5,
        Darwin = 6,
#endif
        Mono_on_Unix = 128, // COMPAT: Mono.
        Unknown = unchecked((int)0xFFFFFFFF)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("caf88b71-e2b9-44ab-9807-4356759e95ef")]
    public enum ProcessorArchitecture : ushort /* COMPAT: Win32. */
    {
        Intel = 0,
        MIPS = 1,
        Alpha = 2,
        PowerPC = 3,
        SHx = 4,
        ARM = 5,
        IA64 = 6,
        Alpha64 = 7,
        MSIL = 8,
        AMD64 = 9,
        IA32_on_Win64 = 10,
        Neutral = 11,
        ARM64 = 12,
        Unknown = 0xFFFF
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("c12ed1e0-dc87-435b-8da8-a70d55341826")]
    public enum BoxCharacter
    {
        TopLeft = 0,
        TopJunction = 1,
        TopRight = 2,
        Horizontal = 3,
        Vertical = 4,
        LeftJunction = 5,
        CenterJunction = 6,
        RightJunction = 7,
        BottomLeft = 8,
        BottomJunction = 9,
        BottomRight = 10,
        Space = 11,
        Count = 12 /* Space + 1 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
    [ObjectId("e87b3a0a-6559-4222-aad5-c2a4b6312392")]
    public enum CacheCountType
    {
        Invalid = -1,
        None = 0,
        Hit = 1,
        Miss = 2,
        Skip = 3,
        Collide = 4,
        Found = 5,
        NotFound = 6,
        Add = 7,
        Change = 8,
        Remove = 9,
        Clear = 10,
        Trim = 11,
        SizeOf = 12
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("cacfb749-6c82-44b7-bf4c-e98b86621291")]
    public enum TestInformationType
    {
        CurrentName = -14,   // The name of the current test, if any.
        Interpreter = -13,   // The parent interpreter of the one running the test.
        RepeatCount = -12,   // The number of times a given test should be repeated.
        Verbose = -11,       // The test output flags (tcltest -verbose).
        KnownBugs = -10,     // The names of tests ran with knownBug constraints.
        Constraints = -9,    // The active test constraints.
#if DEBUGGER
        Breakpoints = -8,    // The active test breakpoints.
#endif
        Counts = -7,         // The number of times a given test has been run.
        PassedNames = -6,    // The names of the failing tests.
        SkippedNames = -5,   // The names of the skipped tests.
        FailedNames = -4,    // The names of the failing tests.
        SkipNames = -3,      // The patterns of tests to skip.
        MatchNames = -2,     // The patterns of tests to run.
        Level = -1,          // The test nesting level.
        Total = 0,           // Total number of tests encountered.
        Skipped = 1,         // Total number of tests that were skipped.
        Disabled = 2,        // Total number of tests that were disabled.
        Passed = 3,          // Total number of tests that passed.
        Failed = 4,          // Total number of tests that failed.
        SkippedBug = 5,      // Total number of tests that were skipped -AND- marked as "knownBug".
        DisabledBug = 6,     // Total number of tests that were disabled -AND- marked as "knownBug".
        PassedBug = 7,       // Total number of tests that passed -AND- marked as "knownBug".
        FailedBug = 8,       // Total number of tests that failed -AND- marked as "knownBug".
        SizeOf = 9           // Total number of statistics that we are keeping track of.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a43b5860-1ad6-433f-940e-ef8d5cd399c2")]
    public enum HostSizeType
    {
        None = 0x0,     /* The host size type is unspecified. */
        Invalid = 0x1,  /* Invalid, do not use. */
        Any = 0x2,      /* The current -OR- maximum size of the host input/output
                         * buffer -OR- window, leaving these choices up to the
                         * underlying host.  All hosts that implement the
                         * ResetSize, GetSize, or SetSize methods should support
                         * this flag. */
        Buffer = 0x4,   /* The size of the host input/output buffer.  All hosts
                         * that implement the ResetSize, GetSize, or SetSize
                         * methods should support this flag. */
        Window = 0x8,   /* The size of the host input/output window, if any.
                         * Not all hosts will support this flag, even if they
                         * implement the ResetSize, GetSize or SetSize methods.
                         */
        Current = 0x10, /* The current size (i.e. as opposed to the maximum
                         * size) of the host input/output buffer -OR- window.
                         * All hosts that implement the ResetSize, GetSize, or
                         * SetSize methods should support this flag. */
        Maximum = 0x20, /* The maximum size (i.e. as opposed to the current
                         * size) of the host input/output buffer -OR- window.
                         * Not all hosts will support this flag, even if they
                         * implement the ResetSize, GetSize, or SetSize methods.
                         */

        BufferCurrent = Buffer | Current,
        BufferMaximum = Buffer | Maximum,

        WindowCurrent = Window | Current,
        WindowMaximum = Window | Maximum,

        Default = Any
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("3496ebd1-6e02-44b0-8527-28ae79ea403b")]
    public enum HostFlags : ulong
    {
        None = 0x0,                                /* The host has no flags set. */
        Invalid = 0x1,                             /* Invalid, do not use. */
#if ISOLATED_PLUGINS
        Isolated = 0x2,                            /* The host is apparently running in an isolated
                                                    * application domain (i.e. it is not from the
                                                    * same application domain as the parent
                                                    * interpreter). */
#endif
        Complain = 0x4,                            /* The host may be used by DebugOps.Complain(). */
        Verbose = 0x8,                             /* DebugOps.Complain() should be used to emit
                                                    * error messages from the internal console
                                                    * support subsystems. */
        Debug = 0x10,                              /* The host may be used by DebugOps.Write(). */
        Test = 0x20,                               /* The host is operating in test mode (i.e.
                                                    * additional diagnostic code may be executed
                                                    * during each method call). */
        Prompt = 0x40,                             /* The host supports the Prompt method. */
        ForcePrompt = 0x80,                        /* The host wishes to display the prompt even
                                                    * when input has been redirected. */
        Title = 0x100,                             /* The host supports the DefaultTitle property
                                                    * and its associated subsystem. */
        Thread = 0x200,                            /* The host supports thread creation. */
        Exit = 0x400,                              /* The host supports the CanExit, CanForceExit,
                                                    * and Exiting properties. */
        WorkItem = 0x800,                          /* The host supports the queueing of work items. */
        Stream = 0x1000,                           /* The host supports the GetStream method. */
        Data = 0x2000,                             /* The host supports the GetData method. */
        Profile = 0x4000,                          /* The host supports the loading of profiles. */
        Sleep = 0x8000,                            /* The host supports the Sleep method. */
        Yield = 0x10000,                           /* The host supports the Yield method. */
        CustomInfo = 0x20000,                      /* The host supports the display of custom
                                                    * information via the WriteCustomInfo method. */
        Resizable = 0x40000,                       /* The host has a resizable input/output area. */
        HighLatency = 0x80000,                     /* The host has a high latency (i.e. it may be
                                                    * performing input/output over a poor quality
                                                    * remote connection). */
        LowBandwidth = 0x100000,                   /* The host has low bandwidth (i.e. it may be
                                                    * performing input/output over a poor quality
                                                    * remote connection). */
        Monochrome = 0x200000,                     /* The host input/output area only supports two
                                                    * colors. */
        Color = 0x400000,                          /* The host input/output area supports at least
                                                    * the "standard" colors (i.e. the ones from the
                                                    * ConsoleColor enumeration). */
        TrueColor = 0x800000,                      /* The host input/output area supports at least
                                                    * 24-bits of color. */
        ReversedColor = 0x1000000,                 /* The host input/output area supports reversing
                                                    * the foreground and background colors. */
        Text = 0x2000000,                          /* The host is text-based (i.e. hosted in a
                                                    * console window). */
        Graphical = 0x4000000,                     /* The host is graphical (i.e. hosted in a window
                                                    * that may or may not look and behave like a
                                                    * console). */
        Virtual = 0x8000000,                       /* The host is "virtual", meaning that it may not
                                                    * have any interactive input/output area (i.e.
                                                    * all the input and output may be simulated). */
        Sizing = 0x10000000,                       /* The host supports getting and setting the size
                                                    * of its content area.  This does not necessarily
                                                    * have anything to do with the width and height
                                                    * of the host [window] itself. */
        Positioning = 0x20000000,                  /* The host supports getting and setting positions
                                                    * within it. */
        Recording = 0x40000000,                    /* The host supports recording commands, including
                                                    * interactive commands (i.e. "demo" mode). */
        Playback = 0x80000000,                     /* The host supports playing back commands,
                                                    * including interactive commands (i.e. "demo"
                                                    * mode). */
        ZeroSize = 0x100000000,                    /* There is no input/output area OR it is so small
                                                    * that is unusable. */
        MinimumSize = 0x200000000,                 /* The input/output area is minimal.  It may not
                                                    * be large enough to display all of the "vital"
                                                    * header information at once. */
        CompactSize = 0x400000000,                 /* The input/output area is at least large enough
                                                    * to display all of the "vital" header information
                                                    * at once. */
        FullSize = 0x800000000,                    /* The input/output area is at least large enough
                                                    * to display all of the "standard" header
                                                    * information at once. */
        SuperFullSize = 0x1000000000,              /* The input/output area is at least large enough
                                                    * to display all of the "standard" header
                                                    * information at once. */
        JumboSize = 0x2000000000,                  /* The input/output area is at least large enough
                                                    * to display all of the "standard" header
                                                    * information and some of the "extra"
                                                    * header information at once. */
        SuperJumboSize = 0x4000000000,             /* The input/output area is at least large enough
                                                    * to display all of the "standard" header
                                                    * information and all of the "extra" header
                                                    * information at once. */
        UnlimitedSize = 0x8000000000,              /* The input/output area should be considered to
                                                    * have an infinite size. */
        MultipleLineInput = 0x10000000000,         /* The input area supports multiple lines of input
                                                    * at the same time. */
        AutoFlushHost = 0x20000000000,             /* Automatically flush from within WriteCore
                                                    * methods. */
        AutoFlushWriter = 0x40000000000,           /* Automatically flush created stream writers. */
        AutoFlushOutput = 0x80000000000,           /* Automatically flush the host output stream
                                                    * after [puts]. */
        AutoFlushError = 0x100000000000,           /* Automatically flush the host error stream
                                                    * after [puts]. */
        AdjustColor = 0x200000000000,              /* Adjust (i.e. "fine-tune") the foreground and
                                                    * background colors. */
        QueryState = 0x400000000000,               /* The host supports the QueryState method. */
        ReadException = 0x800000000000,            /* An exception was thrown when reading. */
        WriteException = 0x1000000000000,          /* An exception was thrown when writing. */
        NoColorNewLine = 0x2000000000000,          /* Do not write a new line while the color is set
                                                    * to a non-default value. */
        SavedColorForNone = 0x4000000000000,       /* Always make use of a foreground and/or background
                                                    * color when writing to the host, even when the
                                                    * caller uses the color "None".  Normally, for the
                                                    * "None" color, the originally saved color (i.e.
                                                    * foreground or background) will be used. */
        RestoreColorAfterWrite = 0x8000000000000,  /* After writing something in color, restore the
                                                    * initially saved colors instead of previously
                                                    * active colors. */
        ResetColorForRestore = 0x10000000000000,   /* When restoring colors, forcibly reset to those
                                                    * internally saved by the Console if either of
                                                    * the saved colors is not valid. */
        MaybeResetColorForSet = 0x20000000000000,  /* When setting colors, use reset instead of they
                                                    * would be set back to the saved foreground and
                                                    * background colors. */
        TraceColorNotChanged = 0x40000000000000,   /* Emit trace messages when the foreground and/or
                                                    * background colors were not actually changed. */
        TreatAsFatalError = 0x80000000000000,      /* Non-zero when error messages should be treated
                                                    * as though they are unrecoverable. */
        NoSetForegroundColor = 0x100000000000000,  /* Disable setting of the foreground color. */
        NoSetBackgroundColor = 0x200000000000000,  /* Disable setting of the background color. */
        NormalizeToNewLine = 0x400000000000000,    /* Change "\r\n" line-endings to new-line "\n"
                                                    * only. */
        TreatMissingLineAsEof = 0x800000000000000, /* If a line cannot be read from the host, treat
                                                    * that condition as end-of-file (no more input,
                                                    * so exit the interactive loop, etc).  In most
                                                    * use cases, this flag should NOT be needed. */

        ExceptionMask = ReadException | WriteException,

        NonMonochromeMask = Color | TrueColor,

        AllColors = Monochrome | NonMonochromeMask,

        AllSizes = ZeroSize | MinimumSize | CompactSize |
                   FullSize | SuperFullSize | JumboSize |
                   SuperJumboSize | UnlimitedSize,

        Reserved = 0x8000000000000000 // NOTE: Reserved, do not use.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9f0e7e9d-6801-4fd3-a48b-ef062d8b2b8f")]
    public enum HostTestFlags
    {
        None = 0x0,          /* The host has no flags set. */
        Invalid = 0x1,       /* Invalid, do not use. */
        CustomInfo = 0x2     /* Test the custom information method. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d607b21a-e810-4d11-8a2a-f8a09aa178f7")]
    public enum HeaderFlags : ulong
    {
        None = 0x0,                    /* Displays nothing. */
        Invalid = 0x1,                 /* This value indicates that the header flags have not yet
                                        * been explicitly set by the user. */
        StopPrompt = 0x2,              /* Display debugger "[Stop]" prompts. */
        GoPrompt = 0x4,                /* Display debugger "[Go]" prompts. */
        AnnouncementInfo = 0x8,        /* Displays debugger banner (e.g. "Eagle Debugger"). */

#if DEBUGGER
        DebuggerInfo = 0x10,           /* Displays properties of the active debugger. */
#endif

        EngineInfo = 0x20,             /* Displays engine related properties of the active
                                        * interpreter. */
        ControlInfo = 0x40,            /* Displays control related properties of the active
                                        * interpreter. */
        EntityInfo = 0x80,             /* Displays per-type entity counts for the active
                                        * interpreter. */
        StackInfo = 0x100,             /* Displays native stack space information. */
        FlagInfo = 0x200,              /* Displays engine, substitution, notification, and other
                                        * flags for the active interpreter. */
        ArgumentInfo = 0x400,          /* Displays the "reason" for breaking into the debugger.
                                        * This is typically only used for breakpoints. */
        TokenInfo = 0x800,             /* Displays the specified token information.  This is
                                        * typically only used for token-based breakpoints. */
        TraceInfo = 0x1000,            /* Displays the specified variable trace information.
                                        * This is typically only used for variable watches and
                                        * traces. */
        InterpreterInfo = 0x2000,      /* Displays the state information for the active
                                        * interpreter that does not fit neatly into the other
                                        * categories. */
        CallStack = 0x4000,            /* Displays the call stack (i.e. the "evaluation stack")
                                        * for the active interpreter (current thread only). */
        CallStackInfo = 0x8000,        /* Displays the call stack (i.e. the "evaluation stack")
                                        * for the active interpreter (current thread only)
                                        * using the "boxed" style. */
        VariableInfo = 0x10000,        /* Displays properties of the specified variable. */
        ObjectInfo = 0x20000,          /* Displays properties of the specified object. */
        HostInfo = 0x40000,            /* Displays properties of the host for the active
                                        * interpreter. */
        TestInfo = 0x80000,            /* Displays test properties and statistics for the
                                        * active interpreter. */
        CallFrameInfo = 0x100000,      /* Displays properties of the specified call frame. */
        ResultInfo = 0x200000,         /* Displays the return code, string result, and error
                                        * line number for the current and previous results,
                                        * if any. */

#if PREVIOUS_RESULT
        PreviousResultInfo = 0x400000, /* Displays the return code, string result, and error
                                        * line number for the current and previous results,
                                        * if any. */
#endif

        ComplaintInfo = 0x800000,      /* Displays the previously stored "complaint" for the
                                        * active interpreter, if any. */

#if HISTORY
        HistoryInfo = 0x1000000,       /* Displays the command history for the active
                                        * interpreter, if any has been recorded. */
#endif

        OtherInfo = 0x2000000,         /* Reserved for future use. */
        CustomInfo = 0x4000000,        /* Displays custom information provided by the
                                        * underlying host implementation, if any. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        User = 0x8000000,              /* Indicates that the header flags have been explicitly
                                        * set by the user. */
        AutoSize = 0x10000000,         /* selectively displays information based on host style
                                        * (i.e. "Compact", "Full", or "Jumbo") as returned by
                                        * GetFlags(). */
        AutoRetry = 0x20000000,        /* If a call to WriteBox fails, retry it again inside
                                        * of a new section. */
        EmptySection = 0x40000000,     /* Display all the selected sections even if they
                                        * contain no meaningful content.  Typically, this flag
                                        * is only set when debugging IHost implementations. */
        EmptyContent = 0x80000000,     /* Display all the content in the selected sections,
                                        * even default and empty values. */
        VerboseSection = 0x100000000,  /* Display all the selected sections even if they
                                        * contain only verbose content.  Typically, this flag
                                        * is only set when debugging IHost implementations. */
        VerboseContent = 0x200000000,  /* Display all the content in the selected sections,
                                        * even verbose content. */
        Debug = 0x400000000,           /* The debugger is active in the parent interactive
                                        * loop. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CallStackAllFrames = 0x800000000,   /* Display all call frames, not just those accessible
                                             * via scripts. */
        DebuggerBreakpoints = 0x1000000000, /* Display debugger breakpoint information, if any. */
        EngineNative = 0x2000000000,        /* Display [engine] native integration information, if
                                             * any. */
        HostDimensions = 0x4000000000,      /* Display interpreter host dimension information, if
                                             * any. */
        HostFormatting = 0x8000000000,      /* Display interpreter host formatting information, if
                                             * any. */
        HostColors = 0x10000000000,         /* Display interpreter host color information, if any.
                                             */
        HostNames = 0x20000000000,          /* Display interpreter host named theme information, if
                                             * any. */
        TraceCached = 0x40000000000,        /* Display interpreter trace information instead of the
                                             * specified trace information. */
        VariableLinks = 0x80000000000,      /* Display variable link information, if any. */
        VariableSearches = 0x100000000000,  /* Display variable search information, if any. */
        VariableElements = 0x200000000000,  /* Display variable array element information, if any.
                                             */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections are considered "vital" information when the
        //       script debugger calls the default host WriteHeader method.
        //
        Level1 = AnnouncementInfo |
#if DEBUGGER
                 DebuggerInfo |
#endif
                 ResultInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections are considered useful information when the
        //       script debugger calls the default host WriteHeader method.
        //
        Level2 = ControlInfo | OtherInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections may not actually be written by the default
        //       host WriteHeader method (e.g. if the associated data is
        //       null or otherwise
        //       unavailable from the current context.
        //
        Level3 = ArgumentInfo | TokenInfo | TraceInfo |
                 VariableInfo | ObjectInfo | CallFrameInfo |
#if PREVIOUS_RESULT
                 PreviousResultInfo |
#endif
                 ComplaintInfo | CustomInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections are written by the default host WriteHeader
        //       method, unless the interpreter is null -OR- from a different
        //       application domain.
        //
        Level4 = InterpreterInfo | CallStackInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections contain a lot of useful data; however, they
        //       are typically more useful for debugging Eagle itself rather
        //       than scripts.
        //
        Level5 = EngineInfo | FlagInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is specifically for use with the default host
        //       WriteHeader method when it is invoked due to the "#show"
        //       interactive command.
        //
        Show = Level1 | Level2 | Level3 | Level4 | Level5,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllPrompt = StopPrompt | GoPrompt,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllInfo = AnnouncementInfo |
#if DEBUGGER
                  DebuggerInfo |
#endif
                  EngineInfo | ControlInfo | EntityInfo |
                  StackInfo | FlagInfo | ArgumentInfo |
                  TokenInfo | TraceInfo | InterpreterInfo |
                  CallStackInfo | VariableInfo |
                  ObjectInfo | HostInfo | TestInfo |
                  CallFrameInfo | ResultInfo |
#if PREVIOUS_RESULT
                  PreviousResultInfo |
#endif
                  ComplaintInfo |
#if HISTORY
                  HistoryInfo |
#endif
                  OtherInfo | CustomInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllEmptyFlags = EmptySection | EmptyContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllVerboseFlags = VerboseSection | VerboseContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllHostFlags = HostDimensions | HostFormatting | HostColors |
                       HostNames,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllAutoFlags = AutoSize | AutoRetry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllContentFlags = CallStackAllFrames | DebuggerBreakpoints | EngineNative |
                          AllHostFlags | TraceCached | VariableLinks |
                          VariableSearches | VariableElements,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllFlags = User | AllAutoFlags | AllEmptyFlags | AllVerboseFlags | Debug |
                   AllContentFlags,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = AllPrompt | CallStack | AllInfo | AllContentFlags | AllAutoFlags,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllForTest = (All | AllEmptyFlags) & ~AutoSize,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Breakpoint = AllPrompt | ArgumentInfo | TokenInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Watchpoint = AllPrompt | TraceInfo | VariableInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Maximum = All & ~AutoSize | AutoRetry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Full = Maximum & ~CallStack,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = AllAutoFlags | Level1 | Level3
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if HISTORY
    [Flags()]
    [ObjectId("21773a14-5704-467e-8df8-9137d5197404")]
    public enum HistoryFlags
    {
        None = 0x0,        /* no flags. */
        Invalid = 0x1,     /* invalid, do not use. */
        Engine = 0x2,      /* script command added via Engine. */
        Interactive = 0x4, /* interactive-only command added via REPL. */

        InstanceMask = Engine | Interactive
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
    [Flags()]
    [ObjectId("f70a8497-05dd-40a5-9b9d-93e2950aa095")]
    public enum FileShareMode /* COMPAT: Win32. */
    {
        FILE_SHARE_NONE = 0x0,
        FILE_SHARE_READ = 0x1,
        FILE_SHARE_WRITE = 0x2,
        FILE_SHARE_DELETE = 0x4,
        FILE_SHARE_READ_WRITE = FILE_SHARE_READ |
                                FILE_SHARE_WRITE,
        FILE_SHARE_ALL = FILE_SHARE_READ |
                         FILE_SHARE_WRITE |
                         FILE_SHARE_DELETE
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("ca9e0cd3-9bd3-47db-9952-79a3c132a53d")]
    public enum FileCreationDisposition /* COMPAT: Win32. */
    {
        NONE = 0,
        CREATE_NEW = 1,
        CREATE_ALWAYS = 2,
        OPEN_EXISTING = 3,
        OPEN_ALWAYS = 4,
        TRUNCATE_EXISTING = 5
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("5e06f9b4-0a39-452e-ac2d-0e6448f0863e")]
    public enum FileFlagsAndAttributes /* COMPAT: Win32. */
    {
        //
        // File attributes used with CreateFile
        //

        FILE_ATTRIBUTE_NONE = 0x0,
        FILE_ATTRIBUTE_READONLY = 0x1,
        FILE_ATTRIBUTE_HIDDEN = 0x2,
        FILE_ATTRIBUTE_SYSTEM = 0x4,
        FILE_ATTRIBUTE_DIRECTORY = 0x10,
        FILE_ATTRIBUTE_ARCHIVE = 0x20,
        FILE_ATTRIBUTE_DEVICE = 0x40,
        FILE_ATTRIBUTE_NORMAL = 0x80,
        FILE_ATTRIBUTE_TEMPORARY = 0x100,
        FILE_ATTRIBUTE_SPARSE_FILE = 0x200,
        FILE_ATTRIBUTE_REPARSE_POINT = 0x400,
        FILE_ATTRIBUTE_COMPRESSED = 0x800,
        FILE_ATTRIBUTE_OFFLINE = 0x1000,
        FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000,
        FILE_ATTRIBUTE_ENCRYPTED = 0x4000,

        //
        // Define the Security Quality of Service bits to be passed
        // into CreateFile
        //

        SECURITY_ANONYMOUS = 0x0,
        SECURITY_IDENTIFICATION = 0x10000,
        SECURITY_IMPERSONATION = 0x20000,
        SECURITY_DELEGATION = 0x30000,
        SECURITY_CONTEXT_TRACKING = 0x40000,
        SECURITY_EFFECTIVE_ONLY = 0x80000,
        SECURITY_SQOS_PRESENT = 0x100000,

        //
        // File creation flags must start at the high end since they
        // are combined with the attributes
        //

        FILE_FLAG_FIRST_PIPE_INSTANCE = 0x80000,
        FILE_FLAG_OPEN_NO_RECALL = 0x100000,
        FILE_FLAG_OPEN_REPARSE_POINT = 0x200000,
        FILE_FLAG_POSIX_SEMANTICS = 0x1000000,
        FILE_FLAG_BACKUP_SEMANTICS = 0x2000000,
        FILE_FLAG_DELETE_ON_CLOSE = 0x4000000,
        FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000,
        FILE_FLAG_RANDOM_ACCESS = 0x10000000,
        FILE_FLAG_NO_BUFFERING = 0x20000000,
        FILE_FLAG_OVERLAPPED = 0x40000000,
        FILE_FLAG_WRITE_THROUGH = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7f34cb91-54db-4bb1-9d11-63afd74bedb7")]
    public enum FileStatusModes /* COMPAT: POSIX. */
    {
        S_INONE = 0x0000,
        S_IEXEC = 0x0040,
        S_IWRITE = 0x0080,
        S_IREAD = 0x0100,
        S_IFDIR = 0x4000,
        S_IFREG = 0x8000,
        S_IFLNK = 0xA000,

        S_IRWX = S_IREAD | S_IWRITE | S_IEXEC
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("687b2526-63ad-4367-8a80-40441fe9396d")]
    public enum ExitCode
    {
        Unknown = -1, /* For internal use only. */
        Success = 0,  /* COMPAT: Unix. */
        Failure = 1,  /* COMPAT: Unix. */
        Exception = 2,
        Fatal = 255
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6c55d49e-b4c3-4159-a608-214802e850fb")]
    public enum DataFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Bytes = 0x2,
        Text = 0x4,

        ReservedName = 0x40,

        NoStream = 0x100,
        NoString = 0x200,

        NoPluginStream = 0x1000,
        NoPluginString = 0x2000,

        NoResourceManagerStream = 0x10000,
        NoResourceManagerString = 0x20000,

        NoAssemblyManifestStream = 0x100000,
        NoAssemblyManifestString = 0x200000,

        Script = Text | NoResourceManagerStream,
        Plugin = Bytes | NoString
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9eb389ed-2d2a-4be1-823f-17717116e9cf")]
    public enum ScriptFlags : ulong
    {
        None = 0x0,                                        /* no special flags. */
        Invalid = 0x1,                                     /* invalid, do not use. */
        UseDefault = 0x2,                                  /* the engine should [also] use the default
                                                            * flags when searching for a script that
                                                            * cannot be found on the file system. */
        System = 0x4,                                      /* reserved, do not use */
        Core = 0x8,                                        /* script is part of the core language
                                                            * distribution. */
        Interactive = 0x10,                                /* script is only used by the interactive
                                                            * shell. */
        Package = 0x20,                                    /* script is part of a user package. */
        Application = 0x40,                                /* script is part of a user application. */
        Vendor = 0x80,                                     /* script is part of a vendor package. */
        User = 0x100,                                      /* script is user customizable. */
        SpecificPath = 0x200,                              /* the name should be checked verbatim if it is
                                                            * a fully qualified path. */
        Mapped = 0x400,                                    /* allow script location resolution via the
                                                            * eagle_paths script variable. */
        AutoSourcePath = 0x800,                            /* allow script location resolution via the
                                                            * auto_source_path script variable. */
        Library = 0x1000,                                  /* script has library semantics, including
                                                            * searching the package directory and the
                                                            * auto_path to find it. */
        Data = 0x2000,                                     /* script is really a data file (not strictly
                                                            * a script). */
        Required = 0x4000,                                 /* script is required for proper operation. */
        Optional = 0x8000,                                 /* script not required for proper operation. */
        File = 0x10000,                                    /* script resides in the specified file. */
        PreferFileSystem = 0x20000,                        /* check the file system before using the
                                                            * IHost.GetData method for library scripts. */
        NoAutoPath = 0x40000,                              /* do not try to find the script along the
                                                            * auto_path. */
        NoHost = 0x80000,                                  /* the IHost.GetData method should not be used
                                                            * for this request. */
        NoFileSystem = 0x100000,                           /* the core host should not look for the script
                                                            * on the native file system. */
        NoResources = 0x200000,                            /* the core host should not look for the script
                                                            * via resources, embedded or otherwise. */
        NoPlugins = 0x400000,                              /* the core host should not look for the script
                                                            * via plugin resource strings. */
        NoResourceManager = 0x800000,                      /* the core host should not look for the script
                                                            * via the interpreter or host resource managers.
                                                            */
#if XML
        NoXml = 0x1000000,                                 /* the core host should not attempt to interpret
                                                            * the file as an XML script. */
#endif
        SkipQualified = 0x2000000,                         /* the core host should skip using the qualfied
                                                            * name as the basis for a resource name. */
        SkipNonQualified = 0x4000000,                      /* the core host should skip using the
                                                            * non-qualified name as the basis for a resource
                                                            * name. */
        SkipRelative = 0x8000000,                          /* the core host should skip using the relative
                                                            * name as the basis for a resource name. */
        SkipRawName = 0x10000000,                          /* the core host should skip treating the name
                                                            * as the raw resource name itself during the
                                                            * search. */
        SkipFileName = 0x20000000,                         /* the core host should skip treating the name
                                                            * as a file name during the search. */
        SkipFileNameOnly = 0x40000000,                     /* the core host should skip file names that
                                                            * ignore the package type during the search. */
        SkipNonFileNameOnly = 0x80000000,                  /* the core host should skip file names that
                                                            * do not ignore the package type during the
                                                            * search. */
        SkipLibraryToLib = 0x100000000,                    /* the core host should skip fixing up the
                                                            * "Library" path portion to "lib" during the
                                                            * search. */
        SkipTestsToLib = 0x200000000,                      /* the core host should skip fixing up the
                                                            * "Tests" path portion to "lib/Tests" during
                                                            * the search. */
        StrictGetFile = 0x400000000,                       /* null must be returned if the script file is
                                                            * not found on the file system */
        ErrorOnEmpty = 0x800000000,                        /* forbid null and empty script values even upon
                                                            * success unless they are flagged as optional
                                                            * and NOT flagged as required. */
        FailOnException = 0x1000000000,                    /* unexpected exceptions should cause the
                                                            * remainder of the script search to be canceled
                                                            * and an error to be returned. */
        StopOnException = 0x2000000000,                    /* unexpected exceptions should cause the
                                                            * remainder of the script search to be canceled.
                                                            */
        FailOnError = 0x4000000000,                        /* unexpected errors should cause the remainder
                                                            * of the script search to be canceled and an
                                                            * error to be returned. */
        IgnoreCanRetry = 0x8000000000,                     /* ignore the canRetry value returned by the
                                                            * script engine for reading script streams, etc. */
        StopOnError = 0x10000000000,                       /* unexpected errors should cause the remainder
                                                            * of the script search to be canceled. */
        Silent = 0x20000000000,                            /* return minimum error information if a script
                                                            * cannot be found. */
        Quiet = 0x40000000000,                             /* return normal error information if a script
                                                            * cannot be found. */
        Verbose = 0x80000000000,                           /* return maximum error information if a script
                                                            * cannot be found. */
        PreferDeepFileNames = 0x100000000000,              /* prefer file names that are longer. */
        PreferDeepResourceNames = 0x200000000000,          /* prefer resource names that are longer. */
        SearchDirectory = 0x400000000000,                  /* when searching for a script on the file
                                                            * system, consider candidate locations that
                                                            * represent a directory. */
        SearchFile = 0x800000000000,                       /* when searching for a script on the file
                                                            * system, consider candidate locations that
                                                            * represent a file. */
        NoLibraryFile = 0x1000000000000,                   /* the file system should be disallowed when
                                                            * searching for the core script library. */
        ClientData = 0x2000000000000,                      /* the IClientData has been filled in with
                                                            * auxiliary data (e.g. the associated plugin
                                                            * file name). */
        LibraryPackage = 0x4000000000000,                  /* the script is part of the core script
                                                            * library. */
        TestPackage = 0x8000000000000,                     /* the script is part of the core script
                                                            * library test package. */
        AutomaticPackage = 0x10000000000000,               /* the script is part of the core script
                                                            * library -OR- test package. */
        FilterOnSuffixMatch = 0x20000000000000,            /* avoid checking resource names that do not
                                                            * fit within the suffix of the specified
                                                            * name. */
        NoTrace = 0x40000000000000,                        /* prevent the core host GetDataTrace and
                                                            * FilterScriptResourceNamesTrace methods
                                                            * from emitting diagnostics. */
        NoPolicy = 0x80000000000000,                       /* Disable policy execution when looking up
                                                            * the script? */
        NoAssemblyManifest = 0x100000000000000,            /* the core host should not look for the
                                                            * script via manifest assembly resources.
                                                            */
        NoLibraryFileNameOnly = 0x200000000000000,         /* do not check the file system for just the
                                                            * file name portion of the requested library
                                                            * script. */
        NoPluginResourceName = 0x400000000000000,          /* skip calling the IPlugin.GetString method
                                                            * for the plugin-name decorated resource
                                                            * name. */
        NoRawResourceName = 0x800000000000000,             /* skip calling the IPlugin.GetString method
                                                            * for the raw, undecorated resource name. */
        NoHostResourceManager = 0x1000000000000000,        /* the core host should not look for the script
                                                            * via the host resource manager.
                                                            */
        NoApplicationResourceManager = 0x2000000000000000, /* the core host should not look for the script
                                                            * via the application resource manager.
                                                            */
        NoLibraryResourceManager = 0x4000000000000000,     /* the core host should not look for the script
                                                            * via the core library resource manager.
                                                            */
        NoPackagesResourceManager = 0x8000000000000000,    /* the core host should not look for the script
                                                            * via the core packages resource manager.
                                                            */

        //
        // NOTE: When using resource names, forbid all names that are
        //       not an exact match.
        //
        ExactNameOnly = SkipLibraryToLib | SkipTestsToLib | SkipFileName |
                        SkipRelative | SkipNonQualified,

        //
        // NOTE: Only consider library resources from the core library
        //       assembly.
        //
        CoreAssemblyOnly = NoFileSystem | NoPlugins | NoHostResourceManager |
                           NoApplicationResourceManager | NoLibraryFile |
                           NoAssemblyManifest | NoLibraryFileNameOnly,

        //
        // NOTE: For plugin binaries, do not consult the file system -AND-
        //       only allow exact name matching.
        //
        PluginBinaryOnly = NoFileSystem | ExactNameOnly,

        //
        // NOTE: If the "locked down" build configuration is in use, force
        //       all scripts to be fetched from the core library assembly.
        //
#if ENTERPRISE_LOCKDOWN
        MaybeCoreAssemblyOnly = CoreAssemblyOnly,
#else
        //
        // WARNING: This value cannot be the same as "None".  Do not modify
        //          or various things may break.
        //
        MaybeCoreAssemblyOnly = Invalid,
#endif

        //
        // NOTE: Even if this module has been built with an embedded
        //       script library, we still want to allow the application
        //       (or the user) to override the various embedded library
        //       scripts by placing the correct file(s) in the proper
        //       location(s) on the native file system.  This flag has
        //       no effect unless the "Library" flag is also specified.
        //
#if EMBEDDED_LIBRARY
        EmbeddedLibrary = PreferFileSystem | AutomaticPackage,
#else
        EmbeddedLibrary = AutomaticPackage,
#endif

        SearchAny = SpecificPath | SearchDirectory | SearchFile,

        RequiredFile = Required | StrictGetFile | SearchAny,
        OptionalFile = Optional | StrictGetFile | SearchAny,

        CoreRequiredFile = Core | RequiredFile | IgnoreCanRetry | EmbeddedLibrary,
        CoreLibraryRequiredFile = CoreRequiredFile | Library | MaybeCoreAssemblyOnly,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CoreLibrarySecurityRequiredFile =
#if XML
            NoXml |
#endif
            CoreRequiredFile | Library |
#if EMBEDDED_LIBRARY
            CoreAssemblyOnly,
#else
            None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        CoreOptionalFile = Core | OptionalFile | IgnoreCanRetry | EmbeddedLibrary,
        CoreLibraryOptionalFile = CoreOptionalFile | Library,

        PackageOptionalFile = Package | OptionalFile | IgnoreCanRetry | EmbeddedLibrary,
        PackageLibraryOptionalFile = PackageOptionalFile | Library,

        PackageRequiredFile = Package | RequiredFile | IgnoreCanRetry | EmbeddedLibrary,
        PackageLibraryRequiredFile = PackageRequiredFile | Library,

        ApplicationRequiredFile = Application | RequiredFile | IgnoreCanRetry | EmbeddedLibrary,
        ApplicationLibraryRequiredFile = ApplicationRequiredFile | Library,

        ApplicationOptionalFile = Application | OptionalFile | IgnoreCanRetry | EmbeddedLibrary,
        ApplicationLibraryOptionalFile = ApplicationOptionalFile | Library,

        VendorRequiredFile = Vendor | RequiredFile | IgnoreCanRetry | EmbeddedLibrary,
        VendorLibraryRequiredFile = VendorRequiredFile | Library,

        VendorOptionalFile = Vendor | OptionalFile | IgnoreCanRetry | EmbeddedLibrary,
        VendorLibraryOptionalFile = VendorOptionalFile | Library,

        UserRequiredFile = User | RequiredFile | IgnoreCanRetry | EmbeddedLibrary,
        UserLibraryRequiredFile = UserRequiredFile | Library,

        UserOptionalFile = User | OptionalFile | IgnoreCanRetry | EmbeddedLibrary,
        UserLibraryOptionalFile = UserOptionalFile | Library,

        UseDefaultGetDataFile = ApplicationRequiredFile | IgnoreCanRetry, // COMPAT: Eagle (legacy).

        Default = UseDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7349287b-88d3-40d1-9c63-1100774aff17")]
    public enum ScriptBlockFlags
    {
        None = 0x0,            /* no special handling. */
        Invalid = 0x1,         /* invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        OkOrReturnOnly = 0x2,  /* only "Ok" And "Return" will be considered
                                * as successful return codes.
                                */
        AllowExceptions = 0x4, /* allow exceptional return codes.  this has
                                * no effect if the OkOrReturnOnly flag is
                                * used.
                                */
        TrimSpace = 0x8,       /* trim all surrounding whitespace from all
                                * successful script block results.
                                */
        EmitErrors = 0x10,     /* include any script block errors in the
                                * generated output.  these are errors from
                                * the script evaluation itself.
                                */
        StopOnError = 0x20,    /* stop further processing if a script block
                                * results in an error.
                                */
        EmitFailures = 0x40,   /* include any script block failures in the
                                * generated output.  these are errors that
                                * prevent the script block from actually
                                * being processed, e.g. parsing issues.
                                */
        StopOnFailure = 0x80,  /* stop further processing if a script block
                                * cannot be processed, e.g. due to a parse
                                * failure.
                                */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = unchecked((int)0x80000000), /* reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Under normal conditions, this is the combination of flags
        //       that should be used.
        //
        Standard = OkOrReturnOnly | AllowExceptions | TrimSpace |
                   EmitErrors | StopOnError | EmitFailures |
                   StopOnFailure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the combination of flags that can be used to force
        //       processing to continue even if script evaluation errors or
        //       parsing failures are encountered.
        //
        Relaxed = Standard & ~(StopOnError | StopOnFailure),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("cb195945-70af-4c06-8e81-dc694ed4182a")]
    public enum Boolean : byte /* COMPAT: Tcl. */
    {
        False = 0,
        True = 1,
        No = 0,
        Yes = 1,
        Off = 0,
        On = 1,
        Disable = 0,
        Enable = 1,
        Disabled = 0,
        Enabled = 1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("435e1a60-3960-4512-9cb9-eecb0439c4da")]
    public enum TestOutputType
    {
        None = 0x0,            /* nothing. */
        Invalid = 0x1,         /* invalid, do not use. */
        AutomaticWrite = 0x2,  /* Interpreter: automatic handling of the test output
                                * to write. */
        AutomaticReturn = 0x4, /* Interpreter: automatic handling of the test output
                                * to return. */
        AutomaticLog = 0x8,    /* Interpreter: automatic handling of the test output
                                * to log. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Body = 0x10,           /* COMPAT: tcltest (body of failed tests). */
        B = Body,              /* Same as "Body". */
        Pass = 0x20,           /* COMPAT: tcltest (when a test passes). */
        P = Pass,              /* Same as "Pass". */
        Skip = 0x40,           /* COMPAT: tcltest (when a test is skipped). */
        S = Skip,              /* Same as "Skip". */
        Start = 0x80,          /* COMPAT: tcltest (when a test starts). */
        T = Start,             /* Same as "Start". */
        Error = 0x100,         /* COMPAT: tcltest (errorCode/errorInfo on
                                * failure). */
        E = Error,             /* Same as "Error". */
        Line = 0x200,          /* COMPAT: tcltest (source file line info on
                                * failure). */
        L = Line,              /* Same as "Line". */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Fail = 0x400,          /* Eagle: when a test fails. */
        F = Fail,              /* Same as "Fail". */
        Reason = 0x800,        /* Eagle: failure mode and details (code
                                * mismatch, result mismatch, output mismatch,
                                * etc). */
        R = Reason,            /* Same as "Reason". */
        Time = 0x1000,         /* Eagle: setup/body/cleanup timing. */
        I = Time,              /* Same as "Time". */
        Exit = 0x2000,         /* Eagle: isolated process exit code detail. */
        X = Exit,              /* Same as "Exit". */
        StdOut = 0x4000,       /* Eagle: isolated process standard output. */
        O = StdOut,            /* Same as "StdOut". */
        StdErr = 0x8000,       /* Eagle: isolated process standard error. */
        D = StdErr,            /* Same as "StdErr". */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Enter = 0x10000,       /* Eagle: for use with "Debug", emit message
                                * upon entering into a script evaluation. */
        N = Enter,             /* Same as "Enter". */
        Leave = 0x20000,       /* Eagle: for use with "Debug", emit message
                                * upon leaving into a script evaluation. */
        A = Leave,             /* Same as "Leave". */
        Track = 0x40000,       /* Eagle: emit to the attached debugger, if
                                * any.  This flag never has an effect on the
                                * test results. */
        G = Track,             /* Same as "Track". */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Automatic = AutomaticWrite | AutomaticReturn | AutomaticLog,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Legacy = Pass | Body | Skip |
                 Start | Error, /* pbste */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Standard = Pass | Body | Skip |
                   Start | Error | Line, /* pbstel */

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Standard | Fail | Reason |
              Time | Exit | StdOut |
              StdErr | Enter | Leave |
              Track,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = (Automatic | All) & ~Start
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("da180459-c94a-4a04-9753-d6f1448964fa")]
    public enum TestPathType
    {
        None = 0x0,    /* unspecified. */
        Invalid = 0x1, /* invalid, do not use. */
        Library = 0x2, /* core library specific tests? */
        Plugins = 0x4, /* run plugin specific tests? */
        Tests = 0x8,   /* generic tests */

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("5f9a351b-67bb-48e1-ae17-15c4dd6e1c41")]
    public enum IsolationDetail
    {
        None = 0x0,
        Invalid = 0x1,
        Lowest = 0x2,
        Low = 0x4,
        Medium = 0x8,
        High = 0x10,
        Highest = 0x20,

        Default = Medium
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b60aaf1f-b159-4c6e-8e95-fe10eb40da0f")]
    public enum IsolationLevel
    {
        None = 0x0,
        Invalid = 0x1,
        Interpreter = 0x2,
        AppDomain = 0x4,
        AppDomainOrInterpreter = 0x8,
        Process = 0x10,
        Session = 0x20,
        Machine = 0x40,
#if ISOLATED_INTERPRETERS
        Maximum = AppDomain,
#else
        Maximum = AppDomainOrInterpreter,
#endif
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("879a3ffc-59dc-43ae-9bad-8c58ada1206b")]
    public enum OptionBehaviorFlags
    {
        None = 0x0,                       /* No special option behavior. */
        Invalid = 0x1,                    /* Invalid, do not use. */
        ValidateLookups = 0x2,            /* Use the Validate flag for all entity
                                           * lookups. */
        StrictLookups = 0x4,              /* Use the Strict flag for all entity
                                           * lookups. */
        ErrorOnEndOfOptions = 0x8,        /* Raise an error if an end-of-options
                                           * indicator is encountered when
                                           * unexpected. */
        StopOnEndOfOptions = 0x10,        /* Stop processing options if an
                                           * end-of-options indicator is encountered
                                           * when unexpected. */
        IgnoreOnEndOfOptions = 0x20,      /* Ignore end-of-options indicator if
                                           * encountered when unexpected. */
        SkipOnEndOfOptions = 0x40,        /* Skip to the next argument after an
                                           * unexpected end-of-options indicator
                                           * (i.e. assume a name/value pair).  This
                                           * flag has no effect unless the
                                           * IgnoreOnEndOfOptions flag is also set. */
        ErrorOnListOfOptions = 0x80,      /* Raise an error if an list-of-options
                                           * indicator is encountered when
                                           * unexpected. */
        StopOnListOfOptions = 0x100,      /* Stop processing options if an
                                           * list-of-options indicator is encountered
                                           * when unexpected. */
        IgnoreOnListOfOptions = 0x200,    /* Ignore list-of-options indicator if
                                           * encountered when unexpected. */
        SkipOnListOfOptions = 0x400,      /* Skip to the next argument after an
                                           * unexpected list-of-options indicator
                                           * (i.e. assume a name/value pair).  This
                                           * flag has no effect unless the
                                           * IgnoreOnListOfOptions flag is also set. */
        ErrorOnAmbiguousOption = 0x800,   /* Raise an error if an Ambiguous option is
                                           * encountered. */
        StopOnAmbiguousOption = 0x1000,   /* Stop processing options if an Ambiguous
                                           * one is encountered. */
        IgnoreOnAmbiguousOption = 0x2000, /* Ignore Ambiguous options if they are
                                           * encountered. */
        SkipOnAmbiguousOption = 0x4000,   /* Skip the next argument after an unknown
                                           * option (i.e. assume a name/value pair).
                                           * This flag has no effect unless the
                                           * IgnoreOnAmbiguous flag is also set. */
        ErrorOnUnknownOption = 0x8000,    /* Raise an error if an unknown option is
                                           * encountered. */
        StopOnUnknownOption = 0x10000,    /* Stop processing options if an unknown
                                           * one is encountered. */
        IgnoreOnUnknownOption = 0x20000,  /* Ignore unknown options if they are
                                           * encountered. */
        SkipOnUnknownOption = 0x40000,    /* Skip the next argument after an unknown
                                           * option (i.e. assume a name/value pair).
                                           * This flag has no effect unless the
                                           * IgnoreOnUnknown flag is also set. */
        ErrorOnNonOption = 0x80000,       /* Raise an error if a non-option is
                                           * encountered when unexpected (i.e. it
                                           * is not simply an option value). */
        StopOnNonOption = 0x100000,       /* Stop processing options if a non-option
                                           * is encountered when unexpected (i.e. it
                                           * is not simply an option value). */
        IgnoreOnNonOption = 0x200000,     /* Ignore the argument if a non-option
                                           * is encountered when unexpected (i.e. it
                                           * is not simply an option value). */
        SkipOnNonOption = 0x400000,       /* Skip to the next argument after a
                                           * non-option is encountered when
                                           * unexpected (i.e. it is not simply an
                                           * option value).  This flag has no effect
                                           * unless the IgnoreOnNonOption flag is
                                           * also set. */
        LastIsNonOption = 0x800000,       /* The last argument cannot be an option;
                                           * therefore, always stop if it is hit
                                           * prior to stopping for another reason. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the behavior needed by the CheckOptions method.
        //
        CheckOptions = StopOnEndOfOptions | IgnoreOnUnknownOption |
                       IgnoreOnAmbiguousOption | IgnoreOnNonOption,

        //
        // NOTE: This is the value for the old "strict" mode behavior.
        //
        Strict = ErrorOnEndOfOptions | ErrorOnUnknownOption |
                 ErrorOnAmbiguousOption | StopOnNonOption,

        //
        // NOTE: This is the value for the old "non-strict" mode behavior.
        //
        Default = StopOnEndOfOptions | StopOnUnknownOption |
                  StopOnAmbiguousOption | StopOnNonOption
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("64fb0580-d2c6-4e7e-a6f4-300144472520")]
    public enum OptionFlags : ulong
    {
        None = 0x0,                              // Regular name-only option, no special handling.
        Invalid = 0x1,                           // Invalid, do not use.
        Present = 0x2,                           // Option was found while parsing arguments.
        Strict = 0x4,                            // Use strict option value processing semantics?
        Verbose = 0x8,                           // Error messages should contain more details?
        NoCase = 0x10,                           // Option is not case-sensitive.
        Unsafe = 0x20,                           // Option is not allowed in "safe" interpreters.
        System = 0x40,                           // Option was added automatically by the system.
        AllowInteger = 0x80,                     // Integers are allowed for an enumeration option.

        ///////////////////////////////////////////////////////////////////////////////////////////

        MustHaveValue = 0x100,                   // Value must have value after name (string, int, etc).
        MustBeBoolean = 0x200,                   // Value must convert to bool via GetBoolean.
        MustBeSignedByte = 0x400,                // Value must convert to int via GetSignedByte2.
        MustBeByte = 0x800,                      // Value must convert to int via GetByte2.
        MustBeNarrowInteger = 0x1000,            // Value must convert to int via GetInteger2.
        MustBeUnsignedNarrowInteger = 0x2000,    // Value must convert to int via GetUnsignedInteger2.
        MustBeInteger = 0x4000,                  // Value must convert to int via GetInteger2.
        MustBeUnsignedInteger = 0x8000,          // Value must convert to int via GetUnsignedInteger2.
        MustBeWideInteger = 0x10000,             // Value must convert to wideInt (long) via GetWideInteger2.
        MustBeUnsignedWideInteger = 0x20000,     // Value must convert to wideInt (long) via GetWideInteger2.
        MustBeIndex = 0x40000,                   // Value must be an int or end[<+|-><int>].
        MustBeLevel = 0x80000,                   // Value must be an int or #<int>.
        MustBeReturnCode = 0x100000,             // Value must be a ReturnCode or int.
        MustBeEnum = 0x200000,                   // Value must convert to the specified Enum type.
        MustBeEnumList = 0x400000,               // Value must be an EnumList object.
        MustBeGuid = 0x800000,                   // Value must convert to System.Guid.
        MustBeDateTime = 0x1000000,              // Value must convert to System.DateTime.
        MustBeTimeSpan = 0x2000000,              // Value must convert to System.DateTime.
        MustBeList = 0x4000000,                  // SplitList on value must succeed.
        MustBeDictionary = 0x8000000,            // Must have an even number of list elements.
        MustBeMatchMode = 0x10000000,            // Value must be "exact", "glob", or "regexp".
        MustBeValue = 0x20000000,                // Value must convert to some value via GetValue.
        MustBeObject = 0x40000000,               // Value must be an opaque object handle.
        MustBeInterpreter = 0x80000000,          // Value must be an opaque interpreter handle.
        MustBeType = 0x100000000,                // Value must be a System.Type object.
        MustBeTypeList = 0x200000000,            // Value must be a TypeList object.
        MustBeAbsoluteUri = 0x400000000,         // Value must be a System.Uri object.
        MustBeVersion = 0x800000000,             // Value must be a System.Version object.
        MustBeReturnCodeList = 0x1000000000,     // Value must be a ReturnCodeList object.
        MustBeIdentifier = 0x2000000000,         // Value must be an IIdentifier object.
        MustBeAlias = 0x4000000000,              // Value must be an IAlias object.
        MustBeOption = 0x8000000000,             // Value must be an IOption object.
        MustBeAbsoluteNamespace = 0x10000000000, // Value must be an INamespace object.
        MustBeRelativeNamespace = 0x20000000000, // Value must be an INamespace object.
        MustBeCultureInfo = 0x40000000000,       // Value must be a CultureInfo object.

#if NATIVE && TCL
        MustBeTclInterpreter = 0x80000000000,    // Value must be a Tcl interpreter.
#endif

        MustBeSecureString = 0x100000000000,     // Value must be a SecureString object.
        MustBeEncoding = 0x200000000000,         // Value must be an Encoding object.
        MustBePlugin = 0x400000000000,           // Value must be an IPlugin object.
        MustBeExecute = 0x800000000000,          // Value must be an IExecute object.
        MustBeCallback = 0x1000000000000,        // Value must be an ICallback object.
        MustBeRuleSet = 0x2000000000000,         // value must be an IRuleSet object.

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Special option flags.
        //
        EndOfOptions = 0x4000000000000,          // This is the end-of-options marker, stop and do not process.
        ListOfOptions = 0x8000000000000,         // This is the list-of-options marker, stop and show the
                                                 // available options, returning an error.

        ///////////////////////////////////////////////////////////////////////////////////////////

        MustBeEnumMask = MustBeEnum | MustBeEnumList,

        MustBeMask = MustBeBoolean | MustBeSignedByte | MustBeByte |
                     MustBeNarrowInteger | MustBeUnsignedNarrowInteger | MustBeInteger |
                     MustBeUnsignedInteger | MustBeWideInteger | MustBeUnsignedWideInteger |
                     MustBeIndex | MustBeLevel | MustBeReturnCode |
                     MustBeEnum | MustBeEnumList | MustBeGuid |
                     MustBeDateTime | MustBeTimeSpan | MustBeList |
                     MustBeDictionary | MustBeMatchMode | MustBeValue |
                     MustBeObject | MustBeInterpreter | MustBeType |
                     MustBeTypeList | MustBeAbsoluteUri | MustBeVersion |
                     MustBeReturnCodeList | MustBeIdentifier | MustBeAlias |
                     MustBeOption | MustBeAbsoluteNamespace | MustBeRelativeNamespace |
                     MustBeCultureInfo |
#if NATIVE && TCL
                     MustBeTclInterpreter |
#endif
                     MustBeSecureString | MustBeEncoding | MustBePlugin |
                     MustBeExecute | MustBeCallback | MustBeRuleSet,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Composite flags provided for shorthand.
        //

        MustHaveCallbackValue = MustHaveValue | MustBeCallback,
        MustHaveExecuteValue = MustHaveValue | MustBeExecute,
        MustHavePluginValue = MustHaveValue | MustBePlugin,
        MustHaveEncodingValue = MustHaveValue | MustBeEncoding,
        MustHaveSecureStringValue = MustHaveValue | MustBeSecureString,
        MustHaveBooleanValue = MustHaveValue | MustBeBoolean,
        MustHaveSignedByteValue = MustHaveValue | MustBeSignedByte,
        MustHaveByteValue = MustHaveValue | MustBeByte,
        MustHaveNarrowIntegerValue = MustHaveValue | MustBeNarrowInteger,
        MustHaveUnsignedNarrowIntegerValue = MustHaveValue | MustBeUnsignedNarrowInteger,
        MustHaveIntegerValue = MustHaveValue | MustBeInteger,
        MustHaveUnsignedIntegerValue = MustHaveValue | MustBeUnsignedInteger,
        MustHaveWideIntegerValue = MustHaveValue | MustBeWideInteger,
        MustHaveUnsignedWideIntegerValue = MustHaveValue | MustBeUnsignedWideInteger,
        MustHaveLevelValue = MustHaveValue | MustBeLevel,
        MustHaveReturnCodeValue = MustHaveValue | MustBeReturnCode,
        MustHaveDateTimeValue = MustHaveValue | MustBeDateTime,
        MustHaveTimeSpanValue = MustHaveValue | MustBeTimeSpan,
        MustHaveEnumValue = AllowInteger /* COMPAT: Eagle beta. */ | MustHaveValue | MustBeEnum,
        MustHaveEnumListValue = AllowInteger | MustHaveValue | MustBeEnumList,
        MustHaveListValue = MustHaveValue | MustBeList,
        MustHaveDictionaryValue = MustHaveValue | MustBeDictionary,
        MustHaveMatchModeValue = MustHaveValue | MustBeMatchMode,
        MustHaveAnyValue = MustHaveValue | MustBeValue,
        MustHaveObjectValue = MustHaveValue | MustBeObject,
        MustHaveInterpreterValue = MustHaveValue | MustBeInterpreter,
        MustHaveTypeValue = MustHaveValue | MustBeType,
        MustHaveTypeListValue = MustHaveValue | MustBeTypeList,
        MustHaveAbsoluteUriValue = MustHaveValue | MustBeAbsoluteUri,
        MustHaveVersionValue = MustHaveValue | MustBeVersion,
        MustHaveReturnCodeListValue = MustHaveValue | MustBeReturnCodeList,
        MustHaveIdentifierValue = MustHaveValue | MustBeIdentifier,
        MustHaveAliasValue = MustHaveValue | MustBeAlias,
        MustHaveOptionValue = MustHaveValue | MustBeOption,
        MustHaveAbsoluteNamespaceValue = MustHaveValue | MustBeAbsoluteNamespace,
        MustHaveRelativeNamespaceValue = MustHaveValue | MustBeRelativeNamespace,
        MustHaveCultureInfoValue = MustHaveValue | MustBeCultureInfo,
        MustHaveGuidValue = MustHaveValue | MustBeGuid,
        MustHaveIndexValue = MustHaveValue | MustBeIndex,

#if NATIVE && TCL
        MustHaveTclInterpreterValue = MustHaveValue | MustBeTclInterpreter,
#endif

        MustHaveRuleSetValue = MustHaveValue | MustBeRuleSet,

        ///////////////////////////////////////////////////////////////////////////////////////////

        MatchOldValueType = 0x800000000000000,  // Old option value must be string or enum.
        Ignored = 0x1000000000000000,           // This option should not be processed.
        Disabled = 0x2000000000000000,          // This option is currently disabled (error).
        Unsupported = 0x4000000000000000,       // This option is not supported by this engine.
        Reserved = 0x8000000000000000           // Reserved, do not use.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c0fea691-2825-4dc4-940e-adfd35aed8ce")]
    public enum InterruptType
    {
        None = 0x0,
        Invalid = 0x1,
        Canceled = 0x2,
        Unwound = 0x4,
        Halted = 0x8,
        Global = 0x10,
        Local = 0x20,
        Deleted = 0x40
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7067e9bb-8903-4fc0-b712-83eb26fa5a02")]
    public enum BreakpointType : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved1 = 0x2,
        Reserved2 = 0x4,
        Reserved3 = 0x8,
        Reserved4 = 0x10,

        SingleStep = 0x20,
        MultipleStep = 0x40,

        Demand = 0x80,
        Token = 0x100,
        Identifier = 0x200,

        Cancel = 0x400, /* NOTE: Special due to Interpreter.Ready(). */
        Unwind = 0x800, /* NOTE: Special due to Interpreter.Ready(). */

        Error = 0x1000,
        Return = 0x2000,
        Test = 0x4000,
        Exit = 0x8000,

        Evaluate = 0x10000,   /* NOTE: Only used together with "Exit". */
        Substitute = 0x20000, /* NOTE: Only used together with "Exit". */

        BeforeText = 0x40000,
        AfterText = 0x80000,
        BeforeBackslash = 0x100000,
        AfterBackslash = 0x200000,
        BeforeUnknown = 0x400000,
        AfterUnknown = 0x800000,

        BeforeExpression = 0x1000000,
        AfterExpression = 0x2000000,
        BeforeIExecute = 0x4000000,
        AfterIExecute = 0x8000000,
        BeforeCommand = 0x10000000,
        AfterCommand = 0x20000000,
        BeforeSubCommand = 0x40000000,
        AfterSubCommand = 0x80000000,
        BeforeOperator = 0x100000000,
        AfterOperator = 0x200000000,
        BeforeFunction = 0x400000000,
        AfterFunction = 0x800000000,
        BeforeProcedure = 0x1000000000,
        AfterProcedure = 0x2000000000,
        BeforeProcedureBody = 0x4000000000,
        AfterProcedureBody = 0x8000000000,
        BeforeLambdaBody = 0x10000000000,
        AfterLambdaBody = 0x20000000000,
        BeforeVariableExist = 0x40000000000,         /* NOTE: Not yet implemented. */
        BeforeVariableCount = 0x80000000000,         /* NOTE: Not yet implemented. */
        BeforeVariableGet = 0x100000000000,
        BeforeVariableSet = 0x200000000000,
        BeforeVariableReset = 0x400000000000,
        BeforeVariableUnset = 0x800000000000,
        BeforeVariableAdd = 0x1000000000000,
        BeforeVariableArrayNames = 0x2000000000000,  /* NOTE: Not yet implemented. */
        BeforeVariableArrayValues = 0x4000000000000, /* NOTE: Not yet implemented. */
        BeforeVariableArrayGet = 0x8000000000000, /* NOTE: Not yet implemented. */

        BeforeInteractiveLoop = 0x10000000000000,
        AfterInteractiveLoop = 0x20000000000000,

        BeforeVariableCommon = BeforeVariableGet | BeforeVariableSet | BeforeVariableUnset,

        BeforeVariableMeta = BeforeVariableExist | BeforeVariableCount,

        BeforeVariableArray = BeforeVariableArrayNames | BeforeVariableArrayValues |
                              BeforeVariableArrayGet,

        BeforeVariableScalar = BeforeVariableCommon | BeforeVariableMeta,

        BeforeVariableSpecialRead = BeforeVariableExist | BeforeVariableCount,
        BeforeVariableSpecialWrite = BeforeVariableReset | BeforeVariableAdd,
        BeforeVariableSpecialAny = BeforeVariableSpecialRead | BeforeVariableSpecialWrite,

        BeforeVariable = BeforeVariableCommon | BeforeVariableArray | BeforeVariableMeta |
                         BeforeVariableSpecialAny,

        EngineCancel = Reserved1 | Cancel | Unwind,
        EngineCode = Reserved2 | Error | Return,
        EngineTest = Reserved3 | Test,
        EngineExit = Reserved4 | Exit | Evaluate | Substitute,

        Text = BeforeText | AfterText,
        Backslash = BeforeBackslash | AfterBackslash,
        Unknown = BeforeUnknown | AfterUnknown,
        Expression = BeforeExpression | AfterExpression,
        IExecute = BeforeIExecute | AfterIExecute,
        Command = BeforeCommand | AfterCommand,
        Operator = BeforeOperator | AfterOperator,
        Function = BeforeFunction | AfterFunction,
        Procedure = BeforeProcedure | AfterProcedure,
        ProcedureBody = BeforeProcedureBody | AfterProcedureBody,
        Lambda = BeforeLambdaBody | AfterLambdaBody,

        BeforeStep = BeforeText | BeforeBackslash | BeforeUnknown |
                     BeforeExpression | BeforeIExecute | BeforeCommand |
                     BeforeSubCommand | BeforeOperator | BeforeFunction |
                     BeforeProcedure | BeforeLambdaBody,

        AfterStep = AfterText | AfterBackslash | AfterUnknown |
                    AfterExpression | AfterIExecute | AfterCommand |
                    AfterSubCommand | AfterOperator | AfterFunction |
                    AfterProcedure | AfterLambdaBody,

        VariableStep = BeforeVariableExist | BeforeVariableGet | BeforeVariableSet |
                       BeforeVariableReset | BeforeVariableUnset | BeforeVariableAdd |
                       BeforeVariableArrayNames | BeforeVariableArrayValues,

        Common = Demand | Identifier | EngineCancel |
                 EngineTest | EngineExit | BeforeStep |
                 VariableStep,

        Standard = Common | Token,

        Ready = Cancel | Unwind,

        //
        // NOTE: No tokens, no expressions (too noisy).
        //
        Express = Common & ~(Expression | Operator | Function),

        //
        // NOTE: The default breakpoint types.
        //
        Default = Express,

        //
        // NOTE: All possible breakpoint types.
        //
        All = SingleStep | MultipleStep | Demand |
              Token | Identifier | EngineCancel |
              EngineCode | EngineTest | EngineExit |
              BeforeStep | AfterStep | VariableStep
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9b351955-a5dd-4306-b6c2-d510d332c353")]
    public enum MatchMode : ulong
    {
        None = 0x0,                 /* Do nothing. */
        Invalid = 0x1,              /* Invalid, do not use. */

        Callback = 0x2,             /* (application/plugin defined) */
        Exact = 0x4,                /* (e.g. [string equal], etc) */
        SubString = 0x8,            /* (e.g. "starts with", etc) */
        Glob = 0x10,                /* (e.g. [string match], etc) */
        RegExp = 0x20,              /* (e.g. [regexp], etc) */
        Integer = 0x40,             /* (e.g. [switch], etc) */
        Double = 0x80,              /* within DoubleEpsilon. */
        Decimal = 0x100,            /* within DecimalEpsilon. */

        // ...

        Substitute = 0x1000,        /* (e.g. [switch], etc) */
        Expression = 0x2000,        /* (e.g. [test1], [test2], etc) */
        Evaluate = 0x4000,          /* (e.g. [string map], etc) */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoCase = 0x10000,           /* Ignore case-sensitivity. */
        ForceCase = 0x20000,        /* Force case-sensitivity. */
        SubPattern = 0x40000,       /* (e.g. {a,b,c} syntax) */
        EmptySubPattern = 0x80000,  /* Permit empty sub-patterns. */
        SystemString = 0x100000,    /* Compare using ordinals. */
        PathString = 0x200000,      /* Compare as file paths. */
        TextToken = 0x400000,       /* Replace text token in pattern. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        TextTokenRaw = 0x1000000,   /* Replace text token verbatim. */
        TextTokenQuote = 0x2000000, /* Replace text token with (list) quoted string. */
        TextTokenList = 0x4000000,  /* Replace text token with string as (sole) list element. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        StopOnError = 0x10000000,   /* Meta: Stop on the first error hit, if any. */
        Any = 0x20000000,           /* Meta: Must match one, not all. */
        All = 0x40000000,           /* Meta: Must match all, not just one. */
        Include = 0x80000000,       /* Meta: Used for including item in collection. */
        Exclude = 0x100000000,      /* Meta: Used for excluding item in collection. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        TextTokenFlagsMask = TextTokenRaw | TextTokenQuote | TextTokenList,

        MetaFlagsMask = StopOnError | Any | All |
                        Include | Exclude,

        RuleSetMask = StopOnError | All,

        SimpleModeMask = Callback | Exact | SubString |
                         Glob | RegExp | Integer |
                         Decimal | Double,

        ComplexModeMask = Substitute | Expression | Evaluate,

        ModeMask = SimpleModeMask | ComplexModeMask,

        FlagsMask = NoCase | ForceCase | SubPattern |
                    EmptySubPattern | PathString |
                    SystemString | TextTokenFlagsMask |
                    MetaFlagsMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("040e4c44-a268-4d36-99d0-cc3c80373681")]
    public enum LevelFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Absolute = 0x2,  /* allow levels from the global frame inward. */
        Relative = 0x4,  /* allow levels from the current frame outward. */
        Invisible = 0x8, /* include otherwise invisible call frames. */

        //
        // NOTE: Tcl compatible call frame search semantics.
        //
        Default = Absolute | Relative,

        //
        // NOTE: Tcl compatible call frame search semantics
        //       with Eagle extensions (allows "invisible"
        //       call frames to be seen).
        //
        All = Absolute | Relative | Invisible
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
    [Flags()]
    [ObjectId("2d4b5503-ea8b-409f-bddd-a586c53ecb82")]
    public enum DbResultFormat
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,

        RawArray = 0x4,
        RawList = 0x8,
        Array = 0x10,
        List = 0x20,
        Dictionary = 0x40,
        NestedList = 0x80,
        NestedDictionary = 0x100,
        DataReader = 0x200,

        FormatMask = RawArray | RawList | Array |
                     List | Dictionary | NestedList |
                     NestedDictionary | DataReader,

        Default = Reserved | Array /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("8e169a10-f996-4ccf-add2-219e330ae1c4")]
    public enum DateTimeBehavior
    {
        None = 0,
        Ticks = 1,         /* Number of 100-nanosecond units since the start-of-time,
                            * 00:00:00.0000000 January 1st, 0001. */
        Seconds = 2,       /* Number of seconds since the standard Unix epoch,
                            * 00:00:00.0000000 January 1st, 1970. */
        ToString = 3,      /* Convert to a string using the specified or default
                            * format. */
        Default = ToString /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("21275a46-4292-4c42-ac70-9875dd6c2b2f")]
    public enum DbVariableFlags
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowNone = 0x2,
        AllowSelect = 0x4,
        AllowInsert = 0x8,
        AllowUpdate = 0x10,
        AllowDelete = 0x20,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowRead = AllowNone | AllowSelect,

        AllowWrite = AllowNone | AllowInsert | AllowUpdate |
                     AllowDelete,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowAll = AllowRead | AllowWrite,
        Default = AllowAll
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f517d99f-5f81-4663-8aec-0e071568d36d")]
    public enum DbExecuteType
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,

        NonQuery = 0x4,
        Scalar = 0x8,
        Reader = 0x10,
        ReaderAndCount = 0x20,

        TypeMask = NonQuery | Scalar | Reader |
                   ReaderAndCount,

        Default = Reserved | NonQuery /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // TODO: Add support for more database backends here.
    //
    [Flags()]
    [ObjectId("5c7e322d-2044-42f0-9545-7f05b80f14dc")]
    public enum DbConnectionType
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,

        Odbc = 0x4,
        OleDb = 0x8,
        Oracle = 0x10,
        Sql = 0x20,
        SqlCe = 0x40,
        SQLite = 0x80, /* COMPAT: Branding. */
        Other = 0x100,

        TypeMask = Odbc | OleDb | Oracle |
                   Sql | SqlCe | SQLite |
                   Other,

        Default = Reserved | Sql /* TODO: Good default? */
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("753458ab-2ef9-4934-83d7-f8a96effc9cc")]
    public enum IdentifierKind : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Interpreter = 0x2,
        PolicyData = 0x4,
        Policy = 0x8,
        TraceData = 0x10,
        Trace = 0x20,
        AnyIExecute = 0x40,
        CommandData = 0x80,
        Command = 0x100,
        HiddenCommand = 0x200,
        SubCommandData = 0x400,
        SubCommand = 0x800,
        ProcedureData = 0x1000,
        Procedure = 0x2000,
        HiddenProcedure = 0x4000,
        IExecute = 0x8000,
        HiddenIExecute = 0x10000,
        LambdaData = 0x20000,
        Lambda = 0x40000,
        OperatorData = 0x80000,
        Operator = 0x100000,
        FunctionData = 0x200000,
        Function = 0x400000,
        EnsembleData = 0x800000,
        Ensemble = 0x1000000,
        Variable = 0x2000000,
        CallFrame = 0x4000000,
        PackageData = 0x8000000,
        Package = 0x10000000,
        PluginData = 0x20000000,
        Plugin = 0x40000000,
        ObjectData = 0x80000000,
        Object = 0x100000000,
        ObjectTypeData = 0x200000000,
        ObjectType = 0x400000000,
        Option = 0x800000000,
        NativeModule = 0x1000000000,
        NativeDelegate = 0x2000000000,
        HostData = 0x4000000000,
        Host = 0x8000000000,
        AliasData = 0x10000000000,
        Alias = 0x20000000000,
        DelegateData = 0x40000000000,
        Delegate = 0x80000000000,
        SubDelegate = 0x100000000000,
        Callback = 0x200000000000,
        Resolve = 0x400000000000,
        ResolveData = 0x800000000000,
        ClockData = 0x1000000000000,
        Script = 0x2000000000000,
        ScriptBuilder = 0x4000000000000,
        NamespaceData = 0x8000000000000,
        Namespace = 0x10000000000000,
        InteractiveLoopData = 0x20000000000000,
        ShellCallbackData = 0x40000000000000,
        KeyPair = 0x80000000000000,
        Certificate = 0x100000000000000,
        KeyRing = 0x200000000000000,
        UpdateData = 0x400000000000000,
        Channel = 0x800000000000000,
        Path = 0x1000000000000000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a7a543b2-f7a1-408a-84ec-06bcf541c6c8")]
    public enum StreamFlags
    {
        None = 0x0,
        Invalid = 0x1,              /* Invalid, do not use. */
        PreventClose = 0x2,         /* When set, reject all Close and Dispose requests. */
        SawCarriageReturn = 0x4,    /* A carriage-return has been seen while processing input. */
        NeedLineFeed = 0x8,         /* A line-feed is needed while processing input. */
        UseAnyEndOfLineChar = 0x10, /* Any end-of-line character can terminate an input line. */
        KeepEndOfLineChars = 0x20,  /* Keep end-of-line characters from input line. */
        Socket = 0x40,              /* The stream is a socket. */
        Client = 0x80,              /* The stream contains a client socket. */
        Server = 0x100,             /* The stream contains a server socket. */
        Listen = 0x200,             /* The stream contains a listen socket. */
        NeedBuffer = 0x400,         /* Enable buffering when adding channels. */

        ListenSocket = Socket | Listen,
        ClientSocket = Socket | Client,
        ServerSocket = PreventClose | Socket | Server
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("0e36e577-a722-4a9b-bbe0-08340cab5bd3")]
    public enum FileSearchFlags
    {
        None = 0x0,                 /* no special handling. */
        Invalid = 0x1,              /* invalid, do not use. */
        SpecificPath = 0x2,         /* check the specified name verbatim (this only
                                     * applies if the name is fully qualified). */
        Mapped = 0x4,               /* search interpreter path map (first). */
        AutoSourcePath = 0x8,       /* search the auto_source_path (second). */
        Current = 0x10,             /* search the current directory (third). */
        User = 0x20,                /* search user-specific locations. */
        Externals = 0x40,           /* search the externals directory. */
        Application = 0x80,         /* search application-specific locations. */
        ApplicationBase = 0x100,    /* search application base directory locations. */
        Vendor = 0x200,             /* also search vendor locations. */
        Strict = 0x400,             /* return null if no existing file is found. */
        DirectorySeparator = 0x800, /* normalize directory separators. */
        Unix = 0x1000,              /* use Unix directory separators. */
        DirectoryLocation = 0x2000, /* allow candidate location to be a directory. */
        FileLocation = 0x4000,      /* allow candidate location to be a file. */
        FullPath = 0x8000,          /* search using the path with its directory
                                     * information intact.  if an absolute path
                                     * specified, all candidate locations will be
                                     * rejected unless one of the following flags
                                     * is also used: StripBasePath FileNameOnly */
        StripBasePath = 0x10000,    /* when an absolute path is specified, attempt
                                     * to remove the base path portion for the
                                     * candidate location being searched.  this
                                     * flag has absolutely, positively no effect
                                     * on non-absolute paths. */
        TailOnly = 0x20000,         /* search using only the tail portion of the
                                     * specified path.  this flag is legal for all
                                     * path types and permits all non-tail parts
                                     * of the path to be removed before checking a
                                     * candidate location. */
        Verbose = 0x40000,          /* show each directory / file checked. */
        Isolated = 0x80000,         /* search is being conducted from an isolated
                                     * AppDomain from the perspective of the host
                                     * interpreter.  this may have no effect. */

        PathMask = FullPath | StripBasePath | TailOnly,

        Standard = SpecificPath | Mapped | AutoSourcePath |
                   User | Externals | Application |
                   ApplicationBase | Vendor | DirectoryLocation |
                   FileLocation | PathMask,

        StandardAndStrict = Standard | Strict,

        Default = PathMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("021d5251-a399-4275-98c8-14d543020237")]
    public enum StreamDirection
    {
        None = 0x0,
        Invalid = 0x1,
        Input = 0x2,
        Output = 0x4,
        Both = Input | Output
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("dcc9ce82-31c2-4366-bcbd-be8e049d0c47")]
    public enum StreamTranslation
    {
        //
        // NOTE: These names are referred to directly from scripts, please do not change.
        //
        binary = 0,
        lf = 1,
        cr = 2,
        crlf = 3,
        platform = 4,
        auto = 5,
        environment = 6,
        protocol = 7 /* cr/lf is required by numerous Internet protocols */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("721b5d85-d2de-42df-9f66-440f9efe2c97")]
    public enum NamespaceFlags
    {
        None = 0x0,
        Invalid = 0x1,

        Qualified = 0x2,
        Absolute = 0x4,
        Global = 0x8,

        Wildcard = 0x10,

        Command = 0x20,
        Variable = 0x40,

        QualifierMask = Qualified | Absolute | Global,
        PatternMask = Wildcard,
        EntityMask = Command | Variable,

        SplitNameMask = QualifierMask | PatternMask | EntityMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ba1fcf45-9ce8-4d7a-9158-fb5c4f345bd2")]
    public enum ScriptDataFlags
    {
        None = 0x0,                        /* No special handling. */
        Invalid = 0x1,                     /* Invalid, do not use. */
        UseSafeInterpreter = 0x2,          /* Create and/or use a "safe"
                                            * interpreter. */
        UseIsolatedInterpreter = 0x4,      /* Create and/or use an interpreter
                                            * in its own application domain. */
        UseStaticDataOnly = 0x8,           /* The script only contains static
                                            * data that uses the [set] command.
                                            */
        FastStaticDataOnly = 0x10,         /* Loading is being done in a time
                                            * critical context and shortcuts
                                            * should be taken as needed. */
        CopyScalars = 0x20,                /* Scalar variables should be copied
                                            * to the resulting dictionary. */
        CopyArrays = 0x40,                 /* Array variables should be copied
                                            * to the resulting dictionary. */
        ExistingOnly = 0x80,               /* When merging the settings, only
                                            * consider those that already
                                            * existed. */
        ErrorOnScalar = 0x100,             /* When this flag is set and the
                                            * associated "copy" flag is not set,
                                            * an error will be returned if a
                                            * scalar variable is found.
                                            */
        ErrorOnArray = 0x200,              /* When this flag is set and the
                                            * associated "copy" flag is not set,
                                            * an error will be returned if an
                                            * array variable is found. */
        DisableSecurity = 0x400,           /* Prevent the "Security" flag from
                                            * being set in the interpreter
                                            * initialization flags. */
        ForceTrustedUri = 0x800,           /* Make sure to trust the public
                                            * key(s) that are associated with
                                            * the software update SSL
                                            * certificate(s) prior to attempting
                                            * to evaluate the settings file.
                                            * This will use save/restore
                                            * semantics on the associated trust
                                            * state. */
        NoCreateInterpreter = 0x1000,      /* Use an existing interpreter, do
                                            * not create a new one. */
        CacheSafeInterpreter = 0x2000,     /* Use existing cached "safe"
                                            * interpreter, if any, creating and
                                            * storing it when needed. */
        CacheIsolatedInterpreter = 0x4000, /* Use existing cached "safe"
                                            * interpreter, if any, creating and
                                            * storing it when needed. */
        DisableHost = 0x8000,              /* Skip creating an interpreter host.
                                            */
        NoConsoleHost = 0x10000,           /* Do not use the console host. */
#if ISOLATED_PLUGINS
        NoIsolatedPlugins = 0x20000,       /* Prevent plugins from being loaded
                                            * in isolated application domains. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        NoPluginUpdateCheck = 0x40000,     /* Skip update checks when loading
                                            * plugins. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoPluginIsolatedOnly = 0x80000,    /* Prevent plugins from setting the
                                            * IsolatedOnly plugin flag. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoStartup = 0x100000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved1 = 0x200000,              /* Reserved value, do not use. */
        Reserved2 = 0x400000,              /* Reserved value, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CopyVariables = CopyScalars | CopyArrays,
        CopyPreExistingVariablesOnly = CopyVariables | ExistingOnly,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeInterpreterAndStaticDataOnly = UseSafeInterpreter | UseStaticDataOnly,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SecurityPackage = DisableSecurity | NoConsoleHost |
#if ISOLATED_PLUGINS
                          NoIsolatedPlugins |
#if SHELL
                          NoPluginUpdateCheck |
#endif
                          NoPluginIsolatedOnly |
#endif
                          None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForKeyRingLoader = CopyPreExistingVariablesOnly |
                           SafeInterpreterAndStaticDataOnly |
                           SecurityPackage,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Lowest = Reserved1 | CopyVariables,
        Low = Lowest | ExistingOnly | NoStartup,
        Medium = Low | UseSafeInterpreter,
        High = Medium | UseStaticDataOnly,
        Highest = High | UseIsolatedInterpreter,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Minimum = Reserved2 | Low, /* COMPAT: Eagle beta. */
        Maximum = Reserved2 | High /* COMPAT: Eagle beta. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /* [Flags()] */
    [ObjectId("747766b6-abe9-4c44-890e-94585ea3cbd2")]
    public enum ReturnCode : int /* COMPAT: Tcl. */
    {
        Invalid = -16, /* NOTE: This return code is reserved as "invalid",
                        *       please do not use it. */

#if false
        ConvertNoSpace = -4,   /* COMPAT: Tcl, these values can be returned by */
        ConvertUnknown = -3,   /*         Tcl_ExternalToUtf and Tcl_UtfToExternal in */
        ConvertSyntax = -2,    /*         addition to TCL_OK. */
        ConvertMultiByte = -1,
#endif

        Ok = 0,        /* COMPAT: Tcl, these five "standard" return code values */
        Error = 1,     /*         are straight from "generic/tcl.h" and are used */
        Return = 2,    /*         by the TclWrapper as well as script engine */
        Break = 3,     /*         itself, please do not change them. */
        Continue = 4,
        WhatIf = 5,    /* COMPAT: Eagle beta 45+ only. */
        Exception = 6, /* COMPAT: Eagle beta 46+ only. */

        //
        // NOTE: If either of these bits are set, it indicates a custom
        //       return code is being provided by the user command.
        //
        CustomOk = 0x20000000,    /* COMPAT: HRESULT. */
        CustomError = 0x40000000, /* COMPAT: HRESULT. */

        //
        // NOTE: The high-bit is reserved, please do not use it.
        //
        Reserved = unchecked((int)0x80000000) /* COMPAT: HRESULT. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f185d6de-8e60-4c80-90e8-7569d5f02b23")]
    public enum FilePermission
    {
        None = 0x0,
        Execute = 0x1, /* COMPAT: Unix. */
        Write = 0x2,   /* COMPAT: Unix. */
        Read = 0x4,    /* COMPAT: Unix. */
        Invalid = 0x4000000,
        Exists = 0x8000000,
        NotExists = 0x10000000,
        Directory = 0x20000000,
        File = 0x40000000,
        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2752be1d-e3a3-42c7-992a-ea249b0161a6")]
    public enum MapOpenAccess /* COMPAT: C + POSIX */
    {
        //
        // NOTE: These names are referred to directly from scripts, please do not change.
        //
        Default = RdOnly,                 /* default handling */
        RdOnly = 0x0,                     /* open for reading only */
        WrOnly = 0x1,                     /* open for writing only */
        RdWr = 0x2,                       /* open for reading and writing */
        Append = 0x8,                     /* writes done at eof */
        SeekToEof = 0x10,                 /* seek to eof at open */
        Creat = 0x100,                    /* create and open file */
        Trunc = 0x200,                    /* open and truncate */
        Excl = 0x400,                     /* open only if file doesn't already exist */
        R = RdOnly,                       /* open for reading only; file must already exist; this is
                                           * the default */
        RPlus = RdWr,                     /* open for reading and writing; file must already exist */
        W = WrOnly | Creat | Trunc,       /* open for writing only; truncate if it exists, otherwise,
                                           * create new file */
        WPlus = RdWr | Creat | Trunc,     /* open for reading and writing; truncate if it exists;
                                           * otherwise, create new file */
        A = WrOnly | Creat | Append,      /* open for writing only; if it doesn't exist, create new
                                           * file, writes done at eof */
        APlus = RdWr | Creat | SeekToEof, /* open for reading and writing; if it doesn't exist, create
                                           * new file, initial position is eof */
        RdWrMask = RdOnly | WrOnly | RdWr /* mask of possible read/write modes */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6fad8db8-ecf0-4662-a6a1-c2930ff3acea")]
    public enum MapSeekOrigin
    {
        //
        // NOTE: These names are referred to directly from scripts, please do not change.
        //
        Begin = SeekOrigin.Begin,
        Start = SeekOrigin.Begin,
        Current = SeekOrigin.Current,
        End = SeekOrigin.End
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f83e7bd1-a0b2-4f5c-99d3-a5a315735031")]
    public enum SettingFlags
    {
        None = 0x0,          /* No special handling. */
        Invalid = 0x1,       /* Invalid, do not use. */
        CurrentUser = 0x2,   /* Enable reading and writing of per-user
                              * settings. */
        LocalMachine = 0x4,  /* Enable reading and writing of per-machine
                              * settings. */
        AnySecurity = 0x8,   /* Search all groups of settings, even when they
                              * may not be applicable to the current user. */
        UserSecurity = 0x10, /* Check the permissions of the current user to
                              * select which group of settings will be read
                              * or written by the current operation. */
        LowSecurity = 0x20,  /* Only read and/or write setting values within
                              * the group of settings that are writable by
                              * all users. */
        HighSecurity = 0x40, /* Only read and/or write setting values within
                              * the group of settings that are writable by
                              * users with administrator access. */
        Verbose = 0x80,      /* Enable detailed error messages. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = CurrentUser | LocalMachine | AnySecurity |
              UserSecurity | LowSecurity | HighSecurity,

        Legacy = CurrentUser | LocalMachine | HighSecurity,

        Default = Legacy
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("12692407-91a1-44ce-af30-5d29bdd8a265")]
    public enum ValueFlags : ulong
    {
        //
        // NOTE: The various flags in this enumeration
        //       control the behavior of the Value class
        //       when it attempts to interpret a given
        //       string as a number (or other supported
        //       type).
        //
        None = 0x0,
        Invalid = 0x1,

        //
        // NOTE: The available number bases.
        //
        BinaryRadix = 0x2,
        OctalRadix = 0x4,
        DecimalRadix = 0x8,
        HexadecimalRadix = 0x10,

        //
        // NOTE: The available integral types.
        //
        Boolean = 0x20,
        Byte = 0x40,
        Character = 0x80,
        String = 0x100,
        List = 0x200,
        NarrowInteger = 0x400,
        Integer = 0x800,
        WideInteger = 0x1000,

        //
        // NOTE: The available enumerated types.  These
        //       are considered to be distinct and are
        //       treated specially from the [integral]
        //       types above, even though they actually
        //       are integral types (primarily because
        //       the "fallback" semantics of the Value
        //       class functions are not designed to
        //       automatically convert a string to a
        //       value of an enumerated type, e.g. via
        //       GetNumber).
        //
        ReturnCode = 0x2000,
        MatchMode = 0x4000,

        //
        // NOTE: The available fixed-point types.
        //
        Decimal = 0x8000,

        //
        // NOTE: The available floating-point types.
        //
        Single = 0x10000,
        Double = 0x20000,

        //
        // NOTE: Some kind of basic numeric value.  This
        //       includes all booleans, integers (both signed
        //       and/or unsigned), fixed-point, and floating
        //       point.
        //
        Number = 0x40000,

        //
        // NOTE: The available miscellaneous types.
        //
        DateTime = 0x80000,
        DateTimeFormat = 0x100000, /* fixup DateTime format string */
        TimeSpan = 0x200000,
        Guid = 0x400000,
        Object = 0x800000, /* opaque object handle */

        //
        // NOTE: For use by Value.GetIndex only.
        //
        NamedIndex = 0x1000000,
        WithOffset = 0x2000000,

        //
        // NOTE: For use with Value.GetNestedObject and Value.GetNestedMember only.
        //
        StopOnNullType = 0x4000000,
        StopOnNullObject = 0x8000000,
        StopOnNullMember = 0x10000000,

        StopOnNullMask = StopOnNullType | StopOnNullObject | StopOnNullMember,

        //
        // NOTE: Extra flags to control the type conversion
        //       behavior of various methods of the Value
        //       class.
        //
        Fast = 0x20000000,
        AllowInteger = 0x40000000,
        IgnoreLeading = 0x80000000, /* NOT USED */
        IgnoreTrailing = 0x100000000, /* NOT USED */
        Strict = 0x200000000,
        Verbose = 0x400000000,
        ShowName = 0x800000000,
        FullName = 0x1000000000,
        NoCase = 0x2000000000,
        NoNested = 0x4000000000,
        NoNamespace = 0x8000000000,
        NoAssembly = 0x10000000000,
        NoException = 0x20000000000,
        NoComObject = 0x40000000000,
        NoDefaultGetType = 0x80000000000,
        AllowBooleanString = 0x100000000000,
        AllowNull = 0x200000000000,
        SkipTypeGetType = 0x400000000000,
        AllowProxyGetType = 0x800000000000,
        ForceProxyGetType = 0x1000000000000,
        ManualProxyGetType = 0x2000000000000,
        NullForProxyType = 0x4000000000000,
        AllGetTypeErrors = 0x8000000000000,
        OneParameterGetType = 0x10000000000000,

        //
        // NOTE: Extra informational flags to indicate when a signed
        //       or unsigned integral number is being parsed.  These
        //       flags are not used in calls to parse the "default"
        //       signedness for a base integral type.
        //
        Signed = 0x20000000000000,
        Unsigned = 0x40000000000000,

        //
        // NOTE: Extra flags to control whether signed and/or
        //       unsigned variations are allowed when processing
        //       integral numbers in the decimal radix.
        //
        DefaultSignedness = 0x80000000000000, /* decimal radix only */
        NonDefaultSignedness = 0x100000000000000, /* decimal radix only */

        AllowRadixSign = 0x200000000000000, /* any radix w/prefix */
        AllowSigned = 0x400000000000000, /* decimal radix only */
        AllowUnsigned = 0x800000000000000, /* decimal radix only */

        //
        // NOTE: When dealing with integer values, allow use of
        //       the sign bit (e.g. 4294967295 would be allowed
        //       for a 32-bit integer value).
        //
        WidenToUnsigned = 0x1000000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SignednessMask = DefaultSignedness | AllowSigned | AllowUnsigned,

        //
        // NOTE: The default value flags used by [object search]
        //       when calling GetType.
        //
        ObjectSearch = ShowName,

        //
        // NOTE: Either an integer or a wide integer.
        //
        IntegerOrWideInteger = Integer | WideInteger,

        //
        // NOTE: Used to mask off characters.
        //
        NonCharacterMask = ~Character,

        //
        // NOTE: Used to mask off things that are not to be
        //       considered as "real numbers".
        //
        NonRealMask = ~(Boolean | Character),

        //
        // NOTE: Allow booleans and their textual strings.
        //
        TclBoolean = Boolean | AllowBooleanString,

        //
        // NOTE: These are the types (more-or-less) handled
        //       by the GetNumeric method.
        //
        NumericMask = TclBoolean | Byte | NarrowInteger |
                      Integer | WideInteger | Decimal |
                      Single | Double | Number,

        //
        // NOTE: Useful combinations of the above flags,
        //       some are used by the engine and some are
        //       provided for convenience.
        //
        AnyRadix = BinaryRadix | OctalRadix | DecimalRadix |
                   HexadecimalRadix,

        AnyRadixAnySign = AnyRadix | AllowRadixSign,

        AnySignedness = AllowSigned | AllowUnsigned,

        AnyIntegral = TclBoolean | Byte | Character |
                      NarrowInteger | Integer | WideInteger,

        AnyIntegralNonCharacter = AnyIntegral & NonCharacterMask,

        AnyBoolean = AnyIntegralAnyRadix,
        AnyStrictBoolean = AnyIntegralAnyRadix | Strict,

        AnyByte = AnyIntegralAnyRadix,

        AnyCharacter = AnyIntegralAnyRadix,

        AnyNarrowInteger = AnyIntegralAnyRadix,

        AnyInteger = AnyIntegralAnyRadix,

        AnyWideInteger = AnyIntegralAnyRadix,

        AnyFixedPoint = AnyDecimal,

        AnyDecimal = Decimal | AnyRadixAnySign,

        AnyDouble = Double | AnyRadixAnySign,

        AnyFloatingPoint = Single | Double,

        AnyReal = AnyNumber & NonRealMask,

        AnyNumber = AnyIntegral | AnyFixedPoint | AnyFloatingPoint | Number,

        AnyIntegralAnyRadix = AnyRadixAnySign | AnyIntegral | DefaultSignedness,
        AnyIntegralAnyRadixAnySignedness = AnyRadixAnySign | AnyIntegral | AnySignedness,

        AnyRealAnyRadix = AnyRadixAnySign | AnyReal | DefaultSignedness,
        AnyRealAnyRadixAnySignedness = AnyRadixAnySign | AnyReal | AnySignedness,

        AnyNumberAnyRadix = AnyRadixAnySign | AnyNumber | DefaultSignedness,
        AnyNumberAnyRadixAnySignedness = AnyRadixAnySign | AnyNumber | AnySignedness,
        AnyNumberAnyRadixFast = AnyNumberAnyRadix | Fast,

        AnyDateTime = AnyIntegralAnyRadix | DateTime | DateTimeFormat,
        AnyStrictDateTime = AnyDateTime | Strict,

        AnyTimeSpan = AnyIntegralAnyRadix | TimeSpan,
        AnyStrictTimeSpan = AnyTimeSpan | Strict,

        AnyReturnCode = AnyIntegralAnyRadix | ReturnCode,

        AnyMatchMode = AnyIntegralAnyRadix | MatchMode,
        AnyStrictMatchMode = AnyMatchMode | Strict,

        AnyVariant = AnyRadixAnySign | AnyNumber | DateTime |
                     DateTimeFormat | TimeSpan | Object |
                     DefaultSignedness,

        AnyVariantAnySignedness = AnyRadixAnySign | AnyNumber | DateTime |
                                  DateTimeFormat | TimeSpan | AnySignedness,

        AnyIndex = AnyRadixAnySign | TclBoolean | Integer |
                   NamedIndex | WithOffset | DefaultSignedness,

        AnyIndexAnySignedness = AnyRadixAnySign | TclBoolean | Integer |
                                NamedIndex | WithOffset | AnySignedness,

        AnyNonCharacter = Any & NonCharacterMask,

        Any = AnyVariant | String | Guid,
        AnyStrict = Any | Strict
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("19c16abe-025f-44cb-af23-10cb924f024d")]
    public enum ParseError
    {
        Success = 0,
        ExtraAfterCloseQuote = 1,
        ExtraAfterCloseBrace = 2,
        MissingBrace = 3,
        MissingBracket = 4,
        MissingParenthesis = 5,
        MissingQuote = 6,
        MissingVariableBrace = 7,
        Syntax = 8,
        BadNumber = 9
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("120473c9-ca35-4446-99a8-f7b5c1be6e31")]
    public enum TokenType
    {
        None = 0x0,
        Invalid = 0x1,
        Word = 0x2,
        SimpleWord = 0x4,
        Text = 0x8,
        Backslash = 0x10,
        Command = 0x20,
        Variable = 0x40,
        SubExpression = 0x80,
        Operator = 0x100,
        Function = 0x200,
        Separator = 0x400,
        NameOnly = 0x800,

        VariableNameOnly = Variable | NameOnly
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("4acc4d6d-ab07-498c-b7db-07781da4e427")]
    public enum TokenFlags
    {
        None = 0x0,
        Invalid = 0x1,

#if DEBUGGER
        Breakpoint = 0x2,
#endif

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2ca81723-b85d-4bec-96f9-817c21e5cec0")]
    public enum TokenSyntaxType
    {
        None = 0x0,
        Invalid = 0x1,
        WhiteSpace = 0x2,       /* the token is whitespace.  not currently supported by the parser. */
        Comment = 0x4,          /* the token is a comment.  not currently supported by the parser. */
        CommandName = 0x8,      /* the token is the first argument to a command (i.e. the command name). */
        Argument = 0x10,        /* the token is an argument to a command. */
        Backslash = 0x20,       /* the token is a backslash substitution. */
        Command = 0x40,         /* the token is a nested command substitution. */
        Variable = 0x80,        /* the token is a variable substitution. */
        Block = 0x100,          /* the token is a block surrounded by braces. */
        StringLiteral = 0x200,  /* the token is a quoted string. */
        NumericLiteral = 0x400, /* the token is a numeric literal. */
        Expression = 0x800,     /* the token is an [expr] expression. */
        Operator = 0x1000,      /* the token is an [expr] operator. */
        Function = 0x2000       /* the token is an [expr] function. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("fa80583c-ff5a-487a-b874-f577ab6e4c53")]
    public enum ListElementFlags
    {
        None = 0x0,
        Invalid = 0x1,
        DontUseBraces = 0x2,   // prevents using braces for quoting list elements.
        UseBraces = 0x4,       // allows using braces for quoting list elements (unless DontUseBraces).
        BracesUnmatched = 0x8, // indicates that there are unmatched braces in the string to convert.
        DontQuoteHash = 0x10   // backward compatibility prior to fixing the quoting of '#' characters.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("0b70b6d4-8cff-4650-9fac-ba1fa68725b1")]
    public enum ReadyFlags
    {
        None = 0x0,                    /* No special handling, not
                                        * recommended. */
        Invalid = 0x1,                 /* Invalid, do not use. */
        Reserved = 0x2,                /* Reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////

        Disabled = 0x4,                /* Skip all interpreter readiness
                                        * checks. */
        NoFlags = 0x8,                 /* Skip adding readiness flags from
                                        * interpreter. */
        Limited = 0x10,                /* Perform full check only every N
                                        * checks. */
        ExitedOk = 0x20,               /* Return 'Ok' even if interpreter
                                        * has exited. */
        DeletedOk = 0x40,              /* Return 'Ok' even if interpreter
                                        * is deleted. */
        NoCallback = 0x80,             /* Skip user-defined callback. */
        NoStack = 0x100,               /* Skip native stack space checks. */
        NoPoolStack = 0x200,           /* Skip native stack space checks
                                        * for thread pool threads. */
        CheckStack = 0x400,            /* Check native stack space. */
        ForceStack = 0x800,            /* Force checking native stack
                                        * space. */
        ForcePoolStack = 0x1000,       /* Check native stack space for
                                        * thread pool threads as well. */
        CheckLevels = 0x2000,          /* Consider the maximum levels
                                        * seen. */
        StackOnly = 0x4000,            /* Skip all checks except native
                                        * stack space. */
        NoCancel = 0x8000,             /* Skip script cancellation
                                        * checking. */
        NoGlobalCancel = 0x10000,      /* Only consider the thread-local
                                        * script cancellation flags. */
        NoGlobalResetCancel = 0x20000, /* Do not reset the global script
                                        * cancellation flag when it is
                                        * tripped by the current thread.
                                        */
        NoHalt = 0x40000,              /* Skip halt checking. */

#if DEBUGGER
        NoBreakpoint = 0x80000,        /* Skip checking for any script
                                        * breakpoints. */
#endif

        ///////////////////////////////////////////////////////////////////////

        ForPublic = 0x100000,          /* Being called from the public
                                        * method. */
        ForParser = 0x200000,          /* Being called from the parser. */
        ForEngine = 0x400000,          /* Being called from the engine. */
        ForEventManager = 0x800000,    /* Being called from the event
                                        * manager. */

#if NATIVE && TCL
        ForTclWrapper = 0x1000000,     /* Being called from the Tcl
                                        * wrapper. */
#endif

#if DEBUGGER
        ForDebugger = 0x2000000,       /* Being called from the script
                                        * debugger. */
#endif

        ForTest = 0x4000000,           /* Being called by the test suite. */
        ForObject = 0x8000000,         /* Being called from within the opaque
                                        * object handle management subsystem. */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default flags used to enable native stack
        //       space checking.
        //
        ForStack = CheckStack | ForcePoolStack | CheckLevels,

        ///////////////////////////////////////////////////////////////////////

        ViaPublic = Default | ForPublic | ForStack | ForceStack,
        ViaParser = Default | ForParser | ForStack | Limited,
        ViaEngine = Default | ForEngine | Limited,
        ViaEventManager = Default | ForEventManager,

#if NATIVE && TCL
        ViaTclWrapper = Default | ForTclWrapper | ForStack,
#endif

#if DEBUGGER
        ViaDebugger = Default | ForDebugger,
#endif

        ViaTest = Default | ForTest,
        ViaObject = Default | ForObject,

        ///////////////////////////////////////////////////////////////////////

        Unknown = None, /* This is the value used when the ready flags
                         * cannot be obtained from the interpreter for
                         * some reason (e.g. locking failure). */

        ///////////////////////////////////////////////////////////////////////

        Default = None  /* These are the flags that are always used by
                         * most callers (i.e. see the "Via*" values
                         * above). */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("85f9cb1e-c237-4a3d-8775-3b5a0700bc8c")]
    public enum InterpreterFlags : ulong
    {
        None = 0x0,                                /* No flags. */
        Invalid = 0x1,                             /* Invalid, do not use. */
        NoBackgroundError = 0x2,                   /* Disable the default background error
                                                    * handling. */
        NoPostProcess = 0x4,                       /* Skip post-processing of returned
                                                    * variable values. */
        TraceInput = 0x8,                          /* Trace interactive input processing.
                                                    * Used by the interactive loop. */
        TraceResult = 0x10,                        /* Trace engine result processing. */
        TraceToHost = 0x20,                        /* Redirect trace listener output to
                                                    * the interpreter host.  Currently,
                                                    * this is used by external plugins
                                                    * only. */
        TraceStack = 0x40,                         /* Attempt to emit a full stack trace
                                                    * if the DebugOps.Complain method is
                                                    * called.  In the future, other trace
                                                    * listener output may be impacted by
                                                    * this flag as well.
                                                    */
        TraceInteractiveCommand = 0x80,            /* Trace interactive commands.  Used
                                                    * by the interactive loop. */
        TracePackageIndex = 0x100,                 /* Trace package index file handling.
                                                    * This is used by the package index
                                                    * processing subsystem. */
        VerbosePackageIndex = 0x200,               /* Verbose trace output from package
                                                    * index file handling.  This is used
                                                    * by the package index processing
                                                    * subsystem. */
        WriteTestData = 0x400,                     /* For [test1]/[test2], write the test
                                                    * data to the host. */
        NoReturnTestData = 0x800,                  /* For [test1]/[test2], do not return
                                                    * the test data. */
        NoLogTestData = 0x1000,                    /* For [test1]/[test2], do not log
                                                    * the test data. */
        NoTrackTestData = 0x2000,                  /* For [test1]/[test2], do not emit
                                                    * the test data to the attached
                                                    * debugger. */
        FinallyResetCancel = 0x4000,               /* Call Engine.ResetCancel prior to
                                                    * evaluating finally blocks in the
                                                    * [try] command. */
        FinallyRestoreCancel = 0x8000,             /* Call Engine.CancelEvaluate after
                                                    * evaluating the finally block in the
                                                    * [try] command, if necessary. */
        FinallyResetExit = 0x10000,                /* Save/reset the Exit property prior
                                                    * to evaluating finally blocks in the
                                                    * [try] command. */
        FinallyRestoreExit = 0x20000,              /* Restore the Exit property after
                                                    * evaluating the finally block in the
                                                    * [try] command, if necessary. */
        NoPackageFallback = 0x40000,               /* Skip using the configured package
                                                    * fallback delegate to locate any
                                                    * packages that fail to load using
                                                    * the standard "ifneeded" mechanism.
                                                    */
        NoPackageUnknown = 0x80000,                /* Skip using the configured [package
                                                    * unknown] command to locate any
                                                    * packages that fail to load using
                                                    * the standard "ifneeded" mechanism.
                                                    * It should also be noted that using
                                                    * this flag, which is enabled by
                                                    * default, breaks automatic [package
                                                    * unknown] handling provided by the
                                                    * Eagle Package Repository Client.
                                                    */
#if !MONO && NATIVE && WINDOWS
        ZeroString = 0x100000,                     /* Enable forcibly zeroing strings
                                                    * that may contain "sensitive" data?
                                                    * WARNING: THIS IS NOT GUARANTEED TO
                                                    * WORK RELIABLY ON ALL PLATFORMS.
                                                    * EXTREME CARE SHOULD BE EXERCISED
                                                    * WHEN HANDLING ANY SENSITIVE DATA,
                                                    * INCLUDING TESTING THAT THIS FLAG
                                                    * WORKS WITHIN THE SPECIFIC TARGET
                                                    * APPLICATION AND ON THE SPECIFIC
                                                    * TARGET PLATFORM. */
#endif
        ReplaceEmptyListOk = 0x200000,             /* The [lreplace] command should permit an
                                                    * empty list to be used, and ignore any
                                                    * indexes specified. */
        AllowProxyStream = 0x400000,               /* The standard channel streams that are
                                                    * provided by the interpreter host are
                                                    * allowed to be transparent proxies even
                                                    * when the interpreter host itself is not
                                                    * a transparent proxy.  Use of this flag
                                                    * should be extremely rare, if ever. */
        IgnoreBgErrorFailure = 0x800000,           /* Failures encountered when trying to
                                                    * handle a background error should be
                                                    * silently ignored. */
        BgErrorResetCancel = 0x1000000,            /* Call Engine.ResetCancel just before
                                                    * executing the configured background
                                                    * error handler. */
        CatchResetCancel = 0x2000000,              /* Call Engine.ResetCancel just after
                                                    * evaluating scripts for the [catch]
                                                    * command. */
        CatchResetGlobalCancel = 0x4000000,        /* Also reset the global flags for
                                                    * script cancellation just after
                                                    * evaluating scripts for the [catch]
                                                    * command. */
        CatchResetExit = 0x8000000,                /* Reset the Exit property just after
                                                    * evaluating scripts for the [catch]
                                                    * command. */
        TestNullIsEmpty = 0x10000000,              /* Treat null results from the [test?]
                                                    * command bodies the same as an empty
                                                    * string. */
        InfoVarsMayHaveGlobal = 0x20000000,        /* Includes global variables in the
                                                    * list returned by [info vars] when
                                                    * executed in a namespace call frame. */
        ComplainViaTest = 0x40000000,              /* Send complaints to the test suite
                                                    * (log, etc). */
        ComplainViaTrace = 0x80000000,             /* Send complaints to the diagnostic
                                                    * Trace/Debug listeners. */
        ForceGlobalLibrary = 0x100000000,          /* Make sure that all library scripts
                                                    * are evaluated in the global context.
                                                    */
        ForceGlobalStartup = 0x200000000,          /* Make sure that all startup scripts
                                                    * are evaluated in the global context.
                                                    */
        TclMathOperators = 0x400000000,            /* Enable adding a command for each
                                                    * operator into the "tcl::mathop"
                                                    * namespace. */
        TclMathFunctions = 0x800000000,            /* Enable adding a command for each
                                                    * operator into the "tcl::mathfunc"
                                                    * namespace. */
        LegacyOctal = 0x1000000000,                /* Use legacy octal support (i.e. leading
                                                    * zero means octal). */
        NoCleanupObjectReferences = 0x2000000000,  /* Skip cleaning up [temporary]
                                                    * object references when exiting
                                                    * the engine back to level zero. */
        StrictExpressionInteger = 0x4000000000,    /* When parsing an expression, failing to
                                                    * convert an integer-like string into an
                                                    * actual integer should result in a script
                                                    * error; otherwise, an attempt will be
                                                    * made to convert it into a floating point
                                                    * value. */
        NoInteractiveTimeout = 0x8000000000,       /* The interactive loop should not mess with
                                                    * the timeout thread. */
        UseNewEngineThread = 0x10000000000,        /* For asynchronous script evaluation, use
                                                    * a new EngineThread instead of the thread
                                                    * pool. */
        NoInteractiveSemaphore = 0x20000000000,    /* Do not use the semaphore when dealing
                                                    * with interactive input from within the
                                                    * interactive loop. */
        WaitInteractiveSemaphore = 0x40000000000,  /* Wait (forever) for the interactive
                                                    * semaphore to be available.  If this flag
                                                    * is not set, the inability to obtain the
                                                    * interactive semaphore will bail out of
                                                    * the interactive loop. */
        UseCultureForOperators = 0x80000000000,    /* String comparison operators should take
                                                    * into account the currently configured
                                                    * culture.  This is the legacy behavior;
                                                    * however, it is no longer enabled by
                                                    * default. */
        StringMatchStackChecking = 0x100000000000, /* Before doing any recursive processing of
                                                    * a [string match] pattern, make sure there
                                                    * is sufficient native stack space. */
        FixFor219233 = 0x200000000000,             /* Fix the [string match] range issue found
                                                    * in the native Tcl ticket:
                                                    *
                                                    *     https://core.tcl.tk/tcl/info/219233
                                                    *
                                                    * From the ticket, the following should be
                                                    * false:
                                                    *
                                                    *     string match {[a-z0-9_/-]} \\
                                                    *
                                                    * Since this "bugfix" has the potential to
                                                    * break older scripts that may rely on it,
                                                    * it is off by default and gated behind
                                                    * this feature flag.
                                                    */
        UsePrintfForDouble = 0x400000000000,       /* Use a variant of native printf() from the
                                                    * C Runtime Library to process the 'E', 'e',
                                                    * 'F', 'f', 'G', and 'g' format specifiers
                                                    * to the [format] command.  This flag is
                                                    * ignored when support for native code is
                                                    * not available (e.g. was disabled at
                                                    * build-time, etc).  This is the legacy
                                                    * default behavior. */
        TrackTestScripts = 0x800000000000,         /* Enable emitting diagnostic information
                                                    * when any test evaluates a script for its
                                                    * setup, body, or cleanup. */
        PreDisposeScripts = 0x1000000000000,       /* Enable evaluation of scripts from within
                                                    * the configured pre-dispose callbacks, if
                                                    * any. */
        SafeTiming = 0x2000000000000,              /* Attempt to prevent timing side-channel
                                                    * information from leaking into "safe"
                                                    * interpreters. */
        NoRefreshHost = 0x4000000000000,           /* Do not refresh the interactive host when
                                                    * preparing to read interactive input from
                                                    * within the interactive loop. */
        TestScriptWhatIf = 0x8000000000000,        /* Skip evaluation of test scripts. */
        NoNullArgument = 0x10000000000000,         /* Do not use null command results to create
                                                    * arguments to pass to subsequent commands.
                                                    */
        ResolveAssemblySearch = 0x20000000000000,  /* For [object resolve], search the loaded
                                                    * assemblies in the AppDomain directly. */
        DebugBreakNoComplain = 0x40000000000000,   /* For [debug break], by default, do not
                                                    * complain when unable to actually enter
                                                    * the debugger. */
        EventThreadAffinity = 0x80000000000000,    /* When waiting for a variable, e.g. via a
                                                    * [vwait] command, etc, process events for
                                                    * the current thread only. */
#if TEST
        CaptureTestTraces = 0x100000000000000,     /* When running a test via the [test1] or
                                                    * [test2] command, capture all of its trace
                                                    * output into a temporary log file. */
#endif
        DoesAnythingExist = 0x200000000000000,     /* When adding new commands, etc, check to
                                                    * be sure that no other executable entity
                                                    * exists first. */
        DoesNonProcedureExist = 0x400000000000000, /* When adding new procedures, check to be
                                                    * sure that no other executable entity
                                                    * exists first. */
        HashCodeAsHandle = 0x800000000000000,      /* When adding opaque object handles, the
                                                    * name of the opaque object handle will
                                                    * contain the runtime hash code for the
                                                    * underlying object. */
        AllowProxyCallback = 0x1000000000000000,   /* Allow transparent proxies to be used for
                                                    * callbacks, e.g. the interactive command
                                                    * callback. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        TclMathMask = TclMathOperators | TclMathFunctions,

        ///////////////////////////////////////////////////////////////////////////////////////////

        QuietUnsetMask = (ComplainViaTest | ComplainViaTrace) & Default,
        QuietSetMask = NoBackgroundError,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are disallowed when creating "safe" interpreters.
        //
        UnsafeMask = ReplaceEmptyListOk | ComplainViaTest |
                     TclMathOperators | TclMathFunctions |
                     LegacyOctal | UsePrintfForDouble|
                     TrackTestScripts | PreDisposeScripts,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default flags for newly created interpreters.
        //
        Default = FinallyResetCancel | FinallyRestoreCancel |
                  FinallyResetExit | FinallyRestoreExit |
                  NoPackageUnknown | ReplaceEmptyListOk |
                  TestNullIsEmpty | ComplainViaTest |
                  TclMathOperators | TclMathFunctions |
                  LegacyOctal | StrictExpressionInteger |
                  UsePrintfForDouble | TrackTestScripts |
                  PreDisposeScripts | SafeTiming |
                  NoNullArgument | DebugBreakNoComplain |
                  DoesAnythingExist | AllowProxyCallback
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e617a055-a7bd-468e-a5fb-052f57251323")]
    public enum CallbackFlags
    {
        None = 0x0,                    /* No special handling. */
        Invalid = 0x1,                 /* Invalid, do not use. */
        ReadOnly = 0x2,                /* The callback may not be modified nor removed. */

        //
        // NOTE: Callback argument handling.
        //

        Arguments = 0x4,               /* Automatically add argument objects. */
        Create = 0x8,                  /* We expect to create an object (e.g. System.Int32.Parse)? */
        Dispose = 0x10,                /* Dipose the object if it cannot be fully created/added? */
        Alias = 0x20,                  /* Create a command alias for the newly created object? */
        AliasRaw = 0x40,               /* The command alias refers to [object invokeall]. */
        AliasAll = 0x80,               /* The command alias refers to [object invokeall]. */
        AliasReference = 0x100,        /* The command alias holds an object reference. */
        ToString = 0x200,              /* Forcibly convert the object to a string and discard it? */
        ByRefStrict = 0x400,           /* Enforce strict type checking on ByRef argument values? */
        Complain = 0x800,              /* Complain about failures that occur when firing events? */
        CatchInterrupt = 0x1000,       /* Catch any ThreadInterruptedException within
                                        * the ThreadStart and ParameterizedThreadStart
                                        * methods and do not re-throw it; otherwise,
                                        * catch it, log it, and then re-throw it. */
        ReturnValue = 0x2000,          /* Automatically handle return values. */
        DefaultValue = 0x4000,         /* Used with ReturnValue in order to force the return of
                                        * the default value (e.g. 0, null) for the method return
                                        * type. */
        AddReference = 0x8000,         /* Add a reference to the callback return value. */
        RemoveReference = 0x10000,     /* Remove a reference from the callback return value. */
        DisposeThread = 0x20000,       /* Call MaybeDisposeThread for delegate types that could
                                        * be used as a thread entry point. */
        ThrowOnError = 0x40000,        /* Throw an exception if the evaluated script returns an
                                        * error? */
        External = 0x80000,            /* The command callback was created via the Utility class.
                                        * This flag is for core library use only. */
        UseOwner = 0x100000,           /* The callback script should be handled by the owner of
                                        * the interpreter being used instead of being directly
                                        * evaluated. */
        Asynchronous = 0x200000,       /* The callback script should be queued asynchronously to
                                        * the owner of the interpreter being used. */
        AsynchronousIfBusy = 0x400000, /* The callback script should be queued asynchronously to
                                        * the owner of the interpreter being used if the owner
                                        * is busy; otherwise, it should be sent synchronously. */
        ResetCancel = 0x800000,        /* Reset the local script cancellation flags for the target
                                        * interpreter prior to evaluating the callback script. */
        MustResetCancel = 0x1000000,   /* Be forceful when resetting script cancellation flags
                                        * for the target interpreter prior to evaluating the
                                        * callback script. */
        FireAndForget = 0x2000000,     /* The callback should be cleaned up automatically after
                                        * it is invoked.  This flag is needed for asynchronous
                                        * callbacks. */
        UseParameterNames = 0x4000000, /* The parameter names for the delegate type, if any,
                                        * should be used. */

        //
        // NOTE: Default argument handling.
        //
        Default = Arguments | Create | Dispose |
                  Alias | Complain | ReturnValue |
                  AddReference
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ad5ea8ab-53a1-4055-8db5-5c1bba309cfb")]
    public enum MarshalFlags : ulong
    {
        None = 0x0,                             /* No special handling. */
        Invalid = 0x1,                          /* Invalid, do not use. */
        ForMask = 0x2,                          /* Canary, do not use. */
        Verbose = 0x4,                          /* Enable [very?] verbose error messages. */
        DefaultValue = 0x8,                     /* Use the default value for a parameter when a
                                                 * scalar variable does not exist. */
        StringList = 0x10,                      /* Enable aggressive automatic conversions to all
                                                 * variants of the StringList type. */
        SkipNullSetupValue = 0x20,              /* For input or output array types, skip changing
                                                 * the script variable into an array when the
                                                 * object value is null. */
        NoSystemArray = 0x40,                   /* Disable transparent detection and use of all
                                                 * System.Array backed variables when marshalling
                                                 * arguments to a method. */
        StrictMatchCount = 0x80,                /* MatchParameterTypes should strictly match the
                                                 * parameter counts. */
        StrictMatchType = 0x100,                /* MatchParameterTypes should strictly match the
                                                 * parameter types. */
        ForceParameterType = 0x200,             /* Force FindMethodsAndFixupArguments to use the
                                                 * specified parameter type, not the one from the
                                                 * method overload itself. */
        ReorderMatches = 0x400,                 /* Reorder the matching methods based on the
                                                 * criteria specified in the ReorderFlags. */
        NoDelegateCallback = 0x800,             /* Do not attempt to perform any conversions when
                                                 * the target type is System.Delegate. */
        NoGenericCallback = 0x1000,             /* Prevent use of the GenericCallback type when
                                                 * creating a command callback. */
        DynamicCallback = 0x2000,               /* Enable use of the DynamicInvokeCallback when
                                                 * creating a command callback. */
        CallbackParameterNames = 0x4000,        /* Populate the parameter names for use with the
                                                 * callback script (e.g. via [nproc]). */
        NoChangeTypeThrow = 0x8000,             /* Skip throwing exceptions from within the
                                                 * script binder ChangeType method. */
        NoBindToFieldThrow = 0x10000,           /* Skip throwing exceptions from within the
                                                 * script binder BindToField method. */
        SkipChangeType = 0x20000,               /* Skip calling the ChangeType method of the
                                                 * fallback (and/or default) binder. */
        SkipBindToField = 0x40000,              /* Skip calling the BindToField method of the
                                                 * fallback (and/or default) binder. */
        SkipReferenceTypeCheck = 0x80000,       /* Skip checking reference type equality when
                                                 * coming back from the ChangeType method. */
        SkipValueTypeCheck = 0x100000,          /* Skip checking value type equality when coming
                                                 * back from the ChangeType method. */
        NoCallbackOptions = 0x200000,           /* Skip parsing per-CommandCallback options. */
        IgnoreCallbackOptions = 0x400000,       /* Ignore per-CommandCallback option values. */
        ThrowOnBindFailure = 0x800000,          /* Throw an exception on delegate binding
                                                 * failures. */
        NoHandle = 0x1000000,                   /* Skip using opaque object handles. */
        UseInOnly = 0x2000000,                  /* When determining if a parameter is "input",
                                                 * use only the ParameterInfo.IsIn property. */
        UseByRefOnly = 0x4000000,               /* When determining if a parameter is "output",
                                                 * use only the Type.IsByRef property. */
        NoByRefArguments = 0x8000000,           /* Skip special handling for output parameters,
                                                 * e.g. do not create ArgumentInfo objects. */
        NoScriptBinder = 0x10000000,            /* Do not assume that the binder implements
                                                 * the full IScriptBinder semantics for the
                                                 * passing of a MarshalClientData as the
                                                 * value parameter. */
        TraceResults = 0x20000000,              /* Emit the final list of method overloads to
                                                 * the trace listeners. */
        HandleByValue = 0x40000000,             /* Prior to looking up an opaque object handle
                                                 * for any parameter, use its scalar variable
                                                 * value as the opaque object handle name. */
        ByValHandleByValue = 0x80000000,        /* Prior to looking up an opaque object handle
                                                 * for an "input" parameter, use its scalar
                                                 * variable value as the opaque object handle
                                                 * name. */
        ByRefHandleByValue = 0x100000000,       /* Prior to looking up an opaque object handle
                                                 * for an "output" parameter, use its scalar
                                                 * variable value as the opaque object handle
                                                 * name. */
        ForceHandleByValue = 0x200000000,       /* Prior to looking up an opaque object handle
                                                 * for any parameter, use its scalar variable
                                                 * value as the opaque object handle name,
                                                 * even if an opaque object handle is not
                                                 * found using that value. */
        IsAssignableFrom = 0x400000000,         /* EXPERIMENTAL: Enable use of custom handling
                                                 * for the Type.IsAssignableFrom method?  This
                                                 * is mostly used to work around Mono issues.
                                                 */
        SpecialValueType = 0x800000000,         /* EXPERIMENTAL: Enable special handling of
                                                 * the ValueType type when determining if a
                                                 * value can be used with a given type. */
        ForceHandleOnly = 0x1000000000,         /* EXPERIMENTAL: If an opaque object handle is
                                                 * found, stop further type conversions. */
        AllowProxyGetType = 0x2000000000,       /* EXPERIMENTAL: Allow GetType to be called on
                                                 * transparent proxy objects. */
        ForceProxyGetType = 0x4000000000,       /* EXPERIMENTAL: Force GetType to be called on
                                                 * transparent proxy objects for types that are
                                                 * not loaded into the AppDomain. */
        AllowAnyMethod = 0x8000000000,          /* Do not bother calling the IsAllowed method
                                                 * on the IScriptBinder interface. */
        MinimumOptionalCount = 0x10000000000,   /* Prefer matching method overloads that have
                                                 * a larger number of optional parameters. */
        SelectMethodIndex = 0x20000000000,      /* Force use of the SelectMethodIndex method
                                                 * even when the method index was explicitly
                                                 * specified. */
        SortMembers = 0x40000000000,            /* Sort members in the order that makes the
                                                 * most sense for the core marshaller, e.g.
                                                 * by name with the least number of parameters
                                                 * first. */
        ReverseOrder = 0x80000000000,           /* Prefer method overloads that accept the
                                                 * greatest number of parameters. */
        NonPublic = 0x100000000000,             /* Include non-public members when performing
                                                 * comparisons. */
        ReturnICallback = 0x200000000000,       /* When creating a Delegate, return its
                                                 * associated ICallback instance instead of
                                                 * the raw Delegate instance itself. */
        SimpleCallback = 0x400000000000,        /* When the target type is a Delegate, attempt
                                                 * to lookup appropriate pre-existing method. */
        FlattenIntoParamArray = 0x800000000000, /* When handling a parameter with the ParamArray
                                                 * attribute, flatten any IEnumerable arguments
                                                 * into the final list of format parameters for
                                                 * the target method, e.g. do not pass an array
                                                 * value as one of the formal parameters to the
                                                 * target method. */
        NoParameterCounts = 0x1000000000000,    /* Skip enforcement of parameter counts.  This
                                                 * is intended for debugging only, do not use
                                                 * this in production code. */
        ShowSignatures = 0x2000000000000,       /* Include the parameters in error messages
                                                 * generated by the core marshaller. */
        WidenToUnsigned = 0x4000000000000,      /* For internal conversions, allow integer
                                                 * values to be widened to unsigned. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        SimpleCallbackErrorMask = ReturnICallback | ForMask,

        SimpleCallbackWarningMask = NoDelegateCallback | NoGenericCallback |
                                    DynamicCallback | CallbackParameterNames |
                                    ForMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = StrictMatchCount | StrictMatchType |
                  ThrowOnBindFailure /* LEGACY */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("fcec8afc-2e5c-479b-bd55-d20621acbcc5")]
    public enum ReorderFlags
    {
        None = 0x0,                       /* No special handling. */
        Invalid = 0x1,                    /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        FewestParametersFirst = 0x2,      /* Prefer method overloads that accept
                                           * the fewest number of parameters. */
        MostParametersFirst = 0x4,        /* Prefer method overloads that accept
                                           * the greatest number of parameters. */
        ShallowestTypesFirst = 0x8,       /* Prefer argument types towards the
                                           * root of the hierarchy. */
        DeepestTypesFirst = 0x10,         /* Prefer argument types towards the
                                           * leaves of the hierarchy. */
        TypeDepthsFirst = 0x20,           /* Compare the type depths before
                                           * comparing the parameter counts. */
        TotalTypeDepths = 0x40,           /* When comparing type depths, instead
                                           * of returning a result at the first
                                           * non-equal type depth, sum all the
                                           * type depths to arrive at the result.
                                           * This is useful primarily when the
                                           * parameters do not conform to a
                                           * well-defined order between method
                                           * overloads. */
        UseParameterTypes = 0x80,         /* When calculating type depths, use
                                           * the formal parameter type instead
                                           * of the argument type, if doing so
                                           * would result in a more accurate
                                           * type depth. */
        SubTypeDepths = 0x100,            /* Consider Array<T> and Nullable<T> as
                                           * levels to be traversed when
                                           * calculating type depths. */
        ValueTypeDepths = 0x200,          /* Consider all reference types, except
                                           * "Object" and "ValueType", as levels
                                           * to be traversed when calculating
                                           * type depths. */
        ByRefTypeDepths = 0x400,          /* Also consider ByRef<T> as a level to
                                           * be traversed when calculating type
                                           * depths. */
        FallbackOkOnError = 0x800,        /* When an error is encountered (e.g.
                                           * parameter validation, exception,
                                           * etc), return "Ok" instead of
                                           * "Error".  This allows the default
                                           * method overload to be called instead
                                           * of failing the entire method call
                                           * operation. */
        UseArgumentCounts = 0x1000,       /* If necessary, take the supplied
                                           * arguments counts into account when
                                           * calculating parameter counts. */
        StrictParameterCounts = 0x2000,   /* Bail out on error while querying
                                           * parameter counts. */
        ContinueParameterCounts = 0x4000, /* Skip to next method overload on
                                           * error while querying parameter
                                           * counts. */
        StrictTypeDepths = 0x8000,        /* Bail out on error while calculating
                                           * parameter type depths. */
        ContinueTypeDepths = 0x10000,     /* Skip to next method overload on
                                           * error while calculating parameter
                                           * type depths. */
        StringTypePenalty = 0x20000,      /* Subtract one level for types that
                                           * are trivially convertible from a
                                           * string (e.g. System.String) when
                                           * calculating type depths. */
        StringTypeBonus = 0x40000,        /* Add one level for types that are
                                           * trivially convertible from a string
                                           * (e.g. System.String) when calculating
                                           * type depths. */
        TraceResults = 0x80000,           /* Emit the final sorted list of method
                                           * overloads to the trace listeners. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ParameterCountMask = FewestParametersFirst | MostParametersFirst,
        ParameterTypeDepthMask = ShallowestTypesFirst | DeepestTypesFirst,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = FewestParametersFirst | DeepestTypesFirst |
                  SubTypeDepths | ValueTypeDepths |
                  ByRefTypeDepths /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1e4879b1-f3db-4bda-8637-6f1495a51ec8")]
    public enum MethodFlags
    {
        None = 0x0,              /* No special handling. */
        Invalid = 0x1,           /* Invalid, do not use. */
        System = 0x2,            /* Method is owned by the core library, do not use. */
        NoAdd = 0x4,             /* Methods with this flag will not be automatically used as a
                                  * policy callback by the plugin manager. */
        PluginPolicy = 0x8,      /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle plugin loading policies. */
        CommandPolicy = 0x10,    /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle command execution policies. */
        SubCommandPolicy = 0x20, /* NOT USED: Method conforms to the ExecuteCallback delegate
                                  * type and is used to handle sub-command execution policies. */
        ProcedurePolicy = 0x40,  /* NOT USED: Method conforms to the ExecuteCallback delegate
                                  * type and is used to handle procedure execution policies. */
        ScriptPolicy = 0x80,     /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle IScript object policies. */
        FilePolicy = 0x100,      /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle script file policies. */
        StreamPolicy = 0x200,    /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle script stream policies. */
        LicensePolicy = 0x400,   /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle "license" policies. */
        TracePolicy = 0x800,     /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle "trace" policies. */
        OtherPolicy = 0x1000,    /* Method conforms to the ExecuteCallback delegate type and is
                                  * used to handle "other" policies. */
        VariableTrace = 0x2000,  /* Method conforms to the TraceCallback delegate type and is
                                  * used to handle variable traces. */

        PolicyMask = PluginPolicy | CommandPolicy |
                     SubCommandPolicy | ProcedurePolicy |
                     ScriptPolicy | FilePolicy |
                     StreamPolicy | LicensePolicy |
                     TracePolicy | OtherPolicy,
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2000c391-277e-435f-8c29-f5f64f1c6e19")]
    public enum UsageType
    {
        None = 0x0,
        Count = 0x1,
        Microseconds = 0x2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("768b5bd7-cb31-465b-827e-94a12dc46dcd")]
    public enum LookupFlags : ulong
    {
        None = 0x0,        /* No special handling. */
        Invalid = 0x1,     /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Generic
        Wrapper = 0x2,     /* Return the wrapper object, not the contained entity. */
        Validate = 0x4,    /* Validate that the returned object is not null.  If it
                            * is null, return an error. */
        Verbose = 0x8,     /* Return a verbose error message. */
        Visible = 0x10,    /* Consider "visible" (i.e. non-hidden) entities. */
        Invisible = 0x20,  /* Consider "invisible" (i.e. hidden) entities. */
        NoUsable = 0x40,   /* Skip usability check for entities. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Encoding Specific (For Now)
        Strict = 0x1000,        /* Always fail if the specified encoding is not found. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Variable Specific
        CreateMissing = 0x2000, /* Create dummy variable if necessary.  The returned
                                 * new variable may or may not reside within its call
                                 * frame. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Plugin Specific
        WithCommands = 0x4000,  /* Must own one or more commands. */
        WithFunctions = 0x8000, /* Must own one or more functions. */
        WithPolicies = 0x10000, /* Must own one or more policies. */
        WithTraces = 0x20000,   /* Must own one or more traces. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Path Specific
        AllPaths = 0x80000,     /* When searching, make sure that
                                 * all paths are considered. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Future IEntityManager Interface Use
        #region Dead Code
#if DEAD_CODE
        Absolute = 0x100000,             /* LookupNamespace */
        NewFrame = 0x200000,             /* CreateNamespace */
        NoCase = 0x400000,               /* ListIExecutes, ListCommands,
                                          * ListFunctions, ListProcedures,
                                          * ListOperators, ListChannels */
        ErrorOnUnavailable = 0x800000,   /* ListIExecutes, ListCommands,
                                          * ListFunctions, ListProcedures,
                                          * ListOperators, ListChannels */
        Delete = 0x1000000,              /* RenameIExecute, RenameCommand,
                                          * RenameProcedure */
        IgnoreAlias = 0x2000000,         /* RenameObject */
        NoNamespaces = 0x4000000,        /* RenameObject */
        ErrorOnRenameAlias = 0x8000000,  /* RenameObject */
        Synchronous = 0x10000000,        /* RemoveObject */
        HasAll = 0x20000000,             /* ListCommands, ListFunctions,
                                          * ListProcedures, ListOperators */
        NotHasAll = 0x40000000,          /* ListCommands, ListFunctions,
                                          * ListProcedures, ListOperators */
        ErrorOnNonStandard = 0x80000000, /* AddFunction */
        AppendMode = 0x100000000,        /* AddChannel */
        AutoFlush = 0x200000000,         /* AddChannel */
        Flush = 0x400000000,             /* RemoveChannel */
        Close = 0x800000000,             /* RemoveChannel */
        ErrorOnFlush = 0x1000000000,     /* RemoveChannel */
        ErrorOnClose = 0x2000000000,     /* RemoveChannel */
        IgnoreNull = 0x4000000000,       /* AddExecuteCallbacks, RemoveExecuteCallbacks */
        StopOnError = 0x8000000000,      /* AddExecuteCallbacks, RemoveExecuteCallbacks */
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Entity Specific Flags
        MustBeAlive = 0x10000000000,      /* THREAD: entity must be considered "alive". */
        NullForProxyType = 0x20000000000, /* OBJECT: transparent proxies have "null" type. */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are for use by the Does*Exist() methods of the
        //       Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
        Exists = Wrapper | Visible,
        ExistsAndValid = Exists | Validate,

        //
        // NOTE: These flags are for use by the Remove*() methods of the
        //       Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
        Remove = Default & ~Validate,
        RemoveNoVerbose = Remove & ~Verbose,

        //
        // NOTE: These flags are for use by the Unload*() methods of the
        //       Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
#if ISOLATED_PLUGINS
        Unload = Default,
#endif

        //
        // NOTE: These flags are for use by the Get*Interpreter() methods of
        //       the Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
        Interpreter = Default & ~Wrapper,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are for use by the Engine class only.  These flags
        //       may NOT be used by external components.
        //
        EngineDefault = Default,
        EngineNoVerbose = EngineDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the Expression class only.  These
        //       flags may NOT be used by external components.
        //
        ExpressionDefault = Default & ~Wrapper,
        ExpressionNoVerbose = ExpressionDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the MarshalOps class only.  These
        //       flags may NOT be used by external components.
        //
        MarshalDefault = Default & ~Verbose,
        MarshalNoVerbose = MarshalDefault,
        MarshalAlias = MarshalDefault & ~Validate,

        //
        // NOTE: These flags are for use by the HelpOps class only.  These flags
        //       may NOT be used by external components.
        //
        HelpDefault = Default & ~Wrapper,
        HelpNoVerbose = HelpDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the Default host class only.  These
        //       flags may NOT be used by external components.
        //
        HostNoVerbose = Default & ~Verbose,

        //
        // NOTE: These flags are for use by the PolicyOps and RuntimeOps classes
        //       only.  These flags may NOT be used by external components.
        //
        PolicyDefault = Default | Invisible,
        PolicyNoVerbose = PolicyDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the GetInterpreterAliasTarget,
        //       HasInterpreterAlias, GetLibraryAliasTarget, GetObjectAliasTarget,
        //       and GetTclAliasTarget methods only.  These flags may NOT be used
        //       by external components.
        //
        AliasDefault = Default | Invisible,
        AliasNoVerbose = AliasDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the Interpreter.GetOptions
        //       method only.  These flags may NOT be used by external
        //       components.
        //
        OptionDefault = NoValidate,

        //
        // NOTE: These flags are used for finally timeout threads only.
        //
        FinallyTimeoutThread = Default | MustBeAlive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags should be used when the caller does not require a
        //       valid (non-null) entity be found (i.e. only that one exists
        //       for the given name or token).  These flags may be used by
        //       external components.
        //
        NoValidate = Wrapper | Verbose | Visible,

        //
        // NOTE: These flags should be used when the caller wants direct access
        //       to the entity itself, not any intermediate wrapper object that
        //       may contain it.  These flags may be used by external components.
        //
        NoWrapper = Validate | Verbose | Visible,

        //
        // NOTE: These flags should be used when the caller does not require an
        //       error message.  These flags may be used by external components.
        //
        NoVerbose = Wrapper | Validate | Visible,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default flags for all entity lookups.  These
        //       flags may be used by external components.
        //
        Default = Wrapper | Validate | Verbose | Visible
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("bd01cdba-63db-4751-9c5e-45504fb336af")]
    public enum AliasFlags
    {
        None = 0x0,               /* No special handling. */
        MergeArguments = 0x1,     /* The alias will merge the current command arguments with the
                                   * arguments originally specified during alias creation,
                                   * maintaining the overall argument order and merging the options
                                   * together. */
        SkipTargetName = 0x2,     /* The merged arguments will not include the target command
                                   * name. */
        SkipSourceName = 0x4,     /* The merged arguments will not include the source command
                                   * name. */
        UseTargetRemaining = 0x8, /* The merged arguments will include all non-option arguments
                                   * from the target. */
        Evaluate = 0x10,          /* The target of the alias will be evaluated rather than executed
                                   * directly. */
        GlobalNamespace = 0x20,   /* The global namespace should be used as the context. */
        CrossCommand = 0x40,      /* The command alias is being used for [interp alias] support. */
        Object = 0x80,            /* The command alias refers to a managed object. */
        Reference = 0x100,        /* The command alias holds a reference to the managed object. */
        Namespace = 0x200,        /* The command alias is being used for [namespace] support. */
        CrossInterpreter = 0x400, /* The command alias is being used for [interp create] support. */

#if EMIT && NATIVE && LIBRARY
        Library = 0x800,          /* The command alias refers to a native library delegate. */
#endif

#if NATIVE && TCL
        TclWrapper = 0x1000,      /* The command alias is being used for [tcl eval] support. */
#endif

        CrossCommandAlias = GlobalNamespace | CrossCommand,         /* For [interp alias]
                                                                     * support. */
        CrossInterpreterAlias = GlobalNamespace | CrossInterpreter, /* For [interp create]
                                                                     * support. */
        NamespaceImport = SkipSourceName | Namespace                /* For [namespace import]
                                                                     * support. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b8e02964-4bde-47dc-a873-15cd5cb3633e")]
    public enum SubCommandFlags
    {
        None = 0x0,                  /* No special handling. */
        Invalid = 0x1,               /* Invalid, do not use. */
        Core = 0x2,                  /* The sub-command is handled by the core. */
        Safe = 0x4,                  /* Sub-command is "safe" to execute for partially
                                      * trusted and/or untrusted scripts. */
        Unsafe = 0x8,                /* Sub-command is NOT "safe" to execute for
                                      * partially trusted and/or untrusted scripts. */
        ForceQuery = 0x10,           /* Instead of modifying the sub-command, just
                                      * return it (i.e. even if the number of arguments
                                      * to the command would suggest otherwise). */
        ForceNew = 0x20,             /* The sub-command must be added, not modified. */
        ForceReset = 0x40,           /* The sub-command must be [re-]added during a
                                      * reset, if it does not already exist. */
        ForceDelete = 0x80,          /* The sub-command must be removed, not reset. */
        NoComplain = 0x100,          /* The query, add, reset, and remove operations
                                      * cannot generate an error just because the
                                      * sub-command may -OR- may not exist.  Queries
                                      * will return an empty string in this case. */
        StrictNoArguments = 0x200,   /* If any arguments are present, raise an error
                                      * because the configured script will not process. */
        UseExecuteArguments = 0x400, /* Append the IExecute.Execute arguments to the
                                      * configured script command before evaluating. */
        SkipNameArguments = 0x800,   /* When used with UseExecuteArguments, causes the
                                      * names of the command and sub-command to be
                                      * omitted from the passed arguments. */

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ae31c0f5-e0a8-44b0-b784-bcdadfc76f0e")]
    public enum CommandFlags
    {
        None = 0x0,               /* No special handling. */
        Invalid = 0x1,            /* Invalid, do not use. */
        Core = 0x2,               /* This command is part of the core command set.
                                   */
        Delegate = 0x4,           /* This command wraps a delegate provided from
                                   * an external source. */
        SubDelegate = 0x8,        /* This command wraps zero or more delegates to
                                   * its available sub-commands. */
#if ISOLATED_PLUGINS
        Isolated = 0x10,          /* The command has been loaded into an isolated
                                   * AppDomain (most likely via its parent plugin).
                                   */
#endif
        Disabled = 0x20,          /* The command may not be executed. */
        Hidden = 0x40,            /* The command may only be executed if allowed
                                   * by policy. */
        ReadOnly = 0x80,          /* The command may not be modified nor removed. */
        NativeCode = 0x100,       /* The command contains, calls, or refers to
                                   * native code. */
        Breakpoint = 0x200,       /* Break into debugger before execution. */
        NoAdd = 0x400,            /* The command will not be auto-loaded by the
                                   * plugin manager. */
        NoPopulate = 0x800,       /* The command will not be populated by the
                                   * plugin manager. */
        Alias = 0x1000,           /* The command is really an alias to another
                                   * command. */
        Replace = 0x2000,         /* Remove any pre-existing command. */
        Restore = 0x4000,         /* Skip over trying to add commands if they
                                   * already exist (restoration mode). */
        Safe = 0x8000,            /* Command is "safe" to execute for partially
                                   * trusted and/or untrusted scripts. */
        Unsafe = 0x10000,         /* Command is NOT "safe" to execute for
                                   * partially trusted and/or untrusted scripts.
                                   */
        Standard = 0x20000,       /* The command is largely (or completely)
                                   * compatible with an identically named command
                                   * from Tcl/Tk 8.4, 8.5, and/or 8.6. */
        NonStandard = 0x40000,    /* The command is not present in Tcl/Tk 8.4,
                                   * 8.5, and/or 8.6 -OR- it is completely
                                   * incompatible with an identically named
                                   * command in Tcl/Tk 8.4, 8.5, and/or 8.6. */
        NoToken = 0x80000,        /* Skip handling of the command token via the
                                   * associated plugin. */
        NoRename = 0x100000,      /* Prevent the command from being renamed. */
        NoRemove = 0x200000,      /* Prevent the command from being removed. */
        Obsolete = 0x400000,      /* The command has been superseded and should
                                   * not be used for new development. */
        Diagnostic = 0x800000,    /* The command is primarily intended to be used
                                   * when debugging and/or testing the core
                                   * library.  Also, the semantics of the
                                   * contained sub-commands are subject to
                                   * change, even in stable releases, and should
                                   * not be relied upon by any production
                                   * applications, plugins, or scripts. */
        Ensemble = 0x1000000,     /* The command is an ensemble and may have
                                   * special dispatch handling for its supported
                                   * sub-commands and/or for unknown sub-commands.
                                   */
        SubCommand = 0x2000000,   /* The command is really a sub-command. */
#if SHELL && INTERACTIVE_COMMANDS
        Interactive = 0x4000000,  /* The command is designed to be used from
                                   * within interactive (REPL) loops only. */
#endif
        Initialize = 0x8000000,   /* This command is needed in order to be able
                                   * to initialize the minimal script library,
                                   * i.e. "init.eagle". */
        SecuritySdk = 0x10000000, /* This command is needed in order to use the
                                   * baseline security SDK. */
        LicenseSdk = 0x20000000,  /* This command is needed in order to use the
                                   * baseline license SDK. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AttributeMask = Core | Delegate |
                        SubDelegate | NativeCode |
                        NoAdd | NoPopulate |
                        Alias | Safe | Unsafe |
                        Standard | NonStandard |
                        NoRename | NoRemove |
                        Obsolete | Diagnostic |
                        Ensemble | SubCommand |
#if SHELL && INTERACTIVE_COMMANDS
                        Interactive,
#else
                        None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        InstanceMask =
#if ISOLATED_PLUGINS
            Isolated |
#endif
            Disabled | Hidden |
            ReadOnly | Breakpoint |
            NoToken | NoRename |
            NoRemove,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NonInstanceMask = Replace | Restore
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d2ee28ef-b37e-4f90-a88a-408d8610c33e")]
    public enum ByRefArgumentFlags
    {
        None = 0x0,             /* No special flags. */
        Invalid = 0x1,          /* Invalid, do not use. */
        Fast = 0x2,             /* Fast mode, skip traces, watches, notifications,
                                 * post-processing (primarily for speed), etc. */
        Direct = 0x4,           /* Direct mode, bypass all use of SetVariableValue
                                 * (primarily for speed).  Currently, this option
                                 * only applies to arrays. */
        Strict = 0x8,           /* Enable strict by-ref argument type handling. */
        Create = 0x10,          /* We expect to create an object (e.g. System.Int32.Parse)? */
        Dispose = 0x20,         /* Dipose the object if it cannot be fully created/added? */
        Alias = 0x40,           /* Create a command alias for the newly created object? */
        AliasRaw = 0x80,        /* The command alias refers to [object invokeraw]. */
        AliasAll = 0x100,       /* The command alias refers to [object invokeall]. */
        AliasReference = 0x200, /* The command alias holds an object reference. */
        ToString = 0x400,       /* Forcibly convert the object to a string and discard it? */
        ArrayAsValue = 0x800,   /* Use opaque object handles for managed arrays instead of
                                 * setting the script array element values. */
        ArrayAsLink = 0x1000,   /* Use a script variable linked to the underlying array object
                                 * instead of copying the data. */
        NoSetVariable = 0x2000, /* Skip setting variables for by-ref arguments. */

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ae93bd6d-556e-4d73-bd51-2a1f4af7bca1")]
    public enum ObjectReferenceType
    {
        None = 0x0,    // None or unknown, do not use.
        Invalid = 0x1, // Invalid, do not use.
        Create = 0x2,  // Reference was added at handle creation.
        Demand = 0x4,  // Reference was added by script request.
        Trace = 0x8,   // Reference was added via [set] trace.
        Return = 0x10, // Reference was added via [return].
        Command = 0x20 // Reference was added for a command (alias?).
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("3aa35d84-3d18-4fb5-a6d1-d1d53108d76f")]
    public enum ObjectFlags : ulong
    {
        None = 0x0,                                /* No special handling.  Be very careful when
                                                    * using this.
                                                    * If the object does not actually belong to
                                                    * the caller, be sure to use the NoDispose
                                                    * flag instead. */
        Invalid = 0x1,                             /* Invalid, do not use. */
        Locked = 0x2,                              /* Automatic reference counting does not apply
                                                    * to this object. */
        Safe = 0x4,                                /* This object can be used by safe interpreters
                                                    * without risking security. */
        WellKnown = 0x8,                           /* This is of a well-known object type. */
        Assembly = 0x10,                           /* This is assembly loaded by [object load]. */
        Runtime = 0x20,                            /* This is an object from this library. */
        Application = 0x40,                        /* This is an object from the application. */
        Interpreter = 0x80,                        /* This is an Interpreter object. */
        InterpreterSettings = 0x100,               /* This is an InterpreterSettings object. */
        InterpreterHelper = 0x200,                 /* This is a InterpreterHelper object. */
        EventManager = 0x400,                      /* This is an event manager object. */
        Debugger = 0x800,                          /* This is a script debugger object. */
        Host = 0x1000,                             /* This is an IHost object of some type. */
        ContextManager = 0x2000,                   /* This is a context manager object. */
        EngineContext = 0x4000,                    /* This is an engine context object. */
        InteractiveContext = 0x8000,               /* This is an interactive context object. */
        TestContext = 0x10000,                     /* This is a test context object. */
        VariableContext = 0x20000,                 /* This is a variable context object. */
        Namespace = 0x40000,                       /* This is a namespace object. */
        CallFrame = 0x80000,                       /* This is a call frame object. */
        Wrapper = 0x100000,                        /* This is an entity wrapper object of some
                                                    * type. */
        CallStack = 0x200000,                      /* This is a call stack. */
        Variable = 0x400000,                       /* This is a variable. */
#if NATIVE && TCL
        NativeTcl = 0x800000,                      /* This is an object for interacting with
                                                    * the native Tcl wrapper. */
#endif
        Alias = 0x1000000,                         /* This object has a command alias. */
        NoDispose = 0x2000000,                     /* This object cannot be disposed ([object
                                                    * dispose] is a NOP). */
        AllowExisting = 0x4000000,                 /* The handle for this object should not be
                                                    * created if one with the same value exists
                                                    * (even if the object name has been
                                                    * specified). */
        ForceNew = 0x8000000,                      /* The handle for this object should always be
                                                    * created (even if one with the same value
                                                    * exists). */
        ForceDelete = 0x10000000,                  /* This bridged Tcl command object should be
                                                    * forcibly deleted during dispose. */
        NoComplain = 0x20000000,                   /* Errors should be ignored when trying to
                                                    * delete the Tcl command during bridge
                                                    * disposal. */
        NoBinder = 0x40000000,                     /* Skip trying to query the Binder property of
                                                    * the interpreter. */
        NoComObjectLookup = 0x80000000,            /* Skip all special type lookup handling for
                                                    * the COM object proxy type (i.e.
                                                    * "System.__ComObject"). */
        NoComObjectReturn = 0x100000000,           /* Skip all special return value handling for
                                                    * the COM object proxy type (i.e.
                                                    * "System.__ComObject"). */
        IgnoreAlias = 0x200000000,                 /* Ignore the Alias flag in FixupReturnValue
                                                    * when looking up existing objects to use for
                                                    * an opaque object handle. */
        NoAutoDispose = 0x400000000,               /* The object cannot be disposed automatically
                                                    * because we may not own it. */
        AutoDispose = 0x800000000,                 /* The object should be disposed automatically.
                                                    */
        NoAttribute = 0x1000000000,                /* Forbid using the ObjectFlagsAttribute when
                                                    * handling object return values. */
        ForceAutomaticName = 0x2000000000,         /* Force a non-empty opaque object handle to be
                                                    * returned for null values when the name is
                                                    * automatically generated. */
        ForceManualName = 0x4000000000,            /* Force a non-empty opaque object handle to be
                                                    * returned for null values when the name is
                                                    * manually specified. */
        NullObject = 0x8000000000,                 /* Reserved for use with the "null" opaque
                                                    * object handle only.  Do NOT use this for any
                                                    * other purpose. */
        SharedObject = 0x10000000000,              /* Reserved for use by the AddSharedObject
                                                    * method.  Do NOT use this for any other
                                                    * purpose. */
        AddReference = 0x20000000000,              /* Add an initial reference when creating a new
                                                    * opaque object handle. */
        StickAlias = 0x40000000000,                /* Create a command alias if the new object was
                                                    * created via an object with this flag set. */
        UnstickAlias = 0x80000000000,              /* Forbid creating a command alias just because
                                                    * the new object was created via an object with
                                                    * the StickAlias flag set. */
        NoReturnReference = 0x100000000000,        /* Skip adding / removing object references in
                                                    * response to [return]. */
        TemporaryReturnReference = 0x200000000000, /* Consider all object references added by
                                                    * [return] to be temporary.  Upon returning to
                                                    * the level 0 call frame, these references will
                                                    * be removed, which may result in the object
                                                    * being disposed (i.e. unless it has been saved
                                                    * into a variable). */
        NoRemoveComplain = 0x400000000000,         /* Do not complain if an attempt to remove this
                                                    * opaque object handle fails. */
        PreferMoreMembers = 0x800000000000,        /* When selecting a type from a list of
                                                    * candidates, prefer the one that has more
                                                    * members.  This flag only applies to COM
                                                    * interop objects. */
        PreferSimilarName = 0x1000000000000,       /* When selecting a type from a list of
                                                    * candidates, prefer the one that has the name
                                                    * the most similar to the one originally
                                                    * specified.  This flag only applies to COM
                                                    * interop objects. */
        RejectDissimilarNames = 0x2000000000000,   /* When selecting a type from a list of
                                                    * candidates, reject them all if none has a
                                                    * similar name.  This flag only applies to COM
                                                    * interop objects. */
        NoCase = 0x4000000000000,                  /* Ignore case when comparing strings.  This
                                                    * flag only applies to COM interop objects. */
        AutoFlagsEnum = 0x8000000000000,           /* When possible, attempt to automatically use
                                                    * the "flags" enumeration handling for fields
                                                    * set via [object invoke]. */
        AllowProxyGetType = 0x10000000000000,      /* Allow object to be queried via GetType even
                                                    * if it is a transparent proxy. */
        ForceProxyGetType = 0x20000000000000,      /* Force object to be queried via GetType even
                                                    * if it is a transparent proxy based on a type
                                                    * that is not loaded into the AppDomain. */
        ManualProxyGetType = 0x40000000000000,     /* Fallback to using the value of the -proxytype
                                                    * option if a transparent proxy is detected. */
        ReturnAlias = 0x80000000000000,            /* If an object alias is added, use it as the
                                                    * return value instead of the object name. */
        NullForProxyType = 0x100000000000000,      /* When dealing with an opaque object handle
                                                    * that is a transparent proxy, ignore the
                                                    * stored IObject type. */
        Reserved1 = 0x200000000000000,             /* This flag bit is reserved and will not be
                                                    * used outside of this class. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        SelectTypeMask = PreferMoreMembers | PreferSimilarName | RejectDissimilarNames,

        AllowProxyGetTypeMask = Runtime | WellKnown | AllowProxyGetType,
        ForceProxyGetTypeMask = Runtime | WellKnown | ForceProxyGetType,

        ForNullObject = Locked | Safe | NullObject, /* This mask is for use when adding the
                                                     * "null" opaque object handle only. */

        NoComObject = NoComObjectLookup | NoComObjectReturn, /* Skip all special handling for
                                                              * the COM object proxy type
                                                              * (i.e. "System.__ComObject").
                                                              */

        Callback = NoDispose | Reserved1, /* Default flags used when creating a
                                           * CommandCallback object. */

        Default = ForceDelete | Reserved1 /* Default flags used by [object create], [object
                                           * foreach], [object get], [object invoke], [object
                                           * load], etc. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a82bd131-a778-4856-a03e-d09e470a83c7")]
    public enum ProcedureFlags
    {
        None = 0x0,
        Invalid = 0x1,                 /* Invalid, do not use. */
        Core = 0x2,                    /* This procedure is included with the runtime. */
        Library = 0x4,                 /* This procedure is part of the script library. */
        Interactive = 0x8,             /* This procedure is included with the interactive shell.
                                        */
        Disabled = 0x10,               /* The procedure may not be executed. */
        Hidden = 0x20,                 /* The procedure may only be executed if allowed by policy.
                                        */
        ReadOnly = 0x40,               /* The procedure may not be modified nor removed. */
        Safe = 0x80,                   /* Procedure is "safe" to execute for partially trusted
                                        * and/or untrusted scripts. */
        Unsafe = 0x100,                /* Procedure is NOT "safe" to execute for partially trusted
                                        * and/or untrusted scripts. */
        Breakpoint = 0x200,            /* Break into debugger upon entry and exit. */
        ScriptLocation = 0x400,        /* Use the previously pushed script location when evaluating
                                        * the procedure body. */
        Private = 0x800,               /* The procedure may be executed only from within the file
                                        * it was defined in.  If the procedure was not defined in
                                        * a file, it may only be executed outside the context of
                                        * a file. */
        NoReplace = 0x1000,            /* Attempts to replace the procedure are silently ignored. */
        NoRename = 0x2000,             /* Attempts to rename the procedure are silently ignored. */
        NoRemove = 0x4000,             /* Attempts to remove the procedure are silently ignored. */
        Fast = 0x8000,                 /* Traces, watchpoints, and similar mechanisms are disabled
                                        * by default for all local variables. */
        Atomic = 0x10000,              /* The interpreter lock should be held while the procedure
                                        * is running. */
        PositionalArguments = 0x20000, /* The procedure uses positional arguments.  The special
                                        * "args" argument is supported. */
        NamedArguments = 0x40000,      /* The procedure uses named arguments instead of positional
                                        * arguments.  Any named argument with an associated default
                                        * value is optional.  The special "args" argument is still
                                        * supported.  There must be an even number of arguments to
                                        * call the procedure as they represent name/value pairs. */
        Obfuscated = 0x80000,          /* The procedure has been obfuscated by a plugin (i.e. its
                                        * body and argument names will be unavailable). */

#if ARGUMENT_CACHE || PARSE_CACHE
        NonCaching = 0x100000,         /* Disable caching for the body of the procedure.  The exact
                                        * cache(s) that is/are disabled is unspecified and subject
                                        * to change in the future. */
#endif

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b2e5ed96-4dac-4070-bbf5-51464b28c4d5")]
    public enum PluginFlags : ulong
    {
        None = 0x0,                               /* No special handling. */
        Invalid = 0x1,                            /* Invalid, do not use. */
        Reserved1 = 0x2,                          /* Reserved, do not use. */
        Primary = 0x4,                            /* The plugin is the primary one
                                                   * for the containing assembly. */
        System = 0x8,                             /* The plugin is a system plugin
                                                   * (i.e. part of the Eagle
                                                   * runtime). */
        Host = 0x10,                              /* The plugin contains a custom
                                                   * host. */
        Debugger = 0x20,                          /* The plugin contains a script
                                                   * debugger (reserved for future
                                                   * use). */
        User = 0x40,                              /* The plugin is a user plugin
                                                   * (i.e. from a third-party
                                                   * vendor). */
        Commercial = 0x80,                        /* The plugin is part of a
                                                   * commercial product, this
                                                   * implies but does not explicitly
                                                   * state that the plugin is closed
                                                   * source and/or is not licensed
                                                   * under a Tcl-style license. */
        Proprietary = 0x100,                      /* The plugin contains proprietary
                                                   * code, this implies but does not
                                                   * explicitly state that the
                                                   * plugin is closed source and/or
                                                   * is not licensed under a
                                                   * Tcl-style license. */
        Command = 0x200,                          /* The plugin contains one or more
                                                   * custom commands. */
        Function = 0x400,                         /* The plugin contains one or more
                                                   * custom [expr] functions. */
        Trace = 0x800,                            /* The plugin traces variables
                                                   * (interpreter-wide and/or
                                                   * specific variables). */
        Notify = 0x1000,                          /* The plugin listens for
                                                   * notifications. */
        Policy = 0x2000,                          /* The plugin contains one or more
                                                   * policies.  Setting this flag
                                                   * requires the Primary flag to be
                                                   * set as well. */
        Resolver = 0x4000,                        /* The plugin contains one or more
                                                   * command and/or variable
                                                   * resolvers. */
        Static = 0x8000,                          /* The plugin was provided
                                                   * "statically" by the
                                                   * application. */
        Demand = 0x10000,                         /* The plugin was loaded on-demand
                                                   * by the [load] command. */
        UnsafeCode = 0x20000,                     /* The plugin contains, calls, or
                                                   * refers to unsafe code. */
        NativeCode = 0x40000,                     /* The plugin contains, calls, or
                                                   * refers to native code. */
        SafeCommands = 0x80000,                   /* The plugin ONLY contains
                                                   * commands which are "safe" when
                                                   * executed by untrusted or
                                                   * marginally trusted scripts.
                                                   * Currently, this flag is never
                                                   * set by Eagle itself; however,
                                                   * plugin authors are encouraged
                                                   * to set this flag for their
                                                   * plugins if all their
                                                   * functionality is safe for use
                                                   * by untrusted or marginally
                                                   * trusted scripts.  In the
                                                   * future, Eagle may make more
                                                   * extensive use of this flag.
                                                   */
        MergeCommands = 0x100000,                 /* When adding commands from the
                                                   * plugin, ignore those that are
                                                   * already present. */
        OverwriteCommands = 0x200000,             /* When adding commands, overwrite
                                                   * existing ones. */
        MergeProcedures = 0x400000,               /* When adding procedures from the
                                                   * plugin, ignore those that are
                                                   * already present. */
        OverwriteProcedures = 0x800000,           /* When adding procedures,
                                                   * overwrite existing ones. */
        MergePolicies = 0x1000000,                /* When adding policies from the
                                                   * plugin, ignore those that are
                                                   * already present. */
        OverwritePolicies = 0x2000000,            /* When adding policies, overwrite
                                                   * existing ones. */
        Test = 0x4000000,                         /* The plugin is primarily for
                                                   * unit testing purposes. */
        UserInterface = 0x8000000,                /* The plugin contains a user
                                                   * interface. */
        NoInitialize = 0x10000000,                /* The default plugin should not
                                                   * perform initialization logic on
                                                   * behalf of the plugin. */
        NoTerminate = 0x20000000,                 /* The default plugin should not
                                                   * perform termination logic on
                                                   * behalf of the plugin. */
        NoCommands = 0x40000000,                  /* The default plugin should not
                                                   * add commands on behalf of the
                                                   * plugin. */
        NoFunctions = 0x80000000,                 /* The default plugin should not
                                                   * add functions on behalf of the
                                                   * plugin. */
        NoPolicies = 0x100000000,                 /* The default plugin should not
                                                   * add policies on behalf of the
                                                   * plugin.
                                                   */
        NoTraces = 0x200000000,                   /* The default plugin should not
                                                   * add traces on behalf of the
                                                   * plugin. */
        NoProvide = 0x400000000,                  /* The notify plugin should not
                                                   * provide the package on behalf
                                                   * of the plugin. */
        NoResources = 0x800000000,                /* The plugin does not have any
                                                   * scripting resources and should
                                                   * not be queried by the
                                                   * interpreter via any resource
                                                   * manager it may contain. */
        NoAuxiliaryData = 0x1000000000,           /* The plugin does not have any
                                                   * auxiliary data and a dictionary
                                                   * should not be created. */
        NoInitializeFlag = 0x2000000000,          /* The default plugin should not
                                                   * set or reset the initialized
                                                   * flag. */
        NoResult = 0x4000000000,                  /* The default plugin should not
                                                   * set the result from within the
                                                   * IState.Initialize and
                                                   * IState.Terminate methods. */
        NoGetStream = 0x8000000000,               /* The core host should not call
                                                   * the GetStream method for the
                                                   * plugin. */
        NoGetString = 0x10000000000,              /* The core host should not call
                                                   * the GetString method for the
                                                   * plugin. */
        StrongName = 0x20000000000,               /* The plugin assembly has a
                                                   * StrongName signature.  May not
                                                   * be 100% reliable.  WARNING: DO
                                                   * NOT SET THIS FLAG MANUALLY AND
                                                   * DO NOT MAKE SECURITY DECISIONS
                                                   * BASED ON THE PRESENCE OR
                                                   * ABSENCE OF THIS FLAG. */
        Verified = 0x40000000000,                 /* The StrongName signature has
                                                   * been "verified" via the CLR
                                                   * native API.  May not be 100%
                                                   * reliable.  WARNING: DO NOT SET
                                                   * THIS FLAG MANUALLY AND DO NOT
                                                   * MAKE SECURITY DECISIONS BASED
                                                   * ON THE PRESENCE OR ABSENCE OF
                                                   * THIS FLAG. */
        VerifiedOnly = 0x80000000000,             /* The StrongName signature must
                                                   * be "verified" via the CLR
                                                   * native API before any plugin
                                                   * file can be loaded.  This may
                                                   * not be 100% reliable. */
        SkipVerified = 0x100000000000,            /* The StrongName signature
                                                   * checking was skipped. */
        Authenticode = 0x200000000000,            /* The plugin assembly has an
                                                   * Authenticode signature.  May
                                                   * not be 100% reliable.  WARNING:
                                                   * DO NOT SET THIS FLAG MANUALLY
                                                   * AND DO NOT MAKE SECURITY
                                                   * DECISIONS BASED ON THE PRESENCE
                                                   * OR ABSENCE OF THIS FLAG. */
        Trusted = 0x400000000000,                 /* The Authenticode signature and
                                                   * certificate appear to be
                                                   * "trusted" by the operating
                                                   * system.  May not be 100%
                                                   * reliable.  WARNING: DO NOT SET
                                                   * THIS FLAG MANUALLY AND DO NOT
                                                   * MAKE SECURITY DECISIONS BASED
                                                   * ON THE PRESENCE OR ABSENCE OF
                                                   * THIS FLAG. */
        TrustedOnly = 0x800000000000,             /* The Authenticode signature and
                                                   * certificate must be "trusted"
                                                   * by the operating system before
                                                   * any plugin file can be loaded.
                                                   * This may not be 100% reliable.
                                                   */
        SkipTrusted = 0x1000000000000,            /* The Authenticode signature and
                                                   * certificate checking was
                                                   * skipped. */
        SkipTerminate = 0x2000000000000,          /* The IState.Terminate method
                                                   * should be skipped during the
                                                   * UnloadPlugin method that
                                                   * accepts an IPlugin instance. */
#if ISOLATED_PLUGINS
        Isolated = 0x4000000000000,               /* The plugin assembly should be
                                                   * (or has been) loaded into an
                                                   * isolated AppDomain. */
        NoIsolated = 0x8000000000000,             /* Prevent the Isolated flag from
                                                   * being honored. */
        IsolatedOnly = 0x10000000000000,          /* The plugin assembly must be
                                                   * loaded into an isolated
                                                   * AppDomain.  This value is only
                                                   * supported when it is present in
                                                   * the custom attributes for the
                                                   * plugin assembly or type. */
        NoIsolatedOnly = 0x20000000000000,        /* Prevent the IsolatedOnly flag
                                                   * from being honored. */
        NoUseEntryAssembly = 0x40000000000000,    /* Disable resetting the value of
                                                   * the entry assembly when
                                                   * creating a new AppDomain. */
        OptionalEntryAssembly = 0x80000000000000, /* Ignore exceptions when forcibly
                                                   * refreshing the entry assembly
                                                   * in created AppDomains. */
        VerifyCoreAssembly = 0x100000000000000,   /* Make sure the AppDomain base
                                                   * directory contains the core
                                                   * library assembly? */
#if SHELL
        UpdateCheck = 0x200000000000000,          /* Check for updates to plugins
                                                   * just prior to loading them
                                                   * (i.e. via their configured
                                                   * "update" URIs).  If the update
                                                   * process fails, just fallback to
                                                   * loading the one that is already
                                                   * present locally. */
        NoUpdateCheck = 0x400000000000000,        /* Prevent the UpdateCheck flag
                                                   * from being honored. */
#endif
        NoPreview = 0x800000000000000,            /* Disable loading plugin
                                                   * assemblies into a "preview"
                                                   * context in order to determine
                                                   * their metadata (e.g. plugin
                                                   * flags). */
#endif
        SimpleName = 0x1000000000000000,          /* Enable using the simple plugin
                                                   * name when adding the associated
                                                   * package. */
        Verbose = 0x2000000000000000,             /* Enable verbose output during
                                                   * plugin loading/unloading? */
        Licensed = 0x4000000000000000,            /* The plugin is a licensed
                                                   * component and that license has
                                                   * been verified. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved2 = 0x8000000000000000,           /* The flag is reserved for future
                                                   * use and must not be set. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
#if SHELL
        //
        // NOTE: By default, prevent any plugin update checks from
        //       running unless the interactive loop is invoked.
        //       This could be very important because plugin update
        //       checks could interfere with a script (file?) being
        //       evaluated via the command line.
        //
        NonInteractiveMask = OptionalEntryAssembly | NoUpdateCheck | Reserved1,
        MaybeVerifyCoreAssembly = VerifyCoreAssembly | Reserved1,
#else
        NonInteractiveMask = OptionalEntryAssembly | Reserved1,
        MaybeVerifyCoreAssembly = VerifyCoreAssembly | Reserved1,
#endif
#else
        NonInteractiveMask = None | Reserved1,
        MaybeVerifyCoreAssembly = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = (NonInteractiveMask | MaybeVerifyCoreAssembly) & ~Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("eb0e3225-f87c-4f47-b30e-6ab38548cd7c")]
    public enum PackageIndexFlags
    {
        None = 0x0,                /* No special handling. */
        Invalid = 0x1,             /* Invalid, do not use. */
        Reserved = 0x2,            /* Reserved, do not use. */
        PreferFileSystem = 0x4,    /* Check the file system before checking the
                                    * interpreter host. */
        PreferHost = 0x8,          /* Check the interpreter host before checking
                                    * the file system. */
        Host = 0x10,               /* Use the interpreter host to find the
                                    * package index. */
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        Plugin = 0x20,             /* Use the external file system to find
                                    * candidate plugin assemblies and probe
                                    * them for their package indexes. */
#endif
        Normal = 0x40,             /* Use the external file system to find
                                    * the package index. */
        NoNormal = 0x80,           /* Forbid using the file system to find
                                    * the package index.  This flag is only
                                    * effective when using the interpreter
                                    * host to find the package index. */
        Recursive = 0x100,         /* Search all sub-directories as well? */
        Refresh = 0x200,           /* Force package index to be re-found
                                    * and re-evaluated. */
        Resolve = 0x400,           /* Resolve the fully qualified file name
                                    * for the package index script. */
        Trace = 0x800,             /* Enable tracing of key package index
                                    * operations. */
        Verbose = 0x1000,          /* Enable verbose tracing output when
                                    * processing package index scripts. */
        Found = 0x2000,            /* The package index script was found
                                    * and processed. */
        Locked = 0x4000,           /* This flag is no longer used. */
        Safe = 0x8000,             /* Evaluate the package index script in
                                    * "safe" mode. */
        Evaluated = 0x10000,       /* The package index script was actually
                                    * evaluated. */
        NoComplain = 0x20000,      /* If a package index script fails, just
                                    * ignore the error. */
        NoFileError = 0x40000,     /* If GetFiles throws an exception, just
                                    * fail. */
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        StopOnError = 0x80000,     /* When processing (sub?) package index
                                    * scripts, stop on the first error? */
#endif
#if NATIVE
        NoTrusted = 0x100000,      /* When processing plugin assemblies, do
                                    * not check if they are signed using an
                                    * Authenticode certificate.
                                    */
        NoVerified = 0x200000,     /* When processing plugin assemblies, do
                                    * not check if they are signed using a
                                    * StrongName key.*/
#endif
        AllowDuplicate = 0x400000, /* Skip removing "logically duplicate"
                                    * package index file names when doing
                                    * a refresh operation. */
        NoSort = 0x800000,         /* Do not sort the package index file
                                    * names when evaluating them. */
        WhatIf = 0x1000000,        /* Running in "what-if" mode, do not
                                    * modify any persistent interpreter
                                    * state. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        MaybePlugin = Plugin,
        NonPluginMask = Host | Normal,
#else
        MaybePlugin = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        MaybeNoTrusted = NoTrusted,
        MaybeNoVerified = NoVerified,
#else
        MaybeNoTrusted = None,
        MaybeNoVerified = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NonHostMask = Normal | MaybePlugin,
        NonNormalMask = Host | MaybePlugin,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the value used by the auto-path variable trace
        //       callback.
        //
#if DEBUG
        AutoPath = Host | Normal | NoNormal |
                   Recursive | Trace | MaybeNoTrusted |
                   MaybeNoVerified | NoSort, /* TODO: Good default? */
#else
        AutoPath = Host | Normal | NoNormal |
                   Recursive | Trace | NoSort, /* TODO: Good default? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used when finding and loading the Harpy / Badge
        //       package index scripts.  This is only done in response to the
        //       "-security" command line option -OR- by calling the ScriptOps
        //       EnableOrDisableSecurity method.
        //
        SecurityPackage = (AutoPath | Safe) & ~(Host | Recursive),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("753ee5b0-e42b-4c69-b398-960f54ba75dd")]
    public enum PackageFlags
    {
        None = 0x0,            /* Nothing special. */
        Invalid = 0x1,         /* Invalid, do not use. */
        System = 0x2,          /* The package is a system package, do not use. */
        Loading = 0x4,         /* The package is currently being loaded via
                                * PackageRequire. */
        Static = 0x8,          /* The package was provided statically. */
        Core = 0x10,           /* The package is included with the runtime. */
        Plugin = 0x20,         /* The package was provided by a plugin. */
        Library = 0x40,        /* The package is part of the script library. */
        Interactive = 0x80,    /* The package is included with the interactive
                                * shell. */
        Automatic = 0x100,     /* The package was added to the interpreter
                                * automatically. */
        NoUpdate = 0x200,      /* Skip updating package flags upon provide. */
        NoProvide = 0x400,     /* The [package provide] sub-command should
                                * always do nothing. */
        AlwaysSatisfy = 0x800, /* The [package vsatisfies] sub-command should
                                * always return true. */
        KeepExisting = 0x1000, /* The [package ifneeded] sub-command should
                                * preserve existing package information. */
        FailExisting = 0x2000, /* The [package ifneeded] sub-command should
                                * fail if existing package information is
                                * present. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used when evaluating the Harpy / Badge package
        //       indexes prior to the interpreter being initialized.  This is
        //       only done in response to the "-security" command line option
        //       -OR- by calling the ScriptOps.EnableOrDisableSecurity method.
        //
        SecurityPackageMask = NoProvide | AlwaysSatisfy,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("864f7d71-9202-4e44-aa42-0d1c9d3468ff")]
    public enum EventWaitFlags
    {
        None = 0x0,
        Invalid = 0x1,
        NoBgError = 0x2,
        NoManagerEvents = 0x4,
        NoTimeout = 0x8,
        NoCancel = 0x10,
        NoGlobalCancel = 0x20,
        StopOnError = 0x40,
        ErrorOnEmpty = 0x80,
        UserInterface = 0x100,
        NoUserInterface = 0x200,
        NoEvents = 0x400,
        NoWindows = 0x800,
        NoWait = 0x1000,
        NoSleep = 0x2000,
        NoComplain = 0x4000,
        StopOnComplain = 0x8000,
        StopOnGlobalComplain = 0x10000,
        OnlyWaiting = 0x20000,
        OnlyExists = 0x40000,
        DoOneEvent = 0x80000,
        FollowLink = 0x100000,
        Trace = 0x200000,

#if NATIVE && TCL
        TclDoOneEvent = 0x400000,
        TclWaitEvent = 0x800000, // NOTE: This flag should rarely, if ever, be used.
        TclAllEvents = 0x1000000,
#endif

        ForDefault = 0x2000000,

        LegacyVariableMask = FollowLink | Trace, // COMPAT: Eagle beta.

        StopOnAnyComplain = StopOnComplain | StopOnGlobalComplain,
        StopOnAny = StopOnError | StopOnComplain | StopOnGlobalComplain,

        Default = DoOneEvent | LegacyVariableMask | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c5c70166-aa98-4f6f-8d08-1e9099b0aa11")]
    public enum VariableFlags : ulong
    {
        /* general flags (instanced) */

        None = 0x0,
        Invalid = 0x1,            /* Invalid, do not use. */
        Array = 0x2,              /* variable is an array */
        ReadOnly = 0x4,           /* cannot be modified by a script */
        WriteOnly = 0x8,          /* cannot be read by a script */
        Virtual = 0x10,           /* cannot be used with [array get], [array names],
                                   * [array values], [array set], [array size],
                                   * [array startsearch], or [array unset]. */
        System = 0x20,            /* pre-defined script library variable */
        Invariant = 0x40,         /* cannot be modified by a script (reserved) */
        Mutable = 0x80,           /* variable can be modified even in an immutable
                                   * interpreter. */
        Safe = 0x100,             /* variable can be used in a "safe" interpreter. */
        Unsafe = 0x200,           /* variable CANNOT be used in a "safe" interpreter. */
        Link = 0x400,             /* global or upvar alias */
        Undefined = 0x800,        /* declared via global or upvar but not yet set */
        Argument = 0x1000,        /* variable is a formal procedure argument */
        Global = 0x2000,          /* variable is global */
        Local = 0x4000,           /* variable is local to the current procedure */
        Wait = 0x8000,            /* variable is being waited on by the interpreter. */
        Dirty = 0x10000,          /* dirty bit, variable has changed (including unset) */
        NoWatchpoint = 0x20000,   /* disable watches for this variable. */
        NoTrace = 0x40000,        /* disable read/write traces for this variable. */
        NoNotify = 0x80000,       /* disable event notifications for this variabe. */
        NoPostProcess = 0x100000, /* disable post-processing of the variable value. */
        BreakOnGet = 0x200000,    /* break into debugger on get access */
        BreakOnSet = 0x400000,    /* break into debugger on set access */
        BreakOnUnset = 0x800000,  /* break into debugger on unset access */
        Substitute = 0x1000000,   /* subst the variable value prior to returning it */
        Evaluate = 0x2000000,     /* eval the variable value prior to returning it */

        /* "action" and/or "result" flags (non-instanced) */

        NotFound = 0x4000000,                 /* the variable was searched for, but was not found;
                                               * otherwise, the variable was not searched for
                                               * because the name was invalid */
        NoCreate = 0x8000000,                 /* do not create a new variable, only modify
                                               * existing an existing one */
        GlobalOnly = 0x10000000,              /* get or set variable only within the global call
                                               * frame */
        NoArray = 0x20000000,                 /* variable name cannot refer to an array */
        NoElement = 0x40000000,               /* variable name cannot be an element reference */
        NoComplain = 0x80000000,              /* unset without raising "does not exist" errors */
        AppendValue = 0x100000000,            /* append to the value instead of setting */
        AppendElement = 0x200000000,          /* append to the list instead of setting */
        NoFollowLink = 0x400000000,           /* operate on variable link, not the variable itself */
        ResetValue = 0x800000000,             /* reset value(s) to null when unset */

#if !MONO && NATIVE && WINDOWS
        ZeroString = 0x1000000000,            /* Upon [unset], enable forcibly zeroing strings
                                               * that may contain "sensitive" data?  WARNING: THIS
                                               * IS NOT GUARANTEED TO WORK RELIABLY ON ALL PLATFORMS.
                                               * EXTREME CARE SHOULD BE EXERCISED WHEN HANDLING
                                               * ANY SENSITIVE DATA, INCLUDING TESTING THAT THIS
                                               * FLAG WORKS WITHIN THE SPECIFIC TARGET APPLICATION
                                               * AND ON THE SPECIFIC TARGET PLATFORM. */
#endif

        Purge = 0x2000000000,                 /* purge deleted variables in call frame on unset */
        NoSplit = 0x4000000000,               /* skip splitting the variable name from the array
                                               * element index. */
        NoReady = 0x8000000000,               /* force skip of check if interpreter is ready */
        NoRemove = 0x10000000000,             /* do not remove variable from the call frame */
        NoGetArray = 0x20000000000,           /* do not validate the variable as an array or
                                               * non-array */
        NoLinkIndex = 0x40000000000,          /* variable cannot have a valid link index. */
        HasLinkIndex = 0x80000000000,         /* the variable has a valid link index and it should
                                               * not (i.e. we do not want an alias to an array
                                               * element). */
        Defined = 0x100000000000,             /* validate that the variable is not undefined */
        NoObject = 0x200000000000,            /* skip opaque object handle processing. */
        SkipWatchpoint = 0x400000000000,      /* skip all variable breakpoints for the duration of
                                               * this method call. */
        SkipTrace = 0x800000000000,           /* skip all variable traces for the duration of this
                                               * method call. */
        SkipNotify = 0x1000000000000,         /* skip all event notifications for the duration of
                                               * this method call. */
        SkipPostProcess = 0x2000000000000,    /* skip post-processing the variable value for this
                                               * method call.  */
        ResolveNull = 0x4000000000000,        /* allow variable resolvers to return a null
                                               * variable along with a successful return code. */
        NonVirtual = 0x8000000000000,         /* disallow returning virtual variables. */
        WasVirtual = 0x10000000000000,        /* the variable was virtual and it should not be. */
        WasElement = 0x20000000000000,        /* the variable name refers to an array element. */
        NewTraceInfo = 0x40000000000000,      /* force the creation of a new TraceInfo object
                                               * instead of using the pre-allocated one for the
                                               * thread. */
        SkipToString = 0x80000000000000,      /* skip converting the variable value to be returned
                                               * to a string. */
        ForceToString = 0x100000000000000,    /* force conversion of the variable value to string
                                               * prior to being returned. */
        FallbackToString = 0x200000000000000, /* as a fallback, when used with SkipToString, allow
                                               * the variable value to be returned as a string if
                                               * it does not conform to a supported type. */
        CreateMissing = 0x400000000000000,    /* Create the variable if it is missing, but do not
                                               * add it to the target call frame. */
        WasMissing = 0x800000000000000,       /* The variable was missing from the call frame and
                                               * a new (dummy) variable was returned. */
        NoUsable = 0x1000000000000000,        /* skip checking if the variable is actually usable
                                               * from the current thread. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved1 = 0x2000000000000000,       /* Reserved value, do not use. */
        Reserved2 = 0x4000000000000000,       /* Reserved value, do not use. */
        ReservedMask = Reserved1 | Reserved2,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /* internal library use only */

        Library = System | GlobalOnly,

        /* virtual / system mask (instanced) */

        VirtualOrSystemMask = Virtual | System,

        /* watch flags mask (instanced) */

        BreakOnAny = BreakOnGet | BreakOnSet | BreakOnUnset,
        WatchpointMask = BreakOnAny | Mutable,

        /* no watch/trace/notify/post-process flags (instanced and non-instanced) */

        NonWatchpoint = NoWatchpoint | SkipWatchpoint,
        NonTrace = NoTrace | SkipTrace,
        NonNotify = NoNotify | SkipNotify,
        NonPostProcess = NoPostProcess | SkipPostProcess,

        /* "action" and/or "result" flags (non-instanced) */

        NonInstanceMask = NotFound | NoCreate | GlobalOnly | NoArray |
                          NoElement | NoComplain | AppendValue | AppendElement |
                          NoFollowLink | ResetValue |
#if !MONO && NATIVE && WINDOWS
                          ZeroString |
#endif
                          Purge | NoSplit | NoReady |
                          NoRemove | NoGetArray | NoLinkIndex | HasLinkIndex |
                          Defined | NoObject | SkipWatchpoint | SkipTrace |
                          SkipNotify | SkipPostProcess | ResolveNull | NonVirtual |
                          WasVirtual | WasElement | NewTraceInfo | SkipToString |
                          ForceToString | FallbackToString | CreateMissing | WasMissing |
                          NoUsable | ReservedMask,

        /* flags not allowed when adding variables */

        NonAddMask = Link | Global | Local | Dirty |
                     NonInstanceMask,

        /* flags not allowed when setting variable values */

        NonSetMask = Array | Virtual | Undefined | NonAddMask,

        /* flags masked off when a variable is recycled */

        NonDefinedMask = ReadOnly | WriteOnly | System | Invariant |
                         Mutable | Safe | Unsafe | Argument |
                         Wait | Dirty | NoWatchpoint | NoTrace |
                         NoNotify | NoPostProcess | BreakOnGet | BreakOnSet |
                         BreakOnUnset | Substitute | Evaluate | NonSetMask,

        /* flags used when querying/setting a raw variable value */

        DirectValueMask = Reserved1 | None,
        DirectGetValueMask = DirectValueMask | SkipToString,
        DirectSetValueMask = DirectValueMask,

        /* flags when the variable is being get/set automatically via a
         * property setter method, the engine, or the interactive loop. */

        ViaProperty = NoReady,
        ViaShell = NoReady,
        ViaEngine = NoReady,
        ViaPrompt = GlobalOnly | NoReady,

        /* flags allowed when the interpreter is read-only and/or immutable */

        ReadOnlyMask = System,
        ImmutableMask = System | Mutable,

        /* used for [array], etc. */

        CommonCommandMask = Defined | NonVirtual,
        ArrayCommandMask = CommonCommandMask | NoElement | NoLinkIndex,

        /* used for GetVariable result flags checking (failure reason) */

        ArrayErrorMask = NotFound | HasLinkIndex,

        /* these flags are set and cleared by GetVariable */

        ErrorMask = ArrayErrorMask | WasVirtual,

        /* flags for maximum performance at the expense of everything else */

        FastMask = NoWatchpoint | NoTrace | NoNotify |
                   NoPostProcess,

        FastTraceMask = NoWatchpoint | NoNotify | NoPostProcess,

        /* flags for use by [unset -zerostring] / [unset -maybezerostring] */

#if !MONO && NATIVE && WINDOWS
        ZeroStringMask = ResetValue | ZeroString,
#endif

        /* flags for use by [namespace which] */

        NamespaceWhichMask = NoElement | Defined,
        GlobalNamespaceWhichMask = GlobalOnly | NamespaceWhichMask,

        /* non-instanced flags for maximum performance */

        FastNonInstanceMask = NoSplit | SkipWatchpoint | SkipTrace |
                              SkipNotify | SkipPostProcess,

        FastNonInstanceTraceMask = NoSplit | SkipWatchpoint | SkipNotify |
                                   SkipPostProcess
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8ed86944-4396-4106-ae22-6c0a0e95c7fc")]
    public enum CallFrameFlags : ulong
    {
        None = 0x0,                  /* No special handling. */
        Invalid = 0x1,               /* Invalid, do not use. */
        NoFree = 0x8,                /* This call frame should not be freed
                                      * via the Free() method unless the
                                      * "global" parameter is true.  This
                                      * flag may be set or unset by the
                                      * resolver. */
        Engine = 0x10,               /* Used to indicate a call frame pushed
                                      * by the engine. */
        Global = 0x20,               /* Used to indicate the outermost call
                                      * frame. */
        GlobalScope = 0x40,          /* Used for supporting the [scope global]
                                      * sub-command and its associated code. */
        Procedure = 0x80,            /* Used when a procedure body is being
                                      * evaluated. */
        After = 0x100,               /* Used for the [after] command. */
        Uplevel = 0x200,             /* Used for the [uplevel] command. */
        Downlevel = 0x400,           /* Used for the [downlevel] command. */
        Lambda = 0x800,              /* Used for the [apply] command. */
        Namespace = 0x1000,          /* Used for the [namespace] command. */
        Scope = 0x2000,              /* Used for the [scope] command. */
        Evaluate = 0x4000,           /* Used for the [debug lockeval],
                                      * [eval], [interp eval], and
                                      * [namespace eval] commands as well
                                      * as the -locked option to the
                                      * [vwait] command. */
        InScope = 0x8000,            /* Used for the [namespace inscope]
                                      * command. */
        Source = 0x10000,            /* Used for the [source] command. */
        Substitute = 0x20000,        /* Used for the [subst] command. */
        Expression = 0x40000,        /* Used for the [expr] command. */
        BackgroundError = 0x80000,   /* Used for the [bgerror] command. */
        Try = 0x100000,              /* Used for the [try] command. */
        Catch = 0x200000,            /* Used for the [catch] command. */
        Interpreter = 0x400000,      /* Used for the [interp] command. */
        Test = 0x800000,             /* Used for the [test1] / [test2]
                                      * commands and the dedicated test
                                      * class. */
        Interactive = 0x1000000,     /* Used for interactive extension
                                      * command execution. */
        Alias = 0x2000000,           /* Used for aliases. */
        Finally = 0x4000000,         /* Used for the finally block of a
                                      * [try] command. */
        Tcl = 0x8000000,             /* The script or file is being
                                      * evaluated via Tcl. */
        External = 0x10000000,       /* Used to indicate the frame is for
                                      * executing commands from external
                                      * components (i.e. those outside of
                                      * the engine). */
        Tracking = 0x20000000,       /* Used to indicate the frame is for
                                      * tracking purposes. */
        UseNamespace = 0x40000000,   /* Used to indicate the frame points to
                                      * a namespace with variables in the
                                      * call frame owned by that
                                      * namespace. */
        Invisible = 0x80000000,      /* Used to indicate the frame should be
                                      * skipped for [uplevel]. */
        NoInvisible = 0x100000000,   /* Used to indicate the frame should be
                                      * able to see other frames that have
                                      * been marked as invisible (e.g. via
                                      * [info level]). */
        Undefined = 0x200000000,     /* Used in call frame variable cleanup,
                                      * indicates that all variables
                                      * belonging to the call frame are now
                                      * undefined. */
        Automatic = 0x400000000,     /* This flag is used exclusively by the
                                      * engine code to detect call frames
                                      * that should be automatically popped
                                      * upon exiting from the core execution
                                      * routine. */
        Fast = 0x800000000,          /* This call frame disables variable
                                      * tracing and other things (e.g. debug
                                      * watches, etc) which can potentially
                                      * take a long time.*/
        SubCommand = 0x1000000000,   /* Used for special sub-command execution.
                                      */
        Command = 0x2000000000,      /* Used for special command execution. */
        Debugger = 0x4000000000,     /* Used by the script debugger. */
        Restricted = 0x8000000000,   /* Special restrictions are applied to
                                      * evaluated scripts, some scripts may
                                      * not work. */
        Application = 0x10000000000, /* Reserved application-defined flag. */
        User = 0x20000000000,        /* Reserved user-defined flag. */

        /* these flags are toggled when marking/unmarking global scope frames */

        GlobalScopeMask = NoFree | GlobalScope,

        /* these frame types MAY "own" variables */

        Variables = Global | Procedure | Lambda | Namespace | Scope | GlobalScope,

        /* these frame types MAY be counted for [info level] */

        InfoLevel = Variables & ~(Global | GlobalScope | Lambda),

        /* these frame types MAY NOT "own" variables */

        NoVariables = Engine | Tracking
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("35136e42-f080-484a-9e4f-871888d49e2b")]
    public enum DetailFlags : ulong
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EmptySection = 0x2,
        EmptyContent = 0x4,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VerboseSection = 0x8,
        VerboseContent = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ICallFrameNameOnly = 0x20,
        ICallFrameToListAll = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CallFrameLinked = 0x80,
        CallFrameSpecial = 0x100,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CallFrameVariables = 0x200,
        CallStackAllFrames = 0x400,
        DebuggerBreakpoints = 0x800,
        HostDimensions = 0x1000,
        HostFormatting = 0x2000,
        HostColors = 0x4000,
        HostNames = 0x8000,
        HostState = 0x10000,
        EngineNative = 0x20000,
        TraceCached = 0x40000,
        VariableLinks = 0x80000,
        VariableSearches = 0x100000,
        VariableElements = 0x200000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InterpreterDisposedException = 0x400000,
        ListOps = 0x800000,
        CommandCallback = 0x1000000,
        CommandCallbackWrapper = 0x2000000,
        ParserOpsData = 0x4000000,
        EngineThread = 0x8000000,
        FactoryOps = 0x10000000,
        HashOps = 0x20000000,
        ProcessOps = 0x40000000,
        ThreadOps = 0x80000000,
        SetupOps = 0x100000000,
        TraceOps = 0x200000000,
        TraceLimits = 0x400000000,
        ScriptOps = 0x800000000,
        ScriptException = 0x1000000000,
        SyntaxOps = 0x2000000000,
        ConfigurationOps = 0x4000000000,
        TestOps = 0x8000000000,
#if WINFORMS
        StatusFormOps = 0x10000000000,
#endif
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        CacheConfiguration = 0x20000000000,
#if NATIVE
        CacheConfigurationMemoryLoad = 0x40000000000,
#endif
#endif
#if NATIVE && WINDOWS
        NativeConsole = 0x80000000000,
#endif
#if DEBUGGER
        Debugger = 0x100000000000,
#endif
#if NATIVE && TCL && NATIVE_PACKAGE
        NativePackage = 0x200000000000,
#endif
#if NATIVE && NATIVE_UTILITY
        NativeUtility = 0x400000000000,
#endif
#if NATIVE
        NativeStack = 0x800000000000,
#endif
#if XML
        ScriptXmlOps = 0x1000000000000,
#endif
#if TEST
        TraceException = 0x2000000000000,
#endif
#if NETWORK
        WebOps = 0x4000000000000,
        SocketOps = 0x8000000000000,
#endif
        ArrayOps = 0x10000000000000,
        PathOps = 0x20000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ThreadInfo = 0x40000000000000,
#if HISTORY
        HistoryInfo = 0x80000000000000,
#endif
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        ListCacheInfo = 0x100000000000000,
#endif
#if (CACHE_ARGUMENTLIST_TOSTRING || CACHE_STRINGLIST_TOSTRING) && CACHE_STATISTICS
        StringCacheInfo = 0x200000000000000,
#endif
        CertificateCacheInfo = 0x400000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        User = 0x800000000000000,      /* Indicates that the detail flags have been explicitly
                                        * set by the user. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        SectionMask = InterpreterDisposedException | ListOps |
                      CommandCallback | CommandCallbackWrapper | ParserOpsData |
                      EngineThread | FactoryOps | HashOps | ProcessOps | ThreadOps |
                      SetupOps | TraceOps | TraceLimits | ScriptOps | SyntaxOps |
                      ConfigurationOps | TestOps | ScriptException |
#if WINFORMS
                      StatusFormOps |
#endif
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                      CacheConfiguration |
#if NATIVE
                      CacheConfigurationMemoryLoad |
#endif
#endif
#if NATIVE && WINDOWS
                      NativeConsole |
#endif
#if DEBUGGER
                      Debugger |
#endif
#if NATIVE && TCL && NATIVE_PACKAGE
                      NativePackage |
#endif
#if NATIVE && NATIVE_UTILITY
                      NativeUtility |
#endif
#if NATIVE
                      NativeStack |
#endif
#if XML
                      ScriptXmlOps |
#endif
#if TEST
                      TraceException |
#endif
#if NETWORK
                      WebOps | SocketOps |
#endif
                      ArrayOps | PathOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ExtraInfoMask = ThreadInfo |
#if HISTORY
                        HistoryInfo |
#endif
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                        ListCacheInfo |
#endif
#if (CACHE_ARGUMENTLIST_TOSTRING || CACHE_STRINGLIST_TOSTRING) && CACHE_STATISTICS
                        StringCacheInfo |
#endif
                        CertificateCacheInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ICallFrameMask = ICallFrameNameOnly | ICallFrameToListAll,

        CallFrameMask = CallFrameLinked | CallFrameSpecial,

        EmptyMask = EmptySection | EmptyContent,

        VerboseMask = VerboseSection | VerboseContent,

        ContentMask = CallFrameVariables | CallStackAllFrames | DebuggerBreakpoints |
                      HostDimensions | HostFormatting | HostColors |
                      HostNames | HostState | EngineNative | TraceCached |
                      VariableLinks | VariableSearches | VariableElements,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ScriptOnly = EmptyContent | VerboseContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DebugTrace = EmptyContent | VerboseContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveOnly = EmptyContent | VerboseContent,
        InteractiveAll = EmptyContent | VerboseContent | ContentMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = EmptyMask | VerboseMask | ContentMask | SectionMask | ExtraInfoMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Standard = InteractiveAll,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f3d19d7d-e026-435b-9a1b-3ac2dd969fc6")]
    public enum ToStringFlags
    {
        None = 0x0,
        Invalid = 0x1,
        NameAndValue = 0x2,
        NameAndDefault = 0x4,
        Decorated = 0x8,
        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("87619eee-2103-4488-a2ce-59cc8d94059e")]
    public enum ResultFlags
    {
        None = 0x0,              /* No extra flags. */
        Invalid = 0x1,           /* Invalid, do not use. */
        Reserved1 = 0x2,         /* Reserved, do not use. */
        Global = 0x4,            /* The result is globally scoped? */
        Local = 0x8,             /* The result is locally scoped? */
        String = 0x10,           /* Result is a string (always true for now) */
        Exception = 0x20,        /* The result has an exception instance */
        Error = 0x40,            /* The result has error information? */
        Application = 0x80,      /* Application-defined flag. */
        User = 0x100,            /* User-defined flag. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ClientData = 0x1000,     /* When cloning, copy the client data, if
                                  * any. */
        ValueData = 0x2000,      /* When cloning, copy the value data, if
                                  * any. */
        ExtraData = 0x4000,      /* When cloning, copy the extra data, if
                                  * any. */
        CallFrame = 0x8000,      /* When cloning, copy the call frame, if
                                  * any. */
        EngineData = 0x10000,    /* When cloning, copy the engine data, if
                                  * any. */
        StackTrace = 0x20000,    /* When cloning, copy the stack trace, if
                                  * any. */
        IgnoreType = 0x40000,    /* When cloning, use new value even if the
                                  * types do not match. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForList = 0x100000,      /* Used for the ResultList class. */
        ForCopy = 0x200000,      /* Used for the Copy methods. */
        ForReset = 0x400000,     /* Used for the Result methods. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AddRange = 0x1000000,     /* ResultList only: when adding a nested
                                   * ResultList, add as a range of values,
                                   * not as a single value. */
        NoAddRange = 0x2000000,   /* ResultList only: when adding a nested
                                   * ResultList, add as a single value, not
                                   * as a range of values. */
        Squash = 0x4000000,       /* ResultList only: collapse zero and one
                                   * element lists into empty string and/or
                                   * just the element itself. */
        NoSquash = 0x8000000,     /* ResultList only: opposite of Squash. */
        SkipEmpty = 0x10000000,   /* ResultList only: skip null values, null
                                   * strings, and empty string values when
                                   * converting the list of a string. */
        NoSkipEmpty = 0x20000000, /* ResultList only: opposite of SkipEmpty. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CompactListMask = Squash | SkipEmpty | ForList,
        FullListMask = NoSquash | NoSkipEmpty | ForList,
        DefaultListMask = None | ForList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Complaint = Error | StackTrace,
        InternalMask = EngineData | StackTrace,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllMask = String | Error | ClientData |
                  ValueData | ExtraData | CallFrame |
                  InternalMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CopyValue = String | ForCopy,
        CopyError = Error | ForCopy,
        CopyObject = String | Error | ForCopy,
        CopyAll = AllMask | ForCopy,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ResetValue = String | ForReset,
        ResetError = Error | ForReset,
        ResetObject = String | Error | ForReset,
        ResetAll = AllMask | ForReset,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ca16c0e6-08b0-414d-9c1b-d09af3d98a7c")]
    public enum ArgumentFlags
    {
        None = 0x0,
        Invalid = 0x1,        // invalid, do not use.
        Reserved1 = 0x2,      // reserved, do not use.
        Expand = 0x4,         // NOT IMPLEMENTED
        HasDefault = 0x8,     // argument has a default value.
        NamedArgument = 0x10, // if no default value, argument should use name and value
                              // when doing ToString().
        ArgumentList = 0x20,  // argument is part of an "args" argument list.
        Debug = 0x40,         // argument should use name and value when doing ToString().
        NameOnly = 0x80,      // argument should use name only when doing ToString().
        Application = 0x100,  // application-defined flag.
        User = 0x200,         // user-defined flag.

        Zero = 0x1000,        // force zero for fields instead of using default values.

        ResetWithDefault = Reserved1,
        ResetWithZero = Reserved1 | Zero,

        Reserved = unchecked((int)0x80000000),

        ToStringMask = Debug | NameOnly /* flags that impact ToString(). */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
    [Flags()]
    [ObjectId("0c443008-9aec-477d-ab7a-894ad71e4acb")]
    public enum NotifyType : ulong
    {
        None = 0x0,                    /* This is a reserved event value that represents a null
                                        * event. */
        Invalid = 0x1,                 /* The value that represents an invalid event. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Types
        ///////////////////////////////////////////////////////////////////////////////////////////

        Interpreter = 0x2,             /* The event pertains to an IInterpreter object. */
        CallFrame = 0x4,               /* The event pertains to an ICallFrame object. */
        Resolver = 0x8,                /* The event pertains to the resolver (abstract component). */
        Engine = 0x10,                 /* The event pertains to the engine (abstract component). */
        Stream = 0x20,                 /* The event pertains to a stream. */
        File = 0x40,                   /* The event pertains to a file. */

#if XML
        Xml = 0x80,                    /* The event pertains to XML integration. */
        XmlBlock = 0x100,              /* The event pertains to an XML script block. */
#endif

        Expression = 0x200,            /* The event pertains to an expression. */
        Script = 0x400,                /* The event pertains to a script. */
        String = 0x800,                /* The event pertains to a string. */

#if DEBUGGER
        Debugger = 0x1000,             /* The event pertains to the script debugger (abstract
                                        * component). */
#endif

        Variable = 0x2000,             /* The event pertains to an IVariable object. */
        Alias = 0x4000,                /* The event pertains to an IAlias object. */
        IExecute = 0x8000,             /* The event pertains to an IExecute object. */
        HiddenIExecute = 0x10000,      /* The event pertains to an IExecute object. */
        Procedure = 0x20000,           /* The event pertains to an IProcedure object. */
        HiddenProcedure = 0x40000,     /* The event pertains to an IProcedure object. */
        Command = 0x80000,             /* The event pertains to an ICommand object. */
        HiddenCommand = 0x100000,      /* The event pertains to an ICommand object. */
        SubCommand = 0x200000,         /* The event pertains to an ISubCommand object. */
        Operator = 0x400000,           /* The event pertains to an IOperator object. */
        Function = 0x800000,           /* The event pertains to an IFunction object. */
        Plugin = 0x1000000,            /* The event pertains to an IPlugin object. */
        Package = 0x2000000,           /* The event pertains to an IPackage object. */
        Resolve = 0x4000000,           /* The event pertains to an IResolve object. */

#if DATA
        Connection = 0x8000000,        /* The event pertains to an IDbConnection object. */
        Transaction = 0x10000000,      /* The event pertains to an IDbTransaction object. */
#endif

        Callback = 0x20000000,         /* The event pertains to an ICallback object. */

#if EMIT && NATIVE && LIBRARY
        Module = 0x40000000,           /* The event pertains to an IModule object. */
        Delegate = 0x80000000,         /* The event pertains to an IDelegate object. */
#endif

        Idle = 0x100000000,            /* The event pertains to an idle IEvent object. */
        Event = 0x200000000,           /* The event pertains to an IEvent object. */
        Object = 0x400000000,          /* The event pertains to an IObject object. */

#if NATIVE && TCL
        Tcl = 0x800000000,             /* The event pertains to Tcl integration. */
#endif

#if HISTORY
        History = 0x1000000000,        /* The event pertains to command history. */
#endif

        Policy = 0x2000000000,         /* The event pertains to a policy. */
        Trace = 0x4000000000,          /* The event pertains to a trace. */

#if NATIVE && TCL && TCL_THREADS
        Thread = 0x8000000000,         /* The event pertains to a thread. */
#endif

#if APPDOMAINS
        AppDomain = 0x10000000000,     /* The event pertains to an AppDomain. */
#endif

        Library = 0x20000000000,       /* The event pertains to the script library. */

#if SHELL
        Shell = 0x40000000000,         /* The event pertains to the interactive shell. */
#endif

        RuntimeOption = 0x80000000000, /* The event pertains to a runtime option. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Masks
        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = Invalid,
        CheckMask = All,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // All
        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Interpreter |
              CallFrame |
              Resolver |
              Engine |
              Stream |
              File |

#if XML
              Xml |
              XmlBlock |
#endif

              Expression |
              Script |
              String |

#if DEBUGGER
              Debugger |
#endif

              Variable |
              Alias |
              IExecute |
              HiddenIExecute |
              Procedure |
              HiddenProcedure |
              Command |
              HiddenCommand |
              SubCommand |
              Operator |
              Function |
              Plugin |
              Package |
              Resolve |

#if DATA
              Connection |
              Transaction |
#endif

              Callback |

#if EMIT && NATIVE && LIBRARY
              Module |
              Delegate |
#endif

              Idle |
              Event |
              Object |

#if NATIVE && TCL
              Tcl |
#endif

#if HISTORY
              History |
#endif

              Policy |
              Trace |

#if NATIVE && TCL && TCL_THREADS
              Thread |
#endif

#if APPDOMAINS
              AppDomain |
#endif

              Library |

#if SHELL
              Shell |
#endif

              RuntimeOption,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Reserved
        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000 /* Reserved value, do not use. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("17e7f251-9140-486f-83ad-5ef4d88c386f")]
    public enum NotifyFlags : ulong
    {
        None = 0x0,            /* This is a reserved event value that represents a null event. */
        Invalid = 0x1,         /* The value that represents an invalid event. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Flags
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        NoNotify = 0x2,        /* If this flag is set, ALL notifications are temporarily
                                * disabled. */
#endif

        Hidden = 0x4,          /* An entity is hidden. */
        Force = 0x8,           /* The operation on the named entity was forced by the caller. */
        Broadcast = 0x10,      /* The notification should be sent to all interpreters, regardless
                                * of their thread affinity. */
        Safe = 0x20,           /* The notification may be sent to "safe" interpreters. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Lifetime
        ///////////////////////////////////////////////////////////////////////////////////////////

        Reset = 0x40,          /* An entity or subsystem has been reset. */
        PreInitialized = 0x80, /* An entity has been initialized [interpreter]. */
        Setup = 0x100,         /* An entity has been setup [interpreter]. */
        Initialized = 0x200,   /* An entity has been initialized (IState?). */
        Terminated = 0x400,    /* An entity has been terminated (IState?). */
        Exit = 0x800,          /* The exit property of the interpreter was changed (we may
                                * be exiting). */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Engine Entity
        ///////////////////////////////////////////////////////////////////////////////////////////

        Matched = 0x1000,      /* A named entity has been located [by the resolver]. */
        NotFound = 0x2000,     /* A named entity could not be located [by the resolver]. */
        Executed = 0x4000,     /* A named entity has been executed.*/
        Exception = 0x8000,    /* An exception has been caught while executing a named entity. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Interpreter Entity
        ///////////////////////////////////////////////////////////////////////////////////////////

        Added = 0x10000,      /* A named entity has been added [to the interpreter or AppDomain]. */
        Copied = 0x20000,     /* A named entity has been copied [to the interpreter or AppDomain]. */
        Renamed = 0x40000,    /* A named entity has been renamed. */
        Updated = 0x80000,    /* A named entity has been updated. */
        Replaced = 0x100000,  /* A named entity has been replaced. */
        Removed = 0x200000,   /* A named entity has been removed [from the interpreter or AppDomain]. */
        Pushed = 0x400000,    /* A named entity has been pushed [Interpreter or CallFrame]. */
        Popped = 0x800000,    /* A named entity has been popped [Interpreter or callFrame]. */
        Deleted = 0x1000000,  /* A named entity has been deleted [CallFrame]. */
        Disposed = 0x2000000, /* A named entity has been disposed [object]. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Engine Core
        ///////////////////////////////////////////////////////////////////////////////////////////

        Substituted = 0x4000000,   /* A string has been substituted. */
        Evaluated = 0x8000000,     /* A script or expression has been evaluated. */
        Completed = 0x10000000,    /* A script or expression has been evaluated at the outermost
                                    * level. */
        PreCanceled = 0x20000000,  /* A script or asynchronous event is about to be canceled. */
        Canceled = 0x40000000,     /* A script or asynchronous event was canceled. */
        Unwound = 0x80000000,      /* A script or asynchronous event was unwound. */
        PreHalted = 0x100000000,   /* A script is about to be halted.  This is used primarily
                                    * [by the interactive user] to unwind nested instances of
                                    * the interactive loop. */
        Halted = 0x200000000,      /* A script was halted.  This is used primarily [by the
                                    * interactive user] to unwind nested instances of the
                                    * interactive loop. */
        Interrupted = 0x400000000, /* A script was interrupted in some way. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Debugger
        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        PreBreakpoint = 0x800000000,  /* A breakpoint is about to be triggered. */
        Breakpoint = 0x1000000000,    /* A breakpoint was triggered. */
        PreWatchpoint = 0x2000000000, /* A variable watch breakpoint is about to be triggered. */
        Watchpoint = 0x4000000000,    /* A variable watch breakpoint was triggered. */
#endif

        Trace = 0x8000000000,         /* A variable trace breakpoint was triggered. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Asynchronous Events
        ///////////////////////////////////////////////////////////////////////////////////////////

        Queued = 0x10000000000,    /* An asynchronous event has been queued to the interpreter. */
        Dequeued = 0x20000000000,  /* An asynchronous event has been dequeued from the interpreter. */
        Discarded = 0x40000000000, /* An asynchronous event has been discarded by the interpreter. */
        Cleared = 0x80000000000,   /* All asynchronous events have been cleared for the interpreter. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Input/Output Events
        ///////////////////////////////////////////////////////////////////////////////////////////

        Read = 0x100000000000,     /* Data has been read. */
        Write = 0x200000000000,    /* Data has been written. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Tcl Variable Events
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        GetVariable = 0x400000000000,    /* The Tcl variable value was fetched. */
        SetVariable = 0x800000000000,    /* A Tcl variable was set. */
        UnsetVariable = 0x1000000000000, /* A Tcl variable was unset. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Load/Unload Events
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY
        PreLoad = 0x2000000000000,   /* A plugin or other loadable module is about to be loaded. */
        PreUnload = 0x4000000000000, /* A plugin or other loadable module is about to be unloaded. */
        Load = 0x8000000000000,      /* A plugin or other loadable module has been loaded. */
        Unload = 0x10000000000000,   /* A plugin or other loadable module has been unloaded. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Masks
        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = Invalid |

#if NOTIFY || NOTIFY_OBJECT
                    NoNotify |
#endif

                    Hidden | Force | Broadcast | Safe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CheckMask = All,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // All
        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Reset |
              PreInitialized |
              Setup |
              Initialized |
              Terminated |
              Exit |
              Matched |
              NotFound |
              Executed |
              Exception |
              Added |
              Copied |
              Renamed |
              Updated |
              Replaced |
              Removed |
              Pushed |
              Popped |
              Deleted |
              Disposed |
              Substituted |
              Evaluated |
              Completed |
              PreCanceled |
              Canceled |
              Unwound |
              PreHalted |
              Halted |

#if DEBUGGER
              PreBreakpoint |
              Breakpoint |
              PreWatchpoint |
              Watchpoint |
#endif

              Trace |
              Queued |
              Dequeued |
              Discarded |
              Cleared |
              Read |
              Write |

#if NOTIFY
              PreLoad |
              PreUnload |
              Load |
              Unload |
#endif

              None,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Reserved
        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000 /* Reserved value, do not use. */
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6b6288ac-4c47-45e6-bea1-8a54b072aea3")]
    public enum PackagePreference
    {
        Invalid = -1,  /* invalid, do not use. */
        None = 0x0,    /* handling is unspecified, do not use. */
        Default = 0x1, /* no specific preference, use default handling. */
        Latest = 0x2,  /* always favor the VERY latest package version, even if alpha or beta. */
        Stable = 0x4   /* always favor the latest stable version. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
    [ObjectId("7ef0e3e3-d336-4a9f-baf1-69c2553f69d3")]
    public enum XmlBlockType
    {
        Invalid = -1,
        None = 0,
        Automatic = 1,
        Text = 2,
        Base64 = 3,
        Uri = 4
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("acdb88e0-517b-4dd1-8b95-cb81ebf0dcae")]
    public enum SubstitutionFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Backslashes = 0x2,
        Variables = 0x4,
        Commands = 0x8,

        All = Backslashes | Variables | Commands,
        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d9f761f8-bfd1-4102-819a-0e583609c45f")]
    public enum ExpressionFlags
    {
        None = 0x0,
        Invalid = 0x1,

#if EXPRESSION_FLAGS
        Backslashes = 0x2,
        Variables = 0x4,
        Commands = 0x8,
        Operators = 0x10,
        Functions = 0x20,
#endif

        BooleanToInteger = 0x40,

#if EXPRESSION_FLAGS
        Substitutions = Backslashes | Variables | Commands,
        Mathematics = Operators | Functions,
        All = Substitutions | Mathematics,
#else
        All = None,
#endif

        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7f46620d-7f45-469b-a4e7-6bbcd644aeee")]
    public enum EngineFlags : ulong
    {
        None = 0x0,                             /* Default flags (i.e. none). */
        Invalid = 0x1,                          /* Invalid, do not use. */
        BracketTerminator = 0x2,                /* Current command should be
                                                 * terminated by a close bracket. */
        UseIExecutes = 0x4,                     /* Use IExecute objects during
                                                 * evaluation? */
        UseCommands = 0x8,                      /* Use ICommand objects during
                                                 * evaluation? */
        UseProcedures = 0x10,                   /* Use IProcedure objects during
                                                 * evaluation? */
        UseHidden = 0x20,                       /* Use the list of hidden IExecute,
                                                 * ICommand, or IProcedure entities. */
        ToExecute = 0x40,                       /* We are looking up an entity to
                                                 * execute it (via the IExecute
                                                 * interface).  This flag is for
                                                 * use by the Engine class only. */
        ExactMatch = 0x80,                      /* Do not match non-exact command
                                                 * and procedure names? */
        GetHidden = 0x100,                      /* Allow fetching of hidden
                                                 * commands and/or procedures? */
        MatchHidden = 0x200,                    /* Allow matching of hidden
                                                 * commands and/or procedures? */
        IgnoreHidden = 0x400,                   /* Totally ignore the hidden
                                                 * command/procedure flags? */
        InvokeHidden = 0x800,                   /* Allow execution of hidden
                                                 * commands and/or procedures? */
        CheckStack = 0x1000,                    /* Enable stack space checking
                                                 * during interpreter readiness
                                                 * checks? */
        ForceStack = 0x2000,                    /* Force stack space checking
                                                 * during interpreter readiness
                                                 * checks? */
        ForcePoolStack = 0x4000,                /* Check native stack space for
                                                 * thread pool threads as well. */
        ForceSoftEof = 0x8000,                  /* The engine should never read
                                                 * past a "soft" end-of-file,
                                                 * even with policy checking
                                                 * enabled. */
        NoUnknown = 0x10000,                    /* Do not fallback on the
                                                 * "unknown" command/procedure
                                                 * for unknown commands? */
        NoCancel = 0x20000,                     /* Skip script cancellation
                                                 * checking during interpreter
                                                 * readiness checks? */
        NoReady = 0x40000,                      /* Disable interpreter
                                                 * readiness checks? */
        NoPolicy = 0x80000,                     /* Disable all policy checking
                                                 * within the script engine? */
#if DEBUGGER
        NoBreakpoint = 0x100000,                /* Disable all debugger
                                                 * breakpoints? */
        NoWatchpoint = 0x200000,                /* Disable all variable
                                                 * watches? */
#endif
        NoEvent = 0x400000,                     /* Disable all asynchronous
                                                 * event processing? */
        NoEvaluate = 0x800000,                  /* Totally disable all script
                                                 * evaluation? */
        NoSubstitute = 0x1000000,               /* Totally disable all token
                                                 * substitution? */
        NoResetResult = 0x2000000,              /* Skip resetting the
                                                 * interpreter result? */
        NoResetError = 0x4000000,               /* Skip resetting the error
                                                 * related engine flags? */
        ResetCancel = 0x8000000,                /* Used to determine if the
                                                 * cancel flag should be
                                                 * forcibly reset. */
        ResetReturnCode = 0x10000000,           /* Used to determine if the
                                                 * last return code should
                                                 * be forcibly reset. */
        EvaluateGlobal = 0x20000000,            /* Evaluate script in the
                                                 * global scope without regard
                                                 * to the current scope? */
        ErrorInProgress = 0x40000000,           /* Error information is being
                                                 * logged as the stack unwinds. */
        ErrorAlreadyLogged = 0x80000000,        /* Error information has
                                                 * already been logged for the
                                                 * current call. */
        ErrorCodeSet = 0x100000000,             /* Error code has been set for
                                                 * the current call. */
#if NOTIFY || NOTIFY_OBJECT
        NoNotify = 0x200000000,                 /* Disable all notifications
                                                 * for the script? */
#endif
#if HISTORY
        NoHistory = 0x400000000,                /* Disable command history
                                                 * for the script? */
#endif
#if CALLBACK_QUEUE
        NoCallbackQueue = 0x800000000,          /* Disable the evaluation
                                                 * engine callback queue? */
#endif
        Interactive = 0x1000000000,             /* Script is being evaluated
                                                 * by an interactive user? */
        NoHost = 0x2000000000,                  /* Do not fallback on using a
                                                 * matching host script? */
        NoRemote = 0x4000000000,                /* Disallow evaluating remote
                                                 * script files? */
#if XML
        NoXml = 0x8000000000,                   /* Disable auto-detection and
                                                 * evaluation of scripts that
                                                 * conform to our XML schema?
                                                 */
#endif
        NoGlobalCancel = 0x10000000000,         /* Only consider thread-local
                                                 * script cancellation flags.
                                                 */
        NoSafeFunction = 0x20000000000,         /* Allow all functions to be
                                                 * executed in safe interpreters,
                                                 * include those that are NOT
                                                 * marked as "safe". */
        ExternalExecution = 0x40000000000,      /* An external execution context
                                                 * is active and still needs to
                                                 * be removed. */
#if DEBUGGER && DEBUGGER_ARGUMENTS
        NoDebuggerArguments = 0x80000000000,    /* Skip setting the command name
                                                 * and arguments for use by the
                                                 * debugger. */
#endif
        NoCache = 0x100000000000,               /* Skip looking up executable
                                                 * entities in the cache (e.g.
                                                 * commands, procedures, etc). */
        GlobalOnly = 0x200000000000,            /* Force command to be looked
                                                 * up in the global namespace. */
        UseInterpreter = 0x400000000000,        /* When applicable, combine
                                                 * engine flags with the
                                                 * ones from the provided
                                                 * interpreter. */
        ExternalScript = 0x800000000000,        /* Force extra BeforeScript
                                                 * policy checks in the
                                                 * ReadScriptStream and
                                                 * ReadScriptFile methods
                                                 * due to being passed an
                                                 * external IScript. */
        ExtraCallFrame = 0x1000000000000,       /* The engine should push and
                                                 * pop an "extra" call frame
                                                 * when processing within the
                                                 * EvaluateStream, EvaluateFile,
                                                 * SubstituteStream, and
                                                 * SubstituteFile methods.
                                                 */
#if TEST
        SetSecurityProtocol = 0x2000000000000,  /* The engine should automatically
                                                 * setup the necessary remote URI
                                                 * security protocols. */
#endif
        IgnoreRootedFileName = 0x4000000000000, /* Allow searching alternate
                                                 * file names even when the
                                                 * original file name was a
                                                 * fully qualified path.
                                                 * COMPAT: Eagle beta. */
        NoFileNameOnly = 0x8000000000000,       /* Disallow use of
                                                 * Path.GetFileName by the
                                                 * Engine class. */
        NoRawName = 0x10000000000000,           /* Disallow use of raw names
                                                 * by the Engine class. */
        AllErrors = 0x20000000000000,           /* Return all errors seen
                                                 * while trying to locate a
                                                 * script [file?] to evaluate. */
        NoDefaultError = 0x40000000000000,      /* Return the detailed error
                                                 * (if possible) that is seen
                                                 * while trying to locate a
                                                 * script [file?] to evaluate. */
#if PARSE_CACHE
        NoCacheParseState = 0x80000000000000,   /* EXPERIMENTAL: Prevent the
                                                 * engine from caching
                                                 * ParseState object instances.
                                                 * This flag may be removed in
                                                 * later releases. */
#endif
#if ARGUMENT_CACHE
        NoCacheArgument = 0x100000000000000,    /* EXPERIMENTAL: Prevent the
                                                 * engine from caching Argument
                                                 * object instances.  This flag
                                                 * may be removed in later
                                                 * releases. */
#endif
        PostScriptBytes = 0x200000000000000,    /* When reading a script file,
                                                 * also read the post-script
                                                 * bytes after the current
                                                 * "soft" end-of-file
                                                 * character. */
        SeekSoftEof = 0x400000000000000,        /* The engine should seek to
                                                 * the next "soft" end-of-file,
                                                 * prior to reading post-script
                                                 * bytes from a stream. */
        NoUsageData = 0x800000000000000,        /* Do not keep track of usage
                                                 * data for executable entities.
                                                 */
        NoNullArgument = 0x1000000000000000,    /* Do not use a null command
                                                 * result to create an argument
                                                 * to pass to a subsequent
                                                 * command. */
        NoResetAbort = 0x2000000000000000,      /* Do not use Thread.ResetAbort
                                                 * when an engine thread is
                                                 * aborted via Thread.Abort. */
#if PREVIOUS_RESULT
        NoPreviousResult = 0x4000000000000000,  /* Skip all "previous result"
                                                 * handling within the script
                                                 * engine. */
#endif

        //
        // NOTE: Stack checking related engine flags.
        //
        BaseStackMask = CheckStack | ForcePoolStack,
        FullStackMask = CheckStack | ForceStack | ForcePoolStack,

        //
        // NOTE: For use by the EvaluatePromptScript method only.
        //
        ForPromptMask = NoCancel
#if DEBUGGER
            | NoBreakpoint
#endif
            ,

        //
        // NOTE: Engine flags allowed for use with [interp readorgetscriptfile]
        //       sub-command.
        //
        ReadOrGetScriptFileMask = ForceSoftEof | NoPolicy | NoNotify |
                                  NoHost | NoRemote |
#if XML
                                  NoXml |
#endif
                                  ExternalScript | NoFileNameOnly | NoRawName |
                                  AllErrors | NoDefaultError,

#if DEBUGGER
        //
        // NOTE: Used when evaluating an interpreter or shell startup script
        //       in the debugger.
        //
        DebuggerExecutionMask = NoWatchpoint,
#endif

        //
        // HACK: Enable this mask to make the script engine run at maximum
        //       speed (i.e. and sacrificing any extra features in order to
        //       do so).
        //
        FastMask = NoReady |
#if DEBUGGER
                   NoBreakpoint | NoWatchpoint |
#endif
#if DEBUGGER && DEBUGGER_ARGUMENTS
                   NoDebuggerArguments |
#endif
#if NOTIFY || NOTIFY_OBJECT
                   NoNotify |
#endif
#if CALLBACK_QUEUE
                   NoCallbackQueue |
#endif
#if ARGUMENT_CACHE
                   NoCacheArgument |
#endif
                   NoUsageData |
#if PREVIOUS_RESULT
                   NoPreviousResult |
#endif
                   None,

        //
        // NOTE: When looking up entities to execute, use all available entity
        //       types.
        //
        UseAllMask = UseCommands | UseProcedures | UseIExecutes,

        //
        // NOTE: All the error handling related flags.
        //
        ErrorMask = ErrorInProgress | ErrorAlreadyLogged | ErrorCodeSet,

        //
        // NOTE: All enabled/disabled related flags.
        //
        EnabledMask = NoEvaluate | NoSubstitute,

        //
        // NOTE: The high-bit is reserved, please do not use it.
        //
        Reserved = 0x8000000000000000 /* Reserved value, do not use. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6572cb04-c5c9-48ff-b0e1-60ace408f8cb")]
    public enum CharacterType
    {
        None = 0x0,
        Invalid = 0x1,
        Normal = None,
        Space = 0x2,
        CommandTerminator = 0x4,
        Substitution = 0x8,
        Quote = 0x10,
        CloseParenthesis = 0x20,
        CloseBracket = 0x40,
        Brace = 0x80
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("ce29fc68-cc0d-4eb1-98c6-7d02b6d0bbd5")]
    public enum Lexeme
    {
        /*
         * Basic lexemes:
         */

        Unknown,
        UnknownCharacter,
        Literal,
        IdentifierName,
        OpenBracket,
        OpenBrace,
        OpenParenthesis,
        CloseParenthesis,
        DollarSign,
        QuotationMark,
        Comma,
        End,

        /*
         * Binary numeric operators:
         */

        Exponent,
        Multiply,
        Divide,
        Modulus,

        Plus,  // NOTE: Also unary.
        Minus, // NOTE: Also unary.

        LeftShift,
        RightShift,
        LeftRotate,
        RightRotate,

        LessThan,
        GreaterThan,
        LessThanOrEqualTo,
        GreaterThanOrEqualTo,

        Equal,
        NotEqual,

        BitwiseAnd,
        BitwiseXor,
        BitwiseOr,
        BitwiseEqv,
        BitwiseImp,

        LogicalAnd,
        LogicalXor,
        LogicalOr,
        LogicalEqv,
        LogicalImp,

        /*
         * The ternary "if" operator.
         */

        Question,
        Colon,

        /*
         * Unary operators. Unary minus and plus are represented by the (binary)
         * lexemes "Minus" and "Plus" (above).
         */

        LogicalNot,
        BitwiseNot,

        /*
         * Binary string operators:
         */

        StringGreaterThan,
        StringGreaterThanOrEqualTo,
        StringLessThan,
        StringLessThanOrEqualTo,

        StringEqual,
        StringNotEqual,

        /*
         * Binary list operators:
         */

        ListIn,
        ListNotIn,

        /*
         * Binary assignment operators:
         */

        VariableAssignment,

        /*
         * Operator value bounds:
         */

        Minimum = Unknown,
        Maximum = VariableAssignment
    }
}
