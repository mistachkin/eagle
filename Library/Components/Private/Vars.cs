/*
 * Vars.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Constants;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class is a container for the well-known names used by the script
    /// engine for its reserved variables and array elements.  It groups those
    /// names into nested static classes by purpose (e.g. package naming, core
    /// library variables, version support, the platform array, etc).
    /// </summary>
    [ObjectId("e8dd6520-63db-4d7a-9ab3-f3bbe0f00d82")]
    internal static class Vars
    {
        #region Core Package Naming
        /// <summary>
        /// This class contains the names and name fragments associated with the
        /// managed Eagle core "package", including the prefix used to construct
        /// reserved variable names.
        /// </summary>
        [ObjectId("b5b671ab-2834-497c-81b2-6897a50c0467")]
        internal static class Package
        {
            //
            // NOTE: The name used by the managed Eagle core "package".
            //
            /// <summary>
            /// The name used by the managed Eagle core "package".
            /// </summary>
            public static readonly string Name = GlobalState.GetPackageName();

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to set the variables in the platform array(s), etc.
            //
            /// <summary>
            /// The lower-cased package name, used to set the variables in the
            /// platform array(s), etc.
            /// </summary>
            public static readonly string NameNoCase = Name.ToLower();

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The typical prefix used for reserved variable names.
            //
            /// <summary>
            /// The typical prefix used for reserved variable names.
            /// </summary>
            public static readonly string Prefix = NameNoCase + "_";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Library Variables
        /// <summary>
        /// This class contains the names of the reserved variables used
        /// internally by the script engine core, including those used by the
        /// marshaller, the debugger subsystem, file name resolution, the test
        /// suite, shell argument handling, and the interactive shell.
        /// </summary>
        [ObjectId("c6ba3a42-92ae-4ada-ab2b-a237ebaeab43")]
        internal static class Core
        {
            #region For Core Marshaller Use Only
            //
            // NOTE: Used by the CommandCallback class for temporary storage
            //       of ByRef parameter values.
            //
            /// <summary>
            /// Used by the CommandCallback class for temporary storage of ByRef
            /// parameter values.
            /// </summary>
            public static readonly string Prefix = Package.Prefix + "vars_";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used by [object] to represent a null object.  Do NOT
            //       use it for any other purpose.
            //
            /// <summary>
            /// Used by [object] to represent a null object.  Do NOT use it for
            /// any other purpose.
            /// </summary>
            public static readonly string Null = _String.Null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For (Local & Remote) Debugger Use Only
            //
            // NOTE: Used by the debugger subsystem (DebuggerOps, etc).  Do
            //       NOT use it for any other purpose.
            //
            /// <summary>
            /// Used by the debugger subsystem (DebuggerOps, etc).  Do NOT use
            /// it for any other purpose.
            /// </summary>
            public static readonly string Debugger = Package.Prefix +
                "debugger";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For File Name Resolution Use Only
            //
            // NOTE: Used by the file name resolution subsystem (PathOps).
            //       This is used within the PathOps static class to mutate
            //       the fully qualified path of a particular script file.
            //       Do NOT use it for any other purpose.
            //
            /// <summary>
            /// Used by the file name resolution subsystem (PathOps).  This is
            /// used within the PathOps static class to mutate the fully
            /// qualified path of a particular script file.  Do NOT use it for
            /// any other purpose.
            /// </summary>
            public static readonly string Paths = Package.Prefix + "paths";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Test Suite Use Only
            //
            // NOTE: Used by the unit testing functionality.  Do NOT use it
            //       for any other purpose.
            //
            /// <summary>
            /// Used by the unit testing functionality.  Do NOT use it for any
            /// other purpose.
            /// </summary>
            public static readonly string Tests = Package.Prefix +
                "tests";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is a transient (temporary) variable for use during
            //       test file evaluation only.  Do NOT use it for any other
            //       purpose.
            //
            /// <summary>
            /// This is a transient (temporary) variable for use during test
            /// file evaluation only.  Do NOT use it for any other purpose.
            /// </summary>
            public static readonly string TestFile = "test_file";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the fully qualified path and file name for the
            //       test suite log.
            //
            /// <summary>
            /// This is the fully qualified path and file name for the test
            /// suite log.
            /// </summary>
            public static readonly string TestLog = "test_log";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is an array variable used by the test suite and its
            //       related procedures to prevent various default actions
            //       from being taken (e.g. constraint checks, warnings, etc).
            //
            /// <summary>
            /// This is an array variable used by the test suite and its related
            /// procedures to prevent various default actions from being taken
            /// (e.g. constraint checks, warnings, etc).
            /// </summary>
            public static readonly string No = "no";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Argument Handling Use Only
            /// <summary>
            /// The name of the variable holding the count of the simulated
            /// ("what-if") shell arguments.
            /// </summary>
            public static readonly string WhatIfShellArgumentCount = "whatIfArgc";

            /// <summary>
            /// The name of the variable holding the simulated ("what-if") shell
            /// arguments.
            /// </summary>
            public static readonly string WhatIfShellArguments = "whatIfArgv";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Support Use Only
            //
            // NOTE: Reserved for use by the interactive shell (this variable
            //       may -OR- may not actually be defined).  Do NOT use it
            //       for any other purpose.
            //
            /// <summary>
            /// Reserved for use by the interactive shell (this variable may
            /// -OR- may not actually be defined).  Do NOT use it for any other
            /// purpose.
            /// </summary>
            public static readonly string Shell = Package.Prefix + "shell";
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Version Support
        /// <summary>
        /// This class contains the well-known string values used to describe
        /// the version, trust, and release characteristics of the script
        /// engine.
        /// </summary>
        [ObjectId("dce6d9bd-6fdf-4f39-b9ce-ff4b1dd557a4")]
        internal static class Version
        {
            //
            // NOTE: Used to show the release as "trusted".  It should only be
            //       used if the primary assembly file has been signed with an
            //       Authenticode (X.509) certificate and the certificate is
            //       trusted on this machine.
            //
            /// <summary>
            /// Used to show the release as "trusted".  It should only be used
            /// if the primary assembly file has been signed with an
            /// Authenticode (X.509) certificate and the certificate is trusted
            /// on this machine.
            /// </summary>
            public static readonly string TrustedValue = "trusted";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to mark the release as "genuine"...  :P~
            //
            /// <summary>
            /// Used to mark the release as "genuine".
            /// </summary>
            public static readonly string GenuineValue = "genuine";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to mark builds as official or unofficial releases.
            //
            /// <summary>
            /// Used to mark builds as official or unofficial releases.
            /// </summary>
            public static readonly string OfficialValue =
                RuntimeOps.IsOfficial() ? "official" : "unofficial";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to mark builds as stable or unsable releases.
            //
            /// <summary>
            /// Used to mark builds as stable or unstable releases.
            /// </summary>
            public static readonly string StableValue =
                RuntimeOps.IsStable() ? "stable" : "unstable";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the system default version when another value
            //       is not available.
            //
            /// <summary>
            /// This is the system default version when another value is not
            /// available.
            /// </summary>
            public static readonly string DefaultValue = "1.0";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "Safe" Interpreter Use Only
        /// <summary>
        /// This class contains the well-known string values used by the "safe"
        /// interpreter support.
        /// </summary>
        [ObjectId("96675184-161a-409a-8803-a341970c1e04")]
        internal static class Safe
        {
            //
            // NOTE: Used by the "safe" interpreter path scrubber.  Do NOT
            //       use it for any other purpose.
            //
            /// <summary>
            /// Used by the "safe" interpreter path scrubber.  Do NOT use it for
            /// any other purpose.
            /// </summary>
            public static readonly string BaseDirectory = "{BaseDirectory}";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Interactive Shell Support Use Only
#if SHELL
        /// <summary>
        /// This class contains the message strings used by the interactive
        /// shell to construct the "about" banner.
        /// </summary>
        [ObjectId("b76c9944-ea28-4c22-a367-d4d6be17654b")]
        internal static class Description
        {
            //
            // NOTE: Used for the "about" banner.
            //
            /// <summary>
            /// Used for the "about" banner.  This is the format string used to
            /// describe the package.
            /// </summary>
            public static readonly string Package =
                "A Tcl {0} compatible interpreter for the Common Language Runtime.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Also used for the "about" banner.
            //
            /// <summary>
            /// Also used for the "about" banner.  This indicates an official
            /// build.
            /// </summary>
            public static readonly string Official =
                "Core: This is an official build.";

            /// <summary>
            /// Also used for the "about" banner.  This indicates an unofficial
            /// build.
            /// </summary>
            public static readonly string Unofficial =
                "Core: This is an unofficial build.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.  Phrases are hard-coded
            //       because the wording is a bit different in English when
            //       using the words "trusted" and "untrusted".
            //
            /// <summary>
            /// Used for the "about" banner.  This indicates a trusted build.
            /// </summary>
            public static readonly string Trusted =
                "Core: This is a trusted build.";

            /// <summary>
            /// Used for the "about" banner.  This indicates an untrusted build.
            /// </summary>
            public static readonly string Untrusted =
                "Core: This is an untrusted build.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.  Phrases are hard-coded
            //       because the wording is a bit different in English when
            //       using the words "stable" and "unstable".
            //
            /// <summary>
            /// Used for the "about" banner.  This indicates a stable build.
            /// </summary>
            public static readonly string Stable =
                "Core: This is a stable build.";

            /// <summary>
            /// Used for the "about" banner.  This indicates an unstable build.
            /// </summary>
            public static readonly string Unstable =
                "Core: This is an unstable build.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.
            //
            /// <summary>
            /// Used for the "about" banner.  This is the format string used to
            /// report whether the interpreter thinks it is "safe".
            /// </summary>
            public static readonly string Safe =
                "Core: Interpreter {0} thinks it is \"{1}\".";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.
            //
            /// <summary>
            /// Used for the "about" banner.  This is the format string used to
            /// report the script security state of the interpreter.
            /// </summary>
            public static readonly string Security =
                "Core: Interpreter {0} thinks script security is {1}.";

            ///////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN || MAYBE_ENTERPRISE_LOCKDOWN
            //
            // NOTE: Used for the "about" banner.
            //
            /// <summary>
            /// Used for the "about" banner.  This is the format string used to
            /// report the enterprise lockdown state of the interpreter.
            /// </summary>
            public static readonly string Lockdown =
                "Core: Interpreter {0} thinks enterprise lockdown is {1}.";
#endif

            ///////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
            //
            // NOTE: Used for the "about" banner.
            //
            /// <summary>
            /// Used for the "about" banner.  This is the format string used to
            /// report the plugin isolation state of the interpreter.
            /// </summary>
            public static readonly string Isolated =
                "Core: Interpreter {0} thinks plugin isolation is {1}.";
#endif
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "eagle_debugger" Array Use Only
        /// <summary>
        /// This class contains the names of the elements of the
        /// "eagle_debugger" array.
        /// </summary>
        [ObjectId("29cb50d9-3570-44d8-b2d2-8364cce53176")]
        internal static class Debugger
        {
            //
            // NOTE: The name of the "special" script currently being
            //       evaluated by the interpreter.  These scripts are
            //       generally evaluated during interpreter creation.
            //
            /// <summary>
            /// The name of the "special" script currently being evaluated by
            /// the interpreter.  These scripts are generally evaluated during
            /// interpreter creation.
            /// </summary>
            public static readonly string ScriptName = "scriptName";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Expression Processing Use Only
        /// <summary>
        /// This class contains the well-known string values recognized during
        /// expression processing.
        /// </summary>
        [ObjectId("70e42624-c59c-464d-b183-4ea48b765962")]
        internal static class Expression
        {
            //
            // NOTE: These strings are recognized as doubles by the
            //       Eagle expression parser.
            //
            /// <summary>
            /// The string recognized as positive infinity (a double) by the
            /// Eagle expression parser.
            /// </summary>
            public static readonly string Infinity =
                Characters.Infinity.ToString();

            /// <summary>
            /// The string recognized as Pi (single or double) by the Eagle
            /// expression parser.
            /// </summary>
            public static readonly string Pi =
                Characters.GreekSmallLetterPi.ToString();

            /// <summary>
            /// The string recognized as Euler's number (single or double) by
            /// the Eagle expression parser.
            /// </summary>
            public static readonly string Euler =
                Characters.EulerConstant.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "eagle_platform" Array Use Only
        //
        // NOTE: These names are referred to directly from scripts and where
        //       applicable are named identically to their Tcl counterparts,
        //       please do not change.
        //
        /// <summary>
        /// This class contains the name of the "eagle_platform" array and the
        /// names of its elements.  These names are referred to directly from
        /// scripts and where applicable are named identically to their Tcl
        /// counterparts.
        /// </summary>
        [ObjectId("f8ff1ea7-dfce-4b34-8bd0-6bb395759605")]
        internal static class Platform
        {
            //
            // NOTE: The name of the script array that contains the
            //       platform specific information.
            //
            /// <summary>
            /// The name of the script array that contains the platform specific
            /// information.
            /// </summary>
            public static readonly string Name = Package.Prefix + "platform";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Script engine version information.
            //
            /// <summary>
            /// The name of the element indicating whether the process is
            /// running with administrative privileges.
            /// </summary>
            public static readonly string Administrator = "administrator";

            /// <summary>
            /// The name of the element containing the address range of the
            /// application.
            /// </summary>
            public static readonly string ApplicationAddressRange = "applicationAddressRange";

            /// <summary>
            /// The name of the element containing the certificate information
            /// for the script engine assembly.
            /// </summary>
            public static readonly string Certificate = "certificate";

            /// <summary>
            /// The name of the element containing the base URI used for update
            /// checking.
            /// </summary>
            public static readonly string UpdateBaseUri = "updateBaseUri";

            /// <summary>
            /// The name of the element containing the path and query used for
            /// update checking.
            /// </summary>
            public static readonly string UpdatePathAndQueryName = "updatePathAndQuery";

            /// <summary>
            /// The name of the element containing the base URI used for
            /// downloads.
            /// </summary>
            public static readonly string DownloadBaseUri = "downloadBaseUri";

            /// <summary>
            /// The name of the element containing the base URI used for
            /// scripts.
            /// </summary>
            public static readonly string ScriptBaseUri = "scriptBaseUri";

            /// <summary>
            /// The name of the element containing the base URI used for
            /// auxiliary content.
            /// </summary>
            public static readonly string AuxiliaryBaseUri = "auxiliaryBaseUri";

            /// <summary>
            /// The name of the element containing the options used when the
            /// script engine was compiled.
            /// </summary>
            public static readonly string CompileOptions = "compileOptions";

            /// <summary>
            /// The name of the element containing the name associated with the
            /// C# compiler options.
            /// </summary>
            public static readonly string CSharpOptionsName = "csharpOptions";

            /// <summary>
            /// The name of the element containing the strong name information
            /// for the script engine assembly.
            /// </summary>
            public static readonly string StrongName = "strongName";

            /// <summary>
            /// The name of the element containing the strong name tag for the
            /// script engine assembly.
            /// </summary>
            public static readonly string StrongNameTag = "strongNameTag";

            /// <summary>
            /// The name of the element containing the hash of the script engine
            /// assembly.
            /// </summary>
            public static readonly string Hash = "hash";

            /// <summary>
            /// The name of the element containing the build epoch.
            /// </summary>
            public static readonly string Epoch = "epoch";

            /// <summary>
            /// The name of the element containing the interpreter time stamp.
            /// </summary>
            public static readonly string InterpreterTimeStamp = "interpreterTimeStamp";

            /// <summary>
            /// The name of the element containing the vendor of the script
            /// engine.
            /// </summary>
            public static readonly string Vendor = "vendor";

            /// <summary>
            /// The name of the element containing the version suffix.
            /// </summary>
            public static readonly string Suffix = "suffix";

            /// <summary>
            /// The name of the element containing the descriptive text or
            /// version suffix.
            /// </summary>
            public static readonly string TextOrSuffix = "textOrSuffix";

            /// <summary>
            /// The name of the element indicating whether the script engine
            /// assembly is in the global assembly cache.
            /// </summary>
            public static readonly string GlobalAssemblyCache = "globalAssemblyCache";

            /// <summary>
            /// The name of the element containing the minimum supported date.
            /// </summary>
            public static readonly string MinimumDate = "minimumDate";

            /// <summary>
            /// The name of the element containing the maximum supported date.
            /// </summary>
            public static readonly string MaximumDate = "maximumDate";

            /// <summary>
            /// The name of the element containing the culture information.
            /// </summary>
            public static readonly string Culture = "culture";

            /// <summary>
            /// The name of the element containing the framework version.
            /// </summary>
            public static readonly string FrameworkVersion = "frameworkVersion";

            /// <summary>
            /// The name of the element containing the extra framework version
            /// information.
            /// </summary>
            public static readonly string FrameworkExtraVersion = "frameworkExtraVersion";

            /// <summary>
            /// The name of the element containing the object identifiers.
            /// </summary>
            public static readonly string ObjectIds = "objectIds";

            /// <summary>
            /// The name of the element indicating whether the process is
            /// running under WOW64.
            /// </summary>
            public static readonly string Wow64 = "wow64";

#if CAS_POLICY
            /// <summary>
            /// The name of the element containing the permission set in effect.
            /// </summary>
            public static readonly string PermissionSet = "permissionSet";
#endif

            /// <summary>
            /// The name of the element containing the processor affinity masks.
            /// </summary>
            public static readonly string ProcessorAffinityMasks = "processorAffinityMasks";

            /// <summary>
            /// The name of the element containing the name of the runtime.
            /// </summary>
            public static readonly string RuntimeName = "runtime";

            /// <summary>
            /// The name of the element containing the image runtime version.
            /// </summary>
            public static readonly string ImageRuntimeVersion = "imageRuntimeVersion";

            /// <summary>
            /// The name of the element containing the target framework.
            /// </summary>
            public static readonly string TargetFramework = "targetFramework";

            /// <summary>
            /// The name of the element containing the runtime version.
            /// </summary>
            public static readonly string RuntimeVersion = "runtimeVersion";

            /// <summary>
            /// The name of the element containing the runtime build.
            /// </summary>
            public static readonly string RuntimeBuild = "runtimeBuild";

            /// <summary>
            /// The name of the element containing the extra runtime version
            /// information.
            /// </summary>
            public static readonly string RuntimeExtraVersion = "runtimeExtraVersion";

            /// <summary>
            /// The name of the element containing the runtime options.
            /// </summary>
            public static readonly string RuntimeOptions = "runtimeOptions";

            /// <summary>
            /// The name of the element containing the build configuration.
            /// </summary>
            public static readonly string Configuration = "configuration";

            /// <summary>
            /// The name of the element containing the build time stamp.
            /// </summary>
            public static readonly string TimeStamp = "timeStamp";

            /// <summary>
            /// The name of the element containing the patch level.
            /// </summary>
            public static readonly string PatchLevel = "patchLevel";

            /// <summary>
            /// The name of the element containing the release name.
            /// </summary>
            public static readonly string Release = "release";

            /// <summary>
            /// The name of the element containing the source identifier.
            /// </summary>
            public static readonly string SourceId = "sourceId";

            /// <summary>
            /// The name of the element containing the source time stamp.
            /// </summary>
            public static readonly string SourceTimeStamp = "sourceTimeStamp";

            /// <summary>
            /// The name of the element containing the source control tag.
            /// </summary>
            public static readonly string Tag = "tag";

            /// <summary>
            /// The name of the element containing the descriptive text.
            /// </summary>
            public static readonly string Text = "text";

            /// <summary>
            /// The name of the element containing the URI.
            /// </summary>
            public static readonly string Uri = "uri";

            /// <summary>
            /// The name of the element containing the public key.
            /// </summary>
            public static readonly string PublicKey = "publicKey";

            /// <summary>
            /// The name of the element containing the public key token.
            /// </summary>
            public static readonly string PublicKeyToken = "publicKeyToken";

            /// <summary>
            /// The name of the element containing the module version
            /// identifier.
            /// </summary>
            public static readonly string ModuleVersionId = "moduleVersionId";

            /// <summary>
            /// The name of the element containing the version.
            /// </summary>
            public static readonly string Version = "version";

            /// <summary>
            /// The name of the element containing the shell patch level.
            /// </summary>
            public static readonly string ShellPatchLevel = "shellPatchLevel";

            /// <summary>
            /// The name of the element containing the shell version.
            /// </summary>
            public static readonly string ShellVersion = "shellVersion";

            /// <summary>
            /// The name of the element containing the native utility
            /// information.
            /// </summary>
            public static readonly string NativeUtility = "nativeUtility";

            /// <summary>
            /// The name of the element containing the timeout value.
            /// </summary>
            public static readonly string Timeout = "timeout";

            ///////////////////////////////////////////////////////////////////

#if NETWORK
            /// <summary>
            /// The name of the element containing the network timeout value.
            /// </summary>
            public static readonly string NetworkTimeout = "networkTimeout";
#endif

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The default value for the C# compiler options (a null
            /// reference).
            /// </summary>
            public static readonly string CSharpOptionsValue = null; /* TODO: Good default? */

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The format string used to construct the path and query for
            /// update checking.
            /// </summary>
            public static readonly string UpdatePathAndQueryFormat =
                ".txt{1}?v={0}";

            /// <summary>
            /// The format string used to construct the path and query for
            /// update checking against the stable release.
            /// </summary>
            public static readonly string UpdateStablePathAndQueryFormat =
                "stable" + UpdatePathAndQueryFormat;

            /// <summary>
            /// The format string used to construct the path and query for
            /// update checking against the latest (unstable) release.
            /// </summary>
            public static readonly string UpdateUnstablePathAndQueryFormat =
                "latest" + UpdatePathAndQueryFormat;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The default value for the update path and query, computed based
            /// on whether this is a stable release.
            /// </summary>
            public static readonly string UpdatePathAndQueryValue =
                RuntimeOps.GetUpdatePathAndQuery(null, RuntimeOps.IsStable(), null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [parse options] Sub-Command Use Only
        /// <summary>
        /// This class contains the names of the array elements used by the
        /// [parse options] sub-command.
        /// </summary>
        [ObjectId("3facc96f-c6ae-495e-813d-3b906382377e")]
        internal static class OptionSet
        {
            /// <summary>
            /// The name of the element containing the parsed value.
            /// </summary>
            public static readonly string Value = "value";

            /// <summary>
            /// The name of the element containing the parsed options.
            /// </summary>
            public static readonly string Options = "options";

            /// <summary>
            /// The name of the element containing the index of the next
            /// argument after the parsed options.
            /// </summary>
            public static readonly string NextIndex = "nextIndex";

            /// <summary>
            /// The name of the element containing the index of the end of the
            /// parsed options.
            /// </summary>
            public static readonly string EndIndex = "endIndex";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [sql execute] / [sql foreach] Sub-Command Use Only
        /// <summary>
        /// This class contains the names of the array elements used by the
        /// [sql execute] and [sql foreach] sub-commands.
        /// </summary>
        [ObjectId("5bcb3c76-9061-40fc-b728-9cd2b1c052a2")]
        internal static class ResultSet
        {
            /// <summary>
            /// The name of the element containing the column names of the
            /// result set.
            /// </summary>
            public static readonly string Names = "names"; // shared

            /// <summary>
            /// The name of the element containing the count of result rows.
            /// </summary>
            public static readonly string Count = "count"; // shared

            /// <summary>
            /// The name of the element containing the result rows (used by
            /// [sql execute]).
            /// </summary>
            public static readonly string Rows = "rows"; // [sql execute]

            /// <summary>
            /// The name of the element containing the current row (used by
            /// [sql foreach]).
            /// </summary>
            public static readonly string Row = "row"; // [sql foreach]

            /// <summary>
            /// The name of the element containing the current value (used by
            /// [sql foreach]).
            /// </summary>
            public static readonly string Value = "value"; // [sql foreach]

            /// <summary>
            /// The name of the element containing the time spent preparing the
            /// statement.
            /// </summary>
            public static readonly string Prepare = "prepare"; // shared

            /// <summary>
            /// The name of the element containing the time spent executing the
            /// statement.
            /// </summary>
            public static readonly string Execute = "execute"; // shared

            /// <summary>
            /// The name of the element containing the total elapsed time.
            /// </summary>
            public static readonly string Time = "time"; // shared
        }
        #endregion
    }
}
