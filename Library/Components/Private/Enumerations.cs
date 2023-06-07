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
    [ObjectId("fe64b30e-2c31-4a1c-925b-8556a3fd2229")]
    internal enum IfNotFoundType
    {
        None = 0x0,
        Invalid = 0x1,
        Null = 0x2,
        Unknown = 0x4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
    [Flags()]
    [ObjectId("cac467bd-9d4a-46b2-aeb3-4a790523b8df")]
    internal enum InteractiveLoopFlags
    {
        None = 0x0,           /* No special handling. */
        Invalid = 0x1,        /* Invalid, do not use. */
        NoTimeout = 0x2,      /* The interactive loop should not mess with
                               * the timeout thread. */
        TraceInput = 0x4,     /* Trace interactive input processing.
                               * Used by the interactive loop. */
        NoSemaphore = 0x8,    /* Do not use the semaphore when dealing with
                               * interactive input from within the
                               * * interactive loop. */
        WaitSemaphore = 0x10, /* Wait (forever) for the interactive
                               * semaphore to be available.  If this flag
                               * is not set, the inability to obtain the
                               * interactive semaphore will bail out of
                               * the interactive loop. */
        NoRefreshHost = 0x20, /* Do not refresh the interactive host when
                               * preparing to read interactive input from
                               * within the interactive loop. */
        TraceCommand = 0x40,  /* Trace interactive commands.  Used by the
                               * interactive loop. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForDefault = 0x100000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = ForDefault
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("b374f974-c19e-4706-b323-033c4d09fb34")]
    internal enum MaybeEnableType
    {
        False = 0,
        True = 1,
        Automatic = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: This enumeration is for use by the WindowOps class only.
    //          PLEASE DO NOT USE.  It is subject to change at any time.
    //
    [ObjectId("509325bb-f329-413f-b201-f1e4dc5e8246")]
    internal enum UserInteractiveType
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
        Enabled = 1,

        Continue = 2,

        Fallback = 3,
        Environment = 4,
        WinForms = 5,
        Framework = 6,

        Interpreter = 7,
        InterpreterIfFalse = 8,
        InterpreterIfTrue = 9,

        MaybeInterpreter = 10,
        MaybeInterpreterIfFalse = 11,
        MaybeInterpreterIfTrue = 12
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: This enumeration is for use by the WinTrustOps class only.
    //          PLEASE DO NOT USE.  It is subject to change at any time.
    //
    [Flags()]
    [ObjectId("cf362264-3ad5-4edf-9e57-e77482586361")]
    internal enum TrustValues : uint
    {
#if NATIVE && WINDOWS
        // WINTRUST_DATA.dwUIChoice

        [ParameterIndex(0)]
        WTD_UI_ALL = 1,

        [ParameterIndex(0)]
        WTD_UI_NONE = 2,

        [ParameterIndex(0)]
        WTD_UI_NOBAD = 3,

        [ParameterIndex(0)]
        WTD_UI_NOGOOD = 4,

        ///////////////////////////////////////////////////////////////////////////////////////////

        // WINTRUST_DATA.fdwRevocationChecks

        [ParameterIndex(1)]
        WTD_REVOKE_NONE = 0,

        [ParameterIndex(1)]
        WTD_REVOKE_WHOLECHAIN = 1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        // WINTRUST_DATA.dwProvFlags

        [ParameterIndex(2)]
        WTD_NONE = 0x0,

        [ParameterIndex(2)]
        WTD_USE_IE4_TRUST_FLAG = 0x1,

        [ParameterIndex(2)]
        WTD_NO_IE4_CHAIN_FLAG = 0x2,

        [ParameterIndex(2)]
        WTD_NO_POLICY_USAGE_FLAG = 0x4,

        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_NONE = 0x10,

        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_END_CERT = 0x20,

        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_CHAIN = 0x40,

        [ParameterIndex(2)]
        WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x80,

        [ParameterIndex(2)]
        WTD_SAFER_FLAG = 0x100,

        [ParameterIndex(2)]
        WTD_HASH_ONLY_FLAG = 0x200,

        [ParameterIndex(2)]
        WTD_USE_DEFAULT_OSVER_CHECK = 0x400,

        [ParameterIndex(2)]
        WTD_LIFETIME_SIGNING_FLAG = 0x800,

        [ParameterIndex(2)]
        WTD_CACHE_ONLY_URL_RETRIEVAL = 0x1000,

        [ParameterIndex(2)]
        WTD_DISABLE_MD2_MD4 = 0x2000,

        [ParameterIndex(2)]
        WTD_MOTW = 0x4000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        // WINTRUST_DATA.dwUIContext

        [ParameterIndex(3)]
        WTD_UICONTEXT_EXECUTE = 0,

        [ParameterIndex(3)]
        WTD_UICONTEXT_INSTALL = 1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is the number of "uint" parameters needed to fill up the
        //       WINTRUST_DATA structure passed into the WinVerifyTrust API.
        //
        PARAMETER_COUNT = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("5ec24744-3bc0-4787-9ab5-0ee4a7a2d3ac")]
    internal enum BufferStats
    {
        Length = 0,
        CrCount = 1,
        LfCount = 2,
        CrLfCount = 3,
        LineCount = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2c24c473-85b2-48d4-b9cb-d6741c7c9870")]
    internal enum VerifyFlags
    {
        None = 0x0,
        Invalid = 0x1,

        GlobalAssemblyCache = 0x2,
        IgnoreNull = 0x4,

        StopOnError = 0x8,
        StopOnNull = 0x10,

        VerifyChain = 0x20,
        NoVerifyChain = 0x40,
        VerboseResults = 0x80,

        ForDefault = 0x1000,
        ForMaximum = 0x2000,

        StopMask = StopOnError | StopOnNull,
        MaximumMask = GlobalAssemblyCache | VerifyChain | VerboseResults,
        AllMask = StopMask | MaximumMask | NoVerifyChain | VerboseResults,

        Maximum = MaximumMask | ForMaximum,
        Default = StopMask | ForDefault,
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("35f46c6a-66ce-4de5-9979-553a915b5965")]
    internal enum DebugPathFlags
    {
        None = 0x0,
        Invalid = 0x1,

        GetAll = 0x2,
        UseFilter = 0x4,
        ExistingOnly = 0x8,
        UniqueOnly = 0x10,

        Automatic = UseFilter | ExistingOnly | UniqueOnly,

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("c8c9371b-e788-4b2b-a13b-0bd22f07d69b")]
    internal enum SetDirection
    {
        None = 0x0,
        Invalid = 0x1,
        Set = 0x200,
        Unset = 0x400
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
    [Flags()]
    [ObjectId("dd744742-ccd6-4a21-877f-4684d249d5fe")]
    internal enum PkgInstallType : ulong
    {
        None = 0x0,
        Invalid = 0x1,

        Install = 0x1000,
        Uninstall = 0x2000,

        Temporary = 0x10000,
        Persistent = 0x20000,

        ForDefault = 0x10000000,

        ActionMask = Install | Uninstall,

        Default = None | ForDefault
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a1767e9d-8382-46b5-a544-f14a73cd5122")]
    internal enum VersionFlags
    {
        None = 0x0,
        Invalid = 0x1,

        Core = 0x100,
        Plugins = 0x200,

        AllowNull = 0x1000,

        ForSetup = 0x10000,
        ForDefault = 0x20000,

        Setup = Core | ForSetup,

        Default = Core | Plugins | AllowNull | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
    [Flags()]
    [ObjectId("e9e91ce3-1054-4930-8091-edf6c481e546")]
    internal enum KioskFlags /* NOTE: Actually, semi-public via command line. */
    {
        None = 0x0,
        Invalid = 0x1,
        Enable = 0x2,
        UseArgv = 0x4,

        Default = None
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d62ac868-4c57-44fd-86e9-77df6fdae7ae")]
    internal enum UpdateFlags
    {
        None = 0x0,
        Invalid = 0x1,

        IdleTasks = 0x1000,
        PreQueue = 0x2000,
        Queue = 0x4000,
        PostQueue = 0x8000,

        ForDefault = 0x10000,

        Default = Queue | PostQueue | ForDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
    [ObjectId("ac07a839-b5d8-420f-81de-55f4faf6591c")]
    internal enum PackageControlType
    {
        None = 0x0,
        Invalid = 0x1,
        Require = 0x2
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("ca4a4834-0084-460e-a78a-55d0bec42d96")]
    internal enum VER_PRODUCT_TYPE : byte /* NOTE: From "WinNT.h". */
    {
        VER_NT_UNKNOWN = 0xFF,          /* Cannot query: possibly Linux, etc. */
        VER_NT_NONE = 0x0,              /* Not yet queried. */
        VER_NT_WORKSTATION = 0x1,       /* Windows 2000/XP/7/8/8.1/10/11, etc. */
        VER_NT_DOMAIN_CONTROLLER = 0x2, /* Windows Server -AND- Domain Controller */
        VER_NT_SERVER = 0x3             /* Windows Server */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
    [Flags()]
    [ObjectId("bbbeed12-5900-470a-b546-16b04009c870")]
    internal enum XmlAttributeListType
    {
        None = 0x0,
        Invalid = 0x1,
        Engine = 0x2,
        Required = 0x4,
        All = 0x8
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("86eaf199-af64-47d9-a546-e9331fa8f2d8")]
    internal enum ScriptSecurityFlags
    {
        None = 0x0,      /* No special handling. */
        Invalid = 0x1,   /* Invalid, do not use. */

        ReadOnly = 0x2,  /* The associated IScript instance is
                          * logically read-only and must not be
                          * modified, e.g. property set accessors
                          * should fail with an exception.  When
                          * using property get accessors for any
                          * non-immutable data types, a deep copy
                          * may be returned.
                          */

        Immutable = 0x4, /* The associated IScript instance is
                          * logically immutable.  This implies
                          * that the IScript is read-only and
                          * further prohibits usage of property
                          * get accessors that may return any
                          * non-immutable and non-copyable data
                          * types, e.g. arbitrary IClientData.
                          */

        AnyMask = ReadOnly | Immutable
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("4af85b57-4920-4117-9a07-d8c647e8ee3c")]
    internal enum MetaMemberTypes
    {
        FlagsEnum = 0,                  /* EnumOps */
        UnsafeObject = 1,               /* MarshalOps */
        ObjectDefault = 2,              /* ObjectOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        IndexMask = 0x3                 /* 0b11 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("0d971c64-33bd-4247-b88b-47fbc833c4c5")]
    internal enum MetaBindingFlags
    {
        PrivateCreateInstance = 0,      /* ObjectOps */
        PrivateInstance = 1,            /* MarshalOps */
        PrivateInstanceGetField = 2,    /* MarshalOps */
        PrivateInstanceGetProperty = 3, /* MarshalOps */
        PrivateInstanceMethod = 4,      /* MarshalOps */
        PrivateStatic = 5,              /* MarshalOps */
        PrivateStaticGetField = 6,      /* MarshalOps */
        PrivateStaticGetProperty = 7,   /* MarshalOps */
        PrivateStaticMethod = 8,        /* MarshalOps */
        PrivateStaticSetField = 9,      /* MarshalOps */
        PrivateStaticSetProperty = 10,  /* MarshalOps */
        PublicCreateInstance = 11,      /* ObjectOps */
        PublicInstance = 12,            /* DelegateOps */
        PublicInstanceGetField = 13,    /* MarshalOps */
        PublicInstanceGetProperty = 14, /* MarshalOps */
        PublicInstanceMethod = 15,      /* MarshalOps */
        PublicStaticGetProperty = 16,   /* MarshalOps */
        PublicStaticMethod = 17,        /* MarshalOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = 18,                   /* MarshalOps */
        EnumField = 19,                 /* MarshalOps */
        HostInfo = 20,                  /* MarshalOps */
        ListProperties = 21,            /* MarshalOps */
        LooseMethod = 22,               /* MarshalOps */
        NestedObject = 23,              /* MarshalOps */
        UnsafeObject = 24,              /* MarshalOps */

        ///////////////////////////////////////////////////////////////////////////////////////////

        DomainId = 25,                  /* AppDomainOps */
        IsLegacyCasPolicyEnabled = 26,  /* AppDomainOps */
        FlagsEnum = 27,                 /* EnumOps */
        ByteBuffer = 28,                /* FileOps */
        HostProperty = 29,              /* _Hosts.Default */
        Items = 30,                     /* ArrayOps */
        Size = 31,                      /* ArrayOps */
        DisposedField = 32,             /* ObjectOps */
        DisposedProperty = 33,          /* ObjectOps */
        Guru = 34,                      /* ObjectOps */
        InvokeRaw = 35,                 /* ObjectOps */
        ObjectDefault = 36,             /* ObjectOps */
        Delegate = 37,                  /* RuntimeOps */
        SocketPrivate = 38,             /* SocketOps */
        SocketPublic = 39,              /* SocketOps */
        Trace = 40,                     /* _Plugins.Trace */
        TransferHelper = 41,            /* TransferHelper */
        InterpreterSettings = 42,       /* SettingsOps */
        TypeDefaultLookup = 43,         /* MarshalOps (System.Type) */

        ///////////////////////////////////////////////////////////////////////////////////////////

        IndexMask = 0x3F                /* 0b111111 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c819b429-f7d5-4a4e-8bc2-9d633506f5a5")]
    internal enum TimeoutFlags
    {
        None = 0x0,          /* Nothing, do not use. */
        Invalid = 0x1,       /* Invalid, do not use. */
        Reserved1 = 0x2,     /* Reserved, do not use. */
        Reserved2 = 0x4,     /* Reserved, do not use. */

        Interactive = 0x100, /* Treat the current timeout
                              * operation as "interactive".
                              * This allows an interative
                              * user to be prompted -AND-
                              * may alter how any script
                              * cancellation is performed. */

        Interrupt = 0x200,   /* Allow the primary thread
                              * associated with the target
                              * interpreter to be forcibly
                              * interrupted. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForWatchdogControl = 0x1000, /* For use by the
                                      * WatchdogControl
                                      * method only. */

        ForInteractiveLoop = 0x2000, /* For use by the
                                      * interactive loop
                                      * methods only. */

        ForTimeout = 0x4000,         /* For use by the
                                      * general script
                                      * timeout thread.
                                      */

        ForFinallyTimeout = 0x8000,  /* For use by the
                                      * try/finally
                                      * script timeout
                                      * thread. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Timeout = Reserved1 | ForTimeout,
        FinallyTimeout = Reserved1 | ForFinallyTimeout,
        InteractiveLoop = Reserved1 | Interactive | ForInteractiveLoop,
        WatchdogControl = Reserved1 | ForWatchdogControl,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Reserved2 | None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("34220ff8-dcff-4821-98d8-c71c74008a8f")]
    internal enum ObjectNamespace
    {
        None = 0x0,            /* Nothing, do not use. */
        Invalid = 0x1,         /* Invalid, do not use. */
        Reserved1 = 0x2,       /* Reserved, do not use. */
        Unknown = 0x4,         /* Cannot determine...? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForInterpreter = 0x10, /* For use by Interpreter class. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Clr = 0x100,           /* e.g. namespaces of Object, etc. */
        Eagle = 0x200,         /* e.g. namespaces of Interpreter,
                                * StringList, etc. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Clr | Eagle | ForInterpreter,
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c72796c0-479e-4613-bfca-8a55ec264629")]
    internal enum RuntimeName
    {
        None = 0x0,      /* Nothing, do not use. */
        Invalid = 0x1,   /* Invalid, do not use. */
        Reserved1 = 0x2, /* Reserved, do not use. */
        Unknown = 0x4,   /* Cannot determine...? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NetFx = 0x10,      /* Windows (only) .NET Framework 2.x, 4.x, etc. */
        Mono = 0x20,       /* Cross-platform Mono, 2.x or higher. */
        DotNetCore = 0x40, /* Cross-platform .NET Core, 2.0 or higher. */
        DotNet = 0x80,     /* Cross-platform .NET, 5.0 or higher. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = NetFx | Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("11e25134-9e1e-4823-95d4-675109cb3dc8")]
    internal enum HomeFlags
    {
        None = 0x0,           /* Nothing, do not use. */
        Invalid = 0x1,        /* Invalid, do not use. */
        Reserved1 = 0x2,      /* Reserved, do not use. */
        Reserved2 = 0x4,      /* Reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Legacy = 0x100,        /* This will always use the legacy "HOME"
                                * environment variable. */
        Data = 0x200,          /* Currently, this uses the "XDG_DATA_HOME"
                                * and "XDG_DATA_DIRS" environment variables,
                                * in that order. */
        Configuration = 0x400, /* Currently, this uses the "XDG_CONFIG_HOME"
                                * and "XDG_CONFIG_DIRS" environment variables,
                                * in that order. */
        Cloud = 0x800,         /* Currently, this uses the "XDG_CLOUD_HOME"
                                * and "XDG_CLOUD_DIRS" environment variables,
                                * in that order. */
        Startup = 0x1000,      /* Currently, this uses the "XDG_STARTUP_HOME"
                                * and "XDG_STARTUP_DIRS" environment variables,
                                * in that order. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Exists = 0x4000,       /* All the returned directories must exist. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ReservedMask = Reserved1 | Reserved2,
        FlagsMask = ReservedMask | Exists,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyDataMask = Legacy | Data | Cloud | Startup,
        AnyConfigurationMask = Legacy | Configuration | Cloud | Startup,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SetupHomeGetMask = (AnyDataMask & ~(Cloud | Startup)) | Exists | Reserved1,
        SetupHomeSetMask = Legacy | Reserved1, /* COMPAT: Tcl */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyMask = Legacy | Data | Configuration | Cloud | Startup
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
    [Flags()]
    [ObjectId("51c62d47-1e53-4679-a713-6ca0a8b46dad")]
    internal enum NativeWindowType
    {
        None = 0x0,
        Invalid = 0x1,
        Active = 0x100,
        Console = 0x200,
        Foreground = 0x400,
        Shell = 0x800,
        Desktop = 0x1000,
        Terminal = 0x2000,
        Input = 0x4000,
        Icon = 0x8000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("69660685-2e73-4194-aea9-6ea6cd1e5b13")]
    internal enum ConsoleScreenBufferFlags
    {
        CONSOLE_TEXTMODE_BUFFER = 1
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && TCL_THREADS
    [ObjectId("581367c7-c7ee-44d1-8583-8fa3fb8e08a5")]
    internal enum TclThreadEvent
    {
        //
        // WARNING: The ordering of these values must match those
        //          in the ThreadStart() method of the TclThread
        //          class.
        //
        DoneEvent = 0x0,
        IdleEvent = 0x1,
        QueueEvent = 0x2
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("0cf808e6-2239-4308-b3ac-d880a7439603")]
    internal enum CacheInformationFlags
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Settings = 0x2,
        Memory = 0x4,
        Statistics = 0x8,
        State = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForDebug = 0x20,
        ForInitialize = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Debug = Settings | Memory | Statistics | State |
                ForDebug,

        Initialize = State | ForInitialize
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7a2e9b30-2671-4f43-806a-745d500713ac")]
    internal enum ConfigurationOperation
    {
        None = 0x0,
        Invalid = 0x1,

        Get = 0x2,
        Set = 0x4,
        Unset = 0x8
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("dbc4da96-c186-4a57-b0ec-8f4a6929f18c")]
    internal enum ConfigurationFlags
    {
        None = 0x0,                        /* No special handling. */
        Invalid = 0x1,                     /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Unprefixed = 0x2,                  /* Check, modify, or remove values
                                            * that are NOT prefixed with the
                                            * package name. */
        Prefixed = 0x4,                    /* Check, modify, or remove values
                                            * that are prefixed with the
                                            * package name. */
        Expand = 0x8,                      /* Expand contained environment
                                            * variables. */
        Verbose = 0x10,                    /* Emit diagnostic messages. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Environment = 0x20,                /* Check, modify, or remove
                                            * environment variables. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AppSettings = 0x40,                /* Check, modify, or remove
                                            * loaded AppSettings. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        SkipUnprefixedEnvironment = 0x80,  /* Skipping checking for the
                                            * unprefixed environment
                                            * variable. */
        SkipUnprefixedAppSettings = 0x100, /* Skipping checking for the
                                            * unprefixed AppSetting. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ExistOnly = 0x200,                 /* Only set when checking if a
                                            * value exists.  Internal use
                                            * only. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ListValue = 0x400,                 /* Make sure the value is a
                                            * valid list; otherwise, a
                                            * null value is returned. */
        PatternValue = 0x800,              /* Make sure the value is a
                                            * glob pattern.  Combine with
                                            * the "ListValue" flag for a
                                            * list of glob patterns. */
        NativePathValue = 0x1000,          /* Make sure the value is a
                                            * native path.  Combine with
                                            * the "ListValue" flag for a
                                            * list of native paths. */

        PatternListValue = ListValue | PatternValue,
        NativePathListValue = ListValue | NativePathValue,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardMask = Unprefixed | Prefixed | Environment | AppSettings,
        ResultMask = StandardMask & ~(Prefixed | AppSettings),
        UtilityMask = StandardMask & ~Prefixed,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        ForCacheConfiguration = 0x2000,    /* For use by the CacheConfiguration class. */
#endif

#if CONSOLE
        ForConsoleOps = 0x4000,            /* For use by the ConsoleOps class. */
#endif

        ForGlobalState = 0x8000,           /* For use by the GlobalState class. */
        ForInteractiveOps = 0x10000,       /* For use by the InteractiveOps class. */
        ForInterpreter = 0x20000,          /* For use by the Interpreter class. */

#if NATIVE && WINDOWS
        ForNativeConsole = 0x40000,        /* For use by the NativeConsole class. */
#endif

#if NATIVE && TCL && NATIVE_PACKAGE
        ForNativePackage = 0x80000,        /* For use by the NativePackage class. */
#endif

#if NATIVE
        ForNativeStack = 0x100000,         /* For use by the NativeStack class. */
#endif

#if NATIVE && NATIVE_UTILITY
        ForNativeUtility = 0x200000,       /* For use by the NativeUtility class. */
#endif

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ForPackageOps = 0x400000,          /* For use by the PackageOps class. */
#endif

        ForPathOps = 0x800000,             /* For use by the PathOps class. */
        ForSetupOps = 0x1000000,           /* For use by the SetupOps class. */
        ForUtility = 0x2000000,            /* For use by the Utility class. */
        ForScriptOps = 0x4000000,          /* For use by the ScriptOps class. */
        ForResult = 0x8000000,             /* For use by the Result class. */
        ForWebOps = 0x10000000,            /* For use by the WebOps class. */

#if NATIVE && WINDOWS
        ForWinTrustOps = 0x20000000,       /* For use by the WinTrustOps class. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        CacheConfiguration = StandardMask | ForCacheConfiguration,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        ConsoleOps = StandardMask | ForConsoleOps,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        GlobalState = StandardMask | ForGlobalState,
        GlobalStateNoPrefix = GlobalState & ~Prefixed,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveOps = StandardMask | ForInteractiveOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Interpreter = StandardMask | ForInterpreter,
        InterpreterVerbose = Verbose | Interpreter,
        InterpreterSkipUnprefixedEnvironment = Interpreter | SkipUnprefixedEnvironment,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        PackageOps = StandardMask | ForPackageOps,
        PackageOpsNoPrefix = PackageOps & ~Prefixed,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        PathOps = StandardMask | ForPathOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SetupOps = StandardMask | ForSetupOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ScriptOps = StandardMask | ForScriptOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Result = ResultMask | ForResult,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Utility = UtilityMask | ForUtility,

        ///////////////////////////////////////////////////////////////////////////////////////////

        WebOps = StandardMask | ForWebOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        WinTrustOps = StandardMask | ForWinTrustOps,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        NativeConsole = StandardMask | ForNativeConsole,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
        NativePackage = StandardMask | ForNativePackage,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        NativeStack = StandardMask | ForNativeStack,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        NativeUtility = StandardMask | ForNativeUtility,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("27e1fa59-a2fc-44db-8c52-155c6b18fbed")]
    internal enum GarbageFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved1 = 0x2,
        Reserved2 = 0x4,
        Reserved3 = 0x8,
        Reserved4 = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NeverCollect = 0x20,
        AlwaysCollect = 0x40,
        MaybeCollect = 0x80,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        NeverCompact = 0x100,
        AlwaysCompact = 0x200,
        MaybeCompact = 0x400,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NeverWait = 0x800,
        AlwaysWait = 0x1000,
        MaybeWait = 0x2000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        WhenPossibleCompact = AlwaysCompact,
        MaybeWhenPossibleCompact = MaybeCompact,
#else
        WhenPossibleCompact = None,
        MaybeWhenPossibleCompact = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForCommand = Reserved1 | AlwaysCollect |
                     WhenPossibleCompact | AlwaysWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForEngine = Reserved2 | AlwaysCollect |
                    WhenPossibleCompact | NeverWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForUnload = Reserved3 | AlwaysCollect |
                    WhenPossibleCompact | AlwaysWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Reserved4 | MaybeCollect |
                  MaybeWhenPossibleCompact | MaybeWait
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2fc84796-ec90-471c-ad91-6d5f218c3102")]
    internal enum DisposalPhase : ulong
    {
        None = 0x0,                      // No special handling.
        Invalid = 0x1,                   // Invalid, do not use.
        Reserved = 0x2,                  // Reserved, do not use.
        Native = 0x4,                    // Native plugin, command, etc.
        Managed = 0x8,                   // Managed plugin, command, etc.
        User = 0x10,                     // Non-system plugin, command, etc.
        System = 0x20,                   // System plugin, command, etc.

        ///////////////////////////////////////////////////////////////////////////////////////////

        Plugin = 0x100,                  // An IPlugin object.
        Command = 0x200,                 // An ICommand object.
        Function = 0x400,                // An IFunction object.
        Operator = 0x800,                // An IOperator object.
        Namespace = 0x1000,              // An INamespace object.
        Resolver = 0x2000,               // An IResolver object.
        Policy = 0x4000,                 // An IPolicy object.
        Trace = 0x8000,                  // An ITrace object.
        EventManager = 0x10000,          // An IEventManager object.
        RandomNumberGenerator = 0x20000, // An RNG of some kind.
        Debugger = 0x40000,              // An IDebugger object.
        Scope = 0x80000,                 // An ICallFrame object.
        Alias = 0x100000,                // An IAlias object.
        Database = 0x200000,             // An ADO.NET object.
        Channel = 0x400000,              // A channel or encoding object.
        Object = 0x800000,               // An IObject or related object.
        Trusted = 0x1000000,             // A trusted type, URI, path, etc.
        Procedure = 0x2000000,           // An IProcedure object.
        Execute = 0x4000000,             // An IExecute object.
        Callback = 0x8000000,            // An ICallback object.
        Package = 0x10000000,            // An IPackage object.
        Thread = 0x20000000,             // System.Threading.Thread object.
        Interpreter = 0x40000000,        // An IInterpreter object.
        AppDomain = 0x80000000,          // System.AppDomain object.
        NativeLibrary = 0x100000000,     // A native delegate or module.
        NativeTcl = 0x200000000,         // Native Tcl integration subsystem.
        Delegate = 0x400000000,          // Other delegate-based callbacks.

        ///////////////////////////////////////////////////////////////////////////////////////////

        Phase0 = 0x1000000000,
        Phase1 = 0x2000000000,
        Phase2 = 0x4000000000,
        Phase3 = 0x8000000000,
        Phase4 = 0x10000000000,
        Phase5 = 0x20000000000,
        Phase6 = 0x40000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VariableMask = Namespace | Resolver | Trace,

        NonBaseMask = Function | Operator | EventManager |
                      RandomNumberGenerator | Debugger |
                      Scope | Alias | Database | Channel |
                      Object | Trusted | Procedure | Execute |
                      Callback | Package | Thread | Interpreter |
                      AppDomain | NativeLibrary | NativeTcl |
                      Delegate,

        All = Plugin | Command | Function | Operator |
              Namespace | Resolver | Policy | Trace |
              EventManager | RandomNumberGenerator |
              Debugger | Scope | Alias | Database |
              Channel | Object | Trusted | Procedure |
              Execute | Callback | Package | Thread |
              Interpreter | AppDomain | NativeLibrary |
              NativeTcl | Delegate,

        Phase0Mask = Phase0 | Reserved,
        Phase1Mask = Phase1 | Native | User,
        Phase2Mask = Phase2 | Native | System,
        Phase3Mask = Phase3 | Managed | User,
        Phase4Mask = Phase4 | Managed | System
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
    //
    // WARNING: Reserved as a placeholder by the core library to represent all
    //          non-flags enumerated types defined by plugins loaded into
    //          isolated application domains.  Do not modify.
    //
    [ObjectId("9829bd1e-25bb-4445-bc00-5ae4dbcd8ab5")]
    internal enum StubEnum
    {
        //
        // HACK: Every enum type must have at least one value and zero is
        //       always implicitly allowed anyhow [by the CLR]; therefore,
        //       this just formalizes that behavior.
        //
        None = 0x0
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: Reserved as a placeholder by the core library to represent all
    //          flags enumerated types defined by plugins loaded into isolated
    //          application domains.  Do not modify.
    //
    [Flags()]
    [ObjectId("a4e04c3a-bd77-426e-8149-9da823537be2")]
    internal enum StubFlagsEnum
    {
        //
        // HACK: Every enum type must have at least one value and zero is
        //       always implicitly allowed anyhow [by the CLR]; therefore,
        //       this just formalizes that behavior.
        //
        None = 0x0
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("609f71c2-9bad-4561-9725-71b47eb928cb")]
    internal enum FloatingPointClass /* TIP #521 */
    {
        NaN = 0,
        Infinite = 1,
        Zero = 2,
        SubNormal = 3,
        Normal = 4
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("956f972f-4c63-4009-a142-98e1765fc752")]
    internal enum InterpreterStateFlags : ulong
    {
        None = 0x0,                      /* No flags. */
        Invalid = 0x1,                   /* Invalid, do not use. */
        Reserved1 = 0x2,                 /* Reserved, do not use. */
        PendingCleanup = 0x4,            /* Interpreter is pending cleanup when
                                          * the current evaluation stack is
                                          * unwound (delete all commands,
                                          * procedures, and global variables).
                                          * This flag is used by the namespace
                                          * deletion subsystem. */
        Shared = 0x8,                    /* The interpreter is shared with an
                                          * external component and must not be
                                          * disposed.  This flag is used by the
                                          * [interp shareinterp] sub-command. */
        PendingPolicies = 0x10,          /* Skip all command and file policy
                                          * checks?  This is used internally to
                                          * prevent unwanted mutual recursion. */
        PendingTraces = 0x20,            /* Skip all variable traces?  This is
                                          * used internally to prevent unwanted
                                          * mutual recursion. */
        PendingPackageIndexes = 0x40,    /* Skip searching for package indexes.
                                          * This flag prevents a package index
                                          * that modifies the auto-path from
                                          * triggering a nested package index
                                          * search.  This is used internally to
                                          * prevent unwanted mutual recursion. */
        SecurityWasEnabled = 0x80,       /* The ScriptOps.EnableOrDisableSecurity
                                          * method successfully enabled security.
                                          */
#if DEBUG
        StrictCallStack = 0x100,         /* Throw an exception if the call stack
                                          * appears to be in an invalid state? */
#endif
        ScriptLocation = 0x200,          /* Keep track of all script locations;
                                          * if not set, only those pushed by
                                          * [source] are tracked. */
        StrictScriptLocation = 0x400,    /* Throw an exception if called upon
                                          * to push or pop a script location
                                          * when they are not available (i.e.
                                          * null). */
#if DEBUGGER && DEBUGGER_BREAKPOINTS
        ArgumentLocation = 0x800,        /* Keep track of Argument locations. */
        ArgumentLocationLock = 0x1000,   /* Do not modify the ArgumentLocation
                                          * flag automatically (e.g. via the
                                          * [source] command, etc). */
#endif
#if SCRIPT_ARGUMENTS
        ScriptArguments = 0x2000,        /* Keep track of script argument lists for
                                          * all nested command invocations. */
        StrictScriptArguments = 0x4000,  /* Throw an exception if called upon to push
                                          * or pop a script argument list when they
                                          * are not available (i.e. null). */
#endif
        ReUseProfiler = 0x8000,          /* The profiler instance associated with the
                                          * interpreter may be reused for non-engine
                                          * operations. */
#if ISOLATED_PLUGINS
        NoIsolatedNotify = 0x10000,      /* Any plugins that are loaded into isolated
                                          * application domains should not be notified
                                          * of any interpreter events. */
#endif
#if SHELL
        KioskLock = 0x20000,             /* The interactive shell is currently operating
                                          * in "kiosk" mode. */
        KioskArgv = 0x40000,             /* Grab the $argv from the interpreter and make
                                          * use of it before reentering the interactive
                                          * loop. */
#endif
        HighPriority = 0x80000,          /* This interpreter instance is important to
                                          * the overall application or process.  This
                                          * flag MAY cause the interpreter to consume
                                          * more resources in the pursuit of a higher
                                          * level of performance. */
        AutoTraceObject = 0x100000,      /* Skip adding the ObjectTraceCallback to the
                                          * list of traces for a variable if the value
                                          * does not currently represent an opaque
                                          * object handle. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        TraceTextWriterOwned = 0x200000, /* The TraceTextWriter is owned by the current
                                          * interpreter and should be disposed. */
        DebugTextWriterOwned = 0x400000, /* The DebugTextWriter is owned by the current
                                          * interpreter and should be disposed. */
        NoDispose = 0x800000,            /* Prevent the interpreter from actually being
                                          * disposed. */
        PackageScanWhatIf = 0x1000000,   /* Do not actually add any packages based on
                                          * the [package ifneeded] sub-command. */

#if SHELL
        InitializeShell = 0x2000000,     /* Perform shell script library initialization
                                          * when entering the interactive loop. */
        ReadLineDisabled = 0x4000000,    /* Disable use of the IHost.ReadLine method for
                                          * use by the interactive loop.  This means the
                                          * interactive loop will not read any input from
                                          * the interactive user, i.e. any input must be
                                          * pre-queued via the IDebugger interface. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForDefaultUse = 0x8000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUG
        MaybeStrictCallStack = StrictCallStack | Reserved1,
#else
        MaybeStrictCallStack = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        MaybeNoIsolatedNotify = NoIsolatedNotify | Reserved1,
#else
        MaybeNoIsolatedNotify = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        MaybeInitializeShell = InitializeShell | Reserved1,
#else
        MaybeInitializeShell = None | Reserved1,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = (MaybeStrictCallStack | ReUseProfiler |
                   TraceTextWriterOwned | DebugTextWriterOwned |
                   MaybeNoIsolatedNotify | MaybeInitializeShell |
                   ForDefaultUse) & ~Reserved1
    }
}
