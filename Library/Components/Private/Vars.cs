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

namespace Eagle._Components.Private
{
    [ObjectId("e8dd6520-63db-4d7a-9ab3-f3bbe0f00d82")]
    internal static class Vars
    {
        #region Core Package Naming
        [ObjectId("b5b671ab-2834-497c-81b2-6897a50c0467")]
        internal static class Package
        {
            //
            // NOTE: The name used by the managed Eagle core "package".
            //
            public static readonly string Name = GlobalState.GetPackageName();

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to set the variables in the platform array(s), etc.
            //
            public static readonly string NameNoCase = Name.ToLower();

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The typical prefix used for reserved variable names.
            //
            public static readonly string Prefix = NameNoCase + "_";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Library Variables
        [ObjectId("c6ba3a42-92ae-4ada-ab2b-a237ebaeab43")]
        internal static class Core
        {
            #region For Core Marshaller Use Only
            //
            // NOTE: Used by the CommandCallback class for temporary storage
            //       of ByRef parameter values.
            //
            public static readonly string Prefix = Package.Prefix + "vars_";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used by [object] to represent a null object.  Do NOT
            //       use it for any other purpose.
            //
            public static readonly string Null = _String.Null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For (Local & Remote) Debugger Use Only
            //
            // NOTE: Used by the debugger subsystem (DebuggerOps, etc).  Do
            //       NOT use it for any other purpose.
            //
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
            public static readonly string Paths = Package.Prefix + "paths";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Test Suite Use Only
            //
            // NOTE: Used by the unit testing functionality.  Do NOT use it
            //       for any other purpose.
            //
            public static readonly string Tests = Package.Prefix +
                "tests";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is a transient (temporary) variable for use during
            //       test file evaluation only.  Do NOT use it for any other
            //       purpose.
            //
            public static readonly string TestFile = "test_file";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the fully qualified path and file name for the
            //       test suite log.
            //
            public static readonly string TestLog = "test_log";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is an array variable used by the test suite and its
            //       related procedures to prevent various default actions
            //       from being taken (e.g. constraint checks, warnings, etc).
            //
            public static readonly string No = "no";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Support Use Only
            //
            // NOTE: Reserved for use by the interactive shell (this variable
            //       may -OR- may not actually be defined).  Do NOT use it
            //       for any other purpose.
            //
            public static readonly string Shell = Package.Prefix + "shell";
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Version Support
        [ObjectId("dce6d9bd-6fdf-4f39-b9ce-ff4b1dd557a4")]
        internal static class Version
        {
            //
            // NOTE: Used to show the release as "trusted".  It should only be
            //       used if the primary assembly file has been signed with an
            //       Authenticode (X.509) certificate and the certificate is
            //       trusted on this machine.
            //
            public static readonly string TrustedValue = "trusted";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to mark the release as "genuine"...  :P~
            //
            public static readonly string GenuineValue = "genuine";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to mark builds as official or unofficial releases.
            //
            public static readonly string OfficialValue =
                RuntimeOps.IsOfficial() ? "official" : "unofficial";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to mark builds as stable or unsable releases.
            //
            public static readonly string StableValue =
                RuntimeOps.IsStable() ? "stable" : "unstable";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "Safe" Interpreter Use Only
        [ObjectId("96675184-161a-409a-8803-a341970c1e04")]
        internal static class Safe
        {
            //
            // NOTE: Used by the "safe" interpreter path scrubber.  Do NOT
            //       use it for any other purpose.
            //
            public static readonly string BaseDirectory = "{BaseDirectory}";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Interactive Shell Support Use Only
#if SHELL
        [ObjectId("b76c9944-ea28-4c22-a367-d4d6be17654b")]
        internal static class Description
        {
            //
            // NOTE: Used for the "about" banner.
            //
            public static readonly string Package =
                "A Tcl {0} compatible interpreter for the Common Language Runtime.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Also used for the "about" banner.
            //
            public static readonly string Official =
                "Core: This is an official build.";

            public static readonly string Unofficial =
                "Core: This is an unofficial build.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.  Phrases are hard-coded
            //       because the wording is a bit different in English when
            //       using the words "trusted" and "untrusted".
            //
            public static readonly string Trusted =
                "Core: This is a trusted build.";

            public static readonly string Untrusted =
                "Core: This is an untrusted build.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.  Phrases are hard-coded
            //       because the wording is a bit different in English when
            //       using the words "stable" and "unstable".
            //
            public static readonly string Stable =
                "Core: This is a stable build.";

            public static readonly string Unstable =
                "Core: This is an unstable build.";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.
            //
            public static readonly string Safe =
                "Core: Interpreter {0} thinks it is \"{1}\".";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used for the "about" banner.
            //
            public static readonly string Security =
                "Core: Interpreter {0} thinks script security is {1}.";

            ///////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
            //
            // NOTE: Used for the "about" banner.
            //
            public static readonly string Isolated =
                "Core: Interpreter {0} thinks plugin isolation is {1}.";
#endif
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "eagle_debugger" Array Use Only
        [ObjectId("29cb50d9-3570-44d8-b2d2-8364cce53176")]
        internal static class Debugger
        {
            //
            // NOTE: The name of the "special" script currently being
            //       evaluated by the interpreter.  These scripts are
            //       generally evaluated during interpreter creation.
            //
            public static readonly string ScriptName = "scriptName";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "eagle_platform" Array Use Only
        //
        // NOTE: These names are referred to directly from scripts and where
        //       applicable are named identically to their Tcl counterparts,
        //       please do not change.
        //
        [ObjectId("f8ff1ea7-dfce-4b34-8bd0-6bb395759605")]
        internal static class Platform
        {
            //
            // NOTE: The name of the script array that contains the
            //       platform specific information.
            //
            public static readonly string Name = Package.Prefix + "platform";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Script engine version information.
            //
            public static readonly string Administrator = "administrator";
            public static readonly string ApplicationAddressRange = "applicationAddressRange";
            public static readonly string Certificate = "certificate";
            public static readonly string UpdateBaseUri = "updateBaseUri";
            public static readonly string UpdatePathAndQueryName = "updatePathAndQuery";
            public static readonly string DownloadBaseUri = "downloadBaseUri";
            public static readonly string ScriptBaseUri = "scriptBaseUri";
            public static readonly string AuxiliaryBaseUri = "auxiliaryBaseUri";
            public static readonly string CompileOptions = "compileOptions";
            public static readonly string CSharpOptionsName = "csharpOptions";
            public static readonly string StrongName = "strongName";
            public static readonly string StrongNameTag = "strongNameTag";
            public static readonly string Hash = "hash";
            public static readonly string Epoch = "epoch";
            public static readonly string InterpreterTimeStamp = "interpreterTimeStamp";
            public static readonly string Vendor = "vendor";
            public static readonly string Suffix = "suffix";
            public static readonly string TextOrSuffix = "textOrSuffix";
            public static readonly string GlobalAssemblyCache = "globalAssemblyCache";
            public static readonly string MinimumDate = "minimumDate";
            public static readonly string MaximumDate = "maximumDate";
            public static readonly string Culture = "culture";
            public static readonly string FrameworkVersion = "frameworkVersion";
            public static readonly string FrameworkExtraVersion = "frameworkExtraVersion";
            public static readonly string ObjectIds = "objectIds";
            public static readonly string Wow64 = "wow64";

#if CAS_POLICY
            public static readonly string PermissionSet = "permissionSet";
#endif

            public static readonly string ProcessorAffinityMasks = "processorAffinityMasks";
            public static readonly string RuntimeName = "runtime";
            public static readonly string ImageRuntimeVersion = "imageRuntimeVersion";
            public static readonly string TargetFramework = "targetFramework";
            public static readonly string RuntimeVersion = "runtimeVersion";
            public static readonly string RuntimeBuild = "runtimeBuild";
            public static readonly string RuntimeExtraVersion = "runtimeExtraVersion";
            public static readonly string RuntimeOptions = "runtimeOptions";
            public static readonly string Configuration = "configuration";
            public static readonly string TimeStamp = "timeStamp";
            public static readonly string PatchLevel = "patchLevel";
            public static readonly string Release = "release";
            public static readonly string SourceId = "sourceId";
            public static readonly string SourceTimeStamp = "sourceTimeStamp";
            public static readonly string Tag = "tag";
            public static readonly string Text = "text";
            public static readonly string Uri = "uri";
            public static readonly string PublicKey = "publicKey";
            public static readonly string PublicKeyToken = "publicKeyToken";
            public static readonly string ModuleVersionId = "moduleVersionId";
            public static readonly string Version = "version";
            public static readonly string ShellPatchLevel = "shellPatchLevel";
            public static readonly string ShellVersion = "shellVersion";
            public static readonly string NativeUtility = "nativeUtility";
            public static readonly string Timeout = "timeout";

            ///////////////////////////////////////////////////////////////////

#if NETWORK
            public static readonly string NetworkTimeout = "networkTimeout";
#endif

            ///////////////////////////////////////////////////////////////////

            public static readonly string CSharpOptionsValue = null; /* TODO: Good default? */

            ///////////////////////////////////////////////////////////////////

            public static readonly string UpdatePathAndQueryFormat =
                ".txt{1}?v={0}";

            public static readonly string UpdateStablePathAndQueryFormat =
                "stable" + UpdatePathAndQueryFormat;

            public static readonly string UpdateUnstablePathAndQueryFormat =
                "latest" + UpdatePathAndQueryFormat;

            ///////////////////////////////////////////////////////////////////

            public static readonly string UpdatePathAndQueryValue =
                RuntimeOps.GetUpdatePathAndQuery(null, RuntimeOps.IsStable(), null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [parse options] Sub-Command Use Only
        [ObjectId("3facc96f-c6ae-495e-813d-3b906382377e")]
        internal static class OptionSet
        {
            public static readonly string Value = "value";
            public static readonly string Options = "options";
            public static readonly string NextIndex = "nextIndex";
            public static readonly string EndIndex = "endIndex";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [sql execute] Sub-Command Use Only
        [ObjectId("5bcb3c76-9061-40fc-b728-9cd2b1c052a2")]
        internal static class ResultSet
        {
            public static readonly string Names = "names";
            public static readonly string Count = "count";
            public static readonly string Rows = "rows";

            public static readonly string Prepare = "prepare";
            public static readonly string Execute = "execute";
            public static readonly string Time = "time";
        }
        #endregion
    }
}
