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
    [ObjectId("4562d910-ab19-460e-8fa0-e56b166d46f2")]
    public static class EnvVars
    {
        #region Runtime Behavior Environment Variables
        public static readonly string AssemblyAnchorPath = "AssemblyAnchorPath";
        public static readonly string Break = "Break";
        public static readonly string NoBreak = "NoBreak";

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        public static readonly string BumpCacheLevel = "BumpCacheLevel";
        public static readonly string CacheFlags = "CacheFlags";
#endif

        public static readonly string CreateFlags = "CreateFlags";
        public static readonly string HostCreateFlags = "HostCreateFlags";
        public static readonly string InterpreterFlags = "InterpreterFlags";
        public static readonly string InitializeFlags = "InitializeFlags";
        public static readonly string ScriptFlags = "ScriptFlags";
        public static readonly string EllipsisLimit = "EllipsisLimit";
        public static readonly string Step = "Step";
        public static readonly string Interactive = "Interactive";
        public static readonly string ClearTrace = "ClearTrace";
        public static readonly string Console = "Console";
        public static readonly string Debug = "Debug";
        public static readonly string MeasureTime = "MeasureTime";

#if TEST
        public static readonly string ScriptTrace = "ScriptTrace";
#endif

#if ISOLATED_PLUGINS
        public static readonly string Isolated = "Isolated";
#endif

        public static readonly string Security = "Security";
        public static readonly string ResultStack = "ResultStack";
        public static readonly string SetupTrace = "SetupTrace";
        public static readonly string TraceStack = "TraceStack";
        public static readonly string TraceToHost = "TraceToHost";
        public static readonly string ComplainViaTrace = "ComplainViaTrace";
        public static readonly string ComplainViaTest = "ComplainViaTest";
        public static readonly string Profile = "Profile";
        public static readonly string NativePackagePreInitialize = "NativePackagePreInitialize";
        public static readonly string ForceSecurity = "ForceSecurity";
        public static readonly string NoSecurityUpdate = "NoSecurityUpdate";
        public static readonly string NoAppSettings = "NoAppSettings";
        public static readonly string UseNamedEvents = "UseNamedEvents";

#if CONFIGURATION
        public static readonly string RefreshAppSettings = "RefreshAppSettings";
#endif

#if XML
        public static readonly string UseXmlFiles = "UseXmlFiles";
        public static readonly string MergeXmlAppSettings = "MergeXmlAppSettings";
        public static readonly string MergeAllAppSettings = "MergeAllAppSettings";
#endif

        public static readonly string NoExit = "NoExit";
        public static readonly string NoInitialize = "NoInitialize";
        public static readonly string NoLoop = "NoLoop";
        public static readonly string NoThrowOnDisposed = "NoThrowOnDisposed";
        public static readonly string UseAttach = "UseAttach";

#if SHELL
        public static readonly string NoInitializeShell = "NoInitializeShell";
#endif

#if CONSOLE
        public static readonly string NoClose = "NoClose";
#endif

        public static readonly string NoColor = "NoColor";
        public static readonly string NoConsole = "NoConsole"; /* EXTERNAL */
        public static readonly string NoConsoleSetup = "NoConsoleSetup";
        public static readonly string NoPopulateOsExtra = "NoPopulateOsExtra";

#if NATIVE && WINDOWS
        public static readonly string NoMutexes = "NoMutexes";
        public static readonly string NoNativeConsole = "NoNativeConsole";
#endif

#if NATIVE
        public static readonly string NoNativeStack = "NoNativeStack";
#endif

        public static readonly string NoSplash = "NoSplash"; /* EXTERNAL */
        public static readonly string NoTitle = "NoTitle";

#if NATIVE && NATIVE_UTILITY
        public static readonly string NoNativeUtility = "NoNativeUtility";
#endif

        public static readonly string NoIcon = "NoIcon";
        public static readonly string NoProfile = "NoProfile";
        public static readonly string NoCancel = "NoCancel";
        public static readonly string NoTrace = "NoTrace";
        public static readonly string NoTraceLimits = "NoTraceLimits";
        public static readonly string NoVerbose = "NoVerbose";
        public static readonly string NoVerified = "NoVerified";
        public static readonly string NoTrusted = "NoTrusted";
        public static readonly string NoTrustedHashes = "NoTrustedHashes";
        public static readonly string NoUpdates = "NoUpdates";
        public static readonly string DefaultQuiet = "DefaultQuiet";
        public static readonly string Quiet = "Quiet";
        public static readonly string DefaultTraceStack = "DefaultTraceStack";
        public static readonly string Throw = "Throw";
        public static readonly string Trace = "Trace";
        public static readonly string Safe = "Safe";
        public static readonly string ShellPreInitialize = "ShellPreInitialize";
        public static readonly string Standard = "Standard";
        public static readonly string VendorPath = "VendorPath";
        public static readonly string StubPath = "StubPath";
        public static readonly string NeverGC = "NeverGC";

        public static readonly string StrictBasePath = "StrictBasePath";

#if NETWORK
        public static readonly string NetworkTimeout = "NetworkTimeout";
#endif

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        public static readonly string NeverCompactForGC = "NeverCompactForGC";
#endif

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        public static readonly string PluginPatterns = "PluginPatterns";
#endif

        public static readonly string NeverWaitForGC = "NeverWaitForGC";
        public static readonly string AlwaysWaitForGC = "AlwaysWaitForGC";
        public static readonly string UtilityPath = "UtilityPath";
        public static readonly string TraceToListeners = "TraceToListeners";
        public static readonly string TraceCategories = "TraceCategories";
        public static readonly string NoTraceCategories = "NoTraceCategories";
        public static readonly string PenaltyTraceCategories = "PenaltyTraceCategories";
        public static readonly string BonusTraceCategories = "BonusTraceCategories";

        public static readonly string TracePriority = "TracePriority";
        public static readonly string TracePriorityLimits = "TracePriorityLimits";
        public static readonly string TracePriorities = "TracePriorities";

        public static readonly string TreatAsFramework20 = "TreatAsFramework20";
        public static readonly string TreatAsFramework40 = "TreatAsFramework40";

        public static readonly string TreatAsDotNetCore = "TreatAsDotNetCore";
        public static readonly string TreatAsMono = "TreatAsMono";

#if NATIVE && WINDOWS && !ENTERPRISE_LOCKDOWN
        public static readonly string TrustFlags = "TrustFlags";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region External Environment Variables
        #region Home Directory Environment Variables
        public static readonly string Home = "HOME";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string HomeDrive = "HOMEDRIVE";
        public static readonly string HomePath = "HOMEPATH";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string ComputerName = "COMPUTERNAME";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string UserDomain = "USERDOMAIN";
        public static readonly string UserName = "USERNAME";
        public static readonly string UserProfile = "USERPROFILE";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string SpecialFolder = "SpecialFolder";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows Terminal Environment Variables
        public static readonly string WindowsTerminalSession = "WT_SESSION";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region X Desktop Group (XDG) Environment Variables
        public static readonly string Display = "DISPLAY";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string XdgDataHome = "XDG_DATA_HOME";
        public static readonly string XdgDataDirs = "XDG_DATA_DIRS";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string XdgConfigHome = "XDG_CONFIG_HOME";
        public static readonly string XdgConfigDirs = "XDG_CONFIG_DIRS";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string XdgRuntimeDir = "XDG_RUNTIME_DIR";

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Extended X Desktop Group (XDG) Environment Variables
        public static readonly string XdgStartupHome = "XDG_STARTUP_HOME";
        public static readonly string XdgStartupDirs = "XDG_STARTUP_DIRS";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string XdgCloudHome = "XDG_CLOUD_HOME";
        public static readonly string XdgCloudDirs = "XDG_CLOUD_DIRS";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string XdgKeyRingHome = "XDG_KEYRING_HOME";
        public static readonly string XdgKeyRingDirs = "XDG_KEYRING_DIRS";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Plugin Environment Variables
        public static readonly string Configuration = "CONFIGURATION";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Operating System Shell Environment Variables
        public static readonly string ComSpec = "ComSpec";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string ProcessorArchitecture = "PROCESSOR_ARCHITECTURE";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Text Editor Environment Variables
        public static readonly string Editor = "EDITOR";
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
        public static readonly string Temp = "TEMP"; /* Windows only? */
        public static readonly string Tmp = "TMP";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Executable Path Environment Variables
        public static readonly string Path = "PATH";
        public static readonly string LdLibraryPath = "LD_LIBRARY_PATH";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Tcl Integration Environment Variables
        public static readonly string EagleTclDll = "Eagle_Tcl_Dll";
        public static readonly string EagleTkDll = "Eagle_Tk_Dll";

        public static readonly string TclDll = "Tcl_Dll";
        public static readonly string TkDll = "Tk_Dll";

        public static readonly string EagleTclShell = "Eagle_Tcl_Shell";
        public static readonly string EagleTkShell = "Eagle_Tk_Shell";

        public static readonly string TclShell = "Tcl_Shell";
        public static readonly string TkShell = "Tk_Shell";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Library & Packages Environment Variables
        #region Tcl & Eagle Environment Variables
        //
        // NOTE: These are used by Tcl and Eagle to locate the script library
        //       and/or additional package indexes.
        //
        public static readonly string TclLibrary = "TCL_LIBRARY";
        public static readonly string TclLibPath = "TCLLIBPATH";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Eagle Only Environment Variables
        //
        // NOTE: The base directory of an Eagle source or binary distribution
        //       (i.e. the directory that contains both the "bin" and "lib"
        //       sub-directories).
        //
        public static readonly string Eagle = "EAGLE";

        //
        // NOTE: The directory that should be used to override the in-use base
        //       directory (i.e. the directory that contains both the "bin"
        //       and "lib" sub-directories).
        //
        public static readonly string EagleBase = "EAGLE_BASE";

        //
        // NOTE: The directory that should be used to override the in-use
        //       externals directory (i.e. the root directory that contains
        //       various binaries and other files from "external" projects).
        //
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
        public static readonly string EagleLibrary = "EAGLE_LIBRARY";

        //
        // NOTE: The list of directories where "pkgIndex.eagle" files should be
        //       searched for (i.e. they will be added to the "auto_path" for
        //       the interpreter).
        //
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
        public static readonly string EagleLibraryHostsConsole =
            "EAGLE_LIBRARY_HOSTS_CONSOLE_";
#endif
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
        public static readonly string EagleClrStopping = "EAGLE_CLR_STOPPING";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Test Suite Only Environment Variables
        //
        // NOTE: If set, this will be used by the test suite prior to falling
        //       back on the "TEMP" and "TMP" environment variables.
        //
        public static readonly string EagleTemp = "EAGLE_TEMP";

        //
        // NOTE: If set, this will be used by the test suite prior to falling
        //       back on the "TEMP" and "TMP" environment variables.
        //
        public static readonly string EagleTestTemp = "EAGLE_TEST_TEMP";
        #endregion
        #endregion
        #endregion
    }
}
