/*
 * EnvVars.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class provides the names of the environment variables recognized by
    /// Eagle, covering runtime behavior overrides, external operating-system
    /// variables, native Tcl integration, and script library and package
    /// discovery.
    /// </summary>
    [ObjectId("4562d910-ab19-460e-8fa0-e56b166d46f2")]
    public static class EnvVars
    {
        #region Runtime Behavior Environment Variables
#if TEST
        /// <summary>
        /// The name of the environment variable that controls whether test-only
        /// commands are made available.
        /// </summary>
        public static readonly string TestCommands = "TestCommands";
#endif

        /// <summary>
        /// The name of the environment variable that controls whether the
        /// interpreter may be used from any thread.
        /// </summary>
        public static readonly string AllowAnyThread = "AllowAnyThread";

        /// <summary>
        /// The name of the environment variable that specifies the assembly
        /// anchor path used to locate the Eagle base directory.
        /// </summary>
        public static readonly string AssemblyAnchorPath = "AssemblyAnchorPath";
        /// <summary>
        /// The name of the environment variable that requests a debugger break
        /// at startup.
        /// </summary>
        public static readonly string Break = "Break";
        /// <summary>
        /// The name of the environment variable that suppresses a debugger
        /// break at startup.
        /// </summary>
        public static readonly string NoBreak = "NoBreak";

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        /// <summary>
        /// The name of the environment variable that bumps the cache level.
        /// </summary>
        public static readonly string BumpCacheLevel = "BumpCacheLevel";
        /// <summary>
        /// The name of the environment variable that specifies the cache flags.
        /// </summary>
        public static readonly string CacheFlags = "CacheFlags";
#endif

        /// <summary>
        /// The name of the environment variable that specifies the data flags.
        /// </summary>
        public static readonly string DataFlags = "DataFlags";
        /// <summary>
        /// The name of the environment variable that enables fail-safe
        /// interpreter creation.
        /// </summary>
        public static readonly string CreateFailSafe = "CreateFailSafe";
        /// <summary>
        /// The name of the environment variable that specifies the interpreter
        /// creation flags.
        /// </summary>
        public static readonly string CreateFlags = "CreateFlags";
        /// <summary>
        /// The name of the environment variable that specifies the host
        /// creation flags.
        /// </summary>
        public static readonly string HostCreateFlags = "HostCreateFlags";
        /// <summary>
        /// The name of the environment variable that specifies the interpreter
        /// flags.
        /// </summary>
        public static readonly string InterpreterFlags = "InterpreterFlags";
        /// <summary>
        /// The name of the environment variable that specifies the interpreter
        /// initialization flags.
        /// </summary>
        public static readonly string InitializeFlags = "InitializeFlags";
        /// <summary>
        /// The name of the environment variable that specifies the script
        /// flags.
        /// </summary>
        public static readonly string ScriptFlags = "ScriptFlags";
        /// <summary>
        /// The name of the environment variable that specifies the ellipsis
        /// limit used when truncating output.
        /// </summary>
        public static readonly string EllipsisLimit = "EllipsisLimit";
        /// <summary>
        /// The name of the environment variable that enables single-stepping.
        /// </summary>
        public static readonly string Step = "Step";
        /// <summary>
        /// The name of the environment variable that forces interactive mode.
        /// </summary>
        public static readonly string Interactive = "Interactive";
        /// <summary>
        /// The name of the environment variable that overrides whether the
        /// process is considered user-interactive.
        /// </summary>
        public static readonly string UserInteractive = "UserInteractive";
        /// <summary>
        /// The name of the environment variable that requests clearing of the
        /// trace listeners.
        /// </summary>
        public static readonly string ClearTrace = "ClearTrace";
        /// <summary>
        /// The name of the environment variable that controls console usage.
        /// </summary>
        public static readonly string Console = "Console";
        /// <summary>
        /// The name of the environment variable that enables verbose output.
        /// </summary>
        public static readonly string Verbose = "Verbose";
        /// <summary>
        /// The name of the environment variable that enables debug output.
        /// </summary>
        public static readonly string Debug = "Debug";
        /// <summary>
        /// The name of the environment variable that enables timing
        /// measurement.
        /// </summary>
        public static readonly string MeasureTime = "MeasureTime";

#if TEST
        /// <summary>
        /// The name of the environment variable that enables script tracing.
        /// </summary>
        public static readonly string ScriptTrace = "ScriptTrace";
#endif

#if ISOLATED_PLUGINS
        /// <summary>
        /// The name of the environment variable that forces plugins to be
        /// loaded in an isolated application domain.
        /// </summary>
        public static readonly string Isolated = "Isolated";
#endif

        /// <summary>
        /// The name of the environment variable that forces use of the modern
        /// cryptographic algorithms.
        /// </summary>
        public static readonly string ForceModernAlgorithms = "ForceModernAlgorithms";
        /// <summary>
        /// The name of the environment variable that controls the security
        /// subsystem.
        /// </summary>
        public static readonly string Security = "Security";
        /// <summary>
        /// The name of the environment variable that enables population of the
        /// result stack.
        /// </summary>
        public static readonly string PopulateResultStack = "PopulateResultStack";
        /// <summary>
        /// The name of the environment variable that enables inclusion of the
        /// result stack.
        /// </summary>
        public static readonly string IncludeResultStack = "IncludeResultStack";
        /// <summary>
        /// The name of the environment variable that enables tracing of the
        /// setup process.
        /// </summary>
        public static readonly string SetupTrace = "SetupTrace";
        /// <summary>
        /// The name of the environment variable that enables capture of a stack
        /// trace for trace output.
        /// </summary>
        public static readonly string TraceStack = "TraceStack";
        /// <summary>
        /// The name of the environment variable that directs trace output to
        /// the interpreter host.
        /// </summary>
        public static readonly string TraceToHost = "TraceToHost";
        /// <summary>
        /// The name of the environment variable that routes complaints through
        /// the trace subsystem.
        /// </summary>
        public static readonly string ComplainViaTrace = "ComplainViaTrace";
        /// <summary>
        /// The name of the environment variable that routes complaints through
        /// the test subsystem.
        /// </summary>
        public static readonly string ComplainViaTest = "ComplainViaTest";
        /// <summary>
        /// The name of the environment variable that enables profiling.
        /// </summary>
        public static readonly string Profile = "Profile";
        /// <summary>
        /// The name of the environment variable that enables native package
        /// pre-initialization.
        /// </summary>
        public static readonly string NativePackagePreInitialize = "NativePackagePreInitialize";
        /// <summary>
        /// The name of the environment variable that forces the security
        /// subsystem to be enabled.
        /// </summary>
        public static readonly string ForceSecurity = "ForceSecurity";
        /// <summary>
        /// The name of the environment variable that suppresses the security
        /// update.
        /// </summary>
        public static readonly string NoSecurityUpdate = "NoSecurityUpdate";
        /// <summary>
        /// The name of the environment variable that disables loading of
        /// application settings.
        /// </summary>
        public static readonly string NoAppSettings = "NoAppSettings";
        /// <summary>
        /// The name of the environment variable that enables use of named
        /// events.
        /// </summary>
        public static readonly string UseNamedEvents = "UseNamedEvents";

#if CONFIGURATION
        /// <summary>
        /// The name of the environment variable that requests a refresh of the
        /// application settings.
        /// </summary>
        public static readonly string RefreshAppSettings = "RefreshAppSettings";
#endif

#if XML
        /// <summary>
        /// The name of the environment variable that enables use of XML
        /// configuration files.
        /// </summary>
        public static readonly string UseXmlFiles = "UseXmlFiles";
        /// <summary>
        /// The name of the environment variable that enables merging of XML
        /// application settings.
        /// </summary>
        public static readonly string MergeXmlAppSettings = "MergeXmlAppSettings";
        /// <summary>
        /// The name of the environment variable that enables merging of all
        /// application settings.
        /// </summary>
        public static readonly string MergeAllAppSettings = "MergeAllAppSettings";
#endif

        /// <summary>
        /// The name of the environment variable that disables process exit
        /// handling.
        /// </summary>
        public static readonly string NoExit = "NoExit";
        /// <summary>
        /// The name of the environment variable that disables interpreter
        /// initialization.
        /// </summary>
        public static readonly string NoInitialize = "NoInitialize";

#if NETWORK && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The name of the environment variable that disables trusting of
        /// remote content.
        /// </summary>
        public static readonly string NoTrustedRemote = "NoTrustedRemote";
        /// <summary>
        /// The name of the environment variable that forces trusting of remote
        /// content.
        /// </summary>
        public static readonly string ForceTrustedRemote = "ForceTrustedRemote";
        /// <summary>
        /// The name of the environment variable that specifies the password
        /// for a trusted bundle.
        /// </summary>
        public static readonly string TrustedBundlePassword = "TrustedBundlePassword";
#endif

        /// <summary>
        /// The name of the environment variable that disables execution of
        /// startup scripts.
        /// </summary>
        public static readonly string NoStartups = "NoStartups";

#if THREADING
        /// <summary>
        /// The name of the environment variable that disables creation of
        /// worker threads.
        /// </summary>
        public static readonly string NoWorkers = "NoWorkers";
#endif

        /// <summary>
        /// The name of the environment variable that disables writing of the
        /// interactive prompt.
        /// </summary>
        public static readonly string NoWritePrompt = "NoWritePrompt";
        /// <summary>
        /// The name of the environment variable that disables the interactive
        /// loop.
        /// </summary>
        public static readonly string NoLoop = "NoLoop";
        /// <summary>
        /// The name of the environment variable that disables throwing when a
        /// disposed object is used.
        /// </summary>
        public static readonly string NoThrowOnDisposed = "NoThrowOnDisposed";
        /// <summary>
        /// The name of the environment variable that enables attaching to an
        /// existing resource.
        /// </summary>
        public static readonly string UseAttach = "UseAttach";
        /// <summary>
        /// The name of the environment variable that enables forced behavior.
        /// </summary>
        public static readonly string UseForce = "UseForce";

#if SHELL
        /// <summary>
        /// The name of the environment variable that disables shell
        /// initialization.
        /// </summary>
        public static readonly string NoInitializeShell = "NoInitializeShell";
#endif

#if CONSOLE
        /// <summary>
        /// The name of the environment variable that prevents the console
        /// window from being closed.
        /// </summary>
        public static readonly string NoClose = "NoClose";
#endif

        /// <summary>
        /// The name of the environment variable that disables colorized output.
        /// </summary>
        public static readonly string NoColor = "NoColor";
        /// <summary>
        /// The name of the environment variable that disables use of the
        /// console.
        /// </summary>
        public static readonly string NoConsole = "NoConsole"; /* EXTERNAL */
        /// <summary>
        /// The name of the environment variable that disables console setup.
        /// </summary>
        public static readonly string NoConsoleSetup = "NoConsoleSetup";
        /// <summary>
        /// The name of the environment variable that disables population of the
        /// extra operating-system information.
        /// </summary>
        public static readonly string NoPopulateOsExtra = "NoPopulateOsExtra";

#if NATIVE && WINDOWS
        /// <summary>
        /// The name of the environment variable that disables use of native
        /// mutexes.
        /// </summary>
        public static readonly string NoMutexes = "NoMutexes";
        /// <summary>
        /// The name of the environment variable that disables use of the
        /// native console.
        /// </summary>
        public static readonly string NoNativeConsole = "NoNativeConsole";
#endif

#if NATIVE
        /// <summary>
        /// The name of the environment variable that disables native stack
        /// checking.
        /// </summary>
        public static readonly string NoNativeStack = "NoNativeStack";
#endif

        /// <summary>
        /// The name of the environment variable that disables the splash
        /// banner.
        /// </summary>
        public static readonly string NoSplash = "NoSplash"; /* EXTERNAL */
        /// <summary>
        /// The name of the environment variable that disables setting of the
        /// console window title.
        /// </summary>
        public static readonly string NoTitle = "NoTitle";

#if NATIVE && NATIVE_UTILITY
        /// <summary>
        /// The name of the environment variable that disables use of the native
        /// utility library.
        /// </summary>
        public static readonly string NoNativeUtility = "NoNativeUtility";
#endif

        /// <summary>
        /// The name of the environment variable that disables setting of the
        /// console window icon.
        /// </summary>
        public static readonly string NoIcon = "NoIcon";
        /// <summary>
        /// The name of the environment variable that disables profiling.
        /// </summary>
        public static readonly string NoProfile = "NoProfile";
        /// <summary>
        /// The name of the environment variable that disables cancellation
        /// handling.
        /// </summary>
        public static readonly string NoCancel = "NoCancel";
        /// <summary>
        /// The name of the environment variable that disables tracing.
        /// </summary>
        public static readonly string NoTrace = "NoTrace";
        /// <summary>
        /// The name of the environment variable that disables trace limits.
        /// </summary>
        public static readonly string NoTraceLimits = "NoTraceLimits";
        /// <summary>
        /// The name of the environment variable that disables verbose output.
        /// </summary>
        public static readonly string NoVerbose = "NoVerbose";
        /// <summary>
        /// The name of the environment variable that disables verification of
        /// signed content.
        /// </summary>
        public static readonly string NoVerified = "NoVerified";
        /// <summary>
        /// The name of the environment variable that disables trusting of
        /// content.
        /// </summary>
        public static readonly string NoTrusted = "NoTrusted";
        /// <summary>
        /// The name of the environment variable that forces use of the trusted
        /// hashes.
        /// </summary>
        public static readonly string ForceTrustedHashes = "ForceTrustedHashes";
        /// <summary>
        /// The name of the environment variable that disables use of the
        /// trusted hashes.
        /// </summary>
        public static readonly string NoTrustedHashes = "NoTrustedHashes";
        /// <summary>
        /// The name of the environment variable that disables update checks.
        /// </summary>
        public static readonly string NoUpdates = "NoUpdates";
        /// <summary>
        /// The name of the environment variable that selects quiet mode by
        /// default.
        /// </summary>
        public static readonly string DefaultQuiet = "DefaultQuiet";
        /// <summary>
        /// The name of the environment variable that enables quiet mode.
        /// </summary>
        public static readonly string Quiet = "Quiet";
        /// <summary>
        /// The name of the environment variable that enables capture of a stack
        /// trace for trace output by default.
        /// </summary>
        public static readonly string DefaultTraceStack = "DefaultTraceStack";
        /// <summary>
        /// The name of the environment variable that enables throwing of
        /// exceptions.
        /// </summary>
        public static readonly string Throw = "Throw";
        /// <summary>
        /// The name of the environment variable that enables tracing.
        /// </summary>
        public static readonly string Trace = "Trace";
        /// <summary>
        /// The name of the environment variable that forces creation of a safe
        /// interpreter.
        /// </summary>
        public static readonly string Safe = "Safe";
        /// <summary>
        /// The name of the environment variable that specifies a script to run
        /// before shell pre-initialization.
        /// </summary>
        public static readonly string ShellPreInitialize = "ShellPreInitialize";
        /// <summary>
        /// The name of the environment variable that forces creation of a
        /// standard interpreter.
        /// </summary>
        public static readonly string Standard = "Standard";
        /// <summary>
        /// The name of the environment variable that specifies the vendor path.
        /// </summary>
        public static readonly string VendorPath = "VendorPath";
        /// <summary>
        /// The name of the environment variable that specifies the stub path.
        /// </summary>
        public static readonly string StubPath = "StubPath";
        /// <summary>
        /// The name of the environment variable that disables garbage
        /// collection.
        /// </summary>
        public static readonly string NeverGC = "NeverGC";

        /// <summary>
        /// The name of the environment variable that enables strict checking of
        /// the base path.
        /// </summary>
        public static readonly string StrictBasePath = "StrictBasePath";

#if NETWORK
        /// <summary>
        /// The format string for the per-tag environment variable used to
        /// override the web client tag.
        /// </summary>
        public static readonly string WebClientTagFormat1 = "WebClientTag_{0}";
        /// <summary>
        /// The name of the environment variable used to override the default
        /// web client tag.
        /// </summary>
        public static readonly string WebClientTagFormat2 = "WebClientTag";
        /// <summary>
        /// The name of the environment variable that specifies the network
        /// timeout.
        /// </summary>
        public static readonly string NetworkTimeout = "NetworkTimeout";
#endif

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        /// <summary>
        /// The name of the environment variable that disables compaction of the
        /// large object heap during garbage collection.
        /// </summary>
        public static readonly string NeverCompactForGC = "NeverCompactForGC";
#endif

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// The name of the environment variable that specifies the plugin name
        /// patterns.
        /// </summary>
        public static readonly string PluginPatterns = "PluginPatterns";
#endif

        /// <summary>
        /// The name of the environment variable that disables waiting for
        /// pending garbage collection.
        /// </summary>
        public static readonly string NeverWaitForGC = "NeverWaitForGC";
        /// <summary>
        /// The name of the environment variable that forces waiting for pending
        /// garbage collection.
        /// </summary>
        public static readonly string AlwaysWaitForGC = "AlwaysWaitForGC";
        /// <summary>
        /// The name of the environment variable that specifies the native
        /// utility path.
        /// </summary>
        public static readonly string UtilityPath = "UtilityPath";
        /// <summary>
        /// The name of the environment variable that directs trace output to
        /// the configured trace listeners.
        /// </summary>
        public static readonly string TraceToListeners = "TraceToListeners";
        /// <summary>
        /// The name of the environment variable that specifies the trace output
        /// format.
        /// </summary>
        public static readonly string TraceFormat = "TraceFormat";
        /// <summary>
        /// The name of the environment variable that specifies the enabled
        /// trace categories.
        /// </summary>
        public static readonly string TraceCategories = "TraceCategories";
        /// <summary>
        /// The name of the environment variable that specifies the disabled
        /// trace categories.
        /// </summary>
        public static readonly string NoTraceCategories = "NoTraceCategories";
        /// <summary>
        /// The name of the environment variable that specifies the trace
        /// categories that incur a priority penalty.
        /// </summary>
        public static readonly string PenaltyTraceCategories = "PenaltyTraceCategories";
        /// <summary>
        /// The name of the environment variable that specifies the trace
        /// categories that receive a priority bonus.
        /// </summary>
        public static readonly string BonusTraceCategories = "BonusTraceCategories";

        /// <summary>
        /// The name of the environment variable that specifies the trace
        /// priority.
        /// </summary>
        public static readonly string TracePriority = "TracePriority";
        /// <summary>
        /// The name of the environment variable that specifies the trace
        /// priority limits.
        /// </summary>
        public static readonly string TracePriorityLimits = "TracePriorityLimits";
        /// <summary>
        /// The name of the environment variable that specifies the trace
        /// priorities.
        /// </summary>
        public static readonly string TracePriorities = "TracePriorities";
        /// <summary>
        /// The name of the environment variable that specifies the global trace
        /// priorities.
        /// </summary>
        public static readonly string GlobalPriorities = "GlobalPriorities";

        /// <summary>
        /// The name of the environment variable that forces the runtime to be
        /// treated as the .NET Framework 2.0.
        /// </summary>
        public static readonly string TreatAsFramework20 = "TreatAsFramework20";
        /// <summary>
        /// The name of the environment variable that forces the runtime to be
        /// treated as the .NET Framework 4.0.
        /// </summary>
        public static readonly string TreatAsFramework40 = "TreatAsFramework40";

        /// <summary>
        /// The name of the environment variable that forces the runtime to be
        /// treated as .NET Core.
        /// </summary>
        public static readonly string TreatAsDotNetCore = "TreatAsDotNetCore";
        /// <summary>
        /// The name of the environment variable that forces the runtime to be
        /// treated as Mono.
        /// </summary>
        public static readonly string TreatAsMono = "TreatAsMono";

#if NATIVE && WINDOWS && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The name of the environment variable that specifies the trust flags.
        /// </summary>
        public static readonly string TrustFlags = "TrustFlags";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region External Environment Variables
        #region Home Directory Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the user home directory.
        /// </summary>
        public static readonly string Home = "HOME";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the drive of the user home directory.
        /// </summary>
        public static readonly string HomeDrive = "HOMEDRIVE";
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the path of the user home directory.
        /// </summary>
        public static readonly string HomePath = "HOMEPATH";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the computer name.
        /// </summary>
        public static readonly string ComputerName = "COMPUTERNAME";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the user domain.
        /// </summary>
        public static readonly string UserDomain = "USERDOMAIN";
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the user name.
        /// </summary>
        public static readonly string UserName = "USERNAME";
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the user profile directory.
        /// </summary>
        public static readonly string UserProfile = "USERPROFILE";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the environment variable that specifies the special
        /// folder used to derive the user home directory.
        /// </summary>
        public static readonly string SpecialFolder = "SpecialFolder";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows Terminal Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that
        /// identifies a Windows Terminal session.
        /// </summary>
        public static readonly string WindowsTerminalSession = "WT_SESSION";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region X Desktop Group (XDG) Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the X display.
        /// </summary>
        public static readonly string Display = "DISPLAY";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the XDG environment variable that specifies the base
        /// directory for user data files.
        /// </summary>
        public static readonly string XdgDataHome = "XDG_DATA_HOME";
        /// <summary>
        /// The name of the XDG environment variable that specifies the list of
        /// directories searched for data files.
        /// </summary>
        public static readonly string XdgDataDirs = "XDG_DATA_DIRS";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the XDG environment variable that specifies the base
        /// directory for user configuration files.
        /// </summary>
        public static readonly string XdgConfigHome = "XDG_CONFIG_HOME";
        /// <summary>
        /// The name of the XDG environment variable that specifies the list of
        /// directories searched for configuration files.
        /// </summary>
        public static readonly string XdgConfigDirs = "XDG_CONFIG_DIRS";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the XDG environment variable that specifies the base
        /// directory for user runtime files.
        /// </summary>
        public static readonly string XdgRuntimeDir = "XDG_RUNTIME_DIR";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the XDG environment variable that specifies the base
        /// directory for user state files.
        /// </summary>
        public static readonly string XdgStateHome = "XDG_STATE_HOME";

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Extended X Desktop Group (XDG) Environment Variables
        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the base directory for user startup files.
        /// </summary>
        public static readonly string XdgStartupHome = "XDG_STARTUP_HOME";
        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the list of directories searched for startup files.
        /// </summary>
        public static readonly string XdgStartupDirs = "XDG_STARTUP_DIRS";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the base directory for user cloud files.
        /// </summary>
        public static readonly string XdgCloudHome = "XDG_CLOUD_HOME";
        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the list of directories searched for cloud files.
        /// </summary>
        public static readonly string XdgCloudDirs = "XDG_CLOUD_DIRS";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the base directory for user key ring files.
        /// </summary>
        public static readonly string XdgKeyRingHome = "XDG_KEYRING_HOME";
        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the list of directories searched for key ring files.
        /// </summary>
        public static readonly string XdgKeyRingDirs = "XDG_KEYRING_DIRS";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the directory searched for shell files.
        /// </summary>
        public static readonly string XdgShellsDir = "XDG_SHELLS_DIR";
        /// <summary>
        /// The name of the Eagle-extended XDG environment variable that
        /// specifies the directory searched for rule set files.
        /// </summary>
        public static readonly string XdgRuleSetDir = "XDG_RULESET_DIR";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Plugin Environment Variables
        /// <summary>
        /// The name of the environment variable that specifies the build
        /// configuration used by plugins.
        /// </summary>
        public static readonly string Configuration = "CONFIGURATION";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Operating System Shell Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the command interpreter (shell) executable.
        /// </summary>
        public static readonly string ComSpec = "ComSpec";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the processor architecture.
        /// </summary>
        public static readonly string ProcessorArchitecture = "PROCESSOR_ARCHITECTURE";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Text Editor Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the preferred text editor.
        /// </summary>
        public static readonly string Editor = "EDITOR";
        /// <summary>
        /// The default text editor used when no preferred text editor is
        /// specified.
        /// </summary>
        public static readonly string EditorValue = "notepad";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Temporary Path Environment Variables
        //
        // HACK: Per MSDN, these environment variables are checked by the
        //       GetTempPath Win32 API; however, to keep things portable,
        //       the PathOps.GetTempPathViaEnvironment method checks them
        //       in reverse order, after checking several other override
        //       environment variables.
        //
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the temporary directory (primarily on Windows).
        /// </summary>
        public static readonly string Temp = "TEMP"; /* Windows only? */
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the temporary directory.
        /// </summary>
        public static readonly string Tmp = "TMP";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Executable Path Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the executable search path.
        /// </summary>
        public static readonly string Path = "PATH";
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the shared library search path.
        /// </summary>
        public static readonly string LdLibraryPath = "LD_LIBRARY_PATH";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Encoding Environment Variables
        /// <summary>
        /// The name of the operating-system environment variable that specifies
        /// the language and locale.
        /// </summary>
        public static readonly string Language = "LANG";
        /// <summary>
        /// The name of the operating-system environment variable that overrides
        /// all locale categories.
        /// </summary>
        public static readonly string LocaleAll = "LC_ALL";
        /// <summary>
        /// The locale value that indicates the UTF-8 encoding.
        /// </summary>
        public static readonly string Utf8Value = "UTF-8";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Tcl Integration Environment Variables
        /// <summary>
        /// The name of the Eagle-specific environment variable that specifies
        /// the directory containing the native Tcl library.
        /// </summary>
        public static readonly string EagleTclDir = "Eagle_Tcl_Dir";
        /// <summary>
        /// The name of the Eagle-specific environment variable that specifies
        /// the native Tcl library file.
        /// </summary>
        public static readonly string EagleTclDll = "Eagle_Tcl_Dll";
        /// <summary>
        /// The name of the Eagle-specific environment variable that specifies
        /// the native Tk library file.
        /// </summary>
        public static readonly string EagleTkDll = "Eagle_Tk_Dll";

        /// <summary>
        /// The name of the environment variable that specifies the directory
        /// containing the native Tcl library.
        /// </summary>
        public static readonly string TclDir = "Tcl_Dir";
        /// <summary>
        /// The name of the environment variable that specifies the native Tcl
        /// library file.
        /// </summary>
        public static readonly string TclDll = "Tcl_Dll";
        /// <summary>
        /// The name of the environment variable that specifies the native Tk
        /// library file.
        /// </summary>
        public static readonly string TkDll = "Tk_Dll";

        /// <summary>
        /// The name of the Eagle-specific environment variable that specifies
        /// the native Tcl shell executable.
        /// </summary>
        public static readonly string EagleTclShell = "Eagle_Tcl_Shell";
        /// <summary>
        /// The name of the Eagle-specific environment variable that specifies
        /// the native Tk shell executable.
        /// </summary>
        public static readonly string EagleTkShell = "Eagle_Tk_Shell";

        /// <summary>
        /// The name of the environment variable that specifies the native Tcl
        /// shell executable.
        /// </summary>
        public static readonly string TclShell = "Tcl_Shell";
        /// <summary>
        /// The name of the environment variable that specifies the native Tk
        /// shell executable.
        /// </summary>
        public static readonly string TkShell = "Tk_Shell";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Library & Packages Environment Variables
        #region Tcl & Eagle Environment Variables
        //
        // NOTE: These are used by Tcl and Eagle to locate the script library
        //       and/or additional package indexes.
        //
        /// <summary>
        /// The name of the environment variable that specifies the Tcl script
        /// library directory.
        /// </summary>
        public static readonly string TclLibrary = "TCL_LIBRARY";
        /// <summary>
        /// The name of the environment variable that specifies the list of
        /// additional package index directories.
        /// </summary>
        public static readonly string TclLibPath = "TCLLIBPATH";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Only Environment Variables
        //
        // NOTE: The base directory of an Eagle source or binary distribution
        //       (i.e. the directory that contains both the "bin" and "lib"
        //       sub-directories).
        //
        /// <summary>
        /// The name of the environment variable that specifies the base
        /// directory of an Eagle source or binary distribution.
        /// </summary>
        public static readonly string Eagle = "EAGLE";

        //
        // NOTE: The directory that should be used to override the in-use base
        //       directory (i.e. the directory that contains both the "bin"
        //       and "lib" sub-directories).
        //
        /// <summary>
        /// The name of the environment variable that overrides the in-use base
        /// directory of an Eagle distribution.
        /// </summary>
        public static readonly string EagleBase = "EAGLE_BASE";

        //
        // NOTE: The directory that should be used to override the in-use
        //       externals directory (i.e. the root directory that contains
        //       various binaries and other files from "external" projects).
        //
        /// <summary>
        /// The name of the environment variable that overrides the in-use
        /// externals directory of an Eagle distribution.
        /// </summary>
        public static readonly string EagleExternals = "EAGLE_EXTERNALS";

        //
        // NOTE: The directory where the "Eagle1.0" directory, containing the
        //       "init.eagle" file, can be found.  Setting this environment
        //       variable overrides the default file search logic used for this
        //       file; however, it will have no effect unless it is set prior
        //       to referring to anything that will cause the Interpreter type
        //       to be loaded from the [Eagle] assembly.  The alternative is to
        //       use the SetLibraryPath method of the Interpreter class.
        //
        /// <summary>
        /// The name of the environment variable that specifies the directory
        /// containing the "Eagle1.0" script library directory.
        /// </summary>
        public static readonly string EagleLibrary = "EAGLE_LIBRARY";

        //
        // NOTE: The list of directories where "pkgIndex.eagle" files should be
        //       searched for (i.e. they will be added to the "auto_path" for
        //       the interpreter).
        //
        /// <summary>
        /// The name of the environment variable that specifies the list of
        /// directories searched for "pkgIndex.eagle" files.
        /// </summary>
        public static readonly string EagleLibPath = "EAGLELIBPATH";

        ///////////////////////////////////////////////////////////////////////

        #region Console Host Only Environment Variables
        //
        // NOTE: The value of this variable is a reference count managed by the
        //       Setup method of the "Eagle._Hosts.Console" class.  The process
        //       Id is always inserted into this name.
        //
        // WARNING: This environment variable should NOT be changed or removed
        //          by any third-party applications, plugins or scripts.
        //
#if CONSOLE
        /// <summary>
        /// The name of the environment variable, managed by the console host,
        /// whose value is a reference count and into which the process
        /// identifier is always inserted.
        /// </summary>
        public static readonly string EagleLibraryHostsConsole =
            "EAGLE_LIBRARY_HOSTS_CONSOLE_";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppDomainOps Only Environment Variables
        //
        // NOTE: The value of this variable is a reference count managed by
        //       various methods of the "AppDomainOps" class.  The process
        //       Id is always inserted into this name.
        //
        // WARNING: This environment variable should NOT be changed or removed
        //          by any third-party applications, plugins or scripts.
        //
        /// <summary>
        /// The name of the environment variable, managed by the
        /// "AppDomainOps" class, whose value is a count of created application
        /// domains and into which the process identifier is always inserted.
        /// </summary>
        public static readonly string EagleLibraryAppDomainCreateCount =
            "EAGLE_LIBRARY_APPDOMAIN_CREATE_COUNT_";

        /// <summary>
        /// The name of the environment variable, managed by the
        /// "AppDomainOps" class, whose value is a count of unloaded application
        /// domains and into which the process identifier is always inserted.
        /// </summary>
        public static readonly string EagleLibraryAppDomainUnloadCount =
            "EAGLE_LIBRARY_APPDOMAIN_UNLOAD_COUNT_";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The value of this variable is a list managed by various
        //       methods of the "AppDomainOps" class.  The process Id is
        //       always inserted into this name.
        //
        // WARNING: This environment variable should NOT be changed or removed
        //          by any third-party applications, plugins or scripts.
        //
        /// <summary>
        /// The name of the environment variable, managed by the
        /// "AppDomainOps" class, whose value is a list of created application
        /// domains and into which the process identifier is always inserted.
        /// </summary>
        public static readonly string EagleLibraryAppDomainCreateList =
            "EAGLE_LIBRARY_APPDOMAIN_CREATE_LIST_";

        /// <summary>
        /// The name of the environment variable, managed by the
        /// "AppDomainOps" class, whose value is a list of unloaded application
        /// domains and into which the process identifier is always inserted.
        /// </summary>
        public static readonly string EagleLibraryAppDomainUnloadList =
            "EAGLE_LIBRARY_APPDOMAIN_UNLOAD_LIST_";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Package Only Environment Variables
        //
        // NOTE: If this variable is set [to anything], the current AppDomain
        //       shall be considered to be finalizing for unload, even if the
        //       current AppDomain is the default AppDomain.
        //
        // WARNING: This environment variable should NOT be changed or removed
        //          by any third-party applications, plugins or scripts.
        //
#if NATIVE_PACKAGE
        /// <summary>
        /// The name of the environment variable that, when set to anything,
        /// indicates that the current application domain should be considered
        /// to be finalizing for unload.
        /// </summary>
        public static readonly string EagleClrStopping = "EAGLE_CLR_STOPPING";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Test Suite Only Environment Variables
        //
        // NOTE: If set, this will be used by the test suite prior to falling
        //       back on the "TEMP" and "TMP" environment variables.
        //
        /// <summary>
        /// The name of the environment variable used by the test suite to
        /// locate the temporary directory, taking precedence over the "TEMP"
        /// and "TMP" environment variables.
        /// </summary>
        public static readonly string EagleTemp = "EAGLE_TEMP";

        //
        // NOTE: If set, this will be used by the test suite prior to falling
        //       back on the "TEMP" and "TMP" environment variables.
        //
        /// <summary>
        /// The name of the environment variable used by the test suite to
        /// locate the temporary directory, taking precedence over the "TEMP"
        /// and "TMP" environment variables.
        /// </summary>
        public static readonly string EagleTestTemp = "EAGLE_TEST_TEMP";
        #endregion
        #endregion
        #endregion
    }
}
