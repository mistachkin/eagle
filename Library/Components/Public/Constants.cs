/*
 * Constants.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if ARM || ARM64 || X86 || IA64 || X64
#define HAVE_SIZEOF
#endif

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Constants
{
    [ObjectId("0c564d1c-a98a-4ed7-aabe-1685abae114f")]
    public static class Limits
    {
        public static readonly int Unlimited = 0;
        public static readonly int Forbidden = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("919f62ff-f2a9-4b24-ac1f-accb23b94324")]
    public static class HashCode
    {
        public static readonly int Invalid = -1;
        public static readonly int None = 0;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("b8693a1a-f084-4aa7-921a-88777090686a")]
    public static class _Object
    {
        public static readonly string Null = _String.Null;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("548b2832-5bd1-4e7e-893c-c2e0f88d2cfc")]
    public static class Milliseconds
    {
        public static readonly double Never = -1.0;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("08289e0a-277d-453c-8209-df7e95640e7b")]
    public static class Identifier
    {
        public static readonly int TypeMismatch = -2;
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("5f7121e8-6c6b-4c72-8d67-d2e8d57eabce")]
    public static class _String
    {
        public static readonly string Null = "null";
        public static readonly string Proxy = "proxy";
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("0a1a7a70-18fb-4164-b5d9-aebb833e1d75")]
    public static class _Version
    {
        public static readonly int Minimum = 0;
    }

    ///////////////////////////////////////////////////////////////////////////

#if XML
    [ObjectId("19989260-5883-4823-98ee-8a73533b6709")]
    public static class _XmlAttribute
    {
        public static readonly string Id = "id";
        public static readonly string Type = "type";
        public static readonly string Text = "text";
        public static readonly string Name = "name";
        public static readonly string Group = "group";
        public static readonly string Description = "description";
        public static readonly string TimeStamp = "timeStamp";
        public static readonly string PublicKeyToken = "publicKeyToken";
        public static readonly string Signature = "signature";

        ///////////////////////////////////////////////////////////////////////

        internal static readonly StringList EngineList = new StringList(
            new string[] { Type, Text });

        ///////////////////////////////////////////////////////////////////////

        internal static readonly StringList RequiredList = new StringList(
            new string[] { Id, Type, Text });

        ///////////////////////////////////////////////////////////////////////

        internal static readonly StringList AllList = new StringList(
            new string[] {
            Id, Type, Text, Name, Group, Description, TimeStamp,
            PublicKeyToken, Signature
        });
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("da7b3fc1-24f5-4a2a-8f9e-deda241ddb15")]
    public static class Xml
    {
        //
        // NOTE: The name of the resource stream that contains our XML schema
        //       data.
        //
        public static readonly string SchemaResourceName =
            GlobalState.GetPackageName(PackageType.Default, null, ".xsd",
                false);

        //
        // NOTE: The name of our XML namespace.
        //
        public static readonly string NamespaceName =
            GlobalState.GetPackageNameNoCase();

        //
        // NOTE: The URI of our XML namespace.
        //
        public static readonly Uri ScriptNamespaceUri =
            GlobalState.GetAssemblyNamespaceUri();

        //
        // HACK: Hard-code the "well-known" security package (Harpy)
        //       script signature schema URI.
        //
        public static readonly Uri SignatureNamespaceUri =
            new Uri("https://eagle.to/2011/harpy");

        //
        // NOTE: The candidate XPath queries used to extract (script)
        //       blocks from an XML document.  The first query that
        //       returns some nodes wins.
        //
        public static readonly StringList XPathList = new StringList(
            new string[] {
            //
            // NOTE: First, check for the necessary elements using the
            //       name of our namespace.
            //
            (NamespaceName != null) ?
                "//" + NamespaceName + ":blocks/" + NamespaceName +
                ":block" : null,

            //
            // NOTE: Second, check for the necessary elements using the
            //       default namespace.
            //
            "//blocks/block",

            //
            // NOTE: These list elements are reserved for future use by
            //       the core library.  Please do not change them.
            //
            null,
            null,
            null,
            null,

            //
            // NOTE: These list elements are reserved for future use by
            //       third-party code.
            //
            null,
            null,
            null,
            null
        });
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("51f47a43-205f-4241-931a-e072256719be")]
    public static class Index
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("ab498ee2-f654-404e-b2df-4af7c5482bb1")]
    public static class Level
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("17cc4b98-cdf3-4a73-ada6-3c8dcf34d9ad")]
    public static class Count
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("c9f34b0c-d4a5-4d63-832c-ff9515924735")]
    public static class Size
    {
        public static readonly long Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("8630da9a-e4f9-40f3-ab61-10201f8007ca")]
    public static class Percent
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("710e4bb2-28a1-439a-ba12-9e91f3d93ff4")]
    public static class _ConsoleColor
    {
        public static readonly ConsoleColor None =
            (ConsoleColor)HostColor.None;

        public static readonly ConsoleColor Invalid =
            (ConsoleColor)HostColor.Invalid;

        public static readonly ConsoleColor Default =
            (ConsoleColor)HostColor.None;
    }

    ///////////////////////////////////////////////////////////////////////////

#if NATIVE
    [ObjectId("03fcde0a-0800-4f49-8462-dddbd66bf90f")]
    public static class DllName
    {
#if WINDOWS
#if !MONO
        //
        // NOTE: This is the file name for the "Microsoft COM Object Runtime
        //       Execution Engine" and it should be available on the "real"
        //       .NET Framework (all versions).  This DLL is not available on
        //       Mono.
        //
        public const string MsCorEe = "mscoree.dll";
#endif

        public const string MsVcRt = "msvcrt.dll";
        public const string AdvApi32 = "advapi32.dll";
        public const string AdvPack = "advpack.dll";
        public const string Crypt32 = "crypt32.dll";
        public const string Kernel32 = "kernel32.dll";
        public const string NtDll = "ntdll.dll";
        public const string Shell32 = "shell32.dll";
        public const string User32 = "user32.dll";
        public const string WinTrust = "wintrust.dll";
        public const string Ws2_32 = "ws2_32.dll";
        public const string WtsApi32 = "wtsapi32.dll";
#endif

#if NATIVE_UTILITY
        public const string Utility = "spilornis.dll";
#endif

#if UNIX
        public const string LibC = "libc";
        public const string LibDL = "libdl";

#if !NET_STANDARD_20
        public const string Internal = "__Internal";
#else
        public const string Internal = LibC;
#endif
#endif
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("4adf4145-56a6-4540-987c-2447543eba40")]
    public static class WaitResult
    {
        public static readonly int Object0 = 0x0;
        public static readonly int Abandoned0 = 0x80;
        public static readonly int IoCompletion = 0xC0;
        public static readonly int Timeout = WaitHandle.WaitTimeout;
        public static readonly int Failed = unchecked((int)0xFFFFFFFF);

        ///////////////////////////////////////////////////////////////////////

#if MONO || MONO_HACKS
        //
        // HACK: *MONO* This value was stolen from:
        //
        //       "/mono/mcs/class/referencesource/ -->
        //       --> mscorlib/system/threading/waithandle.cs"
        //
        public static readonly int MonoFailed = 0x7FFFFFFF;
#endif
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("68a277d1-fb1b-4bbb-8d45-140e80ef0e3f")]
    public static class _Timeout
    {
        public static readonly int Infinite = Timeout.Infinite;
        public static readonly int None = 0;
        public static readonly int Minimum = 1;
        // public static readonly int Maximum = 300000;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("2d9df6da-630f-463f-84f1-bc8ff28f018c")]
    public static class Beep
    {
        public static readonly int Invalid = -1;

        //
        // NOTE: These are the "default" beep values, per MSDN.
        //
        public static readonly int Frequency = 800;
        public static readonly int Duration = 200;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("0407ec6f-852f-4d02-add6-36fdf51fedad")]
    public static class Width
    {
        public static readonly int Invalid = -1;
        public static readonly int Default = 80;
        public static readonly int Minimum = 40;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("cffe4c6d-2308-430e-9ceb-b1980fb5ee9c")]
    public static class Height
    {
        public static readonly int Invalid = -1;
        public static readonly int Default = 25;
        public static readonly int Minimum = 10;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("efc3c71a-c818-48ed-876e-831a25418a54")]
    public static class Margin
    {
        public static readonly int Invalid = -1;
        public static readonly int Default = 4;
        public static readonly int Minimum = 2;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("27c8ffcc-9656-4a55-912a-e68f5fb5bcfc")]
    public static class Length
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("54455e76-14fc-4fb1-bfec-6273b550fefa")]
    public static class _Position
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("cab82601-534e-4c1b-8555-c936e8dc6f73")]
    public static class _Size
    {
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("ff56c130-734d-4a3d-9dc1-f38d3c02a144")]
    public static class Port
    {
        public static readonly int Invalid = -1;
        public static readonly int Automatic = 0; // for clients.
        public static readonly int NetworkTime = 123; /* NTP */
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("5cd54e77-bd26-402f-8556-17a9820cd1e1")]
    public static class _Path
    {
        public static readonly string Current = ".";
        public static readonly string Parent = "..";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string Library = "Library";
        public static readonly string Tests = "Tests";
        public static readonly string Data = "data";
        public static readonly string Tcl = "tcl";
        public static readonly string Plugins = "Plugins";
        public static readonly string BuildTasks = "BuildTasks";
        public static readonly string Externals = "Externals";
        public static readonly string NetPrefix = "net"; // e.g. netstandard2.0
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("c9f38457-538c-4dfc-8e16-a1b8f3d6c03a")]
    public static class FileName
    {
        public static readonly string Initialization =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Initialization,
                PackageType.Library, false, false);

        public static readonly string Embedding =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Embedding,
                PackageType.Library, false, false);

        public static readonly string Vendor =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Vendor,
                PackageType.Library, false, false);

        public static readonly string Startup =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Startup,
                PackageType.Library, false, false);

        public static readonly string Worker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Worker,
                PackageType.Library, false, false);

        public static readonly string Safe =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Safe,
                PackageType.Library, false, false);

        public static readonly string Shell =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Shell,
                PackageType.Library, false, false);

        public static readonly string ShellWorker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.ShellWorker,
                PackageType.Library, false, false);

        public static readonly string Test =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Test,
                PackageType.Library, false, false);

        public static readonly string LibraryPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Library, false, false);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string All =
            FormatOps.ScriptTypeToFileName(ScriptTypes.All,
                PackageType.Test, false, false);

        public static readonly string Constraints =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Constraints,
                PackageType.Test, false, false);

        public static readonly string Epilogue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Epilogue,
                PackageType.Test, false, false);

        public static readonly string Prologue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Prologue,
                PackageType.Test, false, false);

        public static readonly string TestPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Test, false, false);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string KitPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Kit, false, false);
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("1f6dc165-629e-4cbe-aec2-b26239f244e4")]
    public static class FileNameOnly
    {
        public static readonly string PackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.None, true, false);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string Initialization =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Initialization,
                PackageType.Library, true, false);

        public static readonly string Embedding =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Embedding,
                PackageType.Library, true, false);

        public static readonly string Vendor =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Vendor,
                PackageType.Library, true, false);

        public static readonly string Startup =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Startup,
                PackageType.Library, true, false);

        public static readonly string Worker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Worker,
                PackageType.Library, true, false);

        public static readonly string Safe =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Safe,
                PackageType.Library, true, false);

        public static readonly string Shell =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Shell,
                PackageType.Library, true, false);

        public static readonly string ShellWorker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.ShellWorker,
                PackageType.Library, true, false);

        public static readonly string Test =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Test,
                PackageType.Library, true, false);

        public static readonly string LibraryPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Library, true, false);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string All =
            FormatOps.ScriptTypeToFileName(ScriptTypes.All,
                PackageType.Test, true, false);

        public static readonly string Constraints =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Constraints,
                PackageType.Test, true, false);

        public static readonly string Epilogue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Epilogue,
                PackageType.Test, true, false);

        public static readonly string Prologue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Prologue,
                PackageType.Test, true, false);

        public static readonly string TestPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Test, true, false);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string KitPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Kit, true, false);

        ///////////////////////////////////////////////////////////////////////

        internal static readonly string WellKnownAssemblyFilePluginNames =
            "WellKnownAssemblyFilePluginNames.tsv";
    }

    ///////////////////////////////////////////////////////////////////////////

#if SHELL
    [ObjectId("c5ca371c-bd5a-4215-8f41-fcd812da0a00")]
    public static class _Assembly
    {
        public static readonly string Shell = GlobalState.GetPackageName(
            PackageType.Default, null, "Shell", false);

        public static readonly string Kit = GlobalState.GetPackageName(
            PackageType.Default, null, "Kit", false);
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("990c369f-41ee-4f5a-b71e-900d48676b10")]
    public static class ContentType
    {
        public static readonly string Text = "text/plain";

        public static readonly string[] Scripts = {
            "text/x-" + GlobalState.GetPackageNameNoCase(),
            "application/x-" + GlobalState.GetPackageNameNoCase(),
            "text/x-script." + GlobalState.GetPackageNameNoCase()
        };

        public static readonly string[] SafeScripts = {
            "text/x-safe-" + GlobalState.GetPackageNameNoCase(),
            "application/x-safe-" + GlobalState.GetPackageNameNoCase(),
            "text/x-safe-script." + GlobalState.GetPackageNameNoCase()
        };

        public static readonly string Script = Scripts[0];
        public static readonly string SafeScript = SafeScripts[0];
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("8f0c1c60-0e47-4f19-a3d7-8d6f56348946")]
    public static class FileExtension
    {
        public static readonly string Script = GlobalState.GetPackageName(
            PackageType.Default, Characters.Period.ToString(), null, true);

        public static readonly string EncryptedScript = GlobalState.GetPackageName(
            PackageType.Default, String.Format("{0}{1}", Characters.Period,
            Characters.e), null, true);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This list of "well-known" file extensions is used just below
        //       by this class.  Any changes made here will need to be double
        //       checked there.
        //
        private static readonly string[] WellKnown = {
            ".args",         /* 00: Arguments */
            ".bat",          /* 01: Batch */
            ".com",          /* 02: Command */
            ".config",       /* 03: Configuration */
            ".dll",          /* 04: Library */
            ".exe",          /* 05: Executable */
            ".harpy",        /* 06: Signature */
            ".ico",          /* 07: Icon */
            ".ini",          /* 08: Profile */
            ".pdb",          /* 09: Symbols */
            ".pvk",          /* 10: PrivateKey (RSA) */
            ".snk",          /* 11: StrongNameKey (RSA) */
            ".txt",          /* 12: Text */
            ".xml",          /* 13: Markup */
            ".so",           /* 14: SharedObject */
            ".dylib",        /* 15: DynamicLibrary */
            ".sl",           /* 16: SharedLibrary */
            ".db",           /* 17: Database */
            ".tmp",          /* 18: Temporary */
            ".log",          /* 19: Log */
            ".dsasnk",       /* 20: DsaStrongNameKey */
            ".dsapvk",       /* 21: DsaPrivateKey */
            ".b64sig",       /* 22: Base64Signature */
            ".exml",         /* 23: EncryptedMarkup */
            Script,          /* 24: Script */
            EncryptedScript, /* 25: EncryptedScript */
            ".bak",          /* 26: Backup */
            ".settings",     /* 27: Settings */
            ".noPkgIndex",   /* 28: NoPkgIndex */
            ".ruleSet",      /* 29: RuleSet */
            ".*"             /* 30: Any */
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is really a dictionary with a name suffix of "List".
        //       The rationale behind that is that it is logically a list
        //       of "well-known" file extensions that is contained inside
        //       of a physical dictionary for the sole purpose of making
        //       lookups faster.
        //
        internal static readonly PathDictionary<object> WellKnownList =
            new PathDictionary<object>(WellKnown);

        ///////////////////////////////////////////////////////////////////////

        public static readonly string Arguments = WellKnown[0];
        public static readonly string Batch = WellKnown[1];
        public static readonly string Command = WellKnown[2];
        public static readonly string Configuration = WellKnown[3];
        public static readonly string Library = WellKnown[4];
        public static readonly string Executable = WellKnown[5];
        public static readonly string Signature = WellKnown[6];
        public static readonly string Icon = WellKnown[7];
        public static readonly string Profile = WellKnown[8];
        public static readonly string Symbols = WellKnown[9];
        public static readonly string PrivateKey = WellKnown[10];
        public static readonly string StrongNameKey = WellKnown[11];
        public static readonly string Text = WellKnown[12];
        public static readonly string Markup = WellKnown[13];
        public static readonly string SharedObject = WellKnown[14];
        public static readonly string DynamicLibrary = WellKnown[15];
        public static readonly string SharedLibrary = WellKnown[16];
        public static readonly string Database = WellKnown[17];
        public static readonly string Temporary = WellKnown[18];
        public static readonly string Log = WellKnown[19];
        public static readonly string DsaStrongNameKey = WellKnown[20];
        public static readonly string DsaPrivateKey = WellKnown[21];
        public static readonly string Base64Signature = WellKnown[22];
        public static readonly string EncryptedMarkup = WellKnown[23];
        public static readonly string Backup = WellKnown[26];
        public static readonly string Settings = WellKnown[27];
        public static readonly string NoPkgIndex = WellKnown[28];
        public static readonly string RuleSet = WellKnown[29];
        public static readonly string Any = WellKnown[30];
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("a263fc47-65e8-4b95-979b-b47b53629df9")]
    public static class ColorName
    {
        public static readonly string Banner = "Banner";
        public static readonly string Default = "Default";
        public static readonly string Help = "Help";
        public static readonly string HelpItem = "HelpItem";
        public static readonly string Legal = "Legal";
        public static readonly string Official = "Official";
        public static readonly string Unofficial = "Unofficial";
        public static readonly string Trusted = "Trusted";
        public static readonly string Untrusted = "Untrusted";
        public static readonly string Stable = "Stable";
        public static readonly string Unstable = "Unstable";
        public static readonly string Enabled = "Enabled";
        public static readonly string Disabled = "Disabled";
        public static readonly string Undefined = "Undefined";
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("3ea126e0-52f7-4b34-8e80-d428da5c9d8b")]
    internal static class StandardChannel
    {
        public static readonly string Input = Channel.StdIn;
        public static readonly string Output = Channel.StdOut;
        public static readonly string Error = Channel.StdErr;
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("8e21d7ed-fd63-4ea5-a698-740601e2d188")]
    internal static class BuildConfiguration
    {
        public const string Debug = "Debug";
        public const string Release = "Release";
    }

    ///////////////////////////////////////////////////////////////////////////

#if SHELL
    [ObjectId("99951710-f239-43e3-9cea-92fdc4d1d18b")]
    internal static class CommandLineArgument
    {
        public static readonly string StandardInput = "-";
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("656078c8-4735-42ca-a6fe-55eb72502a17")]
    internal static class CommandLineOption
    {
        public static readonly string AnyFile = "anyFile";

#if !ENTERPRISE_LOCKDOWN
        public static readonly string AnyInitialize = "anyInitialize";
#endif

        public static readonly string Arguments = "arguments";
        public static readonly string Break = "break";
        public static readonly string Debug = "debug";
        public static readonly string Encoding = "encoding";

#if !ENTERPRISE_LOCKDOWN
        public static readonly string Evaluate = "evaluate";
        public static readonly string EvaluateEncoded = "evaluateEncoded";
#endif

        public static readonly string Quiet = "quiet";
        public static readonly string File = "file";
        public static readonly string Help = "help";
        public static readonly string About = "?";
        public static readonly string CommandHelp = "??";
        public static readonly string EnvironmentHelp = "???";
        public static readonly string ForceInitialize = "forceInitialize";
        public static readonly string FullHelp = "????";
        public static readonly string Initialize = "initialize";
        public static readonly string Interactive = "interactive";

#if ISOLATED_PLUGINS
        public static readonly string Isolated = "isolated";
#endif

        public static readonly string Kiosk = "kiosk";
        public static readonly string LockHostArguments = "lockHostArguments";
        public static readonly string Namespaces = "namespaces";
        public static readonly string NoArgumentsFileNames = "noArgumentsFileNames";
        public static readonly string NoAppSettings = "noAppSettings";
        public static readonly string NoExit = "noExit";
        public static readonly string NoTrim = "noTrim";
        public static readonly string ClearTrace = "clearTrace";
        public static readonly string Pause = "pause";
        public static readonly string PluginArguments = "pluginArguments";
        public static readonly string PostFile = "postFile";

#if !ENTERPRISE_LOCKDOWN
        public static readonly string PostInitialize = "postInitialize";
#endif

        public static readonly string PreFile = "preFile";

#if !ENTERPRISE_LOCKDOWN
        public static readonly string PreInitialize = "preInitialize";
#endif

        public static readonly string Profile = "profile";
        public static readonly string Reconfigure = "reconfigure";
        public static readonly string Recreate = "recreate";
        public static readonly string RuntimeOption = "runtimeOption";
        public static readonly string Safe = "safe";

#if TEST
        public static readonly string ScriptTrace = "scriptTrace";
#endif

        public static readonly string Security = "security";
        public static readonly string SetLoop = "setLoop";
        public static readonly string SetInitialize = "setInitialize";
        public static readonly string Standard = "standard";
        public static readonly string StartupLibrary = "startupLibrary";

#if TEST
        public static readonly string StartupLogFile = "startupLogFile";
#endif

#if !ENTERPRISE_LOCKDOWN
        public static readonly string StartupPreInitialize = "startupPreInitialize";
#endif

        public static readonly string Step = "step";
        public static readonly string StopOnUnknown = "stopOnUnknown";
        public static readonly string Test = "test";
        public static readonly string PluginTest = "pluginTest";
        public static readonly string TestDirectory = "testDirectory";
        public static readonly string SetupTrace = "setupTrace";
        public static readonly string TraceToHost = "traceToHost";
        public static readonly string VendorPath = "vendorPath";
        public static readonly string Version = "version";

        public static readonly string SetCreate = "setCreate";
        public static readonly string Child = "child";
        public static readonly string Parent = "parent";
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("6475066f-5f77-49c0-bcc0-177abe8cf24a")]
    public static class Prompt
    {
        public static readonly string Namespaces =
            "Namespace support {0}.";

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        public static readonly string BumpCacheLevel =
            "Cache level overridden via configuration: {0}.";

        public static readonly string CacheFlags =
            "Cache flags overridden via configuration: {0}.";
#endif

        public static readonly string UserInteractive =
            "User interactivity overridden via environment: {0}.";

#if ISOLATED_PLUGINS
        public static readonly string Isolated = "Plugin isolation {0}{1}.";
#endif

        public static readonly string ResultStack =
            "Result objects will include managed call stacks.";

        public static readonly string Security =
            "Script signing policies and core script certificates {0}{1}.";

        public static readonly string LockHostArguments =
            "Locked to host arguments.";

        public static readonly string NoAppSettings =
            "Arguments from application settings processing skipped{0}.";

#if CONFIGURATION
        public static readonly string RefreshAppSettings =
            "Application settings will be forcibly refreshed.";
#endif

#if XML
        public static readonly string UseXmlFiles =
            "Application settings from XML files will be favored.";

        public static readonly string MergeXmlAppSettings =
            "Application settings from XML files will be merged.";

        public static readonly string MergeAllAppSettings =
            "Application settings from all sources will be merged.";
#endif

        public static readonly string NoArgumentsFileNames =
            "Arguments from file(s) processing skipped.";

        public static readonly string NoExit =
            "Interactive loop will be entered.";

        public static readonly string NoTrim =
            "Whitespace will not be trimmed from subsequent arguments.";

        public static readonly string NoBreakNotify =
            "Breaking into debugger in process {0} is disabled.";

        public static readonly string NoBreak =
            "Breaking into debugger in process {0} is disabled, press any key to continue.";

        public static readonly string Debugger =
            "Attach a debugger to process {0} and press any key to continue.";

        public static readonly string Interactive =
            "Interactive mode enabled{0}.";

        public static readonly string PluginArguments =
            "Arguments added for plugin: {0}.";

#if TEST
        public static readonly string LogFile =
            "Log file was setup.";
#endif

        public static readonly string LibraryPath =
            "Script library path was overridden.";

        public static readonly string PreInitializeText =
            "Pre-initialize script text was overridden.";

        public static readonly string SingleStep =
            "Script debugger single-step enabled{0}.";

#if !DEBUGGER
        public static readonly string NoDebugger =
            "Script debugger not available{0}.";
#endif

        public static readonly string RuntimeOption = "Runtime option changed: {0}.";

        public static readonly string Recreate = "{0} interpreter {1} was recreated.";

        public static readonly string Reconfigure = "{0} interpreter {1} was reconfigured: {2}.";

        public static readonly string CreateFlags =
            "Interpreter creation flags overridden via configuration: {0}.";

        public static readonly string HostCreateFlags =
            "Interpreter host creation flags overridden via configuration: {0}.";

        public static readonly string InterpreterFlags =
            "Interpreter instance flags overridden via configuration: {0}.";

        public static readonly string InitializeFlags =
            "Interpreter initialize flags overridden via configuration: {0}.";

        public static readonly string ScriptFlags =
            "Interpreter script flags overridden via configuration: {0}.";

        public static readonly string Console =
            "Console enabled.";

        public static readonly string NoConsole =
            "Console disabled.";

        public static readonly string Debug =
            "Debug mode enabled{0}.";

        public static readonly string MeasureTime =
            "Time measurements enabled.";

        // public static readonly string Verbose =
        //     "Verbose mode enabled.";

        public static readonly string NoMutexes =
            "Creating and/or opening \"setup\" mutexes disabled.";

        public static readonly string NoTrace =
            "Tracing disabled.";

        public static readonly string NoTraceLimits =
            "Tracing limits disabled.";

        public static readonly string NoTraceOps =
            "Tracing subsystem disabled.";

        public static readonly string NoDebugTrace =
            "Debug tracing disabled.";

        public static readonly string Trace =
            "Tracing enabled for {0}.";

        public static readonly string TraceOps =
            "Tracing subsystem enabled.";

        public static readonly string NoTrusted =
            "Executable file certificate trust checking disabled.";

        public static readonly string NoVerified =
            "Assembly strong name signature verification disabled.";

        public static readonly string Quiet =
            "Quiet mode {0}{1}.";

#if NATIVE && WINDOWS
        public static readonly string NoNativeConsole =
            "Native console integration disabled.";
#endif

#if NATIVE
        public static readonly string NoNativeStack =
            "Native stack checking will be disabled.";
#endif

        public static readonly string CreateFailSafe =
            "Token interpreter creation cannot be temporarily disabled.";

        public static readonly string ForceTrustedHashes =
            "Trusted hashes will not be ignored.";

        public static readonly string NoTrustedHashes =
            "Trusted hashes disabled.";

        public static readonly string StrictBasePath =
            "Strict base path checking will be used.";

        public static readonly string UseNamedEvents =
            "Internal wrapper class will be used for named events.";

        public static readonly string NoPopulateOsExtra =
            "Extra operating system information will not be populated.";

        public static readonly string DefaultQuiet =
            "Default quiet mode enabled.";

        public static readonly string DefaultTraceStack =
            "Default tracing of managed call stack enabled.";

        public static readonly string NoVerbose =
            "Selected diagnostic messages are disabled.";

#if THREADING
        public static readonly string NoWorkers =
            "Worker threads disabled.";
#endif

        public static readonly string Verbose =
            "Selected diagnostic messages are enabled.";

        public static readonly string UtilityPath =
            "Utility path overridden via configuration: {0}.";

        public static readonly string VendorPath =
            "Vendor path overridden via configuration: {0}.";

        public static readonly string EllipsisLimit =
            "Ellipsis limit overridden via configuration: {0}.";

        public static readonly string Throw =
            "Re-throwing of unhandled exceptions enabled.";

        public static readonly string NeverGC =
            "Internal calls to collect garbage always disabled.";

        public static readonly string NeverWaitForGC =
            "Internal waits for pending finalizers always disabled.";

        public static readonly string AlwaysWaitForGC =
            "Internal waits for pending finalizers always enabled.";

        public static readonly string TreatAsDotNetCore =
            "Will attempt to treat runtime as though it were .NET Core.";

        public static readonly string TreatAsMono =
            "Will attempt to treat runtime as though it were Mono.";

        public static readonly string TreatAsFramework20 =
            "Will attempt to treat runtime as though it were .NET Framework 2.0.";

        public static readonly string TreatAsFramework40 =
            "Will attempt to treat runtime as though it were .NET Framework 4.0.";

        public static readonly string DefaultTraceCategories =
            "Default {0} trace categories overridden via environment: {1}.";

        public static readonly string DefaultTraceCategoriesError =
            "Default {0} trace categories could not be overridden via environment: {1}.";

        public static readonly string DefaultTraceFormat =
            "Default trace format overridden via environment: {0}.";

        public static readonly string DefaultTraceFormatError =
            "Default trace format could not be overridden via environment: {0}.";

        public static readonly string DefaultTracePriorities =
            "Default trace priority mask overridden via environment: {0}.";

        public static readonly string DefaultGlobalPriorities =
            "Default trace priority flags overridden via environment: {0}.";

        public static readonly string DefaultTracePrioritiesError =
            "Default trace priority mask could not be overridden via environment: {0}.";

        public static readonly string DefaultGlobalPrioritiesError =
            "Default trace priority flags could not be overridden via environment: {0}.";

        public static readonly string DefaultTracePriority =
            "Default trace priority overridden via environment: {0}.";

        public static readonly string DefaultTracePriorityError =
            "Default trace priority could not be overridden via environment: {0}.";

        public static readonly string TraceCategories =
            "{0} trace categories overridden via configuration: {1}.";

        public static readonly string TracePriorities =
            "Trace priority mask overridden via configuration: {0}.";

        public static readonly string TracePriority =
            "Trace priority overridden via configuration: {0}.";

        public static readonly string TraceToHost =
            "Tracing to host enabled{0}.";

        public static readonly string TraceError =
            "Tracing cannot be enabled: {0}.";

#if TEST
        public static readonly string ScriptTrace =
            "Script trace listener added{0}.";
#endif

        public static readonly string DebugTrace =
            "Debug tracing enabled.";

        public static readonly string DebugTraceError =
            "Debug tracing cannot be enabled: {0}.";

        public static readonly string ForceInitialize =
            "Script library initialization will be forced.";

        public static readonly string Initialize =
            "Script library initialization will be enabled.";

        public static readonly string NoInitialize =
            "Script library initialization will be skipped{0}.";

#if SHELL
        public static readonly string NoInitializeShell =
            "Shell script library initialization will be skipped.";
#endif

        public static readonly string Kiosk =
            "Kiosk lock {0}.";

        public static readonly string DeniedKiosk =
            "Denied disable of kiosk lock after {0} loop(s).";

        public static readonly string DeniedExit =
            "Denied exit from interactive shell{0}.";

        public static readonly string Loop =
            "Interactive loop will be enabled.";

        public static readonly string NoLoop =
            "Interactive loop will be skipped{0}.";

        public static readonly string StopOnUnknown =
            "Argument processing will stop after unknown argument.";

        public static readonly string NoStopOnUnknown =
            "Argument processing will continue after unknown argument{0}.";

        public static readonly string Parent =
            "Parent interpreter will be used.";

        public static readonly string Child =
            "Child interpreter will be used.";

        public static readonly string Create =
            "Interpreter will be recreated.";

        public static readonly string NoCreate =
            "Interpreter will not be recreated.";

        public static readonly string Disposed =
            "Parent interpreter {0} was {1}disposed.";

        public static readonly string DisposedError =
            "Parent interpreter {0} could not be disposed: {1}.";

        public static readonly string NoThrowOnDisposed =
            "Exceptions will not be thrown when disposed objects are accessed.";

        public static readonly string Profile =
            "Host profile set to \"{0}\".";

        public static readonly string UseAttach =
            "Console will be attached or opened.";

        public static readonly string NoColor =
            "Console output will not be in color.";

        public static readonly string NoClose =
            "Console window will not be closable.";

        public static readonly string NoTitle =
            "Console title will not be changed.";

#if NATIVE && NATIVE_UTILITY
        public static readonly string NoNativeUtility =
            "Native utility library will not be loaded.";
#endif

        public static readonly string NoIcon =
            "Console icon will not be changed.";

        public static readonly string NoProfile =
            "Host profile will not be loaded.";

        public static readonly string NoCancel =
            "Host script cancellation interface will not be enabled.";

        public static readonly string Safe =
            "Safe mode enabled{0}.";

        public static readonly string Standard =
            "Standard mode enabled{0}.";

        public static readonly string ViaCommandLine = " via command line";
        public static readonly string ViaEnvironment = " via environment";
        public static readonly string ViaHostEnvironment = " via host environment";

#if !NET_STANDARD_20
        public static readonly string ViaRegistry = " via registry hive";
#endif
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("332b7844-2734-4413-b835-704abe7547c1")]
    public static class PublicKeyToken
    {
        //
        // NOTE: The .NET Standard reference assemblies appear to use
        //       this public key token.
        //
        public static readonly string NetStandard = "cc7b13ffcd2ddd51";

        //
        // NOTE: Most of the ECMA compliant parts of the CLR use this
        //       public key token.
        //
        public static readonly string Ecma = "b77a5c561934e089";

        //
        // NOTE: Most of the Microsoft specific extensions to the CLR
        //       use this public key token.
        //
        public static readonly string Microsoft = "b03f5f7f11d50a3a";

        //
        // NOTE: Another public key token used for extensions to the
        //       CLR (mostly for Silverlight?).
        //
        public static readonly string SharedLib = "31bf3856ad364e35";

        //
        // NOTE: The Visual C++ runtime libraries use this public key
        //       token; however, only the managed C++ assemblies (e.g.
        //       "msvcm??.dll") are actually signed with it (i.e. the
        //       pure native libraries are not actually signed with
        //       it).
        //
        public static readonly string VcRuntime = "1fc8b3b9a1e18e3b";

        //
        // NOTE: This public key token was first used by Silverlight;
        //       However, it is now used by the .NET Core runtime as
        //       well.
        //
        public static readonly string Silverlight = "7cec85d7bea7798e";

        //
        // NOTE: The Windows Common Controls library uses this public
        //       key, as of version 6.0.
        //
        public static readonly string CommonControls = "6595b64144ccf1df";

        //
        // NOTE: The (open source) WiX project uses this public key
        //       token, as of version 3.x.
        //
        public static readonly string WiX = "ce35f76fcda82bad";

        //
        // NOTE: The SQL Server managed assemblies use this public key
        //       token.
        //
        public static readonly string SqlServer = "89845dcd8080cc91";

        //
        // NOTE: The (open source) System.Data.SQLite project uses this
        //       public key token.
        //
        public static readonly string SQLite = "db937bc2d44ff139";

        //
        // NOTE: The Microsoft Office managed assemblies use this public
        //       key token.
        //
        public static readonly string Office = "71e9bce111e9429c";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official demo licenses for Eagle Enterprise Edition are
        //       signed with this public key token ("EagleDemoPublic.snk",
        //       8192 bits).
        //
        public static readonly string Demo = "5f8230f3e7b9b317";

        //
        // NOTE: Official debug builds (whether public or private) of
        //       the Eagle runtime library are signed with this public
        //       key token ("EagleFastPublic.snk", 4096 bits).
        //
        public static readonly string Fast = "29c6297630be05eb";

        //
        // NOTE: Official public release builds of the Eagle runtime
        //       library are signed with this public key token
        //       ("EagleStrongPublic.snk", 16384 bits).
        //
        public static readonly string Strong = "1e22ec67879739a2";

        //
        // NOTE: Official pre-release builds of the Eagle runtime
        //       library may be signed with this public key token
        //       ("EagleBetaPublic.snk", 8200 bits).
        //
        public static readonly string Beta = "358030063a832bc3";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official licenses for the security packages within
        //       Eagle Enterprise Edition (i.e. "Harpy" and "Badge")
        //       are normally signed with this public key token
        //       ("EagleEnterprisePluginRootPublic.snk", 4096 bits).
        //
        public static readonly string Security = "8bf43b4749e46a0b";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official scripts for the core library and the security
        //       package plugins are normally signed with this public
        //       key token ("EagleEnterpriseClass0RootPublic.snk", 16384
        //       bits).
        //
        public static readonly string Class0 = "9559f6017247e3e2";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official key rings for the security packages within
        //       Eagle Enterprise Edition (i.e. "Harpy" and "Badge")
        //       are normally signed with this public key token
        //       ("EagleEnterpriseTrustRootPublic.snk", 16384 bits).
        //
        public static readonly string TrustRoot = "26f17c3a1a544324";
    }

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("c094f589-7ec2-4494-9cc0-498ea9fe2926")]
    public static class Build
    {
#if HAVE_SIZEOF
#if ARM64 || IA64 || X64
        public const int SizeOfIntPtr = 8;
#elif ARM || X86
        public const int SizeOfIntPtr = 4;
#else
        #warning "Missing define for ARM, ARM64, X86, X64, or IA64."
#endif
#endif

#if DEBUG
        public static readonly bool Debug = true;
#else
        public static readonly bool Debug = false;
#endif

#if CONSOLE
        public static readonly bool Console = true;
#else
        public static readonly bool Console = false;
#endif

#if VERBOSE
        public static readonly bool Verbose = true;
#else
        public static readonly bool Verbose = false;
#endif
    }
}
